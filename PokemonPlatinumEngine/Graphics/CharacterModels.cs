using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

internal enum RigKind { Humanoid, Briefcase, Rift }

/// <summary>Animation inputs for one frame of a character.</summary>
internal struct CharacterPose
{
    /// <summary>Walk cycle: one unit per two steps.</summary>
    public float Walk;

    /// <summary>0 standing still .. 1 walking; blends the walk cycle in and out.</summary>
    public float WalkBlend;

    public bool Running;

    /// <summary>Progress through a ledge hop (0 when on the ground).</summary>
    public float Hop;

    /// <summary>Seconds, for idle breathing and effects.</summary>
    public float Time;

    public bool Blink;
}

/// <summary>One rigid piece of a character, drawn with its own transform.</summary>
internal sealed class RigPart
{
    public MeshBuilder? Geometry;
    public MeshBuilder? FaceGeometry;
    public Vector3 Pivot;

    public Mesh Body;
    public Mesh Outline;
    public Mesh Face;
    public bool Uploaded;
    public bool HasFace;
}

/// <summary>
/// A low-poly chibi character in the style of the Diamond/Pearl remakes: big round head with a painted pixel-art
/// face, short body, and limbs on pivots so they can swing. Model space: feet at the origin, facing +Z.
/// </summary>
internal sealed class CharacterRig
{
    public RigKind Kind;
    public float Scale = 1f;

    /// <summary>
    /// Vertical stretch matching the field, whose walls and furniture are drawn taller than life so they read
    /// from the steep camera. The head is stretched less than the body so it still looks round on screen.
    /// </summary>
    public float BodyStretch = 1f, HeadStretch = 1f;

    public readonly RigPart Torso = new(), Head = new(), ArmL = new(), ArmR = new(), LegL = new(), LegR = new();
    public PixelCanvas? FaceOpenArt, FaceBlinkArt;
    public Material FaceOpen, FaceBlink;

    public IEnumerable<RigPart> Parts
    {
        get
        {
            yield return LegL; yield return LegR; yield return Torso;
            yield return ArmL; yield return ArmR; yield return Head;
        }
    }

    // Model-space joint positions (before Scale)
    public const float HipY = 0.30f, HipX = 0.078f;
    public const float TorsoPivotY = 0.29f;
    public const float ShoulderY = 0.265f, ShoulderX = 0.165f, NeckY = 0.33f;
    public const float HeadRadius = 0.265f;
    public static readonly Vector3 HeadCenter = new(0, 0.245f, 0.005f);

