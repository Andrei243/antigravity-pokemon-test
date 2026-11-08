using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.OpenWorld;

namespace PokemonPlatinumEngine.Graphics.OpenWorld;

/// <summary>
/// A mesh's vertices as the card takes them (plan 32 · OW8), three to a triangle: where each stands, its normal, its
/// two texture coordinates and its colour, with the box round them. Made on any thread; <see cref="Pack"/> puts them in
/// a raylib mesh's own memory (still any thread) and <c>Gpu.Upload</c> sends it to the card on the window's.
/// </summary>
internal sealed class OpenVertices
{
    public Vector3[] Positions = Array.Empty<Vector3>();
    public Vector3[] Normals = Array.Empty<Vector3>();
    public Vector2[] Uvs = Array.Empty<Vector2>();
    public Color[] Colours = Array.Empty<Color>();
    public int Count;
    public Vector3 Min = new(float.MaxValue), Max = new(float.MinValue);

    public int Triangles => Count / 3;

    public OpenVertices(int capacity)
    {
        Positions = new Vector3[capacity];
        Normals = new Vector3[capacity];
        Uvs = new Vector2[capacity];
        Colours = new Color[capacity];
    }

    public void Add(Vector3 p, Vector3 n, Vector2 uv, Color c)
    {
        if (Count == Positions.Length) Grow();
        Positions[Count] = p;
        Normals[Count] = n;
        Uvs[Count] = uv;
        Colours[Count] = c;
        Count++;
        Min = Vector3.Min(Min, p);
        Max = Vector3.Max(Max, p);
    }

    private void Grow()
    {
        int size = Math.Max(16, Positions.Length * 2);
        Array.Resize(ref Positions, size);
        Array.Resize(ref Normals, size);
        Array.Resize(ref Uvs, size);
        Array.Resize(ref Colours, size);
    }

    /// <summary>The vertices in a raylib mesh's own memory, not yet on the card: any thread may do it.</summary>
    public Mesh Pack()
    {
        var mesh = new Mesh(Count, Count / 3);
        mesh.AllocVertices();
        mesh.AllocNormals();
        mesh.AllocTexCoords();
        mesh.AllocColors();
        Positions.AsSpan(0, Count).CopyTo(mesh.VerticesAs<Vector3>());
        Normals.AsSpan(0, Count).CopyTo(mesh.NormalsAs<Vector3>());
        Uvs.AsSpan(0, Count).CopyTo(mesh.TexCoordsAs<Vector2>());
        Colours.AsSpan(0, Count).CopyTo(mesh.ColorsAs<Color>());
        return mesh;
    }
}

/// <summary>
/// A baked chunk's ground and water as the open world's programs draw them (plan 32 · OW8; style guide, "Open world"),
/// worked out from the chunk alone, GPU-free, on the streamer's worker. The ground's triangles are each given three
/// vertices of their own, which carry the atlas rows of the triangle's three corners' covers in their first texture
/// coordinate (the first corner's row, then the second's times 16 and the third's) and their own corner in their colour
/// (red, green, blue), so the program weighs the three covers at every texel and takes the greatest: the border between
/// two covers is drawn texel by texel, never blended. The water is laid over the bake's 2 m cells with the depth over
/// the chunk's own ground in its first texture coordinate.
/// </summary>
internal static class OpenTerrainMesh
{
    /// <summary>A corner's colour: which of the triangle's three it is.</summary>
    public static readonly Color[] Corners = { new(255, 0, 0, 255), new(0, 255, 0, 255), new(0, 0, 255, 255) };

    /// <summary>The covers of a triangle's corners as its vertices carry them: the atlas rows, the first alone and the other two in one number.</summary>
    public static Vector2 Covers(OpenCover a, OpenCover b, OpenCover c) =>
        new(OpenCoverArt.SlotOf(a), OpenCoverArt.SlotOf(b) * 16 + OpenCoverArt.SlotOf(c));

