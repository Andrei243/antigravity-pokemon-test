using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;

namespace PokemonPlatinumEngine.Graphics;

// A shiny Pokémon's sparkle as it comes out (plan 09 · L1; style guide, "Shiny Pokémon"): our own after the original's
// beat, the SHINY of its battle animations that are no moves. A pure function of the cue's age and seed, nothing over
// the screen and no shake, at most ShinyCap quads.
internal static partial class MoveFx
{
    /// <summary>The most quads a sparkle draws at any age: a test holds it under.</summary>
    public const int ShinyCap = 16;

    /// <summary>How long a ring of stars takes to fly out, and how far behind the first the second ring sets off.</summary>
    public const float ShinyFlight = 0.5f, ShinySecondRing = 0.25f;

    /// <summary>A step of the stars' blinking: each is off for one step in every three.</summary>
    public const float ShinyBlink = 0.08f;

    private static readonly Color ShinyGold = new(255, 236, 150, 255);

    private static void ShinySparkle(in FxCtx c, FxList fx)
    {
        float t = c.Age - c.Cue.Lead;
        if (t < 0f || t > BattleAnimator.ShinyTime) return;
        float h = c.ToSize;
        var middle = c.To;

        // A pale gold glow swelling and fading behind the stars (light added; the stars themselves are matter)
        fx.Sprite(FxShape.Glow, middle, h * (0.55f + 0.35f * Fx.EaseOut(Math.Min(1f, t / 0.4f))),
            Fx.Fade(ShinyGold, 0.42f * Fx.Envelope(t, 0f, 0.12f, 0.45f, 0.9f)));

        Ring(c, fx, t, 8, h * 0.75f, h * 0.16f, 0);
        Ring(c, fx, t - ShinySecondRing, 5, h * 0.5f, h * 0.11f, 8);
    }

    /// <summary>One ring of four-pointed stars bursting out of the body's middle and blinking as they go.</summary>
    private static void Ring(in FxCtx c, FxList fx, float t, int count, float reach, float size, int first)
    {
        float k = t / ShinyFlight;
        if (k < 0f || k > 1.6f) return;
        float out1 = Fx.EaseOut(Math.Min(1f, k));
        float fade = k <= 1f ? 1f : 1f - (k - 1f) / 0.6f;
        for (int i = 0; i < count; i++)
        {
            // Off for one step in every three, each star in its own phase
            int step = (int)MathF.Floor(t / ShinyBlink) + i;
            if (step % 3 == 2) continue;
            float a = MathF.Tau * i / count + Fx.Rand(c.Seed, first + i, 401) * 0.4f;
            // Round the body in the picture's plane, a little toward the camera so the body never hides them
            var dir = new Vector3(MathF.Cos(a), MathF.Sin(a), 0.35f);
            var p = c.To + dir * reach * out1;
            float s = size * (0.6f + 0.4f * MathF.Sin(Math.Min(1f, k) * MathF.PI)) * fade;
            if (s <= 0f) continue;
            fx.Sprite(FxShape.Spark, p, s, Fx.Fade(i % 2 == 0 ? ShinyGold : White, fade), FxBlend.Alpha, k * 0.8f);
        }
    }
}