    /// <summary>
    /// World transform of each part for a pose (System.Numerics row-vector convention). <paramref name="root"/>
    /// places the feet and turns the model; <paramref name="trueProportions"/> skips the vertical stretch
    /// (used for the shadow pass, so shadows aren't stretched too).
    /// </summary>
    public void PartTransforms(CharacterPose pose, Matrix4x4 root, Span<Matrix4x4> result, bool trueProportions = false)
    {
        float bodyY = trueProportions ? 1f : BodyStretch;
        float headY = trueProportions ? 1f : HeadStretch;
        root = Matrix4x4.CreateScale(Scale, Scale * bodyY, Scale) * root;

        if (Kind == RigKind.Rift)
        {
            float t = pose.Time;
            var floating = Matrix4x4.CreateTranslation(0, 0.08f * MathF.Sin(t * 1.7f), 0) * root;
            float pulse = 1f + 0.06f * MathF.Sin(t * 3.1f);
            result[2] = Matrix4x4.CreateScale(pulse) * Matrix4x4.CreateTranslation(Torso.Pivot) * floating;
            result[5] = Matrix4x4.CreateRotationY(t * 1.9f) * Matrix4x4.CreateRotationX(0.9f) * Matrix4x4.CreateTranslation(Head.Pivot) * floating;
            result[3] = Matrix4x4.CreateRotationY(-t * 1.3f) * Matrix4x4.CreateRotationZ(1.1f) * Matrix4x4.CreateTranslation(ArmL.Pivot) * floating;
            result[0] = result[1] = result[4] = floating;
            return;
        }
        if (Kind == RigKind.Briefcase)
        {
            for (int i = 0; i < result.Length; i++) result[i] = root;
            return;
        }

        float phase = pose.Walk * MathF.Tau;
        float blend = pose.WalkBlend;
        float swing = blend * (pose.Running ? 0.78f : 0.55f) * MathF.Sin(phase);
        float bob = blend * (pose.Running ? 0.04f : 0.024f) * MathF.Abs(MathF.Sin(phase));
        float idle = 1f - blend;
        float breath = 1f + 0.016f * MathF.Sin(pose.Time * 2.3f) * idle;
        float pitch = blend * (pose.Running ? 0.2f : 0.06f);
        float twist = swing * 0.18f;

        // Ledge hop: legs trail behind, arms fly up
        float hop = MathF.Sin(Math.Clamp(pose.Hop, 0f, 1f) * MathF.PI);
        float legHop = 0.55f * hop, armHop = -2.0f * hop;

        var body = Matrix4x4.CreateTranslation(0, bob, 0) * root;
        result[0] = Matrix4x4.CreateRotationX(swing + legHop) * Matrix4x4.CreateTranslation(LegL.Pivot) * body;
        result[1] = Matrix4x4.CreateRotationX(-swing + legHop) * Matrix4x4.CreateTranslation(LegR.Pivot) * body;

        var torso = Matrix4x4.CreateScale(1f, breath, 1f) * Matrix4x4.CreateRotationY(twist) * Matrix4x4.CreateRotationX(pitch)
            * Matrix4x4.CreateTranslation(Torso.Pivot) * body;
        result[2] = torso;

        float armSwing = swing * (pose.Running ? 1.4f : 1.1f);
        float armBend = pose.Running ? 0.35f * blend : 0f;
        result[3] = Matrix4x4.CreateRotationX(-armSwing + armHop - armBend) * Matrix4x4.CreateRotationZ(-0.14f - 0.5f * hop)
            * Matrix4x4.CreateTranslation(ArmL.Pivot) * torso;
        result[4] = Matrix4x4.CreateRotationX(armSwing + armHop - armBend) * Matrix4x4.CreateRotationZ(0.14f + 0.5f * hop)
            * Matrix4x4.CreateTranslation(ArmR.Pivot) * torso;

        // The head tips back a little so the face reads from the high field camera, and nods while breathing
        float nod = 0.035f * MathF.Sin(pose.Time * 2.3f + 0.6f) * idle + 0.05f * blend * MathF.Sin(phase * 2f);
        result[5] = Matrix4x4.CreateScale(1f, headY / bodyY, 1f) * Matrix4x4.CreateRotationX(-0.2f + nod)
            * Matrix4x4.CreateTranslation(Head.Pivot) * torso;
    }
}

/// <summary>Builds and caches the 3D character rigs, one per character type.</summary>
internal static class CharacterModels
{
    public const float OutlineThickness = 0.011f;
    private static readonly Dictionary<string, CharacterRig> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static RigKind KindOf(string npcType) => npcType.ToUpperInvariant() switch
    {
        "STARTERBRIEFCASE" => RigKind.Briefcase,
        "RIFT" => RigKind.Rift,
        _ => RigKind.Humanoid
    };

    /// <summary>The GPU-ready rig for a character type (built and uploaded on first use).</summary>
    public static CharacterRig Get(string npcType, Shader characterShader)
    {
        string key = npcType;
        if (Cache.TryGetValue(key, out var rig)) return rig;
        rig = Build(npcType);
        Upload(rig, characterShader);
        Cache[key] = rig;
        return rig;
    }

    /// <summary>CPU-side geometry only (no GPU needed).</summary>
    public static CharacterRig Build(string npcType)
    {
        var kind = KindOf(npcType);
        var rig = new CharacterRig { Kind = kind };
        switch (kind)
        {
            case RigKind.Briefcase: BuildBriefcase(rig); break;
            case RigKind.Rift: BuildRift(rig); break;
            default: BuildHumanoid(rig, CharacterStyle.For(npcType)); break;
        }
        return rig;
    }

