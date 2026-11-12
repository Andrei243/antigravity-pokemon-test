using System;
using System.IO;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// The Stroll Counter (plan 35 · BN7; docs/mechanics/rulings.md, "The Stroll Counter"): twenty steps a stride, the pay at a
/// PC a level at most, a seek and a meet the same for a seed, a meet refused in a run that forbids catching, every Pokémon
/// met of its course's region's own grass, and nothing in two places.
/// </summary>
[Collection("Shared clocks and dice")]
public class StrollCounterTests
{
    private static Pokemon Bidoof(int level = 10) => new(PokemonDatabase.Get("Bidoof")!, level, new Random(3));

    [Fact]
    public void TwentyStepsAreAStrideAndASparkOnlyWithAPokemonIn()
    {
        var save = new StrollSave();
        for (int i = 0; i < 100; i++) StrollCounter.Step(save);
        Assert.Equal(0, save.StridesEver);
        Assert.True(StrollCounter.Put(save, Bidoof(), "sinnoh_meadow"));
        int strides = 0;
        for (int i = 0; i < 2000; i++) if (StrollCounter.Step(save)) strides++;
        Assert.Equal(100, strides);
        Assert.Equal(100, StrollCounter.Strides(save));
        Assert.Equal(100, save.Sparks);
        Assert.Equal(100, save.StridesEver);
    }

    [Fact]
    public void ThePayAtAPCIsALevelAtMostAndNeverPastTheCap()
    {
        var save = new StrollSave();
        var p = Bidoof(10);
        int friendship = p.Friendship;
        StrollCounter.Put(save, p, "sinnoh_meadow");
        for (int i = 0; i < 20 * 1000; i++) StrollCounter.Step(save);
        var paid = StrollCounter.TakeBack(save, cap: null)!;
        Assert.Equal(1000, paid.Strides);
        Assert.Equal(1, paid.LevelsGained);
        Assert.Equal(StrollCounter.MostFriendship, paid.Friendship);
        // (the level it gained gives its own friendship besides)
        Assert.True(paid.Pokemon.Friendship >= friendship + StrollCounter.MostFriendship);
        Assert.Null(save.Pokemon);

        // A run's cap at its level pays no EXP at all
        StrollCounter.Put(save, Bidoof(10), "sinnoh_meadow");
        for (int i = 0; i < 20 * 100; i++) StrollCounter.Step(save);
        var capped = StrollCounter.TakeBack(save, cap: 10)!;
        Assert.Equal(0, capped.Exp);
        Assert.Equal(10, capped.Pokemon.Level);

        // Half the strides to a level pay half a level's span
        Assert.Equal(50, StrollCounter.ExpFor(50, 100));
        Assert.Equal(100, StrollCounter.ExpFor(300, 100));
    }

    [Fact]
    public void ASeekAndAMeetAreTheSameForTheSameSave()
    {
        StrollSave Fresh()
        {
            var s = new StrollSave();
            StrollCounter.Put(s, Bidoof(), "sinnoh_meadow");
            s.Sparks = 100;
            s.Draws = 7;
            return s;
        }
        var a = Fresh();
        var b = Fresh();
        var seekA = StrollCounter.BeginSeek(a, 12345)!;
        var seekB = StrollCounter.BeginSeek(b, 12345)!;
        Assert.Equal((seekA.Hidden, seekA.Find), (seekB.Hidden, seekB.Find));
        Assert.Equal(90, a.Sparks);

        var meetA = StrollCounter.BeginMeet(a, null, 12345)!;
        var meetB = StrollCounter.BeginMeet(b, null, 12345)!;
        Assert.Equal((meetA.Species, meetA.Level), (meetB.Species, meetB.Level));
        foreach (var move in new[] { StrollMove.Strike, StrollMove.Strike, StrollMove.Catch })
        {
            meetA.Play(move);
            meetB.Play(move);
            Assert.Equal((meetA.WildHearts, meetA.OwnHearts, meetA.End), (meetB.WildHearts, meetB.OwnHearts, meetB.End));
        }
        Assert.NotEqual(StrollMeetEnd.Going, meetA.End);

        // A seek finds the item in the hidden bush, and the second try is the last
        var seek = StrollCounter.BeginSeek(Fresh(), 99)!;
        int wrong = (seek.Hidden + 1) % StrollCounter.Bushes;
        Assert.True(seek.Look(wrong));
        Assert.False(seek.Over);
        Assert.False(seek.Look(wrong));
        Assert.True(seek.Look(seek.Hidden));
        Assert.True(seek.Found && seek.Over);
    }