    /// <summary>The atlas rows back out of <see cref="Covers"/>, as the program reads them.</summary>
    public static (int A, int B, int C) SlotsOf(Vector2 uv)
    {
        int a = (int)MathF.Round(uv.X), bc = (int)MathF.Round(uv.Y);
        return (a, bc / 16, bc % 16);
    }

    private static Vector3 Pos(BakedChunk c, int v) =>
        new(c.Positions[v * 3] / (float)OpenPos.PerMetre, c.Positions[v * 3 + 1] / (float)OpenPos.PerMetre, c.Positions[v * 3 + 2] / (float)OpenPos.PerMetre);

    private static Vector3 Nrm(BakedChunk c, int v)
    {
        var n = new Vector3(c.Normals[v * 3], c.Normals[v * 3 + 1], c.Normals[v * 3 + 2]) / 127f;
        float l = n.Length();
        return l > 1e-4f ? n / l : Vector3.UnitY;
    }

    /// <summary>
    /// The chunk's ground and its apron (what is drawn and cast into the shadows), and its skirts apart (drawn only),
    /// null where it has none.
    /// </summary>
    public static (OpenVertices Surface, OpenVertices? Skirts) Of(BakedChunk chunk)
    {
        int surface = chunk.GroundTriangles + chunk.ApronTriangles;
        var top = new OpenVertices(surface * 3);
        OpenVertices? hung = chunk.SkirtTriangles > 0 ? new OpenVertices(chunk.SkirtTriangles * 3) : null;
        for (int t = 0; t < chunk.TriangleCount; t++)
        {
            var into = t < surface ? top : hung!;
            int ia = chunk.Indices[t * 3], ib = chunk.Indices[t * 3 + 1], ic = chunk.Indices[t * 3 + 2];
            var covers = Covers(chunk.Covers[ia], chunk.Covers[ib], chunk.Covers[ic]);
            into.Add(Pos(chunk, ia), Nrm(chunk, ia), covers, Corners[0]);
            into.Add(Pos(chunk, ib), Nrm(chunk, ib), covers, Corners[1]);
            into.Add(Pos(chunk, ic), Nrm(chunk, ic), covers, Corners[2]);
        }
        return (top, hung);
    }

    /// <summary>The height of the water's surface over the bake's cells is the bake's; where nothing but dry ground lies under a point, it is this deep.</summary>
    public const float NoGroundDepth = 4f;

