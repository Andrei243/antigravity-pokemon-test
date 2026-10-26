using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// The species of our own that evolve past level 100 (plan 03 Ãƒâ€šÃ‚Â· D14), batch 8: Galar's and Hisui's, in our numbers'
// order (2000 + the forebear's National number), then the supplement's designs grown from regional forms (5000 + a
// Galarian form's National number, 6000 + a Hisuian one's), each built on that form's own look. Each is sculpted from
// its forebear's own sculpt as the look of docs/plans/D14-proposals-galar.md and -galar-supplement.md and the user's
// reference pictures (docs/art/reference/gemini/galar) say, with the tools of batch 1 (PokemonModels.Ours.Kanto.cs)
// and batch 5 (PokemonModels.Ours.Unova.cs). The forebears' own sculpts are never touched.
internal static partial class PokemonModels
{
    // ------------------------------------------------------------------ tools of this batch

    /// <summary>A small ball of a berry or a rivet, sitting on whatever it is put against.</summary>
    private static void Bead(PokeBuilder b, int bone, Vector3 at, float r, Color color, SurfaceMaterial? mat = null, float blend = 0.003f) =>
        b.Ell(bone, at, V(r, r, r), color, mat: mat ?? Shell, blend: blend);

    /// <summary>A ring of current or a band round a limb from <paramref name="a"/> to <paramref name="b2"/> at <paramref name="t"/> along it.</summary>
    private static void Band(PokeBuilder b, int bone, Vector3 a, Vector3 b2, float t, float ring, float tube, Color color, SurfaceMaterial mat) =>
        b.Torus(bone, Vector3.Lerp(a, b2, t), ring, tube, color, Euler(b2 - a), mat: mat, blend: 0.004f);

    // ------------------------------------------------------------------ 2820 Larderous, from Greedent

