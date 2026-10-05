using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Popular species from outside the Sinnoh Pokédex (plan 03, decision 3), Kanto's third and last batch in National
// Pokédex order: Voltorb (100) to Mew (151), but for the species of the Sinnoh Pokédex in that range, hand-built
// before. Their forms are in PokemonModels.Megas.cs, PokemonModels.Gigantamax.cs and PokemonModels.Regional.cs with
// the other forms. Helpers shared with the earlier batches are in PokemonModels.Sinnoh1.cs to PokemonModels.Sinnoh4.cs,
// PokemonModels.Kanto1.cs and PokemonModels.Kanto2.cs.
internal static partial class PokemonModels
{
    // ------------------------------------------------------------------ Voltorb line

    /// <summary>
    /// A ball in two halves, <paramref name="top"/> over <paramref name="bottom"/>, parted at its middle: Voltorb's
    /// line, a Poké Ball come to life. The top is painted by a sphere so large that its edge lies flat at the middle.
    /// </summary>
    private static (Vector3 c, Vector3 r) TwoToneBall(PokeBuilder b, float radius, Color top, Color bottom)
    {
        var c = V(0, radius, 0);
        var r = V(radius, radius, radius);
        b.Ell(Body, c, r, bottom);
        b.PaintEll(Body, c + V(0, radius * 12f, 0), V(radius * 12f, radius * 12f, radius * 12f), top, soft: 0.01f);
        return (c, r);
    }

    /// <summary>The grain of an apricorn's wood: faint rings round a ball between heights <paramref name="y0"/> and <paramref name="y1"/>.</summary>
    private static void Grain(PokeBuilder b, Vector3 c, Vector3 r, float y0, float y1, int rings, Color color)
    {
        for (int i = 0; i < rings; i++)
        {
            float y = y0 + (y1 - y0) * (i + 0.5f) / rings, u = (y - c.Y) / r.Y;
            b.PaintTorus(Body, V(0, y, 0), r.X * MathF.Sqrt(MathF.Max(0.05f, 1f - u * u)), 0.016f, color, V(6f + 4f * i, 0, 3f * (i % 2 == 0 ? 1 : -1)));
        }
    }