    /// <summary>
    /// The chunk's water (style guide, "Open world", "Water"), or null where it has none: every 2 m cell with water and
    /// every cell beside one, each corner's height the mean of the water cells round it (so a river's slope has no
    /// steps), laid at 0.5 m at level 0 and at 2 m at the far levels; the first texture coordinate is the depth over the
    /// chunk's own ground under each vertex (less than nought where it is dry, which the program never draws).
    /// </summary>
    public static OpenVertices? WaterOf(BakedChunk chunk)
    {
        if (!chunk.HasWater) return null;
        const int cells = BakedChunk.WaterCells;
        float side = OpenMap.ChunkSide / (float)OpenPos.PerMetre, cell = side / cells;
        float x0 = chunk.ChunkX * side, z0 = chunk.ChunkZ * side;

        bool Wet(int i, int k) => i >= 0 && k >= 0 && i < cells && k < cells && chunk.WaterAt(i, k) != BakedChunk.NoWater;
        float Level(int i, int k) => chunk.WaterAt(i, k) / (float)OpenPos.PerMetre;

        // Which cells are laid, and a dry one's level: the highest of the water beside it
        var laid = new float?[cells, cells];
        for (int k = 0; k < cells; k++)
            for (int i = 0; i < cells; i++)
            {
                if (Wet(i, k)) { laid[i, k] = Level(i, k); continue; }
                float? best = null;
                for (int dk = -1; dk <= 1; dk++)
                    for (int di = -1; di <= 1; di++)
                        if (Wet(i + di, k + dk)) best = best == null ? Level(i + di, k + dk) : MathF.Max(best.Value, Level(i + di, k + dk));
                laid[i, k] = best;
            }

        // Each corner of the cells: the mean of the water cells round it, or else of the laid cells round it
        var corner = new float[cells + 1, cells + 1];
        for (int ck = 0; ck <= cells; ck++)
            for (int ci = 0; ci <= cells; ci++)
            {
                float wet = 0, any = 0;
                int wetCount = 0, anyCount = 0;
                for (int dk = -1; dk <= 0; dk++)
                    for (int di = -1; di <= 0; di++)
                    {
                        int i = ci + di, k = ck + dk;
                        if (i < 0 || k < 0 || i >= cells || k >= cells) continue;
                        if (Wet(i, k)) { wet += Level(i, k); wetCount++; }
                        if (laid[i, k] is { } l) { any += l; anyCount++; }
                    }
                corner[ci, ck] = wetCount > 0 ? wet / wetCount : anyCount > 0 ? any / anyCount : 0f;
            }

        // The points the water is laid through, and the ground under each
        int sub = chunk.Level == 0 ? 4 : 1, points = cells * sub + 1;
        float step = cell / sub;
        var surface = new float[points, points];
        for (int pk = 0; pk < points; pk++)
            for (int pi = 0; pi < points; pi++)
            {
                int ci = Math.Min(pi / sub, cells - 1), ck = Math.Min(pk / sub, cells - 1);
                float tx = (pi - ci * sub) / (float)sub, tz = (pk - ck * sub) / (float)sub;
                float a = corner[ci, ck], b = corner[ci + 1, ck], c = corner[ci, ck + 1], d = corner[ci + 1, ck + 1];
                surface[pi, pk] = a + (b - a) * tx + (c - a) * tz + (a - b - c + d) * tx * tz;
            }
        var ground = GroundUnder(chunk, x0, z0, step, points, surface);

        var water = new OpenVertices(4096);
        var up = Vector3.UnitY;
        for (int k = 0; k < cells; k++)
            for (int i = 0; i < cells; i++)
            {
                if (laid[i, k] == null) continue;
                for (int sk = 0; sk < sub; sk++)
                    for (int si = 0; si < sub; si++)
                    {
                        int pi = i * sub + si, pk = k * sub + sk;
                        Vector3 P(int a, int b) => new(x0 + a * step, surface[a, b], z0 + b * step);
                        Vector2 D(int a, int b) => new(float.IsNaN(ground[a, b]) ? NoGroundDepth : surface[a, b] - ground[a, b], 0f);
                        // Two triangles facing up: (a, d, c) and (a, c, b) of the quad a b c d, west to east and north to south
                        water.Add(P(pi, pk), up, D(pi, pk), Color.White);
                        water.Add(P(pi, pk + 1), up, D(pi, pk + 1), Color.White);
                        water.Add(P(pi + 1, pk + 1), up, D(pi + 1, pk + 1), Color.White);
                        water.Add(P(pi, pk), up, D(pi, pk), Color.White);
                        water.Add(P(pi + 1, pk + 1), up, D(pi + 1, pk + 1), Color.White);
                        water.Add(P(pi + 1, pk), up, D(pi + 1, pk), Color.White);
                    }
            }
        return water.Count > 0 ? water : null;
    }

