using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// The species of our own that evolve past level 100 (plan 03 · D14), batch 2: Johto's, in our numbers' order
// (2000 + the forebear's National number), sculpted from the user's reference pictures and the looks of
// docs/plans/D14-proposals-johto.md as batch 1 was (PokemonModels.Ours.Kanto.cs, whose helpers these use): the
// forebear's build taken whole where the body stays and the new features added, or, where the body changes its plan,
// built anew in the forebear's colours. The forebears' own sculpts are never touched.
internal static partial class PokemonModels
{
    /// <summary>Changes the white round the pupils of every eye whose white is <paramref name="from"/> (Noctowl's red) to <paramref name="to"/>.</summary>
    private static void Rewhite(PokeBuilder b, Color from, Color to)
    {
        foreach (var d in b.Model.Decals)
            if (d.White is { } w && w.R == from.R && w.G == from.G && w.B == from.B) d.White = to;
    }

    /// <summary>A five-pointed star of light on <paramref name="bone"/> facing <paramref name="facing"/> (the tip of an antenna).</summary>
    private static void LightStar(PokeBuilder b, int bone, Vector3 at, Vector3 facing, float r, Color color)
    {
        var n = Vector3.Normalize(facing);
        var u = Vector3.Normalize(Vector3.Cross(MathF.Abs(n.Y) > 0.9f ? V(1f, 0, 0) : V(0, 1f, 0), n));
        var v = Vector3.Cross(n, u);
        b.Ell(bone, at, V(r * 0.55f, r * 0.55f, r * 0.55f), color, mat: Glow, blend: 0.004f);
        for (int i = 0; i < 5; i++)
        {
            float a = MathF.PI / 2f + i * MathF.Tau / 5f;
            var way = u * MathF.Cos(a) + v * MathF.Sin(a);
            b.Spike(bone, at + way * r * 0.3f, at + way * r * 1.25f, r * 0.42f, color, 0.5f, Glow, 0.004f);
        }
    }

    // ------------------------------------------------------------------ 2162 Ermilong, from Furret

