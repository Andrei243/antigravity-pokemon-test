using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

// Plan 25 · Y8, its first part (Y8a): the moves the story shows from Hearthome City's gate to the Café Cabin on
// Route 210, in MoveSightings' order, each a look of its own. The beats are the original's, read from each move's
// animation script (what comes first, how often it hits, when it lands, who shakes); the shapes, colours and timings
// are our own. Every recipe lands at ImpactTime (FxList.Land, which Impact sets), is over by EffectTime and stays
// under MaxQuads. Chatter is the user's voice: its sound is the cry, and this is the look alone.
internal static partial class MoveFx
{
    private static Dictionary<string, FxRecipe> Platinum4() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Bubble Beam"] = BubbleBeam,
        ["Mach Punch"] = MachPunch,
        ["Metal Claw"] = MetalClaw,
        ["Future Sight"] = FutureSight,
        ["Pain Split"] = PainSplit,
        ["Psybeam"] = Psybeam,
        ["Shadow Ball"] = ShadowBall,
        ["Shadow Claw"] = ShadowClaw,
        ["Shadow Sneak"] = ShadowSneakY8,
        ["Spit Up"] = SpitUp,
        ["Stockpile"] = Stockpile,
        ["Sucker Punch"] = SuckerPunch,
        ["Swallow"] = Swallow,
        ["Will-O-Wisp"] = WillOWispY8,
        ["Minimize"] = Minimize,
        ["Payback"] = Payback,
        ["Attract"] = Attract,
        ["Amnesia"] = Amnesia,
        ["Aqua Ring"] = AquaRing,
        ["Copycat"] = Copycat,
        ["Fury Attack"] = FuryAttack,
        ["Jump Kick"] = JumpKick,
        ["Light Screen"] = LightScreen,
        ["Nasty Plot"] = NastyPlot,
        ["Refresh"] = Refresh,
        ["Soft-Boiled"] = SoftBoiled,
        ["Steel Wing"] = SteelWing,
        ["Sweet Kiss"] = SweetKiss,
        ["Whirlwind"] = Whirlwind,
        ["Chatter"] = Chatter,
        ["Mirror Move"] = MirrorMove,
        ["Haze"] = Haze,
        ["Defog"] = DefogY8,
        ["Dig"] = Dig,
        ["Yawn"] = Yawn,
        ["Acupressure"] = Acupressure,
        ["Air Cutter"] = AirCutter,
        ["False Swipe"] = FalseSwipe,
        ["Feint"] = Feint,
        ["Metronome"] = Metronome,
        ["Pin Missile"] = PinMissile,
        ["Roost"] = Roost,
        ["Scary Face"] = ScaryFace,
        ["Shock Wave"] = ShockWave,
        ["Slash"] = SlashY8,
        ["Swagger"] = Swagger,
        ["Thunder Shock"] = ThunderShock,
        ["Toxic Spikes"] = ToxicSpikes,
        ["Wake-Up Slap"] = WakeUpSlap,
        ["Cosmic Power"] = CosmicPower,
        ["Lick"] = Lick
    };

    // ------------------------------------------------------------------ shared by the batch

    /// <summary>Motes drawn in from round <paramref name="at"/> to it between <paramref name="t0"/> and <paramref name="t1"/>.</summary>
    private static void Gather(FxList fx, float age, int seed, Vector3 at, float h, float t0, float t1, int count, Color a, Color b, float size, float reach = 0.9f)
    {
        float span = t1 - t0, t = age - t0;
        if (t < 0f || t > span) return;
        float env = Fx.Envelope(t, 0f, 0.04f, span - 0.05f, span);
        for (int i = 0; i < count; i++)
        {
            float u = (t / span * 1.5f + Fx.Rand(seed, i, 120)) % 1f;
            var p = at + Fx.Direction(seed, 130 + i) * h * reach * (1f - Fx.EaseOut(u));
            fx.Sprite(i % 3 == 0 ? FxShape.Spark : FxShape.Glow, p, h * size * (0.6f + 0.6f * u), Fx.Fade(Fx.Mix(a, b, Fx.Rand(seed, i, 121)), env * MathF.Sin(u * MathF.PI)));
        }
    }

    /// <summary>A needle lobbed from <paramref name="from"/> to <paramref name="to"/>: <paramref name="k"/> of the way along its arc.</summary>
    private static void Needle(FxList fx, Vector3 from, Vector3 to, float k, float h, Color color, float arc, float length = 3.2f)
    {
        if (k < 0f || k > 1f) return;
        var p = Lob(from, to, k, arc);
        var q = Lob(from, to, Math.Min(1f, k + 0.05f), arc);
        var dir = q - p;
        if (dir.LengthSquared() < 1e-6f) dir = to - from;
        fx.Streak(FxShape.Streak, p, dir, h * 0.11f, length, Fx.Fade(color, 1f), FxBlend.Alpha);
        fx.Streak(FxShape.Streak, p, dir, h * 0.045f, length, White);
    }

    /// <summary>Three parallel claws raked across the target from <paramref name="t0"/>, slanting by <paramref name="tilt"/>.</summary>
    private static void ClawMarks(in FxCtx c, FxList fx, float t0, float scale, Color color, Color core, float tilt)
    {
        if (c.Cue.Missed) return;
        float h = c.ToSize;
        var along = new Vector3(MathF.Sin(tilt), -MathF.Cos(tilt), 0f);
        var perp = new Vector3(MathF.Cos(tilt), MathF.Sin(tilt), 0f);
        for (int i = 0; i < 3; i++)
        {
            float k = (c.Age - t0 - i * 0.022f) / 0.26f;
            if (k < 0f || k > 1f) continue;
            float grow = Fx.EaseOut(Math.Min(1f, k * 2.4f));
            var p = c.To + perp * (i - 1) * h * 0.2f * scale + along * (grow - 0.6f) * h * 0.5f * scale;
            float hold = k < 0.5f ? 1f : (1f - k) / 0.5f;
            fx.Streak(FxShape.Streak, p, along, h * 0.13f * scale, 6.5f * grow, Fx.Fade(color, hold), FxBlend.Alpha);
            fx.Streak(FxShape.Streak, p, along, h * 0.055f * scale, 6.5f * grow, Fx.Fade(core, hold));
        }
    }

    /// <summary>A pale glow over the target that holds while it is tinted: alpha-blended so it does not wash the body out.</summary>
    private static void Tint(in FxCtx c, FxList fx, float t0, float t1, Color color, float strength)
    {
        float env = Fx.Envelope(c.Age, t0, t0 + 0.08f, t1 - 0.2f, t1);
        if (env > 0f) fx.Sprite(FxShape.Glow, c.To, c.ToSize * 1.1f, Fx.Fade(color, env * strength), FxBlend.Alpha);
    }

    // ------------------------------------------------------------------ the recipes

    /// <summary>Bubble Beam: a long stream of bubbles to the target, which bob and sway round it before they pop.</summary>
    private static void BubbleBeam(in FxCtx c, FxColors col, FxList fx)
    {
        var water = ColorsOf(PokemonType.Water);
        float h = c.ToSize;
        Fx.Stream(fx, c, 0.02f, I, 0.2f, 14, FxShape.Dot, Fx.Fade(water.Main, 0.55f), Fx.Fade(water.Light, 0.55f), 0.11f, 0.02f, 0.22f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0.02f, I, 0.2f, 14, FxShape.Bubble, water.Light, White, 0.1f, 0.02f, 0.22f);
        if (c.Cue.Landed)
        {
            float env = Fx.Envelope(c.Age, I, I + 0.05f, 0.9f, 1.2f);
            for (int i = 0; i < 7; i++)
            {
                float u = (c.Age * 0.8f + Fx.Rand(c.Seed, i, 150)) % 1f;
                var p = c.ToFeet + new Vector3(MathF.Sin(c.Age * 12f + i * 1.7f) * h * 0.32f + Fx.Signed(c.Seed, i, 151) * h * 0.28f, h * (0.1f + 0.85f * u), 0.1f);
                float a = env * MathF.Sin(u * MathF.PI);
                fx.Sprite(FxShape.Dot, p, h * 0.075f, Fx.Fade(water.Main, a * 0.5f), FxBlend.Alpha);
                fx.Sprite(FxShape.Bubble, p, h * 0.08f, Fx.Fade(White, a));
            }
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 8, FxShape.Bubble, water.Light, White, 0.08f, 0.8f, -0.5f, salt: 80);
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Mach Punch: so fast it leaves three ghosts of the fist behind it, and lands before the eye follows.</summary>
    private static void MachPunch(in FxCtx c, FxColors col, FxList fx)
    {
        SpeedLines(c, fx, 0.0f, 0.2f, 7, 0.75f);
        var skin = Rgb(255, 224, 190);
        for (int g = 3; g >= 1; g--)
            Strike(c, fx, FxShape.Fist, new Vector3(-1.5f, 0.15f, 0.15f), I + 0.028f * g, 0.38f, Fx.Fade(skin, 0.45f / g), -0.3f, 0.13f);
        Strike(c, fx, FxShape.Fist, new Vector3(-1.5f, 0.15f, 0.15f), I, 0.4f, skin, -0.3f, 0.13f);
        if (c.Cue.Landed) fx.Shake = Math.Max(fx.Shake, 0.22f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.06f, I + 0.2f));
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Metal Claw: the user glints steel, then two silver claws rake across the target, crossing.</summary>
    private static void MetalClaw(in FxCtx c, FxColors col, FxList fx)
    {
        var steel = ColorsOf(PokemonType.Steel);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.05f, 0.14f, 0.26f);
        if (env > 0f) fx.Sprite(FxShape.Glow, c.From, h * 0.9f, Fx.Fade(steel.Main, env * 0.4f), FxBlend.Alpha);
        Fx.Glint(fx, c.Age, c.From + new Vector3(0.2f, 0.2f, 0f) * h, h, 0.04f, White, 0.35f);
        Fx.Glint(fx, c.Age, c.From + new Vector3(-0.25f, -0.1f, 0f) * h, h, 0.11f, White, 0.3f);
        ClawMarks(c, fx, I - 0.14f, 1f, Rgb(214, 224, 244), White, 0.42f);
        ClawMarks(c, fx, I - 0.03f, 1f, Rgb(214, 224, 244), White, -0.42f);
        if (c.Cue.Landed) Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I - 0.03f, 0.45f, 8, FxShape.Spark, steel.Main, White, 0.07f, 0.9f, salt: 81);
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Future Sight: an eye opens over the user and rings of foresight roll toward the foe, a haze settling over it.</summary>
    private static void FutureSight(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var psy = ColorsOf(PokemonType.Psychic);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.5f, 0.75f);
        var head = c.FromFeet + Vector3.UnitY * h * 1.05f;
        if (env > 0f)
        {
            fx.Sprite(FxShape.Glow, c.From, h * 0.9f, Fx.Fade(psy.Light, env * 0.35f), FxBlend.Alpha);
            fx.Sprite(FxShape.Eye, head, h * 0.26f * (0.4f + 0.6f * Fx.EaseOut(c.Age / 0.2f)), Fx.Fade(Rgb(255, 236, 250), env), FxBlend.Alpha);
            fx.Sprite(FxShape.Ring, head, h * 0.42f, Fx.Fade(psy.Main, env * 0.8f));
        }
        fx.Shake = Math.Max(fx.Shake, 0.14f * Fx.Envelope(c.Age, 0.02f, 0.05f, 0.2f, 0.32f));
        for (int j = 0; j < 3; j++)
        {
            float k = (c.Age - 0.12f - j * 0.09f) / 0.3f;
            if (k < 0f || k > 1f) continue;
            fx.Sprite(FxShape.Ring, Vector3.Lerp(head, c.Aim, Fx.EaseOut(k)), c.ScaleAt(k) * (0.3f + 0.35f * k), Fx.Fade(psy.Main, 1f - k * 0.6f));
        }
        if (c.Cue.Missed) return;
        float ht = c.ToSize;
        float haze = Fx.Envelope(c.Age, I - 0.05f, I + 0.1f, 0.95f, 1.2f);
        if (haze <= 0f) return;
        fx.Sprite(FxShape.Swirl, c.To, ht * 1.0f, Fx.Fade(psy.Main, haze * 0.5f), FxBlend.Alpha, c.Age * 3f);
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f - c.Age * 4f;
            fx.Sprite(FxShape.Spark, c.To + new Vector3(MathF.Cos(a) * ht * 0.55f, MathF.Sin(a) * ht * 0.4f, 0.1f), ht * 0.07f, Fx.Fade(psy.Light, haze), rotation: a);
        }
        Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.4f, ht * 1.2f, Fx.Fade(psy.Light, 0.8f));
    }

    /// <summary>Pain Split: red-pink sparks flow from each to the other, crossing on the way, and both shudder.</summary>
    private static void PainSplit(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var red = Rgb(255, 96, 120);
        var pale = Rgb(255, 206, 214);
        Fx.Stream(fx, c, 0.04f, 0.6f, 0.3f, 7, FxShape.Orb, red, pale, 0.2f, 0.06f, 0.1f, spin: 4f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0.04f, 0.6f, 0.3f, 7, FxShape.Orb, red, pale, 0.1f, 0.12f, 0.1f, spin: 4f, blend: FxBlend.Alpha, reverse: true);
        Fx.Stream(fx, c, 0.04f, 0.6f, 0.3f, 7, FxShape.Glow, Fx.Fade(red, 0.6f), Fx.Fade(pale, 0.6f), 0.3f, 0.06f, 0.1f);
        foreach (float t0 in new[] { 0.1f, 0.3f, 0.5f })
            fx.Sprite(FxShape.Glow, c.From, c.FromSize * 0.8f, Fx.Fade(red, 0.4f * Fx.Envelope(c.Age, t0, t0 + 0.04f, t0 + 0.08f, t0 + 0.2f)), FxBlend.Alpha);
        foreach (float t0 in new[] { 0.2f, 0.4f, 0.6f })
            fx.Sprite(FxShape.Glow, c.To, c.ToSize * 0.8f, Fx.Fade(red, 0.4f * Fx.Envelope(c.Age, t0, t0 + 0.04f, t0 + 0.08f, t0 + 0.2f)), FxBlend.Alpha);
        Fx.Shockwave(fx, c.Age, c.Foe, Vector3.Zero, 0.6f, 0.35f, c.FoeSize * 0.9f, Fx.Fade(pale, 0.8f));
        Fx.Shockwave(fx, c.Age, c.From, Vector3.Zero, 0.6f, 0.35f, c.FromSize * 0.9f, Fx.Fade(pale, 0.8f));
        fx.Shake = Math.Max(fx.Shake, 0.16f * Fx.Envelope(c.Age, 0.05f, 0.1f, 0.65f, 0.8f));
    }

    /// <summary>Psybeam: a ribbon of three wobbling colours to the target, rings running down it, the target tinted purple.</summary>
    private static void Psybeam(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var psy = ColorsOf(PokemonType.Psychic);
        var tones = new[] { Rgb(255, 120, 200), Rgb(120, 220, 255), Rgb(255, 236, 120) };
        float k = (c.Age - 0.02f) / (I - 0.05f);
        if (k > 0f && k < 1.7f)
        {
            var dir = c.Aim - c.From;
            var (across, up) = Fx.Frame(dir);
            float dist = dir.Length();
            float head = Math.Min(1f, k);
            float fade = k < 1f ? 1f : Math.Max(0f, 1f - (k - 1f) * 1.4f);
            for (int r = 0; r < 3; r++)
            {
                var pts = new Vector3[12];
                for (int i = 0; i < pts.Length; i++)
                {
                    float u = Math.Max(0f, head - (pts.Length - 1 - i) * 0.03f);
                    float wob = MathF.Sin(u * 15f + r * 2.1f - c.Age * 22f) * dist * 0.025f * (0.4f + u);
                    pts[i] = Vector3.Lerp(c.From, c.Aim, u) + up * wob + across * wob * 0.4f;
                }
                fx.Ribbon(pts, c.FromSize * 0.2f, Fx.Fade(tones[r], 0.85f * fade));
            }
            for (int j = 0; j < 4; j++)
            {
                float u = (c.Age * 2.2f + j / 4f) % 1f;
                fx.Sprite(FxShape.Ring, Vector3.Lerp(c.From, c.Aim, u * head), c.ScaleAt(u) * 0.3f, Fx.Fade(psy.Light, fade * 0.8f));
            }
        }
        if (c.Cue.Missed) return;
        Tint(c, fx, I, 0.95f, Rgb(160, 90, 255), 0.55f);
        fx.Sprite(FxShape.Swirl, c.To, c.ToSize * 0.9f, Fx.Fade(psy.Main, 0.5f * Fx.Envelope(c.Age, I, I + 0.04f, 0.5f, 0.85f)), FxBlend.Alpha, c.Age * 8f);
        fx.Shake = Math.Max(fx.Shake, 0.25f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.12f, I + 0.3f));
    }

    /// <summary>Shadow Ball: shadow gathers into a dark orb at the user, which is thrown and bursts in a cloud of dark.</summary>
    private static void ShadowBall(in FxCtx c, FxColors col, FxList fx)
    {
        var ghost = ColorsOf(PokemonType.Ghost);
        Gather(fx, c.Age, c.Seed, c.From + Vector3.UnitY * c.FromSize * 0.1f, c.FromSize, 0f, 0.16f, 12, ghost.Main, ghost.Light, 0.09f);
        Fx.Orb(fx, c, 0.12f, I, 0.24f, ghost.Main, ghost.Light, FxShape.Smoke, Rgb(40, 24, 66));
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.7f, 9, FxShape.Smoke, Rgb(60, 36, 100, 230), Rgb(30, 20, 56, 230), 0.34f, 0.9f, -0.2f, FxBlend.Alpha, salt: 82);
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.5f, 7, FxShape.Wisp, ghost.Light, White, 0.12f, 1.0f, -0.3f, salt: 83);
            Tint(c, fx, I, 0.9f, Rgb(70, 40, 120), 0.5f);
            fx.Shake = Math.Max(fx.Shake, 0.3f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.08f, I + 0.25f));
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Shadow Claw: a cloud of shadow thickens behind the target and two great ghostly claws rake across it.</summary>
    private static void ShadowClaw(in FxCtx c, FxColors col, FxList fx)
    {
        var ghost = ColorsOf(PokemonType.Ghost);
        float h = c.ToSize;
        if (!c.Cue.Missed)
        {
            float env = Fx.Envelope(c.Age, 0f, 0.12f, 0.6f, 0.95f);
            if (env > 0f)
                for (int i = 0; i < 4; i++)
                    fx.Sprite(FxShape.Smoke, c.To + new Vector3(Fx.Signed(c.Seed, i, 152) * h * 0.4f, Fx.Signed(c.Seed, i, 153) * h * 0.25f, -0.1f), h * 0.55f,
                        Fx.Fade(Rgb(48, 30, 84, 230), env * 0.7f), FxBlend.Alpha, i * 1.9f + c.Age);
        }
        ClawMarks(c, fx, I - 0.12f, 1.3f, ghost.Main, ghost.Light, 0.5f);
        ClawMarks(c, fx, I - 0.02f, 1.3f, ghost.Main, ghost.Light, -0.5f);
        if (c.Cue.Landed) Fx.Burst(fx, c.Seed, c.Age, c.To, h, I - 0.02f, 0.5f, 6, FxShape.Wisp, ghost.Light, White, 0.1f, 0.9f, -0.2f, salt: 84);
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Shadow Sneak: the user is a dark streak along the ground, and a shadowy hand strikes up from under the target.</summary>
    private static void ShadowSneakY8(in FxCtx c, FxColors col, FxList fx)
    {
        var ghost = ColorsOf(PokemonType.Ghost);
        var black = Rgb(30, 22, 44);
        float h = c.ToSize;
        float run = Fx.EaseOut(Math.Clamp(c.Age / (I - 0.08f), 0f, 1f));
        float env = Fx.Envelope(c.Age, 0f, 0.04f, I - 0.1f, I);
        if (env > 0f)
        {
            var at = Vector3.Lerp(c.FromFeet, c.ToFeet, run) + Vector3.UnitY * 0.05f;
            fx.Streak(FxShape.Streak, at, c.ToFeet - c.FromFeet, c.FromSize * 0.4f, 3f, Fx.Fade(black, env), FxBlend.Alpha);
            for (int j = 1; j <= 4; j++)
                fx.Flat(FxShape.Dot, Vector3.Lerp(c.FromFeet, c.ToFeet, Math.Max(0f, run - j * 0.1f)) + Vector3.UnitY * 0.04f, Vector3.UnitY, c.FromSize * 0.45f, Fx.Fade(black, env * (1f - j * 0.2f)), FxBlend.Alpha);
        }
        if (c.Cue.Missed) { fx.Land(I); return; }
        Strike(c, fx, FxShape.Palm, new Vector3(0.1f, -0.9f, 0.2f), I, 0.4f, Rgb(60, 40, 96), 0.2f, 0.1f);
        Fx.Shockwave(fx, c.Age, c.ToFeet + Vector3.UnitY * 0.05f, Vector3.UnitY, I, 0.45f, h * 1.5f, Fx.Fade(ghost.Main, 0.9f));
        Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + Vector3.UnitY * h * 0.2f, h, I, 0.5f, 7, FxShape.Smoke, Rgb(40, 28, 66, 230), Rgb(20, 14, 34, 230), 0.22f, 0.8f, -0.1f, FxBlend.Alpha, salt: 85);
        Tint(c, fx, I, 0.9f, black, 0.5f);
        fx.Shake = Math.Max(fx.Shake, 0.3f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.08f, I + 0.25f));
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Spit Up: the user swells and spits out a great glob of what it stored, which bursts on the target.</summary>
    private static void SpitUp(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var cream = Rgb(255, 238, 190);
        var mouth = c.FromFeet + Vector3.UnitY * h * 0.7f;
        Gather(fx, c.Age, c.Seed, mouth, h, 0f, 0.14f, 8, cream, White, 0.08f, 0.6f);
        Fx.Shockwave(fx, c.Age, mouth, Vector3.Zero, 0.15f, 0.25f, h * 0.7f, Fx.Fade(cream, 0.9f));
        Fx.Orb(fx, c, 0.15f, I, 0.3f, Rgb(255, 214, 120), White, FxShape.Glow, cream);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.6f, 9, FxShape.Drop, cream, Rgb(255, 214, 120), 0.12f, 1.1f, 0.8f, FxBlend.Alpha, salt: 86);
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.4f, 6, FxShape.Star, White, Rgb(255, 236, 160), 0.09f, 0.9f, salt: 87);
            fx.Shake = Math.Max(fx.Shake, 0.4f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.3f));
        }
        Impact(c, col, fx, 1.2f);
    }

    /// <summary>Stockpile: the user draws energy in and swells with it, twice, and a star is kept above its head for each.</summary>
    private static void Stockpile(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var cream = Rgb(255, 244, 210);
        foreach (float b in new[] { 0.0f, 0.5f })
        {
            Gather(fx, c.Age, c.Seed + (int)(b * 10f), c.From, h, b, b + 0.4f, 10, cream, White, 0.08f, 1.0f);
            float pulse = Fx.Envelope(c.Age, b + 0.38f, b + 0.42f, b + 0.46f, b + 0.7f);
            if (pulse > 0f) fx.Sprite(FxShape.Glow, c.From, h * (0.7f + 0.3f * pulse), Fx.Fade(cream, pulse * 0.4f), FxBlend.Alpha);
            Fx.Shockwave(fx, c.Age, c.FromFeet + Vector3.UnitY * 0.04f, Vector3.UnitY, b + 0.4f, 0.4f, h * 1.1f, Fx.Fade(cream, 0.8f));
        }
        for (int s = 0; s < 2; s++)
        {
            float t0 = 0.42f + s * 0.5f;
            float env = Fx.Envelope(c.Age, t0, t0 + 0.08f, 1.0f, 1.25f);
            if (env > 0f)
                fx.Sprite(FxShape.Star, c.FromFeet + new Vector3((s - 0.5f) * h * 0.3f, h * 1.15f, 0f), h * 0.12f, Fx.Fade(Rgb(255, 236, 140), env), FxBlend.Alpha, c.Age * 0.5f);
        }
    }

    /// <summary>Sucker Punch: from the target's blind side a dark fist is driven in without warning.</summary>
    private static void SuckerPunch(in FxCtx c, FxColors col, FxList fx)
    {
        var dark = ColorsOf(PokemonType.Dark);
        float h = c.ToSize;
        if (!c.Cue.Missed)
        {
            float shade = Fx.Envelope(c.Age, 0.05f, 0.18f, I - 0.03f, I + 0.1f);
            if (shade > 0f) fx.Sprite(FxShape.Glow, c.To + new Vector3(-0.5f, 0.3f, -0.1f) * h, h * 0.6f, Fx.Fade(Rgb(30, 20, 40), shade * 0.6f), FxBlend.Alpha);
        }
        Strike(c, fx, FxShape.Fist, new Vector3(-1.1f, 0.8f, 0.1f), I, 0.42f, Rgb(82, 56, 108), -0.6f, 0.07f);
        if (c.Cue.Landed)
        {
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.3f, h * 1.2f, Fx.Fade(dark.Light, 0.9f));
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.45f, 7, FxShape.Shard, dark.Main, dark.Light, 0.08f, 1.0f, 0.5f, FxBlend.Alpha, spin: 8f, salt: 88);
            fx.Shake = Math.Max(fx.Shake, 0.4f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.3f));
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Swallow: the stored energy is drawn into the user's mouth and gulped, and a healing light spreads.</summary>
    private static void Swallow(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var cream = Rgb(255, 244, 210);
        var mouth = c.FromFeet + Vector3.UnitY * h * 0.7f;
        Gather(fx, c.Age, c.Seed, mouth, h, 0f, 0.4f, 12, cream, White, 0.09f, 1.1f);
        Fx.Shockwave(fx, c.Age, mouth, Vector3.Zero, 0.4f, 0.3f, h * 0.7f, Fx.Fade(cream, 0.9f));
        float gulp = Fx.Envelope(c.Age, 0.38f, 0.42f, 0.46f, 0.7f);
        if (gulp > 0f) fx.Sprite(FxShape.Glow, c.From, h * 0.9f, Fx.Fade(cream, gulp * 0.45f), FxBlend.Alpha);
        HealOn(fx, c.Age, c.Seed, c.FromFeet, h, 0.2f);
    }

    /// <summary>Will-O-Wisp: pale blue wisps circle the user, then stream across in a wave and set the target alight.</summary>
    private static void WillOWispY8(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var ghost = ColorsOf(PokemonType.Ghost);
        var blue = Rgb(110, 150, 255);
        var pale = Rgb(210, 226, 255);
        float h = c.FromSize;
        float orbit = Fx.Envelope(c.Age, 0f, 0.05f, 0.1f, 0.2f);
        for (int i = 0; i < 3 && orbit > 0f; i++)
        {
            float a = i * MathF.Tau / 3f + c.Age * 12f;
            fx.Sprite(FxShape.Flame, c.From + new Vector3(MathF.Cos(a) * h * 0.55f, 0.2f * h + MathF.Sin(a) * h * 0.1f, 0.1f), h * 0.3f, Fx.Fade(blue, orbit), FxBlend.Alpha, 0.1f * MathF.Sin(a * 2f));
        }
        Fx.Stream(fx, c, 0.08f, I, 0.2f, 6, FxShape.Flame, blue, pale, 0.28f, 0.03f, 0.1f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0.08f, I, 0.2f, 6, FxShape.Glow, Fx.Fade(ghost.Light, 0.6f), Fx.Fade(pale, 0.6f), 0.2f, 0.03f, 0.1f);
        if (c.Cue.Missed) return;
        float t = c.ToSize;
        float env = Fx.Envelope(c.Age, I, I + 0.05f, 0.85f, 1.15f);
        if (env <= 0f) return;
        for (int i = 0; i < 6; i++)
        {
            float flick = 0.75f + 0.25f * MathF.Sin(c.Age * 20f + i * 2.2f);
            var p = c.ToFeet + new Vector3(Fx.Signed(c.Seed, i, 154) * t * 0.4f, t * (0.2f + 0.55f * Fx.Rand(c.Seed, i, 155)), 0.1f);
            fx.Sprite(FxShape.Flame, p, t * 0.3f * flick, Fx.Fade(i % 2 == 0 ? blue : pale, env), FxBlend.Alpha);
        }
        fx.Sprite(FxShape.Glow, c.To, t * 1.1f, Fx.Fade(blue, env * 0.35f));
        fx.Shake = Math.Max(fx.Shake, 0.3f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.08f, I + 0.25f));
    }

    /// <summary>Minimize: rings and arrows close in on the user from every side, and it is left small and bright.</summary>
    private static void Minimize(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.05f, 0.9f, 1.15f);
        if (env <= 0f) return;
        for (int j = 0; j < 3; j++)
        {
            float k = (c.Age - j * 0.2f) / 0.55f;
            if (k < 0f || k > 1f) continue;
            fx.Sprite(FxShape.Ring, c.From, h * (1.5f - 1.15f * Fx.EaseOut(k)), Fx.Fade(Rgb(190, 230, 255), (1f - k * 0.4f) * Math.Min(1f, k * 6f)));
        }
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f;
            float k = Math.Clamp((c.Age - 0.05f - (i % 2) * 0.08f) / 0.5f, 0f, 1f);
            if (k <= 0f || k >= 1f) continue;
            var p = c.From + new Vector3(MathF.Cos(a), MathF.Sin(a) * 0.8f, 0f) * h * (1.3f - 1.0f * Fx.EaseOut(k));
            fx.Sprite(FxShape.Arrow, p, h * 0.14f, Fx.Fade(Rgb(210, 240, 255), MathF.Sin(k * MathF.PI)), FxBlend.Alpha, a - MathF.PI / 2f);
        }
        Fx.Glint(fx, c.Age, c.From + new Vector3(0.1f, 0.05f, 0.1f) * h, h, 0.55f, White, 0.3f);
    }

    /// <summary>Payback: a dark gold aura gathers round the user, then four heavy blows are paid back with coins flying.</summary>
    private static void Payback(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var gold = Rgb(255, 204, 90);
        var dark = ColorsOf(PokemonType.Dark);
        float env = Fx.Envelope(c.Age, 0f, 0.06f, 0.12f, 0.24f);
        if (env > 0f)
        {
            fx.Sprite(FxShape.Glow, c.From, h * 0.95f, Fx.Fade(Rgb(70, 40, 20), env * 0.6f), FxBlend.Alpha);
            fx.Sprite(FxShape.Ring, c.From, h * 0.7f, Fx.Fade(gold, env * 0.8f), rotation: c.Age * 3f);
        }
        Blows(c, col, fx, 4, 0.07f, 0.22f, 0.22f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I - 0.05f, 0.6f, 8, FxShape.Star, gold, Rgb(255, 244, 190), 0.09f, 1.0f, 1.0f, FxBlend.Alpha, spin: 6f, salt: 89);
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.35f, c.ToSize * 1.3f, Fx.Fade(dark.Light, 0.9f));
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Attract: a ring of pink hearts circles the target's head, more floating up from it, and it blushes.</summary>
    private static void Attract(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        if (c.Cue.Missed) return;
        float h = c.ToSize;
        var pink = Rgb(255, 120, 170);
        float env = Fx.Envelope(c.Age, 0.03f, 0.2f, 0.9f, 1.2f);
        if (env <= 0f) return;
        Tint(c, fx, 0.05f, 1.1f, Rgb(255, 140, 180), 0.4f);
        var head = c.ToFeet + Vector3.UnitY * h * 1.0f;
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f + c.Age * 3f;
            var p = head + new Vector3(MathF.Cos(a) * h * 0.35f, MathF.Sin(a * 2f) * h * 0.04f, MathF.Sin(a) * h * 0.1f);
            fx.Sprite(FxShape.Heart, p, h * 0.17f * (1f + 0.15f * MathF.Sin(c.Age * 14f + i)), Fx.Fade(i % 2 == 0 ? pink : Rgb(255, 180, 210), env), FxBlend.Alpha, 0.2f * MathF.Sin(a));
        }
        for (int i = 0; i < 5; i++)
        {
            float u = (c.Age * 0.8f + i / 5f) % 1f;
            fx.Sprite(FxShape.Heart, c.ToFeet + new Vector3(Fx.Signed(c.Seed, i, 156) * h * 0.4f + MathF.Sin(u * 7f + i) * h * 0.1f, h * (0.3f + 0.9f * u), 0.1f), h * 0.13f,
                Fx.Fade(pink, env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha);
        }
        Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, 0.1f, 0.4f, h * 1.1f, Fx.Fade(pink, 0.8f));
    }

    /// <summary>Amnesia: the user's thoughts drift up out of its head as pale wisps, and a hush settles over it.</summary>
    private static void Amnesia(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.9f, 1.2f);
        if (env <= 0f) return;
        var head = c.FromFeet + Vector3.UnitY * h * 0.95f;
        var blue = Rgb(150, 190, 255);
        fx.Sprite(FxShape.Glow, c.From, h * 0.9f, Fx.Fade(Rgb(80, 100, 190), env * 0.3f), FxBlend.Alpha);
        for (int i = 0; i < 6; i++)
        {
            float u = (c.Age * 0.7f + i / 6f) % 1f;
            var p = head + new Vector3(MathF.Sin(u * 6f + i * 1.3f) * h * 0.22f, h * 0.6f * u, 0.1f);
            fx.Sprite(FxShape.Wisp, p, h * 0.22f * (1f - 0.4f * u), Fx.Fade(Fx.Mix(blue, White, u), env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha, u * 3f);
        }
        float qm = Fx.Envelope(c.Age, 0.25f, 0.35f, 0.85f, 1.05f);
        if (qm > 0f)
            for (int j = 0; j < 3; j++)
                fx.Sprite(FxShape.Bubble, head + new Vector3(0.18f * h + j * 0.1f * h, 0.3f * h + j * 0.14f * h, 0f), h * (0.05f + 0.04f * j), Fx.Fade(White, qm));
        Fx.Shockwave(fx, c.Age, c.From, Vector3.Zero, 0.3f, 0.5f, h * 1.2f, Fx.Fade(blue, 0.7f));
    }

    /// <summary>Aqua Ring: a veil of water settles round the user as a turning ring, bubbles riding it.</summary>
    private static void AquaRing(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var water = ColorsOf(PokemonType.Water);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.2f, 0.95f, 1.25f);
        if (env <= 0f) return;
        float drop = 1f - Fx.EaseOut(Math.Min(1f, c.Age / 0.35f));
        for (int r = 0; r < 2; r++)
            fx.Flat(FxShape.Ring, c.FromFeet + Vector3.UnitY * h * (0.2f + 0.45f * r + 0.5f * drop), Vector3.UnitY, h * (0.62f - 0.05f * r), Fx.Fade(water.Light, env * 0.8f), rotation: c.Age * (r == 0 ? 2f : -2f), depth: true);
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f + c.Age * 2.2f;
            var p = c.FromFeet + new Vector3(MathF.Cos(a) * h * 0.6f, h * (0.25f + 0.2f * MathF.Sin(a * 2f + 1f)) + h * 0.4f * drop, MathF.Sin(a) * h * 0.25f);
            bool behind = MathF.Sin(a) < 0f;
            fx.Sprite(FxShape.Dot, p, h * 0.06f, Fx.Fade(water.Main, env * 0.5f), FxBlend.Alpha, depth: behind);
            fx.Sprite(FxShape.Bubble, p, h * 0.07f, Fx.Fade(White, env), depth: behind);
        }
        fx.Sprite(FxShape.Glow, c.From, h * 0.95f, Fx.Fade(water.Light, env * 0.2f));
    }

    /// <summary>Copycat: twin trails spiral from the foe to the user, and the user flashes gold as it takes the move.</summary>
    private static void Copycat(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var gold = Rgb(255, 214, 100);
        var pale = Rgb(255, 244, 200);
        float hf = c.FoeSize;
        float env = Fx.Envelope(c.Age, 0f, 0.08f, 0.2f, 0.4f);
        if (env > 0f) fx.Sprite(FxShape.Ring, c.Foe, hf * 0.7f, Fx.Fade(gold, env * 0.8f));
        for (int s = 0; s < 2; s++)
            for (int i = 0; i < 6; i++)
            {
                float k = (c.Age - 0.12f - i * 0.05f) / 0.4f;
                if (k < 0f || k > 1f) continue;
                var p = Vector3.Lerp(c.Foe, c.From, Fx.EaseOut(k));
                var (across, up) = Fx.Frame(c.From - c.Foe);
                float sway = MathF.Sin(k * MathF.PI * 2f + s * MathF.PI) * c.FromSize * 0.3f;
                fx.Sprite(FxShape.Orb, p + up * sway, c.FromSize * 0.09f, Fx.Fade(s == 0 ? gold : pale, MathF.Sin(k * MathF.PI)), FxBlend.Alpha);
                fx.Sprite(FxShape.Glow, p + up * sway, c.FromSize * 0.18f, Fx.Fade(gold, MathF.Sin(k * MathF.PI) * 0.6f));
            }
        float flash = Fx.Envelope(c.Age, 0.5f, 0.54f, 0.58f, 0.85f);
        if (flash > 0f) fx.Sprite(FxShape.Glow, c.From, c.FromSize * 1.0f, Fx.Fade(gold, flash * 0.5f), FxBlend.Alpha);
        Fx.Glint(fx, c.Age, c.From + Vector3.UnitY * c.FromSize * 0.3f, c.FromSize, 0.52f, White, 0.4f);
    }

    /// <summary>Fury Attack: needles jab in from either side in quick turns, a spark at each.</summary>
    private static void FuryAttack(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var bone = Rgb(255, 238, 200);
        if (!c.Cue.Missed)
            for (int i = 0; i < 4; i++)
            {
                float at = I - (3 - i) * 0.06f;
                float k = (c.Age - (at - 0.06f)) / 0.06f;
                float after = (c.Age - at) / 0.14f;
                if (k < 0f || after > 1f) continue;
                float side = i % 2 == 0 ? -1f : 1f;
                var dir = new Vector3(side, -0.25f * side + 0.15f, 0f);
                var p = c.To - dir * h * (0.85f - 0.55f * Fx.EaseOut(Math.Min(1f, k)));
                float fade = Math.Min(1f, k * 4f) * (1f - Math.Max(0f, after));
                fx.Streak(FxShape.Streak, p, dir, h * 0.13f, 3.4f, Fx.Fade(bone, fade), FxBlend.Alpha);
                fx.Streak(FxShape.Streak, p, dir, h * 0.055f, 3.4f, Fx.Fade(White, fade));
                if (i < 3) Fx.Contact(fx, c.Age, c.Seed + i, c.To + new Vector3(Fx.Signed(c.Seed, i, 157), Fx.Signed(c.Seed, i, 158), 0.1f) * h * 0.2f, h, at, 0.15f, bone, White);
            }
        SpeedLines(c, fx, 0.02f, 0.22f, 4, 0.5f);
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Jump Kick: the user leaps in a high arc and a foot comes down on the target from above, spinning.</summary>
    private static void JumpKick(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = (c.Age - 0.02f) / (I - 0.02f);
        var skin = Rgb(255, 224, 196);
        if (k > 0f && k < 1.15f)
        {
            float u = Math.Min(1f, k);
            var start = c.From + Vector3.UnitY * c.FromSize * 0.3f;
            var end = c.Cue.Missed ? c.Aim : c.To + Vector3.UnitY * h * 0.05f;
            for (int g = 3; g >= 0; g--)
            {
                float uu = Math.Max(0f, u - g * 0.07f);
                var p = Lob(start, end, uu, 0.07f);
                var turn = -0.9f + 1.2f * uu;
                fx.Sprite(FxShape.Foot, p, c.FromSize * 0.42f, Fx.Fade(skin, (g == 0 ? 1f : 0.4f / g) * (k < 1f ? 1f : 1f - (k - 1f) * 6f)), FxBlend.Alpha, turn);
            }
        }
        if (c.Cue.Landed)
        {
            GroundRing(fx, c.Age, c.ToFeet, I, 0.35f, h * 1.3f, Fx.Fade(Rgb(240, 230, 210), 0.8f));
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.45f, 6, FxShape.Star, White, Rgb(255, 236, 160), 0.09f, 1.0f, salt: 90);
            fx.Shake = Math.Max(fx.Shake, 0.4f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.3f));
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Light Screen: a curtain of golden light rises in front of the user's side, shimmering, and a glint runs along it.</summary>
    private static void LightScreen(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.2f, 0.9f, 1.2f);
        if (env <= 0f) return;
        var gold = Rgb(255, 226, 120);
        var pale = Rgb(255, 250, 214);
        float rise = Fx.EaseOut(Math.Min(1f, c.Age / 0.3f));
        var front = c.FromFeet + new Vector3(0f, 0f, 0.55f * h);
        for (int i = 0; i < 9; i++)
        {
            float x = (i - 4) * h * 0.2f;
            float shimmer = 0.55f + 0.45f * MathF.Sin(c.Age * 11f + i * 1.3f);
            fx.Streak(FxShape.Streak, front + new Vector3(x, h * 0.65f * rise, 0f), Vector3.UnitY, h * 0.11f, 4.4f * rise, Fx.Fade(gold, env * 0.38f * shimmer), FxBlend.Alpha);
            fx.Streak(FxShape.Streak, front + new Vector3(x, h * 0.65f * rise, 0f), Vector3.UnitY, h * 0.035f, 4.4f * rise, Fx.Fade(pale, env * 0.6f * shimmer));
        }
        float sweep = (c.Age - 0.25f) / 0.5f;
        if (sweep > 0f && sweep < 1f)
            Fx.Glint(fx, c.Age, front + new Vector3((sweep * 8f - 4f) * h * 0.2f, h * 0.75f, 0f), h, c.Age - 0.1f, White, 0.3f);
        GroundRing(fx, c.Age, c.FromFeet, 0.08f, 0.5f, h * 1.2f, Fx.Fade(gold, 0.8f));
        for (int i = 0; i < 6; i++)
        {
            float u = (c.Age * 0.9f + Fx.Rand(c.Seed, i, 159)) % 1f;
            fx.Sprite(FxShape.Spark, front + new Vector3(Fx.Signed(c.Seed, i, 160) * h * 0.8f, h * 1.3f * u, 0f), h * 0.06f, Fx.Fade(pale, env * MathF.Sin(u * MathF.PI)));
        }
    }

    /// <summary>Nasty Plot: two red eyes narrow over the user and dark wisps coil up round it.</summary>
    private static void NastyPlot(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var dark = ColorsOf(PokemonType.Dark);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.12f, 0.95f, 1.25f);
        if (env <= 0f) return;
        fx.Sprite(FxShape.Glow, c.From, h * 0.95f, Fx.Fade(Rgb(60, 20, 70), env * 0.4f), FxBlend.Alpha);
        for (int i = 0; i < 7; i++)
        {
            float u = (c.Age * 0.7f + i / 7f) % 1f;
            float a = i * 0.9f + u * 5f;
            var p = c.FromFeet + new Vector3(MathF.Cos(a) * h * 0.45f * (1f - 0.4f * u), h * (0.05f + 1.0f * u), MathF.Sin(a) * h * 0.2f);
            fx.Sprite(FxShape.Wisp, p, h * 0.15f, Fx.Fade(i % 2 == 0 ? dark.Main : Rgb(120, 40, 130), env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha, a, depth: MathF.Sin(a) < 0f);
        }
        float squint = 0.5f + 0.5f * Fx.EaseOut(Math.Min(1f, c.Age / 0.5f));
        var head = c.FromFeet + Vector3.UnitY * h * 1.0f;
        foreach (float side in new[] { -1f, 1f })
        {
            fx.Sprite(FxShape.Eye, head + new Vector3(side * h * 0.18f, 0f, 0f), h * 0.16f * (1.2f - 0.5f * squint), Fx.Fade(Rgb(255, 70, 70), env), FxBlend.Alpha, side * 0.25f);
            fx.Sprite(FxShape.Glow, head + new Vector3(side * h * 0.18f, 0f, 0f), h * 0.18f, Fx.Fade(Rgb(255, 60, 60), env * 0.5f));
        }
        Fx.Glint(fx, c.Age, head, h, 0.7f, Rgb(255, 150, 150), 0.3f);
    }

    /// <summary>Refresh: a soft white-green shower of light falls on the user and washes through it.</summary>
    private static void Refresh(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.8f, 1.1f);
        if (env <= 0f) return;
        Fx.Rain(fx, c.Age, c.Seed, c.From, h, 0.05f, 0.9f, 0.4f, 16, FxShape.Star, Rgb(220, 255, 230), White, 0.06f);
        fx.Sprite(FxShape.Glow, c.From, h * 1.0f, Fx.Fade(Rgb(190, 255, 220), env * 0.35f), FxBlend.Alpha);
        for (int j = 0; j < 3; j++)
        {
            float k = (c.Age - 0.3f - j * 0.12f) / 0.4f;
            if (k < 0f || k > 1f) continue;
            fx.Flat(FxShape.Ring, c.FromFeet + Vector3.UnitY * h * (1.0f - 0.9f * k), Vector3.UnitY, h * 0.6f, Fx.Fade(Rgb(190, 255, 220), (1f - k) * 0.8f), depth: true);
        }
        Fx.Glint(fx, c.Age, c.From + new Vector3(0.1f, 0.1f, 0.1f) * h, h, 0.55f, White, 0.4f);
    }

    /// <summary>Soft-Boiled: a cream egg forms over the user, wobbles and cracks, and healing light spills from it.</summary>
    private static void SoftBoiled(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var shell = Rgb(255, 242, 214);
        var yolk = Rgb(255, 200, 80);
        float env = Fx.Envelope(c.Age, 0.02f, 0.18f, 0.5f, 0.62f);
        var at = c.FromFeet + new Vector3(0f, h * 0.95f, 0.1f);
        if (env > 0f)
        {
            float wob = MathF.Sin(c.Age * 18f) * 0.2f * Fx.Envelope(c.Age, 0.25f, 0.3f, 0.45f, 0.55f);
            fx.Sprite(FxShape.Orb, at, h * 0.4f * Fx.EaseOut(Math.Min(1f, (c.Age - 0.02f) / 0.2f)), Fx.Fade(shell, env), FxBlend.Alpha, wob);
            fx.Sprite(FxShape.Orb, at - Vector3.UnitY * h * 0.02f, h * 0.19f, Fx.Fade(yolk, env * 0.9f), FxBlend.Alpha, wob);
            fx.Sprite(FxShape.Glow, at, h * 0.55f, Fx.Fade(shell, env * 0.3f));
        }
        float pop = Fx.Envelope(c.Age, 0.5f, 0.54f, 0.6f, 0.9f);
        if (pop > 0f)
        {
            Fx.Burst(fx, c.Seed, c.Age, at, h, 0.5f, 0.45f, 6, FxShape.Shard, shell, White, 0.07f, 0.7f, 1.2f, FxBlend.Alpha, spin: 7f, salt: 91);
            fx.Sprite(FxShape.Glow, at, h * 0.5f, Fx.Fade(yolk, pop * 0.5f));
        }
        HealOn(fx, c.Age, c.Seed, c.FromFeet, h, 0.2f);
    }

    /// <summary>Steel Wing: both wings turn to steel and sweep across the target in an X, glints running down their edges.</summary>
    private static void SteelWing(in FxCtx c, FxColors col, FxList fx)
    {
        var steel = ColorsOf(PokemonType.Steel);
        float h = c.ToSize;
        float hu = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.05f, 0.1f, 0.2f);
        if (env > 0f) fx.Sprite(FxShape.Glow, c.From, hu * 0.9f, Fx.Fade(steel.Main, env * 0.35f), FxBlend.Alpha);
        if (!c.Cue.Missed)
            for (int w = 0; w < 2; w++)
            {
                float t0 = I - 0.2f + w * 0.1f;
                float k = (c.Age - t0) / 0.3f;
                if (k < 0f || k > 1f) continue;
                float side = w == 0 ? -1f : 1f;
                float sweep = Fx.EaseOut(Math.Min(1f, k * 2f));
                var p = c.To + new Vector3(side * h * (0.9f - 1.8f * sweep) * -1f * 0.5f, h * (0.5f - 0.9f * sweep) * -side * 0.5f, 0.1f * h);
                float ang = side * (0.9f - 1.4f * sweep) + (w == 0 ? 0.5f : 2.6f);
                float fade = k < 0.7f ? 1f : (1f - k) / 0.3f;
                fx.Sprite(FxShape.Sword, p, h * 0.55f, Fx.Fade(Rgb(206, 218, 238), fade), FxBlend.Alpha, ang);
                fx.Sprite(FxShape.Sword, p, h * 0.58f, Fx.Fade(White, fade * 0.35f), rotation: ang);
                Fx.Glint(fx, c.Age, p + new Vector3(0.1f, 0.1f, 0f) * h, h, t0 + 0.1f, White, 0.3f);
            }
        for (int i = 0; i < 4; i++)
            Fx.Glint(fx, c.Age, c.To + new Vector3(Fx.Signed(c.Seed, i, 161) * 0.45f, Fx.Signed(c.Seed, i, 162) * 0.35f, 0.15f) * h, h, I - 0.12f + i * 0.04f, steel.Light, 0.28f);
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Sweet Kiss: a big pink heart pops over the target's head, small hearts burst from it and stars circle in a daze.</summary>
    private static void SweetKiss(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        if (c.Cue.Missed) return;
        float h = c.ToSize;
        var pink = Rgb(255, 130, 180);
        var head = c.ToFeet + Vector3.UnitY * h * 1.05f;
        float pop = Fx.EaseOut(Math.Clamp((c.Age - 0.05f) / 0.15f, 0f, 1f));
        float env = Fx.Envelope(c.Age, 0.05f, 0.12f, 0.45f, 0.65f);
        if (env > 0f)
        {
            fx.Sprite(FxShape.Heart, head, h * 0.32f * pop, Fx.Fade(pink, env), FxBlend.Alpha, 0.1f * MathF.Sin(c.Age * 12f));
            fx.Sprite(FxShape.Glow, head, h * 0.5f, Fx.Fade(Rgb(255, 180, 210), env * 0.4f));
        }
        Fx.Burst(fx, c.Seed, c.Age, head, h, 0.2f, 0.6f, 8, FxShape.Heart, pink, Rgb(255, 200, 224), 0.07f, 0.8f, -0.2f, FxBlend.Alpha, salt: 92);
        float daze = Fx.Envelope(c.Age, 0.5f, 0.6f, 1.0f, 1.2f);
        for (int i = 0; i < 3 && daze > 0f; i++)
        {
            float a = i * MathF.Tau / 3f + c.Age * 6f;
            fx.Sprite(FxShape.Star, head + new Vector3(MathF.Cos(a) * h * 0.28f, 0f, MathF.Sin(a) * h * 0.1f), h * 0.08f, Fx.Fade(Rgb(255, 170, 210), daze), FxBlend.Alpha, c.Age * 4f);
        }
        Tint(c, fx, 0.5f, 1.15f, Rgb(255, 150, 190), 0.4f);
    }

    /// <summary>Whirlwind: a great wind sweeps across the field and the target is blown along with it, leaves and dust flying.</summary>
    private static void Whirlwind(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        var air = ColorsOf(PokemonType.Flying);
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.8f, 1.1f);
        if (env <= 0f) return;
        var dir = Vector3.Normalize(c.Aim - c.From);
        float roll = Math.Min(1f, c.Age / (I + 0.05f));
        for (int s = 0; s < 3; s++)
            fx.Sprite(FxShape.Swirl, Vector3.Lerp(c.From, c.Aim, Fx.EaseOut(roll)) + Vector3.UnitY * h * (s - 1) * 0.35f, h * (0.8f + 0.2f * s), Fx.Fade(air.Light, env * (0.6f - 0.12f * s)), FxBlend.Alpha, c.Age * (7f + s) * (s % 2 == 0 ? 1f : -1f));
        for (int i = 0; i < 14; i++)
        {
            float u = (c.Age * 1.5f + Fx.Rand(c.Seed, i, 163)) % 1f;
            var p = Vector3.Lerp(c.From, c.Aim + dir * h * 2.5f, u) + new Vector3(0f, Fx.Signed(c.Seed, i, 164) * h * 0.8f + h * 0.3f, Fx.Signed(c.Seed, i, 165) * h * 0.3f);
            fx.Streak(FxShape.Streak, p, dir, h * 0.045f, 8f, Fx.Fade(White, env * 0.8f * MathF.Sin(u * MathF.PI)));
        }
        if (c.Cue.Missed) return;
        float blown = Fx.Envelope(c.Age, I, I + 0.04f, 0.8f, 1.1f);
        for (int i = 0; i < 6; i++)
        {
            float u = (c.Age - I + Fx.Rand(c.Seed, i, 166) * 0.2f) / 0.6f;
            if (u < 0f || u > 1f) continue;
            var p = c.To + dir * h * (0.2f + 2.2f * u) + new Vector3(0f, Fx.Signed(c.Seed, i, 167) * h * 0.5f + h * 0.2f * MathF.Sin(u * 9f), 0f);
            fx.Sprite(i % 2 == 0 ? FxShape.Leaf : FxShape.Smoke, p, h * 0.12f, Fx.Fade(i % 2 == 0 ? Rgb(150, 210, 110) : Rgb(220, 214, 200, 220), blown * (1f - u)), FxBlend.Alpha, u * 12f + i);
        }
        fx.Shake = Math.Max(fx.Shake, 0.4f * Fx.Envelope(c.Age, I, I + 0.04f, I + 0.35f, I + 0.55f));
    }

    /// <summary>Chatter: bright notes of every colour burst from the user in a babble, spreading as they cross to the target.</summary>
    private static void Chatter(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var palette = new[] { Rgb(255, 120, 150), Rgb(255, 214, 90), Rgb(120, 220, 255), Rgb(160, 230, 120), Rgb(200, 150, 255) };
        var mouth = c.FromFeet + Vector3.UnitY * h * 0.8f;
        for (int i = 0; i < 16; i++)
        {
            float leave = 0.02f + i * 0.04f;
            float k = (c.Age - leave) / 0.4f;
            if (k < 0f || k > 1f) continue;
            var (across, up) = Fx.Frame(c.Aim - mouth);
            var p = Vector3.Lerp(mouth, c.Aim, Fx.EaseOut(k)) + (across * Fx.Signed(c.Seed, i, 168) + up * Fx.Signed(c.Seed, i, 169)) * h * 0.5f * MathF.Sin(k * 1.6f)
                + Vector3.UnitY * h * 0.2f * MathF.Sin(k * MathF.PI);
            fx.Sprite(FxShape.Note, p, h * 0.2f, Fx.Fade(palette[i % 5], Math.Min(1f, (1f - k) * 3f)), FxBlend.Alpha, Fx.Signed(c.Seed, i, 170) * 0.5f);
        }
        Fx.Waves(fx, c, 0.03f, 0.5f, 3, FxShape.Wave, Rgb(255, 236, 190), 0.4f, 0.25f);
        Fx.Glint(fx, c.Age, mouth, h, 0.04f, White, 0.3f);
    }

    /// <summary>Mirror Move: nothing of its own in the original; a mirror of silver light stands before the user, flashes and sends a glint back.</summary>
    private static void MirrorMove(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var silver = Rgb(214, 226, 248);
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.5f, 0.75f);
        if (env <= 0f) return;
        var dir = Vector3.Normalize(c.Foe - c.From);
        var at = c.From + dir * h * 0.8f + Vector3.UnitY * h * 0.1f;
        Panel(fx, c.Age, at, h * 0.6f, 0f, silver);
        Panel(fx, c.Age, at, h * 0.5f, 0.02f, White);
        float sweep = (c.Age - 0.12f) / 0.3f;
        if (sweep > 0f && sweep < 1f)
            Fx.Glint(fx, c.Age, at + new Vector3((sweep - 0.5f) * h * 0.8f, (0.5f - sweep) * h * 0.6f, 0.1f), h, c.Age - 0.05f, White, 0.45f);
        float back = (c.Age - I + 0.1f) / 0.3f;
        if (back > 0f && back < 1f)
            fx.Streak(FxShape.Streak, Vector3.Lerp(at, c.Foe, back), c.Foe - at, h * 0.05f, 4f, Fx.Fade(silver, 1f - back));
        Fx.Shockwave(fx, c.Age, at, Vector3.Zero, 0.25f, 0.35f, h * 0.8f, Fx.Fade(silver, 0.8f));
    }

    /// <summary>Haze: a grey mist rolls across the whole field and thickens, the colours under it going dull.</summary>
    private static void Haze(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = Math.Max(c.FromSize, c.FoeSize);
        float env = Fx.Envelope(c.Age, 0f, 0.3f, 0.9f, 1.2f);
        if (env <= 0f) return;
        var mid = Vector3.Lerp(c.FromFeet, c.Foe - Vector3.UnitY * c.FoeSize * 0.5f, 0.5f);
        for (int i = 0; i < 16; i++)
        {
            float u = (c.Age * 0.35f + Fx.Rand(c.Seed, i, 171)) % 1f;
            var p = mid + new Vector3(-9f + 18f * u, h * (0.0f + 0.7f * Fx.Rand(c.Seed, i, 172)), Fx.Signed(c.Seed, i, 173) * h * 2.4f);
            fx.Sprite(FxShape.Smoke, p, h * (1.0f + 0.6f * Fx.Rand(c.Seed, i, 174)), Fx.Fade(Rgb(176, 184, 198, 230), env * 0.85f * MathF.Sin(u * MathF.PI)), FxBlend.Alpha, i * 1.3f + c.Age * 0.3f);
        }
        fx.Sprite(FxShape.Glow, mid + Vector3.UnitY * h * 0.5f, h * 3.5f, Fx.Fade(Rgb(150, 160, 178), env * 0.25f), FxBlend.Alpha);
    }

    /// <summary>Defog: a bright wind sweeps the field from the user across and past the foe, shoving grey cloud before it.</summary>
    private static void DefogY8(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        var air = ColorsOf(PokemonType.Flying);
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.85f, 1.15f);
        if (env <= 0f) return;
        var dir = Vector3.Normalize(c.Aim - c.From);
        float roll = Fx.EaseOut(Math.Min(1f, c.Age / 0.7f));
        fx.Sprite(FxShape.Swirl, Vector3.Lerp(c.From, c.Aim + dir * h * 2f, roll), h * 1.2f, Fx.Fade(air.Light, env * 0.7f), FxBlend.Alpha, c.Age * 9f);
        for (int i = 0; i < 9; i++)
        {
            float u = Math.Clamp(roll * 1.3f - Fx.Rand(c.Seed, i, 175) * 0.3f, 0f, 1f);
            var p = Vector3.Lerp(c.From, c.Aim + dir * h * 3f, u) + new Vector3(0f, h * (0.1f + 1.0f * Fx.Rand(c.Seed, i, 176)), Fx.Signed(c.Seed, i, 177) * h * 0.4f);
            fx.Sprite(FxShape.Smoke, p, h * 0.5f, Fx.Fade(Rgb(150, 156, 172, 200), env * (1f - u) * 0.8f), FxBlend.Alpha, i * 1.7f);
        }
        for (int i = 0; i < 10; i++)
        {
            float u = (c.Age * 1.6f + Fx.Rand(c.Seed, i, 178)) % 1f;
            var p = Vector3.Lerp(c.From, c.Aim + dir * h * 3f, u) + new Vector3(0f, h * Fx.Rand(c.Seed, i, 179) * 1.2f, Fx.Signed(c.Seed, i, 180) * h * 0.3f);
            fx.Streak(FxShape.Streak, p, dir, h * 0.04f, 8f, Fx.Fade(White, env * 0.8f * MathF.Sin(u * MathF.PI)));
        }
        if (!c.Cue.Missed) Fx.Glint(fx, c.Age, c.To, h, I, White, 0.4f);
    }

    /// <summary>Dig: dirt spurts up at the user's feet, a mound runs along the ground to the target and the user bursts up under it.</summary>
    private static void Dig(in FxCtx c, FxColors col, FxList fx)
    {
        var earth = ColorsOf(PokemonType.Ground);
        float h = c.ToSize;
        foreach (float t0 in new[] { 0f, 0.05f, 0.1f })
            Fx.Burst(fx, c.Seed, c.Age, c.FromFeet + Vector3.UnitY * 0.1f, c.FromSize, t0, 0.4f, 5, FxShape.Rock, earth.Main, earth.Dark, 0.07f, 0.7f, 1.6f, FxBlend.Alpha, spin: 6f, salt: 93);
        Dust(c, fx, c.FromFeet, c.FromSize, 0f, 5);
        float run = Math.Clamp((c.Age - 0.05f) / (I - 0.1f), 0f, 1f);
        float env = Fx.Envelope(c.Age, 0.05f, 0.1f, I - 0.06f, I + 0.1f);
        if (env > 0f)
            for (int j = 0; j < 6; j++)
            {
                float u = Math.Max(0f, run - j * 0.09f);
                var p = Vector3.Lerp(c.FromFeet, c.ToFeet, u) + Vector3.UnitY * (0.06f + 0.05f * MathF.Sin(c.Age * 20f + j));
                fx.Sprite(FxShape.Smoke, p, h * 0.2f, Fx.Fade(earth.Dark, env * (1f - j * 0.12f)), FxBlend.Alpha, j);
            }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + Vector3.UnitY * h * 0.1f, h, I, 0.65f, 9, FxShape.Rock, earth.Main, earth.Light, 0.1f, 1.1f, 2.2f, FxBlend.Alpha, spin: 6f, salt: 94);
            GroundRing(fx, c.Age, c.ToFeet, I, 0.45f, h * 1.6f, Fx.Fade(earth.Light, 0.9f));
            Dust(c, fx, c.ToFeet, h * 1.2f, I, 8);
            fx.Shake = Math.Max(fx.Shake, 0.4f * Fx.Envelope(c.Age, I, I + 0.03f, I + 0.1f, I + 0.3f));
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Yawn: the user opens wide and breathes out a drowsy cloud that drifts to the target, which yawns in turn, Z's rising.</summary>
    private static void Yawn(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var mist = Rgb(190, 214, 255);
        float h = c.FromSize;
        var mouth = c.FromFeet + Vector3.UnitY * h * 0.7f;
        float k = (c.Age - 0.1f) / (I + 0.1f - 0.1f);
        if (k > 0f && k < 1.4f)
        {
            float u = Math.Min(1f, k);
            float fade = k < 1f ? 1f : Math.Max(0f, 1f - (k - 1f) * 2.5f);
            var p = Lob(mouth, c.Aim, Fx.EaseOut(u), 0.05f);
            float grow = 0.2f + 0.8f * u;
            for (int i = 0; i < 4; i++)
                fx.Sprite(FxShape.Smoke, p + new Vector3(Fx.Signed(c.Seed, i, 181), Fx.Signed(c.Seed, i, 182), 0f) * h * 0.2f * grow, h * 0.7f * grow, Fx.Fade(mist, fade * 0.85f), FxBlend.Alpha, i + c.Age);
            fx.Sprite(FxShape.Bubble, p, h * 0.9f * grow, Fx.Fade(White, fade * 0.8f));
        }
        if (c.Cue.Missed) return;
        float ht = c.ToSize;
        float env = Fx.Envelope(c.Age, I, I + 0.1f, 0.85f, 1.2f);
        if (env <= 0f) return;
        fx.Sprite(FxShape.Smoke, c.To, ht * 0.9f, Fx.Fade(mist, env * 0.4f), FxBlend.Alpha, c.Age);
        for (int i = 0; i < 3; i++)
        {
            float u = (c.Age * 0.9f + i / 3f) % 1f;
            fx.Sprite(FxShape.Zzz, c.ToFeet + new Vector3(0.2f * ht + MathF.Sin(u * 6f + i) * 0.1f * ht, ht * (0.9f + 0.5f * u), 0f), ht * (0.1f + 0.08f * u), Fx.Fade(Rgb(190, 214, 255), env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha);
        }
    }

    /// <summary>Acupressure: three points of the body are pressed in turn, each pinging a ring, and a warm yellow light fills it.</summary>
    private static void Acupressure(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        var yellow = Rgb(255, 238, 130);
        var points = new[] { new Vector3(0.18f, 0.78f, 0.1f), new Vector3(-0.2f, 0.52f, 0.1f), new Vector3(0.12f, 0.26f, 0.1f) };
        float glow = Fx.Envelope(c.Age, 0.15f, 0.35f, 0.9f, 1.2f);
        if (glow > 0f) fx.Sprite(FxShape.Glow, c.To, h * 0.95f, Fx.Fade(yellow, glow * 0.35f), FxBlend.Alpha);
        for (int i = 0; i < 3; i++)
        {
            float t0 = 0.05f + i * 0.14f;
            var at = c.ToFeet + points[i] * h;
            float k = (c.Age - t0) / 0.5f;
            if (k < 0f || k > 1f) continue;
            fx.Sprite(FxShape.Dot, at, h * 0.06f * (1f + 0.4f * MathF.Sin(k * 12f)), Fx.Fade(White, 1f - k), FxBlend.Alpha);
            fx.Sprite(FxShape.Ring, at, h * (0.08f + 0.4f * Fx.EaseOut(k)), Fx.Fade(yellow, (1f - k) * 0.9f));
            Fx.Glint(fx, c.Age, at, h, t0, White, 0.25f);
        }
        Fx.Aura(fx, c.Age, c.Seed, c.ToFeet, h, 0.45f, 1.15f, yellow, White, 8);
    }

    /// <summary>Air Cutter: two crescents of wind sweep in from either corner and cross on the target, trailing streaks.</summary>
    private static void AirCutter(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var air = ColorsOf(PokemonType.Flying);
        var green = Rgb(200, 245, 215);
        if (!c.Cue.Missed)
            for (int b = 0; b < 2; b++)
            {
                float side = b == 0 ? -1f : 1f;
                float k = (c.Age - (I - 0.22f + b * 0.05f)) / 0.22f;
                if (k < 0f || k > 1.6f) continue;
                float u = Math.Min(1f, k);
                var start = c.To + new Vector3(-side * 1.5f, 0.9f, 0.1f) * h;
                var end = c.To + new Vector3(side * 0.6f, -0.5f, 0.1f) * h;
                var p = Vector3.Lerp(start, end, Fx.EaseOut(u));
                var dir = end - start;
                float fade = k < 1f ? 1f : Math.Max(0f, 1f - (k - 1f) * 1.6f);
                float ang = MathF.Atan2(dir.Y, dir.X) + MathF.PI / 2f;
                fx.Sprite(FxShape.Wave, p, h * 0.8f, Fx.Fade(green, fade), FxBlend.Alpha, ang);
                fx.Sprite(FxShape.Wave, p, h * 0.83f, Fx.Fade(White, fade * 0.6f), rotation: ang);
                fx.Streak(FxShape.Streak, p - dir * 0.12f, dir, h * 0.09f, 6f, Fx.Fade(air.Light, fade * 0.9f));
            }
        if (c.Cue.Landed)
        {
            fx.Shake = Math.Max(fx.Shake, 0.25f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.25f));
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 6, FxShape.Leaf, green, White, 0.07f, 0.9f, 0.3f, FxBlend.Alpha, spin: 8f, salt: 95);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>False Swipe: one clean pale slash that is pulled at the last, ending in a soft ring and a few stars.</summary>
    private static void FalseSwipe(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        if (!c.Cue.Missed)
        {
            Fx.Slash(fx, c.Age, c.Seed, c.To, h, I - 0.1f, 1, 0.1f, Rgb(255, 238, 190), White);
            Strike(c, fx, FxShape.Sword, new Vector3(-0.9f, 0.9f, 0.1f), I - 0.02f, 0.45f, Rgb(236, 240, 250), -1.0f, 0.1f);
            float hold = Fx.Envelope(c.Age, I + 0.03f, I + 0.08f, I + 0.3f, I + 0.5f);
            if (hold > 0f)
            {
                fx.Sprite(FxShape.Ring, c.To, h * 0.5f, Fx.Fade(Rgb(255, 238, 190), hold * 0.8f));
                for (int i = 0; i < 3; i++)
                    fx.Sprite(FxShape.Star, c.To + new Vector3(Fx.Signed(c.Seed, i, 183) * 0.4f, 0.3f + 0.15f * i, 0.1f) * h, h * 0.07f, Fx.Fade(Rgb(255, 236, 160), hold), FxBlend.Alpha, c.Age * 3f + i);
            }
        }
        Impact(c, col, fx, 0.6f);
    }

    /// <summary>Feint: two quick false thrusts flicker in from the sides, pale and thin, before the true blow goes in.</summary>
    private static void Feint(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        if (!c.Cue.Missed)
        {
            foreach (var (t0, side) in new[] { (0.04f, -1f), (0.14f, 1f) })
            {
                float k = (c.Age - t0) / 0.14f;
                if (k < 0f || k > 1f) continue;
                var p = c.To + new Vector3(side * h * (1.1f - 0.8f * Fx.EaseOut(k)), 0.1f * h, 0.1f);
                fx.Streak(FxShape.Streak, p, new Vector3(-side, 0f, 0f), h * 0.1f, 5f, Fx.Fade(Rgb(214, 220, 236), 0.85f * MathF.Sin(k * MathF.PI)), FxBlend.Alpha);
                fx.Sprite(FxShape.Glow, p, h * 0.2f, Fx.Fade(White, 0.25f * MathF.Sin(k * MathF.PI)));
            }
            var dark = ColorsOf(PokemonType.Normal);
            float k2 = (c.Age - (I - 0.1f)) / 0.1f;
            if (k2 > 0f && k2 < 2f)
            {
                var p = c.To + new Vector3(-h * 0.9f * (1f - Fx.EaseOut(Math.Min(1f, k2))), 0.25f * h, 0.1f);
                fx.Streak(FxShape.Streak, p, Vector3.UnitX, h * 0.08f, 6f, Fx.Fade(White, Math.Max(0f, 1f - (k2 - 1f))), FxBlend.Alpha);
                fx.Streak(FxShape.Streak, p, Vector3.UnitX, h * 0.035f, 6f, Fx.Fade(dark.Light, Math.Max(0f, 1f - (k2 - 1f))));
            }
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Metronome: a pendulum swings above the user, ticking, and a ring of every colour pops where it settles.</summary>
    private static void Metronome(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.95f, 1.2f);
        if (env <= 0f) return;
        var pivot = c.FromFeet + new Vector3(h * 0.7f, h * 0.3f, 0.2f * h);
        var wood = Rgb(206, 150, 90);
        float swing = MathF.Sin(c.Age * 14f) * 0.75f * (1f - Fx.EaseOut(Math.Clamp((c.Age - 0.7f) / 0.4f, 0f, 1f)));
        var tip = pivot + new Vector3(MathF.Sin(swing), MathF.Cos(swing), 0f) * h * 0.8f;
        // The body of the metronome
        fx.Ribbon(new[] { pivot + new Vector3(-h * 0.3f, -h * 0.02f, 0f), pivot + new Vector3(0f, h * 0.8f, 0f) }, h * 0.09f, Fx.Fade(wood, env), FxBlend.Alpha);
        fx.Ribbon(new[] { pivot + new Vector3(h * 0.3f, -h * 0.02f, 0f), pivot + new Vector3(0f, h * 0.8f, 0f) }, h * 0.09f, Fx.Fade(wood, env), FxBlend.Alpha);
        fx.Ribbon(new[] { pivot, tip }, h * 0.05f, Fx.Fade(White, env), FxBlend.Alpha);
        fx.Sprite(FxShape.Dot, tip, h * 0.11f, Fx.Fade(Rgb(255, 220, 120), env), FxBlend.Alpha);
        float tick = MathF.Abs(MathF.Cos(c.Age * 14f));
        if (tick < 0.12f) fx.Sprite(FxShape.Spark, pivot + new Vector3(MathF.Sign(swing) * h * 0.4f, h * 0.2f, 0f), h * 0.1f, Fx.Fade(White, env));
        float ring = (c.Age - 0.8f) / 0.35f;
        if (ring > 0f && ring < 1f)
        {
            var tones = new[] { Rgb(255, 120, 150), Rgb(255, 214, 90), Rgb(120, 220, 255) };
            for (int i = 0; i < 3; i++)
                fx.Sprite(FxShape.Ring, c.From, h * (0.3f + (0.5f + 0.15f * i) * Fx.EaseOut(ring)), Fx.Fade(tones[i], (1f - ring) * 0.9f));
        }
    }

    /// <summary>Pin Missile: three needles are lobbed one after another and sink into the target, each with a small flash.</summary>
    private static void PinMissile(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var green = Rgb(214, 244, 150);
        var from = c.From + Vector3.UnitY * c.FromSize * 0.1f;
        for (int i = 0; i < 3; i++)
        {
            float land = I - (2 - i) * 0.055f;
            float k = (c.Age - (land - 0.17f)) / 0.17f;
            var to = c.Cue.Missed ? c.Aim + Vector3.UnitX * (i - 1) * h * 0.2f : c.To + new Vector3(Fx.Signed(c.Seed, i, 184) * 0.3f, Fx.Signed(c.Seed, i, 185) * 0.25f, 0.1f) * h;
            Needle(fx, from, to, k, h, green, 0.06f);
            if (!c.Cue.Missed && i < 2) Fx.Contact(fx, c.Age, c.Seed + i, to, h, land, 0.15f, Rgb(214, 244, 150), White);
            if (!c.Cue.Missed && c.Age > land && c.Age < land + 0.7f)
            {
                float stuck = Fx.Envelope(c.Age, land, land + 0.02f, land + 0.45f, land + 0.7f);
                fx.Streak(FxShape.Streak, to, new Vector3(0.6f, 0.4f, 0f), h * 0.04f, 2.6f, Fx.Fade(green, stuck), FxBlend.Alpha);
            }
        }
        if (c.Cue.Landed) fx.Shake = Math.Max(fx.Shake, 0.2f * Fx.Envelope(c.Age, I - 0.1f, I, I + 0.05f, I + 0.18f));
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Roost: white feathers float down onto the user, which lands, and a healing light settles.</summary>
    private static void Roost(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.9f, 1.2f);
        if (env <= 0f) return;
        for (int i = 0; i < 9; i++)
        {
            float start = Fx.Rand(c.Seed, i, 186) * 0.4f;
            float k = (c.Age - start) / 0.7f;
            if (k < 0f || k > 1f) continue;
            var p = c.FromFeet + new Vector3(Fx.Signed(c.Seed, i, 187) * h * 0.55f + MathF.Sin(k * 7f + i) * h * 0.18f, h * (1.5f - 1.2f * k), 0.1f);
            fx.Sprite(FxShape.Feather, p, h * 0.12f, Fx.Fade(Rgb(250, 250, 255), MathF.Sin(k * MathF.PI)), FxBlend.Alpha, MathF.Sin(k * 6f + i) * 0.8f);
        }
        fx.Sprite(FxShape.Glow, c.From, h * 0.95f, Fx.Fade(Rgb(230, 240, 255), env * 0.25f), FxBlend.Alpha);
        HealOn(fx, c.Age, c.Seed, c.FromFeet, h, 0.2f);
    }

    /// <summary>Scary Face: a hideous face snaps up over the target, two red eyes and a row of fangs, flickering as it looms.</summary>
    private static void ScaryFace(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        if (c.Cue.Missed) return;
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0f, 0.14f, 0.8f, 1.1f);
        if (env <= 0f) return;
        float flick = 0.7f + 0.3f * MathF.Sin(c.Age * 38f);
        float pop = 0.5f + 0.5f * Fx.EaseOut(Math.Min(1f, c.Age / 0.2f));
        var face = c.To + new Vector3(0f, 0.1f * h, 0.15f * h);
        fx.Sprite(FxShape.Glow, face, h * 1.5f * pop, Fx.Fade(Rgb(60, 6, 14), env * 0.6f), FxBlend.Alpha);
        foreach (float side in new[] { -1f, 1f })
        {
            var eye = face + new Vector3(side * h * 0.35f * pop, h * 0.3f * pop, 0f);
            fx.Sprite(FxShape.Eye, eye, h * 0.3f * pop, Fx.Fade(Rgb(255, 70, 60), env * flick), FxBlend.Alpha, side * 0.35f);
            fx.Sprite(FxShape.Glow, eye, h * 0.3f * pop, Fx.Fade(Rgb(255, 60, 40), env * 0.5f * flick));
        }
        for (int i = 0; i < 5; i++)
        {
            float x = (i - 2) * h * 0.2f * pop;
            fx.Sprite(FxShape.Fang, face + new Vector3(x, -h * 0.25f * pop, 0f), h * 0.13f * pop, Fx.Fade(White, env), FxBlend.Alpha, MathF.PI);
            fx.Sprite(FxShape.Fang, face + new Vector3(x * 0.9f + 0.05f * h, -h * 0.55f * pop, 0f), h * 0.12f * pop, Fx.Fade(White, env), FxBlend.Alpha);
        }
        fx.Shake = Math.Max(fx.Shake, 0.16f * Fx.Envelope(c.Age, 0.08f, 0.12f, 0.3f, 0.45f));
    }

    /// <summary>Shock Wave: sparks gather at the user and a crooked bolt jumps straight across, crackling over the target.</summary>
    private static void ShockWave(in FxCtx c, FxColors col, FxList fx)
    {
        var elec = ColorsOf(PokemonType.Electric);
        Gather(fx, c.Age, c.Seed, c.From, c.FromSize, 0f, 0.14f, 10, elec.Main, White, 0.08f, 0.9f);
        Fx.Bolt(fx, c.Age, c.Seed, c.From, c.Aim, 0.12f, 0.55f, c.FromSize * 0.09f, elec.Main, White, 3);
        Fx.Bolt(fx, c.Age, c.Seed + 7, c.From, c.Aim, 0.2f, 0.45f, c.FromSize * 0.05f, elec.Light, White, 1);
        if (c.Cue.Landed)
        {
            float env = Fx.Envelope(c.Age, I, I + 0.04f, 0.65f, 0.95f);
            if (env > 0f)
            {
                fx.Sprite(FxShape.Glow, c.To, c.ToSize * 1.1f, Fx.Fade(elec.Main, env * 0.4f), FxBlend.Alpha);
                for (int i = 0; i < 6; i++)
                {
                    float u = (c.Age * 7f + Fx.Rand(c.Seed, i, 188)) % 1f;
                    fx.Sprite(FxShape.Spark, c.To + Fx.Direction(c.Seed, 190 + i) * c.ToSize * 0.55f, c.ToSize * 0.1f * (1f - u * 0.5f), Fx.Fade(elec.Light, env * (1f - u)), rotation: u * 6f);
                }
            }
            fx.Shake = Math.Max(fx.Shake, 0.35f * Fx.Envelope(c.Age, I, I + 0.03f, I + 0.2f, I + 0.45f));
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Slash: three quick bright cuts, the last the longest, and a flare of light at the end of each.</summary>
    private static void SlashY8(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        if (!c.Cue.Missed)
        {
            var steel = Rgb(255, 244, 214);
            foreach (var (t0, tilt, wide) in new[] { (I - 0.14f, 0.7f, 0.8f), (I - 0.07f, 0.7f, 1.0f), (I - 0.01f, -0.5f, 1.3f) })
            {
                ClawMarks(c, fx, t0, wide, Fx.Fade(steel, 0.9f), White, tilt);
                float k = (c.Age - t0 - 0.07f) / 0.2f;
                if (k > 0f && k < 1f)
                {
                    var end = c.To + new Vector3(MathF.Sin(tilt), -MathF.Cos(tilt), 0f) * h * 0.5f * wide;
                    fx.Sprite(FxShape.Star, end, h * 0.14f * wide * MathF.Sin(k * MathF.PI), Fx.Fade(White, 1f - k), rotation: k * 2f);
                }
            }
            fx.Shake = Math.Max(fx.Shake, 0.28f * Fx.Envelope(c.Age, I - 0.07f, I - 0.04f, I + 0.1f, I + 0.3f));
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Swagger: the user puffs up in a ring of stars and the target is stung red, an anger mark popping twice.</summary>
    private static void Swagger(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var gold = Rgb(255, 214, 100);
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.4f, 0.6f);
        for (int i = 0; i < 6 && env > 0f; i++)
        {
            float a = i * MathF.Tau / 6f + c.Age * 4f;
            fx.Sprite(FxShape.Star, c.From + new Vector3(MathF.Cos(a) * h * 0.6f, MathF.Sin(a) * h * 0.3f + h * 0.2f, 0.1f), h * 0.09f, Fx.Fade(gold, env), FxBlend.Alpha, a);
        }
        Fx.Shockwave(fx, c.Age, c.From, Vector3.Zero, 0.05f, 0.4f, h * 1.1f, Fx.Fade(gold, 0.8f));
        if (c.Cue.Missed) return;
        float ht = c.ToSize;
        var head = c.ToFeet + new Vector3(0.1f * ht, ht * 1.1f, 0f);
        Anger(fx, c.Age, head, ht, 0.3f, 0.7f);
        Anger(fx, c.Age, head + new Vector3(0.18f * ht, 0.1f * ht, 0f), ht * 0.8f, 0.45f, 0.95f);
        Tint(c, fx, 0.3f, 1.0f, Rgb(255, 60, 40), 0.5f);
    }

    /// <summary>Thunder Shock: a small hard crack of electricity bursts on the target, forks of it leaping off in every way.</summary>
    private static void ThunderShock(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        if (c.Cue.Missed) return;
        var elec = ColorsOf(PokemonType.Electric);
        float h = c.ToSize;
        for (int i = 0; i < 6; i++)
        {
            var dir = Fx.Direction(c.Seed, 200 + i);
            Fx.Bolt(fx, c.Age, c.Seed + i, c.To, c.To + dir * h * 0.75f, I - 0.03f, I + 0.35f, h * 0.05f, elec.Main, White, 1);
        }
        float env = Fx.Envelope(c.Age, I - 0.04f, I, I + 0.12f, I + 0.45f);
        if (env > 0f)
        {
            fx.Sprite(FxShape.Spark, c.To, h * 0.6f * env, Fx.Fade(White, env), rotation: c.Age * 5f);
            fx.Sprite(FxShape.Glow, c.To, h * 1.0f, Fx.Fade(elec.Main, env * 0.5f));
        }
        Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.3f, h * 1.1f, Fx.Fade(elec.Light, 0.9f));
        fx.Shake = Math.Max(fx.Shake, 0.3f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.25f));
        Impact(c, col, fx, 0.8f);
    }

    /// <summary>Toxic Spikes: five black-purple spikes are lobbed one after another to the foe's side and stand in the ground, a poison puff at each.</summary>
    private static void ToxicSpikes(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var poison = ColorsOf(PokemonType.Poison);
        float h = c.FromSize;
        var from = c.From + Vector3.UnitY * h * 0.1f;
        for (int i = 0; i < 5; i++)
        {
            float land = 0.17f + i * 0.045f;
            if (i == 4) land = I;
            var spot = c.Foe - Vector3.UnitY * c.FoeSize * 0.5f + new Vector3((i - 2) * 0.85f, 0.06f, (i % 2 == 0 ? 0.55f : -0.4f));
            float k = (c.Age - (land - 0.17f)) / 0.17f;
            Needle(fx, from, spot + Vector3.UnitY * 0.1f, k, h, poison.Main, 0.06f, 2.2f);
            float stand = Fx.Envelope(c.Age, land, land + 0.03f, 1.0f, 1.25f);
            if (stand <= 0f) continue;
            fx.Streak(FxShape.Streak, spot + Vector3.UnitY * 0.4f, Vector3.UnitY, 0.28f, 2.2f, Fx.Fade(Rgb(70, 36, 96), stand), FxBlend.Alpha);
            fx.Streak(FxShape.Streak, spot + Vector3.UnitY * 0.4f, Vector3.UnitY, 0.12f, 2.2f, Fx.Fade(poison.Light, stand), FxBlend.Alpha);
            fx.Flat(FxShape.Dot, spot, Vector3.UnitY, 0.32f, Fx.Fade(poison.Dark, stand * 0.9f), FxBlend.Alpha);
            Fx.Burst(fx, c.Seed, c.Age, spot + Vector3.UnitY * 0.1f, 0.8f, land, 0.4f, 3, FxShape.Bubble, poison.Light, poison.Main, 0.08f, 0.4f, -0.4f, FxBlend.Alpha, salt: 96 + i);
        }
    }

    /// <summary>Wake-Up Slap: four quick slaps from alternate sides, stars at each, and the last wakes it with a bang, Z's flying off.</summary>
    private static void WakeUpSlap(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        if (!c.Cue.Missed)
        {
            for (int i = 0; i < 4; i++)
            {
                float at = I - (3 - i) * 0.07f;
                float side = i % 2 == 0 ? -1f : 1f;
                Strike(c, fx, FxShape.Palm, new Vector3(side * 1.0f, 0.15f, 0.2f), at, 0.3f, Rgb(255, 232, 206), side * -0.5f, 0.06f);
                if (i < 3) Fx.Contact(fx, c.Age, c.Seed + i, c.To + new Vector3(side * -0.15f, 0.1f * i, 0.1f) * h, h, at, 0.16f, Rgb(255, 214, 150), White);
            }
            float env = Fx.Envelope(c.Age, I, I + 0.04f, I + 0.45f, I + 0.7f);
            if (env > 0f)
            {
                fx.Sprite(FxShape.Exclaim, c.ToFeet + new Vector3(0f, h * 1.15f, 0.1f), h * 0.22f * Fx.EaseOut(Math.Min(1f, (c.Age - I) / 0.06f)), Fx.Fade(Rgb(255, 214, 90), env), FxBlend.Alpha);
                for (int i = 0; i < 3; i++)
                {
                    float u = (c.Age - I) / 0.6f;
                    fx.Sprite(FxShape.Zzz, c.ToFeet + new Vector3((0.3f + 0.35f * u) * h * (i - 1) * 0.9f, h * (1.0f + 0.8f * u + 0.1f * i), 0f), h * 0.1f, Fx.Fade(Rgb(190, 214, 255), env * (1f - u)), FxBlend.Alpha, (i - 1) * 0.5f);
                }
            }
            fx.Shake = Math.Max(fx.Shake, 0.35f * Fx.Envelope(c.Age, I - 0.2f, I - 0.15f, I + 0.1f, I + 0.3f));
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Cosmic Power: a night sky closes round the user, stars drifting up through it and three small worlds in orbit.</summary>
    private static void CosmicPower(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.2f, 0.9f, 1.25f);
        if (env <= 0f) return;
        fx.Sprite(FxShape.Glow, c.From, h * 1.5f, Fx.Fade(Rgb(14, 18, 60), env * 0.6f), FxBlend.Alpha);
        fx.Sprite(FxShape.Glow, c.From, h * 1.0f, Fx.Fade(Rgb(40, 30, 110), env * 0.4f), FxBlend.Alpha);
        for (int i = 0; i < 14; i++)
        {
            float u = (c.Age * 0.6f + Fx.Rand(c.Seed, i, 189)) % 1f;
            var p = c.FromFeet + new Vector3(Fx.Signed(c.Seed, i, 190) * h * 0.7f, h * (-0.1f + 1.4f * u), Fx.Signed(c.Seed, i, 191) * h * 0.2f);
            float twinkle = 0.5f + 0.5f * MathF.Sin(c.Age * 9f + i * 2.3f);
            fx.Sprite(i % 4 == 0 ? FxShape.Star : FxShape.Spark, p, h * 0.06f, Fx.Fade(Rgb(230, 236, 255), env * twinkle * MathF.Sin(u * MathF.PI)), rotation: u * 3f);
        }
        var worlds = new[] { Rgb(255, 170, 100), Rgb(120, 200, 255), Rgb(200, 150, 255) };
        for (int i = 0; i < 3; i++)
        {
            float a = i * MathF.Tau / 3f + c.Age * (2f + i * 0.5f);
            var p = c.From + new Vector3(MathF.Cos(a) * h * (0.6f + 0.1f * i), MathF.Sin(a) * h * 0.18f + h * 0.1f * (i - 1), MathF.Sin(a) * h * 0.2f);
            fx.Sprite(FxShape.Orb, p, h * 0.075f, Fx.Fade(worlds[i], env), FxBlend.Alpha, depth: MathF.Sin(a) < 0f);
        }
        Fx.Shockwave(fx, c.Age, c.From, Vector3.Zero, 0.3f, 0.5f, h * 1.2f, Fx.Fade(Rgb(180, 190, 255), 0.7f));
    }

    /// <summary>Lick: a long tongue sweeps up across the target from below and leaves it slimed, drops flying.</summary>
    private static void Lick(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var tongue = Rgb(232, 106, 150);
        var ghost = ColorsOf(PokemonType.Ghost);
        float k = (c.Age - 0.04f) / (I - 0.02f);
        if (k > 0f && k < 1.7f && !c.Cue.Missed)
        {
            float head = Math.Min(1f, k);
            float fade = k < 1f ? 1f : Math.Max(0f, 1f - (k - 1f) * 1.4f);
            var pts = new Vector3[9];
            for (int i = 0; i < pts.Length; i++)
            {
                float u = Math.Max(0f, head - (pts.Length - 1 - i) * 0.07f);
                pts[i] = c.To + new Vector3((-0.7f + 1.4f * u) * h, (-0.55f + 1.15f * u) * h + MathF.Sin(u * 5f) * h * 0.1f, 0.15f * h);
            }
            fx.Ribbon(pts, h * 0.3f, Fx.Fade(tongue, 0.95f * fade), FxBlend.Alpha);
            fx.Ribbon(pts, h * 0.1f, Fx.Fade(Rgb(255, 190, 210), 0.9f * fade), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 6, FxShape.Drop, Rgb(190, 230, 170), Rgb(150, 210, 150), 0.08f, 0.8f, 1.0f, FxBlend.Alpha, salt: 97);
            Tint(c, fx, I, 0.95f, ghost.Main, 0.45f);
            fx.Shake = Math.Max(fx.Shake, 0.2f * Fx.Envelope(c.Age, I, I + 0.03f, I + 0.1f, I + 0.3f));
        }
        Impact(c, col, fx, 0.8f);
    }
}
