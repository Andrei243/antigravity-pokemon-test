using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

// Plan 25 - Y13a, the second half: Generation 7's regular moves from Shell Trap (704) to Double Iron Bash (742), the
// seventh game's own Pikachu and Eevee moves (729 to 733 and the rest of the Let's Go set) among them. As MoveFx.Later4.cs.
internal static partial class MoveFx
{
    private static Dictionary<string, FxRecipe> Later4b() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Shell Trap"] = ShellTrapY13,
        ["Fleur Cannon"] = FleurCannonY13,
        ["Psychic Fangs"] = PsychicFangsY13,
        ["Stomping Tantrum"] = StompingTantrumY13,
        ["Shadow Bone"] = ShadowBoneY13,
        ["Accelerock"] = AccelerockY13,
        ["Liquidation"] = LiquidationY13,
        ["Prismatic Laser"] = PrismaticLaserY13,
        ["Spectral Thief"] = SpectralThiefY13,
        ["Sunsteel Strike"] = SunsteelStrikeY13,
        ["Moongeist Beam"] = MoongeistBeamY13,
        ["Tearful Look"] = TearfulLookY13,
        ["Zing Zap"] = ZingZapY13,
        ["Nature's Madness"] = NaturesMadnessY13,
        ["Multi-Attack"] = MultiAttackY13,
        ["Mind Blown"] = MindBlownY13,
        ["Plasma Fists"] = PlasmaFistsY13,
        ["Photon Geyser"] = PhotonGeyserY13,
        ["Zippy Zap"] = ZippyZapY13,
        ["Splishy Splash"] = SplishySplashY13,
        ["Floaty Fall"] = FloatyFallY13,
        ["Pika Papow"] = PikaPapowY13,
        ["Bouncy Bubble"] = BouncyBubbleY13,
        ["Buzzy Buzz"] = BuzzyBuzzY13,
        ["Sizzly Slide"] = SizzlySlideY13,
        ["Glitzy Glow"] = GlitzyGlowY13,
        ["Baddy Bad"] = BaddyBadY13,
        ["Sappy Seed"] = SappySeedY13,
        ["Freezy Frost"] = FreezyFrostY13,
        ["Sparkly Swirl"] = SparklySwirlY13,
        ["Veevee Volley"] = VeeveeVolleyY13,
        ["Double Iron Bash"] = DoubleIronBashY13
    };

    // ------------------------------------------------------------------ the recipes

    /// <summary>Shell Trap: a spiked trap of orange shell plates is set down at the foe's feet, snaps shut when the blow lands and bursts upward in a column of flame.</summary>
    private static void ShellTrapY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float set = Fx.Envelope(c.Age, 0.02f, 0.12f, I - 0.03f, I + 0.02f);
        if (set > 0f)
        {
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathF.Tau / 6f;
                fx.Flat(FxShape.Hex, c.ToFeet + new Vector3(MathF.Cos(a) * h * 0.55f, 0.05f, MathF.Sin(a) * h * 0.3f), Vector3.UnitY, h * 0.2f, Fx.Fade(Rgb(214, 130, 60), set), FxBlend.Alpha, a);
            }
            fx.Flat(FxShape.Ring, c.ToFeet + Vector3.UnitY * 0.06f, Vector3.UnitY, h * 0.75f, Fx.Fade(col.Main, set), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            float up = Fx.Envelope(c.Age, I, I + 0.05f, I + 0.3f, I + 0.55f);
            fx.Streak(FxShape.Flame, c.ToFeet + new Vector3(0f, h * 0.7f, 0.1f), Vector3.UnitY, h * 0.5f, 1.8f, Fx.Fade(col.Main, up), FxBlend.Add);
            fx.Streak(FxShape.Flame, c.ToFeet + new Vector3(0f, h * 0.6f, 0.15f), Vector3.UnitY, h * 0.28f, 2.2f, Fx.Fade(col.Light, up), FxBlend.Add);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 10, FxShape.Ember, col.Light, col.Main, 0.1f, 1.0f, 0.2f, salt: 351);
            GroundRing(fx, c.Age, c.ToFeet, I, 0.45f, h * 1.3f, col.Light);
            fx.ScreenFlash(Rgb(255, 200, 140), 0.18f * Fx.Envelope(c.Age, I, I + 0.03f, I + 0.06f, I + 0.22f));
            Jolt(fx, c.Age, I, 0.6f, 0.3f);
        }
        Impact(c, col, fx, 1.2f);
    }

    /// <summary>Fleur Cannon: a great pink blossom opens before the user and fires a wide beam of petal-light from its heart, the recoil pressing the user's Sp. Atk down.</summary>
    private static void FleurCannonY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var heart = c.From + new Vector3(h * 0.5f, h * 0.1f, 0.2f);
        float open = Fx.Envelope(c.Age, 0f, 0.14f, 0.5f, 0.7f);
        if (open > 0f) L3Flower(fx, heart, h * 0.5f * open, Rgb(255, 170, 214), Rgb(255, 246, 200), open, 0.3f);
        Fx.Beam(fx, c, 0.14f, 0.62f, h * 0.3f, Fx.Fade(Rgb(255, 150, 210), 0.85f), White, 10);
        Fx.Stream(fx, c, 0.16f, 0.6f, 0.3f, 9, FxShape.Petal, Rgb(255, 180, 220), White, 0.14f, spread: 0.45f, spin: 6f, blend: FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.8f, 12, FxShape.Petal, col.Main, White, 0.14f, 1.2f, 0.2f, FxBlend.Alpha, spin: 5f, salt: 352);
            Jolt(fx, c.Age, I, 0.7f, 0.3f);
        }
        L2Down(fx, c.Age, c.Seed, c.FromFeet, h, 0.6f, 2, Rgb(255, 150, 200));
        Impact(c, col, fx, 1.25f);
    }

    /// <summary>Psychic Fangs: a pair of violet-pink fangs bite shut on the foe, and the glassy panels of any screen it had splinter away in shards.</summary>
    private static void PsychicFangsY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        FangPair(c, fx, I - 0.12f, 0.45f, Rgb(250, 190, 255), col.Light);
        if (c.Cue.Landed)
        {
            for (int i = 0; i < 4; i++)
            {
                float a = -0.6f + i * 0.5f;
                fx.Sprite(FxShape.Hex, c.ToFeet + new Vector3(h * (0.5f + 0.1f * i), h * (0.3f + 0.2f * i), 0.3f) + new Vector3(MathF.Cos(a), MathF.Sin(a), 0f) * h * 0.3f * (c.Age - I), h * 0.14f, Fx.Fade(col.Light, Math.Max(0f, 1f - (c.Age - I) / 0.5f) * 0.8f), FxBlend.Alpha, a + (c.Age - I) * 6f);
            }
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 9, FxShape.Shard, White, col.Light, 0.1f, 1.1f, 0.4f, FxBlend.Alpha, salt: 353);
            Jolt(fx, c.Age, I, 0.5f, 0.25f);
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Stomping Tantrum: an angry mark pops over the user and it stamps its foot twice, the ground shaking, a pair of red marks flying up with the dust.</summary>
    private static void StompingTantrumY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float ang = Fx.Envelope(c.Age, 0f, 0.05f, 0.3f, 0.45f);
        fx.Sprite(FxShape.Exclaim, c.From + new Vector3(c.FromSize * 0.5f, c.FromSize * 0.65f, 0.2f), c.FromSize * 0.2f, Fx.Fade(Rgb(255, 80, 70), ang), FxBlend.Alpha);
        DropOn(c, fx, FxShape.Foot, 0.7f, 0f, I - 0.12f, 0.45f, Rgb(214, 180, 126), 0f);
        DropOn(c, fx, FxShape.Foot, 0.8f, 0f, I, 0.6f, Rgb(236, 206, 150), 0f);
        if (c.Cue.Landed)
        {
            GroundRing(fx, c.Age, c.ToFeet, I - 0.12f, 0.4f, h * 1.0f, col.Main);
            GroundRing(fx, c.Age, c.ToFeet, I, 0.5f, h * 1.6f, col.Light);
            Dust(c, fx, c.ToFeet, h, I, 8);
            Jolt(fx, c.Age, I - 0.12f, 0.4f, 0.2f);
            Jolt(fx, c.Age, I, 0.8f, 0.3f);
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Shadow Bone: a pale bone spins out of the dark and is thrown across, wreathed in violet ghost-fire, and cracks down on the foe.</summary>
    private static void ShadowBoneY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = (c.Age - 0.04f) / (I - 0.04f);
        if (k >= 0f && k <= 1f)
        {
            var p = Lob(c.From, c.Aim, k, 0.1f);
            float a = k * 14f;
            var d = new Vector3(MathF.Cos(a), MathF.Sin(a), 0f);
            float s = c.FromSize * 0.4f;
            fx.Ribbon(new[] { p - d * s, p + d * s }, c.FromSize * 0.06f, Fx.Fade(Rgb(236, 232, 222), 1f), FxBlend.Alpha);
            fx.Sprite(FxShape.Dot, p - d * s, c.FromSize * 0.09f, Fx.Fade(Rgb(236, 232, 222), 1f), FxBlend.Alpha);
            fx.Sprite(FxShape.Dot, p + d * s, c.FromSize * 0.09f, Fx.Fade(Rgb(236, 232, 222), 1f), FxBlend.Alpha);
            fx.Sprite(FxShape.Wisp, p, c.FromSize * 0.4f, Fx.Fade(col.Main, 0.7f), FxBlend.Alpha, a);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 8, FxShape.Wisp, col.Light, col.Main, 0.18f, 0.9f, -0.2f, FxBlend.Alpha, salt: 354);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 5, FxShape.Shard, Rgb(236, 232, 222), White, 0.08f, 1.0f, 0.4f, FxBlend.Alpha, salt: 355);
            Jolt(fx, c.Age, I, 0.5f, 0.25f);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Accelerock: a rock gone ahead of the eye leaves a long grey streak across the field and is on the foe before the streak has faded.</summary>
    private static void AccelerockY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = Math.Clamp((c.Age - 0.04f) / (I - 0.1f), 0f, 1f);
        if (c.Age < I + 0.1f)
        {
            var p = Vector3.Lerp(c.From, c.Aim, Fx.EaseOut(k));
            fx.Streak(FxShape.Streak, p, c.Aim - c.From, c.FromSize * 0.16f, 7f, Fx.Fade(Rgb(210, 200, 180), 0.85f), FxBlend.Alpha);
            fx.Streak(FxShape.Streak, p, c.Aim - c.From, c.FromSize * 0.05f, 7f, White);
            fx.Sprite(FxShape.Rock, p, c.FromSize * 0.22f, Fx.Fade(Rgb(176, 156, 120), 1f), FxBlend.Alpha, k * 9f);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 8, FxShape.Rock, Rgb(190, 170, 130), Rgb(130, 112, 84), 0.1f, 1.0f, 0.5f, FxBlend.Alpha, spin: 6f, salt: 356);
            Jolt(fx, c.Age, I, 0.4f, 0.2f);
        }
        Impact(c, col, fx, 0.85f);
    }

    /// <summary>Liquidation: a blade of pressurised water, white-edged, slices diagonally through the foe in a curtain of foam, and it is left with a thin film of drops.</summary>
    private static void LiquidationY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = (c.Age - (I - 0.14f)) / 0.2f;
        if (k > 0f && c.Age < I + 0.35f)
        {
            float fade = c.Age < I ? 1f : Math.Max(0f, 1f - (c.Age - I) / 0.35f);
            var along = Vector3.Normalize(new Vector3(1f, -0.9f, 0f));
            float len = Fx.EaseOut(Math.Min(1f, k));
            var mid = c.To + new Vector3(-0.1f * h, 0.1f * h, 0.3f);
            fx.Streak(FxShape.Sword, mid, along, h * 0.23f, 3.4f * len, Fx.Fade(col.Main, fade), FxBlend.Alpha);
            fx.Streak(FxShape.Beam, mid, along, h * 0.06f, 12f * len, Fx.Fade(White, fade));
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 12, FxShape.Drop, col.Light, White, 0.1f, 1.2f, 0.5f, FxBlend.Alpha, salt: 357);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 5, FxShape.Bubble, White, col.Light, 0.12f, 0.7f, -0.1f, FxBlend.Alpha, salt: 358);
            L2Down(fx, c.Age, c.Seed, c.ToFeet, h, I + 0.1f, 2, Rgb(150, 200, 255));
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Prismatic Laser: a clear prism of light forms before the user, takes in a single white beam and splits it into five coloured ones that close on the foe.</summary>
    private static void PrismaticLaserY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var prism = c.From + new Vector3(h * 0.6f, h * 0.15f, 0.2f);
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.55f, 0.75f);
        if (env > 0f)
        {
            var tri = new[] { prism + new Vector3(0f, h * 0.3f, 0f), prism + new Vector3(-h * 0.26f, -h * 0.2f, 0f), prism + new Vector3(h * 0.26f, -h * 0.2f, 0f), prism + new Vector3(0f, h * 0.3f, 0f) };
            fx.Ribbon(tri, h * 0.05f, Fx.Fade(White, env), FxBlend.Add);
            fx.Sprite(FxShape.Glow, prism, h * 0.6f * env, Fx.Fade(col.Light, env * 0.5f));
        }
        var hues = new[] { Rgb(255, 80, 80), Rgb(255, 190, 70), Rgb(120, 255, 130), Rgb(80, 190, 255), Rgb(190, 110, 255) };
        float fire = Fx.Envelope(c.Age, 0.12f, 0.22f, I + 0.12f, I + 0.35f);
        if (fire > 0f)
        {
            fx.Ribbon(new[] { c.From, prism }, h * 0.05f, Fx.Fade(White, fire), FxBlend.Add);
            for (int i = 0; i < 5; i++)
            {
                var target = c.Aim + new Vector3(0f, (i - 2) * c.ToSize * 0.1f, 0f);
                var end = Vector3.Lerp(prism, target, Fx.EaseOut(Math.Clamp((c.Age - 0.14f) / 0.2f, 0f, 1f)));
                var mid = Vector3.Lerp(prism, end, 0.5f) + new Vector3(0f, (i - 2) * h * 0.18f * (1f - Math.Clamp((c.Age - 0.14f) / 0.3f, 0f, 1f)), 0f);
                fx.Ribbon(new[] { prism, mid, end }, h * 0.07f, Fx.Fade(hues[i], fire), FxBlend.Add);
            }
        }
        if (c.Cue.Landed)
        {
            fx.ScreenFlash(White, 0.3f * Fx.Envelope(c.Age, I - 0.02f, I, I + 0.05f, I + 0.22f));
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.7f, 10, FxShape.Star, White, col.Light, 0.12f, 1.1f, 0.2f, FxBlend.Alpha, spin: 6f, salt: 359);
            Jolt(fx, c.Age, I, 0.6f, 0.3f);
        }
        Impact(c, col, fx, 1.3f);
    }

    /// <summary>Spectral Thief: a violet ghost hand reaches out of the foe's shadow and pulls its raised stats out as arrows that fly across to the user.</summary>
    private static void SpectralThiefY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.35f, 0.5f);
        if (env > 0f)
        {
            fx.Sprite(FxShape.Palm, c.ToFeet + new Vector3(h * 0.3f, h * 0.5f * Fx.EaseOut(Math.Min(1f, c.Age / 0.2f)), 0.3f), h * 0.45f, Fx.Fade(col.Main, env * 0.9f), FxBlend.Alpha, 0.3f);
            Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + new Vector3(h * 0.3f, h * 0.3f, 0f), h, 0.05f, 0.5f, 5, FxShape.Wisp, col.Light, col.Main, 0.16f, 0.5f, -0.2f, FxBlend.Alpha, salt: 360);
        }
        for (int i = 0; i < 3; i++)
        {
            float k = (c.Age - 0.22f - i * 0.07f) / 0.35f;
            if (k < 0f || k > 1f) continue;
            var p = Vector3.Lerp(c.To + new Vector3(h * 0.3f, h * 0.4f, 0f), c.From + new Vector3(0f, c.FromSize * 0.6f, 0f), Fx.EaseOut(k)) + new Vector3(0f, MathF.Sin(k * MathF.PI) * h * 0.3f, 0.3f);
            fx.Sprite(FxShape.Arrow, p, h * 0.14f, Fx.Fade(Rgb(255, 150, 150), MathF.Sin(k * MathF.PI)), FxBlend.Alpha);
        }
        if (c.Cue.Landed) Fx.Contact(fx, c.Age, c.Seed, c.To, h, I, 0.3f, col.Main, col.Light);
        L3Up(fx, c.Age, c.FromFeet, c.FromSize, 0.55f, 3, Rgb(255, 130, 130));
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Sunsteel Strike: the user streaks down as a blazing steel comet, a silver-orange sun with plates of metal round it, and drives into the foe in a ring of fire.</summary>
    private static void SunsteelStrikeY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = Math.Clamp((c.Age - 0.04f) / (I - 0.04f), 0f, 1f);
        if (c.Age < I + 0.08f)
        {
            var from = c.To + new Vector3(-h * 1.4f, h * 1.3f, 0.2f);
            var p = Vector3.Lerp(from, c.To, k * k);
            var dir = c.To - from;
            fx.Streak(FxShape.Streak, p, dir, h * 0.35f, 4f, Fx.Fade(Rgb(255, 150, 50), 0.8f), FxBlend.Add);
            fx.Sprite(FxShape.Glow, p, h * 0.9f, Fx.Fade(Rgb(255, 190, 90), 0.7f));
            fx.Sprite(FxShape.Orb, p, h * 0.42f, Fx.Fade(Rgb(255, 232, 160), 1f), FxBlend.Alpha);
            for (int i = 0; i < 4; i++)
                fx.Sprite(FxShape.Hex, p + new Vector3(MathF.Cos(i * 1.6f + k * 8f), MathF.Sin(i * 1.6f + k * 8f), 0f) * h * 0.38f, h * 0.12f, Fx.Fade(Rgb(206, 214, 228), 1f), FxBlend.Alpha, i);
        }
        if (c.Cue.Landed)
        {
            GroundRing(fx, c.Age, c.ToFeet, I, 0.5f, h * 1.6f, Rgb(255, 170, 80));
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 12, FxShape.Ember, Rgb(255, 224, 150), Rgb(255, 120, 40), 0.1f, 1.2f, 0.3f, salt: 361);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 5, FxShape.Shard, Rgb(214, 222, 236), White, 0.08f, 1.1f, 0.4f, FxBlend.Alpha, salt: 362);
            Jolt(fx, c.Age, I, 0.9f, 0.35f);
        }
        Impact(c, col, fx, 1.3f);
    }

    /// <summary>Moongeist Beam: a pale moon rises at the user's side and its violet light is thrown across as a beam, ghost wisps and dim stars streaming along it.</summary>
    private static void MoongeistBeamY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var moon = c.From + new Vector3(h * 0.45f, h * 0.7f, 0.1f);
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.5f, 0.7f);
        if (env > 0f)
        {
            fx.Sprite(FxShape.Glow, moon, h * 0.9f * env, Fx.Fade(col.Main, env * 0.55f), FxBlend.Alpha);
            fx.Sprite(FxShape.Orb, moon, h * 0.34f * env, Fx.Fade(Rgb(236, 226, 255), env), FxBlend.Alpha);
            fx.Sprite(FxShape.Orb, moon + new Vector3(h * 0.12f, h * 0.04f, 0.05f), h * 0.3f * env, Fx.Fade(col.Dark, env), FxBlend.Alpha);
        }
        Fx.Beam(fx, c, 0.14f, 0.62f, h * 0.24f, Fx.Fade(col.Main, 0.85f), Rgb(240, 230, 255), 6);
        Fx.Stream(fx, c, 0.14f, 0.55f, 0.3f, 8, FxShape.Wisp, col.Light, col.Main, 0.18f, spread: 0.4f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0.16f, 0.55f, 0.3f, 5, FxShape.Star, Rgb(236, 226, 255), White, 0.09f, spread: 0.3f, spin: 4f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.7f, 8, FxShape.Wisp, col.Light, col.Dark, 0.2f, 1.0f, -0.1f, FxBlend.Alpha, salt: 363);
            Jolt(fx, c.Age, I, 0.5f, 0.3f);
        }
        Impact(c, col, fx, 1.2f);
    }

    /// <summary>Tearful Look: two huge wet eyes shine over the user and tears roll down in drops, while two down arrows settle on the foe.</summary>
    private static void TearfulLookY13(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.6f, 0.8f);
        if (env > 0f)
        {
            for (int s = -1; s <= 1; s += 2)
            {
                var eye = c.FromFeet + new Vector3(h * 0.5f + s * h * 0.28f, h * 1.0f, 0.2f);
                fx.Sprite(FxShape.Eye, eye, h * 0.26f, Fx.Fade(White, env), FxBlend.Alpha);
            }
        }
        for (int i = 0; i < 6; i++)
        {
            float u = (c.Age * 1.2f + i / 6f) % 1f;
            int s = i % 2 == 0 ? -1 : 1;
            fx.Sprite(FxShape.Drop, c.FromFeet + new Vector3(h * 0.5f + s * h * 0.3f, h * (0.9f - 0.5f * u), 0.25f), h * 0.07f, Fx.Fade(Rgb(150, 210, 255), env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha, MathF.PI);
        }
        L2Down(fx, c.Age, c.Seed, c.ToFeet, c.ToSize, 0.35f, 2, Rgb(255, 150, 190));
    }

    /// <summary>Zing Zap: a stinging yellow bolt zigzags across and hits with a loud crack, a flash of white and a jolt that shoves the foe back.</summary>
    private static void ZingZapY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Fx.Bolt(fx, c.Age, c.Seed, c.From, c.Aim, 0.12f, 0.5f, h * 0.07f, col.Main, col.Light, 2);
        if (c.Cue.Landed)
        {
            fx.ScreenFlash(Rgb(255, 250, 200), 0.2f * Fx.Envelope(c.Age, I - 0.02f, I, I + 0.04f, I + 0.2f));
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 8, FxShape.Spark, White, col.Light, 0.12f, 1.1f, salt: 364);
            Jolt(fx, c.Age, I, 0.6f, 0.3f);
        }
        L2Flinch(c, fx, I + 0.08f);
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Nature's Madness: the wild power of the land runs through the foe in whirling green and pink motes, and a ring of vines tightens and draws half its strength away.</summary>
    private static void NaturesMadnessY13(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0.02f, 0.2f, 0.82f, 1.05f);
        if (env <= 0f) return;
        L1Spiral(fx, c.Age, c.Seed, c.ToFeet, h, 0.02f, 1.0f, 10, FxShape.Leaf, Rgb(120, 220, 110), Rgb(90, 190, 90), 0.12f, 0.8f, 1.1f);
        L1Spiral(fx, c.Age, c.Seed + 3, c.ToFeet, h, 0.05f, 1.0f, 8, FxShape.Petal, Rgb(255, 150, 200), Rgb(255, 210, 230), 0.1f, 0.65f, 1.0f);
        float tight = Fx.EaseOut(Math.Clamp((c.Age - 0.15f) / 0.4f, 0f, 1f));
        L2Ellipse(fx, c.ToFeet + new Vector3(0f, h * 0.5f, 0.1f), h * (1.0f - 0.45f * tight), h * 0.25f, h * 0.05f, Fx.Fade(Rgb(80, 170, 80), env), 16, c.Age * 3f, FxBlend.Alpha);
        float half = Fx.Envelope(c.Age, I, I + 0.05f, I + 0.4f, I + 0.6f);
        fx.Sprite(FxShape.Glow, c.To, h * 0.9f, Fx.Fade(col.Main, half * 0.5f), FxBlend.Alpha);
        Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 8, FxShape.Spark, White, col.Light, 0.08f, 0.9f, salt: 365);
    }

    /// <summary>Multi-Attack: six orbs, each in a different colour, whirl round the user and are driven together into one huge white-hot ball that slams the foe.</summary>
    private static void MultiAttackY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var hues = new[] { Rgb(255, 90, 80), Rgb(255, 190, 70), Rgb(120, 230, 100), Rgb(80, 190, 255), Rgb(170, 110, 255), Rgb(255, 130, 210) };
        float join = Math.Clamp((c.Age - 0.12f) / 0.22f, 0f, 1f);
        float env = Fx.Envelope(c.Age, 0f, 0.08f, 0.3f, 0.42f);
        var mid = c.From + new Vector3(h * 0.35f, h * 0.1f, 0.15f);
        if (env > 0f)
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathF.Tau / 6f + c.Age * 9f;
                var p = mid + new Vector3(MathF.Cos(a), MathF.Sin(a) * 0.7f, 0f) * h * 0.6f * (1f - join);
                fx.Sprite(FxShape.Glow, p, h * 0.2f, Fx.Fade(hues[i], env * 0.7f));
                fx.Sprite(FxShape.Orb, p, h * 0.13f, Fx.Fade(hues[i], env), FxBlend.Alpha);
            }
        float throwK = (c.Age - 0.3f) / (I - 0.3f);
        if (throwK >= 0f && throwK <= 1f)
        {
            var p = Vector3.Lerp(mid, c.Aim, Fx.EaseOut(throwK));
            fx.Sprite(FxShape.Glow, p, h * 0.9f, Fx.Fade(White, 0.8f));
            fx.Sprite(FxShape.Orb, p, h * 0.45f, Fx.Fade(White, 1f), FxBlend.Alpha);
            for (int i = 0; i < 6; i++) fx.Sprite(FxShape.Ring, p, h * (0.5f + 0.06f * i), Fx.Fade(hues[i], 0.4f));
        }
        if (c.Cue.Landed)
        {
            for (int i = 0; i < 6; i++) Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I + i * 0.02f, 0.4f, c.ToSize * (0.9f + 0.2f * i), Fx.Fade(hues[i], 0.9f));
            Jolt(fx, c.Age, I, 0.9f, 0.35f);
        }
        Impact(c, col, fx, 1.3f);
    }

    /// <summary>Mind Blown: the user's head swells with fire and bursts in a blast, flames pouring from it, and a ball of the same flame is thrown over to the foe.</summary>
    private static void MindBlownY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var head = c.FromFeet + new Vector3(h * 0.1f, h * 0.95f, 0.2f);
        float swell = Fx.Envelope(c.Age, 0f, 0.2f, 0.25f, 0.34f);
        if (swell > 0f)
        {
            fx.Sprite(FxShape.Glow, head, h * 1.1f * swell, Fx.Fade(col.Main, swell * 0.7f));
            fx.Sprite(FxShape.Flame, head + new Vector3(0f, h * 0.15f, 0f), h * 0.4f * swell, Fx.Fade(col.Light, swell));
        }
        float burst = (c.Age - 0.26f) / 0.45f;
        if (burst > 0f && burst < 1f)
        {
            Fx.Shockwave(fx, c.Age, head, Vector3.Zero, 0.26f, 0.45f, h * 1.6f, Fx.Fade(col.Light, 0.9f));
            Fx.Burst(fx, c.Seed, c.Age, head, h, 0.26f, 0.5f, 12, FxShape.Flame, col.Light, col.Main, 0.2f, 1.0f, 0.2f, salt: 366);
            fx.ScreenFlash(Rgb(255, 220, 160), 0.22f * Fx.Envelope(c.Age, 0.26f, 0.29f, 0.32f, 0.45f));
            Jolt(fx, c.Age, 0.26f, 0.5f, 0.25f);
        }
        float k = (c.Age - 0.3f) / (I - 0.3f);
        if (k >= 0f && k <= 1f)
        {
            var p = Vector3.Lerp(head, c.Aim, Fx.EaseOut(k));
            fx.Sprite(FxShape.Glow, p, h * 0.8f, Fx.Fade(col.Main, 0.8f));
            fx.Sprite(FxShape.Flame, p, h * 0.5f, Fx.Fade(col.Light, 1f), FxBlend.Add, k * 5f);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.7f, 12, FxShape.Ember, col.Light, col.Main, 0.1f, 1.2f, 0.2f, salt: 367);
            Jolt(fx, c.Age, I, 0.8f, 0.35f);
        }
        Impact(c, col, fx, 1.35f);
    }

    /// <summary>Plasma Fists: two yellow-white fists crackling with plasma pound the foe in turn, rings of electric current rippling out of each blow.</summary>
    private static void PlasmaFistsY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Strike(c, fx, FxShape.Fist, new Vector3(-1.2f, 0.4f, 0f), I - 0.12f, 0.45f, Rgb(255, 244, 170), 0f, 0.1f);
        Strike(c, fx, FxShape.Fist, new Vector3(-1.2f, -0.2f, 0f), I, 0.55f, Rgb(255, 250, 200), 0.2f, 0.1f);
        if (c.Cue.Landed)
        {
            for (int i = 0; i < 2; i++)
            {
                float t = I - 0.12f + i * 0.12f;
                Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, t, 0.4f, h * (1.2f + 0.3f * i), Fx.Fade(col.Main, 0.9f));
                Fx.Bolt(fx, c.Age, c.Seed + i * 7, c.To + new Vector3(-h * 0.5f, h * 0.3f, 0f), c.To + new Vector3(h * 0.5f, -h * 0.3f, 0f), t, t + 0.2f, h * 0.04f, col.Main, col.Light, 1);
            }
            Jolt(fx, c.Age, I, 0.7f, 0.3f);
        }
        Impact(c, col, fx, 1.15f);
    }

    /// <summary>Photon Geyser: a column of violet-white light erupts from the ground under the foe, galaxies of stars turning through it, and a beam shoots on up out of the picture.</summary>
    private static void PhotonGeyserY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float gather = Fx.Envelope(c.Age, 0f, 0.08f, I - 0.05f, I);
        if (gather > 0f) fx.Flat(FxShape.Ring, c.ToFeet + Vector3.UnitY * 0.05f, Vector3.UnitY, h * (1.1f - 0.5f * gather), Fx.Fade(col.Light, gather), FxBlend.Alpha);
        Pillar(c, fx, c.ToFeet, I - 0.08f, 1.0f, h * 0.55f, Fx.Fade(col.Main, 0.8f), White);
        if (c.Cue.Landed)
        {
            for (int i = 0; i < 8; i++)
            {
                float u = (c.Age * 1.4f + i / 8f) % 1f;
                float a = i * 1.3f + c.Age * 6f;
                fx.Sprite(i % 2 == 0 ? FxShape.Star : FxShape.Spark, c.ToFeet + new Vector3(MathF.Cos(a) * h * 0.4f, h * (0.1f + 1.0f * u), MathF.Sin(a) * 0.1f), h * 0.1f, Fx.Fade(White, MathF.Sin(u * MathF.PI) * Fx.Envelope(c.Age, I, I + 0.05f, 0.85f, 1.0f)), FxBlend.Alpha, a);
            }
            GroundRing(fx, c.Age, c.ToFeet, I, 0.5f, h * 1.5f, col.Light);
            fx.ScreenFlash(Rgb(236, 220, 255), 0.2f * Fx.Envelope(c.Age, I, I + 0.03f, I + 0.06f, I + 0.22f));
            Jolt(fx, c.Age, I, 0.7f, 0.3f);
        }
        Impact(c, col, fx, 1.25f);
    }

    /// <summary>Zippy Zap: the user flashes across in a quick yellow zigzag of lightning, leaving a trail of sparks that crackles for a moment, and always lands a critical star.</summary>
    private static void ZippyZapY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = Math.Clamp((c.Age - 0.02f) / (I - 0.06f), 0f, 1f);
        if (c.Age < I + 0.2f)
        {
            var pts = new Vector3[7];
            for (int i = 0; i < 7; i++)
            {
                float u = i / 6f * Fx.EaseOut(k);
                pts[i] = Vector3.Lerp(c.From, c.Aim, u) + new Vector3(0f, (i % 2 == 0 ? 1f : -1f) * c.FromSize * 0.3f * (i == 0 || i == 6 ? 0f : 1f), 0.1f);
            }
            float fade = c.Age < I ? 1f : Math.Max(0f, 1f - (c.Age - I) / 0.2f);
            fx.Ribbon(pts, c.FromSize * 0.12f, Fx.Fade(col.Main, fade), FxBlend.Add);
            fx.Ribbon(pts, c.FromSize * 0.04f, Fx.Fade(White, fade));
        }
        if (c.Cue.Landed)
        {
            float star = Fx.Envelope(c.Age, I, I + 0.03f, I + 0.12f, I + 0.3f);
            fx.Sprite(FxShape.Star, c.To, h * 0.7f * star, Fx.Fade(White, star), FxBlend.Alpha, MathF.PI / 4f);
            fx.Sprite(FxShape.Star, c.To, h * 0.5f * star, Fx.Fade(col.Light, star));
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 7, FxShape.Spark, White, col.Main, 0.1f, 1.0f, salt: 368);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Splishy Splash: a big wave of splashing water rolls across with little yellow sparks jumping in it, and breaks over the foe in a burst of foam.</summary>
    private static void SplishySplashY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Fx.WaveWall(fx, c, 0f, 0.55f, col.Main, White);
        Fx.Stream(fx, c, 0.05f, 0.5f, 0.3f, 3, FxShape.Spark, Rgb(255, 240, 110), White, 0.1f, spread: 0.5f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 7, FxShape.Drop, col.Light, White, 0.11f, 1.2f, 0.5f, FxBlend.Alpha, salt: 369);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 3, FxShape.Spark, Rgb(255, 240, 110), White, 0.08f, 1.0f, salt: 370);
            GroundRing(fx, c.Age, c.ToFeet, I, 0.45f, h * 1.4f, col.Light);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Floaty Fall: the user bobs up on soft white puffs and feathers, hangs a moment, then drops on the foe on a cloud that bursts in a flurry of down.</summary>
    private static void FloatyFallY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float up = Fx.Envelope(c.Age, 0f, 0.08f, 0.2f, 0.32f);
        for (int i = 0; i < 4; i++)
        {
            float u = (c.Age * 1.5f + i * 0.25f) % 1f;
            fx.Sprite(FxShape.Feather, c.FromFeet + new Vector3(Fx.Signed(c.Seed, i, 371) * c.FromSize * 0.6f, c.FromSize * (0.2f + 0.6f * u), 0.2f), c.FromSize * 0.12f, Fx.Fade(White, up * MathF.Sin(u * MathF.PI)), FxBlend.Alpha, u * 3f);
        }
        if (up > 0f) fx.Sprite(FxShape.Smoke, c.FromFeet + new Vector3(0f, c.FromSize * 0.1f, 0.1f), c.FromSize * 0.5f, Fx.Fade(Rgb(240, 246, 255, 220), up), FxBlend.Alpha);
        DropOn(c, fx, FxShape.Smoke, 1.0f, 0f, I, 0.65f, Rgb(236, 244, 255), 0f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.8f, 9, FxShape.Feather, White, col.Light, 0.12f, 1.0f, -0.15f, FxBlend.Alpha, spin: 3f, salt: 372);
            GroundRing(fx, c.Age, c.ToFeet, I, 0.45f, h * 1.2f, col.Light);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Pika Papow: a heart-shaped ball of lightning, pink at its edge, floats across to the foe throwing sparks, and goes off in a burst of hearts and bolts.</summary>
    private static void PikaPapowY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = (c.Age - 0.04f) / (I - 0.04f);
        if (k >= 0f && k <= 1f)
        {
            var p = Lob(c.From, c.Aim, k, 0.05f);
            float beat = 1f + 0.12f * MathF.Sin(c.Age * 30f);
            fx.Sprite(FxShape.Glow, p, c.FromSize * 0.8f, Fx.Fade(col.Main, 0.7f));
            fx.Sprite(FxShape.Heart, p, c.FromSize * 0.38f * beat, Fx.Fade(Rgb(255, 120, 150), 1f), FxBlend.Alpha);
            fx.Sprite(FxShape.Heart, p, c.FromSize * 0.28f * beat, Fx.Fade(Rgb(255, 244, 150), 1f));
            fx.Sprite(FxShape.Spark, p + new Vector3(Fx.Signed(c.Seed, (int)(c.Age * 30f), 373) * c.FromSize * 0.4f, Fx.Signed(c.Seed, (int)(c.Age * 30f), 374) * c.FromSize * 0.4f, 0f), c.FromSize * 0.1f, White);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 7, FxShape.Heart, Rgb(255, 130, 160), Rgb(255, 230, 120), 0.12f, 1.0f, 0.1f, FxBlend.Alpha, salt: 375);
            for (int i = 0; i < 3; i++)
                Fx.Bolt(fx, c.Age, c.Seed + i * 11, c.To, c.To + Fx.Direction(c.Seed, 376 + i) * h * 0.9f, I, I + 0.2f, h * 0.035f, col.Main, col.Light, 0);
            Jolt(fx, c.Age, I, 0.5f, 0.25f);
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Bouncy Bubble: a big bubble hops across the field in three bounces, pops on the foe and a small bubble of the drawn-off strength floats back to the user.</summary>
    private static void BouncyBubbleY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = (c.Age - 0.02f) / (I - 0.02f);
        if (k >= 0f && k <= 1f)
        {
            var flat = Vector3.Lerp(c.From, c.Aim, k);
            float hop = MathF.Abs(MathF.Sin(k * MathF.PI * 3f)) * c.FromSize * 0.5f * (1f - 0.3f * k);
            var p = flat + new Vector3(0f, hop, 0f);
            fx.Sprite(FxShape.Bubble, p, c.FromSize * 0.4f, Fx.Fade(col.Main, 0.95f), FxBlend.Alpha);
            fx.Sprite(FxShape.Dot, p + new Vector3(-c.FromSize * 0.1f, c.FromSize * 0.1f, 0.05f), c.FromSize * 0.07f, Fx.Fade(White, 0.9f), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 9, FxShape.Bubble, col.Light, White, 0.1f, 1.0f, 0.1f, FxBlend.Alpha, salt: 377);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 6, FxShape.Drop, col.Light, White, 0.08f, 0.9f, 0.4f, FxBlend.Alpha, salt: 378);
            float back = (c.Age - I) / 0.45f;
            if (back > 0f && back < 1f) fx.Sprite(FxShape.Bubble, Vector3.Lerp(c.To, c.From, Fx.EaseOut(back)), c.FromSize * 0.16f, Fx.Fade(Rgb(150, 240, 190), MathF.Sin(back * MathF.PI)), FxBlend.Alpha);
        }
        Impact(c, col, fx, 0.8f);
    }

    /// <summary>Buzzy Buzz: a swarm of yellow sparks buzzes round the foe in tight loops, short black-edged bolts darting between them.</summary>
    private static void BuzzyBuzzY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Fx.Stream(fx, c, 0f, 0.4f, 0.22f, 6, FxShape.Spark, col.Main, col.Light, 0.1f, spread: 0.3f);
        float env = Fx.Envelope(c.Age, I - 0.1f, I, I + 0.4f, I + 0.6f);
        if (env > 0f)
            for (int i = 0; i < 6; i++)
            {
                float a = c.Age * 12f + i * MathF.Tau / 6f;
                var p = c.To + new Vector3(MathF.Cos(a) * h * 0.55f, MathF.Sin(a * 1.3f) * h * 0.35f, 0.15f);
                var q = c.To + new Vector3(MathF.Cos(a + 2.1f) * h * 0.55f, MathF.Sin((a + 2.1f) * 1.3f) * h * 0.35f, 0.15f);
                fx.Sprite(FxShape.Dot, p, h * 0.07f, Fx.Fade(Rgb(40, 34, 30), env), FxBlend.Alpha);
                fx.Sprite(FxShape.Spark, p, h * 0.1f, Fx.Fade(col.Light, env));
                if (i % 2 == 0) Fx.Bolt(fx, c.Age, c.Seed + i, p, q, I - 0.05f, I + 0.4f, h * 0.025f, col.Main, col.Light, 0);
            }
        if (c.Cue.Landed) Jolt(fx, c.Age, I, 0.35f, 0.3f);
        Impact(c, col, fx, 0.8f);
    }

    /// <summary>Sizzly Slide: the user slides in as a streak of flame along the ground, scorching a black furrow and kicking up fire at the foe's feet.</summary>
    private static void SizzlySlideY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = Math.Clamp((c.Age - 0.02f) / (I - 0.06f), 0f, 1f);
        var a = c.FromFeet;
        var b = c.ToFeet;
        var p = Vector3.Lerp(a, b, Fx.EaseOut(k));
        float env = Fx.Envelope(c.Age, 0f, 0.04f, I + 0.35f, I + 0.6f);
        if (env > 0f)
        {
            fx.Ribbon(new[] { a + Vector3.UnitY * 0.05f, p + Vector3.UnitY * 0.05f }, h * 0.14f, Fx.Fade(Rgb(40, 28, 24), env * 0.8f), FxBlend.Alpha);
            for (int i = 0; i < 7; i++)
            {
                float u = i / 6f * Fx.EaseOut(k);
                fx.Sprite(FxShape.Flame, Vector3.Lerp(a, b, u) + new Vector3(0f, h * (0.12f + 0.06f * MathF.Sin(c.Age * 20f + i)), 0.1f), h * 0.2f, Fx.Fade(i % 2 == 0 ? col.Main : col.Light, env), FxBlend.Add);
            }
            fx.Sprite(FxShape.Glow, p + Vector3.UnitY * h * 0.2f, h * 0.7f, Fx.Fade(col.Main, env * 0.6f));
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + Vector3.UnitY * h * 0.1f, h, I, 0.6f, 9, FxShape.Ember, col.Light, col.Main, 0.09f, 0.9f, 0.3f, salt: 379);
            Dust(c, fx, c.ToFeet, h, I, 4);
        }
        Impact(c, col, fx, 0.85f);
    }

    /// <summary>Glitzy Glow: a spray of pink glitter bursts at the user and a shimmering pane of glass-bright light stands up to defend its side, then the glitter is thrown across.</summary>
    private static void GlitzyGlowY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Fx.Stream(fx, c, 0f, 0.5f, 0.3f, 10, FxShape.Star, Rgb(255, 200, 240), White, 0.11f, arc: 0.04f, spread: 0.45f, spin: 6f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0.04f, 0.5f, 0.3f, 6, FxShape.Spark, Rgb(255, 244, 150), White, 0.1f, spread: 0.4f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 10, FxShape.Star, White, col.Light, 0.12f, 1.1f, 0.2f, FxBlend.Alpha, spin: 6f, salt: 380);
            Fx.Glint(fx, c.Age, c.To, h, I + 0.1f, White, 0.5f);
        }
        float env = Fx.Envelope(c.Age, I - 0.05f, I + 0.1f, 0.9f, 1.1f);
        if (env > 0f)
        {
            var at = c.FromFeet + L1Toward(c) * c.FromSize * 0.2f + new Vector3(c.FromSize * 0.4f, c.FromSize * 0.5f, 0.3f);
            fx.Streak(FxShape.Hex, at, Vector3.UnitY, c.FromSize * 0.3f, 1.5f, Fx.Fade(Rgb(255, 190, 235), env * 0.55f), FxBlend.Alpha);
            fx.Sprite(FxShape.Spark, at + new Vector3(0f, MathF.Sin(c.Age * 8f) * c.FromSize * 0.3f, 0.1f), c.FromSize * 0.2f, Fx.Fade(White, env));
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Baddy Bad: a thick black cloud with a sneering red grin is thrown across, bursts on the foe and leaves a dark red pane standing at the user's side.</summary>
    private static void BaddyBadY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = (c.Age - 0.03f) / (I - 0.03f);
        if (k >= 0f && k <= 1f)
        {
            var p = Lob(c.From, c.Aim, k, 0.06f);
            fx.Sprite(FxShape.Smoke, p, c.FromSize * 0.55f, Fx.Fade(Rgb(40, 24, 56), 0.95f), FxBlend.Alpha, k * 4f);
            fx.Sprite(FxShape.Smoke, p + new Vector3(0.05f, 0.03f, 0.02f), c.FromSize * 0.38f, Fx.Fade(Rgb(70, 40, 90), 0.9f), FxBlend.Alpha, -k * 5f);
            for (int s = -1; s <= 1; s += 2) fx.Sprite(FxShape.Star, p + new Vector3(s * c.FromSize * 0.14f, c.FromSize * 0.07f, 0.05f), c.FromSize * 0.07f, Fx.Fade(Rgb(255, 70, 70), 1f), FxBlend.Alpha, MathF.PI / 4f);
            fx.Sprite(FxShape.Wave, p + new Vector3(0f, -c.FromSize * 0.1f, 0.05f), c.FromSize * 0.16f, Fx.Fade(Rgb(255, 70, 70), 1f), FxBlend.Alpha, MathF.PI);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.9f, 9, FxShape.Smoke, Rgb(50, 30, 70), Rgb(100, 60, 120), 0.3f, 0.9f, -0.1f, FxBlend.Alpha, salt: 381);
            Jolt(fx, c.Age, I, 0.4f, 0.25f);
        }
        float env = Fx.Envelope(c.Age, I - 0.05f, I + 0.1f, 0.9f, 1.1f);
        if (env > 0f)
        {
            var at = c.FromFeet + L1Toward(c) * c.FromSize * 0.2f + new Vector3(c.FromSize * 0.4f, c.FromSize * 0.5f, 0.3f);
            fx.Streak(FxShape.Hex, at, Vector3.UnitY, c.FromSize * 0.3f, 1.5f, Fx.Fade(Rgb(235, 80, 110), env * 0.85f), FxBlend.Alpha);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Sappy Seed: a seed is thrown down at the foe's feet, a vine sprouts from it and grows over the foe in loops, dripping sticky sap.</summary>
    private static void SappySeedY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = (c.Age - 0.03f) / (I - 0.12f);
        if (k >= 0f && k <= 1f) fx.Sprite(FxShape.Seed, Lob(c.From, c.ToFeet + Vector3.UnitY * 0.1f, k, 0.1f), c.FromSize * 0.3f, Fx.Fade(Rgb(150, 220, 90), 1f), FxBlend.Alpha, k * 8f);
        float grow = Fx.EaseOut(Math.Clamp((c.Age - (I - 0.12f)) / 0.45f, 0f, 1f));
        float env = Fx.Envelope(c.Age, I - 0.12f, I - 0.08f, 1.0f, 1.2f);
        if (env > 0f)
        {
            var pts = new List<Vector3>();
            int n = 14;
            int shown = (int)MathF.Ceiling(n * grow);
            for (int i = 0; i <= shown; i++)
            {
                float u = i / (float)n;
                pts.Add(c.ToFeet + new Vector3(MathF.Sin(u * 11f) * h * 0.35f * (0.3f + u), h * 1.25f * u, 0.15f));
            }
            if (pts.Count >= 2)
            {
                fx.Ribbon(pts.ToArray(), h * 0.14f, Fx.Fade(Rgb(70, 160, 70), env), FxBlend.Alpha);
                for (int i = 2; i < pts.Count; i += 3) fx.Sprite(FxShape.Leaf, pts[i], h * 0.22f, Fx.Fade(col.Main, env), FxBlend.Alpha, i * 0.8f);
            }
            for (int i = 0; i < 3; i++)
            {
                float u = (c.Age * 1.1f + i * 0.33f) % 1f;
                fx.Sprite(FxShape.Drop, c.ToFeet + new Vector3((i - 1) * h * 0.4f, h * (0.8f - 0.7f * u), 0.2f), h * 0.07f, Fx.Fade(Rgb(236, 200, 80), env * (1f - u)), FxBlend.Alpha, MathF.PI);
            }
        }
        if (c.Cue.Landed) Jolt(fx, c.Age, I, 0.4f, 0.25f);
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Freezy Frost: a cloud of fine snow swirls up between the two sides, white ice flowers spinning in it, and every stat change is wiped away in the white.</summary>
    private static void FreezyFrostY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.7f, 0.95f);
        if (env > 0f)
        {
            L1Spiral(fx, c.Age, c.Seed, c.ToFeet, h, 0f, 0.95f, 12, FxShape.Snowflake, White, col.Light, 0.12f, 0.8f, 1.1f);
            fx.Sprite(FxShape.Smoke, c.To, h * 1.0f * env, Fx.Fade(Rgb(220, 244, 255, 200), env * 0.55f), FxBlend.Alpha, c.Age * 2f);
        }
        Fx.Stream(fx, c, 0f, 0.4f, 0.3f, 8, FxShape.Snowflake, White, col.Light, 0.12f, spread: 0.4f, spin: 5f, blend: FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.45f, h * 1.4f, Fx.Fade(White, 0.9f));
            GroundRing(fx, c.Age, c.ToFeet, I, 0.45f, h * 1.3f, col.Light);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 8, FxShape.Star, White, col.Light, 0.1f, 1.0f, 0.3f, FxBlend.Alpha, spin: 4f, salt: 382);
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Sparkly Swirl: a spiral of pink sparkles winds up round the foe and all its stars are flung out in a burst, a chime of glitter drifting down.</summary>
    private static void SparklySwirlY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        L1Spiral(fx, c.Age, c.Seed, c.ToFeet, h, 0f, 0.9f, 14, FxShape.Star, Rgb(255, 180, 230), White, 0.1f, 0.85f, 1.15f);
        L1Spiral(fx, c.Age, c.Seed + 5, c.ToFeet, h, 0.05f, 0.9f, 8, FxShape.Heart, Rgb(255, 150, 210), Rgb(255, 220, 240), 0.08f, 0.6f, 1.0f);
        Fx.Stream(fx, c, 0f, 0.4f, 0.28f, 6, FxShape.Spark, White, col.Light, 0.1f, spread: 0.3f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.8f, 12, FxShape.Star, col.Light, White, 0.12f, 1.2f, 0.2f, FxBlend.Alpha, spin: 6f, salt: 383);
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.4f, h * 1.4f, Fx.Fade(col.Light, 0.9f));
            L4Drift(fx, c.Age, c.Seed, c.ToFeet + new Vector3(0f, h * 1.1f, 0f), h, I, I + 0.8f, 6, FxShape.Spark, White, col.Light, 0.07f, 0.7f, -0.9f);
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Veevee Volley: the user bounds in with three quick paw-swats and a ribbon of hearts, each swat a small pop of pink and white.</summary>
    private static void VeeveeVolleyY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        for (int i = 0; i < 3; i++)
        {
            float t = I - 0.2f + i * 0.1f;
            Strike(c, fx, FxShape.Palm, new Vector3((i % 2 == 0 ? -0.9f : 0.9f), 0.3f * (i - 1), 0f), t, 0.3f, Rgb(250, 226, 190), i * 0.5f - 0.5f, 0.08f);
            if (c.Cue.Landed) Fx.Contact(fx, c.Age, c.Seed + i, c.To + new Vector3(Fx.Signed(c.Seed, i, 384) * h * 0.2f, Fx.Signed(c.Seed, i, 385) * h * 0.2f, 0f), h, t, 0.2f, col.Main, col.Light);
        }
        Fx.Stream(fx, c, 0f, 0.45f, 0.25f, 6, FxShape.Heart, Rgb(255, 150, 190), Rgb(255, 220, 230), 0.12f, arc: 0.05f, spread: 0.3f, blend: FxBlend.Alpha);
        if (c.Cue.Landed) Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 6, FxShape.Heart, Rgb(255, 150, 190), White, 0.1f, 0.9f, 0.1f, FxBlend.Alpha, salt: 386);
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Double Iron Bash: a steel fist comes down on the foe, and a second follows it from below, a ring of grey light ringing out from each.</summary>
    private static void DoubleIronBashY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var steel = Rgb(206, 216, 232);
        DropOn(c, fx, FxShape.Fist, 0.9f, 0f, I - 0.12f, 0.6f, steel, 0f);
        Strike(c, fx, FxShape.Fist, new Vector3(0f, -1.0f, 0f), I, 0.6f, steel, MathF.PI, 0.12f);
        if (c.Cue.Landed)
        {
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I - 0.12f, 0.4f, h * 1.2f, Fx.Fade(col.Light, 0.9f));
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.4f, h * 1.5f, Fx.Fade(White, 0.9f));
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 7, FxShape.Spark, White, steel, 0.1f, 1.0f, salt: 387);
            Jolt(fx, c.Age, I - 0.12f, 0.4f, 0.2f);
            Jolt(fx, c.Age, I, 0.7f, 0.3f);
        }
        Impact(c, col, fx, 1.1f);
    }
}
