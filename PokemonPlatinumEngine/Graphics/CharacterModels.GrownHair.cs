using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The grown kit's hair, hats and what grows on a face (plan 30 · FM4, part b; style guide, "Grown characters"): every
/// cut of the chibi kit laid in locks over the grown hair's cap, which follow the cap's own surface (an ellipsoid round
/// the skull, not the chibi's sphere); every hat on the grown hair, the locks under it growing from below its edge;
/// moustaches, beards and bushy brows at a grown face's size. Sizes are in the head's half height (<c>R</c>).
/// </summary>
internal static partial class CharacterModels
{
    /// <summary>The middle of the hair's cap (style guide, "Grown characters"): the skull's, a little up and back.</summary>
    internal static Vector3 CapCentre(GrownFrame f) => Skull(f) + new Vector3(0, 0.06f, -0.02f) * f.HeadR;

    /// <summary>The hair's cap's half sizes across, up and deep: the skull's and the hair's thickness over it.</summary>
    internal static Vector3 CapRadii(GrownFrame f) => SkullRadii(f) + new Vector3(0.12f, 0.06f, 0.12f) * f.HeadR;

    /// <summary>
    /// The point of an ellipsoid's surface along a direction of its own space (where it is a unit sphere), and out from
    /// it along its normal by <paramref name="lift"/> of the head's half height.
    /// </summary>
    private static Vector3 OnShell(Vector3 centre, Vector3 radii, Vector3 direction, float lift, float headR)
    {
        var d = Vector3.Normalize(direction);
        var n = Vector3.Normalize(d / radii);
        return centre + d * radii + n * (lift * headR);
    }

    /// <summary>The point of the hair's cap along a direction of its own space, out from it by <paramref name="lift"/> of R.</summary>
    internal static Vector3 OnCap(GrownFrame f, Vector3 direction, float lift = 0f) => OnShell(CapCentre(f), CapRadii(f), direction, lift, f.HeadR);

    /// <summary>The least a part of the grown kit may be across: two and a half of its cells (style guide, "Meshing").</summary>
    private const float GrownLeastRadius = 1.25f * GrownMeshCell;

    /// <summary>
    /// A lock of grown hair (style guide, "Grown characters", "Hair"): a tapering tube along an ellipsoid's surface from
    /// one direction of its space to another, its middle sunk <paramref name="sink"/> of R under the surface and lifting
    /// off it toward its tip, joined over a small blend so a groove shows between it and the next, never thinner than
    /// the kit's cells allow. Radii and distances are in R.
    /// </summary>
    private static void LockOn(SdfModel m, GrownFrame f, Vector3 centre, Vector3 radii, Color hair, Vector3 from, Vector3 to, float r0, float r1,
        float lift = 0.1f, float sink = 0.08f, int bone = HumanBones.Head)
    {
        var a = Vector3.Normalize(from);
        var b = Vector3.Normalize(to);
        float omega = MathF.Acos(Math.Clamp(Vector3.Dot(a, b), -1f, 1f));
        Vector3 Dir(float t) => omega < 1e-3f ? a : (MathF.Sin((1f - t) * omega) * a + MathF.Sin(t * omega) * b) / MathF.Sin(omega);
        float R = f.HeadR;
        const int Segments = 6;
        Vector3 At(float t) => OnShell(centre, radii, Dir(t), -sink + lift * t * t, R);
        for (int i = 0; i < Segments; i++)
        {
            float t0 = i / (float)Segments, t1 = (i + 1f) / Segments;
            m.Capsule(At(t0), At(t1), Math.Max(GrownLeastRadius, (r0 + (r1 - r0) * t0) * R), Math.Max(GrownLeastRadius, (r0 + (r1 - r0) * t1) * R),
                hair, bone, 0.03f * R, SurfaceMaterial.Hair);
        }
    }

    /// <summary>A lock over the hair's cap (<see cref="LockOn"/>), its directions in the cap's own space.</summary>
    private static void GrownLock(SdfModel m, GrownFrame f, Color hair, Vector3 from, Vector3 to, float r0, float r1, float lift = 0.1f, float sink = 0.08f,
        int bone = HumanBones.Head) => LockOn(m, f, CapCentre(f), CapRadii(f), hair, from, to, r0, r1, lift, sink, bone);

    /// <summary>A tube from point to point that tapers, never thinner than the kit allows (radii in R).</summary>
    private static void GrownTube(SdfModel m, GrownFrame f, Vector3 a, Vector3 b, float r0, float r1, Color colour, int bone, float blend, SurfaceMaterial material) =>
        m.Capsule(a, b, Math.Max(GrownLeastRadius, r0 * f.HeadR), Math.Max(GrownLeastRadius, r1 * f.HeadR), colour, bone, blend * f.HeadR, material);

    /// <summary>
    /// A brim or a peak (style guide, "Grown characters", "Hats"): a flat disc with a fully rounded edge, never thinner
    /// than the kit's cells allow (an ellipsoid's edge thins to nothing and breaks up in the mesh), its half sizes across
    /// and to the front <paramref name="rx"/> and <paramref name="rz"/> of R.
    /// </summary>
    private static SdfPrimitive Brim(SdfModel m, GrownFrame f, Vector3 centre, float rx, float rz, Color colour, Quaternion tilt, SurfaceMaterial material, float blend = 0.02f)
    {
        float h = Math.Max(1.3f * GrownMeshCell, 0.08f * f.HeadR);
        var disc = m.Cylinder(centre, rx * f.HeadR, h, h, colour, HumanBones.Head, blend * f.HeadR, material, tilt);
        disc.Stretch = new Vector3(1f, 1f, rz / rx);
        return disc;
    }

