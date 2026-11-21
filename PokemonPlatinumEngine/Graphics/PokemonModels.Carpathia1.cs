using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Carpathia, batch 1 (plan 46 · CP4 to CP8): Lynkit to Aricorn (20 species), sculpted from nothing after the picture briefs
// of docs/plans/decisions/CP-dex-1-plateau.md and the user's reference pictures. Every builder and helper here starts
// with Cp1, so nothing meets another batch's names in the one partial class.
internal static partial class PokemonModels
{
    private static string[] CarpathiaNames1() => new[]
    {
        "Lynkit", "Firlynx", "Codrulynx", "Salamint", "Furnander", "Fierander", "Pelicub", "Pelireed", "Buciumbill",
        "Trillet", "Nightrill", "Privigale", "Harciog", "Harciogre", "Chirpet", "Fiddlecrick", "Buburel", "Buburoza",
        "Arichip", "Aricorn"
    };

    private static PokeBuilder? CreateCarpathia1(string species) => species switch
    {
        "LYNKIT" => Cp1Lynx(0),
        "FIRLYNX" => Cp1Lynx(1),
        "CODRULYNX" => Cp1Lynx(2),
        "SALAMINT" => Cp1Salamint(),
        "FURNANDER" => Cp1Hearth(false),
        "FIERANDER" => Cp1Hearth(true),
        "PELICUB" => Cp1Pelicub(),
        "PELIREED" => Cp1Pelican(false),
        "BUCIUMBILL" => Cp1Pelican(true),
        "TRILLET" => Cp1Trillet(),
        "NIGHTRILL" => Cp1Nightrill(),
        "PRIVIGALE" => Cp1Privigale(),
        "HARCIOG" => Cp1Hamster(false),
        "HARCIOGRE" => Cp1Hamster(true),
        "CHIRPET" => Cp1Chirpet(),
        "FIDDLECRICK" => Cp1Fiddlecrick(),
        "BUBUREL" => Cp1Buburel(),
        "BUBUROZA" => Cp1Buburoza(),
        "ARICHIP" => Cp1Hedgehog(false),
        "ARICORN" => Cp1Hedgehog(true),
        _ => null
    };

    // ------------------------------------------------------------------ helpers of the batch

    /// <summary>A way across <paramref name="dir"/>: never nothing, whatever way <paramref name="dir"/> points.</summary>
    private static Vector3 Cp1Across(Vector3 dir)
    {
        var d = Vector3.Normalize(dir);
        var hint = MathF.Abs(d.Z) > 0.85f ? V(1f, 0, 0) : V(0, 0, 1f);
        return Vector3.Normalize(Vector3.Cross(d, hint));
    }

    /// <summary>A sprig of fir from <paramref name="root"/> to <paramref name="tip"/>: a twig and pairs of needles along it, swept toward the tip.</summary>
    private static void Cp1Sprig(PokeBuilder b, int bone, Vector3 root, Vector3 tip, float r, Color color, int pairs, float needle)
    {
        b.Limb(bone, root, tip, r, r * 0.6f, color, Leaf, 0.006f);
        var d = Vector3.Normalize(tip - root);
        var side = Cp1Across(d);
        var up = Vector3.Normalize(Vector3.Cross(side, d));
        for (int i = 1; i <= pairs; i++)
        {
            float t = 0.15f + 0.8f * i / (pairs + 1);
            var at = Vector3.Lerp(root, tip, t);
            float len = needle * (1.1f - 0.5f * t);
            foreach (float s in new[] { -1f, 1f })
            {
                var way = Vector3.Normalize(side * s * 0.8f + d * 0.55f + up * 0.25f);
                b.Spike(bone, at, at + way * len, r * 0.75f, color, mat: Leaf, blend: 0.004f);
            }
        }
        b.Spike(bone, tip - d * r, tip + d * needle * 0.6f, r * 0.75f, color, mat: Leaf, blend: 0.004f);
    }

    /// <summary>
    /// Leaves laid over the top of an ellipsoid like a hedgehog's spines, spread by the golden angle: each from a root in its
    /// surface, lying back along it, flat to it. The front within <paramref name="face"/> stays bare for the face.
    /// </summary>
    private static void Cp1LeafCoat(PokeBuilder b, int bone, Vector3 center, Vector3 radii, int count, float length, float width, Color[] colors,
        float face = 0.55f, float below = -0.15f, uint seed = 1)
    {
        var rng = new GenomeRandom(seed);
        float golden = MathF.PI * (3f - MathF.Sqrt(5f));
        for (int i = 0; i < count; i++)
        {
            float y = 1f - 2f * (i + 0.5f) / count;
            if (y < below) continue;
            float ring = MathF.Sqrt(1f - y * y), a = i * golden;
            var dir = V(MathF.Sin(a) * ring, y, MathF.Cos(a) * ring);
            if (dir.Z > face) continue;
            var root = center + dir * radii * 0.92f;
            var n = Vector3.Normalize(dir / radii);
            // Back along the surface: away from the face, a little toward the ground at the sides
            var back = V(0, 0, -1f) - n * Vector3.Dot(V(0, 0, -1f), n);
            if (back.LengthSquared() < 0.09f) back = V(0, 1f, 0) - n * Vector3.Dot(V(0, 1f, 0), n);
            back = Vector3.Normalize(back);
            var way = Vector3.Normalize(back + n * 0.6f);
            float len = length * (0.85f + 0.3f * rng.Float());
            var color = colors[(int)(rng.Float() * colors.Length) % colors.Length];
            PointedLeaf(b, bone, root, root + way * len, width, color, n, 0.2f);
        }
    }

    /// <summary>A fruit on the leaves: a round body, a dimple of its colour and a short brown stalk.</summary>
    private static void Cp1Fruit(PokeBuilder b, int bone, Vector3 at, Vector3 up, float r, Color color, bool pear = false)
    {
        var u = Vector3.Normalize(up);
        if (pear)
        {
            b.Ell(bone, at, V(r, r, r), color, mat: Shell, blend: 0.006f);
            b.Ell(bone, at + u * r * 0.9f, V(r * 0.62f, r * 0.75f, r * 0.62f), color, Euler(u), Shell, 0.02f);
        }
        else
            b.Ell(bone, at, V(r, r * 0.92f, r), color, Euler(u), Shell, 0.006f);
        var top = at + u * (pear ? r * 1.55f : r * 0.85f);
        b.Limb(bone, top, top + u * r * 0.5f + Cp1Across(u) * r * 0.15f, r * 0.12f, r * 0.1f, Rgb(110, 74, 44), blend: 0.004f);
    }

    /// <summary>A four-legged beast's legs, with a cuff of tufts at each ankle if one is given and claws if asked.</summary>
    private static void Cp1Legs(PokeBuilder b, float x, float hip, float front, float back, float thigh, Color coat, Color paw, Color? cuff = null,
        bool claws = false)
    {
        foreach (var (z, isFront) in new[] { (front, true), (back, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(x * s, hip, z), isFront);
                var knee = V(x * 1.04f * s, hip * 0.5f, z + (isFront ? 0.012f : -0.03f));
                var ankle = V(x * 1.06f * s, thigh * 0.9f, z + 0.01f);
                b.Limb(leg, V(x * s, hip + 0.01f, z), knee, thigh, thigh * 0.75f, coat);
                b.Limb(leg, knee, ankle, thigh * 0.72f, thigh * 0.66f, coat);
                var pawAt = V(x * 1.06f * s, thigh * 0.55f, z + thigh * 0.45f);
                b.Ell(leg, pawAt, V(thigh * 0.95f, thigh * 0.55f, thigh * 1.2f), paw);
                foreach (float t in new[] { -1f, 0f, 1f })
                {
                    b.PaintEll(leg, pawAt + V(t * thigh * 0.36f, -thigh * 0.05f, thigh * 1.1f), V(0.004f, thigh * 0.4f, thigh * 0.25f), PixelCanvas.Mix(paw, Black, 0.45f), soft: 0.004f);
                    if (claws)
                        b.Spike(leg, pawAt + V(t * thigh * 0.4f, -thigh * 0.2f, thigh * 1.05f), pawAt + V(t * thigh * 0.45f, -thigh * 0.45f, thigh * 1.5f),
                            thigh * 0.14f, Rgb(214, 208, 190), mat: Shell, blend: 0.004f);
                }
                if (cuff is Color tuft)
                    for (int i = 0; i < 7; i++)
                    {
                        float a = i * MathF.Tau / 7f;
                        var o = V(MathF.Sin(a), 0, MathF.Cos(a));
                        var root = ankle + V(0, thigh * 0.5f, 0) + o * thigh * 0.55f;
                        b.Spike(leg, root, root + o * thigh * 0.55f + V(0, -thigh * 0.55f, 0), thigh * 0.3f, tuft, 0.6f, Leaf, 0.008f);
                    }
            });
    }

    // ------------------------------------------------------------------ Lynkit, Firlynx and Codrulynx

