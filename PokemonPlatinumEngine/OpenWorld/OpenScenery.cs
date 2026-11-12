using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>The two kinds of tree the open world draws (plan 32 · OW9): the battle's soft pine and its round tree.</summary>
public enum OpenTreeKind
{
    Pine,
    Round
}

/// <summary>
/// A tree of an open map (plan 32 · OW9; style guide, "Open world", "Trees"): where its foot stands on the ground, in
/// millimetres (x and z; its height is the ground's there: the highest ground facing up, or, where <see cref="Y"/> is
/// given, the ground nearest that height), its kind, its size (thousandths of its kind's own), which of its kind's
/// <see cref="Variants"/> shapes it is, and how it is turned (65,536ths). Scenery: the capsule walks through it ("OW9 as
/// built" says why), unless its place gives it a <see cref="Trunk"/> (plan 32, after OW10: a region that wants a forest
/// one walks round), which <see cref="OpenTrunks"/> makes a pillar of the collision. GPU-free.
/// </summary>
public readonly record struct OpenTree(int X, int Z, int? Y, OpenTreeKind Kind, int Size, int Variant, int Yaw)
{
    /// <summary>Whether its trunk is solid: the capsule and everyone of the map walk round it (<see cref="OpenTrunks"/>).</summary>
    public bool Trunk { get; init; }

    /// <summary>How many shapes each kind of tree has (each a seed of the soft tree's own).</summary>
    public const int Variants = 3;

    /// <summary>The largest size a tree may have, in thousandths of its kind's own.</summary>
    public const int Largest = 4000;

    /// <summary>The size as a factor.</summary>
    public float Scale => Size / 1000f;
}

/// <summary>
/// A grove of an open map (plan 32 · OW9): a polygon on the ground filled with trees on a jittered grid of
/// <see cref="Spacing"/> millimetres (laid on the world's own grid, so two groves side by side never crowd their
/// border), each cell holding a tree <see cref="Fill"/> thousandths of the time, a pine <see cref="Pines"/> thousandths
/// of the time and a round tree otherwise, sized between <see cref="MinSize"/> and <see cref="MaxSize"/>. Every choice is
/// <see cref="SplitMix.Mix"/> of the grove's seed and the cell, so a grove is the same trees on every machine and in
/// every run, whoever asks first. Where <see cref="Trunks"/> holds, each of its trees has a solid trunk. GPU-free.
/// </summary>
public sealed record OpenGrove(string Name, IReadOnlyList<(int X, int Z)> Polygon, int Spacing, int Fill, int Pines, int MinSize, int MaxSize, int Seed,
    bool Trunks = false)
{
    /// <summary>The grove's trees, row by row from the north-west.</summary>
    public List<OpenTree> Trees()
    {
        var trees = new List<OpenTree>();
        if (Polygon.Count < 3 || Spacing <= 0) return trees;
        int x0 = int.MaxValue, z0 = int.MaxValue, x1 = int.MinValue, z1 = int.MinValue;
        foreach (var (x, z) in Polygon) { x0 = Math.Min(x0, x); z0 = Math.Min(z0, z); x1 = Math.Max(x1, x); z1 = Math.Max(z1, z); }
        int c0 = FloorDiv(x0, Spacing), c1 = FloorDiv(x1, Spacing), r0 = FloorDiv(z0, Spacing), r1 = FloorDiv(z1, Spacing);
        uint seed = (uint)Seed ^ SplitMix.Fnv(Name);
        for (int r = r0; r <= r1; r++)
            for (int c = c0; c <= c1; c++)
            {
                uint h = SplitMix.Mix(seed, (uint)c, (uint)r);
                if ((h & 1023) * 1000 >= (uint)Fill * 1024) continue;
                uint g = SplitMix.Mix(seed ^ 0x5bd1e995u, (uint)c, (uint)r);
                // Somewhere in the middle seven tenths of the cell, so no two trees of neighbouring cells touch
                int x = c * Spacing + (int)((150 + (g & 1023) * 700 / 1024) * (long)Spacing / 1000);
                int z = r * Spacing + (int)((150 + ((g >> 10) & 1023) * 700 / 1024) * (long)Spacing / 1000);
                if (!OpenPolygons.Holds(Polygon, x, z)) continue;
                var kind = ((h >> 10) & 1023) * 1000 < (uint)Pines * 1024 ? OpenTreeKind.Pine : OpenTreeKind.Round;
                int size = MinSize + (int)(((g >> 20) & 1023) * (long)(MaxSize - MinSize) / 1023);
                int variant = (int)((h >> 20) % OpenTree.Variants);
                int yaw = (int)(SplitMix.Mix(seed ^ 0x27d4eb2du, (uint)c, (uint)r) & 0xFFFF);
                trees.Add(new OpenTree(x, z, null, kind, size, variant, yaw) { Trunk = Trunks });
            }
        return trees;
    }

    private static int FloorDiv(int value, int by)
    {
        int q = value / by;
        return value % by != 0 && value < 0 ? q - 1 : q;
    }
}

