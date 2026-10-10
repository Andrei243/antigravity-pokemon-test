using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Popular species from outside the Sinnoh Pokédex (plan 03, decision 3), Paldea's fourth batch in National Pokédex
// order: Tatsugiri (978) to Gholdengo (1000), with Tatsugiri's three looks and their Megas, Dudunsparce's Three-Segment
// Form, Baxcalibur's Mega and Gimmighoul's Roaming Form beside their species (Veluza and Dondozo, 976 and 977, are in
// PokemonModels.Paldea3.cs). Helpers shared with the earlier batches are in the files of those batches,
// PokemonModels.Sinnoh1.cs to PokemonModels.Paldea3.cs.
internal static partial class PokemonModels
{
    // ------------------------------------------------------------------ Tatsugiri

    /// <summary>Tatsugiri's three looks: their names, the colour of the slice of fish over their backs, its pale lines and its dark parts.</summary>
    private static readonly (string Name, string Mega, Color Main, Color Stripe, Color Dark)[] TatsugiriLooks =
    {
        ("Tatsugiri", "Tatsugiri-Curly-Mega", Rgb(242, 128, 92), Rgb(252, 216, 198), Rgb(204, 78, 64)),
        ("Tatsugiri-Droopy", "Tatsugiri-Droopy-Mega", Rgb(234, 100, 140), Rgb(248, 198, 216), Rgb(176, 54, 98)),
        ("Tatsugiri-Stretchy", "Tatsugiri-Stretchy-Mega", Rgb(250, 204, 74), Rgb(253, 238, 178), Rgb(222, 150, 50))
    };

