using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Story;
using Xunit;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Paldea's Tera Raids (plan 34 · PD7; <c>docs/mechanics/rulings.md</c>, "Tera Raids and picnics"): Galar's raid with a boss
/// Terastallized into the crystal's type for the whole battle, its HP raised by its stars, a crystal shield at four stars and
/// more that a Terastallized raider breaks three at a time, the raiders' Tera Orbs charged by the moves they use, and a clock
/// of turns (two more for every fall) in place of the fourth fall; it ends on the catch the crystal's script makes, which
/// never fails, with the boss's Tera Shards; it replays from the Vs. Recorder's file. What a crystal holds is drawn by the day.
/// </summary>
public class PaldeaTeraRaidTests
{
    private static readonly DateTime Day = new(2027, 5, 1);

    private static List<Trainer> Students(int level) => Raids.Allies(Raids.Crystal("area_two_1")!, 7, 1, level, Raids.Paldea);

    private static BattleSetup Setup(int stars, int level, uint seed, TeraType tera, string boss = "Dolliv", string mine = "Skeledirge", int mineLevel = 70,
        int? allyLevel = null, Ruleset? rules = null, string[]? moves = null)
    {
        var party = new Party();
        party.Add(Mon(mine, mineLevel, moves ?? new[] { "Flamethrower", "Shadow Ball", "Torch Song", "Snarl" }));
        var foe = Mon(boss, level, "Razor Leaf", "Tackle", "Growth", "Sweet Scent");
        var bag = new Inventory();
        bag.AddItem(ItemDatabase.Get(BattleCore.TeraOrbItem)!, 1);
        return new BattleSetup
        {
            PlayerParty = party, Inventory = bag, Pokedex = new Pokedex(),
            WildPokemon = { foe },
            Random = new BattleRandom(seed), Rules = rules ?? Ruleset.Modern, Format = BattleFormat.Raid,
            Raid = new RaidTerms(stars, Students(allyLevel ?? level), Tera: tera), PlayerName = "Juliana"
        };
    }

    private static BattleEngine Fought(BattleSetup setup, bool terastallize = false)
    {
        var battle = new BattleEngine(setup);
        for (int i = 0; i < 8000 && !battle.IsBattleOver; i++)
        {
            if (battle.HUD.MenuState == BattleMenuState.Message) battle.ConfirmMessage();
            else
            {
                // The player's Pokémon Terastallizes as soon as its orb lets it, where asked
                if (terastallize && battle.CanTerastallizeNow && !battle.TeraArmed) battle.ToggleTera();
                battle.SelectMove(0);
            }
            battle.Update(1f / 60f);
        }
        Assert.True(battle.IsBattleOver, $"turn {battle.Core.Turn}, falls {battle.Core.RaidFalls}, waiting for {battle.Core.Request}");
        return battle;
    }

    private static List<string> Lines(BattleCore core) => core.Log.OfType<Said>().Select(s => s.Text).ToList();

    [Fact]
    public void ATeraRaidsBossTerastallizesIntoTheCrystalsTypeForTheWholeBattleWithItsHpRaised()
    {
        Assert.Equal((3, 8, 20), (RaidRules.TeraHpTimes(1), RaidRules.TeraHpTimes(4), RaidRules.TeraHpTimes(7)));
        var battle = new BattleEngine(Setup(3, 30, 34701, TeraType.Of(PokemonType.Fire)));
        var core = battle.Core;
        Assert.True(core.IsRaid);
        Assert.True(core.IsTeraRaid);
        Assert.Equal(4, core.PlayerSlots.Count);
        var boss = core.EnemySlots[0].Pokemon!;
        Assert.Equal(TeraType.Of(PokemonType.Fire), BattleCore.TeraOf(core.EnemySlots[0]));
        // Dynamaxing nothing: a crystal knows no Dynamax energy
        Assert.False(core.EnemySlots[0].Volatile.Dynamaxed);
        Assert.NotNull(core.WhyNotDynamax(core.PlayerSlots[0]));
        // Six times its own HP at three stars
        var plain = Mon("Dolliv", 30);
        Assert.Equal(plain.MaxHP * RaidRules.TeraHpTimes(3), boss.MaxHP);
        var lines = Lines(core);
        Assert.Contains(lines, l => l.Contains("ablaze with Tera energy"));
        Assert.Contains(Text.Of("battle.teraRaidClock", ("n", RaidRules.TeraTurns(3))), lines);
        Assert.Equal(RaidRules.TeraTurns(3), core.TeraTurnsLeft);
    }

