using System;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

partial class Harness
{
    // ---------------------------------------------------------------- Route 119's Feebas (plan 38 · EX4; EXPLORE_PART=feebas)

    // A Hoenn game on the day's number 7: the player on Route 119's bank facing one of the day's Feebas tiles with a Good Rod
    // cast, the bite and the battle (ex33_feebas_route_119). It prints the day's six tiles and the one fished.
    private void ExploreFeebas()
    {
        engine.NewGameRegion = RegionDatabase.Hoenn;
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        game.TrainersLookOnArrival = false;
        game.Encounters.DailyNumber = 7;
        string overworld = RegionMap.Of(RegionDatabase.Hoenn)!.Map;
        var map = MapDatabase.Get(overworld);
        var water = HoennFeebas.WaterOf(map);
        var today = HoennFeebas.TilesToday(water, game.Encounters.DailyNumber);
        Console.WriteLine($"explore: Route 119 has {water.Count} water tiles; day 7's Feebas tiles {string.Join(" ", today.Select(t => $"{t.X},{t.Y}"))}");
        // A tile of the day's with walkable ground beside it, the player stood there facing the water
        foreach (var (tx, ty) in today)
            foreach (var (dx, dy, facing) in new[] { (0, 1, Direction.Up), (0, -1, Direction.Down), (1, 0, Direction.Left), (-1, 0, Direction.Right) })
            {
                int sx = tx + dx, sy = ty + dy;
                if (!map.InBounds(sx, sy) || !map.IsWalkable(sx, sy) || TileBehaviors.IsSurfable(map.BehaviourAt(sx, sy))) continue;
                At(overworld, sx, sy, facing);
                Frames(10);
                var entry = new WildEncounterEntry { SpeciesName = HoennFeebas.Species, MinLevel = HoennFeebas.MinLevel, MaxLevel = HoennFeebas.MaxLevel };
                game.Fishing = new FishingAttempt(FishingRod.Good, entry, new Random(3));
                Frames(40);
                for (int i = 0; i < 400 && engine.CastStage != FishingStage.Hooked; i++) Frames(1);
                Frames(4);
                Shot("ex33_feebas_route_119");
                Console.WriteLine($"ex33_feebas_route_119: on {sx},{sy} facing {facing} the day's tile {tx},{ty}, a {HoennFeebas.Species} on the line");
                game.Fishing = null;
                return;
            }
        Console.WriteLine("  !! no tile of the day's has a bank beside it");
    }
}