    /// <summary>
    /// The fir lynxes: Lynkit a round moss-green kitten spotted darker, cream paws too big for it and fir needles for ear
    /// tufts; Firlynx a lean young lynx barred brown like bark, moss at its paws; Codrulynx the old forest's guardian, dark
    /// green barred near-black, a mane of fir boughs over its shoulders, lichen at its chin and eyes of lantern gold.
    /// </summary>
    private static PokeBuilder Cp1Lynx(int stage)
    {
        string name = stage switch { 0 => "Lynkit", 1 => "Firlynx", _ => "Codrulynx" };
        float hip = stage switch { 0 => 0.15f, 1 => 0.3f, _ => 0.42f };
        var bc = stage switch { 0 => V(0, 0.22f, -0.03f), 1 => V(0, 0.38f, -0.04f), _ => V(0, 0.54f, -0.05f) };
        var br = stage switch { 0 => V(0.13f, 0.11f, 0.16f), 1 => V(0.12f, 0.11f, 0.25f), _ => V(0.19f, 0.17f, 0.35f) };
        var b = new PokeBuilder(name, stage switch { 0 => 0.56f, 1 => 0.78f, _ => 0.96f }, BodyPlan.Quadruped, bc) { Coat = Fur };
        var green = stage switch { 0 => Rgb(136, 178, 100), 1 => Rgb(146, 184, 106), _ => Rgb(66, 100, 68) };
        var dark = stage switch { 0 => Rgb(84, 126, 70), 1 => Rgb(120, 92, 62), _ => Rgb(32, 44, 36) };
        var cream = stage == 2 ? Rgb(150, 146, 122) : Rgb(240, 232, 198);
        var moss = stage switch { 0 => Rgb(190, 214, 118), 1 => Rgb(96, 150, 72), _ => Rgb(104, 136, 66) };
        var fir = stage == 2 ? Rgb(34, 70, 46) : Rgb(52, 98, 58);

        float x = stage switch { 0 => 0.07f, 1 => 0.075f, _ => 0.12f };
        float zf = stage switch { 0 => 0.09f, 1 => 0.17f, _ => 0.24f };
        float zb = stage switch { 0 => -0.11f, 1 => -0.2f, _ => -0.3f };
        float thigh = stage switch { 0 => 0.042f, 1 => 0.04f, _ => 0.068f };
        Cp1Legs(b, x, hip, zf, zb, thigh, green, stage == 2 ? green : cream, stage == 0 ? null : moss, stage == 2);

        b.Ell(Body, bc, br, green);
        b.PaintEll(Body, bc + V(0, -br.Y * 0.55f, br.Z * 0.15f), V(br.X * 0.8f, br.Y * 0.55f, br.Z * 0.85f), cream);
        if (stage == 0)
        {
            // Darker spots over its back and flanks
            foreach (var d in new[] { V(-0.6f, 0.6f, 0.1f), V(0.6f, 0.6f, 0.1f), V(-0.35f, 0.85f, -0.3f), V(0.35f, 0.85f, -0.3f), V(-0.8f, 0.3f, -0.45f),
                         V(0.8f, 0.3f, -0.45f), V(0, 1f, 0.15f), V(-0.55f, 0.7f, -0.7f), V(0.55f, 0.7f, -0.7f) })
            {
                var at = Out(bc, br, default, d);
                b.Mark(Body, at, Outward(bc, br, at), 0.018f, 0.014f, dark);
            }
        }
        else
            // Bars of bark down the flanks
            Stripes(b, Body, bc, br, dark, stage == 1 ? 0.016f : 0.022f, 18f,
                stage == 1 ? new[] { 0.14f, 0.06f, -0.02f, -0.1f, -0.18f, -0.26f } : new[] { 0.2f, 0.09f, -0.02f, -0.13f, -0.24f, -0.35f });

        // Tail: a stub with a black tip, longer and held low as it grows
        int tail = b.Tail(bc + V(0, br.Y * 0.4f, -br.Z * 0.9f));
        var tr = bc + V(0, br.Y * 0.4f, -br.Z * 0.92f);
        var tt = stage switch { 0 => tr + V(0, 0.08f, -0.08f), 1 => tr + V(0, -0.02f, -0.2f), _ => tr + V(0, 0.06f, -0.3f) };
        float tw = stage switch { 0 => 0.045f, 1 => 0.03f, _ => 0.05f };
        b.Limb(tail, tr, tt, tw, tw * 1.05f, green);
        b.PaintEll(tail, tt, V(tw * 1.6f, tw * 1.6f, tw * 1.6f), stage == 2 ? Rgb(24, 34, 28) : Rgb(40, 38, 40));
        if (stage == 2)
            for (int i = 0; i < 5; i++)
            {
                float a = (i - 2) * 0.5f;
                b.Spike(tail, tt, tt + V(MathF.Sin(a) * 0.05f, 0.05f + MathF.Cos(a) * 0.02f, -0.06f), 0.02f, Rgb(24, 34, 28), 0.6f, blend: 0.01f);
            }

        // A neck up to the head for the grown two
        var hc = stage switch { 0 => V(0, 0.36f, 0.14f), 1 => V(0, 0.55f, 0.25f), _ => V(0, 0.77f, 0.34f) };
        var hr = stage switch { 0 => V(0.13f, 0.12f, 0.12f), 1 => V(0.1f, 0.095f, 0.105f), _ => V(0.15f, 0.13f, 0.15f) };
        if (stage > 0) b.Limb(Body, bc + V(0, br.Y * 0.3f, br.Z * 0.7f), hc + V(0, -hr.Y * 0.5f, -hr.Z * 0.3f), br.X * 0.8f, hr.X * 0.8f, green);

        int head = b.Head(hc + V(0, -hr.Y * 0.6f, -hr.Z * 0.4f));
        b.Ell(head, hc, hr, green);
        // A muzzle and chin of cream, a small dark nose
        var mc = hc + V(0, -hr.Y * 0.42f, hr.Z * 0.62f);
        b.Ell(head, mc, V(hr.X * 0.55f, hr.Y * 0.42f, hr.Z * 0.45f), cream);
        b.Mark(head, mc + V(0, hr.Y * 0.18f, hr.Z * 0.42f), V(0, 0.4f, 1f), hr.X * 0.14f, hr.X * 0.1f, Rgb(70, 56, 52), MarkShape.Triangle, 180f);
        // Spots between the ears on the kitten, bars on the brows after
        if (stage == 0)
            foreach (var d in new[] { V(-0.25f, 0.8f, 0.55f), V(0.25f, 0.8f, 0.55f), V(0, 0.9f, 0.4f) })
            {
                var at = Out(hc, hr, default, d);
                b.Mark(head, at, Outward(hc, hr, at), 0.014f, 0.01f, dark);
            }

        // The cheek ruff: pale moss on the kitten, fuller and darker as it grows, a beard of lichen on Codrulynx
        int tufts = stage switch { 0 => 9, 1 => 11, _ => 13 };
        for (int i = 0; i < tufts; i++)
        {
            float a = (-1f + 2f * i / (tufts - 1)) * 1.15f;
            var o = V(MathF.Sin(a), -MathF.Cos(a) * 0.55f - 0.1f, 0);
            var root = hc + V(o.X * hr.X * 0.85f, o.Y * hr.Y * 1.1f, hr.Z * 0.05f);
            float len = hr.X * (stage switch { 0 => 0.6f, 1 => 0.75f, _ => 0.9f });
            b.Spike(head, root, root + Vector3.Normalize(o + V(0, -0.35f, -0.25f)) * len, hr.X * 0.32f, moss, 0.6f, blend: 0.012f);
        }
        if (stage == 2)
            foreach (var (dx, len) in new[] { (-0.05f, 0.11f), (0f, 0.15f), (0.05f, 0.11f), (-0.025f, 0.13f), (0.025f, 0.13f) })
            {
                var root = mc + V(dx, -hr.Y * 0.25f, -0.01f);
                b.Spike(head, root, root + V(dx * 0.4f, -len, 0.02f), 0.018f, Rgb(170, 186, 140), 0.6f, blend: 0.008f);
            }
        if (stage == 2)
        {
            // The mane of fir boughs, swept back over its shoulders and down its back
            for (int i = 0; i < 9; i++)
            {
                float t = i / 8f;
                foreach (float s in new[] { -1f, 1f })
                {
                    if (i == 8 && s > 0) continue;
                    var root = bc + V(s * (0.06f + 0.05f * MathF.Sin(t * 3f)), br.Y * 0.85f, br.Z * (0.75f - t * 0.9f));
                    var tip = root + V(s * 0.12f, 0.06f - t * 0.04f, -0.2f);
                    Cp1Sprig(b, Body, root, tip, 0.016f, fir, 3, 0.07f);
                }
            }
            foreach (float s in new[] { -1f, 0f, 1f })
            {
                var root = hc + V(s * 0.08f, hr.Y * 0.55f, -hr.Z * 0.6f);
                Cp1Sprig(b, head, root, root + V(s * 0.06f, 0.04f, -0.2f), 0.015f, fir, 3, 0.06f);
            }
        }

        // Ears with tufts of fir: a bunch of needles on the kitten, a sprig after, a tall fir top on Codrulynx
        PokeBuilder.Both(s =>
        {
            var root = hc + V(hr.X * 0.55f * s, hr.Y * 0.6f, -hr.Z * 0.05f);
            var tip = root + V(hr.X * 0.35f * s, hr.Y * (stage == 0 ? 1.0f : 1.15f), -0.02f);
            int ear = b.Ear(head, s, root);
            CatEar(b, ear, root, tip, hr.X * 0.42f, green, stage == 2 ? Rgb(110, 116, 96) : cream);
            float tuft = stage switch { 0 => 0.07f, 1 => 0.11f, _ => 0.17f };
            Cp1Sprig(b, ear, tip - V(0, 0.01f, 0), tip + V(0.02f * s, tuft, 0), stage == 0 ? 0.008f : 0.009f, fir, stage == 0 ? 2 : 3, tuft * 0.5f);
        });

        PokeBuilder.Both(s =>
        {
            var look = V(0.5f * s, 0.12f, 1f);
            var at = Out(hc, hr, default, V(0.42f * s, 0.15f, 1f));
            float size = stage switch { 0 => 0.042f, 1 => 0.028f, _ => 0.034f };
            b.Eye(head, at, look, size, stage == 2 ? Rgb(244, 206, 70) : Rgb(214, 136, 52), glare: stage == 2);
        });
        return b;
    }

    // ------------------------------------------------------------------ Salamint, Furnander and Fierander

    /// <summary>A coal of the hearth at the tip of a tail: a lumpy black-brown cinder with embers glowing in its cracks.</summary>
    private static void Cp1Coal(PokeBuilder b, int bone, Vector3 at, float r)
    {
        b.Ell(bone, at, V(r, r * 0.92f, r), Rgb(70, 40, 32), mat: Shell, blend: 0.01f);
        foreach (var d in new[] { V(0.7f, 0.4f, 0.3f), V(-0.6f, 0.5f, -0.4f), V(0.1f, -0.6f, 0.7f), V(-0.2f, 0.7f, 0.6f), V(0.5f, -0.3f, -0.7f) })
            b.Ell(bone, at + Vector3.Normalize(d) * r * 0.75f, V(r * 0.38f, r * 0.38f, r * 0.38f), Rgb(84, 46, 36), mat: Shell, blend: 0.006f);
        foreach (var d in new[] { V(0.3f, 0.8f, 0.5f), V(-0.7f, 0.2f, 0.5f), V(0.6f, 0.1f, -0.7f) })
            b.PaintEll(bone, at + Vector3.Normalize(d) * r, V(r * 0.3f, r * 0.3f, r * 0.3f), Rgb(248, 132, 40));
    }

    /// <summary>Salamint: a chubby fire salamander, glossy black blotched yellow-orange, with a wide smile and a coal on its tail.</summary>
    private static PokeBuilder Cp1Salamint()
    {
        var b = new PokeBuilder("Salamint", 0.5f, BodyPlan.Quadruped, V(0, 0.14f, -0.03f)) { Coat = Scales };
        var black = Rgb(42, 40, 46);
        var gold = Rgb(250, 182, 40);
        StubbyLegs(b, 0.1f, 0.1f, 0.08f, -0.12f, 0.038f, black);
        var bc = V(0, 0.15f, -0.03f);
        var br = V(0.13f, 0.1f, 0.17f);
        b.Ell(Body, bc, br, black);
        foreach (var d in new[] { V(-0.5f, 0.8f, 0.2f), V(0.5f, 0.8f, 0.2f), V(-0.3f, 0.9f, -0.35f), V(0.35f, 0.85f, -0.4f), V(-0.85f, 0.35f, -0.1f),
                     V(0.85f, 0.35f, -0.15f), V(0.1f, 1f, -0.05f), V(-0.6f, 0.6f, -0.75f), V(0.6f, 0.55f, -0.7f) })
            b.PaintEll(Body, Out(bc, br, default, d), V(0.026f, 0.022f, 0.026f), gold, soft: 0.008f);

        // The tail tapers up and over, a coal glowing at its tip
        int tail = b.Tail(bc + V(0, 0.02f, -br.Z * 0.9f));
        var t0 = bc + V(0, 0.02f, -br.Z * 0.9f);
        var curve = Smooth(3, t0, t0 + V(0, 0.02f, -0.12f), t0 + V(0, 0.1f, -0.2f), t0 + V(0, 0.2f, -0.2f));
        b.Tube(tail, curve, 0.05f, 0.022f, black, Scales, 0.012f);
        b.PaintEll(tail, t0 + V(0, 0.07f, -0.17f), V(0.02f, 0.02f, 0.02f), gold, soft: 0.006f);
        Cp1Coal(b, tail, t0 + V(0, 0.24f, -0.2f), 0.04f);

        int head = b.Head(V(0, 0.18f, 0.1f));
        var hc = V(0, 0.2f, 0.16f);
        var hr = V(0.13f, 0.1f, 0.11f);
        b.Ell(head, hc, hr, black);
        foreach (var d in new[] { V(-0.45f, 0.8f, 0.0f), V(0.45f, 0.8f, -0.05f), V(0, 0.95f, -0.3f), V(-0.85f, 0.2f, -0.2f), V(0.85f, 0.25f, -0.2f) })
            b.PaintEll(head, Out(hc, hr, default, d), V(0.022f, 0.02f, 0.022f), gold, soft: 0.008f);
        // The wide smile and two nostrils
        b.Mark(head, Out(hc, hr, default, V(0, -0.38f, 1f)), V(0, -0.2f, 1f), 0.08f, 0.03f, Rgb(16, 16, 20), MarkShape.Smile);
        PokeBuilder.Both(s => b.Mark(head, Out(hc, hr, default, V(0.12f * s, 0.05f, 1f)), V(0.1f * s, 0.1f, 1f), 0.007f, 0.005f, Rgb(16, 16, 20)));
        PokeBuilder.Both(s =>
        {
            var at = Out(hc, hr, default, V(0.55f * s, 0.45f, 0.8f));
            b.Eye(head, at, Outward(hc, hr, at), 0.032f, Rgb(92, 56, 40));
        });
        return b;
    }

