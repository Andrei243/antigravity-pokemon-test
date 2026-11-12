using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>Up to four bones a vertex follows and how much each (style guide, "Modelled characters").</summary>
internal readonly struct BoneMix
{
    private readonly int b0, b1, b2, b3;
    private readonly float w0, w1, w2, w3;

    private BoneMix(int b0, float w0, int b1, float w1, int b2, float w2, int b3, float w3)
    {
        this.b0 = b0; this.w0 = w0; this.b1 = b1; this.w1 = w1; this.b2 = b2; this.w2 = w2; this.b3 = b3; this.w3 = w3;
    }

    public static BoneMix One(int bone) => new(bone, 1f, 0, 0f, 0, 0f, 0, 0f);

    /// <summary>Bone <paramref name="a"/> giving way to <paramref name="b"/> as <paramref name="t"/> goes from 0 to 1.</summary>
    public static BoneMix Two(int a, int b, float t) => new(a, 1f - Math.Clamp(t, 0f, 1f), b, Math.Clamp(t, 0f, 1f), 0, 0f, 0, 0f);

    public static BoneMix Lerp(BoneMix x, BoneMix y, float t)
    {
        Span<int> bones = stackalloc int[8];
        Span<float> weights = stackalloc float[8];
        int n = 0;
        void Put(Span<int> bs, Span<float> ws, int bone, float weight)
        {
            if (weight <= 0f) return;
            for (int i = 0; i < n; i++)
                if (bs[i] == bone) { ws[i] += weight; return; }
            bs[n] = bone;
            ws[n++] = weight;
        }
        Put(bones, weights, x.b0, x.w0 * (1f - t)); Put(bones, weights, x.b1, x.w1 * (1f - t));
        Put(bones, weights, x.b2, x.w2 * (1f - t)); Put(bones, weights, x.b3, x.w3 * (1f - t));
        Put(bones, weights, y.b0, y.w0 * t); Put(bones, weights, y.b1, y.w1 * t);
        Put(bones, weights, y.b2, y.w2 * t); Put(bones, weights, y.b3, y.w3 * t);
        // The four heaviest, made to sum to one
        for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
                if (weights[j] > weights[i]) { (weights[i], weights[j]) = (weights[j], weights[i]); (bones[i], bones[j]) = (bones[j], bones[i]); }
        float sum = 0f;
        for (int i = 0; i < Math.Min(n, 4); i++) sum += weights[i];
        if (sum <= 0f) return x;
        float w0 = n > 0 ? weights[0] / sum : 0f, w1 = n > 1 ? weights[1] / sum : 0f, w2 = n > 2 ? weights[2] / sum : 0f, w3 = n > 3 ? weights[3] / sum : 0f;
        int c0 = n > 0 ? bones[0] : 0, c1 = n > 1 ? bones[1] : 0, c2 = n > 2 ? bones[2] : 0, c3 = n > 3 ? bones[3] : 0;
        return new BoneMix(c0, w0, c1, w1, c2, w2, c3, w3);
    }

    public void Write(byte[] indices, float[] weights, int at)
    {
        indices[at] = (byte)b0; indices[at + 1] = (byte)b1; indices[at + 2] = (byte)b2; indices[at + 3] = (byte)b3;
        weights[at] = w0; weights[at + 1] = w1; weights[at + 2] = w2; weights[at + 3] = w3;
    }
}

/// <summary>One closed loop of a lofted surface: its points in order round the loop, their colours and the bones they follow.</summary>
internal sealed class PolyRing
{
    public Vector3[] P = Array.Empty<Vector3>();
    public Color[] C = Array.Empty<Color>();
    public BoneMix W;

    public Vector3 Middle()
    {
        var sum = Vector3.Zero;
        foreach (var p in P) sum += p;
        return sum / P.Length;
    }
}

/// <summary>A key of a swept limb: where its middle is, its half sizes across and through, its colour and its bones.</summary>
internal readonly record struct SweepKey(Vector3 P, float Rx, float Rz, Color C, BoneMix W);

