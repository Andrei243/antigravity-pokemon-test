using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Carpathia, batch 8 (plan 46 · CP4 to CP8): Hatzlet to Rapanel (20 species), sculpted from nothing after the briefs of
// docs/plans/decisions/CP-dex-3-river-and-caves.md and CP-dex-3b-river-and-caves-more.md and the user's reference
// pictures: Hațeg's fossil pterosaurs and dwarf sauropods, the mud volcanoes, the river's otters, beavers, pikes and
// crayfish, the delta's cormorant and water lilies, the Black Sea's jellyfish and the first sea snail. Each line is one
// builder by stage, so its palette and its motifs carry over as it grows.
internal static partial class PokemonModels
{
    private static string[] CarpathiaNames8() => new[]
    {
        "Hatzlet", "Hatzegon", "Minisaur", "Hategant", "Namolel", "Namolcan", "Vidrut", "Vidroar", "Castorel", "Castoroi",
        "Stiucuta", "Stiucon", "Racut", "Racoroi", "Cormoran", "Nufarel", "Nufarina", "Meduzel", "Meduzaur", "Rapanel"
    };

    private static PokeBuilder? CreateCarpathia8(string species) => species switch
    {
        "HATZLET" => Carp8Pterosaur(0),
        "HATZEGON" => Carp8Pterosaur(1),
        "MINISAUR" => Carp8Sauropod(0),
        "HATEGANT" => Carp8Sauropod(1),
        "NAMOLEL" => Carp8Namolel(),
        "NAMOLCAN" => Carp8Namolcan(),
        "VIDRUT" => Carp8Vidrut(),
        "VIDROAR" => Carp8Vidroar(),
        "CASTOREL" => Carp8Castorel(),
        "CASTOROI" => Carp8Castoroi(),
        "STIUCUTA" => Carp8Stiucuta(),
        "STIUCON" => Carp8Stiucon(),
        "RACUT" => Carp8Crayfish(0),
        "RACOROI" => Carp8Crayfish(1),
        "CORMORAN" => Carp8Cormoran(),
        "NUFAREL" => Carp8Nufarel(),
        "NUFARINA" => Carp8Nufarina(),
        "MEDUZEL" => Carp8Jelly(0),
        "MEDUZAUR" => Carp8Jelly(1),
        "RAPANEL" => Carp8Rapanel(),
        _ => null
    };

    /// <summary>The point where a ray from an ellipsoid's centre along <paramref name="dir"/> leaves its surface.</summary>
    private static Vector3 Carp8At(Vector3 center, Vector3 radii, Vector3 dir)
    {
        var d = Vector3.Normalize(dir);
        var q = d / radii;
        return center + d / q.Length();
    }

    /// <summary>The way an ellipsoid's surface faces where a ray along <paramref name="dir"/> leaves it.</summary>
    private static Vector3 Carp8Normal(Vector3 center, Vector3 radii, Vector3 dir) =>
        Vector3.Normalize((Carp8At(center, radii, dir) - center) / (radii * radii));

    // ------------------------------------------------------------------ Hatzlet line

