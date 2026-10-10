using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Popular species from outside the Sinnoh Pokédex (plan 03, decision 3), Paldea's third batch in National Pokédex
// order: Capsakid (951) to Cetitan (975), with Scovillain's and Glimmora's Mega forms and Palafin's Hero Form beside
// their species. Helpers shared with the earlier batches are in the files of those batches, PokemonModels.Sinnoh1.cs to
// PokemonModels.Paldea2.cs.
internal static partial class PokemonModels
{
    // ------------------------------------------------------------------ Capsakid and Scovillain

    private static readonly Color PepperGreen = Rgb(112, 186, 72);
    private static readonly Color PepperDark = Rgb(50, 116, 52);
    private static readonly Color PepperLight = Rgb(190, 222, 116);
    private static readonly Color PepperPale = Rgb(170, 220, 104);
    private static readonly Color PepperRed = Rgb(226, 50, 42);
    private static readonly Color PepperOrange = Rgb(248, 142, 40);
    private static readonly Color PepperWhite = Rgb(248, 246, 236);
    private static readonly Color PepperMouth = Rgb(96, 28, 38);
    private static readonly Color SuitBlack = Rgb(46, 44, 52);

    /// <summary>Capsakid: a little green pepper on two stubby legs with no arms, a round head under a white cap split into six lobes at its rim, a beak-like mouth tipped orange with one front tooth, black eyes and a stubby tail.</summary>
    private static PokeBuilder Capsakid()
    {
        var b = new PokeBuilder("Capsakid", 0.42f, BodyPlan.Biped, V(0, 0.1f, 0)) { Coat = Leaf };
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.035f * s, 0.07f, 0));
            b.Limb(leg, V(0.035f * s, 0.08f, 0), V(0.04f * s, 0.025f, 0.005f), 0.02f, 0.018f, PepperDark);
            b.Ell(leg, V(0.042f * s, 0.016f, 0.014f), V(0.024f, 0.016f, 0.03f), PepperDark);
        });
        var bc = V(0, 0.1f, 0);
        b.Ell(Body, bc, V(0.055f, 0.06f, 0.05f), PepperGreen);
        b.PaintEll(Body, bc + V(0, -0.005f, 0.04f), V(0.035f, 0.045f, 0.025f), PepperLight);
        int tail = b.Tail(bc + V(0, -0.03f, -0.04f));
        b.Spike(tail, bc + V(0, -0.03f, -0.04f), bc + V(0, -0.045f, -0.085f), 0.018f, PepperGreen);
        int head = b.Head(bc + V(0, 0.05f, 0));
        var c = bc + V(0, 0.115f, 0.01f);
        var r = V(0.075f, 0.068f, 0.07f);
        b.Limb(head, bc + V(0, 0.03f, 0), c + V(0, -0.04f, 0), 0.04f, 0.045f, PepperGreen);
        b.Ell(head, c, r, PepperGreen);
        // The beak-like mouth, its tip orange, and the one front tooth under it
        var beakTip = c + V(0, -0.05f, 0.125f);
        b.Spike(head, c + V(0, -0.025f, 0.05f), beakTip, 0.034f, PepperGreen, 0.8f);
        b.PaintEll(head, beakTip + V(0, 0.002f, -0.005f), V(0.026f, 0.024f, 0.03f), PepperOrange, soft: 0.006f);
        b.Ell(head, c + V(0, -0.06f, 0.085f), V(0.009f, 0.012f, 0.006f), PepperWhite, mat: Shell, blend: 0.004f);
        // The white cap, split into six lobes at its rim, and the stalk standing from its middle
        var cap = c + V(0, 0.055f, -0.005f);
        b.Ell(head, cap, V(0.068f, 0.03f, 0.064f), PepperWhite, blend: 0.012f);
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f + MathF.PI / 6f;
            b.Ell(head, cap + V(MathF.Sin(a) * 0.058f, -0.01f, MathF.Cos(a) * 0.055f), V(0.026f, 0.016f, 0.026f), PepperWhite, blend: 0.008f);
        }
        b.Limb(head, cap + V(0, 0.02f, 0), cap + V(0.008f, 0.05f, -0.008f), 0.011f, 0.008f, PepperDark, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.034f * s, c.Y - 0.004f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(30, 30, 30));
        });
        return b;
    }

    /// <summary>
    /// One of Scovillain's heads, a bell pepper (<paramref name="red"/> or pale green) at <paramref name="c"/> with lobes
    /// over its crown, black eyes, a dark mouth with white fangs above and longer ones jutting up from under the jaw,
    /// and an orange tongue stuck out to the side <paramref name="s"/>. The red head glares. A Mega head is rougher,
    /// ridged with white spikes: a row of four down the red one's crown, two over each of the green one's eyes and one
    /// at each side of its jaw.
    /// </summary>
    private static void ScovillainHead(PokeBuilder b, int head, Vector3 c, Vector3 r, bool red, bool mega, float s)
    {
        var skin = red ? PepperRed : PepperPale;
        b.Ell(head, c, r, skin);
        foreach (var (x, z) in new[] { (-0.42f, -0.15f), (0.42f, -0.15f), (0f, 0.3f) })
            b.Ell(head, c + V(x * r.X, 0.55f * r.Y, z * r.Z), r * 0.55f, skin, blend: 0.02f);
        var mouth = c + V(0, -0.45f * r.Y, 0.8f * r.Z);
        b.PaintEll(head, mouth, V(0.66f * r.X, 0.2f * r.Y, 0.35f * r.Z), PepperMouth, soft: 0.006f);
        PokeBuilder.Both(t =>
        {
            b.Spike(head, mouth + V(0.3f * r.X * t, 0.1f * r.Y, 0), mouth + V(0.3f * r.X * t, -0.22f * r.Y, 0.12f * r.Z), 0.1f * r.X, PepperWhite, mat: Shell, blend: 0.004f);
            b.Spike(head, mouth + V(0.5f * r.X * t, -0.42f * r.Y, -0.06f * r.Z), mouth + V(0.52f * r.X * t, 0.12f * r.Y, 0.16f * r.Z), 0.12f * r.X, PepperWhite, mat: Shell, blend: 0.004f);
        });
        b.Limb(head, mouth + V(0, -0.05f * r.Y, 0), mouth + V(0.08f * r.X * s, -0.5f * r.Y, 0.3f * r.Z), 0.17f * r.X, 0.15f * r.X, PepperOrange, blend: 0.006f);
        PokeBuilder.Both(t =>
        {
            var at = On(c, r, c.X + 0.36f * r.X * t, c.Y + 0.06f * r.Y);
            b.Eye(head, at, Outward(c, r, at), 0.15f * r.Y, Rgb(28, 26, 30), glare: red);
        });
        if (!mega) return;
        if (red)
            foreach (float z in new[] { 0.5f, 0.15f, -0.2f, -0.55f })
            {
                var root = c + V(0, 0.9f * r.Y, z * r.Z);
                b.Spike(head, root, root + V(0, 0.42f * r.Y, 0.12f * r.Z), 0.13f * r.X, PepperWhite, mat: Shell, blend: 0.006f);
            }
        else
            PokeBuilder.Both(t =>
            {
                foreach (float dx in new[] { -0.14f, 0.14f })
                {
                    var root = c + V((0.36f + dx) * r.X * t, 0.48f * r.Y, 0.68f * r.Z);
                    b.Spike(head, root, root + V(0.1f * r.X * t, 0.36f * r.Y, 0.18f * r.Z), 0.1f * r.X, PepperWhite, mat: Shell, blend: 0.006f);
                }
                var jaw = c + V(0.8f * r.X * t, -0.45f * r.Y, 0.35f * r.Z);
                b.Spike(head, jaw, jaw + V(0.35f * r.X * t, -0.2f * r.Y, 0.05f * r.Z), 0.12f * r.X, PepperWhite, mat: Shell, blend: 0.006f);
            });
    }

    /// <summary>
    /// Scovillain, or (<paramref name="mega"/>) its Mega form: a two-headed pepper standing on short legs with a white
    /// claw at each foot, dark green below and green above, a stubby tail, a collar of leaves round the root of its two
    /// stalk necks, a red head on its right that glares and a pale green one on its left that grins. The Mega form
    /// stands taller, its top half, arms, necks and collar black like a suit, the collar popped up behind, and its
    /// heads ridged with white spikes.
    /// </summary>
    private static PokeBuilder ScovillainBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Scovillain-Mega" : "Scovillain", mega ? 0.9f : 0.8f, BodyPlan.Biped, V(0, 0.36f, 0)) { Coat = Leaf };
        var top = mega ? SuitBlack : PepperGreen;
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.24f, 0));
            var knee = V(0.09f * s, 0.13f, 0.03f);
            b.Limb(leg, V(0.07f * s, 0.25f, 0), knee, 0.05f, 0.04f, PepperDark);
            b.Limb(leg, knee, V(0.09f * s, 0.045f, 0.0f), 0.04f, 0.035f, PepperDark);
            b.Ell(leg, V(0.09f * s, 0.03f, 0.03f), V(0.045f, 0.03f, 0.06f), PepperDark);
            b.Spike(leg, V(0.09f * s, 0.028f, 0.075f), V(0.09f * s, 0.008f, 0.125f), 0.016f, PepperWhite, mat: Shell);
        });
        var bc = V(0, 0.3f, 0);
        b.Ell(Body, bc, V(0.11f, 0.11f, 0.09f), PepperDark);
        b.Ell(Body, bc + V(0, 0.13f, 0.01f), V(0.095f, 0.1f, 0.075f), top);
        if (mega)
        {
            // The suit's front left open on a green shirt
            b.PaintEll(Body, bc + V(0, 0.1f, 0.07f), V(0.03f, 0.09f, 0.03f), PepperGreen, soft: 0.008f);
        }
        int tail = b.Tail(bc + V(0, -0.04f, -0.07f));
        b.Spike(tail, bc + V(0, -0.04f, -0.06f), bc + V(0, -0.13f, -0.2f), 0.045f, PepperDark);
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.09f * s, 0.17f, 0);
            int arm = b.Arm(s, shoulder);
            var elbow = bc + V(0.15f * s, 0.08f, 0.03f);
            var hand = bc + V(0.16f * s, 0.0f, 0.07f);
            b.Limb(arm, shoulder, elbow, 0.032f, 0.027f, top);
            b.Limb(arm, elbow, hand, 0.027f, 0.025f, top);
            b.Ell(arm, hand + V(0, -0.012f, 0.004f), V(0.03f, 0.028f, 0.03f), PepperGreen);
            for (int i = -1; i <= 1; i++)
                b.Spike(arm, hand + V(0.014f * i * s, -0.03f, 0.016f), hand + V(0.018f * i * s, -0.055f, 0.03f), 0.008f, PepperWhite, mat: Shell, blend: 0.004f);
        });
        // The collar of leaves where the necks spring from the body; popped up high behind on the Mega form
        var root = bc + V(0, 0.21f, 0);
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f;
            var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
            float back = mega ? MathF.Max(0f, -dir.Z) : 0f;
            var tip = root + dir * (0.13f - 0.04f * back) + V(0, 0.04f + 0.12f * back, 0);
            Frond(b, Body, root + dir * 0.03f, tip, 0.04f, mega ? SuitBlack : PepperDark, V(0, 1f, 0) - dir * 0.5f, 0.28f, Leaf, 0.01f);
        }
        // Two stalk necks, and a head on each: red on its right, pale green on its left
        float lift = mega ? 0.05f : 0f;
        PokeBuilder.Both(s =>
        {
            bool red = s < 0;
            var c = bc + V(0.16f * s, 0.42f + lift, 0.06f);
            var r = V(0.1f, 0.094f, 0.104f) * (mega ? 1.1f : 1f);
            var neck = Smooth(3, root + V(0.03f * s, -0.02f, 0), root + V(0.08f * s, 0.09f + lift * 0.5f, 0.01f), c + V(-0.025f * s, -0.075f, -0.04f));
            b.Tube(Body, neck, 0.032f, 0.028f, mega ? SuitBlack : PepperGreen, blend: 0f);
            int head = red ? b.Head(c + V(0, -0.05f, -0.02f)) : b.Part("head2", Body, c + V(0, -0.05f, -0.02f), PokeRole.Head, 1.7f, 1f);
            ScovillainHead(b, head, c, r, red, mega, s);
        });
        return b;
    }

    private static PokeBuilder Scovillain() => ScovillainBuild(false);

    private static PokeBuilder ScovillainMega() => ScovillainBuild(true);

    // ------------------------------------------------------------------ Rellor and Rabsca

    private static readonly Color BeetleBrown = Rgb(132, 86, 56);
    private static readonly Color BeetleYellow = Rgb(236, 196, 80);
    private static readonly Color MudBrown = Rgb(152, 110, 76);
    private static readonly Color ScarabTeal = Rgb(56, 152, 150);
    private static readonly Color ScarabRed = Rgb(212, 50, 62);

    /// <summary>
    /// The four legs of a beetle lying on its back at <paramref name="bc"/>, reaching up to hold a ball at
    /// <paramref name="ball"/> of radius <paramref name="radius"/>, each tipped with <paramref name="spikes"/> little claws.
    /// </summary>
    private static void BallLegs(PokeBuilder b, Vector3 bc, Vector3 ball, float radius, float thick, Color color, int spikes)
    {
        PokeBuilder.Both(s =>
        {
            foreach (float z in new[] { 0.045f, -0.05f })
            {
                var hip = bc + V(0.045f * s, 0.005f, z);
                var knee = bc + V(0.105f * s, 0.035f, z * 1.25f);
                var toward = Vector3.Normalize(knee + V(0, 0.07f, 0) - ball);
                var tip = ball + toward * (radius + thick * 0.3f);
                b.Limb(Body, hip, knee, thick * 1.1f, thick, color);
                b.Limb(Body, knee, tip, thick, thick * 0.85f, color);
                for (int i = 0; i < spikes; i++)
                {
                    var at = Vector3.Lerp(knee, tip, 0.25f + 0.25f * i);
                    b.Spike(Body, at, at + Vector3.Normalize(V(s, 0.3f, z * 4f)) * thick * 1.6f, thick * 0.5f, color, blend: 0.004f);
                }
            }
        });
    }

    /// <summary>Rellor: a little brown dung beetle lying on its back under a mud ball bigger than itself, brown streaked white and orange, held up on its four legs; a pink face with drowsy eyes, a round nose, a black brow above and a black chinstrap below, four little horns about its head and a yellow patch on its belly.</summary>
    private static PokeBuilder Rellor()
    {
        var b = new PokeBuilder("Rellor", 0.4f, BodyPlan.Quadruped, V(0, 0.06f, 0)) { Coat = Shell };
        var bc = V(0, 0.055f, 0);
        b.Ell(Body, bc, V(0.065f, 0.042f, 0.08f), BeetleBrown);
        b.PaintEll(Body, bc + V(0, 0.02f, -0.07f), V(0.045f, 0.03f, 0.03f), BeetleYellow, soft: 0.008f);
        // The mud ball, streaked white and orange
        var ball = V(0, 0.205f, -0.01f);
        const float radius = 0.12f;
        b.Ell(Body, ball, V(radius, radius, radius), MudBrown, mat: Fur);
        b.PaintTorus(Body, ball, radius, 0.012f, PepperWhite, V(25f, 0, 60f));
        b.PaintTorus(Body, ball, radius, 0.01f, PepperOrange, V(-35f, 30f, -20f));
        b.PaintTorus(Body, ball, radius, 0.008f, PepperWhite, V(80f, 10f, 15f));
        BallLegs(b, bc, ball, radius, 0.012f, BeetleBrown, 1);
        int head = b.Head(bc + V(0, 0.01f, 0.06f));
        var c = bc + V(0, 0.012f, 0.088f);
        var r = V(0.048f, 0.042f, 0.036f);
        b.Ell(head, c, r, BeetleBrown);
        b.PaintEll(head, c + V(0, -0.002f, 0.03f), V(0.042f, 0.036f, 0.02f), Rgb(238, 156, 174));
        b.Ell(head, On(c, r, c.X, c.Y - 0.008f) + V(0, 0, -0.002f), V(0.012f, 0.01f, 0.01f), Rgb(224, 120, 146), blend: 0.004f);
        foreach (var d in new[] { V(0.5f, 0.85f, -0.2f), V(-0.5f, 0.85f, -0.2f), V(0.95f, 0.3f, -0.1f), V(-0.95f, 0.3f, -0.1f) })
        {
            var n = Vector3.Normalize(d);
            b.Spike(head, c + n * r * 0.9f, c + n * r * 0.9f + n * 0.03f, 0.011f, BeetleBrown, blend: 0.006f);
        }
        var black = Rgb(36, 30, 32);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.019f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.008f, Rgb(40, 30, 34));
        });
        var brow = On(c, r, c.X, c.Y + 0.024f);
        b.Mark(head, brow, Outward(c, r, brow), 0.024f, 0.004f, black, MarkShape.Bar);
        var chin = On(c, r, c.X, c.Y - 0.024f);
        b.Mark(head, chin, Outward(c, r, chin), 0.024f, 0.007f, black, MarkShape.Smile);
        return b;
    }

    /// <summary>Rabsca: a teal scarab floating on its back under a great glowing pink ball wreathed in purple cloud, held up on its legs; a red face with its eyes closed, a white four-pointed star on its brow, a diamond nose, long white brows and whiskers sweeping back like wings, and four little horns.</summary>
    private static PokeBuilder Rabsca()
    {
        var b = new PokeBuilder("Rabsca", 0.5f, BodyPlan.Floating, V(0, 0.09f, 0)) { Coat = Metal }.Hover();
        var bc = V(0, 0.085f, 0);
        b.Ell(Body, bc, V(0.06f, 0.038f, 0.075f), ScarabTeal);
        foreach (float z in new[] { -0.04f, 0f, 0.04f })
            b.PaintEll(Body, bc + V(0, -0.03f, z), V(0.07f, 0.02f, 0.008f), ScarabRed, soft: 0.006f);
        // The ball, glowing pink and wreathed in purple cloud
        var ball = V(0, 0.24f, -0.01f);
        const float radius = 0.12f;
        b.Ell(Body, ball, V(radius, radius, radius), Rgb(238, 100, 150), mat: Glow);
        b.PaintEll(Body, ball + V(-0.04f, 0.05f, 0.08f), V(0.05f, 0.04f, 0.04f), Rgb(250, 170, 200), soft: 0.02f);
        var cloud = Rgb(150, 92, 204);
        for (int i = 0; i < 7; i++)
        {
            float a = i * MathF.Tau / 7f + 0.3f;
            var at = ball + V(MathF.Sin(a) * 0.112f, -0.03f + 0.025f * MathF.Sin(a * 3f), MathF.Cos(a) * 0.112f);
            b.Ell(Body, at, V(0.042f, 0.034f, 0.042f), cloud, mat: Fur, blend: 0.02f);
        }
        BallLegs(b, bc, ball, radius, 0.011f, ScarabTeal, 3);
        int head = b.Head(bc + V(0, 0.01f, 0.06f));
        var c = bc + V(0, 0.016f, 0.1f);
        var r = V(0.05f, 0.044f, 0.04f);
        b.Ell(head, c, r, ScarabTeal);
        b.PaintEll(head, c + V(0, -0.002f, 0.026f), V(0.036f, 0.032f, 0.018f), ScarabRed);
        foreach (var d in new[] { V(0.45f, 0.85f, -0.3f), V(-0.45f, 0.85f, -0.3f), V(0.9f, 0.4f, -0.3f), V(-0.9f, 0.4f, -0.3f) })
        {
            var n = Vector3.Normalize(d);
            b.Spike(head, c + n * r * 0.9f, c + n * r * 0.9f + n * 0.028f, 0.01f, ScarabTeal, blend: 0.006f);
        }
        // Long white brows and whiskers sweeping back like wings
        PokeBuilder.Both(s =>
        {
            Blade(b, head, c + V(0.02f * s, -0.014f, 0.024f), c + V(0.17f * s, -0.03f, -0.04f), 0.02f, PepperWhite, V(0, 1f, 0.2f), 0.3f, Fur);
            Blade(b, head, c + V(0.022f * s, 0.016f, 0.02f), c + V(0.14f * s, 0.065f, -0.05f), 0.016f, PepperWhite, V(0, 1f, 0.2f), 0.3f, Fur);
            var at = On(c, r, c.X + 0.016f * s, c.Y + 0.002f);
            b.Eye(head, at, Outward(c, r, at), 0.008f, closed: true);
        });
        var star = On(c, r, c.X, c.Y + 0.019f);
        b.Mark(head, star, Outward(c, r, star), 0.008f, 0.008f, PepperWhite, MarkShape.Star);
        var nose = On(c, r, c.X, c.Y - 0.014f);
        b.Mark(head, nose, Outward(c, r, nose), 0.006f, 0.007f, Rgb(140, 28, 40), MarkShape.Diamond);
        return b;
    }

    // ------------------------------------------------------------------ Flittle and Espathra

    private static readonly Color FrillYellow = Rgb(248, 220, 96);
    private static readonly Color FrillPink = Rgb(244, 150, 188);
    private static readonly Color FrillWhite = Rgb(248, 246, 244);
    private static readonly Color FrillLavender = Rgb(176, 146, 222);
    private static readonly Color FrillBlue = Rgb(66, 124, 230);

    /// <summary>Flittle: a little yellow chick of a head floating over a skirt of yellow and pink frills, white frills standing on its crown, big blue eyes with a lavender triangle between them, a small lavender mouth and two tiny pointed feet dangling below.</summary>
    private static PokeBuilder Flittle()
    {
        var b = new PokeBuilder("Flittle", 0.38f, BodyPlan.Floating, V(0, 0.12f, 0)) { Coat = Fur }.Hover();
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.025f * s, 0.09f, 0));
            b.Spike(leg, V(0.025f * s, 0.1f, 0.005f), V(0.03f * s, 0.022f, 0.012f), 0.012f, Rgb(240, 178, 84));
        });
        var bc = V(0, 0.12f, 0);
        b.Ell(Body, bc, V(0.05f, 0.04f, 0.05f), FrillYellow);
        // The skirt of frills: yellow above, pink below
        for (int layer = 0; layer < 2; layer++)
            for (int i = 0; i < 8; i++)
            {
                float a = i * MathF.Tau / 8f + layer * MathF.PI / 8f;
                var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
                var root = bc + V(0, 0.01f - 0.015f * layer, 0) + dir * 0.03f;
                var tip = bc + dir * (0.095f + 0.01f * layer) + V(0, -0.04f - 0.02f * layer, 0);
                Frond(b, Body, root, tip, 0.032f, layer == 0 ? FrillYellow : FrillPink, dir + V(0, 0.8f, 0), 0.3f, Fur, 0.01f);
            }
        int head = b.Head(bc + V(0, 0.04f, 0));
        var c = bc + V(0, 0.095f, 0.0f);
        var r = V(0.075f, 0.07f, 0.07f);
        b.Ell(head, c, r, FrillYellow);
        // White frills standing on its crown
        foreach (var (x, z, h) in new[] { (0f, 0.01f, 0.07f), (0.03f, -0.01f, 0.055f), (-0.03f, -0.01f, 0.055f), (0f, -0.035f, 0.05f) })
        {
            var root = c + V(x * 0.6f, 0.055f, z);
            Frond(b, head, root, root + V(x, h, z * 0.6f), 0.022f, FrillWhite, V(0, 0, 1f), 0.4f, Fur, 0.008f);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.034f * s, c.Y - 0.002f);
            b.Eye(head, at, Outward(c, r, at), 0.019f, FrillBlue);
        });
        var mark = On(c, r, c.X, c.Y + 0.012f);
        b.Mark(head, mark, Outward(c, r, mark), 0.011f, 0.011f, FrillLavender, MarkShape.Triangle);
        var mouth = On(c, r, c.X, c.Y - 0.034f);
        b.Mark(head, mouth, Outward(c, r, mouth), 0.007f, 0.005f, FrillLavender);
        return b;
    }

    /// <summary>Espathra: a tall ostrich of a Pokémon, a great body of white feathers with rounded brown wings folded on top, a long lavender neck ringed brown, a pointed snout, big blue eyes with white pupils under a fan of white frills like a headdress, long yellow and white frills trailing behind, and long lavender legs striped white.</summary>
    private static PokeBuilder Espathra()
    {
        var b = new PokeBuilder("Espathra", 1.0f, BodyPlan.Biped, V(0, 0.56f, -0.02f)) { Coat = Fur };
        var lavender = Rgb(196, 172, 228);
        var brown = Rgb(150, 102, 66);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.44f, -0.02f));
            var knee = V(0.085f * s, 0.27f, 0.03f);
            var ankle = V(0.085f * s, 0.05f, -0.01f);
            b.Limb(leg, V(0.07f * s, 0.46f, -0.02f), knee, 0.045f, 0.028f, lavender);
            b.Limb(leg, knee, ankle, 0.028f, 0.022f, lavender);
            foreach (float y in new[] { 0.11f, 0.17f, 0.23f })
                b.PaintTorus(leg, Vector3.Lerp(ankle, knee, (y - 0.05f) / 0.22f), 0.025f, 0.008f, FrillWhite);
            b.Ell(leg, ankle + V(0, -0.025f, 0.01f), V(0.03f, 0.022f, 0.035f), lavender);
            foreach (float t in new[] { -1f, 1f })
            {
                var toe = ankle + V(0.018f * t, -0.03f, 0.02f);
                b.Spike(leg, toe, toe + V(0.022f * t, -0.012f, 0.085f), 0.016f, lavender);
                b.PaintEll(leg, toe + V(0.012f * t, -0.006f, 0.045f), V(0.02f, 0.02f, 0.006f), FrillWhite, soft: 0.005f);
            }
            b.Spike(leg, ankle + V(0, -0.025f, -0.02f), ankle + V(0, -0.035f, -0.07f), 0.013f, lavender);
        });
        var bc = V(0, 0.56f, -0.02f);
        b.Ell(Body, bc, V(0.2f, 0.17f, 0.24f), FrillWhite);
        // The wings folded over its back, rounded and brown with pale triangles and short frills near their tips
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, bc + V(0.12f * s, 0.07f, 0.06f));
            var wc = bc + V(0.13f * s, 0.1f, -0.03f);
            b.Ell(wing, wc, V(0.1f, 0.07f, 0.2f), brown, V(0, 0, -20f * s), blend: 0.02f);
            foreach (float z in new[] { 0.06f, -0.02f, -0.1f })
                b.PaintEll(wing, wc + V(0.04f * s, 0.045f, z), V(0.03f, 0.03f, 0.016f), Rgb(222, 196, 150), V(0, 0, -20f * s), 0.008f);
            for (int i = 0; i < 3; i++)
            {
                var root = wc + V((0.03f + 0.025f * i) * s, 0.0f, -0.17f);
                Frond(b, wing, root, root + V(0.03f * s * i, -0.03f, -0.07f), 0.022f, i == 1 ? FrillYellow : FrillWhite, V(0, 1f, 0), 0.35f, Fur, 0.008f);
            }
        });
        // The long frills trailing from its back, yellow and white
        int tail = b.Tail(bc + V(0, 0.02f, -0.19f));
        for (int i = 0; i < 7; i++)
        {
            float x = (i - 3) / 3f;
            var root = bc + V(0.12f * x, 0.05f - 0.03f * MathF.Abs(x), -0.21f);
            var tip = root + V(0.08f * x, -0.27f + 0.06f * MathF.Abs(x), -0.16f + 0.03f * MathF.Abs(x));
            Frond(b, tail, root, tip, 0.05f, i % 2 == 0 ? FrillYellow : FrillWhite, V(0, 0.4f, -1f), 0.3f, Fur, 0.012f);
        }
        // The long neck, ringed brown
        var neck = Smooth(3, bc + V(0, 0.07f, 0.15f), bc + V(0, 0.2f, 0.19f), bc + V(0, 0.31f, 0.17f));
        b.Tube(Body, neck, 0.045f, 0.034f, lavender, blend: 0f);
        for (int i = 1; i < neck.Length - 1; i += 2)
            b.PaintTorus(Body, neck[i], 0.04f, 0.009f, brown, Euler(neck[i + 1] - neck[i - 1]));
        int head = b.Head(bc + V(0, 0.3f, 0.17f));
        var c = bc + V(0, 0.37f, 0.18f);
        var r = V(0.07f, 0.065f, 0.068f);
        b.Ell(head, c, r, lavender);
        b.Spike(head, c + V(0, -0.015f, 0.05f), c + V(0, -0.035f, 0.19f), 0.032f, lavender, 0.85f);
        // The fan of white frills over its head like a headdress
        for (int i = 0; i < 7; i++)
        {
            float a = (-75f + 25f * i) * Degree;
            var dir = V(MathF.Sin(a), MathF.Cos(a), 0);
            var root = c + V(0, 0.01f, -0.035f) + dir * 0.04f;
            Frond(b, head, root, root + dir * (0.12f - 0.02f * MathF.Abs(MathF.Sin(a))) + V(0, 0, -0.02f), 0.04f, FrillWhite, V(0, 0.2f, 1f), 0.3f, Fur, 0.01f);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.038f * s, c.Y + 0.006f);
            b.Eye(head, at, Outward(c, r, at), 0.022f, white: FrillBlue, pupil: FrillWhite);
        });
        return b;
    }

    // ------------------------------------------------------------------ Tinkatink line

    private static readonly Color TinkPink = Rgb(240, 142, 178);
    private static readonly Color TinkLight = Rgb(250, 200, 216);
    private static readonly Color TinkWhite = Rgb(250, 236, 240);
    private static readonly Color TinkDark = Rgb(214, 96, 142);
    private static readonly Color TinkSteel = Rgb(150, 156, 172);
    private static readonly Color TinkSteelDark = Rgb(100, 104, 122);
    private static readonly Color TinkEye = Rgb(170, 178, 198);
    private static readonly Color TinkBrow = Rgb(120, 54, 88);

    /// <summary>A stumpy toeless foot of the Tinkatink line at <paramref name="at"/>, a light pink circle on its sole.</summary>
    private static void TinkFoot(PokeBuilder b, int leg, Vector3 at, float k)
    {
        b.Ell(leg, at, V(0.024f, 0.018f, 0.028f) * k, TinkPink);
        b.PaintEll(leg, at + V(0, -0.016f, 0) * k, V(0.018f, 0.008f, 0.021f) * k, TinkLight, soft: 0.005f * k);
    }

    /// <summary>A hand of the Tinkatink line at <paramref name="at"/>: a round palm and three short fingers, or five with the thumb in <paramref name="thumb"/>.</summary>
    private static void TinkHand(PokeBuilder b, int arm, Vector3 at, Vector3 down, float r, Color color, Color? thumb = null)
    {
        b.Ell(arm, at, V(r, r, r * 0.9f), color);
        int count = thumb == null ? 3 : 4;
        var across = Vector3.Normalize(Vector3.Cross(down, V(0, 0, 1f)) + V(0.0001f, 0, 0));
        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0 : i / (count - 1f) - 0.5f;
            var root = at + down * r * 0.6f + across * t * r * 1.1f;
            b.Limb(arm, root, root + down * r * 0.75f + V(0, 0, r * 0.2f), r * 0.32f, r * 0.28f, color, blend: 0.006f);
        }
        if (thumb is Color tc)
            b.Limb(arm, at + V(0, 0, r * 0.6f), at + V(0, -r * 0.3f, r * 1.4f), r * 0.34f, r * 0.3f, tc, blend: 0.006f);
    }

    /// <summary>The face of the Tinkatink line on a head at <paramref name="c"/>: light pink cheeks, silver eyes under thin brows tilted <paramref name="brow"/> degrees (worried or cross) and a mouth.</summary>
    private static void TinkFace(PokeBuilder b, int head, Vector3 c, Vector3 r, float k, float brow, bool glare, MarkShape mouthShape, float mouthWidth)
    {
        PokeBuilder.Both(s =>
        {
            b.PaintEll(head, On(c, r, c.X + 0.055f * k * s, c.Y - 0.024f * k), V(0.02f, 0.015f, 0.02f) * k, TinkLight, soft: 0.006f * k);
            var at = On(c, r, c.X + 0.032f * k * s, c.Y - 0.004f * k);
            b.Eye(head, at, Outward(c, r, at), 0.013f * k, TinkEye, glare: glare);
            var over = On(c, r, c.X + 0.034f * k * s, c.Y + 0.024f * k);
            b.Mark(head, over, Outward(c, r, over), 0.014f * k, 0.0025f * k, TinkBrow, MarkShape.Bar, brow * s);
        });
        var mouth = On(c, r, c.X, c.Y - 0.036f * k);
        b.Mark(head, mouth, Outward(c, r, mouth), mouthWidth * k, 0.006f * k, TinkBrow, mouthShape);
    }

    /// <summary>Tinkatink: a short pink girl of a Pokémon with a big head and big three-fingered arms, a light pink cap with a white tuft tied on top, light pink cheeks, silver eyes under worried brows, a diamond of steel on its chest and stumpy feet, holding up a little rattle of a hammer, a grey drum on a short handle.</summary>
    private static PokeBuilder Tinkatink()
    {
        var b = new PokeBuilder("Tinkatink", 0.45f, BodyPlan.Biped, V(0, 0.1f, 0)) { Coat = Fur };
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.03f * s, 0.06f, 0));
            b.Limb(leg, V(0.03f * s, 0.07f, 0), V(0.034f * s, 0.028f, 0.004f), 0.02f, 0.02f, TinkPink);
            TinkFoot(b, leg, V(0.036f * s, 0.018f, 0.012f), 1f);
        });
        var bc = V(0, 0.1f, 0);
        b.Ell(Body, bc, V(0.045f, 0.048f, 0.04f), TinkPink);
        b.Box(Body, bc + V(0, 0.002f, 0.034f), V(0.02f, 0.02f, 0.012f), 0.004f, TinkSteel, V(0, 0, 45f), Metal, 0.006f);
        b.Spike(Body, bc + V(0, 0.002f, 0.04f), bc + V(0, 0.002f, 0.06f), 0.016f, TinkSteel, mat: Metal, blend: 0.003f);
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.042f * s, 0.028f, 0);
            int arm = b.Arm(s, shoulder);
            // The right arm holds the hammer up beside its head; the left hangs at its side
            var hand = s < 0 ? bc + V(-0.115f, 0.06f, 0.04f) : bc + V(0.085f, -0.03f, 0.025f);
            b.Limb(arm, shoulder, hand, 0.02f, 0.023f, TinkPink);
            TinkHand(b, arm, hand, s < 0 ? V(0, 0, 1f) : V(0, -1f, 0), 0.026f, TinkPink);
            if (s > 0) return;
            var head = hand + V(-0.02f, 0.095f, 0.0f);
            b.Limb(arm, hand + V(0.006f, -0.02f, -0.005f), head, 0.011f, 0.011f, TinkSteelDark, Metal, 0.004f);
            b.Limb(arm, head + V(0, 0, -0.04f), head + V(0, 0, 0.045f), 0.036f, 0.036f, TinkSteel, Metal, 0.006f);
            foreach (float z in new[] { -0.04f, 0.045f })
                b.PaintTorus(arm, head + V(0, 0, z), 0.036f, 0.01f, TinkSteelDark, V(90f, 0, 0));
        });
        int headBone = b.Head(bc + V(0, 0.06f, 0));
        var c = bc + V(0, 0.12f, 0.01f);
        var r = V(0.085f, 0.075f, 0.075f);
        b.Limb(headBone, bc + V(0, 0.03f, 0), c + V(0, -0.05f, 0), 0.025f, 0.03f, TinkPink);
        b.Ell(headBone, c, r, TinkPink);
        // The light pink cap, and the white tuft tied on top of it
        b.PaintEll(headBone, c + V(0, 0.06f, -0.01f), V(0.1f, 0.05f, 0.1f), TinkLight, soft: 0.008f);
        b.Ell(headBone, c + V(0, 0.08f, -0.012f), V(0.026f, 0.024f, 0.024f), TinkWhite, blend: 0.012f);
        b.Ell(headBone, c + V(0, 0.108f, -0.014f), V(0.016f, 0.02f, 0.016f), TinkWhite, blend: 0.008f);
        TinkFace(b, headBone, c, r, 1f, -18f, false, MarkShape.Smile, 0.012f);
        b.Box(headBone, On(c, r, c.X, c.Y - 0.046f) + V(0, 0, -0.003f), V(0.006f, 0.005f, 0.004f), 0.0015f, PepperWhite, mat: Shell, blend: 0.002f);
        return b;
    }

    /// <summary>A crude hammer's head at <paramref name="at"/>: a block of grey metal with loose plates of other greys and purples welded over it.</summary>
    private static void ScrapHead(PokeBuilder b, int bone, Vector3 at, Vector3 half, bool purple)
    {
        b.Box(bone, at, half, half.Y * 0.18f, TinkSteel, mat: Metal, blend: 0.004f);
        var plate = purple ? Rgb(124, 104, 160) : TinkSteelDark;
        b.Box(bone, at + V(-half.X, half.Y * 0.1f, half.Z * 0.15f), V(half.X * 0.12f, half.Y * 0.7f, half.Z * 0.6f), half.Y * 0.08f, plate, V(6f, 0, 0), Metal, 0.004f);
        b.Box(bone, at + V(half.X * 0.2f, half.Y, -half.Z * 0.2f), V(half.X * 0.6f, half.Y * 0.12f, half.Z * 0.55f), half.Y * 0.08f, Rgb(170, 172, 184), V(0, 8f, 0), Metal, 0.004f);
        b.Box(bone, at + V(half.X * 0.1f, -half.Y * 0.15f, half.Z), V(half.X * 0.7f, half.Y * 0.55f, half.Z * 0.1f), half.Y * 0.08f, purple ? TinkSteelDark : Rgb(132, 136, 150), V(0, 0, -5f), Metal, 0.004f);
    }

    /// <summary>Tinkatuff: a pink girl of a Pokémon with a big head and big three-fingered arms, a white growth tied into a ponytail on top, its rim zigzagged light pink, cross silver eyes, square teeth, steel plates turned out at its hips like pockets and stumpy feet, leaning on a big crude grey hammer of loose plates, its old little hammer on top of the handle.</summary>
    private static PokeBuilder Tinkatuff()
    {
        var b = new PokeBuilder("Tinkatuff", 0.62f, BodyPlan.Biped, V(0, 0.15f, 0)) { Coat = Fur };
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.09f, 0));
            b.Limb(leg, V(0.04f * s, 0.1f, 0), V(0.045f * s, 0.035f, 0.005f), 0.027f, 0.027f, TinkPink);
            TinkFoot(b, leg, V(0.047f * s, 0.024f, 0.016f), 1.3f);
        });
        var bc = V(0, 0.15f, 0);
        b.Ell(Body, bc, V(0.06f, 0.068f, 0.05f), TinkPink);
        PokeBuilder.Both(s => b.Box(Body, bc + V(0.062f * s, -0.04f, 0.0f), V(0.01f, 0.026f, 0.034f), 0.004f, TinkSteel, V(0, 0, -25f * s), Metal, 0.006f));
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.055f * s, 0.04f, 0);
            int arm = b.Arm(s, shoulder);
            var elbow = bc + V(0.105f * s, -0.01f, 0.02f);
            var hand = s < 0 ? bc + V(-0.15f, 0.0f, 0.045f) : bc + V(0.115f, -0.06f, 0.035f);
            b.Limb(arm, shoulder, elbow, 0.026f, 0.025f, TinkPink);
            b.Limb(arm, elbow, hand, 0.025f, 0.028f, TinkPink);
            TinkHand(b, arm, hand, V(0, -1f, 0.15f), 0.034f, TinkPink);
            if (s > 0) return;
            // The hammer stands on its head at its right side, its handle in its hand and its old hammer on top
            var block = V(-0.205f, 0.065f, 0.045f);
            b.Limb(arm, block + V(0, 0.04f, 0), hand + V(-0.005f, 0.07f, 0), 0.012f, 0.012f, TinkSteelDark, Metal, 0.004f);
            ScrapHead(b, arm, block, V(0.08f, 0.065f, 0.07f), false);
            var old = hand + V(-0.005f, 0.085f, 0);
            b.Limb(arm, old + V(0, 0, -0.03f), old + V(0, 0, 0.03f), 0.024f, 0.024f, TinkSteel, Metal, 0.004f);
        });
        int headBone = b.Head(bc + V(0, 0.08f, 0));
        var c = bc + V(0, 0.17f, 0.01f);
        var r = V(0.1f, 0.09f, 0.09f);
        b.Limb(headBone, bc + V(0, 0.05f, 0), c + V(0, -0.06f, 0), 0.03f, 0.036f, TinkPink);
        b.Ell(headBone, c, r, TinkPink);
        // The white growth on top, tied into a ponytail, its rim zigzagged light pink
        var crown = c + V(0, 0.06f, -0.02f);
        b.Ell(headBone, crown, V(0.085f, 0.045f, 0.075f), TinkWhite, blend: 0.012f);
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.Tau / 12f;
            float y = i % 2 == 0 ? -0.024f : -0.012f;
            b.PaintEll(headBone, crown + V(MathF.Sin(a) * 0.084f, y, MathF.Cos(a) * 0.074f), V(0.016f, 0.014f, 0.016f), TinkLight, soft: 0.005f);
        }
        var tail = Smooth(3, crown + V(0, 0.03f, -0.02f), crown + V(0, 0.08f, -0.06f), crown + V(0, 0.06f, -0.13f), crown + V(0, -0.01f, -0.16f));
        b.Tube(headBone, tail, 0.03f, 0.022f, TinkWhite, blend: 0f);
        b.Ell(headBone, crown + V(0, 0.045f, -0.035f), V(0.022f, 0.016f, 0.02f), TinkLight, blend: 0.006f);
        TinkFace(b, headBone, c, r, 1.15f, 20f, true, MarkShape.Bar, 0.02f);
        foreach (float y in new[] { -0.03f, -0.052f })
            b.Box(headBone, On(c, r, c.X, c.Y + y) + V(0, 0, -0.003f), V(0.006f, 0.005f, 0.004f), 0.0015f, PepperWhite, mat: Shell, blend: 0.002f);
        return b;
    }

    /// <summary>Tinkaton: a pink girl of a Pokémon with great white five-fingered arms, pink thumbs, a pinkish-white growth like twin tails drooping over its back and zigzagged darker pink, silver eyes, a wide mouth with two square teeth, steel plates turned out at its hips and stumpy feet, its hand on the handle of a gigantic hammer bigger than itself, welded of grey and purple scrap with a spiked beige blade on top.</summary>
    private static PokeBuilder Tinkaton()
    {
        var b = new PokeBuilder("Tinkaton", 0.85f, BodyPlan.Biped, V(0, 0.17f, 0)) { Coat = Fur };
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.045f * s, 0.1f, 0));
            b.Limb(leg, V(0.045f * s, 0.11f, 0), V(0.05f * s, 0.04f, 0.005f), 0.03f, 0.03f, TinkPink);
            TinkFoot(b, leg, V(0.052f * s, 0.026f, 0.018f), 1.45f);
        });
        var bc = V(0, 0.17f, 0);
        b.Ell(Body, bc, V(0.065f, 0.078f, 0.055f), TinkPink);
        PokeBuilder.Both(s => b.Box(Body, bc + V(0.068f * s, -0.045f, 0.0f), V(0.011f, 0.03f, 0.04f), 0.004f, TinkSteel, V(0, 0, -30f * s), Metal, 0.006f));
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.06f * s, 0.045f, 0);
            int arm = b.Arm(s, shoulder);
            var elbow = s < 0 ? bc + V(-0.13f, 0.03f, 0.03f) : bc + V(0.12f, -0.005f, 0.02f);
            var hand = s < 0 ? bc + V(-0.2f, 0.12f, 0.05f) : bc + V(0.13f, -0.07f, 0.04f);
            b.Limb(arm, shoulder, elbow, 0.034f, 0.032f, TinkWhite);
            b.Limb(arm, elbow, hand, 0.032f, 0.036f, TinkWhite);
            TinkHand(b, arm, hand, s < 0 ? V(-0.3f, -1f, 0.2f) : V(0, -1f, 0.15f), 0.04f, TinkWhite, TinkPink);
            if (s > 0) return;
            // The gigantic hammer stands on its head at its right side, the handle in its hand
            var block = V(-0.31f, 0.12f, 0.03f);
            var half = V(0.12f, 0.12f, 0.17f);
            b.Limb(arm, block + V(0.04f, half.Y * 0.8f, 0), hand + V(-0.01f, 0.09f, 0), 0.018f, 0.016f, TinkSteelDark, Metal, 0.004f);
            ScrapHead(b, arm, block, half, true);
            // The spiked beige blade welded along its top
            var beige = Rgb(224, 204, 164);
            var blade = block + V(0, half.Y + 0.035f, -0.06f);
            b.Box(arm, blade, V(0.012f, 0.04f, 0.055f), 0.006f, beige, mat: Metal, blend: 0.004f);
            foreach (float z in new[] { -0.04f, 0f, 0.04f })
                b.Spike(arm, blade + V(0, 0.03f, z), blade + V(0, 0.075f, z - 0.01f), 0.016f, beige, 0.6f, Metal, 0.004f);
        });
        int headBone = b.Head(bc + V(0, 0.09f, 0));
        var c = bc + V(0, 0.19f, 0.01f);
        var r = V(0.1f, 0.09f, 0.09f);
        b.Limb(headBone, bc + V(0, 0.06f, 0), c + V(0, -0.06f, 0), 0.032f, 0.036f, TinkPink);
        b.Ell(headBone, c, r, TinkPink);
        // The growth like twin tails, pinkish white zigzagged darker pink, drooping over its back
        var hair = Rgb(250, 222, 232);
        var crown = c + V(0, 0.055f, -0.02f);
        b.Ell(headBone, crown, V(0.088f, 0.05f, 0.078f), hair, blend: 0.012f);
        PokeBuilder.Both(s =>
        {
            var tail = Smooth(3, crown + V(0.05f * s, 0.035f, -0.01f), crown + V(0.1f * s, 0.045f, -0.07f), crown + V(0.12f * s, -0.03f, -0.13f), crown + V(0.11f * s, -0.17f, -0.14f));
            b.Tube(headBone, tail, 0.05f, 0.036f, hair, blend: 0f);
            for (int i = 2; i < tail.Length - 1; i += 2)
            {
                var side = Vector3.Normalize(Vector3.Cross(tail[i + 1] - tail[i - 1], V(0, 0, 1f)) + V(0.0001f, 0, 0));
                b.PaintEll(headBone, tail[i] + side * 0.03f * ((i / 2) % 2 == 0 ? 1f : -1f), V(0.022f, 0.016f, 0.05f), TinkDark, soft: 0.006f);
            }
        });
        TinkFace(b, headBone, c, r, 1.2f, 8f, false, MarkShape.Smile, 0.03f);
        PokeBuilder.Both(s => b.Box(headBone, On(c, r, c.X + 0.012f * s, c.Y - 0.03f) + V(0, 0, -0.003f), V(0.007f, 0.006f, 0.004f), 0.0015f, PepperWhite, mat: Shell, blend: 0.002f));
        return b;
    }

    // ------------------------------------------------------------------ Wiglett and Wugtrio

    private static readonly Color EelWhite = Rgb(244, 242, 236);
    private static readonly Color EelRed = Rgb(214, 72, 74);
    private static readonly Color EelEye = Rgb(34, 46, 96);

    /// <summary>
    /// One garden eel rising from the ground at <paramref name="foot"/> to its head at <paramref name="top"/>, a slender
    /// body bending gently on the way, a head a little wider than it, a big round <paramref name="nose"/> at its front
    /// and two beady dark blue eyes above it. The body is on <paramref name="body"/>, the head on <paramref name="head"/>.
    /// </summary>
    private static void GardenEel(PokeBuilder b, int body, int head, Vector3 foot, Vector3 top, float r, Color color, Color nose, float lean)
    {
        var mid = Vector3.Lerp(foot, top, 0.5f);
        var path = Smooth(3, foot + V(0, -0.03f, 0), Vector3.Lerp(foot, mid, 0.5f) + V(-lean, 0, -0.006f), mid + V(lean, 0, 0.004f),
            Vector3.Lerp(mid, top, 0.6f) + V(lean * 0.4f, 0, 0.006f), top + V(0, -r * 0.8f, 0));
        b.Tube(body, path, r, r * 0.9f, color, blend: 0f);
        var hr = V(r * 1.15f, r * 1.2f, r * 1.1f);
        var hc = top + V(0, 0, r * 0.15f);
        b.Ell(head, hc, hr, color, blend: 0.012f);
        b.Ell(head, hc + V(0, -r * 0.28f, r * 1.0f), V(r * 0.55f, r * 0.48f, r * 0.4f), nose, mat: Shell, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(hc, hr, hc.X + r * 0.48f * s, hc.Y + r * 0.4f);
            b.Eye(head, at, Outward(hc, hr, at), r * 0.2f, EelEye);
        });
    }

    /// <summary>Wiglett: a long white garden eel buried in the sand up to its middle, a slender body swaying up to a head little wider than it, with a big round pink nose and two beady dark blue eyes.</summary>
    private static PokeBuilder Wiglett()
    {
        var b = new PokeBuilder("Wiglett", 0.42f, BodyPlan.Biped, V(0, 0.08f, 0)) { Coat = Scales };
        Burrow(b, 0.12f, 0.11f, 10, Rgb(228, 206, 158), 960u);
        int head = b.Head(V(0, 0.22f, 0));
        GardenEel(b, Body, head, V(0, 0, 0), V(0, 0.3f, 0.01f), 0.036f, EelWhite, Rgb(240, 150, 170), 0.01f);
        // The sand cuts it off flat, where it goes on down into the seabed
        b.CutBox(Root, V(0, -0.3f, 0), V(0.6f, 0.3f, 0.6f), Quaternion.Identity);
        return b;
    }

    /// <summary>Wugtrio: three long red garden eels rising from one black spiky rock, the tallest behind and the others before it to either side, each with a big round white nose and beady dark blue eyes.</summary>
    private static PokeBuilder Wugtrio()
    {
        var b = new PokeBuilder("Wugtrio", 0.66f, BodyPlan.Biped, V(0, 0.08f, -0.06f)) { Coat = Scales };
        var rock = Rgb(58, 56, 66);
        var rockLight = Rgb(92, 90, 102);
        // The black rock they live in, bristling with points, on the root so it stays put while they sway
        var rc = V(0, 0.04f, 0);
        var rr = V(0.25f, 0.1f, 0.21f);
        b.Ell(Root, rc, rr, rock, mat: Shell, blend: 0.02f);
        b.Ell(Root, rc + V(0.06f, 0.03f, -0.06f), V(0.15f, 0.08f, 0.12f), rock, mat: Shell, blend: 0.03f);
        var rng = new GenomeRandom(961u);
        for (int i = 0; i < 16; i++)
        {
            float a = MathF.Tau * (i + rng.Range(-0.25f, 0.25f)) / 16f;
            float up = rng.Range(0.15f, 0.7f);
            var dir = V(MathF.Sin(a) * (1f - up), up, MathF.Cos(a) * (1f - up));
            var at = Out(rc, rr, default, dir);
            var n = Vector3.Normalize((at - rc) / (rr * rr));
            var tip = at + Vector3.Normalize(n + V(0, 0.3f, 0)) * rng.Range(0.04f, 0.07f);
            b.Spike(Root, at - n * 0.01f, tip, rng.Range(0.02f, 0.03f), i % 3 == 0 ? rockLight : rock, mat: Shell, blend: 0.008f);
        }
        // The three of them, each swaying on its own
        var middle = V(0, 0, -0.06f);
        var left = V(-0.13f, 0, 0.06f);
        var right = V(0.13f, 0, 0.07f);
        int head = b.Head(middle + V(0, 0.3f, 0));
        int l = b.Part("left", Root, left + V(0, 0.08f, 0), PokeRole.Head, 1.7f, -1f);
        int r = b.Part("right", Root, right + V(0, 0.08f, 0), PokeRole.Head, 3.1f, 1f);
        GardenEel(b, Body, head, middle, middle + V(0, 0.46f, 0.01f), 0.045f, EelRed, White, 0.012f);
        GardenEel(b, l, l, left, left + V(-0.01f, 0.36f, 0.01f), 0.042f, EelRed, White, -0.012f);
        GardenEel(b, r, r, right, right + V(0.01f, 0.32f, 0.01f), 0.04f, EelRed, White, 0.012f);
        b.CutBox(Root, V(0, -0.3f, 0), V(0.8f, 0.3f, 0.8f), Quaternion.Identity);
        return b;
    }

    // ------------------------------------------------------------------ Bombirdier

    /// <summary>A stork's two long thin legs from hips <paramref name="x"/> apart at <paramref name="hip"/>, three toes forward and one back, tipped with black claws.</summary>
    private static void StorkLegs(PokeBuilder b, float x, float hip, float z, float thick, Color color, Color claw)
    {
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(x * s, hip, z));
            var knee = V(x * 1.05f * s, hip * 0.5f, z - 0.015f);
            var ankle = V(x * 1.1f * s, 0.03f, z + 0.01f);
            b.Limb(leg, V(x * s, hip, z), knee, thick * 1.2f, thick, color, Scales, 0.008f);
            b.Limb(leg, knee, ankle, thick, thick * 0.85f, color, Scales, 0.006f);
            foreach (var (dx, dz) in new[] { (-0.55f, 1f), (0f, 1.15f), (0.55f, 1f), (0f, -0.7f) })
            {
                var toe = ankle + V(dx * thick * 2.4f * s, -0.017f, dz * thick * 3.4f);
                b.Limb(leg, ankle, toe, thick * 0.6f, thick * 0.45f, color, Scales, 0.005f);
                b.Spike(leg, toe, toe + Vector3.Normalize(toe - ankle) * thick * 1.5f + V(0, -0.004f, 0), thick * 0.42f, claw, mat: Shell, blend: 0.003f);
            }
        });
    }

    /// <summary>Bombirdier: a white stork on tall thin pink legs, its wingtips, lower body and the inside of its square tail black, a long neck up to a head with slanted eyes lined in black, a long pink beak with a knob at its tip, a little fan of a crest, and a long white apron of feathers hanging from its chest past its legs.</summary>
    private static PokeBuilder Bombirdier()
    {
        var b = new PokeBuilder("Bombirdier", 0.8f, BodyPlan.Bird, V(0, 0.46f, -0.02f)) { Coat = Fur };
        var white = Rgb(244, 244, 240);
        var black = Rgb(42, 40, 48);
        var pink = Rgb(232, 116, 132);
        StorkLegs(b, 0.045f, 0.38f, -0.01f, 0.013f, pink, black);
        var bc = V(0, 0.46f, -0.02f);
        var br = V(0.1f, 0.095f, 0.15f);
        b.Ell(Body, bc, br, white, V(-10f, 0, 0));
        b.PaintEll(Body, bc + V(0, -0.075f, -0.05f), V(0.12f, 0.05f, 0.14f), black, V(-10f, 0, 0));
        PokeBuilder.Both(s => FoldedWing(b, s, bc + V(0.08f * s, 0.04f, 0.05f), bc + V(0.1f * s, 0.0f, -0.21f), 0.07f, white, black));
        // The square tail, black inside
        int tail = b.Tail(bc + V(0, 0.01f, -0.13f));
        b.Box(tail, bc + V(0, 0.0f, -0.22f), V(0.06f, 0.014f, 0.07f), 0.012f, white, V(-12f, 0, 0), blend: 0.012f);
        b.PaintEll(tail, bc + V(0, -0.025f, -0.23f), V(0.05f, 0.02f, 0.06f), black, V(-12f, 0, 0), 0.008f);
        // The apron of shed feathers hanging from its chest, well in front of its legs
        var top = bc + V(0, 0.03f, 0.12f);
        var hem = V(0, 0.1f, 0.19f);
        b.Box(Body, (top + hem) / 2f, V(0.06f, Vector3.Distance(top, hem) / 2f, 0.008f), 0.007f, white, Euler(top - hem), blend: 0.012f);
        b.Ell(Body, hem + V(0, 0.01f, 0.0f), V(0.062f, 0.016f, 0.014f), white, Euler(top - hem), blend: 0.008f);
        int head = b.Head(bc + V(0, 0.06f, 0.1f));
        var neck = Smooth(3, bc + V(0, 0.03f, 0.09f), V(0, 0.6f, 0.13f), V(0, 0.7f, 0.1f), V(0, 0.77f, 0.11f));
        b.Tube(head, neck, 0.034f, 0.028f, white, blend: 0f);
        var c = V(0, 0.8f, 0.12f);
        var r = V(0.045f, 0.045f, 0.055f);
        b.Ell(head, c, r, white);
        // The long beak, a knob at its tip
        var beakTip = c + V(0, -0.03f, 0.2f);
        b.Limb(head, c + V(0, -0.01f, 0.04f), beakTip, 0.02f, 0.01f, pink, Shell, 0.008f);
        b.Ell(head, beakTip + V(0, 0.004f, 0.004f), V(0.014f, 0.016f, 0.016f), pink, mat: Shell, blend: 0.006f);
        // The little fan of a crest at the back of its head
        foreach (var d in new[] { V(0, 0.6f, -1f), V(0.45f, 0.65f, -0.85f), V(-0.45f, 0.65f, -0.85f) })
        {
            var root = c + V(0, 0.02f, -0.035f);
            Frond(b, head, root, root + Vector3.Normalize(d) * 0.065f, 0.02f, white, V(0, 1f, 0.3f), 0.3f, Fur, 0.008f);
        }
        PokeBuilder.Both(s =>
        {
            var at = Out(c, r, default, V(0.75f * s, 0.15f, 0.6f));
            var facing = Outward(c, r, at);
            b.Eye(head, at, facing, 0.011f, Rgb(30, 30, 34), glare: true);
            // The black line drawn out behind each eye
            b.PaintEll(head, at + V(0.004f * s, 0.003f, -0.024f), V(0.009f, 0.007f, 0.02f), black, V(-15f, 0, 0), 0.005f);
        });
        return b;
    }

    // ------------------------------------------------------------------ Finizen and Palafin

    private static readonly Color DolphinBlue = Rgb(104, 186, 236);
    private static readonly Color DolphinPale = Rgb(214, 236, 250);
    private static readonly Color DolphinWater = Rgb(150, 222, 250);
    private static readonly Color DolphinEye = Rgb(72, 128, 228);
    private static readonly Color HeroIndigo = Rgb(58, 66, 156);
    private static readonly Color HeroPink = Rgb(214, 62, 128);

    /// <summary>
    /// A heart painted on <paramref name="bone"/> at <paramref name="at"/> of a surface facing <paramref name="n"/>, its
    /// point toward <c>-up</c> and <paramref name="size"/> half as wide: two round lobes and a point, edged in
    /// <paramref name="rim"/> where one is given.
    /// </summary>
    private static void PaintHeart(PokeBuilder b, int bone, Vector3 at, Vector3 n, Vector3 up, float size, Color color, Color? rim, float soft)
    {
        n = Vector3.Normalize(n);
        var u = Vector3.Normalize(up - n * Vector3.Dot(up, n));
        var x = Vector3.Cross(u, n);
        var turn = Euler(u);
        void Heart(float k, Color c)
        {
            float depth = size * 1.4f;
            PokeBuilder.Both(s => b.PaintEll(bone, at + u * size * 0.35f + x * size * 0.48f * s, V(size * 0.56f * k, size * 0.56f * k, depth), c, turn, soft));
            b.PaintEll(bone, at - u * size * 0.15f, V(size * 0.62f * k, size * 0.5f * k, depth), c, turn, soft);
            b.PaintEll(bone, at - u * size * 0.6f, V(size * 0.3f * k, size * 0.42f * k, depth), c, turn, soft);
        }
        if (rim is Color edge) Heart(1.35f, edge);
        Heart(1f, color);
    }

    /// <summary>
    /// Finizen, or Palafin in its Zero Form (<paramref name="heart"/>, a pink heart edged in white on its chest): a
    /// light blue dolphin, pale under its belly, jaw and round its eyes, pale wave marks along its sides, a short snout
    /// with a smile, a pale dorsal fin, two flippers, a split tail and a ring of water round the base of it.
    /// </summary>
    private static PokeBuilder DolphinBuild(string name, float fill, bool heart)
    {
        var b = new PokeBuilder(name, fill, BodyPlan.Fish, V(0, 0.3f, 0)) { Coat = Scales }.Hover();
        var bc = V(0, 0.3f, 0);
        var br = V(0.075f, 0.08f, 0.15f);
        b.Ell(Body, bc, br, DolphinBlue);
        b.PaintEll(Body, bc + V(0, -0.055f, 0.02f), V(0.075f, 0.045f, 0.16f), DolphinPale);
        PokeBuilder.Both(s =>
        {
            var at = Out(bc, br, default, V(s, 0.05f, -0.35f));
            b.Mark(Body, at, Outward(bc, br, at), 0.036f, 0.011f, DolphinPale, MarkShape.Wave);
        });
        if (heart)
        {
            var at = Out(bc, br, default, V(0, -0.45f, 0.85f));
            PaintHeart(b, Body, at, Outward(bc, br, at), V(0, 0, 1f), 0.024f, Rgb(246, 168, 196), Rgb(250, 250, 252), 0.005f);
        }
        int dorsal = b.Part("dorsal", Body, bc + V(0, 0.07f, -0.02f), PokeRole.Fin);
        Blade(b, dorsal, bc + V(0, 0.06f, 0.0f), bc + V(0, 0.15f, -0.08f), 0.036f, DolphinPale, V(1f, 0, 0), 0.26f);
        b.PaintEll(dorsal, bc + V(0, 0.14f, -0.07f), V(0.02f, 0.025f, 0.025f), Rgb(250, 252, 255), soft: 0.006f);
        PokeBuilder.Both(s =>
        {
            int fin = b.Part(s < 0 ? "finL" : "finR", Body, bc + V(0.06f * s, -0.04f, 0.06f), PokeRole.Fin, s, s);
            Blade(b, fin, bc + V(0.055f * s, -0.04f, 0.06f), bc + V(0.15f * s, -0.1f, 0.01f), 0.032f, DolphinBlue, V(0.35f * s, 1f, 0), 0.26f);
        });
        // The tail: a narrowing stock, its flukes spread flat, and the ring of water round it
        int tail = b.Tail(bc + V(0, 0, -0.13f));
        b.Limb(tail, bc + V(0, 0, -0.12f), bc + V(0, 0.01f, -0.21f), 0.056f, 0.032f, DolphinBlue);
        b.Torus(tail, bc + V(0, 0.005f, -0.175f), 0.044f, 0.012f, DolphinWater, V(90f, 0, 0), mat: Shell, blend: 0.004f);
        int flukes = b.Part("flukes", tail, bc + V(0, 0.01f, -0.21f), PokeRole.Fin);
        PokeBuilder.Both(s =>
        {
            var tip = bc + V(0.08f * s, 0.014f, -0.265f);
            Frond(b, flukes, bc + V(0.008f * s, 0.01f, -0.205f), tip, 0.036f, DolphinBlue, V(0, 1f, 0), 0.3f);
            b.PaintEll(flukes, tip + V(0, -0.01f, 0.01f), V(0.05f, 0.008f, 0.04f), DolphinPale, soft: 0.006f);
        });
        int head = b.Head(bc + V(0, 0.01f, 0.1f));
        var c = bc + V(0, 0.02f, 0.15f);
        var r = V(0.064f, 0.064f, 0.07f);
        b.Ell(head, c, r, DolphinBlue);
        var sc = c + V(0, -0.028f, 0.07f);
        var sr = V(0.034f, 0.024f, 0.04f);
        b.Ell(head, sc, sr, DolphinBlue, blend: 0.015f);
        b.PaintEll(head, c + V(0, -0.045f, 0.06f), V(0.055f, 0.022f, 0.08f), DolphinPale);
        b.PaintEll(head, c + V(0, 0.062f, -0.01f), V(0.008f, 0.005f, 0.008f), Rgb(40, 80, 120), soft: 0.004f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.04f * s, c.Y + 0.008f);
            b.PaintEll(head, at, V(0.022f, 0.022f, 0.022f), DolphinPale, soft: 0.006f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, DolphinEye);
        });
        var mouth = Out(sc, sr, default, V(0, -0.2f, 1f));
        b.Mark(head, mouth, Outward(sc, sr, mouth), 0.016f, 0.007f, Rgb(40, 70, 110), MarkShape.Smile);
        return Lift(b);
    }

    /// <summary>Finizen: a light blue dolphin, pale under its belly, jaw and round its eyes, pale wave marks along its sides, a smiling short snout, a pale dorsal fin, a split tail and a ring of water round the base of it.</summary>
    private static PokeBuilder Finizen() => DolphinBuild("Finizen", 0.6f, false);

    /// <summary>Palafin in its Zero Form: Finizen to the life but for a pink heart edged in white on its chest.</summary>
    private static PokeBuilder Palafin() => DolphinBuild("Palafin", 0.62f, true);

    /// <summary>
    /// Palafin in its Hero Form: a hero of a dolphin standing tall on its long flukes, broad indigo shoulders and chest
    /// under a short pale cape, its sides pale blue, a white shield on its chest and belly with a deep pink heart, white
    /// gloves on fists one raised flexing and one at its hip, a dark mask with pale lenses over its eyes, a dorsal fin
    /// striped white and three rings of water round its tail.
    /// </summary>
    private static PokeBuilder PalafinHero()
    {
        var b = new PokeBuilder("Palafin-Hero", 0.95f, BodyPlan.Biped, V(0, 0.5f, 0)) { Coat = Scales };
        var light = Rgb(150, 206, 240);
        var cape = Rgb(178, 222, 246);
        var glove = Rgb(246, 246, 250);
        var mask = Rgb(30, 32, 52);
        // The flukes it stands on, long and slender
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.03f * s, 0.12f, 0));
            Blade(b, leg, V(0.02f * s, 0.035f, -0.005f), V(0.2f * s, 0.016f, 0.09f), 0.055f, HeroIndigo, V(0, 1f, 0), 0.24f);
            b.PaintEll(leg, V(0.12f * s, 0.01f, 0.05f), V(0.09f, 0.012f, 0.05f), light, V(0, -25f * s, 0), 0.008f);
        });
        // The stock of its tail and the three rings of water round it
        b.Limb(Body, V(0, 0.04f, 0), V(0, 0.4f, 0), 0.042f, 0.08f, HeroIndigo);
        foreach (var (y, k) in new[] { (0.13f, 0.95f), (0.2f, 1.02f), (0.27f, 1.1f) })
        {
            float rad = 0.042f + (0.08f - 0.042f) * (y - 0.04f) / 0.36f;
            b.Torus(Body, V(0, y, 0), rad * k + 0.006f, 0.011f, DolphinWater, mat: Shell, blend: 0.004f);
        }
        b.Ell(Body, V(0, 0.42f, 0), V(0.095f, 0.08f, 0.08f), HeroIndigo);
        // The great chest and shoulders
        var cc = V(0, 0.58f, 0);
        b.Ell(Body, cc, V(0.15f, 0.14f, 0.1f), HeroIndigo);
        PokeBuilder.Both(s =>
        {
            b.Ell(Body, cc + V(0.06f * s, 0.035f, 0.055f), V(0.07f, 0.055f, 0.05f), HeroIndigo, blend: 0.02f);
            b.PaintEll(Body, cc + V(0.13f * s, -0.07f, 0), V(0.05f, 0.14f, 0.09f), light);
        });
        // The shield on its chest and belly, a deep pink heart in it
        b.PaintEll(Body, V(0, 0.55f, 0.1f), V(0.1f, 0.12f, 0.07f), glove, soft: 0.008f);
        b.PaintEll(Body, V(0, 0.44f, 0.08f), V(0.045f, 0.055f, 0.07f), glove, soft: 0.008f);
        PaintHeart(b, Body, V(0, 0.54f, 0.11f), V(0, 0, 1f), V(0, 1f, 0), 0.05f, HeroPink, null, 0.008f);
        // The short cape over its shoulders, a mantle round the top of its back
        PokeBuilder.Both(s => b.Ell(Body, cc + V(0.15f * s, 0.075f, -0.005f), V(0.075f, 0.05f, 0.075f), cape, V(0, 0, -20f * s), blend: 0.015f));
        b.Ell(Body, cc + V(0, 0.05f, -0.06f), V(0.17f, 0.09f, 0.06f), cape, V(-10f, 0, 0), blend: 0.015f);
        PokeBuilder.Both(s => b.Ell(Body, cc + V(0.09f * s, -0.03f, -0.075f), V(0.07f, 0.06f, 0.035f), cape, V(-10f, 0, 12f * s), blend: 0.015f));
        // Its arms, the right raised and flexing, the left fist at its hip, in white gloves
        PokeBuilder.Both(s =>
        {
            var shoulder = cc + V(0.17f * s, 0.06f, 0);
            int arm = b.Arm(s, shoulder);
            var elbow = s > 0 ? cc + V(0.29f * s, 0.02f, 0.03f) : cc + V(0.26f * s, -0.07f, -0.01f);
            var fist = s > 0 ? cc + V(0.3f * s, 0.18f, 0.06f) : cc + V(0.19f * s, -0.15f, 0.08f);
            b.Limb(arm, shoulder, elbow, 0.058f, 0.042f, HeroIndigo);
            b.Ell(arm, Vector3.Lerp(shoulder, elbow, 0.5f) + V(0, 0.015f, 0.01f), V(0.055f, 0.05f, 0.055f), HeroIndigo, blend: 0.02f);
            b.Limb(arm, elbow, fist, 0.042f, 0.038f, HeroIndigo);
            b.PaintEll(arm, Vector3.Lerp(elbow, fist, 0.75f), V(0.05f, 0.05f, 0.05f), glove, soft: 0.008f);
            b.Ell(arm, fist, V(0.05f, 0.048f, 0.05f), glove, blend: 0.012f);
        });
        // The dorsal fin, striped white and tipped white
        int dorsal = b.Part("dorsal", Body, cc + V(0, 0.08f, -0.08f), PokeRole.Fin);
        var finRoot = cc + V(0, 0.1f, -0.07f);
        var finTip = cc + V(0, 0.27f, -0.2f);
        Blade(b, dorsal, finRoot, finTip, 0.05f, HeroIndigo, V(1f, 0, 0), 0.24f);
        b.PaintEll(dorsal, Vector3.Lerp(finRoot, finTip, 0.55f), V(0.02f, 0.012f, 0.06f), glove, Euler(finTip - finRoot), 0.006f);
        b.PaintEll(dorsal, Vector3.Lerp(finRoot, finTip, 0.9f), V(0.02f, 0.03f, 0.04f), glove, soft: 0.006f);
        int head = b.Head(cc + V(0, 0.13f, 0.02f));
        var c = cc + V(0, 0.22f, 0.03f);
        var r = V(0.075f, 0.072f, 0.08f);
        b.Ell(head, c, r, HeroIndigo);
        b.Ell(head, c + V(0, -0.032f, 0.08f), V(0.04f, 0.03f, 0.045f), HeroIndigo, blend: 0.015f);
        b.PaintEll(head, c + V(0, -0.05f, 0.07f), V(0.06f, 0.024f, 0.09f), light);
        b.PaintEll(head, c + V(0, 0.07f, -0.01f), V(0.009f, 0.006f, 0.009f), Rgb(24, 26, 60), soft: 0.004f);
        // The mask across its eyes, and its lenses
        b.PaintEll(head, c + V(0, 0.014f, 0.05f), V(0.09f, 0.024f, 0.05f), mask, soft: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.032f * s, c.Y + 0.014f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, white: Rgb(252, 240, 170), pupil: Rgb(30, 40, 90), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Varoom and Revavroom

    private static readonly Color EngineSteel = Rgb(156, 160, 170);
    private static readonly Color EngineDark = Rgb(92, 94, 106);
    private static readonly Color EngineBlack = Rgb(40, 40, 48);
    private static readonly Color EnginePurple = Rgb(150, 72, 172);
    private static readonly Color EngineYellow = Rgb(250, 212, 56);

    /// <summary>Varoom: a single-cylinder engine come alive, a grey block with a yellow eye on each side, a tall fin block along its top striped black, a snout in front ending in a pale skull-like knob with a frowning hole, and a long exhaust pipe curling up behind, stained purple at its end.</summary>
    private static PokeBuilder Varoom()
    {
        var b = new PokeBuilder("Varoom", 0.55f, BodyPlan.Floating, V(0, 0.22f, 0)) { Coat = Metal }.Hover();
        var bc = V(0, 0.22f, 0);
        b.Box(Body, bc, V(0.11f, 0.09f, 0.12f), 0.04f, EngineSteel);
        b.PaintEll(Body, bc + V(0, -0.1f, 0), V(0.14f, 0.04f, 0.15f), EngineDark, soft: 0.01f);
        // The tall block along its top, four black stripes across it
        b.Box(Body, bc + V(0, 0.11f, -0.04f), V(0.065f, 0.05f, 0.12f), 0.022f, EngineSteel, blend: 0.012f);
        foreach (float z in new[] { -0.12f, -0.065f, -0.01f, 0.045f })
            b.PaintEll(Body, bc + V(0, 0.13f, z), V(0.08f, 0.06f, 0.009f), EngineBlack, soft: 0.005f);
        PokeBuilder.Both(s =>
        {
            var at = bc + V(0.11f * s, 0.0f, 0.04f);
            b.Eye(Body, at, V(s, 0, 0.2f), 0.021f, white: EngineYellow, pupil: EngineBlack, glare: true);
        });
        // The snout in front, a skull-like knob at its end with a frowning hole in it
        int head = b.Head(bc + V(0, -0.01f, 0.1f));
        b.Limb(head, bc + V(0, -0.02f, 0.08f), bc + V(0, -0.035f, 0.2f), 0.052f, 0.042f, EngineSteel);
        var kc = bc + V(0, -0.035f, 0.23f);
        b.Ell(head, kc, V(0.052f, 0.046f, 0.036f), Rgb(206, 206, 212), blend: 0.012f);
        PokeBuilder.Both(s => b.Ell(head, kc + V(0.03f * s, 0.012f, 0.012f), V(0.022f, 0.02f, 0.022f), Rgb(206, 206, 212), blend: 0.01f));
        b.Cut(head, kc + V(0, -0.018f, 0.036f), V(0.022f, 0.009f, 0.02f), V(0, 0, 0));
        b.PaintEll(head, kc + V(0, -0.016f, 0.034f), V(0.03f, 0.016f, 0.02f), EngineBlack, soft: 0.004f);
        // The exhaust pipe curling up behind
        int tail = b.Tail(bc + V(0, -0.02f, -0.11f));
        var pipe = Smooth(3, bc + V(0, -0.02f, -0.1f), bc + V(0, -0.04f, -0.2f), bc + V(0, 0.01f, -0.28f), bc + V(0, 0.1f, -0.31f));
        b.Tube(tail, pipe, 0.022f, 0.024f, EngineDark, blend: 0f);
        b.Torus(tail, pipe[^1], 0.022f, 0.009f, EngineDark, default, blend: 0.004f);
        b.PaintEll(tail, pipe[^1] + V(0, 0.01f, 0), V(0.04f, 0.022f, 0.04f), EnginePurple, soft: 0.006f);
        return Lift(b);
    }

    /// <summary>Revavroom: an engine grown into a hot rod, a broad grey block with one great yellow eye in its front, spikes to either side and a frowning hole below, a round mouth on top like an air filter split into a toothed lid and a toothed rim with a long tongue out between them, a long exhaust down each side flaring at the back, and three short cylinders leaning out from each, all stained purple at their ends.</summary>
    private static PokeBuilder Revavroom()
    {
        var b = new PokeBuilder("Revavroom", 0.9f, BodyPlan.Floating, V(0, 0.3f, 0)) { Coat = Metal }.Hover();
        var bc = V(0, 0.3f, 0);
        b.Box(Body, bc, V(0.15f, 0.11f, 0.2f), 0.05f, EngineSteel);
        b.PaintEll(Body, bc + V(0, -0.12f, 0), V(0.18f, 0.04f, 0.22f), EngineDark, soft: 0.012f);
        // The mouth on top: a toothed rim, a dark throat and a long tongue
        var rim = bc + V(0, 0.14f, -0.01f);
        b.Torus(Body, rim, 0.1f, 0.032f, EngineSteel, blend: 0.012f);
        b.PaintEll(Body, rim + V(0, -0.03f, 0), V(0.085f, 0.03f, 0.085f), EngineBlack, soft: 0.008f);
        for (int i = 0; i < 5; i++)
        {
            float a = (-60f + 30f * i) * Degree;
            var at = rim + V(MathF.Sin(a) * 0.1f, 0.022f, MathF.Cos(a) * 0.1f);
            b.Spike(Body, at, at + V(0, 0.034f, 0), 0.014f, Claw, 0.6f, Shell, 0.004f);
        }
        b.Tube(Body, Smooth(3, rim + V(0, 0.0f, -0.02f), rim + V(0, 0.04f, 0.07f), rim + V(0, 0.01f, 0.15f), rim + V(0, -0.04f, 0.2f)), 0.024f, 0.02f,
            Rgb(208, 92, 176), Scales, 0f);
        // The lid above it, joined to the rim at the back
        int jaw = b.Jaw(Body, rim + V(0, 0.04f, -0.1f));
        var lid = rim + V(0, 0.1f, 0);
        b.Limb(jaw, rim + V(0, 0.01f, -0.105f), lid + V(0, 0, -0.105f), 0.03f, 0.03f, EngineSteel);
        b.Torus(jaw, lid, 0.1f, 0.032f, EngineSteel, blend: 0.012f);
        b.Ell(jaw, lid + V(0, 0.022f, 0), V(0.09f, 0.03f, 0.09f), EngineDark, blend: 0.01f);
        for (int i = 0; i < 4; i++)
        {
            float a = (-45f + 30f * i) * Degree;
            var at = lid + V(MathF.Sin(a) * 0.1f, -0.022f, MathF.Cos(a) * 0.1f);
            b.Spike(jaw, at, at + V(0, -0.032f, 0), 0.014f, Claw, 0.6f, Shell, 0.004f);
        }
        // Its face in front: one great eye, spikes to either side and a frowning hole below
        int head = b.Head(bc + V(0, 0, 0.15f));
        var fc = bc + V(0, -0.005f, 0.19f);
        b.Ell(head, fc, V(0.12f, 0.09f, 0.05f), EngineDark, blend: 0.015f);
        PokeBuilder.Both(s =>
        {
            foreach (float y in new[] { 0.035f, -0.035f })
                b.Spike(head, fc + V(0.1f * s, y, 0.0f), fc + V(0.21f * s, y * 1.4f, 0.05f), 0.026f, EngineSteel, 0.6f);
        });
        b.Cut(head, fc + V(0, -0.06f, 0.05f), V(0.03f, 0.011f, 0.025f));
        b.Eye(head, fc + V(0, 0.015f, 0.05f), V(0, 0.05f, 1f), 0.032f, white: EngineYellow, pupil: EngineBlack, glare: true);
        // The cylinders along each side: a long exhaust flaring at the back and three short ones leaning out over it
        PokeBuilder.Both(s =>
        {
            int side = b.Arm(s, bc + V(0.15f * s, -0.04f, 0));
            var front = bc + V(0.14f * s, -0.07f, 0.13f);
            var back = bc + V(0.21f * s, -0.08f, -0.3f);
            b.Limb(side, front, back, 0.034f, 0.05f, EngineSteel);
            b.Torus(side, back, 0.045f, 0.012f, EngineSteel, V(90f, 0, 0), blend: 0.006f);
            b.PaintEll(side, back, V(0.06f, 0.06f, 0.015f), EnginePurple, soft: 0.006f);
            foreach (float z in new[] { 0.09f, -0.03f, -0.15f })
            {
                var root = bc + V(0.13f * s, -0.03f, z);
                var end = bc + V(0.25f * s, 0.07f, z - 0.03f);
                b.Limb(side, root, end, 0.028f, 0.028f, EngineSteel);
                b.PaintEll(side, end, V(0.032f, 0.032f, 0.032f), EnginePurple, soft: 0.006f);
            }
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Cyclizar

    /// <summary>Cyclizar: a long green lizard running on two strong legs, its little forelegs held up, a black neck and a long black tail tipped white, a dark seat on its rump like a motorcycle's, a black tire of a sac at its throat with white inside, and a black head with a crest swept back, green along its side, round orange cheeks and yellow eyes.</summary>
    private static PokeBuilder Cyclizar()
    {
        var b = new PokeBuilder("Cyclizar", 0.85f, BodyPlan.Biped, V(0, 0.4f, -0.04f)) { Coat = Scales };
        var green = Rgb(98, 178, 82);
        var pale = Rgb(170, 214, 120);
        var black = Rgb(40, 40, 46);
        var orange = Rgb(244, 140, 52);
        var seat = Rgb(64, 58, 62);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.34f, -0.12f));
            var knee = V(0.11f * s, 0.2f, -0.05f);
            var ankle = V(0.1f * s, 0.065f, -0.13f);
            b.Ell(leg, V(0.09f * s, 0.3f, -0.1f), V(0.058f, 0.08f, 0.085f), green, V(20f, 0, 0));
            b.Limb(leg, V(0.09f * s, 0.3f, -0.1f), knee, 0.05f, 0.036f, green);
            b.Limb(leg, knee, ankle, 0.034f, 0.026f, green);
            b.Ell(leg, ankle + V(0, -0.038f, 0.04f), V(0.034f, 0.026f, 0.06f), black, blend: 0.015f);
            foreach (float dx in new[] { -0.018f, 0f, 0.018f })
            {
                var toe = ankle + V(dx * s, -0.048f, 0.09f);
                b.Spike(leg, toe, toe + V(dx * 0.4f * s, -0.006f, 0.03f), 0.01f, black, mat: Shell, blend: 0.004f);
            }
        });
        var bc = V(0, 0.4f, -0.04f);
        var br = V(0.1f, 0.095f, 0.25f);
        b.Ell(Body, bc, br, green, V(-8f, 0, 0));
        b.PaintEll(Body, bc + V(0, -0.075f, 0.04f), V(0.085f, 0.04f, 0.22f), pale, V(-8f, 0, 0));
        // The seat on its rump
        var seatAt = bc + V(0, 0.08f, -0.13f);
        b.Ell(Body, seatAt, V(0.075f, 0.032f, 0.11f), seat, V(-4f, 0, 0), Shell, 0.012f);
        b.PaintTorus(Body, seatAt, 0.075f, 0.008f, Rgb(120, 112, 116), V(-4f, 0, 0), 1f, 1.4f);
        int tail = b.Tail(bc + V(0, 0, -0.22f));
        var tp = Smooth(3, bc + V(0, 0.0f, -0.21f), bc + V(0, -0.02f, -0.37f), bc + V(0, -0.1f, -0.54f), bc + V(0, -0.15f, -0.7f));
        b.Tube(tail, tp, 0.062f, 0.018f, black, blend: 0f);
        b.PaintEll(tail, tp[^1] + V(0, 0, 0.02f), V(0.04f, 0.04f, 0.06f), White, soft: 0.01f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.07f * s, 0.0f, 0.18f));
            var paw = bc + V(0.095f * s, -0.08f, 0.26f);
            b.Limb(arm, bc + V(0.07f * s, 0.0f, 0.18f), paw, 0.026f, 0.019f, green);
            b.Ell(arm, paw + V(0, -0.008f, 0.008f), V(0.021f, 0.017f, 0.023f), black, blend: 0.008f);
        });
        int head = b.Head(bc + V(0, 0.06f, 0.22f));
        var turn = V(-8f, 0, 0);
        var c = bc + V(0, 0.2f, 0.4f);
        var r = V(0.068f, 0.06f, 0.11f);
        b.Limb(head, bc + V(0, 0.04f, 0.2f), c + V(0, -0.02f, -0.07f), 0.062f, 0.05f, black);
        // The tire of a sac at its throat
        var sac = bc + V(0, 0.03f, 0.3f);
        b.Torus(head, sac, 0.052f, 0.022f, black, V(0, 0, 90f), blend: 0.008f);
        b.Ell(head, sac, V(0.016f, 0.045f, 0.045f), White, blend: 0.004f);
        b.Ell(head, c, r, black, turn);
        b.Ell(head, c + V(0, -0.034f, 0.035f), V(0.058f, 0.03f, 0.095f), black, blend: 0.015f);
        // The crest swept back, and the green along each side between it and the jaw
        Blade(b, head, c + V(0, 0.04f, -0.04f), c + V(0, 0.085f, -0.21f), 0.05f, black, V(1f, 0, 0), 0.4f);
        b.PaintEll(head, c + V(0, 0.0f, 0.0f), V(0.085f, 0.019f, 0.1f), green, turn, 0.006f);
        PokeBuilder.Both(s =>
        {
            b.PaintEll(head, c + V(0.06f * s, -0.028f, -0.025f), V(0.03f, 0.027f, 0.03f), orange, soft: 0.006f);
            var at = Out(c, r, turn, V(0.55f * s, 0.45f, 0.5f));
            b.Eye(head, at, Outward(c, r, at), 0.014f, sclera: true, white: Rgb(252, 214, 70), pupil: black);
        });
        return b;
    }

    // ------------------------------------------------------------------ Orthworm

    /// <summary>Orthworm: a great earthworm of salmon-red flesh sheathed in bands of steel, lying in a low wave with its round head raised, two little black eyes over a small mouth, and three pairs of red cables on its back tipped with steel plugs.</summary>
    private static PokeBuilder Orthworm()
    {
        var flesh = Rgb(226, 110, 98);
        var steel = Rgb(188, 192, 204);
        var cable = Rgb(208, 72, 70);
        var points = Smooth(2, V(0.12f, 0.06f, -0.4f), V(-0.07f, 0.07f, -0.27f), V(0.05f, 0.08f, -0.12f), V(0, 0.1f, 0.02f), V(0, 0.2f, 0.11f), V(0, 0.32f, 0.13f));
        var b = new PokeBuilder("Orthworm", 1f, BodyPlan.Serpent, points[0]) { Coat = Scales };
        Func<float, float> radius = t => 0.058f + 0.04f * MathF.Min(1f, t * 1.6f);
        int neck = Coils(b, points, radius, flesh);
        int BoneAt(int i) => i < 2 ? Body : b.Model.Skeleton.Find("seg" + i / 2);
        // The steel bands it is sheathed in, one round each point of its body
        for (int i = 1; i < points.Length - 1; i++)
        {
            var d = points[i + 1] - points[i - 1];
            float rr = radius(i / (float)(points.Length - 1));
            b.Torus(BoneAt(i), points[i], rr * 0.95f, rr * 0.2f, steel, Euler(d), mat: Metal, blend: 0.006f);
        }
        b.Ell(Body, points[0] + V(-0.02f, 0, -0.03f), V(0.05f, 0.05f, 0.05f), steel, mat: Metal, blend: 0.02f);
        // Three pairs of cables along its back, curling out and back, each ending in a steel plug
        foreach (int i in new[] { 4, 6, 8 })
        {
            float rr = radius(i / (float)(points.Length - 1));
            var root = points[i] + V(0, rr * 0.8f, 0);
            PokeBuilder.Both(s =>
            {
                float k = 1f - (i - 4) * 0.08f;
                var path = Smooth(3, root + V(0.02f * s, 0, 0), root + V(0.07f * s, 0.09f, -0.01f) * k, root + V(0.15f * s, 0.13f, -0.05f) * k,
                    root + V(0.2f * s, 0.09f, -0.09f) * k);
                b.Tube(BoneAt(i), path, 0.019f, 0.013f, cable, blend: 0f);
                b.Ell(BoneAt(i), path[^1], V(0.022f, 0.022f, 0.022f), steel, mat: Metal, blend: 0.004f);
            });
        }
        // The head raised at the front, a round face banded with steel at its neck
        int head = b.Head(points[^1], neck);
        var c = points[^1] + V(0, 0.06f, 0.04f);
        var r = V(0.1f, 0.095f, 0.1f);
        b.Ell(head, c, r, flesh);
        b.Torus(head, c + V(0, -0.02f, -0.06f), 0.09f, 0.026f, steel, V(55f, 0, 0), mat: Metal, blend: 0.008f);
        b.Torus(head, c + V(0, 0.01f, 0.065f), 0.06f, 0.012f, steel, V(70f, 0, 0), mat: Metal, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.032f * s, c.Y + 0.025f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(30, 28, 34));
        });
        var mouth = On(c, r, c.X, c.Y - 0.022f);
        b.Mark(head, mouth, Outward(c, r, mouth), 0.016f, 0.012f, Rgb(110, 40, 46));
        return b;
    }

    // ------------------------------------------------------------------ Glimmet and Glimmora

    private static readonly Color CrystalTeal = Rgb(104, 210, 222);
    private static readonly Color CrystalPale = Rgb(176, 238, 238);
    private static readonly Color CrystalPink = Rgb(234, 104, 176);
    private static readonly Color OreNavy = Rgb(54, 52, 98);

    /// <summary>A closed bud of <paramref name="count"/> crystal petals standing from a ring <paramref name="r"/> round <paramref name="at"/> to tips that meet <paramref name="height"/> above it, the tips in <paramref name="tip"/>.</summary>
    private static void CrystalBud(PokeBuilder b, int bone, Vector3 at, float r, float height, int count, Color crystal, Color tip, float turn = 0f)
    {
        // A cone of crystal filling the bud just inside the petals' outer faces, or petals meeting all round would shut a hollow in it
        b.Limb(bone, at, at + V(0, height * 0.9f, 0), r * 0.95f, r * 0.25f, crystal, blend: 0.004f);
        for (int i = 0; i < count; i++)
        {
            float a = turn + i * MathF.Tau / count;
            var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
            // Narrow enough to leave gaps between them: petals closed all round would shut a hollow inside the bud
            Blade(b, bone, at + dir * r, at + dir * r * 0.2f + V(0, height, 0), r * 3.1f / count, crystal, dir, 0.4f, Shell);
        }
        b.PaintEll(bone, at + V(0, height * 0.86f, 0), V(r * 0.7f, height * 0.26f, r * 0.7f), tip, soft: r * 0.25f);
    }

    /// <summary>One crystal petal on a bone of its own from <paramref name="root"/> to <paramref name="tip"/>, its face looking along <paramref name="facing"/>, pink at its heart.</summary>
    private static void CrystalPetal(PokeBuilder b, int bone, Vector3 root, Vector3 tip, float width, Color crystal, Color heart, Vector3 facing)
    {
        var d = tip - root;
        Frond(b, bone, root, tip - d * 0.15f, width, crystal, facing, 0.25f, Shell);
        Blade(b, bone, root + d * 0.5f, tip, width * 0.55f, crystal, facing, 0.4f, Shell);
        b.PaintEll(bone, root + d * 0.32f, V(width * 0.55f, d.Length() * 0.3f, width * 0.55f), heart, Euler(d), width * 0.25f);
    }

    /// <summary>Glimmet: a little bud of ore, a round navy body with two yellow eyes under a closed bud of teal crystal petals tipped pink, two crystal nubs at its sides and a pink crystal point beneath.</summary>
    private static PokeBuilder Glimmet()
    {
        var b = new PokeBuilder("Glimmet", 0.45f, BodyPlan.Floating, V(0, 0.18f, 0)) { Coat = Shell }.Hover();
        var bc = V(0, 0.18f, 0);
        var br = V(0.072f, 0.066f, 0.068f);
        b.Ell(Body, bc, br, OreNavy);
        b.Spike(Body, bc + V(0, -0.04f, 0), bc + V(0, -0.14f, 0.01f), 0.032f, CrystalPink, mat: Shell);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.06f * s, -0.01f, 0));
            b.Spike(arm, bc + V(0.05f * s, -0.01f, 0), bc + V(0.11f * s, -0.04f, 0.01f), 0.022f, CrystalTeal, 0.6f, Shell);
        });
        int head = b.Head(bc + V(0, 0.04f, 0));
        CrystalBud(b, head, bc + V(0, 0.045f, -0.008f), 0.048f, 0.13f, 5, CrystalTeal, CrystalPink, 0.3f);
        PokeBuilder.Both(s =>
        {
            var at = On(bc, br, 0.028f * s, bc.Y - 0.008f);
            b.Eye(Body, at, Outward(bc, br, at), 0.014f, Rgb(250, 214, 96));
        });
        return Lift(b);
    }

    /// <summary>
    /// Glimmora and its Mega form. Glimmora is a great flower of ore floating upside down: a navy body with two yellow
    /// eyes, a closed bud of crystal standing on its head, and a skirt of five broad teal crystal petals, pink at their
    /// hearts, spreading out and down round it over five smaller pale ones, a pink crystal point hanging beneath. The Mega
    /// Glimmora's flower is in full bloom: seven longer petals over seven, a ruff of crystal points round its neck, and a
    /// taller crown round a glowing pink gem.
    /// </summary>
    private static PokeBuilder GlimmoraBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Glimmora-Mega" : "Glimmora", mega ? 1f : 0.9f, BodyPlan.Floating, V(0, 0.42f, 0)) { Coat = Shell }.Hover();
        var bc = V(0, 0.42f, 0);
        var br = V(0.095f, 0.105f, 0.09f);
        b.Ell(Body, bc, br, OreNavy);
        b.Ell(Body, bc + V(0, -0.12f, 0), V(0.05f, 0.06f, 0.05f), OreNavy, blend: 0.02f);
        b.Spike(Body, bc + V(0, -0.15f, 0), bc + V(0, -0.3f, 0.01f), 0.04f, CrystalPink, mat: Shell);
        // The skirt of petals: broad outer ones, and smaller pale ones between them
        int count = mega ? 7 : 5;
        float length = mega ? 0.36f : 0.3f;
        for (int i = 0; i < count; i++)
        {
            float a = i * MathF.Tau / count;
            var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = bc + dir * 0.06f + V(0, -0.06f, 0);
            var tip = bc + dir * (length + 0.02f) + V(0, mega ? -0.16f : -0.2f, 0);
            int petal = b.Part("petal" + i, Body, root, PokeRole.Leaf, i * 0.7f, MathF.Sign(dir.X));
            CrystalPetal(b, petal, root, tip, mega ? 0.06f : 0.07f, CrystalTeal, CrystalPink, Vector3.Normalize(dir * 0.5f + V(0, 1f, 0)));
            if (mega)
                Blade(b, petal, tip - (tip - root) * 0.08f, tip + V(dir.X * 0.06f, 0.07f, dir.Z * 0.06f), 0.028f, CrystalPale, dir, 0.4f, Shell);
            float a2 = a + MathF.PI / count;
            var dir2 = V(MathF.Sin(a2), 0, MathF.Cos(a2));
            var root2 = bc + dir2 * 0.05f + V(0, -0.03f, 0);
            Blade(b, Body, root2, bc + dir2 * (mega ? 0.25f : 0.2f) + V(0, -0.08f, 0), 0.05f, CrystalPale, Vector3.Normalize(dir2 * 0.4f + V(0, 1f, 0)), 0.3f, Shell);
        }
        int head = b.Head(bc + V(0, 0.08f, 0));
        if (mega)
        {
            // A ruff of crystal points round its neck, and a crown round a glowing pink gem
            for (int i = 0; i < 9; i++)
            {
                float a = i * MathF.Tau / 9f + 0.35f;
                if (MathF.Cos(a) > 0.8f) continue;
                var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
                b.Spike(head, bc + dir * 0.07f + V(0, 0.07f, 0), bc + dir * 0.15f + V(0, 0.15f, 0), 0.022f, CrystalPale, 0.6f, Shell);
            }
            b.Ell(head, bc + V(0, 0.36f, -0.005f), V(0.04f, 0.05f, 0.04f), CrystalPink, mat: Glow, blend: 0.01f);
            CrystalBud(b, head, bc + V(0, 0.08f, -0.005f), 0.075f, 0.27f, 7, CrystalTeal, CrystalPink, 0.2f);
        }
        else CrystalBud(b, head, bc + V(0, 0.08f, -0.005f), 0.07f, 0.19f, 5, CrystalTeal, CrystalPink, 0.3f);
        PokeBuilder.Both(s =>
        {
            var at = On(bc, br, 0.036f * s, bc.Y + 0.0f);
            b.Eye(Body, at, Outward(bc, br, at), 0.017f, Rgb(250, 214, 96), glare: mega);
        });
        return Lift(b);
    }

    private static PokeBuilder Glimmora() => GlimmoraBuild(false);

    private static PokeBuilder GlimmoraMega() => GlimmoraBuild(true);

    // ------------------------------------------------------------------ Greavard and Houndstone

    private static readonly Color GhostWhite = Rgb(244, 242, 248);
    private static readonly Color GhostShade = Rgb(206, 200, 226);
    private static readonly Color CandleWax = Rgb(250, 244, 222);
    private static readonly Color GhostNose = Rgb(228, 76, 80);

    /// <summary>A candle standing on a head from <paramref name="at"/>, <paramref name="height"/> tall, a drip of wax down its side and a flame flickering on a bone of its own at its top.</summary>
    private static void HeadCandle(PokeBuilder b, int head, Vector3 at, float height, float r, Color rim, Color heart)
    {
        var top = at + V(0, height, 0);
        b.Limb(head, at, top, r, r * 0.95f, CandleWax, blend: 0.01f);
        b.Ell(head, at + V(r * 0.7f, height * 0.6f, r * 0.4f), V(r * 0.45f, r * 0.9f, r * 0.45f), CandleWax, blend: r * 0.4f);
        int flame = b.Part("flame", head, top, PokeRole.Flame);
        FlameTongue(b, flame, top - V(0, r * 0.3f, 0), top + V(0, r * 4.5f, -r * 0.5f), r * 1.1f, rim, heart);
    }

    /// <summary>Greavard: a little white ghost dog of wavy fluff on stubby legs, floppy ears, round black eyes, a red nose, a candle burning on its head and a wisp of a tail curling up.</summary>
    private static PokeBuilder Greavard()
    {
        var b = new PokeBuilder("Greavard", 0.5f, BodyPlan.Quadruped, V(0, 0.13f, -0.03f)) { Coat = Fur };
        StubbyLegs(b, 0.055f, 0.09f, 0.04f, -0.1f, 0.028f, GhostWhite, GhostShade);
        var bc = V(0, 0.13f, -0.03f);
        var br = V(0.078f, 0.07f, 0.11f);
        b.Ell(Body, bc, br, GhostWhite);
        FurTufts(b, Body, bc, br, 18, 0.03f, 0.016f, GhostWhite, 0.6f, -0.35f, 0.35f);
        int tail = b.Tail(bc + V(0, 0.03f, -0.1f));
        var tp = Smooth(3, bc + V(0, 0.03f, -0.09f), bc + V(0, 0.08f, -0.16f), bc + V(0, 0.15f, -0.16f), bc + V(0.02f, 0.2f, -0.12f));
        b.Tube(tail, tp, 0.024f, 0.009f, GhostWhite, blend: 0f);
        b.PaintEll(tail, tp[^1], V(0.03f, 0.045f, 0.03f), GhostShade, soft: 0.01f);
        int head = b.Head(bc + V(0, 0.05f, 0.09f));
        var c = bc + V(0, 0.1f, 0.12f);
        var r = V(0.072f, 0.066f, 0.066f);
        b.Limb(head, bc + V(0, 0.03f, 0.07f), c + V(0, -0.02f, -0.03f), 0.045f, 0.045f, GhostWhite);
        b.Ell(head, c, r, GhostWhite);
        b.Ell(head, c + V(0, -0.024f, 0.052f), V(0.038f, 0.03f, 0.032f), GhostWhite, blend: 0.015f);
        b.Ell(head, c + V(0, -0.01f, 0.085f), V(0.014f, 0.011f, 0.01f), GhostNose, blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.06f * s, 0.03f, -0.01f));
            b.Ell(ear, c + V(0.078f * s, -0.025f, -0.012f), V(0.024f, 0.064f, 0.036f), GhostWhite, V(0, 0, 12f * s));
            b.PaintEll(ear, c + V(0.078f * s, -0.07f, -0.012f), V(0.03f, 0.025f, 0.04f), GhostShade, soft: 0.01f);
            var at = On(c, r, c.X + 0.03f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.015f, Rgb(36, 34, 44));
        });
        HeadCandle(b, head, c + V(0, 0.05f, -0.01f), 0.055f, 0.016f, Rgb(250, 150, 60), Rgb(255, 236, 140));
        return b;
    }

    /// <summary>Houndstone: a great long white ghost dog, a grey gravestone carved with a cross standing on its back, its fur hanging round it like a sheet, long ears falling past its jaw, sleepy eyes, a red nose, a candle's flame on its brow and a long wisp of a tail.</summary>
    private static PokeBuilder Houndstone()
    {
        var b = new PokeBuilder("Houndstone", 0.95f, BodyPlan.Quadruped, V(0, 0.3f, -0.04f)) { Coat = Fur };
        var stone = Rgb(150, 152, 166);
        var carving = Rgb(88, 88, 104);
        StubbyLegs(b, 0.1f, 0.22f, 0.15f, -0.22f, 0.05f, GhostWhite, GhostShade);
        var bc = V(0, 0.3f, -0.04f);
        var br = V(0.12f, 0.11f, 0.27f);
        b.Ell(Body, bc, br, GhostWhite);
        Shag(b, Body, bc, br, bc.Y - 0.03f, 22, 0.08f, 0.032f, GhostWhite, 0.85f);
        // The gravestone on its back, rounded at the top and carved with a cross on both faces
        var g = bc + V(0, 0.14f, -0.07f);
        b.Box(Body, g, V(0.1f, 0.08f, 0.036f), 0.012f, stone, mat: Shell, blend: 0.02f);
        b.Ell(Body, g + V(0, 0.08f, 0), V(0.1f, 0.07f, 0.036f), stone, mat: Shell, blend: 0f);
        foreach (float f in new[] { -1f, 1f })
        {
            var face = g + V(0, 0.06f, 0.037f * f);
            b.Mark(Body, face, V(0, 0, f), 0.011f, 0.045f, carving, MarkShape.Bar);
            b.Mark(Body, face + V(0, 0.02f, 0), V(0, 0, f), 0.032f, 0.009f, carving, MarkShape.Bar);
        }
        int tail = b.Tail(bc + V(0, 0.05f, -0.25f));
        var tp = Smooth(3, bc + V(0, 0.04f, -0.24f), bc + V(0, 0.1f, -0.36f), bc + V(0.03f, 0.22f, -0.4f), bc + V(0.06f, 0.3f, -0.34f));
        b.Tube(tail, tp, 0.04f, 0.012f, GhostWhite, blend: 0f);
        b.PaintEll(tail, tp[^1], V(0.05f, 0.07f, 0.05f), GhostShade, soft: 0.015f);
        int head = b.Head(bc + V(0, 0.1f, 0.22f));
        var c = bc + V(0, 0.17f, 0.3f);
        var r = V(0.085f, 0.08f, 0.085f);
        b.Limb(head, bc + V(0, 0.05f, 0.2f), c + V(0, -0.02f, -0.04f), 0.07f, 0.065f, GhostWhite);
        b.Ell(head, c, r, GhostWhite);
        b.Ell(head, c + V(0, -0.03f, 0.07f), V(0.05f, 0.04f, 0.05f), GhostWhite, blend: 0.02f);
        b.Ell(head, c + V(0, -0.012f, 0.118f), V(0.017f, 0.013f, 0.011f), GhostNose, blend: 0.005f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.07f * s, 0.03f, -0.01f));
            b.Ell(ear, c + V(0.092f * s, -0.07f, -0.015f), V(0.03f, 0.11f, 0.05f), GhostWhite, V(0, 0, 8f * s));
            b.PaintEll(ear, c + V(0.098f * s, -0.15f, -0.015f), V(0.04f, 0.04f, 0.06f), GhostShade, soft: 0.015f);
            var at = On(c, r, c.X + 0.036f * s, c.Y + 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.015f, Rgb(54, 50, 66), glare: true);
        });
        HeadCandle(b, head, c + V(0, 0.065f, 0.0f), 0.03f, 0.02f, Rgb(250, 150, 60), Rgb(255, 236, 140));
        return b;
    }

    // ------------------------------------------------------------------ Flamigo

    /// <summary>Flamigo: a pink flamingo standing on one long leg with the other tucked up under it, a long neck curved in an S, a scowling face masked deep red round yellow eyes, a pale beak bent down to a black tip, red wings tipped dark and short tail feathers.</summary>
    private static PokeBuilder Flamigo()
    {
        var b = new PokeBuilder("Flamigo", 0.9f, BodyPlan.Bird, V(0, 0.62f, -0.02f)) { Coat = Fur };
        var pink = Rgb(246, 156, 174);
        var red = Rgb(226, 82, 110);
        var dark = Rgb(116, 40, 62);
        var leg = Rgb(232, 104, 126);
        var bc = V(0, 0.62f, -0.02f);
        var br = V(0.1f, 0.095f, 0.15f);
        // The leg it stands on, bending back at its ankle, and the other folded up under its body
        int stand = b.Leg(1f, V(0.03f, 0.57f, -0.02f));
        var hip = V(0.03f, 0.57f, -0.02f);
        var joint = V(0.035f, 0.3f, -0.045f);
        var ankle = V(0.035f, 0.03f, 0.0f);
        b.Limb(stand, hip, joint, 0.015f, 0.012f, leg, Scales, 0.008f);
        b.Ell(stand, joint, V(0.015f, 0.015f, 0.015f), leg, mat: Scales, blend: 0.006f);
        b.Limb(stand, joint, ankle, 0.012f, 0.011f, leg, Scales, 0.008f);
        b.Ell(stand, ankle + V(0, -0.018f, 0.03f), V(0.032f, 0.008f, 0.042f), leg, mat: Scales, blend: 0.01f);
        int tuck = b.Leg(-1f, V(-0.03f, 0.57f, -0.02f));
        var knee = V(-0.04f, 0.44f, 0.06f);
        var foot = V(-0.035f, 0.5f, -0.1f);
        b.Limb(tuck, V(-0.03f, 0.57f, -0.02f), knee, 0.015f, 0.012f, leg, Scales, 0.008f);
        b.Ell(tuck, knee, V(0.015f, 0.015f, 0.015f), leg, mat: Scales, blend: 0.006f);
        b.Limb(tuck, knee, foot, 0.012f, 0.011f, leg, Scales, 0.008f);
        b.Ell(tuck, foot + V(0, -0.012f, -0.012f), V(0.026f, 0.01f, 0.032f), leg, V(-40f, 0, 0), Scales, 0.01f);
        b.Ell(Body, bc, br, pink);
        b.PaintEll(Body, bc + V(0, -0.06f, 0), V(0.08f, 0.04f, 0.12f), PixelCanvas.Mix(pink, White, 0.3f));
        PokeBuilder.Both(s => FoldedWing(b, s, bc + V(0.085f * s, 0.045f, 0.06f), bc + V(0.1f * s, 0.01f, -0.19f), 0.075f, red, dark));
        int tail = b.Tail(bc + V(0, 0.01f, -0.12f));
        TailFan(b, tail, bc + V(0, 0.01f, -0.13f), 3, 40f, 0.09f, 0.022f, red, dark, 0.3f);
        // The long neck in an S up to a small head
        b.Tube(Body, Smooth(3, bc + V(0, 0.03f, 0.1f), V(0, 0.8f, 0.14f), V(0, 0.92f, 0.07f), V(0, 1.0f, 0.12f)), 0.042f, 0.03f, pink, blend: 0.01f);
        int head = b.Head(V(0, 1.0f, 0.12f));
        var c = V(0, 1.03f, 0.14f);
        var r = V(0.05f, 0.048f, 0.058f);
        b.Ell(head, c, r, pink);
        // A pale beak bent sharply down, its tip black
        var beak = Smooth(3, c + V(0, -0.005f, 0.04f), c + V(0, -0.005f, 0.095f), c + V(0, -0.035f, 0.125f), c + V(0, -0.08f, 0.12f));
        b.Tube(head, beak, 0.022f, 0.007f, Rgb(246, 236, 220), Shell, 0f);
        b.PaintEll(head, c + V(0, -0.07f, 0.123f), V(0.02f, 0.026f, 0.02f), Rgb(40, 36, 44), soft: 0.006f);
        b.PaintEll(head, c + V(0, 0.012f, 0.02f), V(0.045f, 0.022f, 0.04f), red, soft: 0.008f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, c.Y + 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(250, 206, 70), glare: true);
        });
        // A short crest of feathers swept back from its crown
        Blade(b, head, c + V(0, 0.03f, -0.01f), c + V(0, 0.06f, -0.07f), 0.016f, red, V(1f, 0, 0), 0.3f);
        return b;
    }

    // ------------------------------------------------------------------ Cetoddle and Cetitan

    private static readonly Color WhaleWhite = Rgb(244, 246, 250);
    private static readonly Color WhaleBlue = Rgb(184, 202, 228);
    private static readonly Color WhalePink = Rgb(240, 180, 190);
    private static readonly Color IceBlue = Rgb(150, 212, 240);

    /// <summary>A whale's tail on <paramref name="bone"/>: a short stalk back from <paramref name="root"/> to two flukes spread flat.</summary>
    private static void Flukes(PokeBuilder b, int bone, Vector3 root, float size, Color color)
    {
        var end = root + V(0, size * 0.25f, -size);
        b.Limb(bone, root, end, size * 0.35f, size * 0.2f, color);
        PokeBuilder.Both(s => Frond(b, bone, end + V(0, 0, size * 0.1f), end + V(size * 0.75f * s, size * 0.1f, -size * 0.35f), size * 0.28f, color, V(0, 1f, 0), 0.28f));
    }

    /// <summary>Cetoddle: a round little calf of a whale walking on four stubby legs, white with a pale blue back and feet, a broad head with small dark eyes, a pink chin, a nub of ice on its brow and a small fluked tail.</summary>
    private static PokeBuilder Cetoddle()
    {
        var b = new PokeBuilder("Cetoddle", 0.55f, BodyPlan.Quadruped, V(0, 0.15f, -0.03f)) { Coat = Fur };
        StubbyLegs(b, 0.07f, 0.1f, 0.05f, -0.1f, 0.034f, WhaleWhite, WhaleBlue);
        var bc = V(0, 0.15f, -0.03f);
        b.Ell(Body, bc, V(0.1f, 0.095f, 0.13f), WhaleWhite);
        b.PaintEll(Body, bc + V(0, 0.085f, -0.04f), V(0.07f, 0.035f, 0.1f), WhaleBlue);
        int tail = b.Tail(bc + V(0, 0.02f, -0.12f));
        Flukes(b, tail, bc + V(0, 0.02f, -0.11f), 0.09f, WhaleWhite);
        int head = b.Head(bc + V(0, 0.04f, 0.1f));
        var c = bc + V(0, 0.06f, 0.13f);
        var r = V(0.09f, 0.08f, 0.08f);
        b.Ell(head, c, r, WhaleWhite, blend: 0.03f);
        b.PaintEll(head, c + V(0, -0.055f, 0.055f), V(0.07f, 0.03f, 0.05f), WhalePink);
        b.Spike(head, c + V(0, 0.065f, 0.03f), c + V(0, 0.105f, 0.06f), 0.02f, IceBlue, mat: Shell);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.045f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(40, 40, 50));
        });
        var mouth = On(c, r, c.X, c.Y - 0.035f);
        b.Mark(head, mouth, Outward(c, r, mouth), 0.03f, 0.01f, Rgb(170, 100, 116), MarkShape.Smile);
        return b;
    }

    /// <summary>Cetitan: a great land whale on four pillar legs, white banded pale blue over its back, a long heavy head with small fierce eyes and a pink jaw, a great horn of blue ice rising from its snout with smaller shards beside it, and a fluked tail.</summary>
    private static PokeBuilder Cetitan()
    {
        var b = new PokeBuilder("Cetitan", 1f, BodyPlan.Quadruped, V(0, 0.38f, -0.04f)) { Coat = Fur };
        BeastLegs(b, 0.13f, 0.27f, 0.15f, -0.19f, 0.09f, WhaleWhite, WhaleBlue, WhaleWhite);
        var bc = V(0, 0.4f, -0.04f);
        var br = V(0.19f, 0.17f, 0.28f);
        b.Ell(Body, bc, br, WhaleWhite);
        b.PaintEll(Body, bc + V(0, -0.13f, 0), V(0.13f, 0.04f, 0.24f), WhaleBlue);
        foreach (float z in new[] { -0.14f, -0.04f, 0.06f })
            b.PaintEll(Body, bc + V(0, 0.14f, z), V(0.17f, 0.04f, 0.024f), WhaleBlue, soft: 0.01f);
        int tail = b.Tail(bc + V(0, 0.03f, -0.26f));
        Flukes(b, tail, bc + V(0, 0.03f, -0.25f), 0.16f, WhaleWhite);
        int head = b.Head(bc + V(0, 0.08f, 0.24f));
        var c = bc + V(0, 0.1f, 0.32f);
        var r = V(0.135f, 0.12f, 0.13f);
        b.Limb(head, bc + V(0, 0.05f, 0.2f), c + V(0, -0.02f, -0.05f), 0.13f, 0.12f, WhaleWhite);
        b.Ell(head, c, r, WhaleWhite);
        b.Ell(head, c + V(0, -0.035f, 0.1f), V(0.11f, 0.075f, 0.09f), WhaleWhite, blend: 0.025f);
        b.PaintEll(head, c + V(0, -0.085f, 0.1f), V(0.085f, 0.03f, 0.09f), WhalePink, soft: 0.01f);
        // The horn of ice on its snout, two smaller shards at its foot
        b.Spike(head, c + V(0, 0.05f, 0.11f), c + V(0, 0.3f, 0.27f), 0.06f, IceBlue, mat: Shell);
        PokeBuilder.Both(s => b.Spike(head, c + V(0.045f * s, 0.05f, 0.09f), c + V(0.085f * s, 0.17f, 0.15f), 0.028f, PixelCanvas.Mix(IceBlue, White, 0.3f), mat: Shell));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.06f * s, c.Y + 0.02f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(40, 40, 54), glare: true);
        });
        return b;
    }
}
