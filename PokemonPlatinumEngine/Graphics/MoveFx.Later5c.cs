using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

// Plan 25 - Y13b, the last part: Generation 9's moves from Kowtow Cleave (869) to Malignant Chain (919). As
// MoveFx.Later5.cs, whose L5 helpers they share. Terastallization's own look and the marks Generation 9's conditions
// carry (Salt Cure, Syrup Bomb) are other work's; these are the moves'.
internal static partial class MoveFx
{
    private static Dictionary<string, FxRecipe> Later5c() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Kowtow Cleave"] = KowtowCleaveY13b,
        ["Flower Trick"] = FlowerTrickY13b,
        ["Torch Song"] = TorchSongY13b,
        ["Aqua Step"] = AquaStepY13b,
        ["Raging Bull"] = RagingBullY13b,
        ["Make It Rain"] = MakeItRainY13b,
        ["Psyblade"] = PsybladeY13b,
        ["Hydro Steam"] = HydroSteamY13b,
        ["Ruination"] = RuinationY13b,
        ["Collision Course"] = CollisionCourseY13b,
        ["Electro Drift"] = ElectroDriftY13b,
        ["Shed Tail"] = ShedTailY13b,
        ["Chilly Reception"] = ChillyReceptionY13b,
        ["Tidy Up"] = TidyUpY13b,
        ["Snowscape"] = SnowscapeY13b,
        ["Pounce"] = PounceY13b,
        ["Trailblaze"] = TrailblazeY13b,
        ["Chilling Water"] = ChillingWaterY13b,
        ["Hyper Drill"] = HyperDrillY13b,
        ["Twin Beam"] = TwinBeamY13b,
        ["Rage Fist"] = RageFistY13b,
        ["Armor Cannon"] = ArmorCannonY13b,
        ["Bitter Blade"] = BitterBladeY13b,
        ["Double Shock"] = DoubleShockY13b,
        ["Gigaton Hammer"] = GigatonHammerY13b,
        ["Comeuppance"] = ComeuppanceY13b,
        ["Aqua Cutter"] = AquaCutterY13b,
        ["Blazing Torque"] = BlazingTorqueY13b,
        ["Wicked Torque"] = WickedTorqueY13b,
        ["Noxious Torque"] = NoxiousTorqueY13b,
        ["Combat Torque"] = CombatTorqueY13b,
        ["Magical Torque"] = MagicalTorqueY13b,
        ["Blood Moon"] = BloodMoonY13b,
        ["Matcha Gotcha"] = MatchaGotchaY13b,
        ["Syrup Bomb"] = SyrupBombY13b,
        ["Ivy Cudgel"] = IvyCudgelY13b,
        ["Electro Shot"] = ElectroShotY13b,
        ["Tera Starstorm"] = TeraStarstormY13b,
        ["Fickle Beam"] = FickleBeamY13b,
        ["Burning Bulwark"] = BurningBulwarkY13b,
        ["Thunderclap"] = ThunderclapY13b,
        ["Mighty Cleave"] = MightyCleaveY13b,
        ["Tachyon Cutter"] = TachyonCutterY13b,
        ["Hard Press"] = HardPressY13b,
        ["Dragon Cheer"] = DragonCheerY13b,
        ["Alluring Voice"] = AlluringVoiceY13b,
        ["Temper Flare"] = TemperFlareY13b,
        ["Supercell Slam"] = SupercellSlamY13b,
        ["Psychic Noise"] = PsychicNoiseY13b,
        ["Upper Hand"] = UpperHandY13b,
        ["Malignant Chain"] = MalignantChainY13b
    };

    /// <summary>A wheel of one of the five Torque moves: the engine's wheel rolls from the user to the foe on the ground with its own exhaust behind it.</summary>
    private static void L5Torque(in FxCtx c, FxList fx, Color rim, Color spoke, Action<FxList, Vector3, float, float> exhaust)
    {
        float h = c.FromSize;
        float k = Math.Clamp((c.Age - 0.03f) / (I - 0.03f), 0f, 1f);
        if (c.Age >= I + 0.1f) return;
        float fade = c.Age < I ? 1f : Math.Max(0f, 1f - (c.Age - I) / 0.1f);
        var p = Vector3.Lerp(c.FromFeet + new Vector3(0f, h * 0.38f, 0.15f), c.ToFeet + new Vector3(0f, h * 0.38f, 0.15f), Fx.EaseOut(k));
        L5Wheel(fx, p, h * 0.8f, c.Age * 20f, rim, spoke, fade);
        L5Wheel(fx, p + new Vector3(-h * 0.45f, 0f, -0.05f), h * 0.6f, c.Age * 24f, rim, spoke, fade * 0.8f);
        exhaust(fx, p - new Vector3(h * 0.7f, -h * 0.05f, 0f), fade, h);
        fx.Ribbon(new[] { c.FromFeet + Vector3.UnitY * 0.05f, Vector3.Lerp(c.FromFeet, c.ToFeet, Fx.EaseOut(k)) + Vector3.UnitY * 0.05f }, h * 0.1f, Fx.Fade(Rgb(40, 40, 46), 0.6f * fade), FxBlend.Alpha);
    }

    /// <summary>Kowtow Cleave: the user bows low, a deep arc of dark light dipping with it, then cuts up through the foe in a long dark blade that cannot miss, a locking ring on the target first.</summary>
    private static void KowtowCleaveY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float lockOn = Fx.Envelope(c.Age, 0f, 0.06f, I - 0.08f, I);
        if (lockOn > 0f) fx.Sprite(FxShape.Target, c.To, h * 0.7f * (1.2f - 0.3f * lockOn), Fx.Fade(Rgb(255, 90, 100), lockOn), FxBlend.Alpha);
        float bow = Fx.Envelope(c.Age, 0f, 0.1f, 0.2f, 0.28f);
        if (bow > 0f) L5Arc(fx, c.FromFeet + new Vector3(c.FromSize * 0.2f, c.FromSize * 0.9f, 0.1f), c.FromSize * 0.6f, c.FromSize * 0.45f, 0.3f, 1.9f, c.FromSize * 0.07f, Fx.Fade(Rgb(120, 80, 150), bow), 10);
        float k = (c.Age - (I - 0.16f)) / 0.16f;
        if (k > 0f && c.Age < I + 0.3f)
        {
            float fade = c.Age < I ? 1f : Math.Max(0f, 1f - (c.Age - I) / 0.3f);
            float sweep = Fx.EaseOut(Math.Min(1f, k));
            L5Arc(fx, c.To + new Vector3(-h * 1.4f, -h * 0.3f, 0.2f), h * 1.5f, h * 1.6f, -0.5f, -0.5f + 1.5f * sweep, h * 0.4f, Fx.Fade(Rgb(60, 36, 90), fade), 14);
            L5Arc(fx, c.To + new Vector3(-h * 1.4f, -h * 0.3f, 0.25f), h * 1.5f, h * 1.6f, -0.5f, -0.5f + 1.5f * sweep, h * 0.12f, Fx.Fade(Rgb(255, 140, 160), fade), 14, FxBlend.Add);
        }
        if (c.Cue.Landed) { Fx.Slash(fx, c.Age, c.Seed, c.To, h, I - 0.04f, 1, 0.05f, Rgb(120, 70, 150), White); Jolt(fx, c.Age, I, 0.8f, 0.3f); }
        Impact(c, col, fx, 1.15f);
    }

    /// <summary>Flower Trick: a bouquet is thrown that bursts in confetti, flowers of every colour popping open over the foe and petals blowing everywhere, a star at the heart of it.</summary>
    private static void FlowerTrickY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = (c.Age - 0.03f) / (I - 0.03f);
        if (k >= 0f && k <= 1f)
        {
            var p = Lob(c.From, c.Aim, k, 0.1f);
            L3Flower(fx, p, c.FromSize * 0.2f, Rgb(255, 120, 170), Rgb(255, 230, 120), 1f, k * 8f);
        }
        if (c.Cue.Landed)
        {
            var petals = new[] { Rgb(255, 110, 160), Rgb(255, 214, 80), Rgb(150, 190, 255), Rgb(190, 130, 255), Rgb(255, 255, 255) };
            for (int i = 0; i < 5; i++)
            {
                float a = i * MathF.Tau / 5f + 0.4f;
                float pop = Fx.EaseOut(Math.Clamp((c.Age - I - i * 0.025f) / 0.12f, 0f, 1f));
                L3Flower(fx, c.To + new Vector3(MathF.Cos(a) * h * 0.55f, MathF.Sin(a) * h * 0.5f, 0.3f), h * 0.2f * pop, petals[i], Rgb(255, 245, 190), Fx.Envelope(c.Age, I, I + 0.1f, 0.9f, 1.1f), a);
            }
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.9f, 14, FxShape.Petal, Rgb(255, 150, 190), Rgb(255, 235, 150), 0.12f, 1.0f, 0.6f, FxBlend.Alpha, spin: 4f, salt: 550);
            fx.Sprite(FxShape.Star, c.To, h * 0.6f * Fx.Envelope(c.Age, I, I + 0.04f, I + 0.14f, I + 0.3f), Fx.Fade(Rgb(255, 244, 200), 0.7f), FxBlend.Alpha, 0.3f);
            Jolt(fx, c.Age, I, 0.5f, 0.2f);
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Torch Song: the user sings and each note leaves its mouth as a small tongue of flame, a score of fire rising and flying to the foe while a Sp. Atk arrow climbs.</summary>
    private static void TorchSongY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        for (int i = 0; i < 5; i++)
        {
            float k = (c.Age - 0.02f - i * 0.06f) / 0.32f;
            if (k < 0f || k > 1f) continue;
            var p = Lob(c.From + new Vector3(h * 0.3f, h * 0.1f, 0.2f), c.Aim, k, 0.03f + 0.02f * i);
            fx.Sprite(FxShape.Note, p, h * 0.45f, Fx.Fade(Rgb(255, 120, 40), 0.95f), FxBlend.Alpha, 0.2f * MathF.Sin(k * 9f));
            fx.Sprite(FxShape.Flame, p + new Vector3(0f, h * 0.1f, 0f), h * 0.28f, Fx.Fade(Rgb(255, 214, 90), 0.9f));
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.6f, 9, FxShape.Flame, col.Main, col.Light, 0.15f, 0.9f, -0.6f, salt: 551);
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.6f, 4, FxShape.Note, Rgb(255, 210, 120), White, 0.14f, 0.9f, 0.2f, FxBlend.Alpha, salt: 552);
        }
        L3Up(fx, c.Age, c.FromFeet, h, 0.5f, 1, Rgb(255, 150, 90));
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Aqua Step: light blue ripples bloom under three quick footfalls on the way to the foe and the user ends on a dancing kick, a Speed arrow climbing.</summary>
    private static void AquaStepY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        for (int i = 0; i < 4; i++)
        {
            float u = (i + 1) / 4f;
            float t = 0.03f + u * (I - 0.03f);
            GroundRing(fx, c.Age, Vector3.Lerp(c.FromFeet, c.ToFeet, u * 0.85f), t, 0.45f, h * 1.1f, i % 2 == 0 ? col.Main : col.Light);
            if (c.Age > t && c.Age < t + 0.4f) fx.Sprite(FxShape.Ring, Vector3.Lerp(c.FromFeet, c.ToFeet, u * 0.85f) + new Vector3(0f, h * 0.15f, 0.3f), h * (0.3f + 1.2f * (c.Age - t)), Fx.Fade(col.Light, 1f - (c.Age - t) / 0.4f), FxBlend.Alpha);
            if (c.Age > t && c.Age < t + 0.25f)
                fx.Sprite(FxShape.Drop, Vector3.Lerp(c.FromFeet, c.ToFeet, u * 0.85f) + new Vector3(0f, h * 0.2f * (c.Age - t) * 6f, 0.1f), h * 0.12f, Fx.Fade(col.Light, 1f - (c.Age - t) / 0.25f), FxBlend.Alpha);
        }
        Strike(c, fx, FxShape.Foot, new Vector3(-1.3f, -0.2f, 0f), I, 0.7f, Rgb(150, 210, 255), 0.3f, 0.13f);
        if (c.Cue.Landed) Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.5f, 8, FxShape.Drop, col.Main, White, 0.09f, 1.0f, 1.8f, FxBlend.Alpha, salt: 553);
        L3Up(fx, c.Age, c.FromFeet, h, 0.55f, 1, Rgb(150, 220, 255));
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Raging Bull: the user lowers a pair of red horns and charges in a stampede of dust, shattering a pane of hexagonal glass as it passes through the screen before the foe.</summary>
    private static void RagingBullY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float k = Math.Clamp((c.Age - 0.03f) / (I - 0.03f), 0f, 1f);
        if (c.Age < I + 0.1f)
        {
            float fade = c.Age < I ? 1f : Math.Max(0f, 1f - (c.Age - I) / 0.1f);
            var p = Vector3.Lerp(c.From, c.Aim, Fx.EaseOut(k));
            for (int s = -1; s <= 1; s += 2)
                fx.Sprite(FxShape.Horn, p + new Vector3(h * 0.45f, s * h * 0.35f, 0.2f), h * 0.5f, Fx.Fade(Rgb(240, 70, 60), fade), FxBlend.Alpha, s * 0.7f);
            fx.Sprite(FxShape.Glow, p, h * 1.2f, Fx.Fade(Rgb(255, 90, 70), 0.45f * fade));
            for (int i = 0; i < 5; i++)
                fx.Sprite(FxShape.Smoke, Vector3.Lerp(c.FromFeet, p - new Vector3(0f, h * 0.5f, 0f), 1f - i * 0.12f) + new Vector3(0f, h * 0.1f, 0.1f), h * 0.4f, Fx.Fade(Rgb(210, 180, 150, 220), fade * (1f - i * 0.15f)), FxBlend.Alpha, i);
        }
        if (c.Cue.Landed)
        {
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathF.Tau / 6f;
                float fly = Fx.EaseOut(Math.Clamp((c.Age - I) / 0.5f, 0f, 1f));
                fx.Sprite(FxShape.Hex, c.To + new Vector3(MathF.Cos(a), MathF.Sin(a), 0f) * c.ToSize * (0.3f + 0.9f * fly) + new Vector3(0f, 0f, 0.4f), c.ToSize * 0.3f, Fx.Fade(Rgb(190, 220, 255), 1f - fly), FxBlend.Alpha, a);
            }
            Jolt(fx, c.Age, I, 1.0f, 0.35f);
        }
        Impact(c, col, fx, 1.3f);
    }

    /// <summary>Make It Rain: a shower of gold coins falls over the whole side in a glittering storm, each ringing as it lands, and the user's Sp. Atk arrow sinks after.</summary>
    private static void MakeItRainY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Fx.Rain(fx, c.Age, c.Seed, c.ToFeet, h * 1.3f, 0f, I + 0.3f, 0.3f, 16, FxShape.Orb, Rgb(255, 214, 70), Rgb(255, 244, 160), 0.3f, FxBlend.Alpha);
        Fx.Rain(fx, c.Age, c.Seed + 2, c.ToFeet, h * 1.3f, 0.05f, I + 0.3f, 0.3f, 9, FxShape.Spark, White, Rgb(255, 224, 120), 0.18f);
        Fx.Rain(fx, c.Age, c.Seed + 5, c.ToFeet, h * 1.3f, 0.1f, I + 0.35f, 0.3f, 6, FxShape.Ring, Rgb(255, 214, 70), Rgb(220, 160, 40), 0.12f, FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            GroundRing(fx, c.Age, c.ToFeet, I, 0.4f, h * 1.3f, Rgb(255, 224, 120));
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 8, FxShape.Orb, Rgb(255, 214, 70), Rgb(255, 244, 160), 0.1f, 1.0f, 1.8f, FxBlend.Alpha, salt: 554);
            Jolt(fx, c.Age, I, 0.5f, 0.25f);
        }
        L2Down(fx, c.Age, c.Seed, c.FromFeet, c.FromSize, 0.6f, 2, Rgb(255, 214, 120));
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Psyblade: a blade of pink-white psychic light is drawn out of the air and slashes down twice across the foe, glittering sparks trailing off its edge.</summary>
    private static void PsybladeY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float draw = Fx.Envelope(c.Age, 0f, 0.1f, I - 0.14f, I - 0.06f);
        if (draw > 0f)
        {
            var hilt = c.From + new Vector3(c.FromSize * 0.5f, c.FromSize * 0.3f, 0.2f);
            fx.Ribbon(new[] { hilt, hilt + new Vector3(c.FromSize * 0.3f, c.FromSize * 0.9f * draw, 0f) }, c.FromSize * 0.1f, Fx.Fade(Rgb(255, 140, 210), draw), FxBlend.Alpha);
            fx.Ribbon(new[] { hilt, hilt + new Vector3(c.FromSize * 0.3f, c.FromSize * 0.9f * draw, 0f) }, c.FromSize * 0.04f, Fx.Fade(White, draw), FxBlend.Add);
        }
        Strike(c, fx, FxShape.Sword, new Vector3(1.0f, 1.0f, 0f), I, 0.9f, Rgb(255, 160, 220), 2.4f, 0.13f);
        if (c.Cue.Landed)
        {
            Fx.Slash(fx, c.Age, c.Seed, c.To, h, I - 0.05f, 2, 0.06f, Rgb(255, 140, 210), White);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 8, FxShape.Spark, Rgb(255, 190, 230), White, 0.09f, 1.0f, 0.5f, salt: 555);
            Jolt(fx, c.Age, I, 0.6f, 0.2f);
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Hydro Steam: a scalding jet of white steam and boiling water blasts from the user, shot through with bubbles, and the heat shimmers about the foe.</summary>
    private static void HydroSteamY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Fx.Stream(fx, c, 0f, 0.4f, 0.2f, 9, FxShape.Smoke, Rgb(248, 250, 255, 230), Rgb(210, 226, 245, 230), 0.3f, spread: 0.3f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0.02f, 0.38f, 0.2f, 8, FxShape.Drop, col.Main, col.Light, 0.11f, spread: 0.25f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0.04f, 0.4f, 0.22f, 4, FxShape.Bubble, White, col.Light, 0.12f, spread: 0.3f, blend: FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            float env = Fx.Envelope(c.Age, I, I + 0.1f, 0.85f, 1.05f);
            fx.Sprite(FxShape.Smoke, c.To, h * 1.5f, Fx.Fade(White, env * 0.65f), FxBlend.Alpha, c.Age);
            Rising(fx, c.Age, c.Seed, c.To - new Vector3(0f, h * 0.2f, 0f), h, I, 1.15f, 4, FxShape.Smoke, Rgb(240, 246, 255, 220), 0.2f, 0.1f);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 7, FxShape.Drop, col.Main, White, 0.09f, 1.0f, 1.5f, FxBlend.Alpha, salt: 556);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Ruination: a black sphere closes in on the foe from all round, red signs of ruin turning on its face, and a thin white line splits the foe in half before the sphere falls in on it.</summary>
    private static void RuinationY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, I + 0.1f, I + 0.45f);
        if (env > 0f)
        {
            float shrink = Math.Clamp(c.Age / I, 0f, 1f);
            float r = h * (2.0f - 1.3f * Fx.EaseOut(shrink));
            fx.Sprite(FxShape.Ring, c.To, r, Fx.Fade(Rgb(150, 40, 80), env), FxBlend.Alpha);
            fx.Sprite(FxShape.Orb, c.To, r * 0.8f, Fx.Fade(Rgb(26, 14, 36), env * 0.75f), FxBlend.Alpha);
            for (int i = 0; i < 4; i++)
            {
                float a = c.Age * 2f + i * MathF.PI / 2f;
                fx.Sprite(FxShape.Cross, c.To + new Vector3(MathF.Cos(a), MathF.Sin(a), 0f) * r * 0.65f + new Vector3(0f, 0f, 0.3f), h * 0.25f, Fx.Fade(Rgb(240, 60, 90), env), FxBlend.Alpha, a + 0.8f);
            }
        }
        if (c.Cue.Landed)
        {
            float line = Fx.Envelope(c.Age, I, I + 0.04f, I + 0.3f, I + 0.6f);
            fx.Ribbon(new[] { c.To + new Vector3(-h * 1.1f, 0f, 0.4f), c.To + new Vector3(h * 1.1f, 0f, 0.4f) }, h * 0.06f, Fx.Fade(White, line), FxBlend.Add);
            float split = Fx.EaseOut(Math.Clamp((c.Age - I) / 0.3f, 0f, 1f));
            fx.Sprite(FxShape.Glow, c.To + new Vector3(0f, h * 0.3f * split, 0.3f), h * 0.9f, Fx.Fade(Rgb(255, 90, 130), line * 0.6f), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, c.To - new Vector3(0f, h * 0.3f * split, -0.3f), h * 0.9f, Fx.Fade(Rgb(255, 90, 130), line * 0.6f), FxBlend.Alpha);
            Jolt(fx, c.Age, I, 0.9f, 0.3f);
        }
        Impact(c, col, fx, 1.2f);
    }

    /// <summary>Collision Course: a comet of red-orange fire comes in from one side and the user from the other, and the two crash on the foe, a cross of flame blowing out of the meeting.</summary>
    private static void CollisionCourseY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = Math.Clamp((c.Age - 0.04f) / (I - 0.04f), 0f, 1f);
        if (c.Age < I + 0.05f)
        {
            float e = Fx.EaseOut(k);
            var comet = Vector3.Lerp(c.To + new Vector3(h * 2.4f, h * 1.8f, 0f), c.To, e);
            fx.Streak(FxShape.Streak, comet + new Vector3(h * 0.5f, h * 0.4f, 0f), new Vector3(1f, 0.8f, 0f), h * 0.45f, 4f, Fx.Fade(Rgb(255, 120, 50), 0.9f));
            fx.Sprite(FxShape.Flame, comet, h * 0.9f, Fx.Fade(Rgb(255, 170, 60), 0.95f));
            fx.Sprite(FxShape.Rock, comet, h * 0.4f, Fx.Fade(Rgb(120, 70, 50), 1f), FxBlend.Alpha, k * 7f);
            var me = Vector3.Lerp(c.From, c.Aim - new Vector3(h * 0.3f, 0f, 0f), e);
            fx.Sprite(FxShape.Glow, me, c.FromSize * 1.1f, Fx.Fade(Rgb(255, 230, 190), 0.55f));
            SpeedLines(c, fx, 0.03f, I, 5, 0.5f);
        }
        if (c.Cue.Landed)
        {
            fx.Streak(FxShape.Flame, c.To, Vector3.UnitX, h * 1.2f * Fx.Envelope(c.Age, I, I + 0.05f, I + 0.2f, I + 0.4f), 2.4f, Fx.Fade(Rgb(255, 150, 60), 0.9f));
            fx.Streak(FxShape.Flame, c.To, Vector3.UnitY, h * 1.2f * Fx.Envelope(c.Age, I, I + 0.05f, I + 0.2f, I + 0.4f), 2.4f, Fx.Fade(Rgb(255, 214, 120), 0.9f));
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.45f, h * 2.2f, Fx.Fade(Rgb(255, 150, 70), 0.9f));
            fx.ScreenFlash(Rgb(255, 214, 170), 0.3f * Fx.Envelope(c.Age, I - 0.02f, I, I + 0.05f, I + 0.24f));
            Jolt(fx, c.Age, I, 1.0f, 0.4f);
        }
        Impact(c, col, fx, 1.5f);
    }

    /// <summary>Electro Drift: the user drifts round in a curve across the ground leaving a black skid lined with yellow sparks, then flashes in with a blue-white bolt from the side.</summary>
    private static void ElectroDriftY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float k = Math.Clamp((c.Age - 0.02f) / (I - 0.02f), 0f, 1f);
        if (c.Age < I + 0.3f)
        {
            float env = Fx.Envelope(c.Age, 0f, 0.05f, I + 0.1f, I + 0.3f);
            var pts = new Vector3[13];
            for (int i = 0; i <= 12; i++)
            {
                float u = i / 12f * k;
                var line = Vector3.Lerp(c.FromFeet, c.ToFeet, u);
                pts[i] = line + new Vector3(0f, 0.05f, MathF.Sin(u * MathF.PI * 1.2f) * h * 0.9f);
            }
            fx.Ribbon(pts, h * 0.3f, Fx.Fade(Rgb(40, 40, 52), env * 0.8f), FxBlend.Alpha);
            fx.Ribbon(pts, h * 0.07f, Fx.Fade(Rgb(255, 238, 90), env), FxBlend.Add);
            fx.Sprite(FxShape.Glow, pts[12] + new Vector3(0f, h * 0.3f, 0f), h * 1.0f, Fx.Fade(Rgb(255, 238, 120), env * 0.5f));
            for (int i = 0; i < 6; i++)
                fx.Sprite(FxShape.Spark, pts[Math.Min(12, i * 2)] + new Vector3(0f, h * (0.1f + 0.1f * i % 3), 0.1f), h * 0.14f, Fx.Fade(Rgb(255, 250, 190), env), FxBlend.Add, i);
        }
        if (c.Cue.Landed)
        {
            Fx.Bolt(fx, c.Age, c.Seed, c.To + new Vector3(h * 1.4f, h * 0.5f, 0f), c.To, I - 0.02f, I + 0.16f, c.ToSize * 0.05f, col.Main, Rgb(190, 230, 255), 1);
            Jolt(fx, c.Age, I, 0.7f, 0.25f);
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Shed Tail: the user's tail comes away in a flick of brown and is set down beside it as a small stuffed double with button eyes, the user dropping red drops of its cost.</summary>
    private static void ShedTailY13b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.08f, 0.85f, 1.1f);
        if (env <= 0f) return;
        float k = Fx.EaseOut(Math.Clamp(c.Age / 0.3f, 0f, 1f));
        var root = c.From + new Vector3(h * 0.3f, -h * 0.1f, 0.2f);
        var tip = root + new Vector3(h * (0.5f + 0.3f * k), h * 0.6f * MathF.Sin(k * MathF.PI), 0.1f);
        if (k < 1f) fx.Ribbon(new[] { root, Vector3.Lerp(root, tip, 0.5f) + new Vector3(0f, h * 0.2f, 0f), tip }, h * 0.18f, Fx.Fade(Rgb(190, 140, 90), env), FxBlend.Alpha);
        float doll = Fx.EaseOut(Math.Clamp((c.Age - 0.25f) / 0.2f, 0f, 1f));
        if (doll > 0f)
        {
            var at = c.FromFeet + new Vector3(h * 1.1f, h * 0.4f * doll, 0.3f);
            fx.Sprite(FxShape.Orb, at, h * 0.6f * doll, Fx.Fade(Rgb(214, 170, 120), env), FxBlend.Alpha);
            fx.Sprite(FxShape.Dot, at + new Vector3(-h * 0.08f, h * 0.06f, 0.05f), h * 0.07f, Fx.Fade(Rgb(40, 30, 30), env), FxBlend.Alpha);
            fx.Sprite(FxShape.Dot, at + new Vector3(h * 0.08f, h * 0.06f, 0.05f), h * 0.07f, Fx.Fade(Rgb(40, 30, 30), env), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, at, h * 0.9f * doll, Fx.Fade(Rgb(255, 230, 190), env * 0.4f), FxBlend.Alpha);
        }
        Fx.Burst(fx, c.Seed, c.Age, root, h, 0.12f, 0.45f, 3, FxShape.Drop, Rgb(230, 50, 60), Rgb(255, 140, 140), 0.08f, 0.6f, 1.5f, FxBlend.Alpha, salt: 557);
        Fx.Glint(fx, c.Age, c.FromFeet + new Vector3(h * 0.9f, h * 0.45f, 0.4f), h, 0.4f, White, 0.45f);
    }

    /// <summary>Chilly Reception: a speech bubble with a snowflake in it pops up over the user, a hush of cold white rolls across the field and snow starts to fall.</summary>
    private static void ChillyReceptionY13b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = Math.Max(c.FromSize, c.FoeSize);
        var mid = L1Field(c);
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.85f, 1.1f);
        if (env <= 0f) return;
        float pop = Fx.EaseOut(Math.Clamp(c.Age / 0.15f, 0f, 1f));
        var bubble = c.FromFeet + new Vector3(c.FromSize * 0.2f, c.FromSize * 1.3f, 0.3f);
        fx.Sprite(FxShape.Bubble, bubble, c.FromSize * 0.55f * pop, Fx.Fade(White, env), FxBlend.Alpha);
        fx.Sprite(FxShape.Snowflake, bubble, c.FromSize * 0.3f * pop, Fx.Fade(Rgb(120, 190, 250), env), FxBlend.Alpha, c.Age * 2f);
        float roll = Fx.EaseOut(Math.Clamp((c.Age - 0.12f) / 0.4f, 0f, 1f));
        fx.Sprite(FxShape.Smoke, mid, h * (1f + 4f * roll), Fx.Fade(Rgb(226, 244, 255, 200), env * 0.4f), FxBlend.Alpha, c.Age);
        GroundRing(fx, c.Age, mid, 0.12f, 0.6f, h * 3.5f, Rgb(190, 230, 255));
        Fx.Rain(fx, c.Age, c.Seed, mid, h * 2.2f, 0.2f, 1.0f, 0.55f, 14, FxShape.Snowflake, White, Rgb(176, 226, 255), 0.11f, FxBlend.Alpha);
    }

    /// <summary>Tidy Up: a broom of bright lines sweeps across the ground, dust puffing out ahead of it and the user's rubbish and hazards cleared, glints on the clean ground and Attack and Speed arrows climbing.</summary>
    private static void TidyUpY13b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.85f, 1.1f);
        if (env <= 0f) return;
        var mid = L1Field(c);
        for (int s = 0; s < 2; s++)
        {
            float k = Fx.EaseOut(Math.Clamp((c.Age - 0.04f - s * 0.25f) / 0.3f, 0f, 1f));
            float dir = s == 0 ? 1f : -1f;
            var sweepAt = mid + new Vector3(dir * (k - 0.5f) * h * 3.4f, 0.06f, 0.2f);
            for (int i = 0; i < 5; i++)
                fx.Ribbon(new[] { sweepAt + new Vector3(-dir * h * 0.3f * i, h * 0.15f + (i - 2) * h * 0.06f, (i - 2) * 0.12f), sweepAt + new Vector3(dir * h * 0.4f, h * 0.15f + (i - 2) * h * 0.04f, (i - 2) * 0.06f) }, h * 0.1f, Fx.Fade(Rgb(255, 232, 170), env * (1f - k * 0.5f)), FxBlend.Alpha);
            if (k < 1f) fx.Sprite(FxShape.Smoke, sweepAt + new Vector3(dir * h * 0.4f, h * 0.15f, 0f), h * 0.5f, Fx.Fade(Rgb(220, 206, 180, 200), env * 0.7f), FxBlend.Alpha, c.Age * 3f);
        }
        Fx.Glint(fx, c.Age, mid + new Vector3(-h * 0.8f, h * 0.15f, 0.3f), h, 0.45f, White, 0.5f);
        Fx.Glint(fx, c.Age, mid + new Vector3(h * 0.9f, h * 0.15f, 0.3f), h, 0.6f, Rgb(255, 250, 200), 0.45f);
        L3Up(fx, c.Age, c.FromFeet, h, 0.4f, 2, Rgb(255, 214, 120));
    }

    /// <summary>Snowscape: a soft snowfall begins and thickens over the whole field, the ground paling under it and a cold blue light settling.</summary>
    private static void SnowscapeY13b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = Math.Max(c.FromSize, c.FoeSize);
        var mid = L1Field(c);
        float env = Fx.Envelope(c.Age, 0f, 0.2f, 0.85f, 1.15f);
        if (env <= 0f) return;
        Fx.Rain(fx, c.Age, c.Seed, mid, h * 1.5f, 0f, 1.2f, 0.5f, 24, FxShape.Snowflake, White, Rgb(190, 226, 250), 0.2f, FxBlend.Alpha);
        Fx.Rain(fx, c.Age, c.Seed + 3, mid, h * 2.4f, 0.05f, 1.2f, 0.7f, 14, FxShape.Dot, White, Rgb(220, 240, 255), 0.06f, FxBlend.Alpha);
        float lie = Math.Clamp((c.Age - 0.1f) / 0.7f, 0f, 1f);
        fx.Flat(FxShape.Glow, mid + Vector3.UnitY * 0.05f, Vector3.UnitY, h * 5f, Fx.Fade(Rgb(230, 244, 255), env * 0.5f * lie), FxBlend.Alpha);
        fx.Sprite(FxShape.Glow, mid + Vector3.UnitY * h, h * 5f, Fx.Fade(Rgb(176, 210, 250), env * 0.2f), FxBlend.Alpha);
    }

    /// <summary>Pounce: the user crouches, springs high in a green arc and drops on the foe with claws out, a thread of web left hanging in its path and the foe's Speed arrow sinking.</summary>
    private static void PounceY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = Math.Clamp((c.Age - 0.04f) / (I - 0.04f), 0f, 1f);
        if (c.Age < I + 0.2f)
        {
            float env = Fx.Envelope(c.Age, 0f, 0.06f, I + 0.05f, I + 0.2f);
            var pts = new Vector3[10];
            for (int i = 0; i <= 9; i++) pts[i] = Lob(c.From, c.Aim, i / 9f * k, 0.1f);
            fx.Ribbon(pts, h * 0.07f, Fx.Fade(Rgb(168, 204, 40), env), FxBlend.Alpha);
            fx.Ribbon(pts, h * 0.025f, Fx.Fade(White, env * 0.8f), FxBlend.Add);
            fx.Sprite(FxShape.Claw, pts[9], h * 0.5f, Fx.Fade(Rgb(214, 240, 110), env), FxBlend.Alpha, -1f);
        }
        if (c.Cue.Landed)
        {
            GroundRing(fx, c.Age, c.ToFeet, I, 0.4f, h * 1.2f, Rgb(190, 230, 70));
            Fx.Slash(fx, c.Age, c.Seed, c.To, h, I - 0.04f, 3, 0.04f, Rgb(190, 230, 70), White);
            L2Down(fx, c.Age, c.Seed, c.ToFeet, h, I + 0.1f, 1, Rgb(200, 240, 100));
            Jolt(fx, c.Age, I, 0.6f, 0.25f);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Trailblaze: a bright machete-cut parts the long grass in a diagonal slash and the user dashes down the path, leaves swirling behind and a Speed arrow rising.</summary>
    private static void TrailblazeY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float k = Math.Clamp((c.Age - 0.02f) / (I - 0.02f), 0f, 1f);
        float cut = Fx.Envelope(c.Age, 0f, 0.05f, 0.14f, 0.28f);
        if (cut > 0f)
            fx.Streak(FxShape.Streak, c.FromFeet + new Vector3(h * 0.9f, h * 0.55f, 0.3f), new Vector3(1f, -0.8f, 0f), h * 0.3f, 6f, Fx.Fade(White, cut), FxBlend.Add);
        if (c.Age < I + 0.3f)
        {
            float env = Fx.Envelope(c.Age, 0.05f, 0.12f, I + 0.1f, I + 0.3f);
            for (int i = 0; i < 12; i++)
            {
                float u = Math.Clamp(k - i * 0.05f, 0f, 1f);
                var line = Vector3.Lerp(c.FromFeet, c.ToFeet, u);
                float side = (i % 2 == 0 ? 1f : -1f) * h * (0.15f + 0.4f * (i / 12f));
                fx.Sprite(FxShape.Leaf, line + new Vector3(0f, h * (0.15f + 0.15f * (i % 3)), side * 0.5f), h * 0.14f, Fx.Fade(i % 2 == 0 ? col.Main : col.Light, env), FxBlend.Alpha, i * 1.1f + c.Age * 6f);
            }
            fx.Ribbon(new[] { c.FromFeet + Vector3.UnitY * 0.05f, Vector3.Lerp(c.FromFeet, c.ToFeet, k) + Vector3.UnitY * 0.05f }, h * 0.3f, Fx.Fade(Rgb(120, 200, 100), env * 0.5f), FxBlend.Alpha);
        }
        L3Up(fx, c.Age, c.FromFeet, h, 0.5f, 1, Rgb(190, 250, 140));
        if (c.Cue.Landed) Fx.Contact(fx, c.Age, c.Seed, c.To, c.ToSize, I, 0.2f, col.Main, col.Light);
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Chilling Water: a splash of cold, pale-blue water with ice in it is thrown over the foe, snow and frost on the spray and an Attack arrow sinking in the cold.</summary>
    private static void ChillingWaterY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Fx.Stream(fx, c, 0f, 0.35f, 0.22f, 9, FxShape.Drop, Rgb(150, 210, 255), White, 0.12f, arc: 0.1f, spread: 0.3f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0.03f, 0.35f, 0.22f, 5, FxShape.Snowflake, White, Rgb(190, 232, 255), 0.11f, arc: 0.1f, spread: 0.3f, blend: FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 12, FxShape.Drop, Rgb(150, 210, 255), White, 0.1f, 1.1f, 2.0f, FxBlend.Alpha, salt: 558);
            fx.Sprite(FxShape.Glow, c.To, h * 1.3f * Fx.Envelope(c.Age, I, I + 0.08f, I + 0.35f, I + 0.6f), Fx.Fade(Rgb(190, 232, 255), 0.5f), FxBlend.Alpha);
            for (int i = 0; i < 4; i++)
                fx.Sprite(FxShape.Shard, c.To + new Vector3((i - 1.5f) * h * 0.3f, h * (0.3f - 0.1f * i % 2), 0.3f), h * 0.18f, Fx.Fade(Rgb(210, 240, 255), Fx.Envelope(c.Age, I, I + 0.08f, 0.9f, 1.1f)), FxBlend.Alpha, i);
            L2Down(fx, c.Age, c.Seed, c.ToFeet, h, I + 0.1f, 1, Rgb(150, 210, 255));
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Hyper Drill: a great white drill with a spiral groove spins up on the user and bores into the foe through its guard, the shield before it cracking and spiralling away.</summary>
    private static void HyperDrillY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float k = Math.Clamp((c.Age - 0.03f) / (I - 0.03f), 0f, 1f);
        if (c.Age < I + 0.15f)
        {
            float fade = c.Age < I ? 1f : Math.Max(0f, 1f - (c.Age - I) / 0.15f);
            var dir = Vector3.Normalize(c.Aim - c.From);
            var tip = Vector3.Lerp(c.From + dir * h * 0.6f, c.Aim, Fx.EaseOut(k));
            var back = tip - dir * h * 1.5f;
            fx.Ribbon(new[] { back, tip }, h * 0.7f, Fx.Fade(Rgb(226, 222, 214), fade), FxBlend.Alpha);
            for (int i = 0; i < 7; i++)
            {
                float u = (i + 0.5f) / 7f;
                float ph = u * 14f - c.Age * 40f;
                fx.Sprite(FxShape.Streak, Vector3.Lerp(back, tip, u) + new Vector3(0f, MathF.Sin(ph) * h * 0.18f, 0.1f), h * 0.18f, Fx.Fade(Rgb(150, 150, 164), fade), FxBlend.Alpha, 0.9f);
            }
            fx.Sprite(FxShape.Horn, tip + dir * h * 0.2f, h * 0.9f, Fx.Fade(Rgb(250, 248, 240), fade), FxBlend.Alpha, ScreenAngle(dir) - MathF.PI / 2f);
            Fx.Stream(fx, c, 0.05f, I, 0.15f, 5, FxShape.Spark, White, Rgb(255, 232, 160), 0.08f, spread: 0.2f);
        }
        if (c.Cue.Landed)
        {
            for (int i = 0; i < 5; i++)
            {
                float a = i * MathF.Tau / 5f;
                float fly = Fx.EaseOut(Math.Clamp((c.Age - I) / 0.45f, 0f, 1f));
                fx.Sprite(FxShape.Hex, c.To + new Vector3(MathF.Cos(a), MathF.Sin(a), 0f) * c.ToSize * (0.3f + 0.9f * fly) + new Vector3(0f, 0f, 0.4f), c.ToSize * 0.26f, Fx.Fade(Rgb(190, 214, 255), 1f - fly), FxBlend.Alpha, a);
            }
            Jolt(fx, c.Age, I, 0.8f, 0.3f);
        }
        Impact(c, col, fx, 1.2f);
    }

    /// <summary>Twin Beam: two thin pink beams twist round one another as they cross to the foe, each striking in its turn with a small bloom.</summary>
    private static void TwinBeamY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0.02f, 0.1f, I + 0.05f, I + 0.3f);
        if (env > 0f)
            for (int b = 0; b < 2; b++)
            {
                var pts = new Vector3[17];
                for (int i = 0; i <= 16; i++)
                {
                    float u = i / 16f;
                    float ph = u * 12f - c.Age * 20f + b * MathF.PI;
                    pts[i] = Vector3.Lerp(c.From, c.Aim, u) + new Vector3(0f, MathF.Sin(ph) * h * 0.2f * MathF.Sin(u * MathF.PI), MathF.Cos(ph) * 0.2f);
                }
                fx.Ribbon(pts, h * 0.1f, Fx.Fade(b == 0 ? Rgb(255, 110, 190) : Rgb(200, 130, 255), env), FxBlend.Alpha);
                fx.Ribbon(pts, h * 0.04f, Fx.Fade(White, env), FxBlend.Add);
            }
        if (c.Cue.Landed)
            for (int b = 0; b < 2; b++)
            {
                Fx.Contact(fx, c.Age, c.Seed + b, c.To + new Vector3(0f, (b - 0.5f) * c.ToSize * 0.3f, 0f), c.ToSize, I - 0.04f + b * 0.06f, 0.2f, b == 0 ? Rgb(255, 110, 190) : Rgb(200, 130, 255), White);
                Fx.Burst(fx, c.Seed + b, c.Age, c.To, c.ToSize, I, 0.5f, 4, FxShape.Spark, Rgb(255, 190, 230), White, 0.08f, 0.9f, salt: 559 + b);
            }
        Impact(c, col, fx, 0.8f);
    }

    /// <summary>Rage Fist: a great fist of violet ghost-fire rises from the ground under the foe, a ring of small fists circling it for each blow the user has taken, and drives up into it.</summary>
    private static void RageFistY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0f, 0.08f, I + 0.1f, I + 0.4f);
        if (env > 0f)
        {
            fx.Sprite(FxShape.Flame, c.FromFeet + new Vector3(0f, c.FromSize * 0.4f, 0f), c.FromSize * 1.0f, Fx.Fade(Rgb(150, 100, 255), env * 0.8f));
            L5Ring(fx, c.From, c.FromSize * 0.6f, c.FromSize * 0.5f, 4, FxShape.Fist, Fx.Fade(Rgb(190, 150, 255), env), c.FromSize * 0.2f, c.Age * 6f);
            fx.Sprite(FxShape.Exclaim, c.From + new Vector3(c.FromSize * 0.4f, c.FromSize * 0.7f, 0.3f), c.FromSize * 0.3f, Fx.Fade(Rgb(255, 80, 90), env), FxBlend.Alpha);
        }
        float k = (c.Age - (I - 0.2f)) / 0.2f;
        if (k > 0f && c.Age < I + 0.3f)
        {
            float fade = c.Age < I ? 1f : Math.Max(0f, 1f - (c.Age - I) / 0.3f);
            var p = c.ToFeet + new Vector3(0f, h * (0.4f + 0.3f * Fx.EaseOut(Math.Min(1f, k))), 0.3f) - Vector3.UnitY * h * 1.6f * (1f - Fx.EaseOut(Math.Min(1f, k)));
            fx.Sprite(FxShape.Fist, p, h * 1.2f, Fx.Fade(Rgb(110, 70, 190), fade), FxBlend.Alpha);
            fx.Sprite(FxShape.Flame, p, h * 1.4f, Fx.Fade(Rgb(170, 120, 255), fade * 0.7f));
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 9, FxShape.Wisp, Rgb(170, 120, 255), White, 0.15f, 1.0f, -0.4f, salt: 561);
            Jolt(fx, c.Age, I, 1.0f, 0.35f);
        }
        Impact(c, col, fx, 1.3f);
    }

    /// <summary>Armor Cannon: the user's red armour plates are blown off its body one by one and a great fireball is fired from the gap, the plates left lying about its feet.</summary>
    private static void ArmorCannonY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        for (int i = 0; i < 5; i++)
        {
            float k = (c.Age - 0.02f - i * 0.03f) / 0.4f;
            if (k < 0f || k > 1f) continue;
            float a = i * MathF.Tau / 5f + 0.5f;
            var p = c.From + new Vector3(MathF.Cos(a), MathF.Sin(a), 0f) * h * (0.4f + 0.8f * Fx.EaseOut(k)) + new Vector3(0f, -h * 0.3f * k * k, 0.2f);
            fx.Sprite(FxShape.Hex, p, h * 0.3f, Fx.Fade(Rgb(210, 70, 60), 1f - 0.5f * k), FxBlend.Alpha, a + k * 4f);
            fx.Sprite(FxShape.Hex, p, h * 0.32f, Fx.Fade(Rgb(255, 190, 150), 0.4f * (1f - k)));
        }
        Fx.Orb(fx, c, 0.12f, I, h * 0.5f, Rgb(255, 120, 40), Rgb(255, 230, 150));
        Fx.Stream(fx, c, 0.1f, I, 0.18f, 8, FxShape.Flame, Rgb(255, 150, 50), Rgb(255, 230, 130), 0.18f, spread: 0.3f);
        if (c.Cue.Landed)
        {
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.45f, c.ToSize * 2.0f, Fx.Fade(Rgb(255, 130, 50), 0.9f));
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.7f, 10, FxShape.Flame, Rgb(255, 150, 50), Rgb(255, 230, 130), 0.2f, 1.0f, -0.6f, salt: 562);
            Jolt(fx, c.Age, I, 1.0f, 0.35f);
        }
        L2Down(fx, c.Age, c.Seed, c.FromFeet, h, 0.55f, 2, Rgb(255, 150, 110));
        Impact(c, col, fx, 1.4f);
    }

    /// <summary>Bitter Blade: a sword of dark-red flame cuts the foe in two strokes, and a thread of embers is drawn back along the blade into the user in green.</summary>
    private static void BitterBladeY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Strike(c, fx, FxShape.Sword, new Vector3(1.0f, 1.1f, 0f), I, 0.95f, Rgb(210, 60, 50), 2.3f, 0.14f);
        float k = (c.Age - (I - 0.14f)) / 0.14f;
        if (k > 0f && c.Age < I + 0.2f)
            fx.Sprite(FxShape.Flame, c.To + new Vector3(h * (1.0f - Fx.EaseOut(Math.Min(1f, k))) * 0.8f, h * (1.1f - Fx.EaseOut(Math.Min(1f, k))) * 0.8f, 0.3f), h * 0.8f, Fx.Fade(Rgb(255, 140, 50), Math.Max(0f, 1f - (c.Age - I) / 0.2f)));
        if (c.Cue.Landed)
        {
            Fx.Slash(fx, c.Age, c.Seed, c.To, h, I - 0.04f, 2, 0.05f, Rgb(255, 120, 50), White);
            Fx.Stream(fx, c, I, I + 0.4f, 0.22f, 6, FxShape.Ember, Rgb(255, 214, 120), Rgb(150, 240, 130), 0.08f, spread: 0.3f, reverse: true);
            L2Heal(fx, c.Age, c.Seed, c.FromFeet, c.FromSize, I + 0.2f);
            Jolt(fx, c.Age, I, 0.7f, 0.25f);
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Double Shock: the user lights up in every volt it has, two crossed bolts bursting from its body and striking the foe together, the user's glow draining to nothing after.</summary>
    private static void DoubleShockY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float glow = Fx.Envelope(c.Age, 0f, 0.1f, I, I + 0.05f);
        if (glow > 0f)
        {
            fx.Sprite(FxShape.Glow, c.From, h * 1.6f, Fx.Fade(col.Main, glow * 0.8f));
            for (int i = 0; i < 4; i++)
                Fx.Bolt(fx, c.Age, c.Seed + (int)(c.Age * 25f) + i, c.From, c.From + new Vector3(MathF.Cos(i * 1.57f + 0.7f), MathF.Sin(i * 1.57f + 0.7f), 0f) * h * 0.9f, 0f, 2f, h * 0.04f, col.Main, White, 0);
        }
        for (int b = 0; b < 2; b++)
            Fx.Bolt(fx, c.Age, c.Seed + 10 * b, c.From + new Vector3(0f, b * h * 0.3f, 0f), c.Aim + new Vector3(0f, (1 - b) * c.ToSize * 0.3f - c.ToSize * 0.15f, 0f), I - 0.12f, I + 0.12f, c.ToSize * 0.07f, col.Main, White, 2);
        float drain = Fx.Envelope(c.Age, I + 0.05f, I + 0.1f, I + 0.3f, I + 0.6f);
        if (drain > 0f) fx.Sprite(FxShape.Glow, c.From, h * 1.3f, Fx.Fade(Rgb(120, 140, 200), drain * 0.4f), FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.6f, 10, FxShape.Spark, White, col.Main, 0.1f, 1.2f, salt: 563);
            fx.ScreenFlash(Rgb(255, 250, 200), 0.3f * Fx.Envelope(c.Age, I - 0.02f, I, I + 0.04f, I + 0.22f));
            Jolt(fx, c.Age, I, 1.0f, 0.35f);
        }
        Impact(c, col, fx, 1.4f);
    }

    /// <summary>Gigaton Hammer: a hammer with a pink, gold-rimmed head far bigger than the foe is swung up and brought down with a vast shock, the ground heaving in three rings.</summary>
    private static void GigatonHammerY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = (c.Age - (I - 0.3f)) / 0.3f;
        if (k > 0f && c.Age < I + 0.4f)
        {
            float fade = c.Age < I ? 1f : Math.Max(0f, 1f - (c.Age - I) / 0.4f);
            float swing = Fx.EaseOut(Math.Min(1f, k));
            float ang = 1.0f + 1.6f * swing;
            var grip = c.To + new Vector3(h * 1.2f, -h * 0.6f, 0.3f);
            var head = grip + new Vector3(MathF.Cos(ang), MathF.Sin(ang), 0f) * h * 2.0f;
            fx.Ribbon(new[] { grip, head }, h * 0.1f, Fx.Fade(Rgb(150, 156, 176), fade), FxBlend.Alpha);
            fx.Sprite(FxShape.Hex, head, h * 1.2f, Fx.Fade(Rgb(190, 198, 218), fade), FxBlend.Alpha, ang);
            fx.Sprite(FxShape.Hex, head, h * 0.9f, Fx.Fade(Rgb(255, 160, 205), fade), FxBlend.Alpha, ang);
            fx.Sprite(FxShape.Ring, head, h * 1.35f, Fx.Fade(Rgb(255, 214, 110), fade * 0.8f), FxBlend.Alpha);
            fx.Sprite(FxShape.Star, head, h * 0.5f, Fx.Fade(White, fade), FxBlend.Alpha, ang);
        }
        if (c.Cue.Landed)
        {
            for (int i = 0; i < 3; i++)
                GroundRing(fx, c.Age, c.ToFeet, I + i * 0.06f, 0.5f, h * (1.6f + 0.7f * i), i == 0 ? White : Rgb(255, 190, 220));
            fx.Flat(FxShape.Crack, c.ToFeet + Vector3.UnitY * 0.05f, Vector3.UnitY, h * 2.6f, Fx.Fade(Rgb(70, 60, 76), Fx.Envelope(c.Age, I, I + 0.04f, 0.95f, 1.15f)), FxBlend.Alpha);
            Fx.Burst(fx, c.Seed, c.Age, c.ToFeet, h, I, 0.7f, 9, FxShape.Rock, Rgb(160, 150, 150), Rgb(100, 94, 100), 0.12f, 1.0f, 2.0f, FxBlend.Alpha, salt: 564);
            fx.ScreenFlash(White, 0.2f * Fx.Envelope(c.Age, I - 0.01f, I, I + 0.04f, I + 0.2f));
            Jolt(fx, c.Age, I, 1.0f, 0.45f);
        }
        Impact(c, col, fx, 1.6f);
    }

    /// <summary>Comeuppance: a dark ring takes in the blow on the user and a black-red blast rebounds through it at the foe, a loop of arrows turning back on the way.</summary>
    private static void ComeuppanceY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float absorb = Fx.Envelope(c.Age, 0f, 0.08f, 0.16f, 0.24f);
        if (absorb > 0f)
        {
            fx.Sprite(FxShape.Ring, c.From, h * (1.3f - 0.6f * absorb), Fx.Fade(Rgb(120, 40, 70), absorb), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, c.From, h * 0.9f, Fx.Fade(Rgb(40, 20, 50), absorb * 0.7f), FxBlend.Alpha);
            L5Ring(fx, c.From, h * 0.5f, h * 0.5f, 3, FxShape.Arrow, Fx.Fade(Rgb(240, 70, 80), absorb), h * 0.2f, c.Age * 8f, FxBlend.Alpha, true);
        }
        float k = (c.Age - 0.22f) / (I - 0.22f);
        if (k > 0f && k < 1f)
        {
            var p = Vector3.Lerp(c.From, c.Aim, Fx.EaseOut(k));
            fx.Sprite(FxShape.Orb, p, h * 0.55f, Fx.Fade(Rgb(40, 18, 50), 1f), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, p, h * 1.1f, Fx.Fade(Rgb(240, 60, 80), 0.8f));
            fx.Sprite(FxShape.Arrow, p - new Vector3(h * 0.45f, 0f, 0f), h * 0.35f, Fx.Fade(Rgb(255, 130, 140), 0.9f), FxBlend.Alpha, -MathF.PI / 2f);
        }
        if (c.Cue.Landed)
        {
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.45f, c.ToSize * 2.0f, Fx.Fade(Rgb(240, 60, 80), 0.9f));
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.6f, 8, FxShape.Smoke, Rgb(60, 30, 70, 220), Rgb(120, 40, 70, 220), 0.2f, 0.8f, -0.1f, FxBlend.Alpha, salt: 565);
            Jolt(fx, c.Age, I, 1.0f, 0.35f);
        }
        Impact(c, col, fx, 1.3f);
    }

    /// <summary>Aqua Cutter: a slender crescent of pressed water is flung at the foe and slices through it, a thin clean line of light left where it passed.</summary>
    private static void AquaCutterY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = Math.Clamp((c.Age - 0.04f) / (I - 0.04f), 0f, 1f);
        if (c.Age < I + 0.02f)
        {
            var p = Vector3.Lerp(c.From, c.Aim, Fx.EaseOut(k));
            L5Arc(fx, p, c.FromSize * 0.8f, c.FromSize * 1.2f, 1.1f, -1.1f, c.FromSize * 0.2f, Fx.Fade(col.Main, 1f), 10);
            L5Arc(fx, p, c.FromSize * 0.8f, c.FromSize * 1.2f, 1.0f, -1.0f, c.FromSize * 0.07f, Fx.Fade(White, 1f), 10, FxBlend.Add);
            for (int i = 1; i <= 3; i++) fx.Sprite(FxShape.Drop, Vector3.Lerp(c.From, c.Aim, Math.Max(0f, Fx.EaseOut(k) - 0.08f * i)), c.FromSize * 0.1f, Fx.Fade(col.Light, 0.7f), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            float line = Fx.Envelope(c.Age, I, I + 0.03f, I + 0.18f, I + 0.38f);
            fx.Ribbon(new[] { c.To + new Vector3(-h * 0.2f, -h * 0.9f, 0.3f), c.To + new Vector3(h * 0.2f, h * 0.9f, 0.3f) }, h * 0.05f, Fx.Fade(White, line), FxBlend.Add);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 8, FxShape.Drop, col.Main, White, 0.09f, 1.1f, 1.8f, FxBlend.Alpha, salt: 566);
            Jolt(fx, c.Age, I, 0.4f, 0.2f);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Blazing Torque: a fiery wheel rolls in on the ground, a roaring plume of flame streaming behind it from the exhaust.</summary>
    private static void BlazingTorqueY13b(in FxCtx c, FxColors col, FxList fx)
    {
        L5Torque(c, fx, Rgb(236, 80, 40), Rgb(255, 190, 80), (f, at, a, fs) =>
        {
            for (int i = 0; i < 4; i++) f.Sprite(FxShape.Flame, at - new Vector3(fs * 0.25f * i, -fs * 0.05f * i, 0f), fs * (0.5f - 0.07f * i), Fx.Fade(i % 2 == 0 ? Rgb(255, 150, 50) : Rgb(255, 230, 130), a * (1f - 0.2f * i)));
        });
        if (c.Cue.Landed) { Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.6f, 8, FxShape.Flame, col.Main, col.Light, 0.16f, 0.9f, -0.7f, salt: 567); Jolt(fx, c.Age, I, 0.7f, 0.25f); }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Wicked Torque: a violet wheel rolls in with a black-and-purple exhaust of dark smoke, red lights lit in its spokes.</summary>
    private static void WickedTorqueY13b(in FxCtx c, FxColors col, FxList fx)
    {
        L5Torque(c, fx, Rgb(70, 40, 110), Rgb(236, 70, 110), (f, at, a, fs) =>
        {
            for (int i = 0; i < 4; i++) f.Sprite(FxShape.Smoke, at - new Vector3(fs * 0.3f * i, -fs * 0.05f * i, 0f), fs * (0.5f - 0.04f * i), Fx.Fade(i % 2 == 0 ? Rgb(40, 24, 60, 230) : Rgb(120, 60, 150, 230), a * (1f - 0.18f * i)), FxBlend.Alpha, i);
        });
        if (c.Cue.Landed) { Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.6f, 8, FxShape.Wisp, Rgb(150, 80, 200), Rgb(236, 70, 110), 0.14f, 0.9f, -0.3f, salt: 568); Jolt(fx, c.Age, I, 0.7f, 0.25f); }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Noxious Torque: a purple wheel rolls in trailing green-violet fumes and drips of poison from its tyres, and the foe is left wheezing in the cloud.</summary>
    private static void NoxiousTorqueY13b(in FxCtx c, FxColors col, FxList fx)
    {
        L5Torque(c, fx, Rgb(110, 50, 150), Rgb(170, 230, 90), (f, at, a, fs) =>
        {
            for (int i = 0; i < 4; i++) f.Sprite(FxShape.Smoke, at - new Vector3(fs * 0.3f * i, -fs * 0.05f * i, 0f), fs * (0.5f - 0.04f * i), Fx.Fade(i % 2 == 0 ? Rgb(150, 80, 190, 230) : Rgb(140, 200, 80, 230), a * (1f - 0.18f * i)), FxBlend.Alpha, i);
            f.Sprite(FxShape.Drop, at + new Vector3(0f, -fs * 0.3f, 0.1f), fs * 0.1f, Fx.Fade(Rgb(180, 240, 100), a), FxBlend.Alpha);
        });
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.8f, 8, FxShape.Smoke, Rgb(160, 90, 200, 210), Rgb(150, 210, 90, 210), 0.22f, 0.7f, -0.1f, FxBlend.Alpha, salt: 569);
            Fx.Rain(fx, c.Age, c.Seed, c.To, c.ToSize, I, 1.0f, 0.4f, 4, FxShape.Bubble, Rgb(190, 120, 230), White, 0.1f, FxBlend.Alpha);
            Jolt(fx, c.Age, I, 0.7f, 0.25f);
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Combat Torque: an orange wheel with rubber-black treads rolls in, its tyre-prints stamped flat as rings on the ground and a fist of force punched through the foe as it passes.</summary>
    private static void CombatTorqueY13b(in FxCtx c, FxColors col, FxList fx)
    {
        L5Torque(c, fx, Rgb(226, 100, 50), Rgb(255, 214, 140), (f, at, a, fs) =>
        {
            for (int i = 0; i < 3; i++) f.Sprite(FxShape.Ring, at - new Vector3(fs * 0.3f * i, 0f, 0f), fs * (0.45f + 0.15f * i), Fx.Fade(Rgb(255, 170, 90), a * (1f - 0.3f * i)));
        });
        Strike(c, fx, FxShape.Fist, new Vector3(-1.2f, 0.1f, 0f), I, 0.6f, Rgb(255, 190, 120), 0f, 0.1f);
        if (c.Cue.Landed) { GroundRing(fx, c.Age, c.ToFeet, I, 0.4f, c.ToSize * 1.5f, Rgb(255, 170, 90)); Jolt(fx, c.Age, I, 0.9f, 0.3f); }
        Impact(c, col, fx, 1.15f);
    }

    /// <summary>Magical Torque: a pink wheel set with little stars rolls in, a trail of glitter and sparkling hexagons falling behind and bursting into a flower of light on the foe.</summary>
    private static void MagicalTorqueY13b(in FxCtx c, FxColors col, FxList fx)
    {
        L5Torque(c, fx, Rgb(255, 130, 200), Rgb(255, 240, 160), (f, at, a, fs) =>
        {
            for (int i = 0; i < 4; i++) f.Sprite(i % 2 == 0 ? FxShape.Star : FxShape.Spark, at - new Vector3(fs * 0.3f * i, -fs * 0.1f * (i % 3), 0f), fs * 0.2f, Fx.Fade(i % 2 == 0 ? Rgb(255, 214, 240) : White, a * (1f - 0.2f * i)));
        });
        if (c.Cue.Landed)
        {
            L3Flower(fx, c.To + new Vector3(0f, 0f, 0.4f), c.ToSize * 0.4f * Fx.Envelope(c.Age, I, I + 0.08f, I + 0.3f, I + 0.55f), Rgb(255, 150, 214), Rgb(255, 244, 170), 0.95f, c.Age * 2f);
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.7f, 9, FxShape.Star, Rgb(255, 170, 224), White, 0.12f, 1.0f, 0.4f, salt: 570);
            Jolt(fx, c.Age, I, 0.7f, 0.25f);
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Blood Moon: a vast red moon swells up behind the foe, red drops running down its face, and a heavy beam of crimson moonlight comes down through it onto the foe.</summary>
    private static void BloodMoonY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0f, 0.2f, I + 0.2f, I + 0.5f);
        var moon = c.To + new Vector3(h * 0.9f, h * 1.2f, -0.3f);
        if (env > 0f)
        {
            float rise = Fx.EaseOut(Math.Clamp(c.Age / 0.3f, 0f, 1f));
            fx.Sprite(FxShape.Glow, moon, h * 3.4f * rise, Fx.Fade(Rgb(220, 40, 50), env * 0.35f), FxBlend.Alpha);
            fx.Sprite(FxShape.Orb, moon, h * 1.1f * rise, Fx.Fade(Rgb(196, 30, 46), env), FxBlend.Alpha);
            fx.Sprite(FxShape.Orb, moon + new Vector3(-h * 0.2f, h * 0.2f, 0.05f), h * 0.5f * rise, Fx.Fade(Rgb(236, 90, 90), env * 0.6f), FxBlend.Alpha);
            fx.Sprite(FxShape.Ring, moon, h * 1.9f * rise, Fx.Fade(Rgb(255, 120, 120), env * 0.7f));
            Fx.Rain(fx, c.Age, c.Seed, moon - new Vector3(0f, h * 0.4f, 0f), h * 1.4f, 0.1f, 1.0f, 0.5f, 6, FxShape.Drop, Rgb(210, 30, 40), Rgb(255, 110, 110), 0.1f, FxBlend.Alpha);
        }
        float beam = Fx.Envelope(c.Age, I - 0.15f, I - 0.08f, I + 0.1f, I + 0.3f);
        if (beam > 0f) fx.Ribbon(new[] { moon, c.To }, h * 0.5f, Fx.Fade(Rgb(255, 80, 90), beam * 0.8f), FxBlend.Add);
        if (c.Cue.Landed)
        {
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.5f, h * 2.0f, Fx.Fade(Rgb(255, 90, 100), 0.9f));
            fx.ScreenFlash(Rgb(255, 140, 150), 0.25f * Fx.Envelope(c.Age, I - 0.02f, I, I + 0.05f, I + 0.24f));
            Jolt(fx, c.Age, I, 1.0f, 0.4f);
        }
        Impact(c, col, fx, 1.5f);
    }

    /// <summary>Matcha Gotcha: a whisked green froth swirls up in a bowl of light and is slung at the foe in a steaming spray, a green warmth running back to the user.</summary>
    private static void MatchaGotchaY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var bowl = c.From + new Vector3(h * 0.5f, h * 0.1f, 0.3f);
        float env = Fx.Envelope(c.Age, 0f, 0.08f, 0.14f, 0.22f);
        if (env > 0f)
        {
            fx.Sprite(FxShape.Swirl, bowl, h * 0.7f, Fx.Fade(Rgb(130, 200, 90), env), FxBlend.Alpha, c.Age * 16f);
            fx.Sprite(FxShape.Bubble, bowl, h * 0.3f, Fx.Fade(Rgb(210, 250, 170), env), FxBlend.Alpha);
        }
        Fx.Stream(fx, c, 0.12f, 0.4f, 0.22f, 9, FxShape.Drop, Rgb(120, 190, 80), Rgb(210, 250, 170), 0.24f, arc: 0.04f, spread: 0.35f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0.14f, 0.4f, 0.22f, 4, FxShape.Smoke, Rgb(240, 252, 230, 200), White, 0.2f, spread: 0.3f, blend: FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            Rising(fx, c.Age, c.Seed, c.To, c.ToSize, I, 1.1f, 3, FxShape.Smoke, Rgb(240, 252, 230, 210), 0.18f, 0.1f);
            Fx.Stream(fx, c, I, I + 0.4f, 0.2f, 5, FxShape.Spark, Rgb(210, 255, 160), White, 0.09f, reverse: true);
            L2Heal(fx, c.Age, c.Seed, c.FromFeet, h, I + 0.2f);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Syrup Bomb: a round bomb of amber syrup is lobbed and bursts on the foe, thick strands stretching and dripping off it and a candy-sweet glitter in the air.</summary>
    private static void SyrupBombY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = (c.Age - 0.03f) / (I - 0.03f);
        var amber = Rgb(220, 140, 40);
        if (k >= 0f && k <= 1f)
        {
            var p = Lob(c.From, c.Aim, k, 0.05f);
            fx.Sprite(FxShape.Orb, p, c.FromSize * 0.7f, Fx.Fade(amber, 1f), FxBlend.Alpha, k * 4f);
            fx.Sprite(FxShape.Dot, p + new Vector3(c.FromSize * 0.08f, c.FromSize * 0.1f, 0.05f), c.FromSize * 0.1f, Fx.Fade(White, 1f), FxBlend.Alpha);
            fx.Ribbon(new[] { p + new Vector3(0f, c.FromSize * 0.22f, 0f), p + new Vector3(0f, c.FromSize * 0.5f, 0f) }, c.FromSize * 0.04f, Fx.Fade(Rgb(120, 190, 80), 1f), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            float env = Fx.Envelope(c.Age, I, I + 0.05f, 0.9f, 1.1f);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 10, FxShape.Drop, amber, Rgb(255, 214, 120), 0.11f, 1.0f, 1.5f, FxBlend.Alpha, salt: 571);
            for (int i = 0; i < 4; i++)
            {
                float sag = Fx.EaseOut(Math.Clamp((c.Age - I) / 0.5f, 0f, 1f));
                var top = c.To + new Vector3((i - 1.5f) * h * 0.3f, h * 0.35f, 0.3f);
                fx.Ribbon(new[] { top, top - new Vector3(0f, h * (0.2f + 0.5f * sag), 0f) }, h * 0.06f, Fx.Fade(amber, env), FxBlend.Alpha);
                fx.Sprite(FxShape.Drop, top - new Vector3(0f, h * (0.2f + 0.5f * sag), 0f), h * 0.12f, Fx.Fade(amber, env), FxBlend.Alpha);
            }
            Fx.Rain(fx, c.Age, c.Seed, c.To, h, I, 1.0f, 0.5f, 6, FxShape.Spark, Rgb(255, 230, 150), White, 0.07f);
            L2Down(fx, c.Age, c.Seed, c.ToFeet, h, I + 0.15f, 1, Rgb(240, 180, 70));
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Ivy Cudgel: a heavy wooden club wrapped in a climbing ivy swings in, leaves springing from the vine at every turn, and cracks across the foe in the colour of the mask worn.</summary>
    private static void IvyCudgelY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = (c.Age - (I - 0.22f)) / 0.22f;
        if (k > 0f && c.Age < I + 0.25f)
        {
            float fade = c.Age < I ? 1f : Math.Max(0f, 1f - (c.Age - I) / 0.25f);
            float ang = 0.9f - 1.7f * Fx.EaseOut(Math.Min(1f, k));
            var grip = c.To + new Vector3(-h * 1.3f, -h * 0.4f, 0.3f);
            var head = grip + new Vector3(MathF.Cos(ang), MathF.Sin(ang), 0f) * h * 1.7f;
            fx.Ribbon(new[] { grip, head }, h * 0.3f, Fx.Fade(Rgb(130, 90, 56), fade), FxBlend.Alpha);
            var vine = new Vector3[11];
            for (int i = 0; i <= 10; i++)
            {
                float u = i / 10f;
                vine[i] = Vector3.Lerp(grip, head, u) + new Vector3(0f, MathF.Sin(u * 20f) * h * 0.1f, 0.05f);
            }
            fx.Ribbon(vine, h * 0.05f, Fx.Fade(col.Main, fade), FxBlend.Alpha);
            for (int i = 1; i < 10; i += 2) fx.Sprite(FxShape.Leaf, vine[i], h * 0.16f, Fx.Fade(col.Light, fade), FxBlend.Alpha, i + ang * 3f);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 10, FxShape.Leaf, col.Main, col.Light, 0.13f, 1.1f, 1.0f, FxBlend.Alpha, spin: 4f, salt: 572);
            fx.Sprite(FxShape.Star, c.To, h * 0.9f * Fx.Envelope(c.Age, I, I + 0.04f, I + 0.12f, I + 0.28f), Fx.Fade(col.Light, 0.9f), FxBlend.Add, 0.4f);
            Jolt(fx, c.Age, I, 0.9f, 0.3f);
        }
        Impact(c, col, fx, 1.3f);
    }

    /// <summary>Electro Shot: bolts gather into a bright ball above the user, which sinks toward it as the user's Sp. Atk arrow rises, and then lashes out at the foe in a heavy blue-white beam.</summary>
    private static void ElectroShotY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var ball = c.From + new Vector3(h * 0.2f, h * 0.9f, 0.2f);
        float env = Fx.Envelope(c.Age, 0f, 0.16f, 0.22f, 0.3f);
        if (env > 0f)
        {
            for (int i = 0; i < 4; i++)
                Fx.Bolt(fx, c.Age, c.Seed + (int)(c.Age * 25f) + i, ball + new Vector3(MathF.Cos(i * 1.57f + 0.4f), MathF.Sin(i * 1.57f + 0.4f), 0f) * h * 1.2f * (1f - c.Age * 2f), ball, 0f, 2f, h * 0.035f, col.Main, White, 0);
            fx.Sprite(FxShape.Glow, ball, h * 1.0f * env, Fx.Fade(col.Main, 0.85f));
            fx.Sprite(FxShape.Orb, ball, h * 0.4f * env, Fx.Fade(White, 1f), FxBlend.Alpha);
        }
        Fx.Beam(fx, c, 0.2f, 0.62f, h * 0.3f, Fx.Fade(Rgb(110, 190, 255), 0.85f), White, 12);
        Fx.Stream(fx, c, 0.22f, 0.58f, 0.2f, 7, FxShape.Spark, Rgb(255, 240, 120), White, 0.1f, spread: 0.4f);
        L3Up(fx, c.Age, c.FromFeet, h, 0.1f, 1, Rgb(255, 238, 120));
        if (c.Cue.Landed)
        {
            fx.ScreenFlash(Rgb(230, 244, 255), 0.22f * Fx.Envelope(c.Age, I - 0.02f, I, I + 0.04f, I + 0.2f));
            Jolt(fx, c.Age, I, 1.0f, 0.35f);
        }
        Impact(c, col, fx, 1.4f);
    }

    /// <summary>Tera Starstorm: a storm of crystal stars of every colour pours down over the foes, each a bright prism, and the type's own colour burns at the middle of the fall.</summary>
    private static void TeraStarstormY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var hues = new[] { Rgb(255, 120, 150), Rgb(255, 214, 100), Rgb(120, 230, 150), Rgb(110, 190, 255), Rgb(190, 130, 255) };
        for (int i = 0; i < 5; i++)
            Fx.Rain(fx, c.Age, c.Seed + i * 3, c.ToFeet, h * 0.9f, i * 0.03f, I + 0.3f, 0.28f, 4, i % 2 == 0 ? FxShape.Star : FxShape.Hex, hues[i], White, 0.26f, FxBlend.Alpha);
        float env = Fx.Envelope(c.Age, 0f, 0.14f, I + 0.1f, I + 0.35f);
        if (env > 0f)
        {
            fx.Sprite(FxShape.Glow, c.To + new Vector3(0f, h * 0.2f, 0f), h * 1.8f, Fx.Fade(col.Main, env * 0.5f), FxBlend.Alpha);
            fx.Sprite(FxShape.Star, c.To + new Vector3(0f, h * 1.6f, 0f), h * 1.0f * env, Fx.Fade(col.Light, env), FxBlend.Alpha, c.Age * 2f);
        }
        if (c.Cue.Landed)
        {
            for (int i = 0; i < 5; i++) Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I + i * 0.03f, 0.4f, h * (0.9f + 0.25f * i), Fx.Fade(hues[i], 0.9f));
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 10, FxShape.Shard, col.Light, White, 0.13f, 1.1f, 0.8f, FxBlend.Alpha, salt: 573);
            fx.ScreenFlash(White, 0.25f * Fx.Envelope(c.Age, I - 0.02f, I, I + 0.05f, I + 0.24f));
            Jolt(fx, c.Age, I, 0.9f, 0.3f);
        }
        Impact(c, col, fx, 1.2f);
    }

    /// <summary>Fickle Beam: a beam flickers between two widths and two colours as it crosses to the foe, now thin and blue, now thick and indigo, and sometimes a second flare doubles it.</summary>
    private static void FickleBeamY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0.05f, 0.12f, I + 0.1f, I + 0.35f);
        if (env > 0f)
        {
            int frame = (int)(c.Age * 30f);
            bool thick = Fx.Rand(c.Seed, frame, 650) > 0.45f;
            var tint = thick ? Rgb(110, 90, 250) : Rgb(120, 214, 255);
            fx.Ribbon(new[] { c.From, c.Aim }, h * (thick ? 0.5f : 0.2f), Fx.Fade(tint, env), FxBlend.Alpha);
            fx.Ribbon(new[] { c.From, c.Aim }, h * 0.1f, Fx.Fade(White, env), FxBlend.Add);
            fx.Sprite(FxShape.Glow, c.From + new Vector3(h * 0.5f, 0f, 0.2f), h * 1.0f, Fx.Fade(tint, env * 0.6f));
            if (Fx.Rand(c.Seed, frame, 651) > 0.7f) fx.Sprite(FxShape.Star, c.Aim, h * 0.6f, Fx.Fade(White, env), FxBlend.Add, frame);
        }
        Fx.Stream(fx, c, 0.08f, I + 0.05f, 0.14f, 7, FxShape.Spark, Rgb(190, 180, 255), White, 0.09f, spread: 0.3f);
        if (c.Cue.Landed)
        {
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.4f, c.ToSize * 1.6f, Fx.Fade(Rgb(150, 130, 255), 0.9f));
            Jolt(fx, c.Age, I, 0.8f, 0.3f);
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Burning Bulwark: a dome of hexagonal plates glowing red-hot stands before the user, flames licking from the seams and a ring of fire kindled on the ground round it.</summary>
    private static void BurningBulwarkY13b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0.02f, 0.18f, 0.85f, 1.1f);
        if (env <= 0f) return;
        float grow = Fx.EaseOut(Math.Clamp((c.Age - 0.03f) / 0.2f, 0f, 1f));
        var mid = c.FromFeet + L1Toward(c) * h * 0.2f + new Vector3(h * 0.3f, 0f, 0.25f);
        for (int row = 0; row < 3; row++)
            for (int i = 0; i < 4 - row; i++)
            {
                var at = mid + new Vector3((i - (3 - row) * 0.5f) * h * 0.34f, h * (0.2f + 0.26f * row) * grow, 0f);
                float heat = 0.75f + 0.25f * MathF.Sin(c.Age * 11f + i * 1.7f + row);
                fx.Sprite(FxShape.Hex, at, h * 0.36f * grow, Fx.Fade(Rgb(200, 60, 40), env * 0.85f * heat), FxBlend.Alpha);
                fx.Sprite(FxShape.Hex, at, h * 0.4f * grow, Fx.Fade(Rgb(255, 170, 70), env * 0.4f * heat));
                if ((i + row) % 2 == 0) fx.Sprite(FxShape.Flame, at + new Vector3(0f, h * 0.28f, 0.05f), h * 0.3f, Fx.Fade(col.Main, env * 0.9f));
            }
        fx.Flat(FxShape.Ring, c.FromFeet + Vector3.UnitY * 0.05f, Vector3.UnitY, h * 1.5f, Fx.Fade(Rgb(255, 130, 50), env), FxBlend.Alpha);
        Fx.Burst(fx, c.Seed, c.Age, mid + new Vector3(0f, h * 0.6f, 0f), h, 0.3f, 0.6f, 5, FxShape.Ember, Rgb(255, 170, 70), White, 0.07f, 0.7f, -0.5f, salt: 574);
    }

    /// <summary>Thunderclap: nothing at first, then a single bolt strikes the foe from the sky without a cloud and a ring of thunder-sound runs flat across the ground.</summary>
    private static void ThunderclapY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float t0 = I - 0.08f;
        Fx.Bolt(fx, c.Age, c.Seed, c.To + new Vector3(h * 0.5f, h * 3.0f, -0.2f), c.To - Vector3.UnitY * h * 0.2f, t0, t0 + 0.25f, h * 0.12f, col.Main, White, 3);
        float flash = Fx.Envelope(c.Age, t0, t0 + 0.02f, t0 + 0.08f, t0 + 0.22f);
        if (flash > 0f) fx.Sprite(FxShape.Glow, c.To, h * 2.4f * flash, Fx.Fade(Rgb(255, 250, 200), 0.8f));
        if (c.Cue.Landed)
        {
            GroundRing(fx, c.Age, c.ToFeet, I, 0.5f, h * 2.0f, White);
            GroundRing(fx, c.Age, c.ToFeet, I + 0.07f, 0.5f, h * 2.4f, col.Main);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 10, FxShape.Spark, White, col.Main, 0.1f, 1.2f, salt: 575);
            fx.ScreenFlash(White, 0.3f * Fx.Envelope(c.Age, I - 0.02f, I, I + 0.04f, I + 0.2f));
            Jolt(fx, c.Age, I, 1.0f, 0.4f);
        }
        Impact(c, col, fx, 1.2f);
    }

    /// <summary>Mighty Cleave: a vast blade of pale crystal descends from above and cleaves straight through the foe, a pink-white glitter spraying off it and the cut standing as a bright line.</summary>
    private static void MightyCleaveY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        L5Slam(c, fx, FxShape.Sword, new Vector3(0f, 2.0f, 0f), 1.8f, Rgb(226, 190, 150), Rgb(255, 214, 190), 0f, I, 0.22f);
        if (c.Cue.Landed)
        {
            float line = Fx.Envelope(c.Age, I, I + 0.04f, I + 0.3f, I + 0.6f);
            fx.Ribbon(new[] { c.To + Vector3.UnitY * h * 1.4f, c.To - Vector3.UnitY * h * 0.7f }, h * 0.1f, Fx.Fade(White, line), FxBlend.Add);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 12, FxShape.Spark, Rgb(255, 214, 230), White, 0.09f, 1.1f, 0.5f, salt: 576);
            Fx.Burst(fx, c.Seed, c.Age, c.ToFeet, h, I, 0.6f, 5, FxShape.Shard, Rgb(246, 226, 200), Rgb(255, 190, 210), 0.14f, 0.9f, 1.5f, FxBlend.Alpha, salt: 577);
            Jolt(fx, c.Age, I, 1.0f, 0.35f);
        }
        Impact(c, col, fx, 1.3f);
    }

    /// <summary>Tachyon Cutter: two razor lines of silver-cyan cross at the foe from either side at far more than the eye can follow, an X of light left for a moment.</summary>
    private static void TachyonCutterY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        for (int s = 0; s < 2; s++)
        {
            float t = I - 0.07f + s * 0.07f;
            float k = (c.Age - (t - 0.06f)) / 0.06f;
            if (k < 0f || c.Age > t + 0.28f) continue;
            float dir = s == 0 ? 1f : -1f;
            var a = c.To + new Vector3(-dir * h * 1.6f, h * 0.9f, 0.3f);
            var b = c.To + new Vector3(dir * h * 1.6f, -h * 0.9f, 0.3f);
            var tip = Vector3.Lerp(a, b, Fx.EaseOut(Math.Min(1f, k)));
            float fade = c.Age < t ? 1f : Math.Max(0f, 1f - (c.Age - t) / 0.28f);
            fx.Ribbon(new[] { a, tip }, h * 0.07f, Fx.Fade(Rgb(150, 230, 255), fade), FxBlend.Alpha);
            fx.Ribbon(new[] { a, tip }, h * 0.025f, Fx.Fade(White, fade), FxBlend.Add);
            fx.Sprite(FxShape.Spark, tip, h * 0.3f, Fx.Fade(White, fade), FxBlend.Add);
            if (c.Cue.Landed && c.Age >= t) Fx.Contact(fx, c.Age, c.Seed + s, c.To, h, t, 0.16f, Rgb(150, 230, 255), White);
        }
        if (c.Cue.Landed) Jolt(fx, c.Age, I, 0.5f, 0.2f);
        Impact(c, col, fx, 0.85f);
    }

    /// <summary>Hard Press: two heavy steel bars close on the foe from above and below, pressure rings tightening round it as they meet and rivets of light flaring along their edges.</summary>
    private static void HardPressY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = Math.Clamp((c.Age - 0.05f) / (I - 0.05f), 0f, 1f);
        float env = Fx.Envelope(c.Age, 0.03f, 0.1f, I + 0.2f, I + 0.45f);
        if (env > 0f)
        {
            float gap = h * 1.2f * (1f - Fx.EaseOut(k)) + h * 0.38f;
            for (int s = -1; s <= 1; s += 2)
            {
                var at = c.To + new Vector3(0f, s * gap, 0.3f);
                fx.Ribbon(new[] { at - new Vector3(h * 0.9f, 0f, 0f), at + new Vector3(h * 0.9f, 0f, 0f) }, h * 0.55f, Fx.Fade(Rgb(120, 134, 164), env), FxBlend.Alpha);
                fx.Ribbon(new[] { at - new Vector3(h * 0.85f, -s * h * 0.13f, 0f), at + new Vector3(h * 0.85f, s * h * 0.13f, 0f) }, h * 0.06f, Fx.Fade(White, env * 0.7f), FxBlend.Add);
                for (int i = -2; i <= 2; i++) fx.Sprite(FxShape.Dot, at + new Vector3(i * h * 0.35f, 0f, 0.05f), h * 0.07f, Fx.Fade(Rgb(230, 236, 250), env), FxBlend.Alpha);
            }
            for (int i = 0; i < 2; i++)
                fx.Sprite(FxShape.Ring, c.To, h * (1.5f - 0.7f * k - 0.2f * i), Fx.Fade(Rgb(190, 204, 230), env * (0.8f - 0.3f * i)));
        }
        if (c.Cue.Landed)
        {
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.4f, h * 1.8f, Fx.Fade(White, 0.9f));
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 8, FxShape.Spark, White, Rgb(190, 204, 230), 0.09f, 1.2f, salt: 578);
            Jolt(fx, c.Age, I, 0.9f, 0.3f);
        }
        Impact(c, col, fx, 1.2f);
    }

    /// <summary>Dragon Cheer: the user roars and an indigo dragon-shape of light coils over the ally, stars and red target rings spinning in as its crits are raised.</summary>
    private static void DragonCheerY13b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.85f, 1.1f);
        if (env <= 0f) return;
        var pts = new Vector3[15];
        for (int i = 0; i <= 14; i++)
        {
            float u = i / 14f;
            float a = u * MathF.Tau * 1.1f + c.Age * 6f;
            pts[i] = c.ToFeet + new Vector3(MathF.Cos(a) * h * (0.7f - 0.3f * u), h * (0.1f + 1.2f * u), MathF.Sin(a) * 0.3f + 0.1f);
        }
        fx.Ribbon(pts, h * 0.2f, Fx.Fade(Rgb(110, 96, 250), env), FxBlend.Alpha);
        fx.Ribbon(pts, h * 0.07f, Fx.Fade(Rgb(214, 204, 255), env), FxBlend.Add);
        fx.Sprite(FxShape.Fang, pts[14] + new Vector3(0f, h * 0.1f, 0.1f), h * 0.4f, Fx.Fade(White, env), FxBlend.Alpha, 0.5f);
        Fx.Waves(fx, c, 0f, 0.5f, 3, FxShape.Ring, Rgb(190, 180, 255), 0.5f, 0.2f);
        fx.Sprite(FxShape.Target, c.To, h * 0.8f * (1.2f - 0.3f * Fx.Envelope(c.Age, 0.2f, 0.4f, 0.7f, 0.9f)), Fx.Fade(Rgb(255, 100, 110), env * 0.8f), FxBlend.Alpha);
        L5Ring(fx, c.To, h * 0.7f, h * 0.6f, 4, FxShape.Star, Fx.Fade(Rgb(255, 236, 140), env), h * 0.13f, c.Age * 5f);
    }

    /// <summary>Alluring Voice: a soft pink song leaves the user as rings and hearts and little notes, drifting to the foe, which reels with pink stars about its head.</summary>
    private static void AlluringVoiceY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        Fx.Waves(fx, c, 0f, 0.5f, 4, FxShape.Ring, Rgb(255, 170, 224), 0.5f, 0.3f);
        Fx.Stream(fx, c, 0.04f, 0.42f, 0.28f, 5, FxShape.Heart, Rgb(255, 130, 190), Rgb(255, 214, 236), 0.14f, arc: 0.08f, spread: 0.4f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0.06f, 0.42f, 0.28f, 4, FxShape.Note, Rgb(255, 236, 250), Rgb(255, 190, 230), 0.12f, arc: 0.08f, spread: 0.4f, blend: FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            float env = Fx.Envelope(c.Age, I, I + 0.08f, 0.85f, 1.05f);
            L5Ring(fx, c.ToFeet + new Vector3(0f, c.ToSize * 1.05f, 0f), c.ToSize * 0.4f, c.ToSize * 0.1f, 3, FxShape.Star, Fx.Fade(Rgb(255, 170, 224), env), c.ToSize * 0.13f, c.Age * 5f);
            PulseRing(fx, c.Age, c.To, I, 0.4f, c.ToSize * 0.3f, c.ToSize * 1.4f, Rgb(255, 170, 224), White);
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Temper Flare: a fist of orange flame swirls with sparks as a tantrum is thrown, an angry mark flaring and a ring of flame bursting out of the blow.</summary>
    private static void TemperFlareY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float pre = Fx.Envelope(c.Age, 0f, 0.08f, I - 0.12f, I - 0.04f);
        if (pre > 0f)
        {
            fx.Sprite(FxShape.Exclaim, c.FromFeet + new Vector3(c.FromSize * 0.4f, c.FromSize * 1.1f, 0.3f), c.FromSize * 0.3f, Fx.Fade(Rgb(255, 70, 50), pre), FxBlend.Alpha);
            L5Ring(fx, c.From, c.FromSize * 0.6f, c.FromSize * 0.5f, 5, FxShape.Flame, Fx.Fade(Rgb(255, 150, 50), pre), c.FromSize * 0.28f, c.Age * 9f, FxBlend.Add);
        }
        Strike(c, fx, FxShape.Fist, new Vector3(-1.4f, 0.4f, 0f), I, 0.75f, Rgb(255, 120, 50), 0f, 0.14f);
        if (c.Cue.Landed)
        {
            for (int i = 0; i < 8; i++)
            {
                float a = i * MathF.Tau / 8f;
                float out_ = Fx.EaseOut(Math.Clamp((c.Age - I) / 0.4f, 0f, 1f));
                fx.Sprite(FxShape.Flame, c.To + new Vector3(MathF.Cos(a), MathF.Sin(a), 0f) * h * (0.3f + 0.9f * out_), h * 0.35f, Fx.Fade(i % 2 == 0 ? col.Main : col.Light, 1f - out_), FxBlend.Add);
            }
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 8, FxShape.Ember, Rgb(255, 170, 70), White, 0.08f, 1.2f, -0.4f, salt: 579);
            Jolt(fx, c.Age, I, 0.9f, 0.3f);
        }
        Impact(c, col, fx, 1.15f);
    }

    /// <summary>Supercell Slam: a black cell of cloud gathers over the field, lightning strikes the user, and it slams down into the foe as a crashing bolt, a ring of thunder breaking out.</summary>
    private static void SupercellSlamY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var top = c.From + new Vector3(0f, c.FromSize * 2.2f, 0f);
        float env = Fx.Envelope(c.Age, 0f, 0.12f, 0.2f, 0.3f);
        if (env > 0f)
        {
            for (int i = 0; i < 3; i++) fx.Sprite(FxShape.Smoke, top + new Vector3((i - 1) * c.FromSize * 0.6f, 0f, 0.1f * i), c.FromSize * 1.0f, Fx.Fade(Rgb(60, 62, 84, 230), env), FxBlend.Alpha, i + c.Age);
            Fx.Bolt(fx, c.Age, c.Seed, top, c.From + Vector3.UnitY * c.FromSize * 0.4f, 0.08f, 0.26f, c.FromSize * 0.07f, col.Main, White, 2);
            fx.Sprite(FxShape.Glow, c.From, c.FromSize * 1.4f, Fx.Fade(col.Main, env * 0.6f));
        }
        float k = Math.Clamp((c.Age - 0.18f) / (I - 0.18f), 0f, 1f);
        if (c.Age > 0.18f && c.Age < I + 0.05f)
        {
            var p = Vector3.Lerp(top, c.To, Fx.EaseOut(k));
            fx.Ribbon(new[] { top, p }, c.FromSize * 0.25f, Fx.Fade(col.Main, 0.8f), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, p, c.FromSize * 1.3f, Fx.Fade(Rgb(255, 250, 190), 0.8f));
        }
        if (c.Cue.Landed)
        {
            GroundRing(fx, c.Age, c.ToFeet, I, 0.5f, h * 2.2f, White);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 10, FxShape.Spark, White, col.Main, 0.1f, 1.2f, salt: 580);
            fx.ScreenFlash(Rgb(255, 250, 200), 0.3f * Fx.Envelope(c.Age, I - 0.02f, I, I + 0.04f, I + 0.22f));
            Jolt(fx, c.Age, I, 1.0f, 0.4f);
        }
        Impact(c, col, fx, 1.4f);
    }

    /// <summary>Psychic Noise: lilac sound-rings with zigzag edges roll out of the user, and over the foe's head a heart is struck through with a red bar: its healing barred.</summary>
    private static void PsychicNoiseY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        for (int i = 0; i < 4; i++)
        {
            float k = (c.Age - 0.02f - i * 0.07f) / (I - 0.02f);
            if (k < 0f || k > 1.2f) continue;
            var centre = Vector3.Lerp(c.From, c.Aim, Math.Min(1f, Fx.EaseOut(k)));
            int n = 20;
            var pts = new Vector3[n + 1];
            for (int j = 0; j <= n; j++)
            {
                float a = -1.0f + 2.0f * j / n;
                float r = h * (0.3f + 0.4f * Math.Min(1f, k)) * (1f + ((j % 2 == 0) ? 0.1f : -0.1f));
                pts[j] = centre + new Vector3(MathF.Cos(a) * r, MathF.Sin(a) * r * 1.4f, 0.1f);
            }
            fx.Ribbon(pts, h * 0.1f, Fx.Fade(i % 2 == 0 ? Rgb(214, 150, 255) : Rgb(255, 150, 214), Math.Max(0f, 1f - k * 0.6f)), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            float env = Fx.Envelope(c.Age, I, I + 0.08f, 0.85f, 1.05f);
            var at = c.ToFeet + new Vector3(0f, c.ToSize * 1.15f, 0.4f);
            fx.Sprite(FxShape.Heart, at, c.ToSize * 0.3f, Fx.Fade(Rgb(255, 120, 170), env), FxBlend.Alpha);
            fx.Ribbon(new[] { at + new Vector3(-c.ToSize * 0.2f, c.ToSize * 0.2f, 0.05f), at + new Vector3(c.ToSize * 0.2f, -c.ToSize * 0.2f, 0.05f) }, c.ToSize * 0.05f, Fx.Fade(Rgb(240, 40, 60), env), FxBlend.Alpha);
            PulseRing(fx, c.Age, c.To, I, 0.4f, c.ToSize * 0.3f, c.ToSize * 1.5f, Rgb(214, 150, 255), White);
            Jolt(fx, c.Age, I, 0.4f, 0.25f);
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Upper Hand: an exclamation mark flashes over the foe as the user sees its move coming, then a swift open palm shoots out ahead of it and slaps it down.</summary>
    private static void UpperHandY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float see = Fx.Envelope(c.Age, 0f, 0.04f, 0.14f, 0.22f);
        if (see > 0f) fx.Sprite(FxShape.Exclaim, c.ToFeet + new Vector3(h * 0.3f, h * 1.1f, 0.3f), h * 0.3f, Fx.Fade(Rgb(255, 214, 90), see), FxBlend.Alpha);
        Strike(c, fx, FxShape.Palm, new Vector3(-1.5f, 0.1f, 0f), I, 0.85f, Rgb(255, 196, 140), 0f, 0.1f);
        float k = (c.Age - (I - 0.1f)) / 0.1f;
        if (k > 0f && c.Age < I + 0.12f) fx.Streak(FxShape.Streak, c.To + new Vector3(-h * (1.2f - 1.0f * Fx.EaseOut(Math.Min(1f, k))), 0f, 0.2f), Vector3.UnitX, h * 0.3f, 5f, Fx.Fade(White, 0.8f));
        if (c.Cue.Landed)
        {
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.35f, h * 1.5f, Fx.Fade(White, 0.9f));
            L2Flinch(c, fx, I + 0.05f);
            Jolt(fx, c.Age, I, 0.7f, 0.25f);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Malignant Chain: a chain of violet-green iron links, seeping poison, is flung from the user and coils round the foe, the links glowing as the venom soaks in.</summary>
    private static void MalignantChainY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float reach = Fx.EaseOut(Math.Clamp((c.Age - 0.03f) / (I - 0.03f), 0f, 1f));
        float env = Fx.Envelope(c.Age, 0f, 0.06f, 0.9f, 1.15f);
        if (env > 0f)
        {
            for (int i = 0; i <= 9; i++)
            {
                float u = i / 9f * reach;
                var p = Vector3.Lerp(c.From, c.Aim, u) + new Vector3(0f, MathF.Sin(u * 7f) * c.FromSize * 0.15f, 0.1f);
                L2Ellipse(fx, p, c.FromSize * 0.12f, c.FromSize * 0.07f, c.FromSize * 0.03f, Fx.Fade(i % 2 == 0 ? Rgb(150, 150, 170) : Rgb(120, 80, 160), env), 8, i % 2 == 0 ? 0f : 1.57f, FxBlend.Alpha);
            }
        }
        if (c.Cue.Landed)
        {
            float wrap = Fx.Envelope(c.Age, I, I + 0.06f, 0.9f, 1.15f);
            float grow = Fx.EaseOut(Math.Clamp((c.Age - I) / 0.3f, 0f, 1f));
            for (int b = 0; b < 3; b++)
                for (int i = 0; i < 12; i++)
                {
                    float a = i * MathF.Tau / 12f + b;
                    var p = c.To + new Vector3(MathF.Cos(a) * h * 0.5f * grow, (b - 1) * h * 0.3f + MathF.Sin(a) * h * 0.06f, MathF.Sin(a) * 0.3f + 0.1f);
                    L2Ellipse(fx, p, h * 0.08f, h * 0.05f, h * 0.02f, Fx.Fade(i % 2 == 0 ? Rgb(150, 150, 170) : Rgb(150, 200, 90), wrap), 6, a, FxBlend.Alpha);
                }
            fx.Sprite(FxShape.Glow, c.To, h * 1.2f, Fx.Fade(Rgb(172, 78, 210), wrap * 0.4f), FxBlend.Alpha);
            Fx.Rain(fx, c.Age, c.Seed, c.To, h, I, 1.0f, 0.4f, 4, FxShape.Drop, Rgb(172, 78, 210), Rgb(190, 240, 110), 0.1f, FxBlend.Alpha);
            Jolt(fx, c.Age, I, 0.6f, 0.25f);
        }
        Impact(c, col, fx, 1.05f);
    }
}
