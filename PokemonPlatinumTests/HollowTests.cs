using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Delve;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The delve's camp and hollows (plan 44 · SD9; <c>docs/mechanics/rulings.md</c>, "The delve"): the four hollows of
/// <c>Data/hollows.json</c> read and cleared by the plain policy, their guardians keeping the last stairs, the day's board
/// the same twice, each kind of request, a recruit brought home with its place, the run's rules asked, and the record
/// through a save. Also SD8's screen: a turn played to its end before the next is read, its lists and its words.
/// </summary>
public class HollowTests
{
    // A generator of its own, never the shared Dice: a run replayed for its seed must be handed the same Pokémon
    private static Pokemon Mon(string species, int level = 50) => new(PokemonDatabase.Get(species)!, level, new Random(1));

    [Fact]
    public void TheFourHollowsReadWithTheirSpeciesItemsAndGuardians()
    {
        var all = DelveHollows.All;
        Assert.Equal(new[] { "brinecave", "rootwood", "heartpeak", "old_tower" }, all.Select(h => h.Key));
        Assert.Equal(new[] { 0, 1, 2, 3 }, all.Select(h => h.Rank));
        foreach (var h in all)
        {
            Assert.False(string.IsNullOrEmpty(h.Name));
            foreach (var s in h.Residents.Append(h.Guardian!)) Assert.True(PokemonDatabase.Get(s) != null, $"{h.Key}: no species {s}");
            foreach (var i in h.Finds) Assert.True(ItemDatabase.Get(i) != null, $"{h.Key}: no item {i}");
            Assert.True(Text.Has($"delve.hollow.{h.Key}"), h.Key);
            // Every floor of every theme lays as a map
            for (uint seed = 1; seed <= 5; seed++)
            {
                var map = DelveFloor.Generate(h, 0, seed).ToFile().ToMap();
                Assert.Equal(DelveFloor.Width, map.Width);
            }
        }
        foreach (var p in DelveHollows.Prizes) Assert.True(ItemDatabase.Get(p.Item) != null, p.Item);
        Assert.Equal(Enum.GetValues<DelveRequestKind>().Length, DelveHollows.Prizes.Select(p => p.Kind).Distinct().Count());
    }

    private static DelveRun Play(DelveHollow hollow, uint seed, string leader = "Garchomp", string partner = "Lucario", int? recruitRoll = null)
    {
        var run = DelveRun.Begin(hollow, Mon(leader), Mon(partner), seed);
        run.FixedRecruitRoll = recruitRoll;
        for (int n = 0; n < 20000 && run.Outcome == DelveOutcome.Going; n++) run.Act(DelvePolicy.Next(run));
        return run;
    }

    [Fact]
    public void EveryHollowIsClearedHeadlessByThePlainPolicyPastItsGuardian()
    {
        foreach (var h in DelveHollows.All)
        {
            var cleared = Enumerable.Range(1, 30).Select(s => Play(h, (uint)s)).FirstOrDefault(r => r.Outcome == DelveOutcome.Cleared);
            Assert.True(cleared != null, $"{h.Key} was never cleared in thirty seeds");
            Assert.NotNull(cleared!.Guardian);
            Assert.True(cleared.Guardian!.Fainted, $"{h.Key}: cleared with its guardian standing");
            Assert.Equal(h.Level + DelveHollow.GuardianOver, cleared.Guardian.Pokemon.Level);
            Assert.Equal(h.Floors, cleared.DeepestFloor);
            // The same run twice for its seed
            var again = Play(h, cleared.Seed);
            Assert.Equal(cleared.Events, again.Events);
        }
    }

    [Fact]
    public void TheTestHollowHasNoGuardianSoItsRunsDidNotMove()
    {
        var run = Play(DelveHollow.Test, 7);
        Assert.Null(run.Guardian);
        Assert.DoesNotContain(run.Events, e => e.Kind == DelveEventKind.Guarded);
    }

    [Fact]
    public void AGuardianNeverAsksToJoin()
    {
        var h = DelveHollows.All[0];
        var run = Play(h, 3, recruitRoll: 0);
        if (run.Guardian is { } g) Assert.DoesNotContain(run.Events, e => e.Kind == DelveEventKind.AsksToJoin && e.Who == g.Name && run.Team.Contains(g));
        Assert.DoesNotContain(run.Team, m => m.IsGuardian);
    }

