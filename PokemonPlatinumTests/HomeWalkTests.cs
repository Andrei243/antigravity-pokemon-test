using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 43 · LF7: the home is walked into from a spot of every region that has spots of ours, and out by its door to the
/// tile before the spot. Each region's own maps are built; the base's rooms are Hoenn's maps, built alone, with their
/// door aimed back at the spot as the game aims it (<c>GameEngine.LayOutSecretBase</c>). A way out of the region's own
/// maps (a ship, another region's room) leads to a map of nothing, so the walk stays in the region.
/// </summary>
public class HomeWalkTests
{
    private static readonly Lazy<World> Hoenn = new(() => World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Hoenn));

    private static readonly Dictionary<string, Map> HandMade = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>A hand-made room of the region's (Sinnoh's houses), read from its file.</summary>
    private static Map? Room(string name)
    {
        lock (HandMade)
        {
            if (HandMade.TryGetValue(name, out var known)) return known;
            string path = Path.Combine("maps", name + ".json");
            if (!File.Exists(GameDataFiles.PathOf(path))) return null;
            return HandMade[name] = GameDataFiles.Load<MapFile>(path).ToMap();
        }
    }

    [Theory]
    [InlineData("Sinnoh")]
    [InlineData("Kanto")]
    [InlineData("Johto")]
    [InlineData("Unova")]
    [InlineData("Kalos")]
    public void TheHomeIsWalkedIntoFromASpotOfTheRegionAndOutByItsDoor(string region)
    {
        var world = World.LoadAll().Single(w => w.Index.Region == region);
        var maps = world.BuildMaps().ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);
        var spots = maps.Values.SelectMany(m => m.SecretBaseSpots.Select(s => (Map: m, s.Key.X, s.Key.Y, Id: s.Value))).ToList();
        Assert.True(spots.Count >= 5, $"{region} has {spots.Count} spots of the home");

        // The tile the player stands on to face each spot
        var befores = spots.Select(spot => (spot, Before: new[] { (0, 1), (0, -1), (1, 0), (-1, 0) }
            .Select(d => (X: spot.X + d.Item1, Y: spot.Y + d.Item2))
            .First(t => spot.Map.IsWalkable(t.X, t.Y) && spot.Map.GetWarpAt(t.X, t.Y) == null))).ToList();
        // Each room once, its door aimed at the first of its spots, as the game aims it at the spot the home's door stands at
        var rooms = new Dictionary<string, Map>(StringComparer.OrdinalIgnoreCase);
        foreach (var (spot, before) in befores)
        {
            var layout = SecretBaseLayouts.Of(spot.Id)!;
            if (rooms.ContainsKey(layout.Map)) continue;
            var room = WorldMapBuilder.Build(Hoenn.Value, Hoenn.Value.Index.Maps.Single(e => e.Name == layout.Map));
            var door = room.Warps.First();
            door.TargetMap = spot.Map.Name;
            door.TargetX = before.X;
            door.TargetY = before.Y;
            rooms[layout.Map] = room;
        }
        var nowhere = new Map(1, 1) { Name = "Nowhere" };
        Map MapOf(string name) =>
            rooms.TryGetValue(name, out var r) ? r : maps.TryGetValue(name, out var m) ? m : Room(name) ?? nowhere;

        // Walked from the tiles before the spots alone, as far as the region's own maps lead
        var walked = WorldWalk.From(MapOf, befores.Select(b => new MapSpot(b.spot.Map.Name, b.Before.X, b.Before.Y, Direction.Up)).ToArray());
        foreach (var (spot, _) in befores)
        {
            var room = rooms[SecretBaseLayouts.Of(spot.Id)!.Map];
            Assert.True(walked.ContainsKey(room), $"{region}: the home is never come into from the spot at {spot.X},{spot.Y} of {spot.Map.Name}");
            var door = room.Warps.First();
            Assert.Contains((door.SourceX, door.SourceY - 1), walked[room]);
        }
    }
}
