using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>A type's effect colours: the main one, a light one for cores and sparks, a dark one for matter in shade.</summary>
internal readonly record struct FxColors(Color Main, Color Light, Color Dark);

/// <summary>Draws one effect at its cue's age.</summary>
internal delegate void FxRecipe(in FxCtx c, FxColors col, FxList fx);

/// <summary>
/// What every move looks like (plan 04 · G8): a template per type and category, and moves with an effect of their
/// own. Physical moves land with a flash of contact and a burst of their type; special moves send something across
/// (flames, water, a bolt, leaves, a beam, an orb) that bursts on the target; status moves wrap the user in an aura
/// or send waves at the foe. The impact comes 0.35 s in, with the damage. Stat changes, heals and status conditions
/// have effects too. GPU-free: recipes only fill an <see cref="FxList"/>.
/// </summary>
internal static class MoveFx
{
    private const float I = BattleAnimator.ImpactTime;

    private static Color Rgb(int r, int g, int b, int a = 255) => new(r, g, b, a);

    private static readonly Color White = Rgb(255, 255, 255);

    public static FxColors ColorsOf(PokemonType type) => type switch
    {
        PokemonType.Fire => new(Rgb(255, 128, 36), Rgb(255, 226, 120), Rgb(200, 60, 20)),
        PokemonType.Water => new(Rgb(64, 146, 255), Rgb(190, 232, 255), Rgb(30, 90, 190)),
        PokemonType.Electric => new(Rgb(255, 222, 50), Rgb(255, 255, 210), Rgb(220, 160, 20)),
        PokemonType.Grass => new(Rgb(96, 196, 72), Rgb(206, 244, 130), Rgb(52, 130, 50)),
        PokemonType.Ice => new(Rgb(140, 218, 255), Rgb(236, 252, 255), Rgb(80, 150, 210)),
        PokemonType.Fighting => new(Rgb(236, 110, 50), Rgb(255, 214, 140), Rgb(170, 50, 30)),
        PokemonType.Poison => new(Rgb(172, 78, 210), Rgb(232, 170, 250), Rgb(100, 40, 130)),
        PokemonType.Ground => new(Rgb(206, 164, 92), Rgb(240, 214, 150), Rgb(130, 96, 56)),
        PokemonType.Flying => new(Rgb(176, 206, 255), Rgb(246, 250, 255), Rgb(120, 150, 210)),
        PokemonType.Psychic => new(Rgb(255, 92, 176), Rgb(255, 196, 228), Rgb(190, 40, 120)),
        PokemonType.Bug => new(Rgb(168, 204, 40), Rgb(232, 250, 140), Rgb(100, 140, 20)),
        PokemonType.Rock => new(Rgb(184, 158, 110), Rgb(226, 210, 170), Rgb(110, 92, 64)),
        PokemonType.Ghost => new(Rgb(132, 92, 210), Rgb(206, 180, 255), Rgb(60, 36, 110)),
        PokemonType.Dragon => new(Rgb(112, 96, 250), Rgb(196, 176, 255), Rgb(60, 40, 170)),
        PokemonType.Dark => new(Rgb(110, 80, 120), Rgb(200, 170, 210), Rgb(40, 28, 46)),
        PokemonType.Steel => new(Rgb(196, 208, 228), Rgb(255, 255, 255), Rgb(120, 130, 150)),
        PokemonType.Fairy => new(Rgb(255, 150, 214), Rgb(255, 224, 242), Rgb(210, 90, 160)),
        _ => new(Rgb(250, 246, 226), Rgb(255, 255, 255), Rgb(180, 172, 150))
    };

    /// <summary>Starts every cue that is playing.</summary>
    public static void Emit(BattleAnimator anim, FxPlaces places, FxList fx)
    {
        foreach (var cue in anim.Cues) Play(cue, places, fx);
    }

    public static void Play(EffectCue cue, FxPlaces places, FxList fx)
    {
        var c = new FxCtx(cue, places);
        switch (cue.Kind)
        {
            case CueKind.Move:
                RecipeFor(cue.Move, cue.Type, cue.Category, cue.OnSelf)(c, ColorsOf(cue.Type), fx);
                break;
            case CueKind.StatUp:
                StatChange(c, fx, up: true);
                break;
            case CueKind.StatDown:
                StatChange(c, fx, up: false);
                break;
            case CueKind.Heal:
                Heal(c, fx);
                break;
            case CueKind.Status:
                Status(c, fx);
                break;
        }
    }

    /// <summary>Moves with an effect of their own rather than their type's template.</summary>
    public static bool HasOwnEffect(string move) => Overrides.ContainsKey(move);

    public static IEnumerable<string> MovesWithOwnEffects => Overrides.Keys;

    public static FxRecipe RecipeFor(string move, PokemonType type, MoveCategory category, bool onSelf) =>
        Overrides.TryGetValue(move, out var own) ? own : Template(type, category, onSelf);

    /// <summary>The template every move of a type and category plays unless it has its own.</summary>
    public static FxRecipe Template(PokemonType type, MoveCategory category, bool onSelf) => category switch
    {
        MoveCategory.Status => onSelf ? SelfAura : StatusWaves,
        MoveCategory.Physical => Physical,
        _ => type switch
        {
            PokemonType.Fire => Flames,
            PokemonType.Water => WaterStream,
            PokemonType.Electric => ElectricBolt,
            PokemonType.Grass => LeafStream,
            PokemonType.Ice => IceBeam,
            PokemonType.Fighting => PowerOrb,
            PokemonType.Poison => SludgeArc,
            PokemonType.Ground => Eruption,
            PokemonType.Flying => WindStream,
            PokemonType.Psychic => PsychicWaves,
            PokemonType.Bug => Swarm,
            PokemonType.Rock => RockArc,
            PokemonType.Ghost => ShadowOrb,
            PokemonType.Dragon => EnergyBeam,
            PokemonType.Dark => DarkWaves,
            PokemonType.Steel => EnergyBeam,
            PokemonType.Fairy => Sparkles,
            _ => PowerOrb
        }
    };

    // ------------------------------------------------------------------ the landing

