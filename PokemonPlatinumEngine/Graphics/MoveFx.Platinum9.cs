using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

// Plan 25 - Y10, its second part (Y10b), first half: the moves the story shows nowhere (a tutor's, a shop's TM, bred,
// a legendary's own) from Comet Punch to Arm Thrust, each a look of its own. The beats are the original's, read from
// each move's animation script; the shapes, colours and timings are our own. Every recipe lands at ImpactTime
// (FxList.Land, which Impact sets), is over by EffectTime and stays under MaxQuads. What is the body's own and not the
// effects' (Transform's pixelating of the user's sprite, Nightmare's and Powder Snow's changed backgrounds, Sky
// Attack's blackened stage, Role Play's silhouettes, the flailing of Struggle's user) is not drawn here.
internal static partial class MoveFx
{
    private static Dictionary<string, FxRecipe> Platinum9() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Comet Punch"] = CometPunchY10b,
        ["Pay Day"] = PayDayY10b,
        ["Vise Grip"] = ViseGripY10b,
        ["Rolling Kick"] = RollingKickY10b,
        ["Twineedle"] = TwineedleY10b,
        ["Sleep Powder"] = SleepPowderY10b,
        ["Sludge"] = SludgeY10b,
        ["Bone Club"] = BoneClubY10b,
        ["Spike Cannon"] = SpikeCannonY10b,
        ["Barrage"] = BarrageY10b,
        ["Lovely Kiss"] = LovelyKissY10b,
        ["Sky Attack"] = SkyAttackY10b,
        ["Transform"] = TransformY10b,
        ["Bonemerang"] = BonemerangY10b,
        ["Sharpen"] = SharpenY10b,
        ["Conversion"] = ConversionY10b,
        ["Struggle"] = StruggleY10b,
        ["Sketch"] = SketchY10b,
        ["Spider Web"] = SpiderWebY10b,
        ["Nightmare"] = NightmareY10b,
        ["Aeroblast"] = AeroblastY10b,
        ["Powder Snow"] = PowderSnowY10b,
        ["Zap Cannon"] = ZapCannonY10b,
        ["Milk Drink"] = MilkDrinkY10b,
        ["Sacred Fire"] = SacredFireY10b,
        ["Morning Sun"] = MorningSunY10b,
        ["Flatter"] = FlatterY10b,
        ["Smelling Salts"] = SmellingSaltsY10b,
        ["Nature Power"] = NaturePowerY10b,
        ["Role Play"] = RolePlayY10b,
        ["Magic Coat"] = MagicCoatY10b,
        ["Arm Thrust"] = ArmThrustY10b
    };

    // ------------------------------------------------------------------ shared by the batch

    /// <summary>A bone flung from <paramref name="from"/> to <paramref name="to"/> between <paramref name="t0"/> and <paramref name="t1"/>, turning end over end.</summary>
    private static void FlungBone(FxList fx, float age, Vector3 from, Vector3 to, float t0, float t1, float len, float arc, Color color)
    {
        float k = (age - t0) / (t1 - t0);
        if (k < 0f || k > 1f) return;
        var p = Lob(from, to, k, arc);
        Bone(fx, p, k * 14f, len * 1.7f, color, 1f);
        fx.Sprite(FxShape.Glow, p, len * 3.2f, Fx.Fade(White, 0.3f));
    }

    /// <summary>A mist of <paramref name="count"/> puffs drifting on a heading from <paramref name="at"/>, in <paramref name="color"/>, between <paramref name="t0"/> and <paramref name="t1"/>.</summary>
    private static void Haze(FxList fx, float age, int seed, Vector3 at, float h, float t0, float t1, int count, Color color, float size)
    {
        float env = Fx.Envelope(age, t0, t0 + 0.15f, t1 - 0.2f, t1);
        if (env <= 0f) return;
        for (int i = 0; i < count; i++)
        {
            float u = (age * 0.6f + Fx.Rand(seed, i, 420)) % 1f;
            var p = at + new Vector3(Fx.Signed(seed, i, 421) * h * 0.55f + MathF.Sin(u * 5f + i) * h * 0.1f, Fx.Signed(seed, i, 422) * h * 0.4f + u * h * 0.2f, 0.1f);
            fx.Sprite(FxShape.Smoke, p, h * size * (0.8f + 0.4f * u), Fx.Fade(color, env * 0.55f * MathF.Sin(u * MathF.PI)), FxBlend.Alpha);
        }
    }

    // ------------------------------------------------------------------ the recipes

    /// <summary>Comet Punch: four small fists in a row, each trailing a star, flying in one after another.</summary>
    private static void CometPunchY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Jabs(c, fx, 4, 0.06f, FxShape.Fist, Rgb(255, 214, 170));
        if (c.Cue.Landed)
            for (int i = 0; i < 4; i++)
            {
                float at = I - (3 - i) * 0.06f;
                float k = (c.Age - (at - 0.1f)) / 0.2f;
                if (k < 0f || k > 1f) continue;
                var p = c.To + new Vector3(-h * (0.8f - k * 0.6f), h * (i % 2 == 0 ? 0.2f : -0.1f), 0.1f);
                fx.Sprite(FxShape.Star, p, h * 0.18f, Fx.Fade(Rgb(255, 236, 140), 1f - k), FxBlend.Alpha, k * 8f);
                fx.Streak(FxShape.Streak, p, Vector3.UnitX, h * 0.05f, 5f, Fx.Fade(White, 1f - k));
                Jolt(fx, c.Age, at, 0.12f, 0.1f);
            }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Pay Day: gold coins are flung across in a spray and rain down on the target, ringing, a glitter of them bouncing off.</summary>
    private static void PayDayY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var gold = Rgb(255, 214, 70);
        Fx.Stream(fx, c, 0f, I + 0.05f, 0.3f, 9, FxShape.Orb, gold, Rgb(255, 244, 170), 0.16f, arc: 0.1f, spread: 0.45f, spin: 8f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0f, I + 0.05f, 0.3f, 9, FxShape.Spark, White, gold, 0.08f, arc: 0.1f, spread: 0.45f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 9, FxShape.Orb, gold, Rgb(255, 244, 170), 0.12f, 0.9f, 2.0f, FxBlend.Alpha, spin: 9f, salt: 220);
            PulseRing(fx, c.Age, c.To, I, 0.35f, h * 0.2f, h * 1.1f, gold, White);
            Jolt(fx, c.Age, I, 0.25f, 0.18f);
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Vise Grip: two jaws of steel teeth close on the target from either side and squeeze, rings tightening.</summary>
    private static void ViseGripY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var steel = Rgb(206, 214, 228);
        float k = Fx.EaseOut(Math.Clamp((c.Age - 0.02f) / 0.28f, 0f, 1f));
        float fade = Fx.Envelope(c.Age, 0f, 0.04f, I + 0.2f, I + 0.4f);
        if (fade > 0f)
            for (int s = -1; s <= 1; s += 2)
            {
                float x = s * h * (0.95f - 0.62f * k + (c.Age > I ? -0.04f : 0f));
                fx.Streak(FxShape.Beam, c.To + new Vector3(x, 0f, 0.1f), Vector3.UnitY, h * 0.22f, 2.6f, Fx.Fade(steel, fade), FxBlend.Alpha);
                for (int t = -2; t <= 2; t++)
                    fx.Sprite(FxShape.Fang, c.To + new Vector3(x - s * h * 0.12f, t * h * 0.2f, 0.12f), h * 0.2f, Fx.Fade(White, fade), FxBlend.Alpha, s < 0 ? -MathF.PI / 2f : MathF.PI / 2f);
            }
        if (c.Cue.Landed)
        {
            PulseRing(fx, c.Age, c.To, I, 0.4f, h * 1.1f, h * 0.4f, Rgb(255, 214, 160), White);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.4f, 5, FxShape.Spark, White, steel, 0.08f, 0.9f, 0.2f, salt: 221);
            Jolt(fx, c.Age, I, 0.35f, 0.24f);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Rolling Kick: the user spins into the target, a whirl of dust and speed round it, and a foot comes round in a wide arc.</summary>
    private static void RollingKickY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float spin = Fx.Envelope(c.Age, 0f, 0.05f, 0.26f, 0.36f);
        if (spin > 0f) fx.Sprite(FxShape.Swirl, c.From, c.FromSize * 1.2f, Fx.Fade(Rgb(255, 236, 214), spin * 0.7f), FxBlend.Alpha, c.Age * 18f);
        Dust(c, fx, c.FromFeet, c.FromSize, 0.02f, 6);
        SpeedLines(c, fx, 0.08f, 0.34f, 8, 0.9f);
        Crescent(fx, c.Age, c.To + new Vector3(-h * 0.8f, -h * 0.2f, 0.2f), c.To + new Vector3(h * 0.3f, h * 0.2f, 0.2f), I - 0.16f, 0.18f, h * 0.9f, h * 1.2f, Rgb(255, 196, 140), White);
        Strike(c, fx, FxShape.Foot, new Vector3(-0.9f, -0.2f, 0.25f), I, 0.8f, Rgb(236, 170, 110), 0.5f, 0.14f);
        if (c.Cue.Landed) Jolt(fx, c.Age, I, 0.4f, 0.24f);
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Twineedle: two thin needles are thrown across, twice, and stand in the target, which flushes with poison.</summary>
    private static void TwineedleY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var purple = Rgb(176, 90, 220);
        for (int v = 0; v < 2; v++)
            for (int n = 0; n < 2; n++)
            {
                float land = I - (1 - v) * 0.1f;
                float k = (c.Age - (land - 0.18f)) / 0.18f;
                var from = c.From + new Vector3(0f, (n - 0.5f) * c.FromSize * 0.25f, 0f);
                var to = c.Aim + new Vector3(0f, (n - 0.5f) * h * 0.3f + v * h * 0.1f, 0.1f);
                Needle(fx, from, to, k, h * 1.4f, Rgb(240, 236, 200), 0.04f, 3.4f);
                Needle(fx, from, to, k, h * 0.7f, purple, 0.04f, 3.4f);
                if (c.Cue.Landed && c.Age > land)
                {
                    float stay = Fx.Envelope(c.Age, land, land + 0.03f, I + 0.35f, I + 0.6f);
                    if (stay > 0f) fx.Streak(FxShape.Streak, to, new Vector3(-1f, 0.2f, 0f), h * 0.07f, 3.2f, Fx.Fade(Rgb(240, 236, 200), stay), FxBlend.Alpha);
                }
            }
        if (c.Cue.Landed)
        {
            Tint(c, fx, I, 0.95f, purple, 0.4f);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 5, FxShape.Bubble, purple, Rgb(226, 170, 255), 0.1f, 0.7f, -0.3f, FxBlend.Alpha, salt: 222);
            Jolt(fx, c.Age, I, 0.2f, 0.14f);
        }
        Impact(c, col, fx, 0.85f);
    }

    /// <summary>Sleep Powder: a drift of pale blue-green powder sifts down over the target, which is wreathed in it, Z's rising.</summary>
    private static void SleepPowderY10b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        var powder = Rgb(170, 230, 200);
        Fx.Rain(fx, c.Age, c.Seed, c.To, h, 0f, 0.9f, 0.5f, 22, FxShape.Spore, powder, Rgb(220, 255, 240), 0.2f, FxBlend.Alpha);
        Haze(fx, c.Age, c.Seed, c.To, h, 0.1f, 1.15f, 6, powder, 0.5f);
        Rising(fx, c.Age, c.Seed, c.ToFeet + Vector3.UnitY * h * 0.6f, h, 0.45f, 1.2f, 3, FxShape.Zzz, Rgb(190, 214, 255), 0.17f, 0.25f);
        Tint(c, fx, 0.3f, 1.1f, powder, 0.4f);
    }

    /// <summary>Sludge: a heavy ball of purple filth is lobbed across and splatters over the target, thick drips running down it.</summary>
    private static void SludgeY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var muck = Rgb(140, 60, 170);
        Ball(c, fx, FxShape.Dot, 0.02f, I, 0.5f, 0.1f, muck, Rgb(210, 140, 240));
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 10, FxShape.Dot, muck, Rgb(210, 140, 240), 0.15f, 1.0f, 1.2f, FxBlend.Alpha, salt: 223);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.8f, 4, FxShape.Smoke, Rgb(120, 60, 150, 210), Rgb(170, 110, 200, 200), 0.24f, 0.5f, -0.2f, FxBlend.Alpha, salt: 224);
            Drips(fx, c.Age, c.Seed, c.To + Vector3.UnitY * h * 0.3f, h, I + 0.04f, 6, muck);
            Tint(c, fx, I, 1.0f, muck, 0.4f);
            Jolt(fx, c.Age, I, 0.3f, 0.2f);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Bone Club: a long bone is swung round in an arc and brought down on the target, a thud of dust and stars.</summary>
    private static void BoneClubY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = (c.Age - (I - 0.22f)) / 0.22f;
        if (k >= 0f && c.Age < I + 0.2f)
        {
            float u = Math.Min(1f, k);
            float ang = MathF.PI * (0.9f - 1.1f * Fx.EaseOut(u));
            var pivot = c.To + new Vector3(-h * 0.3f, h * 0.15f, 0.2f);
            var p = pivot + new Vector3(MathF.Cos(ang), MathF.Sin(ang), 0f) * h * 0.55f;
            Bone(fx, p, ang + MathF.PI / 2f, h * 0.8f, Rgb(244, 236, 214), Math.Min(1f, 1f - Math.Max(0f, (c.Age - I) / 0.2f)));
        }
        if (c.Cue.Landed)
        {
            Dust(c, fx, c.ToFeet, h, I, 6);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.45f, 6, FxShape.Star, Rgb(255, 236, 140), White, 0.1f, 1.0f, 0.4f, FxBlend.Alpha, spin: 6f, salt: 225);
            Jolt(fx, c.Age, I, 0.45f, 0.26f);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Spike Cannon: a rapid volley of spikes is fired across from the user, and a clatter of them stands in the target.</summary>
    private static void SpikeCannonY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        for (int i = 0; i < 12; i++)
        {
            float land = I - (11 - i) * 0.018f;
            float k = (c.Age - (land - 0.12f)) / 0.12f;
            var from = c.From + new Vector3(Fx.Signed(c.Seed, i, 423) * c.FromSize * 0.3f, Fx.Signed(c.Seed, i, 424) * c.FromSize * 0.3f, 0f);
            var to = c.Aim + new Vector3(Fx.Signed(c.Seed, i, 425) * h * 0.35f, Fx.Signed(c.Seed, i, 426) * h * 0.35f, 0.1f);
            Needle(fx, from, to, k, h * 1.2f, Rgb(250, 244, 220), 0.0f, 3.0f);
        }
        float muzzle = Fx.Envelope(c.Age, 0f, 0.03f, 0.2f, 0.3f);
        if (muzzle > 0f) fx.Sprite(FxShape.Spark, c.From + new Vector3(c.FromSize * 0.4f, 0f, 0.1f), c.FromSize * 0.4f, Fx.Fade(White, muzzle * 0.8f), rotation: c.Age * 20f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I - 0.1f, 0.5f, 8, FxShape.Spark, White, Rgb(255, 226, 150), 0.08f, 0.9f, 0.3f, salt: 226);
            Jolt(fx, c.Age, I - 0.2f, 0.25f, 0.3f);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Barrage: round balls are lobbed across one after another, each bursting on the target in a ring and a puff.</summary>
    private static void BarrageY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var shell = Rgb(236, 200, 160);
        for (int i = 0; i < 5; i++)
        {
            float land = I - (4 - i) * 0.07f;
            float k = (c.Age - (land - 0.2f)) / 0.2f;
            if (k >= 0f && k <= 1f)
            {
                var p = Lob(c.From, c.Aim + new Vector3(Fx.Signed(c.Seed, i, 427) * h * 0.3f, Fx.Signed(c.Seed, i, 428) * h * 0.25f, 0f), k, 0.06f);
                fx.Sprite(FxShape.Glow, p, h * 0.36f, Fx.Fade(Rgb(255, 230, 170), 0.4f));
                fx.Sprite(FxShape.Orb, p, h * 0.24f, Fx.Fade(shell, 1f), FxBlend.Alpha, k * 10f);
            }
            if (!c.Cue.Landed) continue;
            var at = c.To + new Vector3(Fx.Signed(c.Seed, i, 427) * h * 0.3f, Fx.Signed(c.Seed, i, 428) * h * 0.25f, 0.1f);
            PulseRing(fx, c.Age, at, land, 0.3f, h * 0.1f, h * 0.55f, Rgb(255, 196, 130), White);
            Fx.Burst(fx, c.Seed, c.Age, at, h, land, 0.35f, 4, FxShape.Smoke, Rgb(220, 200, 180, 200), Rgb(170, 150, 130, 200), 0.14f, 0.5f, -0.2f, FxBlend.Alpha, salt: 227 + i);
            Jolt(fx, c.Age, land, 0.18f, 0.12f);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Lovely Kiss: a great pink kiss is blown across and lands on the target, hearts bursting off it, Z's rising as it dozes.</summary>
    private static void LovelyKissY10b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        var pink = Rgb(255, 120, 170);
        float k = (c.Age - 0.02f) / (I - 0.02f);
        if (k >= 0f && k <= 1f)
        {
            var p = Lob(c.From, c.To, k, 0.04f);
            fx.Sprite(FxShape.Glow, p, h * 0.7f, Fx.Fade(pink, 0.4f));
            fx.Sprite(FxShape.Heart, p, h * (0.2f + 0.3f * k), Fx.Fade(pink, 1f), FxBlend.Alpha, MathF.Sin(k * 12f) * 0.3f);
        }
        float land = Fx.Envelope(c.Age, I, I + 0.04f, I + 0.3f, I + 0.55f);
        if (land > 0f)
        {
            fx.Sprite(FxShape.Heart, c.To, h * 0.85f * (0.7f + 0.3f * Fx.EaseOut((c.Age - I) / 0.1f)), Fx.Fade(pink, land * 0.9f), FxBlend.Alpha);
            fx.Sprite(FxShape.Heart, c.To - new Vector3(h * 0.04f, -h * 0.04f, 0f), h * 0.35f, Fx.Fade(White, land * 0.7f), FxBlend.Alpha);
        }
        Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 8, FxShape.Heart, pink, Rgb(255, 200, 220), 0.1f, 1.0f, -0.3f, FxBlend.Alpha, salt: 228);
        Rising(fx, c.Age, c.Seed, c.ToFeet + Vector3.UnitY * h * 0.7f, h, I + 0.1f, 1.2f, 3, FxShape.Zzz, Rgb(190, 214, 255), 0.17f, 0.25f);
    }

    /// <summary>Sky Attack: the user is wrapped in a golden light and streaks across in a long white-gold flare, crashing into the target in a great burst of feathers.</summary>
    private static void SkyAttackY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var gold = Rgb(255, 226, 120);
        float wrap = Fx.Envelope(c.Age, 0f, 0.06f, 0.22f, 0.34f);
        if (wrap > 0f)
        {
            fx.Sprite(FxShape.Glow, c.From, h * 1.3f, Fx.Fade(gold, wrap * 0.6f), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, c.From, h * 0.7f, Fx.Fade(White, wrap * 0.7f));
        }
        Fx.Stream(fx, c, 0.1f, I, 0.22f, 12, FxShape.Feather, White, gold, 0.12f, arc: 0.0f, spread: 0.35f, spin: 6f, blend: FxBlend.Alpha, trail: 0.0f);
        SpeedLines(c, fx, 0.08f, 0.34f, 12, 1.0f);
        if (c.Cue.Landed)
        {
            float t = c.ToSize;
            float k = Fx.EaseOut((c.Age - I) / 0.25f);
            fx.Sprite(FxShape.Star, c.To, t * 1.6f * (0.6f + 0.6f * k), Fx.Fade(Rgb(255, 244, 190), Fx.Envelope(c.Age, I - 0.01f, I, I + 0.08f, I + 0.32f)), FxBlend.Alpha, 0.2f);
            PulseRing(fx, c.Age, c.To, I, 0.5f, t * 0.3f, t * 1.7f, gold, White);
            Fx.Burst(fx, c.Seed, c.Age, c.To, t, I, 0.7f, 10, FxShape.Feather, White, gold, 0.14f, 1.1f, 0.6f, FxBlend.Alpha, spin: 6f, salt: 229);
            Jolt(fx, c.Age, I, 0.7f, 0.34f);
        }
        Impact(c, col, fx, 1.25f);
    }

    /// <summary>Transform: the user breaks into a mosaic of grey-blue tiles that flicker and swirl, and re-forms; a flash of the foe's own light on it.</summary>
    private static void TransformY10b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var tile = Rgb(170, 190, 230);
        float env = Fx.Envelope(c.Age, 0.02f, 0.1f, 0.6f, 0.9f);
        if (env > 0f)
            for (int i = 0; i < 16; i++)
            {
                float a = Fx.Rand(c.Seed, i, 430) * MathF.Tau + c.Age * (3f + 4f * Fx.Rand(c.Seed, i, 431));
                float r = h * (0.15f + 0.55f * Fx.Rand(c.Seed, i, 432));
                var p = c.From + new Vector3(MathF.Cos(a) * r, MathF.Sin(a) * r * 0.9f, 0.1f);
                float flick = Fx.Rand(c.Seed, i + (int)(c.Age * 18f) * 7, 433) > 0.35f ? 1f : 0.2f;
                fx.Sprite(FxShape.Hex, p, h * 0.14f, Fx.Fade(i % 3 == 0 ? Rgb(255, 255, 255) : tile, env * flick), FxBlend.Alpha, a);
            }
        float flash = Fx.Envelope(c.Age, 0.5f, 0.56f, 0.64f, 0.85f);
        if (flash > 0f) fx.Sprite(FxShape.Glow, c.From, h * 1.2f, Fx.Fade(White, flash * 0.7f), FxBlend.Alpha);
        PulseRing(fx, c.Age, c.From, 0.54f, 0.4f, h * 0.3f, h * 1.2f, tile, White);
        // A thread of light from the foe, copied
        float k = (c.Age - 0.1f) / 0.4f;
        if (k >= 0f && k <= 1f)
            fx.Sprite(FxShape.Orb, Lob(c.To, c.From, Fx.EaseOut(k), 0.05f), h * 0.2f, Fx.Fade(Rgb(255, 240, 255), 1f - k * 0.5f), FxBlend.Alpha);
    }

    /// <summary>Bonemerang: a bone is thrown across, strikes the target and spins back to the user's hand, striking it again on the way.</summary>
    private static void BonemerangY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var bone = Rgb(244, 236, 214);
        FlungBone(fx, c.Age, c.From, c.To, 0.02f, I, h * 0.35f, 0.06f, bone);
        FlungBone(fx, c.Age, c.To, c.From, I + 0.04f, I + 0.38f, h * 0.35f, -0.06f, bone);
        if (c.Cue.Landed)
        {
            Fx.Contact(fx, c.Age, c.Seed + 4, c.To, h, I + 0.3f, 0.28f, Rgb(255, 230, 190), White);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I + 0.3f, 0.4f, 5, FxShape.Star, Rgb(255, 236, 140), White, 0.09f, 1.0f, 0.3f, FxBlend.Alpha, salt: 230);
            Jolt(fx, c.Age, I, 0.3f, 0.2f);
            Jolt(fx, c.Age, I + 0.3f, 0.3f, 0.2f);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Sharpen: the user's edges sharpen, a ring of white blades spinning out from it and glints running over its outline.</summary>
    private static void SharpenY10b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0.05f, 0.2f, 0.7f, 1.0f);
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f + c.Age * 5f;
            var p = c.From + new Vector3(MathF.Cos(a) * h * 0.65f, MathF.Sin(a) * h * 0.55f, 0.1f);
            fx.Sprite(FxShape.Shard, p, h * 0.2f, Fx.Fade(Rgb(236, 244, 255), env), FxBlend.Alpha, a - MathF.PI / 2f);
        }
        for (int i = 0; i < 6; i++)
            Fx.Glint(fx, c.Age, c.From + new Vector3(Fx.Signed(c.Seed, i, 434) * h * 0.5f, Fx.Signed(c.Seed, i, 435) * h * 0.5f, 0.1f), h, 0.1f + i * 0.12f, White, 0.35f);
        PulseRing(fx, c.Age, c.From, 0.1f, 0.5f, h * 0.3f, h * 1.1f, Rgb(200, 220, 255), White);
    }

    /// <summary>Conversion: three hexagons of different colours spin up round the user, one after another, and melt into a single glow of the new type.</summary>
    private static void ConversionY10b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        Color[] hues = { Rgb(255, 110, 90), Rgb(100, 180, 255), Rgb(130, 220, 110), Rgb(255, 216, 90) };
        float env = Fx.Envelope(c.Age, 0.02f, 0.1f, 0.75f, 1.05f);
        for (int i = 0; i < 4; i++)
        {
            float t0 = i * 0.18f;
            float k = (c.Age - t0) / 0.32f;
            if (k < 0f || k > 1f) continue;
            float s = MathF.Sin(k * MathF.PI);
            fx.Sprite(FxShape.Hex, c.From, h * (0.5f + 0.6f * k), Fx.Fade(hues[i], s * 0.8f), FxBlend.Alpha, k * 3f * (i % 2 == 0 ? 1f : -1f));
        }
        float glow = Fx.Envelope(c.Age, 0.65f, 0.78f, 0.95f, 1.15f);
        if (glow > 0f) fx.Sprite(FxShape.Glow, c.From, h * 1.1f, Fx.Fade(Rgb(255, 240, 255), glow * 0.6f), FxBlend.Alpha);
        Fx.Aura(fx, c.Age, c.Seed, c.FromFeet, h, 0.1f, 1.0f, Rgb(200, 190, 255), White, 8);
        if (env <= 0f) return;
    }

    /// <summary>Struggle: the user throws itself at the target in a flailing rush, a weak white flash on the target and a red ring of recoil round the user.</summary>
    private static void StruggleY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Dust(c, fx, c.FromFeet, c.FromSize, 0.02f, 5);
        SpeedLines(c, fx, 0.1f, 0.34f, 5, 0.6f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.4f, 5, FxShape.Star, White, Rgb(255, 236, 200), 0.09f, 0.9f, 0.3f, FxBlend.Alpha, spin: 5f, salt: 231);
            PulseRing(fx, c.Age, c.From, I + 0.1f, 0.4f, c.FromSize * 0.3f, c.FromSize * 1.1f, Rgb(230, 60, 60), Rgb(255, 200, 190));
            Jolt(fx, c.Age, I, 0.3f, 0.2f);
        }
        Impact(c, col, fx, 0.8f);
    }

    /// <summary>Sketch: a pencil line is drawn round the target in loops, the line glowing as it closes, and the copy flashes.</summary>
    private static void SketchY10b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        float k = Math.Clamp((c.Age - 0.04f) / 0.5f, 0f, 1f);
        float fade = Fx.Envelope(c.Age, 0.04f, 0.1f, 0.85f, 1.15f);
        if (fade > 0f)
        {
            const int n = 24;
            var pts = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float s = k * i / (n - 1f);
                float a = s * MathF.Tau * 1.6f;
                float r = h * (0.7f - 0.15f * s);
                pts[i] = c.To + new Vector3(MathF.Cos(a) * r, MathF.Sin(a) * r * 0.85f, 0.15f);
            }
            fx.Ribbon(pts, h * 0.14f, Fx.Fade(Rgb(40, 40, 50), fade), FxBlend.Alpha);
            fx.Ribbon(pts, h * 0.06f, Fx.Fade(White, fade * 0.8f));
            fx.Sprite(FxShape.Spark, pts[n - 1], h * 0.16f, Fx.Fade(Rgb(255, 230, 120), fade), rotation: c.Age * 10f);
        }
        float flash = Fx.Envelope(c.Age, 0.55f, 0.6f, 0.7f, 0.95f);
        if (flash > 0f)
        {
            fx.Sprite(FxShape.Glow, c.To, h * 1.2f, Fx.Fade(White, flash * 0.6f), FxBlend.Alpha);
            PulseRing(fx, c.Age, c.To, 0.58f, 0.4f, h * 0.3f, h * 1.2f, Rgb(200, 210, 240), White);
        }
    }

    /// <summary>Spider Web: white threads are shot across and spread over the target as a web of spokes and rings, binding it.</summary>
    private static void SpiderWebY10b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        Fx.Stream(fx, c, 0f, 0.4f, 0.22f, 8, FxShape.Streak, White, Rgb(220, 226, 236), 0.07f, arc: 0.03f, spread: 0.3f, trail: 5f);
        float k = Fx.EaseOut(Math.Clamp((c.Age - 0.18f) / 0.3f, 0f, 1f));
        float env = Fx.Envelope(c.Age, 0.18f, 0.28f, 0.95f, 1.2f);
        if (env <= 0f) return;
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f;
            var dir = new Vector3(MathF.Cos(a), MathF.Sin(a), 0f);
            fx.Streak(FxShape.Streak, c.To + dir * h * 0.45f * k + new Vector3(0f, 0f, 0.15f), dir, h * 0.035f, 6f * k + 0.1f, Fx.Fade(White, env * 0.9f), FxBlend.Alpha);
        }
        for (int r = 1; r <= 3; r++)
            fx.Sprite(FxShape.Ring, c.To + new Vector3(0f, 0f, 0.15f), h * (0.25f + 0.28f * r) * k, Fx.Fade(Rgb(236, 240, 248), env * 0.85f), FxBlend.Alpha);
        Tint(c, fx, 0.3f, 1.1f, Rgb(210, 216, 230), 0.25f);
    }

    /// <summary>Nightmare: a dark violet swirl shuts round the target, two red eyes opening in it, and it flinches in a bad dream.</summary>
    private static void NightmareY10b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        var violet = Rgb(70, 30, 120);
        float env = Fx.Envelope(c.Age, 0.03f, 0.2f, 0.9f, 1.2f);
        if (env <= 0f) return;
        fx.Sprite(FxShape.Swirl, c.To, h * 1.6f, Fx.Fade(violet, env * 0.7f), FxBlend.Alpha, c.Age * 3f);
        fx.Sprite(FxShape.Swirl, c.To, h * 1.0f, Fx.Fade(Rgb(150, 90, 210), env * 0.5f), FxBlend.Alpha, -c.Age * 4f);
        float open = Fx.EaseOut(Math.Clamp((c.Age - 0.25f) / 0.15f, 0f, 1f));
        for (int s = -1; s <= 1; s += 2)
        {
            var eye = c.ToFeet + new Vector3(s * h * 0.32f, h * 1.0f, 0.2f);
            fx.Sprite(FxShape.Eye, eye, h * 0.3f * open, Fx.Fade(Rgb(255, 60, 70), env), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, eye, h * 0.36f * open, Fx.Fade(Rgb(255, 60, 70), env * 0.5f));
        }
        Rising(fx, c.Age, c.Seed, c.ToFeet + Vector3.UnitY * h * 0.4f, h, 0.2f, 1.2f, 3, FxShape.Zzz, Rgb(170, 130, 230), 0.18f, 0.25f);
        Fx.Burst(fx, c.Seed, c.Age, c.To, h, 0.1f, 0.9f, 6, FxShape.Smoke, Rgb(60, 30, 100, 200), Rgb(110, 70, 160, 200), 0.24f, 0.5f, -0.2f, FxBlend.Alpha, salt: 232);
    }

    /// <summary>Aeroblast: three ribbons of pale cyan air twist round one another and tear across, rings of wind after them, and burst over the target.</summary>
    private static void AeroblastY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var air = Rgb(150, 230, 255);
        for (int i = 0; i < 3; i++)
            SineRibbon(fx, c.Age, c.From, c.Aim, 0.02f + i * 0.02f, 0.5f, i * 2.1f, 0.07f, 2.0f, h * 0.2f, i == 1 ? Rgb(220, 250, 255) : air, White);
        Fx.Waves(fx, c, 0.1f, 0.5f, 3, FxShape.Ring, Rgb(200, 244, 255), 0.45f, 0.24f);
        if (c.Cue.Landed)
        {
            PulseRing(fx, c.Age, c.To, I, 0.5f, h * 0.3f, h * 1.8f, air, White);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 9, FxShape.Streak, White, air, 0.1f, 1.2f, 0f, FxBlend.Add, streak: 4f, salt: 233);
            Jolt(fx, c.Age, I, 0.6f, 0.34f);
        }
        Impact(c, col, fx, 1.2f);
    }

    /// <summary>Powder Snow: a flurry of fine white snow is blown across and settles over the foes, which are frosted white.</summary>
    private static void PowderSnowY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Flurry(c, fx, 0f, 0.6f, 20, 0.07f, 0.5f);
        Fx.Rain(fx, c.Age, c.Seed, c.To, h, 0.15f, 0.9f, 0.4f, 14, FxShape.Dot, White, Rgb(210, 236, 255), 0.05f, FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            Tint(c, fx, I, 1.1f, Rgb(236, 248, 255), 0.5f);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 6, FxShape.Snowflake, White, Rgb(180, 226, 255), 0.1f, 0.8f, 0.4f, FxBlend.Alpha, spin: 4f, salt: 234);
            Jolt(fx, c.Age, I, 0.2f, 0.4f);
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Zap Cannon: a great yellow orb gathers its power, crackling, and is fired across with a trail of lightning, bursting in a ring of bolts.</summary>
    private static void ZapCannonY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var el = ColorsOf(PokemonType.Electric);
        float ball = Fx.Envelope(c.Age, 0f, 0.14f, 0.16f, 0.22f);
        Gather(fx, c.Age, c.Seed, c.From, h, 0f, 0.16f, 8, el.Main, White, 0.08f, 0.9f);
        if (ball > 0f)
        {
            fx.Sprite(FxShape.Glow, c.From, h * 0.9f * ball, Fx.Fade(el.Main, 0.6f));
            fx.Sprite(FxShape.Orb, c.From, h * 0.6f * ball, Fx.Fade(el.Light, 1f), FxBlend.Alpha);
        }
        Fx.Orb(fx, c, 0.16f, I, 0.5f, el.Main, White, FxShape.Spark);
        if (c.Age > 0.16f && c.Age < I) Fx.Bolt(fx, c.Age, c.Seed, c.From, c.Aim, 0.16f, I, h * 0.03f, el.Main, White, 1);
        if (c.Cue.Landed)
        {
            for (int i = 0; i < 4; i++)
                Fx.Bolt(fx, c.Age, c.Seed + i, c.To, c.To + new Vector3(MathF.Cos(i * 1.6f), MathF.Sin(i * 1.6f), 0f) * c.ToSize * 0.9f, I, I + 0.4f, c.ToSize * 0.04f, el.Main, White, 1);
            PulseRing(fx, c.Age, c.To, I, 0.45f, c.ToSize * 0.3f, c.ToSize * 1.5f, el.Main, White);
            Tint(c, fx, I, 0.9f, el.Light, 0.45f);
            Jolt(fx, c.Age, I, 0.55f, 0.34f);
        }
        Impact(c, col, fx, 1.2f);
    }

    /// <summary>Milk Drink: a bottle's worth of white milk swirls up round the user, drops and bubbles rising, and a soft glow mends it.</summary>
    private static void MilkDrinkY10b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var milk = Rgb(250, 248, 244);
        float env = Fx.Envelope(c.Age, 0.03f, 0.2f, 0.8f, 1.1f);
        fx.Sprite(FxShape.Swirl, c.From, h * 1.2f, Fx.Fade(milk, env * 0.6f), FxBlend.Alpha, c.Age * 4f);
        for (int i = 0; i < 8; i++)
        {
            float u = (c.Age * 0.9f + Fx.Rand(c.Seed, i, 436)) % 1f;
            var p = c.FromFeet + new Vector3(Fx.Signed(c.Seed, i, 437) * h * 0.5f, h * (0.1f + 0.9f * u), 0.1f);
            fx.Sprite(i % 2 == 0 ? FxShape.Drop : FxShape.Bubble, p, h * 0.12f, Fx.Fade(milk, env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha);
        }
        Rising(fx, c.Age, c.Seed, c.FromFeet + Vector3.UnitY * h * 0.2f, h, 0.35f, 1.1f, 3, FxShape.Heart, Rgb(255, 170, 200), 0.12f, 0.3f);
        HealOn(fx, c.Age, c.Seed, c.FromFeet, h, 0.15f);
    }

    /// <summary>Sacred Fire: a ring of gold-red flame winds up round the target in a rising spiral, a great sacred flame standing in it with a ring of light.</summary>
    private static void SacredFireY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var fire = ColorsOf(PokemonType.Fire);
        float env = Fx.Envelope(c.Age, 0.04f, 0.18f, I + 0.35f, I + 0.6f);
        if (env > 0f)
            for (int i = 0; i < 14; i++)
            {
                float u = i / 14f;
                float a = u * MathF.Tau * 1.5f + c.Age * 6f;
                var p = c.ToFeet + new Vector3(MathF.Cos(a) * h * (0.55f - 0.2f * u), h * (0.1f + 1.0f * u), MathF.Sin(a) * h * 0.2f);
                fx.Sprite(FxShape.Flame, p, h * 0.28f, Fx.Fade(i % 2 == 0 ? Rgb(255, 196, 90) : fire.Main, env), FxBlend.Add);
            }
        Shaft(fx, c.Age, c.ToFeet, 0.1f, 0.7f, h * 1.2f, h * 0.6f, Fx.Fade(Rgb(255, 150, 60), 0.7f), Rgb(255, 244, 190));
        if (c.Cue.Landed)
        {
            GroundRing(fx, c.Age, c.ToFeet, I, 0.5f, h * 1.9f, Fx.Fade(Rgb(255, 214, 120), 0.9f));
            PulseRing(fx, c.Age, c.To, I, 0.5f, h * 0.3f, h * 1.5f, Rgb(255, 190, 80), White);
            Tint(c, fx, I, 1.1f, Rgb(255, 90, 40), 0.4f);
            Jolt(fx, c.Age, I, 0.45f, 0.3f);
        }
        Impact(c, col, fx, 1.15f);
    }

    /// <summary>Morning Sun: a great gold sun climbs behind the user, spokes of light turning round it, and a warm glow and crosses mend the user.</summary>
    private static void MorningSunY10b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var sun = Rgb(255, 214, 90);
        float env = Fx.Envelope(c.Age, 0f, 0.2f, 0.8f, 1.1f);
        var disc = c.FromFeet + new Vector3(-h * 0.5f, h * (0.6f + 0.55f * Fx.EaseOut(Math.Min(1f, c.Age / 0.5f))), 0f);
        fx.Sprite(FxShape.Glow, disc, h * 1.5f, Fx.Fade(sun, env * 0.5f), FxBlend.Alpha);
        for (int i = 0; i < 10; i++)
        {
            float a = i * MathF.Tau / 10f + c.Age * 1.5f;
            fx.Streak(FxShape.Beam, disc + new Vector3(MathF.Cos(a), MathF.Sin(a), 0f) * h * 0.55f, new Vector3(MathF.Cos(a), MathF.Sin(a), 0f), h * 0.1f, 3f, Fx.Fade(Rgb(255, 236, 160), env * 0.7f), FxBlend.Alpha);
        }
        fx.Sprite(FxShape.Orb, disc, h * 0.5f, Fx.Fade(Rgb(255, 244, 190), env), FxBlend.Alpha);
        Fx.Aura(fx, c.Age, c.Seed, c.FromFeet, h, 0.3f, 1.1f, Rgb(255, 214, 120), White, 10);
        Rising(fx, c.Age, c.Seed, c.FromFeet + Vector3.UnitY * h * 0.2f, h, 0.35f, 1.1f, 4, FxShape.Cross, Rgb(170, 255, 170), 0.12f, 0.3f);
    }

    /// <summary>Flatter: coloured petals and stars shower the target, a pink swirl of praise turns over its head and dizzy stars circle.</summary>
    private static void FlatterY10b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        Fx.Rain(fx, c.Age, c.Seed, c.To, h, 0f, 0.8f, 0.45f, 14, FxShape.Petal, Rgb(255, 150, 190), Rgb(255, 230, 130), 0.12f, FxBlend.Alpha);
        Fx.Stream(fx, c, 0f, 0.4f, 0.3f, 6, FxShape.Star, Rgb(255, 240, 150), White, 0.1f, arc: 0.06f, spread: 0.4f, spin: 6f, blend: FxBlend.Alpha);
        float env = Fx.Envelope(c.Age, 0.25f, 0.4f, 0.9f, 1.2f);
        if (env > 0f)
        {
            var head = c.ToFeet + new Vector3(0f, h * 1.0f, 0.2f);
            fx.Sprite(FxShape.Swirl, head, h * 0.5f, Fx.Fade(Rgb(255, 170, 210), env), FxBlend.Alpha, c.Age * 5f);
            for (int i = 0; i < 4; i++)
            {
                float a = i * MathF.Tau / 4f + c.Age * 6f;
                fx.Sprite(FxShape.Star, head + new Vector3(MathF.Cos(a) * h * 0.4f, MathF.Sin(a) * h * 0.1f, 0f), h * 0.09f, Fx.Fade(Rgb(255, 230, 110), env), FxBlend.Alpha, c.Age * 4f);
            }
        }
        Fx.Glint(fx, c.Age, c.To, h, 0.2f, White, 0.4f);
    }

    /// <summary>Smelling Salts: a little vial is waved under the target's nose, its green-white fumes curling, and a jolt of white shock makes it jump, a mark popping over it.</summary>
    private static void SmellingSaltsY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0.02f, 0.1f, I - 0.02f, I + 0.12f);
        var vial = c.To + new Vector3(-h * 0.55f + MathF.Sin(c.Age * 14f) * h * 0.06f, -h * 0.05f, 0.2f);
        if (env > 0f)
        {
            fx.Sprite(FxShape.Drop, vial, h * 0.3f, Fx.Fade(Rgb(210, 240, 230), env), FxBlend.Alpha, MathF.PI * 0.8f);
            for (int i = 0; i < 5; i++)
            {
                float u = (c.Age * 1.4f + i * 0.2f) % 1f;
                fx.Sprite(FxShape.Smoke, vial + new Vector3(h * 0.2f * u + MathF.Sin(u * 8f + i) * h * 0.05f, h * 0.1f * u, 0f), h * 0.2f * (0.6f + u), Fx.Fade(Rgb(190, 250, 220), env * (1f - u) * 0.8f), FxBlend.Alpha);
            }
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.4f, 6, FxShape.Spark, White, Rgb(255, 244, 150), 0.1f, 0.9f, 0.2f, salt: 235);
            float ex = Fx.Envelope(c.Age, I, I + 0.04f, I + 0.35f, I + 0.55f);
            if (ex > 0f) fx.Sprite(FxShape.Exclaim, c.ToFeet + new Vector3(0f, h * 1.1f, 0.1f), h * 0.22f * Fx.EaseOut(Math.Min(1f, (c.Age - I) / 0.06f)), Fx.Fade(Rgb(255, 214, 90), ex), FxBlend.Alpha);
            Jolt(fx, c.Age, I, 0.35f, 0.24f);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Nature Power: the ground's own things (leaves, sand, drops, stones, ice, light) gather and spiral up round the user and are thrown across at the target.</summary>
    private static void NaturePowerY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var ground = ColorsOfGround(c.Cue.Terrain);
        var shape = c.Cue.Terrain switch
        {
            Battle.Sim.BattleTerrain.Grass => FxShape.Leaf,
            Battle.Sim.BattleTerrain.Water or Battle.Sim.BattleTerrain.Puddle => FxShape.Drop,
            Battle.Sim.BattleTerrain.Mountain or Battle.Sim.BattleTerrain.Cave => FxShape.Rock,
            Battle.Sim.BattleTerrain.Snow or Battle.Sim.BattleTerrain.Ice => FxShape.Snowflake,
            Battle.Sim.BattleTerrain.Special => FxShape.Spark,
            _ => FxShape.Dot
        };
        float env = Fx.Envelope(c.Age, 0f, 0.08f, 0.2f, 0.3f);
        for (int i = 0; i < 10; i++)
        {
            float u = i / 10f;
            float a = u * MathF.Tau * 1.3f + c.Age * 9f;
            var p = c.FromFeet + new Vector3(MathF.Cos(a) * h * (0.7f - 0.3f * u), h * (0.1f + 0.9f * u), MathF.Sin(a) * h * 0.2f);
            fx.Sprite(shape, p, h * 0.16f, Fx.Fade(i % 2 == 0 ? ground.Main : ground.Light, env), FxBlend.Alpha, a);
        }
        Fx.Stream(fx, c, 0.14f, I + 0.05f, 0.2f, 10, shape, ground.Main, ground.Light, 0.14f, arc: 0.05f, spread: 0.4f, spin: 8f, blend: FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            PulseRing(fx, c.Age, c.To, I, 0.45f, c.ToSize * 0.3f, c.ToSize * 1.4f, ground.Main, White);
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.55f, 9, shape, ground.Main, ground.Light, 0.13f, 1.0f, 0.8f, FxBlend.Alpha, spin: 6f, salt: 236);
            Jolt(fx, c.Age, I, 0.4f, 0.26f);
        }
        Impact(c, new FxColors(ground.Main, ground.Light, ground.Dark), fx, 1.0f);
    }

    /// <summary>Role Play: a pale double of the foe steps over the user, two white eyes and a ring round it, and the user takes on its glow.</summary>
    private static void RolePlayY10b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var ghost = Rgb(220, 224, 255);
        float k = (c.Age - 0.04f) / 0.4f;
        if (k >= 0f && k <= 1f)
        {
            var p = Vector3.Lerp(c.To, c.From, Fx.EaseOut(k));
            float s = MathF.Sin(k * MathF.PI);
            fx.Sprite(FxShape.Glow, p, c.ToSize * 1.0f, Fx.Fade(ghost, s * 0.5f), FxBlend.Alpha);
            for (int e = -1; e <= 1; e += 2)
                fx.Sprite(FxShape.Eye, p + new Vector3(e * c.ToSize * 0.2f, c.ToSize * 0.2f, 0.1f), c.ToSize * 0.2f, Fx.Fade(White, s), FxBlend.Alpha);
        }
        float own = Fx.Envelope(c.Age, 0.4f, 0.5f, 0.85f, 1.15f);
        if (own > 0f)
        {
            fx.Sprite(FxShape.Glow, c.From, h * 1.2f, Fx.Fade(ghost, own * 0.5f), FxBlend.Alpha);
            PulseRing(fx, c.Age, c.From, 0.42f, 0.5f, h * 0.3f, h * 1.2f, Rgb(170, 160, 255), White);
            Fx.Aura(fx, c.Age, c.Seed, c.FromFeet, h, 0.45f, 1.1f, Rgb(170, 160, 255), White, 8);
        }
    }

    /// <summary>Magic Coat: a bubble of pink-white hexagonal panels springs up round the user, a bright sheen sweeping across it.</summary>
    private static void MagicCoatY10b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var coat = Rgb(255, 190, 235);
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f;
            var p = c.From + new Vector3(MathF.Cos(a) * h * 0.7f, MathF.Sin(a) * h * 0.6f, 0.1f);
            Panel(fx, c.Age, p, h * 0.38f, 0.03f + i * 0.03f, coat);
        }
        float env = Fx.Envelope(c.Age, 0.1f, 0.25f, 0.85f, 1.15f);
        fx.Sprite(FxShape.Glow, c.From, h * 1.3f, Fx.Fade(coat, env * 0.3f), FxBlend.Alpha);
        float sweep = (c.Age - 0.35f) / 0.4f;
        if (sweep >= 0f && sweep <= 1f)
            fx.Streak(FxShape.Beam, c.From + new Vector3(h * (-0.8f + 1.6f * sweep), 0f, 0.2f), new Vector3(0.4f, 1f, 0f), h * 0.12f, 4f, Fx.Fade(White, MathF.Sin(sweep * MathF.PI)), FxBlend.Alpha);
        Fx.Glint(fx, c.Age, c.From + new Vector3(h * 0.3f, h * 0.3f, 0.1f), h, 0.55f, White, 0.4f);
    }

    /// <summary>Arm Thrust: open palms thrust at the target again and again, alternate sides, a puff at each.</summary>
    private static void ArmThrustY10b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        if (!c.Cue.Missed)
            for (int i = 0; i < 5; i++)
            {
                float at = I - (4 - i) * 0.055f;
                float side = i % 2 == 0 ? -1f : 1f;
                Strike(c, fx, FxShape.Palm, new Vector3(side * 0.9f, 0.1f * (i - 2), 0.2f), at, 0.34f, Rgb(255, 224, 190), side * -0.4f, 0.05f);
                if (i < 4) Fx.Contact(fx, c.Age, c.Seed + i, c.To + new Vector3(side * -0.2f, 0.12f * (i - 2), 0.1f) * h, h, at, 0.13f, Rgb(255, 220, 160), White);
                Jolt(fx, c.Age, at, 0.1f, 0.08f);
            }
        Impact(c, col, fx, 0.9f);
    }
}
