using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Carpathia, batch 12 (plan 46 · CP4 to CP8): Fluieras to Lunacolac (20 species), sculpted from nothing after the picture
// briefs of CP-dex-4b and CP-dex-6 and the user's reference pictures. Each line shares its palette and its motifs and grows
// in size and detail: Tobel's red and blue zigzags round Tobosar's belly drum, Filuta's flowered page in Carturel's book,
// Steluta's red and silver points round Stelar's, Mascuta's painted mask on Mascaron, Sobitel's green tiles over Teracotor,
// Pricolel's black scruff and yellow eyes on Pricolici, and the three Fates' one gown and golden thread.
internal static partial class PokemonModels
{
    private static string[] CarpathiaNames12() => new[]
    {
        "Fluieras", "Tobel", "Tobosar", "Filuta", "Carturel", "Steluta", "Stelar", "Mascuta", "Mascaron", "Sperietoare",
        "Sobitel", "Teracotor", "Cumpanel", "Pricolel", "Pricolici", "Ursivia", "Ursifira", "Ursimora", "Solzmeu", "Lunacolac"
    };

    private static PokeBuilder? CreateCarpathia12(string species) => species switch
    {
        "FLUIERAS" => Carpathia12Sculpts.Fluieras(),
        "TOBEL" => Carpathia12Sculpts.Tobel(),
        "TOBOSAR" => Carpathia12Sculpts.Tobosar(),
        "FILUTA" => Carpathia12Sculpts.Filuta(),
        "CARTUREL" => Carpathia12Sculpts.Carturel(),
        "STELUTA" => Carpathia12Sculpts.Star(false),
        "STELAR" => Carpathia12Sculpts.Star(true),
        "MASCUTA" => Carpathia12Sculpts.Mascuta(),
        "MASCARON" => Carpathia12Sculpts.Mascaron(),
        "SPERIETOARE" => Carpathia12Sculpts.Sperietoare(),
        "SOBITEL" => Carpathia12Sculpts.Sobitel(),
        "TERACOTOR" => Carpathia12Sculpts.Teracotor(),
        "CUMPANEL" => Carpathia12Sculpts.Cumpanel(),
        "PRICOLEL" => Carpathia12Sculpts.Pricolel(),
        "PRICOLICI" => Carpathia12Sculpts.Pricolici(),
        "URSIVIA" => Carpathia12Sculpts.Ursivia(),
        "URSIFIRA" => Carpathia12Sculpts.Ursifira(),
        "URSIMORA" => Carpathia12Sculpts.Ursimora(),
        "SOLZMEU" => Carpathia12Sculpts.Solzmeu(),
        "LUNACOLAC" => Carpathia12Sculpts.Lunacolac(),
        _ => null
    };

    /// <summary>The builders of batch 12, kept in a class of their own so that their helpers meet no other batch's names.</summary>
    private static class Carpathia12Sculpts
    {
        private const SurfaceMaterial Cloth = SurfaceMaterial.Cloth, Leather = SurfaceMaterial.Leather;

        // Folk colours the lines share
        private static readonly Color FolkRed = Rgb(206, 62, 58);
        private static readonly Color FolkBlue = Rgb(64, 112, 196);
        private static readonly Color FolkYellow = Rgb(232, 188, 64);
        private static readonly Color DrumSkin = Rgb(240, 228, 192);
        private static readonly Color DrumRim = Rgb(196, 160, 112);
        private static readonly Color Paper = Rgb(238, 222, 172);
        private static readonly Color Vine = Rgb(110, 150, 86);
        private static readonly Color StarRed = Rgb(184, 42, 60);
        private static readonly Color StarSilver = Rgb(204, 208, 218);
        private static readonly Color Candle = Rgb(255, 214, 138);
        private static readonly Color MaskWhite = Rgb(240, 232, 216);
        private static readonly Color MaskRed = Rgb(198, 64, 48);
        private static readonly Color Wood = Rgb(150, 106, 66);
        private static readonly Color Wool = Rgb(238, 230, 208);
        private static readonly Color TileGreen = Rgb(58, 128, 70);
        private static readonly Color Iron = Rgb(62, 60, 64);
        private static readonly Color FireGlow = Rgb(255, 150, 60);
        private static readonly Color NightBlack = Rgb(44, 44, 54);
        private static readonly Color NightGray = Rgb(88, 88, 100);
        private static readonly Color Mist = Rgb(150, 150, 164);
        private static readonly Color Thread = Rgb(255, 210, 100);

        // ------------------------------------------------------------------ helpers

        /// <summary>A zigzag painted round an upright round body: sticks leaning one way and the other round it.</summary>
        private static void ZigBand(PokeBuilder b, int bone, Vector3 c, float rx, float rz, float y, float h, Color color, int peaks, float thick = 0.016f)
        {
            int n = peaks * 2;
            for (int i = 0; i < n; i++)
            {
                float a = (i + 0.5f) / n * MathF.Tau;
                var at = V(c.X + MathF.Sin(a) * rx, y, c.Z + MathF.Cos(a) * rz);
                float lean = (i % 2 == 0 ? 1f : -1f) * MathF.Atan2(MathF.PI * (rx + rz) / n, h) * 180f / MathF.PI;
                float len = MathF.Sqrt(h * h + MathF.Pow(MathF.PI * (rx + rz) / n, 2f)) * 0.55f;
                b.PaintEll(bone, at, V(thick, len, 0.04f), color, V(0, a * 180f / MathF.PI, lean), 0.006f);
            }
        }

        /// <summary>A line of grout painted round a box at height y.</summary>
        private static void Course(PokeBuilder b, int bone, Vector3 c, float hx, float hz, float y, Color color) =>
            b.PaintEll(bone, V(c.X, y, c.Z), V(hx * 1.45f, 0.006f, hz * 1.45f), color, default, 0.005f);

        /// <summary>A strip of ribbon from <paramref name="from"/> along <paramref name="dir"/>, flat across its own z.</summary>
        private static void Ribbon(PokeBuilder b, int bone, Vector3 from, Vector3 dir, float length, float width, Color color, float yaw = 0f)
        {
            var d = Vector3.Normalize(dir);
            var e = Euler(d);
            b.Ell(bone, from + d * length * 0.5f, V(width, length * 0.5f, 0.013f), color, V(e.X, yaw, e.Z), Cloth, 0.01f);
        }

        /// <summary>A small crow perched at <paramref name="at"/> (its feet), looking along +z.</summary>
        private static void Crow(PokeBuilder b, int bone, Vector3 at, float turn)
        {
            var black = Rgb(58, 58, 76);
            var f = V(MathF.Sin(turn), 0, MathF.Cos(turn));
            b.Ell(bone, at + V(0, 0.045f, 0), V(0.034f, 0.04f, 0.05f), black, V(0, turn * 180f / MathF.PI, 0), Leather, 0.012f);
            b.Ell(bone, at + V(0, 0.095f, 0) + f * 0.03f, V(0.027f, 0.027f, 0.028f), black, default, Leather, 0.012f);
            b.Spike(bone, at + V(0, 0.093f, 0) + f * 0.052f, at + V(0, 0.087f, 0) + f * 0.088f, 0.012f, Rgb(96, 92, 96), mat: Shell);
            b.Spike(bone, at + V(0, 0.04f, 0) - f * 0.04f, at + V(0, 0.02f, 0) - f * 0.11f, 0.024f, black, 0.45f, Leather);
            b.PaintEll(bone, at + V(0, 0.1f, 0) + f * 0.042f, V(0.03f, 0.008f, 0.012f), Rgb(150, 150, 170), V(0, turn * 180f / MathF.PI, 0), 0.004f);
        }

        /// <summary>A mist lying under the feet, on the root so it stays on the ground.</summary>
        private static void GroundMist(PokeBuilder b, float rx, float rz) =>
            b.Ell(Root, V(0, 0.012f, 0), V(rx, 0.014f, rz), Mist, default, Glow, 0.02f);

        /// <summary>A gown from the shoulders at <paramref name="top"/> to the ground, cut flat there (made before the legs, which it would carve).</summary>
        private static void Gown(PokeBuilder b, float top, float topR, float hemR, Color color, bool flat = true)
        {
            b.Limb(Body, V(0, top, 0), V(0, flat ? 0.04f : 0.16f, -0.02f), topR, hemR, color, Cloth);
            if (flat) b.CutBox(Body, V(0, -0.5f, 0), V(0.9f, 0.5f, 0.9f), Quaternion.Identity);
        }

        private static float GownRadius(float y, float top, float topR, float hemR, float bottom = 0.04f) =>
            topR + (top - y) / (top - bottom) * (hemR - topR);

        /// <summary>A ring of thread lying round the gown at height y (half sunk in it, so it is one piece with it).</summary>
        private static void ThreadRing(PokeBuilder b, float y, float r, float tilt, Color color)
        {
            var pts = new Vector3[17];
            for (int i = 0; i < pts.Length; i++)
            {
                float a = i / 16f * MathF.Tau;
                pts[i] = V(MathF.Sin(a) * r, y + MathF.Sin(a) * tilt, MathF.Cos(a) * r - 0.01f);
            }
            b.Tube(Body, pts, 0.016f, 0.016f, color, Glow, 0.006f);
        }

        // ------------------------------------------------------------------ Fluieras