    /// <summary>
    /// The ground under each point the water is laid through: the highest of the chunk's own ground and apron that
    /// stands no more than 5 cm over the water's surface there (so the bed under a rock bridge, not the bridge), or,
    /// where none does, the lowest that stands over it (a bank: the depth there is less than nought, so the depth comes
    /// to nought where the bank comes up out of the water and the shallows and the foam lie along the shore); NaN where
    /// the chunk has no ground over the point at all.
    /// </summary>
    private static float[,] GroundUnder(BakedChunk chunk, float x0, float z0, float step, int points, float[,] surface)
    {
        var under = new float[points, points];
        var over = new float[points, points];
        for (int k = 0; k < points; k++)
            for (int i = 0; i < points; i++) under[i, k] = over[i, k] = float.NaN;
        int triangles = chunk.GroundTriangles + chunk.ApronTriangles;
        for (int t = 0; t < triangles; t++)
        {
            var a = Pos(chunk, chunk.Indices[t * 3]);
            var b = Pos(chunk, chunk.Indices[t * 3 + 1]);
            var c = Pos(chunk, chunk.Indices[t * 3 + 2]);
            int i0 = Math.Max(0, (int)MathF.Ceiling((MathF.Min(a.X, MathF.Min(b.X, c.X)) - x0) / step));
            int i1 = Math.Min(points - 1, (int)MathF.Floor((MathF.Max(a.X, MathF.Max(b.X, c.X)) - x0) / step));
            int k0 = Math.Max(0, (int)MathF.Ceiling((MathF.Min(a.Z, MathF.Min(b.Z, c.Z)) - z0) / step));
            int k1 = Math.Min(points - 1, (int)MathF.Floor((MathF.Max(a.Z, MathF.Max(b.Z, c.Z)) - z0) / step));
            for (int k = k0; k <= k1; k++)
                for (int i = i0; i <= i1; i++)
                {
                    float x = x0 + i * step, z = z0 + k * step;
                    if (HeightIn(x, z, a, b, c) is not { } y) continue;
                    if (y > surface[i, k] + 0.05f)
                    {
                        if (float.IsNaN(over[i, k]) || y < over[i, k]) over[i, k] = y;
                    }
                    else if (float.IsNaN(under[i, k]) || y > under[i, k]) under[i, k] = y;
                }
        }
        for (int k = 0; k < points; k++)
            for (int i = 0; i < points; i++)
                if (float.IsNaN(under[i, k])) under[i, k] = over[i, k];
        return under;
    }

    /// <summary>The height of a triangle over a column, if the column passes through it (its edges included).</summary>
    public static float? HeightIn(float x, float z, Vector3 a, Vector3 b, Vector3 c)
    {
        float d = (b.Z - c.Z) * (a.X - c.X) + (c.X - b.X) * (a.Z - c.Z);
        if (MathF.Abs(d) < 1e-9f) return null;
        float l1 = ((b.Z - c.Z) * (x - c.X) + (c.X - b.X) * (z - c.Z)) / d;
        float l2 = ((c.Z - a.Z) * (x - c.X) + (a.X - c.X) * (z - c.Z)) / d;
        float l3 = 1 - l1 - l2;
        const float eps = -1e-5f;
        if (l1 < eps || l2 < eps || l3 < eps) return null;
        return l1 * a.Y + l2 * b.Y + l3 * c.Y;
    }

    /// <summary>The heights of a chunk's vertices that lie within half a metre of its edges on a side (for the plain beyond a region's last chunks).</summary>
    public static List<float> EdgeHeights(BakedChunk chunk, bool west, bool east, bool north, bool south)
    {
        var heights = new List<float>();
        float side = OpenMap.ChunkSide / (float)OpenPos.PerMetre;
        float x0 = chunk.ChunkX * side, z0 = chunk.ChunkZ * side;
        int ground = chunk.GroundTriangles;
        var seen = new HashSet<int>();
        for (int t = 0; t < ground; t++)
            for (int j = 0; j < 3; j++)
            {
                int v = chunk.Indices[t * 3 + j];
                if (!seen.Add(v)) continue;
                var p = Pos(chunk, v);
                bool edge = (west && p.X - x0 < 0.5f) || (east && x0 + side - p.X < 0.5f) || (north && p.Z - z0 < 0.5f) || (south && z0 + side - p.Z < 0.5f);
                if (edge) heights.Add(p.Y);
            }
        return heights;
    }
}