    private static void Upload(CharacterRig rig, Shader shader)
    {
        foreach (var part in rig.Parts)
        {
            if (part.Geometry == null || part.Geometry.VertexCount == 0) continue;

            // Outlines hug the whole part, face included
            var hullSource = part.Geometry.Clone();
            if (part.FaceGeometry != null) hullSource.Append(part.FaceGeometry, Matrix4x4.Identity);
            var hull = hullSource.BuildOutline(OutlineThickness, c => PixelCanvas.Mix(c, new Color(34, 26, 40, 255), ArtLook.OutlineInkAmount));

            part.Body = part.Geometry.Upload();
            part.Outline = hull.Upload();
            if (part.FaceGeometry != null && part.FaceGeometry.VertexCount > 0)
            {
                part.Face = part.FaceGeometry.Upload();
                part.HasFace = true;
            }
            part.Uploaded = true;
        }

        if (rig.FaceOpenArt != null && rig.FaceBlinkArt != null)
        {
            rig.FaceOpen = FaceMaterial(rig.FaceOpenArt, shader);
            rig.FaceBlink = FaceMaterial(rig.FaceBlinkArt, shader);
        }
    }

    private static Material FaceMaterial(PixelCanvas art, Shader shader)
    {
        var tex = art.ToTexture();
        Raylib.SetTextureWrap(tex, TextureWrap.Clamp);
        var material = Raylib.LoadMaterialDefault();
        material.Shader = shader;
        Raylib.SetMaterialTexture(ref material, MaterialMapIndex.Albedo, tex);
        return material;
    }

    // ------------------------------------------------------------------ humanoids

    private static Color Shade(Color c, float f) => MeshBuilder.Scale(c, f);

