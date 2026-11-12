using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The modelled kit's prototype (style guide, "Modelled characters"): a grown body built of lofted surfaces with
/// <see cref="PolyBuilder"/> in place of the SDF kit's meshed shapes, on the grown kit's own frame and skeleton, so
/// every clip and every screen that draws a rig draws it. Only the grown Teen hero is built so far.
/// </summary>
internal static partial class CharacterModels
{
    /// <summary>
    /// Builds the looks the modelled kit knows with it (the prototype's switch: off, every look is the SDF kit's, as
    /// before). A rig built one way stays so until it is forgotten.
    /// </summary>
    public static bool Modelled;

    /// <summary>True for a look the modelled kit builds: the grown Teen hero in his own clothes.</summary>
    private static bool ModelledBuilds(CharacterRig rig) =>
        Modelled && rig.Style.Build == BodyBuild.Teen && rig.Type.StartsWith("PLAYER", StringComparison.Ordinal) &&
        rig.Style is { Jacket: true, Skirt: false, Shorts: false, Coat: false, Hair: HairCut.Short, Holds: HeldProp.None, Hat: Headwear.None or Headwear.Beret };

    private const int Around = 32;

    /// <summary>How much larger than the grown frame's a modelled head is, about the chin (style guide, "Modelled characters", "The head").</summary>
    public const float ModelledHeadScale = 1.16f;

    private static float Hr(GrownFrame f) => f.HeadR * ModelledHeadScale;

    /// <summary>The modelled head's middle: the frame's, raised so the chin stays where the frame has it.</summary>
    private static Vector3 Hc(GrownFrame f) => f.HeadC + new Vector3(0, 1f, 0.12f) * (f.HeadR * (ModelledHeadScale - 1f));

    private static CharacterRig BuildModelledRig(CharacterRig rig, GrownFrame f)
    {
        var s = rig.Style;
        rig.Scale = s.Height;
        GrownSkeleton(rig.Skeleton, f);
        var b = new PolyBuilder();
        ModelledHead(b, f, s);
        ModelledHair(b, f, s);
        if (s.Hat == Headwear.Beret) ModelledBeret(b, f, s);
        ModelledBody(b, f, s);
        foreach (float side in new[] { 1f, -1f })
        {
            ModelledArm(b, f, s, side);
            ModelledLeg(b, f, s, side);
            ModelledShoe(b, f, s, side);
        }
        if (s.Scarf) ModelledScarf(b, f, s);
        if (s.Bag is { } bag) ModelledBag(b, f, bag);

        // The face's patch is cut from the plain mesh, by the skin's own colour; then the contact shade is baked in
        float R = Hr(f);
        var between = HeadPoint(f, MathF.Asin(ModelledFaces.EyeHeight), MathF.PI / 2f);
        rig.Face = new FaceLayout(between, ModelledFaces.EyeHalfSpacing * R, new Vector3(0, Hc(f).Y - 0.2f * R, between.Z), 0.66f * R, Modelled: true);
        rig.Mesh = b.Build();
        SoftenModelledFace(rig, f);
        ExtractModelledFacePatch(rig, f);
        var shaded = b.Build(occlusion: 0.6f);
        shaded.Normals = rig.Mesh.Normals;
        rig.Mesh = shaded;
        rig.Shell = b.Build(GrownOutlineThickness * 0.7f);
        rig.PrepareScratch();
        return rig;
    }

    // ------------------------------------------------------------------ the head

    private static float Along(ReadOnlySpan<(float At, float Value)> keys, float at)
    {
        if (at <= keys[0].At) return keys[0].Value;
        for (int i = 0; i + 1 < keys.Length; i++)
            if (at <= keys[i + 1].At)
            {
                float t = (at - keys[i].At) / (keys[i + 1].At - keys[i].At);
                t = t * t * (3f - 2f * t);
                return keys[i].Value + (keys[i + 1].Value - keys[i].Value) * t;
            }
        return keys[^1].Value;
    }

    /// <summary>
    /// A point of the head's surface (style guide, "Modelled characters", "The head"): a ball shaped by its height into
    /// a round skull above the eyes and soft cheeks that close to a small chin below, with a small nose.
    /// <paramref name="phi"/> is the latitude (−π/2 the chin, π/2 the crown), <paramref name="theta"/> the way round
    /// (π/2 the front), and <paramref name="out"/> how far off the surface (the hair), in head radii.
    /// </summary>
    private static Vector3 HeadPoint(GrownFrame f, float phi, float theta, float @out = 0f, bool nose = true)
    {
        float y = MathF.Sin(phi), across = MathF.Cos(phi);
        float wide = Along(stackalloc[] { (-1f, 0.42f), (-0.85f, 0.54f), (-0.65f, 0.66f), (-0.4f, 0.74f), (-0.1f, 0.77f), (0.2f, 0.78f), (0.6f, 0.78f), (1f, 0.77f) }, y);
        float front = Along(stackalloc[] { (-1f, 0.92f), (-0.85f, 0.90f), (-0.65f, 0.84f), (-0.4f, 0.80f), (-0.1f, 0.78f), (0.2f, 0.78f), (0.6f, 0.80f), (1f, 0.82f) }, y);
        float back = Along(stackalloc[] { (-1f, 0.20f), (-0.85f, 0.30f), (-0.65f, 0.52f), (-0.4f, 0.76f), (-0.1f, 0.87f), (0.2f, 0.89f), (0.6f, 0.87f), (1f, 0.82f) }, y);
        float shift = Along(stackalloc[] { (-1f, 0.12f), (-0.85f, 0.09f), (-0.65f, 0.03f), (-0.4f, -0.03f), (-0.1f, -0.06f), (0.2f, -0.08f), (0.6f, -0.10f), (1f, -0.12f) }, y);
        float c = MathF.Cos(theta), sn = MathF.Sin(theta);
        float e = 2f / 2.2f;
        float x = wide * across * MathF.Sign(c) * MathF.Pow(MathF.Abs(c), e);
        float z = (sn > 0f ? front : back) * across * MathF.Sign(sn) * MathF.Pow(MathF.Abs(sn), e);
        var p = new Vector3(x, y, z);
        if (nose && sn > 0f)
        {
            static float Bump(float dx, float dy, float sx, float sy) => MathF.Exp(-dx * dx / (sx * sx) - dy * dy / (sy * sy));
            // A small upturned nose over a soft bridge, and the eyes set a little back under the brow
            p.Z += 0.105f * Bump(x, y + 0.40f, 0.075f, 0.085f) + 0.045f * Bump(x, y + 0.24f, 0.065f, 0.16f);
            p.Z -= 0.03f * (Bump(x - 0.33f, y + 0.06f, 0.2f, 0.15f) + Bump(x + 0.33f, y + 0.06f, 0.2f, 0.15f));
        }
        if (@out != 0f) p += Vector3.Normalize(new Vector3(x, y * 0.9f, z)) * @out;
        p.Z += shift;
        return Hc(f) + p * Hr(f);
    }

