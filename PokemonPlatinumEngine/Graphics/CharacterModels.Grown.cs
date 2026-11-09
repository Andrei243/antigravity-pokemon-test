using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// A grown body's frame (plan 30 · FM4; style guide, "Grown characters"): its joints and sizes in model units before
/// the look's height scales them, the left side (the right mirrors it). <see cref="CharacterModels.GrownFrameOf"/> works
/// it out from the style guide's table.
/// </summary>
/// <param name="Crown">The top of the skull (the hair lies over it).</param>
/// <param name="HeadC">The head's middle, at the eyes.</param>
/// <param name="HeadR">Half the head's height, from the chin to the crown.</param>
/// <param name="NeckFootY">The neck's foot, between the collarbones.</param>
/// <param name="ChestMidY">The rib cage's middle.</param>
/// <param name="ChestJointY">Where the chest's bone turns on the spine's, at the bottom of the rib cage.</param>
/// <param name="WaistY">The waist, where the spine's bone turns on the hips'.</param>
/// <param name="HipY">The hip joints, where the thighs turn.</param>
/// <param name="ChestW">The rib cage's half width; the rest of the widths and depths are halves too.</param>
/// <param name="Girth">How thick the limbs and the neck are beside a grown adult's.</param>
/// <param name="Forward">How far forward of the body the head is carried (the elder's).</param>
internal sealed record GrownFrame(
    BodyBuild Build, bool Curved,
    float Crown, Vector3 HeadC, float HeadR, float Chin,
    float NeckFootY, float ShoulderY, float ChestMidY, float ChestJointY, float WaistY, float HipY,
    Vector3 Shoulder, Vector3 Elbow, Vector3 Wrist, float HandLength,
    Vector3 LegTop, Vector3 Knee, Vector3 Ankle, float FootLength,
    float ChestW, float ChestD, float WaistW, float WaistD, float PelvisW, float PelvisD,
    float Girth, float Forward)
{
    /// <summary>The body's size beside a grown adult's (its crown over the adult's): what the frame doesn't give scales by it.</summary>
    public float Size => Crown / 1.585f;

    /// <summary>How far out the thighs reach at the hip joints (their tops' outer side), which a garment over the hips covers.</summary>
    public float ThighOuter => LegTop.X + 0.078f * Girth;

    /// <summary>The ball of the left foot, where its toes turn (three quarters of the foot from the heel).</summary>
    public Vector3 Ball => new(Ankle.X, 0.025f * Size, Ankle.Z + 0.54f * FootLength);

    /// <summary>How far the arms are sculpted out from the body (the A-pose), in radians.</summary>
    public const float ArmsOut = 20f * MathF.PI / 180f;
}

internal static partial class CharacterModels
{
    /// <summary>
    /// The grown kit's mesh cell (plan 30 · FM4): a grown limb is a third as thick as a chibi's, so its cells are finer.
    /// Not the plan's 1/160, which put the broad build's body at 64,000 vertices, at the edge of what a mesh can index
    /// (<see cref="SkinnedModel"/>'s 65,535) before part b's locks of hair go on it.
    /// </summary>
    public const float GrownMeshCell = 1f / 144f;

    /// <summary>How far out a grown body's outline shell stands, in model units.</summary>
    public const float GrownOutlineThickness = 0.008f;

    /// <summary>Over how far a grown body's bone weights blend round a joint.</summary>
    public const float GrownWeightBlend = 0.03f;

    /// <summary>
    /// How far a grown shoulder's weight reaches into the chest round it (plan 30 · FM10; style guide, "Grown characters",
    /// "Meshing"): over the model's blend alone the armpit's edges stretched past four times their length wherever an arm
    /// rose to the shoulder's height (FM5's wave, cheer and throw).
    /// </summary>
    public const float GrownShoulderReach = 0.09f;

    /// <summary>The same reach for a sleeve's shoulder into the garment's chest, which lies further out and farther round.</summary>
    public const float GrownSleeveReach = 0.2f;

    /// <summary>
    /// The frame of a grown build (style guide, "Grown characters"): the table's crown, head, hip joints, shoulders and
    /// girth, and everything else worked out from them by the guide's rules. A curved figure has a waist, wider hips and
    /// a bust (never a child's).
    /// </summary>
    internal static GrownFrame GrownFrameOf(BodyBuild build, bool curved = false)
    {
        var (crown, headR, headY, hipY, across, girth) = build switch
        {
            BodyBuild.Teen => (1.485f, 0.105f, 1.38f, 0.76f, 0.30f, 0.88f),
            BodyBuild.GrownBroad => (1.64f, 0.11f, 1.53f, 0.83f, 0.44f, 1.22f),
            BodyBuild.Child => (1.14f, 0.115f, 1.025f, 0.53f, 0.24f, 0.82f),
            BodyBuild.GrownElder => (1.465f, 0.105f, 1.36f, 0.76f, 0.32f, 0.92f),
            BodyBuild.Grown => (1.585f, 0.105f, 1.48f, 0.82f, 0.36f, 1f),
            _ => throw new ArgumentException($"{build} is no build of the grown kit", nameof(build))
        };
        curved &= build != BodyBuild.Child;
        float size = crown / 1.585f;
        float forward = build == BodyBuild.GrownElder ? 0.04f : 0f;
        float chin = headY - headR;
        float shoulderY = chin - 0.9f * headR;
        float torso = shoulderY - hipY;
        float upperArm = 0.65f * torso, forearm = 0.51f * torso;
        float half = across / 2f;

        // The A-pose: the arms out from the body in its own plane, the forearm a little forward, the hands turned in
        var shoulder = new Vector3(half, shoulderY, -0.005f + forward * 0.25f);
        var elbow = shoulder + Vector3.Normalize(new Vector3(MathF.Sin(GrownFrame.ArmsOut), -MathF.Cos(GrownFrame.ArmsOut), -0.03f)) * upperArm;
        var wrist = elbow + Vector3.Normalize(new Vector3(MathF.Sin(GrownFrame.ArmsOut + 0.03f), -MathF.Cos(GrownFrame.ArmsOut + 0.03f), 0.14f)) * forearm;

        float chestW = 0.78f * half, chestD = 0.66f * chestW;
        float waistW = (curved ? 0.76f : build == BodyBuild.Child ? 0.94f : 0.86f) * chestW, waistD = 0.80f * waistW;
        float pelvisW = (curved ? 1.06f : 0.92f) * chestW, pelvisD = 0.66f * pelvisW;

        // The legs down from the hip joints, the feet a little apart (a curved figure's knees a little closer): never so
        // close that the outline's shell, a little out from the body, takes weights from the other leg across the gap
        // between the thighs or the knees (it stretched into a fin in a stride: the child's thick legs set it apart)
        float hipX = Math.Max(0.66f * pelvisW, 0.056f * girth + 0.022f), ankleY = 0.045f * crown;
        var legTop = new Vector3(hipX, hipY, 0f);
        var knee = new Vector3(hipX * (curved ? 0.95f : 1.05f), (ankleY + hipY) / 2f, 0.01f * size);
        var ankle = new Vector3(hipX * (curved ? 1.08f : 1.12f), ankleY, -0.005f * size);

        return new GrownFrame(build, curved, crown, new Vector3(0, headY, 0.005f + forward), headR, chin,
            shoulderY + 0.02f * size, shoulderY, shoulderY - 0.30f * torso, shoulderY - 0.45f * torso, shoulderY - 0.70f * torso, hipY,
            shoulder, elbow, wrist, 0.58f * upperArm,
            legTop, knee, ankle, 0.152f * crown,
            chestW, chestD, waistW, waistD, pelvisW, pelvisD, girth, forward);
    }