    [Fact]
    public void ARaidersOrbChargesByTheMovesItUsesBeforeItMayTerastallize()
    {
        var battle = new BattleEngine(Setup(2, 40, 34702, TeraType.Of(PokemonType.Grass), mineLevel: 30));
        var core = battle.Core;
        var me = core.PlayerSlots[0];
        Assert.Equal(0, core.OrbChargeOf(me));
        Assert.Equal(Text.Of("battle.teraOrbNotCharged"), core.WhyNotTera(me));
        for (int i = 0; i < 4000 && core.OrbChargeOf(me) < RaidRules.OrbCharge && !battle.IsBattleOver; i++)
        {
            if (battle.HUD.MenuState == BattleMenuState.Message) battle.ConfirmMessage();
            else battle.SelectMove(3);
            battle.Update(1f / 60f);
        }
        Assert.Equal(RaidRules.OrbCharge, core.OrbChargeOf(me));
        Assert.Null(core.WhyNotTera(me));
        Assert.Contains(Lines(core), l => l.Contains("Tera Orb is charged"));
    }

    [Fact]
    public void TheCrystalShieldGoesUpAtFourStarsAndATerastallizedRaiderBreaksThreeAtATime()
    {
        Assert.Equal((0, 50, 75), (RaidRules.TeraShieldAt(3), RaidRules.TeraShieldAt(4), RaidRules.TeraShieldAt(5)));
        Assert.Equal((0, 4, 6), (RaidRules.TeraShields(3), RaidRules.TeraShields(4), RaidRules.TeraShields(6)));
        Assert.Equal((1, 3), (RaidRules.CrystalBreaksOf(false), RaidRules.CrystalBreaksOf(true)));
        var battle = Fought(Setup(4, 40, 34703, TeraType.Of(PokemonType.Grass), mineLevel: 80, allyLevel: 70), terastallize: true);
        var barriers = battle.Core.Log.OfType<Said>().SelectMany(s => s.Shows.OfType<RaidBarrier>()).Select(b => b.Shields).ToList();
        Assert.NotEmpty(barriers);
        Assert.Equal(RaidRules.TeraShields(4), barriers[0]);
        for (int i = 1; i < barriers.Count; i++) Assert.Contains(barriers[i - 1] - barriers[i], new[] { 1, 2, 3 });
        Assert.Contains(Lines(battle.Core), l => l.Contains("crystal shield"));
        Assert.Equal(BattleResult.PlayerVictory, battle.Result);
        Assert.Equal(0, barriers.Last());
    }

    [Fact]
    public void TheClockLosesARaidOnceItsTurnsAndTheFallsTakeOffItAreSpent()
    {
        Assert.Equal((10, 12, 15, 18), (RaidRules.TeraTurns(2), RaidRules.TeraTurns(4), RaidRules.TeraTurns(5), RaidRules.TeraTurns(7)));
        // A weak party against a five-star boss: no fall ends it, but the clock does
        var battle = Fought(Setup(5, 65, 34704, TeraType.Of(PokemonType.Grass), boss: "Arboliva", mine: "Lechonk", mineLevel: 8, allyLevel: 8,
            moves: new[] { "Tackle", "Tail Whip" }));
        var core = battle.Core;
        Assert.Equal(BattleResult.PlayerDefeat, battle.Result);
        var lines = Lines(core);
        Assert.Contains(Text.Of("battle.teraRaidTimeUp"), lines);
        Assert.DoesNotContain(Text.Of("battle.raidBlownOut"), lines);
        Assert.True(core.Turn + 1 + RaidRules.FallCost * core.RaidFalls >= RaidRules.TeraTurns(5), $"turn {core.Turn}, falls {core.RaidFalls}");
        Assert.True(core.Turn + 1 <= RaidRules.TeraTurns(5));
    }

    [Fact]
    public void ATeraRaidReplaysFromItsFile()
    {
        var battle = Fought(Setup(3, 30, 34705, TeraType.Of(PokemonType.Water)), terastallize: true);
        Assert.Equal(BattleResult.PlayerVictory, battle.Result);
        var file = battle.RecordFile(Day)!;
        Assert.Equal(3, file.RaidStars);
        Assert.Equal("Water", file.RaidTera);
        var read = BattleRecordFile.Read(file.Serialize());
        var again = read.File!.Replay();
        Assert.Equal(battle.Result, again.Result);
        Assert.Equal(Golden.BattleText.Written(battle.Core), Golden.BattleText.Written(again));
        // A den's raid writes no Tera Type
        Assert.Null(new BattleEngine(new BattleSetup
        {
            PlayerParty = battle.Core.PlayerParty, Inventory = new Inventory(), Pokedex = new Pokedex(), WildPokemon = { Mon("Onix", 30) },
            Random = new BattleRandom(1), Rules = Ruleset.Modern, Format = BattleFormat.Raid,
            Raid = new RaidTerms(3, Students(30)), PlayerName = "Juliana"
        }).RecordFile(Day)?.RaidTera);
    }