    /// <summary>A point of the head by its height (in head radii over the middle) and its angle round from the front (positive to the character's left).</summary>
    private static Vector3 OnHead(GrownFrame f, float y, float signed, float @out) =>
        HeadPoint(f, MathF.Asin(Math.Clamp(y, -0.99f, 0.99f)), MathF.PI / 2f - signed, @out, nose: false);

    private static void ModelledHead(PolyBuilder b, GrownFrame f, CharacterStyle s)
    {
        const int Lon = 80, Lat = 48;
        var head = BoneMix.One(HumanBones.Head);
        var rings = new List<PolyRing>();
        for (int r = 1; r < Lat; r++)
        {
            float phi = -MathF.PI / 2f + MathF.PI * r / Lat;
            var ring = new PolyRing { P = new Vector3[Lon], C = PolyBuilder.Flat(s.Skin, Lon), W = head };
            for (int k = 0; k < Lon; k++) ring.P[k] = HeadPoint(f, phi, MathF.Tau * k / Lon);
            rings.Add(ring);
        }
        b.Tube(rings, SurfaceMaterial.Skin);
        float R = Hr(f);
        var H = Hc(f);
        foreach (float side in new[] { 1f, -1f })
            b.Ball(H + new Vector3(side * 0.75f, -0.16f, -0.10f) * R, new Vector3(0.045f, 0.15f, 0.09f) * R,
                Quaternion.CreateFromYawPitchRoll(side * 0.4f, -0.2f, 0f), s.Skin, head, SurfaceMaterial.Skin);
        float g = f.Girth, sz = f.Size;
        var neck = BoneMix.One(HumanBones.Neck);
        b.Sweep(new[]
        {
            new SweepKey(new Vector3(0, f.NeckFootY - 0.035f * sz, -0.018f * sz), 0.056f * g, 0.054f * g, s.Skin, BoneMix.One(HumanBones.Chest)),
            new SweepKey(new Vector3(0, f.NeckFootY + 0.02f * sz, -0.02f * sz), 0.048f * g, 0.048f * g, s.Skin, neck),
            new SweepKey(H + new Vector3(0, -0.92f, -0.16f) * R, 0.046f * g, 0.047f * g, s.Skin, neck),
            new SweepKey(H + new Vector3(0, -0.6f, -0.2f) * R, 0.045f * g, 0.047f * g, s.Skin, BoneMix.Two(HumanBones.Neck, HumanBones.Head, 0.6f))
        }, Vector3.UnitZ, 18, 3, SurfaceMaterial.Skin);
    }

    /// <summary>Bends the head's skin toward the light of one smooth egg, so the shade's edge crosses the face as one soft line.</summary>
    private static void SoftenModelledFace(CharacterRig rig, GrownFrame f)
    {
        var mesh = rig.Mesh;
        var centre = Hc(f) + new Vector3(0, -0.04f, -0.06f) * Hr(f);
        var radii = new Vector3(0.78f, 1.02f, 0.88f) * Hr(f);
        var forward = Vector3.Normalize(new Vector3(0, 0.6f, 0.8f));
        for (int v = 0; v < mesh.VertexCount; v++)
        {
            if (mesh.Materials[v] != (byte)SurfaceMaterial.Skin || mesh.WeightOf(v, HumanBones.Head) < 0.99f) continue;
            var q = (mesh.Positions[v] - centre) / radii;
            if (q.LengthSquared() < 1e-6f) continue;
            var n = Vector3.Normalize(Vector3.Lerp(mesh.Normals[v], Vector3.Normalize(q / radii), 0.65f));
            float front = Math.Clamp(n.Z * 1.6f + 0.2f, 0f, 1f) * 0.4f;
            mesh.Normals[v] = Vector3.Normalize(Vector3.Lerp(n, forward, front));
        }
    }

    /// <summary>The front of a modelled head as its face's patch: the head's own triangles inside the face's square, a hair's breadth out, mapped onto the texture.</summary>
    private static void ExtractModelledFacePatch(CharacterRig rig, GrownFrame f)
    {
        var mesh = rig.Mesh;
        var face = rig.Face;
        float half = face.TextureHalfSize;
        var c = face.TextureCenter;
        float behind = Hc(f).Z + 0.1f * Hr(f);
        bool Usable(int v)
        {
            var p = mesh.Positions[v];
            return mesh.Materials[v] == (byte)SurfaceMaterial.Skin && mesh.WeightOf(v, HumanBones.Head) > 0.99f && mesh.Colors[v].Equals(rig.Style.Skin) &&
                MathF.Abs(p.X - c.X) < half && MathF.Abs(p.Y - c.Y) < half && p.Z > behind;
        }
        var map = new Dictionary<int, int>();
        var indices = new List<int>();
        int Map(int v)
        {
            if (!map.TryGetValue(v, out int i)) map[v] = i = map.Count;
            return i;
        }
        for (int t = 0; t < mesh.Indices.Length; t += 3)
        {
            int a = mesh.Indices[t], b2 = mesh.Indices[t + 1], d = mesh.Indices[t + 2];
            if (!Usable(a) || !Usable(b2) || !Usable(d)) continue;
            indices.Add(Map(a));
            indices.Add(Map(b2));
            indices.Add(Map(d));
        }
        int n = map.Count;
        var patch = new SdfMesh
        {
            Positions = new Vector3[n], Normals = new Vector3[n], Colors = new Color[n], Materials = new byte[n],
            BoneIndices = new byte[n * 4], BoneWeights = new float[n * 4], Indices = indices.ToArray()
        };
        var uvs = new Vector2[n];
        foreach (var (src, dst) in map)
        {
            var p = mesh.Positions[src];
            patch.Positions[dst] = p + mesh.Normals[src] * GrownFaceLift;
            patch.Normals[dst] = mesh.Normals[src];
            patch.Colors[dst] = Color.White;
            patch.Materials[dst] = (byte)SurfaceMaterial.Skin;
            patch.BoneIndices[dst * 4] = HumanBones.Head;
            patch.BoneWeights[dst * 4] = 1f;
            uvs[dst] = new Vector2(0.5f + (p.X - c.X) / (2f * half), 0.5f - (p.Y - c.Y) / (2f * half));
        }
        rig.FacePatch = patch;
        rig.FaceUVs = uvs;
    }