    /// <summary>
    /// A look of the grown kit (plan 30 · FM4): its skeleton, body, hair, hat and what it holds sculpted and meshed in the
    /// grown kit's cells with its outline shell, its clothes a layer of their own meshed apart with theirs (part b), and
    /// its painted face's patch cut from the front of the head.
    /// </summary>
    private static CharacterRig BuildGrownRig(CharacterRig rig, SdfModel model)
    {
        var s = rig.Style;
        var f = GrownFrameOf(s.Build, s.CurvedFigure());
        rig.Scale = s.Height;
        var wear = new SdfModel("clothes " + rig.Type) { WeightBlend = GrownWeightBlend };
        var held = new SdfModel("held " + rig.Type) { WeightBlend = GrownWeightBlend };
        BuildGrown(rig, model, wear, s, f);
        GrownHeld(held, s, f);
        model.BoneCount = rig.Skeleton.Count;
        rig.Mesh = SdfCache.Get(model, GrownMeshCell);
        SoftenGrownFace(rig, f);
        SoftenGrownHair(rig, f);
        rig.Shell = SdfCache.Get(model, GrownMeshCell * 1.25f, GrownOutlineThickness);
        // A swimsuit and a bare chest wear nothing over the body, and most hold nothing
        if (wear.Shapes.Count > 0)
        {
            wear.BoneCount = rig.Skeleton.Count;
            rig.Clothes = SdfCache.Get(wear, GrownMeshCell);
            rig.ClothesShell = SdfCache.Get(wear, GrownMeshCell * 1.25f, GrownOutlineThickness);
        }
        if (held.Shapes.Count > 0)
        {
            held.BoneCount = rig.Skeleton.Count;
            rig.Held = SdfCache.Get(held, GrownMeshCell);
            rig.HeldShell = SdfCache.Get(held, GrownMeshCell * 1.25f, GrownOutlineThickness);
        }
        ExtractGrownFacePatch(rig, f);
        rig.PrepareScratch();
        return rig;
    }

    /// <summary>
    /// The grown skeleton (style guide, "Grown characters"): the chibi kit's 19 bones at their numbers, then the neck,
    /// the chest, the collarbones, the toes and the coat's tail; the neck, the head, the upper arms and the bag hang from
    /// bones appended after them.
    /// </summary>
    private static void GrownSkeleton(Skeleton sk, GrownFrame f)
    {
        float R = f.HeadR, sz = f.Size;
        sk.Add("root", -1, Vector3.Zero);
        sk.Add("hips", HumanBones.Root, new(0, f.HipY + 0.05f * sz, -0.01f * sz));
        sk.Add("spine", HumanBones.Hips, new(0, f.WaistY, -0.012f * sz));
        // The head turns at the skull's foot, behind the jaw
        sk.Add("head", HumanBones.Spine, f.HeadC + new Vector3(0, -0.55f * R, -0.14f * R));
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
        sk.Add("hair", HumanBones.Head, f.HeadC + new Vector3(0, 0.1f * R, -0.6f * R));
        sk.Add("bag", HumanBones.Spine, new(0, f.ChestMidY + 0.03f * sz, -f.ChestD - 0.05f * sz));
        sk.Add("skirt", HumanBones.Hips, new(0, f.HipY + 0.04f * sz, 0));
        sk.Add("neck", HumanBones.Spine, new(0, f.NeckFootY, -0.02f * sz + f.Forward * 0.3f));
        sk.Add("chest", HumanBones.Spine, new(0, f.ChestJointY, -0.015f * sz));
        foreach (var (side, name) in new[] { (1f, "L"), (-1f, "R") })
            sk.Add("clavicle." + name, HumanBones.Chest, new(side * 0.02f * sz, f.NeckFootY - 0.006f * sz, 0.02f * sz + f.Forward * 0.25f));
        foreach (var (side, name, foot) in new[] { (1f, "L", HumanBones.FootL), (-1f, "R", HumanBones.FootR) })
            sk.Add("toe." + name, foot, Side(f.Ball, side));
        sk.Add("coatTail", HumanBones.Hips, new(0, f.HipY + 0.04f * sz, -f.PelvisD * 0.8f));
        sk.Reparent(HumanBones.Neck, HumanBones.Chest);
        sk.Reparent(HumanBones.Head, HumanBones.Neck);
        sk.Reparent(HumanBones.UpperArmL, HumanBones.ClavicleL);
        sk.Reparent(HumanBones.UpperArmR, HumanBones.ClavicleR);
        sk.Reparent(HumanBones.Bag, HumanBones.Chest);
    }