    private static void BuildHumanoid(CharacterRig rig, CharacterStyle s)
    {
        rig.Scale = 1.08f * s.Height;
        rig.BodyStretch = 1.45f;
        rig.HeadStretch = 1.12f;
        var skin = s.Skin;

        // Legs (pivot at the hip)
        foreach (var (part, x) in new[] { (rig.LegL, -CharacterRig.HipX), (rig.LegR, CharacterRig.HipX) })
        {
            part.Pivot = new Vector3(x, CharacterRig.HipY, 0);
            var b = new MeshBuilder();
            var legColor = s.Skirt || s.Shorts ? skin : s.Bottom;
            b.Lathe(Vector3.Zero, new[]
            {
                new Vector2(0, -0.27f), new Vector2(0.052f, -0.27f), new Vector2(0.057f, -0.12f),
                new Vector2(0.064f, -0.02f), new Vector2(0.05f, 0.03f), new Vector2(0, 0.04f)
            }, 10, i => s.Shorts && i >= 3 ? s.Bottom : (s.Skirt && i <= 1 ? Shade(s.Accent, 0.95f) : legColor));
            b.Ellipsoid(new Vector3(0, -0.272f, 0.028f), new Vector3(0.068f, 0.05f, 0.1f),
                k => k <= 2 ? Shade(s.Shoes, 0.7f) : s.Shoes, 12, 6);
            part.Geometry = b;
        }

        // Torso (pivot at the hips so breathing lifts the chest)
        rig.Torso.Pivot = new Vector3(0, CharacterRig.TorsoPivotY, 0);
        var t = new MeshBuilder();
        t.Lathe(Vector3.Zero, new[]
        {
            new Vector2(0, -0.03f), new Vector2(0.12f, -0.03f), new Vector2(0.15f, 0.02f), new Vector2(0.158f, 0.1f),
            new Vector2(0.152f, 0.19f), new Vector2(0.135f, 0.25f), new Vector2(0.1f, 0.3f), new Vector2(0.045f, 0.335f),
            new Vector2(0, 0.34f)
        }, 16, i => i <= 1 && !s.Skirt && !s.Coat ? s.Bottom : s.Top, 1f, 0.78f);

        if (s.Stripes)
        {
            var stripe = PixelCanvas.Light1(s.Top, 0.55f);
            foreach (float y in new[] { 0.09f, 0.17f })
                t.Lathe(Vector3.Zero, new[] { new Vector2(0.162f, y), new Vector2(0.16f, y + 0.035f) }, 16, stripe, 1f, 0.78f);
        }
        if (s.Skirt)
        {
            t.Lathe(Vector3.Zero, new[]
            {
                new Vector2(0, -0.15f), new Vector2(0.235f, -0.15f), new Vector2(0.215f, -0.1f),
                new Vector2(0.17f, 0.0f), new Vector2(0.15f, 0.05f), new Vector2(0, 0.05f)
            }, 16, i => i <= 1 ? Shade(s.Bottom, 0.85f) : s.Bottom, 1f, 0.82f);
        }
        if (s.Coat)
        {
            // Long lab coat with a lighter shirt showing down the front
            t.Lathe(Vector3.Zero, new[]
            {
                new Vector2(0, -0.2f), new Vector2(0.19f, -0.2f), new Vector2(0.175f, -0.05f),
                new Vector2(0.165f, 0.05f), new Vector2(0, 0.05f)
            }, 16, s.Top, 1f, 0.84f);
            t.Ellipsoid(new Vector3(0, 0.2f, 0.1f), new Vector3(0.05f, 0.1f, 0.03f), s.Accent, 10, 6);
        }

        // Scarf or collar
        t.Torus(new Vector3(0, 0.315f, 0), 0.085f, 0.036f, s.Accent, 16, 8, 1f, 0.82f);

        if (s.Bag.HasValue)
        {
            var bag = s.Bag.Value;
            t.Ellipsoid(new Vector3(0, 0.15f, -0.125f), new Vector3(0.125f, 0.12f, 0.07f), bag, 14, 8);
            t.Ellipsoid(new Vector3(0, 0.21f, -0.15f), new Vector3(0.115f, 0.06f, 0.045f), Shade(bag, 0.85f), 12, 6);
            // Straps over the shoulders
            foreach (float x in new[] { -0.085f, 0.085f })
                t.Add(Matrix4x4.CreateRotationX(MathF.PI / 2f) * Matrix4x4.CreateRotationZ(MathF.PI / 2f) * Matrix4x4.CreateTranslation(x, 0.24f, -0.01f),
                    p => p.Torus(Vector3.Zero, 0.105f, 0.016f, Shade(bag, 0.8f), 14, 6, 1f, 0.75f));
        }
        rig.Torso.Geometry = t;

        // Arms (pivot at the shoulder, children of the torso)
        foreach (var (part, x) in new[] { (rig.ArmL, -CharacterRig.ShoulderX), (rig.ArmR, CharacterRig.ShoulderX) })
        {
            part.Pivot = new Vector3(x, CharacterRig.ShoulderY, 0);
            var b = new MeshBuilder();
            b.Lathe(Vector3.Zero, new[]
            {
                new Vector2(0, -0.2f), new Vector2(0.041f, -0.2f), new Vector2(0.045f, -0.08f),
                new Vector2(0.052f, 0f), new Vector2(0.035f, 0.035f), new Vector2(0, 0.04f)
            }, 10, i => s.ShortSleeves && i <= 1 ? skin : s.Top);
            b.Ellipsoid(new Vector3(0, -0.225f, 0), new Vector3(0.05f, 0.055f, 0.05f), skin, 10, 6);
            part.Geometry = b;
        }

        // Head (pivot at the neck, child of the torso)
        rig.Head.Pivot = new Vector3(0, CharacterRig.NeckY, 0);
        BuildHead(rig, s);
    }

