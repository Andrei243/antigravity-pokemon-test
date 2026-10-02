using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// An indexed triangle mesh made from an <see cref="SdfModel"/>: smooth normals from the distance field, blended
/// vertex colours, a surface material per vertex and up to four bone weights per vertex. CPU-side only.
/// </summary>
internal sealed class SdfMesh
{
    public Vector3[] Positions = Array.Empty<Vector3>();
    public Vector3[] Normals = Array.Empty<Vector3>();
    public Color[] Colors = Array.Empty<Color>();
    public byte[] Materials = Array.Empty<byte>();

    /// <summary>Four bone indices per vertex.</summary>
    public byte[] BoneIndices = Array.Empty<byte>();

    /// <summary>Four weights per vertex, summing to 1.</summary>
    public float[] BoneWeights = Array.Empty<float>();

    /// <summary>Three per triangle, counter-clockwise seen from outside.</summary>
    public int[] Indices = Array.Empty<int>();

    public int VertexCount => Positions.Length;
    public int TriangleCount => Indices.Length / 3;

    /// <summary>The weight vertex <paramref name="v"/> gives <paramref name="bone"/>.</summary>
    public float WeightOf(int v, int bone)
    {
        float w = 0f;
        for (int k = 0; k < 4; k++)
            if (BoneIndices[v * 4 + k] == bone) w += BoneWeights[v * 4 + k];
        return w;
    }
}

/// <summary>
/// Turns a distance field into triangles with surface nets: one vertex in every grid cell the surface passes
/// through (the average of where it crosses the cell's edges, then pulled onto the surface), and a quad across
/// every edge it crosses. Only cells near the surface are sampled: a coarse pass over blocks of 4×4×4 cells finds
/// them first. Sampling runs on every core.
/// </summary>
internal static class SdfMesher
{
    private const int Block = 4;