    // ------------------------------------------------------------------ hair

    private static Color Blend(Color a, Color b, float t) =>
        new((byte)(a.R + (b.R - a.R) * t), (byte)(a.G + (b.G - a.G) * t), (byte)(a.B + (b.B - a.B) * t), (byte)255);

    /// <summary>The hairline's height (in head radii over the middle) at an angle round from the front.</summary>
    private static float Hairline(float a) =>
        Along(stackalloc[] { (0f, 0.50f), (0.7f, 0.48f), (1.0f, 0.30f), (1.28f, -0.10f), (1.5f, 0.12f), (1.68f, 0.06f), (1.9f, -0.42f), (2.5f, -0.60f), (MathF.PI, -0.62f) }, a);

    /// <summary>
    /// A lock of hair: a flat tapering ribbon with a round back, from its root through its belly to a point, lying
    /// along the head (<paramref name="path"/>: height, angle round from the front, and how far off the skull).
    /// </summary>
    private static void Lock(PolyBuilder b, GrownFrame f, (float Y, float A, float Out)[] path, float width, float thick, Color colour, int bone = HumanBones.Head)
    {
        float R = Hr(f);
        var w = BoneMix.One(bone);
        var keys = new List<SweepKey>();
        for (int i = 0; i < path.Length; i++)
        {
            float t = i / (float)(path.Length - 1);
            // Full through its belly, closing to a point
            float girth = MathF.Pow(MathF.Sin(MathF.PI * MathF.Min(1f, 0.18f + t * 0.82f)), 0.7f);
            if (i == path.Length - 1) girth = 0.08f;
            keys.Add(new SweepKey(OnHead(f, path[i].Y, path[i].A, path[i].Out), width * R * MathF.Max(0.08f, girth), thick * R * MathF.Max(0.2f, girth), colour, w));
        }
        var middle = path[path.Length / 2];
        var normal = Vector3.Normalize(OnHead(f, middle.Y, middle.A, middle.Out + 0.2f) - OnHead(f, middle.Y, middle.A, middle.Out));
        b.Sweep(keys, normal, 10, 4, SurfaceMaterial.Hair, 2.4f);
    }

    /// <summary>
    /// Short hair (style guide, "Modelled characters", "Hair"): a shell over the skull from the hairline up, strands
    /// scored into it from the crown, and over it locks laid in layers: a fringe swept across the forehead, a lock
    /// before each ear and a row round the nape that flicks out.
    /// </summary>
    private static void ModelledHair(PolyBuilder b, GrownFrame f, CharacterStyle s)
    {
        if (s.Hair == HairCut.Bald) return;
        const int Lon = 120, Rows = 20;
        var head = BoneMix.One(HumanBones.Head);
        var deep = Shade(s.HairColor, 0.74f);
        var light = Blend(s.HairColor, new Color(255, 236, 224, 255), 0.08f);
        bool hat = s.Hat != Headwear.None;
        var rings = new List<PolyRing>();
        for (int r = 0; r <= Rows; r++)
        {
            float t = r / (float)Rows;
            var ring = new PolyRing { P = new Vector3[Lon], C = new Color[Lon], W = head };
            for (int k = 0; k < Lon; k++)
            {
                float theta = MathF.Tau * k / Lon;
                float signed = MathF.Atan2(MathF.Cos(theta), MathF.Sin(theta));
                float foot = MathF.Asin(Hairline(MathF.Abs(signed)));
                float phi = foot + (MathF.PI / 2f - 0.04f - foot) * t;
                float strand = MathF.Pow(MathF.Abs(MathF.Sin(signed * 8f + (1f - t) * 1.6f)), 0.5f);
                float under = hat ? 1f - 0.85f * Math.Clamp((MathF.Sin(phi) - 0.22f) / 0.2f, 0f, 1f) : 1f + 0.35f * t;
                float lift = (0.10f + 0.035f * strand) * under * MathF.Min(1f, MathF.Sqrt(t / 0.1f)) - 0.02f * (1f - MathF.Min(1f, t / 0.05f));
                ring.P[k] = HeadPoint(f, phi, theta, lift, nose: false);
                ring.C[k] = Blend(deep, s.HairColor, strand);
            }
            rings.Add(ring);
        }
        b.Tube(rings, SurfaceMaterial.Hair, capStart: false);

        // The fringe: locks from under the hat's band (or the crown) across the forehead, swept to one side, uneven
        (float A, float Tip, float Sweep)[] fringe =
        {
            (-1.04f, 0.04f, -0.10f), (-0.80f, 0.20f, 0.04f), (-0.58f, 0.14f, 0.10f), (-0.36f, 0.24f, 0.12f), (-0.14f, 0.16f, 0.14f), (0.08f, 0.26f, 0.14f),
            (0.30f, 0.18f, 0.14f), (0.52f, 0.25f, 0.12f), (0.74f, 0.15f, 0.10f), (0.94f, 0.20f, 0.04f), (1.10f, 0.02f, 0.0f)
        };
        float root = hat ? 0.52f : 0.80f;
        for (int i = 0; i < fringe.Length; i++)
        {
            var (a, tip, sweep) = fringe[i];
            Lock(b, f, new[] { (root, a - sweep * 0.5f, 0.10f), ((root + tip) * 0.5f + 0.06f, a, 0.14f), (tip + 0.09f, a + sweep * 0.7f, 0.12f), (tip, a + sweep, 0.07f) },
                0.14f, 0.06f, i % 3 == 0 ? light : i % 3 == 1 ? s.HairColor : Blend(deep, s.HairColor, 0.5f));
        }
        // Before each ear, down to the jaw
        foreach (float side in new[] { 1f, -1f })
        {
            Lock(b, f, new[] { (0.36f, side * 1.14f, 0.10f), (0.10f, side * 1.20f, 0.12f), (-0.14f, side * 1.18f, 0.09f), (-0.32f, side * 1.12f, 0.05f) }, 0.10f, 0.05f, s.HairColor);
            Lock(b, f, new[] { (0.34f, side * 1.38f, 0.11f), (0.16f, side * 1.44f, 0.13f), (0.04f, side * 1.44f, 0.11f), (-0.06f, side * 1.40f, 0.08f) }, 0.10f, 0.05f, light);
        }
        // Round the nape, flicking out at their ends
        float[] nape = { 1.78f, 2.06f, 2.34f, 2.62f, 2.90f };
        for (int i = 0; i < nape.Length * 2 + 1; i++)
        {
            float a = i == nape.Length * 2 ? MathF.PI : (i % 2 == 0 ? 1f : -1f) * nape[i / 2];
            float drop = 0.06f * MathF.Sin(i * 2.3f) + 0.30f * Math.Clamp((2.5f - MathF.Abs(a)) / 0.7f, 0f, 1f);
            Lock(b, f, new[] { (-0.02f, a, 0.11f), (-0.30f + drop * 0.5f, a, 0.135f), (-0.54f + drop, a * 0.99f, 0.13f), (-0.68f + drop, a * 0.98f, 0.15f) },
                0.17f, 0.06f, i % 3 == 0 ? Blend(deep, s.HairColor, 0.5f) : i % 3 == 1 ? s.HairColor : light, HumanBones.Hair);
        }
    }

