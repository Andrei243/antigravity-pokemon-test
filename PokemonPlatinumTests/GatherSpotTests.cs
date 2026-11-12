using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Gathering in the field, CR2 (plan 39): a spot bears once a calendar day, its find drawn by weight from its kind's
/// table; it is hidden before the kit and talks to nobody without it; every finished region has its spots, each on open
/// ground faced from an open tile; a tree cut or a rock smashed leaves wood or stone one time in three with the kit and
/// draws nothing without it.
/// </summary>
[Collection("Shared clocks and dice")]
public class GatherSpotTests
{
    private sealed class Scripted(params int[] rolls) : Random
    {
        private readonly Queue<int> left = new(rolls);
        public int Drawn { get; private set; }
        public override int Next(int maxValue) { Drawn++; return left.Count > 0 ? Math.Min(left.Dequeue(), Math.Max(0, maxValue - 1)) : 0; }
        public override int Next(int minValue, int maxValue) => minValue + Next(maxValue - minValue);
        public override int Next() { Drawn++; return 0; }
        public override double NextDouble() { Drawn++; return 0; }
    }

    private static HeadlessScriptHost WithKit(HeadlessScriptHost host)
    {
        host.Bag.AddItem(ItemDatabase.Get(GameEngine.CraftingKitItem)!, 1);
        return host;
    }

    private static NPC Spot(string type = NPC.GatherRockType) =>
        new() { Name = "Rock", NpcType = type, Key = "route_208.gather", ScriptFile = "route_208", ShownBy = GameEngine.CraftingKitFlag };

