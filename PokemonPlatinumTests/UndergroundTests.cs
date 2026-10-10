using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Underground;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// The Underground (plan 06 · R16a): the tables read from the original (<c>src/underground/mining.c</c>,
/// <c>goods.c</c>, <c>trap_prices.c</c>, <c>treasure_prices.c</c>, <c>vendors.c</c>), the wall's game, the day's spread
/// of walls and traps, Spheres buried and grown, traps and what frees one from each, the hikers' trade, the secret base
/// and its decorating, the other diggers' flags, the save, the map and the scripts. Every number is the original's,
/// worked out beside it.
/// </summary>
[Collection("MapDatabase")]
public class UndergroundTests
{
    private static int Trap(string id) => UndergroundDatabase.TrapId(id);
    private static int Good(string id) => UndergroundDatabase.GoodId(id);
    private static int Treasure(string id) => UndergroundDatabase.TreasureId(id);

    /// <summary>A wall game for a trainer of an even ID with no National Pokédex, past the first wall.</summary>
    private static MiningGame Wall(ulong seed, bool first = false, bool odd = false, bool national = false, Func<int, bool>? neverMined = null) =>
        new(new Rand32(seed), odd, national, first, neverMined ?? (_ => true));

    // ------------------------------------------------------------------ the tables

    [Fact]
    public void TheTablesAreTheOriginals()
    {
        // sMiningObjects has 85 rows; MiningObjectID runs to 59 before the rocks; 33 traps; 138 goods; 20 hikers
        Assert.Equal(85, UndergroundDatabase.Objects.Count);
        Assert.Equal(60, UndergroundDatabase.TreasureEnd);
        Assert.Equal(33, UndergroundDatabase.TrapCount);
        Assert.Equal(138, UndergroundDatabase.GoodCount);
        Assert.Equal(20, UndergroundDatabase.Vendors.Count);

        // { MINING_SMALL_PRISM_SPHERE, 30, 22, 27, 20, 2 * 2, 2 * 2 }
        var prism = UndergroundDatabase.Objects[0];
        Assert.Equal(("SMALL_PRISM_SPHERE", 30, 22, 27, 20, 2, 2), (prism.Id, prism.Odd, prism.Even, prism.OddNational, prism.EvenNational, prism.Width, prism.Height));
        // The Skull Fossil is an odd ID's and the Armor Fossil an even one's: 25 against nothing, 12 with the National Pokédex
        var skull = UndergroundDatabase.Objects.Single(o => o.Id == "TREASURE_SKULL_FOSSIL");
        var armor = UndergroundDatabase.Objects.Single(o => o.Id == "TREASURE_ARMOR_FOSSIL");
        Assert.Equal((25, 0, 12, 0), (skull.Odd, skull.Even, skull.OddNational, skull.EvenNational));
        Assert.Equal((0, 25, 0, 12), (armor.Odd, armor.Even, armor.OddNational, armor.EvenNational));
        // The Heart Scale's shape: { 'x', 'o' }, { 'x', 'x' }
        Assert.Equal(new[] { "xo", "xx" }, UndergroundDatabase.Objects.Single(o => o.Id == "TREASURE_HEART_SCALE").Shape);
        // The Damp Rock's shape is declared four wide and written three wide: read by its own width it has no hole
        Assert.Null(UndergroundDatabase.Objects.Single(o => o.Id == "TREASURE_DAMP_ROCK").Shape);

        // sMiningItems and sTreasurePrices: an Odd Keystone for a Prism Sphere of 50 to 60, a plate for 70 to 80
        var keystone = UndergroundDatabase.Treasure(Treasure("TREASURE_ODD_KEYSTONE"))!;
        Assert.Equal(("Odd Keystone", SphereKind.Prism, 50, 60), (keystone.Item, keystone.Price.Kind, keystone.Price.Min, keystone.Price.Max));
        Assert.Equal((SphereKind.Red, 70, 80), (UndergroundDatabase.Treasure(Treasure("TREASURE_FLAME_PLATE"))!.Price.Kind, 70, 80));
        Assert.All(Enumerable.Range(UndergroundDatabase.FirstTreasure, 49), id => Assert.NotNull(ItemDatabase.Get(UndergroundDatabase.Treasure(id)!.Item!)));

        // sTrapPrices: the Digger Drill for any kind of Sphere of 15 to 20; the dummies for nothing
        var drill = UndergroundDatabase.Trap(Trap("TRAP_DIGGER_DRILL"))!;
        Assert.True(drill.Price.Any);
        Assert.Equal((15, 20), (drill.Price.Min, drill.Price.Max));
        Assert.False(UndergroundDatabase.Trap(Trap("TRAP_WARP_DUMMY"))!.Price.ForTrade);

        // sGoodData: the Big Table is three by three and all of it blocks; a Wooden Chair blocks nothing
        var table = UndergroundDatabase.Good(Good("UG_GOOD_BIG_TABLE"))!;
        Assert.Equal((3, 3, "111111111", SphereKind.Blue, 12, 15), (table.Width, table.Depth, table.Collision, table.Price.Kind, table.Price.Min, table.Price.Max));
        Assert.Equal("000000000", UndergroundDatabase.Good(Good("UG_GOOD_WOODEN_CHAIR"))!.Collision);
        Assert.Equal("Buneary Doll", UndergroundDatabase.GoodName(Good("UG_GOOD_BUNEARY_DOLL")));
        // The hikers: eight of goods, eight of traps, four of treasures, the first at (72, 104)
        Assert.Equal(new[] { 8, 8, 4 }, new[] { "Goods", "Traps", "Treasures" }.Select(k => UndergroundDatabase.Vendors.Count(v => v.Kind == k)));
        Assert.Equal((72, 104), (UndergroundDatabase.Vendors[0].X, UndergroundDatabase.Vendors[0].Z));
    }

    [Fact]
    public void TheConsolesGeneratorsDrawAsItsKitDoes()
    {
        // MATH_Rand16 from a seed of 0: x = 2531011 = 0x269EC3, whose upper half is 0x26 = 38; of 65536 that is 38
        Assert.Equal(38, new Rand16(0).Next(65536));
        // MATH_Rand32 from 0: x = 2531011, whose upper 32 bits are nothing
        Assert.Equal(0, new Rand32(0).Next(1000));
        var a = new Rand16(77);
        var b = new Rand16(77);
        Assert.Equal(Enumerable.Range(0, 50).Select(_ => a.Next(100)), Enumerable.Range(0, 50).Select(_ => b.Next(100)));
        var c = new Rand32(5);
        Assert.All(Enumerable.Range(0, 200), _ => Assert.InRange(c.Next(13), 0, 12));
    }

    // ------------------------------------------------------------------ the wall's game

    [Fact]
    public void AWallHoldsTwoToFourThingsAndDarkRockBetween()
    {
        for (ulong seed = 1; seed <= 60; seed++)
        {
            var game = Wall(seed * 7919);
            // itemCount = MATH_Rand32(MAX_BURIED_ITEMS - 1) + 2: two to four; eight objects at most with the rocks
            Assert.InRange(game.ItemCount, 2, MiningGame.MaxItems);
            Assert.InRange(game.Objects.Count, game.ItemCount, MiningGame.MaxObjects);
            Assert.All(game.Objects.Take(game.ItemCount), o => Assert.False(o.IsRock));
            Assert.All(game.Objects.Skip(game.ItemCount), o => Assert.True(o.IsRock));
            // Nothing over the wall's edge, and no square under two things
            var covered = new HashSet<(int, int)>();
            foreach (var o in game.Objects)
                for (int y = o.Y; y < o.Y + o.Object.Height; y++)
                    for (int x = o.X; x < o.X + o.Object.Width; x++)
                    {
                        if (!o.Covers(x, y)) continue;
                        Assert.InRange(x, 0, MiningGame.Width - 1);
                        Assert.InRange(y, 0, MiningGame.Height - 1);
                        Assert.True(covered.Add((x, y)));
                        Assert.Equal(game.Objects.ToList().IndexOf(o), game.ObjectAt(x, y));
                    }
            // The dirt lies two, four or six layers thick (Mining_RandomizeDirtLayers)
            for (int y = 0; y < MiningGame.Height; y++)
                for (int x = 0; x < MiningGame.Width; x++) Assert.Contains(game.DirtAt(x, y), new[] { 2, 4, 6 });
            Assert.Equal(MiningGame.InitialIntegrity, game.Integrity);
            Assert.True(game.PickSelected);
        }
    }

    [Fact]
    public void TheFirstWallHoldsThreeThingsAndNoRock()
    {
        for (ulong seed = 1; seed <= 20; seed++)
        {
            var game = Wall(seed, first: true);
            Assert.Equal(3, game.ItemCount);
            Assert.Equal(3, game.Objects.Count);
        }
    }

    [Fact]
    public void WhatAWallHoldsHangsOnTheTrainersNumberAndThePokedex()
    {
        int skull = Treasure("TREASURE_SKULL_FOSSIL"), armor = Treasure("TREASURE_ARMOR_FOSSIL"), helix = Treasure("TREASURE_HELIX_FOSSIL");
        var odd = new List<int>();
        var even = new List<int>();
        var national = new List<int>();
        for (ulong seed = 1; seed <= 400; seed++)
        {
            odd.AddRange(Wall(seed * 31, odd: true).Objects.Select(o => o.Id));
            even.AddRange(Wall(seed * 31).Objects.Select(o => o.Id));
            national.AddRange(Wall(seed * 31, odd: true, national: true).Objects.Select(o => o.Id));
        }
        // An odd number digs Skull Fossils and never an Armor Fossil; an even one the other way round
        Assert.Contains(skull, odd);
        Assert.DoesNotContain(armor, odd);
        Assert.Contains(armor, even);
        Assert.DoesNotContain(skull, even);
        // The Helix Fossil weighs nothing until the National Pokédex is had
        Assert.DoesNotContain(helix, odd);
        Assert.Contains(helix, national);
    }

    [Fact]
    public void APlateIsFoundOnceAndNeverTwiceInAWall()
    {
        var plates = Enumerable.Range(1, 59).Where(UndergroundDatabase.IsPlate).ToList();
        Assert.Equal(16, plates.Count);
        var state = new UndergroundState();
        foreach (int plate in plates.Skip(1)) state.NotePlate(plate);
        Assert.True(state.NeverMined(plates[0]));
        Assert.False(state.NeverMined(plates[1]));
        Assert.True(state.NeverMined(Treasure("TREASURE_HEART_SCALE")));
        for (ulong seed = 1; seed <= 300; seed++)
        {
            var found = Wall(seed * 131, neverMined: state.NeverMined).Objects.Select(o => o.Id).Where(UndergroundDatabase.IsPlate).ToList();
            // Only the plate not yet dug up can turn up, and once
            Assert.All(found, id => Assert.Equal(plates[0], id));
            Assert.True(found.Count <= 1);
        }
    }

