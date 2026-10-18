using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

// Plan 25 - Y12, its second half: Generation 6 from Diamond Storm to Hyperspace Fury (numbers 591 to 621). The first half
// and the table are in MoveFx.Later3.cs.
internal static partial class MoveFx
{
    /// <summary>Diamond Storm: a storm of white-blue diamonds whirls across and round the foe, each with a glint, the user's own guard hardening after it.</summary>
    private static void DiamondStormY12(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var ice = Rgb(200, 236, 255);
        Fx.Stream(fx, c, 0f, 0.5f, 0.32f, 12, FxShape.Shard, ice, White, 0.26f, arc: 0.04f, spread: 0.5f, spin: 7f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0f, 0.5f, 0.32f, 6, FxShape.Spark, White, Rgb(255, 240, 210), 0.1f, arc: 0.04f, spread: 0.5f);
        if (c.Cue.Landed)
        {
            Round(c, fx, FxShape.Shard, 10, ice, Rgb(176, 210, 245), 0.6f, 0.24f, 9f, FxBlend.Alpha, 0.95f);
            for (int i = 0; i < 4; i++) Fx.Glint(fx, c.Age, c.To + new Vector3(Fx.Signed(c.Seed, i, 340) * h * 0.4f, Fx.Signed(c.Seed, i, 341) * h * 0.35f, 0.2f), h, I + i * 0.06f, White, 0.3f);
            L3Up(fx, c.Age, c.FromFeet, c.FromSize, I + 0.2f, 2, Rgb(190, 210, 255));
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Steam Eruption: a geyser of white steam and scalding water bursts up under the foe, billowing out at the top with a red heat in it.</summary>
    private static void SteamEruptionY12(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float rumble = Fx.Envelope(c.Age, 0f, 0.05f, I - 0.05f, I);
        if (rumble > 0f) Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + Vector3.UnitY * 0.1f, h, 0.02f, 0.3f, 4, FxShape.Smoke, Rgb(240, 240, 240, 200), White, 0.12f, 0.3f, -0.1f, FxBlend.Alpha, salt: 342);
        if (c.Cue.Landed)
        {
            Shaft(fx, c.Age, c.ToFeet, I - 0.03f, 0.7f, h * 1.3f, h * 0.6f, Fx.Fade(Rgb(236, 244, 255), 0.9f), White);
            for (int i = 0; i < 8; i++)
            {
                float u = (c.Age - I + 0.03f) * 1.7f - i * 0.05f;
                if (u < 0f || u > 1f) continue;
                float a = i * 0.9f;
                fx.Sprite(FxShape.Smoke, c.ToFeet + new Vector3(MathF.Cos(a) * h * 0.6f * u, h * (0.4f + 0.8f * u), MathF.Sin(a) * 0.1f), h * (0.4f + 0.3f * u), Fx.Fade(Rgb(246, 246, 250, 235), (1f - u) * 0.95f), FxBlend.Alpha, a);
            }
            Splash(c, fx, c.ToFeet + Vector3.UnitY * h * 1.0f, h, I + 0.05f, col.Main, col.Light, 8, 7);
            fx.Sprite(FxShape.Glow, c.To, h * 1.0f, Fx.Fade(Rgb(255, 90, 60), 0.3f * Fx.Envelope(c.Age, I, I + 0.08f, I + 0.3f, I + 0.5f)), FxBlend.Alpha);
            GroundRing(fx, c.Age, c.ToFeet, I, 0.45f, h * 1.3f, Rgb(240, 244, 255));
            Jolt(fx, c.Age, I, 0.3f);
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Hyperspace Hole: a ring of violet space opens in the air beside the foe, stars wheeling in it, and a hand darts out of it and strikes.</summary>
    private static void HyperspaceHoleY12(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var hole = c.To + new Vector3(h * 0.75f, h * 0.45f, 0.2f);
        float env = Fx.Envelope(c.Age, 0f, 0.1f, I + 0.1f, I + 0.35f);
        if (env > 0f)
        {
            float open = Fx.EaseOut(Math.Clamp(c.Age / 0.2f, 0f, 1f));
            fx.Sprite(FxShape.Glow, hole, h * 0.9f * open, Fx.Fade(Rgb(90, 40, 170), env * 0.8f), FxBlend.Alpha);
            L2Ellipse(fx, hole, h * 0.4f * open, h * 0.4f * open, h * 0.05f, Fx.Fade(Rgb(190, 150, 255), env), 16, 0f);
            fx.Sprite(FxShape.Swirl, hole, h * 0.35f * open, Fx.Fade(Rgb(150, 100, 240), env), FxBlend.Alpha, c.Age * 9f);
            for (int i = 0; i < 5; i++)
            {
                float a = i * 1.26f + c.Age * 5f;
                fx.Sprite(FxShape.Star, hole + new Vector3(MathF.Cos(a) * h * 0.22f, MathF.Sin(a) * h * 0.22f, 0.1f), h * 0.05f, Fx.Fade(White, env), FxBlend.Alpha, a);
            }
        }
        // The hand: out of the ring, onto the foe
        float k = Fx.EaseOut(Math.Clamp((c.Age - 0.12f) / (I - 0.12f), 0f, 1f));
        if (c.Age > 0.12f && c.Age < I + 0.25f)
        {
            var p = Vector3.Lerp(hole, c.To, k);
            float fade = c.Age > I ? Math.Max(0f, 1f - (c.Age - I) / 0.25f) : 1f;
            fx.Sprite(FxShape.Palm, p, h * 0.3f, Fx.Fade(Rgb(230, 200, 255), fade), FxBlend.Alpha, -0.7f);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 7, FxShape.Hex, Rgb(190, 150, 255), White, 0.1f, 1.0f, 0.2f, FxBlend.Alpha, spin: 5f, salt: 343);
            Jolt(fx, c.Age, I, 0.2f);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Water Shuriken: three spinning four-pointed stars of water are thrown one after another and each lands with a splash of its own.</summary>
    private static void WaterShurikenY12(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        for (int i = 0; i < 3; i++)
        {
            float leave = 0.02f + i * 0.08f;
            float k = (c.Age - leave) / 0.22f;
            if (k < 0f || k > 1f) continue;
            var end = c.Aim + new Vector3((i - 1) * h * 0.2f, (1 - i) * h * 0.12f, 0.1f);
            var p = Lob(c.From, end, k, 0.04f);
            float fade = Math.Min(1f, (1f - k) * 6f);
            fx.Sprite(FxShape.Glow, p, h * 0.3f, Fx.Fade(col.Main, 0.5f * fade), FxBlend.Alpha);
            fx.Sprite(FxShape.Star, p, h * 0.2f, Fx.Fade(col.Main, fade), FxBlend.Alpha, c.Age * 30f + i);
            fx.Sprite(FxShape.Star, p, h * 0.12f, Fx.Fade(White, fade), FxBlend.Add, c.Age * 30f + i);
            fx.Streak(FxShape.Streak, p, end - c.From, h * 0.04f, 3f, Fx.Fade(col.Light, 0.7f * fade));
            if (k >= 1f - 1e-3f || c.Age > leave + 0.22f)
            {
                if (!c.Cue.Missed) Splash(c, fx, c.To + new Vector3((i - 1) * h * 0.2f, 0f, 0f), h * 0.6f, leave + 0.22f, col.Main, col.Light, 4, 8 + i);
            }
        }
        if (c.Cue.Landed) Fx.Burst(fx, c.Seed, c.Age, c.To, h * 0.6f, I - 0.02f, 0.4f, 4, FxShape.Star, col.Light, White, 0.08f, 0.6f, 0f, FxBlend.Alpha, salt: 344);
        Impact(c, col, fx, 0.75f);
    }

    /// <summary>Mystical Fire: a ball of violet-pink fire with wavering tongues is thrown, and bursts on the foe in a ring of mystic flame that dims its mind.</summary>
    private static void MysticalFireY12(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var violet = Rgb(200, 90, 240);
        var pink = Rgb(255, 160, 220);
        Fx.Orb(fx, c, 0.02f, I, 0.36f, violet, pink, FxShape.Flame);
        Fx.Stream(fx, c, 0.02f, I, 0.25f, 5, FxShape.Wisp, pink, White, 0.14f, spread: 0.3f, blend: FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            float env = Fx.Envelope(c.Age, I, I + 0.05f, I + 0.35f, I + 0.6f);
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathF.Tau / 6f;
                fx.Sprite(FxShape.Flame, c.To + new Vector3(MathF.Cos(a) * h * 0.55f, MathF.Sin(a) * h * 0.45f, 0.15f), h * 0.22f, Fx.Fade(i % 2 == 0 ? violet : pink, env), FxBlend.Add, a - MathF.PI / 2f);
            }
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.4f, h * 1.2f, Fx.Fade(pink, 0.9f));
            L2Down(fx, c.Age, c.Seed, c.ToFeet, h, I + 0.1f, 2, Rgb(230, 130, 230));
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Spiky Shield: a ring of long green thorns thrusts out of the ground and the air round the user, tips glinting.</summary>
    private static void SpikyShieldY12(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0.02f, 0.14f, 0.85f, 1.1f);
        if (env <= 0f) return;
        fx.Sprite(FxShape.Glow, c.From, h * 1.1f, Fx.Fade(col.Main, env * 0.25f), FxBlend.Alpha);
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.Tau / 12f;
            float grow = Fx.EaseOut(Math.Clamp((c.Age - 0.03f - 0.012f * i) / 0.14f, 0f, 1f));
            var d = new Vector3(MathF.Cos(a), MathF.Sin(a), 0f);
            var p = c.From + d * h * (0.5f + 0.2f * grow);
            fx.Streak(FxShape.Shard, p, d, h * 0.15f, 2.4f * grow + 0.01f, Fx.Fade(i % 2 == 0 ? Rgb(40, 130, 60) : Rgb(110, 210, 80), env), FxBlend.Alpha);
            fx.Sprite(FxShape.Spark, c.From + d * h * (0.5f + 0.5f * grow), h * 0.05f * grow, Fx.Fade(White, env * 0.8f));
        }
        Fx.Glint(fx, c.Age, c.From + new Vector3(h * 0.4f, h * 0.35f, 0.1f), h, 0.35f, White, 0.4f);
    }

    /// <summary>Aromatic Mist: a soft lilac mist with pink petals in it drifts across and settles round the one it is for, a blue arrow of guard rising in it.</summary>
    private static void AromaticMistY12(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        Fx.Stream(fx, c, 0f, 0.55f, 0.35f, 8, FxShape.Smoke, Rgb(236, 206, 255, 220), Rgb(255, 214, 236, 220), 0.3f, spread: 0.35f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0f, 0.55f, 0.35f, 6, FxShape.Petal, Rgb(255, 170, 210), Rgb(255, 224, 238), 0.1f, arc: 0.05f, spread: 0.45f, spin: 3f, blend: FxBlend.Alpha);
        if (c.Age > I)
        {
            float env = Fx.Envelope(c.Age, I, I + 0.1f, 0.9f, 1.15f);
            fx.Sprite(FxShape.Glow, c.To, h * 1.0f, Fx.Fade(Rgb(214, 180, 255), env * 0.3f), FxBlend.Alpha);
            Rising(fx, c.Age, c.Seed, c.ToFeet + new Vector3(0f, h * 0.2f, 0f), h, I, 1.1f, 4, FxShape.Heart, Rgb(255, 190, 225), 0.1f, 0.12f);
            L3Up(fx, c.Age, c.ToFeet, h, I + 0.05f, 1, Rgb(120, 170, 255));
        }
    }

    /// <summary>Eerie Impulse: sickly yellow-green pulses ripple out of the user's eyes, thin bolts run to the foe and its mind is dimmed twice over.</summary>
    private static void EerieImpulseY12(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        var sick = Rgb(200, 230, 90);
        float eyes = Fx.Envelope(c.Age, 0f, 0.05f, 0.4f, 0.55f);
        for (int s = -1; s <= 1; s += 2) fx.Sprite(FxShape.Eye, c.From + new Vector3(s * c.FromSize * 0.14f, c.FromSize * 0.4f, 0.25f), c.FromSize * 0.12f, Fx.Fade(sick, eyes), FxBlend.Alpha);
        for (int i = 0; i < 3; i++)
        {
            float k = (c.Age - 0.04f - i * 0.1f) / 0.4f;
            if (k < 0f || k > 1f) continue;
            fx.Sprite(FxShape.Ring, Vector3.Lerp(c.From, c.Aim, k), c.FromSize * (0.3f + 0.7f * k), Fx.Fade(i % 2 == 0 ? sick : Rgb(160, 90, 210), 1f - k), FxBlend.Alpha);
        }
        Fx.Bolt(fx, c.Age, c.Seed, c.From, c.Aim, 0.1f, 0.5f, h * 0.03f, sick, White, 0);
        if (c.Age > I)
        {
            Tint(c, fx, I, 1.0f, Rgb(170, 200, 80), 0.3f);
            L2Down(fx, c.Age, c.Seed, c.ToFeet, h, I + 0.05f, 2, Rgb(180, 220, 90));
        }
    }

    /// <summary>Venom Drench: a violet flask is thrown, bursts on the foe and its venom runs down it into a bubbling puddle, three arrows falling.</summary>
    private static void VenomDrenchY12(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        Ball(c, fx, FxShape.Orb, 0.02f, I, 0.26f, 0.1f, Rgb(120, 60, 170), Rgb(220, 150, 250), 6f);
        if (c.Cue.Landed)
        {
            Splash(c, fx, c.To, h, I, col.Main, col.Light, 10, 9);
            Drips(fx, c.Age, c.Seed, c.To + Vector3.UnitY * h * 0.5f, h, I + 0.05f, 6, col.Main);
            float env = Fx.Envelope(c.Age, I, I + 0.1f, 0.9f, 1.15f);
            fx.Flat(FxShape.Glow, c.ToFeet + Vector3.UnitY * 0.03f, Vector3.UnitY, h * 0.9f * Fx.EaseOut(Math.Clamp((c.Age - I) / 0.3f, 0f, 1f)), Fx.Fade(Rgb(110, 40, 150), env * 0.8f), FxBlend.Alpha);
            for (int i = 0; i < 4; i++)
            {
                float u = (c.Age * 1.2f + i / 4f) % 1f;
                fx.Sprite(FxShape.Bubble, c.ToFeet + new Vector3(Fx.Signed(c.Seed, i, 345) * h * 0.4f, h * (0.05f + 0.3f * u), 0.2f), h * 0.07f, Fx.Fade(col.Light, env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha);
            }
            L2Down(fx, c.Age, c.Seed, c.ToFeet, h, I + 0.1f, 3, Rgb(200, 110, 230));
        }
    }

    /// <summary>Powder: a pale yellow dust is puffed over the foe and hangs there glittering, here and there a tiny spark waiting to catch.</summary>
    private static void PowderY12(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, I - 0.05f, I + 0.1f, 0.9f, 1.15f);
        for (int i = 0; i < 18; i++)
        {
            float start = 0.04f + 0.4f * Fx.Rand(c.Seed, i, 383);
            float k = (c.Age - start) / 0.55f;
            if (k < 0f || k > 1f) continue;
            var p = Vector3.Lerp(c.From + new Vector3(0f, c.FromSize * 0.3f, 0f), c.To + new Vector3(Fx.Signed(c.Seed, i, 384) * h * 0.5f, h * 0.6f, 0.2f), Fx.EaseOut(Math.Min(1f, k * 1.6f))) + Vector3.UnitY * -h * 0.5f * Math.Max(0f, k - 0.5f);
            fx.Sprite(FxShape.Spore, p, h * 0.2f, Fx.Fade(i % 2 == 0 ? Rgb(255, 232, 120) : Rgb(255, 200, 90), MathF.Sin(k * MathF.PI)), FxBlend.Alpha, k * 7f + i);
        }
        if (env <= 0f) return;
        fx.Sprite(FxShape.Smoke, c.To + new Vector3(0f, h * 0.2f, 0.2f), h * 0.8f, Fx.Fade(Rgb(255, 226, 120, 200), env * 0.5f), FxBlend.Alpha, c.Age);
        fx.Sprite(FxShape.Glow, c.To, h * 1.2f, Fx.Fade(Rgb(255, 220, 110), env * 0.3f), FxBlend.Alpha);
        for (int i = 0; i < 6; i++)
        {
            float k = (c.Age * 3f + i * 0.43f) % 1f;
            if (k < 0.5f) fx.Sprite(FxShape.Ember, c.To + new Vector3(Fx.Signed(c.Seed, i, 346) * h * 0.5f, Fx.Signed(c.Seed, i, 347) * h * 0.4f, 0.3f), h * 0.07f, Fx.Fade(Rgb(255, 150, 60), env * MathF.Sin(k * 2f * MathF.PI)));
        }
    }

    /// <summary>Geomancy: six pillars of light stand up round the user from rings of gold on the ground, motes drawn in from the earth and three arrows rising.</summary>
    private static void GeomancyY12(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.15f, 0.85f, 1.1f);
        if (env <= 0f) return;
        var gold = Rgb(255, 226, 130);
        GroundRing(fx, c.Age, c.FromFeet, 0.02f, 0.6f, h * 1.5f, Rgb(255, 170, 224));
        GroundRing(fx, c.Age, c.FromFeet, 0.2f, 0.6f, h * 1.1f, gold);
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f;
            Shaft(fx, c.Age, c.FromFeet + new Vector3(MathF.Cos(a) * h * 0.85f, 0f, MathF.Sin(a) * h * 0.45f), 0.08f + 0.03f * i, 0.7f, h * 0.9f, h * 0.12f, Fx.Fade(i % 2 == 0 ? col.Main : gold, 0.8f), White);
        }
        Gather(fx, c.Age, c.Seed, c.From, h, 0.1f, 0.85f, 12, gold, col.Main, 0.1f, 1.0f);
        L3Up(fx, c.Age, c.FromFeet, h, 0.5f, 3, Rgb(255, 190, 230));
        fx.Sprite(FxShape.Glow, c.From, h * 1.2f, Fx.Fade(col.Light, env * 0.3f), FxBlend.Alpha);
    }