/// <summary>A key of an upright stack: its height, half width, how far it reaches forward and back of its middle, and that middle's depth.</summary>
internal readonly record struct StackKey(float Y, float Wide, float Front, float Back, float Z, Color C, BoneMix W);

/// <summary>
/// Builds a skinned mesh of lofted surfaces (style guide, "Modelled characters"): rings of points joined into
/// tubes with clean rows of quads, which is what a modelled character is made of in place of the SDF kit's
/// meshed blobs. GPU-free; the result is an <see cref="SdfMesh"/>, so everything that draws a rig draws it.
/// </summary>
internal sealed class PolyBuilder
{
    private readonly List<Vector3> positions = new();
    private readonly List<Color> colours = new();
    private readonly List<byte> materials = new();
    private readonly List<BoneMix> bones = new();
    private readonly List<int> indices = new();

    public int VertexCount => positions.Count;

    private int Add(Vector3 p, Color c, SurfaceMaterial material, BoneMix w)
    {
        positions.Add(p);
        colours.Add(c);
        materials.Add((byte)material);
        bones.Add(w);
        return positions.Count - 1;
    }

    /// <summary>
    /// A loop of <paramref name="n"/> points round <paramref name="centre"/>: a superellipse of half width
    /// <paramref name="rx"/> along <paramref name="right"/>, reaching <paramref name="front"/> along
    /// <paramref name="fwd"/> and <paramref name="back"/> against it. A power of 2 is an ellipse; more is squarer.
    /// </summary>
    public static Vector3[] Oval(Vector3 centre, Vector3 right, Vector3 fwd, float rx, float front, float back, int n, float power = 2f)
    {
        var points = new Vector3[n];
        float e = 2f / power;
        for (int k = 0; k < n; k++)
        {
            float a = MathF.Tau * k / n;
            float c = MathF.Cos(a), s = MathF.Sin(a);
            float x = MathF.Sign(c) * MathF.Pow(MathF.Abs(c), e), z = MathF.Sign(s) * MathF.Pow(MathF.Abs(s), e);
            points[k] = centre + right * (rx * x) + fwd * ((z > 0f ? front : back) * z);
        }
        return points;
    }

    public static Color[] Flat(Color c, int n)
    {
        var colours = new Color[n];
        Array.Fill(colours, c);
        return colours;
    }

    /// <summary>
    /// Joins rings of equal counts into a tube, closing either end with a fan if asked. Which way round the rings go
    /// doesn't matter: the triangles are turned so they face away from the tube's middle.
    /// </summary>
    public void Tube(IReadOnlyList<PolyRing> rings, SurfaceMaterial material, bool capStart = true, bool capEnd = true)
    {
        int n = rings[0].P.Length;
        var starts = new int[rings.Count];
        for (int r = 0; r < rings.Count; r++)
        {
            starts[r] = positions.Count;
            for (int k = 0; k < n; k++) Add(rings[r].P[k], rings[r].C[k], material, rings[r].W);
        }
        // Outward: by the sum over every quad of its normal against the way out from its ring's middle
        float facing = 0f;
        for (int r = 0; r + 1 < rings.Count; r++)
        {
            var mid = rings[r].Middle();
            for (int k = 0; k < n; k++)
            {
                Vector3 a = rings[r].P[k], b = rings[r].P[(k + 1) % n], d = rings[r + 1].P[k];
                facing += Vector3.Dot(Vector3.Cross(b - a, d - a), a - mid);
            }
        }
        bool flip = facing < 0f;
        for (int r = 0; r + 1 < rings.Count; r++)
            for (int k = 0; k < n; k++)
            {
                int a = starts[r] + k, b = starts[r] + (k + 1) % n, c = starts[r + 1] + (k + 1) % n, d = starts[r + 1] + k;
                if (flip) { Triangle(a, d, c); Triangle(a, c, b); }
                else { Triangle(a, b, c); Triangle(a, c, d); }
            }
        if (capStart) Cap(rings[0], starts[0], rings[0].Middle() - rings[Math.Min(1, rings.Count - 1)].Middle(), material);
        if (capEnd) Cap(rings[^1], starts[^1], rings[^1].Middle() - rings[Math.Max(0, rings.Count - 2)].Middle(), material);
    }