    /// <summary>
    /// A grown body (style guide, "Grown characters") and what it wears: the hair's cap, the head, the hair laid over the
    /// cap, the hat, a moustache, a beard or bushy brows, the neck, the torso, the arms with their mitten hands and the
    /// legs with their feet and toes, what is held, then the clothes as a layer of their own in <paramref name="w"/>.
    /// Under a garment the torso is the garment's lining and slimmer, an arm under its sleeve slimmer still; a swimsuit
    /// and a bare chest are painted on the body itself, with nothing over them. The trousers, the shoes, the hands, the
    /// head and what is held are the body's own. A cut only carves what came before it, so the hair's cap (a mask's
    /// hood, a bowl) is carved before the head fills its face.
    /// </summary>
    private static void BuildGrown(CharacterRig rig, SdfModel m, SdfModel w, CharacterStyle s, GrownFrame f)
    {
        m.WeightBlend = GrownWeightBlend;
        GrownSkeleton(rig.Skeleton, f);
        var skin = s.Skin;
        var dress = GrownDress.Of(s);
        // A swimsuit's top is painted on the body; under a garment the body is its lining
        var upper = dress.Clothed ? Shade(s.Top, 0.62f) : s.Swimsuit ? s.Top : skin;
        float torsoSlim = dress.Clothed ? GrownUnderGarment : 1f;
        float upperSlim = dress.Sleeves ? GrownUnderSleeve : 1f, foreSlim = dress.LongSleeves ? GrownUnderSleeve : 1f;

        GrownHairCap(m, s, f);
        GrownHead(m, f, skin);
        GrownHair(m, s, f);
        GrownHat(m, s, f);
        GrownFacialHair(m, s, f);
        GrownNeck(m, f, skin);
        GrownTorso(m, f, upper, s.Bottom, skin, torsoSlim);
        foreach (float side in new[] { 1f, -1f })
        {
            // A bare arm is the skin's; a swimsuit leaves it bare, and under a sleeve it is the wearer's own arm
            GrownArm(m, f, side, skin, skin, skin, upperSlim, foreSlim);
            GrownLeg(m, f, side, dress.BareShins ? skin : s.Bottom, skin);
            GrownFoot(m, f, side, s.Shoes, s.Sole ?? Shade(s.Shoes, 0.62f), collar: dress.BareShins);
        }
        GrownSeat(m, f, s.Bottom, skin);
        GrownBaseLayer(m, f, s, upper, dress);
        GrownBlush(m, s, f);
        if (dress.Wears(s)) GrownClothes(w, s, f, dress);

        // The face's place: the surface between the eyes at the head's middle, the eyes a fifth of the head's width
        // apart, and the square the painted face covers
        var anchor = SurfaceAlong(m, new Vector3(0, f.HeadC.Y, f.HeadC.Z + 1f), -Vector3.UnitZ);
        rig.Face = new FaceLayout(anchor, GrownFaces.EyeHalfSpacing * f.HeadR, new Vector3(0, f.HeadC.Y - 0.2f * f.HeadR, anchor.Z), 0.62f * f.HeadR);
    }

    // ------------------------------------------------------------------ the grown sculpting helpers

    /// <summary>
    /// The hair's cap (style guide, "Grown characters"): one mass over the skull in the hair's colour, cut at a hairline
    /// across the forehead, round the ears and at the nape (none when bald). Every cut of part b lays its locks over it.
    /// </summary>
    private static void GrownHairCap(SdfModel m, CharacterStyle s, GrownFrame f)
    {
        float R = f.HeadR;
        var H = f.HeadC;
        var hair = s.HairColor;
        if (s.Hat == Headwear.Mask)
        {
            // A wrestler's mask (style guide, "Grown characters", "Hats"): a hood over the crown, the sides and the back
            // down to the jaw in the hat's colour, its face left bare from the brows down (no lock grows under it)
            m.Ellipsoid(CapCentre(f), CapRadii(f) + new Vector3(0.1f, 0.1f, 0.1f) * R, s.HatColor, HumanBones.Head, 0f, SurfaceMaterial.Cloth);
            m.Ellipsoid(H + new Vector3(0, -0.4f, 0.74f) * R, new Vector3(0.62f, 0.64f, 0.62f) * R, s.HatColor, HumanBones.Head, 0.1f * R, op: SdfOp.Cut);
            return;
        }
        if (s.Hair == HairCut.Bald) return;
        if (s.Hair == HairCut.Bowl)
        {
            // A bowl (style guide, "Grown characters", "Hair"): one round mass a little fuller than the cap, cut by a
            // plane straight across the brow, just over the brows, that slopes down past the tops of the ears to the nape
            m.Ellipsoid(CapCentre(f) + new Vector3(0, 0.02f, 0.02f) * R, CapRadii(f) + new Vector3(0.08f, 0.06f, 0.06f) * R, hair, HumanBones.Head, 0f, SurfaceMaterial.Hair);
            var bowl = Pitch(-0.42f);
            m.Box(H + new Vector3(0, 0.3f, 0.6f) * R + Vector3.Transform(new Vector3(0, -1.6f, -0.6f) * R, bowl), new Vector3(2f, 1.6f, 1.8f) * R, 0f, hair, HumanBones.Head,
                0.06f * R, rotation: bowl, op: SdfOp.Cut);
            return;
        }
        // Over the skull, thicker on top and at the back than at the sides, never thinner than two cells
        m.Ellipsoid(CapCentre(f), CapRadii(f), hair, HumanBones.Head, 0f, SurfaceMaterial.Hair);
        // The face's opening: the hairline across the forehead a little over half the head's half height above the
        // eyes, the temples bare at the eyes' height
        // (each cut over a wide blend, so the hair's edge is a rounded rim and never a sliver a cell thick)
        m.Ellipsoid(H + new Vector3(0, -0.35f * R, 0.62f * R), new Vector3(0.74f, 0.90f, 0.80f) * R, hair, HumanBones.Head, 0.12f * R, op: SdfOp.Cut);
        // Below the sideburns, which end in front of each ear at its middle
        foreach (float side in new[] { 1f, -1f })
            m.Ellipsoid(H + new Vector3(side * 0.95f * R, -0.55f * R, 0.05f * R), new Vector3(0.40f, 0.55f, 0.60f) * R, hair, HumanBones.Head, 0.1f * R, op: SdfOp.Cut);
        // A clean line from over the ears down to the nape: everything under a plane that falls toward the back, from
        // just in front of the ears
        float slope = MathF.Atan2(0.76f, 0.85f);
        var tilt = Pitch(-slope);
        var up = Vector3.Transform(Vector3.UnitY, tilt);
        var along = Vector3.Transform(Vector3.UnitZ, tilt);
        var onLine = H + new Vector3(0, 0.25f * R, -0.10f * R);
        m.Box(onLine - up * (1.2f * R) - along * (0.75f * R), new Vector3(2f, 1.2f, 0.8f) * R, 0f, hair, HumanBones.Head, 0.1f * R, rotation: tilt, op: SdfOp.Cut);
    }

