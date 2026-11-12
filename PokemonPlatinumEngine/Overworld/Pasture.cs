using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// A visit to the pasture as the save keeps it (plan 35 · BN5): the box that walks the meadow and the PC the player came
/// from, where the gate leads back. Null outside the pasture, so no save version was raised.
/// </summary>
public sealed class PastureSave
{
    /// <summary>The box shown in the meadow, from nought.</summary>
    public int Box { get; set; }

    /// <summary>The map of the PC the player came from, and the tile before it.</summary>
    public string Map { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public Direction Facing { get; set; } = Direction.Up;

    public PastureSave Copy() => new() { Box = Box, Map = Map, X = X, Y = Y, Facing = Facing };
}

/// <summary>Why a Pokémon of the meadow can't be taken along.</summary>
public enum PastureRefusal { None, TeamFull, Run, Egg }

/// <summary>
/// The pasture (plan 35 · BN5; docs/mechanics/rulings.md, "The pasture"): a meadow of our own, one plan copied into each
/// finished region (its region file's <c>pasture</c>), reached from every PC and left by its gate back to that PC. One box
/// of the PC walks it at a time. GPU-free: where the herd stands is worked out by <see cref="SplitMix"/> from the box and
/// the slot alone (the same box in the same places every visit, nothing drawn from <c>Dice</c>), and so is the day's find.
/// </summary>
public static class Pasture
{
    /// <summary>The gate: where the player is put down coming in, facing the meadow.</summary>
    public const int GateX = 13, GateY = 17;

    /// <summary>
    /// The herd's homes are north of this row, so that wandering <see cref="Roam"/> tiles from home never reaches the fence's
    /// gap (row 16) nor the gate: the way in and out is never blocked.
    /// </summary>
    public const int GateRows = 13;

    /// <summary>One Pokémon in this many is holding something on a given day.</summary>
    public const int FindOneIn = 8;

    /// <summary>How far each Pokémon of the herd wanders from where it was placed.</summary>
    public const int Roam = 2;

    /// <summary>The id the herd's bodies carry, with their slot after it.</summary>
    public const string HerdPrefix = "pasture_herd_";

    /// <summary>The pasture of a region, by its region file; null for a region that has none.</summary>
    public static string? MapOf(string region) => RegionDatabase.Get(region)?.File?.Pasture is { Length: > 0 } map ? map : null;

    /// <summary>Whether a map is a region's pasture.</summary>
    public static bool IsPasture(string mapName) =>
        RegionDatabase.All.Any(r => string.Equals(r.File?.Pasture, mapName, StringComparison.OrdinalIgnoreCase));

    /// <summary>The id the bodies of those away on a job carry, with their place on the board after it (plan 35 · BN6).</summary>
    public const string WorkerPrefix = "pasture_job_";

    /// <summary>The board by the shed, which is read from the tile south of it.</summary>
    public const int BoardX = 19, BoardY = 13;

    /// <summary>The orchard's pickers stand on this row under its trees, from <see cref="OrchardFrom"/> to <see cref="OrchardTo"/> (plan 35 · BN6).</summary>
    public const int OrchardRow = 9, OrchardFrom = 3, OrchardTo = 9;

    /// <summary>The hot spring's pool, where its bathers wade (plan 35 · BN6).</summary>
    public static readonly IReadOnlyList<(int X, int Y)> SpringPool = new[] { (4, 12), (5, 12), (6, 12), (4, 13), (5, 13), (6, 13) };

    /// <summary>
    /// The tiles of the meadow a Pokémon may be put on: open, not water, on grass or flowers, north of the gate's rows, and
    /// none of the orchard's pickers' places nor the tile the board is read from. In reading order; the same every visit.
    /// </summary>
    public static List<(int X, int Y)> OpenTiles(Map map)
    {
        var tiles = new List<(int X, int Y)>();
        for (int y = 1; y < Math.Min(GateRows, map.Height - 1); y++)
            for (int x = 1; x < map.Width - 1; x++)
            {
                if (map.IsSolid(x, y)) continue;
                var tile = map.GetGroundTile(x, y);
                if (tile is not (TileType.Grass or TileType.FlowerGrass)) continue;
                if (y == OrchardRow && x >= OrchardFrom && x <= OrchardTo) continue;
                if ((x, y) == (BoardX, BoardY + 1)) continue;
                tiles.Add((x, y));
            }
        return tiles;
    }