    /// <summary>
    /// Hațeg's pterosaurs: a big head with a long pale beak and a crest of stone, walking on its folded wings like
    /// crutches; the giant's head heavier and its beak deeper, shards of rock on its head and shoulders.
    /// </summary>
    private static PokeBuilder Carp8Pterosaur(int stage)
    {
        bool big = stage == 1;
        float k = big ? 1.4f : 1f;
        var b = new PokeBuilder(big ? "Hatzegon" : "Hatzlet", big ? 0.96f : 0.6f, BodyPlan.Quadruped, V(0, 0.36f * k, 0)) { Coat = Scales };
        var sand = big ? Rgb(198, 150, 86) : Rgb(208, 172, 120);
        var wing = big ? Rgb(128, 90, 52) : Rgb(150, 112, 74);
        var stone = big ? Rgb(196, 182, 152) : Rgb(212, 202, 180);
        var beak = Rgb(224, 214, 192);
        var claw = Rgb(84, 68, 52);

        // Short hind legs, three claws on each foot
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s * k, 0.3f * k, -0.08f * k), front: false);
            b.Limb(leg, V(0.08f * s * k, 0.32f * k, -0.08f * k), V(0.12f * s * k, 0.17f * k, -0.04f * k), 0.045f * k, 0.035f * k, sand);
            b.Limb(leg, V(0.12f * s * k, 0.17f * k, -0.04f * k), V(0.12f * s * k, 0.04f * k, -0.03f * k), 0.035f * k, 0.028f * k, sand);
            b.Ell(leg, V(0.12f * s * k, 0.028f * k, 0.0f), V(0.04f * k, 0.028f * k, 0.05f * k), sand);
            Digits(b, leg, V(0.12f * s * k, 0.02f * k, 0.04f * k), V(0, -0.15f, 1f), V(0.45f * s, 0, 0), 0.04f * k, 0.011f * k, claw);
        });

        // A deep chest, the giant's barrel of a body
        b.Ell(Body, V(0, 0.36f * k, 0), V((big ? 0.14f : 0.12f) * k, 0.12f * k, 0.17f * k), sand);
        b.Ell(Body, V(0, 0.4f * k, 0.09f * k), V((big ? 0.13f : 0.11f) * k, 0.11f * k, 0.1f * k), sand);
        // Pale patches of the fossil on its back
        foreach (var (x, y, z, pr) in new[] { (0.05f, 0.47f, 0.02f, 0.035f), (-0.06f, 0.46f, -0.04f, 0.03f), (0.08f, 0.42f, -0.1f, 0.028f), (-0.03f, 0.48f, 0.08f, 0.025f) })
            b.PaintEll(Body, V(x * k, y * k, z * k), V(pr * k, pr * k * 0.8f, pr * k), stone);

        int tail = b.Tail(V(0, 0.36f * k, -0.15f * k));
        b.Limb(tail, V(0, 0.36f * k, -0.14f * k), V(0, 0.4f * k, -0.32f * k), 0.035f * k, 0.012f * k, sand);
        b.Ell(tail, V(0, 0.41f * k, -0.33f * k), V(0.008f * k, 0.03f * k, 0.035f * k), wing, mat: Shell);

        // The wings walked on: a long arm down to a clawed hand, the long finger folded up behind, the skin between
        PokeBuilder.Both(s =>
        {
            int arm = b.Leg(s, V(0.11f * s * k, 0.42f * k, 0.1f * k));
            var shoulder = V(0.1f * s * k, 0.42f * k, 0.1f * k);
            var elbow = V(0.19f * s * k, 0.25f * k, 0.09f * k);
            var wrist = V(0.21f * s * k, 0.06f * k, 0.17f * k);
            b.Limb(arm, shoulder, elbow, 0.045f * k, 0.035f * k, sand);
            b.Limb(arm, elbow, wrist, 0.035f * k, 0.028f * k, sand);
            b.Ell(arm, wrist + V(0, -0.025f * k, 0.01f * k), V(0.035f * k, 0.025f * k, 0.035f * k), sand);
            Digits(b, arm, wrist + V(0, -0.035f * k, 0.03f * k), V(0, -0.1f, 1f), V(0.5f * s, 0, 0), 0.035f * k, 0.01f * k, claw);
            var tip = V(0.2f * s * k, (big ? 0.58f : 0.54f) * k, -0.26f * k);
            Blade(b, arm, elbow + V(0, -0.04f * k, 0.02f * k), tip, 0.075f * k, wing, V(s, 0, 0), 0.22f);
            b.Ell(arm, V(0.16f * s * k, 0.32f * k, -0.06f * k), V(0.016f * k, 0.14f * k, 0.17f * k), wing, V(0, 0, 22f * s));
        });

        int head = b.Head(V(0, 0.46f * k, 0.15f * k));
        b.Limb(head, V(0, 0.42f * k, 0.12f * k), V(0, 0.56f * k, 0.2f * k), 0.06f * k, 0.05f * k, sand);
        var c = V(0, 0.62f * k, 0.22f * k);
        var r = big ? V(0.1f * k, 0.09f * k, 0.105f * k) : V(0.085f * k, 0.08f * k, 0.09f * k);
        b.Ell(head, c, r, sand);
        // The long toothless beak, the giant's deep and hooked at the end
        if (big)
        {
            b.Limb(head, V(0, 0.6f * k, 0.28f * k), V(0, 0.56f * k, 0.46f * k), 0.07f * k, 0.045f * k, beak, Shell);
            b.Limb(head, V(0, 0.56f * k, 0.46f * k), V(0, 0.5f * k, 0.55f * k), 0.045f * k, 0.014f * k, beak, Shell);
        }
        else b.Limb(head, V(0, 0.61f * k, 0.28f * k), V(0, 0.55f * k, 0.52f * k), 0.045f * k, 0.01f * k, beak, Shell);
        b.PaintEll(head, V(0, 0.585f * k, 0.4f * k), V(0.08f * k, 0.006f * k, 0.13f * k), Rgb(150, 132, 108), V(-12f, 0, 0), 0.006f);
        // The crest of stone, a lump on the chick and a crown of shards on the giant
        if (big)
        {
            foreach (var (x, z, h, rr) in new[] { (0f, 0.18f, 0.2f, 0.05f), (0.05f, 0.13f, 0.16f, 0.04f), (-0.05f, 0.13f, 0.16f, 0.04f), (0.03f, 0.24f, 0.13f, 0.035f), (-0.03f, 0.24f, 0.13f, 0.035f), (0f, 0.08f, 0.14f, 0.04f) })
                b.Spike(head, V(x * k, 0.66f * k, z * k), V(x * 1.4f * k, (0.66f + h) * k, (z - 0.06f) * k), rr * k, stone, mat: Shell);
            foreach (var (x, y, z, h) in new[] { (0.07f, 0.48f, 0.02f, 0.12f), (-0.07f, 0.48f, 0.02f, 0.12f), (0.0f, 0.5f, -0.05f, 0.14f), (0.09f, 0.45f, -0.08f, 0.09f), (-0.09f, 0.45f, -0.08f, 0.09f) })
                b.Spike(Body, V(x * k, y * k, z * k), V(x * 1.3f * k, (y + h) * k, (z - 0.04f) * k), 0.04f * k, stone, mat: Shell);
        }
        else
        {
            b.Ell(head, V(0, 0.7f * k, 0.18f * k), V(0.045f * k, 0.05f * k, 0.06f * k), stone, mat: Shell);
            b.Ell(head, V(0.015f * k, 0.74f * k, 0.15f * k), V(0.03f * k, 0.035f * k, 0.035f * k), stone, mat: Shell);
            b.Ell(head, V(-0.02f * k, 0.72f * k, 0.22f * k), V(0.025f * k, 0.03f * k, 0.03f * k), stone, mat: Shell);
        }
        PokeBuilder.Both(s => b.PaintEll(head, V(0.07f * s * k, 0.66f * k, 0.17f * k), V(0.025f * k, 0.02f * k, 0.025f * k), stone));
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.06f * s * k, 0.645f * k), V(0.8f * s, 0.15f, 0.55f), (big ? 0.019f : 0.022f) * k,
            Rgb(120, 96, 60), glare: big));
        b.Ground();
        return b;
    }

    // ------------------------------------------------------------------ Minisaur line

    /// <summary>
    /// Hațeg's dwarf sauropods: four stumpy pillars, a long neck and tail, stone plates with moss on the back; the grown
    /// one a small hillside of round scutes, moss and ferns.
    /// </summary>
    private static PokeBuilder Carp8Sauropod(int stage)
    {
        bool big = stage == 1;
        float k = big ? 1.3f : 1f;
        var b = new PokeBuilder(big ? "Hategant" : "Minisaur", big ? 0.95f : 0.62f, BodyPlan.Quadruped, V(0, 0.3f * k, 0)) { Coat = Scales };
        var green = big ? Rgb(136, 160, 94) : Rgb(150, 178, 98);
        var cream = Rgb(226, 228, 160);
        var stone = Rgb(150, 144, 128);
        var moss = big ? Rgb(84, 128, 52) : Rgb(98, 140, 58);
        var nail = Rgb(168, 156, 136);

        foreach (var (z, front) in new[] { (0.13f, true), (-0.14f, false) })
            PokeBuilder.Both(s =>
            {
                float x = 0.11f * s * k, zz = z * k;
                int leg = b.Leg(s, V(x, 0.26f * k, zz), front);
                b.Limb(leg, V(x, 0.26f * k, zz), V(x, 0.06f * k, zz + 0.01f * k), 0.058f * k, 0.052f * k, green);
                b.Ell(leg, V(x, 0.035f * k, zz + 0.015f * k), V(0.06f * k, 0.035f * k, 0.065f * k), green);
                foreach (float t in new[] { -1f, 0f, 1f })
                    b.Ell(leg, V(x + t * 0.03f * k, 0.022f * k, zz + 0.07f * k), V(0.016f * k, 0.018f * k, 0.014f * k), nail, mat: Shell, blend: 0.006f);
            });
        b.Ell(Body, V(0, 0.3f * k, 0), V(0.17f * k, 0.14f * k, 0.24f * k), green);
        b.PaintEll(Body, V(0, 0.19f * k, 0.03f * k), V(0.14f * k, 0.06f * k, 0.2f * k), cream);
        PokeBuilder.Both(s => b.PaintEll(Body, V(0.15f * s * k, 0.3f * k, 0.08f * k), V(0.03f * k, 0.03f * k, 0.03f * k), Rgb(122, 146, 84)));

        // The tail sweeps back and curls up
        int tail = b.Tail(V(0, 0.3f * k, -0.22f * k));
        var tailPath = Smooth(3, V(0, 0.3f, -0.2f) * k, V(0, 0.22f, -0.38f) * k, V(0, 0.2f, -0.55f) * k, V(0, 0.26f, -0.68f) * k, V(0, 0.35f, -0.73f) * k);
        b.Tube(tail, tailPath, 0.075f * k, 0.016f * k, green, blend: 0.02f);

        // The back: a few mossy stones on the young one, a mound of moss and round scutes on the grown one
        if (big)
        {
            var mc = V(0, 0.42f * k, -0.02f * k);
            var mr = V(0.17f * k, 0.13f * k, 0.22f * k);
            b.Ell(Body, mc, mr, moss, mat: Leaf);
            for (int ring = 0; ring < 3; ring++)
            {
                int n = ring == 0 ? 1 : ring * 6;
                for (int i = 0; i < n; i++)
                {
                    float up = 1f - ring * 0.34f;
                    float a = i * MathF.Tau / n + ring * 0.4f;
                    var dir = V(MathF.Cos(a) * (1f - up), up, MathF.Sin(a) * (1f - up) * 1.2f);
                    var at = Carp8At(mc, mr, dir);
                    float sz = (0.045f - ring * 0.004f) * k;
                    b.Ell(Body, at, V(sz, sz * 0.62f, sz), stone, Euler(Carp8Normal(mc, mr, dir)), Shell, 0.012f);
                }
            }
            // Ferns at its edges and moss hanging over its flanks
            foreach (var (x, z, lean) in new[] { (0.15f, 0.12f, 0.6f), (-0.15f, 0.1f, -0.6f), (0.14f, -0.16f, 0.5f), (-0.13f, -0.18f, -0.5f) })
                Carp8Fern(b, Body, V(x * 0.6f * k, 0.42f * k, z * k), V(lean, 1f, 0.2f), 0.13f * k, Rgb(78, 150, 58));
            foreach (var (x, z) in new[] { (0.16f, 0.0f), (-0.16f, -0.04f), (0.12f, -0.14f), (-0.11f, 0.12f) })
                b.Ell(Body, V(x * k, 0.34f * k, z * k), V(0.03f * k, 0.06f * k, 0.035f * k), moss, mat: Leaf, blend: 0.02f);
        }
        else
        {
            b.Ell(Body, V(0, 0.415f * k, -0.03f * k), V(0.11f * k, 0.04f * k, 0.15f * k), moss, mat: Leaf);
            foreach (var (x, z, sr, h) in new[] { (0f, 0f, 0.04f, 0.05f), (-0.05f, 0.07f, 0.03f, 0.035f), (0.05f, -0.08f, 0.033f, 0.04f), (0.02f, -0.15f, 0.028f, 0.03f) })
                b.Spike(Body, V(x * k, 0.42f * k, z * k), V(x * k, (0.44f + h) * k, (z - 0.01f) * k), sr * k, stone, mat: Shell, blend: 0.01f);
            foreach (var (x, z) in new[] { (0.07f, 0.03f), (-0.06f, -0.06f) })
                b.Spike(Body, V(x * k, 0.43f * k, z * k), V(x * 1.3f * k, 0.5f * k, z * k), 0.012f * k, Rgb(86, 150, 60), mat: Leaf);
        }

        // The long neck, the throat pale, a small round head
        int head = b.Head(V(0, 0.36f * k, 0.18f * k));
        var neck = Smooth(3, V(0, 0.32f, 0.16f) * k, V(0, 0.48f, 0.27f) * k, V(0, 0.66f, 0.3f) * k, V(0, 0.78f, 0.29f) * k);
        b.Tube(head, neck, (big ? 0.075f : 0.068f) * k, 0.045f * k, green, blend: 0.02f);
        b.PaintEll(head, V(0, 0.52f * k, 0.34f * k), V(0.05f * k, 0.22f * k, 0.045f * k), cream, V(-12f, 0, 0));
        var c = V(0, 0.82f * k, 0.32f * k);
        var r = V(0.065f * k, 0.06f * k, 0.08f * k);
        b.Ell(head, c, r, green);
        b.Ell(head, V(0, 0.8f * k, 0.39f * k), V(0.048f * k, 0.04f * k, 0.045f * k), green);
        b.PaintEll(head, V(0, 0.775f * k, 0.38f * k), V(0.055f * k, 0.022f * k, 0.06f * k), cream);
        PokeBuilder.Both(s => b.Ell(head, V(0.018f * s * k, 0.815f * k, 0.43f * k), V(0.006f * k, 0.006f * k, 0.005f * k), Rgb(70, 86, 46), blend: 0.004f));
        if (big)
            foreach (var (x, y, z) in new[] { (0f, 0.88f, 0.3f), (0.03f, 0.87f, 0.26f), (-0.03f, 0.87f, 0.26f), (0f, 0.865f, 0.28f) })
                b.Ell(head, V(x * k, y * k, z * k), V(0.016f * k, 0.012f * k, 0.016f * k), stone, mat: Shell, blend: 0.006f);
        else
            Carp8Fern(b, head, V(0.02f * k, 0.78f * k, 0.42f * k), V(0.8f, -0.5f, 0.4f), 0.17f * k, Rgb(84, 160, 60));
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.043f * s * k, 0.835f * k), V(0.75f * s, 0.2f, 0.6f), 0.02f * k));
        b.Ground();
        return b;
    }

    /// <summary>A fern frond: a stem from <paramref name="root"/> along <paramref name="dir"/> with leaflets down both sides.</summary>
    private static void Carp8Fern(PokeBuilder b, int bone, Vector3 root, Vector3 dir, float length, Color color)
    {
        var d = Vector3.Normalize(dir);
        var side = Vector3.Normalize(Vector3.Cross(d, MathF.Abs(d.Y) > 0.9f ? Vector3.UnitZ : Vector3.UnitY));
        var tip = root + d * length;
        b.Limb(bone, root, tip, length * 0.06f, length * 0.035f, color, Leaf, 0.006f);
        for (int i = 1; i <= 5; i++)
        {
            float t = i / 6f;
            var at = root + d * length * t;
            float w = length * 0.28f * (1.1f - t);
            PokeBuilder.Both(s => Petal(b, bone, at, at + (side * s + d * 0.4f) * w, length * 0.07f, color, Leaf, 0.006f));
        }
    }

    // ------------------------------------------------------------------ Namolel line

    private static PokeBuilder Carp8Namolel()
    {
        var b = new PokeBuilder("Namolel", 0.5f, BodyPlan.Floating, V(0, 0.14f, 0)) { Coat = Scales };
        var mud = Rgb(124, 124, 112);
        var crust = Rgb(98, 88, 76);
        var bubble = Rgb(176, 186, 168);
        var gas = Rgb(172, 152, 200);

        // A blob of grey clay slumped in its own puddle
        var c = V(0, 0.15f, 0);
        var r = V(0.17f, 0.13f, 0.15f);
        b.Ell(Body, c, r, mud);
        b.Ell(Body, V(0, 0.035f, 0.01f), V(0.22f, 0.035f, 0.2f), mud, blend: 0.05f);
        foreach (var (x, z) in new[] { (0.2f, 0.06f), (-0.18f, 0.1f), (0.08f, 0.2f), (-0.1f, -0.17f) })
            b.Ell(Body, V(x, 0.03f, z), V(0.045f, 0.03f, 0.04f), mud, blend: 0.03f);
        // The dried crust cracked into plates over one side
        foreach (var (y, z, rz) in new[] { (0.2f, 0.02f, 10f), (0.11f, 0.06f, -8f), (0.12f, -0.07f, 6f), (0.2f, -0.08f, -12f), (0.06f, -0.01f, 0f) })
        {
            var dir = V(1f, (y - 0.15f) * 4f, z * 4f);
            var at = Carp8At(c, r, dir);
            b.Ell(Body, at, V(0.022f, 0.05f, 0.048f), crust, Euler(Carp8Normal(c, r, dir)) + V(0, 0, rz), Shell, 0.008f);
        }
        b.PaintEll(Body, V(0.14f, 0.15f, 0), V(0.06f, 0.12f, 0.13f), crust);
        // The bubble swelling on its head
        int top = b.Part("bubble", Body, V(0, 0.25f, 0), PokeRole.Ear);
        b.Ell(top, V(-0.01f, 0.29f, -0.01f), V(0.1f, 0.085f, 0.095f), bubble, mat: Shell, blend: 0.03f);
        // Wisps of gas rising from its sides
        foreach (var (x, z, s) in new[] { (-0.15f, 0.02f, -1f), (0.13f, -0.05f, 1f), (0.05f, -0.12f, 1f) })
            b.Tube(Body, Smooth(3, V(x, 0.14f, z), V(x + 0.03f * s, 0.24f, z), V(x - 0.01f * s, 0.32f, z), V(x + 0.03f * s, 0.4f, z)), 0.016f, 0.008f, gas, blend: 0.01f);
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.055f * s, 0.15f), V(0.35f * s, 0.05f, 1f), 0.022f));
        return b;
    }

    private static PokeBuilder Carp8Namolcan()
    {
        var b = new PokeBuilder("Namolcan", 0.92f, BodyPlan.Quadruped, V(0, 0.4f, 0)) { Coat = Scales };
        var clay = Rgb(118, 118, 106);
        var dark = Rgb(88, 86, 78);
        var wet = Rgb(96, 100, 90);
        var salt = Rgb(230, 224, 168);
        var bubble = Rgb(186, 190, 180);
        var gas = Rgb(170, 150, 200);

        foreach (var (z, front) in new[] { (0.16f, true), (-0.16f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.24f * s, 0.18f, z), front);
                b.Limb(leg, V(0.24f * s, 0.2f, z), V(0.27f * s, 0.06f, z + 0.03f), 0.085f, 0.075f, clay);
                b.Ell(leg, V(0.27f * s, 0.045f, z + 0.05f), V(0.085f, 0.045f, 0.095f), clay);
                foreach (float t in new[] { -1f, 0f, 1f })
                    b.Ell(leg, V(0.27f * s + t * 0.035f, 0.035f, z + 0.13f), V(0.02f, 0.025f, 0.02f), dark, mat: Shell, blend: 0.008f);
            });
        // A cone of cracked clay on a heavy base
        b.Ell(Body, V(0, 0.24f, -0.02f), V(0.34f, 0.16f, 0.3f), clay);
        b.Limb(Body, V(0, 0.26f, -0.02f), V(0, 0.8f, -0.06f), 0.32f, 0.15f, clay);
        // The crater, cut and filled with wet mud
        b.Cut(Body, V(0, 0.92f, -0.06f), V(0.12f, 0.08f, 0.12f));
        b.Ell(Body, V(0, 0.82f, -0.06f), V(0.11f, 0.03f, 0.11f), wet, mat: Shell, blend: 0.01f);
        // Mud spilling over its lip and down its front
        foreach (var (x, len) in new[] { (0.03f, 0.22f), (-0.08f, 0.12f) })
            b.Tube(Body, new[] { V(x, 0.86f, 0.05f), V(x * 1.1f, 0.78f, 0.1f), V(x * 1.2f, 0.86f - len, 0.1f + len * 0.4f) }, 0.028f, 0.02f, wet, Shell, 0.012f);
        // Salt in the cracks, pale lines down its sides
        foreach (var (a, y, len, roll) in new[] { (0.3f, 0.55f, 0.1f, 20f), (-0.4f, 0.5f, 0.09f, -25f), (1.2f, 0.45f, 0.11f, 30f), (-1.3f, 0.48f, 0.1f, -20f), (2.4f, 0.5f, 0.12f, 10f), (-2.5f, 0.6f, 0.08f, -15f), (0.8f, 0.68f, 0.07f, 40f), (-0.9f, 0.7f, 0.07f, -40f) })
        {
            float rad = 0.32f + (0.15f - 0.32f) * (y - 0.26f) / 0.54f;
            var at = V(MathF.Sin(a) * rad, y, -0.03f + MathF.Cos(a) * rad);
            b.PaintEll(Body, at, V(0.012f, len, 0.04f), salt, V(0, a / Degree, roll), 0.008f);
        }
        // Bubbles in the crater and gas rising from its rim
        int top = b.Part("crater", Body, V(0, 0.84f, -0.06f), PokeRole.Ear);
        b.Ell(top, V(0, 0.88f, -0.06f), V(0.075f, 0.07f, 0.075f), bubble, mat: Shell, blend: 0.01f);
        b.Ell(top, V(0.07f, 0.95f, -0.02f), V(0.048f, 0.048f, 0.048f), bubble, mat: Shell, blend: 0.01f);
        b.Ell(top, V(-0.05f, 0.92f, -0.11f), V(0.035f, 0.035f, 0.035f), bubble, mat: Shell, blend: 0.01f);
        foreach (var (x, z, s) in new[] { (-0.12f, -0.02f, -1f), (0.11f, -0.12f, 1f) })
            b.Tube(Body, Smooth(3, V(x, 0.8f, z), V(x + 0.03f * s, 0.9f, z), V(x - 0.01f * s, 0.98f, z), V(x + 0.03f * s, 1.05f, z)), 0.02f, 0.01f, gas, blend: 0.01f);

        // A heavy, frowning face at its foot
        int head = b.Head(V(0, 0.32f, 0.2f));
        var c = V(0, 0.3f, 0.24f);
        var r = V(0.22f, 0.14f, 0.12f);
        b.Ell(head, c, r, clay);
        PokeBuilder.Both(s => b.Ell(head, V(0.085f * s, 0.385f, 0.33f), V(0.075f, 0.03f, 0.04f), clay, V(0, 0, -12f * s), blend: 0.02f));
        b.Ell(head, V(0, 0.33f, 0.36f), V(0.035f, 0.025f, 0.025f), clay, blend: 0.015f);
        b.Ell(head, V(0, 0.235f, 0.33f), V(0.1f, 0.028f, 0.035f), dark, blend: 0.012f);
        b.PaintEll(head, V(0, 0.255f, 0.35f), V(0.1f, 0.008f, 0.04f), Rgb(60, 58, 54), soft: 0.006f);
        b.Tube(head, new[] { V(0.05f, 0.23f, 0.35f), V(0.055f, 0.19f, 0.35f), V(0.05f, 0.16f, 0.34f) }, 0.014f, 0.01f, wet, Shell, 0.008f);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.085f * s, 0.33f), V(0.4f * s, 0.05f, 1f), 0.026f, glare: true));
        b.Ground();
        return b;
    }

    // ------------------------------------------------------------------ Vidrut line

    private static readonly Color Carp8Otter = Rgb(104, 80, 64);
    private static readonly Color Carp8OtterCream = Rgb(240, 228, 200);
    private static readonly Color Carp8OtterFin = Rgb(130, 160, 204);

    private static PokeBuilder Carp8Vidrut()
    {
        var b = new PokeBuilder("Vidrut", 0.55f, BodyPlan.Quadruped, V(0, 0.13f, 0)) { Coat = Fur };
        var brown = Carp8Otter;
        var cream = Carp8OtterCream;
        var fin = Carp8OtterFin;

        // Floating on its back, its belly up, its hind paws in the air
        b.Ell(Body, V(0, 0.12f, -0.02f), V(0.13f, 0.1f, 0.2f), brown);
        b.PaintEll(Body, V(0, 0.21f, 0.0f), V(0.09f, 0.05f, 0.15f), cream);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.16f, -0.12f), front: false);
            b.Limb(leg, V(0.08f * s, 0.15f, -0.12f), V(0.1f * s, 0.25f, -0.16f), 0.042f, 0.035f, brown);
            b.Ell(leg, V(0.105f * s, 0.27f, -0.165f), V(0.034f, 0.028f, 0.04f), brown, V(-30f, 0, 0));
            b.PaintEll(leg, V(0.105f * s, 0.29f, -0.15f), V(0.016f, 0.012f, 0.018f), fin);
        });
        int tail = b.Tail(V(0, 0.1f, -0.2f));
        b.Tube(tail, Smooth(3, V(0, 0.1f, -0.18f), V(0, 0.09f, -0.32f), V(0, 0.14f, -0.44f)), 0.06f, 0.028f, brown, blend: 0.02f);
        b.Ell(tail, V(0, 0.21f, -0.5f), V(0.016f, 0.075f, 0.055f), fin, V(-35f, 0, 0), Scales, 0.01f);
        b.Ell(tail, V(0, 0.16f, -0.53f), V(0.016f, 0.05f, 0.04f), fin, V(-80f, 0, 0), Scales, 0.01f);

        // The pebble clutched to its belly in both paws
        b.Ell(Body, V(0, 0.255f, 0.06f), V(0.065f, 0.035f, 0.055f), Rgb(140, 140, 136), V(0, 0, 10f), Shell, 0.01f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.09f * s, 0.18f, 0.1f));
            b.Limb(arm, V(0.09f * s, 0.18f, 0.1f), V(0.06f * s, 0.26f, 0.09f), 0.032f, 0.028f, brown);
            b.Ell(arm, V(0.05f * s, 0.27f, 0.085f), V(0.03f, 0.024f, 0.03f), brown);
        });

        // The head propped up, its eyes shut in contentment
        int head = b.Head(V(0, 0.17f, 0.16f));
        var c = V(0, 0.24f, 0.23f);
        var r = V(0.105f, 0.09f, 0.095f);
        b.Ell(head, c, r, brown);
        b.Ell(head, V(0, 0.27f, 0.3f), V(0.06f, 0.045f, 0.045f), cream);
        b.PaintEll(head, V(0, 0.22f, 0.29f), V(0.09f, 0.05f, 0.05f), cream);
        b.Ell(head, V(0, 0.3f, 0.335f), V(0.018f, 0.013f, 0.012f), Rgb(80, 54, 46), blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            Blade(b, head, V(0.08f * s, 0.3f, 0.19f), V(0.12f * s, 0.35f, 0.16f), 0.028f, fin, V(0.2f * s, 0.3f, 1f), 0.4f);
            b.Tube(head, new[] { V(0.04f * s, 0.285f, 0.33f), V(0.1f * s, 0.29f, 0.35f), V(0.15f * s, 0.29f, 0.35f) }, 0.006f, 0.004f, cream, blend: 0.003f);
        });
        PokeBuilder.Both(s =>
        {
            var dir = V(0.42f * s, 0.55f, 0.72f);
            b.Eye(head, Carp8At(c, r, dir), Carp8Normal(c, r, dir), 0.022f, closed: true);
        });
        b.Ground();
        return b;
    }

    private static PokeBuilder Carp8Vidroar()
    {
        var b = new PokeBuilder("Vidroar", 0.9f, BodyPlan.Biped, V(0, 0.5f, 0)) { Coat = Fur };
        var brown = Rgb(86, 64, 52);
        var cream = Carp8OtterCream;
        var fin = Carp8OtterFin;

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.11f * s, 0.32f, 0));
            b.Limb(leg, V(0.11f * s, 0.34f, 0), V(0.14f * s, 0.1f, 0.03f), 0.085f, 0.065f, brown);
            b.Ell(leg, V(0.14f * s, 0.035f, 0.08f), V(0.07f, 0.035f, 0.1f), brown);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Ell(leg, V(0.14f * s + t * 0.03f, 0.03f, 0.18f), V(0.011f, 0.012f, 0.014f), fin, mat: Shell, blend: 0.004f);
        });
        // A wrestler's frame: broad shoulders, a pale chest and belly
        b.Ell(Body, V(0, 0.5f, 0), V(0.19f, 0.25f, 0.16f), brown);
        b.PaintEll(Body, V(0, 0.46f, 0.11f), V(0.13f, 0.22f, 0.09f), cream);
        PokeBuilder.Both(s => b.Ell(Body, V(0.15f * s, 0.66f, -0.01f), V(0.11f, 0.1f, 0.1f), brown));
        // Fists raised before its chest
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.2f * s, 0.66f, 0.02f));
            var elbow = V(0.27f * s, 0.5f, 0.12f);
            var fist = s > 0 ? V(0.16f * s, 0.68f, 0.22f) : V(0.1f * s, 0.6f, 0.21f);
            b.Limb(arm, V(0.2f * s, 0.66f, 0.02f), elbow, 0.07f, 0.06f, brown);
            b.Limb(arm, elbow, fist, 0.06f, 0.05f, brown);
            b.Ell(arm, fist + V(0, 0.01f, 0.02f), V(0.06f, 0.06f, 0.055f), brown);
            b.PaintEll(arm, fist + V(0, -0.02f, 0.06f), V(0.03f, 0.018f, 0.02f), fin);
        });
        // A thick tail braced behind, a broad blue fin at its end
        int tail = b.Tail(V(0, 0.32f, -0.14f));
        b.Tube(tail, Smooth(3, V(0, 0.34f, -0.12f), V(0, 0.14f, -0.3f), V(0, 0.09f, -0.46f), V(0, 0.18f, -0.6f)), 0.085f, 0.04f, brown, blend: 0.02f);
        b.Ell(tail, V(0, 0.32f, -0.64f), V(0.02f, 0.13f, 0.08f), fin, V(-20f, 0, 0), Scales, 0.012f);
        b.PaintEll(tail, V(0, 0.32f, -0.64f), V(0.03f, 0.1f, 0.06f), Rgb(110, 140, 186), V(-20f, 0, 0));

        int head = b.Head(V(0, 0.74f, 0.03f));
        b.Limb(head, V(0, 0.7f, 0.03f), V(0, 0.82f, 0.05f), 0.085f, 0.08f, brown);
        var c = V(0, 0.86f, 0.06f);
        var r = V(0.12f, 0.1f, 0.11f);
        b.Ell(head, c, r, brown);
        b.Ell(head, V(0, 0.81f, 0.15f), V(0.07f, 0.05f, 0.055f), cream);
        b.PaintEll(head, V(0, 0.78f, 0.1f), V(0.1f, 0.05f, 0.08f), cream);
        b.Ell(head, V(0, 0.835f, 0.205f), V(0.022f, 0.016f, 0.014f), Rgb(76, 50, 42), blend: 0.006f);
        PokeBuilder.Both(s => b.Spike(head, V(0.025f * s, 0.775f, 0.19f), V(0.025f * s, 0.745f, 0.195f), 0.012f, White, mat: Shell, blend: 0.003f));
        PokeBuilder.Both(s =>
        {
            Blade(b, head, V(0.09f * s, 0.93f, 0.02f), V(0.13f * s, 1.0f, -0.01f), 0.038f, fin, V(0.2f * s, 0.2f, 1f), 0.35f);
            b.Ell(head, V(0.055f * s, 0.92f, 0.13f), V(0.04f, 0.012f, 0.02f), brown, V(0, 0, 18f * s), blend: 0.01f);
            b.Tube(head, new[] { V(0.05f * s, 0.815f, 0.19f), V(0.12f * s, 0.82f, 0.2f), V(0.18f * s, 0.81f, 0.2f) }, 0.007f, 0.005f, cream, blend: 0.003f);
        });
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.05f * s, 0.885f), V(0.45f * s, 0.1f, 1f), 0.026f, glare: true));
        b.Ground();
        return b;
    }

    // ------------------------------------------------------------------ Castorel line

    private static readonly Color Carp8BeaverWeb = Rgb(44, 82, 86);
    private static readonly Color Carp8BeaverTooth = Rgb(240, 132, 40);

    /// <summary>A beaver's flat paddle tail, dark and scaled, from <paramref name="root"/> back to <paramref name="end"/>.</summary>
    private static void Carp8Paddle(PokeBuilder b, int bone, Vector3 root, Vector3 end, float width)
    {
        var mid = (root + end) / 2f;
        float len = Vector3.Distance(root, end) / 2f;
        b.Ell(bone, mid, V(width, width * 0.22f, len), Carp8BeaverWeb, V(-8f, 0, 0), Scales, 0.02f);
        for (int i = 1; i <= 3; i++)
        {
            var at = root + (end - root) * (i / 4f);
            b.PaintEll(bone, at + V(0, width * 0.2f, 0), V(width * 1.1f, width * 0.1f, width * 0.08f), Rgb(30, 60, 64), soft: 0.008f);
        }
    }

    private static PokeBuilder Carp8Castorel()
    {
        var b = new PokeBuilder("Castorel", 0.55f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = Fur };
        var russet = Rgb(176, 92, 70);
        var belly = Rgb(206, 132, 104);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.1f, 0.01f));
            b.Ell(leg, V(0.08f * s, 0.08f, 0.0f), V(0.06f, 0.07f, 0.065f), russet);
            b.Ell(leg, V(0.09f * s, 0.022f, 0.07f), V(0.055f, 0.022f, 0.08f), Carp8BeaverWeb, mat: Scales);
        });
        b.Ell(Body, V(0, 0.2f, 0), V(0.15f, 0.17f, 0.13f), russet);
        b.PaintEll(Body, V(0, 0.17f, 0.08f), V(0.11f, 0.13f, 0.08f), belly);
        int tail = b.Tail(V(0, 0.08f, -0.12f));
        b.Limb(tail, V(0, 0.16f, -0.07f), V(0, 0.06f, -0.13f), 0.05f, 0.04f, russet);
        Carp8Paddle(b, tail, V(0, 0.05f, -0.1f), V(0, 0.03f, -0.38f), 0.085f);

        int head = b.Head(V(0, 0.32f, 0.02f));
        var c = V(0, 0.41f, 0.03f);
        var r = V(0.12f, 0.11f, 0.11f);
        b.Ell(head, c, r, russet);
        b.Ell(head, V(0, 0.37f, 0.11f), V(0.065f, 0.048f, 0.045f), belly);
        b.Ell(head, V(0, 0.39f, 0.155f), V(0.026f, 0.019f, 0.016f), Rgb(40, 52, 60), mat: Shell, blend: 0.006f);
        PokeBuilder.Both(s => b.Box(head, V(0.013f * s, 0.335f, 0.14f), V(0.012f, 0.02f, 0.008f), 0.005f, Carp8BeaverTooth, mat: Shell, blend: 0.003f));
        PokeBuilder.Both(s => b.Ell(head, V(0.09f * s, 0.5f, 0.0f), V(0.03f, 0.03f, 0.018f), russet, blend: 0.012f));
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.055f * s, 0.44f), V(0.45f * s, 0.1f, 1f), 0.026f, Rgb(30, 70, 80), sclera: true, pupil: Rgb(24, 40, 50)));
        // The twig, gnawed in both paws at its mouth
        b.Limb(head, V(-0.17f, 0.35f, 0.14f), V(0.16f, 0.34f, 0.15f), 0.014f, 0.012f, Rgb(122, 86, 60));
        b.Limb(head, V(-0.1f, 0.35f, 0.14f), V(-0.15f, 0.38f, 0.17f), 0.009f, 0.007f, Rgb(122, 86, 60));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.11f * s, 0.27f, 0.06f));
            b.Limb(arm, V(0.11f * s, 0.27f, 0.06f), V(0.075f * s, 0.33f, 0.14f), 0.032f, 0.028f, russet);
            b.Ell(arm, V(0.07f * s, 0.345f, 0.15f), V(0.026f, 0.026f, 0.024f), russet);
        });
        b.Ground();
        return b;
    }

    private static PokeBuilder Carp8Castoroi()
    {
        var b = new PokeBuilder("Castoroi", 0.88f, BodyPlan.Quadruped, V(0, 0.34f, -0.02f)) { Coat = Fur };
        var red = Rgb(136, 58, 46);
        var belly = Rgb(196, 146, 110);
        var wood = Rgb(120, 86, 56);
        var leaf = Rgb(86, 156, 64);

        foreach (var (z, front) in new[] { (0.16f, true), (-0.16f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.16f * s, 0.2f, z), front);
                b.Limb(leg, V(0.16f * s, 0.24f, z), V(0.17f * s, 0.07f, z + 0.02f), front ? 0.06f : 0.085f, 0.05f, red);
                b.Ell(leg, V(0.17f * s, 0.03f, z + 0.07f), V(0.065f, 0.03f, 0.09f), Carp8BeaverWeb, mat: Scales);
                foreach (float t in new[] { -1f, 0f, 1f })
                    b.Limb(leg, V(0.17f * s + t * 0.025f, 0.03f, z + 0.12f), V(0.17f * s + t * 0.035f, 0.015f, z + 0.17f), 0.012f, 0.007f, Carp8BeaverWeb, Shell, 0.004f);
            });
        b.Ell(Body, V(0, 0.34f, -0.02f), V(0.24f, 0.24f, 0.3f), red);
        b.PaintEll(Body, V(0, 0.24f, 0.14f), V(0.17f, 0.15f, 0.14f), belly);
        int tail = b.Tail(V(0, 0.14f, -0.28f));
        b.Limb(tail, V(0, 0.3f, -0.2f), V(0, 0.1f, -0.3f), 0.07f, 0.05f, red);
        Carp8Paddle(b, tail, V(0, 0.08f, -0.26f), V(0, 0.04f, -0.66f), 0.13f);

        // The lodge on its back: a mound of woven branches with leaves sprouting from it
        var mc = V(0, 0.54f, -0.1f);
        var mr = V(0.2f, 0.12f, 0.22f);
        b.Ell(Body, mc, mr, Rgb(96, 70, 46), mat: Leaf);
        for (int i = 0; i < 14; i++)
        {
            float a = i * 2.39996f;
            var d1 = V(MathF.Cos(a), 0.35f + 0.15f * (i % 3), MathF.Sin(a));
            var d2 = V(MathF.Cos(a + 2.3f), 0.5f + 0.12f * ((i + 1) % 3), MathF.Sin(a + 2.3f));
            var p1 = Carp8At(mc, mr, d1) + Carp8Normal(mc, mr, d1) * 0.012f;
            var p2 = Carp8At(mc, mr, d2) + Carp8Normal(mc, mr, d2) * 0.012f;
            var mid = Carp8At(mc, mr, (d1 + d2) / 2f + V(0, 0.4f, 0)) + V(0, 0.015f, 0);
            b.Tube(Body, Smooth(2, p1, mid, p2), 0.016f, 0.012f, i % 2 == 0 ? wood : Rgb(104, 74, 48), Leaf, 0.006f);
        }
        foreach (var (a, up) in new[] { (0.3f, 0.6f), (1.6f, 0.4f), (2.8f, 0.7f), (4.1f, 0.45f), (5.3f, 0.55f), (0.9f, 0.9f) })
        {
            var dir = V(MathF.Cos(a), up, MathF.Sin(a));
            var at = Carp8At(mc, mr, dir);
            var n = Carp8Normal(mc, mr, dir);
            var tip = at + Vector3.Normalize(n + V(0, 0.8f, 0)) * 0.11f;
            b.Limb(Body, at - n * 0.01f, tip, 0.01f, 0.007f, Rgb(98, 120, 50), Leaf, 0.004f);
            Petal(b, Body, tip, tip + Vector3.Normalize(n + V(0, 0.4f, 0.3f)) * 0.07f, 0.03f, leaf, Leaf, 0.006f);
            Petal(b, Body, tip - n * 0.03f, tip - n * 0.03f + Vector3.Normalize(V(-n.Z, 0.5f, n.X)) * 0.06f, 0.026f, leaf, Leaf, 0.006f);
        }

        int head = b.Head(V(0, 0.44f, 0.22f));
        var c = V(0, 0.46f, 0.3f);
        var r = V(0.15f, 0.13f, 0.13f);
        b.Ell(head, c, r, red);
        b.Ell(head, V(0, 0.42f, 0.4f), V(0.085f, 0.06f, 0.055f), belly);
        b.Ell(head, V(0, 0.445f, 0.455f), V(0.034f, 0.024f, 0.02f), Rgb(40, 52, 60), mat: Shell, blend: 0.006f);
        PokeBuilder.Both(s => b.Box(head, V(0.022f * s, 0.355f, 0.43f), V(0.02f, 0.04f, 0.012f), 0.007f, Carp8BeaverTooth, mat: Shell, blend: 0.003f));
        PokeBuilder.Both(s => b.Ell(head, V(0.11f * s, 0.57f, 0.26f), V(0.035f, 0.035f, 0.022f), red, blend: 0.012f));
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.07f * s, 0.5f), V(0.55f * s, 0.1f, 1f), 0.024f));
        b.Ground();
        return b;
    }

    // ------------------------------------------------------------------ Stiucuta line

    /// <summary>A fish's forked tail fin, its two lobes in the tail fin's own bone.</summary>
    private static void Carp8Fork(PokeBuilder b, int bone, Vector3 at, float size, Color color)
    {
        b.Ell(bone, at + V(0, size * 0.75f, -size * 0.5f), V(size * 0.13f, size, size * 0.55f), color, V(-40f, 0, 0), Scales, 0.01f);
        b.Ell(bone, at + V(0, -size * 0.75f, -size * 0.5f), V(size * 0.13f, size, size * 0.55f), color, V(40f, 0, 0), Scales, 0.01f);
    }

    private static PokeBuilder Carp8Stiucuta()
    {
        var b = new PokeBuilder("Stiucuta", 0.55f, BodyPlan.Fish, V(0, 0.3f, 0)) { Coat = Scales }.Hover();
        var green = Rgb(72, 140, 82);
        var cream = Rgb(232, 226, 188);
        var bar = Rgb(196, 214, 150);
        var fin = Rgb(132, 198, 172);

        // A slim green body, pale bars across its back, cream beneath
        b.Ell(Body, V(0, 0.3f, -0.02f), V(0.058f, 0.07f, 0.24f), green);
        b.PaintEll(Body, V(0, 0.255f, 0.0f), V(0.065f, 0.035f, 0.25f), cream);
        foreach (float z in new[] { -0.17f, -0.11f, -0.05f, 0.01f, 0.07f, 0.13f })
            b.PaintEll(Body, V(0, 0.335f, z), V(0.07f, 0.04f, 0.009f), bar, soft: 0.008f);
        // Fins set far back, the dorsal over the anal
        int dorsal = b.Part("dorsal", Body, V(0, 0.36f, -0.12f), PokeRole.Fin);
        Blade(b, dorsal, V(0, 0.35f, -0.08f), V(0, 0.43f, -0.19f), 0.04f, fin, V(1, 0, 0), 0.25f);
        Blade(b, Body, V(0, 0.25f, -0.1f), V(0, 0.19f, -0.18f), 0.03f, fin, V(1, 0, 0), 0.25f);
        PokeBuilder.Both(s =>
        {
            int side = b.Part(s < 0 ? "finL" : "finR", Body, V(0.045f * s, 0.26f, 0.08f), PokeRole.Fin, s, s);
            b.Ell(side, V(0.07f * s, 0.24f, 0.05f), V(0.01f, 0.03f, 0.05f), fin, V(30f, 25f * s, 30f * s));
        });
        int tail = b.Tail(V(0, 0.3f, -0.22f));
        b.Limb(tail, V(0, 0.3f, -0.22f), V(0, 0.3f, -0.29f), 0.035f, 0.024f, green);
        int tailFin = b.Part("tailFin", tail, V(0, 0.3f, -0.29f), PokeRole.Fin);
        Carp8Fork(b, tailFin, V(0, 0.3f, -0.29f), 0.07f, fin);

        // A long head with a flat little duck-bill snout
        int head = b.Head(V(0, 0.3f, 0.16f));
        var c = V(0, 0.31f, 0.24f);
        var r = V(0.055f, 0.057f, 0.09f);
        b.Ell(head, c, r, green);
        b.Ell(head, V(0, 0.285f, 0.34f), V(0.042f, 0.02f, 0.07f), green);
        b.PaintEll(head, V(0, 0.27f, 0.3f), V(0.05f, 0.02f, 0.11f), cream);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.04f * s, 0.32f), V(0.85f * s, 0.15f, 0.45f), 0.024f, Rgb(200, 150, 50)));
        return Lift(b);
    }

    private static PokeBuilder Carp8Stiucon()
    {
        var b = new PokeBuilder("Stiucon", 0.95f, BodyPlan.Fish, V(0, 0.4f, 0)) { Coat = Scales }.Hover();
        var olive = Rgb(98, 120, 64);
        var back = Rgb(38, 52, 34);
        var cream = Rgb(226, 222, 186);
        var spot = Rgb(216, 214, 130);
        var fin = Rgb(112, 168, 140);

        // A long torpedo of a body, bent like a drawn bow, ready to strike
        var spine = new[] { V(0, 0.46f, 0.24f), V(0.02f, 0.42f, 0.02f), V(0.07f, 0.38f, -0.2f), V(0.03f, 0.36f, -0.42f) };
        b.Tube(Body, Smooth(3, spine), 0.11f, 0.08f, olive, blend: 0.03f);
        foreach (var p in spine)
        {
            b.PaintEll(Body, p + V(0, 0.08f, 0), V(0.07f, 0.05f, 0.11f), back);
            b.PaintEll(Body, p + V(0, -0.08f, 0), V(0.09f, 0.05f, 0.12f), cream);
        }
        for (int i = 0; i < 3; i++)
        {
            var p = Vector3.Lerp(spine[i], spine[i + 1], 0.5f);
            PokeBuilder.Both(s =>
            {
                b.PaintEll(Body, p + V(0.09f * s, 0.03f, 0.03f), V(0.03f, 0.018f, 0.025f), spot, soft: 0.008f);
                b.PaintEll(Body, p + V(0.095f * s, -0.01f, -0.05f), V(0.03f, 0.018f, 0.025f), spot, soft: 0.008f);
            });
        }
        int tail = b.Tail(V(0.03f, 0.36f, -0.42f));
        var tailSpine = new[] { V(0.03f, 0.36f, -0.42f), V(-0.04f, 0.38f, -0.6f), V(-0.03f, 0.43f, -0.76f) };
        b.Tube(tail, Smooth(3, tailSpine), 0.08f, 0.04f, olive, blend: 0.02f);
        b.PaintEll(tail, V(-0.01f, 0.43f, -0.6f), V(0.06f, 0.04f, 0.1f), back);
        PokeBuilder.Both(s => b.PaintEll(tail, V(-0.01f + 0.07f * s, 0.38f, -0.58f), V(0.025f, 0.016f, 0.022f), spot, soft: 0.008f));
        int tailFin = b.Part("tailFin", tail, V(-0.03f, 0.43f, -0.78f), PokeRole.Fin);
        Carp8Fork(b, tailFin, V(-0.03f, 0.43f, -0.79f), 0.12f, fin);

        // Ragged fins along its back and belly
        int dorsal = b.Part("dorsal", Body, V(0.04f, 0.5f, -0.1f), PokeRole.Fin);
        foreach (var (z, h) in new[] { (-0.04f, 0.12f), (-0.14f, 0.15f), (-0.24f, 0.13f), (-0.34f, 0.1f) })
            Blade(b, dorsal, V(0.05f, 0.48f, z), V(0.05f, 0.48f + h, z - 0.08f), 0.035f, fin, V(1, 0, 0), 0.25f);
        foreach (var (z, h) in new[] { (-0.1f, 0.1f), (-0.26f, 0.12f) })
            Blade(b, Body, V(0.05f, 0.3f, z), V(0.05f, 0.3f - h, z - 0.08f), 0.03f, fin, V(1, 0, 0), 0.25f);
        PokeBuilder.Both(s =>
        {
            int side = b.Part(s < 0 ? "finL" : "finR", Body, V(0.08f * s, 0.36f, 0.12f), PokeRole.Fin, s, s);
            Blade(b, side, V(0.08f * s, 0.36f, 0.12f), V(0.2f * s, 0.27f, 0.04f), 0.04f, fin, V(0.2f, 1f, 0.3f), 0.25f);
        });

        // A wide duck-bill jaw open on its needle teeth
        int head = b.Head(V(0, 0.46f, 0.24f));
        var c = V(0, 0.48f, 0.33f);
        var r = V(0.09f, 0.08f, 0.12f);
        b.Ell(head, c, r, olive);
        b.PaintEll(head, V(0, 0.55f, 0.31f), V(0.07f, 0.04f, 0.13f), back);
        b.Ell(head, V(0, 0.47f, 0.5f), V(0.068f, 0.03f, 0.13f), olive);
        b.Ell(head, V(0, 0.44f, 0.44f), V(0.055f, 0.03f, 0.1f), Rgb(150, 64, 72), blend: 0.01f);
        for (int i = 0; i < 4; i++)
        {
            float z = 0.42f + i * 0.05f;
            PokeBuilder.Both(s => b.Spike(head, V(0.052f * s, 0.455f, z), V(0.05f * s, 0.42f, z + 0.005f), 0.011f, White, mat: Shell, blend: 0.003f));
        }
        int jaw = b.Jaw(head, V(0, 0.42f, 0.3f));
        b.Ell(jaw, V(0, 0.385f, 0.45f), V(0.06f, 0.025f, 0.12f), cream, V(10f, 0, 0));
        for (int i = 0; i < 3; i++)
        {
            float z = 0.42f + i * 0.05f;
            PokeBuilder.Both(s => b.Spike(jaw, V(0.045f * s, 0.4f - i * 0.009f, z), V(0.044f * s, 0.43f - i * 0.009f, z + 0.005f), 0.01f, White, mat: Shell, blend: 0.003f));
        }
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.065f * s, 0.52f), V(0.8f * s, 0.25f, 0.5f), 0.022f, Rgb(232, 214, 90), glare: true));
        return Lift(b);
    }

    // ------------------------------------------------------------------ Racut line

    /// <summary>
    /// A river crayfish: a shell over its front, a segmented tail curled under, stalked eyes and long feelers, two claws
    /// too big for it; the grown one plated in bronze like a knight, one claw a crushing hammer.
    /// </summary>
    private static PokeBuilder Carp8Crayfish(int stage)
    {
        bool big = stage == 1;
        float k = big ? 1.45f : 1f;
        var b = new PokeBuilder(big ? "Racoroi" : "Racut", big ? 0.86f : 0.5f, BodyPlan.Quadruped, V(0, 0.13f * k, 0)) { Coat = big ? Metal : Shell };
        var shell = big ? Rgb(112, 100, 64) : Rgb(112, 104, 64);
        var dark = big ? Rgb(84, 74, 46) : Rgb(80, 84, 48);
        var claw = big ? Rgb(116, 50, 44) : Rgb(104, 52, 44);
        var feeler = Rgb(206, 120, 58);
        var legColour = big ? Rgb(100, 90, 58) : Rgb(126, 112, 70);

        // Four thin legs a side
        PokeBuilder.Both(s =>
        {
            int l = b.Leg(s, V(0.05f * s * k, 0.1f * k, 0));
            foreach (float z in new[] { 0.06f, 0.015f, -0.03f, -0.075f })
            {
                var root = V(0.05f * s * k, 0.1f * k, z * k);
                var knee = V(0.11f * s * k, 0.14f * k, z * 1.6f * k);
                var foot = V(0.14f * s * k, 0.008f * k, z * 2.2f * k);
                b.Tube(l, new[] { root, knee, foot }, (big ? 0.012f : 0.009f) * k, (big ? 0.006f : 0.005f) * k, legColour, blend: 0f);
                if (big) b.Spike(l, foot + V(0, 0.03f * k, 0), foot + V(0.01f * s * k, -0.004f * k, 0), 0.008f * k, claw, mat: Metal, blend: 0.003f);
            }
        });
        // The shell over its front
        var c = V(0, 0.13f * k, 0.03f * k);
        var r = V(0.075f * k, 0.065f * k, 0.11f * k);
        b.Ell(Body, c, r, shell);
        b.PaintEll(Body, c + V(0, -0.04f * k, 0), V(0.085f * k, 0.035f * k, 0.12f * k), dark);
        if (big)
        {
            foreach (float z in new[] { 0.0f, 0.07f })
                b.PaintEll(Body, V(0, 0.16f * k, z * k), V(0.09f * k, 0.08f * k, 0.007f * k), Rgb(64, 56, 36), soft: 0.006f);
            foreach (var (x, z) in new[] { (0.055f, 0.04f), (-0.055f, 0.04f), (0.06f, -0.03f), (-0.06f, -0.03f) })
                b.Ell(Body, V(x * k, 0.175f * k, z * k), V(0.01f * k, 0.01f * k, 0.01f * k), Rgb(150, 132, 84), mat: Metal, blend: 0.003f);
        }
        // The tail: segments shrinking back, curled under (the grown one's held up behind, ending in a fan)
        int tail = b.Tail(V(0, 0.13f * k, -0.08f * k));
        int n = 5;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)(n - 1);
            var at = big
                ? V(0, (0.14f + 0.07f * t) * k, (-0.1f - 0.12f * t) * k)
                : V(0, (0.12f - 0.04f * t) * k, (-0.09f - 0.1f * t + 0.04f * t * t) * k);
            float w = (0.062f - 0.018f * t) * k;
            b.Ell(tail, at, V(w, w * 0.75f, w * 0.7f), i % 2 == 0 ? shell : dark, V(big ? -30f * t : 40f * t, 0, 0), blend: 0.012f);
        }
        var fanAt = big ? V(0, 0.23f * k, -0.25f * k) : V(0, 0.07f * k, -0.17f * k);
        foreach (float a in new[] { -40f, 0f, 40f })
            b.Ell(tail, fanAt + V(MathF.Sin(a * Degree) * 0.03f * k, 0, 0), V(0.018f * k, 0.012f * k, 0.035f * k), dark, V(big ? -60f : 70f, a, 0), blend: 0.008f);

        // The claws, raised: two too big for the little one, one a crushing hammer on the grown one
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.06f * s * k, 0.13f * k, 0.1f * k));
            var elbow = V(0.13f * s * k, 0.2f * k, 0.14f * k);
            b.Limb(arm, V(0.06f * s * k, 0.13f * k, 0.1f * k), elbow, 0.022f * k, 0.02f * k, legColour);
            bool hammer = big && s > 0;
            if (hammer)
            {
                var wrist = V(0.15f * s * k, 0.29f * k, 0.18f * k);
                b.Limb(arm, elbow, wrist, 0.03f * k, 0.035f * k, shell);
                // A blunt mallet of a claw pointing forward, its short finger closing under it
                b.Box(arm, V(0.16f * s * k, 0.33f * k, 0.25f * k), V(0.055f * k, 0.05f * k, 0.085f * k), 0.025f * k, shell, V(-25f, 0, 0), Metal);
                b.PaintEll(arm, V(0.16f * s * k, 0.36f * k, 0.32f * k), V(0.06f * k, 0.03f * k, 0.03f * k), claw, V(-25f, 0, 0));
                b.Spike(arm, V(0.16f * s * k, 0.28f * k, 0.22f * k), V(0.16f * s * k, 0.25f * k, 0.32f * k), 0.022f * k, claw, mat: Metal);
            }
            else
            {
                float size = big ? 0.065f : 0.075f;
                CrabClaw(b, arm, elbow, V(0.15f * s, 1f, 0.35f), V(-s, 0, 0), size * k, claw);
            }
        });

        // Eyes on stalks, and feelers sweeping up and back
        int head = b.Head(V(0, 0.15f * k, 0.12f * k));
        b.Ell(head, V(0, 0.15f * k, 0.13f * k), V(0.045f * k, 0.035f * k, 0.035f * k), shell);
        if (big) b.Box(head, V(0, 0.17f * k, 0.15f * k), V(0.04f * k, 0.022f * k, 0.02f * k), 0.008f * k, Rgb(90, 80, 52), mat: Metal, blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            var ball = V(0.035f * s * k, 0.22f * k, 0.15f * k);
            b.Limb(head, V(0.02f * s * k, 0.16f * k, 0.14f * k), ball, 0.012f * k, 0.012f * k, shell);
            b.Ell(head, ball, V(0.034f * k, 0.034f * k, 0.034f * k), Rgb(40, 38, 36), mat: Shell, blend: 0.006f);
            b.Eye(head, ball + V(0.012f * s * k, 0, 0.031f * k), V(0.35f * s, 0.05f, 1f), 0.017f * k, sclera: true, pupil: Black);
            var points = big
                ? new[] { V(0.02f * s, 0.15f, 0.16f), V(0.06f * s, 0.24f, 0.2f), V(0.05f * s, 0.32f, 0.18f), V(0.09f * s, 0.4f, 0.16f), V(0.08f * s, 0.46f, 0.12f) }
                : new[] { V(0.02f * s, 0.15f, 0.16f), V(0.05f * s, 0.22f, 0.21f), V(0.07f * s, 0.3f, 0.2f), V(0.08f * s, 0.36f, 0.16f) };
            b.Tube(head, points.Select(p => p * k).ToArray(), 0.008f * k, 0.005f * k, feeler, blend: 0.003f);
        });
        b.Ground();
        return b;
    }

    // ------------------------------------------------------------------ Cormoran

    private static PokeBuilder Carp8Cormoran()
    {
        var b = new PokeBuilder("Cormoran", 0.86f, BodyPlan.Bird, V(0, 0.42f, 0)) { Coat = Fur };
        var black = Rgb(34, 50, 46);
        var sheen = Rgb(52, 84, 74);
        var blue = Rgb(44, 58, 92);
        var yellow = Rgb(240, 190, 52);
        var beak = Rgb(104, 106, 110);
        var foot = Rgb(40, 40, 44);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.24f, 0));
            b.Limb(leg, V(0.05f * s, 0.28f, 0), V(0.06f * s, 0.05f, 0.02f), 0.025f, 0.018f, foot);
            Digits(b, leg, V(0.06f * s, 0.025f, 0.03f), V(0, -0.3f, 1f), V(0.5f * s, 0, 0), 0.05f, 0.012f, foot, Shell);
        });
        // An upright, glossy body, the tail feathers fanned beneath
        b.Ell(Body, V(0, 0.42f, -0.01f), V(0.11f, 0.17f, 0.1f), black, V(-10f, 0, 0));
        b.PaintEll(Body, V(0, 0.5f, 0.06f), V(0.08f, 0.1f, 0.06f), sheen);
        int tail = b.Tail(V(0, 0.3f, -0.07f));
        foreach (float a in new[] { -18f, 0f, 18f })
            Blade(b, tail, V(0, 0.3f, -0.07f), V(MathF.Sin(a * Degree) * 0.08f, 0.08f, -0.12f), 0.04f, black, V(0, 0.4f, 1f), 0.25f);

        // The wings held wide open to dry, long dark feathers hanging from them like a cloak
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.08f * s, 0.56f, -0.02f));
            var shoulder = V(0.08f * s, 0.56f, -0.02f);
            var wrist = V(0.34f * s, 0.72f, -0.04f);
            b.Limb(wing, shoulder, wrist, 0.035f, 0.025f, black);
            b.Ell(wing, V(0.21f * s, 0.6f, -0.04f), V(0.13f, 0.1f, 0.018f), black, V(0, 0, 28f * s));
            foreach (var (from, to, w) in new[]
                     {
                         (V(0.34f, 0.72f, -0.05f), V(0.4f, 0.2f, -0.06f), 0.045f), (V(0.3f, 0.7f, -0.05f), V(0.33f, 0.26f, -0.06f), 0.045f),
                         (V(0.25f, 0.66f, -0.05f), V(0.25f, 0.32f, -0.06f), 0.045f), (V(0.19f, 0.62f, -0.05f), V(0.18f, 0.37f, -0.06f), 0.04f),
                         (V(0.13f, 0.58f, -0.05f), V(0.12f, 0.4f, -0.06f), 0.035f)
                     })
                Blade(b, wing, V(from.X * s, from.Y, from.Z), V(to.X * s, to.Y, to.Z), w, blue, V(0, 0, 1), 0.3f);
            b.PaintEll(wing, V(0.34f * s, 0.62f, -0.05f), V(0.08f, 0.12f, 0.05f), sheen);
            b.Spike(wing, wrist, wrist + V(0.02f * s, 0.06f, 0), 0.02f, black);
        });

        // A long neck, a hooked grey beak and a bare yellow throat
        int head = b.Head(V(0, 0.58f, 0.04f));
        b.Tube(head, Smooth(3, V(0, 0.56f, 0.03f), V(0, 0.68f, 0.07f), V(0, 0.78f, 0.04f)), 0.05f, 0.04f, black, blend: 0.02f);
        var c = V(0, 0.82f, 0.05f);
        var r = V(0.055f, 0.05f, 0.07f);
        b.Ell(head, c, r, black);
        b.Ell(head, V(0, 0.775f, 0.09f), V(0.035f, 0.04f, 0.035f), yellow, blend: 0.01f);
        b.Tube(head, Smooth(3, V(0, 0.815f, 0.1f), V(0, 0.81f, 0.18f), V(0, 0.785f, 0.215f)), 0.02f, 0.008f, beak, Shell, 0.006f);
        foreach (var (x, z, h) in new[] { (0f, 0.0f, 0.07f), (0.015f, 0.02f, 0.05f), (-0.015f, 0.02f, 0.05f) })
            b.Spike(head, V(x, 0.86f, z), V(x * 2f, 0.86f + h, z - 0.05f), 0.014f, black);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.038f * s, 0.835f), V(0.85f * s, 0.15f, 0.45f), 0.016f, Rgb(40, 214, 200), glare: true));
        b.Ground();
        return b;
    }

    // ------------------------------------------------------------------ Nufarel line

    private static readonly Color Carp8Pad = Rgb(92, 150, 70);
    private static readonly Color Carp8Petal = Rgb(242, 204, 212);
    private static readonly Color Carp8Sepal = Rgb(142, 178, 98);

    /// <summary>A round lily pad with its notch, lying flat at <paramref name="at"/>.</summary>
    private static void Carp8LilyPad(PokeBuilder b, int bone, Vector3 at, float radius, float notchYaw)
    {
        b.Ell(bone, at, V(radius, 0.016f, radius * 0.94f), Carp8Pad, mat: Leaf, blend: 0.01f);
        var dir = V(MathF.Sin(notchYaw * Degree), 0, MathF.Cos(notchYaw * Degree));
        b.Cut(bone, at + dir * radius * 0.92f, V(radius * 0.32f, 0.05f, radius * 0.12f), V(0, notchYaw, 0), 0.004f);
        b.PaintEll(bone, at + V(0, 0.016f, 0), V(radius * 0.08f, 0.01f, radius * 0.08f), Rgb(74, 126, 56));
    }

    private static PokeBuilder Carp8Nufarel()
    {
        var b = new PokeBuilder("Nufarel", 0.5f, BodyPlan.Floating, V(0, 0.15f, 0)) { Coat = Leaf };
        var deep = Rgb(226, 170, 188);

        Carp8LilyPad(b, Body, V(0, 0.016f, 0.0f), 0.25f, 150f);
        // The bud, its petals closed to a point
        var c = V(0, 0.15f, 0);
        var r = V(0.095f, 0.12f, 0.095f);
        b.Ell(Body, c, r, Carp8Petal);
        foreach (var (a, h) in new[] { (30f, 0.26f), (150f, 0.25f), (270f, 0.27f), (90f, 0.24f), (210f, 0.24f), (330f, 0.25f) })
        {
            var d = V(MathF.Sin(a * Degree), 0, MathF.Cos(a * Degree));
            Petal(b, Body, c + d * 0.07f + V(0, -0.04f, 0), V(0, h, 0) + d * 0.02f, 0.05f, a % 60 == 30 ? Carp8Petal : deep, Leaf, 0.012f);
        }
        b.Ell(Body, V(0, 0.27f, 0), V(0.02f, 0.03f, 0.02f), deep, blend: 0.01f);
        // Green sepals cupping it, two of them parted round its eyes
        foreach (float a in new[] { 0f, 75f, -75f, 150f, -150f })
        {
            var d = V(MathF.Sin(a * Degree), 0, MathF.Cos(a * Degree));
            Petal(b, Body, c + d * 0.08f + V(0, -0.06f, 0), c + d * 0.11f + V(0, 0.07f, 0), a == 0f ? 0.032f : 0.05f, Carp8Sepal, Leaf, 0.012f);
        }
        // A thin root trailing behind on the pad
        b.Tube(Body, Smooth(3, V(0.04f, 0.05f, -0.07f), V(0.12f, 0.035f, -0.16f), V(0.24f, 0.04f, -0.2f), V(0.3f, 0.07f, -0.17f)), 0.011f, 0.007f, Rgb(198, 184, 140), blend: 0.004f);
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.052f * s, 0.12f), V(0.5f * s, 0.05f, 1f), 0.016f));
        return b;
    }

    private static PokeBuilder Carp8Nufarina()
    {
        var b = new PokeBuilder("Nufarina", 0.92f, BodyPlan.Biped, V(0, 0.45f, 0)) { Coat = Leaf };
        var white = Rgb(248, 236, 240);
        var stem = Rgb(108, 156, 74);
        var leaf = Rgb(118, 160, 80);
        var skin = Rgb(244, 238, 226);

        // A circle of pads on the water and her gown, a lily in full bloom, spread over them
        int gown = b.Part("gown", Root, V(0, 0.08f, 0), PokeRole.Leaf);
        foreach (var (x, z, rr, yaw) in new[] { (0.22f, 0.12f, 0.17f, 40f), (-0.22f, 0.1f, 0.16f, -60f), (0.08f, -0.24f, 0.17f, 170f), (-0.16f, -0.18f, 0.15f, 220f), (0.0f, 0.24f, 0.16f, 0f), (0.25f, -0.1f, 0.15f, 110f) })
            Carp8LilyPad(b, gown, V(x, 0.016f, z), rr, yaw);
        for (int i = 0; i < 12; i++)
        {
            float a = i * 30f + 15f;
            var d = V(MathF.Sin(a * Degree), 0, MathF.Cos(a * Degree));
            Petal(b, gown, d * 0.08f + V(0, 0.05f, 0), d * 0.36f + V(0, 0.08f, 0), 0.075f, white, Leaf, 0.012f);
        }
        for (int i = 0; i < 9; i++)
        {
            float a = i * 40f;
            var d = V(MathF.Sin(a * Degree), 0, MathF.Cos(a * Degree));
            Petal(b, gown, d * 0.05f + V(0, 0.07f, 0), d * 0.22f + V(0, 0.2f, 0), 0.065f, Carp8Petal, Leaf, 0.012f);
        }
        b.Ell(gown, V(0, 0.09f, 0), V(0.1f, 0.06f, 0.1f), Rgb(232, 206, 120), blend: 0.02f);

        // A slender stem of a body, a skirt of leaves at her waist and a collar at her shoulders
        b.Tube(Body, Smooth(3, V(0, 0.06f, 0), V(0.02f, 0.3f, 0.0f), V(0, 0.48f, 0.0f), V(0, 0.64f, 0.0f)), 0.045f, 0.04f, stem, blend: 0.02f);
        b.Ell(Body, V(0, 0.56f, 0), V(0.06f, 0.08f, 0.05f), stem);
        for (int i = 0; i < 7; i++)
        {
            float a = i * 51.4f;
            var d = V(MathF.Sin(a * Degree), 0, MathF.Cos(a * Degree));
            Petal(b, Body, V(0, 0.5f, 0) + d * 0.03f, V(0, 0.36f, 0) + d * 0.13f, 0.045f, leaf, Leaf, 0.012f);
        }
        PokeBuilder.Both(s => Petal(b, Body, V(0.03f * s, 0.63f, 0), V(0.13f * s, 0.6f, -0.01f), 0.04f, leaf, Leaf, 0.012f));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.05f * s, 0.62f, 0.0f));
            var pts = s > 0
                ? new[] { V(0.05f, 0.62f, 0), V(0.1f, 0.56f, 0.06f), V(0.06f, 0.66f, 0.12f), V(0.04f, 0.74f, 0.1f) }
                : new[] { V(-0.05f, 0.62f, 0), V(-0.13f, 0.52f, 0.02f), V(-0.2f, 0.46f, 0.04f), V(-0.26f, 0.44f, 0.04f) };
            b.Tube(arm, Smooth(3, pts), 0.02f, 0.014f, stem, blend: 0.01f);
            Petal(b, arm, pts[^1], pts[^1] + Vector3.Normalize(pts[^1] - pts[^2]) * 0.06f, 0.022f, leaf, Leaf, 0.008f);
            if (s < 0)
                b.Tube(arm, Spiral(V(-0.225f, 0.44f, 0.04f), V(-1, 0, 0), V(0, 1, 0), 0.035f, 0.012f, 0f, 1.6f * MathF.PI, 12), 0.009f, 0.006f, stem, blend: 0.004f);
        });

        // A pale face, eyes shut, crowned by a pink bud in its green sepals
        int head = b.Head(V(0, 0.68f, 0));
        b.Limb(head, V(0, 0.64f, 0), V(0, 0.74f, 0.01f), 0.022f, 0.022f, stem);
        var c = V(0, 0.8f, 0.02f);
        var r = V(0.06f, 0.07f, 0.058f);
        b.Ell(head, c, r, skin);
        PokeBuilder.Both(s => b.PaintEll(head, V(0.035f * s, 0.78f, 0.07f), V(0.014f, 0.01f, 0.01f), Rgb(240, 170, 176)));
        b.Ell(head, V(0, 0.9f, -0.01f), V(0.07f, 0.08f, 0.07f), Carp8Petal);
        foreach (float a in new[] { 0f, 72f, 144f, 216f, 288f })
        {
            var d = V(MathF.Sin(a * Degree), 0, MathF.Cos(a * Degree));
            Petal(b, head, V(0, 0.9f, -0.01f) + d * 0.05f, V(0, 1.0f, -0.01f) + d * 0.02f, 0.04f, Rgb(232, 180, 196), Leaf, 0.01f);
        }
        foreach (float a in new[] { 36f, 108f, 180f, 252f, 324f })
        {
            var d = V(MathF.Sin(a * Degree), 0, MathF.Cos(a * Degree));
            Petal(b, head, V(0, 0.84f, -0.01f) + d * 0.06f, V(0, 0.92f, -0.01f) + d * 0.1f, 0.036f, leaf, Leaf, 0.01f);
        }
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.025f * s, 0.805f), V(0.35f * s, 0.05f, 1f), 0.014f, closed: true));
        b.Ground();
        return b;
    }

    // ------------------------------------------------------------------ Meduzel line

    /// <summary>
    /// The Black Sea's jellyfish: a milky bell ringed on top with four pale rings, a fringe round its rim and long
    /// tentacles; the grown one's bell ridged like a crown, a ruff of purple mouth arms beneath it.
    /// </summary>
    private static PokeBuilder Carp8Jelly(int stage)
    {
        bool big = stage == 1;
        var b = new PokeBuilder(big ? "Meduzaur" : "Meduzel", big ? 0.94f : 0.55f, BodyPlan.Floating, V(0, 0.6f, 0)) { Coat = Shell }.Hover();
        var bell = big ? Rgb(206, 212, 240) : Rgb(234, 236, 248);
        var ring = big ? Rgb(176, 160, 220) : Rgb(204, 200, 222);
        var fringe = big ? Rgb(170, 150, 222) : Rgb(214, 206, 236);
        var arm = Rgb(150, 104, 196);
        var tentacle = big ? Rgb(190, 190, 232) : Rgb(222, 224, 240);

        var c = V(0, 0.62f, 0);
        var r = big ? V(0.26f, 0.2f, 0.24f) : V(0.18f, 0.12f, 0.17f);
        b.Ell(Body, c, r, bell);
        if (big)
        {
            // Ridges down the bell and a crown of points round its top
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f + 22.5f;
                var d = V(MathF.Sin(a * Degree), 0.25f, MathF.Cos(a * Degree));
                var at = Carp8At(c, r, d);
                b.Ell(Body, at, V(0.03f, 0.13f, 0.03f), bell, Euler(Carp8Normal(c, r, d + V(0, 1.5f, 0))), blend: 0.02f);
                var top = Carp8At(c, r, V(MathF.Sin(a * Degree) * 0.35f, 1f, MathF.Cos(a * Degree) * 0.35f));
                b.Spike(Body, top - V(0, 0.02f, 0), top + V(0, 0.06f, 0) + V(MathF.Sin(a * Degree), 0, MathF.Cos(a * Degree)) * 0.02f, 0.035f, bell, blend: 0.012f);
            }
        }
        // The underside hollowed, a soft inner body showing in front
        b.Cut(Body, c - V(0, r.Y * 0.75f, 0), V(r.X * 0.85f, r.Y * 0.45f, r.Z * 0.85f));
        var ic = c + V(0, -r.Y * 0.25f, r.Z * 0.45f);
        var ir = big ? V(0.1f, 0.09f, 0.09f) : V(0.07f, 0.065f, 0.06f);
        b.Ell(Body, ic, ir, Rgb(228, 226, 246), blend: 0.02f);
        // The four rings on top
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f + 45f;
            var d = V(MathF.Sin(a * Degree) * 0.6f, 1f, MathF.Cos(a * Degree) * 0.6f + 0.25f);
            b.Mark(Body, Carp8At(c, r, d), Carp8Normal(c, r, d), (big ? 0.05f : 0.035f), (big ? 0.05f : 0.035f), ring, MarkShape.Ring);
        }
        // The fringe round its rim
        float rimY = c.Y - r.Y * 0.32f;
        int fringeCount = big ? 20 : 22;
        for (int i = 0; i < fringeCount; i++)
        {
            float a = i * MathF.Tau / fringeCount;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = V(d.X * r.X * 0.92f, rimY, d.Z * r.Z * 0.92f);
            b.Limb(Body, root, root + d * 0.015f + V(0, big ? -0.05f : -0.06f, 0), big ? 0.012f : 0.009f, 0.006f, fringe, blend: 0.004f);
        }
        if (big)
        {
            // A ruff of purple mouth arms
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f;
                var d = V(MathF.Sin(a * Degree), 0, MathF.Cos(a * Degree));
                b.Ell(Body, V(d.X * 0.2f, rimY - 0.04f, d.Z * 0.18f), V(0.06f, 0.035f, 0.04f), arm, V(0, a, 25f), blend: 0.02f);
            }
        }
        // Long tentacles
        int count = big ? 8 : 5;
        for (int i = 0; i < count; i++)
        {
            float a = i * MathF.Tau / count + 0.4f;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = V(d.X * r.X * 0.4f, rimY - 0.01f, d.Z * r.Z * 0.4f);
            int t = b.Part("tentacle" + i, Body, root, PokeRole.Leaf, i * 0.7f);
            float len = big ? 0.5f : 0.38f;
            var swing = V(d.Z, 0, -d.X) * 0.05f * ((i % 2) * 2 - 1);
            var pts = new[] { root, root + d * 0.04f + V(0, -len * 0.3f, 0) + swing, root + d * 0.02f + V(0, -len * 0.65f, 0) - swing, root + d * 0.07f + V(0, -len, 0) + swing };
            b.Tube(t, Smooth(3, pts), big ? 0.02f : 0.016f, big ? 0.01f : 0.009f, tentacle, blend: 0.006f);
            if (big && i % 2 == 0)
                foreach (float y in new[] { 0.12f, 0.24f })
                    b.Ell(t, root + d * 0.03f + V(0, -y, 0), V(0.035f, 0.05f, 0.016f), arm, V(0, a / Degree, 15f), blend: 0.012f);
        }
        PokeBuilder.Both(s => b.Eye(Body, On(ic, ir, ir.X * 0.42f * s, ic.Y + ir.Y * 0.1f), V(0.3f * s, 0.05f, 1f), big ? 0.02f : 0.016f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ Rapanel

    private static PokeBuilder Carp8Rapanel()
    {
        var b = new PokeBuilder("Rapanel", 0.5f, BodyPlan.Floating, V(0, 0.12f, 0)) { Coat = Scales };
        var body = Rgb(118, 136, 150);
        var cream = Rgb(236, 222, 190);
        var stripe = Rgb(160, 112, 72);

        // A soft grey foot creeping forward, the head rising at its front
        b.Ell(Body, V(0, 0.045f, 0.0f), V(0.11f, 0.045f, 0.25f), body);
        b.Ell(Body, V(0, 0.06f, -0.2f), V(0.08f, 0.03f, 0.08f), body, blend: 0.03f);
        // The shell, a ridged spiral of cream with brown bands, its whorls turning out to its left side
        var sc = V(0, 0.2f, -0.05f);
        b.Ell(Body, sc, V(0.1f, 0.15f, 0.15f), cream, mat: Shell);
        b.Torus(Body, sc + V(-0.03f, 0, 0), 0.1f, 0.055f, cream, V(0, 0, 90f), mat: Shell, blend: 0.02f);
        b.Torus(Body, sc + V(-0.075f, 0, 0), 0.055f, 0.035f, cream, V(0, 0, 90f), mat: Shell, blend: 0.015f);
        b.Ell(Body, sc + V(-0.11f, 0, 0), V(0.02f, 0.03f, 0.03f), cream, mat: Shell, blend: 0.01f);
        foreach (var (y, z) in new[] { (0.31f, -0.03f), (0.26f, 0.06f), (0.12f, 0.06f), (0.09f, -0.14f), (0.27f, -0.16f) })
            b.PaintEll(Body, V(0.04f, y, z), V(0.12f, 0.018f, 0.04f), stripe, V(0, 0, 0), 0.01f);
        foreach (var (y, z) in new[] { (0.24f, 0.0f), (0.16f, -0.09f) })
            b.PaintEll(Body, V(-0.08f, y, z), V(0.04f, 0.015f, 0.05f), stripe, V(30f, 0, 0), 0.008f);

        int head = b.Head(V(0, 0.08f, 0.17f));
        var c = V(0, 0.14f, 0.22f);
        var r = V(0.065f, 0.08f, 0.065f);
        b.Ell(head, c, r, body);
        PokeBuilder.Both(s =>
        {
            var ball = V(0.045f * s, 0.32f, 0.24f);
            b.Tube(head, new[] { V(0.025f * s, 0.18f, 0.23f), V(0.035f * s, 0.25f, 0.235f), ball }, 0.016f, 0.014f, body, blend: 0.008f);
            b.Ell(head, ball, V(0.033f, 0.033f, 0.033f), Rgb(236, 232, 220), blend: 0.008f);
            b.Eye(head, ball + V(0, 0, 0.03f), V(0.2f * s, 0.05f, 1f), 0.015f, sclera: true, pupil: Black);
            b.Tube(head, new[] { V(0.04f * s, 0.12f, 0.27f), V(0.08f * s, 0.1f, 0.3f), V(0.1f * s, 0.08f, 0.31f) }, 0.014f, 0.01f, body, blend: 0.006f);
        });
        b.Ground();
        return b;
    }
}