    private static void BuildHead(CharacterRig rig, CharacterStyle s)
    {
        const float R = CharacterRig.HeadRadius;
        var H = CharacterRig.HeadCenter;
        var head = new MeshBuilder();
        head.Lathe(new Vector3(0, -0.03f, 0), new[] { new Vector2(0, -0.01f), new Vector2(0.045f, -0.01f), new Vector2(0.045f, 0.06f), new Vector2(0, 0.07f) }, 10, s.Skin);
        head.Ellipsoid(H, new Vector3(R, R * 0.96f, R * 0.98f), Color.White, 22, 14);

        // The front of the head is textured with the face; the rest is plain skin
        var face = head.Extract(c => c.Z - H.Z > -0.06f && c.Y > -0.02f);
        face.MapUVs(p => new Vector2(0.5f + (p.X - H.X) / (1.8f * R), 0.5f - (p.Y - H.Y) / (1.8f * R)));
        var skinned = new MeshBuilder();
        skinned.Append(head, Matrix4x4.Identity);
        head = Recolor(skinned, c => c.R == 255 && c.G == 255 && c.B == 255 ? s.Skin : c);

        var hair = s.HairColor;
        var hairDark = Shade(hair, 0.82f);
        switch (s.Hair)
        {
            case HairCut.Spiky:
                head.Ellipsoid(H + new Vector3(0, 0.03f, -0.04f), new Vector3(R * 1.06f, R * 0.98f, R * 1.06f), hair, 20, 12);
                foreach (var d in new[]
                {
                    new Vector3(0, 1, 0.2f), new Vector3(0.55f, 0.75f, -0.1f), new Vector3(-0.55f, 0.75f, -0.1f),
                    new Vector3(0.35f, 0.55f, -0.75f), new Vector3(-0.35f, 0.55f, -0.75f), new Vector3(0, 0.3f, -1f),
                    new Vector3(0.8f, 0.3f, 0.2f), new Vector3(-0.8f, 0.3f, 0.2f), new Vector3(0.2f, 0.7f, 0.65f),
                    new Vector3(-0.25f, 0.65f, 0.6f)
                })
                {
                    var dir = Vector3.Normalize(d);
                    head.Add(AlignY(dir, H + new Vector3(0, 0.03f, -0.04f) + dir * R * 0.9f),
                        p => p.Cone(Vector3.Zero, 0.085f, 0.15f, hair, 8));
                }
                break;

            case HairCut.Swept:
                head.Ellipsoid(H + new Vector3(0, 0.04f, -0.045f), new Vector3(R * 1.07f, R * 0.96f, R * 1.08f), hair, 20, 12);
                head.Ellipsoid(H + new Vector3(0, 0.1f, -0.16f), new Vector3(R * 0.8f, R * 0.55f, R * 0.5f), hairDark, 16, 8);
                break;

            default:
                head.Ellipsoid(H + new Vector3(0, 0.035f, -0.045f), new Vector3(R * 1.075f, R * 1f, R * 1.08f), hair, 20, 12);
                // Bangs across the forehead
                foreach (float x in new[] { -0.36f, 0f, 0.36f })
                {
                    var c = H + new Vector3(x * R, 0.6f * R, 0.8f * R);
                    head.Ellipsoid(c, new Vector3(0.085f, 0.07f, 0.05f), x == 0 ? hair : hairDark, 10, 6);
                }
                if (s.Hair == HairCut.Long)
                {
                    head.Ellipsoid(H + new Vector3(0, -0.13f, -0.1f), new Vector3(0.25f, 0.27f, 0.15f), hairDark, 16, 10);
                    foreach (float x in new[] { -0.23f, 0.23f })
                        head.Ellipsoid(H + new Vector3(x, -0.1f, 0.03f), new Vector3(0.065f, 0.15f, 0.08f), hair, 10, 8);
                }
                break;
        }

        switch (s.Hat)
        {
            case Headwear.Beret:
                head.Add(Matrix4x4.CreateRotationX(-0.22f) * Matrix4x4.CreateTranslation(H + new Vector3(0, R * 0.7f, -0.03f)), p =>
                {
                    p.Lathe(Vector3.Zero, new[]
                    {
                        new Vector2(0, -0.02f), new Vector2(0.27f, -0.02f), new Vector2(0.31f, 0.035f),
                        new Vector2(0.29f, 0.085f), new Vector2(0.2f, 0.125f), new Vector2(0, 0.14f)
                    }, 20, i => i <= 1 ? Shade(s.HatColor, 0.8f) : s.HatColor);
                    p.Torus(new Vector3(0, -0.005f, 0), 0.262f, 0.028f, s.HatBand, 20, 6);
                    p.Ellipsoid(new Vector3(0, 0.15f, 0), new Vector3(0.03f, 0.03f, 0.03f), Shade(s.HatColor, 0.85f), 8, 4);
                });
                break;
            case Headwear.Cap:
                head.Add(Matrix4x4.CreateRotationX(-0.12f) * Matrix4x4.CreateTranslation(H + new Vector3(0, R * 0.32f, -0.01f)), p =>
                {
                    p.Lathe(Vector3.Zero, new[]
                    {
                        new Vector2(0, -0.01f), new Vector2(0.29f, -0.01f), new Vector2(0.28f, 0.1f),
                        new Vector2(0.21f, 0.18f), new Vector2(0, 0.21f)
                    }, 20, s.HatColor);
                    p.Ellipsoid(new Vector3(0, 0.0f, 0.24f), new Vector3(0.2f, 0.028f, 0.16f), s.HatBand, 14, 4);
                    p.Ellipsoid(new Vector3(0, 0.215f, 0), new Vector3(0.035f, 0.02f, 0.035f), s.HatBand, 8, 4);
                });
                break;
            case Headwear.NurseCap:
                head.Add(Matrix4x4.CreateRotationX(-0.25f) * Matrix4x4.CreateTranslation(H + new Vector3(0, R * 0.95f, 0.02f)), p =>
                {
                    p.Ellipsoid(Vector3.Zero, new Vector3(0.16f, 0.065f, 0.09f), s.HatColor, 14, 6);
                    p.Ellipsoid(new Vector3(0, 0.01f, 0.085f), new Vector3(0.05f, 0.03f, 0.012f), s.HatBand, 8, 4);
                });
                break;
        }

        rig.Head.Geometry = head;
        rig.Head.FaceGeometry = face;
        rig.FaceOpenArt = PaintFace(s, blink: false);
        rig.FaceBlinkArt = PaintFace(s, blink: true);
    }

