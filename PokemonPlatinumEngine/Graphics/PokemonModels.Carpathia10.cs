using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Carpathia, batch 10 (plan 46 · CP4 to CP8): Stimara to Tramvaior (20 species), sculpted from the briefs of
// docs/plans/decisions/CP-dex-4-folk-and-legends.md and the user's reference pictures. Our own designs: the lake
// dragons, the snowdrops and the March charm, the painted egg, the fresco spirits, the carpenter beetle and the carved
// gate, the căluș dancers and the hora, the doina's ghosts, the reed pipes, the endless column and the trams.
internal static partial class PokemonModels
{
    private static string[] CarpathiaNames10() => new[]
    {
        "Stimara", "Stimalaur", "Ghiocel", "Martisoara", "Oupictat", "Frescuta", "Frescanta", "Dulgherel", "Poartaur", "Calusel",
        "Calusar", "Horuta", "Doinel", "Doinatoare", "Naiet", "Nailar", "Coloanel", "Coloanfinit", "Tramvaiel", "Tramvaior"
    };

    private static PokeBuilder? CreateCarpathia10(string species) => species switch
    {
        "STIMARA" => C10LakeDragon(false),
        "STIMALAUR" => C10LakeDragon(true),
        "GHIOCEL" => C10Ghiocel(),
        "MARTISOARA" => C10Martisoara(),
        "OUPICTAT" => C10Oupictat(),
        "FRESCUTA" => C10Frescuta(),
        "FRESCANTA" => C10Frescanta(),
        "DULGHEREL" => C10Dulgherel(),
        "POARTAUR" => C10Poartaur(),
        "CALUSEL" => C10Calus(false),
        "CALUSAR" => C10Calus(true),
        "HORUTA" => C10Horuta(),
        "DOINEL" => C10Doinel(),
        "DOINATOARE" => C10Doinatoare(),
        "NAIET" => C10Naiet(),
        "NAILAR" => C10Nailar(),
        "COLOANEL" => C10Coloanel(),
        "COLOANFINIT" => C10Coloanfinit(),
        "TRAMVAIEL" => C10Tram(false),
        "TRAMVAIOR" => C10Tram(true),
        _ => null
    };

    // ------------------------------------------------------------------ helpers of this batch

    /// <summary>A twisted cord (the rope carving, the mărțișor's thread): two strands wound about a path, in two colours.</summary>
    private static void C10Cord(PokeBuilder b, int bone, Vector3[] path, float radius, float thick, Color one, Color two, float turns)
    {
        for (int k = 0; k < 2; k++)
        {
            var points = new Vector3[path.Length];
            for (int i = 0; i < path.Length; i++)
            {
                var along = Vector3.Normalize(path[Math.Min(path.Length - 1, i + 1)] - path[Math.Max(0, i - 1)]);
                var u = Vector3.Normalize(Vector3.Cross(along, MathF.Abs(along.Y) < 0.9f ? Vector3.UnitY : Vector3.UnitX));
                var w = Vector3.Cross(along, u);
                float a = MathF.Tau * (turns * i / (path.Length - 1f) + k * 0.5f);
                points[i] = path[i] + (u * MathF.Cos(a) + w * MathF.Sin(a)) * radius;
            }
            b.Tube(bone, points, thick, thick, k == 0 ? one : two, blend: 0.004f);
        }
    }

    /// <summary>A ring of points round a horizontal ellipse at height <paramref name="y"/>, closed.</summary>
    private static Vector3[] C10Ring(Vector3 c, float rx, float rz, int steps, float from = 0f, float to = MathF.Tau)
    {
        var points = new Vector3[steps + 1];
        for (int i = 0; i <= steps; i++)
        {
            float a = from + (to - from) * i / steps;
            points[i] = c + V(MathF.Sin(a) * rx, 0, MathF.Cos(a) * rz);
        }
        return points;
    }

    /// <summary>A bead of the endless column: two cones meeting at a sharp waist, narrow at both ends where the next bead sits.</summary>
    private static void C10Bead(PokeBuilder b, int bone, Vector3 c, float w, float h, Color color, Color shade)
    {
        b.Limb(bone, c - V(0, h, 0), c, w * 0.42f, w, color, Metal, 0f);
        b.Limb(bone, c, c + V(0, h, 0), w, w * 0.42f, color, Metal, 0f);
        // The facets: the lower cone's sides in shade, a lit face on the upper one
        PokeBuilder.Both(s => b.PaintEll(bone, c + V(w * 0.78f * s, -h * 0.45f, w * 0.2f), V(w * 0.35f, h * 0.45f, w * 0.5f), shade));
        b.PaintEll(bone, c + V(-w * 0.25f, h * 0.45f, w * 0.75f), V(w * 0.3f, h * 0.3f, w * 0.3f), PixelCanvas.Light1(color, 0.3f));
    }

    /// <summary>A little bell of brass.</summary>
    private static void C10Bell(PokeBuilder b, int bone, Vector3 at, float r)
    {
        var brass = Rgb(222, 178, 70);
        b.Ell(bone, at, V(r, r, r), brass, mat: Metal, blend: 0.004f);
        b.PaintEll(bone, at + V(0, -r * 0.55f, r * 0.55f), V(r * 0.4f, r * 0.2f, r * 0.3f), Rgb(80, 56, 30));
    }

    // ------------------------------------------------------------------ Stimara, Stimalaur

    /// <summary>
    /// The lake serpents: a long deep-blue body on four short webbed legs, the neck rising in an S to a long head, a
    /// silvery belly, a mane of teal fins down the back and a broad fin at the tail's tip. Stimalaur, the spirit of
    /// the waters, is the great one: teal and glacier blue, every fin longer, fins on its legs, a crown of pale horns
    /// and a beard of mist under its chin.
    /// </summary>
    private static PokeBuilder C10LakeDragon(bool great)
    {
        var b = new PokeBuilder(great ? "Stimalaur" : "Stimara", great ? 1f : 0.95f, BodyPlan.Quadruped, V(0, 0.3f, -0.05f)) { Coat = Scales };
        var blue = great ? Rgb(40, 104, 120) : Rgb(44, 92, 124);
        var legs = PixelCanvas.Shadow(blue, 0.12f);
        var belly = great ? Rgb(196, 226, 232) : Rgb(208, 216, 226);
        var fin = great ? Rgb(128, 214, 212) : Rgb(104, 188, 196);
        var ice = Rgb(214, 236, 246);
        var mist = Rgb(222, 238, 244);
        float k = great ? 1.12f : 1f;

        // The torso, low and long
        var spine = Smooth(3, V(0, 0.3f, -0.36f), V(0.02f, 0.3f, -0.12f), V(0, 0.32f, 0.1f), V(0, 0.36f, 0.22f));
        b.Tube(Body, spine, 0.125f * k, 0.13f * k, blue, blend: 0.02f);
        b.PaintEll(Body, V(0, 0.2f, -0.06f), V(0.1f * k, 0.06f, 0.36f), belly);

        // The tail: off the haunches, along the ground, up in an S to a broad fin
        int tail = b.Tail(V(0, 0.3f, -0.36f));
        var tailPath = Smooth(3, V(0, 0.3f, -0.36f), V(0.06f, 0.24f, -0.62f), V(0.02f, 0.26f, -0.86f), V(-0.06f, 0.42f, -0.98f), V(-0.02f, 0.64f, -0.94f),
            V(0.03f, 0.78f, -0.84f));
        b.Tube(tail, tailPath, 0.115f * k, 0.03f, blue, blend: 0.015f);
        var tip = tailPath[^1];
        Frond(b, tail, tip + V(0, -0.06f, -0.02f), tip + V(0.02f, 0.2f * k, 0.12f), 0.08f * k, fin, V(1f, 0, 0), 0.18f, Leaf, 0.01f);
        Frond(b, tail, tip + V(0, -0.08f, -0.03f), tip + V(0.0f, 0.08f, -0.2f * k), 0.07f * k, fin, V(1f, 0, 0), 0.18f, Leaf, 0.01f);
        // Fins along the tail's top
        for (int i = 2; i < tailPath.Length - 3; i += 2)
        {
            var at = tailPath[i];
            var dir = Vector3.Normalize(tailPath[i + 1] - tailPath[i - 1]);
            var up = Vector3.Normalize(Vector3.Cross(dir, V(1f, 0, 0)));
            if (up.Y < 0) up = -up;
            float r = (0.115f * k) * (1f - i / (float)tailPath.Length) + 0.03f;
            Blade(b, tail, at + up * r * 0.6f, at + up * (r + 0.1f * k) - dir * 0.08f, 0.04f * k, fin, V(1f, 0, 0), 0.3f, Leaf);
        }

        // Four short legs, webbed feet with pale claws
        foreach (var (z, front) in new[] { (0.12f, true), (-0.26f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.1f * s, 0.28f, z), front);
                var knee = V(0.17f * s, 0.15f, z + 0.02f);
                b.Limb(leg, V(0.1f * s, 0.28f, z), knee, 0.065f * k, 0.05f * k, legs);
                b.Limb(leg, knee, V(0.19f * s, 0.04f, z + 0.04f), 0.05f * k, 0.042f * k, legs);
                b.Ell(leg, V(0.19f * s, 0.03f, z + 0.08f), V(0.06f * k, 0.03f, 0.075f * k), legs, blend: 0.012f);
                for (int i = -1; i <= 1; i++)
                    b.Spike(leg, V((0.19f + 0.035f * i) * s, 0.025f, z + 0.13f), V((0.19f + 0.05f * i) * s, 0.012f, z + 0.18f), 0.014f, Claw, mat: Shell);
                if (great)
                    Blade(b, leg, knee + V(0.03f * s, 0, -0.03f), knee + V(0.12f * s, 0.02f, -0.12f), 0.035f, fin, V(0, 0.4f, 1f), 0.3f, Leaf);
            });

        // The neck rising in an S, its front silver
        int neck = b.Part("neck", Body, V(0, 0.38f, 0.24f), PokeRole.Segment);
        var neckPath = Smooth(3, V(0, 0.36f, 0.22f), V(0, 0.5f, 0.32f), V(0, 0.68f, 0.3f), V(0, 0.82f, 0.3f));
        b.Tube(neck, neckPath, 0.1f * k, 0.07f * k, blue, blend: 0.02f);
        for (int i = 0; i < neckPath.Length - 1; i++)
        {
            var mid = (neckPath[i] + neckPath[i + 1]) / 2f;
            var d = Vector3.Normalize(neckPath[i + 1] - neckPath[i]);
            var front = Vector3.Normalize(V(0, 0, 1f) - d * Vector3.Dot(V(0, 0, 1f), d));
            float r = 0.1f * k + (0.07f * k - 0.1f * k) * i / (neckPath.Length - 1);
            b.PaintEll(neck, mid + front * r * 0.85f, V(r * 0.7f, Vector3.Distance(neckPath[i], neckPath[i + 1]) * 0.5f + r * 0.6f, r * 0.6f), belly, Euler(d));
        }

        // The mane: fins down the back of the neck and along the spine
        for (int i = 0; i < 6; i++)
        {
            float t = i / 5f;
            var at = Vector3.Lerp(V(0, 0.82f, 0.22f), V(0, 0.44f, 0.18f), t);
            int bone = t < 0.7f ? neck : Body;
            Blade(b, bone, at + V(0, 0, -0.02f), at + V(0.03f * (i % 2 == 0 ? 1 : -1), 0.06f * k, -0.16f * k - 0.03f * i), 0.045f * k, fin, V(1f, 0, 0), 0.28f, Leaf);
        }
        for (int i = 0; i < 4; i++)
        {
            var at = V(0, 0.42f * k, 0.1f - i * 0.14f);
            Blade(b, Body, at, at + V(0.02f * (i % 2 == 0 ? 1 : -1), 0.1f * k, -0.1f), 0.05f * k, fin, V(1f, 0, 0), 0.28f, Leaf);
        }

