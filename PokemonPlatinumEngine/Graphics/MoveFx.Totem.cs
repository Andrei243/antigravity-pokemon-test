using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;

namespace PokemonPlatinumEngine.Graphics;

// A trial's totem in its aura (plan 23 · Z6; style guide, "Stage", "A totem's aura"): our own look, since Sun and Moon's
// is a glowing outline the shader here doesn't draw. Six wisps of gold flame rise up the edges of its body by turns, a
// warm glow lies under its feet, and a glint now and then over its head; matter in its own colour, alpha-blended, with
// only the glow added, so it keeps its gold over a bright field. A pure function of the battle's clock and the place.
internal static partial class MoveFx
{
    /// <summary>The most a totem's aura draws: six wisps, three of them with a glow, the glow underfoot and a glint.</summary>
    public const int TotemCap = 11;

    /// <summary>How long one wisp is on its way up the body, and how long the aura takes to come up as the line is read.</summary>
    public const float TotemWispTime = 1.6f, TotemRise = 0.5f;

    private static readonly Color TotemGold = Rgb(255, 196, 64), TotemEmber = Rgb(236, 112, 40), TotemHalo = Rgb(255, 226, 150);

    /// <summary>Every totem on the field in its aura, as far as it can be seen.</summary>
    public static void Totems(BattleAnimator anim, FxPlaces places, FxList fx)
    {
        for (int s = 0; s < 2; s++)
            for (int slot = 0; slot < anim.SlotsOf((BattleSide)s); slot++)
            {
                var side = (BattleSide)s;
                var v = anim[side, slot];
                if (v.TotemAge < 0f) continue;
                float shown = anim.LastingShown(side, slot);
                if (shown <= 0f) continue;
                float k = shown * Math.Clamp(v.TotemAge / TotemRise, 0f, 1f);
                TotemAura(places.Feet(side, slot), places.Height(side, slot), places.HalfWidth(side, slot), anim.Time, slot * 41 + s * 113 + 5, k, fx);
            }
    }

    /// <summary>One totem's aura at strength <paramref name="k"/> (0..1): see the file's header. At most <see cref="TotemCap"/> quads.</summary>
    public static void TotemAura(Vector3 feet, float h, float w, float time, int seed, float k, FxList fx)
    {
        if (k <= 0.004f) return;
        fx.Flat(FxShape.Glow, feet + Vector3.UnitY * h * 0.02f, Vector3.UnitY, w + h * 0.25f, Fx.Fade(TotemEmber, k * 0.5f));
        for (int i = 0; i < 6; i++)
        {
            float u = Loop(time / TotemWispTime + i / 6f + Fx.Rand(seed, i, 611) * 0.1f);
            float side = i % 2 == 0 ? -1f : 1f;
            // Up the edge of the body, leaning in as it rises, fading in and out on its way
            var p = feet + new Vector3(side * w * (1.05f - 0.35f * u), h * (0.08f + 0.85f * u), h * 0.05f * Fx.Signed(seed, i, 612));
            float a = k * MathF.Sin(u * MathF.PI);
            float size = h * (0.11f - 0.04f * u);
            fx.Sprite(FxShape.Wisp, p, size, Fx.Fade(i % 3 == 0 ? TotemEmber : TotemGold, a * 0.85f), FxBlend.Alpha, side * 0.3f);
            if (i % 2 == 0) fx.Sprite(FxShape.Glow, p, size * 1.4f, Fx.Fade(TotemHalo, a * 0.35f));
        }
        // A glint over the head for a fifth of a second once in each round
        float t = Loop(time / 2.2f + Fx.Rand(seed, 0, 613));
        if (t < 0.09f) fx.Sprite(FxShape.Spark, feet + Vector3.UnitY * h * 1.06f, h * 0.08f, Fx.Fade(TotemHalo, k * MathF.Sin(t / 0.09f * MathF.PI)));
    }
}
