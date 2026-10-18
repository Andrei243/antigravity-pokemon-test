using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

// Plan 25 - Y11, its first part (Y11a), second half: Round to Bulldoze. See MoveFx.Later1.cs.
internal static partial class MoveFx
{
    /// <summary>Round: two rings of notes circle the foe in opposite ways, taking up the tune one after another, and ring out.</summary>
    private static void RoundY11(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Orbit(fx, c.Age, c.To, h, 0.04f, 1.05f, 5, FxShape.Note, Rgb(255, 214, 150), 0.22f, 0.8f, 4.5f, 0.5f);
        Orbit(fx, c.Age, c.To, h, 0.14f, 1.1f, 5, FxShape.Note, Rgb(255, 160, 200), 0.2f, 0.55f, -5f, 0.4f);
        if (c.Cue.Landed)
        {
            PulseRing(fx, c.Age, c.To, I, 0.4f, h * 0.4f, h * 1.5f, Rgb(255, 224, 170), White);
            PulseRing(fx, c.Age, c.To, I + 0.1f, 0.4f, h * 0.3f, h * 1.2f, Rgb(255, 170, 210), White);
        }
        Impact(c, col, fx, 0.8f);
    }

    /// <summary>Echoed Voice: a ring of sound leaves the user, then the same ring again, each a little paler and smaller, rolling after it.</summary>
    private static void EchoedVoiceY11(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        for (int i = 0; i < 4; i++)
        {
            float k = (c.Age - 0.02f - i * 0.1f) / (I - 0.02f);
            if (k < 0f || k > 1.25f) continue;
            var p = Vector3.Lerp(c.From, c.Aim, Math.Min(1f, k));
            float size = c.FromSize * (0.35f + 0.55f * k) * (1f - 0.16f * i);
            float a = (1f - 0.22f * i) * (k > 1f ? 1f - (k - 1f) * 4f : 1f);
            fx.Sprite(FxShape.Ring, p, size, Fx.Fade(Rgb(255, 224, 190), a * 0.9f), FxBlend.Alpha);
            fx.Sprite(FxShape.Wave, p, size * 0.9f, Fx.Fade(Rgb(255, 186, 150), a * 0.7f), FxBlend.Alpha, ScreenAngle(c.Aim - c.From));
        }
        if (c.Cue.Landed)
            for (int i = 0; i < 3; i++)
                PulseRing(fx, c.Age, c.To, I + i * 0.1f, 0.35f, h * 0.3f, h * (1.2f - 0.25f * i), Rgb(255, 214, 180), White);
        Impact(c, col, fx, 0.85f);
    }

    /// <summary>Chip Away: three quick chisel strokes across the target and flecks of its defence chipped off and flying.</summary>
    private static void ChipAwayY11(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        if (c.Cue.Landed)
        {
            Fx.Slash(fx, c.Age, c.Seed, c.To, h * 1.8f, I - 0.12f, 3, 0.06f, Rgb(250, 246, 236), Rgb(120, 110, 100));
            for (int i = 0; i < 3; i++)
                Fx.Burst(fx, c.Seed, c.Age, c.To + new Vector3(Fx.Signed(c.Seed, i, 460) * h * 0.25f, Fx.Signed(c.Seed, i, 461) * h * 0.25f, 0.1f), h, I - 0.12f + i * 0.06f, 0.45f, 4, FxShape.Shard, Rgb(240, 236, 226), Rgb(190, 184, 170), 0.09f, 1.0f, 1.8f, FxBlend.Alpha, spin: 7f, salt: 242 + i);
        }
        SpeedLines(c, fx, 0.05f, 0.3f, 4, 0.4f);
        Impact(c, col, fx, 0.85f);
    }