    private void Cap(PolyRing ring, int start, Vector3 outward, SurfaceMaterial material)
    {
        int n = ring.P.Length;
        var mid = ring.Middle();
        int centre = Add(mid, ring.C[n / 4], material, ring.W);
        float facing = 0f;
        for (int k = 0; k < n; k++)
            facing += Vector3.Dot(Vector3.Cross(ring.P[k] - mid, ring.P[(k + 1) % n] - mid), outward);
        for (int k = 0; k < n; k++)
        {
            int a = start + k, b = start + (k + 1) % n;
            if (facing < 0f) Triangle(centre, b, a);
            else Triangle(centre, a, b);
        }
    }

    private void Triangle(int a, int b, int c)
    {
        indices.Add(a);
        indices.Add(b);
        indices.Add(c);
    }

    private static float Cr(float a, float b, float c, float d, float t) =>
        0.5f * (2f * b + (c - a) * t + (2f * a - 5f * b + 4f * c - d) * t * t + (3f * b - 3f * c + d - a) * t * t * t);

    private static Vector3 Cr(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t) => new(Cr(a.X, b.X, c.X, d.X, t), Cr(a.Y, b.Y, c.Y, d.Y, t), Cr(a.Z, b.Z, c.Z, d.Z, t));

    /// <summary>
    /// A limb swept through its keys along a smooth path, <paramref name="sub"/> rings to a stretch. The loop's
    /// "front" faces <paramref name="hint"/> as nearly as the path allows. A change of colour between two keys is a
    /// crisp line at the later key.
    /// </summary>
    public void Sweep(IReadOnlyList<SweepKey> keys, Vector3 hint, int n, int sub, SurfaceMaterial material, float power = 2f, bool capStart = true, bool capEnd = true, Func<float, float, float>? shape = null)
    {
        var samples = new List<(Vector3 P, float Rx, float Rz, Color C, BoneMix W)>();
        SweepKey K(int i) => keys[Math.Clamp(i, 0, keys.Count - 1)];
        for (int i = 0; i + 1 < keys.Count; i++)
        {
            bool last = i + 2 == keys.Count;
            bool seam = !last && !keys[i].C.Equals(keys[i + 1].C);
            for (int j = 0; j < sub + (last || seam ? 1 : 0); j++)
            {
                float t = j / (float)sub;
                samples.Add((Cr(K(i - 1).P, K(i).P, K(i + 1).P, K(i + 2).P, t),
                    MathF.Max(0.0005f, Cr(K(i - 1).Rx, K(i).Rx, K(i + 1).Rx, K(i + 2).Rx, t)),
                    MathF.Max(0.0005f, Cr(K(i - 1).Rz, K(i).Rz, K(i + 1).Rz, K(i + 2).Rz, t)),
                    keys[i].C, BoneMix.Lerp(keys[i].W, keys[i + 1].W, t)));
            }
        }
        var rings = new List<PolyRing>();
        for (int i = 0; i < samples.Count; i++)
        {
            // The path's way here, passing over the doubled ring of a colour seam
            int before = i, after = i;
            while (before > 0 && (samples[before].P - samples[i].P).LengthSquared() < 1e-10f) before--;
            while (after < samples.Count - 1 && (samples[after].P - samples[i].P).LengthSquared() < 1e-10f) after++;
            var along = samples[after].P - samples[before].P;
            along = along.LengthSquared() < 1e-12f ? Vector3.UnitY : Vector3.Normalize(along);
            var fwd = hint - along * Vector3.Dot(hint, along);
            fwd = fwd.LengthSquared() < 1e-8f ? Vector3.UnitZ : Vector3.Normalize(fwd);
            var right = Vector3.Normalize(Vector3.Cross(fwd, along));
            var s = samples[i];
            rings.Add(new PolyRing { P = Shaped(Oval(s.P, right, fwd, s.Rx, s.Rz, s.Rz, n, power), s.P, shape, i / (float)Math.Max(1, samples.Count - 1)), C = Flat(s.C, n), W = s.W });
        }
        Tube(rings, material, capStart, capEnd);
    }