    /// <param name="iso">
    /// Mesh the surface this far outside the model instead of the model's own (an offset shell: outlines that
    /// can never cut into the model, however deep its creases).
    /// </param>
    public static SdfMesh Mesh(SdfModel model, float cell, float iso = 0f)
    {
        model.Prepare();
        var (bmin, bmax) = model.Bounds();
        var origin = bmin - new Vector3(cell * 2f + iso);
        var extent = bmax + new Vector3(cell * 2f + iso) - origin;
        float Field(Vector3 p) => model.Distance(p) - iso;

        int bx = Math.Max(1, (int)MathF.Ceiling(extent.X / (cell * Block)));
        int by = Math.Max(1, (int)MathF.Ceiling(extent.Y / (cell * Block)));
        int bz = Math.Max(1, (int)MathF.Ceiling(extent.Z / (cell * Block)));
        int nx = bx * Block, ny = by * Block, nz = bz * Block;      // cells
        int cx = nx + 1, cy = ny + 1, cz = nz + 1;                  // corners

        Vector3 Corner(int i, int j, int k) => origin + new Vector3(i, j, k) * cell;
        int CornerIndex(int i, int j, int k) => (k * cy + j) * cx + i;

        // 1. Coarse pass: the field at every block corner
        int qx = bx + 1, qy = by + 1, qz = bz + 1;
        var coarse = new float[qx * qy * qz];
        Parallel.For(0, qz, k =>
        {
            for (int j = 0; j < qy; j++)
                for (int i = 0; i < qx; i++)
                    coarse[(k * qy + j) * qx + i] = Field(Corner(i * Block, j * Block, k * Block));
        });

        // A block can only hold surface if a corner is nearer to it than the block's diagonal (with a margin for
        // shapes whose distance is an estimate)
        float reach = 1.3f * Block * cell * MathF.Sqrt(3f);
        var active = new bool[bx * by * bz];
        for (int k = 0; k < bz; k++)
            for (int j = 0; j < by; j++)
                for (int i = 0; i < bx; i++)
                {
                    float nearest = float.MaxValue;
                    for (int c = 0; c < 8; c++)
                    {
                        float v = coarse[((k + (c >> 2 & 1)) * qy + j + (c >> 1 & 1)) * qx + i + (c & 1)];
                        nearest = Math.Min(nearest, MathF.Abs(v));
                    }
                    active[(k * by + j) * bx + i] = nearest <= reach;
                }

        // 2. Fine pass: every corner of the active blocks
        var field = new float[cx * cy * cz];
        Array.Fill(field, float.NaN);
        var activeList = new List<(int, int, int)>();
        for (int k = 0; k < bz; k++)
            for (int j = 0; j < by; j++)
                for (int i = 0; i < bx; i++)
                    if (active[(k * by + j) * bx + i]) activeList.Add((i, j, k));

        Parallel.ForEach(activeList, b =>
        {
            var (i0, j0, k0) = b;
            for (int k = k0 * Block; k <= (k0 + 1) * Block; k++)
                for (int j = j0 * Block; j <= (j0 + 1) * Block; j++)
                    for (int i = i0 * Block; i <= (i0 + 1) * Block; i++)
                    {
                        int idx = CornerIndex(i, j, k);
                        if (float.IsNaN(field[idx])) field[idx] = Field(Corner(i, j, k));
                    }
        });

        // 3. A vertex in every cell the surface crosses
        var cellVertex = new int[nx * ny * nz];
        Array.Fill(cellVertex, -1);
        var positions = new List<Vector3>();
        int CellIndex(int i, int j, int k) => (k * ny + j) * nx + i;

        Span<float> v8 = stackalloc float[8];
        foreach (var (bi, bj, bk) in activeList)
        {
            for (int k = bk * Block; k < (bk + 1) * Block; k++)
                for (int j = bj * Block; j < (bj + 1) * Block; j++)
                    for (int i = bi * Block; i < (bi + 1) * Block; i++)
                    {
                        int inside = 0;
                        bool valid = true;
                        for (int c = 0; c < 8; c++)
                        {
                            float v = field[CornerIndex(i + (c & 1), j + (c >> 1 & 1), k + (c >> 2 & 1))];
                            if (float.IsNaN(v)) { valid = false; break; }
                            v8[c] = v;
                            if (v < 0f) inside++;
                        }
                        if (!valid || inside == 0 || inside == 8) continue;

                        // Average of the points where the surface crosses the cell's twelve edges
                        var sum = Vector3.Zero;
                        int crossings = 0;
                        foreach (var (a, b) in Edges)
                        {
                            float va = v8[a], vb = v8[b];
                            if ((va < 0f) == (vb < 0f)) continue;
                            float t = va / (va - vb);
                            var pa = new Vector3(a & 1, a >> 1 & 1, a >> 2 & 1);
                            var pb = new Vector3(b & 1, b >> 1 & 1, b >> 2 & 1);
                            sum += Vector3.Lerp(pa, pb, t);
                            crossings++;
                        }
                        cellVertex[CellIndex(i, j, k)] = positions.Count;
                        positions.Add(Corner(i, j, k) + sum / crossings * cell);
                    }
        }

        // 4. A quad across every crossed edge, joining the four cells round it
        var indices = new List<int>();
        // The four cells are listed counter-clockwise round +x and +z, clockwise round +y; flip so the quad faces
        // from the inside corner to the outside one, then split it along the shorter diagonal
        void Quad(int c0, int c1, int c2, int c3, bool flip)
        {
            if (c0 < 0 || c1 < 0 || c2 < 0 || c3 < 0) return;
            if (flip) (c1, c3) = (c3, c1);
            var p0 = positions[c0]; var p1 = positions[c1]; var p2 = positions[c2]; var p3 = positions[c3];
            if ((p2 - p0).LengthSquared() <= (p3 - p1).LengthSquared())
            {
                indices.Add(c0); indices.Add(c1); indices.Add(c2);
                indices.Add(c0); indices.Add(c2); indices.Add(c3);
            }
            else
            {
                indices.Add(c0); indices.Add(c1); indices.Add(c3);
                indices.Add(c1); indices.Add(c2); indices.Add(c3);
            }
        }

        int Cell(int i, int j, int k) => i < 0 || j < 0 || k < 0 || i >= nx || j >= ny || k >= nz ? -1 : cellVertex[CellIndex(i, j, k)];

        foreach (var (bi, bj, bk) in activeList)
        {
            for (int k = bk * Block; k < (bk + 1) * Block; k++)
                for (int j = bj * Block; j < (bj + 1) * Block; j++)
                    for (int i = bi * Block; i < (bi + 1) * Block; i++)
                    {
                        float v0 = field[CornerIndex(i, j, k)];
                        if (float.IsNaN(v0)) continue;
                        bool in0 = v0 < 0f;

                        // The edges leaving this corner along +x, +y and +z
                        float vx = field[CornerIndex(i + 1, j, k)];
                        if (!float.IsNaN(vx) && (vx < 0f) != in0)
                            Quad(Cell(i, j - 1, k - 1), Cell(i, j, k - 1), Cell(i, j, k), Cell(i, j - 1, k), flip: !in0);
                        float vy = field[CornerIndex(i, j + 1, k)];
                        if (!float.IsNaN(vy) && (vy < 0f) != in0)
                            Quad(Cell(i - 1, j, k - 1), Cell(i, j, k - 1), Cell(i, j, k), Cell(i - 1, j, k), flip: in0);
                        float vz = field[CornerIndex(i, j, k + 1)];
                        if (!float.IsNaN(vz) && (vz < 0f) != in0)
                            Quad(Cell(i - 1, j - 1, k), Cell(i, j - 1, k), Cell(i, j, k), Cell(i - 1, j, k), flip: !in0);
                    }
        }

        // 5. Pull each vertex onto the surface, then read its normal, colour, material and weights there
        int count = positions.Count;
        var mesh = new SdfMesh
        {
            Positions = positions.ToArray(),
            Normals = new Vector3[count],
            Colors = new Color[count],
            Materials = new byte[count],
            BoneIndices = new byte[count * 4],
            BoneWeights = new float[count * 4],
            Indices = indices.ToArray()
        };
        float eps = cell * 0.5f;
        int bones = Math.Max(1, model.BoneCount);
        Parallel.For(0, count, () => new float[bones], (v, _, weights) =>
        {
            var p = mesh.Positions[v];
            var corner = p;
            for (int step = 0; step < 2; step++)
            {
                float d = Field(p);
                var g = Gradient(model, p, eps);
                float g2 = g.LengthSquared();
                if (g2 < 1e-12f) break;
                var next = p - g * (d / g2);
                // Never leave the neighbourhood of the cell the vertex was made in
                var offset = next - corner;
                if (offset.Length() > cell) next = corner + Vector3.Normalize(offset) * cell;
                p = next;
            }
            mesh.Positions[v] = p;
            var n = Gradient(model, p, eps);
            mesh.Normals[v] = n.LengthSquared() > 1e-12f ? Vector3.Normalize(n) : Vector3.UnitY;

            var surface = model.Surface(p, weights);
            var col = surface.Color;
            mesh.Colors[v] = new Color((int)MathF.Round(Math.Clamp(col.X, 0f, 1f) * 255f), (int)MathF.Round(Math.Clamp(col.Y, 0f, 1f) * 255f),
                (int)MathF.Round(Math.Clamp(col.Z, 0f, 1f) * 255f), 255);
            mesh.Materials[v] = (byte)surface.Material;
            TopFour(weights, mesh.BoneIndices.AsSpan(v * 4, 4), mesh.BoneWeights.AsSpan(v * 4, 4));
            return weights;
        }, _ => { });

        return mesh;
    }