    /// <summary>
    /// A band round a head or a hat's crown (style guide, "Grown characters", "Hats"): a ring of its half sizes across
    /// and through (<paramref name="rx"/>, <paramref name="rz"/> of R), a tube never thinner than the kit allows, joined
    /// to what it lies on over a blend of at least 0.08 R (a tight crease specks the outline), and its colour painted
    /// crisp over it and the crease round it (a blended colour's edge breaks into specks along the cells).
    /// </summary>
    private static SdfPrimitive Band(SdfModel m, GrownFrame f, Vector3 centre, float rx, float rz, float tube, Color colour, Quaternion tilt, SurfaceMaterial material,
        float blend = 0.08f, int bone = HumanBones.Head)
    {
        float t = Math.Max(1.6f * GrownMeshCell, tube * f.HeadR);
        var stretch = new Vector3(rx / rz, 1f, 1f);
        var ring = m.Torus(centre, rz * f.HeadR, t, colour, bone, Math.Max(blend, 0.08f) * f.HeadR, material, tilt);
        ring.Stretch = stretch;
        var paint = m.Torus(centre, rz * f.HeadR, t * 1.1f, colour, bone, GrownPaintEdge, material, tilt);
        paint.Stretch = stretch;
        m.Paint(paint, true, bone);
        return ring;
    }

    /// <summary>
    /// The middle of a band round the head (a headband, a visor's) at <paramref name="height"/> of R over the eyes: the
    /// head and its hair there are an ellipse narrower across than through, set back from the head's middle, so a band
    /// of 0.97 by 1.07 R round it lies on the forehead in front and on the hair at the sides and behind.
    /// </summary>
    private static Vector3 HeadBandAt(GrownFrame f, float height) => f.HeadC + new Vector3(0, height, -0.18f) * f.HeadR;

    /// <summary>
    /// Whether a hat comes down to the brow (style guide, "Grown characters", "Hats"): under it no bangs show.
    /// </summary>
    private static bool CoversTheBrow(Headwear hat) => hat is Headwear.Bandana or Headwear.Beanie or Headwear.Helmet or Headwear.HardHat or Headwear.Mask;

    /// <summary>
    /// Whether a hat covers the crown (style guide, "Grown characters", "Hats"): under it the locks grow from below its
    /// edge, and none grows over the top.
    /// </summary>
    private static bool CoversTheCrown(Headwear hat) => hat is Headwear.Beret or Headwear.Cap or Headwear.Fedora or Headwear.Helmet or Headwear.HardHat
        or Headwear.Beanie or Headwear.Bandana or Headwear.Cowboy or Headwear.WideHat or Headwear.Mask;

    /// <summary>The crown's whorl on the cap, where unhatted locks grow from (the cap's own space).</summary>
    private static readonly Vector3 Whorl = Vector3.Normalize(new Vector3(0f, 0.86f, -0.5f));

