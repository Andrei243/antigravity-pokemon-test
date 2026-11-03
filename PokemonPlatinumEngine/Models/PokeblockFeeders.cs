using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The Pokéblock feeders of Hoenn's Safari Zone (plan 21 · H11, part d; Emerald's <c>src/safari_zone.c</c> and
/// <c>PickWildMonNature</c> of <c>src/wild_encounter.c</c>): ten places for a Pokéblock set down on a feeder, each
/// lasting a hundred steps of the Safari Game; while one lies within five steps of the player, four wild Pokémon in five
/// are given a nature that likes it. Nothing of it is saved, as Emerald keeps it in working memory only, and a new game
/// in the zone, or its end, clears every feeder. No drawing, no input.
/// </summary>
/// <remarks>
/// Emerald keeps each of the zone's six parts as a map of its own, forty tiles by forty, and a feeder by that map's number
/// and its own tiles. Here the six are one map, laid side by side from the north-west part at nought (plan 21 · H6, part
/// b), so a part is a tile's place in the grid of forty (<see cref="PartOf"/>) and its own tiles are the rest
/// (<see cref="Local"/>): the range check below works on those, since its carried difference (Emerald's) depends on them.
/// </remarks>
public sealed class PokeblockFeeders
{
    /// <summary><c>NUM_POKEBLOCK_FEEDERS</c>.</summary>
    public const int Count = 10;

    /// <summary>The steps a Pokéblock lasts on its feeder (<c>SafariZoneActivatePokeblockFeeder</c>).</summary>
    public const int Steps = 100;

    /// <summary>How far, in steps across and down together, a feeder's Pokéblock reaches (<c>GetPokeblockFeederWithinRange</c>).</summary>
    public const int Reach = 5;

    /// <summary>The share in a hundred of wild Pokémon whose nature a Pokéblock in reach chooses (<c>PickWildMonNature</c>).</summary>
    public const int Pull = 80;

    /// <summary>The map the zone's six parts lie on (<c>world.json</c>'s <c>HoennSafariZone</c>).</summary>
    public const string MapName = "HoennSafariZone";

    /// <summary>The side of one of the zone's parts, in tiles (Emerald's Safari Zone maps).</summary>
    public const int PartSide = 40;

    /// <summary>A Pokéblock on a feeder: the part it lies in, its tile in the part's own tiles, the steps left and the Pokéblock.</summary>
    public sealed record Feeder(int Part, int X, int Y, int StepsLeft, Pokeblock Block);

    private readonly Feeder?[] slots = new Feeder?[Count];

    /// <summary>The ten places, empty ones null.</summary>
    public IReadOnlyList<Feeder?> Slots => slots;

    /// <summary>The part of the zone a tile of its map lies in (Emerald's map number), counted across and down.</summary>
    public static int PartOf(int x, int y) => (y / PartSide) * 3 + x / PartSide;

    /// <summary>A tile of the zone's map in its part's own tiles.</summary>
    public static (int X, int Y) Local(int x, int y) => (x % PartSide, y % PartSide);

    /// <summary>Every feeder emptied (<c>ClearAllPokeblockFeeders</c>): as a Safari Game begins and ends.</summary>
    public void Clear() => Array.Clear(slots);

    /// <summary>
    /// The place of the Pokéblock lying on the feeder at a tile of the map (<c>GetPokeblockFeederInFront</c>, given the tile
    /// before the player), or −1 for none.
    /// </summary>
    public int At(int x, int y)
    {
        int part = PartOf(x, y);
        var (lx, ly) = Local(x, y);
        for (int i = 0; i < Count; i++)
            if (slots[i] is { } f && f.Part == part && f.X == lx && f.Y == ly) return i;
        return -1;
    }

    /// <summary>
    /// Sets a Pokéblock down on the feeder at a tile of the map (<c>SafariZoneActivatePokeblockFeeder</c>): in the first
    /// empty place, for a hundred steps. False, with nothing set, when all ten are taken.
    /// </summary>
    public bool Place(int x, int y, Pokeblock block)
    {
        int free = Array.FindIndex(slots, s => s == null);
        if (free < 0) return false;
        var (lx, ly) = Local(x, y);
        slots[free] = new Feeder(PartOf(x, y), lx, ly, Steps, block);
        return true;
    }

    /// <summary>A step of the Safari Game (<c>DecrementFeederStepCounters</c>): each Pokéblock a step nearer gone, and gone at none.</summary>
    public void Step()
    {
        for (int i = 0; i < Count; i++)
        {
            if (slots[i] is not { } f) continue;
            slots[i] = f.StepsLeft - 1 <= 0 ? null : f with { StepsLeft = f.StepsLeft - 1 };
        }
    }

    /// <summary>
    /// The Pokéblock in reach of a tile of the map where a step ends (<c>SafariZoneGetActivePokeblock</c>,
    /// <c>GetPokeblockFeederWithinRange</c>), or null. Emerald's check is kept as it is: the difference from one feeder of
    /// the part is carried into the check of the next (it subtracts from the sum it made, never from the tile again), so
    /// with two Pokéblocks in a part the second is found where Emerald finds it, not always where it lies.
    /// </summary>
    public Pokeblock? InReach(int x, int y)
    {
        int part = PartOf(x, y);
        var (dx, dy) = Local(x, y);
        for (int i = 0; i < Count; i++)
        {
            if (slots[i] is not { } f || f.Part != part) continue;
            dx -= f.X;
            dy -= f.Y;
            if (dx < 0) dx = -dx;
            if (dy < 0) dy = -dy;
            if (dx + dy <= Reach) return f.Block;
        }
        return null;
    }

    /// <summary>
    /// The nature a Pokéblock in reach chooses for a wild Pokémon (<c>PickWildMonNature</c>'s first half), or null to leave
    /// it to the rest: four times in five the twenty-five natures are shuffled (each pair swapped on a coin, in Emerald's
    /// order) and the first that likes the Pokéblock is taken. A Pokéblock no nature likes (a black one of no taste)
    /// chooses none.
    /// </summary>
    public static Nature? NatureFor(Pokeblock? block, Random rng)
    {
        if (block == null || rng.Next(100) >= Pull) return null;
        var natures = new Nature[25];
        for (int i = 0; i < natures.Length; i++) natures[i] = (Nature)i;
        for (int i = 0; i < natures.Length - 1; i++)
            for (int j = i + 1; j < natures.Length; j++)
                if ((rng.Next() & 1) != 0) (natures[i], natures[j]) = (natures[j], natures[i]);
        foreach (var nature in natures)
            if (Pokeblocks.Gain(nature, block) > 0) return nature;
        return null;
    }
}