    /// <summary>
    /// Where each Pokémon of a box stands: its slot's number mixed with the box's picks a tile among the open ones, the
    /// next free one along where that is taken. Eggs stay in the box (they don't walk). The same box gives the same places.
    /// </summary>
    public static List<(int Slot, Pokemon Pokemon, int X, int Y)> Place(IReadOnlyList<(int X, int Y)> open, int box, IReadOnlyList<Pokemon?> slots)
    {
        var placed = new List<(int, Pokemon, int, int)>();
        if (open.Count == 0) return placed;
        var taken = new HashSet<int>();
        for (int slot = 0; slot < slots.Count; slot++)
        {
            if (slots[slot] is not { IsEgg: false } p) continue;
            if (taken.Count >= open.Count) break;
            int at = (int)(SplitMix.Mix(0x9A57u, (uint)box, (uint)slot) % (uint)open.Count);
            while (!taken.Add(at)) at = (at + 1) % open.Count;
            placed.Add((slot, p, open[at].X, open[at].Y));
        }
        return placed;
    }

    /// <summary>What may be found on a Pokémon of the meadow, by region: berries, a treat, now and then a Heart Scale (ours).</summary>
    public static IReadOnlyList<string> FindTable(string region) => region switch
    {
        RegionDatabase.Kanto => new[] { "Oran Berry", "Pecha Berry", "Persim Berry", "Sitrus Berry", "Honey Drop", "Crisp Wafer", "Tart Chew", "Heart Scale" },
        RegionDatabase.Johto => new[] { "Oran Berry", "Cheri Berry", "Chesto Berry", "Sitrus Berry", "Cocoa Nib", "Honey Drop", "Pepper Bite", "Heart Scale" },
        RegionDatabase.Hoenn => new[] { "Oran Berry", "Rawst Berry", "Aspear Berry", "Sitrus Berry", "Tart Chew", "Pepper Bite", "Crisp Wafer", "Heart Scale" },
        RegionDatabase.Unova => new[] { "Oran Berry", "Pecha Berry", "Leppa Berry", "Sitrus Berry", "Cocoa Nib", "Crisp Wafer", "Honey Drop", "Heart Scale" },
        _ => new[] { "Oran Berry", "Pecha Berry", "Cheri Berry", "Sitrus Berry", "Honey Drop", "Pepper Bite", "Cocoa Nib", "Heart Scale" }
    };

    /// <summary>
    /// What a Pokémon of the meadow holds today, if anything: one in <see cref="FindOneIn"/>, by the day and its id mixed,
    /// then a row of the region's table by a second mix. The same day and Pokémon always give the same answer.
    /// </summary>
    public static string? FindOf(int day, Guid id, string region)
    {
        uint who = SplitMix.Fnv(id.ToString("N"));
        if (SplitMix.Mix((uint)day, who, 0xF1u) % FindOneIn != 0) return null;
        var table = FindTable(region);
        return table[(int)(SplitMix.Mix((uint)day, who, 0xF2u) % (uint)table.Count)];
    }

    /// <summary>
    /// Why this Pokémon can't be taken along now, as the PC would refuse it: an Egg (it stays in the box, and never walks),
    /// a team at its run's size, or a run's rule that it may not be carried (a Monotype).
    /// A Nuzlocke's fallen are in no box, so they are never in the meadow.
    /// </summary>
    public static PastureRefusal WhyNotTake(Pokemon p, Party party, RunRules run, string? starter)
    {
        if (p.IsEgg) return PastureRefusal.Egg;
        if (party.IsFull) return PastureRefusal.TeamFull;
        if (!run.MayCarry(p, starter)) return PastureRefusal.Run;
        return PastureRefusal.None;
    }

    /// <summary>
    /// Where the <paramref name="nth"/> Pokémon away on a job of this kind is seen at its work (plan 35 · BN6): under the
    /// orchard's trees, wading in the hot spring's pool. Null for a job done out of sight (an errand in town, an Egg in the
    /// nursery's shed, a dig inside the cave).
    /// </summary>
    public static (int X, int Y)? WorkerPlace(JobKind kind, int nth) => kind switch
    {
        JobKind.Orchard when nth <= OrchardTo - OrchardFrom => (OrchardFrom + nth, OrchardRow),
        JobKind.HotSpring when nth < SpringPool.Count => SpringPool[nth],
        _ => null
    };

    /// <summary>The next box along from <paramref name="box"/>, by <paramref name="delta"/>, wrapping round the PC's boxes.</summary>
    public static int Turn(int box, int delta, int count) => count <= 0 ? 0 : ((box + delta) % count + count) % count;
}