    /// <summary>Rotation that turns +Y onto <paramref name="dir"/>, then moves to <paramref name="at"/>.</summary>
    private static Matrix4x4 AlignY(Vector3 dir, Vector3 at)
    {
        var axis = Vector3.Cross(Vector3.UnitY, dir);
        float angle = MathF.Acos(Math.Clamp(Vector3.Dot(Vector3.UnitY, dir), -1f, 1f));
        var rotation = axis.LengthSquared() < 1e-8f ? Matrix4x4.Identity : Matrix4x4.CreateFromAxisAngle(Vector3.Normalize(axis), angle);
        return rotation * Matrix4x4.CreateTranslation(at);
    }

    private static MeshBuilder Recolor(MeshBuilder source, Func<Color, Color> map)
    {
        var copy = new MeshBuilder();
        copy.AppendRecolored(source, map);
        return copy;
    }

    /// <summary>32x32 pixel-art face mapped onto the front of the head (eyes open or closed).</summary>
    public static PixelCanvas PaintFace(CharacterStyle s, bool blink)
    {
        var c = new PixelCanvas(32, 32);
        c.Fill(s.Skin);
        var eye = s.Eyes;
        var iris = PixelCanvas.Light1(eye, 0.35f);
        var lash = PixelCanvas.Mix(eye, new Color(20, 16, 30, 255), 0.5f);
        var blush = PixelCanvas.Mix(s.Skin, new Color(250, 130, 140, 255), 0.45f);
        var brow = PixelCanvas.Shadow(s.HairColor, 0.25f);

        foreach (int ex in new[] { 8, 20 })
        {
            if (blink)
            {
                c.HLine(ex, 17, 4, lash);
                c.Set(ex - 1, 16, lash);
                c.Set(ex + 4, 16, lash);
            }
            else
            {
                c.HLine(ex + 1, 13, 2, lash);
                c.Rect(ex, 14, 4, 5, eye);
                c.HLine(ex + 1, 19, 2, eye);
                c.Rect(ex + 1, 16, 2, 3, iris);
                c.Rect(ex + 1, 14, 2, 2, Color.White);
                c.Set(ex - 1, 14, lash);
                c.Set(ex + 4, 14, lash);
            }
            c.HLine(ex, 11, 4, brow);
        }

        c.Rect(4, 20, 3, 2, blush);
        c.Rect(25, 20, 3, 2, blush);
        c.HLine(15, 22, 2, new Color(170, 80, 80, 255));

        if (s.Mustache)
        {
            var white = s.HairColor;
            var grey = Shade(s.HairColor, 0.78f);
            foreach (int ex in new[] { 7, 19 }) c.Rect(ex, 9, 6, 3, white);
            c.Rect(10, 20, 12, 3, white);
            c.Rect(9, 21, 2, 3, white);
            c.Rect(21, 21, 2, 3, white);
            c.HLine(11, 23, 10, grey);
        }
        return c;
    }

