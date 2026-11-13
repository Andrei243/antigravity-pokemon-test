using System;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Core.Autoplay;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Delve;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The delve on screen (plan 44 · SD8): a floor laid in a game with no window, the leader drawn in the player's place as
/// the <c>Driver</c> tells it, a turn played to its end before the next is read by the engine's own frames, the moves'
/// reach as the screen hands it to the rules, the bag, leaving, and the key fuzzer mashing a floor with nothing thrown
/// and nothing loaded onto the graphics card.
/// </summary>
[Collection("Shared clocks and dice")]
public class DelveScreenTests
{
    private static Pokemon Mon(string species, int level = 50) => new(PokemonDatabase.Get(species)!, level, new Random(1));

    // A game with no window, past the opening, standing at the camp with two who may go down
    private static void Headless(Action<GameEngine> play)
    {
        string folder = Path.Combine(Path.GetTempPath(), "delvescreen-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(31);
            var engine = new GameEngine(new GameSettings { Muted = true });
            engine.InitializeHeadless();
            engine.PeopleStayPut = true;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            var game = engine.Drive;
            StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
            game.Party.Clear();
            game.Party.Add(Mon("Garchomp", 60));
            game.Party.Add(Mon("Lucario", 60));
            game.Place(MapDatabase.Get("Sinnoh"), 116, 886, Direction.Down);
            game.TrainersLookOnArrival = false;
            game.State = GameState.Overworld;
            play(engine);
        }
        finally
        {
            InputManager.Feed(null);
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            PlayerIdentity.InRegion(RegionDatabase.First);
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }

    private static void Frames(GameEngine engine, int n)
    {
        for (int i = 0; i < n; i++) engine.Update(1f / 60f);
    }

    private static void UntilOnTheFloor(GameEngine engine)
    {
        var game = engine.Drive;
        for (int i = 0; i < 600 && !(game.State == GameState.Delve && game.DelveMap != null && game.Map == game.DelveMap); i++) engine.Update(1f / 60f);
        Assert.Equal(GameState.Delve, game.State);
        Assert.Same(game.DelveMap, game.Map);
    }

    [Fact]
    public void TheFloorIsLaidWithNoWindowAndTheLeaderStandsInThePlayersPlace()
    {
        Headless(engine =>
        {
            var game = engine.Drive;
            var camp = (game.Map.Name, game.Player.GridX, game.Player.GridY);
            Assert.Null(game.Avatar);
            Assert.True(game.OpenDelve("test_hollow", 21));
            UntilOnTheFloor(engine);
            var run = game.Delve!;
            // The leader is drawn where the player stands, as the Driver tells it, and stands where the rules have it
            Assert.Equal(run.Leader.Pokemon.ModelName, game.Avatar);
            Assert.Equal((run.Leader.X, run.Leader.Y), (game.Player.GridX, game.Player.GridY));
            Assert.Equal(DelveFloor.MapName, game.Map.Name);
            // The partner stands as a Pokémon of the map on its own tile
            var partner = run.Team[1];
            Assert.Contains(game.Map.NPCs, n => n.NpcType == NPC.PokemonType && n.GridX == partner.X && n.GridY == partner.Y && n.Species == partner.Pokemon.ModelName);

            // Leaving: an Escape Rope's end, every line shown, the camp back as it was and the leader let go
            game.DelveAct(DelveAction.Wait);
            run.Escape();
            game.DelveScreen.Hurry();
            for (int i = 0; i < 600 && game.State != GameState.Overworld; i++)
            {
                game.DelveScreen.Hurry();
                engine.Update(1f / 60f);
            }
            Frames(engine, 30);
            Assert.Equal(GameState.Overworld, game.State);
            Assert.Null(game.Avatar);
            Assert.Null(game.Delve);
            Assert.Equal(camp, (game.Map.Name, game.Player.GridX, game.Player.GridY));
        });
    }

    [Fact]
    public void ATurnIsPlayedToItsEndByTheEnginesFramesBeforeTheNextIsRead()
    {
        Headless(engine =>
        {
            var game = engine.Drive;
            Assert.True(game.OpenDelve("test_hollow", 21));
            UntilOnTheFloor(engine);
            var run = game.Delve!;
            var screen = game.DelveScreen;
            // A strike the way faced: a turn whose lines take frames to show
            InputManager.Feed(ActionFrame.None.Next(new[] { GameAction.Confirm }));
            engine.Update(1f / 60f);
            InputManager.Feed(ActionFrame.None);
            int turn = run.Turn;
            Assert.True(turn > 0);
            Assert.True(screen.Playing);
            // While its lines play, a way held does nothing: no turn is read
            InputManager.Feed(ActionFrame.None.Next(new[] { GameAction.Up }));
            for (int i = 0; i < 4 && screen.Playing; i++) engine.Update(1f / 60f);
            Assert.Equal(turn, run.Turn);
            InputManager.Feed(ActionFrame.None);
            for (int i = 0; i < 600 && screen.Playing; i++) engine.Update(1f / 60f);
            Assert.False(screen.Playing);
            Assert.Equal(turn, run.Turn);
        });
    }

    [Fact]
    public void AMoveIsHandedToTheRulesTheWayFacedAndReachesAsItsTargetSays()
    {
        // The screen's move list hands the move and the way faced; the rules reach by the move's target
        var run = DelveRun.Begin(DelveHollow.Test, Mon("Garchomp"), Mon("Lucario"), 21);
        var screen = new DelveScreen();
        screen.Open(run);
        run.Face(Direction.Left);
        screen.OpenMoves();
        Assert.Equal(DelveMenu.Moves, screen.Menu);
        for (int i = 0; i < run.Leader.Pokemon.Moves.Count; i++)
        {
            Assert.Equal(i, screen.Cursor);
            screen.Steer(Direction.Down);
        }
        // The list wraps round
        Assert.Equal(0, screen.Cursor);
        screen.Confirm();
        var a = screen.TakeAction()!.Value;
        Assert.Equal(DelveActionKind.Move, a.Kind);
        Assert.Equal(Direction.Left, a.Way);
        Assert.Equal(0, a.Move);
        // Each row says where its move reaches, or that it needs a battle, as the rules have it
        for (int i = 0; i < run.Leader.Pokemon.Moves.Count; i++)
        {
            var move = run.Leader.Pokemon.Moves[i];
            string row = DelveScreen.MoveRow(move);
            bool works = DelveMoves.WorksOnAFloor(move.Data);
            Assert.Equal(works, DelveMoves.WhyNot(run.Leader, i) == null);
            Assert.EndsWith(Text.Of($"delve.reach.{(works ? DelveMoves.ReachOf(move.Data) : DelveReach.None)}"), row);
            Assert.DoesNotContain("[delve.", row);
        }
        // A move at one Pokémon reaches the tile ahead, one at every foe the tiles round, one at everyone the room
        Assert.Equal(DelveReach.Ahead, DelveMoves.ReachOf(MoveDatabase.Get("Tackle")!));
        Assert.Equal(DelveReach.Round, DelveMoves.ReachOf(MoveDatabase.Get("Growl")!));
        Assert.Equal(DelveReach.Room, DelveMoves.ReachOf(MoveDatabase.Get("Earthquake")!));
        // A refused move handed over is no turn
        int turn = run.Turn;
        var whirl = run.Leader.Pokemon.Moves.FindIndex(m => !DelveMoves.WorksOnAFloor(m.Data));
        if (whirl >= 0)
        {
            Assert.NotNull(screen.Act(DelveAction.UseMove(whirl, Direction.Left)));
            Assert.Equal(turn, run.Turn);
        }
    }

    [Fact]
    public void TheBagGivesAnItemAndAnEmptyBagGivesNothing()
    {
        var run = DelveRun.Begin(DelveHollow.Test, Mon("Garchomp"), Mon("Lucario"), 21);
        var screen = new DelveScreen();
        screen.Open(run);
        // Nothing in the bag: confirm takes nothing
        run.Bag.Clear();
        screen.OpenBag();
        screen.Confirm();
        Assert.Null(screen.TakeAction());
        Assert.Equal(DelveMenu.Bag, screen.Menu);
        screen.Cancel();
        // A berry to the partner, hurt and hungry: the item and whom, then the floor again once the rules take it
        run.Team[1].Pokemon.CurrentHP = 1;
        run.Hunger = 10;
        run.Bag.Add("Oran Berry");
        screen.OpenBag();
        screen.Confirm();
        screen.Steer(Direction.Down);
        screen.Confirm();
        var give = screen.TakeAction()!.Value;
        Assert.Equal(DelveActionKind.Item, give.Kind);
        Assert.Equal(("Oran Berry", 1), (give.Item, give.On));
        Assert.Null(screen.Act(give));
        Assert.Equal(DelveMenu.Field, screen.Menu);
        Assert.DoesNotContain("Oran Berry", run.Bag);
    }

    [Fact]
    public void TheKeyFuzzerMashesAFloorWithNoWindowAndLoadsNothingOntoTheCard()
    {
        Headless(engine =>
        {
            var game = engine.Drive;
            Assert.True(game.OpenDelve("brinecave", 7));
            UntilOnTheFloor(engine);
            int held = GpuLedger.Game.Snapshot().Total;
            var fuzzer = new Autoplayer(engine, seed: 17, keys: true);
            fuzzer.Play(40);
            fuzzer.Release();
            Assert.Empty(fuzzer.Report.Exceptions);
            Assert.Empty(fuzzer.Report.OutOfRange);
            Assert.Empty(fuzzer.Report.ScriptsGoneWrong);
            Assert.Equal(held, GpuLedger.Game.Snapshot().Total);
            // Wherever the mashing left it, the run is the floor's or over and the camp's
            Assert.True(game.State is GameState.Delve or GameState.Overworld or GameState.Dialogue or GameState.Transition
                || game.Delve == null, game.State.ToString());
        });
    }
}
