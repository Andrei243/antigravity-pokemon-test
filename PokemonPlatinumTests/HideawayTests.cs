using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models.Underground;
using PokemonPlatinumEngine.Overworld;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>The hideaways under the Underground (plan 38 · EX6, part a).</summary>
public class HideawayTests
{
    private static readonly Lazy<Func<string, Map>> Maps = new(MapDatabase.Fresh);

    [Fact]
    public void EveryPieceIsOpenAtItsJointsOneFloorAndDryThere()
    {
        foreach (var piece in Hideaways.Pieces)
        {
            Assert.Equal(8, piece.Length);
            Assert.All(piece, row => Assert.Equal(8, row.Length));
            foreach (int i in new[] { 3, 4 })
            {
                Assert.Equal('.', piece[0][i]);
                Assert.Equal('.', piece[7][i]);
                Assert.Equal('.', piece[i][0]);
                Assert.Equal('.', piece[i][7]);
            }
            var open = new HashSet<(int, int)>();
            for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) if (piece[y][x] == '.') open.Add((x, y));
            var seen = new HashSet<(int, int)> { (3, 0) };
            var queue = new Queue<(int X, int Y)>(seen);
            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                foreach (var next in new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) })
                    if (open.Contains(next) && seen.Add(next)) queue.Enqueue(next);
            }
            Assert.Equal(open.Count, seen.Count);
        }
    }

    [Fact]
    public void ASeedLaysTheSameHideawayEveryTime()
    {
        foreach (var h in Hideaways.All)
        {
            var a = Hideaways.ToFile(h);
            var b = Hideaways.ToFile(h);
            Assert.Equal(a.Ground, b.Ground);
            Assert.Equal(a.Solid, b.Solid);
            Assert.Equal(Hideaways.Lay(h).Layout, Hideaways.Lay(h).Layout);
        }
        // and not every hideaway is the same
        Assert.True(Hideaways.All.Select(h => string.Join("", Hideaways.Lay(h).Layout)).Distinct().Count() > 6);
    }

    [Fact]
    public void AHideawaysFloorIsOnePieceReachedFromItsWayOut()
    {
        foreach (var h in Hideaways.All)
        {
            var map = Maps.Value(h.Map);
            Assert.Equal(MapSetting.Cave, map.Setting);
            Assert.True(map.WildVisible);
            Assert.Equal(FieldCamera.Cave, map.Camera);
            var reached = WorldWalk.From(Maps.Value, new MapSpot(h.Map, Hideaways.ArriveX, Hideaways.ArriveY))[map];
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                    if (!map.IsSolid(x, y) && !TileBehaviors.IsSurfable(map.BehaviourAt(x, y)))
                        Assert.True(reached.Contains((x, y)), $"{h.Map}: {x},{y} is never come to");
            // and its way out leads back to the tunnels before its way down
            var warp = map.GetWarpAt(Hideaways.OutX, Hideaways.OutY)!;
            Assert.Equal("Underground", warp.TargetMap);
            Assert.Equal((h.DownX, h.DownZ + 1), (warp.TargetX, warp.TargetY));
        }
    }


    [Fact]
    public void TwoWaysDownLieInEachOfTheSixTunnelSystems()
    {
        var tunnels = Maps.Value("Underground");
        var systemOf = new Dictionary<(int, int), int>();
        int systems = 0;
        foreach (var h in Hideaways.All)
        {
            var before = (h.DownX, h.DownZ + 1);
            Assert.False(tunnels.IsSolid(before.Item1, before.Item2), $"{h.Name}: the tile before its way down is rock");
            Assert.Equal(TileType.CaveMouth, tunnels.GetGroundTile(h.DownX, h.DownZ));
            var warp = tunnels.GetWarpAt(h.DownX, h.DownZ)!;
            Assert.Equal(h.Map, warp.TargetMap);
            Assert.Equal(Hideaways.OpenedBy, warp.OpenedBy);
            if (systemOf.ContainsKey(before)) continue;
            systems++;
            var queue = new Queue<(int X, int Y)>();
            queue.Enqueue(before);
            systemOf[before] = systems;
            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                foreach (var next in new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) })
                    if (tunnels.InBounds(next.Item1, next.Item2) && !tunnels.IsSolid(next.Item1, next.Item2) && tunnels.GetGroundTile(next.Item1, next.Item2) != TileType.CaveMouth
                        && systemOf.TryAdd(next, systems)) queue.Enqueue(next);
            }
        }
        var counts = Hideaways.All.GroupBy(h => systemOf[(h.DownX, h.DownZ + 1)]).Select(g => g.Count()).ToList();
        Assert.Equal(6, counts.Count);
        Assert.All(counts, c => Assert.Equal(2, c));
        // shut until Sinnoh's Hall of Fame
        tunnels.ApplyDoors(_ => false);
        Assert.All(Hideaways.All, h => Assert.True(tunnels.IsSolid(h.DownX, h.DownZ)));
        tunnels.ApplyDoors(flag => flag == Hideaways.OpenedBy);
        Assert.All(Hideaways.All, h => Assert.False(tunnels.IsSolid(h.DownX, h.DownZ)));
    }

    [Fact]
    public void EveryHideawayIsComeToFromTheTunnels()
    {
        foreach (var h in Hideaways.All)
        {
            var reached = WorldWalk.From(Maps.Value, new MapSpot("Underground", h.DownX, h.DownZ + 1));
            Assert.Contains(reached.Keys, m => m.Name == h.Map);
        }
    }

    [Fact]
    public void EachTableIsOfItsTypesAtTheBattleZonesBand()
    {
        foreach (var h in Hideaways.All)
            foreach (bool national in new[] { false, true })
            {
                var pool = Hideaways.Pool(h, national);
                Assert.True(pool.Count >= 6, $"{h.Name}: {pool.Count}");
                foreach (string name in pool)
                {
                    var s = PokemonDatabase.Get(name)!;
                    Assert.False(s.Legendary || s.Mythical, name);
                    Assert.InRange(s.DexNumber, 1, 493);
                    Assert.Contains(Hideaways.TypesOf(s), t => h.Types.Contains(t));
                    if (!national) Assert.True(s.SinnohNumber > 0, name);
                }
                for (int day = 0; day < 5; day++)
                {
                    var table = Hideaways.Table(h, day, national);
                    Assert.Equal(12, table.Count);
                    Assert.All(table, e => Assert.Contains(e.SpeciesName, pool));
                    Assert.All(table, e => Assert.InRange(e.MinLevel, Hideaways.LowestLevel, Hideaways.HighestLevel));
                    Assert.All(table, e => Assert.InRange(e.MaxLevel, e.MinLevel, Hideaways.HighestLevel));
                    Assert.Equal(Hideaways.Table(h, day, national).Select(e => e.SpeciesName), table.Select(e => e.SpeciesName));
                }
                // another day is another twelve, somewhere
                Assert.Contains(Enumerable.Range(1, 6), day => !Hideaways.Table(h, day, national).Select(e => e.SpeciesName).SequenceEqual(Hideaways.Table(h, 0, national).Select(e => e.SpeciesName)));
            }
    }

    [Fact]
    public void TheAreaPageListsTheHideaways()
    {
        var habitats = Habitats.Sinnoh!;
        Assert.Contains(habitats.Of("Charizard"), p => p.Ways.HasFlag(HabitatWays.Hideaway));
        Assert.Contains(habitats.Of("Ludicolo"), p => p.Ways.HasFlag(HabitatWays.Hideaway) && p.Name == "Lake Cavern");
        // ... and never on the HERE list of the cell over a way down, where nothing of them is on the surface
        foreach (var h in Hideaways.All)
        {
            var (cx, cy) = Hideaways.CellOver(h.DownX, h.DownZ);
            Assert.DoesNotContain(habitats.At(cx, cy), here => here.Ways.HasFlag(HabitatWays.Hideaway));
        }
    }

    [Fact]
    public void AHideawaysPokemonStandOnAnyFloorOfIt()
    {
        var h = Hideaways.All.First(x => x.Floor == ',');
        var map = Maps.Value(h.Map);
        var floor = Enumerable.Range(0, map.Height).SelectMany(y => Enumerable.Range(0, map.Width).Select(x => (x, y)))
            .First(t => !map.IsSolid(t.x, t.y) && map.GetWarpAt(t.x, t.y) == null);
        Assert.True(FieldPokemon.CanStand(map, floor.x, floor.y, water: false));
        // a place with no rule of its own stands them only in its grass, as before
        var plain = new Map(3, 3);
        Assert.False(FieldPokemon.CanStand(plain, 1, 1, water: false));
    }
}