        /// <summary>Fluieras: a pale wooden shepherd's flute walking on twig legs, carved zigzags round it, finger holes down its front, a felt hat and a curl of tune from its mouthpiece.</summary>
        public static PokeBuilder Fluieras()
        {
            var b = new PokeBuilder("Fluieras", 0.72f, BodyPlan.Biped, V(0, 0.6f, 0)) { Coat = Shell };
            var wood = Rgb(232, 218, 186);
            var carve = Rgb(150, 112, 74);
            var twig = Rgb(146, 116, 80);
            var leaf = Rgb(104, 168, 78);
            var hat = Rgb(62, 108, 58);
            var tune = Rgb(176, 232, 170);

            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.06f * s, 0.3f, 0));
                b.Limb(leg, V(0.055f * s, 0.32f, 0), V(0.09f * s, 0.17f, 0.03f), 0.03f, 0.024f, twig);
                b.Limb(leg, V(0.09f * s, 0.17f, 0.03f), V(0.085f * s, 0.04f, -0.01f), 0.024f, 0.021f, twig);
                b.Ell(leg, V(0.087f * s, 0.026f, 0.02f), V(0.036f, 0.026f, 0.046f), PixelCanvas.Shadow(twig, 0.25f));
            });
            // The flute: a long pale pipe, open at its foot, a little bent
            b.Tube(Body, Smooth(3, V(0, 0.28f, 0.0f), V(0.02f, 0.55f, 0.02f), V(0, 0.84f, 0.02f)), 0.085f, 0.08f, wood, blend: 0.02f);
            b.Ell(Body, V(0, 0.3f, 0.0f), V(0.095f, 0.04f, 0.095f), wood, blend: 0.02f);
            b.PaintEll(Body, V(0, 0.255f, 0), V(0.06f, 0.03f, 0.06f), carve, default, 0.008f);
            foreach (float y in new[] { 0.44f, 0.5f, 0.56f, 0.62f, 0.68f })
            {
                float x = 0.02f * MathF.Sin((y - 0.28f) / 0.56f * MathF.PI) * 0.9f;
                b.Cut(Body, V(x, y, 0.092f), V(0.016f, 0.016f, 0.02f));
                b.PaintEll(Body, V(x, y, 0.082f), V(0.022f, 0.022f, 0.03f), Rgb(70, 50, 36), default, 0.006f);
            }
            ZigBand(b, Body, V(0.018f, 0, 0.02f), 0.088f, 0.088f, 0.75f, 0.06f, carve, 6, 0.01f);
            ZigBand(b, Body, V(0.005f, 0, 0.01f), 0.09f, 0.09f, 0.37f, 0.06f, carve, 6, 0.01f);

            // The head is the top of the flute: its mouthpiece the mouth, leaf brows and a felt hat
            int head = b.Head(V(0, 0.8f, 0.02f));
            var c = V(0, 0.93f, 0.02f);
            var r = V(0.085f, 0.14f, 0.082f);
            b.Ell(head, c, r, wood, blend: 0.02f);
            b.Torus(head, V(0, 0.885f, 0.1f), 0.024f, 0.013f, PixelCanvas.Shadow(wood, 0.12f), V(90f, 0, 0), mat: Shell, blend: 0.01f);
            b.PaintEll(head, V(0, 0.885f, 0.11f), V(0.018f, 0.018f, 0.02f), Rgb(80, 58, 40), default, 0.005f);
            PokeBuilder.Both(s =>
            {
                var at = On(c, r, 0.04f * s, 0.955f);
                b.Eye(head, at, Outward(c, r, at), 0.02f, Rgb(70, 52, 40));
                b.Ell(head, On(c, r, 0.045f * s, 0.995f) + V(0, 0, -0.008f), V(0.03f, 0.01f, 0.016f), leaf, V(0, 0, -14f * s), Leaf, 0.008f);
            });
            b.Ell(head, V(0, 1.04f, 0.0f), V(0.15f, 0.018f, 0.14f), hat, V(6f, 0, 0), Cloth, 0.01f);
            b.Ell(head, V(0, 1.09f, -0.005f), V(0.08f, 0.06f, 0.078f), hat, default, Cloth, 0.02f);
            b.Torus(head, V(0, 1.058f, -0.005f), 0.075f, 0.012f, Rgb(40, 58, 38), mat: Cloth, blend: 0.006f);
            b.Ell(head, V(0.075f, 1.075f, -0.02f), V(0.028f, 0.014f, 0.016f), leaf, V(0, 0, 30f), Leaf, 0.008f);
            // The tune curling out of its mouthpiece
            b.Tube(head, Smooth(3, V(0, 0.885f, 0.12f), V(-0.08f, 0.92f, 0.17f), V(-0.17f, 0.99f, 0.12f), V(-0.2f, 1.08f, 0.04f),
                V(-0.14f, 1.14f, 0.0f), V(-0.09f, 1.07f, 0.03f)), 0.022f, 0.017f, tune, Glow, 0.008f);

            // Twig arms with three twig fingers and a leaf or two
            PokeBuilder.Both(s =>
            {
                int arm = b.Arm(s, V(0.08f * s, 0.62f, 0.02f));
                var hand = V(0.25f * s, 0.47f, 0.06f);
                b.Tube(arm, Smooth(3, V(0.06f * s, 0.62f, 0.02f), V(0.17f * s, 0.56f, 0.04f), hand), 0.022f, 0.016f, twig);
                foreach (var (dx, dy) in new[] { (0.05f, -0.03f), (0.03f, -0.06f), (0.0f, -0.07f) })
                    b.Limb(arm, hand, hand + V(dx * s, dy, 0.01f), 0.015f, 0.012f, twig);
                b.Ell(arm, hand + V(0.06f * s, -0.02f, 0.01f), V(0.028f, 0.016f, 0.012f), leaf, V(0, 0, 30f * s), Leaf, 0.008f);
                b.Ell(arm, V(0.16f * s, 0.585f, 0.04f), V(0.024f, 0.014f, 0.012f), leaf, V(0, 0, -30f * s), Leaf, 0.008f);
            });
            return b;
        }

        // ------------------------------------------------------------------ Tobel line

        /// <summary>Tobel: a small round drum on two stick legs, a cream goatskin top, red zigzags on a blue rim, two drumstick arms thumping it.</summary>
        public static PokeBuilder Tobel()
        {
            var b = new PokeBuilder("Tobel", 0.45f, BodyPlan.Biped, V(0, 0.32f, 0)) { Coat = Leather };
            var stick = Rgb(214, 186, 140);
            // The shell, cut flat at the top and the bottom before anything else is added
            b.Ell(Body, V(0, 0.32f, 0), V(0.23f, 0.14f, 0.23f), FolkBlue, mat: Leather);
            b.CutBox(Body, V(0, 0.625f, 0), V(0.6f, 0.2f, 0.6f), Quaternion.Identity);
            b.CutBox(Body, V(0, 0.0f, 0), V(0.6f, 0.2f, 0.6f), Quaternion.Identity);
            ZigBand(b, Body, Vector3.Zero, 0.235f, 0.235f, 0.31f, 0.1f, FolkRed, 7, 0.02f);
            b.Ell(Body, V(0, 0.425f, 0), V(0.215f, 0.022f, 0.215f), DrumSkin, mat: Leather, blend: 0.01f);
            b.PaintEll(Body, V(-0.05f, 0.45f, 0.06f), V(0.1f, 0.02f, 0.05f), PixelCanvas.Light1(DrumSkin, 0.4f), V(0, 30f, 0), 0.02f);
            b.Torus(Body, V(0, 0.425f, 0), 0.212f, 0.022f, DrumRim, mat: Shell, blend: 0.008f);
            b.Ell(Body, V(0, 0.206f, 0), V(0.205f, 0.016f, 0.205f), DrumRim, mat: Shell, blend: 0.01f);
            b.Torus(Body, V(0, 0.208f, 0), 0.2f, 0.022f, DrumRim, mat: Shell, blend: 0.008f);
            PokeBuilder.Both(s =>
            {
                var at = V(0.075f * s, 0.315f, 0.218f);
                b.Eye(Body, at, V(0.33f * s, 0, 1f), 0.03f, sclera: true, pupil: Rgb(40, 34, 40));
            });
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.07f * s, 0.2f, 0));
                b.Limb(leg, V(0.07f * s, 0.21f, 0.0f), V(0.085f * s, 0.04f, 0.02f), 0.022f, 0.018f, stick, Shell);
                b.Ell(leg, V(0.088f * s, 0.024f, 0.04f), V(0.03f, 0.024f, 0.042f), stick, mat: Shell);
            });
            // Drumstick arms bent up over its top, their knobs resting on the skin
            PokeBuilder.Both(s =>
            {
                int arm = b.Arm(s, V(0.22f * s, 0.32f, 0));
                b.Tube(arm, Smooth(3, V(0.21f * s, 0.32f, 0.0f), V(0.33f * s, 0.38f, -0.02f), V(0.33f * s, 0.52f, -0.02f), V(0.2f * s, 0.58f, 0.0f),
                    V(0.12f * s, 0.51f, 0.02f)), 0.017f, 0.017f, stick, Shell);
                b.Ell(arm, V(0.11f * s, 0.475f, 0.02f), V(0.045f, 0.04f, 0.045f), DrumRim, mat: Shell, blend: 0.01f);
            });
            return b;
        }

        /// <summary>Tobosar: a stout drummer with a great folk drum for a belly, ribbons fringing its belt and trailing from its mallet fists, a scowl and rope-knot horns.</summary>
        public static PokeBuilder Tobosar()
        {
            var b = new PokeBuilder("Tobosar", 0.9f, BodyPlan.Biped, V(0, 0.62f, 0)) { Coat = Fur };
            var skin = Rgb(206, 170, 128);
            var dark = Rgb(158, 122, 88);
            var ribbons = new[] { FolkRed, FolkBlue, FolkYellow };

            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.17f * s, 0.42f, 0));
                b.Limb(leg, V(0.17f * s, 0.44f, 0), V(0.21f * s, 0.15f, 0.02f), 0.1f, 0.085f, skin);
                b.Ell(leg, V(0.22f * s, 0.055f, 0.06f), V(0.1f, 0.055f, 0.13f), skin);
                b.Torus(leg, V(0.21f * s, 0.15f, 0.02f), 0.085f, 0.018f, dark, mat: Cloth, blend: 0.006f);
            });
            b.Ell(Body, V(0, 0.66f, -0.02f), V(0.34f, 0.3f, 0.25f), skin);
            b.Ell(Body, V(0, 0.84f, 0.0f), V(0.38f, 0.2f, 0.24f), skin);
            PokeBuilder.Both(s => b.Ell(Body, V(0.3f * s, 0.87f, -0.02f), V(0.14f, 0.12f, 0.13f), skin));
            // The belt the drum hangs from, zigzagged, and ribbons fringing it
            b.Ell(Body, V(0, 0.56f, 0.0f), V(0.34f, 0.14f, 0.28f), FolkBlue, mat: Leather, blend: 0.02f);
            ZigBand(b, Body, Vector3.Zero, 0.34f, 0.28f, 0.56f, 0.12f, FolkRed, 9, 0.025f);
            for (int k = 0; k < 11; k++)
            {
                float a = (-100f + k * 20f) * Degree;
                var at = V(MathF.Sin(a) * 0.3f, 0.5f, MathF.Cos(a) * 0.25f);
                b.Ell(Body, at + V(0, -0.06f, 0), V(0.02f, 0.075f, 0.013f), ribbons[k % 3], V(0, a / Degree, 0), Cloth, 0.008f);
            }
            // The drum: a cream skin on its belly, rimmed
            b.Ell(Body, V(0, 0.6f, 0.26f), V(0.22f, 0.22f, 0.06f), DrumSkin, mat: Leather, blend: 0.02f);
            b.Torus(Body, V(0, 0.6f, 0.265f), 0.22f, 0.028f, DrumRim, V(90f, 0, 0), mat: Shell, blend: 0.008f);
            b.PaintEll(Body, V(-0.07f, 0.68f, 0.31f), V(0.08f, 0.04f, 0.03f), PixelCanvas.Light1(DrumSkin, 0.4f), V(0, 0, 30f), 0.02f);

            // Arms ending in big round mallet fists, ribbons trailing from the wrists
            PokeBuilder.Both(s =>
            {
                int arm = b.Arm(s, V(0.36f * s, 0.86f, 0));
                b.Limb(arm, V(0.36f * s, 0.88f, 0), V(0.5f * s, 0.66f, 0.04f), 0.11f, 0.09f, skin);
                b.Limb(arm, V(0.5f * s, 0.66f, 0.04f), V(0.56f * s, 0.5f, 0.1f), 0.09f, 0.08f, skin);
                b.Ell(arm, V(0.58f * s, 0.42f, 0.12f), V(0.14f, 0.13f, 0.15f), skin);
                b.Torus(arm, V(0.55f * s, 0.53f, 0.1f), 0.085f, 0.022f, FolkBlue, mat: Cloth, blend: 0.006f);
                for (int k = 0; k < 3; k++)
                    Ribbon(b, arm, V((0.6f + 0.03f * k) * s, 0.52f, 0.02f - 0.03f * k), V(0.25f * s, -1f, -0.2f - 0.1f * k), 0.22f - 0.03f * k, 0.022f, ribbons[k]);
            });

            // A small scowling head, tusks, a heavy brow and knots of rope for horns
            int head = b.Head(V(0, 0.96f, 0.08f));
            var c = V(0, 1.02f, 0.12f);
            var r = V(0.15f, 0.13f, 0.14f);
            b.Ell(head, c, r, skin);
            b.Ell(head, V(0, 0.95f, 0.17f), V(0.13f, 0.07f, 0.11f), skin);
            b.Ell(head, V(0, 1.09f, 0.19f), V(0.12f, 0.025f, 0.06f), dark, blend: 0.02f);
            b.Ell(head, V(0, 1.01f, 0.25f), V(0.035f, 0.025f, 0.025f), dark, blend: 0.012f);
            b.Mark(head, V(0, 0.94f, 0.275f), V(0, -0.2f, 1f), 0.04f, 0.008f, PixelCanvas.Shadow(skin, 0.5f), MarkShape.Bar);
            PokeBuilder.Both(s =>
            {
                b.Spike(head, V(0.075f * s, 0.95f, 0.25f), V(0.09f * s, 1.02f, 0.27f), 0.02f, Claw, mat: Shell);
                var at = On(c, r, 0.065f * s, 1.025f);
                b.Eye(head, at, Outward(c, r, at), 0.026f, Rgb(70, 50, 40), glare: true);
                b.Tube(head, Smooth(3, V(0.05f * s, 1.12f, 0.08f), V(0.08f * s, 1.2f, 0.06f), V(0.13f * s, 1.2f, 0.08f)), 0.024f, 0.02f, dark, Cloth);
                b.Tube(head, Smooth(3, V(0.11f * s, 1.09f, 0.04f), V(0.17f * s, 1.12f, 0.02f), V(0.19f * s, 1.08f, 0.03f)), 0.022f, 0.018f, dark, Cloth);
            });
            return b;
        }

        // ------------------------------------------------------------------ Filuta line

        /// <summary>The painted border of a page or a book's leaf: a vine round it, red and purple flowers on it.</summary>
        private static void FlowerBorder(PokeBuilder b, int bone, float x0, float x1, float y0, float y1, float z)
        {
            float cx = (x0 + x1) / 2f, cy = (y0 + y1) / 2f, hx = (x1 - x0) / 2f, hy = (y1 - y0) / 2f;
            b.PaintEll(bone, V(cx, y1, z), V(hx, 0.005f, 0.03f), Vine, default, 0.004f);
            b.PaintEll(bone, V(cx, y0, z), V(hx, 0.005f, 0.03f), Vine, default, 0.004f);
            b.PaintEll(bone, V(x0, cy, z), V(0.005f, hy, 0.03f), Vine, default, 0.004f);
            b.PaintEll(bone, V(x1, cy, z), V(0.005f, hy, 0.03f), Vine, default, 0.004f);
            int i = 0;
            for (float t = 0.1f; t < 1f; t += 0.2f, i++)
            {
                var col = i % 2 == 0 ? Rgb(204, 74, 80) : Rgb(150, 98, 182);
                b.PaintEll(bone, V(x0 + (x1 - x0) * t, y1, z), V(0.013f, 0.013f, 0.03f), col, default, 0.004f);
                b.PaintEll(bone, V(x1 - (x1 - x0) * t, y0, z), V(0.013f, 0.013f, 0.03f), col, default, 0.004f);
                b.PaintEll(bone, V(x0, y0 + (y1 - y0) * t, z), V(0.013f, 0.013f, 0.03f), col, default, 0.004f);
                b.PaintEll(bone, V(x1, y1 - (y1 - y0) * t, z), V(0.013f, 0.013f, 0.03f), col, default, 0.004f);
            }
        }

        /// <summary>Filuta: a loose yellowed page fluttering on the air, its corners curled, a flower border painted round it and a little inked face.</summary>
        public static PokeBuilder Filuta()
        {
            var b = new PokeBuilder("Filuta", 0.45f, BodyPlan.Floating, V(0, 0.45f, 0)) { Coat = Cloth }.Hover();
            var ink = Rgb(60, 50, 44);
            b.Box(Body, V(0, 0.45f, 0), V(0.22f, 0.26f, 0.014f), 0.012f, Paper, mat: Cloth);
            b.Ell(Body, V(0, 0.71f, -0.01f), V(0.21f, 0.025f, 0.02f), Paper, V(-30f, 0, 0), Cloth, 0.01f);
            FlowerBorder(b, Body, -0.19f, 0.19f, 0.22f, 0.68f, 0.015f);
            foreach (float y in new[] { 0.37f, 0.33f, 0.29f })
                b.PaintEll(Body, V(-0.01f, y, 0.015f), V(0.13f, 0.004f, 0.03f), Rgb(130, 110, 96), default, 0.004f);
            // The face, drawn in ink
            PokeBuilder.Both(s =>
            {
                b.Eye(Body, V(0.06f * s, 0.5f, 0.014f), V(0, 0, 1f), 0.022f, Rgb(40, 34, 32));
                b.PaintEll(Body, V(0.1f * s, 0.465f, 0.015f), V(0.022f, 0.011f, 0.03f), Rgb(236, 170, 150), default, 0.006f);
            });
            b.Mark(Body, V(0, 0.445f, 0.014f), V(0, 0, 1f), 0.02f, 0.011f, ink, MarkShape.Smile);
            // Two corners lifting on the air: the lower left curling forward, the upper right blown back
            int left = b.Part("pageL", Body, V(-0.2f, 0.24f, 0), PokeRole.Fin, 0f, -1f);
            b.Ell(left, V(-0.29f, 0.25f, 0.04f), V(0.12f, 0.075f, 0.014f), Paper, V(0, -35f, 20f), Cloth, 0.012f);
            b.Tube(left, Smooth(3, V(-0.36f, 0.2f, 0.085f), V(-0.41f, 0.23f, 0.12f), V(-0.4f, 0.28f, 0.13f)), 0.018f, 0.016f, Paper, Cloth);
            // The curled edge rolled from the page's side to the curl, holding the corner on
            b.Tube(left, Smooth(3, V(-0.2f, 0.25f, 0.005f), V(-0.29f, 0.25f, 0.04f), V(-0.36f, 0.2f, 0.085f)), 0.014f, 0.017f, Paper, Cloth, 0.008f);
            int right = b.Part("pageR", Body, V(0.2f, 0.62f, 0), PokeRole.Fin, 1f, 1f);
            b.Ell(right, V(0.3f, 0.6f, -0.04f), V(0.13f, 0.075f, 0.014f), Paper, V(0, 35f, -15f), Cloth, 0.012f);
            b.Tube(right, Smooth(3, V(0.38f, 0.64f, -0.09f), V(0.43f, 0.6f, -0.12f), V(0.41f, 0.55f, -0.11f)), 0.018f, 0.016f, Paper, Cloth);
            b.PaintEll(right, V(0.3f, 0.6f, -0.03f), V(0.1f, 0.005f, 0.05f), Vine, V(0, 35f, -15f), 0.004f);
            return Lift(b);
        }

        /// <summary>Carturel: an old leather book of tales hovering open, its covers spread like wings, its leaves fluttering, pale eyes in the gutter and a bookmark ribbon for a tail.</summary>
        public static PokeBuilder Carturel()
        {
            var b = new PokeBuilder("Carturel", 0.75f, BodyPlan.Floating, V(0, 0.5f, 0)) { Coat = Leather }.Hover();
            var leather = Rgb(96, 64, 44);
            var gold = Rgb(212, 172, 82);
            var ribbon = Rgb(150, 48, 70);
            // The spine at the back, and the dark gutter in front of it where the eyes are
            b.Ell(Body, V(0, 0.5f, -0.05f), V(0.09f, 0.3f, 0.07f), leather, mat: Leather);
            PokeBuilder.Both(s => b.PaintEll(Body, V(0, 0.5f + 0.24f * s, -0.05f), V(0.1f, 0.014f, 0.08f), gold, default, 0.006f));
            var gc = V(0, 0.52f, 0.01f);
            var gr = V(0.1f, 0.14f, 0.05f);
            b.Ell(Body, gc, gr, Rgb(50, 36, 30), mat: Leather, blend: 0.02f);
            PokeBuilder.Both(s =>
            {
                var at = On(gc, gr, 0.042f * s, 0.53f);
                b.Eye(Body, at, Outward(gc, gr, at), 0.022f, sclera: true, white: Rgb(214, 246, 240), pupil: Rgb(150, 220, 214));
            });
            // The covers opened forward like wings, the leaves fanned inside each
            PokeBuilder.Both(s =>
            {
                int wing = b.Wing(s, V(0.06f * s, 0.5f, -0.04f));
                var dir = V(0.819f * s, 0, 0.574f);
                var inward = V(-0.574f * s, 0, 0.819f);
                float yaw = s > 0 ? -35f : -145f;
                var root = V(0.06f * s, 0.5f, -0.04f);
                b.Box(wing, root + dir * 0.21f, V(0.21f, 0.3f, 0.018f), 0.016f, leather, V(0, yaw, 0), Leather);
                foreach (float t in new[] { 0.06f, 0.33f, 0.6f, 0.87f })
                    b.PaintEll(wing, root + dir * (0.04f + 0.36f * t) - inward * 0.02f, V(0.025f, 0.025f, 0.03f), gold, V(0, yaw, 0), 0.006f);
                for (int k = 0; k < 3; k++)
                {
                    float lift = k * 0.03f;
                    var at = root + dir * (0.18f - 0.01f * k) + inward * (0.028f + 0.022f * k) + V(0, lift, 0);
                    b.Ell(wing, at, V(0.18f, 0.27f - 0.01f * k, 0.011f), Paper, V(0, yaw - s * 6f * k, 0), Cloth, 0.008f);
                }
                // A tale's picture on the top leaf
                var pic = root + dir * 0.2f + inward * 0.08f + V(0, 0.08f, 0);
                b.PaintEll(wing, pic, V(0.05f, 0.05f, 0.03f), Rgb(150, 160, 190), V(0, yaw, 0), 0.008f);
                b.PaintEll(wing, pic + V(0, -0.02f, 0), V(0.03f, 0.03f, 0.035f), Rgb(170, 120, 90), V(0, yaw, 0), 0.006f);
                b.PaintEll(wing, root + dir * 0.18f + inward * 0.08f + V(0, -0.12f, 0), V(0.11f, 0.004f, 0.03f), Rgb(140, 116, 100), V(0, yaw, 0), 0.004f);
                b.PaintEll(wing, root + dir * 0.18f + inward * 0.08f + V(0, -0.16f, 0), V(0.11f, 0.004f, 0.03f), Rgb(140, 116, 100), V(0, yaw, 0), 0.004f);
            });
            // The bookmark ribbon trailing as a tail
            int tail = b.Tail(V(0, 0.24f, -0.08f));
            var path = Smooth(3, V(0, 0.27f, -0.09f), V(-0.06f, 0.14f, -0.2f), V(-0.18f, 0.08f, -0.24f), V(-0.3f, 0.06f, -0.18f), V(-0.38f, 0.02f, -0.1f));
            b.Tube(tail, path, 0.024f, 0.02f, ribbon, Cloth, 0.008f);
            foreach (float dz in new[] { -0.03f, 0f, 0.03f })
                b.Spike(tail, path[^1], path[^1] + V(-0.07f, -0.02f, dz), 0.016f, gold, mat: Cloth);
            return Lift(b);
        }

        // ------------------------------------------------------------------ Steluta line

        /// <summary>
        /// Steluta and Stelar: a carollers' paper star on a wooden stick, red points and silver between, a candle's glow
        /// at its middle with the face in it; Stelar's star bigger, edged in gold, many-pointed, streamers trailing from
        /// its points, flames at its upper points and its mouth open in song.
        /// </summary>
        public static PokeBuilder Star(bool big)
        {
            float k = big ? 1.4f : 1f;
            var b = new PokeBuilder(big ? "Stelar" : "Steluta", big ? 0.85f : 0.5f, BodyPlan.Floating, V(0, 0.66f * k, 0)) { Coat = Cloth }.Hover();
            var wood = Rgb(140, 96, 60);
            var gold = Rgb(226, 182, 84);
            var c = V(0, 0.66f * k, 0);
            float hubR = 0.11f * k;
            // The points: red at the four quarters (and between them on Stelar), silver behind between them
            int reds = big ? 8 : 4;
            for (int i = 0; i < reds; i++)
            {
                float a = i * MathF.Tau / reds + (big ? 0f : MathF.PI / 2f);
                var dir = V(MathF.Cos(a), MathF.Sin(a), 0);
                float len = (big && i % 2 == 1 ? 0.3f : 0.36f) * k;
                if (big) Blade(b, Body, c + dir * hubR * 0.6f + V(0, 0, -0.012f), c + dir * (len + 0.03f), 0.09f * k, gold, V(0, 0, 1f), 0.32f, Shell);
                Blade(b, Body, c + dir * hubR * 0.6f, c + dir * len, 0.075f * k, StarRed, V(0, 0, 1f), 0.36f, Shell);
            }
            for (int i = 0; i < reds; i++)
            {
                float a = (i + 0.5f) * MathF.Tau / reds + (big ? 0f : MathF.PI / 2f);
                var dir = V(MathF.Cos(a), MathF.Sin(a), 0);
                Blade(b, Body, c + dir * hubR * 0.6f + V(0, 0, -0.02f), c + dir * 0.27f * k + V(0, 0, -0.02f), 0.06f * k, StarSilver, V(0, 0, 1f), 0.34f, Shell);
            }
            // The glowing middle, its eight-sided rim, and the face in its light
            var hr = V(hubR, hubR, 0.06f * k);
            b.Ell(Body, c + V(0, 0, 0.01f), hr, Candle, mat: Glow, blend: 0.015f);
            b.Torus(Body, c + V(0, 0, 0.01f), hubR, 0.014f * k, big ? gold : Rgb(232, 186, 100), V(90f, 0, 0), mat: Shell, blend: 0.006f);
            var hc = c + V(0, 0, 0.01f);
            PokeBuilder.Both(s =>
            {
                var at = On(hc, hr, 0.036f * k * s, c.Y + 0.012f * k);
                b.Eye(Body, at, Outward(hc, hr, at), 0.017f * k, Rgb(80, 40, 30), closed: big);
            });
            if (big)
            {
                var m = On(hc, hr, 0, c.Y - 0.04f * k);
                b.PaintEll(Body, m, V(0.03f * k, 0.022f * k, 0.03f), Rgb(150, 50, 40), default, 0.005f);
                b.PaintEll(Body, m + V(0, -0.008f * k, 0.005f), V(0.016f * k, 0.01f * k, 0.03f), Rgb(240, 120, 110), default, 0.004f);
            }
            else b.Mark(Body, On(hc, hr, 0, c.Y - 0.03f * k), V(0, -0.2f, 1f), 0.016f, 0.01f, Rgb(80, 40, 30), MarkShape.Smile);

            // The stick, ribbons tied round its top
            b.Limb(Body, c + V(0, -0.08f * k, -0.02f), V(0, 0.08f, -0.02f), 0.028f * k, 0.024f * k, wood, Shell);
            b.Torus(Body, V(0, c.Y - 0.15f * k, -0.02f), 0.03f * k, 0.012f * k, StarRed, mat: Cloth, blend: 0.006f);
            if (big)
            {
                // Gold and red ribbon wound round the pole
                foreach (var (phase, col) in new[] { (0f, gold), (MathF.PI, StarRed) })
                {
                    var pts = new Vector3[25];
                    for (int i = 0; i < pts.Length; i++)
                    {
                        float t = i / 24f, a = phase + t * MathF.Tau * 2.5f;
                        pts[i] = V(MathF.Sin(a) * 0.034f, c.Y - 0.18f * k - t * 0.5f, MathF.Cos(a) * 0.034f - 0.02f);
                    }
                    b.Tube(Body, pts, 0.015f, 0.015f, col, Cloth, 0.004f);
                }
            }
            int ribbons = b.Part("ribbons", Body, V(0, c.Y - 0.16f * k, 0), PokeRole.Leaf);
            foreach (var (dx, col, len) in new[] { (-0.03f, StarRed, 0.14f), (-0.01f, StarSilver, 0.18f), (0.015f, StarRed, 0.16f), (0.035f, StarSilver, 0.12f) })
                Ribbon(b, ribbons, V(dx * k, c.Y - 0.155f * k, 0.01f), V(dx * 3f, -1f, 0.15f), len * k, 0.016f * k, col);

            if (big)
            {
                // Streamers from the side points, with tassels; flames at the upper points
                PokeBuilder.Both(s =>
                {
                    int side = b.Part(s < 0 ? "streamerL" : "streamerR", Body, c + V(0.36f * k * s, 0, 0), PokeRole.Leaf, s, s);
                    var tip = c + V(0.37f * k * s, 0, 0);
                    var p1 = tip + V(0.08f * s, -0.12f, 0.02f);
                    var p2 = p1 + V(-0.02f * s, -0.16f, 0.01f);
                    var p3 = p2 + V(0.06f * s, -0.14f, 0.0f);
                    b.Tube(side, Smooth(3, tip, p1, p2, p3), 0.018f, 0.015f, s > 0 ? StarRed : gold, Cloth, 0.006f);
                    b.Ell(side, p3 + V(0, -0.03f, 0), V(0.022f, 0.035f, 0.022f), StarSilver, default, Cloth, 0.008f);
                    var low = c + V(MathF.Cos(MathF.PI / 4f) * 0.26f * k * s, -MathF.Sin(MathF.PI / 4f) * 0.26f * k, 0);
                    b.Tube(side, Smooth(3, low, low + V(0.04f * s, -0.12f, 0.02f), low + V(0.0f, -0.25f, 0.02f)), 0.016f, 0.014f, s > 0 ? StarSilver : StarRed, Cloth, 0.006f);
                    b.Ell(side, low + V(0, -0.28f, 0.02f), V(0.02f, 0.032f, 0.02f), gold, default, Cloth, 0.008f);
                    var up = c + V(MathF.Cos(MathF.PI / 4f) * 0.355f * k * s, MathF.Sin(MathF.PI / 4f) * 0.355f * k, 0);
                    Flame(b, Body, up + V(0, -0.02f, 0), 0.22f);
                });
                Flame(b, Body, c + V(0, 0.48f, 0), 0.22f);
            }
            return Lift(b);
        }

        // ------------------------------------------------------------------ Mascuta line

        /// <summary>The red pattern of the winter masks, painted on the face ellipsoid at c with radii r.</summary>
        private static void MaskPaint(PokeBuilder b, int bone, Vector3 c, Vector3 r, float eyeX, float eyeY)
        {
            var front = c + V(0, 0, r.Z * 0.85f);
            b.PaintEll(bone, V(0, c.Y + r.Y * 0.72f, front.Z), V(r.X * 0.32f, r.Y * 0.28f, r.Z), MaskRed, default, 0.008f);
            PokeBuilder.Both(s =>
            {
                b.PaintEll(bone, V(eyeX * s, eyeY, front.Z), V(r.X * 0.36f, r.Y * 0.26f, r.Z), MaskRed, default, 0.008f);
                b.PaintEll(bone, V(r.X * 0.62f * s, c.Y - r.Y * 0.25f, front.Z - r.Z * 0.3f), V(r.X * 0.28f, r.Y * 0.4f, r.Z), MaskRed, V(0, 0, 18f * s), 0.008f);
                b.PaintEll(bone, V(r.X * 0.3f * s, c.Y - r.Y * 0.62f, front.Z), V(r.X * 0.22f, r.Y * 0.16f, r.Z), MaskRed, V(0, 0, -30f * s), 0.008f);
            });
        }

        /// <summary>Mascuta: a little carved wooden mask painted red and white, stub horns and a fringe of wool, walking on two wooden legs, nothing but darkness and two glints in its eyeholes.</summary>
        public static PokeBuilder Mascuta()
        {
            var b = new PokeBuilder("Mascuta", 0.5f, BodyPlan.Biped, V(0, 0.36f, 0)) { Coat = Shell };
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.08f * s, 0.16f, -0.02f));
                b.Limb(leg, V(0.08f * s, 0.18f, -0.03f), V(0.09f * s, 0.04f, -0.01f), 0.036f, 0.028f, Wood, Shell);
                b.Ell(leg, V(0.09f * s, 0.025f, 0.005f), V(0.032f, 0.025f, 0.04f), PixelCanvas.Shadow(Wood, 0.2f), mat: Shell);
            });
            b.Ell(Body, V(0, 0.38f, -0.05f), V(0.17f, 0.22f, 0.12f), Wood, mat: Shell);
            b.Ell(Body, V(0, 0.48f, -0.1f), V(0.2f, 0.16f, 0.13f), Wool, mat: Fur);
            FurTufts(b, Body, V(0, 0.48f, -0.1f), V(0.2f, 0.16f, 0.13f), 16, 0.07f, 0.03f, Wool, 0.4f, -0.3f, 0.2f);
            var c = V(0, 0.38f, 0.03f);
            var r = V(0.18f, 0.24f, 0.08f);
            b.Ell(Body, c, r, MaskWhite, mat: Shell, blend: 0.02f);
            MaskPaint(b, Body, c, r, 0.065f, 0.425f);
            PokeBuilder.Both(s =>
            {
                b.Limb(Body, V(0.13f * s, 0.55f, -0.02f), V(0.19f * s, 0.65f, -0.03f), 0.032f, 0.027f, Wood, Shell);
                var at = On(c, r, 0.065f * s, 0.425f);
                b.Eye(Body, at, Outward(c, r, at), 0.03f, Rgb(28, 22, 20));
            });
            b.Mark(Body, On(c, r, 0, 0.27f), V(0, -0.1f, 1f), 0.03f, 0.009f, Rgb(70, 44, 36), MarkShape.Bar);
            return b;
        }

        /// <summary>Mascaron: a tall winter-dance mask with curling horns over a long sheepskin cloak, a dark woody body in its opening, bells at its hem and wooden legs beneath.</summary>
        public static PokeBuilder Mascaron()
        {
            var b = new PokeBuilder("Mascaron", 0.95f, BodyPlan.Biped, V(0, 0.6f, 0)) { Coat = Fur };
            var fleeceShade = Rgb(206, 192, 166);
            var dark = Rgb(70, 76, 58);
            var horn = Rgb(140, 112, 82);
            // The cloak, cut at its hem before the legs are added
            b.Limb(Body, V(0, 0.9f, -0.02f), V(0, 0.2f, -0.04f), 0.14f, 0.36f, Wool, Fur);
            b.CutBox(Body, V(0, -0.3f, 0), V(0.9f, 0.42f, 0.9f), Quaternion.Identity);
            b.PaintEll(Body, V(0, 0.48f, 0.3f), V(0.07f, 0.34f, 0.12f), dark, default, 0.01f);
            FurTufts(b, Body, V(0, 0.84f, -0.02f), V(0.22f, 0.14f, 0.2f), 20, 0.12f, 0.045f, Wool, 0.75f, -0.5f, 0.6f);
            for (int i = 0; i < 18; i++)
            {
                float a = i / 18f * MathF.Tau;
                var root = V(MathF.Sin(a) * 0.31f, 0.24f, MathF.Cos(a) * 0.31f - 0.04f);
                b.Spike(Body, root, V(MathF.Sin(a) * 0.41f, 0.1f, MathF.Cos(a) * 0.41f - 0.04f), 0.05f, i % 2 == 0 ? Wool : fleeceShade, 0.6f, Fur);
            }
            for (int i = 0; i < 7; i++)
            {
                float a = (-75f + i * 25f) * Degree;
                var at = V(MathF.Sin(a) * 0.35f, 0.15f, MathF.Cos(a) * 0.35f - 0.04f);
                b.Ell(Body, at, V(0.034f, 0.04f, 0.034f), i % 2 == 0 ? Rgb(204, 164, 72) : Rgb(172, 172, 180), mat: Metal, blend: 0.01f);
                b.Ell(Body, at + V(0, -0.035f, 0), V(0.026f, 0.012f, 0.026f), Rgb(60, 54, 50), mat: Metal, blend: 0.006f);
            }
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.12f * s, 0.25f, 0));
                b.Limb(leg, V(0.12f * s, 0.27f, 0.02f), V(0.14f * s, 0.05f, 0.04f), 0.055f, 0.045f, Wood, Shell);
                b.Ell(leg, V(0.14f * s, 0.03f, 0.07f), V(0.06f, 0.03f, 0.08f), PixelCanvas.Shadow(Wood, 0.2f), mat: Shell);
                foreach (float dx in new[] { -0.035f, 0f, 0.035f })
                    b.Spike(leg, V((0.14f + dx) * s, 0.025f, 0.12f), V((0.14f + dx * 1.3f) * s, 0.015f, 0.17f), 0.016f, Rgb(90, 70, 50), mat: Shell);
            });

            // The mask: longer than Mascuta's, a fleece hood round it and ram's horns curling from it
            int head = b.Head(V(0, 0.92f, 0.08f));
            b.Ell(head, V(0, 1.0f, -0.02f), V(0.18f, 0.2f, 0.16f), Wool, mat: Fur);
            b.Ell(head, V(0, 1.0f, 0.08f), V(0.13f, 0.18f, 0.08f), Wood, mat: Shell);
            var c = V(0, 1.0f, 0.15f);
            var r = V(0.14f, 0.2f, 0.07f);
            b.Ell(head, c, r, MaskWhite, mat: Shell, blend: 0.02f);
            MaskPaint(b, head, c, r, 0.052f, 1.04f);
            PokeBuilder.Both(s =>
            {
                var at = On(c, r, 0.052f * s, 1.04f);
                b.Eye(head, at, Outward(c, r, at), 0.027f, Rgb(22, 18, 18));
                b.Tube(head, Smooth(3, V(0.09f * s, 1.12f, 0.1f), V(0.2f * s, 1.27f, 0.02f), V(0.32f * s, 1.3f, -0.06f), V(0.38f * s, 1.2f, -0.04f),
                    V(0.35f * s, 1.1f, 0.03f), V(0.3f * s, 1.12f, 0.08f)), 0.045f, 0.016f, horn, Shell, 0.012f);
            });
            b.Mark(head, On(c, r, 0, 0.88f), V(0, -0.1f, 1f), 0.03f, 0.009f, Rgb(60, 36, 30), MarkShape.Bar);
            return b;
        }

        // ------------------------------------------------------------------ Sperietoare

        /// <summary>Sperietoare: a scarecrow of the maize fields, a straw body in an embroidered shirt on a pole, ragged sleeves on its crossbar, crows perched calmly, a battered hat over two glowing pinpricks.</summary>
        public static PokeBuilder Sperietoare()
        {
            var b = new PokeBuilder("Sperietoare", 0.9f, BodyPlan.Biped, V(0, 0.75f, 0)) { Coat = Cloth };
            var shirt = Rgb(234, 226, 206);
            var stitch = Rgb(184, 40, 46);
            var straw = Rgb(214, 180, 96);
            var strawDark = Rgb(168, 134, 70);
            var wood = Rgb(124, 92, 60);
            var hat = Rgb(190, 160, 96);
            var corn = Rgb(236, 196, 70);

            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.07f * s, 0.42f, 0));
                b.Limb(leg, V(0.07f * s, 0.46f, 0), V(0.11f * s, 0.13f, 0.02f), 0.048f, 0.032f, straw, Leaf);
                b.Torus(leg, V(0.1f * s, 0.18f, 0.015f), 0.034f, 0.012f, Rgb(160, 130, 90), mat: Cloth, blend: 0.004f);
                var foot = V(0.11f * s, 0.11f, 0.02f);
                foreach (var d in new[] { V(-0.06f, -0.1f, 0.05f), V(0.0f, -0.11f, 0.07f), V(0.06f, -0.1f, 0.04f), V(0.03f, -0.1f, -0.05f) })
                    b.Limb(leg, foot, foot + V(d.X * s, d.Y, d.Z), 0.017f, 0.013f, wood, Shell);
            });
            b.Limb(Body, V(0, 0.62f, -0.03f), V(0, 0.16f, -0.03f), 0.028f, 0.026f, wood, Shell);
            // The shirt, a rope belt and a skirt of straw below it
            b.Ell(Body, V(0, 0.73f, 0), V(0.17f, 0.2f, 0.12f), shirt, mat: Cloth);
            for (int i = 0; i < 16; i++)
            {
                float a = i / 16f * MathF.Tau;
                var root = V(MathF.Sin(a) * 0.12f, 0.6f, MathF.Cos(a) * 0.085f);
                b.Spike(Body, root, V(MathF.Sin(a) * 0.22f, 0.36f, MathF.Cos(a) * 0.16f), 0.034f, i % 2 == 0 ? straw : strawDark, 0.5f, Leaf);
            }
            b.Torus(Body, V(0, 0.585f, 0), 0.125f, 0.02f, Rgb(196, 170, 120), sz: 0.72f, mat: Cloth, blend: 0.006f);
            b.PaintEll(Body, V(0, 0.81f, 0.11f), V(0.035f, 0.09f, 0.04f), stitch, default, 0.006f);
            PokeBuilder.Both(s =>
            {
                b.PaintEll(Body, V(0.06f * s, 0.85f, 0.1f), V(0.012f, 0.05f, 0.04f), stitch, V(0, 0, 25f * s), 0.004f);
                b.PaintEll(Body, V(0.065f * s, 0.73f, 0.11f), V(0.018f, 0.018f, 0.04f), stitch, V(0, 0, 45f), 0.004f);
            });
            // Corn tucked in the belt
            b.Ell(Body, V(0.08f, 0.62f, 0.1f), V(0.025f, 0.055f, 0.025f), corn, V(0, 0, -25f), Leaf, 0.01f);
            b.Spike(Body, V(0.07f, 0.6f, 0.1f), V(0.13f, 0.7f, 0.12f), 0.02f, Rgb(120, 160, 80), 0.4f, Leaf);
            b.Ell(Body, V(-0.12f, 0.5f, 0.1f), V(0.022f, 0.05f, 0.022f), corn, V(0, 0, 30f), Leaf, 0.01f);

            // The crossbar arms in ragged sleeves; crows perched on them
            PokeBuilder.Both(s =>
            {
                int arm = b.Arm(s, V(0.12f * s, 0.86f, 0));
                b.Limb(arm, V(0.08f * s, 0.86f, -0.02f), V(0.6f * s, 0.88f, -0.02f), 0.028f, 0.025f, wood, Shell);
                b.Ell(arm, V(0.27f * s, 0.845f, -0.01f), V(0.18f, 0.07f, 0.075f), shirt, V(0, 0, -5f * s), Cloth, 0.02f);
                foreach (float x in new[] { 0.22f, 0.3f, 0.38f, 0.44f })
                    b.Spike(arm, V(x * s, 0.8f, -0.01f), V((x + 0.02f) * s, 0.64f + (x % 0.08f), -0.01f), 0.03f, shirt, 0.45f, Cloth);
                b.PaintEll(arm, V(0.42f * s, 0.845f, -0.01f), V(0.022f, 0.08f, 0.09f), stitch, default, 0.006f);
            });
            Crow(b, b.Model.Skeleton.Count - 1, V(0.53f, 0.875f, -0.02f), 0.6f);
            Crow(b, b.Model.Skeleton.Count - 2, V(-0.48f, 0.875f, -0.02f), -0.4f);

            // A head of straw, its face in shadow, two glowing pinpricks under a battered wide hat
            int head = b.Head(V(0, 0.93f, 0));
            var c = V(0, 1.0f, 0.0f);
            var r = V(0.1f, 0.11f, 0.095f);
            b.Ell(head, c, r, straw, mat: Leaf);
            for (int i = 0; i < 8; i++)
            {
                float a = (i + 0.5f) / 8f * MathF.Tau;
                b.Spike(head, V(MathF.Sin(a) * 0.07f, 0.93f, MathF.Cos(a) * 0.06f), V(MathF.Sin(a) * 0.1f, 0.86f, MathF.Cos(a) * 0.09f), 0.022f, strawDark, 0.5f, Leaf);
            }
            b.PaintEll(head, V(0, 0.98f, 0.075f), V(0.08f, 0.07f, 0.05f), Rgb(54, 42, 30), default, 0.012f);
            PokeBuilder.Both(s =>
            {
                var at = On(c, r, 0.035f * s, 0.995f);
                b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(255, 204, 80));
            });
            b.Ell(head, V(0, 1.07f, 0), V(0.21f, 0.018f, 0.2f), hat, V(8f, 0, 4f), Cloth, 0.01f);
            b.Ell(head, V(0, 1.12f, -0.005f), V(0.09f, 0.07f, 0.09f), hat, V(0, 0, -6f), Cloth, 0.02f);
            b.Torus(head, V(0, 1.09f, -0.005f), 0.086f, 0.012f, Rgb(120, 90, 60), mat: Cloth, blend: 0.004f);
            b.PaintEll(head, V(0.06f, 1.15f, 0.05f), V(0.025f, 0.02f, 0.03f), PixelCanvas.Shadow(hat, 0.25f), default, 0.006f);
            return b;
        }

        // ------------------------------------------------------------------ Sobitel line

        /// <summary>The fire door of a tiled stove: an iron frame round its glow, its little door swung open to the left.</summary>
        private static void FireDoor(PokeBuilder b, Vector3 at, float w, float h)
        {
            b.Box(Body, at, V(w, h, 0.012f), 0.008f, Iron, mat: Metal);
            b.Box(Body, at + V(0, 0, 0.007f), V(w * 0.72f, h * 0.7f, 0.009f), 0.005f, FireGlow, mat: Glow);
            var hinge = at + V(-w, 0, 0.012f);
            var dir = V(-0.5f, 0, 0.866f);
            b.Box(Body, hinge + dir * w * 0.5f, V(w * 0.52f, h * 0.9f, 0.007f), 0.004f, Iron, V(0, -120f, 0), Metal);
            foreach (float t in new[] { 0.3f, 0.6f })
                b.Box(Body, hinge + dir * w * t + V(0.006f, 0, 0.004f), V(0.006f, h * 0.6f, 0.006f), 0.002f, Rgb(40, 38, 42), V(0, -120f, 0), Metal);
        }

        /// <summary>A round iron vent for an eye: a ring set into the tiles round the eye's decal.</summary>
        private static void VentEye(PokeBuilder b, Vector3 at, float size)
        {
            b.Torus(Body, at + V(0, 0, 0.002f), size * 1.45f, size * 0.32f, Iron, V(90f, 0, 0), mat: Metal, blend: 0.004f);
            b.Eye(Body, at, V(0, 0, 1f), size, FireGlow, glare: true);
        }

        /// <summary>Sobitel: a small square stove of glazed green tiles on iron feet, a fire door for a mouth, round vents for eyes, a chimney pipe puffing a wisp.</summary>
        public static PokeBuilder Sobitel()
        {
            var b = new PokeBuilder("Sobitel", 0.55f, BodyPlan.Quadruped, V(0, 0.36f, 0)) { Coat = Shell };
            var grout = Rgb(150, 160, 130);
            foreach (var (z, front) in new[] { (0.13f, true), (-0.13f, false) })
                PokeBuilder.Both(s =>
                {
                    int leg = b.Leg(s, V(0.13f * s, 0.12f, z), front);
                    b.Box(leg, V(0.13f * s, 0.085f, z), V(0.05f, 0.085f, 0.05f), 0.012f, Iron, mat: Metal);
                });
            var c = V(0, 0.37f, 0);
            b.Box(Body, c, V(0.21f, 0.21f, 0.21f), 0.035f, TileGreen, mat: Shell);
            b.PaintEll(Body, V(-0.08f, 0.5f, 0.2f), V(0.1f, 0.06f, 0.03f), PixelCanvas.Light1(TileGreen, 0.2f), default, 0.02f);
            foreach (float y in new[] { 0.3f, 0.48f })
                Course(b, Body, c, 0.21f, 0.21f, y, grout);
            foreach (float x in new[] { -0.07f, 0.07f })
            {
                b.PaintEll(Body, V(x, 0.37f, 0.21f), V(0.005f, 0.17f, 0.03f), grout, default, 0.004f);
                b.PaintEll(Body, V(0.21f, 0.37f, x), V(0.03f, 0.17f, 0.005f), grout, default, 0.004f);
                b.PaintEll(Body, V(-0.21f, 0.37f, x), V(0.03f, 0.17f, 0.005f), grout, default, 0.004f);
                b.PaintEll(Body, V(x, 0.58f, 0), V(0.005f, 0.03f, 0.18f), grout, default, 0.004f);
                b.PaintEll(Body, V(0, 0.58f, x), V(0.18f, 0.03f, 0.005f), grout, default, 0.004f);
            }
            b.Box(Body, V(0, 0.17f, 0), V(0.225f, 0.024f, 0.225f), 0.012f, Iron, mat: Metal);
            b.Box(Body, V(0, 0.565f, 0), V(0.222f, 0.016f, 0.222f), 0.01f, Iron, mat: Metal);
            FireDoor(b, V(0, 0.28f, 0.215f), 0.08f, 0.06f);
            PokeBuilder.Both(s => VentEye(b, V(0.085f * s, 0.425f, 0.211f), 0.03f));
            // The chimney pipe and its wisp
            int pipe = b.Part("chimney", Body, V(0.06f, 0.58f, -0.06f), PokeRole.Flame);
            b.Limb(pipe, V(0.06f, 0.56f, -0.06f), V(0.06f, 0.66f, -0.06f), 0.03f, 0.028f, Iron, Metal);
            b.Limb(pipe, V(0.06f, 0.66f, -0.06f), V(0.1f, 0.72f, -0.06f), 0.028f, 0.026f, Iron, Metal);
            b.Tube(pipe, Smooth(3, V(0.1f, 0.74f, -0.06f), V(0.13f, 0.81f, -0.06f), V(0.09f, 0.87f, -0.06f), V(0.12f, 0.94f, -0.06f)), 0.022f, 0.016f, Rgb(214, 214, 220), Glow, 0.008f);
            return b;
        }

        /// <summary>Teracotor: a big village stove of green and cream tiles with flowers in relief, a glowing fire door, a stout chimney, short iron legs and tiled arms whose grates glow.</summary>
        public static PokeBuilder Teracotor()
        {
            var b = new PokeBuilder("Teracotor", 0.95f, BodyPlan.Biped, V(0, 0.65f, 0)) { Coat = Shell };
            var green = Rgb(52, 118, 62);
            var cream = Rgb(232, 222, 192);
            var grout = Rgb(146, 150, 124);
            var relief = Rgb(150, 172, 120);
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.2f * s, 0.28f, 0));
                b.Box(leg, V(0.2f * s, 0.15f, 0.02f), V(0.1f, 0.15f, 0.1f), 0.02f, Iron, mat: Metal);
            });
            var c = V(0, 0.62f, 0);
            b.Box(Body, c, V(0.34f, 0.34f, 0.28f), 0.04f, green, mat: Shell);
            // The cream band of relief tiles round its upper half, and the grout
            b.PaintEll(Body, V(0, 0.84f, 0.28f), V(0.37f, 0.11f, 0.05f), cream, default, 0.006f);
            PokeBuilder.Both(s => b.PaintEll(Body, V(0.34f * s, 0.84f, 0), V(0.05f, 0.11f, 0.3f), cream, default, 0.006f));
            b.PaintEll(Body, V(0, 0.84f, -0.28f), V(0.37f, 0.11f, 0.05f), cream, default, 0.006f);
            Course(b, Body, c, 0.34f, 0.28f, 0.48f, grout);
            Course(b, Body, c, 0.34f, 0.28f, 0.72f, grout);
            foreach (float x in new[] { -0.22f, 0.0f, 0.22f })
                b.PaintEll(Body, V(x, 0.84f, 0.28f), V(0.005f, 0.11f, 0.03f), grout, default, 0.004f);
            foreach (float x in new[] { -0.11f, 0.11f })
                b.Mark(Body, V(x, 0.84f, 0.281f), V(0, 0, 1f), 0.04f, 0.04f, relief, MarkShape.Star5);
            b.Box(Body, V(0, 0.29f, 0), V(0.35f, 0.025f, 0.29f), 0.012f, Iron, mat: Metal);
            b.Box(Body, V(0, 0.955f, 0), V(0.352f, 0.02f, 0.292f), 0.012f, Iron, mat: Metal);
            foreach (float x in new[] { -0.33f, 0.33f })
                foreach (float z in new[] { -0.27f, 0.27f })
                    b.Ell(Body, V(x, 0.95f, z), V(0.045f, 0.035f, 0.045f), Iron, mat: Metal, blend: 0.008f);
            // Its stepped top and chimney
            b.Box(Body, V(0, 1.0f, 0), V(0.26f, 0.04f, 0.2f), 0.02f, cream, mat: Shell);
            b.Box(Body, V(0, 1.06f, 0), V(0.2f, 0.03f, 0.15f), 0.015f, green, mat: Shell);
            int pipe = b.Part("chimney", Body, V(0, 1.09f, -0.02f), PokeRole.Flame);
            b.Limb(pipe, V(0, 1.08f, -0.02f), V(0, 1.2f, -0.02f), 0.055f, 0.052f, Iron, Metal);
            b.Torus(pipe, V(0, 1.12f, -0.02f), 0.055f, 0.012f, Rgb(44, 42, 46), mat: Metal, blend: 0.004f);
            b.Limb(pipe, V(0, 1.2f, -0.02f), V(0.08f, 1.27f, -0.02f), 0.05f, 0.048f, Iron, Metal);
            b.Tube(pipe, Smooth(3, V(0.1f, 1.3f, -0.02f), V(0.16f, 1.34f, -0.02f), V(0.2f, 1.33f, -0.02f), V(0.26f, 1.37f, -0.02f)), 0.034f, 0.024f, Rgb(240, 186, 140), Glow, 0.01f);
            FireDoor(b, V(0, 0.46f, 0.28f), 0.13f, 0.095f);
            PokeBuilder.Both(s => VentEye(b, V(0.13f * s, 0.65f, 0.281f), 0.036f));

            // Tiled pillar arms reaching the ground, their grates glowing
            PokeBuilder.Both(s =>
            {
                int arm = b.Arm(s, V(0.38f * s, 0.84f, 0));
                b.Box(arm, V(0.45f * s, 0.84f, 0), V(0.11f, 0.08f, 0.12f), 0.02f, Iron, mat: Metal);
                b.Box(arm, V(0.47f * s, 0.64f, 0.02f), V(0.1f, 0.14f, 0.11f), 0.02f, cream, mat: Shell);
                b.Box(arm, V(0.49f * s, 0.37f, 0.04f), V(0.1f, 0.14f, 0.11f), 0.02f, green, mat: Shell);
                b.Box(arm, V(0.5f * s, 0.13f, 0.05f), V(0.12f, 0.11f, 0.12f), 0.02f, Iron, mat: Metal);
                b.Mark(arm, V(0.47f * s, 0.64f, 0.13f), V(0, 0, 1f), 0.035f, 0.035f, relief, MarkShape.Star5);
                foreach (float dx in new[] { -0.06f, 0f, 0.06f })
                    b.Box(arm, V(0.5f * s + dx, 0.13f, 0.172f), V(0.012f, 0.065f, 0.006f), 0.003f, FireGlow, mat: Glow);
            });
            return b;
        }

        // ------------------------------------------------------------------ Cumpanel

        /// <summary>Cumpanel: a mossy stone well on stump legs, a shingle roof on two posts, a well-sweep's long pole balanced on a forked post with a bucket on its end, and two eyes in the dark water.</summary>
        public static PokeBuilder Cumpanel()
        {
            var b = new PokeBuilder("Cumpanel", 0.9f, BodyPlan.Quadruped, V(0, 0.35f, 0)) { Coat = Shell };
            var stone = Rgb(140, 146, 134);
            var grout = Rgb(92, 98, 88);
            var moss = Rgb(96, 140, 70);
            var wood = Rgb(120, 94, 66);
            var roof = Rgb(98, 76, 56);
            // The well, cut flat at the top and the bottom and hollowed, before the legs
            b.Limb(Body, V(0, 0.2f, 0), V(0, 0.46f, 0), 0.25f, 0.25f, stone, Shell);
            b.CutBox(Body, V(0, 0.9f, 0), V(0.7f, 0.4f, 0.7f), Quaternion.Identity);
            b.CutBox(Body, V(0, -0.3f, 0), V(0.7f, 0.44f, 0.7f), Quaternion.Identity);
            b.Cut(Body, V(0, 0.53f, 0), V(0.19f, 0.07f, 0.19f));
            b.Ell(Body, V(0, 0.466f, 0), V(0.2f, 0.012f, 0.2f), Rgb(22, 28, 36), mat: Shell, blend: 0.01f);
            b.Torus(Body, V(0, 0.5f, 0), 0.22f, 0.035f, stone, mat: Shell, blend: 0.01f);
            foreach (float y in new[] { 0.25f, 0.36f })
                b.PaintEll(Body, V(0, y, 0), V(0.36f, 0.006f, 0.36f), grout, default, 0.005f);
            for (int row = 0; row < 3; row++)
                for (int i = 0; i < 8; i++)
                {
                    float a = (i + (row % 2) * 0.5f) / 8f * MathF.Tau;
                    b.PaintEll(Body, V(MathF.Sin(a) * 0.25f, 0.195f + row * 0.11f, MathF.Cos(a) * 0.25f), V(0.006f, 0.05f, 0.03f), grout, V(0, a * 180f / MathF.PI, 0), 0.004f);
                }
            foreach (var (a, y, rx) in new[] { (0.4f, 0.22f, 0.07f), (2.4f, 0.42f, 0.08f), (-1.2f, 0.32f, 0.06f), (3.6f, 0.24f, 0.07f) })
                b.PaintEll(Body, V(MathF.Sin(a) * 0.25f, y, MathF.Cos(a) * 0.25f), V(rx, 0.04f, 0.05f), moss, V(0, a * 180f / MathF.PI, 0), 0.012f);
            PokeBuilder.Both(s => b.Eye(Body, V(0.065f * s, 0.479f, 0.04f), V(0, 1f, 0.6f), 0.03f, sclera: true, white: Rgb(170, 240, 222), pupil: Rgb(20, 40, 40)));
            foreach (var (z, front) in new[] { (0.15f, true), (-0.15f, false) })
                PokeBuilder.Both(s =>
                {
                    int leg = b.Leg(s, V(0.17f * s, 0.14f, z), front);
                    b.Limb(leg, V(0.17f * s, 0.17f, z), V(0.2f * s, 0.04f, z * 1.15f), 0.07f, 0.08f, wood, Shell);
                    b.Ell(leg, V(0.21f * s, 0.025f, z * 1.2f), V(0.08f, 0.025f, 0.09f), PixelCanvas.Shadow(wood, 0.15f), mat: Shell);
                });
            // The roof on its two posts
            PokeBuilder.Both(s => b.Limb(Body, V(0.18f * s, 0.48f, 0.02f), V(0.17f * s, 0.89f, 0.02f), 0.025f, 0.024f, wood, Shell));
            PokeBuilder.Both(s =>
            {
                b.Box(Body, V(0.11f * s, 0.92f, 0.02f), V(0.14f, 0.012f, 0.17f), 0.006f, roof, V(0, 0, -32f * s), Shell);
                foreach (float t in new[] { 0.04f, 0.1f, 0.16f })
                    b.PaintEll(Body, V((0.11f - 0.07f + t) * s, 0.92f - (t - 0.07f) * 0.62f + 0.014f, 0.02f), V(0.006f, 0.02f, 0.2f), PixelCanvas.Shadow(roof, 0.25f), V(0, 0, -32f * s), 0.004f);
            });
            // The forked post, and the sweep balanced on it: its counterweight, its rope and its bucket
            b.Limb(Body, V(0.23f, 0.2f, -0.1f), V(0.24f, 1.0f, -0.1f), 0.034f, 0.03f, wood, Shell);
            b.Spike(Body, V(0.24f, 0.98f, -0.1f), V(0.29f, 1.07f, -0.1f), 0.022f, wood, mat: Shell);
            b.Spike(Body, V(0.24f, 0.98f, -0.1f), V(0.19f, 1.07f, -0.1f), 0.022f, wood, mat: Shell);
            int sweep = b.Head(V(0.24f, 1.02f, -0.1f));
            b.Limb(sweep, V(0.42f, 0.935f, -0.18f), V(-0.46f, 1.3f, 0.12f), 0.026f, 0.02f, wood, Shell);
            b.Ell(sweep, V(0.47f, 0.9f, -0.2f), V(0.08f, 0.1f, 0.07f), stone, mat: Shell, blend: 0.015f);
            b.PaintEll(sweep, V(0.5f, 0.93f, -0.16f), V(0.04f, 0.04f, 0.04f), moss, default, 0.01f);
            b.Limb(sweep, V(-0.455f, 1.3f, 0.12f), V(-0.46f, 1.02f, 0.12f), 0.018f, 0.018f, Rgb(176, 152, 110), Cloth);
            b.Torus(sweep, V(-0.46f, 0.975f, 0.12f), 0.06f, 0.011f, Rgb(120, 120, 126), V(90f, 0, 0), mat: Metal, blend: 0.004f);
            b.Limb(sweep, V(-0.46f, 0.88f, 0.12f), V(-0.46f, 0.94f, 0.12f), 0.068f, 0.08f, wood, Shell);
            b.Torus(sweep, V(-0.46f, 0.92f, 0.12f), 0.078f, 0.011f, Rgb(120, 120, 126), mat: Metal, blend: 0.004f);
            b.Torus(sweep, V(-0.46f, 0.875f, 0.12f), 0.07f, 0.011f, Rgb(120, 120, 126), mat: Metal, blend: 0.004f);
            return b;
        }

        // ------------------------------------------------------------------ Pricolel line

        /// <summary>Pricolel: a scruffy black pup with oversized paws, ragged ears, a low tail, glowing yellow eyes and a wisp of mist at its feet.</summary>
        public static PokeBuilder Pricolel()
        {
            var b = new PokeBuilder("Pricolel", 0.55f, BodyPlan.Quadruped, V(0, 0.3f, 0)) { Coat = Fur };
            var claw = Rgb(140, 140, 150);
            foreach (var (z, front) in new[] { (0.13f, true), (-0.15f, false) })
                PokeBuilder.Both(s =>
                {
                    int leg = b.Leg(s, V(0.075f * s, 0.27f, z), front);
                    var knee = V(0.08f * s, 0.14f, z + (front ? 0.01f : -0.03f));
                    b.Limb(leg, V(0.075f * s, 0.29f, z), knee, 0.055f, 0.034f, NightBlack);
                    b.Limb(leg, knee, V(0.082f * s, 0.05f, z + 0.01f), 0.032f, 0.03f, NightGray);
                    b.Ell(leg, V(0.083f * s, 0.032f, z + 0.03f), V(0.046f, 0.032f, 0.058f), NightGray);
                    foreach (float dx in new[] { -0.025f, 0f, 0.025f })
                        b.Spike(leg, V((0.083f + dx) * s, 0.02f, z + 0.075f), V((0.083f + dx * 1.2f) * s, 0.008f, z + 0.1f), 0.01f, claw, mat: Shell);
                });
            var bc = V(0, 0.31f, -0.01f);
            b.Ell(Body, bc, V(0.115f, 0.1f, 0.19f), NightBlack);
            b.PaintEll(Body, V(0, 0.25f, 0.06f), V(0.08f, 0.05f, 0.14f), NightGray);
            FurTufts(b, Body, V(0, 0.36f, 0.08f), V(0.09f, 0.06f, 0.07f), 9, 0.07f, 0.024f, NightGray, -0.2f, 0.2f, 0.3f);
            FurTufts(b, Body, V(0, 0.34f, -0.06f), V(0.1f, 0.07f, 0.12f), 11, 0.065f, 0.022f, NightBlack, -0.3f, 0.1f, 0.25f);
            int tail = b.Tail(V(0, 0.33f, -0.19f));
            var tp = Smooth(3, V(0, 0.33f, -0.18f), V(0, 0.27f, -0.29f), V(0, 0.22f, -0.36f));
            b.Tube(tail, tp, 0.036f, 0.03f, NightBlack, blend: 0.01f);
            FurTufts(b, tail, tp[^1], V(0.03f, 0.03f, 0.04f), 6, 0.05f, 0.018f, NightBlack, -0.6f, -0.9f, 0.3f);
            // A big head for a pup, grey round its muzzle, one ear up and one flopped and torn
            int head = b.Head(V(0, 0.38f, 0.15f));
            var c = V(0, 0.43f, 0.21f);
            var r = V(0.085f, 0.077f, 0.08f);
            b.Limb(head, V(0, 0.35f, 0.12f), c, 0.07f, 0.06f, NightBlack);
            b.Ell(head, c, r, NightBlack);
            b.Ell(head, V(0, 0.4f, 0.3f), V(0.047f, 0.038f, 0.06f), NightGray, blend: 0.02f);
            b.Ell(head, V(0, 0.415f, 0.355f), V(0.018f, 0.013f, 0.012f), Black, mat: Shell, blend: 0.005f);
            FurTufts(b, head, V(0, 0.37f, 0.17f), V(0.07f, 0.05f, 0.05f), 8, 0.06f, 0.02f, NightGray, 0.5f, -0.3f, 0.4f);
            int earL = b.Ear(head, -1f, V(-0.05f, 0.49f, 0.19f));
            CatEar(b, earL, V(-0.045f, 0.49f, 0.19f), V(-0.085f, 0.6f, 0.16f), 0.03f, Rgb(80, 64, 76), NightBlack);
            int earR = b.Ear(head, 1f, V(0.05f, 0.49f, 0.19f));
            CatEar(b, earR, V(0.05f, 0.49f, 0.19f), V(0.15f, 0.53f, 0.2f), 0.03f, Rgb(80, 64, 76), NightBlack);
            b.Cut(earR, V(0.13f, 0.54f, 0.2f), V(0.012f, 0.014f, 0.03f));
            PokeBuilder.Both(s =>
            {
                var at = On(c, r, 0.04f * s, 0.448f);
                b.Eye(head, at, Outward(c, r, at), 0.018f, Rgb(250, 222, 60), glare: true);
            });
            GroundMist(b, 0.2f, 0.32f);
            return b;
        }

        /// <summary>Pricolici: the night dog of the tales on its hind legs, hunched and shaggy, long arms ending in curved claws, a bushy tail, eyes burning like coals and grey mist trailing from it.</summary>
        public static PokeBuilder Pricolici()
        {
            var b = new PokeBuilder("Pricolici", 0.95f, BodyPlan.Biped, V(0, 0.78f, 0)) { Coat = Fur };
            var claw = Rgb(74, 70, 76);
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.12f * s, 0.62f, -0.05f));
                b.Limb(leg, V(0.12f * s, 0.65f, -0.05f), V(0.15f * s, 0.4f, 0.03f), 0.085f, 0.06f, NightBlack);
                b.Limb(leg, V(0.15f * s, 0.4f, 0.03f), V(0.16f * s, 0.16f, -0.06f), 0.055f, 0.04f, NightGray);
                b.Limb(leg, V(0.16f * s, 0.16f, -0.06f), V(0.17f * s, 0.04f, 0.05f), 0.04f, 0.04f, NightGray);
                b.Ell(leg, V(0.17f * s, 0.032f, 0.08f), V(0.055f, 0.032f, 0.075f), NightGray);
                foreach (float dx in new[] { -0.03f, 0f, 0.03f })
                    b.Spike(leg, V((0.17f + dx) * s, 0.02f, 0.14f), V((0.17f + dx * 1.2f) * s, 0.006f, 0.18f), 0.012f, claw, mat: Shell);
            });
            b.Ell(Body, V(0, 0.68f, -0.06f), V(0.15f, 0.12f, 0.12f), NightBlack);
            b.Ell(Body, V(0, 0.82f, 0.02f), V(0.17f, 0.18f, 0.14f), NightBlack, V(25f, 0, 0));
            b.Ell(Body, V(0, 0.94f, 0.1f), V(0.2f, 0.15f, 0.15f), NightBlack);
            b.Ell(Body, V(0, 0.98f, -0.02f), V(0.17f, 0.12f, 0.12f), NightBlack); // the hunched back the shag grows from
            b.PaintEll(Body, V(0, 0.88f, 0.22f), V(0.09f, 0.1f, 0.06f), NightGray);
            FurTufts(b, Body, V(0, 1.0f, 0.04f), V(0.2f, 0.12f, 0.16f), 20, 0.12f, 0.04f, NightBlack, 0.55f, -0.3f, 0.15f);
            FurTufts(b, Body, V(0, 0.84f, 0.18f), V(0.1f, 0.08f, 0.05f), 8, 0.07f, 0.024f, NightGray, -0.5f, -0.8f, 0.6f);
            PokeBuilder.Both(s => b.Tube(Body, Smooth(3, V(0.18f * s, 1.02f, -0.02f), V(0.24f * s, 1.1f, -0.08f), V(0.21f * s, 1.18f, -0.1f), V(0.25f * s, 1.24f, -0.12f)),
                0.024f, 0.018f, Mist, Glow, 0.008f));
            // Long arms hanging to its knees, curved claws
            PokeBuilder.Both(s =>
            {
                int arm = b.Arm(s, V(0.18f * s, 0.96f, 0.1f));
                b.Limb(arm, V(0.18f * s, 0.96f, 0.1f), V(0.28f * s, 0.72f, 0.16f), 0.06f, 0.05f, NightBlack);
                b.Limb(arm, V(0.28f * s, 0.72f, 0.16f), V(0.3f * s, 0.47f, 0.18f), 0.05f, 0.04f, NightGray);
                b.Ell(arm, V(0.3f * s, 0.43f, 0.19f), V(0.048f, 0.055f, 0.042f), NightGray);
                foreach (float dx in new[] { -0.03f, -0.01f, 0.01f, 0.03f })
                    b.Tube(arm, Smooth(2, V(0.3f * s + dx, 0.4f, 0.21f), V(0.3f * s + dx * 1.2f, 0.34f, 0.24f), V(0.3f * s + dx * 1.3f, 0.3f, 0.22f)), 0.014f, 0.008f, claw, Shell);
            });
            // A wolf's head thrust forward, a ragged ear, fangs and burning eyes
            int head = b.Head(V(0, 1.08f, 0.17f));
            var c = V(0, 1.14f, 0.25f);
            var r = V(0.1f, 0.09f, 0.1f);
            b.Limb(head, V(0, 1.04f, 0.14f), c, 0.09f, 0.08f, NightBlack);
            b.Ell(head, c, r, NightBlack);
            b.Ell(head, V(0, 1.1f, 0.37f), V(0.055f, 0.045f, 0.085f), NightGray, blend: 0.02f);
            b.Ell(head, V(0, 1.12f, 0.45f), V(0.02f, 0.015f, 0.014f), Black, mat: Shell, blend: 0.005f);
            PokeBuilder.Both(s => b.Spike(head, V(0.03f * s, 1.075f, 0.4f), V(0.032f * s, 1.04f, 0.405f), 0.012f, Claw, mat: Shell));
            FurTufts(b, head, V(0, 1.1f, 0.2f), V(0.09f, 0.06f, 0.07f), 10, 0.07f, 0.024f, NightBlack, 0.4f, -0.4f, 0.3f);
            int earL = b.Ear(head, -1f, V(-0.06f, 1.21f, 0.21f));
            CatEar(b, earL, V(-0.055f, 1.21f, 0.21f), V(-0.09f, 1.35f, 0.17f), 0.038f, Rgb(80, 64, 76), NightBlack);
            int earR = b.Ear(head, 1f, V(0.06f, 1.21f, 0.21f));
            CatEar(b, earR, V(0.06f, 1.21f, 0.21f), V(0.17f, 1.27f, 0.2f), 0.036f, Rgb(80, 64, 76), NightBlack);
            b.Cut(earR, V(0.15f, 1.28f, 0.2f), V(0.014f, 0.016f, 0.04f));
            PokeBuilder.Both(s =>
            {
                var at = On(c, r, 0.045f * s, 1.165f);
                b.Eye(head, at, Outward(c, r, at), 0.02f, Rgb(255, 150, 40), glare: true);
            });
            int tail = b.Tail(V(0, 0.66f, -0.17f));
            var tp = Smooth(3, V(0, 0.66f, -0.16f), V(0, 0.52f, -0.32f), V(0, 0.38f, -0.42f));
            b.Tube(tail, tp, 0.07f, 0.09f, NightBlack, blend: 0.015f);
            FurTufts(b, tail, tp[^1], V(0.07f, 0.07f, 0.08f), 10, 0.09f, 0.03f, NightBlack, -0.8f, -0.9f, 0.3f);
            GroundMist(b, 0.34f, 0.36f);
            return b;
        }

        // ------------------------------------------------------------------ the three Fates

        /// <summary>A slender arm from the shoulder in a sleeve, the forearm bare, the hand at <paramref name="hand"/>.</summary>
        private static void FateArm(PokeBuilder b, int arm, float s, Vector3 shoulder, Vector3 elbow, Vector3 hand, Color sleeve, Color skin, float cuff)
        {
            b.Limb(arm, shoulder, elbow, 0.034f, 0.03f, sleeve, Cloth);
            b.Ell(arm, elbow + V(0, -0.02f, 0), V(cuff, 0.05f, cuff), sleeve, default, Cloth, 0.015f);
            b.Limb(arm, elbow, hand, 0.022f, 0.018f, skin);
            b.Ell(arm, hand + V(0.006f * s, -0.005f, 0.006f), V(0.025f, 0.028f, 0.022f), skin);
        }

        /// <summary>Ursivia: the spinner, tall in a white robe of spun wool folded round her, a lamb's long face, pink-lined ears and pale curls, a distaff of pink wool and a spindle, a golden thread looping about her.</summary>
        public static PokeBuilder Ursivia()
        {
            var b = new PokeBuilder("Ursivia", 0.9f, BodyPlan.Biped, V(0, 0.68f, 0)) { Coat = Fur };
            var robe = Rgb(246, 242, 228);
            var fold = Rgb(220, 212, 194);
            var skin = Rgb(206, 200, 236);
            var pink = Rgb(242, 172, 192);
            var woolPink = Rgb(246, 204, 216);
            Gown(b, 0.78f, 0.07f, 0.27f, robe);
            // Spiral folds round the gown, and the thread looping round it
            foreach (float phase in new[] { 0f, MathF.PI })
            {
                var pts = new Vector3[20];
                for (int i = 0; i < pts.Length; i++)
                {
                    float t = i / 19f, y = 0.72f - t * 0.66f, a = phase + t * MathF.Tau * 1.1f;
                    float rr = GownRadius(y, 0.78f, 0.07f, 0.27f);
                    pts[i] = V(MathF.Sin(a) * rr, y, MathF.Cos(a) * rr - 0.02f);
                }
                b.Tube(Body, pts, 0.02f, 0.026f, fold, Cloth, 0.01f);
            }
            ThreadRing(b, 0.3f, GownRadius(0.3f, 0.78f, 0.07f, 0.27f) + 0.004f, 0.04f, Thread);
            b.Ell(Body, V(0, 0.82f, 0), V(0.08f, 0.11f, 0.065f), robe, mat: Cloth);
            b.Torus(Body, V(0, 0.9f, 0), 0.06f, 0.026f, robe, mat: Cloth, blend: 0.01f);
            // The arms: the distaff in one hand, the spindle hanging from the other, the thread between them
            int armL = b.Arm(-1f, V(-0.08f, 0.86f, 0));
            FateArm(b, armL, -1f, V(-0.08f, 0.87f, 0), V(-0.18f, 0.73f, 0.05f), V(-0.26f, 0.69f, 0.12f), robe, skin, 0.045f);
            b.Limb(armL, V(-0.275f, 0.1f, 0.13f), V(-0.275f, 1.08f, 0.13f), 0.017f, 0.015f, Rgb(232, 220, 170), Shell);
            b.Ell(armL, V(-0.275f, 1.1f, 0.13f), V(0.07f, 0.1f, 0.07f), woolPink, mat: Fur, blend: 0.02f);
            FurTufts(b, armL, V(-0.275f, 1.1f, 0.13f), V(0.07f, 0.1f, 0.07f), 10, 0.05f, 0.022f, woolPink, 1.1f, -0.7f, 0.2f);
            b.Spike(armL, V(-0.275f, 1.18f, 0.13f), V(-0.275f, 1.3f, 0.13f), 0.02f, Rgb(232, 220, 170), mat: Shell);
            int armR = b.Arm(1f, V(0.08f, 0.86f, 0));
            FateArm(b, armR, 1f, V(0.08f, 0.87f, 0), V(0.18f, 0.73f, 0.05f), V(0.26f, 0.69f, 0.12f), robe, skin, 0.045f);
            b.Limb(armR, V(0.27f, 0.68f, 0.13f), V(0.27f, 0.5f, 0.13f), 0.012f, 0.01f, Rgb(200, 170, 120), Shell);
            b.Ell(armR, V(0.27f, 0.56f, 0.13f), V(0.032f, 0.05f, 0.032f), Thread, mat: Glow, blend: 0.01f);
            b.Tube(Body, Smooth(3, V(-0.24f, 1.06f, 0.15f), V(-0.12f, 0.9f, 0.16f), V(0.04f, 0.74f, 0.14f), V(0.18f, 0.64f, 0.14f), V(0.26f, 0.6f, 0.13f)),
                0.016f, 0.016f, Thread, Glow, 0.006f);
            // A lamb's long face, long drooping ears lined pink, a crest of pale curls
            int head = b.Head(V(0, 0.95f, 0.01f));
            var c = V(0, 1.03f, 0.03f);
            var r = V(0.065f, 0.075f, 0.07f);
            b.Limb(head, V(0, 0.92f, 0.0f), c, 0.035f, 0.04f, skin);
            b.Ell(head, c, r, skin);
            b.Ell(head, V(0, 0.99f, 0.085f), V(0.042f, 0.045f, 0.06f), skin, blend: 0.03f);
            b.Ell(head, V(0, 0.99f, 0.14f), V(0.012f, 0.009f, 0.007f), pink, mat: Shell, blend: 0.004f);
            PokeBuilder.Both(s =>
            {
                var at = On(c, r, 0.032f * s, 1.04f);
                b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(214, 110, 168));
                int ear = b.Ear(head, s, V(0.055f * s, 1.07f, 0.0f));
                b.Ell(ear, V(0.13f * s, 1.01f, 0.0f), V(0.1f, 0.033f, 0.05f), skin, V(0, 0, 38f * s), mat: Fur, blend: 0.012f);
                b.PaintEll(ear, V(0.135f * s, 1.0f, 0.03f), V(0.075f, 0.02f, 0.03f), pink, V(0, 0, 38f * s), 0.008f);
                b.Tube(head, Smooth(3, V(0.04f * s, 1.09f, 0.0f), V(0.08f * s, 1.15f, -0.02f), V(0.07f * s, 1.2f, 0.01f)), 0.022f, 0.012f, Rgb(244, 240, 228), Fur);
            });
            b.Ell(head, V(0, 1.085f, -0.03f), V(0.07f, 0.05f, 0.07f), Rgb(244, 240, 228), mat: Fur, blend: 0.02f);
            b.Tube(head, Smooth(3, V(0, 1.11f, 0.0f), V(0, 1.18f, -0.02f), V(0.025f, 1.21f, 0.0f)), 0.024f, 0.012f, Rgb(244, 240, 228), Fur);
            return b;
        }

        /// <summary>Ursifira: the measurer, in a green robe layered with leaves and edged in gold, a leafy shawl, a doe's calm face with antlers and leaf ears, a golden thread stretched between her hands with knots that glow.</summary>
        public static PokeBuilder Ursifira()
        {
            var b = new PokeBuilder("Ursifira", 0.9f, BodyPlan.Biped, V(0, 0.68f, 0)) { Coat = Fur };
            var robe = Rgb(78, 140, 72);
            var leaf = Rgb(110, 172, 88);
            var leafDark = Rgb(58, 112, 58);
            var gold = Rgb(226, 182, 76);
            var skin = Rgb(132, 168, 112);
            var antler = Rgb(150, 150, 104);
            Gown(b, 0.78f, 0.07f, 0.28f, robe);
            // Rows of leaves down the robe, gold trim at its hem and down its front
            for (int row = 0; row < 4; row++)
            {
                float y = 0.62f - row * 0.15f;
                float rr = GownRadius(y, 0.78f, 0.07f, 0.28f);
                for (int i = 0; i < 7; i++)
                {
                    float a = (-70f + i * 23f + (row % 2) * 11f) * Degree;
                    var root = V(MathF.Sin(a) * rr, y + 0.04f, MathF.Cos(a) * rr - 0.02f);
                    var outward = V(MathF.Sin(a), 0, MathF.Cos(a));
                    Petal(b, Body, root, root + V(0, -0.12f, 0) + outward * 0.035f, 0.05f, i % 2 == 0 ? leaf : leafDark, Leaf, 0.01f);
                }
            }
            b.PaintTorus(Body, V(0, 0.06f, -0.02f), GownRadius(0.06f, 0.78f, 0.07f, 0.28f), 0.018f, gold);
            b.PaintEll(Body, V(0, 0.4f, 0.24f), V(0.018f, 0.36f, 0.06f), gold, V(-14f, 0, 0), 0.006f);
            b.Ell(Body, V(0, 0.82f, 0), V(0.08f, 0.11f, 0.065f), robe, mat: Cloth);
            // A shawl of leaves round her shoulders
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * MathF.Tau;
                var root = V(MathF.Sin(a) * 0.08f, 0.88f, MathF.Cos(a) * 0.065f);
                Petal(b, Body, root, root + V(MathF.Sin(a) * 0.11f, -0.05f, MathF.Cos(a) * 0.09f), 0.045f, i % 2 == 0 ? leaf : leafDark, Leaf, 0.01f);
            }
            // The thread stretched between her hands, knots glowing on it, its ends hanging
            var handL = V(-0.27f, 0.72f, 0.15f);
            var handR = V(0.27f, 0.82f, 0.15f);
            int armL = b.Arm(-1f, V(-0.08f, 0.86f, 0));
            FateArm(b, armL, -1f, V(-0.08f, 0.87f, 0), V(-0.19f, 0.76f, 0.06f), handL, robe, skin, 0.06f);
            b.Tube(armL, Smooth(3, handL + V(0, -0.02f, 0.01f), handL + V(0.01f, -0.15f, 0.02f), handL + V(-0.02f, -0.3f, 0.02f)), 0.014f, 0.014f, Thread, Glow);
            int armR = b.Arm(1f, V(0.08f, 0.86f, 0));
            FateArm(b, armR, 1f, V(0.08f, 0.87f, 0), V(0.19f, 0.82f, 0.06f), handR, robe, skin, 0.06f);
            b.Tube(armR, Smooth(3, handR + V(0, -0.02f, 0.01f), handR + V(0.02f, -0.15f, 0.02f), handR + V(0.0f, -0.32f, 0.02f)), 0.014f, 0.014f, Thread, Glow);
            b.Limb(Body, handL + V(0.02f, 0, 0.01f), handR + V(-0.02f, 0, 0.01f), 0.014f, 0.014f, Thread, Glow, 0.004f);
            foreach (float t in new[] { 0.25f, 0.5f, 0.75f })
                b.Ell(Body, Vector3.Lerp(handL, handR, t) + V(0, 0, 0.01f), V(0.022f, 0.022f, 0.022f), Rgb(255, 236, 160), mat: Glow, blend: 0.008f);
            // A doe's calm face, leaf ears and branching antlers
            int head = b.Head(V(0, 0.95f, 0.01f));
            var c = V(0, 1.04f, 0.03f);
            var r = V(0.06f, 0.072f, 0.068f);
            b.Limb(head, V(0, 0.9f, 0.0f), c, 0.036f, 0.04f, skin);
            b.Ell(head, c, r, skin);
            b.Ell(head, V(0, 0.995f, 0.09f), V(0.036f, 0.04f, 0.065f), skin, blend: 0.03f);
            b.Ell(head, V(0, 0.995f, 0.15f), V(0.012f, 0.01f, 0.008f), Rgb(50, 60, 40), mat: Shell, blend: 0.004f);
            b.PaintEll(head, V(0, 1.1f, 0.06f), V(0.03f, 0.02f, 0.03f), gold, default, 0.006f);
            PokeBuilder.Both(s =>
            {
                var at = On(c, r, 0.03f * s, 1.045f);
                b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(190, 140, 50), closed: true);
                int ear = b.Ear(head, s, V(0.05f * s, 1.07f, 0.0f));
                Petal(b, ear, V(0.05f * s, 1.07f, 0.0f), V(0.2f * s, 1.01f, -0.02f), 0.045f, leaf, Leaf, 0.012f);
                b.Tube(head, Smooth(3, V(0.03f * s, 1.1f, 0.0f), V(0.06f * s, 1.2f, -0.02f), V(0.1f * s, 1.3f, -0.03f)), 0.016f, 0.012f, antler, Shell);
                b.Tube(head, Smooth(3, V(0.055f * s, 1.18f, -0.02f), V(0.11f * s, 1.22f, -0.02f), V(0.15f * s, 1.26f, -0.01f)), 0.013f, 0.01f, antler, Shell);
                b.Tube(head, Smooth(3, V(0.08f * s, 1.25f, -0.03f), V(0.06f * s, 1.3f, -0.03f), V(0.05f * s, 1.34f, -0.02f)), 0.012f, 0.01f, antler, Shell);
            });
            return b;
        }

        /// <summary>Ursimora: the cutter, hooded in deep violet over grey, silver swirls on her robe, an old gentle face with eyes half shut and long grey hair, silver shears and the thread's end, her hem fading into evening mist.</summary>
        public static PokeBuilder Ursimora()
        {
            var b = new PokeBuilder("Ursimora", 0.9f, BodyPlan.Biped, V(0, 0.68f, 0)) { Coat = Fur }.Hover();
            var robe = Rgb(108, 78, 150);
            var under = Rgb(88, 86, 100);
            var silver = Rgb(212, 212, 226);
            var skin = Rgb(176, 166, 184);
            var hair = Rgb(204, 200, 214);
            var mist = Rgb(140, 132, 186);
            Gown(b, 0.78f, 0.07f, 0.25f, robe, flat: false);
            b.PaintEll(Body, V(0, 0.42f, 0.22f), V(0.06f, 0.34f, 0.1f), under, V(-12f, 0, 0), 0.008f);
            foreach (var (x, y) in new[] { (-0.12f, 0.5f), (0.13f, 0.42f), (-0.17f, 0.3f), (0.18f, 0.24f), (-0.08f, 0.66f), (0.09f, 0.62f) })
            {
                float rr = GownRadius(y, 0.78f, 0.07f, 0.25f, 0.16f);
                var at = V(x, y, MathF.Sqrt(MathF.Max(0f, rr * rr - x * x)) - 0.02f);
                b.PaintTorus(Body, at, 0.022f, 0.004f, silver, Euler(V(x, 0, at.Z + 0.02f)));
            }
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * MathF.Tau;
                var root = V(MathF.Sin(a) * 0.15f, 0.14f, MathF.Cos(a) * 0.15f - 0.02f);
                b.Spike(Body, root, V(MathF.Sin(a + 0.25f) * 0.33f, 0.05f, MathF.Cos(a + 0.25f) * 0.33f - 0.02f), 0.055f, mist, 0.5f, Glow);
            }
            b.Ell(Body, V(0, 0.82f, 0), V(0.08f, 0.11f, 0.065f), robe, mat: Cloth);
            b.Ell(Body, V(0, 0.89f, 0.01f), V(0.075f, 0.04f, 0.07f), under, mat: Cloth, blend: 0.02f);
            // The shears in one hand, the thread's end in the other
            var handR = V(0.22f, 0.74f, 0.16f);
            var handL = V(-0.26f, 0.82f, 0.14f);
            int armR = b.Arm(1f, V(0.08f, 0.86f, 0));
            FateArm(b, armR, 1f, V(0.08f, 0.87f, 0), V(0.17f, 0.74f, 0.08f), handR, robe, skin, 0.07f);
            PokeBuilder.Both(s => Blade(b, armR, handR + V(-0.02f, 0.005f, 0.02f), handR + V(-0.1f, 0.03f + 0.02f * s, 0.05f), 0.016f, silver, V(0, 1f, 0), 0.3f, Metal));
            PokeBuilder.Both(s => b.Torus(armR, handR + V(0.015f, -0.015f + 0.02f * s, 0.01f), 0.018f, 0.006f, silver, V(90f, 0, 0), mat: Metal, blend: 0.004f));
            int armL = b.Arm(-1f, V(-0.08f, 0.86f, 0));
            FateArm(b, armL, -1f, V(-0.08f, 0.87f, 0), V(-0.18f, 0.78f, 0.08f), handL, robe, skin, 0.07f);
            b.Tube(Body, Smooth(3, handL + V(0, -0.02f, 0.01f), V(-0.14f, 0.7f, 0.2f), V(0.0f, 0.68f, 0.22f), handR + V(-0.09f, 0.02f, 0.05f)), 0.014f, 0.014f, Thread, Glow, 0.006f);
            b.Tube(armL, Smooth(3, handL + V(0, -0.03f, 0.01f), handL + V(-0.02f, -0.12f, 0.02f)), 0.012f, 0.012f, Thread, Glow);
            // An old face under a hood, long grey hair falling from it, eyes half shut
            int head = b.Head(V(0, 0.95f, 0.01f));
            b.Ell(head, V(0, 1.06f, -0.02f), V(0.1f, 0.11f, 0.1f), robe, mat: Cloth);
            b.Cut(head, V(0, 1.03f, 0.08f), V(0.068f, 0.075f, 0.06f));
            var c = V(0, 1.03f, 0.035f);
            var r = V(0.062f, 0.07f, 0.066f);
            b.Ell(head, c, r, skin);
            b.Ell(head, V(0, 0.995f, 0.085f), V(0.04f, 0.035f, 0.045f), skin, blend: 0.03f);
            b.Ell(head, V(0, 1.0f, 0.13f), V(0.013f, 0.01f, 0.008f), Rgb(80, 60, 80), mat: Shell, blend: 0.004f);
            PokeBuilder.Both(s =>
            {
                var at = On(c, r, 0.03f * s, 1.045f);
                b.Eye(head, at, Outward(c, r, at), 0.015f, Rgb(120, 100, 140), closed: true);
                b.Tube(head, Smooth(3, V(0.05f * s, 1.06f, 0.04f), V(0.075f * s, 0.98f, 0.06f), V(0.07f * s, 0.88f, 0.07f), V(0.08f * s, 0.8f, 0.06f)), 0.016f, 0.012f, hair, Fur);
                b.Tube(head, Smooth(3, V(0.08f * s, 1.12f, -0.02f), V(0.15f * s, 1.15f, -0.04f), V(0.2f * s, 1.12f, -0.06f)), 0.022f, 0.012f, hair, Fur);
            });
            b.PaintEll(head, V(0, 0.98f, 0.08f), V(0.04f, 0.03f, 0.04f), Rgb(196, 188, 202), default, 0.01f);
            return Lift(b);
        }

        // ------------------------------------------------------------------ the legendaries

        /// <summary>Solzmeu: the zmeu of the tales, a tall man-shaped dragon scaled in bronze and gold, a proud horned head in a mane of flame, a stolen sun caught behind its shoulders and wings folded like a cloak.</summary>
        public static PokeBuilder Solzmeu()
        {
            var b = new PokeBuilder("Solzmeu", 1f, BodyPlan.Biped, V(0, 0.95f, 0)) { Coat = Scales };
            var bronze = Rgb(176, 120, 56);
            var gold = Rgb(230, 186, 82);
            var belly = Rgb(216, 172, 98);
            var membrane = Rgb(146, 88, 42);
            var flame = Rgb(250, 140, 40);
            var sun = Rgb(255, 222, 112);
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.11f * s, 0.78f, 0));
                b.Limb(leg, V(0.11f * s, 0.8f, 0), V(0.14f * s, 0.48f, 0.06f), 0.085f, 0.06f, bronze);
                b.Limb(leg, V(0.14f * s, 0.48f, 0.06f), V(0.15f * s, 0.18f, -0.04f), 0.05f, 0.04f, bronze);
                b.Limb(leg, V(0.15f * s, 0.18f, -0.04f), V(0.16f * s, 0.045f, 0.06f), 0.04f, 0.038f, bronze);
                b.Ell(leg, V(0.16f * s, 0.035f, 0.09f), V(0.05f, 0.035f, 0.07f), bronze);
                foreach (float dx in new[] { -0.03f, 0f, 0.03f })
                    b.Spike(leg, V((0.16f + dx) * s, 0.025f, 0.15f), V((0.16f + dx * 1.3f) * s, 0.008f, 0.2f), 0.014f, gold, mat: Metal);
                b.Spike(leg, V(0.145f * s, 0.5f, 0.1f), V(0.15f * s, 0.6f, 0.15f), 0.03f, gold, 0.5f, Metal);
            });
            b.Ell(Body, V(0, 0.84f, 0), V(0.12f, 0.1f, 0.1f), bronze);
            b.Ell(Body, V(0, 1.0f, 0), V(0.15f, 0.18f, 0.115f), bronze);
            b.Ell(Body, V(0, 1.13f, 0.02f), V(0.2f, 0.11f, 0.13f), bronze);
            b.PaintEll(Body, V(0, 0.98f, 0.1f), V(0.07f, 0.2f, 0.05f), belly);
            foreach (float y in new[] { 0.88f, 0.94f, 1.0f, 1.06f })
                b.PaintEll(Body, V(0, y, 0.115f), V(0.08f, 0.005f, 0.04f), PixelCanvas.Shadow(belly, 0.25f), default, 0.004f);
            PokeBuilder.Both(s => b.Ell(Body, V(0.18f * s, 1.17f, 0.0f), V(0.08f, 0.06f, 0.08f), gold, mat: Metal, blend: 0.02f));
            // The stolen sun behind its shoulders: a blazing disc in a gold ring, flames round its rim
            var sc = V(0, 1.36f, -0.13f);
            b.Ell(Body, sc, V(0.3f, 0.3f, 0.035f), sun, mat: Glow, blend: 0.02f);
            b.Torus(Body, sc, 0.3f, 0.03f, gold, V(90f, 0, 0), mat: Glow, blend: 0.008f);
            for (int i = 0; i < 14; i++)
            {
                float a = i / 14f * MathF.Tau;
                var dir = V(MathF.Cos(a), MathF.Sin(a), 0);
                Blade(b, Body, sc + dir * 0.29f, sc + dir * 0.4f, 0.05f, flame, V(0, 0, 1f), 0.3f, Glow);
            }
            // Arms, one hand on its hip
            PokeBuilder.Both(s =>
            {
                int arm = b.Arm(s, V(0.2f * s, 1.17f, 0));
                var elbow = s > 0 ? V(0.3f, 1.0f, -0.04f) : V(-0.28f, 0.98f, 0.02f);
                var hand = s > 0 ? V(0.16f, 0.88f, 0.06f) : V(-0.3f, 0.78f, 0.08f);
                b.Limb(arm, V(0.2f * s, 1.18f, 0), elbow, 0.055f, 0.045f, bronze);
                b.Limb(arm, elbow, hand, 0.045f, 0.036f, bronze);
                b.Ell(arm, hand, V(0.04f, 0.045f, 0.035f), bronze);
                b.Spike(arm, elbow + V(0.02f * s, 0, -0.02f), elbow + V(0.05f * s, 0.02f, -0.1f), 0.022f, gold, 0.5f, Metal);
                foreach (float dz in new[] { -0.02f, 0.01f, 0.04f })
                    b.Spike(arm, hand + V(0, -0.03f, dz), hand + V(0.005f * s, -0.08f, dz + 0.02f), 0.012f, gold, mat: Metal);
            });
            // Wings folded down its sides like a cloak
            PokeBuilder.Both(s =>
            {
                int wing = b.Wing(s, V(0.14f * s, 1.17f, -0.1f));
                var wrist = V(0.4f * s, 1.42f, -0.12f);
                b.Tube(wing, Smooth(3, V(0.14f * s, 1.17f, -0.1f), V(0.3f * s, 1.3f, -0.14f), wrist), 0.036f, 0.026f, bronze);
                b.Spike(wing, wrist, wrist + V(0.02f * s, 0.12f, -0.02f), 0.026f, gold, mat: Metal);
                foreach (var tip in new[] { V(0.44f * s, 0.45f, -0.16f), V(0.38f * s, 0.36f, -0.05f), V(0.42f * s, 0.58f, -0.26f) })
                    Blade(b, wing, wrist, tip, 0.11f, membrane, V(s, 0, 0.15f), 0.2f);
                Blade(b, wing, V(0.18f * s, 1.12f, -0.12f), V(0.34f * s, 0.52f, -0.12f), 0.1f, membrane, V(s, 0, 0.1f), 0.2f);
                b.PaintEll(wing, V(0.41f * s, 0.5f, -0.14f), V(0.08f, 0.14f, 0.16f), flame, default, 0.03f);
            });
            // The tail sweeping round on the ground, flame along its ridge
            int tail = b.Tail(V(0, 0.82f, -0.1f));
            var tp = Smooth(3, V(0, 0.82f, -0.1f), V(0, 0.6f, -0.28f), V(0.05f, 0.3f, -0.42f), V(0.2f, 0.1f, -0.45f), V(0.36f, 0.06f, -0.3f));
            b.Tube(tail, tp, 0.07f, 0.03f, bronze, blend: 0.01f);
            for (int i = 3; i < tp.Length - 1; i += 3)
                b.Spike(tail, tp[i] + V(0, 0.03f, 0), tp[i] + V(0, 0.1f, -0.02f), 0.025f, flame, 0.5f, Glow);
            // A proud horned head in a mane of flame
            int head = b.Head(V(0, 1.27f, 0.04f));
            var c = V(0, 1.42f, 0.08f);
            var r = V(0.075f, 0.07f, 0.085f);
            b.Limb(head, V(0, 1.2f, 0.0f), V(0, 1.36f, 0.06f), 0.06f, 0.055f, bronze);
            b.Ell(head, c, r, bronze);
            b.Ell(head, V(0, 1.39f, 0.18f), V(0.045f, 0.04f, 0.08f), bronze, blend: 0.03f);
            b.Ell(head, V(0, 1.37f, 0.17f), V(0.042f, 0.022f, 0.07f), belly, blend: 0.02f);
            b.Ell(head, V(0, 1.47f, 0.13f), V(0.06f, 0.016f, 0.05f), gold, mat: Metal, blend: 0.015f);
            PokeBuilder.Both(s =>
            {
                b.Spike(head, V(0.05f * s, 1.47f, 0.05f), V(0.12f * s, 1.62f, -0.08f), 0.026f, gold, mat: Metal);
                var at = On(c, r, 0.045f * s, 1.43f);
                b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(255, 196, 40), glare: true);
            });
            foreach (var (from, to) in new[] { (V(0, 1.48f, 0.04f), V(0.02f, 1.64f, -0.06f)), (V(0.05f, 1.45f, 0.0f), V(0.17f, 1.56f, -0.1f)),
                (V(-0.05f, 1.45f, 0.0f), V(-0.17f, 1.56f, -0.1f)), (V(0.06f, 1.38f, -0.02f), V(0.2f, 1.4f, -0.12f)), (V(-0.06f, 1.38f, -0.02f), V(-0.2f, 1.4f, -0.12f)),
                (V(0.05f, 1.3f, -0.03f), V(0.16f, 1.22f, -0.12f)), (V(-0.05f, 1.3f, -0.03f), V(-0.16f, 1.22f, -0.12f)), (V(0, 1.42f, -0.02f), V(0, 1.5f, -0.16f)) })
                b.Spike(head, from, to, 0.045f, flame, 0.55f, Glow, 0.02f);
            return b;
        }

        /// <summary>Lunacolac: the vârcolac that swallows the moon, a vast wolf of night-blue fur flecked with stars, a mane like the night sky, its head thrown back howling round a silver crescent in its jaws, its eyes two small full moons.</summary>
        public static PokeBuilder Lunacolac()
        {
            var b = new PokeBuilder("Lunacolac", 1f, BodyPlan.Quadruped, V(0, 0.66f, -0.04f)) { Coat = Fur };
            var night = Rgb(48, 56, 100);
            var mane = Rgb(30, 34, 70);
            var star = Rgb(232, 238, 255);
            var silver = Rgb(226, 232, 248);
            var claw = Rgb(170, 176, 200);
            foreach (var (z, front) in new[] { (0.3f, true), (-0.38f, false) })
                PokeBuilder.Both(s =>
                {
                    int leg = b.Leg(s, V(0.16f * s, 0.55f, z), front);
                    var knee = V(0.17f * s, 0.28f, z + (front ? 0.02f : -0.06f));
                    b.Limb(leg, V(0.16f * s, 0.58f, z), knee, 0.11f, 0.07f, night);
                    b.Limb(leg, knee, V(0.175f * s, 0.07f, z + 0.02f), 0.065f, 0.055f, night);
                    b.Ell(leg, V(0.18f * s, 0.045f, z + 0.05f), V(0.075f, 0.045f, 0.1f), night);
                    foreach (float dx in new[] { -0.04f, -0.013f, 0.013f, 0.04f })
                        b.Spike(leg, V((0.18f + dx) * s, 0.03f, z + 0.13f), V((0.18f + dx * 1.2f) * s, 0.012f, z + 0.18f), 0.016f, claw, mat: Shell);
                });
            b.Ell(Body, V(0, 0.68f, -0.06f), V(0.24f, 0.22f, 0.42f), night);
            b.Ell(Body, V(0, 0.74f, 0.24f), V(0.27f, 0.27f, 0.23f), mane);
            FurTufts(b, Body, V(0, 0.8f, 0.2f), V(0.27f, 0.26f, 0.25f), 34, 0.18f, 0.06f, mane, 0.35f, -0.6f, 0.4f);
            FurTufts(b, Body, V(0, 0.84f, -0.06f), V(0.18f, 0.1f, 0.3f), 14, 0.1f, 0.04f, night, -0.3f, 0.3f, 0.3f);
            // Stars flecked through its coat and mane
            var flecks = new[] { V(0.2f, 0.82f, 0.3f), V(-0.18f, 0.9f, 0.22f), V(0.1f, 1.0f, 0.18f), V(-0.24f, 0.7f, 0.3f), V(0.23f, 0.62f, 0.22f),
                V(-0.08f, 0.98f, 0.3f), V(0.15f, 0.78f, -0.2f), V(-0.2f, 0.74f, -0.3f), V(0.05f, 0.88f, -0.32f), V(-0.12f, 0.84f, 0.02f), V(0.22f, 0.7f, 0.0f) };
            foreach (var f in flecks) b.PaintEll(Body, f, V(0.018f, 0.018f, 0.018f), star, default, 0.008f);
            // The tail, high and bushy, starred like the mane
            int tail = b.Tail(V(0, 0.74f, -0.44f));
            var tp = Smooth(3, V(0, 0.74f, -0.44f), V(0, 0.82f, -0.62f), V(0, 0.95f, -0.72f), V(0, 1.06f, -0.7f));
            b.Tube(tail, tp, 0.08f, 0.12f, mane, blend: 0.015f);
            FurTufts(b, tail, tp[^1], V(0.1f, 0.1f, 0.1f), 12, 0.12f, 0.04f, mane, -0.8f, -0.9f, 0.2f);
            b.PaintEll(tail, tp[^2], V(0.02f, 0.02f, 0.02f), star, default, 0.008f);
            // The head thrown back to howl, the jaw open round a bite of the moon
            int head = b.Head(V(0, 0.95f, 0.4f));
            var c = V(0, 1.1f, 0.5f);
            var r = V(0.13f, 0.12f, 0.13f);
            b.Limb(head, V(0, 0.84f, 0.34f), V(0, 1.02f, 0.45f), 0.14f, 0.12f, mane);
            b.Ell(head, c, r, night);
            b.Ell(head, V(0, 1.19f, 0.63f), V(0.065f, 0.05f, 0.12f), night, V(-35f, 0, 0), blend: 0.03f);
            b.Ell(head, V(0, 1.245f, 0.72f), V(0.024f, 0.018f, 0.018f), Black, mat: Shell, blend: 0.006f);
            PokeBuilder.Both(s =>
            {
                int ear = b.Ear(head, s, V(0.08f * s, 1.2f, 0.44f));
                CatEar(b, ear, V(0.08f * s, 1.2f, 0.44f), V(0.13f * s, 1.34f, 0.38f), 0.048f, Rgb(70, 76, 120), night);
                var at = On(c, r, 0.065f * s, 1.15f);
                b.Eye(head, at, Outward(c, r, at), 0.022f, sclera: true, white: Rgb(238, 242, 255), pupil: Rgb(176, 188, 222));
                b.Spike(head, V(0.035f * s, 1.15f, 0.66f), V(0.035f * s, 1.11f, 0.67f), 0.012f, Claw, mat: Shell);
            });
            FurTufts(b, head, V(0, 1.12f, 0.42f), V(0.13f, 0.1f, 0.1f), 12, 0.09f, 0.035f, mane, 0.3f, -0.5f, 0.3f);
            int jaw = b.Jaw(head, V(0, 1.06f, 0.56f));
            b.Ell(jaw, V(0, 1.04f, 0.66f), V(0.058f, 0.032f, 0.1f), night, V(-12f, 0, 0), blend: 0.02f);
            PokeBuilder.Both(s => b.Spike(jaw, V(0.03f * s, 1.06f, 0.71f), V(0.03f * s, 1.1f, 0.72f), 0.011f, Claw, mat: Shell));
            // The crescent: a silver arc thick in its middle and fine at its horns
            var mc = V(0, 1.105f, 0.69f);
            var arcA = new Vector3[8];
            var arcB = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                float t = i / 7f;
                float a1 = MathF.PI - t * 1.95f, a2 = MathF.PI + t * 1.95f;
                arcA[i] = mc + V(MathF.Cos(a1) * 0.048f, MathF.Sin(a1) * 0.048f, 0.01f);
                arcB[i] = mc + V(MathF.Cos(a2) * 0.048f, MathF.Sin(a2) * 0.048f, 0.01f);
            }
            b.Tube(head, arcA, 0.024f, 0.009f, silver, Glow, 0.006f);
            b.Tube(head, arcB, 0.024f, 0.009f, silver, Glow, 0.006f);
            return b;
        }
    }
}
