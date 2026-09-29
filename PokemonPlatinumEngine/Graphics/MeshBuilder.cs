using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

[Flags]
internal enum BoxFaces
{
    None = 0,
    Top = 1,
    Bottom = 2,
    North = 4,
    South = 8,
    West = 16,
    East = 32,
    Sides = North | South | West | East,
    All = Top | Bottom | Sides,
    Visible = Top | Sides
}

/// <summary>
/// Accumulates non-indexed triangles (position, texture coordinate, normal, color) and uploads them as one
/// raylib mesh. Lighting happens in the field shader; vertex colors carry tint and baked ambient occlusion,
/// and vertex alpha doubles as a wind weight (255 = rigid, lower = sways).
/// </summary>
internal sealed class MeshBuilder
{
    private readonly List<Vector3> positions = new();
    private readonly List<Vector2> uvs = new();
    private readonly List<Vector3> normals = new();
    private readonly List<Color> colors = new();

    public int VertexCount => positions.Count;

    /// <summary>Encodes a wind weight (0 = rigid, 1 = sways fully) into a color's alpha.</summary>
    public static Color Sway(Color c, float weight) =>
        new(c.R, c.G, c.B, (int)(255 * (1f - Math.Clamp(weight, 0f, 0.99f))));

    public static Color Scale(Color c, float f) => new(
        (int)Math.Clamp(c.R * f, 0, 255),
        (int)Math.Clamp(c.G * f, 0, 255),
        (int)Math.Clamp(c.B * f, 0, 255),
        (int)c.A);

    public void Vertex(Vector3 p, Vector2 uv, Vector3 n, Color c)
    {
        positions.Add(p);
        uvs.Add(uv);
        normals.Add(n);
        colors.Add(c);
    }