    /// <summary>A beret: its band round the head over the hair and a soft crown puffed out over it, tipped back and to one side, with its stalk.</summary>
    private static void ModelledBeret(PolyBuilder b, GrownFrame f, CharacterStyle s)
    {
        float R = Hr(f);
        var head = BoneMix.One(HumanBones.Head);
        var tilt = Quaternion.CreateFromYawPitchRoll(0f, -0.2f, 0.08f);
        Vector3 up = Vector3.Transform(Vector3.UnitY, tilt), fwd = Vector3.Transform(Vector3.UnitZ, tilt), right = Vector3.Transform(Vector3.UnitX, tilt);
        var foot = Hc(f) + new Vector3(0, 0.46f, -0.10f) * R;
        var rings = new List<PolyRing>();
        void Ring(float h, float r, Color c, float lean = 0f) =>
            rings.Add(new PolyRing { P = PolyBuilder.Oval(foot + up * (h * R) + right * (lean * R), right, fwd, r * 0.94f * R, r * R, r * R, 40), C = PolyBuilder.Flat(c, 40), W = head });
        Ring(-0.02f, 0.80f, s.HatBand);
        Ring(0.0f, 0.905f, s.HatBand);
        Ring(0.07f, 0.915f, s.HatBand);
        Ring(0.14f, 0.905f, s.HatBand);
        Ring(0.14f, 0.905f, s.HatColor);
        Ring(0.18f, 1.00f, s.HatColor, 0.01f);
        Ring(0.28f, 1.16f, s.HatColor, 0.03f);
        Ring(0.42f, 1.24f, s.HatColor, 0.05f);
        Ring(0.56f, 1.18f, s.HatColor, 0.06f);
        Ring(0.68f, 0.98f, s.HatColor, 0.06f);
        Ring(0.76f, 0.66f, s.HatColor, 0.05f);
        Ring(0.80f, 0.28f, s.HatColor, 0.05f);
        b.Tube(rings, SurfaceMaterial.Cloth);
        b.Ball(foot + up * (0.83f * R) + right * (0.05f * R), new Vector3(0.06f, 0.08f, 0.06f) * R, tilt, s.HatColor, head, SurfaceMaterial.Cloth, 8, 5);
    }

    // ------------------------------------------------------------------ the body

    /// <summary>How far forward a trunk's loop reaches at <paramref name="x"/> from its middle (its superellipse).</summary>
    private static float FrontAt(float x, float wide, float front, float power = 2.3f) =>
        front * MathF.Pow(MathF.Max(0f, 1f - MathF.Pow(MathF.Abs(x) / wide, power)), 1f / power);