    /// <summary>
    /// The hair laid over the cap (style guide, "Grown characters", "Hair"), cut by cut: bangs over the forehead ending
    /// over the brows, never over them, and each cut's own shapes. A bowl and an afro are their masses (built with the
    /// cap, <see cref="GrownHairCap"/>), bald a band round the back of the skull; under a mask nothing grows.
    /// </summary>
    private static void GrownHair(SdfModel m, CharacterStyle s, GrownFrame f)
    {
        if (s.Hat == Headwear.Mask) return;
        var hair = s.HairColor;
        float R = f.HeadR;
        bool hatted = CoversTheCrown(s.Hat);
        // Under a hat a lock grows from just below its edge, on the same side as where it falls
        Vector3 Root(Vector3 to) => hatted ? Vector3.Normalize(new Vector3(to.X * 0.92f, 0.3f, to.Z == 0f ? -0.95f : MathF.Sign(to.Z) * 0.9f)) : Whorl;

        switch (s.Hair)
        {
            case HairCut.Bowl:
                return;
            case HairCut.Afro:
                GrownAfro(m, s, f);
                return;
            case HairCut.Bald:
            {
                // A fringe of short locks round the back of the skull from behind one ear to behind the other, two rows
                // deep down to the nape, lying close to it, the crown bare skin (one row across the back read as a
                // headband)
                var c = Skull(f);
                var radii = SkullRadii(f);
                foreach (float side in new[] { 1f, -1f })
                {
                    LockOn(m, f, c, radii, hair, new Vector3(side * 0.94f, -0.14f, -0.32f), new Vector3(side * 0.4f, -0.25f, -0.9f), 0.12f, 0.115f, 0f, 0.05f);
                    LockOn(m, f, c, radii, hair, new Vector3(side * 0.88f, -0.36f, -0.38f), new Vector3(side * 0.36f, -0.47f, -0.82f), 0.12f, 0.11f, 0f, 0.05f);
                }
                LockOn(m, f, c, radii, hair, new Vector3(0.42f, -0.25f, -0.9f), new Vector3(-0.42f, -0.25f, -0.9f), 0.12f, 0.12f, 0f, 0.05f);
                LockOn(m, f, c, radii, hair, new Vector3(0.38f, -0.47f, -0.82f), new Vector3(-0.38f, -0.47f, -0.82f), 0.12f, 0.12f, 0f, 0.05f);
                return;
            }
            case HairCut.Curls:
                GrownCurls(m, s, f, hatted);
                return;
        }

        // Bangs over the forehead, pointed, ending over the brows (swept hair leaves the forehead bare); under a hat they
        // grow from below its brim or band, and a hat that comes down to the brow hides them
        if ((s.Hair is HairCut.Short or HairCut.Long or HairCut.Spiky or HairCut.Braids or HairCut.Bunches or HairCut.Ponytail) && !CoversTheBrow(s.Hat))
        {
            foreach (float x in new[] { -0.46f, -0.16f, 0.16f, 0.46f })
            {
                var tip = new Vector3(x, 0.06f + MathF.Abs(x) * 0.34f, 0.92f);
                GrownLock(m, f, hair, hatted ? new Vector3(x * 0.95f, 0.22f, 0.93f) : Whorl, tip, 0.21f, 0.085f, 0.14f);
            }
        }

        switch (s.Hair)
        {
            case HairCut.Spiky:
            {
                // Tufts standing up and swept back from the crown, long enough to stand out of the outline
                (Vector3 Dir, float Len)[] spikes =
                {
                    (new(0f, 1f, 0.2f), 0.55f), (new(0.55f, 0.8f, 0.1f), 0.48f), (new(-0.55f, 0.8f, 0.1f), 0.48f),
                    (new(0.35f, 0.72f, -0.6f), 0.5f), (new(-0.35f, 0.72f, -0.6f), 0.5f), (new(0f, 0.45f, -0.9f), 0.46f),
                    (new(0.85f, 0.35f, -0.3f), 0.38f), (new(-0.85f, 0.35f, -0.3f), 0.38f), (new(0.3f, 0.78f, 0.55f), 0.42f),
                    (new(-0.3f, 0.78f, 0.55f), 0.42f)
                };
                foreach (var (d, len) in spikes)
                {
                    if (hatted && d.Y > 0.5f) continue;
                    var dir = Vector3.Normalize(d);
                    var root = OnCap(f, dir, -0.12f);
                    var out_ = Vector3.Normalize(dir / CapRadii(f) + new Vector3(0, 0.25f, -0.35f));
                    GrownTube(m, f, root, root + out_ * (len * R), 0.24f, 0.083f, hair, HumanBones.Head, 0.04f, SurfaceMaterial.Hair);
                }
                foreach (float side in new[] { 1f, -1f })
                {
                    GrownLock(m, f, hair, Root(new Vector3(side, 0f, 0.3f)), new Vector3(side * 0.97f, -0.05f, 0.24f), 0.2f, 0.1f, 0.06f);
                    GrownLock(m, f, hair, Root(new Vector3(side, 0f, -0.4f)), new Vector3(side * 0.85f, -0.45f, -0.35f), 0.2f, 0.1f, 0.06f);
                }
                GrownLock(m, f, hair, Root(new Vector3(0f, -0.85f, -0.55f)), new Vector3(0f, -0.85f, -0.55f), 0.22f, 0.11f, 0.06f);
                break;
            }

            case HairCut.Swept:
            case HairCut.Bun:
            {
                // Combed straight back from the hairline over the crown, and back over the ears at the sides: sleek, the
                // locks lying close to the cap so only their grooves and the outline show them
                foreach (float x in new[] { -0.42f, -0.14f, 0.14f, 0.42f })
                    GrownLock(m, f, hair, new Vector3(x, 0.4f, 0.88f), new Vector3(x * 0.85f, -0.05f, -1f), 0.17f, 0.13f, 0f, 0.1f);
                GrownLock(m, f, hair, new Vector3(0f, 0.5f, 0.84f), new Vector3(0f, 0f, -1f), 0.18f, 0.14f, 0f, 0.1f);
                foreach (float side in new[] { 1f, -1f })
                {
                    GrownLock(m, f, hair, new Vector3(side * 0.82f, 0.25f, 0.5f), new Vector3(side * 0.65f, -0.4f, -0.72f), 0.16f, 0.12f, 0f, 0.1f);
                    GrownLock(m, f, hair, new Vector3(side * 0.95f, -0.02f, 0.28f), new Vector3(side * 0.7f, -0.6f, -0.5f), 0.14f, 0.11f, 0f, 0.09f);
                }
                if (s.Hair == HairCut.Bun)
                {
                    // The locks gathered into a round knot high on the back of the head, a twist round its foot
                    var back = Vector3.Normalize(new Vector3(0, 0.5f, -0.86f));
                    var knot = OnCap(f, back, 0.18f);
                    var normal = Vector3.Normalize(back / CapRadii(f));
                    m.Sphere(knot + normal * (0.12f * R), 0.38f * R, hair, HumanBones.Head, 0.06f * R, SurfaceMaterial.Hair);
                    m.Torus(knot - normal * (0.06f * R), 0.3f * R, Math.Max(GrownLeastRadius, 0.07f * R), Shade(hair, 0.86f), HumanBones.Head, 0.02f * R,
                        SurfaceMaterial.Hair, AlignY(normal));
                }
                break;
            }

            default:
            {
                // Short, or long and its kin: locks from the crown over the top, the sides and the back
                if (!hatted)
                    foreach (var to in new[] { new Vector3(0f, 0.72f, 0.7f), new Vector3(0.62f, 0.62f, 0.45f), new Vector3(-0.62f, 0.62f, 0.45f) })
                        GrownLock(m, f, hair, Whorl, to, 0.23f, 0.13f, 0.05f);
                foreach (float side in new[] { 1f, -1f })
                    foreach (var to in new[] { new Vector3(side * 0.97f, -0.05f, 0.24f), new Vector3(side * 0.88f, -0.42f, -0.35f), new Vector3(side * 0.45f, -0.78f, -0.55f) })
                        GrownLock(m, f, hair, Root(to), to, 0.21f, 0.1f, 0.05f);
                GrownLock(m, f, hair, Root(new Vector3(0f, -0.8f, -0.6f)), new Vector3(0f, -0.8f, -0.6f), 0.23f, 0.11f, 0.05f);
                // Under a hat the locks fan out from its edge and would leave gaps at the back for its band to show through
                if (hatted)
                    foreach (float side in new[] { 1f, -1f })
                        GrownLock(m, f, hair, Root(new Vector3(side * 0.25f, -0.8f, -0.6f)), new Vector3(side * 0.25f, -0.8f, -0.6f), 0.22f, 0.11f, 0.05f);
                if (s.Hair == HairCut.Long) GrownLong(m, s, f);
                if (s.Hair == HairCut.Braids) GrownBraids(m, s, f);
                if (s.Hair == HairCut.Bunches) GrownBunches(m, s, f);
                if (s.Hair == HairCut.Ponytail) GrownPonytail(m, s, f, hatted);
                break;
            }
        }
    }