    [Fact]
    public void ACrystalsDayIsDrawnByTheDayAndItsStarsByPaldeasBadges()
    {
        Assert.Equal((2, 3, 4, 5), (Raids.TeraMostStars(0), Raids.TeraMostStars(3), Raids.TeraMostStars(6), Raids.TeraMostStars(8)));
        var crystal = Raids.Crystal("area_two_1")!;
        Assert.Equal(Raids.CrystalToday(crystal, 4321, 1, 0), Raids.CrystalToday(crystal, 4321, 1, 0));
        for (uint day = 0; day < 60; day++)
        {
            var today = Raids.CrystalToday(crystal, day, 1, 0);
            Assert.InRange(today.Stars, 1, 2);
            Assert.NotNull(today.Tera);
            Assert.False(today.Tera!.Value.IsStellar);
            Assert.NotEqual(PokemonType.Sound, today.Tera.Value.Type);
        }
        Assert.Contains(Enumerable.Range(0, 200).Select(d => Raids.CrystalToday(crystal, (uint)d, 1, 8).Stars), s => s >= 4);
        Assert.Contains(Enumerable.Range(0, 60).Select(d => Raids.CrystalToday(crystal, (uint)d, 1, 0).Lit), lit => lit);
        Assert.Contains(Enumerable.Range(0, 60).Select(d => Raids.CrystalToday(crystal, (uint)d, 1, 0).Lit), lit => !lit);
        // The species' own types are the most often drawn, and a fixed row keeps its type
        var types = Enumerable.Range(0, 300).Select(d => Raids.CrystalToday(crystal, (uint)d, 1, 0)).ToList();
        Assert.Contains(types, t => t.Tera!.Value.Type == PokemonType.Grass);
        var fixedRow = Raids.Crystal("east_three_1")!;
        Assert.All(Enumerable.Range(0, 400).Select(d => Raids.CrystalToday(fixedRow, (uint)d, 1, 8)).Where(t => t.Species == "Orthworm"),
            t => Assert.Equal(TeraType.Of(PokemonType.Steel), t.Tera));
        // Every crystal's rows are species of the data, its fixed types types, its raiders' species too
        foreach (var c in Raids.Paldea.Dens)
            foreach (var row in c.Rows)
            {
                Assert.True(PokemonDatabase.Get(row.Species) != null || PokemonDatabase.SpeciesOfForm(row.Species) != null, row.Species);
                Assert.InRange(row.Stars, 1, 7); // (six and seven the black crystals, plan 34 · PD9)
                if (row.Tera != null) Assert.NotNull(TeraType.Parse(row.Tera));
            }
        foreach (var raider in Raids.Paldea.Raiders)
            foreach (string s in raider.Species) Assert.NotNull(PokemonDatabase.Get(s));
    }

    [Fact]
    public void ACrystalsScriptEndsOnTheCatchWithItsTeraShardsAndNeedsATeraOrb()
    {
        var crystal = Raids.Crystal("area_two_1")!;
        uint lit = (uint)Enumerable.Range(0, 60).First(d => Raids.CrystalToday(crystal, (uint)d, 1, 0).Lit);
        var today = Raids.CrystalToday(crystal, lit, 1, 0);
        var story = new StoryState();
        story.SetVar("VAR_PALDEA_VERSION", 1);
        var party = new Party();
        party.Add(Mon("Fuecoco", 14, "Ember"));
        var bag = new Inventory();
        bag.AddItem(ItemDatabase.Get("Poké Ball")!, 3);

        // With no Tera Orb the crystal only hums
        var noOrb = new HeadlessScriptHost(story, party, bag) { Region = RegionDatabase.Paldea, DayNumber = lit };
        var first = new ScriptRunner(ScriptLibrary.Default, noOrb);
        first.Start(ScriptLibrary.Default.Find("CrystalAreaTwo1", "south_province_area_two")!, null);
        first.RunToEnd();
        Assert.Empty(noOrb.Problems);
        Assert.DoesNotContain(noOrb.Log, l => l.StartsWith("raidbattle "));
        Assert.Contains(noOrb.Transcript, t => t.Text.Contains("wants a Tera Orb"));

        bag.AddItem(ItemDatabase.Get(BattleCore.TeraOrbItem)!, 1);
        var host = new HeadlessScriptHost(story, party, bag) { Region = RegionDatabase.Paldea, DayNumber = lit };
        host.Answers.Enqueue(0);
        host.Answers.Enqueue(0);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("CrystalAreaTwo1", "south_province_area_two")!, null);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        Assert.Contains(host.Log, l => l.StartsWith($"raidbattle {PokemonDatabase.SpeciesOfForm(today.Species)?.Name ?? today.Species} ") && l.Contains($"tera {today.Tera!.Value.Name}"));
        // The shards of its type, and a catch that never fails (no die thrown)
        var shard = ItemDatabase.Get(TeraChange.ShardOf(today.Tera!.Value))!;
        Assert.Equal(Raids.ShardsOf(today.Stars), bag.GetQuantity(shard));
        Assert.Equal(2, party.Members.Count);
        var caught = party.Members[1];
        Assert.Equal(today.Species, caught.Form ?? caught.Species.Name);
        Assert.Equal(today.Tera, caught.TeraType);
        Assert.Equal(2, bag.GetQuantity(ItemDatabase.Get("Poké Ball")!));
        Assert.True(story.Has(ScriptRunner.CrystalFlag("area_two_1")));

