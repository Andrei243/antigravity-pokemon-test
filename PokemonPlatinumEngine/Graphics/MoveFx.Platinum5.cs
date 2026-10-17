using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

// Plan 25 · Y8, its second part (Y8b): the moves the story shows from Route 210's northern half to the Seven Stars
// Restaurant, in MoveSightings' order, each a look of its own. The beats are the original's, read from each move's
// animation script (what comes first, how often it hits, when it lands, who shakes); the shapes, colours and timings
// are our own. Every recipe lands at ImpactTime (FxList.Land, which Impact sets), is over by EffectTime and stays
// under MaxQuads. What is the body's own and not the effects' (Fly's user leaving the field, Memento's user fainting
// away, Wrap's squeeze of the target) is not drawn here.
internal static partial class MoveFx
{
    private static Dictionary<string, FxRecipe> Platinum5() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Air Slash"] = AirSlash,
        ["Ancient Power"] = AncientPower,
        ["Aqua Tail"] = AquaTail,
        ["Assurance"] = Assurance,
        ["Calm Mind"] = CalmMind,
        ["Double Hit"] = DoubleHit,
        ["Dragon Claw"] = DragonClaw,
        ["Hyper Fang"] = HyperFang,
        ["Ingrain"] = Ingrain,
        ["Iron Defense"] = IronDefense,
        ["Mist"] = MistY8b,
        ["Poison Powder"] = PoisonPowder,
        ["Submission"] = Submission,
        ["Vital Throw"] = VitalThrow,
        ["Whirlpool"] = Whirlpool,
        ["Beat Up"] = BeatUp,
        ["Fire Fang"] = FireFang,
        ["Odor Sleuth"] = OdorSleuth,
        ["Kinesis"] = Kinesis,
        ["Metal Sound"] = MetalSound,
        ["Miracle Eye"] = MiracleEye,
        ["Ominous Wind"] = OminousWind,
        ["Psychic"] = PsychicY8b,
        ["Wrap"] = Wrap,
        ["Embargo"] = Embargo,
        ["Gravity"] = GravityY8b,
        ["Psycho Cut"] = PsychoCut,
        ["Torment"] = Torment,
        ["Assist"] = Assist,
        ["Bone Rush"] = BoneRush,
        ["Captivate"] = Captivate,
        ["Drain Punch"] = DrainPunch,
        ["Fly"] = FlyY8b,
        ["Recover"] = Recover,
        ["Snatch"] = Snatch,
        ["Strength"] = Strength,
        ["Ice Beam"] = IceBeamY8b,
        ["Poison Fang"] = PoisonFang,
        ["Poison Jab"] = PoisonJab,
        ["Signal Beam"] = SignalBeam,
        ["Sludge Bomb"] = SludgeBomb,
        ["Tri Attack"] = TriAttack,
        ["Memento"] = Memento,
        ["Drill Peck"] = DrillPeck,
        ["Ice Punch"] = IcePunch,
        ["Gyro Ball"] = GyroBall,
        ["X-Scissor"] = XScissor,
        ["Horn Drill"] = HornDrill,
        ["Last Resort"] = LastResort,
        ["Magnet Rise"] = MagnetRise,
        ["Smog"] = Smog,
        ["Vine Whip"] = VineWhip,
        ["Dream Eater"] = DreamEater,
        ["Grass Whistle"] = GrassWhistle,
        ["Reversal"] = Reversal
    };

    // ------------------------------------------------------------------ shared by the batch

    /// <summary>
    /// A ribbon whipped from <paramref name="from"/> to <paramref name="to"/> between <paramref name="t0"/> and
    /// <paramref name="t0"/> + <paramref name="dur"/>: it bows <paramref name="lift"/> (in the way's length) up and
    /// wriggles by <paramref name="wiggle"/>, its head a <paramref name="tip"/> sprite, and it fades 0.2 s after it lands.
    /// </summary>
    private static void Whip(FxList fx, float age, Vector3 from, Vector3 to, float t0, float dur, float lift, float wiggle, float width,
        Color color, Color core, FxShape tip, float tipSize, float tipTurn = 0f)
    {
        float k = (age - t0) / dur;
        if (k < 0f) return;
        float after = Math.Max(0f, (age - t0 - dur) / 0.2f);
        if (after > 1f) return;
        float head = Math.Min(1f, k);
        var path = to - from;
        float len = path.Length();
        var (across, up) = Fx.Frame(path);
        const int n = 12;
        var pts = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            float s = Math.Max(0f, head - (n - 1 - i) * 0.05f) * (i == n - 1 ? 1f : 1f);
            float amp = MathF.Sin(s * MathF.PI);
            pts[i] = Vector3.Lerp(from, to, Fx.EaseOut(s)) + Vector3.UnitY * lift * len * amp
                + (across * 0.5f + up * 0.5f) * wiggle * len * MathF.Sin(s * 9f + age * 24f) * amp;
        }
        float a = 1f - after;
        fx.Ribbon(pts, width, Fx.Fade(color, 0.9f * a), FxBlend.Alpha);
        fx.Ribbon(pts, width * 0.35f, Fx.Fade(core, 0.9f * a));
        fx.Sprite(tip, pts[n - 1], tipSize, Fx.Fade(color, a), FxBlend.Alpha, tipTurn + age * 3f);
    }

    /// <summary>A bone of half-length <paramref name="len"/> lying along <paramref name="angle"/> at <paramref name="p"/>: a shaft with a knob at each end.</summary>
    private static void Bone(FxList fx, Vector3 p, float angle, float len, Color color, float alpha)
    {
        var dir = new Vector3(MathF.Cos(angle), MathF.Sin(angle), 0f);
        fx.Streak(FxShape.Streak, p, dir, len * 0.2f, 5f, Fx.Fade(color, alpha), FxBlend.Alpha);
        foreach (float s in new[] { -1f, 1f })
            for (int j = -1; j <= 1; j += 2)
                fx.Sprite(FxShape.Dot, p + dir * len * 0.92f * s + new Vector3(-dir.Y, dir.X, 0f) * len * 0.14f * j, len * 0.22f, Fx.Fade(color, alpha), FxBlend.Alpha);
    }

    /// <summary>A crescent of force flying from <paramref name="from"/> to <paramref name="to"/>, its bulge leading, growing as it goes.</summary>
    private static void Crescent(FxList fx, float age, Vector3 from, Vector3 to, float t0, float dur, float size0, float size1, Color color, Color rim)
    {
        float k = (age - t0) / dur;
        if (k < 0f || k > 1.5f) return;
        float u = Math.Min(1f, k);
        float fade = k < 1f ? 1f : Math.Max(0f, 1f - (k - 1f) * 2f);
        var p = Vector3.Lerp(from, to, Fx.EaseOut(u));
        var dir = to - from;
        float ang = ScreenAngle(dir);
        float size = size0 + (size1 - size0) * u;
        fx.Sprite(FxShape.Wave, p, size, Fx.Fade(color, fade), FxBlend.Alpha, ang);
        fx.Sprite(FxShape.Wave, p, size * 1.04f, Fx.Fade(rim, fade * 0.6f), rotation: ang);
        fx.Streak(FxShape.Streak, p - Vector3.Normalize(dir) * size * 0.5f, dir, size * 0.1f, 6f, Fx.Fade(rim, fade * 0.8f));
    }

    /// <summary>Poison dripping from <paramref name="at"/>: drops falling one after another from <paramref name="t0"/>.</summary>
    private static void Drips(FxList fx, float age, int seed, Vector3 at, float h, float t0, int count, Color color)
    {
        for (int i = 0; i < count; i++)
        {
            float k = (age - t0 - i * 0.07f) / 0.4f;
            if (k < 0f || k > 1f) continue;
            var p = at + new Vector3(Fx.Signed(seed, i, 191) * h * 0.4f, -h * 0.7f * k * k, 0.1f);
            fx.Sprite(FxShape.Drop, p, h * 0.07f, Fx.Fade(color, Math.Min(1f, (1f - k) * 3f)), FxBlend.Alpha, MathF.PI);
        }
    }

    // ------------------------------------------------------------------ the recipes

    /// <summary>Air Slash: wind gathers at the user, then one great crescent of air grows as it crosses and cuts the target with a white flare.</summary>
    private static void AirSlash(in FxCtx c, FxColors col, FxList fx)
    {
        var air = ColorsOf(PokemonType.Flying);
        var pale = Rgb(200, 236, 255);
        float h = c.ToSize;
        Gather(fx, c.Age, c.Seed, c.From, c.FromSize, 0f, 0.14f, 9, air.Main, pale, 0.07f, 0.8f);
        Crescent(fx, c.Age, c.From, c.Aim, 0.1f, I - 0.08f, h * 0.3f, h * 1.1f, Rgb(190, 232, 255), White);
        Crescent(fx, c.Age, c.From, c.Aim, 0.17f, I - 0.1f, h * 0.2f, h * 0.7f, Rgb(150, 210, 255), White);
        if (c.Cue.Landed)
        {
            fx.Streak(FxShape.Streak, c.To, Vector3.UnitY, h * 0.1f, 7f, Fx.Fade(White, Fx.Envelope(c.Age, I - 0.01f, I + 0.02f, I + 0.06f, I + 0.25f)));
            Tint(c, fx, I, 0.7f, pale, 0.4f);
            fx.Shake = Math.Max(fx.Shake, 0.22f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.08f, I + 0.22f));
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Ancient Power: rocks rise round the user in a gold-brown glow, are flung in a ring and burst on the target in stones and dust.</summary>
    private static void AncientPower(in FxCtx c, FxColors col, FxList fx)
    {
        var stone = Rgb(172, 136, 88);
        var gold = Rgb(244, 204, 112);
        float h = c.FromSize;
        float glow = Fx.Envelope(c.Age, 0f, 0.08f, 0.2f, 0.36f);
        if (glow > 0f) fx.Sprite(FxShape.Glow, c.From, h * 0.9f, Fx.Fade(gold, glow * 0.4f), FxBlend.Alpha);
        for (int i = 0; i < 6; i++)
        {
            float k = (c.Age - i * 0.025f) / 0.3f;
            if (k < 0f || k > 1f) continue;
            float a = i * MathF.Tau / 6f + c.Age * 5f;
            var p = c.FromFeet + new Vector3(MathF.Cos(a) * h * 0.65f, h * (0.05f + 0.8f * Fx.EaseOut(k)), MathF.Sin(a) * h * 0.25f);
            fx.Sprite(FxShape.Rock, p, h * 0.16f, Fx.Fade(stone, Math.Min(1f, k * 4f) * (1f - k * 0.3f)), FxBlend.Alpha, a);
        }
        Fx.Stream(fx, c, 0.2f, I, 0.15f, 6, FxShape.Rock, stone, gold, 0.2f, arc: 0.03f, spread: 0.45f, spin: 6f, blend: FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.7f, 10, FxShape.Rock, stone, Rgb(120, 92, 60), 0.11f, 1.0f, 2.4f, FxBlend.Alpha, spin: 5f, salt: 96);
            Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + Vector3.UnitY * c.ToSize * 0.15f, c.ToSize, I, 0.8f, 6, FxShape.Smoke, Rgb(214, 190, 150, 210), Rgb(170, 146, 110, 200), 0.22f, 0.6f, -0.2f, FxBlend.Alpha, salt: 97);
            fx.Shake = Math.Max(fx.Shake, 0.38f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.28f));
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Aqua Tail: the user is wound in a coil of water, then a whip of water cracks down over the target.</summary>
    private static void AquaTail(in FxCtx c, FxColors col, FxList fx)
    {
        var water = ColorsOf(PokemonType.Water);
        float h = c.FromSize;
        float coil = Fx.Envelope(c.Age, 0f, 0.06f, 0.18f, 0.3f);
        if (coil > 0f)
        {
            fx.Sprite(FxShape.Swirl, c.FromFeet + new Vector3(-0.3f * h, 0.45f * h, 0f), h * 0.7f, Fx.Fade(water.Main, coil * 0.8f), FxBlend.Alpha, -c.Age * 14f);
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathF.Tau / 6f + c.Age * 16f;
                fx.Sprite(FxShape.Drop, c.FromFeet + new Vector3(-0.3f * h + MathF.Cos(a) * h * 0.4f, h * (0.2f + 0.5f * ((c.Age * 3f + i * 0.17f) % 1f)), MathF.Sin(a) * h * 0.1f), h * 0.07f, Fx.Fade(water.Light, coil), FxBlend.Alpha);
            }
        }
        Whip(fx, c.Age, c.FromFeet + Vector3.UnitY * h * 0.7f, c.Aim, I - 0.2f, 0.2f, 0.1f, 0.03f, c.ToSize * 0.3f, water.Main, White, FxShape.Drop, c.ToSize * 0.4f);
        if (c.Cue.Landed)
        {
            GroundRing(fx, c.Age, c.ToFeet, I, 0.45f, c.ToSize * 1.5f, Fx.Fade(water.Light, 0.9f));
            fx.Shake = Math.Max(fx.Shake, 0.35f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.28f));
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Assurance: a dark fist slammed in, and a second, heavier one right behind it.</summary>
    private static void Assurance(in FxCtx c, FxColors col, FxList fx)
    {
        var dark = ColorsOf(PokemonType.Dark);
        float dim = Fx.Envelope(c.Age, 0f, 0.06f, 0.14f, 0.26f);
        if (dim > 0f) fx.Sprite(FxShape.Glow, c.From, c.FromSize * 0.9f, Fx.Fade(Rgb(60, 30, 90), dim * 0.5f), FxBlend.Alpha);
        Strike(c, fx, FxShape.Fist, new Vector3(-1.3f, 0.5f, 0.1f), I - 0.1f, 0.46f, Rgb(140, 100, 190), -0.4f, 0.2f);
        Strike(c, fx, FxShape.Fist, new Vector3(1.2f, -0.3f, 0.1f), I, 0.58f, Rgb(100, 60, 150), 0.5f, 0.2f);
        if (c.Cue.Landed)
        {
            Fx.Contact(fx, c.Age, c.Seed, c.To + new Vector3(-0.2f, 0.15f, 0f) * c.ToSize, c.ToSize, I - 0.1f, 0.26f, dark.Main, dark.Light);
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.5f, 8, FxShape.Shard, dark.Main, dark.Light, 0.09f, 1.0f, 0.6f, FxBlend.Alpha, spin: 8f, salt: 98);
            fx.Shake = Math.Max(fx.Shake, 0.2f * Fx.Envelope(c.Age, I - 0.1f, I - 0.08f, I - 0.04f, I + 0.06f));
            fx.Shake = Math.Max(fx.Shake, 0.45f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.3f));
        }
        Impact(c, col, fx, 1.15f);
    }

    /// <summary>Calm Mind: pink rings draw slowly in to the user's brow and a soft eye closes over it.</summary>
    private static void CalmMind(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var pink = Rgb(255, 168, 220);
        var pale = Rgb(255, 232, 246);
        float h = c.FromSize;
        var brow = c.FromFeet + Vector3.UnitY * h * 0.9f;
        float env = Fx.Envelope(c.Age, 0f, 0.15f, 0.95f, 1.25f);
        for (int i = 0; i < 4; i++)
        {
            float k = (c.Age - i * 0.17f) / 0.7f;
            if (k < 0f || k > 1f) continue;
            fx.Sprite(FxShape.Ring, brow, h * (1.0f - 0.85f * Fx.EaseOut(k)), Fx.Fade(i % 2 == 0 ? pink : pale, MathF.Sin(k * MathF.PI) * 0.9f));
        }
        if (env > 0f)
        {
            fx.Sprite(FxShape.Glow, brow, h * (0.3f + 0.2f * MathF.Sin(c.Age * 4f)), Fx.Fade(pink, env * 0.5f), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, c.From, h * 0.9f, Fx.Fade(pink, env * 0.22f), FxBlend.Alpha);
        }
        // A soft eye shutting: it drops and thins as it closes
        float shut = Fx.Envelope(c.Age, 0.35f, 0.5f, 0.95f, 1.2f);
        if (shut > 0f) fx.Sprite(FxShape.Eye, brow + Vector3.UnitY * h * 0.12f, h * 0.16f, Fx.Fade(pale, shut), FxBlend.Alpha);
        Rising(fx, c.Age, c.Seed, c.FromFeet, h, 0.1f, 1.2f, 5, FxShape.Spark, pale, 0.07f, 0.18f);
    }

    /// <summary>Double Hit: a pale tail swung in across the target from the left, then once more from the right.</summary>
    private static void DoubleHit(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var grey = Rgb(228, 228, 238);
        for (int b = 0; b < 2; b++)
        {
            float side = b == 0 ? -1f : 1f;
            float t0 = I - 0.27f + b * 0.15f;
            float k = (c.Age - t0) / 0.13f;
            if (k < 0f || k > 1.8f) continue;
            float u = Math.Min(1f, k);
            float fade = k < 1f ? 1f : Math.Max(0f, 1f - (k - 1f) * 1.25f);
            var start = c.To + new Vector3(side * 1.5f, 0.35f, 0.1f) * h;
            var end = c.To + new Vector3(-side * 0.9f, -0.2f, 0.1f) * h;
            var dir = end - start;
            var p = Vector3.Lerp(start, end, Fx.EaseOut(u));
            fx.Streak(FxShape.Streak, p, dir, h * 0.3f, 4f, Fx.Fade(grey, fade), FxBlend.Alpha);
            fx.Streak(FxShape.Streak, p, dir, h * 0.12f, 4f, Fx.Fade(White, fade));
        }
        Blows(c, col, fx, 2, 0.15f, 0.28f, 0.24f);
        Impact(c, col, fx, 0.85f);
    }

    /// <summary>Dragon Claw: the user flares red and dragon flames lick round it, then three great claws rake the target, twice, crossing.</summary>
    private static void DragonClaw(in FxCtx c, FxColors col, FxList fx)
    {
        var dragon = ColorsOf(PokemonType.Dragon);
        float h = c.FromSize;
        float flare = Fx.Envelope(c.Age, 0f, 0.05f, 0.18f, 0.3f);
        if (flare > 0f)
        {
            fx.Sprite(FxShape.Glow, c.From, h * 0.9f, Fx.Fade(Rgb(255, 90, 70), flare * 0.4f), FxBlend.Alpha);
            for (int i = 0; i < 5; i++)
            {
                float a = i * MathF.Tau / 5f;
                fx.Sprite(FxShape.Flame, c.FromFeet + new Vector3(MathF.Cos(a) * h * 0.5f, h * (0.25f + 0.35f * ((c.Age * 3f + i * 0.2f) % 1f)), MathF.Sin(a) * h * 0.2f), h * 0.2f, Fx.Fade(dragon.Main, flare), FxBlend.Alpha);
            }
        }
        ClawMarks(c, fx, I - 0.2f, 1.8f, Rgb(132, 120, 255), Rgb(225, 220, 255), 0.45f);
        ClawMarks(c, fx, I - 0.06f, 1.8f, Rgb(132, 120, 255), Rgb(225, 220, 255), -0.45f);
        if (c.Cue.Landed)
        {
            fx.Shake = Math.Max(fx.Shake, 0.3f * Fx.Envelope(c.Age, I - 0.02f, I, I + 0.08f, I + 0.25f));
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I - 0.02f, 0.5f, 7, FxShape.Flame, dragon.Main, dragon.Light, 0.12f, 0.9f, -0.5f, salt: 99);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Hyper Fang: one huge pair of white fangs snaps shut on the target.</summary>
    private static void HyperFang(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        FangPair(c, fx, I - 0.15f, 0.62f, Rgb(255, 250, 232), Rgb(255, 214, 150));
        if (c.Cue.Landed)
        {
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.35f, h * 1.3f, Fx.Fade(Rgb(255, 232, 190), 0.9f));
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.45f, 8, FxShape.Star, White, Rgb(255, 232, 190), 0.1f, 1.0f, 0.8f, FxBlend.Alpha, spin: 6f, salt: 100);
            fx.Shake = Math.Max(fx.Shake, 0.5f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.3f));
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Ingrain: green roots run out from the user's feet over the ground, with healing crosses rising along them.</summary>
    private static void Ingrain(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var feet = c.FromFeet + Vector3.UnitY * 0.05f;
        var root = Rgb(98, 68, 40);
        var green = Rgb(98, 176, 74);
        var light = Rgb(196, 244, 150);
        float env = Fx.Envelope(c.Age, 0f, 0.05f, 1.0f, 1.28f);
        for (int i = 0; i < 6; i++)
        {
            float k = (c.Age - 0.03f - i * 0.04f) / 0.5f;
            if (k <= 0f) continue;
            float len = h * (0.9f + 0.4f * Fx.Rand(c.Seed, i, 193)) * Fx.EaseOut(Math.Min(1f, k));
            float a = i * MathF.Tau / 6f + 0.4f;
            var dir = new Vector3(MathF.Cos(a), 0f, MathF.Sin(a) * 0.6f);
            var side = new Vector3(-dir.Z, 0f, dir.X);
            var pts = new Vector3[6];
            for (int j = 0; j < pts.Length; j++)
                pts[j] = feet + Vector3.UnitY * h * 0.09f + dir * len * (j / 5f) + side * MathF.Sin(j * 1.6f + i) * h * 0.07f;
            fx.Ribbon(pts, h * 0.15f, Fx.Fade(root, env), FxBlend.Alpha, depth: true);
            fx.Ribbon(pts, h * 0.06f, Fx.Fade(green, env * 0.9f), depth: true);
            fx.Sprite(FxShape.Leaf, pts[^1] + Vector3.UnitY * h * 0.06f, h * 0.09f * Math.Min(1f, k * 2f), Fx.Fade(green, env), FxBlend.Alpha, a);
        }
        if (env > 0f) fx.Sprite(FxShape.Glow, c.From, h * 0.8f, Fx.Fade(light, env * 0.2f), FxBlend.Alpha);
        Rising(fx, c.Age, c.Seed, c.FromFeet, h, 0.35f, 1.28f, 5, FxShape.Cross, light, 0.09f, 0.2f, MathF.PI / 4f);
    }

    /// <summary>Iron Defense: two bright bands of steel sheen sweep up the user, glinting.</summary>
    private static void IronDefense(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var steel = ColorsOf(PokemonType.Steel);
        float h = c.FromSize;
        foreach (float t0 in new[] { 0.04f, 0.5f })
        {
            float k = (c.Age - t0) / 0.42f;
            if (k < 0f || k > 1f) continue;
            float y = h * (0.05f + 1.0f * k);
            float a = MathF.Sin(k * MathF.PI);
            fx.Streak(FxShape.Streak, c.FromFeet + new Vector3(0f, y, 0.1f), Vector3.UnitX, h * 0.3f, 2.2f, Fx.Fade(steel.Light, a * 0.7f), FxBlend.Alpha);
            fx.Streak(FxShape.Streak, c.FromFeet + new Vector3(0f, y, 0.12f), Vector3.UnitX, h * 0.1f, 3.4f, Fx.Fade(White, a));
            Fx.Glint(fx, c.Age, c.FromFeet + new Vector3(Fx.Signed(c.Seed, (int)(t0 * 100f), 194) * h * 0.35f, y, 0.1f), h, t0 + 0.1f, White, 0.3f);
            Fx.Glint(fx, c.Age, c.FromFeet + new Vector3(Fx.Signed(c.Seed, (int)(t0 * 100f) + 1, 195) * h * 0.35f, y * 0.8f, 0.1f), h, t0 + 0.24f, White, 0.25f);
        }
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.85f, 1.2f);
        if (env > 0f) fx.Sprite(FxShape.Glow, c.From, h * 0.95f, Fx.Fade(Rgb(190, 204, 226), env * 0.45f), FxBlend.Alpha);
        Fx.Shockwave(fx, c.Age, c.FromFeet + Vector3.UnitY * 0.04f, Vector3.UnitY, 0.04f, 0.4f, h * 1.1f, Fx.Fade(steel.Light, 0.8f));
        Fx.Shockwave(fx, c.Age, c.FromFeet + Vector3.UnitY * 0.04f, Vector3.UnitY, 0.5f, 0.4f, h * 1.1f, Fx.Fade(steel.Light, 0.8f));
    }

    /// <summary>Mist: white clouds roll low across the user's side and hang there.</summary>
    private static void MistY8b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.25f, 0.95f, 1.28f);
        if (env <= 0f) return;
        var white = Rgb(238, 246, 255);
        for (int i = 0; i < 9; i++)
        {
            float u = (c.Age * 0.35f + i / 9f) % 1f;
            var p = c.FromFeet + new Vector3((u - 0.5f) * h * 3.4f, h * (0.12f + 0.35f * Fx.Rand(c.Seed, i, 196)), Fx.Signed(c.Seed, i, 197) * h * 0.25f);
            fx.Sprite(FxShape.Smoke, p, h * (0.5f + 0.35f * Fx.Rand(c.Seed, i, 198)), Fx.Fade(white, env * 0.55f * MathF.Sin(u * MathF.PI)), FxBlend.Alpha, i * 1.7f + c.Age * 0.5f);
        }
        fx.Sprite(FxShape.Glow, c.From, h * 0.9f, Fx.Fade(Rgb(210, 235, 255), env * 0.25f), FxBlend.Alpha);
        Fx.Burst(fx, c.Seed, c.Age, c.FromFeet + Vector3.UnitY * h * 0.5f, h, 0.1f, 1.0f, 8, FxShape.Spark, White, Rgb(200, 230, 255), 0.05f, 0.9f, -0.1f, salt: 101);
    }

    /// <summary>Poison Powder: a purple cloud drifts over to the target and spores rain down through it.</summary>
    private static void PoisonPowder(in FxCtx c, FxColors col, FxList fx)
    {
        var purple = Rgb(168, 72, 208);
        var pale = Rgb(224, 154, 244);
        fx.Land(I);
        Fx.Stream(fx, c, 0.02f, I, 0.3f, 7, FxShape.Smoke, Rgb(150, 62, 190, 210), Rgb(190, 110, 220, 210), 0.24f, arc: 0.02f, spread: 0.4f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0f, 0.35f, 0.3f, 6, FxShape.Spore, purple, pale, 0.09f, arc: 0.02f, spread: 0.3f);
        if (!c.Cue.Missed) Fx.Rain(fx, c.Age, c.Seed, c.To, c.ToSize, 0.25f, 1.1f, 0.6f, 26, FxShape.Spore, purple, pale, 0.06f, FxBlend.Add);
        if (c.Cue.Missed) return;
        float env = Fx.Envelope(c.Age, I, I + 0.12f, 0.9f, 1.25f);
        if (env > 0f)
            for (int i = 0; i < 4; i++)
                fx.Sprite(FxShape.Smoke, c.To + new Vector3(Fx.Signed(c.Seed, i, 199) * 0.4f, 0.15f + 0.3f * Fx.Rand(c.Seed, i, 200), 0f) * c.ToSize, c.ToSize * 0.5f, Fx.Fade(purple, env * 0.35f), FxBlend.Alpha, i * 2f + c.Age);
        Tint(c, fx, I, 1.0f, Rgb(150, 70, 200), 0.4f);
        Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.7f, 5, FxShape.Bubble, purple, pale, 0.07f, 0.6f, -0.6f, FxBlend.Alpha, salt: 102);
    }

    /// <summary>Submission: two hands grip the target, a ring of speed arcs turns about it and five blows land.</summary>
    private static void Submission(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var skin = Rgb(255, 218, 178);
        Strike(c, fx, FxShape.Palm, new Vector3(-1.2f, 0.1f, 0.1f), 0.18f, 0.34f, skin, -0.2f, 0.15f);
        Strike(c, fx, FxShape.Palm, new Vector3(1.2f, 0.1f, 0.1f), 0.18f, 0.34f, skin, MathF.PI + 0.2f, 0.15f);
        float env = Fx.Envelope(c.Age, 0.12f, 0.2f, I, I + 0.12f);
        if (env > 0f && !c.Cue.Missed)
            for (int i = 0; i < 4; i++)
            {
                float a = c.Age * (10f + c.Age * 30f) + i * MathF.PI / 2f;
                fx.Sprite(FxShape.Wave, c.To + new Vector3(MathF.Cos(a) * 0.7f * h, MathF.Sin(a) * 0.4f * h, 0.1f), h * 0.45f, Fx.Fade(Rgb(255, 236, 206), env * 0.9f), FxBlend.Alpha, a + MathF.PI * 0.5f);
            }
        Blows(c, col, fx, 5, 0.06f, 0.2f, 0.13f);
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Vital Throw: a hammer-throw circle of arcs about the target, then a heavy slam that rings the ground.</summary>
    private static void VitalThrow(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var skin = Rgb(255, 218, 178);
        Strike(c, fx, FxShape.Palm, new Vector3(-1.0f, 0.5f, 0.1f), 0.12f, 0.32f, skin, -0.4f, 0.12f);
        Strike(c, fx, FxShape.Palm, new Vector3(1.0f, 0.5f, 0.1f), 0.12f, 0.32f, skin, MathF.PI + 0.4f, 0.12f);
        float env = Fx.Envelope(c.Age, 0.1f, 0.18f, I - 0.06f, I);
        if (env > 0f && !c.Cue.Missed)
            for (int i = 0; i < 5; i++)
            {
                float a = c.Age * (12f + c.Age * 40f) + i * MathF.Tau / 5f;
                float r = h * (0.9f - 0.5f * (c.Age / I));
                fx.Sprite(FxShape.Wave, c.To + new Vector3(MathF.Cos(a) * r, MathF.Sin(a) * r * 0.6f, 0.1f), h * 0.4f, Fx.Fade(Rgb(255, 232, 200), env), FxBlend.Alpha, a + MathF.PI * 0.5f);
            }
        if (c.Cue.Landed)
        {
            GroundRing(fx, c.Age, c.ToFeet, I, 0.5f, h * 1.5f, Fx.Fade(Rgb(255, 214, 170), 0.9f));
            GroundRing(fx, c.Age, c.ToFeet, I + 0.08f, 0.5f, h * 1.0f, Fx.Fade(White, 0.7f));
            Dust(c, fx, c.ToFeet, h, I, 8);
            fx.Shake = Math.Max(fx.Shake, 0.55f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.32f));
        }
        Impact(c, col, fx, 1.15f);
    }

    /// <summary>Whirlpool: a column of water turns about the target on a swirl of the ground, drops circling it.</summary>
    private static void Whirlpool(in FxCtx c, FxColors col, FxList fx)
    {
        var water = ColorsOf(PokemonType.Water);
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0.05f, 0.25f, 1.0f, 1.28f);
        if (env > 0f && !c.Cue.Missed)
        {
            fx.Flat(FxShape.Swirl, c.ToFeet + Vector3.UnitY * 0.04f, Vector3.UnitY, h * 1.3f, Fx.Fade(water.Main, env * 0.75f), FxBlend.Alpha, -c.Age * 7f, depth: true);
            for (int i = 0; i < 3; i++)
                fx.Sprite(FxShape.Swirl, c.ToFeet + new Vector3(0f, h * (0.25f + 0.3f * i), 0.1f), h * (0.7f - 0.08f * i), Fx.Fade(water.Light, env * 0.7f), FxBlend.Alpha, -c.Age * (8f + i * 2f));
            for (int i = 0; i < 12; i++)
            {
                float u = (c.Age * 0.8f + i / 12f) % 1f;
                float a = c.Age * (8f + i % 3 * 2f) + i * 0.9f;
                float r = h * (0.7f - 0.3f * u);
                fx.Sprite(FxShape.Drop, c.ToFeet + new Vector3(MathF.Cos(a) * r, h * (0.1f + 1.0f * u), MathF.Sin(a) * r * 0.5f), h * 0.07f, Fx.Fade(water.Light, env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha);
            }
            Tint(c, fx, I, 1.15f, Rgb(150, 230, 240), 0.3f);
        }
        Fx.Stream(fx, c, 0f, I, 0.3f, 5, FxShape.Drop, water.Main, water.Light, 0.1f, arc: 0.1f, spread: 0.4f, blend: FxBlend.Alpha);
        Impact(c, col, fx, 0.85f);
    }

    /// <summary>Beat Up: three fists close on the target from three sides at once.</summary>
    private static void BeatUp(in FxCtx c, FxColors col, FxList fx)
    {
        var tan = Rgb(232, 196, 150);
        Strike(c, fx, FxShape.Fist, new Vector3(-1.2f, 0.3f, 0.1f), I, 0.46f, Rgb(255, 232, 200), -0.3f, 0.22f);
        Strike(c, fx, FxShape.Fist, new Vector3(1.2f, 0.15f, 0.1f), I, 0.46f, Rgb(255, 214, 170), MathF.PI + 0.3f, 0.22f);
        Strike(c, fx, FxShape.Fist, new Vector3(0.1f, 1.3f, 0.1f), I, 0.46f, Rgb(255, 244, 214), -MathF.PI / 2f, 0.22f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.5f, 7, FxShape.Star, White, tan, 0.09f, 0.9f, 0.8f, FxBlend.Alpha, spin: 6f, salt: 103);
            fx.Shake = Math.Max(fx.Shake, 0.3f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.08f, I + 0.24f));
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Fire Fang: red fangs close in a burst of flame, embers leaping, the target glowing hot.</summary>
    private static void FireFang(in FxCtx c, FxColors col, FxList fx)
    {
        var fire = ColorsOf(PokemonType.Fire);
        FangPair(c, fx, I - 0.15f, 0.36f, Rgb(255, 100, 56), Rgb(255, 232, 160));
        if (c.Cue.Landed)
        {
            Tint(c, fx, I, 1.0f, Rgb(255, 90, 40), 0.42f);
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.6f, 9, FxShape.Ember, fire.Light, fire.Main, 0.08f, 1.0f, -0.6f, salt: 104);
            fx.Shake = Math.Max(fx.Shake, 0.3f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.08f, I + 0.24f));
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Odor Sleuth: scent curls off the target and is drawn to the user's nose, and a ring locks on the foe.</summary>
    private static void OdorSleuth(in FxCtx c, FxColors col, FxList fx)
    {
        var scent = Rgb(244, 232, 140);
        Fx.Stream(fx, c, 0.02f, 0.75f, 0.42f, 10, FxShape.Wisp, scent, Rgb(255, 250, 210), 0.22f, arc: 0.03f, spread: 0.55f, spin: 2f, blend: FxBlend.Alpha, reverse: true);
        Fx.Shockwave(fx, c.Age, c.FromFeet + Vector3.UnitY * c.FromSize * 0.65f, Vector3.Zero, 0.55f, 0.3f, c.FromSize * 0.5f, Fx.Fade(scent, 0.9f));
        Reticle(c, fx, Rgb(255, 196, 80), false);
    }

    /// <summary>Kinesis: a silver spoon swings by the user like a pendulum and a pale swirl settles over the foe's eyes, a red arrow falling.</summary>
    private static void Kinesis(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var psy = ColorsOf(PokemonType.Psychic);
        var toward = c.Foe - c.From;
        toward.Y = 0f;
        var pivot = c.From + (toward.LengthSquared() > 1e-6f ? Vector3.Normalize(toward) : Vector3.UnitX) * h * 0.8f + Vector3.UnitY * h * 0.7f;
        float env = Fx.Envelope(c.Age, 0f, 0.08f, 0.8f, 1.05f);
        if (env > 0f)
        {
            float swing = 0.75f * MathF.Sin(c.Age * 7.5f) * (1f - 0.25f * c.Age);
            var bowl = pivot + new Vector3(MathF.Sin(swing), -MathF.Cos(swing), 0f) * h * 0.85f;
            fx.Ribbon(new[] { pivot, Vector3.Lerp(pivot, bowl, 0.5f), bowl }, h * 0.07f, Fx.Fade(Rgb(214, 220, 232), env), FxBlend.Alpha);
            fx.Ribbon(new[] { pivot, Vector3.Lerp(pivot, bowl, 0.5f), bowl }, h * 0.025f, Fx.Fade(White, env));
            fx.Sprite(FxShape.Orb, bowl, h * 0.2f, Fx.Fade(Rgb(226, 232, 244), env), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, bowl, h * 0.4f, Fx.Fade(psy.Light, env * 0.45f));
            if (MathF.Abs(MathF.Sin(c.Age * 7.5f)) < 0.12f) Fx.Glint(fx, c.Age, bowl, h, c.Age - 0.02f, White, 0.3f);
            fx.Sprite(FxShape.Ring, bowl, h * (0.2f + 0.2f * ((c.Age * 3f) % 1f)), Fx.Fade(psy.Main, env * 0.5f));
        }
        if (c.Cue.Missed) return;
        float ht = c.ToSize;
        float swirl = Fx.Envelope(c.Age, I - 0.1f, I + 0.1f, 0.9f, 1.2f);
        if (swirl > 0f)
        {
            fx.Sprite(FxShape.Swirl, c.To + Vector3.UnitY * ht * 0.25f, ht * 0.4f, Fx.Fade(psy.Light, swirl * 0.8f), FxBlend.Alpha, c.Age * 6f);
            fx.Sprite(FxShape.Swirl, c.To + Vector3.UnitY * ht * 0.25f, ht * 0.28f, Fx.Fade(White, swirl * 0.6f), rotation: -c.Age * 8f);
        }
        float k = (c.Age - I - 0.08f) / 0.5f;
        if (k > 0f && k < 1f)
            fx.Sprite(FxShape.Arrow, c.To + new Vector3(0f, ht * (0.95f - 0.3f * k), 0.1f), ht * 0.24f, Fx.Fade(Rgb(255, 110, 100), Math.Min(1f, (1f - k) * 3f)), FxBlend.Alpha, MathF.PI);
    }

    /// <summary>Metal Sound: a jagged zigzag of grey light runs across and rings of metal close on the target, sparks flying.</summary>
    private static void MetalSound(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var steel = ColorsOf(PokemonType.Steel);
        var path = c.Aim - c.From;
        float dist = path.Length();
        var (across, up) = Fx.Frame(path);
        float k = (c.Age - 0.03f) / 0.3f;
        float fade = Fx.Envelope(c.Age, 0f, 0.04f, 0.4f, 0.65f);
        if (k > 0f && fade > 0f)
        {
            float head = Math.Min(1f, k);
            var pts = new Vector3[16];
            for (int i = 0; i < pts.Length; i++)
            {
                float u = head * i / (pts.Length - 1f);
                float zig = (i % 2 == 0 ? 1f : -1f) * dist * 0.05f * (0.5f + MathF.Sin(u * MathF.PI));
                pts[i] = Vector3.Lerp(c.From, c.Aim, u) + up * zig + across * zig * 0.4f;
            }
            fx.Ribbon(pts, c.FromSize * 0.12f, Fx.Fade(Rgb(190, 202, 220), fade * 0.9f), FxBlend.Alpha);
            fx.Ribbon(pts, c.FromSize * 0.04f, Fx.Fade(White, fade));
        }
        Fx.Waves(fx, c, 0.02f, 0.5f, 3, FxShape.Wave, Fx.Fade(steel.Light, 0.9f), 0.45f, 0.28f);
        if (c.Cue.Missed) return;
        for (int i = 0; i < 3; i++)
        {
            float t = (c.Age - I - i * 0.12f) / 0.35f;
            if (t < 0f || t > 1f) continue;
            fx.Sprite(FxShape.Ring, c.To, c.ToSize * (1.2f - 0.8f * t), Fx.Fade(steel.Light, MathF.Sin(t * MathF.PI)));
        }
        Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.5f, 9, FxShape.Spark, steel.Light, White, 0.09f, 0.9f, salt: 105);
    }

    /// <summary>Miracle Eye: three golden eyes open round the target and draw into one, which glints white.</summary>
    private static void MiracleEye(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        if (c.Cue.Missed) return;
        float h = c.ToSize;
        var gold = Rgb(255, 224, 120);
        var pale = Rgb(255, 246, 200);
        float open = Fx.Envelope(c.Age, 0f, 0.1f, I - 0.03f, I + 0.02f);
        if (open > 0f)
        {
            float draw = Fx.EaseOut(Math.Clamp(c.Age / I, 0f, 1f));
            for (int i = 0; i < 3; i++)
            {
                float a = MathF.PI / 2f + i * MathF.Tau / 3f;
                var p = c.To + new Vector3(MathF.Cos(a) * 1.1f, MathF.Sin(a) * 0.8f, 0.1f) * h * (1f - draw);
                fx.Sprite(FxShape.Eye, p, h * 0.2f * (0.6f + 0.4f * open), Fx.Fade(gold, open), FxBlend.Alpha);
                fx.Sprite(FxShape.Glow, p, h * 0.3f, Fx.Fade(pale, open * 0.5f));
            }
        }
        float one = Fx.Envelope(c.Age, I - 0.02f, I + 0.06f, 0.8f, 1.15f);
        if (one > 0f)
        {
            fx.Sprite(FxShape.Eye, c.To + Vector3.UnitZ * 0.1f, h * 0.38f, Fx.Fade(gold, one), FxBlend.Alpha);
            fx.Sprite(FxShape.Spark, c.To + Vector3.UnitZ * 0.12f, h * 0.7f * one, Fx.Fade(pale, one * 0.8f), rotation: c.Age * 1.5f);
            Tint(c, fx, I, 1.0f, pale, 0.35f);
        }
        Fx.Glint(fx, c.Age, c.To, h, I, White, 0.6f);
    }

    /// <summary>Ominous Wind: dark violet gusts, wisps in them, blow across twice and shake the target.</summary>
    private static void OminousWind(in FxCtx c, FxColors col, FxList fx)
    {
        var ghost = ColorsOf(PokemonType.Ghost);
        var violet = Rgb(120, 76, 176);
        Fx.Stream(fx, c, 0f, I, 0.28f, 8, FxShape.Wisp, ghost.Main, ghost.Light, 0.15f, arc: 0.05f, spread: 0.6f, spin: 3f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0f, I, 0.28f, 5, FxShape.Streak, ghost.Light, White, 0.05f, spread: 0.5f, trail: 4f);
        Fx.Stream(fx, c, 0.38f, 0.8f, 0.28f, 8, FxShape.Wisp, ghost.Main, ghost.Light, 0.15f, arc: 0.05f, spread: 0.6f, spin: 3f, blend: FxBlend.Alpha);
        if (!c.Cue.Missed)
        {
            float env = Fx.Envelope(c.Age, I, I + 0.1f, 0.95f, 1.25f);
            if (env > 0f)
                for (int i = 0; i < 4; i++)
                    fx.Sprite(FxShape.Smoke, c.To + new Vector3(Fx.Signed(c.Seed, i, 201) * 0.45f, Fx.Signed(c.Seed, i, 202) * 0.3f, 0.1f) * c.ToSize, c.ToSize * 0.5f, Fx.Fade(violet, env * 0.4f), FxBlend.Alpha, i + c.Age * 2f);
            Tint(c, fx, I, 1.0f, violet, 0.35f);
            fx.Shake = Math.Max(fx.Shake, 0.3f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.24f));
            fx.Shake = Math.Max(fx.Shake, 0.3f * Fx.Envelope(c.Age, 0.78f, 0.8f, 0.88f, 1.02f));
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Psychic: the user's eyes flare, two violet discs turn against each other round the target and rings crush in.</summary>
    private static void PsychicY8b(in FxCtx c, FxColors col, FxList fx)
    {
        var psy = ColorsOf(PokemonType.Psychic);
        float h = c.ToSize;
        var head = c.FromFeet + Vector3.UnitY * c.FromSize * 0.9f;
        Fx.Glint(fx, c.Age, head + new Vector3(-0.1f, 0f, 0.1f) * c.FromSize, c.FromSize, 0.0f, Rgb(255, 250, 170), 0.3f);
        Fx.Glint(fx, c.Age, head + new Vector3(0.12f, 0f, 0.1f) * c.FromSize, c.FromSize, 0.04f, Rgb(255, 250, 170), 0.3f);
        if (!c.Cue.Missed)
        {
            float env = Fx.Envelope(c.Age, 0.08f, 0.22f, 0.9f, 1.2f);
            if (env > 0f)
            {
                fx.Sprite(FxShape.Swirl, c.To, h * 1.0f, Fx.Fade(psy.Main, env * 0.6f), FxBlend.Alpha, c.Age * 6f);
                fx.Sprite(FxShape.Swirl, c.To, h * 0.75f, Fx.Fade(Rgb(190, 130, 255), env * 0.6f), FxBlend.Alpha, -c.Age * 8f);
                for (int i = 0; i < 8; i++)
                {
                    float u = Math.Min(1f, c.Age / (I + 0.1f));
                    float a = c.Age * 7f + i * MathF.Tau / 8f;
                    float r = h * (1.2f - 0.8f * Fx.EaseOut(u));
                    fx.Sprite(FxShape.Spark, c.To + new Vector3(MathF.Cos(a) * r, MathF.Sin(a) * r * 0.7f, 0.1f), h * 0.07f, Fx.Fade(psy.Light, env), rotation: a);
                }
            }
            for (int i = 0; i < 3; i++)
            {
                float k = (c.Age - (I - 0.25f) - i * 0.1f) / 0.4f;
                if (k < 0f || k > 1f) continue;
                fx.Sprite(FxShape.Ring, c.To, h * (1.4f - 0.9f * k), Fx.Fade(i % 2 == 0 ? psy.Main : psy.Light, MathF.Sin(k * MathF.PI)));
            }
            fx.Shake = Math.Max(fx.Shake, 0.3f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.3f));
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Wrap: two bands of pink ribbon wind up round the target and squeeze.</summary>
    private static void Wrap(in FxCtx c, FxColors col, FxList fx)
    {
        var pink = Rgb(255, 150, 190);
        var rim = Rgb(255, 214, 230);
        float h = c.ToSize;
        if (!c.Cue.Missed)
        {
            float env = Fx.Envelope(c.Age, 0.02f, 0.1f, 1.0f, 1.28f);
            float squeeze = Math.Clamp((c.Age - 0.15f) / (I - 0.1f), 0f, 1f);
            float reach = Math.Clamp((c.Age - 0.02f) / 0.28f, 0f, 1f);
            for (int b = 0; b < 2; b++)
            {
                const int n = 17;
                var pts = new List<Vector3>();
                for (int j = 0; j < n; j++)
                {
                    float s = j / (n - 1f);
                    if (s > reach) break;
                    float th = b * MathF.PI + s * 9f - c.Age * 2f;
                    float r = h * (0.62f - 0.2f * squeeze);
                    pts.Add(c.ToFeet + new Vector3(MathF.Cos(th) * r, h * (0.1f + 0.85f * s), MathF.Sin(th) * r * 0.5f + 0.1f));
                }
                if (pts.Count < 2) continue;
                fx.Ribbon(pts.ToArray(), h * 0.1f, Fx.Fade(pink, env), FxBlend.Alpha);
                fx.Ribbon(pts.ToArray(), h * 0.035f, Fx.Fade(rim, env * 0.8f));
            }
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I - 0.04f, 0.3f, h * 0.9f, Fx.Fade(rim, 0.8f));
            fx.Shake = Math.Max(fx.Shake, 0.18f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.24f));
        }
        Impact(c, col, fx, 0.75f);
    }

    /// <summary>Embargo: a red forbidden sign is stamped on the target and the target darkens.</summary>
    private static void Embargo(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        if (c.Cue.Missed) return;
        float h = c.ToSize;
        var red = Rgb(224, 48, 56);
        float env = Fx.Envelope(c.Age, I - 0.16f, I - 0.06f, 0.95f, 1.2f);
        if (env > 0f)
        {
            float stamp = Math.Clamp((c.Age - (I - 0.16f)) / 0.16f, 0f, 1f);
            float size = h * 0.6f * (1.9f - 0.9f * Fx.EaseOut(stamp));
            var at = c.To + new Vector3(0.25f, 0.35f, 0.2f) * h;
            fx.Sprite(FxShape.Glow, at, size * 1.1f, Fx.Fade(White, env * 0.6f), FxBlend.Alpha);
            fx.Sprite(FxShape.Ring, at, size, Fx.Fade(White, env), FxBlend.Alpha);
            fx.Sprite(FxShape.Ring, at, size * 0.9f, Fx.Fade(red, env), FxBlend.Alpha);
            fx.Sprite(FxShape.Ring, at, size * 0.8f, Fx.Fade(red, env * 0.8f), FxBlend.Alpha);
            fx.Streak(FxShape.Streak, at, new Vector3(1f, -1f, 0f), size * 0.2f, 3.6f, Fx.Fade(White, env), FxBlend.Alpha);
            fx.Streak(FxShape.Streak, at, new Vector3(1f, -1f, 0f), size * 0.13f, 3.6f, Fx.Fade(red, env), FxBlend.Alpha);
        }
        float dark = Fx.Envelope(c.Age, I - 0.05f, I + 0.1f, 0.9f, 1.2f);
        if (dark > 0f) fx.Sprite(FxShape.Glow, c.To, h * 1.1f, Fx.Fade(Rgb(24, 20, 36), dark * 0.45f), FxBlend.Alpha);
        Fx.Shockwave(fx, c.Age, c.To + new Vector3(0.25f, 0.35f, 0.2f) * h, Vector3.Zero, I, 0.3f, h * 0.8f, Fx.Fade(red, 0.9f));
        fx.Shake = Math.Max(fx.Shake, 0.2f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.08f, I + 0.2f));
    }

    /// <summary>Gravity: violet streaks are pressed down onto both platforms, a ring flattening on each.</summary>
    private static void GravityY8b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var violet = Rgb(150, 112, 232);
        var pale = Rgb(214, 196, 255);
        foreach (var (feet, h) in new[] { (c.FromFeet, c.FromSize), (c.ToFeet, c.ToSize) })
        {
            float env = Fx.Envelope(c.Age, 0.1f, 0.2f, 0.95f, 1.25f);
            for (int i = 0; i < 8 && env > 0f; i++)
            {
                float u = (c.Age * 1.8f + Fx.Rand(c.Seed, i, 203)) % 1f;
                var p = feet + new Vector3(Fx.Signed(c.Seed, i, 204) * h * 0.55f, h * (2.2f - 2.05f * u * u), Fx.Signed(c.Seed, i, 205) * h * 0.2f);
                fx.Streak(FxShape.Streak, p, Vector3.UnitY, h * 0.08f, 6f, Fx.Fade(i % 2 == 0 ? violet : pale, env * MathF.Sin(u * MathF.PI)));
            }
            float press = Fx.Envelope(c.Age, 0.2f, 0.35f, 0.9f, 1.2f);
            if (press > 0f)
                fx.Flat(FxShape.Ring, feet + Vector3.UnitY * 0.04f, Vector3.UnitY, h * (1.1f - 0.35f * Fx.EaseOut((c.Age - 0.2f) / 0.3f)), Fx.Fade(violet, press * 0.9f), depth: true);
            Fx.Shockwave(fx, c.Age, feet + Vector3.UnitY * 0.04f, Vector3.UnitY, 0.55f, 0.4f, h * 1.3f, Fx.Fade(pale, 0.7f));
            Dust(c, fx, feet, h, 0.55f, 5);
        }
    }

    /// <summary>Psycho Cut: three pink blades of force gather round the user and are flown at the target one after another.</summary>
    private static void PsychoCut(in FxCtx c, FxColors col, FxList fx)
    {
        var psy = ColorsOf(PokemonType.Psychic);
        var pink = Rgb(255, 130, 200);
        float h = c.FromSize;
        var hover = new[] { new Vector3(-0.6f, 0.9f, 0.1f), new Vector3(0f, 1.25f, 0.1f), new Vector3(0.6f, 0.9f, 0.1f) };
        for (int i = 0; i < 3; i++)
        {
            float launch = I - 0.14f - (2 - i) * 0.1f;
            var home = c.FromFeet + hover[i] * h;
            float gather = Fx.Envelope(c.Age, i * 0.04f, i * 0.04f + 0.1f, launch, launch + 0.01f);
            if (gather > 0f)
            {
                fx.Sprite(FxShape.Sword, home, h * 0.4f, Fx.Fade(pink, gather), FxBlend.Alpha, MathF.PI / 2f - 0.5f * (i - 1));
                fx.Sprite(FxShape.Glow, home, h * 0.4f, Fx.Fade(psy.Light, gather * 0.5f));
            }
            float k = (c.Age - launch) / 0.14f;
            if (k < 0f || k > 1.4f) continue;
            float fade = k < 1f ? 1f : Math.Max(0f, 1f - (k - 1f) * 2.5f);
            var p = Vector3.Lerp(home, c.Aim, k * k);
            var dir = c.Aim - home;
            float ang = ScreenAngle(dir) - MathF.PI / 2f;
            fx.Sprite(FxShape.Sword, p, h * 0.42f, Fx.Fade(pink, fade), FxBlend.Alpha, ang);
            fx.Sprite(FxShape.Sword, p, h * 0.46f, Fx.Fade(White, fade * 0.5f), rotation: ang);
            fx.Streak(FxShape.Streak, p - Vector3.Normalize(dir) * h * 0.3f, dir, h * 0.05f, 6f, Fx.Fade(psy.Light, fade * 0.8f));
            if (c.Cue.Landed && k >= 1f && k < 1.1f)
                fx.Shake = Math.Max(fx.Shake, 0.18f);
        }
        if (c.Cue.Landed)
            for (int i = 0; i < 3; i++)
                Fx.Contact(fx, c.Age, c.Seed + i, c.To + new Vector3(Fx.Signed(c.Seed, i, 206) * 0.2f, Fx.Signed(c.Seed, i, 207) * 0.2f, 0f) * c.ToSize, c.ToSize, I - (2 - i) * 0.1f, 0.2f, psy.Main, psy.Light);
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Torment: six dark jabs at the target, and an anger mark pops over it, red.</summary>
    private static void Torment(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        if (c.Cue.Missed) return;
        float h = c.ToSize;
        var dark = Rgb(86, 52, 124);
        for (int i = 0; i < 6; i++)
        {
            float t0 = 0.03f + i * 0.05f;
            float k = (c.Age - t0) / 0.1f;
            if (k < 0f || k > 1f) continue;
            float side = i % 2 == 0 ? -1f : 1f;
            var dir = new Vector3(side * (0.7f + 0.3f * Fx.Rand(c.Seed, i, 208)), Fx.Signed(c.Seed, i, 209) * 0.5f, 0f);
            var p = c.To + dir * h * (0.95f - 0.6f * Fx.EaseOut(Math.Min(1f, k * 1.8f)));
            fx.Streak(FxShape.Streak, p, -dir, h * 0.2f, 3.2f, Fx.Fade(Rgb(120, 76, 170), 1f - k * 0.5f), FxBlend.Alpha);
            fx.Streak(FxShape.Streak, p, -dir, h * 0.08f, 3.2f, Fx.Fade(White, 1f - k * 0.5f));
            Fx.Contact(fx, c.Age, c.Seed + i, c.To + dir * h * 0.35f, h, t0 + 0.06f, 0.1f, dark, Rgb(210, 190, 240));
        }
        Anger(fx, c.Age, c.To + new Vector3(0.38f, 0.85f, 0.1f) * h, h, I - 0.05f, I + 0.75f);
        Tint(c, fx, I, 1.05f, AngerRed, 0.3f);
        fx.Shake = Math.Max(fx.Shake, 0.18f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.08f, I + 0.2f));
    }

    /// <summary>Assist: five orbs circle the user, quicker and quicker, and one turns gold and bursts in stars.</summary>
    private static void Assist(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var colours = new[] { Rgb(255, 100, 90), Rgb(100, 160, 255), Rgb(255, 226, 96), Rgb(120, 230, 130), Rgb(255, 150, 210) };
        int chosen = Math.Abs(c.Seed) % 5;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, I, I + 0.18f);
        float spin = c.Age * 6f + c.Age * c.Age * 8f;
        for (int i = 0; i < 5; i++)
        {
            float a = spin + i * MathF.Tau / 5f;
            var p = c.FromFeet + new Vector3(MathF.Cos(a) * h * 0.75f, h * (0.55f + 0.1f * MathF.Sin(a * 2f)), MathF.Sin(a) * h * 0.3f);
            bool gold = i == chosen && c.Age > I - 0.1f;
            var tone = gold ? Rgb(255, 214, 90) : colours[i];
            float e = (i == chosen) ? Fx.Envelope(c.Age, 0f, 0.1f, I + 0.1f, I + 0.22f) : env;
            float grow = gold ? 1.5f : 1f;
            fx.Sprite(FxShape.Glow, p, h * 0.2f * grow, Fx.Fade(tone, e * 0.6f));
            fx.Sprite(FxShape.Orb, p, h * 0.09f * grow, Fx.Fade(tone, e), FxBlend.Alpha, spin);
        }
        Fx.Burst(fx, c.Seed, c.Age, c.From, h, I, 0.5f, 9, FxShape.Star, Rgb(255, 232, 130), White, 0.09f, 0.9f, 0.4f, FxBlend.Alpha, spin: 5f, salt: 106);
        Fx.Shockwave(fx, c.Age, c.From, Vector3.Zero, I, 0.35f, h * 0.8f, Fx.Fade(Rgb(255, 220, 110), 0.9f));
    }

    /// <summary>Bone Rush: a cream bone is spun in from the left and a second after it, dust where they land.</summary>
    private static void BoneRush(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var bone = Rgb(246, 234, 204);
        for (int b = 0; b < 2; b++)
        {
            float land = I - (1 - b) * 0.12f;
            float k = (c.Age - (land - 0.22f)) / 0.22f;
            if (k < 0f || k > 1.4f) continue;
            float u = Math.Min(1f, k);
            float fade = k < 1f ? 1f : Math.Max(0f, 1f - (k - 1f) * 2.5f);
            var from = c.From + new Vector3(0f, (b - 0.5f) * 0.3f * h, 0.1f);
            var p = Lob(from, c.Aim + Vector3.UnitZ * 0.1f, u, 0.05f);
            Bone(fx, p, c.Age * 18f + b * 1.3f, h * 0.9f, bone, fade);
            if (c.Cue.Landed && k >= 1f) Fx.Contact(fx, c.Age, c.Seed + b, c.To, h, land, 0.2f, Rgb(214, 190, 150), White);
        }
        if (c.Cue.Landed)
        {
            Dust(c, fx, c.ToFeet, h, I - 0.12f, 4);
            Dust(c, fx, c.ToFeet, h, I, 5);
            fx.Shake = Math.Max(fx.Shake, 0.25f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.08f, I + 0.22f));
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Captivate: a wink-star glints over the user and a stream of small hearts and sparkles floats to the foe.</summary>
    private static void Captivate(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var pink = Rgb(255, 130, 180);
        var pale = Rgb(255, 210, 230);
        Fx.Glint(fx, c.Age, c.FromFeet + new Vector3(0.18f, 1.0f, 0.1f) * h, h, 0.02f, Rgb(255, 236, 246), 0.5f);
        float pop = Fx.Envelope(c.Age, 0.05f, 0.12f, 0.3f, 0.5f);
        if (pop > 0f) fx.Sprite(FxShape.Heart, c.FromFeet + new Vector3(-0.1f, 1.25f + 0.15f * pop, 0f) * h, h * 0.16f, Fx.Fade(pink, pop), FxBlend.Alpha);
        Fx.Stream(fx, c, 0.1f, I, 0.3f, 6, FxShape.Heart, pink, pale, 0.12f, arc: 0.03f, spread: 0.4f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0.08f, I, 0.3f, 12, FxShape.Spark, pale, White, 0.07f, arc: 0.03f, spread: 0.6f);
        if (c.Cue.Missed) return;
        Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.7f, 6, FxShape.Heart, pink, pale, 0.08f, 0.7f, -0.5f, FxBlend.Alpha, salt: 107);
        Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.4f, c.ToSize * 0.9f, Fx.Fade(pink, 0.8f));
    }

    /// <summary>Drain Punch: a fist is driven in, and green motes are drawn back from the target to the user, which glows.</summary>
    private static void DrainPunch(in FxCtx c, FxColors col, FxList fx)
    {
        Strike(c, fx, FxShape.Fist, new Vector3(-1.4f, 0.2f, 0.1f), I, 0.5f, Rgb(255, 214, 170), -0.15f, 0.25f);
        Impact(c, col, fx, 1.0f);
        if (c.Cue.Landed)
        {
            Fx.Stream(fx, c, I + 0.05f, 1.2f, 0.45f, 8, FxShape.Orb, Rgb(120, 230, 120), Rgb(210, 255, 190), 0.1f, arc: 0.12f, spread: 0.6f, blend: FxBlend.Alpha, reverse: true);
            Fx.Stream(fx, c, I + 0.05f, 1.2f, 0.45f, 8, FxShape.Glow, Fx.Fade(Rgb(150, 255, 150), 0.6f), White, 0.16f, arc: 0.12f, spread: 0.6f, reverse: true);
            float glow = Fx.Envelope(c.Age, 0.75f, 0.9f, 1.05f, 1.28f);
            if (glow > 0f) fx.Sprite(FxShape.Glow, c.From, c.FromSize * 0.9f, Fx.Fade(Rgb(150, 255, 160), glow * 0.45f), FxBlend.Alpha);
            Fx.Glint(fx, c.Age, c.From, c.FromSize, 0.95f, Rgb(210, 255, 200), 0.5f);
        }
    }

    /// <summary>Fly: a spiral of wind at the user's feet and a white streak falling from the sky onto the target, feathers behind it.</summary>
    private static void FlyY8b(in FxCtx c, FxColors col, FxList fx)
    {
        var air = ColorsOf(PokemonType.Flying);
        float h = c.FromSize;
        float lift = Fx.Envelope(c.Age, 0f, 0.05f, 0.14f, 0.3f);
        if (lift > 0f)
        {
            fx.Flat(FxShape.Swirl, c.FromFeet + Vector3.UnitY * 0.05f, Vector3.UnitY, h * 0.9f, Fx.Fade(air.Light, lift * 0.8f), FxBlend.Alpha, c.Age * 12f, depth: true);
            for (int i = 0; i < 6; i++)
                fx.Streak(FxShape.Streak, c.FromFeet + new Vector3(Fx.Signed(c.Seed, i, 210) * h * 0.5f, h * (0.2f + 1.3f * ((c.Age * 4f + i * 0.17f) % 1f)), 0f), Vector3.UnitY, h * 0.05f, 6f, Fx.Fade(White, lift));
        }
        float k = (c.Age - (I - 0.3f)) / 0.3f;
        if (k > 0f && k < 1.4f)
        {
            float u = Math.Min(1f, k);
            float fade = k < 1f ? 1f : Math.Max(0f, 1f - (k - 1f) * 2.5f);
            var start = c.To + new Vector3(-1.6f, 2.6f, 0.1f) * c.ToSize;
            var p = Vector3.Lerp(start, c.Aim, u * u);
            var dir = c.Aim - start;
            fx.Streak(FxShape.Streak, p, dir, c.FromSize * 0.22f, 6f, Fx.Fade(air.Light, fade), FxBlend.Alpha);
            fx.Streak(FxShape.Streak, p, dir, c.FromSize * 0.09f, 6f, Fx.Fade(White, fade));
            for (int j = 1; j <= 4; j++)
                fx.Sprite(FxShape.Feather, p - Vector3.Normalize(dir) * c.FromSize * 0.45f * j, c.FromSize * 0.1f, Fx.Fade(White, fade * (1f - j * 0.2f)), FxBlend.Alpha, j * 1.2f + c.Age * 4f);
        }
        if (c.Cue.Landed)
            fx.Shake = Math.Max(fx.Shake, 0.4f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.3f));
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Recover: gold rings of light pulse up the user, healing crosses rising, and a chime glints late.</summary>
    private static void Recover(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var gold = Rgb(255, 226, 112);
        var pale = Rgb(255, 248, 206);
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.9f, 1.2f);
        if (env > 0f) fx.Sprite(FxShape.Glow, c.From, h * 0.95f, Fx.Fade(gold, env * 0.35f), FxBlend.Alpha);
        foreach (float t0 in new[] { 0.05f, 0.35f, 0.65f })
        {
            float k = (c.Age - t0) / 0.5f;
            if (k < 0f || k > 1f) continue;
            fx.Flat(FxShape.Ring, c.FromFeet + Vector3.UnitY * h * Fx.EaseOut(k), Vector3.UnitY, h * (0.65f - 0.15f * k), Fx.Fade(gold, (1f - k) * 0.9f), depth: true);
        }
        Rising(fx, c.Age, c.Seed, c.FromFeet, h, 0.1f, 1.2f, 7, FxShape.Cross, pale, 0.15f, 0.25f, MathF.PI / 4f);
        Fx.Glint(fx, c.Age, c.From + new Vector3(0.1f, 0.2f, 0.1f) * h, h, 0.62f, White, 0.5f);
    }

    /// <summary>Snatch: a dark hand reaches to the target, takes a gold orb from it and carries it back to the user.</summary>
    private static void Snatch(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var dark = Rgb(72, 52, 112);
        var gold = Rgb(255, 214, 96);
        float h = c.ToSize;
        float reach = Math.Clamp(c.Age / I, 0f, 1f);
        float back = Math.Clamp((c.Age - I) / 0.5f, 0f, 1f);
        float env = Fx.Envelope(c.Age, 0f, 0.05f, 0.85f, 1.05f);
        if (env <= 0f) return;
        var hand = c.Cue.Missed ? Vector3.Lerp(c.From, c.Aim, Fx.EaseOut(reach) * (1f - back)) : Vector3.Lerp(Vector3.Lerp(c.From, c.To, Fx.EaseOut(reach)), c.From, Fx.EaseOut(back));
        fx.Sprite(FxShape.Palm, hand, h * 0.3f, Fx.Fade(dark, env), FxBlend.Alpha, ScreenAngle(c.To - c.From) * 0.5f);
        fx.Sprite(FxShape.Glow, hand, h * 0.35f, Fx.Fade(Rgb(150, 110, 220), env * 0.5f));
        if (c.Cue.Missed) return;
        float held = c.Age < I ? 1f : 1f;
        var orb = c.Age < I ? c.To + new Vector3(0.1f, 0.15f, 0.1f) * h : hand + Vector3.UnitZ * 0.1f;
        fx.Sprite(FxShape.Glow, orb, h * 0.25f * held, Fx.Fade(gold, env * 0.7f));
        fx.Sprite(FxShape.Orb, orb, h * 0.1f, Fx.Fade(gold, env), FxBlend.Alpha, c.Age * 4f);
        Fx.Glint(fx, c.Age, c.From, c.FromSize, 0.9f, Rgb(255, 236, 160), 0.45f);
        fx.Shake = Math.Max(fx.Shake, 0.15f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.06f, I + 0.16f));
    }

    /// <summary>Strength: the user flares orange and two great palms shove in, a ring and dust on the ground.</summary>
    private static void Strength(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var orange = Rgb(255, 160, 80);
        float flare = Fx.Envelope(c.Age, 0f, 0.06f, 0.14f, 0.26f);
        if (flare > 0f)
        {
            fx.Sprite(FxShape.Glow, c.From, c.FromSize * 0.95f, Fx.Fade(orange, flare * 0.45f), FxBlend.Alpha);
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathF.Tau / 6f;
                fx.Streak(FxShape.Streak, c.From + new Vector3(MathF.Cos(a), MathF.Sin(a), 0f) * c.FromSize * 0.7f, new Vector3(MathF.Cos(a), MathF.Sin(a), 0f), c.FromSize * 0.05f, 4f, Fx.Fade(Rgb(255, 214, 150), flare));
            }
        }
        var skin = Rgb(255, 214, 170);
        Strike(c, fx, FxShape.Palm, new Vector3(-1.4f, 0.45f, 0.1f), I, 0.46f, skin, -0.2f, 0.15f);
        Strike(c, fx, FxShape.Palm, new Vector3(-1.4f, -0.3f, 0.1f), I, 0.46f, Rgb(240, 196, 150), -0.2f, 0.15f);
        if (c.Cue.Landed)
        {
            GroundRing(fx, c.Age, c.ToFeet, I, 0.45f, h * 1.4f, Fx.Fade(orange, 0.9f));
            Dust(c, fx, c.ToFeet, h, I, 8);
            fx.Shake = Math.Max(fx.Shake, 0.5f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.3f));
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Ice Beam: a pale beam carrying snowflakes, and ice grows up round the target.</summary>
    private static void IceBeamY8b(in FxCtx c, FxColors col, FxList fx)
    {
        var ice = ColorsOf(PokemonType.Ice);
        Fx.Beam(fx, c, 0.06f, 0.6f, 0.1f, ice.Main, Rgb(240, 252, 255), 6);
        float span = 0.54f;
        if (c.Age > 0.1f && c.Age < 0.06f + span)
            for (int i = 0; i < 9; i++)
            {
                float u = (c.Age * 1.6f + i / 9f) % 1f;
                var p = Vector3.Lerp(c.From, c.Aim, u) + new Vector3(Fx.Signed(c.Seed, i, 211) * 0.25f, Fx.Signed(c.Seed, i, 212) * 0.25f, 0f) * c.FromSize;
                fx.Sprite(FxShape.Snowflake, p, c.FromSize * 0.09f, Fx.Fade(White, 0.9f), FxBlend.Alpha, c.Age * 3f + i);
            }
        if (c.Cue.Landed)
        {
            float h = c.ToSize;
            float grow = Fx.EaseOut(Math.Clamp((c.Age - I) / 0.2f, 0f, 1f));
            float env = Fx.Envelope(c.Age, I, I + 0.04f, 0.95f, 1.25f);
            for (int i = 0; i < 6 && env > 0f; i++)
            {
                float x = (i - 2.5f) * 0.22f;
                fx.Sprite(FxShape.Shard, c.ToFeet + new Vector3(x * h, h * (0.25f + 0.2f * (i % 3)) * grow, 0.1f), h * (0.13f + 0.05f * (i % 3)) * grow, Fx.Fade(i % 2 == 0 ? ice.Main : ice.Light, env), FxBlend.Alpha, Fx.Signed(c.Seed, i, 213) * 0.3f);
            }
            Tint(c, fx, I, 1.1f, Rgb(170, 225, 255), 0.4f);
            fx.Flat(FxShape.Ring, c.ToFeet + Vector3.UnitY * 0.04f, Vector3.UnitY, h * 0.9f * grow, Fx.Fade(ice.Light, env * 0.8f), depth: true);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Poison Fang: purple fangs close on the target and poison drips from them.</summary>
    private static void PoisonFang(in FxCtx c, FxColors col, FxList fx)
    {
        var poison = ColorsOf(PokemonType.Poison);
        FangPair(c, fx, I - 0.15f, 0.36f, Rgb(172, 80, 214), Rgb(236, 190, 250));
        if (c.Cue.Landed)
        {
            Drips(fx, c.Age, c.Seed, c.To + Vector3.UnitY * c.ToSize * 0.3f, c.ToSize, I, 5, poison.Light);
            Tint(c, fx, I, 1.0f, Rgb(150, 70, 200), 0.38f);
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.7f, 6, FxShape.Bubble, poison.Main, poison.Light, 0.08f, 0.6f, -0.6f, FxBlend.Alpha, salt: 108);
            fx.Shake = Math.Max(fx.Shake, 0.28f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.08f, I + 0.22f));
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Poison Jab: two purple fists jab in quick turns and droplets fly.</summary>
    private static void PoisonJab(in FxCtx c, FxColors col, FxList fx)
    {
        var poison = ColorsOf(PokemonType.Poison);
        Strike(c, fx, FxShape.Fist, new Vector3(-1.3f, 0.35f, 0.1f), I - 0.1f, 0.42f, Rgb(190, 110, 240), -0.3f, 0.2f);
        Strike(c, fx, FxShape.Fist, new Vector3(-1.3f, -0.25f, 0.1f), I, 0.5f, Rgb(176, 96, 230), 0.1f, 0.2f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I - 0.1f, 0.5f, 5, FxShape.Drop, poison.Main, poison.Light, 0.08f, 0.9f, 1.4f, FxBlend.Alpha, salt: 109);
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.55f, 7, FxShape.Drop, poison.Main, poison.Light, 0.09f, 1.0f, 1.4f, FxBlend.Alpha, salt: 110);
            Tint(c, fx, I, 0.95f, Rgb(150, 70, 200), 0.32f);
            fx.Shake = Math.Max(fx.Shake, 0.32f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.26f));
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Signal Beam: a beam that changes red, green and yellow, the target flashing red and green.</summary>
    private static void SignalBeam(in FxCtx c, FxColors col, FxList fx)
    {
        var tones = new[] { Rgb(255, 90, 90), Rgb(255, 232, 90), Rgb(120, 255, 140) };
        int step = (int)(c.Age * 14f);
        var tone = tones[step % 3];
        var next = tones[(step + 1) % 3];
        Fx.Beam(fx, c, 0.06f, 0.62f, 0.11f, tone, Fx.Mix(tone, White, 0.5f), 8);
        if (c.Age > 0.12f && c.Age < 0.6f)
            for (int i = 0; i < 3; i++)
                fx.Sprite(FxShape.Dot, Vector3.Lerp(c.From, c.Aim, (c.Age * 2f + i / 3f) % 1f), c.FromSize * 0.07f, Fx.Fade(tones[(step + i) % 3], 0.95f), FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            Tint(c, fx, I, I + 0.28f, tones[0], 0.5f);
            Tint(c, fx, I + 0.2f, I + 0.48f, tones[2], 0.5f);
            Tint(c, fx, I + 0.4f, I + 0.68f, tones[0], 0.5f);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Sludge Bomb: a great ball of sludge is lobbed over, bursts in splatter and runs down the target.</summary>
    private static void SludgeBomb(in FxCtx c, FxColors col, FxList fx)
    {
        var poison = ColorsOf(PokemonType.Poison);
        var sludge = Rgb(104, 44, 128);
        float k = c.Age / I;
        if (k >= 0f && k < 1f)
        {
            var p = Lob(c.From, c.Aim, Fx.EaseOut(k) * 0.4f + k * 0.6f, 0.06f);
            float s = c.FromSize * 0.4f;
            fx.Sprite(FxShape.Smoke, p, s, Fx.Fade(sludge, 0.95f), FxBlend.Alpha, c.Age * 4f);
            fx.Sprite(FxShape.Bubble, p + new Vector3(0.05f, 0.05f, 0.1f) * s, s * 0.7f, Fx.Fade(poison.Light, 0.9f));
            for (int j = 1; j <= 3; j++)
                fx.Sprite(FxShape.Drop, Lob(c.From, c.Aim, Math.Max(0f, k - 0.06f * j), 0.06f) - Vector3.UnitY * s * 0.3f * j, s * 0.3f, Fx.Fade(poison.Main, 0.7f - j * 0.15f), FxBlend.Alpha, MathF.PI);
        }
        if (c.Cue.Landed)
        {
            float h = c.ToSize;
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 10, FxShape.Drop, sludge, poison.Main, 0.11f, 1.0f, 1.2f, FxBlend.Alpha, salt: 111);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 6, FxShape.Bubble, poison.Main, poison.Light, 0.09f, 0.7f, -0.4f, FxBlend.Alpha, salt: 112);
            Drips(fx, c.Age, c.Seed, c.To + Vector3.UnitY * h * 0.4f, h, I + 0.05f, 6, sludge);
            Tint(c, fx, I, 1.1f, sludge, 0.45f);
            fx.Shake = Math.Max(fx.Shake, 0.35f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.28f));
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Tri Attack: a red, a yellow and a blue orb gather round the user and are flown in one after another, each bursting in its own colour.</summary>
    private static void TriAttack(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var tones = new[] { Rgb(255, 100, 60), Rgb(255, 226, 70), Rgb(110, 190, 255) };
        for (int i = 0; i < 3; i++)
        {
            float arrive = I - (2 - i) * 0.1f;
            float launch = arrive - 0.14f;
            float a = MathF.PI / 2f + i * MathF.Tau / 3f;
            var home = c.FromFeet + new Vector3(MathF.Cos(a) * 0.6f, 1.0f + MathF.Sin(a) * 0.4f, 0.1f) * h;
            float gather = Fx.Envelope(c.Age, i * 0.03f, i * 0.03f + 0.08f, launch, launch + 0.01f);
            if (gather > 0f)
            {
                fx.Sprite(FxShape.Glow, home, h * 0.3f, Fx.Fade(tones[i], gather * 0.6f));
                fx.Sprite(FxShape.Orb, home, h * 0.12f, Fx.Fade(tones[i], gather), FxBlend.Alpha, c.Age * 4f);
            }
            float k = (c.Age - launch) / 0.14f;
            if (k >= 0f && k <= 1f)
            {
                var p = Vector3.Lerp(home, c.Aim, k);
                fx.Sprite(FxShape.Glow, p, h * 0.34f, Fx.Fade(tones[i], 0.7f));
                fx.Sprite(FxShape.Orb, p, h * 0.13f, Fx.Fade(tones[i], 1f), FxBlend.Alpha, c.Age * 6f);
            }
            if (!c.Cue.Landed) continue;
            float t = c.ToSize;
            if (i == 0) Fx.Burst(fx, c.Seed, c.Age, c.To, t, arrive, 0.5f, 7, FxShape.Flame, tones[0], Rgb(255, 200, 120), 0.14f, 0.8f, -0.7f, salt: 113);
            else if (i == 1) Fx.Bolt(fx, c.Age, c.Seed + 5, c.To + Vector3.UnitY * t * 0.9f, c.ToFeet, arrive, arrive + 0.25f, t * 0.04f, tones[1], White, 1);
            else Fx.Burst(fx, c.Seed, c.Age, c.To, t, arrive, 0.55f, 8, FxShape.Shard, tones[2], Rgb(220, 244, 255), 0.11f, 0.9f, 1.0f, FxBlend.Alpha, spin: 4f, salt: 114);
            fx.Shake = Math.Max(fx.Shake, 0.22f * Fx.Envelope(c.Age, arrive, arrive + 0.02f, arrive + 0.06f, arrive + 0.18f));
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Memento: dark wisps stream out of the user to wrap the target, and two blue arrows fall over it.</summary>
    private static void Memento(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var dark = Rgb(70, 48, 108);
        var ghost = ColorsOf(PokemonType.Dark);
        float h = c.FromSize;
        float aura = Fx.Envelope(c.Age, 0f, 0.1f, 0.3f, 0.5f);
        if (aura > 0f) fx.Sprite(FxShape.Glow, c.From, h * 0.95f, Fx.Fade(dark, aura * 0.55f), FxBlend.Alpha);
        Fx.Stream(fx, c, 0.02f, I, 0.3f, 8, FxShape.Wisp, dark, ghost.Light, 0.2f, arc: 0.02f, spread: 0.5f, spin: 3f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0.02f, I, 0.3f, 8, FxShape.Smoke, Rgb(40, 28, 66, 220), Rgb(70, 48, 108, 200), 0.16f, arc: 0.02f, spread: 0.5f, blend: FxBlend.Alpha);
        if (c.Cue.Missed) return;
        float t = c.ToSize;
        float wrap = Fx.Envelope(c.Age, I, I + 0.1f, 0.9f, 1.2f);
        if (wrap > 0f)
        {
            fx.Sprite(FxShape.Smoke, c.To, t * 0.85f, Fx.Fade(dark, wrap * 0.5f), FxBlend.Alpha, c.Age * 2f);
            for (int i = 0; i < 2; i++)
            {
                float k = (c.Age - I - 0.05f - i * 0.2f) / 0.55f;
                if (k < 0f || k > 1f) continue;
                fx.Sprite(FxShape.Arrow, c.To + new Vector3((i - 0.5f) * 0.5f, 1.0f - 0.45f * k, 0.1f) * t, t * 0.24f, Fx.Fade(Rgb(110, 160, 255), Math.Min(1f, (1f - k) * 3f)), FxBlend.Alpha, MathF.PI);
            }
        }
    }

    /// <summary>Drill Peck: a beak turns like a drill and bores in, and a ring of eight pecks lands about the target.</summary>
    private static void DrillPeck(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var beak = Rgb(255, 214, 120);
        var dir = new Vector3(1.2f, -0.5f, 0f);
        Strike(c, fx, FxShape.Beak, new Vector3(-1.2f, 0.5f, 0.1f), I, 0.4f, beak, MathF.Atan2(dir.Y, dir.X), 0.2f);
        float k = (c.Age - (I - 0.2f)) / 0.2f;
        if (k > 0f && k < 1.5f && !c.Cue.Missed)
        {
            float u = Math.Min(1f, k);
            var tip = c.To + new Vector3(-1.2f, 0.5f, 0.1f) * h * (1f - Fx.EaseOut(u));
            fx.Sprite(FxShape.Swirl, tip, h * 0.4f, Fx.Fade(White, 0.8f * (k < 1f ? 1f : 1.5f - k)), FxBlend.Alpha, c.Age * 30f);
        }
        if (c.Cue.Landed)
            for (int i = 0; i < 8; i++)
            {
                float a = i * MathF.Tau / 8f;
                var at = c.To + new Vector3(MathF.Cos(a) * 0.55f, MathF.Sin(a) * 0.45f, 0.1f) * h;
                Fx.Contact(fx, c.Age, c.Seed + i, at, h, I + i * 0.02f, 0.13f, Rgb(255, 214, 120), White);
            }
        if (c.Cue.Landed) fx.Shake = Math.Max(fx.Shake, 0.4f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.14f, I + 0.32f));
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Ice Punch: a fist of ice, snowflakes round it, and frost settling on the target.</summary>
    private static void IcePunch(in FxCtx c, FxColors col, FxList fx)
    {
        var ice = ColorsOf(PokemonType.Ice);
        float h = c.ToSize;
        Strike(c, fx, FxShape.Fist, new Vector3(-1.4f, 0.2f, 0.1f), I, 0.5f, Rgb(196, 234, 255), -0.15f, 0.25f);
        float k = (c.Age - (I - 0.25f)) / 0.25f;
        if (k > 0f && k < 1.6f)
            for (int i = 0; i < 4; i++)
            {
                float a = i * MathF.Tau / 4f + c.Age * 6f;
                fx.Sprite(FxShape.Snowflake, c.To + new Vector3(-1.3f * (1f - Fx.EaseOut(Math.Min(1f, k))) + MathF.Cos(a) * 0.35f, 0.2f * (1f - Math.Min(1f, k)) + MathF.Sin(a) * 0.35f, 0.15f) * h, h * 0.09f, Fx.Fade(White, k < 1f ? 1f : 1.6f - k), FxBlend.Alpha, a);
            }
        if (c.Cue.Landed)
        {
            Tint(c, fx, I, 1.1f, Rgb(170, 225, 255), 0.42f);
            Fx.Rain(fx, c.Age, c.Seed, c.To, h, I, 1.2f, 0.5f, 10, FxShape.Snowflake, White, ice.Light, 0.06f, FxBlend.Alpha);
            fx.Shake = Math.Max(fx.Shake, 0.3f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.08f, I + 0.24f));
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Gyro Ball: a steel ball is spun up in two tilted rings and driven into the target.</summary>
    private static void GyroBall(in FxCtx c, FxColors col, FxList fx)
    {
        var steel = ColorsOf(PokemonType.Steel);
        float h = c.FromSize;
        var front = c.From + new Vector3(0.7f, 0.1f, 0.1f) * h;
        float build = Math.Clamp(c.Age / 0.25f, 0f, 1f);
        float fly = Math.Clamp((c.Age - 0.25f) / (I - 0.25f), 0f, 1f);
        float env = Fx.Envelope(c.Age, 0f, 0.05f, I, I + 0.05f);
        if (env > 0f)
        {
            var p = Vector3.Lerp(front, c.Aim, fly * fly);
            float spin = c.Age * c.Age * 120f;
            float size = h * (0.1f + 0.16f * build);
            fx.Sprite(FxShape.Glow, p, size * 2f, Fx.Fade(steel.Light, env * 0.5f));
            fx.Sprite(FxShape.Orb, p, size, Fx.Fade(Rgb(190, 202, 220), env), FxBlend.Alpha, spin);
            for (int i = 0; i < 4; i++)
                fx.Sprite(FxShape.Wave, p, size * 1.5f, Fx.Fade(White, env * 0.8f), FxBlend.Alpha, spin * (i % 2 == 0 ? 1f : -1.3f) + i * MathF.PI / 2f);
            if (fly > 0f)
                fx.Sprite(FxShape.Swirl, p, size * 2.2f, Fx.Fade(steel.Light, env * 0.5f), FxBlend.Alpha, spin);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.5f, 10, FxShape.Spark, steel.Light, White, 0.1f, 1.1f, salt: 115);
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.35f, c.ToSize * 1.2f, Fx.Fade(steel.Light, 0.9f));
            fx.Shake = Math.Max(fx.Shake, 0.45f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.3f));
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>X-Scissor: two long green blades cross in an X on the target.</summary>
    private static void XScissor(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var lime = Rgb(150, 222, 76);
        if (!c.Cue.Missed)
            for (int b = 0; b < 2; b++)
            {
                float t0 = I - 0.27f + b * 0.12f;
                float k = (c.Age - t0) / 0.3f;
                if (k < 0f || k > 1f) continue;
                float grow = Fx.EaseOut(Math.Min(1f, k * 2.4f));
                float hold = k < 0.5f ? 1f : (1f - k) / 0.5f;
                float tilt = b == 0 ? 0.75f : -0.75f;
                var along = new Vector3(MathF.Cos(tilt), MathF.Sin(tilt), 0f);
                fx.Streak(FxShape.Streak, c.To + Vector3.UnitZ * 0.1f, along, h * 0.36f, 4f * grow, Fx.Fade(lime, hold), FxBlend.Alpha);
                fx.Streak(FxShape.Streak, c.To + Vector3.UnitZ * 0.12f, along, h * 0.14f, 4f * grow, Fx.Fade(White, hold));
            }
        if (c.Cue.Landed)
        {
            Fx.Glint(fx, c.Age, c.To, h, I - 0.04f, White, 0.7f);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 6, FxShape.Leaf, lime, Rgb(220, 250, 160), 0.08f, 0.9f, 0.4f, FxBlend.Alpha, spin: 7f, salt: 116);
            fx.Shake = Math.Max(fx.Shake, 0.3f * Fx.Envelope(c.Age, I - 0.02f, I, I + 0.08f, I + 0.24f));
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Horn Drill: a horn bores in three times, spirals turning round it, the last the greatest.</summary>
    private static void HornDrill(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Jabs(c, fx, 3, 0.1f, FxShape.Horn, Rgb(240, 230, 204));
        if (!c.Cue.Missed)
        {
            var back = -Vector3.Normalize(c.To - c.From);
            for (int i = 0; i < 3; i++)
            {
                float at = I - (2 - i) * 0.1f;
                float k = (c.Age - (at - 0.07f)) / 0.2f;
                if (k < 0f || k > 1f) continue;
                fx.Sprite(FxShape.Swirl, c.To + back * h * 0.4f, h * (0.35f + 0.1f * i), Fx.Fade(White, (1f - k) * 0.8f), FxBlend.Alpha, c.Age * 26f);
            }
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.4f, h * 1.3f, Fx.Fade(White, 0.9f));
            if (c.Cue.Landed) fx.Shake = Math.Max(fx.Shake, 0.5f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.3f));
        }
        Impact(c, col, fx, 1.15f);
    }

    /// <summary>Last Resort: four orbs of the user's moves are drawn in and flown together, and two heavy blows land, the second the greater.</summary>
    private static void LastResort(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var tones = new[] { Rgb(255, 96, 80), Rgb(100, 160, 255), Rgb(255, 226, 96), Rgb(120, 230, 130) };
        // The four orbs circle the user (0 to 0.18), then fly in together to land with the second, heavier blow
        float gather = Fx.Envelope(c.Age, 0f, 0.06f, 0.16f, 0.24f);
        float fly = Math.Clamp((c.Age - 0.18f) / (I - 0.18f), 0f, 1f);
        float env = Fx.Envelope(c.Age, 0f, 0.05f, I - 0.01f, I);
        if (env > 0f)
            for (int i = 0; i < 4; i++)
            {
                float a = c.Age * 14f + i * MathF.Tau / 4f;
                var home = c.From + new Vector3(MathF.Cos(a) * 0.7f, MathF.Sin(a) * 0.5f, 0.1f) * h;
                var p = Vector3.Lerp(home, c.Aim, fly * fly);
                fx.Sprite(FxShape.Glow, p, h * 0.34f, Fx.Fade(tones[i], env * 0.6f));
                fx.Sprite(FxShape.Orb, p, h * 0.15f, Fx.Fade(tones[i], env), FxBlend.Alpha, a);
            }
        if (gather > 0f) fx.Sprite(FxShape.Glow, c.From, h * 1.0f, Fx.Fade(Rgb(255, 236, 160), gather * 0.4f), FxBlend.Alpha);
        Blows(c, col, fx, 2, 0.23f, 0.36f, 0.3f);
        if (c.Cue.Landed)
        {
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.4f, c.ToSize * 1.3f, Fx.Fade(White, 0.9f));
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.5f, 8, FxShape.Star, White, Rgb(255, 232, 150), 0.1f, 1.0f, 0.8f, FxBlend.Alpha, spin: 6f, salt: 117);
        }
        Impact(c, col, fx, 1.2f);
    }

    /// <summary>Magnet Rise: red and blue field arcs stand either side of the user, arrows rise and the ring on the ground beneath narrows.</summary>
    private static void MagnetRise(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var red = Rgb(255, 90, 90);
        var blue = Rgb(100, 150, 255);
        float env = Fx.Envelope(c.Age, 0f, 0.12f, 0.95f, 1.25f);
        if (env <= 0f) return;
        for (int i = 0; i < 3; i++)
        {
            float s = h * (0.5f + 0.18f * i);
            fx.Sprite(FxShape.Wave, c.From + new Vector3(-0.45f * h, 0f, 0.1f), s, Fx.Fade(red, env * (1f - i * 0.2f)), FxBlend.Alpha, MathF.PI);
            fx.Sprite(FxShape.Wave, c.From + new Vector3(0.45f * h, 0f, 0.1f), s, Fx.Fade(blue, env * (1f - i * 0.2f)), FxBlend.Alpha, 0f);
        }
        float lift = Fx.EaseOut(Math.Clamp((c.Age - 0.1f) / 0.5f, 0f, 1f));
        fx.Flat(FxShape.Ring, c.FromFeet + Vector3.UnitY * 0.04f, Vector3.UnitY, h * (0.8f - 0.3f * lift), Fx.Fade(Rgb(200, 210, 255), env * 0.8f), depth: true);
        Rising(fx, c.Age, c.Seed, c.FromFeet, h, 0.1f, 1.2f, 4, FxShape.Arrow, Rgb(190, 205, 255), 0.09f, 0.1f);
        Fx.Burst(fx, c.Seed, c.Age, c.From, h, 0.1f, 0.8f, 8, FxShape.Spark, Rgb(255, 240, 160), White, 0.05f, 0.8f, salt: 118);
    }

    /// <summary>Smog: puffs of dark smog are belched over one after another and a thick cloud hangs on the target.</summary>
    private static void Smog(in FxCtx c, FxColors col, FxList fx)
    {
        var smog = Rgb(78, 62, 92);
        var poison = ColorsOf(PokemonType.Poison);
        Fx.Stream(fx, c, 0f, I, 0.26f, 9, FxShape.Smoke, Rgb(70, 56, 86, 235), Rgb(110, 88, 120, 220), 0.2f, arc: 0.05f, spread: 0.4f, spin: 2f, blend: FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            float env = Fx.Envelope(c.Age, I, I + 0.12f, 0.9f, 1.25f);
            for (int i = 0; i < 5 && env > 0f; i++)
                fx.Sprite(FxShape.Smoke, c.To + new Vector3(Fx.Signed(c.Seed, i, 214) * 0.45f, Fx.Signed(c.Seed, i, 215) * 0.35f, 0.1f) * c.ToSize, c.ToSize * 0.55f, Fx.Fade(smog, env * 0.7f), FxBlend.Alpha, i * 1.5f + c.Age);
            Tint(c, fx, I, 1.05f, Rgb(20, 14, 30), 0.3f);
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.7f, 5, FxShape.Bubble, poison.Main, poison.Light, 0.07f, 0.6f, -0.5f, FxBlend.Alpha, salt: 119);
        }
        Impact(c, col, fx, 0.7f);
    }

    /// <summary>Vine Whip: two green vines lash across the target from either side, leaves flying.</summary>
    private static void VineWhip(in FxCtx c, FxColors col, FxList fx)
    {
        var grass = ColorsOf(PokemonType.Grass);
        float h = c.ToSize;
        Whip(fx, c.Age, c.From + new Vector3(0.1f, 0.35f, 0.1f) * c.FromSize, c.Aim + new Vector3(0f, 0.25f * h, 0f), I - 0.2f, 0.2f, 0.07f, 0.04f, h * 0.24f, grass.Main, Rgb(200, 245, 150), FxShape.Leaf, h * 0.2f);
        Whip(fx, c.Age, c.From + new Vector3(0.1f, -0.1f, 0.1f) * c.FromSize, c.Aim - new Vector3(0f, 0.2f * h, 0f), I - 0.1f, 0.1f, -0.05f, 0.04f, h * 0.24f, Rgb(70, 150, 60), Rgb(200, 245, 150), FxShape.Leaf, h * 0.2f, 1f);
        if (c.Cue.Landed)
        {
            Fx.Glint(fx, c.Age, c.To, h, I - 0.02f, White, 0.45f);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 8, FxShape.Leaf, grass.Main, grass.Light, 0.1f, 0.95f, 0.6f, FxBlend.Alpha, spin: 8f, salt: 120);
            fx.Shake = Math.Max(fx.Shake, 0.28f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.08f, I + 0.22f));
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Dream Eater: a pink dream floats over the target and is drawn into the user's mouth, the user glowing.</summary>
    private static void DreamEater(in FxCtx c, FxColors col, FxList fx)
    {
        var pink = Rgb(255, 170, 214);
        var pale = Rgb(255, 224, 242);
        float h = c.ToSize;
        if (!c.Cue.Missed)
        {
            float bubble = Fx.Envelope(c.Age, 0f, 0.12f, I, I + 0.1f);
            var dream = c.ToFeet + new Vector3(0.2f, 1.15f, 0f) * h;
            if (bubble > 0f)
            {
                fx.Sprite(FxShape.Smoke, dream, h * 0.4f, Fx.Fade(pink, bubble * 0.55f), FxBlend.Alpha, c.Age);
                fx.Sprite(FxShape.Bubble, dream, h * 0.35f, Fx.Fade(pale, bubble * 0.8f));
                fx.Sprite(FxShape.Zzz, dream, h * 0.14f, Fx.Fade(White, bubble), FxBlend.Alpha, 0.2f);
            }
        }
        var mouth = c.FromFeet + Vector3.UnitY * c.FromSize * 0.7f;
        float open = Fx.Envelope(c.Age, I - 0.05f, I + 0.05f, 0.7f, 0.95f);
        if (open > 0f) fx.Sprite(FxShape.Ring, mouth, c.FromSize * 0.2f, Fx.Fade(Rgb(70, 30, 90), open), FxBlend.Alpha);
        Impact(c, col, fx, 0.9f);
        if (c.Cue.Landed)
        {
            Fx.Stream(fx, c, I, 1.0f, 0.45f, 8, FxShape.Wisp, pink, pale, 0.14f, arc: 0.1f, spread: 0.5f, spin: 2f, blend: FxBlend.Alpha, reverse: true);
            Fx.Stream(fx, c, I, 1.0f, 0.45f, 3, FxShape.Zzz, White, pale, 0.1f, arc: 0.1f, spread: 0.4f, blend: FxBlend.Alpha, reverse: true);
            float glow = Fx.Envelope(c.Age, 0.7f, 0.85f, 1.05f, 1.28f);
            if (glow > 0f) fx.Sprite(FxShape.Glow, c.From, c.FromSize * 0.9f, Fx.Fade(Rgb(190, 255, 190), glow * 0.4f), FxBlend.Alpha);
            Fx.Glint(fx, c.Age, c.From, c.FromSize, 0.85f, Rgb(220, 255, 220), 0.5f);
        }
    }

    /// <summary>Grass Whistle: green notes and spinning leaves ride rings of sound to the target, and sleep gathers on it.</summary>
    private static void GrassWhistle(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var green = Rgb(130, 222, 110);
        var pale = Rgb(214, 250, 176);
        var mouth = c.FromFeet + Vector3.UnitY * c.FromSize * 0.7f;
        Fx.Glint(fx, c.Age, mouth, c.FromSize, 0f, pale, 0.3f);
        Fx.Stream(fx, c, 0.02f, I, 0.34f, 7, FxShape.Note, green, pale, 0.14f, arc: 0.03f, spread: 0.5f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0.04f, I, 0.34f, 9, FxShape.Leaf, Rgb(88, 176, 70), green, 0.09f, arc: 0.02f, spread: 0.7f, spin: 9f, blend: FxBlend.Alpha);
        Fx.Waves(fx, c, 0.02f, 0.6f, 3, FxShape.Ring, Fx.Fade(green, 0.9f), 0.4f, 0.3f);
        if (c.Cue.Missed) return;
        float h = c.ToSize;
        float sleep = Fx.Envelope(c.Age, I, I + 0.1f, 0.9f, 1.25f);
        for (int i = 0; i < 3 && sleep > 0f; i++)
        {
            float u = (c.Age * 0.9f + i / 3f) % 1f;
            fx.Sprite(FxShape.Zzz, c.ToFeet + new Vector3(0.2f * h + MathF.Sin(u * 6f + i) * 0.1f * h, h * (0.9f + 0.5f * u), 0f), h * (0.1f + 0.08f * u), Fx.Fade(pale, sleep * MathF.Sin(u * MathF.PI)), FxBlend.Alpha);
        }
    }

    /// <summary>Reversal: the user's aura flares white twice, rays flung out, and two blows land, the second the heavier.</summary>
    private static void Reversal(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        foreach (float t0 in new[] { 0f, 0.15f })
        {
            float env = Fx.Envelope(c.Age, t0, t0 + 0.05f, t0 + 0.08f, t0 + 0.17f);
            if (env <= 0f) continue;
            fx.Sprite(FxShape.Glow, c.From, h * 1.2f, Fx.Fade(Rgb(255, 240, 220), env * 0.7f), FxBlend.Alpha);
            for (int i = 0; i < 8; i++)
            {
                float a = i * MathF.Tau / 8f + t0 * 3f;
                fx.Streak(FxShape.Streak, c.From + new Vector3(MathF.Cos(a), MathF.Sin(a), 0f) * h * 0.75f, new Vector3(MathF.Cos(a), MathF.Sin(a), 0f), h * 0.09f, 5f, Fx.Fade(Rgb(255, 190, 130), env));
            }
        }
        Strike(c, fx, FxShape.Fist, new Vector3(-1.3f, 0.2f, 0.1f), I - 0.12f, 0.4f, Rgb(255, 214, 170), -0.15f, 0.2f);
        Strike(c, fx, FxShape.Fist, new Vector3(-1.5f, 0.1f, 0.1f), I, 0.56f, Rgb(255, 180, 120), -0.1f, 0.2f);
        Blows(c, col, fx, 2, 0.12f, 0.36f, 0.4f);
        Impact(c, col, fx, 1.15f);
    }
}
