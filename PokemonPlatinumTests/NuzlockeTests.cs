using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 13 · V4: the Nuzlocke. One Pokémon may be caught in each place, the first met there (the dupes and shiny clauses
/// let some pass); a Pokémon that faints goes into the run's graveyard after the battle, out of the team for good; no
/// revive is used; a whiteout ends the run and its save stays, to be loaded and never written again. Under a Normal run
/// nothing of it is written or refused. The games played whole share the dice and the save folder, so the class runs alone.
/// </summary>
[Collection("Shared clocks and dice")]
public class NuzlockeTests
{
    private const float Frame = 1f / 60f;

    // ---- the places met

    [Fact]
    public void TheFirstEncounterOfAPlaceMayBeCaughtAndTheSecondMayNot()
    {
        var state = new RunState();
        var dex = new Pokedex();
        var first = Mon("Bidoof", 3, "Tackle");
        var second = Mon("Starly", 3, "Tackle");

        Assert.Equal(NuzlockeCatch.Free, Nuzlocke.Meet(RunRules.NuzlockePreset, state, "route_201", first, dex));
        Assert.Contains("route_201", state.AreasMet);
        Assert.Equal(NuzlockeCatch.AreaSpent, Nuzlocke.Meet(RunRules.NuzlockePreset, state, "route_201", second, dex));
        // Another place is a chance of its own, each of the Great Marsh's areas too
        Assert.Equal(NuzlockeCatch.Free, Nuzlocke.Meet(RunRules.NuzlockePreset, state, "great_marsh_1", second, dex));
        Assert.Equal(NuzlockeCatch.Free, Nuzlocke.Meet(RunRules.NuzlockePreset, state, "great_marsh_2", Mon("Wooper", 25, "Tackle"), dex));
    }

    [Fact]
    public void ALineAlreadyCaughtPassesWithoutSpendingThePlace()
    {
        var state = new RunState();
        var dex = new Pokedex();
        dex.RegisterCaught(PokemonDatabase.Get("Turtwig")!.DexNumber);
        // Grotle is of Turtwig's line: it can't be caught, and the place's chance is kept for the next
        Assert.Equal(NuzlockeCatch.Duplicate, Nuzlocke.Meet(RunRules.NuzlockePreset, state, "route_204_south", Mon("Grotle", 18, "Tackle"), dex));
        Assert.Empty(state.AreasMet);
        Assert.Equal(NuzlockeCatch.Free, Nuzlocke.Meet(RunRules.NuzlockePreset, state, "route_204_south", Mon("Budew", 5, "Tackle"), dex));
        // Without the clause the line is no different from any other
        var plain = RunRules.NuzlockePreset with { DupesClause = false };
        Assert.Equal(NuzlockeCatch.Free, Nuzlocke.Meet(plain, new RunState(), "route_204_south", Mon("Grotle", 18, "Tackle"), dex));
    }

    [Fact]
    public void AShinyMayAlwaysBeCaughtUnderTheShinyClause()
    {
        var state = new RunState();
        state.Meet("route_202");
        var shiny = Mon("Shinx", 4, "Tackle");
        shiny.IsShiny = true;
        Assert.Equal(NuzlockeCatch.Free, Nuzlocke.Meet(RunRules.NuzlockePreset, state, "route_202", shiny, new Pokedex()));
        Assert.Equal(NuzlockeCatch.AreaSpent, Nuzlocke.Meet(RunRules.NuzlockePreset with { ShinyClause = false }, state, "route_202", shiny, new Pokedex()));
    }

    [Fact]
    public void ANormalRunWritesAndRefusesNothing()
    {
        var state = new RunState();
        for (int i = 0; i < 3; i++)
            Assert.Equal(NuzlockeCatch.Free, Nuzlocke.Meet(RunRules.Normal, state, "route_201", Mon("Bidoof", 3, "Tackle"), new Pokedex()));
        Assert.True(state.IsEmpty);
    }

    // ---- the battle's own refusals

    private static BattleCore Met(NuzlockeTerms? terms, params Pokemon[] bench)
    {
        var party = new Party();
        party.Add(Mon("Turtwig", 10, "Tackle"));
        foreach (var p in bench) party.Add(p);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { Mon("Bidoof", 3, "Tackle") }, Random = Calm(), Rules = Ruleset.Platinum,
            PlayerController = null, EnemyController = null, PlayerName = "Lucas", Nuzlocke = terms
        });
        core.Start();
        core.TakeLog();
        return core;
    }

    [Fact]
    public void ABallIsRefusedWhereThePlaceIsSpentAndThrownWhereItIsNot()
    {
        var ball = BattleChoice.UseItem(Mine, "Poké Ball");
        Assert.Null(Met(new NuzlockeTerms(NuzlockeCatch.Free)).WhyNot(ball));
        Assert.Equal(Text.Of("battle.nuzlockeAreaSpent"), Met(new NuzlockeTerms(NuzlockeCatch.AreaSpent)).WhyNot(ball));
        Assert.Equal(Text.Of("battle.nuzlockeDuplicate"), Met(new NuzlockeTerms(NuzlockeCatch.Duplicate)).WhyNot(BattleChoice.UseItem(Mine, "Great Ball")));
        Assert.Null(Met(null).WhyNot(ball));
    }

    [Fact]
    public void NoReviveIsUsedInABattle()
    {
        var fallen = Mon("Starly", 8, "Tackle");
        fallen.CurrentHP = 0;
        var revive = BattleChoice.UseItem(Mine, "Revive", 1);
        Assert.Equal(Text.Of("battle.nuzlockeNoRevive"), Met(new NuzlockeTerms(), fallen).WhyNot(revive));
        // Under any other run it brings the Starly round, as ever
        var again = Mon("Starly", 8, "Tackle");
        again.CurrentHP = 0;
        Assert.Null(Met(null, again).WhyNot(revive));
        // Other medicine is the Nuzlocke's as anyone's
        var hurt = Mon("Starly", 8, "Tackle");
        hurt.CurrentHP = 3;
        Assert.Null(Met(new NuzlockeTerms(), hurt).WhyNot(BattleChoice.UseItem(Mine, "Potion", 1)));
        // Nor in the field: the bag refuses it and keeps it
        var bag = new PokemonPlatinumEngine.UI.BagScreen { NoRevives = true };
        var inventory = new Inventory();
        var reviveItem = ItemDatabase.Get("Revive")!;
        inventory.AddItem(reviveItem, 1);
        var team = new Party();
        var down = Mon("Starly", 8, "Tackle");
        down.CurrentHP = 0;
        team.Add(down);
        var notes = new List<string>();
        bag.BeginTargetChoice(reviveItem, give: false);
        bag.UseOnTarget(inventory, team, notes.Add, new EvolutionContext { Party = team, Bag = inventory });
        Assert.Equal(Text.Of("bag.nuzlockeNoRevive"), Assert.Single(notes));
        Assert.Equal(1, inventory.GetQuantity(reviveItem));
        Assert.True(down.IsFainted);
        Assert.True(Nuzlocke.Revives(ItemDatabase.Get("Sacred Ash")!));
        Assert.True(Nuzlocke.Revives(ItemDatabase.Get("Revival Herb")!));
        Assert.False(Nuzlocke.Revives(ItemDatabase.Get("Potion")!));
    }

    [Fact]
    public void TheFallenAreBuriedAndAnEggIsNot()
    {
        var party = new Party();
        var alive = Mon("Turtwig", 10, "Tackle");
        var gone = Mon("Starly", 8, "Tackle");
        gone.CurrentHP = 0;
        gone.Nickname = "Wings";
        party.Add(alive);
        party.Add(gone);
        var egg = new Pokemon(PokemonDatabase.Get("Togepi")!, 1) { IsEgg = true };
        party.Add(egg);
        var state = new RunState();

        var fallen = Nuzlocke.Bury(state, party, "route_202", "Route 202", 300);
        Assert.Equal(new[] { gone }, fallen);
        Assert.Equal(new[] { alive, egg }, party.Members);
        var grave = Assert.Single(state.Graveyard);
        Assert.Equal("Starly", grave.Pokemon.SpeciesName);
        Assert.Equal("Wings", grave.Pokemon.Nickname);
        Assert.Equal("route_202", grave.PlaceKey);
        Assert.False(state.IsEmpty);
    }

    // ---- whole games, with no window

    [Fact]
    public void AFaintedMemberIsInTheGraveyardAfterTheBattleAndNoReviveReachesIt()
    {
        var paths = SavePaths.Current;
        string folder = Path.Combine(Path.GetTempPath(), "nuzlocke-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(12);
            var (engine, game) = NewRun();
            var gone = new Pokemon(PokemonDatabase.Get("Starly")!, 8) { CurrentHP = 0 };
            var lead = new Pokemon(PokemonDatabase.Get("Empoleon")!, 60);
            game.Party.Add(lead);
            game.Party.Add(gone);
            game.Bag.AddItem(ItemDatabase.Get("Revive")!, 3);

            Fight(engine, game, "Bidoof", 2, win: true);

            Assert.DoesNotContain(gone, game.Party.Members);
            Assert.Contains(lead, game.Party.Members);
            var grave = Assert.Single(game.RunState.Graveyard);
            Assert.Equal("Starly", grave.Pokemon.SpeciesName);
            // The place of the battle is spent, and its first was the Bidoof
            Assert.Single(game.RunState.AreasMet);
            // It is in no box
            Assert.DoesNotContain(game.Boxes.All, p => p == gone);
            // The run goes on: the save is written, and holds the graveyard
            Assert.True(game.Save());
            var again = Headless();
            again.ContinueGame(game.SaveSlot);
            Until(again, () => again.Drive.State == GameState.Overworld, "the continued game");
            Assert.Single(again.Drive.RunState.Graveyard);
            Assert.False(again.Drive.RunState.Ended);
        }
        finally
        {
            TearDown(paths, folder);
        }
    }

    [Fact]
    public void AWhiteoutEndsTheRunAndTheSaveStillLoads()
    {
        var paths = SavePaths.Current;
        string folder = Path.Combine(Path.GetTempPath(), "nuzlocke-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(12);
            var (engine, game) = NewRun();
            var lead = new Pokemon(PokemonDatabase.Get("Turtwig")!, 5) { CurrentHP = 1 };
            game.Party.Add(lead);

            Fight(engine, game, "Onix", 60, win: false);
            Until(engine, () => game.State == GameState.Overworld && !engine.ScriptRunning, "the field after the whiteout", press: true);

            Assert.True(game.RunState.Ended);
            Assert.Equal("whiteout", game.RunState.EndedBy);
            Assert.Empty(game.Party.Members);
            Assert.Equal("Turtwig", Assert.Single(game.RunState.Graveyard).Pokemon.SpeciesName);
            // The end was written; nothing writes the slot again
            string file = Path.Combine(folder, SavePaths.SlotName(game.SaveSlot));
            Assert.True(File.Exists(file));
            var written = File.GetLastWriteTimeUtc(file);
            Assert.False(game.Save());
            Assert.Equal(written, File.GetLastWriteTimeUtc(file));

            // And the save loads, to be looked at: the run over, its graveyard, its card
            var again = Headless();
            again.ContinueGame(game.SaveSlot);
            Until(again, () => again.Drive.State == GameState.Overworld, "the continued game");
            Assert.True(again.Drive.RunState.Ended);
            Assert.Single(again.Drive.RunState.Graveyard);
            Assert.True(again.Drive.Run.Nuzlocke);
            Assert.False(again.Drive.Save());
        }
        finally
        {
            TearDown(paths, folder);
        }
    }

    [Fact]
    public void ACatchIsNamedOnceTheFieldIsBack()
    {
        var paths = SavePaths.Current;
        try
        {
            Dice.Seed(12);
            var (engine, game) = NewRun();
            var caught = new Pokemon(PokemonDatabase.Get("Starly")!, 4);
            game.Party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 10));
            game.Party.Add(caught);
            game.NameAfterBattle = caught;
            Run(engine, ActionFrame.None);
            Assert.Equal(GameState.Nickname, game.State);
            Assert.Same(caught, game.Nickname.Target);
            // Left with B on an empty name, it keeps its species' name and the field comes back, not the party menu
            game.Nickname.Cancel();
            Run(engine, ActionFrame.None);
            Assert.Equal(GameState.Overworld, game.State);
            Assert.Equal("", caught.Nickname);
            Assert.Null(game.NameAfterBattle);
        }
        finally
        {
            TearDown(paths, null);
        }
    }

    // ---- helpers

    private static (GameEngine Engine, GameEngine.Driver Game) NewRun()
    {
        var engine = Headless();
        var game = engine.Drive;
        engine.NewGameRegion = "Sinnoh";
        engine.StartNewGame(null, PlayerLook.Boy, null, RunRules.NuzlockePreset);
        StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
        game.Party.Clear();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.TrainersLookOnArrival = false;
        var sinnoh = MapDatabase.Get("Sinnoh");
        var (gx, gy) = Grass(sinnoh, 116, 886);
        game.Place(sinnoh, gx, gy, Direction.Right);
        return (engine, game);
    }

    // A wild battle in the grass where the player stands, fought with the lead's first damaging move to its end (or
    // lost), and the field brought back
    private static void Fight(GameEngine engine, GameEngine.Driver game, string species, int level, bool win)
    {
        game.StartWildBattle(new WildEncounterEntry { SpeciesName = species, MinLevel = level, MaxLevel = level, Weight = 100 });
        Until(engine, () => game.State == GameState.Battle, "the battle");
        var battle = game.Battle!;
        Until(engine, () => battle.IsBattleOver || battle.HUD.MenuState == BattleMenuState.Main, "the battle's menu", press: true);
        for (int guard = 0; guard < 60 * 240 && !battle.IsBattleOver; guard++)
        {
            if (battle.HUD.MenuState == BattleMenuState.Main)
            {
                var lead = battle.PlayerPokemon;
                battle.SelectMove(Math.Max(0, lead.Moves.FindIndex(m => m.Data.Category != MoveCategory.Status)));
            }
            Run(engine, guard % 8 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
        }
        Assert.True(battle.IsBattleOver, $"The battle with {species} never ended.");
        Assert.Equal(win ? BattleResult.PlayerVictory : BattleResult.PlayerDefeat, battle.Result);
        Until(engine, () => game.State is GameState.Overworld or GameState.Dialogue, "the field after the battle", press: true);
    }

    private static void TearDown(SavePaths paths, string? folder)
    {
        InputManager.Feed(null);
        Dice.Seed(null);
        Ruleset.Use(RulesPreset.Platinum);
        RunRules.Current = RunRules.Normal;
        SavePaths.Current = paths;
        if (folder != null) try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
    }

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
        engine.Update(Frame);
    }

    private static void Until(GameEngine engine, Func<bool> done, string what, bool press = false, int most = 60 * 120)
    {
        for (int f = 0; f < most; f++)
        {
            if (done()) return;
            Run(engine, press && f % 8 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
        }
        Assert.Fail($"Waited {most / 60} seconds of game time for {what}; the game is in {engine.Drive.State}.");
    }

    private static (int X, int Y) Grass(Map map, int x, int y)
    {
        for (int r = 0; r < 80; r++)
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) == r && map.IsTallGrass(x + dx, y + dy) && map.IsWalkable(x + dx, y + dy))
                        return (x + dx, y + dy);
        throw new InvalidOperationException("No tall grass near Twinleaf Town.");
    }
}