/// <summary>
/// How a solid of an open map is built (plan 32 · OW9; style guide, "Open world", "Buildings"): the kit's building of
/// that kind, in a town's way of building (a house's walls and roof; public buildings look the same everywhere), its
/// storeys (nought for its style's own), the way its front and door face (<see cref="Direction.Down"/> is south) and the
/// name over its door. It stands over the box the capsule collides with (<see cref="Box"/>), drawn at the kit's
/// <c>OpenBuildingKit.MetresPerTile</c>. Drawing only; GPU-free.
/// </summary>
public sealed record OpenBuilding(OpenBox Box, BuildingKind Kind, Architecture Town, int Storeys, Direction Faces, string? Sign = null);

/// <summary>
/// Solid trunks (plan 32, after OW10; style guide, "Open world", "Trees"): a tree whose place gives it a trunk is a
/// pillar of the collision, a square post of four walls and no top (so nothing stands on it and no tree's foot is found
/// on it), <see cref="HalfWidth"/> either side of its foot at size one, from <see cref="Under"/> under its foot to the
/// mount's high jump and a body over it, or <see cref="Over"/> times its size where that is more, its foot found as the trees' are drawn (the highest ground facing up, or the
/// ground nearest its height). The game's collision (<see cref="OpenGround"/>) and the baker's navigation mesh make them
/// the same way, after the map's own boxes, so a forest of them is walked round by the player, its people and its wild
/// Pokémon alike. A tree whose foot is on no ground has none. GPU-free.
/// </summary>
public static class OpenTrunks
{
    /// <summary>Half a trunk's side at size one, in millimetres: the drawn trunk's at the trees' 1.5 times the battle's.</summary>
    public const int HalfWidth = 220;

    /// <summary>How far the post reaches under the foot, and over it at size one, in millimetres (over even the mount's high jump).</summary>
    public const int Under = 300, Over = 3000;

    /// <summary>The pillars of the trees that have trunks, in the trees' order, their feet found on <paramref name="ground"/>.</summary>
    public static List<OpenBox> Pillars(IEnumerable<OpenTree> trees, OpenGround ground)
    {
        var pillars = new List<OpenBox>();
        foreach (var t in trees)
        {
            if (!t.Trunk) continue;
            double x = t.X / (double)OpenPos.PerMetre, z = t.Z / (double)OpenPos.PerMetre;
            var foot = t.Y is { } y
                ? ground.Footing(x, z, y / (double)OpenPos.PerMetre + 3, y / (double)OpenPos.PerMetre - 3)
                : ground.Footing(x, z, 2000, -2000);
            if (foot is not { } f) continue;
            int half = Math.Max(60, (int)((long)HalfWidth * t.Size / 1000));
            int footY = OpenPos.Millimetres(f.Y);
            int over = Math.Max(OpenPos.Millimetres(OpenBody.HighJumpHeight + OpenBody.Height), (int)((long)Over * t.Size / 1000));
            pillars.Add(new OpenBox($"trunk@{t.X},{t.Z}", new OpenPos(t.X - half, footY - Under, t.Z - half), new OpenPos(t.X + half, footY + over, t.Z + half)) { Pillar = true });
        }
        return pillars;
    }
}

/// <summary>An open map's scenery (plan 32 · OW9): its trees and its buildings' looks, in the order laid.</summary>
public sealed partial class OpenMap
{
    private readonly List<OpenTree> trees = new();
    private readonly List<OpenBuilding> buildings = new();

    /// <summary>The map's trees, single and of its groves, in the order laid.</summary>
    public IReadOnlyList<OpenTree> Trees => trees;

    /// <summary>The map's solids that are buildings, with how each is built; a solid not among them is drawn as a block of stone.</summary>
    public IReadOnlyList<OpenBuilding> Buildings => buildings;

    /// <summary>Adds a tree (one with a solid trunk makes the ground again the next time it is asked for, as a box does).</summary>
    public void Add(OpenTree tree)
    {
        trees.Add(tree);
        if (tree.Trunk) ground = null;
    }

    /// <summary>Adds a grove's trees.</summary>
    public void Add(OpenGrove grove)
    {
        trees.AddRange(grove.Trees());
        if (grove.Trunks) ground = null;
    }

    public void Add(OpenBuilding building) => buildings.Add(building);
}
