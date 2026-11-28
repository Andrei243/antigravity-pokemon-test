using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Carpathia, batch 7 (plan 46 · CP4 to CP8): Starcel to Liliacon (20 species), the delta, the sea, the salt mines and
// the caves (docs/plans/decisions/CP-dex-3-river-and-caves.md, Nos. 10121 to 10140). Our own designs from the briefs and
// the user's reference pictures; every helper here is named C7… so no other batch's can meet it.
internal static partial class PokemonModels
{
    private static string[] CarpathiaNames7() => new[]
    {
        "Starcel", "Starcarno", "Plaurel", "Plaurnaut", "Tantarel", "Tantaroi", "Hutulet", "Hutulmare", "Delfinel", "Delfinora",
        "Calutmar", "Calutregal", "Sarel", "Salinax", "Valvisor", "Valvoara", "Ghetel", "Stalaghet", "Liliacel", "Liliacon"
    };

    private static PokeBuilder? CreateCarpathia7(string species) => species switch
    {
        "STARCEL" => Starcel(),
        "STARCARNO" => Starcarno(),
        "PLAUREL" => Plaurel(),
        "PLAURNAUT" => Plaurnaut(),
        "TANTAREL" => C7Mosquito(false),
        "TANTAROI" => C7Mosquito(true),
        "HUTULET" => Hutulet(),
        "HUTULMARE" => Hutulmare(),
        "DELFINEL" => Delfinel(),
        "DELFINORA" => Delfinora(),
        "CALUTMAR" => Calutmar(),
        "CALUTREGAL" => Calutregal(),
        "SAREL" => Sarel(),
        "SALINAX" => Salinax(),
        "VALVISOR" => Valvisor(),
        "VALVOARA" => Valvoara(),
        "GHETEL" => Ghetel(),
        "STALAGHET" => Stalaghet(),
        "LILIACEL" => Liliacel(),
        "LILIACON" => Liliacon(),
        _ => null
    };

    // ------------------------------------------------------------------ helpers of this batch

    /// <summary>A wading bird's leg from the hip down to a foot of three long toes forward and one back, flat on the ground.</summary>
    private static void C7WaderLeg(PokeBuilder b, int bone, Vector3 hip, Vector3 ankle, Vector3 foot, float r, Color color)
    {
        b.Limb(bone, hip, ankle, r * 1.25f, r, color, Scales, 0.008f);
        b.Limb(bone, ankle, foot, r, r * 0.85f, color, Scales, 0.008f);
        b.Ell(bone, ankle, V(r * 1.3f, r * 1.3f, r * 1.3f), color, mat: Scales, blend: 0.006f);
        float toe = r * 0.6f;
        var ground = V(foot.X, toe, foot.Z);
        foreach (var (dx, dz) in new[] { (-0.55f, 1f), (0f, 1.15f), (0.55f, 1f), (0f, -0.6f) })
            b.Limb(bone, ground, ground + V(dx * r * 4.5f, 0, dz * r * 5.5f), toe, toe * 0.75f, color, Scales, 0.005f);
    }

    /// <summary>
    /// Folded wing feathers along a bird's side, from the shoulder back to the tail, in rows of fronds that lie flat
    /// against the body: the long coverts in <paramref name="dark"/>, a shorter row over them in <paramref name="light"/>.
    /// </summary>
    private static void C7FoldedWing(PokeBuilder b, int bone, Vector3 shoulder, Vector3 tip, float width, Color dark, Color light, float s)
    {
        Frond(b, bone, shoulder, tip, width, dark, V(s, 0.35f, 0), 0.24f);
        Frond(b, bone, shoulder + V(0.005f * s, 0.01f, 0), Vector3.Lerp(shoulder, tip, 0.62f) + V(0.01f * s, 0.01f, 0), width * 0.8f, light, V(s, 0.35f, 0), 0.26f);
        Frond(b, bone, Vector3.Lerp(shoulder, tip, 0.3f), tip + V(0.01f * s, -0.03f, -0.02f), width * 0.55f, dark, V(s, 0.2f, 0), 0.25f);
    }

    /// <summary>A strand of reed: a thin pointed blade from <paramref name="root"/> to <paramref name="tip"/>, flat across the way it faces.</summary>
    private static void C7Reed(PokeBuilder b, int bone, Vector3 root, Vector3 tip, float width, Color color) =>
        Blade(b, bone, root, tip, width, color, Vector3.Normalize(V(root.X + 0.01f, 0.05f, root.Z + 0.6f)), 0.5f, Leaf);

    /// <summary>A bulrush: a thin stalk and the brown velvet head at its top.</summary>
    private static void C7Bulrush(PokeBuilder b, int bone, Vector3 root, Vector3 top, float r, Color stalk, Color head)
    {
        b.Limb(bone, root, top, r, r * 0.8f, stalk, Leaf, 0.006f);
        var dir = Vector3.Normalize(top - root);
        b.Ell(bone, top + dir * r * 3.5f, V(r * 2.2f, r * 5f, r * 2.2f), head, Euler(dir), blend: 0.008f);
    }

    /// <summary>
    /// A mat of tangled roots: arcs laid over the surface of the ellipsoid at <paramref name="c"/>, each round an axis of
    /// its own, in two browns. The axes are worked out, never drawn, so it is the same every build.
    /// </summary>
    private static void C7Tangle(PokeBuilder b, int bone, Vector3 c, Vector3 r, int count, float tube, Color root, Color pale)
    {
        float golden = MathF.PI * (3f - MathF.Sqrt(5f));
        for (int i = 0; i < count; i++)
        {
            float y = 1f - 2f * (i + 0.5f) / count, ring = MathF.Sqrt(1f - y * y), a = i * golden;
            var axis = V(MathF.Sin(a) * ring, y * 0.6f, MathF.Cos(a) * ring);
            axis = Vector3.Normalize(axis);
            var u = Vector3.Normalize(Vector3.Cross(axis, MathF.Abs(axis.Y) > 0.9f ? Vector3.UnitX : Vector3.UnitY));
            var v = Vector3.Cross(axis, u);
            float from = (i * 1.3f) % MathF.Tau, span = 3.4f;
            var points = Enumerable.Range(0, 13).Select(k =>
            {
                float t = from + span * k / 12f;
                return c + (Out(c, r, default, u * MathF.Cos(t) + v * MathF.Sin(t)) - c) * 1.01f;
            }).ToArray();
            b.Tube(bone, points, tube, tube * 0.75f, i % 3 == 0 ? pale : root, blend: 0.006f);
        }
    }

    /// <summary>A root hanging from <paramref name="at"/> and curling a little as it goes down.</summary>
    private static void C7HangingRoot(PokeBuilder b, int bone, Vector3 at, float length, float r, float sway, Color color) =>
        b.Tube(bone, Smooth(3, at, at + V(sway * 0.5f, -length * 0.4f, sway * 0.2f), at + V(-sway * 0.3f, -length * 0.75f, sway * 0.5f),
            at + V(sway * 0.4f, -length, sway * 0.3f)), r, r * 0.45f, color, blend: 0.004f);

    /// <summary>A miner's oil lamp hanging from its bail at <paramref name="top"/>: an iron cap, glowing glass and a round foot.</summary>
    private static void C7Lamp(PokeBuilder b, int bone, Vector3 top, float s, Color glow)
    {
        var iron = Rgb(96, 78, 64);
        var rust = Rgb(140, 92, 58);
        // The bail, a ring standing over the cap, joined to it on both sides
        b.Torus(bone, top + V(0, -0.03f * s, 0), 0.03f * s, 0.0075f * s, iron, V(90f, 0, 0), mat: Metal, blend: 0.004f);
        var cap = top + V(0, -0.07f * s, 0);
        b.Limb(bone, cap + V(0, 0.022f * s, 0), cap + V(0, 0.044f * s, 0), 0.012f * s, 0.01f * s, iron, Metal);
        b.Ell(bone, cap, V(0.04f * s, 0.022f * s, 0.04f * s), iron, mat: Metal, blend: 0.006f);
        var glass = cap + V(0, -0.06f * s, 0);
        b.Ell(bone, glass, V(0.033f * s, 0.048f * s, 0.033f * s), glow, mat: Glow, blend: 0.004f);
        b.PaintEll(bone, glass + V(0, -0.005f * s, 0), V(0.018f * s, 0.026f * s, 0.04f * s), Rgb(255, 246, 190));
        // Two guard wires down the glass, the foot under it
        PokeBuilder.Both(k => b.Limb(bone, cap + V(0.03f * k * s, -0.01f * s, 0), glass + V(0.033f * k * s, -0.05f * s, 0), 0.007f * s, 0.007f * s, iron, Metal, 0.003f));
        var foot = glass + V(0, -0.06f * s, 0);
        b.Ell(bone, foot, V(0.045f * s, 0.018f * s, 0.045f * s), iron, mat: Metal, blend: 0.006f);
        b.PaintEll(bone, foot + V(0.02f * s, 0.005f * s, 0.03f * s), V(0.02f * s, 0.012f * s, 0.02f * s), rust);
    }

    // ------------------------------------------------------------------ Starcel and Starcarno