    private static PokeBuilder Voltorb()
    {
        var b = new PokeBuilder("Voltorb", 0.5f, BodyPlan.Floating, V(0, 0.25f, 0)) { Coat = Shell };
        var (c, r) = TwoToneBall(b, 0.25f, Rgb(226, 66, 74), Rgb(242, 242, 246));
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.075f * s, 0.29f), V(0.3f * s, 0.15f, 1f), 0.04f, sclera: true, pupil: Black, glare: true));
        return b;
    }

    private static PokeBuilder Electrode()
    {
        var b = new PokeBuilder("Electrode", 0.66f, BodyPlan.Floating, V(0, 0.3f, 0)) { Coat = Shell };
        var (c, r) = TwoToneBall(b, 0.3f, Rgb(242, 242, 246), Rgb(226, 66, 74));
        // A wide grin across its lower half
        Grin(b, Body, On(c, r, 0, 0.19f), V(0.17f, 0.045f, 0.05f), Rgb(150, 40, 56));
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.09f * s, 0.41f), V(0.3f * s, 0.15f, 1f), 0.04f, sclera: true, pupil: Black, glare: true));
        return b;
    }

    // ------------------------------------------------------------------ Exeggcute line

    /// <summary>
    /// Two eyes on a round head or egg at <paramref name="c"/> (turned by <paramref name="turn"/>), looking along
    /// <paramref name="look"/>, set <paramref name="apart"/> to either side and raised by <paramref name="lift"/>.
    /// </summary>
    private static void FaceOn(PokeBuilder b, int bone, Vector3 c, Vector3 r, Vector3 turn, Vector3 look, float apart, float lift, float size,
        bool sclera = true, bool glare = false, bool closed = false)
    {
        var d = Vector3.Normalize(look);
        var side = Vector3.Normalize(Vector3.Cross(V(0, 1f, 0), d));
        PokeBuilder.Both(s =>
        {
            var e = Vector3.Normalize(d + side * apart * s + V(0, lift, 0));
            b.Eye(bone, Out(c, r, turn, e), e, size, sclera: sclera, pupil: sclera ? Black : null, glare: glare, closed: closed);
        });
    }

    private static PokeBuilder Exeggcute()
    {
        var b = new PokeBuilder("Exeggcute", 0.5f, BodyPlan.Floating, V(0, 0.1f, 0)) { Coat = Shell };
        var shell = Rgb(244, 218, 222);
        var crack = Rgb(150, 104, 116);
        // Six eggs in a huddle, each with its own face and its own mood, one cracked open at its top
        var eggs = new (Vector3 at, float lean, bool glare, bool closed)[]
        {
            (V(-0.13f, 0.09f, 0.07f), -12f, true, false), (V(0.0f, 0.095f, 0.12f), 0f, false, false), (V(0.13f, 0.09f, 0.06f), 12f, true, false),
            (V(-0.07f, 0.11f, -0.06f), -8f, false, true), (V(0.08f, 0.11f, -0.07f), 8f, true, false), (V(-0.01f, 0.22f, 0.0f), 4f, false, false)
        };
        var er = V(0.075f, 0.092f, 0.075f);
        for (int i = 0; i < eggs.Length; i++)
        {
            var (at, lean, glare, closed) = eggs[i];
            int bone = i == 1 ? Body : b.Part("egg" + i, Body, at - V(0, 0.08f, 0), PokeRole.Head, i * 1.1f);
            var turn = V(0, 0, lean);
            b.Ell(bone, at, er, shell, turn);
            if (i is 0 or 4) b.Mark(bone, Out(at, er, turn, V(-0.6f, 0.4f, 0.7f)), V(-0.6f, 0.4f, 0.7f), 0.025f, 0.01f, crack, MarkShape.Zigzag, 40f);
            if (i == 5)
            {
                // The cracked one: yolk showing through a jagged hole in its top
                b.PaintEll(bone, at + V(0, er.Y * 0.95f, 0), V(0.045f, 0.03f, 0.045f), Rgb(250, 212, 80));
                for (int k = 0; k < 8; k++)
                {
                    float a = k * MathF.Tau / 8f;
                    var rim = at + V(MathF.Cos(a) * 0.046f, er.Y * 0.8f, MathF.Sin(a) * 0.046f);
                    b.Spike(bone, rim - V(0, 0.006f, 0), rim + V(MathF.Cos(a) * 0.006f, 0.016f + 0.006f * (k % 2), MathF.Sin(a) * 0.006f), 0.013f, shell, 0.55f, Shell, 0.004f);
                }
            }
            FaceOn(b, bone, at, er, turn, V(at.X * 1.6f, 0f, 1f), 0.36f, i is 3 or 4 ? 0.55f : 0.15f, 0.016f, glare: glare, closed: closed);
        }
        return b;
    }

    /// <summary>One of Exeggutor's coconut heads at <paramref name="at"/>, looking along <paramref name="look"/>, its mouth open or shut.</summary>
    private static void Coconut(PokeBuilder b, int bone, Vector3 at, float r, Color shell, Vector3 look, bool open, bool glare, bool closed)
    {
        var cr = V(r, r * 0.95f, r);
        b.Ell(bone, at, cr, shell);
        var d = Vector3.Normalize(look);
        var mouth = Out(at, cr, default, d + V(0, -0.35f, 0));
        if (open) Grin(b, bone, mouth, V(r * 0.38f, r * 0.24f, r * 0.3f), Rgb(150, 50, 60));
        else b.Mark(bone, mouth, d, r * 0.3f, r * 0.1f, Rgb(80, 50, 40), MarkShape.Wave);
        FaceOn(b, bone, at, cr, default, d + V(0, 0.15f, 0), 0.4f, 0.1f, r * 0.22f, glare: glare, closed: closed);
    }

    /// <summary>
    /// A long narrow leaf along the curve from <paramref name="root"/> by <paramref name="bend"/> to <paramref name="tip"/>,
    /// narrowing to a point, its face turned up off the curve: a palm's or a yucca's blade. No piece of it is thinner
    /// than <paramref name="thick"/>, or the coarser mesh of a tall model would break it into bits.
    /// </summary>
    private static void LongLeaf(PokeBuilder b, int bone, Vector3 root, Vector3 bend, Vector3 tip, float width, Color color, float thick)
    {
        Vector3 At(float t) => (1 - t) * (1 - t) * root + 2 * (1 - t) * t * bend + t * t * tip;
        var side = Vector3.Normalize(Vector3.Cross(tip - root, V(0, 1f, 0)) + V(0.0001f, 0, 0));
        const int pieces = 4;
        for (int i = 0; i < pieces; i++)
        {
            var from = At(MathF.Max(0f, (i - 0.3f) / pieces));
            var to = At((i + 1f) / pieces);
            var facing = Vector3.Cross(side, Vector3.Normalize(to - from));
            if (facing.Y < 0) facing = -facing;
            float w = width * (1f - 0.16f * i);
            if (i < pieces - 1) Frond(b, bone, from, to + (to - from) * 0.15f, w, color, facing, MathF.Max(0.2f, thick / w), Leaf, 0.008f);
            else Blade(b, bone, from, to, w, color, facing, MathF.Max(0.22f, thick / w), Leaf);
        }
    }

    /// <summary>
    /// Exeggutor and its Alolan form: a palm tree walking on stubby legs, three coconut heads with faces under a
    /// crown of long leaves; the Alolan's trunk grown into a towering neck under drooping palm leaves, a fourth head
    /// at the end of its tail.
    /// </summary>
    private static PokeBuilder ExeggutorBuild(bool alola)
    {
        var b = new PokeBuilder(alola ? "Exeggutor-Alola" : "Exeggutor", 0.92f, BodyPlan.Biped, V(0, 0.33f, 0)) { Coat = Leaf };
        var bark = alola ? Rgb(188, 150, 96) : Rgb(170, 140, 100);
        var ring = PixelCanvas.Mix(bark, Rgb(70, 50, 30), 0.3f);
        var shell = alola ? Rgb(234, 206, 112) : Rgb(242, 228, 152);
        var leaf = alola ? Rgb(64, 140, 70) : Rgb(96, 170, 80);
        var cream = Rgb(240, 226, 176);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.09f * s, 0.2f, 0));
            b.Limb(leg, V(0.09f * s, 0.21f, 0), V(0.11f * s, 0.06f, 0.03f), 0.07f, 0.06f, bark);
            b.Ell(leg, V(0.115f * s, 0.035f, 0.06f), V(0.07f, 0.035f, 0.08f), cream);
            for (int i = -1; i <= 1; i++)
                b.Spike(leg, V(0.115f * s + 0.035f * i, 0.025f, 0.12f), V(0.115f * s + 0.045f * i, 0.016f, 0.155f), 0.014f, White, mat: Shell, blend: 0.004f);
        });
        // A short trunk of a body, ringed like bark
        b.Ell(Body, V(0, 0.33f, 0), V(0.16f, 0.2f, 0.14f), bark);
        foreach (float y in new[] { 0.2f, 0.27f, 0.34f, 0.41f })
            b.PaintTorus(Body, V(0, y, 0), 0.16f * MathF.Sqrt(MathF.Max(0.05f, 1f - MathF.Pow((y - 0.33f) / 0.2f, 2))), 0.016f, ring, sz: 0.875f);
        // The heads and the leaves are laid out round the top of the trunk, or of the Alolan's neck and larger
        float top = 0.5f, k = 1f;
        Vector3 Up(float x, float y, float z) => V(x * k, top + y * k, z * k);
        int crown;
        if (alola)
        {
            // Its trunk grown into a towering neck
            int neck = b.Part("neck", Body, V(0, 0.5f, 0), PokeRole.Segment, 0.3f);
            b.Ell(Body, V(0, 0.5f, 0), V(0.1f, 0.1f, 0.09f), bark, blend: 0.06f);
            var spine = Smooth(3, V(0, 0.46f, 0), V(0.012f, 0.85f, 0), V(-0.012f, 1.2f, 0.012f), V(0, 1.52f, 0));
            b.Tube(neck, spine, 0.07f, 0.055f, bark, blend: 0f);
            for (int i = 0; i < 8; i++)
            {
                // Rings round it where it is at each height, as it bends
                float y = 0.62f + i * 0.11f;
                int j = Math.Max(1, Array.FindIndex(spine, p => p.Y >= y));
                b.PaintTorus(neck, Vector3.Lerp(spine[j - 1], spine[j], (y - spine[j - 1].Y) / (spine[j].Y - spine[j - 1].Y)), 0.068f - i * 0.0018f, 0.012f, ring);
            }
            top = 1.52f;
            k = 1.2f;
            crown = b.Head(V(0, 1.46f, 0), neck);
            // A fourth head at the end of its long tail, with leaves of its own
            int tail = b.Tail(V(0, 0.22f, -0.12f));
            b.Tube(tail, Smooth(3, V(0, 0.22f, -0.11f), V(0.05f, 0.14f, -0.32f), V(0.18f, 0.2f, -0.46f), V(0.26f, 0.32f, -0.44f)), 0.055f, 0.038f, bark, blend: 0f);
            Coconut(b, tail, V(0.28f, 0.39f, -0.41f), 0.075f, shell, V(0.6f, 0, 1f), false, true, false);
            foreach (float a in new[] { -40f, 30f, 100f })
            {
                var dir = V(MathF.Sin(a * Degree), 0, -MathF.Cos(a * Degree));
                var root = V(0.28f, 0.46f, -0.42f);
                LongLeaf(b, tail, root, root + dir * 0.05f + V(0, 0.08f, 0), root + dir * 0.18f + V(0, 0.04f, 0), 0.028f, leaf, 0.016f);
            }
        }
        else
        {
            int tail = b.Tail(V(0, 0.2f, -0.11f));
            b.Limb(tail, V(0, 0.2f, -0.1f), V(0, 0.08f, -0.26f), 0.055f, 0.03f, bark);
            crown = b.Head(V(0, 0.46f, 0));
        }
        // Three coconut heads with faces, each in its own mood: two in front, the third peeping out from behind
        b.Ell(crown, Up(0, -0.02f, 0), V(0.11f, 0.09f, 0.1f) * k, bark, blend: 0.04f);
        Coconut(b, crown, Up(-0.115f, 0.06f, 0.05f), 0.11f * k, shell, V(-0.45f, 0, 1f), true, true, false);
        Coconut(b, crown, Up(0.115f, 0.08f, 0.04f), 0.11f * k, shell, V(0.4f, 0, 1f), false, false, false);
        Coconut(b, crown, Up(-0.11f, 0.14f, -0.11f), 0.1f * k, shell, V(-0.85f, 0.25f, 0.45f), false, false, true);
        // A crown of long leaves sprouting from among them: rising and arching out all round, or the Alolan's
        // drooping like a palm's
        var root0 = Up(0, 0.17f, -0.03f);
        int leaves = b.Part("leaves", crown, root0, PokeRole.Leaf);
        b.Ell(leaves, root0, V(0.08f, 0.07f, 0.08f) * k, leaf);
        for (int i = 0; i < 11; i++)
        {
            float a = i * MathF.Tau / 11f + 0.2f;
            var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
            // The leaves over the faces stand up steeper, the ones behind reach further
            float front = MathF.Max(0f, dir.Z);
            float reach = (0.5f - 0.2f * front + 0.04f * (i % 3)) * k, rise = (0.36f + 0.12f * front) * k;
            float end = alola ? -0.35f - 0.12f * (i % 2) + 0.4f * front : 0.6f - 0.15f * (i % 2);
            var root = root0 + dir * 0.03f * k;
            LongLeaf(b, leaves, root, root + dir * reach * (alola ? 0.4f : 0.3f) + V(0, rise, 0), root + dir * reach + V(0, rise * end, 0),
                0.045f * k, leaf, alola ? 0.017f : 0.012f);
        }
        return b;
    }

    private static PokeBuilder Exeggutor() => ExeggutorBuild(false);

    // ------------------------------------------------------------------ Cubone line

    /// <summary>A bone from <paramref name="a"/> to <paramref name="b2"/>, its ends knobbed in two, held as a club.</summary>
    private static void Club(PokeBuilder b, int bone, Vector3 a, Vector3 b2, float r, Color color)
    {
        b.Limb(bone, a, b2, r, r, color, Shell, 0.01f);
        var d = Vector3.Normalize(b2 - a);
        var across = Vector3.Normalize(Vector3.Cross(d, V(0, 0, 1f)) + V(0.001f, 0, 0));
        foreach (var end in new[] { a, b2 })
            foreach (float s in new[] { -1f, 1f })
                b.Ell(bone, end + across * r * 0.9f * s + (end == a ? -d : d) * r * 0.3f, V(r * 1.45f, r * 1.45f, r * 1.45f), color, mat: Shell, blend: 0.01f);
    }

    /// <summary>
    /// Cubone, Marowak and its Alolan form: a little biped wearing a skull, a bone in its hand; Marowak taller, its
    /// bone raised; the Alolan black, its skull marked with dark flames and its bone burning green at both ends.
    /// </summary>
    private static PokeBuilder SkullBuild(bool marowak, bool alola)
    {
        var b = new PokeBuilder(alola ? "Marowak-Alola" : marowak ? "Marowak" : "Cubone", marowak ? 0.74f : 0.52f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = Fur };
        var skin = alola ? Rgb(66, 50, 78) : Rgb(198, 160, 112);
        var belly = alola ? Rgb(158, 142, 136) : Rgb(242, 226, 184);
        var skull = Rgb(232, 230, 224);
        float h = marowak ? 1.18f : 1f;
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.1f * h, 0));
            b.Limb(leg, V(0.07f * s, 0.11f * h, 0), V(0.08f * s, 0.035f, 0.02f), 0.048f, 0.04f, skin);
            b.Ell(leg, V(0.08f * s, 0.024f, 0.04f), V(0.045f, 0.024f, 0.06f), skin);
            foreach (float t in new[] { -1f, 1f })
                b.Spike(leg, V(0.08f * s + 0.02f * t, 0.018f, 0.085f), V(0.08f * s + 0.024f * t, 0.012f, 0.105f), 0.011f, White, mat: Shell, blend: 0.004f);
        });
        var bc = V(0, 0.2f * h, 0);
        b.Ell(Body, bc, V(0.125f, 0.13f * h, 0.11f), skin);
        b.PaintEll(Body, bc + V(0, -0.02f, 0.07f), V(0.1f, 0.1f * h, 0.07f), belly);
        if (alola)
            foreach (float y in new[] { -0.06f, 0f, 0.06f })
                b.PaintEll(Body, bc + V(0, y * h, 0.105f), V(0.07f, 0.006f, 0.03f), skin, soft: 0.008f);
        // A thick tail with a spike on it
        int tail = b.Tail(V(0, 0.12f * h, -0.08f));
        b.Limb(tail, V(0, 0.12f * h, -0.08f), V(0, 0.05f, -0.22f - (marowak ? 0.06f : 0)), 0.035f, 0.015f, skin);
        b.Spike(tail, V(0, 0.1f * h, -0.14f), V(0, 0.15f * h, -0.16f), 0.018f, skin);
        // Its arms; it holds its bone in one hand, Marowak's raised over its shoulder
        int grip = 0;
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.11f * s, 0.27f * h, 0.02f));
            var hand = s < 0 && marowak ? V(-0.16f, 0.4f * h, 0.02f) : V(0.17f * s, 0.17f * h, 0.08f);
            b.Limb(arm, V(0.1f * s, 0.27f * h, 0.02f), hand, 0.032f, 0.026f, skin);
            b.Ell(arm, hand, V(0.032f, 0.03f, 0.032f), skin);
            if (s < 0) grip = arm;
        });
        var held = marowak ? V(-0.16f, 0.4f * h, 0.02f) : V(-0.17f, 0.17f, 0.08f);
        var (bottom, topEnd) = marowak ? (held + V(0.05f, -0.1f, 0.02f), held + V(-0.08f, 0.22f, -0.08f)) : (held + V(-0.06f, -0.15f, 0.08f), held + V(0.03f, 0.08f, -0.04f));
        Club(b, grip, bottom, topEnd, marowak ? 0.024f : 0.02f, skull);
        if (alola)
            foreach (var end in new[] { bottom, topEnd })
            {
                // Green fire burning at both ends of its bone
                var away = Vector3.Normalize((end - held) + V(0, 0.6f, 0));
                FlameTongue(b, grip, end, end + away * 0.16f + V(0, 0.06f, 0), 0.045f, Rgb(70, 196, 160), Rgb(200, 255, 230));
            }
        // The skull it wears over its head, horned, the eyes looking out of its holes
        int head = b.Head(V(0, 0.32f * h, 0.02f));
        var c = V(0, 0.43f * h, 0.04f);
        var r = marowak ? V(0.12f, 0.1f, 0.13f) : V(0.13f, 0.11f, 0.13f);
        b.Ell(head, c + V(0, -0.08f, 0.05f), V(0.09f, 0.05f, 0.08f), skin);
        b.Ell(head, c, r, skull, mat: Shell);
        // A long snout reaching forward and down, and a ridge of brow over the eye holes
        var snout = c + V(0, -0.05f, marowak ? 0.16f : 0.13f);
        var snoutR = V(0.065f, 0.05f, marowak ? 0.11f : 0.085f);
        b.Ell(head, snout, snoutR, skull, V(12f, 0, 0), Shell, 0.03f);
        b.Ell(head, c + V(0, 0.035f, r.Z * 0.8f), V(r.X * 0.85f, 0.024f, 0.04f), skull, mat: Shell, blend: 0.02f);
        // Two horns swept back from the top of the skull
        PokeBuilder.Both(s => b.Spike(head, c + V(0.065f * s, 0.06f, -0.05f), c + V(0.11f * s, 0.16f, -0.15f), 0.028f, skull, mat: Shell));
        if (alola)
            PokeBuilder.Both(s => b.PaintEll(head, c + V(0.11f * s, 0.0f, 0.04f), V(0.03f, 0.06f, 0.06f), Rgb(44, 36, 52), V(0, 0, 20f * s)));
        PokeBuilder.Both(s =>
        {
            var nostril = V(0.22f * s, 0.2f, 1f);
            b.Mark(head, Out(snout, snoutR, V(12f, 0, 0), nostril), nostril, 0.009f, 0.007f, Rgb(70, 60, 60));
        });
        PokeBuilder.Both(s =>
        {
            var socket = On(c, r, 0.055f * s, c.Y - 0.005f);
            b.PaintEll(head, socket, V(0.038f, 0.032f, 0.034f), Rgb(80, 58, 44));
            b.Eye(head, socket, V(0.35f * s, 0.05f, 1f), 0.017f, Black, glare: marowak);
        });
        return b;
    }

    private static PokeBuilder Cubone() => SkullBuild(false, false);

    private static PokeBuilder Marowak() => SkullBuild(true, false);

    // ------------------------------------------------------------------ Hitmonlee and Hitmonchan

    /// <summary>Raised rings down a limb from <paramref name="a"/> to <paramref name="b2"/>, like the coils of a spring.</summary>
    private static void Coils(PokeBuilder b, int bone, Vector3 a, Vector3 b2, int count, float ring, float tube, Color color)
    {
        var turn = Euler(b2 - a);
        for (int i = 0; i < count; i++)
            b.Torus(bone, Vector3.Lerp(a, b2, (i + 0.5f) / count), ring, tube, color, turn);
    }

    private static PokeBuilder Hitmonlee()
    {
        var b = new PokeBuilder("Hitmonlee", 0.9f, BodyPlan.Biped, V(0, 0.6f, 0)) { Coat = Fur };
        var skin = Rgb(182, 148, 120);
        var band = Rgb(236, 220, 170);
        // Long legs that coil like springs, and great feet
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.46f, 0));
            var ankle = V(0.11f * s, 0.07f, 0.03f);
            b.Limb(leg, V(0.06f * s, 0.47f, 0), ankle, 0.042f, 0.038f, skin);
            Coils(b, leg, V(0.065f * s, 0.42f, 0.002f), ankle + V(0, 0.05f, 0), 6, 0.044f, 0.012f, band);
            b.Ell(leg, V(0.115f * s, 0.035f, 0.07f), V(0.06f, 0.035f, 0.1f), skin);
            for (int i = -1; i <= 1; i++)
                b.Spike(leg, V(0.115f * s + 0.03f * i, 0.025f, 0.15f), V(0.115f * s + 0.036f * i, 0.016f, 0.18f), 0.012f, Rgb(240, 236, 200), mat: Shell, blend: 0.004f);
        });
        // A body that is all head, no neck and no mouth
        var c = V(0, 0.64f, 0);
        var r = V(0.13f, 0.2f, 0.12f);
        b.Ell(Body, c, r, skin);
        b.Ell(Body, V(0, 0.74f, 0.01f), V(0.115f, 0.11f, 0.11f), skin, blend: 0.05f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.11f * s, 0.6f, 0));
            var hand = V(0.34f * s, 0.6f, 0.06f);
            b.Limb(arm, V(0.1f * s, 0.6f, 0), hand, 0.034f, 0.032f, skin);
            Coils(b, arm, V(0.15f * s, 0.6f, 0.01f), hand - V(0.04f * s, 0, 0.005f), 4, 0.036f, 0.011f, band);
            b.Ell(arm, hand + V(0.02f * s, 0, 0.01f), V(0.045f, 0.042f, 0.042f), skin);
        });
        PokeBuilder.Both(s => b.Eye(Body, On(c + V(0, 0.1f, 0.01f), V(0.115f, 0.11f, 0.11f), 0.05f * s, 0.77f), V(0.4f * s, 0.05f, 1f), 0.02f, Black, glare: true));
        return b;
    }

    private static PokeBuilder Hitmonchan()
    {
        var b = new PokeBuilder("Hitmonchan", 0.88f, BodyPlan.Biped, V(0, 0.52f, 0)) { Coat = Fur };
        var skin = Rgb(190, 156, 122);
        var tunic = Rgb(214, 210, 236);
        var glove = Rgb(222, 64, 64);
        var shoe = Rgb(198, 194, 234);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.38f, 0));
            var knee = V(0.1f * s, 0.22f, 0.03f);
            b.Limb(leg, V(0.07f * s, 0.39f, 0), knee, 0.05f, 0.042f, skin);
            b.Limb(leg, knee, V(0.09f * s, 0.07f, 0.01f), 0.042f, 0.036f, skin);
            b.Ell(leg, V(0.09f * s, 0.04f, 0.04f), V(0.052f, 0.04f, 0.075f), shoe);
        });
        // A tunic over its body, flared at the hips like a short skirt
        b.Ell(Body, V(0, 0.55f, 0), V(0.12f, 0.14f, 0.09f), tunic);
        b.Ell(Body, V(0, 0.41f, 0), V(0.145f, 0.07f, 0.115f), tunic, blend: 0.03f);
        b.PaintTorus(Body, V(0, 0.46f, 0), 0.115f, 0.016f, Rgb(70, 60, 80), sz: 0.78f);
        // Arms up to box, in great red gloves
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.12f * s, 0.63f, 0));
            var elbow = V(0.21f * s, 0.53f, 0.05f);
            var fist = V(0.17f * s, 0.67f, 0.16f);
            b.Limb(arm, V(0.11f * s, 0.63f, 0), elbow, 0.036f, 0.032f, skin);
            b.Limb(arm, elbow, fist, 0.032f, 0.034f, skin);
            b.Ell(arm, fist + V(0, 0.01f, 0.02f), V(0.062f, 0.058f, 0.068f), glove, mat: Shell);
            b.PaintTorus(arm, fist - V(0, 0.01f, 0.03f), 0.05f, 0.012f, White, Euler(fist - elbow));
        });
        int head = b.Head(V(0, 0.68f, 0.01f));
        var c = V(0, 0.79f, 0.02f);
        var r = V(0.09f, 0.1f, 0.085f);
        b.Ell(head, c, r, skin);
        // A crest of five points like a crown
        for (int i = 0; i < 5; i++)
        {
            float x = -0.06f + 0.03f * i;
            b.Spike(head, V(x, 0.84f, 0.0f), V(x * 1.6f, 0.95f - MathF.Abs(x) * 0.6f, -0.03f), 0.022f, skin, 0.5f);
        }
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.04f * s, 0.8f), V(0.4f * s, 0.05f, 1f), 0.02f, Rgb(60, 60, 140), glare: true));
        return b;
    }

    // ------------------------------------------------------------------ Koffing line

    /// <summary>Craters over a ball of gas: shallow rings set into its surface along each of <paramref name="where"/>.</summary>
    private static void Craters(PokeBuilder b, int bone, Vector3 c, Vector3 r, Color color, params (Vector3 dir, float size)[] where)
    {
        foreach (var (dir, size) in where)
        {
            var at = Out(c, r, default, dir);
            b.Torus(bone, at - Vector3.Normalize(dir) * size * 0.2f, size, size * 0.35f, color, Euler(Outward(c, r, at)));
        }
    }

    /// <summary>A puff of gas leaking from a ball at <paramref name="at"/>: a little cloud of three rounds.</summary>
    private static void GasPuff(PokeBuilder b, int bone, Vector3 at, Vector3 way, float r, Color color)
    {
        var d = Vector3.Normalize(way);
        b.Ell(bone, at + d * r * 0.6f, V(r, r * 0.85f, r), color, blend: 0.02f);
        b.Ell(bone, at + d * r * 1.5f + V(0, r * 0.5f, 0), V(r * 0.8f, r * 0.7f, r * 0.8f), color, blend: 0.02f);
        b.Ell(bone, at + d * r * 1.6f - V(0, r * 0.5f, 0), V(r * 0.7f, r * 0.6f, r * 0.7f), color, blend: 0.02f);
    }

    /// <summary>A skull and crossbones painted at <paramref name="at"/>, facing <paramref name="facing"/>.</summary>
    private static void Crossbones(PokeBuilder b, int bone, Vector3 at, Vector3 facing, float size, Color color)
    {
        b.Mark(bone, at + V(0, size * 0.55f, 0), facing, size * 0.55f, size * 0.5f, color);
        b.Mark(bone, at - V(0, size * 0.25f, 0), facing, size * 1.1f, size * 0.17f, color, MarkShape.Bar, 35f);
        b.Mark(bone, at - V(0, size * 0.25f, 0), facing, size * 1.1f, size * 0.17f, color, MarkShape.Bar, -35f);
    }

    private static PokeBuilder Koffing()
    {
        var b = new PokeBuilder("Koffing", 0.6f, BodyPlan.Floating, V(0, 0.3f, 0)) { Coat = Shell }.Hover();
        var purple = Rgb(128, 108, 170);
        var gas = Rgb(214, 206, 150);
        var c = V(0, 0.3f, 0);
        var r = V(0.2f, 0.2f, 0.19f);
        b.Ell(Body, c, r, purple);
        Craters(b, Body, c, r, PixelCanvas.Mix(purple, Rgb(40, 30, 60), 0.25f),
            (V(0.7f, 0.6f, 0.3f), 0.03f), (V(-0.75f, 0.5f, 0.2f), 0.028f), (V(0.2f, 0.95f, -0.2f), 0.032f), (V(-0.5f, -0.2f, -0.8f), 0.03f),
            (V(0.6f, -0.3f, -0.7f), 0.026f), (V(0.95f, -0.1f, -0.2f), 0.028f), (V(-0.9f, -0.35f, 0.15f), 0.025f));
        foreach (var (dir, size) in new[] { (V(1f, 0.4f, -0.2f), 0.045f), (V(-1f, 0.3f, -0.3f), 0.04f), (V(0.2f, -0.6f, -1f), 0.04f) })
            GasPuff(b, Body, Out(c, r, default, dir), dir, size, gas);
        var mouth = On(c, r, 0, 0.27f);
        Grin(b, Body, mouth, V(0.085f, 0.04f, 0.045f), Rgb(196, 90, 110));
        Crossbones(b, Body, On(c, r, 0, 0.18f), V(0, -0.4f, 1f), 0.035f, Rgb(224, 214, 172));
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.07f * s, 0.36f), V(0.35f * s, 0.15f, 1f), 0.034f, sclera: true, pupil: Black));
        return Lift(b);
    }

    /// <summary>
    /// Weezing and its Galarian form: two balls of gas grown together, the bigger frowning over its crossbones; the
    /// Galarian's dark, with chimneys smoking on top and a beard of green moss round its mouths.
    /// </summary>
    private static PokeBuilder WeezingBuild(bool galar)
    {
        var b = new PokeBuilder(galar ? "Weezing-Galar" : "Weezing", 0.8f, BodyPlan.Floating, V(-0.06f, 0.34f, 0)) { Coat = Shell }.Hover();
        var body = galar ? Rgb(72, 68, 78) : Rgb(126, 106, 168);
        var gas = galar ? Rgb(244, 246, 248) : Rgb(214, 206, 150);
        var moss = Rgb(140, 196, 90);
        var cBig = V(-0.08f, 0.32f, 0);
        var rBig = V(0.2f, 0.2f, 0.19f);
        var cSmall = V(0.19f, 0.42f, -0.04f);
        var rSmall = V(0.14f, 0.14f, 0.135f);
        int small = b.Part("small", Body, cSmall - V(0.06f, 0, 0), PokeRole.Head, 1.6f, 1f);
        b.Ell(Body, cBig, rBig, body);
        b.Ell(small, cSmall, rSmall, body);
        b.Ell(Body, (cBig + cSmall) * 0.5f, V(0.08f, 0.08f, 0.08f), body, blend: 0.05f);
        var crater = PixelCanvas.Mix(body, Rgb(30, 24, 40), 0.25f);
        Craters(b, Body, cBig, rBig, crater, (V(-0.8f, 0.5f, 0.2f), 0.03f), (V(-0.3f, 0.9f, -0.3f), 0.03f), (V(-0.5f, -0.3f, -0.8f), 0.03f), (V(-0.9f, -0.3f, 0.2f), 0.026f));
        Craters(b, small, cSmall, rSmall, crater, (V(0.8f, 0.4f, 0.2f), 0.024f), (V(0.4f, 0.4f, -0.8f), 0.022f));
        if (galar)
        {
            // Chimneys rising from their heads, smoke pouring out of them, and green moss round their mouths
            foreach (var (at, height, rad, bone) in new[] { (cBig + V(-0.04f, 0.15f, -0.04f), 0.36f, 0.042f, Body), (cSmall + V(0.02f, 0.1f, -0.03f), 0.24f, 0.032f, small) })
            {
                b.Limb(bone, at, at + V(0, height, 0), rad, rad * 0.9f, Rgb(84, 80, 88), Metal);
                b.Torus(bone, at + V(0, height, 0), rad, rad * 0.35f, Rgb(64, 60, 68), mat: Metal);
                GasPuff(b, bone, at + V(0, height + rad * 0.6f, 0), V(0.3f, 1f, 0), rad * 1.1f, gas);
            }
            b.PaintEll(Body, On(cBig, rBig, 0, 0.24f), V(0.15f, 0.07f, 0.08f), moss);
            b.PaintEll(small, Out(cSmall, rSmall, default, V(0.3f, -0.35f, 1f)), V(0.1f, 0.05f, 0.06f), moss);
        }
        else
            foreach (var (dir, size) in new[] { (V(-1f, 0.5f, -0.2f), 0.045f), (V(1f, 0.7f, -0.2f), 0.035f), (V(-0.2f, -0.6f, -1f), 0.04f) })
                GasPuff(b, dir.X > 0 ? small : Body, dir.X > 0 ? Out(cSmall, rSmall, default, dir) : Out(cBig, rBig, default, dir), dir, size, gas);
        // The big head frowns, two fangs over its lip; the small one's mouth is a round O
        var mouth = On(cBig, rBig, -0.08f, 0.27f);
        Grin(b, Body, mouth, V(0.08f, 0.035f, 0.045f), Rgb(120, 40, 60));
        PokeBuilder.Both(s => b.Spike(Body, mouth + V(0.035f * s, 0.03f, -0.01f), mouth + V(0.033f * s, 0.0f, 0.004f), 0.011f, White, mat: Shell, blend: 0.004f));
        var o = Out(cSmall, rSmall, default, V(0.3f, -0.2f, 1f));
        b.Torus(small, o, 0.022f, 0.009f, galar ? Rgb(110, 60, 70) : Rgb(150, 120, 190), Euler(Outward(cSmall, rSmall, o)));
        Crossbones(b, Body, On(cBig, rBig, -0.08f, 0.18f), V(0, -0.4f, 1f), 0.034f, Rgb(224, 214, 172));
        PokeBuilder.Both(s => b.Eye(Body, On(cBig, rBig, -0.08f + 0.065f * s, 0.37f), V(0.35f * s, 0.15f, 1f), 0.032f, sclera: true, pupil: Black, glare: true));
        PokeBuilder.Both(s => b.Eye(small, Out(cSmall, rSmall, default, V(0.3f + 0.4f * s, 0.25f, 1f)), V(0.3f + 0.4f * s, 0.25f, 1f), 0.024f, sclera: true, pupil: Black, glare: galar));
        return Lift(b);
    }

    private static PokeBuilder Weezing() => WeezingBuild(false);

    // ------------------------------------------------------------------ Kangaskhan

    /// <summary>
    /// Kangaskhan, and as it Mega Evolves: a great brown parent with a helmet of hard skin and a baby in the pouch on
    /// its belly; the Mega's baby grown and out of the pouch, standing up before it to fight.
    /// </summary>
    private static PokeBuilder KangaskhanBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Kangaskhan-Mega" : "Kangaskhan", 0.95f, BodyPlan.Biped, V(0, 0.5f, 0)) { Coat = Fur };
        var brown = Rgb(160, 124, 104);
        var dark = Rgb(92, 70, 62);
        var cream = Rgb(234, 220, 160);
        var baby = Rgb(150, 154, 196);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.13f * s, 0.32f, -0.02f));
            var knee = V(0.2f * s, 0.2f, 0.06f);
            b.Limb(leg, V(0.12f * s, 0.33f, -0.02f), knee, 0.1f, 0.075f, brown);
            b.Limb(leg, knee, V(0.19f * s, 0.06f, 0.06f), 0.075f, 0.06f, brown);
            b.Spike(leg, knee + V(0.01f * s, 0.02f, 0.06f), knee + V(0.02f * s, 0.05f, 0.12f), 0.025f, White, mat: Shell);
            b.Ell(leg, V(0.19f * s, 0.035f, 0.11f), V(0.07f, 0.035f, 0.11f), brown);
            foreach (float t in new[] { -1f, 1f })
                b.Spike(leg, V(0.19f * s + 0.03f * t, 0.025f, 0.2f), V(0.19f * s + 0.035f * t, 0.016f, 0.235f), 0.014f, White, mat: Shell, blend: 0.004f);
        });
        b.Ell(Body, V(0, 0.52f, 0), V(0.23f, 0.3f, 0.2f), brown);
        // The pouch on its belly
        var pc = V(0, 0.42f, 0.15f);
        b.Ell(Body, pc, V(0.17f, 0.16f, 0.08f), cream, blend: 0.03f);
        b.Cut(Body, pc + V(0, 0.15f, 0.04f), V(0.11f, 0.05f, 0.08f));
        int tail = b.Tail(V(0, 0.36f, -0.16f));
        b.Limb(tail, V(0, 0.36f, -0.16f), V(0, 0.06f, -0.48f), 0.09f, 0.04f, brown);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.2f * s, 0.66f, 0.02f));
            var hand = V(0.33f * s, 0.74f, 0.1f);
            b.Limb(arm, V(0.19f * s, 0.66f, 0.02f), hand, 0.06f, 0.05f, brown);
            b.Ell(arm, hand, V(0.055f, 0.05f, 0.05f), brown);
            for (int i = -1; i <= 1; i++)
                b.Spike(arm, hand + V(0.02f * i, 0.035f, 0.03f), hand + V(0.03f * i, 0.075f, 0.06f), 0.014f, White, mat: Shell, blend: 0.004f);
        });
        // A long head with a helmet of dark hard skin, small ears at its back, its mouth open
        int head = b.Head(V(0, 0.78f, 0.06f));
        var c = V(0, 0.9f, 0.08f);
        var r = V(0.12f, 0.1f, 0.12f);
        b.Ell(head, c, r, brown);
        var snout = c + V(0, -0.03f, 0.13f);
        b.Ell(head, snout, V(0.075f, 0.06f, 0.09f), brown, blend: 0.03f);
        b.PaintEll(head, c + V(0, 0.06f, 0.02f), V(0.14f, 0.07f, 0.16f), dark);
        PokeBuilder.Both(s => CatEar(b, head, c + V(0.08f * s, 0.06f, -0.08f), c + V(0.13f * s, 0.14f, -0.12f), 0.04f, cream, PixelCanvas.Mix(cream, dark, 0.3f)));
        Grin(b, head, snout + V(0, -0.03f, 0.05f), V(0.05f, 0.025f, 0.04f), Rgb(190, 90, 100));
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.055f * s, c.Y + 0.01f), V(0.45f * s, 0.05f, 1f), 0.022f, Rgb(140, 40, 40), glare: true));
        // The baby: peeping out of the pouch, or the Mega's, standing in front of it to fight
        int child = b.Part("baby", Body, mega ? V(0, 0.1f, 0.28f) : V(0, 0.5f, 0.18f), PokeRole.Head, 2.2f);
        var bc = mega ? V(0, 0.36f, 0.3f) : V(0, 0.53f, 0.2f);
        float k = mega ? 1.8f : 1f;
        if (mega)
        {
            b.Ell(child, V(0, 0.22f, 0.3f), V(0.085f, 0.11f, 0.075f), baby);
            b.PaintEll(child, V(0, 0.2f, 0.37f), V(0.06f, 0.08f, 0.04f), cream);
            PokeBuilder.Both(s =>
            {
                b.Limb(child, V(0.05f * s, 0.15f, 0.3f), V(0.06f * s, 0.03f, 0.32f), 0.03f, 0.026f, baby);
                b.Ell(child, V(0.06f * s, 0.02f, 0.35f), V(0.03f, 0.02f, 0.045f), baby);
                b.Limb(child, V(0.07f * s, 0.28f, 0.3f), V(0.12f * s, 0.33f, 0.4f), 0.024f, 0.022f, baby);
            });
            b.Limb(child, V(0, 0.2f, 0.25f), V(0, 0.3f, 0.21f), 0.035f, 0.035f, baby);
        }
        b.Ell(child, bc, V(0.065f, 0.058f, 0.06f) * k, baby);
        b.PaintEll(child, bc + V(0, 0.035f, 0.0f) * k, V(0.07f, 0.03f, 0.065f) * k, dark);
        PokeBuilder.Both(s => b.Spike(child, bc + V(0.035f * s, 0.035f, -0.02f) * k, bc + V(0.06f * s, 0.075f, -0.04f) * k, 0.016f * k, cream));
        FaceOn(b, child, bc, V(0.065f, 0.058f, 0.06f) * k, default, V(0, 0, 1f), 0.4f, 0.0f, 0.013f * k, sclera: false, glare: mega);
        return b;
    }

    private static PokeBuilder Kangaskhan() => KangaskhanBuild(false);

    // ------------------------------------------------------------------ Horsea line

    /// <summary>
    /// A seahorse's tail: from <paramref name="from"/> down behind <paramref name="center"/>, then curled forward under
    /// it and up into a spiral, <paramref name="turns"/> times round, thinning from <paramref name="thick"/>.
    /// </summary>
    private static void SeahorseTail(PokeBuilder b, int bone, Vector3 from, Vector3 center, float r, float turns, float thick, Color color)
    {
        var curl = Spiral(center, V(0, 0, -1f), V(0, -1f, 0), r, r * 0.25f, -0.35f, MathF.Tau * turns, (int)(14 * turns));
        b.Tube(bone, new[] { from }.Concat(curl).ToArray(), thick, thick * 0.32f, color, blend: 0f);
    }

    private static PokeBuilder Horsea()
    {
        var b = new PokeBuilder("Horsea", 0.55f, BodyPlan.Floating, V(0, 0.27f, 0)) { Coat = Scales }.Hover();
        var blue = Rgb(110, 192, 234);
        var cream = Rgb(246, 228, 164);
        var band = PixelCanvas.Mix(cream, Rgb(150, 120, 70), 0.35f);
        // A round belly, cream down its front in bands, and its tail curled forward under it
        b.Ell(Body, V(0, 0.27f, 0.01f), V(0.075f, 0.105f, 0.08f), blue);
        int tail = b.Tail(V(0, 0.19f, -0.02f));
        SeahorseTail(b, tail, V(0, 0.21f, -0.01f), V(0, 0.1f, 0.02f), 0.08f, 1.5f, 0.05f, blue);
        b.PaintEll(Body, V(0, 0.26f, 0.06f), V(0.06f, 0.11f, 0.05f), cream);
        foreach (float y in new[] { 0.2f, 0.24f, 0.28f, 0.32f })
            b.PaintEll(Body, V(0, y, 0.085f), V(0.05f, 0.004f, 0.03f), band, soft: 0.006f);
        // A small fin on its back, ribbed like a fan
        int fin = b.Part("fin", Body, V(0, 0.28f, -0.06f), PokeRole.Fin, 0.5f);
        Frond(b, fin, V(0, 0.27f, -0.05f), V(0, 0.33f, -0.19f), 0.05f, cream, V(1f, 0, 0), 0.24f, Shell);
        foreach (float t in new[] { -1f, 0f, 1f })
            b.PaintEll(fin, V(0, 0.3f + 0.02f * t, -0.13f), V(0.02f, 0.004f, 0.06f), band, V(-25f + 22f * t, 0, 0), 0.005f);
        // A round head, a tube of a snout open at its end, three spikes down the back of the head
        int head = b.Head(V(0, 0.36f, 0));
        var c = V(0, 0.43f, 0);
        var r = V(0.096f, 0.092f, 0.1f);
        b.Ell(head, c, r, blue);
        var tip = V(0, 0.415f, 0.205f);
        b.Limb(head, V(0, 0.42f, 0.07f), tip, 0.037f, 0.034f, blue);
        b.Ell(head, tip, V(0.041f, 0.041f, 0.024f), blue, blend: 0.015f);
        BellMouth(b, head, tip + V(0, 0, 0.014f), V(0, 0, 1f), 0.022f, blue, Rgb(40, 50, 70), 0.9f);
        foreach (var (from, to) in new[] { (V(0, 0.49f, -0.05f), V(0, 0.57f, -0.11f)), (V(0, 0.45f, -0.08f), V(0, 0.49f, -0.17f)), (V(0, 0.4f, -0.08f), V(0, 0.39f, -0.16f)) })
            b.Spike(head, from, to, 0.024f, blue);
        PokeBuilder.Both(s =>
        {
            var look = V(0.75f * s, 0.2f, 0.62f);
            b.Eye(head, Out(c, r, default, look), look, 0.034f, sclera: true, pupil: Rgb(196, 50, 56));
        });
        return Lift(b);
    }

    private static PokeBuilder Seadra()
    {
        var b = new PokeBuilder("Seadra", 0.8f, BodyPlan.Floating, V(0, 0.38f, 0)) { Coat = Scales }.Hover();
        var blue = Rgb(104, 186, 230);
        var cream = Rgb(234, 216, 162);
        var plate = PixelCanvas.Mix(cream, Rgb(150, 120, 70), 0.4f);
        b.Ell(Body, V(0, 0.36f, 0), V(0.09f, 0.13f, 0.09f), blue);
        int tail = b.Tail(V(0, 0.25f, -0.02f));
        SeahorseTail(b, tail, V(0, 0.27f, -0.02f), V(0, 0.13f, 0.03f), 0.11f, 1.6f, 0.066f, blue);
        // Its belly plated in cream scales, row over row
        b.PaintEll(Body, V(0, 0.35f, 0.07f), V(0.072f, 0.14f, 0.05f), cream);
        for (int i = 0; i < 5; i++)
            b.PaintEll(Body, V(0, 0.27f + i * 0.04f, 0.1f), V(0.062f, 0.005f, 0.03f), plate, soft: 0.006f);
        // Great spiked fins on its back, cream from blue roots, flaring like a fan
        int fin = b.Part("fin", Body, V(0, 0.42f, -0.08f), PokeRole.Fin, 0.5f);
        foreach (var (to, w, x) in new[] { (V(0, 0.66f, -0.34f), 0.05f, -0.03f), (V(0, 0.5f, -0.44f), 0.055f, 0.03f), (V(0, 0.34f, -0.38f), 0.045f, 0f) })
            Blade(b, fin, V(0, 0.42f, -0.07f), to + V(x, 0, 0), w, cream, V(1f, 0, 0), 0.3f, Shell);
        b.PaintEll(fin, V(0, 0.43f, -0.11f), V(0.05f, 0.08f, 0.08f), blue);
        int head = b.Head(V(0, 0.5f, 0));
        var c = V(0, 0.58f, 0.01f);
        var r = V(0.095f, 0.09f, 0.11f);
        b.Ell(head, c, r, blue);
        // A long snout open at its end, flaring a little
        var tip = V(0, 0.565f, 0.3f);
        b.Limb(head, V(0, 0.565f, 0.08f), tip, 0.038f, 0.034f, blue);
        b.Ell(head, tip, V(0.046f, 0.046f, 0.028f), blue, blend: 0.015f);
        BellMouth(b, head, tip + V(0, 0, 0.018f), V(0, 0, 1f), 0.028f, blue, Rgb(40, 50, 70), 0.9f);
        // A crest of spines over the head and frills of them down its cheeks
        foreach (var (from, to, w) in new[] { (V(0, 0.66f, 0.03f), V(0, 0.77f, -0.05f), 0.032f), (V(0, 0.65f, -0.05f), V(0, 0.72f, -0.17f), 0.03f), (V(0, 0.6f, -0.09f), V(0, 0.6f, -0.22f), 0.028f) })
            Blade(b, head, from, to, w, blue, V(1f, 0, 0), 0.4f);
        PokeBuilder.Both(s =>
        {
            Blade(b, head, V(0.07f * s, 0.54f, -0.02f), V(0.18f * s, 0.5f, -0.12f), 0.032f, blue, V(s, 0.4f, 0), 0.4f);
            Blade(b, head, V(0.065f * s, 0.6f, -0.05f), V(0.17f * s, 0.65f, -0.16f), 0.03f, blue, V(s, 0.4f, 0), 0.4f);
        });
        // Narrowed eyes under a heavy brow
        PokeBuilder.Both(s =>
        {
            var look = V(0.72f * s, 0.25f, 0.65f);
            b.Ell(head, Out(c, r, default, look + V(0, 0.35f, 0)), V(0.04f, 0.016f, 0.03f), blue, V(0, 0, -12f * s), blend: 0.02f);
            b.Eye(head, Out(c, r, default, look), look, 0.026f, Rgb(176, 60, 48), glare: true);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Staryu line

    /// <summary>
    /// Five points of a star standing on edge round <paramref name="c"/>, <paramref name="length"/> long, the first
    /// <paramref name="turn"/> degrees round from straight up, each <paramref name="flat"/> as thick front to back as
    /// it is wide.
    /// </summary>
    private static void StarPoints(PokeBuilder b, int bone, Vector3 c, float length, float width, float turn, float flat, Color color)
    {
        for (int i = 0; i < 5; i++)
        {
            float a = (turn + i * 72f) * Degree;
            var d = V(MathF.Sin(a), MathF.Cos(a), 0);
            b.Spike(bone, c + d * width * 0.3f, c + d * length, width, color, flat, Shell);
        }
    }

    /// <summary>
    /// The core of Staryu's line on its front at <paramref name="at"/>: a red jewel in a ring of gold, with
    /// <paramref name="spokes"/> short bars standing out from the ring, the first <paramref name="turn"/> degrees round
    /// from straight up.
    /// </summary>
    private static void StarCore(PokeBuilder b, int bone, Vector3 at, float r, int spokes, float turn, Color gold, Color jewel)
    {
        b.Ell(bone, at - V(0, 0, r * 0.25f), V(r * 0.95f, r * 0.95f, r * 0.45f), PixelCanvas.Mix(gold, Rgb(120, 90, 30), 0.3f), mat: Metal);
        b.Torus(bone, at, r, r * 0.24f, gold, V(90f, 0, 0), mat: Metal);
        for (int i = 0; i < spokes; i++)
        {
            float a = (turn + i * 360f / spokes) * Degree;
            var d = V(MathF.Sin(a), MathF.Cos(a), 0);
            b.Limb(bone, at + d * r * 1.05f, at + d * r * 1.75f - V(0, 0, r * 0.25f), r * 0.17f, r * 0.15f, gold, Metal, 0.006f);
        }
        b.Ell(bone, at + V(0, 0, r * 0.15f), V(r * 0.7f, r * 0.7f, r * 0.45f), jewel, mat: Glow, blend: 0.01f);
    }

    private static PokeBuilder Staryu()
    {
        var b = new PokeBuilder("Staryu", 0.62f, BodyPlan.Floating, V(0, 0.36f, 0)) { Coat = Shell }.Hover();
        var brown = Rgb(176, 132, 80);
        var c = V(0, 0.36f, 0);
        b.Ell(Body, c, V(0.13f, 0.13f, 0.075f), brown);
        StarPoints(b, Body, c, 0.38f, 0.12f, 0f, 0.42f, brown);
        StarCore(b, Body, c + V(0, 0, 0.075f), 0.06f, 5, 0f, Rgb(244, 206, 84), Rgb(226, 62, 86));
        return Lift(b);
    }

    private static PokeBuilder Starmie()
    {
        var b = new PokeBuilder("Starmie", 0.82f, BodyPlan.Floating, V(0, 0.42f, 0)) { Coat = Shell }.Hover();
        var purple = Rgb(132, 118, 190);
        var c = V(0, 0.42f, 0);
        b.Ell(Body, c, V(0.14f, 0.14f, 0.09f), purple);
        StarPoints(b, Body, c + V(0, 0, 0.02f), 0.42f, 0.13f, 0f, 0.42f, purple);
        // A second star behind the first, turned half a point
        StarPoints(b, Body, c - V(0, 0, 0.05f), 0.4f, 0.12f, 36f, 0.42f, PixelCanvas.Mix(purple, Rgb(60, 50, 110), 0.25f));
        StarCore(b, Body, c + V(0, 0, 0.09f), 0.068f, 8, 22.5f, Rgb(240, 206, 96), Rgb(214, 48, 78));
        return Lift(b);
    }

    // ------------------------------------------------------------------ Jynx

    private static PokeBuilder Jynx()
    {
        var b = new PokeBuilder("Jynx", 0.86f, BodyPlan.Biped, V(0, 0.48f, 0)) { Coat = Fur };
        var red = Rgb(214, 72, 66);
        var purple = Rgb(112, 92, 168);
        var hair = Rgb(244, 226, 156);
        var gold = Rgb(242, 200, 84);
        var sleeve = Rgb(240, 240, 246);
        // A long red gown flaring to the ground in pleats, its hem cut level
        b.Limb(Body, V(0, 0.08f, 0), V(0, 0.44f, 0), 0.25f, 0.11f, red);
        for (int i = 0; i < 9; i++)
        {
            float a = i * MathF.Tau / 9f + 0.35f;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            b.Limb(Body, d * 0.1f + V(0, 0.42f, 0), d * 0.25f + V(0, 0.02f, 0), 0.015f, 0.035f, red, blend: 0.03f);
        }
        b.CutBox(Body, V(0, -0.3f, 0), V(0.6f, 0.3f, 0.6f), Quaternion.Identity);
        // A red bodice, gold along its neckline
        b.Ell(Body, V(0, 0.5f, 0), V(0.12f, 0.1f, 0.095f), red);
        PokeBuilder.Both(s => b.Ell(Body, V(0.05f * s, 0.52f, 0.07f), V(0.055f, 0.05f, 0.045f), red, blend: 0.02f));
        b.PaintEll(Body, V(0, 0.578f, 0.05f), V(0.12f, 0.018f, 0.1f), gold);
        // White sleeves held out, great purple hands with their fingers spread
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.11f * s, 0.54f, 0));
            var wrist = V(0.29f * s, 0.56f, 0.05f);
            b.Limb(arm, V(0.1f * s, 0.54f, 0), wrist, 0.04f, 0.036f, sleeve);
            b.Ell(arm, wrist - V(0.015f * s, 0, 0), V(0.032f, 0.046f, 0.046f), sleeve, blend: 0.02f);
            var palm = wrist + V(0.05f * s, 0.012f, 0.01f);
            b.Ell(arm, palm, V(0.042f, 0.045f, 0.022f), purple, V(0, 0, -15f * s));
            foreach (float deg in new[] { -40f, 2f, 28f, 54f, 80f })
            {
                var d = V(MathF.Cos(deg * Degree) * s, MathF.Sin(deg * Degree), 0.08f);
                b.Limb(arm, palm + d * 0.03f, palm + d * (deg < 0f ? 0.075f : 0.09f), 0.013f, 0.012f, purple, blend: 0.006f);
            }
        });
        // Long golden hair parted in the middle, falling past its shoulders and down its back; its purple face looks
        // out of an opening in it, wide-eyed, its lips pursed
        int head = b.Head(V(0, 0.6f, 0));
        var c = V(0, 0.72f, 0.01f);
        var r = V(0.1f, 0.125f, 0.095f);
        b.Ell(head, c + V(0, 0.03f, -0.015f), V(0.125f, 0.135f, 0.115f), hair);
        PokeBuilder.Both(s => b.Ell(head, V(0.118f * s, 0.52f, -0.005f), V(0.042f, 0.19f, 0.05f), hair, V(0, 0, -5f * s), blend: 0.03f));
        b.Ell(head, V(0, 0.55f, -0.07f), V(0.13f, 0.19f, 0.05f), hair, blend: 0.03f);
        b.Cut(head, c + V(0, -0.02f, 0.09f), V(0.085f, 0.11f, 0.1f));
        b.PaintEll(head, c + V(0, 0.165f, 0.04f), V(0.006f, 0.03f, 0.09f), PixelCanvas.Mix(hair, Rgb(150, 120, 60), 0.4f), soft: 0.006f);
        b.Ell(head, c, r, purple);
        var mouth = On(c, r, 0, 0.665f);
        b.PaintEll(head, mouth, V(0.016f, 0.02f, 0.02f), Rgb(110, 40, 64));
        b.Torus(head, mouth + V(0, 0, 0.004f), 0.02f, 0.012f, Rgb(244, 156, 176), V(90f, 0, 0), sx: 0.85f, sz: 1.15f, mat: Shell);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.042f * s, 0.745f), V(0.4f * s, 0.05f, 1f), 0.03f, sclera: true, pupil: Black));
        return b;
    }

    // ------------------------------------------------------------------ Pinsir

    /// <summary>
    /// Pinsir and its Mega Evolution: a stag beetle on short legs, its body all head, a grille of teeth down its front,
    /// two great horns spiked along their insides and its arms raised; the Mega's horns branched like antlers, flying on
    /// yellow wings spread out of a shell striped orange.
    /// </summary>
    private static PokeBuilder PinsirBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Pinsir-Mega" : "Pinsir", 0.9f, BodyPlan.Biped, V(0, 0.44f, 0)) { Coat = Shell };
        if (mega) b.Hover();
        var brown = mega ? Rgb(166, 150, 140) : Rgb(176, 162, 150);
        var dark = PixelCanvas.Mix(brown, Rgb(60, 50, 50), 0.35f);
        var horn = Rgb(232, 232, 236);
        // Short legs, each foot two claws
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.09f * s, 0.25f, 0));
            var knee = V(0.12f * s, 0.12f, 0.03f);
            b.Limb(leg, V(0.09f * s, 0.26f, 0), knee, 0.05f, 0.04f, brown);
            b.Limb(leg, knee, V(0.12f * s, 0.05f, 0.04f), 0.04f, 0.035f, brown);
            foreach (float t in new[] { -1f, 1f })
                b.Spike(leg, V(0.12f * s + 0.02f * t, 0.04f, 0.05f), V(0.12f * s + 0.03f * t, 0.008f, 0.11f), 0.018f, horn, blend: 0.006f);
        });
        var c = V(0, 0.44f, 0);
        var r = V(0.17f, 0.22f, 0.14f);
        b.Ell(Body, c, r, brown);
        // Bands round its lower half, a beetle's segments
        foreach (float y in new[] { 0.28f, 0.32f, 0.36f })
            b.PaintTorus(Body, V(0, y, 0), r.X * MathF.Sqrt(1f - MathF.Pow((y - c.Y) / r.Y, 2)), 0.008f, dark, sz: r.Z / r.X);
        // A great mouth down its front: a dark hollow behind a grille of teeth from either side, a lip round it
        var mc = On(c, r, 0, 0.42f);
        b.Cut(Body, mc + V(0, 0, 0.01f), V(0.055f, 0.085f, 0.04f));
        b.PaintEll(Body, mc - V(0, 0, 0.02f), V(0.06f, 0.09f, 0.05f), Rgb(46, 36, 40));
        for (int i = 0; i < 6; i++)
        {
            float y = mc.Y - 0.065f + i * 0.026f;
            PokeBuilder.Both(s => b.Limb(Body, V(0.055f * s, y, mc.Z - 0.025f), V(0.012f * s, y, mc.Z - 0.02f), 0.0095f, 0.0085f, White, blend: 0.004f));
        }
        b.Torus(Body, mc, 0.058f, 0.012f, brown, V(90f, 0, 0), sz: 1.5f);
        // Small fierce eyes under a heavy brow
        b.Ell(Body, On(c, r, 0, 0.6f) - V(0, 0, 0.02f), V(0.11f, 0.026f, 0.04f), brown, blend: 0.03f);
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.055f * s, 0.555f), V(0.4f * s, 0.15f, 1f), 0.02f, sclera: true, pupil: Black, glare: true));
        // Two great horns curving up and in, spikes along their insides; the Mega's taller, branched like antlers
        int head = b.Head(V(0, 0.6f, 0));
        PokeBuilder.Both(s =>
        {
            float top = mega ? 1.12f : 1.0f;
            var path = Smooth(3, V(0.07f * s, 0.6f, 0), V(0.16f * s, 0.74f, 0.01f), V(0.16f * s, (0.9f + top) * 0.5f, 0.02f), V(0.07f * s, top, 0.03f));
            b.Tube(head, path, 0.05f, 0.016f, horn, Shell, 0f);
            foreach (float t in mega ? new[] { 0.3f, 0.45f, 0.6f, 0.75f, 0.88f } : new[] { 0.32f, 0.5f, 0.68f, 0.84f })
            {
                int i = (int)(t * (path.Length - 2));
                var p = path[i];
                var d = Vector3.Normalize(path[i + 1] - path[i]);
                var inward = Vector3.Normalize(V(-s, 0, 0) - d * Vector3.Dot(V(-s, 0, 0), d));
                float rr = 0.05f + (0.016f - 0.05f) * t;
                b.Spike(head, p + inward * rr * 0.4f, p + inward * (rr + 0.04f) + d * 0.012f, 0.016f, horn, mat: Shell, blend: 0.006f);
            }
            foreach (float t in mega ? new[] { 0.42f, 0.62f, 0.8f } : new[] { 0.55f })
            {
                int i = (int)(t * (path.Length - 2));
                var p = path[i];
                var d = Vector3.Normalize(path[i + 1] - path[i]);
                var outward = Vector3.Normalize(V(s, 0, 0) - d * Vector3.Dot(V(s, 0, 0), d));
                float rr = 0.05f + (0.016f - 0.05f) * t;
                b.Spike(head, p + outward * rr * 0.4f, p + outward * (rr + (mega ? 0.08f : 0.045f)) + d * 0.04f, mega ? 0.022f : 0.017f, horn, mat: Shell, blend: 0.006f);
            }
        });
        // Arms raised, jointed like a beetle's, each hand three claws
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.15f * s, 0.5f, 0.02f));
            var shoulder = V(0.16f * s, 0.5f, 0.02f);
            var elbow = V(0.3f * s, mega ? 0.46f : 0.52f, 0.06f);
            var hand = mega ? V(0.36f * s, 0.32f, 0.12f) : V(0.33f * s, 0.72f, 0.08f);
            b.Ell(arm, shoulder, V(0.042f, 0.042f, 0.042f), brown);
            b.Limb(arm, shoulder, elbow, 0.03f, 0.028f, brown);
            b.Ell(arm, elbow, V(0.035f, 0.035f, 0.035f), brown);
            b.Limb(arm, elbow, hand, 0.028f, 0.026f, brown);
            b.Ell(arm, hand, V(0.036f, 0.036f, 0.032f), brown);
            var reach = Vector3.Normalize(hand - elbow);
            Digits(b, arm, hand + reach * 0.02f, reach + V(0, 0, 0.2f), V(0.45f, 0, 0.3f), 0.055f, 0.014f, horn, Shell);
        });
        if (mega)
        {
            // The two halves of its shell raised behind it, dark and striped orange, spiked orange along their tops,
            // and great yellow wings spread from under them, veined orange
            var orange = Rgb(236, 128, 40);
            PokeBuilder.Both(s =>
            {
                int wing = b.Wing(s, V(0.08f * s, 0.56f, -0.1f));
                var cc = V(0.15f * s, 0.6f, -0.15f);
                var cr = V(0.13f, 0.21f, 0.05f);
                var turn = V(0, 25f * s, -28f * s);
                b.Ell(wing, cc, cr, Rgb(56, 48, 50), turn, Shell);
                foreach (float t in new[] { -0.08f, 0f, 0.08f })
                    b.PaintEll(wing, cc + V(t * s * 0.6f, t, 0), V(0.012f, 0.2f, 0.07f), orange, turn, 0.008f);
                foreach (float t in new[] { 0.2f, 0.55f, 0.85f })
                {
                    var at = Out(cc, cr, turn, V(0.6f * s, 0.9f - t, -0.4f));
                    b.Spike(wing, at - V(0, 0.01f, 0), at + V(0.04f * s, 0.07f, -0.03f), 0.016f, orange, mat: Shell, blend: 0.006f);
                }
                var yellow = Rgb(242, 228, 82);
                Frond(b, wing, V(0.12f * s, 0.58f, -0.14f), V(0.56f * s, 0.66f, -0.22f), 0.13f, yellow, V(0, 0.2f, -1f), 0.12f, Shell);
                Frond(b, wing, V(0.12f * s, 0.52f, -0.14f), V(0.5f * s, 0.4f, -0.2f), 0.1f, yellow, V(0, 0.2f, -1f), 0.14f, Shell);
                foreach (var (from, to) in new[] { (V(0.16f, 0.6f, -0.17f), V(0.5f, 0.66f, -0.23f)), (V(0.2f, 0.58f, -0.18f), V(0.42f, 0.7f, -0.22f)), (V(0.16f, 0.52f, -0.17f), V(0.44f, 0.43f, -0.21f)) })
                {
                    var (a, z) = (V(from.X * s, from.Y, from.Z), V(to.X * s, to.Y, to.Z));
                    b.PaintEll(wing, (a + z) / 2f, V(0.008f, Vector3.Distance(a, z) / 2f, 0.04f), orange, Euler(z - a), 0.006f);
                }
            });
            return Lift(b);
        }
        return b;
    }

    private static PokeBuilder Pinsir() => PinsirBuild(false);

    // ------------------------------------------------------------------ Tauros

    /// <summary>
    /// Tauros and its Paldean breeds (0 is Kanto's, 1 the Combat Breed, 2 the Blaze, 3 the Aqua): a bull, its mane
    /// shaggy over its shoulders, three studs on its brow and three tails, each with a tuft; the Paldean breeds black,
    /// the Blaze Breed's horns longer, its tails braided into one and its mane streaked red, the Aqua Breed's horns
    /// curled back like a ram's and its mane marked with blue drops.
    /// </summary>
    private static PokeBuilder TaurosBuild(string name, int breed)
    {
        bool paldea = breed > 0;
        var b = new PokeBuilder(name, 1f, BodyPlan.Quadruped, V(0, 0.44f, -0.04f)) { Coat = Fur };
        var hide = paldea ? Rgb(60, 58, 62) : Rgb(206, 158, 98);
        var mane = paldea ? Rgb(42, 40, 44) : Rgb(116, 106, 100);
        var shin = paldea ? Rgb(52, 50, 54) : Rgb(112, 78, 62);
        var hoof = paldea ? Rgb(36, 36, 42) : Rgb(118, 124, 160);
        var horn = paldea ? Rgb(172, 172, 178) : Rgb(150, 152, 170);
        var tuft = paldea ? Rgb(36, 36, 40) : Rgb(96, 98, 140);
        foreach (var (z, front) in new[] { (0.2f, true), (-0.24f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.11f * s, 0.38f, z), front);
                var knee = V(0.12f * s, 0.2f, z + (front ? 0.02f : -0.04f));
                b.Limb(leg, V(0.11f * s, 0.4f, z), knee, 0.078f, 0.05f, front ? mane : hide);
                b.Limb(leg, knee, V(0.12f * s, 0.06f, z + 0.01f), 0.042f, 0.036f, shin);
                b.Ell(leg, V(0.12f * s, 0.034f, z + 0.02f), V(0.043f, 0.034f, 0.05f), hoof, mat: Shell);
            });
        var bc = V(0, 0.45f, -0.04f);
        var br = V(0.185f, 0.17f, 0.3f);
        b.Ell(Body, bc, br, hide);
        // A shaggy mane over its shoulders and chest
        var mc = V(0, 0.5f, 0.15f);
        var mr = V(0.215f, 0.2f, 0.17f);
        b.Ell(Body, mc, mr, mane, blend: 0.04f);
        FurTufts(b, Body, mc, mr, 36, 0.06f, 0.035f, mane, 0.75f, -0.5f, 0.5f);
        if (breed == 2)
            PokeBuilder.Both(s => b.PaintEll(Body, V(0.19f * s, 0.57f, 0.12f), V(0.05f, 0.014f, 0.14f), Rgb(214, 54, 50), V(-15f, 0, 0), 0.008f));
        if (breed == 3)
            PokeBuilder.Both(s =>
            {
                foreach (var (y, z) in new[] { (0.58f, 0.2f), (0.5f, 0.08f), (0.44f, 0.22f) })
                {
                    var n = V(s, (y - mc.Y) / mr.Y, (z - mc.Z) / mr.Z * 0.6f);
                    b.Mark(Body, Out(mc, mr, default, n), n, 0.012f, 0.022f, Rgb(64, 146, 236));
                }
            });
        // Three tails whipping up behind it, each with a tuft; the Blaze Breed's braided into one
        int tail = b.Tail(V(0, 0.5f, -0.31f));
        var root = V(0, 0.5f, -0.31f);
        if (breed == 2)
        {
            var path = Smooth(3, root, V(0, 0.6f, -0.44f), V(0, 0.82f, -0.47f), V(0, 0.97f, -0.38f), V(0, 1.0f, -0.28f));
            b.Tube(tail, path, 0.028f, 0.02f, hide, blend: 0f);
            for (int i = 1; i < path.Length - 1; i += 2)
                b.PaintEll(tail, path[i], V(0.032f, 0.008f, 0.032f), PixelCanvas.Mix(hide, Rgb(20, 20, 24), 0.5f), Euler(path[i + 1] - path[i]), 0.006f);
            var end = path[^1];
            foreach (float sx in new[] { -1f, 0f, 1f })
                PointedLeaf(b, tail, end - V(0, 0, 0.01f), end + V(0.06f * sx, 0.07f - 0.02f * MathF.Abs(sx), 0.06f), 0.03f, tuft, V(0, 0.3f, -1f), 0.35f);
        }
        else
            foreach (float sx in new[] { -1f, 0f, 1f })
            {
                var path = Smooth(3, root, root + V(0.04f * sx, 0.1f, -0.12f), root + V(0.1f * sx, 0.27f, -0.2f), root + V(0.15f * sx, 0.38f, -0.12f));
                b.Tube(tail, path, 0.016f, 0.011f, paldea ? hide : mane, blend: 0f);
                var end = path[^1];
                var way = Vector3.Normalize(end - path[^2]);
                PointedLeaf(b, tail, end - way * 0.01f, end + way * 0.1f, 0.03f, tuft, V(0, 0.2f, -1f), 0.35f);
            }
        // A broad head low on its neck: a muzzle, three studs on its brow and its horns
        int head = b.Head(V(0, 0.5f, 0.3f));
        var c = V(0, 0.52f, 0.37f);
        var r = V(0.085f, 0.08f, 0.09f);
        b.Ell(head, c, r, hide);
        var muzzle = V(0, 0.47f, 0.45f);
        var mzr = V(0.065f, 0.052f, 0.06f);
        b.Ell(head, muzzle, mzr, paldea ? hide : PixelCanvas.Mix(hide, Rgb(120, 90, 60), 0.3f), blend: 0.03f);
        PokeBuilder.Both(s =>
        {
            var n = V(0.35f * s, 0.2f, 1f);
            b.Mark(head, Out(muzzle, mzr, default, n), n, 0.011f, 0.008f, Rgb(46, 38, 40));
        });
        foreach (var (x, y) in new[] { (0f, 0.565f), (-0.028f, 0.54f), (0.028f, 0.54f) })
            b.Ell(head, On(c, r, x, y), V(0.016f, 0.016f, 0.012f), horn, mat: Shell, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            var path = breed switch
            {
                0 => Smooth(3, V(0.06f * s, 0.56f, 0.35f), V(0.15f * s, 0.585f, 0.36f), V(0.2f * s, 0.65f, 0.36f), V(0.18f * s, 0.74f, 0.38f)),
                1 => Smooth(3, V(0.06f * s, 0.56f, 0.35f), V(0.15f * s, 0.58f, 0.37f), V(0.2f * s, 0.6f, 0.44f), V(0.2f * s, 0.64f, 0.51f)),
                2 => Smooth(3, V(0.06f * s, 0.56f, 0.35f), V(0.17f * s, 0.58f, 0.37f), V(0.25f * s, 0.61f, 0.46f), V(0.27f * s, 0.66f, 0.58f)),
                _ => new[] { V(0.05f * s, 0.56f, 0.35f) }.Concat(Spiral(V(0.12f * s, 0.53f, 0.31f), V(0, 1f, 0), V(0, 0, -1f), 0.07f, 0.035f, 0f, MathF.PI * 1.4f, 16)
                    .Select((p, i) => p + V(0.035f * s * i / 16f, 0, 0))).ToArray()
            };
            b.Tube(head, path, breed == 3 ? 0.034f : 0.03f, breed == 3 ? 0.014f : 0.012f, horn, Shell, 0f);
        });
        PokeBuilder.Both(s =>
        {
            var look = V(0.7f * s, 0.15f, 0.7f);
            b.Eye(head, Out(c, r, default, look), look, 0.017f, paldea ? Rgb(150, 150, 160) : Rgb(80, 56, 44), glare: true);
        });
        return b;
    }

    private static PokeBuilder Tauros() => TaurosBuild("Tauros", 0);

    // ------------------------------------------------------------------ Lapras

    /// <summary>
    /// Lapras and its Gigantamax form: a gentle plesiosaur, blue and cream, that ferries people over the sea on its
    /// knobbled grey shell; Gigantamax, darker, its shell grown into a crested hull ringed by the lines of a stave hung
    /// with ice, the red clouds over it.
    /// </summary>
    private static PokeBuilder LaprasBuild(bool gmax)
    {
        var b = new PokeBuilder(gmax ? "Lapras-Gmax" : "Lapras", gmax ? 1f : 0.95f, BodyPlan.Quadruped, V(0, 0.28f, -0.04f)) { Coat = Scales };
        var blue = gmax ? Rgb(70, 78, 142) : Rgb(92, 170, 230);
        var cream = Rgb(244, 236, 200);
        var shell = gmax ? Rgb(116, 100, 136) : Rgb(170, 170, 178);
        // Four flippers, the front ones broad
        foreach (var (z, front, len, w) in new[] { (0.16f, true, 0.28f, 0.11f), (-0.27f, false, 0.2f, 0.085f) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.15f * s, 0.18f, z), front);
                var from = V(0.08f * s, 0.18f, z);
                Frond(b, leg, from, from + V((len * 0.9f + 0.08f) * s, -0.16f, len * (front ? 0.35f : -0.4f)), w, blue, V(0, 1f, 0), 0.28f);
            });
        var bc = V(0, 0.27f, -0.04f);
        var br = V(0.245f, 0.17f, 0.32f);
        b.Ell(Body, bc, br, blue);
        b.PaintEll(Body, bc - V(0, 0.1f, -0.02f), V(0.22f, 0.09f, 0.3f), cream);
        // The shell on its back, studded with knobs
        var sc = V(0, 0.36f, -0.09f);
        var sr = gmax ? V(0.245f, 0.16f, 0.3f) : V(0.235f, 0.13f, 0.29f);
        b.Ell(Body, sc, sr, shell, mat: Shell);
        foreach (var dir in new[] { V(0, 1f, 0.1f), V(0.5f, 0.8f, 0.4f), V(-0.5f, 0.8f, 0.4f), V(0.6f, 0.7f, -0.3f), V(-0.6f, 0.7f, -0.3f), V(0.2f, 0.75f, -0.7f), V(-0.2f, 0.75f, -0.7f), V(0.9f, 0.3f, 0.1f), V(-0.9f, 0.3f, 0.1f), V(0, 0.6f, 0.85f) })
        {
            var at = Out(sc, sr, default, dir);
            var n = Outward(sc, sr, at);
            b.Limb(Body, at - n * 0.01f, at + n * 0.035f, 0.03f, 0.016f, shell, Shell, 0.01f);
        }
        // A long neck rising from the front, cream down its throat
        int neck = b.Part("neck", Body, V(0, 0.36f, 0.2f), PokeRole.Segment, 0.4f);
        var spine = Smooth(3, V(0, 0.33f, 0.2f), V(0, 0.5f, 0.33f), V(0, 0.67f, 0.34f), V(0, 0.79f, 0.3f));
        b.Tube(neck, spine, 0.075f, 0.055f, blue, blend: 0f);
        for (int i = 0; i < spine.Length - 1; i++)
        {
            var d = Vector3.Normalize(spine[i + 1] - spine[i]);
            var throat = Vector3.Normalize(V(0, 0, 1f) - d * Vector3.Dot(V(0, 0, 1f), d));
            b.PaintEll(neck, spine[i] + throat * 0.06f, V(0.045f, 0.05f, 0.04f), cream, Euler(d));
        }
        // Its head: a short horn, a curled ear like a shell on either side, kind eyes and a gentle smile
        int head = b.Head(V(0, 0.79f, 0.31f), neck);
        var c = V(0, 0.85f, 0.33f);
        var r = V(0.085f, 0.075f, 0.095f);
        b.Ell(head, c, r, blue);
        var snout = c + V(0, -0.025f, 0.07f);
        var snr = V(0.062f, 0.048f, 0.065f);
        b.Ell(head, snout, snr, blue, blend: 0.03f);
        b.Spike(head, c + V(0, 0.06f, 0.02f), c + V(0, 0.13f, -0.01f), 0.022f, blue);
        PokeBuilder.Both(s => Curl(b, head, c + V(0.05f * s, 0.05f, -0.03f), c + V(0.085f * s, 0.085f, -0.045f), V(0, 1f, 0), V(0, 0, -1f), 0.03f, 1.1f, 0.018f, blue));
        b.Mark(head, Out(snout, snr, default, V(0, -0.15f, 1f)), V(0, -0.15f, 1f), 0.035f, 0.01f, Rgb(50, 60, 90), MarkShape.Smile);
        PokeBuilder.Both(s =>
        {
            var look = V(0.6f * s, 0.15f, 0.8f);
            b.Eye(head, Out(c, r, default, look), look, 0.021f, Rgb(80, 56, 44));
        });
        if (gmax)
        {
            // Its shell has grown into a hull with a crest of points down its middle
            for (int i = 0; i < 6; i++)
            {
                float z = 0.12f - i * 0.075f;
                var at = Out(sc, sr, default, V(0, 1f, (z - sc.Z) / sr.Z * 1.6f));
                b.Spike(Body, at - V(0, 0.01f, 0), at + V(0, 0.07f + 0.02f * (i % 2), -0.02f), 0.03f, PixelCanvas.Mix(shell, Rgb(50, 40, 60), 0.3f), 0.6f, Shell);
            }
            b.PaintEll(Body, sc + V(0, -0.1f, 0), V(0.24f, 0.05f, 0.3f), Rgb(194, 196, 220));
            // The lines of a stave ringing it, hung with crystals of ice
            var ice = Rgb(196, 234, 255);
            for (int i = 0; i < 3; i++)
            {
                var ring = sc + V(0, -0.03f + i * 0.045f, 0.02f);
                b.Torus(Body, ring, 0.3f, 0.009f, ice, V(8f, 0, -6f), sx: 0.72f, sz: 1.2f, mat: Glow);
            }
            foreach (var (a, i) in new[] { (0.6f, 0), (1.5f, 1), (2.5f, 2), (3.3f, 0), (4.3f, 1), (5.4f, 2) })
            {
                var at = sc + V(0, -0.03f + i * 0.045f, 0.02f) + V(MathF.Sin(a) * 0.3f * 0.72f, 0, MathF.Cos(a) * 0.3f * 1.2f);
                b.Spike(Body, at, at + V(0, 0.045f, 0), 0.018f, ice, 0.6f, Glow);
                b.Spike(Body, at, at - V(0, 0.03f, 0), 0.018f, ice, 0.6f, Glow);
            }
            MaxClouds(b, Body, sc + V(0, sr.Y + 0.02f, -0.06f), 0.035f, 0.1f, 0.14f, 1.2f, 0.4f);
        }
        return b;
    }

    private static PokeBuilder Lapras() => LaprasBuild(false);

    // ------------------------------------------------------------------ Ditto

    private static PokeBuilder Ditto()
    {
        var b = new PokeBuilder("Ditto", 0.55f, BodyPlan.Floating, V(0, 0.14f, 0)) { Coat = Scales };
        var purple = Rgb(196, 160, 222);
        // A lump of jelly spreading over the ground, lopsided, its bottom flat; two dots of eyes and a thin smile
        var c = V(0, 0.15f, 0);
        var r = V(0.22f, 0.15f, 0.18f);
        b.Ell(Body, c, r, purple);
        b.Ell(Body, V(-0.14f, 0.07f, 0.04f), V(0.13f, 0.08f, 0.13f), purple, blend: 0.06f);
        b.Ell(Body, V(0.15f, 0.06f, 0f), V(0.12f, 0.07f, 0.13f), purple, blend: 0.06f);
        b.Ell(Body, V(0.02f, 0.05f, 0.11f), V(0.16f, 0.05f, 0.09f), purple, blend: 0.06f);
        b.Ell(Body, V(-0.05f, 0.24f, -0.02f), V(0.13f, 0.08f, 0.12f), purple, blend: 0.06f);
        b.CutBox(Body, V(0, -0.3f, 0), V(0.6f, 0.3f, 0.6f), Quaternion.Identity);
        PokeBuilder.Both(s =>
        {
            var look = V(0.25f * s, 0.15f, 1f);
            b.Eye(Body, On(c, r, 0.045f * s, 0.2f), look, 0.011f, pupil: Rgb(40, 34, 50));
        });
        b.Mark(Body, On(c, r, 0, 0.165f), V(0, 0.1f, 1f), 0.05f, 0.012f, Rgb(70, 50, 80), MarkShape.Smile);
        return b;
    }

    // ------------------------------------------------------------------ Omanyte line

    /// <summary>
    /// An ammonite's shell at <paramref name="c"/>, coiled round the x axis: a spiral drawn on each side, painted along
    /// the surface a stroke at a time, and lines over its back where its chambers meet, at each of
    /// <paramref name="chambers"/> degrees round from the top toward the front.
    /// </summary>
    private static void Ammonite(PokeBuilder b, int bone, Vector3 c, Vector3 r, Color shell, Color line, params float[] chambers)
    {
        b.Ell(bone, c, r, shell, mat: Shell);
        Vector3 Side(float s, float y, float z) => c + V(MathF.Sqrt(MathF.Max(0f, 1f - y * y - z * z)) * r.X * s, y * r.Y, z * r.Z);
        PokeBuilder.Both(s =>
        {
            const int Strokes = 44;
            var prev = Side(s, 0f, -0.92f);
            for (int i = 1; i <= Strokes; i++)
            {
                float t = i / (float)Strokes, a = t * MathF.Tau * 2.2f, rad = 0.92f * (1f - t * 0.86f);
                var next = Side(s, MathF.Sin(a) * rad, -MathF.Cos(a) * rad);
                b.PaintEll(bone, (prev + next) / 2f, V(0.007f, Vector3.Distance(prev, next) / 2f + 0.004f, 0.007f), line, Euler(next - prev), 0.005f);
                prev = next;
            }
        });
        foreach (float deg in chambers)
        {
            float a = deg * Degree;
            var at = Out(c, r, default, V(0, MathF.Cos(a), MathF.Sin(a)));
            b.PaintEll(bone, at, V(r.X * 0.9f, 0.006f, r.Y * 0.55f), line, V(deg + 90f, 0, 0), 0.005f);
        }
    }

    /// <summary>A tentacle from <paramref name="root"/> out along <paramref name="way"/> and down, curling up at its end.</summary>
    private static void Tentacle(PokeBuilder b, int bone, Vector3 root, Vector3 way, float length, float thick, Color color)
    {
        var o = Vector3.Normalize(way);
        b.Tube(bone, Smooth(3, root, root + o * length * 0.35f - V(0, length * 0.32f, 0), root + o * length * 0.8f - V(0, length * 0.3f, 0),
            root + o * length + V(0, length * 0.02f, 0), root + o * length * 0.9f + V(0, length * 0.14f, 0)), thick, thick * 0.4f, color, blend: 0f);
    }

    private static PokeBuilder Omanyte()
    {
        var b = new PokeBuilder("Omanyte", 0.55f, BodyPlan.Floating, V(0, 0.24f, 0)) { Coat = Scales }.Hover();
        var blue = Rgb(98, 190, 222);
        // A coiled shell over its back, and its soft blue body peering out of the shell's mouth: two big round eyes
        // rimmed in black and a skirt of short tentacles
        Ammonite(b, Body, V(0, 0.3f, -0.05f), V(0.15f, 0.2f, 0.19f), Rgb(236, 224, 188), Rgb(176, 156, 112), 15f, -15f, -45f, -75f, -105f, -135f);
        int head = b.Head(V(0, 0.2f, 0.06f));
        var c = V(0, 0.19f, 0.1f);
        var r = V(0.13f, 0.11f, 0.09f);
        b.Ell(head, c, r, blue);
        for (int i = 0; i < 9; i++)
        {
            float a = (-80f + i * 20f) * Degree;
            Tentacle(b, head, c + V(MathF.Sin(a) * r.X * 0.85f, -r.Y * 0.5f, MathF.Cos(a) * r.Z * 0.8f), V(MathF.Sin(a), 0, MathF.Cos(a)), 0.13f, 0.024f, blue);
        }
        PokeBuilder.Both(s =>
        {
            var look = V(0.42f * s, 0.2f, 1f);
            var at = Out(c, r, default, look);
            b.Torus(head, at, 0.04f, 0.01f, Rgb(56, 56, 66), Euler(Outward(c, r, at)), mat: Shell);
            b.Eye(head, at, look, 0.028f, sclera: true, pupil: Black);
        });
        return Lift(b);
    }

    private static PokeBuilder Omastar()
    {
        var b = new PokeBuilder("Omastar", 0.75f, BodyPlan.Floating, V(0, 0.3f, 0)) { Coat = Scales }.Hover();
        var blue = Rgb(98, 186, 220);
        var shell = Rgb(232, 220, 182);
        // A bigger shell, spikes standing out of it
        var sc = V(0, 0.4f, -0.06f);
        var sr = V(0.18f, 0.22f, 0.21f);
        Ammonite(b, Body, sc, sr, shell, Rgb(172, 152, 108), 10f, -25f, -60f, -95f, -130f);
        foreach (var dir in new[] { V(0, 1f, 0.35f), V(0.65f, 0.7f, 0.15f), V(-0.65f, 0.7f, 0.15f), V(0.35f, 0.75f, -0.55f), V(-0.35f, 0.75f, -0.55f), V(0, 0.35f, -1f) })
        {
            var at = Out(sc, sr, default, dir);
            var n = Outward(sc, sr, at);
            b.Spike(Body, at - n * 0.01f, at + n * 0.1f, 0.035f, shell, mat: Shell);
        }
        // A broad blue face with yellow eyes and a round beak marked with a cross, long tentacles curling from under it
        int head = b.Head(V(0, 0.26f, 0.06f));
        var c = V(0, 0.25f, 0.12f);
        var r = V(0.17f, 0.12f, 0.1f);
        b.Ell(head, c, r, blue);
        for (int i = 0; i < 8; i++)
        {
            float a = (-100f + i * (200f / 7f)) * Degree;
            Tentacle(b, head, c + V(MathF.Sin(a) * r.X * 0.85f, -r.Y * 0.45f, MathF.Cos(a) * r.Z * 0.75f), V(MathF.Sin(a) * 1.3f, 0, MathF.Cos(a)), 0.24f, 0.028f, blue);
        }
        var d = Vector3.Normalize(V(0, -0.45f, 1f));
        var beak = Out(c, r, default, d);
        b.Ell(head, beak, V(0.045f, 0.045f, 0.03f), shell, mat: Shell, blend: 0.01f);
        var front = beak + d * 0.028f;
        b.Mark(head, front, d, 0.04f, 0.007f, Rgb(40, 30, 34), MarkShape.Bar);
        b.Mark(head, front, d, 0.04f, 0.007f, Rgb(40, 30, 34), MarkShape.Bar, 90f);
        PokeBuilder.Both(s =>
        {
            var look = V(0.45f * s, 0.25f, 1f);
            b.Eye(head, Out(c, r, default, look), look, 0.03f, sclera: true, white: Rgb(246, 226, 120), pupil: Rgb(30, 26, 30));
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Kabuto line

    private static PokeBuilder Kabuto()
    {
        var b = new PokeBuilder("Kabuto", 0.5f, BodyPlan.Quadruped, V(0, 0.12f, 0)) { Coat = Shell };
        var brown = Rgb(178, 114, 62);
        var dark = Rgb(34, 30, 36);
        var foot = Rgb(232, 196, 104);
        // A domed brown shell, hollowed beneath and open at the front, its rim cut level
        var c = V(0, 0.1f, 0);
        var r = V(0.22f, 0.16f, 0.2f);
        b.Ell(Body, c, r, brown);
        b.Cut(Body, c + V(0, -0.06f, 0.07f), V(0.19f, 0.13f, 0.17f));
        b.CutBox(Body, V(0, -0.27f, 0), V(0.6f, 0.3f, 0.6f), Quaternion.Identity);
        PokeBuilder.Both(s => b.Mark(Body, Out(c, r, default, V(0.3f * s, 0.6f, -0.75f)), V(0.3f * s, 0.6f, -0.75f), 0.013f, 0.013f, dark));
        // Its dark body in the shadow under it, two eyes glowing out of the dark, and little legs under its front
        int head = b.Head(V(0, 0.08f, 0.04f));
        var bc = V(0, 0.09f, 0.02f);
        var br = V(0.18f, 0.07f, 0.16f);
        b.Ell(head, bc, br, dark);
        PokeBuilder.Both(s =>
        {
            var look = V(0.35f * s, 0.05f, 1f);
            b.Eye(head, Out(bc, br, default, look), look, 0.026f, pupil: Rgb(250, 136, 156));
        });
        foreach (var (z, front) in new[] { (0.08f, true), (-0.04f, false) })
            PokeBuilder.Both(s =>
            {
                int l = b.Leg(s, V(0.1f * s, 0.06f, z), front);
                b.Limb(l, V(0.09f * s, 0.06f, z), V(0.15f * s, 0.035f, z + 0.05f), 0.022f, 0.017f, foot, Shell);
                b.Spike(l, V(0.15f * s, 0.035f, z + 0.05f), V(0.13f * s, 0.006f, z + 0.11f), 0.017f, foot, mat: Shell, blend: 0.006f);
            });
        return b;
    }

    private static PokeBuilder Kabutops()
    {
        var b = new PokeBuilder("Kabutops", 0.95f, BodyPlan.Biped, V(0, 0.5f, 0)) { Coat = Shell };
        var brown = Rgb(170, 116, 70);
        var cream = Rgb(232, 214, 170);
        var blade = Rgb(228, 228, 232);
        // Thin legs bent like a bird's, two claws each
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.4f, -0.02f));
            var knee = V(0.11f * s, 0.24f, 0.06f);
            var ankle = V(0.1f * s, 0.08f, -0.02f);
            b.Limb(leg, V(0.07f * s, 0.41f, -0.02f), knee, 0.045f, 0.03f, brown);
            b.Ell(leg, knee, V(0.034f, 0.034f, 0.034f), brown);
            b.Limb(leg, knee, ankle, 0.028f, 0.024f, brown);
            foreach (float t in new[] { -1f, 1f })
                b.Spike(leg, ankle, ankle + V(0.03f * t, -0.074f, 0.07f), 0.019f, brown, blend: 0.006f);
        });
        // A slender body plated cream down its front, three spines down its back
        b.Ell(Body, V(0, 0.52f, 0), V(0.1f, 0.15f, 0.085f), brown);
        b.PaintEll(Body, V(0, 0.5f, 0.05f), V(0.075f, 0.13f, 0.05f), cream);
        for (int i = 0; i < 4; i++)
            b.PaintEll(Body, V(0, 0.42f + i * 0.05f, 0.08f), V(0.07f, 0.005f, 0.03f), PixelCanvas.Mix(cream, brown, 0.5f), soft: 0.006f);
        foreach (var (y, len) in new[] { (0.63f, 0.12f), (0.53f, 0.15f), (0.43f, 0.12f) })
            Blade(b, Body, V(0, y, -0.06f), V(0, y + 0.05f, -0.06f - len), 0.036f, brown, V(1f, 0, 0), 0.35f);
        // Arms ending in great scythes, curving out and down from its wrists
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.09f * s, 0.62f, 0));
            var elbow = V(0.2f * s, 0.55f, 0.04f);
            var wrist = V(0.25f * s, 0.67f, 0.08f);
            b.Limb(arm, V(0.08f * s, 0.62f, 0), elbow, 0.03f, 0.026f, brown);
            b.Ell(arm, elbow, V(0.03f, 0.03f, 0.03f), brown);
            b.Limb(arm, elbow, wrist, 0.026f, 0.03f, brown);
            // The scythe: a heel out from its wrist, and a long blade from it down to a point
            var heel = wrist + V(0.1f * s, 0.01f, 0.03f);
            Frond(b, arm, wrist - V(0.01f * s, 0, 0), heel + V(0.02f * s, 0, 0), 0.04f, blade, V(0, 0, 1f), 0.36f, Shell, 0.006f);
            Blade(b, arm, heel - V(0.02f * s, 0.02f, 0), heel + V(0.03f * s, -0.3f, -0.01f), 0.045f, blade, V(0, 0, 1f), 0.34f, Shell);
        });
        // A head under a helmet like a crescent, pointed before and behind; sharp eyes under its brim
        int head = b.Head(V(0, 0.68f, 0.02f));
        var c = V(0, 0.75f, 0.04f);
        var r = V(0.07f, 0.065f, 0.08f);
        b.Limb(head, V(0, 0.63f, 0), c - V(0, 0.03f, 0.01f), 0.04f, 0.035f, brown);
        b.Ell(head, c, r, brown);
        b.Ell(head, c + V(0, 0.055f, 0), V(0.1f, 0.035f, 0.13f), brown, blend: 0.02f);
        b.Spike(head, c + V(0, 0.06f, 0.08f), c + V(0, 0.11f, 0.2f), 0.04f, brown, 0.5f);
        b.Spike(head, c + V(0, 0.06f, -0.08f), c + V(0, 0.13f, -0.21f), 0.04f, brown, 0.5f);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.035f * s, 0.735f), V(0.5f * s, 0, 1f), 0.016f, Black, glare: true));
        return b;
    }

    // ------------------------------------------------------------------ Aerodactyl

    /// <summary>
    /// Aerodactyl and its Mega Evolution: a flying reptile of amber, grey-violet with purple wings of skin, its jaws
    /// wide and full of fangs, a horn swept back from its skull and a tail ending in a spade; the Mega's body broken
    /// out in spikes of dark rock.
    /// </summary>
    private static PokeBuilder AerodactylBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Aerodactyl-Mega" : "Aerodactyl", 1f, BodyPlan.Bird, V(0, 0.48f, 0)) { Coat = Scales }.Hover();
        var grey = mega ? Rgb(150, 142, 172) : Rgb(182, 174, 206);
        var membrane = mega ? Rgb(104, 78, 136) : Rgb(156, 126, 196);
        var rock = Rgb(74, 70, 84);
        // Wide wings of skin, made first because they are carved, two hooked claws at each wrist
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.08f * s, 0.56f, 0));
            var wrist = V(0.32f * s, 0.74f, -0.03f);
            BatWing(b, wing, V(0.08f * s, 0.56f, 0), wrist, new[] { V(0.66f * s, 0.62f, -0.06f), V(0.58f * s, 0.44f, -0.06f), V(0.38f * s, 0.34f, -0.04f) },
                V(0.07f * s, 0.42f, -0.02f), membrane, grey, 0.026f);
            foreach (float t in new[] { -1f, 1f })
                b.Spike(wing, wrist, wrist + V(0.01f * s, 0.05f, 0.03f + 0.02f * t), 0.014f, Claw, mat: Shell, blend: 0.005f);
            if (mega)
                foreach (float t in new[] { 0.3f, 0.6f })
                    b.Spike(wing, Vector3.Lerp(V(0.08f * s, 0.56f, 0), wrist, t), Vector3.Lerp(V(0.08f * s, 0.56f, 0), wrist, t) + V(-0.02f * s, 0.08f, -0.03f), 0.022f, rock, 0.6f, Shell);
        });
        b.Ell(Body, V(0, 0.48f, 0), V(0.1f, 0.13f, 0.11f), grey, V(-20f, 0, 0));
        // Short legs dangling, three claws each
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.4f, -0.02f));
            var ankle = V(0.08f * s, 0.25f, 0.02f);
            b.Limb(leg, V(0.06f * s, 0.41f, -0.02f), ankle, 0.035f, 0.024f, grey);
            Digits(b, leg, ankle, V(0, -0.5f, 1f), V(0.45f, 0, 0.1f), 0.045f, 0.01f, Claw, Shell);
        });
        // A long tail ending in a spade
        int tail = b.Tail(V(0, 0.4f, -0.08f));
        var path = Smooth(3, V(0, 0.42f, -0.07f), V(0, 0.36f, -0.25f), V(0.02f, 0.33f, -0.44f));
        b.Tube(tail, path, 0.04f, 0.014f, grey, blend: 0f);
        var end = path[^1];
        var way = Vector3.Normalize(end - path[^2]);
        var across = Vector3.Normalize(Vector3.Cross(way, V(0, 1f, 0)));
        Blade(b, tail, end - way * 0.01f, end + way * 0.08f, 0.04f, grey, V(0, 1f, 0), 0.3f);
        PokeBuilder.Both(s => Blade(b, tail, end + way * 0.02f, end - way * 0.03f + across * 0.05f * s, 0.022f, grey, V(0, 1f, 0), 0.4f));
        // A big head on a short neck: its jaws open wide, fangs along them, a horn swept back from its skull
        int head = b.Head(V(0, 0.6f, 0.06f));
        var c = V(0, 0.68f, 0.12f);
        var r = V(0.075f, 0.068f, 0.1f);
        b.Ell(head, c, r, grey);
        b.Limb(head, V(0, 0.6f, 0.04f), c, 0.06f, 0.06f, grey);
        var upper = c + V(0, 0.005f, 0.11f);
        b.Ell(head, upper, V(0.055f, 0.032f, 0.1f), grey, V(-8f, 0, 0));
        int jaw = b.Jaw(head, c + V(0, -0.03f, 0.03f));
        var lower = c + V(0, -0.06f, 0.09f);
        b.Ell(jaw, lower, V(0.045f, 0.024f, 0.09f), grey, V(18f, 0, 0));
        b.PaintEll(head, upper + V(0, -0.03f, 0.01f), V(0.045f, 0.01f, 0.08f), Rgb(150, 60, 80), soft: 0.008f);
        b.PaintEll(jaw, lower + V(0, 0.022f, 0.0f), V(0.038f, 0.01f, 0.075f), Rgb(150, 60, 80), soft: 0.008f);
        PokeBuilder.Both(s =>
        {
            foreach (float t in new[] { 0.25f, 0.55f, 0.85f })
            {
                var top = upper + V(0.04f * s * (1f - t * 0.4f), -0.022f, -0.06f + 0.14f * t);
                b.Spike(head, top + V(0, 0.01f, 0), top - V(0, 0.03f, 0), 0.009f, White, mat: Shell, blend: 0.003f);
                var bottom = lower + V(0.033f * s * (1f - t * 0.4f), 0.016f, -0.05f + 0.12f * t);
                b.Spike(jaw, bottom - V(0, 0.01f, 0), bottom + V(0, 0.026f, 0), 0.008f, White, mat: Shell, blend: 0.003f);
            }
        });
        Blade(b, head, c + V(0, 0.04f, -0.02f), c + V(0, 0.07f, -0.18f), 0.045f, mega ? rock : grey, V(1f, 0, 0), 0.4f, mega ? Shell : null);
        PokeBuilder.Both(s =>
        {
            var look = V(0.75f * s, 0.25f, 0.6f);
            b.Eye(head, Out(c, r, default, look), look, 0.017f, mega ? Rgb(120, 200, 110) : Rgb(130, 40, 60), glare: true);
        });
        if (mega)
        {
            // Spikes of dark rock broken out of its back, its neck and its tail
            foreach (var (at, to) in new[] { (V(0, 0.55f, -0.08f), V(0, 0.64f, -0.18f)), (V(0.05f, 0.47f, -0.09f), V(0.1f, 0.52f, -0.2f)), (V(-0.05f, 0.47f, -0.09f), V(-0.1f, 0.52f, -0.2f)), (V(0, 0.4f, 0.08f), V(0, 0.36f, 0.18f)) })
                b.Spike(Body, at, to, 0.03f, rock, 0.6f, Shell);
            foreach (float t in new[] { 0.3f, 0.65f })
            {
                int i = (int)(t * (path.Length - 1));
                b.Spike(tail, path[i], path[i] + V(0, 0.07f, -0.02f), 0.022f, rock, 0.6f, Shell);
            }
            PokeBuilder.Both(s => b.Spike(head, c + V(0.05f * s, 0.03f, -0.02f), c + V(0.1f * s, 0.07f, -0.1f), 0.02f, rock, 0.6f, Shell));
        }
        return Lift(b);
    }

    private static PokeBuilder Aerodactyl() => AerodactylBuild(false);

    // ------------------------------------------------------------------ The legendary birds

    /// <summary>
    /// A bird's wing spread on side <paramref name="s"/>: its arm from <paramref name="shoulder"/> to
    /// <paramref name="wrist"/>, and <paramref name="count"/> long flight feathers fanned from along it, the first
    /// <paramref name="from"/> degrees up from straight out and the last <paramref name="to"/>, swept
    /// <paramref name="back"/>; rounded feathers, or <paramref name="pointed"/> ones like spikes. The feathers from
    /// <paramref name="tipFrom"/> on have their ends in <paramref name="tip"/>.
    /// </summary>
    private static int SpreadWing(PokeBuilder b, float s, Vector3 shoulder, Vector3 wrist, int count, float from, float to, float length, float width,
        Color color, Color tip, float back = 0.3f, bool pointed = false, int tipFrom = 0)
    {
        int wing = b.Wing(s, shoulder);
        b.Limb(wing, shoulder, wrist, width * 0.9f, width * 0.6f, color);
        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 1f : i / (count - 1f);
            float a = (from + (to - from) * t) * Degree;
            var root = Vector3.Lerp(Vector3.Lerp(shoulder, wrist, 0.3f), wrist, t);
            var dir = Vector3.Normalize(V(MathF.Cos(a) * s, MathF.Sin(a), -back));
            float len = length * (0.7f + 0.3f * MathF.Sin(MathF.PI * (0.25f + 0.75f * t)));
            var end = root + dir * len;
            if (pointed) Blade(b, wing, root, end, width, color, V(0, 0, 1f), 0.32f);
            else Frond(b, wing, root, end, width, color, V(0, 0, 1f), 0.3f, Fur, 0.012f);
            if (i >= tipFrom && tip != color) b.PaintEll(wing, root + (end - root) * 0.86f, V(width * 1.3f, len * 0.2f, width * 1.3f), tip, Euler(dir));
        }
        return wing;
    }

    /// <summary>A long flat streamer along <paramref name="points"/>, flat across <paramref name="facing"/>, narrowing toward its end, never thinner than <paramref name="thick"/>.</summary>
    private static void Ribbon(PokeBuilder b, int bone, Vector3[] points, float width, Color color, Vector3 facing, float thick)
    {
        // Each piece reaches well past both its ends, so the edges dip little where the pieces meet
        for (int i = 0; i < points.Length - 1; i++)
        {
            float w = width * (1f - 0.55f * i / (points.Length - 1));
            var step = points[i + 1] - points[i];
            Frond(b, bone, points[i] - step * (i == 0 ? 0f : 0.7f), points[i + 1] + step * 0.7f, w, color, facing, MathF.Max(0.2f, thick / w), Fur, 0.01f);
        }
    }

    /// <summary>
    /// A flying bird's legs hanging from hips <paramref name="x"/> apart at <paramref name="hip"/>,
    /// <paramref name="length"/> long, three toes forward and one back, curled under and tipped with pale talons.
    /// </summary>
    private static void DanglingLegs(PokeBuilder b, float x, float hip, float z, float length, float thick, Color color)
    {
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(x * s, hip, z));
            var ankle = V(x * 1.1f * s, hip - length, z + 0.02f);
            b.Limb(leg, V(x * s, hip, z), ankle, thick, thick * 0.8f, color, Scales, 0.008f);
            foreach (var (dx, dz) in new[] { (-0.5f, 1f), (0f, 1.1f), (0.5f, 1f), (0f, -0.7f) })
            {
                var toe = ankle + V(dx * thick * 2.2f * s, -thick * 1.6f, dz * thick * 2.6f);
                b.Limb(leg, ankle, toe, thick * 0.6f, thick * 0.45f, color, Scales, 0.006f);
                b.Spike(leg, toe, toe + V(0, -thick * 1.2f, dz * thick * 0.8f), thick * 0.4f, Claw, mat: Shell, blend: 0.003f);
            }
        });
    }

    /// <summary>
    /// Articuno and its Galarian form: a slender blue bird with its wings raised, a pale breast, a crest of three
    /// plumes and a long tail of two streamers sweeping down in a curve; the Galarian purple, its neck and legs black,
    /// a ruff of white feathers at its breast, a hood of dark plumes and its eyes glowing.
    /// </summary>
    private static PokeBuilder ArticunoBuild(bool galar)
    {
        var b = new PokeBuilder(galar ? "Articuno-Galar" : "Articuno", 1f, BodyPlan.Bird, V(0, 0.55f, 0)) { Coat = Fur }.Hover();
        var blue = galar ? Rgb(172, 142, 208) : Rgb(116, 178, 234);
        var breast = galar ? Rgb(244, 240, 250) : Rgb(206, 232, 250);
        var dark = galar ? Rgb(112, 84, 170) : Rgb(54, 114, 198);
        var black = Rgb(44, 38, 54);
        DanglingLegs(b, 0.045f, 0.44f, 0f, 0.22f, 0.016f, galar ? black : Rgb(150, 150, 164));
        b.Ell(Body, V(0, 0.55f, 0), V(0.11f, 0.16f, 0.12f), blue, V(-12f, 0, 0));
        b.PaintEll(Body, V(0, 0.53f, 0.07f), V(0.09f, 0.14f, 0.08f), breast);
        if (galar)
            for (int i = 0; i < 7; i++)
            {
                // A ruff of white feathers hanging over its breast
                float a = (-48f + i * 16f) * Degree;
                var root = V(MathF.Sin(a) * 0.1f, 0.6f, MathF.Cos(a) * 0.09f + 0.01f);
                Blade(b, Body, root, root + V(MathF.Sin(a) * 0.03f, -0.11f, MathF.Cos(a) * 0.04f), 0.026f, breast, V(MathF.Sin(a), 0, MathF.Cos(a)), 0.34f);
            }
        // Wings raised and spread; the Galarian's lower, drooping like a cloak
        PokeBuilder.Both(s => SpreadWing(b, s, V(0.08f * s, 0.64f, -0.02f), V(0.24f * s, galar ? 0.74f : 0.8f, -0.06f), 6, galar ? 80f : 112f, galar ? -20f : 28f,
            0.38f, 0.04f, blue, galar ? breast : dark, 0.25f, tipFrom: 2));
        // A long tail of streamers sweeping down and round in a curve
        int tail = b.Tail(V(0, 0.45f, -0.1f));
        foreach (float sx in new[] { -1f, 1f })
            Ribbon(b, tail, Smooth(3, V(0.02f * sx, 0.46f, -0.1f), V(0.07f * sx, 0.3f, -0.28f), V(0.14f * sx, 0.12f, -0.38f), V(0.25f * sx, 0.06f, -0.3f), V(0.33f * sx, 0.12f, -0.14f)),
                0.055f, dark, V(sx, 0, 0.6f), 0.014f);
        // A small head on a long neck, a short beak and a crest swept back: three plumes, or the Galarian's dark hood
        int head = b.Head(V(0, 0.7f, 0.04f));
        var c = V(0, 0.79f, 0.07f);
        var r = V(0.065f, 0.06f, 0.07f);
        b.Limb(head, V(0, 0.66f, 0.03f), c, 0.05f, 0.045f, galar ? black : blue);
        b.Ell(head, c, r, galar ? black : blue);
        b.Spike(head, c + V(0, -0.01f, 0.06f), c + V(0, -0.03f, 0.12f), 0.02f, galar ? black : Rgb(120, 116, 132), 0.8f, Shell);
        if (galar)
        {
            b.Ell(head, c + V(0, 0.05f, -0.02f), V(0.075f, 0.045f, 0.085f), dark, V(-20f, 0, 0), blend: 0.02f);
            b.Spike(head, c + V(0, 0.07f, -0.04f), c + V(0, 0.09f, -0.16f), 0.04f, dark, 0.6f);
        }
        else
            foreach (var (x, h) in new[] { (0f, 0.1f), (-0.028f, 0.07f), (0.028f, 0.07f) })
                Blade(b, head, c + V(x, 0.04f, 0.03f), c + V(x * 1.6f, 0.04f + h, -0.08f), 0.022f, dark, V(1f, 0, 0), 0.4f);
        PokeBuilder.Both(s =>
        {
            var look = V(0.6f * s, 0.05f, 0.8f);
            b.Eye(head, Out(c, r, default, look), look, 0.017f, galar ? Rgb(150, 230, 255) : Rgb(200, 40, 60), glare: true);
        });
        return Lift(b);
    }

    private static PokeBuilder Articuno() => ArticunoBuild(false);

    private static PokeBuilder Zapdos()
    {
        var b = new PokeBuilder("Zapdos", 1f, BodyPlan.Bird, V(0, 0.56f, 0)) { Coat = Fur }.Hover();
        var yellow = Rgb(246, 214, 70);
        var black = Rgb(40, 36, 44);
        var orange = Rgb(214, 136, 70);
        DanglingLegs(b, 0.06f, 0.44f, 0.02f, 0.2f, 0.02f, orange);
        // A body bristling with spiky feathers
        var c = V(0, 0.56f, 0);
        var r = V(0.12f, 0.14f, 0.12f);
        b.Ell(Body, c, r, yellow);
        foreach (var dir in new[] { V(0.7f, -0.6f, 0.4f), V(-0.7f, -0.6f, 0.4f), V(0.3f, -0.9f, 0.5f), V(-0.3f, -0.9f, 0.5f), V(0, -0.8f, 0.8f), V(0.8f, 0.2f, -0.5f), V(-0.8f, 0.2f, -0.5f), V(0, 0.3f, -1f) })
        {
            var n = Vector3.Normalize(dir);
            var at = Out(c, r, default, n);
            Blade(b, Body, at - n * 0.03f, at + n * 0.12f + V(0, -0.03f, 0), 0.034f, yellow, Vector3.Cross(n, V(0, 1f, 0)) + V(0, 0, 0.01f), 0.35f);
        }
        // Wings spread wide, every feather a spike, the outer ones tipped black
        PokeBuilder.Both(s => SpreadWing(b, s, V(0.08f * s, 0.62f, -0.02f), V(0.26f * s, 0.72f, -0.04f), 7, 100f, -15f, 0.42f, 0.05f, yellow, black, 0.2f, pointed: true, tipFrom: 4));
        // A fan of spikes for a tail
        int tail = b.Tail(V(0, 0.48f, -0.1f));
        foreach (float a in new[] { -40f, -20f, 0f, 20f, 40f })
            Blade(b, tail, V(0, 0.48f, -0.1f), V(MathF.Sin(a * Degree) * 0.2f, 0.38f - MathF.Abs(a) * 0.002f, -0.32f), 0.04f, yellow, V(0, 1f, 0), 0.3f);
        // A head with a long sharp beak and a crest of spikes
        int head = b.Head(V(0, 0.66f, 0.05f));
        var hc = V(0, 0.75f, 0.08f);
        var hr = V(0.065f, 0.06f, 0.07f);
        b.Ell(head, hc, hr, yellow);
        b.Spike(head, hc + V(0, -0.01f, 0.05f), hc + V(0, -0.09f, 0.22f), 0.03f, orange, 0.8f, Shell);
        foreach (var (x, h, z) in new[] { (0f, 0.13f, -0.06f), (-0.04f, 0.1f, -0.08f), (0.04f, 0.1f, -0.08f), (-0.06f, 0.06f, -0.1f), (0.06f, 0.06f, -0.1f) })
            Blade(b, head, hc + V(x * 0.5f, 0.04f, 0.02f), hc + V(x * 2f, 0.04f + h, z), 0.024f, yellow, V(1f, 0, 0.3f), 0.4f);
        PokeBuilder.Both(s =>
        {
            var look = V(0.6f * s, 0.1f, 0.8f);
            b.Eye(head, Out(hc, hr, default, look), look, 0.016f, Black, glare: true);
        });
        return Lift(b);
    }

    private static PokeBuilder Moltres() => MoltresBuild(false);

    /// <summary>
    /// Moltres and its Galarian form: a long slender bird whose wings and tail are sheets of fire and whose crest
    /// is a flame; the Galarian black, burning with dark flames tipped crimson, its legs and beak red.
    /// </summary>
    private static PokeBuilder MoltresBuild(bool galar)
    {
        var b = new PokeBuilder(galar ? "Moltres-Galar" : "Moltres", 1f, BodyPlan.Bird, V(0, 0.55f, 0)) { Coat = Fur }.Hover();
        var body = galar ? Rgb(48, 42, 56) : Rgb(244, 198, 84);
        var rim = galar ? Rgb(222, 40, 96) : Rgb(244, 112, 50);
        var heart = galar ? Rgb(60, 36, 64) : Rgb(255, 214, 90);
        var red = Rgb(212, 40, 70);
        DanglingLegs(b, 0.045f, 0.46f, 0f, 0.15f, 0.016f, galar ? red : Rgb(150, 140, 140));
        b.Ell(Body, V(0, 0.56f, 0), V(0.09f, 0.11f, 0.15f), body, V(-25f, 0, 0));
        // Wings spread wide: an arm of feathers, a row of flames rising off it
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.07f * s, 0.62f, 0);
            int wing = b.Wing(s, shoulder);
            var wrist = V(0.3f * s, 0.72f, -0.06f);
            var end = V(0.56f * s, 0.84f, -0.1f);
            b.Limb(wing, shoulder, wrist, 0.045f, 0.035f, body);
            b.Limb(wing, wrist, end, 0.035f, 0.02f, body);
            for (int i = 0; i < 5; i++)
            {
                float t = (i + 0.5f) / 5f;
                var root = t < 0.5f ? Vector3.Lerp(shoulder, wrist, t * 2f) : Vector3.Lerp(wrist, end, t * 2f - 1f);
                FlameTongue(b, wing, root, root + V(0.05f * s, 0.2f + 0.07f * MathF.Sin(t * MathF.PI), -0.14f), 0.06f, rim, heart);
                FlameTongue(b, wing, root, root + V(0.04f * s, -0.02f, -0.2f), 0.045f, rim, heart);
            }
        });
        // A tail of flame
        int tail = b.Tail(V(0, 0.5f, -0.14f));
        foreach (var (dx, dy) in new[] { (-0.07f, 0.04f), (0f, 0.09f), (0.07f, 0.04f) })
            FlameTongue(b, tail, V(0, 0.5f, -0.14f), V(dx, 0.5f + dy, -0.44f), 0.05f, rim, heart);
        // A long neck, a small head under a crest of flame, a long thin beak
        int head = b.Head(V(0, 0.64f, 0.1f));
        var c = V(0, 0.73f, 0.17f);
        var r = V(0.055f, 0.05f, 0.065f);
        b.Limb(head, V(0, 0.6f, 0.1f), c, 0.04f, 0.035f, body);
        b.Ell(head, c, r, body);
        b.Spike(head, c + V(0, -0.005f, 0.05f), c + V(0, -0.04f, 0.2f), 0.02f, galar ? red : Rgb(170, 140, 116), 0.8f, Shell);
        foreach (var (dz, dy) in new[] { (0.02f, 0.12f), (-0.03f, 0.1f), (-0.07f, 0.07f) })
            FlameTongue(b, head, c + V(0, 0.03f, dz), c + V(0, 0.03f + dy, dz - 0.08f), 0.03f, rim, heart);
        PokeBuilder.Both(s =>
        {
            var look = V(0.6f * s, 0.1f, 0.8f);
            b.Eye(head, Out(c, r, default, look), look, 0.015f, galar ? red : Black, glare: true);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Dratini line

    /// <summary>
    /// White down the front of a dragon's rising neck, where the body <see cref="Coils"/> laid along
    /// <paramref name="points"/> (a bone every two) climbs above <paramref name="from"/>: each stroke on the bone of
    /// its own stretch, since paint colours only its bone.
    /// </summary>
    private static void Throat(PokeBuilder b, Vector3[] points, float from, float r, Color color)
    {
        for (int i = 0; i < points.Length - 1; i++)
        {
            if (points[i].Y < from) continue;
            int bone = i < 2 ? Body : b.Model.Skeleton.Find("seg" + i / 2);
            var d = Vector3.Normalize(points[i + 1] - points[i]);
            var front = Vector3.Normalize(V(0, 0, 1f) - d * Vector3.Dot(V(0, 0, 1f), d));
            var mid = (points[i] + points[i + 1]) / 2f;
            b.PaintEll(bone, mid + front * r * 0.85f, V(r * 0.7f, Vector3.Distance(points[i], points[i + 1]) * 0.5f + r * 0.6f, r * 0.6f), color, Euler(d));
        }
    }

    /// <summary>A white fin on the side of a dragon's head: three points spread like a little wing.</summary>
    private static void HeadFin(PokeBuilder b, int bone, Vector3 root, float s, float size, Color color)
    {
        foreach (var (dy, dz, w) in new[] { (0.55f, -0.25f, 0.34f), (0.1f, -0.6f, 0.3f), (-0.3f, -0.35f, 0.26f) })
            Blade(b, bone, root, root + V(0.75f * s, dy, dz) * size, w * size, color, V(s, 0.1f, 0.3f), 0.32f);
    }

    private static PokeBuilder Dratini()
    {
        var blue = Rgb(124, 164, 234);
        var white = Rgb(242, 244, 252);
        // A long body lying in an S on the ground, the neck rising at its front
        var points = Smooth(3, V(-0.3f, 0.02f, -0.3f), V(-0.12f, 0.04f, -0.34f), V(0.1f, 0.05f, -0.22f), V(0.14f, 0.055f, 0.0f), V(0.04f, 0.06f, 0.12f),
            V(0.0f, 0.18f, 0.12f), V(0, 0.32f, 0.08f), V(0, 0.42f, 0.06f));
        var b = new PokeBuilder("Dratini", 0.62f, BodyPlan.Serpent, points[0]) { Coat = Scales };
        int neck = Coils(b, points, t => 0.014f + 0.042f * MathF.Min(1f, t * 2.5f), blue);
        Throat(b, points, 0.1f, 0.055f, white);
        // A round white snout, a white bump on its brow, white fins for ears and big eyes
        int head = b.Head(points[^1], neck);
        var c = V(0, 0.48f, 0.08f);
        var r = V(0.075f, 0.07f, 0.08f);
        b.Ell(head, c, r, blue);
        b.Ell(head, c + V(0, -0.02f, 0.07f), V(0.055f, 0.046f, 0.05f), white, blend: 0.025f);
        b.Ell(head, c + V(0, 0.066f, 0.03f), V(0.015f, 0.013f, 0.015f), white, blend: 0.008f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.06f * s, 0.02f, -0.02f));
            HeadFin(b, ear, c + V(0.06f * s, 0.015f, -0.02f), s, 0.1f, white);
        });
        PokeBuilder.Both(s =>
        {
            var look = V(0.45f * s, 0.12f, 1f);
            b.Eye(head, Out(c, r, default, look), look, 0.028f, Rgb(110, 60, 126));
        });
        return b;
    }

    private static PokeBuilder Dragonair()
    {
        var blue = Rgb(86, 148, 226);
        var white = Rgb(242, 244, 252);
        var orb = Rgb(72, 120, 232);
        // A long, graceful body: the tail laid in a curve on the ground, the neck rising high in an S
        var points = Smooth(3, V(-0.34f, 0.07f, -0.44f), V(-0.14f, 0.04f, -0.4f), V(0.14f, 0.05f, -0.3f), V(0.22f, 0.06f, -0.06f), V(0.08f, 0.07f, 0.1f),
            V(-0.06f, 0.18f, 0.12f), V(-0.04f, 0.4f, 0.08f), V(0.0f, 0.6f, 0.06f), V(0.02f, 0.72f, 0.08f));
        var b = new PokeBuilder("Dragonair", 0.9f, BodyPlan.Serpent, points[0]) { Coat = Scales };
        int neck = Coils(b, points, t => 0.016f + 0.042f * MathF.Min(1f, t * 2.5f), blue);
        Throat(b, points, 0.12f, 0.058f, white);
        // A crystal at its throat and three at the end of its tail
        b.Ell(neck, V(0.0f, 0.6f, 0.115f), V(0.026f, 0.026f, 0.026f), orb, mat: Glow, blend: 0.008f);
        int tail = b.Tail(points[0]);
        for (int i = 0; i < 3; i++)
            b.Ell(tail, Vector3.Lerp(points[0], points[1], -0.2f + i * 0.45f) + V(0, 0.03f, 0), V(0.024f, 0.024f, 0.024f), orb, mat: Glow, blend: 0.008f);
        // A long head: a white horn, white wings for ears and dark eyes
        int head = b.Head(points[^1], neck);
        var c = V(0.02f, 0.78f, 0.1f);
        var r = V(0.074f, 0.066f, 0.095f);
        b.Ell(head, c, r, blue);
        b.Ell(head, c + V(0, -0.015f, 0.085f), V(0.05f, 0.042f, 0.055f), blue, blend: 0.025f);
        b.Spike(head, c + V(0, 0.05f, 0.03f), c + V(0, 0.12f, 0.02f), 0.018f, white, mat: Shell);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.05f * s, 0.02f, -0.02f));
            HeadFin(b, ear, c + V(0.05f * s, 0.02f, -0.02f), s, 0.12f, white);
        });
        PokeBuilder.Both(s =>
        {
            var look = V(0.55f * s, 0.12f, 1f);
            b.Eye(head, Out(c, r, default, look), look, 0.022f, Rgb(90, 50, 110));
        });
        return b;
    }

    /// <summary>
    /// Dragonite and its Mega Evolution: a big friendly dragon, orange with a cream belly banded across, little
    /// sea-green wings, two feelers on its brow and a thick tail; the Mega's back grown great white feathered wings,
    /// a pearl at the end of its tail.
    /// </summary>
    private static PokeBuilder DragoniteBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Dragonite-Mega" : "Dragonite", 1f, BodyPlan.Biped, V(0, 0.45f, 0)) { Coat = Scales };
        var orange = Rgb(242, 168, 82);
        var cream = Rgb(246, 226, 170);
        var band = PixelCanvas.Mix(cream, Rgb(190, 130, 60), 0.35f);
        var green = Rgb(64, 160, 140);
        // Little wings, made first because they are carved
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.09f * s, 0.62f, -0.13f));
            BatWing(b, wing, V(0.09f * s, 0.62f, -0.13f), V(0.24f * s, 0.78f, -0.17f), new[] { V(0.38f * s, 0.7f, -0.2f), V(0.32f * s, 0.55f, -0.2f) },
                V(0.08f * s, 0.52f, -0.15f), green, orange, 0.02f);
            if (mega)
            {
                // Great white wings fanned up from its back, feather over feather
                var root = V(0.06f * s, 0.66f, -0.16f);
                for (int i = 0; i < 6; i++)
                {
                    float a = (95f - i * 15f) * Degree;
                    float len = 0.34f + 0.1f * MathF.Sin(MathF.PI * i / 5f);
                    var tip = root + V(MathF.Cos(a) * s, MathF.Sin(a), -0.45f) * len;
                    Frond(b, wing, root + V(0.03f * s, 0, -0.01f), tip, 0.06f, Rgb(246, 238, 214), V(0, 0.15f, 1f), 0.24f, Fur, 0.012f);
                }
            }
        });
        // Stubby legs, three claws each
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.12f * s, 0.24f, 0));
            b.Limb(leg, V(0.12f * s, 0.26f, 0), V(0.14f * s, 0.08f, 0.04f), 0.09f, 0.07f, orange);
            b.Ell(leg, V(0.14f * s, 0.04f, 0.07f), V(0.075f, 0.04f, 0.1f), orange);
            for (int i = -1; i <= 1; i++)
                b.Spike(leg, V(0.14f * s + 0.035f * i, 0.03f, 0.15f), V(0.14f * s + 0.042f * i, 0.018f, 0.185f), 0.014f, Claw, mat: Shell, blend: 0.004f);
        });
        // A big round body, its cream belly banded across
        var bc = V(0, 0.45f, 0);
        var br = V(0.22f, 0.27f, 0.2f);
        b.Ell(Body, bc, br, orange);
        b.PaintEll(Body, bc + V(0, -0.03f, 0.1f), V(0.18f, 0.24f, 0.13f), cream);
        for (int i = 0; i < 7; i++)
            b.PaintEll(Body, V(0, 0.27f + i * 0.055f, 0.19f), V(0.16f, 0.005f, 0.06f), band, soft: 0.006f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.17f * s, 0.6f, 0.06f));
            var hand = V(0.27f * s, 0.52f, 0.16f);
            b.Limb(arm, V(0.16f * s, 0.6f, 0.06f), hand, 0.05f, 0.04f, orange);
            b.Ell(arm, hand, V(0.042f, 0.04f, 0.04f), orange);
            Digits(b, arm, hand + V(0, 0.01f, 0.025f), V(0.1f * s, 0.6f, 1f), V(0.4f, 0, 0.1f), 0.04f, 0.011f, Claw, Shell);
        });
        // A thick tail, curling round at its end; the Mega's carrying a pearl
        int tail = b.Tail(V(0, 0.3f, -0.16f));
        var path = Smooth(3, V(0, 0.32f, -0.15f), V(0.04f, 0.16f, -0.3f), V(0.1f, 0.07f, -0.46f), V(0.16f, 0.06f, -0.58f));
        b.Tube(tail, path, 0.1f, 0.032f, orange, blend: 0f);
        if (mega) b.Ell(tail, path[^1] + V(0.04f, 0.0f, -0.03f), V(0.055f, 0.055f, 0.055f), Rgb(96, 150, 146), mat: Shell, blend: 0.01f);
        // A friendly face: a short horn, two long feelers curling from its brow
        int head = b.Head(V(0, 0.72f, 0.03f));
        var c = V(0, 0.84f, 0.06f);
        var r = V(0.1f, 0.09f, 0.1f);
        b.Limb(head, V(0, 0.66f, 0.02f), c - V(0, 0.03f, 0.01f), 0.09f, 0.075f, orange);
        b.Ell(head, c, r, orange);
        var snout = c + V(0, -0.025f, 0.08f);
        var snr = V(0.07f, 0.055f, 0.07f);
        b.Ell(head, snout, snr, orange, blend: 0.03f);
        b.Spike(head, c + V(0, 0.07f, 0.04f), c + V(0, 0.12f, 0.03f), 0.018f, orange);
        PokeBuilder.Both(s => b.Tube(head, Smooth(3, c + V(0.035f * s, 0.07f, 0.03f), c + V(0.06f * s, 0.18f, 0.06f), c + V(0.12f * s, 0.24f, 0.05f), c + V(0.17f * s, 0.22f, 0.02f)),
            0.014f, 0.012f, orange, blend: 0f));
        PokeBuilder.Both(s =>
        {
            var n = V(0.3f * s, 0.3f, 1f);
            b.Mark(head, Out(snout, snr, default, n), n, 0.009f, 0.007f, Rgb(120, 70, 40));
        });
        b.Mark(head, Out(snout, snr, default, V(0, -0.3f, 1f)), V(0, -0.3f, 1f), 0.035f, 0.01f, Rgb(120, 70, 40), MarkShape.Smile);
        PokeBuilder.Both(s =>
        {
            var look = V(0.5f * s, 0.12f, 1f);
            b.Eye(head, Out(c, r, default, look), look, 0.022f, Rgb(60, 50, 50));
        });
        return b;
    }

    private static PokeBuilder Dragonite() => DragoniteBuild(false);

    // ------------------------------------------------------------------ Mewtwo and Mew

    /// <summary>
    /// Mewtwo and its Mega Evolutions (0, then 1 for X and 2 for Y): tall and pale, a purple belly and a long purple
    /// tail, a tube running from the back of its skull down its neck, three fingers tipped with balls; Mega X heavier,
    /// the tube grown into a purple band over its shoulder, its crest swept back into a horn; Mega Y slighter, its
    /// ears long horns, a great loop running back from its head and a long tail tipped with a bulb.
    /// </summary>
    private static PokeBuilder MewtwoBuild(int form)
    {
        string name = form switch { 1 => "Mewtwo-Mega-X", 2 => "Mewtwo-Mega-Y", _ => "Mewtwo" };
        var b = new PokeBuilder(name, 1f, BodyPlan.Biped, V(0, 0.62f, 0)) { Coat = Fur };
        var pale = Rgb(230, 226, 238);
        var purple = form == 1 ? Rgb(176, 112, 150) : Rgb(170, 128, 190);
        float bulk = form switch { 1 => 1.2f, 2 => 0.85f, _ => 1f };
        // Long legs, the thighs strong, three round toes on each foot
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.5f, 0));
            var knee = V(0.12f * s, 0.29f, 0.04f);
            var ankle = V(0.12f * s, 0.07f, -0.02f);
            b.Ell(leg, V(0.1f * s, 0.42f, 0.01f), V(0.07f, 0.11f, 0.07f) * bulk, pale);
            b.Limb(leg, knee, ankle, 0.042f * bulk, 0.03f, pale);
            foreach (float t in new[] { -1f, 0f, 1f })
            {
                var toe = ankle + V(0.03f * t, -0.045f, 0.075f);
                b.Limb(leg, ankle, toe, 0.022f, 0.02f, pale);
                b.Ell(leg, toe + V(0, 0.002f, 0.008f), V(0.022f, 0.022f, 0.024f), form == 2 ? purple : pale);
            }
        });
        // A narrow waist under a broad chest, its belly purple
        b.Ell(Body, V(0, 0.55f, 0.01f), V(0.1f, 0.09f, 0.09f) * bulk, form == 2 ? pale : purple);
        b.Ell(Body, V(0, 0.68f, 0), V(0.085f, 0.1f, 0.07f) * bulk, pale);
        b.Ell(Body, V(0, 0.77f, 0.01f), V(0.12f, 0.075f, 0.085f) * bulk, pale);
        // Its tail, thick at the root, sweeping back and round; Mega Y's longer, tipped with a bulb
        int tail = b.Tail(V(0, 0.5f, -0.07f));
        var tailPath = form == 2
            ? Smooth(3, V(0, 0.52f, -0.06f), V(0.08f, 0.36f, -0.24f), V(0.22f, 0.16f, -0.32f), V(0.36f, 0.06f, -0.22f), V(0.4f, 0.05f, -0.06f))
            : Smooth(3, V(0, 0.52f, -0.06f), V(0.05f, 0.38f, -0.25f), V(0.18f, 0.3f, -0.42f), V(0.32f, 0.36f, -0.46f), V(0.4f, 0.48f, -0.4f));
        b.Tube(tail, tailPath, 0.07f * bulk, form == 2 ? 0.022f : 0.035f, purple, blend: 0f);
        if (form == 2) b.Ell(tail, tailPath[^1] + V(0.02f, 0.01f, 0.03f), V(0.045f, 0.04f, 0.055f), purple, blend: 0.01f);
        // Arms held ready, three fingers with round tips
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.12f * s, 0.8f, 0));
            var elbow = V(0.22f * s, 0.68f, 0.04f);
            var hand = V(0.27f * s, 0.6f, 0.13f);
            b.Limb(arm, V(0.11f * s, 0.8f, 0), elbow, 0.04f * bulk, 0.032f * bulk, pale);
            b.Limb(arm, elbow, hand, 0.032f * bulk, 0.028f * bulk, pale);
            b.Ell(arm, hand, V(0.03f, 0.028f, 0.03f) * bulk, pale);
            foreach (float t in new[] { -1f, 0f, 1f })
            {
                var tip = hand + V((0.025f * t + 0.02f) * s, -0.02f + 0.01f * t, 0.05f);
                b.Limb(arm, hand, tip, 0.012f, 0.011f, pale);
                b.Ell(arm, tip, V(0.019f, 0.019f, 0.019f), form == 2 ? purple : pale);
            }
        });
        // Its head on a slender neck: two short horns, a tube from the back of its skull, purple eyes
        int head = b.Head(V(0, 0.84f, 0));
        var c = V(0, 0.97f, 0.04f);
        var r = V(0.07f, 0.065f, 0.075f);
        b.Limb(head, V(0, 0.82f, 0), c - V(0, 0.03f, 0.02f), 0.035f, 0.033f, pale);
        b.Ell(head, c, r, pale);
        b.Ell(head, c + V(0, -0.03f, 0.05f), V(0.045f, 0.035f, 0.045f), pale, blend: 0.02f);
        if (form == 2)
            PokeBuilder.Both(s => b.Tube(head, Smooth(3, c + V(0.04f * s, 0.04f, -0.01f), c + V(0.1f * s, 0.1f, -0.04f), c + V(0.12f * s, 0.2f, -0.08f), c + V(0.1f * s, 0.26f, -0.14f)), 0.028f, 0.012f, pale, blend: 0f));
        else
            PokeBuilder.Both(s => b.Spike(head, c + V(0.045f * s, 0.035f, -0.01f), c + V(0.07f * s, 0.11f, -0.05f), 0.03f, pale, 0.6f));
        switch (form)
        {
            case 0:
                b.Tube(head, Smooth(3, c + V(0, 0.01f, -0.06f), c + V(0, -0.04f, -0.12f), V(0, 0.85f, -0.1f), V(0, 0.8f, -0.06f)), 0.025f, 0.024f, pale, blend: 0f);
                break;
            case 1:
                // A horn swept back from its crest, and the tube grown into a purple band over its shoulder
                b.Spike(head, c + V(0, 0.04f, -0.03f), c + V(0, 0.09f, -0.2f), 0.035f, pale);
                b.Tube(head, Smooth(3, c + V(0, 0.0f, -0.06f), V(0.05f, 0.9f, -0.1f), V(0.12f, 0.82f, -0.06f), V(0.15f, 0.77f, 0.04f), V(0.12f, 0.74f, 0.1f)), 0.036f, 0.03f, purple, blend: 0f);
                break;
            default:
                // A great loop running back from its head, down to its back
                b.Tube(head, Smooth(3, c + V(0, 0.02f, -0.06f), c + V(0, 0.1f, -0.16f), c + V(0, 0.04f, -0.28f), V(0, 0.82f, -0.26f), V(0, 0.72f, -0.1f)), 0.03f, 0.028f, pale, blend: 0f);
                break;
        }
        PokeBuilder.Both(s =>
        {
            var look = V(0.5f * s, 0.05f, 1f);
            b.Eye(head, Out(c, r, default, look), look, 0.018f, form switch { 1 => Rgb(70, 140, 230), 2 => Rgb(220, 60, 50), _ => Rgb(130, 70, 150) }, glare: true);
        });
        return b;
    }

    private static PokeBuilder Mewtwo() => MewtwoBuild(0);

    private static PokeBuilder Mew()
    {
        var b = new PokeBuilder("Mew", 0.62f, BodyPlan.Biped, V(0, 0.33f, 0)) { Coat = Fur }.Hover();
        var pink = Rgb(246, 190, 212);
        // Long feet and legs drawn up as it floats
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.27f, 0));
            var knee = V(0.07f * s, 0.15f, 0.04f);
            b.Limb(leg, V(0.05f * s, 0.28f, 0), knee, 0.035f, 0.03f, pink);
            b.Limb(leg, knee, V(0.08f * s, 0.06f, 0.02f), 0.03f, 0.026f, pink);
            b.Ell(leg, V(0.085f * s, 0.035f, 0.05f), V(0.03f, 0.028f, 0.06f), pink);
        });
        b.Ell(Body, V(0, 0.33f, 0), V(0.075f, 0.09f, 0.07f), pink);
        // Small arms held out
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.06f * s, 0.38f, 0.02f));
            b.Limb(arm, V(0.06f * s, 0.38f, 0.02f), V(0.15f * s, 0.36f, 0.06f), 0.022f, 0.02f, pink);
            b.Ell(arm, V(0.16f * s, 0.36f, 0.065f), V(0.022f, 0.02f, 0.02f), pink);
        });
        // A long thin tail looping up behind it, an oval at its end
        int tail = b.Tail(V(0, 0.27f, -0.06f));
        var path = Smooth(3, V(0, 0.27f, -0.06f), V(0.08f, 0.2f, -0.2f), V(0.2f, 0.35f, -0.3f), V(0.2f, 0.6f, -0.2f), V(0.08f, 0.72f, -0.08f), V(-0.08f, 0.7f, -0.04f));
        b.Tube(tail, path, 0.017f, 0.015f, pink, blend: 0f);
        b.Ell(tail, path[^1] + V(-0.03f, 0, 0), V(0.04f, 0.022f, 0.022f), pink, blend: 0.01f);
        // A big round head, small ears and great blue eyes
        int head = b.Head(V(0, 0.42f, 0.01f));
        var c = V(0, 0.52f, 0.02f);
        var r = V(0.12f, 0.11f, 0.11f);
        b.Ell(head, c, r, pink);
        PokeBuilder.Both(s => CatEar(b, head, c + V(0.07f * s, 0.07f, -0.02f), c + V(0.11f * s, 0.15f, -0.03f), 0.035f, pink, Rgb(250, 210, 226)));
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.05f * s, 0.53f), V(0.45f * s, 0.05f, 1f), 0.034f, Rgb(70, 140, 220)));
        b.Mark(head, On(c, r, 0, 0.47f), V(0, -0.2f, 1f), 0.018f, 0.006f, Rgb(170, 100, 130), MarkShape.Smile);
        return Lift(b);
    }
}