    // ------------------------------------------------------------------ props

    private static void BuildBriefcase(CharacterRig rig)
    {
        rig.Scale = 1f;
        var leather = new Color(170, 112, 64, 255);
        var b = new MeshBuilder();
        b.Box(new Vector3(-0.26f, 0.02f, -0.1f), new Vector3(0.26f, 0.34f, 0.1f), leather, BoxFaces.All);
        b.Box(new Vector3(-0.265f, 0.2f, -0.105f), new Vector3(0.265f, 0.225f, 0.105f), Shade(leather, 0.7f), BoxFaces.All);
        b.Box(new Vector3(-0.27f, 0f, -0.11f), new Vector3(0.27f, 0.03f, 0.11f), Shade(leather, 0.8f), BoxFaces.All);
        b.Add(Matrix4x4.CreateRotationX(MathF.PI / 2f) * Matrix4x4.CreateTranslation(0, 0.37f, 0),
            p => p.Torus(Vector3.Zero, 0.075f, 0.022f, Shade(leather, 0.75f), 14, 6));
        foreach (float x in new[] { -0.14f, 0.14f })
            b.Box(new Vector3(x - 0.03f, 0.19f, 0.1f), new Vector3(x + 0.03f, 0.25f, 0.12f), new Color(250, 210, 80, 255), BoxFaces.All);
        rig.Torso.Geometry = b;
    }

    private static void BuildRift(CharacterRig rig)
    {
        rig.Scale = 1f;
        rig.Torso.Pivot = new Vector3(0, 0.9f, 0);
        rig.Head.Pivot = rig.Torso.Pivot;
        rig.ArmL.Pivot = rig.Torso.Pivot;

        var core = new MeshBuilder();
        core.Ellipsoid(Vector3.Zero, new Vector3(0.3f, 0.46f, 0.3f), k => PixelCanvas.Mix(new Color(20, 8, 30, 255), new Color(110, 40, 150, 255), k / 12f), 16, 12);
        core.Ellipsoid(new Vector3(0, 0, 0.12f), new Vector3(0.16f, 0.26f, 0.2f), new Color(200, 50, 80, 255), 12, 8);
        rig.Torso.Geometry = core;

        var ring = new MeshBuilder();
        ring.Torus(Vector3.Zero, 0.46f, 0.04f, new Color(210, 60, 90, 255), 24, 6);
        rig.Head.Geometry = ring;

        var ring2 = new MeshBuilder();
        ring2.Torus(Vector3.Zero, 0.56f, 0.028f, new Color(150, 90, 210, 255), 24, 6);
        rig.ArmL.Geometry = ring2;
    }
}