    /// <summary>
    /// Long hair (style guide, "Grown characters", "Hair"): a smooth mass behind the head falling over the back to the
    /// shoulder blades on the hair's bone, locks down it that swing, and a lock framing each cheek down past the jaw.
    /// </summary>
    private static void GrownLong(SdfModel m, CharacterStyle s, GrownFrame f)
    {
        var hair = s.HairColor;
        float R = f.HeadR;
        var H = f.HeadC;
        const SurfaceMaterial Hair = SurfaceMaterial.Hair;
        // The mass behind the head and the nape, then locks from the back of the head down to the shoulder blades,
        // slanting back to clear the upper back however deep the chest is (a broad build's is further back than the head).
        // Long hair rests on the back: what is above the nape moves with the head and what lies on the back with the
        // chest, so between them it stretches as the neck does (on the hair's bone, which a bow turns past the head, its
        // outline tore away from the back)
        m.Ellipsoid(H + new Vector3(0, -0.62f, -1.0f) * R, new Vector3(0.78f, 0.8f, 0.3f) * R, hair, HumanBones.Head, 0.12f * R, Hair);
        // (the elder's rounded upper back stands further back still)
        float back = Math.Min(H.Z - 1.06f * R, -(f.ChestD * (f.Forward > 0f ? 1.15f : 1f) + 0.016f * f.Size) - 0.12f * R);
        const float Nape = -1.3f;
        foreach (float x in new[] { -0.55f, -0.18f, 0.18f, 0.55f })
        {
            // Full to their ends and close enough to join down the back, the outer two ending a little higher, so the ends
            // make one soft curve (thin, wide-set tips hung apart like fingers with the back between them)
            float end = MathF.Abs(x) > 0.3f ? -2.2f : -2.38f;
            var top = H + new Vector3(x, 0.05f, -1.02f + MathF.Abs(x) * 0.22f) * R;
            var tip = new Vector3(H.X + x * 0.98f * R, H.Y + end * R, back + MathF.Abs(x) * 0.15f * R);
            var nape = Vector3.Lerp(top, tip, (0.05f - Nape) / (0.05f - end));
            GrownTube(m, f, top, nape, 0.26f, 0.22f, hair, HumanBones.Head, 0.05f, Hair);
            GrownTube(m, f, nape, tip, 0.22f, 0.15f, hair, HumanBones.Chest, 0.05f, Hair);
        }
        // A lock framing each cheek, from over the temple down in front of the ear to below the jaw
        foreach (float side in new[] { 1f, -1f })
            GrownTube(m, f, H + new Vector3(side * 0.8f, 0.2f, 0.18f) * R, H + new Vector3(side * 0.8f, -1.1f, 0.08f) * R, 0.17f, 0.09f, hair, HumanBones.Head, 0.05f, Hair);
    }

    /// <summary>
    /// A ponytail (style guide, "Grown characters", "Hair"): a tail tied high at the back of the head in the band's
    /// colour (lower, below the hat's edge, under a hat), falling to the shoulder blades on the hair's bone.
    /// </summary>
    private static void GrownPonytail(SdfModel m, CharacterStyle s, GrownFrame f, bool hatted)
    {
        var hair = s.HairColor;
        float R = f.HeadR;
        var H = f.HeadC;
        const SurfaceMaterial Hair = SurfaceMaterial.Hair;
        var back = Vector3.Normalize(hatted ? new Vector3(0, 0.05f, -1f) : new Vector3(0, 0.45f, -0.9f));
        var tie = OnCap(f, back, 0.05f);
        var normal = Vector3.Normalize(back / CapRadii(f));
        var bend = tie + normal * (0.28f * R) + new Vector3(0, -0.3f, 0) * R;
        var tip = H + new Vector3(0, -2.2f, -1.3f) * R;
        m.Sphere(tie, 0.2f * R, hair, HumanBones.Hair, 0.06f * R, Hair);
        GrownTube(m, f, tie, bend, 0.2f, 0.22f, hair, HumanBones.Hair, 0.06f, Hair);
        GrownTube(m, f, bend, tip, 0.22f, 0.085f, hair, HumanBones.Hair, 0.06f, Hair);
        m.Torus(tie + normal * (0.08f * R), 0.17f * R, Math.Max(GrownLeastRadius, 0.07f * R), s.HatBand, HumanBones.Hair, 0.01f * R, SurfaceMaterial.Cloth, AlignY(normal));
    }

    /// <summary>
    /// Two plaits (style guide, "Grown characters", "Hair") from under the ears over the front of the shoulders down to
    /// the chest: a short tube to the shoulder, then beads one after another, each tied at its end in the band's colour
    /// with a tuft below.
    /// </summary>
    private static void GrownBraids(SdfModel m, CharacterStyle s, GrownFrame f)
    {
        var hair = s.HairColor;
        float R = f.HeadR, sz = f.Size;
        var H = f.HeadC;
        const SurfaceMaterial Hair = SurfaceMaterial.Hair;
        foreach (float side in new[] { 1f, -1f })
        {
            var root = H + new Vector3(side * 0.82f, -0.35f, -0.18f) * R;
            // In front of the shoulder, far enough out that the arm's and the neck's blends don't reach it
            var shoulder = new Vector3(side * (f.Shoulder.X * 0.55f + 0.02f * sz), f.ShoulderY + 0.03f * sz, f.ChestD * 0.55f + 0.02f * sz);
            var end = new Vector3(side * f.ChestW * 0.62f, f.ChestMidY - 0.01f * sz, f.ChestD + 0.035f * sz);
            GrownTube(m, f, root, shoulder, 0.2f, 0.18f, hair, HumanBones.Head, 0.04f, Hair);
            const int Beads = 5;
            var along = Vector3.Normalize(end - shoulder);
            for (int i = 0; i < Beads; i++)
            {
                var at = Vector3.Lerp(shoulder, end, (i + 0.5f) / Beads);
                m.Ellipsoid(at, new Vector3(0.17f * R, Vector3.Distance(shoulder, end) / Beads * 0.62f, 0.15f * R), hair, HumanBones.Head, 0.01f * R, Hair, AlignY(along));
            }
            m.Sphere(end + along * (0.08f * R), Math.Max(GrownLeastRadius, 0.11f * R), s.HatBand, HumanBones.Head, 0.01f * R, SurfaceMaterial.Cloth);
            GrownTube(m, f, end + along * (0.16f * R), end + along * (0.5f * R), 0.12f, 0.085f, hair, HumanBones.Head, 0.01f, Hair);
        }
    }

