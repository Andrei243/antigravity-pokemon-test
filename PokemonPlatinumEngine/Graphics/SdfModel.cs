using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

internal enum SdfShape { Sphere, Ellipsoid, RoundCone, RoundBox, Torus, Cylinder }

/// <summary>
/// What a shape does to the model so far: <see cref="Add"/> joins it (smoothly when it has a blend radius),
/// <see cref="Cut"/> carves it away, <see cref="Paint"/> recolours the surface inside it without changing the shape.
/// </summary>
internal enum SdfOp { Add, Cut, Paint }

/// <summary>
/// Surface kinds that the character shader lights differently (style guide, "Materials"): how soft the light's
/// edge is, how much shine and how much light of its own. The table is <see cref="SurfaceMaterials"/>.
/// </summary>
internal enum SurfaceMaterial : byte { Default = 0, Skin, Cloth, Hair, Leather, Plastic, Metal, Glow, Fur, Scales, Shell, Leaf }

/// <summary>One shape of an <see cref="SdfModel"/>, in model space.</summary>
internal sealed class SdfPrimitive
{
    public SdfShape Shape;
    public SdfOp Op;

    /// <summary>Centre; for a round cone, the centre of its first end.</summary>
    public Vector3 A;

    /// <summary>For a round cone, the centre of its second end.</summary>
    public Vector3 B;

    /// <summary>
    /// Sphere: (r). Ellipsoid: radii. Round cone: (radius at A, radius at B). Round box: half extents (z rounding
    /// in <see cref="Rounding"/>). Torus: (ring radius, tube radius), round the local Y axis. Cylinder: (radius,
    /// half height), round the local Y axis, edges rounded by <see cref="Rounding"/>.
    /// </summary>
    public Vector3 Size;

    public float Rounding;

    /// <summary>
    /// Orientation of the shape. A round cone's ends say where it lies; its rotation only matters together with
    /// <see cref="Stretch"/>, as the frame the stretch is measured in.
    /// </summary>
    public Quaternion Rotation = Quaternion.Identity;

    /// <summary>
    /// Scales the shape along its own axes (after <see cref="Rotation"/>): a spike flattened into a blade, an oval
    /// ring. A round cone keeps its ends where they are and only its girth changes.
    /// </summary>
    public Vector3 Stretch = Vector3.One;

    /// <summary>Smooth-union or smooth-cut radius (0 joins with a crease).</summary>
    public float Blend;

    public Color Color = Color.White;
    public SurfaceMaterial Material;

    /// <summary>Paint only: whether it also sets the material (otherwise only the colour changes).</summary>
    public bool PaintsMaterial;

    /// <summary>
    /// Paint only: the bones (one bit each) whose surface it may colour, judged by the heaviest bone at the
    /// point; 0 paints whatever lies inside. Lets a sleeve end on the arm without touching the body beside it.
    /// </summary>
    public ulong PaintBones;

    /// <summary>The bone this shape moves with.</summary>
    public int Bone;

    // Prepared once the model is complete
    internal Quaternion Inverse = Quaternion.Identity;
    internal Vector3 BoundsMin, BoundsMax;

    // Round cone constants (see Distance)
    private Vector3 ba;
    private float l2, rr, a2, il2;

    // Stretched shapes are measured in their own scaled frame, and the distance scaled back by the smallest stretch
    // (which never overstates it)
    private bool stretched;
    private float minStretch = 1f;

    internal void Prepare()
    {
        Inverse = Quaternion.Inverse(Quaternion.Normalize(Rotation));
        stretched = Stretch != Vector3.One;
        minStretch = Math.Min(Stretch.X, Math.Min(Stretch.Y, Stretch.Z));
        float grow = Math.Max(1f, Math.Max(Stretch.X, Math.Max(Stretch.Y, Stretch.Z)));
        if (Shape == SdfShape.RoundCone)
        {
            ba = stretched ? Vector3.Transform(B - A, Inverse) / Stretch : B - A;
            l2 = Math.Max(1e-8f, ba.LengthSquared());
            rr = Size.X - Size.Y;
            a2 = l2 - rr * rr;
            il2 = 1f / l2;
            BoundsMin = Vector3.Min(A - new Vector3(Size.X * grow), B - new Vector3(Size.Y * grow));
            BoundsMax = Vector3.Max(A + new Vector3(Size.X * grow), B + new Vector3(Size.Y * grow));
        }
        else
        {
            // A sphere round the shape's furthest reach is a safe box whatever the rotation
            float reach = Shape switch
            {
                SdfShape.Sphere => Size.X,
                SdfShape.Ellipsoid => Math.Max(Size.X, Math.Max(Size.Y, Size.Z)),
                SdfShape.RoundBox => Size.Length(),
                SdfShape.Torus => Size.X + Size.Y,
                SdfShape.Cylinder => MathF.Sqrt(Size.X * Size.X + Size.Y * Size.Y),
                _ => Size.Length()
            } * grow;
            BoundsMin = A - new Vector3(reach);
            BoundsMax = A + new Vector3(reach);
        }
    }