        // Felled today, it is quiet, and says nothing
        var again = new HeadlessScriptHost(story, party, bag) { Region = RegionDatabase.Paldea, DayNumber = lit };
        var second = new ScriptRunner(ScriptLibrary.Default, again);
        second.Start(ScriptLibrary.Default.Find("CrystalAreaTwo1", "south_province_area_two")!, null);
        second.RunToEnd();
        Assert.Empty(again.Transcript);
        Assert.Empty(again.Asked);
    }

    [Fact]
    public void EveryCrystalTriggerNamesACrystalAndItsGlowStandsWhereItsScriptSaysItIsLit()
    {
        var map = OpenRegions.Load(RegionDatabase.Get(RegionDatabase.Paldea)!, withLand: false).Single(m => m.Name == "Paldea");
        var crystals = map.Triggers.Where(t => t.Name.StartsWith("crystal_")).ToList();
        Assert.Equal(Raids.Paldea.Dens.Count, crystals.Count);
        Assert.All(crystals, t => Assert.NotNull(Raids.CrystalOfTrigger(t.Name)));
        Assert.All(Raids.Paldea.Dens, c => Assert.Contains(crystals, t => t.Name == "crystal_" + c.Id));
        Assert.Null(Raids.CrystalOfTrigger("den_rolling_1"));
        // Each crystal's script is in its place's file
        foreach (var t in crystals) Assert.NotNull(ScriptLibrary.Default.Find(t.Script, t.Place));

        var crystal = Raids.Crystal("poco_1")!;
        uint lit = (uint)Enumerable.Range(0, 60).First(d => Raids.CrystalToday(crystal, (uint)d, 1, 0).Lit);
        uint dark = (uint)Enumerable.Range(0, 60).First(d => !Raids.CrystalToday(crystal, (uint)d, 1, 0).Lit);
        Assert.True(Raids.CrystalStands(crystal, lit, 1, 0, felled: false));
        Assert.False(Raids.CrystalStands(crystal, lit, 1, 0, felled: true));
        Assert.False(Raids.CrystalStands(crystal, dark, 1, 0, felled: false));

        // The glow: a crystal at the foot in its type's colour, under the den's beam's height, the same twice
        var beams = new[] { new PokemonPlatinumEngine.Graphics.OpenWorld.DenBeam(new System.Numerics.Vector3(10, 2, -40), 3, TeraType.Of(PokemonType.Fire)) };
        var a = new List<PokemonPlatinumEngine.Graphics.OpenWorld.WeatherPiece>();
        var b = new List<PokemonPlatinumEngine.Graphics.OpenWorld.WeatherPiece>();
        PokemonPlatinumEngine.Graphics.OpenWorld.DenBeams.Pieces(beams, new System.Numerics.Vector3(0, 10, 0), 3.5, a);
        PokemonPlatinumEngine.Graphics.OpenWorld.DenBeams.Pieces(beams, new System.Numerics.Vector3(0, 10, 0), 3.5, b);
        Assert.Equal(a, b);
        Assert.NotEmpty(a);
        Assert.All(a, p => Assert.InRange(p.Head.Y, 2f, 2f + PokemonPlatinumEngine.Graphics.OpenWorld.DenBeams.CrystalGlow + 0.01f));
        var fire = PokemonPlatinumEngine.Graphics.OpenWorld.DenBeams.CrystalColour(TeraType.Of(PokemonType.Fire));
        Assert.Contains(a, p => p.Colour.R == fire.R && p.Colour.G == fire.G && p.Colour.B == fire.B);
    }
}