    /// <summary>A game with a square at least as thick as asked, its four neighbours and corners inside the wall, and no dark rock under it.</summary>
    private static (MiningGame Game, int X, int Y) WallWithThickSquare()
    {
        for (ulong seed = 1; seed < 500; seed++)
        {
            var game = Wall(seed * 977);
            for (int y = 1; y < MiningGame.Height - 1; y++)
                for (int x = 1; x < MiningGame.Width - 1; x++)
                {
                    bool thick = true;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++) thick &= game.DirtAt(x + dx, y + dy) >= 4;
                    if (thick && game.ObjectAt(x, y) < 0) return (game, x, y);
                }
        }
        throw new InvalidOperationException("no such wall");
    }

    [Fact]
    public void ThePickTakesTwoLayersAndOneFromTheSquaresBeside()
    {
        var (game, x, y) = WallWithThickSquare();
        int[,] before = Snapshot(game);
        var blow = game.Strike(x, y);
        Assert.True(blow.Landed);
        // Mining_RemoveDirt: two off the square; the pick takes one off each of the four beside it and leaves the corners
        Assert.Equal(before[y, x] - 2, game.DirtAt(x, y));
        foreach (var (dx, dy) in new[] { (0, 1), (0, -1), (1, 0), (-1, 0) }) Assert.Equal(before[y + dy, x + dx] - 1, game.DirtAt(x + dx, y + dy));
        foreach (var (dx, dy) in new[] { (1, 1), (-1, -1), (1, -1), (-1, 1) }) Assert.Equal(before[y + dy, x + dx], game.DirtAt(x + dx, y + dy));
        // The wall loses 4 of its 196
        Assert.Equal(192, game.Integrity);
    }

    [Fact]
    public void TheHammerTakesTwoFromTheSquaresBesideAndOneFromTheCorners()
    {
        var (game, x, y) = WallWithThickSquare();
        game.PickSelected = false;
        int[,] before = Snapshot(game);
        game.Strike(x, y);
        Assert.Equal(before[y, x] - 2, game.DirtAt(x, y));
        foreach (var (dx, dy) in new[] { (0, 1), (0, -1), (1, 0), (-1, 0) }) Assert.Equal(before[y + dy, x + dx] - 2, game.DirtAt(x + dx, y + dy));
        foreach (var (dx, dy) in new[] { (1, 1), (-1, -1), (1, -1), (-1, 1) }) Assert.Equal(before[y + dy, x + dx] - 1, game.DirtAt(x + dx, y + dy));
        // And 8 of its 196
        Assert.Equal(188, game.Integrity);
    }

    private static int[,] Snapshot(MiningGame game)
    {
        var dirt = new int[MiningGame.Height, MiningGame.Width];
        for (int y = 0; y < MiningGame.Height; y++)
            for (int x = 0; x < MiningGame.Width; x++) dirt[y, x] = game.DirtAt(x, y);
        return dirt;
    }

    [Fact]
    public void ABlowOnBareDarkRockTakesNothingFromTheSquaresBeside()
    {
        for (ulong seed = 1; seed < 300; seed++)
        {
            var game = Wall(seed * 613);
            var rock = game.Objects.FirstOrDefault(o => o.IsRock && o.X > 0 && o.Y > 0 && o.X < MiningGame.Width - 1 && o.Y < MiningGame.Height - 1 && o.Covers(o.X, o.Y));
            if (rock == null) continue;
            // Bare the rock's corner square, then strike it again
            while (game.DirtAt(rock.X, rock.Y) > 0) game.Strike(rock.X, rock.Y);
            int[,] before = Snapshot(game);
            int stood = game.Integrity;
            var blow = game.Strike(rock.X, rock.Y);
            Assert.True(blow.HitRock);
            for (int y = 0; y < MiningGame.Height; y++)
                for (int x = 0; x < MiningGame.Width; x++) Assert.Equal(before[y, x], game.DirtAt(x, y));
            // The wall suffers all the same
            Assert.Equal(stood - MiningGame.PickDamage, game.Integrity);
            return;
        }
        Assert.Fail("no wall with a rock away from its edge");
    }

    [Fact]
    public void TheWallFallsInAfterFortyNineBlowsOfThePickOrTwentyFiveOfTheHammer()
    {
        // 196 / 4 = 49 blows of the pick; the hammer's 8 leave 4 after 24 blows, and the 25th takes the rest
        foreach (var (pick, blows) in new[] { (true, 49), (false, 25) })
        {
            // A corner square bares nothing whole on its own in these walls: look for a wall where hammering the corner ends in a collapse
            for (ulong seed = 1; seed < 200; seed++)
            {
                var game = Wall(seed * 101);
                if (game.ObjectAt(0, 0) >= 0 || game.ObjectAt(1, 0) >= 0 || game.ObjectAt(0, 1) >= 0 || game.ObjectAt(1, 1) >= 0) continue;
                game.PickSelected = pick;
                int struck = 0;
                while (!game.Over)
                {
                    game.Strike(0, 0);
                    struck++;
                }
                Assert.Equal(blows, struck);
                Assert.True(game.Collapsed);
                Assert.Empty(game.Finds());
                Assert.False(game.Strike(5, 5).Landed);
                break;
            }
        }
    }

    [Fact]
    public void WhatLiesWhollyBareIsThePlayersAndASpheresSizeGrowsWithTheWallLeftStanding()
    {
        int spheres = 0;
        for (ulong seed = 1; seed <= 80; seed++)
        {
            var game = Wall(seed * 271);
            // Dig every thing out with the pick, square by square, as long as the wall stands
            foreach (var o in game.Objects.Take(game.ItemCount))
                for (int y = o.Y; y < o.Y + o.Object.Height; y++)
                    for (int x = o.X; x < o.X + o.Object.Width; x++)
                        while (!game.Over && o.Covers(x, y) && game.DirtAt(x, y) > 0) game.Strike(x, y);
            var finds = game.Finds();
            Assert.Equal(Enumerable.Range(0, game.ItemCount).Count(game.IsBare), finds.Count);
            Assert.Equal(game.AllDug, finds.Count == game.ItemCount);
            foreach (var find in finds)
            {
                if (!UndergroundDatabase.IsSphere(find.Id))
                {
                    Assert.Equal(0, find.SphereSize);
                    continue;
                }
                spheres++;
                // Mining_GenerateSizeOfMinedSphere: 1 (Prism, Pale) or 1 to 4, +10 for a large one, +integrity / 39, +5 unless the wall fell
                var kind = UndergroundDatabase.SphereOf(find.Id);
                int bonus = (UndergroundDatabase.IsLargeSphere(find.Id) ? 10 : 0) + game.Integrity / 39 + (game.Integrity != 0 ? 5 : 0);
                if (kind is SphereKind.Prism or SphereKind.Pale) Assert.Equal(1 + bonus, find.SphereSize);
                else Assert.InRange(find.SphereSize, 1 + bonus, 4 + bonus);
            }
        }
        Assert.True(spheres > 20);
        // The records' fossils: the six of the first games, the Old Amber and, by the original's slip, the Rare Bone
        Assert.True(MiningGame.CountsAsFossil(Treasure("TREASURE_SKULL_FOSSIL")));
        Assert.True(MiningGame.CountsAsFossil(Treasure("TREASURE_RARE_BONE")));
        Assert.False(MiningGame.CountsAsFossil(Treasure("TREASURE_HEART_SCALE")));
    }

    [Fact]
    public void TheWallsGameOnScreenPutsWhatWasDugIntoTheBagAndTheRecords()
    {
        var state = new UndergroundState();
        var screen = new MiningScreen();
        var game = Wall(4242, first: true);
        screen.Open(game, state);
        Assert.Equal(MiningPhase.Intro, screen.Phase);
        Assert.True(screen.Lesson);
        screen.PressConfirm();
        // The first wall comes with a word of advice
        Assert.Equal(MiningPhase.Intro, screen.Phase);
        screen.PressConfirm();
        Assert.Equal(MiningPhase.Digging, screen.Phase);
        screen.PressTool();
        Assert.False(game.PickSelected);
        screen.PressTool();
        // Walk the cursor over each thing and strike until it is bare or the wall is down
        foreach (var o in game.Objects)
            for (int y = o.Y; y < o.Y + o.Object.Height; y++)
                for (int x = o.X; x < o.X + o.Object.Width; x++)
                    while (!game.Over && o.Covers(x, y) && game.DirtAt(x, y) > 0)
                    {
                        screen.Move(x - screen.CursorX, y - screen.CursorY);
                        screen.PressConfirm();
                    }
        Assert.Equal(MiningPhase.Ended, screen.Phase);
        Assert.True(state.HasMined);
        screen.Advance(1f);
        int guard = 0;
        while (screen.IsActive && guard++ < 40) screen.PressConfirm();
        Assert.False(screen.IsActive);
        Assert.Equal(screen.Finds.Count, screen.Kept);
        int spheres = screen.Finds.Count(f => UndergroundDatabase.IsSphere(f.Id));
        Assert.Equal(spheres, state.Spheres.Count);
        Assert.Equal(screen.Finds.Count - spheres, state.Treasures.Count);
        Assert.Equal(spheres, state.Records.SpheresDug);
        Assert.Equal(screen.Finds.Count - spheres, state.Records.FossilsDug + state.Records.TreasuresDug);
    }

    // ------------------------------------------------------------------ the tunnels

    private static Map Tunnel => MapDatabase.Get("Underground");

    private static UndergroundField Field(UndergroundState state) => new(state, (x, z) => !Tunnel.InBounds(x, z) || Tunnel.IsSolid(x, z));

    [Fact]
    public void TheUndergroundLiesUnderTheWholeRegionAtHalfItsSize()
    {
        var map = Tunnel;
        Assert.Equal("Underground", map.Name);
        Assert.Equal((480, 480), (map.Width, map.Height));
        Assert.True(map.IsCave);
        // MapChangeUndergroundContext_New: the chunk at (1, 6) of the overworld lies over the first of the tunnels' own, and
        // whoever goes down lands on (8, 8), (23, 8), (8, 23) or (23, 23) of the chunk below
        Assert.Equal((40, 104), UndergroundField.Below(32, 6 * 32));
        Assert.Equal((40, 104), UndergroundField.Below(63, 6 * 32 + 31));
        Assert.Equal((55, 104), UndergroundField.Below(64, 6 * 32));
        Assert.Equal((40, 119), UndergroundField.Below(32, 7 * 32));
        Assert.Equal((72, 136), UndergroundField.Below(96, 8 * 32));
        // Wherever a chunk of Sinnoh has ground to stand on, the tile below it is floor. The tunnels are several systems
        // that don't join, so this also says that each of them was opened (world.json's arrivalsInEveryChunk)
        var above = MapDatabase.Get("Sinnoh");
        int places = 0;
        for (int cz = 6; cz < 30; cz++)
            for (int cx = 1; cx < 29; cx++)
            {
                bool stoodOn = false;
                for (int y = cz * 32; y < cz * 32 + 32 && !stoodOn; y++)
                    for (int x = cx * 32; x < cx * 32 + 32 && !stoodOn; x++) stoodOn = !above.IsSolid(x, y);
                if (!stoodOn) continue;
                var (bx, bz) = UndergroundField.Below(cx * 32, cz * 32);
                Assert.False(map.IsSolid(bx, bz), $"rock under chunk {cx}, {cz}: {bx}, {bz}");
                places++;
            }
        Assert.True(places >= 100, $"{places} chunks of Sinnoh to dig down from");
        // The far corners of the tunnels, where the treasure hikers stand, are open
        Assert.False(map.IsSolid(471, 471));
        Assert.False(map.IsSolid(40, 72));
        // The base's room: its floor of twelve by twelve, and its mat
        for (int z = SecretBaseRules.FloorTop; z <= SecretBaseRules.FloorBottom; z++)
            for (int x = SecretBaseRules.FloorLeft; x <= SecretBaseRules.FloorRight; x++) Assert.Equal(TileType.CaveFloor, map.GetGroundTile(x, z));
        Assert.False(map.IsSolid(SecretBaseRules.ExitX, SecretBaseRules.ExitZ));
        Assert.True(UndergroundField.InABase(15, 20));
        Assert.True(UndergroundField.InABase(32, 100));
        Assert.False(UndergroundField.InABase(33, 65));
        // Every hiker's tile is floor, and each hiker on the map stands where the original's table has him, not where
        // the map's file does (the overlay's "at"). Those of a system nothing open lies over yet are held back
        Assert.All(UndergroundDatabase.Vendors, v => Assert.False(map.IsSolid(v.X, v.Z)));
        string[] prefixes = { "goods_vendor_", "traps_vendor_", "treasures_vendor_" };
        var counts = new int[3];
        int standing = 0;
        for (int i = 0; i < UndergroundDatabase.Vendors.Count; i++)
        {
            var vendor = UndergroundDatabase.Vendors[i];
            int kind = vendor.Kind == "Goods" ? 0 : vendor.Kind == "Traps" ? 1 : 2;
            string key = prefixes[kind] + ++counts[kind];
            if (map.Everyone.SingleOrDefault(n => n.Key == key) is not { } hiker) continue;
            Assert.Equal((vendor.X, vendor.Z), (hiker.GridX, hiker.GridY));
            Assert.Equal(i, UndergroundVendors.At(hiker.GridX, hiker.GridY));
            Assert.Equal("Hiker" + vendor.Kind, hiker.Script);
            standing++;
        }
        Assert.True(standing >= 17, $"{standing} hikers stand in the tunnels");
        Assert.Equal(standing, map.Everyone.Count(n => n.Key != null && n.Key.Contains("_vendor_")));
    }

    [Fact]
    public void ANewDaySpreadsWallsAndTrapsFromItsNumber()
    {
        var state = new UndergroundState { Seed = 20261011 };
        var field = Field(state);
        Assert.True(field.SpreadIfDue());
        Assert.False(state.SpreadAnew);
        Assert.False(field.SpreadIfDue());
        // Fourteen clusters of six to eleven walls each, and nought to five traps
        Assert.InRange(state.MiningSpots.Count, 14 * 6, 14 * 11);
        Assert.InRange(state.SpawnedTraps.Count, 1, Math.Min(UndergroundState.MaxSpawnedTraps, 14 * 5));
        Assert.All(state.MiningSpots, s =>
        {
            Assert.True(field.CanHoldTreasure(s.X, s.Z));
            Assert.True(Tunnel.IsSolid(s.X, s.Z));
            Assert.False(UndergroundField.InABase(s.X, s.Z));
        });
        Assert.All(state.SpawnedTraps, t =>
        {
            Assert.False(Tunnel.IsSolid(t.X, t.Z));
            Assert.Contains(UndergroundDatabase.Trap(t.Trap)!.Id, UndergroundField.SpreadTraps);
        });
        Assert.Equal(state.SpawnedTraps.Count, state.SpawnedTraps.Select(t => (t.X, t.Z)).Distinct().Count());

        // The same number spreads the same walls; the next day's another set
        var again = new UndergroundState { Seed = 20261011 };
        Field(again).SpreadIfDue();
        Assert.Equal(state.MiningSpots.Select(s => (s.X, s.Z)), again.MiningSpots.Select(s => (s.X, s.Z)));
        state.DaysPass(1);
        // Underground_HandleDailyEvents: the number moves on to ARNG(seed + days) = (seed + 1) × 1812433253 + 1
        Assert.Equal(unchecked((20261011u + 1u) * 1812433253u + 1u), state.Seed);
        Assert.True(state.SpreadAnew);
        var first = again.MiningSpots.Select(s => (s.X, s.Z)).ToList();
        field.SpreadIfDue();
        Assert.NotEqual(first, state.MiningSpots.Select(s => (s.X, s.Z)).ToList());
        // A wall that is dug at is gone from the map
        var spot = state.MiningSpots[0];
        int walls = state.MiningSpots.Count(s => s.X == spot.X && s.Z == spot.Z);
        Assert.True(field.StartMining(spot.X, spot.Z));
        Assert.Equal(walls - 1, state.MiningSpots.Count(s => s.X == spot.X && s.Z == spot.Z));
    }

    [Fact]
    public void ABuriedSphereGrowsEveryDayAndJoinsOneOfItsKind()
    {
        var state = new UndergroundState { Seed = 99 };
        var field = Field(state);
        var (x, z) = (40, 104);
        state.TryAddSphere(SphereKind.Red, 10);
        state.TryAddSphere(SphereKind.Red, 20);
        state.TryAddSphere(SphereKind.Blue, 5);
        // In a base, onto someone, into a wall: the original's refusals in its order
        Assert.Equal(BuryResult.InTheBase, field.Bury(0, 15, 20, false));
        Assert.Equal(BuryResult.SomeoneThere, field.Bury(0, x, z, true));
        var wall = Enumerable.Range(33, 400).Select(wx => (wx, 100)).First(t => Tunnel.IsSolid(t.wx, t.Item2));
        Assert.Equal(BuryResult.Wall, field.Bury(0, wall.wx, wall.Item2, false));
        Assert.Equal(BuryResult.Done, field.Bury(0, x, z, false));
        Assert.Equal(2, state.Spheres.Count);
        // A larger one of its kind onto it: the larger, a fifth of the smaller and one more: 20 + 10 / 5 + 1 = 23
        Assert.Equal(BuryResult.Done, field.Bury(0, x, z, false));
        Assert.Equal(23, field.SphereAt(x, z)!.Size);
        // Another kind is turned away
        Assert.Equal(BuryResult.SomethingThere, field.Bury(0, x, z, false));
        Assert.Single(state.Buried);

        // A Red Sphere grows 3 to 6 a day (MATH_Rand16(4) + 3), a Green one 5 to 9, a Prism or Pale one 1 or 2
        state.Buried.Add(new BuriedSphere { Kind = SphereKind.Green, X = 41, Z = 104, Size = 1 });
        state.Buried.Add(new BuriedSphere { Kind = SphereKind.Prism, X = 42, Z = 104, Size = 1 });
        state.DaysPass(1);
        Assert.InRange(state.Buried[0].Growth, 3, 6);
        Assert.InRange(state.Buried[1].Growth, 5, 9);
        Assert.InRange(state.Buried[2].Growth, 1, 2);
        // Three more days, each drawn afresh; a hundred and fifty count as ninety-nine, and growth stops at 99
        int red = state.Buried[0].Growth;
        state.DaysPass(3);
        Assert.InRange(state.Buried[0].Growth - red, 9, 18);
        state.DaysPass(150);
        Assert.Equal(99, state.Buried[1].Growth);
        // Dug up, it is worth what it was and what it grew, 99 at most
        var dug = field.DigUp(41, 104);
        Assert.NotNull(dug);
        Assert.Equal(99, state.Spheres[^1].Size);
        Assert.Null(field.SphereAt(41, 104));
        Assert.Null(field.DigUp(41, 104));
        // With forty Spheres in the bag a buried one stays where it is
        while (state.Spheres.Count < UndergroundState.MaxSpheres) state.TryAddSphere(SphereKind.Pale, 1);
        Assert.Null(field.DigUp(x, z));
        Assert.NotNull(field.SphereAt(x, z));
    }

    [Fact]
    public void ATrapIsSetTakenUpAndSteppedOn()
    {
        var state = new UndergroundState();
        var field = Field(state);
        for (int i = 0; i < 18; i++) state.TryAddTrap(Trap("TRAP_HOLE"));
        Assert.Equal(BuryResult.InTheBase, field.SetTrap(0, 15, 20, false));
        Assert.Equal(BuryResult.Done, field.SetTrap(0, 40, 104, false));
        Assert.Equal(BuryResult.SomethingThere, field.SetTrap(0, 40, 104, false));
        Assert.Equal(17, state.Traps.Count);
        // Taken up again with the button, it is back in the bag
        Assert.Equal(Trap("TRAP_HOLE"), field.Disarm(40, 104));
        Assert.Equal(18, state.Traps.Count);
        Assert.Equal(0, field.Disarm(40, 104));
        // Sixteen of one's own at most (MAX_PLACED_TRAPS)
        var floor = new List<(int X, int Z)>();
        for (int x = 34; x < 470 && floor.Count < 17; x++)
            if (!Tunnel.IsSolid(x, 104)) floor.Add((x, 104));
        for (int i = 0; i < 16; i++) Assert.Equal(BuryResult.Done, field.SetTrap(0, floor[i].X, floor[i].Z, false));
        Assert.Equal(BuryResult.NoRoom, field.SetTrap(0, floor[16].X, floor[16].Z, false));
        // Stepped on, it goes off and is gone, and counts in the records
        Assert.Equal(Trap("TRAP_HOLE"), field.StepOn(floor[0].X, floor[0].Z));
        Assert.Equal(0, field.StepOn(floor[0].X, floor[0].Z));
        Assert.Equal(1, state.Records.TrapsTriggered);
        // With a full bag a trap seen stays where it lies
        while (state.Traps.Count < UndergroundState.MaxTraps) state.TryAddTrap(Trap("TRAP_SMOKE"));
        Assert.Equal(-1, field.Disarm(floor[1].X, floor[1].Z));
        Assert.NotNull(field.TrapAt(floor[1].X, floor[1].Z));
    }

    [Fact]
    public void TheRadarsPulseFindsWhatLiesWithinSixSteps()
    {
        var state = new UndergroundState();
        var field = Field(state);
        state.MiningSpots.Add(new Spot(100, 100));
        state.MiningSpots.Add(new Spot(103, 103));
        state.MiningSpots.Add(new Spot(104, 103));
        state.SpawnedTraps.Add(new BuriedTrap { Trap = 1, X = 100, Z = 98 });
        state.Buried.Add(new BuriedSphere { Kind = SphereKind.Red, X = 99, Z = 100, Size = 1 });
        var pulse = field.Pulse(100, 100);
        // (103, 103) is six steps off and found; (104, 103) is seven and isn't. The nearest comes first
        Assert.Equal(new[] { (100, 100), (103, 103) }, pulse.Walls.Select(s => (s.X, s.Z)));
        Assert.Single(pulse.Traps);
        Assert.Single(pulse.Spheres);
        // Eight of a kind at most (MAX_TOUCH_RADAR_RESULTS_OF_TYPE)
        for (int i = 0; i < 12; i++) state.SpawnedTraps.Add(new BuriedTrap { Trap = 1, X = 100 + i % 4, Z = 100 + i / 4 });
        Assert.Equal(UndergroundField.RadarMost, field.Pulse(100, 100).Traps.Count);
    }

    // ------------------------------------------------------------------ traps at work

    private static TrapEffect Sprung(string id, int seed = 3) => new(Trap(id), new Random(seed));

    [Fact]
    public void AHoleIsClimbedOutOfInEightTurnsAndAPitInEighteen()
    {
        // CommPlayerMan_PutPlayerInHole(netID, 10) and 20, of which the last two are never asked for
        foreach (var (id, turns) in new[] { ("TRAP_HOLE", 8), ("TRAP_PIT", 18) })
        {
            var trap = Sprung(id);
            Assert.Equal(TrapFamily.Hole, trap.Family);
            Assert.False(trap.Walks);
            for (int i = 0; i < turns - 1; i++) trap.Advance(turned: true);
            Assert.False(trap.Done);
            // Standing there does nothing
            for (int i = 0; i < 100; i++) trap.Advance();
            Assert.False(trap.Done);
            trap.Advance(turned: true);
            Assert.True(trap.Done);
        }
    }

    [Fact]
    public void WrongSteeringLastsThirtySteps()
    {
        var reverse = Sprung("TRAP_REVERSE");
        Assert.True(reverse.Walks);
        Assert.Equal(Direction.Down, reverse.Steer(Direction.Up));
        Assert.Equal(Direction.Left, reverse.Steer(Direction.Right));
        // CommPlayerMan_SetPlayerAlteredMovement(netID, 30): thirty steps taken set it right
        for (int i = 0; i < 29; i++) reverse.Stepped();
        Assert.False(reverse.Done);
        reverse.Stepped();
        Assert.True(reverse.Done);
        Assert.Equal(Direction.Up, reverse.Steer(Direction.Up));
        // A Confuse Trap deals the four ways out afresh, never as they were
        for (int seed = 0; seed < 40; seed++)
        {
            var confuse = Sprung("TRAP_CONFUSE", seed);
            var dealt = new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right }.Select(confuse.Steer).ToList();
            Assert.Equal(4, dealt.Distinct().Count());
            Assert.NotEqual(new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right }, dealt);
        }
    }

    [Fact]
    public void AMoveTrapSlidesTwentyFiveTilesAndAHurlTrapFortyThreeUnlessSomethingStopsThem()
    {
        // CommPlayer_StartSlide: 27 tiles left for a Move Trap and 45 for a Hurl Trap; CommPlayer_MoveSlide moves while more than one is left
        foreach (var (id, way, tiles) in new[] { ("TRAP_MOVE_UP", Direction.Up, 25), ("TRAP_HURL_LEFT", Direction.Left, 43) })
        {
            var trap = Sprung(id);
            Assert.Equal((TrapFamily.Slide, way), (trap.Family, trap.Way));
            int moved = 0;
            while (trap.Slide(blockedAhead: false)) moved++;
            Assert.Equal(tiles, moved);
            Assert.True(trap.Done);
        }
        // Rock ahead ends it there
        var stopped = Sprung("TRAP_MOVE_DOWN");
        Assert.True(stopped.Slide(false));
        Assert.True(stopped.Slide(false));
        Assert.False(stopped.Slide(true));
        Assert.True(stopped.Done);
    }

    [Fact]
    public void ABoulderBreaksAfterElevenBlowsAndARockfallsAfterSeventeen()
    {
        // Traps_ProcessBoulder: the damage starts at 5 (Rock) or -1 (Rockfall) and the boulder breaks past 15
        foreach (var (id, blows) in new[] { ("TRAP_ROCK", 11), ("TRAP_ROCKFALL", 17) })
        {
            var trap = Sprung(id);
            for (int i = 0; i < blows - 1; i++) trap.Advance(tap: true);
            for (int i = 0; i < 30; i++) trap.Advance();
            Assert.False(trap.Done);
            trap.Advance(tap: true);
            Assert.True(trap.Done);
        }
    }

    [Fact]
    public void BubblesAreBurstOneATapSevenForFoamAndTwentyForABubbleTrap()
    {
        foreach (var (id, count) in new[] { ("TRAP_FOAM", 7), ("TRAP_BUBBLE", 20) })
        {
            var trap = Sprung(id);
            Assert.Equal(count, trap.Floaters.Count);
            // A Bubble Trap has one or two large bubbles (MATH_Rand32(2) + 1); foam none
            Assert.InRange(trap.Floaters.Count(f => f.Size == 0), id == "TRAP_BUBBLE" ? 1 : 0, id == "TRAP_BUBBLE" ? 2 : 0);
            for (int i = 0; i < count - 1; i++) trap.Advance(tap: true);
            Assert.False(trap.Done);
            // They drift, and stay on the screen
            for (int i = 0; i < 200; i++) trap.Advance();
            Assert.All(trap.Floaters.Where(f => !f.Gone), f => Assert.InRange(f.X, 0f, TrapEffect.ScreenWidth));
            trap.Advance(tap: true);
            Assert.True(trap.Done);
        }
    }

    [Fact]
    public void AFlameIsBlownOutWithOneLongBreath()
    {
        // Traps_ProcessFlame: a Fire Trap's flame is 4 high and an Ember Trap's 2; a breath wears one off at its 9th, 19th... frame
        foreach (var (id, frames) in new[] { ("TRAP_EMBER", 19), ("TRAP_FIRE", 39) })
        {
            var trap = Sprung(id);
            for (int i = 0; i < frames - 1; i++) trap.Advance(held: true);
            Assert.False(trap.Done);
            // A pause, and it is as high as ever
            trap.Advance();
            Assert.Equal(id == "TRAP_FIRE" ? 4 : 2, trap.Flame);
            for (int i = 0; i < frames - 1; i++) trap.Advance(held: true);
            Assert.False(trap.Done);
            trap.Advance(held: true);
            Assert.True(trap.Done);
        }
    }

    [Fact]
    public void LeavesAreBlownOffTheScreenAndSmokeWipedAway()
    {
        var leaves = Sprung("TRAP_LEAF");
        var flowers = Sprung("TRAP_FLOWER");
        // Sixteen leaves, thirty-two for a Flower Trap, which a breath moves less (the reading over 33 against over 25)
        Assert.Equal((16, 32), (leaves.Floaters.Count, flowers.Floaters.Count));
        for (int i = 0; i < 100; i++) leaves.Advance();
        Assert.False(leaves.Done);
        int frames = 0;
        while (!leaves.Done && frames++ < 400) leaves.Advance(held: true);
        Assert.True(leaves.Done);
        // 127 / 25 = 5 dots a frame: off a screen of 256 from its middle in 26 frames at most
        Assert.InRange(frames, 1, 27);

        var smoke = Sprung("TRAP_BIG_SMOKE");
        Assert.Equal(TrapEffect.SmokeColumns * TrapEffect.SmokeRows, smoke.SmokeLeft);
        // Traps_ClearSmoke: a touch thins its tile by three, the four beside it by two and the corners by one, and past five a tile is clear
        smoke.Wipe(4, 4);
        Assert.Equal((4, 3, 3, 2, 1), (smoke.Smoke[4, 4], smoke.Smoke[4, 5], smoke.Smoke[3, 4], smoke.Smoke[5, 5], smoke.Smoke[4, 6]));
        smoke.Wipe(4, 4);
        // 4 + 3 is past five; 3 + 2 is not yet
        Assert.Equal((0, 5), (smoke.Smoke[4, 4], smoke.Smoke[4, 5]));
        smoke.Wipe(4, 4);
        Assert.Equal(0, smoke.Smoke[4, 5]);
        // The keys' hand stands in for the stylus's scribble: five touches one over another (HandReach), so a step of
        // it clears the five tiles of its own column (1 + 3 + 2 at the least) and leaves the column beside it at 5
        int hx = smoke.HandX + 1, hy = smoke.HandY;
        smoke.Advance(dx: 1);
        Assert.Equal((hx, hy), (smoke.HandX, smoke.HandY));
        for (int k = -TrapEffect.HandReach; k <= TrapEffect.HandReach; k++) Assert.Equal(0, smoke.Smoke[hy + k, hx]);
        Assert.Equal(5, smoke.Smoke[hy, hx + 1]);
        Assert.Equal(3, smoke.Smoke[hy + 3, hx]);
        // Sweeping the whole screen clears it: fewer than thirteen tiles left is clear enough
        for (int pass = 0; pass < 6 && !smoke.Done; pass++)
            for (int y = 0; y < TrapEffect.SmokeRows && !smoke.Done; y++)
            {
                for (int x = 0; x < TrapEffect.SmokeColumns && !smoke.Done; x++) smoke.Advance(dx: y % 2 == 0 ? 1 : -1);
                smoke.Advance(dy: 1);
                if (smoke.HandY == TrapEffect.SmokeRows - 1 && y == TrapEffect.SmokeRows - 1)
                    for (int up = 0; up < TrapEffect.SmokeRows; up++) smoke.Advance(dy: -1);
            }
        Assert.True(smoke.Done);
        Assert.True(smoke.SmokeLeft < 13);
        // An Alert Trap only calls out, and is over in a moment
        var alert = Sprung("TRAP_ALERT_2");
        Assert.NotNull(alert.Call);
        for (int i = 0; i < 39; i++) alert.Advance();
        Assert.True(alert.Done);
        // Every trap the day spreads has a family of its own
        Assert.All(UndergroundField.SpreadTraps, id => Assert.NotEqual(TrapFamily.None, TrapEffect.FamilyOf(Trap(id))));
    }

    // ------------------------------------------------------------------ the hikers

    [Fact]
    public void AHikersStockIsTheDaysAndHisOwn()
    {
        const uint seed = 123456;
        for (int vendor = 0; vendor < 16; vendor++)
        {
            var stock = UndergroundVendors.Stock(seed, vendor, national: false);
            Assert.Equal(UndergroundVendors.StockSize, stock.Count);
            Assert.Equal(5, stock.Select(o => o.Thing).Distinct().Count());
            bool traps = UndergroundVendors.KindOf(vendor) == "Traps";
            // A trap hiker's first ware is always the Digger Drill
            if (traps) Assert.Equal(Trap("TRAP_DIGGER_DRILL"), stock[0].Thing);
            foreach (var offer in stock)
            {
                var price = traps ? UndergroundDatabase.Trap(offer.Thing)!.Price : UndergroundDatabase.Good(offer.Thing)!.Price;
                Assert.True(price.ForTrade);
                if (!price.Any) Assert.Equal(price.Kind, offer.Kind);
                Assert.NotEqual(SphereKind.None, offer.Kind);
                // MATH_Rand16(max - min) + min: from the least up to one short of the most
                Assert.InRange(offer.Size, price.Min, Math.Max(price.Min, price.Max - 1));
                if (!traps) Assert.False(UndergroundDatabase.Good(offer.Thing)!.National);
            }
            // The same day, the same wares; another day, others
            Assert.Equal(stock, UndergroundVendors.Stock(seed, vendor, false));
        }
        Assert.NotEqual(UndergroundVendors.Stock(seed, 0, false), UndergroundVendors.Stock(seed + 1, 0, false));
        Assert.Empty(UndergroundVendors.Stock(seed, 16, false));
        Assert.Equal(0, UndergroundVendors.At(72, 104));
        Assert.Equal(19, UndergroundVendors.At(40, 72));
        Assert.Equal(-1, UndergroundVendors.At(1, 1));
    }

    [Fact]
    public void AHikerTakesASphereOfTheRightKindAndSizeAndGivesHalfForWhatHeTradesIn()
    {
        const uint seed = 777;
        int vendor = 8;
        var stock = UndergroundVendors.Stock(seed, vendor, false);
        var drill = stock[0];
        var state = new UndergroundState { Seed = seed };
        var wrong = drill.Kind == SphereKind.Red ? SphereKind.Blue : SphereKind.Red;
        state.TryAddSphere(wrong, 99);
        state.TryAddSphere(drill.Kind, drill.Size - 1);
        state.TryAddSphere(drill.Kind, drill.Size);
        Assert.Equal(TradeResult.WrongSphere, UndergroundVendors.Buy(state, vendor, drill, 0));
        Assert.Equal(TradeResult.SphereTooSmall, UndergroundVendors.Buy(state, vendor, drill, 1));
        Assert.Equal(TradeResult.Done, UndergroundVendors.Buy(state, vendor, drill, 2));
        Assert.Equal(new[] { Trap("TRAP_DIGGER_DRILL") }, state.Traps);
        Assert.Equal(2, state.Spheres.Count);
        // With forty traps there is no room, and the Sphere stays
        while (state.Traps.Count < UndergroundState.MaxTraps) state.TryAddTrap(Trap("TRAP_HOLE"));
        state.TryAddSphere(drill.Kind, 99);
        Assert.Equal(TradeResult.BagFull, UndergroundVendors.Buy(state, vendor, drill, 2));
        Assert.Equal(3, state.Spheres.Count);

        // He gives a Sphere for a trap: of the trap's kind, half the size he would ask (a Pit Trap: Prism, 12 to 14, so 6 or 7)
        var worth = UndergroundVendors.Worth(seed, vendor, Trap("TRAP_PIT"))!;
        Assert.Equal(SphereKind.Prism, worth.Kind);
        Assert.InRange(worth.Size, 6, 7);
        Assert.Null(UndergroundVendors.Worth(seed, vendor, Trap("TRAP_WARP_DUMMY")));
        state.Traps[0] = Trap("TRAP_PIT");
        state.Spheres.Clear();
        Assert.Equal(TradeResult.Done, UndergroundVendors.Sell(state, vendor, 0));
        Assert.Equal((SphereKind.Prism, worth.Size), (state.Spheres[0].Kind, state.Spheres[0].Size));
        // A treasure hiker gives the whole of it: a Heart Scale for a Red Sphere of 5 to 9
        state.TryAddTreasure(Treasure("TREASURE_HEART_SCALE"));
        var scale = UndergroundVendors.Worth(seed, 16, Treasure("TREASURE_HEART_SCALE"))!;
        Assert.Equal(SphereKind.Red, scale.Kind);
        Assert.InRange(scale.Size, 5, 9);
        Assert.Equal(TradeResult.Done, UndergroundVendors.Sell(state, 16, 0));
        Assert.Empty(state.Treasures);
    }

    // ------------------------------------------------------------------ the secret base

    [Fact]
    public void ANewBaseHasSixteenBouldersClearOfTheWayToThePC()
    {
        for (ulong seed = 1; seed <= 40; seed++)
        {
            var made = SecretBaseRules.Create(200, 150, Direction.Up, new Rand32(seed * 53));
            Assert.Equal(Direction.Down, made.EntranceFacing);
            Assert.Equal(SecretBaseRules.MaxBoulders, made.Boulders.Count);
            Assert.Equal(16, made.Boulders.Select(b => (b.X, b.Z)).Distinct().Count());
            // SecretBases_CreateBase: columns 10, 12, 16, 18 and 20, rows 12 to 22 by twos; never the place beside the PC
            Assert.All(made.Boulders, b =>
            {
                Assert.Contains(b.X, new[] { 10, 12, 16, 18, 20 });
                Assert.Contains(b.Z, new[] { 12, 14, 16, 18, 20, 22 });
                Assert.NotEqual((16, 12), (b.X, b.Z));
            });
            // So the two columns from the mat to the PC are clear, and the PC's own two tiles are shut
            var blocked = SecretBaseRules.BlockedTiles(made);
            for (int z = SecretBaseRules.PcZ + 1; z <= SecretBaseRules.FloorBottom; z++)
            {
                Assert.DoesNotContain((14, z), blocked);
                Assert.DoesNotContain((15, z), blocked);
            }
            Assert.Contains((15, 12), blocked);
            Assert.Contains((16, 12), blocked);
            Assert.Equal(2 + 16 * 4, blocked.Count);
        }
    }

    [Fact]
    public void FlagsBroughtHomeEarnABetterFlagFewerBouldersAndMoreGoods()
    {
        // FLAG_CAPTURED_COUNT: 1 bronze, 3 silver, 10 gold, 50 platinum
        Assert.Equal(new[] { BaseFlag.Normal, BaseFlag.Bronze, BaseFlag.Bronze, BaseFlag.Silver, BaseFlag.Silver, BaseFlag.Gold, BaseFlag.Gold, BaseFlag.Platinum },
            new[] { 0, 1, 2, 3, 9, 10, 49, 50 }.Select(SecretBaseRules.FlagFor));
        // SecretBase_GetUnremovableBoulderCount and GetPlacedGoodsLimit
        Assert.Equal(new[] { 16, 15, 11, 6, 0 }, new[] { 0, 1, 3, 10, 50 }.Select(SecretBaseRules.BouldersThatStay));
        Assert.Equal(new[] { 10, 12, 15, 15, 15 }, new[] { 0, 1, 3, 10, 50 }.Select(SecretBaseRules.GoodsLimit));
        Assert.Equal("UG_GOOD_PC_GOLD_FLAG", UndergroundDatabase.Good(SecretBaseRules.PcGood(10))!.Id);
        // A tool of the base is the trap of its name
        Assert.Equal(Trap("TRAP_BIG_SMOKE"), SecretBaseRules.ToolTrap(Good("UG_GOOD_BIG_SMOKE_TOOL")));
        Assert.Equal(Trap("TRAP_ALERT_3"), SecretBaseRules.ToolTrap(Good("UG_GOOD_ALERT_TOOL_3")));
        Assert.Equal(0, SecretBaseRules.ToolTrap(Good("UG_GOOD_PLAIN_TABLE")));
        // What is written on a good is the records'
        var records = new UndergroundRecords { FossilsDug = 4 };
        Assert.Contains("4", SecretBaseRules.RecordLine(Good("UG_GOOD_BIG_BOOKSHELF"), records));
        Assert.Null(SecretBaseRules.RecordLine(Good("UG_GOOD_WOODEN_CHAIR"), records));
    }

    [Fact]
    public void TheDrillOpensABaseOnlyInSolidRockOfTheTunnels()
    {
        bool Rock(int x, int z) => !Tunnel.InBounds(x, z) || Tunnel.IsSolid(x, z);
        // Looking up at a wall three tiles wide: found somewhere along a tunnel's north side
        (int X, int Z)? good = null, thin = null;
        for (int z = 66; z < 470 && (good == null || thin == null); z++)
            for (int x = 34; x < 470; x++)
            {
                if (Rock(x, z + 1) || !Rock(x, z)) continue;
                if (Rock(x - 1, z) && Rock(x + 1, z)) good ??= (x, z);
                else thin ??= (x, z);
            }
        Assert.NotNull(good);
        Assert.True(SecretBaseRules.CanDrill(Rock, good!.Value.X, good.Value.Z, Direction.Up));
        // Rock with open floor beside it across the way faced won't do, nor floor, nor a base's own walls
        if (thin != null) Assert.False(SecretBaseRules.CanDrill(Rock, thin.Value.X, thin.Value.Z, Direction.Up));
        Assert.False(SecretBaseRules.CanDrill(Rock, good.Value.X, good.Value.Z + 1, Direction.Up));
        Assert.False(SecretBaseRules.CanDrill(Rock, 5, 5, Direction.Up));
    }

    /// <summary>
    /// The player's base as the decorating screen is given it (<see cref="SecretBaseRules.Decorating"/>), with sixteen
    /// boulders in the two columns on the left and down the right-hand side, so the middle of the floor is clear.
    /// </summary>
    private static (DecoratingSession Session, DecorationRoom Room, UndergroundState State) Decorating(int flags, params string[] goods)
    {
        var state = new UndergroundState { Base = new SavedBase { EntranceX = 200, EntranceZ = 150, EntranceFacing = Direction.Down } };
        foreach (int x in new[] { 10, 12 })
            for (int z = 12; z <= 22; z += 2) state.Base.Boulders.Add(new Spot(x, z));
        for (int z = 12; z <= 18; z += 2) state.Base.Boulders.Add(new Spot(20, z));
        state.Records.FlagsCaptured = flags;
        foreach (string good in goods) state.TryAddPcGood(Good(good));
        var session = SecretBaseRules.Decorating(state);
        return (session, session.Room, state);
    }

    [Fact]
    public void AGoodIsCarriedOverTheFloorAndSetDownWhereNothingStands()
    {
        var (session, room, state) = Decorating(0, "UG_GOOD_PLAIN_TABLE", "UG_GOOD_WOODEN_CHAIR");
        var store = session.Store;
        // The floor of twelve by twelve, the PC and the sixteen boulders as fixtures, ten goods at most with no flag
        Assert.Equal((10, 12, 12, 12, 10), (room.Left, room.Top, room.Width, room.Height, room.Limit));
        Assert.Equal(17, room.Pieces.Count(p => p.IsFixture));
        Assert.Equal(DecoratingStep.Menu, session.Step);
        session.Confirm();
        Assert.Equal(DecoratingStep.Choosing, session.Step);
        session.Confirm();
        // The Plain Table, two by two, hangs at (16, 13), where the tile before the PC is kept clear (BaseDecoration_CanPlaceGood)
        Assert.Equal(DecoratingStep.Placing, session.Step);
        Assert.Equal((16, 13), (session.X, session.Y));
        Assert.False(session.Fits);
        session.Confirm();
        Assert.Equal(DecoratingStep.Placing, session.Step);
        Assert.NotNull(session.Message);
        Assert.True(session.Refused);
        // A step right and it stands clear
        session.Move(1, 0);
        Assert.True(session.Fits);
        // It can't be carried off the floor: twelve tiles wide, and the table two
        for (int i = 0; i < 20; i++) session.Move(1, 0);
        Assert.Equal(20, session.X);
        // Nor set down on a boulder
        Assert.False(session.Fits);
        session.Move(-1, 0);
        session.Move(-1, 0);
        session.Move(0, 1);
        session.Move(0, 1);
        Assert.Equal((18, 15), (session.X, session.Y));
        Assert.True(session.Fits);
        session.Confirm();
        Assert.Equal(DecoratingStep.Menu, session.Step);
        Assert.True(store[0].Out);
        Assert.Equal(1, room.Out);
        Assert.True(session.Changed);
        Assert.False(session.Refused);
        var table = room.Pieces.Single(p => !p.IsFixture);
        Assert.Equal((18, 15, 0), (table.X, table.Y, table.Slot));
        // What stands in the room is the base's once the decorating is kept (BaseDecoration_SaveBaseGoods)
        SecretBaseRules.Keep(state.Base!, room);
        Assert.Equal((Good("UG_GOOD_PLAIN_TABLE"), 18, 15, 0), (state.Base!.Goods[0].Good, state.Base.Goods[0].X, state.Base.Goods[0].Z, state.Base.Goods[0].Slot));
        Assert.True(state.IsDisplayed(0));
        Assert.Equal(16, state.Base.Boulders.Count);

        // The same good can't be put out twice
        session.Confirm();
        session.Confirm();
        Assert.Equal(DecoratingStep.Choosing, session.Step);
        Assert.NotNull(session.Message);
        // Backing out of carrying one puts nothing down
        session.Move(0, 1);
        session.Confirm();
        Assert.Equal(DecoratingStep.Placing, session.Step);
        session.Cancel();
        Assert.Equal(DecoratingStep.Choosing, session.Step);
        Assert.False(store[1].Out);
        session.Cancel();
        Assert.Equal(DecoratingStep.Menu, session.Step);

        // MOVE: picked up and set down elsewhere, or put back where it stood
        session.Move(0, 1);
        session.Move(0, 1);
        session.Confirm();
        Assert.Equal(DecoratingStep.Picking, session.Step);
        while (session.X < 18) session.Move(1, 0);
        while (session.Y < 15) session.Move(0, 1);
        Assert.Same(table, session.Under);
        session.Confirm();
        Assert.Equal(DecoratingStep.Moving, session.Step);
        session.Move(0, 1);
        session.Cancel();
        Assert.Equal((18, 15), (table.X, table.Y));
        session.Confirm();
        session.Move(-1, 0);
        session.Confirm();
        Assert.Equal(DecoratingStep.Picking, session.Step);
        Assert.Equal((17, 15), (table.X, table.Y));
        // The PC and the boulders stay where they are (the original's "can't be moved")
        while (session.X > 15) session.Move(-1, 0);
        while (session.Y > 12) session.Move(0, -1);
        session.Confirm();
        Assert.Equal(DecoratingStep.Picking, session.Step);
        Assert.NotNull(session.Message);
        while (session.X > 12) session.Move(-1, 0);
        session.Confirm();
        Assert.Equal(DecoratingStep.Picking, session.Step);
        Assert.NotNull(session.Message);
        session.Cancel();

        // PUT AWAY: the good goes back into the store; the PC doesn't
        session.Move(0, -1);
        session.Confirm();
        Assert.Equal(DecoratingStep.PuttingAway, session.Step);
        while (session.X < 15) session.Move(1, 0);
        session.Confirm();
        Assert.NotNull(session.Message);
        Assert.Equal(1, room.Out);
        while (session.X < 17) session.Move(1, 0);
        while (session.Y < 15) session.Move(0, 1);
        session.Confirm();
        Assert.False(store[0].Out);
        Assert.Equal(0, room.Out);
        session.Cancel();
        session.Cancel();
        Assert.Equal(DecoratingStep.Done, session.Step);
        SecretBaseRules.Keep(state.Base, room);
        Assert.Empty(state.Base.Goods);
        Assert.False(state.IsDisplayed(0));
    }

    [Fact]
    public void OnlyAsManyGoodsAsTheFlagAllowsAndBouldersGoWhenTheFlagLetsThem()
    {
        // Ten goods with no flag (SecretBase_GetPlacedGoodsLimit): the eleventh is turned away at the menu
        var chairs = Enumerable.Repeat("UG_GOOD_WOODEN_CHAIR", 11).ToArray();
        var (session, room, _) = Decorating(0, chairs);
        for (int i = 0; i < 10; i++)
        {
            session.Confirm();
            Assert.Equal(DecoratingStep.Choosing, session.Step);
            while (session.StoreCursor < i) session.Move(0, 1);
            session.Confirm();
            // Two rows of five in the middle of the floor
            (int x, int y) = (14 + i % 5, 18 + i / 5);
            while (session.Y < y) session.Move(0, 1);
            while (session.X > x) session.Move(-1, 0);
            while (session.X < x) session.Move(1, 0);
            Assert.True(session.Fits, $"chair {i} at {session.X}, {session.Y}");
            session.Confirm();
            Assert.Equal(DecoratingStep.Menu, session.Step);
        }
        Assert.Equal(10, room.Out);
        session.Confirm();
        Assert.Equal(DecoratingStep.Menu, session.Step);
        Assert.NotNull(session.Message);
        // With no flag brought home every boulder stays (SecretBase_GetUnremovableBoulderCount: 16)
        session.Move(0, 1);
        session.Confirm();
        while (session.X > 10) session.Move(-1, 0);
        while (session.Y > 14) session.Move(0, -1);
        session.Confirm();
        Assert.Equal(DecoratingStep.PuttingAway, session.Step);
        Assert.NotNull(session.Message);
        Assert.Equal(16, room.Pieces.Count(p => p.Kind == SecretBaseRules.BoulderGood));
        Assert.Equal(10, session.Store.Count(s => s.Out));

        // With the bronze flag twelve goods and fifteen boulders: one may go, for good, and no second
        var (bronze, bronzeRoom, state) = Decorating(1);
        Assert.Equal(12, bronzeRoom.Limit);
        // With nothing in the PC there is nothing to put out
        bronze.Confirm();
        Assert.Equal(DecoratingStep.Menu, bronze.Step);
        Assert.NotNull(bronze.Message);
        bronze.Move(0, 1);
        bronze.Confirm();
        while (bronze.X > 10) bronze.Move(-1, 0);
        while (bronze.Y < 14) bronze.Move(0, 1);
        bronze.Confirm();
        Assert.Equal(DecoratingStep.Menu, bronze.Step);
        Assert.Equal(15, bronzeRoom.Pieces.Count(p => p.Kind == SecretBaseRules.BoulderGood));
        Assert.Null(bronzeRoom.PieceAt(10, 14));
        bronze.Confirm();
        while (bronze.Y < 16) bronze.Move(0, 1);
        bronze.Confirm();
        Assert.Equal(DecoratingStep.PuttingAway, bronze.Step);
        Assert.NotNull(bronze.Message);
        Assert.Equal(15, bronzeRoom.Pieces.Count(p => p.Kind == SecretBaseRules.BoulderGood));
        SecretBaseRules.Keep(state.Base!, bronzeRoom);
        Assert.Equal(15, state.Base!.Boulders.Count);
        Assert.DoesNotContain(state.Base.Boulders, b => (b.X, b.Z) == (10, 14));

        // With the silver flag eleven stay: five may go, and the room says how many more
        var (silver, silverRoom, _) = Decorating(3);
        Assert.Equal(15, silverRoom.Limit);
        silver.Move(0, 1);
        silver.Confirm();
        while (silver.X > 10) silver.Move(-1, 0);
        while (silver.Y < 14) silver.Move(0, 1);
        silver.Confirm();
        Assert.Contains("4", silver.Message);
    }

    [Fact]
    public void TheBasesGoodsAreKeptByTheirPlaceInThePC()
    {
        var state = new UndergroundState { Base = new SavedBase() };
        foreach (string good in new[] { "UG_GOOD_PLAIN_TABLE", "UG_GOOD_WOODEN_CHAIR", "UG_GOOD_BIG_TABLE" }) state.TryAddPcGood(Good(good));
        state.Base.Goods.Add(new PlacedGood { Good = Good("UG_GOOD_BIG_TABLE"), X = 12, Z = 16, Slot = 2 });
        Assert.True(state.IsDisplayed(2));
        // A good on display stays in the PC (Underground_RemoveGoodAtSlotPC asserts it); one before it taken out moves its place up
        Assert.False(state.Withdraw(2));
        Assert.True(state.Withdraw(0));
        Assert.Equal(1, state.Base.Goods[0].Slot);
        Assert.Equal(new[] { Good("UG_GOOD_PLAIN_TABLE") }, state.Goods);
        Assert.True(state.Deposit(0));
        Assert.Equal(3, state.PcGoods.Count);
        Assert.Empty(state.Goods);
        // What the base's good blocks: the Big Table's nine tiles
        Assert.Equal(9, SecretBaseRules.Blocked(Good("UG_GOOD_BIG_TABLE"), 12, 16).Count());
        Assert.Same(state.Base.Goods[0], SecretBaseRules.GoodBlocking(state.Base, 14, 18));
        Assert.Same(state.Base.Goods[0], SecretBaseRules.GoodStandingAt(state.Base, 12, 16));
        Assert.Null(SecretBaseRules.GoodStandingAt(state.Base, 13, 16));
        // The PC holds two hundred goods and the bag forty of each thing
        while (state.TryAddPcGood(Good("UG_GOOD_WOODEN_CHAIR"))) { }
        Assert.Equal(UndergroundState.MaxPcGoods, state.PcGoods.Count);
        while (state.TryAddTreasure(Treasure("TREASURE_REVIVE"))) { }
        Assert.Equal(UndergroundState.MaxTreasures, state.Treasures.Count);
        Assert.False(state.TryAddTreasure(1));
    }

    // ------------------------------------------------------------------ other diggers and their flags

    [Fact]
    public void TheDaysDiggersHaveBasesInTheTunnelsAndAFlagEach()
    {
        bool Rock(int x, int z) => !Tunnel.InBounds(x, z) || Tunnel.IsSolid(x, z);
        var today = Diggers.Today(4711, Rock);
        Assert.InRange(today.Count, 1, Diggers.Count);
        Assert.Equal(today.Count, today.Select(d => d.Name).Distinct().Count());
        Assert.All(today, d =>
        {
            // A way in drilled looking north: solid rock, with the tunnel's floor to the south of it
            Assert.True(SecretBaseRules.CanDrill(Rock, d.Base.EntranceX, d.Base.EntranceZ, Direction.Up));
            Assert.False(Rock(d.Base.EntranceX, d.Base.EntranceZ + 1));
            Assert.Equal(Direction.Down, d.Base.EntranceFacing);
            // Their goods stand clear of the PC, of the boulders and of the way in
            var boulders = d.Base.Boulders.SelectMany(b => SecretBaseRules.Blocked(SecretBaseRules.BoulderGood, b.X, b.Z)).ToHashSet();
            foreach (var good in d.Base.Goods)
            {
                var record = UndergroundDatabase.Good(good.Good)!;
                for (int x = good.X; x < good.X + record.Width; x++)
                    for (int z = good.Z; z < good.Z + record.Depth; z++)
                    {
                        Assert.DoesNotContain((x, z), boulders);
                        Assert.NotEqual(SecretBaseRules.ExitX, x);
                        Assert.InRange(z, SecretBaseRules.PcZ + 1, SecretBaseRules.FloorBottom);
                    }
            }
        });
        Assert.Equal(today.Select(d => (d.Name, d.Base.EntranceX)), Diggers.Today(4711, Rock).Select(d => (d.Name, d.Base.EntranceX)));

        // One flag is carried at a time, and each digger gives one a day
        var state = new UndergroundState { Seed = 4711 };
        Assert.True(Diggers.Take(state, today[0].Number));
        Assert.False(Diggers.CanTake(state, today[^1].Number));
        Assert.Null(Diggers.BringHome(new UndergroundState(), "Nobody"));
        // The first flag home earns the bronze flag; the second nothing new; the third the silver
        Assert.Equal(BaseFlag.Bronze, Diggers.BringHome(state, today[0].Name));
        Assert.Equal(1, state.Records.FlagsCaptured);
        Assert.False(Diggers.Take(state, today[0].Number));
        state.DaysPass(1);
        Assert.True(Diggers.Take(state, today[0].Number));
        Assert.Null(Diggers.BringHome(state, "Odette"));
        state.DaysPass(1);
        Diggers.Take(state, 0);
        Assert.Equal(BaseFlag.Silver, Diggers.BringHome(state, "Corwin"));
        // The last five owners are kept (MAX_CAPTURED_FLAG_RECORDS), the newest first
        for (int i = 0; i < 5; i++)
        {
            state.HeldFlag = 1;
            Diggers.BringHome(state, $"Digger {i}");
        }
        Assert.Equal(new[] { "Digger 4", "Digger 3", "Digger 2", "Digger 1", "Digger 0" }, state.FlagOwners);
    }

    // ------------------------------------------------------------------ the save and the day

    [Fact]
    public void TheUndergroundIsSavedWhole()
    {
        // A new game's number is a step on from one the game has already: 5 × 1812433253 + 1
        var state = UndergroundState.NewGame(5u);
        Assert.Equal(unchecked(5u * 1812433253u + 1u), state.Seed);
        state.TryAddSphere(SphereKind.Green, 31);
        state.TryAddTreasure(Treasure("TREASURE_SKULL_FOSSIL"));
        state.TryAddTrap(Trap("TRAP_DIGGER_DRILL"));
        state.TryAddGood(Good("UG_GOOD_GLOBE"));
        state.TryAddPcGood(Good("UG_GOOD_PLAIN_TABLE"));
        state.Buried.Add(new BuriedSphere { Kind = SphereKind.Pale, X = 300, Z = 301, Size = 7, Growth = 3 });
        state.MiningSpots.Add(new Spot(123, 234));
        state.PlacedTraps.Add(new BuriedTrap { Trap = Trap("TRAP_FIRE"), X = 77, Z = 88 });
        state.Base = SecretBaseRules.Create(200, 150, Direction.Left, new Rand32(9));
        state.Base.Goods.Add(new PlacedGood { Good = Good("UG_GOOD_PLAIN_TABLE"), X = 14, Z = 16, Slot = 0 });
        state.Records.FossilsDug = 12;
        state.Records.FlagsCaptured = 3;
        state.HasMined = true;
        state.SpreadAnew = false;
        state.NotePlate(Treasure("TREASURE_IRON_PLATE"));
        state.Surface = new SurfaceSpot { Map = "Sinnoh", X = 304, Y = 528, Facing = Direction.Left };
        state.HeldFlag = 2;
        state.Visiting = 4;
        state.FlagsTakenToday.Add(2);
        state.FlagOwners.Add("Ilse");

        var save = new PokemonPlatinumEngine.Core.SaveData { Underground = state };
        var back = GameDataFiles.Deserialize<PokemonPlatinumEngine.Core.SaveData>(GameDataFiles.Serialize(save)).Underground!;
        Assert.Equal(state.Seed, back.Seed);
        Assert.Equal((SphereKind.Green, 31), (back.Spheres[0].Kind, back.Spheres[0].Size));
        Assert.Equal(state.Treasures, back.Treasures);
        Assert.Equal(state.Traps, back.Traps);
        Assert.Equal(state.Goods, back.Goods);
        Assert.Equal(state.PcGoods, back.PcGoods);
        Assert.Equal((SphereKind.Pale, 300, 301, 7, 3), (back.Buried[0].Kind, back.Buried[0].X, back.Buried[0].Z, back.Buried[0].Size, back.Buried[0].Growth));
        Assert.Equal((123, 234), (back.MiningSpots[0].X, back.MiningSpots[0].Z));
        Assert.Equal((Trap("TRAP_FIRE"), 77, 88), (back.PlacedTraps[0].Trap, back.PlacedTraps[0].X, back.PlacedTraps[0].Z));
        Assert.Equal((200, 150, Direction.Right, 16), (back.Base!.EntranceX, back.Base.EntranceZ, back.Base.EntranceFacing, back.Base.Boulders.Count));
        Assert.Equal((14, 16, 0), (back.Base.Goods[0].X, back.Base.Goods[0].Z, back.Base.Goods[0].Slot));
        Assert.Equal((12, 3), (back.Records.FossilsDug, back.Records.FlagsCaptured));
        Assert.True(back.HasMined);
        Assert.False(back.SpreadAnew);
        Assert.False(back.NeverMined(Treasure("TREASURE_IRON_PLATE")));
        Assert.Equal(("Sinnoh", 304, 528, Direction.Left), (back.Surface!.Map, back.Surface.X, back.Surface.Y, back.Surface.Facing));
        Assert.Equal(2, back.HeldFlag);
        Assert.Equal(4, back.Visiting);
        Assert.Equal(new[] { "Ilse" }, back.FlagOwners);
        // A save from before the Underground has none, and is given a new one
        Assert.Null(GameDataFiles.Deserialize<PokemonPlatinumEngine.Core.SaveData>("{}").Underground);
        Assert.True(new UndergroundState().SpreadAnew);
    }

    [Fact]
    public void ANewDayIsTheUndergroundsFirst()
    {
        var story = new StoryState();
        var encounters = SpecialEncounters.NewGame(new Random(1));
        var state = new UndergroundState { Seed = 1000, SpreadAnew = false };
        state.FlagsTakenToday.Add(3);
        DailyEvents.DaysPass(2, story, encounters, "underground", new Random(2), state);
        // Underground_HandleDailyEvents: ARNG(seed + days)
        Assert.Equal(unchecked(1002u * 1812433253u + 1u), state.Seed);
        Assert.True(state.SpreadAnew);
        Assert.Empty(state.FlagsTakenToday);
        // No day gone by, nothing moves
        DailyEvents.DaysPass(0, story, encounters, null, new Random(2), state);
        Assert.Equal(unchecked(1002u * 1812433253u + 1u), state.Seed);
        // The steps below count to a hundred and start again (Underground_IncrementStepCount)
        for (int i = 0; i < 99; i++) state.Step();
        Assert.Equal(99, state.Steps);
        state.Step();
        Assert.Equal(0, state.Steps);
    }

    // ------------------------------------------------------------------ the lists

    [Fact]
    public void TheBagBelowGroundPutsATreasureInTheBagAndAsksForWhatHappensInTheTunnels()
    {
        var state = new UndergroundState();
        state.TryAddTrap(Trap("TRAP_DIGGER_DRILL"));
        state.TryAddTrap(Trap("TRAP_HOLE"));
        state.TryAddSphere(SphereKind.Red, 8);
        state.TryAddTreasure(Treasure("TREASURE_SKULL_FOSSIL"));
        var bag = new List<string>();
        var screen = new UndergroundScreen();
        screen.OpenBag(state, item =>
        {
            bag.Add(item);
            return true;
        });
        Assert.Equal(new[] { "TRAPS", "SPHERES", "GOODS", "TREASURES", "SURFACE" }, screen.Tabs);
        Assert.Equal(new[] { "Digger Drill", "Hole Trap" }, screen.Rows.Select(r => r.Name));
        // The drill is USEd; any other trap SET
        screen.Confirm();
        Assert.Equal(new[] { "USE", "TRASH", "CANCEL" }, screen.Actions);
        screen.Cancel();
        screen.Move(0, 1);
        screen.Confirm();
        Assert.Equal("SET", screen.Actions![0]);
        screen.Confirm();
        Assert.False(screen.IsActive);
        Assert.Equal(new UndergroundRequest(UndergroundRequestKind.SetTrap, 1), screen.TakeRequest());
        Assert.Equal(UndergroundRequestKind.None, screen.TakeRequest().Kind);

        // A treasure put in the bag is the item above ground (Underground_ConvertTreasureToBagItem)
        screen.OpenBag(state, item =>
        {
            bag.Add(item);
            return true;
        }, tab: 3);
        screen.Confirm();
        Assert.Equal("PUT IN BAG", screen.Actions![0]);
        screen.Confirm();
        Assert.Equal(new[] { "Skull Fossil" }, bag);
        Assert.Empty(state.Treasures);
        Assert.NotNull(screen.Message);
        screen.Confirm();
        // A Sphere thrown away after a question; BURY is the tunnels'
        screen.Move(-1, 0);
        screen.Move(-1, 0);
        Assert.Equal("SPHERES", screen.Tabs[screen.Tab]);
        screen.Confirm();
        Assert.Equal(new[] { "BURY", "TRASH", "CANCEL" }, screen.Actions);
        screen.Move(0, 1);
        screen.Confirm();
        Assert.NotNull(screen.Question);
        screen.Move(0, 1);
        screen.Confirm();
        Assert.Single(state.Spheres);
        screen.Confirm();
        screen.Move(0, 1);
        screen.Confirm();
        screen.Confirm();
        Assert.Empty(state.Spheres);
        screen.Confirm();
        // SURFACE: the way up, and the records
        screen.Move(1, 0);
        screen.Move(1, 0);
        screen.Move(1, 0);
        Assert.Equal("SURFACE", screen.Tabs[screen.Tab]);
        Assert.Equal("GO UP", screen.Rows[0].Name);
        screen.Confirm();
        Assert.Equal(UndergroundRequestKind.GoUp, screen.TakeRequest().Kind);
    }

    [Fact]
    public void AHikersTradeAndTheBasesPCOnScreen()
    {
        var state = new UndergroundState { Seed = 31337 };
        const int vendor = 0;
        var stock = UndergroundVendors.Stock(state.Seed, vendor, false);
        state.TryAddSphere(stock[0].Kind, 99);
        state.TryAddGood(Good("UG_GOOD_WOODEN_CHAIR"));
        var screen = new UndergroundScreen();
        screen.OpenHiker(state, vendor, national: false);
        Assert.Equal(new[] { "HIS WARES", "YOUR GOODS" }, screen.Tabs);
        Assert.Equal(stock.Select(o => UndergroundDatabase.GoodName(o.Thing)), screen.Rows.Select(r => r.Name));
        // A ware, then the Sphere to pay with
        screen.Confirm();
        Assert.True(screen.Paying);
        screen.Confirm();
        Assert.False(screen.Paying);
        Assert.Equal(new[] { Good("UG_GOOD_WOODEN_CHAIR"), stock[0].Thing }, state.Goods);
        Assert.Empty(state.Spheres);
        screen.Confirm();
        // With no Sphere he has nothing to say but that
        screen.Confirm();
        Assert.False(screen.Paying);
        Assert.NotNull(screen.Message);
        screen.Confirm();
        // A good of the player's for a Sphere: asked first
        screen.Move(1, 0);
        screen.Confirm();
        Assert.NotNull(screen.Question);
        screen.Confirm();
        Assert.Single(state.Spheres);
        Assert.Single(state.Goods);
        screen.Confirm();
        screen.Cancel();
        Assert.False(screen.IsActive);
        // A treasure hiker only takes treasures
        screen.OpenHiker(state, 16, false);
        Assert.Equal(new[] { "YOUR TREASURES" }, screen.Tabs);
        screen.Cancel();

        // The base's PC: goods stored and taken out, the flags, and the decorating handed back to the game
        screen.OpenPc(state);
        Assert.Equal(new[] { "DECORATE", "STORE", "TAKE", "FLAGS" }, screen.Tabs);
        screen.Move(1, 0);
        screen.Confirm();
        Assert.Empty(state.Goods);
        Assert.Single(state.PcGoods);
        screen.Confirm();
        screen.Move(1, 0);
        screen.Confirm();
        Assert.Single(state.Goods);
        screen.Confirm();
        screen.Move(1, 0);
        Assert.Contains(screen.Rows, r => r.Name == "This base's flag" && r.Value == "Normal");
        screen.Move(1, 0);
        screen.Confirm();
        Assert.Equal(UndergroundRequestKind.Decorate, screen.TakeRequest().Kind);
    }

    // ------------------------------------------------------------------ the scripts

    private static HeadlessScriptHost Play(string script, string place, HeadlessScriptHost? host = null, params int[] answers)
    {
        host ??= new HeadlessScriptHost();
        foreach (int answer in answers) host.Answers.Enqueue(answer);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script, place)!, new NPC { Name = "Someone" });
        runner.RunToEnd();
        return host;
    }

    [Fact]
    public void TheUndergroundManTeachesByMissions()
    {
        const string house = "EternaUndergroundManHouse", mission = "VAR_CURRENT_UNDERGROUND_MISSION", active = "FLAG_HAS_ACTIVE_UNDERGROUND_MISSION";
        var host = Play("UndergroundMan", house, null, 0);
        // The kit, the offer taken, and the first mission set
        Assert.True(host.Story.Has("FLAG_RECEIVED_EXPLORER_KIT"));
        Assert.True(host.Story.Has("FLAG_ACCEPTED_UNDERGROUND_MAN_AS_MENTOR"));
        Assert.True(host.Story.Has(active));
        Assert.Equal(0, host.Story.Var(mission));
        // Not been down yet: nothing moves
        Play("UndergroundMan", house, host);
        Assert.Equal(0, host.Story.Var(mission));
        Assert.Empty(host.Underground.Traps);

        // Each mission: done below, rewarded here, and the next set by speaking again
        void Done(string flag, int next)
        {
            host.Story.Set(flag);
            Play("UndergroundMan", house, host);
            Assert.Equal(next, host.Story.Var(mission));
            Assert.False(host.Story.Has(active));
            Play("UndergroundMan", house, host);
            Assert.Equal(next <= 5, host.Story.Has(active));
            Assert.Equal(next, host.Story.Var(mission));
        }
        Done("FLAG_ENTERED_UNDERGROUND", 1);
        Assert.Equal(new[] { "TRAP_MOVE_UP", "TRAP_BUBBLE", "TRAP_LEAF" }, host.Underground.Traps.Select(t => UndergroundDatabase.Trap(t)!.Id));
        Done("FLAG_DIGGING_FOR_FOSSILS", 2);
        // Three Spheres of size 1 (ScrCmd_GiveSphere with 1)
        Assert.Equal(new[] { (SphereKind.Prism, 1), (SphereKind.Red, 1), (SphereKind.Blue, 1) }, host.Underground.Spheres.Select(s => (s.Kind, s.Size)));
        Done("FLAG_SPHERE_ACQUIRED", 3);
        Assert.Contains(Trap("TRAP_DIGGER_DRILL"), host.Underground.Traps);
        Done("FLAG_CREATED_SECRET_BASE", 4);
        Assert.Equal(new[] { "UG_GOOD_PLAIN_TABLE", "UG_GOOD_WOODEN_CHAIR", "UG_GOOD_SMALL_BOOKSHELF", "UG_GOOD_BUNEARY_DOLL" }, host.Underground.PcGoods.Select(g => UndergroundDatabase.Good(g)!.Id));
        host.Story.ChooseStarter("Piplup");
        Done("FLAG_DECORATED_SECRET_BASE", 5);
        Assert.Equal("UG_GOOD_PIPLUP_DOLL", UndergroundDatabase.Good(host.Underground.PcGoods[^1])!.Id);

        // The flags: one brought home, then three, ten and fifty of them
        host.Story.Set("FLAG_DELIVERED_STOLEN_FLAG");
        host.Underground.Records.FlagsCaptured = 1;
        Play("UndergroundMan", house, host);
        Assert.Equal(6, host.Story.Var(mission));
        Assert.Equal("UG_GOOD_PRETTY_GEM", UndergroundDatabase.Good(host.Underground.PcGoods[^1])!.Id);
        Play("UndergroundMan", house, host);
        Assert.Equal(6, host.Story.Var(mission));
        host.Underground.Records.FlagsCaptured = 3;
        Play("UndergroundMan", house, host);
        Assert.Equal(7, host.Story.Var(mission));
        host.Underground.Records.FlagsCaptured = 10;
        // With a full PC the reward waits (FLAG_COULD_NOT_RECEIVE_UNDERGROUND_MAN_REWARD)
        var kept = host.Underground.PcGoods.ToList();
        while (host.Underground.TryAddPcGood(Good("UG_GOOD_WOODEN_CHAIR"))) { }
        Play("UndergroundMan", house, host);
        Assert.Equal(7, host.Story.Var(mission));
        Assert.True(host.Story.Has("FLAG_COULD_NOT_RECEIVE_UNDERGROUND_MAN_REWARD"));
        host.Underground.PcGoods.Clear();
        host.Underground.PcGoods.AddRange(kept);
        Play("UndergroundMan", house, host);
        Assert.Equal(8, host.Story.Var(mission));
        Assert.False(host.Story.Has("FLAG_COULD_NOT_RECEIVE_UNDERGROUND_MAN_REWARD"));
        Assert.Equal("UG_GOOD_MYSTIC_GEM", UndergroundDatabase.Good(host.Underground.PcGoods[^1])!.Id);
        host.Underground.Records.FlagsCaptured = 50;
        Play("UndergroundMan", house, host);
        Assert.Equal(9, host.Story.Var(mission));
        Assert.Equal("UG_GOOD_GLITTER_GEM", UndergroundDatabase.Good(host.Underground.PcGoods[^1])!.Id);
        int goods = host.Underground.PcGoods.Count;
        Play("UndergroundMan", house, host);
        Assert.Equal(goods, host.Underground.PcGoods.Count);
    }

    [Fact]
    public void TheUndergroundsOwnScriptsAskAndHandOn()
    {
        // The wall: dug at only on a yes
        var host = Play("Wall", "underground", null, 1);
        Assert.DoesNotContain("underground mine", host.Log);
        host = Play("Wall", "underground", null, 0);
        Assert.Contains("underground mine", host.Log);
        // The way up
        host = Play("GoUp", "underground", null, 0);
        Assert.Contains("underground up", host.Log);
        // A hiker opens his trade
        foreach (string hiker in new[] { "HikerGoods", "HikerTraps", "HikerTreasures" }) Assert.Contains("underground hiker", Play(hiker, "underground").Log);
        // The base's PC: a flag carried is put up first, then the PC's own lists
        host = new HeadlessScriptHost();
        host.UndergroundResults["home"] = 2;
        Play("BasePC", "underground", host);
        Assert.Equal(new[] { "underground home", "underground pc" }, host.Log.Where(l => l.StartsWith("underground")));
        Assert.Contains(host.Transcript, line => line.Text.Contains("Bram"));
        // Another digger's PC: the flag taken on a yes, and nothing to take when today's is gone
        host = Play("DiggerPC", "underground", null, 0);
        Assert.Contains("underground takeflag", host.Log);
        host = new HeadlessScriptHost();
        host.UndergroundResults["cantake"] = 0;
        Play("DiggerPC", "underground", host);
        Assert.DoesNotContain("underground takeflag", host.Log);
        // The commands that change the bag do so without the game
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptParser.Parse("underground", """
            script Gifts
              underground give sphere green 12
              underground give trap "Pit Trap"
              underground give good "Globe"
              underground room
              end
            """)[0], null);
        runner.RunToEnd();
        Assert.Equal((SphereKind.Green, 12), (host.Underground.Spheres[0].Kind, host.Underground.Spheres[0].Size));
        Assert.Equal(new[] { Trap("TRAP_PIT") }, host.Underground.Traps);
        Assert.Equal(new[] { Good("UG_GOOD_GLOBE") }, host.Underground.PcGoods);
        Assert.Equal(1, runner.Result);
        Assert.Throws<ScriptException>(() => ScriptParser.Parse("underground", "script Bad\n  underground give trap \"No Such Trap\"\n"));
    }

    [Fact]
    public void RoarkMeetsWhoeverComesDownForTheFirstTime()
    {
        var map = MapDatabase.Fresh()("Underground");
        var host = new HeadlessScriptHost { Map = map };
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("OnEnter", "underground")!, null);
        runner.RunToEnd();
        Assert.Equal(1, host.Story.Var("VAR_HAS_SEEN_UNDERGROUND_ROARK_INTRO"));
        Assert.True(host.Story.Has("FLAG_HIDE_ROARK_UNDERGROUND"));
        Assert.Contains(host.Transcript, line => line.Text.Contains("Underground Man"));
        int said = host.Transcript.Count;
        // Once only
        runner.Start(ScriptLibrary.Default.Find("OnEnter", "underground")!, null);
        runner.RunToEnd();
        Assert.Equal(said, host.Transcript.Count);
    }
}
