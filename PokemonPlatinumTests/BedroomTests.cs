using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 43 · LF7: the bedroom at home in every region, decorated from the decoration PC. Each region's room is a map of
/// the game with a computer, its floor and wall from its map, the tiles its scenes and steps need kept clear; a piece
/// standing in a bedroom stands in no other room; nothing may shut a way off; and the rooms come back from a save.
/// </summary>
public class BedroomTests
{
    private static readonly Lazy<List<World>> Worlds = new(World.LoadAll);

    /// <summary>A bedroom's map as the game makes it: a room of a region's world, or a hand-made room's file.</summary>
    private static Map BedroomMap(string region, string name)
    {
        var world = Worlds.Value.FirstOrDefault(w => string.Equals(w.Index.Region, region, StringComparison.OrdinalIgnoreCase));
        if (world?.Index.Maps.FirstOrDefault(e => e.Name == name) is { } entry) return WorldMapBuilder.Build(world, entry);
        return GameDataFiles.Load<MapFile>(Path.Combine("maps", name + ".json")).ToMap();
    }

    public static IEnumerable<object[]> EveryBedroom() =>
        Bedrooms.Maps.SelectMany(p => p.Value.Select(m => new object[] { p.Key, m }));

    [Theory]
    [MemberData(nameof(EveryBedroom))]
    public void EveryRegionsBedroomHasAComputerAndFloorToDecorate(string region, string map)
    {
        var room = BedroomMap(region, map);
        Assert.Equal(map, room.Name);
        Assert.Equal(region, Bedrooms.RegionOf(map), StringComparer.OrdinalIgnoreCase);
        Assert.True(Bedrooms.HasComputer(room), $"{map} has no computer to decorate it from: its props {string.Join(", ", room.Props.Select(p => $"{p.Type} {p.X},{p.Y}"))}");
        var floor = Bedrooms.FloorOf(room, region);
        // Room enough for a few pieces beyond what must stay clear
        Assert.True(floor.Floor.Count - floor.KeptClear.Count >= 6, $"{map}: {floor.Floor.Count} floor tiles, {floor.KeptClear.Count} kept clear");
        // The ways out and where the region's game begins are kept clear
        foreach (var warp in room.Warps.Where(w => floor.Floor.Contains((w.SourceX, w.SourceY - 1))))
            Assert.Contains((warp.SourceX, warp.SourceY - 1), floor.KeptClear);
        if (RegionDatabase.Get(region)?.Start is { } start && start.Map == map)
            Assert.Contains((start.X, start.Y), floor.KeptClear);
        Assert.DoesNotContain(floor.Start, floor.KeptClear);
    }

    [Fact]
    public void APieceInABedroomStandsInNoOtherRoom()
    {
        var store = new List<StoredDecoration>();
        foreach (string n in new[] { "Small Desk", "Pikachu Doll", "Ball Poster" }) Assert.True(SecretBases.Add(store, DecorationDatabase.Get(n)!.Id), n);
        var home = SecretBases.Open(null, store, 161, "Hoenn", 10, 10, 10, 11);
        home.Placed.Add(new PlacedDecoration { Slot = 0, X = 2, Y = 3 });
        var bedrooms = new Dictionary<string, List<PlacedDecoration>> { ["Sinnoh"] = new() { new PlacedDecoration { Slot = 1, X = 5, Y = 8 } } };
        Bedrooms.MarkOut(store, home, bedrooms);
        Assert.True(store[0].Out);
        Assert.True(store[1].Out);
        Assert.False(store[2].Out);

        // The base's decorating keeps what stands in it and leaves the bedroom's piece out
        var floor = new BaseFloor { Floor = Enumerable.Range(0, 6).SelectMany(x => Enumerable.Range(0, 6).Select(y => (x, y))).ToHashSet() };
        var room = SecretBases.Room(home, store, floor);
        SecretBases.Keep(home, room, store);
        Bedrooms.MarkOut(store, home, bedrooms);
        Assert.True(store[1].Out);

        // Packing the base up gives its desk back, never the bedroom's doll
        SecretBases.PackUp(home, store);
        Bedrooms.MarkOut(store, null, bedrooms);
        Assert.False(store[0].Out);
        Assert.True(store[1].Out);
    }

    [Fact]
    public void ABedroomsDecoratingKeepsTwelveAndNeverShutsAWayOff()
    {
        var room = BedroomMap(RegionDatabase.Sinnoh, "PlayerHouse2F");
        var floor = Bedrooms.FloorOf(room, RegionDatabase.Sinnoh);
        var store = new List<StoredDecoration>();
        var desk = DecorationDatabase.Get("Small Desk")!;
        SecretBases.Add(store, desk.Id);
        var decorating = Bedrooms.Room(new List<PlacedDecoration>(), store, floor);
        Assert.Equal(Bedrooms.MostOut, decorating.Limit);

        // A row of walls across the room's floor from one side to the other shuts the stairs off: the last piece of it is refused
        var piece = new RoomPiece { Kind = desk.Id, Name = desk.Name, Width = desk.Footprint.Width, Depth = desk.Footprint.Depth, Slot = 0 };
        bool everRefused = false;
        foreach (var (x, y) in floor.Floor.Where(t => !floor.KeptClear.Contains(t)).OrderBy(t => t.Item2).ThenBy(t => t.Item1))
        {
            if (!floor.Fits(desk, x, y)) continue;
            if (!Bedrooms.StillJoined(floor, decorating.Pieces, desk, x, y)) { everRefused = true; continue; }
            decorating.Pieces.Add(new RoomPiece { Kind = desk.Id, Name = desk.Name, X = x, Y = y, Width = piece.Width, Depth = piece.Depth, Slot = decorating.Pieces.Count });
        }
        Assert.True(everRefused, "a room filled with desks was never refused one that shut a way off");
        // Whatever stands, every tile kept clear is still reached from the others
        Assert.True(Bedrooms.StillJoined(floor, decorating.Pieces.Skip(1), desk, decorating.Pieces[0].X, decorating.Pieces[0].Y));
    }

    [Fact]
    public void TheBedroomsComeBackFromASave()
    {
        var store = new List<StoredDecoration>();
        SecretBases.Add(store, DecorationDatabase.Get("Pikachu Doll")!.Id);
        var bedrooms = new Dictionary<string, List<PlacedDecoration>> { ["Kalos"] = new() { new PlacedDecoration { Slot = 0, X = 3, Y = 4 } } };
        var back = SaveManager.Read(SaveManager.Write(new SaveData { Decorations = store, Bedrooms = bedrooms })!)!;
        var kalos = Assert.Single(back.Bedrooms!["Kalos"]);
        Assert.Equal((0, 3, 4), (kalos.Slot, kalos.X, kalos.Y));
        // A save with no bedroom decorated writes none
        Assert.Null(SaveManager.Read(SaveManager.Write(new SaveData())!)!.Bedrooms);
    }
}