    /// <summary>Starcel: a scrawny grey heron chick, hunched and grumpy on long yellow legs, a scruffy crest and a long yellow beak.</summary>
    private static PokeBuilder Starcel()
    {
        var b = new PokeBuilder("Starcel", 0.5f, BodyPlan.Bird, V(0, 0.42f, -0.03f)) { Coat = Fur };
        var grey = Rgb(132, 146, 164);
        var slate = Rgb(96, 110, 134);
        var pale = Rgb(206, 216, 230);
        var yellow = Rgb(236, 196, 92);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.36f, -0.02f));
            C7WaderLeg(b, leg, V(0.05f * s, 0.36f, -0.02f), V(0.06f * s, 0.19f, -0.05f), V(0.065f * s, 0.02f, 0.0f), 0.014f, yellow);
        });
        // A round fluffy body, the breast pale and ragged
        var bc = V(0, 0.42f, -0.03f);
        var br = V(0.12f, 0.11f, 0.15f);
        b.Ell(Body, bc, br, grey);
        b.PaintEll(Body, bc + V(0, -0.01f, 0.1f), V(0.1f, 0.1f, 0.08f), pale);
        FurTufts(b, Body, bc + V(0, -0.01f, 0.03f), V(0.11f, 0.1f, 0.12f), 22, 0.04f, 0.02f, pale, 0.9f, -0.5f, 0.5f);
        FurTufts(b, Body, bc + V(0, 0.02f, -0.04f), V(0.11f, 0.1f, 0.12f), 16, 0.035f, 0.018f, grey, 1.1f, -0.2f, 0.3f);
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, bc + V(0.1f * s, 0.05f, 0.05f));
            C7FoldedWing(b, wing, bc + V(0.1f * s, 0.05f, 0.05f), bc + V(0.12f * s, -0.05f, -0.2f), 0.065f, slate, grey, s);
        });
        int tail = b.Tail(bc + V(0, -0.01f, -0.14f));
        foreach (float a in new[] { -18f, 0f, 18f })
            Frond(b, tail, bc + V(0, -0.01f, -0.13f), bc + V(MathF.Sin(a * Degree) * 0.05f, -0.06f, -0.24f), 0.03f, slate, V(0, 1f, 0.2f), 0.3f);

        // A short hunched neck and a big head, sunk between its shoulders
        b.Limb(Body, bc + V(0, 0.05f, 0.08f), bc + V(0, 0.13f, 0.12f), 0.065f, 0.055f, grey);
        int head = b.Head(bc + V(0, 0.13f, 0.12f));
        var c = bc + V(0, 0.19f, 0.13f);
        var r = V(0.075f, 0.07f, 0.075f);
        b.Ell(head, c, r, grey);
        b.PaintEll(head, c + V(0, -0.04f, 0.04f), V(0.06f, 0.035f, 0.05f), pale);
        // The long yellow beak, a darker line along its gape
        b.Spike(head, c + V(0, -0.012f, 0.055f), c + V(0, -0.05f, 0.27f), 0.026f, yellow, 0.75f, Shell);
        b.PaintEll(head, c + V(0, -0.024f, 0.16f), V(0.03f, 0.004f, 0.11f), PixelCanvas.Shadow(yellow, 0.35f), V(-9f, 0, 0), 0.005f);
        // A scruffy crest of loose feathers blown up and back
        foreach (var (x, up, back, w) in new[] { (0f, 0.13f, -0.06f, 0.026f), (-0.03f, 0.1f, -0.1f, 0.022f), (0.03f, 0.11f, -0.09f, 0.022f), (0.015f, 0.07f, -0.13f, 0.02f), (-0.02f, 0.12f, 0.01f, 0.018f) })
            b.Spike(head, c + V(x * 0.6f, 0.05f, -0.01f), c + V(x * 1.6f, 0.05f + up, back), w, PixelCanvas.Mix(grey, slate, 0.4f), 0.55f);
        b.Spike(head, c + V(0, 0.055f, 0.0f), c + V(0.01f, 0.16f, 0.03f), 0.016f, pale, 0.55f);
        // Grumpy eyes under heavy brows
        PokeBuilder.Both(s =>
        {
            var look = V(0.7f * s, 0.25f, 0.62f);
            b.Ell(head, Out(c, r, default, look + V(0, 0.32f, 0)), V(0.035f, 0.013f, 0.025f), slate, V(0, 0, -16f * s), blend: 0.016f);
            b.Eye(head, Out(c, r, default, look), look, 0.022f, Rgb(232, 220, 160), sclera: true, pupil: Rgb(50, 50, 60), glare: true);
        });
        return b;
    }

    /// <summary>Starcarno: a tall purple-grey heron on one leg, a long S of a neck, a black crest with two plumes trailing like ribbons and a dagger of a golden beak.</summary>
    private static PokeBuilder Starcarno()
    {
        var b = new PokeBuilder("Starcarno", 0.95f, BodyPlan.Bird, V(0, 0.6f, -0.06f)) { Coat = Fur };
        var violet = Rgb(130, 122, 160);
        var slate = Rgb(92, 106, 134);
        var pale = Rgb(214, 220, 236);
        var black = Rgb(44, 48, 62);
        var gold = Rgb(222, 178, 86);
        var leg = Rgb(208, 178, 120);

        // It stands on its right leg; the left is drawn up under it, the toes curled
        int standing = b.Leg(1f, V(0.045f, 0.54f, -0.05f));
        C7WaderLeg(b, standing, V(0.045f, 0.54f, -0.05f), V(0.05f, 0.27f, -0.07f), V(0.05f, 0.02f, -0.03f), 0.014f, leg);
        int raised = b.Leg(-1f, V(-0.045f, 0.54f, -0.05f));
        var knee = V(-0.05f, 0.4f, 0.06f);
        var claw = V(-0.045f, 0.31f, -0.02f);
        b.Limb(raised, V(-0.045f, 0.54f, -0.05f), knee, 0.017f, 0.014f, leg, Scales, 0.008f);
        b.Limb(raised, knee, claw, 0.014f, 0.012f, leg, Scales, 0.008f);
        b.Ell(raised, knee, V(0.018f, 0.018f, 0.018f), leg, mat: Scales, blend: 0.006f);
        foreach (float dx in new[] { -0.03f, 0f, 0.03f })
            b.Tube(raised, Smooth(2, claw, claw + V(dx, -0.02f, 0.05f), claw + V(dx * 1.2f, -0.06f, 0.04f)), 0.009f, 0.006f, leg, blend: 0.004f);

        var bc = V(0, 0.6f, -0.06f);
        var br = V(0.1f, 0.1f, 0.2f);
        b.Ell(Body, bc, br, violet, V(-12f, 0, 0));
        b.PaintEll(Body, bc + V(0, -0.06f, 0.08f), V(0.08f, 0.05f, 0.12f), slate);
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, bc + V(0.08f * s, 0.05f, 0.1f));
            C7FoldedWing(b, wing, bc + V(0.08f * s, 0.05f, 0.1f), bc + V(0.1f * s, -0.07f, -0.33f), 0.085f, slate, violet, s);
            b.PaintEll(wing, bc + V(0.1f * s, 0.04f, 0.04f), V(0.04f, 0.04f, 0.08f), pale);
        });
        int tail = b.Tail(bc + V(0, -0.04f, -0.2f));
        Frond(b, tail, bc + V(0, -0.04f, -0.19f), bc + V(0, -0.12f, -0.34f), 0.05f, slate, V(0, 1f, 0.3f), 0.3f);

        // The neck in an S, pale down its front, long feathers falling from its foot over the breast
        var neck = Smooth(3, bc + V(0, 0.04f, 0.16f), bc + V(0, 0.14f, 0.24f), bc + V(0, 0.27f, 0.15f), bc + V(0, 0.38f, 0.14f), bc + V(0, 0.45f, 0.2f));
        b.Tube(Body, neck, 0.05f, 0.033f, violet, blend: 0.01f);
        b.PaintEll(Body, bc + V(0, 0.2f, 0.24f), V(0.03f, 0.13f, 0.04f), pale, V(-15f, 0, 0));
        for (int i = 0; i < 5; i++)
        {
            float x = (i - 2) * 0.022f;
            var root = bc + V(x, 0.1f, 0.2f + MathF.Abs(x) * -0.6f);
            Blade(b, Body, root, root + V(x * 0.6f, -0.13f - (i % 2) * 0.03f, 0.03f), 0.022f, pale, V(0, 0, 1f), 0.35f);
        }
        int head = b.Head(neck[^1]);
        var c = neck[^1] + V(0, 0.02f, 0.03f);
        var r = V(0.048f, 0.046f, 0.062f);
        b.Ell(head, c, r, violet);
        b.PaintEll(head, c + V(0, -0.025f, 0.03f), V(0.04f, 0.02f, 0.04f), pale);
        // A black cap from the brow back, and its two long plumes trailing behind like ribbons
        b.PaintEll(head, c + V(0, 0.03f, -0.01f), V(0.055f, 0.03f, 0.066f), black);
        foreach (var (x, up, back) in new[] { (0f, 0.07f, -0.06f), (-0.02f, 0.05f, -0.08f), (0.02f, 0.055f, -0.07f) })
            b.Spike(head, c + V(x * 0.5f, 0.035f, -0.02f), c + V(x, 0.035f + up, back), 0.018f, black, 0.55f);
        PokeBuilder.Both(s =>
            b.Tube(head, Smooth(3, c + V(0.012f * s, 0.03f, -0.045f), c + V(0.02f * s, 0.05f, -0.16f), c + V(0.03f * s, 0.0f, -0.3f), c + V(0.045f * s, -0.07f, -0.4f)),
                0.014f, 0.009f, black, blend: 0.004f));
        // The beak: a long golden dagger
        b.Spike(head, c + V(0, -0.008f, 0.05f), c + V(0, -0.03f, 0.3f), 0.022f, gold, 0.75f, Shell);
        b.PaintEll(head, c + V(0, -0.017f, 0.17f), V(0.025f, 0.0035f, 0.12f), PixelCanvas.Shadow(gold, 0.35f), V(-5f, 0, 0), 0.004f);
        PokeBuilder.Both(s =>
        {
            var look = V(0.72f * s, 0.18f, 0.62f);
            b.Eye(head, Out(c, r, default, look), look, 0.014f, Rgb(236, 206, 90), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Plaurel and Plaurnaut

    /// <summary>Plaurel: a round mat of tangled roots and peat, green moss on it, a tuft of reeds and bulrushes for hair and two teal eyes peeking from a hollow.</summary>
    private static PokeBuilder Plaurel()
    {
        var b = new PokeBuilder("Plaurel", 0.45f, BodyPlan.Floating, V(0, 0.2f, 0)) { Coat = Fur }.Hover();
        var peat = Rgb(92, 72, 54);
        var root = Rgb(130, 104, 72);
        var pale = Rgb(160, 136, 98);
        var moss = Rgb(104, 140, 64);
        var reed = Rgb(88, 150, 70);
        var reedLight = Rgb(132, 182, 92);
        var rush = Rgb(124, 88, 58);

        var c = V(0, 0.2f, 0);
        var r = V(0.16f, 0.15f, 0.15f);
        b.Ell(Body, c, r, peat);
        C7Tangle(b, Body, c, r, 9, 0.014f, root, pale);
        foreach (var (x, y, z) in new[] { (0.08f, 0.31f, 0.06f), (-0.11f, 0.27f, -0.05f), (0.12f, 0.16f, -0.09f), (-0.06f, 0.12f, 0.12f), (0.02f, 0.33f, -0.08f) })
            b.PaintEll(Body, V(x, y, z), V(0.05f, 0.04f, 0.05f), moss, soft: 0.02f);
        // Roots hanging from under the mat
        foreach (var (x, z, len) in new[] { (0.06f, 0.05f, 0.09f), (-0.07f, 0.03f, 0.08f), (0.0f, -0.08f, 0.1f), (0.09f, -0.05f, 0.07f), (-0.04f, 0.09f, 0.06f), (-0.09f, -0.06f, 0.07f) })
            C7HangingRoot(b, Body, V(x, 0.08f, z), len, 0.011f, 0.02f, root);
        // A dark hollow in the roots, and two teal eyes in it
        var face = On(c, r, 0, 0.2f);
        b.PaintEll(Body, face, V(0.085f, 0.05f, 0.06f), Rgb(52, 42, 34), soft: 0.02f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.042f * s, 0.205f);
            b.Eye(Body, at, Outward(c, r, at), 0.018f, Rgb(80, 230, 210), white: Rgb(150, 250, 236), pupil: Rgb(20, 70, 70));
        });
        // The reeds, two clumps that sway apart, with three bulrushes among them
        PokeBuilder.Both(s =>
        {
            int clump = b.Part(s < 0 ? "reedsL" : "reedsR", Body, V(0.04f * s, 0.32f, 0), PokeRole.Leaf, s, s);
            for (int i = 0; i < 7; i++)
            {
                float a = (i - 3) * 0.32f;
                var foot = V((0.03f + 0.012f * (i % 3)) * s, 0.32f, -0.05f + 0.016f * i);
                var tip = foot + V((0.08f + 0.06f * MathF.Abs(a)) * s, 0.26f + 0.06f * ((i + 1) % 3) - MathF.Abs(a) * 0.08f, 0.05f * a);
                C7Reed(b, clump, foot, tip, 0.02f, i % 2 == 0 ? reed : reedLight);
            }
            C7Bulrush(b, clump, V(0.05f * s, 0.32f, 0.02f), V(0.16f * s, 0.52f, 0.05f), 0.007f, reedLight, rush);
            if (s > 0) C7Bulrush(b, clump, V(0.02f, 0.33f, -0.04f), V(0.05f, 0.58f, -0.09f), 0.007f, reedLight, rush);
        });
        b.Ell(Body, V(0, 0.33f, -0.01f), V(0.1f, 0.03f, 0.09f), moss, blend: 0.02f);
        return Lift(b);
    }

    /// <summary>
    /// Plaurnaut: a wide floating island of the delta, a raft of peat and roots with a broad, gentle face at its front,
    /// a forest of reeds and bulrushes, a willow and a pond of water lilies on its back, and two little birds nesting there.
    /// </summary>
    private static PokeBuilder Plaurnaut()
    {
        var b = new PokeBuilder("Plaurnaut", 1f, BodyPlan.Floating, V(0, 0.2f, 0)) { Coat = Fur }.Hover();
        var peat = Rgb(84, 68, 52);
        var root = Rgb(122, 98, 70);
        var pale = Rgb(150, 128, 94);
        var moss = Rgb(92, 132, 58);
        var bark = Rgb(116, 96, 72);
        var reed = Rgb(84, 146, 68);
        var reedLight = Rgb(128, 178, 88);
        var rush = Rgb(118, 84, 56);
        var willow = Rgb(164, 196, 96);
        var pond = Rgb(108, 150, 168);
        var pad = Rgb(90, 156, 82);
        var lily = Rgb(242, 190, 206);

        // The raft: wide and shallow, tangled roots over its sides and a mossy top with a pond in it
        var c = V(0, 0.2f, 0);
        var r = V(0.48f, 0.14f, 0.4f);
        b.Ell(Body, c, r, peat);
        b.Ell(Body, c + V(0, 0.08f, -0.02f), V(0.46f, 0.07f, 0.38f), moss, blend: 0.03f);
        C7Tangle(b, Body, c + V(0, -0.01f, 0), V(0.48f, 0.13f, 0.4f), 8, 0.022f, root, pale);
        b.PaintEll(Body, c + V(0.08f, 0.15f, -0.08f), V(0.2f, 0.03f, 0.15f), pond, soft: 0.02f);
        for (int i = 0; i < 12; i++)
        {
            float a = i * 0.52f + 0.2f;
            var at = c + V(MathF.Sin(a) * 0.34f, -0.08f, MathF.Cos(a) * 0.28f);
            C7HangingRoot(b, Body, at, 0.1f + 0.03f * (i % 3), 0.016f, 0.03f, i % 2 == 0 ? root : pale);
        }
        // Water lilies on the pond: flat pads, and a flower on two of them
        foreach (var (x, z, w) in new[] { (0.0f, -0.02f, 0.05f), (0.16f, -0.12f, 0.045f), (0.12f, 0.02f, 0.04f), (-0.02f, -0.16f, 0.04f) })
            b.Ell(Body, c + V(x, 0.155f, z), V(w, 0.008f, w), pad, mat: Leaf, blend: 0.004f);
        foreach (var (x, z) in new[] { (0.0f, -0.02f), (0.16f, -0.12f) })
        {
            var at = c + V(x, 0.17f, z);
            for (int k = 0; k < 5; k++)
            {
                float a = k * MathF.Tau / 5f;
                b.Ell(Body, at + V(MathF.Sin(a) * 0.014f, 0.006f, MathF.Cos(a) * 0.014f), V(0.012f, 0.008f, 0.018f), lily, V(20f, a / Degree, 0), Leaf, 0.003f);
            }
            b.Ell(Body, at + V(0, 0.012f, 0), V(0.008f, 0.008f, 0.008f), Rgb(250, 220, 90), blend: 0.003f);
        }

        // The face at the front: a brow of bark, a broad nose and a gentle smile, teal eyes
        int head = b.Head(c + V(0, 0, 0.3f));
        var fc = c + V(0, -0.01f, 0.27f);
        var fr = V(0.2f, 0.11f, 0.14f);
        b.Ell(head, fc, fr, bark);
        PokeBuilder.Both(s => b.Ell(head, fc + V(0.075f * s, 0.06f, 0.1f), V(0.07f, 0.022f, 0.035f), PixelCanvas.Mix(bark, moss, 0.4f), V(0, 0, -8f * s), blend: 0.02f));
        b.Ell(head, fc + V(0, -0.0f, 0.13f), V(0.04f, 0.035f, 0.03f), PixelCanvas.Shadow(bark, 0.1f), blend: 0.02f);
        b.Mark(head, On(fc, fr, 0, fc.Y - 0.055f), V(0, -0.2f, 1f), 0.08f, 0.02f, Rgb(54, 40, 30), MarkShape.Smile);
        PokeBuilder.Both(s =>
        {
            var at = On(fc, fr, 0.075f * s, fc.Y + 0.025f);
            b.Eye(head, at, Outward(fc, fr, at), 0.022f, Rgb(80, 230, 210), white: Rgb(150, 250, 236), pupil: Rgb(20, 70, 70));
        });
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < 3; i++)
                C7HangingRoot(b, head, fc + V((0.05f + 0.06f * i) * s, -0.07f, 0.08f - 0.02f * i), 0.07f, 0.013f, 0.02f * s, root);
        });

        // Reeds round the edge and bulrushes among them, in two clumps that sway
        PokeBuilder.Both(s =>
        {
            int clump = b.Part(s < 0 ? "reedsL" : "reedsR", Body, c + V(0.25f * s, 0.14f, 0), PokeRole.Leaf, s, s);
            for (int i = 0; i < 9; i++)
            {
                float t = i / 8f;
                var foot = c + V((0.2f + 0.14f * MathF.Sin(t * 3f)) * s, 0.13f, 0.16f - 0.36f * t);
                var tip = foot + V(0.05f * s * (i % 2 == 0 ? 1f : -0.4f), 0.22f + 0.07f * (i % 3), 0.03f * (i % 2 == 0 ? 1 : -1));
                C7Reed(b, clump, foot, tip, 0.028f, i % 2 == 0 ? reed : reedLight);
            }
            C7Bulrush(b, clump, c + V(0.28f * s, 0.13f, 0.06f), c + V(0.3f * s, 0.44f, 0.08f), 0.01f, reedLight, rush);
            C7Bulrush(b, clump, c + V(0.2f * s, 0.13f, -0.2f), c + V(0.24f * s, 0.46f, -0.22f), 0.01f, reedLight, rush);
        });
        // A willow sapling at the back, its branches drooping in curtains of leaves
        int tree = b.Part("willow", Body, c + V(-0.12f, 0.15f, -0.18f), PokeRole.Leaf, 0.5f);
        var trunkTop = c + V(-0.1f, 0.52f, -0.2f);
        b.Limb(tree, c + V(-0.12f, 0.12f, -0.18f), trunkTop, 0.035f, 0.022f, Rgb(120, 92, 66), Shell);
        foreach (var (dx, dz) in new[] { (0.14f, 0.02f), (-0.13f, 0.03f), (0.02f, -0.12f), (0.03f, 0.12f) })
        {
            var end = trunkTop + V(dx, 0.08f, dz);
            b.Limb(tree, trunkTop - V(0, 0.06f, 0), end, 0.016f, 0.01f, Rgb(120, 92, 66), Shell);
            b.Ell(tree, end + V(0, -0.02f, 0), V(0.1f, 0.06f, 0.1f), willow, mat: Leaf, blend: 0.03f);
            for (int k = 0; k < 3; k++)
            {
                float a = k * 2.1f + dx * 10f;
                var drop = end + V(MathF.Sin(a) * 0.07f, -0.04f, MathF.Cos(a) * 0.07f);
                Frond(b, tree, drop, drop + V(MathF.Sin(a) * 0.03f, -0.15f, MathF.Cos(a) * 0.03f), 0.03f, PixelCanvas.Shadow(willow, 0.1f), V(MathF.Sin(a), 0, MathF.Cos(a)), 0.4f, Leaf);
            }
        }
        b.Ell(tree, trunkTop + V(0, 0.1f, 0), V(0.12f, 0.07f, 0.12f), PixelCanvas.Light1(willow, 0.1f), mat: Leaf, blend: 0.03f);
        // Two small birds nesting on its back
        foreach (var (at, col, face) in new[] { (c + V(-0.3f, 0.19f, 0.1f), Rgb(110, 160, 220), 0.6f), (c + V(0.3f, 0.19f, -0.16f), Rgb(236, 150, 90), -0.4f) })
        {
            b.Ell(Body, at, V(0.03f, 0.026f, 0.034f), col, V(0, face / Degree, 0), blend: 0.01f);
            var hc = at + V(MathF.Sin(face) * 0.025f, 0.028f, MathF.Cos(face) * 0.025f);
            b.Ell(Body, hc, V(0.019f, 0.019f, 0.019f), col, blend: 0.008f);
            b.Spike(Body, hc + V(MathF.Sin(face) * 0.016f, 0, MathF.Cos(face) * 0.016f), hc + V(MathF.Sin(face) * 0.04f, -0.004f, MathF.Cos(face) * 0.04f), 0.008f, Rgb(240, 196, 80), mat: Shell);
        }
        return Lift(b);
    }

    // ------------------------------------------------------------------ Tantarel and Tantaroi

    /// <summary>
    /// Tantarel, a spindly mosquito with a striped grey body, clear wings, long legs, feathered feelers, big green eyes and
    /// a long proboscis; and Tantaroi, the swarm of them in the shape of one great fierce mosquito, its outline studded
    /// with the little ones that make it, washed violet where they glow.
    /// </summary>
    private static PokeBuilder C7Mosquito(bool swarm)
    {
        float k = swarm ? 2.2f : 1f;
        var b = new PokeBuilder(swarm ? "Tantaroi" : "Tantarel", swarm ? 0.9f : 0.42f, BodyPlan.Bird, V(0, 0.3f, 0) * k) { Coat = Shell }.Hover();
        var grey = Rgb(104, 108, 118);
        var dark = Rgb(58, 60, 70);
        var band = swarm ? Rgb(126, 92, 164) : Rgb(132, 110, 160);
        var green = Rgb(132, 200, 104);
        var wing = swarm ? Rgb(214, 200, 240) : Rgb(226, 232, 242);
        var violet = Rgb(170, 120, 236);

        // Six long legs from under the thorax, three on each side, banded pale at the joints
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.03f * s, 0.27f, 0.01f) * k);
            foreach (var (dz, reach) in new[] { (0.035f, 0.12f), (0.0f, 0.16f), (-0.035f, 0.13f) })
            {
                var hip = V(0.025f * s, 0.27f, dz) * k;
                var knee = V((0.06f + reach * 0.5f) * s, 0.2f, dz * 2.2f) * k;
                var foot = V((0.07f + reach * 0.55f) * s, 0.02f, dz * 2.8f) * k;
                b.Limb(leg, hip, knee, 0.0075f * k, 0.0065f * k, dark, blend: 0.004f);
                b.Limb(leg, knee, foot, 0.0065f * k, 0.005f * k, dark, blend: 0.004f);
                b.PaintEll(leg, Vector3.Lerp(knee, foot, 0.4f), V(0.012f, 0.012f, 0.012f) * k, Rgb(200, 204, 214), soft: 0.004f * k);
            }
        });
        // The thorax and the long striped abdomen hanging back from it
        var tc = V(0, 0.3f, 0) * k;
        b.Ell(Body, tc, V(0.048f, 0.05f, 0.055f) * k, grey);
        var ac = V(0, 0.27f, -0.13f) * k;
        var ar = V(0.045f, 0.045f, 0.1f) * k;
        b.Ell(Body, ac, ar, grey, V(-14f, 0, 0));
        for (int i = 0; i < 4; i++)
            b.PaintTorus(Body, ac + V(0, 0.008f * (1.5f - i), 0.06f - 0.035f * i) * k, 0.044f * k, 0.008f * k, i % 2 == 0 ? dark : band, V(76f, 0, 0), 1f, 1f);
        b.PaintEll(Body, ac + V(0, -0.02f, -0.08f) * k, V(0.03f, 0.022f, 0.03f) * k, green);
        b.Limb(Body, tc + V(0, 0, -0.03f) * k, ac + V(0, 0.01f, 0.06f) * k, 0.03f * k, 0.03f * k, grey);

        // Clear wings swept up and back, veined
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.03f * s, 0.34f, -0.01f) * k);
            var root = V(0.03f * s, 0.34f, -0.01f) * k;
            var tip = V(0.17f * s, 0.46f, -0.14f) * k;
            Frond(b, w, root, tip, 0.045f * k, wing, V(0.25f * s, 1f, 0.1f), 0.18f, Shell);
            Frond(b, w, root + V(0, -0.01f, -0.01f) * k, V(0.15f * s, 0.38f, -0.18f) * k, 0.035f * k, wing, V(0.2f * s, 1f, 0.15f), 0.18f, Shell);
            b.PaintEll(w, Vector3.Lerp(root, tip, 0.55f), V(0.08f, 0.006f, 0.012f) * k, PixelCanvas.Shadow(wing, 0.25f), Euler(tip - root), 0.003f * k);
            if (swarm) b.PaintEll(w, tip, V(0.06f, 0.04f, 0.06f) * k, violet, soft: 0.02f * k);
        });

        // The head: big green eyes, a sneer, feathered feelers and the long proboscis
        int head = b.Head(V(0, 0.32f, 0.05f) * k);
        var c = V(0, 0.33f, 0.085f) * k;
        var r = V(0.042f, 0.04f, 0.038f) * k;
        b.Ell(head, c, r, grey);
        b.Spike(head, c + V(0, -0.015f, 0.03f) * k, c + V(0, -0.13f, 0.17f) * k, 0.011f * k, dark, mat: Shell);
        if (!swarm) b.PaintEll(head, c + V(0, -0.11f, 0.15f) * k, V(0.015f, 0.025f, 0.02f) * k, band);
        PokeBuilder.Both(s =>
        {
            var root = c + V(0.015f * s, 0.035f, 0.0f) * k;
            var tip = c + V(0.05f * s, 0.11f, -0.02f) * k;
            b.Limb(head, root, tip, 0.006f * k, 0.005f * k, dark, blend: 0.004f);
            for (int i = 1; i <= 3; i++)
            {
                var at = Vector3.Lerp(root, tip, 0.25f * i);
                b.Spike(head, at, at + V(0.02f * s, 0.008f, 0.012f) * k, 0.005f * k, dark, blend: 0.003f);
                b.Spike(head, at, at + V(-0.012f * s, 0.012f, 0.014f) * k, 0.005f * k, dark, blend: 0.003f);
            }
            var look = V(0.62f * s, 0.15f, 0.75f);
            b.Ell(head, Out(c, r, default, look + V(0, 0.45f, -0.1f)), V(0.022f, 0.007f, 0.015f) * k, dark, V(0, 0, -22f * s), blend: 0.008f * k);
            b.Eye(head, Out(c, r, default, look), look, 0.017f * k, green, sclera: true, white: Rgb(200, 236, 120), pupil: Rgb(40, 50, 40), glare: true);
        });

        if (swarm)
        {
            // The swarm: the little ones studding its body and abdomen, each a dark speck with a pale pair of wings, and
            // a violet glow where they gather thickest
            float golden = MathF.PI * (3f - MathF.Sqrt(5f));
            foreach (var (bc, br, count, turn) in new[] { (tc, V(0.048f, 0.05f, 0.055f) * k, 16, V(0, 0, 0)), (ac, ar, 30, V(-14f, 0, 0)) })
            {
                for (int i = 0; i < count; i++)
                {
                    float y = 1f - 2f * (i + 0.5f) / count;
                    float ring = MathF.Sqrt(1f - y * y), a = i * golden;
                    var n = V(MathF.Sin(a) * ring, y, MathF.Cos(a) * ring);
                    var at = Out(bc, br, turn, n);
                    var outward = Vector3.Normalize(n / br);
                    b.Ell(Body, at, V(0.006f, 0.006f, 0.011f) * k, dark, Euler(Vector3.Cross(outward, V(0.3f, 1f, 0.2f))), blend: 0.004f * k);
                    b.Ell(Body, at + outward * 0.005f * k, V(0.009f, 0.0035f, 0.006f) * k, wing, Euler(Vector3.Cross(outward, V(1f, 0.2f, 0.4f))), Shell, 0.003f * k);
                }
                b.PaintEll(Body, bc, br * 0.9f, PixelCanvas.Mix(grey, violet, 0.25f), turn, 0.02f * k);
            }
            foreach (var at in new[] { ac + V(0.04f, 0.03f, -0.05f) * k, ac + V(-0.04f, -0.02f, 0.03f) * k, tc + V(0.03f, 0.04f, 0.0f) * k })
                b.PaintEll(Body, at, V(0.03f, 0.03f, 0.04f) * k, violet, soft: 0.015f * k);
            // Stragglers clinging to its legs' ends and its abdomen's tip
            foreach (var at in new[] { V(0.14f, 0.05f, 0.08f) * k, V(-0.15f, 0.05f, -0.1f) * k, ac + V(0, -0.03f, -0.1f) * k })
            {
                b.Ell(Root, at, V(0.012f, 0.009f, 0.018f) * k, dark, blend: 0.006f * k);
                PokeBuilder.Both(s => b.Ell(Root, at + V(0.012f * s, 0.01f, 0) * k, V(0.013f, 0.003f, 0.008f) * k, wing, V(0, 0, 25f * s), Shell, 0.003f * k));
            }
        }
        return Lift(b);
    }

    // ------------------------------------------------------------------ Hutulet and Hutulmare

    /// <summary>A pony's leg from the hip to its hoof, darker below the knee, with a feathered fetlock over the hoof.</summary>
    private static void C7PonyLeg(PokeBuilder b, int bone, Vector3 hip, float length, float r, Color coat, Color lower, Color hoof, float bars)
    {
        var knee = hip + V(0, -length * 0.5f, 0.005f);
        var fetlock = V(hip.X, r * 1.4f, hip.Z + 0.01f);
        b.Limb(bone, hip, knee, r * 1.25f, r, coat);
        b.Limb(bone, knee, fetlock, r, r * 0.9f, coat);
        b.PaintEll(bone, Vector3.Lerp(knee, fetlock, 0.65f), V(r * 1.4f, (fetlock - knee).Length() * 0.55f, r * 1.4f), lower, soft: r * 0.5f);
        b.Ell(bone, V(hip.X, r * 0.5f, hip.Z + 0.012f), V(r * 1.12f, r * 0.55f, r * 1.2f), hoof, mat: Shell, blend: 0.01f);
        FurTufts(b, bone, V(hip.X, r * 1.45f, hip.Z + 0.01f), V(r, r * 0.6f, r), 8, r * 0.7f, r * 0.35f, lower, 1.2f, -0.3f, 1.2f);
        for (int i = 0; i < (int)bars; i++)
            b.PaintEll(bone, Vector3.Lerp(knee, hip, 0.1f + 0.22f * i), V(r * 1.5f, r * 0.13f, r * 1.5f), lower, V(0, 0, 8f), r * 0.12f);
    }

    /// <summary>A woven folk blanket over the back: red, a black border, and white zigzags and diamonds down each side, tassels along its hem.</summary>
    private static void C7Blanket(PokeBuilder b, Vector3 bc, Vector3 br, float lift, Color red, Color black, Color white, int tassels)
    {
        var c = bc + V(0, lift, 0);
        var r = br * V(1.06f, 0.75f, 0.55f);
        b.Ell(Body, c, r, red, mat: Fur, blend: 0.012f);
        b.PaintEll(Body, c + V(0, -r.Y * 0.65f, 0), V(r.X * 1.1f, r.Y * 0.18f, r.Z * 1.1f), black, soft: 0.008f);
        PokeBuilder.Both(s =>
        {
            for (int i = -1; i <= 1; i++)
            {
                var at = Out(c, r, default, V(s, 0.15f, i * 0.55f));
                b.Mark(Body, at, V(s, 0.15f, i * 0.3f), r.Z * 0.17f, r.Y * 0.2f, white, MarkShape.Diamond);
            }
            var stripe = Out(c, r, default, V(s, -0.35f, 0));
            b.Mark(Body, stripe, V(s, -0.3f, 0), r.Z * 0.75f, r.Y * 0.08f, white, MarkShape.Zigzag);
            for (int i = 0; i < tassels; i++)
            {
                float t = (i + 0.5f) / tassels;
                var root = Out(c, r, default, V(s, -0.62f, -0.85f + 1.7f * t));
                b.Spike(Body, root, root + V(0.004f * s, -r.Y * 0.4f, 0), r.Y * 0.09f, i % 2 == 0 ? white : black, blend: 0.004f);
            }
        });
    }

    /// <summary>Hutulet: a shaggy bay foal with a dark stripe down its back, a black mane falling in its eyes, short strong legs and a red and white woven blanket over its back.</summary>
    private static PokeBuilder Hutulet()
    {
        var b = new PokeBuilder("Hutulet", 0.6f, BodyPlan.Quadruped, V(0, 0.34f, -0.02f)) { Coat = Fur };
        var bay = Rgb(146, 86, 52);
        var dark = Rgb(62, 44, 38);
        var black = Rgb(38, 34, 38);
        var muzzle = Rgb(92, 78, 76);
        var red = Rgb(170, 48, 46);
        var cream = Rgb(240, 226, 206);

        foreach (var (z, front) in new[] { (0.13f, true), (-0.17f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.1f * s, 0.27f, z), front);
                C7PonyLeg(b, leg, V(0.1f * s, 0.27f, z), 0.25f, 0.052f, bay, dark, Rgb(74, 64, 62), 0);
            });
        var bc = V(0, 0.34f, -0.02f);
        var br = V(0.16f, 0.14f, 0.24f);
        b.Ell(Body, bc, br, bay);
        FurTufts(b, Body, bc + V(0, -0.02f, 0.04f), br * 0.95f, 24, 0.04f, 0.022f, bay, 0.95f, -0.85f, 0.6f);
        b.PaintEll(Body, bc + V(0, 0.13f, 0), V(0.03f, 0.03f, 0.24f), dark);
        C7Blanket(b, bc + V(0, 0, -0.03f), br, 0.05f, red, black, cream, 5);

        // The tail, a bushy black brush
        int tail = b.Tail(bc + V(0, 0.06f, -0.22f));
        b.Tube(tail, Smooth(3, bc + V(0, 0.06f, -0.22f), bc + V(0, 0.04f, -0.3f), bc + V(0.02f, -0.06f, -0.33f), bc + V(0.04f, -0.13f, -0.3f)), 0.04f, 0.026f, black, blend: 0.01f);
        FurTufts(b, tail, bc + V(0.02f, -0.04f, -0.31f), V(0.035f, 0.07f, 0.035f), 10, 0.035f, 0.016f, black, 1.2f, -0.9f, 0.8f);

        // A short thick neck to a big head
        b.Limb(Body, bc + V(0, 0.05f, 0.16f), bc + V(0, 0.15f, 0.24f), 0.1f, 0.085f, bay);
        int head = b.Head(bc + V(0, 0.15f, 0.25f));
        var c = bc + V(0, 0.22f, 0.3f);
        var r = V(0.1f, 0.095f, 0.1f);
        b.Ell(head, c, r, bay);
        var mc = c + V(0, -0.055f, 0.09f);
        var mr = V(0.07f, 0.055f, 0.065f);
        b.Ell(head, mc, mr, muzzle, blend: 0.03f);
        PokeBuilder.Both(s => b.Mark(head, Out(mc, mr, default, V(0.4f * s, 0.2f, 1f)), V(0.4f * s, 0.2f, 1f), 0.011f, 0.007f, Rgb(40, 32, 32)));
        b.Mark(head, Out(mc, mr, default, V(0, -0.35f, 1f)), V(0, -0.3f, 1f), 0.035f, 0.012f, Rgb(70, 40, 40), MarkShape.Smile);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.06f * s, 0.07f, -0.02f));
            b.Spike(ear, c + V(0.06f * s, 0.07f, -0.02f), c + V(0.08f * s, 0.15f, -0.01f), 0.03f, bay, 0.55f);
            var look = V(0.62f * s, 0.12f, 0.78f);
            b.Eye(head, Out(c, r, default, look), look, 0.02f, Rgb(54, 40, 34));
        });
        // The mane: a thick black forelock falling between its eyes and over to one side, and long locks down the neck
        b.Ell(head, c + V(0, 0.08f, 0.0f), V(0.08f, 0.04f, 0.075f), black, blend: 0.02f);
        foreach (var (x, len, tilt) in new[] { (-0.012f, 0.075f, 0.3f), (0.01f, 0.06f, -0.15f), (-0.03f, 0.045f, 0.55f) })
            b.Spike(head, c + V(x, 0.085f, 0.05f), c + V(x - tilt * 0.06f, 0.085f - len, 0.105f), 0.022f, black, 0.55f);
        for (int i = 0; i < 6; i++)
        {
            float t = i / 5f;
            var root = Vector3.Lerp(c + V(0, 0.07f, -0.06f), bc + V(0, 0.12f, 0.13f), t);
            PokeBuilder.Both(s => b.Spike(head, root, root + V((0.07f + 0.02f * (i % 2)) * s, -0.06f - 0.02f * (i % 3), -0.01f), 0.026f, black, 0.5f, blend: 0.01f));
        }
        return b;
    }

    /// <summary>
    /// Hutulmare: a strong, compact dun mountain horse, a black mane and tail, a stripe down its spine and bars on its
    /// legs, an embroidered folk saddle blanket with tassels and a breast band, and wooden saddlebags.
    /// </summary>
    private static PokeBuilder Hutulmare()
    {
        var b = new PokeBuilder("Hutulmare", 1f, BodyPlan.Quadruped, V(0, 0.5f, -0.02f)) { Coat = Fur };
        var dun = Rgb(152, 98, 60);
        var dark = Rgb(66, 46, 38);
        var black = Rgb(38, 34, 38);
        var muzzle = Rgb(96, 80, 78);
        var red = Rgb(160, 40, 42);
        var cream = Rgb(240, 226, 206);
        var wood = Rgb(132, 94, 60);
        var iron = Rgb(110, 110, 118);

        foreach (var (z, front) in new[] { (0.2f, true), (-0.24f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.12f * s, 0.42f, z), front);
                C7PonyLeg(b, leg, V(0.12f * s, 0.42f, z), 0.4f, 0.058f, dun, dark, Rgb(70, 62, 62), 3);
            });
        var bc = V(0, 0.52f, -0.02f);
        var br = V(0.18f, 0.16f, 0.34f);
        b.Ell(Body, bc, br, dun);
        b.PaintEll(Body, bc + V(0, 0.155f, 0), V(0.028f, 0.03f, 0.34f), dark);
        C7Blanket(b, bc + V(0, 0, -0.05f), V(0.18f, 0.16f, 0.3f), 0.05f, red, black, cream, 7);
        // The saddle's horn, and the two wooden saddlebags over the blanket, strapped and buckled
        b.Limb(Body, bc + V(0, 0.18f, 0.08f), bc + V(0, 0.24f, 0.1f), 0.02f, 0.025f, wood, Shell);
        b.Ell(Body, bc + V(0, 0.255f, 0.105f), V(0.03f, 0.018f, 0.025f), wood, mat: Shell, blend: 0.006f);
        b.Box(Body, bc + V(0, 0.22f, -0.12f), V(0.09f, 0.045f, 0.06f), 0.012f, wood, mat: Shell, blend: 0.012f);
        PokeBuilder.Both(s =>
        {
            var at = bc + V(0.2f * s, 0.07f, -0.09f);
            b.Box(Body, at, V(0.045f, 0.075f, 0.085f), 0.012f, wood, V(0, 0, -6f * s), Shell, 0.012f);
            b.PaintEll(Body, at + V(0.045f * s, 0.035f, 0), V(0.01f, 0.012f, 0.09f), PixelCanvas.Shadow(wood, 0.35f), soft: 0.005f);
            foreach (float dz in new[] { -0.045f, 0.045f })
            {
                b.PaintEll(Body, at + V(0.045f * s, -0.01f, dz), V(0.01f, 0.08f, 0.012f), dark, soft: 0.004f);
                b.Mark(Body, at + V(0.048f * s, 0.0f, dz), V(s, 0, 0), 0.012f, 0.012f, iron, MarkShape.Ring);
            }
        });
        // A breast band, embroidered and tasselled
        var collar = bc + V(0, 0.03f, 0.28f);
        b.Torus(Body, collar, 0.14f, 0.018f, red, V(68f, 0, 0), 1.15f, 1f, blend: 0.008f);
        for (int i = -3; i <= 3; i++)
        {
            float a = i * 0.32f;
            var root = collar + V(MathF.Sin(a) * 0.15f, -MathF.Cos(a) * 0.05f - 0.02f, MathF.Cos(a) * 0.06f);
            b.Spike(Body, root, root + V(0, -0.05f, 0.005f), 0.011f, i % 2 == 0 ? black : cream, blend: 0.004f);
        }

        int tail = b.Tail(bc + V(0, 0.08f, -0.32f));
        b.Tube(tail, Smooth(3, bc + V(0, 0.08f, -0.32f), bc + V(0, 0.02f, -0.4f), bc + V(0.02f, -0.18f, -0.42f), bc + V(0.03f, -0.36f, -0.4f)), 0.045f, 0.03f, black, blend: 0.01f);
        FurTufts(b, tail, bc + V(0.02f, -0.2f, -0.41f), V(0.04f, 0.16f, 0.04f), 12, 0.04f, 0.018f, black, 1.2f, -0.95f, 0.9f);

        // A strong neck to a long head
        var neck = Smooth(3, bc + V(0, 0.06f, 0.24f), bc + V(0, 0.2f, 0.34f), bc + V(0, 0.32f, 0.38f));
        b.Tube(Body, neck, 0.11f, 0.08f, dun, blend: 0.02f);
        int head = b.Head(neck[^1]);
        var c = neck[^1] + V(0, 0.03f, 0.05f);
        var r = V(0.085f, 0.085f, 0.1f);
        b.Ell(head, c, r, dun, V(25f, 0, 0));
        var mc = c + V(0, -0.08f, 0.13f);
        var mr = V(0.065f, 0.06f, 0.07f);
        b.Limb(head, c + V(0, -0.02f, 0.05f), mc, 0.075f, 0.065f, dun);
        b.Ell(head, mc, mr, muzzle, blend: 0.03f);
        PokeBuilder.Both(s => b.Mark(head, Out(mc, mr, default, V(0.45f * s, 0.25f, 1f)), V(0.45f * s, 0.25f, 1f), 0.012f, 0.008f, Rgb(40, 32, 32)));
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.05f * s, 0.07f, -0.04f));
            b.Spike(ear, c + V(0.05f * s, 0.07f, -0.04f), c + V(0.07f * s, 0.16f, -0.04f), 0.028f, dun, 0.55f);
            var look = V(0.75f * s, 0.2f, 0.62f);
            b.Eye(head, Out(c, r, V(25f, 0, 0), look), look, 0.017f, Rgb(70, 50, 40));
        });
        // The mane, long black locks down both sides of the neck and a forelock to one side
        b.Spike(head, c + V(0.01f, 0.08f, 0.02f), c + V(0.045f, 0.0f, 0.1f), 0.026f, black, 0.55f);
        for (int i = 0; i < 8; i++)
        {
            float t = i / 7f;
            var root = Vector3.Lerp(c + V(0, 0.07f, -0.06f), bc + V(0, 0.16f, 0.2f), t);
            PokeBuilder.Both(s => b.Spike(head, root, root + V((0.07f + 0.02f * (i % 2)) * s, -0.12f - 0.03f * (i % 3), -0.02f), 0.028f, black, 0.5f, blend: 0.01f));
        }
        return b;
    }

    // ------------------------------------------------------------------ Delfinel and Delfinora

    /// <summary>Delfinel: a playful dolphin calf leaping, grey-blue above and cream below, a short beak open in a big smile, a little curled fin on its back.</summary>
    private static PokeBuilder Delfinel()
    {
        var b = new PokeBuilder("Delfinel", 0.55f, BodyPlan.Fish, V(0, 0.32f, 0)) { Coat = Scales }.Hover();
        var grey = Rgb(128, 146, 160);
        var cream = Rgb(246, 236, 206);
        var mouth = Rgb(214, 108, 110);

        var bc = V(0, 0.32f, -0.02f);
        var br = V(0.11f, 0.12f, 0.2f);
        b.Ell(Body, bc, br, grey, V(-18f, 0, 0));
        b.PaintEll(Body, bc + V(0, -0.05f, 0.05f), V(0.1f, 0.08f, 0.18f), cream, V(-18f, 0, 0));
        // A little dorsal fin, its tip curled over into a ring
        int dorsal = b.Part("dorsal", Body, bc + V(0, 0.1f, -0.04f), PokeRole.Fin);
        b.Tube(dorsal, new[] { bc + V(0, 0.08f, -0.0f) }.Concat(Spiral(bc + V(0, 0.17f, -0.07f), V(0, 0, 1f), V(0, 1f, 0), 0.045f, 0.012f, -2.4f, 2.2f, 12)).ToArray(),
            0.03f, 0.012f, grey, blend: 0.004f);
        b.Ell(dorsal, bc + V(0, 0.11f, -0.04f), V(0.022f, 0.04f, 0.05f), grey, V(30f, 0, 0), blend: 0.015f);
        PokeBuilder.Both(s =>
        {
            int fin = b.Part(s < 0 ? "finL" : "finR", Body, bc + V(0.09f * s, -0.04f, 0.06f), PokeRole.Fin, s, s);
            Frond(b, fin, bc + V(0.09f * s, -0.04f, 0.06f), bc + V(0.17f * s, -0.12f, 0.02f), 0.035f, grey, V(s, 0.3f, 0.3f), 0.3f);
        });
        // The tail curving down and its two flukes
        int tail = b.Tail(bc + V(0, -0.04f, -0.17f));
        var tailEnd = bc + V(0, -0.16f, -0.26f);
        b.Tube(tail, Smooth(3, bc + V(0, -0.04f, -0.17f), bc + V(0, -0.1f, -0.23f), tailEnd), 0.06f, 0.028f, grey, blend: 0.01f);
        PokeBuilder.Both(s => Frond(b, tail, tailEnd, tailEnd + V(0.1f * s, -0.05f, -0.04f), 0.035f, grey, V(0, 0.4f, -1f), 0.3f));
        // A round head, a short beak opened in a big smile
        int head = b.Head(bc + V(0, 0.05f, 0.14f));
        var c = bc + V(0, 0.08f, 0.17f);
        var r = V(0.1f, 0.095f, 0.1f);
        b.Ell(head, c, r, grey);
        b.PaintEll(head, c + V(0, -0.06f, 0.05f), V(0.09f, 0.05f, 0.08f), cream);
        var snout = c + V(0, -0.025f, 0.12f);
        b.Ell(head, snout, V(0.045f, 0.032f, 0.055f), grey, blend: 0.025f);
        b.PaintEll(head, snout + V(0, -0.02f, 0), V(0.045f, 0.018f, 0.06f), cream);
        Grin(b, head, snout + V(0, -0.012f, 0.0f), V(0.04f, 0.018f, 0.05f), mouth);
        PokeBuilder.Both(s =>
        {
            var look = V(0.55f * s, 0.3f, 0.78f);
            b.Eye(head, Out(c, r, default, look), look, 0.024f, Rgb(70, 86, 110), sclera: true, pupil: Rgb(40, 46, 60));
        });
        return Lift(b);
    }

    /// <summary>
    /// Delfinora: a sleek, graceful dolphin in deep sea-blue with a pale belly, an hourglass of yellow and grey along its
    /// sides, a glowing crescent on its brow and long tail flukes that flow like a gown.
    /// </summary>
    private static PokeBuilder Delfinora()
    {
        var b = new PokeBuilder("Delfinora", 0.95f, BodyPlan.Fish, V(0, 0.5f, 0)) { Coat = Scales }.Hover();
        var blue = Rgb(40, 84, 126);
        var belly = Rgb(238, 232, 204);
        var yellow = Rgb(240, 220, 110);
        var grey = Rgb(156, 166, 178);
        var crescent = Rgb(252, 244, 180);

        var bc = V(0, 0.52f, 0.0f);
        var br = V(0.11f, 0.13f, 0.3f);
        b.Ell(Body, bc, br, blue, V(-22f, 0, 0));
        b.PaintEll(Body, bc + V(0, -0.07f, 0.06f), V(0.09f, 0.07f, 0.27f), belly, V(-22f, 0, 0));
        // The hourglass along each side: grey outside, yellow within
        PokeBuilder.Both(s =>
        {
            foreach (var (z, y) in new[] { (0.06f, 0.035f), (-0.1f, -0.03f) })
            {
                b.PaintEll(Body, bc + V(0.09f * s, y, z), V(0.04f, 0.06f, 0.09f), grey, V(-22f, 0, 0));
                b.PaintEll(Body, bc + V(0.1f * s, y, z), V(0.03f, 0.045f, 0.065f), yellow, V(-22f, 0, 0));
            }
            b.PaintEll(Body, bc + V(0.1f * s, 0.0f, -0.02f), V(0.03f, 0.025f, 0.03f), grey);
        });
        int dorsal = b.Part("dorsal", Body, bc + V(0, 0.12f, -0.06f), PokeRole.Fin);
        b.Tube(dorsal, Smooth(3, bc + V(0, 0.1f, 0.0f), bc + V(0, 0.18f, -0.1f), bc + V(0, 0.2f, -0.2f), bc + V(0, 0.16f, -0.24f)), 0.035f, 0.012f, blue, blend: 0.006f);
        b.Ell(dorsal, bc + V(0, 0.13f, -0.08f), V(0.02f, 0.05f, 0.08f), blue, V(35f, 0, 0), blend: 0.015f);
        PokeBuilder.Both(s =>
        {
            int fin = b.Part(s < 0 ? "finL" : "finR", Body, bc + V(0.09f * s, -0.04f, 0.1f), PokeRole.Fin, s, s);
            Frond(b, fin, bc + V(0.09f * s, -0.04f, 0.1f), bc + V(0.18f * s, -0.18f, 0.02f), 0.045f, blue, V(s, 0.3f, 0.3f), 0.3f);
        });
        // The tail down into long flowing flukes, blue, yellow and grey like the folds of a gown
        int tail = b.Tail(bc + V(0, -0.1f, -0.24f));
        var tailEnd = bc + V(0, -0.3f, -0.3f);
        b.Tube(tail, Smooth(3, bc + V(0, -0.08f, -0.24f), bc + V(0, -0.2f, -0.32f), tailEnd), 0.07f, 0.035f, blue, blend: 0.01f);
        b.PaintEll(tail, tailEnd + V(0, 0.08f, 0.04f), V(0.05f, 0.08f, 0.05f), belly);
        int flukes = b.Part("flukes", tail, tailEnd, PokeRole.Fin, 0.7f);
        var ribbons = new (float X, float Down, float Back, Color Col)[]
        {
            (-0.16f, 0.2f, 0.06f, blue), (-0.1f, 0.26f, -0.04f, yellow), (-0.04f, 0.3f, 0.08f, grey), (0.03f, 0.3f, -0.06f, blue),
            (0.09f, 0.26f, 0.05f, yellow), (0.15f, 0.2f, -0.03f, blue)
        };
        foreach (var (x, down, back, col) in ribbons)
            b.Tube(flukes, Smooth(3, tailEnd, tailEnd + V(x * 0.5f, -down * 0.4f, back * 0.5f - 0.04f), tailEnd + V(x * 1.1f, -down * 0.75f, back),
                tailEnd + V(x * 1.4f + 0.03f, -down, back * 0.5f - 0.06f)), 0.03f, 0.014f, col, blend: 0.008f);
        PokeBuilder.Both(s => Frond(b, flukes, tailEnd, tailEnd + V(0.17f * s, -0.04f, -0.08f), 0.05f, blue, V(0, 0.4f, -1f), 0.25f));

        int head = b.Head(bc + V(0, 0.08f, 0.22f));
        var c = bc + V(0, 0.12f, 0.27f);
        var r = V(0.09f, 0.085f, 0.1f);
        b.Ell(head, c, r, blue);
        b.PaintEll(head, c + V(0, -0.06f, 0.05f), V(0.08f, 0.04f, 0.08f), belly);
        var snout = c + V(0, -0.03f, 0.13f);
        b.Limb(head, c + V(0, -0.02f, 0.05f), snout, 0.045f, 0.034f, blue);
        b.PaintEll(head, snout + V(0, -0.022f, -0.02f), V(0.04f, 0.016f, 0.07f), belly);
        b.Mark(head, snout + V(0, -0.012f, 0.03f), V(0, -0.1f, 1f), 0.02f, 0.006f, Rgb(24, 40, 64), MarkShape.Smile);
        // The crescent on its brow, glowing: a pale moon with the blue laid over its inner half
        var brow = Out(c, r, default, V(0, 0.7f, 0.7f));
        b.PaintEll(head, brow, V(0.04f, 0.035f, 0.03f), crescent, soft: 0.006f);
        b.PaintEll(head, brow + V(0, 0.016f, 0.004f), V(0.034f, 0.026f, 0.03f), blue, soft: 0.006f);
        PokeBuilder.Both(s =>
        {
            var look = V(0.7f * s, 0.25f, 0.65f);
            b.Eye(head, Out(c, r, default, look), look, 0.017f, Rgb(70, 130, 170), glare: false);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Calutmar and Calutregal

    /// <summary>Calutmar: a tiny sandy-yellow seahorse with a curled tail, a long snout, a frill of orange fins on its head like a mane and bright, brave eyes.</summary>
    private static PokeBuilder Calutmar()
    {
        var b = new PokeBuilder("Calutmar", 0.42f, BodyPlan.Floating, V(0, 0.27f, 0)) { Coat = Scales }.Hover();
        var sand = Rgb(232, 196, 112);
        var band = PixelCanvas.Shadow(sand, 0.18f);
        var orange = Rgb(236, 128, 70);
        var teal = Rgb(56, 132, 140);

        b.Ell(Body, V(0, 0.27f, 0.01f), V(0.08f, 0.11f, 0.085f), sand);
        int tail = b.Tail(V(0, 0.19f, -0.02f));
        SeahorseTail(b, tail, V(0, 0.21f, -0.01f), V(0, 0.1f, 0.02f), 0.08f, 1.6f, 0.05f, sand);
        foreach (float y in new[] { 0.2f, 0.24f, 0.28f, 0.32f })
            b.PaintEll(Body, V(0, y, 0.085f), V(0.06f, 0.005f, 0.04f), band, soft: 0.006f);
        // A fin on its back: orange webbing between teal spines
        int fin = b.Part("fin", Body, V(0, 0.27f, -0.07f), PokeRole.Fin, 0.5f);
        Frond(b, fin, V(0, 0.25f, -0.06f), V(0, 0.29f, -0.17f), 0.05f, orange, V(1f, 0, 0), 0.22f, Shell);
        foreach (float t in new[] { -1f, 0f, 1f })
            b.Spike(fin, V(0, 0.27f + 0.01f * t, -0.07f), V(0, 0.3f + 0.035f * t, -0.17f), 0.008f, teal, mat: Shell);
        PokeBuilder.Both(s => b.Ell(fin, V(0.065f * s, 0.27f, -0.02f), V(0.008f, 0.03f, 0.035f), PixelCanvas.Light1(orange, 0.25f), V(0, 25f * s, 0), Shell));

        int head = b.Head(V(0, 0.36f, 0));
        var c = V(0, 0.43f, 0);
        var r = V(0.09f, 0.088f, 0.095f);
        b.Ell(head, c, r, sand);
        var tip = V(0, 0.41f, 0.2f);
        b.Limb(head, V(0, 0.415f, 0.07f), tip, 0.034f, 0.03f, sand);
        b.Ell(head, tip, V(0.038f, 0.038f, 0.022f), sand, blend: 0.015f);
        BellMouth(b, head, tip + V(0, 0, 0.013f), V(0, 0, 1f), 0.02f, sand, Rgb(80, 54, 40), 0.9f);
        // The mane: a frill of orange fins over its crown and down the back of its head, teal spines through them
        for (int i = 0; i < 5; i++)
        {
            float a = (i - 0.5f) * 0.42f;
            var root = c + V(0, MathF.Cos(a) * 0.075f, -MathF.Sin(a) * 0.075f - 0.01f);
            var outward = V(0, MathF.Cos(a), -MathF.Sin(a));
            Blade(b, head, root, root + outward * 0.1f + V(0, 0, -0.03f), 0.035f, orange, V(1f, 0, 0), 0.25f, Shell);
            b.Spike(head, root, root + outward * 0.11f + V(0, 0, -0.03f), 0.008f, teal, mat: Shell, blend: 0.004f);
        }
        PokeBuilder.Both(s =>
        {
            var look = V(0.75f * s, 0.25f, 0.6f);
            b.Ell(head, Out(c, r, default, look + V(0, 0.4f, -0.05f)), V(0.03f, 0.01f, 0.022f), PixelCanvas.Shadow(sand, 0.15f), V(0, 0, -14f * s), blend: 0.015f);
            b.Eye(head, Out(c, r, default, look), look, 0.03f, Rgb(70, 150, 190), sclera: true, pupil: Rgb(40, 36, 44));
        });
        return Lift(b);
    }

    /// <summary>
    /// Calutregal: a large, regal seahorse in pearly cream and coral pink, its head a proud horse's with pointed ears and a
    /// flowing mane of coral fins edged in mint, a crown of shells between its ears, a great fin at its side and strings
    /// of pearls round its curled tail.
    /// </summary>
    private static PokeBuilder Calutregal()
    {
        var b = new PokeBuilder("Calutregal", 0.85f, BodyPlan.Floating, V(0, 0.46f, 0)) { Coat = Scales }.Hover();
        var pearl = Rgb(248, 238, 228);
        var coral = Rgb(238, 140, 128);
        var mint = Rgb(150, 214, 196);
        var shell = Rgb(240, 160, 130);
        var pearls = Rgb(244, 236, 242);

        b.Ell(Body, V(0, 0.46f, 0.0f), V(0.11f, 0.17f, 0.11f), pearl);
        for (int i = 0; i < 6; i++)
            b.PaintEll(Body, V(0, 0.34f + i * 0.045f, 0.1f), V(0.08f, 0.005f, 0.04f), PixelCanvas.Shadow(pearl, 0.12f), soft: 0.006f);
        // Coral swirls on its back and sides
        PokeBuilder.Both(s =>
        {
            b.PaintEll(Body, V(0.08f * s, 0.5f, -0.04f), V(0.04f, 0.07f, 0.03f), coral, V(0, 0, 25f * s));
            b.PaintEll(Body, V(0.07f * s, 0.38f, -0.06f), V(0.035f, 0.05f, 0.03f), coral, V(0, 0, -20f * s));
        });
        int tail = b.Tail(V(0, 0.32f, -0.03f));
        SeahorseTail(b, tail, V(0, 0.34f, -0.03f), V(0, 0.15f, 0.04f), 0.13f, 1.6f, 0.085f, pearl);
        // Two strings of pearls round its tail
        foreach (var (at, ring) in new[] { (V(0, 0.27f, -0.05f), 0.07f), (V(0, 0.16f, -0.1f), 0.065f) })
        {
            b.Torus(tail, at, ring, 0.007f, pearls, V(80f, 0, 0), mat: Shell, blend: 0.003f);
            for (int k = 0; k < 10; k++)
            {
                float a = k * MathF.Tau / 10f;
                b.Ell(tail, at + V(MathF.Cos(a) * ring, MathF.Sin(a) * ring * 0.17f, MathF.Sin(a) * ring), V(0.012f, 0.012f, 0.012f), pearls, mat: Shell, blend: 0.003f);
            }
        }
        // The great side fins, coral ribbed in mint
        PokeBuilder.Both(s =>
        {
            int fin = b.Part(s < 0 ? "finL" : "finR", Body, V(0.09f * s, 0.44f, -0.05f), PokeRole.Fin, s, s);
            foreach (var (to, w) in new[] { (V(0.28f * s, 0.62f, -0.12f), 0.05f), (V(0.3f * s, 0.46f, -0.16f), 0.055f), (V(0.24f * s, 0.32f, -0.13f), 0.045f) })
            {
                Blade(b, fin, V(0.09f * s, 0.44f, -0.05f), to, w, coral, V(0, 0, 1f), 0.25f, Shell);
                b.Spike(fin, V(0.09f * s, 0.44f, -0.05f), to + V(0.01f * s, 0, 0), 0.009f, mint, mat: Shell, blend: 0.004f);
            }
        });

        // A proud horse's head on an arching neck
        var neck = Smooth(3, V(0, 0.56f, 0.0f), V(0, 0.66f, 0.03f), V(0, 0.74f, 0.07f));
        b.Tube(Body, neck, 0.08f, 0.065f, pearl, blend: 0.015f);
        int head = b.Head(neck[^1]);
        var c = neck[^1] + V(0, 0.03f, 0.02f);
        var r = V(0.07f, 0.075f, 0.08f);
        b.Ell(head, c, r, pearl);
        var nose = c + V(0, -0.06f, 0.15f);
        b.Limb(head, c + V(0, -0.01f, 0.04f), nose, 0.055f, 0.04f, pearl);
        b.Ell(head, nose, V(0.042f, 0.035f, 0.035f), pearl, blend: 0.015f);
        PokeBuilder.Both(s => b.PaintEll(head, nose + V(0.016f * s, 0.006f, 0.03f), V(0.008f, 0.006f, 0.01f), Rgb(170, 100, 100), soft: 0.004f));
        b.PaintEll(head, c + V(0, 0.01f, 0.07f), V(0.02f, 0.05f, 0.04f), coral, V(-30f, 0, 0));
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.04f * s, 0.06f, -0.02f));
            b.Spike(ear, c + V(0.04f * s, 0.06f, -0.02f), c + V(0.06f * s, 0.15f, -0.03f), 0.022f, pearl, 0.55f);
            b.PaintEll(ear, c + V(0.05f * s, 0.1f, -0.01f), V(0.012f, 0.03f, 0.012f), coral);
            var look = V(0.8f * s, 0.2f, 0.55f);
            b.Eye(head, Out(c, r, default, look), look, 0.02f, Rgb(70, 150, 200), sclera: true, pupil: Rgb(40, 40, 60));
        });
        // The crown: a scallop shell, a little conch and pearls between the ears
        var crown = c + V(0, 0.075f, 0.0f);
        b.Ell(head, crown + V(0, 0.02f, 0.01f), V(0.035f, 0.03f, 0.012f), shell, V(-10f, 0, 0), Shell, 0.006f);
        foreach (float a in new[] { -40f, -20f, 0f, 20f, 40f })
            b.PaintEll(head, crown + V(MathF.Sin(a * Degree) * 0.02f, 0.02f + MathF.Cos(a * Degree) * 0.012f, 0.022f), V(0.003f, 0.02f, 0.006f), PixelCanvas.Shadow(shell, 0.25f), V(0, 0, -a), 0.003f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, crown + V(0.035f * s, 0.0f, 0.0f), crown + V(0.045f * s, 0.05f, -0.01f), 0.014f, PixelCanvas.Light1(shell, 0.2f), mat: Shell);
            b.Ell(head, crown + V(0.022f * s, -0.005f, 0.018f), V(0.01f, 0.01f, 0.01f), pearls, mat: Shell, blend: 0.003f);
        });
        // The mane: coral fins flowing down the back of the neck, edged in mint
        for (int i = 0; i < 6; i++)
        {
            float t = i / 5f;
            var root = Vector3.Lerp(c + V(0, 0.05f, -0.06f), V(0, 0.52f, -0.09f), t);
            var tip = root + V(0.02f * ((i % 2) * 2 - 1), -0.07f - 0.03f * (i % 3), -0.14f - 0.03f * t);
            Blade(b, head, root, tip, 0.04f, coral, V(1f, 0, 0), 0.25f, Shell);
            b.Spike(head, root, tip + V(0, -0.005f, -0.005f), 0.008f, mint, mat: Shell, blend: 0.003f);
        }
        return Lift(b);
    }

    // ------------------------------------------------------------------ Sarel and Salinax

    /// <summary>Sarel: a little cube of cloudy white and pale pink rock salt, blocks of salt stacked on it, a sleepy face on its front and stubby crystal feet.</summary>
    private static PokeBuilder Sarel()
    {
        var b = new PokeBuilder("Sarel", 0.42f, BodyPlan.Biped, V(0, 0.18f, 0)) { Coat = Shell };
        var salt = Rgb(240, 230, 232);
        var pink = Rgb(232, 196, 196);
        var deep = Rgb(212, 176, 182);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.055f * s, 0.07f, 0.0f));
            b.Box(leg, V(0.058f * s, 0.035f, 0.01f), V(0.026f, 0.035f, 0.03f), 0.008f, salt, V(0, 12f * s, 0));
            b.Spike(leg, V(0.058f * s, 0.06f, 0.01f), V(0.06f * s, 0.005f, 0.015f), 0.022f, salt, 0.7f);
        });
        // The body: a cube, rough with blocks of salt stacked on top and round the sides, mottled pink
        var bc = V(0, 0.18f, 0);
        b.Box(Body, bc, V(0.11f, 0.095f, 0.1f), 0.018f, salt);
        foreach (var (x, z, h, turn) in new[] { (-0.055f, 0.04f, 0.03f, 6f), (0.055f, 0.04f, 0.028f, -8f), (-0.05f, -0.05f, 0.032f, -4f), (0.055f, -0.05f, 0.03f, 9f) })
            b.Box(Body, bc + V(x, 0.1f + h * 0.4f, z), V(0.052f, h, 0.047f), 0.01f, x * z > 0 ? salt : pink, V(0, turn, x * 30f), blend: 0.004f);
        PokeBuilder.Both(s => b.Box(Body, bc + V(0.1f * s, -0.01f, -0.02f), V(0.025f, 0.075f, 0.08f), 0.01f, pink, V(0, 0, 4f * s), blend: 0.004f));
        b.Box(Body, bc + V(0, -0.085f, 0.0f), V(0.1f, 0.025f, 0.095f), 0.01f, pink, blend: 0.004f);
        foreach (var (x, y, z) in new[] { (0.06f, 0.03f, 0.1f), (-0.05f, -0.04f, 0.1f), (0.09f, 0.1f, 0.0f), (-0.1f, 0.03f, -0.05f), (0.0f, 0.13f, -0.06f) })
            b.PaintEll(Body, bc + V(x, y, z), V(0.03f, 0.022f, 0.03f), deep, soft: 0.015f);
        // The sleepy face on its front
        PokeBuilder.Both(s =>
        {
            var at = bc + V(0.04f * s, 0.01f, 0.1f);
            b.PaintEll(Body, at + V(0, 0.012f, 0), V(0.02f, 0.008f, 0.01f), deep, soft: 0.004f);
            b.Eye(Body, at, V(0, 0, 1f), 0.015f, Rgb(70, 50, 56));
        });
        b.Mark(Body, bc + V(0, -0.03f, 0.1f), V(0, 0, 1f), 0.008f, 0.006f, Rgb(200, 120, 120), MarkShape.Triangle, 180f);
        return b;
    }

    /// <summary>A banded leg of rock salt, a block from the hip to the knee and one from the knee to a broad toed foot.</summary>
    private static void C7SaltLeg(PokeBuilder b, int bone, Vector3 hip, float r, Color[] bands)
    {
        var knee = V(hip.X, hip.Y * 0.48f, hip.Z + 0.01f);
        b.Box(bone, (hip + knee) / 2f, V(r, (hip.Y - knee.Y) / 2f + r * 0.2f, r * 1.05f), r * 0.3f, bands[0]);
        b.Box(bone, V(knee.X, knee.Y / 2f + 0.01f, knee.Z + 0.01f), V(r * 1.12f, knee.Y / 2f + 0.005f, r * 1.15f), r * 0.3f, bands[0], blend: 0.01f);
        b.Box(bone, V(knee.X, r * 0.32f, knee.Z + r * 0.35f), V(r * 1.2f, r * 0.32f, r * 1.3f), r * 0.2f, bands[0], blend: 0.008f);
        for (int i = 0; i < 6; i++)
            b.PaintEll(bone, V(hip.X, hip.Y * (0.2f + 0.14f * i), hip.Z), V(r * 1.6f, r * 0.12f, r * 1.6f), bands[1 + i % (bands.Length - 1)], V(0, 0, 4f * (i % 2 == 0 ? 1 : -1)), r * 0.1f);
        foreach (float t in new[] { -0.45f, 0.45f })
            b.PaintEll(bone, V(knee.X + t * r, r * 0.3f, knee.Z + r * 1.6f), V(r * 0.06f, r * 0.3f, r * 0.2f), Rgb(160, 140, 142), soft: r * 0.08f);
    }

    /// <summary>
    /// Salinax: a great stately creature carved of banded rock salt, white, grey, rose and amber in stripes like the walls
    /// of a salt mine, a domed head with gentle sleepy eyes, cubes of clear crystal on its shoulders and a pool of brine
    /// in the hollow of its back.
    /// </summary>
    private static PokeBuilder Salinax()
    {
        var b = new PokeBuilder("Salinax", 1f, BodyPlan.Quadruped, V(0, 0.42f, 0)) { Coat = Shell };
        var white = Rgb(236, 230, 228);
        var grey = Rgb(186, 186, 190);
        var rose = Rgb(220, 170, 168);
        var amber = Rgb(222, 156, 80);
        var crystal = Rgb(224, 234, 244);
        var brine = Rgb(170, 206, 220);
        var bands = new[] { white, rose, grey, amber, white, rose };

        // The body first, and the hollow of its pool straight after, so the cut carves nothing else
        var bc = V(0, 0.44f, -0.03f);
        b.Box(Body, bc, V(0.25f, 0.19f, 0.3f), 0.09f, white);
        b.Cut(Body, bc + V(0, 0.22f, -0.04f), V(0.15f, 0.07f, 0.18f));
        b.Ell(Body, bc + V(0, 0.155f, -0.04f), V(0.15f, 0.012f, 0.18f), brine, mat: Glow, blend: 0.004f);
        b.PaintEll(Body, bc + V(0.03f, 0.165f, -0.07f), V(0.05f, 0.01f, 0.07f), PixelCanvas.Light1(brine, 0.3f), soft: 0.02f);
        for (int i = 0; i < 7; i++)
            b.PaintEll(Body, bc + V(0, -0.15f + 0.05f * i, 0), V(0.3f, 0.012f, 0.36f), bands[1 + i % (bands.Length - 1)], V(0, 0, 3f * (i % 2 == 0 ? 1 : -1)), 0.008f);
        // The rim of the pool, a ring of white blocks
        b.Torus(Body, bc + V(0, 0.175f, -0.04f), 0.155f, 0.03f, white, default, 1f, 1.2f, blend: 0.01f);
        foreach (var (x, y, z) in new[] { (0.1f, 0.02f, 0.18f), (-0.18f, -0.06f, 0.05f), (0.2f, 0.06f, -0.12f), (-0.06f, -0.1f, -0.22f) })
            b.PaintEll(Body, bc + V(x, y, z), V(0.04f, 0.03f, 0.04f), PixelCanvas.Shadow(white, 0.08f), soft: 0.02f);

        foreach (var (z, front) in new[] { (0.17f, true), (-0.23f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.19f * s, 0.34f, z), front);
                C7SaltLeg(b, leg, V(0.19f * s, 0.34f, z), 0.075f, bands);
            });
        // Clusters of clear salt cubes on its shoulders
        PokeBuilder.Both(s =>
        {
            var at = bc + V(0.22f * s, 0.15f, 0.16f);
            b.Box(Body, at + V(0.02f * s, 0.06f, 0), V(0.055f, 0.07f, 0.05f), 0.008f, crystal, V(15f, 30f * s, -20f * s), Glow, 0.006f);
            b.Box(Body, at + V(0.07f * s, 0.0f, 0.04f), V(0.035f, 0.035f, 0.035f), 0.006f, crystal, V(-20f, 15f * s, 35f * s), Glow, 0.006f);
            b.Box(Body, at + V(-0.03f * s, 0.01f, 0.05f), V(0.03f, 0.03f, 0.03f), 0.006f, crystal, V(30f, -20f * s, 10f), Glow, 0.006f);
        });
        // A domed head low at its front, banded too, with a wide mouth and two little teeth
        int head = b.Head(bc + V(0, 0.0f, 0.3f));
        var c = bc + V(0, 0.02f, 0.34f);
        var r = V(0.15f, 0.13f, 0.11f);
        b.Ell(head, c, r, white);
        for (int i = 0; i < 4; i++)
            b.PaintEll(head, c + V(0, 0.09f - 0.05f * i, -0.02f), V(0.17f, 0.009f, 0.13f), bands[1 + (i + 1) % (bands.Length - 1)], soft: 0.007f);
        b.Mark(head, On(c, r, 0, c.Y - 0.055f), V(0, -0.25f, 1f), 0.08f, 0.012f, Rgb(120, 96, 96), MarkShape.Bar);
        PokeBuilder.Both(s => b.Spike(head, On(c, r, 0.03f * s, c.Y - 0.06f) + V(0, 0, -0.008f), On(c, r, 0.03f * s, c.Y - 0.06f) + V(0, -0.02f, 0.0f), 0.008f, white, 0.7f));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.06f * s, c.Y + 0.0f);
            b.PaintEll(head, at + V(0, 0.012f, 0), V(0.028f, 0.009f, 0.02f), PixelCanvas.Shadow(white, 0.18f), soft: 0.004f);
            b.Eye(head, at, Outward(c, r, at), 0.017f, Rgb(70, 60, 64));
        });
        return b;
    }

    // ------------------------------------------------------------------ Valvisor and Valvoara

    /// <summary>Valvisor: a small pale spirit of the mines shaped like a candle's flame, a little face with dark eyes, a wisp of a tail sprinkled with gold dust, an old oil lamp in its hand.</summary>
    private static PokeBuilder Valvisor()
    {
        var b = new PokeBuilder("Valvisor", 0.42f, BodyPlan.Floating, V(0, 0.28f, 0)) { Coat = Fur }.Hover();
        var pale = Rgb(226, 234, 246);
        var shade = Rgb(188, 204, 228);
        var gold = Rgb(236, 190, 80);

        // A little body tapering back into a wisp
        var bc = V(0, 0.22f, -0.01f);
        b.Ell(Body, bc, V(0.06f, 0.07f, 0.055f), pale);
        b.Tube(Body, Smooth(3, bc + V(0, -0.03f, -0.02f), bc + V(0.02f, -0.1f, -0.06f), bc + V(0.06f, -0.15f, -0.13f), bc + V(0.11f, -0.17f, -0.2f)), 0.045f, 0.01f, pale, blend: 0.012f);
        b.PaintEll(Body, bc + V(0.06f, -0.15f, -0.13f), V(0.05f, 0.04f, 0.07f), shade);
        foreach (var (x, y, z) in new[] { (0.05f, -0.13f, -0.11f), (0.08f, -0.16f, -0.16f), (0.03f, -0.1f, -0.07f), (0.1f, -0.17f, -0.19f) })
            b.PaintEll(Body, bc + V(x, y, z), V(0.008f, 0.008f, 0.008f), gold, soft: 0.004f);
        // Its free arm folded at its middle; the other holds the lamp out by its bail
        int free = b.Arm(1f, bc + V(0.05f, 0.03f, 0.02f));
        b.Limb(free, bc + V(0.05f, 0.03f, 0.02f), bc + V(0.03f, -0.0f, 0.06f), 0.018f, 0.015f, pale);
        int arm = b.Arm(-1f, bc + V(-0.05f, 0.03f, 0.01f));
        var hand = bc + V(-0.13f, 0.0f, 0.06f);
        b.Limb(arm, bc + V(-0.05f, 0.03f, 0.01f), hand, 0.018f, 0.015f, pale);
        b.Ell(arm, hand, V(0.02f, 0.02f, 0.02f), pale, blend: 0.008f);
        C7Lamp(b, arm, hand + V(0, -0.006f, 0), 0.85f, Rgb(250, 206, 110));

        // A head like a candle's flame, swept up into curling points
        int head = b.Head(bc + V(0, 0.06f, 0));
        var c = bc + V(0, 0.13f, 0.01f);
        var r = V(0.085f, 0.085f, 0.075f);
        b.Ell(head, c, r, pale);
        b.Spike(head, c + V(0, 0.05f, -0.01f), c + V(0.02f, 0.2f, -0.03f), 0.06f, pale, 0.8f);
        PokeBuilder.Both(s => b.Spike(head, c + V(0.045f * s, 0.04f, -0.01f), c + V(0.085f * s, 0.12f, -0.02f), 0.028f, pale, 0.7f));
        b.PaintEll(head, c + V(0, 0.12f, -0.02f), V(0.03f, 0.07f, 0.04f), shade);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.035f * s, c.Y - 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.022f, Rgb(200, 230, 190), pupil: Rgb(36, 40, 54));
            b.PaintEll(head, On(c, r, 0.055f * s, c.Y - 0.04f), V(0.012f, 0.007f, 0.01f), Rgb(200, 212, 234), soft: 0.004f);
        });
        b.Mark(head, On(c, r, 0, c.Y - 0.045f), V(0, -0.2f, 1f), 0.006f, 0.005f, Rgb(80, 80, 100));
        return Lift(b);
    }

    /// <summary>
    /// Valvoara, the spirit who guards the mines: a tall pale figure with gold veins showing through her, a diadem and a
    /// veil of hammered gold and copper leaf, her lower body fading into a stream of ore dust, a miner's lamp on a chain
    /// in her hand and eyes like two gold nuggets.
    /// </summary>
    private static PokeBuilder Valvoara()
    {
        var b = new PokeBuilder("Valvoara", 0.95f, BodyPlan.Floating, V(0, 0.62f, 0)) { Coat = Fur }.Hover();
        var pale = Rgb(214, 226, 240);
        var shade = Rgb(178, 196, 224);
        var gold = Rgb(222, 172, 70);
        var copper = Rgb(186, 118, 64);
        var vein = Rgb(232, 190, 86);
        var iron = Rgb(92, 76, 64);

        // The veil first: leaves of gold and copper falling from her crown behind her and down her back
        int veil = b.Part("veil", Body, V(0, 0.92f, -0.06f), PokeRole.Leaf, 0.4f);
        Frond(b, veil, V(0, 0.98f, -0.05f), V(0.02f, 0.42f, -0.16f), 0.13f, gold, V(0, 0, 1f), 0.12f, Metal);
        PokeBuilder.Both(s =>
        {
            Frond(b, veil, V(0.07f * s, 0.97f, -0.02f), V(0.17f * s, 0.46f, -0.1f), 0.09f, gold, V(s, 0, 0.6f), 0.14f, Metal);
            b.PaintEll(veil, V(0.13f * s, 0.62f, -0.08f), V(0.05f, 0.1f, 0.06f), copper, soft: 0.03f);
        });
        b.PaintEll(veil, V(0, 0.6f, -0.14f), V(0.08f, 0.12f, 0.05f), copper, soft: 0.04f);

        // A slim body, pale and veined with gold, flowing down into a tail of ore dust
        var bc = V(0, 0.66f, 0.0f);
        b.Ell(Body, bc, V(0.065f, 0.13f, 0.05f), pale);
        b.Tube(Body, Smooth(3, bc + V(0, -0.1f, 0), bc + V(0.01f, -0.3f, -0.03f), bc + V(0.05f, -0.5f, -0.12f), bc + V(0.13f, -0.6f, -0.24f)), 0.07f, 0.014f, pale, blend: 0.015f);
        foreach (var (x, side) in new[] { (0.03f, 1f), (-0.04f, -1f) })
            b.Tube(Body, Smooth(3, bc + V(x, -0.2f, 0.0f), bc + V(x * 2f, -0.38f, -0.04f), bc + V(x * 3f + 0.03f, -0.52f, -0.1f)), 0.03f, 0.01f, pale, blend: 0.012f);
        b.PaintEll(Body, bc + V(0.08f, -0.52f, -0.17f), V(0.08f, 0.1f, 0.1f), shade, soft: 0.03f);
        foreach (var (x, y, z) in new[] { (0.05f, -0.42f, -0.08f), (0.09f, -0.5f, -0.14f), (0.12f, -0.57f, -0.2f), (0.02f, -0.36f, -0.05f), (0.07f, -0.55f, -0.12f), (0.11f, -0.46f, -0.15f) })
            b.PaintEll(Body, bc + V(x, y, z), V(0.012f, 0.012f, 0.012f), x > 0.07f ? copper : gold, soft: 0.005f);
        foreach (var (from, to) in new[] { (V(0.02f, 0.08f, 0.04f), V(-0.03f, -0.05f, 0.05f)), (V(-0.03f, -0.02f, 0.05f), V(0.02f, -0.22f, 0.05f)), (V(0.03f, -0.15f, 0.05f), V(0.0f, -0.34f, 0.03f)) })
            b.PaintEll(Body, bc + (from + to) / 2f, V(0.004f, (to - from).Length() / 2f, 0.04f), vein, Euler(to - from), 0.003f);

        // Her arms: the left holds the lamp's chain out before her, the right rests at her waist
        int rest = b.Arm(1f, bc + V(0.06f, 0.08f, 0.0f));
        b.Limb(rest, bc + V(0.06f, 0.08f, 0.0f), bc + V(0.1f, -0.01f, 0.04f), 0.018f, 0.015f, pale);
        b.Limb(rest, bc + V(0.1f, -0.01f, 0.04f), bc + V(0.05f, -0.06f, 0.07f), 0.015f, 0.012f, pale);
        int arm = b.Arm(-1f, bc + V(-0.06f, 0.08f, 0.0f));
        var hand = bc + V(-0.17f, -0.02f, 0.09f);
        b.Limb(arm, bc + V(-0.06f, 0.08f, 0.0f), bc + V(-0.12f, 0.0f, 0.04f), 0.018f, 0.015f, pale);
        b.Limb(arm, bc + V(-0.12f, 0.0f, 0.04f), hand, 0.015f, 0.013f, pale);
        b.Ell(arm, hand, V(0.018f, 0.02f, 0.018f), pale, blend: 0.008f);
        // The chain, link by link down from her hand to the lamp
        var chainEnd = hand + V(-0.02f, -0.22f, 0.02f);
        for (int i = 0; i < 9; i++)
        {
            var at = Vector3.Lerp(hand + V(0, -0.018f, 0), chainEnd, i / 8f);
            b.Torus(arm, at, 0.012f, 0.0075f, iron, V(0, i % 2 == 0 ? 0f : 90f, 90f), mat: Metal, blend: 0.004f);
        }
        C7Lamp(b, arm, chainEnd + V(0, -0.005f, 0), 1.2f, Rgb(252, 170, 80));

        // Her head: pale, gold nugget eyes, flames of hair rising through a golden diadem
        int head = b.Head(bc + V(0, 0.15f, 0.0f));
        var c = bc + V(0, 0.27f, 0.01f);
        var r = V(0.06f, 0.075f, 0.062f);
        b.Ell(head, c, r, pale);
        b.Limb(head, bc + V(0, 0.12f, 0.0f), c + V(0, -0.05f, -0.01f), 0.025f, 0.022f, pale);
        b.Torus(head, c + V(0, 0.045f, -0.005f), 0.058f, 0.009f, gold, V(-12f, 0, 0), 1f, 1.05f, Metal, 0.005f);
        b.Spike(head, c + V(0, 0.05f, 0.055f), c + V(0, 0.11f, 0.065f), 0.018f, gold, 0.5f, Metal);
        foreach (var (x, h, back) in new[] { (0f, 0.15f, -0.02f), (-0.03f, 0.11f, -0.04f), (0.035f, 0.12f, -0.03f), (0.012f, 0.09f, -0.07f) })
            b.Spike(head, c + V(x * 0.6f, 0.05f, -0.02f), c + V(x * 1.6f, 0.05f + h, back), 0.024f, shade, 0.7f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.025f * s, c.Y + 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(250, 196, 70), white: Rgb(250, 214, 96), pupil: Rgb(150, 98, 30));
        });
        b.Mark(head, On(c, r, 0, c.Y - 0.04f), V(0, -0.2f, 1f), 0.012f, 0.005f, Rgb(120, 110, 140), MarkShape.Smile);
        return Lift(b);
    }

    // ------------------------------------------------------------------ Ghetel and Stalaghet

    /// <summary>Ghetel: a little icicle turned point-down, clear blue with air bubbles caught in it, a round frosty-browed face on its top and two nubs of ice for arms.</summary>
    private static PokeBuilder Ghetel()
    {
        var b = new PokeBuilder("Ghetel", 0.42f, BodyPlan.Floating, V(0, 0.24f, 0)) { Coat = Shell }.Hover();
        var ice = Rgb(184, 218, 240);
        var deep = Rgb(150, 196, 232);
        var frost = Rgb(240, 248, 252);

        // The icicle: a cone of faceted ice pointing down, bubbles caught in it
        b.Spike(Body, V(0, 0.27f, 0), V(0, 0.0f, 0.01f), 0.095f, ice);
        foreach (var (x, z, turn) in new[] { (0.04f, 0.04f, 20f), (-0.05f, 0.02f, -25f), (0.0f, -0.05f, 0f) })
            b.Box(Body, V(x, 0.17f, z), V(0.03f, 0.07f, 0.025f), 0.006f, deep, V(0, turn, x * 120f), blend: 0.01f);
        foreach (var (x, y, z) in new[] { (0.03f, 0.18f, 0.07f), (-0.035f, 0.12f, 0.055f), (0.015f, 0.08f, 0.04f), (-0.02f, 0.22f, 0.08f), (0.045f, 0.24f, 0.07f) })
            b.PaintEll(Body, V(x, y, z), V(0.007f, 0.007f, 0.012f), frost, soft: 0.004f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.07f * s, 0.24f, 0.01f));
            b.Limb(arm, V(0.07f * s, 0.24f, 0.01f), V(0.11f * s, 0.2f, 0.03f), 0.026f, 0.022f, ice);
            b.Ell(arm, V(0.115f * s, 0.195f, 0.03f), V(0.026f, 0.028f, 0.024f), ice, blend: 0.01f);
        });
        // The round top and its face
        int head = b.Head(V(0, 0.28f, 0));
        var c = V(0, 0.33f, 0.0f);
        var r = V(0.1f, 0.085f, 0.09f);
        b.Ell(head, c, r, ice);
        b.PaintEll(head, c + V(-0.03f, 0.05f, 0.02f), V(0.04f, 0.02f, 0.04f), frost, soft: 0.02f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.04f * s, c.Y + 0.0f);
            // Tufted frosty brows over each eye
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(head, at + V((0.0f + 0.012f * t) * s, 0.032f, -0.01f), at + V((0.012f + 0.016f * t) * s, 0.05f + 0.004f * t, 0.01f), 0.009f, frost, 0.6f);
            b.Eye(head, at, Outward(c, r, at), 0.02f, Rgb(60, 90, 120), sclera: true, pupil: Rgb(30, 44, 60));
        });
        b.Mark(head, On(c, r, 0.008f, c.Y - 0.022f), V(0, -0.1f, 1f), 0.009f, 0.006f, Rgb(54, 80, 110));
        return Lift(b);
    }

    /// <summary>
    /// Stalaghet: a tall pillar of a great ice cave, a cluster of ice stalagmites banded blue and white with age rising
    /// from a base of dark limestone, long icicle arms, a round head crowned with spikes of ice and an angry frosty face.
    /// </summary>
    private static PokeBuilder Stalaghet()
    {
        var b = new PokeBuilder("Stalaghet", 1f, BodyPlan.Biped, V(0, 0.55f, 0)) { Coat = Shell };
        var ice = Rgb(170, 206, 236);
        var deep = Rgb(110, 160, 214);
        var pale = Rgb(226, 238, 248);
        var rock = Rgb(78, 82, 92);
        var rockLight = Rgb(110, 114, 124);

        // The base of limestone: two great blocks it stands on, chips of rock round them
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.15f, 0.0f));
            b.Box(leg, V(0.12f * s, 0.08f, 0.0f), V(0.13f, 0.08f, 0.17f), 0.025f, rock, V(0, 10f * s, 0));
            b.Box(leg, V(0.2f * s, 0.05f, 0.1f), V(0.07f, 0.05f, 0.07f), 0.015f, rockLight, V(0, 30f * s, 8f), blend: 0.008f);
            b.Spike(leg, V(0.24f * s, 0.05f, -0.06f), V(0.28f * s, 0.18f, -0.08f), 0.05f, rock);
            b.PaintEll(leg, V(0.12f * s, 0.16f, 0.0f), V(0.14f, 0.01f, 0.18f), rockLight, soft: 0.008f);
        });
        // The pillar: a cluster of stalagmites, banded with the ages of the ice
        b.Ell(Body, V(0, 0.48f, 0), V(0.17f, 0.32f, 0.13f), ice);
        foreach (var (x, z, top, w) in new[] { (-0.1f, 0.08f, 0.62f, 0.07f), (0.08f, 0.09f, 0.58f, 0.065f), (0.0f, 0.11f, 0.48f, 0.06f), (-0.16f, -0.02f, 0.5f, 0.055f), (0.15f, -0.03f, 0.52f, 0.055f), (0.04f, -0.1f, 0.66f, 0.07f) })
            b.Spike(Body, V(x, 0.18f, z), V(x * 1.1f, top, z * 1.1f), w, x < 0 ? ice : PixelCanvas.Light1(ice, 0.1f));
        foreach (var (x, z, top) in new[] { (-0.05f, 0.13f, 0.3f), (0.06f, 0.13f, 0.28f), (0.14f, 0.08f, 0.26f) })
            b.Spike(Body, V(x, 0.16f, z), V(x, top, z + 0.02f), 0.035f, pale);
        for (int i = 0; i < 6; i++)
            b.PaintEll(Body, V(0, 0.22f + 0.08f * i, 0), V(0.22f, 0.013f, 0.18f), i % 2 == 0 ? deep : pale, V(0, 0, 4f * (i % 2 == 0 ? 1 : -1)), 0.01f);
        foreach (var (x, y) in new[] { (0.05f, 0.36f), (-0.07f, 0.44f), (0.02f, 0.52f), (-0.03f, 0.3f) })
            b.Mark(Body, V(x, y, 0.135f), V(x * 2f, 0, 1f), 0.007f, 0.007f, pale);
        // Long arms of icicles, broken to points that hang down
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.16f * s, 0.66f, 0.0f));
            var elbow = V(0.27f * s, 0.5f, 0.03f);
            var wrist = V(0.32f * s, 0.3f, 0.05f);
            b.Box(arm, (V(0.16f * s, 0.66f, 0.0f) + elbow) / 2f, V(0.05f, 0.11f, 0.05f), 0.015f, ice, Euler(elbow - V(0.16f * s, 0.66f, 0)));
            b.Box(arm, (elbow + wrist) / 2f, V(0.045f, 0.11f, 0.045f), 0.015f, ice, Euler(wrist - elbow), blend: 0.01f);
            b.PaintEll(arm, elbow, V(0.06f, 0.015f, 0.06f), deep, soft: 0.01f);
            b.Spike(arm, wrist + V(0, 0.02f, 0), wrist + V(0.02f * s, -0.16f, 0.02f), 0.045f, ice);
            b.Spike(arm, wrist + V(-0.02f * s, 0.03f, 0.02f), wrist + V(-0.035f * s, -0.1f, 0.04f), 0.03f, pale);
            b.Spike(arm, elbow + V(0.02f * s, 0.0f, -0.02f), elbow + V(0.08f * s, 0.07f, -0.04f), 0.03f, ice);
        });
        // The head, crowned with ice, an angry face breathing cold
        int head = b.Head(V(0, 0.82f, 0.0f));
        var c = V(0, 0.92f, 0.02f);
        var r = V(0.15f, 0.14f, 0.12f);
        b.Ell(head, c, r, ice);
        for (int i = 0; i < 7; i++)
        {
            float a = (i - 3) * 0.42f;
            var root = c + V(MathF.Sin(a) * 0.11f, 0.08f + MathF.Cos(a) * 0.02f, -0.02f - MathF.Abs(MathF.Sin(a)) * 0.02f);
            b.Spike(head, root, root + V(MathF.Sin(a) * 0.05f, 0.1f - MathF.Abs(a) * 0.025f, -0.01f), 0.03f, i % 2 == 0 ? pale : ice);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.055f * s, c.Y + 0.0f);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(head, at + V((0.01f + 0.015f * t) * s, 0.035f, -0.012f), at + V((0.03f + 0.02f * t) * s, 0.045f - 0.008f * t, 0.012f), 0.012f, pale, 0.6f);
            b.Eye(head, at, Outward(c, r, at), 0.021f, Rgb(60, 90, 130), sclera: true, pupil: Rgb(30, 40, 60), glare: true);
        });
        b.PaintEll(head, On(c, r, 0.0f, c.Y - 0.05f), V(0.016f, 0.013f, 0.02f), Rgb(40, 60, 90), soft: 0.005f);
        return b;
    }

    // ------------------------------------------------------------------ Liliacel and Liliacon

    /// <summary>
    /// Liliacel: a small brown bat with its wings wrapped round itself like a cloak, huge round ears glowing pale violet
    /// within, a pug nose and tiny eyes. It hangs upside down in the original picture; here it hovers upright, since the
    /// clips assume a head that is up.
    /// </summary>
    private static PokeBuilder Liliacel()
    {
        var b = new PokeBuilder("Liliacel", 0.42f, BodyPlan.Bird, V(0, 0.22f, 0)) { Coat = Fur }.Hover();
        var brown = Rgb(116, 86, 72);
        var wing = Rgb(82, 64, 58);
        var violet = Rgb(204, 182, 240);
        var nose = Rgb(92, 66, 60);

        var bc = V(0, 0.22f, 0);
        b.Ell(Body, bc, V(0.07f, 0.085f, 0.065f), brown);
        // The wings wrapped round its body, their bones showing as ridges
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, bc + V(0.05f * s, 0.06f, 0));
            var wc = bc + V(0.045f * s, -0.01f, 0.005f);
            b.Ell(w, wc, V(0.05f, 0.1f, 0.075f), wing, V(0, 0, 8f * s), blend: 0.01f);
            b.Spike(w, wc + V(0.01f * s, 0.07f, 0.0f), wc + V(0.035f * s, 0.12f, -0.01f), 0.02f, wing, 0.6f);
            foreach (float t in new[] { -0.4f, 0f, 0.4f })
                b.PaintEll(w, wc + V(0.045f * s, -0.01f + t * 0.06f, 0.0f), V(0.012f, 0.004f, 0.07f), PixelCanvas.Shadow(wing, 0.3f), V(0, 0, 18f * s * t), 0.004f);
        });
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, bc + V(0.025f * s, -0.08f, 0.0f));
            b.Limb(leg, bc + V(0.025f * s, -0.08f, 0.0f), bc + V(0.03f * s, -0.12f, 0.01f), 0.012f, 0.01f, wing);
            Claws(b, leg, bc + V(0.03f * s, -0.125f, 0.015f), V(0.007f, 0, 0), V(0, -0.5f, 0.7f), 0.016f, 0.0055f);
        });
        // The head: big round ears, a pug nose, tiny eyes
        int head = b.Head(bc + V(0, 0.07f, 0.0f));
        var c = bc + V(0, 0.12f, 0.015f);
        var r = V(0.062f, 0.058f, 0.056f);
        b.Ell(head, c, r, brown);
        var snout = On(c, r, 0, c.Y - 0.015f);
        b.Ell(head, snout + V(0, 0, -0.005f), V(0.028f, 0.02f, 0.016f), nose, blend: 0.01f);
        PokeBuilder.Both(s => b.Mark(head, snout + V(0.01f * s, 0.002f, 0.01f), V(0.3f * s, 0.1f, 1f), 0.005f, 0.004f, Rgb(40, 28, 28)));
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.04f * s, 0.04f, -0.01f));
            var ec = c + V(0.09f * s, 0.09f, -0.01f);
            b.Limb(ear, c + V(0.04f * s, 0.04f, -0.01f), ec, 0.018f, 0.018f, brown);
            b.Ell(ear, ec, V(0.048f, 0.06f, 0.016f), brown, V(10f, 0, -30f * s), blend: 0.008f);
            b.PaintEll(ear, ec + V(0, 0, 0.012f), V(0.036f, 0.046f, 0.012f), violet, V(10f, 0, -30f * s));
            var at = On(c, r, 0.03f * s, c.Y + 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(40, 30, 34));
        });
        return Lift(b);
    }

    /// <summary>
    /// Liliacon: a large bat, a sound hunter: wide leathery wings in dark brown with violet skin, enormous ridged ears like
    /// twin speaker horns, violet within, a leaf of a nose, a shaggy ruff, an open fanged mouth and clawed feet.
    /// </summary>
    private static PokeBuilder Liliacon()
    {
        var b = new PokeBuilder("Liliacon", 0.95f, BodyPlan.Bird, V(0, 0.44f, 0)) { Coat = Fur }.Hover();
        var brown = Rgb(84, 64, 56);
        var bone = Rgb(66, 50, 46);
        var membrane = Rgb(116, 72, 150);
        var violet = Rgb(186, 150, 236);
        var ridge = Rgb(150, 110, 210);

        // The wings first: their membranes are carved
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.08f * s, 0.52f, -0.02f));
            BatWing(b, w, V(0.08f * s, 0.52f, -0.02f), V(0.32f * s, 0.68f, -0.05f),
                new[] { V(0.6f * s, 0.56f, -0.06f), V(0.55f * s, 0.34f, -0.06f), V(0.36f * s, 0.2f, -0.05f) }, V(0.07f * s, 0.34f, -0.03f), membrane, bone, 0.016f);
            b.Spike(w, V(0.32f * s, 0.68f, -0.05f), V(0.34f * s, 0.74f, -0.04f), 0.012f, Claw, mat: Shell);
        });
        b.Ell(Body, V(0, 0.44f, 0), V(0.085f, 0.12f, 0.075f), brown);
        FurTufts(b, Body, V(0, 0.5f, 0.01f), V(0.08f, 0.06f, 0.07f), 18, 0.03f, 0.016f, PixelCanvas.Light1(brown, 0.08f), 1.1f, -0.7f, 0.6f);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.35f, 0));
            var foot = V(0.07f * s, 0.24f, 0.03f);
            b.Limb(leg, V(0.05f * s, 0.36f, 0), foot, 0.026f, 0.016f, brown);
            Claws(b, leg, foot + V(0, -0.005f, 0.01f), V(0.01f, 0, 0), V(0, -0.6f, 0.5f), 0.026f, 0.007f);
        });
        int tail = b.Tail(V(0, 0.36f, -0.06f));
        b.Tube(tail, Smooth(3, V(0, 0.36f, -0.06f), V(0, 0.3f, -0.12f), V(0.01f, 0.26f, -0.16f)), 0.025f, 0.01f, brown, blend: 0f);
        FurTufts(b, tail, V(0.01f, 0.27f, -0.15f), V(0.02f, 0.02f, 0.02f), 6, 0.02f, 0.01f, brown, 1.2f, -0.9f, 0.4f);

        int head = b.Head(V(0, 0.56f, 0.02f));
        var c = V(0, 0.62f, 0.04f);
        var r = V(0.065f, 0.058f, 0.062f);
        b.Ell(head, c, r, brown);
        Gape(b, head, On(c, r, 0, c.Y - 0.024f) - V(0, 0, 0.01f), V(0.026f, 0.013f, 0.018f), Rgb(120, 40, 60), 0.013f);
        // A leaf of a nose standing up over a squashed muzzle
        var muzzle = On(c, r, 0, c.Y - 0.002f);
        b.Ell(head, muzzle + V(0, 0, -0.006f), V(0.024f, 0.016f, 0.016f), bone, blend: 0.01f);
        Blade(b, head, muzzle + V(0, 0.005f, 0.004f), muzzle + V(0, 0.05f, 0.0f), 0.016f, bone, V(0, 0.1f, 1f), 0.35f);
        PokeBuilder.Both(s =>
        {
            // The great ears, cones like speaker horns ridged violet inside
            int ear = b.Ear(head, s, c + V(0.04f * s, 0.04f, -0.01f));
            var ec = c + V(0.1f * s, 0.14f, -0.01f);
            b.Limb(ear, c + V(0.04f * s, 0.04f, -0.01f), ec + V(-0.02f * s, -0.05f, 0), 0.024f, 0.028f, brown);
            b.Ell(ear, ec, V(0.06f, 0.11f, 0.03f), brown, V(14f, 0, -22f * s), blend: 0.01f);
            b.PaintEll(ear, ec + V(0, -0.005f, 0.022f), V(0.046f, 0.09f, 0.02f), violet, V(14f, 0, -22f * s));
            for (int i = 0; i < 3; i++)
                b.PaintEll(ear, ec + V(0, -0.02f + 0.03f * i, 0.025f), V(0.04f - 0.008f * i, 0.005f, 0.02f), ridge, V(14f, 0, -22f * s), 0.004f);
            var at = On(c, r, 0.03f * s, c.Y + 0.016f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(36, 28, 32), glare: true);
        });
        return Lift(b);
    }
}