    /// <summary>The skull's middle (style guide, "Grown characters"): behind and above the head's.</summary>
    private static Vector3 Skull(GrownFrame f) => f.HeadC + new Vector3(0, 0.20f, -0.12f) * f.HeadR;

    /// <summary>The skull's half sizes across, up and deep.</summary>
    private static Vector3 SkullRadii(GrownFrame f) => new Vector3(0.74f, 0.80f, 0.88f) * f.HeadR;

    /// <summary>
    /// The head (style guide, "Grown characters"): the skull behind and above the head's middle, a face below it
    /// narrowing over two jaw lines to a rounded chin, cheekbones, a soft brow, the ears tilted back and a nose with its
    /// wings. No eye or mouth is sculpted: the face is painted.
    /// </summary>
    private static void GrownHead(SdfModel m, GrownFrame f, Color skin)
    {
        const SurfaceMaterial Skin = SurfaceMaterial.Skin;
        const int Head = HumanBones.Head;
        float R = f.HeadR;
        var H = f.HeadC;
        // The skull (rounded into the hair's cut edge), and the face's plate below its front (the cheeks and the upper jaw).
        // Every part of the face is joined over a wide blend: a tight crease reads as an ink line under the toon light
        m.Ellipsoid(Skull(f), SkullRadii(f), skin, Head, 0.1f * R, Skin);
        m.Ellipsoid(H + new Vector3(0, -0.30f, 0.28f) * R, new Vector3(0.56f, 0.55f, 0.54f) * R, skin, Head, 0.2f * R, Skin);
        foreach (float side in new[] { 1f, -1f })
        {
            // The jaw: up from its angle under the ear, and forward from it to the chin
            var angle = H + new Vector3(side * 0.50f, -0.62f, -0.10f) * R;
            m.Capsule(H + new Vector3(side * 0.56f, -0.20f, -0.14f) * R, angle, 0.10f * R, 0.11f * R, skin, Head, 0.16f * R, Skin);
            m.Capsule(angle, H + new Vector3(side * 0.18f, -0.90f, 0.52f) * R, 0.11f * R, 0.10f * R, skin, Head, 0.16f * R, Skin);
            m.Ellipsoid(H + new Vector3(side * 0.46f, -0.12f, 0.50f) * R, new Vector3(0.18f, 0.11f, 0.15f) * R, skin, Head, 0.16f * R, Skin);
        }
        // The chin forward under the mouth, the lips' soft swell above it, and a soft brow
        m.Ellipsoid(H + new Vector3(0, -0.86f, 0.58f) * R, new Vector3(0.22f, 0.15f, 0.16f) * R, skin, Head, 0.14f * R, Skin);
        m.Ellipsoid(H + new Vector3(0, -0.62f, 0.70f) * R, new Vector3(0.22f, 0.11f, 0.10f) * R, skin, Head, 0.10f * R, Skin);
        m.Ellipsoid(H + new Vector3(0, 0.18f, 0.66f) * R, new Vector3(0.48f, 0.10f, 0.14f) * R, skin, Head, 0.14f * R, Skin);
        GrownNose(m, f, skin);
        // The ears at the middle of the sides, from the eyes' height to the nose's foot, tilted back
        foreach (float side in new[] { 1f, -1f })
            m.Ellipsoid(H + new Vector3(side * 0.73f, -0.12f, -0.20f) * R, new Vector3(0.09f, 0.26f, 0.15f) * R, skin, Head, 0.05f * R, Skin, Pitch(-0.2f));
    }

    /// <summary>
    /// The nose (style guide, "Grown characters"), which a chibi face never had: a bridge from between the eyes down to a
    /// round tip standing forward of the face, and two wings beside the tip.
    /// </summary>
    private static void GrownNose(SdfModel m, GrownFrame f, Color skin)
    {
        float R = f.HeadR;
        var H = f.HeadC;
        m.Capsule(H + new Vector3(0, 0.05f, 0.74f) * R, H + new Vector3(0, -0.34f, 0.88f) * R, 0.06f * R, 0.08f * R, skin, HumanBones.Head, 0.05f * R, SurfaceMaterial.Skin);
        m.Sphere(H + new Vector3(0, -0.36f, 0.90f) * R, 0.09f * R, skin, HumanBones.Head, 0.04f * R, SurfaceMaterial.Skin);
        foreach (float side in new[] { 1f, -1f })
            m.Sphere(H + new Vector3(side * 0.11f, -0.40f, 0.80f) * R, 0.07f * R, skin, HumanBones.Head, 0.04f * R, SurfaceMaterial.Skin);
    }

    /// <summary>The neck (style guide, "Grown characters"): a round cone from its foot up and back to under the skull, on its own bone.</summary>
    private static void GrownNeck(SdfModel m, GrownFrame f, Color skin)
    {
        float R = f.HeadR, g = f.Girth, sz = f.Size;
        var foot = new Vector3(0, f.NeckFootY - 0.025f * sz, -0.02f * sz + f.Forward * 0.3f);
        // Its top under the skull, behind the jaw, so the jaw's underside shows above the throat
        var top = f.HeadC + new Vector3(0, -0.72f, -0.25f) * R;
        m.Capsule(foot, top, 0.055f * g, 0.050f * g, skin, HumanBones.Neck, 0.02f * sz, SurfaceMaterial.Skin);
    }

