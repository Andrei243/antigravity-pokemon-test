using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using PokemonPlatinumEngine.Models.Cabinets;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>A species' picture for the picture-logic cabinet: its grid size and its cells, row by row (true filled).</summary>
internal sealed record SilhouettePicture(int Size, bool[] Cells);

/// <summary>
/// Silhouettes for the picture-logic cabinet (plan 42 · PS6; style guide, "The arcade"). A species' silhouette is made
/// from its own model, never taken from the games: every triangle of its mesh seen from its left side (the body's
/// length across, its height up), laid into a square grid with the longer side filling it and the shorter centred
/// (<see cref="Make"/>, GPU-free). A species is offered at the smallest of <see cref="PictureLogic.Sizes"/> whose
/// silhouette line logic finishes alone (<see cref="Offer"/>), and at none when no size does. Kept this run by species;
/// a model built only for a silhouette is borrowed and given back, as a footprint's is.
/// </summary>
internal static class Silhouettes
{
    /// <summary>The fewest cells a silhouette fills, as a share of its grid: less is a scatter, not a picture.</summary>
    public const float LeastShare = 0.12f;

    /// <summary>A model's silhouette at a grid <paramref name="size"/> cells square. The same model always gives the same cells.</summary>
    public static bool[] Make(PokeModel model, int size)
    {
        var cells = new bool[size * size];
        var mesh = model.Mesh;
        if (mesh.VertexCount == 0 || mesh.TriangleCount == 0) return cells;
        float minZ = float.MaxValue, maxZ = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
        for (int v = 0; v < mesh.VertexCount; v++)
        {
            var p = mesh.Positions[v];
            minZ = MathF.Min(minZ, p.Z); maxZ = MathF.Max(maxZ, p.Z);
            minY = MathF.Min(minY, p.Y); maxY = MathF.Max(maxY, p.Y);
        }
        float span = MathF.Max(maxZ - minZ, maxY - minY);
        if (span <= 1e-5f) return cells;
        float scale = size / span;
        float x0 = (size - (maxZ - minZ) * scale) / 2f, y0 = (size - (maxY - minY) * scale) / 2f;
        var p3 = new Vector2[3];
        for (int t = 0; t < mesh.TriangleCount; t++)
        {
            for (int k = 0; k < 3; k++)
            {
                var q = mesh.Positions[mesh.Indices[t * 3 + k]];
                // The front (+Z) to the left, up at the top
                p3[k] = new Vector2(x0 + (maxZ - q.Z) * scale, y0 + (maxY - q.Y) * scale);
            }
            int left = Math.Max(0, (int)MathF.Floor(MathF.Min(p3[0].X, MathF.Min(p3[1].X, p3[2].X))));
            int right = Math.Min(size - 1, (int)MathF.Ceiling(MathF.Max(p3[0].X, MathF.Max(p3[1].X, p3[2].X))));
            int top = Math.Max(0, (int)MathF.Floor(MathF.Min(p3[0].Y, MathF.Min(p3[1].Y, p3[2].Y))));
            int bottom = Math.Min(size - 1, (int)MathF.Ceiling(MathF.Max(p3[0].Y, MathF.Max(p3[1].Y, p3[2].Y))));
            for (int y = top; y <= bottom; y++)
                for (int x = left; x <= right; x++)
                    if (!cells[y * size + x] && Inside(new Vector2(x + 0.5f, y + 0.5f), p3[0], p3[1], p3[2])) cells[y * size + x] = true;
        }
        return cells;
    }

    private static bool Inside(Vector2 q, Vector2 a, Vector2 b, Vector2 c)
    {
        static float Cross(Vector2 o, Vector2 u, Vector2 v) => (u.X - o.X) * (v.Y - o.Y) - (u.Y - o.Y) * (v.X - o.X);
        float d1 = Cross(a, b, q), d2 = Cross(b, c, q), d3 = Cross(c, a, q);
        bool negative = d1 < 0 || d2 < 0 || d3 < 0, positive = d1 > 0 || d2 > 0 || d3 > 0;
        return !(negative && positive);
    }

    /// <summary>The picture offered for a model: the smallest grid whose silhouette fills enough and solves alone, or null.</summary>
    public static SilhouettePicture? Offer(PokeModel model)
    {
        foreach (int size in PictureLogic.Sizes)
        {
            var cells = Make(model, size);
            int filled = 0;
            foreach (bool b in cells) if (b) filled++;
            if (filled < LeastShare * size * size) continue;
            if (PictureLogic.SolvesAlone(size, cells)) return new SilhouettePicture(size, cells);
        }
        return null;
    }

    // ------------------------------------------------------------------ keeping them

    private static readonly ConcurrentDictionary<string, SilhouettePicture?> Known = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, bool> Working = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> Borrowing = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Makes a picture at once from a model built on the spot: the harness sets it, so two runs draw the same pictures.</summary>
    public static bool Patient { get; set; }

    /// <summary>Whether a species' picture is known this run, and what it is (null: none solves alone).</summary>
    public static bool TryHeld(string species, out SilhouettePicture? picture) => Known.TryGetValue(species, out picture);

    /// <summary>A species' picture now, made from its model at once if need be (the model built on this thread and let go after): for a tool or a test.</summary>
    public static SilhouettePicture? Now(string species)
    {
        if (TryHeld(species, out var held)) return held;
        bool had = PokemonModels.Holds(species);
        var picture = Offer(PokemonModels.Get(species));
        Known[species] = picture;
        if (!had) PokemonModels.Release(species);
        return picture;
    }

    /// <summary>
    /// Asks for a species' picture (main thread): true once it is known (<paramref name="picture"/> null when none solves),
    /// false while its model is borrowed and the picture is made in the background.
    /// </summary>
    public static bool Ask(string species, out SilhouettePicture? picture)
    {
        if (Patient) { picture = Now(species); return true; }
        if (TryHeld(species, out picture))
        {
            if (Borrowing.Remove(species)) PokemonModels.Return(species);
            return true;
        }
        if (Working.ContainsKey(species)) return false;
        if (PokemonModels.TryGet(species, out var model))
        {
            Working[species] = true;
            Task.Run(() =>
            {
                try { Known[species] = Offer(model); }
                finally { Working.TryRemove(species, out _); }
            });
        }
        else if (Borrowing.Add(species)) PokemonModels.Borrow(species);
        return false;
    }
}
