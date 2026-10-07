using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// The regional forms of the hand-built species, Dialga's and Palkia's Origin Formes, and the other forms that are neither
// Mega nor Gigantamax (Basculin's stripes, Darmanitan's Zen Modes, Deerling's and Sawsbuck's seasons, the female Frillish
// and Jellicent), hand-built like the species
// (after plan 03 · D11), in National Pokédex order: the Alolan Rattata line, Alolan Raichu, the Alolan Sandshrew,
// Vulpix and Diglett lines, Alolan and Galarian Meowth, Alolan Persian, the Hisuian Growlithe line, the Alolan Geodude
// line, Galarian Ponyta and Rapidash, the Galarian Slowpoke line, Galarian Farfetch'd, the Alolan Grimer line, Galarian
// Mr. Mime, Paldean Wooper and Hisuian Sneasel. Each is our own sculpt after the design.
internal static partial class PokemonModels
{
    // ------------------------------------------------------------------ The Alolan Rattata line

    private static PokeBuilder RattataAlola() => RattataBuild(true);

    private static PokeBuilder RaticateAlola() => RaticateBuild(true);

    // ------------------------------------------------------------------ Alolan Raichu

    private static PokeBuilder RaichuAlola()
    {
        var b = new PokeBuilder("Raichu-Alola", 0.72f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Fur }.Hover();
        var brown = Rgb(190, 116, 58);
        var cream = Rgb(250, 238, 212);
        var tail = Rgb(120, 72, 40);
        var yellow = Rgb(250, 218, 70);
        var white = Rgb(252, 250, 246);

        // It rides its own tail: the thin dark tail loops round behind it to the flat yellow board under its feet, which
        // stays level whatever the rest does
        var path = Smooth(3, V(0, 0.2f, -0.11f), V(0.0f, 0.12f, -0.27f), V(-0.12f, 0.08f, -0.36f), V(-0.22f, 0.05f, -0.26f), V(-0.2f, 0.02f, -0.12f), V(-0.1f, -0.002f, -0.12f));
        b.Tube(Root, path, 0.014f, 0.016f, tail);
        Frond(b, Root, V(-0.07f, -0.014f, -0.3f), V(0.08f, -0.014f, 0.28f), 0.16f, yellow, V(0, 1f, 0), 0.11f);
        Blade(b, Root, V(0.06f, -0.012f, 0.2f), V(0.17f, 0.07f, 0.4f), 0.065f, yellow, V(0, 1f, -0.4f), 0.3f);
        Blade(b, Root, V(-0.05f, -0.014f, -0.2f), V(-0.24f, -0.014f, -0.4f), 0.06f, yellow, V(0, 1f, 0), 0.3f);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.18f, 0));
            b.Limb(leg, V(0.07f * s, 0.18f, 0), V(0.09f * s, 0.05f, 0.02f), 0.05f, 0.04f, brown);
            b.Ell(leg, V(0.095f * s, 0.03f, 0.05f), V(0.05f, 0.03f, 0.075f), white);
        });
        // A pear of a body, cream from the chest down
        b.Ell(Body, V(0, 0.3f, 0), V(0.14f, 0.17f, 0.12f), brown);
        b.PaintEll(Body, V(0, 0.26f, 0.08f), V(0.105f, 0.14f, 0.08f), cream);
        // Short arms ending in white mitts dipped in yellow
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.11f * s, 0.38f, 0.03f));
            b.Limb(arm, V(0.11f * s, 0.38f, 0.03f), V(0.18f * s, 0.33f, 0.08f), 0.032f, 0.028f, brown);
            b.Ell(arm, V(0.195f * s, 0.32f, 0.09f), V(0.036f, 0.03f, 0.032f), white);
            b.PaintEll(arm, V(0.226f * s, 0.32f, 0.095f), V(0.013f, 0.032f, 0.032f), yellow);
        });

        int head = b.Head(V(0, 0.44f, 0));
        b.Ell(head, V(0, 0.53f, 0.01f), V(0.13f, 0.11f, 0.11f), brown);
        // Broad yellow ears, brown at the root, their tips curled round
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.08f * s, 0.6f, -0.01f));
            Petal(b, ear, V(0.08f * s, 0.6f, -0.01f), V(0.19f * s, 0.79f, -0.04f), 0.07f, yellow, Fur, 0.015f);
            b.PaintEll(ear, V(0.1f * s, 0.63f, -0.02f), V(0.045f, 0.05f, 0.04f), brown);
            Curl(b, ear, V(0.19f * s, 0.79f, -0.04f), V(0.205f * s, 0.75f, -0.04f), V(s, 0, 0), V(0, 1, 0), 0.025f, 1f, 0.016f, yellow);
        });
        // Tall yellow cheeks, a tiny nose and a smile, blue eyes
        PokeBuilder.Both(s => b.Mark(head, V(0.1f * s, 0.5f, 0.075f), V(0.75f * s, -0.05f, 0.65f), 0.02f, 0.032f, yellow));
        b.Mark(head, V(0, 0.515f, 0.119f), V(0, 0.1f, 1f), 0.011f, 0.008f, Rgb(70, 40, 30));
        b.Mark(head, V(0, 0.49f, 0.112f), V(0, -0.2f, 1f), 0.022f, 0.008f, Rgb(90, 40, 30), MarkShape.Wave);
        PokeBuilder.Both(s => b.Eye(head, V(0.05f * s, 0.55f, 0.1f), V(0.4f * s, 0.05f, 1f), 0.027f, Rgb(64, 150, 230)));
        return Lift(b);
    }

    // ------------------------------------------------------------------ The Alolan Sandshrew line

    private static PokeBuilder SandshrewAlola() => SandshrewBuild(true);

    private static PokeBuilder SandslashAlola() => SandslashBuild(true);

    // ------------------------------------------------------------------ The Alolan Vulpix line

    private static PokeBuilder VulpixAlola() => VulpixBuild(true);

    private static PokeBuilder NinetalesAlola() => NinetalesBuild(true);

    // ------------------------------------------------------------------ The Alolan Diglett line

    private static PokeBuilder DiglettAlola() => DiglettBuild(true);

    private static PokeBuilder DugtrioAlola() => DugtrioBuild(true);

    // ------------------------------------------------------------------ The Alolan and Galarian Meowth, Alolan Persian

    private static PokeBuilder MeowthAlola() => MeowthBuild(true, false);

    private static PokeBuilder MeowthGalar() => MeowthBuild(false, true);

    private static PokeBuilder PersianAlola() => PersianBuild(true);

    // ------------------------------------------------------------------ The Hisuian Growlithe line

    private static PokeBuilder GrowlitheHisui() => GrowlitheBuild(true);

    private static PokeBuilder ArcanineHisui() => ArcanineBuild(true);

    // ------------------------------------------------------------------ the Alolan Geodude line

    /// <summary>Black iron sand standing up off a rock like hair: thin spikes rising from <paramref name="roots"/>.</summary>
    private static void IronSand(PokeBuilder b, int bone, Vector3[] roots, Vector3 center, float length, float r)
    {
        var black = Rgb(40, 40, 46);
        foreach (var root in roots)
        {
            var out_ = Vector3.Normalize(root - center);
            b.Spike(bone, root, root + Vector3.Normalize(out_ + V(0, 1.4f, 0)) * length, r, black, mat: Shell);
        }
    }

    private static PokeBuilder GeodudeAlola()
    {
        var b = new PokeBuilder("Geodude-Alola", 0.56f, BodyPlan.Floating, V(0, 0.42f, 0)) { Coat = Shell }.Hover();
        var gray = Rgb(170, 172, 178);
        var black = Rgb(44, 44, 50);

        var c = V(0, 0.42f, 0);
        b.Ell(Body, c, V(0.17f, 0.15f, 0.15f), gray);
        Rubble(b, Body, V(0, 0.42f, -0.02f), V(0.16f, 0.14f, 0.13f), 0.05f, gray, 8, 76u);
        // Thick black brows over a scowl, and the iron sand standing up off its crown
        PokeBuilder.Both(s => b.Box(Body, V(0.058f * s, 0.478f, 0.128f), V(0.05f, 0.016f, 0.024f), 0.01f, black, V(-14f, 0, -14f * s), Shell, 0.004f));
        b.Mark(Body, V(0, 0.36f, 0.143f), V(0, -0.25f, 1f), 0.04f, 0.009f, Rgb(70, 68, 62), MarkShape.Bar);
        PokeBuilder.Both(s => b.Eye(Body, V(0.055f * s, 0.43f, 0.142f), V(0.3f * s, -0.05f, 1f), 0.022f, glare: true));
        IronSand(b, Body, new[] { V(-0.06f, 0.555f, 0.04f), V(-0.02f, 0.565f, 0.06f), V(0.03f, 0.565f, 0.05f), V(0.07f, 0.55f, 0.02f), V(-0.04f, 0.56f, -0.03f), V(0.02f, 0.565f, -0.02f) }, c, 0.09f, 0.012f);
        // Square arms of stone ending in blocky fists
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.15f * s, 0.42f, 0));
            b.Box(arm, V(0.23f * s, 0.41f, 0.02f), V(0.075f, 0.045f, 0.045f), 0.02f, gray, V(0, 0, -6f * s), Shell, 0.02f);
            b.Box(arm, V(0.32f * s, 0.46f, 0.06f), V(0.042f, 0.075f, 0.042f), 0.018f, gray, V(0, 0, 8f * s), Shell, 0.015f);
            b.Box(arm, V(0.335f * s, 0.575f, 0.08f), V(0.062f, 0.066f, 0.058f), 0.022f, gray, V(0, 0, -8f * s), Shell, 0.012f);
            b.Box(arm, V(0.335f * s, 0.6f, 0.136f), V(0.052f, 0.024f, 0.012f), 0.008f, gray, V(0, 0, -8f * s), Shell, 0.008f);
        });
        return Lift(b);
    }

    private static PokeBuilder GravelerAlola()
    {
        var b = new PokeBuilder("Graveler-Alola", 0.78f, BodyPlan.Biped, V(0, 0.4f, 0)) { Coat = Shell };
        var gray = Rgb(150, 150, 144);
        var amber = Rgb(250, 168, 44);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.12f * s, 0.18f, 0));
            b.Limb(leg, V(0.12f * s, 0.2f, 0), V(0.13f * s, 0.08f, 0.03f), 0.07f, 0.065f, gray);
            b.Ell(leg, V(0.13f * s, 0.05f, 0.04f), V(0.08f, 0.05f, 0.09f), gray);
        });
        var c = V(0, 0.4f, 0);
        var r = V(0.27f, 0.26f, 0.23f);
        b.Ell(Body, c, r, gray);
        Rubble(b, Body, V(0, 0.4f, -0.02f), V(0.26f, 0.25f, 0.21f), 0.09f, gray, 18, 77u);
        // Crystals glowing amber in the rock
        foreach (var dir in new[] { V(-0.7f, 0.45f, 0.55f), V(0.78f, 0.25f, 0.55f), V(0.55f, -0.45f, 0.7f), V(-0.45f, -0.35f, 0.82f), V(0.9f, 0.3f, -0.3f), V(-0.85f, -0.1f, -0.45f), V(0.2f, 0.6f, -0.75f), V(-0.3f, 0.8f, 0.1f) })
        {
            var n = Vector3.Normalize(dir);
            b.Ell(Body, c + n * r * 1.06f, V(0.034f, 0.034f, 0.02f), amber, Toward(n), Glow, 0.006f);
        }
        // A face in the rocks: a heavy brow, eyes, and a mouth gaping open
        b.Ell(Body, V(0, 0.5f, 0.2f), V(0.16f, 0.04f, 0.05f), gray, V(-12f, 0, 0));
        PokeBuilder.Both(s => b.Eye(Body, V(0.075f * s, 0.455f, 0.225f), V(0.3f * s, -0.05f, 1f), 0.03f, glare: true));
        b.Cut(Body, V(0, 0.34f, 0.25f), V(0.1f, 0.045f, 0.05f), blend: 0.01f);
        b.PaintEll(Body, V(0, 0.34f, 0.21f), V(0.11f, 0.05f, 0.06f), Rgb(120, 54, 60));
        b.PaintEll(Body, V(0, 0.32f, 0.21f), V(0.07f, 0.025f, 0.05f), Rgb(222, 124, 132));
        // Iron sand bristling off its crown
        IronSand(b, Body, new[] { V(-0.12f, 0.63f, 0.05f), V(-0.06f, 0.655f, 0.08f), V(0.0f, 0.66f, 0.07f), V(0.06f, 0.655f, 0.07f), V(0.12f, 0.635f, 0.04f), V(-0.09f, 0.65f, -0.03f), V(0.03f, 0.665f, -0.02f) }, c, 0.12f, 0.016f);
        // Four arms: a pair high, a pair low, three fingers on each hand, and more iron sand on the upper ones
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.22f * s, 0.48f, 0.05f));
            b.Limb(arm, V(0.22f * s, 0.48f, 0.05f), V(0.35f * s, 0.5f, 0.1f), 0.075f, 0.065f, gray);
            b.Ell(arm, V(0.4f * s, 0.49f, 0.13f), V(0.065f, 0.06f, 0.065f), gray);
            Digits(b, arm, V(0.44f * s, 0.48f, 0.15f), V(s, -0.3f, 0.6f), V(0, 0.6f, 0), 0.065f, 0.025f, gray);
            IronSand(b, arm, new[] { V(0.29f * s, 0.55f, 0.07f), V(0.33f * s, 0.555f, 0.09f), V(0.37f * s, 0.55f, 0.11f) }, V(0.3f * s, 0.49f, 0.08f), 0.08f, 0.012f);
            int lower = b.Part(s < 0 ? "lowArmL" : "lowArmR", Body, V(0.22f * s, 0.3f, 0.08f), PokeRole.Arm, s + 1.4f, s);
            b.Limb(lower, V(0.22f * s, 0.3f, 0.08f), V(0.34f * s, 0.24f, 0.14f), 0.07f, 0.06f, gray);
            b.Ell(lower, V(0.38f * s, 0.21f, 0.17f), V(0.06f, 0.055f, 0.06f), gray);
            Digits(b, lower, V(0.41f * s, 0.19f, 0.19f), V(s, -0.6f, 0.6f), V(0, 0.6f, 0), 0.06f, 0.023f, gray);
        });
        return b;
    }

    private static PokeBuilder GolemAlola()
    {
        var b = new PokeBuilder("Golem-Alola", 0.88f, BodyPlan.Biped, V(0, 0.5f, -0.04f)) { Coat = Fur };
        var shell = Rgb(124, 134, 112);
        var skin = Rgb(120, 102, 90);
        var coal = Rgb(64, 64, 72);
        var black = Rgb(40, 38, 42);
        var amber = Rgb(250, 168, 44);
        var red = Rgb(204, 64, 62);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.18f * s, 0.22f, 0.08f));
            b.Limb(leg, V(0.18f * s, 0.22f, 0.08f), V(0.2f * s, 0.07f, 0.14f), 0.085f, 0.075f, skin);
            b.Ell(leg, V(0.2f * s, 0.045f, 0.18f), V(0.085f, 0.045f, 0.1f), skin);
            Digits(b, leg, V(0.2f * s, 0.04f, 0.26f), V(0, 0, 1f), V(0.6f, 0, 0), 0.05f, 0.018f, Claw, Shell);
        });
        // The great shell of grey-green rock, cream horns at its sides, and on top the dark boulders it fires,
        // charged with amber
        b.Ell(Body, V(0, 0.52f, -0.06f), V(0.34f, 0.33f, 0.32f), shell, mat: Shell);
        Rubble(b, Body, V(0, 0.52f, -0.08f), V(0.33f, 0.32f, 0.3f), 0.11f, shell, 22, 78u);
        PokeBuilder.Both(s => b.Spike(Body, V(0.31f * s, 0.6f, 0.12f), V(0.44f * s, 0.68f, 0.2f), 0.05f, Claw, mat: Shell));
        var rock = V(-0.07f, 0.9f, -0.04f);
        var tilt = V(-16f, 0, 8f);
        b.Box(Body, rock, V(0.085f, 0.15f, 0.1f), 0.03f, coal, tilt, Shell, 0.02f);
        b.Box(Body, V(0.13f, 0.86f, -0.12f), V(0.075f, 0.11f, 0.09f), 0.03f, coal, V(-8f, 30f, -16f), Shell, 0.02f);
        var q = Quaternion.CreateFromYawPitchRoll(tilt.Y * Degree, tilt.X * Degree, tilt.Z * Degree);
        for (int i = 0; i < 3; i++)
            b.Ell(Body, rock + Vector3.Transform(V(0, 0.07f - 0.055f * i, 0.1f), q), V(0.022f, 0.02f, 0.01f), amber, Toward(Vector3.Transform(Vector3.UnitZ, q)), Glow, 0.004f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.3f * s, 0.52f, 0.12f));
            b.Limb(arm, V(0.3f * s, 0.52f, 0.12f), V(0.42f * s, 0.44f, 0.24f), 0.075f, 0.062f, skin);
            b.Ell(arm, V(0.44f * s, 0.42f, 0.28f), V(0.07f, 0.065f, 0.07f), skin);
            Digits(b, arm, V(0.45f * s, 0.41f, 0.33f), V(0.2f * s, -0.2f, 1f), V(0.6f, 0, 0), 0.05f, 0.018f, Claw, Shell);
        });

        // A dark face low in the shell, a beard of black iron sand, red eyes, an angry open mouth
        int head = b.Head(V(0, 0.42f, 0.22f));
        b.Ell(head, V(0, 0.45f, 0.33f), V(0.13f, 0.11f, 0.12f), skin);
        int jaw = b.Jaw(head, V(0, 0.4f, 0.3f));
        b.Ell(jaw, V(0, 0.355f, 0.39f), V(0.1f, 0.045f, 0.09f), skin, blend: 0.008f);
        b.Ell(jaw, V(0, 0.33f, 0.34f), V(0.12f, 0.06f, 0.1f), black, blend: 0.02f);
        foreach (var (x, z, len) in new[] { (-0.09f, 0.33f, 0.09f), (-0.05f, 0.38f, 0.12f), (0f, 0.4f, 0.13f), (0.05f, 0.38f, 0.12f), (0.09f, 0.33f, 0.09f), (-0.11f, 0.27f, 0.08f), (0.11f, 0.27f, 0.08f) })
            b.Spike(jaw, V(x, 0.31f, z), V(x * 1.3f, 0.31f - len, z + 0.03f), 0.022f, black);
        b.Ell(head, V(0, 0.395f, 0.4f), V(0.075f, 0.035f, 0.07f), red, blend: 0.006f);
        PokeBuilder.Both(s => b.Ell(head, V(0.06f * s, 0.53f, 0.38f), V(0.06f, 0.025f, 0.04f), skin, V(0, 0, -15f * s)));
        PokeBuilder.Both(s => b.Eye(head, V(0.062f * s, 0.49f, 0.43f), V(0.4f * s, 0.05f, 1f), 0.03f, red, glare: true));
        return b;
    }

    // ------------------------------------------------------------------ Galarian Ponyta and Rapidash

    /// <summary>A puff of pastel cloud (Galarian Ponyta's mane, tail and anklets): balls of pink, mint and lilac run together.</summary>
    private static void PastelPuff(PokeBuilder b, int bone, Vector3 at, float r, int turn)
    {
        var colors = new[] { Rgb(248, 184, 222), Rgb(160, 228, 214), Rgb(204, 186, 244) };
        var offsets = new[] { V(0, 0, 0), V(0.7f, 0.25f, -0.3f), V(-0.65f, 0.2f, -0.35f), V(0.1f, 0.6f, -0.5f), V(0.05f, -0.35f, -0.7f) };
        for (int i = 0; i < offsets.Length; i++)
            b.Ell(bone, at + offsets[i] * r, V(r, r * 0.9f, r) * (i == 0 ? 1f : 0.75f), colors[(i + turn) % 3], blend: 0.02f);
    }

    /// <summary>A long lock of pastel hair flowing along <paramref name="path"/> (Galarian Rapidash's mane and tail).</summary>
    private static void PastelLock(PokeBuilder b, int bone, Vector3[] path, float r, int turn)
    {
        var colors = new[] { Rgb(248, 184, 222), Rgb(160, 228, 214), Rgb(204, 186, 244) };
        b.Tube(bone, Smooth(3, path), r, r * 0.35f, colors[turn % 3], blend: 0f);
    }

    private static PokeBuilder HorseGalar(bool grown)
    {
        var b = new PokeBuilder(grown ? "Rapidash-Galar" : "Ponyta-Galar", grown ? 0.9f : 0.78f, BodyPlan.Quadruped, V(0, 0.5f, -0.02f)) { Coat = Fur };
        var white = Rgb(250, 248, 252);
        var hoof = Rgb(96, 70, 128);
        var lilac = Rgb(204, 186, 244);
        var violet = Rgb(124, 92, 178);
        float k = grown ? 1.12f : 1f;

        int t = 0;
        foreach (var (z, front) in new[] { (0.16f, true), (-0.2f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.07f * s, 0.42f, z * k), front);
                b.Limb(leg, V(0.07f * s, 0.42f, z * k), V(0.075f * s, 0.2f, (z + 0.01f) * k), 0.045f, 0.03f, white);
                b.Limb(leg, V(0.075f * s, 0.2f, (z + 0.01f) * k), V(0.075f * s, 0.05f, (z + 0.02f) * k), 0.03f, 0.028f, white);
                b.Ell(leg, V(0.075f * s, 0.03f, (z + 0.03f) * k), V(0.034f, 0.03f, 0.04f), hoof, mat: Shell);
                // A fluffy anklet of pastel cloud over each hoof
                PastelPuff(b, leg, V(0.075f * s, 0.075f, (z + 0.02f) * k), grown ? 0.034f : 0.03f, t++);
            });
        b.Ell(Body, V(0, 0.5f, -0.02f), V(0.12f, 0.12f, 0.25f * k), white);
        int tail = b.Tail(V(0, 0.56f, -0.24f * k));
        if (grown)
        {
            // A long tail of flowing pastel locks
            foreach (var (dx, i) in new[] { (0f, 0), (0.05f, 1), (-0.05f, 2), (0.02f, 3) })
                PastelLock(b, tail, new[] { V(0, 0.56f, -0.24f * k), V(dx, 0.5f, -0.4f), V(dx * 2f, 0.3f, -0.5f), V(dx * 2.5f + 0.02f, 0.12f, -0.56f), V(dx * 3f, 0.05f, -0.68f) }, 0.04f, i);
        }
        else
        {
            PastelPuff(b, tail, V(0, 0.6f, -0.3f), 0.08f, 0);
            PastelPuff(b, tail, V(0, 0.5f, -0.38f), 0.065f, 1);
        }

        int head = b.Head(V(0, 0.6f, 0.18f * k));
        b.Limb(head, V(0, 0.56f, 0.16f * k), V(0, 0.76f, 0.26f * k), 0.075f, 0.06f, white);
        b.Ell(head, V(0, 0.8f, 0.31f * k), V(0.07f, 0.07f, 0.11f), white);
        b.Ell(head, V(0, 0.76f, 0.39f * k), V(0.05f, 0.05f, 0.06f), white);
        PokeBuilder.Both(s => b.Mark(head, V(0.025f * s, 0.77f, 0.445f * k), V(0.3f * s, 0, 1f), 0.008f, 0.006f, Rgb(150, 130, 170)));
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.04f * s, 0.86f, 0.27f * k));
            b.Spike(ear, V(0.04f * s, 0.85f, 0.27f * k), V(0.06f * s, 0.94f, 0.24f * k), 0.025f, white, 0.5f);
            b.PaintEll(ear, V(0.055f * s, 0.92f, 0.25f * k), V(0.012f, 0.025f, 0.014f), lilac);
        });
        // A mane of pastel cloud down the neck (a grown one's flows long), and a horn: short on the foal, a long
        // spiral on the grown one
        int mane = b.Part("mane", head, V(0, 0.82f, 0.24f * k), PokeRole.Leaf);
        if (grown)
        {
            foreach (var (y, z, i) in new[] { (0.88f, 0.27f, 0), (0.8f, 0.23f, 1), (0.71f, 0.19f, 2), (0.62f, 0.15f, 0) })
                PokeBuilder.Both(s => PastelLock(b, mane, new[] { V(0.02f * s, y, z * k), V(0.07f * s, y - 0.06f, (z - 0.08f) * k), V(0.09f * s, y - 0.2f, (z - 0.14f) * k), V(0.08f * s, y - 0.34f, (z - 0.12f) * k) }, 0.035f, i + (s > 0 ? 1 : 0)));
            PastelLock(b, mane, new[] { V(0, 0.9f, 0.32f * k), V(0.02f, 0.86f, 0.4f * k), V(0.05f, 0.78f, 0.43f * k) }, 0.025f, 0);
            b.Spike(head, V(0, 0.85f, 0.34f * k), V(0, 1.08f, 0.47f * k), 0.028f, white, mat: Shell);
            var dir = Vector3.Normalize(V(0, 0.23f, 0.13f * k));
            for (int i = 0; i < 4; i++)
                b.PaintTorus(head, V(0, 0.85f, 0.34f * k) + dir * (0.05f + 0.045f * i), 0.024f - 0.005f * i, 0.005f, violet, Euler(dir) + V(25f, 0, 0));
        }
        else
        {
            foreach (var (at, r, i) in new[] { (V(0, 0.88f, 0.26f), 0.05f, 0), (V(0, 0.79f, 0.21f), 0.055f, 1), (V(0, 0.69f, 0.16f), 0.05f, 2) })
                PastelPuff(b, mane, at * V(1, 1, k), r, i);
            b.Spike(head, V(0, 0.86f, 0.34f * k), V(0, 0.95f, 0.39f * k), 0.022f, Rgb(232, 222, 248), mat: Shell);
        }
        PokeBuilder.Both(s => b.Eye(head, V(0.05f * s, 0.82f, 0.37f * k), V(0.8f * s, 0.1f, 0.6f), 0.022f, Rgb(140, 100, 210)));
        return b;
    }

    // ------------------------------------------------------------------ The Galarian Slowpoke line, Galarian Farfetch'd

    private static PokeBuilder SlowpokeGalar() => SlowpokeBuild(true);

    private static PokeBuilder SlowbroGalar() => SlowbroBuild(true);

    private static PokeBuilder FarfetchdGalar() => FarfetchdBuild(true);

    // ------------------------------------------------------------------ The Alolan Grimer line

    private static PokeBuilder GrimerAlola() => SludgeBuild(false, true);

    private static PokeBuilder MukAlola() => SludgeBuild(true, true);

    // ------------------------------------------------------------------ The Hisuian Voltorb line, Alolan Exeggutor

    /// <summary>Voltorb's Hisuian form: an apricorn, orange over grained wood, its eyes in a raised frame like a latch and a hole in its top.</summary>
    private static PokeBuilder VoltorbHisui()
    {
        var b = new PokeBuilder("Voltorb-Hisui", 0.5f, BodyPlan.Floating, V(0, 0.25f, 0)) { Coat = Shell };
        var orange = Rgb(226, 114, 48);
        var wood = Rgb(214, 174, 126);
        var (c, r) = TwoToneBall(b, 0.25f, orange, wood);
        Grain(b, c, r, 0.03f, 0.2f, 3, PixelCanvas.Mix(wood, Rgb(120, 80, 40), 0.25f));
        var front = On(c, r, 0, 0.27f);
        b.Torus(Body, front + V(0, 0, -0.008f), 0.085f, 0.022f, orange, V(90f, 0, 0), 1.45f, 0.72f);
        b.Mark(Body, c + V(0, r.Y, 0), V(0, 1f, 0), 0.016f, 0.016f, Rgb(70, 40, 30));
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.045f * s, 0.27f), V(0.2f * s, 0.1f, 1f), 0.028f, sclera: true, pupil: Black));
        return b;
    }

    /// <summary>Electrode's Hisuian form: grained wood over orange, heavy brows and a grimace with one fang.</summary>
    private static PokeBuilder ElectrodeHisui()
    {
        var b = new PokeBuilder("Electrode-Hisui", 0.66f, BodyPlan.Floating, V(0, 0.3f, 0)) { Coat = Shell };
        var wood = Rgb(228, 200, 152);
        var (c, r) = TwoToneBall(b, 0.3f, wood, Rgb(214, 92, 42));
        Grain(b, c, r, 0.34f, 0.56f, 3, PixelCanvas.Mix(wood, Rgb(120, 80, 40), 0.22f));
        PokeBuilder.Both(s => b.Ell(Body, On(c, r, 0.09f * s, 0.47f) + V(0, 0, -0.012f), V(0.065f, 0.016f, 0.024f), Rgb(170, 132, 90), V(-35f, 0, -22f * s), Shell, 0.01f));
        var mouth = On(c, r, 0, 0.17f);
        Grin(b, Body, mouth, V(0.11f, 0.03f, 0.045f), Rgb(120, 30, 30));
        b.Spike(Body, mouth + V(0.03f, 0.024f, -0.012f), mouth + V(0.028f, -0.004f, 0.0f), 0.012f, White, mat: Shell, blend: 0.004f);
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.09f * s, 0.41f), V(0.3f * s, 0.15f, 1f), 0.036f, sclera: true, pupil: Black, glare: true));
        return b;
    }

    private static PokeBuilder ExeggutorAlola() => ExeggutorBuild(true);

    // ------------------------------------------------------------------ Alolan Marowak, Galarian Weezing

    private static PokeBuilder MarowakAlola() => SkullBuild(true, true);

    private static PokeBuilder WeezingGalar() => WeezingBuild(true);

    // ------------------------------------------------------------------ Galarian Mr. Mime

    private static PokeBuilder MrMimeGalar()
    {
        var b = new PokeBuilder("Mr. Mime-Galar", 0.86f, BodyPlan.Biped, V(0, 0.6f, 0)) { Coat = Fur };
        var white = Rgb(246, 246, 250);
        var navy = Rgb(50, 62, 104);
        var ice = Rgb(156, 210, 240);
        var deep = Rgb(86, 152, 212);
        var pale = Rgb(232, 232, 240);

        // Thin navy legs, white spats, and tap shoes of ice curled at the toes
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.09f * s, 0.46f, 0));
            b.Limb(leg, V(0.09f * s, 0.46f, 0), V(0.14f * s, 0.24f, 0.03f), 0.032f, 0.026f, navy);
            b.Limb(leg, V(0.14f * s, 0.24f, 0.03f), V(0.15f * s, 0.08f, 0.0f), 0.026f, 0.024f, navy);
            b.Ell(leg, V(0.15f * s, 0.075f, 0.01f), V(0.04f, 0.03f, 0.04f), white);
            b.Ell(leg, V(0.16f * s, 0.035f, 0.05f), V(0.05f, 0.035f, 0.08f), ice, mat: Shell);
            b.Tube(leg, new[] { V(0.17f * s, 0.04f, 0.11f), V(0.18f * s, 0.06f, 0.15f), V(0.17f * s, 0.09f, 0.15f) }, 0.022f, 0.014f, ice, Shell);
            b.PaintEll(leg, V(0.16f * s, 0.03f, 0.08f), V(0.05f, 0.02f, 0.05f), deep);
        });
        // A round navy body under a white collar scalloped like a cloud, an ice crystal at its heart
        b.Ell(Body, V(0, 0.56f, 0), V(0.15f, 0.16f, 0.13f), navy);
        b.Ell(Body, V(0, 0.68f, 0.0f), V(0.16f, 0.08f, 0.13f), white);
        foreach (float x in new[] { -0.1f, -0.035f, 0.035f, 0.1f })
            b.Ell(Body, V(x, 0.615f, 0.1f - Math.Abs(x) * 0.3f), V(0.045f, 0.045f, 0.035f), white);
        b.Box(Body, V(0, 0.64f, 0.13f), V(0.038f, 0.038f, 0.022f), 0.006f, ice, V(0, 0, 45f), Shell, 0.004f);
        b.PaintEll(Body, V(0.015f, 0.625f, 0.15f), V(0.022f, 0.03f, 0.02f), deep, V(0, 0, 45f));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.15f * s, 0.73f, 0));
            b.Ell(arm, V(0.16f * s, 0.72f, 0), V(0.065f, 0.06f, 0.065f), ice);
            // Thin pale arms, white gloves
            var elbow = s < 0 ? V(-0.28f, 0.8f, 0.06f) : V(0.29f, 0.8f, 0.06f);
            var hand = s < 0 ? V(-0.34f, 0.94f, 0.1f) : V(0.36f, 0.94f, 0.1f);
            b.Limb(arm, V(0.16f * s, 0.72f, 0), elbow, 0.02f, 0.018f, pale);
            b.Limb(arm, elbow, hand, 0.018f, 0.017f, pale);
            b.Ell(arm, hand, V(0.045f, 0.05f, 0.035f), white);
            b.Ell(arm, hand + V(0.03f * s, 0.035f, 0.01f), V(0.018f, 0.025f, 0.018f), white, blend: 0.008f);
        });

        int head = b.Head(V(0, 0.78f, 0));
        b.Ell(head, V(0, 0.86f, 0.02f), V(0.1f, 0.095f, 0.095f), pale);
        PokeBuilder.Both(s => b.Mark(head, V(0.065f * s, 0.84f, 0.09f), V(0.6f * s, 0, 1f), 0.018f, 0.014f, Rgb(240, 170, 190)));
        Grin(b, head, V(0, 0.825f, 0.1f), V(0.05f, 0.018f, 0.02f), Rgb(226, 110, 140));
        PokeBuilder.Both(s => b.Eye(head, V(0.038f * s, 0.885f, 0.1f), V(0.35f * s, 0.05f, 1f), 0.016f));
        // Navy hair swept out to both sides like a moustache
        PokeBuilder.Both(s =>
        {
            Frond(b, head, V(0.03f * s, 0.95f, -0.01f), V(0.25f * s, 1.02f, -0.03f), 0.06f, navy, V(0, 0.25f, 1f), 0.3f);
            Frond(b, head, V(0.2f * s, 1.0f, -0.03f), V(0.3f * s, 0.93f, -0.03f), 0.04f, navy, V(0, 0.2f, 1f), 0.35f);
        });
        b.Ell(head, V(0, 0.95f, 0.0f), V(0.06f, 0.035f, 0.06f), navy);
        return b;
    }

    // ------------------------------------------------------------------ Paldean Tauros

    private static PokeBuilder TaurosCombat() => TaurosBuild("Tauros-Paldea-Combat-Breed", 1);

    private static PokeBuilder TaurosBlaze() => TaurosBuild("Tauros-Paldea-Blaze-Breed", 2);

    private static PokeBuilder TaurosAqua() => TaurosBuild("Tauros-Paldea-Aqua-Breed", 3);

    // ------------------------------------------------------------------ The Galarian legendary birds

    private static PokeBuilder ArticunoGalar() => ArticunoBuild(true);

    private static PokeBuilder ZapdosGalar()
    {
        // A great running bird: long black legs on feet with yellow talons, a body of orange feathers bristling with
        // black, wings of spikes half spread, a long beak and a spiky crest
        var b = new PokeBuilder("Zapdos-Galar", 1f, BodyPlan.Biped, V(0, 0.66f, 0)) { Coat = Fur };
        var orange = Rgb(232, 98, 52);
        var black = Rgb(42, 36, 40);
        var yellow = Rgb(246, 196, 40);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.09f * s, 0.58f, -0.02f));
            var knee = V(0.12f * s, 0.38f, 0.08f);
            var ankle = V(0.11f * s, 0.11f, -0.04f);
            b.Limb(leg, V(0.09f * s, 0.6f, -0.02f), knee, 0.07f, 0.045f, black);
            b.Limb(leg, knee, ankle, 0.04f, 0.032f, black);
            foreach (var (dx, dz) in new[] { (-0.5f, 1f), (0f, 1.15f), (0.5f, 1f), (0f, -0.6f) })
            {
                var toe = ankle + V(dx * 0.05f * s, -0.075f, dz * 0.1f);
                b.Limb(leg, ankle, toe, 0.022f, 0.018f, black);
                b.Spike(leg, toe, toe + Vector3.Normalize(toe - ankle) * 0.04f + V(0, -0.018f, 0), 0.016f, yellow, mat: Shell, blend: 0.004f);
            }
        });
        var c = V(0, 0.68f, 0);
        var r = V(0.15f, 0.14f, 0.17f);
        b.Ell(Body, c, r, orange, V(-15f, 0, 0));
        foreach (var dir in new[] { V(0.8f, -0.4f, 0.4f), V(-0.8f, -0.4f, 0.4f), V(0, -0.6f, 0.9f), V(0.5f, 0.3f, -0.8f), V(-0.5f, 0.3f, -0.8f) })
        {
            var n = Vector3.Normalize(dir);
            var at = Out(c, r, V(-15f, 0, 0), n);
            Blade(b, Body, at - n * 0.03f, at + n * 0.12f, 0.036f, black, Vector3.Cross(n, V(0, 1f, 0)) + V(0, 0, 0.01f), 0.35f);
        }
        PokeBuilder.Both(s => SpreadWing(b, s, V(0.12f * s, 0.74f, 0.02f), V(0.26f * s, 0.8f, -0.06f), 6, 70f, -25f, 0.32f, 0.05f, orange, black, 0.55f, pointed: true, tipFrom: 0));
        int tail = b.Tail(V(0, 0.64f, -0.15f));
        foreach (float a in new[] { -30f, 0f, 30f })
            Blade(b, tail, V(0, 0.64f, -0.15f), V(MathF.Sin(a * Degree) * 0.16f, 0.6f, -0.42f), 0.045f, a == 0f ? black : orange, V(0, 1f, 0), 0.3f);
        int head = b.Head(V(0, 0.8f, 0.1f));
        var hc = V(0, 0.96f, 0.17f);
        var hr = V(0.065f, 0.06f, 0.075f);
        b.Limb(head, V(0, 0.76f, 0.1f), hc, 0.065f, 0.05f, orange);
        b.Ell(head, hc, hr, orange);
        b.Spike(head, hc + V(0, -0.005f, 0.06f), hc + V(0, 0.02f, 0.3f), 0.022f, black, 0.8f, Shell);
        foreach (var (x, h, z) in new[] { (0f, 0.14f, -0.08f), (-0.04f, 0.11f, -0.1f), (0.04f, 0.11f, -0.1f), (-0.06f, 0.06f, -0.12f), (0.06f, 0.06f, -0.12f) })
            Blade(b, head, hc + V(x * 0.5f, 0.04f, 0.02f), hc + V(x * 2f, 0.04f + h, z), 0.026f, x == 0f ? black : orange, V(1f, 0, 0.3f), 0.4f);
        PokeBuilder.Both(s =>
        {
            var look = V(0.6f * s, 0.1f, 0.8f);
            b.Eye(head, Out(hc, hr, default, look), look, 0.017f, yellow, glare: true);
        });
        return b;
    }

    private static PokeBuilder MoltresGalar() => MoltresBuild(true);

    // ------------------------------------------------------------------ Hisuian Typhlosion (its build, Galarian Slowking's, Hisuian Qwilfish's and
    // Galarian Corsola's in PokemonModels.Johto1.cs)

    private static PokeBuilder TyphlosionHisui() => TyphlosionBuild(true);

    // ------------------------------------------------------------------ Paldean Wooper

    /// <summary>A gill like the end of a bone: a stalk out from the head forking into two knobbed prongs.</summary>
    private static void BoneGill(PokeBuilder b, int bone, Vector3 root, Vector3 fork, float r, Color color)
    {
        b.Tube(bone, new[] { root, fork }, r, r * 0.9f, color);
        var out_ = Vector3.Normalize(fork - root);
        foreach (float up in new[] { 1f, -1f })
        {
            var tip = fork + Vector3.Normalize(out_ + V(0, 1.3f * up, 0)) * r * 3.2f;
            b.Limb(bone, fork, tip, r * 0.9f, r * 0.85f, color);
            b.Ell(bone, tip, V(r * 1.3f, r * 1.3f, r * 1.2f), color);
        }
    }

    private static PokeBuilder WooperPaldea()
    {
        var b = new PokeBuilder("Wooper-Paldea", 0.52f, BodyPlan.Biped, V(0, 0.15f, 0)) { Coat = Fur };
        var brown = Rgb(118, 98, 92);
        var dark = Rgb(60, 48, 48);
        var gill = Rgb(224, 208, 238);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.045f * s, 0.08f, 0.0f));
            b.Limb(leg, V(0.045f * s, 0.08f, 0.0f), V(0.05f * s, 0.035f, 0.01f), 0.03f, 0.028f, brown);
            b.Ell(leg, V(0.052f * s, 0.025f, 0.025f), V(0.035f, 0.025f, 0.045f), brown);
        });
        // A small body with a dark stripe down its front and three bars across it, like the bones of a fish, and a
        // broad flat tail
        b.Ell(Body, V(0, 0.14f, -0.01f), V(0.075f, 0.085f, 0.07f), brown);
        b.Mark(Body, V(0, 0.135f, 0.058f), V(0, 0, 1f), 0.007f, 0.05f, dark, MarkShape.Bar);
        foreach (var (y, z, w) in new[] { (0.165f, 0.054f, 0.026f), (0.135f, 0.058f, 0.03f), (0.105f, 0.053f, 0.024f) })
            b.Mark(Body, V(0, y, z), V(0, (y - 0.14f) * 6f, 1f), w, 0.006f, dark, MarkShape.Bar);
        int tail = b.Tail(V(0, 0.1f, -0.07f));
        b.Limb(tail, V(0, 0.1f, -0.07f), V(0.0f, 0.09f, -0.12f), 0.035f, 0.032f, brown);
        b.Ell(tail, V(0.02f, 0.085f, -0.2f), V(0.09f, 0.026f, 0.1f), brown, V(14f, -15f, 0));

        int head = b.Head(V(0, 0.22f, 0));
        b.Ell(head, V(0, 0.3f, 0.01f), V(0.14f, 0.115f, 0.11f), brown);
        // Pale gills like the ends of bones either side, small eyes, a great open mouth
        PokeBuilder.Both(s =>
        {
            int g = b.Ear(head, s, V(0.12f * s, 0.31f, -0.01f));
            BoneGill(b, g, V(0.12f * s, 0.31f, -0.01f), V(0.22f * s, 0.33f, -0.02f), 0.014f, gill);
        });
        Grin(b, head, V(0, 0.265f, 0.1f), V(0.06f, 0.042f, 0.03f), Rgb(232, 122, 128));
        PokeBuilder.Both(s => b.Eye(head, V(0.062f * s, 0.34f, 0.098f), V(0.4f * s, 0.05f, 1f), 0.015f));
        return b;
    }

    // ------------------------------------------------------------------ Galarian Slowking, Hisuian Qwilfish

    private static PokeBuilder SlowkingGalar() => SlowkingBuild(true);

    private static PokeBuilder QwilfishHisui() => QwilfishBuild(true);

    // ------------------------------------------------------------------ Hisuian Sneasel

    private static PokeBuilder SneaselHisui()
    {
        var b = new PokeBuilder("Sneasel-Hisui", 0.7f, BodyPlan.Biped, V(0, 0.4f, 0)) { Coat = Fur };
        var gray = Rgb(206, 204, 222);
        var purple = Rgb(118, 94, 178);
        var gold = Rgb(244, 204, 64);

        // Lean grey legs that bend back at the heel, a purple patch on each thigh
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.32f, -0.01f));
            b.Limb(leg, V(0.05f * s, 0.32f, -0.01f), V(0.08f * s, 0.19f, 0.05f), 0.042f, 0.03f, gray);
            b.Limb(leg, V(0.08f * s, 0.19f, 0.05f), V(0.08f * s, 0.06f, -0.02f), 0.028f, 0.024f, gray);
            b.Limb(leg, V(0.08f * s, 0.06f, -0.02f), V(0.085f * s, 0.025f, 0.05f), 0.024f, 0.02f, gray);
            Claws(b, leg, V(0.085f * s, 0.02f, 0.07f), V(0.018f, 0, 0), V(0, -0.3f, 1f), 0.03f, 0.009f);
            b.PaintEll(leg, V(0.075f * s, 0.25f, 0.04f), V(0.03f, 0.04f, 0.03f), purple);
        });
        b.Ell(Body, V(0, 0.42f, 0), V(0.075f, 0.12f, 0.065f), gray);
        b.Ell(Body, V(0, 0.46f, 0.06f), V(0.02f, 0.028f, 0.014f), gold, mat: Shell, blend: 0.006f);
        // A fan of purple feathers off its back
        int tail = b.Tail(V(0, 0.36f, -0.05f));
        foreach (var tip in new[] { V(0.1f, 0.5f, -0.2f), V(0.03f, 0.56f, -0.22f), V(-0.05f, 0.52f, -0.21f), V(-0.11f, 0.44f, -0.18f) })
            Blade(b, tail, V(0, 0.36f, -0.05f), tip, 0.035f, purple, V(0, 0.3f, -1f), 0.3f);
        // Grey arms turning purple from the elbow, purple claws
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.07f * s, 0.5f, 0.02f));
            b.Limb(arm, V(0.07f * s, 0.5f, 0.02f), V(0.16f * s, 0.46f, 0.07f), 0.028f, 0.026f, gray);
            b.Limb(arm, V(0.16f * s, 0.46f, 0.07f), V(0.22f * s, 0.43f, 0.12f), 0.03f, 0.028f, purple);
            for (int i = -1; i <= 1; i++)
                b.Spike(arm, V(0.235f * s, 0.43f + 0.018f * i, 0.14f), V(0.27f * s, 0.41f + 0.02f * i, 0.19f), 0.011f, purple, mat: Shell, blend: 0.004f);
        });

        int head = b.Head(V(0, 0.52f, 0.02f));
        var c = V(0, 0.61f, 0.03f);
        var r = V(0.1f, 0.085f, 0.09f);
        b.Ell(head, c, r, gray);
        // A purple patch over the back of its head, one tall purple ear and one short grey one, gold spots and red eyes
        b.PaintEll(head, V(0.04f, 0.64f, -0.05f), V(0.09f, 0.07f, 0.07f), purple, V(0, 0, -20f));
        int earL = b.Ear(head, -1f, V(-0.06f, 0.67f, 0.0f));
        b.Spike(earL, V(-0.06f, 0.66f, 0.0f), V(-0.1f, 0.76f, -0.03f), 0.04f, gray, 0.5f);
        int earR = b.Ear(head, 1f, V(0.06f, 0.67f, 0.0f));
        Blade(b, earR, V(0.06f, 0.66f, -0.01f), V(0.13f, 0.92f, -0.06f), 0.045f, purple, V(0, 0, 1f), 0.35f);
        b.Ell(head, V(0, 0.665f, 0.1f), V(0.016f, 0.02f, 0.012f), gold, mat: Shell, blend: 0.006f);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.042f * s, 0.6f), V(0.4f * s, 0.1f, 1f), 0.022f, Rgb(220, 50, 70), glare: true));
        return b;
    }

    // ------------------------------------------------------------------ Galarian Corsola

    private static PokeBuilder CorsolaGalar() => CorsolaBuild(true);

    // ------------------------------------------------------------------ Galarian Zigzagoon and Linoone (their builds in PokemonModels.Hoenn1.cs)

    private static PokeBuilder ZigzagoonGalar() => ZigzagoonBuild(true);

    private static PokeBuilder LinooneGalar() => LinooneBuild(true);

    // ------------------------------------------------------------------ Dialga's and Palkia's Origin Formes

    private static PokeBuilder DialgaOrigin()
    {
        var b = new PokeBuilder("Dialga-Origin", 1f, BodyPlan.Quadruped, V(0, 0.5f, -0.04f)) { Coat = Metal };
        var navy = Rgb(42, 66, 132);
        var plate = Rgb(86, 120, 186);
        var cyan = Rgb(110, 214, 246);
        var gem = Rgb(70, 140, 236);

        // Four long legs, a skirt of steel-blue plates down each, a line of light along it
        foreach (var (z, front) in new[] { (0.14f, true), (-0.2f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.13f * s, 0.44f, z), front);
                var knee = V(0.15f * s, 0.24f, z + 0.03f);
                b.Ell(leg, V(0.14f * s, 0.42f, z), V(0.085f, 0.11f, 0.095f), navy);
                b.Limb(leg, V(0.14f * s, 0.4f, z), knee, 0.075f, 0.058f, navy);
                b.Limb(leg, knee, V(0.15f * s, 0.05f, z + 0.03f), 0.058f, 0.05f, navy);
                b.PaintEll(leg, V(0.2f * s, 0.32f, z + 0.01f), V(0.012f, 0.12f, 0.03f), cyan, V(0, 0, 8f * s));
                foreach (var (dx, dz) in new[] { (0.03f, 0.06f), (0.07f, 0f), (0.03f, -0.06f) })
                    Blade(b, leg, V((0.14f + dx) * s, 0.36f, z + dz), V((0.17f + dx * 1.4f) * s, 0.1f, z + dz * 1.6f), 0.05f, plate, V(s * dx, 0, dz + 0.01f * s), 0.22f, Metal);
                Claws(b, leg, V(0.15f * s, 0.02f, z + 0.09f), V(0.03f, 0, 0), V(0, -0.2f, 1f), 0.035f, 0.015f);
            });
        // A deep navy body, a great blue crystal in its chest, and crystal fins rising off its shoulders and back
        b.Ell(Body, V(0, 0.5f, -0.04f), V(0.18f, 0.17f, 0.3f), navy);
        PokeBuilder.Both(s => b.PaintEll(Body, V(0.155f * s, 0.53f, -0.06f), V(0.035f, 0.014f, 0.22f), cyan, V(8f, 0, 0)));
        b.Ell(Body, V(0, 0.54f, 0.2f), V(0.14f, 0.15f, 0.09f), navy, blend: 0.02f);
        b.Box(Body, V(0, 0.58f, 0.27f), V(0.06f, 0.07f, 0.03f), 0.01f, gem, V(-10f, 0, 0), Glow, 0.006f);
        b.Box(Body, V(0, 0.58f, 0.292f), V(0.03f, 0.04f, 0.012f), 0.006f, cyan, V(-10f, 0, 45f), Glow, 0.004f);
        for (int i = 0; i < 4; i++)
            PokeBuilder.Both(s =>
            {
                var root = V(0.07f * s, 0.64f, 0.1f - 0.1f * i);
                var tip = root + V((0.16f + 0.05f * i) * s, 0.36f - 0.06f * i, -0.16f - 0.04f * i);
                Blade(b, Body, root, tip, 0.065f - 0.006f * i, plate, V(1f, 0, 0.6f * s), 0.2f, Metal);
                Blade(b, Body, root + V(0.01f * s, 0.01f, -0.015f), tip + V(0.015f * s, 0.02f, -0.02f), 0.03f, cyan, V(1f, 0, 0.6f * s), 0.25f, Glow);
            });
        // A cape of plates over its hips
        foreach (float x in new[] { -0.1f, 0f, 0.1f })
            Blade(b, Body, V(x, 0.6f, -0.18f), V(x * 1.6f, 0.36f, -0.42f), 0.07f, plate, V(0, 1f, -0.3f), 0.2f, Metal);
        int tail = b.Tail(V(0, 0.46f, -0.32f));
        var path = Smooth(3, V(0, 0.48f, -0.3f), V(0, 0.4f, -0.5f), V(0, 0.3f, -0.66f), V(0, 0.26f, -0.82f));
        b.Tube(tail, path, 0.08f, 0.015f, navy, blend: 0f);
        Blade(b, tail, path[^3], path[^1] + V(0, 0.06f, -0.04f), 0.045f, plate, V(1f, 0, 0), 0.25f, Metal);
        // A long neck raised high to a head crowned with swept-back crests, red eyes
        var neck = Smooth(3, V(0, 0.58f, 0.18f), V(0, 0.76f, 0.26f), V(0, 0.92f, 0.28f), V(0, 1.02f, 0.3f));
        b.Tube(Body, neck, 0.095f, 0.058f, navy, blend: 0f);
        b.PaintEll(Body, V(0, 0.8f, 0.33f), V(0.045f, 0.15f, 0.024f), plate, V(-12f, 0, 0));
        int head = b.Head(V(0, 1.02f, 0.3f));
        b.Ell(head, V(0, 1.05f, 0.35f), V(0.065f, 0.062f, 0.08f), navy);
        b.Ell(head, V(0, 1.03f, 0.44f), V(0.045f, 0.04f, 0.065f), navy, blend: 0.02f);
        b.PaintEll(head, V(0, 1.015f, 0.46f), V(0.04f, 0.018f, 0.055f), plate);
        Blade(b, head, V(0, 1.09f, 0.34f), V(0, 1.27f, 0.14f), 0.055f, plate, V(1f, 0, 0), 0.28f, Metal);
        PokeBuilder.Both(s =>
        {
            Blade(b, head, V(0.045f * s, 1.08f, 0.33f), V(0.16f * s, 1.2f, 0.16f), 0.04f, plate, V(1f, 0, 0.4f * s), 0.28f, Metal);
            Blade(b, head, V(0.05f * s, 1.05f, 0.3f), V(0.17f * s, 1.04f, 0.12f), 0.03f, cyan, V(0, 1f, 0), 0.3f, Glow);
        });
        PokeBuilder.Both(s => b.Eye(head, V(0.054f * s, 1.065f, 0.39f), V(0.8f * s, 0.15f, 0.6f), 0.018f, Rgb(220, 40, 50), glare: true));
        return b;
    }

    private static PokeBuilder PalkiaOrigin()
    {
        var b = new PokeBuilder("Palkia-Origin", 1f, BodyPlan.Quadruped, V(0, 0.54f, -0.04f)) { Coat = Scales };
        var white = Rgb(238, 232, 246);
        var purple = Rgb(150, 98, 192);
        var pearl = Rgb(250, 168, 160);
        var wing = Rgb(232, 222, 248);

        // Four slender legs like a horse's, banded in purple
        foreach (var (z, front) in new[] { (0.16f, true), (-0.22f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.1f * s, 0.46f, z), front);
                var knee = V(0.11f * s, 0.26f, z + (front ? 0.03f : -0.03f));
                b.Ell(leg, V(0.1f * s, 0.44f, z), V(0.07f, 0.1f, 0.08f), white);
                b.Limb(leg, V(0.1f * s, 0.42f, z), knee, 0.055f, 0.04f, white);
                b.Limb(leg, knee, V(0.11f * s, 0.05f, z + 0.01f), 0.04f, 0.036f, white);
                b.PaintTorus(leg, knee, 0.042f, 0.01f, purple);
                b.Ell(leg, V(0.11f * s, 0.03f, z + 0.03f), V(0.045f, 0.03f, 0.055f), white);
                Claws(b, leg, V(0.11f * s, 0.02f, z + 0.075f), V(0.02f, 0, 0), V(0, -0.2f, 1f), 0.025f, 0.01f);
            });
        // A pale barrel of a body ringed with purple, a great pearl in a purple setting on each shoulder
        b.Ell(Body, V(0, 0.54f, -0.04f), V(0.14f, 0.14f, 0.28f), white);
        foreach (float z in new[] { -0.12f, 0.0f })
            b.PaintTorus(Body, V(0, 0.54f, z), 0.135f, 0.012f, purple, V(90f, 0, 0), 1f, 1f);
        PokeBuilder.Both(s =>
        {
            var at = V(0.14f * s, 0.66f, 0.12f);
            b.Ell(Body, at, V(0.1f, 0.1f, 0.1f), pearl, mat: Glow, blend: 0.02f);
            b.Torus(Body, at + V(0.02f * s, 0, 0), 0.085f, 0.022f, purple, V(0, 0, 90f), mat: Scales, blend: 0.006f);
            b.Torus(Body, at + V(0.02f * s, 0, 0), 0.085f, 0.018f, purple, V(90f, 0, 0), mat: Scales, blend: 0.006f);
        });
        // Wings like pale fins rising off its back, ribbed in purple
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.08f * s, 0.68f, -0.04f));
            var root = V(0.06f * s, 0.66f, -0.04f);
            var tip = V(0.24f * s, 1.12f, -0.26f);
            var p = Frond(b, w, root, tip, 0.13f, wing, V(1f * s, 0.1f, -0.4f), 0.16f);
            var across = Vector3.Transform(Vector3.UnitX, p.Rotation);
            foreach (float o in new[] { -0.05f, 0f, 0.05f })
                b.PaintEll(w, Vector3.Lerp(root, tip, 0.55f) + across * o, V(0.006f, 0.21f, 0.03f), purple).Rotation = p.Rotation;
        });
        // A neck up to a small head with a crest swept back, red eyes
        b.Tube(Body, new[] { V(0, 0.62f, 0.2f), V(0, 0.76f, 0.28f), V(0, 0.88f, 0.32f) }, 0.07f, 0.05f, white, blend: 0f);
        b.PaintTorus(Body, V(0, 0.76f, 0.28f), 0.062f, 0.01f, purple, V(-30f, 0, 0));
        int head = b.Head(V(0, 0.88f, 0.32f));
        var hc = V(0, 0.92f, 0.36f);
        b.Ell(head, hc, V(0.06f, 0.055f, 0.08f), white);
        b.Ell(head, V(0, 0.9f, 0.44f), V(0.04f, 0.033f, 0.055f), white, blend: 0.02f);
        Blade(b, head, V(0, 0.96f, 0.34f), V(0, 1.02f, 0.14f), 0.05f, white, V(1f, 0, 0), 0.3f);
        PokeBuilder.Both(s => Blade(b, head, V(0.04f * s, 0.95f, 0.34f), V(0.1f * s, 0.98f, 0.2f), 0.03f, purple, V(1f, 0, 0.3f * s), 0.3f));
        PokeBuilder.Both(s => b.Eye(head, V(0.048f * s, 0.935f, 0.41f), V(0.8f * s, 0.15f, 0.6f), 0.016f, Rgb(220, 40, 50), glare: true));
        // A long tail ending in pale plumes
        int tail = b.Tail(V(0, 0.52f, -0.3f));
        var path = Smooth(3, V(0, 0.54f, -0.3f), V(0.02f, 0.5f, -0.46f), V(0.06f, 0.46f, -0.6f), V(0.08f, 0.5f, -0.72f));
        b.Tube(tail, path, 0.06f, 0.02f, white, blend: 0f);
        b.PaintTorus(tail, path[3], 0.05f, 0.01f, purple, Euler(path[4] - path[2]));
        foreach (var (dy, dx) in new[] { (0.12f, 0.02f), (0.04f, 0.1f), (-0.04f, 0.02f), (0.08f, -0.06f) })
            Frond(b, tail, path[^2], path[^1] + V(dx, dy, -0.2f), 0.045f, wing, V(0, 1f, 0.2f), 0.45f);
        return b;
    }

    // ------------------------------------------------------------------ Hisuian Samurott (its build in PokemonModels.Unova1.cs)

    private static PokeBuilder SamurottHisui() => SamurottBuild(true);

    // ------------------------------------------------------------------ Hisuian Lilligant, Basculin's other stripes, Galarian Darumaka, Darmanitan's Zen Mode and Galarian shapes, Galarian Yamask and the Hisuian Zorua line (their builds in PokemonModels.Unova2.cs)

    private static PokeBuilder LilligantHisui() => LilligantBuild(true);

    private static PokeBuilder BasculinBlue() => BasculinBuild(Stripe.Blue);

    private static PokeBuilder BasculinWhite() => BasculinBuild(Stripe.White);

    private static PokeBuilder DarumakaGalar() => DarumakaBuild(true);

    private static PokeBuilder DarmanitanZen() => DarmanitanBuild(true, false);

    private static PokeBuilder DarmanitanGalar() => DarmanitanBuild(false, true);

    private static PokeBuilder DarmanitanGalarZen() => DarmanitanBuild(true, true);

    private static PokeBuilder YamaskGalar() => YamaskBuild(true);

    private static PokeBuilder ZoruaHisui() => ZoruaBuild(true);

    private static PokeBuilder ZoroarkHisui() => ZoroarkBuild(true);

    // ------------------------------------------------------------------ Deerling's and Sawsbuck's seasons and the female Frillish and Jellicent (their builds in PokemonModels.Unova3.cs)

    private static PokeBuilder DeerlingSummer() => DeerlingBuild(Season.Summer);

    private static PokeBuilder DeerlingAutumn() => DeerlingBuild(Season.Autumn);

    private static PokeBuilder DeerlingWinter() => DeerlingBuild(Season.Winter);

    private static PokeBuilder SawsbuckSummer() => SawsbuckBuild(Season.Summer);

    private static PokeBuilder SawsbuckAutumn() => SawsbuckBuild(Season.Autumn);

    private static PokeBuilder SawsbuckWinter() => SawsbuckBuild(Season.Winter);

    private static PokeBuilder FrillishFemale() => FrillishBuild(true);

    private static PokeBuilder JellicentFemale() => JellicentBuild(true);

    // ------------------------------------------------------------------ The rest of Unova's forms (their builds in PokemonModels.Unova4.cs)

    private static PokeBuilder StunfiskGalar() => StunfiskBuild(true);

    private static PokeBuilder BraviaryHisui() => BraviaryBuild(true);

    private static PokeBuilder TornadusTherian() => TornadusTherianBuild();

    private static PokeBuilder ThundurusTherian() => ThundurusTherianBuild();

    private static PokeBuilder LandorusTherian() => LandorusTherianBuild();

    private static PokeBuilder KyuremWhite() => KyuremBuild(Fusion.White);

    private static PokeBuilder KyuremBlack() => KyuremBuild(Fusion.Black);

    private static PokeBuilder KeldeoResolute() => KeldeoBuild(true);

    private static PokeBuilder MeloettaPirouette() => MeloettaBuild(true);

    private static PokeBuilder GenesectDouse() => GenesectBuild("Douse");

    private static PokeBuilder GenesectShock() => GenesectBuild("Shock");

    private static PokeBuilder GenesectBurn() => GenesectBuild("Burn");

    private static PokeBuilder GenesectChill() => GenesectBuild("Chill");

    // ------------------------------------------------------------------ Kalos's forms (their builds in PokemonModels.Kalos1.cs)

    private static PokeBuilder GreninjaAsh() => GreninjaBuild(1);

    /// <summary>Vivillon in one of its patterns, from the form's name (VIVILLON-POLAR), or null for a pattern it has none of.</summary>
    private static PokeBuilder? VivillonForm(string form)
    {
        var key = VivillonPatterns.Keys.FirstOrDefault(k => ("VIVILLON-" + k).Equals(form, StringComparison.OrdinalIgnoreCase));
        return key == null || key == "Meadow" ? null : VivillonBuild(key);
    }

    private static PokeBuilder PyroarFemale() => PyroarBuild(1);

    private static PokeBuilder FlabebeYellow() => FlabebeBuild("Yellow");

    private static PokeBuilder FlabebeOrange() => FlabebeBuild("Orange");

    private static PokeBuilder FlabebeBlue() => FlabebeBuild("Blue");

    private static PokeBuilder FlabebeWhite() => FlabebeBuild("White");

    private static PokeBuilder FloetteYellow() => FloetteBuild("Yellow");

    private static PokeBuilder FloetteOrange() => FloetteBuild("Orange");

    private static PokeBuilder FloetteBlue() => FloetteBuild("Blue");

    private static PokeBuilder FloetteWhite() => FloetteBuild("White");

    private static PokeBuilder FloetteEternal() => FloetteBuild(null, eternal: true);

    private static PokeBuilder FlorgesYellow() => FlorgesBuild("Yellow");

    private static PokeBuilder FlorgesOrange() => FlorgesBuild("Orange");

    private static PokeBuilder FlorgesBlue() => FlorgesBuild("Blue");

    private static PokeBuilder FlorgesWhite() => FlorgesBuild("White");

    /// <summary>The trims Furfrou can be given, by the form's name after "Furfrou-".</summary>
    private static readonly string[] FurfrouTrims = { "Heart", "Star", "Diamond", "Debutante", "Matron", "Dandy", "La-Reine", "Kabuki", "Pharaoh" };

    /// <summary>Furfrou in one of its trims, from the form's name (FURFROU-LA-REINE), or null for a trim it has none of.</summary>
    private static PokeBuilder? FurfrouForm(string form)
    {
        var trim = FurfrouTrims.FirstOrDefault(t => ("FURFROU-" + t).Equals(form, StringComparison.OrdinalIgnoreCase));
        return trim == null ? null : FurfrouBuild(trim);
    }

    private static PokeBuilder MeowsticFemale() => MeowsticBuild(true, false);

    private static PokeBuilder AegislashBlade() => AegislashBuild(true);

    private static PokeBuilder SliggooHisui() => SliggooBuild(true);

    private static PokeBuilder GoodraHisui() => GoodraBuild(true);

    private static PokeBuilder PumpkabooSmall() => PumpkabooBuild(0);

    private static PokeBuilder PumpkabooLarge() => PumpkabooBuild(2);

    private static PokeBuilder PumpkabooSuper() => PumpkabooBuild(3);

    private static PokeBuilder GourgeistSmall() => GourgeistBuild(0);

    private static PokeBuilder GourgeistLarge() => GourgeistBuild(2);

    private static PokeBuilder GourgeistSuper() => GourgeistBuild(3);

    private static PokeBuilder AvaluggHisui() => AvaluggBuild(true);

    private static PokeBuilder XerneasActive() => XerneasBuild(true);

    private static PokeBuilder Zygarde10() => ZygardeBuild(1);

    private static PokeBuilder ZygardeComplete() => ZygardeBuild(2);

    private static PokeBuilder HoopaUnbound() => HoopaBuild(true);

    // ------------------------------------------------------------------ Alola's forms (their builds in PokemonModels.Alola1.cs)

    private static PokeBuilder DecidueyeHisui() => DecidueyeBuild(true);

    // ------------------------------------------------------------------ Alola's second batch's forms (their builds in PokemonModels.Alola2.cs)

    /// <summary>Silvally carrying one of its memories, from the form's name (SILVALLY-FIRE), or null for a type it has no memory of.</summary>
    private static PokeBuilder? SilvallyForm(string form)
    {
        int i = Array.FindIndex(SilvallyMemories, m => m.Type != null && ("SILVALLY-" + m.Type).Equals(form, StringComparison.OrdinalIgnoreCase));
        return i < 0 ? null : SilvallyBuild(i);
    }

    /// <summary>Minior's core of one colour, from the form's name (MINIOR-RED), or null for a meteor, which looks like Minior whatever its core.</summary>
    private static PokeBuilder? MiniorForm(string form)
    {
        int i = Array.FindIndex(MiniorCores, m => ("MINIOR-" + m.Name).Equals(form, StringComparison.OrdinalIgnoreCase));
        return i < 0 ? null : MiniorBuild(i);
    }

    private static PokeBuilder MimikyuBusted() => MimikyuBuild(true);

    // ------------------------------------------------------------------ Alola's third batch's forms (their builds in PokemonModels.Alola3.cs)

    private static PokeBuilder NecrozmaDusk() => SolgaleoBuild(true);

    private static PokeBuilder NecrozmaDawn() => LunalaBuild(true);

    private static PokeBuilder NecrozmaUltra() => NecrozmaBuild(true);

    private static PokeBuilder MagearnaOriginal() => MagearnaBuild(true, false);

    // ------------------------------------------------------------------ Cramorant's catches and the Low Key Toxtricity (their builds in PokemonModels.Galar2.cs)

    private static PokeBuilder CramorantGulping() => CramorantBuild(1);

    private static PokeBuilder CramorantGorging() => CramorantBuild(2);

    private static PokeBuilder ToxtricityLowKey() => ToxtricityBuild(true, false);

    // ------------------------------------------------------------------ Alcremie's creams and sweets, Eiscue's Noice Face, the female Indeedee and the Hangry Morpeko (their builds in PokemonModels.Galar3.cs)

    /// <summary>Alcremie of the cream and sweet a form names (ALCREMIE-RUBY-SWIRL-STAR-SWEET), or null for a name that names none.</summary>
    private static PokeBuilder? AlcremieForm(string form)
    {
        for (int cream = 0; cream < AlcremieCreams.Length; cream++)
            for (int sweet = 0; sweet < AlcremieSweets.Length; sweet++)
                if (("ALCREMIE-" + AlcremieCreams[cream].Name + "-" + AlcremieSweets[sweet] + "-SWEET").Equals(form, StringComparison.OrdinalIgnoreCase) && (cream, sweet) != (0, 0))
                    return AlcremieBuild(cream, sweet);
        return null;
    }

    private static PokeBuilder EiscueNoice() => EiscueBuild(true);

    private static PokeBuilder IndeedeeFemale() => IndeedeeBuild(true);

    private static PokeBuilder MorpekoHangry() => MorpekoBuild(true);
}