    /// <summary>Magnetic Flux: a red plus and a blue minus hang either side of the user and curved lines of force arc between them, sparks running along.</summary>
    private static void MagneticFluxY12(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0.02f, 0.14f, 0.85f, 1.1f);
        if (env <= 0f) return;
        var plus = c.From + new Vector3(-h * 0.8f, h * 0.05f, 0.25f);
        var minus = c.From + new Vector3(h * 0.8f, h * 0.05f, 0.25f);
        fx.Sprite(FxShape.Glow, plus, h * 0.5f, Fx.Fade(Rgb(255, 80, 80), env * 0.5f), FxBlend.Alpha);
        fx.Sprite(FxShape.Glow, minus, h * 0.5f, Fx.Fade(Rgb(80, 130, 255), env * 0.5f), FxBlend.Alpha);
        fx.Sprite(FxShape.Cross, plus, h * 0.2f, Fx.Fade(Rgb(255, 120, 110), env), FxBlend.Alpha);
        fx.Streak(FxShape.Beam, minus, Vector3.UnitX, h * 0.04f, 5f, Fx.Fade(Rgb(120, 160, 255), env), FxBlend.Alpha);
        for (int line = 0; line < 3; line++)
        {
            const int n = 14;
            var pts = new Vector3[n];
            float lift = 0.25f + 0.2f * line;
            for (int j = 0; j < n; j++)
            {
                float t = j / (float)(n - 1);
                pts[j] = Vector3.Lerp(plus, minus, t) + new Vector3(0f, MathF.Sin(t * MathF.PI) * h * lift * (line % 2 == 0 ? 1f : -1f), 0f);
            }
            fx.Ribbon(pts, h * 0.03f, Fx.Fade(col.Main, env * 0.8f), FxBlend.Alpha);
            float k = (c.Age * 1.4f + line * 0.3f) % 1f;
            int idx = Math.Clamp((int)(k * (n - 1)), 0, n - 1);
            fx.Sprite(FxShape.Spark, pts[idx], h * 0.12f, Fx.Fade(White, env * MathF.Sin(k * MathF.PI)));
        }
        Fx.Aura(fx, c.Age, c.Seed, c.FromFeet, h, 0.05f, 0.95f, col.Main, col.Light, 5);
    }

    /// <summary>Happy Hour: gold coins and confetti rain down on the user's side with a shining glow and notes of a jaunty tune.</summary>
    private static void HappyHourY12(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.85f, 1.1f);
        if (env <= 0f) return;
        for (int i = 0; i < 22; i++)
        {
            float start = 0.02f + 0.8f * Fx.Rand(c.Seed, i, 380);
            float k = (c.Age - start) / 0.45f;
            if (k < 0f || k > 1f) continue;
            var p = c.FromFeet + new Vector3(Fx.Signed(c.Seed, i, 381) * h * 1.2f, h * (1.35f - 1.3f * k), Fx.Signed(c.Seed, i, 382) * 0.3f);
            if (i % 3 == 0) fx.Sprite(FxShape.Star, p, h * 0.12f, Fx.Fade(i % 2 == 0 ? Rgb(255, 130, 170) : Rgb(120, 200, 255), env * MathF.Sin(k * MathF.PI)), FxBlend.Alpha, k * 8f);
            else
            {
                float squash = MathF.Abs(MathF.Cos(k * 14f + i));
                fx.Sprite(FxShape.Dot, p, h * 0.1f * (0.4f + 0.6f * squash), Fx.Fade(Rgb(255, 214, 60), env * MathF.Sin(k * MathF.PI)), FxBlend.Alpha);
                fx.Sprite(FxShape.Glow, p, h * 0.14f, Fx.Fade(Rgb(255, 244, 150), env * 0.5f * MathF.Sin(k * MathF.PI)), FxBlend.Add);
            }
        }
        for (int i = 0; i < 3; i++)
        {
            float u = (c.Age * 0.9f + i / 3f) % 1f;
            fx.Sprite(FxShape.Note, c.From + new Vector3((i - 1) * h * 0.7f, h * (0.2f + 0.7f * u), 0.2f), h * 0.14f, Fx.Fade(Rgb(255, 240, 180), env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha);
        }
        fx.Sprite(FxShape.Glow, c.From, h * 1.2f, Fx.Fade(Rgb(255, 226, 120), env * 0.3f), FxBlend.Alpha);
    }

    /// <summary>Electric Terrain: a lattice of yellow-blue lines is laid over the ground of the whole field, sparks crawling along it and the ground humming.</summary>
    private static void ElectricTerrainY12(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = Math.Max(c.FromSize, c.FoeSize);
        var mid = L1Field(c);
        float env = Fx.Envelope(c.Age, 0f, 0.15f, 0.85f, 1.1f);
        if (env <= 0f) return;
        float grow = Fx.EaseOut(Math.Clamp(c.Age / 0.4f, 0f, 1f));
        fx.Flat(FxShape.Glow, mid + Vector3.UnitY * 0.03f, Vector3.UnitY, h * 3.0f * grow, Fx.Fade(Rgb(255, 230, 90), env * 0.3f), FxBlend.Alpha);
        for (int i = -3; i <= 3; i++)
        {
            float t = i * h * 0.6f;
            fx.Ribbon(new[] { mid + new Vector3(t * grow, 0.05f, -h * 1.1f), mid + new Vector3(t * grow, 0.05f, h * 1.1f) }, h * 0.06f, Fx.Fade(i % 2 == 0 ? col.Main : Rgb(120, 190, 255), env * 0.9f), FxBlend.Alpha);
            fx.Ribbon(new[] { mid + new Vector3(-h * 2.2f * grow, 0.05f, t * 0.5f), mid + new Vector3(h * 2.2f * grow, 0.05f, t * 0.5f) }, h * 0.06f, Fx.Fade(i % 2 == 0 ? Rgb(120, 190, 255) : col.Main, env * 0.9f), FxBlend.Alpha);
        }
        for (int i = 0; i < 9; i++)
        {
            float k = (c.Age * 1.8f + i * 0.37f) % 1f;
            fx.Sprite(FxShape.Spark, mid + new Vector3((k - 0.5f) * h * 4f, h * (0.05f + 0.2f * MathF.Sin(k * MathF.PI)), Fx.Signed(c.Seed, i, 348) * h * 0.9f), h * 0.1f, Fx.Fade(White, env * MathF.Sin(k * MathF.PI)));
        }
    }

    /// <summary>Dazzling Gleam: a blinding pink-white flare blooms in the middle of the foe, long rays of light shooting out of it in every direction.</summary>
    private static void DazzlingGleamY12(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Gather(fx, c.Age, c.Seed, c.From, c.FromSize, 0f, 0.4f, 8, col.Light, White, 0.1f);
        float charge = Fx.Envelope(c.Age, 0f, 0.1f, I - 0.04f, I);
        fx.Sprite(FxShape.Glow, c.From, c.FromSize * 0.6f * charge, Fx.Fade(col.Light, charge * 0.8f));
        if (c.Cue.Landed)
        {
            float env = Fx.Envelope(c.Age, I - 0.02f, I + 0.03f, I + 0.2f, I + 0.55f);
            fx.Sprite(FxShape.Glow, c.To, h * 1.8f * env, Fx.Fade(col.Main, env * 0.7f), FxBlend.Alpha);
            fx.Sprite(FxShape.Star, c.To, h * 1.0f * env, Fx.Fade(White, env), FxBlend.Add, c.Age * 0.5f);
            for (int i = 0; i < 10; i++)
            {
                float a = i * MathF.Tau / 10f + 0.2f;
                var d = new Vector3(MathF.Cos(a), MathF.Sin(a), 0f);
                fx.Streak(FxShape.Beam, c.To + d * h * 0.9f, d, h * 0.045f, 11f, Fx.Fade(i % 2 == 0 ? White : col.Light, env));
            }
            fx.ScreenFlash(Rgb(255, 224, 244), 0.22f * Fx.Envelope(c.Age, I - 0.02f, I + 0.02f, I + 0.06f, I + 0.22f));
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Celebrate: a party popper goes off over the user, confetti of every colour, curling streamers and little stars bursting out.</summary>
    private static void CelebrateY12(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.05f, 0.85f, 1.1f);
        if (env <= 0f) return;
        var colours = new[] { Rgb(255, 90, 110), Rgb(255, 210, 70), Rgb(90, 210, 130), Rgb(100, 170, 255), Rgb(220, 120, 255) };
        float pop = Fx.Envelope(c.Age, 0.05f, 0.08f, 0.2f, 0.35f);
        fx.Sprite(FxShape.Star, c.From + new Vector3(0f, h * 0.3f, 0.3f), h * 0.4f * pop, Fx.Fade(White, pop), FxBlend.Add, c.Age * 4f);
        for (int i = 0; i < 18; i++)
        {
            float k = (c.Age - 0.08f) / 0.9f;
            if (k < 0f || k > 1f) continue;
            var d = Fx.Direction(c.Seed, i);
            d.Y = Math.Abs(d.Y) * 0.4f + 0.1f;
            var p = c.From + new Vector3(0f, h * 0.3f, 0.2f) + new Vector3(d.X * 1.4f, d.Y, d.Z) * h * 0.85f * Fx.EaseOut(k) - Vector3.UnitY * h * 0.5f * k * k;
            fx.Sprite(i % 3 == 0 ? FxShape.Star : FxShape.Dot, p, h * 0.11f, Fx.Fade(colours[i % colours.Length], env * (1f - k * k)), FxBlend.Alpha, i + k * 8f);
        }
        for (int i = 0; i < 3; i++)
        {
            float k = Fx.EaseOut(Math.Clamp((c.Age - 0.08f) / 0.35f, 0f, 1f));
            const int n = 8;
            var pts = new Vector3[n];
            for (int j = 0; j < n; j++)
            {
                float t = k * j / (n - 1);
                pts[j] = c.From + new Vector3((i - 1) * h * 0.7f * t + MathF.Sin(t * 14f + i) * h * 0.1f, h * (0.5f + 0.45f * t) - h * 0.35f * t * t, 0.2f);
            }
            fx.Ribbon(pts, h * 0.03f, Fx.Fade(colours[(i * 2) % colours.Length], env), FxBlend.Alpha);
        }
    }

    /// <summary>Hold Hands: a chain of small hearts is strung between the user and the other, each pulsing in turn, and both glow in the same warm light.</summary>
    private static void HoldHandsY12(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float env = Fx.Envelope(c.Age, 0f, 0.15f, 0.85f, 1.1f);
        if (env <= 0f) return;
        var a = c.From + new Vector3(0f, c.FromSize * 0.1f, 0.2f);
        var b = c.To + new Vector3(0f, c.ToSize * 0.1f, 0.2f);
        float reach = Fx.EaseOut(Math.Clamp(c.Age / 0.35f, 0f, 1f));
        for (int i = 0; i < 7; i++)
        {
            float t = i / 6f;
            if (t > reach) break;
            float pulse = 0.7f + 0.3f * MathF.Sin(c.Age * 9f - i * 0.9f);
            fx.Sprite(FxShape.Heart, Vector3.Lerp(a, b, t) + new Vector3(0f, MathF.Sin(t * MathF.PI) * c.ToSize * 0.25f, 0f), c.ToSize * 0.2f * pulse, Fx.Fade(Rgb(255, 130, 180), env), FxBlend.Alpha);
        }
        fx.Sprite(FxShape.Glow, c.From, c.FromSize * 1.0f, Fx.Fade(Rgb(255, 200, 150), env * 0.3f), FxBlend.Alpha);
        fx.Sprite(FxShape.Glow, c.To, c.ToSize * 1.0f, Fx.Fade(Rgb(255, 200, 150), env * 0.3f), FxBlend.Alpha);
        Fx.Glint(fx, c.Age, Vector3.Lerp(a, b, 0.5f), c.ToSize, 0.4f, White, 0.4f);
    }

    /// <summary>Baby-Doll Eyes: two huge round shining eyes open over the user's face, hearts floating off them to the foe, whose Attack falls.</summary>
    private static void BabyDollEyesY12(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.5f, 0.65f);
        if (env > 0f)
            for (int s = -1; s <= 1; s += 2)
            {
                var p = c.From + new Vector3(s * h * 0.24f, h * 0.4f, 0.3f);
                fx.Sprite(FxShape.Orb, p, h * 0.27f, Fx.Fade(Rgb(250, 240, 250), env), FxBlend.Alpha);
                fx.Sprite(FxShape.Orb, p, h * 0.21f, Fx.Fade(Rgb(150, 80, 150), env), FxBlend.Alpha);
                fx.Sprite(FxShape.Dot, p, h * 0.1f, Fx.Fade(Rgb(60, 20, 70), env), FxBlend.Alpha);
                fx.Sprite(FxShape.Star, p + new Vector3(-h * 0.05f, h * 0.05f, 0.1f), h * 0.1f, Fx.Fade(White, env), FxBlend.Add, c.Age * 3f);
            }
        for (int i = 0; i < 5; i++)
        {
            float k = (c.Age - 0.12f - i * 0.06f) / 0.42f;
            if (k < 0f || k > 1f) continue;
            fx.Sprite(FxShape.Heart, Vector3.Lerp(c.From + new Vector3(0f, h * 0.4f, 0f), c.Aim, k) + new Vector3(0f, MathF.Sin(k * 8f + i) * c.ToSize * 0.12f, 0.2f), c.ToSize * 0.12f, Fx.Fade(Rgb(255, 150, 200), MathF.Sin(k * MathF.PI)), FxBlend.Alpha);
        }
        if (c.Age > I) L2Down(fx, c.Age, c.Seed, c.ToFeet, c.ToSize, I + 0.05f, 1, Rgb(255, 170, 200));
    }

    /// <summary>Nuzzle: the user's two red cheeks spark and it rubs against the foe, little yellow arcs jumping across and a soft tingle left behind.</summary>
    private static void NuzzleY12(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = Fx.EaseOut(Math.Clamp((c.Age - 0.03f) / (I - 0.03f), 0f, 1f));
        float cheeks = Fx.Envelope(c.Age, 0f, 0.06f, I + 0.1f, I + 0.3f);
        var at = Vector3.Lerp(c.From, c.Aim, k);
        for (int s = -1; s <= 1; s += 2)
        {
            var cheek = at + new Vector3(s * c.FromSize * 0.18f, c.FromSize * 0.05f, 0.25f);
            fx.Sprite(FxShape.Glow, cheek, c.FromSize * 0.2f, Fx.Fade(Rgb(255, 80, 70), cheeks * 0.8f), FxBlend.Alpha);
            fx.Sprite(FxShape.Spark, cheek, c.FromSize * 0.14f * (0.6f + 0.4f * MathF.Sin(c.Age * 30f + s)), Fx.Fade(col.Light, cheeks));
        }
        if (c.Cue.Landed)
        {
            for (int i = 0; i < 3; i++) Fx.Bolt(fx, c.Age, c.Seed + i * 4, c.To + new Vector3((i - 1) * h * 0.2f, h * 0.35f, 0.2f), c.To + new Vector3(Fx.Signed(c.Seed, i, 349) * h * 0.5f, -h * 0.1f, 0.2f), I, I + 0.4f, h * 0.03f, col.Main, White, 0);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 6, FxShape.Spark, White, col.Main, 0.07f, 0.7f, 0f, salt: 350);
        }
        Impact(c, col, fx, 0.65f);
    }

    /// <summary>Hold Back: a small gentle pat of a hand, a ring of pale light round the wrist holding the force back, a little puff and a single sparkle.</summary>
    private static void HoldBackY12(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Strike(c, fx, FxShape.Palm, new Vector3(-0.8f, 0.3f, 0.2f), I, 0.4f, Rgb(250, 238, 222), 0.4f, 0.2f);
        float ring = Fx.Envelope(c.Age, I - 0.2f, I - 0.12f, I + 0.05f, I + 0.3f);
        if (ring > 0f) fx.Sprite(FxShape.Ring, c.To + new Vector3(-h * 0.25f, h * 0.1f, 0.3f), h * 0.3f, Fx.Fade(White, ring * 0.8f), FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.4f, 4, FxShape.Smoke, Rgb(246, 242, 236, 200), White, 0.12f, 0.4f, -0.2f, FxBlend.Alpha, salt: 351);
            Fx.Glint(fx, c.Age, c.To + new Vector3(h * 0.2f, h * 0.2f, 0.2f), h, I + 0.05f, White, 0.35f);
        }
        Impact(c, col, fx, 0.55f);
    }

    /// <summary>Infestation: a crowd of tiny dark bugs crawls over the foe and circles it tightly, a sticky web of threads left between them.</summary>
    private static void InfestationY12(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var dark = Rgb(70, 50, 36);
        Fx.Stream(fx, c, 0f, 0.4f, 0.28f, 12, FxShape.Dot, dark, Rgb(120, 90, 50), 0.12f, arc: 0.04f, spread: 0.5f, blend: FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            float env = Fx.Envelope(c.Age, I, I + 0.1f, 0.9f, 1.15f);
            Swarm(fx, c.Age, c.Seed, c.To, h, I - 0.05f, 1.15f, 16, FxShape.Dot, dark, Rgb(255, 150, 60), 0.1f, 0.5f);
            for (int i = 0; i < 4; i++)
            {
                float a = i * MathF.PI / 2f + c.Age * 3f;
                fx.Ribbon(new[] { c.To + new Vector3(MathF.Cos(a) * h * 0.5f, MathF.Sin(a) * h * 0.4f, 0.2f), c.To + new Vector3(MathF.Cos(a + 2.2f) * h * 0.5f, MathF.Sin(a + 2.2f) * h * 0.4f, 0.2f) }, h * 0.015f, Fx.Fade(Rgb(236, 230, 214), env * 0.8f), FxBlend.Alpha);
            }
            Haze(fx, c.Age, c.Seed, c.To, h, I, 1.1f, 3, Rgb(120, 96, 60, 150), 0.18f);
        }
        Impact(c, col, fx, 0.7f);
    }

    /// <summary>Power-Up Punch: a red glowing fist drives into the foe and the user's whole arm flares with orange power and rising arrows after it.</summary>
    private static void PowerUpPunchY12(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Strike(c, fx, FxShape.Glow, new Vector3(-1.1f, 0.15f, 0.2f), I, 0.8f, Rgb(255, 90, 50), 0f, 0.16f);
        Strike(c, fx, FxShape.Fist, new Vector3(-1.1f, 0.15f, 0.2f), I, 0.65f, Rgb(255, 214, 180), 0.15f, 0.16f);
        SpeedLines(c, fx, 0.08f, I, 4, 0.4f);
        if (c.Cue.Landed)
        {
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.35f, h * 1.1f, Fx.Fade(col.Light, 0.9f));
            Jolt(fx, c.Age, I, 0.3f);
            float aura = Fx.Envelope(c.Age, I + 0.1f, I + 0.18f, I + 0.5f, I + 0.65f);
            fx.Sprite(FxShape.Glow, c.From, c.FromSize * 1.1f, Fx.Fade(Rgb(255, 120, 60), aura * 0.5f), FxBlend.Alpha);
            L3Up(fx, c.Age, c.FromFeet, c.FromSize, I + 0.1f, 2, Rgb(255, 150, 90));
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Oblivion Wing: a pair of great red-black wings spreads behind the user and a beam of crimson life is drawn out of the foe into it.</summary>
    private static void OblivionWingY12(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var red = Rgb(220, 50, 70);
        var dark = Rgb(70, 24, 50);
        float env = Fx.Envelope(c.Age, 0f, 0.14f, I + 0.3f, I + 0.55f);
        float spread = Fx.EaseOut(Math.Clamp(c.Age / 0.25f, 0f, 1f));
        if (env > 0f)
            for (int s = -1; s <= 1; s += 2)
                for (int i = 0; i < 5; i++)
                {
                    float a = (0.4f + 0.28f * i) * spread;
                    fx.Sprite(FxShape.Feather, c.From + new Vector3(s * (h * 0.4f + h * 0.28f * i * MathF.Cos(a * 0.5f)), h * (0.2f + 0.2f * i * MathF.Sin(a * 0.6f)), -0.1f), h * 0.4f, Fx.Fade(i % 2 == 0 ? red : dark, env * 0.9f), FxBlend.Alpha, s * (1.0f + 0.2f * i));
                }
        Fx.Beam(fx, c, 0.14f, I + 0.1f, 0.1f, red, Rgb(255, 190, 190), 6);
        if (c.Cue.Landed)
        {
            Fx.Stream(fx, c, I, I + 0.7f, 0.35f, 8, FxShape.Glow, Rgb(255, 120, 130), White, 0.14f, arc: 0.06f, spread: 0.3f, blend: FxBlend.Alpha, reverse: true);
            L2Heal(fx, c.Age, c.Seed, c.FromFeet, c.FromSize, I + 0.3f);
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Thousand Arrows: a rain of long brown arrows of earth falls on the foe from above, each thudding into the ground in a puff of dust.</summary>
    private static void ThousandArrowsY12(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var clay = Rgb(170, 124, 70);
        for (int i = 0; i < 16; i++)
        {
            float land = 0.12f + 0.025f * i;
            float k = (c.Age - (land - 0.2f)) / 0.2f;
            if (k < 0f || c.Age > land + 0.25f) continue;
            float x = (Fx.Rand(c.Seed, i, 352) - 0.5f) * h * 1.5f;
            float z = Fx.Signed(c.Seed, i, 353) * h * 0.4f;
            var top = c.ToFeet + new Vector3(x + h * 0.3f, h * 1.2f, z + 0.3f);
            var bottom = c.ToFeet + new Vector3(x, h * 0.15f, z);
            var p = Vector3.Lerp(top, bottom, Math.Min(1f, k) * Math.Min(1f, k));
            float fade = c.Age > land ? Math.Max(0f, 1f - (c.Age - land) / 0.25f) : 1f;
            fx.Streak(FxShape.Arrow, p, bottom - top, h * 0.11f, 2.6f, Fx.Fade(i % 2 == 0 ? clay : Rgb(214, 170, 100), fade), FxBlend.Alpha);
            if (c.Age > land && c.Age < land + 0.3f) Fx.Burst(fx, c.Seed, c.Age, bottom, h * 0.5f, land, 0.3f, 2, FxShape.Smoke, Rgb(222, 206, 176, 200), Rgb(176, 160, 132, 200), 0.1f, 0.4f, -0.1f, FxBlend.Alpha, salt: 354 + i);
        }
        if (c.Cue.Landed)
        {
            GroundRing(fx, c.Age, c.ToFeet, I, 0.5f, h * 1.3f, col.Light);
            Jolt(fx, c.Age, I, 0.3f);
        }
        Impact(c, col, fx, 0.85f);
    }

    /// <summary>Thousand Waves: wave after wave of sand rolls across the ground, each a low curling crest, and the foe is pinned where it stands by the last.</summary>
    private static void ThousandWavesY12(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var sand = Rgb(214, 176, 110);
        for (int i = 0; i < 6; i++)
        {
            float k = (c.Age - 0.02f - i * 0.07f) / 0.34f;
            if (k < 0f || k > 1f) continue;
            var p = Vector3.Lerp(c.FromFeet, c.ToFeet, k) + new Vector3(0f, h * 0.2f, 0.1f + 0.02f * i);
            float fade = MathF.Sin(k * MathF.PI);
            fx.Sprite(FxShape.Wave, p, h * (0.7f + 0.4f * k), Fx.Fade(i % 2 == 0 ? Rgb(236, 196, 120) : Rgb(190, 140, 80), fade), FxBlend.Alpha, ScreenAngle(c.ToFeet - c.FromFeet));
            Fx.Burst(fx, c.Seed, c.Age, p, h * 0.3f, 0.02f + i * 0.07f + k * 0.34f, 0.05f, 1, FxShape.Smoke, Rgb(222, 196, 150, 160), sand, 0.08f, 0.2f, 0f, FxBlend.Alpha, salt: 360 + i);
        }
        if (c.Cue.Landed)
        {
            float pin = Fx.Envelope(c.Age, I, I + 0.1f, 0.9f, 1.15f);
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathF.Tau / 6f;
                fx.Sprite(FxShape.Wave, c.ToFeet + new Vector3(MathF.Cos(a) * h * 0.55f, h * 0.15f, MathF.Sin(a) * h * 0.3f), h * 0.2f, Fx.Fade(sand, pin), FxBlend.Alpha, a + MathF.PI / 2f, depth: MathF.Sin(a) < 0f);
            }
            GroundRing(fx, c.Age, c.ToFeet, I, 0.5f, h * 1.2f, col.Light);
            Jolt(fx, c.Age, I, 0.25f);
        }
        Impact(c, col, fx, 0.85f);
    }

    /// <summary>Land's Wrath: the earth itself rises: radial cracks spread from under the foe and a great glowing column of earth and boulders erupts through it.</summary>
    private static void LandsWrathY12(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var gold = Rgb(255, 190, 80);
        float tremor = Fx.Envelope(c.Age, 0f, 0.04f, I - 0.05f, I);
        for (int i = 0; i < 7; i++)
        {
            float a = i * MathF.Tau / 7f + 0.3f;
            float grow = Fx.EaseOut(Math.Clamp((c.Age - 0.02f) / (I - 0.02f), 0f, 1f));
            float fade = Fx.Envelope(c.Age, 0f, 0.04f, 0.85f, 1.1f);
            fx.Ribbon(new[] { c.ToFeet + Vector3.UnitY * 0.04f, c.ToFeet + new Vector3(MathF.Cos(a) * h * 1.4f * grow, 0.04f, MathF.Sin(a) * h * 0.8f * grow) }, h * 0.1f, Fx.Fade(Rgb(70, 46, 30), fade), FxBlend.Alpha);
            fx.Ribbon(new[] { c.ToFeet + Vector3.UnitY * 0.05f, c.ToFeet + new Vector3(MathF.Cos(a) * h * 1.3f * grow, 0.05f, MathF.Sin(a) * h * 0.75f * grow) }, h * 0.04f, Fx.Fade(gold, fade * 0.9f));
        }
        if (tremor > 0f) Jolt(fx, c.Age, 0.02f, 0.2f, I);
        if (c.Cue.Landed)
        {
            Shaft(fx, c.Age, c.ToFeet, I - 0.03f, 0.75f, h * 1.3f, h * 0.85f, Fx.Fade(Rgb(150, 110, 70), 0.95f), gold);
            fx.Sprite(FxShape.Glow, c.To, h * 1.6f * Fx.Envelope(c.Age, I, I + 0.05f, I + 0.25f, I + 0.55f), Fx.Fade(gold, 0.55f), FxBlend.Alpha);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 10, FxShape.Rock, Rgb(176, 150, 108), Rgb(110, 86, 60), 0.26f, 1.0f, 1.4f, FxBlend.Alpha, spin: 6f, salt: 370);
            GroundRing(fx, c.Age, c.ToFeet, I, 0.5f, h * 1.6f, gold);
            GroundRing(fx, c.Age, c.ToFeet, I + 0.1f, 0.5f, h * 1.1f, col.Light);
            Dust(c, fx, c.ToFeet, h, I, 6);
            Jolt(fx, c.Age, I, 0.65f, 0.28f);
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Light of Ruin: pink petals of light gather into a vast flower over the user, which opens and fires one wide beam of rose-white, a red recoil flickering back.</summary>
    private static void LightOfRuinY12(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float bloom = Fx.Envelope(c.Age, 0f, 0.1f, I - 0.02f, I + 0.1f);
        var at = c.From + new Vector3(c.FromSize * 0.1f, c.FromSize * 0.2f, 0.3f);
        if (bloom > 0f)
        {
            L3Flower(fx, at, c.FromSize * 0.7f * bloom, Rgb(255, 160, 214), Rgb(255, 250, 230), bloom, c.Age * 1.5f);
            fx.Sprite(FxShape.Glow, at, c.FromSize * 1.2f * bloom, Fx.Fade(col.Light, bloom * 0.6f), FxBlend.Alpha);
            Gather(fx, c.Age, c.Seed, at, c.FromSize, 0.02f, I, 12, col.Main, White, 0.1f, 1.1f);
        }
        Fx.Beam(fx, c, I - 0.02f, 0.7f, 0.3f, col.Main, White, 10);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 10, FxShape.Petal, col.Main, White, 0.14f, 1.2f, -0.2f, FxBlend.Alpha, spin: 5f, salt: 371);
            Fx.Burst(fx, c.Seed, c.Age, c.From, c.FromSize, I + 0.1f, 0.4f, 6, FxShape.Spark, Rgb(255, 90, 90), Rgb(255, 190, 150), 0.08f, 0.8f, 0f, salt: 372);
            fx.ScreenFlash(Rgb(255, 214, 236), 0.2f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.06f, I + 0.22f));
            Jolt(fx, c.Age, I, 0.4f);
        }
        Impact(c, col, fx, 1.2f);
    }

    /// <summary>Origin Pulse: three glowing deep-blue orbs hang round the user and each throws a thin white-blue beam that crosses the others over the foe.</summary>
    private static void OriginPulseY12(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var deep = Rgb(40, 100, 230);
        var pale = Rgb(190, 226, 255);
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.6f, 0.75f);
        for (int i = 0; i < 3; i++)
        {
            var orb = c.From + new Vector3((i - 1) * c.FromSize * 0.5f, c.FromSize * (0.55f + 0.15f * (i == 1 ? 1 : 0)), 0.2f);
            fx.Sprite(FxShape.Glow, orb, c.FromSize * 0.5f, Fx.Fade(deep, env * 0.7f), FxBlend.Alpha);
            fx.Sprite(FxShape.Orb, orb, c.FromSize * 0.15f, Fx.Fade(pale, env), FxBlend.Alpha);
            float t0 = 0.1f + 0.05f * i;
            float k = Math.Clamp((c.Age - t0) / 0.12f, 0f, 1f);
            if (k > 0f && c.Age < 0.62f)
            {
                var end = c.Aim + new Vector3((1 - i) * h * 0.25f, (i - 1) * h * 0.2f, 0.1f);
                var tip = Vector3.Lerp(orb, end, k);
                fx.Ribbon(new[] { orb, tip }, h * 0.13f, Fx.Fade(deep, env * 0.9f), FxBlend.Alpha);
                fx.Ribbon(new[] { orb, tip }, h * 0.05f, Fx.Fade(White, env));
            }
        }
        if (c.Cue.Landed)
        {
            fx.Sprite(FxShape.Glow, c.To, h * 1.3f * Fx.Envelope(c.Age, I, I + 0.05f, I + 0.2f, I + 0.5f), Fx.Fade(deep, 0.6f), FxBlend.Alpha);
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.4f, h * 1.2f, Fx.Fade(pale, 0.9f));
            Splash(c, fx, c.To, h, I, col.Main, col.Light, 6, 10);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Precipice Blades: great blades of red-hot rock thrust up out of the ground under the foe in a fan, the cracks between them glowing like lava.</summary>
    private static void PrecipiceBladesY12(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var lava = Rgb(255, 110, 40);
        float tremor = Fx.Envelope(c.Age, 0f, 0.04f, I - 0.05f, I);
        for (int i = 0; i < 6; i++)
        {
            float a = -1f + 2f * i / 5f;
            float grow = Fx.EaseOut(Math.Clamp((c.Age - (I - 0.14f) - 0.02f * i) / 0.16f, 0f, 1f));
            float fade = Fx.Envelope(c.Age, I - 0.2f, I - 0.14f, 0.85f, 1.1f);
            if (grow <= 0f) continue;
            var foot = c.ToFeet + new Vector3(a * h * 1.25f, 0f, 0.35f + 0.03f * i);
            var d = new Vector3(a * 0.3f, 1f, 0f);
            fx.Streak(FxShape.Shard, foot + d * h * 0.6f * grow, d, h * 0.22f, 3.0f * grow + 0.01f, Fx.Fade(Rgb(120, 60, 44), fade), FxBlend.Alpha);
            fx.Streak(FxShape.Shard, foot + d * h * 0.6f * grow, d, h * 0.13f, 2.8f * grow + 0.01f, Fx.Fade(lava, fade * 0.85f));
            fx.Sprite(FxShape.Glow, foot, h * 0.3f, Fx.Fade(lava, fade * 0.6f * grow), FxBlend.Alpha);
        }
        if (tremor > 0f) fx.Flat(FxShape.Crack, c.ToFeet + Vector3.UnitY * 0.04f, Vector3.UnitY, h * 1.2f * Fx.EaseOut(Math.Clamp(c.Age / I, 0f, 1f)), Fx.Fade(lava, tremor * 0.9f), FxBlend.Alpha, 0.4f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 8, FxShape.Rock, Rgb(176, 120, 90), Rgb(110, 70, 56), 0.13f, 1.0f, 1.3f, FxBlend.Alpha, spin: 6f, salt: 373);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 6, FxShape.Ember, lava, Rgb(255, 220, 130), 0.08f, 0.9f, -0.3f, salt: 374);
            Jolt(fx, c.Age, I, 0.5f, 0.24f);
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Dragon Ascent: a spiral of green-gold wind winds up round the user, which climbs out of the picture and dives back at the foe in a long streak.</summary>
    private static void DragonAscentY12(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var green = Rgb(120, 220, 140);
        var gold = Rgb(255, 224, 120);
        L1Spiral(fx, c.Age, c.Seed, c.FromFeet, c.FromSize, 0f, 0.45f, 12, FxShape.Wisp, green, gold, 0.38f, 0.7f, 1.3f);
        float dive = Math.Clamp((c.Age - 0.12f) / (I - 0.12f), 0f, 1f);
        if (c.Age > 0.12f && c.Age < I + 0.2f)
        {
            var start = c.Aim + new Vector3(-h * 1.3f, h * 0.9f, 0.2f);
            var p = Vector3.Lerp(start, c.Aim, dive * dive);
            float fade = c.Age > I ? Math.Max(0f, 1f - (c.Age - I) / 0.2f) : 1f;
            fx.Streak(FxShape.Streak, p - (c.Aim - start) * 0.2f, c.Aim - start, h * 0.12f, 5f, Fx.Fade(green, fade), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, p, h * 0.7f, Fx.Fade(gold, fade * 0.6f), FxBlend.Alpha);
            fx.Sprite(FxShape.Orb, p, h * 0.28f, Fx.Fade(White, fade), FxBlend.Alpha);
            for (int i = 0; i < 3; i++) fx.Sprite(FxShape.Feather, p - (c.Aim - start) * 0.12f * (i + 1), h * 0.14f, Fx.Fade(i % 2 == 0 ? green : gold, fade * 0.8f), FxBlend.Alpha, i * 0.8f);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 9, FxShape.Feather, green, gold, 0.14f, 1.1f, 0.3f, FxBlend.Alpha, spin: 4f, salt: 375);
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.4f, h * 1.3f, Fx.Fade(gold, 0.9f));
            Jolt(fx, c.Age, I, 0.4f);
            L2Down(fx, c.Age, c.Seed, c.FromFeet, c.FromSize, I + 0.15f, 2, Rgb(150, 190, 200));
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Hyperspace Fury: four dark rifts tear open round the foe and a spectral fist darts out of each in turn and pounds it, the last rift swallowing the rest.</summary>
    private static void HyperspaceFuryY12(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var violet = Rgb(160, 90, 240);
        var spots = new[] { new Vector3(-0.8f, 0.5f, 0f), new Vector3(0.8f, 0.55f, 0f), new Vector3(-0.7f, -0.25f, 0f), new Vector3(0.75f, -0.2f, 0f) };
        for (int i = 0; i < 4; i++)
        {
            float t0 = 0.02f + 0.07f * i;
            float open = Fx.Envelope(c.Age, t0, t0 + 0.06f, I + 0.2f, I + 0.45f);
            if (open <= 0f) continue;
            var rift = c.To + spots[i] * h + new Vector3(0f, 0f, 0.2f);
            fx.Sprite(FxShape.Glow, rift, h * 0.7f, Fx.Fade(Rgb(60, 20, 110), open * 0.9f), FxBlend.Alpha);
            L2Ellipse(fx, rift, h * 0.42f * open, h * 0.2f * open, h * 0.05f, Fx.Fade(violet, open), 12, c.Age * 3f);
            float k = (c.Age - t0 - 0.05f) / 0.12f;
            if (k > 0f && k < 2.2f)
            {
                float u = k < 1f ? Fx.EaseOut(k) : Math.Max(0f, 1f - (k - 1f) * 0.8f);
                var p = Vector3.Lerp(rift, c.To, u * 0.85f);
                fx.Sprite(FxShape.Glow, p, h * 0.45f, Fx.Fade(violet, Math.Min(1f, open * 2f) * 0.6f), FxBlend.Alpha);
                fx.Sprite(FxShape.Fist, p, h * 0.33f, Fx.Fade(Rgb(226, 206, 255), Math.Min(1f, open * 2f)), FxBlend.Alpha, spots[i].X < 0 ? 0.5f : -0.5f);
                if (k >= 1f && k < 1.5f && !c.Cue.Missed) Fx.Contact(fx, c.Age, c.Seed + i, c.To + spots[i] * h * 0.2f, h, t0 + 0.17f, 0.18f, col.Main, col.Light);
            }
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 8, FxShape.Hex, violet, White, 0.1f, 1.0f, 0.2f, FxBlend.Alpha, spin: 5f, salt: 376);
            Jolt(fx, c.Age, I, 0.4f);
        }
        Impact(c, col, fx, 1.05f);
    }
}
