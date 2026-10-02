using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
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

    /// <summary>Seconds since the send-out began: the ball flies onto the platform, opens, and the Pokémon grows out of it.</summary>
    public float SendOutAge { get; internal set; } = -1f;
    public float AttackAge { get; internal set; } = -1f;

    /// <summary>The kind of move being used, which decides how the attack looks (a strike, a cast or a hop).</summary>
    public MoveCategory AttackCategory { get; internal set; } = MoveCategory.Physical;

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

/// <summary>What an effect cue shows: a move, or something that happens to one Pokémon.</summary>
public enum CueKind { Move, StatUp, StatDown, Heal, Status }

/// <summary>
/// An effect for the renderer to play (plan 04 · G8): a move from one place to another and how it landed, or a
/// stat change, a heal or a status condition on one Pokémon. A cue holds no graphics; what it looks like is a
/// function of the cue and its age, so the same cue always plays the same way.
/// </summary>
public sealed class EffectCue
{
    public CueKind Kind { get; init; }

    /// <summary>The move's name (moves with an effect of their own are looked up by it), or empty.</summary>
    public string Move { get; init; } = "";
    public PokemonType Type { get; init; } = PokemonType.Normal;
    public MoveCategory Category { get; init; } = MoveCategory.Physical;

    /// <summary>For <see cref="CueKind.Status"/>: the condition that was given.</summary>
    public StatusCondition Status { get; init; }

    public BattleSide FromSide { get; init; }
    public int FromSlot { get; init; }
    public BattleSide ToSide { get; init; }
    public int ToSlot { get; init; }

    /// <summary>The move missed: it flies past and nothing lands.</summary>
    public bool Missed { get; init; }

    /// <summary>The move reached the target but did nothing (an immunity, an ability that absorbs it).</summary>
    public bool Blocked { get; init; }
    public bool Critical { get; init; }
    public bool SuperEffective { get; init; }

    /// <summary>Varies the particles from one use to the next, without touching the battle's random numbers.</summary>
    public int Seed { get; internal set; }

    public float Age { get; internal set; }

    /// <summary>A move the user aims at itself (Swords Dance, Harden).</summary>
    public bool OnSelf => FromSide == ToSide && FromSlot == ToSlot;

    /// <summary>The move hit and did something.</summary>
    public bool Landed => !Missed && !Blocked;

    /// <summary>How long this cue plays.</summary>
    public float Length => Kind == CueKind.Move ? BattleAnimator.EffectTime : BattleAnimator.MarkTime;
}

/// <summary>The steps of a thrown Poké Ball, in order.</summary>
public enum BallPhase { RunIn, Flight, Open, Drop, Wobble, Pause, Click, Burst, Rest }

/// <summary>
/// A Poké Ball thrown at a wild Pokémon (plan 04 · G8). The trainer runs in and throws it; it flies over to the
/// foe, opens and draws it in, drops onto the platform, wobbles once per successful shake check, and then clicks
/// shut (and stays there) or bursts open.
/// </summary>
public sealed class BallThrowView
{
    /// <summary>The ball's item name, which decides its colours.</summary>
    public string Ball { get; init; } = "Poké Ball";
    public int Slot { get; init; }

    /// <summary>Successful shake checks; 4 means caught.</summary>
    public int Shakes { get; init; }

    public float Age { get; internal set; }

    public bool Caught => Shakes >= 4;
}

/// <summary>
/// Presentation state for a battle, driven by events from <see cref="BattleEngine"/>. It holds no graphics, so
/// the renderer reads it and tests can check it.
/// </summary>
public sealed class BattleAnimator
{
    /// <summary>A send-out: the ball flies onto the platform in <see cref="SendOutBallTime"/>, then the Pokémon grows out of it.</summary>
    public const float SendOutTime = 1.0f;
    public const float SendOutBallTime = 0.35f;
    // A physical strike and a special move's release peak about when the damage lands (the engine's hit delay)
    public const float AttackTime = 0.65f;
    public const float HitTime = 0.55f;
    public const float FaintTime = 1.0f;
    public const float RecallTime = 0.45f;
    public const float CaptureTime = 0.45f;
    public const float TrainerExitTime = 0.9f;

    /// <summary>How long a move's effect plays; it lands <see cref="ImpactTime"/> in, when the damage does.</summary>
    public const float EffectTime = 1.3f;
    public const float ImpactTime = 0.35f;

    /// <summary>How long a stat change, a heal or a status condition shows on its Pokémon.</summary>
    public const float MarkTime = 1.1f;

    // The thrown ball: the trainer runs in and throws, the ball flies, opens over the foe and pulls it in, drops
    // onto the platform, wobbles once per successful shake check with a short pause after each, then clicks shut
    // or bursts open
    public const float BallReleaseTime = 0.6f;
    public const float BallFlightTime = 0.8f;
    public const float BallOpenTime = 0.55f;
    public const float BallDropTime = 0.3f;
    public const float BallWobbleTime = 0.45f;
    public const float BallPauseTime = 0.2f;
    public const float BallEndTime = 0.5f;