    /// <summary>Signed distance from <paramref name="p"/> to the shape's surface (negative inside).</summary>
    public float Distance(Vector3 p)
    {
        switch (Shape)
        {
            case SdfShape.RoundCone:
                return stretched ? RoundCone(Vector3.Transform(p - A, Inverse) / Stretch) * minStretch : RoundCone(p - A);
            case SdfShape.Sphere when !stretched:
                return (p - A).Length() - Size.X;
        }

        var q = Vector3.Transform(p - A, Inverse);
        if (stretched) return Local(q / Stretch) * minStretch;
        return Local(q);
    }

    /// <summary>Distance to the shape centred at the origin of its own frame (no stretch).</summary>
    private float Local(Vector3 q)
    {
        switch (Shape)
        {
            case SdfShape.Ellipsoid:
            {
                // Inigo Quilez's ellipsoid bound: exact on the surface, close to it nearby
                var r = Size;
                float k0 = (q / r).Length();
                float k1 = (q / (r * r)).Length();
                if (k1 < 1e-9f) return -Math.Min(r.X, Math.Min(r.Y, r.Z));
                return k0 * (k0 - 1f) / k1;
            }
            case SdfShape.RoundBox:
            {
                var d = Vector3.Abs(q) - Size + new Vector3(Rounding);
                var outside = Vector3.Max(d, Vector3.Zero).Length();
                float inside = Math.Min(Math.Max(d.X, Math.Max(d.Y, d.Z)), 0f);
                return outside + inside - Rounding;
            }
            case SdfShape.Torus:
            {
                float x = MathF.Sqrt(q.X * q.X + q.Z * q.Z) - Size.X;
                return MathF.Sqrt(x * x + q.Y * q.Y) - Size.Y;
            }
            case SdfShape.Cylinder:
            {
                float r = Rounding;
                float dx = MathF.Sqrt(q.X * q.X + q.Z * q.Z) - Size.X + r;
                float dy = MathF.Abs(q.Y) - Size.Y + r;
                float outside = MathF.Sqrt(Math.Max(dx, 0f) * Math.Max(dx, 0f) + Math.Max(dy, 0f) * Math.Max(dy, 0f));
                return Math.Min(Math.Max(dx, dy), 0f) + outside - r;
            }
            case SdfShape.Sphere:
                return q.Length() - Size.X;
        }
        return float.MaxValue;
    }

    /// <summary>
    /// Exact distance to a cone with round ends (Inigo Quilez), radius Size.X at its first end and Size.Y at its
    /// second; <paramref name="pa"/> is the point relative to the first end.
    /// </summary>
    private float RoundCone(Vector3 pa)
    {
        float y = Vector3.Dot(pa, ba);
        float z = y - l2;
        float x2 = (pa * l2 - ba * y).LengthSquared();
        float y2 = y * y * l2;
        float z2 = z * z * l2;
        float k = MathF.Sign(rr) * rr * rr * x2;
        if (MathF.Sign(z) * a2 * z2 > k) return MathF.Sqrt(x2 + z2) * il2 - Size.Y;
        if (MathF.Sign(y) * a2 * y2 < k) return MathF.Sqrt(x2 + y2) * il2 - Size.X;
        return (MathF.Sqrt(x2 * a2 * il2) + y * rr) * il2 - Size.X;
    }

    /// <summary>Distance from <paramref name="p"/> to the shape's bounding box (0 inside it): never more than the true distance.</summary>
    public float BoundsDistance(Vector3 p)
    {
        var d = Vector3.Max(Vector3.Max(BoundsMin - p, p - BoundsMax), Vector3.Zero);
        return d.Length();
    }