    /// <summary>
    /// Larderous: Greedent rounder and broader and an earthier brown, its buck teeth longer, a burrower's dark claws on
    /// its forepaws and feet and soil smudged over its cream belly, and behind it its tail doubled: a second, fuller
    /// tail curled round into a basket heaped with red berries.
    /// </summary>
    private static PokeBuilder Larderous()
    {
        var b = Become(Greedent(), "Larderous", 0.9f);
        var brown = Rgb(150, 112, 80);
        var dark = Rgb(118, 86, 62);
        var cream = Rgb(240, 222, 186);
        var soil = Rgb(112, 92, 70);
        var berry = Rgb(176, 40, 52);
        Recolour(b, (Rgb(178, 112, 92), brown), (Rgb(138, 82, 70), dark), (Rgb(240, 214, 182), cream));
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        // Longer teeth
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == head && s.Shape == SdfShape.RoundBox && Is(s.Color, White)))
        {
            s.Size = new Vector3(s.Size.X * 1.15f, s.Size.Y * 1.5f, s.Size.Z);
            s.A += V(0, -0.012f, 0);
            s.B += V(0, -0.012f, 0);
        }
        b.Sdf.Changed();
        // The first tail goes back and to the side, a second beside it, and between them and the back a basket
        Move(b, i => i == tail, V(-0.04f, 0.02f, -0.12f));
        var tc = V(0.14f, 0.52f, -0.34f);
        b.Ell(tail, tc, V(0.13f, 0.22f, 0.09f), dark, V(-10f, 0, -12f));
        b.Ell(tail, tc + V(0.02f, 0.2f, 0.06f), V(0.1f, 0.07f, 0.08f), dark, blend: 0.04f);
        b.PaintEll(tail, tc + V(0.05f, -0.05f, 0.06f), V(0.06f, 0.16f, 0.05f), Rgb(196, 170, 140));
        var bk = V(0.1f, 0.42f, -0.2f);
        var br = V(0.17f, 0.1f, 0.12f);
        b.Ell(tail, bk, br, dark, blend: 0.03f);
        b.PaintEll(tail, bk + V(0, -0.02f, -0.06f), V(0.15f, 0.06f, 0.08f), Rgb(196, 170, 140));
        b.Cut(tail, bk + V(0, 0.08f, 0), V(0.13f, 0.07f, 0.09f));
        for (int i = 0; i < 18; i++)
        {
            float a = i * 2.4f, rad = 0.03f + 0.08f * MathF.Sqrt((i + 0.5f) / 18f);
            var at = bk + V(MathF.Cos(a) * rad * 1.3f, 0.02f + 0.015f * (1f - rad / 0.11f), MathF.Sin(a) * rad * 0.85f);
            Bead(b, tail, at, 0.024f, i % 4 == 0 ? Rgb(150, 30, 44) : berry, blend: 0.004f);
        }
        // Soil smudged over the belly
        foreach (var (x, y, k) in new[] { (-0.06f, 0.14f, 1f), (0.05f, 0.12f, 0.8f), (0.0f, 0.1f, 1.1f), (0.08f, 0.18f, 0.6f), (-0.09f, 0.2f, 0.5f) })
            b.PaintEll(Body, V(x, y, 0.15f), V(0.035f, 0.025f, 0.05f) * k, PixelCanvas.Mix(soil, Rgb(240, 222, 186), 0.35f), soft: 0.02f);
        // A burrower's claws on its paws and feet
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            Claws(b, arm, V(0.055f * s, 0.28f, 0.2f), V(0.013f, 0, 0), V(0, -0.8f, 0.6f), 0.035f, 0.007f);
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            Claws(b, leg, V(0.11f * s, 0.02f, 0.145f), V(0.018f, 0, 0), V(0, -0.2f, 1f), 0.03f, 0.008f);
        });
        Stretch(b, V(1.14f, 1.02f, 1.1f));
        return b;
    }

    // ------------------------------------------------------------------ 2828 Reynardine, from Thievul

    /// <summary>
    /// Reynardine: Thievul taller in the leg and longer, its black mask turned into a glimmering mask of pale violet,
    /// a curled moustache under its nose, a ruff of black and lilac at its chest, and its tail raised and split into
    /// three plumes, each tipped lilac and black like a fox of the old stories.
    /// </summary>
    private static PokeBuilder Reynardine()
    {
        var b = Become(Thievul(), "Reynardine", 0.86f);
        var red = Rgb(188, 80, 48);
        var black = Rgb(44, 40, 46);
        var white = Rgb(242, 238, 232);
        var lilac = Rgb(214, 196, 236);
        var violet = Rgb(186, 150, 236);
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        // The mask glimmers violet, the white of muzzle and ruff turns lilac
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == head && s.Op == SdfOp.Paint && Is(s.Color, black)))
        {
            s.Color = violet;
            s.Material = Glow;
        }
        Recolour(b, (white, lilac));
        b.Sdf.Changed();
        var bc = V(0, 0.28f, -0.01f);
        FurTufts(b, Body, bc + V(0, 0.05f, 0.12f), V(0.06f, 0.05f, 0.05f), 9, 0.04f, 0.015f, black, 1.8f, -0.5f, 0.6f);
        var c = bc + V(0, 0.16f, 0.16f) + V(0, 0.03f, 0.03f);
        // The moustache, curled at both ends
        PokeBuilder.Both(s =>
        {
            var root = c + V(0.004f * s, -0.035f, 0.1f);
            var curl = Spiral(root + V(0.045f * s, -0.004f, -0.004f), V(-s, 0, 0), V(0, 1f, 0), 0.012f, 0.004f, 0f, MathF.PI * 1.6f, 10);
            b.Tube(head, new[] { root, root + V(0.025f * s, -0.008f, -0.002f) }.Concat(curl).ToArray(), 0.004f, 0.003f, black, Shell, 0.002f);
        });
        // The tail rises and splits into three plumes
        b.Sdf.RemoveAll(s => s.Bone == tail);
        var root0 = bc + V(0, 0.02f, -0.14f);
        var tp = Smooth(3, root0, bc + V(0, -0.03f, -0.22f), bc + V(0, 0.0f, -0.3f), bc + V(0, 0.08f, -0.34f));
        b.Tube(tail, tp, 0.032f, 0.045f, red, blend: 0f);
        var fork = tp[^1];
        foreach (var d in new[] { V(-0.06f, 0.15f, 0.02f), V(0.0f, 0.18f, -0.05f), V(0.07f, 0.14f, 0.0f) })
        {
            var mid = fork + d * 0.55f;
            var end = fork + d;
            b.Limb(tail, fork, mid, 0.04f, 0.035f, red, blend: 0.015f);
            b.Limb(tail, mid, end, 0.035f, 0.03f, lilac, blend: 0.01f);
            b.Spike(tail, end - Vector3.Normalize(d) * 0.01f, end + Vector3.Normalize(d) * 0.07f, 0.03f, black, 0.7f, blend: 0.01f);
        }
        Stretch(b, V(0.95f, 1.16f, 1.1f));
        return b;
    }

    // ------------------------------------------------------------------ 2830 Thistlewisp, from Eldegoss

    /// <summary>
    /// Thistlewisp: Eldegoss lifted off the ground, its cotton grown into a crown of thistledown with long feathery
    /// spokes each tipped with a brown seed, two wings of long leaves spread from its little green body and a tangle
    /// of roots trailing below it.
    /// </summary>
    private static PokeBuilder Thistlewisp()
    {
        var b = Become(Eldegoss(), "Thistlewisp", 0.92f);
        var cotton = Rgb(246, 242, 228);
        var seed = Rgb(120, 92, 64);
        var green = Rgb(134, 166, 72);
        var dark = Rgb(98, 130, 56);
        // The skirt of leaves goes; roots hang in its place
        b.Sdf.RemoveAll(s => s.Bone == Body && s.A.Y < 0.12f && (Is(s.Color, green) || Is(s.Color, dark)) && s.A.Z > 0.08f && s.B.Y < s.A.Y);
        var cc = V(0, 0.36f, -0.07f);
        Lumps(b, Body, cc, V(0.24f, 0.24f, 0.23f), 46, 0.06f, cotton, Rgb(226, 222, 206), -0.2f);
        float golden = MathF.PI * (3f - MathF.Sqrt(5f));
        for (int i = 0; i < 18; i++)
        {
            float y = 1f - 2f * (i + 0.5f) / 18f, ring = MathF.Sqrt(1f - y * y), a = i * golden;
            var dir = V(MathF.Sin(a) * ring, y * 0.9f + 0.1f, MathF.Cos(a) * ring * 0.7f - 0.2f);
            if (dir.Y < -0.35f) continue;
            dir = Vector3.Normalize(dir);
            var from = cc + dir * 0.2f;
            var tip = cc + dir * 0.4f;
            b.Limb(Body, from, tip, 0.008f, 0.004f, cotton, blend: 0.004f);
            FurTufts(b, Body, Vector3.Lerp(from, tip, 0.55f), V(0.012f, 0.012f, 0.012f), 4, 0.03f, 0.005f, cotton, -1f, -1f, 0f);
            b.Ell(Body, tip + dir * 0.01f, V(0.01f, 0.01f, 0.01f) + Vector3.Abs(dir) * 0.008f, seed, mat: Shell, blend: 0.003f);
        }
        // Wings of long leaves
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.06f * s, 0.15f, 0.12f));
            for (int i = 0; i < 6; i++)
            {
                float t = i / 5f;
                var root = V(0.06f * s, 0.16f - 0.01f * i, 0.12f - 0.008f * i);
                var tip = root + V((0.3f - 0.05f * t) * s, 0.04f - 0.14f * t, -0.03f - 0.02f * t);
                Frond(b, w, root, tip, 0.04f - 0.004f * i, i % 2 == 0 ? green : dark, V(0, 1f, 0.3f), 0.2f, Leaf, 0.006f);
            }
        });
        // Roots trailing under it
        foreach (var (x, z, len) in new[] { (-0.02f, 0.12f, 0.16f), (0.02f, 0.14f, 0.2f), (0.0f, 0.11f, 0.13f), (0.035f, 0.12f, 0.12f), (-0.035f, 0.135f, 0.15f) })
        {
            var top = V(x, 0.08f, z);
            var path = Smooth(3, top, top + V(x * 0.6f, -len * 0.5f, 0.01f), top + V(-x * 0.3f, -len, -0.01f));
            b.Tube(Body, path, 0.009f, 0.004f, dark, Leaf, 0.004f);
            Frond(b, Body, path[^4], path[^4] + V(x > 0 ? 0.025f : -0.025f, -0.01f, 0), 0.008f, green, V(0, 0, 1f), 0.3f, Leaf, 0.003f);
        }
        b.Hover();
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2832 Ramsworth, from Dubwool

    /// <summary>
    /// Ramsworth: Dubwool heavier in the shoulder, its fleece all white and hanging in thick ropes, its face, legs and
    /// hooves black, and its horns grown into two full curls of grey-gold that meet over its brow as a helmet.
    /// </summary>
    private static PokeBuilder Ramsworth()
    {
        var b = Become(Dubwool(), "Ramsworth", 0.92f);
        var wool = Rgb(244, 242, 236);
        var shade = Rgb(212, 210, 206);
        var horn = Rgb(150, 142, 104);
        var dark = Rgb(56, 52, 54);
        int head = BoneOf(b, "head");
        // The patches go and the collar is white; the old horns come off
        b.Sdf.RemoveAll(s => Is(s.Color, Rgb(92, 66, 58)) || (s.Bone == head && Is(s.Color, Rgb(132, 82, 62))));
        Recolour(b, (Rgb(150, 148, 152), wool), (Rgb(176, 174, 178), shade), (Rgb(62, 54, 58), dark));
        var c = V(0, 0.42f, 0.25f);
        // The helmet of horn over its brow, and a full curl of horn each side
        b.Ell(head, c + V(0, 0.05f, -0.005f), V(0.06f, 0.03f, 0.05f), horn, V(20f, 0, 0), Shell, 0.01f);
        b.Spike(head, c + V(0, 0.05f, 0.02f), c + V(0, 0.02f, 0.07f), 0.03f, horn, 0.6f, Shell);
        PokeBuilder.Both(s => RamHorn(b, head, c + V(0.05f * s, 0.06f, -0.02f), s, 0.11f, 0.04f, horn));
        // The fleece in thick ropes hanging all round
        var bc = V(0, 0.34f, -0.03f);
        var br = V(0.17f, 0.14f, 0.2f);
        for (int ring = 0; ring < 5; ring++)
            for (int i = 0; i < 9; i++)
            {
                float a = (i + (ring % 2) * 0.5f) / 9f * MathF.PI;
                float z = 0.16f - ring * 0.08f;
                var dir = V(MathF.Cos(a), MathF.Sin(a) * 0.6f + 0.15f, z / br.Z);
                var top = Out(bc, br, default, dir) - Vector3.Normalize(dir) * 0.02f;
                var bottom = top + V(MathF.Sign(top.X) * 0.025f, -0.12f - 0.05f * MathF.Abs(MathF.Cos(a)), 0.005f);
                b.Limb(Body, top, bottom, 0.022f, 0.016f, (i + ring) % 3 == 0 ? shade : wool, blend: 0.012f);
            }
        Stretch(b, V(1.1f, 1.08f, 1.05f));
        return b;
    }

    // ------------------------------------------------------------------ 2834 Juggernaw, from Drednaw

    /// <summary>
    /// Juggernaw: Drednaw larger and dark green, its shell an ironclad's armour banded in brown iron studded with
    /// rivets, its spikes of grey stone, its helm and horn pale stone with the horn grown into a ram's prow, and its
    /// jaw heavier.
    /// </summary>
    private static PokeBuilder Juggernaw()
    {
        var b = Become(Drednaw(), "Juggernaw", 0.97f);
        var skin = Rgb(76, 120, 74);
        var shell = Rgb(54, 92, 58);
        var stone = Rgb(150, 142, 126);
        var iron = Rgb(96, 70, 56);
        var pale = Rgb(214, 208, 190);
        Recolour(b, (Rgb(70, 168, 170), skin), (Rgb(150, 98, 66), shell), (Rgb(222, 162, 62), stone), (Rgb(238, 228, 202), Rgb(200, 196, 170)));
        foreach (var s in b.Sdf.Shapes.Where(s => s.Op == SdfOp.Paint && Is(s.Color, PixelCanvas.Mix(Rgb(150, 98, 66), Black, 0.18f))))
            s.Color = PixelCanvas.Mix(shell, Black, 0.15f);
        b.Sdf.Changed();
        int head = BoneOf(b, "head");
        var c = V(0, 0.34f, 0.38f);
        Move(b, i => i == head, V(0, 0.01f, 0.04f));
        c += V(0, 0.01f, 0.04f);
        b.Limb(head, V(0, 0.3f, 0.24f), c - V(0, 0.02f, 0.06f), 0.08f, 0.075f, skin, blend: 0.02f);
        // The prow of a horn, pale stone, and a heavier jaw
        b.Spike(head, c + V(0, 0.06f, 0.06f), c + V(0, 0.16f, 0.22f), 0.045f, pale, 0.85f, Shell);
        b.Ell(head, c + V(0, 0.06f, 0.02f), V(0.09f, 0.04f, 0.1f), pale, mat: Shell, blend: 0.012f);
        b.Ell(head, c + V(0, -0.06f, 0.03f), V(0.095f, 0.04f, 0.1f), skin, blend: 0.015f);
        // The ironclad's bands and rivets over the shell
        var shc = V(0, 0.32f, -0.04f);
        var shr = V(0.22f, 0.13f, 0.28f);
        b.PaintTorus(Body, shc + V(0, -0.06f, 0), 0.21f, 0.025f, iron, sz: 1.3f);
        foreach (float x in new[] { -0.6f, 0f, 0.6f })
            b.PaintEll(Body, Out(shc, shr, default, V(x, 1f, 0)), V(0.018f, 0.06f, 0.3f), iron, V(0, 0, -x * 50f), 0.006f);
        foreach (float z in new[] { -0.55f, 0.1f, 0.6f })
            b.PaintEll(Body, Out(shc, shr, default, V(0, 1f, z)), V(0.24f, 0.08f, 0.016f), iron, V(z * 40f, 0, 0), 0.006f);
        for (int i = 0; i < 9; i++)
        {
            float z = -0.8f + i * 0.2f;
            Bead(b, Body, Out(shc, shr, default, V(0, 1f, z)), 0.009f, Rgb(120, 110, 104), Metal, 0.002f);
            foreach (float x in new[] { -0.6f, 0.6f })
                Bead(b, Body, Out(shc, shr, default, V(x, 1f, z)), 0.008f, Rgb(120, 110, 104), Metal, 0.002f);
        }
        Stretch(b, V(1.1f, 1.08f, 1.08f));
        return b;
    }

    // ------------------------------------------------------------------ 2836 Voltcourser, from Boltund

    /// <summary>
    /// Voltcourser: Boltund longer and leaner, a greyhound's deep chest, all dark grey with lightning streaked yellow
    /// down its flanks, its ears swept long behind it lined in yellow, glowing eyes, three rings of current round each
    /// leg and its tail a lightning bolt in three steps with a glowing point.
    /// </summary>
    private static PokeBuilder Voltcourser()
    {
        var b = Become(Boltund(), "Voltcourser", 0.9f);
        var dark = Rgb(66, 68, 72);
        var deep = Rgb(48, 50, 54);
        var yellow = Rgb(246, 220, 70);
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        // Every cuff goes; the rest of its yellow and white turns dark
        b.Sdf.RemoveAll(s => s.Shape == SdfShape.Torus && Is(s.Color, Rgb(246, 212, 64)));
        b.Sdf.RemoveAll(s => s.Bone == tail);
        Recolour(b, (Rgb(246, 212, 64), dark), (Rgb(246, 246, 240), deep), (Rgb(86, 98, 76), dark));
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
            d.Iris = yellow;
        var bc = V(0, 0.3f, -0.01f);
        var c = bc + V(0, 0.13f, 0.2f) + V(0, 0.025f, 0.035f);
        // A deeper chest
        b.Ell(Body, bc + V(0, -0.01f, 0.09f), V(0.07f, 0.095f, 0.08f), dark, blend: 0.03f);
        // Yellow lightning down its flanks and neck, and lining its ears
        PokeBuilder.Both(s =>
        {
            foreach (var (y, z, roll) in new[] { (0.31f, 0.1f, 30f), (0.3f, 0.0f, -35f), (0.29f, -0.1f, 30f), (0.33f, -0.06f, 60f) })
                PaintAs(b, Body, V(0.075f * s, y, z), V(0.012f, 0.05f, 0.012f), yellow, Glow, V(0, 0, roll * s));
            PaintAs(b, Body, V(0.035f * s, 0.38f, 0.17f), V(0.01f, 0.04f, 0.01f), yellow, Glow, V(-30f, 0, 20f * s));
            // Long ears swept back, lined in yellow
            Blade(b, head, c + V(0.022f * s, 0.03f, -0.04f), c + V(0.06f * s, 0.07f, -0.22f), 0.03f, deep, V(0, 1f, 0.2f));
            Blade(b, head, c + V(0.028f * s, 0.038f, -0.06f), c + V(0.058f * s, 0.072f, -0.2f), 0.012f, yellow, V(0, 1f, 0.2f), 0.3f, Glow);
            PaintAs(b, head, c + V(0.03f * s, 0.012f, 0.025f), V(0.014f, 0.005f, 0.02f), yellow, Glow, V(0, 0, -15f * s));
            // Three rings of current round each leg
            foreach (var (z, front) in new[] { (0.12f, true), (-0.14f, false) })
            {
                int leg = BoneOf(b, (front ? "leg" : "hind") + (s < 0 ? "L" : "R"));
                var knee = V(0.0525f * s, 0.13f, z + (front ? 0.015f : -0.035f));
                var ankle = V(0.0525f * s, 0.06f, z + 0.01f);
                foreach (float t in new[] { 0.1f, 0.5f, 0.9f })
                    Band(b, leg, knee, ankle, t, 0.026f, 0.005f, yellow, Glow);
            }
        });
        // The tail, a bolt in three steps with a glowing point
        var tr = bc + V(0, 0.03f, -0.16f);
        var bolt = new[] { tr, tr + V(0, 0.06f, -0.1f), tr + V(0, 0.03f, -0.13f), tr + V(0, 0.12f, -0.2f), tr + V(0, 0.09f, -0.23f), tr + V(0, 0.2f, -0.3f) };
        b.Tube(tail, bolt, 0.02f, 0.014f, deep, blend: 0f);
        Blade(b, tail, bolt[^1], bolt[^1] + V(0, 0.08f, -0.08f), 0.03f, yellow, V(1f, 0, 0), 0.3f, Glow);
        Stretch(b, V(0.94f, 1.12f, 1.16f));
        return b;
    }

    // ------------------------------------------------------------------ 2841 Pippinwyrm, from Flapple

    /// <summary>
    /// Pippinwyrm: Flapple's little dragon grown into a green wyrm, cream-bellied, its body wound twice round a great
    /// red apple of its own that it carries like a shield and its tail looped beneath, wings of red apple skin cut into
    /// feathers and lined cream, and a stalk with one leaf on its head. Built anew, its body's plan changed, from
    /// Flapple's colours and parts.
    /// </summary>
    private static PokeBuilder Pippinwyrm()
    {
        var green = Rgb(112, 172, 82);
        var red = Rgb(198, 52, 62);
        var cream = Rgb(234, 208, 152);
        var belly = Rgb(222, 226, 160);
        var stalk = Rgb(120, 82, 52);
        var ac = V(0.02f, 0.26f, 0.08f);
        // The tail loops under and behind the apple, then winds round it twice and rises behind it to the neck
        var loop = new[] { V(0.02f, 0.06f, -0.16f), V(0.1f, 0.04f, -0.12f), V(0.14f, 0.07f, -0.04f), V(0.12f, 0.12f, 0.0f) };
        var wind = Enumerable.Range(0, 17).Select(i =>
        {
            float t = i / 16f, a = 1.3f + t * MathF.Tau * 1.6f;
            return ac + V(MathF.Cos(a) * 0.145f, -0.09f + 0.15f * t, MathF.Sin(a) * 0.13f);
        });
        var neck = new[] { ac + V(-0.06f, 0.11f, -0.14f), V(-0.02f, 0.46f, -0.08f), V(0, 0.54f, -0.02f) };
        var points = Smooth(2, loop.Concat(wind).Concat(neck).ToArray());
        var b = new PokeBuilder("Pippinwyrm", 0.85f, BodyPlan.Serpent, points[0]) { Coat = Scales };
        int last = Coils(b, points, t => 0.012f + 0.024f * MathF.Min(1f, t * 3f), green, 4);
        // The apple, its stalk and a shine
        b.Ell(Body, ac, V(0.13f, 0.12f, 0.12f), red, mat: Shell);
        b.Cut(Body, ac + V(0, 0.13f, 0), V(0.035f, 0.025f, 0.035f));
        b.Limb(Body, ac + V(0, 0.09f, 0), ac + V(-0.015f, 0.17f, 0.01f), 0.01f, 0.008f, stalk, Shell, 0.004f);
        b.PaintEll(Body, ac + V(-0.06f, 0.06f, 0.09f), V(0.025f, 0.018f, 0.02f), Rgb(232, 120, 120));
        b.PaintEll(Body, ac + V(0, -0.11f, 0), V(0.04f, 0.02f, 0.04f), PixelCanvas.Mix(red, Black, 0.3f));
        // The pale belly along the neck
        b.PaintEll(last, V(0, 0.44f, -0.06f), V(0.022f, 0.08f, 0.02f), belly);
        int head = b.Head(V(0, 0.55f, -0.02f), last);
        var c = V(0, 0.58f, 0.02f);
        var r = V(0.045f, 0.04f, 0.055f);
        b.Ell(head, c, r, green);
        b.Ell(head, c + V(0, -0.012f, 0.045f), V(0.03f, 0.02f, 0.03f), green, blend: 0.012f);
        b.PaintEll(head, c + V(0, -0.028f, 0.03f), V(0.03f, 0.01f, 0.05f), belly);
        b.Mark(head, On(c, r, 0, c.Y - 0.022f) + V(0, 0, 0.03f), V(0, -0.2f, 1f), 0.016f, 0.006f, Rgb(70, 110, 50), MarkShape.Smile);
        PokeBuilder.Both(s =>
        {
            var at = Out(c, r, default, V(0.62f * s, 0.25f, 0.72f));
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(70, 50, 40));
        });
        // The stalk and its leaf on top of its head
        b.Limb(head, c + V(0, 0.035f, -0.005f), c + V(-0.01f, 0.08f, -0.01f), 0.007f, 0.006f, stalk, Shell, 0.003f);
        Frond(b, head, c + V(-0.008f, 0.07f, -0.01f), c + V(0.06f, 0.12f, -0.02f), 0.025f, green, V(0, 0.4f, 1f), 0.2f, Leaf);
        // The wings of apple skin, cut into feathers, red over cream
        PokeBuilder.Both(s =>
        {
            var root = V(0.025f * s, 0.45f, -0.1f);
            int wing = b.Wing(s, root);
            for (int i = 0; i < 5; i++)
            {
                float t = i / 4f;
                var tip = root + V((0.24f - 0.06f * t) * s, 0.2f - 0.17f * t, -0.06f - 0.04f * t);
                Frond(b, wing, root, tip, 0.038f, red, V(0, 0.3f, 1f), 0.18f, Shell, 0.008f);
                b.PaintEll(wing, Vector3.Lerp(root, tip, 0.45f), V(0.018f, 0.018f, 0.018f), cream);
            }
        });
        return Lift(b.Hover());
    }

    // ------------------------------------------------------------------ 2842 Crumbledrake, from Appletun

    /// <summary>
    /// Crumbledrake: Appletun grown into a long-necked beast of cream pastry, its apple shell gone dark red and cracked
    /// over baked apple that glows amber, its top a heap of golden crumble, a sleepy cream head with drooping ears of
    /// crust, leaves of red apple skin round its ankles and a tail of twisted pastry.
    /// </summary>
    private static PokeBuilder Crumbledrake()
    {
        var b = Become(Appletun(), "Crumbledrake", 0.95f);
        var cream = Rgb(242, 228, 186);
        var shell = Rgb(160, 40, 44);
        var gold = Rgb(222, 170, 88);
        var light = Rgb(240, 196, 116);
        var amber = Rgb(250, 180, 80);
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        // The lattice and the leaf go, the head and tail are rebuilt
        b.Sdf.RemoveAll(s => s.Bone == head || s.Bone == tail || Is(s.Color, Rgb(250, 208, 130)) || Is(s.Color, Rgb(120, 82, 52)) || (s.Bone == Body && Is(s.Color, Rgb(120, 172, 82)) && (s.Shape != SdfShape.Ellipsoid || s.A.Y > 0.4f)));
        b.Model.Decals.RemoveAll(d => d.Bone == head);
        Recolour(b, (Rgb(120, 172, 82), cream), (Rgb(236, 180, 96), shell));
        var bc = V(0, 0.22f, -0.02f);
        var cc = V(0, 0.32f, -0.04f);
        var cr = V(0.22f, 0.15f, 0.25f);
        // Cracks of baked apple glowing through the shell
        foreach (var (dir, roll) in new[] { (V(0.9f, 0.1f, 0.3f), 20f), (V(-0.9f, 0.0f, -0.2f), -30f), (V(0.6f, 0.0f, -0.8f), 60f), (V(-0.5f, 0.1f, 0.8f), -10f), (V(0.95f, -0.1f, -0.3f), -50f), (V(-0.9f, -0.1f, 0.4f), 40f) })
            PaintAs(b, Body, Out(cc, cr, default, dir), V(0.006f, 0.05f, 0.006f), amber, Glow, V(0, 0, roll));
        // The crumble heaped on top
        var top = cc + V(0, 0.09f, 0);
        b.Ell(Body, top, V(0.2f, 0.07f, 0.23f), gold, mat: Shell, blend: 0.02f);
        Lumps(b, Body, top + V(0, 0.02f, 0), V(0.18f, 0.06f, 0.21f), 40, 0.032f, gold, light, 9f);
        // The long neck and the sleepy head
        var neck = Smooth(3, bc + V(0, 0.02f, 0.18f), bc + V(0, 0.16f, 0.26f), bc + V(0, 0.32f, 0.26f), bc + V(0, 0.4f, 0.3f));
        b.Tube(head, neck, 0.07f, 0.045f, cream, blend: 0.01f);
        b.PaintEll(head, bc + V(0, 0.12f, 0.29f), V(0.04f, 0.12f, 0.03f), Rgb(252, 244, 220));
        var c = bc + V(0, 0.44f, 0.34f);
        var r = V(0.065f, 0.055f, 0.08f);
        b.Ell(head, c, r, cream);
        b.Ell(head, c + V(0, -0.015f, 0.06f), V(0.045f, 0.035f, 0.04f), cream, blend: 0.015f);
        b.Mark(head, On(c, r, 0, c.Y - 0.03f) + V(0, 0, 0.04f), V(0, -0.2f, 1f), 0.022f, 0.008f, Rgb(120, 90, 60), MarkShape.Smile);
        PokeBuilder.Both(s =>
        {
            var at = Out(c, r, default, V(0.6f * s, 0.32f, 0.72f));
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(80, 70, 50), closed: false);
            b.PaintEll(head, at + V(0, 0.01f, 0), V(0.02f, 0.006f, 0.02f), Rgb(220, 196, 150), soft: 0.004f);
            // Drooping ears of crust
            Frond(b, head, c + V(0.05f * s, 0.03f, -0.03f), c + V(0.085f * s, -0.07f, -0.04f), 0.03f, gold, V(s, 0, 0.3f), 0.3f, Shell);
        });
        // Leaves of apple skin round the ankles
        foreach (var leg in b.Model.Bones.Select((bn, i) => (bn, i)).Where(x => x.bn.Role == PokeRole.Leg).Select(x => x.i).ToList())
        {
            var j = b.Model.Skeleton[leg].Joint;
            var at = V(j.X * 1.05f, 0.08f, j.Z + 0.01f);
            for (int k = -1; k <= 1; k++)
                Frond(b, leg, at + V(0.015f * k, -0.01f, 0.02f), at + V(0.03f * k, 0.05f, 0.05f), 0.022f, shell, V(0, 0, 1f), 0.25f, Shell);
        }
        // A tail of twisted pastry
        var tp = Smooth(3, bc + V(0, 0.0f, -0.22f), bc + V(0, 0.02f, -0.32f), bc + V(0, 0.08f, -0.4f), bc + V(0, 0.16f, -0.44f));
        b.Tube(tail, tp, 0.045f, 0.022f, gold, Shell, 0.01f);
        for (int i = 1; i < tp.Length - 1; i++)
        {
            float a = i * 1.9f;
            var d = tp[i + 1] - tp[i - 1];
            var side = Vector3.Normalize(Vector3.Cross(d, V(1f, 0, 0))) * MathF.Cos(a) + V(MathF.Sin(a), 0, 0);
            b.Ell(tail, tp[i] + side * 0.012f * (1f - i / (float)tp.Length), V(0.02f, 0.02f, 0.02f), light, mat: Shell, blend: 0.008f);
        }
        Stretch(b, V(1.06f, 1.1f, 1.08f));
        return b;
    }

    // ------------------------------------------------------------------ 2844 Duneconda, from Sandaconda

    /// <summary>
    /// Duneconda: Sandaconda longer and tan, its bands of brown diamonds, a second sack of sand along its body beside
    /// the one at its neck, the rim of its mouth a ring of orange scales and its tail's tip a rattle of hardened sand.
    /// </summary>
    private static PokeBuilder Duneconda()
    {
        var b = Become(Sandaconda(), "Duneconda", 0.95f);
        var tan = Rgb(196, 160, 104);
        var diamond = Rgb(112, 86, 58);
        var olive = Rgb(176, 146, 100);
        var orange = Rgb(238, 96, 48);
        Recolour(b, (Rgb(150, 128, 92), tan), (Rgb(230, 220, 190), Rgb(214, 208, 196)), (Rgb(140, 132, 90), olive), (Rgb(80, 74, 52), diamond));
        // The same points the forebear lies along
        var coil = Spiral3(0.24f, 0.13f, 0.07f, 0.15f, 2.0f, 1.25f, 20);
        var points = new[] { V(0.3f, 0.04f, -0.24f), V(0.27f, 0.05f, -0.2f) }.Concat(coil).Concat(new[] { V(0, 0.28f, 0.07f), V(0, 0.4f, 0.08f) }).ToArray();
        // Brown diamonds down its back
        for (int i = 8; i < points.Length - 3; i += 2)
        {
            int bone = i < 2 ? Body : b.Model.Skeleton.Find("seg" + i / 2);
            var d = points[i + 1] - points[i];
            var up = Vector3.Normalize(Vector3.Cross(Vector3.Cross(d, V(0, 1f, 0)), d));
            if (up.Y < 0) up = -up;
            float rad = 0.03f + 0.05f * MathF.Min(1f, i / (float)(points.Length - 1) * 2.5f);
            b.Mark(bone, points[i] + d * 0.5f + up * rad, up, rad * 0.6f, rad * 0.6f, diamond, MarkShape.Diamond);
        }
        // The second sack, along the coil
        int mid = b.Model.Skeleton.Find("seg4");
        var at = points[9];
        b.Torus(mid, at, 0.045f, 0.045f, olive, Euler(points[10] - points[8]), mat: Fur, blend: 0.015f);
        foreach (float a in new[] { 0.5f, 2.1f, 3.8f, 5.2f })
            b.PaintEll(mid, at + V(MathF.Sin(a) * 0.08f, 0.06f, MathF.Cos(a) * 0.12f), V(0.025f, 0.02f, 0.025f), PixelCanvas.Mix(olive, Black, 0.2f), soft: 0.01f);
        // The rattle at its tail's tip
        var tip = points[0];
        var back = Vector3.Normalize(points[0] - points[2]);
        var upT = V(0, 1f, 0);
        for (int i = 0; i < 6; i++)
        {
            var p = tip + back * 0.012f * i + upT * 0.025f * i;
            b.Ell(Body, p, V(0.024f - 0.002f * i, 0.016f, 0.024f - 0.002f * i), i % 2 == 0 ? Rgb(204, 168, 110) : Rgb(176, 140, 90), Euler(back + upT * 2f), Shell, 0.006f);
        }
        // The ring of orange scales round its mouth, on a tan head
        int head = BoneOf(b, "head");
        Tint(b, s => s.Bone == head && s.Op == SdfOp.Add, tan);
        var c = V(0, 0.4f, 0.08f) + V(0, 0.1f, 0.05f);
        for (int i = 0; i < 13; i++)
        {
            float a = (i / 12f - 0.5f) * 2.8f;
            Bead(b, head, c + V(MathF.Sin(a) * 0.072f, -0.03f, MathF.Cos(a) * 0.09f), 0.011f, orange, Shell, 0.004f);
        }
        Stretch(b, V(1.1f, 1.04f, 1.1f));
        return b;
    }

    // ------------------------------------------------------------------ 2845 Gannetgulp, from Cramorant

    /// <summary>
    /// Gannetgulp: Cramorant larger and a deeper blue, a gannet's sharp white head and crest, a slate beak with a
    /// fish's tail sticking out of it, a throat pouch of yellow twice the size and long black tips to its wings and
    /// tail; its blank stare is kept.
    /// </summary>
    private static PokeBuilder Gannetgulp()
    {
        var b = Become(Cramorant(), "Gannetgulp", 0.85f);
        var blue = Rgb(58, 112, 204);
        var dark = Rgb(40, 62, 124);
        var navy = Rgb(48, 78, 132);
        var pale = Rgb(92, 120, 170);
        var white = Rgb(244, 242, 236);
        var yellow = Rgb(240, 208, 104);
        var slate = Rgb(104, 136, 158);
        var black = Rgb(30, 32, 38);
        int head = BoneOf(b, "head");
        Tint(b, s => s.Bone == head && (Is(s.Color, blue) || Is(s.Color, dark)), white);
        Recolour(b, (blue, navy), (dark, black), (Rgb(240, 238, 248), pale), (Rgb(242, 200, 62), slate), (PixelCanvas.Mix(Rgb(242, 200, 62), White, 0.3f), slate));
        var bc = V(0, 0.21f, 0);
        var c = bc + V(0, 0.24f, 0.05f) + V(0, 0.03f, 0.02f);
        var root = c + V(0, -0.005f, 0.04f);
        // The pouch, great and yellow, hanging under the beak
        b.Ell(head, root + V(0, -0.08f, 0.02f), V(0.055f, 0.085f, 0.05f), yellow, mat: Shell, blend: 0.02f);
        b.Ell(head, root + V(0, -0.02f, 0.06f), V(0.03f, 0.03f, 0.06f), yellow, mat: Shell, blend: 0.02f);
        b.PaintEll(head, root + V(-0.03f, -0.07f, 0.06f), V(0.015f, 0.025f, 0.01f), Rgb(250, 236, 160));
        // A fish's tail sticking out of the beak
        var fish = root + V(0.02f, -0.04f, 0.16f);
        b.Limb(head, root + V(0.01f, -0.03f, 0.12f), fish, 0.012f, 0.008f, Rgb(110, 150, 170), blend: 0.004f);
        Frond(b, head, fish, fish + V(0.04f, -0.03f, 0.02f), 0.016f, Rgb(110, 150, 170), V(0, 0, 1f), 0.25f);
        Frond(b, head, fish, fish + V(0.0f, -0.05f, 0.03f), 0.016f, Rgb(110, 150, 170), V(1f, 0, 0), 0.25f);
        // A white crest point, and long black tips to its wings and tail
        b.Spike(head, c + V(0, 0.03f, -0.02f), c + V(0, 0.06f, -0.06f), 0.015f, white, 0.5f);
        PokeBuilder.Both(s =>
        {
            int w = BoneOf(b, s < 0 ? "wingL" : "wingR");
            Blade(b, w, bc + V(0.08f * s, -0.04f, -0.08f), bc + V(0.1f * s, -0.1f, -0.26f), 0.03f, black, V(s, 0, 0));
        });
        int tail = BoneOf(b, "tail");
        Blade(b, tail, bc + V(0, -0.06f, -0.08f), bc + V(0, -0.1f, -0.26f), 0.03f, black, V(0, 1f, 0));
        Stretch(b, V(1.1f, 1.12f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2847 Rapierfin, from Barraskewda

    /// <summary>
    /// Rapierfin: Barraskewda longer and thinner, brown above and yellow below, its jaw grown into a long rapier's
    /// blade of bright steel, a basket-hilt of steel bars and fins behind its head, its fins of steel and its tail a
    /// propeller of four steel blades.
    /// </summary>
    private static PokeBuilder Rapierfin()
    {
        var b = Become(Barraskewda(), "Rapierfin", 0.9f);
        Unlift(b, V(0, 0.32f, 0));
        var brown = Rgb(98, 84, 66);
        var yellow = Rgb(226, 184, 70);
        var steel = Rgb(196, 202, 212);
        var grey = Rgb(124, 112, 102);
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        // The fishbone and the pale jaws go; the red fins become steel
        b.Sdf.RemoveAll(s => (s.Op == SdfOp.Paint && Is(s.Color, Rgb(236, 222, 180))) || (s.Bone == head && s.Shape != SdfShape.Ellipsoid) || s.Bone == tail);
        Recolour(b, (grey, brown), (Rgb(236, 222, 180), yellow), (Rgb(228, 84, 54), steel));
        Make(b, s => Is(s.Color, steel), Metal);
        var bc = V(0, 0.32f, -0.04f);
        b.PaintEll(Body, bc + V(0, -0.045f, 0), V(0.065f, 0.035f, 0.26f), yellow);
        var c = bc + V(0, 0.005f, 0.24f);
        b.PaintEll(head, c + V(0, -0.03f, 0.02f), V(0.055f, 0.03f, 0.07f), yellow);
        // The rapier
        b.Spike(head, c + V(0, -0.01f, 0.04f), c + V(0, -0.02f, 0.36f), 0.026f, steel, 0.85f, Metal);
        b.Ell(head, c + V(0, -0.01f, 0.06f), V(0.035f, 0.025f, 0.03f), steel, mat: Metal, blend: 0.008f);
        // The basket-hilt behind the head: steel bars arching over and round it
        for (int i = -2; i <= 2; i++)
            b.Torus(head, c + V(0, 0.005f, -0.04f), 0.07f, 0.005f, steel, V(i * 22f, 0, 90f), 1f, 0.6f, Metal, 0.003f);
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < 4; i++)
                b.Spike(head, c + V(0.04f * s, -0.01f, -0.04f), c + V(0.1f * s, 0.05f - i * 0.03f, -0.1f), 0.008f, steel, 0.5f, Metal, 0.004f);
        });
        // The tail: a propeller of four blades
        var tr = bc + V(0, 0, -0.25f);
        b.Limb(tail, tr + V(0, 0, 0.01f), tr + V(0, 0, -0.08f), 0.03f, 0.015f, brown);
        var hub = tr + V(0, 0, -0.09f);
        b.Ell(tail, hub, V(0.018f, 0.018f, 0.02f), steel, mat: Metal, blend: 0.006f);
        for (int i = 0; i < 4; i++)
        {
            float a = MathF.PI / 4f + i * MathF.PI / 2f;
            var d = V(MathF.Cos(a), MathF.Sin(a), 0);
            Blade(b, tail, hub, hub + d * 0.1f + V(0, 0, -0.02f), 0.035f, steel, V(0, 0, 1f) + V(-d.Y, d.X, 0) * 0.5f, 0.2f, Metal);
        }
        Stretch(b, V(0.9f, 0.95f, 1.12f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2849 Amplipunk, from Toxtricity

    /// <summary>
    /// Amplipunk: Toxtricity taller, streaked yellow and blue, its crest grown into a glowing mohawk of five spikes,
    /// the frets on its chest a full row of glowing strings with a second row of cyan strings down its back, a
    /// speaker set in its shoulder, dark spines down its forearms, and its tail a long curve stacked with three
    /// amplifiers.
    /// </summary>
    private static PokeBuilder Amplipunk()
    {
        var b = Become(Toxtricity(), "Amplipunk", 0.97f);
        var purple = Rgb(118, 80, 166);
        var dark = Rgb(76, 54, 108);
        var yellow = Rgb(250, 228, 80);
        var blue = Rgb(70, 140, 230);
        var cyan = Rgb(110, 236, 230);
        var metal = Rgb(150, 150, 166);
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => (s.Bone == head && Is(s.Color, Rgb(250, 220, 70)) && s.Op == SdfOp.Add) || s.Bone == tail);
        Recolour(b, (Rgb(140, 98, 182), purple));
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
            d.Iris = cyan;
        var bc = V(0, 0.56f, 0);
        var c = bc + V(0, 0.18f, 0.03f);
        // The mohawk: five glowing spikes front to back
        for (int i = 0; i < 5; i++)
        {
            float z = 0.04f - i * 0.03f;
            b.Spike(head, c + V(0, 0.04f, z), c + V(0, 0.18f - MathF.Abs(i - 1.5f) * 0.02f, z - 0.04f - i * 0.01f), 0.02f, yellow, 0.5f, Glow, 0.006f);
        }
        PokeBuilder.Both(s => b.Spike(head, c + V(0.04f * s, 0.02f, -0.02f), c + V(0.07f * s, 0.03f, -0.05f), 0.01f, Rgb(214, 210, 196), mat: Shell, blend: 0.003f));
        // Strings down its chest, and a second row down its back
        for (int i = -2; i <= 2; i++)
        {
            b.Limb(Body, bc + V(0.009f * i, 0.05f, 0.046f), bc + V(0.009f * i, -0.07f, 0.039f), 0.004f, 0.004f, i % 2 == 0 ? yellow : cyan, Glow, 0.002f);
            b.Tube(Body, Smooth(2, bc + V(0.012f * i, 0.08f, -0.035f), bc + V(0.014f * i, 0.0f, -0.051f), bc + V(0.016f * i, -0.08f, -0.035f)), 0.005f, 0.005f, cyan, Glow, 0.002f);
        }
        // Streaks of yellow and blue over its limbs and flanks
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var elbow = bc + V(0.11f * s, -0.02f, 0.0f);
            var hand = bc + V(0.1f * s, -0.12f, 0.05f);
            PaintAs(b, arm, Vector3.Lerp(elbow, hand, 0.4f), V(0.02f, 0.03f, 0.02f), yellow, Glow);
            b.PaintEll(arm, Vector3.Lerp(elbow, hand, 0.7f), V(0.02f, 0.02f, 0.02f), blue);
            for (int i = 0; i < 3; i++)
                b.Spike(arm, Vector3.Lerp(elbow, hand, 0.2f + 0.25f * i) + V(0.012f * s, 0, -0.012f), Vector3.Lerp(elbow, hand, 0.2f + 0.25f * i) + V(0.04f * s, 0.02f, -0.04f), 0.01f, dark, 0.6f);
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            PaintAs(b, leg, V(0.06f * s, 0.32f, 0.02f), V(0.025f, 0.05f, 0.025f), yellow, Glow, V(0, 0, 20f * s));
            b.PaintEll(leg, V(0.065f * s, 0.15f, 0.03f), V(0.02f, 0.04f, 0.02f), blue);
            PaintAs(b, Body, bc + V(0.05f * s, -0.03f, 0.02f), V(0.012f, 0.05f, 0.02f), yellow, Glow, V(0, 0, -15f * s));
        });
        // A speaker in its right shoulder
        int armR = BoneOf(b, "armR");
        var sh = bc + V(0.07f, 0.08f, 0.0f);
        b.Ell(armR, sh, V(0.03f, 0.03f, 0.03f), metal, mat: Metal, blend: 0.008f);
        b.Torus(armR, sh + V(0.022f, 0, 0), 0.02f, 0.005f, Rgb(70, 70, 80), V(0, 0, 90f), mat: Metal, blend: 0.003f);
        b.PaintEll(armR, sh + V(0.03f, 0, 0), V(0.01f, 0.014f, 0.014f), Rgb(40, 40, 46));
        // The tail: a long curve, three amplifiers stacked along it
        var tp = Smooth(3, V(0, 0.44f, -0.03f), V(0, 0.3f, -0.14f), V(0, 0.2f, -0.28f), V(0, 0.26f, -0.4f), V(0, 0.4f, -0.44f), V(0, 0.5f, -0.4f));
        b.Tube(tail, tp, 0.04f, 0.01f, purple, blend: 0f);
        foreach (float t in new[] { 0.48f, 0.62f, 0.76f })
        {
            var at = tp[(int)(t * (tp.Length - 1))];
            b.Box(tail, at, V(0.045f, 0.035f, 0.035f), 0.006f, metal, V(0, 0, 0), Metal, 0.004f);
            PokeBuilder.Both(s => b.PaintEll(tail, at + V(0.045f * s, 0, 0), V(0.006f, 0.026f, 0.026f), Rgb(60, 60, 70), soft: 0.003f));
        }
        Stretch(b, V(0.95f, 1.12f, 0.95f));
        return b;
    }

    // ------------------------------------------------------------------ 2851 Furnapede, from Centiskorch

    /// <summary>
    /// Furnapede: Centiskorch longer and a deep brick red, each segment's side a furnace's door with a grille of fire
    /// glowing behind it, its legs black and hooked, its whiskers of flame grown into two pairs of long whip-like
    /// antennae tipped with flame, and heavy black jaws sooty and toothed.
    /// </summary>
    private static PokeBuilder Furnapede()
    {
        var b = Become(Centiskorch(), "Furnapede", 0.97f);
        var brick = Rgb(150, 46, 38);
        var band = Rgb(96, 30, 28);
        var soot = Rgb(52, 46, 48);
        var iron = Rgb(70, 60, 58);
        var fire = Rgb(250, 156, 50);
        int head = BoneOf(b, "head");
        b.Sdf.RemoveAll(s => s.Bone == head && s.Shape != SdfShape.Ellipsoid);
        Recolour(b, (Rgb(222, 84, 46), brick), (Rgb(140, 40, 36), band), (Rgb(250, 196, 70), PixelCanvas.Mix(brick, fire, 0.35f)));
        var points = Smooth(3, V(0.22f, 0.05f, -0.3f), V(0.2f, 0.05f, -0.12f), V(0.05f, 0.05f, -0.02f), V(-0.02f, 0.12f, 0.06f), V(0, 0.3f, 0.06f), V(0, 0.48f, 0.04f), V(0, 0.6f, 0.08f));
        // A furnace door on each side of every other segment
        for (int i = 1; i < points.Length - 1; i += 2)
        {
            int seg = i < 2 ? Body : b.Model.Skeleton.Find("seg" + i / 2);
            var d = Vector3.Normalize(points[i + 1] - points[i]);
            var side = Vector3.Normalize(Vector3.Cross(d, V(0, 1f, 0)) + V(0, 0.0001f, 0));
            if (MathF.Abs(d.Y) > 0.8f) side = V(1f, 0, 0);
            var up = Vector3.Normalize(Vector3.Cross(side, d));
            float rad = 0.055f * (0.7f + 0.3f * MathF.Min(1f, i / (float)(points.Length - 1) * 3f));
            var mid = (points[i] + points[i + 1]) * 0.5f;
            PokeBuilder.Both(s =>
            {
                var at = mid + side * s * rad * 0.92f + up * rad * 0.2f;
                Plate(b, seg, at - side * s * rad * 0.06f, side * s, V(rad * 0.55f, 0.008f, rad * 0.5f), iron, Metal, 0.004f);
                for (int k = -1; k <= 1; k++)
                    b.Limb(seg, at + d * (k * rad * 0.22f) - up * rad * 0.28f, at + d * (k * rad * 0.22f) + up * rad * 0.28f, rad * 0.07f, rad * 0.07f, fire, Glow, 0.002f);
            });
        }
        var c = points[^1] + V(0, 0.03f, 0.05f);
        var r = V(0.065f, 0.055f, 0.06f);
        b.PaintEll(head, c + V(0, -0.02f, 0.05f), V(0.05f, 0.03f, 0.03f), soot);
        PokeBuilder.Both(s =>
        {
            // Two pairs of whip-like antennae, flame at their ends
            foreach (var (lift, spread) in new[] { (0.22f, 0.12f), (0.14f, 0.2f) })
            {
                var root = c + V(0.03f * s, 0.04f, 0.02f);
                var whip = Smooth(3, root, root + V(spread * 0.4f * s, lift * 0.6f, 0.04f), root + V(spread * s, lift, -0.02f), root + V(spread * 1.4f * s, lift * 0.85f, -0.08f));
                b.Tube(head, whip, 0.008f, 0.006f, band, Shell, 0.003f);
                FlameTongue(b, head, whip[^1], whip[^1] + V(0.03f * s, 0.03f, -0.02f), 0.01f, Rgb(240, 90, 40), Rgb(255, 220, 110));
            }
            // A heavy black jaw, toothed on its inner edge
            var jaw = Smooth(2, c + V(0.04f * s, -0.03f, 0.04f), c + V(0.08f * s, -0.05f, 0.11f), c + V(0.04f * s, -0.06f, 0.17f), c + V(0.0f * s, -0.065f, 0.18f));
            b.Tube(head, jaw, 0.018f, 0.008f, soot, Shell, 0.006f);
            for (int k = 1; k < 3; k++)
                b.Spike(head, jaw[k * 2], jaw[k * 2] + V(-0.02f * s, 0.0f, 0.005f), 0.006f, soot, mat: Shell, blend: 0.002f);
        });
        Stretch(b, V(1.06f, 1.06f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2853 Kraketsu, from Grapploct

    /// <summary>
    /// Kraketsu: Grapploct larger and a sea blue, its mask gone and its eyes narrowed, two tentacles curling down from
    /// its face like a moustache, a second pair of arms raised and flexed, every fist knuckled with pale suckers, and
    /// a champion's belt of dark blue round its middle.
    /// </summary>
    private static PokeBuilder Kraketsu()
    {
        var b = Become(Grapploct(), "Kraketsu", 0.95f);
        var blue = Rgb(70, 118, 178);
        var light = Rgb(110, 150, 200);
        var cream = Rgb(234, 222, 186);
        var belt = Rgb(36, 48, 84);
        b.Sdf.RemoveAll(s => s.Bone == Body && s.Op == SdfOp.Paint && Is(s.Color, Rgb(242, 214, 70)));
        Recolour(b, (Rgb(50, 56, 112), blue), (Rgb(242, 214, 70), cream), (Rgb(78, 86, 150), light));
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
            d.Iris = Rgb(220, 226, 236);
        var c = V(0, 0.52f, 0.01f);
        var r = V(0.14f, 0.13f, 0.12f);
        // A heavy brow over narrowed eyes
        b.Ell(Body, c + V(0, 0.035f, 0.09f), V(0.1f, 0.022f, 0.035f), blue, blend: 0.02f);
        // Two tentacles curling down from its face
        PokeBuilder.Both(s =>
        {
            var root = c + V(0.035f * s, -0.07f, 0.1f);
            b.Tube(Body, new[] { root }.Concat(Spiral(root + V(0.04f * s, -0.07f, 0.02f), V(s, 0, 0), V(0, 1f, 0), 0.04f, 0.01f, MathF.PI * 1.1f, MathF.PI * 3f, 12)).ToArray(), 0.022f, 0.008f, blue, blend: 0.006f);
        });
        // The belt
        var bc = V(0, 0.34f, 0);
        b.Torus(Body, bc + V(0, 0.02f, 0), 0.115f, 0.025f, belt, V(0, 0, 0), 1f, 0.9f, Shell, 0.006f);
        b.Ell(Body, bc + V(0, 0.02f, 0.11f), V(0.06f, 0.04f, 0.02f), belt, mat: Shell, blend: 0.006f);
        // A second pair of arms, raised and flexed
        PokeBuilder.Both(s =>
        {
            int arm = b.Part(s < 0 ? "arm2L" : "arm2R", Body, c + V(0.12f * s, -0.02f, -0.02f), PokeRole.Arm, s, s);
            var shoulder = c + V(0.11f * s, -0.02f, -0.02f);
            var elbow = c + V(0.25f * s, 0.0f, -0.02f);
            var fist = c + V(0.24f * s, 0.16f, 0.0f);
            b.Limb(arm, shoulder, elbow, 0.05f, 0.045f, blue);
            b.Limb(arm, elbow, fist, 0.045f, 0.045f, blue);
            b.Ell(arm, fist, V(0.055f, 0.06f, 0.055f), blue);
            b.PaintEll(arm, Vector3.Lerp(elbow, fist, 0.5f) + V(-0.03f * s, 0, 0.02f), V(0.03f, 0.06f, 0.03f), cream);
            foreach (float x in new[] { -0.025f, 0f, 0.025f })
                Bead(b, arm, fist + V(x, 0.055f, 0.025f), 0.012f, cream, Fur, 0.004f);
            int low = BoneOf(b, s < 0 ? "armL" : "armR");
            var lfist = c + V(0.2f * s, -0.28f, 0.12f);
            foreach (float x in new[] { -0.025f, 0f, 0.025f })
                Bead(b, low, lfist + V(x, 0.0f, 0.06f), 0.012f, cream, Fur, 0.004f);
        });
        Stretch(b, V(1.08f, 1.1f, 1.08f));
        return b;
    }

    // ------------------------------------------------------------------ 2855 Urngeist, from Polteageist

    /// <summary>
    /// Urngeist: Polteageist's tea grown into a tall tea urn of pale china on a footed stand, gold rims and painted
    /// scrolls, a little brass tap low on its front, the chipped spout kept, its lid lifted aside by the ghost of the
    /// tea rising out of it as a curl of purple steam with a grinning face, and two cups floating beside it with tea
    /// in them. Built anew, its pot become an urn, from Polteageist's colours and parts.
    /// </summary>
    private static PokeBuilder Urngeist()
    {
        var b = new PokeBuilder("Urngeist", 0.85f, BodyPlan.Floating, V(0, 0.28f, 0)) { Coat = Shell }.Hover();
        var china = Rgb(226, 236, 240);
        var white = Rgb(244, 246, 246);
        var scroll = Rgb(120, 150, 200);
        var brass = Rgb(176, 140, 70);
        var tea = TeaPurple;
        // The urn: a tall round body on a footed stand, hollowed at the top
        b.Limb(Body, V(0, 0.15f, 0), V(0, 0.43f, 0), 0.12f, 0.11f, china, blend: 0.03f);
        b.Ell(Body, V(0, 0.15f, 0), V(0.115f, 0.04f, 0.115f), china, blend: 0.02f);
        b.Limb(Body, V(0, 0.02f, 0), V(0, 0.12f, 0), 0.08f, 0.05f, china, blend: 0.02f);
        b.Ell(Body, V(0, 0.02f, 0), V(0.085f, 0.02f, 0.085f), china, blend: 0.01f);
        b.Cut(Body, V(0, 0.5f, 0), V(0.09f, 0.08f, 0.09f));
        b.Ell(Body, V(0, 0.4f, 0), V(0.085f, 0.03f, 0.085f), tea, mat: Glow, blend: 0.01f);
        b.Torus(Body, V(0, 0.45f, 0), 0.1f, 0.01f, TeaGold, mat: Metal, blend: 0.004f);
        b.Torus(Body, V(0, 0.36f, 0), 0.118f, 0.005f, TeaGold, mat: Metal, blend: 0.003f);
        b.Torus(Body, V(0, 0.17f, 0), 0.118f, 0.005f, TeaGold, mat: Metal, blend: 0.003f);
        b.Torus(Body, V(0, 0.03f, 0), 0.08f, 0.008f, TeaGold, mat: Metal, blend: 0.003f);
        foreach (float a in new[] { 0.3f, 1.4f, 2.6f, 3.7f, 4.9f })
            b.PaintEll(Body, V(MathF.Sin(a) * 0.12f, 0.29f, MathF.Cos(a) * 0.12f), V(0.025f, 0.012f, 0.025f), scroll, soft: 0.005f);
        // The brass tap, low on its front
        b.Limb(Body, V(0.02f, 0.2f, 0.1f), V(0.02f, 0.2f, 0.16f), 0.012f, 0.011f, brass, Metal, 0.004f);
        b.Limb(Body, V(0.02f, 0.2f, 0.16f), V(0.02f, 0.16f, 0.17f), 0.01f, 0.009f, brass, Metal, 0.003f);
        b.Limb(Body, V(0.02f, 0.2f, 0.15f), V(0.02f, 0.25f, 0.15f), 0.006f, 0.006f, brass, Metal, 0.003f);
        b.Limb(Body, V(0.0f, 0.25f, 0.15f), V(0.04f, 0.25f, 0.15f), 0.006f, 0.006f, brass, Metal, 0.003f);
        // The chipped spout
        b.Tube(Body, Smooth(2, V(-0.09f, 0.33f, 0.04f), V(-0.16f, 0.36f, 0.06f), V(-0.2f, 0.42f, 0.07f)), 0.03f, 0.016f, white, blend: 0.01f);
        b.Cut(Body, V(-0.205f, 0.43f, 0.08f), V(0.012f, 0.01f, 0.012f));
        // The ghost of the tea rising out of the top, its face grinning, an arm of steam reaching out
        var gc = V(-0.02f, 0.56f, 0.0f);
        var gr = V(0.07f, 0.08f, 0.065f);
        b.Tube(Body, Smooth(2, V(0, 0.4f, 0), V(0.02f, 0.46f, 0), gc - V(0, 0.05f, 0)), 0.05f, 0.05f, tea, Glow, 0.02f);
        b.Ell(Body, gc, gr, tea, mat: Glow, blend: 0.02f);
        foreach (var (from, to) in new[] { (V(0.02f, 0.06f, -0.02f), V(0.06f, 0.16f, -0.04f)), (V(-0.03f, 0.06f, -0.02f), V(-0.05f, 0.13f, -0.03f)), (V(0.05f, 0.03f, -0.01f), V(0.1f, 0.09f, -0.02f)) })
            b.Spike(Body, gc + from, gc + to, 0.025f, tea, 0.6f, Glow, 0.01f);
        b.Tube(Body, Smooth(2, gc + V(-0.05f, -0.03f, 0.0f), gc + V(-0.12f, -0.03f, 0.02f), gc + V(-0.17f, -0.07f, 0.03f)), 0.022f, 0.008f, tea, Glow, 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = Out(gc, gr, default, V(0.4f * s, 0.15f, 0.9f));
            b.Eye(Body, at, Outward(gc, gr, at), 0.014f, sclera: true, white: Rgb(240, 236, 252), pupil: Rgb(220, 200, 250), glare: true);
        });
        b.Mark(Body, Out(gc, gr, default, V(0, -0.4f, 0.9f)), V(0, -0.3f, 1f), 0.03f, 0.012f, Rgb(70, 30, 90), MarkShape.Smile);
        // The lid, lifted aside on the steam
        var lid = V(0.12f, 0.5f, 0.0f);
        b.Ell(Body, lid, V(0.065f, 0.018f, 0.065f), china, V(0, 0, -35f), blend: 0.006f);
        b.Ell(Body, lid + V(0.012f, 0.02f, 0), V(0.016f, 0.016f, 0.016f), china, blend: 0.006f);
        b.Limb(Body, V(0.04f, 0.42f, 0), lid - V(0.02f, 0.01f, 0), 0.02f, 0.012f, tea, Glow, 0.01f);
        // Two cups floating beside it, tea in each
        foreach (var (at, s) in new[] { (V(-0.22f, 0.18f, 0.06f), -1f), (V(0.22f, 0.12f, 0.04f), 1f) })
        {
            b.Ell(Body, at, V(0.05f, 0.04f, 0.05f), china, blend: 0.004f);
            b.CutBox(Body, at + V(0, 0.045f, 0), V(0.07f, 0.02f, 0.07f), Quaternion.Identity);
            b.Ell(Body, at + V(0, 0.015f, 0), V(0.04f, 0.012f, 0.04f), tea, mat: Glow, blend: 0.004f);
            b.Torus(Body, at + V(0.055f * s, 0.005f, 0), 0.016f, 0.005f, china, V(90f, 0, 0), mat: Shell, blend: 0.003f);
            b.Torus(Body, at + V(0, 0.02f, 0), 0.042f, 0.005f, TeaGold, mat: Metal, blend: 0.002f);
            b.Tube(Body, Smooth(2, at + V(0, 0.015f, 0), at + V(-0.03f * s, 0.12f, -0.01f), V(0.1f * s, at.Y + 0.1f, 0.0f)), 0.016f, 0.012f, tea, Glow, 0.004f);
        }
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2863 Jarlkatt, from Perrserker

    /// <summary>
    /// Jarlkatt: Perrserker larger and a dark brown, wrapped in a shaggy cloak of fur, its coin gone under a horned
    /// iron helm with a nose-guard, its beard braided in two and bound in iron, a round iron shield on its left
    /// forearm and long cream claws.
    /// </summary>
    private static PokeBuilder Jarlkatt()
    {
        var b = Become(Perrserker(), "Jarlkatt", 0.9f);
        var fur = Rgb(96, 72, 54);
        var dark = Rgb(72, 54, 42);
        var beard = Rgb(110, 84, 62);
        var iron = Rgb(78, 78, 84);
        var rim = Rgb(108, 108, 116);
        var horn = Rgb(64, 60, 58);
        int head = BoneOf(b, "head");
        b.Sdf.RemoveAll(s => s.Bone == head && (Is(s.Color, Rgb(150, 152, 164)) || Is(s.Color, Rgb(52, 46, 44))));
        Recolour(b, (Rgb(70, 62, 58), fur), (Rgb(200, 198, 194), beard), (Rgb(176, 150, 120), Rgb(150, 116, 88)), (Rgb(52, 46, 44), dark));
        var bc = V(0, 0.3f, 0);
        var c = bc + V(0, 0.18f, 0.05f);
        // The helm, its rim, its nose-guard, rivets and horns
        b.Ell(head, c + V(0, 0.035f, -0.005f), V(0.105f, 0.065f, 0.085f), iron, mat: Metal, blend: 0.006f);
        b.Torus(head, c + V(0, 0.02f, -0.005f), 0.098f, 0.008f, rim, V(0, 0, 0), 1f, 0.85f, Metal, 0.003f);
        b.Box(head, c + V(0, 0.0f, 0.08f), V(0.008f, 0.03f, 0.006f), 0.003f, rim, V(-20f, 0, 0), Metal, 0.003f);
        b.Limb(head, c + V(0, 0.1f, 0.0f), c + V(0, 0.04f, 0.085f), 0.006f, 0.006f, rim, Metal, 0.003f);
        foreach (float a in new[] { -1.2f, -0.6f, 0.6f, 1.2f })
            Bead(b, head, c + V(MathF.Sin(a) * 0.1f, 0.025f, MathF.Cos(a) * 0.085f), 0.006f, rim, Metal);
        PokeBuilder.Both(s =>
        {
            var root = c + V(0.08f * s, 0.06f, -0.01f);
            b.Tube(head, Smooth(3, root, root + V(0.06f * s, 0.03f, 0), root + V(0.09f * s, 0.09f, -0.01f), root + V(0.07f * s, 0.14f, 0.0f)), 0.026f, 0.006f, horn, Shell, 0.006f);
        });
        // The beard braided in two, bound in iron
        PokeBuilder.Both(s =>
        {
            var top = c + V(0.035f * s, -0.07f, 0.06f);
            var path = Smooth(2, top, top + V(0.005f * s, -0.07f, 0.02f), top + V(0, -0.15f, 0.02f));
            b.Tube(head, path, 0.02f, 0.012f, beard, blend: 0.004f);
            for (int i = 1; i < path.Length; i++)
                b.PaintTorus(head, path[i], 0.018f, 0.003f, dark, Euler(path[i] - path[i - 1]));
            b.Torus(head, Vector3.Lerp(path[^2], path[^1], 0.3f), 0.016f, 0.006f, rim, Euler(path[^1] - path[^2]), mat: Metal, blend: 0.003f);
        });
        // A shaggy cloak of fur over its shoulders and back
        var cloak = bc + V(0, 0.08f, -0.04f);
        b.Ell(Body, cloak, V(0.18f, 0.12f, 0.13f), dark, blend: 0.03f);
        FurTufts(b, Body, cloak + V(0, -0.02f, -0.02f), V(0.17f, 0.12f, 0.13f), 30, 0.07f, 0.024f, dark, 0.5f, -0.9f, 0.9f);
        // The round iron shield on its left forearm
        int armL = BoneOf(b, "armL");
        var hand = bc + V(-0.2f, -0.07f, 0.08f);
        var face = Vector3.Normalize(V(-0.7f, 0.05f, 0.7f));
        var sc = hand + V(-0.035f, 0.03f, 0.02f);
        Plate(b, armL, sc, face, V(0.11f, 0.014f, 0.11f), iron, Metal, 0.006f);
        b.Torus(armL, sc + face * 0.008f, 0.105f, 0.008f, rim, Euler(face), mat: Metal, blend: 0.003f);
        b.Ell(armL, sc + face * 0.016f, V(0.028f, 0.028f, 0.028f), rim, mat: Metal, blend: 0.004f);
        // Long cream claws on its hands and feet
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            Claws(b, arm, bc + V(0.2f * s, -0.1f, 0.12f), V(0.016f, 0, 0), V(0, -0.8f, 0.5f), 0.04f, 0.009f);
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            Claws(b, leg, V(0.08f * s, 0.02f, 0.1f), V(0.018f, 0, 0), V(0, -0.3f, 1f), 0.03f, 0.009f);
        });
        Stretch(b, V(1.1f, 1.12f, 1.1f));
        return b;
    }

    // ------------------------------------------------------------------ 2864 Wraithreef, from Cursola

    /// <summary>
    /// Wraithreef: Cursola larger and wider, its head become a core of three ghostly spheres glowing pale violet, each
    /// with a face, a crown of bleached branches spread round them like antlers, streamers of grey weed hanging from
    /// the branches and its base shattered into shards of dark rock.
    /// </summary>
    private static PokeBuilder Wraithreef()
    {
        var b = Become(Cursola(), "Wraithreef", 0.95f);
        Unlift(b, V(0, 0.36f, 0));
        var white = Rgb(236, 236, 240);
        var violet = Rgb(206, 194, 246);
        var weed = Rgb(104, 114, 104);
        var rock = Rgb(66, 70, 76);
        var hc = V(0, 0.36f, 0);
        // The ring of stone goes, shattered
        b.Sdf.RemoveAll(s => Is(s.Color, Rgb(190, 190, 200)) || (s.A.Y < 0.13f && s.Shape == SdfShape.RoundCone));
        foreach (var s in b.Sdf.Shapes.Where(s => s.Shape == SdfShape.Ellipsoid && Vector3.Distance(s.A, hc) < 0.001f))
            s.Color = violet;
        b.Sdf.Changed();
        b.Model.Decals.RemoveAll(d => !d.IsEye);
        // Two more spheres beside the first, a face on each
        foreach (var o in new[] { V(-0.12f, -0.08f, 0.06f), V(0.12f, -0.08f, 0.06f) })
        {
            var sc = hc + o;
            var sr = V(0.075f, 0.075f, 0.075f);
            b.Ell(Body, sc, sr, violet, mat: Glow, blend: 0.01f);
            PokeBuilder.Both(s =>
            {
                var at = On(sc, sr, sc.X + 0.024f * s, sc.Y + 0.006f);
                b.Eye(Body, at, Outward(sc, sr, at), 0.011f, sclera: true, pupil: Rgb(120, 90, 180), glare: true);
            });
            b.Mark(Body, On(sc, sr, sc.X, sc.Y - 0.03f), V(0, -0.3f, 1f), 0.015f, 0.008f, Rgb(120, 90, 180), MarkShape.Wave);
        }
        b.Mark(Body, On(hc, V(0.1f, 0.1f, 0.1f), 0, hc.Y - 0.04f), V(0, -0.3f, 1f), 0.02f, 0.008f, Rgb(120, 90, 180), MarkShape.Wave);
        // A crown of antler branches spreading wide, weed hanging from them
        PokeBuilder.Both(s =>
        {
            foreach (var (root, tip, twig) in new[]
            {
                (V(0.06f, 0.0f, -0.03f), V(0.34f, 0.22f, -0.04f), V(0.04f, 0.09f, 0.0f)),
                (V(0.07f, 0.03f, -0.04f), V(0.26f, 0.34f, -0.06f), V(0.06f, 0.05f, 0.0f)),
                (V(0.05f, -0.04f, -0.04f), V(0.3f, 0.05f, 0.0f), V(0.03f, 0.08f, 0.02f)),
            })
            {
                var r0 = hc + V(root.X * s, root.Y, root.Z);
                var t0 = hc + V(tip.X * s, tip.Y, tip.Z);
                Coral(b, Body, r0, t0, 0.02f, white, V(twig.X * s, twig.Y, twig.Z));
                var mid = Vector3.Lerp(r0, t0, 0.8f);
                b.Tube(Body, Smooth(2, mid, mid + V(0.01f * s, -0.08f, 0.02f), mid + V(-0.01f * s, -0.18f, 0.01f)), 0.012f, 0.005f, weed, Fur, 0.004f);
            }
            var drape = Smooth(2, hc + V(0.12f * s, 0.02f, 0.02f), hc + V(0.18f * s, -0.06f, 0.05f), hc + V(0.24f * s, 0.08f, 0.0f));
            b.Tube(Body, drape, 0.01f, 0.01f, weed, Fur, 0.004f);
        });
        // The shattered base: shards of dark rock and bleached coral round the wisp's end
        foreach (var (d, len) in new[] { (V(0.6f, -0.5f, 0.3f), 0.07f), (V(-0.6f, -0.6f, 0.2f), 0.06f), (V(0.1f, -1f, -0.4f), 0.07f), (V(-0.3f, -0.8f, 0.5f), 0.05f), (V(0.5f, -0.7f, -0.5f), 0.05f), (V(0f, -1f, 0.2f), 0.08f) })
        {
            var n = Vector3.Normalize(d);
            var root = V(0, 0.15f, 0) + n * 0.02f;
            b.Spike(Body, root, root + n * len, 0.022f, rock, 0.5f, Shell, 0.008f);
        }
        PokeBuilder.Both(s => Coral(b, Body, V(0.03f * s, 0.16f, 0.02f), V(0.09f * s, 0.24f, 0.04f), 0.012f, white, V(0.02f * s, 0.03f, 0)));
        Stretch(b, V(1.12f, 1.08f, 1.08f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2865 Paladrake, from Sirfetch'd

    /// <summary>
    /// Paladrake: Sirfetch'd armoured as a paladin, a breastplate, pauldrons and a skirt of pale steel over its white
    /// feathers, a steel helm with a plume of white feathers sweeping back, its leek lance tipped with a point of steel
    /// and bound with steel collars, and its shield of leek leaves become a heater shield of a great green leaf bound in
    /// steel; its heavy brows are kept.
    /// </summary>
    private static PokeBuilder Paladrake()
    {
        var b = Become(Sirfetchd(), "Paladrake", 0.88f);
        var white = Rgb(244, 244, 240);
        var green = Rgb(118, 176, 74);
        var dark = Rgb(62, 112, 62);
        var steel = Rgb(188, 196, 210);
        var shade = Rgb(150, 160, 178);
        int head = BoneOf(b, "head");
        int shieldArm = BoneOf(b, "armL");
        int lanceArm = BoneOf(b, "armR");
        b.Sdf.RemoveAll(s => s.Bone == shieldArm && (Is(s.Color, green) || Is(s.Color, dark)));
        var bc = V(0, 0.25f, 0);
        var c = bc + V(0, 0.16f, 0.03f);
        // The breastplate, the pauldrons and a skirt of plates
        b.Ell(Body, bc + V(0, 0.02f, 0.02f), V(0.09f, 0.085f, 0.085f), steel, mat: Metal, blend: 0.006f);
        b.PaintEll(Body, bc + V(0, 0.0f, 0.1f), V(0.004f, 0.07f, 0.02f), shade, soft: 0.003f);
        b.Torus(Body, bc + V(0, -0.04f, 0), 0.082f, 0.012f, steel, V(0, 0, 0), 1f, 1f, Metal, 0.004f);
        b.Torus(Body, bc + V(0, -0.065f, 0), 0.074f, 0.01f, shade, V(0, 0, 0), 1f, 1f, Metal, 0.004f);
        PokeBuilder.Both(s => b.Ell(s < 0 ? shieldArm : lanceArm, bc + V(0.08f * s, 0.06f, 0.02f), V(0.04f, 0.03f, 0.04f), steel, V(0, 0, -25f * s), Metal, 0.006f));
        // The helm over its head, open at the face, and its plume
        b.Ell(head, c + V(0, 0.02f, -0.01f), V(0.066f, 0.058f, 0.064f), steel, mat: Metal, blend: 0.004f);
        b.Cut(head, c + V(0, -0.005f, 0.065f), V(0.045f, 0.03f, 0.04f));
        b.Ell(head, c + V(0, 0.0f, 0.0f), V(0.058f, 0.05f, 0.058f), white, blend: 0.002f);
        b.Limb(head, c + V(0, 0.08f, -0.02f), c + V(0, 0.06f, 0.06f), 0.006f, 0.006f, shade, Metal, 0.003f);
        foreach (var (up, back, x) in new[] { (0.12f, 0.06f, 0f), (0.1f, 0.1f, -0.015f), (0.1f, 0.1f, 0.015f), (0.06f, 0.15f, 0f) })
            Frond(b, head, c + V(x, 0.06f, -0.02f), c + V(x * 2f, 0.06f + up, -0.02f - back), 0.028f, white, V(1f, 0, 0), 0.3f);
        // The lance: a steel point and collars on the leek
        var grip = bc + V(0.12f, -0.01f, 0.07f);
        var top = grip + V(0.01f, 0.56f, 0.01f);
        b.Spike(lanceArm, top - V(0, 0.01f, 0), top + V(0.003f, 0.16f, 0.003f), 0.024f, steel, 0.8f, Metal);
        b.Torus(lanceArm, top - V(0, 0.01f, 0), 0.022f, 0.007f, shade, mat: Metal, blend: 0.003f);
        b.Torus(lanceArm, grip + V(0.003f, 0.2f, 0.003f), 0.025f, 0.007f, shade, mat: Metal, blend: 0.003f);
        // The heater shield: a great green leaf in a rim of steel
        var boss = bc + V(-0.13f, 0.0f, 0.08f);
        var face = Vector3.Normalize(V(-0.45f, 0, 1f));
        var sc = boss + face * 0.025f;
        var rot = Euler(face);
        b.Limb(shieldArm, boss, sc, 0.022f, 0.02f, Rgb(150, 160, 178), Metal, 0.006f);
        b.Ell(shieldArm, sc - face * 0.004f, V(0.1f, 0.012f, 0.13f), shade, rot, Metal, 0.004f);
        b.Ell(shieldArm, sc + face * 0.006f, V(0.088f, 0.01f, 0.118f), green, rot, Leaf, 0.003f);

        Stretch(b, V(1.05f, 1.1f, 1.05f));
        return b;
    }

    // ------------------------------------------------------------------ 2867 Runedolmen, from Runerigus

    /// <summary>
    /// Runedolmen: Runerigus's slab become a dolmen, two grey stones leaning together under a capstone, every stone cut
    /// with glowing red runes, and the shadow that is its body crouched in the gap between them, its yellow eyes
    /// burning and its black arms reaching out with clawed hands, its smoke pooled on the ground. Built anew, its slab
    /// become three stones, from Runerigus's colours and parts.
    /// </summary>
    private static PokeBuilder Runedolmen()
    {
        var b = new PokeBuilder("Runedolmen", 0.95f, BodyPlan.Floating, V(0, 0.42f, 0)) { Coat = Shell };
        var stone = Rgb(130, 128, 124);
        var light = Rgb(156, 154, 150);
        var dark = Rgb(40, 36, 44);
        var red = Rgb(214, 52, 60);
        // The two standing stones, leaning in, and the capstone over them
        foreach (var (x, lean) in new[] { (-0.17f, -8f), (0.17f, 8f) })
        {
            b.Box(Body, V(x, 0.3f, 0.0f), V(0.065f, 0.3f, 0.06f), 0.018f, stone, V(0, 0, lean), blend: 0.006f);
            for (int i = 0; i < 5; i++)
                PaintAs(b, Body, V(x - 0.01f * MathF.Sign(x) * i * 0.4f, 0.12f + i * 0.09f, 0.062f), V(0.022f, 0.006f, 0.01f), red, Glow, V(0, 0, lean + (i % 2 == 0 ? 25f : -30f)));
            PaintAs(b, Body, V(x, 0.33f, 0.062f), V(0.005f, 0.17f, 0.01f), red, Glow, V(0, 0, lean));
        }
        var cap = V(0, 0.64f, 0);
        b.Box(Body, cap, V(0.27f, 0.05f, 0.12f), 0.022f, light, V(0, 0, -3f), blend: 0.008f);
        for (int i = -3; i <= 3; i++)
            PaintAs(b, Body, cap + V(i * 0.07f, 0.0f, 0.12f), V(0.018f, 0.006f, 0.01f), red, Glow, V(0, 0, i % 2 == 0 ? 60f : -20f));
        // The shadow in the gap, its eyes and its arms
        var sc = V(0, 0.36f, -0.01f);
        b.Ell(Body, sc, V(0.09f, 0.2f, 0.06f), dark, blend: 0.03f);
        int head = b.Head(sc + V(0, 0.12f, 0.02f));
        var c = sc + V(0, 0.16f, 0.03f);
        var r = V(0.07f, 0.06f, 0.05f);
        b.Ell(head, c, r, dark);
        PokeBuilder.Both(s =>
        {
            var at = Out(c, r, default, V(0.4f * s, 0.1f, 0.9f));
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(250, 220, 70), glare: true);
            int arm = b.Arm(s, sc + V(0.06f * s, 0.06f, 0.03f));
            var from = sc + V(0.06f * s, 0.06f, 0.03f);
            var to = sc + V(0.24f * s, -0.04f, 0.12f);
            b.Tube(arm, Smooth(2, from, Vector3.Lerp(from, to, 0.5f) + V(0, 0.05f, 0.02f), to), 0.022f, 0.018f, dark, blend: 0f);
            b.Ell(arm, to, V(0.03f, 0.03f, 0.025f), dark);
            for (int i = 0; i < 4; i++)
                b.Limb(arm, to, to + V((0.03f + 0.01f * i) * s, -0.05f + 0.012f * i, 0.03f - 0.015f * i), 0.01f, 0.006f, dark, blend: 0.004f);
        });
        // Its smoke pooled at its foot
        foreach (var (o, k) in new[] { (V(0, 0.03f, 0.02f), 0.12f), (V(0.12f, 0.02f, 0.06f), 0.07f), (V(-0.12f, 0.02f, 0.07f), 0.08f), (V(0.02f, 0.02f, 0.14f), 0.06f) })
            b.Ell(Body, o, V(k, 0.025f, k * 0.8f), dark, blend: 0.03f);
        b.Limb(Body, V(0, 0.04f, 0), sc - V(0, 0.15f, 0), 0.06f, 0.07f, dark, blend: 0.03f);
        return b;
    }

    // ------------------------------------------------------------------ 2869 Patisserene, from Alcremie

    /// <summary>
    /// Patisserene: Alcremie grown into a cake of three tiers, the lowest a skirt of swagged cream, the middle its body
    /// with its two small arms of cream, the top its head with its face, a crown of cream swirls round a pink heart and
    /// red berries, piped pink hearts and berries round every tier. Built anew, its body become a cake, from
    /// Alcremie's cream and parts.
    /// </summary>
    private static PokeBuilder Patisserene()
    {
        var b = new PokeBuilder("Patisserene", 0.75f, BodyPlan.Biped, V(0, 0.22f, 0)) { Coat = Fur };
        var cream = Rgb(250, 242, 228);
        var shade = Rgb(232, 222, 206);
        var pink = Rgb(240, 150, 176);
        var berry = Rgb(196, 44, 64);
        // The lowest tier: a skirt of swagged cream
        b.Ell(Body, V(0, 0.07f, 0), V(0.18f, 0.07f, 0.17f), cream);
        for (int i = 0; i < 10; i++)
        {
            float a = i * MathF.Tau / 10f;
            b.Ell(Body, V(MathF.Sin(a) * 0.15f, 0.08f, MathF.Cos(a) * 0.14f), V(0.05f, 0.05f, 0.04f), i % 2 == 0 ? cream : shade, V(0, a / Degree, 0), blend: 0.015f);
            b.Ell(Body, V(MathF.Sin(a) * 0.18f, 0.02f, MathF.Cos(a) * 0.17f), V(0.03f, 0.022f, 0.03f), cream, blend: 0.01f);
        }
        // The middle tier, its body
        b.Limb(Body, V(0, 0.13f, 0), V(0, 0.24f, 0), 0.12f, 0.115f, cream, blend: 0.01f);
        b.Torus(Body, V(0, 0.135f, 0), 0.12f, 0.016f, cream, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.1f * s, 0.21f, 0.02f));
            b.Limb(arm, V(0.1f * s, 0.21f, 0.02f), V(0.16f * s, 0.24f, 0.05f), 0.024f, 0.02f, cream);
            b.Ell(arm, V(0.165f * s, 0.25f, 0.05f), V(0.025f, 0.022f, 0.022f), cream);
        });
        int head = b.Head(V(0, 0.26f, 0));
        // The top tier, its head, and the crown of swirls
        var c = V(0, 0.32f, 0.0f);
        b.Limb(head, V(0, 0.26f, 0), V(0, 0.37f, 0), 0.09f, 0.085f, cream, blend: 0.01f);
        b.Torus(head, V(0, 0.27f, 0), 0.095f, 0.016f, pink, blend: 0.006f);
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f;
            var at = V(MathF.Sin(a) * 0.065f, 0.4f, MathF.Cos(a) * 0.065f);
            b.Ell(head, at, V(0.028f, 0.022f, 0.028f), cream, blend: 0.01f);
            b.Spike(head, at, at + V(MathF.Sin(a) * 0.012f, 0.03f, MathF.Cos(a) * 0.012f), 0.016f, cream, blend: 0.008f);
        }
        AlcremieSweet(b, head, V(0, 0.44f, 0.01f), V(0, 0.3f, 1f), "Love");
        foreach (var o in new[] { V(0.02f, 0.445f, -0.03f), V(-0.025f, 0.445f, -0.02f), V(0.0f, 0.455f, -0.035f) })
            Bead(b, head, o, 0.012f, berry);
        var r = V(0.088f, 0.06f, 0.088f);
        PokeBuilder.Both(s =>
        {
            var at = Out(c, r, default, V(0.32f * s, 0.05f, 0.95f));
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(200, 52, 64));
            b.PaintEll(head, Out(c, r, default, V(0.55f * s, -0.25f, 0.8f)), V(0.016f, 0.01f, 0.012f), Rgb(250, 190, 200), soft: 0.006f);
        });
        b.Mark(head, Out(c, r, default, V(0, -0.35f, 1f)), V(0, -0.2f, 1f), 0.012f, 0.007f, Rgb(160, 70, 80), MarkShape.Smile);
        // Piped hearts and berries round the tiers
        for (int i = 0; i < 8; i++)
        {
            float a = (i + 0.5f) * MathF.Tau / 8f;
            var n = V(MathF.Sin(a), 0, MathF.Cos(a));
            HeartPaint(b, Body, V(0, 0.19f, 0) + n * 0.12f, n, 0.035f, pink);
            Bead(b, Body, V(0, 0.135f, 0) + n * 0.135f, 0.01f, berry);
            HeartPaint(b, Body, V(0, 0.04f, 0) + n * 0.185f, n, 0.035f, pink);
        }
        return b;
    }

    // ------------------------------------------------------------------ 2870 Phalanxion, from Falinks

    /// <summary>
    /// Phalanxion: Falinks's squad grown into a phalanx of seven brass troopers in two ranks behind their leader,
    /// each holding a round brass shield studded with rivets on its outer side, and the leader at the front in a
    /// tall brass helmet with cheek-guards and a red crest. Built on Falinks's troopers.
    /// </summary>
    private static PokeBuilder Phalanxion()
    {
        var b = new PokeBuilder("Phalanxion", 0.9f, BodyPlan.Serpent, V(0, 0.1f, 0.2f)) { Coat = Metal };
        var brass = Rgb(176, 148, 64);
        var rivet = Rgb(130, 110, 60);
        var red = Rgb(186, 40, 40);
        var places = new[]
        {
            (V(0, 0.1f, 0.16f), 0.08f),
            (V(-0.125f, 0.075f, 0.14f), 0.055f), (V(0.125f, 0.075f, 0.14f), 0.055f),
            (V(-0.065f, 0.075f, 0.05f), 0.055f), (V(0.065f, 0.075f, 0.05f), 0.055f), (V(0.0f, 0.075f, -0.03f), 0.055f), (V(-0.13f, 0.075f, 0.04f), 0.055f),
        };
        for (int i = 0; i < places.Length; i++)
        {
            var (c, r) = places[i];
            int bone = i == 0 ? Body : b.Part("trooper" + i, Body, c, PokeRole.Segment, i * 0.5f);
            Trooper(b, bone, c, r, i == 0);
            // A helmet of brass over each
            b.Ell(bone, c + V(0, r * 0.55f, 0), V(r * 1.02f, r * 0.5f, r * 1.07f), brass, mat: Metal, blend: 0.004f);
            b.Torus(bone, c + V(0, r * 0.3f, 0), r * 1.0f, r * 0.08f, brass, mat: Metal, blend: 0.003f);
            // Its round shield on its outer side
            float side = c.X == 0 ? (i == 0 ? 1f : -1f) : MathF.Sign(c.X);
            var face = Vector3.Normalize(V(side, 0, 0.35f));
            var sc = c + face * r * 1.15f + V(0, -r * 0.1f, 0);
            Plate(b, bone, sc, face, V(r * 0.75f, r * 0.12f, r * 0.75f), brass, Metal, 0.004f);
            b.Ell(bone, sc + face * r * 0.1f, V(r * 0.2f, r * 0.2f, r * 0.2f), rivet, mat: Metal, blend: 0.003f);
            for (int k = 0; k < 6; k++)
            {
                float a = k * MathF.Tau / 6f;
                var u = Vector3.Normalize(Vector3.Cross(face, V(0, 1f, 0)));
                var at = sc + (u * MathF.Cos(a) + V(0, MathF.Sin(a), 0)) * r * 0.6f + face * r * 0.08f;
                b.Ell(bone, at, V(r * 0.07f, r * 0.07f, r * 0.07f), rivet, mat: Metal, blend: 0.002f);
            }
        }
        // The ranks behind stand with their faces in the backs of the ranks before them: their eyes don't show
        b.Model.Decals.RemoveAll(d => d.IsEye && d.Center.Z < 0.12f);
        // The leader's tall helmet, its cheek-guards and crest
        var lc = places[0].Item1;
        float lr = places[0].Item2;
        b.Ell(Body, lc + V(0, lr * 0.75f, -0.005f), V(lr * 1.1f, lr * 0.8f, lr * 1.12f), brass, mat: Metal, blend: 0.004f);
        PokeBuilder.Both(s => b.Ell(Body, lc + V(lr * 0.75f * s, 0.0f, lr * 0.5f), V(lr * 0.3f, lr * 0.55f, lr * 0.45f), brass, mat: Metal, blend: 0.004f));
        for (int k = 0; k < 7; k++)
        {
            float t = k / 6f;
            var root = lc + V(0, lr * 1.45f, lr * (0.6f - 1.4f * t));
            b.Spike(Body, root, root + V(0, lr * (1.3f - 0.3f * t), -lr * 0.35f * t), lr * 0.22f, red, 0.35f, Fur, 0.006f);
        }
        return b;
    }

    // ------------------------------------------------------------------ 2871 Voltechinus, from Pincurchin

    /// <summary>
    /// Voltechinus: Pincurchin rounder, its spines long and black banded with yellow, each tipped with a spark of
    /// glowing yellow, a ring of shorter spines round its round mouth of teeth, and a bed of sea foam round its base.
    /// </summary>
    private static PokeBuilder Voltechinus()
    {
        var b = Become(Pincurchin(), "Voltechinus", 0.9f);
        var purple = Rgb(84, 62, 120);
        var black = Rgb(36, 34, 40);
        var yellow = Rgb(244, 204, 50);
        var foam = Rgb(228, 238, 246);
        b.Sdf.RemoveAll(s => s.Shape != SdfShape.Ellipsoid || s.Op == SdfOp.Paint);
        Recolour(b, (Rgb(72, 58, 92), purple));
        var c = V(0, 0.1f, 0);
        var r = V(0.12f, 0.09f, 0.12f);
        Grow(b, V(1.1f, 1.25f, 1.1f), c, (SdfPrimitive _) => true, _ => true);
        r = V(0.132f, 0.112f, 0.132f);
        c = V(0, 0.12f, 0);
        foreach (var s in b.Sdf.Shapes) { s.A += V(0, 0.02f, 0); s.B += V(0, 0.02f, 0); }
        b.Sdf.Changed();
        foreach (var d in b.Model.Decals) d.Center += V(0, 0.02f, 0);
        float golden = MathF.PI * (3f - MathF.Sqrt(5f));
        for (int i = 0; i < 30; i++)
        {
            float y = 1f - 1.3f * (i + 0.5f) / 30f, ring = MathF.Sqrt(1f - y * y), a = i * golden;
            var dir = V(MathF.Sin(a) * ring, y, MathF.Cos(a) * ring);
            if (dir.Z > 0.55f && dir.Y < 0.45f) continue;
            var root = Out(c, r, default, dir) - dir * 0.02f;
            var end = root + dir * (0.17f + 0.06f * (i % 3) / 2f);
            b.Spike(Body, root, end, 0.016f, black, mat: Shell, blend: 0.008f);
            foreach (float t in new[] { 0.35f, 0.62f })
                b.PaintEll(Body, Vector3.Lerp(root, end, t), V(0.02f, 0.012f, 0.02f), yellow, Euler(dir), 0.003f);
            b.Ell(Body, end, V(0.008f, 0.008f, 0.008f), yellow, mat: Glow, blend: 0.003f);
        }
        // The ring of short spines round its mouth
        var mouth = Out(c, r, default, V(0, -0.15f, 1f));
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.Tau / 12f;
            var d = Vector3.Normalize(V(MathF.Cos(a) * 0.5f, MathF.Sin(a) * 0.5f, 1f));
            var root = mouth + V(MathF.Cos(a), MathF.Sin(a), 0) * 0.045f - V(0, 0, 0.01f);
            b.Spike(Body, root, root + d * 0.05f, 0.008f, black, mat: Shell, blend: 0.004f);
        }
        b.PaintEll(Body, mouth, V(0.03f, 0.03f, 0.02f), Rgb(80, 30, 40));
        // Sea foam round its base
        for (int i = 0; i < 14; i++)
        {
            float a = i * MathF.Tau / 14f;
            b.Ell(Body, V(MathF.Sin(a) * 0.13f, 0.025f, MathF.Cos(a) * 0.13f), V(0.04f, 0.028f, 0.04f), foam, blend: 0.012f);
        }
        return b;
    }

    // ------------------------------------------------------------------ 2873 Auroramoth, from Frosmoth

    /// <summary>
    /// Auroramoth: Frosmoth with wider wings of pale ice, both pairs crossed by glowing bands of aurora, green above
    /// violet, its antennae feathered with frost, its fur longer, and dark eyes.
    /// </summary>
    private static PokeBuilder Auroramoth()
    {
        var b = Become(Frosmoth(), "Auroramoth", 0.95f);
        Unlift(b, V(0, 0.4f, 0));
        var white = Rgb(246, 248, 252);
        var ice = Rgb(206, 230, 246);
        var green = Rgb(110, 240, 160);
        var violet = Rgb(150, 110, 220);
        var bc = V(0, 0.4f, 0);
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            var root = bc + V(0.04f * s, 0.03f, 0.01f);
            Tint(b, sh => sh.Bone == wing && Is(sh.Color, white), ice);
            Grow(b, V(1.25f, 1.15f, 1.1f), root, i => i == wing);
            foreach (var (tip, w) in new[] { (root + V(0.45f * s, 0.18f, -0.02f), 0.2f), (root + V(0.32f * s, -0.14f, -0.07f), 0.13f) })
            {
                var d = tip - root;
                var mid = root + d * 0.55f;
                PaintAs(b, wing, mid + V(0, w * 0.18f, 0), V(0.016f, d.Length() * 0.42f, 0.03f), green, Glow, Euler(d));
                PaintAs(b, wing, mid + V(0, -w * 0.05f, 0), V(0.012f, d.Length() * 0.38f, 0.03f), violet, Glow, Euler(d));
            }
        });
        // Frost on the antennae, and longer fur
        int head = BoneOf(b, "head");
        var c = bc + V(0, 0.07f, 0.1f);
        PokeBuilder.Both(s =>
        {
            for (int i = 1; i <= 4; i++)
            {
                var at = Vector3.Lerp(c + V(0.02f * s, 0.035f, -0.01f), c + V(0.1f * s, 0.12f, -0.04f), i / 5f);
                b.Spike(head, at, at + V(0.03f * s, -0.005f, 0.02f), 0.005f, ice, mat: Glow, blend: 0.002f);
                b.Spike(head, at, at + V(-0.02f * s, 0.02f, 0.02f), 0.005f, ice, mat: Glow, blend: 0.002f);
            }
        });
        FurTufts(b, Body, bc + V(0, -0.01f, 0.0f), V(0.07f, 0.07f, 0.1f), 22, 0.04f, 0.016f, white, 0.3f, -0.95f, 0.6f);
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
            d.Iris = Rgb(60, 40, 110);
        Stretch(b, V(1.05f, 1.05f, 1.05f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2874 Henjourner, from Stonjourner

    /// <summary>
    /// Henjourner: Stonjourner grown into a henge, three standing stones under one great lintel, its face glowing on
    /// the lintel, lichen yellow-green on its weathered sides and a gold sun disc glowing in the gap between its
    /// stones.
    /// </summary>
    private static PokeBuilder Henjourner()
    {
        var b = Become(Stonjourner(), "Henjourner", 1f);
        var grey = Rgb(150, 150, 156);
        var lichen = Rgb(150, 160, 70);
        var gold = Rgb(246, 210, 110);
        int head = BoneOf(b, "head");
        // The blocks at its ends go; the lintel grows long and heavy
        b.Sdf.RemoveAll(s => s.Bone == head && s.Shape != SdfShape.Ellipsoid);
        var lc = V(0, 0.66f, 0);
        Grow(b, V(1.25f, 1.4f, 1.6f), lc, s => s.Bone == Body && s.Shape == SdfShape.RoundBox, _ => false);
        Recolour(b, (Rgb(132, 98, 72), lichen));
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
        {
            d.Iris = gold;
            d.Center += V(0, 0.02f, 0.045f);
        }
        b.Model.Decals.RemoveAll(d => !d.IsEye);
        PokeBuilder.Both(s => PaintAs(b, Body, lc + V(0.035f * s, 0.05f, 0.12f), V(0.025f, 0.006f, 0.01f), gold, Glow, V(0, 0, -30f * s)));
        // The third stone behind
        b.Box(Body, V(0, 0.31f, -0.1f), V(0.065f, 0.31f, 0.055f), 0.025f, grey, blend: 0.006f);
        // Lichen on the stones
        foreach (var (at, k) in new[] { (V(-0.2f, 0.4f, 0.06f), 1f), (V(0.2f, 0.2f, 0.06f), 0.8f), (V(0.06f, 0.5f, 0.06f), 0.7f), (V(-0.06f, 0.25f, 0.06f), 0.6f), (V(0.25f, 0.68f, 0.1f), 0.8f), (V(-0.24f, 0.7f, 0.0f), 0.7f) })
            b.PaintEll(Math.Abs(at.Y - 0.68f) < 0.05f ? Body : (at.X < 0 ? BoneOf(b, "legL") : BoneOf(b, "legR")), at, V(0.04f, 0.05f, 0.04f) * k, lichen, soft: 0.01f);
        // The sun disc in the gap
        var dc = V(0, 0.36f, 0.0f);
        Plate(b, Body, dc, V(0, 0, 1f), V(0.08f, 0.012f, 0.08f), gold, Glow, 0.004f);
        b.Torus(Body, dc + V(0, 0, 0.008f), 0.07f, 0.006f, Rgb(200, 160, 70), V(90f, 0, 0), mat: Metal, blend: 0.002f);
        b.Limb(Body, dc + V(0, 0.08f, -0.01f), lc - V(0, 0.08f, 0.01f), 0.004f, 0.004f, Rgb(200, 160, 70), Metal, 0.002f);
        Stretch(b, V(1.1f, 1.05f, 1.05f));
        return b;
    }

    // ------------------------------------------------------------------ 2875 Floeguin, from Eiscue

    /// <summary>
    /// Floeguin: Eiscue grown tall and a deep blue, its cube of ice grown into a floe that covers its head and runs on
    /// down its back like a sledge, icicles hanging at its chin, its flippers long, and its dark feeler become a trail of
    /// yellow crest-tufts streaming back from its brow.
    /// </summary>
    private static PokeBuilder Floeguin()
    {
        var b = Become(Eiscue(), "Floeguin", 0.92f);
        var blue = Rgb(58, 90, 140);
        var ice = Rgb(200, 226, 242);
        var yellow = Rgb(240, 200, 70);
        int head = BoneOf(b, "head");
        b.Sdf.RemoveAll(s => s.Bone == head && Is(s.Color, Rgb(42, 46, 64)));
        Recolour(b, (Rgb(42, 46, 64), blue), (Rgb(176, 216, 240), ice));
        Make(b, s => Is(s.Color, ice), Shell);
        var bc = V(0, 0.18f, 0);
        var cc = bc + V(0, 0.22f, 0.02f);
        // The floe over its back, in facets
        b.Box(head, cc + V(0, -0.02f, -0.14f), V(0.095f, 0.07f, 0.1f), 0.025f, ice, V(-20f, 0, 0), Shell, 0.012f);
        b.Box(Body, bc + V(0, 0.05f, -0.1f), V(0.075f, 0.11f, 0.045f), 0.03f, ice, V(-12f, 0, 0), Shell, 0.012f);
        // Icicles at its chin
        foreach (var (x, len) in new[] { (-0.04f, 0.07f), (0.0f, 0.1f), (0.035f, 0.06f) })
            b.Spike(head, cc + V(x, -0.08f, 0.08f), cc + V(x, -0.08f - len, 0.085f), 0.012f, ice, mat: Glow, blend: 0.004f);
        // The yellow tufts streaming back
        PokeBuilder.Both(s =>
        {
            foreach (var (dx, up) in new[] { (0.03f, 0.04f), (0.05f, 0.0f), (0.06f, -0.03f) })
                Frond(b, head, cc + V(dx * s, 0.07f, -0.04f), cc + V((dx + 0.06f) * s, 0.07f + up, -0.25f), 0.016f, yellow, V(0, 1f, 0), 0.3f);
        });
        // Longer flippers
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            Grow(b, V(1.3f, 1.6f, 1.2f), bc + V(0.06f * s, 0.06f, 0), i => i == arm);
        });
        Stretch(b, V(1.05f, 1.3f, 1.05f));
        return b;
    }

    // ------------------------------------------------------------------ 2876 Majordomee, from Indeedee

    /// <summary>
    /// Majordomee: Indeedee grown tall and slim as a butler, its feelers risen into long horns curled at their tips,
    /// a white collar of fur, a tailcoat of purple falling to the ground behind it lined in black, and a gold chain at
    /// its waist; the female's colours, white where his are black, are kept.
    /// </summary>
    /// <param name="female">A female keeps Indeedee's: white below the waist and white lining her coat.</param>
    private static PokeBuilder Majordomee(bool female = false)
    {
        var b = Become(IndeedeeBuild(female), "Majordomee", 0.9f);
        var dark = Rgb(70, 62, 112);
        var white = Rgb(244, 242, 248);
        var black = Rgb(36, 32, 44);
        var gold = Rgb(226, 182, 70);
        var lining = female ? white : black;
        int head = BoneOf(b, "head");
        var bc = V(0, 0.15f, 0);
        var c = bc + V(0, 0.24f, 0.02f);
        // The feelers rise and curl at their tips
        PokeBuilder.Both(s =>
        {
            var root = c + V(0.03f * s, 0.04f, -0.01f);
            var rise = Smooth(2, root, root + V(0.03f * s, 0.08f, -0.02f), root + V(0.08f * s, 0.12f, -0.03f));
            var curl = Spiral(rise[^1] + V(0.03f * s, 0.0f, 0), V(-s, 0, 0), V(0, 1f, 0), 0.03f, 0.008f, 0f, MathF.PI * 1.7f, 10);
            b.Tube(head, rise.Concat(curl).ToArray(), 0.012f, 0.006f, dark, blend: 0.004f);
        });
        // The white collar of fur
        FurTufts(b, Body, bc + V(0, 0.15f, 0.01f), V(0.055f, 0.03f, 0.05f), 14, 0.035f, 0.014f, white, 1.2f, -0.6f, 0.6f);
        // The tailcoat, falling behind it to the ground, lined
        PokeBuilder.Both(s =>
        {
            var top = bc + V(0.05f * s, 0.06f, -0.06f);
            var path = Smooth(3, top, bc + V(0.07f * s, -0.04f, -0.1f), bc + V(0.06f * s, -0.12f, -0.16f), V(0.04f * s, 0.01f, -0.24f));
            b.Tube(Body, path, 0.03f, 0.022f, dark, blend: 0.012f);
            b.PaintEll(Body, Vector3.Lerp(path[0], path[^1], 0.6f) + V(0.02f * s, 0, 0.005f), V(0.012f, 0.08f, 0.04f), lining, Euler(path[^1] - path[0]), 0.006f);
        });
        PokeBuilder.Both(s => b.PaintEll(Body, bc + V(0.04f * s, 0.06f, 0.07f), V(0.012f, 0.06f, 0.02f), lining, V(0, 0, -20f * s), 0.006f));
        // A gold chain at its waist
        b.Tube(Body, Smooth(2, bc + V(-0.07f, 0.03f, 0.06f), bc + V(0, 0.0f, 0.095f), bc + V(0.07f, 0.03f, 0.06f)), 0.005f, 0.005f, gold, Metal, 0.002f);
        PokeBuilder.Both(s => Bead(b, Body, bc + V(0.07f * s, 0.03f, 0.065f), 0.01f, gold, Metal));
        Stretch(b, V(0.95f, 1.28f, 0.95f));
        return b;
    }

    // ------------------------------------------------------------------ 2877 Gorgehog, from Morpeko

    /// <summary>
    /// Gorgehog: Morpeko rounder and quilled like a hedgehog, its fur split down the middle into yellow and dark purple
    /// (its two moods in one body), two great red cheek pouches, its tail a coiled spring striped yellow and purple and
    /// a cord at its waist with the seed pouch kept.
    /// </summary>
    private static PokeBuilder Gorgehog()
    {
        var b = Become(Morpeko(), "Gorgehog", 0.85f);
        var yellow = Rgb(244, 220, 60);
        var purple = Rgb(66, 50, 96);
        var red = Rgb(214, 60, 60);
        var cord = Rgb(170, 150, 110);
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        int armR = BoneOf(b, "armR");
        b.Sdf.RemoveAll(s => s.Bone == tail || (s.Bone == armR && s.Shape != SdfShape.RoundCone) || (s.Bone == armR && Is(s.Color, Rgb(120, 140, 70))));
        b.Model.Decals.RemoveAll(d => !d.IsEye && d.Shape == MarkShape.Disc);
        Recolour(b, (Rgb(242, 216, 92), yellow), (Rgb(152, 112, 72), yellow), (Rgb(84, 82, 90), purple));
        var c = V(0, 0.15f, 0);
        var r = V(0.075f, 0.12f, 0.065f);
        Grow(b, V(1.35f, 1.0f, 1.3f), c, s => s.Bone == Body && s.Op == SdfOp.Add && Vector3.Distance(s.A, c) < 0.001f, _ => false);
        r = V(0.1f, 0.12f, 0.085f);
        // The right half purple, down the middle
        b.PaintEll(Body, c + V(0.1f, 0, 0), V(0.1f, 0.16f, 0.12f), purple, soft: 0.004f);
        foreach (var leg in new[] { BoneOf(b, "legR"), armR })
            Tint(b, s => s.Bone == leg && s.Op == SdfOp.Add, purple);
        // Quills over its back and head, each half its own colour
        for (int i = 0; i < 26; i++)
        {
            float a = -1.4f + 2.8f * (i % 9) / 8f, y = 0.12f + 0.035f * (i / 9);
            var dir = Vector3.Normalize(V(MathF.Sin(a), 0.5f + (i / 9) * 0.3f, -MathF.Cos(a) * 0.9f - 0.2f));
            var root = c + V(dir.X * r.X * 0.8f, y - 0.06f, dir.Z * r.Z * 0.8f);
            b.Spike(Body, root, root + dir * 0.09f, 0.025f, root.X > 0.005f ? purple : yellow, 0.5f, blend: 0.01f);
        }
        // Hands out past the rounder body
        PokeBuilder.Both(s => b.Ell(BoneOf(b, s < 0 ? "armL" : "armR"), c + V(0.115f * s, -0.01f, 0.06f), V(0.028f, 0.026f, 0.026f), s < 0 ? yellow : purple));
        // Great red cheek pouches
        PokeBuilder.Both(s => b.Ell(Body, Out(c, r, default, V(0.85f * s, 0.25f, 0.55f)), V(0.04f, 0.035f, 0.03f), red, mat: Shell, blend: 0.012f));
        // The cord at its waist and the seed pouch
        b.Torus(Body, c + V(0, -0.04f, 0), 0.098f, 0.006f, cord, V(0, 0, 0), 1f, 0.87f, Fur, 0.003f);
        b.Ell(Body, c + V(0.06f, -0.06f, 0.07f), V(0.028f, 0.025f, 0.015f), cord, mat: Fur, blend: 0.006f);
        // The spring of a tail
        var coil = Spiral(c + V(0, -0.06f, -0.16f), V(1f, 0, 0), V(0, 1f, 0), 0.03f, 0.03f, 0f, MathF.Tau * 3f, 36);
        for (int i = 0; i < coil.Length; i++) coil[i] += V(0, 0, -0.012f * i / 3f);
        b.Tube(tail, new[] { c + V(0, -0.07f, -0.07f) }.Concat(coil).ToArray(), 0.008f, 0.007f, yellow, blend: 0f);
        for (int i = 6; i < coil.Length; i += 6)
            b.PaintEll(tail, coil[i], V(0.012f, 0.012f, 0.012f), purple, soft: 0.003f);
        return b;
    }

    // ------------------------------------------------------------------ 2879 Verdigrand, from Copperajah

    /// <summary>
    /// Verdigrand: Copperajah larger and older, its copper gone to verdigris green worn orange at the edges and studded
    /// with rivets, its tusks pale stone, its trunk long and banded down to a broad shovel of copper on the ground, and a
    /// howdah-like ridge of crenellated stone on its back.
    /// </summary>
    private static PokeBuilder Verdigrand()
    {
        var b = Become(Copperajah(), "Verdigrand", 1f);
        var verdigris = Rgb(70, 150, 128);
        var dark = Rgb(48, 108, 92);
        var copper = Rgb(204, 112, 60);
        var stone = Rgb(204, 194, 164);
        var sand = Rgb(184, 166, 120);
        int head = BoneOf(b, "head");
        b.Sdf.RemoveAll(s => s.Bone == head && (s.Shape == SdfShape.RoundCone && Is(s.Color, Rgb(52, 148, 128)) && s.A.Y < 0.5f || (s.Op == SdfOp.Paint && Is(s.Color, Rgb(226, 124, 52)) && s.A.Y < 0.3f)));
        Recolour(b, (Rgb(52, 148, 128), verdigris), (Rgb(34, 92, 80), dark), (Rgb(226, 124, 52), copper), (Rgb(240, 240, 234), stone));
        var bc = V(0, 0.42f, -0.02f);
        var c = bc + V(0, 0.1f, 0.33f);
        // Rivets and worn edges over its body and legs
        var br = V(0.2f, 0.18f, 0.28f);
        foreach (var d in new[] { V(1f, 0.2f, 0.5f), V(-1f, 0.2f, 0.5f), V(1f, 0.1f, -0.5f), V(-1f, 0.1f, -0.5f), V(1f, -0.4f, 0f), V(-1f, -0.4f, 0f) })
            for (int k = -1; k <= 1; k++)
                Bead(b, Body, Out(bc, br, default, d + V(0, k * 0.25f, 0)), 0.009f, dark, Metal, 0.002f);
        PokeBuilder.Both(s => b.PaintEll(Body, bc + V(0.19f * s, -0.12f, 0.0f), V(0.04f, 0.03f, 0.22f), copper, soft: 0.01f));
        // The long banded trunk down to a shovel of copper
        var trunk = Smooth(3, c + V(0, -0.03f, 0.09f), c + V(0, -0.16f, 0.18f), c + V(0, -0.32f, 0.24f), c + V(0, -0.42f, 0.3f));
        b.Tube(head, trunk, 0.075f, 0.055f, verdigris, blend: 0f);
        for (int i = 2; i < trunk.Length - 1; i += 2)
            b.PaintTorus(head, trunk[i], 0.07f - 0.002f * i, 0.008f, dark, Euler(trunk[i + 1] - trunk[i - 1]));
        var sh = trunk[^1] + V(0, -0.03f, 0.07f);
        b.Box(head, sh, V(0.12f, 0.06f, 0.08f), 0.03f, copper, V(15f, 0, 0), Metal, 0.01f);
        b.CutBox(head, sh + V(0, 0.04f, 0.04f), V(0.1f, 0.05f, 0.07f), Quaternion.CreateFromYawPitchRoll(0, 15f * Degree, 0));
        foreach (float x in new[] { -0.06f, 0f, 0.06f })
            b.PaintEll(head, sh + V(x, 0.0f, -0.06f), V(0.006f, 0.06f, 0.03f), Rgb(160, 84, 46), soft: 0.004f);
        // The howdah of stone on its back, crenellated
        var hd = bc + V(0, 0.18f, -0.04f);
        b.Ell(Body, hd, V(0.19f, 0.05f, 0.24f), sand, mat: Shell, blend: 0.02f);
        b.Box(Body, hd + V(0, 0.06f, 0), V(0.1f, 0.05f, 0.16f), 0.01f, dark, mat: Metal, blend: 0.01f);
        for (int i = -2; i <= 2; i++)
            PokeBuilder.Both(s => b.Box(Body, hd + V(0.08f * s, 0.13f, i * 0.06f), V(0.02f, 0.025f, 0.018f), 0.005f, stone, mat: Shell, blend: 0.004f));
        Stretch(b, V(1.06f, 1.06f, 1.06f));
        return b;
    }

    // ------------------------------------------------------------------ 2880 Fulgurex, from Dracozolt

    /// <summary>
    /// Fulgurex: Dracozolt's two halves grown together, a long neck rising from a ruff of orange feathers over the
    /// drake's shoulders to its crested bird's head, its arms longer and clawed, its drake half olive green, and the
    /// spikes at its tail's end crystals of glowing yellow sparking.
    /// </summary>
    private static PokeBuilder Fulgurex()
    {
        var b = Become(Dracozolt(), "Fulgurex", 0.95f);
        var green = Rgb(110, 156, 64);
        var orange = Rgb(240, 130, 44);
        var spike = Rgb(214, 220, 80);
        var glow = Rgb(246, 240, 110);
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        Recolour(b, (FossilGreen, green), (FossilRed, spike));
        var bc = V(0, 0.34f, -0.06f);
        // The head rises on a long neck
        Move(b, i => i == head, V(0, 0.14f, 0.02f));
        b.Tube(head, Smooth(2, bc + V(0, 0.14f, 0.13f), bc + V(0, 0.28f, 0.17f), bc + V(0, 0.4f, 0.2f)), 0.04f, 0.034f, FossilYellow, blend: 0.01f);
        var c = bc + V(0, 0.43f, 0.22f);
        FurTufts(b, head, c + V(0, 0.0f, -0.03f), V(0.04f, 0.035f, 0.04f), 10, 0.035f, 0.012f, orange, -0.2f, -0.6f, 0.4f);
        // The ruff of orange feathers over its shoulders
        var ruff = bc + V(0, 0.14f, 0.1f);
        b.Ell(Body, ruff, V(0.12f, 0.08f, 0.1f), orange, blend: 0.03f);
        FurTufts(b, Body, ruff, V(0.12f, 0.08f, 0.1f), 28, 0.06f, 0.02f, orange, 0.9f, -0.9f, 0.7f);
        // Longer arms
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            Grow(b, V(1.4f, 1.6f, 1.3f), bc + V(0.05f * s, 0.14f, 0.17f), i => i == arm);
        });
        // The tail's end: crystals of glowing yellow
        var tp = Smooth(3, bc + V(0, 0, -0.18f), bc + V(0, -0.03f, -0.34f), bc + V(0, -0.07f, -0.5f), bc + V(0, -0.07f, -0.63f));
        for (int i = tp.Length - 4; i < tp.Length; i++)
            foreach (var d in new[] { V(0.04f, 0.1f, -0.02f), V(-0.04f, 0.09f, -0.02f), V(0, 0.12f, -0.04f) })
                b.Spike(tail, tp[i], tp[i] + d * (0.7f + 0.3f * (i - tp.Length + 4) / 3f), 0.016f, glow, 0.6f, Glow, 0.004f);
        Stretch(b, V(1.05f, 1.1f, 1.05f));
        return b;
    }

    // ------------------------------------------------------------------ 2881 Glacivolt, from Arctozolt

    /// <summary>
    /// Glacivolt: Arctozolt's two halves grown together, its ball of ice drawn out into a pale blue beast on two legs
    /// with a tail rising behind, the bird's yellow top grown larger and shaggy with long feathered arms that reach the
    /// ground and end in claws of ice, and its cap of snow grown into a mantle of ice crystals over its shoulders.
    /// </summary>
    private static PokeBuilder Glacivolt()
    {
        var b = Become(Arctozolt(), "Glacivolt", 0.98f);
        var ice = Rgb(196, 226, 246);
        var crystal = Rgb(220, 238, 250);
        var yellow = FossilYellow;
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => Is(s.Color, Rgb(244, 248, 252)) && s.Bone == Body);
        b.Sdf.RemoveAll(s => s.Bone == tail || s.Bone == BoneOf(b, "armL") || s.Bone == BoneOf(b, "armR"));
        Recolour(b, (Rgb(56, 112, 196), ice));
        var bc = V(0, 0.33f, 0);
        // The yellow top, shaggy
        var top = bc + V(0, 0.12f, 0.06f);
        b.Ell(Body, top, V(0.13f, 0.12f, 0.1f), yellow, blend: 0.03f);
        FurTufts(b, Body, top, V(0.13f, 0.12f, 0.1f), 24, 0.05f, 0.018f, yellow, 2f, -0.95f, 0.6f);
        // Long feathered arms to the ground, claws of ice
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var sh = bc + V(0.12f * s, 0.15f, 0.08f);
            var elbow = bc + V(0.19f * s, 0.0f, 0.14f);
            var hand = V(0.2f * s, 0.05f, 0.22f);
            b.Limb(arm, sh, elbow, 0.045f, 0.042f, yellow);
            b.Limb(arm, elbow, hand + V(0, 0.03f, 0), 0.042f, 0.035f, yellow);
            foreach (float k in new[] { 0.3f, 0.5f, 0.7f })
            {
                var at = k < 0.5f ? Vector3.Lerp(sh, elbow, k * 2f) : Vector3.Lerp(elbow, hand, k * 2f - 1f);
                b.Spike(arm, at, at + V(0.035f * s, -0.035f, -0.02f), 0.02f, yellow, 0.6f, blend: 0.008f);
            }
            b.Ell(arm, hand, V(0.03f, 0.025f, 0.03f), ice, mat: Shell);
            Claws(b, arm, hand + V(0, -0.01f, 0.025f), V(0.015f, 0, 0), V(0, -0.5f, 1f), 0.04f, 0.009f);
        });
        // The mantle of ice crystals over its shoulders and back
        for (int i = 0; i < 18; i++)
        {
            float a = -1.6f + 3.2f * (i % 9) / 8f;
            var dir = Vector3.Normalize(V(MathF.Sin(a), 0.9f - 0.3f * (i / 9), -MathF.Cos(a) * 0.6f - 0.3f));
            var root = Out(top, V(0.13f, 0.12f, 0.1f), default, V(MathF.Sin(a), 0.6f - 0.4f * (i / 9), -MathF.Cos(a) * 0.6f - 0.3f)) - dir * 0.015f;
            b.Spike(Body, root, root + dir * (0.09f + 0.03f * (i % 3)), 0.022f, crystal, 0.6f, Glow, 0.006f);
        }
        // A tail rising behind, ice-ridged
        var tp = Smooth(3, bc + V(0, -0.08f, -0.13f), bc + V(0, -0.12f, -0.26f), bc + V(0, -0.02f, -0.36f), bc + V(0, 0.14f, -0.38f));
        b.Tube(tail, tp, 0.06f, 0.02f, ice, blend: 0f);
        for (int i = 2; i < tp.Length - 1; i += 2)
            b.Spike(tail, tp[i], tp[i] + V(0, 0.04f, -0.04f), 0.014f, crystal, 0.6f, Glow, 0.004f);
        Stretch(b, V(1.05f, 1.1f, 1.08f));
        return b;
    }

    // ------------------------------------------------------------------ 2882 Abyssaurus, from Dracovish

    /// <summary>
    /// Abyssaurus: Dracovish's halves grown together, its fish's head blue-green on a thick neck lined with gills, its
    /// jaws gaping, its drake's body a deep olive with a brown belly and a row of studs down its back, and the drake's
    /// tail grown long and finned along both edges in blue-green.
    /// </summary>
    private static PokeBuilder Abyssaurus()
    {
        var b = Become(Dracovish(), "Abyssaurus", 0.95f);
        var teal = Rgb(88, 150, 150);
        var deep = Rgb(58, 108, 116);
        var olive = Rgb(78, 104, 58);
        var brown = Rgb(116, 100, 72);
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        Recolour(b, (Rgb(172, 202, 230), teal), (FossilRed, deep), (Rgb(64, 92, 140), deep), (FossilGreen, olive), (Rgb(214, 96, 110), Rgb(150, 70, 80)));
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == Body && s.Op == SdfOp.Paint && Is(s.Color, deep)))
            s.Color = brown;
        foreach (var s in b.Sdf.Shapes.Where(s => (s.Bone == Body || s.Bone == tail) && s.Op == SdfOp.Add && Is(s.Color, deep)))
            s.Color = PixelCanvas.Mix(olive, Black, 0.2f);
        b.Sdf.Changed();
        var bc = V(0, 0.36f, -0.04f);
        // The thick neck, gills along its throat
        b.Limb(head, bc + V(0, 0.08f, 0.04f), bc + V(0, 0.22f, 0.06f), 0.11f, 0.09f, teal, blend: 0.03f);
        b.PaintEll(Body, bc + V(0, -0.04f, 0.1f), V(0.09f, 0.1f, 0.06f), brown);
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < 4; i++)
                b.PaintEll(head, bc + V(0.085f * s, 0.12f + i * 0.03f, 0.1f), V(0.03f, 0.004f, 0.03f), deep, V(0, 0, -15f * s), 0.003f);
            Frond(b, head, bc + V(0.09f * s, 0.2f, 0.04f), bc + V(0.17f * s, 0.15f, -0.04f), 0.04f, teal, V(0, 1f, 0), 0.2f);
        });
        // A long finned tail
        var tp = Smooth(3, bc + V(0, -0.04f, -0.12f), bc + V(0, -0.14f, -0.24f), bc + V(0, -0.21f, -0.35f), bc + V(0, -0.24f, -0.45f), bc + V(0.06f, -0.27f, -0.58f), bc + V(0.14f, -0.28f, -0.66f));
        b.Tube(tail, tp.Skip(9).ToArray(), 0.025f, 0.012f, olive, blend: 0f);
        for (int i = 4; i < tp.Length - 1; i += 2)
        {
            var d = tp[i + 1] - tp[i - 1];
            float k = 1f - i / (float)tp.Length;
            Blade(b, tail, tp[i], tp[i] + V(0, 0.04f + 0.03f * k, 0) - Vector3.Normalize(d) * 0.04f, 0.03f, teal, V(1f, 0, 0), 0.2f);
            Blade(b, tail, tp[i], tp[i] + V(0, -0.04f - 0.03f * k, 0) - Vector3.Normalize(d) * 0.04f, 0.03f, teal, V(1f, 0, 0), 0.2f);
        }
        for (int i = 0; i < 6; i++)
            Bead(b, Body, bc + V(0, 0.14f - 0.03f * i, -0.0f - 0.03f * i), 0.012f, PixelCanvas.Mix(olive, Black, 0.2f), Scales, 0.006f);
        Stretch(b, V(1.05f, 1.08f, 1.1f));
        return b;
    }

    // ------------------------------------------------------------------ 2883 Glacichthys, from Arctovish

    /// <summary>
    /// Glacichthys: Arctovish's head grown armoured and deep blue, its strong jaw full of fangs, set on a long body of
    /// pale ice with long ragged flippers, a frill of frost crystals along its back and a forked tail.
    /// </summary>
    private static PokeBuilder Glacichthys()
    {
        var b = Become(Arctovish(), "Glacichthys", 0.95f);
        Unlift(b, V(0, 0.3f, -0.04f));
        var blue = Rgb(64, 108, 170);
        var deep = Rgb(44, 78, 136);
        var ice = Rgb(214, 234, 248);
        var frost = Rgb(236, 246, 252);
        Recolour(b, (Rgb(192, 220, 242), blue), (Rgb(70, 118, 198), ice), (Rgb(46, 84, 160), deep), (Rgb(244, 248, 252), frost));
        Make(b, s => Is(s.Color, ice), Shell);
        int head = BoneOf(b, "head");
        var hc = V(0, 0.42f, 0.06f);
        // A heavy jaw, fanged
        b.Ell(head, hc + V(0, -0.13f, 0.07f), V(0.12f, 0.06f, 0.11f), deep, blend: 0.02f);
        for (int i = -2; i <= 2; i++)
        {
            float t = i / 2f;
            b.Spike(head, hc + V(t * 0.09f, -0.1f, 0.15f - MathF.Abs(t) * 0.05f), hc + V(t * 0.095f, -0.04f, 0.16f - MathF.Abs(t) * 0.05f), 0.014f, frost, mat: Shell, blend: 0.004f);
        }
        // A longer body of ice
        b.Limb(Body, V(0, 0.29f, -0.02f), V(0, 0.25f, -0.32f), 0.13f, 0.07f, ice, blend: 0.03f);
        int tail = BoneOf(b, "tail");
        Move(b, i => i == tail, V(0, 0.0f, -0.12f));
        // Long ragged flippers
        PokeBuilder.Both(s =>
        {
            int fin = BoneOf(b, s < 0 ? "finL" : "finR");
            Grow(b, V(1.4f, 1.7f, 1.4f), hc + V(0.12f * s, -0.12f, 0.02f), i => i == fin);
            Move(b, i => i == fin, V(-0.035f * s, 0.0f, -0.06f));
            Tint(b, sh => sh.Bone == fin, ice);
            var root = hc + V(0.1f * s, -0.12f, -0.18f);
            Frond(b, Body, root, root + V(0.12f * s, -0.14f, -0.08f), 0.05f, ice, V(s, 0, 0.4f), 0.25f, Shell);
        });
        // The frill of frost along its back
        for (int i = 0; i < 8; i++)
        {
            float z = 0.0f - i * 0.05f;
            var root = V(0, 0.36f - i * 0.012f, z);
            b.Spike(Body, root, root + V(0.01f * (i % 2 == 0 ? 1 : -1), 0.09f - i * 0.005f, -0.04f), 0.022f, frost, 0.5f, Glow, 0.008f);
        }
        // A smaller head on the longer body
        Grow(b, V(0.62f, 0.6f, 0.66f), hc + V(0, -0.1f, -0.02f), i => i == head);
        Stretch(b, V(1.05f, 1.05f, 1.15f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2894 Regifulgur, from Regieleki

    /// <summary>
    /// Regifulgur: Regieleki's core of current walled in black armour, a giant on two thick legs with great banded arms
    /// and clawed hands, its glowing yellow core showing through the plates of its chest, its domed face dotted with a
    /// cross of seven glowing dots, and its one ring of current doubled into two rings that cross round it like an
    /// orrery. Built anew, its body standing, from Regieleki's colours and parts.
    /// </summary>
    private static PokeBuilder Regifulgur()
    {
        var b = new PokeBuilder("Regifulgur", 1f, BodyPlan.Biped, V(0, 0.45f, 0)) { Coat = Metal };
        var black = Rgb(48, 48, 54);
        var plate = Rgb(70, 70, 78);
        var yellow = Rgb(246, 220, 80);
        var spark = Rgb(250, 246, 170);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.09f * s, 0.26f, 0));
            b.Limb(leg, V(0.09f * s, 0.27f, 0), V(0.1f * s, 0.07f, 0.02f), 0.06f, 0.055f, black);
            b.Box(leg, V(0.1f * s, 0.15f, 0.03f), V(0.05f, 0.04f, 0.05f), 0.012f, plate, mat: Metal, blend: 0.006f);
            b.Ell(leg, V(0.1f * s, 0.04f, 0.04f), V(0.065f, 0.04f, 0.08f), black);
        });
        var bc = V(0, 0.45f, 0);
        var br = V(0.17f, 0.17f, 0.14f);
        b.Ell(Body, bc, br, yellow, mat: Glow);
        // Armour plates over the core, the core glowing between them
        foreach (float y in new[] { -0.1f, 0.02f, 0.12f })
            b.Torus(Body, bc + V(0, y, 0), 0.16f - MathF.Abs(y) * 0.4f, 0.03f, black, mat: Metal, blend: 0.006f);
        PokeBuilder.Both(s => b.Ell(Body, bc + V(0.13f * s, 0.02f, 0), V(0.06f, 0.15f, 0.12f), black, mat: Metal, blend: 0.01f));
        int head = b.Head(bc + V(0, 0.14f, 0.02f));
        var c = bc + V(0, 0.21f, 0.05f);
        var r = V(0.085f, 0.08f, 0.075f);
        b.Ell(head, c, r, black, mat: Metal);
        b.PaintEll(head, c + V(0, 0.0f, 0.06f), V(0.06f, 0.07f, 0.03f), yellow);
        b.PaintEll(head, c + V(0, -0.005f, 0.07f), V(0.045f, 0.055f, 0.02f), black);
        foreach (var (x, y) in new[] { (0f, 0f), (0f, 0.022f), (0f, -0.022f), (0f, -0.044f), (-0.022f, 0f), (0.022f, 0f), (0f, 0.044f) })
            b.Mark(head, On(c, r, x, c.Y + y), V(0, 0, 1f), 0.007f, 0.007f, spark);
        // The great banded arms, clawed hands
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.17f * s, 0.08f, 0));
            var hand = bc + V(0.3f * s, -0.2f, 0.06f);
            b.Ell(arm, bc + V(0.19f * s, 0.09f, 0), V(0.07f, 0.06f, 0.07f), plate, mat: Metal, blend: 0.01f);
            b.Limb(arm, bc + V(0.18f * s, 0.06f, 0), hand, 0.05f, 0.065f, black);
            foreach (float t in new[] { 0.45f, 0.7f })
                b.Torus(arm, Vector3.Lerp(bc + V(0.18f * s, 0.06f, 0), hand, t), 0.065f, 0.018f, plate, Euler(hand - bc), mat: Metal, blend: 0.004f);
            for (int k = -1; k <= 1; k++)
            {
                var root = hand + V(0.01f * s, -0.04f, 0.03f * k);
                b.Limb(arm, root, root + V(0.01f * s, -0.04f, 0.01f), 0.014f, 0.01f, black, blend: 0.004f);
                Bead(b, arm, root + V(0.01f * s, -0.045f, 0.01f), 0.008f, yellow, Glow);
            }
        });
        // The two crossing rings of current
        foreach (float roll in new[] { 35f, -35f })
            b.Torus(Body, bc + V(0, 0.02f, 0), 0.22f, 0.012f, Rgb(120, 116, 90), V(0, 0, roll), 1f, 0.8f, Metal, 0.003f);
        return b;
    }

    // ------------------------------------------------------------------ 2895 Regidraconis, from Regidrago

    /// <summary>
    /// Regidraconis: Regidrago green all over, its face a dark plate with a cross of seven glowing dots, orange crystals
    /// grown large over its shoulders, arms and feet, and a second dragon's head of orange crystal rising from its back.
    /// </summary>
    private static PokeBuilder Regidraconis()
    {
        var b = Become(Regidrago(), "Regidraconis", 1f);
        var green = Rgb(54, 116, 82);
        var dark = Rgb(36, 72, 58);
        var orange = Rgb(244, 150, 90);
        var maw = Rgb(150, 70, 40);
        int head = BoneOf(b, "head");
        b.Sdf.RemoveAll(s => s.Bone == head);
        b.Model.Decals.Clear();
        Recolour(b, (Rgb(214, 42, 92), green), (Rgb(40, 76, 66), dark), (Rgb(150, 30, 62), maw));
        foreach (var s in b.Sdf.Shapes.Where(s => s.Shape == SdfShape.RoundCone && Is(s.Color, green) && s.Size.X < 0.02f))
            s.Color = orange;
        b.Sdf.Changed();
        var bc = V(0, 0.42f, 0);
        var br = V(0.16f, 0.17f, 0.14f);
        // The face: a dark plate, seven dots in a cross
        b.PaintEll(Body, bc + V(0, 0.05f, 0.12f), V(0.05f, 0.14f, 0.04f), Rgb(30, 50, 44));
        foreach (var (x, y) in new[] { (0f, 0.06f), (0f, 0.085f), (0f, 0.035f), (0f, 0.01f), (-0.025f, 0.06f), (0.025f, 0.06f), (0f, 0.11f) })
        {
            var at = On(bc, br, x, bc.Y + y);
            b.Mark(Body, at, Outward(bc, br, at), 0.009f, 0.009f, Rgb(250, 246, 220));
        }
        // Crystals over its shoulders, arms and feet
        void Crystal(int bone, Vector3 at, Vector3 dir, float len, float w) => b.Spike(bone, at, at + Vector3.Normalize(dir) * len, w, orange, 0.55f, Glow, 0.006f);
        PokeBuilder.Both(s =>
        {
            Crystal(Body, bc + V(0.12f * s, 0.12f, 0.0f), V(0.4f * s, 1f, -0.2f), 0.16f, 0.035f);
            Crystal(Body, bc + V(0.1f * s, 0.14f, -0.05f), V(0.2f * s, 1f, -0.4f), 0.1f, 0.025f);
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            Crystal(arm, bc + V(0.24f * s, 0.06f, 0.0f), V(0.3f * s, 1f, 0), 0.1f, 0.025f);
            Crystal(arm, bc + V(0.34f * s, 0.07f, -0.02f), V(0.2f * s, 1f, -0.2f), 0.09f, 0.022f);
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            Crystal(leg, V(0.12f * s, 0.12f, 0.03f), V(0.6f * s, 1f, 0.2f), 0.08f, 0.02f);
        });
        // The dragon's head of crystal rising from its back
        var neck = Smooth(2, bc + V(0, 0.12f, -0.06f), bc + V(0, 0.24f, -0.08f), bc + V(0, 0.32f, -0.04f));
        b.Tube(head, neck, 0.06f, 0.045f, orange, Glow, 0.02f);
        var hc = neck[^1] + V(0, 0.03f, 0.04f);
        b.Ell(head, hc, V(0.06f, 0.05f, 0.07f), orange, mat: Glow);
        b.Spike(head, hc + V(0, 0.0f, 0.04f), hc + V(0, -0.01f, 0.13f), 0.04f, orange, 0.6f, Glow);
        b.Spike(head, hc + V(0, -0.04f, 0.03f), hc + V(0, -0.06f, 0.11f), 0.025f, orange, 0.6f, Glow);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, hc + V(0.035f * s, 0.03f, -0.02f), hc + V(0.07f * s, 0.12f, -0.08f), 0.02f, orange, 0.5f, Glow);
            b.Mark(head, hc + V(0.045f * s, 0.015f, 0.04f), V(s, 0.2f, 0.6f), 0.01f, 0.007f, Rgb(120, 50, 30));
        });
        return b;
    }

    // ------------------------------------------------------------------ 2896 Hoarsteed, from Glastrier

    /// <summary>
    /// Hoarsteed: Glastrier larger, its mask of ice grown into a helm of hoarfrost with two great curved horns, its mane
    /// of ice long and streaming, a cloak of icicles hanging over its back and flanks, its tail long and crystalline and
    /// its hooves ringed with frost; its eyes are black.
    /// </summary>
    private static PokeBuilder Hoarsteed()
    {
        var b = Become(Glastrier(), "Hoarsteed", 1f);
        var white = Rgb(240, 242, 248);
        var ice = Rgb(172, 202, 240);
        var pale = Rgb(206, 226, 248);
        int head = BoneOf(b, "head");
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
            d.Iris = Rgb(24, 26, 40);
        var bc = V(0, 0.55f, -0.02f);
        var c = bc + V(0, 0.33f, 0.33f);
        var mc = c + V(0, 0.02f, 0.03f);
        // The two horns of the helm
        PokeBuilder.Both(s => b.Tube(head, Smooth(3, mc + V(0.03f * s, 0.05f, 0.0f), mc + V(0.06f * s, 0.15f, -0.02f), mc + V(0.05f * s, 0.24f, -0.08f), mc + V(0.02f * s, 0.29f, -0.15f)), 0.026f, 0.006f, pale, Shell, 0.006f));
        // A long mane
        for (int i = 0; i < 7; i++)
        {
            float t = i / 6f;
            var root = Vector3.Lerp(c + V(0, 0.05f, -0.06f), bc + V(0, 0.17f, 0.12f), t);
            PokeBuilder.Both(s => b.Tube(head, Smooth(2, root, root + V(0.05f * s, -0.04f, -0.05f), root + V(0.08f * s, -0.16f, -0.06f)), 0.02f, 0.006f, i % 2 == 0 ? ice : pale, Shell, 0.004f));
        }
        // The cloak of icicles over its back
        var br = V(0.15f, 0.15f, 0.28f);
        for (int i = 0; i < 16; i++)
        {
            float a = -1.3f + 2.6f * (i % 8) / 7f;
            float z = i < 8 ? 0.1f : -0.12f;
            var root = bc + V(MathF.Sin(a) * br.X * 0.95f, MathF.Cos(a) * br.Y * 0.9f, z);
            b.Spike(Body, root, root + V(MathF.Sin(a) * 0.02f, -0.1f - 0.03f * (i % 3), 0), 0.022f, i % 3 == 0 ? pale : ice, 0.6f, Shell, 0.006f);
        }
        b.Ell(Body, bc + V(0, 0.1f, 0), V(0.15f, 0.07f, 0.26f), ice, mat: Shell, blend: 0.02f);
        // Frost round each hoof
        foreach (var leg in b.Model.Bones.Select((bn, i) => (bn, i)).Where(x => x.bn.Role == PokeRole.Leg).Select(x => x.i).ToList())
        {
            var j = b.Model.Skeleton[leg].Joint;
            FurTufts(b, leg, V(j.X * 1.05f, 0.11f, j.Z + 0.015f), V(0.06f, 0.03f, 0.06f), 10, 0.03f, 0.012f, white, -2f, -1f, 0.6f);
        }
        Stretch(b, V(1.06f, 1.06f, 1.06f));
        return b;
    }

    // ------------------------------------------------------------------ 2897 Shadesteed, from Spectrier

    /// <summary>
    /// Shadesteed: Spectrier longer in the leg, its smoke of a mane and tail become a trailing flame of glowing pale
    /// blue, its hooves gone to wisps of pale smoke, a crown of black tines on its brow and eyes glowing pale blue.
    /// </summary>
    private static PokeBuilder Shadesteed()
    {
        var b = Become(Spectrier(), "Shadesteed", 1f);
        var black = Rgb(40, 40, 54);
        var flame = Rgb(150, 190, 240);
        var light = Rgb(206, 228, 250);
        Recolour(b, (Rgb(104, 76, 182), flame), (Rgb(196, 180, 234), light), (Rgb(66, 54, 110), Rgb(52, 54, 66)));
        Make(b, s => Is(s.Color, flame) || Is(s.Color, light), Glow);
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
            d.Pupil = Rgb(120, 170, 240);
        int head = BoneOf(b, "head");
        var bc = V(0, 0.55f, -0.02f);
        var c = bc + V(0, 0.32f, 0.32f);
        // A crown of black tines on its brow
        for (int i = -2; i <= 2; i++)
            b.Spike(head, c + V(i * 0.018f, 0.05f, -0.02f), c + V(i * 0.04f, 0.15f - MathF.Abs(i) * 0.015f, -0.05f), 0.01f, black, 0.6f, Shell, 0.004f);
        // More of the mane's flame
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < 3; i++)
            {
                var root = c + V(0.02f * s, 0.06f - 0.04f * i, -0.04f - 0.06f * i);
                b.Tube(head, Smooth(3, root, root + V(0.04f * s, 0.06f, -0.1f), root + V(0.08f * s, 0.02f, -0.2f), root + V(0.06f * s, 0.08f, -0.3f)), 0.024f, 0.006f, i == 1 ? light : flame, Glow, 0.004f);
            }
        });
        // Wisps of smoke at its hooves
        foreach (var leg in b.Model.Bones.Select((bn, i) => (bn, i)).Where(x => x.bn.Role == PokeRole.Leg).Select(x => x.i).ToList())
        {
            var j = b.Model.Skeleton[leg].Joint;
            var hoof = V(j.X * 1.05f, 0.03f, j.Z + 0.015f);
            foreach (var d in new[] { V(0.05f, 0.02f, 0.03f), V(-0.05f, 0.03f, -0.02f), V(0.0f, 0.02f, -0.06f) })
                b.Tube(leg, Smooth(2, hoof, hoof + d * 0.6f + V(0, 0.02f, 0), hoof + d + V(0, 0.04f, 0)), 0.02f, 0.006f, flame, Glow, 0.004f);
        }
        Stretch(b, V(0.95f, 1.12f, 1.04f));
        return b;
    }

    // ------------------------------------------------------------------ 2899 Wyrdstag, from Wyrdeer

    /// <summary>
    /// Wyrdstag: Wyrdeer larger and a deeper grey, its golden antlers grown into a dark crown of many tines set with six
    /// orbs of glowing pale blue, its white beard longer and a mantle of white fur over its shoulders, and its eyes
    /// glowing pale blue.
    /// </summary>
    private static PokeBuilder Wyrdstag()
    {
        var b = Become(Wyrdeer(), "Wyrdstag", 1f);
        var grey = Rgb(160, 162, 172);
        var dark = Rgb(118, 120, 132);
        var white = Rgb(244, 242, 246);
        var antler = Rgb(56, 58, 66);
        var orb = Rgb(176, 220, 250);
        Recolour(b, (Rgb(196, 198, 208), grey), (Rgb(140, 142, 156), dark), (Rgb(226, 194, 118), antler), (Rgb(54, 42, 58), orb));
        Make(b, s => Is(s.Color, orb), Glow);
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
            d.Iris = Rgb(140, 200, 250);
        int head = BoneOf(b, "head");
        var bc = V(0, 0.46f, -0.02f);
        var c = bc + V(0, 0.31f, 0.25f);
        // More tines, and a third orb on each antler
        PokeBuilder.Both(s =>
        {
            var path = Smooth(3, c + V(0.025f * s, 0.05f, -0.01f), c + V(0.07f * s, 0.18f, -0.04f), c + V(0.15f * s, 0.27f, -0.03f), c + V(0.25f * s, 0.33f, 0.02f));
            foreach (var (k, d) in new[] { (0.2f, V(0.06f * s, 0.06f, 0.05f)), (0.45f, V(-0.03f * s, 0.14f, -0.04f)), (0.65f, V(0.05f * s, 0.12f, 0.0f)), (0.92f, V(0.04f * s, 0.08f, -0.03f)), (1f, V(0.06f * s, 0.03f, 0.03f)) })
            {
                var at = path[(int)(k * (path.Length - 1))];
                b.Limb(head, at, at + d, 0.012f, 0.007f, antler, Shell, 0.004f);
            }
            b.Ell(head, path[(int)(0.88f * (path.Length - 1))], V(0.022f, 0.022f, 0.022f), orb, mat: Glow, blend: 0.006f);
        });
        // A longer beard and a mantle of fur over its shoulders
        var beard = bc + V(0, -0.02f, 0.17f);
        FurTufts(b, Body, beard + V(0, -0.03f, 0.01f), V(0.11f, 0.14f, 0.085f), 18, 0.07f, 0.024f, white, 1.2f, -1f, 0.9f);
        FurTufts(b, Body, bc + V(0, 0.04f, 0.08f), V(0.12f, 0.08f, 0.1f), 22, 0.06f, 0.022f, white, -0.2f, -0.9f, 0.8f);
        Stretch(b, V(1.06f, 1.06f, 1.06f));
        return b;
    }

    // ------------------------------------------------------------------ 2900 Halberdon, from Kleavor

    /// <summary>
    /// Halberdon: Kleavor taller and a beetle's brown, its horn grown into a great curved horn of dark stone, lumps of
    /// black augurite on its shoulders and knees, its axes become halberds, a long brown haft through each hand with its
    /// stone blade and a spike of stone at each end, and its wings short and stiff like a grey cape.
    /// </summary>
    private static PokeBuilder Halberdon()
    {
        var b = Become(Kleavor(), "Halberdon", 0.95f);
        var brown = Rgb(130, 96, 62);
        var stone = Rgb(62, 56, 56);
        var grey = Rgb(150, 150, 146);
        var haft = Rgb(110, 80, 52);
        int head = BoneOf(b, "head");
        Recolour(b, (Rgb(198, 170, 118), brown), (Rgb(84, 70, 64), stone), (Rgb(232, 218, 182), grey));
        var bc = V(0, 0.5f, 0);
        var hc = bc + V(0, 0.2f, 0.04f);
        // The great curved horn
        b.Tube(head, Smooth(3, hc + V(0, 0.05f, 0.03f), hc + V(0, 0.15f, 0.04f), hc + V(0, 0.22f, -0.03f), hc + V(0, 0.22f, -0.12f)), 0.035f, 0.008f, stone, Shell, 0.006f);
        b.Spike(head, hc + V(0, 0.0f, 0.06f), hc + V(0, -0.02f, 0.14f), 0.02f, stone, 0.6f, Shell);
        // Lumps of augurite on its shoulders and knees
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            Lumps(b, arm, bc + V(0.09f * s, 0.08f, 0.02f), V(0.04f, 0.035f, 0.04f), 7, 0.02f, stone, Rgb(84, 76, 74), 9f);
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            Lumps(b, leg, V(0.1f * s, 0.26f, 0.06f), V(0.03f, 0.03f, 0.03f), 5, 0.017f, stone, Rgb(84, 76, 74), 9f);
            // The halberd: a haft through the hand, its spike at each end
            var hand = bc + V(0.2f * s, 0.06f, 0.12f);
            var low = hand + V(0.04f * s, -0.34f, 0.08f);
            var high = hand + V(-0.03f * s, 0.24f, -0.06f);
            b.Limb(arm, low, high, 0.012f, 0.012f, haft, Shell, 0.004f);
            b.Spike(arm, high, high + Vector3.Normalize(high - low) * 0.08f, 0.016f, stone, 0.6f, Shell, 0.003f);
            b.Spike(arm, low, low + Vector3.Normalize(low - high) * 0.1f, 0.018f, stone, 0.6f, Shell, 0.003f);
            Blade(b, arm, low + V(0, 0.04f, 0), low + V(0.1f * s, 0.06f, 0.0f), 0.05f, stone, V(0, 0, 1f), 0.25f, Shell);
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            Grow(b, V(1.2f, 1.9f, 1.1f), bc + V(0.03f * s, 0.06f, -0.06f), i => i == wing);
        });
        Stretch(b, V(1.02f, 1.12f, 1.02f));
        return b;
    }

    // ------------------------------------------------------------------ 2902 Shoalwraith, from Basculegion

    /// <summary>
    /// Shoalwraith: Basculegion longer, its fins grown ragged, its eyes red, and the souls of its school grown to a
    /// shoal of seven little ghost fish of glowing pale blue trailing round it; a female has a red stripe along her
    /// sides where she was the one.
    /// </summary>
    /// <param name="female">A female keeps Basculegion's: spotted, a red stripe along each side.</param>
    private static PokeBuilder Shoalwraith(bool female = false)
    {
        var b = Become(BasculegionBuild(female), "Shoalwraith", 1f);
        Unlift(b, V(0, 0.32f, -0.04f));
        var green = Rgb(80, 130, 92);
        var ghost = Rgb(196, 230, 246);
        var red = Rgb(190, 50, 70);
        Recolour(b, (Rgb(116, 154, 116), green), (Rgb(232, 82, 120), ghost), (Rgb(200, 236, 236), ghost), (Rgb(236, 236, 228), Rgb(170, 210, 196)));
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
            d.Pupil = Rgb(230, 40, 60);
        var mc = V(0, 0.32f, -0.06f);
        var mr = V(0.11f, 0.14f, 0.26f);
        if (female)
            PokeBuilder.Both(s => b.PaintEll(Body, Out(mc, mr, default, V(s, 0.15f, 0)), V(0.02f, 0.025f, 0.24f), red, soft: 0.006f));
        // Ragged fins
        PokeBuilder.Both(s =>
        {
            foreach (var (from, to) in new[] { (V(0.08f, 0.24f, -0.1f), V(0.17f, 0.12f, -0.2f)), (V(0.06f, 0.4f, -0.16f), V(0.14f, 0.48f, -0.26f)) })
                Blade(b, Body, V(from.X * s, from.Y, from.Z), V(to.X * s, to.Y, to.Z), 0.035f, Rgb(170, 210, 196), V(0, 1f, 0.3f), 0.2f);
        });
        // The shoal of seven ghost fish round it
        for (int i = 0; i < 7; i++)
        {
            float a = i * MathF.Tau / 7f + 0.4f;
            var at = V(MathF.Cos(a) * 0.26f, 0.32f + MathF.Sin(a * 2f) * 0.1f, MathF.Sin(a) * 0.36f - 0.05f);
            var way = Vector3.Normalize(V(-MathF.Sin(a), 0.05f, MathF.Cos(a)));
            b.Ell(Body, at, V(0.022f, 0.022f, 0.022f) + Vector3.Abs(way) * 0.02f, ghost, mat: Glow, blend: 0.004f);
            b.Spike(Body, at - way * 0.02f, at - way * 0.07f, 0.016f, ghost, 0.5f, Glow, 0.004f);
            b.Limb(Body, at, Vector3.Lerp(at, V(0, 0.32f, -0.06f), 0.8f), 0.009f, 0.009f, ghost, Glow, 0.003f);
        }
        Stretch(b, V(1f, 1f, 1.2f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2903 Clifflasher, from Sneasler

    /// <summary>
    /// Clifflasher: Sneasler taller and a darker grey, its claws long and purple with a sheen of poison, a second pair
    /// of hooked claws on its feet for climbing, and its crest grown tall and swept back, a mane of cream with purple
    /// tips.
    /// </summary>
    private static PokeBuilder Clifflasher()
    {
        var b = Become(Sneasler(), "Clifflasher", 0.97f);
        var grey = Rgb(150, 150, 160);
        var dark = Rgb(84, 86, 98);
        var cream = Rgb(240, 232, 196);
        var purple = Rgb(110, 80, 160);
        int head = BoneOf(b, "head");
        Tint(b, s => s.Bone == head && (Is(s.Color, Rgb(108, 70, 152)) || Is(s.Color, Rgb(214, 66, 122))) && s.Shape == SdfShape.RoundCone, cream);
        Recolour(b, (Rgb(178, 190, 228), grey), (Rgb(214, 66, 122), purple), (Rgb(108, 70, 152), dark));
        Make(b, s => Is(s.Color, purple), Glow);
        var bc = V(0, 0.52f, 0);
        var c = bc + V(0, 0.24f, 0.03f);
        // The tall swept crest of cream spikes
        for (int i = 0; i < 9; i++)
        {
            float t = i / 8f;
            var root = c + V(-0.05f + 0.1f * t, 0.05f, -0.02f - 0.02f * (i % 3));
            var tip = root + V((t - 0.5f) * 0.2f, 0.16f - MathF.Abs(t - 0.5f) * 0.12f, -0.2f - 0.06f * (i % 2));
            b.Spike(head, root, tip, 0.03f, cream, 0.5f, blend: 0.01f);
            b.Spike(head, Vector3.Lerp(root, tip, 0.8f), tip + Vector3.Normalize(tip - root) * 0.04f, 0.01f, purple, 0.5f, Glow, 0.003f);
        }
        // Dark patches, and climbing claws on its feet
        PokeBuilder.Both(s =>
        {
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            b.PaintEll(leg, V(0.08f * s, 0.32f, 0.02f), V(0.04f, 0.06f, 0.04f), dark);
            for (int i = -1; i <= 1; i++)
                b.Tube(leg, Smooth(2, V(0.1f * s + 0.018f * i, 0.03f, 0.07f), V(0.1f * s + 0.02f * i, 0.04f, 0.11f), V(0.1f * s + 0.02f * i, 0.0f, 0.13f)), 0.009f, 0.003f, purple, Glow, 0.003f);
        });
        Stretch(b, V(1f, 1.12f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2904 Spinarmada, from Overqwil

    /// <summary>
    /// Spinarmada: Overqwil longer and black all over, its spines grown into a forest of black spears with purple tips,
    /// a ragged sail of a fin along its back, its jaw wider and lined with fangs, and yellow eyes.
    /// </summary>
    private static PokeBuilder Spinarmada()
    {
        var b = Become(Overqwil(), "Spinarmada", 0.98f);
        Unlift(b, V(0, 0.32f, 0));
        var black = Rgb(30, 30, 40);
        var purple = Rgb(110, 50, 150);
        var fang = Rgb(230, 216, 150);
        b.Sdf.RemoveAll(s => s.Op == SdfOp.Paint && Is(s.Color, Rgb(226, 218, 190)));
        b.Sdf.RemoveAll(s => s.Shape == SdfShape.Ellipsoid && s.Op == SdfOp.Add && Is(s.Color, Rgb(226, 218, 190)));
        Recolour(b, (Rgb(44, 44, 78), black), (Rgb(110, 64, 160), black), (Rgb(226, 218, 190), black), (Rgb(222, 92, 172), purple), (Rgb(170, 110, 200), Rgb(90, 40, 60)));
        b.Model.Decals.RemoveAll(d => !d.IsEye);
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
        {
            d.Pupil = Black;
            d.White = Rgb(240, 214, 90);
        }
        var c = V(0, 0.32f, 0);
        var r = V(0.2f, 0.19f, 0.19f);
        // A forest of spears with purple tips, longer behind
        float golden = MathF.PI * (3f - MathF.Sqrt(5f));
        for (int i = 0; i < 26; i++)
        {
            float u = -0.7f + 1.5f * (i + 0.5f) / 26f, ring = MathF.Sqrt(1f - u * u), a = i * golden;
            var dir = V(MathF.Sin(a) * ring, u, MathF.Cos(a) * ring);
            if (dir.Z > 0.3f) continue;
            var root = c + dir * r * 0.9f;
            var end = root + Vector3.Normalize(dir + V(0, 0, -0.6f)) * (0.18f + 0.04f * (i % 3));
            b.Spike(Body, root, end, 0.016f, black, mat: Shell, blend: 0.006f);
            b.PaintEll(Body, Vector3.Lerp(root, end, 0.88f), V(0.012f, 0.025f, 0.012f), purple, Euler(end - root), 0.004f);
        }
        // The sail along its back
        for (int i = 0; i < 6; i++)
        {
            float z = 0.1f - i * 0.07f;
            Blade(b, Body, c + V(0, r.Y * 0.85f, z), c + V(0, r.Y + 0.2f - MathF.Abs(i - 2.5f) * 0.02f, z - 0.05f), 0.05f, Rgb(70, 30, 90), V(1f, 0, 0), 0.15f);
        }
        // A wide jaw lined with fangs
        var mouth = Out(c, r, default, V(0, -0.25f, 1f));
        Grin(b, Body, mouth + V(0, 0, -0.02f), V(0.09f, 0.04f, 0.05f), Rgb(90, 40, 60));
        for (int i = -4; i <= 4; i++)
        {
            float x = i * 0.02f;
            float z = mouth.Z - 0.015f - MathF.Abs(i) * 0.006f;
            b.Spike(Body, V(x, mouth.Y + 0.035f, z), V(x, mouth.Y + 0.005f, z + 0.005f), 0.007f, fang, mat: Shell, blend: 0.002f);
            b.Spike(Body, V(x + 0.01f, mouth.Y - 0.035f, z), V(x + 0.01f, mouth.Y - 0.008f, z + 0.005f), 0.007f, fang, mat: Shell, blend: 0.002f);
        }
        Stretch(b, V(1f, 1f, 1.3f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2905 Enamorata, from Enamorus

    /// <summary>
    /// Enamorata: Enamorus larger, its cloud grown into a chariot of cloud with a heart-shaped prow, horns long and
    /// curving back in deep rose, its tail's dark spots grown into a ring of hearts trailing round it.
    /// </summary>
    private static PokeBuilder Enamorata()
    {
        var b = Become(Enamorus(), "Enamorata", 1f);
        Unlift(b, V(0, 0.44f, 0));
        var white = Rgb(244, 232, 240);
        var rose = Rgb(170, 50, 100);
        int head = BoneOf(b, "head");
        var bc = V(0, 0.44f, 0);
        var c = bc + V(0, 0.21f, 0.02f);
        // The chariot of cloud with its heart-shaped prow
        foreach (var (o, k) in new[] { (V(0, 0.22f, 0.0f), 0.18f), (V(0.14f, 0.24f, 0.02f), 0.1f), (V(-0.14f, 0.24f, 0.02f), 0.1f), (V(0, 0.24f, -0.14f), 0.12f), (V(0, 0.18f, 0.12f), 0.12f) })
            b.Ell(Root, o, V(k * 1.1f, k * 0.6f, k), white, blend: 0.03f);
        var prow = V(0, 0.26f, 0.22f);
        PokeBuilder.Both(s => b.Ell(Root, prow + V(0.035f * s, 0.03f, 0), V(0.045f, 0.045f, 0.03f), white, blend: 0.02f));
        b.Spike(Root, prow + V(0, 0.02f, 0), prow + V(0, -0.06f, 0.01f), 0.05f, white, 0.6f, blend: 0.02f);
        HeartPaint(b, Root, prow + V(0, 0.01f, 0.03f), V(0, 0, 1f), 0.06f, EnamorusPink);
        // Long curved horns
        PokeBuilder.Both(s => b.Tube(head, Smooth(3, c + V(0.03f * s, 0.04f, -0.02f), c + V(0.05f * s, 0.12f, -0.05f), c + V(0.09f * s, 0.16f, -0.13f), c + V(0.14f * s, 0.12f, -0.18f)), 0.016f, 0.005f, rose, Shell, 0.004f));
        // A trail of hearts winding up from the cloud round behind it
        var trail = Enumerable.Range(0, 13).Select(i =>
        {
            float t = i / 12f, a = -0.4f - t * MathF.PI * 1.3f;
            return V(MathF.Cos(a) * (0.13f + 0.15f * t), 0.24f + 0.38f * t, MathF.Sin(a) * (0.13f + 0.15f * t));
        }).ToArray();
        b.Tube(Root, trail, 0.011f, 0.009f, EnamorusPink, Glow, 0.003f);
        for (int i = 2; i < trail.Length; i += 2)
        {
            var n = Vector3.Normalize(V(trail[i].X, 0, trail[i].Z));
            var side = Vector3.Normalize(Vector3.Cross(n, V(0, 1f, 0)));
            var col = i % 4 == 0 ? rose : EnamorusPink;
            b.Spike(Root, trail[i] + V(0, 0.004f, 0), trail[i] - V(0, 0.022f, 0), 0.014f, col, 0.6f, Glow, 0.004f);
            PokeBuilder.Both(s => b.Ell(Root, trail[i] + V(0, 0.006f, 0) + side * 0.01f * s, V(0.012f, 0.012f, 0.012f), col, mat: Glow, blend: 0.004f));
        }
        Stretch(b, V(1.08f, 1.08f, 1.08f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ The supplement: designs grown from regional forms

    // ------------------------------------------------------------------ 5078 Glimmane, from Galarian Rapidash

    /// <summary>
    /// Glimmane: Galarian Rapidash taller and finer, its pastel mane grown into a cloak of glowing pastel light that
    /// falls past its shoulders to its hocks, its horn longer and banded with a faint rainbow, and a second small mane
    /// of pastel puffs at its tail's root.
    /// </summary>
    private static PokeBuilder Glimmane()
    {
        var b = Become(HorseGalar(true), "Glimmane", 0.95f);
        float k = 1.12f;
        int mane = BoneOf(b, "mane");
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        foreach (var c in new[] { Rgb(248, 184, 222), Rgb(160, 228, 214), Rgb(204, 186, 244) })
            Make(b, s => (s.Bone == mane || s.Bone == tail) && Is(s.Color, c), Glow);
        // The cloak: long locks from the crest down past the shoulders and flanks to the hocks
        var colors = new[] { Rgb(248, 184, 222), Rgb(170, 220, 240), Rgb(214, 196, 248) };
        int n = 0;
        foreach (var (y, z) in new[] { (0.76f, 0.2f), (0.67f, 0.16f), (0.58f, 0.12f) })
            PokeBuilder.Both(s =>
            {
                var path = new[] { V(0.03f * s, y, z * k), V(0.1f * s, y - 0.1f, (z - 0.1f) * k), V(0.14f * s, y - 0.3f, (z - 0.2f) * k), V(0.15f * s, 0.32f, (z - 0.3f) * k), V(0.14f * s, 0.16f, (z - 0.38f) * k) };
                b.Tube(mane, Smooth(3, path), 0.032f, 0.012f, colors[n++ % 3], Glow, 0f);
            });
        // A longer horn, banded in pastel
        var root = V(0, 0.85f, 0.34f * k);
        var dir = Vector3.Normalize(V(0, 0.23f, 0.13f * k));
        b.Spike(head, root, root + dir * 0.34f, 0.03f, Rgb(244, 240, 250), mat: Shell);
        for (int i = 0; i < 6; i++)
            b.PaintTorus(head, root + dir * (0.06f + 0.055f * i), 0.026f - 0.0035f * i, 0.005f, colors[i % 3], Euler(dir) + V(25f, 0, 0));
        // A second, smaller mane at the tail's root
        foreach (var (o, i) in new[] { (V(0, 0.62f, -0.26f), 0), (V(0.04f, 0.6f, -0.3f), 1), (V(-0.04f, 0.6f, -0.3f), 2) })
            PastelPuff(b, tail, o * V(1, 1, k), 0.04f, i);
        Stretch(b, V(0.95f, 1.1f, 1.04f));
        return b;
    }

    // ------------------------------------------------------------------ 5080 Slowslinger, from Galarian Slowbro

    /// <summary>
    /// Slowslinger: Galarian Slowbro taller with a gunslinger's lean, the Shellder on its right arm grown into a long
    /// purple-grey shell with a bore like a barrel, a second, smaller one on its left forearm, a third on its tail, a
    /// shell worn on its head like a hat, and a cuff of braided Galarica twigs round each wrist.
    /// </summary>
    private static PokeBuilder Slowslinger()
    {
        var b = Become(SlowbroGalar(), "Slowslinger", 0.92f);
        var shell = Rgb(150, 134, 176);
        var spike = Rgb(110, 96, 140);
        var band = Rgb(120, 104, 150);
        var twig = Rgb(132, 92, 58);
        var dark = Rgb(60, 54, 70);
        Recolour(b, (Rgb(182, 194, 210), shell), (Rgb(140, 148, 166), spike), (Rgb(146, 158, 178), band));
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var hand = V(0.2f * s, 0.32f, 0.12f);
            // The braided cuff
            var d = Vector3.Normalize(hand - V(0.15f * s, 0.44f, 0.03f));
            b.Torus(arm, hand - d * 0.04f, 0.042f, 0.012f, twig, Euler(d), mat: Shell, blend: 0.004f);
            b.Torus(arm, hand - d * 0.06f, 0.044f, 0.01f, PixelCanvas.Mix(twig, Black, 0.2f), Euler(d) + V(15f, 0, 0), mat: Shell, blend: 0.004f);
            if (s > 0)
            {
                // The barrel: a long cone of shell along the forearm, its bore open at the end
                var muzzle = hand + V(0.04f, -0.05f, 0.2f);
                b.Spike(arm, hand + V(0, 0, -0.08f), muzzle, 0.07f, shell, 0.9f, Shell, 0.01f);
                b.Limb(arm, muzzle - Vector3.Normalize(muzzle - hand) * 0.03f, muzzle + Vector3.Normalize(muzzle - hand) * 0.03f, 0.024f, 0.024f, dark, Metal, 0.004f);
                b.Ell(arm, muzzle + Vector3.Normalize(muzzle - hand) * 0.03f, V(0.016f, 0.016f, 0.016f), Rgb(20, 18, 24), blend: 0.002f);
            }
            else
                ShellderOn(b, arm, hand + V(0.0f, 0.02f, -0.02f), V(-0.4f, -0.8f, 0.3f), 0.07f, shell, spike, band);
        });
        // A small shell at the tail's end and a hat of shell
        var path = Smooth(3, V(0, 0.22f, -0.13f), V(0.08f, 0.12f, -0.28f), V(0.2f, 0.09f, -0.34f), V(0.3f, 0.12f, -0.3f));
        b.Torus(tail, path[^3], 0.06f, 0.02f, shell, Euler(path[^2] - path[^4]), mat: Shell, blend: 0.006f);
        var hat = V(0, 0.65f, 0.05f) + V(0, 0.14f, -0.02f);
        b.Ell(head, hat, V(0.16f, 0.025f, 0.15f), shell, mat: Shell, blend: 0.01f);
        b.Spike(head, hat, hat + V(0.02f, 0.11f, -0.03f), 0.08f, shell, mat: Shell, blend: 0.01f);
        b.PaintTorus(head, hat + V(0, 0.03f, 0), 0.075f, 0.008f, band, default);
        // Its lean
        b.Model.Decals.Where(dd => dd.IsEye).ToList().ForEach(dd => dd.Closed = false);
        Stretch(b, V(1f, 1.1f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 5199 Slowmagus, from Galarian Slowking

    /// <summary>
    /// Slowmagus: Galarian Slowking's head and arms pink in a long hooded robe of grey-violet trimmed dark, a rope belt
    /// with a flask of glowing poison hanging at its waist, a wreath of Galarica twigs round its brow, and its Shellder
    /// grown into a tall spiral crown of purple shell with an eye glowing at its top.
    /// </summary>
    private static PokeBuilder Slowmagus()
    {
        var b = Become(SlowkingGalar(), "Slowmagus", 0.92f);
        var pink = Rgb(240, 160, 184);
        var robe = Rgb(160, 150, 176);
        var trim = Rgb(70, 62, 80);
        var twig = Rgb(120, 84, 56);
        var shell = Rgb(120, 96, 160);
        int head = BoneOf(b, "head");
        // The cloak goes; the head is pink again
        b.Sdf.RemoveAll(s => s.Bone == Body && Is(s.Color, Rgb(70, 56, 92)));
        Recolour(b, (Rgb(126, 104, 156), pink));
        var hc = V(0, 0.66f, 0.04f);
        var mouth = hc + V(0, 0.095f, 0.0f);
        // The tall spiral crown and its eye
        var a = Vector3.Normalize(V(0, 1f, -0.12f));
        for (int i = 0; i < 4; i++)
            b.Torus(head, mouth + a * (0.2f + 0.06f * i), 0.06f - 0.012f * i, 0.016f, shell, Euler(a) + V(12f, 0, 0), mat: Shell, blend: 0.01f);
        b.Spike(head, mouth + a * 0.18f, mouth + a * 0.48f, 0.06f, shell, mat: Shell, blend: 0.01f);
        b.Ell(head, mouth + a * 0.4f + V(0, 0, 0.03f), V(0.016f, 0.016f, 0.012f), Rgb(190, 110, 240), mat: Glow, blend: 0.004f);
        // The wreath of twigs round its brow
        for (int i = 0; i < 12; i++)
        {
            float t = i * MathF.Tau / 12f;
            var at = hc + V(MathF.Sin(t) * 0.13f, 0.07f + 0.006f * MathF.Sin(t * 3f), MathF.Cos(t) * 0.12f);
            var along = V(MathF.Cos(t), 0.1f * MathF.Sin(t * 2f), -MathF.Sin(t));
            b.Limb(head, at - along * 0.04f, at + along * 0.04f, 0.007f, 0.006f, twig, Shell, 0.003f);
            b.Limb(head, at, at + V(MathF.Sin(t) * 0.02f, 0.03f, MathF.Cos(t) * 0.02f), 0.004f, 0.003f, twig, Shell, 0.002f);
        }
        // The robe: a hood behind its head, down to the ground, wide sleeves
        b.Limb(Body, V(0, 0.5f, -0.01f), V(0, 0.2f, -0.01f), 0.17f, 0.19f, robe, Fur, 0.02f);
        b.Ell(Body, V(0, 0.56f, -0.06f), V(0.15f, 0.08f, 0.12f), robe, mat: Fur, blend: 0.02f);
        b.PaintEll(Body, V(0, 0.3f, 0.2f), V(0.012f, 0.3f, 0.04f), trim, soft: 0.004f);
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            b.Limb(arm, V(0.14f * s, 0.44f, 0.03f), V(0.19f * s, 0.34f, 0.1f), 0.045f, 0.06f, robe, Fur, 0.01f);
            b.PaintTorus(arm, V(0.19f * s, 0.34f, 0.1f), 0.058f, 0.008f, trim, Euler(V(0.04f * s, -0.1f, 0.07f)));
        });
        // The rope belt and the flask
        b.Torus(Body, V(0, 0.32f, 0), 0.19f, 0.007f, trim, V(0, 0, 0), 1f, 1f, Fur, 0.003f);
        var flask = V(0.08f, 0.24f, 0.2f);
        b.Limb(Body, V(0.08f, 0.31f, 0.18f), flask + V(0, 0.04f, 0), 0.004f, 0.004f, trim, Fur, 0.002f);
        b.Ell(Body, flask, V(0.03f, 0.032f, 0.03f), Rgb(170, 130, 230), mat: Glow, blend: 0.004f);
        b.Limb(Body, flask + V(0, 0.025f, 0), flask + V(0, 0.05f, 0), 0.01f, 0.008f, Rgb(200, 210, 220), Shell, 0.002f);
        Stretch(b, V(1.02f, 1.04f, 1.02f));
        return b;
    }

    // ------------------------------------------------------------------ 5110 Purifume, from Galarian Weezing

    /// <summary>
    /// Purifume: Galarian Weezing grown to three bodies, a third risen above the two, each of dark grey cratered stone
    /// under a black top hat, each with a long curled moustache of pale green mist, chimney pots of brick between them
    /// breathing out pale green clouds.
    /// </summary>
    private static PokeBuilder Purifume()
    {
        var b = Become(WeezingGalar(), "Purifume", 1f);
        Unlift(b, V(-0.06f, 0.34f, 0));
        var body = Rgb(72, 68, 78);
        var mist = Rgb(200, 236, 180);
        var hat = Rgb(36, 34, 40);
        var brick = Rgb(150, 90, 66);
        int small = BoneOf(b, "small");
        // The old chimneys and white smoke and moss go
        b.Sdf.RemoveAll(s => Is(s.Color, Rgb(84, 80, 88)) || Is(s.Color, Rgb(64, 60, 68)) || Is(s.Color, Rgb(244, 246, 248)));
        Recolour(b, (Rgb(140, 196, 90), mist));
        var cBig = V(-0.08f, 0.32f, 0);
        var rBig = V(0.2f, 0.2f, 0.19f);
        var cSmall = V(0.19f, 0.42f, -0.04f);
        var rSmall = V(0.14f, 0.14f, 0.135f);
        // The third body, risen behind and between the two
        var cTop = V(0.04f, 0.62f, -0.08f);
        var rTop = V(0.15f, 0.15f, 0.145f);
        int top = b.Part("third", Body, cTop - V(0, 0.08f, 0), PokeRole.Head, 0.8f);
        b.Ell(top, cTop, rTop, body);
        b.Ell(Body, (cBig + cTop) * 0.5f, V(0.09f, 0.09f, 0.09f), body, blend: 0.05f);
        Craters(b, top, cTop, rTop, PixelCanvas.Mix(body, Rgb(30, 24, 40), 0.25f), (V(0.6f, 0.5f, -0.4f), 0.026f), (V(-0.7f, 0.2f, -0.3f), 0.024f));
        PokeBuilder.Both(s => b.Eye(top, Out(cTop, rTop, default, V(0.35f * s, 0.15f, 1f)), V(0.35f * s, 0.15f, 1f), 0.024f, sclera: true, pupil: Rgb(40, 70, 40), glare: true));
        // A hat on each, and a curled moustache of mist under each
        foreach (var (c, r, bone) in new[] { (cBig, rBig, Body), (cSmall, rSmall, small), (cTop, rTop, top) })
        {
            var crown = c + V(0, r.Y * 0.85f, 0);
            b.Ell(bone, crown, V(r.X * 0.95f, r.Y * 0.08f, r.Z * 0.95f), hat, mat: Fur, blend: 0.004f);
            b.Limb(bone, crown, crown + V(0, r.Y * 0.55f, 0), r.X * 0.48f, r.X * 0.5f, hat, Fur, 0.004f);
            var lip = Out(c, r, default, V(0, -0.15f, 1f));
            PokeBuilder.Both(s =>
            {
                var curl = Spiral(lip + V(r.X * 0.55f * s, 0.02f, -r.Z * 0.15f), V(s, 0, 0), V(0, 1f, 0), r.X * 0.13f, r.X * 0.04f, MathF.PI, MathF.PI * 2.6f, 10);
                b.Tube(bone, new[] { lip + V(0.01f * s, 0, 0.005f), lip + V(r.X * 0.3f * s, -0.01f, -r.Z * 0.04f) }.Concat(curl).ToArray(), r.X * 0.09f, r.X * 0.04f, mist, Glow, 0.004f);
            });
        }
        // Chimney pots of brick between them, breathing out green clouds
        foreach (var (at, h, bone) in new[] { (cBig + V(-0.12f, 0.13f, -0.08f), 0.12f, Body), (cSmall + V(0.08f, 0.09f, -0.06f), 0.1f, small), (cTop + V(-0.11f, 0.06f, -0.06f), 0.1f, top) })
        {
            b.Limb(bone, at, at + V(0, h, 0), 0.025f, 0.025f, brick, Shell, 0.006f);
            b.Torus(bone, at + V(0, h, 0), 0.026f, 0.008f, PixelCanvas.Mix(brick, Black, 0.3f), mat: Shell, blend: 0.003f);
            GasPuff(b, bone, at + V(0, h + 0.03f, 0), V(0.2f, 1f, 0), 0.05f, mist);
        }
        return Lift(b);
    }

    // ------------------------------------------------------------------ 5555 Frostruma, from Galarian Darmanitan

    /// <summary>
    /// Frostruma: Galarian Darmanitan standing up on broad feet like snowshoes, its white fur long and frosted with
    /// icicles, holding over its head in both arms a great ball of packed snow and ice cracked with the orange flame lit
    /// again inside it, the flame burning on its brows too.
    /// </summary>
    private static PokeBuilder Frostruma()
    {
        var b = Become(DarmanitanGalar(), "Frostruma", 0.95f);
        var white = Rgb(236, 242, 248);
        var ice = Rgb(196, 222, 242);
        var grey = Rgb(110, 120, 136);
        var flame = Rgb(240, 110, 40);
        var heart = Rgb(252, 214, 100);
        int head = BoneOf(b, "head");
        b.Sdf.RemoveAll(s => s.Bone == head && (Is(s.Color, White) && Vector3.Distance(s.A, V(0, 0.54f, 0.07f)) < 0.001f));
        b.Sdf.RemoveAll(s => s.Bone == head && (s.Shape == SdfShape.Ellipsoid && s.Size.X < 0.03f && !Is(s.Color, White)));
        Recolour(b, (Rgb(240, 242, 246), white), (Rgb(140, 176, 220), grey));
        var c = V(0, 0.41f, 0.11f);
        // Flaming brows
        PokeBuilder.Both(s => FlameRow(b, head, c + V(0.02f * s, 0.055f, 0.06f), c + V(0.085f * s, 0.06f, 0.04f), V(0.6f * s, 1f, -0.1f), 3, 0.1f, 0.02f, flame, heart, 0.8f));
        // The arms raised over its head, holding the great snowball
        var ball = V(0, 0.84f, 0.02f);
        var br = V(0.22f, 0.18f, 0.2f);
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "legL" : "legR");
            b.Sdf.RemoveAll(sh => sh.Bone == arm);
            var sh0 = V(0.15f * s, 0.42f, 0.04f);
            var elbow = V(0.26f * s, 0.6f, 0.04f);
            var hand = V(0.2f * s, 0.78f, 0.04f);
            b.Limb(arm, sh0, elbow, 0.06f, 0.05f, white);
            b.Limb(arm, elbow, hand, 0.05f, 0.045f, white);
            b.Ell(arm, hand, V(0.05f, 0.05f, 0.055f), grey);
            FurTufts(b, arm, Vector3.Lerp(sh0, elbow, 0.5f), V(0.05f, 0.08f, 0.05f), 8, 0.04f, 0.014f, white, 2f, -1f, 0.9f);
        });
        b.Ell(Body, ball, br, white, blend: 0.02f);
        Lumps(b, Body, ball, br, 26, 0.05f, white, ice, 9f);
        foreach (var (d, roll) in new[] { (V(0.4f, 0.8f, 0.4f), 30f), (V(-0.7f, 0.2f, 0.6f), -50f), (V(0.8f, -0.3f, 0.4f), 70f) })
            PaintAs(b, Body, Out(ball, br, default, d), V(0.012f, 0.05f, 0.012f), flame, Glow, V(0, 0, roll));
        var crack = Out(ball, br, default, V(0.3f, 1f, 0.2f));
        FlameTongue(b, Body, crack - V(0, 0.03f, 0), crack + V(0.02f, 0.12f, -0.01f), 0.045f, flame, heart);
        // It stands: legs lengthened, broad feet like snowshoes
        PokeBuilder.Both(s =>
        {
            int leg = BoneOf(b, s < 0 ? "hindL" : "hindR");
            b.Ell(leg, V(0.13f * s, 0.02f, -0.02f), V(0.075f, 0.02f, 0.1f), grey, blend: 0.01f);
            FurTufts(b, leg, V(0.12f * s, 0.14f, -0.08f), V(0.05f, 0.05f, 0.05f), 8, 0.035f, 0.012f, white, 2f, -1f, 0.9f);
        });
        // Icicles hanging from its fur
        foreach (var (x, y, z) in new[] { (-0.14f, 0.28f, 0.06f), (0.14f, 0.28f, 0.06f), (0f, 0.24f, 0.12f) })
            b.Spike(Body, V(x, y, z), V(x, y - 0.06f, z + 0.005f), 0.012f, ice, mat: Glow, blend: 0.004f);
        Stretch(b, V(1.05f, 1.06f, 1.05f));
        return b;
    }

    // ------------------------------------------------------------------ 5618 Snareflat, from Galarian Stunfisk

    /// <summary>
    /// Snareflat: Galarian Stunfisk wider and flatter, brown plated with olive, a great mouth across its front lined in
    /// yellow lips with two rows of steel teeth like a double trap, a steel plate riveted on its back, a fringe of brown
    /// weed along its edges and stubby clawed feet.
    /// </summary>
    private static PokeBuilder Snareflat()
    {
        var b = Become(StunfiskGalar(), "Snareflat", 0.88f);
        var brown = Rgb(124, 96, 70);
        var olive = Rgb(120, 130, 70);
        var steel = Rgb(170, 176, 186);
        var yellow = Rgb(226, 196, 60);
        var weed = Rgb(110, 104, 56);
        b.Sdf.RemoveAll(s => s.Shape == SdfShape.RoundCone || Is(s.Color, Rgb(210, 50, 50)) || Is(s.Color, White));
        Recolour(b, (Rgb(96, 100, 86), brown), (Rgb(150, 170, 80), olive));
        Grow(b, V(1.25f, 1.6f, 1.15f), V(0, 0.0f, 0), (SdfPrimitive _) => true, _ => true);
        var c = V(0, 0.08f, 0);
        var r = V(0.21f, 0.08f, 0.16f);
        // The mouth: lips of yellow and two rows of steel teeth
        var m = V(0, 0.07f, 0.12f);
        b.Cut(Body, m + V(0, 0, 0.05f), V(0.17f, 0.035f, 0.07f));
        foreach (float dy in new[] { 0.035f, -0.035f })
            b.Tube(Body, Enumerable.Range(0, 11).Select(i => { float t = (i / 10f - 0.5f) * 2.4f; return m + V(MathF.Sin(t) * 0.17f, dy * (0.6f + 0.4f * MathF.Cos(t)), MathF.Cos(t) * 0.05f - 0.0f); }).ToArray(), 0.01f, 0.01f, yellow, Shell, 0.004f);
        for (int row = 0; row < 2; row++)
            for (int i = -4; i <= 4; i++)
            {
                float t = i / 4.5f * 1.1f;
                var root = m + V(MathF.Sin(t) * (0.15f - row * 0.03f), 0.03f, MathF.Cos(t) * 0.04f - row * 0.03f);
                b.Spike(Body, root, root + V(0, -0.03f, 0.004f), 0.009f, steel, mat: Metal, blend: 0.002f);
                var low = root + V(0.008f, -0.06f, 0);
                b.Spike(Body, low, low + V(0, 0.028f, 0.004f), 0.008f, steel, mat: Metal, blend: 0.002f);
            }
        // The steel plate riveted on its back
        Plate(b, Body, V(0.1f, 0.15f, -0.06f), V(0.15f, 1f, 0), V(0.07f, 0.008f, 0.05f), steel, Metal, 0.004f);
        foreach (var o in new[] { V(0.05f, 0.155f, -0.1f), V(0.15f, 0.145f, -0.1f), V(0.05f, 0.16f, -0.02f), V(0.15f, 0.15f, -0.02f) })
            Bead(b, Body, o, 0.007f, Rgb(140, 80, 60), Metal);
        // The fringe of weed round its edges
        for (int i = 0; i < 22; i++)
        {
            float a = (i / 21f) * MathF.PI * 1.4f + MathF.PI * 0.8f;
            var at = c + V(MathF.Cos(a) * r.X * 0.95f, -0.02f, MathF.Sin(a) * r.Z * 0.95f);
            var outw = V(MathF.Cos(a), 0, MathF.Sin(a));
            b.Tube(Body, Smooth(2, at, at + outw * 0.03f + V(0, -0.01f, 0), at + outw * 0.05f + V(0, -0.05f, 0)), 0.007f, 0.003f, weed, Fur, 0.003f);
        }
        // Stubby clawed feet
        foreach (var (x, z) in new[] { (-0.14f, 0.06f), (0.14f, 0.06f), (-0.12f, -0.08f), (0.12f, -0.08f) })
        {
            b.Ell(Body, V(x, 0.025f, z), V(0.03f, 0.025f, 0.03f), brown, blend: 0.01f);
            Claws(b, Body, V(x, 0.008f, z + 0.025f), V(0.01f, 0, 0), V(0, -0.2f, 1f), 0.018f, 0.006f);
        }
        return b;
    }

    // ------------------------------------------------------------------ 5144 Hypnoskye, from Galarian Articuno

    /// <summary>
    /// Hypnoskye: Galarian Articuno larger and a deeper violet, long blades of violet light hovering out from its wings'
    /// edges, a crest of three long plumes curling back, and its tail grown long and full, ending in a feather with a
    /// glowing eye of light.
    /// </summary>
    private static PokeBuilder Hypnoskye()
    {
        var b = Become(ArticunoGalar(), "Hypnoskye", 1f);
        Unlift(b, V(0, 0.55f, 0));
        var violet = Rgb(120, 76, 170);
        var deep = Rgb(84, 52, 130);
        var light = Rgb(200, 160, 250);
        Recolour(b, (Rgb(172, 142, 208), violet), (Rgb(112, 84, 170), deep), (Rgb(244, 240, 250), Rgb(150, 116, 196)));
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        // Blades of light hovering out from each wing's edge
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            var wrist = V(0.24f * s, 0.74f, -0.06f);
            for (int i = 0; i < 5; i++)
            {
                float a = (70f - i * 20f) * Degree;
                var dir = Vector3.Normalize(V(MathF.Cos(a) * s, MathF.Sin(a), -0.25f));
                var root = wrist + dir * 0.3f;
                Blade(b, wing, root, root + dir * 0.16f, 0.02f, light, V(0, 0, 1f), 0.25f, Glow);
                b.Limb(wing, wrist + dir * 0.12f, root + dir * 0.02f, 0.01f, 0.009f, light, Glow, 0.004f);
            }
        });
        // A crest of three plumes curling back
        var c = V(0, 0.79f, 0.07f);
        foreach (var x in new[] { -0.025f, 0f, 0.025f })
            b.Tube(head, Smooth(2, c + V(x, 0.05f, -0.02f), c + V(x * 2f, 0.16f, -0.06f), c + V(x * 2.5f, 0.2f, -0.14f)), 0.01f, 0.005f, deep, blend: 0.003f);
        // The long tail and its eye of light
        var tp = Smooth(3, V(0, 0.45f, -0.1f), V(0, 0.28f, -0.22f), V(0.05f, 0.1f, -0.3f), V(0.16f, 0.04f, -0.22f), V(0.3f, 0.06f, -0.1f));
        Ribbon(b, tail, tp, 0.07f, deep, V(0, 1f, 0.3f), 0.014f);
        var eye = tp[^1];
        b.Ell(tail, eye, V(0.045f, 0.012f, 0.03f), light, mat: Glow, blend: 0.006f);
        b.PaintEll(tail, eye + V(0, 0.01f, 0), V(0.015f, 0.015f, 0.015f), Rgb(80, 60, 200));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 5145 Stridestorm, from Galarian Zapdos

    /// <summary>
    /// Stridestorm: Galarian Zapdos taller in the leg, its thighs thick with orange feathered muscle banded twice in
    /// black above the knee, its black wings short and folded at its sides like a runner's arms, its crest swept back
    /// into three points and its beak hooked and grey.
    /// </summary>
    private static PokeBuilder Stridestorm()
    {
        var b = Become(ZapdosGalar(), "Stridestorm", 1f);
        var orange = Rgb(232, 110, 52);
        var black = Rgb(42, 36, 40);
        var grey = Rgb(120, 116, 120);
        int head = BoneOf(b, "head");
        // The wings fold short against its sides
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            Tint(b, sh => sh.Bone == wing, black);
            Grow(b, V(0.45f, 0.55f, 0.9f), V(0.12f * s, 0.74f, 0.02f), i => i == wing);
            Move(b, i => i == wing, V(0, -0.06f, -0.04f));
            // Thick thighs banded black
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            var hip = V(0.09f * s, 0.6f, -0.02f);
            var knee = V(0.12f * s, 0.38f, 0.08f);
            b.Limb(leg, hip, knee, 0.085f, 0.06f, orange);
            foreach (float t in new[] { 0.55f, 0.75f })
                b.PaintTorus(leg, Vector3.Lerp(hip, knee, t), 0.075f - 0.02f * t, 0.01f, black, Euler(knee - hip));
        });
        b.Sdf.RemoveAll(s => s.Bone == head && s.Shape == SdfShape.RoundCone && Is(s.Color, black) && s.B.Z > 0.3f);
        var hc = V(0, 0.96f, 0.17f);
        // A hooked grey beak
        b.Tube(head, Smooth(2, hc + V(0, -0.005f, 0.05f), hc + V(0, 0.0f, 0.12f), hc + V(0, -0.04f, 0.15f)), 0.026f, 0.008f, grey, Shell, 0.004f);
        Stretch(b, V(1f, 1.08f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 5146 Duskphoenix, from Galarian Moltres

    /// <summary>
    /// Duskphoenix: Galarian Moltres larger, its wings of dark flame grown into long veils trailing violet and red, a
    /// crown of black feathers on its head, and its tail a long train of smoke with embers glowing in it.
    /// </summary>
    private static PokeBuilder Duskphoenix()
    {
        var b = Become(MoltresGalar(), "Duskphoenix", 1f);
        Unlift(b, V(0, 0.55f, 0));
        var black = Rgb(36, 32, 44);
        var violet = Rgb(130, 70, 180);
        var red = Rgb(170, 40, 60);
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        Recolour(b, (Rgb(48, 42, 56), black), (Rgb(222, 40, 96), violet));
        Make(b, s => Is(s.Color, violet), Glow);
        // Veils trailing from the wings
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            for (int i = 0; i < 5; i++)
            {
                float t = (i + 0.5f) / 5f;
                var root = Vector3.Lerp(V(0.1f * s, 0.63f, 0), V(0.5f * s, 0.82f, -0.09f), t);
                var veil = Smooth(2, root, root + V(0.02f * s, -0.15f, -0.06f), root + V(0.04f * s, -0.3f - 0.06f * t, -0.08f));
                b.Tube(wing, veil, 0.03f, 0.008f, i % 2 == 0 ? violet : red, Glow, 0.006f);
            }
        });
        // The crown of black feathers
        var c = V(0, 0.73f, 0.17f);
        for (int i = -2; i <= 2; i++)
            b.Spike(head, c + V(i * 0.015f, 0.03f, -0.02f), c + V(i * 0.04f, 0.13f - MathF.Abs(i) * 0.02f, -0.05f), 0.014f, black, 0.5f);
        // The train of smoke with embers
        var tp = Smooth(3, V(0, 0.5f, -0.14f), V(0, 0.36f, -0.3f), V(0.04f, 0.2f, -0.4f), V(0.1f, 0.1f, -0.5f), V(0.2f, 0.06f, -0.56f));
        Ribbon(b, tail, tp, 0.09f, black, V(0, 1f, 0.2f), 0.016f);
        for (int i = 2; i < tp.Length; i += 2)
            b.PaintEll(tail, tp[i] + V(0.012f * (i % 4 == 0 ? 1 : -1), 0.01f, 0), V(0.01f, 0.01f, 0.01f), i % 4 == 0 ? red : violet, soft: 0.003f);
        Stretch(b, V(1.06f, 1.06f, 1.06f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 6059 Obsidhound, from Hisuian Arcanine

    /// <summary>
    /// Obsidhound: Hisuian Arcanine larger, its mane grown long and red over a cream ruff, its horn of rock grown into a
    /// helmet of black obsidian over its brow and eyes with spikes rising from it, plates of obsidian running down its
    /// back with flames licking between them, a tail of smoke and embers and claws of black glass.
    /// </summary>
    private static PokeBuilder Obsidhound()
    {
        var b = Become(ArcanineHisui(), "Obsidhound", 1f);
        const float Lift = 0.09f;
        var red = Rgb(170, 66, 44);
        var mane = Rgb(150, 50, 40);
        var cream = Rgb(236, 216, 180);
        var obsidian = Rgb(36, 34, 44);
        var flame = Rgb(250, 140, 50);
        int head = BoneOf(b, "head");
        b.Sdf.RemoveAll(s => s.Bone == head && Is(s.Color, Rgb(118, 118, 124)));
        Recolour(b, (Rgb(208, 74, 46), red), (Rgb(86, 88, 94), mane), (Rgb(70, 70, 78), red), (Rgb(40, 38, 48), PixelCanvas.Mix(red, Black, 0.35f)));
        var mc = V(0, 0.5f + Lift, 0.16f);
        b.PaintEll(Body, mc + V(0, -0.08f, 0.1f), V(0.12f, 0.12f, 0.08f), cream);
        var c = V(0, 0.64f + Lift, 0.25f);
        var r = V(0.115f, 0.1f, 0.1f);
        // The helmet of obsidian over its brow and eyes, spiked
        b.Ell(head, c + V(0, 0.04f, 0.02f), V(0.12f, 0.06f, 0.1f), obsidian, V(15f, 0, 0), Shell, 0.008f);
        b.Spike(head, c + V(0, 0.06f, 0.04f), c + V(0, 0.2f, -0.02f), 0.035f, obsidian, 0.5f, Shell);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, c + V(0.07f * s, 0.05f, 0.0f), c + V(0.11f * s, 0.17f, -0.04f), 0.03f, obsidian, 0.5f, Shell);
            PaintAs(b, head, c + V(0.04f * s, 0.07f, 0.09f), V(0.008f, 0.02f, 0.01f), flame, Glow, V(0, 0, 25f * s));
        });
        // Plates of obsidian down its back, flames between them
        var bc = V(0, 0.38f + Lift, -0.02f);
        for (int i = 0; i < 6; i++)
        {
            float z = 0.14f - i * 0.065f;
            PokeBuilder.Both(s =>
            {
                var root = bc + V(0.05f * s, 0.1f - i * 0.004f, z);
                Blade(b, Body, root, root + V(0.05f * s, 0.1f - i * 0.008f, -0.06f), 0.05f, obsidian, V(s, 0.3f, 0), 0.3f, Shell);
            });
            if (i % 2 == 0)
                FlameTongue(b, Body, bc + V(0, 0.11f, z - 0.03f), bc + V(0, 0.2f, z - 0.08f), 0.02f, Rgb(240, 90, 40), Rgb(255, 220, 110));
        }
        // Claws of black glass
        foreach (var leg in b.Model.Bones.Select((bn, i) => (bn, i)).Where(x => x.bn.Role == PokeRole.Leg).Select(x => x.i).ToList())
        {
            var j = b.Model.Skeleton[leg].Joint;
            Claws(b, leg, V(j.X * 1.07f, 0.02f, j.Z + 0.09f), V(0.02f, 0, 0), V(0, -0.3f, 1f), 0.04f, 0.009f);
            Tint(b, sh => sh.Bone == leg && sh.Shape == SdfShape.RoundCone && Is(sh.Color, Claw), obsidian);
        }
        // Embers in the tail
        int tail = BoneOf(b, "tail");
        var tc = V(0, 0.55f + Lift, -0.39f);
        Tint(b, sh => sh.Bone == tail, Rgb(110, 50, 40));
        foreach (var o in new[] { V(0.04f, 0.05f, -0.02f), V(-0.05f, 0.0f, -0.04f), V(0.02f, 0.12f, -0.05f) })
            PaintAs(b, tail, tc + o, V(0.015f, 0.02f, 0.015f), flame, Glow);
        Stretch(b, V(1.06f, 1.06f, 1.06f));
        return b;
    }

    // ------------------------------------------------------------------ 6101 Apricharge, from Hisuian Electrode

    /// <summary>
    /// Apricharge: Hisuian Electrode as a ripe Apricorn, red above and cream below, its shell split into segments with
    /// sparks glowing yellow in the gaps, a band of gnarled wood round its middle rising into a stalk with two leaves on
    /// its crown, root-like feet beneath, and its grin and eyes glowing yellow.
    /// </summary>
    private static PokeBuilder Apricharge()
    {
        var b = Become(ElectrodeHisui(), "Apricharge", 0.85f);
        var red = Rgb(200, 84, 54);
        var cream = Rgb(236, 230, 210);
        var wood = Rgb(96, 74, 56);
        var spark = Rgb(250, 230, 90);
        var green = Rgb(90, 160, 70);
        b.Sdf.RemoveAll(s => s.Op == SdfOp.Paint && (Is(s.Color, PixelCanvas.Mix(Rgb(228, 200, 152), Rgb(120, 80, 40), 0.22f))));
        Recolour(b, (Rgb(228, 200, 152), red), (Rgb(214, 92, 42), cream), (Rgb(120, 30, 30), Rgb(250, 210, 80)));
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
        {
            d.Pupil = Rgb(250, 230, 90);
            d.White = Rgb(250, 240, 160);
        }
        var c = V(0, 0.3f, 0);
        float rad = 0.3f;
        // The band of gnarled wood round its middle
        b.Torus(Body, c + V(0, -0.02f, 0), rad * 0.98f, 0.035f, wood, mat: Shell, blend: 0.01f);
        for (int i = 0; i < 10; i++)
        {
            float a = i * MathF.Tau / 10f;
            b.Ell(Body, c + V(MathF.Sin(a) * rad, -0.03f + 0.01f * (i % 2), MathF.Cos(a) * rad), V(0.04f, 0.03f, 0.04f), wood, mat: Shell, blend: 0.01f);
        }
        // The segments' seams, glowing with sparks
        foreach (float a in new[] { -0.6f, 0.9f, 2.4f, 3.9f, 5.4f })
        {
            var n = V(MathF.Sin(a), 0, MathF.Cos(a));
            PaintAs(b, Body, c + n * rad, V(0.008f, rad * 1.02f, 0.008f), spark, Glow, V(0, a / Degree, 0));
        }
        // The stalk rising from the crown with two leaves
        var top = c + V(0, rad, 0);
        b.Tube(Body, Smooth(2, top - V(0, 0.03f, 0), top + V(0.02f, 0.06f, 0), top + V(-0.01f, 0.13f, 0.0f)), 0.03f, 0.012f, wood, Shell, 0.01f);
        foreach (var (x, z) in new[] { (-0.07f, 0.05f), (0.07f, 0.05f), (0f, -0.08f), (0.06f, -0.05f) })
            b.Tube(Body, Smooth(2, top + V(x * 0.2f, -0.01f, z * 0.2f), top + V(x * 0.6f, -0.03f, z * 0.6f), top + V(x, -0.08f, z)), 0.012f, 0.006f, wood, Shell, 0.006f);
        PokeBuilder.Both(s => Frond(b, Body, top + V(0, 0.1f, 0), top + V(0.13f * s, 0.14f, 0.0f), 0.05f, green, V(0, 1f, 0.2f), 0.2f, Leaf));
        // Root-like feet
        foreach (float a in new[] { 0.6f, 2.5f, 4.4f })
            b.Tube(Body, Smooth(2, c + V(MathF.Sin(a) * 0.18f, -0.22f, MathF.Cos(a) * 0.18f), c + V(MathF.Sin(a) * 0.22f, -0.28f, MathF.Cos(a) * 0.22f), V(MathF.Sin(a) * 0.26f, 0.01f, MathF.Cos(a) * 0.26f)), 0.025f, 0.012f, wood, Shell, 0.008f);
        return b;
    }

    // ------------------------------------------------------------------ 6549 Fandangleaf, from Hisuian Lilligant

    /// <summary>
    /// Fandangleaf: Hisuian Lilligant taller, its leaf-arms grown into two fans of pleated leaf it opens with a snap,
    /// green edged with a flower of orange and white, a skirt of white leaves over green that flares as it spins, and
    /// the flower on its head in full bloom, orange in two rings of petals; its hair hangs in green and gold tresses.
    /// </summary>
    private static PokeBuilder Fandangleaf()
    {
        var b = Become(LilligantHisui(), "Fandangleaf", 0.95f);
        var green = Rgb(110, 170, 90);
        var leaf = Rgb(60, 150, 70);
        var white = Rgb(244, 246, 238);
        var orange = Rgb(236, 110, 50);
        var gold = Rgb(236, 200, 80);
        int head = BoneOf(b, "head");
        float neck = 0.49f;
        // The fans
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            b.Sdf.RemoveAll(sh => sh.Bone == arm && sh.Shape != SdfShape.RoundCone);
            var shoulder = V(0.04f * s, neck - 0.02f, 0);
            var hand = shoulder + V(0.18f * s, s > 0 ? 0.12f : -0.08f, 0.04f);
            b.Limb(arm, Vector3.Lerp(shoulder, hand, 0.3f), hand, 0.01f, 0.009f, green);
            var up = s > 0 ? V(0.4f * s, 1f, 0) : V(1f * s, -0.2f, 0);
            var face = V(0, 0, 1f);
            for (int i = 0; i < 9; i++)
            {
                float a = (i / 8f - 0.5f) * 2.2f;
                var u = Vector3.Normalize(up);
                var side = Vector3.Normalize(Vector3.Cross(face, u));
                var dir = u * MathF.Cos(a) + side * MathF.Sin(a);
                Blade(b, arm, hand, hand + dir * 0.14f, 0.02f, i % 2 == 0 ? leaf : green, face, 0.15f, Leaf);
                b.PaintEll(arm, hand + dir * 0.09f, V(0.012f, 0.02f, 0.012f), i % 3 == 0 ? orange : white, Euler(dir), 0.005f);
            }
        });
        // The flaring skirt of white leaves over green
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f;
            var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
            PointedLeaf(b, Body, V(0, 0.4f, 0) + dir * 0.03f, V(0, 0.33f, 0) + dir * 0.14f, 0.05f, white, dir + V(0, 0.6f, 0), 0.14f);
            PointedLeaf(b, Body, V(0, 0.37f, 0) + dir * 0.03f, V(0, 0.28f, 0) + dir * 0.13f, 0.04f, leaf, dir + V(0, 0.6f, 0), 0.14f);
        }
        // The full bloom on its head
        var c = V(0, neck + 0.05f, 0.02f);
        var fc = c + V(0.02f, 0.06f, -0.01f);
        b.Sdf.RemoveAll(sh => sh.Bone == head && (Is(sh.Color, Rgb(240, 160, 190)) || Is(sh.Color, Rgb(250, 220, 80))));
        foreach (var (ring, n, k) in new[] { (0, 7, 1.0f), (1, 6, 0.75f) })
            for (int i = 0; i < n; i++)
            {
                float a = i * MathF.Tau / n + ring * 0.4f;
                var dir = V(MathF.Cos(a), 0.6f + ring * 0.4f, MathF.Sin(a));
                b.Ell(head, fc + V(0, 0.01f * ring, 0) + dir * 0.04f * k, V(0.03f, 0.05f, 0.022f) * k, ring == 0 ? orange : Rgb(244, 150, 60), Euler(dir), blend: 0.012f);
            }
        b.Ell(head, fc + V(0, 0.03f, 0), V(0.025f, 0.02f, 0.025f), Rgb(220, 80, 40), blend: 0.01f);
        // Tresses of green and gold
        PokeBuilder.Both(s =>
        {
            foreach (var (dz, len) in new[] { (0.0f, 0.12f), (-0.02f, 0.15f) })
                b.Tube(head, Smooth(2, c + V(0.035f * s, 0.02f, dz), c + V(0.07f * s, -0.04f, dz - 0.01f), c + V(0.09f * s, -len, dz)), 0.01f, 0.004f, dz == 0f ? green : gold, Leaf, 0.004f);
        });
        Stretch(b, V(1f, 1.1f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 6571 Spectrevix, from Hisuian Zoroark

    /// <summary>
    /// Spectrevix: Hisuian Zoroark taller, red streaks over its white fur and down its forearms and shins, red eyes,
    /// its mane grown into a great mane of white fire tipped with glowing pale red floating above its shoulders, and a
    /// second ghostly tail of pale glow that fades at its end beside its own.
    /// </summary>
    private static PokeBuilder Spectrevix()
    {
        var b = Become(ZoroarkHisui(), "Spectrevix", 1f);
        var white = Rgb(236, 234, 240);
        var red = Rgb(176, 40, 56);
        var glow = Rgb(250, 196, 204);
        var pale = Rgb(230, 230, 244);
        int tail = BoneOf(b, "tail");
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
            d.Iris = Rgb(230, 50, 60);
        Recolour(b, (Rgb(160, 150, 160), red), (Rgb(220, 70, 80), Rgb(250, 160, 170)));
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            b.PaintEll(arm, V(0.17f * s, 0.4f, 0.11f), V(0.03f, 0.06f, 0.03f), red);
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            b.PaintEll(leg, V(0.08f * s, 0.12f, 0.0f), V(0.03f, 0.06f, 0.04f), red);
            b.PaintEll(Body, V(0.06f * s, 0.46f, 0.05f), V(0.015f, 0.05f, 0.02f), red, V(0, 0, 20f * s), 0.006f);
        });
        // The mane of white fire rising over its shoulders
        for (int i = 0; i < 12; i++)
        {
            float a = (i / 11f - 0.5f) * 3f;
            var root = V(MathF.Sin(a) * 0.1f, 0.72f, -0.06f + MathF.Cos(a) * 0.02f);
            var tip = root + V(MathF.Sin(a) * 0.12f, 0.16f + 0.06f * MathF.Cos(a), -0.14f);
            FlameTongue(b, tail, root, tip, 0.035f, glow, white);
        }
        // The ghostly second tail beside its own
        var gt = Smooth(3, V(-0.05f, 0.36f, -0.1f), V(-0.12f, 0.24f, -0.24f), V(-0.2f, 0.26f, -0.38f), V(-0.24f, 0.4f, -0.46f), V(-0.2f, 0.52f, -0.44f));
        b.Tube(tail, gt, 0.035f, 0.01f, pale, Glow, 0.01f);
        foreach (int i in new[] { 4, 7, 10 })
            FurTufts(b, tail, gt[i], V(0.02f, 0.02f, 0.02f), 4, 0.03f, 0.008f, pale, -1f, -1f, 0f);
        Stretch(b, V(0.98f, 1.1f, 0.98f));
        return b;
    }

    // ------------------------------------------------------------------ 6628 Augurhawk, from Hisuian Braviary

    /// <summary>
    /// Augurhawk: Hisuian Braviary larger and red, white across its breast and the coverts of its wings, its wing and
    /// tail feathers tipped with glowing pink, a crest of long red plumes streaming back tipped pink, a pale ring round
    /// each eye, and pink signs of light drifting in an arc beside it.
    /// </summary>
    private static PokeBuilder Augurhawk()
    {
        var b = Become(BraviaryHisui(), "Augurhawk", 1f);
        Unlift(b, V(0, 0.42f, 0));
        var red = Rgb(170, 30, 50);
        var white = Rgb(242, 238, 244);
        var pink = Rgb(250, 140, 200);
        int head = BoneOf(b, "head");
        Recolour(b, (Rgb(58, 54, 64), red), (Rgb(64, 60, 70), red), (Rgb(36, 34, 42), pink), (Rgb(238, 236, 242), white));
        Make(b, s => Is(s.Color, pink), Glow);
        var bc = V(0, 0.42f, 0);
        b.PaintEll(Body, bc + V(0, 0.02f, 0.08f), V(0.08f, 0.1f, 0.06f), white);
        var c = bc + V(0, 0.17f, 0.17f);
        // The crest: long red plumes, tipped pink
        for (int i = 0; i < 5; i++)
        {
            float x = (i - 2) * 0.02f;
            var root = c + V(x, 0.05f, -0.02f);
            var end = c + V(x * 2.2f, 0.13f + 0.02f * (2 - MathF.Abs(i - 2)), -0.22f);
            Frond(b, head, root, end, 0.022f, red, V(0, 1f, 0.2f), 0.25f);
            PaintAs(b, head, Vector3.Lerp(root, end, 0.85f), V(0.02f, 0.02f, 0.03f), pink, Glow);
        }
        PokeBuilder.Both(s => b.PaintTorus(head, On(c, V(0.06f, 0.06f, 0.065f), 0.035f * s, c.Y + 0.01f), 0.014f, 0.003f, white, Euler(V(0.4f * s, 0, 1f))));
        // Signs of light drifting in an arc beside it
        var arc = Enumerable.Range(0, 9).Select(i => { float a = 1.0f + i * 0.28f; return bc + V(MathF.Cos(a) * 0.36f, -0.05f + 0.03f * MathF.Sin(i * 1.3f), MathF.Sin(a) * 0.2f - 0.1f); }).ToArray();
        b.Tube(Body, arc, 0.004f, 0.004f, pink, Glow, 0.002f);
        for (int i = 1; i < arc.Length; i += 2)
            b.Ell(Body, arc[i] + V(0, 0.012f, 0), V(0.01f, 0.008f, 0.004f), pink, V(0, 0, 20f * i), Glow, 0.002f);
        Stretch(b, V(1.06f, 1.06f, 1.06f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 6713 Bergbreaker, from Hisuian Avalugg

    /// <summary>
    /// Bergbreaker: Hisuian Avalugg its body grey rock in layers, its jaw grown into a ram of rock and ice jutting
    /// forward like a ship's bow, a brow of rock over its glowing eyes, the ice of its back in great tilted slabs, and
    /// little Bergmite riding in the cracks between them.
    /// </summary>
    private static PokeBuilder Bergbreaker()
    {
        var b = Become(AvaluggHisui(), "Bergbreaker", 1f);
        var rock = Rgb(106, 110, 116);
        var ice = Rgb(206, 228, 244);
        var pale = Rgb(230, 242, 252);
        int head = BoneOf(b, "head");
        b.Sdf.RemoveAll(s => s.Bone == Body && s.Shape != SdfShape.Ellipsoid && Is(s.Color, Rgb(214, 232, 248)));
        b.Sdf.RemoveAll(s => Is(s.Color, Rgb(150, 130, 196)));
        Recolour(b, (Rgb(100, 84, 76), rock));
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
            d.Iris = Rgb(120, 220, 250);
        // The great tilted slabs of ice over its back
        foreach (var (x, z, h, roll, len) in new[] { (0f, -0.02f, 0.2f, 0f, 0.22f), (-0.12f, 0.04f, 0.14f, 25f, 0.16f), (0.13f, 0.0f, 0.16f, -25f, 0.18f), (0.04f, -0.16f, 0.15f, -10f, 0.16f), (-0.1f, -0.14f, 0.12f, 20f, 0.14f) })
        {
            var at = V(x, 0.34f, z);
            b.Box(Body, at + V(0, h * 0.5f, 0), V(0.04f, h * 0.6f, len * 0.5f), 0.02f, ice, V(-20f, 0, roll), Shell, 0.02f);
        }
        b.Ell(Body, V(0, 0.32f, -0.02f), V(0.21f, 0.06f, 0.24f), ice, mat: Shell, blend: 0.02f);
        // Bergmite riding in the cracks
        foreach (var at in new[] { V(-0.06f, 0.385f, 0.06f), V(0.08f, 0.385f, -0.08f), V(-0.04f, 0.385f, -0.18f) })
        {
            b.Ell(Body, at, V(0.022f, 0.02f, 0.022f), pale, mat: Shell, blend: 0.006f);
            b.PaintEll(Body, at + V(0, 0.004f, 0.02f), V(0.012f, 0.004f, 0.006f), Rgb(80, 110, 150), soft: 0.003f);
        }
        // The ram of a jaw: rock and ice jutting forward like a bow
        var c = V(0, 0.2f, 0.25f);
        b.Ell(head, c + V(0, 0.052f, -0.008f), V(0.09f, 0.022f, 0.055f), rock, mat: Shell, blend: 0.006f);
        b.Box(head, c + V(0, -0.055f, 0.1f), V(0.08f, 0.03f, 0.08f), 0.015f, ice, V(10f, 0, 0), Shell, 0.008f);
        b.Spike(head, c + V(0, -0.055f, 0.14f), c + V(0, -0.06f, 0.26f), 0.07f, ice, 0.5f, Shell, 0.01f);
        PokeBuilder.Both(s => b.Ell(head, c + V(0.09f * s, -0.01f, 0.07f), V(0.035f, 0.06f, 0.07f), rock, mat: Shell, blend: 0.01f));
        // Layers of rock on the legs
        foreach (var leg in b.Model.Bones.Select((bn, i) => (bn, i)).Where(x => x.bn.Role == PokeRole.Leg).Select(x => x.i).ToList())
        {
            var j = b.Model.Skeleton[leg].Joint;
            foreach (float y in new[] { 0.09f, 0.16f })
                b.PaintTorus(leg, V(j.X * 1.07f, y, j.Z), 0.058f, 0.006f, PixelCanvas.Mix(rock, Black, 0.25f), default);
        }
        Stretch(b, V(1.05f, 1.08f, 1.05f));
        return b;
    }

    // ------------------------------------------------------------------ 2892 Ursensei, from Urshifu

    /// <summary>
    /// Ursensei: Urshifu taller and heavier, an old master in a full robe of pale cloth tied with a black belt knotted
    /// twice, its mane long and grey with a grey beard, its fists wrapped in bandages, and both scrolls of the towers
    /// worn on it: the Darkness's slung across its back and the Waters' at its hip.
    /// </summary>
    private static PokeBuilder Ursensei()
    {
        var b = Become(Urshifu(), "Ursensei", 0.97f);
        var body = Rgb(64, 64, 72);
        var gi = Rgb(232, 224, 200);
        var black = Rgb(36, 36, 44);
        var grey = Rgb(170, 170, 176);
        var wrap = Rgb(220, 210, 184);
        var scroll = Rgb(150, 136, 100);
        int head = BoneOf(b, "head");
        // The gi over its body: the dark robe's tatters turn pale cloth
        Recolour(b, (Rgb(36, 36, 44), gi), (Rgb(150, 150, 160), grey), (Rgb(238, 238, 242), gi));
        var bc = V(0, 0.5f, 0);
        b.Ell(Body, bc + V(0, 0.0f, 0), V(0.18f, 0.21f, 0.14f), gi, mat: Fur, blend: 0.01f);
        b.PaintEll(Body, bc + V(0.02f, 0.06f, 0.13f), V(0.012f, 0.14f, 0.03f), PixelCanvas.Mix(gi, Black, 0.2f), V(0, 0, -30f), 0.004f);
        // Trousers of the gi on its legs
        PokeBuilder.Both(s =>
        {
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            b.Limb(leg, V(0.1f * s, 0.3f, 0), V(0.11f * s, 0.08f, 0.02f), 0.075f, 0.065f, gi, Fur, 0.01f);
            // Wrapped fists
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var elbow = bc + V(0.23f * s, -0.02f, 0.06f);
            var fist = bc + V(0.12f * s, 0.05f, 0.2f);
            b.Limb(arm, bc + V(0.15f * s, 0.12f, 0.0f), elbow, 0.065f, 0.058f, gi, Fur, 0.008f);
            for (int i = 0; i < 4; i++)
                b.Torus(arm, Vector3.Lerp(elbow, fist, 0.25f + 0.2f * i), 0.05f, 0.01f, wrap, Euler(fist - elbow) + V(10f * i, 0, 0), mat: Fur, blend: 0.004f);
        });
        // The black belt, knotted, its ends hanging
        b.Torus(Body, bc + V(0, -0.08f, 0), 0.165f, 0.018f, black, V(0, 0, 0), 1f, 0.8f, Fur, 0.004f);
        var knot = bc + V(0, -0.08f, 0.13f);
        b.Ell(Body, knot, V(0.03f, 0.022f, 0.02f), black, mat: Fur, blend: 0.004f);
        foreach (float x in new[] { -0.015f, 0.015f })
            b.Box(Body, knot + V(x, -0.08f, 0.005f), V(0.012f, 0.07f, 0.005f), 0.003f, black, V(0, 0, x * 400f), Fur, 0.003f);
        // The long grey mane and beard
        var c = bc + V(0, 0.29f, 0.06f);
        b.Tube(head, Smooth(3, c + V(0, 0.08f, -0.02f), c + V(0, 0.1f, -0.12f), c + V(0, 0.0f, -0.18f), c + V(0, -0.12f, -0.2f)), 0.05f, 0.02f, grey, blend: 0.01f);
        FurTufts(b, head, c + V(0, -0.08f, 0.06f), V(0.06f, 0.05f, 0.04f), 12, 0.06f, 0.018f, grey, 2f, -1f, 1f);
        // The scrolls: across its back and at its hip
        var s0 = bc + V(-0.12f, 0.16f, -0.15f);
        var s1 = bc + V(0.14f, -0.12f, -0.14f);
        b.Limb(Body, s0, s1, 0.035f, 0.035f, scroll, Shell, 0.006f);
        PokeBuilder.Both(s => b.Torus(Body, Vector3.Lerp(s0, s1, s < 0 ? 0.08f : 0.92f), 0.036f, 0.006f, Rgb(70, 54, 40), Euler(s1 - s0), mat: Shell, blend: 0.002f));
        b.Limb(Body, bc + V(0.17f, -0.1f, 0.06f), bc + V(0.2f, -0.12f, -0.1f), 0.032f, 0.032f, Rgb(110, 130, 150), Shell, 0.006f);
        b.Tube(Body, Smooth(2, bc + V(-0.14f, 0.16f, 0.06f), bc + V(0.02f, 0.06f, 0.15f), bc + V(0.17f, -0.1f, 0.07f)), 0.006f, 0.006f, black, Fur, 0.003f);
        Stretch(b, V(1.06f, 1.1f, 1.06f));
        return b;
    }
}
