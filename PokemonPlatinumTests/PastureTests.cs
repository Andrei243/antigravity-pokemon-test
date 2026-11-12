using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 35 · BN5, the pasture: every region's copy is the plan, the herd is the box shown (one body each, no Egg, none of
/// the team), placed the same every visit, TAKE ALONG moves one into the team and is refused with a full team or by a run,
/// no Pokémon is in two places, a day's find comes once, the gate leads back to the PC, a save made in the meadow lays the
/// same herd, and nobody flies out.
/// </summary>
[Collection("Shared clocks and dice")]
public class PastureTests
{
    private static readonly string[] Regions = { RegionDatabase.Sinnoh, RegionDatabase.Johto, RegionDatabase.Unova };

    [Fact]
    public void EveryFinishedRegionHasACopyOfThePlanAmongItsMaps()
    {
        var plan = MapDatabase.Get("SinnohPasture");
        foreach (string region in Regions)
        {
            string? name = Pasture.MapOf(region);
            Assert.Equal(region + "Pasture", name);
            Assert.Contains(name!, RegionDatabase.Get(region)!.Maps);
            var copy = MapDatabase.Get(name!);
            Assert.Equal(name, copy.Name);
            Assert.True(copy.NoBoxLink);
            Assert.Equal((plan.Width, plan.Height), (copy.Width, copy.Height));
            for (int y = 0; y < plan.Height; y++)
                for (int x = 0; x < plan.Width; x++)
                {
                    Assert.Equal(plan.GetGroundTile(x, y), copy.GetGroundTile(x, y));
                    Assert.Equal(plan.IsSolid(x, y), copy.IsSolid(x, y));
                }
            Assert.Empty(copy.Warps);
            Assert.True(copy.IsWalkable(Pasture.GateX, Pasture.GateY));
            Assert.Contains(copy.Triggers, t => t.Script == "PastureGate");
        }
        // A region with none offers none
        Assert.Null(Pasture.MapOf(RegionDatabase.Kalos));
        Assert.Null(Pasture.MapOf(RegionDatabase.Kanto));
        Assert.Null(Pasture.MapOf(RegionDatabase.Hoenn));
    }

    [Fact]
    public void PlacesComeFromTheBoxAndSlotAloneAndNoTwoShareATile()
    {
        var map = MapDatabase.Get("SinnohPasture");
        var open = Pasture.OpenTiles(map);
        Assert.True(open.Count >= PcBoxes.BoxSize, "the meadow holds a whole box");
        Assert.All(open, t => Assert.True(t.Y < Pasture.GateRows && !map.IsSolid(t.X, t.Y)));
        var slots = Enumerable.Range(0, PcBoxes.BoxSize).Select(i => (Pokemon?)new Pokemon(PokemonDatabase.Get("Bidoof")!, 5)).ToArray();
        slots[3] = null;
        slots[7]!.EggCycles = 5;
        var egg = slots[7]!;
        var first = Pasture.Place(open, 2, slots);
        var again = Pasture.Place(open, 2, slots);
        Assert.Equal(first.Select(p => (p.X, p.Y)), again.Select(p => (p.X, p.Y)));
        Assert.Equal(first.Count, first.Select(p => (p.X, p.Y)).Distinct().Count());
        Assert.DoesNotContain(first, p => p.Slot == 3);
        if (egg.IsEgg) Assert.DoesNotContain(first, p => p.Slot == 7);
        Assert.NotEqual(first.Select(p => (p.X, p.Y)), Pasture.Place(open, 3, slots).Select(p => (p.X, p.Y)));
    }

    [Fact]
    public void ADaysFindIsTheSameEveryTimeAndComesOneTimeInEightOrSo()
    {
        var ids = Enumerable.Range(1, 800).Select(i => new Guid(i, 3, 5, new byte[8])).ToList();
        int found = ids.Count(id => Pasture.FindOf(100, id, RegionDatabase.Sinnoh) != null);
        Assert.InRange(found, 60, 140);
        foreach (var id in ids.Take(50))
            Assert.Equal(Pasture.FindOf(100, id, RegionDatabase.Johto), Pasture.FindOf(100, id, RegionDatabase.Johto));
        foreach (string region in Regions)
            Assert.All(Pasture.FindTable(region), item => Assert.NotNull(ItemDatabase.Get(item)));
    }

    // ------------------------------------------------------------------ in the game

    private static void Frames(GameEngine engine, int n)
    {
        for (int i = 0; i < n; i++) engine.Update(1f / 60f);
    }