    /// <summary>
    /// The trunk (style guide, "Modelled characters", "The body"): the trousers' seat on the hips, and over it a jacket
    /// that blouses over its ribbed hem, with shoulders that slope from the neck into the sleeves, a stand-up collar,
    /// its zip and two pockets.
    /// </summary>
    private static void ModelledBody(PolyBuilder b, GrownFrame f, CharacterStyle s)
    {
        float sz = f.Size;
        var hips = BoneMix.One(HumanBones.Hips);
        var spine = BoneMix.One(HumanBones.Spine);
        var chest = BoneMix.One(HumanBones.Chest);
        var rib = Shade(s.Top, 0.80f);

        b.Stack(new[]
        {
            new StackKey(f.HipY - 0.085f * sz, f.PelvisW * 0.45f, f.PelvisD * 0.6f, f.PelvisD * 0.6f, -0.005f, s.Bottom, hips),
            new StackKey(f.HipY - 0.03f * sz, f.PelvisW * 0.98f, f.PelvisD * 0.98f, f.PelvisD * 1.05f, -0.005f, s.Bottom, hips),
            new StackKey(f.HipY + 0.04f * sz, f.PelvisW * 1.02f, f.PelvisD, f.PelvisD * 1.08f, -0.005f, s.Bottom, hips),
            new StackKey(f.WaistY - 0.02f * sz, f.WaistW, f.WaistD, f.WaistD, 0f, s.Bottom, BoneMix.Two(HumanBones.Hips, HumanBones.Spine, 0.7f))
        }, Around, 3, SurfaceMaterial.Cloth, 2.3f);

        float half = f.Shoulder.X;
        // Soft folds where the jacket blouses over its hem and under the arms
        float Folds(float along, float theta) =>
            1f + 0.022f * MathF.Sin(theta * 5f + along * 9f) * MathF.Exp(-MathF.Pow((along - 0.36f) / 0.14f, 2f)) * (0.4f + 0.6f * MathF.Abs(MathF.Cos(theta)));
        b.Stack(new[]
        {
            new StackKey(f.HipY + 0.012f * sz, f.PelvisW * 1.02f, f.PelvisD * 1.05f, f.PelvisD * 1.12f, -0.004f, rib, hips),
            new StackKey(f.HipY + 0.020f * sz, f.PelvisW * 1.15f, f.PelvisD * 1.22f, f.PelvisD * 1.28f, -0.004f, rib, hips),
            new StackKey(f.HipY + 0.060f * sz, f.PelvisW * 1.14f, f.PelvisD * 1.22f, f.PelvisD * 1.28f, -0.004f, rib, hips),
            new StackKey(f.HipY + 0.066f * sz, f.PelvisW * 1.26f, f.PelvisD * 1.38f, f.PelvisD * 1.42f, -0.004f, s.Top, BoneMix.Two(HumanBones.Hips, HumanBones.Spine, 0.3f)),
            new StackKey(f.HipY + 0.10f * sz, f.PelvisW * 1.27f, f.PelvisD * 1.40f, f.PelvisD * 1.42f, -0.002f, s.Top, BoneMix.Two(HumanBones.Hips, HumanBones.Spine, 0.6f)),
            new StackKey(f.WaistY, f.WaistW * 1.24f, f.WaistD * 1.22f, f.WaistD * 1.20f, 0f, s.Top, spine),
            new StackKey(f.ChestJointY, f.ChestW * 1.12f, f.ChestD * 1.22f, f.ChestD * 1.16f, 0f, s.Top, BoneMix.Two(HumanBones.Spine, HumanBones.Chest, 0.5f)),
            new StackKey(f.ChestMidY + 0.02f * sz, f.ChestW * 1.16f, f.ChestD * 1.26f, f.ChestD * 1.18f, 0f, s.Top, chest),
            new StackKey(f.ShoulderY - 0.055f * sz, half * 0.95f, f.ChestD * 1.16f, f.ChestD * 1.12f, -0.004f, s.Top, chest),
            new StackKey(f.ShoulderY - 0.022f * sz, half * 0.90f, f.ChestD * 1.02f, f.ChestD * 1.04f, -0.006f, s.Top, chest),
            new StackKey(f.ShoulderY + 0.006f * sz, half * 0.66f, f.ChestD * 0.86f, f.ChestD * 0.92f, -0.008f, s.Top, chest),
            new StackKey(f.NeckFootY + 0.014f * sz, 0.058f * f.Girth, 0.056f * f.Girth, 0.060f * f.Girth, -0.016f * sz, s.Top, chest)
        }, Around, 4, SurfaceMaterial.Cloth, 2.3f, shape: Folds);

        // The collar: a band standing round the neck, lower at the throat than at the nape
        var rings = new List<PolyRing>();
        var at = new Vector3(0, f.NeckFootY, -0.016f * sz);
        void Collar(float y, float r)
        {
            var points = PolyBuilder.Oval(at + new Vector3(0, y * sz, 0), Vector3.UnitX, Vector3.UnitZ, r * f.Girth, r * f.Girth, r * f.Girth * 1.05f, 24);
            for (int k = 0; k < points.Length; k++) points[k].Y -= 0.012f * sz * MathF.Max(0f, MathF.Sin(MathF.Tau * k / points.Length));
            rings.Add(new PolyRing { P = points, C = PolyBuilder.Flat(s.Top, 24), W = chest });
        }
        Collar(-0.012f, 0.068f);
        Collar(0.018f, 0.072f);
        Collar(0.040f, 0.074f);
        Collar(0.044f, 0.068f);
        Collar(0.030f, 0.062f);
        Collar(-0.01f, 0.058f);
        b.Tube(rings, SurfaceMaterial.Cloth, capStart: false, capEnd: false);

        // The zip, down the front from the collar to the hem
        var zip = new List<SweepKey>();
        void Zip(float y, float z, BoneMix w) => zip.Add(new SweepKey(new Vector3(0, y, z + 0.0015f), 0.006f * sz, 0.004f * sz, s.Accent, w));
        Zip(f.ShoulderY - 0.02f * sz, f.ChestD * 1.0f, chest);
        Zip(f.ChestMidY + 0.02f * sz, f.ChestD * 1.26f, chest);
        Zip(f.ChestJointY, f.ChestD * 1.22f, BoneMix.Two(HumanBones.Spine, HumanBones.Chest, 0.5f));
        Zip(f.WaistY, f.WaistD * 1.22f, spine);
        Zip(f.HipY + 0.10f * sz, f.PelvisD * 1.40f - 0.002f, BoneMix.Two(HumanBones.Hips, HumanBones.Spine, 0.6f));
        Zip(f.HipY + 0.066f * sz, f.PelvisD * 1.38f - 0.004f, BoneMix.Two(HumanBones.Hips, HumanBones.Spine, 0.3f));
        Zip(f.HipY + 0.022f * sz, f.PelvisD * 1.22f - 0.004f, hips);
        b.Sweep(zip, Vector3.UnitZ, 8, 3, SurfaceMaterial.Plastic);

        // Two slanted pockets, low on the front
        var welt = Shade(s.Top, 0.55f);
        foreach (float side in new[] { 1f, -1f })
        {
            Vector3 Front(float x, float y) => new(side * x, y, FrontAt(x, f.WaistW * 1.25f, f.WaistD * 1.3f) + 0.002f);
            var w = BoneMix.Two(HumanBones.Hips, HumanBones.Spine, 0.7f);
            b.Sweep(new[]
            {
                new SweepKey(Front(0.042f * sz, f.WaistY - 0.005f * sz), 0.002f * sz, 0.002f * sz, welt, w),
                new SweepKey(Front(0.050f * sz, f.WaistY - 0.022f * sz), 0.005f * sz, 0.003f * sz, welt, w),
                new SweepKey(Front(0.062f * sz, f.WaistY - 0.045f * sz), 0.005f * sz, 0.003f * sz, welt, w),
                new SweepKey(Front(0.070f * sz, f.WaistY - 0.060f * sz), 0.002f * sz, 0.002f * sz, welt, w)
            }, Vector3.UnitZ, 6, 2, SurfaceMaterial.Cloth);
        }
    }