    /// <summary>
    /// Two bunches (style guide, "Grown characters", "Hair"): thick tails tied high at the sides of the head, standing
    /// out and curling down past the jaw, on the hair's bone so they swing, tied in the band's colour.
    /// </summary>
    private static void GrownBunches(SdfModel m, CharacterStyle s, GrownFrame f)
    {
        var hair = s.HairColor;
        float R = f.HeadR;
        const SurfaceMaterial Hair = SurfaceMaterial.Hair;
        foreach (float side in new[] { 1f, -1f })
        {
            // Tied at the side of the head a little behind the ear's line, the tail falling close beside the head
            var dir = new Vector3(side * 0.9f, 0.3f, -0.34f);
            var tie = OnCap(f, dir, 0.02f);
            var normal = Vector3.Normalize(Vector3.Normalize(dir) / CapRadii(f));
            var out1 = tie + normal * (0.22f * R) + new Vector3(0, -0.06f, -0.06f) * R;
            var tip = tie + normal * (0.3f * R) + new Vector3(0, -1.1f, -0.02f) * R;
            GrownTube(m, f, tie, out1, 0.2f, 0.19f, hair, HumanBones.Hair, 0.06f, Hair);
            GrownTube(m, f, out1, tip, 0.19f, 0.085f, hair, HumanBones.Hair, 0.06f, Hair);
            m.Sphere(tie + normal * (0.1f * R), Math.Max(GrownLeastRadius, 0.13f * R), s.HatBand, HumanBones.Hair, 0.02f * R, SurfaceMaterial.Cloth);
        }
    }

    /// <summary>
    /// Curls (style guide, "Grown characters", "Hair"): small round curls a fifth of the head's half height across over
    /// the cap in rings, and a row at the fringe whose lowest stands over the brows; none over the face. Under a hat only
    /// the rings below its edge show.
    /// </summary>
    private static void GrownCurls(SdfModel m, CharacterStyle s, GrownFrame f, bool hatted)
    {
        var hair = s.HairColor;
        float R = f.HeadR;
        var dirs = new List<Vector3>();
        if (!hatted) dirs.Add(new Vector3(0, 1, 0));
        foreach (var (y, count) in new[] { (0.78f, 7), (0.45f, 11), (0.08f, 13), (-0.3f, 11), (-0.62f, 8) })
        {
            if (hatted && y > 0.35f) continue;
            for (int i = 0; i < count; i++)
            {
                float a = (i + (count % 2 == 0 ? 0.5f : 0f)) / count * MathF.Tau;
                var d = new Vector3(MathF.Sin(a), y, MathF.Cos(a));
                // The face's half-circle bare below the fringe, and the cap's own edge kept round the ears and the nape
                if (d.Z > 0.25f && y < 0.4f) continue;
                if (y < -0.2f && d.Z > -0.35f) continue;
                dirs.Add(d);
            }
        }
        // The fringe: a row across the brow, its lowest curl over the brows
        foreach (float x in new[] { -0.5f, -0.17f, 0.17f, 0.5f })
            dirs.Add(new Vector3(x, 0.2f + MathF.Abs(x) * 0.15f, 0.88f));
        foreach (var d in dirs)
            m.Sphere(OnCap(f, d, -0.02f), 0.21f * R, hair, HumanBones.Head, 0.04f * R, SurfaceMaterial.Hair);
    }

    /// <summary>
    /// An afro (style guide, "Grown characters", "Hair"): a round mass over the crown, the back and the sides whose front
    /// stops at the hairline over the brow, and a crown of small round curls on its surface, never over the face.
    /// </summary>
    private static void GrownAfro(SdfModel m, CharacterStyle s, GrownFrame f)
    {
        var hair = s.HairColor;
        float R = f.HeadR;
        // Set back and up from the cap, so its front meets the hairline over the forehead and the brows stay bare
        var c = CapCentre(f) + new Vector3(0, 0.34f, -0.34f) * R;
        var radii = new Vector3(1.16f, 0.98f, 1.08f) * R;
        m.Ellipsoid(c, radii, hair, HumanBones.Head, 0.06f * R, SurfaceMaterial.Hair);
        var curls = new List<Vector3> { new(0f, 1f, 0f), new(0f, 0.75f, 0.65f), new(0.5f, 0.7f, 0.5f), new(-0.5f, 0.7f, 0.5f) };
        // Two rings round the back and the sides, leaving the face's half-circle bare
        foreach (float y in new[] { 0.6f, 0.15f, -0.3f })
            foreach (float degrees in new[] { 70f, 105f, 140f, 180f, 220f, 255f, 290f })
            {
                float a = degrees * MathF.PI / 180f;
                curls.Add(new Vector3(MathF.Sin(a), y, MathF.Cos(a)));
            }
        foreach (var d in curls)
            m.Sphere(c + Vector3.Normalize(d) * radii * 0.92f, 0.26f * R, hair, HumanBones.Head, 0.06f * R, SurfaceMaterial.Hair);
    }

    // ------------------------------------------------------------------ hats

