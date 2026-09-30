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

    public IReadOnlyList<Vector3> Vertices => positions;

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
            Tri(b0, t1, b1, new(u0, 1), new(u1, 0), new(u1, 1), n0, n1, n1, color, color, color);
            Tri(b0, t0, t1, new(u0, 1), new(u0, 0), new(u1, 0), n0, n0, n1, color, color, color);
            if (cap && r1 > 0.001f)
            {
                var top = baseCenter + new Vector3(0, height, 0);
                var cc = capColor ?? color;
                Tri(top, t1, t0, new(0.5f, 0.5f), new(0.5f, 0.5f), new(0.5f, 0.5f), Vector3.UnitY, Vector3.UnitY, Vector3.UnitY, cc, cc, cc);
            }
        }
    }

    /// <summary>
    /// Surface of revolution about the vertical axis through <paramref name="origin"/>. The profile is a list of
    /// (radius, height) points traversed counter-clockwise in the (r, y) half-plane, e.g. bottom centre → out →
    /// up → back to the axis; that gives outward normals and counter-clockwise front faces. The circular
    /// cross-section is stretched into an ellipse by <paramref name="sx"/> and <paramref name="sz"/>.
    /// </summary>
    public void Lathe(Vector3 origin, IReadOnlyList<Vector2> profile, int segments, Func<int, Color> colorOf, float sx = 1f, float sz = 1f)
    {
        int n = profile.Count;
        var ringNormal = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            var acc = Vector2.Zero;
            if (i > 0) acc += SegmentNormal(profile[i - 1], profile[i]);
            if (i < n - 1) acc += SegmentNormal(profile[i], profile[i + 1]);
            ringNormal[i] = acc.LengthSquared() > 0 ? Vector2.Normalize(acc) : Vector2.UnitY;
        }

        Vector3 P(int i, float c, float s) => origin + new Vector3(sx * profile[i].X * c, profile[i].Y, sz * profile[i].X * s);
        Vector3 N(int i, float c, float s) => Vector3.Normalize(new Vector3(ringNormal[i].X * c / sx, ringNormal[i].Y, ringNormal[i].X * s / sz));

        for (int i = 0; i < n - 1; i++)
        {
            var c0 = colorOf(i);
            var c1 = colorOf(i + 1);
            float v0 = i / (float)(n - 1), v1 = (i + 1) / (float)(n - 1);
            for (int j = 0; j < segments; j++)
            {
                float a0 = j * MathF.Tau / segments, a1 = (j + 1) * MathF.Tau / segments;
                float cos0 = MathF.Cos(a0), sin0 = MathF.Sin(a0), cos1 = MathF.Cos(a1), sin1 = MathF.Sin(a1);
                float u0 = j / (float)segments, u1 = (j + 1) / (float)segments;

                // Skip the degenerate triangle at a pole (radius 0)
                if (profile[i].X > 1e-5f)
                {
                    Tri(P(i, cos0, sin0), P(i + 1, cos1, sin1), P(i, cos1, sin1),
                        new(u0, v0), new(u1, v1), new(u1, v0),
                        N(i, cos0, sin0), N(i + 1, cos1, sin1), N(i, cos1, sin1), c0, c1, c0);
                }
                if (profile[i + 1].X > 1e-5f)
                {
                    Tri(P(i, cos0, sin0), P(i + 1, cos0, sin0), P(i + 1, cos1, sin1),
                        new(u0, v0), new(u0, v1), new(u1, v1),
                        N(i, cos0, sin0), N(i + 1, cos0, sin0), N(i + 1, cos1, sin1), c0, c1, c1);
                }
            }
        }
    }

    public void Lathe(Vector3 origin, IReadOnlyList<Vector2> profile, int segments, Color color, float sx = 1f, float sz = 1f) =>
        Lathe(origin, profile, segments, _ => color, sx, sz);

    private static Vector2 SegmentNormal(Vector2 a, Vector2 b)
    {
        var d = b - a;
        var n = new Vector2(d.Y, -d.X);
        return n.LengthSquared() > 0 ? Vector2.Normalize(n) : Vector2.Zero;
    }

    /// <summary>Ellipsoid with smooth normals, built as a lathe from pole to pole.</summary>
    public void Ellipsoid(Vector3 center, Vector3 radii, Color color, int segments = 16, int rings = 10) =>
        Ellipsoid(center, radii, _ => color, segments, rings);

    /// <summary>Ellipsoid whose colour can change by ring (0 = bottom pole, <paramref name="rings"/> = top pole).</summary>
    public void Ellipsoid(Vector3 center, Vector3 radii, Func<int, Color> colorOfRing, int segments = 16, int rings = 10)
    {
        var profile = new Vector2[rings + 1];
        for (int k = 0; k <= rings; k++)
        {
            float t = k * MathF.PI / rings;
            profile[k] = new Vector2(MathF.Sin(t), -MathF.Cos(t) * radii.Y);
        }
        Lathe(center, profile, segments, colorOfRing, radii.X, radii.Z);
    }

    /// <summary>Doughnut around the vertical axis: <paramref name="ringRadius"/> to the tube centre, tube radius <paramref name="tube"/>.</summary>
    public void Torus(Vector3 center, float ringRadius, float tube, Color color, int segments = 20, int sides = 8, float sx = 1f, float sz = 1f)
    {
        var profile = new Vector2[sides + 1];
        for (int k = 0; k <= sides; k++)
        {
            float t = -MathF.PI / 2f + k * MathF.Tau / sides;
            profile[k] = new Vector2(ringRadius + MathF.Cos(t) * tube, MathF.Sin(t) * tube);
        }
        Lathe(center, profile, segments, color, sx, sz);
    }

    /// <summary>Cone standing on <paramref name="baseCenter"/> with its tip straight up (closed base).</summary>
    public void Cone(Vector3 baseCenter, float radius, float height, Color color, int segments = 10) =>
        Lathe(baseCenter, new[] { new Vector2(0, 0), new Vector2(radius, 0), new Vector2(0, height) }, segments, color);

    /// <summary>
    /// Appends another builder's triangles transformed by <paramref name="transform"/> (System.Numerics row-vector
    /// convention: rotations and uniform scale, plus translation).
    /// </summary>
    public void Append(MeshBuilder other, Matrix4x4 transform)
    {
        for (int i = 0; i < other.positions.Count; i++)
        {
            positions.Add(Vector3.Transform(other.positions[i], transform));
            normals.Add(Vector3.Normalize(Vector3.TransformNormal(other.normals[i], transform)));
            uvs.Add(other.uvs[i]);
            colors.Add(other.colors[i]);
        }
    }

    /// <summary>Appends another builder's triangles unchanged except for their vertex colours.</summary>
    public void AppendRecolored(MeshBuilder other, Func<Color, Color> map)
    {
        for (int i = 0; i < other.positions.Count; i++)
        {
            positions.Add(other.positions[i]);
            normals.Add(other.normals[i]);
            uvs.Add(other.uvs[i]);
            colors.Add(map(other.colors[i]));
        }
    }

    /// <summary>Builds a primitive with <paramref name="build"/> and appends it moved by <paramref name="transform"/>.</summary>
    public void Add(Matrix4x4 transform, Action<MeshBuilder> build)
    {
        var part = new MeshBuilder();
        build(part);
        Append(part, transform);
    }

    /// <summary>
    /// Inverted-hull outline: every vertex pushed out along its (position-averaged) normal and every triangle's
    /// winding reversed. Drawn with back-face culling on, only the rim that peeks out around the model shows.
    /// </summary>
    public MeshBuilder BuildOutline(float thickness, Func<Color, Color> tint)
    {
        static (int, int, int) Key(Vector3 p) => ((int)MathF.Round(p.X * 2000f), (int)MathF.Round(p.Y * 2000f), (int)MathF.Round(p.Z * 2000f));

        var smooth = new Dictionary<(int, int, int), Vector3>();
        for (int i = 0; i < positions.Count; i++)
        {
            var key = Key(positions[i]);
            smooth[key] = smooth.TryGetValue(key, out var acc) ? acc + normals[i] : normals[i];
        }

        var hull = new MeshBuilder();
        for (int i = 0; i < positions.Count; i += 3)
        {
            foreach (int k in new[] { i, i + 2, i + 1 })
            {
                var n = smooth[Key(positions[k])];
                n = n.LengthSquared() > 1e-8f ? Vector3.Normalize(n) : normals[k];
                hull.Vertex(positions[k] + n * thickness, uvs[k], n, tint(colors[k]));
            }
        }
        return hull;
    }

    /// <summary>Normal of the plane through a, b and d, following the quad corner convention.</summary>
    public static Vector3 Normal(Vector3 a, Vector3 b, Vector3 d) => Vector3.Normalize(Vector3.Cross(b - a, d - a));

    /// <summary>Axis-aligned bounds of everything added so far.</summary>
    public (Vector3 Min, Vector3 Max) Bounds()
    {
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var p in positions)
        {
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }
        return (min, max);
    }

    /// <summary>Copies of the triangles whose centroid passes <paramref name="keep"/>, removed from this builder.</summary>
    public MeshBuilder Extract(Func<Vector3, bool> keep)
    {
        var taken = new MeshBuilder();
        var rest = new MeshBuilder();
        for (int i = 0; i < positions.Count; i += 3)
        {
            var centroid = (positions[i] + positions[i + 1] + positions[i + 2]) / 3f;
            var target = keep(centroid) ? taken : rest;
            for (int k = i; k < i + 3; k++) target.Vertex(positions[k], uvs[k], normals[k], colors[k]);
        }
        positions.Clear(); uvs.Clear(); normals.Clear(); colors.Clear();
        Append(rest, Matrix4x4.Identity);
        return taken;
    }

    /// <summary>Rewrites every texture coordinate with <paramref name="map"/> applied to the vertex position.</summary>
    public void MapUVs(Func<Vector3, Vector2> map)
    {
        for (int i = 0; i < positions.Count; i++) uvs[i] = map(positions[i]);
    }

    /// <summary>A copy of the triangle data, for building derived meshes.</summary>
    public MeshBuilder Clone()
    {
        var copy = new MeshBuilder();
        copy.Append(this, Matrix4x4.Identity);
        return copy;
    }

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