    /// <summary>An arm in its sleeve: a shoulder that carries on the trunk's slope, folds at the elbow, a ribbed cuff, and a hand with fingers.</summary>
    private static void ModelledArm(PolyBuilder b, GrownFrame f, CharacterStyle s, float side)
    {
        float g = f.Girth, sz = f.Size;
        int upperB = side > 0f ? HumanBones.UpperArmL : HumanBones.UpperArmR;
        int foreB = side > 0f ? HumanBones.ForeArmL : HumanBones.ForeArmR;
        int handB = side > 0f ? HumanBones.HandL : HumanBones.HandR;
        Vector3 sh = Side(f.Shoulder, side), el = Side(f.Elbow, side), wr = Side(f.Wrist, side);
        var down = Vector3.Normalize(el - sh);
        var fore = Vector3.Normalize(wr - el);
        var upper = BoneMix.One(upperB);
        var lower = BoneMix.One(foreB);
        var rib = Shade(s.Top, 0.80f);
        var cuff = wr - fore * (0.045f * sz);
        // The sleeve's head sits inside the trunk's shoulder, a little under the joint, so the slope from the neck runs on into it
        var top = sh + new Vector3(-side * 0.012f * sz, -0.02f * sz, 0f);
        float Folds(float along, float theta) =>
            1f + 0.045f * MathF.Sin(along * 95f + MathF.Sin(theta) * 1.5f) * MathF.Exp(-MathF.Pow((along - 0.50f) / 0.09f, 2f))
               + 0.035f * MathF.Sin(along * 120f + theta) * MathF.Exp(-MathF.Pow((along - 0.80f) / 0.07f, 2f));
        b.Sweep(new[]
        {
            new SweepKey(top - down * (0.012f * g), 0.020f * g, 0.024f * g, s.Top, BoneMix.Two(HumanBones.Chest, upperB, 0.4f)),
            new SweepKey(top + down * (0.006f * g), 0.036f * g, 0.042f * g, s.Top, BoneMix.Two(HumanBones.Chest, upperB, 0.6f)),
            new SweepKey(top + down * (0.034f * g), 0.045f * g, 0.050f * g, s.Top, BoneMix.Two(HumanBones.Chest, upperB, 0.85f)),
            new SweepKey(Vector3.Lerp(sh, el, 0.45f), 0.046f * g, 0.048f * g, s.Top, upper),
            new SweepKey(Vector3.Lerp(sh, el, 0.85f), 0.042f * g, 0.043f * g, s.Top, BoneMix.Two(upperB, foreB, 0.2f)),
            new SweepKey(el, 0.041f * g, 0.042f * g, s.Top, BoneMix.Two(upperB, foreB, 0.5f)),
            new SweepKey(Vector3.Lerp(el, wr, 0.18f), 0.041f * g, 0.041f * g, s.Top, BoneMix.Two(upperB, foreB, 0.85f)),
            new SweepKey(Vector3.Lerp(el, cuff, 0.6f), 0.038f * g, 0.037f * g, s.Top, lower),
            new SweepKey(cuff, 0.038f * g, 0.036f * g, s.Top, lower),
            new SweepKey(cuff + fore * (0.004f * sz), 0.031f * g, 0.029f * g, rib, lower),
            new SweepKey(wr - fore * (0.004f * sz), 0.031f * g, 0.029f * g, rib, lower),
            new SweepKey(wr, 0.026f * g, 0.024f * g, rib, lower)
        }, Vector3.UnitZ, 20, 6, SurfaceMaterial.Cloth, shape: Folds);
        ModelledHand(b, f, s.Skin, wr, fore, side, handB);
    }

    /// <summary>A hand (style guide, "Modelled characters", "Hands"): a palm facing the body, four fingers a little curled, and a thumb forward.</summary>
    private static void ModelledHand(PolyBuilder b, GrownFrame f, Color skin, Vector3 wrist, Vector3 along, float side, int bone)
    {
        float L = f.HandLength * 0.80f;
        var w = BoneMix.One(bone);
        var toBody = new Vector3(-side, 0, 0);
        var inward = Vector3.Normalize(toBody - along * Vector3.Dot(toBody, along));
        var forward = Vector3.Normalize(Vector3.Cross(along, inward) * side);
        if (forward.Z < 0f) forward = -forward;
        b.Sweep(new[]
        {
            new SweepKey(wrist - along * (0.04f * L), 0.085f * L, 0.12f * L, skin, w),
            new SweepKey(wrist + along * (0.06f * L), 0.085f * L, 0.15f * L, skin, w),
            new SweepKey(wrist + along * (0.26f * L), 0.085f * L, 0.24f * L, skin, w),
            new SweepKey(wrist + along * (0.46f * L), 0.075f * L, 0.25f * L, skin, w),
            new SweepKey(wrist + along * (0.53f * L), 0.045f * L, 0.22f * L, skin, w)
        }, forward, 14, 2, SurfaceMaterial.Skin, 2.6f);
        (float Across, float Length)[] fingers = { (0.185f, 0.40f), (0.062f, 0.45f), (-0.062f, 0.42f), (-0.185f, 0.33f) };
        foreach (var (across, length) in fingers)
        {
            var knuckle = wrist + along * (0.47f * L) + forward * (across * L);
            var middle = knuckle + along * (length * 0.55f * L) + inward * (0.035f * L);
            var tip = knuckle + along * (length * 0.98f * L) + inward * (0.12f * L);
            b.Sweep(new[]
            {
                new SweepKey(knuckle, 0.056f * L, 0.058f * L, skin, w),
                new SweepKey(middle, 0.052f * L, 0.054f * L, skin, w),
                new SweepKey(tip - (tip - middle) * 0.12f, 0.046f * L, 0.047f * L, skin, w),
                new SweepKey(tip, 0.022f * L, 0.024f * L, skin, w)
            }, forward, 8, 2, SurfaceMaterial.Skin);
        }
        var root = wrist + along * (0.12f * L) + forward * (0.17f * L) + inward * (0.02f * L);
        var joint = root + (along * 0.6f + forward * 0.75f + inward * 0.2f) * (0.2f * L);
        var end = joint + (along * 0.9f + forward * 0.35f + inward * 0.3f) * (0.2f * L);
        b.Sweep(new[]
        {
            new SweepKey(root, 0.08f * L, 0.085f * L, skin, w),
            new SweepKey(joint, 0.066f * L, 0.068f * L, skin, w),
            new SweepKey(end - (end - joint) * 0.15f, 0.056f * L, 0.056f * L, skin, w),
            new SweepKey(end, 0.026f * L, 0.026f * L, skin, w)
        }, inward, 8, 2, SurfaceMaterial.Skin);
    }

