using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

// Plan 25 · Y7, its second part (Y7b): the moves the story shows from Route 206 to Route 208, in MoveSightings'
// order, each a look of its own. The beats are the original's, read from each move's animation script (what comes
// first, how often it hits, when it lands, who shakes); the shapes, colours and timings are our own. Every recipe
// lands at ImpactTime (FxList.Land, which Impact sets), is over by EffectTime and stays under MaxQuads.
internal static partial class MoveFx
{
    private static Dictionary<string, FxRecipe> Platinum3() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Double Team"] = DoubleTeam,
        ["Endeavor"] = Endeavor,
        ["Flame Wheel"] = FlameWheelY7b,
        ["Fury Cutter"] = FuryCutter,
        ["Knock Off"] = KnockOff,
        ["Magnitude"] = Magnitude,
        ["Roar"] = Roar,
        ["Seismic Toss"] = SeismicToss,
        ["Self-Destruct"] = SelfDestructY7b,
        ["Slam"] = Slam,
        ["Stomp"] = Stomp,
        ["Block"] = Block,
        ["Detect"] = Detect,
        ["Mind Reader"] = MindReader,
        ["Disable"] = Disable,
        ["Dragon Rage"] = DragonRage,
        ["Extrasensory"] = Extrasensory,
        ["Imprison"] = Imprison,
        ["Sand Tomb"] = SandTomb,
        ["Sandstorm"] = SandstormY7b,
        ["Tickle"] = Tickle,
        ["Uproar"] = Uproar,
        ["Double Slap"] = DoubleSlap,
        ["Follow Me"] = FollowMe,
        ["Sing"] = SingY7b,
        ["Bug Bite"] = BugBite,
        ["Encore"] = Encore,
        ["Explosion"] = ExplosionY7b,
        ["Flail"] = Flail,
        ["Ice Fang"] = IceFang,
        ["Lucky Chant"] = LuckyChant,
        ["Mimic"] = Mimic,
        ["Rest"] = RestY7b,
        ["Revenge"] = Revenge,
        ["Rock Blast"] = RockBlast,
        ["Snore"] = Snore,
        ["Sweet Scent"] = SweetScent,
        ["Twister"] = TwisterY7b,
        ["Worry Seed"] = WorrySeed
    };

    // ------------------------------------------------------------------ shared by the batch

    /// <summary>A flat ring on the ground at <paramref name="feet"/>, growing out from the foot of something and fading.</summary>
    private static void GroundRing(FxList fx, float age, Vector3 feet, float t0, float life, float radius, Color color) =>
        Fx.Shockwave(fx, age, feet + Vector3.UnitY * 0.03f, Vector3.UnitY, t0, life, radius, color);

    /// <summary>Blows landing on the target one after another, each a small flash and a jolt: the last at the landing's own moment.</summary>
    private static void Blows(in FxCtx c, FxColors col, FxList fx, int count, float gap, float size, float jolt)
    {
        if (!c.Cue.Landed) return;
        for (int i = 0; i < count; i++)
        {
            float at = I - (count - 1 - i) * gap;
            var off = new Vector3(Fx.Signed(c.Seed, i, 90), Fx.Signed(c.Seed, i, 91), 0f) * c.ToSize * 0.2f;
            Fx.Contact(fx, c.Age, c.Seed + i, c.To + off, c.ToSize, at, size, col.Main, col.Light);
            fx.Shake = Math.Max(fx.Shake, jolt * Fx.Envelope(c.Age, at, at + 0.02f, at + 0.04f, at + 0.12f));
        }
    }

    /// <summary>A pair of fangs closing on the target from above and below, the way Bite's do, in <paramref name="color"/>.</summary>
    private static void FangPair(in FxCtx c, FxList fx, float t0, float size, Color color, Color rim)
    {
        float k = (c.Age - t0) / 0.3f;
        if (k < 0f || k > 1f) return;
        float close = Fx.EaseOut(Math.Min(1f, k * 1.9f));
        float gap = c.ToSize * 0.42f * (1f - close);
        float fade = 1f - Math.Max(0f, k - 0.6f) / 0.4f;
        for (int i = -1; i <= 1; i += 2)
        {
            var x = Vector3.UnitX * i * c.ToSize * 0.13f;
            fx.Sprite(FxShape.Fang, c.To + x + Vector3.UnitY * (gap + c.ToSize * 0.1f), c.ToSize * size, Fx.Fade(color, fade), FxBlend.Alpha);
            fx.Sprite(FxShape.Fang, c.To + x + Vector3.UnitY * (gap + c.ToSize * 0.1f), c.ToSize * size * 1.1f, Fx.Fade(rim, fade * 0.35f));
            fx.Sprite(FxShape.Fang, c.To + x - Vector3.UnitY * (gap + c.ToSize * 0.1f), c.ToSize * size, Fx.Fade(color, fade), FxBlend.Alpha, MathF.PI);
            fx.Sprite(FxShape.Fang, c.To + x - Vector3.UnitY * (gap + c.ToSize * 0.1f), c.ToSize * size * 1.1f, Fx.Fade(rim, fade * 0.35f), rotation: MathF.PI);
        }
    }

    /// <summary>Things (Z's, notes) rising from a point and fading as they climb: <paramref name="count"/> of them in turn.</summary>
    private static void Rising(FxList fx, float age, int seed, Vector3 from, float h, float t0, float t1, int count, FxShape shape, Color color, float size, float sway, float turn = 0f)
    {
        // Each lives 2.4 spans and starts 0.6 of a span after the one before, the last ending at t1
        float span = (t1 - t0) / (0.6f * (count - 1) + 2.4f);
        for (int i = 0; i < count; i++)
        {
            float k = (age - t0 - i * span * 0.6f) / (span * 2.4f);
            if (k < 0f || k > 1f) continue;
            var p = from + new Vector3(MathF.Sin(k * 6f + i * 2f) * h * sway + h * 0.12f * k, h * (0.15f + 0.9f * k), 0f);
            fx.Sprite(shape, p, h * size * (0.55f + 0.7f * k), Fx.Fade(color, MathF.Sin(k * MathF.PI)), FxBlend.Alpha, Fx.Signed(seed, i, 92) * 0.3f + turn);
        }
    }

    // ------------------------------------------------------------------ the recipes

    /// <summary>Double Team: four pale afterimages slide out either side of the user, swaying, and come home.</summary>
    private static void DoubleTeam(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.08f, 0.85f, 1.15f);
        if (env <= 0f) return;
        float out0 = Fx.EaseOut(Math.Min(1f, c.Age / 0.3f));
        float home = Fx.EaseOut(Math.Clamp((c.Age - 0.75f) / 0.3f, 0f, 1f));
        float spread = out0 * (1f - home);
        for (int i = 0; i < 4; i++)
        {
            float side = i < 2 ? -1f : 1f;
            float far = i % 2 == 0 ? 0.55f : 1.1f;
            float sway = MathF.Sin(c.Age * 9f + i * 1.7f) * 0.06f;
            var p = c.From + new Vector3(side * (far + sway) * h * spread, 0f, (i % 2 == 0 ? 0.06f : -0.1f) * h);
            float a = env * (i % 2 == 0 ? 0.34f : 0.24f);
            fx.Sprite(FxShape.Glow, p, h * 0.95f, Fx.Fade(Rgb(226, 232, 255), a), FxBlend.Alpha);
            fx.Sprite(FxShape.Ring, p, h * 0.5f, Fx.Fade(White, a * 0.9f));
            fx.Streak(FxShape.Streak, c.From + new Vector3(side * far * h * 0.5f * spread, (i % 2 == 0 ? 0.15f : -0.15f) * h, 0f), Vector3.UnitX, h * 0.05f, 6f * spread + 0.1f, Fx.Fade(White, a * 1.4f));
        }
        Fx.Glint(fx, c.Age, c.From + Vector3.UnitY * h * 0.2f, h, 0.1f, White, 0.4f);
        Fx.Glint(fx, c.Age, c.From + Vector3.UnitY * h * 0.2f, h, 0.75f, White, 0.35f);
    }

    /// <summary>Endeavor: the user flares yellow four times as four quick blows land.</summary>
    private static void Endeavor(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var yellow = Rgb(255, 230, 90);
        for (int k = 0; k < 4; k++)
        {
            float t0 = k * 0.1f;
            float pulse = Fx.Envelope(c.Age, t0, t0 + 0.03f, t0 + 0.05f, t0 + 0.1f);
            if (pulse > 0f) fx.Sprite(FxShape.Glow, c.From, h * (0.8f + 0.1f * k), Fx.Fade(yellow, pulse * 0.32f));
        }
        SpeedLines(c, fx, 0.05f, 0.3f, 4, 0.45f);
        Blows(c, col, fx, 4, 0.08f, 0.2f, 0.18f);
        if (c.Cue.Landed)
            for (int i = 0; i < 4; i++)
                fx.Sprite(FxShape.Impact, c.To + new Vector3(Fx.Signed(c.Seed, i, 93), Fx.Signed(c.Seed, i, 94), 0.1f) * c.ToSize * 0.22f, c.ToSize * 0.2f,
                    Fx.Fade(Rgb(255, 244, 170), Fx.Envelope(c.Age, I - (3 - i) * 0.08f, I - (3 - i) * 0.08f + 0.02f, I - (3 - i) * 0.08f + 0.06f, I - (3 - i) * 0.08f + 0.2f)), FxBlend.Alpha, i * 1.1f);
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Flame Wheel: a wheel of flame winds up round the user, rolls across and breaks into fire on the target.</summary>
    private static void FlameWheelY7b(in FxCtx c, FxColors col, FxList fx)
    {
        var fire = ColorsOf(PokemonType.Fire);
        float roll = Fx.EaseOut(Math.Clamp((c.Age - 0.12f) / (I - 0.12f), 0f, 1f));
        float env = Fx.Envelope(c.Age, 0f, 0.06f, I, I + 0.14f);
        if (env > 0f)
        {
            var at = Vector3.Lerp(c.From, c.Aim, roll);
            float size = c.ScaleAt(roll) * (0.4f + 0.25f * Math.Min(1f, c.Age / 0.12f));
            for (int i = 0; i < 8; i++)
            {
                float a = i * MathF.Tau / 8f - c.Age * 16f;
                fx.Sprite(FxShape.Flame, at + new Vector3(MathF.Cos(a), MathF.Sin(a), 0f) * size, size * 0.7f, Fx.Fade(i % 2 == 0 ? fire.Main : Rgb(255, 170, 60), env), FxBlend.Alpha, a - MathF.PI / 2f);
            }
            fx.Sprite(FxShape.Ring, at, size * 1.9f, Fx.Fade(fire.Main, env * 0.8f), rotation: c.Age * 5f);
            fx.Sprite(FxShape.Glow, at, size * 1.8f, Fx.Fade(fire.Dark, env * 0.3f), FxBlend.Alpha);
            for (int j = 1; j <= 4; j++)
            {
                float back = Math.Max(0f, roll - j * 0.09f);
                fx.Sprite(FxShape.Ember, Vector3.Lerp(c.From, c.Aim, back) + Vector3.UnitY * size * 0.2f * j, size * 0.22f, Fx.Fade(fire.Light, env * (1f - j * 0.2f)));
            }
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.55f, 9, FxShape.Flame, fire.Main, fire.Light, 0.2f, 0.9f, -0.3f, salt: 71);
            fx.Sprite(FxShape.Glow, c.To, c.ToSize * 1.1f, Fx.Fade(Rgb(255, 80, 40), 0.45f * Fx.Envelope(c.Age, I, I + 0.04f, I + 0.15f, I + 0.45f)), FxBlend.Alpha);
        }
        Impact(c, col, fx);
    }

    /// <summary>Fury Cutter: three cuts one after another across the target, each longer and brighter, green scraps flying.</summary>
    private static void FuryCutter(in FxCtx c, FxColors col, FxList fx)
    {
        if (c.Cue.Landed)
        {
            var bug = ColorsOf(PokemonType.Bug);
            float h = c.ToSize;
            int n = 0;
            foreach (var (t0, tilt, wide) in new[] { (I - 0.12f, 0.55f, 0.16f), (I - 0.03f, -0.55f, 0.22f), (I + 0.07f, 0.05f, 0.3f) })
            {
                float k = (c.Age - t0) / 0.3f;
                n++;
                if (k < 0f || k > 1f) continue;
                var along = Vector3.Normalize(new Vector3(1f, -tilt, 0f));
                float grow = 9f * Fx.EaseOut(k * 3f);
                fx.Streak(FxShape.Streak, c.To, along, h * wide, grow, Fx.Fade(bug.Main, (1f - k) * 0.9f), FxBlend.Alpha);
                fx.Streak(FxShape.Streak, c.To, along, h * wide * 0.4f, grow, Fx.Fade(White, 1f - k));
            }
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I - 0.03f, 0.55f, 7, FxShape.Shard, bug.Light, White, 0.08f, 0.9f, 1.4f, FxBlend.Alpha, spin: 7f, salt: 72);
            fx.Shake = Math.Max(fx.Shake, 0.3f * Fx.Envelope(c.Age, I - 0.03f, I, I + 0.1f, I + 0.25f));
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Knock Off: a fist driven up under the target's head and the thing it held popping off, spinning away.</summary>
    private static void KnockOff(in FxCtx c, FxColors col, FxList fx)
    {
        Strike(c, fx, FxShape.Fist, new Vector3(-0.55f, -0.7f, 0.2f), I, 0.34f, Rgb(255, 214, 170), -0.4f, 0.14f);
        if (c.Cue.Landed)
        {
            float h = c.ToSize;
            float t = c.Age - (I + 0.02f);
            if (t >= 0f && t < 0.8f)
            {
                var p = c.To + new Vector3(0.1f + 1.15f * t, 0.5f + 1.7f * t - 3.0f * t * t, 0.15f) * h;
                float fade = Math.Clamp((0.8f - t) / 0.2f, 0f, 1f);
                fx.Sprite(FxShape.Glow, p, h * 0.3f, Fx.Fade(Rgb(255, 232, 140), fade * 0.7f));
                fx.Sprite(FxShape.Orb, p, h * 0.15f, Fx.Fade(Rgb(250, 238, 200), fade), FxBlend.Alpha, c.Age * 14f);
                if (t < 0.12f) Fx.Glint(fx, c.Age, p, h, I + 0.02f, White, 0.5f);
            }
            Fx.Burst(fx, c.Seed, c.Age, c.To + Vector3.UnitY * h * 0.3f, h, I, 0.4f, 5, FxShape.Spark, Rgb(255, 210, 120), White, 0.08f, 0.8f, salt: 73);
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Magnitude: the ground heaves in three jolts, cracking under the target and throwing up rocks; how hard is the seed's.</summary>
    private static void Magnitude(in FxCtx c, FxColors col, FxList fx)
    {
        float m = 0.45f + 0.55f * Fx.Rand(c.Seed, 0, 95);
        float h = c.ToSize;
        foreach (float at in new[] { 0.05f, 0.2f, I })
        {
            GroundRing(fx, c.Age, c.FromFeet, at, 0.4f, c.FromSize * (1.2f + m), Fx.Fade(Rgb(214, 190, 150), 0.8f));
            Dust(c, fx, c.FromFeet, c.FromSize * (0.8f + 0.5f * m), at, 3);
        }
        if (!c.Cue.Missed)
        {
            float shown = Fx.Envelope(c.Age, I - 0.05f, I, 0.85f, 1.15f);
            if (shown > 0f)
            {
                fx.Flat(FxShape.Crack, c.ToFeet + Vector3.UnitY * 0.03f, Vector3.UnitY, h * (0.9f + 0.5f * m), Fx.Fade(Rgb(60, 44, 30), shown), FxBlend.Alpha, 0.4f);
                fx.Flat(FxShape.Crack, c.ToFeet + Vector3.UnitY * 0.03f, Vector3.UnitY, h * (0.7f + 0.4f * m), Fx.Fade(Rgb(60, 44, 30), shown * 0.8f), FxBlend.Alpha, 2.3f);
            }
            Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + Vector3.UnitY * h * 0.1f, h, I, 0.7f, (int)(3 + 5 * m), FxShape.Rock, Rgb(196, 170, 130), Rgb(120, 96, 70), 0.09f, 1.0f, 2.4f, FxBlend.Alpha, spin: 5f, salt: 74);
        }
        fx.Shake = Math.Max(fx.Shake, m * Math.Max(Fx.Envelope(c.Age, 0.05f, 0.1f, 0.4f, 0.55f), Fx.Envelope(c.Age, 0.5f, I + 0.02f, 0.6f, 0.9f)));
        Impact(c, col, fx, 0.8f + 0.4f * m);
    }

    /// <summary>Roar: great arcs of the user's voice rolling over the target, wind streaking past it and away.</summary>
    private static void Roar(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        Fx.Waves(fx, c, 0.04f, 0.5f, 3, FxShape.Wave, Rgb(255, 226, 190), 0.55f, 0.3f);
        fx.Sprite(FxShape.Glow, c.From + Vector3.UnitY * h * 0.1f, h * 0.9f, Fx.Fade(Rgb(255, 200, 130), 0.3f * Fx.Envelope(c.Age, 0f, 0.05f, 0.25f, 0.4f)));
        if (c.Cue.Missed) return;
        var dir = Vector3.Normalize(c.To - c.From);
        var (across, up) = Fx.Frame(dir);
        for (int i = 0; i < 9; i++)
        {
            float k = (c.Age - I + 0.05f - i * 0.025f) / 0.45f;
            if (k < 0f || k > 1f) continue;
            var p = c.To + (across * Fx.Signed(c.Seed, i, 96) * 0.6f + up * Fx.Signed(c.Seed, i, 97) * 0.5f) * c.ToSize + dir * c.ToSize * (k * 2.4f - 0.6f);
            fx.Streak(FxShape.Streak, p, dir, c.ToSize * 0.05f, 8f, Fx.Fade(White, 0.8f * MathF.Sin(k * MathF.PI)));
        }
        Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.4f, c.ToSize * 1.4f, Fx.Fade(Rgb(255, 226, 190), 0.8f));
        fx.Shake = Math.Max(fx.Shake, 0.25f * Fx.Envelope(c.Age, I, I + 0.03f, I + 0.2f, I + 0.45f));
    }

    /// <summary>Seismic Toss: the target flung up in a spin of streaks and brought down hard, the ground answering twice.</summary>
    private static void SeismicToss(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        if (c.Cue.Landed)
        {
            for (int i = 0; i < 8; i++)
            {
                float side = Fx.Signed(c.Seed, i, 98) * 0.5f;
                float x = side * h;
                float phase = i * 0.012f;
                float up = (c.Age - phase) / 0.17f;
                float down = (c.Age - 0.17f - phase) / (I - 0.17f);
                if (up >= 0f && up <= 1f)
                    fx.Streak(FxShape.Streak, c.ToFeet + new Vector3(x, h * (0.3f + 1.7f * Fx.EaseOut(up)), 0f), Vector3.UnitY, h * 0.1f, 4f, Fx.Fade(White, 0.8f * (1f - up * 0.4f)));
                else if (down >= 0f && down <= 1f)
                    fx.Streak(FxShape.Streak, c.ToFeet + new Vector3(x, h * (2.0f - 1.7f * down * down), 0f), -Vector3.UnitY, h * 0.1f, 5f, Fx.Fade(White, 0.9f));
            }
            float spin = Fx.Envelope(c.Age, 0f, 0.05f, I - 0.08f, I);
            fx.Sprite(FxShape.Swirl, c.To + Vector3.UnitY * h * 0.5f * Fx.EaseOut(Math.Min(1f, c.Age / 0.17f)), h * 0.9f, Fx.Fade(col.Light, spin * 0.6f), rotation: c.Age * 20f);
            GroundRing(fx, c.Age, c.ToFeet, I, 0.45f, h * 2f, Fx.Fade(Rgb(240, 230, 210), 0.9f));
            GroundRing(fx, c.Age, c.ToFeet, I + 0.25f, 0.35f, h * 1.2f, Fx.Fade(Rgb(210, 196, 170), 0.7f));
            Dust(c, fx, c.ToFeet, h * 1.3f, I, 8);
            fx.Flat(FxShape.Crack, c.ToFeet + Vector3.UnitY * 0.03f, Vector3.UnitY, h * 0.9f, Fx.Fade(Rgb(60, 44, 30), Fx.Envelope(c.Age, I, I + 0.04f, 0.8f, 1.1f)), FxBlend.Alpha, 1.2f);
            fx.Shake = Math.Max(fx.Shake, 0.7f * Fx.Envelope(c.Age, I, I + 0.03f, I + 0.12f, I + 0.35f));
            fx.Shake = Math.Max(fx.Shake, 0.35f * Fx.Envelope(c.Age, I + 0.25f, I + 0.27f, I + 0.3f, I + 0.45f));
        }
        Impact(c, col, fx, 1.2f);
    }

    /// <summary>The fireball of a blast at <paramref name="at"/>: flame, smoke and rings, with the screen's flash.</summary>
    private static void Blast(in FxCtx c, FxList fx, Vector3 at, Vector3 feet, float h, float t0, int flames, int smoke, float reach)
    {
        var fire = ColorsOf(PokemonType.Fire);
        Fx.Burst(fx, c.Seed, c.Age, at, h, t0, 0.8f, flames, FxShape.Flame, fire.Main, fire.Light, 0.24f, reach, -0.4f, salt: 75);
        Fx.Burst(fx, c.Seed, c.Age, at, h, t0 + 0.04f, 0.85f, smoke, FxShape.Smoke, Rgb(120, 110, 100, 230), Rgb(80, 74, 70, 230), 0.38f, reach * 0.6f, -0.25f, FxBlend.Alpha, salt: 76);
        GroundRing(fx, c.Age, feet, t0, 0.5f, h * 2.6f, Fx.Fade(Rgb(255, 214, 140), 0.9f));
        Fx.Shockwave(fx, c.Age, at, Vector3.Zero, t0, 0.4f, h * 2.2f, Fx.Fade(White, 0.9f));
        fx.ScreenFlash(White, 0.45f * Fx.Envelope(c.Age, t0 - 0.02f, t0 + 0.02f, t0 + 0.06f, t0 + 0.3f));
    }

    /// <summary>Self-Destruct: the user glows hotter and swells, sparks drawn into it, then it goes off.</summary>
    private static void SelfDestructY7b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float swell = Math.Clamp(c.Age / I, 0f, 1f);
        float env = Fx.Envelope(c.Age, 0f, 0.05f, I - 0.02f, I + 0.03f);
        if (env > 0f)
        {
            fx.Sprite(FxShape.Glow, c.From, h * (0.6f + 0.9f * swell), Fx.Fade(Rgb(255, 120, 50), env * (0.35f + 0.4f * swell)), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, c.From, h * (0.3f + 0.5f * swell), Fx.Fade(Rgb(255, 244, 200), env * 0.7f * swell));
            for (int i = 0; i < 8; i++)
            {
                float u = (c.Age * 3.2f + Fx.Rand(c.Seed, i, 99)) % 1f;
                var dir = Fx.Direction(c.Seed, 100 + i);
                fx.Sprite(FxShape.Spark, c.From + dir * h * 1.1f * (1f - u), h * 0.08f, Fx.Fade(Rgb(255, 200, 100), env * MathF.Sin(u * MathF.PI)), rotation: u * 6f);
            }
        }
        Blast(c, fx, c.From, c.FromFeet, h, I, 12, 6, 1.3f);
        fx.Shake = Math.Max(fx.Shake, 0.65f * Fx.Envelope(c.Age, I, I + 0.03f, I + 0.2f, I + 0.55f));
        Impact(c, col, fx, 1.2f);
    }

    /// <summary>Explosion: the user's whole body a fireball that grows into a rolling pillar of smoke over the field.</summary>
    private static void ExplosionY7b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float swell = Math.Clamp(c.Age / I, 0f, 1f);
        float pre = Fx.Envelope(c.Age, 0f, 0.05f, I - 0.03f, I + 0.02f);
        if (pre > 0f)
        {
            fx.Sprite(FxShape.Glow, c.From, h * (0.5f + 1.1f * swell * swell), Fx.Fade(Rgb(255, 150, 60), pre * 0.6f), FxBlend.Alpha);
            for (int i = 0; i < 6; i++)
            {
                float u = (c.Age * 2.4f + Fx.Rand(c.Seed, i, 101)) % 1f;
                fx.Sprite(FxShape.Ember, c.From + Fx.Direction(c.Seed, 110 + i) * h * 0.5f * u + Vector3.UnitY * h * u * 0.3f, h * 0.08f, Fx.Fade(Rgb(255, 220, 120), pre * (1f - u)));
            }
        }
        float ball = Fx.Envelope(c.Age, I, I + 0.04f, I + 0.18f, I + 0.55f);
        if (ball > 0f)
        {
            float grow = Fx.EaseOut((c.Age - I) / 0.35f);
            fx.Sprite(FxShape.Glow, c.From, h * (0.9f + 1.5f * grow), Fx.Fade(Rgb(255, 110, 30), ball * 0.6f), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, c.From, h * (0.6f + 0.9f * grow), Fx.Fade(Rgb(255, 236, 170), ball * 0.7f));
        }
        float stem = Fx.Envelope(c.Age, I + 0.05f, I + 0.25f, 0.95f, 1.2f);
        if (stem > 0f)
            for (int j = 0; j < 5; j++)
            {
                float rise = Fx.EaseOut(Math.Min(1f, (c.Age - I) / 0.6f));
                var p = c.FromFeet + new Vector3(Fx.Signed(c.Seed, j, 102) * h * 0.25f * (j + 1) * 0.5f, h * (0.3f + 0.45f * j) * rise, 0f);
                fx.Sprite(FxShape.Smoke, p, h * (0.55f + 0.12f * j), Fx.Fade(Rgb(96, 88, 84, 235), stem * 0.85f), FxBlend.Alpha, j * 1.7f + c.Age);
            }
        Blast(c, fx, c.From, c.FromFeet, h, I, 12, 6, 1.7f);
        fx.Shake = Math.Max(fx.Shake, 0.9f * Fx.Envelope(c.Age, I, I + 0.04f, I + 0.35f, I + 0.8f));
        Impact(c, col, fx, 1.35f);
    }

    /// <summary>Slam: a whipping tail comes over and down onto the target, a ring on the ground and dust at the blow.</summary>
    private static void Slam(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = (c.Age - 0.02f) / (I - 0.02f);
        if (k > 0f && k < 1.5f)
        {
            var start = c.From + new Vector3(-0.3f * c.FromSize, 0.2f * c.FromSize, 0f);
            var mid = c.To + new Vector3(0.9f * h, 0.9f * h, 0.1f * h);
            var end = c.Cue.Missed ? c.Aim - Vector3.UnitY * h * 0.3f : c.ToFeet + Vector3.UnitY * h * 0.15f;
            var pts = new Vector3[7];
            float head = Math.Min(1f, k);
            for (int i = 0; i < pts.Length; i++)
            {
                float u = Math.Max(0f, head - (pts.Length - 1 - i) * 0.07f);
                pts[i] = Vector3.Lerp(Vector3.Lerp(start, mid, u), Vector3.Lerp(mid, end, u), u);
            }
            float fade = k < 1f ? 1f : Math.Max(0f, 1f - (k - 1f) * 2f);
            fx.Ribbon(pts, h * 0.16f, Fx.Fade(Rgb(214, 196, 170), 0.85f * fade), FxBlend.Alpha);
            fx.Ribbon(pts, h * 0.06f, Fx.Fade(White, 0.9f * fade));
        }
        if (c.Cue.Landed)
        {
            GroundRing(fx, c.Age, c.ToFeet, I, 0.4f, h * 1.8f, Fx.Fade(Rgb(240, 230, 210), 0.9f));
            Dust(c, fx, c.ToFeet, h * 1.2f, I, 7);
            fx.Shake = Math.Max(fx.Shake, 0.5f * Fx.Envelope(c.Age, I, I + 0.03f, I + 0.12f, I + 0.35f));
        }
        Impact(c, col, fx, 1.15f);
    }

    /// <summary>Stomp: a great foot dropped on the target from above, a shadow growing under it, held a moment.</summary>
    private static void Stomp(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float fall = (c.Age - (I - 0.22f)) / 0.22f;
        float held = Fx.Envelope(c.Age, I - 0.22f, I - 0.15f, 0.8f, 1.05f);
        if (fall >= 0f && held > 0f)
        {
            float drop = fall < 1f ? Fx.EaseOut(fall * fall) : 1f;
            float lift = Math.Clamp((c.Age - 0.7f) / 0.3f, 0f, 1f);
            var foot = c.To + new Vector3(0.05f * h, (c.Cue.Missed ? 0.95f : 0.5f) * h + (2.2f * (1f - drop) + 0.8f * lift) * h, 0.1f * h);
            var shadow = Fx.Fade(Rgb(30, 26, 22), 0.35f * Math.Min(1f, fall) * held);
            fx.Flat(FxShape.Dot, c.ToFeet + Vector3.UnitY * 0.03f, Vector3.UnitY, h * (0.5f + 0.5f * drop), shadow, FxBlend.Alpha);
            fx.Sprite(FxShape.Foot, foot, h * 0.9f, Fx.Fade(Rgb(208, 192, 168), held), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            GroundRing(fx, c.Age, c.ToFeet, I, 0.4f, h * 1.6f, Fx.Fade(Rgb(240, 230, 210), 0.9f));
            Dust(c, fx, c.ToFeet, h, I, 6);
            fx.Shake = Math.Max(fx.Shake, 0.4f * Fx.Envelope(c.Age, I, I + 0.03f, I + 0.1f, I + 0.3f));
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Block: three slabs close in on the target, left, right and over it, and lock with a clang.</summary>
    private static void Block(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        if (c.Cue.Missed) return;
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0.04f, 0.12f, 0.85f, 1.15f);
        if (env <= 0f) return;
        float close = Fx.EaseOut(Math.Clamp((c.Age - 0.04f) / (I - 0.08f), 0f, 1f));
        var steel = Rgb(116, 148, 210);
        var offsets = new[] { new Vector3(-1f, 0f, 0f), new Vector3(1f, 0f, 0f), new Vector3(0f, 1f, 0f) };
        for (int i = 0; i < 3; i++)
        {
            var at = c.To + offsets[i] * h * (1.5f - 0.95f * close) + new Vector3(0f, i == 2 ? 0.1f : 0f, 0.15f * h);
            Panel(fx, c.Age, at, h * 0.8f, 0.04f + i * 0.03f, steel);
            Panel(fx, c.Age, at, h * 0.74f, 0.04f + i * 0.03f, Rgb(70, 92, 150));
        }
        GroundRing(fx, c.Age, c.ToFeet, I, 0.35f, h * 1.3f, Fx.Fade(steel, 0.9f));
        foreach (float t0 in new[] { I, I + 0.12f })
            Fx.Glint(fx, c.Age, c.To + new Vector3(Fx.Signed(c.Seed, (int)(t0 * 10f), 103) * 0.5f, 0.35f, 0.2f) * h, h, t0, White, 0.4f);
        fx.Sprite(FxShape.Glow, c.To, h * 1.2f, Fx.Fade(steel, env * 0.18f));
        fx.Shake = Math.Max(fx.Shake, 0.2f * Fx.Envelope(c.Age, I - 0.02f, I, I + 0.04f, I + 0.14f));
    }

    /// <summary>Detect: a bright eye opening over the user's head, its rays flashing, and a white halo that pulses twice.</summary>
    private static void Detect(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.75f, 1.05f);
        if (env <= 0f) return;
        var head = c.FromFeet + Vector3.UnitY * h * 1.0f;
        float open = Fx.EaseOut(Math.Min(1f, c.Age / 0.2f));
        fx.Sprite(FxShape.Eye, head + Vector3.UnitY * h * 0.1f, h * 0.45f * (0.3f + 0.7f * open), Fx.Fade(White, env), FxBlend.Alpha);
        fx.Sprite(FxShape.Glow, head + Vector3.UnitY * h * 0.1f, h * 0.7f, Fx.Fade(Rgb(255, 244, 190), env * 0.5f));
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f + c.Age * 1.4f;
            float flick = 0.5f + 0.5f * MathF.Sin(c.Age * 30f + i);
            fx.Streak(FxShape.Streak, head + Vector3.UnitY * h * 0.1f + new Vector3(MathF.Cos(a), MathF.Sin(a), 0f) * h * 0.4f, new Vector3(MathF.Cos(a), MathF.Sin(a), 0f), h * 0.04f, 3f, Fx.Fade(White, env * 0.8f * flick));
        }
        foreach (float t0 in new[] { I - 0.1f, I + 0.2f })
            Fx.Shockwave(fx, c.Age, c.From, Vector3.Zero, t0, 0.35f, h * 1.1f, Fx.Fade(White, 0.9f));
        fx.Sprite(FxShape.Glow, c.From, h * 0.9f, Fx.Fade(White, env * 0.25f));
    }

    /// <summary>Mind Reader: an eye over the target's head, a band of light scanning down its body twice, thoughts rising.</summary>
    private static void MindReader(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        if (c.Cue.Missed) return;
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0.04f, 0.14f, 0.85f, 1.15f);
        if (env <= 0f) return;
        var cyan = Rgb(150, 230, 255);
        var head = c.ToFeet + Vector3.UnitY * h * 1.1f;
        fx.Sprite(FxShape.Eye, head, h * 0.32f, Fx.Fade(Rgb(214, 244, 255), env), FxBlend.Alpha);
        foreach (float t0 in new[] { 0.12f, 0.5f })
        {
            float k = (c.Age - t0) / 0.38f;
            if (k < 0f || k > 1f) continue;
            float y = h * (1.05f - 1.0f * k);
            var at = c.ToFeet + Vector3.UnitY * y;
            fx.Streak(FxShape.Streak, at, Vector3.UnitX, h * 0.06f, 7f, Fx.Fade(cyan, 0.9f * MathF.Sin(k * MathF.PI)));
            fx.Streak(FxShape.Streak, at, Vector3.UnitX, h * 0.025f, 7f, Fx.Fade(White, MathF.Sin(k * MathF.PI)));
        }
        for (int j = 0; j < 3; j++)
        {
            float k = (c.Age - 0.1f - j * 0.16f) / 0.5f;
            if (k < 0f || k > 1f) continue;
            fx.Sprite(FxShape.Ring, head, h * (0.2f + 0.7f * k), Fx.Fade(cyan, 0.8f * (1f - k)));
        }
        for (int i = 0; i < 4; i++)
        {
            float u = (c.Age * 0.8f + i / 4f) % 1f;
            fx.Sprite(FxShape.Bubble, head + new Vector3(MathF.Sin(u * 7f + i) * h * 0.25f, h * (0.15f + 0.5f * u), 0f), h * 0.07f, Fx.Fade(cyan, env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha);
        }
    }

    /// <summary>Disable: the colour drains out of the target, a dark cross stamped on it and grey smoke sinking.</summary>
    private static void Disable(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        if (c.Cue.Missed) return;
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0.03f, I, 0.85f, 1.15f);
        if (env <= 0f) return;
        fx.Sprite(FxShape.Glow, c.To, h * 1.15f, Fx.Fade(Rgb(60, 62, 78), env * 0.5f), FxBlend.Alpha);
        float stamp = Fx.EaseOut(Math.Clamp((c.Age - I + 0.1f) / 0.12f, 0f, 1f));
        if (stamp > 0f)
        {
            fx.Sprite(FxShape.Cross, c.To + Vector3.UnitZ * 0.1f, h * 0.8f * (1.4f - 0.4f * stamp), Fx.Fade(White, env * stamp * 0.8f), FxBlend.Alpha);
            fx.Sprite(FxShape.Cross, c.To + Vector3.UnitZ * 0.12f, h * 0.7f * (1.4f - 0.4f * stamp), Fx.Fade(Rgb(70, 70, 92), env * stamp), FxBlend.Alpha);
        }
        for (int i = 0; i < 6; i++)
        {
            float u = (c.Age * 0.9f + Fx.Rand(c.Seed, i, 104)) % 1f;
            fx.Sprite(FxShape.Smoke, c.To + new Vector3(Fx.Signed(c.Seed, i, 105) * h * 0.4f, h * (0.4f - 0.8f * u), 0.1f), h * 0.25f, Fx.Fade(Rgb(130, 132, 150, 220), env * MathF.Sin(u * MathF.PI) * 0.8f), FxBlend.Alpha, i);
        }
        GroundRing(fx, c.Age, c.ToFeet, I, 0.4f, h * 1.1f, Fx.Fade(Rgb(150, 150, 180), 0.8f));
    }

    /// <summary>Dragon Rage: three ribbons of blue-violet fire weaving across to the target, and the blast they break into.</summary>
    private static void DragonRage(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var dragon = ColorsOf(PokemonType.Dragon);
        var blue = Rgb(120, 156, 255);
        float k = (c.Age - 0.02f) / (I - 0.04f);
        if (k > 0f && k < 1.5f)
        {
            var dir = c.Aim - c.From;
            var (across, up) = Fx.Frame(dir);
            float dist = dir.Length();
            float head = Math.Min(1f, k);
            float fade = k < 1f ? 1f : Math.Max(0f, 1f - (k - 1f) * 2.5f);
            for (int r = 0; r < 3; r++)
            {
                var pts = new Vector3[14];
                for (int i = 0; i < pts.Length; i++)
                {
                    float u = Math.Max(0f, head - (pts.Length - 1 - i) * 0.045f);
                    float amp = dist * 0.09f * MathF.Sin(u * MathF.PI) * (r == 1 ? -1f : 1f);
                    float wave = MathF.Sin(u * 9f + r * 2.1f - c.Age * 14f);
                    pts[i] = Vector3.Lerp(c.From, c.Aim, u) + across * amp * wave + up * amp * MathF.Cos(u * 7f + r) * 0.6f;
                }
                fx.Ribbon(pts, c.FromSize * (r == 0 ? 0.3f : 0.2f), Fx.Fade(r == 2 ? blue : dragon.Main, 0.9f * fade));
                fx.Ribbon(pts, c.FromSize * 0.08f, Fx.Fade(White, 0.9f * fade));
            }
            Fx.Stream(fx, c, 0.02f, I, 0.2f, 8, FxShape.Flame, blue, dragon.Light, 0.14f, spread: 0.22f);
        }
        if (c.Cue.Missed) return;
        float h = c.ToSize;
        Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 8, FxShape.Flame, blue, dragon.Light, 0.22f, 1.0f, -0.2f, salt: 77);
        for (int j = 0; j < 3; j++)
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I + j * 0.08f, 0.4f, h * (0.9f + 0.3f * j), Fx.Fade(j % 2 == 0 ? dragon.Main : White, 0.9f));
        fx.Shake = Math.Max(fx.Shake, 0.5f * Math.Max(Fx.Envelope(c.Age, I, I + 0.02f, I + 0.06f, I + 0.18f), Fx.Envelope(c.Age, I + 0.2f, I + 0.22f, I + 0.26f, I + 0.38f)));
        if (c.Cue.Landed) fx.Sprite(FxShape.Glow, c.To, h * 1.2f, Fx.Fade(blue, 0.5f * Fx.Envelope(c.Age, I, I + 0.04f, I + 0.15f, I + 0.45f)));
    }

    /// <summary>Extrasensory: three pulses of force sent over, each squeezing the air round the target in.</summary>
    private static void Extrasensory(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var teal = Rgb(90, 232, 204);
        var psychic = ColorsOf(PokemonType.Psychic);
        fx.Sprite(FxShape.Glow, c.From, c.FromSize * 0.95f, Fx.Fade(teal, 0.4f * Fx.Envelope(c.Age, 0f, 0.06f, 0.3f, 0.5f)), FxBlend.Alpha);
        for (int p = 0; p < 3; p++)
        {
            float t0 = 0.04f + p * 0.13f;
            float k = (c.Age - t0) / 0.2f;
            if (k >= 0f && k <= 1f)
                fx.Sprite(FxShape.Ring, Vector3.Lerp(c.From, c.Aim, Fx.EaseOut(k)), c.ScaleAt(k) * (0.35f + 0.4f * k), Fx.Fade(teal, 1f - k * 0.5f));
            if (c.Cue.Missed) continue;
            float s = (c.Age - t0 - 0.16f) / 0.25f;
            if (s < 0f || s > 1f) continue;
            fx.Sprite(FxShape.Ring, c.To, h * (1.45f - 0.8f * s), Fx.Fade(psychic.Main, MathF.Sin(s * MathF.PI)));
            for (int i = 0; i < 4; i++)
            {
                float a = i * MathF.Tau / 4f + p * 0.6f;
                var dir = new Vector3(MathF.Cos(a), MathF.Sin(a), 0f);
                fx.Streak(FxShape.Streak, c.To + dir * h * (1.0f - 0.55f * s), -dir, h * 0.04f, 3f, Fx.Fade(psychic.Light, 0.9f * MathF.Sin(s * MathF.PI)));
            }
        }
        if (c.Cue.Landed) fx.Sprite(FxShape.Swirl, c.To, h * 0.9f, Fx.Fade(psychic.Main, 0.55f * Fx.Envelope(c.Age, I, I + 0.04f, I + 0.2f, I + 0.5f)), FxBlend.Alpha, c.Age * 9f);
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Imprison: five bars dropped one after another in front of the target, clinking, a seal ringing the ground.</summary>
    private static void Imprison(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        // Cast on the user's own side but for the foe across the field: the cage stands round it
        float h = c.FoeSize;
        var feet = c.Foe - Vector3.UnitY * h * 0.5f;
        float env = Fx.Envelope(c.Age, 0f, 0.04f, 0.9f, 1.15f);
        if (env <= 0f) return;
        var steel = Rgb(150, 164, 196);
        var seal = Rgb(176, 110, 230);
        for (int i = 0; i < 5; i++)
        {
            float t0 = 0.04f + i * 0.045f;
            float k = (c.Age - t0) / 0.14f;
            if (k < 0f) continue;
            float drop = Fx.EaseOut(Math.Min(1f, k));
            float x = (i - 2) * h * 0.28f;
            var p = feet + new Vector3(x, h * (1.15f + 1.2f * (1f - drop) - 0.1f), 0.3f * h);
            fx.Streak(FxShape.Streak, p, Vector3.UnitY, h * 0.2f, 2.7f, Fx.Fade(steel, env), FxBlend.Alpha);
            fx.Streak(FxShape.Streak, p, Vector3.UnitY, h * 0.09f, 2.7f, Fx.Fade(White, env * 0.9f));
            Fx.Glint(fx, c.Age, feet + new Vector3(x, h * 0.05f, 0.3f * h), h, t0 + 0.12f, White, 0.3f);
        }
        float bar = Fx.EaseOut(Math.Clamp((c.Age - 0.28f) / 0.1f, 0f, 1f));
        fx.Streak(FxShape.Streak, feet + new Vector3(0f, h * 0.95f, 0.3f * h), Vector3.UnitX, h * 0.2f, 2.4f * bar, Fx.Fade(steel, env * bar), FxBlend.Alpha);
        GroundRing(fx, c.Age, feet, 0.28f, 0.5f, h * 1.2f, Fx.Fade(seal, 0.9f));
        fx.Sprite(FxShape.Glow, c.Foe, h * 1.2f, Fx.Fade(seal, env * 0.2f));
    }

    /// <summary>Sand Tomb: sand whirling up round the target in a narrowing spiral, a haze at its foot.</summary>
    private static void SandTomb(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        if (c.Cue.Missed) return;
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0.04f, 0.3f, 0.85f, 1.15f);
        if (env <= 0f) return;
        var sand = Rgb(232, 208, 150);
        var dark = Rgb(190, 152, 96);
        for (int i = 0; i < 18; i++)
        {
            float u = (i / 18f + c.Age * 0.5f) % 1f;
            float a = u * MathF.Tau * 2.5f + i * 0.9f + c.Age * 6f;
            float rad = h * (0.75f - 0.35f * u) * (0.9f + 0.2f * Fx.Rand(c.Seed, i, 106));
            var p = c.ToFeet + new Vector3(MathF.Cos(a) * rad, h * (0.05f + 1.1f * u), MathF.Sin(a) * rad * 0.5f);
            fx.Sprite(FxShape.Dot, p, h * 0.07f, Fx.Fade(i % 2 == 0 ? sand : dark, env * 0.9f), FxBlend.Alpha, depth: MathF.Sin(a) < 0f);
        }
        for (int j = 0; j < 4; j++)
        {
            float a = j * MathF.Tau / 4f + c.Age * 3f;
            fx.Sprite(FxShape.Smoke, c.ToFeet + new Vector3(MathF.Cos(a) * h * 0.5f, h * 0.12f, MathF.Sin(a) * h * 0.25f), h * 0.4f, Fx.Fade(sand, env * 0.5f), FxBlend.Alpha, a);
        }
        fx.Sprite(FxShape.Glow, c.To, h * 1.0f, Fx.Fade(Rgb(230, 190, 80), env * 0.25f), FxBlend.Alpha);
        fx.Shake = Math.Max(fx.Shake, 0.12f * env);
    }

    /// <summary>Sandstorm: gusts of sand streaking across the whole field and a tan haze drifting through.</summary>
    private static void SandstormY7b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = Math.Max(c.FromSize, c.FoeSize);
        float env = Fx.Envelope(c.Age, 0f, 0.2f, 0.9f, 1.2f);
        if (env <= 0f) return;
        // The whole field: from the middle between the two sides, whoever the cue names
        var mid = Vector3.Lerp(c.FromFeet, c.Foe - Vector3.UnitY * c.FoeSize * 0.5f, 0.5f);
        var sand = Rgb(222, 190, 128);
        var dark = Rgb(176, 138, 84);
        const float span = 20f;
        for (int i = 0; i < 34; i++)
        {
            float u = (c.Age * (1.1f + 0.8f * Fx.Rand(c.Seed, i, 107)) + Fx.Rand(c.Seed, i, 108)) % 1f;
            var p = mid + new Vector3(-span * 0.5f + span * u, h * (0.1f + 2.6f * Fx.Rand(c.Seed, i, 109)), Fx.Signed(c.Seed, i, 110) * h * 3f);
            fx.Streak(FxShape.Streak, p, Vector3.UnitX, h * 0.35f, 2.2f, Fx.Fade(i % 2 == 0 ? sand : dark, env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha);
        }
        for (int j = 0; j < 6; j++)
        {
            float u = (c.Age * 0.5f + j / 6f) % 1f;
            fx.Sprite(FxShape.Smoke, mid + new Vector3(-span * 0.45f + span * 0.9f * u, h * (0.6f + 0.25f * (j % 3)), Fx.Signed(c.Seed, j, 111) * h * 2f), h * 1.1f, Fx.Fade(sand, env * 0.35f * MathF.Sin(u * MathF.PI)), FxBlend.Alpha, j);
        }
    }

    /// <summary>Tickle: feathers brushing over the target in sways, sparkles round it and little waves of a giggle.</summary>
    private static void Tickle(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        if (c.Cue.Missed) return;
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0.05f, 0.15f, 0.85f, 1.15f);
        if (env <= 0f) return;
        for (int i = 0; i < 3; i++)
        {
            float t = c.Age * 7f + i * 2.1f;
            var p = c.ToFeet + new Vector3(MathF.Sin(t) * h * 0.4f, h * (0.25f + 0.25f * i + 0.12f * MathF.Sin(t * 1.7f)), 0.2f * h);
            fx.Sprite(FxShape.Feather, p, h * 0.3f, Fx.Fade(i == 1 ? Rgb(255, 214, 236) : White, env), FxBlend.Alpha, MathF.Cos(t) * 0.7f - 0.4f);
        }
        for (int s = 0; s < 4; s++)
            Fx.Glint(fx, c.Age, c.To + new Vector3(Fx.Signed(c.Seed, s, 112) * 0.5f, Fx.Signed(c.Seed, s, 113) * 0.4f, 0.1f) * h, h, 0.2f + s * 0.17f, Rgb(255, 240, 200), 0.3f);
        for (int w = 0; w < 3; w++)
        {
            float k = (c.Age - 0.12f - w * 0.17f) / 0.4f;
            if (k < 0f || k > 1f) continue;
            fx.Sprite(FxShape.Wave, c.To + new Vector3(((w % 2 == 0) ? -1f : 1f) * h * 0.55f, h * 0.15f, 0f), h * (0.25f + 0.25f * k), Fx.Fade(Rgb(255, 232, 244), (1f - k) * 0.9f), rotation: (w % 2 == 0) ? MathF.PI : 0f);
        }
        fx.Sprite(FxShape.Glow, c.From, c.FromSize * 0.7f, Fx.Fade(Rgb(255, 214, 236), 0.3f * env));
    }

    /// <summary>Uproar: rings of noise spreading from the user again and again, notes bursting from it, a loud shake.</summary>
    private static void Uproar(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var warm = Rgb(255, 214, 150);
        float pulse = 0.5f + 0.5f * MathF.Sin(c.Age * 26f);
        fx.Sprite(FxShape.Glow, c.From, h * (0.7f + 0.15f * pulse), Fx.Fade(warm, 0.35f * Fx.Envelope(c.Age, 0f, 0.05f, 0.6f, 0.9f)), FxBlend.Alpha);
        for (int i = 0; i < 4; i++)
            Fx.Shockwave(fx, c.Age, c.From, Vector3.Zero, i * 0.15f, 0.4f, h * 1.9f, Fx.Fade(i % 2 == 0 ? warm : White, 0.75f));
        Fx.Waves(fx, c, 0.02f, 0.5f, 3, FxShape.Wave, warm, 0.45f, 0.3f);
        Fx.Burst(fx, c.Seed, c.Age, c.From + Vector3.UnitY * h * 0.2f, h, 0.05f, 0.7f, 5, FxShape.Note, warm, Rgb(255, 160, 120), 0.1f, 1.1f, 0.2f, FxBlend.Alpha, salt: 78);
        fx.Shake = Math.Max(fx.Shake, 0.22f * Fx.Envelope(c.Age, 0.08f, 0.15f, 0.8f, 1.0f));
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Double Slap: an open hand slapping from the left, then one from the right.</summary>
    private static void DoubleSlap(in FxCtx c, FxColors col, FxList fx)
    {
        Strike(c, fx, FxShape.Palm, new Vector3(-0.85f, 0.2f, 0.2f), I - 0.2f, 0.34f, Rgb(255, 214, 170), -0.5f, 0.1f);
        Strike(c, fx, FxShape.Palm, new Vector3(0.85f, 0.2f, 0.2f), I, 0.34f, Rgb(255, 214, 170), 0.5f, 0.1f);
        if (c.Cue.Landed)
        {
            float h = c.ToSize;
            Fx.Contact(fx, c.Age, c.Seed + 1, c.To + new Vector3(-0.15f, 0.05f, 0f) * h, h, I - 0.2f, 0.26f, col.Main, col.Light);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I - 0.2f, 0.35f, 4, FxShape.Star, Rgb(255, 230, 120), White, 0.07f, 0.8f, salt: 79);
            fx.Sprite(FxShape.Glow, c.To, h, Fx.Fade(Rgb(255, 90, 80), 0.3f * Fx.Envelope(c.Age, I - 0.2f, I - 0.15f, I + 0.1f, I + 0.35f)), FxBlend.Alpha);
            fx.Shake = Math.Max(fx.Shake, 0.18f * Fx.Envelope(c.Age, I - 0.2f, I - 0.18f, I - 0.15f, I - 0.07f));
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Follow Me: a shaft of light falling on the user and stars popping over it three times.</summary>
    private static void FollowMe(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.15f, 0.85f, 1.15f);
        if (env <= 0f) return;
        var gold = Rgb(255, 232, 140);
        for (int i = 0; i < 5; i++)
            fx.Sprite(FxShape.Glow, c.FromFeet + new Vector3(0f, h * (0.1f + 0.75f * i), 0f), h * (0.7f + 0.22f * i), Fx.Fade(gold, env * 0.16f), FxBlend.Alpha);
        fx.Sprite(FxShape.Glow, c.FromFeet + Vector3.UnitY * h * 0.05f, h * 1.1f, Fx.Fade(White, env * 0.2f));
        GroundRing(fx, c.Age, c.FromFeet, 0.05f, 0.9f, h * 0.9f, Fx.Fade(gold, 0.5f));
        foreach (float t0 in new[] { 0.2f, 0.38f, 0.56f })
        {
            Fx.Burst(fx, c.Seed + (int)(t0 * 100), c.Age, c.FromFeet + Vector3.UnitY * h * 1.0f, h, t0, 0.45f, 4, FxShape.Star, gold, White, 0.1f, 0.7f, 0.5f, FxBlend.Alpha, spin: 5f, salt: 80);
            Fx.Glint(fx, c.Age, c.FromFeet + Vector3.UnitY * h * 1.05f, h, t0, White, 0.4f);
        }
    }

    /// <summary>Sing: three wavering lines of a stave run from the user to the target with notes riding them, and sleep gathering on the target.</summary>
    private static void SingY7b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var dir = c.Aim - c.From;
        var (across, up) = Fx.Frame(dir);
        float dist = dir.Length();
        float head = Math.Clamp((c.Age - 0.02f) / 0.4f, 0f, 1f);
        float fade = Fx.Envelope(c.Age, 0f, 0.05f, 0.7f, 1.0f);
        var pink = Rgb(255, 188, 220);
        if (fade > 0f && head > 0f)
        {
            for (int j = 0; j < 3; j++)
            {
                var pts = new Vector3[8];
                for (int i = 0; i < pts.Length; i++)
                {
                    float u = head * i / (pts.Length - 1);
                    pts[i] = Vector3.Lerp(c.From, c.Aim, u) + up * (j - 1) * c.FromSize * 0.12f + across * MathF.Sin(u * 9f - c.Age * 8f + j) * dist * 0.03f
                        + up * MathF.Sin(u * 7f - c.Age * 9f) * dist * 0.04f;
                }
                fx.Ribbon(pts, c.FromSize * 0.025f, Fx.Fade(j == 1 ? White : pink, fade * 0.8f), FxBlend.Alpha);
            }
            for (int i = 0; i < 6; i++)
            {
                float u = (c.Age * 1.6f + i / 6f) % 1f * head;
                if (u <= 0f) continue;
                var p = Vector3.Lerp(c.From, c.Aim, u) + up * (MathF.Sin(u * 7f - c.Age * 9f) * dist * 0.04f + (i % 3 - 1) * c.FromSize * 0.12f);
                fx.Sprite(FxShape.Note, p, c.ScaleAt(u) * 0.16f, Fx.Fade(i % 2 == 0 ? pink : White, fade), FxBlend.Alpha, MathF.Sin(c.Age * 6f + i) * 0.3f);
            }
        }
        if (c.Cue.Missed) return;
        float h = c.ToSize;
        fx.Sprite(FxShape.Glow, c.To, h * 1.1f, Fx.Fade(pink, 0.3f * Fx.Envelope(c.Age, I - 0.05f, I + 0.1f, 0.8f, 1.1f)), FxBlend.Alpha);
        Rising(fx, c.Age, c.Seed, c.ToFeet + Vector3.UnitY * h * 0.85f, h, I, 1.0f, 2, FxShape.Zzz, Rgb(226, 224, 255), 0.22f, 0.08f);
    }

    /// <summary>Bug Bite: a pair of jaws snapping together across the target twice, green juice flying.</summary>
    private static void BugBite(in FxCtx c, FxColors col, FxList fx)
    {
        if (c.Cue.Landed)
        {
            var bug = ColorsOf(PokemonType.Bug);
            float h = c.ToSize;
            foreach (float t0 in new[] { I - 0.2f, I - 0.06f })
            {
                float k = (c.Age - t0) / 0.2f;
                if (k < 0f || k > 1f) continue;
                float close = Fx.EaseOut(Math.Min(1f, k * 2f));
                float gap = h * 0.5f * (1f - close) + h * 0.06f;
                float fade = 1f - Math.Max(0f, k - 0.5f) / 0.5f;
                for (int side = -1; side <= 1; side += 2)
                {
                    var at = c.To + new Vector3(side * (gap + h * 0.1f), 0f, 0.1f);
                    // Jaws pointing in at the target, as the side they come from turns them
                    fx.Sprite(FxShape.Fang, at, h * 0.3f, Fx.Fade(Rgb(184, 214, 64), fade), FxBlend.Alpha, -side * MathF.PI / 2f);
                    fx.Sprite(FxShape.Fang, at, h * 0.33f, Fx.Fade(White, fade * 0.3f), rotation: -side * MathF.PI / 2f);
                }
            }
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I - 0.06f, 0.5f, 6, FxShape.Drop, bug.Main, bug.Light, 0.08f, 0.8f, 1.2f, FxBlend.Alpha, salt: 81);
            fx.Shake = Math.Max(fx.Shake, 0.18f * Fx.Envelope(c.Age, I - 0.06f, I - 0.04f, I - 0.01f, I + 0.08f));
        }
        Impact(c, col, fx, 0.85f);
    }

    /// <summary>Encore: five beats of a clap, each a ring and a flick of notes and stars on the target, arrows circling it to go again.</summary>
    private static void Encore(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        if (c.Cue.Missed) return;
        float h = c.ToSize;
        var gold = Rgb(255, 214, 120);
        float env = Fx.Envelope(c.Age, 0f, 0.06f, 0.9f, 1.15f);
        if (env <= 0f) return;
        for (int b = 0; b < 5; b++)
        {
            float t0 = I - 0.28f + b * 0.14f;
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, t0, 0.3f, h * 0.9f, Fx.Fade(b % 2 == 0 ? gold : White, 0.9f));
            Fx.Burst(fx, c.Seed + b, c.Age, c.To + Vector3.UnitY * h * 0.3f, h, t0, 0.35f, 2, b % 2 == 0 ? FxShape.Note : FxShape.Star, gold, White, 0.08f, 0.7f, 0.3f, FxBlend.Alpha, salt: 82);
        }
        for (int i = 0; i < 3; i++)
        {
            float a = i * MathF.Tau / 3f + c.Age * 3.5f;
            var p = c.To + new Vector3(MathF.Cos(a) * h * 0.65f, MathF.Sin(a) * h * 0.3f, MathF.Sin(a) * 0.2f);
            fx.Sprite(FxShape.Arrow, p, h * 0.2f, Fx.Fade(Rgb(255, 170, 200), env), FxBlend.Alpha, a + MathF.PI);
            fx.Sprite(FxShape.Glow, p, h * 0.22f, Fx.Fade(gold, env * 0.4f));
        }
    }

    /// <summary>Flail: the user thrashing, streaks of agitation spinning round it and sweat flying, then a flurry of little blows.</summary>
    private static void Flail(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.04f, I - 0.08f, I + 0.05f);
        if (env > 0f)
        {
            for (int i = 0; i < 8; i++)
            {
                float a = i * MathF.Tau / 8f + c.Age * 17f;
                var d = new Vector3(MathF.Cos(a), MathF.Sin(a), 0f);
                fx.Streak(FxShape.Streak, c.From + d * h * 0.8f, d, h * 0.08f, 3f, Fx.Fade(Rgb(255, 214, 190), env * (0.4f + 0.5f * MathF.Abs(MathF.Sin(c.Age * 25f + i)))));
            }
            for (int s = 0; s < 4; s++)
            {
                float u = (c.Age * 2.6f + s * 0.25f) % 1f;
                var d = Fx.Direction(c.Seed, 120 + s);
                fx.Sprite(FxShape.Drop, c.From + d * h * (0.5f + 0.5f * u) + Vector3.UnitY * h * 0.3f, h * 0.07f, Fx.Fade(Rgb(190, 226, 255), env * (1f - u)), FxBlend.Alpha);
            }
            fx.Sprite(FxShape.Glow, c.From, h * 0.8f, Fx.Fade(Rgb(255, 80, 70), env * 0.2f));
        }
        Blows(c, col, fx, 5, 0.06f, 0.18f, 0.16f);
        Impact(c, col, fx, 0.85f);
    }

    /// <summary>Ice Fang: two great fangs of ice closing on the target, frost and shards thrown off it.</summary>
    private static void IceFang(in FxCtx c, FxColors col, FxList fx)
    {
        var ice = ColorsOf(PokemonType.Ice);
        if (c.Cue.Landed)
        {
            float h = c.ToSize;
            FangPair(c, fx, I - 0.2f, 0.34f, Rgb(214, 244, 255), ice.Main);
            fx.Sprite(FxShape.Glow, c.To, h * 1.1f, Fx.Fade(ice.Main, 0.4f * Fx.Envelope(c.Age, I - 0.08f, I, I + 0.15f, I + 0.5f)), FxBlend.Alpha);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 6, FxShape.Shard, Rgb(226, 248, 255), ice.Light, 0.09f, 1.0f, 1.3f, FxBlend.Alpha, spin: 6f, salt: 83);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I + 0.03f, 0.9f, 5, FxShape.Snowflake, White, ice.Main, 0.1f, 0.7f, -0.4f, FxBlend.Alpha, spin: 3f, salt: 84);
            GroundRing(fx, c.Age, c.ToFeet, I + 0.04f, 0.5f, h * 1.2f, Fx.Fade(ice.Light, 0.8f));
            fx.Shake = Math.Max(fx.Shake, 0.2f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.06f, I + 0.2f));
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Lucky Chant: a golden ring turning on the ground round the user's side with stars riding it and a column of light rising.</summary>
    private static void LuckyChant(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.12f, 0.85f, 1.15f);
        if (env <= 0f) return;
        var gold = Rgb(255, 224, 120);
        var green = Rgb(176, 236, 150);
        float r = h * 1.15f;
        fx.Flat(FxShape.Ring, c.FromFeet + Vector3.UnitY * 0.04f, Vector3.UnitY, r, Fx.Fade(gold, env * 0.9f), depth: true);
        fx.Flat(FxShape.Ring, c.FromFeet + Vector3.UnitY * 0.05f, Vector3.UnitY, r * 0.72f, Fx.Fade(green, env * 0.7f), depth: true);
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f + c.Age * 2.4f;
            var p = c.FromFeet + new Vector3(MathF.Cos(a) * r * 0.85f, h * 0.12f + h * 0.08f * MathF.Sin(c.Age * 7f + i), MathF.Sin(a) * r * 0.5f);
            fx.Sprite(FxShape.Star, p, h * 0.13f, Fx.Fade(i % 2 == 0 ? gold : green, env), FxBlend.Alpha, c.Age * 3f + i, depth: MathF.Sin(a) < 0f);
        }
        for (int j = 0; j < 4; j++)
            fx.Sprite(FxShape.Glow, c.FromFeet + new Vector3(0f, h * (0.2f + 0.45f * j), 0f), h * (0.55f - 0.06f * j), Fx.Fade(gold, env * 0.16f), FxBlend.Alpha);
        foreach (float t0 in new[] { 0.25f, 0.45f, 0.65f })
            Fx.Glint(fx, c.Age, c.FromFeet + new Vector3(Fx.Signed(c.Seed, (int)(t0 * 10f), 114) * 0.4f, 1.0f, 0.1f) * h, h, t0, White, 0.35f);
    }

    /// <summary>Mimic: a pale ghost of the target lifts off it, shimmering, and floats across to the user, which flashes as it takes it.</summary>
    private static void Mimic(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        if (c.Cue.Missed) return;
        var ghost = Rgb(220, 226, 255);
        float form = Fx.EaseOut(Math.Min(1f, c.Age / I));
        float travel = Fx.EaseOut(Math.Clamp((c.Age - I) / 0.5f, 0f, 1f));
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.8f, 1.0f);
        if (env > 0f)
        {
            var at = Vector3.Lerp(c.To, c.From, travel) + Vector3.UnitY * c.ToSize * 0.3f * MathF.Sin(travel * MathF.PI);
            float size = c.ScaleAt(travel);
            float wobble = 1f + 0.06f * MathF.Sin(c.Age * 30f);
            fx.Sprite(FxShape.Glow, at, size * 1.0f * wobble * form, Fx.Fade(ghost, env * 0.5f), FxBlend.Alpha);
            fx.Sprite(FxShape.Ring, at, size * 0.55f * wobble * form, Fx.Fade(White, env * 0.8f));
            for (int j = 1; j <= 4; j++)
            {
                float back = Math.Max(0f, travel - j * 0.1f);
                fx.Sprite(FxShape.Glow, Vector3.Lerp(c.To, c.From, back), size * 0.35f, Fx.Fade(ghost, env * (0.5f - j * 0.1f)));
            }
        }
        if (c.Age < I)
            for (int i = 0; i < 6; i++)
            {
                float u = (c.Age * 3f + i / 6f) % 1f;
                fx.Sprite(FxShape.Spark, c.To + Fx.Direction(c.Seed, 130 + i) * c.ToSize * 0.6f * (1f - u), c.ToSize * 0.07f, Fx.Fade(White, MathF.Sin(u * MathF.PI)), rotation: u * 5f);
            }
        fx.Sprite(FxShape.Glow, c.From, c.FromSize, Fx.Fade(White, 0.45f * Fx.Envelope(c.Age, 0.7f, 0.78f, 0.85f, 1.1f)));
        fx.Sprite(FxShape.Swirl, c.From, c.FromSize * 0.8f, Fx.Fade(ghost, 0.6f * Fx.Envelope(c.Age, 0.72f, 0.8f, 0.9f, 1.15f)), FxBlend.Alpha, c.Age * 8f);
        Fx.Glint(fx, c.Age, c.From + Vector3.UnitY * c.FromSize * 0.3f, c.FromSize, 0.8f, White, 0.45f);
    }

    /// <summary>Rest: a soft glow settling over the user, a bubble swelling at its nose, Z's drifting up and little crosses of healing.</summary>
    private static void RestY7b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.15f, 0.9f, 1.2f);
        if (env <= 0f) return;
        var calm = Rgb(190, 214, 255);
        fx.Sprite(FxShape.Glow, c.From, h * 1.0f, Fx.Fade(calm, env * 0.3f), FxBlend.Alpha);
        float breath = 0.5f + 0.5f * MathF.Sin(c.Age * 5f);
        var nose = c.FromFeet + new Vector3(h * 0.38f, h * 0.55f, h * 0.1f);
        fx.Sprite(FxShape.Bubble, nose, h * (0.12f + 0.1f * breath), Fx.Fade(White, env * 0.8f), FxBlend.Alpha);
        fx.Sprite(FxShape.Dot, nose, h * 0.1f * (0.6f + 0.4f * breath), Fx.Fade(calm, env * 0.5f), FxBlend.Alpha);
        Rising(fx, c.Age, c.Seed, c.FromFeet + new Vector3(h * 0.3f, h * 0.7f, 0f), h, 0.1f, 1.1f, 3, FxShape.Zzz, Rgb(220, 226, 255), 0.22f, 0.1f);
        var green = Rgb(130, 236, 150);
        for (int i = 0; i < 4; i++)
        {
            float u = (c.Age * 0.6f + i / 4f) % 1f;
            fx.Sprite(FxShape.Cross, c.FromFeet + new Vector3(MathF.Sin(i * 2.2f) * h * 0.5f, h * (0.1f + 0.9f * u), 0f), h * 0.12f, Fx.Fade(green, env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha, MathF.PI / 4f);
        }
    }

    /// <summary>Revenge: a dark red aura gathering round the user, then a fist driven in trailing red, and a dark burst.</summary>
    private static void Revenge(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var red = Rgb(214, 40, 50);
        float env = Fx.Envelope(c.Age, 0f, 0.08f, I - 0.12f, I - 0.02f);
        if (env > 0f)
        {
            Fx.Aura(fx, c.Age, c.Seed, c.FromFeet, h, 0f, I - 0.02f, red, Rgb(255, 120, 100), 8);
            fx.Sprite(FxShape.Glow, c.From, h * 0.9f, Fx.Fade(Rgb(150, 20, 40), env * 0.3f));
        }
        float k = (c.Age - (I - 0.14f)) / 0.14f;
        if (k > 0f && (k < 1.6f))
        {
            var p = Vector3.Lerp(c.From, c.Aim, Fx.EaseOut(Math.Min(1f, k)));
            float fade = k < 1f ? 1f : Math.Max(0f, 1f - (k - 1f) * 1.6f);
            for (int j = 1; j <= 3; j++)
                fx.Sprite(FxShape.Glow, Vector3.Lerp(c.From, p, 1f - j * 0.18f), h * 0.3f, Fx.Fade(red, fade * (0.6f - j * 0.15f)));
            fx.Sprite(FxShape.Fist, p, h * 0.42f, Fx.Fade(Rgb(255, 196, 170), fade), FxBlend.Alpha, ScreenAngle(c.Aim - c.From) - 0.3f);
            fx.Sprite(FxShape.Glow, p, h * 0.5f, Fx.Fade(red, fade * 0.5f));
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.6f, 7, FxShape.Smoke, Rgb(60, 24, 40, 220), Rgb(130, 30, 50, 210), 0.2f, 0.8f, -0.2f, FxBlend.Alpha, salt: 85);
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.4f, c.ToSize * 1.5f, Fx.Fade(red, 0.9f));
            fx.Shake = Math.Max(fx.Shake, 0.55f * Fx.Envelope(c.Age, I, I + 0.03f, I + 0.18f, I + 0.45f));
        }
        Impact(c, col, fx, 1.25f);
    }

    /// <summary>Rock Blast: a volley of four rocks flung one after another, a burst of chips where each lands.</summary>
    private static void RockBlast(in FxCtx c, FxColors col, FxList fx)
    {
        const int count = 4;
        const float flight = 0.2f;
        Fx.Stream(fx, c, 0f, I, flight, count, FxShape.Rock, Rgb(196, 170, 130), Rgb(150, 124, 92), 0.26f, arc: 0.03f, spread: 0.18f, spin: 8f, blend: FxBlend.Alpha);
        if (!c.Cue.Missed)
            for (int i = 0; i < count; i++)
            {
                float arrive = flight + (I - flight) * i / (count - 1);
                var at = c.To + new Vector3(Fx.Signed(c.Seed, i, 115), Fx.Signed(c.Seed, i, 116), 0f) * c.ToSize * 0.2f;
                Fx.Burst(fx, c.Seed + i, c.Age, at, c.ToSize, arrive, 0.4f, 3, FxShape.Shard, Rgb(214, 190, 150), Rgb(150, 124, 92), 0.07f, 0.8f, 1.6f, FxBlend.Alpha, spin: 6f, salt: 86);
                Fx.Shockwave(fx, c.Age, at, Vector3.Zero, arrive, 0.2f, c.ToSize * 0.45f, Fx.Fade(Rgb(240, 226, 200), 0.8f));
                fx.Shake = Math.Max(fx.Shake, 0.15f * Fx.Envelope(c.Age, arrive, arrive + 0.01f, arrive + 0.03f, arrive + 0.1f));
            }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Snore: a big Z and great arcs of a snore sent over, rumbling, the target ringed where they break.</summary>
    private static void Snore(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var pale = Rgb(214, 224, 255);
        Rising(fx, c.Age, c.Seed, c.FromFeet + new Vector3(h * 0.25f, h * 0.7f, 0f), h, 0f, 1.0f, 3, FxShape.Zzz, pale, 0.3f, 0.1f);
        Fx.Waves(fx, c, 0.05f, 0.55f, 3, FxShape.Wave, pale, 0.5f, 0.3f);
        fx.Sprite(FxShape.Glow, c.From, h * 0.8f, Fx.Fade(pale, 0.2f * Fx.Envelope(c.Age, 0f, 0.1f, 0.6f, 0.9f)), FxBlend.Alpha);
        fx.Shake = Math.Max(fx.Shake, 0.14f * Fx.Envelope(c.Age, 0.05f, 0.15f, 0.8f, 1.0f));
        if (c.Cue.Missed) return;
        foreach (float t0 in new[] { I - 0.1f, I + 0.18f })
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, t0, 0.4f, c.ToSize * 1.2f, Fx.Fade(pale, 0.8f));
    }

    /// <summary>Sweet Scent: pink petals drifting across in lazy curves, a soft haze on the target and sparkles in it.</summary>
    private static void SweetScent(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var pink = Rgb(255, 150, 190);
        var pale = Rgb(255, 214, 230);
        Fx.Stream(fx, c, 0f, 0.7f, 0.5f, 12, FxShape.Petal, pink, pale, 0.12f, arc: 0.1f, spread: 0.5f, spin: 3f, blend: FxBlend.Alpha);
        if (c.Cue.Missed) return;
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, I - 0.1f, I + 0.15f, 0.9f, 1.2f);
        if (env <= 0f) return;
        for (int i = 0; i < 4; i++)
        {
            float a = i * MathF.Tau / 4f + c.Age * 0.8f;
            fx.Sprite(FxShape.Smoke, c.To + new Vector3(MathF.Cos(a) * h * 0.35f, MathF.Sin(a) * h * 0.2f, 0.12f), h * 0.5f, Fx.Fade(pink, env * 0.35f), FxBlend.Alpha, a);
        }
        fx.Sprite(FxShape.Glow, c.To, h * 1.1f, Fx.Fade(pale, env * 0.25f), FxBlend.Alpha);
        for (int s = 0; s < 4; s++)
            Fx.Glint(fx, c.Age, c.To + new Vector3(Fx.Signed(c.Seed, s, 117) * 0.5f, Fx.Signed(c.Seed, s, 118) * 0.45f, 0.1f) * h, h, I + s * 0.13f, White, 0.3f);
    }

    /// <summary>Twister: a funnel of wind standing up on the target, debris whirling in it, wind streaking at its foot.</summary>
    private static void TwisterY7b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var dragon = ColorsOf(PokemonType.Dragon);
        float env = Fx.Envelope(c.Age, 0.05f, I, 0.9f, 1.15f);
        if (env > 0f && !c.Cue.Missed)
        {
            float grow = Fx.EaseOut(Math.Min(1f, (c.Age - 0.05f) / 0.3f));
            float sway = MathF.Sin(c.Age * 6f) * h * 0.08f;
            // The funnel: puffs of wind turning round a middle that sways, narrow at the foot and wide at the top
            for (int j = 0; j < 6; j++)
                for (int q = 0; q < 3; q++)
                {
                    float y = h * (0.1f + 0.3f * j) * grow;
                    float r = h * (0.22f + 0.14f * j) * grow;
                    float a = q * MathF.Tau / 3f + j * 0.9f + c.Age * (9f - j);
                    var at = c.ToFeet + new Vector3(MathF.Cos(a) * r + sway * j * 0.3f, y, MathF.Sin(a) * r * 0.5f);
                    fx.Sprite(FxShape.Smoke, at, h * (0.36f + 0.07f * j), Fx.Fade(q == 0 ? Rgb(236, 240, 255, 230) : dragon.Light, env * 0.8f), FxBlend.Alpha, a, depth: MathF.Sin(a) < 0f);
                }
            for (int i = 0; i < 12; i++)
            {
                float u = (i / 12f + c.Age * 0.7f) % 1f;
                float a = u * MathF.Tau * 3f + i * 1.3f + c.Age * 11f;
                float level = Fx.Rand(c.Seed, i, 119);
                float r = h * (0.22f + 0.7f * level) * grow;
                var p = c.ToFeet + new Vector3(MathF.Cos(a) * r + sway * level, h * 2.0f * level * grow, MathF.Sin(a) * r * 0.5f);
                fx.Sprite(i % 3 == 0 ? FxShape.Leaf : FxShape.Dot, p, h * 0.07f, Fx.Fade(i % 2 == 0 ? dragon.Main : White, env), FxBlend.Alpha, a, depth: MathF.Sin(a) < 0f);
            }
            fx.Sprite(FxShape.Smoke, c.ToFeet + new Vector3(0f, h * 0.3f, 0.1f), h * 0.8f * grow, Fx.Fade(Rgb(200, 206, 230, 200), env * 0.35f), FxBlend.Alpha, c.Age * 4f);
        }
        fx.Shake = Math.Max(fx.Shake, 0.25f * Fx.Envelope(c.Age, I - 0.02f, I, 0.8f, 1.0f));
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Worry Seed: a seed lobbed high onto the target's head that splits, sweat flying and a small grey cloud gathering over it.</summary>
    private static void WorrySeed(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float k = c.Age / I;
        var lime = Rgb(150, 200, 70);
        float h = c.ToSize;
        if (k <= 1f)
        {
            var p = Lob(c.From, c.Cue.Missed ? c.Aim : c.ToFeet + Vector3.UnitY * h * 0.95f, k, 0.2f);
            fx.Sprite(FxShape.Glow, p, c.ScaleAt(k) * 0.4f, Fx.Fade(Rgb(255, 250, 200), 0.55f));
            fx.Sprite(FxShape.Seed, p, c.ScaleAt(k) * 0.3f, Fx.Fade(Rgb(110, 76, 40), 1f), FxBlend.Alpha, c.Age * 9f);
            fx.Sprite(FxShape.Seed, p, c.ScaleAt(k) * 0.2f, Fx.Fade(lime, 1f), FxBlend.Alpha, c.Age * 9f);
        }
        if (c.Cue.Missed) return;
        var head = c.ToFeet + Vector3.UnitY * h * 0.95f;
        Fx.Burst(fx, c.Seed, c.Age, head, h, I, 0.5f, 4, FxShape.Smoke, Fx.Fade(lime, 0.8f), Rgb(200, 230, 140, 200), 0.14f, 0.5f, -0.3f, FxBlend.Alpha, salt: 87);
        Fx.Burst(fx, c.Seed, c.Age, head, h, I + 0.03f, 0.7f, 4, FxShape.Drop, Rgb(150, 206, 255), White, 0.14f, 0.9f, 1.4f, FxBlend.Alpha, salt: 88);
        float cloud = Fx.Envelope(c.Age, I + 0.05f, I + 0.25f, 0.9f, 1.2f);
        if (cloud > 0f)
            for (int i = 0; i < 3; i++)
                fx.Sprite(FxShape.Smoke, head + new Vector3((i - 1) * h * 0.22f, h * (0.3f + 0.05f * (i % 2)), 0.1f), h * 0.32f, Fx.Fade(Rgb(96, 104, 98, 230), cloud * 0.85f), FxBlend.Alpha, i * 1.4f);
        float trem = MathF.Sin(c.Age * 40f) * 0.04f * h * cloud;
        fx.Sprite(FxShape.Exclaim, head + new Vector3(h * 0.38f + trem, h * 0.2f, 0.1f), h * 0.22f, Fx.Fade(Rgb(120, 170, 70), cloud), FxBlend.Alpha);
    }
}