    /// <summary>When the ball reaches the foe and opens.</summary>
    public const float BallArriveTime = BallReleaseTime + BallFlightTime;

    /// <summary>When the ball stops wobbling: it clicks shut, or bursts open and the Pokémon breaks free.</summary>
    public static float BallSettleTime(int shakes) =>
        BallArriveTime + BallOpenTime + BallDropTime + (BallWobbleTime + BallPauseTime) * Math.Min(shakes, 3);

    /// <summary>How long a throw with <paramref name="shakes"/> successful shake checks takes to play out.</summary>
    public static float BallThrowTime(int shakes) => BallSettleTime(shakes) + BallEndTime;

    /// <summary>Where a thrown ball is at <paramref name="age"/>: its phase, progress through it (0..1) and which wobble (0-based).</summary>
    public static (BallPhase Phase, float Progress, int Wobble) BallStage(float age, int shakes)
    {
        if (age < BallReleaseTime) return (BallPhase.RunIn, age / BallReleaseTime, 0);
        float t = age - BallReleaseTime;
        if (t < BallFlightTime) return (BallPhase.Flight, t / BallFlightTime, 0);
        if ((t -= BallFlightTime) < BallOpenTime) return (BallPhase.Open, t / BallOpenTime, 0);
        if ((t -= BallOpenTime) < BallDropTime) return (BallPhase.Drop, t / BallDropTime, 0);
        t -= BallDropTime;
        int wobbles = Math.Min(shakes, 3);
        float step = BallWobbleTime + BallPauseTime;
        int index = (int)(t / step);
        if (index < wobbles)
        {
            float within = t - index * step;
            return within < BallWobbleTime
                ? (BallPhase.Wobble, within / BallWobbleTime, index)
                : (BallPhase.Pause, (within - BallWobbleTime) / BallPauseTime, index);
        }
        t -= wobbles * step;
        if (t < BallEndTime) return (shakes >= 4 ? BallPhase.Click : BallPhase.Burst, t / BallEndTime, wobbles);
        return (shakes >= 4 ? BallPhase.Rest : BallPhase.Burst, 1f, wobbles);
    }

    /// <summary>Seconds since the battle started (drives the camera intro and idle loops).</summary>
    public float Time { get; private set; }

    private readonly CombatantView[,] views = { { new(), new() }, { new(), new() } };

    /// <summary>Pokémon per side: 1 in a single battle, 2 in a double.</summary>
    public int Slots { get; internal set; } = 1;

    /// <summary>The first (or only) place on each side.</summary>
    public CombatantView Player => views[0, 0];
    public CombatantView Enemy => views[1, 0];

    /// <summary>Character type standing on each platform before the Pokémon are sent out (null = none).</summary>
    public string? PlayerTrainer { get; internal set; } = "PLAYER";
    public string? EnemyTrainer { get; internal set; }

    /// <summary>A second opposing trainer standing beside the first (two trainers battling together).</summary>
    public string? EnemyTrainer2 { get; internal set; }

    /// <summary>Seconds since the trainer started walking off the platform (-1 = still standing there).</summary>
    public float PlayerTrainerExit { get; private set; } = -1f;
    public float EnemyTrainerExit { get; private set; } = -1f;

    /// <summary>Seconds since the last big hit, for camera shake (-1 = none).</summary>
    public float ShakeAge { get; private set; } = -1f;

    /// <summary>How hard the last hit shakes the camera: 1 for a hit, more for a critical or super-effective one.</summary>
    public float ShakeStrength { get; private set; } = 1f;

    private readonly List<EffectCue> cues = new();
    private int cueCount;

    /// <summary>The effects playing now, oldest first.</summary>
    public IReadOnlyList<EffectCue> Cues => cues;

    /// <summary>The Poké Ball thrown at the foe, while it plays out (a caught Pokémon's ball stays until the battle ends).</summary>
    public BallThrowView? Ball { get; private set; }

    public CombatantView this[BattleSide side] => this[side, 0];
    public CombatantView this[BattleSide side, int slot] => views[(int)side, slot];

    /// <summary>A Pokémon already on the field when the battle opens (wild encounters).</summary>
    public void Appear(BattleSide side, Pokemon pokemon, int slot = 0)
    {
        var v = this[side, slot];
        Reset(v, pokemon);
        v.Present = true;
    }

    /// <summary>Thrown out of a Poké Ball: the trainer throws it and steps aside, and the Pokémon bursts out as it opens.</summary>
    public void SendOut(BattleSide side, Pokemon pokemon, int slot = 0)
    {
        var v = this[side, slot];
        Reset(v, pokemon);
        v.Present = true;
        v.SendOutAge = 0f;
        if (side == BattleSide.Player && PlayerTrainerExit < 0f) PlayerTrainerExit = 0f;
        if (side == BattleSide.Enemy && EnemyTrainer != null && EnemyTrainerExit < 0f) EnemyTrainerExit = 0f;
    }

    public void Attack(BattleSide side, int slot = 0, MoveCategory category = MoveCategory.Physical)
    {
        var v = this[side, slot];
        v.AttackAge = 0f;
        v.AttackCategory = category;
    }