    /// <summary>
    /// The hearth salamanders, standing half upright: Furnander armoured in soot-black scales like the bricks of a hearth,
    /// orange cracks glowing between them, vents along its back and a smith's apron of skin; Fierander the village smith,
    /// an anvil for a head, iron plates riveted over it and a hammer of iron at its tail's end.
    /// </summary>
    private static PokeBuilder Cp1Hearth(bool smith)
    {
        float k = smith ? 1.25f : 1f;
        var b = new PokeBuilder(smith ? "Fierander" : "Furnander", smith ? 0.98f : 0.78f, BodyPlan.Biped, V(0, 0.46f, 0) * k) { Coat = Scales };
        var black = smith ? Rgb(52, 50, 56) : Rgb(48, 44, 46);
        var brick = smith ? Rgb(66, 62, 66) : Rgb(64, 56, 56);
        var glow = Rgb(250, 140, 40);
        var leather = Rgb(124, 82, 52);
        var iron = Rgb(132, 136, 144);
        var rivet = Rgb(172, 176, 184);

        // Thick bent legs and broad clawed feet
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.13f * s, 0.32f, -0.02f) * k);
            b.Limb(leg, V(0.13f * s, 0.34f, -0.02f) * k, V(0.17f * s, 0.18f, 0.06f) * k, 0.085f * k, 0.065f * k, black);
            b.Limb(leg, V(0.17f * s, 0.18f, 0.06f) * k, V(0.17f * s, 0.06f, 0.02f) * k, 0.06f * k, 0.05f * k, black);
            b.Ell(leg, V(0.17f * s, 0.035f, 0.07f) * k, V(0.07f, 0.035f, 0.1f) * k, black);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(leg, V(0.17f * s + t * 0.035f, 0.025f, 0.15f) * k, V(0.17f * s + t * 0.045f, 0.01f, 0.2f) * k, 0.012f * k, Rgb(200, 196, 186), mat: Shell, blend: 0.004f);
            if (smith)
            {
                b.Ell(leg, V(0.17f * s, 0.26f, 0.06f) * k, V(0.09f, 0.08f, 0.08f) * k, iron, V(0, 0, 10f * s), Metal, 0.008f);
                b.Ell(leg, V(0.2f * s, 0.27f, 0.12f) * k, V(0.016f, 0.016f, 0.016f) * k, rivet, mat: Metal, blend: 0.004f);
            }
        });

        // The body leans forward, a belly of brick-dark scales
        var bc = V(0, 0.48f, 0.0f) * k;
        var br = V(0.17f, 0.24f, 0.15f) * k;
        b.Ell(Body, bc, br, black, V(12f, 0, 0));
        b.Ell(Body, bc + V(0, -0.14f, -0.04f) * k, V(0.18f, 0.14f, 0.17f) * k, black);
        // Rows of scales down its back and the cracks glowing between them
        for (int row = 0; row < 4; row++)
            foreach (float s in new[] { -1f, 1f })
            {
                var at = bc + V(s * 0.08f * k, (0.14f - row * 0.1f) * k, -br.Z * 0.85f + row * 0.01f);
                b.Box(Body, at, V(0.05f, 0.035f, 0.03f) * k, 0.015f * k, brick, V(-10f, 0, 0), Scales, 0.012f);
            }
        foreach (var (x, y, z) in new[] { (-0.14f, 0.6f, -0.02f), (0.14f, 0.56f, 0.02f), (-0.15f, 0.42f, -0.06f), (0.16f, 0.4f, -0.04f), (0f, 0.6f, -0.15f) })
        {
            var at = V(x, y, z) * k;
            var n = Vector3.Normalize(V(x, 0.15f, z - 0.05f));
            b.Mark(Body, at + n * 0.02f * k, n, 0.04f * k, 0.035f * k, glow, MarkShape.Zigzag, x * 300f);
        }
        // Vents along the back
        foreach (float y in new[] { 0.66f, 0.56f, 0.46f })
        {
            var at = V(0.05f, y, -0.14f) * k;
            b.Torus(Body, at, 0.025f * k, 0.01f * k, brick, V(80f, 0, 0), mat: Scales, blend: 0.006f);
            b.PaintEll(Body, at + V(0, 0, -0.01f) * k, V(0.018f, 0.018f, 0.02f) * k, Rgb(200, 90, 30), soft: 0.004f);
        }

        // The apron over its chest and belly, and its straps for the smith
        var apron = bc + V(0, -0.05f, br.Z * 0.85f);
        b.Box(Body, apron, V(0.13f, 0.2f, 0.018f) * k, 0.02f * k, leather, V(-12f, 0, 0), Fur, 0.01f);
        b.Box(Body, apron + V(0, -0.24f, 0.03f) * k, V(0.11f, 0.06f, 0.016f) * k, 0.02f * k, leather, V(8f, 0, 0), Fur, 0.01f);
        if (smith)
            PokeBuilder.Both(s => b.Limb(Body, apron + V(0.1f * s, 0.18f, -0.02f) * k, bc + V(0.08f * s, 0.24f, -0.1f) * k, 0.012f * k, 0.012f * k, Rgb(98, 64, 42), blend: 0.004f));

        // Forearms: strong, with claws; plates of iron on the smith's shoulders and forearms
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.17f * s, 0.62f, 0.04f) * k);
            var elbow = V(0.25f * s, 0.48f, 0.1f) * k;
            var hand = V(0.22f * s, 0.38f, 0.2f) * k;
            b.Limb(arm, V(0.16f * s, 0.62f, 0.04f) * k, elbow, 0.06f * k, 0.05f * k, black);
            b.Limb(arm, elbow, hand, 0.05f * k, 0.045f * k, black);
            b.Ell(arm, hand + V(0, -0.02f, 0.02f) * k, V(0.05f, 0.045f, 0.055f) * k, black);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(arm, hand + V(t * 0.025f, -0.04f, 0.05f) * k, hand + V(t * 0.03f, -0.08f, 0.08f) * k, 0.01f * k, Rgb(200, 196, 186), mat: Shell, blend: 0.004f);
            if (smith)
            {
                b.Ell(arm, V(0.19f * s, 0.65f, 0.03f) * k, V(0.1f, 0.06f, 0.09f) * k, iron, V(0, 0, -25f * s), Metal, 0.008f);
                b.Limb(arm, Vector3.Lerp(elbow, hand, 0.15f), Vector3.Lerp(elbow, hand, 0.75f), 0.062f * k, 0.058f * k, iron, Metal, 0.006f);
                foreach (var at in new[] { V(0.22f * s, 0.7f, 0.08f) * k, Vector3.Lerp(elbow, hand, 0.45f) + V(0.05f * s, 0.01f, 0.03f) * k })
                    b.Ell(arm, at, V(0.014f, 0.014f, 0.014f) * k, rivet, mat: Metal, blend: 0.004f);
            }
            else
                b.Mark(arm, elbow + V(0.04f * s, 0.02f, 0.02f) * k, V(s, 0.2f, 0.4f), 0.03f * k, 0.03f * k, glow, MarkShape.Zigzag);
        });

        // A long tail curving along the ground and up, a coal at its tip, or the smith's hammer of iron
        int tail = b.Tail(bc + V(0, -0.2f, -0.12f) * k);
        var t0 = bc + V(0, -0.22f, -0.12f) * k;
        var points = Smooth(3, t0, t0 + V(0, -0.14f, -0.18f) * k, t0 + V(0, -0.18f, -0.42f) * k, t0 + V(0, -0.05f, -0.6f) * k, t0 + V(0, 0.18f, -0.62f) * k);
        b.Tube(tail, points, 0.09f * k, 0.04f * k, black, Scales, 0.012f);
        foreach (float t in new[] { 0.3f, 0.5f, 0.7f })
        {
            var at = points[(int)(t * (points.Length - 1))];
            b.Mark(tail, at + V(0.05f, 0.02f, 0) * k, V(1f, 0.4f, 0), 0.03f * k, 0.03f * k, glow, MarkShape.Zigzag);
        }
        var end = t0 + V(0, 0.24f, -0.62f) * k;
        if (smith)
        {
            // The hammer's head lies across the tail's tip, banded and riveted
            b.Box(tail, end, V(0.13f, 0.07f, 0.075f) * k, 0.02f * k, iron, V(0, 0, 0), Metal, 0.01f);
            foreach (float s in new[] { -1f, 1f })
            {
                b.Box(tail, end + V(0.1f * s, 0, 0) * k, V(0.02f, 0.085f, 0.09f) * k, 0.01f * k, Rgb(96, 98, 106), mat: Metal, blend: 0.004f);
                b.PaintEll(tail, end + V(0.14f * s, 0, 0) * k, V(0.03f, 0.06f, 0.06f) * k, glow, soft: 0.006f);
            }
            foreach (float x in new[] { -0.05f, 0f, 0.05f })
                b.Ell(tail, end + V(x, 0.07f, 0.03f) * k, V(0.012f, 0.012f, 0.012f) * k, rivet, mat: Metal, blend: 0.004f);
        }
        else
            Cp1Coal(b, tail, end, 0.07f);

        // The head: a long snout; on the smith an anvil's face of iron over the crown, its horn to the front
        int head = b.Head(V(0, 0.68f, 0.06f) * k);
        var hc = V(0, 0.78f, 0.12f) * k;
        var hr = V(0.12f, 0.1f, 0.13f) * k;
        b.Ell(head, hc, hr, black);
        b.Ell(head, hc + V(0, -0.03f, 0.12f) * k, V(0.09f, 0.065f, 0.1f) * k, black);
        b.Mark(head, hc + V(0, -0.06f, 0.21f) * k, V(0, -0.3f, 1f), 0.07f * k, 0.02f * k, smith ? glow : Rgb(20, 18, 22), MarkShape.Smile);
        PokeBuilder.Both(s => b.Mark(head, hc + V(0.035f * s, 0.0f, 0.215f) * k, V(0.1f * s, 0.4f, 1f), 0.008f * k, 0.006f * k, Rgb(20, 18, 22)));
        if (smith)
        {
            var top = hc + V(0, hr.Y * 0.85f, -0.01f);
            b.Box(head, top, V(0.13f, 0.035f, 0.11f) * k, 0.012f * k, iron, V(-6f, 0, 0), Metal, 0.012f);
            PokeBuilder.Both(s => b.Spike(head, top + V(0.1f * s, 0.01f, 0) * k, top + V(0.24f * s, 0.03f, 0) * k, 0.04f * k, iron, 0.55f, Metal, 0.008f));
            foreach (float x in new[] { -0.08f, 0f, 0.08f })
                b.Ell(head, top + V(x, 0.03f, 0.07f) * k, V(0.012f, 0.012f, 0.012f) * k, rivet, mat: Metal, blend: 0.004f);
            // A glowing seam down its neck
            b.Mark(head, hc + V(0.1f, -0.02f, -0.04f) * k, V(1f, 0, -0.2f), 0.035f * k, 0.03f * k, glow, MarkShape.Zigzag);
        }
        else
            b.Mark(head, Out(hc, hr, default, V(0.4f, 0.8f, -0.3f)), V(0.4f, 0.8f, -0.3f), 0.03f * k, 0.03f * k, glow, MarkShape.Zigzag);
        PokeBuilder.Both(s =>
        {
            var at = Out(hc, hr, default, V(0.62f * s, 0.35f, 0.62f));
            b.Eye(head, at, Outward(hc, hr, at), 0.026f * k, smith ? Rgb(250, 120, 40) : Rgb(120, 80, 50), glare: smith);
        });
        return b;
    }

    // ------------------------------------------------------------------ Pelicub, Pelireed and Buciumbill

    /// <summary>A bird's leg ending in a broad webbed foot, splayed on the ground.</summary>
    private static void Cp1WebbedLegs(PokeBuilder b, float x, float hip, float z, float thick, float foot, Color color)
    {
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(x * s, hip, z));
            var ankle = V(x * 1.1f * s, foot * 0.25f, z + 0.01f);
            b.Limb(leg, V(x * s, hip, z), ankle, thick, thick * 0.85f, color, Scales, 0.008f);
            b.Ell(leg, V(x * 1.12f * s, foot * 0.13f, z + foot * 0.6f), V(foot * 0.62f, foot * 0.13f, foot * 0.75f), color, V(0, 12f * s, 0), Scales, 0.01f);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.PaintEll(leg, V(x * 1.12f * s + t * foot * 0.3f, foot * 0.2f, z + foot * 0.85f), V(0.004f, foot * 0.12f, foot * 0.4f), PixelCanvas.Shadow(color, 0.25f), V(0, t * 18f + 12f * s, 0), 0.004f);
        });
    }

    /// <summary>Pelicub: a round, fluffy pelican chick in white down tinged blue, its beak too big for it and a pouch it can barely hold up.</summary>
    private static PokeBuilder Cp1Pelicub()
    {
        var b = new PokeBuilder("Pelicub", 0.56f, BodyPlan.Bird, V(0, 0.22f, -0.02f)) { Coat = Fur };
        var down = Rgb(232, 242, 250);
        var shade = Rgb(196, 218, 236);
        var beak = Rgb(240, 216, 150);
        var pouch = Rgb(244, 226, 166);
        Cp1WebbedLegs(b, 0.07f, 0.12f, 0.0f, 0.018f, 0.1f, Rgb(244, 150, 72));
        var bc = V(0, 0.22f, -0.02f);
        b.Ell(Body, bc, V(0.16f, 0.15f, 0.16f), down);
        b.PaintEll(Body, bc + V(0, -0.1f, 0), V(0.17f, 0.07f, 0.17f), shade);
        FurTufts(b, Body, bc, V(0.16f, 0.15f, 0.16f), 24, 0.04f, 0.022f, down, 0.4f, -0.3f, 0.5f);
        int tail = b.Tail(bc + V(0, 0.02f, -0.14f));
        b.Spike(tail, bc + V(0, 0.02f, -0.12f), bc + V(0, 0.08f, -0.24f), 0.04f, down, 0.6f);
        PokeBuilder.Both(s => FoldedWing(b, s, bc + V(0.14f * s, 0.06f, 0.02f), bc + V(0.16f * s, -0.03f, -0.13f), 0.07f, down, shade));

        int head = b.Head(bc + V(0, 0.12f, 0.04f));
        var hc = bc + V(0, 0.18f, 0.05f);
        var hr = V(0.11f, 0.105f, 0.1f);
        b.Ell(head, hc, hr, down);
        foreach (float s in new[] { -1f, 0f, 1f })
            b.Spike(head, hc + V(0.03f * s, hr.Y * 0.85f, -0.02f), hc + V(0.05f * s, hr.Y + 0.05f, -0.04f), 0.018f, down, 0.6f);
        // The great beak, its tip hooked a little, and the pouch sagging under it
        var root = hc + V(0, -0.01f, hr.Z * 0.75f);
        b.Ell(head, root + V(0, 0, 0.1f), V(0.05f, 0.026f, 0.13f), beak, V(8f, 0, 0), Shell, 0.012f);
        b.Ell(head, root + V(0, -0.025f, 0.22f), V(0.03f, 0.025f, 0.03f), PixelCanvas.Shadow(beak, 0.15f), mat: Shell, blend: 0.01f);
        int jaw = b.Jaw(head, root + V(0, -0.03f, 0));
        b.Ell(jaw, root + V(0, -0.085f, 0.08f), V(0.065f, 0.07f, 0.11f), pouch, V(-10f, 0, 0), Shell, 0.015f);
        PokeBuilder.Both(s =>
        {
            var at = Out(hc, hr, default, V(0.62f * s, 0.32f, 0.65f));
            b.Eye(head, at, Outward(hc, hr, at), 0.026f, Rgb(36, 36, 44));
        });
        return b;
    }

    /// <summary>
    /// The grown pelicans: Pelireed of the delta, white with reed-green wingtips, a crest of reed blades and a pouch
    /// striped like a little drum, one wing raised; Buciumbill a great pelican with a grey-blue mantle and a curly crest,
    /// its beak grown into a shepherd's alphorn bound with bark, its pouch a drum of blue skin.
    /// </summary>
    private static PokeBuilder Cp1Pelican(bool horn)
    {
        float k = horn ? 1.3f : 1f;
        var b = new PokeBuilder(horn ? "Buciumbill" : "Pelireed", horn ? 0.98f : 0.78f, BodyPlan.Bird, V(0, 0.3f, -0.04f) * k) { Coat = Fur };
        var white = Rgb(238, 244, 240);
        var shade = Rgb(200, 214, 220);
        var reed = Rgb(106, 160, 98);
        var mantle = Rgb(132, 150, 168);
        var teal = Rgb(54, 120, 110);
        var beak = Rgb(234, 206, 146);
        var blue = Rgb(84, 130, 196);
        var cream = Rgb(240, 222, 170);
        Cp1WebbedLegs(b, 0.08f * k, 0.2f * k, 0.0f, 0.022f * k, 0.12f * k, horn ? Rgb(240, 150, 70) : Rgb(226, 182, 110));
        var bc = V(0, 0.32f, -0.04f) * k;
        var br = V(0.17f, 0.15f, 0.21f) * k;
        b.Ell(Body, bc, br, white, V(-10f, 0, 0));
        b.PaintEll(Body, bc + V(0, -0.1f, 0) * k, V(0.18f, 0.07f, 0.22f) * k, shade);
        int tail = b.Tail(bc + V(0, 0.02f, -0.18f) * k);
        TailFan(b, tail, bc + V(0, 0.03f, -0.18f) * k, 3, 40f, 0.14f * k, 0.04f * k, white, horn ? mantle : reed, -0.3f);

        // Wings: Pelireed's right one raised, both tipped reed green; Buciumbill's folded under a grey-blue mantle
        PokeBuilder.Both(s =>
        {
            if (!horn && s > 0)
            {
                int wing = b.Wing(s, bc + V(0.14f * s, 0.07f, 0.02f) * k);
                var sh = bc + V(0.14f * s, 0.07f, 0.02f) * k;
                foreach (var (dx, dy, w) in new[] { (0.1f, 0.26f, 0.06f), (0.16f, 0.2f, 0.055f), (0.2f, 0.12f, 0.05f), (0.21f, 0.04f, 0.045f) })
                {
                    var tip = sh + V(dx * s, dy, -0.04f) * k;
                    Frond(b, wing, sh, tip, w * k, white, V(0, 0.2f, 1f), 0.25f);
                    b.PaintEll(wing, Vector3.Lerp(sh, tip, 0.82f), V(w * 1.3f, 0.06f, w * 1.3f) * k, reed, Euler(tip - sh));
                }
            }
            else
                FoldedWing(b, s, bc + V(0.14f * s, 0.07f, 0.04f) * k, bc + V(0.17f * s, -0.04f, -0.24f) * k, 0.1f * k, horn ? mantle : white, horn ? teal : reed);
        });

        // A neck curving up to the head
        var hc = V(0, horn ? 0.7f : 0.66f, 0.07f) * k;
        var neck = Smooth(3, bc + V(0, 0.08f, 0.12f) * k, bc + V(0, 0.2f, 0.16f) * k, hc + V(0, -0.1f, -0.03f) * k);
        b.Tube(Body, neck, 0.07f * k, 0.055f * k, white, blend: 0.03f);
        int head = b.Head(hc + V(0, -0.06f, -0.02f) * k);
        var hr = V(0.075f, 0.07f, 0.08f) * k;
        b.Ell(head, hc, hr, white);
        if (horn)
        {
            // The curly crest: locks of grey with dark green between, curling back
            for (int i = 0; i < 9; i++)
            {
                float a = (i - 4) * 0.36f;
                var root = hc + V(MathF.Sin(a) * hr.X * 0.7f, hr.Y * 0.7f, -hr.Z * 0.1f - MathF.Abs(a) * 0.02f);
                var color = i % 3 == 1 ? Rgb(64, 104, 84) : Rgb(132, 146, 158);
                b.Tube(head, Smooth(3, root, root + V(MathF.Sin(a) * 0.04f, 0.07f, -0.02f) * k, root + V(MathF.Sin(a) * 0.08f, 0.08f, -0.08f) * k,
                    root + V(MathF.Sin(a) * 0.08f, 0.04f, -0.07f) * k), 0.016f * k, 0.011f * k, color, blend: 0.006f);
            }
        }
        else
        {
            // The crest of reed blades, fanned up and back
            for (int i = 0; i < 9; i++)
            {
                float a = (i - 4) * 0.3f;
                var root = hc + V(MathF.Sin(a) * hr.X * 0.5f, hr.Y * 0.7f, -0.01f);
                var tip = root + V(MathF.Sin(a) * 0.08f, 0.14f - MathF.Abs(a) * 0.05f, -0.06f - MathF.Abs(a) * 0.04f);
                Blade(b, head, root, tip, 0.016f, reed, V(MathF.Cos(a) * 0.3f, 0, 1f), 0.3f, Leaf);
            }
        }

        var root2 = hc + V(0, -0.015f, hr.Z * 0.8f);
        int jaw = b.Jaw(head, root2 + V(0, -0.02f, 0));
        if (horn)
        {
            // The bucium: a long wooden horn out of its face, bands of woven bark round it, flaring at the mouth
            var mouth = root2 + V(0, -0.28f, 0.42f) * k;
            var dir = Vector3.Normalize(mouth - root2);
            var wood = Rgb(150, 110, 70);
            b.Limb(head, root2 - dir * 0.02f, mouth - dir * 0.1f * k, 0.03f * k, 0.042f * k, wood, Shell, 0.01f);
            b.Spike(head, mouth - dir * 0.12f * k, mouth, 0.03f * k, wood, mat: Shell);
            var bell = mouth - dir * 0.02f * k;
            b.Torus(head, bell, 0.075f * k, 0.017f * k, wood, Euler(dir), mat: Shell, blend: 0.02f);
            b.Limb(head, mouth - dir * 0.1f * k, bell, 0.045f * k, 0.08f * k, wood, Shell, 0.01f);
            b.Cut(head, bell + dir * 0.03f * k, V(0.062f, 0.05f, 0.062f) * k, Euler(dir));
            b.PaintEll(head, bell, V(0.064f, 0.03f, 0.064f) * k, Rgb(78, 54, 36), Euler(dir));
            foreach (float t in new[] { 0.3f, 0.36f, 0.62f, 0.68f })
                b.Torus(head, Vector3.Lerp(root2, mouth, t), (0.034f + 0.006f * t) * k, 0.008f * k, Rgb(110, 80, 52), Euler(dir), mat: Shell, blend: 0.004f);
            // The pouch: a drum of blue skin under the horn, striped cream
            var pc = root2 + V(0, -0.1f, 0.1f) * k;
            var pr = V(0.06f, 0.08f, 0.11f) * k;
            b.Ell(jaw, pc, pr, blue, V(-30f, 0, 0), Shell, 0.015f);
            foreach (float z in new[] { -0.05f, 0.0f, 0.05f })
                b.PaintEll(jaw, pc + V(0, -z * 0.5f, z) * k, V(0.08f, 0.1f, 0.012f) * k, cream, V(-30f, 0, 0), 0.006f);
        }
        else
        {
            // A long beak, a pouch under it striped blue and cream like a drum's skin
            var tip = root2 + V(0, -0.04f, 0.26f);
            b.Ell(head, Vector3.Lerp(root2, tip, 0.5f), V(0.035f, 0.022f, 0.14f), beak, V(9f, 0, 0), Shell, 0.012f);
            b.Ell(head, tip + V(0, -0.01f, 0), V(0.024f, 0.02f, 0.025f), PixelCanvas.Shadow(beak, 0.15f), mat: Shell, blend: 0.008f);
            var pc = root2 + V(0, -0.075f, 0.13f);
            b.Ell(jaw, pc, V(0.05f, 0.065f, 0.12f), cream, V(-12f, 0, 0), Shell, 0.015f);
            foreach (float z in new[] { -0.07f, -0.01f, 0.05f, 0.11f })
                b.PaintEll(jaw, pc + V(0, 0, z), V(0.07f, 0.09f, 0.018f), blue, V(-12f, 0, 0), 0.006f);
        }
        PokeBuilder.Both(s =>
        {
            var at = Out(hc, hr, default, V(0.75f * s, 0.32f, 0.5f));
            b.Eye(head, at, Outward(hc, hr, at), 0.02f * k, Rgb(92, 60, 44), glare: horn);
        });
        return b;
    }

    // ------------------------------------------------------------------ Trillet, Nightrill and Privigale

    /// <summary>An open beak, mid-song: an upper and a lower half apart, the mouth between them pink.</summary>
    private static void Cp1SingingBeak(PokeBuilder b, int head, Vector3 root, Vector3 dir, float length, float r, Color beak)
    {
        var d = Vector3.Normalize(dir);
        var up = Vector3.Normalize(V(0, 1f, 0) - d * d.Y);
        b.Ell(head, root + d * r * 0.3f, V(r * 0.9f, r * 0.9f, r * 0.9f), Rgb(222, 120, 128), mat: Shell, blend: 0.008f);
        b.Spike(head, root + up * r * 0.5f, root + d * length + up * r * 0.6f, r, beak, 0.7f, Shell, 0.006f);
        b.Spike(head, root - up * r * 0.5f, root + d * length * 0.8f - up * r * 1.4f, r * 0.85f, beak, 0.7f, Shell, 0.006f);
    }

    /// <summary>Trillet: a tiny brown songbird round as a ball, its pale breast speckled, its beak open in song and its tail cocked.</summary>
    private static PokeBuilder Cp1Trillet()
    {
        var b = new PokeBuilder("Trillet", 0.46f, BodyPlan.Bird, V(0, 0.15f, 0)) { Coat = Fur }.Hover();
        var brown = Rgb(178, 130, 80);
        var dark = Rgb(126, 88, 56);
        var cream = Rgb(246, 232, 190);
        BirdLegs(b, 0.045f, 0.07f, 0.0f, 0.008f, Rgb(84, 64, 60));
        var bc = V(0, 0.16f, 0);
        var br = V(0.12f, 0.115f, 0.12f);
        b.Ell(Body, bc, br, brown);
        b.PaintEll(Body, bc + V(0, -0.02f, 0.07f), V(0.1f, 0.1f, 0.08f), cream);
        foreach (var (x, y) in new[] { (-0.05f, 0.14f), (0.03f, 0.12f), (0.0f, 0.17f), (-0.03f, 0.1f), (0.06f, 0.16f), (0.02f, 0.08f), (-0.06f, 0.18f) })
        {
            var at = On(bc, br, x, y);
            b.Mark(Body, at, Outward(bc, br, at), 0.009f, 0.007f, Rgb(140, 104, 70));
        }
        int tail = b.Tail(bc + V(0, 0.02f, -0.1f));
        TailFan(b, tail, bc + V(0, 0.03f, -0.1f), 3, 26f, 0.13f, 0.03f, brown, dark, -1.2f);
        PokeBuilder.Both(s => FoldedWing(b, s, bc + V(0.1f * s, 0.05f, 0.02f), bc + V(0.13f * s, -0.03f, -0.1f), 0.055f, brown, dark));

        int head = b.Head(bc + V(0, 0.07f, 0.04f));
        var hc = bc + V(0, 0.09f, 0.04f);
        var hr = V(0.085f, 0.08f, 0.085f);
        b.Ell(head, hc, hr, brown);
        Cp1SingingBeak(b, head, Out(hc, hr, default, V(0, -0.05f, 1f)), V(0, 0.35f, 1f), 0.045f, 0.016f, Rgb(116, 104, 100));
        PokeBuilder.Both(s =>
        {
            var at = Out(hc, hr, default, V(0.6f * s, 0.3f, 0.7f));
            b.Eye(head, at, Outward(hc, hr, at), 0.022f, Rgb(64, 56, 40));
        });
        return b;
    }

    /// <summary>Nightrill: a slim nightingale in warm russet, a pale ring round its eye, its throat warm and its rufous tail fanned like sheet music.</summary>
    private static PokeBuilder Cp1Nightrill()
    {
        var b = new PokeBuilder("Nightrill", 0.62f, BodyPlan.Bird, V(0, 0.25f, -0.02f)) { Coat = Fur }.Hover();
        var brown = Rgb(176, 112, 64);
        var rufous = Rgb(184, 88, 48);
        var dark = Rgb(110, 60, 36);
        var cream = Rgb(244, 228, 190);
        var throat = Rgb(246, 196, 130);
        BirdLegs(b, 0.045f, 0.14f, 0.0f, 0.01f, Rgb(80, 60, 56));
        var bc = V(0, 0.25f, -0.02f);
        var br = V(0.1f, 0.1f, 0.15f);
        b.Ell(Body, bc, br, brown, V(-25f, 0, 0));
        b.PaintEll(Body, bc + V(0, -0.01f, 0.07f), V(0.08f, 0.09f, 0.09f), cream, V(-25f, 0, 0));
        // Speckles on the breast, painted: the body is tipped, so a mark's square would hang off its curve
        foreach (var (x, y, z) in new[] { (-0.03f, 0.27f, 0.12f), (0.03f, 0.25f, 0.13f), (0.0f, 0.21f, 0.13f), (-0.045f, 0.2f, 0.11f), (0.045f, 0.29f, 0.11f) })
            b.PaintEll(Body, V(x, y, z), V(0.008f, 0.007f, 0.03f), Rgb(140, 96, 64), soft: 0.004f);
        int tail = b.Tail(bc + V(0, 0.0f, -0.13f));
        // The fanned tail, each feather banded twice like lines of a stave
        var tr = bc + V(0, 0.01f, -0.13f);
        for (int i = 0; i < 7; i++)
        {
            float a = (-50f + 100f * i / 6f) * Degree;
            var dir = Vector3.Normalize(V(MathF.Sin(a) * 0.9f, 0.75f, -0.6f));
            var tip = tr + dir * 0.22f;
            Frond(b, tail, tr, tip, 0.034f, rufous, V(0, -0.5f, -0.8f), 0.22f, Fur, 0.01f);
            foreach (float t in new[] { 0.55f, 0.72f })
                b.PaintEll(tail, Vector3.Lerp(tr, tip, t), V(0.05f, 0.008f, 0.05f), dark, Euler(dir), 0.004f);
        }
        PokeBuilder.Both(s => FoldedWing(b, s, bc + V(0.08f * s, 0.05f, 0.04f), bc + V(0.1f * s, -0.04f, -0.15f), 0.055f, brown, dark));

        int head = b.Head(bc + V(0, 0.08f, 0.08f));
        var hc = bc + V(0, 0.12f, 0.1f);
        var hr = V(0.07f, 0.068f, 0.075f);
        b.Ell(head, hc, hr, brown);
        b.PaintEll(head, hc + V(0, -0.045f, 0.03f), V(0.06f, 0.04f, 0.06f), throat);
        Cp1SingingBeak(b, head, Out(hc, hr, default, V(0, -0.1f, 1f)), V(0, 0.15f, 1f), 0.06f, 0.012f, Rgb(96, 86, 84));
        PokeBuilder.Both(s =>
        {
            var at = Out(hc, hr, default, V(0.65f * s, 0.3f, 0.6f));
            b.PaintEll(head, at, V(0.024f, 0.024f, 0.024f), cream, soft: 0.006f);
            b.Eye(head, at, Outward(hc, hr, at), 0.016f, Rgb(40, 34, 30));
        });
        return b;
    }

    /// <summary>
    /// Privigale: a large, elegant nightingale, chest out and wings half open like a singer's arms, a glowing ruff at its
    /// throat and long trailing tail feathers in russet and moonlight silver, banded like the lines of a stave.
    /// </summary>
    private static PokeBuilder Cp1Privigale()
    {
        var b = new PokeBuilder("Privigale", 0.96f, BodyPlan.Bird, V(0, 0.5f, -0.02f)) { Coat = Fur }.Hover();
        var brown = Rgb(170, 104, 60);
        var rufous = Rgb(176, 86, 46);
        var dark = Rgb(92, 52, 34);
        var silver = Rgb(212, 220, 230);
        var cream = Rgb(244, 230, 196);
        var glow = Rgb(238, 240, 250);
        BirdLegs(b, 0.06f, 0.34f, 0.0f, 0.016f, Rgb(72, 56, 52));
        var bc = V(0, 0.5f, -0.02f);
        var br = V(0.14f, 0.18f, 0.15f);
        b.Ell(Body, bc, br, brown, V(-12f, 0, 0));
        b.PaintEll(Body, bc + V(0, 0.0f, 0.08f), V(0.11f, 0.15f, 0.1f), cream, V(-12f, 0, 0));
        foreach (var (x, y) in new[] { (-0.04f, 0.46f), (0.04f, 0.44f), (0.0f, 0.4f), (-0.06f, 0.39f), (0.06f, 0.41f) })
        {
            var at = On(bc, br, x, y);
            b.Mark(Body, at, Outward(bc, br, at), 0.01f, 0.008f, Rgb(140, 96, 64));
        }

        // The long train: feathers sweeping back and down to trail along the ground, russet and silver, banded
        int tail = b.Tail(bc + V(0, -0.08f, -0.13f));
        var tr = bc + V(0, -0.08f, -0.13f);
        for (int i = 0; i < 7; i++)
        {
            float a = (-36f + 72f * i / 6f) * Degree;
            float len = 0.52f - MathF.Abs(a) * 0.12f;
            var mid = tr + V(MathF.Sin(a) * 0.12f, -0.2f, -0.18f);
            var tip = tr + V(MathF.Sin(a) * 0.26f, -0.4f, -0.2f - len);
            var color = i % 2 == 0 ? rufous : silver;
            Frond(b, tail, tr, mid, 0.04f, color, V(0, 1f, -0.4f), 0.25f, Fur, 0.012f);
            Frond(b, tail, mid, tip, 0.036f, color, V(0, 1f, 0.3f), 0.25f, Fur, 0.012f);
            foreach (float t in new[] { 0.35f, 0.6f, 0.85f })
                b.PaintEll(tail, Vector3.Lerp(mid, tip, t), V(0.05f, 0.05f, 0.01f), i % 2 == 0 ? dark : Rgb(150, 160, 176), Euler(tip - mid), 0.004f);
        }

        // Wings half open like a singer's arms, each primary russet banded dark, silver at its edge
        PokeBuilder.Both(s =>
        {
            var sh = bc + V(0.12f * s, 0.1f, 0.0f);
            int wing = b.Wing(s, sh);
            var wrist = sh + V(0.16f * s, 0.12f, -0.04f);
            b.Limb(wing, sh, wrist, 0.05f, 0.035f, brown);
            foreach (var (dx, dy, dz, w) in new[] { (0.36f, 0.3f, -0.1f, 0.05f), (0.38f, 0.18f, -0.1f, 0.05f), (0.34f, 0.05f, -0.1f, 0.048f), (0.26f, -0.06f, -0.08f, 0.045f),
                         (0.16f, -0.12f, -0.06f, 0.045f) })
            {
                var tip = sh + V(dx * s, dy, dz);
                var from = Vector3.Lerp(sh, wrist, 0.6f);
                Frond(b, wing, from, tip, w, rufous, V(0, 0.1f, 1f), 0.22f, Fur, 0.01f);
                b.PaintEll(wing, Vector3.Lerp(from, tip, 0.7f), V(w * 1.4f, 0.012f, w * 1.4f), dark, Euler(tip - from), 0.004f);
                b.PaintEll(wing, Vector3.Lerp(from, tip, 0.95f), V(w * 1.4f, 0.03f, w * 1.4f), silver, Euler(tip - from), 0.006f);
            }
        });

        int head = b.Head(bc + V(0, 0.16f, 0.04f));
        var hc = bc + V(0, 0.28f, 0.06f);
        var hr = V(0.085f, 0.08f, 0.09f);
        b.Ell(head, hc, hr, brown);
        b.PaintEll(head, hc + V(0, -0.05f, 0.04f), V(0.07f, 0.04f, 0.06f), cream);
        // A neck up to the head, the ruff of glowing throat feathers round it, and a crest swept back
        b.Limb(Body, bc + V(0, 0.1f, 0.02f), hc + V(0, -0.05f, -0.01f), 0.1f, 0.07f, brown);
        for (int i = 0; i < 12; i++)
        {
            float a = (-1f + 2f * i / 11f) * 1.6f;
            var o = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = bc + V(0, 0.17f, 0.03f) + o * 0.07f;
            b.Spike(Body, root, root + o * 0.09f + V(0, -0.05f, 0), 0.03f, i % 2 == 0 ? glow : silver, 0.55f, Glow, 0.015f);
        }
        for (int i = 0; i < 5; i++)
        {
            float a = (i - 2) * 0.35f;
            var root = hc + V(MathF.Sin(a) * 0.03f, hr.Y * 0.75f, -0.02f);
            Blade(b, head, root, root + V(MathF.Sin(a) * 0.06f, 0.08f, -0.12f + MathF.Abs(a) * 0.04f), 0.02f, i == 2 ? silver : rufous, V(0, 1f, 0.3f), 0.3f);
        }
        Cp1SingingBeak(b, head, Out(hc, hr, default, V(0, -0.1f, 1f)), V(0, 0.3f, 1f), 0.07f, 0.016f, Rgb(86, 78, 78));
        PokeBuilder.Both(s =>
        {
            var at = Out(hc, hr, default, V(0.65f * s, 0.3f, 0.6f));
            b.PaintEll(head, at, V(0.028f, 0.028f, 0.028f), cream, soft: 0.006f);
            b.Eye(head, at, Outward(hc, hr, at), 0.018f, Rgb(40, 34, 30));
        });
        return b;
    }

    // ------------------------------------------------------------------ Harciog and Harciogre

    /// <summary>
    /// The field hamsters: Harciog sitting up, rusty orange over a pure black belly, its white cheeks stuffed to twice
    /// its head's size and its paws together; Harciogre big and grumpy, its forepaws grown into spades of claws caked
    /// in earth, a sack of grain on its back held by a strap of roots.
    /// </summary>
    private static PokeBuilder Cp1Hamster(bool hoarder)
    {
        float k = hoarder ? 1.5f : 1f;
        var b = new PokeBuilder(hoarder ? "Harciogre" : "Harciog", hoarder ? 0.86f : 0.54f, BodyPlan.Biped, V(0, 0.22f, 0) * k) { Coat = Fur };
        var orange = hoarder ? Rgb(222, 112, 52) : Rgb(196, 92, 48);
        var black = Rgb(40, 38, 42);
        var white = Rgb(246, 246, 248);
        var pink = Rgb(226, 182, 184);
        var earth = Rgb(122, 100, 76);

        // Feet: pink soles flat on the ground
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.09f * s, 0.08f, 0.02f) * k);
            b.Ell(leg, V(0.1f * s, 0.06f, 0.03f) * k, V(0.07f, 0.06f, 0.07f) * k, orange);
            b.Ell(leg, V(0.11f * s, 0.018f, 0.08f) * k, V(0.04f, 0.018f, 0.06f) * k, pink);
        });
        var bc = V(0, 0.22f, 0) * k;
        var br = V(0.17f, 0.21f, 0.15f) * k;
        b.Ell(Body, bc, br, orange);
        b.PaintEll(Body, bc + V(0, -0.02f, 0.1f) * k, V(0.11f, 0.17f, 0.09f) * k, black);
        int tail = b.Tail(bc + V(0, -0.12f, -0.1f) * k);
        b.Ell(tail, bc + V(0, -0.13f, -0.13f) * k, V(0.035f, 0.03f, 0.04f) * k, orange);

        if (hoarder)
        {
            // The sack of grain on its back, its neck tied and open at the top, and the strap of roots across the chest
            var sack = bc + V(-0.05f, 0.08f, -0.2f) * k;
            var burlap = Rgb(178, 150, 104);
            b.Ell(Body, sack, V(0.13f, 0.15f, 0.1f) * k, burlap, V(0, 0, 15f), Fur, 0.02f);
            b.Ell(Body, sack + V(-0.04f, 0.17f, 0) * k, V(0.06f, 0.05f, 0.05f) * k, burlap, V(0, 0, 20f), Fur, 0.02f);
            b.Torus(Body, sack + V(-0.03f, 0.13f, 0) * k, 0.05f * k, 0.012f * k, Rgb(108, 76, 48), V(0, 0, 20f), mat: Fur, blend: 0.006f);
            b.Ell(Body, sack + V(-0.05f, 0.21f, 0) * k, V(0.045f, 0.015f, 0.04f) * k, Rgb(214, 188, 120), mat: Fur, blend: 0.006f);
            for (int i = 0; i < 6; i++)
                b.PaintEll(Body, sack + V(0, 0.1f - i * 0.04f, 0) * k, V(0.15f, 0.004f, 0.12f) * k, PixelCanvas.Shadow(burlap, 0.18f), V(0, 0, 15f), 0.003f);
            b.Torus(Body, bc + V(0, 0.04f, 0) * k, 0.175f * k, 0.014f * k, Rgb(110, 74, 46), V(0, 0, 35f), 1f, 0.9f, Fur, 0.006f);
        }

        // Arms: tiny paws held together, or the hoarder's spades of claws
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.13f * s, 0.3f, 0.06f) * k);
            if (hoarder)
            {
                var hand = V(0.22f * s, 0.12f, 0.12f) * k;
                b.Limb(arm, V(0.13f * s, 0.3f, 0.06f) * k, hand, 0.05f * k, 0.045f * k, orange);
                b.Ell(arm, hand, V(0.06f, 0.045f, 0.06f) * k, orange);
                b.PaintEll(arm, hand + V(0, 0.02f, 0) * k, V(0.075f, 0.07f, 0.075f) * k, earth);
                foreach (float t in new[] { -1f, 0f, 1f })
                    b.Spike(arm, hand + V(t * 0.03f, -0.02f, 0.04f) * k, hand + V(t * 0.04f, -0.08f, 0.09f) * k, 0.016f * k, Rgb(150, 144, 136), 0.6f, Shell, 0.004f);
            }
            else
            {
                var paw = V(0.035f * s, 0.27f, 0.15f) * k;
                b.Limb(arm, V(0.13f * s, 0.3f, 0.06f) * k, paw, 0.035f, 0.03f, orange);
                b.Ell(arm, paw + V(0, -0.005f, 0.01f), V(0.025f, 0.02f, 0.022f), pink);
            }
        });

        int head = b.Head(V(0, 0.36f, 0.02f) * k);
        var hc = V(0, 0.46f, 0.03f) * k;
        var hr = V(0.12f, 0.11f, 0.11f) * k;
        b.Ell(head, hc, hr, orange);
        // The cheeks, stuffed white, and a white muzzle with a pink nose between them
        PokeBuilder.Both(s => b.Ell(head, hc + V(0.11f * s, -0.07f, 0.06f) * k, V(0.1f, 0.085f, 0.09f) * k, white, blend: 0.02f));
        b.Ell(head, hc + V(0, -0.06f, 0.1f) * k, V(0.05f, 0.04f, 0.04f) * k, white);
        b.Ell(head, hc + V(0, -0.035f, 0.14f) * k, V(0.016f, 0.012f, 0.012f) * k, Rgb(214, 140, 140), mat: Shell, blend: 0.006f);
        b.Mark(head, hc + V(0, -0.075f, 0.135f) * k, V(0, -0.2f, 1f), 0.02f * k, 0.01f * k, Rgb(90, 80, 84), hoarder ? MarkShape.Wave : MarkShape.Smile, hoarder ? 180f : 0f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, hc + V(0.08f * s, 0.08f, -0.02f) * k);
            b.Ell(ear, hc + V(0.085f * s, 0.095f, -0.02f) * k, V(0.04f, 0.04f, 0.022f) * k, orange, V(0, 0, -20f * s));
            b.PaintEll(ear, hc + V(0.09f * s, 0.105f, 0.0f) * k, V(0.025f, 0.025f, 0.015f) * k, PixelCanvas.Light1(orange, 0.3f), V(0, 0, -20f * s));
        });
        PokeBuilder.Both(s =>
        {
            var at = Out(hc, hr, default, V(0.42f * s, 0.32f, 0.85f));
            b.Eye(head, at, Outward(hc, hr, at), 0.022f * k, Rgb(70, 40, 36), glare: hoarder);
        });
        return b;
    }

    // ------------------------------------------------------------------ Chirpet and Fiddlecrick

    /// <summary>Chirpet: a little green field cricket with long jumping legs, long thin antennae and ridged wing cases, a bright, cheerful face.</summary>
    private static PokeBuilder Cp1Chirpet()
    {
        var b = new PokeBuilder("Chirpet", 0.46f, BodyPlan.Quadruped, V(0, 0.13f, -0.02f)) { Coat = Shell };
        var green = Rgb(132, 204, 76);
        var dark = Rgb(76, 146, 52);
        var yellow = Rgb(208, 226, 96);

        // The body: thorax and a long abdomen, yellow beneath, banded
        var tc = V(0, 0.13f, 0.03f);
        b.Ell(Body, tc, V(0.055f, 0.055f, 0.065f), green);
        var ac = V(0, 0.12f, -0.09f);
        var ar = V(0.06f, 0.055f, 0.1f);
        b.Ell(Body, ac, ar, green);
        b.PaintEll(Body, ac + V(0, -0.035f, 0), V(0.055f, 0.03f, 0.1f), yellow);
        // The wing cases over its back, ridged like a file
        PokeBuilder.Both(s =>
        {
            var wc = V(0.03f * s, 0.17f, -0.07f);
            b.Ell(Body, wc, V(0.04f, 0.018f, 0.11f), dark, V(-6f, 0, -12f * s), Shell, 0.008f);
            for (int i = 0; i < 5; i++)
                b.PaintEll(Body, wc + V(0.01f * s, 0.016f, 0.07f - i * 0.03f), V(0.03f, 0.006f, 0.004f), Rgb(52, 104, 40), V(0, 0, -12f * s), 0.003f);
            b.PaintEll(Body, wc + V(0.035f * s, -0.005f, 0), V(0.008f, 0.012f, 0.1f), yellow, soft: 0.004f);
        });
        // Middle legs, folded under it
        PokeBuilder.Both(s =>
        {
            b.Limb(Body, V(0.04f * s, 0.11f, -0.01f), V(0.1f * s, 0.07f, -0.02f), 0.012f, 0.01f, green);
            b.Limb(Body, V(0.1f * s, 0.07f, -0.02f), V(0.11f * s, 0.01f, 0.0f), 0.01f, 0.009f, green);
        });
        // Front legs, small; the great hind legs, knees high above its back
        PokeBuilder.Both(s =>
        {
            int fore = b.Leg(s, V(0.04f * s, 0.11f, 0.06f), true);
            b.Limb(fore, V(0.04f * s, 0.11f, 0.06f), V(0.08f * s, 0.07f, 0.1f), 0.012f, 0.01f, green);
            b.Limb(fore, V(0.08f * s, 0.07f, 0.1f), V(0.08f * s, 0.01f, 0.11f), 0.01f, 0.009f, green);
            int hind = b.Leg(s, V(0.05f * s, 0.12f, -0.07f), false);
            var knee = V(0.09f * s, 0.27f, -0.12f);
            b.Ell(hind, Vector3.Lerp(V(0.055f * s, 0.12f, -0.07f), knee, 0.5f), V(0.026f, 0.085f, 0.03f), green, Euler(knee - V(0.055f * s, 0.12f, -0.07f)), Shell, 0.01f);
            b.Limb(hind, knee, V(0.11f * s, 0.02f, -0.2f), 0.012f, 0.009f, green);
            b.Limb(hind, V(0.11f * s, 0.02f, -0.2f), V(0.12f * s, 0.008f, -0.15f), 0.009f, 0.008f, dark);
            for (int i = 1; i <= 2; i++)
            {
                var at = Vector3.Lerp(knee, V(0.11f * s, 0.02f, -0.2f), i * 0.33f);
                b.Spike(hind, at, at + V(0.012f * s, 0, -0.018f), 0.005f, dark, blend: 0.002f);
            }
        });

        int head = b.Head(V(0, 0.17f, 0.06f));
        var hc = V(0, 0.22f, 0.1f);
        var hr = V(0.085f, 0.08f, 0.075f);
        b.Ell(head, hc, hr, green);
        b.Mark(head, Out(hc, hr, default, V(0, -0.45f, 1f)), V(0, -0.3f, 1f), 0.018f, 0.012f, Rgb(160, 64, 60), MarkShape.Smile);
        PokeBuilder.Both(s => b.PaintEll(head, Out(hc, hr, default, V(0.6f * s, -0.25f, 0.8f)), V(0.016f, 0.01f, 0.016f), Rgb(222, 236, 108), soft: 0.006f));
        // Long antennae, curving up and back over its body
        PokeBuilder.Both(s =>
        {
            var root = Out(hc, hr, default, V(0.25f * s, 0.85f, 0.4f));
            b.Tube(head, Smooth(4, root, root + V(0.02f * s, 0.09f, -0.02f), root + V(0.06f * s, 0.15f, -0.12f), root + V(0.1f * s, 0.12f, -0.26f)),
                0.008f, 0.006f, dark, blend: 0f);
        });
        PokeBuilder.Both(s =>
        {
            var at = Out(hc, hr, default, V(0.45f * s, 0.2f, 0.85f));
            b.Eye(head, at, Outward(hc, hr, at), 0.026f, Rgb(70, 46, 36));
        });
        return b;
    }

    /// <summary>
    /// Fiddlecrick: the carefree cricket of the fable, standing tall on its hind legs, its wing cases shaped and varnished
    /// like a violin's body with two f-holes, a bow in its hand and a straw hat on its head.
    /// </summary>
    private static PokeBuilder Cp1Fiddlecrick()
    {
        var b = new PokeBuilder("Fiddlecrick", 0.82f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Shell };
        var green = Rgb(124, 196, 72);
        var dark = Rgb(72, 140, 50);
        var yellow = Rgb(210, 226, 100);
        var varnish = Rgb(184, 96, 44);
        var deep = Rgb(124, 58, 30);
        var straw = Rgb(222, 190, 110);

        // Long legs bent at the knee, long narrow feet
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.3f, -0.02f));
            var knee = V(0.1f * s, 0.18f, 0.05f);
            b.Limb(leg, V(0.06f * s, 0.32f, -0.02f), knee, 0.03f, 0.022f, green);
            b.Limb(leg, knee, V(0.1f * s, 0.03f, -0.02f), 0.02f, 0.016f, green);
            b.Ell(leg, V(0.1f * s, 0.015f, 0.03f), V(0.025f, 0.015f, 0.06f), dark, mat: Shell, blend: 0.006f);
        });
        // The abdomen, long and upright, its belly ringed yellow
        var ac = V(0, 0.4f, 0);
        var ar = V(0.08f, 0.15f, 0.075f);
        b.Ell(Body, ac, ar, green);
        b.PaintEll(Body, ac + V(0, -0.01f, 0.05f), V(0.06f, 0.14f, 0.05f), yellow);
        for (int i = 0; i < 5; i++)
            b.PaintEll(Body, ac + V(0, 0.09f - i * 0.045f, 0.07f), V(0.06f, 0.004f, 0.03f), PixelCanvas.Shadow(yellow, 0.25f), soft: 0.003f);
        var tc = V(0, 0.58f, 0.01f);
        b.Ell(Body, tc, V(0.065f, 0.06f, 0.06f), green);

        // The violin: its wing cases, folded down its back, shaped with a waist and varnished, two f-holes
        var vc = V(0, 0.42f, -0.08f);
        b.Ell(Body, vc + V(0, 0.08f, 0), V(0.09f, 0.08f, 0.03f), varnish, V(8f, 0, 0), Shell, 0.012f);
        b.Ell(Body, vc + V(0, -0.07f, -0.01f), V(0.1f, 0.09f, 0.03f), varnish, V(8f, 0, 0), Shell, 0.012f);
        b.PaintEll(Body, vc + V(0, 0, -0.03f), V(0.012f, 0.2f, 0.02f), deep, V(8f, 0, 0), 0.004f);
        PokeBuilder.Both(s => b.Mark(Body, vc + V(0.05f * s, -0.01f, -0.035f), V(0, 0.15f, -1f), 0.008f, 0.03f, deep, MarkShape.Bar, 8f * s));

        // Arms: one draws the bow across its chest, the other is raised as it plays
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.06f * s, 0.6f, 0.03f));
            var elbow = s > 0 ? V(0.13f, 0.5f, 0.08f) : V(-0.14f, 0.56f, 0.06f);
            var hand = s > 0 ? V(0.06f, 0.46f, 0.15f) : V(-0.16f, 0.66f, 0.1f);
            b.Limb(arm, V(0.06f * s, 0.6f, 0.03f), elbow, 0.018f, 0.015f, green);
            b.Limb(arm, elbow, hand, 0.015f, 0.013f, green);
            b.Ell(arm, hand, V(0.02f, 0.018f, 0.02f), dark);
            if (s > 0)
            {
                // The bow: a thin stick with its pale hair, held across the chest
                var a = hand + V(0.12f, -0.06f, 0.01f);
                var z = hand + V(-0.2f, 0.12f, 0.03f);
                b.Limb(arm, a, z, 0.008f, 0.007f, deep, Shell, 0.004f);
                b.Limb(arm, a + V(0, -0.012f, 0.004f), z + V(0, -0.012f, 0.004f), 0.004f, 0.004f, Rgb(240, 232, 210), Shell, 0.003f);
                b.Ell(arm, z, V(0.012f, 0.014f, 0.01f), deep, mat: Shell, blend: 0.004f);
            }
        });
        // A small pair of middle legs at its sides
        PokeBuilder.Both(s => b.Limb(Body, ac + V(0.06f * s, 0.06f, 0.02f), ac + V(0.13f * s, 0.01f, 0.06f), 0.012f, 0.01f, green));

        int head = b.Head(V(0, 0.64f, 0.02f));
        var hc = V(0, 0.72f, 0.05f);
        var hr = V(0.07f, 0.075f, 0.07f);
        b.Ell(head, hc, hr, green);
        b.Ell(head, hc + V(0, -0.04f, 0.04f), V(0.045f, 0.035f, 0.04f), green);
        b.Mark(head, hc + V(0, -0.055f, 0.08f), V(0, -0.3f, 1f), 0.02f, 0.012f, Rgb(160, 64, 60), MarkShape.Smile);
        // The straw hat: a brim round its crown and a crown on top, a band round it
        var brim = hc + V(0, hr.Y * 0.62f, -0.01f);
        b.Torus(head, brim, 0.09f, 0.012f, straw, V(-8f, 0, 0), 1f, 0.95f, Fur, 0.004f);
        b.Ell(head, brim + V(0, 0, 0), V(0.095f, 0.008f, 0.09f), straw, V(-8f, 0, 0), Fur, 0.006f);
        b.Ell(head, brim + V(0, 0.035f, -0.005f), V(0.055f, 0.04f, 0.05f), straw, V(-8f, 0, 0), Fur, 0.01f);
        b.PaintEll(head, brim + V(0, 0.012f, -0.005f), V(0.06f, 0.008f, 0.056f), Rgb(150, 70, 50), V(-8f, 0, 0), 0.004f);
        PokeBuilder.Both(s =>
        {
            var root = hc + V(0.03f * s, 0.02f, hr.Z * 0.85f);
            b.Tube(head, Smooth(4, root, root + V(0.02f * s, 0.1f, 0.01f), root + V(0.08f * s, 0.2f, -0.04f), root + V(0.16f * s, 0.2f, -0.12f)),
                0.007f, 0.006f, dark, blend: 0f);
        });
        PokeBuilder.Both(s =>
        {
            var at = Out(hc, hr, default, V(0.5f * s, 0.12f, 0.85f));
            b.Eye(head, at, Outward(hc, hr, at), 0.02f, Rgb(70, 46, 36));
        });
        return b;
    }

    // ------------------------------------------------------------------ Buburel and Buburoza

    /// <summary>Buburel: a round red ladybird with seven black spots, a black head with two white cheek dots, held up in the air.</summary>
    private static PokeBuilder Cp1Buburel()
    {
        var b = new PokeBuilder("Buburel", 0.46f, BodyPlan.Floating, V(0, 0.15f, -0.02f)) { Coat = Shell }.Hover();
        var red = Rgb(214, 70, 52);
        var black = Rgb(40, 38, 44);
        var sc = V(0, 0.16f, -0.03f);
        var sr = V(0.15f, 0.12f, 0.15f);
        b.Ell(Body, sc, sr, red);
        b.Ell(Body, sc + V(0, -0.06f, 0.01f), V(0.13f, 0.05f, 0.13f), Rgb(70, 62, 60));
        b.PaintEll(Body, sc + V(0, 0.08f, -0.02f), V(0.006f, 0.08f, 0.16f), black, soft: 0.004f);
        foreach (var d in new[] { V(0, 0.5f, 1f), V(-0.55f, 0.65f, 0.3f), V(0.55f, 0.65f, 0.3f), V(-0.6f, 0.55f, -0.45f), V(0.6f, 0.55f, -0.45f),
                     V(-0.95f, 0.15f, 0f), V(0.95f, 0.15f, 0f) })
        {
            if (d.X == 0f) continue;
            var at = Out(sc, sr, default, d);
            b.Mark(Body, at, Outward(sc, sr, at), 0.026f, 0.026f, black);
        }
        var back = Out(sc, sr, default, V(0, 0.55f, -1f));
        b.Mark(Body, back, Outward(sc, sr, back), 0.026f, 0.026f, black);
        // Six small legs tucked under it
        for (int i = 0; i < 3; i++)
            PokeBuilder.Both(s =>
            {
                var root = sc + V(0.08f * s, -0.07f, 0.06f - i * 0.06f);
                b.Limb(Body, root, root + V(0.035f * s, -0.04f, 0.01f), 0.01f, 0.008f, black, blend: 0.006f);
            });

        int head = b.Head(sc + V(0, -0.01f, 0.1f));
        var hc = sc + V(0, -0.02f, 0.14f);
        var hr = V(0.09f, 0.075f, 0.07f);
        b.Ell(head, hc, hr, black);
        PokeBuilder.Both(s =>
        {
            var at = Out(hc, hr, default, V(0.72f * s, -0.3f, 0.6f));
            b.Mark(head, at, Outward(hc, hr, at), 0.018f, 0.018f, Rgb(244, 236, 214));
            Antenna(b, head, hc + V(0.03f * s, hr.Y * 0.8f, -0.01f), hc + V(0.05f * s, hr.Y + 0.04f, 0.01f), hc + V(0.06f * s, hr.Y + 0.06f, 0.04f), 0.008f, black, black, 0.012f);
            var eye = Out(hc, hr, default, V(0.4f * s, 0.15f, 0.9f));
            b.Eye(head, eye, Outward(hc, hr, eye), 0.024f, sclera: true, pupil: Black);
        });
        return Lift(b);
    }

    /// <summary>
    /// Buburoza: a larger ladybird, its red wing cases opened out like the petals of a wild rose with pale gold spots, thin
    /// pink wings shimmering under them and a little bouquet of wild flowers held in its front legs.
    /// </summary>
    private static PokeBuilder Cp1Buburoza()
    {
        var b = new PokeBuilder("Buburoza", 0.74f, BodyPlan.Biped, V(0, 0.3f, -0.02f)) { Coat = Shell }.Hover();
        var red = Rgb(206, 72, 56);
        var deepRed = Rgb(170, 50, 44);
        var gold = Rgb(232, 206, 120);
        var black = Rgb(54, 50, 58);
        var pink = Rgb(246, 196, 214);
        var stem = Rgb(96, 150, 72);

        // A dark round body, its belly ringed
        var bc = V(0, 0.28f, -0.02f);
        var br = V(0.11f, 0.13f, 0.1f);
        b.Ell(Body, bc, br, black);
        for (int i = 0; i < 3; i++)
            b.PaintEll(Body, bc + V(0, -0.02f - i * 0.04f, 0.08f), V(0.1f, 0.004f, 0.05f), Rgb(84, 78, 88), soft: 0.003f);
        // Legs dangling
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, bc + V(0.05f * s, -0.1f, 0.02f));
            b.Limb(leg, bc + V(0.05f * s, -0.1f, 0.02f), bc + V(0.07f * s, -0.2f, 0.05f), 0.016f, 0.013f, black);
            b.Limb(leg, bc + V(0.07f * s, -0.2f, 0.05f), bc + V(0.07f * s, -0.26f, 0.02f), 0.013f, 0.012f, black);
        });

        // The wing cases opened like petals of a rose, gold spots on them; pink wings spread under them
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, bc + V(0.05f * s, 0.1f, -0.06f));
            var pc = bc + V(0.14f * s, 0.12f, -0.1f);
            var pr = V(0.06f, 0.15f, 0.12f);
            var turn = V(-20f, 30f * s, 35f * s);
            b.Ell(wing, pc, pr, red, turn, Shell, 0.02f);
            b.Ell(wing, pc + V(0.02f * s, 0.02f, -0.06f), V(0.04f, 0.13f, 0.08f), deepRed, turn + V(0, 20f * s, 15f * s), Shell, 0.02f);
            b.Ell(wing, pc + V(0.06f * s, 0.13f, -0.02f), V(0.035f, 0.05f, 0.07f), red, turn + V(-40f, 0, 30f * s), Shell, 0.015f);
            foreach (var d in new[] { V(0.8f * s, 0.6f, 0.2f), V(0.9f * s, -0.1f, 0.3f), V(0.6f * s, 0.3f, -0.7f) })
            {
                var at = Out(pc, pr, turn, d);
                var n = Vector3.Normalize(at - pc);
                b.Mark(wing, at, n, 0.022f, 0.022f, gold);
            }
            var root = bc + V(0.06f * s, 0.06f, -0.08f);
            Frond(b, wing, root, root + V(0.32f * s, -0.04f, -0.08f), 0.07f, pink, V(0, 1f, 0.3f), 0.18f, Glow, 0.01f);
            Frond(b, wing, root, root + V(0.26f * s, -0.14f, -0.02f), 0.055f, pink, V(0, 1f, 0.4f), 0.18f, Glow, 0.01f);
            for (int i = 0; i < 2; i++)
                b.PaintEll(wing, root + V((0.12f + 0.08f * i) * s, -0.02f - 0.01f * i, -0.04f), V(0.003f, 0.06f, 0.06f), Rgb(222, 150, 178), V(0, 0, 20f * s), 0.003f);
        });

        // Arms hugging the bouquet: stems and a handful of wild flowers
        var posy = bc + V(-0.03f, 0.02f, 0.14f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.08f * s, 0.06f, 0.06f));
            b.Limb(arm, bc + V(0.08f * s, 0.06f, 0.06f), bc + V(0.09f * s, -0.02f, 0.13f), 0.016f, 0.013f, black);
            b.Limb(arm, bc + V(0.09f * s, -0.02f, 0.13f), posy + V(0.03f * s, -0.03f, 0.02f), 0.013f, 0.012f, black);
            if (s < 0)
            {
                foreach (var (dx, dy) in new[] { (-0.03f, 0.08f), (0.0f, 0.1f), (0.03f, 0.08f), (-0.05f, 0.04f), (0.04f, 0.04f) })
                    b.Limb(arm, posy + V(0, -0.08f, 0), posy + V(dx, dy - 0.02f, 0.02f), 0.006f, 0.005f, stem, Leaf, 0.003f);
                var flowers = new[] { (V(-0.03f, 0.08f, 0.03f), Rgb(250, 214, 80)), (V(0.0f, 0.1f, 0.025f), Rgb(240, 150, 180)), (V(0.03f, 0.08f, 0.03f), Rgb(250, 250, 244)),
                    (V(-0.05f, 0.04f, 0.03f), Rgb(240, 150, 180)), (V(0.04f, 0.04f, 0.03f), Rgb(250, 214, 80)) };
                foreach (var (at, color) in flowers)
                    FlowerHead(b, arm, posy + at, V(0, 0.4f, 1f), 5, 0.022f, 0.011f, color, Rgb(244, 190, 60), 0.008f);
            }
        });

        int head = b.Head(bc + V(0, 0.12f, 0.04f));
        var hc = bc + V(0, 0.2f, 0.06f);
        var hr = V(0.095f, 0.085f, 0.085f);
        b.Ell(head, hc, hr, black);
        PokeBuilder.Both(s =>
        {
            var cheek = Out(hc, hr, default, V(0.7f * s, -0.35f, 0.62f));
            b.Mark(head, cheek, Outward(hc, hr, cheek), 0.018f, 0.018f, Rgb(244, 236, 214));
            // Antennae ending in little curls
            var root = hc + V(0.03f * s, hr.Y * 0.8f, -0.01f);
            b.Tube(head, Smooth(3, root, root + V(0.03f * s, 0.08f, 0), root + V(0.06f * s, 0.12f, 0.03f), root + V(0.08f * s, 0.11f, 0.05f),
                root + V(0.075f * s, 0.095f, 0.04f)), 0.008f, 0.007f, black, blend: 0f);
            var eye = Out(hc, hr, default, V(0.4f * s, 0.12f, 0.9f));
            b.Eye(head, eye, Outward(hc, hr, eye), 0.026f, Rgb(100, 74, 60));
        });
        b.Mark(head, Out(hc, hr, default, V(0, -0.35f, 1f)), V(0, -0.2f, 1f), 0.018f, 0.01f, Rgb(16, 14, 18), MarkShape.Smile);
        return Lift(b);
    }

    // ------------------------------------------------------------------ Arichip and Aricorn

    /// <summary>
    /// The leaf hedgehogs: Arichip small and brown, its spines dry autumn leaves of yellow, orange and brown with one apple
    /// caught on them; Aricorn big and kindly, grey of snout, its back a little orchard of green leaves carrying apples,
    /// yellow plums and pears to share.
    /// </summary>
    private static PokeBuilder Cp1Hedgehog(bool orchard)
    {
        float k = orchard ? 1.6f : 1f;
        var b = new PokeBuilder(orchard ? "Aricorn" : "Arichip", orchard ? 0.84f : 0.5f, BodyPlan.Quadruped, V(0, 0.15f, -0.03f) * k) { Coat = Fur };
        var brown = Rgb(176, 124, 86);
        var face = Rgb(196, 146, 104);
        var snout = orchard ? Rgb(150, 150, 156) : face;
        Cp1Legs(b, 0.075f * k, 0.09f * k, 0.08f * k, -0.1f * k, 0.03f * k, brown, brown, null, orchard);

        var bc = V(0, 0.16f, -0.04f) * k;
        var br = V(0.16f, 0.12f, 0.18f) * k;
        b.Ell(Body, bc, br, brown);
        var colors = orchard
            ? new[] { Rgb(92, 150, 70), Rgb(74, 130, 60), Rgb(110, 166, 80), Rgb(92, 150, 70), Rgb(226, 144, 52), Rgb(240, 186, 70) }
            : new[] { Rgb(236, 186, 60), Rgb(222, 132, 48), Rgb(170, 100, 50), Rgb(244, 206, 84), Rgb(206, 110, 44) };
        Cp1LeafCoat(b, Body, bc + V(0, 0.02f, -0.01f) * k, br * 1.02f, orchard ? 110 : 80, 0.1f * k, 0.035f * k, colors, 0.55f, -0.2f, orchard ? 7u : 3u);

        if (orchard)
        {
            foreach (var (d, color, pear) in new[] { (V(0.1f, 1f, 0.1f), Rgb(208, 56, 48), false), (V(-0.6f, 0.75f, -0.2f), Rgb(208, 56, 48), false),
                         (V(0.75f, 0.4f, -0.5f), Rgb(208, 56, 48), false), (V(-0.3f, 0.9f, 0.4f), Rgb(240, 206, 70), false), (V(0.5f, 0.8f, -0.4f), Rgb(240, 206, 70), false),
                         (V(-0.8f, 0.4f, 0.1f), Rgb(240, 206, 70), false), (V(0.85f, 0.35f, 0.2f), Rgb(184, 200, 76), true), (V(-0.45f, 0.6f, -0.75f), Rgb(184, 200, 76), true) })
            {
                var n = Vector3.Normalize(d);
                var at = bc + n * (br + V(0.06f, 0.06f, 0.06f) * k);
                Cp1Fruit(b, Body, at, n + V(0, 0.6f, 0), (pear ? 0.03f : 0.034f) * k, color, pear);
            }
        }
        else
        {
            var n = Vector3.Normalize(V(0.4f, 0.9f, -0.2f));
            Cp1Fruit(b, Body, bc + n * (br + V(0.055f, 0.055f, 0.055f)), n + V(0, 0.5f, 0), 0.032f, Rgb(208, 56, 48));
        }

        // The face out in front of the leaves: a pointed snout with a black nose, round ears
        int head = b.Head(bc + V(0, -0.01f, br.Z * 0.7f));
        var hc = bc + V(0, -0.02f, br.Z * 0.85f);
        var hr = V(0.1f, 0.085f, 0.09f) * k;
        b.Ell(head, hc, hr, face);
        var nose = hc + V(0, -0.025f, hr.Z + 0.06f * k);
        b.Spike(head, hc + V(0, -0.02f, hr.Z * 0.5f), nose, 0.05f * k, snout);
        b.Ell(head, nose + V(0, 0.003f, -0.004f) * k, V(0.018f, 0.015f, 0.015f) * k, Rgb(40, 36, 40), mat: Shell, blend: 0.006f);
        b.Mark(head, nose + V(0, -0.03f, -0.025f) * k, V(0, -0.5f, 1f), 0.02f * k, 0.01f * k, Rgb(110, 76, 56), MarkShape.Smile);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, hc + V(0.07f * s, 0.05f, -0.02f) * k);
            b.Ell(ear, hc + V(0.085f * s, 0.075f, -0.02f) * k, V(0.03f, 0.03f, 0.016f) * k, brown, V(0, 0, -25f * s));
            b.PaintEll(ear, hc + V(0.085f * s, 0.075f, -0.004f) * k, V(0.018f, 0.018f, 0.012f) * k, Rgb(136, 94, 64), V(0, 0, -25f * s));
            var at = Out(hc, hr, default, V(0.55f * s, 0.25f, 0.8f));
            b.Mark(head, Out(hc, hr, default, V(0.65f * s, -0.15f, 0.7f)), V(0.65f * s, -0.15f, 0.7f), 0.014f * k, 0.008f * k, Rgb(226, 150, 140));
            b.Eye(head, at, Outward(hc, hr, at), 0.017f * k, Rgb(50, 40, 36));
        });
        return b;
    }
}