    /// <summary>
    /// The torso (style guide, "Grown characters"): a pelvis on the hips, a waist on the spine and a rib cage on the
    /// chest, the top of the chest out to the shoulders under the collarbones, a slope from the neck's foot to each
    /// shoulder on its collarbone, and a curved figure's bust.
    /// </summary>
    private static void GrownTorso(SdfModel m, GrownFrame f, Color upper, Color lower, Color skin, float slim = 1f)
    {
        var Cloth = SurfaceOf(upper, skin);
        float sz = f.Size, g = f.Girth;
        // Under a garment the torso is a little slimmer across and through, so nothing of it pokes through
        var under = new Vector3(slim, 1f, slim);
        m.Ellipsoid(new Vector3(0, f.HipY + 0.045f * sz, -0.008f * sz), new Vector3(f.PelvisW, 0.1f * sz, f.PelvisD), lower, HumanBones.Hips, 0f, SurfaceOf(lower, skin));
        m.Ellipsoid(new Vector3(0, f.WaistY, 0.004f * sz), new Vector3(f.WaistW, (f.ChestJointY - f.HipY) * 0.62f, f.WaistD) * under, upper, HumanBones.Spine, 0.04f * sz, Cloth);
        m.Ellipsoid(new Vector3(0, f.ChestMidY, 0f), new Vector3(f.ChestW, (f.ShoulderY - f.WaistY) * 0.5f, f.ChestD) * under, upper, HumanBones.Chest, 0.04f * sz, Cloth);
        m.Ellipsoid(new Vector3(0, f.ShoulderY - 0.035f * sz, -0.008f * sz + f.Forward * 0.25f), new Vector3(f.Shoulder.X * 0.92f, 0.055f * sz, f.ChestD * 0.85f) * under,
            upper, HumanBones.Chest, 0.03f * sz, Cloth);
        // The trapezius: from up the back of the neck down and out to the top of each shoulder
        foreach (var (side, clavicle) in new[] { (1f, HumanBones.ClavicleL), (-1f, HumanBones.ClavicleR) })
            m.Capsule(new Vector3(side * 0.028f * sz, f.NeckFootY + 0.045f * sz, -0.035f * sz + f.Forward * 0.25f),
                new Vector3(side * (f.Shoulder.X - 0.035f * sz), f.ShoulderY + 0.022f * sz, -0.015f * sz + f.Forward * 0.25f),
                0.036f * g, 0.028f * g, upper, clavicle, 0.03f * sz, Cloth);
        if (f.Curved)
            foreach (float side in new[] { 1f, -1f })
                m.Ellipsoid(new Vector3(side * f.ChestW * 0.42f, f.ChestMidY + 0.02f * sz, f.ChestD * 0.66f), new Vector3(0.046f, 0.044f, 0.04f) * sz,
                    upper, HumanBones.Chest, 0.025f * sz, Cloth);
        // The elder's upper back is rounded
        if (f.Forward > 0f)
            m.Ellipsoid(new Vector3(0, f.ChestMidY + 0.06f * sz, -f.ChestD * 0.55f), new Vector3(f.ChestW * 0.8f, 0.09f * sz, f.ChestD * 0.6f), upper, HumanBones.Chest, 0.04f * sz, Cloth);
    }

    /// <summary>
    /// An arm (style guide, "Grown characters"): the upper arm a tapered cone with the deltoid at the shoulder, an elbow a
    /// little fuller than both its segments on the forearm's bone, the forearm fuller near the elbow, and its hand.
    /// </summary>
    private static void GrownArm(SdfModel m, GrownFrame f, float side, Color upperArm, Color forearm, Color skin, float upperSlim = 1f, float foreSlim = 1f)
    {
        // Under a sleeve the arm is slimmer (style guide, "Grown characters", "Clothes"), so the sleeve never meets it
        float g = f.Girth, sz = f.Size, gu = g * upperSlim, gf = g * foreSlim;
        int upperB = side > 0f ? HumanBones.UpperArmL : HumanBones.UpperArmR;
        int foreB = side > 0f ? HumanBones.ForeArmL : HumanBones.ForeArmR;
        int handB = side > 0f ? HumanBones.HandL : HumanBones.HandR;
        var sh = Side(f.Shoulder, side);
        var el = Side(f.Elbow, side);
        var wr = Side(f.Wrist, side);
        var Mat = SurfaceOf(upperArm, skin);
        // The deltoid, whose bone's weight reaches down the side of the chest under it (GrownShoulderReach), so a raised
        // arm's bend is spread over the armpit and no edge there stretches (plan 30 · FM10)
        m.Ellipsoid(sh + new Vector3(side * 0.006f * gu, -0.03f * sz, 0f), new Vector3(0.046f * gu, 0.07f * sz, 0.045f * gu), upperArm, upperB, 0.025f * sz, Mat, AlignY(el - sh))
            .WeightReach = GrownShoulderReach * sz;
        m.Capsule(sh, el, 0.044f * gu, 0.030f * Math.Max(gu, gf), upperArm, upperB, 0.02f * sz, Mat);
        var fMat = SurfaceOf(forearm, skin);
        m.Sphere(el, 0.032f * gf, forearm, foreB, 0.015f * sz, fMat);
        m.Capsule(el, wr, 0.031f * gf, 0.024f * gf, forearm, foreB, 0.015f * sz, fMat);
        m.Ellipsoid(Vector3.Lerp(el, wr, 0.25f), new Vector3(0.035f * gf, 0.25f * Vector3.Distance(el, wr), 0.032f * gf), forearm, foreB, 0.02f * sz, fMat, AlignY(wr - el));
        GrownHand(m, f, wr, Vector3.Normalize(wr - el), side, skin, handB);
    }

