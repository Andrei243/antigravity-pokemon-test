using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Accumulates non-indexed triangles (position, texture coordinate, vertex color) and uploads them
/// as a single raylib mesh. Lighting is baked into vertex colors, the way DS-era field models were lit.
/// </summary>
internal sealed class MeshBuilder
{
    private readonly List<Vector3> positions = new();
    private readonly List<Vector2> uvs = new();
    private readonly List<Color> colors = new();

    // Sun from the upper left and slightly in front, so faces toward the camera stay bright
    public static readonly Vector3 SunDirection = Vector3.Normalize(new Vector3(-0.4f, 0.75f, 0.55f));

    public int VertexCount => positions.Count;

    /// <summary>Scales a color by how much a surface with this normal faces the sun.</summary>
    public static Color Lit(Color c, Vector3 normal, float ambient = 0.58f, float strength = 0.47f)
    {
        float light = ambient + strength * MathF.Max(0f, Vector3.Dot(Vector3.Normalize(normal), SunDirection));
        return Scale(c, light);
    }

    /// <summary>Like <see cref="Lit"/> but snapped to a few bands, for chunky cel-shaded foliage.</summary>
    public static Color LitBanded(Color c, Vector3 normal, float variation = 0f)
    {
        float d = Vector3.Dot(Vector3.Normalize(normal), SunDirection) + variation;
        float light = d < -0.05f ? 0.6f : d < 0.35f ? 0.76f : d < 0.7f ? 0.92f : 1.06f;
        return Scale(c, light);
    }

    public static Color Scale(Color c, float f) => new(
        (int)Math.Clamp(c.R * f, 0, 255),
        (int)Math.Clamp(c.G * f, 0, 255),
        (int)Math.Clamp(c.B * f, 0, 255),
        (int)c.A);

    public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector2 ta, Vector2 tb, Vector2 tc, Color ca, Color cb, Color cc)
    {
        positions.Add(a); uvs.Add(ta); colors.Add(ca);
        positions.Add(b); uvs.Add(tb); colors.Add(cb);
        positions.Add(c); uvs.Add(tc); colors.Add(cc);
    }

    public void Tri(Vector3 a, Vector3 b, Vector3 c, Color color) =>
        Tri(a, b, c, Vector2.Zero, Vector2.Zero, Vector2.Zero, color, color, color);

    /// <summary>
    /// Quad with corners a (bottom-left), b (bottom-right), c (top-right), d (top-left) as seen from its front.
    /// </summary>
    public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ta, Vector2 tb, Vector2 tc, Vector2 td, Color color)
    {
        Tri(a, b, c, ta, tb, tc, color, color, color);
        Tri(a, c, d, ta, tc, td, color, color, color);
    }

    /// <summary>Quad whose bottom edge (a, b) and top edge (c, d) get different colors.</summary>
    public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ta, Vector2 tb, Vector2 tc, Vector2 td, Color bottom, Color top)
    {
        Tri(a, b, c, ta, tb, tc, bottom, bottom, top);
        Tri(a, c, d, ta, tc, td, bottom, top, top);
    }

    /// <summary>Untextured quad lit by its own normal.</summary>
    public void LitQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color)
    {
        var lit = Lit(color, Normal(a, b, d));
        Quad(a, b, c, d, Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, lit);
    }

    /// <summary>Axis-aligned box with each face lit by its normal. Faces can be skipped with the flags.</summary>
    public void Box(Vector3 min, Vector3 max, Color color, bool top = true, bool bottom = false,
        bool north = true, bool south = true, bool west = true, bool east = true)
    {
        var (x0, y0, z0) = (min.X, min.Y, min.Z);
        var (x1, y1, z1) = (max.X, max.Y, max.Z);
        if (top) LitQuad(new(x0, y1, z1), new(x1, y1, z1), new(x1, y1, z0), new(x0, y1, z0), color);
        if (bottom) LitQuad(new(x0, y0, z0), new(x1, y0, z0), new(x1, y0, z1), new(x0, y0, z1), color);
        if (south) LitQuad(new(x0, y0, z1), new(x1, y0, z1), new(x1, y1, z1), new(x0, y1, z1), color);
        if (north) LitQuad(new(x1, y0, z0), new(x0, y0, z0), new(x0, y1, z0), new(x1, y1, z0), color);
        if (west) LitQuad(new(x0, y0, z0), new(x0, y0, z1), new(x0, y1, z1), new(x0, y1, z0), color);
        if (east) LitQuad(new(x1, y0, z1), new(x1, y0, z0), new(x1, y1, z0), new(x1, y1, z1), color);
    }

    /// <summary>Normal of the plane through a, b and d, following the quad corner convention.</summary>
    public static Vector3 Normal(Vector3 a, Vector3 b, Vector3 d) => Vector3.Normalize(Vector3.Cross(b - a, d - a));

    public unsafe Mesh Upload()
    {
        var mesh = new Mesh(positions.Count, positions.Count / 3);
        mesh.AllocVertices();
        mesh.AllocTexCoords();
        mesh.AllocColors();

        var v = mesh.VerticesAs<Vector3>();
        var t = mesh.TexCoordsAs<Vector2>();
        var col = mesh.ColorsAs<Color>();
        for (int i = 0; i < positions.Count; i++)
        {
            v[i] = positions[i];
            t[i] = uvs[i];
            col[i] = colors[i];
        }

        Raylib.UploadMesh(ref mesh, false);
        return mesh;
    }
}