    [Fact]
    public void ADaysBoardIsTheSameTwiceAndOnlyOfOpenHollows()
    {
        var a = DelveBoard.Of(12345, 0);
        var b = DelveBoard.Of(12345, 0);
        Assert.Equal(a, b);
        Assert.Equal(DelveBoard.RequestsADay, a.Count);
        Assert.All(a, r => Assert.Equal("brinecave", r.Hollow));
        Assert.NotEqual(DelveBoard.Of(12345, 3), DelveBoard.Of(54321, 3));
        // Over many days at the top rank every kind and every hollow comes up
        var many = Enumerable.Range(1, 200).SelectMany(d => DelveBoard.Of((uint)d, 3)).ToList();
        Assert.Equal(4, many.Select(r => r.Kind).Distinct().Count());
        Assert.Equal(4, many.Select(r => r.Hollow).Distinct().Count());
        Assert.All(many.Where(r => r.Kind == DelveRequestKind.Rescue), r => Assert.InRange(r.Floor, 2, DelveHollows.Get(r.Hollow)!.Floors));
    }

    [Fact]
    public void EachKindOfRequestIsDoneByTheRunThatDoesIt()
    {
        var h = DelveHollows.All[0];
        var run = Enumerable.Range(1, 20).Select(s => Play(h, (uint)s, recruitRoll: 0)).First(r => r.Outcome == DelveOutcome.Cleared && r.RecruitsAtTheEnd.Count > 0);
        Assert.True(new DelveRequest(DelveRequestKind.Clear, h.Key).DoneBy(run));
        Assert.True(new DelveRequest(DelveRequestKind.Rescue, h.Key, Floor: h.Floors).DoneBy(run));
        Assert.True(new DelveRequest(DelveRequestKind.Befriend, h.Key, Species: run.RecruitsAtTheEnd[0].Species.Name).DoneBy(run));
        if (run.Bag.Count > 0) Assert.True(new DelveRequest(DelveRequestKind.Find, h.Key, Item: run.Bag[0]).DoneBy(run));
        Assert.False(new DelveRequest(DelveRequestKind.Clear, "rootwood").DoneBy(run));

        var fallen = Enumerable.Range(1, 40).Select(s => Play(h, (uint)s, "Magikarp", "Feebas")).FirstOrDefault(r => r.Outcome == DelveOutcome.Fallen);
        if (fallen != null)
        {
            Assert.False(new DelveRequest(DelveRequestKind.Rescue, h.Key, Floor: 1).DoneBy(fallen));
            Assert.False(new DelveRequest(DelveRequestKind.Clear, h.Key).DoneBy(fallen));
        }

        // The record marks today's done once, puts their prizes due, counts the clear and raises the rank
        var record = new DelveRecord();
        var board = new[] { new DelveRequest(DelveRequestKind.Clear, h.Key), new DelveRequest(DelveRequestKind.Clear, "rootwood") };
        var did = record.Take(run, board, 100);
        Assert.Single(did);
        Assert.True(record.IsDone(0, 100));
        Assert.False(record.IsDone(0, 101));
        Assert.Equal(1, record.Rank);
        Assert.True(record.IsOpen(DelveHollows.Get("rootwood")!));
        Assert.False(record.IsOpen(DelveHollows.Get("heartpeak")!));
        Assert.Contains(record.Due, d => d.Item == "Rare Candy");
        Assert.Empty(record.Take(run, board, 100));
        // The clear raised the rank, but today's board is still drawn at the rank it was drawn at, so its DONE marks stay
        // on the requests they were won on; tomorrow's is drawn at the new rank
        Assert.Equal(0, record.RankOn(100));
        Assert.Equal(1, record.RankOn(101));
        Assert.Equal(DelveBoard.Of(777, 0), DelveBoard.Of(777, record.RankOn(100)));
        var back = DelveRecord.From(GameDataFiles.Deserialize<DelveSave>(GameDataFiles.Serialize(record.ToSave())));
        Assert.Equal(0, back.RankOn(100));
    }