    /// <summary>
    /// What is worn on a grown head (style guide, "Grown characters", "Hats"), each on the hair's surface, never in it:
    /// the chibi kit's fourteen at a grown head's size, their brims no further forward than the face, so the eyes show
    /// from the field's camera. A mask's hood is the cap's (<see cref="GrownHairCap"/>), with its crest here.
    /// </summary>
    private static void GrownHat(SdfModel m, CharacterStyle s, GrownFrame f)
    {
        float R = f.HeadR;
        var C = CapCentre(f);
        var H = f.HeadC;
        const SurfaceMaterial Cloth = SurfaceMaterial.Cloth, Plastic = SurfaceMaterial.Plastic;
        float thin = GrownLeastRadius;
        switch (s.Hat)
        {
            case Headwear.Beret:
            {
                // Soft and flat, worn toward the back, with a band and a little stalk on top; it covers the hair's crown
                var tilt = Pitch(-0.22f);
                var at = C + new Vector3(0, 0.62f, -0.06f) * R;
                m.Ellipsoid(at, new Vector3(1.08f, 0.44f, 1.12f) * R, s.HatColor, HumanBones.Head, 0f, Cloth, tilt);
                Band(m, f, at + Vector3.Transform(new Vector3(0, -0.26f, 0.02f) * R, tilt), 0.96f, 1.0f, 0.14f, s.HatBand, tilt, Cloth, 0.04f);
                var stalk = at + Vector3.Transform(new Vector3(0, 0.42f, -0.04f) * R, tilt);
                GrownTube(m, f, stalk, stalk + new Vector3(0, 0.16f, -0.05f) * R, 0.1f, 0.09f, Shade(s.HatColor, 0.86f), HumanBones.Head, 0.04f, Cloth);
                break;
            }
            case Headwear.Cap:
            {
                // A crown snug over the hair, its band above the brows, a button on top, and the peak out of the crown's
                // front edge over the bangs
                var tilt = Pitch(-0.14f);
                var at = C + new Vector3(0, 0.36f, 0.02f) * R;
                m.Cylinder(at, 0.98f * R, 0.5f * R, 0.45f * R, s.HatColor, HumanBones.Head, 0f, Cloth, tilt);
                Brim(m, f, H + new Vector3(0, 0.42f, 0.92f) * R, 0.72f, 0.56f, s.HatBand, Pitch(0.22f), Plastic, 0.03f);
                m.Sphere(at + Vector3.Transform(new Vector3(0, 0.5f, 0) * R, tilt), Math.Max(thin, 0.1f * R), Shade(s.HatColor, 0.88f), HumanBones.Head, 0.03f * R, Cloth);
                break;
            }
            case Headwear.Bow:
            {
                // A big ribbon at the top of the back of the head: two loops tilted up and out round a knot, its face
                // turned the way the hair's surface faces there, and two short tails falling behind
                var dir = Vector3.Normalize(new Vector3(0, 0.72f, -0.7f));
                var normal = Vector3.Normalize(dir / CapRadii(f));
                var knot = OnCap(f, dir, 0.18f);
                var lie = Quaternion.Normalize(Quaternion.Concatenate(AlignY(normal), Pitch(-0.25f)));
                var up = normal;
                foreach (float side in new[] { 1f, -1f })
                {
                    var tilt = Quaternion.Concatenate(Quaternion.CreateFromAxisAngle(Vector3.UnitZ, side * 0.35f), Pitch(0.9f));
                    m.Ellipsoid(knot + new Vector3(side * 0.42f * R, 0, 0) + up * (0.08f * R), new Vector3(0.42f, 0.24f, 0.12f) * R,
                        s.HatColor, HumanBones.Head, 0.04f * R, Cloth, tilt);
                    GrownTube(m, f, knot, knot + new Vector3(side * 0.26f, -0.55f, -0.36f) * R, 0.12f, 0.09f, s.HatBand, HumanBones.Head, 0.02f, Cloth);
                }
                m.Ellipsoid(knot + normal * (0.08f * R), new Vector3(0.2f, 0.2f, 0.15f) * R, s.HatBand, HumanBones.Head, 0f, Cloth, lie);
                break;
            }
            case Headwear.Mask:
            {
                // The hood is the cap's; its edge round the face painted crisp (the face's wide blends would smudge it),
                // and its crest in the band's colour runs from the brow over the crown to the nape
                var c = CapCentre(f);
                var radii = CapRadii(f) + new Vector3(0.1f, 0.1f, 0.1f) * R;
                m.Paint(m.Ellipsoid(c, radii + new Vector3(0.04f, 0.04f, 0.04f) * R, s.HatColor, HumanBones.Head, GrownPaintEdge), true, HumanBones.Head).Material = Cloth;
                m.Paint(m.Ellipsoid(H + new Vector3(0, -0.4f, 0.74f) * R, new Vector3(0.6f, 0.62f, 0.6f) * R, s.Skin, HumanBones.Head, GrownPaintEdge), true, HumanBones.Head).Material =
                    SurfaceMaterial.Skin;
                const int Steps = 8;
                for (int i = 0; i < Steps; i++)
                {
                    float a0 = 0.75f - i * (2.7f / Steps), a1 = 0.75f - (i + 1) * (2.7f / Steps);
                    var p0 = OnShell(c, radii, new Vector3(0, MathF.Cos(a0), MathF.Sin(a0)), -0.02f, R);
                    var p1 = OnShell(c, radii, new Vector3(0, MathF.Cos(a1), MathF.Sin(a1)), -0.02f, R);
                    GrownTube(m, f, p0, p1, 0.14f, 0.14f, s.HatBand, HumanBones.Head, 0f, Cloth);
                }
                break;
            }
            case Headwear.Fedora:
            {
                // A cap's crown, taller and rounder, a brim all round at its foot (narrower to the front, so the eyes
                // still show from the field's camera) and a band just above the brim
                var tilt = Pitch(-0.1f);
                var at = C + new Vector3(0, 0.5f, -0.02f) * R;
                m.Cylinder(at, 0.98f * R, 0.62f * R, 0.42f * R, s.HatColor, HumanBones.Head, 0f, Cloth, tilt);
                var foot = C + new Vector3(0, -0.06f, -0.02f) * R;
                Brim(m, f, foot, 1.62f, 1.5f, s.HatColor, tilt, Cloth);
                Band(m, f, foot + Vector3.Transform(new Vector3(0, 0.17f, 0) * R, tilt), 0.99f, 0.99f, 0.14f, s.HatBand, tilt, Cloth);
                break;
            }
            case Headwear.NurseCap:
            {
                // A starched band standing on the hair at the top front of the head, a red cross on its front
                var dir = Vector3.Normalize(new Vector3(0, 0.82f, 0.45f));
                var at = OnCap(f, dir, 0.22f);
                var tilt = Pitch(-0.32f);
                m.Box(at, new Vector3(0.48f * R, 0.26f * R, Math.Max(thin, 0.1f * R)), 0.09f * R, s.HatColor, HumanBones.Head, 0.04f * R, Cloth, tilt);
                var front = at + Vector3.Transform(new Vector3(0, 0.02f, 0.1f) * R, tilt);
                m.Paint(m.Box(front, new Vector3(0.05f, 0.15f, 0.1f) * R, 0f, s.HatBand, HumanBones.Head, rotation: tilt));
                m.Paint(m.Box(front, new Vector3(0.15f, 0.05f, 0.1f) * R, 0f, s.HatBand, HumanBones.Head, rotation: tilt));
                break;
            }
            case Headwear.Visor:
            {
                // A band round the brow in the band's colour and a peak at its front in the hat's; the hair shows above it
                Band(m, f, HeadBandAt(f, 0.36f), 0.97f, 1.07f, 0.13f, s.HatBand, Pitch(0.15f), Cloth, 0.02f);
                Brim(m, f, H + new Vector3(0, 0.34f, 0.96f) * R, 0.62f, 0.4f, s.HatColor, Pitch(0.24f), Plastic, 0.03f);
                break;
            }
            case Headwear.WideHat:
            {
                // A low round crown with a band at its foot and a wide brim all round, lower at the back than at the
                // front so the eyes still show from the field's camera
                var tilt = Pitch(-0.14f);
                var at = C + new Vector3(0, 0.52f, -0.04f) * R;
                m.Ellipsoid(at, new Vector3(1.02f, 0.62f, 1.12f) * R, s.HatColor, HumanBones.Head, 0f, Cloth, tilt);
                var foot = C + new Vector3(0, 0.14f, -0.06f) * R;
                Band(m, f, foot + Vector3.Transform(new Vector3(0, 0.08f, 0) * R, tilt), 0.99f, 1.09f, 0.14f, s.HatBand, tilt, Cloth);
                Brim(m, f, foot, 1.72f, 1.68f, s.HatColor, tilt, Cloth);
                break;
            }
            case Headwear.Cowboy:
            {
                // A tall crown creased along its top, a band at its foot and a wide brim curled up at the sides, no wider
                // at the front than a fedora's
                var tilt = Pitch(-0.08f);
                var at = C + new Vector3(0, 0.56f, -0.02f) * R;
                m.Cylinder(at, 0.94f * R, 0.56f * R, 0.38f * R, s.HatColor, HumanBones.Head, 0f, Cloth, tilt);
                foreach (float side in new[] { 1f, -1f })
                    GrownTube(m, f, at + Vector3.Transform(new Vector3(side * 0.28f, 0.5f, 0.5f) * R, tilt), at + Vector3.Transform(new Vector3(side * 0.28f, 0.5f, -0.55f) * R, tilt),
                        0.12f, 0.12f, Shade(s.HatColor, 1.06f), HumanBones.Head, 0.06f, Cloth);
                var foot = C + new Vector3(0, 0.02f, -0.04f) * R;
                Band(m, f, foot + Vector3.Transform(new Vector3(0, 0.12f, 0) * R, tilt), 0.95f, 0.95f, 0.14f, s.HatBand, tilt, Cloth);
                Brim(m, f, foot, 1.68f, 1.48f, s.HatColor, tilt, Cloth);
                foreach (float side in new[] { 1f, -1f })
                    GrownTube(m, f, foot + new Vector3(side * 1.45f, 0.02f, 0f) * R, foot + new Vector3(side * 1.66f, 0.32f, 0f) * R, 0.16f, 0.15f, s.HatColor, HumanBones.Head, 0.06f, Cloth);
                break;
            }
            case Headwear.Helmet:
            {
                // A smooth shell over the crown, its front edge above the brows, a stripe in the band's colour from front
                // to back over its top, and a short peak over the eyes
                var tilt = Pitch(-0.12f);
                var at = C + new Vector3(0, 0.22f, -0.02f) * R;
                m.Ellipsoid(at, new Vector3(1.04f, 0.92f, 1.16f) * R, s.HatColor, HumanBones.Head, 0f, Plastic, tilt);
                m.Paint(m.Box(at + new Vector3(0, 0.6f, 0) * R, new Vector3(0.18f, 0.6f, 1.5f) * R, 0f, s.HatBand, HumanBones.Head, GrownPaintEdge, rotation: tilt), false, HumanBones.Head);
                Brim(m, f, H + new Vector3(0, 0.44f, 0.95f) * R, 0.56f, 0.3f, s.HatColor, Pitch(0.2f), Plastic, 0.03f);
                break;
            }
            case Headwear.HardHat:
            {
                // A round crown with a ridge in the band's colour from front to back and a brim all round its foot, no
                // wider at the front than at the sides, so the eyes still show from the field's camera
                var at = C + new Vector3(0, 0.3f, -0.02f) * R;
                var radii = new Vector3(1.04f, 0.9f, 1.12f) * R;
                m.Ellipsoid(at, radii, s.HatColor, HumanBones.Head, 0f, Plastic);
                Brim(m, f, C + new Vector3(0, 0.0f, -0.02f) * R, 1.32f, 1.4f, s.HatColor, Quaternion.Identity, Plastic);
                Vector3 OnCrown(float angle) => at + new Vector3(0, MathF.Cos(angle) * radii.Y, MathF.Sin(angle) * radii.Z);
                const int Steps = 6;
                for (int i = 0; i < Steps; i++)
                {
                    float a0 = 1.15f - i * (2.3f / Steps), a1 = 1.15f - (i + 1) * (2.3f / Steps);
                    GrownTube(m, f, OnCrown(a0), OnCrown(a1), 0.13f, 0.13f, s.HatBand, HumanBones.Head, 0.03f, Plastic);
                }
                break;
            }
            case Headwear.Beanie:
            {
                // A snug knitted crown, a rolled cuff in the band's colour at its foot and a pompom on top
                var tilt = Pitch(-0.15f);
                var at = C + new Vector3(0, 0.24f, -0.04f) * R;
                m.Ellipsoid(at, new Vector3(1.0f, 0.9f, 1.12f) * R, s.HatColor, HumanBones.Head, 0f, Cloth, tilt);
                Band(m, f, at + Vector3.Transform(new Vector3(0, -0.36f, 0) * R, tilt), 0.96f, 1.08f, 0.16f, s.HatBand, tilt, Cloth, 0.05f);
                m.Sphere(at + Vector3.Transform(new Vector3(0, 0.92f, -0.08f) * R, tilt), 0.24f * R, s.HatBand, HumanBones.Head, 0.06f * R, Cloth);
                break;
            }
            case Headwear.Bandana:
            {
                // A cloth over the crown down to the brow, knotted at the back in the band's colour with two short tails
                // that swing
                var at = C + new Vector3(0, 0.16f, -0.06f) * R;
                m.Ellipsoid(at, new Vector3(1.0f, 0.9f, 1.12f) * R, s.HatColor, HumanBones.Head, 0f, Cloth);
                var knot = C + new Vector3(0, 0.0f, -1.16f) * R;
                m.Sphere(knot, 0.17f * R, s.HatBand, HumanBones.Head, 0.03f * R, Cloth);
                foreach (float side in new[] { 1f, -1f })
                    GrownTube(m, f, knot, knot + new Vector3(side * 0.28f, -0.6f, -0.25f) * R, 0.12f, 0.09f, s.HatBand, HumanBones.Hair, 0.03f, Cloth);
                break;
            }
            case Headwear.Headband:
            {
                // A band round the brow over the bangs, lower at the front than at the back, and a knot at the back with
                // two tails in the band's colour that hang and swing; the hair shows above it as it is
                Band(m, f, HeadBandAt(f, 0.42f), 0.97f, 1.07f, 0.14f, s.HatColor, Pitch(0.18f), Cloth, 0.02f);
                var knot = HeadBandAt(f, 0.42f) + new Vector3(0, 0.2f, -1.05f) * R;
                m.Sphere(knot, 0.14f * R, s.HatBand, HumanBones.Head, 0.03f * R, Cloth);
                foreach (float side in new[] { 1f, -1f })
                    GrownTube(m, f, knot, knot + new Vector3(side * 0.22f, -0.75f, -0.3f) * R, 0.11f, 0.085f, s.HatBand, HumanBones.Hair, 0.03f, Cloth);
                break;
            }
        }
    }