        // The head: a long skull and snout, fin ears, horn buds or a crown of ice
        int head = b.Head(V(0, 0.82f, 0.3f), neck);
        var c = V(0, 0.88f, 0.34f);
        var hr = V(0.08f * k, 0.07f * k, 0.09f * k);
        b.Ell(head, c, hr, blue);
        var snout = c + V(0, -0.025f, 0.1f * k);
        b.Ell(head, snout, V(0.055f * k, 0.042f * k, 0.08f * k), blue, blend: 0.03f);
        b.PaintEll(head, snout + V(0, -0.035f * k, 0.01f), V(0.05f * k, 0.022f, 0.07f * k), belly);
        b.Mark(head, snout + V(0, -0.014f * k, 0.075f * k), V(0, -0.2f, 1f), 0.035f * k, 0.006f, PixelCanvas.Shadow(blue, 0.5f), MarkShape.Bar);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.06f * s * k, 0.02f, -0.03f));
            HeadFin(b, ear, c + V(0.06f * s * k, 0.02f, -0.03f), s, 0.14f * k, fin);
            if (!great)
                b.Spike(head, c + V(0.035f * s, 0.05f, -0.03f), c + V(0.05f * s, 0.12f, -0.1f), 0.018f, PixelCanvas.Light1(blue, 0.3f), mat: Shell);
        });
        if (great)
        {
            // A crown of pale horns, the middle ones the tallest, swept back
            for (int i = 0; i < 6; i++)
            {
                float x = (i - 2.5f) * 0.03f;
                float tall = 0.2f - MathF.Abs(i - 2.5f) * 0.03f;
                b.Spike(head, c + V(x, 0.05f, -0.02f), c + V(x * 2.2f, 0.05f + tall, -0.1f - tall * 0.3f), 0.016f, ice, mat: Shell);
            }
            // A beard of mist and two long whiskers from the snout
            for (int i = 0; i < 5; i++)
            {
                float x = (i - 2f) * 0.022f;
                b.Tube(head, Smooth(2, snout + V(x, -0.03f, -0.02f), snout + V(x * 1.4f, -0.09f, 0.0f), snout + V(x * 1.6f + 0.01f * (i % 2), -0.16f - 0.02f * (i % 3), -0.02f)),
                    0.016f, 0.008f, mist, Glow, 0.006f);
            }
            PokeBuilder.Both(s => b.Tube(head, Smooth(3, snout + V(0.04f * s, 0.0f, 0.04f), snout + V(0.14f * s, -0.02f, 0.02f), snout + V(0.24f * s, -0.1f, -0.06f)),
                0.008f, 0.006f, mist, Glow, 0.004f));
        }
        PokeBuilder.Both(s =>
        {
            var look = V(0.55f * s, 0.15f, 1f);
            b.Eye(head, Out(c, hr, default, look), look, 0.024f * k, great ? Rgb(150, 210, 220) : Rgb(90, 120, 140), glare: great);
        });
        return b;
    }

    // ------------------------------------------------------------------ Ghiocel, Martisoara

    /// <summary>A snowdrop's head: a little face under nodding white petals tipped in green, a green cap and its snow.</summary>
    private static void C10Snowdrop(PokeBuilder b, int bone, Vector3 c, float r, bool face, bool snow)
    {
        var petal = Rgb(244, 248, 246);
        var green = Rgb(112, 168, 88);
        var skin = Rgb(248, 242, 230);
        if (face) b.Ell(bone, c, V(r * 0.95f, r * 0.85f, r * 0.85f), skin);
        // The petals hang from the cap round the face, leaving its front bare
        var top = c + V(0, r * 1.25f, -r * 0.1f);
        foreach (float a in face ? new[] { -105f, -55f, 55f, 105f, 160f, -160f } : new[] { -120f, 0f, 120f })
        {
            float rad = a * Degree;
            var tip = c + V(MathF.Sin(rad) * r * 1.25f, -r * 0.9f, MathF.Cos(rad) * r * 1.15f);
            Petal(b, bone, top, tip, r * 0.62f, petal, Leaf, 0.02f);
            b.PaintEll(bone, Vector3.Lerp(top, tip, 0.92f), V(r * 0.45f, r * 0.2f, r * 0.45f), green);
        }
        b.Ell(bone, top + V(0, r * 0.15f, 0), V(r * 0.42f, r * 0.35f, r * 0.42f), green, mat: Leaf);
        if (snow) b.Ell(bone, top + V(0, r * 0.42f, 0), V(r * 0.5f, r * 0.2f, r * 0.48f), Rgb(250, 252, 255), blend: 0.01f);
    }

    /// <summary>Ghiocel: a tiny snowdrop, its nodding flower a head with a face, a thin stem, two slender leaves for arms and root feet.</summary>
    private static PokeBuilder C10Ghiocel()
    {
        var b = new PokeBuilder("Ghiocel", 0.4f, BodyPlan.Biped, V(0, 0.28f, 0)) { Coat = Leaf };
        var green = Rgb(104, 160, 82);
        var dark = PixelCanvas.Shadow(green, 0.2f);

        // Two short stems for legs, each on a foot of little roots
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.025f * s, 0.18f, 0));
            b.Limb(leg, V(0.02f * s, 0.2f, 0), V(0.035f * s, 0.03f, 0.01f), 0.022f, 0.02f, dark);
            Frond(b, leg, V(0.035f * s, 0.018f, 0.0f), V(0.09f * s, 0.012f, 0.06f), 0.025f, green, V(0, 1f, 0), 0.3f, Leaf);
            Frond(b, leg, V(0.035f * s, 0.018f, 0.0f), V(0.05f * s, 0.012f, -0.06f), 0.022f, green, V(0, 1f, 0), 0.3f, Leaf);
        });
        // The stem body, curving up and over to the nodding head
        b.Tube(Body, Smooth(3, V(0, 0.16f, 0), V(0, 0.32f, -0.02f), V(0, 0.48f, -0.03f)), 0.04f, 0.03f, green, blend: 0.015f);
        // The leaves for arms
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.03f * s, 0.34f, 0));
            LongLeaf(b, arm, V(0.03f * s, 0.34f, 0), V(0.14f * s, 0.42f, 0.03f), V(0.22f * s, 0.34f, 0.06f), 0.03f, green, 0.012f);
        });
        int head = b.Head(V(0, 0.48f, -0.02f));
        b.Tube(head, Smooth(3, V(0, 0.46f, -0.03f), V(0, 0.6f, -0.02f), V(0, 0.66f, 0.04f)), 0.028f, 0.024f, green, blend: 0.015f);
        var c = V(0, 0.55f, 0.07f);
        float r = 0.09f;
        C10Snowdrop(b, head, c, r, true, true);
        PokeBuilder.Both(s =>
        {
            var look = V(0.45f * s, -0.05f, 1f);
            b.Eye(head, Out(c, V(r * 0.95f, r * 0.85f, r * 0.85f), default, look), look, 0.018f, Rgb(70, 50, 46));
            b.Mark(head, Out(c, V(r * 0.95f, r * 0.85f, r * 0.85f), default, V(0.6f * s, -0.4f, 1f)), V(0.6f * s, -0.4f, 1f), 0.016f, 0.01f, Rgb(244, 160, 170));
        });
        b.Mark(head, Out(c, V(r * 0.95f, r * 0.85f, r * 0.85f), default, V(0, -0.45f, 1f)), V(0, -0.45f, 1f), 0.012f, 0.008f, Rgb(120, 70, 60), MarkShape.Smile);
        return b;
    }

    /// <summary>
    /// Martisoara: a spring spirit of snowdrop blossoms, its body a bundle of green stems tied round the middle with the
    /// mărțișor's twisted red and white cord, a red tassel and a white one hanging, a four-leaf clover charm at its chest,
    /// a big snowdrop for a head and three more nodding round it.
    /// </summary>
    private static PokeBuilder C10Martisoara()
    {
        var b = new PokeBuilder("Martisoara", 0.62f, BodyPlan.Biped, V(0, 0.38f, 0)) { Coat = Leaf };
        var green = Rgb(98, 156, 78);
        var dark = PixelCanvas.Shadow(green, 0.2f);
        var red = Rgb(214, 46, 46);
        var white = Rgb(246, 244, 240);
        var gold = Rgb(220, 176, 70);

        // Legs: bundles of stems ending in leaves
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.2f, 0));
            b.Limb(leg, V(0.04f * s, 0.24f, 0), V(0.07f * s, 0.05f, 0.01f), 0.035f, 0.028f, dark);
            Frond(b, leg, V(0.07f * s, 0.025f, -0.02f), V(0.1f * s, 0.02f, 0.09f), 0.035f, green, V(0, 1f, 0), 0.3f, Leaf);
        });
        // The body: a bundle of stems, wider at the shoulders, its tops spreading into the flowers
        for (int i = 0; i < 9; i++)
        {
            float a = i * MathF.Tau / 9f;
            var foot = V(MathF.Sin(a) * 0.055f, 0.2f, MathF.Cos(a) * 0.045f);
            var top = V(MathF.Sin(a) * 0.08f, 0.6f, MathF.Cos(a) * 0.06f - 0.01f);
            b.Limb(Body, foot, top, 0.03f, 0.034f, i % 2 == 0 ? green : dark, blend: 0.012f);
        }
        b.Ell(Body, V(0, 0.42f, 0), V(0.1f, 0.17f, 0.08f), green, blend: 0.02f);
        // The mărțișor: a twisted cord round the waist, two strands hanging to a red tassel and a white one
        C10Cord(b, Body, C10Ring(V(0, 0.33f, 0), 0.1f, 0.085f, 24), 0.011f, 0.012f, red, white, 10f);
        C10Cord(b, Body, C10Ring(V(0, 0.52f, 0), 0.096f, 0.078f, 24), 0.009f, 0.01f, red, white, 9f);
        C10Cord(b, Body, Smooth(3, V(0.09f, 0.32f, 0.05f), V(0.15f, 0.24f, 0.06f), V(0.2f, 0.16f, 0.06f)), 0.008f, 0.009f, red, white, 4f);
        b.Ell(Body, V(0.2f, 0.15f, 0.06f), V(0.022f, 0.022f, 0.022f), red, blend: 0.006f);
        b.Spike(Body, V(0.2f, 0.14f, 0.06f), V(0.21f, 0.06f, 0.07f), 0.03f, red, blend: 0.01f);
        C10Cord(b, Body, Smooth(3, V(0.09f, 0.32f, 0.05f), V(0.18f, 0.3f, 0.04f), V(0.27f, 0.22f, 0.03f)), 0.008f, 0.009f, red, white, 4f);
        b.Ell(Body, V(0.27f, 0.21f, 0.03f), V(0.02f, 0.02f, 0.02f), white, blend: 0.006f);
        b.Spike(Body, V(0.27f, 0.2f, 0.03f), V(0.29f, 0.12f, 0.03f), 0.028f, white, blend: 0.01f);
        // The clover charm at its chest: a gold disc and four green leaves painted on it
        var charm = V(0, 0.44f, 0.085f);
        b.Ell(Body, charm, V(0.034f, 0.034f, 0.012f), gold, mat: Metal, blend: 0.006f);
        b.Mark(Body, charm + V(0, 0, 0.012f), V(0, 0, 1f), 0.024f, 0.024f, Rgb(70, 150, 70), MarkShape.Star5);
        // Leafy arms raised in joy
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.08f * s, 0.5f, 0));
            LongLeaf(b, arm, V(0.08f * s, 0.5f, 0), V(0.2f * s, 0.58f, 0.02f), V(0.3f * s, 0.66f, 0.05f), 0.035f, green, 0.012f);
            LongLeaf(b, arm, V(0.08f * s, 0.47f, 0), V(0.2f * s, 0.46f, 0.02f), V(0.3f * s, 0.42f, 0.04f), 0.03f, dark, 0.012f);
        });
        // The head, and three smaller snowdrops nodding on stems round it
        int head = b.Head(V(0, 0.6f, 0));
        var c = V(0, 0.7f, 0.05f);
        float r = 0.1f;
        C10Snowdrop(b, head, c, r, true, true);
        foreach (var (x, y, z) in new[] { (-0.2f, 0.78f, -0.04f), (0.21f, 0.76f, -0.03f), (0.08f, 0.9f, -0.1f) })
        {
            b.Tube(head, Smooth(3, V(x * 0.3f, 0.62f, -0.03f), V(x * 0.8f, y + 0.1f, z), V(x, y + 0.07f, z)), 0.014f, 0.012f, green, blend: 0.008f);
            C10Snowdrop(b, head, V(x, y - 0.01f, z), 0.05f, false, true);
        }
        var fr = V(r * 0.95f, r * 0.85f, r * 0.85f);
        PokeBuilder.Both(s =>
        {
            var look = V(0.45f * s, 0f, 1f);
            b.Eye(head, Out(c, fr, default, look), look, 0.02f, Rgb(90, 50, 46));
            b.Mark(head, Out(c, fr, default, V(0.62f * s, -0.38f, 1f)), V(0.62f * s, -0.38f, 1f), 0.017f, 0.011f, Rgb(244, 156, 166));
        });
        b.Mark(head, Out(c, fr, default, V(0, -0.42f, 1f)), V(0, -0.42f, 1f), 0.02f, 0.012f, Rgb(170, 60, 60), MarkShape.Smile);
        return b;
    }

    // ------------------------------------------------------------------ Oupictat

    /// <summary>
    /// Oupictat: a painted egg of Bucovina, deep red banded in black and gold, stars and waves on its bands, a cream
    /// panel on its front with a little face and a beak, tiny feet and a crack across its top glowing with light.
    /// </summary>
    private static PokeBuilder C10Oupictat()
    {
        var b = new PokeBuilder("Oupictat", 0.45f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Shell };
        var red = Rgb(150, 30, 38);
        var black = Rgb(46, 30, 30);
        var gold = Rgb(222, 168, 70);
        var cream = Rgb(244, 232, 206);
        var c = V(0, 0.33f, 0);
        var er = V(0.21f, 0.28f, 0.19f);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.1f, 0.02f));
            b.Ell(leg, V(0.075f * s, 0.035f, 0.03f), V(0.04f, 0.04f, 0.05f), red);
            b.PaintEll(leg, V(0.075f * s, 0.02f, 0.03f), V(0.05f, 0.012f, 0.06f), gold);
        });
        b.Ell(Body, c, er, red);
        // A narrower top, as an egg has
        b.Ell(Body, c + V(0, 0.1f, 0), V(0.17f, 0.2f, 0.155f), red);
        // The bands round it: gold over black, a black girdle low down
        float Rad(float y) => MathF.Sqrt(MathF.Max(0f, 1f - (y - c.Y) * (y - c.Y) / (er.Y * er.Y)));
        foreach (var (y, col, tube) in new[] { (0.2f, black, 0.03f), (0.24f, gold, 0.012f), (0.13f, gold, 0.01f), (0.42f, black, 0.024f), (0.45f, gold, 0.01f) })
        {
            float rr = er.X * Rad(y);
            b.PaintTorus(Body, V(0, y, 0), rr, tube, col, sz: er.Z / er.X);
        }
        // A band from top to bottom down each side, black edged in gold
        PokeBuilder.Both(s =>
        {
            b.PaintTorus(Body, c, er.Y * 0.98f, 0.03f, black, V(0, 0, 90f), sx: 1f, sz: er.X / er.Y);
        });
        // The cream panel on its front with the face, and the stars on its bands
        b.PaintEll(Body, c + V(0, 0.03f, er.Z * 0.9f), V(0.11f, 0.09f, 0.08f), cream);
        var look = V(0, 0.1f, 1f);
        PokeBuilder.Both(s =>
        {
            var l = V(0.3f * s, 0.12f, 1f);
            b.Eye(Body, Out(c, er, default, l), l, 0.022f, Rgb(70, 30, 30));
            b.Mark(Body, Out(c, er, default, V(0.48f * s, -0.04f, 1f)), V(0.48f * s, -0.04f, 1f), 0.02f, 0.012f, Rgb(230, 110, 100));
            b.Mark(Body, Out(c, er, default, V(1f * s, -0.4f, 0.6f)), V(1f * s, -0.4f, 0.6f), 0.03f, 0.03f, gold, MarkShape.Star);
            b.Mark(Body, Out(c, er, default, V(0.6f * s, 0.55f, 0.7f)), V(0.6f * s, 0.55f, 0.7f), 0.026f, 0.026f, gold, MarkShape.Star);
        });
        // A beak and a star under it
        b.Spike(Body, Out(c, er, default, look) + V(0, -0.02f, -0.01f), Out(c, er, default, look) + V(0, -0.025f, 0.04f), 0.016f, Rgb(236, 120, 60), mat: Shell);
        b.Mark(Body, Out(c, er, default, V(0, -0.35f, 1f)), V(0, -0.35f, 1f), 0.026f, 0.026f, red, MarkShape.Star);
        // The crack across the top, light showing through
        b.Mark(Body, c + V(0, er.Y * 0.88f + 0.06f, 0.06f), V(0, 1f, 0.5f), 0.07f, 0.02f, Rgb(255, 224, 120), MarkShape.Zigzag);
        return b;
    }

    // ------------------------------------------------------------------ Frescuta, Frescanta

    /// <summary>The fresco spirits' face: a pale oval in a blue hood, ochre marks on its cheeks and a gentle painted look.</summary>
    private static void C10FrescoFace(PokeBuilder b, int head, Vector3 c, Vector3 r, Color blue, bool shut)
    {
        var skin = Rgb(240, 222, 196);
        var ochre = Rgb(200, 104, 60);
        // The hood first, cut open at the front, then the face whole inside it
        b.Ell(head, c + V(0, 0.01f, -r.Z * 0.2f), r * 1.14f, blue);
        b.Cut(head, c + V(0, -0.005f, r.Z * 0.95f), V(r.X * 0.82f, r.Y * 0.86f, r.Z * 0.6f));
        b.Ell(head, c + V(0, -0.005f, -r.Z * 0.05f), r * 0.98f, skin, blend: 0.004f);
        PokeBuilder.Both(s => b.Mark(head, Out(c, r, default, V(0.5f * s, -0.25f, 1f)), V(0.5f * s, -0.25f, 1f), r.X * 0.2f, r.X * 0.12f, ochre, MarkShape.Wave, 60f * s));
        b.Mark(head, Out(c, r, default, V(0, -0.4f, 1f)), V(0, -0.4f, 1f), r.X * 0.13f, r.X * 0.065f, Rgb(170, 80, 70), MarkShape.Smile);
        PokeBuilder.Both(s =>
        {
            var look = V(0.38f * s, 0.12f, 1f);
            b.Eye(head, Out(c, r, default, look), look, r.Y * 0.17f, Rgb(110, 104, 92), closed: shut);
        });
    }

    /// <summary>A gold disc of light behind a head, its rim raised and a ring of red petals painted inside.</summary>
    private static void C10Halo(PokeBuilder b, int bone, Vector3 c, float r, float depth)
    {
        var gold = Rgb(222, 176, 76);
        b.Ell(bone, c, V(r, r, depth), gold, mat: Metal, blend: 0.01f);
        b.Torus(bone, c + V(0, 0, depth * 0.4f), r * 0.93f, depth * 0.55f, PixelCanvas.Shadow(gold, 0.18f), V(90f, 0, 0), mat: Metal, blend: 0.004f);
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f;
            b.PaintEll(bone, c + V(MathF.Sin(a) * r * 0.72f, MathF.Cos(a) * r * 0.72f, depth), V(r * 0.08f, r * 0.08f, depth * 0.6f), Rgb(190, 70, 50));
        }
    }

    /// <summary>Frescuta: a little flat figure stepped out of a painted wall, its robe the monasteries' blue with ochre and red borders, a gold halo behind its hooded head.</summary>
    private static PokeBuilder C10Frescuta()
    {
        var b = new PokeBuilder("Frescuta", 0.5f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Fur };
        var blue = Rgb(40, 76, 160);
        var skin = Rgb(240, 222, 196);
        var ochre = Rgb(212, 150, 70);
        var red = Rgb(186, 70, 46);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.12f, 0));
            b.Limb(leg, V(0.05f * s, 0.14f, 0), V(0.055f * s, 0.035f, 0.005f), 0.03f, 0.028f, skin);
            b.Ell(leg, V(0.056f * s, 0.022f, 0.015f), V(0.032f, 0.022f, 0.04f), skin);
        });
        // A robe flat as a panel, flaring at the hem, a border of ochre and red
        b.Ell(Body, V(0, 0.3f, 0), V(0.13f, 0.17f, 0.06f), blue);
        b.Ell(Body, V(0, 0.18f, 0), V(0.17f, 0.07f, 0.065f), blue, blend: 0.03f);
        b.PaintEll(Body, V(0, 0.13f, 0), V(0.3f, 0.035f, 0.2f), ochre);
        b.PaintEll(Body, V(0, 0.105f, 0), V(0.3f, 0.015f, 0.2f), red);
        // The collar: a red and ochre point down the chest with a gold star on it
        b.PaintEll(Body, V(0, 0.42f, 0.04f), V(0.1f, 0.05f, 0.05f), ochre);
        b.PaintEll(Body, V(0, 0.4f, 0.05f), V(0.06f, 0.03f, 0.04f), red);
        b.Mark(Body, V(0, 0.36f, 0.06f), V(0, 0, 1f), 0.024f, 0.024f, Rgb(236, 196, 90), MarkShape.Star);
        // Short sleeves and pale hands
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.1f * s, 0.38f, 0));
            b.Limb(arm, V(0.1f * s, 0.38f, 0), V(0.2f * s, 0.27f, 0.01f), 0.045f, 0.05f, blue);
            b.PaintEll(arm, V(0.2f * s, 0.27f, 0.01f), V(0.05f, 0.03f, 0.06f), ochre);
            b.Ell(arm, V(0.225f * s, 0.24f, 0.01f), V(0.028f, 0.035f, 0.02f), skin);
        });
        int head = b.Head(V(0, 0.44f, 0));
        var c = V(0, 0.56f, 0.01f);
        C10Halo(b, head, V(0, 0.6f, -0.045f), 0.17f, 0.022f);
        C10FrescoFace(b, head, c, V(0.1f, 0.11f, 0.065f), blue, false);
        return b;
    }

    /// <summary>
    /// Frescanta: the painted wall's spirit, tall and flat, floating: a panel of deep blue painted with clouds, stars and
    /// gold, long bell sleeves bordered in ochre and red, an elongated hooded head before a great gold disc, and an edge
    /// of old plaster cracking away at its foot.
    /// </summary>
    private static PokeBuilder C10Frescanta()
    {
        var b = new PokeBuilder("Frescanta", 0.85f, BodyPlan.Floating, V(0, 0.6f, 0)) { Coat = Fur }.Hover();
        var blue = Rgb(36, 70, 150);
        var ochre = Rgb(212, 150, 70);
        var red = Rgb(180, 70, 46);
        var plaster = Rgb(214, 196, 162);
        var cloud = Rgb(232, 226, 210);

        // The panel: a tall slab, its foot broken off in a ragged edge of plaster
        b.Box(Body, V(0, 0.62f, 0), V(0.17f, 0.44f, 0.05f), 0.04f, blue);
        b.Box(Body, V(0.155f, 0.56f, -0.005f), V(0.035f, 0.38f, 0.045f), 0.02f, plaster, V(0, 0, -3f));
        foreach (var (x, y, r) in new[] { (-0.12f, 0.17f, 0.07f), (0.02f, 0.15f, 0.06f), (0.13f, 0.2f, 0.065f), (-0.04f, 0.12f, 0.045f) })
            b.Cut(Body, V(x, y, 0), V(r, r * 0.9f, 0.2f));
        b.PaintEll(Body, V(0, 0.22f, 0), V(0.25f, 0.06f, 0.2f), plaster);
        // Painted on it: a cloud band, little haloed figures, stars, a red and ochre collar
        b.PaintEll(Body, V(-0.02f, 0.36f, 0.05f), V(0.12f, 0.035f, 0.03f), cloud);
        b.PaintEll(Body, V(0.06f, 0.6f, 0.05f), V(0.07f, 0.025f, 0.03f), cloud);
        foreach (var (x, y) in new[] { (-0.05f, 0.5f), (0.07f, 0.78f), (-0.08f, 0.82f) })
        {
            b.Mark(Body, V(x, y + 0.03f, 0.05f), V(0, 0, 1f), 0.024f, 0.024f, Rgb(226, 176, 72), MarkShape.Ring);
            b.PaintEll(Body, V(x, y - 0.03f, 0.05f), V(0.022f, 0.04f, 0.02f), red);
        }
        foreach (var (x, y) in new[] { (0.1f, 0.42f), (-0.11f, 0.66f), (0.02f, 0.97f) })
            b.Mark(Body, V(x, y, 0.05f), V(0, 0, 1f), 0.018f, 0.018f, Rgb(236, 200, 96), MarkShape.Star);
        b.PaintEll(Body, V(0, 0.98f, 0.04f), V(0.14f, 0.06f, 0.05f), ochre);
        b.PaintEll(Body, V(0, 0.96f, 0.045f), V(0.09f, 0.035f, 0.04f), red);

        // Long bell sleeves hanging from the shoulders, open at their ends
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.15f * s, 0.94f, 0));
            b.Limb(arm, V(0.15f * s, 0.94f, 0.005f), V(0.26f * s, 0.78f, 0.03f), 0.05f, 0.06f, blue);
            b.Limb(arm, V(0.26f * s, 0.78f, 0.03f), V(0.3f * s, 0.46f, 0.04f), 0.06f, 0.085f, blue);
            b.PaintEll(arm, V(0.285f * s, 0.62f, 0.04f), V(0.1f, 0.12f, 0.1f), red, V(0, 0, 8f * s));
            b.PaintEll(arm, V(0.3f * s, 0.46f, 0.04f), V(0.12f, 0.03f, 0.12f), ochre);
            b.PaintEll(arm, V(0.3f * s, 0.42f, 0.04f), V(0.06f, 0.02f, 0.06f), Rgb(24, 34, 70));
        });
        int head = b.Head(V(0, 1.02f, 0));
        var c = V(0, 1.14f, 0.012f);
        C10Halo(b, head, V(0, 1.18f, -0.045f), 0.22f, 0.024f);
        C10FrescoFace(b, head, c, V(0.075f, 0.1f, 0.06f), blue, false);
        return Lift(b);
    }

    // ------------------------------------------------------------------ Dulgherel, Poartaur

    /// <summary>A curl of wood shaving: a tight little roll.</summary>
    private static void C10Shaving(PokeBuilder b, int bone, Vector3 at, float r, Vector3 turn)
    {
        var shaving = Rgb(232, 204, 160);
        b.Torus(bone, at, r, r * 0.32f, shaving, turn, mat: Leaf, blend: 0.006f);
        b.Torus(bone, at + V(0, r * 0.15f, 0), r * 0.6f, r * 0.28f, PixelCanvas.Shadow(shaving, 0.1f), turn, mat: Leaf, blend: 0.006f);
    }

    /// <summary>
    /// Dulgherel: a small brown beetle, a carpenter: a domed shell carved with a rope twist, chisel-blade jaws, wood
    /// shavings and a leaf on its back and a twig of a pencil behind one antenna.
    /// </summary>
    private static PokeBuilder C10Dulgherel()
    {
        var b = new PokeBuilder("Dulgherel", 0.45f, BodyPlan.Quadruped, V(0, 0.16f, -0.04f)) { Coat = Shell };
        var shell = Rgb(160, 102, 58);
        var body = Rgb(132, 96, 64);
        var head = Rgb(170, 128, 86);
        var steel = Rgb(212, 206, 196);
        var green = Rgb(96, 150, 70);

        // Six legs, three a side
        foreach (var (z, i) in new[] { (0.06f, 0), (-0.06f, 1), (-0.17f, 2) })
            PokeBuilder.Both(s =>
            {
                var hip = V(0.08f * s, 0.12f, z);
                int leg = i == 1 ? b.Part(s < 0 ? "midL" : "midR", Root, hip, PokeRole.Leg, s * 0.5f + 0.8f, s) : b.Leg(s, hip, i == 0);
                var knee = V(0.17f * s, 0.12f, z + 0.02f * (1 - i));
                b.Limb(leg, hip, knee, 0.022f, 0.02f, body);
                b.Limb(leg, knee, V(0.2f * s, 0.02f, z + 0.04f * (1 - i)), 0.02f, 0.016f, body);
                b.Spike(leg, V(0.2f * s, 0.025f, z + 0.04f * (1 - i)), V(0.215f * s, 0.006f, z + 0.04f * (1 - i) + 0.03f), 0.012f, PixelCanvas.Shadow(body, 0.3f));
            });
        b.Ell(Body, V(0, 0.12f, -0.04f), V(0.12f, 0.07f, 0.17f), body);
        // The shell: a dome, its seam down the middle and a rope twist carved round its lower edge
        b.Ell(Body, V(0, 0.18f, -0.06f), V(0.16f, 0.12f, 0.2f), shell);
        b.PaintEll(Body, V(0, 0.3f, -0.06f), V(0.008f, 0.04f, 0.2f), PixelCanvas.Shadow(shell, 0.3f));
        var rope = C10Ring(V(0, 0.13f, -0.06f), 0.158f, 0.198f, 28, -2.3f, 2.3f);
        for (int i = 0; i < rope.Length - 1; i++)
        {
            var mid = (rope[i] + rope[i + 1]) / 2f;
            var out1 = Vector3.Normalize(V(mid.X, 0, (mid.Z + 0.06f) * 0.8f));
            b.Ell(Body, mid + out1 * 0.004f, V(0.013f, 0.02f, 0.013f), PixelCanvas.Light1(shell, 0.15f), V(0, 0, 35f), mat: Shell, blend: 0.004f);
        }
        // On its back: shavings and a leaf
        foreach (var (at, turn) in new[]
                 {
                     (V(-0.05f, 0.3f, -0.08f), V(80f, 20f, 10f)), (V(0.05f, 0.3f, -0.04f), V(70f, -30f, 0f)), (V(0.0f, 0.29f, -0.15f), V(90f, 60f, 20f)),
                     (V(0.08f, 0.27f, -0.13f), V(60f, 10f, -30f))
                 })
            C10Shaving(b, Body, at, 0.03f, turn);
        Frond(b, Body, V(-0.03f, 0.29f, -0.02f), V(-0.09f, 0.31f, 0.04f), 0.02f, green, V(0, 1f, 0), 0.3f, Leaf);

        // A round head with big eyes, chisel jaws and two segmented antennae, a pencil behind the right one
        int h = b.Head(V(0, 0.15f, 0.1f));
        var c = V(0, 0.16f, 0.15f);
        var hr = V(0.085f, 0.075f, 0.07f);
        b.Ell(h, c, hr, head);
        b.Ell(h, c + V(0, -0.025f, 0.06f), V(0.035f, 0.03f, 0.03f), head, blend: 0.02f);
        PokeBuilder.Both(s =>
        {
            Blade(b, h, c + V(0.04f * s, -0.04f, 0.04f), c + V(0.02f * s, -0.07f, 0.16f), 0.025f, steel, V(0, 1f, 0), 0.32f, Metal);
            b.Limb(h, c + V(0.04f * s, -0.04f, 0.03f), c + V(0.035f * s, -0.05f, 0.08f), 0.016f, 0.014f, PixelCanvas.Shadow(head, 0.2f));
            Antenna(b, h, c + V(0.04f * s, 0.06f, 0.0f), c + V(0.06f * s, 0.12f, 0.01f), c + V(0.085f * s, 0.18f, 0.03f), 0.008f, body, body, 0.012f);
            var look = V(0.55f * s, 0.1f, 1f);
            b.Eye(h, Out(c, hr, default, look), look, 0.028f, Rgb(60, 40, 34));
        });
        // The pencil: pale wood, a dark point
        var p0 = c + V(0.12f, 0.08f, -0.03f);
        var p1 = c + V(-0.01f, 0.06f, 0.0f);
        b.Limb(h, p0, p1, 0.01f, 0.01f, Rgb(196, 150, 96), Leaf, 0.004f);
        b.Spike(h, p0, p0 + Vector3.Normalize(p0 - p1) * 0.03f, 0.01f, Rgb(60, 48, 40));
        return b;
    }

    /// <summary>
    /// Poartaur: a carved gate of Maramureș, living: two oak pillars for legs carved with suns and the tree of life and
    /// wound with ivy, a beam across them carved with the rope twist, a shingled roof at its middle and a carved sun for
    /// its face, curls of shavings on the beam and root claws at its feet.
    /// </summary>
    private static PokeBuilder C10Poartaur()
    {
        var b = new PokeBuilder("Poartaur", 1f, BodyPlan.Biped, V(0, 0.86f, 0)) { Coat = Shell };
        var wood = Rgb(156, 104, 60);
        var light = PixelCanvas.Light1(wood, 0.2f);
        var dark = PixelCanvas.Shadow(wood, 0.3f);
        var shingle = Rgb(110, 72, 46);
        var ivy = Rgb(84, 148, 66);
        var sun = Rgb(196, 140, 74);

        // The pillars: square posts of oak, carved, wound with ivy, rooted in clawed feet
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.36f * s, 0.76f, 0));
            b.Box(leg, V(0.36f * s, 0.42f, 0), V(0.1f, 0.38f, 0.1f), 0.03f, wood);
            b.Ell(leg, V(0.36f * s, 0.08f, 0), V(0.14f, 0.1f, 0.14f), wood, blend: 0.04f);
            foreach (var (dx, dz) in new[] { (-0.1f, 0.12f), (0.0f, 0.15f), (0.1f, 0.12f), (0.12f * s, -0.08f) })
                b.Spike(leg, V(0.36f * s + dx, 0.04f, dz * 0.7f), V(0.36f * s + dx * 1.4f, 0.012f, dz * 1.35f), 0.03f, Rgb(214, 210, 196));
            b.Mark(leg, V(0.36f * s, 0.6f, 0.1f), V(0, 0, 1f), 0.06f, 0.06f, dark, MarkShape.Star);
            b.Mark(leg, V(0.36f * s, 0.34f, 0.1f), V(0, 0, 1f), 0.055f, 0.08f, dark, MarkShape.Star5);
            // The rope twist carved down each post's edge, and ivy round it
            C10Cord(b, leg, new[] { V(0.36f * s - 0.1f, 0.74f, 0.1f), V(0.36f * s - 0.1f, 0.1f, 0.1f) }, 0.008f, 0.012f, light, wood, 9f);
            var ivyPath = new Vector3[10];
            for (int i = 0; i < ivyPath.Length; i++)
            {
                float a = i * 0.9f + (s > 0 ? 1f : 0f);
                ivyPath[i] = V(0.36f * s + MathF.Sin(a) * 0.115f, 0.72f - i * 0.065f, MathF.Cos(a) * 0.115f);
            }
            b.Tube(leg, Smooth(2, ivyPath), 0.01f, 0.008f, PixelCanvas.Shadow(ivy, 0.2f), Leaf, 0.004f);
            for (int i = 1; i < ivyPath.Length; i += 2)
            {
                var n = Vector3.Normalize(ivyPath[i] - V(0.36f * s, ivyPath[i].Y, 0));
                Frond(b, leg, ivyPath[i], ivyPath[i] + n * 0.05f + V(0, 0.02f, 0), 0.025f, ivy, V(0, 1f, 0) + n, 0.3f, Leaf);
            }
        });

        // The beam across the shoulders: a squared log, its ends showing rings, the rope twist along its front
        b.Box(Body, V(0, 0.86f, 0), V(0.56f, 0.085f, 0.11f), 0.03f, wood);
        PokeBuilder.Both(s =>
        {
            b.PaintEll(Body, V(0.56f * s, 0.86f, 0), V(0.02f, 0.06f, 0.08f), light);
            b.PaintTorus(Body, V(0.56f * s, 0.86f, 0), 0.04f, 0.006f, dark, V(0, 0, 90f));
            foreach (var (x, y) in new[] { (0.36f, 0.95f), (0.46f, 0.96f), (0.24f, 0.96f) })
                C10Shaving(b, Body, V(x * s, y + 0.01f, -0.04f), 0.04f, V(80f, 30f * s, 0f));
        });
        C10Cord(b, Body, new[] { V(-0.53f, 0.9f, 0.11f), V(0.53f, 0.9f, 0.11f) }, 0.01f, 0.014f, light, wood, 26f);
        b.PaintEll(Body, V(0, 0.81f, 0.11f), V(0.6f, 0.012f, 0.03f), dark);

        // The head: a little shingled roof over the beam's middle, and under it the carved sun with its face
        int head = b.Head(V(0, 0.9f, 0));
        b.Box(head, V(0, 1.0f, -0.02f), V(0.09f, 0.08f, 0.08f), 0.01f, light, V(0, 0, 45f));
        PokeBuilder.Both(s =>
        {
            b.Box(head, V(0.085f * s, 1.065f, -0.02f), V(0.13f, 0.016f, 0.11f), 0.008f, shingle, V(0, 0, -38f * s));
            for (int i = 0; i < 3; i++)
                b.PaintEll(head, V((0.04f + i * 0.045f) * s, 1.1f - i * 0.035f, -0.02f), V(0.006f, 0.01f, 0.12f), PixelCanvas.Shadow(shingle, 0.3f), V(0, 0, -38f * s));
        });
        var c = V(0, 0.87f, 0.12f);
        var sr = V(0.14f, 0.14f, 0.04f);
        for (int i = 0; i < 14; i++)
        {
            float a = i * MathF.Tau / 14f;
            var d = V(MathF.Sin(a), MathF.Cos(a), 0);
            Blade(b, head, c + d * 0.11f + V(0, 0, -0.01f), c + d * 0.2f + V(0, 0, -0.01f), 0.035f, sun, V(0, 0, 1f), 0.4f, Shell);
        }
        b.Ell(head, c, sr, sun, blend: 0.01f);
        b.PaintTorus(head, c + V(0, 0, 0.02f), 0.125f, 0.008f, PixelCanvas.Shadow(sun, 0.25f), V(90f, 0, 0));
        b.Ell(head, c + V(0, -0.02f, 0.03f), V(0.02f, 0.03f, 0.02f), sun, blend: 0.015f);
        b.Mark(head, Out(c, sr, default, V(0, -0.5f, 1f)), V(0, -0.5f, 1f), 0.04f, 0.02f, PixelCanvas.Shadow(sun, 0.45f), MarkShape.Smile);
        PokeBuilder.Both(s =>
        {
            var look = V(0.45f * s, 0.25f, 1f);
            b.Eye(head, Out(c, sr, default, look), look, 0.028f, Rgb(236, 196, 120), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Calusel, Calusar

    /// <summary>
    /// The căluș dancers: hares in white embroidered shirts with red sashes, bells tied at the ankles and a stick in
    /// hand. Calusel is small and springy, its stick short; Calusar is tall and athletic in white trousers and booted
    /// legs hung with bells, red sashes crossed over its chest, a felt hat crowned with flowers and streaming ribbons,
    /// and a long decorated staff.
    /// </summary>
    private static PokeBuilder C10Calus(bool grown)
    {
        float k = grown ? 1f : 0.62f;
        var b = new PokeBuilder(grown ? "Calusar" : "Calusel", grown ? 0.88f : 0.55f, BodyPlan.Biped, V(0, 0.62f * k, 0)) { Coat = Fur };
        var fur = Rgb(216, 172, 110);
        var cream = Rgb(244, 226, 190);
        var shirt = Rgb(246, 246, 244);
        var stitch = Rgb(132, 30, 46);
        var red = Rgb(214, 40, 40);
        var stick = Rgb(110, 70, 42);
        var boot = Rgb(92, 60, 40);

        // Legs: long hind feet, white trousers on the grown one, bells at the ankles
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s * k, 0.46f * k, 0));
            var knee = V(0.09f * s * k, 0.26f * k, 0.03f * k);
            var ankle = V(0.09f * s * k, 0.07f * k, -0.01f * k);
            var cloth = grown ? shirt : fur;
            b.Limb(leg, V(0.07f * s * k, 0.46f * k, 0), knee, 0.075f * k, 0.06f * k, cloth);
            b.Limb(leg, knee, ankle, grown ? 0.06f * k : 0.05f * k, 0.045f * k, grown ? boot : fur);
            b.Ell(leg, V(0.095f * s * k, 0.03f * k, 0.05f * k), V(0.05f * k, 0.032f * k, 0.1f * k), grown ? boot : cream);
            if (grown)
            {
                b.PaintEll(leg, V(0.09f * s, 0.12f, 0), V(0.08f, 0.012f, 0.08f), Rgb(60, 40, 28));
                b.PaintEll(leg, V(0.09f * s, 0.18f, 0.01f), V(0.08f, 0.012f, 0.08f), Rgb(60, 40, 28));
            }
            // A ring of bells round the ankle (two rings on the grown one)
            foreach (float y in grown ? new[] { 0.1f, 0.19f } : new[] { 0.1f })
                for (int i = 0; i < 5; i++)
                {
                    float a = (i - 2f) * 0.6f;
                    float rr = (grown ? 0.058f : 0.05f) * k + 0.012f;
                    C10Bell(b, leg, V(0.09f * s * k + MathF.Sin(a) * rr, y * k, MathF.Cos(a) * rr), grown ? 0.016f : 0.014f);
                }
        });
        // The shirt: a tunic to the hips, embroidered at the hem and the collar, a red sash at the waist
        b.Ell(Body, V(0, 0.6f * k, 0), V(0.14f * k, 0.18f * k, 0.11f * k), shirt);
        b.Ell(Body, V(0, 0.46f * k, 0), V(0.15f * k, 0.07f * k, 0.12f * k), shirt, blend: 0.03f);
        b.PaintTorus(Body, V(0, 0.415f * k, 0), 0.148f * k, 0.01f * k, stitch, sz: 0.8f);
        b.Torus(Body, V(0, 0.52f * k, 0), 0.137f * k, 0.025f * k, red, sz: 0.82f, mat: SurfaceMaterial.Cloth, blend: 0.01f);
        Ribbon(b, Body, Smooth(2, V(0.12f * k, 0.52f * k, 0.06f * k), V(0.2f * k, 0.46f * k, 0.03f * k), V(0.26f * k, 0.38f * k, 0.0f)), 0.03f * k, red, V(0, 0, 1f), 0.008f);
        b.Mark(Body, V(0, 0.73f * k, 0.105f * k), V(0, 0.2f, 1f), 0.035f * k, 0.03f * k, stitch, MarkShape.Diamond);
        if (grown)
        {
            // Two red sashes crossed over the chest, tasselled at the waist
            PokeBuilder.Both(s => b.Tube(Body, new[] { V(-0.12f * s, 0.76f, 0.075f), V(-0.03f * s, 0.65f, 0.115f), V(0.07f * s, 0.55f, 0.11f), V(0.12f * s, 0.5f, 0.07f) },
                0.016f, 0.016f, red, SurfaceMaterial.Cloth, 0.004f));
            PokeBuilder.Both(s => b.Spike(Body, V(0.1f * s, 0.5f, 0.1f), V(0.11f * s, 0.4f, 0.11f), 0.022f, red, blend: 0.006f));
        }
        // Sleeves with embroidered cuffs, paws; the right one holds the stick (or the staff, upright beside it)
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.13f * s * k, 0.7f * k, 0));
            var elbow = V(0.22f * s * k, 0.6f * k, 0.03f * k);
            var hand = grown ? V(0.27f * s, 0.52f, 0.08f) : (s > 0 ? V(0.24f * s * k, 0.86f * k, 0.04f * k) : V(0.3f * s * k, 0.6f * k, 0.06f * k));
            b.Limb(arm, V(0.13f * s * k, 0.7f * k, 0), elbow, 0.05f * k, 0.048f * k, shirt);
            b.Limb(arm, elbow, hand, 0.048f * k, 0.046f * k, shirt);
            b.PaintEll(arm, Vector3.Lerp(elbow, hand, 0.85f), V(0.06f * k, 0.03f * k, 0.06f * k), stitch, Euler(hand - elbow));
            b.Ell(arm, hand + Vector3.Normalize(hand - elbow) * 0.04f * k, V(0.04f * k, 0.04f * k, 0.04f * k), fur);
            var grip = hand + Vector3.Normalize(hand - elbow) * 0.04f * k;
            if (grown && s < 0)
            {
                b.Limb(arm, V(grip.X, 0.06f, grip.Z + 0.02f), V(grip.X, 1.02f, grip.Z - 0.04f), 0.02f, 0.018f, stick, Leaf, 0.004f);
                b.PaintTorus(arm, V(grip.X, 0.8f, grip.Z - 0.03f), 0.02f, 0.006f, Rgb(220, 196, 140));
                b.PaintTorus(arm, V(grip.X, 0.3f, grip.Z + 0.005f), 0.02f, 0.006f, Rgb(220, 196, 140));
                Ribbon(b, arm, Smooth(2, V(grip.X, 0.86f, grip.Z - 0.03f), V(grip.X - 0.03f, 0.76f, grip.Z), V(grip.X - 0.04f, 0.66f, grip.Z + 0.02f)), 0.016f, Rgb(70, 150, 70), V(0, 0, 1f), 0.006f);
            }
            else if (!grown && s > 0)
                b.Limb(arm, grip + V(-0.04f * k, -0.08f * k, 0.0f), grip + V(0.05f * k, 0.14f * k, 0.0f), 0.018f, 0.016f, stick, Leaf, 0.008f);
        });
        // A short tail of cream fur
        int tail = b.Tail(V(0, 0.46f * k, -0.11f * k));
        b.Ell(tail, V(0, 0.46f * k, -0.14f * k), V(0.045f * k, 0.045f * k, 0.045f * k), cream);

        // The head: a hare's, long ears with dark tips and a curl painted inside, a cream muzzle and a pink nose
        int head = b.Head(V(0, 0.78f * k, 0));
        var c = V(0, 0.9f * k, 0.02f * k);
        var hr = V(0.1f * k, 0.095f * k, 0.09f * k);
        b.Limb(head, V(0, 0.74f * k, 0), c + V(0, -0.04f * k, 0), 0.06f * k, 0.055f * k, fur);
        b.Ell(head, c, hr, fur);
        PokeBuilder.Both(s => b.Ell(head, c + V(0.07f * s * k, -0.04f * k, 0.02f * k), V(0.05f * k, 0.04f * k, 0.05f * k), fur, blend: 0.03f));
        b.Ell(head, c + V(0, -0.035f * k, 0.07f * k), V(0.05f * k, 0.04f * k, 0.04f * k), cream, blend: 0.025f);
        b.Ell(head, c + V(0, -0.015f * k, 0.105f * k), V(0.014f * k, 0.011f * k, 0.01f * k), Rgb(222, 120, 124), blend: 0.006f);
        b.Mark(head, c + V(0, -0.06f * k, 0.1f * k), V(0, -0.4f, 1f), 0.024f * k, 0.012f * k, Rgb(90, 50, 40), MarkShape.Smile);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.05f * s * k, 0.07f * k, -0.02f * k));
            var root = c + V(0.05f * s * k, 0.07f * k, -0.02f * k);
            var tip = root + V((grown ? 0.07f : 0.13f) * s * k, 0.3f * k, -0.03f * k);
            b.Ell(ear, (root + tip) / 2f, V(0.045f * k, 0.17f * k, 0.022f * k), fur, Euler(tip - root));
            b.PaintEll(ear, Vector3.Lerp(root, tip, 0.5f) + V(0, 0, 0.02f * k), V(0.03f * k, 0.12f * k, 0.02f * k), Rgb(232, 168, 150), Euler(tip - root));
            b.PaintEll(ear, Vector3.Lerp(root, tip, 0.92f), V(0.05f * k, 0.05f * k, 0.04f * k), Rgb(84, 60, 40));
            var look = V(0.5f * s, 0.12f, 1f);
            b.Eye(head, Out(c, hr, default, look), look, 0.024f * k, Rgb(120, 76, 40), glare: grown);
        });
        if (grown)
        {
            // A black felt hat with a band of flowers, ribbons streaming from it
            var hatC = c + V(0, 0.085f, -0.005f);
            b.Ell(head, hatC, V(0.15f, 0.018f, 0.14f), Rgb(40, 34, 36), mat: SurfaceMaterial.Cloth, blend: 0.01f);
            b.Limb(head, hatC, hatC + V(0, 0.07f, 0), 0.09f, 0.085f, Rgb(46, 40, 42), SurfaceMaterial.Cloth, 0.01f);
            var flowers = new[] { Rgb(220, 40, 46), Rgb(60, 110, 210), Rgb(240, 200, 60), Rgb(220, 40, 46), Rgb(250, 250, 250) };
            for (int i = 0; i < 7; i++)
            {
                float a = (i - 3f) * 0.38f;
                b.Ell(head, hatC + V(MathF.Sin(a) * 0.092f, 0.03f, MathF.Cos(a) * 0.088f), V(0.022f, 0.022f, 0.016f), flowers[i % flowers.Length], blend: 0.004f);
            }
            var ribbons = new[] { Rgb(220, 40, 46), Rgb(70, 150, 70), Rgb(246, 244, 240) };
            PokeBuilder.Both(s =>
            {
                for (int i = 0; i < 3; i++)
                {
                    var from = hatC + V(0.13f * s, 0.0f, -0.03f * i);
                    Ribbon(b, head, Smooth(2, from, from + V(0.04f * s, -0.12f, -0.02f), from + V(0.07f * s + 0.02f * i * s, -0.28f + 0.03f * i, -0.06f)), 0.022f,
                        ribbons[i], V(s, 0, 0.4f), 0.006f);
                }
            });
        }
        return b;
    }

    // ------------------------------------------------------------------ Horuta

    /// <summary>
    /// Horuta: three little round cream dancers in embroidered vests, red, yellow and blue, holding hands in a ring and
    /// stepping round the hora. The front one looks out with open eyes; the two behind dance with theirs shut, smiling.
    /// </summary>
    private static PokeBuilder C10Horuta()
    {
        var b = new PokeBuilder("Horuta", 0.7f, BodyPlan.Biped, V(0, 0.2f, 0.2f)) { Coat = Fur };
        var cream = Rgb(244, 230, 200);
        var inner = Rgb(206, 160, 130);
        var vests = new[] { Rgb(170, 36, 40), Rgb(234, 186, 40), Rgb(40, 70, 170) };
        var trim = Rgb(232, 196, 96);
        const float Ring = 0.22f;
        var angles = new[] { 0f, 120f * Degree, -120f * Degree };
        var hands = new Vector3[3];
        for (int i = 0; i < 3; i++)
        {
            float a = angles[i] + 60f * Degree;
            hands[i] = V(MathF.Sin(a) * Ring * 1.05f, 0.22f, MathF.Cos(a) * Ring * 1.05f);
        }
        for (int i = 0; i < 3; i++)
        {
            float a = angles[i];
            var at = V(MathF.Sin(a) * Ring, 0, MathF.Cos(a) * Ring);
            var outward = Vector3.Normalize(V(at.X, 0, at.Z));
            var look = Vector3.Normalize(outward + V(0, 0, 1.3f));
            int bone = i == 0 ? Body : b.Part("dancer" + i, Root, at + V(0, 0.2f, 0), PokeRole.Segment, i);
            // Stubby legs, one stepping
            PokeBuilder.Both(s =>
            {
                var side = Vector3.Normalize(Vector3.Cross(V(0, 1f, 0), look)) * s;
                var hip = at + side * 0.05f + V(0, 0.1f, 0);
                var foot = at + side * 0.06f + look * (s > 0 ? 0.03f : -0.01f) + V(0, 0.03f, 0);
                b.Limb(bone, hip, foot, 0.035f, 0.032f, cream);
                b.Ell(bone, foot + look * 0.015f + V(0, -0.005f, 0), V(0.035f, 0.025f, 0.04f), cream, blend: 0.012f);
            });
            // A round body in its vest, a round head
            var bc = at + V(0, 0.2f, 0);
            b.Ell(bone, bc, V(0.1f, 0.1f, 0.095f), cream);
            b.PaintEll(bone, bc + V(0, 0.02f, 0), V(0.11f, 0.075f, 0.11f), vests[i]);
            b.PaintEll(bone, bc + look * 0.09f + V(0, 0.03f, 0), V(0.03f, 0.05f, 0.03f), cream);
            b.PaintEll(bone, bc + V(0, -0.055f, 0), V(0.12f, 0.008f, 0.12f), trim);
            b.Mark(bone, bc + look * 0.085f + Vector3.Normalize(Vector3.Cross(V(0, 1f, 0), look)) * 0.055f + V(0, 0.0f, 0), look, 0.018f, 0.018f, trim, MarkShape.Star);
            var hc = bc + V(0, 0.15f, 0);
            var hr = V(0.08f, 0.072f, 0.075f);
            b.Ell(bone, hc, hr, cream);
            PokeBuilder.Both(s =>
            {
                var side = Vector3.Normalize(Vector3.Cross(V(0, 1f, 0), look)) * s;
                b.Ell(bone, hc + side * 0.06f + V(0, 0.06f, 0) - look * 0.01f, V(0.03f, 0.03f, 0.018f), cream, blend: 0.01f);
                b.PaintEll(bone, hc + side * 0.062f + V(0, 0.062f, 0) + look * 0.008f, V(0.016f, 0.016f, 0.012f), inner);
            });
            // The face, turned between its outward way and the front
            var nose = Out(hc, hr, default, look);
            b.Ell(bone, nose - look * 0.004f + V(0, -0.01f, 0), V(0.008f, 0.006f, 0.006f), Rgb(90, 60, 50), blend: 0.004f);
            var mouthDir = Vector3.Normalize(look + V(0, -0.45f, 0));
            b.Mark(bone, Out(hc, hr, default, mouthDir), mouthDir, 0.016f, 0.012f, Rgb(170, 70, 70), MarkShape.Smile);
            var sideways = Vector3.Normalize(Vector3.Cross(V(0, 1f, 0), look));
            PokeBuilder.Both(s =>
            {
                var eyeDir = Vector3.Normalize(look + sideways * 0.42f * s + V(0, 0.15f, 0));
                var cheek = Vector3.Normalize(look + sideways * 0.6f * s + V(0, -0.25f, 0));
                b.Mark(bone, Out(hc, hr, default, cheek), cheek, 0.014f, 0.009f, Rgb(240, 160, 150));
                if (i == 0) b.Eye(bone, Out(hc, hr, default, eyeDir), eyeDir, 0.018f, Rgb(50, 36, 34));
                else b.Mark(bone, Out(hc, hr, default, eyeDir), eyeDir, 0.014f, 0.009f, Rgb(60, 40, 36), MarkShape.Smile, 180f);
            });
            // Arms out to the hands held between it and each neighbour
            foreach (var hand in new[] { hands[i], hands[(i + 2) % 3] })
            {
                var shoulder = bc + Vector3.Normalize(V(hand.X - bc.X, 0, hand.Z - bc.Z)) * 0.07f + V(0, 0.03f, 0);
                b.Limb(bone, shoulder, hand, 0.03f, 0.028f, cream);
            }
            b.Ell(bone, hands[i], V(0.032f, 0.03f, 0.032f), cream, blend: 0.01f);
        }
        return b;
    }

    // ------------------------------------------------------------------ Doinel, Doinatoare

    /// <summary>Doinel: a little sad ghost, a pale blue teardrop with a long trailing tail, big melancholy eyes and a mouth open in a quiet song.</summary>
    private static PokeBuilder C10Doinel()
    {
        var b = new PokeBuilder("Doinel", 0.45f, BodyPlan.Floating, V(0, 0.36f, 0)) { Coat = Fur }.Hover();
        var blue = Rgb(184, 218, 236);
        var deep = Rgb(120, 160, 200);
        var c = V(0, 0.38f, 0);
        var r = V(0.14f, 0.15f, 0.13f);
        b.Ell(Body, c, r, blue);
        // The tip of the teardrop, drawn up and back into a wisp
        b.Tube(Body, Smooth(3, c + V(0.02f, 0.1f, -0.03f), c + V(0.06f, 0.2f, -0.07f), c + V(0.14f, 0.26f, -0.06f)), 0.06f, 0.014f, blue, blend: 0.03f);
        // Little arm nubs
        PokeBuilder.Both(s => b.Ell(Body, c + V(0.12f * s, -0.04f, 0.04f), V(0.035f, 0.055f, 0.03f), blue, V(0, 0, -25f * s), blend: 0.02f));
        // A long tail trailing from its foot, curling round
        int tail = b.Tail(c + V(0, -0.12f, -0.02f));
        b.Tube(tail, Smooth(3, c + V(0, -0.1f, -0.02f), c + V(0.06f, -0.2f, -0.08f), c + V(0.2f, -0.24f, -0.04f), c + V(0.22f, -0.25f, 0.1f),
            c + V(0.08f, -0.26f, 0.16f), c + V(-0.06f, -0.24f, 0.12f)), 0.06f, 0.012f, blue, blend: 0.02f);
        b.PaintEll(tail, c + V(0.2f, -0.26f, 0.02f), V(0.06f, 0.03f, 0.1f), deep);
        // Sad brows, big eyes, a small round mouth
        PokeBuilder.Both(s =>
        {
            var look = V(0.36f * s, 0.12f, 1f);
            b.Eye(Body, Out(c, r, default, look), look, 0.032f, Rgb(70, 110, 150));
            b.Mark(Body, Out(c, r, default, V(0.36f * s, 0.42f, 1f)), V(0.36f * s, 0.42f, 1f), 0.03f, 0.006f, deep, MarkShape.Bar, -20f * s);
        });
        b.Mark(Body, Out(c, r, default, V(0, -0.32f, 1f)), V(0, -0.32f, 1f), 0.012f, 0.014f, Rgb(70, 60, 90));
        return Lift(b);
    }

    /// <summary>
    /// Doinatoare: the lament singer, tall and mournful, a long flowing body like a veil of dusk-blue mist curling to a
    /// tail, a dark embroidered headscarf knotted under a pale face, eyes closed as it sings, hands at its breast, and a
    /// ribbon of pale light spiralling up out of its mouth like a line of melody.
    /// </summary>
    private static PokeBuilder C10Doinatoare()
    {
        var b = new PokeBuilder("Doinatoare", 0.82f, BodyPlan.Floating, V(0, 0.8f, 0)) { Coat = Fur }.Hover();
        var veil = Rgb(98, 146, 160);
        var pale = PixelCanvas.Light1(veil, 0.35f);
        var skin = Rgb(212, 228, 230);
        var scarf = Rgb(52, 62, 72);
        var light = Rgb(214, 240, 244);

        // The body: a long fall of mist from the shoulders, curling into a tail
        b.Ell(Body, V(0, 0.92f, 0), V(0.12f, 0.19f, 0.1f), veil);
        b.Ell(Body, V(0, 0.62f, 0.0f), V(0.13f, 0.28f, 0.11f), veil, blend: 0.04f);
        int tail = b.Tail(V(0, 0.36f, 0));
        b.Tube(tail, Smooth(3, V(0, 0.42f, 0), V(0.02f, 0.26f, -0.03f), V(0.12f, 0.16f, 0.0f), V(0.16f, 0.12f, 0.12f), V(0.06f, 0.1f, 0.18f)), 0.1f, 0.02f, veil, blend: 0.03f);
        for (int i = 0; i < 4; i++)
            b.PaintEll(Body, V(-0.06f + i * 0.04f, 0.6f, 0.08f), V(0.008f, 0.25f, 0.06f), pale);
        // A cape of veil hanging from the shoulders
        PokeBuilder.Both(s =>
        {
            Frond(b, Body, V(0.09f * s, 1.0f, -0.02f), V(0.22f * s, 0.62f, -0.04f), 0.07f, pale, V(s, 0, 0.5f), 0.18f, Fur, 0.02f);
            Frond(b, Body, V(0.05f * s, 1.0f, -0.06f), V(0.14f * s, 0.56f, -0.1f), 0.07f, pale, V(s * 0.4f, 0, -1f), 0.18f, Fur, 0.02f);
        });
        // Arms folded up to the breast
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.1f * s, 1.0f, 0));
            var elbow = V(0.13f * s, 0.86f, 0.06f);
            var hand = V(0.03f * s, 0.95f, 0.12f);
            b.Limb(arm, V(0.1f * s, 1.0f, 0.0f), elbow, 0.03f, 0.026f, veil);
            b.Limb(arm, elbow, hand, 0.026f, 0.022f, skin);
            b.Ell(arm, hand, V(0.022f, 0.032f, 0.018f), skin, blend: 0.01f);
        });
        // The head in its scarf, knotted under the chin
        int head = b.Head(V(0, 1.06f, 0));
        var c = V(0, 1.2f, 0.02f);
        var hr = V(0.07f, 0.09f, 0.07f);
        b.Ell(head, c + V(0, 0.015f, -0.015f), hr * 1.18f, scarf, mat: SurfaceMaterial.Cloth);
        b.Ell(head, c + V(0, -0.05f, -0.05f), V(0.11f, 0.08f, 0.08f), scarf, mat: SurfaceMaterial.Cloth, blend: 0.03f);
        b.Cut(head, c + V(0, -0.012f, 0.08f), V(0.058f, 0.08f, 0.06f));
        b.Ell(head, c + V(0, -0.008f, 0.0f), hr * 0.98f, skin, blend: 0.004f);
        b.Ell(head, c + V(0, -0.1f, 0.06f), V(0.025f, 0.02f, 0.02f), scarf, mat: SurfaceMaterial.Cloth, blend: 0.006f);
        PokeBuilder.Both(s => b.Spike(head, c + V(0.01f * s, -0.11f, 0.06f), c + V(0.05f * s, -0.17f, 0.08f), 0.02f, scarf, 0.4f, SurfaceMaterial.Cloth));
        b.PaintEll(head, c + V(0, 0.08f, 0.04f), V(0.04f, 0.012f, 0.05f), Rgb(110, 120, 130));
        PokeBuilder.Both(s =>
        {
            var look = V(0.38f * s, 0.12f, 1f);
            b.Eye(head, Out(c, hr, default, look), look, 0.016f, Rgb(80, 100, 110), closed: true);
        });
        var mouth = Out(c, hr, default, V(0, -0.42f, 1f));
        b.PaintEll(head, mouth, V(0.012f, 0.014f, 0.012f), Rgb(50, 60, 80));
        // The song: a ribbon of light spiralling up out of its mouth
        int song = b.Part("song", head, mouth, PokeRole.Tail);
        b.Tube(song, Smooth(4, mouth + V(0, 0, -0.01f), mouth + V(0.06f, 0.01f, 0.08f), mouth + V(0.16f, 0.08f, 0.1f), mouth + V(0.24f, 0.18f, 0.04f),
            mouth + V(0.2f, 0.28f, -0.04f), mouth + V(0.12f, 0.3f, 0.02f), mouth + V(0.16f, 0.24f, 0.08f)), 0.016f, 0.006f, light, Glow, 0.006f);
        return Lift(b);
    }

    // ------------------------------------------------------------------ Naiet, Nailar

    /// <summary>A hollow reed: a pale golden-green tube from foot to top, banded at its joints and open at its top.</summary>
    private static void C10Reed(PokeBuilder b, int bone, Vector3 foot, Vector3 top, float r, Color color)
    {
        b.Limb(bone, foot, top, r, r, color, Leaf, 0.004f);
        var d = Vector3.Normalize(top - foot);
        float len = Vector3.Distance(foot, top);
        for (float t = 0.3f; t < 0.95f; t += 0.32f)
            b.PaintTorus(bone, Vector3.Lerp(foot, top, t), r * 1.02f, r * 0.14f, PixelCanvas.Shadow(color, 0.22f), Euler(d));
        b.Cut(bone, top + d * (r * 0.35f), V(r * 0.72f, Math.Min(len * 0.2f, r * 1.6f), r * 0.72f), Euler(d));
    }

    /// <summary>Naiet: a bundle of hollow reeds of different lengths bound with a band of green leaf, a small face on the band and two leaf feet.</summary>
    private static PokeBuilder C10Naiet()
    {
        var b = new PokeBuilder("Naiet", 0.45f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Leaf };
        var green = Rgb(110, 172, 70);
        var reeds = new[] { Rgb(210, 186, 120), Rgb(196, 200, 120), Rgb(222, 196, 140), Rgb(214, 168, 100), Rgb(186, 196, 126) };
        // A core of pith in the bundle's middle, so no hollow is shut in between the reeds under the band
        b.Limb(Body, V(0, 0.09f, 0.005f), V(0, 0.3f, 0.005f), 0.055f, 0.055f, PixelCanvas.Shadow(reeds[0], 0.3f), Leaf);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.1f, 0));
            b.Limb(leg, V(0.05f * s, 0.12f, 0), V(0.05f * s, 0.04f, 0.01f), 0.022f, 0.02f, PixelCanvas.Shadow(green, 0.15f));
            Frond(b, leg, V(0.05f * s, 0.02f, -0.02f), V(0.06f * s, 0.018f, 0.08f), 0.03f, green, V(0, 1f, 0), 0.3f, Leaf);
        });
        var spots = new[] { (0f, 0f, 0.66f), (-0.06f, 0.02f, 0.56f), (0.06f, 0.025f, 0.6f), (-0.03f, -0.05f, 0.62f), (0.035f, -0.05f, 0.52f),
            (-0.075f, -0.04f, 0.48f), (0.08f, -0.03f, 0.5f), (-0.02f, 0.06f, 0.5f), (0.025f, 0.06f, 0.46f) };
        for (int i = 0; i < spots.Length; i++)
        {
            var (x, z, top) = spots[i];
            C10Reed(b, Body, V(x, 0.1f, z), V(x, top, z), 0.034f, reeds[i % reeds.Length]);
        }
        // The band of leaf round the middle, its ends tied off in two leaves at the side
        var c = V(0, 0.3f, 0.01f);
        var br = V(0.125f, 0.075f, 0.115f);
        b.Ell(Body, c, br, green);
        b.PaintEll(Body, c + V(0.02f, 0.02f, 0.11f), V(0.1f, 0.006f, 0.03f), PixelCanvas.Shadow(green, 0.2f), V(0, 0, 18f));
        b.Ell(Body, c + V(0.12f, 0.005f, 0.01f), V(0.025f, 0.025f, 0.025f), green, blend: 0.01f);
        Frond(b, Body, c + V(0.1f, 0.01f, 0.01f), c + V(0.2f, 0.07f, 0.03f), 0.028f, green, V(0, 0, 1f), 0.35f, Leaf);
        Frond(b, Body, c + V(0.1f, 0.0f, 0.01f), c + V(0.2f, -0.03f, 0.04f), 0.025f, green, V(0, 0, 1f), 0.35f, Leaf);
        PokeBuilder.Both(s =>
        {
            var look = V(0.3f * s, 0.1f, 1f);
            b.Eye(Body, Out(c, br, default, look), look, 0.017f, Rgb(50, 40, 30));
            b.Mark(Body, Out(c, br, default, V(0.5f * s, -0.15f, 1f)), V(0.5f * s, -0.15f, 1f), 0.014f, 0.008f, Rgb(232, 160, 110));
        });
        b.Mark(Body, Out(c, br, default, V(0, -0.15f, 1f)), V(0, -0.15f, 1f), 0.007f, 0.009f, Rgb(70, 50, 40));
        return b;
    }

    /// <summary>
    /// Nailar: the panpipe, a graceful bird of reeds: broad wings each a row of golden-brown pipes falling away in
    /// length under a carved green band, a body bundled in leaf, a long neck of reed stems to a small head in a leaf
    /// hood with a sprig on top, long green legs and a tail of trailing leaves.
    /// </summary>
    private static PokeBuilder C10Nailar()
    {
        var b = new PokeBuilder("Nailar", 0.82f, BodyPlan.Bird, V(0, 0.6f, 0)) { Coat = Leaf };
        var gold = Rgb(204, 150, 74);
        var green = Rgb(84, 150, 66);
        var stem = Rgb(176, 196, 110);
        var beak = Rgb(196, 140, 80);

        // Long legs with three toes forward
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.5f, 0));
            var knee = V(0.08f * s, 0.27f, 0.03f);
            b.Limb(leg, V(0.06f * s, 0.52f, 0), knee, 0.03f, 0.022f, green);
            b.Limb(leg, knee, V(0.09f * s, 0.04f, -0.01f), 0.022f, 0.018f, green);
            Digits(b, leg, V(0.09f * s, 0.025f, 0.0f), V(0, -0.1f, 1f), V(0.5f, 0, 0), 0.07f, 0.012f, green);
            b.Limb(leg, V(0.09f * s, 0.025f, 0.0f), V(0.09f * s, 0.015f, -0.05f), 0.011f, 0.009f, green);
        });
        // The body: a short bundle of reeds wrapped in leaf
        for (int i = 0; i < 7; i++)
        {
            float a = i * MathF.Tau / 7f;
            C10Reed(b, Body, V(MathF.Sin(a) * 0.06f, 0.48f, MathF.Cos(a) * 0.05f), V(MathF.Sin(a) * 0.07f, 0.72f + 0.04f * (i % 3), MathF.Cos(a) * 0.06f), 0.03f,
                i % 2 == 0 ? gold : stem);
        }
        b.Ell(Body, V(0, 0.58f, 0), V(0.12f, 0.1f, 0.1f), green);
        b.PaintEll(Body, V(0, 0.6f, 0.1f), V(0.12f, 0.006f, 0.03f), PixelCanvas.Shadow(green, 0.25f), V(0, 0, -15f));
        // The wings: a carved band out from each shoulder, a row of pipes hanging from it, longer toward the tip
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.08f * s, 0.68f, -0.02f);
            var end = V(0.52f * s, 0.8f, -0.06f);
            int wing = b.Wing(s, shoulder);
            b.Limb(wing, shoulder, end, 0.03f, 0.026f, green, Leaf, 0.01f);
            b.PaintEll(wing, Vector3.Lerp(shoulder, end, 0.5f) + V(0, 0, 0.028f), V(0.2f, 0.008f, 0.01f), PixelCanvas.Light1(green, 0.25f), Euler(end - shoulder) + V(0, 0, 90f));
            for (int i = 0; i < 8; i++)
            {
                float t = 0.12f + i * 0.12f;
                var at = Vector3.Lerp(shoulder, end, t);
                float len = 0.16f + 0.24f * t;
                C10Reed(b, wing, at + V(0, -len, 0.0f), at + V(0, 0.06f, 0), 0.026f, i % 2 == 0 ? gold : PixelCanvas.Light1(gold, 0.12f));
            }
        });
        // A tail of long trailing leaves
        int tail = b.Tail(V(0, 0.52f, -0.08f));
        for (int i = -2; i <= 2; i++)
            LongLeaf(b, tail, V(0.02f * i, 0.52f, -0.08f), V(0.05f * i, 0.42f, -0.22f), V(0.09f * i, 0.26f, -0.32f - 0.03f * (2 - Math.Abs(i))), 0.03f, i % 2 == 0 ? green : stem, 0.01f);
        // The neck: two reed stems bound together, rising to a small head
        int neck = b.Part("neck", Body, V(0, 0.72f, 0.02f), PokeRole.Segment);
        b.Tube(neck, Smooth(3, V(0, 0.72f, 0.02f), V(0, 0.9f, 0.05f), V(0, 1.05f, 0.04f)), 0.03f, 0.026f, stem, Leaf, 0.008f);
        b.Tube(neck, Smooth(3, V(0.025f, 0.72f, 0.0f), V(0.03f, 0.9f, 0.02f), V(0.02f, 1.03f, 0.02f)), 0.018f, 0.016f, gold, Leaf, 0.004f);
        int head = b.Head(V(0, 1.06f, 0.04f), neck);
        var c = V(0, 1.11f, 0.06f);
        var hr = V(0.055f, 0.058f, 0.06f);
        // The leaf hood over the back of the head, then the head before it, a beak and a sprig on top
        b.Ell(head, c + V(0, 0.015f, -0.02f), V(0.068f, 0.07f, 0.064f), green);
        b.Spike(head, c + V(0, 0.05f, -0.02f), c + V(0, 0.12f, -0.04f), 0.035f, green, 0.4f);
        b.Ell(head, c + V(0, -0.005f, 0.012f), hr, Rgb(226, 196, 140), blend: 0.006f);
        b.Spike(head, c + V(0, -0.012f, 0.05f), c + V(0, -0.025f, 0.115f), 0.022f, beak, mat: Shell);
        b.Limb(head, c + V(0.02f, 0.08f, -0.04f), c + V(0.05f, 0.16f, -0.04f), 0.007f, 0.006f, stem, Leaf, 0.004f);
        Frond(b, head, c + V(0.05f, 0.15f, -0.04f), c + V(0.09f, 0.18f, -0.04f), 0.014f, green, V(0, 0, 1f), 0.3f, Leaf);
        PokeBuilder.Both(s =>
        {
            var look = V(0.6f * s, 0.15f, 1f);
            b.Eye(head, Out(c + V(0, -0.005f, 0.012f), hr, default, look), look, 0.017f, Rgb(60, 40, 30));
        });
        return b;
    }

    // ------------------------------------------------------------------ Coloanel, Coloanfinit

    /// <summary>Coloanel: two stacked rhomboid beads of polished golden-brown metal, the upper one a stern little face, on tiny stubby feet.</summary>
    private static PokeBuilder C10Coloanel()
    {
        var b = new PokeBuilder("Coloanel", 0.5f, BodyPlan.Biped, V(0, 0.18f, 0)) { Coat = Metal };
        var bronze = Rgb(186, 136, 72);
        var shade = PixelCanvas.Shadow(bronze, 0.22f);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.07f, 0));
            b.Box(leg, V(0.065f * s, 0.035f, 0.01f), V(0.03f, 0.035f, 0.035f), 0.008f, shade, V(0, 45f, 0));
        });
        C10Bead(b, Body, V(0, 0.2f, 0), 0.17f, 0.13f, bronze, shade);
        int head = b.Head(V(0, 0.33f, 0));
        var c = V(0, 0.46f, 0);
        C10Bead(b, head, c, 0.18f, 0.13f, bronze, shade);
        // The face on the upper cone: glowing eyes under a heavy brow
        PokeBuilder.Both(s =>
        {
            var at = c + V(0.065f * s, 0.035f, 0);
            float rr = 0.18f - 0.58f * 0.18f * 0.035f / 0.13f;
            at.Z = MathF.Sqrt(rr * rr - at.X * at.X);
            var look = V(0.3f * s, 0.55f, 1f);
            b.Eye(head, at, look, 0.022f, Rgb(170, 240, 250), sclera: false, white: Rgb(226, 252, 255));
            b.Mark(head, at + V(0, 0.035f, -0.02f), V(0.3f * s, 0.6f, 1f), 0.03f, 0.007f, PixelCanvas.Shadow(bronze, 0.5f), MarkShape.Bar, 14f * s);
        });
        return b;
    }

    /// <summary>
    /// Coloanfinit: the endless column, a tall swaying stack of the same rhomboid beads, each on a bone of its own so
    /// it leans and sways, a pale psychic glow at every joint, a calm face on the third bead from the top and the
    /// topmost half-bead fading into light.
    /// </summary>
    private static PokeBuilder C10Coloanfinit()
    {
        var b = new PokeBuilder("Coloanfinit", 1f, BodyPlan.Serpent, V(0, 0.16f, 0)) { Coat = Metal };
        var bronze = Rgb(184, 134, 70);
        var shade = PixelCanvas.Shadow(bronze, 0.22f);
        var glow = Rgb(150, 236, 240);
        PokeBuilder.Both(s => b.Box(Body, V(0.06f * s, 0.025f, 0.01f), V(0.028f, 0.03f, 0.03f), 0.008f, shade, V(0, 45f, 0)));
        const int Beads = 7;
        float h = 0.105f, w = 0.15f;
        int bone = Body, faceBone = Body;
        Vector3 c = default;
        for (int i = 0; i < Beads; i++)
        {
            float lean = MathF.Sin(i * 0.7f) * 0.025f + i * 0.006f;
            c = V(lean, 0.16f + i * 2f * h * 0.93f, 0);
            if (i > 0)
            {
                bone = i == Beads - 3 ? b.Head(c - V(0, h, 0), bone) : b.Part("bead" + i, bone, c - V(0, h, 0), PokeRole.Segment, i * 0.6f);
                if (i == Beads - 3) faceBone = bone;
                b.Torus(bone, c - V(0, h * 0.95f, 0), w * 0.4f, 0.014f, glow, mat: Glow, blend: 0.006f);
            }
            C10Bead(b, bone, c, w, h, bronze, shade);
            if (i == Beads - 3)
            {
                PokeBuilder.Both(s =>
                {
                    var at = c + V(0.055f * s, 0.028f, 0);
                    float rr = w - 0.58f * w * 0.028f / h;
                    at.Z = MathF.Sqrt(rr * rr - at.X * at.X);
                    var look = V(0.3f * s, 0.55f, 1f);
                    b.Eye(bone, at, look, 0.02f, Rgb(160, 238, 248), white: Rgb(226, 252, 255));
                    b.Mark(bone, at + V(0, 0.032f, -0.018f), V(0.3f * s, 0.6f, 1f), 0.026f, 0.006f, PixelCanvas.Shadow(bronze, 0.5f), MarkShape.Bar, 10f * s);
                });
            }
        }
        // The last half-bead fading into light above it
        int top = b.Part("crown", bone, c + V(0, h, 0), PokeRole.Segment, Beads * 0.6f);
        var tc = c + V(0.02f, 2f * h * 0.93f - h * 0.3f, 0);
        b.Torus(top, c + V(0, h * 1.02f, 0), w * 0.4f, 0.014f, glow, mat: Glow, blend: 0.006f);
        b.Limb(top, tc - V(0, h * 0.7f, 0), tc, w * 0.42f, w * 0.9f, PixelCanvas.Light1(bronze, 0.35f), Metal, 0f);
        b.Ell(top, tc, V(w * 0.9f, h * 0.35f, w * 0.9f), Rgb(208, 236, 236), mat: Glow, blend: 0.03f);
        return b;
    }

    // ------------------------------------------------------------------ Tramvaiel, Tramvaior

    /// <summary>A tram's pantograph: a base on the roof and a diamond of arms up to a contact bar.</summary>
    private static void C10Pantograph(PokeBuilder b, int bone, Vector3 at, float k)
    {
        var steel = Rgb(150, 150, 156);
        b.Box(bone, at + V(0, 0.012f * k, 0), V(0.05f * k, 0.014f * k, 0.035f * k), 0.008f * k, steel, mat: Metal);
        var mid = at + V(0, 0.08f * k, -0.03f * k);
        var top = at + V(0, 0.15f * k, 0.0f);
        b.Limb(bone, at + V(0, 0.02f * k, 0.02f * k), mid, 0.009f * k, 0.008f * k, steel, Metal, 0.004f);
        b.Limb(bone, mid, top, 0.008f * k, 0.008f * k, steel, Metal, 0.004f);
        b.Limb(bone, at + V(0, 0.02f * k, -0.02f * k), mid + V(0, 0, 0.05f * k), 0.008f * k, 0.008f * k, steel, Metal, 0.004f);
        b.Limb(bone, mid + V(0, 0, 0.05f * k), top, 0.008f * k, 0.008f * k, steel, Metal, 0.004f);
        b.Limb(bone, top + V(-0.07f * k, 0, 0), top + V(0.07f * k, 0, 0), 0.01f * k, 0.01f * k, steel, Metal, 0.004f);
    }

    /// <summary>A tram car's body: a rounded box, cream above and red below with a dark band between.</summary>
    private static void C10Car(PokeBuilder b, int bone, Vector3 c, Vector3 half, float round)
    {
        var cream = Rgb(240, 228, 196);
        var red = Rgb(214, 54, 50);
        b.Box(bone, c, half, round, cream, mat: SurfaceMaterial.Plastic);
        float waist = c.Y - half.Y * 0.05f;
        b.PaintEll(bone, V(c.X, waist - 0.6f, c.Z), V(half.X * 3f, 0.6f, half.Z * 3f), red);
        b.PaintEll(bone, V(c.X, waist, c.Z), V(half.X * 3f, half.Y * 0.06f, half.Z * 3f), Rgb(70, 60, 60));
    }

    /// <summary>
    /// The trams: Tramvaiel is a small round toy tram, cream and red, its windows its eyes, a little bumper for a mouth,
    /// one pantograph on its roof and four wheels for feet. Tramvaior is the city line: a long car with a bumper and
    /// headlamps, a second car behind it on an articulated bellows, glowing windows along both, two pantographs.
    /// </summary>
    private static PokeBuilder C10Tram(bool grown)
    {
        float k = grown ? 1.2f : 1f;
        var b = new PokeBuilder(grown ? "Tramvaior" : "Tramvaiel", grown ? 1f : 0.55f, BodyPlan.Quadruped, V(0, 0.3f * k, 0)) { Coat = SurfaceMaterial.Plastic };
        var glass = Rgb(190, 222, 236);
        var dark = Rgb(70, 64, 66);
        var steel = Rgb(150, 150, 156);
        var lit = Rgb(252, 226, 120);
        var c = V(0, 0.32f * k, 0);
        var half = V(0.2f * k, 0.21f * k, grown ? 0.32f : 0.24f);
        c.Z = grown ? 0.08f : 0f;
        float frontZ = c.Z + half.Z;

        // Wheels: a pair under the front, a pair under the back
        foreach (var (z, isFront) in new[] { (c.Z + half.Z * 0.55f, true), (c.Z - half.Z * 0.55f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.15f * s * k, 0.12f * k, z), isFront);
                b.Ell(leg, V(0.16f * s * k, 0.07f * k, z), V(0.028f * k, 0.07f * k, 0.07f * k), dark, mat: Metal, blend: 0.006f);
                b.Ell(leg, V(0.185f * s * k, 0.07f * k, z), V(0.008f * k, 0.03f * k, 0.03f * k), steel, mat: Metal, blend: 0.004f);
                b.Limb(leg, V(0.13f * s * k, 0.07f * k, z), V(0.12f * s * k, 0.13f * k, z), 0.022f * k, 0.022f * k, dark, Metal, 0.004f);
            });
        C10Car(b, Body, c, half, 0.12f * k);
        // Side windows and a door, painted along both flanks (lit on the city line)
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < (grown ? 4 : 2); i++)
            {
                float z = frontZ - 0.15f * k - i * 0.08f * k;
                b.PaintEll(Body, V(half.X * s, c.Y + 0.08f * k, z), V(0.03f, 0.05f * k, 0.025f * k), grown ? lit : glass);
            }
            b.PaintEll(Body, V(half.X * s, c.Y - 0.02f * k, frontZ - 0.06f * k - (grown ? 0.32f * k : 0.16f * k)), V(0.03f, 0.13f * k, 0.03f * k), PixelCanvas.Shadow(Rgb(214, 54, 50), 0.25f));
        });
        // The face: two big windows for eyes, a lamp for a nose, a bumper for a mouth
        PokeBuilder.Both(s =>
        {
            b.PaintEll(Body, V(0.085f * s * k, c.Y + 0.08f * k, frontZ), V(0.07f * k, 0.075f * k, 0.03f), glass);
            b.Eye(Body, V(0.085f * s * k, c.Y + 0.075f * k, frontZ + 0.002f), V(0.15f * s, 0.05f, 1f), 0.034f * k, grown ? Rgb(200, 130, 40) : Rgb(110, 80, 50));
            if (grown)
            {
                b.Mark(Body, V(0.085f * s * k, c.Y + 0.16f * k, frontZ - 0.01f), V(0, 0.4f, 1f), 0.04f * k, 0.008f, Rgb(80, 60, 60), MarkShape.Bar, -12f * s);
                b.Ell(Body, V(0.12f * s * k, c.Y - 0.1f * k, frontZ - 0.005f), V(0.025f * k, 0.025f * k, 0.014f), lit, mat: Glow, blend: 0.006f);
            }
        });
        b.Ell(Body, V(0, c.Y - 0.01f * k, frontZ - 0.006f), V(0.02f * k, 0.02f * k, 0.014f), Rgb(250, 246, 230), mat: Glow, blend: 0.006f);
        b.Tube(Body, new[] { V(-0.1f * k, c.Y - 0.11f * k, frontZ + 0.012f), V(-0.06f * k, c.Y - 0.14f * k, frontZ + 0.018f), V(0.06f * k, c.Y - 0.14f * k, frontZ + 0.018f),
            V(0.1f * k, c.Y - 0.11f * k, frontZ + 0.012f) }, 0.02f * k, 0.02f * k, dark, Metal, 0.008f);
        if (grown)
        {
            b.Box(Body, V(0, c.Y - 0.18f * k, frontZ - 0.01f), V(0.17f * k, 0.022f * k, 0.03f), 0.012f, steel, mat: Metal, blend: 0.006f);
            b.Mark(Body, V(0, c.Y - 0.08f * k, frontZ + 0.004f), V(0, 0, 1f), 0.035f, 0.012f, dark, MarkShape.Bar);
        }
        C10Pantograph(b, Body, V(0, c.Y + half.Y - 0.01f, c.Z + 0.02f), k);

        if (grown)
        {
            // The bellows, and the second car behind it on wheels of its own, its own pantograph
            float back = c.Z - half.Z;
            int tail = b.Tail(V(0, c.Y, back));
            for (int i = 0; i < 4; i++)
                b.Ell(tail, V(0, c.Y, back + 0.01f - i * 0.03f), V(half.X * 0.86f, half.Y * 0.86f, 0.024f), dark, blend: 0.004f);
            var rc = V(0, c.Y, back - 0.08f - 0.24f);
            var rh = V(half.X, half.Y, 0.24f);
            C10Car(b, tail, rc, rh, 0.12f * k);
            PokeBuilder.Both(s =>
            {
                for (int i = 0; i < 4; i++)
                    b.PaintEll(tail, V(rh.X * s, rc.Y + 0.08f * k, rc.Z + 0.15f - i * 0.1f), V(0.03f, 0.05f * k, 0.03f), lit);
                b.Ell(tail, V(0.16f * s * k, 0.07f * k, rc.Z - 0.08f), V(0.028f * k, 0.07f * k, 0.07f * k), dark, mat: Metal, blend: 0.006f);
                b.Limb(tail, V(0.13f * s * k, 0.07f * k, rc.Z - 0.08f), V(0.12f * s * k, 0.14f * k, rc.Z - 0.08f), 0.022f * k, 0.022f * k, dark, Metal, 0.004f);
            });
            C10Pantograph(b, tail, V(0, rc.Y + rh.Y - 0.01f, rc.Z), k * 0.85f);
        }
        return b;
    }
}