    internal void Describe(StringBuilder sb)
    {
        sb.Append((int)Shape).Append(',').Append((int)Op).Append(',');
        foreach (var v in new[] { A, B, Size }) sb.Append(v.X.ToString("R")).Append(',').Append(v.Y.ToString("R")).Append(',').Append(v.Z.ToString("R")).Append(',');
        if (Stretch != Vector3.One)
            sb.Append('s').Append(Stretch.X.ToString("R")).Append(',').Append(Stretch.Y.ToString("R")).Append(',').Append(Stretch.Z.ToString("R")).Append(',');
        sb.Append(Rounding.ToString("R")).Append(',').Append(Rotation.X.ToString("R")).Append(',').Append(Rotation.Y.ToString("R")).Append(',')
            .Append(Rotation.Z.ToString("R")).Append(',').Append(Rotation.W.ToString("R")).Append(',').Append(Blend.ToString("R")).Append(',')
            .Append(Color.R).Append(',').Append(Color.G).Append(',').Append(Color.B).Append(',').Append((int)Material).Append(',')
            .Append(PaintsMaterial ? 1 : 0).Append(',').Append(PaintBones).Append(',').Append(Bone).Append(';');
    }
}

/// <summary>Colour, material and bone weights of the surface at a point.</summary>
internal struct SdfSurface
{
    public float Distance;
    public Vector3 Color;
    public SurfaceMaterial Material;
}

/// <summary>
/// A model sculpted from signed-distance shapes (plan 04, "SDF modelling kit"): spheres, ellipsoids, round cones,
/// round boxes, tori and cylinders, joined with smooth unions, carved with cuts and coloured with paint, each shape
/// carrying a colour, a surface material and the bone it moves with. <see cref="SdfMesher"/> turns it into a smooth
/// mesh with blended vertex colours and skinning weights. Model space: feet at the origin, facing +Z.
/// No GPU calls, so tests can build and mesh models.
/// </summary>
internal sealed class SdfModel
{
    private readonly List<SdfPrimitive> shapes = new();
    private bool prepared;

    public string Name { get; }

    /// <summary>How many bones the shapes refer to (weights are kept for each).</summary>
    public int BoneCount { get; set; } = 1;

    /// <summary>
    /// Bone weights blend over this distance (at least each shape's own blend radius), so skin bends smoothly
    /// round a joint even where the shapes meet with a sharp crease.
    /// </summary>
    public float WeightBlend { get; set; } = 0.035f;

    /// <summary>Colours change over the middle part of a smooth union only, so a sleeve ends in a clean line.</summary>
    public float ColorSharpness { get; set; } = 0.6f;

    public SdfModel(string name) => Name = name;

    public IReadOnlyList<SdfPrimitive> Shapes => shapes;

    public SdfPrimitive Add(SdfPrimitive p)
    {
        shapes.Add(p);
        prepared = false;
        return p;
    }

    // ------------------------------------------------------------------ building

    public SdfPrimitive Sphere(Vector3 c, float r, Color color, int bone, float blend = 0f, SurfaceMaterial mat = SurfaceMaterial.Default, SdfOp op = SdfOp.Add) =>
        Add(new SdfPrimitive { Shape = SdfShape.Sphere, Op = op, A = c, Size = new Vector3(r, 0, 0), Color = color, Bone = bone, Blend = blend, Material = mat });

    public SdfPrimitive Ellipsoid(Vector3 c, Vector3 radii, Color color, int bone, float blend = 0f, SurfaceMaterial mat = SurfaceMaterial.Default,
        Quaternion? rotation = null, SdfOp op = SdfOp.Add) =>
        Add(new SdfPrimitive { Shape = SdfShape.Ellipsoid, Op = op, A = c, Size = radii, Color = color, Bone = bone, Blend = blend, Material = mat, Rotation = rotation ?? Quaternion.Identity });

    /// <summary>A capsule from <paramref name="a"/> to <paramref name="b"/> that tapers from radius <paramref name="ra"/> to <paramref name="rb"/>.</summary>
    public SdfPrimitive Capsule(Vector3 a, Vector3 b, float ra, float rb, Color color, int bone, float blend = 0f, SurfaceMaterial mat = SurfaceMaterial.Default, SdfOp op = SdfOp.Add)
    {
        // The exact round cone needs one end not to swallow the other
        float length = Vector3.Distance(a, b);
        if (MathF.Abs(ra - rb) >= length * 0.98f)
        {
            float r = Math.Max(ra, rb);
            return Sphere(ra >= rb ? a : b, r, color, bone, blend, mat, op);
        }
        return Add(new SdfPrimitive { Shape = SdfShape.RoundCone, Op = op, A = a, B = b, Size = new Vector3(ra, rb, 0), Color = color, Bone = bone, Blend = blend, Material = mat });
    }