    /// <summary>
    /// Ermilong: Furret down on four short legs, its body grown into one long sinuous curve with the neck rising high
    /// at the front, its stripes gone to a winter coat of white shaded blue-grey, a fringe of longer fur along its
    /// spine, a dark mask round its eyes, and its bushy tail curling up to a black tip (an ermine's).
    /// </summary>
    private static PokeBuilder Ermilong()
    {
        var b = new PokeBuilder("Ermilong", 0.94f, BodyPlan.Quadruped, V(0, 0.24f, -0.04f)) { Coat = Fur };
        var white = Rgb(244, 246, 250);
        var shade = Rgb(196, 206, 222);
        var fringe = Rgb(214, 224, 236);
        var black = Rgb(36, 38, 46);
        var mask = Rgb(72, 80, 102);
        var pink = Rgb(222, 170, 176);
        // Four short legs under the long body, the hind pair set well back
        foreach (var (z, front) in new[] { (0.16f, true), (-0.26f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.06f * s, 0.2f, z), front);
                b.Limb(leg, V(0.06f * s, 0.2f, z), V(0.07f * s, 0.04f, z + 0.02f), 0.04f, 0.026f, white);
                b.Ell(leg, V(0.072f * s, 0.022f, z + 0.045f), V(0.03f, 0.02f, 0.045f), shade);
                Claws(b, leg, V(0.072f * s, 0.016f, z + 0.085f), V(0.012f, 0, 0), V(0, -0.2f, 1f), 0.016f, 0.006f);
            });
        // The long body: low over the hind legs, dipping, rising over the shoulders into a tall neck
        var spine = Smooth(3, V(0, 0.22f, -0.34f), V(0, 0.27f, -0.18f), V(0, 0.23f, -0.02f), V(0, 0.24f, 0.12f), V(0, 0.34f, 0.22f), V(0, 0.5f, 0.27f), V(0, 0.64f, 0.27f));
        b.Tube(Body, spine, 0.078f, 0.05f, white, blend: 0.02f);
        // Blue-grey shade along its underside, a fringe of longer fur down its spine and the back of its neck
        b.PaintEll(Body, V(0, 0.15f, -0.08f), V(0.08f, 0.05f, 0.32f), shade);
        for (int i = 2; i < spine.Length - 2; i++)
        {
            var at = spine[i];
            var along = Vector3.Normalize(spine[i + 1] - spine[i - 1]);
            var up = Vector3.Normalize(Vector3.Cross(Vector3.Cross(along, V(0, 1f, 0)), along));
            if (up.Y < 0) up = -up;
            if (along.Y > 0.6f) up = V(0, 0.2f, -1f);
            var root = at + up * 0.055f;
            b.Spike(Body, root, root + up * 0.05f - along * 0.05f, 0.022f, fringe, 0.45f, blend: 0.01f);
        }
        // The tail: thick and bushy, curling up behind and forward over the back, black for its last third
        int tail = b.Tail(V(0, 0.22f, -0.36f));
        var path = Smooth(3, V(0, 0.22f, -0.36f), V(0, 0.2f, -0.52f), V(0, 0.32f, -0.64f), V(0, 0.5f, -0.62f), V(0, 0.62f, -0.52f), V(0, 0.64f, -0.42f));
        b.Tube(tail, path, 0.06f, 0.035f, white, blend: 0.02f);
        int from = path.Length * 2 / 3;
        for (int i = from - 1; i < path.Length; i++)
            b.PaintEll(tail, path[i], V(0.075f, 0.075f, 0.075f), black, soft: 0.01f);
        for (int i = 2; i < path.Length - 1; i += 2)
        {
            var along = Vector3.Normalize(path[i + 1] - path[i - 1]);
            var side = Vector3.Normalize(Vector3.Cross(along, V(1f, 0, 0)));
            foreach (float sgn in new[] { 1f, -1f })
                b.Spike(tail, path[i], path[i] + side * sgn * 0.08f - along * 0.04f, 0.026f, i >= from ? black : white, 0.45f, blend: 0.01f);
        }
        b.Spike(tail, path[^1], path[^1] + V(0, -0.02f, 0.08f), 0.03f, black, 0.5f);
        // The head: small and sleek, a short muzzle, round ears pink inside, a dark mask across its eyes
        int head = b.Head(V(0, 0.64f, 0.27f));
        var c = V(0, 0.68f, 0.32f);
        var r = V(0.065f, 0.055f, 0.075f);
        b.Ell(head, c, r, white);
        b.Ell(head, V(0, 0.66f, 0.39f), V(0.036f, 0.03f, 0.04f), white);
        b.Ell(head, V(0, 0.667f, 0.432f), V(0.012f, 0.01f, 0.009f), pink, blend: 0.004f);
        b.PaintEll(head, V(0, 0.69f, 0.35f), V(0.075f, 0.022f, 0.045f), mask);
        PokeBuilder.Both(s =>
        {
            b.Ell(head, V(0.05f * s, 0.725f, 0.3f), V(0.024f, 0.024f, 0.014f), white, V(0, 20f * s, 0));
            b.PaintEll(head, V(0.052f * s, 0.727f, 0.31f), V(0.014f, 0.015f, 0.01f), pink, V(0, 20f * s, 0));
            var look = V(0.5f * s, 0.05f, 1f);
            b.Eye(head, Out(c, r, default, look), look, 0.013f, Rgb(40, 40, 48));
        });
        return b;
    }

    // ------------------------------------------------------------------ 2164 Noctaugur, from Noctowl

    /// <summary>
    /// Noctaugur: Noctowl taller, its brown deepened to the blue-black of the night sky with pale gold bars across its
    /// folded wings and gold tips to its tail, amber eyes ringed with small gold dots, a third eye closed on its brow
    /// between the tufts, and gold talons.
    /// </summary>
    private static PokeBuilder Noctaugur()
    {
        var b = Become(Noctowl(), "Noctaugur", 0.92f);
        var navy = Rgb(44, 52, 98);
        var deep = Rgb(28, 32, 66);
        var front = Rgb(64, 76, 128);
        var gold = Rgb(226, 190, 104);
        var pale = Rgb(230, 222, 186);
        Recolour(b, (Rgb(150, 108, 76), navy), (Rgb(84, 58, 44), deep), (Rgb(232, 210, 170), front), (Rgb(236, 220, 178), Rgb(60, 70, 120)),
            (Rgb(220, 70, 60), Rgb(236, 150, 48)), (Rgb(232, 196, 170), Rgb(52, 58, 90)), (Claw, gold));
        int head = BoneOf(b, "head");
        Rewhite(b, Rgb(220, 70, 60), Rgb(236, 150, 48));
        // No arrowheads on its breast: a night sky, flecked with a few pale stars
        b.Model.Decals.RemoveAll(d => d.Bone == Body && d.Shape == MarkShape.Triangle);
        foreach (var (x, y) in new[] { (-0.06f, 0.5f), (0.04f, 0.44f), (-0.02f, 0.36f), (0.07f, 0.33f), (-0.07f, 0.4f) })
            b.PaintEll(Body, V(x, y, 0.135f), V(0.01f, 0.01f, 0.01f), pale, soft: 0.004f);
        // Pale gold edges to the crest's feathers
        PokeBuilder.Both(s => b.PaintEll(head, V(0.17f * s, 0.98f, -0.01f), V(0.07f, 0.04f, 0.05f), pale));
        // Bars of pale gold across the folded wings, gold tips to the tail
        foreach (var wing in new[] { BoneOf(b, "wingL"), BoneOf(b, "wingR") })
        {
            float s = wing == BoneOf(b, "wingL") ? -1f : 1f;
            foreach (float y in new[] { 0.32f, 0.4f, 0.48f, 0.56f })
                b.PaintEll(wing, V(0.17f * s, y, -0.03f), V(0.07f, 0.011f, 0.14f), y > 0.5f ? pale : gold, V(0, 0, 4f * s), soft: 0.006f);
        }
        b.PaintEll(BoneOf(b, "tail"), V(0, 0.19f, -0.21f), V(0.08f, 0.04f, 0.05f), gold);
        // Rings of small gold dots round each eye
        var hc = V(0, 0.75f, 0.03f);
        var hr = V(0.14f, 0.12f, 0.12f);
        PokeBuilder.Both(s =>
        {
            var eye = V(0.05f * s, 0.765f, 0.135f);
            var look = Vector3.Normalize(V(0.4f * s, 0.05f, 1f));
            var u = Vector3.Normalize(Vector3.Cross(V(0, 1f, 0), look));
            var v = Vector3.Cross(look, u);
            for (int i = 0; i < 12; i++)
            {
                float a = i * MathF.Tau / 12f;
                var p = eye + (u * MathF.Cos(a) + v * MathF.Sin(a)) * 0.048f;
                var at = Out(hc, hr, default, p - hc);
                b.PaintEll(head, at, V(0.009f, 0.009f, 0.009f), gold, soft: 0.004f);
            }
        });
        // The third eye on its brow, closed, in a gold lid
        b.Model.Decals.RemoveAll(d => d.Bone == head && d.Shape == MarkShape.Triangle);
        var brow = Out(hc, hr, default, V(0, 0.55f, 1f));
        b.Mark(head, brow, Outward(hc, hr, brow), 0.03f, 0.016f, gold);
        b.Eye(head, brow, Outward(hc, hr, brow), 0.014f, closed: true);
        Stretch(b, V(1f, 1.08f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2166 Coccinova, from Ledian

    /// <summary>
    /// Coccinova: Ledian come down to stand on two heavy legs in a boxer's crouch, its body gone a deep night blue
    /// with an orange collar and a V down its breast, its red shell broadened over its back with five spots that glow
    /// pale yellow, its four white gloves grown into heavy mitts with red cuffs, and its antennae ending in small
    /// stars of light.
    /// </summary>
    /// <param name="female">A female keeps Ledian's: shorter antennae.</param>
    private static PokeBuilder Coccinova(bool female = false)
    {
        var b = new PokeBuilder("Coccinova", 0.92f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Shell };
        var navy = Rgb(60, 54, 88);
        var red = Rgb(214, 62, 52);
        var spot = Rgb(250, 240, 162);
        var orange = Rgb(240, 168, 52);
        var mitt = Rgb(236, 238, 242);
        var cuff = Rgb(206, 52, 46);
        // Two heavy legs, plated at the knee, set wide in a crouch
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.3f, 0));
            b.Limb(leg, V(0.07f * s, 0.31f, 0), V(0.11f * s, 0.17f, 0.04f), 0.055f, 0.045f, navy);
            b.Limb(leg, V(0.11f * s, 0.17f, 0.04f), V(0.1f * s, 0.05f, 0.0f), 0.045f, 0.035f, navy);
            b.Ell(leg, V(0.115f * s, 0.18f, 0.07f), V(0.035f, 0.04f, 0.02f), Rgb(76, 70, 108));
            b.Ell(leg, V(0.1f * s, 0.028f, 0.03f), V(0.05f, 0.028f, 0.07f), navy);
        });
        // The body upright, an orange collar and a V down its breast
        b.Ell(Body, V(0, 0.45f, 0.02f), V(0.12f, 0.16f, 0.1f), navy);
        b.PaintEll(Body, V(0, 0.585f, 0.05f), V(0.09f, 0.016f, 0.07f), orange);
        PokeBuilder.Both(s => b.PaintEll(Body, V(0.035f * s, 0.47f, 0.1f), V(0.012f, 0.11f, 0.04f), orange, V(0, 0, 22f * s), soft: 0.006f));
        // The shell over its back: a broad red dome split down the middle, its five spots glowing
        var sc = V(0, 0.47f, -0.09f);
        var sr = V(0.2f, 0.23f, 0.14f);
        b.Ell(Body, sc, sr, red, mat: Shell);
        b.PaintEll(Body, sc + V(0, 0, -0.14f), V(0.008f, 0.24f, 0.05f), Rgb(60, 40, 44), soft: 0.006f);
        foreach (var dir in new[] { V(0.45f, 0.55f, -1f), V(-0.45f, 0.55f, -1f), V(0.75f, -0.2f, -1f), V(-0.75f, -0.2f, -1f), V(0.35f, -0.7f, -1f) })
        {
            var at = Out(sc, sr, default, dir);
            PaintAs(b, Body, at, V(0.04f, 0.04f, 0.04f), spot, Glow);
        }
        // Four arms raised like a boxer's, each in a heavy white mitt with a red cuff
        foreach (var (i, y, hand) in new[] { (0, 0.54f, V(0.2f, 0.62f, 0.15f)), (1, 0.42f, V(0.22f, 0.42f, 0.15f)) })
            PokeBuilder.Both(s =>
            {
                var shoulder = V(0.1f * s, y, 0.03f);
                var h = V(hand.X * s, hand.Y, hand.Z);
                int arm = b.Part((s < 0 ? "armL" : "armR") + i, Body, shoulder, PokeRole.Arm, i * 0.7f, s);
                var elbow = V(0.2f * s, y - 0.06f, 0.02f);
                b.Tube(arm, new[] { shoulder, elbow, h }, 0.024f, 0.02f, navy, blend: 0.006f);
                var d = Vector3.Normalize(h - elbow);
                b.Limb(arm, h - d * 0.04f, h - d * 0.012f, 0.03f, 0.03f, cuff, Shell, 0.004f);
                b.Ell(arm, h + d * 0.03f, V(0.048f, 0.044f, 0.05f), mitt, blend: 0.008f);
            });
        // The head: night blue, great blue eyes, small mandibles, antennae tipped with stars
        int head = b.Head(V(0, 0.6f, 0.04f));
        var hc = V(0, 0.69f, 0.06f);
        var hr = V(0.085f, 0.075f, 0.08f);
        b.Ell(head, hc, hr, navy);
        PokeBuilder.Both(s => b.Spike(head, hc + V(0.02f * s, -0.06f, 0.06f), hc + V(0.012f * s, -0.09f, 0.07f), 0.01f, Rgb(80, 74, 110), blend: 0.004f));
        float reach = female ? 0.7f : 1f;
        PokeBuilder.Both(s =>
        {
            var root = hc + V(0.03f * s, hr.Y * 0.8f, -0.01f);
            var tip = root + V(0.04f * s, 0.14f, 0.02f) * reach;
            b.Tube(head, Smooth(3, root, root + V(0.02f * s, 0.07f, -0.01f) * reach, tip), 0.009f, 0.007f, Rgb(44, 40, 64), blend: 0f);
            LightStar(b, head, tip + V(0, 0.018f, 0), V(0, 0.1f, 1f), 0.032f, spot);
        });
        PokeBuilder.Both(s =>
        {
            var look = V(0.45f * s, 0.05f, 1f);
            b.Eye(head, Out(hc, hr, default, look), look, 0.032f, Rgb(70, 120, 214));
        });
        return b;
    }

    // ------------------------------------------------------------------ 2168 Orbweavix, from Ariados

    /// <summary>
    /// Orbweavix: Ariados grown heavy in a great round abdomen of dark red and violet with an orb web traced across it
    /// in gold, its legs banded red, black and cream, its white horn grown long and pointing ahead, and a small round
    /// web of white strands strung between its hind legs, which it carries.
    /// </summary>
    private static PokeBuilder Orbweavix()
    {
        var b = Become(Ariados(), "Orbweavix", 0.97f);
        var maroon = Rgb(140, 40, 52);
        var violet = Rgb(126, 72, 168);
        var gold = Rgb(236, 196, 92);
        var cream = Rgb(236, 228, 168);
        var dark = Rgb(56, 30, 40);
        var silk = Rgb(240, 240, 246);
        int head = BoneOf(b, "head");
        b.Sdf.RemoveAll(s => s.Bone == head && s.Color.Equals(Rgb(240, 240, 240)));
        Recolour(b, (Rgb(210, 60, 72), maroon), (Rgb(150, 110, 190), Rgb(176, 46, 52)), (Rgb(250, 214, 80), cream));
        // The legs darkened toward their ends, banded black between the cream
        foreach (var leg in Enumerable.Range(0, b.Model.Bones.Count).Where(i => b.Model.Bones[i].Role == PokeRole.Leg))
        {
            var tube = b.Sdf.Shapes.Where(s => s.Bone == leg && s.Op == SdfOp.Add).ToList();
            foreach (var t in tube.Where(t => t.Shape == SdfShape.RoundCone))
                b.PaintEll(leg, Vector3.Lerp(t.A, t.B, 0.85f), V(0.028f, 0.028f, 0.028f), dark, soft: 0.006f);
        }
        // The great abdomen behind, its web of gold over violet and maroon
        var ac = V(0, 0.42f, -0.44f);
        var ar = V(0.26f, 0.25f, 0.28f);
        b.Ell(Body, ac, ar, maroon, blend: 0.05f);
        b.PaintEll(Body, ac + V(0, 0.12f, 0.02f), V(0.22f, 0.16f, 0.24f), violet);
        var hub = Out(ac, ar, default, V(0.1f, 0.55f, -0.8f));
        var n = Outward(ac, ar, hub);
        foreach (float ring in new[] { 0.05f, 0.1f, 0.15f, 0.2f })
            b.PaintTorus(Body, hub - n * 0.02f, ring, 0.009f, gold, Euler(n));
        foreach (var d in new[] { V(0.9f, 0.3f, 0.2f), V(-0.9f, 0.3f, 0.1f), V(0.6f, 0.8f, 0.2f), V(-0.6f, 0.8f, 0.0f) })
            b.PaintEll(Body, Out(ac, ar, default, d), V(0.03f, 0.12f, 0.05f), violet, Euler(Vector3.Normalize(d)) + V(0, 0, 60f));
        var u = Vector3.Normalize(Vector3.Cross(n, V(1f, 0, 0)));
        var w = Vector3.Cross(n, u);
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f;
            var way = u * MathF.Cos(a) + w * MathF.Sin(a);
            b.PaintEll(Body, hub + way * 0.14f, V(0.006f, 0.006f, 0.14f), gold, Euler(way), soft: 0.004f);
        }
        // The horn, long and white, pointing ahead over its face
        var hc = V(0, 0.21f, 0.15f);
        b.Spike(head, hc + V(0, 0.07f, 0.02f), hc + V(0, 0.13f, 0.34f), 0.03f, Rgb(244, 238, 236), 0.8f, Shell);
        // The web it carries, strung between its hind legs: spokes from a hub and three rings of thread
        var webHub = V(0, 0.11f, -0.3f);
        var anchors = new[] { V(0.39f, 0.42f, -0.27f), V(0.5f, 0.14f, -0.3f), V(0.22f, 0.015f, -0.3f), V(-0.22f, 0.015f, -0.3f), V(-0.5f, 0.14f, -0.3f), V(-0.39f, 0.42f, -0.27f) };
        foreach (var a in anchors) b.Limb(Body, webHub, a, 0.007f, 0.007f, silk, blend: 0f);
        foreach (float t in new[] { 0.3f, 0.55f, 0.8f })
        {
            var ring = anchors.Select(a => Vector3.Lerp(webHub, a, t)).ToArray();
            for (int i = 0; i < ring.Length; i++)
                b.Limb(Body, ring[i], ring[(i + 1) % ring.Length], 0.006f, 0.006f, silk, blend: 0f);
        }
        return b;
    }
    // ------------------------------------------------------------------ 2171 Abyssolux, from Lanturn

    /// <summary>
    /// Abyssolux: Lanturn grown long and dark, deep navy over a pale belly, a jaw of thin teeth, a tall dorsal fin,
    /// its one light on its stalk joined by two more (yellow and pale blue), glowing cyan eyes and two rows of small
    /// lights down each flank.
    /// </summary>
    private static PokeBuilder Abyssolux()
    {
        var b = Become(Lanturn(), "Abyssolux", 0.98f);
        var navy = Rgb(34, 46, 88);
        var belly = Rgb(196, 214, 232);
        var fin = Rgb(84, 118, 160);
        var yellow = Rgb(250, 230, 120);
        var blue = Rgb(170, 220, 250);
        var dot = Rgb(176, 244, 214);
        Recolour(b, (Rgb(70, 110, 200), navy), (Rgb(150, 190, 236), fin), (Rgb(250, 220, 90), yellow));
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
        {
            d.Iris = Rgb(84, 232, 226);
            d.White = Rgb(30, 44, 78);
            d.Pupil = Rgb(220, 250, 250);
        }
        var lift = Shifted(b, "lure", V(0, 0.46f, 0.1f));
        var c = V(0, 0.32f, 0) + lift;
        var r = V(0.15f, 0.14f, 0.22f);
        b.PaintEll(Body, c + V(0, -0.075f, 0.04f), V(0.16f, 0.075f, 0.22f), belly);
        // Two more lights on stalks from its brow, one to each side
        int lure = BoneOf(b, "lure");
        PokeBuilder.Both(s =>
        {
            var path = Smooth(3, c + V(0.04f * s, r.Y * 0.8f, 0.1f), c + V(0.1f * s, r.Y + 0.08f, 0.1f), c + V(0.15f * s, r.Y + 0.02f, 0.22f));
            b.Tube(lure, path, 0.011f, 0.009f, navy, blend: 0f);
            b.Ell(lure, path[^1] + V(0, -0.02f, 0.01f), V(0.032f, 0.036f, 0.032f), s < 0 ? blue : yellow, mat: Glow, blend: 0.006f);
        });
        // A tall ragged dorsal fin
        b.Ell(Body, c + V(0, r.Y * 0.85f, -0.05f), V(0.012f, 0.06f, 0.12f), fin, V(-20f, 0, 0), blend: 0.02f);
        foreach (var (z, h) in new[] { (0.04f, 0.08f), (-0.04f, 0.12f), (-0.12f, 0.1f) })
            Frond(b, Body, c + V(0, r.Y * 0.8f, z), c + V(0, r.Y + h, z - 0.08f), 0.035f, fin, V(1f, 0, 0), 0.25f);
        // A jaw of thin teeth along its lower lip
        for (int i = 0; i < 7; i++)
        {
            float x = -0.06f + 0.02f * i;
            var at = Out(c, r, default, V(x * 4f, -0.32f, 1f));
            b.Spike(Body, at - V(0, 0.005f, 0.004f), at + V(0, 0.028f, 0.008f), 0.009f, Rgb(240, 242, 236), blend: 0.003f);
        }
        // Two rows of small lights down each flank
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < 9; i++)
                foreach (float y in new[] { 0.12f, -0.12f })
                {
                    float z = 0.08f - i * 0.045f;
                    var dir = V(s, y + i * 0.01f, z / r.Z * 1.2f);
                    var at = Out(c, r, default, dir);
                    b.PaintEll(Body, at, V(0.011f, 0.011f, 0.011f), dot, soft: 0.004f);
                }
        });
        Stretch(b, V(1f, 1f, 1.45f));
        return b;
    }

    // ------------------------------------------------------------------ 2178 Totemsight, from Xatu

    /// <summary>
    /// Totemsight: Xatu taller and straighter, its folded wings a deep green cloak whose front is carved with two
    /// faces one over the other, as on a totem pole, a crest of green, white and red feathers on its head, a hooked
    /// tan beak, and two long red tail feathers marked with white. The female's bands are kept.
    /// </summary>
    /// <param name="female">A female keeps Xatu's: one band fewer on her cloak.</param>
    private static PokeBuilder Totemsight(bool female = false)
    {
        var b = Become(Xatu(female), "Totemsight", 0.94f);
        var green = Rgb(64, 150, 84);
        var bright = Rgb(110, 200, 92);
        var cream = Rgb(244, 236, 214);
        var red = Rgb(196, 58, 50);
        var yellow = Rgb(240, 200, 70);
        var ink = Rgb(40, 50, 44);
        Recolour(b, (Rgb(250, 196, 70), Rgb(214, 176, 120)), (Rgb(110, 190, 74), green));
        // The cloak: green over the body's sides and back, a carved panel down its front
        var c = V(0, 0.38f, 0);
        var r = V(0.15f, 0.28f, 0.12f);
        b.Model.Decals.RemoveAll(d => d.Bone == Body && !d.IsEye);
        b.PaintEll(Body, c + V(0, 0.02f, -0.03f), V(0.17f, 0.26f, 0.14f), green);
        b.PaintEll(Body, c + V(0, 0.0f, 0.1f), V(0.1f, 0.25f, 0.06f), cream);
        PokeBuilder.Both(s =>
        {
            foreach (float y in new[] { 0.3f, 0.42f, 0.54f })
                b.PaintEll(Body, V(0.13f * s, y, 0.0f), V(0.04f, 0.012f, 0.12f), bright, V(0, 0, -30f * s), soft: 0.006f);
        });
        // The two carved faces, one over the other
        foreach (var (y, k) in new[] { (0.48f, 1.55f), (0.27f, 1.3f) })
        {
            PokeBuilder.Both(s =>
            {
                var eye = On(c, r, 0.03f * s * k, y + 0.03f * k);
                b.Mark(Body, eye, Outward(c, r, eye), 0.022f * k, 0.014f * k, Rgb(250, 248, 240));
                b.Mark(Body, eye, Outward(c, r, eye), 0.012f * k, 0.012f * k, Rgb(90, 130, 90));
                var brow = On(c, r, 0.035f * s * k, y + 0.06f * k);
                b.Mark(Body, brow, Outward(c, r, brow), 0.024f * k, 0.012f * k, red, MarkShape.Bar, 18f * s);
                var cheek = On(c, r, 0.05f * s * k, y - 0.01f * k);
                b.Mark(Body, cheek, Outward(c, r, cheek), 0.012f * k, 0.012f * k, red);
            });
            var nose = On(c, r, 0, y);
            b.Mark(Body, nose, Outward(c, r, nose), 0.012f * k, 0.026f * k, red, MarkShape.Triangle, 180f);
            var mouth = On(c, r, 0, y - 0.05f * k);
            b.Mark(Body, mouth, Outward(c, r, mouth), 0.04f * k, 0.012f * k, red, MarkShape.Bar);
        }
        // Long red tail feathers trailing to the ground, marked white
        int tail = BoneOf(b, "tail");
        foreach (float x in new[] { -0.05f, 0.05f })
        {
            var root = c + V(x, -0.12f, -0.1f);
            var tip = c + V(x * 3f, -0.36f, -0.42f);
            Frond(b, tail, root, tip, 0.035f, red, V(0, 1f, -0.3f), 0.25f);
            for (int i = 1; i <= 3; i++)
                b.PaintEll(tail, Vector3.Lerp(root, tip, 0.25f + 0.2f * i), V(0.02f, 0.02f, 0.02f), cream, soft: 0.005f);
        }
        // A crest of green, white and red feathers fanned over its head, a white face round its eyes
        int head = BoneOf(b, "head");
        var hc = V(0, 0.72f, 0.02f);
        var hr = V(0.09f, 0.09f, 0.09f);
        b.PaintEll(head, hc + V(0, -0.03f, 0.07f), V(0.07f, 0.05f, 0.04f), cream);
        var crest = new[] { green, cream, red, green, cream, red, green };
        for (int i = 0; i < crest.Length; i++)
        {
            float a = (-60f + 20f * i) * Degree;
            var root = hc + V(MathF.Sin(a) * 0.05f, 0.06f, -0.02f);
            var tip = hc + V(MathF.Sin(a) * 0.15f, 0.08f + MathF.Cos(a) * 0.1f, -0.07f);
            Frond(b, head, root, tip, 0.022f, crest[i], V(0, 0.2f, 1f), 0.3f);
        }
        b.Spike(head, hc + V(0, -0.01f, 0.1f), hc + V(0, -0.07f, 0.14f), 0.018f, Rgb(214, 176, 120), 0.7f, Shell, 0.006f);
        Stretch(b, V(1f, 1.1f, 1f));
        return b;
    }
    // ------------------------------------------------------------------ 2185 Mimicedar, from Sudowoodo

    /// <summary>
    /// Mimicedar: Sudowoodo grown taller and older, its brown gone to darker bark with plates of grey stone over its
    /// belly where its buds were, the green balls on its arms and head grown into real leafy crowns, and roots
    /// spreading from its feet over the ground. The female's smaller branch is kept under her crown.
    /// </summary>
    /// <param name="female">A female keeps Sudowoodo's: a smaller branch on her head.</param>
    private static PokeBuilder Mimicedar(bool female = false)
    {
        var b = Become(Sudowoodo(female), "Mimicedar", 0.98f);
        var bark = Rgb(122, 88, 60);
        var dark = Rgb(88, 62, 44);
        var stone = Rgb(150, 150, 142);
        var leaf = Rgb(58, 130, 56);
        var light = Rgb(92, 160, 72);
        Recolour(b, (Rgb(150, 108, 70), bark));
        // The buds give way to plates of grey stone over its belly, and grooves of darker bark run up its trunk
        b.Model.Decals.RemoveAll(d => d.Bone == Body && d.Shape == MarkShape.Disc);
        foreach (var (x, y, w) in new[] { (0f, 0.26f, 0.09f), (-0.07f, 0.31f, 0.06f), (0.07f, 0.3f, 0.06f), (0.0f, 0.36f, 0.06f) })
            b.PaintEll(Body, V(x, y, 0.11f), V(w, 0.04f, 0.04f), stone, soft: 0.006f);
        foreach (float x in new[] { -0.08f, -0.03f, 0.03f, 0.08f })
            b.PaintEll(Body, V(x, 0.52f, 0.1f), V(0.006f, 0.1f, 0.05f), dark, soft: 0.004f);
        // Leafy crowns: on each arm's end and over its head
        foreach (var (bone, at, r) in new[] { (BoneOf(b, "armL"), V(-0.33f, 0.72f, 0.03f), V(0.11f, 0.1f, 0.1f)), (BoneOf(b, "armR"), V(0.33f, 0.72f, 0.03f), V(0.11f, 0.1f, 0.1f)), (BoneOf(b, "head"), V(0, female ? 0.9f : 0.95f, -0.01f), V(0.15f, 0.08f, 0.12f)) })
        {
            b.Ell(bone, at, r, leaf, mat: Leaf, blend: 0.02f);
            Lumps(b, bone, at, r, 20, MathF.Min(r.X, r.Y) * 0.4f, leaf, light, 10f);
        }
        // Roots spreading over the ground from each foot
        PokeBuilder.Both(s =>
        {
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            foreach (float a in new[] { -40f, 10f, 60f, 110f })
            {
                float t = (a * s + (s < 0 ? 180f : 0f)) * Degree;
                var foot = V(0.1f * s, 0.03f, 0.04f);
                var way = V(MathF.Cos(t), 0, MathF.Sin(t));
                b.Spike(leg, foot, foot + way * 0.13f + V(0, -0.02f, 0), 0.026f, bark, 0.7f);
            }
        });
        Stretch(b, V(1.05f, 1.1f, 1.05f));
        return b;
    }

    // ------------------------------------------------------------------ 2192 Helianthra, from Sunflora

    /// <summary>
    /// Helianthra: Sunflora grown tall, its face a disc that glows like the sun, its ring of petals twice as wide and
    /// gold shading to orange with a second ring behind, a crown of small flames round them, two broad leaves for
    /// arms veined in orange, and its stem twisted in green and orange like a flame.
    /// </summary>
    private static PokeBuilder Helianthra()
    {
        var b = Become(Sunflora(), "Helianthra", 0.98f);
        var gold = Rgb(252, 200, 60);
        var orange = Rgb(238, 120, 40);
        var face = Rgb(252, 230, 120);
        var green = Rgb(80, 140, 70);
        int head = BoneOf(b, "head");
        foreach (var s in b.Sdf.Shapes.Where(s => s.Color.Equals(Rgb(250, 228, 124)))) s.Material = Glow;
        Recolour(b, (Rgb(250, 228, 124), face), (Rgb(250, 196, 50), gold), (Rgb(112, 182, 92), green));
        foreach (var d in b.Model.Decals.Where(d => d.IsEye)) d.Closed = false;
        // A second, wider ring of petals behind the first, orange, and a crown of small flames round it
        var c = V(0, 0.43f, 0.02f);
        FlowerHead(b, head, c - V(0, 0, 0.02f), V(0, 0, 1f), 21, 0.26f, 0.05f, orange, face, 0.09f, 0.5f);
        for (int i = 0; i < 18; i++)
        {
            float a = i * MathF.Tau / 18f;
            var o = V(MathF.Cos(a), MathF.Sin(a), 0);
            FlameTongue(b, head, c + o * 0.25f - V(0, 0, 0.03f), c + o * 0.31f - V(0, 0, 0.03f), 0.016f, orange, Rgb(255, 220, 110));
        }
        // Broad leaves for arms, veined orange
        foreach (var arm in new[] { BoneOf(b, "armL"), BoneOf(b, "armR") })
        {
            float s = arm == BoneOf(b, "armL") ? -1f : 1f;
            Frond(b, arm, V(0.04f * s, 0.24f, 0), V(0.3f * s, 0.34f, 0.02f), 0.09f, green, V(0, 1f, 0.3f), 0.18f, Leaf);
            b.PaintEll(arm, V(0.17f * s, 0.29f, 0.01f), V(0.13f, 0.008f, 0.08f), orange, V(0, 0, 21f * s), soft: 0.005f);
            foreach (float t in new[] { 0.35f, 0.6f, 0.8f })
                b.PaintEll(arm, V((0.04f + 0.26f * t) * s, 0.24f + 0.1f * t + 0.03f, 0.01f), V(0.006f, 0.04f, 0.08f), orange, V(0, 0, -30f * s), soft: 0.004f);
        }
        // The stem twisted green and orange like a flame
        for (int i = 0; i < 4; i++)
            b.PaintEll(Body, V(0.02f * (i % 2 == 0 ? 1 : -1), 0.15f + i * 0.045f, 0.05f), V(0.03f, 0.012f, 0.04f), orange, V(0, 0, (i % 2 == 0 ? 30f : -30f)), soft: 0.006f);
        Stretch(b, V(1.08f, 1.12f, 1.08f));
        return b;
    }

    // ------------------------------------------------------------------ 2195 Mirelord, from Quagsire

    /// <summary>
    /// Mirelord: Quagsire broader and heavier, its blue darkened to the slate of a marsh with a pale belly, moss
    /// growing over its head, shoulders and tail and reeds and cattails standing in it, and three long frilled gills
    /// on each side of its head like an axolotl's. The female's smaller fin is kept.
    /// </summary>
    /// <param name="female">A female keeps Quagsire's: a smaller fin.</param>
    private static PokeBuilder Mirelord(bool female = false)
    {
        var b = Become(Quagsire(female), "Mirelord", 0.98f);
        var slate = Rgb(84, 126, 132);
        var belly = Rgb(214, 212, 168);
        var moss = Rgb(96, 136, 60);
        var mossLight = Rgb(132, 164, 74);
        var gill = Rgb(56, 84, 90);
        var frill = Rgb(120, 60, 70);
        var reed = Rgb(110, 150, 70);
        Recolour(b, (Rgb(112, 192, 232), slate), (Rgb(40, 74, 128), moss));
        b.PaintEll(Body, V(0, 0.32f, 0.1f), V(0.15f, 0.22f, 0.1f), belly);
        int head = BoneOf(b, "head");
        b.PaintEll(head, V(0, 0.62f, 0.12f), V(0.13f, 0.05f, 0.08f), belly);
        // Moss over its head, shoulders and tail
        b.PaintEll(head, V(0, 0.8f, 0.0f), V(0.16f, 0.06f, 0.14f), moss);
        Lumps(b, head, V(0, 0.79f, -0.01f), V(0.12f, 0.03f, 0.1f), 14, 0.03f, moss, mossLight, 10f);
        PokeBuilder.Both(s => b.PaintEll(Body, V(0.15f * s, 0.52f, -0.05f), V(0.08f, 0.06f, 0.1f), moss));
        b.PaintEll(BoneOf(b, "tail"), V(0.03f, 0.18f, -0.28f), V(0.07f, 0.05f, 0.12f), moss);
        // Reeds and cattails standing in the moss on its head
        foreach (var (x, z, h, cat) in new[] { (-0.07f, -0.02f, 0.16f, true), (-0.03f, 0.03f, 0.12f, false), (0.02f, -0.04f, 0.2f, true), (0.06f, 0.01f, 0.13f, false), (0.09f, -0.05f, 0.17f, true) })
        {
            var root = V(x, 0.8f, z);
            var top = root + V(x * 0.3f, h, -0.01f);
            b.Limb(head, root, top, 0.009f, 0.008f, reed, Leaf, 0.004f);
            if (cat) b.Ell(head, top + V(0, 0.02f, 0), V(0.012f, 0.026f, 0.012f), Rgb(110, 72, 48), blend: 0.004f);
        }
        // Three frilled gills on each side of its head
        PokeBuilder.Both(s =>
        {
            foreach (var (dy, dz, len) in new[] { (0.07f, -0.01f, 0.14f), (0.0f, -0.02f, 0.16f), (-0.07f, -0.01f, 0.13f) })
            {
                var root = V(0.14f * s, 0.7f + dy * 0.6f, dz);
                var tip = root + V(0.09f * s, dy * 1.4f + 0.04f, -0.04f) * (len / 0.14f);
                b.Limb(head, root, tip, 0.016f, 0.01f, gill, blend: 0.006f);
                for (int i = 1; i <= 4; i++)
                {
                    var at = Vector3.Lerp(root, tip, 0.15f + 0.2f * i);
                    var along = Vector3.Normalize(tip - root);
                    foreach (float side in new[] { 1f, -1f })
                    {
                        var across = Vector3.Normalize(Vector3.Cross(along, V(0, 0, 1f))) * side;
                        b.Spike(head, at, at + across * 0.035f + along * 0.02f, 0.01f, frill, 0.5f, blend: 0.004f);
                    }
                }
            }
        });
        Stretch(b, V(1.14f, 1.04f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2196 Zenitheon, from Espeon

    /// <summary>
    /// Zenitheon: Espeon longer and finer on taller legs, a cream throat and a violet saddle down its back, its forked
    /// tail grown into three long tails each tipped with a plume of violet, and the red jewel on its brow ringed with a
    /// small halo of pale gold light.
    /// </summary>
    private static PokeBuilder Zenitheon()
    {
        var b = Become(Espeon(), "Zenitheon", 0.86f);
        var lilac = Rgb(222, 196, 236);
        var violet = Rgb(102, 84, 150);
        var cream = Rgb(244, 234, 218);
        var gold = Rgb(250, 226, 150);
        const float h = 0.24f;
        Recolour(b, (Rgb(232, 194, 232), lilac), (Rgb(130, 120, 200), violet));
        b.PaintEll(Body, V(0, h + 0.06f, 0.12f), V(0.06f, 0.08f, 0.05f), cream);
        b.PaintEll(Body, V(0, h + 0.1f, -0.05f), V(0.05f, 0.04f, 0.13f), violet);
        int head = BoneOf(b, "head");
        b.Torus(head, V(0, h + 0.27f, 0.214f), 0.026f, 0.006f, gold, V(90f, 0, 0), mat: Glow, blend: 0.003f);
        // Three long tails in place of the fork, each ending in a violet plume
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail);
        var root = V(0, h + 0.04f, -0.16f);
        foreach (var (x, up, back) in new[] { (0f, 0.26f, 0.2f), (0.08f, 0.1f, 0.3f), (-0.06f, 0.0f, 0.34f) })
        {
            var path = Smooth(3, root, root + V(x * 0.3f, -0.02f, -0.12f), root + V(x, up * 0.5f, -back * 0.8f), root + V(x * 1.2f, up, -back));
            b.Tube(tail, path, 0.02f, 0.012f, lilac, blend: 0.004f);
            var end = path[^1];
            var way = Vector3.Normalize(path[^1] - path[^3]);
            Frond(b, tail, end - way * 0.04f, end + way * 0.08f, 0.032f, violet, V(1f, 0, 0), 0.3f);
            b.PaintEll(tail, end + way * 0.03f, V(0.01f, 0.01f, 0.04f), gold, Euler(way), soft: 0.004f);
        }        Stretch(b, V(0.96f, 1.1f, 1.15f));
        return b;
    }
    // ------------------------------------------------------------------ 2197 Eclipseon, from Umbreon

    /// <summary>
    /// Eclipseon: Umbreon larger and leaner, its black gone to the deep blue of a night sky, its gold rings turned to
    /// eclipses (dark discs rimmed in pale gold) on its brow, haunches and shoulders and on the tuft of its tail, a mane
    /// of black fur at its neck, its ears longer, and amber eyes.
    /// </summary>
    private static PokeBuilder Eclipseon()
    {
        var b = Become(Umbreon(), "Eclipseon", 0.86f);
        var navy = Rgb(56, 64, 98);
        var black = Rgb(26, 28, 38);
        var rim = Rgb(246, 226, 170);
        var gold = Rgb(246, 210, 70);
        const float h = 0.26f;
        b.Sdf.RemoveAll(s => s.Bone == Body && s.Op != SdfOp.Add && s.Color.Equals(gold));
        Recolour(b, (Rgb(46, 50, 60), navy), (gold, black), (Rgb(220, 40, 50), Rgb(236, 168, 80)));
        int head = BoneOf(b, "head");
        // Eclipses: a dark disc rimmed in pale gold, on its brow and its haunches and shoulders
        foreach (var d in b.Model.Decals.Where(d => d.Shape == MarkShape.Ring)) { d.Color = rim; }
        var brow = b.Model.Decals.First(d => d.Shape == MarkShape.Ring);
        b.Mark(head, brow.Center, brow.Normal, 0.018f, 0.018f, black);
        foreach (var (z, y) in new[] { (0.04f, h * 1.1f), (-0.12f, h * 1.1f) })
            PokeBuilder.Both(s =>
            {
                var at = V(0.075f * s, y, z);
                var n = V(s, 0.2f, 0);
                b.Mark(Body, at, n, 0.045f, 0.045f, rim);
                b.Mark(Body, at, n, 0.036f, 0.036f, black);
            });
        // Longer ears, a mane of black fur round its neck
        foreach (var ear in new[] { BoneOf(b, "earL"), BoneOf(b, "earR") })
        {
            float s = ear == BoneOf(b, "earL") ? -1f : 1f;
            Grow(b, V(1f, 1.4f, 1f), V(0.05f * s, h + 0.27f, 0.13f), bone => bone == ear);
        }
        for (int i = 0; i < 11; i++)
        {
            float a = (-150f + 30f * i) * Degree;
            var o = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = V(0, h + 0.1f, 0.09f) + o * V(0.07f, 0, 0.06f);
            b.Spike(Body, root, root + o * 0.06f + V(0, -0.07f, 0), 0.035f, black, 0.5f, blend: 0.012f);
        }
        b.Ell(Body, V(0, h + 0.09f, 0.1f), V(0.08f, 0.08f, 0.07f), black, blend: 0.03f);
        // The tail ends in a black tuft with a crescent on it
        int tail = BoneOf(b, "tail");
        var end = V(0, h + 0.2f, -0.29f);
        b.Ell(tail, end + V(0, 0.03f, -0.02f), V(0.05f, 0.07f, 0.05f), black, V(-20f, 0, 0), blend: 0.02f);
        foreach (var (dx, dy) in new[] { (0f, 0.12f), (0.03f, 0.1f), (-0.03f, 0.09f) })
            b.Spike(tail, end + V(0, 0.05f, -0.02f), end + V(dx, dy, -0.05f), 0.03f, black, 0.5f);
        PokeBuilder.Both(s => b.Mark(tail, end + V(0.05f * s, 0.03f, -0.02f), V(s, 0, 0), 0.022f, 0.022f, rim, MarkShape.Ring));
        Stretch(b, V(1f, 1.1f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2199 Slowmonarch, from Slowking

    /// <summary>
    /// Slowmonarch: Slowking taller and heavier, its white ruff grown into a royal cape of pink lined and edged with
    /// white that falls down its back to the ground, the Shellder on its head grown into a greater spiral shell set
    /// with three jewels, red, blue and gold, and a staff of coral in its hand crowned with a pale shell.
    /// </summary>
    private static PokeBuilder Slowmonarch()
    {
        var b = Become(Slowking(), "Slowmonarch", 0.96f);
        var pink = Rgb(232, 140, 170);
        var deep = Rgb(206, 108, 148);
        var lining = Rgb(250, 236, 240);
        var shellPink = Rgb(238, 214, 210);
        var coral = Rgb(222, 96, 92);
        int head = BoneOf(b, "head");
        var hc = V(0, 0.66f, 0.04f);
        var mouth = hc + V(0, 0.095f, 0.0f);
        // The crown grows into a greater shell, pale pink, its spikes longer
        Grow(b, V(1.35f, 1.35f, 1.35f), mouth, s => s.Bone == head && (s.Color.Equals(Rgb(208, 210, 220)) || s.Color.Equals(Rgb(170, 176, 190)) || s.Color.Equals(Rgb(180, 186, 200))), _ => false);
        Recolour(b, (Rgb(208, 210, 220), shellPink), (Rgb(170, 176, 190), Rgb(226, 200, 196)), (Rgb(180, 186, 200), Rgb(220, 186, 190)), (White, lining));
        b.Sdf.RemoveAll(s => s.Bone == head && s.Material == Glow);
        foreach (var (x, color) in new[] { (-0.045f, Rgb(220, 50, 60)), (0f, Rgb(60, 110, 230)), (0.045f, Rgb(240, 190, 60)) })
            b.Ell(head, mouth + V(x, 0.06f, 0.11f - MathF.Abs(x) * 0.4f), V(0.018f, 0.022f, 0.014f), color, mat: Glow, blend: 0.004f);
        b.Torus(head, mouth + V(0, 0.06f, 0.0f), 0.105f, 0.012f, Rgb(232, 196, 96), V(-8f, 0, 0), mat: Metal, blend: 0.004f);
        // The cape: from its shoulders, falling down its back to the ground, pink lined with white
        for (int i = 0; i < 11; i++)
        {
            float a = (100f + i * 16f) * Degree;
            var o = V(MathF.Sin(a), 0, MathF.Cos(a));
            var top = V(0, 0.5f, -0.01f) + o * V(0.13f, 0, 0.11f);
            var bottom = V(0, 0.03f, -0.04f) + o * V(0.26f, 0, 0.22f);
            Frond(b, Body, top, bottom, 0.065f, i % 2 == 0 ? pink : deep, o + V(0, 0.3f, 0), 0.2f, Fur);
            b.PaintEll(Body, bottom + V(0, 0.03f, 0), V(0.07f, 0.025f, 0.05f), lining, soft: 0.006f);
        }
        // The staff of coral in its left hand, a pale spiral shell at its top
        int arm = BoneOf(b, "armL");
        var grip = V(-0.21f, 0.36f, 0.11f);
        b.Ell(arm, grip, V(0.032f, 0.03f, 0.03f), Rgb(240, 160, 184));
        var foot = V(-0.23f, 0.0f, 0.12f);
        var top2 = V(-0.2f, 0.78f, 0.1f);
        b.Limb(arm, foot, top2, 0.016f, 0.014f, coral, Shell, 0.004f);
        foreach (var (t, twig) in new[] { (0.75f, V(-0.08f, 0.1f, 0)), (0.85f, V(0.07f, 0.1f, 0)), (0.6f, V(-0.06f, 0.06f, 0.02f)) })
        {
            var root = Vector3.Lerp(foot, top2, t);
            b.Limb(arm, root, root + twig, 0.011f, 0.008f, coral, Shell, 0.004f);
        }
        b.Ell(arm, top2 + V(0, 0.03f, 0), V(0.035f, 0.035f, 0.03f), shellPink, mat: Shell, blend: 0.006f);
        b.Mark(arm, top2 + V(0, 0.03f, 0.03f), V(0, 0, 1f), 0.02f, 0.02f, Rgb(214, 160, 170), MarkShape.Ring);
        Stretch(b, V(1.06f, 1.08f, 1.06f));
        return b;
    }

    // ------------------------------------------------------------------ 2202 Wobbulwark, from Wobbuffet

    /// <summary>
    /// Wobbulwark: Wobbuffet larger and darker, its front grown into a shining mirror, round ears at the sides of its
    /// head, and its black tail grown into a dark twin of itself that rises behind it, wreathed in shadow, reaching
    /// forward with clawed hands and watching with two glowing eyes. The female's red lips are kept.
    /// </summary>
    /// <param name="female">A female keeps Wobbuffet's: red lips round her mouth.</param>
    private static PokeBuilder Wobbulwark(bool female = false)
    {
        var b = Become(Wobbuffet(female), "Wobbulwark", 0.98f);
        var blue = Rgb(64, 102, 160);
        var mirror = Rgb(206, 222, 238);
        var shadow = Rgb(36, 26, 52);
        var edge = Rgb(84, 60, 116);
        var eye = Rgb(246, 244, 190);
        Recolour(b, (Rgb(92, 172, 222), blue));
        var c = V(0, 0.34f, 0);
        var r = V(0.16f, 0.3f, 0.12f);
        PaintAs(b, Body, V(0, 0.24f, 0.1f), V(0.12f, 0.14f, 0.06f), mirror, Metal);
        // Round ears hanging at the sides of its head
        PokeBuilder.Both(s => b.Ell(Body, V(0.15f * s, 0.5f, 0.0f), V(0.06f, 0.035f, 0.045f), blue, V(0, 0, -25f * s), blend: 0.02f));
        // The twin: the tail's head taken away, a dark figure rising from its stalk
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail);
        b.Model.Decals.RemoveAll(d => d.Bone == tail);
        var path = Smooth(3, V(0, 0.1f, -0.08f), V(0.08f, 0.08f, -0.22f), V(0.16f, 0.2f, -0.3f), V(0.18f, 0.4f, -0.3f));
        b.Tube(tail, path, 0.035f, 0.07f, shadow, blend: 0.01f);
        var tc = V(0.18f, 0.52f, -0.28f);
        b.Ell(tail, tc, V(0.1f, 0.16f, 0.08f), shadow, blend: 0.04f);
        var th = V(0.17f, 0.76f, -0.25f);
        var tr = V(0.085f, 0.09f, 0.075f);
        b.Ell(tail, th, tr, shadow, blend: 0.03f);
        // Wisps of shadow trailing up from its head and down its sides
        foreach (var (from, to) in new[] { (th + V(0.04f, 0.06f, -0.02f), th + V(0.12f, 0.18f, -0.06f)), (th + V(-0.03f, 0.07f, -0.02f), th + V(-0.02f, 0.16f, -0.05f)),
                     (tc + V(0.08f, 0.0f, -0.02f), tc + V(0.18f, -0.08f, -0.06f)), (tc + V(0.06f, -0.1f, -0.02f), tc + V(0.16f, -0.22f, -0.06f)), (tc + V(-0.06f, -0.08f, -0.02f), tc + V(-0.04f, -0.26f, -0.08f)) })
            b.Spike(tail, from, to, 0.035f, edge, 0.4f, blend: 0.02f);
        // Thin arms reaching forward over its twin's head, ending in claws
        foreach (float s in new[] { -1f, 1f })
        {
            var shoulder = tc + V(0.08f * s, 0.08f, 0.02f);
            var hand = tc + V(0.16f * s, 0.06f, 0.18f);
            b.Limb(tail, shoulder, hand, 0.025f, 0.016f, shadow, blend: 0.01f);
            foreach (float k in new[] { -1f, 0f, 1f })
                b.Spike(tail, hand, hand + V(0.02f * k, -0.04f, 0.03f), 0.008f, shadow, blend: 0.003f);
        }
        // Two slanted eyes that glow
        PokeBuilder.Both(s =>
        {
            var look = V(0.45f * s, 0.05f, 1f);
            var at = Out(th, tr, default, look);
            b.Ell(tail, at - Vector3.Normalize(look) * 0.004f, V(0.02f, 0.01f, 0.006f), eye, V(0, 25f * s, -20f * s), Glow, 0.003f);
        });
        Stretch(b, V(1.08f, 1.08f, 1.08f));
        return b;
    }

    // ------------------------------------------------------------------ 2205 Pinecitadel, from Forretress

    /// <summary>
    /// Pinecitadel: Forretress come out of its shell onto six armoured legs, the shell grown into a fortress of dark
    /// plates rimmed in copper, four towers with battlements standing on its back, the gaps between the plates
    /// showing a core that glows orange, steel spikes at the plates' corners, and glowing eyes under a helm of plate.
    /// </summary>
    private static PokeBuilder Pinecitadel()
    {
        var b = new PokeBuilder("Pinecitadel", 0.98f, BodyPlan.Quadruped, V(0, 0.34f, 0)) { Coat = Shell };
        var plate = Rgb(104, 112, 122);
        var under = Rgb(78, 62, 58);
        var copper = Rgb(170, 100, 68);
        var core = Rgb(250, 146, 50);
        var stone = Rgb(92, 92, 98);
        var spike = Rgb(168, 174, 186);
        // Six legs, armoured, three a side
        for (int i = 0; i < 3; i++)
            PokeBuilder.Both(s =>
            {
                float z = 0.16f - i * 0.17f;
                var hip = V(0.18f * s, 0.24f, z);
                int leg = i == 0 ? b.Leg(s, hip, true) : i == 2 ? b.Leg(s, hip, false) : b.Part(s < 0 ? "midL" : "midR", Root, hip, PokeRole.Leg, 0.8f, s);
                var knee = V(0.31f * s, 0.2f, z * 1.15f);
                var foot = V(0.36f * s, 0.02f, z * 1.25f + 0.02f);
                b.Limb(leg, hip, knee, 0.045f, 0.04f, under);
                b.Limb(leg, knee, foot, 0.04f, 0.032f, under);
                b.Ell(leg, Vector3.Lerp(knee, foot, 0.4f), V(0.05f, 0.07f, 0.05f), plate, Euler(foot - knee), Shell);
                b.PaintEll(leg, Vector3.Lerp(knee, foot, 0.12f), V(0.06f, 0.012f, 0.06f), copper, Euler(foot - knee), soft: 0.005f);
                b.Spike(leg, knee + V(0.02f * s, 0.02f, 0), knee + V(0.07f * s, 0.07f, 0), 0.016f, spike, mat: Metal);
                Claws(b, leg, foot + V(0, -0.005f, 0.02f), V(0.014f, 0, 0), V(0, -0.3f, 1f), 0.024f, 0.008f);
            });
        // The fortress: a glowing core under a dome of plates with gaps between them
        var c = V(0, 0.36f, -0.02f);
        var r = V(0.26f, 0.2f, 0.29f);
        b.Ell(Body, c, r, core, mat: Glow);
        b.Ell(Body, c + V(0, -0.06f, 0), V(0.25f, 0.15f, 0.27f), under, blend: 0.02f);
        foreach (var (pitch, count, w) in new[] { (4f, 9, 0.095f), (28f, 9, 0.09f), (52f, 7, 0.085f), (76f, 3, 0.07f) })
            for (int i = 0; i < count; i++)
            {
                float yaw = (i + (pitch > 20f && pitch < 60f ? 0.5f : 0f)) * MathF.Tau / count;
                float p = pitch * Degree;
                var dir = V(MathF.Sin(yaw) * MathF.Cos(p), MathF.Sin(p), MathF.Cos(yaw) * MathF.Cos(p));
                var at = Out(c, r, default, dir);
                var n = Outward(c, r, at);
                b.Box(Body, at, V(w, 0.016f, w * 0.85f), 0.008f, plate, Euler(n), Shell, 0.004f);
                b.PaintEll(Body, at, V(w * 1.08f, 0.03f, w * 0.95f), copper, Euler(n), soft: 0.004f);
                b.PaintEll(Body, at + n * 0.01f, V(w * 0.9f, 0.03f, w * 0.78f), plate, Euler(n), soft: 0.004f);
                if (pitch > 20f && pitch < 60f && i % 2 == 0)
                    b.Spike(Body, at + n * 0.01f, at + n * 0.08f, 0.018f, spike, mat: Metal);
            }
        // Four towers on its back, each crowned with battlements
        foreach (var (x, z, hgt) in new[] { (-0.1f, 0.08f, 0.12f), (0.1f, -0.02f, 0.15f), (-0.06f, -0.15f, 0.11f), (0.09f, -0.2f, 0.09f) })
        {
            var foot = Out(c, r, default, V(x * 3f, 1f, z * 3f));
            var top = foot + V(0, hgt, 0);
            b.Box(Body, (foot + top) / 2f - V(0, 0.02f, 0), V(0.034f, hgt / 2f + 0.02f, 0.034f), 0.004f, stone, mat: Shell, blend: 0.004f);
            b.Box(Body, top, V(0.042f, 0.012f, 0.042f), 0.003f, stone, mat: Shell, blend: 0.003f);
            foreach (var (dx, dz) in new[] { (-1f, -1f), (-1f, 1f), (1f, -1f), (1f, 1f) })
                b.Box(Body, top + V(0.03f * dx, 0.022f, 0.03f * dz), V(0.011f, 0.012f, 0.011f), 0.002f, stone, mat: Shell, blend: 0.002f);
            b.PaintEll(Body, (foot + top) / 2f + V(0, 0.01f, 0.0345f), V(0.008f, 0.016f, 0.01f), Rgb(40, 36, 40), soft: 0.003f);
        }
        // The head in front, under a helm of plate: glowing eyes, curved mandibles, two short feelers
        int head = b.Head(V(0, 0.26f, 0.24f));
        var hc = V(0, 0.24f, 0.31f);
        var hr = V(0.11f, 0.08f, 0.07f);
        b.Ell(head, hc, hr, under);
        b.Ell(head, hc + V(0, 0.05f, 0.0f), V(0.12f, 0.05f, 0.08f), plate, mat: Shell, blend: 0.01f);
        b.PaintEll(head, hc + V(0, 0.03f, 0.05f), V(0.13f, 0.008f, 0.06f), copper, soft: 0.004f);
        PokeBuilder.Both(s =>
        {
            b.Tube(head, Smooth(3, hc + V(0.03f * s, -0.05f, 0.05f), hc + V(0.06f * s, -0.08f, 0.09f), hc + V(0.03f * s, -0.11f, 0.1f)), 0.014f, 0.008f, Rgb(120, 92, 74), blend: 0.004f);
            b.Tube(head, Smooth(3, hc + V(0.05f * s, 0.07f, 0.04f), hc + V(0.12f * s, 0.1f, 0.12f), hc + V(0.18f * s, 0.08f, 0.16f)), 0.009f, 0.008f, Rgb(120, 92, 74), blend: 0f);
            var look = V(0.5f * s, -0.1f, 1f);
            b.Eye(head, Out(hc, hr, default, look), look, 0.022f, core, glare: true);
        });
        return b;
    }
    // ------------------------------------------------------------------ 2208 Ironwyrm, from Steelix

    /// <summary>A cluster of clear crystals standing out of <paramref name="bone"/> at <paramref name="at"/> along <paramref name="dir"/>.</summary>
    private static void CrystalCluster(PokeBuilder b, int bone, Vector3 at, Vector3 dir, float length, float r, Color color, Color glow)
    {
        dir = Vector3.Normalize(dir);
        var side = Vector3.Normalize(Vector3.Cross(dir, MathF.Abs(dir.Y) > 0.9f ? V(1f, 0, 0) : V(0, 1f, 0)));
        b.Spike(bone, at - dir * r, at + dir * length, r, color, 0.7f, Shell, 0.004f);
        b.Spike(bone, at - dir * r + side * r * 0.9f, at + (dir * 0.8f + side * 0.45f) * length * 0.7f, r * 0.8f, glow, 0.7f, Glow, 0.004f);
        b.Spike(bone, at - dir * r - side * r * 0.9f, at + (dir * 0.8f - side * 0.45f) * length * 0.6f, r * 0.75f, color, 0.7f, Shell, 0.004f);
    }

    /// <summary>
    /// Ironwyrm: Steelix grown longer, its steel gone to dark grey-brown stone cut into facets, clear crystals grown
    /// out at every joint between its boulders (some glowing faintly white), its jaw broader and set with pale teeth
    /// (the female's square front teeth kept), and a crest of crystal standing up from its head.
    /// </summary>
    /// <param name="female">A female keeps Steelix's: two square teeth at the front of her jaw.</param>
    private static PokeBuilder Ironwyrm(bool female = false)
    {
        var b = Become(Steelix(female), "Ironwyrm", 1f);
        var stone = Rgb(112, 106, 98);
        var dark = Rgb(66, 62, 60);
        var crystal = Rgb(222, 232, 244);
        var glow = Rgb(236, 244, 255);
        var tooth = Rgb(232, 222, 196);
        foreach (var s in b.Sdf.Shapes.Where(s => s.Color.Equals(Rgb(150, 168, 196)))) s.Material = Shell;
        Recolour(b, (Rgb(150, 168, 196), stone), (Rgb(70, 76, 96), dark), (Rgb(214, 40, 44), Rgb(210, 236, 250)));
        // Darker stones every other boulder, and the blades along its body turned to crystal
        var path = ArchPath;
        for (int i = 1; i < path.Length; i += 2)
            b.PaintEll(BoneOf(b, "seg" + i), path[i], V(0.2f, 0.2f, 0.2f), Rgb(96, 92, 86));
        foreach (var s in b.Sdf.Shapes.Where(s => s.Shape == SdfShape.RoundCone && s.Bone != BoneOf(b, "head") && s.Color.Equals(stone)))
        {
            s.Color = crystal;
            s.Material = Shell;
        }
        // Crystals at every joint between boulders
        for (int i = 1; i < path.Length; i++)
        {
            int seg = BoneOf(b, "seg" + i);
            var at = (path[i - 1] + path[i]) / 2f;
            var along = Vector3.Normalize(path[i] - path[i - 1]);
            float r = 0.06f + 0.085f * MathF.Min(1f, i / 6f);
            var outward = Vector3.Normalize(Vector3.Cross(along, V(0, 0, 1f)) + V(0, 0.3f, 0));
            foreach (float s in new[] { 1f, -1f })
            {
                var dir = Vector3.Normalize(outward * s + V(0, 0, -0.3f * s));
                CrystalCluster(b, seg, at + dir * r * 0.7f, dir, r * 0.75f, r * 0.2f, crystal, glow);
            }
        }
        // A broader jaw set with teeth
        int head = BoneOf(b, "head");
        int jaw = BoneOf(b, "jaw");
        Grow(b, V(1.2f, 1.1f, 1.05f), V(0, 0.79f, 0.2f), bone => bone == jaw);
        for (int i = 0; i < 6; i++)
        {
            float x = -0.12f + 0.048f * i;
            b.Spike(jaw, V(x, 0.84f, 0.32f + (0.12f - MathF.Abs(x)) * 0.3f), V(x, 0.9f, 0.33f + (0.12f - MathF.Abs(x)) * 0.3f), 0.016f, tooth, mat: Shell, blend: 0.004f);
            b.Spike(head, V(x, 0.86f, 0.32f + (0.12f - MathF.Abs(x)) * 0.3f), V(x, 0.81f, 0.33f + (0.12f - MathF.Abs(x)) * 0.3f), 0.014f, tooth, mat: Shell, blend: 0.004f);
        }
        // A crest of crystal on its head
        foreach (var (x, z, h) in new[] { (0f, 0.02f, 0.2f), (0.06f, 0.06f, 0.15f), (-0.06f, 0.06f, 0.15f), (0.09f, -0.02f, 0.13f), (-0.09f, -0.02f, 0.13f), (0.03f, 0.12f, 0.1f), (-0.03f, 0.12f, 0.1f) })
            CrystalCluster(b, head, V(x, 0.99f, z), V(x * 1.5f, 1f, -0.5f), h, 0.028f, crystal, glow);
        Stretch(b, V(1.05f, 1.08f, 1.05f));
        return b;
    }

    // ------------------------------------------------------------------ 2210 Mastiffang, from Granbull

    /// <summary>
    /// Mastiffang: Granbull grown heavy in the chest, a duskier purple with a pale belly, a short cape of shaggy pink fur
    /// over its shoulders, a studded black collar, fuller jowls and its fangs grown long.
    /// </summary>
    private static PokeBuilder Mastiffang()
    {
        var b = Become(Granbull(), "Mastiffang", 0.94f);
        var mauve = Rgb(164, 120, 162);
        var pale = Rgb(222, 196, 214);
        var fur = Rgb(222, 164, 222);
        var furDark = Rgb(196, 136, 200);
        var collar = Rgb(60, 58, 74);
        Recolour(b, (Rgb(204, 164, 214), mauve));
        Grow(b, V(1.2f, 1.08f, 1.15f), V(0, 0.36f, 0), s => s.Bone == Body, bone => bone == Body);
        b.PaintEll(Body, V(0, 0.32f, 0.11f), V(0.12f, 0.14f, 0.08f), pale);
        int head = BoneOf(b, "head");
        var c = V(0, 0.62f, 0.06f);
        // Fuller jowls, longer fangs
        PokeBuilder.Both(s => b.Ell(head, c + V(0.07f * s, -0.07f, 0.07f), V(0.05f, 0.05f, 0.05f), mauve, blend: 0.02f));
        foreach (var f in b.Sdf.Shapes.Where(s => s.Bone == head && s.Shape == SdfShape.RoundCone && s.Color.Equals(White)))
            f.B = f.A + (f.B - f.A) * 1.5f;
        // A studded collar at its neck
        var neck = V(0, 0.53f, 0.03f);
        b.Torus(Body, neck, 0.12f, 0.022f, collar, V(-8f, 0, 0), mat: Metal, blend: 0.004f);
        for (int i = 0; i < 10; i++)
        {
            float a = (i + 0.5f) * MathF.Tau / 10f;
            var o = V(MathF.Sin(a), 0, MathF.Cos(a));
            if (o.Z < -0.5f) continue;
            b.Spike(Body, neck + o * 0.13f, neck + o * 0.17f, 0.012f, Steel, mat: Metal, blend: 0.003f);
        }
        // A short cape of shaggy fur over its shoulders and back
        b.Ell(Body, V(0, 0.52f, -0.06f), V(0.2f, 0.07f, 0.13f), fur, blend: 0.03f);
        for (int i = 0; i < 13; i++)
        {
            float a = (60f + i * 20f) * Degree;
            var o = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = V(0, 0.51f, -0.05f) + o * V(0.19f, 0, 0.12f);
            b.Spike(Body, root, root + o * 0.05f + V(0, -0.1f, 0), 0.035f, i % 2 == 0 ? fur : furDark, 0.5f, Fur, 0.012f);
        }
        Stretch(b, V(1.04f, 1.04f, 1.04f));
        return b;
    }

    // ------------------------------------------------------------------ 2211 Fugumace, from Qwilfish

    /// <summary>
    /// Fugumace: Qwilfish larger and always half puffed, deep teal over a pale belly, its spikes longer and tipped in
    /// purple, a band of heavy purple flanges round its middle like a mace's, its round tail fin wider, and a glare.
    /// </summary>
    private static PokeBuilder Fugumace()
    {
        var b = Become(Qwilfish(), "Fugumace", 0.86f);
        var teal = Rgb(50, 116, 128);
        var belly = Rgb(198, 232, 226);
        var purple = Rgb(128, 64, 164);
        var face = Rgb(112, 176, 160);
        Recolour(b, (Rgb(62, 112, 112), teal), (Rgb(204, 222, 150), belly), (Rgb(170, 200, 130), purple));
        var lift = Shifted(b, "tail", V(0, 0.25f, -0.14f));
        var c = V(0, 0.25f, 0) + lift;
        var r = V(0.17f, 0.16f, 0.16f);
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == Body && s.Shape == SdfShape.RoundCone && s.Op == SdfOp.Add).ToList())
        {
            s.B = s.A + (s.B - s.A) * 1.6f;
            b.PaintEll(Body, s.B, V(0.03f, 0.03f, 0.03f), purple, soft: 0.006f);
        }
        b.PaintEll(Body, c + V(0, 0.02f, 0.12f), V(0.12f, 0.1f, 0.06f), face);
        foreach (var d in b.Model.Decals.Where(d => d.IsEye)) d.Glare = true;
        // The band of flanges round its middle
        var band = c + V(0, 0, 0.02f);
        b.Torus(Body, band, 0.168f, 0.016f, purple, V(90f, 0, 0), mat: Shell, blend: 0.004f);
        for (int i = 0; i < 14; i++)
        {
            float a = i * MathF.Tau / 14f;
            var o = V(MathF.Cos(a), MathF.Sin(a), 0);
            var at = band + o * 0.17f;
            b.Box(Body, at + o * 0.02f, V(0.016f, 0.03f, 0.022f), 0.004f, purple, V(0, 0, a / Degree - 90f), Shell, 0.004f);
        }
        // A wider tail fin
        int tail = BoneOf(b, "tail");
        var fin = c + V(0, 0.02f, -0.29f);
        Grow(b, V(1f, 1.5f, 1.35f), fin, bone => bone == tail);
        b.PaintEll(tail, fin + V(0, 0, -0.08f), V(0.03f, 0.12f, 0.04f), purple);
        Stretch(b, V(1.18f, 1.18f, 1.18f));
        return b;
    }

    // ------------------------------------------------------------------ 2212 Guillotyne, from Scizor

    /// <summary>
    /// Guillotyne: Scizor taller and leaner, its round pincers grown into long open jaws of red, each lined along its
    /// inside with a serrated blade of steel, its wings grown long and clear, and its crest of three points longer.
    /// The female's fuller abdomen is kept.
    /// </summary>
    /// <param name="female">A female keeps Scizor's: a fuller abdomen.</param>
    private static PokeBuilder Guillotyne(bool female = false)
    {
        var b = Become(Scizor(female), "Guillotyne", 0.96f);
        var red = Rgb(178, 40, 48);
        var grey = Rgb(80, 82, 94);
        var blade = Rgb(206, 214, 226);
        Recolour(b, (Rgb(214, 40, 48), red), (Rgb(72, 72, 82), grey));
        // The pincers: the round claws taken away, long jaws with steel blades in their place
        foreach (var arm in new[] { BoneOf(b, "armL"), BoneOf(b, "armR") })
        {
            float s = arm == BoneOf(b, "armL") ? -1f : 1f;
            var claw = V(0.26f * s, 0.67f, 0.15f);
            b.Sdf.RemoveAll(p => p.Bone == arm && (p.A - claw).Length() < 0.1f);
            b.Model.Decals.RemoveAll(d => d.Bone == arm);
            var wrist = V(0.23f * s, 0.6f, 0.12f);
            b.Ell(arm, wrist, V(0.04f, 0.04f, 0.04f), grey, mat: Metal, blend: 0.008f);
            var down = Vector3.Normalize(V(0.15f * s, -1f, 0.55f));
            var side = Vector3.Normalize(Vector3.Cross(down, V(1f, 0, 0)));
            foreach (float k in new[] { 1f, -1f })
            {
                var root = wrist + side * 0.025f * k;
                var tip = wrist + down * 0.3f + side * 0.035f * k;
                var mid = Vector3.Lerp(root, tip, 0.5f) + side * 0.03f * k;
                b.Tube(arm, Smooth(3, root, mid, tip), 0.03f, 0.008f, red, Metal, 0.006f);
                var edge = Smooth(3, root + down * 0.04f - side * 0.01f * k, mid - side * 0.02f * k, tip - side * 0.012f * k);
                b.Tube(arm, edge, 0.012f, 0.006f, blade, Metal, 0.004f);
                for (int i = 1; i < edge.Length - 1; i += 2)
                    b.Spike(arm, edge[i], edge[i] - side * 0.022f * k, 0.008f, blade, mat: Metal, blend: 0.003f);
            }
        }
        // Longer wings, longer crest
        foreach (var w in new[] { BoneOf(b, "wingL"), BoneOf(b, "wingR") })
        {
            float s = w == BoneOf(b, "wingL") ? -1f : 1f;
            Grow(b, V(1.6f, 1.9f, 1f), V(0.04f * s, 0.54f, -0.05f), bone => bone == w);
        }
        int head = BoneOf(b, "head");
        foreach (var sp in b.Sdf.Shapes.Where(p => p.Bone == head && p.Shape == SdfShape.RoundCone))
            sp.B = sp.A + (sp.B - sp.A) * 1.6f;
        Stretch(b, V(0.95f, 1.12f, 0.95f));
        return b;
    }
    // ------------------------------------------------------------------ 2213 Fermentoise, from Shuckle

    /// <summary>
    /// Fermentoise: Shuckle grown into a tortoise, its yellow body longer on a long neck, its shell grown into a
    /// cluster of shells like a hive in terracotta, and berries stored and fermenting in their openings, glowing red
    /// and purple.
    /// </summary>
    private static PokeBuilder Fermentoise()
    {
        var b = Become(Shuckle(), "Fermentoise", 0.9f);
        var clay = Rgb(176, 98, 70);
        var clayDark = Rgb(146, 76, 56);
        var yellow = Rgb(242, 204, 82);
        var inside = Rgb(70, 30, 34);
        var berries = new[] { Rgb(214, 40, 60), Rgb(150, 60, 170), Rgb(196, 54, 110) };
        // Its shell's holes filled over, the shell clay-red
        b.Sdf.RemoveAll(s => s.Bone == Body && s.Op != SdfOp.Add);
        Recolour(b, (Rgb(198, 60, 56), clay));
        var c = V(0, 0.17f, -0.02f);
        var r = V(0.16f, 0.13f, 0.17f);
        Grow(b, V(1.2f, 1.25f, 1.2f), c, s => s.Bone == Body, bone => bone == Body);
        r *= V(1.2f, 1.25f, 1.2f);
        // A belly plate of yellow under the shell's rim
        b.Ell(Body, c + V(0, -0.07f, 0.02f), V(0.17f, 0.05f, 0.18f), yellow, blend: 0.02f);
        // The hive: knobs of shell, each opened into a cell with berries in it
        int cell = 0;
        foreach (var d in new[] { V(0, 1f, 0.1f), V(0.6f, 0.75f, 0.35f), V(-0.6f, 0.75f, 0.35f), V(0.65f, 0.65f, -0.45f), V(-0.65f, 0.65f, -0.45f), V(0, 0.7f, -0.75f), V(0.9f, 0.1f, 0f), V(-0.9f, 0.1f, 0f) })
        {
            var dir = Vector3.Normalize(d);
            var at = Out(c, r, default, dir);
            var knob = at + dir * 0.015f;
            b.Ell(Body, knob, V(0.065f, 0.065f, 0.065f), cell % 2 == 0 ? clay : clayDark, Euler(dir), Shell, 0.02f);
            b.Cut(Body, knob + dir * 0.05f, V(0.042f, 0.06f, 0.042f), Euler(dir), 0.004f);
            b.PaintEll(Body, knob + dir * 0.01f, V(0.05f, 0.05f, 0.05f), inside, Euler(dir), soft: 0.006f);
            foreach (var (u, w) in new[] { (0.015f, 0.01f), (-0.016f, 0.004f), (0f, -0.016f) })
            {
                var side = Vector3.Normalize(Vector3.Cross(dir, V(0, 0, 1f) + V(0.1f, 0, 0)));
                var up = Vector3.Cross(side, dir);
                b.Ell(Body, knob + dir * 0.0f + side * u + up * w, V(0.016f, 0.016f, 0.016f), berries[(cell + (int)(u * 100)) % 3 < 0 ? 0 : (cell + (int)(u * 100) + 3) % 3], mat: Glow, blend: 0.002f);
            }
            cell++;
        }
        // A longer neck, the head carried higher and further forward
        int head = BoneOf(b, "head");
        Move(b, bone => bone == head, V(0, 0.08f, 0.1f));
        var hc = V(0, 0.48f, 0.29f);
        b.Ell(head, hc + V(0, 0.0f, -0.025f), V(0.06f, 0.05f, 0.05f), Rgb(242, 208, 82), blend: 0.02f);
        b.Tube(head, Smooth(3, V(0, 0.2f, 0.14f), V(0, 0.32f, 0.22f), V(0, 0.45f, 0.27f)), 0.04f, 0.034f, yellow, blend: 0.01f);
        PokeBuilder.Both(s => b.Spike(head, V(0.025f * s, 0.52f, 0.26f), V(0.04f * s, 0.58f, 0.25f), 0.009f, yellow, blend: 0.004f));
        // A short tail
        b.Spike(Body, c + V(0, -0.04f, -0.18f), c + V(0, -0.08f, -0.3f), 0.035f, yellow, 0.6f);
        // Eyes sized for the larger model's mesh
        foreach (var d in b.Model.Decals.Where(d => d.IsEye)) { d.Size *= 1.6f; d.Half *= 1.6f; }
        Stretch(b, V(1.05f, 1.05f, 1.1f));
        return b;
    }

    // ------------------------------------------------------------------ 2214 Atlascross, from Heracross

    /// <summary>
    /// Atlascross: Heracross larger, its blue darkened to the metallic navy of an atlas beetle, its great forked horn
    /// joined by a curved horn rising from each shoulder (the atlas beetle's three), dark claws, and amber eyes. The
    /// female's heart-tipped horn is kept.
    /// </summary>
    /// <param name="female">A female keeps Heracross's: her horn ends in a heart.</param>
    private static PokeBuilder Atlascross(bool female = false)
    {
        var b = Become(Heracross(female), "Atlascross", 0.95f);
        var navy = Rgb(42, 60, 100);
        var light = Rgb(62, 88, 136);
        var horn = Rgb(40, 46, 62);
        Recolour(b, (Rgb(82, 132, 210), navy), (Rgb(120, 168, 230), light), (Rgb(250, 214, 60), Rgb(230, 162, 48)), (Claw, horn));
        // A curved horn rising from each shoulder plate
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var root = V(0.21f * s, 0.68f, 0.0f);
            var path = Smooth(3, root, root + V(0.08f * s, 0.12f, 0.02f), root + V(0.1f * s, 0.26f, 0.08f), root + V(0.06f * s, 0.36f, 0.14f));
            b.Tube(arm, path, 0.045f, 0.012f, horn, Shell, 0.012f);
            b.Ell(arm, V(0.21f * s, 0.67f, 0.02f), V(0.09f, 0.07f, 0.08f), navy, mat: Shell, blend: 0.02f);
            b.PaintTorus(arm, V(0.21f * s, 0.62f, 0.02f), 0.085f, 0.008f, horn, V(0, 0, 70f * s));
        });
        // Dark seams along the armour
        b.PaintEll(Body, V(0, 0.6f, 0.15f), V(0.12f, 0.01f, 0.04f), horn, soft: 0.005f);
        Stretch(b, V(1.06f, 1.06f, 1.06f));
        return b;
    }

    // ------------------------------------------------------------------ 2219 Calderon, from Magcargo

    /// <summary>
    /// Calderon: Magcargo's body brighter, a molten orange crusted with dark cooled rock, its shell grown into a small
    /// volcano cone of layered black stone with a crater at its top glowing orange and a wisp of smoke, seams of lava
    /// down its flanks, rocky horns and heavy brows of stone.
    /// </summary>
    private static PokeBuilder Calderon()
    {
        var b = Become(Magcargo(), "Calderon", 0.88f);
        var orange = Rgb(236, 104, 48);
        var deep = Rgb(204, 74, 40);
        var rockColor = Rgb(62, 60, 64);
        var lava = Rgb(252, 150, 50);
        var smoke = Rgb(120, 116, 116);
        Recolour(b, (Rgb(228, 92, 62), orange), (Rgb(196, 64, 50), deep));
        // The round shell gives way to a cone
        int rock = BoneOf(b, "shell");
        b.Sdf.RemoveAll(s => s.Bone == rock);
        var sc = V(0, 0.24f, -0.12f);
        b.Ell(rock, sc + V(0, -0.05f, 0), V(0.18f, 0.09f, 0.17f), rockColor, mat: Shell, blend: 0.02f);
        b.Limb(rock, sc + V(0, -0.02f, 0), sc + V(0, 0.18f, -0.02f), 0.15f, 0.055f, rockColor, Shell, 0.01f);
        foreach (var (a, y) in new[] { (0.3f, 0.02f), (2.2f, 0.05f), (4.0f, 0.0f) })
            b.Ell(rock, sc + V(MathF.Sin(a) * 0.12f, y - 0.06f, MathF.Cos(a) * 0.11f), V(0.07f, 0.04f, 0.07f), Rgb(80, 76, 80), mat: Shell, blend: 0.02f);
        var crater = sc + V(0, 0.2f, -0.02f);
        b.Cut(rock, crater + V(0, 0.03f, 0), V(0.05f, 0.05f, 0.05f), blend: 0.006f);
        b.Ell(rock, crater - V(0, 0.01f, 0), V(0.045f, 0.02f, 0.045f), lava, mat: Glow, blend: 0.004f);
        // Seams of lava running down the cone from the crater's rim
        for (int i = 0; i < 6; i++)
        {
            float a = (i * 60f + 20f) * Degree;
            var o = V(MathF.Sin(a), 0, MathF.Cos(a));
            var top = crater + o * 0.05f;
            var foot = sc + V(0, -0.02f, -0.02f) + o * 0.16f;
            b.PaintEll(rock, Vector3.Lerp(top, foot, 0.4f), V(0.007f, 0.08f, 0.03f), lava, Euler(top - foot), soft: 0.004f);
        }
        // A wisp of smoke rising from the crater
        b.Tube(rock, Smooth(3, crater, crater + V(0.02f, 0.06f, -0.03f), crater + V(-0.03f, 0.12f, -0.06f), crater + V(0.01f, 0.17f, -0.1f)), 0.016f, 0.03f, smoke, blend: 0.006f);
        // Dark crust on its flanks, rocky horns and brows
        foreach (var (x, z) in new[] { (0.12f, 0.06f), (-0.12f, 0.03f), (0.1f, -0.1f), (-0.09f, -0.12f), (0.0f, 0.13f) })
            b.PaintEll(Body, V(x, 0.06f, z), V(0.035f, 0.02f, 0.03f), rockColor, soft: 0.006f);
        int head = BoneOf(b, "head");
        var c = V(0, 0.3f, 0.05f);
        b.Sdf.RemoveAll(s => s.Bone == head && s.Material == Glow);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, c + V(0.05f * s, 0.06f, -0.02f), c + V(0.1f * s, 0.2f, -0.02f), 0.026f, rockColor, 0.6f, Shell);
            b.PaintEll(head, c + V(0.1f * s, 0.19f, -0.02f), V(0.02f, 0.03f, 0.02f), lava, soft: 0.004f);
            b.Box(head, c + V(0.05f * s, 0.075f, 0.07f), V(0.035f, 0.01f, 0.02f), 0.006f, rockColor, V(0, 0, -15f * s), Shell, 0.004f);
        });
        Stretch(b, V(1.12f, 1.1f, 1.18f));
        return b;
    }

    // ------------------------------------------------------------------ 2222 Reefcrown, from Corsola

    /// <summary>
    /// Reefcrown: Corsola grown into a broad beast of reef stone, mossy grey over sea-green, with barnacles and shells
    /// on its back and legs, its branches grown into a great crown of coral in pink, white and orange where small fish
    /// shelter, and a pearl set at the crown's middle.
    /// </summary>
    private static PokeBuilder Reefcrown()
    {
        var b = Become(Corsola(), "Reefcrown", 0.96f);
        var stone = Rgb(156, 154, 124);
        var sea = Rgb(92, 150, 140);
        var pink = Rgb(236, 120, 156);
        var white = Rgb(250, 236, 236);
        var orange = Rgb(244, 150, 96);
        var pearl = Rgb(236, 234, 250);
        Recolour(b, (Rgb(246, 140, 160), stone), (Rgb(182, 222, 240), sea));
        var c = V(0, 0.18f, 0);
        int coral = BoneOf(b, "coral");
        b.Sdf.RemoveAll(s => s.Bone == coral);
        Grow(b, V(1.3f, 1.1f, 1.3f), c, s => s.Bone != coral, _ => true);
        var r = V(0.14f, 0.13f, 0.14f) * V(1.3f, 1.1f, 1.3f);
        // Barnacles and shells on its back and legs, sea-green patches over the stone
        foreach (var d in new[] { V(0.7f, 0.3f, 0.3f), V(-0.7f, 0.2f, -0.2f), V(0.5f, 0.1f, -0.7f), V(-0.4f, 0.4f, 0.6f) })
            b.PaintEll(Body, Out(c, r, default, d), V(0.05f, 0.04f, 0.05f), sea);
        foreach (var d in new[] { V(0.8f, 0.0f, 0.4f), V(-0.85f, -0.1f, 0.2f), V(0.6f, -0.2f, -0.6f) })
        {
            var at = Out(c, r, default, d);
            b.Ell(Body, at, V(0.018f, 0.014f, 0.018f), Rgb(214, 210, 200), Euler(Vector3.Normalize(d)), Shell, 0.004f);
        }
        // The crown of coral: branches fanned over its back in pink, white and orange, small fish on their tips
        var colours = new[] { pink, white, orange, pink, orange, white, pink, orange, pink };
        for (int i = 0; i < colours.Length; i++)
        {
            float a = (-80f + 20f * i) * Degree;
            var root = c + V(MathF.Sin(a) * 0.07f, r.Y * 0.8f, -0.02f - MathF.Abs(MathF.Sin(a)) * 0.02f);
            float h = 0.26f - MathF.Abs(MathF.Sin(a)) * 0.08f;
            var tip = root + V(MathF.Sin(a) * 0.22f, h, -0.06f + (i % 3) * 0.03f);
            Coral(b, coral, root, tip, 0.024f, colours[i], V(MathF.Sin(a) * 0.06f + 0.03f * (i % 2 == 0 ? 1 : -1), 0.07f, 0.02f));
            var mid = Vector3.Lerp(root, tip, 0.8f);
            b.Limb(coral, mid, mid + V(-MathF.Sin(a) * 0.04f + 0.02f, 0.06f, 0.0f), 0.015f, 0.012f, colours[i], blend: 0.008f);
            if (i % 3 == 1) b.Ell(coral, tip + V(0, 0.028f, 0), V(0.014f, 0.01f, 0.018f), orange, blend: 0.004f);
        }
        // The pearl at the crown's middle
        b.Ell(coral, c + V(0, r.Y + 0.035f, 0.04f), V(0.05f, 0.05f, 0.05f), pearl, mat: Glow, blend: 0.012f);
        Stretch(b, V(1.05f, 1.05f, 1.05f));
        return b;
    }
    // ------------------------------------------------------------------ 2224 Krakannon, from Octillery

    /// <summary>
    /// Krakannon: Octillery grown larger and darker, a deep maroon mottled over its head, its snout grown into a long
    /// barrel ringed with dark iron bands with a glowing bore and a wisp of smoke at its rim, its eight arms thicker,
    /// with suckers glowing orange along their undersides, and eyes glowing gold. The female's smaller head is kept.
    /// </summary>
    /// <param name="female">A female keeps Octillery's: a smaller head.</param>
    private static PokeBuilder Krakannon(bool female = false)
    {
        var b = Become(Octillery(female), "Krakannon", 0.98f);
        var maroon = Rgb(166, 48, 52);
        var dark = Rgb(120, 32, 40);
        var sucker = Rgb(250, 150, 64);
        var iron = Rgb(70, 70, 78);
        var smoke = Rgb(150, 146, 146);
        b.Sdf.RemoveAll(s => s.Shape == SdfShape.Torus && s.Bone == Body);
        b.Model.Decals.RemoveAll(d => d.Bone == Body && !d.IsEye);
        Recolour(b, (Rgb(228, 56, 50), maroon), (Rgb(250, 208, 62), sucker));
        foreach (var s in b.Sdf.Shapes.Where(s => s.Color.Equals(sucker))) s.Material = Glow;
        foreach (var d in b.Model.Decals.Where(d => d.IsEye)) d.White = Rgb(252, 214, 90);
        var c = female ? V(0, 0.35f, -0.02f) : V(0, 0.36f, -0.02f);
        var r = female ? V(0.155f, 0.175f, 0.155f) : V(0.16f, 0.19f, 0.16f);
        // Mottled darker over its head, ridged at its crown
        foreach (var d in new[] { V(0.3f, 0.9f, -0.2f), V(-0.4f, 0.8f, -0.1f), V(0.0f, 0.6f, -0.8f), V(-0.6f, 0.4f, -0.6f), V(0.7f, 0.3f, -0.5f) })
            b.PaintEll(Body, Out(c, r, default, d), V(0.05f, 0.035f, 0.05f), dark);
        Lumps(b, Body, c + V(0, 0.04f, -0.03f), r * V(0.85f, 0.85f, 0.85f), 10, 0.04f, maroon, dark, 0.48f);
        // The barrel: on from its snout, ringed with iron, glowing in its bore, smoking at its rim
        var muzzle = V(0, 0.57f, 0.21f);
        var aim = Vector3.Normalize(V(0, 0.45f, 1f));
        var end = muzzle + aim * 0.2f;
        b.Limb(Body, muzzle - aim * 0.04f, end, 0.058f, 0.06f, maroon, blend: 0.01f);
        foreach (float t in new[] { 0.35f, 0.7f })
            b.Torus(Body, Vector3.Lerp(muzzle, end, t), 0.06f, 0.014f, iron, Euler(aim), mat: Metal, blend: 0.004f);
        b.Torus(Body, end, 0.058f, 0.016f, iron, Euler(aim), mat: Metal, blend: 0.004f);
        b.Cut(Body, end + aim * 0.03f, V(0.04f, 0.06f, 0.04f), Euler(aim), 0.004f);
        b.Ell(Body, end - aim * 0.03f, V(0.038f, 0.012f, 0.038f), sucker, Euler(aim), Glow, 0.004f);
        b.Tube(Body, Smooth(3, end + V(0.03f, 0.03f, -0.02f), end + V(0.05f, 0.1f, -0.02f), end + V(0.02f, 0.18f, -0.04f), end + V(0.05f, 0.24f, -0.06f)), 0.012f, 0.022f, smoke, blend: 0.006f);
        // Thicker arms, more suckers down their undersides
        for (int i = 0; i < 8; i++)
        {
            int arm = BoneOf(b, "tentacle" + i);
            float a = i * MathF.Tau / 8f + MathF.PI / 8f;
            var out1 = V(MathF.Sin(a), 0, MathF.Cos(a));
            Grow(b, V(1.25f, 1.25f, 1.25f), V(0, 0.1f, -0.02f) + out1 * 0.2f, s => s.Bone == arm && s.Color.Equals(maroon), _ => false);
            foreach (float t in new[] { 0.35f, 0.5f, 0.95f })
            {
                var at = V(0, 0.12f - 0.08f * MathF.Sin(t * MathF.PI), -0.02f) + out1 * (0.12f + 0.22f * t);
                b.Ell(arm, at + V(0, -0.025f, 0) + out1 * 0.02f, V(0.017f, 0.017f, 0.017f), sucker, mat: Glow, blend: 0.006f);
            }
        }
        Stretch(b, V(1.1f, 1.1f, 1.1f));
        return b;
    }

    // ------------------------------------------------------------------ 2225 Sleighwing, from Delibird

    /// <summary>
    /// Sleighwing: Delibird grown taller, a crimson bird with broad wings for flying whose flight feathers are frosted
    /// pale blue, flying goggles pushed up on its brow, a striped blue and white scarf, and its sack grown into a
    /// satchel slung on a strap over its shoulder.
    /// </summary>
    private static PokeBuilder Sleighwing()
    {
        var b = Become(Delibird(), "Sleighwing", 0.98f);
        var crimson = Rgb(180, 50, 66);
        var frost = Rgb(200, 228, 246);
        var blue = Rgb(70, 110, 190);
        var white = Rgb(246, 244, 240);
        var leather = Rgb(140, 108, 76);
        var strap = Rgb(84, 62, 46);
        var brass = Rgb(196, 160, 84);
        var glass = Rgb(150, 210, 210);
        Recolour(b, (Rgb(222, 66, 58), crimson), (Rgb(246, 200, 72), Rgb(240, 150, 60)));
        // The sack goes; a satchel hangs at its hip on a strap over its shoulder
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail);
        b.Spike(tail, V(0, 0.2f, -0.12f), V(0, 0.12f, -0.26f), 0.05f, crimson, 0.35f);
        b.Box(Body, V(0.15f, 0.2f, 0.05f), V(0.03f, 0.08f, 0.1f), 0.025f, leather, V(0, 0, -6f), Fur, 0.01f);
        b.PaintEll(Body, V(0.18f, 0.24f, 0.05f), V(0.02f, 0.03f, 0.11f), strap, soft: 0.004f);
        b.Torus(Body, V(0.02f, 0.36f, 0.0f), 0.19f, 0.01f, strap, V(0, 0, 38f), 1f, 0.75f, mat: Fur, blend: 0.004f);
        // Broad wings: red coverts over frosted flight feathers
        foreach (var arm in new[] { BoneOf(b, "armL"), BoneOf(b, "armR") })
        {
            float s = arm == BoneOf(b, "armL") ? -1f : 1f;
            b.Sdf.RemoveAll(p => p.Bone == arm);
            var shoulder = V(0.12f * s, 0.38f, 0.0f);
            var wrist = V(0.3f * s, 0.56f, -0.03f);
            b.Limb(arm, shoulder, wrist, 0.05f, 0.03f, crimson, Fur, 0.02f);
            for (int i = 0; i < 6; i++)
            {
                float t = i / 5f;
                var root = Vector3.Lerp(shoulder + V(0.05f * s, 0.02f, 0), wrist, 0.25f + 0.75f * t);
                var dir = Vector3.Normalize(V((0.25f + 0.25f * t) * s, -1f, -0.15f));
                float len = 0.22f + 0.1f * t;
                Frond(b, arm, root, root + dir * len, 0.04f, frost, V(0, 0.1f, 1f), 0.25f, Fur);
                Frond(b, arm, root, root + dir * len * 0.5f, 0.045f, crimson, V(0, 0.1f, 1f), 0.3f, Fur);
            }
        }
        // A scarf round its neck, striped, one end hanging over its chest
        var neck = V(0, 0.41f, 0.01f);
        b.Torus(Body, neck, 0.13f, 0.03f, blue, mat: Fur, blend: 0.006f);
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f;
            b.PaintEll(Body, neck + V(MathF.Sin(a) * 0.13f, 0, MathF.Cos(a) * 0.13f), V(0.02f, 0.04f, 0.02f), white, soft: 0.004f);
        }
        var hang = neck + V(-0.08f, -0.02f, 0.1f);
        Frond(b, Body, hang, hang + V(-0.04f, -0.16f, 0.03f), 0.035f, blue, V(0, 0, 1f), 0.3f, Fur);
        b.PaintEll(Body, hang + V(-0.02f, -0.08f, 0.015f), V(0.04f, 0.015f, 0.03f), white, soft: 0.004f);
        // Flying goggles pushed up on its brow, their strap round its head
        int head = BoneOf(b, "head");
        var c = V(0, 0.48f, 0.02f);
        b.Torus(head, c + V(0, 0.05f, 0), 0.118f, 0.009f, strap, V(-10f, 0, 0), mat: Fur, blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            var lens = c + V(0.045f * s, 0.085f, 0.085f);
            b.Torus(head, lens, 0.026f, 0.008f, brass, V(70f, 0, 0), mat: Metal, blend: 0.004f);
            b.Ell(head, lens + V(0, 0.003f, 0.004f), V(0.022f, 0.01f, 0.022f), glass, V(70f, 0, 0), Metal, 0.003f);
        });
        Stretch(b, V(1f, 1.18f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2226 Raysail, from Mantine

    /// <summary>
    /// Raysail: Mantine wider, a deep violet ray scattered with pale stars over its back, long pale streamers trailing
    /// from its wing tips, its horns longer, and a long whip of a tail ending in a small fin.
    /// </summary>
    private static PokeBuilder Raysail()
    {
        var b = Become(Mantine(), "Raysail", 1f);
        var violet = Rgb(86, 62, 144);
        var under = Rgb(196, 196, 238);
        var streamer = Rgb(226, 230, 248);
        var star = Rgb(248, 240, 210);
        var blue = Rgb(170, 196, 240);
        Recolour(b, (Rgb(40, 56, 100), violet), (Rgb(216, 208, 240), under), (Rgb(196, 224, 244), violet));
        var lift = Shifted(b, "tail", V(0, 0.34f, -0.16f));
        Grow(b, V(1.15f, 1.5f, 1.15f), V(0, 0.36f, 0) + lift, bone => bone == Body);
        // Wider wings, scattered with stars, a streamer from each tip
        foreach (var fin in new[] { BoneOf(b, "finL"), BoneOf(b, "finR") })
        {
            float s = fin == BoneOf(b, "finL") ? -1f : 1f;
            var joint = V(0.12f * s, 0.36f, 0) + lift;
            Grow(b, V(1.4f, 1.8f, 1.2f), joint, bone => bone == fin);
            var tip = V(0.6f * s, 0.38f, -0.1f) + lift;
            b.Tube(fin, Smooth(3, tip + V(-0.04f * s, 0, 0.02f), tip + V(0.06f * s, 0.06f, -0.1f), tip + V(0.04f * s, 0.12f, -0.26f), tip + V(0.12f * s, 0.16f, -0.4f)), 0.014f, 0.007f, streamer, blend: 0.004f);
            foreach (var (x, z, size, colour) in new[] { (0.25f, 0.0f, 0.026f, star), (0.37f, -0.06f, 0.02f, blue), (0.46f, 0.0f, 0.018f, star) })
            {
                var at = V(x * s, 0.4f, z - 0.03f) + lift;
                b.Mark(fin, at, V(-0.12f * s, 1f, 0), size, size, colour, MarkShape.Star5);
            }
        }
        foreach (var (x, z) in new[] { (0.05f, 0.02f), (-0.06f, -0.05f), (0.0f, -0.1f) })
            b.Mark(Body, V(x, 0.48f, z) + lift, V(0, 1f, 0), 0.018f, 0.018f, star, MarkShape.Star5);
        // Longer horns, a longer tail tipped with a fin
        int head = BoneOf(b, "head");
        PokeBuilder.Both(s => b.Tube(head, Smooth(3, V(0.11f * s, 0.52f, 0.12f) + lift, V(0.13f * s, 0.55f, 0.18f) + lift, V(0.12f * s, 0.54f, 0.26f) + lift), 0.018f, 0.01f, violet, blend: 0.006f));
        int tail = BoneOf(b, "tail");
        var tailEnd = V(0.08f, 0.31f, -0.54f) + lift;
        b.Tube(tail, Smooth(3, tailEnd, tailEnd + V(0.04f, 0.04f, -0.1f), tailEnd + V(0.02f, 0.12f, -0.18f)), 0.008f, 0.006f, violet, blend: 0.003f);
        Frond(b, tail, tailEnd + V(0.02f, 0.12f, -0.18f), tailEnd + V(0.02f, 0.16f, -0.24f), 0.025f, streamer, V(1f, 0, 0), 0.3f);
        return b;
    }

    // ------------------------------------------------------------------ 2227 Rapierwing, from Skarmory

    /// <summary>
    /// Rapierwing: Skarmory grown larger, its armour plated in silver with red between the plates, its red blades of
    /// flight feathers grown long and narrow like rapiers, its crest a long blade swept back, and its tail a fan of
    /// steel blades.
    /// </summary>
    private static PokeBuilder Rapierwing()
    {
        var b = Become(Skarmory(), "Rapierwing", 1f);
        var steel = Rgb(198, 206, 222);
        var red = Rgb(186, 44, 62);
        var dark = Rgb(74, 82, 114);
        var lift = Shifted(b, "head", V(0, 0.66f, 0.07f));
        // The wings: their blades taken away, longer and narrower rapiers in their place
        foreach (var wing in new[] { BoneOf(b, "wingL"), BoneOf(b, "wingR") })
        {
            float s = wing == BoneOf(b, "wingL") ? -1f : 1f;
            b.Sdf.RemoveAll(p => p.Bone == wing && p.Color.Equals(Rgb(196, 54, 64)));
            var shoulder = V(0.06f * s, 0.62f, 0.02f) + lift;
            var wrist = V(0.22f * s, 0.78f, -0.05f) + lift;
            for (int i = 0; i < 7; i++)
            {
                float t = i / 6f;
                var root = Vector3.Lerp(shoulder + V(0.04f * s, 0.02f, 0), wrist + V(0.12f * s, 0.05f, -0.04f), 0.15f + 0.85f * t);
                var dir = Vector3.Normalize(V((0.35f + 0.4f * t) * s, -0.35f + 0.15f * t, -1f));
                float len = 0.34f + 0.14f * t;
                Blade(b, wing, root, root + dir * len, 0.026f, red, V(0, 1f, 0.2f), 0.25f, Metal);
            }
        }
        // The tail: a fan of steel blades
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(p => p.Bone == tail);
        for (int i = 0; i < 7; i++)
        {
            float x = (i - 3f) / 3f;
            var root = V(x * 0.02f, 0.47f, -0.08f) + lift;
            Blade(b, tail, root, root + Vector3.Normalize(V(x * 0.9f, -0.6f, -1f)) * (0.32f - MathF.Abs(x) * 0.06f), 0.026f, i % 2 == 0 ? steel : dark, V(0, 1f, 0), 0.25f, Metal);
        }
        // Red between the plates of its breast and on its thighs
        var bc = V(0, 0.55f, 0) + lift;
        b.PaintEll(Body, bc + V(0, 0.08f, 0.07f), V(0.03f, 0.08f, 0.04f), red, V(30f, 0, 0));
        b.PaintEll(Body, bc + V(0, -0.1f, 0.0f), V(0.09f, 0.05f, 0.08f), red);
        // A long blade of a crest swept back from its head
        int head = BoneOf(b, "head");
        var c = V(0, 0.75f, 0.15f) + lift;
        Blade(b, head, c + V(0, 0.04f, -0.02f), c + V(0, 0.2f, -0.3f), 0.032f, steel, V(1f, 0, 0), 0.25f, Metal);
        b.PaintEll(head, c + V(0, 0.035f, -0.03f), V(0.03f, 0.02f, 0.03f), red);
        Stretch(b, V(1.08f, 1.08f, 1.08f));
        return b;
    }
    // ------------------------------------------------------------------ 2229 Cerberhound, from Houndoom

    /// <summary>
    /// Cerberhound: Houndoom with two smaller heads grown from its shoulders beside its own, each with short horns of
    /// bone (the female's shorter), glowing eyes and fire at its mouth, bone plates over its shoulders, and its tail
    /// ending in a longer arrowhead.
    /// </summary>
    /// <param name="female">A female keeps Houndoom's: shorter horns, on her side heads too.</param>
    private static PokeBuilder Cerberhound(bool female = false)
    {
        var b = Become(Houndoom(female), "Cerberhound", 0.94f);
        var black = Rgb(44, 44, 52);
        var grey = Rgb(80, 78, 84);
        var bone = Rgb(232, 228, 214);
        var fire = Rgb(250, 120, 40);
        var flame = Rgb(255, 214, 90);
        Recolour(b, (Rgb(50, 50, 60), black), (Rgb(236, 128, 52), grey));
        foreach (var d in b.Model.Decals.Where(d => d.IsEye)) d.Iris = fire;
        int head = BoneOf(b, "head");
        // Fire at the main head's mouth
        FlameTongue(b, head, V(0.03f, 0.5f, 0.33f), V(0.07f, 0.53f, 0.4f), 0.018f, fire, flame);
        // Bone plates over its shoulders
        PokeBuilder.Both(s => b.Ell(Body, V(0.09f * s, 0.42f, 0.1f), V(0.05f, 0.025f, 0.07f), bone, V(0, 0, -35f * s), Shell, 0.01f));
        // The two heads at its shoulders
        PokeBuilder.Both(s =>
        {
            var hc = V(0.17f * s, 0.47f, 0.15f);
            var hr = V(0.062f, 0.058f, 0.068f);
            b.Limb(Body, V(0.07f * s, 0.37f, 0.06f), hc, 0.05f, 0.04f, black, blend: 0.02f);
            b.Ell(Body, hc, hr, black);
            var muzzle = hc + V(0.02f * s, -0.025f, 0.07f);
            b.Ell(Body, muzzle, V(0.032f, 0.028f, 0.042f), grey, blend: 0.012f);
            float horn = female ? 0.7f : 1f;
            foreach (float t in new[] { -1f, 1f })
            {
                var root = hc + V(0.03f * t, 0.045f, -0.01f);
                b.Tube(Body, Smooth(3, root, root + V(0.015f * t, 0.04f, -0.01f) * horn, root + V(0.0f, 0.07f, -0.04f) * horn), 0.014f, 0.006f, bone, Shell, 0.004f);
            }
            PokeBuilder.Both(t =>
            {
                var look = Vector3.Normalize(V(0.45f * t + 0.3f * s, 0.15f, 1f));
                b.Eye(Body, Out(hc, hr, default, look), look, 0.014f, fire, glare: true);
            });
            FlameTongue(b, Body, muzzle + V(0.01f * s, -0.01f, 0.03f), muzzle + V(0.06f * s, 0.02f, 0.07f), 0.014f, fire, flame);
        });
        // A longer arrowhead at the tail's end
        int tail = BoneOf(b, "tail");
        foreach (var p in b.Sdf.Shapes.Where(p => p.Bone == tail && p.Shape == SdfShape.Ellipsoid)) p.Size *= V(1.6f, 1.7f, 1.6f);
        Stretch(b, V(1.05f, 1.05f, 1.05f));
        return b;
    }

    // ------------------------------------------------------------------ 2232 Siegephant, from Donphan

    /// <summary>
    /// Siegephant: Donphan grown into a war elephant, its plates thick, riveted steel over its taupe hide, a howdah of
    /// steel with battlements standing on its back, a crowned helm over its brow, its trunk grown long and banded in
    /// steel and its tusks capped in steel (the female's shorter).
    /// </summary>
    /// <param name="female">A female keeps Donphan's: shorter tusks.</param>
    private static PokeBuilder Siegephant(bool female = false)
    {
        var b = Become(Donphan(female), "Siegephant", 0.98f);
        var hide = Rgb(146, 140, 126);
        var steel = Rgb(116, 124, 138);
        var dark = Rgb(66, 70, 82);
        var rust = Rgb(126, 100, 76);
        foreach (var s in b.Sdf.Shapes.Where(s => s.Color.Equals(Rgb(84, 88, 102)))) s.Material = Metal;
        Recolour(b, (Rgb(148, 162, 186), hide), (Rgb(84, 88, 102), steel), (Rgb(54, 56, 66), dark), (Rgb(164, 60, 84), hide));
        // Rust worn into the plates, rivets along their edges
        foreach (var d in new[] { V(0.6f, 0.6f, 0.3f), V(-0.5f, 0.7f, -0.4f), V(0.3f, 0.8f, -0.6f) })
            b.PaintEll(Body, Out(V(0, 0.36f, -0.02f), V(0.2f, 0.15f, 0.27f), default, d), V(0.05f, 0.04f, 0.06f), rust);
        PokeBuilder.Both(s =>
        {
            for (int i = -2; i <= 2; i++)
            {
                var at = Out(V(0, 0.36f, -0.02f), V(0.2f, 0.15f, 0.27f), default, V(s, 0.2f, i * 0.4f));
                b.PaintEll(Body, at, V(0.01f, 0.01f, 0.01f), dark, soft: 0.003f);
            }
        });
        // The howdah: a keep of steel on its back, crowned with battlements
        var keep = V(0, 0.55f, -0.06f);
        b.Box(Body, keep, V(0.11f, 0.07f, 0.12f), 0.01f, steel, mat: Metal, blend: 0.01f);
        b.Box(Body, keep + V(0, 0.075f, 0), V(0.125f, 0.012f, 0.135f), 0.004f, dark, mat: Metal, blend: 0.004f);
        foreach (var (x, z) in new[] { (-1f, -1f), (-1f, 0f), (-1f, 1f), (1f, -1f), (1f, 0f), (1f, 1f), (0f, -1f), (0f, 1f) })
            b.Box(Body, keep + V(0.1f * x, 0.1f, 0.11f * z), V(0.022f, 0.016f, 0.022f), 0.003f, steel, mat: Metal, blend: 0.003f);
        b.Mark(Body, keep + V(0, 0.01f, 0.121f), V(0, 0, 1f), 0.012f, 0.03f, Rgb(40, 40, 46));
        // A crowned helm over its brow
        int head = BoneOf(b, "head");
        var c = V(0, 0.27f, 0.25f);
        b.Ell(head, c + V(0, 0.05f, 0.03f), V(0.1f, 0.05f, 0.08f), steel, mat: Metal, blend: 0.01f);
        foreach (float x in new[] { -0.06f, -0.02f, 0.02f, 0.06f })
            b.Box(head, c + V(x, 0.11f, 0.03f), V(0.013f, 0.025f, 0.012f), 0.003f, steel, mat: Metal, blend: 0.004f);
        // A longer trunk banded in steel, tusks capped in steel
        var trunk = Smooth(3, V(0, 0.12f, 0.38f), V(0, 0.06f, 0.42f), V(0, 0.04f, 0.5f));
        b.Tube(head, trunk, 0.032f, 0.03f, hide, blend: 0.01f);
        foreach (float y in new[] { 0.23f, 0.18f, 0.13f })
            b.PaintTorus(head, V(0, y, 0.37f), 0.04f, 0.009f, steel, V(80f, 0, 0));
        PokeBuilder.Both(s =>
        {
            var tip = female ? V(0.1f * s, 0.225f, 0.44f) : V(0.13f * s, 0.24f, 0.5f);
            PaintAs(b, head, tip, V(0.03f, 0.03f, 0.03f), Steel, Metal);
        });
        Stretch(b, V(1.06f, 1.06f, 1.08f));
        return b;
    }
    // ------------------------------------------------------------------ 2235 Muralist, from Smeargle

    /// <summary>
    /// Muralist: Smeargle grown taller, its cap of fur grown into a maroon beret, a painter's smock of shaggy pale fur
    /// flecked with paint of every colour, dark stockings of fur on its shins, and its tail grown into a long brush
    /// bound below its bristles, which run from dark to violet and blue.
    /// </summary>
    private static PokeBuilder Muralist()
    {
        var b = Become(Smeargle(), "Muralist", 0.92f);
        var beret = Rgb(132, 46, 56);
        var smock = Rgb(232, 230, 222);
        var stocking = Rgb(108, 96, 86);
        var ferrule = Rgb(150, 140, 120);
        var violet = Rgb(126, 80, 206);
        var blue = Rgb(70, 130, 220);
        Recolour(b, (Rgb(230, 222, 198), beret), (Rgb(108, 178, 82), violet));
        int head = BoneOf(b, "head");
        // The beret: broader and flatter, leaning to one side
        Grow(b, V(1.3f, 1f, 1.25f), V(0, 0.64f, 0), s => s.Bone == head && s.Color.Equals(beret), _ => false);
        // The smock over its body, ragged at its hem and flecked with paint
        var bc = V(0, 0.35f, 0);
        var br = V(0.085f, 0.125f, 0.075f);
        b.Ell(Body, bc + V(0, -0.02f, 0), br * V(1.25f, 1.05f, 1.3f), smock, mat: Fur, blend: 0.02f);
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.Tau / 12f;
            var o = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = bc + V(0, -0.1f, 0) + o * V(0.1f, 0, 0.09f);
            b.Spike(Body, root, root + o * 0.025f + V(0, -0.06f, 0), 0.024f, smock, 0.5f, Fur, 0.01f);
        }
        var flecks = new[] { Rgb(220, 50, 50), Rgb(240, 200, 40), Rgb(60, 110, 210), Rgb(70, 170, 80) };
        for (int i = 0; i < 14; i++)
        {
            float a = i * 2.4f;
            float y = 0.27f + (i * 0.037f) % 0.16f;
            var n = V(MathF.Sin(a), 0, MathF.Cos(a) * 0.9f + 0.3f);
            var at = Out(bc + V(0, -0.02f, 0), br * V(1.25f, 1.05f, 1.3f), default, V(n.X, (y - bc.Y) / br.Y, n.Z));
            b.PaintEll(Body, at, V(0.011f, 0.011f, 0.011f), flecks[i % 4], soft: 0.004f);
        }
        // Dark stockings of fur on its shins
        PokeBuilder.Both(s => b.PaintEll(BoneOf(b, s < 0 ? "legL" : "legR"), V(0.062f * s, 0.12f, 0.005f), V(0.05f, 0.07f, 0.05f), stocking));
        // The brush: bound at its neck, bristles from dark to violet and blue
        int tail = BoneOf(b, "tail");
        var top = V(0.18f, 0.6f, 0.08f);
        var dir = Vector3.Normalize(top - V(0.22f, 0.5f, 0.04f));
        foreach (float t in new[] { -0.02f, 0.0f })
            b.Torus(tail, top + dir * t, 0.02f, 0.006f, ferrule, Euler(dir), mat: Metal, blend: 0.003f);
        b.Spike(tail, top + dir * 0.01f, top + dir * 0.16f, 0.04f, stocking, 0.8f, Fur, 0.008f);
        b.PaintEll(tail, top + dir * 0.12f, V(0.045f, 0.045f, 0.045f), violet);
        b.PaintEll(tail, top + dir * 0.15f, V(0.03f, 0.03f, 0.03f), blue);
        Stretch(b, V(1f, 1.15f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2237 Gyrodrill, from Hitmontop

    /// <summary>
    /// Gyrodrill: Hitmontop come right side up, a heavy-shouldered fighter with its horn grown into a drill with a
    /// spiral groove over its brow, a broad ring of armour round its waist like a spinning top's, bands round its
    /// wrists and ankles, claws, and a long tail banded dark and light ending in a point of stone.
    /// </summary>
    private static PokeBuilder Gyrodrill()
    {
        var b = new PokeBuilder("Gyrodrill", 0.96f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Fur };
        var skin = Rgb(196, 148, 82);
        var belly = Rgb(222, 196, 130);
        var drill = Rgb(148, 104, 70);
        var groove = Rgb(96, 64, 44);
        var ring = Rgb(184, 110, 82);
        var band = Rgb(130, 90, 64);
        var dark = Rgb(88, 80, 60);
        var light = Rgb(176, 156, 106);
        // Legs bent and wide, bands at the ankles, claws on the feet
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.3f, 0));
            b.Limb(leg, V(0.08f * s, 0.31f, 0), V(0.14f * s, 0.17f, 0.04f), 0.055f, 0.045f, skin);
            b.Limb(leg, V(0.14f * s, 0.17f, 0.04f), V(0.14f * s, 0.05f, 0.0f), 0.045f, 0.036f, skin);
            b.Torus(leg, V(0.14f * s, 0.07f, 0.0f), 0.04f, 0.01f, band, mat: Shell, blend: 0.004f);
            b.Ell(leg, V(0.14f * s, 0.03f, 0.04f), V(0.05f, 0.03f, 0.07f), skin);
            Claws(b, leg, V(0.14f * s, 0.022f, 0.1f), V(0.018f, 0, 0), V(0, -0.2f, 1f), 0.03f, 0.011f);
        });
        // A broad chest over a narrow waist
        b.Ell(Body, V(0, 0.36f, 0), V(0.1f, 0.08f, 0.08f), skin);
        b.Ell(Body, V(0, 0.5f, 0.01f), V(0.15f, 0.12f, 0.11f), skin, blend: 0.04f);
        b.PaintEll(Body, V(0, 0.45f, 0.08f), V(0.08f, 0.1f, 0.05f), belly);
        // The ring of armour round its waist
        b.Torus(Body, V(0, 0.36f, 0), 0.17f, 0.035f, ring, sz: 1f, mat: Shell, blend: 0.01f);
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.Tau / 12f;
            b.PaintEll(Body, V(MathF.Sin(a) * 0.2f, 0.36f, MathF.Cos(a) * 0.2f), V(0.008f, 0.04f, 0.008f), band, soft: 0.004f);
        }
        b.Box(Body, V(0, 0.37f, 0.15f), V(0.045f, 0.035f, 0.016f), 0.01f, ring, mat: Shell, blend: 0.006f);
        // Thick arms, fists clawed, bands at the wrists
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.14f * s, 0.56f, 0.01f));
            var elbow = V(0.24f * s, 0.46f, 0.04f);
            var fist = V(0.22f * s, 0.42f, 0.16f);
            b.Ell(arm, V(0.15f * s, 0.57f, 0.01f), V(0.06f, 0.055f, 0.055f), skin);
            b.Limb(arm, V(0.15f * s, 0.56f, 0.01f), elbow, 0.05f, 0.045f, skin);
            b.Limb(arm, elbow, fist, 0.045f, 0.04f, skin);
            b.Torus(arm, Vector3.Lerp(elbow, fist, 0.6f), 0.044f, 0.012f, band, Euler(fist - elbow), mat: Shell, blend: 0.004f);
            b.Ell(arm, fist + V(0, 0, 0.03f), V(0.045f, 0.042f, 0.045f), skin);
            Claws(b, arm, fist + V(0, -0.01f, 0.07f), V(0.016f, 0, 0), V(0, -0.4f, 1f), 0.03f, 0.01f);
        });
        // A long tail banded dark and light, a point of stone at its end
        int tail = b.Tail(V(0, 0.32f, -0.08f));
        var path = Smooth(3, V(0, 0.32f, -0.08f), V(-0.05f, 0.18f, -0.24f), V(-0.12f, 0.16f, -0.42f), V(-0.16f, 0.32f, -0.5f), V(-0.14f, 0.48f, -0.46f));
        b.Tube(tail, path, 0.07f, 0.025f, light, blend: 0f);
        for (int i = 1; i < path.Length - 1; i += 2)
            b.PaintTorus(tail, path[i], 0.07f - 0.0035f * i, 0.016f, dark, Euler(path[i + 1] - path[i - 1]));
        var tip = path[^1] + Vector3.Normalize(path[^1] - path[^2]) * 0.08f;
        b.Spike(tail, path[^1], tip, 0.03f, Rgb(150, 150, 156), 0.7f, Shell);
        // A head under its drill: a heavy brow, small eyes, a jutting jaw with two short tusks
        int head = b.Head(V(0, 0.6f, 0.04f));
        var c = V(0, 0.66f, 0.07f);
        var r = V(0.085f, 0.08f, 0.08f);
        b.Ell(head, c, r, skin);
        b.Ell(head, c + V(0, -0.035f, 0.05f), V(0.065f, 0.045f, 0.045f), belly, blend: 0.02f);
        PokeBuilder.Both(s => b.Spike(head, c + V(0.035f * s, -0.04f, 0.085f), c + V(0.04f * s, 0.0f, 0.09f), 0.01f, White, mat: Shell, blend: 0.003f));
        b.Ell(head, c + V(0, 0.04f, 0.04f), V(0.08f, 0.035f, 0.05f), drill, mat: Shell, blend: 0.01f);
        Drill(b, head, c + V(0, 0.05f, -0.01f), c + V(0.04f, 0.4f, -0.08f), 0.075f, drill, groove, 6);
        PokeBuilder.Both(s =>
        {
            var look = V(0.45f * s, 0.05f, 1f);
            b.Eye(head, Out(c, r, default, look), look, 0.016f, Rgb(80, 60, 40), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ 2241 Pastorine, from Miltank

    /// <summary>
    /// Pastorine: Miltank grown larger, its pink body spotted black, its horns grown long and curling up, a brass bell
    /// hanging from a leather collar at its throat, and a garland of pink, yellow and blue flowers round its neck.
    /// </summary>
    private static PokeBuilder Pastorine()
    {
        var b = Become(Miltank(), "Pastorine", 0.94f);
        var black = Rgb(52, 48, 60);
        var horn = Rgb(242, 234, 200);
        var gold = Rgb(226, 180, 64);
        var leather = Rgb(130, 90, 60);
        var leaf = Rgb(84, 150, 70);
        var flowers = new[] { Rgb(240, 130, 170), Rgb(246, 210, 70), Rgb(110, 150, 230) };
        int head = BoneOf(b, "head");
        var c = V(0, 0.6f, 0.03f);
        // Longer horns, curling out and up
        b.Sdf.RemoveAll(s => s.Bone == head && s.Color.Equals(Rgb(238, 226, 190)));
        PokeBuilder.Both(s =>
        {
            var root = c + V(0.06f * s, 0.07f, -0.01f);
            b.Tube(head, Smooth(3, root, root + V(0.08f * s, 0.03f, 0), root + V(0.13f * s, 0.1f, 0.0f), root + V(0.11f * s, 0.17f, 0.02f)), 0.022f, 0.008f, horn, Shell, 0.004f);
        });
        // Black spots on its body and legs
        var bc = V(0, 0.34f, 0);
        var br = V(0.18f, 0.2f, 0.16f);
        foreach (var d in new[] { V(0.9f, 0.4f, -0.3f), V(-0.85f, 0.1f, -0.4f), V(0.6f, -0.2f, -0.8f), V(-0.4f, 0.6f, -0.8f), V(0.95f, -0.3f, 0.1f) })
            b.PaintEll(Body, Out(bc, br, default, d), V(0.05f, 0.055f, 0.05f), black);
        // The collar and its bell
        var neck = V(0, 0.48f, 0.02f);
        b.Torus(Body, neck, 0.12f, 0.016f, leather, V(-6f, 0, 0), mat: Fur, blend: 0.006f);
        var bell = neck + V(0, -0.06f, 0.14f);
        b.Limb(Body, bell + V(0, 0.04f, -0.01f), bell - V(0, 0.02f, 0), 0.025f, 0.04f, gold, Metal, 0.006f);
        b.Ell(Body, bell - V(0, 0.026f, 0), V(0.012f, 0.012f, 0.012f), gold, mat: Metal, blend: 0.004f);
        b.Limb(Body, bell + V(0, 0.03f, -0.01f), neck + V(0, 0, 0.13f), 0.008f, 0.008f, leather, Fur, 0.004f);
        // A garland of flowers round its neck
        for (int i = 0; i < 16; i++)
        {
            float a = i * MathF.Tau / 16f;
            var o = V(MathF.Sin(a), 0, MathF.Cos(a));
            var at = neck + V(0, 0.012f, 0) + o * 0.13f;
            b.Spike(Body, at, at + o * 0.03f + V(0, -0.015f, 0), 0.012f, leaf, 0.5f, Leaf, 0.004f);
            if (i % 2 == 0)
            {
                b.Ell(Body, at + o * 0.006f + V(0, 0.01f, 0), V(0.022f, 0.022f, 0.016f), flowers[i / 2 % 3], Euler(o), Leaf, 0.004f);
                b.Ell(Body, at + o * 0.022f + V(0, 0.01f, 0), V(0.008f, 0.008f, 0.006f), Rgb(250, 230, 120), Euler(o), Leaf, 0.003f);
            }
        }
        Stretch(b, V(1.06f, 1.08f, 1.06f));
        return b;
    }

    // ------------------------------------------------------------------ 2243 Raitenko, from Raikou

    /// <summary>
    /// Raitenko: Raikou grown larger, its storm-cloud mane grown into a towering thundercloud over its shoulders and
    /// back with bolts of white lightning in it, its stripes jagged as bolts, its fangs grown into sabres, and a puff
    /// of cloud at its tail's end.
    /// </summary>
    private static PokeBuilder Raitenko()
    {
        var b = Become(Raikou(), "Raitenko", 1f);
        var cloud = Rgb(96, 92, 132);
        var cloudLight = Rgb(140, 136, 176);
        var navy = Rgb(40, 50, 90);
        var bolt = Rgb(250, 250, 220);
        Recolour(b, (Rgb(162, 124, 180), cloud), (Rgb(194, 164, 210), cloudLight), (Rgb(48, 46, 60), navy));
        // The cloud grows, towering over its back
        Grow(b, V(1.4f, 1.5f, 1.2f), V(0, 0.58f, -0.2f), s => s.Bone == Body && s.Op == SdfOp.Add && s.Color.Equals(cloud), _ => false);
        foreach (var (at, size) in new[] { (V(0.08f, 0.8f, -0.06f), 0.09f), (V(-0.07f, 0.84f, -0.16f), 0.1f), (V(0.04f, 0.88f, -0.28f), 0.09f) })
            b.Ell(Body, at, V(size * 1.3f, size, size * 1.2f), cloud, blend: 0.03f);
        // Bolts of lightning in the cloud
        foreach (var (from, way) in new[] { (V(0.12f, 0.88f, -0.08f), V(0.06f, -0.08f, 0.02f)), (V(-0.14f, 0.84f, -0.22f), V(-0.04f, -0.08f, 0.02f)), (V(0.02f, 0.96f, -0.24f), V(0.03f, -0.07f, -0.02f)), (V(0.15f, 0.76f, -0.02f), V(0.05f, -0.06f, 0.0f)) })
        {
            var p = from;
            var zig = new[] { p, p + way, p + way + V(-way.X * 0.6f, -0.03f, 0.01f), p + way * 2f + V(-way.X * 0.4f, -0.03f, 0.01f) };
            b.Tube(Body, zig, 0.009f, 0.006f, bolt, Glow, 0.002f);
        }
        // More stripes, jagged as bolts
        var bc = V(0, 0.46f, -0.02f);
        var br = V(0.15f, 0.14f, 0.28f);
        PokeBuilder.Both(s =>
        {
            foreach (var (y, z) in new[] { (0.42f, -0.26f), (0.4f, -0.1f), (0.44f, 0.06f), (0.4f, 0.16f) })
            {
                var n = V(s, (y - bc.Y) / br.Y, (z - bc.Z) / br.Z * 0.6f);
                b.Mark(Body, Out(bc, br, default, n), n, 0.04f, 0.02f, navy, MarkShape.Zigzag, 75f);
            }
        });
        // Sabre fangs, a puff of cloud at the tail's end
        int head = BoneOf(b, "head");
        foreach (var f in b.Sdf.Shapes.Where(p => p.Bone == head && p.Shape == SdfShape.RoundCone && p.Color.Equals(Claw)))
            f.B = f.A + (f.B - f.A) * 1.8f;
        int tail = BoneOf(b, "tail");
        GasPuff(b, tail, V(0, 0.86f, -0.66f), V(0, 0.3f, -1f), 0.06f, cloud);
        Stretch(b, V(1.05f, 1.05f, 1.05f));
        return b;
    }

    // ------------------------------------------------------------------ 2244 Enkaiou, from Entei

    /// <summary>
    /// Enkaiou: Entei grown larger and darker, its legs heavier, its steel plates turned to bone-pale horns, its mane
    /// grown into a mantle of smoke over a ridge of dark rock split by glowing lava down its back, the plate on its face
    /// grown into a crown, and eyes glowing orange.
    /// </summary>
    private static PokeBuilder Enkaiou()
    {
        var b = Become(Entei(), "Enkaiou", 1f);
        var brown = Rgb(96, 72, 58);
        var bone = Rgb(206, 196, 172);
        var rockColor = Rgb(64, 56, 52);
        var lava = Rgb(250, 130, 40);
        var smoke = Rgb(120, 116, 116);
        Recolour(b, (Rgb(152, 94, 58), brown), (Rgb(190, 194, 206), bone), (Rgb(242, 204, 66), bone), (Rgb(212, 54, 50), brown), (Rgb(84, 56, 44), Rgb(70, 54, 44)));
        foreach (var d in b.Model.Decals.Where(d => d.IsEye)) { d.Iris = lava; d.White = null; }
        // Heavier legs
        foreach (var leg in Enumerable.Range(0, b.Model.Bones.Count).Where(i => b.Model.Bones[i].Role == PokeRole.Leg))
            foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == leg && s.Shape == SdfShape.RoundCone))
                s.Size *= V(1.2f, 1.2f, 1f);
        // The ridge of rock down its back, lava glowing in its cracks, and smoke over it
        for (int i = 0; i < 7; i++)
        {
            float z = 0.1f - i * 0.06f;
            var at = V(0, 0.6f - MathF.Abs(z + 0.08f) * 0.3f, z);
            b.Ell(Body, at, V(0.06f, 0.03f, 0.05f), lava, mat: Glow, blend: 0.01f);
            foreach (float s in new[] { -1f, 1f })
                b.Spike(Body, at + V(0.025f * s, -0.01f, 0), at + V(0.05f * s, 0.11f - (i % 2) * 0.03f, -0.02f), 0.035f, rockColor, 0.6f, Shell, 0.01f);
        }
        foreach (var (at, way, r) in new[] { (V(0, 0.66f, -0.02f), V(0, 1f, -0.4f), 0.09f), (V(0.04f, 0.64f, -0.16f), V(0.3f, 0.8f, -1f), 0.09f), (V(-0.04f, 0.6f, -0.28f), V(-0.3f, 0.6f, -1f), 0.08f), (V(0, 0.56f, -0.4f), V(0, 0.4f, -1f), 0.07f) })
            GasPuff(b, Body, at, way, r, smoke);
        // A crown on its brow, lit by lava
        int head = BoneOf(b, "head");
        b.PaintEll(head, V(0, 0.8f, 0.33f), V(0.02f, 0.04f, 0.03f), lava, soft: 0.005f);
        Stretch(b, V(1.05f, 1.05f, 1.05f));
        return b;
    }

    // ------------------------------------------------------------------ 2245 Suiseiren, from Suicune

    /// <summary>
    /// Suiseiren: Suicune grown larger, a deeper blue marked with pale waves, its mane and streamers turned to
    /// rippling water, two more streamers looping round its body to make four, its crystal crest a clear crown, white
    /// socks and a mist of soft blue light about its feet.
    /// </summary>
    private static PokeBuilder Suiseiren()
    {
        var b = Become(Suicune(), "Suiseiren", 1f);
        var blue = Rgb(84, 124, 214);
        var pale = Rgb(214, 234, 250);
        var water = Rgb(196, 222, 246);
        var clear = Rgb(200, 226, 246);
        var mist = Rgb(170, 210, 250);
        Recolour(b, (Rgb(122, 206, 228), blue), (Rgb(162, 122, 202), water), (Rgb(66, 146, 196), clear));
        foreach (var s in b.Sdf.Shapes.Where(s => s.Color.Equals(clear))) s.Material = Glow;
        // Two more streamers from its shoulders, looping round its body and away behind
        PokeBuilder.Both(s => Ribbon(b, Body, Smooth(3, V(0.08f * s, 0.6f, 0.12f), V(0.2f * s, 0.62f, 0.06f), V(0.22f * s, 0.58f, -0.14f), V(0.16f * s, 0.66f, -0.32f), V(0.24f * s, 0.74f, -0.46f)), 0.05f, water, V(0, 1f, 0.2f), 0.014f));
        // White socks, a mist of light round its feet
        foreach (var leg in Enumerable.Range(0, b.Model.Bones.Count).Where(i => b.Model.Bones[i].Role == PokeRole.Leg))
        {
            var foot = b.Sdf.Shapes.Where(p => p.Bone == leg && p.Shape == SdfShape.Ellipsoid).OrderBy(p => p.A.Y).First().A;
            b.PaintEll(leg, foot + V(0, 0.06f, 0), V(0.06f, 0.08f, 0.06f), pale);
            b.Ell(leg, foot + V(0, -0.005f, 0.01f), V(0.08f, 0.012f, 0.08f), mist, mat: Glow, blend: 0.012f);
        }
        // Pale waves along its flanks
        var bc = V(0, 0.48f, -0.02f);
        var br = V(0.12f, 0.11f, 0.27f);
        PokeBuilder.Both(s => b.PaintEll(Body, bc + V(0.1f * s, -0.04f, -0.06f), V(0.04f, 0.04f, 0.2f), pale));
        Stretch(b, V(1.05f, 1.05f, 1.05f));
        return b;
    }
    // @@more@@
}