    [Fact]
    public void AMeetIsRefusedInARunThatForbidsCatchingAndWithoutSparks()
    {
        var save = new StrollSave();
        StrollCounter.Put(save, Bidoof(), "sinnoh_meadow");
        save.Sparks = 100;
        Assert.Equal(StrollRefusal.NoCatching, StrollCounter.WhyNotMeet(save, RunRules.Normal with { NoCatching = true }));
        Assert.Equal(StrollRefusal.NoCatching, StrollCounter.WhyNotMeet(save, RunRules.Normal with { Nuzlocke = true }));
        Assert.Equal(StrollRefusal.None, StrollCounter.WhyNotMeet(save, RunRules.Normal));
        save.Sparks = StrollCounter.SparksPerTry - 1;
        Assert.Equal(StrollRefusal.NoSparks, StrollCounter.WhyNotMeet(save, null));
        Assert.Equal(StrollRefusal.NoSparks, StrollCounter.WhyNotSeek(save));
        Assert.Null(StrollCounter.BeginMeet(save, null, 1));
        // An Egg never goes in, nor a second Pokémon
        var egg = Bidoof();
        egg.IsEgg = true;
        Assert.Equal(StrollRefusal.Egg, StrollCounter.WhyNotPut(new StrollSave(), egg));
        Assert.Equal(StrollRefusal.Occupied, StrollCounter.WhyNotPut(save, Bidoof()));
    }

    [Fact]
    public void EveryPokemonMetIsOfItsCoursesRegionsOwnGrassAndEveryFindIsAnItem()
    {
        var misses = new System.Collections.Generic.List<string>();
        foreach (var course in StrollCounter.Courses)
        {
            var region = RegionDatabase.Get(course.Region)!;
            var habitats = Habitats.Of(region)!;
            Assert.NotNull(habitats);
            foreach (string species in course.Species)
            {
                Assert.NotNull(PokemonDatabase.Get(species));
                if (!habitats.Of(species).Any(h => (h.Ways & HabitatWays.Grass) != 0))
                    misses.Add($"{species} of {course.Id}");
            }
            Assert.All(course.Finds, item => Assert.NotNull(ItemDatabase.Get(item)));
            Assert.InRange(course.MinLevel, 1, course.MaxLevel);
        }
        Assert.True(misses.Count == 0, "met in none of their region's grass: " + string.Join(", ", misses));
        Assert.Equal(StrollCounter.Courses.Count, StrollCounter.Courses.Select(c => c.Id).Distinct().Count());
    }

    [Fact]
    public void ACaughtPokemonIsMadeOnTheMeetsOwnGeneratorAndTheDiceAreUntouched()
    {
        var save = new StrollSave();
        StrollCounter.Put(save, Bidoof(), "kanto_meadow");
        save.Sparks = 500;
        Dice.Seed(42);
        int before = Dice.New().Next();
        Dice.Seed(42);
        StrollMeet? won = null;
        for (int tries = 0; tries < 40 && won == null; tries++)
        {
            var meet = StrollCounter.BeginMeet(save, null, 777)!;
            while (meet.End == StrollMeetEnd.Going) meet.Play(meet.WildHearts > 0 ? StrollMove.Strike : StrollMove.Catch);
            if (meet.End == StrollMeetEnd.Caught) won = meet;
            save.Sparks = 500;
        }
        Assert.NotNull(won);
        var caught = StrollCounter.EndMeet(save, won!, 777, level => level, null, new DateTime(2026, 11, 12))!;
        Assert.Contains(caught.Species.Name, StrollCounter.Course("kanto_meadow")!.Species);
        Assert.Equal(StrollCounter.MetPlace, caught.MetLocation);
        Assert.Single(save.Caught);
        Assert.Equal(before, Dice.New().Next());
    }

    // ------------------------------------------------------------------ in the game

    private static void Frames(GameEngine engine, int n)
    {
        for (int i = 0; i < n; i++) engine.Update(1f / 60f);
    }

