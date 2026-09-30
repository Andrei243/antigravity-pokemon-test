using System;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

public enum BattleSide { Player, Enemy }

/// <summary>What one side of the field shows, and the timers of its running animations (seconds, -1 = idle).</summary>
public sealed class CombatantView
{
    /// <summary>The Pokémon drawn on this side's platform (can lag behind the battle logic during a switch).</summary>
    public Pokemon? Shown { get; internal set; }

    /// <summary>On the platform (fainted, recalled or captured Pokémon are not).</summary>
    public bool Present { get; internal set; }

    public float SendOutAge { get; internal set; } = -1f;
    public float AttackAge { get; internal set; } = -1f;
    public float HitAge { get; internal set; } = -1f;
    public float FaintAge { get; internal set; } = -1f;
    public float RecallAge { get; internal set; } = -1f;
    public float CaptureAge { get; internal set; } = -1f;

    /// <summary>HP and EXP as the bars show them: they drain toward the real values instead of jumping.</summary>
    public float DisplayedHp { get; internal set; }
    public float DisplayedExp { get; internal set; }

    internal float FaintDelay;
    internal float CaptureDelay;
}

/// <summary>
/// Presentation state for a battle, driven by events from <see cref="BattleEngine"/>. It holds no graphics, so
/// the renderer reads it and tests can check it.
/// </summary>
public sealed class BattleAnimator
{
    public const float SendOutTime = 0.6f;
    public const float AttackTime = 0.45f;
    public const float HitTime = 0.55f;
    public const float FaintTime = 0.7f;
    public const float RecallTime = 0.45f;
    public const float CaptureTime = 0.45f;
    public const float TrainerExitTime = 0.9f;

    /// <summary>Seconds since the battle started (drives the camera intro and idle loops).</summary>
    public float Time { get; private set; }

    public CombatantView Player { get; } = new();
    public CombatantView Enemy { get; } = new();

    /// <summary>Character type standing on each platform before the Pokémon are sent out (null = none).</summary>
    public string? PlayerTrainer { get; internal set; } = "PLAYER";
    public string? EnemyTrainer { get; internal set; }

    /// <summary>Seconds since the trainer started walking off the platform (-1 = still standing there).</summary>
    public float PlayerTrainerExit { get; private set; } = -1f;
    public float EnemyTrainerExit { get; private set; } = -1f;

    /// <summary>Seconds since the last big hit, for camera shake (-1 = none).</summary>
    public float ShakeAge { get; private set; } = -1f;

    public CombatantView this[BattleSide side] => side == BattleSide.Player ? Player : Enemy;

    /// <summary>A Pokémon already on the field when the battle opens (wild encounters).</summary>
    public void Appear(BattleSide side, Pokemon pokemon)
    {
        var v = this[side];
        Reset(v, pokemon);
        v.Present = true;
    }

    /// <summary>Thrown out of a Poké Ball: the trainer steps aside and the Pokémon bursts out of a flash.</summary>
    public void SendOut(BattleSide side, Pokemon pokemon)
    {
        var v = this[side];
        Reset(v, pokemon);
        v.Present = true;
        v.SendOutAge = 0f;
        if (side == BattleSide.Player && PlayerTrainerExit < 0f) PlayerTrainerExit = 0f;
        if (side == BattleSide.Enemy && EnemyTrainer != null && EnemyTrainerExit < 0f) EnemyTrainerExit = 0f;
    }

    public void Attack(BattleSide side) => this[side].AttackAge = 0f;

    public void Hit(BattleSide side)
    {
        this[side].HitAge = 0f;
        ShakeAge = 0f;
    }

    /// <summary>Faints after <paramref name="delay"/> seconds (so the hit and the HP drain play first).</summary>
    public void Faint(BattleSide side, float delay = 0.6f)
    {
        var v = this[side];
        v.FaintDelay = delay;
        v.FaintAge = 0f;
    }

    public void Recall(BattleSide side) => this[side].RecallAge = 0f;

    /// <summary>The foe is drawn into the Poké Ball once it lands (after <paramref name="delay"/> seconds).</summary>
    public void Capture(float delay)
    {
        Enemy.CaptureDelay = delay;
        Enemy.CaptureAge = 0f;
    }

    /// <summary>The Pokémon broke out of the ball.</summary>
    public void BreakFree()
    {
        Enemy.CaptureAge = -1f;
        Enemy.Present = true;
        Enemy.SendOutAge = 0f;
    }

    public void Update(float dt, Pokemon player, Pokemon enemy)
    {
        Time += dt;
        if (ShakeAge >= 0f && (ShakeAge += dt) > 0.4f) ShakeAge = -1f;
        if (PlayerTrainerExit >= 0f) PlayerTrainerExit += dt;
        if (EnemyTrainerExit >= 0f) EnemyTrainerExit += dt;
        if (PlayerTrainerExit > TrainerExitTime) PlayerTrainer = null;
        if (EnemyTrainerExit > TrainerExitTime) EnemyTrainer = null;

        Advance(Player, dt, player);
        Advance(Enemy, dt, enemy);
    }

    private static void Advance(CombatantView v, float dt, Pokemon logical)
    {
        v.SendOutAge = Tick(v.SendOutAge, dt, SendOutTime);
        v.AttackAge = Tick(v.AttackAge, dt, AttackTime);
        v.HitAge = Tick(v.HitAge, dt, HitTime);

        if (v.FaintAge >= 0f)
        {
            if (v.FaintDelay > 0f) v.FaintDelay -= dt;
            else if ((v.FaintAge += dt) >= FaintTime)
            {
                v.FaintAge = -1f;
                v.Present = false;
            }
        }
        if (v.RecallAge >= 0f && (v.RecallAge += dt) >= RecallTime)
        {
            v.RecallAge = -1f;
            v.Present = false;
        }
        if (v.CaptureAge >= 0f)
        {
            if (v.CaptureDelay > 0f) v.CaptureDelay -= dt;
            else if ((v.CaptureAge += dt) >= CaptureTime)
            {
                v.CaptureAge = -1f;
                v.Present = false;
            }
        }

        // Bars follow the Pokémon on the platform (or the logical one before anything was sent out)
        var shown = v.Shown ?? logical;
        float hpRate = Math.Max(1f, shown.MaxHP * 0.9f);
        v.DisplayedHp = Toward(v.DisplayedHp, shown.CurrentHP, hpRate * dt);
        float exp = shown.ExpProgressRatio;
        v.DisplayedExp = exp < v.DisplayedExp ? exp : Toward(v.DisplayedExp, exp, 0.8f * dt);
    }

    private static void Reset(CombatantView v, Pokemon pokemon)
    {
        v.Shown = pokemon;
        v.SendOutAge = v.AttackAge = v.HitAge = v.FaintAge = v.RecallAge = v.CaptureAge = -1f;
        v.FaintDelay = v.CaptureDelay = 0f;
        v.DisplayedHp = pokemon.CurrentHP;
        v.DisplayedExp = pokemon.ExpProgressRatio;
    }

    private static float Tick(float age, float dt, float length) => age < 0f ? age : (age + dt >= length ? -1f : age + dt);

    private static float Toward(float value, float target, float step) =>
        value < target ? Math.Min(target, value + step) : Math.Max(target, value - step);

    /// <summary>Progress 0..1 of a running animation, or -1 when it isn't running.</summary>
    public static float Progress(float age, float length) => age < 0f ? -1f : Math.Clamp(age / length, 0f, 1f);
}
