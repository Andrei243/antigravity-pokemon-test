using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

internal enum RigKind { Humanoid, Briefcase, Rift }

/// <summary>Faces a character can pull (smooth faces in battle, pixel faces on field sprites).</summary>
internal enum Expression { Neutral, Happy, Surprised, Sad, Angry }

/// <summary>Short animations played over the idle or the walk.</summary>
internal enum Emote { None, Wave, Surprised, Nod, Cheer, Throw }

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

    public Emote Emote;

    /// <summary>Seconds since the emote began.</summary>
    public float EmoteTime;

    public Expression Expression;
}

/// <summary>Bone numbers of every humanoid skeleton (L is the character's left, +X).</summary>
internal static class HumanBones
{
    public const int Root = 0, Hips = 1, Spine = 2, Head = 3;
    public const int UpperArmL = 4, ForeArmL = 5, HandL = 6, UpperArmR = 7, ForeArmR = 8, HandR = 9;
    public const int ThighL = 10, ShinL = 11, FootL = 12, ThighR = 13, ShinR = 14, FootR = 15;
    public const int Hair = 16, Bag = 17, Skirt = 18;
    public const int Count = 19;
}

/// <summary>
/// Where a humanoid's face is in the sculpted pose: the point on the surface between the eyes (field sprites
/// stamp their pixel faces there), the eyes' half spacing, and the square the battle face texture covers.
/// </summary>
internal readonly record struct FaceLayout(Vector3 Anchor, float EyeHalfSpacing, Vector3 TextureCenter, float TextureHalfSize);

/// <summary>
/// A character sculpted with the SDF kit (<see cref="SdfModel"/>), meshed once into a smooth skinned mesh, and
/// posed by its <see cref="Skeleton"/>. Model space: feet at the origin, facing +Z; <see cref="Scale"/> sizes it.
/// </summary>
internal sealed class CharacterRig
{
    public RigKind Kind;
    public string Type = "";
    public CharacterStyle Style = new();
    public float Scale = 1f;
    public readonly Skeleton Skeleton = new();
    public SdfMesh Mesh = new();

    /// <summary>The model's surface pushed out by the outline's thickness (an offset shell, so outlines never cut into creases).</summary>
    public SdfMesh Shell = new();

    /// <summary>Humanoids only: where the face is, and the patch of the head the battle face is drawn on.</summary>
    public FaceLayout Face;
    public SdfMesh? FacePatch;
    public Vector2[]? FaceUVs;

    // On the GPU (see CharacterModels.Upload)
    public SkinnedModel? Body;
    public SkinnedModel? Outline;
    public SkinnedModel? FaceModel;
    public Material[] FaceMaterials = Array.Empty<Material>();
    public bool Uploaded;

    // Scratch for posing: one pose at a time
    internal SkeletonPose Pose = null!;
    internal Matrix4x4[] Skin = Array.Empty<Matrix4x4>();

    internal void PrepareScratch()
    {
        Pose = new SkeletonPose(Skeleton.Count);
        Skin = new Matrix4x4[Skeleton.Count];
    }

    /// <summary>Poses the skeleton for <paramref name="pose"/>; the result is in <see cref="Skin"/>.</summary>
    public void Animate(CharacterPose pose)
    {
        CharacterAnimation.Apply(this, pose, Pose);
        Skeleton.Evaluate(Pose, Skin);
    }

    /// <summary>The face material for an expression, eyes open or shut.</summary>
    public Material FaceMaterial(Expression expression, bool blink) => FaceMaterials[(int)expression * 2 + (blink ? 1 : 0)];
}

/// <summary>
/// Builds and caches the characters, one per character type: sculpted with the SDF kit, meshed (cached on disk by
/// <see cref="SdfCache"/>) and skinned to a humanoid skeleton. The same rig is drawn in 3D in battle and baked
/// into the field's pixel sprites (<see cref="CharacterSprites"/>).
/// </summary>
internal static class CharacterModels
{
    /// <summary>Outline of the 3D characters in battle, in model units.</summary>
    public const float OutlineThickness = 0.009f;

    /// <summary>Mesh cell size: about a hundredth of a character's height.</summary>
    public const float MeshCell = 1f / 112f;

