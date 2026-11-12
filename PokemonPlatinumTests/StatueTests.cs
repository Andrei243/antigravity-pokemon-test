using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

using PokemonPlatinumEngine.Models.Underground;
using PokemonPlatinumEngine.Overworld;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>The hideaways' statues (plan 38 · EX6, part b): goods of ours dug from the hideaways' walls.</summary>
public class StatueTests
{
    private static Hideaway Lake => Hideaways.All.First(h => h.Map == "HideawayLakeCavern");

    [Fact]
    public void EverySpeciesOfTheHideawaysPoolsHasAStatueAndNoOtherHasOne()
    {
        var pools = Hideaways.All.SelectMany(h => Hideaways.Pool(h, national: true)).ToHashSet(StringComparer.Ordinal);
        var statues = UndergroundDatabase.Statues.Select(s => s.Species).ToList();
        Assert.Equal(statues.Count, statues.Distinct().Count());
        Assert.Empty(pools.Except(statues));
        Assert.Empty(statues.Except(pools));
        // In National order, each with a rarity of one to three
        Assert.Equal(statues, statues.OrderBy(s => PokemonDatabase.Get(s)!.DexNumber).ToList());
        Assert.All(UndergroundDatabase.Statues, s => Assert.InRange(s.Rarity, 1, 3));
    }

    [Fact]
    public void AStatueIsAGoodOfItsOwnPastTheOriginalsNumbers()
    {
        Assert.Equal(138, UndergroundDatabase.GoodCount);
        int id = UndergroundDatabase.StatueOf("Dewgong");
        Assert.True(id >= UndergroundDatabase.StatueFirst);
        Assert.True(UndergroundDatabase.IsStatue(id));
        Assert.False(UndergroundDatabase.IsStatue(UndergroundDatabase.GoodId("UG_GOOD_PIPLUP_DOLL")));
        var good = UndergroundDatabase.Good(id)!;
        Assert.Equal((1, 1), (good.Width, good.Depth));
        Assert.Equal(new[] { (4, 5) }, SecretBaseRules.Blocked(id, 4, 5).ToArray());
        Assert.Contains("Dewgong", UndergroundDatabase.GoodName(id));
        Assert.Equal("Dewgong", UndergroundDatabase.Statue(id)!.Species);
        Assert.Equal(new[] { PokemonType.Water, PokemonType.Ice }, Hideaways.TypesOf(PokemonDatabase.Get("Dewgong")!));
        // A statue goes in the bag and the PC as any good does
        var state = new UndergroundState();
        Assert.True(state.TryAddGood(id));
        Assert.True(state.Deposit(0));
        Assert.Equal(id, state.PcGoods.Single());
    }

    [Fact]
    public void TheBaseTakesStatuesWithinItsLimit()
    {
        var state = new UndergroundState { Base = SecretBaseRules.Create(100, 100, Direction.Up, new Rand32(5)) };
        state.Base.Boulders.Clear();
        int statue = UndergroundDatabase.StatueOf("Golduck");
        for (int i = 0; i < 12; i++) state.TryAddPcGood(statue);
        var session = SecretBaseRules.Decorating(state);
        Assert.Equal(SecretBaseRules.GoodsLimit(0), session.Room.Limit);
        Assert.Equal(10, session.Room.Limit);
    }

    [Fact]
    public void TheWeightsCountThePlacedStatuesByTheirTypes()
    {
        Assert.Empty(Hideaways.Weights(null));
        Assert.Empty(Hideaways.Weights(new SavedBase()));
        var room = new SavedBase();
        // a doll counts for nothing
        room.Goods.Add(new PlacedGood { Good = UndergroundDatabase.GoodId("UG_GOOD_PIPLUP_DOLL"), X = 3, Z = 3 });
        Assert.Empty(Hideaways.Weights(room));
        room.Goods.Add(new PlacedGood { Good = UndergroundDatabase.StatueOf("Dewgong"), X = 4, Z = 3 });
        var one = Hideaways.Weights(room);
        Assert.Equal(1, one[PokemonType.Water]);
        Assert.Equal(1, one[PokemonType.Ice]);
        for (int i = 0; i < 14; i++) room.Goods.Add(new PlacedGood { Good = UndergroundDatabase.StatueOf("Golduck"), X = 5 + i % 4, Z = 4 + i / 4 });
        var many = Hideaways.Weights(room);
        Assert.Equal(Hideaways.MostStatuesOfAType, many[PokemonType.Water]);
        Assert.Equal(1, many[PokemonType.Ice]);
    }