    /// <summary>A leg in its trousers, straight-cut: full at the thigh, folds behind the knee, and the cloth stacking over the shoe at its hem.</summary>
    private static void ModelledLeg(PolyBuilder b, GrownFrame f, CharacterStyle s, float side)
    {
        float g = f.Girth, sz = f.Size;
        int thighB = side > 0f ? HumanBones.ThighL : HumanBones.ThighR;
        int shinB = side > 0f ? HumanBones.ShinL : HumanBones.ShinR;
        Vector3 hip = Side(f.LegTop, side), knee = Side(f.Knee, side), ankle = Side(f.Ankle, side);
        var thigh = BoneMix.One(thighB);
        var shin = BoneMix.One(shinB);
        var hem = ankle + new Vector3(0, 0.035f * sz, 0);
        float Folds(float along, float theta) =>
            1f + 0.03f * MathF.Sin(along * 80f + MathF.Cos(theta) * 1.6f) * MathF.Exp(-MathF.Pow((along - 0.50f) / 0.07f, 2f))
               + 0.045f * MathF.Sin(along * 105f + theta * 1.0f) * Math.Clamp((along - 0.76f) / 0.06f, 0f, 1f) * Math.Clamp((0.985f - along) / 0.03f, 0f, 1f);
        b.Sweep(new[]
        {
            new SweepKey(hip + new Vector3(0, 0.05f * sz, 0), 0.058f * g, 0.062f * g, s.Bottom, BoneMix.Two(HumanBones.Hips, thighB, 0.4f)),
            new SweepKey(hip + new Vector3(side * 0.004f, -0.03f * sz, 0), 0.072f * g, 0.076f * g, s.Bottom, BoneMix.Two(HumanBones.Hips, thighB, 0.85f)),
            new SweepKey(Vector3.Lerp(hip, knee, 0.45f) + new Vector3(0, 0, 0.006f * g), 0.064f * g, 0.068f * g, s.Bottom, thigh),
            new SweepKey(Vector3.Lerp(hip, knee, 0.85f), 0.054f * g, 0.056f * g, s.Bottom, BoneMix.Two(thighB, shinB, 0.2f)),
            new SweepKey(knee, 0.051f * g, 0.053f * g, s.Bottom, BoneMix.Two(thighB, shinB, 0.5f)),
            new SweepKey(Vector3.Lerp(knee, ankle, 0.2f) + new Vector3(0, 0, -0.006f * g), 0.050f * g, 0.052f * g, s.Bottom, BoneMix.Two(thighB, shinB, 0.85f)),
            new SweepKey(Vector3.Lerp(knee, ankle, 0.6f), 0.046f * g, 0.047f * g, s.Bottom, shin),
            new SweepKey(hem + new Vector3(0, 0.02f * sz, 0), 0.046f * g, 0.048f * g, s.Bottom, shin),
            new SweepKey(hem, 0.047f * g, 0.050f * g, s.Bottom, shin),
            new SweepKey(hem + new Vector3(0, 0.004f * sz, 0), 0.036f * g, 0.038f * g, Shade(s.Bottom, 0.6f), shin)
        }, Vector3.UnitZ, 20, 6, SurfaceMaterial.Cloth, shape: Folds);
    }

    /// <summary>A trainer: its upper from the heel's counter over the instep to a rubber toe, laced, on a pale sole that shows all round.</summary>
    private static void ModelledShoe(PolyBuilder b, GrownFrame f, CharacterStyle s, float side)
    {
        const int N = 24;
        float sz = f.Size, F = f.FootLength;
        int footB = side > 0f ? HumanBones.FootL : HumanBones.FootR;
        int toeB = side > 0f ? HumanBones.ToeL : HumanBones.ToeR;
        var ankle = Side(f.Ankle, side);
        var sole = s.Sole ?? Shade(s.Shoes, 0.62f);
        float heel = ankle.Z - 0.26f * F, tip = ankle.Z + 0.78f * F;
        float ball = Side(f.Ball, side).Z;
        (float T, float Wide, float Top)[] keys =
        {
            (0f, 0.09f, 0.030f), (0.03f, 0.15f, 0.062f), (0.10f, 0.185f, 0.086f), (0.24f, 0.20f, 0.092f), (0.36f, 0.205f, 0.084f),
            (0.50f, 0.215f, 0.066f), (0.66f, 0.225f, 0.052f), (0.80f, 0.22f, 0.046f), (0.80f, 0.22f, 0.046f), (0.90f, 0.19f, 0.042f), (0.96f, 0.15f, 0.036f), (1f, 0.09f, 0.024f)
        };
        var rings = new List<PolyRing>();
        float thick = 0.02f * sz;
        for (int i = 0; i < keys.Length; i++)
        {
            var (t, wide, top) = keys[i];
            // The rubber toe: everything from the second of the doubled rings on
            bool rubber = i >= 8;
            float z = heel + (tip - heel) * t;
            var points = PolyBuilder.Oval(new Vector3(ankle.X, thick, z), Vector3.UnitX, Vector3.UnitY, wide * F, top * sz - thick, thick, N, 2.6f);
            var colours = new Color[N];
            for (int k = 0; k < N; k++)
            {
                bool under = MathF.Sin(MathF.Tau * k / N) < -0.02f || k == 0 || k == N / 2;
                colours[k] = under || rubber ? sole : s.Shoes;
                if (under) points[k].X += MathF.Sign(points[k].X - ankle.X) * 0.005f * sz;
                if (t > 0.8f) points[k].Y += (t - 0.8f) * 0.03f * sz;
            }
            float toe = Math.Clamp((z - ball + 0.02f * sz) / (0.04f * sz), 0f, 1f);
            rings.Add(new PolyRing { P = points, C = colours, W = BoneMix.Two(footB, toeB, toe) });
        }
        b.Tube(rings, SurfaceMaterial.Leather);
        var foot = BoneMix.One(footB);
        b.Ball(new Vector3(ankle.X, ankle.Y + 0.012f * sz, ankle.Z + 0.005f * sz), new Vector3(0.036f, 0.03f, 0.04f) * f.Girth, Quaternion.Identity, s.Shoes, foot, SurfaceMaterial.Leather, 12, 6);
        // Laces across the instep
        (float T, float Top)[] laces = { (0.40f, 0.079f), (0.47f, 0.070f), (0.54f, 0.062f), (0.61f, 0.056f) };
        foreach (var (t, top) in laces)
            b.Ball(new Vector3(ankle.X, top * sz + 0.001f, heel + (tip - heel) * t), new Vector3(0.105f * F, 0.004f * sz, 0.007f * sz), Quaternion.CreateFromAxisAngle(Vector3.UnitX, 0.2f),
                sole, foot, SurfaceMaterial.Cloth, 8, 4);
    }