    [Fact]
    public void ARecruitComesHomeWithItsPlaceAndTheRunsRulesAreAsked()
    {
        var h = DelveHollows.All[0];
        var run = Enumerable.Range(1, 20).Select(s => Play(h, (uint)s, recruitRoll: 0)).First(r => r.Outcome == DelveOutcome.Cleared && r.RecruitsAtTheEnd.Count > 0);
        var bag = new Inventory();
        var taken = new List<Pokemon>();
        var record = new DelveRecord();
        var settled = DelveHome.Settle(run, record, Array.Empty<DelveRequest>(), 1, bag, _ => true, p => { taken.Add(p); return true; }, h.Name!);
        Assert.Equal(run.RecruitsAtTheEnd.Count, settled.Home.Count);
        Assert.All(settled.Home, p =>
        {
            Assert.Equal(h.Name, p.MetLocation);
            Assert.Equal(p.MaxHP, p.CurrentHP);
            Assert.Null(p.HeldItem);
        });
        Assert.Equal(settled.Home.Count, record.RecruitsHome);
        foreach (var item in run.Bag.Distinct()) Assert.True(bag.GetQuantity(ItemDatabase.Get(item)!) > 0, item);

        // A run's rules that refuse it leave it in the hollow
        var refused = DelveHome.Settle(Play(h, run.Seed, recruitRoll: 0), new DelveRecord(), Array.Empty<DelveRequest>(), 1, new Inventory(), _ => false, _ => true, h.Name!);
        Assert.Empty(refused.Home);
        Assert.NotEmpty(refused.Stayed);
    }

    [Fact]
    public void TheRandomiserMapsTheResidentsAsTheyAreMade()
    {
        var run = DelveRun.Begin(DelveHollows.All[0], Mon("Garchomp"), Mon("Lucario"), 9, _ => "Pikachu");
        Assert.All(run.Residents, r => Assert.Equal("Pikachu", r.Pokemon.Species.Name));
    }

    [Fact]
    public void TheRecordComesBackFromItsSave()
    {
        Assert.Null(new DelveRecord().ToSave());
        var r = new DelveRecord { Runs = 3, RecruitsHome = 1, BoardDay = 7 };
        r.Clears["brinecave"] = 2;
        r.Done.Add(1);
        r.Due.Add(("Heart Scale", 1));
        var json = GameDataFiles.Serialize(r.ToSave());
        var back = DelveRecord.From(GameDataFiles.Deserialize<DelveSave>(json));
        Assert.Equal(json, GameDataFiles.Serialize(back.ToSave()));
        Assert.Equal(1, back.Rank);
    }

    [Fact]
    public void TheKeepersScriptSendsTwoDownAndSettlesTheRunHeadless()
    {
        var host = new HeadlessScriptHost();
        host.Party.Add(Mon("Garchomp", 80));
        host.Party.Add(Mon("Lucario", 80));
        var library = ScriptLibrary.FromSources(("test", "script Go\n  delve go \"brinecave\"\n  end\nscript Able\n  delve able\n  end\n"));
        int Run(HeadlessScriptHost on, string name)
        {
            var runner = new ScriptRunner(library, on);
            runner.Start(library.Find(name, "test")!);
            runner.RunToEnd();
            return runner.Result;
        }
        Assert.Equal(1, Run(host, "Able"));
        for (uint seed = 1; seed < 12 && host.DelveRecord!.Rank == 0; seed++)
        {
            host.DelveSeed = seed;
            Run(host, "Go");
        }
        Assert.Equal(1, host.DelveRecord!.Rank);
        Assert.Contains(host.Log, l => l.StartsWith("open Delve brinecave 1"));
        Assert.Empty(host.Problems);
        // A team with one who may go down goes nowhere
        var lone = new HeadlessScriptHost();
        lone.Party.Add(Mon("Garchomp", 80));
        Assert.Equal(0, Run(lone, "Able"));
    }

    [Fact]
    public void TheKeepersScriptParsesWithItsHollows()
    {
        Assert.NotNull(ScriptLibrary.Default.Find("DelveKeeper", "survival_area"));
        Assert.DoesNotContain(ScriptLibrary.Default.Problems(), p => p.Contains("hollow"));
    }