    [Fact]
    public void NoStatueLeavesEveryTableAsItWas()
    {
        foreach (var h in Hideaways.All)
            for (int day = 0; day < 5; day++)
            {
                var plain = Hideaways.Table(h, day, true).Select(r => (r.SpeciesName, r.MinLevel)).ToList();
                var none = Hideaways.Table(h, day, true, new Dictionary<PokemonType, int>()).Select(r => (r.SpeciesName, r.MinLevel)).ToList();
                Assert.Equal(plain, none);
            }
    }

    [Fact]
    public void ThreeWaterStatuesRaiseTheLakeCavernsWaterShareByTheRule()
    {
        var pool = Hideaways.Pool(Lake, national: true).Select(s => PokemonDatabase.Get(s)!).ToList();
        int water = pool.Count(s => Hideaways.TypesOf(s).Contains(PokemonType.Water)), others = pool.Count - water;
        var three = new Dictionary<PokemonType, int> { [PokemonType.Water] = 3 };
        // The rule: a species is picked by a weight of StatueBase, and one more for each placed statue of a type it has
        Assert.Equal(Hideaways.StatueBase + 3, Hideaways.PickWeight(pool.First(s => Hideaways.TypesOf(s).Contains(PokemonType.Water)), three));
        Assert.Equal(Hideaways.StatueBase, Hideaways.PickWeight(pool.First(s => !Hideaways.TypesOf(s).Contains(PokemonType.Water)), three));
        double before = water / (double)pool.Count;
        double after = water * 5.0 / (water * 5.0 + others * 2.0);
        Assert.Equal(after, Hideaways.PickShare(Lake, true, three, PokemonType.Water), 9);
        Assert.Equal(before, Hideaways.PickShare(Lake, true, new Dictionary<PokemonType, int>(), PokemonType.Water), 9);
        Assert.True(after > before);

        // and the tables over a year show it: more of the slots' weight is Water's with the statues
        double Share(IReadOnlyDictionary<PokemonType, int>? w) => Enumerable.Range(0, 365).Average(day =>
        {
            var rows = Hideaways.Table(Lake, day, true, w);
            return rows.Where(r => Hideaways.TypesOf(PokemonDatabase.Get(r.SpeciesName)!).Contains(PokemonType.Water)).Sum(r => r.Weight) / (double)rows.Sum(r => r.Weight);
        });
        Assert.True(Share(three) > Share(null));
    }

    [Fact]
    public void AHideawaysWallsAreItsRockFacingItsFloorAndTheSameForADay()
    {
        foreach (var h in Hideaways.All)
        {
            var file = Hideaways.ToFile(h);
            var walls = Hideaways.WallsOf(h, 7);
            Assert.Equal(Hideaways.WallsPerDay, walls.Count);
            Assert.Equal(walls, Hideaways.WallsOf(h, 7));
            Assert.Equal(walls.Count, walls.Distinct().Count());
            foreach (var (x, z) in walls)
            {
                Assert.Equal('#', file.Solid[z][x]);
                Assert.Equal('.', file.Solid[z + 1][x]);
                Assert.NotEqual('~', file.Ground[z + 1][x]);
            }
        }
        Assert.Contains(Enumerable.Range(0, 20), d => !Hideaways.WallsOf(Lake, d).SequenceEqual(Hideaways.WallsOf(Lake, 0)));
        Assert.Equal("FLAG_DAILY_HIDEAWAY_2_WALL_1", Hideaways.WallFlag(Lake, 1));
    }