    /// <summary>
    /// Where a damaging move lands: a flash of contact and a burst of its type, bigger for a critical or
    /// super-effective hit (a critical one also flashes the screen); a puff of smoke when it does nothing; nothing
    /// when it misses.
    /// </summary>
    private static void Impact(in FxCtx c, FxColors col, FxList fx, float scale = 1f, float at = I)
    {
        if (c.Cue.Missed) return;
        float h = c.ToSize;
        if (c.Cue.Blocked)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, at, 0.6f, 6, FxShape.Smoke, Rgb(210, 206, 200, 200), Rgb(170, 166, 160, 200), 0.18f, 0.35f, -0.4f, FxBlend.Alpha);
            return;
        }
        float big = scale * (c.Cue.Critical ? 1.35f : 1f) * (c.Cue.SuperEffective ? 1.2f : 1f);
        Fx.Contact(fx, c.Age, c.Seed, c.To, h, at, 0.34f * big, col.Main, col.Light);
        Flavor(c, col, fx, at, big);
        if (c.Cue.Critical)
        {
            fx.ScreenFlash(White, 0.24f * Fx.Envelope(c.Age, at - 0.01f, at + 0.03f, at + 0.06f, at + 0.24f));
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, at, 0.35f, h * 1.3f, Fx.Fade(White, 0.9f));
        }
        if (c.Cue.SuperEffective) Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, at + 0.04f, 0.4f, h * 1.1f, Fx.Fade(col.Light, 0.8f));
    }

    /// <summary>The burst of a type where a move lands.</summary>
    private static void Flavor(in FxCtx c, FxColors col, FxList fx, float at, float big)
    {
        float h = c.ToSize;
        int n(int count) => (int)MathF.Round(count * big);
        var to = c.To;
        int s = c.Seed;
        float age = c.Age;
        switch (c.Cue.Type)
        {
            case PokemonType.Fire:
                Fx.Burst(fx, s, age, to, h, at, 0.6f, n(14), FxShape.Flame, col.Main, col.Dark, 0.17f, 0.75f, -0.9f);
                Fx.Burst(fx, s, age, to, h, at, 0.5f, n(10), FxShape.Ember, col.Light, col.Main, 0.08f, 1.1f, -0.6f, salt: 3);
                break;
            case PokemonType.Water:
                Fx.Burst(fx, s, age, to, h, at, 0.7f, n(16), FxShape.Drop, col.Main, col.Light, 0.09f, 1.0f, 2.4f, FxBlend.Alpha);
                Fx.Shockwave(fx, age, to, Vector3.Zero, at, 0.4f, h * 0.8f, Fx.Fade(col.Light, 0.8f));
                break;
            case PokemonType.Electric:
                Fx.Burst(fx, s, age, to, h, at, 0.45f, n(14), FxShape.Spark, col.Main, col.Light, 0.12f, 1.0f, 0f, streak: 0f);
                for (int k = 0; k < 3; k++)
                {
                    var end = to + Fx.Direction(s, 90 + k) * h * 0.75f;
                    Fx.Bolt(fx, age, s + k * 17, to, end, at, at + 0.3f, h * 0.035f, col.Main, col.Light, 0);
                }
                break;
            case PokemonType.Grass:
                Fx.Burst(fx, s, age, to, h, at, 0.8f, n(12), FxShape.Leaf, col.Main, col.Light, 0.11f, 0.95f, 0.7f, FxBlend.Alpha, spin: 7f);
                break;
            case PokemonType.Ice:
                Fx.Burst(fx, s, age, to, h, at, 0.7f, n(12), FxShape.Shard, col.Main, col.Light, 0.12f, 1.0f, 1.3f, FxBlend.Alpha, spin: 3f);
                Fx.Burst(fx, s, age, to, h, at, 0.5f, n(8), FxShape.Spark, col.Light, White, 0.08f, 0.8f, salt: 2);
                break;
            case PokemonType.Fighting:
                Fx.Shockwave(fx, age, to, Vector3.Zero, at, 0.3f, h * 0.9f, col.Light);
                Fx.Shockwave(fx, age, to, Vector3.Zero, at + 0.08f, 0.3f, h * 0.7f, col.Main);
                Fx.Burst(fx, s, age, to, h, at, 0.6f, n(8), FxShape.Star, col.Light, col.Main, 0.08f, 0.9f, 1f, FxBlend.Alpha, spin: 5f);
                break;
            case PokemonType.Poison:
                Fx.Burst(fx, s, age, to, h, at, 0.8f, n(12), FxShape.Bubble, col.Main, col.Light, 0.09f, 0.65f, -0.7f, FxBlend.Alpha);
                break;
            case PokemonType.Ground:
                Fx.Burst(fx, s, age, c.ToFeet + Vector3.UnitY * h * 0.1f, h, at, 0.75f, n(10), FxShape.Rock, col.Main, col.Dark, 0.1f, 0.9f, 2.4f, FxBlend.Alpha, spin: 4f);
                Fx.Burst(fx, s, age, c.ToFeet + Vector3.UnitY * h * 0.15f, h, at, 0.9f, n(6), FxShape.Smoke, Fx.Fade(col.Light, 0.8f), Fx.Fade(col.Main, 0.7f), 0.24f, 0.5f, -0.3f, FxBlend.Alpha, salt: 4);
                break;
            case PokemonType.Flying:
                Fx.Burst(fx, s, age, to, h, at, 0.9f, n(9), FxShape.Feather, White, col.Main, 0.11f, 0.85f, 0.4f, FxBlend.Alpha, spin: 3f);
                Fx.Burst(fx, s, age, to, h, at, 0.35f, n(8), FxShape.Streak, col.Light, White, 0.06f, 1.1f, streak: 3f, salt: 5);
                break;
            case PokemonType.Psychic:
                Fx.Shockwave(fx, age, to, Vector3.Zero, at, 0.45f, h * 1.0f, col.Main);
                Fx.Shockwave(fx, age, to, Vector3.Zero, at + 0.1f, 0.45f, h * 0.75f, col.Light);
                Fx.Burst(fx, s, age, to, h, at, 0.5f, n(8), FxShape.Spark, col.Light, col.Main, 0.09f, 0.8f);
                break;
            case PokemonType.Bug:
                Fx.Burst(fx, s, age, to, h, at, 0.6f, n(14), FxShape.Spore, col.Main, col.Light, 0.07f, 0.9f, 0.3f);
                break;
            case PokemonType.Rock:
                Fx.Burst(fx, s, age, to, h, at, 0.75f, n(10), FxShape.Rock, col.Light, col.Dark, 0.12f, 1.0f, 2.6f, FxBlend.Alpha, spin: 4f);
                break;
            case PokemonType.Ghost:
                Fx.Burst(fx, s, age, to, h, at, 0.8f, n(10), FxShape.Wisp, col.Main, col.Light, 0.15f, 0.7f, -0.6f);
                break;
            case PokemonType.Dragon:
                Fx.Burst(fx, s, age, to, h, at, 0.6f, n(14), FxShape.Flame, col.Main, col.Light, 0.15f, 0.8f, -0.7f);
                break;
            case PokemonType.Dark:
                Fx.Burst(fx, s, age, to, h, at, 0.8f, n(8), FxShape.Smoke, Rgb(70, 50, 80, 220), Rgb(40, 28, 46, 220), 0.2f, 0.55f, -0.2f, FxBlend.Alpha);
                Fx.Burst(fx, s, age, to, h, at, 0.4f, n(6), FxShape.Spark, col.Light, col.Main, 0.08f, 0.8f, salt: 6);
                break;
            case PokemonType.Steel:
                Fx.Burst(fx, s, age, to, h, at, 0.45f, n(14), FxShape.Streak, col.Light, White, 0.05f, 1.3f, 0.6f, streak: 2.5f);
                Fx.Burst(fx, s, age, to, h, at, 0.4f, n(5), FxShape.Spark, White, col.Main, 0.12f, 0.6f, salt: 7);
                break;
            case PokemonType.Fairy:
                Fx.Burst(fx, s, age, to, h, at, 0.8f, n(10), FxShape.Petal, col.Main, col.Light, 0.09f, 0.85f, 0.4f, FxBlend.Alpha, spin: 4f);
                Fx.Burst(fx, s, age, to, h, at, 0.6f, n(8), FxShape.Spark, col.Light, White, 0.08f, 0.9f, salt: 8);
                break;
            default:
                Fx.Burst(fx, s, age, to, h, at, 0.6f, n(8), FxShape.Star, Rgb(255, 240, 160), White, 0.08f, 0.9f, 0.8f, FxBlend.Alpha, spin: 5f);
                Fx.Burst(fx, s, age, to, h, at, 0.4f, n(8), FxShape.Spark, White, Rgb(255, 250, 220), 0.08f, 0.9f, salt: 9);
                break;
        }
    }

    // ------------------------------------------------------------------ templates

    /// <summary>A physical move: speed lines as the user lunges, then the blow lands.</summary>
    private static void Physical(in FxCtx c, FxColors col, FxList fx)
    {
        SpeedLines(c, fx, 0.1f, 0.34f, 5, 0.5f);
        Impact(c, col, fx);
    }

    /// <summary>Streaks round the user along the way it lunges.</summary>
    private static void SpeedLines(in FxCtx c, FxList fx, float t0, float t1, int count, float strength)
    {
        var dir = c.To - c.From;
        var (across, up) = Fx.Frame(dir);
        for (int i = 0; i < count; i++)
        {
            float k = (c.Age - t0 - (t1 - t0) * 0.5f * Fx.Rand(c.Seed, i, 31)) / ((t1 - t0) * 0.5f);
            if (k < 0f || k > 1f) continue;
            var p = c.From + (across * Fx.Signed(c.Seed, i, 32) * 0.6f + up * Fx.Signed(c.Seed, i, 33) * 0.5f) * c.FromSize
                + Vector3.Normalize(dir) * c.FromSize * (k * 0.8f - 0.3f);
            fx.Streak(FxShape.Streak, p, dir, c.FromSize * 0.05f, 9f, Fx.Fade(White, strength * MathF.Sin(k * MathF.PI)));
        }
    }

    /// <summary>A status move on itself: an aura rises round it and a glint pops.</summary>
    private static void SelfAura(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.Aura(fx, c.Age, c.Seed, c.FromFeet, c.FromSize, 0f, 0.95f, col.Main, col.Light);
        Fx.Glint(fx, c.Age, c.FromFeet + Vector3.UnitY * c.FromSize * 0.85f, c.FromSize, 0.55f, col.Light);
    }

    /// <summary>A status move on a foe: waves of its type roll over to the target.</summary>
    private static void StatusWaves(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.Waves(fx, c, 0.05f, 0.75f, 3, FxShape.Ring, Fx.Fade(col.Main, 0.9f), 0.45f);
        Fx.Glint(fx, c.Age, c.To, c.ToSize, 0.55f, col.Light, 0.35f);
    }

    private static void Flames(in FxCtx c, FxColors col, FxList fx) => FlameStream(c, col, fx, 22, 0.18f, 0.5f);

    private static void FlameStream(in FxCtx c, FxColors col, FxList fx, int count, float size, float t1)
    {
        Fx.Stream(fx, c, 0f, t1, 0.3f, count, FxShape.Flame, col.Main, col.Dark, size, arc: 0.03f, spread: 0.3f);
        Fx.Stream(fx, c, 0.02f, t1, 0.3f, count / 2, FxShape.Ember, col.Light, col.Main, size * 0.55f, arc: 0.03f, spread: 0.18f);
        Impact(c, col, fx);
    }

    private static void WaterStream(in FxCtx c, FxColors col, FxList fx) => Water(c, col, fx, 24, 0.09f);

    private static void Water(in FxCtx c, FxColors col, FxList fx, int count, float size)
    {
        Fx.Stream(fx, c, 0f, 0.5f, 0.28f, count, FxShape.Drop, col.Main, col.Light, size, arc: 0.06f, spread: 0.18f, blend: FxBlend.Alpha, trail: 1.6f);
        Fx.Stream(fx, c, 0.01f, 0.5f, 0.28f, count / 2, FxShape.Glow, col.Light, White, size * 0.7f, arc: 0.06f, spread: 0.1f);
        Impact(c, col, fx);
    }

    private static void ElectricBolt(in FxCtx c, FxColors col, FxList fx) => Bolt(c, col, fx, 0.06f, 2);

    private static void Bolt(in FxCtx c, FxColors col, FxList fx, float width, int branches)
    {
        float w = width * (c.FromSize + c.ToSize) * 0.5f;
        Fx.Bolt(fx, c.Age, c.Seed, c.From, c.Aim, 0.12f, 0.52f, w, col.Main, col.Light, branches);
        fx.Sprite(FxShape.Glow, c.From, c.FromSize * 0.6f, Fx.Fade(col.Main, Fx.Envelope(c.Age, 0.04f, 0.12f, 0.4f, 0.55f)));
        Impact(c, col, fx);
    }

    private static void LeafStream(in FxCtx c, FxColors col, FxList fx) => Leaves(c, col, fx, 10, 9f, false);

    private static void Leaves(in FxCtx c, FxColors col, FxList fx, int count, float spin, bool glow)
    {
        Fx.Stream(fx, c, 0f, 0.5f, 0.32f, count, FxShape.Leaf, col.Main, col.Light, 0.12f, arc: 0.05f, spread: 0.5f, spin: spin, blend: FxBlend.Alpha);
        if (glow) Fx.Stream(fx, c, 0f, 0.5f, 0.32f, count, FxShape.Glow, col.Light, White, 0.12f, arc: 0.05f, spread: 0.5f);
        Impact(c, col, fx);
    }

    private static void IceBeam(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.Beam(fx, c, 0.08f, 0.6f, 0.13f, col.Main, White);
        Fx.Stream(fx, c, 0.08f, 0.6f, 0.25f, 10, FxShape.Shard, col.Light, col.Main, 0.08f, spread: 0.25f, spin: 6f, blend: FxBlend.Alpha);
        Impact(c, col, fx);
    }

    /// <summary>A ball of power: Fighting's template, and the generic one.</summary>
    private static void PowerOrb(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.Orb(fx, c, 0.02f, I, 0.24f, col.Main, col.Light);
        Impact(c, col, fx, 1.1f);
    }

    private static void SludgeArc(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.Stream(fx, c, 0f, 0.4f, 0.32f, 7, FxShape.Orb, col.Main, col.Dark, 0.15f, arc: 0.16f, spread: 0.3f, blend: FxBlend.Alpha);
        Impact(c, col, fx);
    }

    /// <summary>Ground: the earth heaves under the target and throws up rocks and dust.</summary>
    private static void Eruption(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Fx.Shockwave(fx, c.Age, c.ToFeet + Vector3.UnitY * 0.02f, Vector3.UnitY, 0.12f, 0.6f, h * 1.6f, Fx.Fade(col.Dark, 0.9f));
        Fx.Burst(fx, c.Seed, c.Age, c.ToFeet, h, 0.15f, 0.8f, 10, FxShape.Rock, col.Main, col.Dark, 0.1f, 1.2f, 2.2f, FxBlend.Alpha, spin: 4f, salt: 12);
        fx.Shake = Math.Max(fx.Shake, 0.35f * Fx.Envelope(c.Age, 0.1f, 0.2f, 0.4f, 0.7f));
        Impact(c, col, fx);
    }

    private static void WindStream(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.Stream(fx, c, 0f, 0.5f, 0.25f, 14, FxShape.Streak, col.Light, White, 0.05f, spread: 0.6f, trail: 4f);
        Whirl(c, col, fx, 0.25f, 0.75f);
        Impact(c, col, fx);
    }

    /// <summary>Wind whirling round the target.</summary>
    private static void Whirl(in FxCtx c, FxColors col, FxList fx, float t0, float t1)
    {
        float env = Fx.Envelope(c.Age, t0, t0 + 0.1f, t1 - 0.15f, t1);
        if (env <= 0f) return;
        for (int i = 0; i < 3; i++)
            fx.Sprite(FxShape.Swirl, c.To + Vector3.UnitY * c.ToSize * (i - 1) * 0.25f, c.ToSize * (0.55f + 0.1f * i), Fx.Fade(col.Light, env * 0.8f),
                rotation: -c.Age * (9f + i * 2f));
    }

    private static void PsychicWaves(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.Waves(fx, c, 0f, 0.45f, 4, FxShape.Ring, col.Main, 0.4f, 0.25f);
        Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, 0.22f, 0.4f, c.ToSize * 0.9f, col.Light);
        Impact(c, col, fx);
    }

    private static void Swarm(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.Stream(fx, c, 0f, 0.5f, 0.3f, 22, FxShape.Spore, col.Main, col.Light, 0.06f, arc: 0.04f, spread: 0.7f);
        Impact(c, col, fx);
    }

    private static void RockArc(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.Stream(fx, c, 0f, 0.4f, 0.32f, 4, FxShape.Rock, col.Light, col.Main, 0.17f, arc: 0.22f, spread: 0.3f, spin: 6f, blend: FxBlend.Alpha);
        Impact(c, col, fx);
    }

    private static void ShadowOrb(in FxCtx c, FxColors col, FxList fx)
    {
        // A ball of shadow: a dark core in a violet glow
        Fx.Orb(fx, c, 0.02f, I, 0.26f, col.Main, Fx.Fade(col.Light, 0.5f), FxShape.Wisp, col.Dark);
        Impact(c, col, fx, 1.1f);
    }

    private static void EnergyBeam(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.Beam(fx, c, 0.06f, 0.58f, 0.15f, col.Main, col.Light);
        Impact(c, col, fx);
    }

    private static void DarkWaves(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.Waves(fx, c, 0f, 0.45f, 3, FxShape.Ring, Rgb(90, 60, 110), 0.5f, 0.25f);
        Fx.Waves(fx, c, 0.04f, 0.49f, 3, FxShape.Ring, Rgb(200, 150, 220, 160), 0.36f, 0.25f);
        Impact(c, col, fx);
    }

    private static void Sparkles(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.Stream(fx, c, 0f, 0.5f, 0.3f, 16, FxShape.Spark, col.Main, col.Light, 0.12f, arc: 0.06f, spread: 0.5f, spin: 4f);
        Impact(c, col, fx);
    }

    // ------------------------------------------------------------------ moves with their own effect

    private static readonly Dictionary<string, FxRecipe> Overrides = new(StringComparer.OrdinalIgnoreCase)
    {
        // Electric
        ["Thunder"] = Thunder,
        ["Thunder Shock"] = (in FxCtx c, FxColors col, FxList fx) => Bolt(c, col, fx, 0.035f, 1),
        ["Thunderbolt"] = Thunderbolt,
        ["Discharge"] = Thunderbolt,
        ["Charge Beam"] = EnergyBeam,
        ["Thunder Wave"] = ThunderWave,
        ["Spark"] = Sparked,
        ["Volt Tackle"] = Sparked,
        ["Wild Charge"] = Sparked,
        // Fire
        ["Ember"] = (in FxCtx c, FxColors col, FxList fx) => FlameStream(c, col, fx, 9, 0.15f, 0.42f),
        ["Flamethrower"] = (in FxCtx c, FxColors col, FxList fx) => FlameStream(c, col, fx, 36, 0.21f, 0.62f),
        ["Heat Wave"] = (in FxCtx c, FxColors col, FxList fx) => FlameStream(c, col, fx, 30, 0.24f, 0.6f),
        ["Fire Blast"] = FireBlast,
        ["Overheat"] = FireBlast,
        ["Flame Wheel"] = FlameWheel,
        ["Flare Blitz"] = FlameWheel,
        ["Will-O-Wisp"] = WillOWisp,
        ["Fire Spin"] = FireSpin,
        // Water
        ["Water Gun"] = (in FxCtx c, FxColors col, FxList fx) => Water(c, col, fx, 26, 0.09f),
        ["Water Pulse"] = (in FxCtx c, FxColors col, FxList fx) => { Fx.Waves(fx, c, 0f, 0.4f, 3, FxShape.Ring, col.Main, 0.42f, 0.25f); Impact(c, col, fx); },
        ["Bubble"] = (in FxCtx c, FxColors col, FxList fx) => Bubbles(c, col, fx, 9),
        ["Bubble Beam"] = (in FxCtx c, FxColors col, FxList fx) => Bubbles(c, col, fx, 18),
        ["Hydro Pump"] = HydroPump,
        ["Surf"] = Surf,
        ["Muddy Water"] = Surf,
        ["Whirlpool"] = (in FxCtx c, FxColors col, FxList fx) => { Whirl(c, col, fx, 0.15f, 0.9f); Impact(c, col, fx); },
        ["Splash"] = Splash,
        // Grass
        ["Razor Leaf"] = (in FxCtx c, FxColors col, FxList fx) => Leaves(c, col, fx, 8, 10f, false),
        ["Magical Leaf"] = (in FxCtx c, FxColors col, FxList fx) => Leaves(c, col, fx, 10, 8f, true),
        ["Leaf Storm"] = (in FxCtx c, FxColors col, FxList fx) => Leaves(c, col, fx, 28, 12f, false),
        ["Energy Ball"] = PowerOrb,
        ["Seed Bomb"] = RockArc,
        ["Vine Whip"] = (in FxCtx c, FxColors col, FxList fx) => { Fx.Slash(fx, c.Age, c.Seed, c.To, c.ToSize, I - 0.08f, 2, 0.1f, col.Main, col.Light); Impact(c, col, fx); },
        ["Leaf Blade"] = Slashes,
        ["Absorb"] = (in FxCtx c, FxColors col, FxList fx) => Drain(c, col, fx, 10),
        ["Mega Drain"] = (in FxCtx c, FxColors col, FxList fx) => Drain(c, col, fx, 16),
        ["Giga Drain"] = (in FxCtx c, FxColors col, FxList fx) => Drain(c, col, fx, 24),
        ["Drain Punch"] = (in FxCtx c, FxColors col, FxList fx) => Drain(c, col, fx, 14),
        ["Leech Seed"] = (in FxCtx c, FxColors col, FxList fx) => { Fx.Stream(fx, c, 0f, 0.4f, 0.32f, 3, FxShape.Dot, col.Dark, col.Main, 0.06f, arc: 0.2f, spread: 0.2f, blend: FxBlend.Alpha); Impact(c, col, fx, 0.6f); },
        ["Sleep Powder"] = (in FxCtx c, FxColors col, FxList fx) => Powder(c, fx, Rgb(120, 170, 255), Rgb(200, 230, 255)),
        ["Stun Spore"] = (in FxCtx c, FxColors col, FxList fx) => Powder(c, fx, Rgb(255, 220, 60), Rgb(255, 250, 180)),
        ["Poison Powder"] = (in FxCtx c, FxColors col, FxList fx) => Powder(c, fx, Rgb(180, 90, 220), Rgb(230, 180, 250)),
        ["Spore"] = (in FxCtx c, FxColors col, FxList fx) => Powder(c, fx, Rgb(150, 210, 90), Rgb(230, 250, 180)),
        ["Synthesis"] = HealAura,
        ["Growth"] = SelfAura,
        // Ice
        ["Ice Beam"] = IceBeam,
        ["Aurora Beam"] = (in FxCtx c, FxColors col, FxList fx) => { Fx.Beam(fx, c, 0.08f, 0.6f, 0.13f, Rgb(255, 150, 210), Rgb(170, 240, 255)); Impact(c, col, fx); },
        ["Blizzard"] = Blizzard,
        ["Powder Snow"] = (in FxCtx c, FxColors col, FxList fx) => { Fx.Stream(fx, c, 0f, 0.5f, 0.3f, 18, FxShape.Snowflake, White, col.Light, 0.07f, spread: 0.6f, spin: 3f, blend: FxBlend.Alpha); Impact(c, col, fx); },
        ["Hail"] = Blizzard,
        // Psychic
        ["Confusion"] = PsychicWaves,
        ["Psychic"] = PsychicStrong,
        ["Psybeam"] = (in FxCtx c, FxColors col, FxList fx) => { Fx.Beam(fx, c, 0.06f, 0.58f, 0.12f, col.Main, Rgb(255, 230, 120)); Fx.Waves(fx, c, 0.06f, 0.58f, 5, FxShape.Ring, col.Light, 0.3f, 0.2f); Impact(c, col, fx); },
        ["Hypnosis"] = (in FxCtx c, FxColors col, FxList fx) => Fx.Waves(fx, c, 0.05f, 0.85f, 4, FxShape.Swirl, col.Light, 0.45f, 0.35f),
        ["Rest"] = Rest,
        // Ghost and Dark
        ["Shadow Ball"] = ShadowOrb,
        ["Shadow Sneak"] = ShadowSneak,
        ["Ominous Wind"] = (in FxCtx c, FxColors col, FxList fx) => { Fx.Stream(fx, c, 0f, 0.5f, 0.3f, 12, FxShape.Wisp, col.Main, col.Light, 0.12f, spread: 0.6f); Impact(c, col, fx); },
        ["Confuse Ray"] = (in FxCtx c, FxColors col, FxList fx) => Fx.Orb(fx, c, 0.05f, 0.6f, 0.18f, Rgb(255, 230, 90), White),
        ["Dark Pulse"] = DarkWaves,
        ["Night Shade"] = DarkWaves,
        // Fighting
        ["Aura Sphere"] = (in FxCtx c, FxColors col, FxList fx) => { Fx.Orb(fx, c, 0.02f, I, 0.24f, Rgb(80, 160, 255), Rgb(220, 240, 255)); Impact(c, new FxColors(Rgb(80, 160, 255), Rgb(220, 240, 255), Rgb(40, 90, 200)), fx, 1.1f); },
        ["Focus Blast"] = (in FxCtx c, FxColors col, FxList fx) => { Fx.Orb(fx, c, 0.02f, I, 0.32f, col.Main, col.Light); Impact(c, col, fx, 1.4f); },
        ["Close Combat"] = Barrage,
        ["Cross Chop"] = (in FxCtx c, FxColors col, FxList fx) => { CrossMark(c, col, fx); Impact(c, col, fx); },
        ["X-Scissor"] = (in FxCtx c, FxColors col, FxList fx) => { CrossMark(c, col, fx); Impact(c, col, fx); },
        ["Bulk Up"] = SelfAura,
        // Dragon
        ["Dragon Rage"] = Flames,
        ["Dragon Breath"] = Flames,
        ["Dragon Pulse"] = EnergyBeam,
        ["Dragon Claw"] = Claws,
        ["Dragon Dance"] = SelfAura,
        // Normal
        ["Hyper Beam"] = HyperBeam,
        ["Swift"] = (in FxCtx c, FxColors col, FxList fx) => { Fx.Stream(fx, c, 0f, 0.45f, 0.28f, 7, FxShape.Star, Rgb(255, 236, 120), White, 0.12f, arc: 0.05f, spread: 0.5f, spin: 8f, blend: FxBlend.Alpha); Impact(c, col, fx); },
        ["Quick Attack"] = QuickStrike,
        ["Extreme Speed"] = QuickStrike,
        ["Aerial Ace"] = QuickStrike,
        ["Growl"] = (in FxCtx c, FxColors col, FxList fx) => Sound(c, fx, 3, FxShape.Wave, true),
        ["Roar"] = (in FxCtx c, FxColors col, FxList fx) => Sound(c, fx, 5, FxShape.Wave, false),
        ["Screech"] = (in FxCtx c, FxColors col, FxList fx) => Sound(c, fx, 6, FxShape.Wave, false),
        ["Supersonic"] = (in FxCtx c, FxColors col, FxList fx) => Sound(c, fx, 5, FxShape.Ring, false),
        ["Sing"] = Sing,
        ["Leer"] = Leer,
        ["Scary Face"] = Leer,
        ["Tail Whip"] = (in FxCtx c, FxColors col, FxList fx) => { SpeedLines(c, fx, 0.05f, 0.45f, 4, 0.45f); Fx.Glint(fx, c.Age, c.To, c.ToSize, 0.45f, White, 0.3f); },
        ["Charm"] = Hearts,
        ["Sweet Kiss"] = Hearts,
        ["Attract"] = Hearts,
        ["Captivate"] = Hearts,
        ["Swords Dance"] = SwordsDance,
        ["Harden"] = Shine,
        ["Withdraw"] = Shine,
        ["Iron Defense"] = Shine,
        ["Defense Curl"] = Shine,
        ["Barrier"] = Shine,
        ["Protect"] = Shine,
        ["Detect"] = Shine,
        ["Agility"] = (in FxCtx c, FxColors col, FxList fx) => { SpeedLines(c, fx, 0f, 0.9f, 10, 0.6f); Fx.Glint(fx, c.Age, c.From, c.FromSize, 0.6f, White); },
        ["Recover"] = HealAura,
        ["Roost"] = HealAura,
        ["Moonlight"] = HealAura,
        ["Morning Sun"] = HealAura,
        ["Slack Off"] = HealAura,
        ["Milk Drink"] = HealAura,
        ["Bite"] = Chomp,
        ["Crunch"] = Chomp,
        ["Hyper Fang"] = Chomp,
        ["Fire Fang"] = Chomp,
        ["Ice Fang"] = Chomp,
        ["Thunder Fang"] = Chomp,
        ["Scratch"] = Claws,
        ["Fury Swipes"] = Claws,
        ["Metal Claw"] = Claws,
        ["Shadow Claw"] = Claws,
        ["Crush Claw"] = Claws,
        ["Slash"] = Slashes,
        ["Night Slash"] = Slashes,
        ["Psycho Cut"] = Slashes,
        ["Air Slash"] = Slashes,
        // Ground and Rock
        ["Earthquake"] = Earthquake,
        ["Magnitude"] = Earthquake,
        ["Sand Attack"] = (in FxCtx c, FxColors col, FxList fx) => Spray(c, fx, Rgb(232, 208, 150), Rgb(196, 160, 100), false),
        ["Mud-Slap"] = (in FxCtx c, FxColors col, FxList fx) => { Spray(c, fx, Rgb(150, 110, 70), Rgb(110, 80, 50), true); Impact(c, col, fx, 0.8f); },
        ["Mud Shot"] = SludgeArc,
        ["Rock Throw"] = RockArc,
        ["Rock Slide"] = RockSlide,
        ["Stone Edge"] = StoneEdge,
        ["Power Gem"] = (in FxCtx c, FxColors col, FxList fx) => { Fx.Stream(fx, c, 0f, 0.45f, 0.28f, 8, FxShape.Shard, Rgb(255, 120, 150), Rgb(255, 230, 140), 0.1f, spread: 0.4f, spin: 5f); Impact(c, col, fx); },
        ["Ancient Power"] = RockArc,
        // Poison and Bug
        ["Sludge Bomb"] = SludgeArc,
        ["Sludge"] = SludgeArc,
        ["Poison Sting"] = (in FxCtx c, FxColors col, FxList fx) => Needles(c, col, fx, 3),
        ["Pin Missile"] = (in FxCtx c, FxColors col, FxList fx) => Needles(c, col, fx, 5),
        ["Toxic"] = (in FxCtx c, FxColors col, FxList fx) => Powder(c, fx, Rgb(150, 60, 190), Rgb(210, 150, 240)),
        ["Signal Beam"] = (in FxCtx c, FxColors col, FxList fx) => { Fx.Beam(fx, c, 0.06f, 0.58f, 0.12f, Rgb(255, 120, 200), Rgb(200, 255, 140)); Impact(c, col, fx); },
        ["Bug Buzz"] = (in FxCtx c, FxColors col, FxList fx) => { Sound(c, fx, 5, FxShape.Wave, false, col.Main); Impact(c, col, fx); },
        ["String Shot"] = (in FxCtx c, FxColors col, FxList fx) => Fx.Stream(fx, c, 0f, 0.6f, 0.3f, 18, FxShape.Dot, White, Rgb(230, 230, 230), 0.035f, spread: 0.15f, blend: FxBlend.Alpha, trail: 2f),
        // Flying
        ["Gust"] = WindStream,
        ["Twister"] = (in FxCtx c, FxColors col, FxList fx) => { Whirl(c, ColorsOf(PokemonType.Dragon), fx, 0.1f, 0.8f); Impact(c, col, fx); },
        // Steel
        ["Flash Cannon"] = EnergyBeam,
        ["Iron Tail"] = Physical,
        // Weather and field
        ["Rain Dance"] = RainDance,
        ["Sunny Day"] = SunnyDay,
        ["Sandstorm"] = (in FxCtx c, FxColors col, FxList fx) => Spray(c, fx, Rgb(232, 208, 150), Rgb(196, 160, 100), false),
        ["Explosion"] = Explosion,
        ["Self-Destruct"] = Explosion
    };

    private static void Thunder(in FxCtx c, FxColors col, FxList fx)
    {
        // A bolt out of the sky onto the target, with a flash
        var top = c.To + new Vector3(c.ToSize * 0.8f, c.ToSize * 7f, -c.ToSize * 1.5f);
        var bottom = c.Cue.Missed ? c.Aim - Vector3.UnitY * c.ToSize * 0.4f : c.ToFeet;
        Fx.Bolt(fx, c.Age, c.Seed, top, bottom, 0.16f, 0.55f, c.ToSize * 0.09f, col.Main, col.Light, 3);
        fx.ScreenFlash(Rgb(255, 250, 210), 0.45f * Fx.Envelope(c.Age, 0.16f, 0.2f, 0.24f, 0.4f));
        Impact(c, col, fx, 1.3f);
    }

    private static void Thunderbolt(in FxCtx c, FxColors col, FxList fx)
    {
        Bolt(c, col, fx, 0.07f, 3);
        Fx.Bolt(fx, c.Age, c.Seed + 101, c.From, c.Aim, 0.18f, 0.5f, (c.FromSize + c.ToSize) * 0.02f, col.Main, col.Light, 1);
        fx.ScreenFlash(Rgb(255, 250, 210), 0.18f * Fx.Envelope(c.Age, 0.12f, 0.16f, 0.2f, 0.34f));
    }

    private static void ThunderWave(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.Waves(fx, c, 0f, 0.4f, 3, FxShape.Ring, col.Main, 0.35f, 0.25f);
        for (int k = 0; k < 4; k++)
        {
            var a = c.To + Fx.Direction(c.Seed, 70 + k) * c.ToSize * 0.55f;
            var b = c.To + Fx.Direction(c.Seed, 80 + k) * c.ToSize * 0.55f;
            Fx.Bolt(fx, c.Age, c.Seed + k * 13, a, b, 0.32f + k * 0.08f, 0.62f + k * 0.08f, c.ToSize * 0.03f, col.Main, col.Light, 0);
        }
    }

    /// <summary>A charge crackling with electricity: sparks round the user as it lunges, then the blow.</summary>
    private static void Sparked(in FxCtx c, FxColors col, FxList fx)
    {
        for (int k = 0; k < 3; k++)
        {
            var a = c.From + Fx.Direction(c.Seed, 60 + k) * c.FromSize * 0.5f;
            var b = c.From + Fx.Direction(c.Seed, 65 + k) * c.FromSize * 0.5f;
            Fx.Bolt(fx, c.Age, c.Seed + k * 7, a, b, 0f, 0.32f, c.FromSize * 0.03f, col.Main, col.Light, 0);
        }
        Physical(c, col, fx);
    }

    private static void FireBlast(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.Orb(fx, c, 0.02f, I, 0.3f, col.Main, col.Light, FxShape.Flame);
        if (!c.Cue.Missed)
        {
            // The star of flame it bursts into
            float k = (c.Age - I) / 0.5f;
            if (k >= 0f && k <= 1f)
                for (int arm = 0; arm < 5; arm++)
                {
                    float a = arm * MathF.Tau / 5f + MathF.PI / 2f;
                    var dir = new Vector3(MathF.Cos(a), MathF.Sin(a), 0);
                    for (int j = 1; j <= 4; j++)
                        fx.Sprite(FxShape.Flame, c.To + dir * c.ToSize * 0.22f * j * Fx.EaseOut(k * 1.5f), c.ToSize * (0.2f - j * 0.025f),
                            Fx.Fade(j % 2 == 0 ? col.Light : col.Main, 1f - k), rotation: a - MathF.PI / 2f);
                }
            fx.ScreenFlash(Rgb(255, 200, 120), 0.25f * Fx.Envelope(c.Age, I, I + 0.04f, I + 0.08f, I + 0.3f));
        }
        Impact(c, col, fx, 1.3f);
    }

    /// <summary>A wheel of flame round the user as it charges.</summary>
    private static void FlameWheel(in FxCtx c, FxColors col, FxList fx)
    {
        float env = Fx.Envelope(c.Age, 0f, 0.08f, 0.3f, 0.42f);
        if (env > 0f)
            for (int i = 0; i < 10; i++)
            {
                float a = i * MathF.Tau / 10f + c.Age * 14f;
                var p = c.From + new Vector3(MathF.Cos(a), MathF.Sin(a), 0) * c.FromSize * 0.62f;
                fx.Sprite(FxShape.Flame, p, c.FromSize * 0.2f, Fx.Fade(i % 3 == 0 ? col.Light : col.Main, env), rotation: a);
            }
        Impact(c, col, fx);
    }

    private static void FireSpin(in FxCtx c, FxColors col, FxList fx)
    {
        float env = Fx.Envelope(c.Age, I - 0.1f, I, 0.9f, 1.2f);
        if (env > 0f)
            for (int i = 0; i < 14; i++)
            {
                float a = i * MathF.Tau / 14f + c.Age * 6f;
                float y = (i % 4) / 4f;
                var p = c.ToFeet + new Vector3(MathF.Cos(a) * c.ToSize * 0.6f, c.ToSize * (0.15f + y * 0.8f), MathF.Sin(a) * c.ToSize * 0.35f);
                fx.Sprite(FxShape.Flame, p, c.ToSize * 0.18f, Fx.Fade(i % 2 == 0 ? col.Main : col.Light, env));
            }
        Impact(c, col, fx, 0.8f);
    }

    private static void WillOWisp(in FxCtx c, FxColors col, FxList fx)
    {
        var ghost = ColorsOf(PokemonType.Ghost);
        Fx.Stream(fx, c, 0f, 0.7f, 0.45f, 3, FxShape.Wisp, Rgb(120, 140, 255), ghost.Light, 0.18f, arc: 0.1f, spread: 0.4f);
    }

    private static void Bubbles(in FxCtx c, FxColors col, FxList fx, int count)
    {
        Fx.Stream(fx, c, 0f, 0.55f, 0.38f, count, FxShape.Bubble, col.Light, White, 0.11f, arc: 0.05f, spread: 0.55f, blend: FxBlend.Alpha);
        if (!c.Cue.Missed)
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I + 0.05f, 0.35f, count / 2, FxShape.Ring, col.Light, White, 0.08f, 0.7f, salt: 13);
        Impact(c, col, fx, 0.9f);
    }

    private static void HydroPump(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.Beam(fx, c, 0.05f, 0.62f, 0.22f, col.Main, col.Light);
        Fx.Stream(fx, c, 0.05f, 0.62f, 0.22f, 20, FxShape.Drop, col.Light, col.Main, 0.08f, spread: 0.45f, blend: FxBlend.Alpha, trail: 1.4f);
        Impact(c, col, fx, 1.3f);
    }

    /// <summary>A wave crashing over the target.</summary>
    private static void Surf(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.WaveWall(fx, c, 0.0f, I + 0.05f, col.Main, col.Light);
        Fx.Rain(fx, c.Age, c.Seed, c.To, c.ToSize, 0.3f, 0.7f, 0.25f, 16, FxShape.Drop, col.Main, col.Light, 0.09f, FxBlend.Alpha, 1.5f);
        Fx.Shockwave(fx, c.Age, c.ToFeet + Vector3.UnitY * 0.03f, Vector3.UnitY, 0.3f, 0.6f, c.ToSize * 1.6f, Fx.Fade(col.Light, 0.9f));
        Impact(c, col, fx, 1.2f);
    }

    private static void Splash(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.Burst(fx, c.Seed, c.Age, c.FromFeet + Vector3.UnitY * c.FromSize * 0.2f, c.FromSize, 0.1f, 0.8f, 14, FxShape.Drop, col.Main, col.Light, 0.08f, 1.0f, 2.5f, FxBlend.Alpha);
    }

    /// <summary>Life drawn out of the target in green motes that drift back to the user.</summary>
    private static void Drain(in FxCtx c, FxColors col, FxList fx, int count)
    {
        Impact(c, col, fx, 0.8f);
        if (c.Cue.Missed || c.Cue.Blocked) return;
        Fx.Stream(fx, c, 0.4f, 1.2f, 0.45f, count, FxShape.Glow, Rgb(150, 255, 140), Rgb(230, 255, 200), 0.09f, arc: 0.12f, spread: 0.7f, reverse: true);
        Fx.Glint(fx, c.Age, c.From, c.FromSize, 0.85f, Rgb(200, 255, 190), 0.5f);
    }

    /// <summary>A powder sprinkled over the target.</summary>
    private static void Powder(in FxCtx c, FxList fx, Color a, Color b)
    {
        Fx.Stream(fx, c, 0f, 0.35f, 0.3f, 6, FxShape.Spore, a, b, 0.07f, arc: 0.12f, spread: 0.3f);
        Fx.Rain(fx, c.Age, c.Seed, c.To, c.ToSize, 0.25f, 1.1f, 0.6f, 26, FxShape.Spore, a, b, 0.05f, FxBlend.Add);
    }

    private static void Blizzard(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.Rain(fx, c.Age, c.Seed, c.To, c.ToSize, 0f, 0.7f, 0.3f, 36, FxShape.Snowflake, White, col.Light, 0.07f, FxBlend.Alpha, 0f);
        Fx.Stream(fx, c, 0f, 0.55f, 0.28f, 16, FxShape.Shard, col.Light, col.Main, 0.08f, spread: 0.7f, spin: 5f, blend: FxBlend.Alpha);
        fx.ScreenFlash(Rgb(230, 245, 255), 0.18f * Fx.Envelope(c.Age, I - 0.05f, I, I + 0.05f, I + 0.3f));
        Impact(c, col, fx, 1.2f);
    }

    private static void PsychicStrong(in FxCtx c, FxColors col, FxList fx)
    {
        PsychicWaves(c, col, fx);
        // The target is held in rings that close round it
        for (int i = 0; i < 3; i++)
        {
            float k = (c.Age - 0.2f - i * 0.12f) / 0.45f;
            if (k < 0f || k > 1f) continue;
            fx.Sprite(FxShape.Ring, c.To, c.ToSize * (1.3f - 0.8f * k), Fx.Fade(i % 2 == 0 ? col.Main : col.Light, MathF.Sin(k * MathF.PI)));
        }
    }

    private static void Rest(in FxCtx c, FxColors col, FxList fx)
    {
        HealAura(c, col, fx);
        for (int i = 0; i < 3; i++)
        {
            float k = (c.Age - 0.2f - i * 0.18f) / 0.7f;
            if (k < 0f || k > 1f) continue;
            var p = c.FromFeet + new Vector3(c.FromSize * (0.3f + 0.4f * k), c.FromSize * (0.9f + 0.6f * k), 0);
            fx.Sprite(FxShape.Zzz, p, c.FromSize * (0.1f + 0.08f * k), Fx.Fade(Rgb(200, 220, 255), MathF.Sin(k * MathF.PI)), FxBlend.Alpha, 0.2f);
        }
    }

    private static void ShadowSneak(in FxCtx c, FxColors col, FxList fx)
    {
        // Shadows slide over the ground to the target, then rise round it
        Fx.Stream(fx, c, 0f, 0.4f, 0.3f, 8, FxShape.Smoke, Rgb(50, 36, 70, 220), Rgb(30, 20, 40, 220), 0.16f, spread: 0.3f, blend: FxBlend.Alpha);
        Fx.Burst(fx, c.Seed, c.Age, c.ToFeet, c.ToSize, I - 0.05f, 0.6f, 8, FxShape.Wisp, col.Main, col.Light, 0.14f, 0.5f, -1.2f);
        Impact(c, col, fx);
    }

    /// <summary>Blows raining down on the target one after another.</summary>
    private static void Barrage(in FxCtx c, FxColors col, FxList fx)
    {
        if (!c.Cue.Missed)
            for (int i = 0; i < 4; i++)
            {
                var off = new Vector3(Fx.Signed(c.Seed, i, 91), Fx.Signed(c.Seed, i, 92), 0) * c.ToSize * 0.3f;
                Fx.Contact(fx, c.Age, c.Seed + i, c.To + off, c.ToSize, 0.18f + i * 0.06f, 0.22f, col.Main, col.Light);
            }
        Impact(c, col, fx, 1.1f);
    }

    private static void CrossMark(in FxCtx c, FxColors col, FxList fx)
    {
        if (c.Cue.Missed) return;
        float k = (c.Age - I + 0.05f) / 0.4f;
        if (k < 0f || k > 1f) return;
        fx.Sprite(FxShape.Cross, c.To, c.ToSize * 0.6f * (0.7f + 0.3f * Fx.EaseOut(k * 3f)), Fx.Fade(col.Light, 1f - k));
        fx.Sprite(FxShape.Cross, c.To, c.ToSize * 0.66f, Fx.Fade(col.Main, (1f - k) * 0.6f));
    }

    private static void HyperBeam(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.Beam(fx, c, 0.05f, 0.7f, 0.3f, Rgb(255, 200, 120), Rgb(255, 250, 230), 16);
        fx.ScreenFlash(Rgb(255, 244, 220), 0.3f * Fx.Envelope(c.Age, 0.1f, 0.15f, 0.3f, 0.55f));
        Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.5f, c.ToSize * 1.5f, Rgb(255, 220, 160));
        fx.Shake = Math.Max(fx.Shake, 0.3f * Fx.Envelope(c.Age, 0.3f, 0.35f, 0.55f, 0.75f));
        Impact(c, col, fx, 1.4f);
    }

    private static void QuickStrike(in FxCtx c, FxColors col, FxList fx)
    {
        SpeedLines(c, fx, 0f, 0.36f, 12, 0.8f);
        Impact(c, col, fx);
    }

    /// <summary>Sound waves rolling out of the user (and notes, for a growl).</summary>
    private static void Sound(in FxCtx c, FxList fx, int count, FxShape shape, bool notes, Color? tint = null)
    {
        var color = tint ?? Rgb(236, 240, 255);
        Fx.Waves(fx, c, 0f, 0.7f, count, shape, color, 0.5f, 0.3f);
        if (!notes) return;
        for (int i = 0; i < 3; i++)
        {
            float k = (c.Age - i * 0.15f) / 0.6f;
            if (k < 0f || k > 1f) continue;
            var p = c.From + new Vector3(Fx.Signed(c.Seed, i, 101) * 0.4f, 0.4f + k * 0.5f, 0) * c.FromSize;
            fx.Sprite(FxShape.Note, p, c.FromSize * 0.14f, Fx.Fade(Rgb(255, 240, 200), MathF.Sin(k * MathF.PI)), FxBlend.Alpha, 0.2f * MathF.Sin(k * 9f));
        }
    }

    private static void Sing(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.Stream(fx, c, 0f, 0.9f, 0.6f, 6, FxShape.Note, Rgb(255, 210, 240), Rgb(200, 230, 255), 0.13f, arc: 0.1f, spread: 0.6f, blend: FxBlend.Alpha);
    }

    private static void Leer(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.Glint(fx, c.Age, c.FromFeet + Vector3.UnitY * c.FromSize * 0.8f, c.FromSize, 0.05f, Rgb(255, 120, 120), 0.5f);
        Fx.Waves(fx, c, 0.2f, 0.7f, 2, FxShape.Ring, Rgb(255, 120, 120, 180), 0.4f, 0.25f);
    }

    private static void Hearts(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.Stream(fx, c, 0f, 0.7f, 0.45f, 6, FxShape.Heart, Rgb(255, 120, 170), Rgb(255, 190, 220), 0.12f, arc: 0.08f, spread: 0.5f, blend: FxBlend.Alpha);
        Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, 0.55f, 0.6f, 6, FxShape.Heart, Rgb(255, 120, 170), Rgb(255, 200, 225), 0.09f, 0.7f, -0.5f, FxBlend.Alpha);
    }

    /// <summary>Swords circling the user, closing in, and a flash as they meet.</summary>
    private static void SwordsDance(in FxCtx c, FxColors col, FxList fx)
    {
        float env = Fx.Envelope(c.Age, 0f, 0.15f, 0.7f, 0.9f);
        if (env > 0f)
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathF.Tau / 6f + c.Age * 5f;
                float rad = c.FromSize * (0.8f - 0.35f * Math.Clamp(c.Age / 0.8f, 0f, 1f));
                var p = c.FromFeet + new Vector3(MathF.Cos(a) * rad, c.FromSize * 0.55f, MathF.Sin(a) * rad * 0.55f);
                fx.Sprite(FxShape.Sword, p, c.FromSize * 0.22f, Fx.Fade(Rgb(230, 236, 250), env), FxBlend.Alpha);
            }
        Fx.Aura(fx, c.Age, c.Seed, c.FromFeet, c.FromSize, 0.1f, 1.0f, Rgb(255, 140, 90), Rgb(255, 220, 170), 8);
        Fx.Glint(fx, c.Age, c.FromFeet + Vector3.UnitY * c.FromSize * 0.55f, c.FromSize, 0.75f, White, 0.7f);
    }

    /// <summary>A hardened body: a pale ring of light passes over it and glints pop.</summary>
    private static void Shine(in FxCtx c, FxColors col, FxList fx)
    {
        float k = c.Age / 0.7f;
        if (k <= 1f)
            fx.Flat(FxShape.Ring, c.FromFeet + Vector3.UnitY * c.FromSize * k, Vector3.UnitY, c.FromSize * 0.6f, Fx.Fade(Rgb(220, 236, 255), MathF.Sin(k * MathF.PI)));
        fx.Sprite(FxShape.Glow, c.From, c.FromSize * 0.8f, Fx.Fade(Rgb(220, 236, 255), 0.4f * Fx.Envelope(c.Age, 0f, 0.2f, 0.5f, 0.8f)));
        for (int i = 0; i < 3; i++)
            Fx.Glint(fx, c.Age, c.From + new Vector3(Fx.Signed(c.Seed, i, 111), Fx.Signed(c.Seed, i, 112), 0) * c.FromSize * 0.35f,
                c.FromSize, 0.2f + i * 0.18f, White, 0.35f);
    }

    private static void HealAura(in FxCtx c, FxColors col, FxList fx) => HealOn(fx, c.Age, c.Seed, c.FromFeet, c.FromSize, 0f);

    /// <summary>Fangs closing on the target from above and below.</summary>
    private static void Chomp(in FxCtx c, FxColors col, FxList fx)
    {
        if (!c.Cue.Missed)
        {
            float k = (c.Age - (I - 0.16f)) / 0.3f;
            if (k >= 0f && k <= 1f)
            {
                float close = Fx.EaseOut(Math.Min(1f, k * 1.9f));
                float gap = c.ToSize * 0.42f * (1f - close);
                var fang = Fx.Fade(White, 1f - Math.Max(0f, k - 0.6f) / 0.4f);
                for (int i = -1; i <= 1; i += 2)
                {
                    var x = Vector3.UnitX * i * c.ToSize * 0.12f;
                    fx.Sprite(FxShape.Fang, c.To + x + Vector3.UnitY * (gap + c.ToSize * 0.1f), c.ToSize * 0.17f, fang, FxBlend.Alpha);
                    fx.Sprite(FxShape.Fang, c.To + x - Vector3.UnitY * (gap + c.ToSize * 0.1f), c.ToSize * 0.17f, fang, FxBlend.Alpha, MathF.PI);
                }
            }
        }
        Impact(c, col, fx);
    }

    private static void Claws(in FxCtx c, FxColors col, FxList fx)
    {
        if (!c.Cue.Missed) Fx.Slash(fx, c.Age, c.Seed, c.To, c.ToSize, I - 0.06f, 1, 0.1f, col.Light, White, FxShape.Claw);
        Impact(c, col, fx);
    }

    private static void Slashes(in FxCtx c, FxColors col, FxList fx)
    {
        if (!c.Cue.Missed) Fx.Slash(fx, c.Age, c.Seed, c.To, c.ToSize, I - 0.08f, 2, 0.09f, col.Main, White);
        Impact(c, col, fx);
    }

    private static void Earthquake(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        for (int i = 0; i < 3; i++)
            Fx.Shockwave(fx, c.Age, c.ToFeet + Vector3.UnitY * 0.03f, Vector3.UnitY, 0.05f + i * 0.12f, 0.55f, h * (1.4f + i * 0.4f), Fx.Fade(col.Dark, 0.9f));
        Fx.Burst(fx, c.Seed, c.Age, c.ToFeet, h, 0.15f, 0.9f, 14, FxShape.Rock, col.Main, col.Dark, 0.1f, 1.4f, 2.4f, FxBlend.Alpha, spin: 4f, salt: 14);
        Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + Vector3.UnitY * h * 0.1f, h, 0.1f, 1.0f, 8, FxShape.Smoke, Fx.Fade(col.Light, 0.85f), Fx.Fade(col.Main, 0.8f), 0.3f, 0.7f, -0.2f, FxBlend.Alpha, salt: 15);
        fx.Shake = Math.Max(fx.Shake, Fx.Envelope(c.Age, 0f, 0.08f, 0.6f, 0.9f));
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Sand or mud flung at the target from the user's feet.</summary>
    private static void Spray(in FxCtx c, FxList fx, Color a, Color b, bool wet)
    {
        Fx.Stream(fx, c, 0f, 0.4f, 0.24f, 34, wet ? FxShape.Drop : FxShape.Dot, a, b, wet ? 0.06f : 0.035f, arc: 0.04f, spread: 0.55f, blend: FxBlend.Alpha);
        if (!wet) Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, 0.24f, 0.6f, 6, FxShape.Smoke, Fx.Fade(a, 0.7f), Fx.Fade(b, 0.6f), 0.2f, 0.4f, -0.2f, FxBlend.Alpha);
    }

    private static void RockSlide(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.Rain(fx, c.Age, c.Seed, c.To, c.ToSize, 0f, 0.55f, 0.3f, 7, FxShape.Rock, col.Light, col.Main, 0.16f);
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Stone spikes bursting up out of the ground round the target.</summary>
    private static void StoneEdge(in FxCtx c, FxColors col, FxList fx)
    {
        if (!c.Cue.Missed)
            for (int i = 0; i < 6; i++)
            {
                float k = (c.Age - 0.12f - i * 0.03f) / 0.6f;
                if (k < 0f || k > 1f) continue;
                float a = i * MathF.Tau / 6f + 0.3f;
                var p = c.ToFeet + new Vector3(MathF.Cos(a) * c.ToSize * 0.55f, c.ToSize * 0.3f * Fx.EaseOut(k * 3f), MathF.Sin(a) * c.ToSize * 0.3f);
                fx.Sprite(FxShape.Shard, p, c.ToSize * 0.3f, Fx.Fade(col.Light, Math.Min(1f, (1f - k) * 3f)), FxBlend.Alpha, MathF.Cos(a) * 0.4f);
            }
        Impact(c, col, fx, 1.1f);
    }

    private static void Needles(in FxCtx c, FxColors col, FxList fx, int count)
    {
        Fx.Stream(fx, c, 0f, 0.4f, 0.22f, count, FxShape.Streak, col.Light, White, 0.04f, spread: 0.2f, trail: 5f);
        Impact(c, col, fx, 0.8f);
    }

    private static void RainDance(in FxCtx c, FxColors col, FxList fx)
    {
        var blue = ColorsOf(PokemonType.Water);
        Fx.Rain(fx, c.Age, c.Seed, c.From, c.FromSize * 2f, 0f, 1.2f, 0.35f, 40, FxShape.Streak, blue.Light, blue.Main, 0.03f, FxBlend.Add, 4f);
    }

    private static void SunnyDay(in FxCtx c, FxColors col, FxList fx)
    {
        fx.ScreenFlash(Rgb(255, 220, 150), 0.3f * Fx.Envelope(c.Age, 0f, 0.2f, 0.6f, 1.1f));
        Fx.Aura(fx, c.Age, c.Seed, c.FromFeet, c.FromSize, 0f, 1.1f, Rgb(255, 180, 80), Rgb(255, 240, 180));
    }

    private static void Explosion(in FxCtx c, FxColors col, FxList fx)
    {
        var fire = ColorsOf(PokemonType.Fire);
        Fx.Burst(fx, c.Seed, c.Age, c.From, c.FromSize, 0.15f, 0.8f, 26, FxShape.Flame, fire.Main, fire.Light, 0.25f, 1.6f, -0.5f);
        Fx.Burst(fx, c.Seed, c.Age, c.From, c.FromSize, 0.2f, 1.0f, 12, FxShape.Smoke, Rgb(120, 110, 100, 230), Rgb(80, 74, 70, 230), 0.4f, 1.0f, -0.3f, FxBlend.Alpha, salt: 16);
        fx.ScreenFlash(White, 0.5f * Fx.Envelope(c.Age, 0.12f, 0.16f, 0.22f, 0.5f));
        fx.Shake = Math.Max(fx.Shake, Fx.Envelope(c.Age, 0.12f, 0.2f, 0.5f, 0.9f));
        Impact(c, col, fx, 1.3f);
    }

    // ------------------------------------------------------------------ marks on one Pokémon

    /// <summary>A stat rising (warm streaks climbing round it) or falling (cool streaks sinking).</summary>
    private static void StatChange(in FxCtx c, FxList fx, bool up)
    {
        float h = c.FromSize;
        var feet = c.FromFeet;
        var color = up ? Rgb(255, 130, 90) : Rgb(90, 150, 255);
        var light = up ? Rgb(255, 226, 170) : Rgb(190, 220, 255);
        float env = Fx.Envelope(c.Age, 0f, 0.15f, 0.8f, BattleAnimator.MarkTime);
        for (int i = 0; i < 12; i++)
        {
            float u = (c.Age * 1.4f + Fx.Rand(c.Seed, i, 121)) % 1f;
            if (!up) u = 1f - u;
            float a = Fx.Rand(c.Seed, i, 122) * MathF.Tau;
            var p = feet + new Vector3(MathF.Cos(a) * h * 0.5f, h * (0.1f + 1.1f * u), MathF.Sin(a) * h * 0.3f);
            fx.Streak(FxShape.Streak, p, Vector3.UnitY, h * 0.05f, 6f, Fx.Fade(Fx.Mix(color, light, Fx.Rand(c.Seed, i, 123)), env * MathF.Sin(u * MathF.PI)));
        }
        for (int i = 0; i < 2; i++)
        {
            float k = (c.Age * 0.9f + i * 0.5f) % 1f;
            float y = up ? k : 1f - k;
            fx.Flat(FxShape.Ring, feet + Vector3.UnitY * h * y, Vector3.UnitY, h * 0.55f, Fx.Fade(color, env * MathF.Sin(k * MathF.PI) * 0.8f));
        }
        fx.Sprite(FxShape.Glow, feet + Vector3.UnitY * h * 0.5f, h * 0.8f, Fx.Fade(color, env * 0.3f));
    }

    private static void Heal(in FxCtx c, FxList fx) => HealOn(fx, c.Age, c.Seed, c.FromFeet, c.FromSize, 0f);

    private static void HealOn(FxList fx, float age, int seed, Vector3 feet, float h, float t0)
    {
        Fx.Aura(fx, age, seed, feet, h, t0, t0 + 1.0f, Rgb(120, 230, 140), Rgb(230, 255, 220), 18);
        for (int i = 0; i < 5; i++)
        {
            float k = (age - t0 - 0.1f - i * 0.12f) / 0.5f;
            if (k < 0f || k > 1f) continue;
            var p = feet + new Vector3(Fx.Signed(seed, i, 131) * h * 0.45f, h * (0.3f + 0.6f * Fx.Rand(seed, i, 132)), 0);
            fx.Sprite(FxShape.Spark, p, h * 0.2f * MathF.Sin(k * MathF.PI), Rgb(230, 255, 230), rotation: k);
        }
    }

    /// <summary>A status condition taking hold: flames, bubbles, sparks, sleep or ice.</summary>
    private static void Status(in FxCtx c, FxList fx)
    {
        float h = c.FromSize;
        var feet = c.FromFeet;
        float age = c.Age;
        float env = Fx.Envelope(age, 0f, 0.1f, 0.8f, BattleAnimator.MarkTime);
        switch (c.Cue.Status)
        {
            case StatusCondition.Burn:
            {
                var fire = ColorsOf(PokemonType.Fire);
                for (int i = 0; i < 7; i++)
                {
                    float flicker = 0.75f + 0.25f * MathF.Sin(age * 22f + i * 2f);
                    var p = feet + new Vector3(Fx.Signed(c.Seed, i, 141) * h * 0.45f, h * (0.15f + 0.6f * Fx.Rand(c.Seed, i, 142)), 0.1f);
                    fx.Sprite(FxShape.Flame, p + Vector3.UnitY * h * 0.1f * ((age * 2f + i * 0.3f) % 1f), h * 0.16f * flicker, Fx.Fade(i % 2 == 0 ? fire.Main : fire.Light, env));
                }
                break;
            }
            case StatusCondition.Poison:
            case StatusCondition.Toxic:
            {
                var poison = ColorsOf(PokemonType.Poison);
                Fx.Burst(fx, c.Seed, age, feet + Vector3.UnitY * h * 0.4f, h, 0f, 1.0f, 12, FxShape.Bubble, poison.Main, poison.Light, 0.08f, 0.5f, -0.9f, FxBlend.Alpha);
                fx.Sprite(FxShape.Glow, feet + Vector3.UnitY * h * 0.5f, h * 0.8f, Fx.Fade(poison.Main, env * 0.3f));
                break;
            }
            case StatusCondition.Paralyze:
            {
                var electric = ColorsOf(PokemonType.Electric);
                for (int k = 0; k < 4; k++)
                {
                    var a = feet + Vector3.UnitY * h * 0.5f + Fx.Direction(c.Seed, 150 + k) * h * 0.5f;
                    var b = feet + Vector3.UnitY * h * 0.5f + Fx.Direction(c.Seed, 160 + k) * h * 0.5f;
                    Fx.Bolt(fx, age, c.Seed + k * 11, a, b, k * 0.15f, 0.45f + k * 0.15f, h * 0.03f, electric.Main, electric.Light, 0);
                }
                break;
            }
            case StatusCondition.Sleep:
                for (int i = 0; i < 3; i++)
                {
                    float k = (age - i * 0.25f) / 0.75f;
                    if (k < 0f || k > 1f) continue;
                    var p = feet + new Vector3(h * (0.25f + 0.4f * k), h * (0.85f + 0.5f * k), 0);
                    fx.Sprite(FxShape.Zzz, p, h * (0.09f + 0.08f * k), Fx.Fade(Rgb(210, 226, 255), MathF.Sin(k * MathF.PI)), FxBlend.Alpha, 0.25f);
                }
                break;
            case StatusCondition.Freeze:
            {
                var ice = ColorsOf(PokemonType.Ice);
                Fx.Burst(fx, c.Seed, age, feet + Vector3.UnitY * h * 0.5f, h, 0.05f, 0.7f, 12, FxShape.Shard, ice.Main, ice.Light, 0.11f, 0.8f, 1.2f, FxBlend.Alpha, spin: 3f);
                fx.Sprite(FxShape.Glow, feet + Vector3.UnitY * h * 0.5f, h * 0.9f, Fx.Fade(ice.Light, env * 0.5f));
                fx.ScreenFlash(Rgb(230, 245, 255), 0.18f * Fx.Envelope(age, 0f, 0.04f, 0.08f, 0.3f));
                break;
            }
        }
    }
}