    /// <summary>
    /// A mitten hand (style guide, "Grown characters"): a flat palm facing the body, the fingers one mass a little
    /// curled toward it and a thumb forward, on the hand's bone. Nothing of it is thinner than the kit's cells allow.
    /// </summary>
    private static void GrownHand(SdfModel m, GrownFrame f, Vector3 wrist, Vector3 along, float side, Color skin, int bone)
    {
        const SurfaceMaterial Skin = SurfaceMaterial.Skin;
        float L = f.HandLength, sz = f.Size;
        var toBody = new Vector3(-side, 0, 0);
        var inward = Vector3.Normalize(toBody - along * Vector3.Dot(toBody, along));
        var forward = Vector3.UnitZ - along * along.Z - inward * inward.Z;
        forward = forward.LengthSquared() > 1e-6f ? Vector3.Normalize(forward) : Vector3.UnitZ;
        var basis = Basis(inward, along);
        // The thinnest a part may be: two and a half cells across, the thumb two
        float thin = 1.25f * GrownMeshCell, thumb = 1f * GrownMeshCell;
        m.Ellipsoid(wrist + along * (0.28f * L), new Vector3(Math.Max(0.07f * L, thin), 0.27f * L, 0.19f * L), skin, bone, 0.008f * sz, Skin, basis);
        // The fingers, long and curled a little toward the palm
        var curl = Quaternion.CreateFromAxisAngle(Vector3.Normalize(Vector3.Cross(along, inward)), 0.15f);
        var fingers = Vector3.Transform(along, curl);
        m.Ellipsoid(wrist + along * (0.48f * L) + fingers * (0.25f * L) + inward * (0.005f * L), new Vector3(Math.Max(0.055f * L, thin), 0.27f * L, 0.17f * L),
            skin, bone, 0.008f * sz, Skin, Quaternion.Concatenate(basis, curl));
        // The thumb along the palm's forward edge, toward the fingers
        m.Capsule(wrist + along * (0.14f * L) + inward * (0.04f * L) + forward * (0.11f * L), wrist + along * (0.44f * L) + inward * (0.08f * L) + forward * (0.16f * L),
            Math.Max(0.06f * L, thumb), Math.Max(0.05f * L, thumb), skin, bone, 0.006f * sz, Skin);
    }

    /// <summary>
    /// A leg (style guide, "Grown characters"): the thigh a cone from inside the pelvis tapering into the knee with its
    /// front fuller, a knee a little fuller than both its segments on the shin's bone, and the shin with the calf
    /// swelling at the back of its upper third.
    /// </summary>
    private static void GrownLeg(SdfModel m, GrownFrame f, float side, Color leg, Color skin)
    {
        float g = f.Girth, sz = f.Size;
        int thighB = side > 0f ? HumanBones.ThighL : HumanBones.ThighR;
        int shinB = side > 0f ? HumanBones.ShinL : HumanBones.ShinR;
        var hip = Side(f.LegTop, side);
        var knee = Side(f.Knee, side);
        var ankle = Side(f.Ankle, side);
        var mat = SurfaceOf(leg, skin);
        // Small blends: a wide one joins the leg to the other across the gap between them, and the field's dip there
        // brings the outline's shells of the two together
        // The thigh's top inside the pelvis, below the waist (higher, a broad build's reached its side, which a sitter's
        // thigh then dragged forward)
        m.Capsule(hip + new Vector3(0, 0.01f * sz, 0), knee, 0.074f * g, 0.046f * g, leg, thighB, 0.015f * sz, mat);
        m.Ellipsoid(Vector3.Lerp(hip, knee, 0.38f) + new Vector3(side * 0.006f * g, 0, 0.022f * g), new Vector3(0.058f * g, 0.30f * Vector3.Distance(hip, knee), 0.05f * g),
            leg, thighB, 0.015f * sz, mat, AlignY(knee - hip));
        m.Sphere(knee, 0.047f * g, leg, shinB, 0.012f * sz, mat);
        m.Capsule(knee, ankle + new Vector3(0, 0.01f * sz, 0), 0.045f * g, 0.031f * g, leg, shinB, 0.012f * sz, mat);
        m.Ellipsoid(Vector3.Lerp(knee, ankle, 0.3f) + new Vector3(side * 0.004f * g, 0, -0.02f * g), new Vector3(0.046f * g, 0.26f * Vector3.Distance(knee, ankle), 0.043f * g),
            leg, shinB, 0.015f * sz, mat, AlignY(ankle - knee));
    }

    /// <summary>
    /// The seat (style guide, "Grown characters"): a piece of the pelvis between the tops of the thighs, on the hips'
    /// bone, made after both legs so the crotch moves with the pelvis and never with both thighs at once (it stretched
    /// between them in a stride).
    /// </summary>
    private static void GrownSeat(SdfModel m, GrownFrame f, Color bottom, Color skin)
    {
        float sz = f.Size;
        m.Ellipsoid(new Vector3(0, f.HipY - 0.03f * sz, -0.012f * sz), new Vector3(f.LegTop.X * 0.7f, 0.05f * sz, f.PelvisD * 0.8f), bottom, HumanBones.Hips, 0.015f * sz,
            SurfaceOf(bottom, skin));
    }

    /// <summary>
    /// A foot in its shoe (style guide, "Grown characters"): the heel and the instep on the foot's bone, the ball and the
    /// toes on the toe's, the shoe the foot's own surface in its colour with its sole painted flat under it.
    /// </summary>
    private static void GrownFoot(SdfModel m, GrownFrame f, float side, Color shoe, Color sole, bool collar)
    {
        const SurfaceMaterial Leather = SurfaceMaterial.Leather;
        int footB = side > 0f ? HumanBones.FootL : HumanBones.FootR;
        int toeB = side > 0f ? HumanBones.ToeL : HumanBones.ToeR;
        float F = f.FootLength, sz = f.Size;
        var ankle = Side(f.Ankle, side);
        var ball = Side(f.Ball, side);
        float heel = ankle.Z - 0.20f * F, tip = ankle.Z + 0.80f * F;
        float w = 0.18f * F;
        // The heel to the ball, flat on the ground, and the instep rising from it to the ankle
        m.Box(new Vector3(ankle.X, 0.03f * sz, (heel + ball.Z) / 2f), new Vector3(w * 0.95f, 0.03f * sz, (ball.Z - heel) / 2f + 0.01f * sz), 0.025f * sz,
            shoe, footB, 0.012f * sz, Leather);
        m.Ellipsoid(new Vector3(ankle.X, 0.058f * sz, ankle.Z + 0.14f * F), new Vector3(w * 0.85f, 0.034f * sz, 0.3f * F), shoe, footB, 0.02f * sz, Leather);
        // Over bare legs the shoe's collar round the ankle shows; trousers fall over it
        if (collar) m.Sphere(new Vector3(ankle.X, ankle.Y + 0.004f * sz, ankle.Z - 0.004f * sz), 0.036f * f.Girth, shoe, footB, 0.015f * sz, Leather);
        // The ball and the toes
        float toeH = 0.026f * sz;
        m.Box(new Vector3(ball.X, toeH, (ball.Z + tip) / 2f), new Vector3(w * 0.92f, toeH, (tip - ball.Z) / 2f), 0.022f * sz, shoe, toeB, 0.012f * sz, Leather);
        // The sole, flat on the ground
        m.Paint(m.Box(new Vector3(ankle.X, 0f, (heel + tip) / 2f), new Vector3(w * 1.6f, 0.012f * sz, F), 0f, sole, footB, GrownPaintEdge), true, footB, toeB).Material = SurfaceMaterial.Plastic;
    }

