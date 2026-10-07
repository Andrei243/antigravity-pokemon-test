using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// The last species of Sinnoh to be hand-built (plan 03, decision 3): the seven Platinum keeps outside its Sinnoh
// Pokédex, Heatran (485), Regigigas (486), Cresselia (488), Phione (489), Darkrai (491), Shaymin (492) and Arceus (493).
// Shaymin's Sky Forme and Arceus's types are in PokemonModels.Forms.cs with Platinum's other forms (Arceus's build and
// its plates are here), the Megas of Legends: Z-A in PokemonModels.Megas.cs. Helpers shared with the earlier batches
// are in the files of those batches.
internal static partial class PokemonModels
{
    // ------------------------------------------------------------------ Heatran

    /// <summary>
    /// Heatran and its Mega Evolution. Heatran crouches on four legs bowed out like a spider's, its body a dome of brown
    /// iron splashed with glowing orange and studded with grey, its head a grey helmet with a horn at each side over a
    /// dark face ringed with grey studs like teeth, and orange eyes; grey boots with claws on its feet. The Mega has
    /// melted: a heap of red-hot iron running with yellow, grey plates sliding off it, its face a pool of yellow and
    /// smoke rising from a vent on its back.
    /// </summary>
    private static PokeBuilder HeatranBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Heatran-Mega" : "Heatran", 0.95f, BodyPlan.Quadruped, V(0, 0.3f, 0)) { Coat = Metal };
        var hide = mega ? Rgb(204, 62, 38) : Rgb(130, 64, 40);
        var spot = mega ? Rgb(250, 200, 60) : Rgb(236, 138, 50);
        var grey = Rgb(180, 180, 186);
        var dark = Rgb(62, 60, 66);
        // Four legs bowed out to the sides, studded grey at the knee, ending in grey boots with three claws
        foreach (var (z, front) in new[] { (0.12f, true), (-0.12f, false) })
            PokeBuilder.Both(s =>
            {
                var hip = V(0.13f * s, 0.28f, z);
                int leg = b.Leg(s, hip, front);
                var knee = V(0.29f * s, 0.27f, z * 1.45f);
                var foot = V(0.33f * s, 0.07f, z * 1.6f);
                b.Limb(leg, hip, knee, 0.07f, 0.06f, hide);
                b.Limb(leg, knee, foot, 0.058f, 0.05f, hide);
                b.PaintEll(leg, Vector3.Lerp(hip, knee, 0.5f) + V(0, 0.05f, 0), V(0.035f, 0.025f, 0.03f), spot);
                b.Ell(leg, knee + V(0.02f * s, 0.04f, 0), V(0.04f, 0.03f, 0.04f), grey, blend: 0.012f);
                b.Ell(leg, knee + V(0.05f * s, 0.0f, 0.02f), V(0.03f, 0.025f, 0.03f), grey, blend: 0.012f);
                b.Limb(leg, foot + V(0, 0.03f, 0), foot + V(0, -0.02f, 0), mega ? 0.075f : 0.065f, mega ? 0.08f : 0.07f, grey);
                var outward = V(s * 0.8f, 0, z > 0 ? 0.6f : -0.6f);
                foreach (float t in new[] { -1f, 0f, 1f })
                {
                    var dir = Vector3.Normalize(outward + V(-outward.Z * t * 0.9f * s, 0, outward.X * t * 0.9f * s));
                    var root = foot + V(0, -0.04f, 0) + dir * 0.055f;
                    b.Spike(leg, root, root + dir * 0.05f + V(0, -0.045f, 0), 0.022f, grey, blend: 0.008f);
                }
            });
        var bc = mega ? V(0, 0.34f, -0.03f) : V(0, 0.3f, -0.02f);
        var br = mega ? V(0.21f, 0.2f, 0.21f) : V(0.19f, 0.15f, 0.21f);
        b.Ell(Body, bc, br, hide);
        // The dark iron underneath, ringed by a seam
        b.PaintEll(Body, bc + V(0, -br.Y * 0.95f, 0), V(br.X * 1.1f, br.Y * 0.55f, br.Z * 1.1f), dark);
        float golden = MathF.PI * (3f - MathF.Sqrt(5f));
        for (int i = 0; i < 22; i++)
        {
            float y = 0.95f - 1.3f * (i + 0.5f) / 22f, ring = MathF.Sqrt(1f - y * y), a = i * golden;
            var dir = V(MathF.Sin(a) * ring, y, MathF.Cos(a) * ring);
            if (dir.Z > 0.55f) continue;
            var at = Out(bc, br, default, dir);
            if (i % 2 == 0)
                b.PaintEll(Body, at, V(0.045f, 0.035f, 0.045f) * (mega ? 1.4f : 1f), spot);
            else
                b.Ell(Body, at, V(0.026f, 0.02f, 0.026f), grey, Euler(Outward(bc, br, at)), blend: 0.01f);
        }
        if (mega)
        {
            // Grey plates slipping off the melted body, and a vent on its back with smoke rising from it
            foreach (var dir in new[] { V(0.7f, 0.5f, 0.1f), V(-0.75f, 0.35f, -0.2f), V(0.2f, 0.6f, -0.75f), V(-0.3f, 0.8f, 0.3f) })
            {
                var at = Out(bc, br, default, dir);
                b.Ell(Body, at, V(0.07f, 0.02f, 0.06f), grey, Euler(Outward(bc, br, at)), blend: 0.012f);
            }
            var vent = bc + V(0.06f, br.Y * 0.9f, -0.08f);
            b.Limb(Body, vent + V(0, -0.04f, 0), vent + V(0, 0.05f, 0), 0.05f, 0.045f, grey);
            var smoke = Rgb(196, 186, 168);
            // The smoke rises from the vent and billows out to one side in a cloud of round puffs
            b.Ell(Body, vent + V(0.01f, 0.08f, 0), V(0.045f, 0.04f, 0.045f), smoke, mat: Fur, blend: 0.02f);
            var cloud = vent + V(0.1f, 0.17f, -0.04f);
            b.Ell(Body, cloud, V(0.1f, 0.06f, 0.075f), smoke, mat: Fur, blend: 0.02f);
            foreach (var (o, k) in new[] { (V(-0.07f, 0.0f, 0.02f), 0.055f), (V(0.0f, 0.045f, 0.0f), 0.06f), (V(0.08f, 0.03f, -0.01f), 0.055f), (V(0.14f, -0.01f, 0.01f), 0.045f), (V(-0.03f, 0.04f, -0.05f), 0.045f), (V(0.06f, -0.03f, 0.05f), 0.045f), (V(0.04f, 0.08f, -0.02f), 0.04f) })
                b.Ell(Body, cloud + o, V(k, k * 0.85f, k), smoke, mat: Fur, blend: 0.02f);
        }
        // The head: a grey helmet with a ridge and a horn at each side, over a dark face ringed with studs
        int head = b.Head(bc + V(0, 0.02f, 0.15f));
        var c = bc + V(0, mega ? 0.08f : 0.06f, mega ? 0.2f : 0.2f);
        b.Ell(head, c + V(0, 0.05f, -0.01f), V(0.13f, 0.08f, 0.11f), grey);
        b.Ell(head, c + V(0, 0.115f, -0.02f), V(0.03f, 0.04f, 0.085f), grey, blend: 0.02f);
        PokeBuilder.Both(s => b.Spike(head, c + V(0.1f * s, 0.06f, -0.02f), c + V(0.2f * s, 0.075f, -0.06f), 0.05f, grey, 0.8f));
        var fc = c + V(0, -0.03f, 0.055f);
        var fr = V(0.105f, 0.065f, 0.07f);
        b.Ell(head, fc, fr, mega ? Rgb(250, 196, 64) : dark, mat: mega ? Glow : Shell);
        for (int i = -3; i <= 3; i++)
        {
            float a = i * 22f * Degree;
            var root = fc + V(MathF.Sin(a) * fr.X * 0.95f, -MathF.Cos(a) * fr.Y * 0.8f, MathF.Cos(a) * 0.04f);
            b.Ell(head, root, V(0.018f, 0.022f, 0.016f), grey, V(0, 0, -i * 22f), blend: 0.006f);
        }
        PokeBuilder.Both(s =>
        {
            var at = Out(fc, fr, default, V(0.42f * s, 0.32f, 0.85f));
            b.Eye(head, at, Outward(fc, fr, at), 0.016f, Rgb(246, 132, 36), glare: true);
        });
        return b;
    }

    private static PokeBuilder Heatran() => HeatranBuild(false);

    // ------------------------------------------------------------------ Regigigas

    /// <summary>
    /// Regigigas: a colossus of white, a great dome of a body with no neck, a band of gold set with black dots over its
    /// back and crown, six coloured dots in pairs on its face (red, cyan, grey), long arms striped black with gold discs
    /// on the shoulders and gold cuffs at the wrists, black fingers, short striped legs and moss grown over its feet and
    /// shoulders.
    /// </summary>
    private static PokeBuilder Regigigas()
    {
        var b = new PokeBuilder("Regigigas", 1f, BodyPlan.Biped, V(0, 0.5f, 0)) { Coat = Shell };
        var white = Rgb(238, 238, 232);
        var black = Rgb(40, 40, 46);
        var gold = Rgb(230, 196, 102);
        var moss = Rgb(66, 136, 78);
        var mossLight = Rgb(110, 176, 100);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.33f, 0));
            var ankle = V(0.12f * s, 0.1f, 0.01f);
            b.Limb(leg, V(0.1f * s, 0.35f, 0), ankle, 0.072f, 0.062f, white);
            foreach (float y in new[] { 0.29f, 0.21f })
                b.PaintTorus(leg, V(0.11f * s, y, 0.005f), 0.068f, 0.013f, black, V(0, 0, -5f * s));
            // Moss over the foot
            b.Ell(leg, V(0.12f * s, 0.055f, 0.02f), V(0.08f, 0.06f, 0.085f), moss, mat: Leaf);
            Lumps(b, leg, V(0.12f * s, 0.06f, 0.02f), V(0.08f, 0.055f, 0.085f), 14, 0.03f, moss, mossLight, 1f);
        });
        var bc = V(0, 0.52f, 0);
        var br = V(0.17f, 0.2f, 0.13f);
        b.Ell(Body, bc, br, white);
        // The crown, part of the body's dome
        var crown = bc + V(0, 0.17f, -0.005f);
        b.Ell(Body, crown, V(0.12f, 0.09f, 0.11f), white, blend: 0.05f);
        int head = b.Head(bc + V(0, 0.16f, 0));
        b.Ell(head, crown + V(0, 0.02f, 0), V(0.1f, 0.07f, 0.095f), white, blend: 0.03f);
        // The gold band from the small of the back over the crown to the brow, set with black dots
        var band = Smooth(4, bc + V(0, -0.08f, -0.11f), bc + V(0, 0.1f, -0.13f), crown + V(0, 0.08f, -0.06f), crown + V(0, 0.1f, 0.04f), crown + V(0, 0.04f, 0.1f));
        b.Tube(head, band, 0.05f, 0.04f, gold, Metal, 0.01f);
        for (int i = 1; i < band.Length - 1; i += 2)
            PokeBuilder.Both(s => b.PaintEll(head, band[i] + Vector3.Normalize(band[i] - bc) * 0.04f + V(0.022f * s, 0, 0), V(0.011f, 0.011f, 0.011f), black, soft: 0.005f));
        // Black marks scattered over the white
        foreach (var (dir, roll) in new[] { (V(0.85f, 0.2f, 0.45f), 80f), (V(-0.85f, 0.2f, 0.45f), -80f), (V(0.6f, -0.6f, 0.5f), 30f), (V(-0.6f, -0.6f, 0.5f), -30f), (V(0.4f, 0.75f, 0.5f), 0f), (V(-0.4f, 0.75f, 0.5f), 0f) })
            b.PaintEll(Body, Out(bc, br, default, dir), V(0.03f, 0.011f, 0.03f), black, V(0, 0, roll), 0.006f);
        // Six dots in pairs on the face: red, cyan and grey, each ringed dark
        var dots = new[] { Rgb(232, 82, 70), Rgb(84, 200, 204), Rgb(160, 160, 166) };
        for (int row = 0; row < 3; row++)
            PokeBuilder.Both(s =>
            {
                var at = On(bc, br, 0.04f * s, bc.Y + 0.12f - row * 0.065f);
                b.Mark(Body, at, Outward(bc, br, at), 0.017f, 0.02f, Rgb(70, 66, 70), MarkShape.Ring);
                b.Mark(Body, at, Outward(bc, br, at), 0.012f, 0.015f, dots[row]);
            });
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.19f * s, 0.6f, 0);
            int arm = b.Arm(s, shoulder);
            var elbow = V(0.27f * s, 0.43f, 0.03f);
            var wrist = V(0.31f * s, 0.27f, 0.06f);
            b.Limb(arm, shoulder + V(-0.04f * s, 0, 0), elbow, 0.06f, 0.05f, white);
            b.Limb(arm, elbow, wrist, 0.05f, 0.046f, white);
            foreach (float t in new[] { 0.35f, 0.75f })
                b.PaintEll(arm, Vector3.Lerp(shoulder, elbow, t), V(0.065f, 0.016f, 0.065f), black, Euler(elbow - shoulder), 0.006f);
            b.PaintEll(arm, Vector3.Lerp(elbow, wrist, 0.45f), V(0.06f, 0.016f, 0.06f), black, Euler(wrist - elbow), 0.006f);
            // The gold disc on the shoulder, moss on it, and the gold cuff round the wrist
            b.Ell(arm, shoulder + V(0.03f * s, 0.01f, 0), V(0.045f, 0.1f, 0.1f), gold, V(0, 0, -15f * s), Metal);
            b.Ell(arm, shoulder + V(0.06f * s, 0.01f, 0), V(0.02f, 0.06f, 0.06f), PixelCanvas.Mix(gold, Black, 0.15f), V(0, 0, -15f * s), Metal, 0.01f);
            b.Ell(Body, V(0.11f * s, 0.69f, -0.02f), V(0.045f, 0.03f, 0.05f), moss, mat: Leaf, blend: 0.015f);
            b.Torus(arm, Vector3.Lerp(elbow, wrist, 0.85f), 0.07f, 0.028f, gold, Euler(wrist - elbow), mat: Metal, blend: 0.01f);
            // A broad hand, white, with black fingers
            var hand = wrist + V(0.01f * s, -0.06f, 0.01f);
            b.Ell(arm, hand, V(0.045f, 0.05f, 0.04f), white);
            for (int i = -1; i <= 1; i++)
                b.Limb(arm, hand + V(0, -0.02f, 0.025f * i), hand + V(0.015f * s, -0.1f, 0.035f * i), 0.016f, 0.014f, i == 0 ? black : white, blend: 0.008f);
            b.Limb(arm, hand + V(-0.03f * s, 0, 0.02f), hand + V(-0.06f * s, -0.06f, 0.03f), 0.015f, 0.013f, black, blend: 0.008f);
        });
        return b;
    }

    // ------------------------------------------------------------------ Cresselia

    /// <summary>
    /// Cresselia: a swan of moonlight floating with its neck held high, its body pale blue over a hull of violet, a gold
    /// crescent rising behind its small head and a mask of pink round its eye, and on each side two great rings of
    /// pink like ribbons of light for wings.
    /// </summary>
    private static PokeBuilder Cresselia()
    {
        var b = new PokeBuilder("Cresselia", 0.95f, BodyPlan.Floating, V(0, 0.4f, 0)) { Coat = Fur }.Hover();
        var blue = Rgb(150, 186, 232);
        var violet = Rgb(176, 120, 170);
        var gold = Rgb(244, 208, 120);
        var pink = Rgb(242, 168, 200);
        var bc = V(0, 0.38f, -0.04f);
        var br = V(0.1f, 0.085f, 0.22f);
        b.Ell(Body, bc, br, blue, V(-8f, 0, 0));
        // The hull below: violet, drawn out to a point behind, edged gold
        b.Ell(Body, bc + V(0, -0.04f, -0.04f), V(0.075f, 0.05f, 0.22f), violet, V(-8f, 0, 0), blend: 0.02f);
        b.Spike(Body, bc + V(0, -0.02f, -0.15f), bc + V(0, 0.02f, -0.36f), 0.05f, violet, 0.5f);
        b.PaintEll(Body, bc + V(0, -0.075f, 0.0f), V(0.05f, 0.025f, 0.2f), gold, V(-8f, 0, 0));
        // The neck rises in a curve to a small head with a pointed beak
        var neck = Smooth(3, bc + V(0, 0.04f, 0.16f), bc + V(0, 0.16f, 0.22f), bc + V(0, 0.3f, 0.21f), bc + V(0, 0.38f, 0.2f));
        b.Tube(Body, neck, 0.045f, 0.03f, blue, blend: 0f);
        int head = b.Head(neck[^2]);
        var c = neck[^1] + V(0, 0.02f, 0.01f);
        var r = V(0.04f, 0.04f, 0.05f);
        b.Ell(head, c, r, blue);
        b.Spike(head, c + V(0, -0.005f, 0.035f), c + V(0, -0.02f, 0.11f), 0.022f, gold, 0.7f, Shell);
        // The crescent: a horn of gold rising from the back of the head and curving forward over it
        var horn = Smooth(4, c + V(0, 0.01f, -0.03f), c + V(0, 0.09f, -0.08f), c + V(0, 0.19f, -0.05f), c + V(0, 0.25f, 0.03f), c + V(0, 0.26f, 0.1f));
        b.Tube(head, horn, 0.03f, 0.008f, gold, Shell, 0.004f);
        PokeBuilder.Both(s =>
        {
            var at = Out(c, r, default, V(0.75f * s, 0.15f, 0.65f));
            b.PaintEll(head, at, V(0.022f, 0.02f, 0.022f), pink, soft: 0.006f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(200, 60, 140));
        });
        // Two rings of pink light on each side, one rising and swept back, one dipping forward
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, bc + V(0.07f * s, 0.03f, 0.02f));
            var up = Vector3.Normalize(V(0.75f * s, 0.55f, -0.35f));
            var down = Vector3.Normalize(V(0.8f * s, -0.45f, 0.25f));
            foreach (var (dir, ring, back) in new[] { (up, 0.16f, V(0, 0.45f, -0.9f)), (down, 0.12f, V(0, 0.2f, 1f)) })
            {
                var center = bc + V(0.05f * s, 0.02f, 0.02f) + dir * ring;
                var axis = Vector3.Normalize(Vector3.Cross(dir, back));
                b.Torus(wing, center, ring, 0.022f, pink, Euler(axis), sx: 1f, sz: 0.6f, mat: Glow, blend: 0.01f);
            }
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Phione

    /// <summary>
    /// Phione: a little sea drifter of pale blue, a big round head drawn up to a point and a long feeler from it trailing
    /// back, three dark blue dots over its big eyes, a red dot on its chest, stubby flippers for arms and feet.
    /// </summary>
    private static PokeBuilder Phione()
    {
        var b = new PokeBuilder("Phione", 0.5f, BodyPlan.Biped, V(0, 0.13f, 0)) { Coat = Fur };
        var blue = Rgb(112, 200, 230);
        var dark = Rgb(42, 104, 188);
        var red = Rgb(232, 76, 76);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.045f * s, 0.07f, 0.01f));
            b.Ell(leg, V(0.05f * s, 0.035f, 0.035f), V(0.035f, 0.035f, 0.055f), blue, V(0, 15f * s, 0));
        });
        b.Ell(Body, V(0, 0.12f, 0), V(0.06f, 0.07f, 0.055f), blue);
        b.Ell(Body, V(0, 0.13f, 0.052f), V(0.016f, 0.016f, 0.008f), red, mat: Glow, blend: 0.005f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.05f * s, 0.15f, 0.01f));
            b.Ell(arm, V(0.1f * s, 0.12f, 0.02f), V(0.055f, 0.02f, 0.032f), blue, V(0, 0, 30f * s));
        });
        // The round head, drawn up to a point, and the long feeler from its tip trailing back
        int head = b.Head(V(0, 0.18f, 0));
        var c = V(0, 0.26f, 0.01f);
        var r = V(0.095f, 0.085f, 0.085f);
        b.Ell(head, c, r, blue);
        b.Spike(head, c + V(0, 0.04f, -0.01f), c + V(0, 0.15f, -0.03f), 0.06f, blue);
        var feeler = Smooth(3, c + V(0, 0.14f, -0.03f), c + V(0, 0.18f, -0.1f), c + V(0.04f, 0.14f, -0.2f), c + V(0.08f, 0.04f, -0.26f), c + V(0.1f, -0.1f, -0.28f));
        b.Tube(head, feeler, 0.013f, 0.01f, blue, blend: 0f);
        Frond(b, head, feeler[^1] + V(0, 0.03f, 0), feeler[^1] + V(0.01f, -0.06f, -0.01f), 0.02f, blue, V(1f, 0, 0), 0.3f);
        foreach (float x in new[] { -0.045f, 0f, 0.045f })
        {
            var dot = On(c, r, x, c.Y + (x == 0f ? 0.055f : 0.04f));
            b.Mark(head, dot, Outward(c, r, dot), 0.013f, 0.013f, dark);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.042f * s, c.Y - 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.026f, Rgb(40, 84, 190));
        });
        b.Mark(head, On(c, r, 0, c.Y - 0.05f), V(0, -0.2f, 1f), 0.013f, 0.006f, Rgb(40, 60, 110), MarkShape.Smile);
        return b;
    }

    // ------------------------------------------------------------------ Darkrai

    /// <summary>
    /// Darkrai's head: small and black, a ruff of red round the neck, its white plume swept up and over to one side,
    /// hiding one eye; the other glows <paramref name="eye"/>. The Mega's plume stands straight up like a flame.
    /// </summary>
    private static void DarkraiHead(PokeBuilder b, int head, Vector3 c, Color black, Color eye, bool plumeUp)
    {
        var white = Rgb(236, 236, 240);
        var r = V(0.05f, 0.055f, 0.055f);
        b.Ell(head, c, r, black);
        var plume = plumeUp
            ? Smooth(3, c + V(-0.01f, 0.03f, -0.01f), c + V(-0.02f, 0.12f, -0.03f), c + V(0.01f, 0.22f, -0.05f), c + V(-0.01f, 0.31f, -0.04f))
            : Smooth(3, c + V(0.0f, 0.03f, 0.0f), c + V(-0.05f, 0.12f, -0.03f), c + V(-0.14f, 0.17f, -0.05f), c + V(-0.25f, 0.15f, -0.05f));
        b.Tube(head, plume, 0.06f, 0.02f, white, blend: 0f);
        // Its hem torn into points along the underside
        for (int i = 2; i < plume.Length - 1; i += 2)
        {
            var under = plumeUp ? V(-0.06f, 0.0f, 0) : V(0.02f, -0.06f, 0);
            b.Spike(head, plume[i], plume[i] + under * (1.3f - i / (float)plume.Length * 0.5f), 0.025f, white, 0.5f, blend: 0.006f);
        }
        // The plume falls over the left of the face down to the cheek, hiding that eye
        if (!plumeUp) b.Ell(head, c + V(-0.03f, 0.0f, 0.03f), V(0.03f, 0.05f, 0.03f), white, blend: 0.01f);
        var at = Out(c, r, default, V(0.45f, 0.1f, 0.88f));
        b.Eye(head, at, Outward(c, r, at), 0.017f, eye, glare: true);
    }

    /// <summary>
    /// Darkrai and its Mega Evolution. Darkrai floats like a shadow, a black figure whose lower body flares into a cloak
    /// torn into points, a red ruff of spikes round its neck, thin arms trailing ragged black like smoke. The Mega has
    /// spread itself flat: a body lying along the dark, ragged trails of smoke streaming out to each side, its plume
    /// standing up and its one eye pink.
    /// </summary>
    private static PokeBuilder DarkraiBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Darkrai-Mega" : "Darkrai", 1f, BodyPlan.Floating, V(0, mega ? 0.3f : 0.5f, 0)) { Coat = Fur }.Hover();
        var black = Rgb(48, 46, 56);
        var smoke = Rgb(70, 66, 78);
        var red = Rgb(186, 56, 60);
        if (mega)
        {
            var bc = V(0, 0.3f, 0);
            b.Ell(Body, bc, V(0.12f, 0.07f, 0.08f), black);
            PokeBuilder.Both(s =>
            {
                // Ragged trails of smoke streaming out to the side, kinked like lightning
                for (int k = 0; k < 3; k++)
                {
                    float y0 = 0.02f - 0.025f * k, z0 = 0.03f - 0.03f * k;
                    var trail = new[]
                    {
                        bc + V(0.06f * s, y0, z0), bc + V(0.16f * s, y0 + 0.05f, z0 - 0.01f), bc + V(0.24f * s, y0 - 0.03f, z0 + 0.02f),
                        bc + V(0.33f * s, y0 + 0.04f, z0), bc + V(0.42f * s, y0 - 0.02f, z0 - 0.02f), bc + V(0.48f * s, y0 + 0.03f, z0)
                    };
                    int part = b.Part(s < 0 ? "trailL" + k : "trailR" + k, Body, trail[0], PokeRole.Wing, k * 0.7f, s);
                    b.Tube(part, trail, 0.035f - 0.006f * k, 0.008f, k == 1 ? smoke : black, blend: 0.012f);
                    for (int i = 1; i < trail.Length - 1; i += 2)
                        b.Spike(part, trail[i], trail[i] + V(0.03f * s, k == 1 ? 0.06f : -0.06f, 0), 0.016f, black, 0.5f, blend: 0.006f);
                }
            });
            b.Ell(Body, bc + V(0, 0.05f, 0.05f), V(0.07f, 0.04f, 0.05f), red, blend: 0.02f);
            int head = b.Head(bc + V(0, 0.07f, 0.06f));
            b.Limb(head, bc + V(0, 0.05f, 0.04f), bc + V(0, 0.1f, 0.08f), 0.04f, 0.035f, black);
            DarkraiHead(b, head, bc + V(0, 0.12f, 0.09f), black, Rgb(236, 120, 170), true);
            return Lift(b);
        }
        // The cloak below the waist, torn into long points
        var hip = V(0, 0.36f, 0);
        b.Ell(Body, hip, V(0.1f, 0.13f, 0.085f), black);
        for (int i = 0; i < 9; i++)
        {
            float a = i * MathF.Tau / 9f + 0.2f;
            var root = hip + V(MathF.Sin(a) * 0.08f, -0.06f, MathF.Cos(a) * 0.065f);
            float len = i % 2 == 0 ? 1f : 0.7f;
            b.Spike(Body, root, root + V(MathF.Sin(a) * 0.16f, -0.24f * len, MathF.Cos(a) * 0.11f), 0.055f, black, 0.4f, blend: 0.015f);
        }
        // The chest, the ruff of red spikes round the neck
        var chest = V(0, 0.55f, 0);
        b.Ell(Body, chest, V(0.08f, 0.12f, 0.065f), black);
        b.Ell(Body, chest + V(0, 0.09f, 0.02f), V(0.085f, 0.04f, 0.075f), red, blend: 0.02f);
        foreach (var (x, len) in new[] { (-0.05f, 0.07f), (0f, 0.1f), (0.05f, 0.07f) })
            b.Spike(Body, chest + V(x, 0.08f, 0.06f), chest + V(x * 1.3f, 0.08f - len, 0.08f), 0.03f, red, 0.5f, blend: 0.01f);
        PokeBuilder.Both(s => b.Spike(Body, chest + V(0.07f * s, 0.1f, 0.0f), chest + V(0.14f * s, 0.09f, -0.02f), 0.03f, red, 0.5f, blend: 0.01f));
        int neckHead = b.Head(chest + V(0, 0.12f, 0.01f));
        b.Limb(neckHead, chest + V(0, 0.1f, 0.01f), chest + V(0, 0.17f, 0.03f), 0.035f, 0.035f, black);
        DarkraiHead(b, neckHead, chest + V(0, 0.2f, 0.04f), black, Rgb(84, 214, 220), false);
        PokeBuilder.Both(s =>
        {
            var shoulder = chest + V(0.07f * s, 0.06f, 0);
            int arm = b.Arm(s, shoulder);
            var elbow = V(0.17f * s, 0.46f, 0.03f);
            var hand = V(0.22f * s, 0.33f, 0.08f);
            b.Limb(arm, shoulder, elbow, 0.03f, 0.024f, black);
            b.Limb(arm, elbow, hand, 0.024f, 0.02f, black);
            // Ragged black streaming back from the arm like smoke
            foreach (var (t, d) in new[] { (0.3f, V(0.1f * s, 0.03f, -0.05f)), (0.65f, V(0.1f * s, -0.04f, -0.04f)) })
                b.Spike(arm, Vector3.Lerp(shoulder, elbow, t), Vector3.Lerp(shoulder, elbow, t) + d, 0.03f, smoke, 0.4f, blend: 0.01f);
            b.Spike(arm, Vector3.Lerp(elbow, hand, 0.5f), Vector3.Lerp(elbow, hand, 0.5f) + V(0.1f * s, -0.02f, -0.03f), 0.026f, smoke, 0.4f, blend: 0.01f);
            for (int i = -1; i <= 1; i++)
                b.Spike(arm, hand, hand + V((0.02f + 0.01f * i) * s, -0.06f, 0.025f * i), 0.012f, black, blend: 0.004f);
        });
        return Lift(b);
    }

    private static PokeBuilder Darkrai() => DarkraiBuild(false);

    // ------------------------------------------------------------------ Shaymin

    /// <summary>
    /// Shaymin in its Land Forme: a little hedgehog of white under a mound of grass that covers its back, two yellow
    /// dots in the grass, a pink flower of five petals on the side of its head, green eyes and a happy open mouth.
    /// </summary>
    private static PokeBuilder Shaymin()
    {
        var b = new PokeBuilder("Shaymin", 0.5f, BodyPlan.Quadruped, V(0, 0.13f, 0)) { Coat = Fur };
        var white = Rgb(236, 240, 244);
        var green = Rgb(112, 192, 92);
        var light = Rgb(150, 214, 120);
        var pink = Rgb(244, 168, 180);
        foreach (var (z, front) in new[] { (0.06f, true), (-0.07f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.06f * s, 0.07f, z), front);
                b.Limb(leg, V(0.06f * s, 0.08f, z), V(0.065f * s, 0.02f, z + 0.01f), 0.028f, 0.026f, white);
            });
        var bc = V(0, 0.12f, -0.01f);
        b.Ell(Body, bc, V(0.11f, 0.09f, 0.12f), white);
        // The mound of grass over its back, in tufts, falling over its sides
        var gc = V(0, 0.18f, -0.035f);
        var gr = V(0.135f, 0.1f, 0.13f);
        b.Ell(Body, gc, gr, green, mat: Leaf, blend: 0.02f);
        Lumps(b, Body, gc + V(0, 0.01f, 0), gr * V(1f, 0.9f, 1f), 26, 0.03f, green, light, 0.15f);
        PokeBuilder.Both(s => b.PaintEll(Body, Out(gc, gr, default, V(0.25f * s, 0.9f, 0.3f)), V(0.012f, 0.01f, 0.012f), Rgb(246, 210, 70), soft: 0.005f));
        // A head at the front under the grass's brim, the flower on its side
        int head = b.Head(bc + V(0, 0.02f, 0.08f));
        var c = V(0, 0.13f, 0.1f);
        var r = V(0.07f, 0.06f, 0.055f);
        b.Ell(head, c, r, white);
        FlowerHead(b, head, c + V(0.065f, 0.065f, 0.0f), V(0.6f, 0.3f, 0.75f), 5, 0.058f, 0.017f, pink, Rgb(214, 104, 128), 0.011f, 0.25f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.035f * s, c.Y + 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(64, 170, 96));
        });
        b.Mark(head, On(c, r, 0, c.Y - 0.022f), V(0, -0.25f, 1f), 0.014f, 0.01f, Rgb(200, 90, 100), MarkShape.Smile);
        return b;
    }

    // ------------------------------------------------------------------ Arceus

    /// <summary>
    /// The plate each of Arceus's forms holds: its type after "Arceus-" (null for the Normal type) and the colour its
    /// grey parts and the gems on its wheel take.
    /// </summary>
    private static readonly (string? Type, Color Under, Color Gem)[] ArceusPlates =
    {
        (null, Rgb(150, 152, 164), Rgb(64, 182, 112)),
        ("Fighting", Rgb(196, 70, 52), Rgb(232, 120, 96)),
        ("Flying", Rgb(150, 170, 236), Rgb(196, 214, 250)),
        ("Poison", Rgb(158, 74, 170), Rgb(206, 128, 216)),
        ("Ground", Rgb(214, 178, 96), Rgb(240, 214, 140)),
        ("Rock", Rgb(176, 150, 72), Rgb(214, 192, 120)),
        ("Bug", Rgb(156, 186, 40), Rgb(200, 224, 90)),
        ("Ghost", Rgb(110, 86, 156), Rgb(160, 136, 206)),
        ("Steel", Rgb(176, 180, 204), Rgb(216, 220, 236)),
        ("Fire", Rgb(238, 120, 48), Rgb(250, 180, 90)),
        ("Water", Rgb(90, 138, 236), Rgb(150, 190, 250)),
        ("Grass", Rgb(110, 190, 78), Rgb(160, 226, 120)),
        ("Electric", Rgb(246, 206, 50), Rgb(252, 236, 120)),
        ("Psychic", Rgb(244, 92, 140), Rgb(250, 150, 186)),
        ("Ice", Rgb(140, 212, 220), Rgb(196, 240, 244)),
        ("Dragon", Rgb(110, 64, 236), Rgb(160, 126, 250)),
        ("Dark", Rgb(98, 78, 68), Rgb(150, 126, 110)),
        ("Fairy", Rgb(238, 150, 190), Rgb(250, 196, 222))
    };

    /// <summary>
    /// Arceus holding one of its plates (an index into <see cref="ArceusPlates"/>): a tall beast of white like a horse
    /// of the heavens, grey beneath and on its face, a long neck, a great crest sweeping back from its head, slender legs
    /// on gold hooves, a long tail, and round its middle a golden wheel with four arms reaching out past its rim, a green
    /// gem where each crosses it. A plate turns the grey and the gems to its type's colours.
    /// </summary>
    private static PokeBuilder ArceusBuild(int plate)
    {
        var (type, under, gem) = ArceusPlates[plate];
        var b = new PokeBuilder(type == null ? "Arceus" : "Arceus-" + type, 1f, BodyPlan.Quadruped, V(0, 0.4f, -0.02f)) { Coat = Fur };
        var white = Rgb(240, 240, 236);
        var gold = Rgb(232, 200, 108);
        foreach (var (z, front) in new[] { (0.13f, true), (-0.15f, false) })
            PokeBuilder.Both(s =>
            {
                var hip = V(0.065f * s, 0.36f, z);
                int leg = b.Leg(s, hip, front);
                var knee = V(0.07f * s, 0.21f, z + (front ? 0.02f : -0.05f));
                var ankle = V(0.07f * s, 0.07f, z + 0.01f);
                b.Limb(leg, hip + V(0, 0.02f, 0), knee, 0.045f, 0.028f, white);
                b.Limb(leg, knee, ankle, 0.024f, 0.02f, under);
                // A gold hoof to a point, and a gold spur at the back of the fetlock
                b.Spike(leg, ankle + V(0, 0.02f, 0), V(0.07f * s, 0f, z + 0.025f), 0.03f, gold, mat: Metal, blend: 0.01f);
                b.Spike(leg, knee + V(0, -0.02f, -0.01f), knee + V(0, 0.0f, -0.07f), 0.016f, under, blend: 0.006f);
            });
        var bc = V(0, 0.4f, -0.02f);
        var br = V(0.1f, 0.095f, 0.21f);
        b.Ell(Body, bc, br, white);
        b.PaintEll(Body, bc + V(0, -0.08f, 0), V(0.08f, 0.04f, 0.17f), under);
        // The wheel round its middle, tipped up at the front, four arms crossing it with a gem at each crossing
        var n = Vector3.Normalize(V(0, 0.34f, 0.94f));
        var u = V(1f, 0, 0);
        var w = Vector3.Cross(n, u);
        var hub = bc + V(0, 0.01f, -0.01f);
        b.Torus(Body, hub, 0.19f, 0.012f, gold, Euler(n), mat: Metal, blend: 0.004f);
        foreach (float deg in new[] { 45f, 135f, 225f, 315f })
        {
            float a = deg * Degree;
            var dir = u * MathF.Cos(a) + w * MathF.Sin(a);
            var at = hub + dir * 0.19f;
            b.Limb(Body, hub + dir * 0.06f, at, 0.013f, 0.011f, gold, Metal, 0.006f);
            b.Spike(Body, at, at + dir * 0.09f + n * 0.02f, 0.013f, gold, mat: Metal, blend: 0.004f);
            Gem(b, Body, at + n * 0.012f, n, 0.016f, gem);
        }
        // The long neck, the head with a grey face, and the crest sweeping back over the neck
        var neck = Smooth(3, bc + V(0, 0.04f, 0.15f), bc + V(0, 0.2f, 0.21f), bc + V(0, 0.33f, 0.2f));
        b.Tube(Body, neck, 0.055f, 0.038f, white, blend: 0f);
        int head = b.Head(neck[^1]);
        var c = neck[^1] + V(0, 0.03f, 0.04f);
        var r = V(0.046f, 0.05f, 0.078f);
        b.Limb(head, neck[^1] + V(0, -0.01f, 0), c, 0.04f, 0.038f, white);
        b.Ell(head, c, r, white, V(15f, 0, 0));
        b.PaintEll(head, c + V(0, -0.005f, 0.05f), V(0.045f, 0.04f, 0.045f), under, V(15f, 0, 0));
        var crest = Smooth(4, c + V(0, 0.03f, -0.03f), c + V(0, 0.12f, -0.07f), c + V(0, 0.19f, -0.15f), c + V(0, 0.2f, -0.26f), c + V(0, 0.15f, -0.34f));
        b.Tube(head, crest, 0.038f, 0.008f, white, blend: 0.006f);
        for (int i = 1; i < crest.Length - 1; i++)
            b.PaintEll(head, crest[i] + V(0, -0.02f, 0.012f), V(0.012f, 0.012f, 0.025f), under, Euler(crest[i + 1] - crest[i - 1]), 0.006f);
        PokeBuilder.Both(s =>
        {
            // A point back from each cheek like an ear, and eyes of green
            b.Spike(head, c + V(0.03f * s, 0.01f, -0.03f), c + V(0.07f * s, 0.0f, -0.11f), 0.018f, white, blend: 0.006f);
            var at = Out(c, r, V(15f, 0, 0), V(0.65f * s, 0.25f, 0.7f));
            b.Eye(head, at, Out(c, r, V(15f, 0, 0), V(0.65f * s, 0.25f, 0.7f)) - c, 0.01f, Rgb(70, 190, 110), glare: true);
        });
        // A ridge of white over the shoulders, and the long tail sweeping down behind, grey at its end
        PokeBuilder.Both(s => b.Spike(Body, bc + V(0.05f * s, 0.06f, 0.1f), bc + V(0.09f * s, 0.12f, -0.02f), 0.03f, white, 0.5f));
        int tail = b.Tail(bc + V(0, 0.03f, -0.18f));
        var tp = Smooth(3, bc + V(0, 0.03f, -0.18f), bc + V(0, 0.02f, -0.3f), bc + V(0, -0.08f, -0.42f), bc + V(0, -0.2f, -0.46f));
        b.Tube(tail, tp, 0.04f, 0.014f, white, blend: 0f);
        b.PaintEll(tail, tp[^1], V(0.04f, 0.07f, 0.04f), under);
        PokeBuilder.Both(s => b.Spike(tail, tp[^3], tp[^3] + V(0.07f * s, -0.08f, -0.03f), 0.02f, white, 0.5f));
        return b;
    }

    private static PokeBuilder Arceus() => ArceusBuild(0);
}
