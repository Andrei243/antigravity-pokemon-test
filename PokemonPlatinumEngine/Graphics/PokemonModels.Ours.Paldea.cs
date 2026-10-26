using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// The species of our own that evolve past level 100 (plan 03 · D14), batch 9 and its supplement: Paldea's, in our
// numbers' order (2000 + the forebear's National number). Each is sculpted from its forebear's own sculpt as the look
// of docs/plans/D14-proposals-paldea.md (and -paldea-supplement.md) and the user's reference pictures
// (docs/art/reference/gemini/paldea) say: the forebear's build is taken whole (renamed, stretched, its colours changed,
// a part taken away) and the picture's new features are added to it, with the tools of the earlier batches
// (PokemonModels.Ours.Kanto.cs, .Unova.cs). The forebears' own sculpts are never touched.
internal static partial class PokemonModels
{
    // ------------------------------------------------------------------ tools of this batch

    /// <summary>A ring of <paramref name="count"/> chain links round <paramref name="center"/> about <paramref name="axis"/> (a collar, a mantle's edge).</summary>
    private static void LinkRing(PokeBuilder b, int bone, Vector3 center, Vector3 axis, float radius, int count, float link, float tube, Color color)
    {
        var n = Vector3.Normalize(axis);
        var u = Vector3.Normalize(Vector3.Cross(n, MathF.Abs(n.Y) > 0.9f ? V(1f, 0, 0) : V(0, 1f, 0)));
        var v = Vector3.Cross(n, u);
        for (int i = 0; i < count; i++)
        {
            float a = i * MathF.Tau / count;
            var p = center + (u * MathF.Cos(a) + v * MathF.Sin(a)) * radius;
            var along = -u * MathF.Sin(a) + v * MathF.Cos(a);
            var face = i % 2 == 0 ? n : Vector3.Normalize(Vector3.Cross(along, n));
            b.Torus(bone, p, link, tube, color, Euler(face), 1f, 1.5f, Metal, 0.004f);
        }
    }

    /// <summary>A little mushroom standing on <paramref name="at"/>: a stalk and a cap.</summary>
    private static void Toadstool(PokeBuilder b, int bone, Vector3 at, Vector3 up, float h, Color stalk, Color cap)
    {
        var u = Vector3.Normalize(up);
        b.Limb(bone, at - u * h * 0.2f, at + u * h, h * 0.32f, h * 0.28f, stalk, blend: 0.004f);
        b.Ell(bone, at + u * h, V(h * 0.55f, h * 0.28f, h * 0.55f), cap, Euler(u), blend: 0.004f);
    }

    // ------------------------------------------------------------------ 2916 Trufflord, from Oinkologne

    /// <summary>
    /// Trufflord: Oinkologne heavier, pale grey (the female a paler brown), its snout broadened into a rooting spade
    /// crusted with earth, two ochre tusks, a wig of tight grey curls over its head and shoulders and a band of dark
    /// bristle round its middle.
    /// </summary>
    /// <param name="female">A female keeps Oinkologne's female look: brown, a paler face, pink tufts to her tail.</param>
    private static PokeBuilder Trufflord(bool female = false)
    {
        var b = Become(OinkologneBuild(female), "Trufflord", 0.86f);
        var bc = V(0, 0.3f, -0.02f);
        var c = bc + V(0, 0.07f, 0.22f);
        var r = V(0.1f, 0.09f, 0.085f);
        int head = BoneOf(b, "head");
        var curls = female ? Rgb(170, 132, 112) : Rgb(150, 150, 146);
        var bristle = female ? Rgb(84, 56, 46) : Rgb(70, 64, 58);
        if (female)
            Recolour(b, (Rgb(120, 84, 68), Rgb(150, 112, 92)), (Rgb(176, 124, 100), Rgb(200, 158, 132)));
        else
            Recolour(b, (Rgb(88, 82, 102), Rgb(150, 146, 132)), (Rgb(58, 52, 72), Rgb(120, 118, 112)));
        var pink = Rgb(236, 140, 156);
        // The rooting spade: the snout broadened and flattened at its end, crusted brown with earth
        b.Ell(head, c + V(0, -0.025f, 0.09f), V(0.058f, 0.04f, 0.04f), pink, blend: 0.012f);
        b.Box(head, c + V(0, -0.03f, 0.125f), V(0.058f, 0.032f, 0.012f), 0.012f, pink, blend: 0.008f);
        PokeBuilder.Both(s => b.PaintEll(head, c + V(0.022f * s, -0.03f, 0.138f), V(0.01f, 0.014f, 0.006f), Rgb(150, 70, 90), soft: 0.003f));
        b.PaintEll(head, c + V(0.03f, -0.055f, 0.13f), V(0.04f, 0.02f, 0.02f), Rgb(140, 104, 70), soft: 0.008f);
        // Two short tusks of pale ochre curling up beside the spade
        PokeBuilder.Both(s => b.Spike(head, c + V(0.05f * s, -0.045f, 0.095f), c + V(0.075f * s, 0.005f, 0.13f), 0.013f, Rgb(226, 198, 132), mat: Shell, blend: 0.004f));
        // A wig of tight curls over the crown and down over the shoulders, leaving the face clear
        float golden = MathF.PI * (3f - MathF.Sqrt(5f));
        for (int i = 0; i < 46; i++)
        {
            float y = 1f - 2f * (i + 0.5f) / 46f;
            if (y < 0.1f) continue;
            float ring = MathF.Sqrt(1f - y * y), a = i * golden;
            var n = V(MathF.Sin(a) * ring, y, MathF.Cos(a) * ring);
            if (n.Z > 0.55f && y < 0.75f) continue;
            var at = c + V(0, 0.01f, -0.03f) + n * V(0.105f, 0.1f, 0.1f);
            b.Ell(head, at, V(0.026f, 0.024f, 0.026f), curls, blend: 0.01f);
            b.PaintTorus(head, at + n * 0.012f, 0.012f, 0.004f, PixelCanvas.Shadow(curls, 0.25f), Euler(n));
        }
        for (int i = 0; i < 40; i++)
        {
            float y = 1f - 2f * (i + 0.5f) / 40f;
            if (y < 0.15f) continue;
            float ring = MathF.Sqrt(1f - y * y), a = i * golden + 0.4f;
            var n = V(MathF.Sin(a) * ring, y, MathF.Cos(a) * ring);
            if (n.Z < -0.2f) continue;
            var at = bc + V(0, 0.02f, 0.1f) + n * V(0.15f, 0.13f, 0.12f);
            b.Ell(Body, at, V(0.03f, 0.026f, 0.03f), curls, blend: 0.012f);
        }
        // A band of dark bristle round its middle
        b.PaintTorus(Body, bc + V(0, 0, -0.05f), 0.15f, 0.05f, bristle, V(90f, 0, 0), 1f, 0.95f);
        for (int i = 0; i < 9; i++)
        {
            float a = (-80f + i * 20f) * Degree;
            var n = V(MathF.Sin(a), MathF.Cos(a), 0);
            var root = bc + V(0, 0, -0.05f) + n * V(0.145f, 0.135f, 0);
            b.Spike(Body, root, root + n * 0.03f + V(0, 0, -0.01f), 0.014f, bristle, 0.5f, blend: 0.006f);
        }
        Stretch(b, V(1.08f, 1.05f, 1.06f));
        return b;
    }

    // ------------------------------------------------------------------ 2918 Fortarach, from Spidops