    /// <summary>
    /// What is painted on the body itself (style guide, "Grown characters", "The body under the clothes"): the
    /// waistline between what is over the torso (a garment's lining, a swimsuit's top, a bare chest's skin) and the
    /// trousers or a swimsuit's bottom, the neck bare above a round neckline, shorts' hems at mid-thigh and the
    /// trousers' hems over the shoes. The sleeves, collars and hems of what is worn over the body are the clothes'
    /// layer's own. Painted once everything is joined on, or the shapes joined after would blur the lines.
    /// </summary>
    private static void GrownBaseLayer(SdfModel m, GrownFrame f, CharacterStyle s, Color upper, GrownDress dress)
    {
        float sz = f.Size;
        const SurfaceMaterial Cloth = SurfaceMaterial.Cloth, Skin = SurfaceMaterial.Skin;
        // The waistline: the top above it, the trousers (or a swimsuit's bottom) below
        float waistline = f.WaistY - 0.025f * sz;
        PaintBox(m, new Vector3(0, waistline + 0.3f, 0), new Vector3(0.5f, 0.3f, 0.5f), Quaternion.Identity, upper, SurfaceOf(upper, s.Skin),
            HumanBones.Spine, HumanBones.Hips, HumanBones.Chest, HumanBones.ClavicleL, HumanBones.ClavicleR);
        // Behind the neck's foot, where the garment's own rise lies close over the body, the body is the garment's colour,
        // so nothing darker shows between them
        if (dress.Clothed)
            PaintBox(m, new Vector3(0, f.NeckFootY + 0.03f * sz - 0.3f, -0.34f), new Vector3(0.5f, 0.3f, 0.3f), Quaternion.Identity, s.Top, Cloth,
                HumanBones.Neck, HumanBones.Chest, HumanBones.ClavicleL, HumanBones.ClavicleR);
        PaintBox(m, new Vector3(0, waistline - 0.3f, 0), new Vector3(0.5f, 0.3f, 0.5f), Quaternion.Identity, s.Bottom, Cloth, HumanBones.Hips, HumanBones.Spine);
        // A round neckline at the neck's foot, the neck bare above it: a crew neck, lower at the front than at the back,
        // where the shirt rises up the neck's foot
        var neckline = new Vector3(0, f.NeckFootY - 0.014f * sz, 0.012f * sz + f.Forward * 0.3f);
        float round = 0.055f * f.Girth + 0.008f * sz;
        m.Paint(m.Capsule(neckline, f.HeadC + new Vector3(0, 0, 0.012f * sz), round, round, s.Skin, HumanBones.Neck, GrownPaintEdge), true,
            HumanBones.Neck, HumanBones.Chest, HumanBones.ClavicleL, HumanBones.ClavicleR).Material = Skin;
        foreach (float side in new[] { 1f, -1f })
        {
            int thighB = side > 0f ? HumanBones.ThighL : HumanBones.ThighR;
            int shinB = side > 0f ? HumanBones.ShinL : HumanBones.ShinR;
            int footB = side > 0f ? HumanBones.FootL : HumanBones.FootR;
            var hip = Side(f.LegTop, side);
            var knee = Side(f.Knee, side);
            var ankle = Side(f.Ankle, side);
            if (dress.BareShins && !dress.BareThighs)
            {
                // Shorts end at mid-thigh (a skirt is the clothes' own, over bare legs)
                var along = Vector3.Normalize(knee - hip);
                var hem = Vector3.Lerp(hip, knee, 0.5f);
                PaintBox(m, hem - along * 0.2f, new Vector3(0.12f, 0.2f, 0.12f), AlignY(along), s.Bottom, Cloth, thighB, HumanBones.Hips);
            }
            if (!dress.BareShins)
            {
                // The trousers' hem, a shade darker, over the shoe's top
                PaintBox(m, ankle + new Vector3(0, 0.15f + 0.03f * sz, 0), new Vector3(0.1f, 0.15f, 0.1f), Quaternion.Identity, s.Bottom, Cloth, shinB);
                PaintBox(m, ankle + new Vector3(0, 0.024f * sz, 0), new Vector3(0.1f, 0.006f * sz, 0.1f), Quaternion.Identity, Shade(s.Bottom, 0.86f), Cloth, shinB, footB);
            }
        }
    }

    /// <summary>
    /// How soft a painted edge of the grown kit is: a cell and a half, the least that keeps it from stepping along the
    /// mesh's cells (a body this fine shows a crisp paint's every step; the chibi kit's hems are geometry instead).
    /// </summary>
    public const float GrownPaintEdge = 1.5f * GrownMeshCell;

    /// <summary>Paints the surface inside a box on the given bones, in a colour and a material, its edges a cell and a half soft.</summary>
    private static void PaintBox(SdfModel m, Vector3 centre, Vector3 half, Quaternion turn, Color colour, SurfaceMaterial material, params int[] bones) =>
        m.Paint(m.Box(centre, half, 0f, colour, bones[0], GrownPaintEdge, rotation: turn), true, bones).Material = material;

    /// <summary>Skin where a colour is the skin's, cloth otherwise.</summary>
    private static SurfaceMaterial SurfaceOf(Color colour, Color skin) => colour.Equals(skin) ? SurfaceMaterial.Skin : SurfaceMaterial.Cloth;