    [Fact]
    public void ABoxedPokemonWalksTwoThousandStepsAMeetIsWonAndItsPokemonReachesTheBoxes()
    {
        var paths = SavePaths.Current;
        string folder = Path.Combine(Path.GetTempPath(), "stroll-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        SavePaths.Current = SavePaths.In(folder);
        Dice.Seed(7);
        var engine = new GameEngine(new GameSettings { Muted = true });
        try
        {
            engine.InitializeHeadless();
            engine.PeopleStayPut = true;
            var game = engine.Drive;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            game.Party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 10));
            var starly = new Pokemon(PokemonDatabase.Get("Starly")!, 12);
            game.Boxes.Boxes[0].Slots[4] = starly;
            game.GiveIds();
            game.Bag.AddItem(ItemDatabase.Get(StrollCounter.Item)!, 1);

            // At a PC: the boxes opened to pick one, the Starly picked and put in
            Assert.Equal(1, game.StrollAct("pick").Result);
            Assert.Equal(GameState.PCStorage, game.State);
            Assert.True(game.PcScreen.PickingPokemon);
            game.PcScreen.Move(1, 0, game.Party.Count);
            game.PcScreen.Move(1, 0, game.Party.Count);
            game.PcScreen.Move(1, 0, game.Party.Count);
            game.PcScreen.Move(1, 0, game.Party.Count);
            game.PcScreen.Confirm(game.Party, game.Boxes, _ => { });
            Frames(engine, 2);
            var put = game.StrollAct("put");
            Assert.Equal(1, put.Result);
            Assert.DoesNotContain(starly, game.Boxes.All);
            Assert.Equal(starly.Id, game.Stroll.Pokemon!.Id);
            Assert.Equal("sinnoh_meadow", game.Stroll.Course);

            // Two thousand steps of the player's
            for (int i = 0; i < 2000; i++) StrollCounter.Step(game.Stroll);
            Assert.Equal(100, StrollCounter.Strides(game.Stroll));

            // A meet on the screen, played until one is caught
            game.OpenStroll();
            var screen = game.StrollScreen;
            Assert.Equal(GameState.StrollCounter, game.State);
            for (int tries = 0; tries < 9 && game.Stroll.Caught.Count == 0; tries++)
            {
                screen.Move(0, 1); // MEET
                screen.Confirm();
                Assert.Equal(StrollPage.Meet, screen.Page);
                while (screen.Meet!.End == StrollMeetEnd.Going)
                {
                    while (screen.Cursor != (screen.Meet.WildHearts > 0 ? 0 : 2)) screen.Move(1, 0);
                    screen.Confirm();
                }
                screen.Confirm();
                Assert.Equal(StrollPage.Home, screen.Page);
            }
            Assert.Single(game.Stroll.Caught);
            screen.Cancel();
            Frames(engine, 2);
            Assert.Equal(GameState.Overworld, game.State);

            // Back at a PC: the Starly paid and stored, the one met in the boxes too, each in one place
            int seenBefore = game.Pokedex.CaughtCount;

            // With the boxes full nothing leaves the counter; with one place, its own Pokémon comes back first and
            // what it met waits in the counter
            var fillers = new System.Collections.Generic.List<Pokemon>();
            foreach (var box in game.Boxes.Boxes)
                for (int s = 0; s < box.Slots.Length; s++)
                    if (box.Slots[s] == null) { var f = new Pokemon(PokemonDatabase.Get("Bidoof")!, 2, new Random(s)); box.Slots[s] = f; fillers.Add(f); }
            Assert.Equal(3, game.StrollAct("back").Result);
            Assert.NotNull(game.Stroll.Pokemon);
            Assert.Single(game.Stroll.Caught);
            game.Boxes.Remove(fillers[0]);
            var back = game.StrollAct("back");
            Assert.Equal(1, back.Result);
            Assert.Equal(100, back.Number);
            Assert.Null(game.Stroll.Pokemon);
            Assert.Single(game.Stroll.Caught);
            Assert.Equal(0, game.StrollAct("caught").Result);
            Assert.Contains(game.Boxes.All, p => p.Id == starly.Id && p.Level == 13);
            // With room again, what it met reaches the boxes too
            foreach (var f in fillers) game.Boxes.Remove(f);
            Assert.Equal(0, game.StrollAct("back").Result);
            Assert.Equal(1, game.StrollAct("caught").Result);
            Assert.Contains(game.Boxes.All, p => p.Id == starly.Id && p.Level == 13);
            Assert.Null(game.Stroll.Pokemon);
            Assert.Empty(game.Stroll.Caught);
            Assert.True(game.Pokedex.CaughtCount >= seenBefore);
            var ids = game.Party.Members.Concat(game.Boxes.All).Select(p => p.Id).ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());
            Assert.DoesNotContain(Guid.Empty, ids);

            // A save of it all reads back with the same counter
            var data = game.BuildSave();
            Assert.Equal(100, data.Stroll!.StridesEver);
        }
        finally
        {
            SavePaths.Current = paths;
            Ruleset.Use(RulesPreset.Platinum);
        }
    }
}
