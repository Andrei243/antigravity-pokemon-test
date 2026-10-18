using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

// Plan 25 - Y13b, the second part: Dragon Energy (820) to Fillet Away (868), the rest of Generation 8 and the first of
// Generation 9. As MoveFx.Later5.cs, whose L5 helpers they share.
internal static partial class MoveFx
{
    private static Dictionary<string, FxRecipe> Later5b() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Dragon Energy"] = DragonEnergyY13b,
        ["Freezing Glare"] = FreezingGlareY13b,
        ["Fiery Wrath"] = FieryWrathY13b,
        ["Thunderous Kick"] = ThunderousKickY13b,
        ["Glacial Lance"] = GlacialLanceY13b,
        ["Astral Barrage"] = AstralBarrageY13b,
        ["Eerie Spell"] = EerieSpellY13b,
        ["Dire Claw"] = DireClawY13b,
        ["Psyshield Bash"] = PsyshieldBashY13b,
        ["Power Shift"] = PowerShiftY13b,
        ["Stone Axe"] = StoneAxeY13b,
        ["Springtide Storm"] = SpringtideStormY13b,
        ["Mystical Power"] = MysticalPowerY13b,
        ["Raging Fury"] = RagingFuryY13b,
        ["Wave Crash"] = WaveCrashY13b,
        ["Chloroblast"] = ChloroblastY13b,
        ["Mountain Gale"] = MountainGaleY13b,
        ["Victory Dance"] = VictoryDanceY13b,
        ["Headlong Rush"] = HeadlongRushY13b,
        ["Barb Barrage"] = BarbBarrageY13b,
        ["Esper Wing"] = EsperWingY13b,
        ["Bitter Malice"] = BitterMaliceY13b,
        ["Shelter"] = ShelterY13b,
        ["Triple Arrows"] = TripleArrowsY13b,
        ["Infernal Parade"] = InfernalParadeY13b,
        ["Ceaseless Edge"] = CeaselessEdgeY13b,
        ["Bleakwind Storm"] = BleakwindStormY13b,
        ["Wildbolt Storm"] = WildboltStormY13b,
        ["Sandsear Storm"] = SandsearStormY13b,
        ["Lunar Blessing"] = LunarBlessingY13b,
        ["Take Heart"] = TakeHeartY13b,
        ["Tera Blast"] = TeraBlastY13b,
        ["Silk Trap"] = SilkTrapY13b,
        ["Axe Kick"] = AxeKickY13b,
        ["Last Respects"] = LastRespectsY13b,
        ["Lumina Crash"] = LuminaCrashY13b,
        ["Order Up"] = OrderUpY13b,
        ["Jet Punch"] = JetPunchY13b,
        ["Spicy Extract"] = SpicyExtractY13b,
        ["Spin Out"] = SpinOutY13b,
        ["Population Bomb"] = PopulationBombY13b,
        ["Ice Spinner"] = IceSpinnerY13b,
        ["Glaive Rush"] = GlaiveRushY13b,
        ["Revival Blessing"] = RevivalBlessingY13b,
        ["Salt Cure"] = SaltCureY13b,
        ["Triple Dive"] = TripleDiveY13b,
        ["Mortal Spin"] = MortalSpinY13b,
        ["Doodle"] = DoodleY13b,
        ["Fillet Away"] = FilletAwayY13b
    };

    /// <summary>Dragon Energy: a dragon of gold and indigo light rears up out of the user and plunges down onto the foes in a roaring red-gold blast.</summary>
    private static void DragonEnergyY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float k = Math.Clamp((c.Age - 0.03f) / (I + 0.02f), 0f, 1f);
        float env = Fx.Envelope(c.Age, 0f, 0.1f, I + 0.1f, I + 0.35f);
        if (env > 0f)
        {
            var pts = new Vector3[19];
            for (int i = 0; i <= 18; i++)
            {
                float u = i / 18f;
                float back = Math.Max(0f, k - u * 0.45f);
                var p = Lob(c.From, c.Aim, back, 0.22f);
                pts[i] = p + new Vector3(0f, MathF.Sin(u * 9f - c.Age * 14f) * h * 0.14f, 0.15f);
            }
            fx.Ribbon(pts, h * 0.5f, Fx.Fade(Rgb(110, 96, 250), env), FxBlend.Alpha);
            fx.Ribbon(pts, h * 0.2f, Fx.Fade(Rgb(255, 214, 110), env), FxBlend.Add);
            fx.Sprite(FxShape.Fang, pts[0] + new Vector3(h * 0.1f, h * 0.1f, 0.1f), h * 0.6f, Fx.Fade(White, env), FxBlend.Alpha, -0.9f);
            fx.Sprite(FxShape.Glow, pts[0], h * 1.1f, Fx.Fade(Rgb(255, 190, 90), env * 0.6f));
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.7f, 10, FxShape.Flame, Rgb(255, 160, 70), Rgb(255, 230, 140), 0.18f, 1.0f, -0.4f, salt: 520);
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.5f, c.ToSize * 2.0f, Fx.Fade(Rgb(255, 190, 90), 0.9f));
            Jolt(fx, c.Age, I, 1.0f, 0.35f);
        }
        Impact(c, col, fx, 1.4f);
    }

    /// <summary>Freezing Glare: two eyes open on the user and send a pale pink glare that crosses to the foe, which is glazed over with ice crystals spreading across it.</summary>
    private static void FreezingGlareY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float eye = Fx.Envelope(c.Age, 0f, 0.08f, I - 0.08f, I);
        if (eye > 0f)
            for (int s = -1; s <= 1; s += 2)
            {
                var at = c.From + new Vector3(h * 0.48f + s * h * 0.12f, h * 0.35f, 0.3f);
                fx.Sprite(FxShape.Eye, at, h * 0.3f, Fx.Fade(Rgb(255, 170, 230), eye), FxBlend.Alpha);
                fx.Sprite(FxShape.Glow, at, h * 0.5f, Fx.Fade(Rgb(190, 240, 255), eye * 0.7f));
            }
        Fx.Beam(fx, c, 0.12f, I + 0.05f, h * 0.2f, Fx.Fade(Rgb(190, 230, 255), 0.85f), Rgb(255, 210, 240), 8);
        if (c.Cue.Landed)
        {
            float env = Fx.Envelope(c.Age, I, I + 0.1f, 0.95f, 1.15f);
            float spread = Fx.EaseOut(Math.Clamp((c.Age - I) / 0.35f, 0f, 1f));
            for (int i = 0; i < 7; i++)
            {
                float a = i * MathF.Tau / 7f;
                var p = c.To + new Vector3(MathF.Cos(a) * c.ToSize * 0.55f * spread, MathF.Sin(a) * c.ToSize * 0.5f * spread, 0.3f);
                fx.Sprite(FxShape.Shard, p, c.ToSize * 0.3f, Fx.Fade(Rgb(190, 232, 255), env), FxBlend.Alpha, a);
                fx.Sprite(FxShape.Snowflake, p * 0.5f + c.To * 0.5f, c.ToSize * 0.2f, Fx.Fade(White, env * 0.8f), FxBlend.Alpha, c.Age * 3f + i);
            }
            fx.Sprite(FxShape.Glow, c.To, c.ToSize * 1.3f * spread, Fx.Fade(Rgb(170, 230, 255), env * 0.4f), FxBlend.Alpha);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Fiery Wrath: a wave of black and crimson flame sweeps out of the user across the whole side, dark red lightning inside it and an anger mark flaring over each foe.</summary>
    private static void FieryWrathY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var crimson = Rgb(220, 40, 50);
        var black = Rgb(50, 24, 50);
        for (int i = 0; i < 11; i++)
        {
            float k = (c.Age - 0.03f - i * 0.022f) / 0.36f;
            if (k < 0f || k > 1f) continue;
            var p = Lob(c.From, c.Aim, k, 0.04f) + new Vector3(Fx.Signed(c.Seed, i, 580) * h * 0.25f, Fx.Signed(c.Seed, i, 581) * h * 0.3f, 0.05f);
            fx.Sprite(FxShape.Flame, p, c.FromSize * 0.45f, Fx.Fade(i % 2 == 0 ? black : crimson, 0.9f), FxBlend.Alpha, k);
            fx.Sprite(FxShape.Flame, p, c.FromSize * 0.3f, Fx.Fade(crimson, 0.7f));
        }
        if (c.Cue.Landed)
        {
            float env = Fx.Envelope(c.Age, I, I + 0.06f, 0.8f, 1.0f);
            fx.Sprite(FxShape.Exclaim, c.ToFeet + new Vector3(h * 0.35f, h * 1.05f, 0.3f), h * 0.3f, Fx.Fade(Rgb(255, 70, 70), env), FxBlend.Alpha);
            Fx.Bolt(fx, c.Age, c.Seed, c.To + Vector3.UnitY * h * 0.9f, c.To - Vector3.UnitY * h * 0.3f, I, I + 0.2f, h * 0.05f, crimson, Rgb(255, 150, 150), 1);
            Jolt(fx, c.Age, I, 0.7f, 0.25f);
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Thunderous Kick: a kick wound in lightning, the foot ringed by zigzag bolts, lands twice in the one blow and the foe's guard sags under an arrow.</summary>
    private static void ThunderousKickY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Strike(c, fx, FxShape.Foot, new Vector3(-1.4f, -0.2f, 0f), I, 0.8f, Rgb(255, 170, 60), 0.3f, 0.14f);
        float k = (c.Age - (I - 0.14f)) / 0.14f;
        if (k > 0f && c.Age < I + 0.1f)
        {
            var foot = c.To + new Vector3(-h * 1.4f * (1f - Fx.EaseOut(Math.Min(1f, k))), -h * 0.2f, 0.2f);
            for (int i = 0; i < 3; i++)
                Fx.Bolt(fx, c.Age, c.Seed + i, foot + new Vector3(-h * 0.4f, (i - 1) * h * 0.25f, 0f), foot + new Vector3(h * 0.2f, (i - 1) * h * 0.1f, 0f), 0f, 2f, h * 0.035f, Rgb(255, 232, 70), White, 0);
        }
        if (c.Cue.Landed)
        {
            Fx.Contact(fx, c.Age, c.Seed + 1, c.To + new Vector3(0f, h * 0.15f, 0f), h, I + 0.05f, 0.25f, Rgb(255, 232, 70), White);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 7, FxShape.Spark, Rgb(255, 238, 120), White, 0.1f, 1.1f, salt: 521);
            L2Down(fx, c.Age, c.Seed, c.ToFeet, h, I + 0.1f, 1, Rgb(255, 160, 70));
            Jolt(fx, c.Age, I, 0.8f, 0.3f);
        }
        Impact(c, col, fx, 1.15f);
    }

    /// <summary>Glacial Lance: a spear of ice as long as the field is hurled, ringed with frost, and bursts on the foes into a spray of crystals and a cold white mist.</summary>
    private static void GlacialLanceY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float k = Math.Clamp((c.Age - 0.05f) / (I - 0.05f), 0f, 1f);
        if (c.Age < I + 0.02f)
        {
            var dir = Vector3.Normalize(c.Aim - c.From);
            var tip = Vector3.Lerp(c.From + dir * h * 0.6f, c.Aim, Fx.EaseOut(k));
            var tail = tip - dir * h * 2.6f;
            fx.Ribbon(new[] { tail, tip }, h * 0.4f, Fx.Fade(Rgb(150, 220, 255), 1f), FxBlend.Alpha);
            fx.Ribbon(new[] { tail, tip }, h * 0.08f, Fx.Fade(White, 1f), FxBlend.Add);
            fx.Sprite(FxShape.Arrow, tip, h * 1.0f, Fx.Fade(Rgb(214, 246, 255), 1f), FxBlend.Alpha, ScreenAngle(dir) - MathF.PI / 2f);
            for (int i = 0; i < 6; i++) fx.Sprite(FxShape.Snowflake, Vector3.Lerp(tail, tip, i / 6f) + new Vector3(0f, Fx.Signed(c.Seed, i, 585) * h * 0.2f, 0.1f), h * 0.12f, Fx.Fade(White, 0.8f), FxBlend.Alpha, i + c.Age * 5f);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.8f, 12, FxShape.Shard, Rgb(170, 232, 255), White, 0.15f, 1.1f, 1.2f, FxBlend.Alpha, salt: 522);
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.9f, 5, FxShape.Smoke, Rgb(230, 248, 255, 200), Rgb(190, 225, 245, 200), 0.24f, 0.6f, -0.1f, FxBlend.Alpha, salt: 523);
            Jolt(fx, c.Age, I, 0.9f, 0.3f);
        }
        Impact(c, col, fx, 1.3f);
    }

    /// <summary>Astral Barrage: a swarm of small ghost lights, pale violet with trailing tails, streams down onto the foes from above, each its own small flash.</summary>
    private static void AstralBarrageY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        for (int i = 0; i < 9; i++)
        {
            float t = I - 0.28f + i * 0.04f;
            float k = (c.Age - (t - 0.16f)) / 0.16f;
            if (k < 0f || c.Age > t + 0.12f) continue;
            var land = c.To + new Vector3(Fx.Signed(c.Seed, i, 590) * h * 0.55f, Fx.Signed(c.Seed, i, 591) * h * 0.3f, 0.1f);
            var start = land + new Vector3(h * 0.9f, h * 1.5f, 0f);
            var p = Vector3.Lerp(start, land, Fx.EaseOut(Math.Min(1f, k)));
            float fade = c.Age < t ? 1f : Math.Max(0f, 1f - (c.Age - t) / 0.12f);
            fx.Streak(FxShape.Streak, p, land - start, h * 0.2f, 5f, Fx.Fade(Rgb(190, 160, 255), fade), FxBlend.Add);
            fx.Sprite(FxShape.Wisp, p, h * 0.42f, Fx.Fade(Rgb(150, 110, 240), fade), FxBlend.Alpha);
            fx.Sprite(FxShape.Star, p, h * 0.22f, Fx.Fade(White, fade));
            if (c.Cue.Landed && c.Age >= t) Fx.Contact(fx, c.Age, c.Seed + i, land, h, t, 0.14f, Rgb(160, 120, 250), White);
        }
        if (c.Cue.Landed) Jolt(fx, c.Age, I, 0.6f, 0.3f);
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Eerie Spell: a violet spell circle of ring, star and runes turns over the foe and draws a thread of light up out of it, the move's power draining away upward.</summary>
    private static void EerieSpellY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0.05f, 0.18f, 0.85f, 1.1f);
        if (env > 0f)
        {
            var at = c.To + new Vector3(0f, 0f, 0.3f);
            fx.Sprite(FxShape.Ring, at, h * 1.8f, Fx.Fade(Rgb(190, 120, 255), env), FxBlend.Alpha, c.Age * 1.5f);
            fx.Sprite(FxShape.Ring, at, h * 1.2f, Fx.Fade(Rgb(255, 140, 210), env * 0.9f), FxBlend.Alpha, -c.Age * 2f);
            fx.Sprite(FxShape.Star, at, h * 1.5f, Fx.Fade(Rgb(214, 170, 255), env * 0.55f), FxBlend.Add, c.Age * 1.5f);
            for (int i = 0; i < 6; i++)
            {
                float a = c.Age * 1.5f + i * MathF.Tau / 6f;
                fx.Sprite(FxShape.Cross, at + new Vector3(MathF.Cos(a) * h * 0.82f, MathF.Sin(a) * h * 0.82f, 0.05f), h * 0.2f, Fx.Fade(White, env * 0.9f), FxBlend.Add, a);
            }
        }
        if (c.Cue.Landed)
        {
            Rising(fx, c.Age, c.Seed, c.ToFeet + new Vector3(0f, 0.1f, 0.1f), h, I, 1.15f, 5, FxShape.Dot, Rgb(220, 190, 255), 0.09f, 0.25f);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 6, FxShape.Spark, Rgb(255, 170, 230), White, 0.09f, 0.8f, salt: 524);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Dire Claw: three claws of dark violet tear down across the foe, a thick poison dripping from each cut, and a bubble of venom rising.</summary>
    private static void DireClawY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        if (c.Cue.Landed) Fx.Slash(fx, c.Age, c.Seed, c.To, h, I - 0.1f, 3, 0.05f, Rgb(160, 70, 210), White, FxShape.Claw);
        if (c.Cue.Landed)
        {
            for (int i = 0; i < 3; i++)
            {
                float k = Math.Clamp((c.Age - I - i * 0.03f) / 0.6f, 0f, 1f);
                if (k <= 0f || c.Age > 1.1f) continue;
                var top = c.To + new Vector3((i - 1) * h * 0.28f, h * 0.35f, 0.3f);
                fx.Ribbon(new[] { top, top - new Vector3(0f, h * (0.1f + 0.6f * k), 0f) }, h * 0.05f, Fx.Fade(Rgb(160, 70, 200), 1f - 0.5f * k), FxBlend.Alpha);
                fx.Sprite(FxShape.Drop, top - new Vector3(0f, h * (0.1f + 0.6f * k), 0f), h * 0.1f, Fx.Fade(Rgb(190, 100, 230), 1f - k), FxBlend.Alpha);
            }
            Fx.Rain(fx, c.Age, c.Seed, c.ToFeet + Vector3.UnitY * h * 0.3f, h * 0.5f, I, 1.0f, 0.4f, 3, FxShape.Bubble, Rgb(190, 110, 230), White, 0.1f, FxBlend.Alpha);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Psyshield Bash: the user gathers a shield of pink light and charges, the shield's rim ringing with hexagons, and slams it into the foe in a pink-white pulse.</summary>
    private static void PsyshieldBashY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float k = Math.Clamp((c.Age - 0.05f) / (I - 0.05f), 0f, 1f);
        if (c.Age < I + 0.15f)
        {
            float fade = c.Age < I ? 1f : Math.Max(0f, 1f - (c.Age - I) / 0.15f);
            var p = Vector3.Lerp(c.From + new Vector3(h * 0.4f, 0f, 0.2f), c.Aim - new Vector3(c.ToSize * 0.3f, 0f, 0f), Fx.EaseOut(k));
            fx.Sprite(FxShape.Hex, p, h * 1.0f, Fx.Fade(Rgb(255, 140, 210), 0.85f * fade), FxBlend.Alpha);
            fx.Sprite(FxShape.Hex, p, h * 1.1f, Fx.Fade(White, 0.5f * fade));
            fx.Sprite(FxShape.Ring, p, h * 1.3f, Fx.Fade(Rgb(255, 190, 230), 0.7f * fade));
            L5Ring(fx, p, h * 0.55f, h * 0.55f, 6, FxShape.Dot, Fx.Fade(White, fade), h * 0.08f, k * 5f);
        }
        if (c.Cue.Landed)
        {
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.4f, c.ToSize * 1.6f, Fx.Fade(Rgb(255, 170, 225), 0.9f));
            Jolt(fx, c.Age, I, 0.8f, 0.3f);
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Power Shift: an orange arrow and a blue one change places in a ring round the user, crossing over its middle, Attack and Defense traded.</summary>
    private static void PowerShiftY13b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.8f, 1.05f);
        if (env <= 0f) return;
        float k = Fx.EaseOut(Math.Clamp((c.Age - 0.08f) / 0.5f, 0f, 1f));
        for (int s = 0; s < 2; s++)
        {
            float a = s * MathF.PI + k * MathF.PI;
            var p = c.From + new Vector3(MathF.Cos(a) * h * 0.65f, MathF.Sin(a) * h * 0.6f, 0.3f);
            var col2 = s == 0 ? Rgb(255, 150, 70) : Rgb(100, 170, 255);
            fx.Sprite(FxShape.Arrow, p, h * 0.34f, Fx.Fade(col2, env), FxBlend.Alpha, a);
            fx.Sprite(FxShape.Glow, p, h * 0.5f, Fx.Fade(col2, env * 0.5f));
        }
        L5Arc(fx, c.From, h * 0.65f, h * 0.6f, 0.2f, 0.2f + MathF.PI * 1.6f * k, h * 0.04f, Fx.Fade(White, env * 0.6f), 16, FxBlend.Add);
        Fx.Glint(fx, c.Age, c.From + new Vector3(0f, 0f, 0.4f), h, 0.5f, White, 0.45f);
    }

    /// <summary>Stone Axe: an axe of rock chops down on the foe and splinters, chips of rock flying and pointed stones left standing on the ground round it.</summary>
    private static void StoneAxeY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = (c.Age - (I - 0.22f)) / 0.22f;
        if (k > 0f && c.Age < I + 0.3f)
        {
            float ang = 0.9f - 1.8f * Fx.EaseOut(Math.Min(1f, k));
            float fade = c.Age < I ? 1f : Math.Max(0f, 1f - (c.Age - I) / 0.3f);
            var grip = c.To + new Vector3(-h * 1.2f, -h * 0.5f, 0.3f);
            var head = grip + new Vector3(MathF.Cos(ang) * h * 1.7f, MathF.Sin(ang) * h * 1.7f, 0f);
            fx.Ribbon(new[] { grip, head }, h * 0.14f, Fx.Fade(Rgb(120, 86, 56), fade), FxBlend.Alpha);
            fx.Sprite(FxShape.Rock, head, h * 0.8f, Fx.Fade(Rgb(176, 156, 120), fade), FxBlend.Alpha, ang);
            fx.Sprite(FxShape.Shard, head + new Vector3(-h * 0.1f, -h * 0.1f, 0.05f), h * 0.65f, Fx.Fade(Rgb(226, 210, 170), fade), FxBlend.Alpha, ang + 1f);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 9, FxShape.Rock, Rgb(190, 164, 120), Rgb(110, 92, 64), 0.11f, 1.0f, 2.0f, FxBlend.Alpha, salt: 525);
            for (int i = 0; i < 4; i++)
            {
                float rise = Fx.EaseOut(Math.Clamp((c.Age - I - i * 0.03f) / 0.12f, 0f, 1f));
                var at = c.ToFeet + new Vector3((i - 1.5f) * h * 0.5f, h * 0.15f * rise, 0.15f);
                fx.Sprite(FxShape.Shard, at, h * 0.3f * rise, Fx.Fade(Rgb(176, 156, 120), Fx.Envelope(c.Age, I, I + 0.1f, 1.0f, 1.2f)), FxBlend.Alpha, (i - 1.5f) * 0.3f);
            }
            Jolt(fx, c.Age, I, 0.9f, 0.3f);
        }
        Impact(c, col, fx, 1.15f);
    }

    /// <summary>Springtide Storm: a spring gale of pink petals whirls out of the user, flower-shaped lightning in the cloud of them, and sweeps across the whole side.</summary>
    private static void SpringtideStormY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0f, 0.12f, 0.8f, 1.05f);
        if (env > 0f)
        {
            for (int i = 0; i < 16; i++)
            {
                float u = (c.Age * 1.2f + Fx.Rand(c.Seed, i, 595)) % 1f;
                float a = u * MathF.Tau * 2f + i;
                float reach = Math.Clamp((c.Age - 0.04f) / 0.4f, 0f, 1f);
                var centre = Vector3.Lerp(c.From, c.Aim, reach);
                fx.Sprite(FxShape.Petal, centre + new Vector3(MathF.Cos(a) * h * (0.4f + 0.7f * u), MathF.Sin(a) * h * (0.3f + 0.5f * u), 0.1f * i % 0.4f), h * 0.14f, Fx.Fade(i % 3 == 0 ? Rgb(255, 120, 190) : Rgb(255, 200, 230), env), FxBlend.Alpha, a);
            }
            fx.Sprite(FxShape.Swirl, Vector3.Lerp(c.From, c.Aim, Math.Clamp((c.Age - 0.04f) / 0.4f, 0f, 1f)), h * 1.7f, Fx.Fade(Rgb(255, 170, 220), env * 0.5f), FxBlend.Alpha, c.Age * 4f);
        }
        if (c.Cue.Landed)
        {
            Fx.Bolt(fx, c.Age, c.Seed, c.To + new Vector3(h * 0.3f, h * 1.1f, 0f), c.To - Vector3.UnitY * h * 0.2f, I, I + 0.2f, h * 0.05f, Rgb(255, 130, 200), White, 1);
            L3Flower(fx, c.To + new Vector3(0f, h * 0.1f, 0.4f), h * 0.35f * Fx.Envelope(c.Age, I, I + 0.08f, I + 0.2f, I + 0.4f), Rgb(255, 150, 210), White, 0.9f, c.Age);
            Jolt(fx, c.Age, I, 0.6f, 0.25f);
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Mystical Power: a hexagon of violet power lights round the user, a bright orb in the middle of it flung at the foe in a short beam, and a Sp. Atk arrow climbs.</summary>
    private static void MysticalPowerY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.3f, 0.4f);
        if (env > 0f)
        {
            L5Ring(fx, c.From, h * 0.7f, h * 0.65f, 6, FxShape.Hex, Fx.Fade(Rgb(214, 130, 255), env), h * 0.2f, c.Age * 6f);
            L5Ring(fx, c.From, h * 0.7f, h * 0.65f, 6, FxShape.Glow, Fx.Fade(White, env * 0.5f), h * 0.3f, c.Age * 6f, FxBlend.Add);
            fx.Sprite(FxShape.Orb, c.From, h * 0.4f * env, Fx.Fade(Rgb(255, 230, 255), 1f), FxBlend.Alpha);
        }
        Fx.Orb(fx, c, 0.2f, I, h * 0.34f, Rgb(214, 130, 255), White);
        L3Up(fx, c.Age, c.FromFeet, h, 0.5f, 1, Rgb(230, 150, 255));
        if (c.Cue.Landed) Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.4f, c.ToSize * 1.5f, Fx.Fade(Rgb(214, 130, 255), 0.9f));
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Raging Fury: three pillars of fire burst from the ground round the foe, a face of fury flaring in the flames and a ring of embers thrown out.</summary>
    private static void RagingFuryY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        for (int i = 0; i < 3; i++)
        {
            float t = I - 0.2f + i * 0.1f;
            float k = (c.Age - t) / 0.55f;
            if (k < 0f || k > 1f) continue;
            float env = Fx.Envelope(k, 0f, 0.15f, 0.55f, 1f);
            var foot = c.ToFeet + new Vector3((i - 1) * h * 0.65f, 0.05f, 0.1f);
            fx.Streak(FxShape.Flame, foot + Vector3.UnitY * h * 0.8f * env, Vector3.UnitY, h * 0.5f, 3.4f, Fx.Fade(col.Main, env), FxBlend.Add);
            fx.Streak(FxShape.Flame, foot + Vector3.UnitY * h * 0.7f * env, Vector3.UnitY, h * 0.28f, 3f, Fx.Fade(col.Light, env), FxBlend.Add);
            Fx.Burst(fx, c.Seed + i, c.Age, foot, h, t, 0.5f, 4, FxShape.Ember, Rgb(255, 160, 60), White, 0.07f, 1.0f, -0.4f, salt: 526 + i);
        }
        if (c.Cue.Landed)
        {
            float env = Fx.Envelope(c.Age, I, I + 0.06f, I + 0.35f, I + 0.6f);
            fx.Sprite(FxShape.Exclaim, c.To + new Vector3(h * 0.4f, h * 0.8f, 0.4f), h * 0.4f, Fx.Fade(Rgb(255, 80, 60), env), FxBlend.Alpha);
            GroundRing(fx, c.Age, c.ToFeet, I, 0.45f, h * 1.8f, Rgb(255, 150, 60));
            Jolt(fx, c.Age, I, 0.9f, 0.3f);
        }
        Impact(c, col, fx, 1.2f);
    }

    /// <summary>Wave Crash: the user wraps itself in a rolling wave and charges, the wave rearing over the foe and crashing, a second splash breaking back over the user.</summary>
    private static void WaveCrashY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float k = Math.Clamp((c.Age - 0.03f) / (I - 0.03f), 0f, 1f);
        float env = Fx.Envelope(c.Age, 0f, 0.08f, I + 0.1f, I + 0.35f);
        if (env > 0f)
        {
            var p = Vector3.Lerp(c.From, c.Aim, Fx.EaseOut(k));
            for (int i = 0; i < 3; i++)
                fx.Sprite(FxShape.Wave, p + new Vector3(-h * 0.4f * i * (1f - k * 0.5f), h * (0.15f + 0.2f * MathF.Sin(i + c.Age * 12f)), 0.05f * i), h * (1.3f - 0.1f * i), Fx.Fade(i % 2 == 0 ? col.Main : col.Light, env), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, p, h * 1.3f, Fx.Fade(col.Main, env * 0.3f), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            fx.Sprite(FxShape.Wave, c.To + new Vector3(0f, c.ToSize * 0.3f, 0.4f), c.ToSize * 2.2f * Fx.Envelope(c.Age, I - 0.05f, I, I + 0.15f, I + 0.5f), Fx.Fade(col.Main, 0.85f), FxBlend.Alpha);
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.7f, 9, FxShape.Drop, col.Main, White, 0.11f, 1.1f, 2.2f, FxBlend.Alpha, salt: 527);
            Fx.Burst(fx, c.Seed + 4, c.Age, c.From, h, I + 0.08f, 0.6f, 8, FxShape.Drop, col.Light, White, 0.09f, 0.8f, 2.0f, FxBlend.Alpha, salt: 528);
            Jolt(fx, c.Age, I, 1.0f, 0.35f);
        }
        Impact(c, col, fx, 1.35f);
    }

    /// <summary>Chloroblast: the user takes in sunlight with a ring of leaves, then fires a broad beam of green and gold, its leaves shredded along it, and the recoil dims the user's glow.</summary>
    private static void ChloroblastY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float charge = Fx.Envelope(c.Age, 0f, 0.1f, 0.16f, 0.22f);
        if (charge > 0f)
        {
            L5Gather(fx, c.Age, c.Seed, c.From + new Vector3(h * 0.4f, 0f, 0.2f), h, 0f, 0.22f, 8, FxShape.Leaf, Rgb(120, 230, 90), Rgb(255, 240, 150), 0.14f, 1.3f);
            fx.Sprite(FxShape.Glow, c.From + new Vector3(h * 0.4f, 0f, 0.2f), h * 0.9f * charge, Fx.Fade(Rgb(255, 240, 150), 0.7f));
        }
        Fx.Beam(fx, c, 0.18f, 0.66f, h * 0.36f, Fx.Fade(Rgb(90, 220, 80), 0.9f), Rgb(255, 252, 190), 14);
        Fx.Stream(fx, c, 0.2f, 0.62f, 0.22f, 10, FxShape.Leaf, Rgb(110, 220, 90), Rgb(230, 250, 140), 0.14f, spread: 0.45f, spin: 5f, blend: FxBlend.Alpha);
        float rec = Fx.Envelope(c.Age, 0.5f, 0.58f, 0.7f, 0.9f);
        if (rec > 0f) fx.Sprite(FxShape.Glow, c.From, h * 1.2f, Fx.Fade(Rgb(240, 80, 70), rec * 0.45f), FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            fx.ScreenFlash(Rgb(230, 255, 190), 0.22f * Fx.Envelope(c.Age, I - 0.02f, I, I + 0.05f, I + 0.22f));
            Jolt(fx, c.Age, I, 1.0f, 0.35f);
        }
        Impact(c, col, fx, 1.5f);
    }

    /// <summary>Mountain Gale: a bitter wind drives down the field and great icicles are driven out of the ground under the foe, a white blast of snow over them.</summary>
    private static void MountainGaleY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        SpeedLines(c, fx, 0f, I, 7, 0.8f);
        Fx.Stream(fx, c, 0f, 0.4f, 0.2f, 8, FxShape.Snowflake, White, col.Light, 0.12f, spread: 0.5f, spin: 3f, blend: FxBlend.Alpha);
        for (int i = 0; i < 5; i++)
        {
            float t = I - 0.12f + i * 0.04f;
            float rise = Fx.EaseOut(Math.Clamp((c.Age - t) / 0.12f, 0f, 1f));
            if (rise <= 0f) continue;
            float fade = Fx.Envelope(c.Age, t, t + 0.05f, 0.95f, 1.15f);
            float x = (i - 2) * h * 0.38f;
            float len = h * (1.4f + 0.5f * (i % 3 == 1 ? 1 : 0.4f)) * rise;
            fx.Streak(FxShape.Shard, c.ToFeet + new Vector3(x, len * 0.5f, 0.1f), new Vector3(x * 0.15f, 1f, 0f), h * 0.2f, len / (h * 0.2f), Fx.Fade(Rgb(176, 232, 255), fade), FxBlend.Alpha);
            fx.Streak(FxShape.Shard, c.ToFeet + new Vector3(x, len * 0.5f, 0.15f), new Vector3(x * 0.15f, 1f, 0f), h * 0.08f, len / (h * 0.08f), Fx.Fade(White, fade * 0.8f));
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.8f, 8, FxShape.Smoke, Rgb(240, 250, 255, 210), Rgb(200, 226, 245, 210), 0.22f, 0.7f, -0.1f, FxBlend.Alpha, salt: 529);
            Jolt(fx, c.Age, I, 0.8f, 0.3f);
        }
        Impact(c, col, fx, 1.2f);
    }

    /// <summary>Victory Dance: the user dances with both fists high, orange stars bursting at each step, and three arrows climb at once for Attack, Defense and Speed.</summary>
    private static void VictoryDanceY13b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.08f, 0.85f, 1.1f);
        if (env <= 0f) return;
        for (int s = -1; s <= 1; s += 2)
        {
            float bob = MathF.Sin(c.Age * 16f + s);
            fx.Sprite(FxShape.Fist, c.From + new Vector3(s * h * 0.55f, h * (0.55f + 0.1f * bob), 0.3f), h * 0.3f, Fx.Fade(Rgb(255, 150, 80), env), FxBlend.Alpha, s * 0.3f);
        }
        for (int i = 0; i < 4; i++)
        {
            float t = 0.08f + i * 0.18f;
            Fx.Burst(fx, c.Seed + i, c.Age, c.FromFeet + new Vector3((i % 2 == 0 ? -1f : 1f) * h * 0.4f, h * 0.2f, 0.2f), h, t, 0.45f, 5, FxShape.Star, Rgb(255, 190, 80), Rgb(255, 250, 200), 0.13f, 0.9f, 0.5f, salt: 530 + i);
        }
        GroundRing(fx, c.Age, c.FromFeet, 0.1f, 0.4f, h * 1.1f, Rgb(255, 160, 70));
        GroundRing(fx, c.Age, c.FromFeet, 0.4f, 0.4f, h * 1.1f, Rgb(255, 200, 100));
        L3Up(fx, c.Age, c.FromFeet, h, 0.35f, 3, Rgb(255, 190, 90));
    }

    /// <summary>Headlong Rush: the user charges with its head down in a plume of brown dust, ploughing a crack across the ground into the foe, and its own guard arrows sink after.</summary>
    private static void HeadlongRushY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float k = Math.Clamp((c.Age - 0.02f) / (I - 0.02f), 0f, 1f);
        if (c.Age < I + 0.3f)
        {
            var a = c.FromFeet + Vector3.UnitY * 0.05f;
            var b = c.ToFeet + Vector3.UnitY * 0.05f;
            float env = Fx.Envelope(c.Age, 0f, 0.05f, I + 0.1f, I + 0.3f);
            fx.Ribbon(new[] { a, Vector3.Lerp(a, b, k) }, h * 0.18f, Fx.Fade(Rgb(110, 84, 56), env), FxBlend.Alpha);
            for (int i = 0; i < 8; i++)
            {
                float u = Math.Clamp(k - i * 0.08f, 0f, 1f);
                fx.Sprite(FxShape.Smoke, Vector3.Lerp(a, b, u) + new Vector3(0f, h * (0.15f + 0.15f * i * 0.3f), 0.1f), h * (0.4f + 0.04f * i), Fx.Fade(Rgb(190, 160, 120, 210), env * (1f - i * 0.1f)), FxBlend.Alpha, i);
            }
            fx.Sprite(FxShape.Horn, Vector3.Lerp(c.From, c.Aim, Fx.EaseOut(k)) + new Vector3(h * 0.3f, h * 0.1f, 0.2f), h * 0.5f, Fx.Fade(Rgb(240, 226, 200), env), FxBlend.Alpha, -0.3f);
        }
        if (c.Cue.Landed)
        {
            fx.Flat(FxShape.Crack, c.ToFeet + Vector3.UnitY * 0.05f, Vector3.UnitY, c.ToSize * 2f, Fx.Fade(Rgb(60, 44, 30), Fx.Envelope(c.Age, I, I + 0.04f, 0.95f, 1.15f)), FxBlend.Alpha);
            L2Down(fx, c.Age, c.Seed, c.FromFeet, h, I + 0.15f, 2, Rgb(230, 170, 90));
            Jolt(fx, c.Age, I, 1.0f, 0.35f);
        }
        Impact(c, col, fx, 1.3f);
    }

    /// <summary>Barb Barrage: a volley of barbed spines, each with a poison-wet hook, rains in from the user in a quick fan and sticks into the foe in a ring of drops.</summary>
    private static void BarbBarrageY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        for (int i = 0; i < 8; i++)
        {
            float t = I - 0.16f + i * 0.025f;
            float k = (c.Age - (t - 0.14f)) / 0.14f;
            if (k < 0f || c.Age > t + 0.55f) continue;
            var land = c.To + new Vector3(Fx.Signed(c.Seed, i, 600) * h * 0.4f, Fx.Signed(c.Seed, i, 601) * h * 0.4f, 0.1f);
            var start = c.From + new Vector3(c.FromSize * 0.3f, Fx.Signed(c.Seed, i, 602) * c.FromSize * 0.3f, 0f);
            var p = Vector3.Lerp(start, land, Fx.EaseOut(Math.Min(1f, k)));
            float fade = c.Age < t ? 1f : Math.Max(0f, 1f - (c.Age - t) / 0.55f);
            fx.Streak(FxShape.Streak, p, land - start, h * 0.07f, 5f, Fx.Fade(Rgb(100, 40, 130), fade), FxBlend.Alpha);
            fx.Sprite(FxShape.Drop, p, h * 0.1f, Fx.Fade(Rgb(190, 100, 230), fade), FxBlend.Alpha);
            if (c.Cue.Landed && c.Age >= t) fx.Sprite(FxShape.Drop, land + new Vector3(0f, -h * 0.2f * Math.Min(1f, (c.Age - t) * 3f), 0.1f), h * 0.08f, Fx.Fade(Rgb(172, 78, 210), fade), FxBlend.Alpha);
        }
        if (c.Cue.Landed) Jolt(fx, c.Age, I, 0.5f, 0.2f);
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Esper Wing: two pale pink wings of light unfold behind the user and sweep forward, slicing crescents of pink-white off their edges, and a Speed arrow climbs.</summary>
    private static void EsperWingY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float open = Fx.Envelope(c.Age, 0f, 0.1f, I - 0.04f, I + 0.12f);
        if (open > 0f)
            for (int s = -1; s <= 1; s += 2)
            {
                fx.Sprite(FxShape.Feather, c.From + new Vector3(s * h * 0.6f, h * 0.45f, -0.05f), h * 0.7f, Fx.Fade(Rgb(255, 150, 215), open * 0.8f), FxBlend.Alpha, s * 1.1f);
            }
        for (int i = 0; i < 2; i++)
        {
            float t = I - 0.04f + i * 0.0f;
            float k = (c.Age - (t - 0.2f + i * 0.06f)) / 0.2f;
            if (k < 0f || k > 1.4f) continue;
            var p = Vector3.Lerp(c.From, c.Aim, Fx.EaseOut(Math.Min(1f, k)));
            L5Arc(fx, p, h * 0.6f, h * 0.9f, i == 0 ? 0.9f : -0.9f, i == 0 ? -0.9f : 0.9f, h * 0.1f, Fx.Fade(Rgb(255, 190, 235), Math.Max(0f, 1f - k * 0.7f)), 10, FxBlend.Add);
        }
        L3Up(fx, c.Age, c.FromFeet, h, 0.5f, 1, Rgb(255, 170, 225));
        if (c.Cue.Landed) Fx.Slash(fx, c.Age, c.Seed, c.To, c.ToSize, I - 0.04f, 2, 0.05f, Rgb(255, 150, 215), White);
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Bitter Malice: a pale chill creeps out of the user in ghost-fire and frost, wisps and snow circling the foe and its Attack arrow sinking in the cold.</summary>
    private static void BitterMaliceY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Fx.Stream(fx, c, 0f, 0.4f, 0.3f, 8, FxShape.Wisp, Rgb(150, 120, 250), Rgb(200, 220, 255), 0.18f, arc: 0.04f, spread: 0.4f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0.04f, 0.4f, 0.3f, 6, FxShape.Snowflake, White, Rgb(190, 220, 255), 0.1f, spread: 0.4f, spin: 2f, blend: FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            float env = Fx.Envelope(c.Age, I, I + 0.1f, 0.9f, 1.1f);
            fx.Sprite(FxShape.Smoke, c.To, h * 1.5f, Fx.Fade(Rgb(190, 210, 245, 200), env * 0.6f), FxBlend.Alpha, c.Age);
            L5Ring(fx, c.To, h * 0.65f, h * 0.55f, 5, FxShape.Wisp, Fx.Fade(Rgb(150, 120, 250), env), h * 0.3f, c.Age * 4f);
            L5Ring(fx, c.To, h * 0.5f, h * 0.4f, 4, FxShape.Snowflake, Fx.Fade(White, env), h * 0.14f, -c.Age * 3f);
            L2Down(fx, c.Age, c.Seed, c.ToFeet, h, I + 0.1f, 1, Rgb(190, 200, 255));
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Shelter: four steel plates slide in from either side and top and bottom to close over the user like a shell, gleams running along the seams, and Evasion arrows climb.</summary>
    private static void ShelterY13b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.12f, 0.8f, 1.05f);
        if (env <= 0f) return;
        float close = Fx.EaseOut(Math.Clamp((c.Age - 0.03f) / 0.3f, 0f, 1f));
        var dirs = new[] { new Vector3(1f, 0f, 0f), new Vector3(-1f, 0f, 0f), new Vector3(0f, 1f, 0f), new Vector3(0f, -1f, 0f) };
        foreach (var d in dirs)
        {
            var at = c.From + d * h * (1.2f * (1f - close) + 0.3f) + new Vector3(0f, 0f, 0.3f);
            fx.Sprite(FxShape.Hex, at, h * 0.4f, Fx.Fade(Rgb(150, 164, 192), env * 0.9f), FxBlend.Alpha, d.X == 0f ? 0f : MathF.PI / 2f);
        }
        fx.Sprite(FxShape.Ring, c.From, h * (1.1f - 0.2f * close), Fx.Fade(Rgb(210, 220, 240), env * 0.45f), FxBlend.Alpha);
        Fx.Glint(fx, c.Age, c.From + new Vector3(h * 0.3f, h * 0.2f, 0.5f), h, 0.38f, White, 0.55f);
        L3Up(fx, c.Age, c.FromFeet, h, 0.4f, 2, Rgb(210, 226, 255));
    }

    /// <summary>Triple Arrows: three arrows, red, orange and yellow, are loosed in turn, each landing with its own small mark (a crack, a flinch, a flash).</summary>
    private static void TripleArrowsY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var tints = new[] { Rgb(236, 80, 60), Rgb(255, 160, 60), Rgb(255, 224, 80) };
        for (int d = 0; d < 3; d++)
        {
            float t = I - 0.08f + d * 0.07f;
            float k = (c.Age - (t - 0.22f)) / 0.22f;
            if (k < 0f || c.Age > t + 0.3f) continue;
            var to = c.Aim + new Vector3(0f, (1 - d) * h * 0.28f, 0f);
            var from = c.From + new Vector3(0f, (1 - d) * c.FromSize * 0.2f, 0f);
            var p = Vector3.Lerp(from, to, Fx.EaseOut(Math.Min(1f, k)));
            float fade = c.Age < t ? 1f : Math.Max(0f, 1f - (c.Age - t) / 0.3f);
            fx.Streak(FxShape.Streak, p, to - from, c.FromSize * 0.14f, 8f, Fx.Fade(tints[d], fade), FxBlend.Alpha);
            fx.Streak(FxShape.Arrow, p + Vector3.Normalize(to - from) * c.FromSize * 0.3f, to - from, c.FromSize * 0.26f, 1.4f, Fx.Fade(White, fade), FxBlend.Alpha);
            if (c.Cue.Landed && c.Age >= t)
            {
                Fx.Contact(fx, c.Age, c.Seed + d, to, h, t, 0.2f, tints[d], White);
                if (d == 0) L2Down(fx, c.Age, c.Seed, c.ToFeet, h, t + 0.05f, 1, tints[0]);
                if (d == 1) L2Flinch(c, fx, t + 0.05f);
            }
        }
        if (c.Cue.Landed) Jolt(fx, c.Age, I, 0.6f, 0.25f);
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Infernal Parade: a line of lantern-lit wisps in violet flame marches one behind another to the foe, bowing as they pass, and sets it burning in cold fire.</summary>
    private static void InfernalParadeY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        for (int i = 0; i < 6; i++)
        {
            float k = (c.Age - 0.02f - i * 0.045f) / 0.34f;
            if (k < 0f || k > 1f) continue;
            var p = Vector3.Lerp(c.From, c.Aim, k) + new Vector3(0f, h * 0.12f * MathF.Abs(MathF.Sin(k * 14f)), 0f);
            fx.Sprite(FxShape.Flame, p, c.FromSize * 0.4f, Fx.Fade(Rgb(150, 90, 250), 0.9f), FxBlend.Add);
            fx.Sprite(FxShape.Flame, p, c.FromSize * 0.24f, Fx.Fade(Rgb(214, 190, 255), 0.9f));
            fx.Sprite(FxShape.Dot, p - new Vector3(0f, c.FromSize * 0.28f, 0f), c.FromSize * 0.1f, Fx.Fade(Rgb(70, 50, 100), 0.9f), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            float env = Fx.Envelope(c.Age, I, I + 0.08f, 0.85f, 1.05f);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 8, FxShape.Flame, Rgb(150, 90, 250), Rgb(214, 190, 255), 0.17f, 0.8f, -0.8f, salt: 531);
            fx.Sprite(FxShape.Flame, c.To, h * 1.1f * env, Fx.Fade(Rgb(130, 80, 230), env * 0.9f));
            Jolt(fx, c.Age, I, 0.5f, 0.2f);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Ceaseless Edge: two dark slashes cut the foe, and the cutting blades are left standing in the ground round it, a ring of black shells ready to cut again.</summary>
    private static void CeaselessEdgeY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        if (c.Cue.Landed) Fx.Slash(fx, c.Age, c.Seed, c.To, h, I - 0.06f, 2, 0.06f, Rgb(120, 80, 150), White);
        Strike(c, fx, FxShape.Sword, new Vector3(1.0f, 0.9f, 0f), I, 0.8f, Rgb(70, 48, 90), 2.3f, 0.12f);
        if (c.Cue.Landed)
            for (int i = 0; i < 5; i++)
            {
                float rise = Fx.EaseOut(Math.Clamp((c.Age - I - 0.04f * i) / 0.1f, 0f, 1f));
                float a = i * MathF.Tau / 5f + 0.4f;
                var foot = c.ToFeet + new Vector3(MathF.Cos(a) * h * 0.85f, 0.05f, MathF.Sin(a) * h * 0.45f);
                fx.Sprite(FxShape.Sword, foot + new Vector3(0f, h * 0.22f * rise, 0f), h * 0.45f * rise, Fx.Fade(Rgb(80, 56, 100), Fx.Envelope(c.Age, I, I + 0.12f, 1.0f, 1.2f)), FxBlend.Alpha, 1.57f - 0.2f * (i - 2));
            }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Bleakwind Storm: a freezing gale scours across the whole side, white streaks of wind and drifting snowflakes in a tilting gyre, the cold biting the foes.</summary>
    private static void BleakwindStormY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.85f, 1.1f);
        if (env > 0f)
        {
            for (int i = 0; i < 10; i++)
            {
                float u = (c.Age * 1.5f + Fx.Rand(c.Seed, i, 610)) % 1f;
                var a = Vector3.Lerp(c.From, c.Aim, 0.15f) + new Vector3(0f, (Fx.Rand(c.Seed, i, 611) - 0.3f) * h * 1.3f, Fx.Signed(c.Seed, i, 612) * 0.3f);
                var p = Vector3.Lerp(a, c.Aim + new Vector3(h * 0.6f, a.Y - c.Aim.Y, 0f), u);
                fx.Streak(FxShape.Streak, p, c.Aim - c.From, h * 0.1f, 6f, Fx.Fade(Rgb(214, 236, 255), env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha);
            }
            fx.Sprite(FxShape.Swirl, c.To, h * 1.7f, Fx.Fade(Rgb(190, 226, 255), env * 0.45f), FxBlend.Alpha, c.Age * 4f);
            L5Ring(fx, c.To, h * 0.8f, h * 0.55f, 6, FxShape.Snowflake, Fx.Fade(White, env), h * 0.15f, c.Age * 5f);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 6, FxShape.Snowflake, White, Rgb(176, 226, 255), 0.13f, 0.9f, 0.3f, FxBlend.Alpha, salt: 532);
            Jolt(fx, c.Age, I, 0.6f, 0.3f);
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Wildbolt Storm: a black storm cloud builds over the field, rain slashing down from it and forks of lightning striking the foes in turn.</summary>
    private static void WildboltStormY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0f, 0.14f, 0.85f, 1.1f);
        var top = c.To + new Vector3(0f, h * 1.15f, 0f);
        if (env > 0f)
        {
            for (int i = 0; i < 4; i++)
                fx.Sprite(FxShape.Smoke, top + new Vector3((i - 1.5f) * h * 0.65f, (i % 2) * h * 0.1f, 0.1f * i), h * 0.8f * Fx.EaseOut(Math.Min(1f, c.Age / 0.2f)), Fx.Fade(Rgb(70, 74, 96, 230), env), FxBlend.Alpha, i + c.Age);
            fx.Sprite(FxShape.Glow, top, h * 1.6f, Fx.Fade(Rgb(255, 238, 120), env * 0.3f * (0.5f + 0.5f * MathF.Sin(c.Age * 30f))));
            Fx.Rain(fx, c.Age, c.Seed, c.ToFeet, h, 0.1f, 0.95f, 0.3f, 14, FxShape.Streak, Rgb(180, 200, 235), Rgb(120, 150, 210), 0.07f, FxBlend.Alpha, 5f);
        }
        for (int i = 0; i < 3; i++)
        {
            float t = I - 0.2f + i * 0.1f;
            Fx.Bolt(fx, c.Age, c.Seed + i, top + new Vector3((i - 1) * h * 0.5f, -h * 0.2f, 0.1f), c.ToFeet + new Vector3((i - 1) * h * 0.4f, h * 0.2f, 0.1f), t, t + 0.18f, h * 0.06f, col.Main, col.Light, 2);
        }
        if (c.Cue.Landed)
        {
            fx.ScreenFlash(Rgb(255, 250, 200), 0.2f * Fx.Envelope(c.Age, I - 0.02f, I, I + 0.04f, I + 0.2f));
            Jolt(fx, c.Age, I, 0.9f, 0.3f);
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Sandsear Storm: a tornado of scorching sand, orange at its heart, climbs round the foe, embers and heat-haze streaming off it.</summary>
    private static void SandsearStormY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0.02f, 0.15f, 0.85f, 1.1f);
        if (env > 0f)
        {
            for (int t = 0; t < 4; t++)
            {
                var pts = new Vector3[16];
                for (int i = 0; i <= 15; i++)
                {
                    float u = i / 15f;
                    float a = c.Age * 9f + u * MathF.Tau * 2.2f + t * MathF.PI / 2f;
                    float r = h * (0.4f + 0.7f * u);
                    pts[i] = c.ToFeet + new Vector3(MathF.Cos(a) * r, h * 1.7f * u, MathF.Sin(a) * r * 0.5f + 0.1f);
                }
                fx.Ribbon(pts, h * 0.14f, Fx.Fade(t % 2 == 0 ? Rgb(230, 180, 100) : Rgb(255, 140, 60), env * 0.85f), FxBlend.Alpha);
            }
            fx.Sprite(FxShape.Glow, c.To, h * 1.4f, Fx.Fade(Rgb(255, 150, 60), env * 0.35f), FxBlend.Alpha);
            Fx.Rain(fx, c.Age, c.Seed, c.To, h, 0.1f, 0.95f, 0.4f, 8, FxShape.Ember, Rgb(255, 160, 60), Rgb(255, 230, 130), 0.07f);
        }
        if (c.Cue.Landed) Jolt(fx, c.Age, I, 0.7f, 0.3f);
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Lunar Blessing: a crescent moon rises over the user and its side and lets fall a soft rain of silver light, the glow mending and clearing everyone under it.</summary>
    private static void LunarBlessingY13b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.15f, 0.8f, 1.05f);
        if (env <= 0f) return;
        var moon = c.FromFeet + new Vector3(h * 0.9f, h * 1.35f, 0.1f);
        float rise = Fx.EaseOut(Math.Clamp(c.Age / 0.3f, 0f, 1f));
        moon += Vector3.UnitY * h * 0.3f * (1f - rise);
        L5Arc(fx, moon, h * 0.55f, h * 0.55f, 1.3f, 5.0f, h * 0.2f, Fx.Fade(Rgb(255, 246, 190), env), 18);
        L5Arc(fx, moon, h * 0.55f, h * 0.55f, 1.3f, 5.0f, h * 0.07f, Fx.Fade(White, env), 18, FxBlend.Add);
        fx.Sprite(FxShape.Glow, moon, h * 1.6f, Fx.Fade(Rgb(255, 240, 170), env * 0.35f), FxBlend.Alpha);
        Fx.Rain(fx, c.Age, c.Seed, c.FromFeet, h, 0.2f, 0.9f, 0.45f, 12, FxShape.Spark, Rgb(255, 250, 210), Rgb(190, 210, 255), 0.09f);
        fx.Sprite(FxShape.Glow, c.From, h * 1.2f, Fx.Fade(Rgb(210, 230, 255), env * 0.3f), FxBlend.Alpha);
        L2Heal(fx, c.Age, c.Seed, c.FromFeet, h, 0.35f);
    }

    /// <summary>Take Heart: a golden heart swells in the user's chest and pulses out two rings, the user steadies, and Sp. Atk and Sp. Def arrows climb together.</summary>
    private static void TakeHeartY13b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.85f, 1.1f);
        if (env <= 0f) return;
        float beat = 1f + 0.18f * MathF.Max(0f, MathF.Sin(c.Age * 16f));
        fx.Sprite(FxShape.Heart, c.From + new Vector3(h * 0.5f, h * 0.15f, 0.3f), h * 0.32f * beat * Fx.EaseOut(Math.Min(1f, c.Age / 0.15f)), Fx.Fade(Rgb(255, 214, 90), env), FxBlend.Alpha);
        fx.Sprite(FxShape.Glow, c.From, h * 1.2f, Fx.Fade(Rgb(255, 230, 140), env * 0.45f), FxBlend.Alpha);
        PulseRing(fx, c.Age, c.From, 0.12f, 0.35f, h * 0.3f, h * 1.2f, Rgb(255, 214, 90), White);
        PulseRing(fx, c.Age, c.From, 0.34f, 0.35f, h * 0.3f, h * 1.3f, Rgb(255, 160, 200), White);
        L3Up(fx, c.Age, c.FromFeet, h, 0.35f, 2, Rgb(255, 150, 215));
        L3Up(fx, c.Age, c.FromFeet + new Vector3(h * 0.16f, 0f, 0f), h, 0.45f, 2, Rgb(120, 190, 255));
    }

    /// <summary>Tera Blast: a faceted crystal of the move's type forms in the user's arms, its facets throwing prisms, and is blasted out as a beam of shards in that colour.</summary>
    private static void TeraBlastY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var gem = c.From + new Vector3(h * 0.5f, h * 0.1f, 0.3f);
        float env = Fx.Envelope(c.Age, 0f, 0.12f, 0.2f, 0.28f);
        if (env > 0f)
        {
            L5Ring(fx, gem, h * 0.3f, h * 0.3f, 6, FxShape.Shard, Fx.Fade(col.Light, env), h * 0.26f, c.Age * 7f, FxBlend.Alpha, true);
            fx.Sprite(FxShape.Hex, gem, h * 0.55f * env, Fx.Fade(col.Main, env), FxBlend.Alpha, c.Age * 3f);
            fx.Sprite(FxShape.Star, gem, h * 0.5f * env, Fx.Fade(White, env * 0.5f), FxBlend.Add, c.Age * 3f);
            fx.Sprite(FxShape.Glow, gem, h * 1.2f, Fx.Fade(col.Main, env * 0.5f));
        }
        Fx.Beam(fx, c, 0.18f, 0.6f, h * 0.26f, Fx.Fade(col.Main, 0.9f), White, 8);
        Fx.Stream(fx, c, 0.2f, 0.55f, 0.2f, 9, FxShape.Shard, col.Light, White, 0.13f, spread: 0.35f, spin: 3f, blend: FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.7f, 10, FxShape.Shard, col.Light, col.Main, 0.14f, 1.1f, 1.0f, FxBlend.Alpha, salt: 533);
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.45f, c.ToSize * 1.8f, Fx.Fade(col.Light, 0.9f), FxShape.Hex);
            Jolt(fx, c.Age, I, 0.8f, 0.3f);
        }
        Impact(c, col, fx, 1.2f);
    }

    /// <summary>Silk Trap: the user spins a web of fine white threads upright before it, spokes and rings strung tight, a green glint running round the web.</summary>
    private static void SilkTrapY13b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0.02f, 0.18f, 0.85f, 1.1f);
        if (env <= 0f) return;
        float draw = Fx.EaseOut(Math.Clamp((c.Age - 0.03f) / 0.3f, 0f, 1f));
        var mid = c.FromFeet + L1Toward(c) * h * 0.2f + new Vector3(h * 0.4f, h * 0.55f, 0.35f);
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f;
            fx.Ribbon(new[] { mid, mid + new Vector3(MathF.Cos(a), MathF.Sin(a), 0f) * h * 0.65f * draw }, h * 0.018f, Fx.Fade(White, env), FxBlend.Alpha);
        }
        foreach (float f in new[] { 0.3f, 0.6f, 0.95f })
        {
            var pts = new Vector3[9];
            for (int i = 0; i <= 8; i++)
            {
                float a = i * MathF.Tau / 8f;
                pts[i] = mid + new Vector3(MathF.Cos(a), MathF.Sin(a), 0f) * h * 0.65f * f * draw;
            }
            fx.Ribbon(pts, h * 0.016f, Fx.Fade(Rgb(226, 246, 226), env), FxBlend.Alpha);
        }
        float g = c.Age * 5f;
        fx.Sprite(FxShape.Spark, mid + new Vector3(MathF.Cos(g), MathF.Sin(g), 0.1f) * h * 0.6f, h * 0.16f, Fx.Fade(Rgb(150, 240, 130), env));
        fx.Sprite(FxShape.Glow, mid, h * 1.1f, Fx.Fade(Rgb(190, 235, 160), env * 0.2f), FxBlend.Alpha);
    }

    /// <summary>Axe Kick: the user's leg is raised high overhead and chops down, a crescent of orange drawn behind it, the ground cracking and stars wheeling over the foe's head.</summary>
    private static void AxeKickY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        L5Slam(c, fx, FxShape.Foot, new Vector3(0.2f, 1.8f, 0f), 0.6f, Rgb(255, 190, 120), Rgb(255, 120, 60), 0.5f, I, 0.22f);
        float env = Fx.Envelope(c.Age, I - 0.2f, I - 0.12f, I, I + 0.2f);
        if (env > 0f)
        {
            L5Arc(fx, c.To + new Vector3(0f, -h * 0.3f, 0f), h * 1.7f, h * 1.9f, 1.9f, 1.2f + 1.4f * Math.Clamp((c.Age - (I - 0.2f)) / 0.2f, 0f, 1f), h * 0.18f, Fx.Fade(Rgb(255, 150, 70), env), 14);
            L5Arc(fx, c.To + new Vector3(0f, -h * 0.3f, 0f), h * 1.7f, h * 1.9f, 1.9f, 1.2f + 1.4f * Math.Clamp((c.Age - (I - 0.2f)) / 0.2f, 0f, 1f), h * 0.06f, Fx.Fade(White, env), 14, FxBlend.Add);
        }
        if (c.Cue.Landed)
        {
            fx.Flat(FxShape.Crack, c.ToFeet + Vector3.UnitY * 0.05f, Vector3.UnitY, h * 1.8f, Fx.Fade(Rgb(90, 70, 56), Fx.Envelope(c.Age, I, I + 0.04f, 0.95f, 1.15f)), FxBlend.Alpha);
            L5Ring(fx, c.ToFeet + new Vector3(0f, h * 1.1f, 0f), h * 0.4f, h * 0.1f, 3, FxShape.Star, Fx.Fade(Rgb(255, 236, 130), Fx.Envelope(c.Age, I, I + 0.08f, I + 0.5f, I + 0.7f)), h * 0.13f, c.Age * 5f);
            Jolt(fx, c.Age, I, 1.0f, 0.35f);
        }
        Impact(c, col, fx, 1.3f);
    }

    /// <summary>Last Respects: pale ghost-lights of fallen friends rise from the ground round the user and rush in a stream into the foe, small crosses glowing where they leave.</summary>
    private static void LastRespectsY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        for (int i = 0; i < 6; i++)
        {
            float rise = Fx.EaseOut(Math.Clamp((c.Age - i * 0.03f) / 0.2f, 0f, 1f));
            float fade = Fx.Envelope(c.Age, i * 0.03f, i * 0.03f + 0.06f, 0.22f, 0.3f);
            if (fade > 0f)
            {
                var at = c.FromFeet + new Vector3((i - 2.5f) * c.FromSize * 0.4f, 0.05f, 0.1f);
                fx.Sprite(FxShape.Cross, at + Vector3.UnitY * c.FromSize * 0.2f * rise, c.FromSize * 0.2f, Fx.Fade(Rgb(210, 210, 240), fade), FxBlend.Alpha);
                fx.Sprite(FxShape.Wisp, at + Vector3.UnitY * c.FromSize * (0.2f + 0.5f * rise), c.FromSize * 0.3f, Fx.Fade(Rgb(170, 150, 255), fade));
            }
            float k = (c.Age - 0.16f - i * 0.025f) / 0.2f;
            if (k > 0f && k < 1f)
            {
                var p = Lob(c.FromFeet + new Vector3((i - 2.5f) * c.FromSize * 0.4f, c.FromSize * 0.6f, 0f), c.Aim, k, 0.1f);
                fx.Sprite(FxShape.Wisp, p, c.FromSize * 0.55f, Fx.Fade(Rgb(180, 160, 255), 1f - 0.4f * k));
                fx.Sprite(FxShape.Glow, p, c.FromSize * 0.45f, Fx.Fade(White, 0.7f));
            }
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 9, FxShape.Wisp, Rgb(170, 140, 255), White, 0.15f, 0.9f, -0.3f, salt: 534);
            Jolt(fx, c.Age, I, 0.7f, 0.3f);
        }
        Impact(c, col, fx, 1.15f);
    }

    /// <summary>Lumina Crash: a bright sphere of rainbow light is flung at the foe and crashes into it in a spray of coloured shards, the colours sinking its Sp. Def.</summary>
    private static void LuminaCrashY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var hues = new[] { Rgb(255, 120, 150), Rgb(255, 214, 100), Rgb(120, 230, 150), Rgb(110, 190, 255), Rgb(190, 130, 255) };
        float k = (c.Age - 0.04f) / (I - 0.04f);
        if (k >= 0f && k <= 1f)
        {
            var p = Vector3.Lerp(c.From, c.Aim, Fx.EaseOut(k));
            fx.Sprite(FxShape.Glow, p, c.FromSize * 1.3f, Fx.Fade(White, 0.8f));
            fx.Sprite(FxShape.Orb, p, c.FromSize * 0.45f, Fx.Fade(White, 1f), FxBlend.Alpha);
            for (int i = 0; i < 5; i++)
                fx.Sprite(FxShape.Spark, p + new Vector3(MathF.Cos(k * 8f + i * 1.26f), MathF.Sin(k * 8f + i * 1.26f), 0f) * c.FromSize * 0.3f, c.FromSize * 0.22f, Fx.Fade(hues[i], 0.95f));
        }
        if (c.Cue.Landed)
        {
            for (int i = 0; i < 5; i++)
            {
                Fx.Burst(fx, c.Seed + i, c.Age, c.To, h, I, 0.7f, 3, FxShape.Shard, hues[i], White, 0.14f, 1.1f, 0.8f, FxBlend.Alpha, salt: 535 + i);
                Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I + i * 0.03f, 0.4f, h * (0.9f + 0.2f * i), Fx.Fade(hues[i], 0.9f));
            }
            fx.ScreenFlash(White, 0.3f * Fx.Envelope(c.Age, I - 0.02f, I, I + 0.05f, I + 0.26f));
            L2Down(fx, c.Age, c.Seed, c.ToFeet, h, I + 0.12f, 2, Rgb(200, 170, 255));
            Jolt(fx, c.Age, I, 0.9f, 0.3f);
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Order Up: a silver cloche is lifted from a platter and a blue-gold dragon coils out of it, snapping at the foe and leaving a ring of sparkle.</summary>
    private static void OrderUpY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var tray = c.From + new Vector3(h * 0.55f, h * 0.15f, 0.3f);
        float env = Fx.Envelope(c.Age, 0f, 0.08f, 0.22f, 0.32f);
        if (env > 0f)
        {
            float lift = Fx.EaseOut(Math.Clamp((c.Age - 0.08f) / 0.16f, 0f, 1f));
            L2Ellipse(fx, tray - new Vector3(0f, h * 0.18f, 0f), h * 0.5f, h * 0.08f, h * 0.04f, Fx.Fade(Rgb(220, 226, 238), env), 14, 0f, FxBlend.Alpha);
            fx.Sprite(FxShape.Orb, tray + new Vector3(0f, h * (0.05f + 0.5f * lift), 0f), h * 0.6f, Fx.Fade(Rgb(214, 222, 238), env), FxBlend.Alpha);
            fx.Sprite(FxShape.Dot, tray + new Vector3(0f, h * (0.36f + 0.5f * lift), 0.05f), h * 0.1f, Fx.Fade(White, env), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, tray, h * 0.9f * lift, Fx.Fade(Rgb(255, 224, 140), env * 0.7f));
        }
        float k = (c.Age - 0.18f) / (I - 0.18f);
        if (k > 0f && k < 1.3f)
        {
            var pts = new Vector3[10];
            for (int i = 0; i <= 9; i++)
            {
                float u = i / 9f;
                pts[i] = Vector3.Lerp(tray, c.Aim, Math.Min(1f, Fx.EaseOut(Math.Clamp(k, 0f, 1f)) * (1f - 0.7f * u))) + new Vector3(0f, MathF.Sin(u * 7f - c.Age * 18f) * h * 0.15f, 0.1f);
            }
            fx.Ribbon(pts, h * 0.36f, Fx.Fade(Rgb(100, 170, 250), Math.Max(0f, 1.3f - k)), FxBlend.Alpha);
            fx.Ribbon(pts, h * 0.07f, Fx.Fade(Rgb(255, 214, 110), Math.Max(0f, 1.3f - k)), FxBlend.Add);
            fx.Sprite(FxShape.Fang, pts[0], h * 0.4f, Fx.Fade(White, Math.Max(0f, 1.3f - k)), FxBlend.Alpha, -1.2f);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.6f, 8, FxShape.Spark, Rgb(255, 224, 140), Rgb(150, 190, 255), 0.1f, 0.9f, salt: 536);
            Jolt(fx, c.Age, I, 0.7f, 0.25f);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Jet Punch: a fist leads a hard jet of water from the user and drives it into the foe, a blue cone of spray trailing the fist and bursting at the end.</summary>
    private static void JetPunchY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float k = Math.Clamp((c.Age - 0.04f) / (I - 0.04f), 0f, 1f);
        if (c.Age < I + 0.1f)
        {
            var dir = Vector3.Normalize(c.Aim - c.From);
            var tip = Vector3.Lerp(c.From + dir * h * 0.4f, c.Aim, Fx.EaseOut(k));
            float fade = c.Age < I ? 1f : Math.Max(0f, 1f - (c.Age - I) / 0.1f);
            for (int i = 0; i < 3; i++)
                fx.Ribbon(new[] { tip - dir * h * (1.6f + 0.2f * i) + new Vector3(0f, (i - 1) * h * 0.2f, 0f), tip + new Vector3(0f, (i - 1) * h * 0.05f, 0f) }, h * (0.26f - 0.06f * Math.Abs(i - 1)), Fx.Fade(i == 1 ? col.Light : col.Main, 0.85f * fade), FxBlend.Alpha);
            fx.Sprite(FxShape.Fist, tip, h * 0.6f, Fx.Fade(Rgb(190, 230, 255), fade), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, tip, h * 0.9f, Fx.Fade(White, 0.5f * fade));
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.6f, 12, FxShape.Drop, col.Main, White, 0.1f, 1.2f, 1.8f, FxBlend.Alpha, salt: 537);
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.4f, c.ToSize * 1.4f, Fx.Fade(col.Light, 0.9f));
            Jolt(fx, c.Age, I, 0.7f, 0.25f);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Spicy Extract: a squirt of red-hot chili sauce spatters the foe, flames licking up from the drops, an Attack arrow rising and a Defense arrow sinking.</summary>
    private static void SpicyExtractY13b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        float k = (c.Age - 0.03f) / (I - 0.03f);
        if (k >= 0f && k <= 1f)
            for (int i = 0; i < 4; i++)
                fx.Sprite(FxShape.Drop, Lob(c.From + new Vector3(c.FromSize * 0.3f, 0f, 0f), c.Aim + new Vector3((i - 1.5f) * h * 0.15f, 0f, 0f), Math.Max(0f, k - i * 0.04f), 0.05f), c.FromSize * 0.26f, Fx.Fade(Rgb(220, 40, 30), 1f), FxBlend.Alpha);
        if (c.Age >= I)
        {
            float env = Fx.Envelope(c.Age, I, I + 0.06f, 0.85f, 1.05f);
            for (int i = 0; i < 6; i++)
            {
                var p = c.To + new Vector3(Fx.Signed(c.Seed, i, 620) * h * 0.4f, Fx.Signed(c.Seed, i, 621) * h * 0.4f, 0.3f);
                fx.Sprite(FxShape.Drop, p, h * 0.14f, Fx.Fade(Rgb(210, 36, 30), env), FxBlend.Alpha);
                fx.Sprite(FxShape.Flame, p + new Vector3(0f, h * 0.15f, 0f), h * 0.2f, Fx.Fade(Rgb(255, 150, 50), env), FxBlend.Add);
            }
            L3Up(fx, c.Age, c.ToFeet, h, I + 0.05f, 2, Rgb(255, 130, 70));
            L2Down(fx, c.Age, c.Seed, c.ToFeet + new Vector3(h * 0.25f, 0f, 0f), h, I + 0.15f, 1, Rgb(150, 190, 255));
        }
    }

    /// <summary>Spin Out: the user spins like a steel wheel, sparks pouring off its rim in a scraping arc, and cuts a pair of black skid marks into the foe, its Speed arrows falling.</summary>
    private static void SpinOutY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float k = Math.Clamp((c.Age - 0.03f) / (I - 0.03f), 0f, 1f);
        if (c.Age < I + 0.12f)
        {
            float fade = c.Age < I ? 1f : Math.Max(0f, 1f - (c.Age - I) / 0.12f);
            var p = Vector3.Lerp(c.FromFeet + new Vector3(0f, h * 0.45f, 0.1f), c.ToFeet + new Vector3(0f, h * 0.45f, 0.1f), Fx.EaseOut(k));
            L5Wheel(fx, p, h * 0.9f, c.Age * 22f, Rgb(176, 188, 210), Rgb(120, 132, 156), fade);
            Fx.Stream(fx, c, 0.04f, I, 0.15f, 8, FxShape.Spark, Rgb(255, 232, 150), White, 0.09f, spread: 0.2f, trail: 2f);
        }
        if (c.Cue.Landed)
        {
            for (int s = -1; s <= 1; s += 2)
                fx.Ribbon(new[] { c.ToFeet + new Vector3(-c.ToSize * 0.8f, 0.05f, s * 0.2f), c.ToFeet + new Vector3(c.ToSize * 0.6f, 0.05f, s * 0.2f) }, c.ToSize * 0.06f, Fx.Fade(Rgb(40, 40, 50), Fx.Envelope(c.Age, I, I + 0.04f, 0.95f, 1.15f)), FxBlend.Alpha);
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.5f, 10, FxShape.Spark, Rgb(255, 232, 150), White, 0.09f, 1.2f, 1.0f, salt: 538);
            L2Down(fx, c.Age, c.Seed, c.FromFeet, h, I + 0.15f, 2, Rgb(170, 190, 230));
            Jolt(fx, c.Age, I, 0.7f, 0.25f);
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Population Bomb: ten small cream mice stream in at the foe, ears and tails flying, each landing its own small hit one right after another.</summary>
    private static void PopulationBombY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        for (int i = 0; i < 10; i++)
        {
            float t = I - 0.27f + i * 0.03f;
            float k = (c.Age - (t - 0.22f)) / 0.22f;
            if (k < 0f || c.Age > t + 0.15f) continue;
            var from = c.From + new Vector3(0f, (i % 5 - 2) * c.FromSize * 0.2f, (i % 2) * 0.2f);
            var to = c.Aim + new Vector3(Fx.Signed(c.Seed, i, 630) * h * 0.3f, Fx.Signed(c.Seed, i, 631) * h * 0.3f, 0.1f);
            var p = Vector3.Lerp(from, to, Fx.EaseOut(Math.Min(1f, k))) + new Vector3(0f, MathF.Abs(MathF.Sin(k * 9f)) * c.FromSize * 0.12f, 0f);
            float fade = c.Age < t ? 1f : Math.Max(0f, 1f - (c.Age - t) / 0.15f);
            fx.Sprite(FxShape.Dot, p, c.FromSize * 0.32f, Fx.Fade(Rgb(246, 232, 200), fade), FxBlend.Alpha);
            fx.Sprite(FxShape.Dot, p + new Vector3(c.FromSize * 0.12f, c.FromSize * 0.14f, 0f), c.FromSize * 0.13f, Fx.Fade(Rgb(255, 170, 170), fade), FxBlend.Alpha);
            fx.Streak(FxShape.Streak, p - new Vector3(c.FromSize * 0.18f, 0f, 0f), to - from, c.FromSize * 0.04f, 3f, Fx.Fade(Rgb(210, 190, 160), fade * 0.8f), FxBlend.Alpha);
            if (c.Cue.Landed && c.Age >= t) Fx.Contact(fx, c.Age, c.Seed + i, to, h, t, 0.12f, col.Main, col.Light);
        }
        if (c.Cue.Landed) Jolt(fx, c.Age, I, 0.7f, 0.3f);
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Ice Spinner: the user spins on a disc of flat ice that grows under it, skating out along the ground to the foe and shearing the ground into glittering shards.</summary>
    private static void IceSpinnerY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float k = Math.Clamp((c.Age - 0.03f) / (I - 0.03f), 0f, 1f);
        if (c.Age < I + 0.25f)
        {
            float fade = Fx.Envelope(c.Age, 0f, 0.05f, I + 0.05f, I + 0.25f);
            var p = Vector3.Lerp(c.FromFeet, c.ToFeet, Fx.EaseOut(k)) + Vector3.UnitY * 0.06f;
            fx.Sprite(FxShape.Swirl, p + new Vector3(0f, h * 0.45f, 0.2f), h * 1.3f, Fx.Fade(Rgb(190, 236, 255), fade), FxBlend.Alpha, c.Age * 14f);
            fx.Sprite(FxShape.Snowflake, p + new Vector3(0f, h * 0.45f, 0.25f), h * 0.8f, Fx.Fade(White, fade * 0.9f), FxBlend.Alpha, -c.Age * 10f);
            fx.Ribbon(new[] { c.FromFeet + Vector3.UnitY * 0.05f, p }, h * 0.12f, Fx.Fade(Rgb(176, 230, 255), fade * 0.7f), FxBlend.Alpha);
            for (int i = 0; i < 6; i++)
                fx.Sprite(FxShape.Shard, p + new Vector3(MathF.Cos(c.Age * 14f + i) * h * 0.7f, h * 0.15f, MathF.Sin(c.Age * 14f + i) * h * 0.3f), h * 0.14f, Fx.Fade(White, fade), FxBlend.Alpha, i);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.7f, 9, FxShape.Shard, Rgb(190, 236, 255), White, 0.13f, 1.0f, 1.2f, FxBlend.Alpha, salt: 539);
            Jolt(fx, c.Age, I, 0.7f, 0.25f);
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Glaive Rush: the user hurls itself in with a vast glaive swung wide, two dragon wings of indigo light spread behind it, and is left open, glowing red.</summary>
    private static void GlaiveRushY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float wing = Fx.Envelope(c.Age, 0f, 0.08f, I, I + 0.25f);
        if (wing > 0f)
            for (int s = -1; s <= 1; s += 2)
                fx.Sprite(FxShape.Feather, c.From + new Vector3(-h * 0.1f, s * h * 0.3f + h * 0.2f, -0.05f), c.FromSize * 0.6f, Fx.Fade(Rgb(120, 104, 250), wing * 0.7f), FxBlend.Alpha, 2.2f + s * 0.7f);
        L5Slam(c, fx, FxShape.Sword, new Vector3(0.8f, 1.8f, 0f), 1.5f, Rgb(130, 112, 240), Rgb(190, 180, 255), 1.2f, I, 0.24f);
        if (c.Cue.Landed)
        {
            L5Arc(fx, c.To, h * 1.5f, h * 1.2f, 2.4f, 0.4f, h * 0.2f, Fx.Fade(Rgb(120, 104, 250), Fx.Envelope(c.Age, I - 0.05f, I, I + 0.1f, I + 0.3f)), 12);
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.45f, h * 1.9f, Fx.Fade(Rgb(180, 170, 255), 0.9f));
            Jolt(fx, c.Age, I, 1.0f, 0.35f);
        }
        float open = Fx.Envelope(c.Age, I + 0.1f, I + 0.18f, 0.95f, 1.15f);
        if (open > 0f) fx.Sprite(FxShape.Glow, c.From, c.FromSize * 1.3f, Fx.Fade(Rgb(240, 70, 70), open * 0.5f), FxBlend.Alpha);
        Impact(c, col, fx, 1.4f);
    }

    /// <summary>Revival Blessing: a column of white light stands over the user with a halo turning at its head, white feathers falling out of it and a warm glow raising everyone.</summary>
    private static void RevivalBlessingY13b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.15f, 0.8f, 1.05f);
        if (env <= 0f) return;
        float grow = Fx.EaseOut(Math.Clamp(c.Age / 0.25f, 0f, 1f));
        fx.Streak(FxShape.Beam, c.FromFeet + new Vector3(h * 0.9f, h * 1.2f, 0f), Vector3.UnitY, h * 0.5f, 2.6f * grow, Fx.Fade(Rgb(255, 244, 200), env * 0.45f), FxBlend.Alpha);
        fx.Streak(FxShape.Beam, c.FromFeet + new Vector3(h * 0.9f, h * 1.2f, 0f), Vector3.UnitY, h * 0.18f, 3.4f * grow, Fx.Fade(White, env * 0.8f), FxBlend.Alpha);
        var halo = c.FromFeet + new Vector3(h * 0.9f, h * 1.55f, 0.2f);
        L2Ellipse(fx, halo, h * 0.4f, h * 0.1f, h * 0.045f, Fx.Fade(Rgb(255, 232, 140), env), 16, 0f, FxBlend.Alpha);
        Fx.Rain(fx, c.Age, c.Seed, c.FromFeet + new Vector3(h * 0.9f, 0f, 0f), h, 0.1f, 0.9f, 0.55f, 10, FxShape.Feather, White, Rgb(255, 244, 200), 0.2f, FxBlend.Alpha);
        GroundRing(fx, c.Age, c.FromFeet, 0.15f, 0.5f, h * 1.2f, Rgb(255, 240, 180));
        L2Heal(fx, c.Age, c.Seed, c.FromFeet, h, 0.3f);
    }

    /// <summary>Salt Cure: a rain of coarse white salt crystals pours over the foe and grows into a crust on it, its edges pink and glittering and each grain sizzling.</summary>
    private static void SaltCureY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Fx.Rain(fx, c.Age, c.Seed, c.ToFeet, h * 1.0f, 0f, I + 0.1f, 0.25f, 14, FxShape.Hex, White, Rgb(255, 214, 224), 0.17f, FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            float env = Fx.Envelope(c.Age, I, I + 0.12f, 0.9f, 1.1f);
            float grow = Fx.EaseOut(Math.Clamp((c.Age - I) / 0.3f, 0f, 1f));
            for (int i = 0; i < 9; i++)
            {
                float a = i * MathF.Tau / 9f;
                var p = c.To + new Vector3(MathF.Cos(a) * h * 0.45f, MathF.Sin(a) * h * 0.4f, 0.3f);
                fx.Sprite(FxShape.Rock, p, h * 0.22f * grow, Fx.Fade(i % 2 == 0 ? White : Rgb(255, 214, 224), env), FxBlend.Alpha, a);
            }
            fx.Sprite(FxShape.Glow, c.To, h * 1.2f, Fx.Fade(Rgb(255, 224, 235), env * 0.3f), FxBlend.Alpha);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 6, FxShape.Spark, White, Rgb(255, 200, 215), 0.07f, 0.7f, salt: 540);
            Jolt(fx, c.Age, I, 0.5f, 0.2f);
        }
        Impact(c, col, fx, 0.85f);
    }

    /// <summary>Triple Dive: the user dives at the foe three times from three heights, each dive a blue arc into a splash ring, the last the biggest.</summary>
    private static void TripleDiveY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        for (int i = 0; i < 3; i++)
        {
            float t = I - 0.2f + i * 0.1f;
            float k = (c.Age - (t - 0.14f)) / 0.14f;
            if (k < 0f || c.Age > t + 0.45f) continue;
            float reach = Fx.EaseOut(Math.Min(1f, k));
            var from = c.To + new Vector3(-h * (1.5f - 0.3f * i), h * (1.3f - 0.3f * i), 0f);
            var p = Vector3.Lerp(from, c.To, reach);
            float fade = c.Age < t ? 1f : Math.Max(0f, 1f - (c.Age - t) / 0.45f);
            if (c.Age < t + 0.05f)
            {
                fx.Ribbon(new[] { from, Vector3.Lerp(from, p, 0.5f) + new Vector3(0f, h * 0.2f, 0f), p }, h * 0.28f, Fx.Fade(col.Main, fade), FxBlend.Alpha);
                fx.Sprite(FxShape.Drop, p, h * 0.5f, Fx.Fade(col.Light, fade), FxBlend.Alpha);
            }
            if (c.Cue.Landed && c.Age >= t)
            {
                GroundRing(fx, c.Age, c.ToFeet, t, 0.4f, h * (0.9f + 0.4f * i), col.Light);
                Fx.Burst(fx, c.Seed + i, c.Age, c.To, h, t, 0.45f, 4 + 2 * i, FxShape.Drop, col.Main, White, 0.1f, 1.0f, 2.0f, FxBlend.Alpha, salt: 541 + i);
            }
        }
        if (c.Cue.Landed) Jolt(fx, c.Age, I, 0.7f, 0.25f);
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Mortal Spin: the user spins like a poisoned top, a ring of violet blades whirling out from it and a sickly cloud thrown wide, clearing away whatever hangs about.</summary>
    private static void MortalSpinY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.08f, I, I + 0.3f);
        if (env > 0f)
        {
            float reach = Fx.EaseOut(Math.Clamp(c.Age / (I + 0.1f), 0f, 1f));
            var centre = Vector3.Lerp(c.From, c.Aim, reach * 0.7f);
            for (int ring = 0; ring < 2; ring++)
                L5Ring(fx, centre, h * (0.55f + 0.5f * ring + 0.5f * reach), h * (0.35f + 0.3f * ring + 0.3f * reach), 8, FxShape.Shard, Fx.Fade(ring == 0 ? Rgb(190, 100, 230) : Rgb(120, 54, 160), env), h * 0.26f, c.Age * (ring == 0 ? 14f : -11f), FxBlend.Alpha, true);
            fx.Sprite(FxShape.Swirl, centre, h * 1.4f, Fx.Fade(Rgb(172, 78, 210), env * 0.6f), FxBlend.Alpha, c.Age * 12f);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.7f, 8, FxShape.Smoke, Rgb(170, 90, 200, 210), Rgb(110, 60, 140, 210), 0.2f, 0.8f, -0.1f, FxBlend.Alpha, salt: 542);
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.5f, 6, FxShape.Drop, Rgb(190, 100, 230), White, 0.1f, 1.0f, 1.5f, FxBlend.Alpha, salt: 543);
            Jolt(fx, c.Age, I, 0.5f, 0.2f);
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Doodle: a crayon scribble in four colours is drawn round the foe, line by line, and is then drawn across to the user as a scribbled copy of what it was.</summary>
    private static void DoodleY13b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0f, 0.08f, 0.85f, 1.1f);
        if (env <= 0f) return;
        var tints = new[] { Rgb(255, 90, 90), Rgb(255, 214, 70), Rgb(90, 190, 255), Rgb(120, 220, 110) };
        float draw = Math.Clamp(c.Age / 0.4f, 0f, 1f);
        for (int s = 0; s < 4; s++)
        {
            int n = (int)(14 * draw) + 1;
            var pts = new Vector3[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float u = i / 14f;
                pts[i] = c.To + new Vector3(MathF.Cos(u * MathF.Tau * 1.6f + s * 1.5f) * h * (0.45f + 0.15f * Fx.Signed(c.Seed, i + s * 17, 640)), MathF.Sin(u * MathF.Tau * 1.6f + s * 1.5f) * h * (0.4f + 0.15f * Fx.Signed(c.Seed, i + s * 17, 641)), 0.2f);
            }
            fx.Ribbon(pts, h * 0.045f, Fx.Fade(tints[s], env), FxBlend.Alpha);
        }
        float flight = (c.Age - 0.4f) / 0.4f;
        if (flight > 0f && flight < 1f)
            for (int s = 0; s < 4; s++)
                fx.Sprite(FxShape.Star, Vector3.Lerp(c.To, c.From, Fx.EaseOut(flight)) + new Vector3(0f, (s - 1.5f) * h * 0.14f, 0.3f), h * 0.12f, Fx.Fade(tints[s], env), FxBlend.Alpha, s);
        Fx.Glint(fx, c.Age, c.From + new Vector3(0f, 0f, 0.3f), c.FromSize, 0.8f, White, 0.4f);
    }

    /// <summary>Fillet Away: the user shaves itself with a clean white cut and sheds scales, slim and quick, a drop of red leaving it as three arrows of power climb.</summary>
    private static void FilletAwayY13b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.08f, 0.8f, 1.05f);
        if (env <= 0f) return;
        Fx.Slash(fx, c.Age, c.Seed, c.From, h, 0.04f, 3, 0.07f, Rgb(255, 250, 240), White);
        Fx.Burst(fx, c.Seed, c.Age, c.From, h, 0.14f, 0.55f, 9, FxShape.Hex, Rgb(255, 230, 220), Rgb(255, 180, 190), 0.09f, 0.9f, 0.9f, FxBlend.Alpha, salt: 544);
        Fx.Burst(fx, c.Seed, c.Age, c.From, h, 0.2f, 0.5f, 3, FxShape.Drop, Rgb(230, 50, 60), Rgb(255, 130, 130), 0.09f, 0.7f, 1.6f, FxBlend.Alpha, salt: 545);
        fx.Sprite(FxShape.Glow, c.From, h * 1.0f, Fx.Fade(Rgb(255, 245, 235), env * 0.4f), FxBlend.Alpha);
        L3Up(fx, c.Age, c.FromFeet, h, 0.32f, 3, Rgb(255, 214, 190));
    }
}