    /// <summary>A scarf: a soft roll wound round the neck over the collar, its knot at one side and one end hanging down the chest.</summary>
    private static void ModelledScarf(PolyBuilder b, GrownFrame f, CharacterStyle s)
    {
        float sz = f.Size, g = f.Girth;
        var chest = BoneMix.One(HumanBones.Chest);
        var at = new Vector3(0, f.NeckFootY + 0.012f * sz, -0.014f * sz);
        var rings = new List<PolyRing>();
        void Roll(float y, float r)
        {
            var points = PolyBuilder.Oval(at + new Vector3(0, y * sz, 0), Vector3.UnitX, Vector3.UnitZ, r * g, r * g * 1.04f, r * g * 1.04f, 32);
            for (int k = 0; k < points.Length; k++)
            {
                // Wound: it rides higher on one side, sags at the throat, and its folds run round it
                float a = MathF.Tau * k / points.Length;
                points[k].Y += (0.008f * MathF.Cos(a) - 0.012f * MathF.Max(0f, MathF.Sin(a)) + 0.003f * MathF.Sin(a * 3f + y * 60f)) * sz;
            }
            rings.Add(new PolyRing { P = points, C = PolyBuilder.Flat(y is > 0.005f and < 0.03f ? Shade(s.Accent, 0.9f) : s.Accent, 32), W = chest });
        }
        Roll(-0.022f, 0.070f);
        Roll(-0.018f, 0.088f);
        Roll(-0.004f, 0.097f);
        Roll(0.010f, 0.090f);
        Roll(0.018f, 0.096f);
        Roll(0.034f, 0.090f);
        Roll(0.046f, 0.078f);
        Roll(0.050f, 0.066f);
        Roll(0.034f, 0.060f);
        b.Tube(rings, SurfaceMaterial.Cloth);
        float x = 0.03f * sz;
        b.Ball(new Vector3(x, f.NeckFootY - 0.012f * sz, f.ChestD * 0.98f), new Vector3(0.028f, 0.024f, 0.022f) * sz, Quaternion.Identity, s.Accent, chest, SurfaceMaterial.Cloth, 10, 6);
        b.Sweep(new[]
        {
            new SweepKey(new Vector3(x, f.NeckFootY - 0.015f * sz, f.ChestD * 1.0f), 0.020f * sz, 0.010f * sz, s.Accent, chest),
            new SweepKey(new Vector3(x + 0.004f, f.ShoulderY - 0.06f * sz, f.ChestD * 1.28f), 0.026f * sz, 0.009f * sz, s.Accent, chest),
            new SweepKey(new Vector3(x + 0.008f, f.ChestMidY - 0.01f * sz, f.ChestD * 1.40f), 0.028f * sz, 0.008f * sz, s.Accent, chest),
            new SweepKey(new Vector3(x + 0.010f, f.ChestMidY - 0.06f * sz, f.ChestD * 1.40f), 0.029f * sz, 0.008f * sz, s.Accent, chest),
            new SweepKey(new Vector3(x + 0.010f, f.ChestMidY - 0.064f * sz, f.ChestD * 1.40f), 0.029f * sz, 0.008f * sz, Shade(s.Accent, 0.8f), chest),
            new SweepKey(new Vector3(x + 0.010f, f.ChestMidY - 0.085f * sz, f.ChestD * 1.39f), 0.030f * sz, 0.007f * sz, Shade(s.Accent, 0.8f), chest)
        }, Vector3.UnitZ, 12, 3, SurfaceMaterial.Cloth, 3f);
    }

    /// <summary>A small rucksack high on the back: a rounded body, its flap and clasp, and two straps over the shoulders.</summary>
    private static void ModelledBag(PolyBuilder b, GrownFrame f, Color bag)
    {
        float sz = f.Size;
        var bone = BoneMix.One(HumanBones.Bag);
        var chest = BoneMix.One(HumanBones.Chest);
        var dark = Shade(bag, 0.78f);
        float back = -f.ChestD * 1.18f;
        float z = back - 0.042f * sz;
        float mid = f.ChestMidY - 0.01f * sz;
        b.Stack(new[]
        {
            new StackKey(mid - 0.095f * sz, 0.050f * sz, 0.030f * sz, 0.030f * sz, z, bag, bone),
            new StackKey(mid - 0.085f * sz, 0.072f * sz, 0.044f * sz, 0.046f * sz, z, bag, bone),
            new StackKey(mid - 0.02f * sz, 0.076f * sz, 0.046f * sz, 0.052f * sz, z, bag, bone),
            new StackKey(mid + 0.05f * sz, 0.072f * sz, 0.044f * sz, 0.046f * sz, z, bag, bone),
            new StackKey(mid + 0.078f * sz, 0.060f * sz, 0.036f * sz, 0.036f * sz, z, bag, bone),
            new StackKey(mid + 0.086f * sz, 0.036f * sz, 0.022f * sz, 0.022f * sz, z, bag, bone)
        }, 24, 3, SurfaceMaterial.Leather, 3.2f);
        b.Stack(new[]
        {
            new StackKey(mid + 0.005f * sz, 0.060f * sz, 0.02f * sz, 0.012f * sz, z - 0.046f * sz, dark, bone),
            new StackKey(mid + 0.012f * sz, 0.070f * sz, 0.02f * sz, 0.014f * sz, z - 0.046f * sz, dark, bone),
            new StackKey(mid + 0.06f * sz, 0.068f * sz, 0.02f * sz, 0.012f * sz, z - 0.040f * sz, dark, bone),
            new StackKey(mid + 0.08f * sz, 0.050f * sz, 0.02f * sz, 0.006f * sz, z - 0.030f * sz, dark, bone)
        }, 16, 2, SurfaceMaterial.Leather, 3.2f);
        b.Ball(new Vector3(0, mid + 0.008f * sz, z - 0.060f * sz), new Vector3(0.012f, 0.012f, 0.006f) * sz, Quaternion.Identity, new Color(240, 240, 244, 255), bone, SurfaceMaterial.Metal, 8, 5);
        foreach (float side in new[] { 1f, -1f })
        {
            float x = side * f.Shoulder.X * 0.58f;
            var strap = new List<SweepKey>();
            void At(float sx, float y, float sz_, BoneMix w) => strap.Add(new SweepKey(new Vector3(sx, y, sz_), 0.011f * sz, 0.007f * sz, dark, w));
            At(x * 0.8f, mid + 0.07f * sz, back - 0.012f * sz, bone);
            At(x, f.ShoulderY - 0.004f * sz, -f.ChestD * 0.8f, chest);
            At(x * 1.02f, f.ShoulderY + 0.022f * sz, -0.004f, chest);
            At(x * 1.04f, f.ShoulderY - 0.014f * sz, f.ChestD * 0.98f, chest);
            At(x * 1.14f, f.ChestMidY + 0.03f * sz, f.ChestD * 1.27f, chest);
            At(x * 1.30f, f.ChestMidY - 0.04f * sz, f.ChestD * 1.12f, chest);
            At(x * 1.42f, f.ChestJointY, f.ChestD * 0.2f, chest);
            At(x * 1.3f, mid - 0.06f * sz, back - 0.004f * sz, bone);
            b.Sweep(strap, new Vector3(side, 0.2f, 0f), 8, 3, SurfaceMaterial.Leather, 3f);
        }
    }
}
