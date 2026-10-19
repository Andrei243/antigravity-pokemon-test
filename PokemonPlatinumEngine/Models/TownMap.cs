using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models;

/// <summary>What the map of the region is opened for (the original's <c>TownMapMode</c>).</summary>
public enum TownMapMode
{
    /// <summary>The Town Map from the bag: the map, the places' words, the way come.</summary>
    Item,
    /// <summary>Fly's choice of a place to land.</summary>
    Fly,
    /// <summary>A map on a wall: looked at, with no way come on it.</summary>
    Wall,
    /// <summary>The Pokégear's map card (plan 20 · J3): the Town Map's own screen, which goes back to the Pokégear.</summary>
    Card
}

/// <summary>One of Fly's places as a game has it: open to fly to or not, and whether anything of it is drawn.</summary>
public sealed record TownMapSpot(RegionFlySpot Spot, SpawnLocation Town, bool Open)
{
    /// <summary>Two of the original's (Pal Park, Victory Road) are not drawn at all until they are open.</summary>
    public bool Drawn => Open || !Spot.Unseen;

    public bool Covers(int x, int y) => Spot.Cells.Contains((x, y));
}

/// <summary>
/// What a game has put on its region's map (plan 01 · M12; the original's <c>TownMapContext</c>,
/// <c>src/applications/town_map/context.c</c>): the hidden places revealed, the words a first arrival has earned,
/// Fly's places open, the cell the player is in and the way they came. Worked out once as the map is opened, from
/// the story; it changes nothing. No drawing or input.
/// </summary>
public sealed class TownMapState
{
    private readonly HashSet<string> revealed;
    private readonly HashSet<string> told;

    public RegionMap Region { get; }

    /// <summary>The cell the player is in, or went in from; null when nothing says (the original then puts them at its start).</summary>
    public (int X, int Y)? Here { get; }

    /// <summary>The cells passed through before this one, the latest first.</summary>
    public IReadOnlyList<TrailStep> Trail { get; }

    public IReadOnlyList<TownMapSpot> Spots { get; }

    public TownMapState(RegionMap region, StoryState story, (int X, int Y)? here = null, MapTrail? trail = null, Func<SpawnLocation, bool>? reachable = null)
    {
        Region = region;
        revealed = region.Hidden.Where(h => story.Var(h.Variable) == h.Value).Select(h => h.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        told = region.Areas.Concat(region.Landmarks).Select(p => p.Flag).OfType<string>().Where(story.Has).ToHashSet(StringComparer.Ordinal);
        Here = here is { } h2 && region.InView(h2.X, h2.Y) ? here : null;
        Trail = trail?.Recent(Here).Where(s => region.InView(s.X, s.Y)).ToList() ?? new List<TrailStep>();
        Spots = region.FlySpots
            .Select(s => (Spot: s, Town: SpawnLocations.Get(RegionDatabase.RegionOfMap(region.Map)?.Id, s.Spawn)))
            .Where(s => s.Town != null)
            .Select(s => new TownMapSpot(s.Spot, s.Town!, story.Has(s.Spot.Flag ?? s.Town!.ArrivalFlag) && (reachable?.Invoke(s.Town!) ?? true)))
            .ToList();
    }

    /// <summary>Whether a hidden place has been revealed (<c>SystemVars_CheckHiddenLocation</c>).</summary>
    public bool Revealed(RegionHiddenPlace place) => revealed.Contains(place.Key);

    /// <summary>The hidden places revealed, by their keys.</summary>
    public IReadOnlySet<string> RevealedPlaces => revealed;

    /// <summary>
    /// The block under a cell (<c>TownMap_GetMapBlockAtPosition</c>): null where there is none, and where it belongs
    /// to a hidden place the story has not revealed.
    /// </summary>
    public RegionBlock? BlockAt(int x, int y) =>
        Region.BlockAt(x, y) is { } block && (block.Hidden == null || revealed.Contains(block.Hidden.Key)) ? block : null;

    /// <summary>Whether a place's words are told (<c>PrintLocationDescription</c>): it waits for no flag, or its flag is set.</summary>
    public bool Tells(RegionPlace place) => place.Flag == null || told.Contains(place.Flag);

    /// <summary>
    /// Whether a block's landmark is told. Where the landmark waits for nothing of its own it waits for what its area
    /// waits for (the original's <c>make_town_map_data.py</c> gives it the area's check).
    /// </summary>
    public bool TellsLandmark(RegionBlock block) =>
        block.Landmark is { } landmark && (landmark.Flag != null ? told.Contains(landmark.Flag) : Tells(block.Area));

    /// <summary>
    /// Fly's place under a cell (<c>TownMap_GetFlyLocationAtPos</c>): the one drawn there, open or not. Where two
    /// share a town (the League's two), each has its own cell.
    /// </summary>
    public TownMapSpot? SpotAt(int x, int y) => BlockAt(x, y) == null ? null : Spots.FirstOrDefault(s => s.Drawn && s.Covers(x, y));

    /// <summary>Whether Fly may land at a cell (<c>CanFlyToHoveredLocation</c>).</summary>
    public bool CanFlyTo(int x, int y) => SpotAt(x, y) is { Open: true };

    /// <summary>Where the cursor stands as the map opens: on the player's cell, or at the region's start, held to the cursor's bounds.</summary>
    public (int X, int Y) FirstCursor()
    {
        var (x, y) = Here ?? Region.Start;
        return (Math.Clamp(x, Region.Bounds.Left, Region.Bounds.Right), Math.Clamp(y, Region.Bounds.Top, Region.Bounds.Bottom));
    }

    /// <summary>The name of the place the player is in, as the map has it; null where the map says nothing of it.</summary>
    public string? HereName => Here is var (x, y) ? Region.BlockAt(x, y)?.Name : null;
}
