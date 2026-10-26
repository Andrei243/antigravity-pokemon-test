using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// The species of our own that evolve past level 100 (plan 03 · D14), batch 4: Sinnoh's, in our numbers' order
// (2000 + the forebear's National number). Each is sculpted from its forebear's own sculpt as the look of
// docs/plans/D14-proposals-sinnoh.md and the user's reference picture say, built as batch 1's are
// (PokemonModels.Ours.Kanto.cs: Become, Recolour, Stretch, Grow, Move): the forebear's build taken whole where the
// body stays and the new features added to it. The forebears' own sculpts are never touched; a female keeps her
// forebear's difference by asking the forebear's sculptor for its female.
internal static partial class PokemonModels
{
    // ------------------------------------------------------------------ 2400 Dambarel, from Bibarel

    /// <summary>
    /// Dambarel: Bibarel heavier and standing taller, its teeth broader and pale gold, a ruff of wet dark fur at its
    /// neck, a twig caught behind one ear, clawed feet, and its tail a wide grey paddle scored like old planks.
    /// </summary>
    /// <param name="female">A female keeps Bibarel's: a smaller muzzle.</param>
    private static PokeBuilder Dambarel(bool female = false)
    {
        var b = Become(Bibarel(female), "Dambarel", 0.88f);
        var brown = Rgb(156, 104, 62);
        var dark = Rgb(98, 64, 40);
        var wet = Rgb(86, 62, 44);
        var gold = Rgb(234, 206, 120);
        var wood = Rgb(104, 100, 94);
        int head = BoneOf(b, "head");
        Recolour(b, (brown, Rgb(166, 110, 62)), (Rgb(226, 196, 142), Rgb(222, 188, 140)));
        // Broader teeth, pale gold
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == head && s.Shape == SdfShape.RoundBox && s.Color.Equals(White)).ToList())
        {
            s.Size = new Vector3(s.Size.X * 1.35f, s.Size.Y * 1.35f, s.Size.Z);
            s.A += V(Math.Sign(s.A.X) * 0.006f, -0.01f, 0);
            s.Color = gold;
        }
        // The tail becomes a broad paddle, flat and grey, scored like planks
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail);
        var pc = V(0, 0.18f, -0.56f);
        var pr = V(0.24f, 0.07f, 0.34f);
        b.Limb(tail, V(0, 0.2f, -0.2f), V(0, 0.14f, -0.32f), 0.1f, 0.07f, Rgb(166, 110, 62), blend: 0.03f);
        b.Ell(tail, pc, pr, wood, V(-24f, 0, 0), Shell, 0.02f);
        foreach (float x in new[] { -0.12f, -0.04f, 0.05f, 0.13f })
            b.PaintEll(tail, pc + V(x, 0.05f, 0.02f), V(0.006f, 0.06f, 0.3f), Rgb(70, 66, 62), V(-24f, 0, 0), 0.006f);
        foreach (var (x, z, a) in new[] { (-0.08f, -0.08f, 30f), (0.09f, 0.08f, -25f), (0.0f, -0.22f, 60f) })
            b.PaintEll(tail, pc + V(x, 0.06f, z), V(0.05f, 0.06f, 0.005f), Rgb(76, 72, 68), V(-24f, a, 0), 0.006f);
        // A ruff of wet dark fur round the neck, hanging over the chest and shoulders
        var rc = V(0, 0.6f, 0.02f);
        var rr = V(0.27f, 0.1f, 0.24f);
        b.Ell(Body, rc, rr, wet, blend: 0.03f);
        FurTufts(b, Body, rc, rr, 34, 0.08f, 0.028f, wet, 0.92f, -0.7f, 0.9f);
        foreach (float x in new[] { -0.1f, -0.03f, 0.04f, 0.1f })
            b.Spike(Body, V(x, 0.56f, 0.2f), V(x * 1.2f, 0.4f, 0.24f), 0.03f, wet, 0.5f);
        // A twig caught behind its right ear
        var twig = Rgb(112, 84, 58);
        b.Tube(head, new[] { V(0.05f, 0.92f, -0.12f), V(0.2f, 0.98f, -0.1f), V(0.34f, 1.04f, -0.06f) }, 0.009f, 0.006f, twig, Shell, 0.004f);
        b.Tube(head, new[] { V(0.24f, 1f, -0.09f), V(0.29f, 1.08f, -0.1f) }, 0.006f, 0.004f, twig, Shell, 0.003f);
        // Claws on its feet and hands
        PokeBuilder.Both(s =>
        {
            int foot = BoneOf(b, s < 0 ? "legL" : "legR");
            Claws(b, foot, V(0.14f * s, 0.03f, 0.23f), V(0.03f, 0, 0), V(0, -0.2f, 1f), 0.03f, 0.01f);
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            Claws(b, arm, V(0.26f * s, 0.3f, 0.22f), V(0.02f, 0, 0), V(0, -0.6f, 0.6f), 0.025f, 0.008f);
        });
        Stretch(b, V(1.08f, 1.08f, 1.06f));
        return b;
    }

    // ------------------------------------------------------------------ 2402 Maestrikket, from Kricketune

    /// <summary>
    /// Maestrikket: Kricketune taller and darker, its red deepened to crimson armour trimmed in black and silver, its
    /// moustache swept into great curls, its wings a long tail-coat to its ankles, a violin's scroll at its brow, a
    /// long steel blade in one hand and a bow strung in the other.
    /// </summary>
    /// <param name="female">A female keeps Kricketune's: a smaller moustache.</param>
    private static PokeBuilder Maestrikket(bool female = false)
    {
        var b = Become(Kricketune(female), "Maestrikket", 0.94f);
        var crimson = Rgb(150, 36, 52);
        var black = Rgb(52, 46, 56);
        var silver = Rgb(196, 198, 210);
        Recolour(b, (Rgb(226, 80, 44), crimson), (Rgb(244, 226, 170), black), (Rgb(96, 96, 110), black), (Rgb(128, 56, 34), silver),
            (Rgb(164, 80, 50), Rgb(110, 30, 44)), (Rgb(132, 132, 144), Rgb(176, 50, 66)));
        // The arms: both bows go, a sword in one hand and a strung bow in the other
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            b.Sdf.RemoveAll(sh => sh.Bone == arm);
            var shoulder = V(0.15f * s, 0.6f, 0.04f);
            var elbow = V(0.24f * s, 0.46f, 0.06f);
            var hand = V(0.27f * s, 0.32f, 0.1f);
            b.Ell(arm, shoulder + V(0.02f * s, 0.01f, 0), V(0.05f, 0.045f, 0.05f), crimson, mat: Shell);
            b.Limb(arm, shoulder, elbow, 0.026f, 0.024f, black, Shell);
            b.Ell(arm, elbow, V(0.03f, 0.03f, 0.03f), crimson, mat: Shell);
            b.Limb(arm, elbow, hand, 0.024f, 0.022f, silver, Metal);
            b.Ell(arm, hand, V(0.032f, 0.03f, 0.03f), black, mat: Shell);
            if (s > 0)
            {
                // A long straight sword held down and out
                var tip = hand + Vector3.Normalize(V(0.4f, -0.55f, 0.3f)) * 0.62f;
                b.Box(arm, hand + V(0, 0, 0.0f), V(0.045f, 0.01f, 0.012f), 0.004f, silver, Euler(tip - hand) + V(0, 0, 90f), Metal, 0.004f);
                Blade(b, arm, hand + Vector3.Normalize(tip - hand) * 0.03f, tip, 0.05f, Steel, V(0, 0, 1f), 0.18f, Metal);
            }
            else
            {
                // A tall bow strung beside it
                var top = hand + V(-0.06f, 0.42f, 0.02f);
                var foot = hand + V(-0.02f, -0.3f, 0.04f);
                var arc = Smooth(3, foot, hand + V(-0.09f, -0.12f, 0.04f), hand, hand + V(-0.12f, 0.22f, 0.03f), top);
                b.Tube(arm, arc, 0.012f, 0.008f, Rgb(110, 30, 44), Shell, 0.004f);
                b.Tube(arm, new[] { foot, top }, 0.003f, 0.003f, silver, Metal, 0f);
            }
        });
        // The wings grow into a long tail-coat, crimson lined in black, falling to its ankles
        PokeBuilder.Both(s =>
        {
            int w = BoneOf(b, s < 0 ? "wingL" : "wingR");
            Grow(b, V(1.05f, 1.35f, 1.1f), V(0.12f * s, 0.6f, -0.06f), bone => bone == w);
            b.PaintEll(w, V(0.22f * s, 0.3f, -0.0f), V(0.05f, 0.28f, 0.1f), crimson);
        });
        // A violin's scroll curling up from its brow in place of its crest
        int head = BoneOf(b, "head");
        var scroll = Spiral(V(0, 1.1f, 0.02f), V(0, 0, 1), V(0, 1, 0), 0.06f, 0.018f, MathF.PI, MathF.PI * 3.2f, 16);
        b.Tube(head, new[] { V(0, 0.95f, 0.0f), V(0, 1.03f, -0.02f) }.Concat(scroll).ToArray(), 0.024f, 0.012f, Rgb(110, 30, 44), Shell, 0.006f);
        PokeBuilder.Both(s => b.Spike(head, V(0.02f * s, 1.06f, -0.02f), V(0.06f * s, 1.07f, -0.02f), 0.012f, black, mat: Shell, blend: 0.003f));
        // Silver trim down the breast
        b.PaintEll(Body, V(0, 0.46f, 0.15f), V(0.016f, 0.2f, 0.06f), silver, soft: 0.006f);
        Stretch(b, V(0.92f, 1.14f, 0.92f));
        return b;
    }

    // ------------------------------------------------------------------ 2409 Cranialith, from Rampardos

    /// <summary>
    /// Cranialith: Rampardos larger and darker, slate blue under plates of grey-green stone, its dome grown into a ram's
    /// crown with two great curled horns and a ridge of stone spikes behind it, and its tail curled up into a club of
    /// rock.
    /// </summary>
    private static PokeBuilder Cranialith()
    {
        var b = Become(HeadButter(true), "Cranialith", 0.96f);
        var slate = Rgb(56, 68, 96);
        var stone = Rgb(108, 116, 108);
        var dome = Rgb(92, 110, 128);
        Recolour(b, (Rgb(70, 74, 88), slate), (Rgb(60, 152, 232), dome), (Rgb(236, 232, 214), stone));
        int head = BoneOf(b, "head");
        // The dome sinks lower into a crown of stone, ridged
        Grow(b, V(0.86f, 0.8f, 0.86f), V(0, 0.55f, 0.2f), s => s.Bone == head && s.Color.Equals(dome) && s.Op == SdfOp.Add, _ => false);
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == head && s.Color.Equals(dome)))
            s.Material = Scales;
        for (int i = -2; i <= 2; i++)
            b.Spike(head, V(i * 0.05f, 0.7f - Math.Abs(i) * 0.02f, 0.26f), V(i * 0.06f, 0.75f - Math.Abs(i) * 0.03f, 0.12f), 0.03f, dome, 0.45f, Shell);
        // Two great ram's horns curling round from the crown
        var c = V(0, 0.63f, 0.2f);
        PokeBuilder.Both(s => RamHorn(b, head, c + V(0.16f * s, 0.08f, -0.04f), s, 0.2f, 0.066f, stone));
        // Plates of stone over its chest, shoulders and thighs
        foreach (var (at, r) in new[] { (V(0, 0.38f, 0.14f), V(0.11f, 0.12f, 0.06f)), (V(0.13f, 0.48f, 0.08f), V(0.06f, 0.05f, 0.06f)), (V(-0.13f, 0.48f, 0.08f), V(0.06f, 0.05f, 0.06f)) })
            b.PaintEll(Body, at, r, stone);
        PokeBuilder.Both(s => b.PaintEll(BoneOf(b, s < 0 ? "legL" : "legR"), V(0.13f * s * 1.15f, 0.25f, 0.02f), V(0.07f, 0.06f, 0.06f), stone));
        // A ridge of stone spikes down the back
        for (int i = 0; i < 5; i++)
        {
            float z = 0.06f - i * 0.07f;
            var root = V(0, 0.56f - i * 0.025f, z);
            b.Spike(Body, root, root + V(0, 0.1f - i * 0.01f, -0.05f), 0.032f, stone, 0.5f, Shell);
        }
        // The tail curls up behind into a club of rock
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail);
        var path = Smooth(3, V(0, 0.36f, -0.18f), V(0, 0.26f, -0.4f), V(0, 0.3f, -0.62f), V(0, 0.48f, -0.7f), V(0, 0.64f, -0.62f));
        b.Tube(tail, path, 0.09f, 0.05f, slate, blend: 0.02f);
        b.PaintEll(tail, V(0, 0.2f, -0.42f), V(0.07f, 0.06f, 0.12f), stone);
        var club = V(0, 0.7f, -0.58f);
        b.Ell(tail, club, V(0.11f, 0.12f, 0.11f), stone, V(10f, 20f, 0), Shell, 0.02f);
        foreach (var d in new[] { V(1f, 0.4f, 0), V(-1f, 0.3f, 0.2f), V(0.2f, 1f, -0.3f), V(0.3f, 0.2f, -1f), V(-0.4f, 0.6f, 0.6f), V(0.5f, -0.4f, 0.6f) })
        {
            var n = Vector3.Normalize(d);
            b.Spike(tail, club + n * 0.09f, club + n * 0.15f, 0.03f, PixelCanvas.Shadow(stone, 0.1f), 0.7f, Shell);
        }
        Stretch(b, V(1.06f, 1.06f, 1.04f));
        return b;
    }

    // ------------------------------------------------------------------ 2411 Bastionsaur, from Bastiodon

    /// <summary>
    /// Bastionsaur: Bastiodon larger and browner, its face-shield widened into a gate of three steel plates under a
    /// rim of crenellations, horns at its sides, stone plates down its back, its legs in greaves and a hammer of
    /// stone at its tail's end.
    /// </summary>
    private static PokeBuilder Bastionsaur()
    {
        var b = Become(Bastiodon(), "Bastionsaur", 0.94f);
        var hide = Rgb(120, 98, 76);
        var steel = Rgb(86, 88, 96);
        var rim = Rgb(118, 92, 66);
        var stone = Rgb(104, 100, 96);
        Recolour(b, (Rgb(232, 200, 116), hide), (Rgb(150, 150, 156), Rgb(146, 116, 92)));
        int head = BoneOf(b, "head");
        // The windows go from the shield
        b.Model.Decals.RemoveAll(d => !d.IsEye && d.Bone == head);
        // A rim of bronze round the middle plate, and two side plates swung forward at its flanks
        b.Box(head, V(0, 0.42f, 0.13f), V(0.29f, 0.28f, 0.05f), 0.03f, rim, mat: Metal);
        PokeBuilder.Both(s =>
        {
            var at = V(0.42f * s, 0.4f, 0.06f);
            b.Box(head, at, V(0.13f, 0.27f, 0.05f), 0.03f, rim, V(0, -28f * s, 0), Metal);
            b.Box(head, at + V(0.01f * s, 0, 0.02f), V(0.11f, 0.25f, 0.045f), 0.03f, steel, V(0, -28f * s, 0), Metal, 0.004f);
            foreach (float t in new[] { -0.08f, 0.08f })
                b.Box(head, at + V(t * 0.88f * s, 0.29f, -t * 0.47f * s * s), V(0.03f, 0.03f, 0.04f), 0.008f, rim, V(0, -28f * s, 0), Metal, 0.006f);
        });
        // A row of crenellations along the middle plate's top, and the old battlements gone
        b.Sdf.RemoveAll(s => s.Bone == head && s.Shape == SdfShape.RoundBox && s.A.Y > 0.6f && s.Size.X < 0.06f);
        foreach (float x in new[] { -0.24f, -0.12f, 0f, 0.12f, 0.24f })
            b.Box(head, V(x, 0.72f, 0.14f), V(0.035f, 0.035f, 0.045f), 0.008f, rim, mat: Metal, blend: 0.006f);
        // The plates divided by dark seams
        PokeBuilder.Both(s => b.PaintEll(head, V(0.1f * s, 0.42f, 0.2f), V(0.006f, 0.26f, 0.05f), Rgb(48, 48, 54), soft: 0.006f));
        // Short horns at its cheeks, the old tusks drawn in
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == head && s.Shape == SdfShape.RoundCone && s.Color.Equals(Claw) && Math.Abs(s.B.X) > 0.3f).ToList())
            s.B = s.A + (s.B - s.A) * 0.75f;
        // Stone plates down the back and the flanks
        for (int i = 0; i < 5; i++)
        {
            float z = 0.02f - i * 0.09f;
            b.Spike(Body, V(0, 0.44f, z), V(0, 0.58f - i * 0.01f, z - 0.03f), 0.05f, stone, 0.35f, Shell);
            PokeBuilder.Both(s => b.Spike(Body, V(0.13f * s, 0.41f, z), V(0.19f * s, 0.5f, z - 0.02f), 0.035f, stone, 0.4f, Shell));
        }
        // Greaves on its legs
        foreach (var (z, front) in new[] { (0.02f, true), (-0.3f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = BoneOf(b, (front ? "leg" : "hind") + (s < 0 ? "L" : "R"));
                b.Torus(leg, V(0.17f * s, 0.1f, z + 0.01f), 0.07f, 0.02f, steel, mat: Metal, blend: 0.006f);
                b.Torus(leg, V(0.17f * s, 0.16f, z + 0.01f), 0.072f, 0.016f, rim, mat: Metal, blend: 0.006f);
            });
        // A longer tail, spiked, ending in a hammer of stone
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail);
        var path = Smooth(3, V(0, 0.28f, -0.38f), V(0, 0.26f, -0.52f), V(0, 0.34f, -0.66f), V(0, 0.48f, -0.72f));
        b.Tube(tail, path, 0.07f, 0.04f, hide, blend: 0.02f);
        for (int i = 2; i < path.Length - 1; i += 2)
            b.Spike(tail, path[i] + V(0, 0.03f, 0), path[i] + V(0, 0.08f, -0.02f), 0.02f, stone, 0.6f, Shell);
        b.Box(tail, V(0, 0.52f, -0.72f), V(0.1f, 0.05f, 0.05f), 0.015f, stone, mat: Shell, blend: 0.01f);
        Stretch(b, V(1.06f, 1.04f, 1.08f));
        return b;
    }

    // ------------------------------------------------------------------ 2414 Nocturmoth, from Mothim

    /// <summary>
    /// Nocturmoth: Mothim grown into a great brown moth, its wings broad and dark-rimmed with an eye of pale gold on
    /// each (Glow), its body a cloud of cream and orange fur, a segmented abdomen and two feathered antennae.
    /// </summary>
    private static PokeBuilder Nocturmoth()
    {
        var b = Become(Mothim(), "Nocturmoth", 0.94f);
        var lift = Shifted(b, "head", V(0, 0.5f, 0.01f));
        var wingFill = Rgb(200, 118, 56);
        var rim = Rgb(76, 44, 28);
        var orange = Rgb(214, 128, 62);
        var cream = Rgb(244, 226, 186);
        var brown = Rgb(130, 82, 48);
        var gold = Rgb(252, 228, 150);
        Recolour(b, (Rgb(252, 222, 128), wingFill), (PixelCanvas.Mix(Rgb(252, 222, 128), Rgb(244, 140, 42), 0.5f), rim),
            (Rgb(244, 140, 42), orange), (Rgb(78, 78, 90), brown), (White, cream));
        // Broader wings, an eye of pale gold on each
        PokeBuilder.Both(s =>
        {
            int w = BoneOf(b, s < 0 ? "wingL" : "wingR");
            var shoulder = V(0.05f * s, 0.48f, -0.02f) + lift;
            b.Sdf.RemoveAll(sh => sh.Bone == w && sh.Shape == SdfShape.RoundBox);
            Grow(b, V(1.55f, 1.45f, 1f), shoulder, bone => bone == w);
            foreach (var (at, r, roll) in new[] { (V(0.4f * s, 0.61f, -0.03f), V(0.07f, 0.04f, 0.03f), 14f * s), (V(0.23f * s, 0.32f, -0.035f), V(0.045f, 0.03f, 0.03f), -38f * s) })
            {
                PaintAs(b, w, at + lift, r * 1.45f, rim, Fur, V(0, 0, roll));
                PaintAs(b, w, at + lift, r, gold, Glow, V(0, 0, roll));
            }
            // A pale band across the forewing, inside its dark rim
            b.PaintEll(w, V(0.5f * s, 0.66f, -0.03f) + lift, V(0.015f, 0.12f, 0.03f), gold, V(0, 0, 20f * s), 0.008f);
        });
        // A fat body: a cloud of fur at the thorax, a ringed abdomen hanging below
        b.Sdf.RemoveAll(s => s.Bone == Body);
        var thorax = V(0, 0.46f, 0.0f) + lift;
        b.Ell(Body, thorax, V(0.12f, 0.1f, 0.1f), orange);
        b.PaintEll(Body, thorax + V(0, 0.01f, 0.07f), V(0.09f, 0.08f, 0.05f), cream);
        FurTufts(b, Body, thorax, V(0.12f, 0.1f, 0.1f), 30, 0.05f, 0.024f, cream, 0.85f, -0.5f, 0.2f);
        var abdomen = new[] { V(0, 0.37f, -0.02f), V(0, 0.3f, -0.03f), V(0, 0.23f, -0.03f), V(0, 0.17f, -0.02f) };
        for (int i = 0; i < abdomen.Length; i++)
            b.Ell(Body, abdomen[i] + lift, V(0.075f - i * 0.012f, 0.045f, 0.07f - i * 0.01f), brown, blend: 0.012f);
        // A bigger head, the face orange, and two feathered antennae sweeping up from it
        int head = BoneOf(b, "head");
        Grow(b, V(1.3f, 1.3f, 1.3f), V(0, 0.5f, 0.01f) + lift, bone => bone == head);
        PokeBuilder.Both(s =>
        {
            int ear = BoneOf(b, s < 0 ? "earL" : "earR");
            b.Sdf.RemoveAll(sh => sh.Bone == ear);
            var root = V(0.03f * s, 0.62f, 0.02f) + lift;
            var tip = V(0.13f * s, 0.9f, -0.04f) + lift;
            b.Tube(ear, Smooth(3, root, V(0.07f * s, 0.75f, 0.0f) + lift, tip), 0.008f, 0.005f, rim, blend: 0.004f);
            var along = tip - root;
            var side = Vector3.Normalize(Vector3.Cross(along, V(0, 0, 1f)));
            for (int i = 1; i < 9; i++)
            {
                float t = i / 9f;
                var at = root + along * (0.25f + 0.75f * t);
                float len = 0.05f * MathF.Sin(MathF.PI * (0.15f + 0.85f * t));
                foreach (float k in new[] { -1f, 1f })
                    b.Spike(ear, at, at + side * k * len + Vector3.Normalize(along) * len * 0.5f, 0.008f, brown, 0.5f, blend: 0.003f);
            }
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2416 Hivempress, from Vespiquen

    /// <summary>
    /// Hivempress: Vespiquen taller, its skirt grown into a gown of honeycomb in two tiers with honey glowing in some
    /// cells (Glow, amber), a ruff of golden fur at its neck, a tall dark crown set with glowing cells and long wings
    /// of amber trailing behind like a train.
    /// </summary>
    private static PokeBuilder Hivempress()
    {
        var b = Become(Vespiquen(), "Hivempress", 0.96f);
        var lift = Shifted(b, "head", V(0, 0.68f, 0.01f));
        var honey = Rgb(250, 176, 40);
        var comb = PixelCanvas.Mix(honey, Rgb(220, 120, 20), 0.35f);
        var dark = Rgb(66, 64, 74);
        var amber = Rgb(255, 168, 36);
        var gold = Rgb(236, 190, 66);
        Recolour(b, (honey, Rgb(222, 160, 48)));
        // The dark bands of the bodice go: the gown is comb from the waist down
        b.Sdf.RemoveAll(s => s.Bone == Body && s.Op == SdfOp.Paint && s.Color.Equals(dark));
        // The second tier of the gown, wider, falling under the first
        var tier = V(0, 0.1f, 0) + lift;
        b.Ell(Body, tier, V(0.36f, 0.16f, 0.32f), Rgb(208, 146, 44), blend: 0.03f);
        for (int row = 0; row < 2; row++)
            for (int i = 0; i < 14; i++)
            {
                float a = (i + row * 0.5f) * MathF.Tau / 14f;
                float y = 0.13f - row * 0.08f;
                float k = MathF.Sqrt(Math.Max(0.1f, 1f - MathF.Pow((y - 0.1f) / 0.16f, 2f)));
                var n = V(MathF.Sin(a), 0, MathF.Cos(a));
                var at = V(n.X * 0.35f * k, y, n.Z * 0.31f * k) + lift;
                bool lit = (i * 7 + row * 3) % 5 == 0;
                As(b, lit ? Glow : Shell, () => HexCell(b, Body, at, n + V(0, -0.2f, 0), 0.05f, 0.03f, lit ? amber : comb, 30f, 0.004f));
            }
        // Some cells of the upper tier glow with honey
        int litCount = 0;
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == Body && s.Shape == SdfShape.RoundBox && s.Color.Equals(comb) && s.A.Y > 0.12f + lift.Y).ToList())
            if (litCount++ % 9 == 0) { s.Color = amber; s.Material = Glow; }
        // A ruff of golden fur round the neck
        var rc = V(0, 0.63f, 0) + lift;
        b.Ell(Body, rc, V(0.11f, 0.05f, 0.1f), gold, blend: 0.02f);
        FurTufts(b, Body, rc, V(0.11f, 0.05f, 0.1f), 26, 0.045f, 0.02f, gold, 0.95f, -0.5f, 0.2f);
        // A tall dark crown of points, glowing cells set in it
        int head = BoneOf(b, "head");
        var crown = V(0, 0.86f, 0.0f) + lift;
        foreach (var (x, h, z) in new[] { (0f, 0.2f, 0f), (-0.05f, 0.15f, 0.01f), (0.05f, 0.15f, 0.01f), (-0.08f, 0.1f, -0.03f), (0.08f, 0.1f, -0.03f) })
            b.Spike(head, crown + V(x, 0, z), crown + V(x * 1.6f, h, z - 0.02f), 0.035f, dark, 0.5f, Shell);
        foreach (var (x, y) in new[] { (0f, 0.07f), (-0.035f, 0.04f), (0.035f, 0.04f) })
            As(b, Glow, () => HexCell(b, head, crown + V(x, y, 0.035f), V(0, 0, 1f), 0.018f, 0.008f, amber, 30f, 0f));
        // Long wings of amber trailing behind like a train
        PokeBuilder.Both(s =>
        {
            int w = BoneOf(b, s < 0 ? "wingL" : "wingR");
            Recolour(b, (Rgb(236, 240, 248), Rgb(250, 222, 150)));
            Grow(b, V(1.2f, 1.6f, 1.4f), V(0.06f * s, 0.66f, -0.06f) + lift, bone => bone == w);
            Petal(b, w, V(0.06f * s, 0.62f, -0.08f) + lift, V(0.24f * s, 0.12f, -0.26f) + lift, 0.07f, Rgb(250, 222, 150), Shell, 0.01f);
        });
        Stretch(b, V(1f, 1.12f, 1f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2417 Voltglider, from Pachirisu

    /// <summary>
    /// Voltglider: Pachirisu longer and standing taller, its arms spread on a blue gliding membrane to its ankles with
    /// a bolt of light along each edge (Glow, yellow), its ears longer and tipped in sparks, and its tail far larger.
    /// </summary>
    /// <param name="female">A female keeps Pachirisu's: a shorter stripe on her head.</param>
    private static PokeBuilder Voltglider(bool female = false)
    {
        var b = Become(Pachirisu(female), "Voltglider", 0.94f);
        var white = Rgb(240, 244, 252);
        var blue = Rgb(86, 136, 206);
        var spark = Rgb(255, 238, 96);
        Recolour(b, (Rgb(246, 246, 250), white), (Rgb(110, 200, 236), blue));
        Stretch(b, V(1f, 1.2f, 1f));
        float k = 1.2f;
        // Arms spread wide
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            b.Sdf.RemoveAll(sh => sh.Bone == arm);
            var shoulder = V(0.07f * s, 0.27f * k, 0.02f);
            var wrist = V(0.27f * s, 0.36f * k, 0.03f);
            b.Limb(arm, shoulder, wrist, 0.028f, 0.022f, white);
            Claws(b, arm, wrist + V(0.015f * s, 0.01f, 0), V(0, 0.012f, 0), V(s, 0.4f, 0), 0.025f, 0.007f);
        });
        // The gliding membrane, wrist to ankle, its edge lit by a bolt
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.07f * s, 0.29f * k, 0.0f);
            var wrist = V(0.26f * s, 0.355f * k, 0.0f);
            var hip = V(0.08f * s, 0.12f * k, 0.0f);
            var ankle = V(0.1f * s, 0.04f * k, 0.0f);
            var membrane = Rgb(94, 140, 204);
            Panel(b, Body, shoulder, wrist, hip, 0f, membrane);
            Panel(b, Body, wrist, ankle, hip, 0f, membrane);
            Panel(b, Body, wrist, V(0.2f * s, 0.14f * k, 0f), ankle, 0f, membrane);
            var bolt = new[] { wrist + V(-0.01f * s, -0.03f, 0.012f), V(0.22f * s, 0.3f * k, 0.012f), V(0.25f * s, 0.25f * k, 0.012f), V(0.18f * s, 0.18f * k, 0.012f), V(0.2f * s, 0.14f * k, 0.012f), ankle + V(0.02f * s, 0.03f, 0.012f) };
            b.Tube(Body, bolt, 0.008f, 0.006f, spark, Glow, 0.002f);
        });
        // Longer ears, sparks at their tips
        PokeBuilder.Both(s =>
        {
            int ear = BoneOf(b, s < 0 ? "earL" : "earR");
            Grow(b, V(1f, 1.4f, 1f), V(0.07f * s, 0.5f * k, 0), bone => bone == ear);
            var tip = V(0.17f * s, 0.68f * k, -0.035f);
            foreach (var d in new[] { V(0.3f * s, 1f, 0), V(-0.4f * s, 1f, 0), V(0.9f * s, 0.6f, 0) })
                b.Spike(ear, tip - V(0, 0.02f, 0), tip + Vector3.Normalize(d) * 0.05f, 0.012f, spark, 0.5f, Glow, 0.004f);
        });
        // A far larger tail, a second blue stripe beside the first
        int tail = BoneOf(b, "tail");
        Grow(b, V(1.2f, 1.15f, 1.2f), V(0, 0.16f * k, -0.07f), bone => bone == tail);
        b.PaintEll(tail, V(0.06f, 0.5f * k, -0.34f), V(0.03f, 0.2f, 0.12f), blue);
        return b;
    }

    // ------------------------------------------------------------------ 2419 Tidestoat, from Floatzel

    /// <summary>
    /// Tidestoat: Floatzel down on all fours and longer, an otter of the sea: its float grown into a full ring round
    /// its chest, banded in cream, its twin tails long and each ending in a propeller of three fins, pale stripes
    /// along its back (the female's one) and a blue line down its flanks.
    /// </summary>
    /// <param name="female">A female keeps Floatzel's: one pale stripe on her back.</param>
    private static PokeBuilder Tidestoat(bool female = false)
    {
        var b = new PokeBuilder("Tidestoat", 0.9f, BodyPlan.Quadruped, V(0, 0.28f, -0.04f)) { Coat = Fur };
        var orange = Rgb(236, 138, 56);
        var cream = Rgb(250, 232, 176);
        var blue = Rgb(56, 108, 168);
        var dark = Rgb(60, 40, 32);
        foreach (var (z, front) in new[] { (0.2f, true), (-0.24f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.09f * s, 0.24f, z), front);
                b.Limb(leg, V(0.09f * s, 0.24f, z), V(0.1f * s, 0.05f, z + 0.02f), 0.05f, 0.038f, orange);
                b.Ell(leg, V(0.1f * s, 0.022f, z + 0.06f), V(0.045f, 0.022f, 0.07f), orange);
                Claws(b, leg, V(0.1f * s, 0.02f, z + 0.12f), V(0.018f, 0, 0), V(0, -0.2f, 1f), 0.02f, 0.006f);
                if (front) b.Spike(leg, V(0.12f * s, 0.16f, z - 0.02f), V(0.16f * s, 0.2f, z - 0.12f), 0.035f, blue, 0.3f);
            });
        var bc = V(0, 0.28f, -0.03f);
        var br = V(0.12f, 0.11f, 0.32f);
        b.Ell(Body, bc, br, orange);
        b.PaintEll(Body, V(0, 0.2f, 0.06f), V(0.09f, 0.07f, 0.26f), cream);
        PokeBuilder.Both(s => b.PaintEll(Body, V(0.105f * s, 0.25f, -0.02f), V(0.03f, 0.012f, 0.26f), blue, soft: 0.008f));
        foreach (float x in female ? new[] { 0f } : new[] { -0.04f, 0.04f })
            b.PaintEll(Body, V(x, 0.39f, -0.06f), V(0.018f, 0.03f, 0.22f), cream, soft: 0.008f);
        b.Mark(Body, Out(bc, br, default, V(0.4f, 0.8f, -0.4f)), V(0.4f, 0.8f, -0.4f), 0.022f, 0.02f, Rgb(80, 50, 40));
        // The neck rising in front, a cream throat
        b.Limb(Body, V(0, 0.32f, 0.2f), V(0, 0.52f, 0.3f), 0.08f, 0.065f, orange, blend: 0.03f);
        b.PaintEll(Body, V(0, 0.44f, 0.32f), V(0.05f, 0.1f, 0.05f), cream);
        // The float, a full ring round its chest, banded cream
        var ring = V(0, 0.4f, 0.24f);
        b.Torus(Body, ring, 0.12f, 0.055f, orange, V(55f, 0, 0), mat: Shell, blend: 0.012f);
        foreach (float a in new[] { 40f, 100f, 160f, 220f, 280f, 340f })
        {
            float r = a * Degree;
            var on = ring + Vector3.Transform(V(MathF.Cos(r) * 0.12f, 0, MathF.Sin(r) * 0.12f), Quaternion.CreateFromYawPitchRoll(0, 55f * Degree, 0));
            b.PaintEll(Body, on, V(0.03f, 0.07f, 0.07f), cream, V(55f, -a + 90f, 0));
        }
        // Two long tails, each ending in a propeller of three fins
        int tail = b.Tail(V(0, 0.3f, -0.32f));
        PokeBuilder.Both(s =>
        {
            var root = V(0, 0.3f, -0.32f);
            var tip = s > 0 ? V(0.16f, 0.5f, -0.78f) : V(-0.14f, 0.28f, -0.82f);
            var path = Smooth(3, root, V(0.06f * s, 0.3f, -0.48f), V(0.1f * s, s > 0 ? 0.38f : 0.26f, -0.64f), tip);
            b.Tube(tail, path, 0.05f, 0.022f, orange, blend: 0.02f);
            var axis = Vector3.Normalize(tip - path[^3]);
            var u = Vector3.Normalize(Vector3.Cross(axis, V(0, 1f, 0)));
            var w = Vector3.Cross(axis, u);
            for (int i = 0; i < 3; i++)
            {
                float a = i * MathF.Tau / 3f + 0.4f;
                var d = u * MathF.Cos(a) + w * MathF.Sin(a);
                Blade(b, tail, tip, tip + d * 0.11f - axis * 0.02f, 0.05f, i == 0 ? orange : blue, axis, 0.25f);
            }
        });
        int head = b.Head(V(0, 0.54f, 0.3f));
        var hc = V(0, 0.6f, 0.33f);
        var hr = V(0.085f, 0.08f, 0.085f);
        b.Ell(head, hc, hr, orange);
        b.Ell(head, V(0, 0.57f, 0.4f), V(0.05f, 0.038f, 0.05f), cream);
        b.Ell(head, V(0, 0.585f, 0.45f), V(0.016f, 0.013f, 0.012f), dark, blend: 0.005f);
        PokeBuilder.Both(s =>
        {
            b.PaintEll(head, V(0.045f * s, 0.62f, 0.39f), V(0.035f, 0.016f, 0.03f), Rgb(150, 82, 50), V(0, 0, -15f * s));
            int ear = b.Ear(head, s, V(0.06f * s, 0.66f, 0.3f));
            b.Ell(ear, V(0.07f * s, 0.675f, 0.3f), V(0.025f, 0.024f, 0.014f), orange);
        });
        PokeBuilder.Both(s => b.Eye(head, On(hc, hr, 0.042f * s, 0.62f), V(0.5f * s, 0.05f, 1f), 0.022f));
        return b;
    }

    // ------------------------------------------------------------------ 2421 Sakurabloom, from Cherrim

    /// <summary>
    /// Sakurabloom: Cherrim open in the sun and staying so, its face pale pink, its blossom grown into two rings of
    /// pink petals round it, a crown of small blossoms on its brow, a skirt of green leaves and green legs.
    /// </summary>
    private static PokeBuilder Sakurabloom()
    {
        var b = Become(CherrimSunshine(), "Sakurabloom", 0.86f);
        var face = Rgb(250, 222, 222);
        var petal = Rgb(246, 168, 196);
        var deep = Rgb(236, 128, 170);
        var green = Rgb(98, 160, 78);
        var cherry = Rgb(222, 64, 100);
        b.Sdf.RemoveAll(s => s.Color.Equals(cherry));
        Recolour(b, (Rgb(250, 226, 112), face), (Rgb(246, 178, 196), petal));
        PokeBuilder.Both(s =>
        {
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            foreach (var sh in b.Sdf.Shapes.Where(sh => sh.Bone == leg)) sh.Color = green;
        });
        // A second, larger ring of deeper petals behind the first
        int top = BoneOf(b, "petals");
        for (int i = 0; i < 7; i++)
        {
            float a = (i + 0.5f) * MathF.Tau / 7f;
            var dir = V(MathF.Sin(a), MathF.Cos(a), 0);
            var root = V(0, 0.34f, -0.1f) + dir * 0.1f;
            var tip = V(0, 0.34f, 0.04f) + dir * 0.36f;
            Frond(b, top, root, tip, 0.1f, deep, Vector3.Normalize(V(0, 0, 1f) - dir * 0.5f), 0.2f, Leaf);
        }
        // An inner ring of small petals cupping the face
        for (int i = 0; i < 6; i++)
        {
            float a = (i - 2.5f) * 0.55f;
            var dir = V(MathF.Sin(a), -MathF.Cos(a) * 0.4f + 0.2f, 0);
            Petal(b, Body, V(0, 0.3f, 0.06f) + dir * 0.08f, V(0, 0.3f, 0.1f) + dir * 0.17f, 0.05f, petal, Leaf, 0.008f);
        }
        // A crown of small blossoms on its brow
        foreach (var (x, y, z) in new[] { (0f, 0.45f, 0.06f), (-0.065f, 0.43f, 0.05f), (0.065f, 0.43f, 0.05f), (-0.11f, 0.4f, 0.03f), (0.11f, 0.4f, 0.03f) })
        {
            var c = V(x, y, z);
            for (int k = 0; k < 5; k++)
            {
                float a = k * MathF.Tau / 5f;
                b.Ell(Body, c + V(MathF.Sin(a) * 0.018f, MathF.Cos(a) * 0.018f, 0.004f), V(0.016f, 0.016f, 0.008f), Rgb(252, 214, 228), mat: Leaf, blend: 0.004f);
            }
            b.Ell(Body, c + V(0, 0, 0.01f), V(0.008f, 0.008f, 0.006f), Rgb(250, 220, 110), blend: 0.002f);
            b.Spike(Body, c + V(0, -0.01f, -0.01f), c + V(x * 0.4f + 0.02f, 0.05f, -0.02f), 0.008f, green, 0.4f, Leaf, 0.003f);
        }
        // Rosy cheeks
        var fc = V(0, 0.32f, 0.02f);
        var fr = V(0.13f, 0.12f, 0.11f);
        PokeBuilder.Both(s => b.PaintEll(Body, On(fc, fr, 0.075f * s, 0.3f), V(0.022f, 0.014f, 0.02f), Rgb(244, 160, 170), soft: 0.008f));
        // A skirt of green leaves beneath the blossom
        for (int i = 0; i < 7; i++)
        {
            float a = (i - 3) * 0.5f;
            var root = V(MathF.Sin(a) * 0.08f, 0.17f, MathF.Cos(a) * 0.05f - 0.02f);
            var tip = root + V(MathF.Sin(a) * 0.14f, -0.08f, MathF.Cos(a) * 0.05f);
            Frond(b, Body, root, tip, 0.04f, green, V(0, 1f, 0.3f), 0.2f, Leaf);
        }
        Stretch(b, V(1.1f, 1.1f, 1.1f));
        return b;
    }

    // ------------------------------------------------------------------ 2423 Gastromire, from Gastrodon

    /// <summary>
    /// Gastromire: Gastrodon larger and one colour, deep sea teal over an olive foot, two long curved horns, a mantle
    /// of ruffles down its back with paler edges, and rings and dots of pale yellow along its back.
    /// </summary>
    private static PokeBuilder Gastromire()
    {
        var b = Become(SeaSlug(true), "Gastromire", 0.86f);
        const float k = 1.3f;
        var teal = Rgb(40, 104, 116);
        var light = Rgb(64, 130, 140);
        var foot = Rgb(150, 140, 100);
        var rim = Rgb(124, 116, 82);
        var spot = Rgb(226, 214, 160);
        Recolour(b, (Rgb(150, 98, 70), teal), (Rgb(242, 140, 176), light), (Rgb(244, 160, 186), foot), (Rgb(250, 222, 90), rim));
        int head = BoneOf(b, "head");
        float hy = 0.28f * k;
        // The lobes become two long horns curving up and back
        b.Sdf.RemoveAll(s => s.Bone == head && s.Color.Equals(teal) && Math.Abs(s.A.X) > 0.03f);
        PokeBuilder.Both(s =>
        {
            var root = V(0.05f * s * k, hy + 0.06f * k, 0.1f * k);
            b.Tube(head, Smooth(3, root, V(0.08f * s, hy + 0.22f, 0.12f), V(0.1f * s, hy + 0.36f, 0.06f), V(0.13f * s, hy + 0.44f, -0.04f)), 0.036f, 0.01f, teal, blend: 0.012f);
        });
        // A mantle of ruffles down the back, layer over layer, their edges paler
        var path = new[] { V(0, 0.07f * k, -0.24f * k), V(0, 0.08f * k, -0.12f * k), V(0, 0.1f * k, 0.0f), V(0, 0.18f * k, 0.08f * k) };
        for (int i = 0; i < 3; i++)
        {
            int seg = i == 0 ? Body : BoneOf(b, "seg" + i);
            float r = (0.08f + 0.012f * i) * k;
            PokeBuilder.Both(s =>
            {
                foreach (float dz in new[] { -0.05f, 0.03f })
                {
                    var at = path[i] + V(r * 0.6f * s, r * 1.5f, dz);
                    b.Ell(seg, at, V(0.018f, r * 0.75f, r * 0.7f), teal, V(0, 0, -40f * s), blend: 0.012f);
                    b.PaintEll(seg, at + V(r * 0.3f * s, r * 0.5f, 0), V(0.03f, 0.03f, r * 0.8f), Rgb(110, 160, 160), V(0, 0, -40f * s), 0.01f);
                }
            });
            // Rings and dots of pale yellow
            var top = path[i] + V(0, r * 1.25f, 0);
            PokeBuilder.Both(s => b.PaintTorus(seg, path[i] + V(r * 0.85f * s, r * 0.9f, 0.02f), 0.024f, 0.006f, spot, V(0, 0, -50f * s)));
            b.PaintEll(seg, top + V(0, 0.02f, 0.03f), V(0.012f, 0.012f, 0.012f), spot, soft: 0.006f);
        }
        PokeBuilder.Both(s => b.PaintTorus(head, V(0.07f * s * k, hy + 0.06f * k, 0.1f * k), 0.018f, 0.005f, spot, V(0, 0, -40f * s)));
        Stretch(b, V(1.12f, 1.12f, 1.2f));
        return b;
    }

    // ------------------------------------------------------------------ 2424 Quadripom, from Ambipom

    /// <summary>
    /// Quadripom: Ambipom with four tails, each ending in a great cream hand, its tuft grown into a tall plume of
    /// purple hair (the female's taller still) and its grin wider.
    /// </summary>
    /// <param name="female">A female keeps Ambipom's: a longer tuft.</param>
    private static PokeBuilder Quadripom(bool female = false)
    {
        var b = Become(Ambipom(female), "Quadripom", 0.94f);
        var purple = Rgb(132, 96, 168);
        var hand = Rgb(246, 230, 176);
        Recolour(b, (Rgb(170, 116, 206), purple), (Rgb(250, 224, 132), hand), (Rgb(240, 102, 72), hand));
        // Two more tails, reaching low to either side
        int tail = BoneOf(b, "tail");
        PokeBuilder.Both(s =>
        {
            b.Tube(tail, new[] { V(0, 0.22f, -0.06f), V(0.12f * s, 0.18f, -0.14f), V(0.28f * s, 0.14f, -0.1f), V(0.4f * s, 0.2f, -0.02f), V(0.45f * s, 0.3f, 0.02f) }, 0.026f, 0.022f, purple);
            TailHand(b, tail, V(0.46f * s, 0.35f, 0.03f), V(0.3f * s, 1f, 0.3f), 0.055f, hand, hand);
        });
        // A tall plume of hair swept up and back from its crown
        int head = BoneOf(b, "head");
        float tall = female ? 1.2f : 1f;
        foreach (var (x, h, back, w) in new[] { (0f, 0.38f, 0.1f, 0.07f), (-0.03f, 0.3f, 0.12f, 0.05f), (0.03f, 0.3f, 0.13f, 0.05f), (0.0f, 0.24f, 0.18f, 0.045f) })
            Blade(b, head, V(x, 0.64f, -0.02f), V(x * 2f, 0.64f + h * tall, -back * tall), w, PixelCanvas.Shadow(purple, 0.25f), V(0, 0.3f, 1f), 0.25f);
        // A wider grin
        foreach (var d in b.Model.Decals.Where(d => d.Bone == head && d.Shape == MarkShape.Bar))
        {
            d.Size *= 1.5f;
        }
        Stretch(b, V(1.06f, 1.08f, 1.06f));
        return b;
    }

    // ------------------------------------------------------------------ 2426 Dirigeist, from Drifblim

    /// <summary>
    /// Dirigeist: Drifblim stretched into an airship of a ghost, its envelope long and ribbed under a net of pale cords,
    /// a cloud trailing over its back, a face of glowing eyes at its nose, a gondola of tattered cloth slung beneath
    /// with eyes of its own, and its four arms grown into long streamers.
    /// </summary>
    private static PokeBuilder Dirigeist()
    {
        var b = Become(Drifblim(), "Dirigeist", 0.98f);
        var lift = Shifted(b, "body", V(0, 0.6f, 0));
        var purple = Rgb(96, 76, 140);
        var lilac = Rgb(196, 182, 226);
        var cloth = Rgb(52, 46, 72);
        var glow = Rgb(250, 244, 170);
        Recolour(b, (Rgb(112, 84, 166), purple), (Rgb(232, 70, 92), lilac), (Rgb(232, 60, 80), glow), (Rgb(250, 214, 64), lilac));
        var c = V(0, 0.6f, 0) + lift;
        // The envelope stretched long along its length; its face and eyes go with it to its nose
        b.Model.Decals.RemoveAll(d => !d.IsEye);
        Grow(b, V(1f, 0.82f, 1.8f), c, s => s.Bone == Body && s.Op == SdfOp.Add || s.Bone == Body && s.Op == SdfOp.Paint, bone => bone == Body);
        foreach (var d in b.Model.Decals.Where(d => d.IsEye)) d.Glare = true;
        var rx = 0.24f;
        var ry = 0.26f * 0.82f;
        var rz = 0.23f * 1.8f;
        // Ribs round it and cords along it
        foreach (float t in new[] { -0.7f, -0.42f, -0.14f, 0.14f, 0.42f })
        {
            float z = t * rz;
            float f = MathF.Sqrt(1f - t * t);
            b.Torus(Body, c + V(0, 0, z), rx * f, 0.01f, lilac, V(90f, 0, 0), 1f, ry / rx, blend: 0.004f);
        }
        foreach (float a in new[] { 30f, 90f, 150f })
        {
            float r = a * Degree;
            var pts = Enumerable.Range(0, 13).Select(i =>
            {
                float t = -0.92f + 1.6f * i / 12f;
                float f = MathF.Sqrt(Math.Max(0f, 1f - t * t)) * 1.03f;
                return c + V(MathF.Cos(r) * rx * f, MathF.Sin(r) * ry * f, t * rz);
            }).ToArray();
            b.Tube(Body, pts, 0.008f, 0.008f, lilac, blend: 0.003f);
        }
        // A stitched grin under the eyes at its nose
        var nose = c + V(0, -0.04f, rz * 0.97f);
        b.Tube(Body, Smooth(3, nose + V(-0.1f, 0.02f, -0.04f), nose + V(0, -0.03f, 0.005f), nose + V(0.1f, 0.02f, -0.04f)), 0.006f, 0.006f, Rgb(40, 32, 56), blend: 0.002f);
        // The cloud trailing over its back
        b.Sdf.RemoveAll(s => s.Bone == Body && s.Color.Equals(White));
        foreach (var (z, r) in new[] { (0.22f, 0.06f), (0.06f, 0.075f), (-0.12f, 0.07f), (-0.3f, 0.055f) })
            Puff(b, Body, c + V(0, ry * 0.95f, z), r);
        Recolour(b, (White, Rgb(226, 218, 242)));
        // Wisps of ghost trailing from its stern
        foreach (var (dx, dy) in new[] { (0f, 0.08f), (0.1f, -0.02f), (-0.1f, -0.02f), (0f, -0.1f) })
            Blade(b, Body, c + V(dx * 0.5f, dy * 0.5f, -rz * 0.9f), c + V(dx * 1.5f, dy * 1.4f, -rz - 0.2f), 0.05f, lilac, V(dy, -dx, 0.01f), 0.2f);
        // The gondola of tattered cloth slung beneath on cords, eyes glowing in it
        var g = c + V(0, -0.38f, 0.02f);
        b.Box(Body, g, V(0.1f, 0.05f, 0.14f), 0.03f, cloth, blend: 0.004f);
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f;
            var at = g + V(MathF.Sin(a) * 0.09f, -0.03f, MathF.Cos(a) * 0.12f);
            b.Spike(Body, at, at + V(MathF.Sin(a) * 0.03f, -0.14f - 0.04f * (i % 3), MathF.Cos(a) * 0.03f), 0.035f, i % 2 == 0 ? cloth : Rgb(110, 90, 150), 0.4f, blend: 0.006f);
        }
        foreach (float x in new[] { -0.05f, 0.05f })
            PokeBuilder.Both(s => b.Ell(Body, g + V(x + 0.012f * s, 0.005f, 0.145f), V(0.009f, 0.013f, 0.006f), glow, mat: Glow, blend: 0f));
        foreach (var (x, z) in new[] { (-0.08f, 0.1f), (0.08f, 0.1f), (-0.08f, -0.1f), (0.08f, -0.1f) })
            b.Tube(Body, new[] { g + V(x, 0.04f, z), c + V(x * 1.6f, -ry * 0.82f, z * 1.8f) }, 0.006f, 0.006f, lilac, blend: 0.003f);
        // The four arms lengthen into streamers
        foreach (var name in new[] { "armL", "armR", "backArmL", "backArmR" })
        {
            int arm = BoneOf(b, name);
            float s = name.EndsWith("L") ? -1f : 1f;
            float z = name.StartsWith("back") ? -0.1f : 0.1f;
            Grow(b, V(1.15f, 1.3f, 1.6f), V(0.1f * s, 0.38f, z) + lift, bone => bone == arm);
        }
        Recolour(b, (Rgb(240, 236, 246), Rgb(206, 196, 232)));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2428 Lapinova, from Lopunny

    /// <summary>
    /// Lapinova: Lopunny taller and slimmer, its ears longer still, the cream fluff at their roots grown into long
    /// ribbons of cream and brown streaming to the ground, cream fur at its legs like leg-warmers and a cream breast.
    /// </summary>
    private static PokeBuilder Lapinova()
    {
        var b = Become(Lopunny(), "Lapinova", 0.94f);
        var brown = Rgb(146, 92, 60);
        var cream = Rgb(246, 228, 172);
        Recolour(b, (Rgb(150, 100, 70), brown));
        int head = BoneOf(b, "head");
        float hy = 0.82f;
        // The ear fluff goes; long ribbons stream from the ears' roots to the ground
        b.Sdf.RemoveAll(s => s.Bone == head && s.Color.Equals(cream) && s.Shape == SdfShape.Ellipsoid);
        PokeBuilder.Both(s =>
        {
            foreach (var (dx, dz, low, color) in new[] { (0.0f, 0.0f, 0.1f, cream), (0.05f, -0.05f, 0.22f, brown) })
            {
                var path = Smooth(3, V(0.09f * s, hy + 0.04f, -0.02f), V((0.2f + dx) * s, hy - 0.12f, -0.06f + dz), V((0.26f + dx) * s, hy - 0.36f, -0.04f + dz),
                    V((0.2f + dx) * s, hy - 0.56f, 0.02f + dz), V((0.28f + dx) * s, low + 0.06f, -0.04f + dz), V((0.36f + dx) * s, low, -0.02f + dz));
                b.Tube(head, path, 0.03f, 0.014f, color, blend: 0.006f);
            }
        });
        // Longer ears
        PokeBuilder.Both(s =>
        {
            int ear = BoneOf(b, s < 0 ? "earL" : "earR");
            Grow(b, V(1f, 1.3f, 1f), V(0.06f * s, hy + 0.08f, -0.01f), bone => bone == ear);
        });
        // A cream breast and leg-warmers of cream fur
        b.PaintEll(Body, V(0, 0.6f, 0.07f), V(0.06f, 0.1f, 0.04f), cream);
        PokeBuilder.Both(s =>
        {
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            var c = V(0.09f * s, 0.16f, 0.01f);
            var r = V(0.075f, 0.12f, 0.075f);
            b.Ell(leg, c, r, cream, blend: 0.02f);
            FurTufts(b, leg, c, r, 22, 0.04f, 0.02f, cream, 1.1f, -0.9f, 0.6f);
        });
        Stretch(b, V(0.9f, 1.16f, 0.9f));
        return b;
    }

    // ------------------------------------------------------------------ 2429 Hexmagistra, from Mismagius

    /// <summary>
    /// Hexmagistra: Mismagius larger, its hat taller and its point curled, a band of red gems round its crown and a
    /// collar of them at its neck, its gown fallen into long frills that fray into mist, and in a hand of mist a
    /// twisted staff of shadow crowned with a glowing red crystal.
    /// </summary>
    private static PokeBuilder Hexmagistra()
    {
        var b = Become(Mismagius(), "Hexmagistra", 0.96f);
        var lift = Shifted(b, "head", V(0, 0.56f, 0));
        var purple = Rgb(100, 60, 140);
        var hat = Rgb(78, 44, 110);
        var mist = Rgb(176, 148, 210);
        var red = Rgb(224, 40, 64);
        var shadow = Rgb(40, 30, 56);
        Recolour(b, (Rgb(118, 72, 156), purple), (Rgb(92, 54, 128), hat), (Rgb(236, 120, 170), mist));
        int head = BoneOf(b, "head");
        // A taller hat whose point curls over
        b.Tube(head, new[] { V(0.06f, 0.98f, -0.24f), V(0.1f, 1.04f, -0.3f), V(0.15f, 1.02f, -0.33f), V(0.16f, 0.97f, -0.3f) }.Select(p => p + lift).ToArray(), 0.022f, 0.01f, hat, blend: 0.008f);
        for (int i = 0; i < 10; i++)
        {
            float a = i * MathF.Tau / 10f;
            As(b, Glow, () => b.Ell(head, V(MathF.Sin(a) * 0.12f, 0.745f, MathF.Cos(a) * 0.11f - 0.02f) + lift, V(0.018f, 0.022f, 0.014f), red, V(0, a / Degree, 0), blend: 0.004f));
        }
        // A collar of red gems at its neck
        for (int i = 0; i < 7; i++)
        {
            float a = (i - 3) * 0.42f;
            As(b, Glow, () => b.Ell(Body, V(MathF.Sin(a) * 0.1f, 0.55f - MathF.Cos(a) * 0.02f, MathF.Cos(a) * 0.085f) + lift, V(0.016f, 0.02f, 0.012f), red, V(0, a / Degree, 0), blend: 0.004f));
        }
        // The gown falls in long frills to a hem of mist
        b.Ell(Body, V(0, 0.26f, 0) + lift, V(0.17f, 0.15f, 0.15f), purple, blend: 0.04f);
        for (int i = 0; i < 10; i++)
        {
            float a = i * MathF.Tau / 10f + 0.2f;
            var at = V(MathF.Sin(a) * 0.14f, 0.2f, MathF.Cos(a) * 0.12f) + lift;
            Lock(b, Body, at, at + V(MathF.Sin(a) * 0.08f, -0.24f - 0.05f * (i % 3), MathF.Cos(a) * 0.06f), 0.06f, purple, mist);
        }
        // An arm of mist holding a twisted staff of shadow, a red crystal burning at its head
        int arm = b.Arm(1f, V(0.1f, 0.5f, 0.02f) + lift);
        var hand = V(0.27f, 0.46f, 0.08f) + lift;
        b.Tube(arm, new[] { V(0.09f, 0.5f, 0.02f) + lift, V(0.18f, 0.47f, 0.05f) + lift, hand }, 0.03f, 0.022f, mist, blend: 0.02f);
        foreach (int f in new[] { -1, 0, 1 })
            b.Spike(arm, hand, hand + V(0.02f, 0.03f * f, 0.05f), 0.01f, mist, blend: 0.004f);
        var foot = V(0.3f, 0.02f, 0.1f) + lift;
        var top = V(0.25f, 0.92f, 0.08f) + lift;
        var axis = Vector3.Normalize(top - foot);
        var u = Vector3.Normalize(Vector3.Cross(axis, V(0, 0, 1f)));
        var w = Vector3.Cross(axis, u);
        foreach (float phase in new[] { 0f, MathF.PI })
        {
            var twist = Enumerable.Range(0, 17).Select(i =>
            {
                float t = i / 16f;
                float a = t * MathF.Tau * 2.5f + phase;
                return foot + (top - foot) * t + (u * MathF.Cos(a) + w * MathF.Sin(a)) * 0.012f;
            }).ToArray();
            b.Tube(arm, twist, 0.014f, 0.012f, shadow, blend: 0.004f);
        }
        foreach (var (d, len) in new[] { (V(0, 1f, 0), 0.1f), (V(0.5f, 0.8f, 0.1f), 0.07f), (V(-0.5f, 0.8f, -0.1f), 0.07f), (V(0.1f, 0.7f, 0.6f), 0.06f) })
            As(b, Glow, () => b.Spike(arm, top - V(0, 0.01f, 0), top + Vector3.Normalize(d) * len, 0.022f, red, 0.7f, blend: 0.004f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2430 Corvicapo, from Honchkrow

    /// <summary>
    /// Corvicapo: Honchkrow larger and darker, its hat a broad-brimmed fedora with a white band, a cape of black
    /// feathers falling from its shoulders and a white beard of feathers down its chest.
    /// </summary>
    private static PokeBuilder Corvicapo()
    {
        var b = Become(Honchkrow(), "Corvicapo", 0.94f);
        var black = Rgb(38, 40, 60);
        var violet = Rgb(66, 54, 92);
        var white = Rgb(244, 244, 248);
        Recolour(b, (Rgb(44, 54, 112), black), (Rgb(214, 40, 60), violet), (Rgb(250, 208, 64), Rgb(108, 108, 116)));
        int head = BoneOf(b, "head");
        // A broad brim and a tall crown, a white band round it
        b.Sdf.RemoveAll(s => s.Bone == head && s.Color.Equals(black) && (s.Shape == SdfShape.RoundCone && s.A.Y > 0.68f || s.Shape == SdfShape.Ellipsoid && s.A.Y > 0.66f));
        b.Ell(head, V(0, 0.69f, 0.07f), V(0.26f, 0.024f, 0.22f), black, V(-8f, 0, 0), blend: 0.01f);
        b.Ell(head, V(0, 0.76f, 0.05f), V(0.12f, 0.08f, 0.11f), black, blend: 0.01f);
        b.Cut(head, V(0, 0.86f, 0.05f), V(0.02f, 0.04f, 0.08f));
        b.PaintTorus(head, V(0, 0.725f, 0.05f), 0.12f, 0.018f, white, default, 1f, 0.92f);
        // A white beard of feathers down its chest
        for (int i = 0; i < 7; i++)
        {
            float x = (i - 3) * 0.03f;
            b.Spike(Body, V(x, 0.5f, 0.17f), V(x * 1.2f, 0.26f - Math.Abs(x) * 0.6f, 0.2f), 0.035f, white, 0.5f, blend: 0.01f);
        }
        // A cape of black feathers from the shoulders, falling past the wings to the ground
        foreach (var (y, len, n) in new[] { (0.5f, 0.3f, 9), (0.4f, 0.32f, 9) })
            for (int i = 0; i < n; i++)
            {
                float a = (-110f + 220f * i / (n - 1)) * Degree;
                var dir = V(MathF.Sin(a), 0, -MathF.Cos(a));
                var root = V(0, y, -0.02f) + dir * 0.15f;
                Blade(b, Body, root, root + dir * 0.025f + V(0, -len, 0), 0.07f, i % 2 == 0 ? black : violet, dir, 0.22f);
            }
        // The collar of the cape turned up behind its head
        b.Ell(Body, V(0, 0.55f, -0.04f), V(0.15f, 0.08f, 0.1f), black, V(20f, 0, 0), blend: 0.02f);
        Stretch(b, V(1.08f, 1.1f, 1.08f));
        return b;
    }

    // ------------------------------------------------------------------ 2432 Brutabby, from Purugly

    /// <summary>
    /// Brutabby: Purugly larger and heavier, grey striped dark like a tabby, a ruff of pale fur at its neck, the tail
    /// round its waist knotted in front like a belt with its tufted end hanging, a torn ear, a scowl and fangs.
    /// </summary>
    private static PokeBuilder Brutabby()
    {
        var b = Become(Purugly(), "Brutabby", 0.9f);
        var grey = Rgb(132, 132, 140);
        var dark = Rgb(52, 52, 56);
        var pale = Rgb(214, 210, 200);
        Recolour(b, (Rgb(148, 160, 196), grey), (Rgb(236, 238, 244), pale), (Rgb(196, 160, 226), Rgb(150, 118, 118)));
        // Dark stripes over its back and flanks
        for (int i = 0; i < 5; i++)
        {
            float z = 0.14f - i * 0.08f;
            PokeBuilder.Both(s => b.PaintEll(Body, V(0.14f * s, 0.44f, z), V(0.13f, 0.05f, 0.022f), dark, V(0, 0, 40f * s), 0.008f));
        }
        // The belt's knot in front, its tufted end hanging
        b.Ell(Body, V(0, 0.3f, 0.2f), V(0.05f, 0.045f, 0.035f), grey, blend: 0.01f);
        b.Tube(Body, new[] { V(0.01f, 0.28f, 0.22f), V(0.03f, 0.18f, 0.24f), V(0.02f, 0.1f, 0.25f) }, 0.025f, 0.02f, grey, blend: 0.006f);
        FurTufts(b, Body, V(0.02f, 0.08f, 0.25f), V(0.03f, 0.03f, 0.03f), 10, 0.04f, 0.015f, dark, 2f, -0.9f, 1.5f);
        // A ruff of pale fur round its neck
        var rc = V(0, 0.46f, 0.12f);
        FurTufts(b, Body, rc, V(0.16f, 0.08f, 0.1f), 26, 0.05f, 0.022f, pale, 0.95f, -0.6f, 0.5f);
        int head = BoneOf(b, "head");
        // Stripes on its brow, a scowl and fangs
        foreach (float x in new[] { -0.04f, 0f, 0.04f })
            b.PaintEll(head, V(x, 0.62f, 0.18f), V(0.008f, 0.04f, 0.04f), dark, V(-30f, 0, 0), 0.006f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, V(0.03f * s, 0.46f, 0.25f), V(0.032f * s, 0.42f, 0.255f), 0.009f, Claw, mat: Shell, blend: 0.003f);
            b.Box(head, V(0.05f * s, 0.585f, 0.225f), V(0.035f, 0.008f, 0.012f), 0.004f, grey, V(0, 0, -22f * s), blend: 0.006f);
        });
        // A notch torn out of its right ear
        int ear = BoneOf(b, "earR");
        b.Cut(ear, V(0.17f, 0.74f, 0.07f), V(0.035f, 0.03f, 0.06f));
        Stretch(b, V(1.08f, 1.1f, 1.08f));
        return b;
    }

    // ------------------------------------------------------------------ 2435 Mephitarch, from Skuntank

    /// <summary>
    /// Mephitarch: Skuntank larger and darker, its tail split into two great plumes raised over its back each with
    /// its cream stripe, a haze of violet fume about their tips, the cream stripe of its back running down to its
    /// nose, and heavier clawed legs.
    /// </summary>
    private static PokeBuilder Mephitarch()
    {
        var b = Become(Skunk(true), "Mephitarch", 0.92f);
        const float k = 1.35f;
        var purple = Rgb(74, 56, 96);
        var cream = Rgb(236, 226, 190);
        var fume = Rgb(170, 120, 200);
        Recolour(b, (Rgb(110, 70, 140), purple), (Rgb(242, 224, 170), cream));
        // The tail splits into two plumes, raised and parted over its back
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail);
        PokeBuilder.Both(s =>
        {
            var path = new[] { V(0, 0.24f, -0.16f), V(0.03f * s, 0.42f, -0.28f), V(0.1f * s, 0.6f, -0.24f), V(0.18f * s, 0.7f, -0.12f), V(0.22f * s, 0.66f, -0.02f) };
            BushyTail(b, tail, path.Select(p => p * k).ToArray(), 0.055f * k, 0.085f * k, purple, cream, V(0.4f * s, 1f, -0.5f));
            var tip = path[^1] * k;
            foreach (var d in new[] { V(0.3f * s, 1f, 0.2f), V(0.8f * s, 0.6f, -0.2f), V(0.1f * s, 0.8f, 0.6f) })
                As(b, Glow, () => b.Spike(tail, tip, tip + Vector3.Normalize(d) * 0.1f, 0.03f, fume, 0.4f, blend: 0.012f));
        });
        // The stripe runs on down its brow to its nose
        int head = BoneOf(b, "head");
        b.PaintEll(head, V(0, 0.36f * k, 0.2f * k), V(0.02f, 0.06f, 0.06f), cream);
        // Heavier forelegs, dark socks
        foreach (var (z, front) in new[] { (0.09f, true), (-0.12f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = BoneOf(b, (front ? "leg" : "hind") + (s < 0 ? "L" : "R"));
                Grow(b, V(1.25f, 1f, 1.25f), V(0.075f * s * k, 0.1f, z * k), bone => bone == leg);
                b.PaintEll(leg, V(0.075f * s * k, 0.05f, z * k), V(0.06f, 0.06f, 0.06f), Rgb(46, 42, 66));
            });
        Stretch(b, V(1.08f, 1.06f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2437 Dotakuron, from Bronzong

    /// <summary>
    /// Dotakuron: Bronzong larger, its patina green, its bell banded in raised bronze patterns and studs, a crown of
    /// three spires in place of its loop, a fringe of small bronze bells round its rim (Metal), and its arms grown
    /// into armoured forearms with hands.
    /// </summary>
    private static PokeBuilder Dotakuron()
    {
        var b = Become(Bronzong(), "Dotakuron", 0.96f);
        var lift = Shifted(b, "body", V(0, 0.45f, 0));
        var green = Rgb(88, 132, 106);
        var bronze = Rgb(150, 122, 70);
        var dark = Rgb(48, 60, 54);
        Recolour(b, (Rgb(96, 158, 160), green), (Rgb(56, 100, 106), bronze));
        // The loop goes; three spires crown the bell
        b.Sdf.RemoveAll(s => s.Bone == Body && s.Shape == SdfShape.Torus && s.A.Y > 0.78f + lift.Y);
        foreach (var (x, h) in new[] { (0f, 0.3f), (-0.1f, 0.2f), (0.1f, 0.2f) })
        {
            var at = V(x, 0.76f - Math.Abs(x) * 0.4f, 0) + lift;
            b.Ell(Body, at + V(0, 0.03f, 0), V(0.04f, 0.04f, 0.04f), Rgb(108, 108, 96), mat: Metal, blend: 0.01f);
            b.Spike(Body, at + V(0, 0.05f, 0), at + V(0, h, 0), 0.035f, Rgb(108, 108, 96), mat: Metal, blend: 0.006f);
            b.Torus(Body, at + V(0, 0.08f, 0), 0.03f, 0.008f, bronze, mat: Metal, blend: 0.003f);
        }
        // Raised bands of bronze with studs between them
        foreach (var (y, r) in new[] { (0.66f, 0.17f), (0.5f, 0.19f), (0.3f, 0.225f) })
            b.Torus(Body, V(0, y, 0) + lift, r, 0.014f, bronze, mat: Metal, blend: 0.004f);
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.Tau / 12f;
            b.Ell(Body, V(MathF.Sin(a) * 0.19f, 0.58f, MathF.Cos(a) * 0.19f) + lift, V(0.012f, 0.012f, 0.012f), bronze, mat: Metal, blend: 0.003f);
        }
        // A fringe of small bells hanging round the rim
        for (int i = 0; i < 14; i++)
        {
            float a = i * MathF.Tau / 14f;
            var hang = V(MathF.Sin(a) * 0.2f, 0.12f, MathF.Cos(a) * 0.2f) + lift;
            b.Limb(Body, hang + V(0, 0.04f, 0), hang, 0.006f, 0.006f, dark, Metal, 0.003f);
            b.Ell(Body, hang - V(0, 0.03f, 0), V(0.026f, 0.03f, 0.026f), bronze, mat: Metal, blend: 0.006f);
            b.Torus(Body, hang - V(0, 0.055f, 0), 0.024f, 0.007f, bronze, mat: Metal, blend: 0.004f);
        }
        // Armoured forearms ending in hands
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            b.Sdf.RemoveAll(sh => sh.Bone == arm);
            var shoulder = V(0.19f * s, 0.62f, 0) + lift;
            var elbow = V(0.32f * s, 0.5f, 0.02f) + lift;
            var hand = V(0.38f * s, 0.3f, 0.06f) + lift;
            b.Ell(arm, shoulder, V(0.05f, 0.05f, 0.05f), bronze, mat: Metal, blend: 0.01f);
            b.Limb(arm, shoulder, elbow, 0.04f, 0.038f, green, Metal, 0.01f);
            b.Limb(arm, elbow, hand, 0.055f, 0.05f, green, Metal, 0.01f);
            foreach (float t in new[] { 0.3f, 0.75f })
                b.Torus(arm, Vector3.Lerp(elbow, hand, t), 0.056f, 0.01f, bronze, Euler(hand - elbow), mat: Metal, blend: 0.004f);
            b.Box(arm, hand + V(0, -0.05f, 0.01f), V(0.04f, 0.04f, 0.03f), 0.012f, green, mat: Metal, blend: 0.01f);
            foreach (int f in new[] { -1, 0, 1 })
                b.Limb(arm, hand + V(0.022f * f, -0.08f, 0.02f), hand + V(0.026f * f, -0.13f, 0.03f), 0.012f, 0.01f, green, Metal, 0.004f);
        });
        Stretch(b, V(1.06f, 1.08f, 1.06f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2441 Cantatot, from Chatot

    /// <summary>
    /// Cantatot: Chatot larger and brighter, a cream face, a ruff of red and white at its neck, a yellow breast, its
    /// note grown into two beamed quavers over a black crest, and its tail fanned out like a sheet of music, cream
    /// ruled with dark staves.
    /// </summary>
    private static PokeBuilder Cantatot()
    {
        var b = Become(Chatot(), "Cantatot", 0.8f);
        var black = Rgb(36, 40, 60);
        var cream = Rgb(246, 232, 196);
        var red = Rgb(214, 52, 56);
        var yellow = Rgb(250, 214, 64);
        Recolour(b, (Rgb(56, 58, 68), black), (Rgb(242, 120, 146), Rgb(246, 186, 60)));
        int head = BoneOf(b, "head");
        // The old note goes; a beamed pair rises from the crest
        b.Sdf.RemoveAll(s => s.Bone == head && s.Color.Equals(black) && (s.A.Y > 0.43f || s.B.Y > 0.5f));
        foreach (float x in new[] { -0.045f, 0.045f })
        {
            b.Ell(head, V(x, 0.46f, 0.04f), V(0.03f, 0.024f, 0.026f), black, V(0, 0, 20f), blend: 0.01f);
            b.Limb(head, V(x + 0.022f, 0.47f, 0.035f), V(x + 0.03f, 0.62f, 0.02f), 0.008f, 0.008f, black, blend: 0.004f);
        }
        b.Box(head, V(0.0375f - 0.0f, 0.615f, 0.02f), V(0.05f, 0.012f, 0.01f), 0.004f, black, V(0, 0, 8f), blend: 0.004f);
        // A spiky black crest
        foreach (float x in new[] { -0.05f, 0f, 0.05f })
            b.Spike(head, V(x, 0.43f, 0.0f), V(x * 1.4f, 0.47f, -0.08f), 0.03f, black, 0.5f);
        // A cream face round the eyes
        b.PaintEll(head, V(0, 0.37f, 0.12f), V(0.07f, 0.055f, 0.05f), cream);
        // A ruff of red under the white
        for (int i = 0; i < 10; i++)
        {
            float a = (i + 0.5f) * MathF.Tau / 10f;
            var at = V(MathF.Sin(a) * 0.09f, 0.28f, MathF.Cos(a) * 0.085f + 0.03f);
            b.Spike(head, at, at + V(MathF.Sin(a) * 0.05f, -0.04f, MathF.Cos(a) * 0.045f), 0.03f, red, 0.45f);
        }
        // A bright breast
        b.PaintEll(Body, V(0, 0.2f, 0.08f), V(0.07f, 0.08f, 0.06f), yellow);
        // The tail fans out like a sheet of music
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail);
        for (int i = 0; i < 6; i++)
        {
            float a = (-50f + 20f * i) * Degree;
            var dir = Vector3.Normalize(V(MathF.Sin(a), 0.7f, -0.8f));
            Blade(b, tail, V(0, 0.2f, -0.12f), V(0, 0.2f, -0.12f) + dir * 0.32f, 0.07f, cream, V(0, 0.75f, 0.7f), 0.2f);
        }
        for (int j = 0; j < 4; j++)
        {
            float d = 0.18f + 0.03f * j;
            b.PaintTorus(tail, V(0, 0.2f, -0.12f), d, 0.003f, Rgb(60, 50, 50), V(-50f, 0, 0), 1f, 1f);
        }
        b.PaintEll(tail, V(0.06f, 0.36f, -0.3f), V(0.012f, 0.01f, 0.012f), Rgb(60, 50, 50), soft: 0.004f);
        b.PaintEll(tail, V(-0.08f, 0.4f, -0.28f), V(0.012f, 0.01f, 0.012f), Rgb(60, 50, 50), soft: 0.004f);
        // Longer wing tips, green and yellow
        PokeBuilder.Both(s =>
        {
            int w = BoneOf(b, s < 0 ? "wingL" : "wingR");
            b.Spike(w, V(0.11f * s, 0.18f, -0.1f), V(0.13f * s, 0.12f, -0.3f), 0.035f, Rgb(56, 178, 96), 0.4f);
            b.PaintEll(w, V(0.12f * s, 0.14f, -0.22f), V(0.03f, 0.03f, 0.06f), yellow);
        });
        Stretch(b, V(1.05f, 1.15f, 1.05f));
        return b;
    }

    // ------------------------------------------------------------------ 2442 Ossuarion, from Spiritomb

    /// <summary>
    /// Ossuarion: Spiritomb's swirl grown wider and turned the green of its orbs, streaked with dark, risen over a
    /// cairn of two mossy keystones bound with chains, green light seeping from their cracks and rising as wisps.
    /// </summary>
    private static PokeBuilder Ossuarion()
    {
        var b = Become(Spiritomb(), "Ossuarion", 0.96f);
        var green = Rgb(112, 186, 132);
        var streak = Rgb(58, 50, 78);
        var moss = Rgb(84, 92, 70);
        var stone = Rgb(104, 106, 98);
        var lit = Rgb(150, 250, 170);
        var chain = Rgb(80, 86, 82);
        // The old keystone goes and everything rises over the cairn
        b.Sdf.RemoveAll(s => s.Shape == SdfShape.RoundBox);
        var up = V(0, 0.26f, 0);
        Move(b, _ => true, up);
        Recolour(b, (Rgb(198, 150, 200), green));
        // A wider swirl, streaked dark
        Grow(b, V(1.25f, 1.08f, 1.2f), V(0, 0.48f, -0.02f) + up, s => s.Color.Equals(green), _ => false);
        for (int i = 0; i < 5; i++)
        {
            float y = 0.3f + i * 0.13f;
            b.PaintEll(Body, V(0, y, -0.02f) + up, V(0.36f, 0.02f, 0.2f), streak, V(0, 0, (i % 2 == 0 ? 18f : -14f)), 0.01f);
        }
        // The cairn: two mossy keystones stacked, the upper askew
        b.Box(Body, V(0, 0.1f, 0), V(0.21f, 0.1f, 0.17f), 0.05f, stone, mat: Shell);
        b.Box(Body, V(0, 0.27f, 0.01f), V(0.18f, 0.08f, 0.15f), 0.045f, stone, V(0, 12f, 0), Shell, 0.01f);
        foreach (var (at, r) in new[] { (V(0.1f, 0.18f, 0.12f), V(0.08f, 0.05f, 0.06f)), (V(-0.12f, 0.04f, 0.1f), V(0.1f, 0.05f, 0.08f)), (V(0.02f, 0.34f, 0.04f), V(0.12f, 0.04f, 0.1f)), (V(-0.14f, 0.2f, -0.06f), V(0.06f, 0.08f, 0.08f)) })
            b.PaintEll(Body, at, r, moss);
        // Cracks of green light, wisps rising from them
        foreach (var (from, to) in new[] { (V(0, 0.2f, 0.152f), V(0.03f, 0.06f, 0.152f)), (V(-0.06f, 0.33f, 0.14f), V(-0.02f, 0.22f, 0.148f)), (V(0.09f, 0.16f, 0.152f), V(0.12f, 0.04f, 0.15f)) })
            b.Limb(Body, from, to, 0.007f, 0.005f, lit, Glow, 0.002f);
        foreach (var (at, d) in new[] { (V(0.16f, 0.22f, 0.06f), V(0.4f, 1f, 0)), (V(-0.17f, 0.12f, 0.04f), V(-0.4f, 1f, 0.1f)), (V(0.05f, 0.36f, 0.1f), V(0.1f, 1f, 0.3f)) })
            As(b, Glow, () => b.Spike(Body, at, at + Vector3.Normalize(d) * 0.1f, 0.022f, lit, 0.4f, blend: 0.006f));
        // A chain of links round the lower stone
        for (int i = 0; i < 18; i++)
        {
            float a = i * MathF.Tau / 18f;
            var at = V(MathF.Sin(a) * 0.205f, 0.13f + MathF.Sin(a * 2f) * 0.012f, MathF.Cos(a) * 0.175f);
            b.Torus(Body, at, 0.016f, 0.005f, chain, V(i % 2 == 0 ? 0f : 90f, a / Degree, 90f), mat: Metal, blend: 0.002f);
        }
        return b;
    }

    // ------------------------------------------------------------------ 2448 Aurasage, from Lucario

    /// <summary>
    /// Aurasage: Lucario taller and darker blue, a mantle of shaggy black fur over its shoulders, its four aura
    /// sensors grown long and lit (Glow, blue), the spikes of its hands and chest grown into long blades of steel,
    /// steel greaves on its shins and a long bushy tail banded black.
    /// </summary>
    private static PokeBuilder Aurasage()
    {
        var b = Become(Lucario(), "Aurasage", 0.96f);
        var blue = Rgb(52, 98, 172);
        var black = Rgb(36, 36, 46);
        var aura = Rgb(110, 196, 255);
        var pale = Rgb(170, 180, 200);
        int head = BoneOf(b, "head");
        // The sensors grow long and burn with aura
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == head && s.Color.Equals(Rgb(42, 44, 60)) && s.Shape == SdfShape.RoundCone && s.A.Z < -0.05f).ToList())
        {
            s.B = s.A + (s.B - s.A) * 2.2f + V(0, -0.02f, -0.1f);
            s.Color = aura;
            s.Material = Glow;
        }
        // Blue all over but for a grey muzzle and breast; the black is the mantle's alone
        Recolour(b, (Rgb(74, 132, 212), blue), (Rgb(42, 44, 60), Rgb(44, 76, 140)), (Rgb(236, 220, 162), pale));
        // The chest spike and the hand spikes grown into long blades
        foreach (var s in b.Sdf.Shapes.Where(s => s.Color.Equals(Claw) && s.Shape == SdfShape.RoundCone).ToList())
        {
            s.B = s.A + (s.B - s.A) * 2.1f;
            s.Size = new Vector3(s.Size.X * 1.35f, s.Size.Y, s.Size.Z);
            s.Color = Steel;
            s.Material = Metal;
        }
        // A mantle of black fur over its shoulders
        var mc = V(0, 0.78f, 0.0f);
        var mr = V(0.2f, 0.09f, 0.15f);
        b.Ell(Body, mc, mr, black, blend: 0.03f);
        FurTufts(b, Body, mc, mr, 36, 0.07f, 0.03f, black, 0.95f, -0.7f, 0.8f);
        foreach (float x in new[] { -0.08f, 0f, 0.08f })
            b.Spike(Body, V(x, 0.72f, 0.12f), V(x, 0.55f, 0.15f), 0.035f, black, 0.5f);
        // Steel greaves and bracers
        PokeBuilder.Both(s =>
        {
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            foreach (float y in new[] { 0.1f, 0.16f, 0.22f })
                b.Torus(leg, V(0.12f * s, y, 0.03f), 0.054f, 0.01f, pale, mat: Metal, blend: 0.004f);
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            b.Torus(arm, V(0.24f * s, 0.6f, 0.05f), 0.05f, 0.012f, pale, Euler(V(0.4f * s, -1f, 0.2f)), mat: Metal, blend: 0.004f);
        });
        // A long bushy tail, banded black
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail && s.Shape == SdfShape.Ellipsoid);
        BushyTail(b, tail, new[] { V(0, 0.44f, -0.17f), V(0, 0.32f, -0.3f), V(0.06f, 0.22f, -0.46f), V(0.14f, 0.24f, -0.6f), V(0.18f, 0.34f, -0.68f) }, 0.05f, 0.08f, blue, black, V(0, -1f, -0.3f));
        Stretch(b, V(0.96f, 1.08f, 0.96f));
        return b;
    }

    // ------------------------------------------------------------------ 2450 Behemowdon, from Hippowdon

    /// <summary>
    /// Behemowdon: Hippowdon larger, its hide a dark umber (the female's darker still), its back heaped with plates of
    /// sandstone, sand pouring from the vents along its flanks, its jaw wider and its tusks longer.
    /// </summary>
    /// <param name="female">A female keeps Hippowdon's: a darker hide.</param>
    private static PokeBuilder Behemowdon(bool female = false)
    {
        var b = Become(Hippowdon(female), "Behemowdon", 1f);
        var hideWas = female ? Rgb(104, 102, 108) : Rgb(214, 176, 74);
        var plateWas = female ? Rgb(206, 172, 82) : Rgb(92, 92, 98);
        var hide = female ? Rgb(70, 64, 58) : Rgb(100, 90, 76);
        var sand = Rgb(206, 170, 112);
        Recolour(b, (hideWas, hide), (plateWas, Rgb(84, 78, 68)));
        // Sandstone plates heaped on its back, overlapping like slabs
        int n = 0;
        for (int row = 0; row < 4; row++)
            for (int col = -2; col <= 2; col++)
            {
                if (Math.Abs(col) == 2 && (row == 0 || row == 3)) continue;
                float z = 0.12f - row * 0.12f;
                float x = col * 0.08f;
                float y = 0.32f + 0.2f * MathF.Sqrt(Math.Max(0.05f, 1f - x * x / 0.07f - (z + 0.06f) * (z + 0.06f) / 0.11f));
                var tint = n++ % 3 == 0 ? PixelCanvas.Shadow(sand, 0.12f) : sand;
                b.Box(Body, V(x * 1.1f, y + 0.05f + (2 - Math.Abs(col)) * 0.015f, z), V(0.095f, 0.035f, 0.08f), 0.025f, tint, V(-15f + row * 4f, col * 9f, -col * 14f), Shell, 0.008f);
            }
        // Sand pouring from the vents
        foreach (var (x, z) in new[] { (0.12f, -0.02f), (-0.12f, -0.02f), (0.12f, -0.2f), (-0.12f, -0.2f) })
        {
            float y = 0.32f + 0.2f * MathF.Sqrt(1f - x * x / (0.26f * 0.26f) - (z + 0.06f) * (z + 0.06f) / (0.33f * 0.33f));
            float sx = MathF.Sign(x);
            b.Tube(Body, Smooth(3, V(x * 1.5f, y - 0.04f, z), V(x * 2.05f, y - 0.12f, z), V(x * 2.15f, y - 0.22f, z + 0.01f)), 0.012f, 0.004f, sand, blend: 0.004f);
        }
        // A wider jaw and longer tusks
        int jaw = BoneOf(b, "jaw");
        Grow(b, V(1.25f, 1.05f, 1.1f), V(0, 0.36f, 0.2f), bone => bone == jaw);
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == jaw && s.Color.Equals(White) && s.Shape == SdfShape.RoundCone).ToList())
        {
            s.B = s.A + (s.B - s.A) * 1.8f;
            s.Size = new Vector3(s.Size.X * 1.3f, s.Size.Y, s.Size.Z);
        }
        Stretch(b, V(1.06f, 1.04f, 1.06f));
        return b;
    }

    // ------------------------------------------------------------------ 2452 Dreadpion, from Drapion

    /// <summary>
    /// Dreadpion: Drapion larger and darker, armoured in dusky violet, plates rising along its back, its claws
    /// heavier and serrated inside, and two long segmented tails curled up over it, each ending in a spiked bulb and
    /// a hooked sting.
    /// </summary>
    private static PokeBuilder Dreadpion()
    {
        var b = Become(Drapion(), "Dreadpion", 1f);
        var dusk = Rgb(84, 58, 100);
        var light = Rgb(116, 80, 128);
        var dark = Rgb(52, 38, 66);
        var bone = Rgb(196, 168, 186);
        Recolour(b, (Rgb(196, 112, 164), dusk), (Rgb(226, 160, 198), light), (Rgb(140, 46, 98), dark), (Rgb(40, 150, 226), Rgb(230, 70, 220)));
        // Heavier claws, serrated inside
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var wrist = V(0.31f * s, 0.34f, 0.22f);
            const float g = 1.35f;
            Grow(b, V(g, g, g), wrist, sh => sh.Bone == arm && Vector3.Distance(sh.A, wrist) < 0.15f, bn => bn == arm);
            var d = Vector3.Normalize(V(-0.03f * s, 0.07f, 0.08f));
            const float size = 0.075f;
            var palm = wrist + d * size * 0.6f;
            var tip = palm + d * size * 1.5f;
            Vector3 At(Vector3 p) => wrist + (p - wrist) * g;
            for (int i = 1; i <= 3; i++)
            {
                float t = 0.25f + 0.22f * i;
                var up = Vector3.Lerp(palm + d * size * 0.3f + V(0, size * 0.15f, 0), tip + V(0, size * 0.25f, 0), t);
                var low = Vector3.Lerp(palm + d * size * 0.3f - V(0, size * 0.15f, 0), tip - V(0, size * 0.35f, 0), t);
                b.Spike(arm, At(up), At(up - V(0, 0.03f, 0)), 0.01f, bone, mat: Shell, blend: 0.003f);
                b.Spike(arm, At(low), At(low + V(0, 0.03f, 0)), 0.01f, bone, mat: Shell, blend: 0.003f);
            }
        });
        // Armour plates rising along the back
        foreach (float z in new[] { 0.1f, -0.02f, -0.14f })
            b.Box(Body, V(0, 0.39f, z), V(0.05f, 0.05f, 0.04f), 0.012f, dark, V(-20f, 45f, 0), Shell, 0.01f);
        // Two long segmented tails curling over its back
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(sh => sh.Bone == tail);
        PokeBuilder.Both(s =>
        {
            var path = Smooth(3, V(0.04f * s, 0.27f, -0.24f), V(0.08f * s, 0.36f, -0.42f), V(0.14f * s, 0.58f, -0.44f), V(0.18f * s, 0.72f, -0.28f), V(0.17f * s, 0.7f, -0.12f));
            for (int i = 0; i < path.Length; i++)
            {
                float r = 0.05f - 0.018f * i / (path.Length - 1);
                b.Ell(tail, path[i], V(r, r, r * 1.1f), i % 2 == 0 ? dusk : light, blend: 0.012f);
                if (i % 2 == 1)
                {
                    var dir = Vector3.Normalize(path[Math.Min(path.Length - 1, i + 1)] - path[i - 1]);
                    var up = Vector3.Normalize(Vector3.Cross(dir, V(s, 0, 0)));
                    if (up.Y < 0) up = -up;
                    b.Spike(tail, path[i] + up * r * 0.8f, path[i] + up * (r + 0.035f), 0.012f, bone, mat: Shell, blend: 0.003f);
                }
            }
            var end = path[^1] + V(0, -0.01f, 0.03f);
            b.Ell(tail, end, V(0.05f, 0.05f, 0.05f), light, mat: Shell, blend: 0.01f);
            foreach (var dd in new[] { V(s, 0.5f, 0), V(0, 1f, 0.2f), V(-s * 0.3f, 0.4f, 0.8f) })
                b.Spike(tail, end + Vector3.Normalize(dd) * 0.04f, end + Vector3.Normalize(dd) * 0.07f, 0.01f, bone, mat: Shell, blend: 0.003f);
            b.Tube(tail, Smooth(3, end + V(0, -0.03f, 0.03f), end + V(0, -0.08f, 0.07f), end + V(0, -0.1f, 0.04f)), 0.016f, 0.004f, bone, Shell, 0.004f);
        });
        Stretch(b, V(1.06f, 1.04f, 1.08f));
        return b;
    }

    // ------------------------------------------------------------------ 2454 Venomsensei, from Toxicroak

    /// <summary>
    /// Venomsensei: Toxicroak taller and leaner, its belly and throat sac red (the sac grown larger, the female's
    /// smaller still), a sash of black tied at its waist with its ends hanging, and long curved claws of steel on its
    /// knuckles in place of its red ones.
    /// </summary>
    /// <param name="female">A female keeps Toxicroak's: a smaller sac.</param>
    private static PokeBuilder Venomsensei(bool female = false)
    {
        var b = Become(Toxicroak(female), "Venomsensei", 0.96f);
        var blue = Rgb(40, 84, 140);
        var black = Rgb(30, 32, 44);
        var red = Rgb(214, 54, 60);
        var sash = Rgb(44, 40, 40);
        Recolour(b, (Rgb(64, 132, 214), blue), (Rgb(36, 40, 56), black), (Rgb(232, 48, 86), red), (White, red));
        // The sac grows
        Grow(b, V(1.3f, 1.3f, 1.25f), V(0, 0.61f, 0.04f), s => s.Bone == Body && s.Color.Equals(red) && s.Op == SdfOp.Add, _ => false);
        // The black band at its waist becomes a sash, its ends hanging at one hip
        b.Sdf.RemoveAll(s => s.Bone == Body && s.Op == SdfOp.Paint && s.Color.Equals(black) && s.Shape == SdfShape.Torus);
        b.Torus(Body, V(0, 0.46f, 0), 0.14f, 0.025f, sash, default, 1f, 0.84f, blend: 0.008f);
        b.Ell(Body, V(0.06f, 0.46f, 0.11f), V(0.035f, 0.03f, 0.02f), sash, blend: 0.006f);
        foreach (var (dx, len) in new[] { (0.04f, 0.2f), (0.08f, 0.16f) })
            Blade(b, Body, V(0.06f, 0.45f, 0.12f), V(0.06f + dx, 0.45f - len, 0.14f), 0.035f, sash, V(0, 0, 1f), 0.25f);
        // Long curved claws of steel from the knuckles
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            b.Sdf.RemoveAll(sh => sh.Bone == arm && sh.Color.Equals(red));
            b.Tube(arm, Smooth(3, V(0.25f * s, 0.46f, 0.12f), V(0.32f * s, 0.4f, 0.12f), V(0.35f * s, 0.28f, 0.14f), V(0.33f * s, 0.2f, 0.16f)), 0.016f, 0.004f, Steel, Metal, 0.004f);
        });
        Stretch(b, V(0.92f, 1.1f, 0.92f));
        return b;
    }

    /// <summary>A copy of a shape moved by <paramref name="d"/> (a second head made from the first).</summary>
    private static SdfPrimitive CopyOf(SdfPrimitive s, Vector3 d) => new()
    {
        Shape = s.Shape, Op = s.Op, A = s.A + d, B = s.B + d, Size = s.Size, Rounding = s.Rounding, Rotation = s.Rotation, Stretch = s.Stretch,
        Blend = s.Blend, Color = s.Color, Material = s.Material, PaintsMaterial = s.PaintsMaterial, PaintBones = s.PaintBones, Bone = s.Bone
    };

    // ------------------------------------------------------------------ 2455 Flytrapex, from Carnivine

    /// <summary>
    /// Flytrapex: Carnivine grown into a rooted plant of two heads: a bulbous trunk on gnarled roots, two jaws on two
    /// long necks, deep green with violet throats, a sweet glowing drop at each jaw's corner (Glow, yellow), and vine
    /// arms thick with leaves.
    /// </summary>
    private static PokeBuilder Flytrapex()
    {
        var b = Become(Carnivine(), "Flytrapex", 0.98f);
        var lift = Shifted(b, "body", V(0, 0.62f, 0));
        var green = Rgb(76, 126, 64);
        var light = Rgb(110, 156, 76);
        var leaf = Rgb(60, 140, 66);
        var throat = Rgb(170, 70, 120);
        var drop = Rgb(255, 222, 80);
        Recolour(b, (Rgb(144, 204, 82), green), (Rgb(196, 232, 124), light), (Rgb(232, 104, 146), throat), (Rgb(220, 206, 80), Rgb(96, 110, 60)));
        // The head moves up and to one side; a second grows beside it
        float cut = 0.5f + lift.Y;
        var d1 = V(-0.2f, 0.22f, 0);
        var d2 = V(0.2f, 0.22f, 0);
        var head = b.Sdf.Shapes.Where(s => s.Bone == Body && Math.Min(s.A.Y, s.Shape == SdfShape.RoundCone ? s.B.Y : s.A.Y) > cut).ToList();
        foreach (var s in head) { s.A += d1; s.B += d1; }
        foreach (var s in head) b.Sdf.Add(CopyOf(s, d2 - d1));
        b.Sdf.Changed();
        foreach (var d in b.Model.Decals.Where(d => d.Bone == Body && d.Center.Y > cut).ToList()) d.Center += d1;
        var c = V(0, 0.62f, 0) + lift;
        var r = V(0.17f, 0.14f, 0.15f);
        PokeBuilder.Both(s => b.Eye(Body, On(c + d2, r, 0.2f + 0.06f * s, 0.67f + lift.Y + 0.22f), V(0.35f * s, 0.15f, 1f), 0.024f, sclera: true, pupil: Rgb(30, 36, 30)));
        // Two necks from the trunk, the trunk itself, a glowing drop at each jaw
        foreach (var d in new[] { d1, d2 })
        {
            float s = MathF.Sign(d.X);
            b.Tube(Body, Smooth(3, V(0.04f * s, 0.46f, 0) + lift, V(0.02f * s, 0.6f, -0.04f) + lift, V(0.16f * s, 0.66f, -0.06f) + lift, c + d + V(0, -0.1f, -0.06f)), 0.05f, 0.045f, green, blend: 0.02f);
            As(b, Glow, () => b.Ell(Body, c + d + V(0.14f * s, -0.06f, 0.1f), V(0.016f, 0.022f, 0.016f), drop, blend: 0.004f));
        }
        var trunk = V(0, 0.38f, 0) + lift;
        b.Ell(Body, trunk, V(0.15f, 0.15f, 0.14f), green, blend: 0.03f);
        b.PaintEll(Body, trunk + V(0, -0.06f, 0.05f), V(0.12f, 0.08f, 0.1f), Rgb(84, 92, 74));
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f + 0.3f;
            b.Spike(Body, trunk + V(MathF.Sin(a) * 0.13f, 0.02f, MathF.Cos(a) * 0.12f), trunk + V(MathF.Sin(a) * 0.17f, 0.04f, MathF.Cos(a) * 0.16f), 0.012f, Rgb(150, 110, 130), blend: 0.004f);
        }
        // Gnarled roots splaying to the ground
        int vines = BoneOf(b, "vines");
        b.Sdf.RemoveAll(s => s.Bone == vines);
        for (int i = 0; i < 5; i++)
        {
            float a = i * MathF.Tau / 5f + 0.6f;
            var o = V(MathF.Sin(a), 0, MathF.Cos(a));
            var path = Smooth(3, trunk + o * 0.08f + V(0, -0.08f, 0), trunk + o * 0.2f + V(0, -0.14f, 0), trunk + o * 0.27f + V(0, -0.2f, 0), trunk + o * 0.36f + V(0, -0.3f, 0));
            b.Tube(vines, path, 0.04f, 0.016f, i % 2 == 0 ? green : Rgb(88, 100, 70), blend: 0.015f);
        }
        // Thicker leafy vines for arms
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var root = V(0.11f * s, 0.45f, 0.02f) + lift;
            var path = Smooth(3, root, root + V(0.12f * s, 0.04f, 0.02f), root + V(0.26f * s, -0.04f, 0.0f), root + V(0.36f * s, 0.06f, -0.02f));
            b.Tube(arm, path, 0.02f, 0.01f, leaf, blend: 0.006f);
            for (int k = 2; k < path.Length; k += 2)
                Blade(b, arm, path[k], path[k] + V(0.02f * s, 0.06f, (k % 4 == 0 ? 0.04f : -0.04f)), 0.03f, leaf, V(0, 0.3f, 1f), 0.3f, Leaf);
        });
        Stretch(b, V(1.05f, 1.05f, 1.05f));
        // It stands on its roots now
        b.Model.Hovers = false;
        b.Ground(0f);
        return b;
    }

    // ------------------------------------------------------------------ 2457 Lumifathom, from Lumineon

    /// <summary>
    /// Lumifathom: Lumineon larger and gone deep-sea dark, navy and indigo, a second pair of fins behind its great
    /// ones, its spots pink ringed in cyan light (Glow), a row of glowing dots along its flank, and a long lure
    /// curving from its brow to a glowing pink bulb. The female's bigger lower fins are kept.
    /// </summary>
    /// <param name="female">A female keeps Lumineon's: bigger lower fins.</param>
    private static PokeBuilder Lumifathom(bool female = false)
    {
        var b = Become(Lumineon(female), "Lumifathom", 0.96f);
        var lift = Shifted(b, "head", V(0, 0.42f, 0.1f));
        var body = Rgb(48, 54, 124);
        var deep = Rgb(28, 30, 76);
        var cyan = Rgb(80, 220, 250);
        var pink = Rgb(250, 120, 220);
        Recolour(b, (Rgb(150, 210, 244), body), (Rgb(28, 50, 90), deep), (Rgb(240, 110, 166), pink));
        // A second pair of fins behind the first
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "finL" : "finR");
            RimmedFin(b, wing, V(0.13f * s, 0.56f, -0.2f) + lift, V(0.012f, 0.13f, 0.07f), V(-55f, 0, -40f * s), deep, body, 0.02f);
            // Eye-spots of pink ringed in cyan on each fin
            foreach (var (at, size) in new[] { (V(0.2f * s, 0.66f, -0.1f), 0.035f), (V(0.14f * s, 0.58f, -0.06f), 0.022f), (V(0.15f * s, 0.62f, -0.24f), 0.026f) })
            {
                PaintAs(b, wing, at + lift, V(size * 1.5f, size * 1.5f, 0.04f), cyan, Glow, V(-30f, 0, -55f * s));
                PaintAs(b, wing, at + lift, V(size, size, 0.045f), pink, Glow, V(-30f, 0, -55f * s));
            }
        });
        // A row of glowing dots along each flank
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < 5; i++)
                PaintAs(b, Body, V(0.06f * s, 0.43f, 0.06f - i * 0.05f) + lift, V(0.016f, 0.016f, 0.016f), i % 2 == 0 ? cyan : pink, Glow);
        });
        // A long lure from the brow, a glowing bulb at its end
        int head = BoneOf(b, "head");
        var lure = Smooth(3, V(0, 0.5f, 0.12f) + lift, V(0, 0.62f, 0.2f) + lift, V(0, 0.6f, 0.34f) + lift, V(0, 0.48f, 0.4f) + lift);
        b.Tube(head, lure, 0.01f, 0.006f, body, blend: 0.004f);
        As(b, Glow, () => b.Ell(head, lure[^1] + V(0, -0.03f, 0), V(0.028f, 0.036f, 0.028f), pink, blend: 0.006f));
        Stretch(b, V(1.06f, 1.06f, 1.1f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2460 Glaciarbor, from Abomasnow

    /// <summary>
    /// Glaciarbor: Abomasnow larger, its snow grown into a frosted cloak of icy fur, its face dark under a scowl, its
    /// limbs turned to dark bark, gnarled branches growing from its shoulders and back with icicles hanging from them,
    /// and a short trunk of a tail. The female's longer chest fur is kept.
    /// </summary>
    /// <param name="female">A female keeps Abomasnow's: longer fur at her chest.</param>
    private static PokeBuilder Glaciarbor(bool female = false)
    {
        var b = Become(Abomasnow(female), "Glaciarbor", 1f);
        var frost = Rgb(222, 234, 246);
        var bark = Rgb(98, 86, 72);
        var ice = Rgb(184, 222, 244);
        var face = Rgb(92, 96, 106);
        Recolour(b, (Rgb(240, 244, 248), frost), (Rgb(66, 138, 132), bark), (Rgb(176, 146, 214), Rgb(200, 230, 250)));
        var c = V(0, 0.45f, 0);
        var r = V(0.24f, 0.3f, 0.2f);
        // A dark face under its brow
        b.PaintEll(Body, On(c, r, 0, 0.56f), V(0.12f, 0.1f, 0.06f), face);
        // Branches growing from its shoulders and back, icicles hanging from them
        foreach (var (root, tip) in new[] { (V(0.16f, 0.66f, -0.06f), V(0.32f, 0.98f, -0.12f)), (V(-0.16f, 0.66f, -0.06f), V(-0.3f, 0.96f, -0.14f)), (V(0.2f, 0.52f, -0.12f), V(0.42f, 0.68f, -0.2f)), (V(-0.2f, 0.52f, -0.12f), V(-0.42f, 0.66f, -0.18f)) })
        {
            var mid = Vector3.Lerp(root, tip, 0.55f);
            b.Tube(Body, Smooth(3, root, mid + V(0, 0, -0.02f), tip), 0.03f, 0.01f, bark, Shell, 0.01f);
            var side = mid + V(Math.Sign(tip.X) * 0.08f, 0.08f, 0.0f);
            b.Tube(Body, new[] { mid, side }, 0.016f, 0.008f, bark, Shell, 0.006f);
            foreach (float t in new[] { 0.35f, 0.6f, 0.85f })
            {
                var at = Vector3.Lerp(root, tip, t) - V(0, 0.015f, 0);
                b.Spike(Body, at, at - V(0, 0.07f - 0.02f * t, 0), 0.012f, ice, mat: Shell, blend: 0.003f);
            }
            b.PaintEll(Body, Vector3.Lerp(root, tip, 0.7f) + V(0, 0.02f, 0), V(0.04f, 0.015f, 0.04f), frost, soft: 0.006f);
        }
        // Icicles from its branch-fingers
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            foreach (var tip in new[] { V(0.29f * s, 1.0f, 0.07f), V(0.37f * s, 1.0f, 0.06f), V(0.44f * s, 0.94f, 0.05f) })
                b.Spike(arm, Vector3.Lerp(V(0.34f * s, 0.87f, 0.06f), tip, 0.6f), Vector3.Lerp(V(0.34f * s, 0.87f, 0.06f), tip, 0.6f) - V(0, 0.06f, 0), 0.01f, ice, mat: Shell, blend: 0.003f);
        });
        // A short trunk of a tail, its end ringed
        int tail = b.Tail(V(0, 0.3f, -0.16f));
        b.Limb(tail, V(0, 0.3f, -0.16f), V(0, 0.22f, -0.38f), 0.09f, 0.08f, bark, Shell, 0.02f);
        b.PaintTorus(tail, V(0, 0.224f, -0.385f), 0.045f, 0.006f, Rgb(140, 120, 96), Euler(V(0, -0.3f, -1f)));
        Stretch(b, V(1.08f, 1.06f, 1.08f));
        return b;
    }

    // ------------------------------------------------------------------ 2461 Rimeripper, from Weavile

    /// <summary>
    /// Rimeripper: Weavile taller and darker, its crest split into three long feathers, crimson frosted white at their
    /// tips (the female's shorter), its collar a short cape of dark fur rimed with frost, its claws grown long and
    /// white with frost, a bushy frost-tipped tail and eyes of pale ice.
    /// </summary>
    /// <param name="female">A female keeps Weavile's: a shorter crest.</param>
    private static PokeBuilder Rimeripper(bool female = false)
    {
        var b = Become(Weavile(female), "Rimeripper", 0.94f);
        var dark = Rgb(40, 44, 64);
        var belly = Rgb(64, 70, 104);
        var crimson = Rgb(150, 30, 46);
        var frost = Rgb(216, 232, 246);
        var red = Rgb(214, 40, 70);
        Recolour(b, (Rgb(46, 66, 98), dark), (Rgb(220, 50, 70), Rgb(150, 220, 250)), (Rgb(244, 204, 64), Rgb(150, 210, 240)));
        b.PaintEll(Body, V(0, 0.46f, 0.07f), V(0.07f, 0.11f, 0.05f), belly);
        int head = BoneOf(b, "head");
        // Three long feathers in place of the crown
        b.Sdf.RemoveAll(s => s.Bone == head && s.Color.R > 160 && s.Color.G < 90 && s.Color.B < 110);
        float k = female ? 0.78f : 1f;
        foreach (float a in new[] { -34f, 0f, 34f })
        {
            var d = Vector3.Normalize(V(MathF.Sin(a * Degree), MathF.Cos(a * Degree), -0.5f));
            var root = V(a * 0.0014f, 0.77f, -0.03f);
            var tip = root + d * (0.34f - MathF.Abs(a) * 0.002f) * k;
            Blade(b, head, root, tip, 0.032f, crimson, V(0, 0.4f, 1f), 0.3f);
            b.PaintEll(head, Vector3.Lerp(root, tip, 0.85f), V(0.04f, 0.06f, 0.04f), frost, Euler(d));
        }
        // Longer pointed ears
        b.Spike(head, V(0.08f, 0.76f, 0.0f), V(0.2f, 0.9f, -0.06f), 0.04f, dark, 0.5f);
        b.Spike(head, V(-0.08f, 0.76f, 0.0f), V(-0.2f, 0.9f, -0.06f), 0.04f, dark, 0.5f);
        // The collar a short cape of dark fur, rimed at its tips
        b.Sdf.RemoveAll(s => s.Bone == Body && s.Color.Equals(red));
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.Tau / 12f;
            var at = V(MathF.Sin(a) * 0.1f, 0.6f, MathF.Cos(a) * 0.08f);
            var tip = at + V(MathF.Sin(a) * 0.08f, -0.12f - (MathF.Cos(a) < 0 ? 0.06f : 0f), MathF.Cos(a) * 0.06f);
            b.Spike(Body, at, tip, 0.04f, dark, 0.5f);
            b.PaintEll(Body, tip + (at - tip) * 0.15f, V(0.022f, 0.022f, 0.022f), frost, soft: 0.006f);
        }
        // Long frosted claws
        foreach (var s in b.Sdf.Shapes.Where(s => s.Color.Equals(Claw) && s.Shape == SdfShape.RoundCone).ToList())
        {
            bool hand = s.A.Y > 0.3f;
            s.B = s.A + (s.B - s.A) * (hand ? 2.4f : 1.3f);
            s.Color = frost;
            s.Material = Shell;
        }
        // A bushy tail, frost at its tips, in place of the feathers
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail);
        BushyTail(b, tail, new[] { V(0, 0.37f, -0.05f), V(0, 0.3f, -0.18f), V(0.04f, 0.36f, -0.32f), V(0.08f, 0.5f, -0.38f), V(0.1f, 0.62f, -0.34f) }, 0.035f, 0.06f, dark, frost, V(0, 1f, -0.6f));
        Stretch(b, V(0.95f, 1.12f, 0.95f));
        return b;
    }

    // ------------------------------------------------------------------ 2463 Lickolossus, from Lickilicky

    /// <summary>
    /// Lickolossus: Lickilicky larger, its tongue grown long enough to coil twice round its body from its open mouth,
    /// its bib grown into a belly band striped cream and yellow, a cream curl on its head and a long tail curving up.
    /// </summary>
    private static PokeBuilder Lickolossus()
    {
        var b = Become(Lickilicky(), "Lickolossus", 0.98f);
        var pink = Rgb(236, 150, 168);
        var tongue = Rgb(222, 112, 128);
        var cream = Rgb(248, 232, 176);
        var yellow = Rgb(244, 204, 96);
        Recolour(b, (Rgb(236, 140, 160), pink), (White, cream), (Rgb(250, 214, 80), yellow));
        int head = BoneOf(b, "head");
        // The old tongue goes; the new one leaves the mouth and coils twice round the body
        b.Sdf.RemoveAll(s => s.Bone == head && s.Color.Equals(Rgb(250, 190, 196)));
        var c = V(0, 0.38f, -0.02f);
        var coil = Enumerable.Range(0, 33).Select(i =>
        {
            float t = i / 32f;
            float a = MathF.PI * 0.15f + t * MathF.Tau * 2f;
            float y = 0.55f - 0.26f * t;
            float k = MathF.Sqrt(Math.Max(0.2f, 1f - MathF.Pow((y - c.Y) / 0.3f, 2f)));
            return c + V(MathF.Sin(a) * 0.27f * k * 1.06f, y - c.Y, MathF.Cos(a) * 0.24f * k * 1.06f);
        }).ToList();
        var path = new[] { V(0, 0.63f, 0.15f), V(0.04f, 0.6f, 0.24f) }.Concat(coil).ToArray();
        b.Tube(head, path, 0.05f, 0.04f, tongue, blend: 0.004f);
        // Stripes of cream and yellow across the belly band
        foreach (float y in new[] { 0.41f, 0.3f, 0.23f })
            b.PaintEll(Body, V(0, y, 0.16f), V(0.14f, 0.018f, 0.1f), cream, soft: 0.008f);
        // A curl of cream on its head
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == head && s.A.Y > 0.74f && s.Color.Equals(pink))) s.Color = cream;
        // A long tail curving up behind
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail);
        b.Tube(tail, Smooth(3, V(0, 0.2f, -0.22f), V(0.04f, 0.1f, -0.4f), V(0.1f, 0.14f, -0.58f), V(0.14f, 0.32f, -0.66f), V(0.16f, 0.46f, -0.62f)), 0.09f, 0.02f, pink, blend: 0.02f);
        Stretch(b, V(1.05f, 1.08f, 1.05f));
        return b;
    }

    // ------------------------------------------------------------------ 2465 Tanglewood, from Tangrowth

    /// <summary>
    /// Tanglewood: Tangrowth larger, its vines grown with green leaves and small pink and yellow flowers, more vines
    /// hanging from its long arms, and a crown of fern fronds rising from its head. The female's bigger hands are kept.
    /// </summary>
    /// <param name="female">A female keeps Tangrowth's: bigger red hands.</param>
    private static PokeBuilder Tanglewood(bool female = false)
    {
        var b = Become(Tangrowth(female), "Tanglewood", 0.98f);
        var blue = Rgb(46, 112, 140);
        var deep = Rgb(28, 70, 96);
        var leaf = Rgb(84, 150, 74);
        var fern = Rgb(98, 164, 80);
        Recolour(b, (Rgb(58, 132, 214), blue), (Rgb(30, 74, 146), deep), (Rgb(228, 58, 74), Rgb(176, 70, 66)));
        var c = V(0, 0.45f, 0);
        var r = V(0.27f, 0.3f, 0.24f);
        var rng = new GenomeRandom(2465);
        // Leaves and flowers caught in the vines
        for (int i = 0; i < 22; i++)
        {
            float a = rng.Range(-2.6f, 2.6f), y = rng.Range(-0.6f, 0.75f);
            var d = Vector3.Normalize(V(MathF.Sin(a), y, MathF.Cos(a)));
            var at = Out(c, r, default, d);
            if (i % 3 == 0)
            {
                var color = i % 2 == 0 ? Rgb(244, 160, 196) : Rgb(250, 220, 90);
                b.Ell(Body, at + d * 0.01f, V(0.018f, 0.018f, 0.018f), color, blend: 0.004f);
                b.Ell(Body, at + d * 0.022f, V(0.007f, 0.007f, 0.007f), Rgb(250, 240, 200), blend: 0.002f);
            }
            else
                Blade(b, Body, at - d * 0.01f, at + d * 0.05f + V(0, 0.02f, 0), 0.03f, leaf, Vector3.Cross(d, V(0, 1f, 0.1f)) + V(0, 0.2f, 0), 0.3f, Leaf);
        }
        // A crown of fern fronds rising from the head
        int head = BoneOf(b, "head");
        for (int i = 0; i < 9; i++)
        {
            float a = (-80f + 20f * i) * Degree;
            var root = V(MathF.Sin(a) * 0.06f, 0.74f, -0.02f + MathF.Cos(a) * 0.02f);
            var tip = root + V(MathF.Sin(a) * 0.22f, 0.24f - MathF.Abs(MathF.Sin(a)) * 0.08f, -0.06f);
            Frond(b, head, root, tip, 0.04f, fern, V(0, 0.4f, 1f), 0.2f, Leaf);
            var along = tip - root;
            var side = Vector3.Normalize(Vector3.Cross(along, V(0, 0, 1f)));
            for (int k = 1; k <= 4; k++)
            {
                var at = root + along * (0.2f * k);
                foreach (float sd in new[] { -1f, 1f })
                    b.Spike(head, at, at + side * sd * 0.04f * (1.1f - 0.2f * k) + along * 0.08f, 0.008f, fern, 0.4f, Leaf, 0.003f);
            }
        }
        // More vines hanging from the arms
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            foreach (var (x, drop) in new[] { (0.4f, 0.16f), (0.44f, 0.22f), (0.36f, 0.12f) })
            {
                var top = V(x * s, 0.5f, 0.1f);
                b.Tube(arm, Smooth(2, top, top + V(0.02f * s, -drop * 0.6f, 0.01f), top + V(-0.01f * s, -drop, 0.02f)), 0.016f, 0.01f, blue, blend: 0.004f);
            }
        });
        Stretch(b, V(1.06f, 1.06f, 1.06f));
        return b;
    }

    // ------------------------------------------------------------------ 2469 Meganeuron, from Yanmega

    /// <summary>
    /// Meganeuron: Yanmega grown into a giant of the old forests, its body banded green and red, a third pair of
    /// small wings between the others, its eyes greater, its tail longer and ending in a fan of long fins.
    /// </summary>
    private static PokeBuilder Meganeuron()
    {
        var b = Become(Yanmega(), "Meganeuron", 1f);
        var lift = Shifted(b, "head", V(0, 0.42f, 0.14f));
        var green = Rgb(74, 120, 72);
        var red = Rgb(170, 62, 64);
        var clear = Rgb(234, 238, 244);
        Recolour(b, (Rgb(86, 160, 72), green), (Rgb(50, 104, 52), red), (Rgb(236, 82, 112), Rgb(200, 52, 80)));
        // A third pair of small wings between the others
        PokeBuilder.Both(s =>
        {
            int third = b.Part(s < 0 ? "wing3L" : "wing3R", Body, V(0.07f * s, 0.48f, 0.04f) + lift, PokeRole.Wing, 0.2f, s);
            Dragonfly(b, third, V(0.07f * s, 0.475f, 0.04f) + lift, s, 0.13f, 0.045f, 2f, clear, Rgb(206, 210, 220), Rgb(60, 50, 50), 0.12f, 34f, 25f);
        });
        // Greater eyes
        int head = BoneOf(b, "head");
        Grow(b, V(1.25f, 1.25f, 1.2f), V(0, 0.41f, 0.17f) + lift, bone => bone == head);
        // A longer tail ending in a fan of fins
        int tail = BoneOf(b, "tail");
        var path = Smooth(3, V(0, 0.44f, -0.06f), V(0, 0.4f, -0.22f), V(0, 0.26f, -0.32f), V(0, 0.12f, -0.26f), V(0, 0.08f, -0.1f));
        b.Sdf.RemoveAll(s => s.Bone == tail);
        b.Tube(tail, path.Select(p => p + lift).ToArray(), 0.06f, 0.035f, green, blend: 0f);
        for (int i = 1; i < path.Length - 1; i += 2)
            b.PaintTorus(tail, path[i] + lift, 0.05f - 0.018f * i / path.Length, 0.01f, red, Euler(path[i + 1] - path[i - 1]));
        var end = path[^1] + lift;
        foreach (var (d, w) in new[] { (V(0, 0.4f, 1f), 0.05f), (V(0.5f, 0.2f, 1f), 0.045f), (V(-0.5f, 0.2f, 1f), 0.045f), (V(0, -0.3f, 1f), 0.04f) })
            Blade(b, tail, end, end + Vector3.Normalize(d) * 0.2f, w, Rgb(120, 150, 110), V(0, 1f, 0), 0.2f);
        Stretch(b, V(1.08f, 1.04f, 1.12f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2470 Dryadeon, from Leafeon

    /// <summary>
    /// Dryadeon: Leafeon longer of body and leg, its coat a sage green, a mane of overlapping leaves round its neck
    /// and shoulders, its ears long leaves, and its tail three long leaves with small flowers among them.
    /// </summary>
    private static PokeBuilder Dryadeon()
    {
        var b = Become(Leafeon(), "Dryadeon", 0.92f);
        const float h = 0.24f;
        var sage = Rgb(204, 206, 156);
        var olive = Rgb(122, 140, 86);
        var leaf = Rgb(98, 156, 92);
        var pale = Rgb(176, 198, 120);
        Recolour(b, (Rgb(242, 228, 172), sage), (Rgb(150, 100, 60), olive), (Rgb(76, 172, 96), leaf));
        // Longer leaf ears
        int head = BoneOf(b, "head");
        PokeBuilder.Both(s =>
        {
            int ear = BoneOf(b, s < 0 ? "earL" : "earR");
            Grow(b, V(1.1f, 1.5f, 1f), V(0.055f * s, h + 0.26f, 0.13f), bone => bone == ear);
        });
        // A mane of overlapping leaves round its neck and shoulders
        var rng = new GenomeRandom(2470);
        for (int ring = 0; ring < 3; ring++)
            for (int i = 0; i < 11; i++)
            {
                float a = (i / 10f - 0.5f) * 3.6f + ring * 0.12f;
                var root = V(MathF.Sin(a) * 0.08f, h + 0.16f - ring * 0.04f, 0.1f + MathF.Cos(a) * 0.04f - ring * 0.03f);
                var dir = Vector3.Normalize(V(MathF.Sin(a), -0.6f - ring * 0.15f, MathF.Cos(a) * 0.6f + 0.2f));
                Blade(b, Body, root, root + dir * (0.1f + rng.Range(0f, 0.03f)), 0.035f, (i + ring) % 3 == 0 ? pale : leaf, V(0, 0.5f, 1f), 0.25f, Leaf);
            }
        // A tail of three long leaves with flowers among them
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail);
        foreach (var (dx, dy) in new[] { (0f, 0.18f), (0.1f, 0.08f), (-0.08f, 0.04f) })
        {
            var root = V(0, h + 0.04f, -0.16f);
            var tip = root + V(dx, dy, -0.36f);
            Blade(b, tail, root, tip, 0.05f, leaf, V(1f, 0.4f, 0.1f), 0.2f, Leaf);
            foreach (float t in new[] { 0.45f, 0.75f })
            {
                var at = Vector3.Lerp(root, tip, t) + V(0, 0.015f, 0);
                b.Ell(tail, at, V(0.014f, 0.014f, 0.014f), t < 0.6f ? Rgb(244, 150, 180) : Rgb(250, 220, 96), blend: 0.003f);
            }
        }
        Stretch(b, V(0.96f, 1.12f, 1.2f));
        return b;
    }

    // ------------------------------------------------------------------ 2471 Borealeon, from Glaceon

    /// <summary>
    /// Borealeon: Glaceon longer and paler, a mantle of ice crystals over its shoulders, the locks at its ears grown
    /// into long hanging crystals, a teal saddle down its back and an aurora streaming from it into a long flowing
    /// tail of green and blue light (Glow).
    /// </summary>
    private static PokeBuilder Borealeon()
    {
        var b = Become(Glaceon(), "Borealeon", 0.94f);
        const float h = 0.22f;
        var pale = Rgb(214, 236, 246);
        var teal = Rgb(40, 130, 150);
        var crystal = Rgb(190, 228, 248);
        var auroraG = Rgb(120, 240, 170);
        var auroraB = Rgb(110, 200, 250);
        Recolour(b, (Rgb(176, 222, 244), pale), (Rgb(40, 110, 170), teal));
        b.Model.Decals.RemoveAll(d => d.Shape == MarkShape.Diamond);
        b.PaintEll(Body, V(0, h + 0.09f, -0.04f), V(0.06f, 0.03f, 0.13f), teal);
        int head = BoneOf(b, "head");
        // The locks grow long and end in hanging crystals
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == head && s.Color.Equals(teal) && s.A.Y < h + 0.13f).ToList())
        {
            s.Color = crystal;
            s.Material = Shell;
        }
        PokeBuilder.Both(s => b.Box(head, V(0.11f * s, h + 0.0f, 0.15f), V(0.014f, 0.05f, 0.01f), 0.004f, crystal, V(0, 0, 0), Shell, 0.004f));
        // A mantle of ice crystals over its shoulders
        for (int i = 0; i < 12; i++)
        {
            float a = (i / 11f - 0.5f) * 3.4f;
            var root = V(MathF.Sin(a) * 0.08f, h + 0.12f, 0.08f + MathF.Cos(a) * 0.04f);
            var dir = Vector3.Normalize(V(MathF.Sin(a) * 1.2f, 0.5f + (i % 3) * 0.2f, MathF.Cos(a) * 0.3f - 0.2f));
            b.Spike(Body, root, root + dir * (0.08f + (i % 3) * 0.02f), 0.022f, crystal, 0.6f, Shell, 0.004f);
        }
        // The aurora streaming from its back into a long flowing tail of light
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail);
        foreach (var (dy, dx, color) in new[] { (0.0f, 0f, auroraB), (0.04f, 0.04f, auroraG), (0.08f, -0.03f, auroraG) })
        {
            var path = Smooth(3, V(0, h + 0.08f, -0.06f), V(dx, h + 0.1f + dy, -0.24f), V(dx * 2f, h + 0.06f + dy, -0.42f), V(dx * 3f, h + 0.12f + dy, -0.6f));
            As(b, Glow, () => b.Tube(tail, path, 0.04f, 0.014f, color, blend: 0.012f));
        }
        b.Tube(tail, Smooth(3, V(0, h + 0.04f, -0.16f), V(0, h + 0.02f, -0.3f), V(0.06f, h - 0.04f, -0.42f), V(0.14f, h + 0.04f, -0.52f)), 0.035f, 0.015f, pale, blend: 0.01f);
        Stretch(b, V(0.96f, 1.08f, 1.18f));
        return b;
    }

    // ------------------------------------------------------------------ 2472 Fangliscor, from Gliscor

    /// <summary>
    /// Fangliscor: Gliscor larger and darker, plated in dull gold over its chest, its wings wider and darker with a
    /// claw at each wing's joint, long fangs from its jaw and its tail's pincer become a single great hooked sting.
    /// </summary>
    private static PokeBuilder Fangliscor()
    {
        var b = Become(Gliscor(), "Fangliscor", 0.98f);
        var lift = Shifted(b, "head", V(0, 0.58f, 0.02f));
        var violet = Rgb(92, 70, 140);
        var dark = Rgb(46, 34, 62);
        var lining = Rgb(110, 70, 110);
        var gold = Rgb(168, 146, 92);
        var bone = Rgb(206, 186, 176);
        Recolour(b, (Rgb(144, 134, 194), violet), (Rgb(58, 54, 66), dark), (Rgb(214, 40, 56), lining));
        // Gold plates over its chest
        b.PaintEll(Body, V(0, 0.46f, 0.06f) + lift, V(0.07f, 0.11f, 0.05f), gold);
        // Wider wings, a claw at each wing's joint
        PokeBuilder.Both(s =>
        {
            int w = BoneOf(b, s < 0 ? "wingL" : "wingR");
            Grow(b, V(1.3f, 1.12f, 1f), V(0.08f * s, 0.55f, 0) + lift, sh => sh.Bone == w && sh.Color.Equals(dark) || sh.Bone == w && sh.Op == SdfOp.Paint, _ => false);
            var joint = V(0.25f * s, 0.72f, 0.03f) + lift;
            b.Tube(w, Smooth(3, joint + V(0, 0.04f, 0), joint + V(-0.05f * s, 0.1f, 0.02f), joint + V(-0.1f * s, 0.08f, 0.04f)), 0.016f, 0.004f, bone, Shell, 0.004f);
        });
        // Long fangs from its jaw
        int head = BoneOf(b, "head");
        PokeBuilder.Both(s => b.Spike(head, V(0.04f * s, 0.62f, 0.12f) + lift, V(0.05f * s, 0.53f, 0.14f) + lift, 0.012f, bone, mat: Shell, blend: 0.004f));
        // The tail's pincer a single hooked sting
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail && s.Shape == SdfShape.RoundCone);
        var end = V(0.05f, 0.06f, 0.02f) + lift;
        b.Tube(tail, Smooth(3, end, end + V(0.06f, 0.06f, 0.0f), end + V(0.08f, 0.16f, -0.02f), end + V(0.03f, 0.2f, 0.02f)), 0.03f, 0.005f, bone, Shell, 0.006f);
        Stretch(b, V(1.06f, 1.08f, 1.06f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2476 Lodestarch, from Probopass

    /// <summary>
    /// Lodestarch: Probopass larger and darker, its red cap become a tall red shard at its peak like a compass's
    /// needle, its iron moustache grown into great sweeping whiskers, and five Mini-Noses round it in a star, their
    /// tips red and blue in turn.
    /// </summary>
    private static PokeBuilder Lodestarch()
    {
        var b = Become(Probopass(), "Lodestarch", 0.96f);
        var stone = Rgb(72, 86, 112);
        var red = Rgb(196, 44, 56);
        var blue = Rgb(64, 96, 190);
        var iron = Rgb(56, 56, 62);
        Recolour(b, (Rgb(102, 152, 208), stone), (Rgb(226, 68, 68), red), (Rgb(62, 62, 68), iron));
        // The cap becomes a tall shard
        b.Sdf.RemoveAll(s => s.Bone == Body && s.Shape == SdfShape.Ellipsoid && s.Color.Equals(red));
        b.Spike(Body, V(0, 0.5f, 0.0f), V(0.05f, 0.86f, -0.06f), 0.06f, red, 0.4f, Shell, 0.01f);
        // The nose turned stone with a dark band, as a lodestone's
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == Body && s.Shape == SdfShape.RoundCone && s.Color.Equals(red) && s.A.Z > 0.1f)) s.Color = PixelCanvas.Light1(stone, 0.1f);
        // Brows of stone
        PokeBuilder.Both(s => b.Box(Body, V(0.08f * s, 0.5f, 0.15f), V(0.05f, 0.015f, 0.02f), 0.01f, stone, V(0, 0, -15f * s), Shell, 0.008f));
        // Great sweeping whiskers of iron
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < 4; i++)
            {
                float dy = i * 0.02f;
                var path = Smooth(3, V(0.03f * s, 0.3f - dy, 0.19f), V(0.14f * s, 0.2f - dy, 0.2f), V(0.28f * s, 0.06f - dy * 0.5f, 0.14f), V(0.4f * s, 0.06f + dy, 0.08f));
                b.Tube(Body, path, 0.03f - i * 0.004f, 0.006f, iron, blend: 0.006f);
            }
        });
        // Three more Mini-Noses, five in all round it in a star, tipped red and blue in turn
        foreach (var (at, tip, color) in new[] { (V(-0.2f, 0.52f, -0.02f), V(-0.27f, 0.58f, -0.02f), blue), (V(0.2f, 0.52f, -0.02f), V(0.27f, 0.58f, -0.02f), red), (V(0, 0.36f, -0.2f), V(0, 0.4f, -0.28f), blue) })
        {
            b.Box(Body, at, V(0.045f, 0.05f, 0.045f), 0.022f, stone, Euler(tip - at), Shell, 0.01f);
            b.Spike(Body, at + (tip - at) * 0.4f, tip, 0.03f, color, mat: Shell, blend: 0.006f);
        }
        PokeBuilder.Both(s =>
        {
            int unit = BoneOf(b, s < 0 ? "armL" : "armR");
            foreach (var sh in b.Sdf.Shapes.Where(sh => sh.Bone == unit && sh.Color.Equals(red))) sh.Color = s < 0 ? red : blue;
        });
        Stretch(b, V(1.08f, 1.04f, 1.08f));
        return b;
    }

    // ------------------------------------------------------------------ 2478 Frostbride, from Froslass

    /// <summary>
    /// Frostbride: Froslass taller, robed in white and lilac with a gown that frays into ice at its hem, its sleeves
    /// trailing into a veil of snow, its sash crimson and its ribbons long enough to trail below it, and a crown of
    /// ice crystals on its head.
    /// </summary>
    private static PokeBuilder Frostbride()
    {
        var b = Become(Froslass(), "Frostbride", 0.96f);
        var lift = Shifted(b, "head", V(0, 0.58f, 0));
        var white = Rgb(244, 242, 250);
        var lilac = Rgb(220, 212, 240);
        var ice = Rgb(176, 214, 240);
        var crimson = Rgb(190, 42, 64);
        Recolour(b, (Rgb(240, 84, 40), crimson), (Rgb(132, 82, 184), Rgb(150, 140, 200)));
        // A gown falling from the waist, fraying into ice at its hem
        var hem = V(0, 0.2f, 0) + lift;
        b.Ell(Body, V(0, 0.3f, 0) + lift, V(0.1f, 0.14f, 0.09f), white, blend: 0.03f);
        b.PaintEll(Body, V(0, 0.42f, 0.07f) + lift, V(0.04f, 0.1f, 0.04f), lilac);
        for (int i = 0; i < 9; i++)
        {
            float a = i * MathF.Tau / 9f;
            var at = hem + V(MathF.Sin(a) * 0.08f, 0.02f, MathF.Cos(a) * 0.07f);
            b.Spike(Body, at, at + V(MathF.Sin(a) * 0.04f, -0.14f - 0.04f * (i % 3), MathF.Cos(a) * 0.03f), 0.035f, i % 2 == 0 ? ice : white, 0.5f, blend: 0.01f);
        }
        // Sleeves trailing into a veil of snow
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var cuff = V(0.05f * s, 0.45f, 0.12f) + lift;
            foreach (var (dx, len) in new[] { (0.08f, 0.3f), (0.12f, 0.24f), (0.04f, 0.34f) })
                Blade(b, arm, cuff, cuff + V(dx * s, -len, -0.08f), 0.045f, (int)(dx * 100) % 2 == 0 ? white : ice, V(s, 0, 0.3f), 0.2f);
        });
        // Long ribbons trailing from the sash
        PokeBuilder.Both(s =>
        {
            var knot = V(0.03f * s, 0.4f, 0.09f) + lift;
            b.Tube(Body, Smooth(3, knot, knot + V(0.06f * s, -0.12f, 0.03f), knot + V(0.04f * s, -0.28f, 0.0f), knot + V(0.12f * s, -0.38f, 0.02f)), 0.014f, 0.01f, crimson, blend: 0.004f);
        });
        // A crown of ice crystals
        int head = BoneOf(b, "head");
        foreach (var (x, h) in new[] { (0f, 0.14f), (-0.04f, 0.1f), (0.04f, 0.1f), (-0.075f, 0.07f), (0.075f, 0.07f) })
            b.Spike(head, V(x, 0.73f, 0.0f) + lift, V(x * 1.3f, 0.73f + h, -0.02f) + lift, 0.022f, ice, 0.6f, Shell, 0.006f);
        Stretch(b, V(0.96f, 1.16f, 0.96f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ the lake guardians' heirs (2480 to 2482)

    /// <summary>
    /// What the three heirs of the lakes add to their forebears' shared body: a third tail between the two, its gem
    /// glowing as theirs do, and a ring of light hugging the crown of the head (Glow) at <paramref name="ringY"/>.
    /// </summary>
    private static void HeirOfTheLake(PokeBuilder b, Vector3 lift, Color skin, Color gem, Color ring, float ringY, float ringR)
    {
        int tail = b.Part("tailM", Body, V(0, 0.35f, -0.02f) + lift, PokeRole.Tail, 0.2f);
        var path = Smooth(3, V(0, 0.36f, -0.02f), V(0, 0.22f, -0.07f), V(0, 0.12f, -0.12f), V(0, 0.08f, -0.2f), V(0, 0.13f, -0.26f)).Select(p => p + lift).ToArray();
        b.Tube(tail, path, 0.03f, 0.017f, skin, blend: 0f);
        var tip = path[^1];
        foreach (var d in new[] { V(0, 1f, -0.2f), V(0.8f, -0.2f, -0.3f), V(-0.8f, -0.2f, -0.3f) })
            Blade(b, tail, tip, tip + Vector3.Normalize(d) * 0.07f, 0.03f, skin, V(0, 0.2f, 1f), 0.35f);
        b.Ell(tail, tip + V(0, 0, 0.016f), V(0.022f, 0.027f, 0.016f), gem, mat: Glow, blend: 0.004f);
        int head = BoneOf(b, "head");
        As(b, Glow, () => b.Torus(head, V(0, ringY, -0.01f) + lift, ringR, 0.008f, ring, V(-8f, 0, 0), blend: 0.004f));
    }

    /// <summary>Uxinous: Uxie larger, its helmet's crest swept long and high, a third tail and gem, and a ring of light round its crown.</summary>
    private static PokeBuilder Uxinous()
    {
        var b = Become(Uxie(), "Uxinous", 0.6f);
        var lift = Shifted(b, "head", V(0, 0.54f, 0));
        var skin = Rgb(226, 230, 236);
        var yellow = Rgb(240, 206, 80);
        Recolour(b, (Rgb(190, 214, 238), skin), (Rgb(246, 206, 60), yellow));
        int head = BoneOf(b, "head");
        // A crest swept up from the helmet, and its side lobes drawn out into points
        b.Tube(head, Smooth(3, V(0, 0.74f, -0.02f), V(0.02f, 0.84f, -0.05f), V(0.07f, 0.92f, -0.08f), V(0.13f, 0.94f, -0.06f)).Select(p => p + lift).ToArray(), 0.045f, 0.012f, yellow, blend: 0.012f);
        PokeBuilder.Both(s => b.Spike(head, V(0.12f * s, 0.66f, -0.02f) + lift, V(0.2f * s, 0.78f, -0.06f) + lift, 0.045f, yellow, 0.5f));
        HeirOfTheLake(b, lift, skin, Rgb(232, 50, 70), Rgb(250, 228, 170), 0.715f, 0.118f);
        Stretch(b, V(1.08f, 1.12f, 1.08f));
        return Lift(b);
    }

    /// <summary>Mespathos: Mesprit larger, its locks grown long and swept back like hair, a third tail and gem, and a ring of light round its crown.</summary>
    private static PokeBuilder Mespathos()
    {
        var b = Become(Mesprit(), "Mespathos", 0.6f);
        var lift = Shifted(b, "head", V(0, 0.54f, 0));
        var skin = Rgb(236, 226, 232);
        var pink = Rgb(236, 150, 180);
        Recolour(b, (Rgb(190, 214, 238), skin), (Rgb(244, 150, 190), pink));
        int head = BoneOf(b, "head");
        // Long locks swept back from the head like hair
        foreach (var (x, y, len) in new[] { (-0.06f, 0.72f, 0.3f), (0.0f, 0.73f, 0.34f), (0.06f, 0.71f, 0.28f), (-0.1f, 0.66f, 0.24f), (0.1f, 0.65f, 0.22f) })
        {
            var root = V(x, y, -0.02f) + lift;
            b.Tube(head, Smooth(3, root, root + V(x * 0.6f, 0.03f, -len * 0.5f), root + V(x * 1.2f, -0.06f, -len)), 0.04f, 0.03f, pink, blend: 0.012f);
        }
        HeirOfTheLake(b, lift, skin, Rgb(232, 50, 70), Rgb(250, 228, 150), 0.72f, 0.105f);
        Stretch(b, V(1.08f, 1.12f, 1.08f));
        return Lift(b);
    }

    /// <summary>Azelthymos: Azelf larger and deeper blue, its points grown long into a crown, a third tail and gem, and a ring of red light round its crown.</summary>
    private static PokeBuilder Azelthymos()
    {
        var b = Become(Azelf(), "Azelthymos", 0.6f);
        var lift = Shifted(b, "head", V(0, 0.54f, 0));
        var skin = Rgb(196, 210, 236);
        var blue = Rgb(70, 110, 210);
        Recolour(b, (Rgb(190, 214, 238), skin), (Rgb(60, 168, 230), blue));
        int head = BoneOf(b, "head");
        // The points grow long, swept up and out like a crown
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == head && s.Shape == SdfShape.RoundCone && s.Color.Equals(blue)).ToList())
        {
            var d = s.B - s.A;
            s.B = s.A + V(d.X * 1.2f, MathF.Abs(d.Y) * 1.6f + 0.12f, d.Z * 0.6f);
        }
        PokeBuilder.Both(s => b.Spike(head, V(0.05f * s, 0.74f, -0.02f) + lift, V(0.16f * s, 0.96f, -0.06f) + lift, 0.035f, blue, 0.6f));
        HeirOfTheLake(b, lift, skin, Rgb(232, 50, 70), Rgb(250, 110, 130), 0.71f, 0.108f);
        Stretch(b, V(1.08f, 1.12f, 1.08f));
        return Lift(b);
    }
}