    private static void Play(HeadlessScriptHost host, NPC spot)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(FieldScripts.For(spot)!)!, spot);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
    }

    [Fact]
    public void TheTablesAreFieldMaterialsWithWeights()
    {
        Assert.Empty(Gathering.Problems());
        foreach (var kind in Enum.GetValues<GatherKind>()) Assert.NotEmpty(Gathering.FindsOf(kind));
    }

    [Fact]
    public void ASpotBearsOnceACalendarDayItsFindByWeight()
    {
        var state = new CraftingState();
        // A roll of 0 is the first find, the rock's quarry stone, and the count's roll its least (two)
        var rng = new Scripted(0, 0);
        var found = Gathering.Pick(state, "a:b", GatherKind.Rock, 100, rng);
        Assert.Equal(new MaterialCount("quarryStone", 2), found);
        Assert.Equal(2, state.Pouch.Count("quarryStone"));
        // Picked today: nothing, and nothing drawn
        int drawn = rng.Drawn;
        Assert.Null(Gathering.Pick(state, "a:b", GatherKind.Rock, 100, rng));
        Assert.Equal(drawn, rng.Drawn);
        Assert.False(Gathering.Bears(state, "a:b", 100));
        // The next day it bears again; a roll of 8 of the rock's ten weights is its clay
        Assert.True(Gathering.Bears(state, "a:b", 101));
        Assert.Equal("riverClay", Gathering.Pick(state, "a:b", GatherKind.Rock, 101, new Scripted(8, 0))!.Value.Id);
        // Saved and read back
        var back = new CraftingState();
        back.Load(state.ToSave());
        Assert.False(Gathering.Bears(back, "a:b", 101));
    }

    [Fact]
    public void TheSpotsScriptGivesOnceADayUnderAFixedDate()
    {
        GameClock.FixedDate = new DateTime(2026, 6, 1);
        try
        {
            var spot = Spot();
            var host = WithKit(new HeadlessScriptHost { Rng = new Scripted(0, 0) });
            Play(host, spot);
            Assert.Equal(2, host.Crafting!.Pouch.Count("quarryStone"));
            Assert.Contains(host.Transcript, t => t.Text.Contains("gathered 2 × Quarry Stone") || t.Text.Contains("gathered 2 ×"));
            var again = WithKit(new HeadlessScriptHost { Crafting = host.Crafting });
            Play(again, spot);
            Assert.Equal(2, host.Crafting.Pouch.Count("quarryStone"));
            Assert.Contains(again.Transcript, t => t.Text.Contains("Come back tomorrow"));
            GameClock.FixedDate = new DateTime(2026, 6, 2);
            var tomorrow = WithKit(new HeadlessScriptHost { Crafting = host.Crafting, Rng = new Scripted(0, 0) });
            Play(tomorrow, spot);
            Assert.Equal(4, host.Crafting.Pouch.Count("quarryStone"));
        }
        finally { GameClock.FixedDate = null; }
    }

    [Fact]
    public void WithoutTheKitASpotIsHiddenAndGivesNothing()
    {
        var spot = Spot();
        Assert.True(spot.IsThing);
        Assert.Equal(PokemonPlatinumEngine.Overworld.PropType.GatherRock, spot.GymThing);
        spot.GatherBearing = false;
        Assert.Equal(PokemonPlatinumEngine.Overworld.PropType.GatherRockBare, spot.GymThing);
        var rng = new Scripted();
        var host = new HeadlessScriptHost { Rng = rng };
        Play(host, Spot());
        Assert.Equal(0, rng.Drawn);
        Assert.Equal(0, host.Crafting!.Pouch.Kinds);
    }

    [Fact]
    public void EveryFinishedRegionHasItsSpotsShownByTheKitAndOnOpenGround()
    {
        foreach (var world in World.LoadAll().Where(w => w.Index.Region is "Sinnoh" or "Kanto" or "Johto" or "Hoenn" or "Unova"))
        {
            var spots = world.BuildMaps().SelectMany(m => m.Everyone.Where(n => n.IsGatherSpot).Select(n => (Map: m, Spot: n))).ToList();
            Assert.InRange(spots.Count, 6, 10);
            foreach (var (map, spot) in spots)
            {
                Assert.Equal(GameEngine.CraftingKitFlag, spot.ShownBy);
                Assert.Equal(FieldScripts.Gather, FieldScripts.For(spot));
                Assert.Empty(ContentCheck.GatherSpots.Why(map, spot.GridX, spot.GridY, spot.ShownBy));
                // Hidden in a game without the kit, there with it
                map.ApplyPresence(_ => false);
                Assert.DoesNotContain(spot, map.NPCs);
                map.ApplyPresence(f => f == GameEngine.CraftingKitFlag);
                Assert.Contains(spot, map.NPCs);
            }
        }
    }

    [Fact]
    public void ATreeCutOrARockSmashedLeavesWoodOrStoneOneTimeInThreeWithTheKit()
    {
        var pouch = new MaterialPouch();
        Assert.Equal(new MaterialCount("timber", 1), Gathering.Cleared(pouch, tree: true, new Scripted(0)));
        Assert.Null(Gathering.Cleared(pouch, tree: false, new Scripted(1)));
        Assert.Equal(new MaterialCount("quarryStone", 1), Gathering.Cleared(pouch, tree: false, new Scripted(0)));

        // The script's word: with the kit one draw, without it none
        var source = "script T\n  craft cleared tree\n  end";
        var withKit = WithKit(new HeadlessScriptHost { Rng = new Scripted(0) });
        var runner = new ScriptRunner(ScriptLibrary.Default, withKit);
        runner.Start(ScriptParser.Parse("t", source)[0], null);
        runner.RunToEnd();
        Assert.Equal(1, runner.Result);
        Assert.Equal(1, withKit.Crafting!.Pouch.Count("timber"));
        var rng = new Scripted(0);
        var without = new HeadlessScriptHost { Rng = rng };
        var bare = new ScriptRunner(ScriptLibrary.Default, without);
        bare.Start(ScriptParser.Parse("t", source)[0], null);
        bare.RunToEnd();
        Assert.Equal(0, bare.Result);
        Assert.Equal(0, rng.Drawn);
    }
}