    public SdfPrimitive Box(Vector3 c, Vector3 halfExtents, float rounding, Color color, int bone, float blend = 0f, SurfaceMaterial mat = SurfaceMaterial.Default,
        Quaternion? rotation = null, SdfOp op = SdfOp.Add) =>
        Add(new SdfPrimitive
        {
            Shape = SdfShape.RoundBox, Op = op, A = c, Size = halfExtents, Rounding = Math.Min(rounding, Math.Min(halfExtents.X, Math.Min(halfExtents.Y, halfExtents.Z))),
            Color = color, Bone = bone, Blend = blend, Material = mat, Rotation = rotation ?? Quaternion.Identity
        });

    public SdfPrimitive Torus(Vector3 c, float ring, float tube, Color color, int bone, float blend = 0f, SurfaceMaterial mat = SurfaceMaterial.Default,
        Quaternion? rotation = null, SdfOp op = SdfOp.Add) =>
        Add(new SdfPrimitive { Shape = SdfShape.Torus, Op = op, A = c, Size = new Vector3(ring, tube, 0), Color = color, Bone = bone, Blend = blend, Material = mat, Rotation = rotation ?? Quaternion.Identity });

    public SdfPrimitive Cylinder(Vector3 c, float radius, float halfHeight, float rounding, Color color, int bone, float blend = 0f,
        SurfaceMaterial mat = SurfaceMaterial.Default, Quaternion? rotation = null, SdfOp op = SdfOp.Add) =>
        Add(new SdfPrimitive
        {
            Shape = SdfShape.Cylinder, Op = op, A = c, Size = new Vector3(radius, halfHeight, 0), Rounding = Math.Min(rounding, Math.Min(radius, halfHeight)),
            Color = color, Bone = bone, Blend = blend, Material = mat, Rotation = rotation ?? Quaternion.Identity
        });

    /// <summary>
    /// Recolours the surface inside <paramref name="region"/> (a shape just added, which becomes paint), only
    /// on the given bones' surface if any are named.
    /// </summary>
    public SdfPrimitive Paint(SdfPrimitive region, bool paintsMaterial = false, params int[] onBones)
    {
        region.Op = SdfOp.Paint;
        region.PaintsMaterial = paintsMaterial;
        foreach (int b in onBones) region.PaintBones |= 1UL << b;
        prepared = false;
        return region;
    }

    // ------------------------------------------------------------------ evaluation

    public void Prepare()
    {
        if (prepared) return;
        foreach (var s in shapes) s.Prepare();
        prepared = true;
    }