    /// <summary>
    /// What grows on a grown face (style guide, "Grown characters", "Faces"), in the hair's colour: a moustache, two
    /// tapered masses under the nose over the upper lip; a full beard round the chin and the jaw below the mouth with a
    /// sideburn up each side to the ear, never over the mouth or the cheeks; bushy brows, two ridges over the eyes where
    /// the painted brows would be.
    /// </summary>
    private static void GrownFacialHair(SdfModel m, CharacterStyle s, GrownFrame f)
    {
        var hair = s.HairColor;
        float R = f.HeadR;
        var H = f.HeadC;
        const SurfaceMaterial Hair = SurfaceMaterial.Hair;
        if (s.Mustache)
            foreach (float side in new[] { 1f, -1f })
                m.Ellipsoid(H + new Vector3(side * 0.13f, -0.51f, 0.78f) * R, new Vector3(0.17f, 0.075f, 0.09f) * R, hair, HumanBones.Head, 0.04f * R, Hair,
                    Quaternion.Concatenate(Quaternion.CreateFromAxisAngle(Vector3.UnitY, side * 0.35f), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, side * -0.32f)));
        if (s.Beard)
        {
            // Round the chin and the jaw below the mouth, and up each side to the ear
            m.Ellipsoid(H + new Vector3(0, -0.92f, 0.5f) * R, new Vector3(0.32f, 0.2f, 0.22f) * R, hair, HumanBones.Head, 0.08f * R, Hair);
            foreach (float side in new[] { 1f, -1f })
            {
                GrownTube(m, f, H + new Vector3(side * 0.62f, -0.1f, -0.1f) * R, H + new Vector3(side * 0.5f, -0.7f, 0.0f) * R, 0.14f, 0.16f, hair, HumanBones.Head, 0.06f, Hair);
                GrownTube(m, f, H + new Vector3(side * 0.5f, -0.7f, 0.0f) * R, H + new Vector3(side * 0.2f, -0.94f, 0.46f) * R, 0.16f, 0.18f, hair, HumanBones.Head, 0.06f, Hair);
            }
        }
        if (s.BushyBrows)
            foreach (float side in new[] { 1f, -1f })
                m.Ellipsoid(H + new Vector3(side * 0.3f, 0.2f, 0.74f) * R, new Vector3(0.19f, 0.06f, 0.08f) * R, hair, HumanBones.Head, 0.03f * R, Hair,
                    Quaternion.Concatenate(Quaternion.CreateFromAxisAngle(Vector3.UnitY, side * 0.3f), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, side * 0.12f)));
    }

    /// <summary>Rosy cheeks (style guide, "Characters"), painted on the grown face's cheeks below the eyes' outer corners.</summary>
    private static void GrownBlush(SdfModel m, CharacterStyle s, GrownFrame f)
    {
        if (!s.Blush) return;
        float R = f.HeadR;
        var H = f.HeadC;
        var blush = PixelCanvas.Mix(s.Skin, new Color(250, 120, 130, 255), 0.26f);
        foreach (float side in new[] { 1f, -1f })
            m.Paint(m.Ellipsoid(H + new Vector3(side * 0.42f, -0.28f, 0.6f) * R, new Vector3(0.16f, 0.09f, 0.3f) * R, blush, HumanBones.Head, 0.04f * R), false, HumanBones.Head);
    }
}