    /// <summary>Clear Smog: a thick purple-grey cloud of smog bursts on the target, then pale streaks sweep through it and clear it away.</summary>
    private static void ClearSmogY11(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Fx.Orb(fx, c, 0.02f, I, 0.26f, Rgb(120, 70, 150), Rgb(190, 140, 220), FxShape.Smoke, Rgb(90, 56, 110));
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.8f, 10, FxShape.Smoke, Rgb(120, 80, 150, 220), Rgb(80, 56, 100, 200), 0.28f, 0.6f, -0.2f, FxBlend.Alpha, salt: 243);
            float k = (c.Age - I - 0.2f) / 0.5f;
            if (k > 0f && k < 1f)
                for (int i = 0; i < 6; i++)
                {
                    float a = i * MathF.Tau / 6f + 0.4f;
                    fx.Streak(FxShape.Streak, c.To + new Vector3(MathF.Cos(a), MathF.Sin(a), 0f) * h * (0.2f + 0.8f * k), new Vector3(MathF.Cos(a), MathF.Sin(a), 0f), h * 0.07f, 5f, Fx.Fade(Rgb(236, 255, 250), 1f - k));
                }
        }
        Impact(c, col, fx, 0.8f);
    }

    /// <summary>Stored Power: small pink stars that have been saved up circle the user, glow, and are all let fly across at once.</summary>
    private static void StoredPowerY11(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float hold = Fx.Envelope(c.Age, 0f, 0.1f, I - 0.12f, I - 0.02f);
        if (hold > 0f)
        {
            Orbit(fx, c.Age, c.From + Vector3.UnitY * h * 0.1f, h, 0f, I - 0.02f, 8, FxShape.Star, Rgb(255, 150, 210), 0.16f, 0.65f, 6f, 0.6f);
            fx.Sprite(FxShape.Glow, c.From, h * 0.8f, Fx.Fade(col.Main, hold * 0.35f), FxBlend.Alpha);
        }
        Fx.Stream(fx, c, I - 0.1f, I + 0.1f, 0.22f, 8, FxShape.Star, Rgb(255, 170, 220), White, 0.14f, arc: 0.04f, spread: 0.35f, spin: 6f, blend: FxBlend.Alpha);
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Quick Guard: one tall orange screen snaps up in front of the user, stray streaks of speed glancing off it.</summary>
    private static void QuickGuardY11(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var f = ColorsOf(PokemonType.Fighting);
        var towards = L1Toward(c);
        float env = Fx.Envelope(c.Age, 0.02f, 0.08f, 0.8f, 1.05f);
        if (env <= 0f) return;
        var at = c.FromFeet + towards * h * 0.8f + new Vector3(0f, 0f, 0.2f);
        float rise = Fx.EaseOut(Math.Clamp(c.Age / 0.1f, 0f, 1f));
        fx.Streak(FxShape.Beam, at + Vector3.UnitY * h * 0.5f, Vector3.UnitY, h * 0.18f, 2.8f * rise, Fx.Fade(f.Main, env * 0.8f), FxBlend.Alpha);
        for (int i = 0; i < 3; i++)
            fx.Sprite(FxShape.Hex, at + Vector3.UnitY * h * (0.2f + 0.3f * i), h * 0.36f, Fx.Fade(f.Light, env * 0.45f), FxBlend.Alpha);
        for (int i = 0; i < 4; i++)
        {
            float k = (c.Age * 2f - 0.15f * i + 0.5f) % 1f;
            var p = at + new Vector3(towards.X * h * (0.9f - 1.0f * k), h * (0.2f + 0.2f * i), 0f);
            fx.Streak(FxShape.Streak, p + new Vector3(0f, h * 0.15f * k, 0f), new Vector3(1f, 0.6f, 0f), h * 0.05f, 6f, Fx.Fade(White, env * 0.8f * MathF.Sin(k * MathF.PI)));
        }
        Fx.Glint(fx, c.Age, at + Vector3.UnitY * h * 0.5f, h, 0.2f, White, 0.45f);
    }

    /// <summary>Ally Switch: two glowing silhouettes of the user trade places in a sweep, pink swirls curling round the crossing.</summary>
    private static void AllySwitchY11(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float k = Fx.EaseOut(Math.Clamp((c.Age - 0.05f) / 0.55f, 0f, 1f));
        float env = Fx.Envelope(c.Age, 0.02f, 0.1f, 0.8f, 1.05f);
        if (env <= 0f) return;
        for (int s = -1; s <= 1; s += 2)
        {
            float x = s * h * 1.4f * (1f - 2f * k);
            float y = h * 0.3f * MathF.Sin(k * MathF.PI) * s;
            var p = c.FromFeet + new Vector3(x, h * 0.5f + y, 0.15f);
            fx.Sprite(FxShape.Glow, p, h * 1.0f, Fx.Fade(s < 0 ? col.Main : col.Light, env * 0.55f), FxBlend.Alpha);
            fx.Sprite(FxShape.Wisp, p, h * 0.9f, Fx.Fade(s < 0 ? col.Main : col.Light, env * 0.7f), FxBlend.Alpha);
            fx.Streak(FxShape.Streak, p, Vector3.UnitX * -s, h * 0.1f, 5f, Fx.Fade(White, env * 0.7f));
        }
        for (int i = 0; i < 3; i++)
            fx.Sprite(FxShape.Swirl, c.FromFeet + new Vector3(0f, h * 0.5f, 0.2f), h * (0.6f + 0.2f * i), Fx.Fade(col.Light, env * 0.5f * MathF.Sin(k * MathF.PI)), FxBlend.Alpha, c.Age * (6f + i));
    }

    /// <summary>Scald: a jet of near-boiling water thrown across, steam rolling off it and off the target, red with the heat.</summary>
    private static void ScaldY11(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Fx.Stream(fx, c, 0f, I, 0.2f, 12, FxShape.Drop, Rgb(120, 190, 255), Rgb(220, 244, 255), 0.26f, arc: 0.04f, spread: 0.25f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0f, I, 0.2f, 6, FxShape.Smoke, Rgb(240, 244, 250, 220), Rgb(200, 210, 224, 200), 0.32f, arc: 0.06f, spread: 0.3f, blend: FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.9f, 8, FxShape.Smoke, Rgb(245, 248, 252, 230), Rgb(210, 220, 232, 200), 0.26f, 0.45f, -1.2f, FxBlend.Alpha, salt: 244);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.4f, 6, FxShape.Flame, Rgb(255, 120, 90), Rgb(255, 200, 150), 0.07f, 0.6f, -0.8f, salt: 245);
            Splash(c, fx, c.To, h, I, Rgb(150, 210, 255), White, 6, 2);
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Shell Smash: the user's shell bursts open in cream-white pieces flung out on a ring, red sparks rushing outward and blue ones in.</summary>
    private static void ShellSmashY11(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        Fx.Burst(fx, c.Seed, c.Age, c.From, h, 0.12f, 0.8f, 12, FxShape.Shard, Rgb(255, 244, 220), Rgb(214, 190, 160), 0.15f, 1.3f, 1.4f, FxBlend.Alpha, spin: 6f, salt: 246);
        PulseRing(fx, c.Age, c.From, 0.12f, 0.45f, h * 0.4f, h * 1.8f, Rgb(255, 230, 190), White);
        Fx.Burst(fx, c.Seed, c.Age, c.From, h, 0.14f, 0.55f, 8, FxShape.Streak, Rgb(255, 110, 80), Rgb(255, 210, 160), 0.06f, 1.2f, 0f, streak: 3f, salt: 247);
        float env = Fx.Envelope(c.Age, 0.3f, 0.4f, 0.8f, 1.05f);
        if (env > 0f)
            for (int i = 0; i < 6; i++)
            {
                float u = (c.Age * 1.5f + i / 6f) % 1f;
                float a = i * MathF.Tau / 6f;
                fx.Sprite(FxShape.Spark, c.From + new Vector3(MathF.Cos(a), MathF.Sin(a) * 0.6f, 0f) * h * 0.9f * (1f - u), h * 0.1f, Fx.Fade(Rgb(120, 180, 255), env * MathF.Sin(u * MathF.PI)));
            }
        Fx.Glint(fx, c.Age, c.From, h, 0.14f, White, 0.5f);
    }

    /// <summary>Heal Pulse: a soft pink pulse rolls out of the user to the target and rings it twice, hearts rising and the body glowing.</summary>
    private static void HealPulseY11(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        PulseRing(fx, c.Age, c.From, 0.02f, 0.3f, c.FromSize * 0.3f, c.FromSize * 1.0f, Rgb(255, 150, 200), Rgb(255, 230, 240));
        Fx.Stream(fx, c, 0.04f, I, 0.26f, 5, FxShape.Ring, Rgb(255, 160, 210), White, 0.14f, spread: 0.06f, blend: FxBlend.Alpha);
        PulseRing(fx, c.Age, c.To, I - 0.02f, 0.4f, h * 0.3f, h * 1.2f, Rgb(255, 150, 200), White);
        PulseRing(fx, c.Age, c.To, I + 0.15f, 0.4f, h * 0.3f, h * 1.0f, Rgb(255, 190, 220), White);
        Fx.Aura(fx, c.Age, c.Seed, c.ToFeet, h, 0.15f, 1.15f, Rgb(255, 130, 190), Rgb(255, 226, 240), 10);
        Rising(fx, c.Age, c.Seed, c.ToFeet + Vector3.UnitY * h * 0.4f, h, 0.2f, 1.2f, 4, FxShape.Heart, Rgb(255, 140, 190), 0.2f, 0.2f);
        Tint(c, fx, I, 1.0f, Rgb(255, 190, 220), 0.3f);
    }

    /// <summary>Hex: a purple hexagram of six linked hexes turns round the target and tightens on it, wisps rising from each point.</summary>
    private static void HexY11(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0.02f, 0.14f, I + 0.1f, I + 0.35f);
        if (env > 0f)
        {
            float close = 1f - 0.35f * Fx.EaseOut(Math.Clamp((c.Age - 0.1f) / (I - 0.1f), 0f, 1f));
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathF.Tau / 6f + c.Age * 3f;
                var p = c.To + new Vector3(MathF.Cos(a) * h * 0.95f * close, MathF.Sin(a) * h * 0.6f * close, 0.12f);
                fx.Sprite(FxShape.Hex, p, h * 0.3f, Fx.Fade(col.Main, env), FxBlend.Alpha, a);
                fx.Sprite(FxShape.Hex, p, h * 0.34f, Fx.Fade(col.Light, env * 0.4f), rotation: a);
                fx.Sprite(FxShape.Wisp, p + new Vector3(0f, h * 0.22f, 0f), h * 0.16f, Fx.Fade(col.Light, env * 0.8f), FxBlend.Alpha);
            }
            fx.Sprite(FxShape.Target, c.To, h * 0.8f * close, Fx.Fade(col.Light, env * 0.5f), FxBlend.Alpha, c.Age * 2f);
        }
        if (c.Cue.Landed) Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 8, FxShape.Wisp, col.Light, col.Main, 0.15f, 0.7f, -0.6f, salt: 248);
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Sky Drop: a whirl of wind lifts the target off the ground, two talons close on it, and it is dropped back with a thud and a ring of dust.</summary>
    private static void SkyDropY11(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0.02f, 0.1f, I - 0.04f, I + 0.1f);
        if (env > 0f)
        {
            L1Spiral(fx, c.Age, c.Seed, c.ToFeet, h, 0.02f, I + 0.1f, 8, FxShape.Feather, White, col.Main, 0.16f, 0.6f, 1.4f);
            float lift = Fx.EaseOut(Math.Clamp((c.Age - 0.05f) / 0.22f, 0f, 1f));
            for (int s = -1; s <= 1; s += 2)
                fx.Sprite(FxShape.Claw, c.To + new Vector3(s * h * (0.8f - 0.4f * lift), h * (0.85f - 0.1f * lift), 0.2f), h * 0.7f, Fx.Fade(Rgb(236, 226, 206), env), FxBlend.Alpha, s * 0.3f + MathF.PI);
        }
        if (c.Cue.Landed)
        {
            GroundRing(fx, c.Age, c.ToFeet, I, 0.4f, h * 1.4f, col.Light);
            Dust(c, fx, c.ToFeet, h, I, 7);
            Jolt(fx, c.Age, I, 0.4f, 0.2f);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Shift Gear: two interlocking cogs turn against each other at the user's side, sparks flying from their teeth, and speed rushes up.</summary>
    private static void ShiftGearY11(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var steel = ColorsOf(PokemonType.Steel);
        float env = Fx.Envelope(c.Age, 0.02f, 0.12f, 0.85f, 1.1f);
        if (env <= 0f) return;
        for (int g = 0; g < 2; g++)
        {
            float dir = g == 0 ? 1f : -1f;
            var at = c.FromFeet + new Vector3((g == 0 ? -0.26f : 0.32f) * h, h * (0.65f - 0.18f * g), 0.2f);
            float size = h * (g == 0 ? 0.62f : 0.46f);
            float ang = dir * c.Age * (g == 0 ? 5f : 6.8f);
            fx.Sprite(FxShape.Star, at, size * 1.15f, Fx.Fade(Rgb(150, 160, 180), env), FxBlend.Alpha, ang);
            fx.Sprite(FxShape.Hex, at, size * 0.85f, Fx.Fade(steel.Main, env), FxBlend.Alpha, ang);
            fx.Sprite(FxShape.Ring, at, size * 0.45f, Fx.Fade(Rgb(90, 100, 120), env), FxBlend.Alpha);
        }
        for (int i = 0; i < 4; i++)
        {
            float k = (c.Age * 2.2f + i / 4f) % 1f;
            fx.Sprite(FxShape.Spark, c.FromFeet + new Vector3(0.03f * h + Fx.Signed(c.Seed, i, 470) * h * 0.12f, h * (0.6f + 0.25f * k), 0.25f), h * 0.1f, Fx.Fade(Rgb(255, 230, 150), env * MathF.Sin(k * MathF.PI)));
        }
        for (int i = 0; i < 3; i++)
        {
            float u = (c.Age * 1.4f + i / 3f) % 1f;
            fx.Streak(FxShape.Streak, c.FromFeet + new Vector3((i - 1) * h * 0.5f, h * (0.1f + 0.9f * u), 0.1f), Vector3.UnitY, h * 0.05f, 6f, Fx.Fade(White, env * 0.7f * MathF.Sin(u * MathF.PI)));
        }
    }

    /// <summary>Circle Throw: the target is swung round the user on a ring of orange speed, a streak following it, and thrown away with a shock.</summary>
    private static void CircleThrowY11(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0.02f, 0.1f, I - 0.04f, I + 0.1f);
        if (env > 0f)
        {
            float head = c.Age * 22f;
            for (int i = 0; i < 10; i++)
            {
                float a = head - i * 0.3f;
                var p = c.To + new Vector3(MathF.Cos(a) * h * 0.95f, MathF.Sin(a) * h * 0.5f, 0.12f);
                fx.Sprite(FxShape.Dot, p, h * (0.34f - 0.026f * i), Fx.Fade(i < 3 ? White : Rgb(255, 150, 60), env * (1f - i / 11f)), FxBlend.Alpha);
            }
            fx.Sprite(FxShape.Ring, c.To, h * 1.2f, Fx.Fade(col.Main, env * 0.4f), FxBlend.Alpha);
        }
        Strike(c, fx, FxShape.Fist, new Vector3(-1.0f, 0.1f, 0.2f), I, 0.4f, Rgb(255, 224, 196), -0.5f, 0.14f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.45f, 8, FxShape.Streak, col.Light, White, 0.06f, 1.4f, 0f, streak: 3f, salt: 249);
            GroundRing(fx, c.Age, c.ToFeet, I, 0.4f, h * 1.2f, col.Light);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Incinerate: flames sweep over the target and a red berry on it is burnt to a puff of grey ash that drifts away.</summary>
    private static void IncinerateY11(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        FlameStream(c, col, fx, 10, 0.2f, I);
        float berry = Fx.Envelope(c.Age, 0.06f, 0.14f, I + 0.05f, I + 0.1f);
        var at = c.To + new Vector3(h * 0.45f, h * 0.7f, 0.2f);
        if (berry > 0f)
        {
            fx.Sprite(FxShape.Orb, at, h * 0.2f, Fx.Fade(Rgb(230, 60, 80), berry), FxBlend.Alpha);
            fx.Sprite(FxShape.Leaf, at + new Vector3(0f, h * 0.12f, 0f), h * 0.12f, Fx.Fade(Rgb(90, 190, 80), berry), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, at, h, I + 0.05f, 0.8f, 6, FxShape.Smoke, Rgb(150, 146, 140, 220), Rgb(100, 96, 92, 200), 0.14f, 0.35f, -0.9f, FxBlend.Alpha, salt: 250);
            Fx.Burst(fx, c.Seed, c.Age, at, h, I + 0.05f, 0.5f, 6, FxShape.Ember, col.Light, col.Main, 0.07f, 0.8f, -0.4f, salt: 251);
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Quash: a vast dark hand comes down on the foe and presses it flat, smoke rolling out under the weight.</summary>
    private static void QuashY11(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        float k = (c.Age - 0.05f) / (I - 0.05f);
        float fade = Fx.Envelope(c.Age, 0.04f, 0.1f, I + 0.25f, I + 0.5f);
        if (fade > 0f)
        {
            float down = Fx.EaseOut(Math.Clamp(k, 0f, 1f));
            var p = c.To + new Vector3(0f, h * (0.95f - 0.45f * down), 0.2f);
            fx.Sprite(FxShape.Glow, p, h * 1.5f, Fx.Fade(col.Dark, fade * 0.7f), FxBlend.Alpha);
            fx.Sprite(FxShape.Palm, p, h * 0.8f, Fx.Fade(Rgb(70, 46, 90), fade), FxBlend.Alpha, MathF.PI);
            fx.Sprite(FxShape.Palm, p, h * 0.86f, Fx.Fade(col.Main, fade * 0.35f), rotation: MathF.PI);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + Vector3.UnitY * h * 0.1f, h, I, 0.7f, 8, FxShape.Smoke, Rgb(70, 50, 80, 230), Rgb(40, 28, 46, 220), 0.24f, 0.65f, -0.1f, FxBlend.Alpha, salt: 252);
            GroundRing(fx, c.Age, c.ToFeet, I, 0.4f, h * 1.4f, col.Main);
            Jolt(fx, c.Age, I, 0.35f, 0.22f);
        }
    }

    /// <summary>Acrobatics: the user loops and tumbles through the air toward the target in a streaming white ribbon of feathers and cuts it on the last turn.</summary>
    private static void AcrobaticsY11(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = Math.Clamp((c.Age - 0.02f) / (I - 0.02f), 0f, 1f);
        float fade = Fx.Envelope(c.Age, 0f, 0.03f, I + 0.1f, I + 0.4f);
        if (fade > 0f)
        {
            const int n = 14;
            var pts = new Vector3[n];
            var path = c.Aim - c.From;
            for (int i = 0; i < n; i++)
            {
                float t = Math.Max(0f, k - (n - 1 - i) * 0.05f);
                float loop = t * MathF.Tau * 1.5f;
                pts[i] = c.From + path * Fx.EaseOut(t) + new Vector3(0f, MathF.Sin(loop) * c.FromSize * 0.6f * MathF.Sin(t * MathF.PI), MathF.Cos(loop) * 0.1f);
            }
            fx.Ribbon(pts, c.FromSize * 0.12f, Fx.Fade(Rgb(220, 236, 255), 0.85f * fade), FxBlend.Alpha);
            fx.Ribbon(pts, c.FromSize * 0.04f, Fx.Fade(White, fade));
            for (int i = 2; i < n; i += 3)
                fx.Sprite(FxShape.Feather, pts[i], c.FromSize * 0.25f, Fx.Fade(White, fade * i / n), FxBlend.Alpha, i * 1.3f);
        }
        if (c.Cue.Landed) Fx.Slash(fx, c.Age, c.Seed, c.To, h, I - 0.04f, 2, 0.07f, Rgb(210, 230, 255), White, FxShape.Wave);
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Reflect Type: a pale mirror panel catches the foe, a sweep of light crosses it, and the foe's colours pour across onto the user.</summary>
    private static void ReflectTypeY11(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0.02f, 0.12f, 0.5f, 0.7f);
        if (env > 0f)
        {
            fx.Sprite(FxShape.Hex, c.To, h * 1.2f, Fx.Fade(Rgb(214, 232, 255), env * 0.45f), FxBlend.Alpha);
            fx.Sprite(FxShape.Hex, c.To, h * 1.28f, Fx.Fade(White, env * 0.3f));
            float k = (c.Age - 0.12f) / 0.35f;
            if (k > 0f && k < 1f) fx.Streak(FxShape.Beam, c.To + new Vector3(h * (-0.8f + 1.6f * k), 0f, 0.2f), new Vector3(0.5f, 1f, 0f), h * 0.12f, 3f, Fx.Fade(White, env * MathF.Sin(k * MathF.PI)), FxBlend.Alpha);
        }
        Fx.Stream(fx, c, 0.3f, 0.7f, 0.3f, 12, FxShape.Glow, Rgb(255, 170, 210), Rgb(150, 220, 255), 0.13f, arc: 0.08f, spread: 0.3f, blend: FxBlend.Alpha, reverse: true);
        float after = Fx.Envelope(c.Age, 0.65f, 0.75f, 0.9f, 1.1f);
        if (after > 0f)
        {
            fx.Flat(FxShape.Ring, c.FromFeet + Vector3.UnitY * c.FromSize * 0.5f, Vector3.UnitY, c.FromSize * 1.0f, Fx.Fade(Rgb(180, 220, 255), after), depth: true);
            Fx.Glint(fx, c.Age, c.From, c.FromSize, 0.66f, White, 0.45f);
        }
    }

    /// <summary>Retaliate: pale spirits of fallen friends rise behind the user and rush with it at the target, streaming light behind them.</summary>
    private static void RetaliateY11(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float k = Math.Clamp((c.Age - 0.12f) / (I - 0.12f), 0f, 1f);
        float fade = Fx.Envelope(c.Age, 0f, 0.1f, I + 0.05f, I + 0.35f);
        if (fade > 0f)
            for (int i = 0; i < 3; i++)
            {
                float rise = Fx.EaseOut(Math.Clamp(c.Age / 0.16f, 0f, 1f));
                var home = c.From + new Vector3((i - 1) * h * 0.6f, h * (0.2f + 0.5f * rise), -0.1f);
                var p = Vector3.Lerp(home, c.Aim + new Vector3((i - 1) * c.ToSize * 0.3f, 0f, 0f), k * k);
                fx.Sprite(FxShape.Glow, p, h * 0.6f, Fx.Fade(Rgb(120, 130, 220), fade * 0.5f), FxBlend.Alpha);
                fx.Sprite(FxShape.Wisp, p, h * 0.55f, Fx.Fade(Rgb(236, 240, 255), fade), FxBlend.Alpha);
                fx.Sprite(FxShape.Eye, p + new Vector3(0f, h * 0.1f, 0.1f), h * 0.13f, Fx.Fade(Rgb(80, 70, 160), fade), FxBlend.Alpha);
                fx.Streak(FxShape.Streak, p, c.Aim - c.From, h * 0.07f, 5f * k + 1f, Fx.Fade(White, fade * 0.7f * k));
            }
        SpeedLines(c, fx, 0.1f, I, 4, 0.4f);
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Final Gambit: the user gathers all it has into a blinding gold-white light, fires it in one beam and sags, its own ring fading.</summary>
    private static void FinalGambitY11(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var gold = Rgb(255, 214, 110);
        float charge = Fx.Envelope(c.Age, 0f, 0.1f, I - 0.04f, I + 0.04f);
        if (charge > 0f)
        {
            Gather(fx, c.Age, c.Seed, c.From, h, 0f, I + 0.02f, 12, gold, White, 0.1f, 1.1f);
            fx.Sprite(FxShape.Glow, c.From, h * (0.5f + 0.8f * c.Age / I), Fx.Fade(gold, charge * 0.6f));
        }
        Fx.Beam(fx, c, I - 0.1f, 0.75f, 0.18f, gold, White, 12);
        float after = Fx.Envelope(c.Age, I, I + 0.04f, 0.8f, 1.05f);
        if (after > 0f) fx.Flat(FxShape.Ring, c.FromFeet + Vector3.UnitY * 0.04f, Vector3.UnitY, h * 0.8f, Fx.Fade(col.Main, after * 0.7f), depth: true);
        if (c.Cue.Landed)
        {
            fx.ScreenFlash(Rgb(255, 244, 210), 0.3f * Fx.Envelope(c.Age, I - 0.02f, I + 0.03f, I + 0.06f, I + 0.22f));
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.6f, 10, FxShape.Star, gold, White, 0.12f, 1.2f, 0.8f, FxBlend.Alpha, spin: 6f, salt: 253);
        }
        Impact(c, col, fx, 1.25f);
    }

    /// <summary>Bestow: a small gift tied with a pink ribbon is lobbed across to the target, hearts and sparkles in its wake.</summary>
    private static void BestowY11(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float k = (c.Age - 0.03f) / (I - 0.03f);
        float h = c.ToSize;
        if (k > 0f && k < 1f)
        {
            var p = Lob(c.From, c.Aim, k, 0.04f);
            fx.Sprite(FxShape.Glow, p, h * 0.7f, Fx.Fade(Rgb(255, 200, 220), 0.55f));
            fx.Sprite(FxShape.Hex, p, h * 0.34f, Fx.Fade(Rgb(255, 244, 226), 1f), FxBlend.Alpha, k * 5f);
            fx.Sprite(FxShape.Cross, p, h * 0.34f, Fx.Fade(Rgb(255, 110, 160), 1f), FxBlend.Alpha, k * 5f);
        }
        for (int i = 0; i < 6; i++)
        {
            float t = k - i * 0.07f;
            if (t < 0f || t > 1f) continue;
            fx.Sprite(i % 2 == 0 ? FxShape.Heart : FxShape.Spark, Lob(c.From, c.Aim, t, 0.04f) + new Vector3(0f, h * 0.1f, 0f), h * 0.11f, Fx.Fade(Rgb(255, 170, 210), 1f - i / 7f), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 8, FxShape.Heart, Rgb(255, 150, 200), Rgb(255, 230, 240), 0.1f, 0.7f, -0.4f, FxBlend.Alpha, salt: 254);
            Fx.Glint(fx, c.Age, c.To, h, I, White, 0.5f);
        }
    }

    /// <summary>Inferno: a spiral of red and violet-tinged flame winds up round the target in a roaring pillar, the ground burning in a ring.</summary>
    private static void InfernoY11(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        L1Spiral(fx, c.Age, c.Seed, c.ToFeet, h, 0.04f, 1.1f, 16, FxShape.Flame, Rgb(255, 70, 40), Rgb(255, 190, 90), 0.34f, 0.8f, 1.7f);
        L1Spiral(fx, c.Age, c.Seed + 3, c.ToFeet, h, 0.2f, 1.1f, 8, FxShape.Flame, Rgb(200, 60, 150), Rgb(255, 120, 120), 0.26f, 0.5f, 1.9f);
        float ring = Fx.Envelope(c.Age, 0.04f, 0.2f, 0.85f, 1.1f);
        if (ring > 0f) fx.Flat(FxShape.Ring, c.ToFeet + Vector3.UnitY * 0.04f, Vector3.UnitY, h * 1.3f, Fx.Fade(Rgb(255, 90, 40), ring), depth: true);
        if (c.Cue.Landed) Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 8, FxShape.Ember, col.Light, col.Main, 0.08f, 1.0f, -0.7f, salt: 255);
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Water Pledge: a tall pale-blue column of water rises under the target, rings of foam winding up it, and breaks in a wide splash.</summary>
    private static void WaterPledgeY11(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        L1Column(c, fx, Rgb(80, 160, 255), Rgb(214, 240, 255), col.Light, 0.1f, 0.34f);
        L1Spiral(fx, c.Age, c.Seed, c.ToFeet, h, 0.14f, 0.95f, 8, FxShape.Bubble, White, col.Light, 0.16f, 0.45f, 1.25f);
        if (c.Cue.Landed) Splash(c, fx, c.ToFeet + Vector3.UnitY * h * 1.1f, h, I + 0.1f, col.Main, col.Light, 9, 4);
        Impact(c, col, fx, 0.85f);
    }

    /// <summary>Fire Pledge: a column of fire stands up under the target, embers raining and a ring of flame burning out round its foot.</summary>
    private static void FirePledgeY11(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        L1Column(c, fx, Rgb(255, 110, 40), Rgb(255, 232, 150), col.Main, 0.1f, 0.36f);
        float embers = Fx.Envelope(c.Age, 0.2f, 0.3f, 1.0f, 1.2f);
        for (int i = 0; i < 8 && embers > 0f; i++)
        {
            float u = (c.Age * 1.1f + i / 8f) % 1f;
            fx.Sprite(FxShape.Ember, c.ToFeet + new Vector3(Fx.Signed(c.Seed, i, 480) * h * 0.7f, h * (1.25f - 1.15f * u), 0.1f), h * 0.1f, Fx.Fade(col.Light, embers * MathF.Sin(u * MathF.PI)));
        }
        if (c.Cue.Landed) Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 9, FxShape.Flame, col.Main, col.Dark, 0.16f, 0.9f, -0.8f, salt: 256);
        Impact(c, col, fx, 0.85f);
    }

    /// <summary>Grass Pledge: thick green vines burst from the ground round the target, climb it in a tangle and shed leaves.</summary>
    private static void GrassPledgeY11(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0.06f, 0.2f, 0.85f, 1.1f);
        if (env > 0f)
            for (int i = 0; i < 5; i++)
            {
                const int n = 10;
                var pts = new Vector3[n];
                float root = (i - 2) * h * 0.38f;
                float grow = Fx.EaseOut(Math.Clamp((c.Age - 0.06f - i * 0.03f) / 0.3f, 0f, 1f));
                for (int j = 0; j < n; j++)
                {
                    float t = grow * j / (n - 1);
                    pts[j] = c.ToFeet + new Vector3(root + MathF.Sin(t * 7f + i) * h * 0.22f * t, h * 1.15f * t, 0.1f + 0.05f * i);
                }
                fx.Ribbon(pts, h * 0.12f, Fx.Fade(col.Dark, env), FxBlend.Alpha);
                fx.Ribbon(pts, h * 0.05f, Fx.Fade(col.Main, env));
                fx.Sprite(FxShape.Leaf, pts[n - 1], h * 0.2f, Fx.Fade(col.Light, env), FxBlend.Alpha, i);
            }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.8f, 11, FxShape.Leaf, col.Main, col.Light, 0.12f, 0.9f, 0.7f, FxBlend.Alpha, spin: 7f, salt: 257);
            GroundRing(fx, c.Age, c.ToFeet, I - 0.1f, 0.45f, h * 1.1f, col.Light);
        }
        Impact(c, col, fx, 0.85f);
    }

    /// <summary>Volt Switch: a bolt strikes the target and the user, as the bolt dies, streaks back in a U-turn to its own place, an arrow turning over it.</summary>
    private static void VoltSwitchY11(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Fx.Bolt(fx, c.Age, c.Seed, c.From, c.Aim, 0.04f, I + 0.1f, (c.FromSize + h) * 0.03f, col.Main, col.Light, 2);
        float k = (c.Age - I - 0.08f) / 0.35f;
        if (k > 0f && k < 1f)
        {
            var p = Vector3.Lerp(c.Aim, c.From, Fx.EaseOut(k)) + Vector3.UnitY * c.FromSize * 0.7f * MathF.Sin(k * MathF.PI);
            fx.Sprite(FxShape.Glow, p, c.FromSize * 0.7f, Fx.Fade(col.Main, 1f - k * 0.5f));
            fx.Streak(FxShape.Streak, p, c.From - c.Aim, c.FromSize * 0.08f, 6f, Fx.Fade(White, 1f - k));
            fx.Sprite(FxShape.Arrow, c.From + new Vector3(c.FromSize * 0.5f, c.FromSize * 1.0f, 0.2f), c.FromSize * 0.3f, Fx.Fade(col.Light, MathF.Sin(k * MathF.PI)), FxBlend.Alpha, MathF.PI / 2f + k * MathF.PI);
        }
        fx.ScreenFlash(Rgb(255, 250, 210), 0.14f * Fx.Envelope(c.Age, 0.08f, 0.12f, 0.16f, 0.3f));
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Struggle Bug: a scuttling mob of little green bugs runs at the target and swarms over it, nipping, a last few buzzing off.</summary>
    private static void StruggleBugY11(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var green = Rgb(150, 200, 50);
        for (int i = 0; i < 8; i++)
        {
            float k = (c.Age - 0.02f - 0.02f * i) / (I - 0.04f);
            if (k < 0f || k > 1f) continue;
            var p = Lob(c.From, c.Aim, k, 0.02f) + new Vector3(Fx.Signed(c.Seed, i, 490) * h * 0.3f * k, Fx.Signed(c.Seed, i, 491) * h * 0.25f * k, 0.1f);
            fx.Sprite(FxShape.Dot, p, h * 0.13f, Fx.Fade(green, 1f), FxBlend.Alpha);
            fx.Streak(FxShape.Streak, p, c.Aim - c.From, h * 0.04f, 3f, Fx.Fade(Rgb(236, 250, 160), 0.8f));
        }
        Swarm(fx, c.Age, c.Seed, c.To, h, I - 0.05f, 0.95f, 9, FxShape.Dot, green, Rgb(236, 250, 160), 0.11f, 0.6f);
        if (c.Cue.Landed) Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 6, FxShape.Spore, col.Light, col.Main, 0.07f, 0.9f, 0.3f, salt: 258);
        Impact(c, col, fx, 0.8f);
    }

    /// <summary>Bulldoze: a line of rocks and a heaving ridge of earth plough along the ground from the user to the target and burst on it in a shower of stones.</summary>
    private static void BulldozeY11(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = Math.Clamp((c.Age - 0.02f) / (I - 0.02f), 0f, 1f);
        float fade = Fx.Envelope(c.Age, 0f, 0.02f, I - 0.02f, I + 0.12f);
        if (fade > 0f)
            for (int i = 0; i < 7; i++)
            {
                float u = Math.Max(0f, k - i * 0.07f);
                var p = Vector3.Lerp(c.FromFeet, c.ToFeet, u) + new Vector3(0f, h * (0.12f + 0.1f * MathF.Abs(MathF.Sin(c.Age * 16f + i))), 0.1f);
                fx.Sprite(FxShape.Rock, p, h * (0.34f - 0.02f * i), Fx.Fade(i % 2 == 0 ? col.Main : col.Dark, fade), FxBlend.Alpha, i + c.Age * 6f);
                fx.Sprite(FxShape.Smoke, p + new Vector3(0f, h * 0.15f, 0f), h * 0.3f, Fx.Fade(Rgb(206, 180, 140, 200), fade * 0.7f), FxBlend.Alpha);
            }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + Vector3.UnitY * h * 0.15f, h, I, 0.7f, 10, FxShape.Rock, col.Main, col.Dark, 0.12f, 1.0f, 2.4f, FxBlend.Alpha, spin: 4f, salt: 259);
            GroundRing(fx, c.Age, c.ToFeet, I, 0.4f, h * 1.3f, col.Light);
            Jolt(fx, c.Age, I, 0.5f, 0.24f);
        }
        Impact(c, col, fx, 0.85f);
    }
}