    /// <summary>The box the surface lies in (shapes that only paint or cut don't widen it).</summary>
    public (Vector3 Min, Vector3 Max) Bounds()
    {
        Prepare();
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var s in shapes)
        {
            if (s.Op != SdfOp.Add) continue;
            min = Vector3.Min(min, s.BoundsMin - new Vector3(s.Blend));
            max = Vector3.Max(max, s.BoundsMax + new Vector3(s.Blend));
        }
        return (min, max);
    }

    /// <summary>The signed distance to the model's surface (negative inside).</summary>
    public float Distance(Vector3 p)
    {
        if (!prepared) Prepare();
        float d = float.MaxValue;
        foreach (var s in shapes)
        {
            switch (s.Op)
            {
                case SdfOp.Add:
                {
                    // Outside its box a shape is at least that far away, so if that is further than the current
                    // surface plus its blend radius it can't change anything
                    float box = s.BoundsDistance(p);
                    if (d < float.MaxValue && box > 0f && box > d + s.Blend) continue;
                    float di = s.Distance(p);
                    d = s.Blend > 0f ? SmoothMin(d, di, s.Blend) : Math.Min(d, di);
                    break;
                }
                case SdfOp.Cut:
                {
                    // A cut matters wherever the carved surface is nearer than the current one, deep inside too
                    float box = s.BoundsDistance(p);
                    if (box > 0f && box > Math.Max(0f, -d) + s.Blend) continue;
                    float di = s.Distance(p);
                    d = s.Blend > 0f ? SmoothMax(d, -di, s.Blend) : Math.Max(d, -di);
                    break;
                }
            }
        }
        return d;
    }

    /// <summary>
    /// The surface's colour and material at <paramref name="p"/> (a point on or near it), and the bone weights
    /// there, written into <paramref name="weights"/> (one per bone).
    /// </summary>
    public SdfSurface Surface(Vector3 p, Span<float> weights)
    {
        if (!prepared) Prepare();
        weights.Clear();
        float d = float.MaxValue;
        var color = Vector3.One;
        var material = SurfaceMaterial.Default;
        bool any = false;

        foreach (var s in shapes)
        {
            switch (s.Op)
            {
                case SdfOp.Add:
                {
                    float wBlend = Math.Max(WeightBlend, s.Blend);
                    float box = s.BoundsDistance(p);
                    if (any && box > 0f && box > d + wBlend) continue;
                    float di = s.Distance(p);
                    var ci = Vec(s.Color);
                    if (!any)
                    {
                        d = di;
                        color = ci;
                        material = s.Material;
                        weights[s.Bone] = 1f;
                        any = true;
                        break;
                    }

                    // h = 1 keeps what was there, h = 0 takes the new shape (Inigo Quilez's smooth minimum)
                    float h = s.Blend > 0f ? Math.Clamp(0.5f + 0.5f * (di - d) / s.Blend, 0f, 1f) : (di < d ? 0f : 1f);
                    float hw = Math.Clamp(0.5f + 0.5f * (di - d) / wBlend, 0f, 1f);
                    float hc = Sharpen(h);
                    d = s.Blend > 0f ? Lerp(di, d, h) - s.Blend * h * (1f - h) : Math.Min(d, di);
                    color = Vector3.Lerp(ci, color, hc);
                    if (h < 0.5f) material = s.Material;
                    if (hw < 1f)
                    {
                        for (int b = 0; b < weights.Length; b++) weights[b] *= hw;
                        weights[s.Bone] += 1f - hw;
                    }
                    break;
                }
                case SdfOp.Cut:
                {
                    float box = s.BoundsDistance(p);
                    if (!any || (box > 0f && box > Math.Max(0f, -d) + s.Blend)) continue;
                    float di = s.Distance(p);
                    d = s.Blend > 0f ? SmoothMax(d, -di, s.Blend) : Math.Max(d, -di);
                    break;
                }
                case SdfOp.Paint:
                {
                    if (!any || s.BoundsDistance(p) > s.Blend * 0.5f) continue;
                    if (s.PaintBones != 0 && (s.PaintBones & (1UL << Heaviest(weights))) == 0) continue;
                    float di = s.Distance(p);
                    // A crisp edge, softened over the shape's blend radius if it has one
                    float t = s.Blend > 0f ? Math.Clamp(0.5f - di / s.Blend, 0f, 1f) : (di <= 0f ? 1f : 0f);
                    if (t <= 0f) break;
                    color = Vector3.Lerp(color, Vec(s.Color), t);
                    if (s.PaintsMaterial && t >= 0.5f) material = s.Material;
                    break;
                }
            }
        }

        float sum = 0f;
        foreach (float w in weights) sum += w;
        if (sum > 1e-6f)
            for (int b = 0; b < weights.Length; b++) weights[b] /= sum;
        return new SdfSurface { Distance = d, Color = color, Material = material };
    }

    private static int Heaviest(Span<float> weights)
    {
        int best = 0;
        for (int b = 1; b < weights.Length; b++)
            if (weights[b] > weights[best]) best = b;
        return best;
    }

    private float Sharpen(float h)
    {
        if (ColorSharpness <= 0f) return h;
        float half = 0.5f * (1f - ColorSharpness);
        float t = Math.Clamp((h - half) / Math.Max(1e-4f, 1f - 2f * half), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    /// <summary>A text that changes whenever anything about the model does (for the mesh cache).</summary>
    public string Describe()
    {
        var sb = new StringBuilder();
        sb.Append(Name).Append('|').Append(BoneCount).Append('|').Append(WeightBlend.ToString("R")).Append('|').Append(ColorSharpness.ToString("R")).Append('|');
        foreach (var s in shapes) s.Describe(sb);
        return sb.ToString();
    }

    private static Vector3 Vec(Color c) => new(c.R / 255f, c.G / 255f, c.B / 255f);

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    /// <summary>Polynomial smooth minimum with radius <paramref name="k"/>.</summary>
    public static float SmoothMin(float a, float b, float k)
    {
        if (a == float.MaxValue) return b;
        float h = Math.Clamp(0.5f + 0.5f * (b - a) / k, 0f, 1f);
        return Lerp(b, a, h) - k * h * (1f - h);
    }

    public static float SmoothMax(float a, float b, float k)
    {
        float h = Math.Clamp(0.5f - 0.5f * (b - a) / k, 0f, 1f);
        return Lerp(b, a, h) + k * h * (1f - h);
    }
}