    /// <summary>The rotation that turns +X onto <paramref name="x"/> and +Y onto <paramref name="y"/> (perpendicular unit vectors).</summary>
    private static Quaternion Basis(Vector3 x, Vector3 y)
    {
        var z = Vector3.Cross(x, y);
        var rows = new Matrix4x4(x.X, x.Y, x.Z, 0, y.X, y.Y, y.Z, 0, z.X, z.Y, z.Z, 0, 0, 0, 0, 1);
        return Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(rows));
    }

    /// <summary>
    /// Bends the normals of the grown head's skin (style guide, "Grown characters", "Light"): 60 % toward those of a
    /// smooth egg round the head, so the face lights as one form and its creases never read as ink lines, then the
    /// front of the face half as far toward <c>0, 0.75, 0.66</c> as a chibi face, so the shade's edge doesn't split it;
    /// both less where the head's weight gives way to the neck's and where the nose and the ears stand out of the egg,
    /// so they still shade.
    /// </summary>
    private static void SoftenGrownFace(CharacterRig rig, GrownFrame f)
    {
        var mesh = rig.Mesh;
        var skin = rig.Style.Skin;
        var forward = Vector3.Normalize(new Vector3(0, 0.75f, 0.66f));
        var (centre, radii) = GrownHeadEgg(f);
        for (int v = 0; v < mesh.VertexCount; v++)
        {
            float head = mesh.WeightOf(v, HumanBones.Head);
            if (head < 0.5f) continue;
            var c = mesh.Colors[v];
            int dr = c.R - skin.R, dg = c.G - skin.G, db = c.B - skin.B;
            if (dr * dr + dg * dg + db * db > 70 * 70) continue;
            var q = (mesh.Positions[v] - centre) / radii;
            float outside = q.Length();
            if (outside < 1e-4f) continue;
            float standsOut = Math.Clamp((outside - 1.06f) / 0.08f, 0f, 1f);
            float keep = Math.Clamp((head - 0.5f) * 2f, 0f, 1f) * (1f - standsOut * standsOut * (3f - 2f * standsOut));
            var egg = Vector3.Normalize(q / radii);
            var n = Vector3.Normalize(Vector3.Lerp(mesh.Normals[v], egg, 0.6f * keep));
            float front = Math.Clamp(n.Z * 1.6f + 0.2f, 0f, 1f) * 0.42f * keep;
            mesh.Normals[v] = Vector3.Normalize(Vector3.Lerp(n, forward, front));
        }
    }

    /// <summary>The smooth egg a grown head's light follows: round the skull and the face together, from the crown to the chin.</summary>
    internal static (Vector3 Centre, Vector3 Radii) GrownHeadEgg(GrownFrame f) =>
        (f.HeadC + new Vector3(0, -0.04f, -0.04f) * f.HeadR, new Vector3(0.74f, 1.02f, 0.88f) * f.HeadR);

    /// <summary>
    /// Bends the hair's normals 80 % toward those of the smooth mass it fills (style guide, "Grown characters", "Light"):
    /// a sphere round the skull, stretched into a capsule down the back for hair that falls below the head (long hair, a
    /// ponytail), for one clean shade edge over the head (a grown head's locks are finer beside its cells than a chibi
    /// head's, so its normals turn more between neighbours and want the stronger bend); a beard and a moustache keep
    /// theirs, which follow the face.
    /// </summary>
    private static void SoftenGrownHair(CharacterRig rig, GrownFrame f)
    {
        var mesh = rig.Mesh;
        float R = f.HeadR;
        var middle = f.HeadC + new Vector3(0, 0.24f * R, -0.10f * R);
        // What hangs below the skull's own foot behind the head (long hair, a ponytail) lies along a capsule down the back
        float lowest = middle.Y;
        for (int v = 0; v < mesh.VertexCount; v++)
            if (mesh.Materials[v] == (byte)SurfaceMaterial.Hair && mesh.Positions[v].Z < f.HeadC.Z - 0.5f * R)
                lowest = Math.Min(lowest, mesh.Positions[v].Y);
        float drop = Math.Max(0f, middle.Y - lowest - 1.2f * R);
        var axis = new Vector3(0, -drop, -drop * 0.2f);
        float length2 = axis.LengthSquared();
        for (int v = 0; v < mesh.VertexCount; v++)
        {
            if (mesh.Materials[v] != (byte)SurfaceMaterial.Hair) continue;
            var p = mesh.Positions[v];
            // Facial hair in front of the face is lit with the face
            if (p.Z > f.HeadC.Z + 0.3f * R && p.Y < f.HeadC.Y - 0.25f * R) continue;
            float t = length2 < 1e-6f ? 0f : Math.Clamp(Vector3.Dot(p - middle, axis) / length2, 0f, 1f);
            var away = p - (middle + axis * t);
            if (away.LengthSquared() < 1e-8f) continue;
            mesh.Normals[v] = Vector3.Normalize(Vector3.Lerp(mesh.Normals[v], Vector3.Normalize(away), 0.8f));
        }
    }

    /// <summary>
    /// The front of a grown head, where its painted face is drawn (style guide, "Grown characters", "Faces"): the skin's
    /// triangles moving only with the head inside the face's square and facing forward, copied a hair's breadth out from
    /// the head and mapped onto the face's texture, as the chibi kit's patch is.
    /// </summary>
    private static void ExtractGrownFacePatch(CharacterRig rig, GrownFrame f)
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
            if (MathF.Abs(p.X - c.X) > half || MathF.Abs(p.Y - c.Y) > half || p.Z < f.HeadC.Z) return false;
            if (mesh.Normals[v].Z < 0.15f) return false;
            // Skin, or the blush painted on it; never hair
            var col = mesh.Colors[v];
            int dr = col.R - skin.R, dg = col.G - skin.G, db = col.B - skin.B;
            return dr * dr + dg * dg + db * db < 70 * 70;
        }

        var map = new System.Collections.Generic.Dictionary<int, int>();
        var indices = new System.Collections.Generic.List<int>();
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
            patch.Positions[dst] = mesh.Positions[src] + nrm * GrownFaceLift;
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

    /// <summary>How far out from a grown head its face patch lies: a hair's breadth at a grown head's size.</summary>
    public const float GrownFaceLift = 0.0012f;
}