    /// <summary>An upright stack of loops through its keys, smooth between them, facing +Z: a torso, a neck, a hem.</summary>
    public void Stack(IReadOnlyList<StackKey> keys, int n, int sub, SurfaceMaterial material, float power = 2f, bool capStart = true, bool capEnd = true, Func<float, float, float>? shape = null)
    {
        var rings = new List<PolyRing>();
        StackKey K(int i) => keys[Math.Clamp(i, 0, keys.Count - 1)];
        for (int i = 0; i + 1 < keys.Count; i++)
        {
            bool last = i + 2 == keys.Count;
            bool seam = !last && !keys[i].C.Equals(keys[i + 1].C);
            for (int j = 0; j < sub + (last || seam ? 1 : 0); j++)
            {
                float t = j / (float)sub;
                float V(Func<StackKey, float> of) => Cr(of(K(i - 1)), of(K(i)), of(K(i + 1)), of(K(i + 2)), t);
                var centre = new Vector3(0, V(k => k.Y), V(k => k.Z));
                rings.Add(new PolyRing
                {
                    P = Shaped(Oval(centre, Vector3.UnitX, Vector3.UnitZ, MathF.Max(0.0005f, V(k => k.Wide)), MathF.Max(0.0005f, V(k => k.Front)), MathF.Max(0.0005f, V(k => k.Back)), n, power), centre, shape, (i + t) / (keys.Count - 1)),
                    C = Flat(keys[i].C, n), W = BoneMix.Lerp(keys[i].W, keys[i + 1].W, t)
                });
            }
        }
        Tube(rings, material, capStart, capEnd);
    }


    /// <summary>A loop's points moved in or out from its middle by <paramref name="shape"/> of how far along the surface the loop is and the angle round it (folds, a seam's dip).</summary>
    private static Vector3[] Shaped(Vector3[] points, Vector3 centre, Func<float, float, float>? shape, float along)
    {
        if (shape == null) return points;
        for (int k = 0; k < points.Length; k++)
            points[k] = centre + (points[k] - centre) * shape(along, MathF.Tau * k / points.Length);
        return points;
    }