    public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector2 ta, Vector2 tb, Vector2 tc,
        Vector3 na, Vector3 nb, Vector3 nc, Color ca, Color cb, Color cc)
    {
        Vertex(a, ta, na, ca);
        Vertex(b, tb, nb, cb);
        Vertex(c, tc, nc, cc);
    }

    /// <summary>Flat-shaded triangle; its normal is flipped if needed to point along <paramref name="outward"/>.</summary>
    public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector2 ta, Vector2 tb, Vector2 tc, Color color, Vector3? outward = null)
    {
        var n = Vector3.Normalize(Vector3.Cross(b - a, c - a));
        if (outward.HasValue && Vector3.Dot(n, outward.Value) < 0f) n = -n;
        Tri(a, b, c, ta, tb, tc, n, n, n, color, color, color);
    }

    /// <summary>
    /// Quad with corners a (bottom-left), b (bottom-right), c (top-right), d (top-left) as seen from its front;
    /// the face normal is (b - a) x (d - a) unless <paramref name="normal"/> is given.
    /// </summary>
    public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ta, Vector2 tb, Vector2 tc, Vector2 td,
        Color bottom, Color top, Vector3? normal = null)
    {
        var n = normal ?? Normal(a, b, d);
        Tri(a, b, c, ta, tb, tc, n, n, n, bottom, bottom, top);
        Tri(a, c, d, ta, tc, td, n, n, n, bottom, top, top);
    }

    public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ta, Vector2 tb, Vector2 tc, Vector2 td,
        Color color, Vector3? normal = null) => Quad(a, b, c, d, ta, tb, tc, td, color, color, normal);

    /// <summary>Quad mapped to the whole texture (0..1), for decals.</summary>
    public void Decal(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color, Vector3? normal = null) =>
        Quad(a, b, c, d, new(0, 1), new(1, 1), new(1, 0), new(0, 0), color, normal);

    /// <summary>
    /// Axis-aligned box. Texture coordinates are world units times <paramref name="uvScale"/>, with vertical
    /// faces divided by <paramref name="vs"/> so tiling textures keep square texels on screen.
    /// </summary>
    public void Box(Vector3 min, Vector3 max, Color color, BoxFaces faces = BoxFaces.Visible, float uvScale = 1f, float vs = 1f)
    {
        var (x0, y0, z0) = (min.X, min.Y, min.Z);
        var (x1, y1, z1) = (max.X, max.Y, max.Z);
        float s = uvScale, v = uvScale / vs;

        if (faces.HasFlag(BoxFaces.Top))
            Quad(new(x0, y1, z1), new(x1, y1, z1), new(x1, y1, z0), new(x0, y1, z0),
                new(x0 * s, z1 * s), new(x1 * s, z1 * s), new(x1 * s, z0 * s), new(x0 * s, z0 * s), color);
        if (faces.HasFlag(BoxFaces.Bottom))
            Quad(new(x0, y0, z0), new(x1, y0, z0), new(x1, y0, z1), new(x0, y0, z1),
                new(x0 * s, z0 * s), new(x1 * s, z0 * s), new(x1 * s, z1 * s), new(x0 * s, z1 * s), color);
        if (faces.HasFlag(BoxFaces.South))
            Quad(new(x0, y0, z1), new(x1, y0, z1), new(x1, y1, z1), new(x0, y1, z1),
                new(x0 * s, -y0 * v), new(x1 * s, -y0 * v), new(x1 * s, -y1 * v), new(x0 * s, -y1 * v), color);
        if (faces.HasFlag(BoxFaces.North))
            Quad(new(x1, y0, z0), new(x0, y0, z0), new(x0, y1, z0), new(x1, y1, z0),
                new(-x1 * s, -y0 * v), new(-x0 * s, -y0 * v), new(-x0 * s, -y1 * v), new(-x1 * s, -y1 * v), color);
        if (faces.HasFlag(BoxFaces.West))
            Quad(new(x0, y0, z0), new(x0, y0, z1), new(x0, y1, z1), new(x0, y1, z0),
                new(z0 * s, -y0 * v), new(z1 * s, -y0 * v), new(z1 * s, -y1 * v), new(z0 * s, -y1 * v), color);
        if (faces.HasFlag(BoxFaces.East))
            Quad(new(x1, y0, z1), new(x1, y0, z0), new(x1, y1, z0), new(x1, y1, z1),
                new(-z1 * s, -y0 * v), new(-z0 * s, -y0 * v), new(-z0 * s, -y1 * v), new(-z1 * s, -y1 * v), color);
    }

    /// <summary>Vertical cylinder (or cone when the radii differ) with smooth normals and an optional cap.</summary>
    public void Cylinder(Vector3 baseCenter, float r0, float r1, float height, int sides, Color color, bool cap = true, Color? capColor = null)
    {
        float slope = (r0 - r1) / Math.Max(0.001f, height);
        for (int i = 0; i < sides; i++)
        {
            float a0 = i * MathF.Tau / sides, a1 = (i + 1) * MathF.Tau / sides;
            var d0 = new Vector3(MathF.Cos(a0), 0, MathF.Sin(a0));
            var d1 = new Vector3(MathF.Cos(a1), 0, MathF.Sin(a1));
            var n0 = Vector3.Normalize(d0 + new Vector3(0, slope, 0));
            var n1 = Vector3.Normalize(d1 + new Vector3(0, slope, 0));
            var b0 = baseCenter + d0 * r0;
            var b1 = baseCenter + d1 * r0;
            var t0 = baseCenter + d0 * r1 + new Vector3(0, height, 0);
            var t1 = baseCenter + d1 * r1 + new Vector3(0, height, 0);
            float u0 = i / (float)sides, u1 = (i + 1) / (float)sides;
            Tri(b0, b1, t1, new(u0, 1), new(u1, 1), new(u1, 0), n0, n1, n1, color, color, color);
            Tri(b0, t1, t0, new(u0, 1), new(u1, 0), new(u0, 0), n0, n1, n0, color, color, color);
            if (cap && r1 > 0.001f)
            {
                var top = baseCenter + new Vector3(0, height, 0);
                var cc = capColor ?? color;
                Tri(top, t1, t0, new(0.5f, 0.5f), new(0.5f, 0.5f), new(0.5f, 0.5f), Vector3.UnitY, Vector3.UnitY, Vector3.UnitY, cc, cc, cc);
            }
        }
    }

    /// <summary>Normal of the plane through a, b and d, following the quad corner convention.</summary>
    public static Vector3 Normal(Vector3 a, Vector3 b, Vector3 d) => Vector3.Normalize(Vector3.Cross(b - a, d - a));

    public unsafe Mesh Upload()
    {
        var mesh = new Mesh(positions.Count, positions.Count / 3);
        mesh.AllocVertices();
        mesh.AllocTexCoords();
        mesh.AllocNormals();
        mesh.AllocColors();

        var v = mesh.VerticesAs<Vector3>();
        var t = mesh.TexCoordsAs<Vector2>();
        var n = mesh.NormalsAs<Vector3>();
        var col = mesh.ColorsAs<Color>();
        for (int i = 0; i < positions.Count; i++)
        {
            v[i] = positions[i];
            t[i] = uvs[i];
            n[i] = normals[i];
            col[i] = colors[i];
        }

        Raylib.UploadMesh(ref mesh, false);
        return mesh;
    }
}
