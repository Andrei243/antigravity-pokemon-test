using System;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

partial class Harness
{
    // ---------------------------------------------------------------- Unova's special tiles (plan 38 · EX11; EXPLORE_PART=special)

    // A Unova game on the day's number 7: the player stood two tiles off a patch of each kind in a place that has one, a shot of
    // each (ex95_shaking_grass, ex96_rippling_water, ex97_dust_cloud, ex98_bridge_shadow) and what a step onto it meets printed.
    private void ExploreUnovaSpecial()
    {
        engine.NewGameRegion = RegionDatabase.Unova;
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        game.TrainersLookOnArrival = false;
        game.Encounters.DailyNumber = 7;
        const uint day = 7;
        var maps = RegionDatabase.Get(RegionDatabase.Unova)!.Maps.Select(n => MapDatabase.Get(n)).Where(m => m.Width * m.Height < 300_000).ToList();
        foreach (var (kind, shot, prefer) in new[]
        {
            (SpecialKind.Shaking, "ex95_shaking_grass", "route"),
            (SpecialKind.Rippling, "ex96_rippling_water", "undella"),
            (SpecialKind.Dust, "ex97_dust_cloud", "chargestone"),
            (SpecialKind.Shadow, "ex98_bridge_shadow", "driftveil_drawbridge")
        })
        {
            (Map Map, int X, int Y)? hit = null;
            foreach (var pass in new[] { true, false })
                foreach (var map in maps)
                {
                    for (int y = 0; y < map.Height && hit == null; y++)
                        for (int x = 0; x < map.Width && hit == null; x++)
                            if (map.AreaAt(x, y) is { } a && (!pass || a.Key.Contains(prefer))
                                && UnovaSpecialGrass.KindAt(map, x, y, day, kind == SpecialKind.Rippling) == kind
                                && StandBeside(map, x, y, kind) != null)
                                hit = (map, x, y);
                    if (hit != null) break;
                }
            if (hit is not { } found) { Console.WriteLine($"  !! no {kind} patch in any place"); continue; }
            var (px, py, facing) = StandBeside(found.Map, found.X, found.Y, kind)!.Value;
            game.Place(found.Map, px, py, facing);
            Frames(20);
            Shot(shot);
            var area = found.Map.AreaAt(found.X, found.Y)!;
            var (table, rate) = UnovaSpecialGrass.TableOf(area, kind);
            Console.WriteLine($"{shot}: {kind} patch at {found.Map.Name} {found.X},{found.Y} in {area.Key}, player {px},{py}; table {string.Join(" ", table.Select(e => e.SpeciesName).Distinct())} at rate {rate}"
                + (kind == SpecialKind.Dust ? $"; today's find {UnovaSpecialGrass.DustAt(area, day, found.X, found.Y)}" : ""));
        }
    }

    // A walkable tile two or three tiles from the patch that is in line with it, for the player to stand on facing it
    private static (int X, int Y, Direction Facing)? StandBeside(Map map, int x, int y, SpecialKind kind)
    {
        foreach (var (dx, dy, facing) in new[] { (0, 3, Direction.Up), (0, -3, Direction.Down), (3, 0, Direction.Left), (-3, 0, Direction.Right) })
        {
            int sx = x + dx, sy = y + dy;
            if (map.InBounds(sx, sy) && map.IsWalkable(sx, sy) && !TileBehaviors.IsSurfable(map.BehaviourAt(sx, sy))) return (sx, sy, facing);
        }
        return null;
    }
}