    /// <summary>
    /// Fortarach: Spidops grown into a gatehouse: its body a great armoured abdomen of steel plates bristling with
    /// spikes strung with silver silk, four legs a side tipped with grappling hooks, and a portcullis of steel bars
    /// over its face where its folded legs were.
    /// </summary>
    private static PokeBuilder Fortarach()
    {
        var b = Become(Spidops(), "Fortarach", 0.96f);
        var bc = V(0, 0.44f, -0.02f);
        var c = bc + V(0, 0.15f, 0.04f);
        var steel = Rgb(118, 124, 132);
        var plate = Rgb(150, 156, 164);
        var green = Rgb(54, 96, 70);
        var silk = Rgb(222, 228, 240);
        // The thread held in front goes: the bars take its place
        b.Sdf.RemoveAll(s => Is(s.Color, Rgb(244, 244, 240)));
        Recolour(b, (Rgb(100, 130, 62), green), (Rgb(140, 150, 80), Rgb(96, 104, 92)), (Rgb(200, 172, 112), Rgb(150, 120, 80)));
        Make(b, s => s.Op == SdfOp.Add, Metal);
        // The great abdomen behind, plated and spiked, silk strung between its spikes
        var ac = bc + V(0, -0.01f, -0.22f);
        var ar = V(0.16f, 0.13f, 0.19f);
        b.Ell(Body, ac, ar, steel, mat: Metal, blend: 0.03f);
        for (int i = 0; i < 5; i++)
            b.PaintTorus(Body, ac + V(0, 0, 0.14f - i * 0.07f), MathF.Sqrt(1f - MathF.Pow((0.14f - i * 0.07f) / ar.Z, 2)) * 0.15f, 0.007f, Rgb(70, 74, 82), V(90f, 0, 0));
        b.PaintEll(Body, ac + V(0, 0.08f, 0), V(0.1f, 0.06f, 0.17f), plate);
        var tips = new System.Collections.Generic.List<Vector3>();
        foreach (float x in new[] { -0.08f, 0.08f })
            for (int i = 0; i < 4; i++)
            {
                float z = 0.11f - i * 0.075f;
                var n = Vector3.Normalize(V(x / ar.X * 0.6f, 1f, z / ar.Z * 0.5f));
                var root = Out(ac, ar, default, V(x / ar.X, 0.9f, z / ar.Z));
                var tip = root + n * 0.11f;
                b.Spike(Body, root - n * 0.01f, tip, 0.022f, Rgb(84, 88, 96), mat: Metal);
                tips.Add(tip - n * 0.012f);
            }
        for (int i = 0; i < 4; i++)
        {
            b.Limb(Body, tips[i], tips[i + 4], 0.005f, 0.005f, silk, Glow, 0.003f);
            if (i < 3)
            {
                b.Limb(Body, tips[i], tips[i + 5], 0.005f, 0.005f, silk, Glow, 0.003f);
                b.Limb(Body, tips[i + 4], tips[i + 1], 0.005f, 0.005f, silk, Glow, 0.003f);
            }
        }
        // Two more legs a side, from under the abdomen, all four tipped with grappling hooks
        foreach (var z in new[] { -0.1f, -0.18f })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, bc + V(0.08f * s, -0.04f, z), false);
                var knee = bc + V(0.22f * s, 0.1f, z * 1.4f);
                var foot = V(0.31f * s, 0.02f, bc.Z + z * 2.1f);
                b.Limb(leg, bc + V(0.08f * s, -0.04f, z), knee, 0.022f, 0.018f, green, Metal);
                b.Ell(leg, knee, V(0.024f, 0.024f, 0.024f), Rgb(150, 120, 80), mat: Metal);
                b.Limb(leg, knee, foot, 0.018f, 0.013f, green, Metal);
            });
        foreach (var leg in Enumerable.Range(0, b.Model.Bones.Count).Where(i => HasRole(b, i, PokeRole.Leg)).ToArray())
        {
            var shapes = b.Sdf.Shapes.Where(s => s.Bone == leg && s.Op == SdfOp.Add && s.Shape == SdfShape.RoundCone).ToList();
            var low = shapes.Select(s => s.A.Y < s.B.Y ? s.A : s.B).OrderBy(p => p.Y).First();
            float side = MathF.Sign(low.X);
            var foot = V(low.X, 0.024f, low.Z);
            b.Ell(leg, foot, V(0.016f, 0.016f, 0.016f), Rgb(84, 88, 96), mat: Metal, blend: 0.006f);
            b.Spike(leg, foot, foot + V(0.045f * side, 0.035f, 0.01f), 0.01f, Rgb(84, 88, 96), mat: Metal, blend: 0.004f);
            b.Spike(leg, foot, foot + V(-0.03f * side, 0.035f, 0.02f), 0.01f, Rgb(84, 88, 96), mat: Metal, blend: 0.004f);
        }
        // The portcullis over its face: steel bars from its crown down past its jaw, two cross bars
        int head = BoneOf(b, "head");
        foreach (float x in new[] { -0.045f, -0.022f, 0f, 0.022f, 0.045f })
            b.Tube(head, Smooth(3, c + V(x * 0.8f, 0.045f, 0.0f), c + V(x, 0.03f, 0.065f), c + V(x, -0.03f, 0.075f), c + V(x * 0.8f, -0.065f, 0.045f)), 0.0065f, 0.0065f, plate, Metal, 0.004f);
        foreach (float y in new[] { 0.01f, -0.035f })
            b.Limb(head, c + V(-0.055f, y, 0.06f), c + V(0.055f, y, 0.06f), 0.006f, 0.006f, plate, Metal, 0.004f);
        foreach (var d in b.Model.Decals.Where(d => d.IsEye)) d.Iris = Rgb(170, 236, 220);
        return b;
    }

    // ------------------------------------------------------------------ 2920 Locustorm, from Lokix

    /// <summary>
    /// Locustorm: Lokix taller, its armour gunmetal, its visor drawn to a sharper point with a red band across it like
    /// a mask, its shins and forearms crimson banded in black, long smoky wings for flying far and a ragged cloak of
    /// wing-cases at its back.
    /// </summary>
    private static PokeBuilder Locustorm()
    {
        var b = Become(Lokix(), "Locustorm", 0.95f);
        var bc = V(0, 0.5f, 0);
        var c = bc + V(0, 0.2f, 0.02f);
        var black = Rgb(48, 50, 60);
        var red = Rgb(156, 34, 48);
        Recolour(b, (Rgb(96, 102, 122), red), (Rgb(242, 150, 40), Rgb(186, 44, 56)), (Rgb(232, 232, 238), Rgb(120, 124, 136)));
        int head = BoneOf(b, "head");
        // The visor drawn to a point over its brow, a red band across it
        b.Spike(head, c + V(0, 0.03f, 0.03f), c + V(0, 0.07f, 0.1f), 0.028f, black, 0.5f);
        b.PaintEll(head, c + V(0, 0.016f, 0.05f), V(0.065f, 0.013f, 0.03f), red, soft: 0.005f);
        // Black bands round its crimson shins
        PokeBuilder.Both(s =>
        {
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            var knee = V(0.09f * s, 0.26f, 0.08f);
            var heel = V(0.09f * s, 0.1f, -0.06f);
            foreach (float t in new[] { 0.3f, 0.65f })
                b.PaintTorus(leg, Vector3.Lerp(knee, heel, t), 0.023f, 0.008f, black, Euler(heel - knee));
        });
        // Long smoky wings folded along its back, reaching past its knees
        PokeBuilder.Both(s =>
        {
            var root = bc + V(0.03f * s, 0.08f, -0.06f);
            var tip = bc + V(0.13f * s, -0.42f, -0.2f);
            Frond(b, Body, root, tip, 0.075f, Rgb(84, 80, 74), V(0.4f * s, 0, -1f), 0.08f, Shell, 0.01f);
            b.Limb(Body, root, Vector3.Lerp(root, tip, 0.85f), 0.008f, 0.005f, Rgb(60, 58, 56), Shell, 0.004f);
        });
        // A ragged cloak of dark wing-cases from its shoulders
        for (int i = 0; i < 7; i++)
        {
            float x = -0.075f + i * 0.025f;
            float drop = 0.2f + 0.06f * ((i * 5) % 3);
            Blade(b, Body, bc + V(x, 0.1f, -0.055f), bc + V(x * 1.7f, 0.1f - drop - 0.1f, -0.11f), 0.03f, Rgb(40, 40, 48), V(0, 0.1f, -1f), 0.2f, Shell);
        }
        b.Ell(Body, bc + V(0, 0.1f, -0.03f), V(0.085f, 0.035f, 0.05f), Rgb(40, 40, 48), blend: 0.015f);
        Stretch(b, V(1f, 1.1f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2925 Squeakshire, from Maushold

    /// <summary>
    /// Squeakshire: Maushold's family grown to six: two big parents with a white scarf of fur each, two little ones in
    /// the middle, the smallest at their feet and another riding on a parent's head, all with pink cheeks, pink ears and
    /// long pink tails curling over the ground.
    /// </summary>
    private static PokeBuilder Squeakshire()
    {
        var b = new PokeBuilder("Squeakshire", 0.62f, BodyPlan.Biped, V(0, 0.1f, 0)) { Coat = Fur };
        var pink = Rgb(242, 168, 176);
        var scarf = Rgb(252, 252, 252);
        const float big = 1.35f;
        var left = V(-0.12f, 0, 0.0f);
        var right = V(0.12f, 0, -0.02f);
        var kidA = V(-0.05f, 0, 0.085f);
        var kidB = V(0.065f, 0, 0.09f);
        var tiny = V(0.005f, 0, 0.14f);
        var rider = V(0.12f, 0.32f, -0.03f);
        int l = b.Head(left + V(0, 0.15f, 0) * big);
        int r = b.Part("head2", Body, right + V(0, 0.15f, 0) * big, PokeRole.Head, 1.9f, 1f);
        int ka = b.Part("head3", Body, kidA + V(0, 0.09f, 0), PokeRole.Head, 3.3f, -1f);
        int kb = b.Part("head4", Body, kidB + V(0, 0.09f, 0), PokeRole.Head, 4.6f, 1f);
        int kt = b.Part("head5", Body, tiny + V(0, 0.06f, 0), PokeRole.Head, 2.4f);
        int kr = b.Part("head6", r, rider + V(0, 0.05f, 0), PokeRole.Head, 5.7f);
        Mouse(b, Body, l, left, big, null, V(-0.06f, 0.11f, 0.07f), false);
        Mouse(b, Body, r, right, big, V(0.07f, 0.115f, 0.076f), null, false);
        Mouse(b, Body, ka, kidA, 0.62f, null, null, false);
        Mouse(b, Body, kb, kidB, 0.65f, null, null, false);
        Mouse(b, Body, kt, tiny, 0.42f, V(-0.016f, 0.052f, 0.1f), V(0.03f, 0.055f, 0.105f), false);
        Mouse(b, kr, kr, rider, 0.4f, null, null, false);
        Recolour(b, (MouseEarIn, pink), (MouseShorts, Rgb(238, 230, 222)));
        // Pink cheeks on every face
        foreach (var (foot, k, bone) in new[] { (left, big, l), (right, big, r), (kidA, 0.62f, ka), (kidB, 0.65f, kb), (tiny, 0.42f, kt), (rider, 0.4f, kr) })
        {
            var hc = foot + V(0, 0.185f, 0.005f) * k;
            var hr = V(0.075f, 0.065f, 0.062f) * k;
            PokeBuilder.Both(s => b.PaintEll(bone, On(hc, hr, hc.X + 0.045f * s * k, hc.Y - 0.02f * k), V(0.016f, 0.011f, 0.012f) * k, pink, soft: 0.004f * k));
        }
        // The parents' scarves of white fur
        foreach (var foot in new[] { left, right })
        {
            var neck = foot + V(0, 0.135f, 0.004f) * big;
            b.Torus(Body, neck, 0.047f * big, 0.017f * big, scarf, blend: 0.01f);
            FurTufts(b, Body, neck, V(0.06f, 0.025f, 0.055f) * big, 14, 0.03f, 0.012f, scarf, 1.1f, -0.3f, 0.6f);
        }
        // Long pink tails curling out over the ground and crossing before them
        foreach (var (foot, k, s) in new[] { (left, big, -1f), (right, big, 1f), (kidA, 0.62f, -1f), (kidB, 0.65f, 1f) })
        {
            var root = foot + V(0, 0.06f, -0.035f) * k;
            b.Tube(Body, Smooth(3, root, foot + V(0.05f * s, 0.01f, -0.08f) * k, foot + V(0.17f * s, 0.008f, -0.02f) * k,
                foot + V(0.12f * s, 0.008f, 0.12f) * k, foot + V(-0.05f * s, 0.008f, 0.2f) * k), 0.0075f * k, 0.005f, pink, blend: 0.004f);
        }
        b.Tube(kr, Smooth(3, rider + V(0, 0.025f, -0.015f), rider + V(0.02f, -0.02f, -0.04f), rider + V(0.03f, -0.1f, -0.05f), rider + V(0.04f, -0.16f, -0.03f)), 0.004f, 0.0035f, pink, blend: 0.003f);
        return b;
    }

    // ------------------------------------------------------------------ 2927 Brioschund, from Dachsbun

    /// <summary>
    /// Brioschund: Dachsbun longer, glazed a deep gold, its bun grown into a braided brioche down its whole back, a
    /// scatter of pale seeds on its ears, and its tail a rolled bun with a little flame at its tip.
    /// </summary>
    private static PokeBuilder Brioschund()
    {
        var b = Become(Dachsbun(), "Brioschund", 0.72f);
        var bc = V(0, 0.15f, -0.05f);
        var glaze = Rgb(196, 112, 42);
        var golden = Rgb(222, 146, 64);
        var crust = Rgb(176, 96, 38);
        Recolour(b, (Rgb(198, 98, 44), golden), (Rgb(140, 62, 30), crust));
        int tail = BoneOf(b, "tail");
        int head = BoneOf(b, "head");
        b.Sdf.RemoveAll(s => s.Bone == tail && Is(s.Color, Rgb(248, 226, 142)));
        // The braid: lobes laid aslant down its back, each glazed
        for (int i = 0; i < 8; i++)
        {
            float z = 0.1f - i * 0.03f;
            float s = i % 2 == 0 ? 1f : -1f;
            var at = bc + V(0.025f * s, 0.108f - MathF.Abs(z + 0.02f) * 0.3f, z - 0.02f);
            b.Ell(Body, at, V(0.056f, 0.034f, 0.03f), glaze, V(0, 35f * s, 0), Shell, 0.012f);
            b.PaintEll(Body, at + V(-0.01f * s, 0.025f, 0.005f), V(0.02f, 0.008f, 0.012f), Rgb(240, 186, 110), V(0, 35f * s, 0), 0.005f);
        }
        // Pale seeds on its ears
        PokeBuilder.Both(s =>
        {
            int ear = BoneOf(b, s < 0 ? "earL" : "earR");
            var c = bc + V(0, 0.1f, 0.17f);
            var at = c + V(0.078f * s, 0.01f, -0.01f);
            foreach (var (dy, dz) in new[] { (0.02f, 0.0f), (0.006f, 0.018f), (-0.012f, -0.008f), (0.018f, -0.02f) })
                b.PaintEll(ear, at + V(0.045f * s, dy, dz), V(0.008f, 0.004f, 0.005f), Rgb(250, 238, 200), V(0, 0, 30f), 0.002f);
        });
        // The tail rolled into a spiral bun, a small flame at its tip
        var end = bc + V(0, 0.12f, -0.18f);
        b.Ell(tail, end + V(0, 0.01f, -0.015f), V(0.04f, 0.048f, 0.044f), glaze, mat: Shell, blend: 0.012f);
        PokeBuilder.Both(s => b.Torus(tail, end + V(0.03f * s, 0.01f, -0.015f), 0.022f, 0.011f, crust, V(0, 0, 90f), mat: Shell, blend: 0.006f));
        b.PaintTorus(tail, end + V(0, 0.01f, -0.015f), 0.04f, 0.007f, crust, V(90f, 0, 0));
        FlameTongue(b, tail, end + V(0, 0.05f, -0.02f), end + V(0.01f, 0.11f, -0.03f), 0.014f, Rgb(244, 120, 40), Rgb(255, 222, 110));
        _ = head;
        Stretch(b, V(1f, 1.05f, 1.22f));
        return b;
    }

    // ------------------------------------------------------------------ 2931 Bossquawk, from Squawkabilly

    /// <summary>
    /// Bossquawk: Squawkabilly larger and taller, its crest grown into a tall swept pompadour dark green tipped yellow,
    /// a ruff of orange feathers at its neck like a collar, and its beak heavier, hooked and grey.
    /// </summary>
    private static PokeBuilder Bossquawk()
    {
        var b = Become(SquawkabillyBuild("Green"), "Bossquawk", 0.86f);
        var bc = V(0, 0.2f, 0);
        var c = bc + V(0, 0.14f, 0.02f);
        int head = BoneOf(b, "head");
        Recolour(b, (Rgb(246, 168, 40), Rgb(112, 110, 108)), (Rgb(228, 140, 40), Rgb(88, 86, 86)), (Rgb(244, 244, 240), Rgb(222, 236, 170)));
        // A heavier hooked beak
        Grow(b, V(1.3f, 1.3f, 1.25f), c + V(0, -0.02f, 0.05f), s => s.Bone == head && (Is(s.Color, Rgb(112, 110, 108)) || Is(s.Color, Rgb(88, 86, 86))), _ => false);
        // The pompadour: long feathers swept up and back from its brow, dark with yellow tips
        var dark = Rgb(34, 72, 42);
        var yellow = Rgb(232, 214, 70);
        for (int i = 0; i < 9; i++)
        {
            float x = -0.04f + i * 0.01f;
            float lean = (i % 3 - 1) * 0.02f;
            var root = c + V(x, 0.05f, 0.02f - MathF.Abs(x) * 0.3f);
            var tip = c + V(x * 2.2f + lean, 0.21f - MathF.Abs(x) * 1.2f, -0.06f - (i % 2) * 0.03f);
            Frond(b, head, root, tip, 0.022f, i % 2 == 0 ? dark : Rgb(46, 98, 52), V(x * 20f, 0.2f, 1f), 0.3f, Fur, 0.01f);
            b.PaintEll(head, Vector3.Lerp(root, tip, 0.92f), V(0.02f, 0.025f, 0.02f), yellow, soft: 0.006f);
        }
        // A ruff of orange feathers round its neck
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.Tau / 12f;
            var n = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = bc + V(0, 0.085f, 0.01f) + n * V(0.05f, 0, 0.05f);
            var tip = root + n * 0.06f + V(0, 0.04f, 0);
            Frond(b, Body, root, tip, 0.024f, Rgb(242, 128, 36), V(0, 1f, 0) - n * 0.3f, 0.3f, Fur, 0.008f);
            b.PaintEll(Body, Vector3.Lerp(root, tip, 0.9f), V(0.02f, 0.02f, 0.02f), Rgb(248, 200, 60), soft: 0.006f);
        }
        Stretch(b, V(1.05f, 1.12f, 1.05f));
        return b;
    }

    // ------------------------------------------------------------------ 2936 Pyropaladin, from Armarouge

    /// <summary>
    /// Pyropaladin: Armarouge taller, its armour red trimmed in gold, a gold crest on its helm, a kite shield of gold
    /// plates on its left forearm, a longer cannon on its right glowing at the muzzle, and a mantle of flame behind
    /// its shoulders.
    /// </summary>
    private static PokeBuilder Pyropaladin()
    {
        var b = Become(Armarouge(), "Pyropaladin", 1.0f);
        var bc = V(0, 0.66f, 0);
        var c = bc + V(0, 0.21f, 0.01f);
        var gold = Rgb(236, 196, 70);
        var crimson = Rgb(160, 34, 42);
        Tint(b, s => s.Bone == Body && s.Op == SdfOp.Add && Is(s.Color, Rgb(246, 214, 46)), crimson);
        Recolour(b, (Rgb(246, 214, 46), gold));
        b.PaintEll(Body, bc + V(0, 0.03f, 0.055f), V(0.025f, 0.03f, 0.02f), gold, soft: 0.006f);
        b.PaintTorus(Body, bc + V(0, -0.07f, 0), 0.075f, 0.01f, gold);
        int head = BoneOf(b, "head");
        // A gold crest down the middle of the helm
        Blade(b, head, c + V(0, 0.05f, 0.035f), c + V(0, 0.11f, -0.05f), 0.035f, gold, V(1f, 0, 0), 0.2f, Metal);
        // The shield on the left forearm, gold rimmed with a red field
        int armL = BoneOf(b, "armL");
        var sc = bc + V(-0.255f, -0.1f, 0.08f);
        b.Box(armL, sc, V(0.012f, 0.11f, 0.08f), 0.012f, gold, V(0, 10f, 0), Metal, 0.006f);
        b.Spike(armL, sc + V(0, -0.08f, 0), sc + V(0.004f, -0.18f, 0.01f), 0.07f, gold, 0.17f, Metal, 0.006f);
        b.PaintEll(armL, sc + V(-0.014f, -0.02f, 0.01f), V(0.008f, 0.1f, 0.06f), crimson, V(0, 10f, 0), 0.006f);
        b.PaintEll(armL, sc + V(-0.014f, 0.0f, 0.01f), V(0.006f, 0.03f, 0.03f), Rgb(246, 140, 50), soft: 0.004f);
        // The cannon on the right forearm, longer, ringed in gold, glowing at its muzzle
        int armR = BoneOf(b, "armR");
        var from = bc + V(0.2f, -0.12f, 0.04f);
        var to = bc + V(0.27f, -0.2f, 0.3f);
        b.Limb(armR, from, to, 0.05f, 0.048f, crimson, Metal, 0.01f);
        foreach (float t in new[] { 0.2f, 0.55f, 0.95f })
            b.Torus(armR, Vector3.Lerp(from, to, t), 0.05f, 0.01f, gold, Euler(to - from), mat: Metal, blend: 0.004f);
        b.Ell(armR, to + Vector3.Normalize(to - from) * 0.03f, V(0.034f, 0.012f, 0.034f), Rgb(255, 170, 60), Euler(to - from), Glow, 0.006f);
        // A mantle of flame behind the shoulders
        FlameRow(b, Body, bc + V(-0.17f, 0.12f, -0.06f), bc + V(0.17f, 0.12f, -0.06f), V(0, 0.55f, -1f), 7, 0.24f, 0.04f, Rgb(244, 110, 40), Rgb(255, 210, 100), 1.2f);
        FlameRow(b, Body, bc + V(-0.12f, 0.02f, -0.06f), bc + V(0.12f, 0.02f, -0.06f), V(0, -0.3f, -1f), 5, 0.2f, 0.035f, Rgb(244, 110, 40), Rgb(255, 210, 100), 1.0f);
        b.Ell(Body, bc + V(0, 0.08f, -0.04f), V(0.1f, 0.06f, 0.04f), crimson, mat: Metal, blend: 0.02f);
        foreach (var d in b.Model.Decals.Where(d => d.IsEye)) d.Iris = Rgb(220, 90, 240);
        Stretch(b, V(1.02f, 1.08f, 1.02f));
        return b;
    }

    // ------------------------------------------------------------------ 2937 Duskenblade, from Ceruledge

    /// <summary>
    /// Duskenblade: Ceruledge taller, its two blades joined into one long glaive held across its body, a blade of blue
    /// fire at each end, a long tattered cloak of blue flame from its shoulders, and a crown of dark spikes on its helm.
    /// </summary>
    private static PokeBuilder Duskenblade()
    {
        var b = Become(Ceruledge(), "Duskenblade", 1.0f);
        var bc = V(0, 0.7f, 0);
        var c = bc + V(0, 0.2f, 0.01f);
        var fire = Rgb(96, 128, 246);
        var pale = Rgb(170, 190, 255);
        var dark = Rgb(30, 28, 62);
        int armL = BoneOf(b, "armL"), armR = BoneOf(b, "armR");
        b.Sdf.RemoveAll(s => (s.Bone == armL || s.Bone == armR) && (Is(s.Color, Rgb(150, 170, 246)) || Is(s.Color, Rgb(200, 150, 236))));
        // The glaive across its body, through both hands, a blade of blue fire at each end
        var p0 = bc + V(-0.32f, -0.08f, 0.06f);
        var p1 = bc + V(0.32f, -0.16f, 0.06f);
        b.Limb(armR, p0, p1, 0.011f, 0.011f, Rgb(46, 44, 70), Metal, 0.006f);
        foreach (var (end, way) in new[] { (p1, Vector3.Normalize(p1 - p0)), (p0, Vector3.Normalize(p0 - p1)) })
        {
            b.Ell(armR, end, V(0.02f, 0.02f, 0.02f), dark, mat: Metal, blend: 0.004f);
            var tip = end + way * 0.18f + V(0, 0.05f, 0);
            Blade(b, armR, end, tip, 0.05f, fire, V(0, 0, 1f), 0.16f, Glow);
            b.Limb(armR, end, tip - way * 0.04f, 0.007f, 0.005f, pale, Glow, 0.004f);
            Blade(b, armR, end + way * 0.04f, end + way * 0.09f + V(0, -0.07f, 0), 0.026f, fire, V(0, 0, 1f), 0.2f, Glow);
        }
        // A long tattered cloak of blue flame falling from the shoulders to the ground
        b.Ell(Body, bc + V(0, 0.08f, -0.035f), V(0.09f, 0.04f, 0.045f), dark, mat: Metal, blend: 0.02f);
        b.Ell(Body, bc + V(0, -0.25f, -0.06f), V(0.13f, 0.34f, 0.016f), fire, default, Glow, 0.03f);
        for (int i = 0; i < 7; i++)
        {
            float x = -0.09f + i * 0.03f;
            var root = bc + V(x, -0.5f, -0.062f);
            b.Spike(Body, root, root + V(x * 0.3f, -0.12f - 0.04f * (i % 2), -0.02f), 0.026f, fire, 0.25f, Glow, 0.01f);
        }
        b.PaintEll(Body, bc + V(0, -0.2f, -0.075f), V(0.06f, 0.25f, 0.01f), pale, soft: 0.03f);
        // A circlet of dark spikes on its helm
        int head = BoneOf(b, "head");
        var ring = c + V(0, 0.04f, 0.0f);
        b.Torus(head, ring, 0.034f, 0.007f, dark, mat: Metal, blend: 0.004f);
        for (int i = 0; i < 7; i++)
        {
            float a = i * MathF.Tau / 7f + 0.2f;
            var p = ring + V(MathF.Sin(a) * 0.034f, 0, MathF.Cos(a) * 0.034f);
            b.Spike(head, p, p + V(MathF.Sin(a) * 0.02f, 0.065f, MathF.Cos(a) * 0.02f), 0.01f, dark, mat: Metal, blend: 0.003f);
        }
        foreach (var d in b.Model.Decals.Where(d => d.IsEye)) d.Iris = Rgb(140, 220, 255);
        Stretch(b, V(1f, 1.08f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2939 Thunderbelly, from Bellibolt

    /// <summary>
    /// Thunderbelly: Bellibolt larger and a deeper teal, its belly a drum ridged in coils of yellow and grey, its eyes
    /// great glowing yellow bulbs, yellow streaks of charge on its hide and a cloud of grey froth round its feet.
    /// </summary>
    private static PokeBuilder Thunderbelly()
    {
        var b = Become(Bellibolt(), "Thunderbelly", 0.95f);
        var bc = V(0, 0.2f, 0);
        var br = V(0.2f, 0.18f, 0.17f);
        var teal = Rgb(36, 122, 132);
        var yellow = Rgb(238, 210, 70);
        var grey = Rgb(132, 136, 140);
        Recolour(b, (Rgb(36, 172, 142), teal), (Rgb(232, 142, 62), yellow), (Rgb(84, 74, 68), Rgb(70, 74, 84)));
        // The drum of a belly: rings of coil ridged out on it, yellow and grey in turn
        var bulb = bc + V(0, -0.02f, 0.11f);
        for (int i = 0; i < 4; i++)
        {
            float rr = 0.025f + i * 0.028f;
            float z = 0.08f * MathF.Sqrt(MathF.Max(0f, 1f - MathF.Pow(rr / 0.135f, 2)));
            b.Torus(Body, bulb + V(0, 0, z - 0.006f), rr, 0.009f, i % 2 == 0 ? yellow : grey, V(90f, 0, 0), mat: Shell, blend: 0.005f);
        }
        // Eyes grown into glowing bulbs
        int head = BoneOf(b, "head");
        Grow(b, V(1.2f, 1.2f, 1.2f), bc + V(0, 0.17f, 0.07f), s => s.Bone == head, bone => bone == head);
        Tint(b, s => s.Bone == head && Is(s.Color, Rgb(250, 250, 246)), Rgb(255, 222, 80));
        Make(b, s => s.Bone == head && Is(s.Color, Rgb(255, 222, 80)), Glow);
        foreach (var d in b.Model.Decals.Where(d => d.IsEye)) d.White = Rgb(255, 236, 120);
        // Yellow streaks of charge over its back
        foreach (var (x, y, z, rot) in new[] { (0.14f, 0.1f, -0.06f, 30f), (-0.15f, 0.06f, -0.05f, -20f), (0.08f, 0.15f, -0.08f, 70f), (-0.06f, 0.14f, -0.1f, -60f) })
            b.PaintEll(Body, Out(bc, br, default, V(x, y, z)), V(0.008f, 0.04f, 0.008f), yellow, V(0, 0, rot), 0.004f);
        // A cloud of grey froth round each foot
        PokeBuilder.Both(s =>
        {
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            var foot = V(0.1f * s, 0.035f, 0.05f);
            foreach (var (dx, dz, rad) in new[] { (0.05f, 0.03f, 0.04f), (-0.03f, 0.06f, 0.034f), (0.06f, -0.04f, 0.036f), (0.0f, -0.05f, 0.03f), (0.08f, 0.07f, 0.028f) })
                b.Ell(leg, foot + V(dx * s, -0.01f, dz), V(rad, rad * 0.75f, rad), Rgb(206, 210, 216), blend: 0.015f);
        });
        Stretch(b, V(1.06f, 1.1f, 1.06f));
        return b;
    }

    // ------------------------------------------------------------------ 2941 Terawattrel, from Kilowattrel

    /// <summary>
    /// Terawattrel: Kilowattrel wider, its wings longer and forked at their tips into glowing points of lightning, its
    /// yellow throat pouch larger, a ragged crest on its head and two long streamers trailing from its tail.
    /// </summary>
    private static PokeBuilder Terawattrel()
    {
        var b = Become(Kilowattrel(), "Terawattrel", 1.0f);
        var bc = V(0, 0.36f, 0);
        Unlift(b, bc);
        var c = bc + V(0, 0.04f, 0.19f);
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            var shoulder = bc + V(0.05f * s, 0.03f, 0.03f);
            var k = V(1.05f, 1.1f, 1.12f);
            Grow(b, k, shoulder, bone => bone == wing);
            Make(b, x => x.Bone == wing && Is(x.Color, StormYellow) && x.Op == SdfOp.Add, Glow);
            var tip = shoulder + (bc + V(0.6f * s, 0.09f, -0.12f) - shoulder) * k;
            var back = tip - Vector3.Normalize(tip - (shoulder + (bc + V(0.3f * s, 0.15f, 0.0f) - shoulder) * k)) * 0.06f;
            foreach (var d in new[] { V(0.08f * s, 0.05f, -0.02f), V(0.06f * s, -0.02f, -0.08f), V(0.03f * s, -0.03f, -0.13f) })
                Blade(b, wing, back, tip + d, 0.03f, StormYellow, V(-0.3f * s, 1f, 0.35f), 0.25f, Glow);
        });
        // A larger pouch
        Grow(b, V(1.3f, 1.3f, 1.25f), bc + V(0, -0.045f, 0.09f), s => s.Bone == Body && Is(s.Color, StormYellow), _ => false);
        // A ragged crest swept back over its head
        int head = BoneOf(b, "head");
        foreach (var (x, h) in new[] { (-0.02f, 0.05f), (0f, 0.07f), (0.02f, 0.05f), (0f, 0.04f) })
            b.Spike(head, c + V(x, 0.03f, -0.01f), c + V(x * 2f, 0.03f + h, -0.07f - h * 0.4f), 0.012f, StormBlack, 0.45f);
        // Two long streamers from the tail, yellow at their ends
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(x => x.Bone == tail);
        PokeBuilder.Both(s =>
        {
            var path = Smooth(3, bc + V(0.015f * s, -0.01f, -0.13f), bc + V(0.03f * s, -0.07f, -0.3f), bc + V(0.09f * s, -0.12f, -0.5f), bc + V(0.14f * s, -0.09f, -0.66f));
            b.Tube(tail, path, 0.018f, 0.013f, StormBlack, blend: 0f);
            Blade(b, tail, path[^2], path[^1] + V(0.05f * s, 0.02f, -0.1f), 0.03f, StormYellow, V(0, 1f, 0), 0.25f, Glow);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2943 Mastiffdon, from Mabosstiff

    /// <summary>
    /// Mastiffdon: Mabosstiff larger and grey, a coat of charcoal fur over its back and chest, its beard gone into
    /// heavy jowls, dark drooping ears, a scar across its brow, and a heavy chain round its neck with a gold plate.
    /// </summary>
    private static PokeBuilder Mastiffdon()
    {
        var b = Become(Mabosstiff(), "Mastiffdon", 1.0f);
        var bc = V(0, 0.3f, -0.05f);
        var c = bc + V(0, 0.13f, 0.27f);
        var r = V(0.1f, 0.1f, 0.09f);
        int head = BoneOf(b, "head");
        var grey = Rgb(124, 128, 140);
        var charcoal = Rgb(58, 60, 66);
        b.Sdf.RemoveAll(s => s.Bone == head && Is(s.Color, Rgb(54, 48, 52)) && s.A.Y < c.Y - 0.08f && s.Shape == SdfShape.Ellipsoid);
        Recolour(b, (Rgb(126, 112, 100), grey), (Rgb(84, 70, 64), Rgb(118, 122, 134)), (Rgb(198, 198, 206), charcoal), (Rgb(54, 48, 52), charcoal), (Rgb(240, 140, 66), Rgb(40, 38, 44)));
        // The coat of charcoal fur over its back, shoulders and chest
        b.Ell(Body, bc + V(0, 0.04f, 0.04f), V(0.14f, 0.1f, 0.2f), charcoal, blend: 0.03f);
        Shag(b, Body, bc + V(0, 0.04f, 0.06f), V(0.14f, 0.1f, 0.2f), bc.Y + 0.0f, 18, 0.07f, 0.024f, charcoal, 2f);
        FurTufts(b, Body, bc + V(0, 0.0f, 0.18f), V(0.11f, 0.12f, 0.08f), 18, 0.05f, 0.02f, charcoal, 2f, -0.6f, 0.5f);
        // Heavy jowls and dark drooping ears
        PokeBuilder.Both(s =>
        {
            b.Ell(head, c + V(0.05f * s, -0.055f, 0.06f), V(0.05f, 0.05f, 0.05f), Rgb(118, 122, 134), blend: 0.015f);
            b.Ell(head, c + V(0.095f * s, 0.04f, -0.01f), V(0.022f, 0.05f, 0.035f), charcoal, V(0, 0, 25f * s), blend: 0.01f);
        });
        // A scar across its brow
        b.PaintEll(head, c + V(-0.045f, 0.045f, 0.07f), V(0.005f, 0.035f, 0.012f), Rgb(160, 80, 80), V(0, 0, -30f), 0.003f);
        // The chain round its neck, a gold plate hanging at its throat
        var neck = bc + V(0, 0.09f, 0.16f);
        LinkRing(b, Body, neck, V(0, 0.75f, 0.65f), 0.09f, 16, 0.017f, 0.006f, Rgb(70, 72, 78));
        b.Box(Body, neck + V(0, -0.075f, 0.075f), V(0.04f, 0.022f, 0.008f), 0.006f, Rgb(214, 176, 70), V(-35f, 0, 0), Metal, 0.006f);
        Stretch(b, V(1.06f, 1.08f, 1.06f));
        return b;
    }

    // ------------------------------------------------------------------ 2945 Graffitape, from Grafaiai

    /// <summary>
    /// Graffitape: Grafaiai taller, a grey ruff at its neck, swirls of paint over its chest, every finger dipped in a
    /// colour of its own, and its tail grown long and lying over the ground, streaked with paint, ending in a brush.
    /// </summary>
    private static PokeBuilder Graffitape()
    {
        var b = Become(Grafaiai(), "Graffitape", 0.88f);
        var bc = V(0, 0.25f, 0);
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail);
        var dark = Rgb(62, 62, 80);
        var grey = Rgb(152, 150, 162);
        var purple = Rgb(140, 70, 200);
        var green = Rgb(80, 186, 90);
        var colours = new[] { Rgb(228, 62, 76), Rgb(244, 150, 40), Rgb(240, 222, 60), green, PaintBlue, purple };
        // A grey ruff at its neck and swirls of paint over its chest
        FurTufts(b, Body, bc + V(0, 0.06f, 0.01f), V(0.05f, 0.02f, 0.045f), 12, 0.03f, 0.012f, grey, 1.1f, -0.4f, 0.4f);
        foreach (var (x, y, col) in new[] { (0.015f, 0.03f, PaintPink), (-0.01f, 0.0f, purple), (0.02f, -0.03f, green), (-0.005f, -0.05f, PaintPink) })
            b.PaintEll(Body, bc + V(x, y, 0.04f), V(0.022f, 0.014f, 0.015f), col, V(0, 0, 30f), 0.005f);
        // Every finger a colour of its own
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var hand = s > 0 ? bc + V(0.03f, 0.08f, 0.09f) : bc + V(-0.09f, -0.09f, 0.03f);
            var up = s > 0 ? V(0.2f, 1f, 0.3f) : V(-0.2f, -1f, 0.3f);
            for (int i = 0; i < 4; i++)
            {
                var spread = V((i - 1.5f) * 0.4f * (s > 0 ? 1 : -1), 0, (i - 1.5f) * 0.1f);
                var d = Vector3.Normalize(up + spread);
                b.Spike(arm, hand + d * 0.01f, hand + d * 0.05f, 0.008f, colours[(i + (s > 0 ? 0 : 2)) % colours.Length], mat: Shell, blend: 0.004f);
            }
        });
        // The long tail over the ground, swirled with paint, its end a brush
        var root = bc + V(0, -0.06f, -0.04f);
        var path = Smooth(3, root, bc + V(0.03f, -0.16f, -0.12f), V(0.08f, 0.024f, -0.24f), V(0.26f, 0.024f, -0.22f), V(0.34f, 0.024f, -0.04f), V(0.3f, 0.024f, 0.12f));
        b.Tube(tail, path, 0.022f, 0.02f, dark, blend: 0f);
        for (int i = 3; i < path.Length; i += 2)
            b.PaintEll(tail, path[i] + V(0, 0.01f, 0), V(0.025f, 0.02f, 0.03f), colours[(i / 2) % colours.Length], Euler(path[i] - path[i - 1]), 0.006f);
        var end = path[^1];
        var way = Vector3.Normalize(end - path[^2]);
        b.Torus(tail, end, 0.022f, 0.008f, purple, Euler(way), mat: Metal, blend: 0.004f);
        b.Ell(tail, end + way * 0.04f, V(0.035f, 0.03f, 0.03f), Rgb(110, 90, 140), Euler(way), Fur, 0.012f);
        for (int i = 0; i < 7; i++)
        {
            float a = i * MathF.Tau / 7f;
            var side = Vector3.Normalize(Vector3.Cross(way, V(0, 1f, 0)));
            var off = side * MathF.Cos(a) * 0.022f + V(0, MathF.Sin(a) * 0.016f + 0.004f, 0);
            b.Spike(tail, end + way * 0.04f + off, end + way * 0.14f + off * 2.2f, 0.016f, i % 2 == 0 ? purple : PaintBlue, 0.6f, Fur, 0.008f);
        }
        Stretch(b, V(1f, 1.1f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2947 Thornwraith, from Brambleghast

    /// <summary>
    /// Thornwraith: Brambleghast larger, its wreath of brambles wound round a ghostly core of pale green light with a
    /// hollow-eyed face and a tail of mist, two long thorned vines hanging from the wreath like arms, clawed at their ends.
    /// </summary>
    private static PokeBuilder Thornwraith()
    {
        var b = Become(Brambleghast(), "Thornwraith", 1.0f);
        var c = V(0, 0.45f, 0);
        Unlift(b, c);
        const float R = 0.36f;
        int head = BoneOf(b, "head");
        b.Sdf.RemoveAll(s => s.Bone == head);
        var ghost = Rgb(150, 232, 196);
        var vine = Rgb(78, 92, 58);
        // The ghostly core, hollow-eyed, trailing a tail of mist
        var gc = c + V(0, 0.02f, 0);
        var gr = V(0.15f, 0.19f, 0.12f);
        b.Ell(head, gc, gr, ghost, mat: Glow, blend: 0.02f);
        b.Spike(head, gc + V(0, -0.12f, -0.02f), gc + V(0.04f, -0.42f, -0.05f), 0.09f, ghost, 0.7f, Glow, 0.03f);
        b.Spike(head, gc + V(0, 0.12f, -0.02f), gc + V(-0.03f, 0.3f, -0.08f), 0.05f, ghost, 0.6f, Glow, 0.02f);
        PokeBuilder.Both(s =>
        {
            var at = Out(gc, gr, default, V(0.32f * s, 0.12f, 1f));
            b.PaintEll(head, at, V(0.03f, 0.034f, 0.02f), Rgb(46, 92, 80), soft: 0.006f);
            b.Eye(head, at + Outward(gc, gr, at) * 0.002f, Outward(gc, gr, at), 0.014f, white: Rgb(230, 255, 240), pupil: Rgb(30, 60, 50));
        });
        var mouth = Out(gc, gr, default, V(0, -0.35f, 1f));
        b.Mark(head, mouth, Outward(gc, gr, mouth), 0.035f, 0.016f, Rgb(46, 92, 80), MarkShape.Zigzag);
        // Vines of the wreath reaching in to hold the core
        foreach (float a in new[] { 0.6f, 2.5f, 4.4f })
        {
            var rim = c + V(MathF.Cos(a), MathF.Sin(a), 0) * R;
            b.Tube(head, Smooth(2, rim, Vector3.Lerp(rim, gc, 0.55f) + V(0, 0, 0.03f), gc + V(MathF.Cos(a), MathF.Sin(a), 0) * 0.1f), 0.014f, 0.012f, BrambleBrown, blend: 0.006f);
        }
        // Two long thorned vines hanging from the wreath, clawed at their ends
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, c + V(R * 0.95f * s, -0.08f, 0));
            var path = Smooth(3, c + V(R * 0.97f * s, -0.08f, 0), c + V(0.5f * s, -0.14f, 0.05f), c + V(0.55f * s, -0.28f, 0.02f), c + V(0.47f * s, -0.4f, 0.06f), c + V(0.5f * s, -0.5f, 0.08f));
            b.Tube(arm, path, 0.02f, 0.014f, vine, Shell, 0.006f);
            for (int i = 2; i < path.Length - 1; i += 2)
            {
                var d = Vector3.Normalize(Vector3.Cross(path[i + 1] - path[i - 1], V(0, 0, 1f))) * (i % 4 == 0 ? 1f : -1f);
                b.Spike(arm, path[i], path[i] + d * 0.035f, 0.008f, vine, mat: Shell, blend: 0.003f);
            }
            var end = path[^1];
            foreach (var d in new[] { V(0.04f * s, -0.05f, 0.03f), V(-0.01f * s, -0.06f, 0.04f), V(-0.04f * s, -0.04f, 0.0f) })
                b.Tube(arm, Smooth(2, end, end + d, end + d * 1.5f + V(0, 0.02f, 0.02f)), 0.01f, 0.004f, vine, Shell, 0.004f);
        });
        Recolour(b, (BrambleTan, Rgb(132, 110, 80)));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2949 Myceliarch, from Toedscruel

    /// <summary>
    /// Myceliarch: Toedscruel taller, its cap pale and broader with a ring of little mushrooms growing round it, its
    /// body and tentacles dark, more tentacles than before, pale threads between them and spots that glow pale blue.
    /// </summary>
    private static PokeBuilder Myceliarch()
    {
        var b = Become(Toedscruel(), "Myceliarch", 1.0f);
        var c = V(0, 0.64f, 0);
        var cap = c + V(0, 0.11f, 0);
        int head = BoneOf(b, "head");
        var capColour = Rgb(232, 220, 192);
        var dark = Rgb(76, 68, 56);
        var thread = Rgb(226, 222, 206);
        var glow = Rgb(150, 220, 250);
        Tint(b, s => s.Bone == head && Is(s.Color, Rgb(46, 42, 50)), capColour);
        Recolour(b, (Rgb(240, 186, 168), Rgb(214, 200, 170)), (Rgb(244, 238, 208), dark), (Rgb(238, 216, 82), Rgb(110, 96, 74)));
        Grow(b, V(1.35f, 1.15f, 1.35f), cap, bone => bone == head);
        // A ring of little mushrooms round the cap
        for (int i = 0; i < 14; i++)
        {
            float a = i * MathF.Tau / 14f + 0.1f;
            var at = cap + V(MathF.Cos(a) * 0.23f, 0.02f, MathF.Sin(a) * 0.21f);
            float h = 0.03f + 0.015f * (i % 3);
            Toadstool(b, head, at, V(MathF.Cos(a) * 0.3f, 1f, MathF.Sin(a) * 0.3f), h, Rgb(236, 230, 214), capColour);
        }
        // More tentacles, falling between the others to the ground, and pale threads strung between them
        var mids = new Vector3[14];
        for (int i = 0; i < 7; i++)
        {
            float a = i * MathF.Tau / 7f + 0.3f + MathF.Tau / 14f;
            var n = V(MathF.Cos(a), 0, MathF.Sin(a));
            var root = c + n * 0.04f + V(0, -0.06f, 0);
            var path = Smooth(3, root, root + n * 0.07f + V(0, -0.22f, 0), n * 0.17f + V(0, 0.15f, 0), n * 0.2f + V(0, 0.02f, 0));
            b.Tube(Body, path, 0.016f, 0.011f, dark, blend: 0.006f);
            mids[i * 2 + 1] = n * 0.16f + V(0, 0.25f, 0);
        }
        for (int i = 0; i < 7; i++)
        {
            float a = i * MathF.Tau / 7f + 0.3f;
            var n = V(MathF.Cos(a), 0, MathF.Sin(a));
            mids[i * 2] = n * 0.115f + V(0, 0.25f, 0);
        }
        for (int i = 0; i < 14; i++)
        {
            var p = mids[i];
            var q = mids[(i + 1) % 14];
            b.Limb(Body, p, q, 0.0065f, 0.0065f, thread, blend: 0.004f);
            b.Limb(Body, p + V(0, -0.08f, 0) * 1.1f, Vector3.Lerp(p, q, 0.5f), 0.0065f, 0.0065f, thread, blend: 0.004f);
        }
        // Spots that glow pale blue on the body and down the tentacles
        foreach (var at in new[] { c + V(0.05f, -0.03f, 0.05f), c + V(-0.055f, -0.01f, 0.045f), c + V(0.06f, 0.03f, -0.03f), c + V(-0.03f, -0.06f, -0.05f) })
            PaintAs(b, Body, at, V(0.03f, 0.03f, 0.03f), glow, Glow);
        foreach (var d in b.Model.Decals.Where(d => d.IsEye)) d.White = glow;
        Stretch(b, V(1.05f, 1.1f, 1.05f));
        return b;
    }

    // ------------------------------------------------------------------ 2950 Klawlossus, from Klawf

    /// <summary>
    /// Klawlossus: Klawf larger, its body crimson plated in grey steel with spikes, a row of rocky teeth at its mouth,
    /// its eyestalks longer, its right claw grown into one great claw of rock (a fiddler's), its legs plated grey.
    /// </summary>
    private static PokeBuilder Klawlossus()
    {
        var b = Become(Klawf(), "Klawlossus", 0.95f);
        var bc = V(0, 0.2f, 0);
        var br = V(0.17f, 0.11f, 0.12f);
        var crimson = Rgb(150, 40, 54);
        var plate = Rgb(110, 116, 126);
        var rock = Rgb(156, 142, 124);
        var red = Rgb(232, 100, 56);
        Recolour(b, (Rgb(238, 210, 168), crimson), (Rgb(96, 102, 54), rock), (Rgb(242, 152, 142), Rgb(176, 70, 80)));
        int head = BoneOf(b, "head");
        int armR = BoneOf(b, "armR");
        // The great claw: the right arm's pincer grown to rock
        var wristR = bc + V(0.3f, 0.16f, 0.08f);
        Grow(b, V(1.8f, 1.8f, 1.8f), wristR, s => s.Bone == armR && s.A.Y > wristR.Y - 0.02f, bone => false);
        Tint(b, s => s.Bone == armR && Is(s.Color, red) && s.A.Y > wristR.Y - 0.02f, rock);
        Grow(b, V(1.4f, 1.4f, 1.4f), bc + V(0.15f, 0.02f, 0.04f), s => s.Bone == armR && s.A.Y <= wristR.Y - 0.02f, _ => false);
        foreach (var d in new[] { V(0.4f, 1f, 0.3f), V(1f, 0.6f, 0.2f), V(0.8f, 0.9f, -0.3f) })
            b.Spike(armR, wristR + V(0.02f, 0.1f, 0) + Vector3.Normalize(d) * 0.05f, wristR + V(0.02f, 0.1f, 0) + Vector3.Normalize(d) * 0.1f, 0.016f, Steel, mat: Metal);
        // Steel plates over its back and front, spiked at the rim
        foreach (var (dir, size) in new[] { (V(0, 1f, 0.2f), V(0.09f, 0.012f, 0.06f)), (V(0.7f, 0.7f, 0.1f), V(0.06f, 0.012f, 0.05f)), (V(-0.7f, 0.7f, 0.1f), V(0.06f, 0.012f, 0.05f)), (V(0.35f, 0.15f, 1f), V(0.05f, 0.04f, 0.012f)), (V(-0.35f, 0.15f, 1f), V(0.05f, 0.04f, 0.012f)) })
        {
            var at = Out(bc, br, default, dir);
            Plate(b, Body, at, Outward(bc, br, at), V(MathF.Max(size.X, size.Z), 0.014f, MathF.Min(size.X + size.Y, 0.06f)), plate, Metal, 0.01f);
        }
        foreach (var d in new[] { V(0.85f, 0.5f, -0.2f), V(-0.85f, 0.5f, -0.2f), V(0.6f, 0.8f, -0.4f), V(-0.6f, 0.8f, -0.4f) })
        {
            var at = Out(bc, br, default, d);
            b.Spike(Body, at, at + Outward(bc, br, at) * 0.06f, 0.016f, Steel, mat: Metal);
        }
        // Rocky teeth along the frown
        for (int i = 0; i < 6; i++)
        {
            float x = -0.06f + i * 0.024f;
            var at = On(bc, br, x, bc.Y - 0.005f);
            b.Spike(Body, at - V(0, 0, 0.01f), at + V(0, (i % 2 == 0 ? -0.025f : 0.02f), 0.012f), 0.011f, rock, 0.6f, Shell, 0.004f);
        }
        // Longer eyestalks
        Move(b, bone => bone == head, V(0, 0.06f, 0));
        PokeBuilder.Both(s => b.Limb(head, bc + V(0.05f * s, 0.07f, 0.06f), bc + V(0.055f * s, 0.16f, 0.065f), 0.013f, 0.012f, Rgb(170, 120, 100), blend: 0.006f));
        Tint(b, s => s.Bone == head && Is(s.Color, red), Rgb(170, 120, 100));
        // Grey plating down the legs
        foreach (var leg in Enumerable.Range(0, b.Model.Bones.Count).Where(i => HasRole(b, i, PokeRole.Leg)))
            Tint(b, s => s.Bone == leg && s.Shape == SdfShape.RoundCone && MathF.Min(s.A.Y, s.B.Y) < 0.05f, plate);
        Stretch(b, V(1.06f, 1.06f, 1.06f));
        return b;
    }

    // ------------------------------------------------------------------ 2952 Habanerogue, from Scovillain

    /// <summary>
    /// Habanerogue: Scovillain larger, its body ripened into a ribbed orange pepper under its collar of leaves, the red
    /// head's crown of leaves a jester's cap with a little pepper at each point, the green head's leaves standing up
    /// wild, a third small green head between the two, and two vine arms with clawed fingers and buds of flame.
    /// </summary>
    private static PokeBuilder Habanerogue()
    {
        var b = Become(ScovillainBuild(false), "Habanerogue", 0.92f);
        var bc = V(0, 0.32f, 0);
        var orange = Rgb(230, 108, 44);
        var rib = Rgb(186, 70, 34);
        Tint(b, s => s.Bone == Body && s.Op == SdfOp.Add && s.Shape == SdfShape.Ellipsoid && Is(s.Color, PepperDark) && s.Size.X > 0.1f, orange);
        for (int i = 0; i < 10; i++)
        {
            float a = i * MathF.Tau / 10f;
            b.PaintEll(Body, bc + V(MathF.Sin(a), 0, MathF.Cos(a)) * 0.11f, V(0.012f, 0.1f, 0.012f), rib, V(0, a * 180f / MathF.PI, 0), 0.006f);
        }
        // The jester's cap on the red head: three drooping leaf points, a small red pepper at each end
        var cl = bc + V(-0.14f, 0.33f, 0.03f);
        int hl = BoneOf(b, "headL");
        foreach (var d in new[] { V(-1f, 0, 0.1f), V(1f, 0, 0.1f), V(0, 0, -1f) })
        {
            var root = cl + V(d.X * 0.03f, 0.07f, d.Z * 0.03f);
            var path = Smooth(3, root, root + d * 0.06f + V(0, 0.09f, 0), root + d * 0.13f + V(0, 0.07f, 0), root + d * 0.15f + V(0, -0.01f, 0));
            b.Tube(hl, path, 0.024f, 0.007f, PepperDark, Leaf, 0.008f);
            b.Ell(hl, path[^1] + V(0, -0.015f, 0), V(0.013f, 0.02f, 0.013f), PepperRed, mat: Shell, blend: 0.004f);
        }
        // The green head's leaves standing up wild
        var cr = bc + V(0.14f, 0.33f, 0.03f);
        int hr = BoneOf(b, "headR");
        foreach (var d in new[] { V(0, 1f, -0.2f), V(0.5f, 0.8f, -0.3f), V(-0.4f, 0.9f, -0.4f), V(0.8f, 0.5f, -0.1f), V(0.2f, 0.9f, 0.2f) })
        {
            var n = Vector3.Normalize(d);
            Frond(b, hr, cr + n * 0.06f, cr + n * 0.15f, 0.02f, PepperDark, V(-n.Z, 0, n.X) + V(0.01f, 0, 0), 0.25f, Leaf, 0.006f);
        }
        // The third, small green head between the two
        int h3 = b.Part("head3", Body, bc + V(0, 0.12f, 0.03f), PokeRole.Head, 1.2f);
        var c3 = bc + V(0, 0.22f, 0.07f);
        b.Limb(h3, bc + V(0, 0.08f, 0.02f), c3 + V(0, -0.03f, -0.01f), 0.03f, 0.028f, PepperDark, Leaf, 0.008f);
        PepperHead(b, h3, c3, 0.55f, PepperGreen, false);
        b.Spike(h3, c3 + V(0, 0.04f, -0.005f), c3 + V(0.01f, 0.08f, -0.01f), 0.01f, PepperDark, mat: Leaf, blend: 0.004f);
        // Vine arms with clawed fingers, a bud of flame on a tendril from each
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.1f * s, 0.02f, 0.02f));
            var path = Smooth(3, bc + V(0.1f * s, 0.02f, 0.02f), bc + V(0.2f * s, -0.02f, 0.06f), bc + V(0.25f * s, -0.1f, 0.1f));
            b.Tube(arm, path, 0.024f, 0.018f, PepperDark, Leaf, 0.008f);
            var hand = path[^1];
            foreach (var d in new[] { V(0.4f * s, -1f, 0.3f), V(0, -1f, 0.6f), V(-0.3f * s, -1f, 0.4f) })
                b.Tube(arm, Smooth(2, hand, hand + Vector3.Normalize(d) * 0.04f, hand + Vector3.Normalize(d) * 0.06f + V(0, 0.01f, 0.02f)), 0.01f, 0.004f, PepperDark, Leaf, 0.004f);
            var bud = bc + V(0.27f * s, 0.12f, 0.02f);
            b.Tube(arm, Smooth(2, path[1], bc + V(0.24f * s, 0.06f, 0.04f), bud), 0.008f, 0.006f, PepperDark, Leaf, 0.004f);
            FlameTongue(b, arm, bud, bud + V(0.01f * s, 0.05f, 0), 0.012f, Rgb(240, 90, 40), Rgb(255, 210, 90));
        });
        Stretch(b, V(1.05f, 1.08f, 1.05f));
        return b;
    }

    // ------------------------------------------------------------------ 2954 Khepristar, from Rabsca

    /// <summary>
    /// Khepristar: Rabsca larger, its ball a glowing sphere of gold with a ring round it like a small sun, the beetle
    /// beneath it green with longer legs, a pair of antler-horns and a pale sun on its wing-cases.
    /// </summary>
    private static PokeBuilder Khepristar()
    {
        var b = Become(Rabsca(), "Khepristar", 0.66f);
        var bc = V(0, 0.32f, 0);
        Unlift(b, bc);
        var gold = Rgb(244, 200, 84);
        var sun = Rgb(255, 232, 140);
        var green = Rgb(40, 140, 92);
        Recolour(b, (Rgb(112, 62, 142), gold), (Rgb(236, 72, 112), sun), (Rgb(255, 210, 226), Rgb(255, 250, 220)), (Rgb(72, 170, 182), green));
        Make(b, s => s.Bone == Body && s.Op == SdfOp.Add, Glow);
        // The rings round the sphere
        var tilt = V(18f, 0, -14f);
        b.Torus(Body, bc, 0.13f, 0.012f, Rgb(222, 168, 80), tilt, mat: Metal, blend: 0.004f);
        b.Torus(Body, bc, 0.2f, 0.007f, Rgb(222, 168, 80), tilt, mat: Metal, blend: 0.003f);
        var q = Quaternion.CreateFromYawPitchRoll(tilt.Y * Degree, tilt.X * Degree, tilt.Z * Degree);
        for (int i = 0; i < 4; i++)
        {
            float a = i * MathF.Tau / 4f + 0.4f;
            var d = Vector3.Transform(V(MathF.Cos(a), 0, MathF.Sin(a)), q);
            b.Limb(Body, bc + d * 0.135f, bc + d * 0.2f, 0.005f, 0.005f, Rgb(222, 168, 80), Metal, 0.003f);
        }
        // The beetle: a shell with a pale sun on it, horns, longer green legs
        int head = BoneOf(b, "head");
        var c = bc + V(0, -0.2f, 0.02f);
        b.Ell(head, c + V(0, -0.02f, -0.03f), V(0.05f, 0.03f, 0.06f), green, mat: Shell, blend: 0.01f);
        b.PaintEll(head, c + V(0, -0.045f, -0.03f), V(0.022f, 0.01f, 0.022f), Rgb(240, 236, 190), soft: 0.004f);
        PokeBuilder.Both(s =>
        {
            b.Tube(head, Smooth(2, c + V(0.012f * s, 0.02f, 0.025f), c + V(0.03f * s, 0.01f, 0.06f), c + V(0.04f * s, 0.03f, 0.08f)), 0.006f, 0.004f, green, Shell, 0.003f);
        });
        foreach (var leg in Enumerable.Range(0, b.Model.Bones.Count).Where(i => HasRole(b, i, PokeRole.Leg)).ToArray())
        {
            Tint(b, s => s.Bone == leg, Rgb(46, 112, 82));
            var root = b.Model.Skeleton[leg].Joint;
            Grow(b, V(1.5f, 1.3f, 1.5f), root, bone => bone == leg);
        }
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2956 Mirastrich, from Espathra

    /// <summary>
    /// Mirastrich: Espathra taller, pink and lilac, its hood of feathers opened into a full ring of eye feathers round
    /// its head whose eyes glow pink and blue, and a train of long feathers falling behind to the ground.
    /// </summary>
    private static PokeBuilder Mirastrich()
    {
        var b = Become(Espathra(), "Mirastrich", 1.0f);
        var bc = V(0, 0.54f, -0.02f);
        var c = bc + V(0, 0.44f, 0.15f);
        var pink = Rgb(226, 128, 176);
        var lilac = Rgb(190, 168, 236);
        var cream = Rgb(248, 234, 176);
        Recolour(b, (Rgb(246, 182, 82), pink), (Rgb(250, 232, 124), lilac), (Rgb(246, 244, 236), cream), (Rgb(150, 112, 190), Rgb(238, 186, 204)));
        int head = BoneOf(b, "head");
        // The ring of eye feathers round its head
        var hub = c + V(0, 0.03f, -0.07f);
        for (int i = 0; i < 16; i++)
        {
            float a = i * MathF.Tau / 16f;
            var d = V(MathF.Cos(a), MathF.Sin(a), -0.15f);
            var tip = hub + d * 0.27f;
            Frond(b, head, hub + d * 0.04f, tip, 0.042f, i % 2 == 0 ? cream : Rgb(240, 214, 236), V(0, 0, 1f), 0.2f, Fur, 0.008f);
            PaintAs(b, head, hub + d * 0.22f + V(0, 0, 0.01f), V(0.03f, 0.03f, 0.02f), i % 2 == 0 ? Rgb(150, 120, 240) : Rgb(236, 120, 200), Glow);
            b.PaintEll(head, hub + d * 0.22f + V(0, 0, 0.01f), V(0.013f, 0.013f, 0.02f), Rgb(90, 180, 240), soft: 0.004f);
        }
        // A train of long feathers falling behind
        for (int i = 0; i < 5; i++)
        {
            float x = (i - 2) * 0.05f;
            var root = bc + V(x * 0.6f, -0.02f, -0.14f);
            Frond(b, Body, root, V(x * 1.6f, 0.06f, -0.48f + MathF.Abs(x) * 0.6f), 0.05f, i % 2 == 0 ? lilac : pink, V(0, 1f, -0.3f), 0.2f, Fur, 0.01f);
        }
        Stretch(b, V(1f, 1.08f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2961 Wugsextet, from Wugtrio

    /// <summary>
    /// Wugsextet: Wugtrio grown into a colony of six red garden eels rising through a heap of earth in two rings of
    /// three, the middle ones tallest, each with yellow lips and a line of pale spots down its sides.
    /// </summary>
    private static PokeBuilder Wugsextet()
    {
        var b = new PokeBuilder("Wugsextet", 0.88f, BodyPlan.Biped, V(0, 0.12f, 0)) { Coat = Scales };
        var red = Rgb(214, 80, 72);
        var earth = Rgb(110, 98, 78);
        var spot = Rgb(246, 226, 196);
        foreach (var (at, half, turn) in new[]
        {
            (V(0, 0.07f, 0.0f), V(0.16f, 0.07f, 0.12f), V(5f, 20f, 8f)), (V(-0.17f, 0.05f, 0.06f), V(0.09f, 0.05f, 0.08f), V(-10f, -25f, 15f)),
            (V(0.18f, 0.05f, 0.05f), V(0.09f, 0.05f, 0.08f), V(10f, 30f, -12f)), (V(0.0f, 0.13f, -0.02f), V(0.08f, 0.05f, 0.07f), V(20f, 45f, 10f)),
            (V(0.05f, 0.04f, 0.13f), V(0.08f, 0.04f, 0.06f), V(0f, 10f, 6f)), (V(-0.08f, 0.04f, -0.1f), V(0.08f, 0.04f, 0.06f), V(6f, -10f, 0f)),
        })
            b.Box(Body, at, half, 0.03f, earth, turn, Shell, 0.02f);
        foreach (var (name, x, z, height, phase) in new[]
        {
            ("headM", 0f, -0.05f, 0.62f, 0f), ("headL", -0.14f, -0.06f, 0.48f, 1.7f), ("headR", 0.15f, -0.05f, 0.5f, 3.1f),
            ("head4", -0.21f, 0.07f, 0.34f, 4.2f), ("head5", 0.06f, 0.08f, 0.3f, 5.1f), ("head6", 0.24f, 0.08f, 0.4f, 0.8f),
        })
        {
            int head = name == "headM" ? b.Head(V(x, height * 0.8f, z)) : b.Part(name, Body, V(x, height * 0.8f, z), PokeRole.Head, phase, MathF.Sign(x));
            var root = V(x, 0.05f, z);
            GardenEel(b, Body, head, root, height, 0.036f, red, red, true);
            var path = Smooth(3, root, root + V(0, height * 0.45f, -height * 0.05f), root + V(0, height * 0.82f, -height * 0.04f), root + V(0, height * 0.9f, height * 0.02f));
            for (int i = 3; i < path.Length - 1; i++)
                foreach (float s in new[] { -1f, 1f })
                    b.PaintEll(Body, path[i] + V(0.033f * s, 0, 0.012f), V(0.007f, 0.007f, 0.007f), spot, soft: 0.003f);
        }
        Recolour(b, (Rgb(246, 244, 240), Rgb(244, 204, 64)));
        return b;
    }

    // ------------------------------------------------------------------ 2962 Bomberon, from Bombirdier

    /// <summary>
    /// Bomberon: Bombirdier larger, white with black flight feathers, its bill grown heavy, grey and hooked, a black
    /// band over its eyes like goggles, and a satchel slung across its chest with a boulder in it.
    /// </summary>
    private static PokeBuilder Bomberon()
    {
        var b = Become(Bombirdier(), "Bomberon", 1.0f);
        var bc = V(0, 0.38f, -0.02f);
        var c = bc + V(0, 0.44f, 0.06f);
        int head = BoneOf(b, "head");
        b.Sdf.RemoveAll(s => s.Bone == head && Is(s.Color, Rgb(230, 112, 112)));
        Recolour(b, (Rgb(200, 196, 208), Rgb(232, 232, 236)), (Rgb(210, 72, 82), Rgb(112, 112, 118)));
        // The heavy hooked bill
        var bill = Smooth(3, c + V(0, -0.005f, 0.03f), c + V(0, -0.02f, 0.1f), c + V(0, -0.035f, 0.16f), c + V(0, -0.07f, 0.17f));
        b.Tube(head, bill, 0.03f, 0.009f, Rgb(128, 128, 134), Shell, 0.006f);
        // Goggles of black feathers over the eyes
        b.PaintEll(head, c + V(0, 0.012f, 0.02f), V(0.055f, 0.016f, 0.045f), Rgb(36, 36, 42), soft: 0.004f);
        // The satchel across its chest, a boulder in it
        var sack = Rgb(104, 100, 86);
        b.PaintTorus(Body, bc, 0.112f, 0.014f, sack, V(-15f, 0, 38f));
        b.Ell(Body, bc + V(0.035f, -0.075f, 0.095f), V(0.08f, 0.06f, 0.05f), sack, blend: 0.02f);
        b.Ell(Body, bc + V(0.03f, -0.035f, 0.12f), V(0.055f, 0.042f, 0.036f), Rgb(116, 112, 106), mat: Shell, blend: 0.008f);
        Stretch(b, V(1.04f, 1.06f, 1.04f));
        return b;
    }

    // ------------------------------------------------------------------ 2964 Zerofin, from Palafin

    /// <summary>
    /// Zerofin: Palafin in its Zero Form grown larger, a deeper blue with navy along its back, its fins longer and
    /// swept back, a pale streak down its back like a racing stripe, its little smile kept.
    /// </summary>
    private static PokeBuilder Zerofin()
    {
        var b = Become(DolphinBuild(true), "Zerofin", 0.85f);
        var bc = V(0, 0.25f, 0);
        Unlift(b, bc);
        const float k = 1.1f;
        var blue = Rgb(64, 152, 200);
        var navy = Rgb(36, 70, 124);
        var teal = Rgb(96, 206, 196);
        b.Sdf.RemoveAll(s => s.Bone == Body && Is(s.Color, DolphinDark));
        Recolour(b, (DolphinBlue, blue));
        // The swept-back dorsal fin, navy
        Blade(b, Body, bc + V(0, 0.07f, 0.04f) * k, bc + V(0, 0.24f, -0.17f) * k, 0.055f * k, navy, V(1f, 0, 0), 0.25f);
        // Longer pectoral fins, swept back
        PokeBuilder.Both(s =>
        {
            int fin = BoneOf(b, s < 0 ? "finL" : "finR");
            var root = bc + V(0.07f * s, -0.04f, 0.06f) * k;
            Grow(b, V(1.3f, 1.3f, 1.45f), root, bone => bone == fin);
        });
        int tail = BoneOf(b, "tail");
        Grow(b, V(1.3f, 1.2f, 1.3f), bc + V(0, 0, -0.24f) * k, bone => bone == tail);
        // The racing stripe down its back and teal panels on its flanks
        b.PaintEll(Body, bc + V(0, 0.09f, -0.02f) * k, V(0.022f, 0.02f, 0.2f) * k, Rgb(206, 246, 246), soft: 0.008f);
        PokeBuilder.Both(s => b.PaintEll(Body, bc + V(0.08f * s, 0.03f, -0.03f) * k, V(0.03f, 0.035f, 0.12f) * k, teal, soft: 0.01f));
        b.PaintEll(Body, bc + V(0, 0.06f, -0.06f) * k, V(0.09f, 0.03f, 0.16f) * k, navy, soft: 0.02f);
        Stretch(b, V(1f, 1f, 1.12f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2966 Turbovroom, from Revavroom

    /// <summary>
    /// Turbovroom: Revavroom longer and lower, a racing machine of dark gunmetal and purple, two more exhaust pipes
    /// standing up at its back, a spoiler of steel over its rear and headlights glowing yellow at its front.
    /// </summary>
    private static PokeBuilder Turbovroom()
    {
        var b = Become(Revavroom(), "Turbovroom", 1.0f);
        var bc = V(0, 0.27f, 0);
        Recolour(b, (EngineGrey, Rgb(84, 82, 96)), (Rgb(96, 72, 128), Rgb(88, 52, 128)));
        var steel = Rgb(170, 172, 182);
        // Four pipes standing up at its back
        foreach (var x in new[] { -0.06f, 0.06f })
        {
            b.Tube(Body, Smooth(3, bc + V(x, 0.06f, -0.06f), bc + V(x * 1.1f, 0.18f, -0.09f), bc + V(x * 1.2f, 0.28f, -0.11f)), 0.022f, 0.02f, steel, Metal, 0.006f);
            b.Ell(Body, bc + V(x * 1.2f, 0.28f, -0.11f), V(0.028f, 0.028f, 0.028f), steel, mat: Metal, blend: 0.006f);
            b.PaintEll(Body, bc + V(x * 1.2f, 0.3f, -0.11f), V(0.03f, 0.02f, 0.03f), EnginePurple, soft: 0.008f);
        }
        // The spoiler over its rear
        PokeBuilder.Both(s => b.Limb(Body, bc + V(0.07f * s, 0.05f, -0.12f), bc + V(0.08f * s, 0.17f, -0.2f), 0.012f, 0.01f, steel, Metal, 0.006f));
        b.Box(Body, bc + V(0, 0.18f, -0.21f), V(0.17f, 0.008f, 0.045f), 0.006f, steel, V(-12f, 0, 0), Metal, 0.006f);
        PokeBuilder.Both(s => b.Box(Body, bc + V(0.17f * s, 0.15f, -0.21f), V(0.006f, 0.04f, 0.05f), 0.004f, steel, default, Metal, 0.004f));
        // Headlights at its front
        int head = BoneOf(b, "head");
        var face = bc + V(0, 0, 0.2f);
        PokeBuilder.Both(s => b.Ell(head, face + V(0.058f * s, 0.035f, 0.035f), V(0.017f, 0.014f, 0.012f), Rgb(255, 236, 110), mat: Glow, blend: 0.004f));
        Stretch(b, V(1f, 0.9f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2967 Velocidrake, from Cyclizar

    /// <summary>
    /// Velocidrake: Cyclizar larger and longer, its tyre gone into a saddle of hard brown and orange plates over its
    /// back, two short horns swept back from its helmet and its tail a long whip of green with an orange tip.
    /// </summary>
    private static PokeBuilder Velocidrake()
    {
        var b = Become(Cyclizar(), "Velocidrake", 1.0f);
        var bc = V(0, 0.53f, 0.02f);
        var br = V(0.08f, 0.14f, 0.08f);
        var c = bc + V(0, 0.21f, 0.09f);
        var green = Rgb(128, 180, 120);
        b.Sdf.RemoveAll(s => s.Bone == Body && s.Shape == SdfShape.Torus && Is(s.Color, Rgb(40, 40, 46)));
        // The saddle of plates
        var turn = V(-15f, 0, 0);
        foreach (var (d, col) in new[] { (V(0, 0.4f, -1f), Rgb(140, 92, 60)), (V(0.6f, 0.2f, -0.8f), Rgb(204, 104, 48)), (V(-0.6f, 0.2f, -0.8f), Rgb(204, 104, 48)),
                     (V(0.4f, -0.3f, -0.9f), Rgb(140, 92, 60)), (V(-0.4f, -0.3f, -0.9f), Rgb(140, 92, 60)), (V(0, -0.1f, -1f), Rgb(204, 104, 48)), (V(0, 0.75f, -0.6f), Rgb(204, 104, 48)) })
        {
            var at = Out(bc, br, turn, d);
            Plate(b, Body, at, Vector3.Normalize(d), V(0.07f, 0.016f, 0.058f), col, Shell);
        }
        // Two short horns swept back
        int head = BoneOf(b, "head");
        PokeBuilder.Both(s => b.Spike(head, c + V(0.03f * s, 0.07f, -0.07f), c + V(0.05f * s, 0.15f, -0.18f), 0.014f, green, 0.7f));
        // The tail run on into a long whip, orange at its tip
        int tail = BoneOf(b, "tail");
        var end = bc + V(0, -0.05f, -0.45f);
        var whip = Smooth(3, end + V(0, 0.01f, 0.02f), bc + V(0.02f, -0.02f, -0.58f), bc + V(0.05f, -0.12f, -0.7f), bc + V(0.06f, -0.22f, -0.72f));
        b.Tube(tail, whip, 0.013f, 0.009f, green, blend: 0f);
        Blade(b, tail, whip[^2], whip[^1] + V(0.04f, -0.08f, 0.06f), 0.025f, Rgb(240, 110, 40), V(1f, 0, 0), 0.3f);
        Stretch(b, V(1.05f, 1.06f, 1.08f));
        return b;
    }

    // ------------------------------------------------------------------ 2968 Orthobore, from Orthworm

    /// <summary>
    /// Orthobore: Orthworm longer and pinker, every segment banded in a plate of steel, and its head a drill of three
    /// ridged cones of steel thrust forward.
    /// </summary>
    private static PokeBuilder Orthobore()
    {
        var b = Become(Orthworm(), "Orthobore", 1.0f);
        Recolour(b, (Rgb(232, 98, 82), Rgb(196, 112, 112)));
        var steel = Rgb(138, 142, 150);
        var path = Smooth(2, V(0.44f, 0.07f, -0.02f), V(0.3f, 0.1f, -0.05f), V(0.14f, 0.11f, -0.02f), V(-0.02f, 0.13f, 0.02f), V(-0.12f, 0.24f, 0.06f), V(-0.15f, 0.38f, 0.1f));
        for (int i = 0; i < path.Length; i += 1)
        {
            float t = (float)i / (path.Length - 1);
            float rr = 0.065f + 0.035f * t;
            int bone = i < 2 ? Body : BoneOf(b, "seg" + i);
            var axis = path[Math.Min(i + 1, path.Length - 1)] - path[Math.Max(i - 1, 0)];
            b.Torus(bone, path[i], rr * 0.98f, 0.016f, steel, Euler(axis), mat: Metal, blend: 0.004f);
        }
        // The drill of three cones, ridged, thrust forward from the head
        int head = BoneOf(b, "head");
        var c = path[^1] + V(-0.01f, 0.08f, 0.03f);
        b.Torus(head, c + V(0, 0, 0.06f), 0.1f, 0.022f, steel, V(90f, 0, 0), mat: Metal, blend: 0.006f);
        foreach (var (o, len) in new[] { (V(0, 0.035f, 0.08f), 0.24f), (V(-0.05f, -0.04f, 0.08f), 0.2f), (V(0.05f, -0.04f, 0.08f), 0.2f) })
            Drill(b, head, c + o, c + o + V(o.X * 0.4f, o.Y * 0.4f, len), 0.05f, steel, Rgb(80, 82, 90), 4);
        Stretch(b, V(1.1f, 1f, 1.1f));
        return b;
    }

    // ------------------------------------------------------------------ 2970 Geodiadem, from Glimmora

    /// <summary>
    /// Geodiadem: Glimmora larger, its petals indigo, a second ring of violet crystal petals spread round it with their
    /// tips lit pink, and a crown of small pink crystals over its face.
    /// </summary>
    private static PokeBuilder Geodiadem()
    {
        var b = Become(Glimmora(), "Geodiadem", 1.0f);
        var bc = V(0, 0.4f, -0.03f);
        Unlift(b, V(0, 0.4f, 0));
        var indigo = Rgb(62, 54, 124);
        var violet = Rgb(132, 86, 196);
        var pink = Rgb(246, 120, 226);
        Recolour(b, (CrystalBlue, indigo), (CrystalTeal, Rgb(200, 100, 220)));
        // The second ring of petals
        for (int i = 0; i < 9; i++)
        {
            float a = i * MathF.Tau / 9f + 0.15f;
            var n = V(MathF.Cos(a), MathF.Sin(a), 0);
            var root = bc + n * 0.13f + V(0, 0, -0.06f);
            var tip = bc + n * 0.38f + V(0, 0, -0.1f);
            Blade(b, Body, root, tip, 0.065f, violet, V(0, 0, 1f), 0.25f, Shell);
            PaintAs(b, Body, Vector3.Lerp(root, tip, 0.88f), V(0.05f, 0.05f, 0.05f), pink, Glow);
        }
        // A crown of small crystals over its face
        for (int i = 0; i < 7; i++)
        {
            float x = (i - 3) * 0.035f;
            var root = bc + V(x, 0.14f - MathF.Abs(x) * 0.3f, 0.1f);
            b.Spike(Body, root, root + V(x * 0.4f, 0.08f + (i % 2) * 0.03f, 0.01f), 0.016f, pink, mat: Glow, blend: 0.005f);
        }
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2972 Barrowhound, from Houndstone

    /// <summary>
    /// Barrowhound: Houndstone longer and paler, its gravestone grown into a cairn of three mossy stones on its back, a
    /// candle burning with a white flame at its brow, and its ears long and ragged.
    /// </summary>
    private static PokeBuilder Barrowhound()
    {
        var b = Become(Houndstone(), "Barrowhound", 1.0f);
        var bc = V(0, 0.33f, -0.05f);
        var c = bc + V(0, 0.07f, 0.28f);
        int head = BoneOf(b, "head");
        var stone = Rgb(146, 146, 158);
        b.Sdf.RemoveAll(s => s.Bone == head && s.A.Y > c.Y + 0.06f);
        var pale = Rgb(206, 214, 210);
        Recolour(b, (Rgb(172, 170, 216), pale));
        // The cairn on its back, mossy at its foot
        foreach (var (y, r) in new[] { (0.15f, V(0.09f, 0.045f, 0.075f)), (0.22f, V(0.066f, 0.036f, 0.054f)), (0.275f, V(0.042f, 0.03f, 0.036f)) })
            b.Ell(Body, bc + V(0.005f * (y * 30f % 2f), y, -0.03f), r, Rgb(150, 148, 140), V(0, y * 200f, (y - 0.2f) * 40f), Shell, 0.012f);
        b.PaintEll(Body, bc + V(0, 0.125f, -0.03f), V(0.1f, 0.03f, 0.085f), Rgb(110, 140, 80), soft: 0.012f);
        // A candle at its brow with a white flame
        b.Limb(head, c + V(0, 0.05f, 0.04f), c + V(0, 0.11f, 0.045f), 0.013f, 0.012f, Rgb(244, 244, 236), Shell, 0.006f);
        int flame = b.Part("flame", head, c + V(0, 0.12f, 0.045f), PokeRole.Flame, 0.5f);
        b.Ell(flame, c + V(0, 0.135f, 0.045f), V(0.016f, 0.026f, 0.016f), Rgb(236, 250, 246), mat: Glow, blend: 0.006f);
        b.Spike(flame, c + V(0, 0.15f, 0.045f), c + V(0.004f, 0.19f, 0.045f), 0.013f, Rgb(236, 250, 246), mat: Glow, blend: 0.006f);
        // Long ragged ears
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.07f * s, 0.03f, -0.04f));
            Frond(b, ear, c + V(0.07f * s, 0.03f, -0.04f), c + V(0.1f * s, -0.16f, -0.01f), 0.03f, pale, V(s, 0, 0.2f), 0.3f, Fur, 0.008f);
            foreach (float t in new[] { -0.12f, -0.16f })
                b.Spike(ear, c + V(0.095f * s, t, -0.015f), c + V(0.1f * s, t - 0.05f, -0.01f), 0.01f, pale, 0.5f, blend: 0.004f);
        });
        _ = stone;
        Stretch(b, V(1f, 1.05f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2973 Flamenkick, from Flamigo

    /// <summary>
    /// Flamenkick: Flamigo taller, a ruffled collar of pink, red and orange frills at its neck like a dancer's, a long
    /// train of red tail feathers sweeping the ground, red tufts at its knees and a dark red crest.
    /// </summary>
    private static PokeBuilder Flamenkick()
    {
        var b = Become(Flamigo(), "Flamenkick", 1.0f);
        var bc = V(0, 0.58f, -0.03f);
        var c = bc + V(0, 0.425f, 0.04f);
        var pink = Rgb(234, 106, 136);
        var red = Rgb(214, 46, 62);
        var orange = Rgb(244, 120, 50);
        // The ruffled collar round the base of its neck, three rings of frills
        foreach (var (y, reach, col) in new[] { (0.02f, 0.11f, orange), (0.05f, 0.1f, red), (0.08f, 0.085f, pink) })
            for (int i = 0; i < 12; i++)
            {
                float a = i * MathF.Tau / 12f + y * 10f;
                var n = V(MathF.Sin(a), 0, MathF.Cos(a));
                var root = bc + V(0, y + 0.03f, 0.07f) + n * 0.03f;
                Frond(b, Body, root, root + n * reach + V(0, -0.02f, 0), 0.035f, col, V(0, 1f, 0), 0.25f, Fur, 0.008f);
            }
        // The train of tail feathers sweeping the ground
        int tail = BoneOf(b, "tail");
        for (int i = 0; i < 5; i++)
        {
            float x = (i - 2) * 0.035f;
            Frond(b, tail, bc + V(x * 0.5f, 0.0f, -0.1f), V(x * 2.5f, 0.05f, -0.5f), 0.045f, i % 2 == 0 ? red : Rgb(188, 40, 84), V(0, 1f, -0.2f), 0.2f, Fur, 0.01f);
        }
        // Red tufts at its knees and a dark red crest
        b.Ell(BoneOf(b, "legR"), V(0.03f, 0.27f, 0.02f), V(0.024f, 0.03f, 0.024f), red, blend: 0.008f);
        b.Ell(BoneOf(b, "legL"), V(-0.035f, 0.32f, 0.02f), V(0.024f, 0.03f, 0.024f), red, blend: 0.008f);
        int head = BoneOf(b, "head");
        foreach (var (x, h) in new[] { (-0.012f, 0.05f), (0f, 0.07f), (0.012f, 0.05f) })
            b.Spike(head, c + V(x, 0.02f, -0.01f), c + V(x * 1.5f, 0.02f + h, -0.05f), 0.012f, Rgb(130, 30, 50), 0.5f);
        Stretch(b, V(1f, 1.08f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2975 Cetiberg, from Cetitan

    /// <summary>
    /// Cetiberg: Cetitan larger, an iceberg of peaks crusted on its back, its horn grown longer and clear, and two
    /// heavy tusks of ice hanging from its mouth.
    /// </summary>
    private static PokeBuilder Cetiberg()
    {
        var b = Become(Cetitan(), "Cetiberg", 1.0f);
        var bc = V(0, 0.3f, -0.02f);
        var c = bc + V(0, 0.04f, 0.2f);
        var berg = Rgb(214, 232, 246);
        var clear = Rgb(196, 226, 250);
        foreach (var (root, tip, r) in new[]
        {
            (bc + V(0, 0.1f, -0.04f), bc + V(0.01f, 0.44f, -0.07f), 0.13f), (bc + V(0.1f, 0.1f, 0.04f), bc + V(0.13f, 0.3f, 0.03f), 0.08f),
            (bc + V(-0.1f, 0.1f, 0.03f), bc + V(-0.12f, 0.32f, 0.0f), 0.085f), (bc + V(0.08f, 0.1f, -0.13f), bc + V(0.1f, 0.27f, -0.17f), 0.075f),
            (bc + V(-0.07f, 0.1f, -0.14f), bc + V(-0.09f, 0.25f, -0.19f), 0.07f), (bc + V(0, 0.1f, 0.1f), bc + V(0.01f, 0.24f, 0.1f), 0.07f),
        })
        {
            b.Spike(Body, root, tip, r, berg, 0.85f, Shell, 0.03f);
            b.PaintEll(Body, Vector3.Lerp(root, tip, 0.7f), V(r * 0.5f, 0.06f, r * 0.5f), Rgb(244, 250, 255), soft: 0.02f);
        }
        int head = BoneOf(b, "head");
        b.Spike(head, c + V(0, 0.06f, 0.07f), c + V(0, 0.24f, 0.44f), 0.065f, clear, mat: Shell, blend: 0.01f);
        PokeBuilder.Both(s => b.Spike(head, c + V(0.07f * s, -0.06f, 0.08f), c + V(0.09f * s, -0.25f, 0.17f), 0.026f, clear, mat: Shell, blend: 0.006f));
        Stretch(b, V(1.05f, 1.05f, 1.05f));
        return b;
    }

    // ------------------------------------------------------------------ 2976 Sashimaru, from Veluza

    /// <summary>
    /// Sashimaru: Veluza longer, slate grey with a wide pink stripe down its side, its fins grown into a row of pink
    /// and silver blades along its back, and its head finned like a helm.
    /// </summary>
    private static PokeBuilder Sashimaru()
    {
        var b = Become(Veluza(), "Sashimaru", 1.0f);
        var bc = V(0, 0.3f, 0);
        Unlift(b, bc);
        var slate = Rgb(72, 96, 116);
        var silver = Rgb(196, 204, 216);
        var pink = Rgb(222, 92, 162);
        Recolour(b, (Rgb(182, 186, 198), slate), (Rgb(222, 224, 232), Rgb(130, 142, 156)));
        b.PaintEll(Body, bc + V(0, 0.005f, -0.02f), V(0.075f, 0.018f, 0.26f), pink, soft: 0.008f);
        // The row of blades along its back
        for (int i = 0; i < 7; i++)
        {
            float z = 0.17f - i * 0.055f;
            float h = 0.16f - MathF.Abs(i - 2) * 0.02f;
            Blade(b, Body, bc + V(0, 0.06f, z), bc + V(0, 0.06f + h, z - 0.09f), 0.035f, i % 2 == 0 ? pink : silver, V(1f, 0, 0), 0.22f, Metal);
        }
        // The helm: blades swept back from its brow
        PokeBuilder.Both(s => Blade(b, Body, bc + V(0.035f * s, 0.05f, 0.24f), bc + V(0.06f * s, 0.12f, 0.13f), 0.025f, silver, V(s, 0.3f, 0), 0.25f, Metal));
        b.Spike(Body, bc + V(0, 0.05f, 0.26f), bc + V(0, 0.1f, 0.19f), 0.02f, silver, 0.4f, Metal);
        Stretch(b, V(1f, 1f, 1.15f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2977 Grandozo, from Dondozo

    /// <summary>
    /// Grandozo: Dondozo larger and a deeper navy, its lips a deep pink, its fins magenta, a pale bumpy ridge along its
    /// back and its whiskers grown long, trailing pink and blue.
    /// </summary>
    private static PokeBuilder Grandozo()
    {
        var b = Become(Dondozo(), "Grandozo", 1.0f);
        Unlift(b, V(0, 0.3f, 0));
        var bc = V(0, 0.3f, 0.04f);
        var br = V(0.2f, 0.17f, 0.3f);
        var lip = Rgb(206, 84, 120);
        Recolour(b, (Rgb(42, 124, 204), Rgb(52, 82, 150)), (Rgb(30, 84, 152), Rgb(38, 56, 108)), (Rgb(232, 236, 242), Rgb(176, 66, 112)), (Rgb(196, 212, 228), Rgb(232, 196, 212)));
        // Lips
        b.Ell(Body, bc + V(0, -0.005f, 0.285f), V(0.12f, 0.022f, 0.04f), lip, blend: 0.01f);
        b.Ell(Body, bc + V(0, -0.095f, 0.275f), V(0.11f, 0.024f, 0.04f), lip, blend: 0.01f);
        // The pale bumpy ridge along its back
        for (int i = 0; i < 12; i++)
        {
            float z = 0.18f - i * 0.035f;
            float x = (i % 3 - 1) * 0.04f;
            var at = Out(bc, br, default, V(x, 1f, z / br.Z * 0.6f));
            b.Ell(Body, at, V(0.03f, 0.018f, 0.03f), Rgb(228, 216, 204), blend: 0.012f);
        }
        // Long trailing whiskers
        PokeBuilder.Both(s =>
        {
            foreach (var (dy, col, reach) in new[] { (0f, Rgb(214, 120, 160), 1f), (-0.03f, Rgb(84, 112, 186), 0.85f) })
            {
                var root = bc + V(0.11f * s, -0.06f + dy, 0.24f);
                b.Tube(Body, Smooth(3, root, root + V(0.08f * s, -0.12f, 0.02f) * reach, root + V(0.14f * s, -0.26f, -0.1f) * reach, root + V(0.2f * s, -0.3f, -0.32f) * reach, root + V(0.22f * s, -0.26f, -0.5f) * reach), 0.011f, 0.006f, col, blend: 0.004f);
            }
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2978 Tatsushogun, from Tatsugiri

    /// <summary>
    /// Tatsushogun: Tatsugiri in its Curly Form grown larger and navy, its curled fin become an admiral's hat trimmed
    /// in gold, a cape of fin down its back edged teal and gold, a pale baton of a fin under its chin and gold whiskers.
    /// </summary>
    private static PokeBuilder Tatsushogun()
    {
        var b = Become(TatsugiriBuild(0, false), "Tatsushogun", 0.52f);
        var bc = V(0, 0.13f, 0);
        Unlift(b, bc);
        var c = bc + V(0, 0.09f, 0.012f);
        var navy = Rgb(46, 70, 128);
        var gold = Rgb(234, 180, 72);
        var teal = Rgb(120, 206, 196);
        int crest = BoneOf(b, "crest");
        b.Sdf.RemoveAll(s => s.Bone == crest);
        Recolour(b, (Rgb(242, 128, 92), navy), (Rgb(252, 216, 198), gold), (Rgb(204, 78, 64), Rgb(30, 44, 92)));
        int head = BoneOf(b, "head");
        // The admiral's hat: a bicorne set across its head, its points turned up, trimmed in gold
        var hat = c + V(0, 0.045f, -0.006f);
        b.Ell(crest, hat, V(0.075f, 0.024f, 0.04f), navy, blend: 0.008f);
        b.Ell(crest, hat + V(0, 0.022f, -0.005f), V(0.045f, 0.026f, 0.03f), navy, blend: 0.008f);
        PokeBuilder.Both(s => b.Spike(crest, hat + V(0.06f * s, 0.005f, 0), hat + V(0.095f * s, 0.04f, -0.005f), 0.02f, navy, 0.55f));
        b.PaintTorus(head, hat + V(0, -0.004f, 0), 0.06f, 0.006f, gold, default, 1.2f, 0.62f);
        b.PaintEll(head, hat + V(0, 0.03f, 0.02f), V(0.012f, 0.014f, 0.012f), gold, soft: 0.003f);
        // The cape down its back, edged teal and gold
        b.Ell(Body, bc + V(0, 0.015f, -0.048f), V(0.07f, 0.065f, 0.012f), navy, V(-12f, 0, 0), blend: 0.008f);
        b.PaintEll(Body, bc + V(0, -0.045f, -0.058f), V(0.075f, 0.016f, 0.03f), teal, soft: 0.005f);
        b.PaintEll(Body, bc + V(0, -0.03f, -0.058f), V(0.075f, 0.005f, 0.03f), gold, soft: 0.003f);
        // The baton under its chin and the gold whiskers
        b.Limb(head, c + V(-0.045f, -0.045f, 0.032f), c + V(0.045f, -0.048f, 0.034f), 0.008f, 0.008f, Rgb(244, 236, 214), Shell, 0.004f);
        PokeBuilder.Both(s => b.Tube(head, Smooth(2, c + V(0.02f * s, -0.018f, 0.044f), c + V(0.05f * s, -0.022f, 0.05f), c + V(0.065f * s, -0.01f, 0.045f)), 0.0045f, 0.0035f, gold, blend: 0.002f));
        Stretch(b, V(1.12f, 1.12f, 1.12f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2980 Clodkaiser, from Clodsire

    /// <summary>
    /// Clodkaiser: Clodsire larger and placid, its spines grown into a crown of purple poison quills over its back,
    /// purple spots along its flanks and its tail broader.
    /// </summary>
    private static PokeBuilder Clodkaiser()
    {
        var b = Become(Clodsire(), "Clodkaiser", 0.95f);
        var bc = V(0, 0.2f, -0.04f);
        var br = V(0.25f, 0.165f, 0.3f);
        var purple = Rgb(124, 82, 150);
        var drip = Rgb(176, 140, 206);
        Recolour(b, (Rgb(224, 208, 238), purple));
        foreach (float x in new[] { -0.1f, -0.05f, 0f, 0.05f, 0.1f })
            for (int i = 0; i < 6; i++)
            {
                float z = 0.15f - i * 0.065f + (x == 0f ? 0f : 0.03f);
                var root = Out(bc, br, default, V(x / br.X, 1f, z / br.Z));
                var n = Outward(bc, br, root);
                float len = (x == 0f ? 0.17f : 0.12f) - MathF.Abs(i - 2) * 0.012f;
                b.Spike(Body, root - n * 0.01f, root + Vector3.Normalize(n + V(0, 0.3f, -0.3f)) * len, 0.026f, purple, 0.6f, Shell, 0.008f);
                b.PaintEll(Body, root + Vector3.Normalize(n + V(0, 0.3f, -0.3f)) * len * 0.75f, V(0.012f, 0.03f, 0.012f), drip, soft: 0.006f);
            }
        PokeBuilder.Both(s =>
        {
            foreach (var (y, z, size) in new[] { (0.05f, 0.05f, 0.022f), (0.0f, -0.05f, 0.026f), (0.06f, -0.12f, 0.02f), (-0.04f, -0.15f, 0.018f), (0.02f, -0.2f, 0.022f), (-0.06f, 0.0f, 0.018f) })
                b.PaintEll(Body, Out(bc, br, default, V(s, y / br.Y, z / br.Z * 0.8f)), V(size, size * 0.8f, size), Rgb(150, 126, 176), soft: 0.004f);
        });
        int tail = BoneOf(b, "tail");
        Grow(b, V(1.35f, 1.1f, 1.25f), bc + V(0, -0.07f, -0.3f), bone => bone == tail);
        Stretch(b, V(1.06f, 1.06f, 1.06f));
        return b;
    }

    // ------------------------------------------------------------------ 2981 Farigigiraf, from Farigiraf

    /// <summary>
    /// Farigigiraf: Farigiraf taller, striped in zigzags of purple, its tail's head grown a long neck of its own that
    /// rises from its rump and curves over so the two heads face each other across its back, and the horns of both
    /// heads longer, glowing violet at their tips.
    /// </summary>
    private static PokeBuilder Farigigiraf()
    {
        var b = Become(Farigiraf(), "Farigigiraf", 1.0f);
        var bc = V(0, 0.52f, -0.01f);
        var br = V(0.12f, 0.11f, 0.22f);
        var c = bc + V(0, 0.42f, 0.24f);
        var dark = Rgb(72, 58, 92);
        var violet = Rgb(206, 110, 230);
        var yellow = Rgb(240, 206, 110);
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail);
        b.Model.Decals.RemoveAll(d => d.Bone == tail);
        // Zigzag stripes of violet over its body and neck
        foreach (var d in new[] { V(1f, 0.5f, 0.6f), V(1f, 0.1f, -0.1f), V(0.7f, 0.7f, -0.5f), V(1f, -0.3f, 0.3f) })
            PokeBuilder.Both(s => b.PaintEll(Body, Out(bc, br, default, V(d.X * s, d.Y, d.Z)), V(0.018f, 0.06f, 0.03f), violet, V(0, 0, 30f * s), 0.006f));
        // The second head, now on a long neck rising from its rump and curving over to face the first
        var neck = Smooth(3, bc + V(0, 0.05f, -0.2f), bc + V(0, 0.22f, -0.32f), bc + V(0, 0.38f, -0.3f), bc + V(0, 0.44f, -0.2f));
        b.Tube(tail, neck, 0.055f, 0.04f, dark, Shell, 0f);
        for (int i = 2; i < neck.Length - 1; i += 2)
            b.PaintEll(tail, neck[i] + V(0, 0.02f, -0.03f), V(0.03f, 0.025f, 0.03f), yellow, soft: 0.006f);
        var tc = bc + V(0, 0.46f, -0.13f);
        var tr = V(0.06f, 0.055f, 0.08f);
        b.Ell(tail, tc, tr, dark, mat: Shell, blend: 0.02f);
        b.Ell(tail, tc + V(0, -0.03f, 0.04f), V(0.045f, 0.022f, 0.05f), Rgb(52, 40, 66), mat: Shell, blend: 0.01f);
        foreach (float x in new[] { -0.025f, 0f, 0.025f })
            b.Spike(tail, tc + V(x, -0.03f, 0.075f), tc + V(x, -0.055f, 0.085f), 0.007f, White, mat: Shell, blend: 0.003f);
        PokeBuilder.Both(s =>
        {
            var at = Out(tc, tr, default, V(0.5f * s, 0.35f, 0.8f));
            b.Mark(tail, at, Outward(tc, tr, at), 0.014f, 0.01f, violet, MarkShape.Diamond);
            b.Tube(tail, Smooth(2, tc + V(0.02f * s, 0.04f, -0.02f), tc + V(0.04f * s, 0.12f, -0.05f), tc + V(0.05f * s, 0.2f, -0.06f)), 0.012f, 0.009f, dark, Shell, 0f);
            PaintAs(b, tail, tc + V(0.05f * s, 0.19f, -0.06f), V(0.018f, 0.03f, 0.018f), violet, Glow);
        });
        // The first head's horns run on, glowing at their ends
        int head = BoneOf(b, "head");
        PokeBuilder.Both(s =>
        {
            var knob = c + V(0.03f * s, 0.21f, -0.02f);
            b.Spike(head, knob, knob + V(0.02f * s, 0.11f, -0.04f), 0.014f, dark, mat: Shell, blend: 0.006f);
            PaintAs(b, head, knob + V(0.016f * s, 0.085f, -0.03f), V(0.02f, 0.03f, 0.02f), violet, Glow);
        });
        Stretch(b, V(1f, 1.06f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2982 Dunsprawl, from Dudunsparce

    /// <summary>
    /// Dunsprawl: Dudunsparce longer, lying in four segments banded blue, pale beneath, a drill of a tail behind them,
    /// short horns on its head and its little wings larger.
    /// </summary>
    private static PokeBuilder Dunsprawl()
    {
        var b = Become(DudunsparceBuild(true), "Dunsprawl", 0.95f);
        var yellow = Rgb(242, 224, 110);
        var blue = Rgb(70, 150, 196);
        var pale = Rgb(250, 242, 206);
        var sr = V(0.105f, 0.09f, 0.115f);
        var c2 = V(-0.03f, 0.1f, 0.04f - 0.42f);
        var c3 = V(0.03f, 0.1f, 0.04f - 0.63f);
        int tail = BoneOf(b, "tail");
        Move(b, bone => bone == tail, c3 - c2);
        int seg3 = b.Part("seg3", BoneOf(b, "seg2"), Vector3.Lerp(c2, c3, 0.5f), PokeRole.Segment, 3f);
        b.Limb(seg3, c2 + V(0, 0, -0.06f), c3, 0.075f, 0.08f, yellow);
        b.Ell(seg3, c3, sr, yellow);
        foreach (float dz in new[] { 0.05f, -0.01f, -0.07f })
            b.PaintEll(seg3, c3 + V(0, 0.085f, dz), V(0.13f, 0.06f, 0.016f), blue, soft: 0.008f);
        // Pale bellies under every segment
        foreach (var (bone, at) in new[] { (Body, V(0f, 0.1f, 0.04f)), (BoneOf(b, "seg1"), V(0.05f, 0.1f, -0.17f)), (BoneOf(b, "seg2"), c2), (seg3, c3) })
            b.PaintEll(bone, at + V(0, -0.07f, 0), V(0.11f, 0.04f, 0.12f), pale, soft: 0.012f);
        // Short horns on its head and larger wings
        int head = BoneOf(b, "head");
        var c = V(0, 0.22f, 0.24f);
        PokeBuilder.Both(s => b.Spike(head, c + V(0.05f * s, 0.08f, -0.02f), c + V(0.07f * s, 0.14f, -0.06f), 0.018f, Rgb(214, 196, 150), mat: Shell));
        foreach (var name in new[] { "wingL", "wingR" })
        {
            int wing = BoneOf(b, name);
            Grow(b, V(1.35f, 1.35f, 1.35f), b.Model.Skeleton[wing].Joint, bone => bone == wing);
        }
        return b;
    }

    // ------------------------------------------------------------------ 2984 Tuskendon, from Great Tusk

    /// <summary>
    /// Tuskendon: Great Tusk larger, its hide purple, its armour cracked like dry clay over its shoulders and back, a
    /// mane of coarse red hair down its spine to a red tuft on its tail, and a second, shorter pair of tusks under the
    /// first.
    /// </summary>
    private static PokeBuilder Tuskendon()
    {
        var b = Become(GreatTusk(), "Tuskendon", 1.0f);
        var bc = V(0, 0.32f, -0.02f);
        var ac = bc + V(0, 0.07f, 0);
        var ar = V(0.215f, 0.14f, 0.285f);
        var red = Rgb(170, 40, 44);
        Recolour(b, (Rgb(150, 150, 162), Rgb(112, 86, 120)), (Rgb(82, 64, 104), Rgb(156, 100, 66)), (Rgb(52, 40, 68), Rgb(92, 58, 40)), (Rgb(226, 86, 116), red));
        // Cracks over the armour
        foreach (var (d, rot) in new[] { (V(0.6f, 0.6f, 0.5f), 30f), (V(-0.6f, 0.6f, 0.5f), -40f), (V(0.8f, 0.3f, 0.1f), 70f), (V(-0.8f, 0.3f, -0.1f), -60f), (V(0.4f, 0.7f, -0.4f), 10f), (V(-0.4f, 0.7f, -0.4f), -15f) })
            b.PaintEll(Body, Out(ac, ar, default, d), V(0.004f, 0.04f, 0.012f), Rgb(80, 50, 36), V(0, 0, rot), 0.003f);
        // The mane down its spine
        for (int i = 0; i < 14; i++)
        {
            float z = 0.24f - i * 0.035f;
            foreach (float x in new[] { -0.025f, 0.025f })
            {
                var root = Out(ac, ar, default, V(x / ar.X, 1f, z / ar.Z));
                var n = Outward(ac, ar, root);
                b.Spike(Body, root - n * 0.01f, root + Vector3.Normalize(n + V(x * 8f, 0.2f, -0.8f)) * (0.1f - MathF.Abs(i - 4) * 0.004f), 0.022f, red, 0.5f, Fur, 0.01f);
            }
        }
        int tail = BoneOf(b, "tail");
        foreach (var d in new[] { V(0.4f, -0.5f, -0.6f), V(-0.4f, -0.5f, -0.6f), V(0, -0.8f, -0.4f) })
            b.Spike(tail, bc + V(0, -0.06f, -0.4f), bc + V(0, -0.06f, -0.4f) + Vector3.Normalize(d) * 0.08f, 0.02f, red, 0.5f, Fur, 0.008f);
        // The second pair of tusks under the first
        int head = BoneOf(b, "head");
        PokeBuilder.Both(s =>
        {
            var tusk = Smooth(3, V(0.05f * s, 0.2f, 0.37f), V(0.1f * s, 0.11f, 0.45f), V(0.09f * s, 0.07f, 0.53f), V(0.04f * s, 0.09f, 0.57f));
            b.Tube(head, tusk, 0.026f, 0.009f, Claw, Shell, 0f);
        });
        Stretch(b, V(1.05f, 1.06f, 1.05f));
        return b;
    }

    // ------------------------------------------------------------------ 2985 Shriekpuff, from Scream Tail

    /// <summary>
    /// Shriekpuff: Scream Tail rounder, its ruff grown into a cloud of pale fur round its whole head, its ears tipped
    /// pale violet, and its own tail grown long, curling up behind it with two tufts of fur, the last violet.
    /// </summary>
    private static PokeBuilder Shriekpuff()
    {
        var b = Become(ScreamTail(), "Shriekpuff", 0.66f);
        var c = V(0, 0.2f, 0);
        var r = V(0.17f, 0.16f, 0.16f);
        var cloud = Rgb(250, 234, 244);
        var violet = Rgb(186, 164, 230);
        // The cloud of fur round its head and over its crown, leaving its face
        for (int i = 0; i < 16; i++)
        {
            float a = i * MathF.Tau / 16f;
            var d = V(MathF.Cos(a) * 0.95f, MathF.Sin(a) * 0.95f + 0.1f, 0.3f);
            if (d.Y < -0.4f) continue;
            b.Ell(Body, Out(c, r, default, d), V(0.06f, 0.055f, 0.055f), cloud, blend: 0.02f);
        }
        foreach (var d in new[] { V(0, 1f, -0.2f), V(0.5f, 0.8f, -0.4f), V(-0.5f, 0.8f, -0.4f), V(0.8f, 0.4f, -0.6f), V(-0.8f, 0.4f, -0.6f), V(0, 0.6f, -0.8f), V(0.3f, 1f, 0.2f), V(-0.3f, 1f, 0.2f) })
            b.Ell(Body, Out(c, r, default, d), V(0.065f, 0.06f, 0.06f), cloud, blend: 0.02f);
        // Ears tipped violet
        PokeBuilder.Both(s =>
        {
            int ear = BoneOf(b, s < 0 ? "earL" : "earR");
            b.PaintEll(ear, V(0.145f * s, 0.42f, -0.01f), V(0.035f, 0.03f, 0.035f), violet, soft: 0.008f);
        });
        // The tail grown long, curling up behind, a pale tuft and a violet one
        int tail = BoneOf(b, "tail");
        var path = Smooth(3, V(0.12f, 0.13f, -0.22f), V(0.2f, 0.18f, -0.26f), V(0.26f, 0.3f, -0.24f), V(0.24f, 0.42f, -0.2f), V(0.18f, 0.48f, -0.18f));
        b.Tube(tail, path, 0.024f, 0.018f, Rgb(246, 172, 198), blend: 0f);
        foreach (var (at, col, rr) in new[] { (path[5], cloud, 0.045f), (path[^1], violet, 0.05f) })
        {
            b.Ell(tail, at, V(rr, rr * 0.9f, rr), col, blend: 0.012f);
            foreach (var d in new[] { V(1f, 0.4f, 0), V(-0.6f, 0.8f, 0.2f), V(0.2f, 0.8f, -0.6f) })
                b.Spike(tail, at, at + Vector3.Normalize(d) * rr * 1.6f, rr * 0.5f, col, 0.6f, blend: 0.01f);
        }
        Stretch(b, V(1.05f, 1.05f, 1.05f));
        return b;
    }

    // ------------------------------------------------------------------ 2986 Fungoliath, from Brute Bonnet

    /// <summary>
    /// Fungoliath: Brute Bonnet taller, its cap wider, dark maroon mottled with moss and spotted with pale scales, two
    /// rows of fangs ringing the dark gills under it, its body grown into a mossy trunk and its arms into thick roots.
    /// </summary>
    private static PokeBuilder Fungoliath()
    {
        var b = Become(BruteBonnet(), "Fungoliath", 0.88f);
        var c = V(0, 0.42f, 0);
        var maroon = Rgb(96, 42, 52);
        var gill = Rgb(52, 58, 44);
        var bark = Rgb(78, 72, 54);
        var moss = Rgb(96, 128, 70);
        int head = BoneOf(b, "head");
        int armL = BoneOf(b, "armL"), armR = BoneOf(b, "armR");
        b.Sdf.RemoveAll(s => (s.Bone == armL || s.Bone == armR) && !(Is(s.Color, Rgb(92, 88, 98))));
        b.Model.Decals.RemoveAll(d => d.Bone == armL || d.Bone == armR);
        Recolour(b, (Rgb(214, 62, 72), maroon), (Rgb(238, 234, 228), gill), (Rgb(92, 88, 98), bark), (Rgb(136, 102, 74), bark), (Rgb(108, 150, 82), moss));
        // The cap wider and raised on a trunk
        Grow(b, V(1.25f, 1.1f, 1.25f), c, bone => bone == head);
        Move(b, bone => bone == head, V(0, 0.07f, 0));
        c += V(0, 0.07f, 0);
        b.Limb(Body, V(0, 0.22f, 0), c + V(0, -0.04f, 0), 0.13f, 0.09f, bark, blend: 0.03f);
        // Pale scales on the cap
        foreach (var (x, z) in new[] { (0.1f, 0.12f), (-0.12f, 0.08f), (0.04f, -0.14f), (-0.16f, -0.1f), (0.18f, -0.04f), (-0.02f, 0.02f), (0.14f, 0.2f), (-0.2f, 0.16f), (0.22f, 0.1f) })
            b.PaintEll(head, c + V(x, 0.09f, z), V(0.02f, 0.03f, 0.016f), Rgb(214, 206, 186), soft: 0.004f);
        // Two rows of fangs under the rim
        foreach (var (rho, count, len) in new[] { (0.24f, 18, 0.05f), (0.17f, 13, 0.04f) })
            for (int i = 0; i < count; i++)
            {
                float a = i * MathF.Tau / count;
                float y = c.Y - 0.099f * MathF.Sqrt(MathF.Max(0f, 1f - MathF.Pow(rho / 0.275f, 2f)));
                var root = V(MathF.Cos(a) * rho, y + 0.008f, MathF.Sin(a) * rho);
                b.Spike(head, root, root + V(MathF.Cos(a) * 0.01f, -len, MathF.Sin(a) * 0.01f), 0.01f, Claw, mat: Shell, blend: 0.003f);
            }
        // Its arms grown into roots reaching down to the ground and spreading
        PokeBuilder.Both(s =>
        {
            int arm = s < 0 ? armL : armR;
            var path = Smooth(3, V(0.2f * s, 0.27f, 0.05f), V(0.3f * s, 0.18f, 0.08f), V(0.36f * s, 0.05f, 0.1f), V(0.42f * s, 0.015f, 0.18f));
            b.Tube(arm, path, 0.026f, 0.012f, bark, blend: 0.004f);
        });
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f + 0.5f;
            var n = V(MathF.Sin(a), 0, MathF.Cos(a));
            b.Tube(Body, Smooth(3, V(0, 0.12f, 0) + n * 0.1f, n * 0.2f + V(0, 0.05f, 0), n * 0.3f + V(0, 0.015f, 0), n * 0.38f + V(0, 0.012f, 0)), 0.03f, 0.01f, bark, blend: 0.006f);
        }
        b.PaintEll(Body, V(0.06f, 0.3f, 0.1f), V(0.04f, 0.06f, 0.03f), moss, soft: 0.01f);
        Stretch(b, V(1f, 1.06f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2987 Banshemane, from Flutter Mane

    /// <summary>
    /// Banshemane: Flutter Mane longer, grey and rose, a pale face with three red gems on its brow, its feathered hair
    /// falling behind it to the ground in a veil and the lower edge of its gown ragged and wine red.
    /// </summary>
    private static PokeBuilder Banshemane()
    {
        var b = Become(FlutterMane(), "Banshemane", 0.82f);
        var c = V(0, 0.5f, 0);
        var r = V(0.12f, 0.12f, 0.11f);
        Unlift(b, V(0, 0.46f, 0));
        var grey = Rgb(150, 136, 144);
        var rose = Rgb(234, 164, 190);
        var wine = Rgb(150, 54, 90);
        Recolour(b, (Rgb(46, 94, 102), grey), (Rgb(204, 84, 156), rose));
        b.PaintEll(Body, c + V(0, -0.01f, 0.08f), V(0.075f, 0.085f, 0.05f), Rgb(238, 232, 236), soft: 0.01f);
        foreach (float x in new[] { -0.024f, 0f, 0.024f })
            b.Ell(Body, On(c, r, x, c.Y + 0.065f - MathF.Abs(x) * 0.4f), V(0.01f, 0.013f, 0.008f), Rgb(220, 40, 70), mat: Shell, blend: 0.004f);
        // The veil of feathered hair falling behind to the ground
        for (int i = 0; i < 9; i++)
        {
            float x = (i - 4) * 0.03f;
            var root = c + V(x, 0.07f - MathF.Abs(x) * 0.3f, -0.07f);
            var tip = V(x * 2.2f, 0.06f + (i % 2) * 0.04f, -0.24f - (i % 3) * 0.02f);
            Frond(b, Body, root, tip, 0.045f, i % 2 == 0 ? grey : rose, V(0, 0.4f, -1f), 0.2f, Fur, 0.01f);
        }
        // The ragged lower edge of its gown
        for (int i = 0; i < 9; i++)
        {
            float a = i * MathF.Tau / 9f + 0.2f;
            var at = V(MathF.Sin(a) * 0.08f, 0.32f, MathF.Cos(a) * 0.075f);
            Frond(b, Body, at, at + V(MathF.Sin(a) * 0.05f, -0.2f - (i % 2) * 0.05f, MathF.Cos(a) * 0.045f), 0.035f, wine, V(MathF.Sin(a), 0, MathF.Cos(a)), 0.25f, Fur, 0.01f);
        }
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2988 Saurmoth, from Slither Wing

    /// <summary>
    /// Saurmoth: Slither Wing longer, its abdomen orange, two more pairs of wings along its body like a centipede's,
    /// a third pair of heavier legs, and the edges of every wing burning red.
    /// </summary>
    private static PokeBuilder Saurmoth()
    {
        var b = Become(SlitherWing(), "Saurmoth", 1.0f);
        var bc = V(0, 0.4f, 0);
        var orange = Rgb(236, 116, 48);
        var red = Rgb(196, 36, 40);
        var fuzz = Rgb(228, 224, 218);
        Recolour(b, (Rgb(62, 150, 150), Rgb(222, 112, 58)), (Rgb(44, 54, 98), Rgb(150, 56, 40)), (Rgb(250, 192, 72), red));
        // A third pair of legs under the abdomen
        PokeBuilder.Both(s =>
        {
            var hip = V(0.06f * s, 0.36f, -0.22f);
            int leg = b.Leg(s, hip, false);
            var knee = V(0.19f * s, 0.21f, -0.28f);
            var ankle = V(0.18f * s, 0.05f, -0.3f);
            b.Limb(leg, hip, knee, 0.045f, 0.034f, fuzz);
            b.Limb(leg, knee, ankle, 0.034f, 0.026f, Rgb(150, 56, 40));
            b.Ell(leg, ankle + V(0, -0.022f, 0.012f), V(0.03f, 0.024f, 0.04f), Rgb(36, 34, 40), mat: Shell);
        });
        // Two more pairs of wings along the abdomen
        int tail = BoneOf(b, "tail");
        var abdomen = Smooth(3, bc + V(0, -0.02f, -0.1f), bc + V(0, -0.04f, -0.25f), bc + V(0, -0.1f, -0.4f), bc + V(0, -0.18f, -0.5f));
        foreach (int i in new[] { 3, 6 })
            PokeBuilder.Both(s =>
            {
                var root = abdomen[i] + V(0.03f * s, 0.06f, 0);
                var tip = root + V(0.26f * s, 0.16f - i * 0.015f, -0.12f);
                Frond(b, tail, root, tip, 0.065f, orange, V(0, 1f, 0.2f), 0.2f, Fur, 0.012f);
                b.PaintEll(tail, Vector3.Lerp(root, tip, 0.86f), V(0.06f, 0.06f, 0.06f), red, soft: 0.01f);
                b.PaintEll(tail, Vector3.Lerp(root, tip, 0.5f), V(0.03f, 0.02f, 0.03f), fuzz, soft: 0.008f);
            });
        Stretch(b, V(1f, 1f, 1.1f));
        return b;
    }

    // ------------------------------------------------------------------ 2989 Lodestatic, from Sandy Shocks

    /// <summary>
    /// Lodestatic: Sandy Shocks larger, its mane of iron sand grown into a coat of filings standing on end over all
    /// three of its units, and two great horns of fused sand curving up from the top one, crackling with yellow charge.
    /// </summary>
    private static PokeBuilder Lodestatic()
    {
        var b = Become(SandyShocks(), "Lodestatic", 0.95f);
        var filings = Rgb(78, 64, 60);
        var sand = Rgb(176, 144, 92);
        var spark = Rgb(255, 234, 110);
        int head = BoneOf(b, "head");
        var tc = V(0, 0.56f, 0);
        var tr = V(0.13f, 0.13f, 0.13f);
        // Longer filings over the top unit
        float golden = MathF.PI * (3f - MathF.Sqrt(5f));
        for (int i = 0; i < 36; i++)
        {
            float y = 1f - 2f * (i + 0.5f) / 36f;
            float ring = MathF.Sqrt(1f - y * y), a = i * golden + 0.3f;
            var n = V(MathF.Sin(a) * ring, y, MathF.Cos(a) * ring);
            if (y < -0.2f || (n.Z > 0.3f && y < 0.7f)) continue;
            b.Spike(head, tc + n * tr.X * 0.9f, tc + n * (tr.X + 0.12f) + V(0, 0, -0.03f), 0.018f, filings, blend: 0.008f);
        }
        // A coat of filings over the two units below
        PokeBuilder.Both(s =>
        {
            var lc = V(0.11f * s, 0.37f, 0);
            for (int i = 0; i < 18; i++)
            {
                float y = 1f - 2f * (i + 0.5f) / 18f;
                float ring = MathF.Sqrt(1f - y * y), a = i * golden;
                var n = V(MathF.Sin(a) * ring, y, MathF.Cos(a) * ring);
                if (y < 0f || n.Z > 0.4f || n.X * s < -0.3f) continue;
                b.Spike(Body, lc + n * 0.09f, lc + n * 0.18f + V(0, 0.02f, -0.02f), 0.016f, filings, blend: 0.008f);
            }
        });
        // Two horns of fused sand curving up from the top, sparks on them
        PokeBuilder.Both(s =>
        {
            var horn = Smooth(3, tc + V(0.06f * s, 0.09f, 0.0f), tc + V(0.11f * s, 0.18f, 0.0f), tc + V(0.12f * s, 0.28f, -0.03f), tc + V(0.08f * s, 0.34f, -0.02f));
            b.Tube(head, horn, 0.034f, 0.01f, sand, Shell, 0.004f);
            for (int i = 2; i < horn.Length - 1; i += 2)
                PaintAs(b, head, horn[i], V(0.012f, 0.03f, 0.012f), spark, Glow, V(0, 0, 40f * s));
        });
        return b;
    }

    // ------------------------------------------------------------------ 2990 Treadnought, from Iron Treads

    /// <summary>
    /// Treadnought: Iron Treads wider and darker, a long track of treads down each side in place of its wheels, a ring
    /// of glowing blue light round its body, and its short tusks shaped into curved blades.
    /// </summary>
    private static PokeBuilder Treadnought()
    {
        var b = Become(IronTreads(), "Treadnought", 0.86f);
        var bc = V(0, 0.24f, 0);
        var grey = Rgb(118, 124, 134);
        var bronze = Rgb(150, 124, 80);
        var dark = Rgb(44, 44, 50);
        var cyan = Rgb(90, 230, 250);
        var steel = Rgb(196, 200, 210);
        int head = BoneOf(b, "head");
        b.Sdf.RemoveAll(s => s.Bone == head && Is(s.Color, Rgb(40, 40, 48)) && s.Shape == SdfShape.RoundCone && s.A.Y < 0.16f);
        Recolour(b, (Rgb(178, 184, 196), grey));
        // The treads down each side
        PokeBuilder.Both(s =>
        {
            var tc = V(0.2f * s, 0.08f, 0);
            b.Box(Body, tc, V(0.05f, 0.075f, 0.21f), 0.05f, dark, default, Shell, 0.008f);
            for (int i = -4; i <= 4; i++)
                b.PaintEll(Body, tc + V(0, 0.07f, i * 0.045f), V(0.06f, 0.012f, 0.01f), Rgb(84, 84, 90), soft: 0.003f);
            foreach (float z in new[] { -0.13f, 0f, 0.13f })
                b.Torus(Body, tc + V(0.05f * s, 0, z), 0.04f, 0.012f, bronze, V(0, 0, 90f), mat: Metal, blend: 0.004f);
            b.PaintEll(Body, tc + V(0.05f * s, 0.06f, 0), V(0.01f, 0.012f, 0.2f), bronze, soft: 0.004f);
        });
        // The ring of blue light round its body
        b.Torus(Body, bc + V(0, 0, -0.04f), 0.18f, 0.012f, cyan, V(90f, 0, 0), mat: Glow, blend: 0.004f);
        // Blades for tusks
        PokeBuilder.Both(s =>
        {
            var root = V(0.07f * s, 0.14f, 0.19f);
            Blade(b, head, root, root + V(0.08f * s, -0.06f, 0.14f), 0.035f, steel, V(s, 0.3f, 0), 0.25f, Metal);
            Blade(b, head, root + V(0.06f * s, -0.045f, 0.1f), root + V(0.11f * s, 0.05f, 0.16f), 0.025f, steel, V(s, 0.3f, 0), 0.25f, Metal);
        });
        Stretch(b, V(1.1f, 1f, 1.05f));
        return b;
    }

    // ------------------------------------------------------------------ 2991 Cryoparcel, from Iron Bundle

    /// <summary>
    /// Cryoparcel: Iron Bundle taller and icy blue, its bag become a second pack of gleaming plates on its back, a visor
    /// of ice over its brow and four jets glowing blue on its underside.
    /// </summary>
    private static PokeBuilder Cryoparcel()
    {
        var b = Become(IronBundle(), "Cryoparcel", 0.72f);
        var bc = V(0, 0.17f, 0);
        var c = V(0, 0.36f, 0.01f);
        var ice = Rgb(120, 172, 222);
        var plate = Rgb(196, 220, 240);
        var glow = Rgb(150, 230, 255);
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail);
        Recolour(b, (Rgb(216, 54, 56), ice), (Rgb(244, 244, 248), Rgb(230, 240, 250)), (Rgb(250, 226, 130), Rgb(190, 200, 214)));
        // The pack of gleaming plates on its back
        b.Box(tail, bc + V(0, 0.04f, -0.13f), V(0.085f, 0.08f, 0.04f), 0.02f, Rgb(150, 180, 214), default, Metal, 0.012f);
        foreach (var (y, z) in new[] { (0.1f, -0.15f), (0.04f, -0.16f), (-0.02f, -0.15f) })
            b.Box(tail, bc + V(0, y, z), V(0.09f, 0.022f, 0.025f), 0.01f, plate, V(-15f, 0, 0), Metal, 0.006f);
        // The visor of ice over its brow
        int head = BoneOf(b, "head");
        b.Ell(head, c + V(0, 0.062f, 0.038f), V(0.09f, 0.022f, 0.055f), Rgb(176, 226, 252), V(-10f, 0, 0), Glow, 0.008f);
        // Four jets on its underside
        foreach (var (x, z) in new[] { (-0.07f, 0.05f), (0.07f, 0.05f), (-0.07f, -0.05f), (0.07f, -0.05f) })
        {
            b.Limb(Body, bc + V(x * 0.8f, -0.08f, z * 0.8f), bc + V(x, -0.115f, z), 0.018f, 0.016f, Rgb(170, 182, 200), Metal, 0.006f);
            b.Ell(Body, bc + V(x, -0.12f, z), V(0.013f, 0.008f, 0.013f), glow, mat: Glow, blend: 0.003f);
        }
        Stretch(b, V(1f, 1.12f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2992 Gauntletron, from Iron Hands

    /// <summary>
    /// Gauntletron: Iron Hands larger and gunmetal, its hands grown into gauntlets of four plates each banded gold,
    /// coils lit yellow round its upper arms, and a crest on its head like a helmet's.
    /// </summary>
    private static PokeBuilder Gauntletron()
    {
        var b = Become(IronHands(), "Gauntletron", 1.0f);
        var gunmetal = Rgb(110, 114, 124);
        var gold = Rgb(206, 170, 72);
        var yellow = Rgb(250, 214, 60);
        Recolour(b, (Rgb(198, 202, 210), gunmetal), (Rgb(44, 62, 128), Rgb(64, 66, 76)));
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var shoulder = V(0.18f * s, 0.53f, 0);
            var elbow = s > 0 ? V(0.3f * s, 0.58f, 0.04f) : V(0.3f * s, 0.42f, 0.06f);
            var hand = s > 0 ? V(0.37f * s, 0.74f, 0.08f) : V(0.36f * s, 0.36f, 0.17f);
            Grow(b, V(1.3f, 1.3f, 1.3f), hand, x => x.Bone == arm && Vector3.Distance(x.A, hand) < 0.2f, _ => false);
            // Gauntlet plates round the forearm, banded gold
            for (int i = 0; i < 4; i++)
            {
                float t = 0.35f + i * 0.15f;
                b.Torus(arm, Vector3.Lerp(elbow, hand, t), 0.062f, 0.016f, i % 2 == 0 ? gunmetal : gold, Euler(hand - elbow), mat: Metal, blend: 0.004f);
            }
            // Coils lit round the upper arm
            for (int i = 0; i < 3; i++)
                b.Torus(arm, Vector3.Lerp(shoulder, elbow, 0.35f + i * 0.2f), 0.068f, 0.008f, yellow, Euler(elbow - shoulder), mat: Glow, blend: 0.003f);
        });
        // The crest on its head
        int head = BoneOf(b, "head");
        var c = V(0, 0.67f, 0.05f);
        Blade(b, head, c + V(0, 0.08f, 0.05f), c + V(0, 0.19f, -0.07f), 0.055f, yellow, V(1f, 0, 0), 0.22f, Metal);
        b.Ell(head, c + V(0, 0.07f, -0.01f), V(0.03f, 0.02f, 0.08f), gold, mat: Metal, blend: 0.008f);
        Stretch(b, V(1.04f, 1.06f, 1.04f));
        return b;
    }

    // ------------------------------------------------------------------ 2993 Hydrajet, from Iron Jugulis

    /// <summary>
    /// Hydrajet: Iron Jugulis longer and violet-lit, a jet glowing at the tip of each upper wing, a ring of lights round
    /// its neck, and a third head grown at the end of its tail.
    /// </summary>
    private static PokeBuilder Hydrajet()
    {
        var b = Become(IronJugulis(), "Hydrajet", 1.0f);
        var bc = V(0, 0.5f, 0);
        Unlift(b, bc);
        var violet = Rgb(150, 86, 226);
        var light = Rgb(150, 210, 255);
        Recolour(b, (JugulisFuchsia, violet));
        // The third head at the end of the tail, its blade gone
        int tail = BoneOf(b, "tail");
        var tp = Smooth(3, bc + V(0, -0.1f, -0.03f), bc + V(0, -0.24f, -0.07f), bc + V(0, -0.33f, -0.17f), bc + V(0, -0.36f, -0.28f));
        b.Sdf.RemoveAll(s => s.Bone == tail && Is(s.Color, JugulisArmor));
        b.Tube(tail, Smooth(3, tp[^1], tp[^1] + V(0, 0.02f, -0.06f), tp[^1] + V(0, 0.08f, -0.08f)), 0.016f, 0.022f, JugulisNavy, blend: 0f);
        JugulisArmHead(b, tail, tp[^1] + V(0, 0.12f, -0.07f), 0.95f);
        // Jets at the tips of the upper wings
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            var root = bc + V(0.05f * s, 0.09f, -0.07f);
            var dir = Vector3.Normalize(V(0.8f * s, 0.9f, -0.3f));
            var joint = root + dir * 0.32f * 0.45f;
            var way = Vector3.Normalize(dir + V(0, -0.3f, -0.1f));
            var tip = joint + way * 0.32f * 0.6f;
            b.Limb(wing, tip - way * 0.06f, tip + way * 0.01f, 0.018f, 0.02f, JugulisArmor, Metal, 0.006f);
            b.Ell(wing, tip + way * 0.02f, V(0.016f, 0.016f, 0.016f), violet, mat: Glow, blend: 0.004f);
        });
        // A ring of lights round the main neck's collar
        int head = BoneOf(b, "head");
        var c = bc + V(0, 0.23f, 0.06f);
        var at = c + V(0, -0.06f, -0.018f);
        var n = Vector3.Normalize(V(0, 1f, 0.3f));
        var u = Vector3.Normalize(Vector3.Cross(n, V(1f, 0, 0)));
        var w = Vector3.Cross(n, u);
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f;
            b.Ell(head, at + (u * MathF.Cos(a) + w * MathF.Sin(a)) * 0.056f, V(0.009f, 0.009f, 0.009f), light, mat: Glow, blend: 0.003f);
        }
        Stretch(b, V(1f, 1f, 1.08f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2994 Fusionmoth, from Iron Moth

    /// <summary>
    /// Fusionmoth: Iron Moth wider and darker, a second ring of armour round a glowing orange core, its wings longer and
    /// blazing at their tips in orange and violet fire, and its horns grown into two curved prongs.
    /// </summary>
    private static PokeBuilder Fusionmoth()
    {
        var b = Become(IronMoth(), "Fusionmoth", 1.0f);
        var bc = V(0, 0.46f, 0);
        Unlift(b, bc);
        var dark = Rgb(84, 86, 96);
        Recolour(b, (MothArmor, dark), (MothBlue, Rgb(110, 112, 122)));
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            var root = bc + V(0.04f * s, 0.02f, -0.04f);
            Grow(b, V(1.2f, 1.2f, 1.2f), root, bone => bone == wing);
            foreach (var (a, len) in new[] { (65f, 0.3f), (10f, 0.32f), (-45f, 0.26f) })
            {
                var d = Vector3.Normalize(V(MathF.Cos(a * Degree) * s, MathF.Sin(a * Degree), -0.15f));
                var tip = root + d * len * 1.2f;
                FlameTongue(b, wing, tip - d * 0.03f, tip + d * 0.1f, 0.024f, Rgb(214, 70, 210), Rgb(255, 200, 70));
            }
        });
        // The second ring of armour round the core, the core glowing at its front
        b.Torus(Body, bc, 0.084f, 0.016f, Rgb(140, 140, 150), V(90f, 0, 0), mat: Metal, blend: 0.006f);
        b.Ell(Body, bc + V(0, 0, 0.062f), V(0.038f, 0.038f, 0.016f), Rgb(255, 150, 60), mat: Glow, blend: 0.006f);
        // The horns grown on into two curved prongs
        int head = BoneOf(b, "head");
        var c = bc + V(0, 0.1f, 0.05f);
        PokeBuilder.Both(s => b.Tube(head, Smooth(3, c + V(0.062f * s, 0.12f, -0.035f), c + V(0.07f * s, 0.18f, -0.02f), c + V(0.04f * s, 0.22f, 0.02f)), 0.008f, 0.005f, MothRed, blend: 0.003f));
        Stretch(b, V(1.08f, 1f, 1f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2995 Thornframe, from Iron Thorns

    /// <summary>
    /// Thornframe: Iron Thorns taller, dark as stone banded in bronze, its spines grown into a cage of glowing yellow
    /// crystal rods over its back, and its jaw a heavier plate.
    /// </summary>
    private static PokeBuilder Thornframe()
    {
        var b = Become(IronThorns(), "Thornframe", 1.0f);
        var crystal = Rgb(240, 196, 80);
        b.Sdf.RemoveAll(s => s.Bone == Body && Is(s.Color, ThornsGlow) && s.Shape == SdfShape.RoundCone);
        Recolour(b, (ThornsGreen, Rgb(72, 68, 60)), (ThornsPlate, Rgb(140, 108, 62)), (ThornsGlow, Rgb(250, 220, 90)));
        // The cage of crystal rods
        var tops = new Vector3[2, 4];
        PokeBuilder.Both(s =>
        {
            int k = 0;
            foreach (var (y, len) in new[] { (0.66f, 0.24f), (0.57f, 0.27f), (0.47f, 0.25f), (0.37f, 0.2f) })
            {
                var root = V(0.065f * s, y, -0.095f);
                var tip = root + Vector3.Normalize(V(0.35f * s, 0.6f, -0.6f)) * len;
                b.Limb(Body, root, tip, 0.014f, 0.012f, crystal, Glow, 0.004f);
                tops[s < 0 ? 0 : 1, k++] = tip;
            }
        });
        for (int k = 0; k < 4; k++)
        {
            b.Limb(Body, tops[0, k], tops[1, k], 0.01f, 0.01f, crystal, Glow, 0.003f);
            if (k < 3)
                for (int side = 0; side < 2; side++)
                    b.Limb(Body, tops[side, k], tops[side, k + 1], 0.01f, 0.01f, crystal, Glow, 0.003f);
        }
        // The heavier jaw
        int jaw = BoneOf(b, "jaw");
        Grow(b, V(1.35f, 1.4f, 1.3f), V(0, 0.73f, 0.07f), bone => bone == jaw);
        Stretch(b, V(1f, 1.06f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 3000 Bullionaire, from Gholdengo

    /// <summary>
    /// Bullionaire: Gholdengo taller, standing on stacks of gold coins, a gown of linked coins of gold and silver down
    /// its body flaring at its foot, and a crown of coins on its head.
    /// </summary>
    private static PokeBuilder Bullionaire()
    {
        var b = Become(Gholdengo(), "Bullionaire", 0.95f);
        var lift = V(0, 0.12f, 0);
        Move(b, _ => true, lift);
        b.Model.Skeleton.Shift(lift, first: 1);
        var silver = Rgb(198, 202, 212);
        // The stacks of coins it stands on
        foreach (var (x, z, h, r) in new[] { (-0.05f, 0.02f, 0.14f, 0.05f), (0.05f, 0.02f, 0.14f, 0.05f), (0.0f, -0.06f, 0.11f, 0.048f), (-0.12f, 0.05f, 0.08f, 0.04f), (0.12f, -0.02f, 0.06f, 0.04f) })
        {
            b.Limb(Body, V(x, r * 0.3f, z), V(x, h, z), r, r, GimmiGold, Metal, 0.004f);
            for (float y = 0.025f; y < h; y += 0.022f)
                b.PaintTorus(Body, V(x, y, z), r, 0.004f, GimmiGoldDark);
        }
        // The gown of linked coins down its body, flaring at its foot
        b.Limb(Body, V(0, 0.32f, 0) + lift, V(0, 0.2f, 0) + lift, 0.075f, 0.12f, GimmiGold, Metal, 0.01f);
        for (int row = 0; row < 7; row++)
        {
            float y = 0.44f - row * 0.035f;
            float colR = y >= 0.32f ? 0.085f - 0.01f * (0.44f - y) / 0.24f : 0.075f + (0.32f - y) / 0.12f * 0.045f;
            int count = 13 + row * 2;
            for (int i = 0; i < count; i++)
            {
                float a = (i + 0.5f * (row % 2)) * MathF.Tau / count;
                var n = V(MathF.Sin(a), y >= 0.32f ? 0.15f : 0.4f, MathF.Cos(a));
                var at = V(0, y, 0) + lift + V(MathF.Sin(a), 0, MathF.Cos(a)) * (colR + 0.004f);
                b.Ell(Body, at, V(0.023f, 0.006f, 0.023f), (i + row) % 3 == 0 ? silver : GimmiGold, Euler(n), Metal, 0.003f);
            }
        }
        // The crown of coins on its head
        int head = BoneOf(b, "head");
        var c = V(0, 0.55f, 0.01f) + lift;
        c += V(0, 0.05f, 0);
        b.Torus(head, c + V(0, 0.075f, 0), 0.05f, 0.014f, GimmiGold, mat: Metal, blend: 0.004f);
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            b.Ell(head, c + V(0, 0.11f, 0) + d * 0.05f, V(0.028f, 0.03f, 0.007f), i % 2 == 0 ? GimmiGold : silver, V(0, a * 180f / MathF.PI, 0), Metal, 0.004f);
        }
        return b;
    }

    // ------------------------------------------------------------------ 3001 Tabletruin, from Wo-Chien

    /// <summary>
    /// Tabletruin: Wo-Chien larger, its cone of tablets opened into a fan of eight wooden tablets carved with glowing
    /// red seals, its mound of moss heavier with roots trailing from its foot over the ground.
    /// </summary>
    private static PokeBuilder Tabletruin()
    {
        var b = Become(WoChien(), "Tabletruin", 0.95f);
        var wood = Rgb(110, 98, 78);
        var band = Rgb(92, 64, 44);
        var seal = Rgb(240, 60, 60);
        var root = Rgb(84, 76, 54);
        var foot = V(0.07f, 0.2f, -0.1f);
        var top = V(0.11f, 0.52f, -0.19f);
        b.Sdf.RemoveAll(s => s.Bone == Body && (Is(s.Color, Rgb(142, 102, 66)) || Is(s.Color, band) || Is(s.Color, Rgb(84, 130, 70))));
        // The fan of eight tablets standing on the mound's back
        var hub = V(0.04f, 0.2f, -0.12f);
        for (int i = 0; i < 8; i++)
        {
            float a = (-72f + i * (144f / 7f)) * Degree;
            var d = Vector3.Normalize(V(MathF.Sin(a), MathF.Cos(a), -0.25f));
            float len = 0.36f - MathF.Abs(MathF.Sin(a)) * 0.08f;
            var mid = hub + d * (0.1f + len * 0.5f);
            b.Box(Body, mid, V(0.035f, len * 0.5f, 0.012f), 0.01f, wood, Euler(d), Shell, 0.006f);
            for (int k = 0; k < 3; k++)
                PaintAs(b, Body, hub + d * (0.17f + k * len * 0.25f) + V(0, 0, 0.012f), V(0.016f, 0.012f, 0.012f), seal, Glow, Euler(d));
        }
        b.Ell(Body, hub + V(0, 0.02f, 0.02f), V(0.14f, 0.07f, 0.1f), Rgb(84, 92, 62), blend: 0.03f);
        // Roots trailing from its foot over the ground
        for (int i = 0; i < 10; i++)
        {
            float a = i * MathF.Tau / 10f + 0.3f;
            var n = V(MathF.Sin(a), 0, MathF.Cos(a));
            var r0 = V(0, 0.06f, -0.02f) + V(n.X * 0.26f, 0, n.Z * 0.24f);
            b.Tube(Body, Smooth(3, r0, r0 + n * 0.08f + V(0, -0.04f, 0), r0 + n * 0.16f + V(n.Z * 0.03f, -0.05f, 0)), 0.014f, 0.006f, root, blend: 0.004f);
        }
        foreach (var d in b.Model.Decals.Where(d => d.IsEye)) d.Iris = Rgb(240, 40, 40);
        Stretch(b, V(1.06f, 1.06f, 1.06f));
        return b;
    }

    // ------------------------------------------------------------------ 3002 Swordruin, from Chien-Pao

    /// <summary>
    /// Swordruin: Chien-Pao larger, its fur gone dark slate frosted pale at its tips, a red seal glowing on its brow,
    /// its two sword fangs hanging from its jaw and two more blades of ice standing from its shoulders.
    /// </summary>
    private static PokeBuilder Swordruin()
    {
        var b = Become(ChienPao(), "Swordruin", 1.0f);
        var slate = Rgb(66, 76, 100);
        var frost = Rgb(170, 200, 226);
        var ice = Rgb(170, 220, 244);
        Recolour(b, (Rgb(238, 242, 248), slate), (Rgb(150, 196, 226), frost));
        int head = BoneOf(b, "head");
        var c = V(0, 0.59f, 0.3f);
        // The seal on its brow
        PaintAs(b, head, c + V(0, 0.05f, 0.075f), V(0.02f, 0.022f, 0.02f), Rgb(240, 50, 50), Glow);
        foreach (var d in b.Model.Decals.Where(d => d.IsEye)) d.Iris = Rgb(140, 220, 255);
        // Frosted tips over its back
        for (int i = 0; i < 12; i++)
        {
            float z = 0.18f - i * 0.04f;
            float x = (i % 3 - 1) * 0.06f;
            var at = V(x, 0.5f - MathF.Abs(z - 0.05f) * 0.2f, z - 0.02f);
            b.Spike(Body, at, at + V(x * 0.6f, 0.04f, -0.05f), 0.02f, frost, 0.5f, blend: 0.008f);
        }
        // The shoulder blades of ice
        PokeBuilder.Both(s => Blade(b, Body, V(0.1f * s, 0.55f, 0.12f), V(0.16f * s, 0.86f, -0.04f), 0.05f, ice, V(s, 0, 0.3f), 0.25f, Glow));
        Stretch(b, V(1.04f, 1.06f, 1.04f));
        return b;
    }

    // ------------------------------------------------------------------ 3003 Vesselruin, from Ting-Lu

    /// <summary>
    /// Vesselruin: Ting-Lu larger and heavier, its vessel become a great bronze bell of a cauldron over its head with a
    /// red seal glowing on its side, its four prongs grown into branching antlers.
    /// </summary>
    private static PokeBuilder Vesselruin()
    {
        var b = Become(TingLu(), "Vesselruin", 1.0f);
        var bronze = Rgb(62, 104, 92);
        var antler = Rgb(100, 84, 60);
        // The prongs go: antlers take their place
        b.Sdf.RemoveAll(s => s.Bone == Body && Is(s.Color, bronze) && s.Shape == SdfShape.RoundCone);
        var vc = V(0, 0.78f, 0.12f);
        // The bell's rim flared over its face and the seal on its front
        b.Torus(Body, vc + V(0, -0.12f, 0.03f), 0.23f, 0.03f, Rgb(52, 88, 78), V(15f, 0, 0), 1f, 0.9f, Metal, 0.01f);
        PaintAs(b, Body, vc + V(0, -0.02f, 0.2f), V(0.05f, 0.05f, 0.03f), Rgb(240, 50, 40), Glow);
        PokeBuilder.Both(s =>
        {
            var root = vc + V(0.18f * s, 0.06f, -0.04f);
            var main = Smooth(3, root, root + V(0.08f * s, 0.12f, -0.02f), root + V(0.12f * s, 0.25f, -0.06f), root + V(0.1f * s, 0.34f, -0.08f));
            b.Tube(Body, main, 0.026f, 0.01f, antler, Shell, 0.006f);
            foreach (var (k, d) in new[] { (3, V(0.08f * s, 0.06f, 0.04f)), (5, V(-0.05f * s, 0.09f, 0.02f)), (7, V(0.07f * s, 0.08f, -0.03f)) })
                b.Spike(Body, main[k], main[k] + d, 0.014f, antler, mat: Shell, blend: 0.004f);
        });
        foreach (var d in b.Model.Decals.Where(d => d.IsEye)) { d.Size *= 1.5f; d.Half *= 1.6f; }
        Stretch(b, V(1.05f, 1.05f, 1.05f));
        return b;
    }

    // ------------------------------------------------------------------ 3004 Beadruin, from Chi-Yu

    /// <summary>
    /// Beadruin: Chi-Yu grown long, a serpent of black scales cracked with glowing orange, fins of flame along its back
    /// and tail, a seal on its brow and six red beads ringed round it.
    /// </summary>
    private static PokeBuilder Beadruin()
    {
        var b = Become(ChiYu(), "Beadruin", 0.82f);
        var bc = V(0, 0.2f, 0);
        Unlift(b, bc);
        var black = Rgb(48, 42, 46);
        var ember = Rgb(250, 120, 50);
        var red = Rgb(214, 58, 40);
        Recolour(b, (Rgb(246, 150, 74), black), (Rgb(252, 214, 150), Rgb(70, 54, 52)), (Rgb(62, 104, 86), Rgb(120, 60, 40)));
        // The long body behind, curving, cracked with glow, flame fins along it
        int tail = BoneOf(b, "tail");
        var path = Smooth(3, bc + V(0, -0.02f, -0.06f), bc + V(0.1f, -0.06f, -0.2f), bc + V(0.02f, -0.1f, -0.36f), bc + V(-0.12f, -0.08f, -0.44f), bc + V(-0.16f, 0.0f, -0.56f));
        b.Tube(tail, path, 0.07f, 0.03f, black, blend: 0f);
        for (int i = 2; i < path.Length - 1; i += 2)
        {
            PaintAs(b, tail, path[i] + V(0.02f, 0.05f, 0), V(0.008f, 0.03f, 0.008f), ember, Glow, V(0, 0, 30f));
            Frond(b, tail, path[i] + V(0, 0.05f, 0), path[i] + V(0, 0.13f, -0.04f), 0.03f, red, V(1f, 0, 0), 0.2f, Glow, 0.008f);
        }
        // The ring of six beads round it
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f;
            b.Ell(Body, bc + V(MathF.Cos(a) * 0.16f, -0.02f + MathF.Sin(a) * 0.03f, MathF.Sin(a) * 0.16f - 0.04f), V(0.028f, 0.028f, 0.028f), Rgb(230, 50, 40), mat: Glow, blend: 0.004f);
        }
        b.Torus(Body, bc + V(0, -0.02f, -0.04f), 0.16f, 0.004f, Rgb(80, 60, 56), V(10f, 0, 0), mat: Metal, blend: 0.002f);
        PaintAs(b, Body, bc + V(0, 0.07f, 0.06f), V(0.018f, 0.014f, 0.016f), Rgb(255, 170, 60), Glow);
        return Lift(b);
    }

    // ------------------------------------------------------------------ 3005 Lunaroar, from Roaring Moon

    /// <summary>
    /// Lunaroar: Roaring Moon larger, near black with its red belly brighter, its wings longer and ragged like torn
    /// sails, and its crest of horns curling forward into a crown.
    /// </summary>
    private static PokeBuilder Lunaroar()
    {
        var b = Become(RoaringMoon(), "Lunaroar", 1.0f);
        Unlift(b, V(0, 0.42f, 0));
        var bc = V(0, 0.42f, 0.02f);
        var c = bc + V(0, 0.18f, 0.12f);
        var black = Rgb(40, 40, 58);
        Recolour(b, (Rgb(60, 140, 204), black), (Rgb(206, 38, 66), Rgb(236, 48, 46)), (Rgb(132, 196, 232), Rgb(70, 30, 40)), (Rgb(206, 214, 224), Rgb(70, 66, 80)));
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            Grow(b, V(1.2f, 1.18f, 1.1f), bc + V(0.08f * s, 0.06f, -0.03f), bone => bone == wing);
        });
        // The crown of horns curling forward
        int head = BoneOf(b, "head");
        for (int i = 0; i < 7; i++)
        {
            float a = (-75f + i * 25f) * Degree;
            var root = c + V(MathF.Sin(a) * 0.05f, 0.03f, -0.02f);
            var h = Smooth(3, root, root + V(MathF.Sin(a) * 0.05f, 0.08f, -0.04f), root + V(MathF.Sin(a) * 0.07f, 0.14f, 0.02f), root + V(MathF.Sin(a) * 0.06f, 0.13f, 0.06f));
            b.Tube(head, h, 0.014f, 0.005f, Rgb(70, 66, 80), Shell, 0.004f);
        }
        Stretch(b, V(1.04f, 1.06f, 1.04f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 3006 Paladroid, from Iron Valiant

    /// <summary>
    /// Paladroid: Iron Valiant taller, its blades joined into a long lance of green crystal glowing pink, a second
    /// blade on its other arm, a cape of iridescent light from its shoulders and its visor sharper.
    /// </summary>
    private static PokeBuilder Paladroid()
    {
        var b = Become(IronValiant(), "Paladroid", 1.0f);
        var cc = V(0, 0.7f, 0);
        var c = cc + V(0, 0.17f, 0.01f);
        var crystal = Rgb(110, 220, 150);
        var pink = Rgb(236, 120, 200);
        int armR = BoneOf(b, "armR"), armL = BoneOf(b, "armL");
        b.Sdf.RemoveAll(s => s.Bone == armR && Is(s.Color, Rgb(236, 68, 140)));
        // The lance through its right hand
        var hand = cc + V(0.17f, -0.12f, 0.06f);
        var tip = hand + V(0.18f, 0.38f, -0.02f);
        var butt = hand - V(0.18f, 0.38f, -0.02f) * 0.35f;
        b.Limb(armR, butt, hand + (tip - hand) * 0.15f, 0.012f, 0.012f, Rgb(220, 224, 230), Metal, 0.004f);
        Blade(b, armR, hand + (tip - hand) * 0.12f, tip, 0.05f, crystal, V(0, 0, 1f), 0.3f, Glow);
        b.PaintEll(armR, Vector3.Lerp(hand, tip, 0.6f), V(0.02f, 0.12f, 0.02f), pink, Euler(tip - hand), 0.01f);
        // A blade on the left arm
        var handL = cc + V(-0.1f, -0.14f, 0.05f);
        Blade(b, armL, handL, handL + V(-0.2f, -0.3f, 0.04f), 0.04f, crystal, V(0, 0, 1f), 0.3f, Glow);
        // The cape of light from its shoulders
        PokeBuilder.Both(s => Frond(b, Body, cc + V(0.05f * s, 0.06f, -0.04f), cc + V(0.2f * s, -0.32f, -0.12f), 0.07f, Rgb(214, 170, 236), V(0.3f * s, 0, -1f), 0.08f, Glow, 0.01f));
        // A sharper visor
        int head = BoneOf(b, "head");
        PokeBuilder.Both(s => b.Spike(head, c + V(0.035f * s, 0.04f, -0.01f), c + V(0.06f * s, 0.14f, -0.06f), 0.014f, Rgb(220, 224, 230), 0.4f, Metal));
        b.PaintEll(head, c + V(0, -0.005f, 0.05f), V(0.03f, 0.012f, 0.02f), Rgb(200, 60, 160), soft: 0.004f);
        Stretch(b, V(1f, 1.08f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 3013 Chagamage, from Sinistcha

    /// <summary>
    /// Chagamage: Sinistcha's bowl become an iron kettle crusted with moss, a handle over it wound with vines, a spout
    /// dripping glowing matcha, a lid lifted on its brew, a jagged glowing face on its side, stubby legs of twig, and
    /// its whisk floating free over it.
    /// </summary>
    private static PokeBuilder Chagamage()
    {
        var b = Become(Sinistcha(), "Chagamage", 0.66f);
        var c = V(0, 0.12f, 0);
        Unlift(b, V(0, 0.14f, 0));
        var iron = Rgb(70, 72, 74);
        var moss = Rgb(96, 128, 70);
        var glow = Rgb(120, 250, 110);
        var twig = Rgb(110, 92, 64);
        int head = BoneOf(b, "head");
        b.Sdf.RemoveAll(s => s.Bone == head && s.A.Y > c.Y + 0.08f && !Is(s.Color, Rgb(224, 210, 146)) && !Is(s.Color, Rgb(36, 34, 36)));
        b.Model.Decals.RemoveAll(d => d.Bone == head);
        Recolour(b, (Rgb(66, 48, 40), iron), (Rgb(120, 76, 50), Rgb(56, 58, 60)), (Rgb(240, 232, 212), moss));
        Make(b, s => s.Bone == head && Is(s.Color, Matcha), Glow);
        // The kettle swelled out, its handle, spout and lid
        Grow(b, V(1.15f, 1.1f, 1.15f), c, s => s.Bone == Body && s.Op == SdfOp.Add, _ => false);
        b.Tube(Body, Smooth(3, c + V(-0.11f, 0.06f, 0), c + V(-0.1f, 0.24f, 0), c + V(0, 0.29f, 0), c + V(0.1f, 0.24f, 0), c + V(0.11f, 0.06f, 0)), 0.01f, 0.01f, iron, Metal, 0.004f);
        b.Tube(Body, Smooth(3, c + V(-0.07f, 0.24f, 0.02f), c + V(-0.02f, 0.28f, -0.02f), c + V(0.05f, 0.27f, 0.02f), c + V(0.09f, 0.2f, -0.01f)), 0.004f, 0.004f, moss, Leaf, 0.002f);
        b.Limb(Body, c + V(-0.11f, -0.01f, 0.02f), c + V(-0.18f, 0.05f, 0.04f), 0.03f, 0.018f, iron, Metal, 0.008f);
        b.Ell(Body, c + V(-0.18f, 0.035f, 0.04f), V(0.012f, 0.016f, 0.012f), glow, mat: Glow, blend: 0.004f);
        b.Ell(Body, c + V(0.02f, 0.085f, -0.01f), V(0.08f, 0.01f, 0.075f), iron, V(0, 0, -14f), Metal, 0.004f);
        b.Ell(Body, c + V(0.02f, 0.105f, -0.01f), V(0.014f, 0.012f, 0.014f), iron, mat: Metal, blend: 0.004f);
        // A jagged glowing face on its side
        PokeBuilder.Both(s => PaintAs(b, Body, c + V(0.04f * s, 0.0f, 0.12f), V(0.022f, 0.014f, 0.02f), glow, Glow, V(0, 0, -20f * s)));
        b.Mark(Body, c + V(0, -0.045f, 0.124f), V(0, -0.2f, 1f), 0.06f, 0.016f, glow, MarkShape.Zigzag);
        // Twig legs
        foreach (float a in new[] { 0.6f, 2.6f, 4.6f })
        {
            var n = V(MathF.Sin(a), 0, MathF.Cos(a));
            b.Limb(Body, c + V(0, -0.08f, 0) + n * 0.06f, n * 0.11f + V(0, 0.005f, 0), 0.012f, 0.008f, twig, blend: 0.004f);
        }
        // The whisk floats free over it
        Move(b, bone => bone == head, V(0.06f, 0.08f, 0));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 3014 Okidaimyo, from Okidogi

    /// <summary>
    /// Okidaimyo: Okidogi larger, its arms thicker with plates of armour, its fur standing up behind its head like a
    /// lion's mane and its chain grown into a heavy mantle of links over its shoulders.
    /// </summary>
    private static PokeBuilder Okidaimyo()
    {
        var b = Become(Okidogi(), "Okidaimyo", 1.0f);
        var bc = V(0, 0.5f, 0);
        var c = bc + V(0, 0.3f, 0.07f);
        var black = Rgb(40, 40, 46);
        var green = Rgb(60, 86, 52);
        var armour = Rgb(70, 70, 64);
        Recolour(b, (Rgb(98, 156, 62), green));
        b.Sdf.RemoveAll(s => s.Bone == Body && Is(s.Color, ToxicChain));
        // The mane standing up behind its head
        int head = BoneOf(b, "head");
        for (int i = 0; i < 11; i++)
        {
            float a = (-110f + i * 22f) * Degree;
            var root = c + V(MathF.Sin(a) * 0.07f, MathF.Cos(a) * 0.05f + 0.02f, -0.05f);
            b.Spike(head, root, root + V(MathF.Sin(a) * 0.11f, MathF.Cos(a) * 0.1f + 0.06f, -0.08f), 0.03f, Rgb(36, 62, 40), 0.5f, blend: 0.01f);
        }
        // Plates of armour on the arms and shoulders
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var shoulder = bc + V(0.16f * s, 0.12f, 0.02f);
            b.Ell(arm, shoulder + V(0.03f * s, 0.03f, 0), V(0.09f, 0.05f, 0.09f), armour, V(0, 0, -20f * s), Metal, 0.01f);
            var elbow = s > 0 ? shoulder + V(0.12f, -0.06f, 0.08f) : shoulder + V(-0.06f, -0.16f, 0.04f);
            var hand = s > 0 ? elbow + V(0.1f, 0.06f, 0.08f) : elbow + V(-0.02f, -0.14f, 0.04f);
            b.Torus(arm, Vector3.Lerp(elbow, hand, 0.4f), 0.07f, 0.014f, Rgb(140, 120, 90), Euler(hand - elbow), mat: Shell, blend: 0.004f);
            Grow(b, V(1.2f, 1.2f, 1.2f), shoulder, x => x.Bone == arm && x.Op == SdfOp.Add && Is(x.Color, green), _ => false);
        });
        // The mantle of chain links over its shoulders
        foreach (float y in new[] { 0.22f, 0.17f })
        {
            var links = new System.Collections.Generic.List<Vector3>();
            for (int i = 0; i < 12; i++)
            {
                float a = (90f + i * 30f) * Degree;
                links.Add(bc + V(MathF.Sin(a) * (0.14f + (0.22f - y)), y, MathF.Cos(a) * 0.11f + 0.02f));
            }
            links.Add(links[0]);
            ChainLinks(b, Body, links.ToArray(), 0.026f, 0.01f);
        }
        Stretch(b, V(1.05f, 1.05f, 1.05f));
        return b;
    }

    // ------------------------------------------------------------------ 3015 Munkiregent, from Munkidori

    /// <summary>
    /// Munkiregent: Munkidori taller and leaner, its fur tipped in poison-green light at its shoulders and wrists, the
    /// chain at its neck a collar of linked rings, and its tail grown long and forked, each end a green flame.
    /// </summary>
    private static PokeBuilder Munkiregent()
    {
        var b = Become(Munkidori(), "Munkiregent", 0.92f);
        var bc = V(0, 0.36f, 0);
        var green = Rgb(110, 250, 110);
        int head = BoneOf(b, "head");
        b.Sdf.RemoveAll(s => s.Bone == head && Is(s.Color, ToxicChain));
        // The collar of rings
        var links = new System.Collections.Generic.List<Vector3>();
        for (int i = 0; i < 10; i++)
        {
            float a = i * MathF.Tau / 10f;
            links.Add(bc + V(MathF.Sin(a) * 0.075f, 0.2f, MathF.Cos(a) * 0.065f + 0.01f));
        }
        links.Add(links[0]);
        ChainLinks(b, Body, links.ToArray(), 0.018f, 0.007f);
        // Green tips at its shoulders and the hem of its cloak
        PokeBuilder.Both(s =>
        {
            foreach (var d in new[] { V(0.4f, 0.4f, 0), V(0.7f, 0.1f, 0.2f) })
                b.Spike(Body, bc + V(0.08f * s, 0.15f, 0), bc + V(0.08f * s, 0.15f, 0) + V(d.X * s, d.Y, d.Z) * 0.08f, 0.018f, green, 0.5f, Glow, 0.006f);
        });
        // The long forked tail, green flame at each end
        int tail = b.Tail(bc + V(0, -0.14f, -0.06f));
        var path = Smooth(3, bc + V(0, -0.14f, -0.06f), bc + V(0.06f, -0.26f, -0.16f), bc + V(0.14f, -0.12f, -0.24f), bc + V(0.16f, 0.1f, -0.24f));
        b.Tube(tail, path, 0.016f, 0.012f, Rgb(40, 40, 50), blend: 0f);
        foreach (var d in new[] { V(-0.05f, 0.14f, 0.02f), V(0.07f, 0.12f, -0.02f) })
        {
            var end = path[^1] + d;
            b.Tube(tail, Smooth(2, path[^1], path[^1] + d * 0.5f + V(0, 0.02f, 0), end), 0.011f, 0.008f, Rgb(40, 40, 50), blend: 0.004f);
            FlameTongue(b, tail, end, end + V(d.X * 0.3f, 0.08f, 0), 0.02f, Rgb(90, 220, 90), Rgb(210, 255, 170));
        }
        foreach (var d in b.Model.Decals.Where(d => d.IsEye)) d.Iris = Rgb(220, 80, 240);
        Stretch(b, V(0.96f, 1.1f, 0.96f));
        return b;
    }

    // ------------------------------------------------------------------ 3016 Fezantessa, from Fezandipiti

    /// <summary>
    /// Fezantessa: Fezandipiti longer, purple and black, its tail feathers grown into a sweeping train with poison-green
    /// eyes on them, a fan of dark feathers at its throat and a crest of green plumes.
    /// </summary>
    private static PokeBuilder Fezantessa()
    {
        var b = Become(Fezandipiti(), "Fezantessa", 1.0f);
        var bc = V(0, 0.48f, -0.02f);
        var purple = Rgb(110, 60, 150);
        var green = Rgb(110, 220, 100);
        var dark = Rgb(46, 60, 50);
        Recolour(b, (Rgb(156, 82, 50), purple), (Rgb(244, 242, 238), Rgb(120, 200, 100)), (Rgb(214, 60, 60), Rgb(170, 50, 120)));
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail);
        // The train
        for (int i = 0; i < 7; i++)
        {
            float x = (i - 3) * 0.04f;
            var root = bc + V(x * 0.4f, -0.02f, -0.14f);
            var tip = V(x * 2.4f, 0.03f, -0.62f + MathF.Abs(x) * 0.6f);
            Frond(b, tail, root, tip, 0.05f, i % 2 == 0 ? purple : Rgb(70, 50, 120), V(0, 1f, -0.2f), 0.18f, Fur, 0.01f);
            foreach (float t in new[] { 0.55f, 0.85f })
            {
                var at = Vector3.Lerp(root, tip, t) + V(0, 0.012f, 0);
                PaintAs(b, tail, at, V(0.02f, 0.012f, 0.02f), green, Glow);
                b.PaintEll(tail, at, V(0.009f, 0.012f, 0.009f), Rgb(40, 30, 60), soft: 0.003f);
            }
        }
        // The fan of dark feathers at its throat
        for (int i = 0; i < 10; i++)
        {
            float a = i * MathF.Tau / 10f;
            var n = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = bc + V(0, 0.12f, 0.1f) + n * 0.04f;
            Frond(b, Body, root, root + n * 0.08f + V(0, 0.03f, 0), 0.026f, dark, V(0, 1f, 0), 0.25f, Fur, 0.008f);
        }
        // The crest of green plumes
        int head = BoneOf(b, "head");
        var c = bc + V(0, 0.36f, 0.08f) + V(0, 0.04f, 0.03f);
        foreach (var (x, lean) in new[] { (-0.02f, -0.6f), (0f, 0f), (0.02f, 0.6f), (-0.01f, -0.3f), (0.01f, 0.3f) })
        {
            var root = c + V(x, 0.04f, -0.01f);
            var tip = root + V(lean * 0.04f, 0.08f, -0.02f);
            b.Limb(head, root, tip, 0.007f, 0.006f, dark, blend: 0.004f);
            b.Ell(head, tip, V(0.01f, 0.016f, 0.006f), green, mat: Glow, blend: 0.003f);
        }
        Stretch(b, V(1f, 1.05f, 1.08f));
        return b;
    }
}