    /// <summary>Gradient of the field by central differences.</summary>
    public static Vector3 Gradient(SdfModel model, Vector3 p, float eps)
    {
        var dx = new Vector3(eps, 0, 0);
        var dy = new Vector3(0, eps, 0);
        var dz = new Vector3(0, 0, eps);
        return new Vector3(
            model.Distance(p + dx) - model.Distance(p - dx),
            model.Distance(p + dy) - model.Distance(p - dy),
            model.Distance(p + dz) - model.Distance(p - dz)) / (2f * eps);
    }

    /// <summary>Keeps the four heaviest bones (dropping weights under 2 %) and renormalises them.</summary>
    private static void TopFour(float[] weights, Span<byte> indices, Span<float> top)
    {
        indices.Clear();
        top.Clear();
        for (int b = 0; b < weights.Length; b++)
        {
            float w = weights[b];
            if (w < 0.02f) continue;
            int slot = -1;
            for (int k = 0; k < 4; k++)
                if (top[k] < w && (slot < 0 || top[k] < top[slot])) slot = k;
            if (slot < 0) continue;
            top[slot] = w;
            indices[slot] = (byte)b;
        }
        float sum = top[0] + top[1] + top[2] + top[3];
        if (sum <= 1e-6f)
        {
            top[0] = 1f;
            return;
        }
        for (int k = 0; k < 4; k++) top[k] /= sum;
    }

    // The twelve edges of a cell, as pairs of corner numbers (bit 0 = x, bit 1 = y, bit 2 = z)
    private static readonly (int A, int B)[] Edges =
    {
        (0, 1), (2, 3), (4, 5), (6, 7),
        (0, 2), (1, 3), (4, 6), (5, 7),
        (0, 4), (1, 5), (2, 6), (3, 7)
    };
}