    private static readonly Dictionary<string, CharacterRig> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, Task<CharacterRig>> Building = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Starts sculpting and meshing these character types in the background (the first run meshes them; later
    /// runs read the mesh cache), so none holds up a frame when it first appears.
    /// </summary>
    public static void Preload(IEnumerable<string> npcTypes)
    {
        foreach (var type in npcTypes.Distinct(StringComparer.OrdinalIgnoreCase))
            Building.GetOrAdd(type, t => Task.Run(() => Build(t)));
    }

    public static RigKind KindOf(string npcType) => npcType.ToUpperInvariant() switch
    {
        "STARTERBRIEFCASE" => RigKind.Briefcase,
        "RIFT" => RigKind.Rift,
        _ => RigKind.Humanoid
    };

    /// <summary>The GPU-ready rig for a character type (built and uploaded on first use).</summary>
    public static CharacterRig Get(string npcType, FieldShaders shaders)
    {
        if (Cache.TryGetValue(npcType, out var rig) && rig.Uploaded) return rig;
        // Waits for a background build if one is under way
        rig ??= Building.TryGetValue(npcType, out var building) ? building.Result : Build(npcType);
        Upload(rig, shaders);
        Cache[npcType] = rig;
        return rig;
    }

    /// <summary>CPU-side model only (no GPU needed).</summary>
    public static CharacterRig Build(string npcType)
    {
        var kind = KindOf(npcType);
        var rig = new CharacterRig { Kind = kind, Type = npcType.ToUpperInvariant() };
        var model = new SdfModel("character " + rig.Type);
        switch (kind)
        {
            case RigKind.Briefcase: BuildBriefcase(rig, model); break;
            case RigKind.Rift: BuildRift(rig, model); break;
            default:
                rig.Style = CharacterStyle.For(npcType);
                BuildHumanoid(rig, model, rig.Style);
                break;
        }
        model.BoneCount = rig.Skeleton.Count;
        rig.Mesh = SdfCache.Get(model, MeshCell);
        if (kind == RigKind.Humanoid) { SoftenFace(rig); SoftenHair(rig); }
        rig.Shell = SdfCache.Get(model, MeshCell * 1.25f, OutlineThickness);
        if (kind == RigKind.Humanoid) ExtractFacePatch(rig);
        rig.PrepareScratch();
        return rig;
    }

    private static void Upload(CharacterRig rig, FieldShaders shaders)
    {
        rig.Body = SkinnedModel.Upload(rig.Mesh, rig.Skeleton.Count);
        rig.Outline = SkinnedModel.Upload(rig.Shell, rig.Skeleton.Count);
        if (rig.FacePatch != null && rig.FaceUVs != null && rig.FacePatch.VertexCount > 0)
        {
            rig.FaceModel = SkinnedModel.Upload(rig.FacePatch, rig.Skeleton.Count, rig.FaceUVs, _ => Color.White);
            rig.FaceMaterials = CharacterFaces.Materials(rig, shaders);
        }
        rig.Uploaded = true;
    }

    // ------------------------------------------------------------------ humanoids

    /// <summary>Joint positions and sizes of a body build (left side; the right mirrors it).</summary>
    private sealed record Frame(
        float HipY, float WaistY, float ChestY, float NeckY, Vector3 HeadC, float HeadR,
        Vector3 Shoulder, Vector3 Elbow, Vector3 Wrist, Vector3 LegTop, Vector3 Knee, Vector3 Ankle,
        float TorsoW, float TorsoD, float ArmR, float LegR, float EyeDrop, float EyeHalf);

    // Bound in an A-pose (arms a little out from the body), which keeps arms and body apart for clean weights
    private static readonly Frame Kid = new(
        HipY: 0.40f, WaistY: 0.50f, ChestY: 0.585f, NeckY: 0.655f, HeadC: new(0, 0.915f, 0.012f), HeadR: 0.27f,
        Shoulder: new(0.128f, 0.625f, 0), Elbow: new(0.19f, 0.505f, -0.006f), Wrist: new(0.23f, 0.395f, 0.008f),
        LegTop: new(0.068f, 0.395f, 0), Knee: new(0.071f, 0.222f, 0.008f), Ankle: new(0.072f, 0.07f, 0),
        TorsoW: 0.122f, TorsoD: 0.09f, ArmR: 0.047f, LegR: 0.061f, EyeDrop: 0.045f, EyeHalf: 0.092f);

    private static readonly Frame Adult = new(
        HipY: 0.47f, WaistY: 0.585f, ChestY: 0.69f, NeckY: 0.775f, HeadC: new(0, 1.02f, 0.012f), HeadR: 0.25f,
        Shoulder: new(0.142f, 0.742f, 0), Elbow: new(0.202f, 0.606f, -0.006f), Wrist: new(0.24f, 0.476f, 0.008f),
        LegTop: new(0.072f, 0.465f, 0), Knee: new(0.075f, 0.262f, 0.008f), Ankle: new(0.076f, 0.072f, 0),
        TorsoW: 0.13f, TorsoD: 0.095f, ArmR: 0.049f, LegR: 0.064f, EyeDrop: 0.035f, EyeHalf: 0.085f);

    private static Vector3 Side(Vector3 v, float side) => v with { X = v.X * side };

    private static Color Shade(Color c, float f) => MeshBuilder.Scale(c, f);

    /// <summary>Rotation that turns +Y onto <paramref name="dir"/>.</summary>
    private static Quaternion AlignY(Vector3 dir)
    {
        dir = Vector3.Normalize(dir);
        var axis = Vector3.Cross(Vector3.UnitY, dir);
        float angle = MathF.Acos(Math.Clamp(Vector3.Dot(Vector3.UnitY, dir), -1f, 1f));
        return axis.LengthSquared() < 1e-10f ? Quaternion.Identity : Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), angle);
    }

    private static Quaternion Pitch(float radians) => Quaternion.CreateFromAxisAngle(Vector3.UnitX, radians);

    private static void BuildHumanoid(CharacterRig rig, SdfModel m, CharacterStyle s)
    {
        var f = s.Build == BodyBuild.Adult ? Adult : Kid;
        rig.Scale = s.Height;
        m.WeightBlend = 0.04f;

        var sk = rig.Skeleton;
        sk.Add("root", -1, Vector3.Zero);
        sk.Add("hips", HumanBones.Root, new(0, f.HipY, 0));
        sk.Add("spine", HumanBones.Hips, new(0, f.WaistY, 0));
        sk.Add("head", HumanBones.Spine, new(0, f.NeckY, 0));
        foreach (var (side, name) in new[] { (1f, "L"), (-1f, "R") })
        {
            int upper = sk.Add("upperArm." + name, HumanBones.Spine, Side(f.Shoulder, side));
            int fore = sk.Add("foreArm." + name, upper, Side(f.Elbow, side));
            sk.Add("hand." + name, fore, Side(f.Wrist, side));
        }
        foreach (var (side, name) in new[] { (1f, "L"), (-1f, "R") })
        {
            int thigh = sk.Add("thigh." + name, HumanBones.Hips, Side(f.LegTop, side));
            int shin = sk.Add("shin." + name, thigh, Side(f.Knee, side));
            sk.Add("foot." + name, shin, Side(f.Ankle, side));
        }
        sk.Add("hair", HumanBones.Head, f.HeadC + new Vector3(0, 0.02f, -0.12f));
        sk.Add("bag", HumanBones.Spine, new(0, f.ChestY + 0.05f, -0.1f));
        sk.Add("skirt", HumanBones.Hips, new(0, f.HipY + 0.04f, 0));

        var skin = s.Skin;
        var hair = s.HairColor;
        var H = f.HeadC;
        float R = f.HeadR;
        const SurfaceMaterial Cloth = SurfaceMaterial.Cloth, Hair = SurfaceMaterial.Hair, Skin = SurfaceMaterial.Skin;

        // 1. The hair's base: a cap snug on the skull with the face carved out of it (a cut only carves what came
        // before it). The locks laid over it later give the hair its shape.
        m.Ellipsoid(H + new Vector3(0, 0.02f, -0.02f), new Vector3(R * 1.04f, R * 1.0f, R * 1.04f), hair, HumanBones.Head, 0f, Hair);
        if (s.Hair == HairCut.Swept)
            m.Ellipsoid(H + new Vector3(0, -R * 0.2f, R * 0.62f), new Vector3(R * 0.9f, R * 0.98f, R * 0.8f), hair, HumanBones.Head, 0.03f, op: SdfOp.Cut);
        else
            m.Ellipsoid(H + new Vector3(0, -R * 0.42f, R * 0.62f), new Vector3(R * 0.86f, R * 0.78f, R * 0.8f), hair, HumanBones.Head, 0.03f, op: SdfOp.Cut);
        // The hair ends in a clean line that slopes from above the ears down to the nape (long hair adds its own fall later)
        m.Box(H + new Vector3(0, -R * 1.25f, R * 0.15f), new Vector3(R * 1.6f, R * 0.62f, R * 1.6f), 0f, hair, HumanBones.Head, 0.02f,
            rotation: Pitch(-0.42f), op: SdfOp.Cut);

        // 2. Head: a round skull, fuller cheeks, small ears and a hint of a nose
        m.Ellipsoid(H, new Vector3(R, R * 0.95f, R * 0.95f), skin, HumanBones.Head, 0.02f, Skin);
        m.Ellipsoid(H + new Vector3(0, -R * 0.24f, R * 0.18f), new Vector3(R * 0.83f, R * 0.62f, R * 0.76f), skin, HumanBones.Head, 0.07f, Skin);
        foreach (float side in new[] { 1f, -1f })
            m.Ellipsoid(H + new Vector3(side * R * 0.97f, -R * 0.1f, -0.005f), new Vector3(0.03f, 0.048f, 0.034f), skin, HumanBones.Head, 0.02f, Skin);
        float eyeY = H.Y - f.EyeDrop;
        m.Ellipsoid(new Vector3(0, eyeY - 0.055f, H.Z + R * 0.93f), new Vector3(0.017f, 0.014f, 0.012f), skin, HumanBones.Head, 0.015f, Skin);

        // 3. Hair over the head: bangs, and the cut's own shapes
        BuildHair(m, s, f, eyeY);
        BuildHat(m, s, f);
        if (s.Mustache)
        {
            foreach (float side in new[] { 1f, -1f })
                m.Ellipsoid(new Vector3(side * 0.045f, eyeY - 0.088f, H.Z + R * 0.9f), new Vector3(0.05f, 0.022f, 0.026f), hair, HumanBones.Head, 0.012f, Hair,
                    Quaternion.CreateFromAxisAngle(Vector3.UnitZ, side * -0.28f));
        }
        if (s.BushyBrows)
        {
            foreach (float side in new[] { 1f, -1f })
                m.Ellipsoid(new Vector3(side * f.EyeHalf, eyeY + 0.07f, H.Z + R * 0.86f), new Vector3(0.05f, 0.017f, 0.024f), hair, HumanBones.Head, 0.01f, Hair,
                    Quaternion.CreateFromAxisAngle(Vector3.UnitZ, side * 0.12f));
        }

        // 4. Neck and body
        float neckBottom = f.ChestY + 0.02f, neckTop = f.NeckY + 0.07f;
        m.Capsule(new Vector3(0, neckBottom, -0.005f), new Vector3(0, neckTop, 0.004f), 0.044f, 0.04f, skin, HumanBones.Head, 0.03f, Skin);
        var top = s.Top;
        var bottom = s.Bottom;
        m.Ellipsoid(new Vector3(0, f.HipY + 0.005f, 0), new Vector3(f.TorsoW * 0.98f, 0.082f, f.TorsoD * 1.02f), s.Coat ? top : bottom, HumanBones.Hips, 0f, Cloth);
        m.Ellipsoid(new Vector3(0, f.WaistY, 0.004f), new Vector3(f.TorsoW, (f.ChestY - f.HipY) * 0.62f, f.TorsoD), top, HumanBones.Spine, 0.05f, Cloth);
        m.Ellipsoid(new Vector3(0, f.ChestY, 0), new Vector3(f.TorsoW * 1.08f, 0.075f, f.TorsoD), top, HumanBones.Spine, 0.05f, Cloth);

        // Collar or scarf
        m.Torus(new Vector3(0, f.ChestY + 0.055f, 0.004f), 0.07f, 0.028f, s.Accent, HumanBones.Spine, 0.012f, Cloth);
        if (s.Scarf)
        {
            // A scarf rather than a collar: it hangs down in front a little
            m.Capsule(new Vector3(0.03f, f.ChestY + 0.03f, f.TorsoD * 0.92f), new Vector3(0.045f, f.ChestY - 0.06f, f.TorsoD * 1.0f), 0.024f, 0.02f,
                s.Accent, HumanBones.Spine, 0.01f, Cloth);
        }

        if (s.Coat)
        {
            // A long coat over the body: the upper part moves with the chest, the tails swing (skirt bone)
            float hem = f.Knee.Y + 0.035f;
            m.Capsule(new Vector3(0, f.HipY + 0.03f, 0), new Vector3(0, hem + 0.02f, -0.006f), f.TorsoD * 1.05f, f.TorsoW * 1.3f, top, HumanBones.Skirt, 0.04f, Cloth);
            m.Box(new Vector3(0, hem - 0.3f, 0), new Vector3(0.5f, 0.3f, 0.5f), 0f, top, HumanBones.Skirt, 0f, op: SdfOp.Cut);
            // The shirt shows down the open front
            m.Paint(m.Box(new Vector3(0, f.ChestY - 0.02f, f.TorsoD + 0.05f), new Vector3(0.032f, 0.075f, 0.06f), 0.01f, s.Accent, HumanBones.Spine),
                false, HumanBones.Spine);
        }
        if (s.Skirt)
        {
            float hem = f.Knee.Y + 0.065f;
            m.Capsule(new Vector3(0, f.HipY - 0.01f, 0), new Vector3(0, hem + 0.015f, -0.004f), f.TorsoD * 0.88f, f.TorsoW * 1.55f, bottom, HumanBones.Skirt, 0.03f, Cloth);
            m.Box(new Vector3(0, hem - 0.3f, 0), new Vector3(0.5f, 0.3f, 0.5f), 0f, bottom, HumanBones.Skirt, 0f, op: SdfOp.Cut);
        }
        if (s.Bag is Color bag)
        {
            float bz = -f.TorsoD - 0.045f;
            m.Box(new Vector3(0, f.ChestY - 0.035f, bz), new Vector3(0.1f, 0.1f, 0.055f), 0.045f, bag, HumanBones.Bag, 0.015f, SurfaceMaterial.Leather);
            m.Box(new Vector3(0, f.ChestY + 0.035f, bz - 0.012f), new Vector3(0.096f, 0.04f, 0.055f), 0.03f, Shade(bag, 0.86f), HumanBones.Bag, 0.006f, SurfaceMaterial.Leather);
            m.Box(new Vector3(0, f.ChestY + 0.0f, bz - 0.066f), new Vector3(0.022f, 0.016f, 0.008f), 0.006f, new Color(236, 230, 214, 255), HumanBones.Bag, 0f, SurfaceMaterial.Metal);
            foreach (float side in new[] { 1f, -1f })
            {
                // Straps over the shoulders and down the front
                var back = new Vector3(side * 0.07f, f.ChestY + 0.05f, -f.TorsoD * 0.85f);
                var over = new Vector3(side * 0.078f, f.ChestY + 0.085f, 0.0f);
                var front = new Vector3(side * 0.082f, f.ChestY - 0.075f, f.TorsoD * 0.98f);
                m.Capsule(back, over, 0.017f, 0.017f, Shade(bag, 0.78f), HumanBones.Spine, 0.004f, SurfaceMaterial.Leather);
                m.Capsule(over, front, 0.017f, 0.016f, Shade(bag, 0.78f), HumanBones.Spine, 0.004f, SurfaceMaterial.Leather);
            }
        }

        // 5. Arms and hands
        foreach (var (side, upperB, foreB, handB) in new[] { (1f, HumanBones.UpperArmL, HumanBones.ForeArmL, HumanBones.HandL), (-1f, HumanBones.UpperArmR, HumanBones.ForeArmR, HumanBones.HandR) })
        {
            var sh = Side(f.Shoulder, side);
            var el = Side(f.Elbow, side);
            var wr = Side(f.Wrist, side);
            var sleeve = top;
            m.Capsule(sh, el, f.ArmR, f.ArmR * 0.9f, sleeve, upperB, 0.035f, Cloth);
            m.Capsule(el, wr, f.ArmR * 0.9f, f.ArmR * 0.76f, s.ShortSleeves ? skin : sleeve, foreB, 0.015f, s.ShortSleeves ? Skin : Cloth);
            var along = Vector3.Normalize(wr - el);
            var palm = wr + along * 0.044f;
            m.Ellipsoid(palm, new Vector3(0.036f, 0.046f, 0.029f), skin, handB, 0.016f, Skin, AlignY(along));
            m.Capsule(wr + along * 0.018f + new Vector3(-side * 0.012f, 0, 0.02f), wr + along * 0.045f + new Vector3(-side * 0.016f, 0, 0.038f),
                0.014f, 0.012f, skin, handB, 0.01f, Skin);
            if (s.ShortSleeves)
            {
                // The sleeve ends two thirds of the way down the upper arm
                var hemPoint = Vector3.Lerp(sh, el, 0.68f);
                m.Paint(m.Capsule(hemPoint, wr + along * 0.1f, 0.075f, 0.075f, skin, upperB), true, upperB, foreB).Material = Skin;
            }
            else
            {
                // A cuff where the sleeve meets the hand
                m.Torus(wr - along * 0.006f, f.ArmR * 0.72f, 0.011f, Shade(sleeve, 1.08f), foreB, 0.008f, Cloth, AlignY(along));
            }
        }

        // 6. Legs and shoes (hard joins under a skirt or coat, so the legs don't pull the hem)
        bool covered = s.Skirt || s.Coat;
        var sole = s.Sole ?? Shade(s.Shoes, 0.62f);
        foreach (var (side, thighB, shinB, footB) in new[] { (1f, HumanBones.ThighL, HumanBones.ShinL, HumanBones.FootL), (-1f, HumanBones.ThighR, HumanBones.ShinR, HumanBones.FootR) })
        {
            var hip = Side(f.LegTop, side);
            var knee = Side(f.Knee, side);
            var ankle = Side(f.Ankle, side);
            bool bare = s.Skirt || s.Shorts;
            m.Capsule(hip + new Vector3(0, 0.03f, 0), knee, f.LegR, f.LegR * 0.84f, s.Skirt ? skin : bottom, thighB, covered ? 0f : 0.04f, s.Skirt ? Skin : Cloth);
            m.Capsule(knee, ankle + new Vector3(0, 0.01f, 0), f.LegR * 0.84f, f.LegR * 0.7f, bare ? skin : bottom, shinB, 0.02f, bare ? Skin : Cloth);
            if (s.Shorts)
                m.Paint(m.Capsule(Vector3.Lerp(hip, knee, 0.72f), ankle, 0.09f, 0.09f, skin, thighB), true, thighB, shinB).Material = Skin;

            var shoe = new Vector3(ankle.X, 0.044f, ankle.Z + 0.03f);
            m.Box(shoe, new Vector3(0.05f, 0.044f, 0.086f), 0.04f, s.Shoes, footB, 0.02f, SurfaceMaterial.Leather);
            m.Paint(m.Box(new Vector3(shoe.X, 0.008f, shoe.Z), new Vector3(0.07f, 0.018f, 0.12f), 0f, sole, footB), true, footB).Material = SurfaceMaterial.Plastic;
        }

        // 7. Colour: a clean waistline, stripes across the shirt, rosy cheeks
        if (!s.Coat)
            m.Paint(m.Box(new Vector3(0, f.HipY - 0.1f, 0), new Vector3(0.3f, (f.WaistY - f.HipY) * 0.42f + 0.1f, 0.3f), 0f, bottom, HumanBones.Hips),
                false, HumanBones.Hips, HumanBones.Spine, HumanBones.Skirt);
        if (s.Stripes)
        {
            // Light stripes on a coloured shirt, coloured ones on a white shirt
            var stripe = s.Top.R + s.Top.G + s.Top.B > 690 ? s.Accent : PixelCanvas.Light1(s.Top, 0.55f);
            foreach (float y in new[] { f.ChestY - 0.045f, f.WaistY - 0.025f })
                m.Paint(m.Box(new Vector3(0, y, 0), new Vector3(0.3f, 0.013f, 0.3f), 0f, stripe, HumanBones.Spine), false, HumanBones.Spine);
        }
        if (s.Blush)
        {
            var blush = PixelCanvas.Mix(skin, new Color(250, 120, 130, 255), 0.32f);
            foreach (float side in new[] { 1f, -1f })
                m.Paint(m.Ellipsoid(new Vector3(side * (f.EyeHalf + 0.035f), eyeY - 0.065f, H.Z + R * 0.75f), new Vector3(0.042f, 0.024f, 0.08f), blush, HumanBones.Head),
                    false, HumanBones.Head);
        }

        // The face: the surface point between the eyes, found by walking in from the front
        var anchor = SurfaceAlong(m, new Vector3(0, eyeY, H.Z + 1f), -Vector3.UnitZ);
        var center = new Vector3(0, eyeY - 0.03f, anchor.Z);
        rig.Face = new FaceLayout(anchor, f.EyeHalf, center, R * 0.82f);
    }

    /// <summary>
    /// Sculpted hair: locks laid over the head, each a tapering tube that follows the skull along a great circle
    /// from where it grows (the crown, or the hairline for swept hair) to its tip. Small blends leave grooves
    /// between them, so the hair reads as locks and its sheen breaks along them. Directions are seen from the
    /// head's centre (x to the character's left, y up, z forward).
    /// </summary>
    private static void BuildHair(SdfModel m, CharacterStyle s, Frame f, float eyeY)
    {
        var hair = s.HairColor;
        var H = f.HeadC + new Vector3(0, 0.02f, -0.015f);
        float R = f.HeadR;
        const SurfaceMaterial Hair = SurfaceMaterial.Hair;
        var crown = new Vector3(0f, 0.86f, -0.5f);

        // Under a beret or a cap only the hair below its edge shows, so locks grow from there instead of the crown
        bool hatted = s.Hat is Headwear.Beret or Headwear.Cap;
        Vector3 Root(Vector3 to) => hatted ? Vector3.Normalize(new Vector3(to.X * 0.9f, 0.42f, to.Z == 0f ? -0.9f : MathF.Sign(to.Z) * 0.86f)) : crown;

        void Lock(Vector3 from, Vector3 to, float r0, float r1, float lift = 0.012f, int bone = HumanBones.Head)
        {
            var a = Vector3.Normalize(from);
            var b = Vector3.Normalize(to);
            float omega = MathF.Acos(Math.Clamp(Vector3.Dot(a, b), -1f, 1f));
            Vector3 Dir(float t) => omega < 1e-3f ? a : (MathF.Sin((1f - t) * omega) * a + MathF.Sin(t * omega) * b) / MathF.Sin(omega);
            const int Segments = 6;
            for (int i = 0; i < Segments; i++)
            {
                float t0 = i / (float)Segments, t1 = (i + 1) / (float)Segments;
                // The tip lifts off the skull a little, so each lock ends in a point that stands proud
                var p0 = H + Dir(t0) * (R * 0.98f + lift * t0 * t0);
                var p1 = H + Dir(t1) * (R * 0.98f + lift * t1 * t1);
                m.Capsule(p0, p1, r0 + (r1 - r0) * t0, r0 + (r1 - r0) * t1, hair, bone, 0.012f, Hair);
            }
        }

        // Bangs over the forehead, pointed, ending just above the eyes (swept hair leaves the forehead bare)
        if (s.Hair is HairCut.Short or HairCut.Long or HairCut.Spiky)
        {
            float fringe = (eyeY + 0.03f - H.Y) / R;
            foreach (float x in new[] { -0.5f, -0.17f, 0.17f, 0.5f })
            {
                var tip = new Vector3(x, fringe + MathF.Abs(x) * 0.25f, 0.92f);
                Lock(hatted ? new Vector3(x * 0.95f, 0.5f, 0.86f) : crown, tip, 0.075f, 0.026f, 0.03f);
            }
        }

        switch (s.Hair)
        {
            case HairCut.Spiky:
                // Tufts that stand up and sweep back, long enough to stand out of the outline at sprite size
                (Vector3 Dir, float Len)[] spikes =
                {
                    (new(0f, 1f, 0.15f), 0.25f), (new(0.55f, 0.8f, 0.05f), 0.23f), (new(-0.55f, 0.8f, 0.05f), 0.23f),
                    (new(0.35f, 0.7f, -0.6f), 0.23f), (new(-0.35f, 0.7f, -0.6f), 0.23f), (new(0f, 0.45f, -0.9f), 0.22f),
                    (new(0.85f, 0.35f, -0.25f), 0.18f), (new(-0.85f, 0.35f, -0.25f), 0.18f), (new(0.3f, 0.75f, 0.55f), 0.19f),
                    (new(-0.3f, 0.75f, 0.55f), 0.19f)
                };
                foreach (var (d, len) in spikes)
                {
                    var dir = Vector3.Normalize(d);
                    var root = H + dir * R * 0.82f;
                    m.Capsule(root, root + dir * len, 0.075f, 0.012f, hair, HumanBones.Head, 0.03f, Hair);
                }
                foreach (float side in new[] { 1f, -1f })
                    Lock(crown, new Vector3(side * 0.95f, -0.05f, 0.3f), 0.07f, 0.03f, 0.02f);
                break;

            case HairCut.Swept:
                // Combed straight back from the hairline over the crown, and back over the ears at the sides
                foreach (float x in new[] { -0.42f, -0.14f, 0.14f, 0.42f })
                    Lock(new Vector3(x, 0.62f, 0.78f), new Vector3(x * 0.85f, 0.05f, -1f), 0.075f, 0.05f, 0.02f);
                Lock(new Vector3(0f, 0.7f, 0.7f), new Vector3(0f, 0.1f, -1f), 0.08f, 0.055f, 0.02f);
                foreach (float side in new[] { 1f, -1f })
                {
                    Lock(new Vector3(side * 0.8f, 0.42f, 0.45f), new Vector3(side * 0.62f, -0.25f, -0.75f), 0.065f, 0.045f, 0.01f);
                    Lock(new Vector3(side * 0.92f, 0.05f, 0.35f), new Vector3(side * 0.7f, -0.45f, -0.55f), 0.055f, 0.04f, 0.0f);
                }
                break;

            default:
                // Short or long: locks from the crown over the top, the sides and the back
                if (!hatted)
                    foreach (var to in new[] { new Vector3(0f, 0.72f, 0.7f), new Vector3(0.62f, 0.62f, 0.45f), new Vector3(-0.62f, 0.62f, 0.45f) })
                        Lock(crown, to, 0.08f, 0.05f, 0.02f);
                foreach (float side in new[] { 1f, -1f })
                {
                    foreach (var to in new[] { new Vector3(side * 0.95f, 0.02f, 0.35f), new Vector3(side * 0.9f, -0.3f, -0.25f), new Vector3(side * 0.5f, -0.55f, -0.7f) })
                        Lock(Root(to), to, 0.075f, 0.04f, 0.02f);
                }
                Lock(Root(new Vector3(0f, -0.6f, -0.85f)), new Vector3(0f, -0.6f, -0.85f), 0.085f, 0.05f, 0.02f);
                // Under a hat the locks fan out from its edge and would leave gaps at the back for the hat's band to show through
                if (hatted)
                    foreach (float side in new[] { 1f, -1f })
                        Lock(Root(new Vector3(side * 0.25f, -0.6f, -0.8f)), new Vector3(side * 0.25f, -0.6f, -0.8f), 0.08f, 0.045f, 0.02f);

                if (s.Hair == HairCut.Long)
                {
                    // A smooth mass behind, under the locks
                    m.Ellipsoid(H + new Vector3(0, -R * 0.75f, -R * 0.5f), new Vector3(R * 0.98f, R * 1.0f, R * 0.5f), hair, HumanBones.Hair, 0.05f, Hair);
                    // Hair falls down the back to the shoulders in locks that swing, with a lock framing each cheek
                    foreach (float x in new[] { -0.55f, -0.18f, 0.18f, 0.55f })
                    {
                        var top = H + new Vector3(x * R, -R * 0.2f, -R * 0.82f + MathF.Abs(x) * R * 0.25f);
                        var tip = H + new Vector3(x * R * 1.12f, -R * 1.65f, -R * 0.6f + MathF.Abs(x) * R * 0.2f);
                        m.Capsule(top, tip, 0.1f, 0.05f, hair, HumanBones.Hair, 0.025f, Hair);
                    }
                    foreach (float side in new[] { 1f, -1f })
                        m.Capsule(H + new Vector3(side * R * 0.9f, -R * 0.05f, R * 0.18f), H + new Vector3(side * R * 0.92f, -R * 1.05f, R * 0.12f),
                            0.065f, 0.035f, hair, HumanBones.Head, 0.025f, Hair);
                }
                break;
        }
    }

    private static void BuildHat(SdfModel m, CharacterStyle s, Frame f)
    {
        var H = f.HeadC;
        float R = f.HeadR;
        switch (s.Hat)
        {
            case Headwear.Beret:
            {
                // A soft, flat beret worn toward the back, with a white band and a little stalk on top
                // It covers the crown of the hair entirely, so none shows through its top
                var at = H + new Vector3(0, R * 0.84f, -0.035f);
                var tilt = Pitch(-0.2f);
                m.Ellipsoid(at, new Vector3(R * 1.14f, R * 0.44f, R * 1.08f), s.HatColor, HumanBones.Head, 0f, SurfaceMaterial.Cloth, tilt);
                m.Torus(at + new Vector3(0, -R * 0.24f, 0.012f), R * 1.0f, 0.027f, s.HatBand, HumanBones.Head, 0f, SurfaceMaterial.Cloth, tilt);
                m.Capsule(at + new Vector3(0, R * 0.38f, -0.012f), at + new Vector3(0, R * 0.52f, -0.03f), 0.022f, 0.016f, Shade(s.HatColor, 0.86f), HumanBones.Head, 0.012f, SurfaceMaterial.Cloth);
                break;
            }
            case Headwear.Cap:
            {
                var at = H + new Vector3(0, R * 0.62f, -0.015f);
                m.Cylinder(at, R * 1.08f, R * 0.6f, R * 0.5f, s.HatColor, HumanBones.Head, 0f, SurfaceMaterial.Cloth, Pitch(-0.12f));
                // The peak comes out of the crown's front edge, over the bangs
                m.Ellipsoid(H + new Vector3(0, R * 0.6f, R * 1.0f), new Vector3(R * 0.8f, 0.024f, R * 0.62f), s.HatBand, HumanBones.Head, 0f, SurfaceMaterial.Plastic, Pitch(0.2f));
                m.Sphere(at + new Vector3(0, R * 0.62f, -0.03f), 0.02f, Shade(s.HatColor, 0.88f), HumanBones.Head, 0.01f, SurfaceMaterial.Cloth);
                break;
            }
            case Headwear.NurseCap:
            {
                // A starched band standing on the hair at the top of the head (the hair is built first and stands
                // about 0.3 off the head's centre there, so anything closer is buried), with a red cross on its front
                var at = H + new Vector3(0, R * 1.2f, R * 0.5f);
                var tilt = Pitch(-0.3f);
                m.Box(at, new Vector3(R * 0.5f, 0.07f, 0.03f), 0.028f, s.HatColor, HumanBones.Head, 0.012f, SurfaceMaterial.Cloth, tilt);
                var front = at + Vector3.Transform(new Vector3(0, 0.01f, 0.03f), tilt);
                m.Paint(m.Box(front, new Vector3(0.013f, 0.036f, 0.02f), 0f, s.HatBand, HumanBones.Head, rotation: tilt));
                m.Paint(m.Box(front, new Vector3(0.036f, 0.013f, 0.02f), 0f, s.HatBand, HumanBones.Head, rotation: tilt));
                break;
            }
        }
    }

    /// <summary>
    /// Bends the normals on the front of the head toward the face's forward direction (a little upward), the way
    /// cel-shaded characters are usually made: the face lights evenly instead of being split by the shade's edge.
    /// </summary>
    private static void SoftenFace(CharacterRig rig)
    {
        var mesh = rig.Mesh;
        var forward = Vector3.Normalize(new Vector3(0, 0.75f, 0.66f));
        var head = rig.Skeleton[HumanBones.Head].Joint;
        var skin = rig.Style.Skin;
        for (int v = 0; v < mesh.VertexCount; v++)
        {
            if (mesh.WeightOf(v, HumanBones.Head) < 0.9f || mesh.Positions[v].Y < head.Y + 0.04f) continue;
            var c = mesh.Colors[v];
            int dr = c.R - skin.R, dg = c.G - skin.G, db = c.B - skin.B;
            if (dr * dr + dg * dg + db * db > 70 * 70) continue;
            var n = mesh.Normals[v];
            float t = Math.Clamp(n.Z * 1.6f + 0.2f, 0f, 1f) * 0.85f;
            mesh.Normals[v] = Vector3.Normalize(Vector3.Lerp(n, forward, t));
        }
    }

    /// <summary>
    /// Bends the hair's normals toward those of the smooth mass it fills (a capsule from the middle of the head
    /// down the fall of the hair), as cel-shaded hair is usually made: one clean shade edge and sheen across the
    /// head instead of a blotch on every lock. The locks keep their shape in the silhouette and the outline.
    /// </summary>
    private static void SoftenHair(CharacterRig rig)
    {
        var mesh = rig.Mesh;
        var f = rig.Style.Build == BodyBuild.Adult ? Adult : Kid;
        var top = f.HeadC + new Vector3(0, 0.02f, -0.015f);
        float lowest = top.Y;
        for (int v = 0; v < mesh.VertexCount; v++)
            if (mesh.Materials[v] == (byte)SurfaceMaterial.Hair) lowest = Math.Min(lowest, mesh.Positions[v].Y);
        // Short hair ends round the head, which leaves a sphere; long hair hangs behind, which leaves a capsule
        float drop = Math.Max(0f, top.Y - lowest - f.HeadR * 0.6f);
        var axis = new Vector3(0, -drop, -drop * 0.25f);
        float length2 = axis.LengthSquared();
        for (int v = 0; v < mesh.VertexCount; v++)
        {
            if (mesh.Materials[v] != (byte)SurfaceMaterial.Hair) continue;
            var p = mesh.Positions[v];
            float t = length2 < 1e-6f ? 0f : Math.Clamp(Vector3.Dot(p - top, axis) / length2, 0f, 1f);
            var away = p - (top + axis * t);
            if (away.LengthSquared() < 1e-8f) continue;
            mesh.Normals[v] = Vector3.Normalize(Vector3.Lerp(mesh.Normals[v], Vector3.Normalize(away), 0.7f));
        }
    }

    /// <summary>Walks from <paramref name="from"/> along <paramref name="dir"/> to the model's surface (sphere tracing).</summary>
    internal static Vector3 SurfaceAlong(SdfModel model, Vector3 from, Vector3 dir)
    {
        var p = from;
        for (int i = 0; i < 256; i++)
        {
            float d = model.Distance(p);
            if (d < 0.0005f) break;
            p += dir * Math.Max(d * 0.9f, 0.0005f);
        }
        return p;
    }

    /// <summary>
    /// The front of the head, where the battle face is drawn: skin triangles moving only with the head, inside
    /// the face square, copied a hair's breadth out from the head and mapped onto the face texture.
    /// </summary>
    private static void ExtractFacePatch(CharacterRig rig)
    {
        var mesh = rig.Mesh;
        var face = rig.Face;
        var skin = rig.Style.Skin;
        float half = face.TextureHalfSize;
        var c = face.TextureCenter;

        bool Usable(int v)
        {
            var p = mesh.Positions[v];
            if (mesh.WeightOf(v, HumanBones.Head) < 0.97f) return false;
            if (MathF.Abs(p.X - c.X) > half || MathF.Abs(p.Y - c.Y) > half || p.Z < c.Z - 0.2f) return false;
            if (mesh.Normals[v].Z < 0.2f) return false;
            var col = mesh.Colors[v];
            // Skin, or the blush painted on it; never hair
            int dr = col.R - skin.R, dg = col.G - skin.G, db = col.B - skin.B;
            return dr * dr + dg * dg + db * db < 70 * 70;
        }

        var map = new Dictionary<int, int>();
        var indices = new List<int>();
        int Map(int v)
        {
            if (!map.TryGetValue(v, out int i))
            {
                i = map.Count;
                map[v] = i;
            }
            return i;
        }
        for (int t = 0; t < mesh.Indices.Length; t += 3)
        {
            int a = mesh.Indices[t], b = mesh.Indices[t + 1], d = mesh.Indices[t + 2];
            if (!Usable(a) || !Usable(b) || !Usable(d)) continue;
            indices.Add(Map(a));
            indices.Add(Map(b));
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
            var nrm = mesh.Normals[src];
            patch.Positions[dst] = mesh.Positions[src] + nrm * 0.0025f;
            patch.Normals[dst] = nrm;
            patch.Colors[dst] = Color.White;
            patch.Materials[dst] = (byte)SurfaceMaterial.Skin;
            patch.BoneIndices[dst * 4] = HumanBones.Head;
            patch.BoneWeights[dst * 4] = 1f;
            var p = mesh.Positions[src];
            uvs[dst] = new Vector2(0.5f + (p.X - c.X) / (2f * half), 0.5f - (p.Y - c.Y) / (2f * half));
        }
        rig.FacePatch = patch;
        rig.FaceUVs = uvs;
    }

    // ------------------------------------------------------------------ props

    private static void BuildBriefcase(CharacterRig rig, SdfModel m)
    {
        rig.Scale = 1f;
        rig.Skeleton.Add("root", -1, Vector3.Zero);
        var leather = new Color(170, 112, 64, 255);
        var gold = new Color(250, 210, 80, 255);
        m.Box(new Vector3(0, 0.18f, 0), new Vector3(0.27f, 0.16f, 0.1f), 0.035f, leather, 0, 0f, SurfaceMaterial.Leather);
        m.Torus(new Vector3(0, 0.375f, 0), 0.07f, 0.02f, Shade(leather, 0.75f), 0, 0.012f, SurfaceMaterial.Leather, Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2f));
        foreach (float x in new[] { -0.14f, 0.14f })
            m.Box(new Vector3(x, 0.235f, 0.1f), new Vector3(0.03f, 0.026f, 0.012f), 0.008f, gold, 0, 0.004f, SurfaceMaterial.Metal);
        // The lid's seam and a darker base
        m.Paint(m.Box(new Vector3(0, 0.215f, 0), new Vector3(0.4f, 0.008f, 0.2f), 0f, Shade(leather, 0.68f), 0));
        m.Paint(m.Box(new Vector3(0, 0.0f, 0), new Vector3(0.4f, 0.03f, 0.2f), 0f, Shade(leather, 0.8f), 0));
    }

    private static void BuildRift(CharacterRig rig, SdfModel m)
    {
        // A tear in the world: a dark core glowing red at its heart, ringed by two slowly turning bands of light
        rig.Scale = 1f;
        int root = rig.Skeleton.Add("root", -1, Vector3.Zero);
        int core = rig.Skeleton.Add("core", root, new Vector3(0, 0.9f, 0));
        int ringA = rig.Skeleton.Add("ringA", core, new Vector3(0, 0.9f, 0));
        int ringB = rig.Skeleton.Add("ringB", core, new Vector3(0, 0.9f, 0));
        var c = new Vector3(0, 0.9f, 0);
        m.Ellipsoid(c, new Vector3(0.3f, 0.46f, 0.3f), new Color(30, 12, 44, 255), core, 0f, SurfaceMaterial.Default);
        m.Ellipsoid(c + new Vector3(0, 0.12f, 0), new Vector3(0.22f, 0.3f, 0.22f), new Color(96, 36, 132, 255), core, 0.12f, SurfaceMaterial.Default);
        m.Paint(m.Ellipsoid(c + new Vector3(0, 0, 0.2f), new Vector3(0.14f, 0.24f, 0.2f), new Color(220, 56, 90, 255), core), true).Material = SurfaceMaterial.Glow;
        m.Torus(c, 0.46f, 0.035f, new Color(210, 60, 90, 255), ringA, 0f, SurfaceMaterial.Glow, Quaternion.CreateFromAxisAngle(Vector3.UnitX, 0.9f));
        m.Torus(c, 0.56f, 0.026f, new Color(150, 90, 210, 255), ringB, 0f, SurfaceMaterial.Glow, Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 1.1f));
    }
}