    [Fact]
    public void AWallsTableIsTheDaysStatuesAndTheSmallSpheres()
    {
        var table = Hideaways.WallTable(Lake, 3, true);
        var day = Hideaways.Table(Lake, 3, true).Select(r => r.SpeciesName).Distinct().ToHashSet();
        var statues = table.Where(t => UndergroundDatabase.IsStatue(t.Id)).ToList();
        Assert.Equal(day.Count, statues.Count);
        Assert.All(statues, t => Assert.Contains(UndergroundDatabase.Statue(t.Id)!.Species, day));
        Assert.All(table.Where(t => !UndergroundDatabase.IsStatue(t.Id)), t => Assert.True(UndergroundDatabase.IsSphere(t.Id) && !UndergroundDatabase.IsLargeSphere(t.Id)));

        // A wall laid from it holds two to four things, a statue among them now and then, and dark rock
        int statuesFound = 0;
        for (ulong seed = 1; seed <= 40; seed++)
        {
            var game = new MiningGame(new Rand32(seed), table);
            Assert.InRange(game.ItemCount, 2, 4);
            Assert.All(game.Objects.Take(game.ItemCount), o => Assert.False(o.IsRock));
            Assert.All(game.Objects.Skip(game.ItemCount), o => Assert.True(o.IsRock));
            statuesFound += game.Objects.Take(game.ItemCount).Count(o => UndergroundDatabase.IsStatue(o.Id));
            // dug whole, a statue comes out as itself
            for (int y = 0; y < MiningGame.Height; y++)
                for (int x = 0; x < MiningGame.Width; x++)
                {
                    game.PickSelected = false;
                    game.Strike(x, y);
                }
        }
        Assert.InRange(statuesFound, 10, 120);
    }

    [Fact]
    public void EveryFigureIsPaintedInStoneOnItsPlinthWithItsPlaque()
    {
        Assert.Equal(PokemonPlatinumEngine.Graphics.UndergroundArt.StatueFigure.Coil, PokemonPlatinumEngine.Graphics.UndergroundArt.StatueLook("Gyarados").Figure);
        foreach (var figure in Enum.GetValues<PokemonPlatinumEngine.Graphics.UndergroundArt.StatueFigure>())
        {
            var c = new PokemonPlatinumEngine.Graphics.PixelCanvas(24, 36);
            PokemonPlatinumEngine.Graphics.UndergroundArt.PaintStatue(c, figure, new Raylib_cs.Color(200, 60, 60, 255));
            int opaque = 0;
            for (int y = 0; y < 27; y++) for (int x = 0; x < 24; x++) if (c.Get(x, y).A == 255) opaque++;
            Assert.True(opaque > 60, $"{figure}: {opaque}");
            Assert.Equal(255, c.Get(12, 32).A);
        }
        // Every statue's species gives a figure, and a type's plaque
        Assert.All(UndergroundDatabase.Statues, s => Assert.NotEqual(0, PokemonPlatinumEngine.Graphics.UndergroundArt.StatueLook(s.Species).Plaque.A));
    }

    [Fact]
    public void AStatueKeptInTheBaseSurvivesASave()
    {
        var state = new UndergroundState { Base = new SavedBase() };
        int id = UndergroundDatabase.StatueOf("Steelix");
        state.TryAddPcGood(id);
        state.Base.Goods.Add(new PlacedGood { Good = id, X = 6, Z = 6, Slot = 0 });
        string json = JsonSerializer.Serialize(state, GameDataFiles.Json);
        var back = JsonSerializer.Deserialize<UndergroundState>(json, GameDataFiles.Json)!;
        Assert.Equal(id, back.PcGoods.Single());
        Assert.Equal(id, back.Base!.Goods.Single().Good);
        Assert.Equal(2, Hideaways.Weights(back.Base).Count);
    }
}