    /// <summary>
    /// Darkens each point by how much of the mesh stands close before it (style guide, "Modelled characters",
    /// "Contact shade"): under a hem, a chin, a fringe, in an armpit, between fingers. Each neighbour within
    /// <see cref="OcclusionReach"/> counts by the surface it stands for, how squarely it is in front and how near.
    /// </summary>
    private void Occlude(SdfMesh mesh, float strength)
    {
        const float R = OcclusionReach;
        int n = mesh.VertexCount;
        var area = new float[n];
        for (int t = 0; t < mesh.Indices.Length; t += 3)
        {
            int a = mesh.Indices[t], b = mesh.Indices[t + 1], c = mesh.Indices[t + 2];
            float third = Vector3.Cross(positions[b] - positions[a], positions[c] - positions[a]).Length() / 6f;
            area[a] += third;
            area[b] += third;
            area[c] += third;
        }
        static (int, int, int) Cell(Vector3 p) => ((int)MathF.Floor(p.X / R), (int)MathF.Floor(p.Y / R), (int)MathF.Floor(p.Z / R));
        var grid = new Dictionary<(int, int, int), List<int>>();
        for (int v = 0; v < n; v++)
        {
            var cell = Cell(positions[v]);
            if (!grid.TryGetValue(cell, out var list)) grid[cell] = list = new List<int>();
            list.Add(v);
        }
        var shade = new float[n];
        System.Threading.Tasks.Parallel.For(0, n, v =>
        {
            var p = positions[v];
            var normal = mesh.Normals[v];
            var (cx, cy, cz) = Cell(p);
            float sum = 0f;
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (!grid.TryGetValue((cx + dx, cy + dy, cz + dz), out var list)) continue;
                        foreach (int u in list)
                        {
                            var d = positions[u] - p;
                            float far = d.Length();
                            if (far < 0.004f || far >= R) continue;
                            float facing = Vector3.Dot(normal, d) / far - 0.18f;
                            if (facing <= 0f) continue;
                            float near = 1f - far / R;
                            sum += area[u] * facing * near * near;
                        }
                    }
            shade[v] = Math.Clamp(sum / (R * R * 0.42f), 0f, 1f);
        });
        for (int v = 0; v < n; v++)
        {
            float a = strength * shade[v];
            var c = mesh.Colors[v];
            // A shade a little toward violet, as the style's shadows are
            mesh.Colors[v] = new Color((byte)(c.R * (1f - a)), (byte)(c.G * (1f - a * 1.04f)), (byte)(c.B * (1f - a * 0.86f)), c.A);
        }
    }

    /// <summary>How far a point looks for what stands before it, in model units.</summary>
    public const float OcclusionReach = 0.05f;

    /// <summary>A ball of half sizes <paramref name="radii"/>, turned by <paramref name="turn"/>.</summary>
    public void Ball(Vector3 centre, Vector3 radii, Quaternion turn, Color c, BoneMix w, SurfaceMaterial material, int around = 14, int rows = 8)
    {
        var rings = new List<PolyRing>();
        Vector3 x = Vector3.Transform(Vector3.UnitX, turn), y = Vector3.Transform(Vector3.UnitY, turn), z = Vector3.Transform(Vector3.UnitZ, turn);
        for (int r = 1; r < rows; r++)
        {
            float phi = -MathF.PI / 2f + MathF.PI * r / rows;
            float across = MathF.Cos(phi);
            rings.Add(new PolyRing { P = Oval(centre + y * (radii.Y * MathF.Sin(phi)), x, z, radii.X * across, radii.Z * across, radii.Z * across, around), C = Flat(c, around), W = w });
        }
        Tube(rings, material);
    }

    /// <summary>
    /// The finished mesh: smooth normals (the same at points that coincide, so a colour seam's doubled ring shades as
    /// one surface), and with <paramref name="grow"/> above nought every point pushed that far out along its normal
    /// (the outline's shell).
    /// </summary>
    public SdfMesh Build(float grow = 0f, float occlusion = 0f)
    {
        int n = positions.Count;
        var normals = new Vector3[n];
        for (int t = 0; t < indices.Count; t += 3)
        {
            int a = indices[t], b = indices[t + 1], c = indices[t + 2];
            var face = Vector3.Cross(positions[b] - positions[a], positions[c] - positions[a]);
            normals[a] += face;
            normals[b] += face;
            normals[c] += face;
        }
        var shared = new Dictionary<(int, int, int), Vector3>();
        static (int, int, int) Key(Vector3 p) => ((int)MathF.Round(p.X * 20000f), (int)MathF.Round(p.Y * 20000f), (int)MathF.Round(p.Z * 20000f));
        for (int v = 0; v < n; v++)
        {
            var key = Key(positions[v]);
            shared[key] = shared.TryGetValue(key, out var sum) ? sum + normals[v] : normals[v];
        }
        var mesh = new SdfMesh
        {
            Positions = new Vector3[n], Normals = new Vector3[n], Colors = colours.ToArray(), Materials = materials.ToArray(),
            BoneIndices = new byte[n * 4], BoneWeights = new float[n * 4], Indices = indices.ToArray()
        };
        for (int v = 0; v < n; v++)
        {
            var normal = shared[Key(positions[v])];
            normal = normal.LengthSquared() < 1e-20f ? Vector3.UnitY : Vector3.Normalize(normal);
            mesh.Normals[v] = normal;
            mesh.Positions[v] = positions[v] + normal * grow;
            bones[v].Write(mesh.BoneIndices, mesh.BoneWeights, v * 4);
        }
        if (occlusion > 0f) Occlude(mesh, occlusion);
        return mesh;
    }
}
