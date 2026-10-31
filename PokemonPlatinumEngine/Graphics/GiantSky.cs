using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The sky while a giant stands (plan 25 · YD1; style guide, "The giant's arrival"): the stage's light washed toward
/// a red-violet for as long as any Pokémon on the field is Dynamaxed, eased in through its growth and out through its
/// end, and banks of red-violet cloud low on the sky. State of the giant standing, so it shows with the Battle Scene off
/// too. GPU-free: a pure function of the animator's views and the battle's clock.
/// </summary>
internal static class GiantSky
{
    /// <summary>The colour the stage is washed toward, how far at the full, and how far it is greyed.</summary>
    public static readonly Vector3 WashColor = new(0.42f, 0.08f, 0.30f);
    public const float WashAmount = 0.3f, WashGrey = 0.12f;

    /// <summary>The most the cloud banks draw.</summary>
    public const int CloudCap = 8;

    private static float Smooth(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    /// <summary>How far one place's giant has come in (0..1): through its growth (after any call), held, and out through its end.</summary>
    public static float PresenceOf(CombatantView v)
    {
        if (v.Dynamaxed)
        {
            if (v.GiantAge < 0f) return 1f;
            return Smooth((v.GiantAge - v.GiantLead) / BattleAnimator.DynamaxGrown);
        }
        if (v.GiantAge < 0f) return 0f;
        return 1f - Smooth(v.GiantAge / BattleAnimator.DynamaxEndTime);
    }

    /// <summary>How far the giant's sky has come in over the field: the strongest of every place's.</summary>
    public static float Presence(BattleAnimator anim)
    {
        float most = 0f;
        for (int s = 0; s < 2; s++)
            for (int slot = 0; slot < anim.SlotsOf((BattleSide)s); slot++)
                most = Math.Max(most, PresenceOf(anim[(BattleSide)s, slot]));
        return most;
    }

    /// <summary>The stage's light at a presence.</summary>
    public static StageLight Light(float presence)
    {
        if (presence <= 0f) return StageLight.None;
        presence = Math.Clamp(presence, 0f, 1f);
        return new StageLight(WashColor, WashAmount * presence, WashGrey * presence);
    }

    /// <summary>The stage's light while a giant stands.</summary>
    public static StageLight Light(BattleAnimator anim) => Light(Presence(anim));

    /// <summary>
    /// Four banks of red-violet cloud low on the sky north of the stage, each a pair of puffs turning slowly, at the
    /// presence's strength: at most <see cref="CloudCap"/> quads, and none indoors or with no giant.
    /// </summary>
    public static void Clouds(float presence, float time, bool outdoors, FxList fx)
    {
        if (presence <= 0f || !outdoors) return;
        var color = new Color(150, 40, 90, 255);
        for (int i = 0; i < 4; i++)
        {
            float x = -30f + i * 15f + 3f * MathF.Sin(time * 0.05f + i * 1.9f);
            var at = new Vector3(x, 5.5f + 1.2f * (i % 2), -44f - 3f * (i % 3));
            for (int k = 0; k < 2; k++)
            {
                var p = at + new Vector3(k * 5.5f - 2.7f, k * 0.8f, k * 1.5f);
                fx.Sprite(FxShape.Smoke, p, 9f + 2f * k, Fx.Fade(color, 0.45f * presence * (k == 0 ? 1f : 0.8f)), FxBlend.Alpha,
                    time * 0.04f * (k == 0 ? 1f : -1f) + i, depth: true);
            }
        }
    }
}