    /// <param name="strength">How hard the camera shakes: 1 for a hit, up to 2 for a critical or super-effective one.</param>
    public void Hit(BattleSide side, int slot = 0, float strength = 1f)
    {
        this[side, slot].HitAge = 0f;
        ShakeAge = 0f;
        ShakeStrength = strength;
    }

    /// <summary>Starts an effect; it plays for <see cref="EffectCue.Length"/> seconds.</summary>
    public EffectCue Cue(EffectCue cue)
    {
        cue.Seed = ++cueCount * 7919 + cue.Move.Length;
        cue.Age = 0f;
        cues.Add(cue);
        return cue;
    }

    /// <summary>A stat going up or down on one Pokémon.</summary>
    public EffectCue StatChange(BattleSide side, int slot, bool up) =>
        Cue(new EffectCue { Kind = up ? CueKind.StatUp : CueKind.StatDown, FromSide = side, FromSlot = slot, ToSide = side, ToSlot = slot });

    /// <summary>HP restored to one Pokémon.</summary>
    public EffectCue Heal(BattleSide side, int slot) =>
        Cue(new EffectCue { Kind = CueKind.Heal, FromSide = side, FromSlot = slot, ToSide = side, ToSlot = slot, Type = PokemonType.Grass });

    /// <summary>A status condition given to one Pokémon.</summary>
    public EffectCue StatusGiven(BattleSide side, int slot, StatusCondition status) =>
        Cue(new EffectCue { Kind = CueKind.Status, Status = status, FromSide = side, FromSlot = slot, ToSide = side, ToSlot = slot });

    /// <summary>Faints after <paramref name="delay"/> seconds (so the hit and the HP drain play first).</summary>
    public void Faint(BattleSide side, float delay = 0.6f, int slot = 0)
    {
        var v = this[side, slot];
        v.FaintDelay = delay;
        v.FaintAge = 0f;
    }

    public void Recall(BattleSide side, int slot = 0) => this[side, slot].RecallAge = 0f;

    /// <summary>The foe is drawn into the Poké Ball once it lands (after <paramref name="delay"/> seconds).</summary>
    public void Capture(float delay, int slot = 0)
    {
        var v = this[BattleSide.Enemy, slot];
        v.CaptureDelay = delay;
        v.CaptureAge = 0f;
    }

    /// <summary>The player throws <paramref name="ball"/> at the foe in <paramref name="slot"/>; it is drawn in when the ball arrives.</summary>
    public void ThrowBall(string ball, int slot, int shakes)
    {
        Ball = new BallThrowView { Ball = ball, Slot = slot, Shakes = shakes };
        Capture(BallArriveTime, slot);
    }

    /// <summary>The Pokémon broke out of the ball: it bursts out of it where it lies, without a new throw.</summary>
    public void BreakFree(int slot = 0)
    {
        var v = this[BattleSide.Enemy, slot];
        v.CaptureAge = -1f;
        v.Present = true;
        v.SendOutAge = SendOutBallTime;
    }

    public void Update(float dt, Pokemon? player, Pokemon? enemy) =>
        Update(dt, (side, slot) => slot > 0 ? null : side == BattleSide.Player ? player : enemy);

    /// <param name="logical">The Pokémon the battle logic has in each place (the bars follow it before anything is shown).</param>
    public void Update(float dt, Func<BattleSide, int, Pokemon?> logical)
    {
        Time += dt;
        if (ShakeAge >= 0f && (ShakeAge += dt) > 0.4f) ShakeAge = -1f;
        if (PlayerTrainerExit >= 0f) PlayerTrainerExit += dt;
        if (EnemyTrainerExit >= 0f) EnemyTrainerExit += dt;
        if (PlayerTrainerExit > TrainerExitTime) PlayerTrainer = null;
        if (EnemyTrainerExit > TrainerExitTime) EnemyTrainer = EnemyTrainer2 = null;

        for (int i = 0; i < cues.Count; i++)
        {
            var cue = cues[i];
            if ((cue.Age += dt) >= cue.Length) cues.RemoveAt(i--);
        }
        if (Ball != null)
        {
            Ball.Age += dt;
            if (!Ball.Caught && Ball.Age >= BallThrowTime(Ball.Shakes)) Ball = null;
        }

        for (int side = 0; side < 2; side++)
            for (int slot = 0; slot < Slots; slot++)
                Advance(views[side, slot], dt, logical((BattleSide)side, slot));
    }

    private static void Advance(CombatantView v, float dt, Pokemon? logical)
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
        if (shown == null) return;
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

    /// <summary>
    /// How far a send-out has grown the Pokémon out of its ball: -1 before the ball opens (or when no send-out is
    /// running), then 0..1.
    /// </summary>
    public static float Emerge(float sendOutAge) =>
        sendOutAge < SendOutBallTime ? -1f : Math.Clamp((sendOutAge - SendOutBallTime) / (SendOutTime - SendOutBallTime), 0f, 1f);
}