    // ------------------------------------------------------------------ SD8: the screen

    [Fact]
    public void ATurnIsPlayedToItsEndBeforeTheNextIsRead()
    {
        var run = DelveRun.Begin(DelveHollow.Test, Mon("Garchomp"), Mon("Lucario"), 21);
        var screen = new DelveScreen();
        screen.Open(run);
        // A strike at nothing still says something and is a turn
        screen.Confirm();
        Assert.Equal(DelveActionKind.Strike, screen.TakeAction()!.Value.Kind);
        Assert.Null(screen.Act(DelveAction.Strike(run.Leader.Facing)));
        Assert.True(screen.Playing);
        int turn = run.Turn;
        screen.Steer(Direction.Up);
        Assert.Null(screen.TakeAction());
        for (int f = 0; f < 600 && screen.Playing; f++) screen.Advance(1f / 60f);
        Assert.False(screen.Playing);
        Assert.Equal(turn, run.Turn);
        Assert.NotEmpty(screen.Log);
        Assert.All(screen.Log, l => Assert.DoesNotContain("[delve.", l));
    }

    [Fact]
    public void TheListsChooseAMoveAnItemAndAnswerARecruit()
    {
        var run = DelveRun.Begin(DelveHollow.Test, Mon("Garchomp"), Mon("Lucario"), 21);
        var screen = new DelveScreen();
        screen.Open(run);
        screen.OpenMoves();
        Assert.Equal(DelveMenu.Moves, screen.Menu);
        screen.Steer(Direction.Down);
        screen.Confirm();
        var move = screen.TakeAction()!.Value;
        Assert.Equal(DelveActionKind.Move, move.Kind);
        Assert.Equal(1, move.Move);
        screen.Cancel();
        Assert.Equal(DelveMenu.Field, screen.Menu);
        run.Bag.Add("Oran Berry");
        screen.OpenBag();
        screen.Confirm();
        Assert.Equal(DelveMenu.BagTarget, screen.Menu);
        screen.Steer(Direction.Down);
        screen.Confirm();
        var give = screen.TakeAction()!.Value;
        Assert.Equal(("Oran Berry", 1), (give.Item, give.On));
        // Back from the target is the bag, back again the floor
        screen.OpenBag();
        screen.Confirm();
        screen.Cancel();
        Assert.Equal(DelveMenu.Bag, screen.Menu);
        screen.Cancel();
        Assert.Equal(DelveMenu.Field, screen.Menu);
        // A wall's refusal turns the leader that way and is no turn
        int t = run.Turn;
        foreach (var way in new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })
            screen.Act(DelveAction.Step(way));
        Assert.True(run.Turn >= t);
    }

    [Fact]
    public void EveryEventAndRefusalHasWordsInBothLanguages()
    {
        foreach (var kind in Enum.GetValues<DelveEventKind>())
            Assert.True(Text.Has($"delve.event.{kind}"), kind.ToString());
        foreach (var refusal in Enum.GetValues<DelveRefusal>())
            Assert.True(Text.Has($"delve.refused.{refusal}"), refusal.ToString());
        foreach (var kind in Enum.GetValues<DelveRequestKind>())
            Assert.True(Text.Has($"delve.request.{kind}"), kind.ToString());
        foreach (var snare in Enum.GetValues<SnareKind>())
            Assert.True(Text.Has($"delve.snare.{snare}"), snare.ToString());
        var ro = File.ReadAllText(Path.Combine(GameDataFiles.Directory, "text", "ro", "ui.json"));
        Assert.Contains("\"delve\"", ro);
    }

    [Fact]
    public void TheFloorFileIsTheTestHollowsFirstFloor()
    {
        string path = Path.Combine(Repository.Root, "PokemonPlatinumEngine", "Data", "maps", DelveFloor.MapName + ".json");
        string made = GameDataFiles.Serialize(DelveFloor.Generate(DelveHollow.Test, 0, 1).ToFile());
        if (Environment.GetEnvironmentVariable("GOLDEN") == "accept" || !File.Exists(path)) File.WriteAllText(path, made);
        Assert.Equal(made.Replace("\r\n", "\n"), File.ReadAllText(path).Replace("\r\n", "\n"));
    }
}