    private static (GameEngine Engine, GameEngine.Driver Game, string Folder) NewGame()
    {
        string folder = Path.Combine(Path.GetTempPath(), "pasture-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        SavePaths.Current = SavePaths.In(folder);
        Dice.Seed(7);
        var engine = new GameEngine(new GameSettings { Muted = true });
        engine.InitializeHeadless();
        engine.PeopleStayPut = true;
        var game = engine.Drive;
        engine.NewGameRegion = "Sinnoh";
        engine.StartNewGame();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 10));
        game.GiveIds();
        // A Pokémon Center's PC, the tile before it
        game.Place(MapDatabase.Get("JubilifePokemonCenter"), 12, 4, Direction.Up);
        game.ArriveOnMap();
        return (engine, game, folder);
    }

    private static void FillBox(GameEngine.Driver game, int box, params string[] species)
    {
        for (int i = 0; i < species.Length; i++)
            game.Boxes.Boxes[box].Slots[i] = new Pokemon(PokemonDatabase.Get(species[i])!, 8 + i);
        game.GiveIds();
    }

    private static void Enter(GameEngine engine, GameEngine.Driver game)
    {
        Assert.True(game.PastureHere);
        Assert.Equal(1, game.Pasture("enter").Result);
        for (int i = 0; i < 240 && game.Map.Name != "SinnohPasture"; i++) Frames(engine, 1);
        Frames(engine, 60);
        Assert.Equal("SinnohPasture", game.Map.Name);
        Assert.Equal((Pasture.GateX, Pasture.GateY), (game.Player.GridX, game.Player.GridY));
    }

    private static NPC BodyOf(GameEngine.Driver game, Pokemon p) =>
        game.Map.NPCs.Single(n => game.Herd.TryGetValue(n.Id, out var h) && ReferenceEquals(h, p));

    [Fact]
    public void TheHerdIsTheBoxShownLaidTheSameEveryVisitAndTheGateLeadsBackToThePC()
    {
        var paths = SavePaths.Current;
        var (engine, game, folder) = NewGame();
        try
        {
            FillBox(game, 0, "Bidoof", "Starly", "Shinx", "Budew", "Kricketot");
            game.Boxes.Boxes[0].Slots[2]!.EggCycles = 3;
            game.Boxes.Boxes[0].Slots[2]!.IsEgg = true;
            game.Boxes.CurrentBox = 0;
            Enter(engine, game);

            Assert.Equal(4, game.Herd.Count);
            Assert.DoesNotContain(game.Herd.Values, p => p.IsEgg);
            Assert.DoesNotContain(game.Herd.Values, p => game.Party.Members.Contains(p));
            Assert.All(game.Herd.Values, p => Assert.Single(game.Map.NPCs, n => game.Herd.TryGetValue(n.Id, out var h) && ReferenceEquals(h, p)));
            var places = game.Herd.OrderBy(h => h.Key).Select(h => (h.Key, game.Map.NPCs.Single(n => n.Id == h.Key).GridX, game.Map.NPCs.Single(n => n.Id == h.Key).GridY)).ToList();

            // Out at the gate, back on the PC's tile facing it, and the herd taken off the meadow
            Assert.Equal(1, game.Pasture("leave").Result);
            for (int i = 0; i < 240 && game.Map.Name != "JubilifePokemonCenter"; i++) Frames(engine, 1);
            Frames(engine, 30);
            Assert.Equal("JubilifePokemonCenter", game.Map.Name);
            Assert.Equal((12, 4, Direction.Up), (game.Player.GridX, game.Player.GridY, game.Player.Facing));
            Assert.Null(game.PastureVisit);
            Assert.Empty(game.Herd);
            Assert.DoesNotContain(MapDatabase.Get("SinnohPasture").NPCs, n => n.Id.StartsWith(Pasture.HerdPrefix));

            // In again: the same places
            Enter(engine, game);
            var again = game.Herd.OrderBy(h => h.Key).Select(h => (h.Key, game.Map.NPCs.Single(n => n.Id == h.Key).GridX, game.Map.NPCs.Single(n => n.Id == h.Key).GridY)).ToList();
            Assert.Equal(places, again);

            // The board's next box: an empty one walks nobody, and the PC's box follows
            Assert.Equal(2, game.Pasture("box", 1).Result);
            Assert.Equal(1, game.Boxes.CurrentBox);
            Assert.Empty(game.Herd);
            Assert.Equal(1, game.Pasture("box", -1).Result);
            Assert.Equal(4, game.Herd.Count);

            // Nobody flies, teleports or digs out of the meadow
            Assert.False(FieldMoveRules.SpotOf(game.Map, game.Player.GridX, game.Player.GridY, Direction.Up, new Walker(TravelMode.OnFoot, 0)).FlyAllowed);
        }
        finally
        {
            SavePaths.Current = paths;
            Ruleset.Use(RulesPreset.Platinum);
        }
    }

    [Fact]
    public void TakeAlongMovesOneIntoTheTeamAndNoPokemonIsEverInTwoPlaces()
    {
        var paths = SavePaths.Current;
        var (engine, game, _) = NewGame();
        try
        {
            FillBox(game, 0, "Bidoof", "Starly", "Shinx");
            Enter(engine, game);
            var bidoof = game.Boxes.Boxes[0].Slots[0]!;
            var body = BodyOf(game, bidoof);

            Assert.Equal(1, game.Pasture("take", subject: body).Result);
            Assert.Contains(bidoof, game.Party.Members);
            Assert.DoesNotContain(bidoof, game.Boxes.All);
            Assert.DoesNotContain(bidoof, game.Herd.Values);
            Assert.DoesNotContain(body, game.Map.NPCs);
            AssertEachInOnePlace(game);

            // A full team refuses
            while (!game.Party.IsFull) game.Party.Add(new Pokemon(PokemonDatabase.Get("Geodude")!, 5));
            game.GiveIds();
            var starly = game.Boxes.Boxes[0].Slots[1]!;
            Assert.Equal(2, game.Pasture("take", subject: BodyOf(game, starly)).Result);
            Assert.Contains(starly, game.Boxes.All);
            AssertEachInOnePlace(game);
        }
        finally
        {
            SavePaths.Current = paths;
            Ruleset.Use(RulesPreset.Platinum);
        }
    }

    private static void AssertEachInOnePlace(GameEngine.Driver game)
    {
        var all = game.Party.Members.Concat(game.Boxes.All).Select(p => p.Id).Concat(game.JobBoard.Away.Where(j => j.Id != null).Select(j => j.Id!.Value)).ToList();
        Assert.Equal(all.Count, all.Distinct().Count());
        foreach (var p in game.Herd.Values) Assert.Contains(p, game.Boxes.All);
    }

    [Fact]
    public void APatAFindAndAMealOfTheMeadowCountByTheDay()
    {
        var paths = SavePaths.Current;
        var (engine, game, _) = NewGame();
        try
        {
            FillBox(game, 0, Enumerable.Repeat("Bidoof", 30).ToArray());
            Enter(engine, game);
            var (id, p) = game.Herd.First();
            var body = game.Map.NPCs.Single(n => n.Id == id);
            int before = p.Friendship;
            Assert.Equal(1, game.Pasture("pet", subject: body).Result);
            Assert.Equal(2, game.Pasture("pet", subject: body).Result);
            Assert.True(p.Friendship > before);

            // Whichever of the thirty hold something today give it up once
            int gave = 0;
            foreach (var (bodyId, _) in game.Herd.ToList())
            {
                var who = game.Map.NPCs.Single(n => n.Id == bodyId);
                var first = game.Pasture("find", subject: who);
                if (first.Result == 1)
                {
                    gave++;
                    Assert.NotNull(first.Item);
                    Assert.True(game.Bag.GetQuantity(ItemDatabase.Get(first.Item!)!) > 0);
                }
                Assert.Equal(0, game.Pasture("find", subject: who).Result);
            }
            Assert.InRange(gave, 0, 30);

            // A berry eaten, and a full Pokémon refuses the sixth
            var berry = ItemDatabase.Get("Oran Berry")!;
            game.Bag.AddItem(berry, 10);
            for (int i = 0; i < CareRules.Fullness; i++) Assert.Equal(1, game.Pasture("feed", subject: body, item: berry.Name).Result);
            Assert.Equal(0, game.Pasture("feed", subject: body, item: berry.Name).Result);
            Assert.Equal(10 - CareRules.Fullness, game.Bag.GetQuantity(berry));
        }
        finally
        {
            SavePaths.Current = paths;
            Ruleset.Use(RulesPreset.Platinum);
        }
    }

    [Fact]
    public void ASaveMadeInTheMeadowLaysTheSameHerdAsItLoads()
    {
        var paths = SavePaths.Current;
        var (engine, game, _) = NewGame();
        try
        {
            FillBox(game, 0, "Bidoof", "Starly", "Shinx");
            Enter(engine, game);
            var places = game.Herd.ToDictionary(h => h.Key, h => (game.Map.NPCs.Single(n => n.Id == h.Key).GridX, game.Map.NPCs.Single(n => n.Id == h.Key).GridY));
            var save = game.BuildSave();
            Assert.NotNull(save.Pasture);
            Assert.Equal("JubilifePokemonCenter", save.Pasture!.Map);

            var (engine2, game2, _) = NewGame();
            game2.ApplySave(save);
            game2.ArriveOnMap();
            Assert.Equal("SinnohPasture", game2.Map.Name);
            Assert.NotNull(game2.PastureVisit);
            Assert.Equal(places, game2.Herd.ToDictionary(h => h.Key, h => (game2.Map.NPCs.Single(n => n.Id == h.Key).GridX, game2.Map.NPCs.Single(n => n.Id == h.Key).GridY)));
            Assert.Equal(game.Herd.Values.Select(p => p.Id).OrderBy(i => i), game2.Herd.Values.Select(p => p.Id).OrderBy(i => i));
        }
        finally
        {
            SavePaths.Current = paths;
            Ruleset.Use(RulesPreset.Platinum);
        }
    }
}
