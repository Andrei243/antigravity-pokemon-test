using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

// Plan 25 - Y10, its first part (Y10a): the moves the post-game shows, from the Fuego Ironworks' sea to Stark
// Mountain, each a look of its own. The beats are the original's, read from each move's animation script (what comes
// first, how often it hits, when it lands, who shakes); the shapes, colours and timings are our own. Every recipe
// lands at ImpactTime (FxList.Land, which Impact sets), is over by EffectTime and stays under MaxQuads. What is the
// body's own and not the effects' (Belly Drum's and Slack Off's squashes, Skull Bash's and Superpower's lunges, the
// fades of the bodies and of the stage for Crabhammer, Eruption, Mega Punch, Glare, Healing Wish, Heal Block, Psycho
// Shift and the rest, the background switched for Fissure, Sky Uppercut and Superpower) is not drawn here.
internal static partial class MoveFx
{
    private static Dictionary<string, FxRecipe> Platinum8() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Clamp"] = ClampY10,
        ["Crabhammer"] = CrabhammerY10,
        ["Double Kick"] = DoubleKickY10,
        ["Icicle Spear"] = IcicleSpearY10,
        ["Rapid Spin"] = RapidSpinY10,
        ["Water Spout"] = WaterSpoutY10,
        ["Belly Drum"] = BellyDrumY10,
        ["Covet"] = CovetY10,
        ["Slack Off"] = SlackOffY10,
        ["Superpower"] = SuperpowerY10,
        ["Blaze Kick"] = BlazeKickY10,
        ["Eruption"] = EruptionY10,
        ["Facade"] = FacadeY10,
        ["Seed Bomb"] = SeedBombY10,
        ["Triple Kick"] = TripleKickY10,
        ["Mega Punch"] = MegaPunchY10,
        ["Skull Bash"] = SkullBashY10,
        ["Glare"] = GlareY10,
        ["Healing Wish"] = HealingWishY10,
        ["Lava Plume"] = LavaPlumeY10,
        ["Sky Uppercut"] = SkyUppercutY10,
        ["Cotton Spore"] = CottonSporeY10,
        ["Crush Claw"] = CrushClawY10,
        ["Fissure"] = FissureY10,
        ["Needle Arm"] = NeedleArmY10,
        ["Spikes"] = SpikesY10,
        ["Teeter Dance"] = TeeterDanceY10,
        ["Guillotine"] = GuillotineY10,
        ["Present"] = PresentY10,
        ["Heal Block"] = HealBlockY10,
        ["Hyper Voice"] = HyperVoiceY10,
        ["Psycho Shift"] = PsychoShiftY10
    };

    // ------------------------------------------------------------------ shared by the batch

    /// <summary>A shake of the camera that rises at <paramref name="at"/> and dies away over <paramref name="len"/>.</summary>
    private static void Jolt(FxList fx, float age, float at, float amount, float len = 0.2f) =>
        fx.Shake = Math.Max(fx.Shake, amount * Fx.Envelope(age, at, at + 0.02f, at + len * 0.4f, at + len));

    /// <summary>Spray of drops flung out from <paramref name="at"/>: water, thrown wide and falling.</summary>
    private static void Splash(in FxCtx c, FxList fx, Vector3 at, float h, float t0, Color body, Color glint, int count = 8, int salt = 0)
    {
        Fx.Burst(fx, c.Seed, c.Age, at, h, t0, 0.6f, count, FxShape.Drop, body, glint, 0.1f, 1.0f, 1.6f, FxBlend.Alpha, salt: 180 + salt);
        Fx.Burst(fx, c.Seed, c.Age, at, h, t0, 0.5f, count / 2, FxShape.Bubble, glint, White, 0.08f, 0.8f, 0.4f, FxBlend.Alpha, salt: 190 + salt);
    }

    /// <summary>The two halves of a shell closing on the target from above and below (a clam's), each a scalloped arc with ribs, in <paramref name="color"/>.</summary>
    private static void Shells(in FxCtx c, FxList fx, float t0, float size, Color color, Color rim)
    {
        float k = (c.Age - t0) / 0.3f;
        if (k < 0f || k > 1f) return;
        float close = Fx.EaseOut(Math.Min(1f, k * 1.9f));
        float h = c.ToSize;
        float gap = h * 0.5f * (1f - close);
        float fade = 1f - Math.Max(0f, k - 0.6f) / 0.4f;
        for (int s = -1; s <= 1; s += 2)
        {
            var p = c.To + Vector3.UnitY * s * (gap + h * 0.14f);
            float turn = s > 0 ? -MathF.PI / 2f : MathF.PI / 2f;
            fx.Sprite(FxShape.Wave, p, h * size, Fx.Fade(color, fade), FxBlend.Alpha, turn);
            fx.Sprite(FxShape.Wave, p, h * size * 1.06f, Fx.Fade(rim, fade * 0.5f), rotation: turn);
            for (int r = -2; r <= 2; r++)
                fx.Streak(FxShape.Streak, p + new Vector3(r * h * 0.16f * size, s * h * 0.06f, 0.05f), new Vector3(r * 0.25f, s, 0f), h * 0.03f, 4f, Fx.Fade(rim, fade * 0.8f), FxBlend.Alpha);
        }
    }

    // ------------------------------------------------------------------ the recipes

    /// <summary>Clamp: a pair of shells closes on the target and snaps shut again tighter, bubbles squeezed out.</summary>
    private static void ClampY10(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var shell = Rgb(214, 150, 190);
        Shells(c, fx, I - 0.3f, 1.3f, shell, Rgb(255, 232, 244));
        Shells(c, fx, I - 0.06f, 1.1f, Rgb(190, 130, 180), Rgb(255, 232, 244));
        if (c.Cue.Landed)
        {
            Splash(c, fx, c.To, h, I, Rgb(150, 210, 255), White, 6, 1);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.4f, 5, FxShape.Star, Rgb(255, 250, 200), White, 0.09f, 0.9f, 0.2f, FxBlend.Alpha, spin: 5f, salt: 192);
            Jolt(fx, c.Age, I - 0.17f, 0.18f);
            Jolt(fx, c.Age, I, 0.3f);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Crabhammer: a great teal claw comes down on the target like a hammer, the ground splitting and a spray thrown up.</summary>
    private static void CrabhammerY10(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var teal = Rgb(70, 190, 200);
        // The sea gathers over the user first
        Gather(fx, c.Age, c.Seed, c.From + Vector3.UnitY * c.FromSize * 0.4f, c.FromSize, 0f, 0.2f, 8, teal, White, 0.09f, 0.8f);
        float raise = Fx.Envelope(c.Age, 0.03f, 0.1f, 0.16f, 0.22f);
        if (raise > 0f) fx.Sprite(FxShape.Fist, c.To + new Vector3(-h * 0.25f, h * 0.7f, 0.2f), h * 0.9f, Fx.Fade(Rgb(120, 220, 230), raise * 0.9f), FxBlend.Alpha, 0.3f);
        DropOn(c, fx, FxShape.Fist, 0.7f, -0.25f, I, 1.0f, Rgb(120, 220, 230), 0.3f);
        DropOn(c, fx, FxShape.Claw, 0.7f, -0.25f, I, 0.8f, Rgb(230, 255, 255), 0.3f);
        if (c.Cue.Landed)
        {
            GroundRing(fx, c.Age, c.ToFeet, I, 0.45f, h * 1.7f, Fx.Fade(Rgb(190, 240, 255), 0.9f));
            fx.Flat(FxShape.Crack, c.ToFeet + Vector3.UnitY * 0.04f, Vector3.UnitY, h * 1.1f, Fx.Fade(Rgb(40, 70, 90), Fx.Envelope(c.Age, I, I + 0.03f, 0.75f, 1.0f)), FxBlend.Alpha, c.Seed * 0.1f, depth: true);
            Splash(c, fx, c.ToFeet + Vector3.UnitY * h * 0.15f, h, I, teal, White, 10, 2);
            Jolt(fx, c.Age, I, 0.6f, 0.34f);
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Double Kick: two kicks from either side, ten frames apart, each with a flash.</summary>
    private static void DoubleKickY10(in FxCtx c, FxColors col, FxList fx)
    {
        var tan = Rgb(236, 170, 110);
        if (!c.Cue.Missed)
        {
            Strike(c, fx, FxShape.Foot, new Vector3(-1.0f, -0.15f, 0.2f), I - 0.17f, 0.65f, tan, 0.3f, 0.1f);
            Strike(c, fx, FxShape.Foot, new Vector3(1.0f, 0.1f, 0.2f), I, 0.75f, tan, -0.3f, 0.1f);
            Fx.Contact(fx, c.Age, c.Seed, c.To + new Vector3(-0.1f, -0.05f, 0.1f) * c.ToSize, c.ToSize, I - 0.17f, 0.2f, Rgb(255, 220, 160), White);
            Jolt(fx, c.Age, I - 0.17f, 0.2f, 0.14f);
            Jolt(fx, c.Age, I, 0.3f, 0.2f);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Icicle Spear: four icicles are thrown across one after another and stand in the target, ice shards flying at each.</summary>
    private static void IcicleSpearY10(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var ice = ColorsOf(PokemonType.Ice);
        for (int i = 0; i < 4; i++)
        {
            float land = I - (3 - i) * 0.05f;
            float k = (c.Age - (land - 0.2f)) / 0.2f;
            var from = c.From + new Vector3(0f, c.FromSize * (0.1f + 0.12f * i), 0f);
            var to = c.Aim + new Vector3(Fx.Signed(c.Seed, i, 400) * h * 0.25f, Fx.Signed(c.Seed, i, 401) * h * 0.25f, 0.1f);
            Needle(fx, from, to, k, h * 1.8f, Rgb(120, 200, 255), 0.04f, 3.4f);
            Needle(fx, from, to, k, h * 0.9f, White, 0.04f, 3.4f);
            if (!c.Cue.Landed) continue;
            Fx.Burst(fx, c.Seed, c.Age, to, h, land, 0.4f, 4, FxShape.Shard, ice.Light, White, 0.09f, 0.8f, 0.8f, FxBlend.Alpha, spin: 6f, salt: 193 + i);
            // And the icicle stands where it struck, fading
            float stay = Fx.Envelope(c.Age, land, land + 0.03f, I + 0.3f, I + 0.6f);
            if (stay > 0f)
            {
                fx.Streak(FxShape.Shard, to, new Vector3(-0.6f, 0.4f, 0f), h * 0.2f, 3.2f, Fx.Fade(Rgb(120, 200, 255), stay), FxBlend.Alpha);
                fx.Streak(FxShape.Shard, to, new Vector3(-0.6f, 0.4f, 0f), h * 0.08f, 3.2f, Fx.Fade(White, stay * 0.9f), FxBlend.Alpha);
            }
            Jolt(fx, c.Age, land, i == 3 ? 0.35f : 0.15f, 0.14f);
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Rapid Spin: a whirl of white wind winds up round the user, which rushes in spinning; a tight whirl of dust and sparks at the target.</summary>
    private static void RapidSpinY10(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float wind = Fx.Envelope(c.Age, 0f, 0.05f, 0.18f, 0.3f);
        if (wind > 0f)
        {
            fx.Sprite(FxShape.Swirl, c.From, h * 1.3f, Fx.Fade(Rgb(236, 240, 248), wind * 0.8f), FxBlend.Alpha, c.Age * 16f);
            fx.Sprite(FxShape.Swirl, c.From, h * 0.9f, Fx.Fade(White, wind * 0.6f), rotation: -c.Age * 12f);
        }
        SpeedLines(c, fx, 0.12f, 0.36f, 8, 0.9f);
        if (c.Cue.Landed)
        {
            float spin = Fx.Envelope(c.Age, I, I + 0.04f, I + 0.45f, I + 0.7f);
            if (spin > 0f)
            {
                fx.Sprite(FxShape.Swirl, c.To, c.ToSize * 1.3f, Fx.Fade(Rgb(236, 240, 248), spin * 0.8f), FxBlend.Alpha, c.Age * 18f);
                for (int i = 0; i < 3; i++)
                {
                    float a = i * MathF.Tau / 3f + c.Age * 20f;
                    fx.Sprite(FxShape.Spark, c.To + new Vector3(MathF.Cos(a) * c.ToSize * 0.5f, MathF.Sin(a) * c.ToSize * 0.3f, 0.1f), c.ToSize * 0.1f, Fx.Fade(White, spin), rotation: a);
                }
            }
            Dust(c, fx, c.ToFeet, c.ToSize, I, 8);
            Jolt(fx, c.Age, I, 0.55f, 0.4f);
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Water Spout: a great jet of water is thrown up from the user and falls as heavy rain and a wave on the target.</summary>
    private static void WaterSpoutY10(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var water = ColorsOf(PokemonType.Water);
        Shaft(fx, c.Age, c.FromFeet, 0.03f, 0.55f, c.FromSize * 1.25f, c.FromSize * 0.7f, Fx.Fade(water.Main, 0.9f), White);
        Splash(c, fx, c.FromFeet + Vector3.UnitY * c.FromSize * 0.2f, c.FromSize, 0.05f, water.Main, White, 6, 3);
        Fx.Rain(fx, c.Age, c.Seed, c.To, h, 0.12f, I + 0.25f, 0.2f, 18, FxShape.Drop, water.Main, water.Light, 0.11f, FxBlend.Alpha, 1.4f);
        if (c.Cue.Landed)
        {
            PulseRing(fx, c.Age, c.To, I, 0.45f, h * 0.3f, h * 1.5f, water.Main, White);
            GroundRing(fx, c.Age, c.ToFeet, I, 0.45f, h * 1.9f, Fx.Fade(water.Light, 0.9f));
            Splash(c, fx, c.ToFeet + Vector3.UnitY * h * 0.2f, h, I, water.Main, White, 10, 4);
            Jolt(fx, c.Age, I, 0.5f, 0.3f);
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Belly Drum: the user beats its belly six times, a ring of sound at each beat, and swells with a red glow and anger marks.</summary>
    private static void BellyDrumY10(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var belly = c.FromFeet + new Vector3(0f, h * 0.35f, 0.12f);
        for (int i = 0; i < 6; i++)
        {
            float t0 = 0.05f + i * 0.14f;
            PulseRing(fx, c.Age, belly, t0, 0.3f, h * 0.15f, h * 0.7f, Rgb(255, 214, 120), White);
            Fx.Glint(fx, c.Age, belly, h, t0, White, 0.3f);
            Jolt(fx, c.Age, t0, 0.1f, 0.1f);
        }
        float red = Fx.Envelope(c.Age, 0.2f, 0.6f, 0.95f, 1.2f);
        if (red > 0f) fx.Sprite(FxShape.Glow, c.From, h * 1.0f, Fx.Fade(Rgb(255, 70, 50), red * 0.4f), FxBlend.Alpha);
        Anger(fx, c.Age, c.FromFeet + new Vector3(h * 0.4f, h * 1.1f, 0f), h, 0.7f, 1.2f);
        Fx.Aura(fx, c.Age, c.Seed, c.FromFeet, h, 0.6f, 1.2f, Rgb(255, 90, 60), Rgb(255, 210, 160), 8);
    }

    /// <summary>Covet: the user begs, hearts and stars popping about it, pats the target and a little light is whisked back to it.</summary>
    private static void CovetY10(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var pink = Rgb(255, 150, 190);
        Rising(fx, c.Age, c.Seed, c.FromFeet + Vector3.UnitY * h * 0.3f, h, 0f, 0.5f, 4, FxShape.Heart, pink, 0.13f, 0.35f);
        for (int i = 0; i < 4; i++)
            Fx.Glint(fx, c.Age, c.From + new Vector3(Fx.Signed(c.Seed, i, 402) * h * 0.5f, Fx.Rand(c.Seed, i, 403) * h * 0.5f, 0.1f), h, 0.05f + i * 0.09f, Rgb(255, 244, 190), 0.3f);
        if (!c.Cue.Missed)
        {
            Strike(c, fx, FxShape.Palm, new Vector3(-0.9f, 0.1f, 0.2f), I, 0.32f, Rgb(255, 230, 210), 0.4f, 0.1f);
            float k = (c.Age - I) / 0.4f;
            if (k >= 0f && k <= 1f)
            {
                var p = Lob(c.To, c.From, Fx.EaseOut(k), 0.1f);
                fx.Sprite(FxShape.Glow, p, c.ToSize * 0.5f, Fx.Fade(Rgb(255, 240, 180), 0.6f));
                fx.Sprite(FxShape.Star, p, c.ToSize * 0.2f, Fx.Fade(Rgb(255, 232, 120), 1f), FxBlend.Alpha, k * 8f);
            }
            Jolt(fx, c.Age, I, 0.18f, 0.14f);
        }
        Impact(c, col, fx, 0.8f);
    }

    /// <summary>Slack Off: the user sprawls and goes slack, Z's rising slowly and a soft white light mending it.</summary>
    private static void SlackOffY10(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float glow = Fx.Envelope(c.Age, 0.1f, 0.35f, 0.8f, 1.1f);
        if (glow > 0f) fx.Sprite(FxShape.Glow, c.From, h * 1.0f, Fx.Fade(Rgb(255, 250, 235), glow * 0.45f), FxBlend.Alpha);
        Rising(fx, c.Age, c.Seed, c.FromFeet + Vector3.UnitY * h * 0.7f, h, 0.05f, 1.15f, 3, FxShape.Zzz, Rgb(190, 214, 255), 0.18f, 0.25f);
        Rising(fx, c.Age, c.Seed, c.FromFeet + Vector3.UnitY * h * 0.2f, h, 0.3f, 1.15f, 4, FxShape.Cross, Rgb(170, 255, 170), 0.12f, 0.3f);
        HealOn(fx, c.Age, c.Seed, c.FromFeet, h, 0.15f);
    }

    /// <summary>Superpower: red-gold power is drawn into the user's arms, then a vast fist drives in and the ground bursts.</summary>
    private static void SuperpowerY10(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var power = Rgb(255, 120, 70);
        Gather(fx, c.Age, c.Seed, c.From, c.FromSize, 0f, 0.28f, 12, power, Rgb(255, 224, 150), 0.1f, 1.0f);
        float aura = Fx.Envelope(c.Age, 0f, 0.1f, 0.26f, 0.36f);
        if (aura > 0f) fx.Sprite(FxShape.Glow, c.From, c.FromSize * 1.1f, Fx.Fade(power, aura * 0.5f), FxBlend.Alpha);
        SpeedLines(c, fx, 0.2f, 0.36f, 8, 0.9f);
        Strike(c, fx, FxShape.Fist, new Vector3(-1.3f, 0.2f, 0.3f), I, 1.15f, Rgb(255, 190, 130), 0.4f, 0.12f);
        if (c.Cue.Landed)
        {
            PulseRing(fx, c.Age, c.To, I, 0.5f, h * 0.3f, h * 1.8f, power, White);
            GroundRing(fx, c.Age, c.ToFeet, I + 0.02f, 0.5f, h * 2.0f, Fx.Fade(White, 0.9f));
            fx.Flat(FxShape.Crack, c.ToFeet + Vector3.UnitY * 0.04f, Vector3.UnitY, h * 1.4f, Fx.Fade(Rgb(60, 44, 34), Fx.Envelope(c.Age, I, I + 0.03f, 0.8f, 1.05f)), FxBlend.Alpha, c.Seed * 0.1f, depth: true);
            Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + Vector3.UnitY * h * 0.1f, h, I, 0.6f, 7, FxShape.Rock, Rgb(176, 148, 104), Rgb(120, 98, 68), 0.12f, 1.0f, 2.2f, FxBlend.Alpha, spin: 5f, salt: 194);
            Jolt(fx, c.Age, I, 0.8f, 0.36f);
        }
        Impact(c, col, fx, 1.3f);
    }

    /// <summary>Blaze Kick: a flaming foot swings in from below, trailing fire, and the kick bursts into flame.</summary>
    private static void BlazeKickY10(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var fire = ColorsOf(PokemonType.Fire);
        Fx.Stream(fx, c, 0.05f, I + 0.05f, 0.24f, 10, FxShape.Flame, fire.Main, fire.Light, 0.14f, arc: 0.03f, spread: 0.3f);
        Strike(c, fx, FxShape.Foot, new Vector3(-1.0f, -0.35f, 0.25f), I - 0.04f, 0.85f, Rgb(255, 190, 110), 0.5f, 0.14f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 12, FxShape.Flame, fire.Main, fire.Light, 0.2f, 1.0f, -0.3f, salt: 195);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 6, FxShape.Ember, Rgb(255, 220, 120), White, 0.08f, 1.2f, 0.5f, salt: 196);
            Tint(c, fx, I, 0.9f, Rgb(255, 80, 40), 0.4f);
            Jolt(fx, c.Age, I, 0.4f, 0.26f);
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Eruption: fire fountains up from the user, twice, then a rain of burning rock and lava falls on the target, blast after blast.</summary>
    private static void EruptionY10(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var fire = ColorsOf(PokemonType.Fire);
        foreach (float t0 in new[] { 0f, 0.14f })
        {
            Shaft(fx, c.Age, c.FromFeet, t0, 0.4f, c.FromSize * 1.2f, c.FromSize * 0.65f, Fx.Fade(fire.Main, 0.9f), Rgb(255, 240, 180));
            Fx.Burst(fx, c.Seed, c.Age, c.FromFeet + Vector3.UnitY * c.FromSize * 0.4f, c.FromSize, t0 + 0.04f, 0.5f, 7, FxShape.Flame, fire.Main, fire.Light, 0.14f, 0.9f, 0.6f, salt: 197);
        }
        Fx.Rain(fx, c.Age, c.Seed, c.To, h, 0.12f, I + 0.45f, 0.22f, 12, FxShape.Rock, Rgb(255, 130, 50), Rgb(120, 50, 30), 0.14f);
        Fx.Rain(fx, c.Age, c.Seed + 1, c.To, h, 0.12f, I + 0.45f, 0.22f, 8, FxShape.Flame, fire.Main, fire.Light, 0.16f, FxBlend.Add);
        if (c.Cue.Landed)
        {
            Blast(c, fx, c.To, c.ToFeet, h, I, 12, 4, 1.2f);
            Tint(c, fx, I, 1.1f, Rgb(160, 30, 20), 0.5f);
            for (int i = 0; i < 4; i++) Jolt(fx, c.Age, I + i * 0.09f, 0.4f, 0.14f);
        }
        Impact(c, col, fx, 1.25f);
    }

    /// <summary>Facade: the user flares red, then green, then gold, squashing at each, and rams the target with all its colours, a white star at the blow.</summary>
    private static void FacadeY10(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var tints = new[] { Rgb(255, 80, 70), Rgb(80, 210, 150), Rgb(255, 224, 110) };
        for (int i = 0; i < 3; i++)
        {
            float t0 = i * 0.1f;
            float p = Fx.Envelope(c.Age, t0, t0 + 0.03f, t0 + 0.06f, t0 + 0.12f);
            if (p > 0f) fx.Sprite(FxShape.Glow, c.From, h * 1.1f, Fx.Fade(tints[i], p * 0.5f), FxBlend.Alpha);
            PulseRing(fx, c.Age, c.From, t0, 0.18f, h * 0.4f, h * 1.0f, tints[i], White);
        }
        SpeedLines(c, fx, 0.2f, 0.36f, 8, 0.9f);
        if (c.Cue.Landed)
        {
            float k = Fx.EaseOut((c.Age - I) / 0.2f);
            fx.Sprite(FxShape.Star, c.To, c.ToSize * 1.2f * (0.6f + 0.6f * k), Fx.Fade(White, Fx.Envelope(c.Age, I - 0.01f, I, I + 0.08f, I + 0.3f)), FxBlend.Alpha, 0.2f);
            PulseRing(fx, c.Age, c.To, I, 0.4f, c.ToSize * 0.3f, c.ToSize * 1.5f, Rgb(255, 230, 180), White);
            Jolt(fx, c.Age, I, 0.4f, 0.24f);
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Seed Bomb: five seeds are lobbed across in a string and burst on the target one after another, then burst twice more.</summary>
    private static void SeedBombY10(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var green = Rgb(120, 200, 70);
        for (int i = 0; i < 5; i++)
        {
            float land = I - (4 - i) * 0.035f;
            float k = (c.Age - (land - 0.2f)) / 0.2f;
            if (k >= 0f && k <= 1f)
            {
                var p = Lob(c.From, c.Aim, k, 0.05f);
                fx.Sprite(FxShape.Glow, p, h * 0.38f, Fx.Fade(Rgb(255, 240, 150), 0.5f));
                fx.Sprite(FxShape.Seed, p, h * 0.26f, Fx.Fade(Rgb(150, 110, 60), 1f), FxBlend.Alpha, k * 12f);
            }
            if (c.Cue.Landed)
                Fx.Burst(fx, c.Seed, c.Age, c.To + new Vector3(Fx.Signed(c.Seed, i, 404), Fx.Signed(c.Seed, i, 405), 0f) * h * 0.2f, h, land, 0.3f, 3, FxShape.Spark, Rgb(255, 232, 120), White, 0.08f, 0.8f, 0.2f, salt: 198 + i);
        }
        if (c.Cue.Landed)
        {
            foreach (float at in new[] { I, I + 0.17f, I + 0.33f })
            {
                PulseRing(fx, c.Age, c.To, at, 0.3f, h * 0.2f, h * 1.1f, green, White);
                Fx.Burst(fx, c.Seed, c.Age, c.To, h, at, 0.45f, 5, FxShape.Smoke, Rgb(200, 190, 170, 200), Rgb(150, 140, 120, 200), 0.18f, 0.6f, -0.2f, FxBlend.Alpha, salt: 203);
                Jolt(fx, c.Age, at, 0.25f, 0.14f);
            }
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Triple Kick: three kicks in a row, each harder than the one before, a flash at each.</summary>
    private static void TripleKickY10(in FxCtx c, FxColors col, FxList fx)
    {
        var tan = Rgb(236, 170, 110);
        if (!c.Cue.Missed)
        {
            float[] size = { 0.55f, 0.65f, 0.8f };
            Vector3[] from = { new(-1.0f, -0.2f, 0.2f), new(1.0f, 0.1f, 0.2f), new(-0.7f, 0.5f, 0.25f) };
            for (int i = 0; i < 3; i++)
            {
                float at = I - (2 - i) * 0.1f;
                Strike(c, fx, FxShape.Foot, from[i], at, size[i], tan, (i - 1) * 0.5f, 0.07f);
                if (i < 2) Fx.Contact(fx, c.Age, c.Seed + i, c.To + new Vector3(0.1f * (i - 1), 0.1f * i, 0.1f) * c.ToSize, c.ToSize, at, 0.14f + 0.06f * i, Rgb(255, 220, 160), White);
                Jolt(fx, c.Age, at, 0.12f + 0.12f * i, 0.12f);
            }
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.45f, 7, FxShape.Star, Rgb(255, 230, 150), White, 0.1f, 1.0f, 0.3f, FxBlend.Alpha, spin: 5f, salt: 204);
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Mega Punch: a huge pale fist is driven into the target, rings of force and a white star round the blow.</summary>
    private static void MegaPunchY10(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        SpeedLines(c, fx, 0.1f, 0.34f, 6, 0.8f);
        if (c.Cue.Landed)
        {
            float k = Fx.EaseOut((c.Age - I) / 0.25f);
            fx.Sprite(FxShape.Star, c.To, h * 1.5f * (0.6f + 0.6f * k), Fx.Fade(Rgb(255, 238, 190), 0.55f * Fx.Envelope(c.Age, I - 0.01f, I, I + 0.08f, I + 0.32f)), FxBlend.Alpha, 0.2f);
            for (int i = 0; i < 2; i++) PulseRing(fx, c.Age, c.To, I + i * 0.06f, 0.42f, h * 0.3f, h * (1.5f + 0.5f * i), Rgb(255, 214, 160), White);
            fx.ScreenFlash(White, 0.2f * Fx.Envelope(c.Age, I - 0.01f, I + 0.01f, I + 0.04f, I + 0.16f));
            Jolt(fx, c.Age, I, 0.7f, 0.3f);
        }
        Strike(c, fx, FxShape.Fist, new Vector3(-1.4f, 0.1f, 0.3f), I, 1.15f, Rgb(255, 214, 170), 0.3f, 0.14f);
        Impact(c, col, fx, 1.25f);
    }

    /// <summary>Skull Bash: the user tucks its head, then charges in head first, spinning, and the crown of it cracks the target, stars flying.</summary>
    private static void SkullBashY10(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        Gather(fx, c.Age, c.Seed, c.From + Vector3.UnitY * h * 0.3f, h, 0f, 0.2f, 7, Rgb(240, 240, 250), Rgb(190, 200, 230), 0.08f, 0.7f);
        float spin = Fx.Envelope(c.Age, 0.08f, 0.14f, 0.28f, I);
        if (spin > 0f) fx.Sprite(FxShape.Swirl, c.From, h * 1.1f, Fx.Fade(Rgb(236, 240, 252), spin * 0.7f), FxBlend.Alpha, c.Age * 20f);
        Dust(c, fx, c.FromFeet, h, 0.04f, 6);
        SpeedLines(c, fx, 0.14f, 0.36f, 10, 1.0f);
        if (c.Cue.Landed)
        {
            float h2 = c.ToSize;
            float k = Fx.EaseOut((c.Age - I) / 0.22f);
            fx.Sprite(FxShape.Impact, c.To, h2 * 1.3f * (0.6f + 0.5f * k), Fx.Fade(Rgb(255, 244, 214), Fx.Envelope(c.Age, I - 0.01f, I, I + 0.08f, I + 0.3f)), FxBlend.Alpha, 0.3f);
            fx.Flat(FxShape.Crack, c.ToFeet + Vector3.UnitY * 0.04f, Vector3.UnitY, h2 * 1.0f, Fx.Fade(Rgb(60, 50, 44), Fx.Envelope(c.Age, I, I + 0.03f, 0.7f, 0.95f)), FxBlend.Alpha, c.Seed * 0.1f, depth: true);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h2, I, 0.5f, 7, FxShape.Star, Rgb(255, 236, 140), White, 0.1f, 1.1f, 0.4f, FxBlend.Alpha, spin: 6f, salt: 205);
            Jolt(fx, c.Age, I, 0.7f, 0.3f);
        }
        Impact(c, col, fx, 1.2f);
    }

    /// <summary>Glare: two huge slit eyes open over the target with rays from them, and yellow sparks crawl over the target as it freezes.</summary>
    private static void GlareY10(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        var green = Rgb(200, 240, 80);
        float env = Fx.Envelope(c.Age, 0.06f, 0.2f, 0.7f, 1.0f);
        if (env <= 0f) return;
        float open = Fx.EaseOut(Math.Min(1f, (c.Age - 0.06f) / 0.18f));
        var head = c.ToFeet + new Vector3(0f, h * 0.8f, 0.2f);
        fx.Sprite(FxShape.Glow, head, h * 1.3f, Fx.Fade(Rgb(40, 60, 10), env * 0.4f), FxBlend.Alpha);
        for (int s = -1; s <= 1; s += 2)
        {
            var eye = head + new Vector3(s * h * 0.42f, 0f, 0f);
            fx.Sprite(FxShape.Eye, eye, h * 0.6f * open, Fx.Fade(green, env), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, eye, h * 0.5f, Fx.Fade(green, env * 0.5f));
            fx.Streak(FxShape.Beam, eye, Vector3.UnitY, h * 0.12f, 2.2f * open, Fx.Fade(Rgb(30, 40, 4), env), FxBlend.Alpha);
            for (int j = 0; j < 3; j++)
                fx.Streak(FxShape.Beam, eye, new Vector3(s * 0.6f, -0.3f + j * 0.3f, 0f), h * 0.06f, 5f * open, Fx.Fade(green, env * 0.7f));
        }
        float crawl = Fx.Envelope(c.Age, 0.4f, 0.5f, 0.85f, 1.05f);
        if (crawl > 0f)
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathF.Tau / 6f + c.Age * 5f;
                fx.Sprite(FxShape.Spark, c.To + new Vector3(MathF.Cos(a) * h * 0.45f, MathF.Sin(a * 1.7f) * h * 0.4f, 0.1f), h * 0.09f, Fx.Fade(Rgb(255, 236, 80), crawl), rotation: a);
            }
        Tint(c, fx, 0.4f, 1.0f, Rgb(255, 230, 80), 0.35f);
    }

    /// <summary>Healing Wish: the user is wrapped in white light that rises from it as a column of falling stars, hearts and crosses carrying its life away.</summary>
    private static void HealingWishY10(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var pink = Rgb(255, 190, 220);
        float env = Fx.Envelope(c.Age, 0f, 0.2f, 0.8f, 1.15f);
        fx.Sprite(FxShape.Glow, c.From, h * 1.3f, Fx.Fade(pink, env * 0.5f), FxBlend.Alpha);
        Shaft(fx, c.Age, c.FromFeet, 0.1f, 0.9f, h * 1.3f, h * 0.35f, Fx.Fade(Rgb(255, 230, 245), 0.7f), White);
        Rising(fx, c.Age, c.Seed, c.FromFeet + Vector3.UnitY * h * 0.2f, h, 0.2f, 1.15f, 4, FxShape.Heart, pink, 0.14f, 0.3f);
        for (int i = 0; i < 8; i++)
        {
            float u = (c.Age * 0.8f + Fx.Rand(c.Seed, i, 406)) % 1f;
            var p = c.FromFeet + new Vector3(Fx.Signed(c.Seed, i, 407) * h * 0.55f, h * (1.25f - 1.1f * u), Fx.Signed(c.Seed, i, 408) * h * 0.2f);
            fx.Sprite(FxShape.Star, p, h * 0.1f, Fx.Fade(White, env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha, u * 6f);
        }
        Fx.Glint(fx, c.Age, c.From, h, 0.5f, White, 0.6f);
        Rising(fx, c.Age, c.Seed, c.FromFeet + Vector3.UnitY * h * 0.2f, h, 0.4f, 1.15f, 3, FxShape.Cross, Rgb(170, 255, 190), 0.12f, 0.3f);
    }

    /// <summary>Lava Plume: jets of molten rock are thrown out of the user in a fan, big burning drops arcing across onto the target.</summary>
    private static void LavaPlumeY10(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var fire = ColorsOf(PokemonType.Fire);
        Shaft(fx, c.Age, c.FromFeet, 0f, 0.35f, c.FromSize * 0.9f, c.FromSize * 0.6f, Fx.Fade(fire.Main, 0.8f), Rgb(255, 230, 160));
        for (int k = 0; k < 3; k++)
            Fx.Stream(fx, c, 0.02f + k * 0.03f, I + 0.06f, 0.26f, 7, FxShape.Dot, Rgb(255, 120, 40), Rgb(255, 200, 80), 0.16f, arc: 0.06f + 0.02f * k, spread: 0.55f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0.02f, I + 0.06f, 0.26f, 10, FxShape.Flame, fire.Main, fire.Light, 0.14f, arc: 0.04f, spread: 0.5f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 10, FxShape.Dot, Rgb(255, 120, 40), Rgb(255, 210, 90), 0.14f, 1.0f, 1.5f, FxBlend.Alpha, salt: 206);
            GroundRing(fx, c.Age, c.ToFeet, I, 0.45f, h * 1.8f, Fx.Fade(Rgb(255, 170, 80), 0.9f));
            Tint(c, fx, I, 1.0f, Rgb(255, 70, 30), 0.4f);
            Jolt(fx, c.Age, I, 0.4f, 0.26f);
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Sky Uppercut: streaks of sky race upward behind the target and a fist rises from below, driving it up in a ring of stars.</summary>
    private static void SkyUppercutY10(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float sky = Fx.Envelope(c.Age, 0f, 0.1f, 0.5f, 0.8f);
        for (int i = 0; i < 9; i++)
        {
            float u = (c.Age * 3f + Fx.Rand(c.Seed, i, 409)) % 1f;
            var p = c.ToFeet + new Vector3(Fx.Signed(c.Seed, i, 410) * h * 1.1f, -h * 0.3f + u * h * 2.4f, -0.3f);
            if (sky > 0f) fx.Streak(FxShape.Streak, p, Vector3.UnitY, h * 0.09f, 7f, Fx.Fade(Rgb(190, 230, 255), sky));
        }
        Strike(c, fx, FxShape.Fist, new Vector3(0.1f, -0.9f, 0.3f), I - 0.04f, 1.0f, Rgb(255, 200, 150), -0.4f, 0.14f);
        if (c.Cue.Landed)
        {
            fx.Streak(FxShape.Beam, c.To + Vector3.UnitY * h * 0.5f, Vector3.UnitY, h * 0.3f, 4f, Fx.Fade(White, Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.3f)), FxBlend.Alpha);
            Fx.Burst(fx, c.Seed, c.Age, c.To + Vector3.UnitY * h * 0.3f, h, I, 0.6f, 8, FxShape.Star, Rgb(255, 236, 140), White, 0.1f, 1.1f, -0.2f, FxBlend.Alpha, spin: 6f, salt: 207);
            PulseRing(fx, c.Age, c.To, I, 0.4f, h * 0.3f, h * 1.4f, Rgb(255, 214, 170), White);
            Jolt(fx, c.Age, I, 0.55f, 0.3f);
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Cotton Spore: soft white cotton puffs float across and settle on the target, which is smothered in a pale cloud.</summary>
    private static void CottonSporeY10(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        var cotton = Rgb(250, 250, 244);
        Fx.Stream(fx, c, 0f, 0.7f, 0.5f, 16, FxShape.Smoke, cotton, Rgb(220, 226, 236), 0.2f, arc: 0.1f, spread: 0.5f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0f, 0.7f, 0.5f, 10, FxShape.Spore, White, cotton, 0.1f, arc: 0.08f, spread: 0.45f, blend: FxBlend.Alpha);
        float cloud = Fx.Envelope(c.Age, 0.3f, 0.55f, 0.95f, 1.2f);
        if (cloud > 0f)
        {
            fx.Sprite(FxShape.Smoke, c.To, h * 1.5f, Fx.Fade(cotton, cloud * 0.6f), FxBlend.Alpha);
            for (int i = 0; i < 6; i++)
                fx.Sprite(FxShape.Smoke, c.To + new Vector3(Fx.Signed(c.Seed, i, 411) * h * 0.45f, Fx.Signed(c.Seed, i, 412) * h * 0.4f, 0.1f), h * 0.4f, Fx.Fade(cotton, cloud * 0.8f), FxBlend.Alpha);
        }
    }

    /// <summary>Crush Claw: two heavy slashes of great claws cross the target and the armour of it cracks.</summary>
    private static void CrushClawY10(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var steel = Rgb(236, 230, 222);
        ClawMarks(c, fx, I - 0.2f, 1.25f, steel, White, 0.5f);
        ClawMarks(c, fx, I - 0.04f, 1.25f, steel, White, -0.5f);
        if (c.Cue.Landed)
        {
            fx.Flat(FxShape.Crack, c.To, Vector3.UnitZ, h * 1.0f, Fx.Fade(Rgb(70, 60, 54), Fx.Envelope(c.Age, I, I + 0.03f, 0.7f, 0.95f)), FxBlend.Alpha, c.Seed * 0.1f, depth: false);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.45f, 6, FxShape.Shard, Rgb(214, 208, 200), White, 0.1f, 1.0f, 1.0f, FxBlend.Alpha, spin: 6f, salt: 208);
            Jolt(fx, c.Age, I - 0.2f, 0.2f, 0.14f);
            Jolt(fx, c.Age, I - 0.04f, 0.35f, 0.2f);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Fissure: the ground shakes, cracks race out across it and a black chasm opens under the target and swallows it, dust and rock thrown up.</summary>
    private static void FissureY10(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var feet = c.ToFeet + Vector3.UnitY * 0.04f;
        float open = Fx.EaseOut(Math.Clamp((c.Age - 0.04f) / 0.4f, 0f, 1f));
        float env = Fx.Envelope(c.Age, 0.02f, 0.14f, 0.85f, 1.15f);
        if (env > 0f)
        {
            for (int i = 0; i < 5; i++)
            {
                float rot = i * MathF.PI / 5f + Fx.Signed(c.Seed, i, 413) * 0.3f;
                fx.Flat(FxShape.Crack, feet, Vector3.UnitY, h * 2.4f * open, Fx.Fade(Rgb(24, 16, 14), env), FxBlend.Alpha, rot, depth: true);
            }
            fx.Flat(FxShape.Dot, feet, Vector3.UnitY, h * 1.6f * open, Fx.Fade(Rgb(4, 2, 6), env), FxBlend.Alpha, 0f, depth: true);
            fx.Flat(FxShape.Glow, feet, Vector3.UnitY, h * 2.0f * open, Fx.Fade(Rgb(120, 40, 20), env * 0.4f), FxBlend.Alpha, 0f, depth: true);
        }
        if (!c.Cue.Missed)
        {
            Fx.Burst(fx, c.Seed, c.Age, feet + Vector3.UnitY * h * 0.1f, h, 0.18f, 0.7f, 9, FxShape.Rock, Rgb(176, 148, 104), Rgb(120, 98, 68), 0.12f, 1.0f, 2.2f, FxBlend.Alpha, spin: 5f, salt: 209);
            Dust(c, fx, c.ToFeet, h, 0.15f, 8);
            GroundRing(fx, c.Age, c.ToFeet, 0.12f, 0.5f, h * 2.2f, Fx.Fade(Rgb(230, 210, 170), 0.8f));
            for (int i = 0; i < 4; i++) Jolt(fx, c.Age, 0.1f + i * 0.12f, 0.6f, 0.2f);
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Needle Arm: a thorny arm jabs the target four times in quick succession, green needles flying off each.</summary>
    private static void NeedleArmY10(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var green = Rgb(110, 200, 80);
        Jabs(c, fx, 4, 0.065f, FxShape.Fist, green);
        if (c.Cue.Landed)
            for (int i = 0; i < 4; i++)
            {
                float at = I - (3 - i) * 0.065f;
                for (int j = 0; j < 4; j++)
                {
                    float a = (j + i * 0.4f) * MathF.Tau / 4f + Fx.Signed(c.Seed, i, 414) * 0.3f;
                    var dir = new Vector3(MathF.Cos(a), MathF.Sin(a), 0f);
                    float k = (c.Age - at) / 0.22f;
                    if (k < 0f || k > 1f) continue;
                    fx.Streak(FxShape.Streak, c.To + dir * h * (0.2f + 0.45f * Fx.EaseOut(k)), dir, h * 0.05f, 3f, Fx.Fade(Rgb(190, 250, 120), 1f - k), FxBlend.Alpha);
                }
                Jolt(fx, c.Age, at, i == 3 ? 0.3f : 0.12f, 0.1f);
            }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Spikes: iron caltrops are thrown from the user in arcs and scatter over the ground by the foes, where they lie glinting.</summary>
    private static void SpikesY10(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var iron = Rgb(150, 154, 166);
        var from = c.From + Vector3.UnitY * h * 0.1f;
        for (int i = 0; i < 5; i++)
        {
            float land = 0.17f + i * 0.045f;
            if (i == 4) land = I;
            var spot = c.Foe - Vector3.UnitY * c.FoeSize * 0.5f + new Vector3((i - 2) * c.FoeSize * 0.45f, 0.08f, (i % 2 == 0 ? 0.5f : -0.45f));
            float k = (c.Age - (land - 0.17f)) / 0.17f;
            if (k >= 0f && k <= 1f)
            {
                var p = Lob(from, spot, k, 0.06f);
                fx.Sprite(FxShape.Glow, p, h * 0.34f, Fx.Fade(White, 0.4f));
                fx.Sprite(FxShape.Star, p, h * 0.3f, Fx.Fade(iron, 1f), FxBlend.Alpha, k * 14f);
                fx.Sprite(FxShape.Star, p, h * 0.14f, Fx.Fade(White, 0.8f), rotation: k * 14f);
            }
            float stay = Fx.Envelope(c.Age, land, land + 0.03f, 1.0f, 1.25f);
            if (stay <= 0f) continue;
            float s = c.FoeSize * 0.32f;
            fx.Flat(FxShape.Dot, spot, Vector3.UnitY, s * 1.5f, Fx.Fade(Rgb(40, 40, 52), stay * 0.7f), FxBlend.Alpha, 0f, depth: true);
            fx.Flat(FxShape.Star, spot + Vector3.UnitY * 0.02f, Vector3.UnitY, s * 1.4f, Fx.Fade(Rgb(214, 218, 230), stay), FxBlend.Alpha, i * 0.7f, depth: true);
            fx.Sprite(FxShape.Star, spot + Vector3.UnitY * s * 0.3f, s * 0.9f, Fx.Fade(Rgb(190, 196, 210), stay), FxBlend.Alpha, i * 0.4f);
            Fx.Glint(fx, c.Age, spot + Vector3.UnitY * s * 0.4f, c.FoeSize, land + 0.06f, White, 0.2f);
            Fx.Burst(fx, c.Seed, c.Age, spot + Vector3.UnitY * 0.05f, 0.7f, land, 0.3f, 3, FxShape.Smoke, Rgb(200, 190, 170, 190), Rgb(150, 140, 120, 190), 0.12f, 0.3f, -0.2f, FxBlend.Alpha, salt: 210 + i);
        }
    }

    /// <summary>Teeter Dance: the user sways from side to side with a ring of swirls and notes round it, and giddy stars circle over the foe.</summary>
    private static void TeeterDanceY10(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.7f, 1.0f);
        for (int i = 0; i < 4; i++)
        {
            float a = i * MathF.Tau / 4f + c.Age * 6f * (i % 2 == 0 ? 1f : -1f);
            var p = c.From + new Vector3(MathF.Cos(a) * h * 0.65f + MathF.Sin(c.Age * 9f) * h * 0.15f, MathF.Sin(a) * h * 0.4f, 0.1f);
            fx.Sprite(i % 2 == 0 ? FxShape.Swirl : FxShape.Note, p, h * 0.2f, Fx.Fade(i % 2 == 0 ? Rgb(255, 170, 200) : Rgb(255, 232, 130), env), FxBlend.Alpha, a);
        }
        PulseRing(fx, c.Age, c.From, 0.05f, 0.5f, h * 0.3f, h * 1.0f, Rgb(255, 190, 215), White);
        PulseRing(fx, c.Age, c.From, 0.35f, 0.5f, h * 0.3f, h * 1.0f, Rgb(255, 190, 215), White);
        // And over the foe
        float dizzy = Fx.Envelope(c.Age, 0.4f, 0.5f, 0.95f, 1.15f);
        if (dizzy > 0f)
            for (int i = 0; i < 4; i++)
            {
                float a = i * MathF.Tau / 4f + c.Age * 6f;
                var p = c.ToFeet + new Vector3(MathF.Cos(a) * c.ToSize * 0.3f, c.ToSize * 1.05f, MathF.Sin(a) * c.ToSize * 0.12f);
                fx.Sprite(FxShape.Star, p, c.ToSize * 0.09f, Fx.Fade(Rgb(255, 230, 110), dizzy), FxBlend.Alpha, c.Age * 4f);
            }
    }

    /// <summary>Guillotine: two great pincers close on the target from either side and a white blade of light falls straight through it.</summary>
    private static void GuillotineY10(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var steel = Rgb(220, 228, 240);
        for (int s = -1; s <= 1; s += 2)
        {
            float k = Fx.EaseOut(Math.Clamp((c.Age - 0.02f) / 0.3f, 0f, 1f));
            float fade = Fx.Envelope(c.Age, 0f, 0.04f, I + 0.05f, I + 0.25f);
            if (fade <= 0f) continue;
            var p = c.To + new Vector3(s * h * (1.1f - 0.75f * k), 0f, 0.1f);
            fx.Sprite(FxShape.Claw, p, h * 0.8f, Fx.Fade(steel, fade), FxBlend.Alpha, s < 0 ? 0f : MathF.PI);
            fx.Sprite(FxShape.Claw, p, h * 0.84f, Fx.Fade(White, fade * 0.4f), rotation: s < 0 ? 0f : MathF.PI);
        }
        if (c.Cue.Landed)
        {
            float cut = Fx.Envelope(c.Age, I - 0.02f, I, I + 0.08f, I + 0.3f);
            fx.Streak(FxShape.Beam, c.To, Vector3.UnitY, h * 0.18f, 6f, Fx.Fade(White, cut), FxBlend.Alpha);
            fx.Streak(FxShape.Beam, c.To, Vector3.UnitY, h * 0.06f, 8f, Fx.Fade(White, cut));
            fx.ScreenFlash(White, 0.3f * Fx.Envelope(c.Age, I - 0.01f, I + 0.01f, I + 0.05f, I + 0.2f));
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 8, FxShape.Shard, steel, White, 0.1f, 1.0f, 0.8f, FxBlend.Alpha, spin: 6f, salt: 215);
            Jolt(fx, c.Age, I, 0.6f, 0.3f);
        }
        Impact(c, col, fx, 1.15f);
    }

    /// <summary>Present: a wrapped box is lobbed across turning over, and opens on the target in a burst of confetti and stars.</summary>
    private static void PresentY10(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = (c.Age - 0.02f) / (I - 0.04f);
        if (k >= 0f && k <= 1f)
        {
            var p = Lob(c.From, c.Aim, k, 0.05f);
            float s = c.ToSize * 0.42f;
            fx.Sprite(FxShape.Glow, p, s * 1.6f, Fx.Fade(Rgb(255, 230, 150), 0.4f));
            fx.Sprite(FxShape.Hex, p, s, Fx.Fade(Rgb(220, 60, 70), 1f), FxBlend.Alpha, k * 8f);
            fx.Sprite(FxShape.Cross, p, s * 0.95f, Fx.Fade(Rgb(255, 224, 90), 1f), FxBlend.Alpha, k * 8f);
            fx.Sprite(FxShape.Heart, p + Vector3.UnitY * s * 0.5f, s * 0.4f, Fx.Fade(Rgb(255, 224, 90), 1f), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.8f, 10, FxShape.Star, Rgb(255, 224, 90), Rgb(255, 120, 160), 0.1f, 1.0f, 0.8f, FxBlend.Alpha, spin: 7f, salt: 216);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.8f, 8, FxShape.Petal, Rgb(120, 220, 255), Rgb(150, 255, 160), 0.11f, 0.9f, 1.2f, FxBlend.Alpha, spin: 6f, salt: 217);
            PulseRing(fx, c.Age, c.To, I, 0.4f, h * 0.2f, h * 1.2f, Rgb(255, 214, 120), White);
            Jolt(fx, c.Age, I, 0.3f, 0.2f);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Heal Block: a ring of crimson seals shuts round the target and a heart is struck through with a dark cross over it, a grey pall settling.</summary>
    private static void HealBlockY10(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        var crimson = Rgb(220, 50, 90);
        float env = Fx.Envelope(c.Age, 0.04f, 0.2f, 0.85f, 1.15f);
        if (env <= 0f) return;
        fx.Sprite(FxShape.Glow, c.To, h * 1.2f, Fx.Fade(Rgb(60, 56, 70), env * 0.45f), FxBlend.Alpha);
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f + c.Age * 2f;
            var p = c.To + new Vector3(MathF.Cos(a) * h * 0.7f, MathF.Sin(a) * h * 0.5f, 0.1f);
            Panel(fx, c.Age, p, h * 0.28f, 0.04f + i * 0.04f, crimson);
        }
        var head = c.ToFeet + new Vector3(0f, h * 0.7f, 0.2f);
        float pop = Fx.EaseOut(Math.Min(1f, (c.Age - 0.2f) / 0.15f));
        fx.Sprite(FxShape.Heart, head, h * 0.3f * pop, Fx.Fade(Rgb(255, 130, 160), env), FxBlend.Alpha);
        fx.Sprite(FxShape.Cross, head, h * 0.42f * pop, Fx.Fade(Rgb(36, 30, 44), env), FxBlend.Alpha, MathF.PI / 4f);
        PulseRing(fx, c.Age, c.To, 0.2f, 0.5f, h * 0.3f, h * 1.3f, crimson, Rgb(255, 190, 210));
    }

    /// <summary>Hyper Voice: the user bellows twice, three great arcs of sound rolling out each time and shaking the target (the user's own cry is heard).</summary>
    private static void HyperVoiceY10(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var yellow = Rgb(255, 236, 140);
        foreach (float t0 in new[] { 0.02f, 0.2f })
        {
            Fx.Waves(fx, c, t0, t0 + 0.28f, 3, FxShape.Wave, yellow, 0.5f, 0.2f);
            Fx.Waves(fx, c, t0 + 0.02f, t0 + 0.3f, 3, FxShape.Wave, White, 0.34f, 0.2f);
            float m = Fx.Envelope(c.Age, t0, t0 + 0.03f, t0 + 0.1f, t0 + 0.18f);
            if (m > 0f) fx.Sprite(FxShape.Glow, c.From + new Vector3(h * 0.4f, h * 0.2f, 0.1f), h * 0.6f, Fx.Fade(yellow, m * 0.6f), FxBlend.Alpha);
        }
        Tint(c, fx, 0.15f, I + 0.35f, Rgb(255, 244, 170), 0.25f);
        if (c.Cue.Landed)
        {
            PulseRing(fx, c.Age, c.To, I, 0.4f, c.ToSize * 0.3f, c.ToSize * 1.3f, yellow, White);
            Jolt(fx, c.Age, I - 0.17f, 0.2f, 0.16f);
            Jolt(fx, c.Age, I, 0.35f, 0.24f);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Psycho Shift: the user flares white, a spiral of violet motes is drawn across to the target and it flares violet as they enter it.</summary>
    private static void PsychoShiftY10(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var psy = ColorsOf(PokemonType.Psychic);
        float h = c.FromSize;
        float flare = Fx.Envelope(c.Age, 0f, 0.1f, 0.3f, 0.5f);
        if (flare > 0f) fx.Sprite(FxShape.Glow, c.From, h * 1.0f, Fx.Fade(White, flare * 0.6f), FxBlend.Alpha);
        PulseRing(fx, c.Age, c.From, 0.04f, 0.4f, h * 0.3f, h * 1.1f, psy.Main, White);
        for (int j = 0; j < 3; j++)
        {
            float k = (c.Age - 0.12f - j * 0.07f) / 0.34f;
            if (k < 0f || k > 1f) continue;
            float a = k * 14f + j * 2.1f;
            var p = Vector3.Lerp(c.From, c.To, Fx.EaseOut(k)) + new Vector3(0f, MathF.Sin(a) * h * 0.4f, MathF.Cos(a) * h * 0.2f);
            fx.Sprite(FxShape.Orb, p, h * 0.3f, Fx.Fade(psy.Main, 1f), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, p, h * 0.55f, Fx.Fade(psy.Light, 0.6f));
        }
        Fx.Stream(fx, c, 0.1f, 0.6f, 0.3f, 10, FxShape.Spark, psy.Light, White, 0.08f, arc: 0.05f, spread: 0.4f);
        float arrive = Fx.Envelope(c.Age, 0.42f, 0.5f, 0.85f, 1.1f);
        if (arrive > 0f)
        {
            fx.Sprite(FxShape.Glow, c.To, c.ToSize * 1.1f, Fx.Fade(Rgb(150, 60, 220), arrive * 0.5f), FxBlend.Alpha);
            PulseRing(fx, c.Age, c.To, 0.44f, 0.45f, c.ToSize * 0.3f, c.ToSize * 1.3f, psy.Main, White);
        }
    }
}