    /// <summary>
    /// Tatsugiri in one of its three looks, or (<paramref name="mega"/>) its Mega form: a little dragon of a fish like a
    /// piece of sushi, its body and face white as rice under a slice of fish draped over its back and the top of its
    /// head (salmon orange and curly, pink and droopy, or egg yellow and stretchy) with pale lines across it, a tail
    /// curling up behind, little side fins, big golden eyes and a small smile. On its head a fin of the slice's colour
    /// curls back over itself (Curly), hangs down behind (Droopy) or stands straight up (Stretchy). The Mega form is
    /// bigger, a band of dark seaweed wrapped round its middle and knotted with a gold bead, two ends of it trailing
    /// behind, its head fin and side fins grown long and tipped gold.
    /// </summary>
    private static PokeBuilder TatsugiriBuild(int look, bool mega)
    {
        var (name, megaName, main, stripe, dark) = TatsugiriLooks[look];
        float k = mega ? 1.2f : 1f;
        float g = mega ? 1.5f : 1f;
        var gold = Rgb(244, 198, 72);
        var rice = Rgb(250, 247, 238);
        var b = new PokeBuilder(mega ? megaName : name, mega ? 0.5f : 0.42f, BodyPlan.Fish, V(0, 0.13f, 0) * k) { Coat = Scales }.Hover();
        var bc = V(0, 0.13f, 0) * k;
        var br = V(0.045f, 0.06f, 0.042f) * k;
        b.Ell(Body, bc, br, rice);
        // The slice of fish draped over its back, pale lines across it
        b.PaintEll(Body, bc + V(0, 0.01f, -0.035f) * k, V(0.06f, 0.075f, 0.04f) * k, main);
        foreach (float y in new[] { 0.022f, -0.014f })
            b.PaintEll(Body, bc + V(0, y, -0.04f) * k, V(0.06f, 0.006f, 0.03f) * k, stripe, V(-25f, 0, 0), 0.004f * k);
        // The tail curling up behind, a little fin at its end
        int tail = b.Tail(bc + V(0, -0.045f, -0.01f) * k);
        var tp = Smooth(3, bc + V(0, -0.04f, -0.005f) * k, bc + V(0, -0.08f, -0.03f) * k, bc + V(0, -0.075f, -0.07f) * k, bc + V(0, -0.045f, -0.085f) * k);
        b.Tube(tail, tp, 0.03f * k, 0.013f * k, main, blend: 0f);
        Frond(b, tail, tp[^1], tp[^1] + V(0, 0.04f, -0.02f) * k * g, 0.016f * k, dark, V(1f, 0, 0), 0.3f);
        // The little fins at its sides
        PokeBuilder.Both(s =>
        {
            var root = bc + V(0.035f * s, -0.01f, 0f) * k;
            int fin = b.Part(s < 0 ? "finL" : "finR", Body, root, PokeRole.Fin, s, s);
            var tip = root + V(0.045f * s, -0.015f, -0.02f) * k * g;
            Frond(b, fin, root, tip, 0.017f * k, main, V(0, 1f, 0.2f), 0.3f);
            if (mega) b.PaintEll(fin, tip, V(0.022f, 0.022f, 0.022f) * k, gold, soft: 0.004f * k);
        });
        if (mega)
        {
            // The band of seaweed round its middle, knotted in front with a gold bead, its two ends trailing behind
            var nori = Rgb(44, 66, 56);
            var band = bc + V(0, -0.012f, 0) * k;
            float ring = br.X * 0.97f;
            b.Torus(Body, band, ring, 0.011f * k, nori, sz: br.Z / br.X, blend: 0.004f);
            b.Ell(Body, band + V(0, 0, br.Z * 0.97f + 0.008f * k), V(0.012f, 0.012f, 0.01f) * k, gold, mat: Metal, blend: 0.004f);
            PokeBuilder.Both(s => Frond(b, Body, band + V(0.01f * s, 0, -br.Z * 0.9f), band + V(0.03f * s, -0.065f, -0.075f) * k, 0.012f * k, nori, V(0, 0.2f, -1f), 0.3f));
        }
        int head = b.Head(bc + V(0, 0.045f, 0.005f) * k);
        var c = bc + V(0, 0.09f, 0.012f) * k;
        var r = V(0.05f, 0.045f, 0.047f) * k;
        b.Limb(head, bc + V(0, 0.03f, 0) * k, c + V(0, -0.025f, -0.005f) * k, 0.03f * k, 0.034f * k, rice);
        b.Ell(head, c, r, rice);
        b.PaintEll(head, c + V(0, 0.03f, -0.02f) * k, V(0.06f, 0.035f, 0.055f) * k, main);
        // The fin on its head
        var from = c + V(0, 0.03f, -0.012f) * k;
        int crest = b.Part("crest", head, from, PokeRole.Fin);
        Vector3 crestTip;
        switch (look)
        {
            case 0:
                // Curly: rising from its crown and curling back over on itself
                var center = from + V(0, 0.04f, -0.025f) * k * g;
                Curl(b, crest, from, center, V(0, 0, 1f), V(0, 1f, 0), 0.025f * k * g, 1.3f, 0.013f * k * g, main);
                crestTip = center + V(0, 0.025f, 0) * k * g;
                break;
            case 1:
                // Droopy: flopping back from its crown and hanging down behind its head
                var droop = Smooth(3, from, from + V(0, 0.03f, -0.03f) * k * g, from + V(0, 0.015f, -0.07f) * k * g, from + V(0, -0.035f, -0.085f) * k * g);
                b.Tube(crest, droop, 0.016f * k * g, 0.01f * k * g, main, blend: 0f);
                crestTip = droop[^1];
                break;
            default:
                // Stretchy: standing straight up, darker at its tip
                crestTip = from + V(0, 0.08f, -0.012f) * k * g;
                Frond(b, crest, from, crestTip, 0.018f * k * g, main, V(0, 0, 1f), 0.3f);
                b.PaintEll(crest, crestTip + V(0, -0.006f, 0) * k * g, V(0.02f, 0.016f, 0.02f) * k * g, dark, soft: 0.004f * k);
                break;
        }
        if (mega) b.PaintEll(crest, crestTip, V(0.022f, 0.022f, 0.022f) * k * g, gold, soft: 0.004f * k);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.022f * s, c.Y + 0.002f * k);
            b.Eye(head, at, Outward(c, r, at), 0.012f * k, Rgb(236, 176, 48));
        });
        var mouth = On(c, r, c.X, c.Y - 0.025f * k);
        b.Mark(head, mouth, Outward(c, r, mouth), 0.012f * k, 0.006f * k, dark, MarkShape.Smile);
        return Lift(b);
    }

    private static PokeBuilder Tatsugiri() => TatsugiriBuild(0, false);

    private static PokeBuilder TatsugiriDroopy() => TatsugiriBuild(1, false);

    private static PokeBuilder TatsugiriStretchy() => TatsugiriBuild(2, false);

    private static PokeBuilder TatsugiriCurlyMega() => TatsugiriBuild(0, true);

    private static PokeBuilder TatsugiriDroopyMega() => TatsugiriBuild(1, true);

    private static PokeBuilder TatsugiriStretchyMega() => TatsugiriBuild(2, true);

    // ------------------------------------------------------------------ Annihilape

    /// <summary>Annihilape: Primeape's rage outlived, a tall ball of pale grey shaggy fur with pointed ears, a grey mask of a face with a pig's snout and furious white eyes with red pupils, wisps of violet ghost fire rising from its crown, and long, thick, near-black arms and legs whose fists are big as its head, rings of glowing violet energy where Primeape's shackles were.</summary>
    private static PokeBuilder Annihilape()
    {
        var b = new PokeBuilder("Annihilape", 0.85f, BodyPlan.Biped, V(0, 0.46f, 0)) { Coat = Fur };
        var fur = Rgb(212, 212, 222);
        var limb = Rgb(72, 68, 88);
        var face = Rgb(168, 164, 182);
        var ghost = Rgb(134, 112, 240);
        var c = V(0, 0.46f, 0);
        var r = V(0.19f, 0.21f, 0.17f);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.09f * s, 0.32f, 0));
            var knee = V(0.17f * s, 0.2f, 0.05f);
            var ankle = V(0.16f * s, 0.07f, 0.04f);
            b.Tube(leg, new[] { V(0.09f * s, 0.33f, 0), knee, ankle }, 0.042f, 0.036f, limb, blend: 0f);
            b.Ell(leg, V(0.16f * s, 0.032f, 0.08f), V(0.05f, 0.032f, 0.08f), limb);
            b.Torus(leg, ankle + V(0, 0.015f, 0), 0.04f, 0.012f, ghost, Euler(knee - ankle), mat: Glow, blend: 0.004f);

            // Long arms held up to fight, great fists at their ends
            var shoulder = V(0.16f * s, c.Y + 0.02f, 0);
            int arm = b.Arm(s, shoulder);
            var elbow = V(0.31f * s, c.Y - 0.06f, 0.05f);
            var wrist = V(0.3f * s, c.Y + 0.08f, 0.15f);
            b.Tube(arm, new[] { shoulder - V(0.01f * s, 0, 0), elbow, wrist }, 0.05f, 0.042f, limb, blend: 0f);
            b.Ell(arm, wrist + V(0, 0.045f, 0.025f), V(0.066f, 0.062f, 0.06f), limb);
            b.Torus(arm, wrist, 0.046f, 0.014f, ghost, Euler(wrist - elbow), mat: Glow, blend: 0.004f);
        });
        b.Ell(Body, c, r, fur);
        FurTufts(b, Body, c, r, 72, 0.075f, 0.032f, fur, 0.6f, -0.7f, 0.35f);
        PokeBuilder.Both(s =>
        {
            int e = b.Ear(Body, s, V(0.11f * s, c.Y + 0.15f, -0.01f));
            CatEar(b, e, V(0.11f * s, c.Y + 0.14f, -0.01f), V(0.19f * s, c.Y + 0.27f, -0.03f), 0.05f, fur, face);
        });
        // Wisps of ghost fire rising from its crown
        foreach (var (x, lean) in new[] { (-0.045f, -1f), (0f, 0f), (0.045f, 1f) })
        {
            var root = c + V(x, r.Y * 0.86f, -0.03f);
            b.Spike(Body, root, root + V(0.035f * lean, 0.12f - 0.025f * MathF.Abs(lean), -0.06f), 0.026f, ghost, 0.6f, Glow, 0.01f);
        }
        // The grey mask of its face, the snout and the furious eyes
        b.PaintEll(Body, On(c, r, 0, c.Y + 0.01f) + V(0, 0, -0.02f), V(0.13f, 0.1f, 0.06f), face);
        Snout(b, Body, On(c, r, 0, c.Y - 0.035f) + V(0, 0, 0.004f), 0.05f, Rgb(204, 178, 194));
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.066f * s, c.Y + 0.05f), V(0.4f * s, 0.1f, 1f), 0.03f, sclera: true, pupil: Rgb(200, 40, 52), glare: true));
        return b;
    }

    // ------------------------------------------------------------------ Clodsire

    /// <summary>Clodsire: a great low, round lump of dark brown on four stubby legs, its head one with its body, wide and flat with small dark eyes far apart and a long thin smile over a paler chin, a cluster of three pale lilac spikes like gills at each side of its head, a row of tall pale spines down its back and a broad flat tail.</summary>
    private static PokeBuilder Clodsire()
    {
        var b = new PokeBuilder("Clodsire", 0.9f, BodyPlan.Quadruped, V(0, 0.2f, -0.04f)) { Coat = Fur };
        var brown = Rgb(108, 86, 80);
        var dark = Rgb(56, 44, 46);
        var chin = Rgb(142, 118, 110);
        var gill = Rgb(224, 208, 238);
        StubbyLegs(b, 0.15f, 0.12f, 0.12f, -0.2f, 0.05f, brown);
        var bc = V(0, 0.2f, -0.04f);
        var br = V(0.25f, 0.165f, 0.3f);
        b.Ell(Body, bc, br, brown);
        // The row of spines down its back, flat like fins
        for (int i = 0; i < 5; i++)
        {
            float z = 0.12f - 0.085f * i;
            float top = br.Y * MathF.Sqrt(1f - z * z / (br.Z * br.Z));
            var root = bc + V(0, top - 0.025f, z);
            Blade(b, Body, root, root + V(0, 0.1f - 0.012f * MathF.Abs(i - 2), -0.045f), 0.034f, gill, V(1f, 0, 0), 0.3f, Shell);
        }
        // The broad flat tail
        int tail = b.Tail(bc + V(0, -0.04f, -0.26f));
        b.Ell(tail, bc + V(0, -0.07f, -0.38f), V(0.15f, 0.035f, 0.13f), brown, V(-8f, 0, 0), blend: 0.04f);
        // The wide head, one with its body
        int head = b.Head(bc + V(0, 0.03f, 0.2f));
        var c = bc + V(0, 0.05f, 0.27f);
        var r = V(0.2f, 0.13f, 0.12f);
        b.Ell(head, c, r, brown, blend: 0.06f);
        b.PaintEll(head, c + V(0, -0.09f, 0.04f), V(0.17f, 0.05f, 0.1f), chin);
        PokeBuilder.Both(s =>
        {
            // The spiked gills at each side of its head
            var root = c + V(0.17f * s, 0.03f, -0.05f);
            int g = b.Ear(head, s, root);
            b.Ell(g, root, V(0.032f, 0.03f, 0.03f), gill, mat: Shell, blend: 0.012f);
            foreach (var d in new[] { V(1f, 0.65f, -0.2f), V(1f, 0f, -0.4f), V(1f, -0.55f, -0.2f) })
                b.Spike(g, root, root + Vector3.Normalize(V(d.X * s, d.Y, d.Z)) * 0.11f, 0.022f, gill, 0.7f, Shell);
            var at = On(c, r, c.X + 0.085f * s, c.Y + 0.05f);
            b.Eye(head, at, Outward(c, r, at), 0.017f, Rgb(36, 30, 32));
        });
        var mouth = On(c, r, c.X, c.Y - 0.035f);
        b.Mark(head, mouth, Outward(c, r, mouth), 0.08f, 0.014f, dark, MarkShape.Smile);
        return b;
    }

    // ------------------------------------------------------------------ Farigiraf

    /// <summary>Farigiraf: a tall giraffe, yellow patched brown, on long legs, yellow in front and brown behind, a long neck with a brown mane down its back, a head with a pink muzzle, ears out to the sides and two tall cream horns tipped with dark knobs; its tail has grown into a great dark grey head that sits over its rump like a helmet, looking back with yellow ringed eyes over a mouth full of white teeth, two little horns on top.</summary>
    private static PokeBuilder Farigiraf()
    {
        var b = new PokeBuilder("Farigiraf", 1f, BodyPlan.Quadruped, V(0, 0.52f, -0.01f)) { Coat = Fur };
        var yellow = Rgb(240, 206, 110);
        var brown = Rgb(124, 84, 62);
        var cream = Rgb(246, 236, 210);
        var pink = Rgb(228, 140, 168);
        var helmet = Rgb(74, 68, 80);
        var hoof = Rgb(98, 92, 104);
        foreach (var (z, front, color) in new[] { (0.15f, true, yellow), (-0.16f, false, brown) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.065f * s, 0.45f, z), front);
                var knee = V(0.07f * s, 0.25f, z + 0.01f);
                b.Limb(leg, V(0.065f * s, 0.46f, z), knee, 0.042f, 0.03f, color);
                b.Limb(leg, knee, V(0.07f * s, 0.06f, z + 0.02f), 0.03f, 0.026f, color);
                b.Ell(leg, V(0.07f * s, 0.03f, z + 0.03f), V(0.031f, 0.03f, 0.037f), hoof, mat: Shell);
            });
        var bc = V(0, 0.52f, -0.01f);
        var br = V(0.12f, 0.11f, 0.22f);
        b.Ell(Body, bc, br, yellow);
        foreach (var d in new[] { V(1f, 0.2f, 0.4f), V(1f, -0.1f, 0.05f), V(0.6f, 0.75f, 0.3f), V(0.8f, 0.4f, -0.2f) })
            PokeBuilder.Both(s => b.PaintEll(Body, Out(bc, br, default, V(d.X * s, d.Y, d.Z)), V(0.035f, 0.03f, 0.04f), brown, soft: 0.008f));
        // The long neck, a brown mane down its back
        var neck = Smooth(3, bc + V(0, 0.05f, 0.14f), bc + V(0, 0.2f, 0.19f), bc + V(0, 0.36f, 0.21f));
        b.Tube(Body, neck, 0.055f, 0.038f, yellow, blend: 0f);
        for (int i = 1; i < neck.Length; i++)
            b.PaintEll(Body, neck[i] + V(0, 0, -0.04f), V(0.03f, 0.035f, 0.03f), brown, soft: 0.008f);
        int head = b.Head(bc + V(0, 0.36f, 0.21f));
        var c = bc + V(0, 0.42f, 0.24f);
        var r = V(0.064f, 0.06f, 0.09f);
        b.Ell(head, c, r, yellow);
        b.Ell(head, c + V(0, -0.025f, 0.08f), V(0.045f, 0.04f, 0.05f), pink, blend: 0.02f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.045f * s, 0.02f, -0.03f));
            b.Spike(ear, c + V(0.045f * s, 0.02f, -0.03f), c + V(0.12f * s, 0.04f, -0.05f), 0.022f, yellow, 0.5f);
            // The tall horns, a dark knob on each
            b.Tube(head, Smooth(3, c + V(0.022f * s, 0.04f, -0.02f), c + V(0.04f * s, 0.12f, -0.04f), c + V(0.03f * s, 0.2f, -0.02f)), 0.014f, 0.011f, cream, blend: 0f);
            b.Ell(head, c + V(0.03f * s, 0.21f, -0.02f), V(0.024f, 0.024f, 0.024f), Rgb(60, 52, 64), mat: Shell, blend: 0.006f);
            var at = On(c, r, c.X + 0.046f * s, c.Y + 0.03f);
            b.Eye(head, at, Outward(c, r, at), 0.017f, Rgb(40, 34, 30));
        });
        // The tail grown into a great head over its rump, looking back
        int tail = b.Tail(bc + V(0, 0.04f, -0.2f));
        var tc = bc + V(0, 0.08f, -0.25f);
        var tr = V(0.15f, 0.15f, 0.14f);
        b.Ell(tail, tc, tr, helmet, mat: Shell, blend: 0.03f);
        PokeBuilder.Both(s => b.Spike(tail, tc + V(0.06f * s, 0.12f, 0.02f), tc + V(0.08f * s, 0.2f, 0.0f), 0.022f, helmet, mat: Shell));
        b.PaintEll(tail, tc + V(0, -0.02f, -0.12f), V(0.16f, 0.025f, 0.08f), Rgb(34, 28, 38), soft: 0.008f);
        var teeth = Out(tc, tr, default, V(0, -0.14f, -1f));
        b.Mark(tail, teeth, Outward(tc, tr, teeth), 0.06f, 0.012f, White, MarkShape.Zigzag);
        PokeBuilder.Both(s =>
        {
            var at = Out(tc, tr, default, V(0.45f * s, 0.35f, -0.8f));
            b.Mark(tail, at, Outward(tc, tr, at), 0.022f, 0.022f, Rgb(250, 214, 60), MarkShape.Ring);
        });
        return b;
    }

    // ------------------------------------------------------------------ Dudunsparce

    /// <summary>
    /// Dudunsparce, or its Three-Segment Form (<paramref name="three"/>): a long yellow serpent of Dunsparce grown,
    /// lying along the ground in two or three round segments that weave a little from side to side, each banded blue
    /// over its back, a big drill of a tail ringed blue raised behind, and a big round head with a pale face, a blue
    /// ring round its mouth, a blue band behind it, narrow blue eyes and two pairs of little white wings.
    /// </summary>
    private static PokeBuilder DudunsparceBuild(bool three)
    {
        var yellow = Rgb(242, 224, 110);
        var blue = Rgb(70, 150, 196);
        var pale = Rgb(250, 242, 206);
        int n = three ? 3 : 2;
        float[] weave = { 0f, 0.05f, -0.03f };
        var centers = new Vector3[n];
        for (int i = 0; i < n; i++) centers[i] = V(weave[i], 0.1f, 0.04f - 0.21f * i);
        var b = new PokeBuilder(three ? "Dudunsparce-Three-Segment" : "Dudunsparce", three ? 0.95f : 0.9f, BodyPlan.Serpent, centers[0]) { Coat = Scales };
        var sr = V(0.105f, 0.09f, 0.115f);
        var bones = new int[n];
        bones[0] = Body;
        for (int i = 0; i < n; i++)
        {
            if (i > 0)
            {
                bones[i] = b.Part("seg" + i, bones[i - 1], Vector3.Lerp(centers[i - 1], centers[i], 0.5f), PokeRole.Segment, i);
                // The narrower waist joining it to the segment before
                b.Limb(bones[i], centers[i - 1] + V(0, 0, -0.06f), centers[i], 0.075f, 0.08f, yellow);
            }
            b.Ell(bones[i], centers[i], sr, yellow);
            foreach (float dz in new[] { 0.05f, -0.01f, -0.07f })
                b.PaintEll(bones[i], centers[i] + V(0, 0.085f, dz), V(0.13f, 0.06f, 0.016f), blue, soft: 0.008f);
        }
        // The drill of a tail, raised behind
        var last = centers[n - 1];
        int tail = b.Tail(last + V(0, 0, -0.1f), bones[n - 1]);
        var path = Smooth(3, last + V(0, 0.01f, -0.08f), last + V(0, 0.03f, -0.17f), last + V(0, 0.1f, -0.23f));
        b.Tube(tail, path, 0.07f, 0.05f, yellow, blend: 0f);
        var tip = last + V(0, 0.3f, -0.31f);
        b.Spike(tail, path[^1], tip, 0.06f, yellow, mat: Shell);
        var axis = tip - path[^1];
        foreach (float f in new[] { 0.2f, 0.45f, 0.7f })
            b.PaintTorus(tail, path[^1] + axis * f, 0.06f * (1f - f), 0.01f, blue, Euler(axis));
        // The big round head on a short neck, a blue band behind it
        int head = b.Head(centers[0] + V(0, 0.04f, 0.1f));
        var c = centers[0] + V(0, 0.12f, 0.2f);
        var r = V(0.12f, 0.11f, 0.105f);
        b.Limb(head, centers[0] + V(0, 0.02f, 0.05f), c + V(0, -0.03f, -0.04f), 0.085f, 0.09f, yellow);
        b.Ell(head, c, r, yellow);
        b.PaintEll(head, c + V(0, 0.03f, -0.06f), V(0.14f, 0.13f, 0.018f), blue, soft: 0.008f);
        b.PaintEll(head, c + V(0, -0.015f, 0.09f), V(0.075f, 0.07f, 0.035f), pale);
        var face = Out(c, r, default, V(0, -0.15f, 1f));
        b.Mark(head, face, Outward(c, r, face), 0.05f, 0.05f, blue, MarkShape.Ring);
        b.Mark(head, face, Outward(c, r, face), 0.03f, 0.006f, Rgb(80, 70, 60), MarkShape.Bar);
        PokeBuilder.Both(s =>
        {
            var at = Out(c, r, default, V(0.62f * s, 0.35f, 0.7f));
            b.Eye(head, at, Outward(c, r, at), 0.02f, blue);
            // Two pairs of little wings behind its head
            int wing = b.Part(s < 0 ? "wingL" : "wingR", head, c + V(0.07f * s, 0.07f, -0.05f), PokeRole.Wing, 0f, s);
            Frond(b, wing, c + V(0.06f * s, 0.07f, -0.05f), c + V(0.2f * s, 0.17f, -0.12f), 0.05f, White, V(0, 1f, 0.3f), 0.25f);
            Frond(b, wing, c + V(0.05f * s, 0.04f, -0.08f), c + V(0.16f * s, 0.08f, -0.2f), 0.04f, White, V(0, 1f, 0.3f), 0.25f);
        });
        return b;
    }

    private static PokeBuilder Dudunsparce() => DudunsparceBuild(false);

    private static PokeBuilder DudunsparceThreeSegment() => DudunsparceBuild(true);

    // ------------------------------------------------------------------ Kingambit

    /// <summary>Kingambit: the king of the Pawniard line, a broad black body armoured red, gold blades across its chest, a ring of steel blades round its waist and a long mantle of black blades edged red hanging behind to the ground; great red pauldrons each with a steel blade, steel sickles on its forearms, red-kneed black legs on red feet tipped with blades, and a red helmet over a black face with yellow eyes, a beard of two steel blades below it and a tall crown of gold blades above.</summary>
    private static PokeBuilder Kingambit()
    {
        var b = new PokeBuilder("Kingambit", 1f, BodyPlan.Biped, V(0, 0.5f, 0)) { Coat = Metal };
        var red = Rgb(190, 44, 52);
        var black = Rgb(44, 42, 52);
        var steel = Rgb(214, 216, 226);
        var gold = Rgb(236, 192, 66);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.36f, 0));
            var knee = V(0.09f * s, 0.2f, 0.03f);
            var ankle = V(0.09f * s, 0.06f, 0.01f);
            b.Limb(leg, V(0.07f * s, 0.37f, 0), knee, 0.05f, 0.036f, black);
            b.Ell(leg, knee, V(0.042f, 0.04f, 0.042f), red, blend: 0.012f);
            b.Limb(leg, knee, ankle, 0.034f, 0.028f, black);
            b.Ell(leg, ankle + V(0, -0.025f, 0.03f), V(0.042f, 0.035f, 0.065f), red);
            Blade(b, leg, ankle + V(0, -0.035f, 0.08f), ankle + V(0, -0.045f, 0.16f), 0.026f, steel, V(0, 1f, 0), 0.3f, Metal);
        });
        // The waist, the broad chest and the red breastplate with gold blades across it
        b.Ell(Body, V(0, 0.44f, 0), V(0.1f, 0.09f, 0.08f), black);
        b.Ell(Body, V(0, 0.58f, 0), V(0.135f, 0.13f, 0.1f), black, blend: 0.04f);
        b.Ell(Body, V(0, 0.61f, 0.03f), V(0.125f, 0.08f, 0.085f), red, blend: 0.02f);
        PokeBuilder.Both(s => Blade(b, Body, V(0.015f * s, 0.6f, 0.11f), V(0.1f * s, 0.55f, 0.1f), 0.024f, gold, V(0, 0, 1f), 0.3f, Metal));
        // The ring of steel blades round its waist
        for (int i = 0; i < 10; i++)
        {
            float a = (i + 0.5f) * 36f * Degree;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            Blade(b, Body, V(0, 0.41f, 0) + d * 0.06f, V(0, 0.38f, 0) + d * 0.16f, 0.03f, steel, V(0, 1f, 0), 0.3f, Metal);
        }
        // The mantle of long black blades hanging behind it to the ground, edged red at their ends
        int cape = b.Tail(V(0, 0.42f, -0.06f));
        for (int i = 0; i < 5; i++)
        {
            float x = (i - 2) / 2f;
            var root = V(0.07f * x, 0.44f, -0.05f);
            var tip = V(0.13f * x, 0.07f, -0.17f + 0.02f * MathF.Abs(x));
            Blade(b, cape, root, tip, 0.05f, black, V(0.2f * x, 0.25f, -1f), 0.22f, Metal);
            b.PaintEll(cape, Vector3.Lerp(root, tip, 0.85f), V(0.05f, 0.06f, 0.05f), red, soft: 0.01f);
        }
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.15f * s, 0.65f, 0);
            int arm = b.Arm(s, shoulder);
            var elbow = V(0.22f * s, 0.52f, 0.04f);
            var hand = V(0.22f * s, 0.4f, 0.1f);
            // The great red pauldron, a steel blade standing out of it
            b.Ell(arm, shoulder + V(0.01f * s, 0.01f, 0), V(0.075f, 0.06f, 0.075f), red);
            Blade(b, arm, shoulder + V(0.03f * s, 0.04f, 0), shoulder + V(0.13f * s, 0.12f, -0.02f), 0.035f, steel, V(0, 0, 1f), 0.28f, Metal);
            b.Limb(arm, shoulder, elbow, 0.03f, 0.026f, black);
            b.Limb(arm, elbow, hand, 0.034f, 0.03f, red);
            b.Ell(arm, hand + V(0, -0.02f, 0.01f), V(0.03f, 0.032f, 0.03f), black);
            Sickle(b, arm, elbow + V(0.015f * s, 0, -0.01f), elbow + V(0.08f * s, 0.08f, -0.03f), elbow + V(0.1f * s, 0.22f, -0.07f), 0.04f, steel, V(s, 0, 0.3f));
        });
        int head = b.Head(V(0, 0.71f, 0.01f));
        var c = V(0, 0.8f, 0.02f);
        var r = V(0.075f, 0.072f, 0.072f);
        b.Limb(head, V(0, 0.68f, 0.005f), c, 0.035f, 0.035f, black);
        b.Ell(head, c, r, red);
        b.PaintEll(head, c + V(0, -0.015f, 0.055f), V(0.055f, 0.048f, 0.035f), black);
        // The beard of two steel blades under its face
        PokeBuilder.Both(s => Blade(b, head, c + V(0.045f * s, -0.055f, 0.02f), c + V(0.035f * s, -0.15f, 0.06f), 0.022f, steel, V(0, 0, 1f), 0.28f, Metal));
        // The crown: a gold band, a tall blade swept back in the middle and a curved one rising at each side
        b.Ell(head, c + V(0, 0.06f, 0f), V(0.05f, 0.025f, 0.05f), gold, mat: Metal, blend: 0.012f);
        Blade(b, head, c + V(0, 0.05f, -0.01f), c + V(0, 0.3f, -0.08f), 0.06f, gold, V(0, 0, 1f), 0.24f, Metal);
        PokeBuilder.Both(s =>
        {
            Sickle(b, head, c + V(0.035f * s, 0.045f, -0.005f), c + V(0.12f * s, 0.12f, -0.02f), c + V(0.15f * s, 0.25f, -0.06f), 0.042f, gold, V(0, 0, 1f));
            b.PaintEll(head, c + V(0.062f * s, 0f, 0.02f), V(0.022f, 0.04f, 0.04f), steel, soft: 0.006f);
            var at = On(c, r, c.X + 0.027f * s, c.Y - 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(250, 196, 50), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Great Tusk

    /// <summary>
    /// Great Tusk: Donphan's ancient past, a great grey elephant of a beast on thick legs ringed with tufts of
    /// pinkish-red fur, dark purple plates over its back, its tail and its long trunk, three rows of pinkish-red
    /// spikes along its back, red spots on its flanks, slanted yellow eyes ringed red, a jagged dark mouth, two huge
    /// ivory tusks sweeping forward and curling in, and long thin ears hanging at its cheeks, serrated and red inside.
    /// </summary>
    private static PokeBuilder GreatTusk()
    {
        var b = new PokeBuilder("Great Tusk", 0.95f, BodyPlan.Quadruped, V(0, 0.32f, -0.02f)) { Coat = Shell };
        var gray = Rgb(150, 150, 162);
        var plate = Rgb(82, 64, 104);
        var groove = Rgb(52, 40, 68);
        var red = Rgb(226, 86, 116);
        var mouth = Rgb(48, 36, 52);
        foreach (var (z, front) in new[] { (0.14f, true), (-0.17f, false) })
            PokeBuilder.Both(s =>
            {
                var hip = V(0.13f * s, 0.24f, z);
                int leg = b.Leg(s, hip, front);
                b.Limb(leg, hip, V(0.14f * s, 0.06f, z + 0.01f), 0.07f, 0.06f, gray);
                b.Ell(leg, V(0.14f * s, 0.04f, z + 0.025f), V(0.065f, 0.04f, 0.075f), gray);
                foreach (float dx in new[] { -0.03f, 0f, 0.03f })
                    b.Ell(leg, V(0.14f * s + dx, 0.03f, z + 0.085f), V(0.016f, 0.014f, 0.014f), Claw, mat: Shell, blend: 0.006f);
                // A tuft of pinkish-red fur round each knee
                for (int i = 0; i < 6; i++)
                {
                    float a = i * MathF.Tau / 6f + 0.3f;
                    var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
                    var root = V(0.138f * s, 0.14f, z + 0.005f) + dir * 0.05f;
                    b.Spike(leg, root, root + dir * 0.04f + V(0, -0.045f, 0), 0.024f, red, 0.6f, Fur, 0.01f);
                }
            });
        var bc = V(0, 0.32f, -0.02f);
        var br = V(0.2f, 0.17f, 0.26f);
        b.Ell(Body, bc, br, gray);
        // Red spots on its flanks, under the edge of its armour
        PokeBuilder.Both(s =>
        {
            foreach (var (y, z) in new[] { (0.25f, -0.06f), (0.22f, 0.0f), (0.29f, 0.05f) })
            {
                var at = Out(bc, br, default, V(s, (y - bc.Y) / br.Y, (z - bc.Z) / br.Z * 0.6f));
                b.Mark(Body, at, Outward(bc, br, at), 0.016f, 0.016f, red);
            }
        });
        // The dark purple armour over its back, grooved across like Donphan's, three rows of spikes along it
        var ac = bc + V(0, 0.07f, 0);
        var ar = V(0.215f, 0.14f, 0.285f);
        b.Ell(Body, ac, ar, plate, blend: 0.015f);
        for (int i = -3; i <= 3; i++)
        {
            float z = ac.Z + i * 0.075f;
            float k = MathF.Sqrt(MathF.Max(0f, 1f - MathF.Pow((z - ac.Z) / ar.Z, 2f)));
            b.PaintTorus(Body, V(0, ac.Y, z), ar.X * k, 0.007f, groove, V(90f, 0, 0), 1f, ar.Y / ar.X);
        }
        foreach (float x in new[] { -0.1f, 0f, 0.1f })
            for (int i = 0; i < 5; i++)
            {
                float z = ac.Z - 0.2f + 0.095f * i;
                var root = Out(ac, ar, default, V(x / ar.X, 1f, (z - ac.Z) / ar.Z));
                var n = Outward(ac, ar, root);
                float length = x == 0f ? 0.075f : 0.055f;
                b.Spike(Body, root - n * 0.01f, root + n * length + V(0, 0, -0.02f), 0.024f, red, 0.6f, Shell, 0.008f);
            }
        // A short tail, plated
        int tail = b.Tail(bc + V(0, 0.04f, -0.27f));
        b.Limb(tail, bc + V(0, 0.04f, -0.26f), bc + V(0, -0.03f, -0.34f), 0.024f, 0.017f, plate);
        Blade(b, tail, bc + V(0, -0.02f, -0.33f), bc + V(0, -0.07f, -0.42f), 0.04f, plate, V(1f, 0, 0), 0.3f);
        // The head under the front of its armour
        int head = b.Head(V(0, 0.32f, 0.22f));
        var c = V(0, 0.3f, 0.3f);
        var r = V(0.12f, 0.11f, 0.1f);
        b.Ell(head, c, r, gray);
        b.PaintEll(head, c + V(0, 0.075f, -0.01f), V(0.13f, 0.06f, 0.11f), plate);
        // The long trunk, plated and ringed
        var trunk = Smooth(3, V(0, 0.23f, 0.385f), V(0, 0.18f, 0.43f), V(0, 0.11f, 0.44f), V(0, 0.06f, 0.41f));
        b.Tube(head, trunk, 0.05f, 0.036f, plate, blend: 0f);
        for (int i = 2; i < trunk.Length - 1; i += 2)
            b.PaintTorus(head, trunk[i], 0.045f, 0.006f, groove, Euler(trunk[i + 1] - trunk[i - 1]));
        // The jagged mouth at each side of the trunk, and the great tusks sweeping forward and curling in
        PokeBuilder.Both(s =>
        {
            b.PaintEll(head, V(0.055f * s, 0.215f, 0.35f), V(0.04f, 0.012f, 0.035f), mouth, V(0, 0, 15f * s), 0.004f);
            var tusk = Smooth(3, V(0.06f * s, 0.24f, 0.36f), V(0.15f * s, 0.14f, 0.44f), V(0.16f * s, 0.2f, 0.56f), V(0.08f * s, 0.3f, 0.6f));
            b.Tube(head, tusk, 0.036f, 0.012f, Claw, Shell, 0f);
        });
        // The long thin ears hanging at its cheeks, serrated along their lower edge and red inside
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.09f * s, 0.34f, 0.24f));
            var ec = V(0.1f * s, 0.27f, 0.23f);
            var turn = V(-10f, 0, 12f * s);
            b.Ell(ear, ec, V(0.025f, 0.12f, 0.08f), plate, turn, blend: 0.02f);
            b.PaintEll(ear, ec + V(-0.02f * s, 0, 0), V(0.012f, 0.1f, 0.065f), red, turn, 0.006f);
            var q = Quaternion.CreateFromYawPitchRoll(turn.Y * Degree, turn.X * Degree, turn.Z * Degree);
            foreach (float deg in new[] { -150f, -115f, -80f, -45f })
            {
                float a = deg * Degree;
                var root = ec + Vector3.Transform(V(0, 0.12f * 0.8f * MathF.Sin(a), 0.08f * 0.8f * MathF.Cos(a)), q);
                var tip = ec + Vector3.Transform(V(0.01f * s, 0.12f * 1.3f * MathF.Sin(a), 0.08f * 1.3f * MathF.Cos(a)), q);
                Blade(b, ear, root, tip, 0.018f, plate, V(1f, 0, 0), 0.35f);
            }
        });
        // Slanted yellow eyes, ringed pinkish red
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.075f * s, c.Y + 0.035f);
            b.PaintEll(head, at, V(0.034f, 0.026f, 0.03f), red, V(0, 0, -20f * s), 0.006f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(250, 214, 60), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Scream Tail

    /// <summary>
    /// Scream Tail: Jigglypuff's ancient past, a round pink balloon of a body on stubby feet with short arms, pointed
    /// ears black inside and little tufts of fur under them, big yellow eyes lined darker pink like makeup, a small
    /// open mouth with two tiny fangs, a very long tuft of fur that curls up from its brow and runs down its back to
    /// the ground, darker pink at its end, and a short real tail of its own.
    /// </summary>
    private static PokeBuilder ScreamTail()
    {
        var b = new PokeBuilder("Scream Tail", 0.6f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = Fur };
        var pink = Rgb(246, 172, 198);
        var deep = Rgb(214, 98, 148);
        var black = Rgb(48, 40, 52);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.075f * s, 0.06f, 0.02f));
            b.Ell(leg, V(0.08f * s, 0.03f, 0.055f), V(0.048f, 0.03f, 0.068f), pink);
            int arm = b.Arm(s, V(0.15f * s, 0.2f, 0.04f));
            b.Limb(arm, V(0.14f * s, 0.2f, 0.04f), V(0.21f * s, 0.15f, 0.07f), 0.034f, 0.028f, pink);
            b.Ell(arm, V(0.215f * s, 0.14f, 0.075f), V(0.034f, 0.03f, 0.032f), pink);
        });
        var c = V(0, 0.2f, 0);
        var r = V(0.17f, 0.16f, 0.16f);
        b.Ell(Body, c, r, pink);
        // Pointed ears black inside, and little tufts of fur under them
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(Body, s, V(0.09f * s, 0.32f, 0.0f));
            b.Spike(ear, V(0.09f * s, 0.3f, 0.0f), V(0.15f * s, 0.43f, -0.01f), 0.06f, pink, 0.5f);
            b.PaintEll(ear, V(0.12f * s, 0.37f, 0.02f), V(0.03f, 0.05f, 0.03f), black, V(0, 0, -25f * s));
            b.Spike(Body, V(0.12f * s, 0.3f, -0.02f), V(0.2f * s, 0.29f, -0.03f), 0.026f, pink, 0.6f, blend: 0.012f);
            b.Spike(Body, V(0.14f * s, 0.25f, -0.01f), V(0.22f * s, 0.22f, 0f), 0.022f, pink, 0.6f, blend: 0.012f);
        });
        // The long tuft of fur: curling up from its brow, then down its back to the ground, darker at its end
        int mane = b.Part("mane", Body, V(0, 0.35f, 0.02f), PokeRole.Tail, 0.6f);
        var lock_ = Smooth(3, V(0, 0.33f, 0.08f), V(0, 0.42f, 0.1f), V(0, 0.48f, 0.03f), V(0, 0.46f, -0.08f), V(0, 0.38f, -0.2f),
            V(0, 0.24f, -0.28f), V(0, 0.13f, -0.34f), V(0, 0.07f, -0.39f), V(0, 0.1f, -0.44f));
        b.Tube(mane, lock_, 0.045f, 0.03f, pink, blend: 0f);
        b.PaintEll(mane, lock_[^2], V(0.07f, 0.09f, 0.09f), deep, soft: 0.01f);
        // The short real tail, curling out to one side
        int tail = b.Tail(V(0, 0.1f, -0.14f));
        b.Tube(tail, Smooth(3, V(0, 0.1f, -0.14f), V(0.07f, 0.08f, -0.21f), V(0.12f, 0.12f, -0.22f)), 0.03f, 0.02f, pink, blend: 0f);
        b.Ell(tail, V(0.13f, 0.13f, -0.22f), V(0.03f, 0.03f, 0.03f), pink, blend: 0.01f);
        // Big yellow eyes lined with makeup, and the small mouth with its fangs
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.065f * s, 0.23f);
            b.PaintEll(Body, at + V(0.012f * s, -0.035f, 0), V(0.035f, 0.03f, 0.04f), deep, V(0, 0, 25f * s), 0.008f);
            b.Eye(Body, at, Outward(c, r, at), 0.042f, Rgb(250, 212, 70));
        });
        var mouth = On(c, r, 0, 0.14f);
        b.Mark(Body, mouth, Outward(c, r, mouth), 0.03f, 0.012f, Rgb(150, 50, 80), MarkShape.Smile);
        PokeBuilder.Both(s =>
        {
            var fang = On(c, r, 0.017f * s, 0.128f);
            b.Mark(Body, fang, Outward(c, r, fang), 0.008f, 0.011f, White, MarkShape.Triangle, 180f);
        });
        return b;
    }

    // ------------------------------------------------------------------ Brute Bonnet

    /// <summary>
    /// Brute Bonnet: Amoonguss's ancient past, a squat dark mushroom on four stumpy brown legs with a stubby tail, a
    /// huge cap like a Poké Ball on its head, red over white with a dark band, spiked on top and mossy green
    /// underneath, wide jagged pink lips, small glaring yellow eyes in the cap's shade, and two long arms holding up
    /// caps like Poké Balls at its sides.
    /// </summary>
    private static PokeBuilder BruteBonnet()
    {
        var b = new PokeBuilder("Brute Bonnet", 0.75f, BodyPlan.Quadruped, V(0, 0.22f, 0)) { Coat = Fur };
        var body = Rgb(92, 88, 98);
        var legs = Rgb(136, 102, 74);
        var red = Rgb(214, 62, 72);
        var white = Rgb(238, 234, 228);
        var band = Rgb(58, 52, 56);
        var moss = Rgb(108, 150, 82);
        var lips = Rgb(240, 130, 164);
        StubbyLegs(b, 0.1f, 0.12f, 0.07f, -0.08f, 0.048f, legs);
        var bc = V(0, 0.22f, 0);
        var br = V(0.17f, 0.15f, 0.16f);
        b.Ell(Body, bc, br, body);
        // The wide jagged lips and the dark of the mouth behind them
        var mouth = On(bc, br, 0, 0.19f);
        b.PaintEll(Body, mouth, V(0.06f, 0.016f, 0.03f), Rgb(60, 30, 44), soft: 0.006f);
        b.Mark(Body, mouth, Outward(bc, br, mouth), 0.045f, 0.012f, lips, MarkShape.Zigzag);
        PokeBuilder.Both(s =>
        {
            var at = On(bc, br, 0.06f * s, 0.28f);
            b.Eye(Body, at, Outward(bc, br, at), 0.016f, Rgb(244, 210, 60), glare: true);
        });
        int tail = b.Tail(V(0, 0.2f, -0.15f));
        b.Limb(tail, V(0, 0.2f, -0.14f), V(0, 0.14f, -0.26f), 0.032f, 0.016f, body);
        // Long arms holding up a cap like a Poké Ball at each side
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.14f * s, 0.26f, 0.02f));
            var ball = V(0.32f * s, 0.3f, 0.07f);
            var cap = V(0.085f, 0.075f, 0.08f);
            b.Tube(arm, Smooth(3, V(0.13f * s, 0.26f, 0.02f), V(0.21f * s, 0.29f, 0.05f), ball - V(0.06f * s, 0.01f, 0)), 0.026f, 0.022f, body, blend: 0f);
            b.Ell(arm, ball, cap, white);
            b.PaintEll(arm, ball + V(0, 0.05f, 0), V(0.1f, 0.055f, 0.1f), red);
            b.PaintTorus(arm, ball, 0.086f, 0.007f, band, default, 1f, 0.94f);
            var button = Out(ball, cap, default, V(0.6f * s, 0, 0.8f));
            b.Mark(arm, button, Outward(ball, cap, button), 0.016f, 0.016f, band, MarkShape.Ring);
        });
        // The huge cap: red over white with a dark band, spikes on top and moss under its rim
        int head = b.Head(V(0, 0.36f, 0));
        var c = V(0, 0.42f, 0);
        var r = V(0.22f, 0.09f, 0.21f);
        b.Ell(head, c, r, white);
        b.PaintEll(head, c + V(0, 0.06f, 0), V(0.25f, 0.065f, 0.24f), red);
        b.PaintTorus(head, c + V(0, 0.005f, 0), 0.216f, 0.009f, band, default, 1f, 0.95f);
        b.PaintEll(head, c + V(0, -0.075f, 0), V(0.21f, 0.03f, 0.2f), moss, soft: 0.01f);
        for (int i = 0; i < 7; i++)
        {
            float a = i * MathF.Tau / 7f + 0.2f;
            var root = Out(c, r, default, V(MathF.Sin(a) * 0.55f, 1f, MathF.Cos(a) * 0.55f));
            var n = Outward(c, r, root);
            b.Spike(head, root - n * 0.01f, root + n * 0.06f + V(0, 0.02f, 0), 0.026f, red, mat: Shell, blend: 0.008f);
        }
        b.Spike(head, c + V(0, 0.08f, 0), c + V(0, 0.16f, -0.01f), 0.03f, red, mat: Shell, blend: 0.008f);
        return b;
    }

    // ------------------------------------------------------------------ Flutter Mane

    /// <summary>
    /// Flutter Mane: Misdreavus's ancient past, a dark bluish-green ghost of a head floating over a ruffled dress of
    /// hanging locks, its hair swept out at each side like arms fringed with magenta-tipped feathers, red spikes on
    /// its crown, a string of round red gems at its neck, and big yellow eyes set in red.
    /// </summary>
    private static PokeBuilder FlutterMane()
    {
        var b = new PokeBuilder("Flutter Mane", 0.72f, BodyPlan.Floating, V(0, 0.46f, 0)) { Coat = Fur }.Hover();
        var teal = Rgb(46, 94, 102);
        var magenta = Rgb(204, 84, 156);
        var red = Rgb(214, 44, 64);
        var c = V(0, 0.5f, 0);
        var r = V(0.12f, 0.12f, 0.11f);
        b.Ell(Body, c, r, teal);
        // The ruffled dress of locks below it
        var dress = V(0, 0.37f, 0);
        b.Ell(Body, dress, V(0.09f, 0.075f, 0.085f), teal);
        for (int i = 0; i < 7; i++)
        {
            float a = i * MathF.Tau / 7f;
            var at = dress + V(MathF.Sin(a) * 0.07f, -0.03f, MathF.Cos(a) * 0.065f);
            Lock(b, Body, at, at + V(MathF.Sin(a) * 0.06f, -0.12f, MathF.Cos(a) * 0.055f), 0.042f, teal, magenta);
        }
        // The red gems round its neck
        for (int i = 0; i < 7; i++)
        {
            float a = (i - 3) * 0.36f;
            b.Ell(Body, V(MathF.Sin(a) * 0.095f, 0.41f - MathF.Abs(MathF.Sin(a)) * 0.012f, MathF.Cos(a) * 0.09f), V(0.024f, 0.024f, 0.024f), red, mat: Shell, blend: 0.006f);
        }
        // Red spikes on its crown
        b.Spike(Body, V(0, 0.6f, -0.02f), V(0, 0.73f, -0.07f), 0.026f, red, 0.7f, Shell);
        PokeBuilder.Both(s => b.Spike(Body, V(0.05f * s, 0.59f, -0.02f), V(0.1f * s, 0.69f, -0.05f), 0.022f, red, 0.7f, Shell));
        // Its hair swept out like two arms, fringed with feathers tipped magenta
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.09f * s, 0.55f, -0.04f));
            var path = Smooth(3, V(0.08f * s, 0.55f, -0.04f), V(0.2f * s, 0.64f, -0.06f), V(0.33f * s, 0.62f, -0.06f), V(0.42f * s, 0.5f, -0.03f), V(0.44f * s, 0.4f, 0f));
            b.Tube(wing, path, 0.045f, 0.022f, teal, blend: 0f);
            for (int i = 3; i < path.Length; i += 3)
            {
                var root = path[i];
                var tip = root + V(0.045f * s, -0.13f, 0.02f);
                Frond(b, wing, root, tip, 0.036f, teal, V(0, 0, 1f), 0.25f, Fur, 0.01f);
                b.PaintEll(wing, tip - (tip - root) * 0.12f, V(0.034f, 0.03f, 0.03f), magenta, soft: 0.008f);
            }
        });
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.045f * s, 0.51f);
            b.Eye(Body, at, Outward(c, r, at), 0.036f, white: red, pupil: Rgb(250, 214, 60));
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Slither Wing

    /// <summary>
    /// Slither Wing: Volcarona's ancient past, a great moth walking on four fuzzy legs, a navy thorax wrapped in
    /// messy smoky-white fuzz, a long teal abdomen spotted black trailing behind, six leaf-like orange wings tipped
    /// yellow-orange with little green diamonds lying back over it like a cape, red compound eyes, and a red spiny
    /// horn sweeping out from each side of its face.
    /// </summary>
    private static PokeBuilder SlitherWing()
    {
        var b = new PokeBuilder("Slither Wing", 1f, BodyPlan.Quadruped, V(0, 0.4f, 0)) { Coat = Fur };
        var fuzz = Rgb(228, 224, 218);
        var navy = Rgb(44, 54, 98);
        var teal = Rgb(62, 150, 150);
        var orange = Rgb(236, 116, 48);
        var amber = Rgb(250, 192, 72);
        var green = Rgb(96, 170, 88);
        var red = Rgb(214, 52, 52);
        var black = Rgb(36, 34, 40);
        // Four long fuzzy legs
        foreach (var (z, front) in new[] { (0.1f, true), (-0.08f, false) })
            PokeBuilder.Both(s =>
            {
                var hip = V(0.07f * s, 0.36f, z * 0.8f);
                int leg = b.Leg(s, hip, front);
                var knee = V(0.19f * s, 0.21f, z * 1.5f);
                var ankle = V(0.18f * s, 0.05f, z * 1.5f);
                b.Limb(leg, hip, knee, 0.045f, 0.034f, fuzz);
                b.Limb(leg, knee, ankle, 0.034f, 0.026f, fuzz);
                b.Ell(leg, ankle + V(0, -0.022f, 0.012f), V(0.03f, 0.024f, 0.04f), black, mat: Shell);
                foreach (float t in new[] { 0.3f, 0.65f })
                {
                    var at = Vector3.Lerp(hip, knee, t);
                    b.Spike(leg, at, at + V(0.04f * s, 0.015f, 0.02f), 0.024f, fuzz, 0.6f, blend: 0.012f);
                }
                b.Spike(leg, knee, knee + V(0.045f * s, -0.01f, 0), 0.022f, fuzz, 0.6f, blend: 0.012f);
            });
        // The navy thorax, its sides wrapped in messy fuzz
        var bc = V(0, 0.4f, 0);
        var br = V(0.12f, 0.11f, 0.13f);
        b.Ell(Body, bc, br, navy);
        float golden = MathF.PI * (3f - MathF.Sqrt(5f));
        for (int i = 0; i < 44; i++)
        {
            float y = 1f - 2f * (i + 0.5f) / 44f;
            float ring = MathF.Sqrt(1f - y * y), a = i * golden;
            var n = V(MathF.Sin(a) * ring, y, MathF.Cos(a) * ring);
            if (y < -0.5f || n.Z > 0.6f || (y > 0.2f && MathF.Abs(n.X) < 0.45f)) continue;
            var root = bc + n * br * 0.9f;
            var dir = Vector3.Normalize(n / br + V(0, -0.3f, 0));
            b.Spike(Body, root, root + dir * 0.06f, 0.026f, fuzz, 0.6f, blend: 0.015f);
        }
        // The long teal abdomen spotted black
        int tail = b.Tail(bc + V(0, -0.01f, -0.11f));
        var abdomen = Smooth(3, bc + V(0, -0.02f, -0.1f), bc + V(0, -0.04f, -0.25f), bc + V(0, -0.1f, -0.4f), bc + V(0, -0.18f, -0.5f));
        b.Tube(tail, abdomen, 0.1f, 0.035f, teal, blend: 0f);
        for (int i = 2; i < abdomen.Length - 1; i += 2)
        {
            float rr = 0.1f - 0.065f * i / (abdomen.Length - 1f);
            b.PaintEll(tail, abdomen[i] + V(0, rr * 0.95f, 0), V(0.016f, 0.016f, 0.016f), black, soft: 0.005f);
            PokeBuilder.Both(s => b.PaintEll(tail, abdomen[i] + V(rr * 0.95f * s, 0.01f, 0.01f), V(0.014f, 0.014f, 0.014f), black, soft: 0.005f));
        }
        // Six leaf-like wings lying back over it like a cape
        PokeBuilder.Both(s =>
        {
            var root = bc + V(0.04f * s, 0.09f, -0.02f);
            int wing = b.Wing(s, root);
            foreach (var (d, len) in new[] { (V(0.45f * s, 0.55f, -0.7f), 0.3f), (V(0.8f * s, 0.35f, -0.48f), 0.32f), (V(0.92f * s, 0.02f, -0.4f), 0.27f) })
            {
                var dir = Vector3.Normalize(d);
                var tip = root + dir * len;
                Frond(b, wing, root, tip, 0.075f, orange, V(0, 1f, 0.2f), 0.2f, Fur, 0.012f);
                b.PaintEll(wing, root + dir * len * 0.86f, V(0.06f, 0.06f, 0.06f), amber, soft: 0.01f);
                b.PaintEll(wing, root + dir * len * 0.55f, V(0.016f, 0.016f, 0.016f), green, soft: 0.005f);
            }
        });
        // The head: red compound eyes and a red spiny horn sweeping out from each side
        int head = b.Head(bc + V(0, 0.01f, 0.12f));
        var c = bc + V(0, 0.01f, 0.18f);
        var r = V(0.075f, 0.07f, 0.065f);
        b.Ell(head, c, r, navy);
        PokeBuilder.Both(s =>
        {
            var root = c + V(0.05f * s, 0.03f, -0.035f);
            var tip = c + V(0.2f * s, 0.11f, -0.06f);
            b.Spike(head, root, tip, 0.024f, red, mat: Shell);
            foreach (float t in new[] { 0.35f, 0.6f })
            {
                var at = Vector3.Lerp(root, tip, t);
                b.Spike(head, at, at + V(0.01f * s, 0.035f, 0.015f), 0.01f, red, mat: Shell, blend: 0.004f);
            }
            var eye = On(c, r, 0.032f * s, c.Y + 0.004f);
            b.Eye(head, eye, Outward(c, r, eye), 0.017f, white: Rgb(170, 30, 44), pupil: Rgb(244, 92, 84), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Sandy Shocks

    /// <summary>
    /// A horseshoe magnet of Sandy Shocks's, rough and dull as a thing of old: like <see cref="Magnet"/>, bent at
    /// <paramref name="bend"/> and opening toward <paramref name="open"/>, in <paramref name="body"/> with its two
    /// ends painted <paramref name="tip"/>.
    /// </summary>
    private static void SandyShocksMagnet(PokeBuilder b, int bone, Vector3 bend, Vector3 open, Vector3 across, float size, Color body, Color tip)
    {
        var u = Vector3.Normalize(open);
        var x = Vector3.Normalize(across - u * Vector3.Dot(across, u));
        float bendR = size * 0.5f, bar = size * 0.2f, prong = size * 0.8f;
        var center = bend + u * bendR;
        var a = center + x * bendR + u * prong;
        var z = center - x * bendR + u * prong;
        b.Tube(bone, new[] { a }.Concat(Arc(center, x, -u, bendR, 0f, MathF.PI, 8)).Append(z).ToArray(), bar, bar, body, Metal, 0f);
        b.PaintEll(bone, a, V(prong * 0.3f, prong * 0.3f, prong * 0.3f), tip, soft: 0.008f);
        b.PaintEll(bone, z, V(prong * 0.3f, prong * 0.3f, prong * 0.3f), tip, soft: 0.008f);
    }

    /// <summary>
    /// Sandy Shocks: Magneton's ancient past, three rusty units like ancient Magnemite, each with a yellow eye: the top
    /// one bristling with iron filings, striped with war paint and crowned with horseshoe magnets swept back like
    /// hair, the two below walking on a single long magnet each, bent like a leg and splayed into filings at its
    /// foot, and a string of screws hanging between them like a tail, frayed into filings at its end.
    /// </summary>
    private static PokeBuilder SandyShocks()
    {
        var b = new PokeBuilder("Sandy Shocks", 0.9f, BodyPlan.Biped, V(0, 0.4f, 0)) { Coat = Metal };
        var rust = Rgb(170, 112, 74);
        var filings = Rgb(78, 64, 60);
        var magnet = Rgb(120, 92, 74);
        var dullRed = Rgb(176, 72, 60);
        var dullBlue = Rgb(84, 104, 150);
        var warPaint = Rgb(244, 226, 170);
        var yellow = Rgb(250, 214, 60);
        var pupil = Rgb(36, 30, 30);
        // The two units below, joined in the middle
        b.Ell(Body, V(0, 0.37f, 0), V(0.08f, 0.06f, 0.06f), rust, blend: 0.02f);
        PokeBuilder.Both(s =>
        {
            var lc = V(0.11f * s, 0.37f, 0);
            var lr = V(0.1f, 0.1f, 0.1f);
            b.Ell(Body, lc, lr, rust);
            var at = On(lc, lr, lc.X + 0.01f * s, lc.Y + 0.01f);
            b.Eye(Body, at, Outward(lc, lr, at), 0.034f, white: yellow, pupil: pupil);
            // Its single long magnet, bent like a leg and splayed into filings at the foot
            int leg = b.Leg(s, V(0.11f * s, 0.3f, 0));
            var bar = Smooth(3, V(0.11f * s, 0.32f, 0), V(0.14f * s, 0.19f, 0.04f), V(0.13f * s, 0.07f, 0));
            b.Tube(leg, bar, 0.035f, 0.03f, magnet, blend: 0f);
            b.PaintEll(leg, bar[^1] + V(0, 0.02f, 0), V(0.045f, 0.04f, 0.045f), s < 0 ? dullRed : dullBlue, soft: 0.008f);
            for (int i = 0; i < 7; i++)
            {
                float a = i * MathF.Tau / 7f + 0.4f;
                var root = bar[^1] + V(0, 0.005f, 0);
                b.Spike(leg, root, root + V(MathF.Sin(a) * 0.055f, -0.068f, MathF.Cos(a) * 0.055f), 0.016f, filings, blend: 0.006f);
            }
        });
        // The string of screws hanging between them like a tail, frayed into filings at its end
        int tail = b.Tail(V(0, 0.37f, -0.03f));
        var string_ = Smooth(3, V(0, 0.37f, -0.02f), V(0, 0.3f, -0.12f), V(0, 0.22f, -0.17f), V(0, 0.15f, -0.19f));
        b.Tube(tail, string_, 0.018f, 0.015f, magnet, Metal);
        for (int i = 3; i < string_.Length; i += 3)
        {
            var d = string_[Math.Min(string_.Length - 1, i + 1)] - string_[i - 1];
            b.Ell(tail, string_[i], V(0.03f, 0.011f, 0.03f), rust, Euler(d), Metal, 0.004f);
        }
        for (int i = 0; i < 5; i++)
        {
            float a = i * MathF.Tau / 5f;
            var root = string_[^1];
            b.Spike(tail, root, root + V(MathF.Sin(a) * 0.04f, -0.05f, MathF.Cos(a) * 0.04f - 0.01f), 0.013f, filings, blend: 0.006f);
        }
        // The top unit: a big yellow eye, war paint, filings bristling and magnets swept back like hair
        int head = b.Head(V(0, 0.48f, 0));
        var tc = V(0, 0.56f, 0);
        var tr = V(0.13f, 0.13f, 0.13f);
        b.Ell(head, tc, tr, rust);
        PokeBuilder.Both(s =>
        {
            foreach (float y in new[] { 0.5f, 0.53f })
                b.PaintEll(head, On(tc, tr, 0.1f * s, y), V(0.03f, 0.008f, 0.03f), warPaint, V(0, 0, 20f * s), 0.005f);
        });
        float golden = MathF.PI * (3f - MathF.Sqrt(5f));
        for (int i = 0; i < 40; i++)
        {
            float y = 1f - 2f * (i + 0.5f) / 40f;
            float ring = MathF.Sqrt(1f - y * y), a = i * golden;
            var n = V(MathF.Sin(a) * ring, y, MathF.Cos(a) * ring);
            if (y < -0.3f || (n.Z > 0.35f && y < 0.75f)) continue;
            b.Spike(head, tc + n * tr.X * 0.9f, tc + n * (tr.X + 0.06f), 0.016f, filings, blend: 0.008f);
        }
        foreach (float x in new[] { -0.07f, -0.025f, 0.025f, 0.07f })
            SandyShocksMagnet(b, head, tc + V(x, 0.11f, 0.02f), V(x * 4f, 0.25f, -1f), V(0, 1f, 0.3f), 0.1f, magnet, x < 0 ? dullRed : dullBlue);
        b.Eye(head, On(tc, tr, 0, tc.Y), V(0, 0, 1f), 0.05f, white: yellow, pupil: pupil);
        return b;
    }

    // ------------------------------------------------------------------ Iron Treads

    /// <summary>
    /// Iron Treads: Donphan's future, a ball of grey metal with a black underbelly on four stubby legs plated grey, a
    /// segmented black tread running from low on its back over its top to its brow, a red stripe glowing along it, a
    /// black screen on its face showing two angry red eyes over a metal plate where its mouth is, short black tusks
    /// and long black plates hanging at its sides for ears.
    /// </summary>
    private static PokeBuilder IronTreads()
    {
        var b = new PokeBuilder("Iron Treads", 0.75f, BodyPlan.Quadruped, V(0, 0.24f, 0)) { Coat = Metal };
        var silver = Rgb(178, 184, 196);
        var black = Rgb(40, 40, 48);
        var red = Rgb(244, 50, 60);
        StubbyLegs(b, 0.11f, 0.12f, 0.09f, -0.1f, 0.055f, silver, black);
        var bc = V(0, 0.24f, 0);
        b.Ell(Body, bc, V(0.18f, 0.18f, 0.19f), silver);
        b.PaintEll(Body, bc + V(0, -0.13f, 0), V(0.17f, 0.08f, 0.18f), black, soft: 0.01f);
        // The face at its front, a black screen with two angry red eyes over the plate of its mouth
        int head = b.Head(bc + V(0, -0.02f, 0.12f));
        var c = V(0, 0.19f, 0.14f);
        var r = V(0.13f, 0.1f, 0.09f);
        b.Ell(head, c, r, silver);
        b.PaintEll(head, c + V(0, 0.025f, 0.08f), V(0.1f, 0.045f, 0.04f), black, soft: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.045f * s, c.Y + 0.025f);
            b.Eye(head, at, Outward(c, r, at), 0.018f, white: red, pupil: Rgb(110, 10, 20), glare: true);
            b.Spike(head, V(0.07f * s, 0.14f, 0.19f), V(0.11f * s, 0.11f, 0.27f), 0.022f, black, mat: Shell, blend: 0.006f);
        });
        // The tread over its back, a band of black segments from low behind to its brow, a red stripe along it
        const float tread = 0.195f;
        for (int i = 0; i < 13; i++)
        {
            float deg = -120f + 15f * i;
            float a = deg * Degree;
            var at = bc + V(0, tread * MathF.Cos(a), tread * MathF.Sin(a));
            b.Box(deg > 40f ? head : Body, at, V(0.065f, 0.022f, 0.03f), 0.008f, black, V(deg, 0, 0), Metal, 0.004f);
        }
        b.PaintTorus(Body, bc, tread + 0.022f, 0.01f, red, V(0, 0, 90f));
        b.PaintTorus(head, bc, tread + 0.022f, 0.01f, red, V(0, 0, 90f));
        // Long black plates hanging at its sides for ears
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.15f * s, 0.27f, 0.06f));
            b.Ell(ear, V(0.165f * s, 0.2f, 0.06f), V(0.02f, 0.1f, 0.06f), black, V(-8f, 0, 6f * s), blend: 0.02f);
        });
        return b;
    }

    // ------------------------------------------------------------------ Iron Bundle

    /// <summary>
    /// Iron Bundle: Delibird's future, a little red robot bird, its white-plated face crowned with two three-pointed
    /// white crests, round blue eyes ringed black, a pale yellow beak, a steel ring round its neck, flat red flippers
    /// for wings, a blue spot on its round belly, two-toed yellow feet and a white bag behind it on a black cable
    /// banded blue.
    /// </summary>
    private static PokeBuilder IronBundle()
    {
        var b = new PokeBuilder("Iron Bundle", 0.55f, BodyPlan.Biped, V(0, 0.17f, 0)) { Coat = Metal };
        var red = Rgb(216, 54, 56);
        var white = Rgb(244, 244, 248);
        var yellow = Rgb(250, 226, 130);
        var blue = Rgb(70, 150, 232);
        var black = Rgb(38, 38, 46);
        var steel = Rgb(184, 190, 204);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.055f * s, 0.08f, 0));
            b.Limb(leg, V(0.055f * s, 0.09f, 0), V(0.06f * s, 0.03f, 0.015f), 0.02f, 0.018f, yellow);
            foreach (float t in new[] { -1f, 1f })
                b.Spike(leg, V(0.06f * s, 0.022f, 0.02f), V(0.06f * s + 0.02f * t, 0.012f, 0.075f), 0.015f, yellow);
        });
        var bc = V(0, 0.17f, 0);
        var br = V(0.12f, 0.12f, 0.11f);
        b.Ell(Body, bc, br, red);
        var spot = On(bc, br, 0, 0.13f);
        b.Mark(Body, spot, Outward(bc, br, spot), 0.024f, 0.024f, blue);
        // Flat red flippers for wings, tipped white
        PokeBuilder.Both(s =>
        {
            var root = V(0.1f * s, 0.21f, 0.0f);
            int arm = b.Arm(s, root);
            var tip = V(0.21f * s, 0.11f, 0.04f);
            Frond(b, arm, root, tip, 0.045f, red, V(s, 0.3f, 0.4f), 0.3f, Metal, 0.015f);
            b.PaintEll(arm, Vector3.Lerp(root, tip, 0.9f), V(0.035f, 0.035f, 0.035f), white, soft: 0.006f);
        });
        // The bag behind it, on a black cable banded blue
        int tail = b.Tail(V(0, 0.15f, -0.1f));
        var cable = Smooth(3, V(0, 0.15f, -0.09f), V(0, 0.12f, -0.17f), V(0.06f, 0.12f, -0.24f), V(0.11f, 0.15f, -0.26f));
        b.Tube(tail, cable, 0.016f, 0.014f, black, blend: 0f);
        b.PaintEll(tail, cable[4], V(0.024f, 0.024f, 0.024f), blue, soft: 0.005f);
        b.Ell(tail, V(0.16f, 0.17f, -0.27f), V(0.075f, 0.08f, 0.075f), white, blend: 0.012f);
        // The steel ring round its neck, and the head over it
        b.Torus(Body, V(0, 0.285f, 0), 0.045f, 0.018f, steel, blend: 0.006f);
        int head = b.Head(V(0, 0.29f, 0));
        var c = V(0, 0.36f, 0.01f);
        var r = V(0.095f, 0.085f, 0.085f);
        b.Ell(head, c, r, red);
        b.PaintEll(head, c + V(0, -0.005f, 0.05f), V(0.085f, 0.075f, 0.05f), white);
        b.Spike(head, On(c, r, 0, c.Y - 0.035f) - V(0, 0, 0.012f), On(c, r, 0, c.Y - 0.035f) + V(0, -0.01f, 0.04f), 0.02f, yellow, 0.7f, Shell);
        PokeBuilder.Both(s =>
        {
            // A crest of three white points over each eye
            var root = c + V(0.04f * s, 0.065f, 0);
            for (int k = -1; k <= 1; k++)
            {
                var dir = Vector3.Normalize(V(s * (0.45f + 0.35f * k), 1f, -0.15f));
                Blade(b, head, root, root + dir * (k == 0 ? 0.08f : 0.06f), 0.018f, white, V(0, 0, 1f), 0.3f);
            }
            var at = On(c, r, 0.04f * s, c.Y + 0.012f);
            b.PaintEll(head, at, V(0.026f, 0.026f, 0.026f), black, soft: 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, blue);
        });
        return b;
    }

    // ------------------------------------------------------------------ Iron Hands

    /// <summary>
    /// One of Iron Hands's giant hands at <paramref name="at"/>, palm forward and fingers up: a rounded block of grey,
    /// its palm four bright yellow panels lined black, a yellow circle on its back and three thick fingers on top.
    /// </summary>
    private static void IronHandsHand(PokeBuilder b, int arm, Vector3 at, Color grey, Color yellow, Color black)
    {
        b.Box(arm, at, V(0.1f, 0.11f, 0.045f), 0.035f, grey, mat: Metal, blend: 0.01f);
        b.PaintEll(arm, at + V(0, 0, 0.05f), V(0.09f, 0.1f, 0.02f), yellow, soft: 0.008f);
        b.PaintEll(arm, at + V(0, 0, 0.05f), V(0.006f, 0.11f, 0.02f), black, soft: 0.004f);
        b.PaintEll(arm, at + V(0, 0, 0.05f), V(0.1f, 0.006f, 0.02f), black, soft: 0.004f);
        b.PaintEll(arm, at + V(0, 0, -0.05f), V(0.045f, 0.045f, 0.02f), yellow, soft: 0.006f);
        foreach (float x in new[] { -0.06f, 0f, 0.06f })
            b.Limb(arm, at + V(x, 0.08f, 0), at + V(x * 1.1f, 0.16f, 0.01f), 0.036f, 0.032f, grey, Metal, 0.008f);
    }

    /// <summary>
    /// Iron Hands: Hariyama's future, a huge cyborg sumo wrestler, a light grey armoured chest with yellow lights at
    /// its shoulders, a grey belt studded with yellow squares over a round grey belly lined black, dark blue
    /// half-sphere thighs over black shoes, a small grey head with a deep blue crest lined black, cylinders for ears
    /// and yellow eyes in a black faceplate, and two giant three-fingered hands, one raised and one thrust out.
    /// </summary>
    private static PokeBuilder IronHands()
    {
        var b = new PokeBuilder("Iron Hands", 1f, BodyPlan.Biped, V(0, 0.4f, 0)) { Coat = Metal };
        var grey = Rgb(198, 202, 210);
        var blue = Rgb(44, 62, 128);
        var yellow = Rgb(250, 214, 60);
        var black = Rgb(36, 36, 44);
        var belt = Rgb(150, 154, 164);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.12f * s, 0.24f, 0));
            b.Ell(leg, V(0.13f * s, 0.2f, 0), V(0.09f, 0.075f, 0.09f), blue);
            b.Limb(leg, V(0.14f * s, 0.15f, 0), V(0.15f * s, 0.07f, 0.02f), 0.045f, 0.042f, belt);
            b.Ell(leg, V(0.15f * s, 0.04f, 0.05f), V(0.065f, 0.04f, 0.1f), black, mat: Shell);
            b.Box(leg, V(0.15f * s, 0.068f, 0.11f), V(0.025f, 0.012f, 0.015f), 0.004f, yellow, V(-30f, 0, 0), Metal, 0.004f);
        });
        // The round belly lined black, the belt studded with yellow squares, the armoured chest
        var bc = V(0, 0.36f, 0);
        var br = V(0.22f, 0.18f, 0.18f);
        b.Ell(Body, bc, br, grey);
        foreach (float y in new[] { 0.26f, 0.22f })
        {
            float k = MathF.Sqrt(1f - MathF.Pow((y - bc.Y) / br.Y, 2f));
            b.PaintTorus(Body, V(0, y, 0), br.X * k, 0.006f, black, default, 1f, br.Z / br.X);
        }
        b.Torus(Body, V(0, 0.33f, 0), 0.21f, 0.026f, belt, default, 1f, 0.82f, Metal, 0.01f);
        PokeBuilder.Both(s =>
        {
            foreach (float deg in new[] { 45f, 70f, 95f })
            {
                float a = deg * Degree;
                var at = V(MathF.Sin(a) * 0.232f * s, 0.33f, MathF.Cos(a) * 0.19f);
                b.Box(Body, at, V(0.018f, 0.018f, 0.008f), 0.004f, yellow, V(0, deg * s, 0), Metal, 0.004f);
            }
        });
        b.Ell(Body, V(0, 0.5f, 0), V(0.2f, 0.13f, 0.15f), grey);
        PokeBuilder.Both(s => b.PaintEll(Body, V(0.12f * s, 0.56f, 0.11f), V(0.032f, 0.032f, 0.032f), yellow, soft: 0.006f));
        // Thick arms and the giant hands: the right raised, the left thrust out low
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.18f * s, 0.53f, 0);
            int arm = b.Arm(s, shoulder);
            var elbow = s > 0 ? V(0.3f * s, 0.58f, 0.04f) : V(0.3f * s, 0.42f, 0.06f);
            var hand = s > 0 ? V(0.37f * s, 0.74f, 0.08f) : V(0.36f * s, 0.36f, 0.17f);
            b.Limb(arm, shoulder, elbow, 0.065f, 0.058f, grey, Metal);
            b.Limb(arm, elbow, hand, 0.058f, 0.05f, grey, Metal);
            IronHandsHand(b, arm, hand, grey, yellow, black);
        });
        // The small head: a deep blue crest lined black, cylinders for ears, yellow eyes in a black faceplate
        int head = b.Head(V(0, 0.6f, 0.02f));
        var c = V(0, 0.67f, 0.05f);
        var r = V(0.085f, 0.075f, 0.075f);
        b.Ell(head, c, r, grey);
        var crest = c + V(0, 0.06f, -0.01f);
        b.Ell(head, crest, V(0.095f, 0.045f, 0.1f), blue, blend: 0.01f);
        foreach (float x in new[] { -0.045f, 0f, 0.045f })
            b.PaintEll(head, crest + V(x, 0.03f, 0), V(0.006f, 0.03f, 0.11f), black, soft: 0.004f);
        PokeBuilder.Both(s => b.Limb(head, c + V(0.065f * s, 0, 0), c + V(0.105f * s, 0, 0), 0.03f, 0.03f, belt, Metal, 0.008f));
        b.PaintEll(head, c + V(0, -0.002f, 0.06f), V(0.07f, 0.035f, 0.04f), black, soft: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, c.Y);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(250, 214, 60), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Iron Jugulis

    private static readonly Color JugulisNavy = Rgb(48, 62, 116);
    private static readonly Color JugulisArmor = Rgb(34, 34, 44);
    private static readonly Color JugulisFuchsia = Rgb(216, 54, 150);
    private static readonly Color JugulisLed = Rgb(112, 200, 252);
    private static readonly Color JugulisSpot = Rgb(248, 246, 250);

    /// <summary>A fuchsia collar of armour round a neck at <paramref name="at"/>, its axis along <paramref name="axis"/>, spotted white.</summary>
    private static void JugulisCollar(PokeBuilder b, int bone, Vector3 at, Vector3 axis, float ring, float tube)
    {
        var n = Vector3.Normalize(axis);
        b.Torus(bone, at, ring, tube, JugulisFuchsia, Euler(n), mat: Metal, blend: 0.006f);
        var u = Vector3.Normalize(Vector3.Cross(n, MathF.Abs(n.Y) > 0.9f ? V(1f, 0, 0) : V(0, 1f, 0)));
        var w = Vector3.Cross(n, u);
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f + 0.4f;
            var dir = u * MathF.Cos(a) + w * MathF.Sin(a);
            b.PaintEll(bone, at + dir * (ring + tube * 0.8f), V(tube * 0.75f, tube * 0.75f, tube * 0.75f), JugulisSpot, soft: tube * 0.3f);
        }
    }

    /// <summary>
    /// One of Iron Jugulis's heads at <paramref name="c"/>, <paramref name="k"/> times the size of an arm's: a shell
    /// of black armour behind and over it and a face of glowing blue light in front, its eyes shown on it.
    /// </summary>
    private static void JugulisArmHead(PokeBuilder b, int bone, Vector3 c, float k)
    {
        b.Ell(bone, c + V(0, 0.01f, -0.015f) * k, V(0.05f, 0.048f, 0.048f) * k, JugulisArmor, mat: Metal);
        var fc = c + V(0, -0.005f, 0.02f) * k;
        var fr = V(0.042f, 0.038f, 0.04f) * k;
        b.Ell(bone, fc, fr, JugulisLed, mat: Glow, blend: 0.015f * k);
        // Three plates of the shell swept back over its crown
        foreach (float x in new[] { -0.6f, 0f, 0.6f })
        {
            var root = c + V(x * 0.03f, 0.04f, -0.02f) * k;
            b.Spike(bone, root, root + V(x * 0.03f, 0.03f, -0.06f) * k, 0.014f * k, JugulisArmor, 0.6f, Metal);
        }
        PokeBuilder.Both(t =>
        {
            var at = On(fc, fr, fc.X + 0.016f * k * t, fc.Y + 0.006f * k);
            b.Eye(bone, at, Outward(fc, fr, at), 0.009f * k, Rgb(20, 22, 34));
        });
    }

    /// <summary>
    /// Iron Jugulis: Hydreigon of the far future, a machine dragon in flight. A dark blue body under black armour like
    /// fur over its neck and shoulders, two fuchsia stripes round its belly, fin-like feet and a tail striped fuchsia
    /// ending in a black plate; six thin pointed black wings, each bent at a joint; fuchsia collars spotted white round
    /// each neck; and three heads whose faces are screens of blue light: the main one with fuchsia pupils over a black
    /// jaw plate of pointed teeth, and one at the end of each arm, which in the original float free of them.
    /// </summary>
    private static PokeBuilder IronJugulis()
    {
        var b = new PokeBuilder("Iron Jugulis", 1f, BodyPlan.Bird, V(0, 0.5f, 0)) { Coat = Metal }.Hover();
        var bc = V(0, 0.5f, 0);
        b.Ell(Body, bc, V(0.085f, 0.13f, 0.085f), JugulisNavy);
        b.PaintTorus(Body, bc + V(0, -0.06f, 0), 0.075f, 0.009f, JugulisFuchsia);
        b.PaintTorus(Body, bc + V(0, -0.095f, 0), 0.058f, 0.008f, JugulisFuchsia);
        // The armour over its shoulders: a black ruff of plates pointing out and down
        var rc = bc + V(0, 0.07f, 0);
        b.Ell(Body, rc, V(0.12f, 0.07f, 0.1f), JugulisArmor, blend: 0.02f);
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.Tau / 12f + MathF.PI / 12f;
            if (MathF.Cos(a) > 0.85f) continue;
            var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = rc + dir * 0.08f + V(0, 0.02f, 0);
            var tip = rc + dir * 0.165f + V(0, -0.05f, 0);
            Blade(b, Body, root, tip, 0.035f, JugulisArmor, dir * 0.4f + V(0, 1f, 0), 0.3f, Metal);
        }
        // Fin-like feet
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, bc + V(0.045f * s, -0.08f, 0.02f));
            var foot = bc + V(0.06f * s, -0.17f, 0.03f);
            b.Limb(leg, bc + V(0.045f * s, -0.08f, 0.02f), foot, 0.024f, 0.017f, JugulisNavy);
            Blade(b, leg, foot + V(0, 0.01f, 0), foot + V(0.02f * s, -0.065f, 0.035f), 0.032f, JugulisArmor, V(s, 0, 0), 0.25f, Metal);
        });
        // Six thin wings, three to a side, each bent at a joint
        PokeBuilder.Both(s =>
        {
            var root = bc + V(0.05f * s, 0.09f, -0.07f);
            int wing = b.Wing(s, root);
            foreach (var (dx, dy, len) in new[] { (0.8f, 0.9f, 0.32f), (1f, 0.3f, 0.34f), (0.85f, -0.25f, 0.28f) })
            {
                var dir = Vector3.Normalize(V(dx * s, dy, -0.3f));
                var joint = root + dir * len * 0.45f;
                var tip = joint + Vector3.Normalize(dir + V(0, -0.3f, -0.1f)) * len * 0.6f;
                b.Limb(wing, root, joint, 0.017f, 0.012f, JugulisArmor, Metal, 0.01f);
                b.Ell(wing, joint, V(0.017f, 0.017f, 0.017f), JugulisArmor, mat: Metal, blend: 0.006f);
                Blade(b, wing, joint, tip, 0.03f, JugulisArmor, V(0, 0, 1f), 0.22f, Metal);
            }
        });
        // The tail, a fuchsia stripe round it, ending in a black plate
        int tail = b.Tail(bc + V(0, -0.1f, -0.03f));
        var tp = Smooth(3, bc + V(0, -0.1f, -0.03f), bc + V(0, -0.24f, -0.07f), bc + V(0, -0.33f, -0.17f), bc + V(0, -0.36f, -0.28f));
        b.Tube(tail, tp, 0.05f, 0.014f, JugulisNavy, blend: 0f);
        int mid = tp.Length / 2;
        b.PaintTorus(tail, tp[mid], 0.034f, 0.008f, JugulisFuchsia, Euler(tp[mid + 1] - tp[mid - 1]));
        var tailWay = Vector3.Normalize(tp[^1] - tp[^2]);
        Blade(b, tail, tp[^1] - tailWay * 0.01f, tp[^1] + tailWay * 0.08f, 0.04f, JugulisArmor, V(1f, 0, 0), 0.25f, Metal);
        // The arms, armoured black at the shoulder, each ending in a head of its own
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.09f * s, 0.05f, 0.02f);
            int arm = b.Arm(s, shoulder);
            var hand = bc + V(0.2f * s, 0.0f, 0.12f);
            b.Limb(arm, shoulder, hand, 0.032f, 0.026f, JugulisNavy);
            b.Ell(arm, Vector3.Lerp(shoulder, hand, 0.3f), V(0.045f, 0.04f, 0.045f), JugulisArmor, blend: 0.015f);
            JugulisCollar(b, arm, Vector3.Lerp(shoulder, hand, 0.82f), hand - shoulder, 0.028f, 0.01f);
            JugulisArmHead(b, arm, hand + V(0.03f * s, 0.015f, 0.035f), 0.95f);
        });
        // The main head on a short neck, a collar at its root
        int head = b.Head(bc + V(0, 0.12f, 0.03f));
        var c = bc + V(0, 0.23f, 0.06f);
        b.Limb(head, bc + V(0, 0.1f, 0.02f), c + V(0, -0.05f, -0.02f), 0.04f, 0.036f, JugulisNavy);
        JugulisCollar(b, head, c + V(0, -0.06f, -0.018f), V(0, 1f, 0.3f), 0.042f, 0.014f);
        var hc = c + V(0, 0.015f, -0.015f);
        b.Ell(head, hc, V(0.075f, 0.068f, 0.072f), JugulisArmor);
        var fc = c + V(0, -0.012f, 0.03f);
        var fr = V(0.058f, 0.048f, 0.055f);
        b.Ell(head, fc, fr, JugulisLed, mat: Glow, blend: 0.015f);
        // The black jaw plate, its pointed teeth turned up into the face
        b.Ell(head, fc + V(0, -0.042f, 0.005f), V(0.05f, 0.022f, 0.045f), JugulisArmor, blend: 0.012f);
        foreach (float x in new[] { -0.027f, -0.009f, 0.009f, 0.027f })
            b.Spike(head, fc + V(x, -0.032f, 0.03f), fc + V(x, -0.02f, 0.05f), 0.008f, JugulisSpot, mat: Shell, blend: 0.004f);
        // The hood's plates swept back like a mane
        foreach (float a in new[] { -80f, -50f, -20f, 20f, 50f, 80f, 0f })
        {
            var d = V(MathF.Sin(a * Degree), MathF.Cos(a * Degree) * 0.9f, -0.35f);
            b.Spike(head, hc + V(0, 0, -0.005f) + d * 0.05f, hc + V(0, 0, -0.005f) + d * 0.13f, 0.016f, JugulisArmor, 0.6f, Metal);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(fc, fr, fc.X + 0.026f * s, fc.Y + 0.014f);
            b.Eye(head, at, Outward(fc, fr, at), 0.012f, JugulisFuchsia, glare: true);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Iron Moth

    private static readonly Color MothOrange = Rgb(244, 122, 46);
    private static readonly Color MothArmor = Rgb(234, 236, 242);
    private static readonly Color MothBlack = Rgb(38, 38, 46);
    private static readonly Color MothBlue = Rgb(120, 192, 232);
    private static readonly Color MothRed = Rgb(226, 46, 52);

    /// <summary>
    /// Iron Moth: Volcarona of the far future, a machine moth. A thorax of white armour with a flared collar of white
    /// plates, a black head with red compound eyes and red horns curling up from its sides, an abdomen of light blue
    /// banded black with a black diamond on each side, four small black feet, and six diamond-shaped orange wings
    /// speckled white with a black wedge on each, which in the original hover a little apart from its body.
    /// </summary>
    private static PokeBuilder IronMoth()
    {
        var b = new PokeBuilder("Iron Moth", 1f, BodyPlan.Bird, V(0, 0.46f, 0)) { Coat = Metal }.Hover();
        var bc = V(0, 0.46f, 0);
        // Six diamond wings, each two blades meeting at its widest, and a knob where it is held to the body
        PokeBuilder.Both(s =>
        {
            var root = bc + V(0.04f * s, 0.02f, -0.04f);
            int wing = b.Wing(s, root);
            foreach (var (a, len, wide) in new[] { (65f, 0.3f, 0.085f), (10f, 0.32f, 0.095f), (-45f, 0.26f, 0.075f) })
            {
                var d = Vector3.Normalize(V(MathF.Cos(a * Degree) * s, MathF.Sin(a * Degree), -0.15f));
                var widest = root + d * len * 0.42f;
                b.Limb(wing, root, root + d * 0.05f, 0.016f, 0.014f, MothBlack, Metal, 0.008f);
                b.Ell(wing, root + d * 0.05f, V(0.022f, 0.022f, 0.022f), MothBlack, mat: Metal, blend: 0.008f);
                Blade(b, wing, widest, root + d * len, wide, MothOrange, V(0, 0, 1f), 0.22f, Metal);
                Blade(b, wing, widest, root + d * 0.04f, wide, MothOrange, V(0, 0, 1f), 0.22f, Metal);
                var across = Vector3.Normalize(Vector3.Cross(d, V(0, 0, 1f)));
                b.PaintEll(wing, root + d * len * 0.72f, V(wide * 0.38f, len * 0.13f, 0.03f), MothBlack, Euler(d), 0.006f);
                foreach (var (t, side) in new[] { (0.3f, 0.3f), (0.42f, -0.45f), (0.5f, 0.5f), (0.58f, -0.1f), (0.86f, 0.05f) })
                    b.PaintEll(wing, root + d * len * t + across * side * wide, V(0.012f, 0.012f, 0.03f), MothArmor, soft: 0.005f);
            }
        });
        // The thorax of white armour and the collar of plates flared round its neck
        b.Ell(Body, bc, V(0.075f, 0.08f, 0.07f), MothArmor);
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f + MathF.PI / 8f;
            var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
            Blade(b, Body, bc + dir * 0.05f + V(0, 0.045f, 0), bc + dir * 0.12f + V(0, 0.02f, 0), 0.035f, MothArmor, V(0, 1f, 0) + dir * 0.3f, 0.3f, Metal);
        }
        // The abdomen hanging below, light blue banded black, a black diamond on each side
        var ac = bc + V(0, -0.15f, 0);
        var ar = V(0.05f, 0.1f, 0.048f);
        b.Ell(Body, ac, ar, MothBlue, blend: 0.02f);
        foreach (float y in new[] { 0.05f, -0.06f })
            b.PaintEll(Body, ac + V(0, y, 0), V(0.06f, 0.009f, 0.06f), MothBlack, soft: 0.005f);
        b.PaintEll(Body, ac + V(0, -0.09f, 0), V(0.04f, 0.03f, 0.04f), MothBlack, soft: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = Out(ac, ar, default, V(s, -0.6f, 0.2f));
            b.Mark(Body, at, Outward(ac, ar, at), 0.013f, 0.017f, MothBlack, MarkShape.Diamond);
        });
        // Four small black feet under the thorax
        foreach (var (z, front) in new[] { (0.03f, true), (-0.025f, false) })
            PokeBuilder.Both(s =>
            {
                var hip = bc + V(0.035f * s, -0.05f, z);
                int leg = b.Leg(s, hip, front);
                var knee = bc + V(0.075f * s, -0.09f, z + 0.015f);
                b.Limb(leg, hip, knee, 0.013f, 0.011f, MothBlack);
                b.Limb(leg, knee, bc + V(0.07f * s, -0.135f, z + 0.025f), 0.011f, 0.008f, MothBlack);
            });
        // The black head, red compound eyes, and red horns curling up from its sides
        int head = b.Head(bc + V(0, 0.07f, 0.03f));
        var c = bc + V(0, 0.1f, 0.05f);
        var r = V(0.052f, 0.046f, 0.048f);
        b.Ell(head, c, r, MothBlack);
        b.PaintEll(head, c + V(0, 0.045f, -0.01f), V(0.03f, 0.02f, 0.04f), MothArmor, soft: 0.006f);
        PokeBuilder.Both(s =>
        {
            b.Tube(head, Smooth(3, c + V(0.035f * s, 0.02f, -0.01f), c + V(0.075f * s, 0.07f, -0.02f), c + V(0.062f * s, 0.13f, -0.035f)), 0.014f, 0.006f, MothRed, blend: 0f);
            var at = On(c, r, c.X + 0.024f * s, c.Y + 0.002f);
            b.Eye(head, at, Outward(c, r, at), 0.015f, MothRed, glare: true);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Iron Thorns

    private static readonly Color ThornsGreen = Rgb(108, 160, 104);
    private static readonly Color ThornsPlate = Rgb(72, 112, 80);
    private static readonly Color ThornsBlack = Rgb(40, 42, 46);
    private static readonly Color ThornsGlow = Rgb(160, 252, 150);

    /// <summary>A window of glowing green set into armour at <paramref name="at"/> on a surface facing <paramref name="n"/>, bulging a little out of it.</summary>
    private static void ThornsWindow(PokeBuilder b, int bone, Vector3 at, Vector3 n, Vector3 radii)
    {
        var u = Vector3.Normalize(n);
        b.Ell(bone, at - u * 0.012f, radii, ThornsGlow, mat: Glow, blend: 0.006f);
    }

    /// <summary>
    /// Iron Thorns: Tyranitar of the far future, a great machine dinosaur standing on two legs. Green armour plate over
    /// its body and limbs, broad pauldrons on its shoulders, long arms ending in black claws, windows of glowing green
    /// let into its chest and thighs, a row of glowing green spikes down its back, black bands across its belly, a horned
    /// green helm with a black lower jaw, glowing eyes, and a thick tail tipped black.
    /// </summary>
    private static PokeBuilder IronThorns()
    {
        var b = new PokeBuilder("Iron Thorns", 1f, BodyPlan.Biped, V(0, 0.48f, 0)) { Coat = Metal };
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.11f * s, 0.32f, 0));
            var tc = V(0.13f * s, 0.25f, 0.01f);
            var tr = V(0.085f, 0.11f, 0.095f);
            b.Ell(leg, tc, tr, ThornsGreen);
            b.Limb(leg, V(0.13f * s, 0.2f, 0.01f), V(0.13f * s, 0.07f, 0.03f), 0.065f, 0.055f, ThornsPlate);
            b.Ell(leg, V(0.13f * s, 0.035f, 0.06f), V(0.068f, 0.035f, 0.095f), ThornsGreen);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(leg, V(0.13f * s + t * 0.035f, 0.03f, 0.13f), V(0.13f * s + t * 0.045f, 0.012f, 0.18f), 0.015f, ThornsBlack, mat: Shell, blend: 0.004f);
            var n = V(0.8f * s, 0, 0.6f);
            ThornsWindow(b, leg, Out(tc, tr, default, n), n, V(0.03f, 0.05f, 0.03f));
        });
        var bc = V(0, 0.5f, 0);
        var br = V(0.17f, 0.22f, 0.15f);
        b.Ell(Body, bc, br, ThornsGreen);
        // The black bands across its belly and the glowing window in its chest
        foreach (float y in new[] { 0.36f, 0.41f })
            b.PaintEll(Body, V(0, y, 0.12f), V(0.1f, 0.016f, 0.06f), ThornsBlack, soft: 0.006f);
        var chest = V(0, 0.25f, 1f);
        ThornsWindow(b, Body, Out(bc, br, default, chest), chest, V(0.06f, 0.06f, 0.03f));
        // Pauldrons over its shoulders
        PokeBuilder.Both(s => b.Box(Body, V(0.16f * s, 0.65f, 0.0f), V(0.08f, 0.05f, 0.09f), 0.025f, ThornsPlate, V(0, 0, -22f * s), blend: 0.015f));
        // Glowing spikes down its back in two rows
        PokeBuilder.Both(s =>
        {
            foreach (var (y, len) in new[] { (0.66f, 0.14f), (0.57f, 0.16f), (0.47f, 0.14f), (0.37f, 0.1f) })
            {
                var root = V(0.065f * s, y, -0.095f);
                Blade(b, Body, root, root + Vector3.Normalize(V(0.45f * s, 0.25f, -1f)) * len, 0.045f, ThornsGlow, V(0, 1f, 0), 0.32f, Glow);
            }
        });
        // Long arms with great black claws
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.16f * s, 0.62f, 0.02f);
            int arm = b.Arm(s, shoulder);
            var elbow = V(0.26f * s, 0.47f, 0.05f);
            var hand = V(0.28f * s, 0.31f, 0.12f);
            b.Limb(arm, shoulder, elbow, 0.058f, 0.048f, ThornsGreen);
            b.Limb(arm, elbow, hand, 0.048f, 0.044f, ThornsPlate);
            b.Torus(arm, Vector3.Lerp(elbow, hand, 0.7f), 0.046f, 0.012f, ThornsBlack, Euler(hand - elbow), blend: 0.006f);
            b.Ell(arm, hand, V(0.05f, 0.045f, 0.05f), ThornsGreen);
            Digits(b, arm, hand + V(0, -0.02f, 0.02f), V(0.15f * s, -0.7f, 0.6f), V(0.45f, 0, 0), 0.07f, 0.016f, ThornsBlack, Shell);
        });
        // A thick tail, tipped black
        int tail = b.Tail(V(0, 0.34f, -0.12f));
        var path = Smooth(3, V(0, 0.36f, -0.1f), V(0, 0.2f, -0.3f), V(0, 0.12f, -0.48f));
        b.Tube(tail, path, 0.085f, 0.032f, ThornsGreen, blend: 0f);
        b.PaintEll(tail, path[^1], V(0.05f, 0.05f, 0.07f), ThornsBlack, soft: 0.01f);
        // The horned helm, its black lower jaw hanging open below
        int head = b.Head(V(0, 0.68f, 0.04f));
        var c = V(0, 0.78f, 0.06f);
        var r = V(0.085f, 0.075f, 0.09f);
        b.Ell(head, c, r, ThornsGreen);
        b.Ell(head, V(0, 0.735f, 0.14f), V(0.06f, 0.035f, 0.06f), ThornsGreen, blend: 0.02f);
        b.Spike(head, c + V(0, 0.06f, 0.01f), c + V(0, 0.2f, -0.03f), 0.035f, ThornsGreen, 0.7f);
        PokeBuilder.Both(s => Blade(b, head, c + V(0.06f * s, 0.02f, -0.05f), c + V(0.14f * s, 0.06f, -0.14f), 0.035f, ThornsPlate, V(0, 1f, 0), 0.32f));
        int jaw = b.Jaw(head, V(0, 0.73f, 0.07f));
        b.Ell(jaw, V(0, 0.7f, 0.13f), V(0.062f, 0.026f, 0.065f), ThornsBlack, blend: 0.012f);
        PokeBuilder.Both(s => b.Spike(jaw, V(0.035f * s, 0.715f, 0.17f), V(0.035f * s, 0.735f, 0.175f), 0.009f, Claw, mat: Shell, blend: 0.003f));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.045f * s, 0.808f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(230, 250, 120), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Frigibax, Arctibax and Baxcalibur

    private static readonly Color FrigiGrey = Rgb(150, 160, 178);
    private static readonly Color FrigiSnout = Rgb(208, 214, 224);
    private static readonly Color FrigiBelly = Rgb(178, 214, 240);
    private static readonly Color FrigiFin = Rgb(84, 88, 106);
    private static readonly Color FrigiYellow = Rgb(250, 218, 80);
    private static readonly Color BaxcalIce = Rgb(176, 226, 248);
    private static readonly Color BaxcalClaw = Rgb(228, 238, 248);
    private static readonly Color BaxcalPupil = Rgb(30, 30, 36);

    /// <summary>Frigibax: a stout little grey dragon on two stubby legs, a big head with yellow eyes, a bulbous pale snout with a wide mouth and a square crystal of ice under its lip, a light blue belly, a dark triangular fin on a whitish round patch on its back, a long thin yellow quill on the back of each hand and a short tail.</summary>
    private static PokeBuilder Frigibax()
    {
        var b = new PokeBuilder("Frigibax", 0.45f, BodyPlan.Biped, V(0, 0.1f, 0)) { Coat = Scales };
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.035f * s, 0.06f, 0));
            b.Limb(leg, V(0.035f * s, 0.07f, 0), V(0.04f * s, 0.025f, 0.005f), 0.024f, 0.022f, FrigiGrey);
            b.Ell(leg, V(0.042f * s, 0.016f, 0.018f), V(0.026f, 0.016f, 0.03f), FrigiGrey);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(leg, V(0.042f * s + t * 0.012f, 0.012f, 0.04f), V(0.042f * s + t * 0.014f, 0.008f, 0.054f), 0.006f, BaxcalClaw, mat: Shell, blend: 0.003f);
        });
        var bc = V(0, 0.1f, 0);
        b.Ell(Body, bc, V(0.062f, 0.065f, 0.058f), FrigiGrey);
        b.PaintEll(Body, bc + V(0, -0.01f, 0.045f), V(0.045f, 0.05f, 0.025f), FrigiBelly);
        // The whitish round patch on its back and the fin standing on it
        b.PaintEll(Body, bc + V(0, 0.015f, -0.055f), V(0.042f, 0.042f, 0.02f), Rgb(232, 236, 242), soft: 0.006f);
        Blade(b, Body, bc + V(0, 0.025f, -0.05f), bc + V(0, 0.1f, -0.095f), 0.03f, FrigiFin, V(1f, 0, 0), 0.25f, Shell);
        int tail = b.Tail(bc + V(0, -0.03f, -0.05f));
        b.Spike(tail, bc + V(0, -0.03f, -0.045f), bc + V(0, -0.05f, -0.1f), 0.022f, FrigiGrey);
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.05f * s, 0.03f, 0.01f);
            int arm = b.Arm(s, shoulder);
            var hand = bc + V(0.085f * s, -0.005f, 0.03f);
            b.Limb(arm, shoulder, hand, 0.017f, 0.015f, FrigiGrey);
            b.Ell(arm, hand, V(0.018f, 0.017f, 0.018f), FrigiGrey);
            Digits(b, arm, hand + V(0, -0.006f, 0.008f), V(0.3f * s, -0.5f, 0.8f), V(0.5f, 0, 0), 0.016f, 0.0065f, BaxcalClaw, Shell);
            // The long thin yellow quill on the back of its hand
            b.Spike(arm, hand + V(0, 0.008f, -0.01f), hand + V(0.03f * s, 0.065f, -0.06f), 0.006f, Rgb(246, 208, 64), mat: Shell, blend: 0.004f);
        });
        int head = b.Head(bc + V(0, 0.05f, 0));
        var c = bc + V(0, 0.115f, 0.015f);
        var r = V(0.08f, 0.068f, 0.072f);
        b.Limb(head, bc + V(0, 0.03f, 0), c + V(0, -0.04f, 0), 0.04f, 0.045f, FrigiGrey);
        b.Ell(head, c, r, FrigiGrey);
        // The bulbous pale snout, its nostrils and its wide mouth
        var sc = c + V(0, -0.032f, 0.06f);
        var sr = V(0.05f, 0.03f, 0.035f);
        b.Ell(head, sc, sr, FrigiSnout, blend: 0.015f);
        PokeBuilder.Both(s =>
        {
            var nose = Out(sc, sr, default, V(0.3f * s, 0.45f, 0.85f));
            b.Mark(head, nose, Outward(sc, sr, nose), 0.004f, 0.005f, Rgb(70, 72, 90));
        });
        var mouth = Out(sc, sr, default, V(0, -0.5f, 1f));
        b.Mark(head, mouth, Outward(sc, sr, mouth), 0.024f, 0.008f, Rgb(70, 62, 82), MarkShape.Smile);
        b.Box(head, Out(sc, sr, default, V(0.65f, -0.6f, 0.5f)), V(0.009f, 0.009f, 0.009f), 0.002f, BaxcalIce, V(0, 20f, 12f), Shell, 0.004f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.044f * s, c.Y + 0.03f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, white: FrigiYellow, pupil: BaxcalPupil);
        });
        return b;
    }

    /// <summary>Arctibax: a bluish-grey dragon crouched forward on two legs, a light blue underside, a great dark triangular dorsal fin rimmed with ice, a mask of ice frozen over its snout and jaw with icicles at its chin, yellow eyes, three pale claws to each hand and foot, long thin orange quills in a V on the back of each hand and a medium tail.</summary>
    private static PokeBuilder Arctibax()
    {
        var b = new PokeBuilder("Arctibax", 0.7f, BodyPlan.Biped, V(0, 0.28f, -0.02f)) { Coat = Scales };
        var slate = Rgb(118, 132, 162);
        var orange = Rgb(246, 140, 50);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.075f * s, 0.24f, -0.04f));
            var knee = V(0.1f * s, 0.15f, 0.0f);
            var ankle = V(0.095f * s, 0.05f, -0.04f);
            b.Ell(leg, V(0.085f * s, 0.21f, -0.03f), V(0.05f, 0.07f, 0.065f), slate, V(20f, 0, 0));
            b.Limb(leg, V(0.085f * s, 0.2f, -0.03f), knee, 0.042f, 0.032f, slate);
            b.Limb(leg, knee, ankle, 0.03f, 0.024f, slate);
            b.Ell(leg, ankle + V(0, -0.025f, 0.03f), V(0.032f, 0.022f, 0.05f), slate);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(leg, ankle + V(t * 0.018f, -0.03f, 0.07f), ankle + V(t * 0.022f, -0.038f, 0.095f), 0.009f, BaxcalClaw, mat: Shell, blend: 0.003f);
        });
        // The body crouched forward, its underside light blue
        var bc = V(0, 0.28f, -0.02f);
        var tilt = V(-35f, 0, 0);
        b.Ell(Body, bc, V(0.1f, 0.11f, 0.15f), slate, tilt);
        b.PaintEll(Body, bc + V(0, -0.06f, 0.05f), V(0.07f, 0.06f, 0.1f), FrigiBelly, tilt);
        // The great dorsal fin, rimmed with ice along its trailing edge
        var finRoot = bc + V(0, 0.09f, -0.04f);
        var finTip = bc + V(0, 0.29f, -0.21f);
        Blade(b, Body, finRoot, finTip, 0.085f, FrigiFin, V(1f, 0, 0), 0.18f, Shell);
        b.PaintEll(Body, Vector3.Lerp(finRoot, finTip, 0.7f) + V(0, -0.03f, -0.035f), V(0.03f, 0.05f, 0.025f), BaxcalIce, Euler(finTip - finRoot), 0.008f);
        int tail = b.Tail(bc + V(0, -0.05f, -0.12f));
        var tp = Smooth(3, bc + V(0, -0.05f, -0.11f), bc + V(0, -0.12f, -0.24f), bc + V(0, -0.17f, -0.36f));
        b.Tube(tail, tp, 0.055f, 0.016f, slate, blend: 0f);
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.065f * s, 0.07f, 0.09f);
            int arm = b.Arm(s, shoulder);
            var hand = bc + V(0.12f * s, -0.06f, 0.19f);
            b.Limb(arm, shoulder, hand, 0.03f, 0.024f, slate);
            b.Ell(arm, hand, V(0.026f, 0.024f, 0.026f), slate);
            Digits(b, arm, hand + V(0, -0.012f, 0.01f), V(0.2f * s, -0.6f, 0.7f), V(0.5f, 0, 0), 0.03f, 0.009f, BaxcalClaw, Shell);
            // The orange quills in a V on the back of its hand
            var back = hand + V(0, 0.012f, -0.012f);
            b.Spike(arm, back, back + V(0.015f * s, 0.085f, -0.06f), 0.007f, orange, mat: Shell, blend: 0.004f);
            b.Spike(arm, back, back + V(0.06f * s, 0.065f, -0.07f), 0.007f, orange, mat: Shell, blend: 0.004f);
        });
        int head = b.Head(bc + V(0, 0.1f, 0.12f));
        var c = bc + V(0, 0.2f, 0.19f);
        var r = V(0.06f, 0.055f, 0.065f);
        b.Limb(head, bc + V(0, 0.07f, 0.1f), c + V(0, -0.03f, -0.03f), 0.05f, 0.042f, slate);
        b.Ell(head, c, r, slate);
        b.Ell(head, c + V(0, -0.03f, 0.06f), V(0.045f, 0.032f, 0.05f), slate, blend: 0.015f);
        // The mask of ice frozen over its snout and jaw, and icicles hanging from its chin
        b.Ell(head, c + V(0, -0.035f, 0.07f), V(0.05f, 0.032f, 0.052f), BaxcalIce, mat: Shell, blend: 0.008f);
        PokeBuilder.Both(s => b.Spike(head, c + V(0.018f * s, -0.055f, 0.09f), c + V(0.02f * s, -0.09f, 0.095f), 0.009f, BaxcalIce, mat: Shell, blend: 0.004f));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.03f * s, c.Y + 0.03f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, white: FrigiYellow, pupil: BaxcalPupil, glare: true);
        });
        return b;
    }

    /// <summary>
    /// Baxcalibur, or (<paramref name="mega"/>) its Mega form. Baxcalibur is a great dark blue dragon standing tall on
    /// two legs, hexagonal plates of ice on its chest, its knees and under its tail, light blue soles, long strong arms
    /// with three white claws and three red quills at each wrist, white claws on its feet, a mask of ice over its face
    /// with two spikes of ice standing on its snout and icicles hanging from its chin, yellow eyes, a long heavy tail and
    /// a black sail like the blade of an axe on its back, coated in ice along its edge. The Mega form, whose design is
    /// sculpted here as a plausible stronger Baxcalibur, has its sail grown into a great sword standing over its back,
    /// an icy hilt with a crossguard and a gem at the pit of its chest, more ice over its belly and two plates more at
    /// the root of its neck, its mask grown round its eyes and the back of its head, and red pupils.
    /// </summary>
    private static PokeBuilder BaxcaliburBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Baxcalibur-Mega" : "Baxcalibur", 1f, BodyPlan.Biped, V(0, 0.55f, -0.02f)) { Coat = Scales };
        var navy = Rgb(70, 86, 128);
        var sail = Rgb(44, 46, 58);
        var red = Rgb(216, 56, 52);
        var sole = Rgb(170, 210, 240);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.42f, -0.02f));
            var tc = V(0.12f * s, 0.33f, 0.02f);
            var tr = V(0.08f, 0.12f, 0.09f);
            b.Ell(leg, tc, tr, navy);
            var ankle = V(0.13f * s, 0.07f, -0.02f);
            b.Limb(leg, V(0.125f * s, 0.27f, 0.02f), ankle, 0.055f, 0.042f, navy);
            b.Ell(leg, V(0.13f * s, 0.035f, 0.04f), V(0.06f, 0.035f, 0.09f), navy);
            b.PaintEll(leg, V(0.13f * s, 0.0f, 0.04f), V(0.055f, 0.014f, 0.085f), sole, soft: 0.008f);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(leg, V(0.13f * s + t * 0.032f, 0.03f, 0.115f), V(0.13f * s + t * 0.04f, 0.012f, 0.16f), 0.014f, BaxcalClaw, mat: Shell, blend: 0.004f);
            var knee = Out(tc, tr, default, V(0.25f * s, -0.4f, 1f));
            HexCell(b, leg, knee, Outward(tc, tr, knee), 0.03f, 0.01f, BaxcalIce);
        });
        var bc = V(0, 0.55f, -0.02f);
        var br = V(0.15f, 0.2f, 0.15f);
        b.Ell(Body, bc, br, navy);
        // The hexagonal plates of ice over its chest
        var plates = new List<Vector3> { V(0.3f, 0.3f, 1f), V(-0.3f, 0.3f, 1f), V(0, 0.05f, 1f), V(0.33f, -0.2f, 1f), V(-0.33f, -0.2f, 1f) };
        if (mega) plates.AddRange(new[] { V(0, -0.45f, 1f), V(0.6f, 0.62f, 0.75f), V(-0.6f, 0.62f, 0.75f), V(0.55f, -0.5f, 0.85f), V(-0.55f, -0.5f, 0.85f) });
        foreach (var dir in plates)
        {
            if (mega && dir.X == 0 && dir.Y > 0) continue;
            var at = Out(bc, br, default, dir);
            HexCell(b, Body, at, Outward(bc, br, at), 0.036f, 0.012f, BaxcalIce);
        }
        if (mega)
        {
            // The icy hilt at the pit of its chest: a grip, a crossguard and a gem
            var hilt = Out(bc, br, default, V(0, 0.1f, 1f));
            b.Box(Body, hilt + V(0, -0.01f, 0.0f), V(0.026f, 0.075f, 0.022f), 0.008f, BaxcalIce, mat: Shell, blend: 0.008f);
            b.Box(Body, hilt + V(0, 0.07f, -0.01f), V(0.085f, 0.018f, 0.024f), 0.008f, BaxcalIce, mat: Shell, blend: 0.006f);
            b.Ell(Body, hilt + V(0, 0.07f, 0.02f), V(0.022f, 0.028f, 0.014f), Rgb(120, 230, 250), V(0, 0, 45f), Glow, 0.004f);
        }
        // The sail on its back: an axe's blade of black ice, or the Mega form's great sword
        var sailRoot = bc + V(0, 0.08f, -0.11f);
        if (mega)
        {
            var swordTip = bc + V(0, 0.75f, -0.42f);
            var way = Vector3.Normalize(swordTip - sailRoot);
            var midSword = Vector3.Lerp(sailRoot, swordTip, 0.45f);
            b.Box(Body, midSword, V(0.022f, Vector3.Distance(sailRoot, swordTip) * 0.45f, 0.1f), 0.01f, sail, Euler(way), Shell, 0.015f);
            Blade(b, Body, sailRoot + way * Vector3.Distance(sailRoot, swordTip) * 0.88f, swordTip + way * 0.08f, 0.1f, sail, V(1f, 0, 0), 0.22f, Shell);
            var back = Vector3.Normalize(Vector3.Cross(way, V(1f, 0, 0)));
            foreach (float t in new[] { 0.3f, 0.55f, 0.8f })
                b.PaintEll(Body, Vector3.Lerp(sailRoot, swordTip, t) + back * 0.1f, V(0.04f, 0.09f, 0.03f), Rgb(120, 170, 220), Euler(way), 0.012f);
        }
        else
        {
            var sailTip = bc + V(0, 0.4f, -0.4f);
            Frond(b, Body, sailRoot, sailTip, 0.15f, sail, V(1f, 0, 0), 0.16f, Shell);
            Blade(b, Body, Vector3.Lerp(sailRoot, sailTip, 0.6f), sailTip + V(0, 0.06f, -0.03f), 0.06f, sail, V(1f, 0, 0), 0.3f, Shell);
            b.PaintEll(Body, Vector3.Lerp(sailRoot, sailTip, 0.72f) + V(0, -0.085f, -0.085f), V(0.035f, 0.13f, 0.035f), BaxcalIce, Euler(sailTip - sailRoot), 0.01f);
        }
        // Long strong arms with three white claws, and three red quills at each wrist
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.14f * s, 0.12f, 0.03f);
            int arm = b.Arm(s, shoulder);
            var elbow = bc + V(0.25f * s, -0.03f, 0.05f);
            var hand = bc + V(0.27f * s, -0.19f, 0.13f);
            b.Limb(arm, shoulder, elbow, 0.055f, 0.046f, navy);
            b.Limb(arm, elbow, hand, 0.046f, 0.04f, navy);
            b.Ell(arm, hand, V(0.045f, 0.042f, 0.045f), navy);
            Digits(b, arm, hand + V(0, -0.02f, 0.02f), V(0.15f * s, -0.7f, 0.6f), V(0.45f, 0, 0), 0.065f, 0.014f, BaxcalClaw, Shell);
            var wrist = Vector3.Lerp(elbow, hand, 0.75f);
            for (int i = 0; i < 3; i++)
                b.Spike(arm, wrist + V(0.01f * s, 0.0f, -0.025f), wrist + V((0.04f + 0.02f * i) * s, 0.03f + 0.03f * i, -0.13f + 0.015f * i), 0.011f, red, mat: Shell, blend: 0.005f);
        });
        // A long heavy tail, plates of ice under it
        int tail = b.Tail(bc + V(0, -0.1f, -0.12f));
        var tp = Smooth(3, bc + V(0, -0.1f, -0.11f), bc + V(0, -0.25f, -0.3f), bc + V(0, -0.38f, -0.5f), bc + V(0.02f, -0.44f, -0.68f));
        b.Tube(tail, tp, 0.1f, 0.03f, navy, blend: 0f);
        foreach (int i in new[] { 2, 4, 6 })
        {
            float rad = 0.1f - 0.07f * i / (tp.Length - 1);
            var way = Vector3.Normalize(tp[i + 1] - tp[i - 1]);
            var down = Vector3.Normalize(Vector3.Cross(Vector3.Cross(way, V(0, -1f, 0)), way));
            HexCell(b, tail, tp[i] + down * rad * 0.95f, down, rad * 0.45f, 0.01f, BaxcalIce);
        }
        // The head on a thick neck, its face masked in ice
        int head = b.Head(bc + V(0, 0.2f, 0.06f));
        var c = bc + V(0, 0.37f, 0.12f);
        var r = V(0.085f, 0.08f, 0.1f);
        b.Limb(head, bc + V(0, 0.15f, 0.04f), c + V(0, -0.04f, -0.04f), 0.075f, 0.065f, navy);
        b.Ell(head, c, r, navy);
        b.Ell(head, c + V(0, -0.035f, 0.09f), V(0.062f, 0.048f, 0.08f), navy, blend: 0.02f);
        b.Ell(head, c + V(0, -0.045f, 0.11f), V(0.06f, 0.045f, 0.08f), BaxcalIce, mat: Shell, blend: 0.01f);
        PokeBuilder.Both(s => b.Spike(head, c + V(0.03f * s, -0.01f, 0.15f), c + V(0.045f * s, 0.11f, 0.16f), 0.018f, BaxcalIce, mat: Shell, blend: 0.006f));
        foreach (float x in new[] { -0.03f, 0f, 0.03f })
            b.Spike(head, c + V(x, -0.07f, 0.13f - MathF.Abs(x)), c + V(x * 1.1f, -0.135f, 0.125f - MathF.Abs(x)), 0.014f, BaxcalIce, mat: Shell, blend: 0.005f);
        if (mega)
        {
            // The mask grown round its eyes and over the back of its head
            b.PaintEll(head, c + V(0, 0.03f, -0.06f), V(0.1f, 0.07f, 0.07f), BaxcalIce, soft: 0.012f);
            PokeBuilder.Both(s =>
            {
                var at = Out(c, r, default, V(0.6f * s, 0.55f, -0.6f));
                HexCell(b, head, at, Outward(c, r, at), 0.026f, 0.009f, BaxcalIce);
            });
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.054f * s, c.Y + 0.035f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, white: FrigiYellow, pupil: mega ? red : BaxcalPupil, glare: true);
        });
        return b;
    }

    private static PokeBuilder Baxcalibur() => BaxcaliburBuild(false);

    private static PokeBuilder BaxcaliburMega() => BaxcaliburBuild(true);

    // ------------------------------------------------------------------ Gimmighoul and Gholdengo

    private static readonly Color GimmiGold = Rgb(242, 196, 64);
    private static readonly Color GimmiGoldDark = Rgb(196, 144, 40);
    private static readonly Color GimmiRed = Rgb(196, 50, 48);
    private static readonly Color GimmiBlack = Rgb(42, 36, 40);
    private static readonly Color GimmiSilver = Rgb(198, 202, 212);
    private static readonly Color GimmiGrey = Rgb(148, 152, 172);
    private static readonly Color GimmiLimb = Rgb(100, 102, 122);

    /// <summary>An ancient gold coin standing on its edge at <paramref name="at"/>, face toward <c>+z</c> (or <c>-z</c> where <paramref name="back"/>), a raised rim round it and a star stamped in the middle of the face that shows.</summary>
    private static void GimmighoulCoin(PokeBuilder b, int bone, Vector3 at, float radius, bool back)
    {
        float t = radius * 0.22f;
        b.Ell(bone, at, V(radius, radius, t), GimmiGold, mat: Metal, blend: 0.004f);
        b.Torus(bone, at, radius * 0.88f, radius * 0.11f, GimmiGoldDark, V(90f, 0, 0), mat: Metal, blend: 0.004f);
        float f = back ? -1f : 1f;
        b.Mark(bone, at + V(0, 0, t * f), V(0, 0, f), radius * 0.42f, radius * 0.42f, GimmiGoldDark, MarkShape.Star5);
    }

    /// <summary>
    /// Gimmighoul in its Chest Form: a red treasure chest trimmed black and gold, a silver buckle with an oval pattern
    /// on its front, the lid lifted a little on a gold hinge over a heap of gold coins, and Gimmighoul itself peeking
    /// out from under the lid, a small grey head with two glinting eyes and two thin arms gripping the chest's rim.
    /// </summary>
    private static PokeBuilder Gimmighoul()
    {
        var b = new PokeBuilder("Gimmighoul", 0.4f, BodyPlan.Biped, V(0, 0.08f, 0)) { Coat = Shell };
        // The chest, banded black with gold corners, and its buckle
        b.Box(Body, V(0, 0.075f, 0), V(0.12f, 0.07f, 0.085f), 0.012f, GimmiRed, mat: Shell);
        PokeBuilder.Both(s =>
        {
            b.PaintEll(Body, V(0.07f * s, 0.075f, 0), V(0.012f, 0.1f, 0.11f), GimmiBlack, soft: 0.004f);
            foreach (float z in new[] { -1f, 1f })
                foreach (float y in new[] { 0.005f, 0.145f })
                    b.PaintEll(Body, V(0.12f * s, y, 0.085f * z), V(0.022f, 0.022f, 0.022f), GimmiGold, soft: 0.004f);
        });
        b.Box(Body, V(0, 0.11f, 0.088f), V(0.025f, 0.03f, 0.008f), 0.006f, GimmiSilver, mat: Metal, blend: 0.004f);
        b.Mark(Body, V(0, 0.11f, 0.096f), V(0, 0, 1f), 0.013f, 0.018f, Rgb(120, 124, 140), MarkShape.Ring);
        // The heap of gold coins inside, a few lying tilted on top
        b.Ell(Body, V(0, 0.145f, -0.01f), V(0.1f, 0.03f, 0.06f), GimmiGold, mat: Metal, blend: 0.012f);
        foreach (var (x, z, tip) in new[] { (-0.06f, 0.03f, 20f), (0.065f, 0.035f, -25f), (-0.03f, -0.035f, 35f), (0.04f, -0.03f, -15f) })
            b.Ell(Body, V(x, 0.168f, z), V(0.02f, 0.02f, 0.005f), GimmiGold, V(60f, tip, 0), Metal, 0.004f);
        // The lid, lifted on its hinge at the back
        var hinge = V(0, 0.145f, -0.085f);
        int lid = b.Jaw(Body, hinge);
        var open = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -28f * Degree);
        Vector3 L(Vector3 p) => hinge + Vector3.Transform(p - hinge, open);
        var lidTurn = V(-28f, 0, 0);
        b.Limb(lid, hinge + V(-0.1f, 0, -0.004f), hinge + V(0.1f, 0, -0.004f), 0.013f, 0.013f, GimmiGold, Metal, 0.006f);
        b.Box(lid, L(V(0, 0.165f, 0)), V(0.122f, 0.022f, 0.087f), 0.012f, GimmiRed, lidTurn, Shell, 0.01f);
        b.Ell(lid, L(V(0, 0.18f, 0)), V(0.122f, 0.035f, 0.086f), GimmiRed, lidTurn, Shell, 0.01f);
        PokeBuilder.Both(s => b.PaintEll(lid, L(V(0.07f * s, 0.18f, 0)), V(0.012f, 0.06f, 0.11f), GimmiBlack, lidTurn, 0.004f));
        b.PaintEll(lid, L(V(0, 0.16f, 0.09f)), V(0.13f, 0.012f, 0.012f), GimmiGold, lidTurn, 0.004f);
        // Gimmighoul peeking out under the lid
        int head = b.Head(V(0, 0.16f, 0.02f));
        var c = V(0, 0.175f, 0.04f);
        var r = V(0.04f, 0.033f, 0.034f);
        b.Ell(head, c, r, GimmiGrey);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.016f * s, c.Y + 0.003f);
            b.Eye(head, at, Outward(c, r, at), 0.01f, Rgb(252, 214, 84));
        });
        PokeBuilder.Both(s =>
        {
            var shoulder = c + V(0.03f * s, -0.02f, 0.0f);
            int arm = b.Arm(s, shoulder);
            var hand = V(0.075f * s, 0.148f, 0.086f);
            b.Limb(arm, shoulder, hand, 0.008f, 0.007f, GimmiLimb);
            b.Ell(arm, hand, V(0.012f, 0.01f, 0.012f), GimmiLimb, blend: 0.006f);
        });
        return b;
    }

    /// <summary>Gimmighoul in its Roaming Form: a tiny grey ghost walking on thin dark legs, a round head with two glinting eyes over a small body, thin arms, and a tail curled up behind holding an ancient gold coin as big as its head against its back.</summary>
    private static PokeBuilder GimmighoulRoaming()
    {
        var b = new PokeBuilder("Gimmighoul-Roaming", 0.35f, BodyPlan.Biped, V(0, 0.09f, 0)) { Coat = Fur };
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.018f * s, 0.07f, 0));
            b.Limb(leg, V(0.018f * s, 0.078f, 0), V(0.024f * s, 0.012f, 0.004f), 0.009f, 0.008f, GimmiLimb);
            b.Ell(leg, V(0.025f * s, 0.008f, 0.012f), V(0.012f, 0.008f, 0.016f), GimmiLimb);
        });
        var bc = V(0, 0.09f, 0);
        b.Ell(Body, bc, V(0.03f, 0.032f, 0.026f), GimmiGrey);
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.024f * s, 0.014f, 0);
            int arm = b.Arm(s, shoulder);
            var hand = bc + V(0.058f * s, -0.02f, 0.016f);
            b.Limb(arm, shoulder, hand, 0.007f, 0.006f, GimmiLimb);
            b.Ell(arm, hand, V(0.01f, 0.009f, 0.01f), GimmiLimb, blend: 0.005f);
        });
        // The tail curled up behind, holding the coin against its back
        int tail = b.Tail(bc + V(0, -0.012f, -0.02f));
        b.Tube(tail, Smooth(3, bc + V(0, -0.012f, -0.018f), bc + V(0, -0.008f, -0.05f), bc + V(0, 0.02f, -0.066f), bc + V(0, 0.05f, -0.07f)), 0.008f, 0.007f, GimmiLimb, blend: 0f);
        GimmighoulCoin(b, tail, V(0, 0.13f, -0.07f), 0.048f, true);
        int head = b.Head(bc + V(0, 0.03f, 0));
        var c = V(0, 0.16f, 0.006f);
        var r = V(0.05f, 0.045f, 0.044f);
        b.Limb(head, bc + V(0, 0.015f, 0), c + V(0, -0.03f, 0), 0.016f, 0.02f, GimmiGrey);
        b.Ell(head, c, r, GimmiGrey);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.02f * s, c.Y + 0.002f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(252, 214, 84));
        });
        return b;
    }

    /// <summary>Gholdengo: a golden figure made of a thousand coins stacked into a column, banded where they lie on each other, a round head with oval eyes and a crooked smile, four dreadlocks of stacked coins falling from its crown, skinny arms ending in great three-fingered hands, stumpy toeless legs, and a Gimmighoul's black strap belted round its waist, fastening a small red chest at its side.</summary>
    private static PokeBuilder Gholdengo()
    {
        var b = new PokeBuilder("Gholdengo", 0.9f, BodyPlan.Biped, V(0, 0.32f, 0)) { Coat = Metal };
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.045f * s, 0.2f, 0));
            b.Limb(leg, V(0.045f * s, 0.2f, 0), V(0.05f * s, 0.05f, 0.01f), 0.036f, 0.034f, GimmiGold);
            b.Ell(leg, V(0.05f * s, 0.03f, 0.02f), V(0.042f, 0.03f, 0.05f), GimmiGold);
            b.PaintTorus(leg, V(0.048f * s, 0.12f, 0.005f), 0.035f, 0.007f, GimmiGoldDark);
        });
        // The column of stacked coins, flaring at the hips
        b.Ell(Body, V(0, 0.2f, 0), V(0.085f, 0.05f, 0.075f), GimmiGold);
        b.Limb(Body, V(0, 0.2f, 0), V(0, 0.44f, 0), 0.075f, 0.085f, GimmiGold);
        foreach (float y in new[] { 0.29f, 0.335f, 0.38f, 0.425f })
            b.PaintTorus(Body, V(0, y, 0), 0.075f + 0.01f * (y - 0.2f) / 0.24f, 0.007f, GimmiGoldDark);
        // The strap belted round its waist, its buckle, and the little chest at its side
        b.Torus(Body, V(0, 0.245f, 0), 0.078f, 0.012f, GimmiBlack, mat: Shell, blend: 0.004f);
        b.Box(Body, V(0, 0.245f, 0.088f), V(0.016f, 0.016f, 0.006f), 0.004f, GimmiSilver, blend: 0.004f);
        b.Box(Body, V(0.105f, 0.2f, 0.0f), V(0.034f, 0.028f, 0.04f), 0.006f, GimmiRed, mat: Shell, blend: 0.006f);
        b.Ell(Body, V(0.105f, 0.228f, 0.0f), V(0.034f, 0.014f, 0.04f), GimmiRed, mat: Shell, blend: 0.004f);
        b.PaintEll(Body, V(0.105f, 0.225f, 0.0f), V(0.04f, 0.007f, 0.046f), GimmiBlack, soft: 0.003f);
        // Skinny arms and great three-fingered hands
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.075f * s, 0.42f, 0);
            int arm = b.Arm(s, shoulder);
            var elbow = V(0.15f * s, 0.36f, 0.03f);
            var hand = V(0.18f * s, 0.28f, 0.06f);
            b.Ell(arm, shoulder + V(0.008f * s, 0, 0), V(0.024f, 0.024f, 0.024f), GimmiGold);
            b.Limb(arm, shoulder, elbow, 0.016f, 0.015f, GimmiGold);
            b.Limb(arm, elbow, hand, 0.015f, 0.016f, GimmiGold);
            b.Ell(arm, hand, V(0.035f, 0.032f, 0.03f), GimmiGold);
            Digits(b, arm, hand + V(0, -0.015f, 0.005f), V(0.1f * s, -0.8f, 0.4f), V(0.5f, 0, 0), 0.05f, 0.012f, GimmiGold);
        });
        int head = b.Head(V(0, 0.46f, 0));
        var c = V(0, 0.55f, 0.01f);
        var r = V(0.09f, 0.085f, 0.085f);
        b.Ell(head, c, r, GimmiGold);
        // Four dreadlocks of stacked coins falling from its crown
        foreach (var (x, z, outX, outZ) in new[] { (0.03f, 0.0f, 0.12f, -0.02f), (0.06f, -0.04f, 0.13f, -0.1f) })
            PokeBuilder.Both(s =>
            {
                var root = c + V(x * s, 0.07f, z);
                var path = Smooth(3, root, root + V((outX * 0.5f) * s, 0.04f, outZ * 0.4f), c + V(outX * s, 0.02f, outZ * 1.1f - 0.04f), c + V((outX + 0.015f) * s, -0.09f, outZ * 1.2f - 0.06f));
                b.Tube(head, path, 0.028f, 0.022f, GimmiGold, blend: 0f);
                for (int i = 2; i < path.Length - 1; i += 2)
                    b.PaintTorus(head, path[i], 0.024f, 0.007f, GimmiGoldDark, Euler(path[i + 1] - path[i - 1]));
            });
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.032f * s, c.Y + 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.017f, Rgb(36, 36, 52));
        });
        var mouth = On(c, r, c.X + 0.006f, c.Y - 0.035f);
        b.Mark(head, mouth, Outward(c, r, mouth), 0.026f, 0.009f, Rgb(120, 70, 24), MarkShape.Smile, 12f);
        return b;
    }
}
