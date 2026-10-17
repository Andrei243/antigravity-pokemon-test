using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

// Plan 25 - Y10, its second part (Y10b), second half: the moves the story shows nowhere, from Tail Glow to Shadow
// Force, each a look of its own. The beats are the original's, read from each move's animation script; the shapes,
// colours and timings are our own. Every recipe lands at ImpactTime (FxList.Land, which Impact sets), is over by
// EffectTime and stays under MaxQuads. What is the body's own and not the effects' (Bulk Up's and Power Trick's
// squashes, Crush Grip's squeeze of the target, Shadow Force's vanishing, the stage faded or switched for Psycho Boost,
// Dark Void, Spacial Rend, Roar of Time, Judgment and the rest) is not drawn here.
internal static partial class MoveFx
{
    private static Dictionary<string, FxRecipe> Platinum10() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Tail Glow"] = TailGlowY10b,
        ["Luster Purge"] = LusterPurgeY10b,
        ["Mist Ball"] = MistBallY10b,
        ["Feather Dance"] = FeatherDanceY10b,
        ["Ice Ball"] = IceBallY10b,
        ["Blast Burn"] = BlastBurnY10b,
        ["Hydro Cannon"] = HydroCannonY10b,
        ["Weather Ball"] = WeatherBallY10b,
        ["Fake Tears"] = FakeTearsY10b,
        ["Howl"] = HowlY10b,
        ["Frenzy Plant"] = FrenzyPlantY10b,
        ["Bulk Up"] = BulkUpY10b,
        ["Poison Tail"] = PoisonTailY10b,
        ["Volt Tackle"] = VoltTackleY10b,
        ["Doom Desire"] = DoomDesireY10b,
        ["Psycho Boost"] = PsychoBoostY10b,
        ["Trump Card"] = TrumpCardY10b,
        ["Power Trick"] = PowerTrickY10b,
        ["Me First"] = MeFirstY10b,
        ["Heart Swap"] = HeartSwapY10b,
        ["Vacuum Wave"] = VacuumWaveY10b,
        ["Switcheroo"] = SwitcherooY10b,
        ["Draco Meteor"] = DracoMeteorY10b,
        ["Judgment"] = JudgmentY10b,
        ["Roar of Time"] = RoarOfTimeY10b,
        ["Spacial Rend"] = SpacialRendY10b,
        ["Lunar Dance"] = LunarDanceY10b,
        ["Crush Grip"] = CrushGripY10b,
        ["Magma Storm"] = MagmaStormY10b,
        ["Dark Void"] = DarkVoidY10b,
        ["Seed Flare"] = SeedFlareY10b,
        ["Shadow Force"] = ShadowForceY10b
    };

    // ------------------------------------------------------------------ shared by the batch

    /// <summary>Things (<paramref name="shape"/>) on a ring round <paramref name="at"/> that turns, <paramref name="count"/> of them at <paramref name="radius"/> of the height, faded in and out between <paramref name="t0"/> and <paramref name="t1"/>.</summary>
    private static void Orbit(FxList fx, float age, Vector3 at, float h, float t0, float t1, int count, FxShape shape, Color color, float size, float radius, float speed, float flat = 0.6f)
    {
        float env = Fx.Envelope(age, t0, t0 + 0.1f, t1 - 0.15f, t1);
        if (env <= 0f) return;
        for (int i = 0; i < count; i++)
        {
            float a = i * MathF.Tau / count + age * speed;
            var p = at + new Vector3(MathF.Cos(a) * h * radius, MathF.Sin(a) * h * radius * flat, MathF.Sin(a) * 0.1f);
            fx.Sprite(shape, p, h * size, Fx.Fade(color, env), FxBlend.Alpha, a);
        }
    }

    /// <summary>A column of light standing on the target from the top of the picture down, in <paramref name="color"/>, between <paramref name="t0"/> and <paramref name="t1"/>.</summary>
    private static void Pillar(in FxCtx c, FxList fx, Vector3 foot, float t0, float t1, float width, Color color, Color core)
    {
        float env = Fx.Envelope(c.Age, t0, t0 + 0.05f, t1 - 0.12f, t1);
        if (env <= 0f) return;
        float h = c.ToSize;
        var mid = foot + Vector3.UnitY * h * 1.2f;
        fx.Streak(FxShape.Beam, mid, Vector3.UnitY, width, h * 1.2f / width, Fx.Fade(color, env * 0.8f), FxBlend.Alpha);
        fx.Streak(FxShape.Beam, mid, Vector3.UnitY, width * 0.35f, h * 1.2f / (width * 0.35f), Fx.Fade(core, env));
    }

    // ------------------------------------------------------------------ the recipes

    /// <summary>Tail Glow: the tip of the user's tail flares into a dazzling star of yellow-white light, long rays and sparkles flying off it.</summary>
    private static void TailGlowY10b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.15f, 0.8f, 1.1f);
        var tip = c.FromFeet + new Vector3(h * 0.55f, h * 0.7f, 0.1f);
        fx.Sprite(FxShape.Glow, tip, h * 1.2f * env, Fx.Fade(Rgb(255, 240, 140), 0.7f * env), FxBlend.Alpha);
        fx.Sprite(FxShape.Star, tip, h * 0.7f * env, Fx.Fade(White, env), FxBlend.Alpha, c.Age * 1.5f);
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f + c.Age * 0.8f;
            fx.Streak(FxShape.Beam, tip + new Vector3(MathF.Cos(a), MathF.Sin(a), 0f) * h * 0.4f, new Vector3(MathF.Cos(a), MathF.Sin(a), 0f), h * 0.06f, 5f, Fx.Fade(Rgb(255, 244, 170), env * 0.8f), FxBlend.Alpha);
        }
        for (int i = 0; i < 5; i++)
            Fx.Glint(fx, c.Age, tip + new Vector3(Fx.Signed(c.Seed, i, 440) * h * 0.6f, Fx.Signed(c.Seed, i, 441) * h * 0.5f, 0f), h, 0.2f + i * 0.14f, White, 0.3f);
        Fx.Aura(fx, c.Age, c.Seed, c.FromFeet, h, 0.35f, 1.15f, Rgb(255, 230, 120), White, 8);
    }

    /// <summary>Luster Purge: a ball of silver-white light gathers before the user, rings of glitter turning round it, and it bursts across the target in a shower of crystal stars.</summary>
    private static void LusterPurgeY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var silver = Rgb(236, 244, 255);
        var ball = c.From + new Vector3(h * 0.5f, h * 0.15f, 0.1f);
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.2f, 0.26f);
        if (env > 0f)
        {
            fx.Sprite(FxShape.Glow, ball, h * 1.0f * env, Fx.Fade(silver, 0.7f * env));
            fx.Sprite(FxShape.Orb, ball, h * 0.4f * env, Fx.Fade(White, env), FxBlend.Alpha);
            Orbit(fx, c.Age, ball, h, 0f, 0.28f, 6, FxShape.Spark, White, 0.12f, 0.45f, 12f);
        }
        Gather(fx, c.Age, c.Seed, ball, h, 0f, 0.2f, 8, silver, Rgb(180, 220, 255), 0.07f, 0.9f);
        Fx.Stream(fx, c, 0.18f, I + 0.05f, 0.2f, 8, FxShape.Star, silver, Rgb(200, 230, 255), 0.13f, arc: 0.03f, spread: 0.3f, spin: 8f, blend: FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            fx.ScreenFlash(White, 0.3f * Fx.Envelope(c.Age, I - 0.02f, I, I + 0.06f, I + 0.24f));
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.7f, 12, FxShape.Star, White, Rgb(180, 220, 255), 0.12f, 1.2f, 0.2f, FxBlend.Alpha, spin: 7f, salt: 240);
            PulseRing(fx, c.Age, c.To, I, 0.5f, c.ToSize * 0.3f, c.ToSize * 1.7f, Rgb(200, 230, 255), White);
            Jolt(fx, c.Age, I, 0.5f, 0.3f);
        }
        Impact(c, col, fx, 1.15f);
    }

    /// <summary>Mist Ball: a ball of pale pink and blue mist rolls up before the user and is flung across, bursting on the target in a great puff and a ring of fine droplets.</summary>
    private static void MistBallY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var pink = Rgb(255, 196, 226);
        var blue = Rgb(170, 220, 255);
        var ball = c.From + new Vector3(h * 0.5f, h * 0.15f, 0.1f);
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.2f, 0.26f);
        if (env > 0f)
        {
            fx.Sprite(FxShape.Smoke, ball, h * 0.9f * env, Fx.Fade(pink, 0.8f * env), FxBlend.Alpha, c.Age * 3f);
            fx.Sprite(FxShape.Smoke, ball, h * 0.6f * env, Fx.Fade(blue, 0.8f * env), FxBlend.Alpha, -c.Age * 4f);
        }
        Haze(fx, c.Age, c.Seed, ball, h, 0f, 0.3f, 4, pink, 0.4f);
        float k = (c.Age - 0.2f) / (I - 0.2f);
        if (k >= 0f && k <= 1f)
        {
            var p = Lob(ball, c.Aim, k, 0.04f);
            fx.Sprite(FxShape.Smoke, p, h * 0.7f, Fx.Fade(pink, 0.9f), FxBlend.Alpha, k * 6f);
            fx.Sprite(FxShape.Orb, p, h * 0.35f, Fx.Fade(White, 0.9f), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.8f, 7, FxShape.Smoke, pink, blue, 0.3f, 0.9f, -0.2f, FxBlend.Alpha, salt: 241);
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.6f, 10, FxShape.Dot, White, blue, 0.05f, 1.1f, 0.4f, FxBlend.Alpha, salt: 242);
            PulseRing(fx, c.Age, c.To, I, 0.5f, c.ToSize * 0.3f, c.ToSize * 1.6f, pink, White);
            Jolt(fx, c.Age, I, 0.4f, 0.26f);
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Feather Dance: soft grey feathers whirl down round the target in a slow spiral and settle on it.</summary>
    private static void FeatherDanceY10b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0.02f, 0.15f, 0.95f, 1.2f);
        if (env <= 0f) return;
        for (int i = 0; i < 12; i++)
        {
            float u = (c.Age * 0.7f + Fx.Rand(c.Seed, i, 442)) % 1f;
            float a = u * MathF.Tau * 1.2f + i;
            var p = c.ToFeet + new Vector3(MathF.Cos(a) * h * (0.7f - 0.3f * u), h * (1.5f - 1.35f * u), MathF.Sin(a) * h * 0.2f);
            fx.Sprite(FxShape.Feather, p, h * 0.2f, Fx.Fade(i % 3 == 0 ? Rgb(236, 238, 246) : Rgb(190, 196, 214), env * Math.Min(1f, (1f - u) * 4f)), FxBlend.Alpha, a * 2f);
        }
        Tint(c, fx, 0.3f, 1.1f, Rgb(200, 206, 222), 0.3f);
    }

    /// <summary>Ice Ball: a ball of packed ice rolls across turning, glittering and leaving frost, and breaks on the target in shards and a chill.</summary>
    private static void IceBallY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var ice = ColorsOf(PokemonType.Ice);
        float k = (c.Age - 0.02f) / (I - 0.02f);
        if (k >= 0f && k <= 1f)
        {
            var p = Vector3.Lerp(c.From, c.Aim, k) + Vector3.UnitY * MathF.Abs(MathF.Sin(k * 9f)) * h * 0.18f;
            fx.Sprite(FxShape.Glow, p, h * 0.7f, Fx.Fade(ice.Light, 0.5f));
            fx.Sprite(FxShape.Orb, p, h * 0.46f, Fx.Fade(Rgb(190, 226, 255), 1f), FxBlend.Alpha, k * 12f);
            fx.Sprite(FxShape.Snowflake, p, h * 0.34f, Fx.Fade(White, 0.9f), FxBlend.Alpha, -k * 10f);
            Fx.Burst(fx, c.Seed, c.Age, p, h, c.Age - 0.01f, 0.3f, 2, FxShape.Dot, White, ice.Light, 0.05f, 0.3f, 0.2f, FxBlend.Alpha, salt: 243);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 10, FxShape.Shard, ice.Light, White, 0.12f, 1.1f, 1.0f, FxBlend.Alpha, spin: 6f, salt: 244);
            PulseRing(fx, c.Age, c.To, I, 0.45f, h * 0.3f, h * 1.5f, ice.Main, White);
            Tint(c, fx, I, 0.95f, Rgb(210, 240, 255), 0.45f);
            Jolt(fx, c.Age, I, 0.35f, 0.24f);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Blast Burn: flames wheel round the user in a growing ring, then a vast ball of fire is thrown across and goes off on the target in a great blast with a ring of flame on the ground.</summary>
    private static void BlastBurnY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var fire = ColorsOf(PokemonType.Fire);
        float env = Fx.Envelope(c.Age, 0f, 0.08f, 0.2f, 0.28f);
        if (env > 0f)
        {
            Orbit(fx, c.Age, c.From, h, 0f, 0.3f, 8, FxShape.Flame, fire.Main, 0.3f, 0.7f, 12f, 0.9f);
            fx.Sprite(FxShape.Glow, c.From, h * 1.2f, Fx.Fade(Rgb(255, 120, 50), env * 0.5f), FxBlend.Alpha);
        }
        float k = (c.Age - 0.16f) / (I - 0.16f);
        if (k >= 0f && k <= 1f)
        {
            var p = Lob(c.From, c.Aim, k, 0.04f);
            fx.Sprite(FxShape.Glow, p, h * 1.0f, Fx.Fade(Rgb(255, 140, 60), 0.8f));
            fx.Sprite(FxShape.Flame, p, h * 0.9f, Fx.Fade(fire.Main, 1f), FxBlend.Add, c.Age * 4f);
            fx.Sprite(FxShape.Orb, p, h * 0.35f, Fx.Fade(Rgb(255, 246, 200), 1f), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            Blast(c, fx, c.To, c.ToFeet, c.ToSize, I, 18, 6, 1.5f);
            Tint(c, fx, I, 1.1f, Rgb(160, 30, 20), 0.5f);
            Jolt(fx, c.Age, I, 0.8f, 0.4f);
        }
        Impact(c, col, fx, 1.3f);
    }

    /// <summary>Hydro Cannon: the user gathers a great ball of water at its middle and fires it across with a roaring jet behind it, bursting on the target in a huge splash and ring.</summary>
    private static void HydroCannonY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var water = ColorsOf(PokemonType.Water);
        Gather(fx, c.Age, c.Seed, c.From, h, 0f, 0.18f, 9, water.Main, White, 0.09f, 0.9f);
        float ball = Fx.Envelope(c.Age, 0f, 0.16f, 0.18f, 0.24f);
        if (ball > 0f)
        {
            fx.Sprite(FxShape.Glow, c.From, h * 1.0f * ball, Fx.Fade(water.Light, 0.6f));
            fx.Sprite(FxShape.Orb, c.From, h * 0.6f * ball, Fx.Fade(water.Main, 1f), FxBlend.Alpha);
        }
        float k = (c.Age - 0.18f) / (I - 0.18f);
        if (k >= 0f && k <= 1f)
        {
            var p = Vector3.Lerp(c.From, c.Aim, k);
            fx.Sprite(FxShape.Glow, p, h * 1.0f, Fx.Fade(water.Light, 0.6f));
            fx.Sprite(FxShape.Orb, p, h * 0.65f, Fx.Fade(water.Main, 1f), FxBlend.Alpha, k * 6f);
            fx.Sprite(FxShape.Ring, p, h * 0.65f, Fx.Fade(White, 0.8f));
        }
        Fx.Stream(fx, c, 0.15f, I + 0.05f, 0.25f, 14, FxShape.Drop, water.Main, water.Light, 0.11f, spread: 0.2f, blend: FxBlend.Alpha, trail: 2f);
        if (c.Cue.Landed)
        {
            PulseRing(fx, c.Age, c.To, I, 0.5f, c.ToSize * 0.3f, c.ToSize * 1.9f, water.Main, White);
            GroundRing(fx, c.Age, c.ToFeet, I, 0.5f, c.ToSize * 2.2f, Fx.Fade(water.Light, 0.9f));
            Splash(c, fx, c.To, c.ToSize, I, water.Main, White, 12, 5);
            Jolt(fx, c.Age, I, 0.75f, 0.38f);
        }
        Impact(c, col, fx, 1.3f);
    }

    /// <summary>Weather Ball: a ball of swirling weather (sun, rain, hail, sand or clear sky, by the move's type) gathers at the user, a white flash, and it is hurled across and bursts in its own weather.</summary>
    private static void WeatherBallY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var t = ColorsOf(c.Cue.Type);
        var ball = c.From + new Vector3(0f, h * 0.2f, 0.1f);
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.2f, 0.26f);
        if (env > 0f)
        {
            fx.Sprite(FxShape.Glow, ball, h * 1.0f * env, Fx.Fade(t.Light, 0.6f * env));
            fx.Sprite(FxShape.Swirl, ball, h * 0.7f * env, Fx.Fade(t.Main, env), FxBlend.Alpha, c.Age * 10f);
            fx.Sprite(FxShape.Orb, ball, h * 0.35f * env, Fx.Fade(White, env), FxBlend.Alpha);
        }
        Gather(fx, c.Age, c.Seed, ball, h, 0f, 0.2f, 7, t.Main, White, 0.07f, 0.8f);
        float k = (c.Age - 0.2f) / (I - 0.2f);
        if (k >= 0f && k <= 1f)
        {
            var p = Vector3.Lerp(ball, c.Aim, k);
            fx.Sprite(FxShape.Glow, p, h * 0.9f, Fx.Fade(t.Light, 0.6f));
            fx.Sprite(FxShape.Swirl, p, h * 0.6f, Fx.Fade(t.Main, 1f), FxBlend.Alpha, k * 14f);
            fx.Sprite(FxShape.Orb, p, h * 0.3f, Fx.Fade(White, 1f), FxBlend.Alpha);
        }
        fx.ScreenFlash(White, 0.2f * Fx.Envelope(c.Age, 0.17f, 0.2f, 0.22f, 0.3f));
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.6f, 12, FxShape.Dot, t.Main, t.Light, 0.12f, 1.1f, 0.8f, FxBlend.Alpha, salt: 245);
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.6f, 5, FxShape.Swirl, t.Light, White, 0.2f, 0.8f, 0f, FxBlend.Alpha, spin: 6f, salt: 246);
            PulseRing(fx, c.Age, c.To, I, 0.45f, c.ToSize * 0.3f, c.ToSize * 1.5f, t.Main, White);
            Jolt(fx, c.Age, I, 0.45f, 0.28f);
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Fake Tears: big blue tears well up at the user's eyes and stream down, glinting, and a few land on the foe in a pale blue sadness.</summary>
    private static void FakeTearsY10b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var tear = Rgb(150, 210, 255);
        var eyes = c.FromFeet + new Vector3(0f, h * 0.85f, 0.15f);
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.7f, 1.0f);
        for (int s = -1; s <= 1; s += 2)
            for (int i = 0; i < 4; i++)
            {
                float u = (c.Age * 1.1f + i * 0.25f + (s > 0 ? 0.1f : 0f)) % 1f;
                var p = eyes + new Vector3(s * h * 0.2f + s * u * h * 0.05f, -u * h * 0.7f, 0f);
                fx.Sprite(FxShape.Drop, p, h * 0.18f, Fx.Fade(tear, env * Math.Min(1f, (1f - u) * 3f)), FxBlend.Alpha, MathF.PI);
                fx.Sprite(FxShape.Dot, p + new Vector3(h * 0.03f, h * 0.04f, 0f), h * 0.06f, Fx.Fade(White, env * (1f - u)), FxBlend.Alpha);
            }
        Fx.Glint(fx, c.Age, eyes + new Vector3(-h * 0.2f, 0f, 0f), h, 0.1f, White, 0.3f);
        Fx.Glint(fx, c.Age, eyes + new Vector3(h * 0.2f, 0f, 0f), h, 0.18f, White, 0.3f);
        Fx.Rain(fx, c.Age, c.Seed, c.To, c.ToSize, 0.3f, 1.0f, 0.35f, 8, FxShape.Drop, tear, Rgb(210, 236, 255), 0.09f, FxBlend.Alpha);
        Tint(c, fx, 0.4f, 1.1f, Rgb(160, 200, 255), 0.3f);
    }

    /// <summary>Howl: the user throws its head back, rings of red sound rolling up and out in arcs, a red glow and a pulse of anger.</summary>
    private static void HowlY10b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var red = Rgb(255, 90, 70);
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.8f, 1.1f);
        var mouth = c.FromFeet + new Vector3(h * 0.25f, h * 0.95f, 0.15f);
        fx.Sprite(FxShape.Glow, c.From, h * 1.1f, Fx.Fade(red, env * 0.35f), FxBlend.Alpha);
        for (int i = 0; i < 4; i++)
        {
            float k = (c.Age - 0.05f - i * 0.18f) / 0.45f;
            if (k < 0f || k > 1f) continue;
            fx.Sprite(FxShape.Wave, mouth + new Vector3(h * 0.15f * k, h * 0.3f * k, 0f), h * (0.4f + 0.7f * k), Fx.Fade(i % 2 == 0 ? red : Rgb(255, 214, 190), (1f - k) * 0.9f), FxBlend.Alpha, MathF.PI / 3f);
        }
        PulseRing(fx, c.Age, c.From, 0.04f, 0.5f, h * 0.3f, h * 1.2f, red, White);
        Anger(fx, c.Age, c.FromFeet + new Vector3(-h * 0.35f, h * 1.1f, 0f), h, 0.4f, 1.1f);
        Fx.Aura(fx, c.Age, c.Seed, c.FromFeet, h, 0.3f, 1.1f, red, Rgb(255, 220, 200), 8);
    }

    /// <summary>Frenzy Plant: great thorned vines burst from the ground under the target one after another and lash it, wrapping, leaves flying.</summary>
    private static void FrenzyPlantY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var green = Rgb(70, 170, 60);
        for (int i = 0; i < 4; i++)
        {
            float t0 = 0.02f + i * 0.07f;
            float side = (i - 1.5f) * 0.45f;
            var root = c.ToFeet + new Vector3(side * h, 0.05f, 0.1f);
            Whip(fx, c.Age, root, c.To + new Vector3(-side * h * 0.2f, h * 0.2f * (i % 2 == 0 ? 1f : -1f), 0.1f), t0, I - t0 + 0.05f, 0.4f, 0.1f, h * 0.4f, i % 2 == 0 ? green : Rgb(100, 200, 80), Rgb(200, 250, 150), FxShape.Leaf, h * 0.3f, 0f);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 10, FxShape.Leaf, green, Rgb(200, 250, 150), 0.14f, 1.0f, 0.6f, FxBlend.Alpha, spin: 7f, salt: 247);
            GroundRing(fx, c.Age, c.ToFeet, I, 0.5f, h * 1.8f, Fx.Fade(Rgb(180, 240, 140), 0.9f));
            Tint(c, fx, I, 0.95f, green, 0.35f);
            Jolt(fx, c.Age, I, 0.7f, 0.36f);
        }
        Impact(c, col, fx, 1.2f);
    }

    /// <summary>Bulk Up: the user flexes, red-orange muscle-glow pulsing out of it in beats, two fists pumping at its sides and anger marks popping.</summary>
    private static void BulkUpY10b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var red = Rgb(255, 120, 60);
        for (int i = 0; i < 3; i++)
        {
            float t0 = 0.05f + i * 0.26f;
            PulseRing(fx, c.Age, c.From, t0, 0.4f, h * 0.35f, h * 1.1f, red, Rgb(255, 230, 190));
            float p = Fx.Envelope(c.Age, t0, t0 + 0.05f, t0 + 0.1f, t0 + 0.22f);
            if (p > 0f)
                for (int s = -1; s <= 1; s += 2)
                    fx.Sprite(FxShape.Fist, c.From + new Vector3(s * h * 0.6f, h * 0.1f + p * h * 0.12f, 0.15f), h * (0.25f + 0.1f * p), Fx.Fade(Rgb(255, 196, 150), p), FxBlend.Alpha, s * 0.5f);
        }
        float glow = Fx.Envelope(c.Age, 0.05f, 0.3f, 0.85f, 1.1f);
        fx.Sprite(FxShape.Glow, c.From, h * 1.1f, Fx.Fade(red, glow * 0.4f), FxBlend.Alpha);
        Anger(fx, c.Age, c.FromFeet + new Vector3(h * 0.4f, h * 1.1f, 0f), h, 0.5f, 1.1f);
    }

    /// <summary>Poison Tail: a tail of violet light is swung across in a wide crescent, poison dripping from it, and strikes the target in a purple spray.</summary>
    private static void PoisonTailY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var purple = ColorsOf(PokemonType.Poison);
        Crescent(fx, c.Age, c.To + new Vector3(-h * 1.1f, -h * 0.4f, 0.2f), c.To + new Vector3(h * 0.3f, h * 0.2f, 0.2f), 0.05f, I - 0.05f, h * 0.8f, h * 1.3f, purple.Main, purple.Light);
        Drips(fx, c.Age, c.Seed, c.To + new Vector3(-h * 0.3f, h * 0.2f, 0f), h, 0.2f, 4, purple.Main);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 9, FxShape.Bubble, purple.Main, purple.Light, 0.11f, 1.0f, 0.8f, FxBlend.Alpha, salt: 248);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 5, FxShape.Drop, purple.Dark, purple.Main, 0.1f, 0.9f, 1.4f, FxBlend.Alpha, salt: 249);
            Tint(c, fx, I, 0.95f, purple.Main, 0.4f);
            Jolt(fx, c.Age, I, 0.4f, 0.26f);
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Volt Tackle: the user is cloaked in crackling yellow lightning, streaks across with a long white-yellow trail and crashes in a ring of bolts; a red-yellow ring of recoil on the user.</summary>
    private static void VoltTackleY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var el = ColorsOf(PokemonType.Electric);
        float cloak = Fx.Envelope(c.Age, 0f, 0.08f, 0.3f, 0.38f);
        if (cloak > 0f)
        {
            fx.Sprite(FxShape.Glow, c.From, h * 1.3f, Fx.Fade(el.Main, cloak * 0.6f), FxBlend.Alpha);
            for (int i = 0; i < 3; i++)
                Fx.Bolt(fx, c.Age, c.Seed + i, c.From + new Vector3(-h * 0.6f, (i - 1) * h * 0.4f, 0f), c.From + new Vector3(h * 0.6f, (1 - i) * h * 0.3f, 0f), 0f, 0.38f, h * 0.03f, el.Main, White, 1);
        }
        SpeedLines(c, fx, 0.1f, 0.34f, 12, 1.0f);
        Fx.Stream(fx, c, 0.08f, I, 0.22f, 12, FxShape.Spark, el.Main, White, 0.12f, spread: 0.3f);
        if (c.Cue.Landed)
        {
            for (int i = 0; i < 5; i++)
            {
                float a = i * MathF.Tau / 5f;
                Fx.Bolt(fx, c.Age, c.Seed + 9 + i, c.To, c.To + new Vector3(MathF.Cos(a), MathF.Sin(a), 0f) * c.ToSize * 1.0f, I, I + 0.4f, c.ToSize * 0.04f, el.Main, White, 1);
            }
            PulseRing(fx, c.Age, c.To, I, 0.5f, c.ToSize * 0.3f, c.ToSize * 1.7f, el.Main, White);
            PulseRing(fx, c.Age, c.From, I + 0.1f, 0.4f, h * 0.3f, h * 1.1f, Rgb(255, 130, 50), el.Light);
            Jolt(fx, c.Age, I, 0.7f, 0.34f);
        }
        Impact(c, col, fx, 1.25f);
    }

    /// <summary>Doom Desire: a golden star of light is drawn together over the user and sent rising into the sky as a ring of lesser stars turns under it (the strike is the turns after).</summary>
    private static void DoomDesireY10b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var gold = Rgb(255, 224, 120);
        float env = Fx.Envelope(c.Age, 0f, 0.12f, 0.6f, 0.9f);
        var head = c.FromFeet + new Vector3(0f, h * 0.85f, 0.1f);
        Gather(fx, c.Age, c.Seed, head, h, 0f, 0.4f, 10, gold, White, 0.08f, 1.0f);
        fx.Sprite(FxShape.Glow, c.From, h * 1.1f, Fx.Fade(gold, env * 0.4f), FxBlend.Alpha);
        float rise = Fx.EaseOut(Math.Clamp((c.Age - 0.35f) / 0.5f, 0f, 1f));
        var star = head + new Vector3(0f, h * 0.25f * rise, 0f);
        float s = Fx.Envelope(c.Age, 0.1f, 0.35f, 0.85f, 1.15f);
        fx.Sprite(FxShape.Star, star, h * 0.55f * s, Fx.Fade(Rgb(255, 244, 190), s), FxBlend.Alpha, c.Age * 2f);
        fx.Sprite(FxShape.Glow, star, h * 0.9f * s, Fx.Fade(gold, 0.6f * s));
        Orbit(fx, c.Age, c.FromFeet + Vector3.UnitY * h * 0.1f, h, 0.1f, 1.1f, 6, FxShape.Star, gold, 0.12f, 0.6f, 4f, 0.3f);
        PulseRing(fx, c.Age, c.From, 0.5f, 0.5f, h * 0.3f, h * 1.2f, gold, White);
    }

    /// <summary>Psycho Boost: the user is lit by a swelling violet-white charge, rings drawn in, then a great wave of psychic force breaks over the target in rings and a swirl.</summary>
    private static void PsychoBoostY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var psy = ColorsOf(PokemonType.Psychic);
        float ch = Fx.Envelope(c.Age, 0f, 0.12f, 0.22f, 0.3f);
        if (ch > 0f)
        {
            fx.Sprite(FxShape.Glow, c.From, h * 1.4f, Fx.Fade(psy.Main, ch * 0.6f), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, c.From, h * 0.8f, Fx.Fade(White, ch * 0.7f));
        }
        Gather(fx, c.Age, c.Seed, c.From, h, 0f, 0.24f, 12, psy.Main, White, 0.09f, 1.1f);
        Fx.Waves(fx, c, 0.16f, I, 4, FxShape.Ring, psy.Light, 0.5f, 0.2f);
        fx.Sprite(FxShape.Swirl, c.To, c.ToSize * 1.4f, Fx.Fade(psy.Main, 0.7f * Fx.Envelope(c.Age, I - 0.05f, I, I + 0.4f, I + 0.7f)), FxBlend.Alpha, c.Age * 6f);
        if (c.Cue.Landed)
        {
            fx.ScreenFlash(Rgb(230, 200, 255), 0.3f * Fx.Envelope(c.Age, I - 0.02f, I, I + 0.06f, I + 0.28f));
            for (int i = 0; i < 3; i++) PulseRing(fx, c.Age, c.To, I + i * 0.07f, 0.45f, c.ToSize * 0.3f, c.ToSize * (1.5f + 0.4f * i), psy.Main, White);
            Tint(c, fx, I, 1.1f, psy.Main, 0.5f);
            Jolt(fx, c.Age, I, 0.7f, 0.4f);
        }
        Impact(c, col, fx, 1.3f);
    }

    /// <summary>Trump Card: four playing cards are flicked across, spinning, one after another, and strike the target in white flashes, the last with a great star.</summary>
    private static void TrumpCardY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        for (int i = 0; i < 4; i++)
        {
            float land = I - (3 - i) * 0.06f;
            float k = (c.Age - (land - 0.16f)) / 0.16f;
            if (k >= 0f && k <= 1f)
            {
                var p = Lob(c.From, c.Aim + new Vector3(0f, (i - 1.5f) * h * 0.2f, 0f), k, 0.03f);
                float spin = k * 9f + i;
                var dir = new Vector3(MathF.Cos(spin), MathF.Sin(spin), 0f);
                fx.Streak(FxShape.Beam, p, dir, h * 0.2f, 1.5f, Fx.Fade(White, 1f), FxBlend.Alpha);
                fx.Streak(FxShape.Beam, p, dir, h * 0.1f, 0.7f, Fx.Fade(i % 2 == 0 ? Rgb(220, 50, 70) : Rgb(40, 40, 60), 1f), FxBlend.Alpha);
                fx.Sprite(FxShape.Glow, p, h * 0.4f, Fx.Fade(White, 0.3f));
            }
            if (!c.Cue.Landed) continue;
            Fx.Contact(fx, c.Age, c.Seed + i, c.To + new Vector3(0f, (i - 1.5f) * h * 0.2f, 0.1f), h, land, i == 3 ? 0.34f : 0.18f, White, Rgb(255, 240, 200));
            Jolt(fx, c.Age, land, 0.12f + 0.1f * i, 0.1f);
        }
        if (c.Cue.Landed)
        {
            float k = Fx.EaseOut((c.Age - I) / 0.22f);
            fx.Sprite(FxShape.Star, c.To, h * 1.3f * (0.6f + 0.5f * k), Fx.Fade(White, Fx.Envelope(c.Age, I - 0.01f, I, I + 0.08f, I + 0.3f)), FxBlend.Alpha, 0.2f);
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Power Trick: a red orb and a blue one circle the user, swap places twice with a flash and a ring, and the user glows violet.</summary>
    private static void PowerTrickY10b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.9f, 1.2f);
        if (env <= 0f) return;
        for (int o = 0; o < 2; o++)
        {
            float swap = Fx.EaseOut(Math.Clamp((c.Age - 0.4f) / 0.35f, 0f, 1f));
            float a = c.Age * 6f + (o == 0 ? 0f : MathF.PI) + swap * MathF.PI;
            var p = c.From + new Vector3(MathF.Cos(a) * h * 0.75f, MathF.Sin(a) * h * 0.4f + h * 0.1f, MathF.Sin(a) * 0.1f);
            var color = (o == 0) == (swap < 0.5f) ? Rgb(255, 90, 70) : Rgb(90, 150, 255);
            fx.Sprite(FxShape.Glow, p, h * 0.55f, Fx.Fade(color, env * 0.6f));
            fx.Sprite(FxShape.Orb, p, h * 0.28f, Fx.Fade(color, env), FxBlend.Alpha);
        }
        foreach (float t0 in new[] { 0.42f, 0.72f })
        {
            PulseRing(fx, c.Age, c.From, t0, 0.4f, h * 0.3f, h * 1.1f, Rgb(200, 150, 255), White);
            Fx.Glint(fx, c.Age, c.From + new Vector3(h * 0.5f, 0f, 0.1f), h, t0, White, 0.4f);
        }
        float glow = Fx.Envelope(c.Age, 0.4f, 0.55f, 0.95f, 1.2f);
        fx.Sprite(FxShape.Glow, c.From, h * 1.1f, Fx.Fade(Rgb(190, 130, 255), glow * 0.4f), FxBlend.Alpha);
    }

    /// <summary>Me First: light is drawn out of the foe toward the user, which glows teal, and a bolt of it is thrown back at the foe first.</summary>
    private static void MeFirstY10b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var teal = Rgb(110, 230, 210);
        Fx.Stream(fx, c, 0f, 0.4f, 0.25f, 10, FxShape.Glow, teal, White, 0.12f, arc: 0.06f, spread: 0.35f, blend: FxBlend.Alpha, reverse: true);
        float glow = Fx.Envelope(c.Age, 0.2f, 0.35f, 0.5f, 0.75f);
        if (glow > 0f) fx.Sprite(FxShape.Glow, c.From, h * 1.2f, Fx.Fade(teal, glow * 0.6f), FxBlend.Alpha);
        float k = (c.Age - 0.38f) / 0.3f;
        if (k >= 0f && k <= 1f)
        {
            var p = Lob(c.From, c.To, Fx.EaseOut(k), 0.04f);
            fx.Sprite(FxShape.Glow, p, h * 0.7f, Fx.Fade(teal, 0.7f));
            fx.Sprite(FxShape.Orb, p, h * 0.35f, Fx.Fade(White, 1f), FxBlend.Alpha);
        }
        float hit = Fx.Envelope(c.Age, 0.66f, 0.7f, 0.85f, 1.1f);
        if (hit > 0f)
        {
            fx.Sprite(FxShape.Glow, c.To, c.ToSize * 1.1f, Fx.Fade(teal, hit * 0.5f), FxBlend.Alpha);
            PulseRing(fx, c.Age, c.To, 0.67f, 0.4f, c.ToSize * 0.3f, c.ToSize * 1.3f, teal, White);
        }
    }

    /// <summary>Heart Swap: pink hearts stream from the user to the foe and from the foe to the user, crossing in the middle, and both ring with a pink pulse.</summary>
    private static void HeartSwapY10b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var pink = Rgb(255, 130, 180);
        for (int i = 0; i < 3; i++)
        {
            float t0 = i * 0.2f;
            float k = (c.Age - t0) / 0.4f;
            if (k < 0f || k > 1f) continue;
            for (int s = 0; s < 2; s++)
            {
                var a = s == 0 ? c.From : c.To;
                var b = s == 0 ? c.To : c.From;
                var p = Lob(a, b, Fx.EaseOut(k), s == 0 ? 0.06f : -0.06f);
                float sz = (s == 0 ? c.FromSize : c.ToSize) * 0.22f;
                fx.Sprite(FxShape.Glow, p, sz * 2f, Fx.Fade(pink, 0.35f * MathF.Sin(k * MathF.PI)));
                fx.Sprite(FxShape.Heart, p, sz, Fx.Fade(s == 0 ? pink : Rgb(255, 190, 220), MathF.Sin(k * MathF.PI)), FxBlend.Alpha, MathF.Sin(k * 9f) * 0.3f);
            }
        }
        foreach (float t0 in new[] { 0.5f, 0.7f })
        {
            PulseRing(fx, c.Age, c.From, t0, 0.4f, c.FromSize * 0.3f, c.FromSize * 1.1f, pink, White);
            PulseRing(fx, c.Age, c.To, t0 + 0.04f, 0.4f, c.ToSize * 0.3f, c.ToSize * 1.1f, pink, White);
        }
        Fx.Glint(fx, c.Age, (c.From + c.To) * 0.5f, c.ToSize, 0.4f, White, 0.5f);
    }

    /// <summary>Vacuum Wave: three small whirling crescents of air are sent spinning across one after another and strike the target, a punch of wind behind the last.</summary>
    private static void VacuumWaveY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var air = Rgb(190, 230, 255);
        for (int i = 0; i < 3; i++)
        {
            float t0 = 0.02f + i * 0.06f;
            float k = (c.Age - t0) / 0.28f;
            if (k < 0f || k > 1f) continue;
            var p = Vector3.Lerp(c.From, c.Aim, Fx.EaseOut(k)) + new Vector3(0f, (i - 1) * h * 0.2f, 0f);
            fx.Sprite(FxShape.Wave, p, h * (0.35f + 0.25f * k), Fx.Fade(air, (1f - k * 0.5f)), FxBlend.Alpha, k * 14f + i);
            fx.Sprite(FxShape.Swirl, p, h * 0.3f, Fx.Fade(White, 0.8f * (1f - k)), FxBlend.Alpha, -k * 10f);
        }
        SpeedLines(c, fx, 0.02f, 0.3f, 6, 0.8f);
        if (c.Cue.Landed)
        {
            Strike(c, fx, FxShape.Fist, new Vector3(-0.9f, 0f, 0.2f), I, 0.5f, Rgb(255, 224, 190), 0.3f, 0.06f);
            PulseRing(fx, c.Age, c.To, I, 0.4f, h * 0.3f, h * 1.4f, air, White);
            Jolt(fx, c.Age, I, 0.4f, 0.26f);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Switcheroo: three cups shuffle in the air between the two Pokémon, lifted to show the items beneath, which are whisked across to each side.</summary>
    private static void SwitcherooY10b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        var mid = (c.From + c.To) * 0.5f + Vector3.UnitY * h * 0.3f;
        float env = Fx.Envelope(c.Age, 0.04f, 0.12f, 0.8f, 1.1f);
        Color[] hues = { Rgb(255, 120, 100), Rgb(120, 190, 255), Rgb(255, 220, 100) };
        for (int i = 0; i < 3; i++)
        {
            float shuffle = MathF.Sin(c.Age * 11f + i * 2.1f) * h * 0.35f * Fx.Envelope(c.Age, 0.12f, 0.2f, 0.55f, 0.65f);
            var p = mid + new Vector3((i - 1) * h * 0.6f + shuffle * (i == 1 ? -1f : 1f), 0f, 0.1f);
            float lift = Fx.Envelope(c.Age, 0.6f, 0.7f, 0.85f, 0.95f);
            fx.Sprite(FxShape.Hex, p + Vector3.UnitY * lift * h * 0.35f, h * 0.38f, Fx.Fade(hues[i], env), FxBlend.Alpha);
            fx.Sprite(FxShape.Ring, p + Vector3.UnitY * lift * h * 0.35f, h * 0.2f, Fx.Fade(White, env * 0.7f));
        }
        float k = (c.Age - 0.62f) / 0.36f;
        if (k >= 0f && k <= 1f)
        {
            fx.Sprite(FxShape.Orb, Lob(mid, c.To, Fx.EaseOut(k), 0.05f), h * 0.22f, Fx.Fade(White, 1f), FxBlend.Alpha);
            fx.Sprite(FxShape.Orb, Lob(mid, c.From, Fx.EaseOut(k), -0.05f), h * 0.22f, Fx.Fade(Rgb(255, 210, 80), 1f), FxBlend.Alpha);
        }
        Fx.Burst(fx, c.Seed, c.Age, mid, h, 0.65f, 0.6f, 8, FxShape.Star, Rgb(255, 230, 120), Rgb(255, 150, 220), 0.1f, 1.0f, 0.6f, FxBlend.Alpha, spin: 6f, salt: 250);
    }

    /// <summary>Draco Meteor: the sky darkens blue and a rain of burning blue-violet meteors falls on the target, each ending in a blast, the last a vast one.</summary>
    private static void DracoMeteorY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var blue = Rgb(110, 120, 255);
        var fire = Rgb(150, 100, 255);
        float up = Fx.Envelope(c.Age, 0f, 0.1f, 0.2f, 0.3f);
        if (up > 0f) fx.Sprite(FxShape.Glow, c.From + new Vector3(0f, c.FromSize * 0.8f, 0f), c.FromSize * 1.2f, Fx.Fade(blue, up * 0.6f), FxBlend.Alpha);
        for (int i = 0; i < 6; i++)
        {
            float land = 0.2f + i * 0.04f;
            if (i == 5) land = I;
            float k = (c.Age - (land - 0.2f)) / 0.2f;
            var to = c.To + new Vector3(Fx.Signed(c.Seed, i, 443) * h * 0.55f, Fx.Signed(c.Seed, i, 444) * h * 0.3f, 0.1f);
            var from = to + new Vector3(h * 0.7f, h * 1.6f, 0f);
            if (k >= 0f && k <= 1f)
            {
                var p = Vector3.Lerp(from, to, k * k);
                var dir = to - from;
                fx.Streak(FxShape.Streak, p, dir, h * 0.2f, 3.2f, Fx.Fade(fire, 0.9f), FxBlend.Add);
                fx.Sprite(FxShape.Glow, p, h * 0.55f, Fx.Fade(blue, 0.7f));
                fx.Sprite(FxShape.Rock, p, h * 0.28f, Fx.Fade(Rgb(60, 50, 120), 1f), FxBlend.Alpha, k * 6f);
            }
            if (!c.Cue.Landed) continue;
            PulseRing(fx, c.Age, to, land, 0.3f, h * 0.1f, h * (i == 5 ? 0.9f : 0.5f), blue, White);
            Fx.Burst(fx, c.Seed, c.Age, to, h, land, 0.4f, i == 5 ? 8 : 4, FxShape.Flame, fire, blue, 0.14f, 0.8f, -0.3f, salt: 251 + i);
            Jolt(fx, c.Age, land, i == 5 ? 0.6f : 0.2f, 0.14f);
        }
        if (c.Cue.Landed) GroundRing(fx, c.Age, c.ToFeet, I, 0.5f, h * 2.0f, Fx.Fade(Rgb(190, 180, 255), 0.9f));
        Impact(c, col, fx, 1.3f);
    }

    /// <summary>Judgment: five pillars of white-gold light fall on the target one after another, a halo of rings turning over it and a great white flash at the last.</summary>
    private static void JudgmentY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var t = ColorsOf(c.Cue.Type);
        for (int i = 0; i < 5; i++)
        {
            float t0 = 0.03f + i * 0.06f;
            var foot = c.ToFeet + new Vector3((i - 2) * h * 0.35f, 0f, 0.1f);
            Pillar(c, fx, foot, t0, t0 + 0.4f, h * 0.22f, Rgb((t.Light.R + 255) / 2, (t.Light.G + 255) / 2, (t.Light.B + 255) / 2), White);
        }
        float halo = Fx.Envelope(c.Age, 0.1f, 0.2f, I, I + 0.3f);
        if (halo > 0f)
        {
            fx.Sprite(FxShape.Ring, c.ToFeet + new Vector3(0f, h * 1.1f, 0.2f), h * 0.9f, Fx.Fade(t.Light, halo), FxBlend.Alpha);
            fx.Sprite(FxShape.Ring, c.ToFeet + new Vector3(0f, h * 1.1f, 0.2f), h * 0.6f, Fx.Fade(White, halo * 0.8f));
        }
        if (c.Cue.Landed)
        {
            fx.ScreenFlash(White, 0.35f * Fx.Envelope(c.Age, I - 0.02f, I, I + 0.08f, I + 0.3f));
            GroundRing(fx, c.Age, c.ToFeet, I, 0.5f, h * 2.0f, Fx.Fade(White, 0.9f));
            PulseRing(fx, c.Age, c.To, I, 0.5f, h * 0.3f, h * 1.7f, t.Main, White);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 10, FxShape.Star, White, t.Light, 0.12f, 1.1f, 0.2f, FxBlend.Alpha, spin: 7f, salt: 257);
            Jolt(fx, c.Age, I, 0.7f, 0.36f);
        }
        Impact(c, col, fx, 1.3f);
    }

    /// <summary>Roar of Time: a clock face of rings and turning hands opens round the user and the time it holds is thrown across as a great beam, a second clock turning over the target as it breaks.</summary>
    private static void RoarOfTimeY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var time = Rgb(140, 170, 255);
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.2f, 0.3f);
        if (env > 0f)
        {
            fx.Sprite(FxShape.Ring, c.From, h * 1.3f, Fx.Fade(time, env), FxBlend.Alpha);
            fx.Sprite(FxShape.Ring, c.From, h * 0.9f, Fx.Fade(White, env * 0.8f));
            for (int i = 0; i < 12; i++)
            {
                float a = i * MathF.Tau / 12f;
                fx.Streak(FxShape.Streak, c.From + new Vector3(MathF.Cos(a), MathF.Sin(a), 0f) * h * 1.0f, new Vector3(MathF.Cos(a), MathF.Sin(a), 0f), h * 0.04f, 2f, Fx.Fade(White, env));
            }
            fx.Streak(FxShape.Beam, c.From + new Vector3(MathF.Cos(c.Age * 18f), MathF.Sin(c.Age * 18f), 0f) * h * 0.35f, new Vector3(MathF.Cos(c.Age * 18f), MathF.Sin(c.Age * 18f), 0f), h * 0.08f, 4f, Fx.Fade(White, env), FxBlend.Alpha);
        }
        Fx.Beam(fx, c, 0.2f, 0.8f, 0.26f, time, White, 8);
        if (c.Cue.Landed)
        {
            float clock = Fx.Envelope(c.Age, I, I + 0.05f, I + 0.45f, I + 0.7f);
            if (clock > 0f)
            {
                fx.Sprite(FxShape.Ring, c.To, c.ToSize * 1.5f, Fx.Fade(time, clock), FxBlend.Alpha);
                fx.Streak(FxShape.Beam, c.To + new Vector3(MathF.Cos(c.Age * -14f), MathF.Sin(c.Age * -14f), 0f) * c.ToSize * 0.4f, new Vector3(MathF.Cos(c.Age * -14f), MathF.Sin(c.Age * -14f), 0f), c.ToSize * 0.1f, 4f, Fx.Fade(White, clock), FxBlend.Alpha);
            }
            fx.ScreenFlash(White, 0.35f * Fx.Envelope(c.Age, I - 0.02f, I, I + 0.08f, I + 0.3f));
            PulseRing(fx, c.Age, c.To, I, 0.55f, c.ToSize * 0.3f, c.ToSize * 2.0f, time, White);
            Jolt(fx, c.Age, I, 0.8f, 0.5f);
        }
        Impact(c, col, fx, 1.35f);
    }

    /// <summary>Spacial Rend: a white-pearl slash tears across the target and space opens in its wake as a dark rift full of stars, widening and closing again.</summary>
    private static void SpacialRendY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var pearl = Rgb(255, 232, 250);
        Gather(fx, c.Age, c.Seed, c.From, c.FromSize, 0f, 0.22f, 8, pearl, Rgb(180, 150, 255), 0.08f, 0.9f);
        Crescent(fx, c.Age, c.To + new Vector3(-h * 1.0f, h * 0.6f, 0.2f), c.To + new Vector3(h * 0.9f, -h * 0.6f, 0.2f), I - 0.14f, 0.16f, h * 1.1f, h * 1.5f, pearl, White);
        float rift = Fx.Envelope(c.Age, I, I + 0.05f, I + 0.45f, I + 0.75f);
        if (rift > 0f)
        {
            float w = h * (0.15f + 0.3f * MathF.Sin(Math.Min(1f, (c.Age - I) / 0.5f) * MathF.PI));
            var dir = new Vector3(0.7f, -0.7f, 0f);
            fx.Streak(FxShape.Beam, c.To, dir, w * 1.4f, 3.2f, Fx.Fade(pearl, rift * 0.9f), FxBlend.Alpha);
            fx.Streak(FxShape.Beam, c.To, dir, w, 3.2f, Fx.Fade(Rgb(14, 8, 36), rift), FxBlend.Alpha);
            for (int i = 0; i < 6; i++)
                fx.Sprite(FxShape.Spark, c.To + dir * h * (i - 2.5f) * 0.28f + new Vector3(Fx.Signed(c.Seed, i, 445) * w * 0.3f, 0f, 0.1f), h * 0.07f, Fx.Fade(White, rift), rotation: c.Age * 6f + i);
        }
        if (c.Cue.Landed)
        {
            fx.ScreenFlash(White, 0.25f * Fx.Envelope(c.Age, I - 0.02f, I, I + 0.06f, I + 0.24f));
            Jolt(fx, c.Age, I, 0.7f, 0.4f);
        }
        Impact(c, col, fx, 1.3f);
    }

    /// <summary>Lunar Dance: a crescent moon rises over the user and a ring of pale stars dances round it, light drawn up into the moon as the user fades.</summary>
    private static void LunarDanceY10b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var moon = Rgb(220, 230, 255);
        float env = Fx.Envelope(c.Age, 0f, 0.2f, 0.8f, 1.15f);
        var disc = c.FromFeet + new Vector3(0f, h * 1.0f, 0.1f);
        fx.Sprite(FxShape.Glow, disc, h * 1.2f, Fx.Fade(moon, env * 0.5f), FxBlend.Alpha);
        fx.Sprite(FxShape.Wave, disc, h * 0.7f, Fx.Fade(Rgb(255, 250, 230), env), FxBlend.Alpha, MathF.PI * 0.75f);
        Orbit(fx, c.Age, c.FromFeet + Vector3.UnitY * h * 0.45f, h, 0.05f, 1.15f, 8, FxShape.Star, moon, 0.13f, 0.7f, 3.5f, 0.5f);
        Orbit(fx, c.Age, c.FromFeet + Vector3.UnitY * h * 0.45f, h, 0.2f, 1.15f, 5, FxShape.Spark, White, 0.1f, 0.5f, -5f, 0.5f);
        Rising(fx, c.Age, c.Seed, c.FromFeet + Vector3.UnitY * h * 0.2f, h, 0.2f, 1.1f, 4, FxShape.Heart, Rgb(255, 190, 220), 0.12f, 0.3f);
        Shaft(fx, c.Age, c.FromFeet, 0.15f, 0.8f, h * 1.1f, h * 0.4f, Fx.Fade(Rgb(200, 215, 255), 0.5f), White);
    }

    /// <summary>Crush Grip: two vast open palms close on the target from either side and squeeze, red rings tightening round it and cracks of force running out.</summary>
    private static void CrushGripY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = Fx.EaseOut(Math.Clamp((c.Age - 0.02f) / 0.3f, 0f, 1f));
        float fade = Fx.Envelope(c.Age, 0f, 0.04f, I + 0.2f, I + 0.4f);
        if (fade > 0f)
            for (int s = -1; s <= 1; s += 2)
                fx.Sprite(FxShape.Palm, c.To + new Vector3(s * h * (1.0f - 0.65f * k - (c.Age > I ? 0.04f : 0f)), 0f, 0.15f), h * 0.95f, Fx.Fade(Rgb(255, 196, 170), fade), FxBlend.Alpha, s < 0 ? -MathF.PI / 2f : MathF.PI / 2f);
        if (c.Cue.Landed)
        {
            for (int i = 0; i < 3; i++) PulseRing(fx, c.Age, c.To, I + i * 0.06f, 0.4f, h * (1.4f - 0.2f * i), h * 0.4f, Rgb(255, 80, 70), Rgb(255, 210, 190));
            for (int i = 0; i < 5; i++)
            {
                float a = i * MathF.Tau / 5f + 0.4f;
                fx.Flat(FxShape.Crack, c.To + new Vector3(0f, 0f, 0.15f), Vector3.UnitZ, h * 0.9f, Fx.Fade(Rgb(70, 30, 30), Fx.Envelope(c.Age, I, I + 0.03f, I + 0.5f, I + 0.75f)), FxBlend.Alpha, a, depth: false);
            }
            Jolt(fx, c.Age, I, 0.75f, 0.4f);
        }
        Impact(c, col, fx, 1.25f);
    }

    /// <summary>Magma Storm: a spiral of fire, lava and burning stones winds up round the target in a rising column and pours over it.</summary>
    private static void MagmaStormY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var fire = ColorsOf(PokemonType.Fire);
        float env = Fx.Envelope(c.Age, 0.04f, 0.2f, I + 0.45f, I + 0.75f);
        if (env > 0f)
            for (int i = 0; i < 18; i++)
            {
                float u = i / 18f;
                float a = u * MathF.Tau * 2f + c.Age * 8f;
                float r = h * (0.7f - 0.3f * u);
                var p = c.ToFeet + new Vector3(MathF.Cos(a) * r, h * (0.0f + 1.3f * u), MathF.Sin(a) * h * 0.3f);
                if (i % 3 == 0) fx.Sprite(FxShape.Rock, p, h * 0.2f, Fx.Fade(Rgb(255, 130, 50), env), FxBlend.Alpha, a);
                else fx.Sprite(FxShape.Flame, p, h * 0.3f, Fx.Fade(i % 2 == 0 ? fire.Main : Rgb(255, 200, 90), env), FxBlend.Add);
            }
        Shaft(fx, c.Age, c.ToFeet, 0.05f, 0.9f, h * 1.3f, h * 0.9f, Fx.Fade(Rgb(255, 110, 40), 0.5f), Rgb(255, 230, 150));
        if (c.Cue.Landed)
        {
            GroundRing(fx, c.Age, c.ToFeet, I, 0.5f, h * 2.0f, Fx.Fade(Rgb(255, 170, 70), 0.9f));
            Tint(c, fx, I, 1.1f, Rgb(255, 70, 30), 0.45f);
            Jolt(fx, c.Age, I, 0.55f, 0.4f);
        }
        Impact(c, col, fx, 1.15f);
    }

    /// <summary>Dark Void: a black hole opens over the foes and a violet-black swirl drags down over them, dark rings spreading, Z's drifting out of it.</summary>
    private static void DarkVoidY10b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        var dark = Rgb(24, 10, 44);
        float env = Fx.Envelope(c.Age, 0.02f, 0.2f, 0.85f, 1.15f);
        if (env <= 0f) return;
        float grow = Fx.EaseOut(Math.Min(1f, (c.Age - 0.02f) / 0.3f));
        fx.Sprite(FxShape.Glow, c.To, h * 2.0f * grow, Fx.Fade(Rgb(110, 50, 170), env * 0.5f), FxBlend.Alpha);
        fx.Sprite(FxShape.Swirl, c.To, h * 1.7f * grow, Fx.Fade(dark, env * 0.9f), FxBlend.Alpha, c.Age * 4f);
        fx.Sprite(FxShape.Swirl, c.To, h * 1.1f * grow, Fx.Fade(Rgb(150, 90, 220), env * 0.6f), FxBlend.Alpha, -c.Age * 6f);
        fx.Sprite(FxShape.Dot, c.To, h * 0.6f * grow, Fx.Fade(Rgb(4, 0, 10), env), FxBlend.Alpha);
        PulseRing(fx, c.Age, c.To, 0.1f, 0.5f, h * 0.3f, h * 1.6f, Rgb(80, 40, 140), Rgb(210, 170, 255));
        PulseRing(fx, c.Age, c.To, 0.3f, 0.5f, h * 0.3f, h * 1.6f, Rgb(80, 40, 140), Rgb(210, 170, 255));
        Rising(fx, c.Age, c.Seed, c.ToFeet + Vector3.UnitY * h * 0.5f, h, 0.4f, 1.2f, 3, FxShape.Zzz, Rgb(190, 160, 255), 0.18f, 0.25f);
    }

    /// <summary>Seed Flare: a point of white-green light gathers over the user, grows into a dazzling flare with long emerald rays and is flung across, bursting in a bloom of light and leaves.</summary>
    private static void SeedFlareY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var emerald = Rgb(110, 240, 150);
        var pt = c.From + new Vector3(0f, h * 0.2f, 0.1f);
        float env = Fx.Envelope(c.Age, 0f, 0.14f, 0.22f, 0.28f);
        Gather(fx, c.Age, c.Seed, pt, h, 0f, 0.22f, 10, emerald, White, 0.08f, 1.0f);
        if (env > 0f)
        {
            fx.Sprite(FxShape.Glow, pt, h * 1.2f * env, Fx.Fade(emerald, 0.7f * env));
            fx.Sprite(FxShape.Star, pt, h * 0.7f * env, Fx.Fade(White, env), FxBlend.Alpha, c.Age * 3f);
        }
        float k = (c.Age - 0.2f) / (I - 0.2f);
        if (k >= 0f && k <= 1f)
        {
            var p = Vector3.Lerp(pt, c.Aim, k);
            fx.Sprite(FxShape.Glow, p, h * 1.1f, Fx.Fade(emerald, 0.8f));
            fx.Sprite(FxShape.Star, p, h * 0.6f, Fx.Fade(White, 1f), FxBlend.Alpha, k * 8f);
            for (int i = 0; i < 4; i++)
            {
                float a = i * MathF.PI / 2f + k * 5f;
                fx.Streak(FxShape.Beam, p, new Vector3(MathF.Cos(a), MathF.Sin(a), 0f), h * 0.06f, 5f, Fx.Fade(emerald, 0.8f), FxBlend.Alpha);
            }
        }
        if (c.Cue.Landed)
        {
            fx.ScreenFlash(Rgb(240, 255, 240), 0.35f * Fx.Envelope(c.Age, I - 0.02f, I, I + 0.08f, I + 0.3f));
            PulseRing(fx, c.Age, c.To, I, 0.55f, c.ToSize * 0.3f, c.ToSize * 1.8f, emerald, White);
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.7f, 10, FxShape.Leaf, emerald, White, 0.13f, 1.1f, 0.4f, FxBlend.Alpha, spin: 7f, salt: 258);
            Jolt(fx, c.Age, I, 0.7f, 0.4f);
        }
        Impact(c, col, fx, 1.3f);
    }

    /// <summary>Shadow Force: the user melts into a violet-black shadow that slides across the ground, and rises over the target in great dark claws that slam down on it.</summary>
    private static void ShadowForceY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var shadow = Rgb(50, 20, 90);
        float melt = Fx.Envelope(c.Age, 0f, 0.08f, 0.2f, 0.3f);
        if (melt > 0f)
        {
            fx.Sprite(FxShape.Swirl, c.From, c.FromSize * 1.3f, Fx.Fade(shadow, melt * 0.9f), FxBlend.Alpha, c.Age * 8f);
            fx.Sprite(FxShape.Glow, c.From, c.FromSize * 1.2f, Fx.Fade(Rgb(120, 60, 190), melt * 0.5f), FxBlend.Alpha);
        }
        float k = (c.Age - 0.12f) / 0.22f;
        if (k >= 0f && k <= 1f)
        {
            var p = Vector3.Lerp(c.FromFeet, c.ToFeet, Fx.EaseOut(k)) + Vector3.UnitY * 0.1f;
            fx.Flat(FxShape.Smoke, p, Vector3.UnitY, h * 1.0f, Fx.Fade(shadow, 0.9f * MathF.Sin(k * MathF.PI)), FxBlend.Alpha, k * 6f, depth: true);
        }
        float rise = Fx.Envelope(c.Age, I - 0.1f, I - 0.04f, I + 0.1f, I + 0.35f);
        if (rise > 0f)
        {
            fx.Sprite(FxShape.Smoke, c.ToFeet + new Vector3(0f, h * 0.7f, 0.1f), h * 1.5f, Fx.Fade(shadow, rise * 0.7f), FxBlend.Alpha);
            ClawMarks(c, fx, I - 0.04f, 1.2f, Rgb(180, 120, 255), Rgb(236, 220, 255), 0.4f);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 8, FxShape.Smoke, Rgb(40, 20, 80, 220), Rgb(100, 60, 160, 200), 0.26f, 0.8f, -0.2f, FxBlend.Alpha, salt: 259);
            PulseRing(fx, c.Age, c.To, I, 0.5f, h * 0.3f, h * 1.6f, shadow, Rgb(200, 150, 255));
            Tint(c, fx, I, 1.0f, shadow, 0.5f);
            Jolt(fx, c.Age, I, 0.7f, 0.36f);
        }
        Impact(c, col, fx, 1.25f);
    }
}
