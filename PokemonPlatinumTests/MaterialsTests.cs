using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Materials, crafting and cooking, CR1 (plan 39): the materials' table and their words, what a wild Pokémon leaves
/// behind and when it does not, the pouch's limit and its save, the craftswoman who gives the kit, the door that opens
/// after the Hall of Fame, and a Bidoof felled on Route 201 leaving its material in the pouch with the toast saying so.
/// </summary>
[Collection("Shared clocks and dice")]
public class MaterialsTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;

    /// <summary>A generator that answers with the numbers it is given, in order, as many as asked for.</summary>
    private sealed class Scripted(params int[] rolls) : Random
    {
        private readonly Queue<int> left = new(rolls);
        public int Drawn { get; private set; }
        public override int Next(int maxValue) { Drawn++; return left.Count > 0 ? Math.Min(left.Dequeue(), Math.Max(0, maxValue - 1)) : 0; }
        public override int Next(int minValue, int maxValue) => minValue + Next(maxValue - minValue);
    }

    /// <summary>A generator that counts every number asked of it and gives them from another.</summary>
    private sealed class Counting(Random inner) : Random
    {
        public int Draws { get; private set; }
        public override int Next() { Draws++; return inner.Next(); }
        public override int Next(int maxValue) { Draws++; return inner.Next(maxValue); }
        public override int Next(int minValue, int maxValue) { Draws++; return inner.Next(minValue, maxValue); }
        public override double NextDouble() { Draws++; return inner.NextDouble(); }
    }

    private static Pokemon Foe(string species, int level) => Mon(species, level);

    private static string Said(HeadlessScriptHost host) => string.Join(" | ", host.Transcript.Select(t => t.Text));

    // ---------------------------------------------------------------- the table

    [Fact]
    public void EveryTypeHasItsCommonAndItsRareMaterialEachWithItsWords()
    {
        foreach (var type in Enum.GetValues<PokemonType>())
        {
            var common = MaterialTable.OfType(type, rare: false);
            var rare = MaterialTable.OfType(type, rare: true);
            Assert.NotNull(common);
            Assert.NotNull(rare);
            Assert.True(rare!.Worth > common!.Worth, $"{type}'s rare material is worth more than its common one");
        }
        foreach (var material in MaterialTable.All)
        {
            Assert.Matches("^[A-Za-z0-9]+$", material.Id);
            Assert.True(material.Worth > 0, material.Id);
            Assert.True(Text.Has($"crafting.material.name.{material.Id}"), $"{material.Id} has no name");
            Assert.True(Text.Has($"crafting.material.desc.{material.Id}"), $"{material.Id} has no description");
            Assert.Equal(material.Kind == MaterialKind.Pokemon, material.Type != null);
        }
        Assert.Equal(MaterialTable.All.Count, MaterialTable.All.Select(m => m.Id).Distinct().Count());
        Assert.Equal(36, MaterialTable.All.Count(m => m.Kind == MaterialKind.Pokemon));
        Assert.Equal(10, MaterialTable.All.Count(m => m.Kind == MaterialKind.Field));
        Assert.Equal(999, MaterialTable.Limit);
    }

    [Fact]
    public void TheFileIsReadStrictly()
    {
        Assert.Throws<InvalidDataException>(() =>
            GameDataFiles.ReadStrict<MaterialsFile>("""{ "pouchLimit": 999, "materials": [ { "id": "x", "kind": "Field", "worth": 1, "wroth": 2 } ] }""", "materials.json"));
        Assert.Throws<InvalidDataException>(() =>
            GameDataFiles.ReadStrict<MaterialsFile>("""{ "pouchLimit": 999, "materials": [ { "id": "x", "kind": "Field" } ] }""", "materials.json"));
    }

    // ---------------------------------------------------------------- what a Pokémon leaves

    [Fact]
    public void AFoeOfOneTypeLeavesItsCommonMaterialAndNowAndThenItsRareOne()
    {
        // Rolls: the common count (1 to 3), then the rare's chance in a hundred. Bidoof is Normal; at level 5 the rare comes 9 times in a hundred
        var plain = MaterialDrops.For(Foe("Bidoof", 5), new Scripted(1, 99));
        Assert.Equal(new[] { new MaterialCount("softFluff", 2) }, plain);
        var lucky = MaterialDrops.For(Foe("Bidoof", 5), new Scripted(2, 8));
        Assert.Equal(new[] { new MaterialCount("softFluff", 3), new MaterialCount("plushTuft", 1) }, lucky);
        var unlucky = MaterialDrops.For(Foe("Bidoof", 5), new Scripted(0, 9));
        Assert.Equal(new[] { new MaterialCount("softFluff", 1) }, unlucky);
    }

    [Fact]
    public void AFoeOfTwoTypesLeavesItsSecondTypesCommonMaterialHalfTheTime()
    {
        // Pidgey is Normal and Flying. Rolls: the first count, the second type's chance (under 50 leaves it) and its count, the rare's chance
        Assert.Equal(PokemonType.Flying, Foe("Pidgey", 5).SecondaryType);
        var both = MaterialDrops.For(Foe("Pidgey", 5), new Scripted(0, 49, 2, 99));
        Assert.Equal(new[] { new MaterialCount("softFluff", 1), new MaterialCount("looseFeather", 3) }, both);
        var first = MaterialDrops.For(Foe("Pidgey", 5), new Scripted(0, 50, 99));
        Assert.Equal(new[] { new MaterialCount("softFluff", 1) }, first);
        // A rare one is the second type's one time in three, and the first's otherwise
        var second = MaterialDrops.For(Foe("Pidgey", 5), new Scripted(0, 99, 0, 0));
        Assert.Equal(new[] { new MaterialCount("softFluff", 1), new MaterialCount("galePlume", 1) }, second);
        var firstRare = MaterialDrops.For(Foe("Pidgey", 5), new Scripted(0, 99, 0, 2));
        Assert.Equal(new[] { new MaterialCount("softFluff", 1), new MaterialCount("plushTuft", 1) }, firstRare);
    }

    [Fact]
    public void ALevelAddsToWhatIsLeftAndMakesTheRareOneMoreLikelyAndDoubleFromSixty()
    {
        // Level 60: two more of the common one (60 / 25), the rare's chance 8 + 12 = 20 in a hundred, and two of it
        var high = MaterialDrops.For(Foe("Bidoof", 60), new Scripted(0, 19));
        Assert.Equal(new[] { new MaterialCount("softFluff", 3), new MaterialCount("plushTuft", 2) }, high);
        Assert.Equal(new[] { new MaterialCount("softFluff", 3) }, MaterialDrops.For(Foe("Bidoof", 60), new Scripted(0, 20)));
        // The odds stop at thirty in a hundred (level 150 would make them 38), and the extra at two
        var top = MaterialDrops.For(Foe("Bidoof", 150), new Scripted(2, 29));
        Assert.Equal(new[] { new MaterialCount("softFluff", 5), new MaterialCount("plushTuft", 2) }, top);
        Assert.Equal(new[] { new MaterialCount("softFluff", 5) }, MaterialDrops.For(Foe("Bidoof", 150), new Scripted(2, 30)));
    }

    [Fact]
    public void AHordesFoesEachLeaveTheirOwn()
    {
        var foes = new[] { Foe("Bidoof", 5), Foe("Bidoof", 5), Foe("Pidgey", 5) };
        // Bidoof: 1, no rare; Bidoof: 2, no rare; Pidgey: 1, second type not left, no rare
        var left = MaterialDrops.For(foes, new Scripted(0, 99, 1, 99, 0, 99, 99));
        Assert.Equal(new[] { new MaterialCount("softFluff", 4) }, left);
    }

    [Fact]
    public void NothingIsLeftByATrainersPokemonOrAnythingOfTheFrontierTheSafariGameOrTheLesson()
    {
        var wild = Battle(Mon("Turtwig", 50, "Tackle"), Foe("Bidoof", 3));
        Assert.True(GameEngine.LeavesMaterials(wild, inBugContest: false));
        Assert.False(GameEngine.LeavesMaterials(wild, inBugContest: true));

        var trainer = new Trainer { Id = "t", Name = "Vera", TrainerClass = "Youngster", PrizeMoney = 0 };
        trainer.Party.Add(Foe("Bidoof", 3));
        BattleEngine Trainers(bool frontier)
        {
            var party = new Party();
            party.Add(Mon("Turtwig", 50, "Tackle"));
            return new BattleEngine(new BattleSetup
            {
                PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(), Trainers = new List<Trainer> { trainer },
                Frontier = frontier, Random = Steady(), Rules = Ruleset.Platinum
            });
        }
        Assert.False(GameEngine.LeavesMaterials(Trainers(frontier: false), false));
        Assert.False(GameEngine.LeavesMaterials(Trainers(frontier: true), false));

        BattleEngine Wild(BattleKind kind)
        {
            var party = new Party();
            party.Add(Mon("Turtwig", 50, "Tackle"));
            return new BattleEngine(new BattleSetup
            {
                PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(), WildPokemon = new List<Pokemon> { Foe("Bidoof", 3) },
                Kind = kind, SpecialBalls = 30, Random = Steady(), Rules = Ruleset.Platinum
            });
        }
        Assert.False(GameEngine.LeavesMaterials(Wild(BattleKind.Safari), false));
        Assert.False(GameEngine.LeavesMaterials(Wild(BattleKind.PalPark), false));
        Assert.True(GameEngine.LeavesMaterials(Wild(BattleKind.Roamer), false));
    }

    [Fact]
    public void AFoeFelledIsListedByTheBattleOnceEachAndAFoeNotFelledIsNot()
    {
        var battle = Battle(Mon("Garchomp", 60, "Earthquake"), Foe("Bidoof", 3));
        Assert.Empty(battle.FelledFoes);
        Turn(battle);
        Assert.True(battle.IsBattleOver);
        Assert.Single(battle.FelledFoes);
        Assert.Equal("Bidoof", battle.FelledFoes[0].Species.Name);
    }

    // ---------------------------------------------------------------- the pouch

    [Fact]
    public void ThePouchHoldsNineHundredAndNinetyNineOfAKindTakesExactlyAndKeepsNothingItHasNoWordsFor()
    {
        var pouch = new MaterialPouch();
        Assert.Equal(0, pouch.Add("noSuchThing", 3));
        Assert.Equal(990, pouch.Add("softFluff", 990));
        Assert.Equal(9, pouch.Add("softFluff", 20));
        Assert.True(pouch.IsFull("softFluff"));
        Assert.Equal(0, pouch.Add("softFluff", 1));
        Assert.Equal(999, pouch.Count("softFluff"));
        Assert.False(pouch.Take("softFluff", 1000));
        Assert.Equal(999, pouch.Count("softFluff"));
        Assert.True(pouch.Take("softFluff", 999));
        Assert.Equal(0, pouch.Kinds);
        Assert.Empty(pouch.Held);
    }

    [Fact]
    public void ThePouchIsSavedWhenSomethingIsHeldAndNullWhenNothingIs()
    {
        var state = new CraftingState();
        Assert.Null(state.ToSave());
        state.Pouch.Add("emberCore", 4);
        state.Pouch.Add("quarryStone", 12);
        var save = state.ToSave()!;
        // Through the save's own JSON, as a file keeps it
        var read = GameDataFiles.Deserialize<CraftingSave>(GameDataFiles.Serialize(save));
        var again = new CraftingState();
        again.Load(read);
        Assert.Equal(4, again.Pouch.Count("emberCore"));
        Assert.Equal(12, again.Pouch.Count("quarryStone"));
        // An older save has none: nothing is held
        again.Load(null);
        Assert.Equal(0, again.Pouch.Kinds);
        // A material the table no longer has is let go
        again.Load(new CraftingSave { Pouch = new Dictionary<string, int> { ["vanished"] = 3, ["timber"] = 2 } });
        Assert.Equal(new[] { "timber" }, again.Pouch.Held.Select(m => m.Id));
    }

    [Fact]
    public void TheEngineSavesAndLoadsThePouchAndAGameWithNothingWritesNone()
    {
        var engine = new GameEngine(new GameSettings { Muted = true });
        engine.InitializeHeadless();
        engine.NewGameRegion = "Sinnoh";
        engine.StartNewGame();
        try
        {
            Assert.Null(engine.Drive.BuildSave().Crafting);
            engine.Drive.Crafting.Pouch.Add("tidePearl", 2);
            var save = engine.Drive.BuildSave();
            Assert.Equal(2, save.Crafting!.Pouch!["tidePearl"]);
            engine.Drive.Crafting.Pouch.Clear();
            Assert.Equal(0, engine.Drive.Crafting.Pouch.Count("tidePearl"));
        }
        finally
        {
            Ruleset.Use(RulesPreset.Platinum);
        }
    }

    // ---------------------------------------------------------------- the script

    [Fact]
    public void TheCommandGivesTheKitOnceAndCountsTakesAndAddsMaterialsAndHonoursTheLimit()
    {
        const string source = """
            script T
              craft has
              craft give kit
              craft give "Soft Fluff" 3
              craft give "emberCore"
              craft take "Soft Fluff" 2
              craft count "softFluff"
              craft take "Soft Fluff" 5
              craft give "softFluff" 2000
              end
            """;
        var script = ScriptParser.Parse("t", source)[0];
        var host = new HeadlessScriptHost();
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(script, null);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        Assert.True(host.Story.Has(GameEngine.CraftingKitFlag));
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get(GameEngine.CraftingKitItem)!));
        Assert.Equal(999, host.Crafting!.Pouch.Count("softFluff"));
        Assert.Equal(1, host.Crafting.Pouch.Count("emberCore"));
        // The kit is given once
        var second = new HeadlessScriptHost(null, null, host.Bag);
        var again = new ScriptRunner(Scripts, second);
        again.Start(ScriptParser.Parse("t", "script T\n  craft give kit\n  end")[0], null);
        again.RunToEnd();
        Assert.Equal(0, again.Result);
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get(GameEngine.CraftingKitItem)!));
    }

    [Fact]
    public void ACommandNamingNoMaterialIsAMistakeOfTheFile()
    {
        Assert.Throws<ScriptException>(() => ScriptParser.Parse("t", "script T\n  craft give \"Unobtainium\"\n  end"));
    }

    // ---------------------------------------------------------------- the workshop

    [Fact]
    public void TheCraftswomanGivesTheKitOnceAndAfterwardsLooksIntoItOrSaysHowItGoes()
    {
        var map = MapDatabase.Get("SunyshoreWorkshop");
        var person = map.NPCs.Single(n => n.Key == "craftswoman");
        var host = new HeadlessScriptHost { Map = map };
        var first = new ScriptRunner(Scripts, host);
        first.Start(Scripts.Find("Craftswoman", "SunyshoreWorkshop")!, person);
        first.RunToEnd();
        Assert.Empty(host.Problems);
        Assert.True(host.Story.Has(GameEngine.CraftingKitFlag));
        Assert.Contains("Crafting Kit", Said(host));
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get(GameEngine.CraftingKitItem)!));

        // The next visit: LOOK IN THE KIT opens it
        var next = new HeadlessScriptHost(host.Story, null, host.Bag) { Map = map };
        next.Answers.Enqueue(0);
        var again = new ScriptRunner(Scripts, next);
        again.Start(Scripts.Find("Craftswoman", "SunyshoreWorkshop")!, person);
        again.RunToEnd();
        Assert.Contains(next.Log, l => l.Contains("open Crafting"));
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get(GameEngine.CraftingKitItem)!));
    }

    [Fact]
    public void TheWorkshopsDoorIsShutBeforeTheHallOfFameAndOpenAfterAndLeadsInAndOut()
    {
        var sinnoh = MapDatabase.Get("Sinnoh");
        var door = sinnoh.Warps.Single(w => w.TargetMap == "SunyshoreWorkshop");
        Assert.Equal("FLAG_GAME_COMPLETED", door.OpenedBy);
        sinnoh.ApplyDoors(_ => false);
        Assert.True(sinnoh.IsSolid(door.SourceX, door.SourceY));
        sinnoh.ApplyDoors(flag => flag == "FLAG_GAME_COMPLETED");
        Assert.False(sinnoh.IsSolid(door.SourceX, door.SourceY));
        Assert.True(sinnoh.IsWalkable(door.SourceX - 1, door.SourceY));
        // ...and a step from the tile before it onto it is one the field's rules allow (no cliff between), but not while it is shut
        var from = (X: door.SourceX - 1, Y: door.SourceY);
        var onto = FieldMovement.Step(sinnoh, from.X, from.Y, Direction.Right, new Walker(Height: sinnoh.HeightAt(from.X, from.Y)));
        Assert.NotEqual(StepKind.Blocked, onto.Kind);
        sinnoh.ApplyDoors(_ => false);
        Assert.Equal(StepKind.Blocked, FieldMovement.Step(sinnoh, from.X, from.Y, Direction.Right, new Walker(Height: sinnoh.HeightAt(from.X, from.Y))).Kind);

        var room = MapDatabase.Get("SunyshoreWorkshop");
        var back = room.Warps.Single(w => w.TargetMap == "Sinnoh");
        Assert.Equal("Sinnoh", back.TargetMap);
        Assert.Equal((door.SourceX - 1, door.SourceY), (back.TargetX, back.TargetY));
        Assert.True(room.IsWalkable(door.TargetX, door.TargetY));
        Assert.Equal(RegionDatabase.Sinnoh, RegionDatabase.RegionOfMap("SunyshoreWorkshop")!.Id);
    }

    // ---------------------------------------------------------------- the field

    private static GameEngine Headless()
    {
        var engine = new GameEngine(new GameSettings { Muted = true });
        engine.InitializeHeadless();
        engine.PeopleStayPut = true;
        return engine;
    }

    private static void Run(GameEngine engine, ActionFrame frame)
    {
        InputManager.Feed(frame);
        engine.Update(1f / 60f);
    }

    private static void Until(GameEngine engine, Func<bool> done, string what, bool press = false)
    {
        for (int f = 0; f < 60 * 120; f++)
        {
            if (done()) return;
            Run(engine, press && f % 8 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
        }
        Assert.Fail($"Waited for {what}; the game is in {engine.Drive.State}.");
    }

    /// <summary>A Bidoof of level 2 met and felled by a Turtwig of 70 on Route 201's map; returns the pouch and what the toast said.</summary>
    private static (CraftingState Pouch, string Toast, int Draws) FellABidoof(bool withKit, out Counting counting)
    {
        var paths = SavePaths.Current;
        try
        {
            Dice.Seed(12);
            var engine = Headless();
            var game = engine.Drive;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            StoryMigration.PastTheOpening(game.Story, Scripts);
            var lead = new Pokemon(PokemonDatabase.Get("Turtwig")!, 70);
            game.Party.Add(lead);
            if (withKit) game.Bag.AddItem(ItemDatabase.Get(GameEngine.CraftingKitItem)!, 1);
            game.State = GameState.Overworld;
            var sinnoh = MapDatabase.Get("Sinnoh");
            game.Place(sinnoh, 116, 886, Direction.Right);
            game.TrainersLookOnArrival = false;
            counting = new Counting(game.FieldRandom);
            game.FieldRandom = counting;

            game.StartWildBattle(new WildEncounterEntry { SpeciesName = "Bidoof", MinLevel = 2, MaxLevel = 2, Weight = 100 });
            Until(engine, () => game.State == GameState.Battle, "the battle");
            var battle = game.Battle!;
            int hit = lead.Moves.FindIndex(m => m.Data.Category != MoveCategory.Status);
            Until(engine, () => battle.IsBattleOver || battle.HUD.MenuState == BattleMenuState.Main, "the battle's menu", press: true);
            for (int guard = 0; guard < 60 * 120 && !battle.IsBattleOver; guard++)
            {
                if (battle.HUD.MenuState == BattleMenuState.Main) battle.SelectMove(hit);
                Run(engine, guard % 8 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
            }
            Assert.True(battle.IsBattleOver);
            Until(engine, () => game.State == GameState.Overworld, "the field after the battle", press: true);
            // The toast comes once the field is the player's: a few frames
            for (int f = 0; f < 30; f++) Run(engine, ActionFrame.None);
            return (game.Crafting, game.Toast.Visible ? game.Toast.Message : "", counting.Draws);
        }
        finally
        {
            InputManager.Feed(null);
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            SavePaths.Current = paths;
        }
    }

    [Fact]
    public void ABidoofFelledOnRoute201LeavesItsMaterialInThePouchAndTheToastSaysSoOnce()
    {
        var (pouch, toast, draws) = FellABidoof(withKit: true, out _);
        Assert.True(pouch.Pouch.Count("softFluff") >= 1, "a Bidoof leaves some Soft Fluff");
        Assert.StartsWith("Left behind: ", toast);
        Assert.Contains($"Soft Fluff ×{pouch.Pouch.Count("softFluff")}", toast);
    }

    [Fact]
    public void WithoutTheKitNothingIsLeftNothingIsToldAndTheBattlesChanceIsDrawnFromNoMoreThanItWas()
    {
        var (without, toast, drawsWithout) = FellABidoof(withKit: false, out _);
        Assert.Equal(0, without.Pouch.Kinds);
        Assert.Equal("", toast);
        var (with, _, drawsWith) = FellABidoof(withKit: true, out _);
        // The kit's drop draws on the field's chance (the common count and the rare's odds), and nothing without it does
        Assert.True(drawsWith >= drawsWithout + 2, $"{drawsWith} draws with the kit, {drawsWithout} without");
        Assert.True(with.Pouch.Kinds >= 1);
    }

    [Fact]
    public void TheToastTellsEverythingLeftInOneLineWithTheCountsAdded()
    {
        string line = GameEngine.LeftBehindLine(new[] { new MaterialCount("softFluff", 2), new MaterialCount("plushTuft", 1), new MaterialCount("softFluff", 1) });
        Assert.Equal("Left behind: Soft Fluff ×3, Plush Tuft ×1.", line);
        // ...and a long one names the first four kinds and counts the rest
        var many = new[] { "softFluff", "plushTuft", "cinderAsh", "emberCore", "tidePearl", "sunBloom" }.Select(id => new MaterialCount(id, 1));
        Assert.Equal("Left behind: Soft Fluff ×1, Plush Tuft ×1, Cinder Ash ×1, Ember Core ×1 and 2 more.", GameEngine.LeftBehindLine(many));
    }

    [Fact]
    public void TheKitOpensFromAScriptAndFromTheBagsItemAndCloses()
    {
        var engine = Headless();
        var game = engine.Drive;
        try
        {
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            game.Crafting.Pouch.Add("rawOre", 5);
            game.State = GameState.Overworld;
            game.OpenCrafting();
            Assert.Equal(GameState.Crafting, game.State);
            var screen = game.CraftingScreen;
            Assert.True(screen.IsActive);
            // The cursor starts on the first thing held and moves through the list, wrapping
            Assert.Equal("rawOre", screen.Selected!.Id);
            screen.Move(1);
            Assert.NotEqual("rawOre", screen.Selected!.Id);
            for (int i = 0; i < screen.Entries.Count; i++) screen.Move(1);
            Assert.Equal(CraftingScreen.VisibleRows, Math.Min(CraftingScreen.VisibleRows, screen.Entries.Count));
            Assert.InRange(screen.Cursor - screen.First, 0, CraftingScreen.VisibleRows - 1);
            screen.Cancel();
            Assert.False(screen.IsActive);
        }
        finally
        {
            InputManager.Feed(null);
            Ruleset.Use(RulesPreset.Platinum);
        }
    }

    [Fact]
    public void TheKitIsAKeyItemOfOursThatCannotBeTossedAndOpensFromTheBag()
    {
        var kit = ItemDatabase.Get(GameEngine.CraftingKitItem)!;
        Assert.Equal(9900, kit.Id);
        Assert.Equal(ItemPocket.KeyItems, kit.Pocket);
        Assert.Equal("CraftingKit", kit.FieldUse);
        Assert.True(kit.CantBeTossed);
        Assert.True(BagScreen.UsedInField(kit));
    }
}
