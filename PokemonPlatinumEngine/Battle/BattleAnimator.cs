using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.Battle;

public enum BattleSide { Player, Enemy }

/// <summary>What one side of the field shows, and the timers of its running animations (seconds, -1 = idle).</summary>
public sealed class CombatantView
{
    /// <summary>The Pokémon drawn on this side's platform (can lag behind the battle logic during a switch).</summary>
    public Pokemon? Shown { get; internal set; }

    /// <summary>On the platform (fainted, recalled or captured Pokémon are not).</summary>
    public bool Present { get; internal set; }

    /// <summary>Still in the battle but out of sight for now: up in the air or under the ground, in the middle of a move.</summary>
    public bool Away { get; internal set; }

    /// <summary>
    /// Seconds since it began to leave the field for its move's turn out of sight (plan 25 · Y16): it is drawn going
    /// for <see cref="Graphics.BodyMotions.LeaveTime"/>, and not at all after (-1 = not leaving).
    /// </summary>
    public float AwayAge { get; internal set; } = -1f;

    /// <summary>Seconds since it came back from out of sight (-1 = not coming back).</summary>
    public float BackAge { get; internal set; } = -1f;

    /// <summary>The move it went out of sight for: how it goes and comes back (up, under, into the dark).</summary>
    public string AwayMove { get; internal set; } = "";

    /// <summary>Seconds since a move began to spend it (Memento, Final Gambit, Healing Wish): it collapses until it faints (-1 = none).</summary>
    public float SpentAge { get; internal set; } = -1f;

    /// <summary>Seconds since the send-out began: the ball flies onto the platform, opens, and the Pokémon grows out of it.</summary>
    public float SendOutAge { get; internal set; } = -1f;

    /// <summary>Seconds since an ally that answered a call began its hop onto the place (plan 14 · B5; -1 = none).</summary>
    public float ArriveAge { get; internal set; } = -1f;
    public float AttackAge { get; internal set; } = -1f;

    /// <summary>The kind of move being used, which decides how the attack looks (a strike, a cast or a hop).</summary>
    public MoveCategory AttackCategory { get; internal set; } = MoveCategory.Physical;

    public float HitAge { get; internal set; } = -1f;
    public float FaintAge { get; internal set; } = -1f;
    public float RecallAge { get; internal set; } = -1f;
    public float CaptureAge { get; internal set; } = -1f;

    /// <summary>
    /// The ball this place's Pokémon comes out of and goes back into (plan 25 · Y4): the one it was caught in, a
    /// Poké Ball for one that was not. Set as it is sent out, called back or breaks out of a thrown ball.
    /// </summary>
    public string Ball { get; internal set; } = Graphics.BattleBall.DefaultBall;

    /// <summary>HP and EXP as the bars show them: they drain toward the real values instead of jumping.</summary>
    public float DisplayedHp { get; internal set; }
    public float DisplayedExp { get; internal set; }

    internal float FaintDelay;
    internal float CaptureDelay;

    /// <summary>
    /// The Substitute's doll standing in front of this place's Pokémon (plan 25 · Y1), or null when there is none and
    /// none is fading. Set by the log's lines (<c>SubstituteChanged</c>), never read from the rules.
    /// </summary>
    public SubstituteView? Substitute { get; internal set; }

    /// <summary>Whether a doll stands here (or is dropping in): what hides a mark on the Pokémon behind it.</summary>
    public bool BehindDoll => Substitute is { Up: true };

    /// <summary>
    /// What the Pokémon here shows for as long as it has it (plan 09 · L2): its status condition, a confusion, a
    /// love. Set by the log's lines (<see cref="ConditionLooks.Tell"/>), never read from the rules.
    /// </summary>
    public ConditionLook Condition { get; internal set; } = new();

    // A battle's answers (plan 25 · Y5; style guide, "Bodies that answer"), set by the log's lines, never the rules

    /// <summary>Seconds since a move that missed began to pass it (-1 = no dodge playing).</summary>
    public float DodgeAge { get; internal set; } = -1f;

    /// <summary>Where the move it dodges came from: the dodge goes away from the miss's path.</summary>
    public BattleSide DodgeFromSide { get; internal set; }
    public int DodgeFromSlot { get; internal set; }

    /// <summary>Seconds since it began to cheer (-1 = none).</summary>
    public float CheerAge { get; internal set; } = -1f;

    /// <summary>Seconds since its charge turn began, for as long as it lasts (-1 = not charging).</summary>
    public float ChargeAge { get; internal set; } = -1f;

    /// <summary>Seconds since one of its stats rose or fell, for the eyes (-1 = none lately), and which.</summary>
    public float StatAge { get; internal set; } = -1f;
    public bool StatRose { get; internal set; }

    /// <summary>0..1 how tired it shows: its HP bar in the red, eased in and out as the bar crosses.</summary>
    public float Tired { get; internal set; }

    // A Dynamax (plan 25 · Y15; style guide, "Dynamax, Max Moves and Terastallization"), set by the log's lines and
    // settled with the rules once a menu opens

    /// <summary>It is Dynamaxed, as the lines have told: it stands giant, its wreath over its head.</summary>
    public bool Dynamaxed { get; internal set; }

    /// <summary>Its Dynamax is a Gigantamax: gold in its wreath.</summary>
    public bool Gigantamax { get; internal set; }

    /// <summary>Seconds since it began to grow (<see cref="Dynamaxed"/>) or to shrink back (-1 = standing at its size).</summary>
    public float GiantAge { get; internal set; } = -1f;

    /// <summary>The turns its Dynamax has left, for its HP box (3, 2, 1; 0 when it isn't Dynamaxed).</summary>
    public int DynamaxTurnsLeft { get; internal set; }

    /// <summary>
    /// How long its trainer's call comes before its growth (plan 25 · YD1: <see cref="BattleAnimator.DynamaxCallTime"/>,
    /// or nought for a growth alone): <see cref="GiantAge"/> counts from the call, and the growth begins this far in.
    /// </summary>
    public float GiantLead { get; internal set; }

    /// <summary>
    /// Seconds since a Max Move struck it (plan 25 · YD2; -1 = none): its HP box jolts and the share it lost stays
    /// white (<see cref="GhostHp"/>) a moment before it drains.
    /// </summary>
    public float PunchAge { get; internal set; } = -1f;

    /// <summary>The HP the white ghost of the bar reaches: the bar's own as the Max Move struck, draining after a moment.</summary>
    public float GhostHp { get; internal set; }

    /// <summary>Its Max Guard went up this turn (plan 25 · YD3): a blow that comes to nothing on it rings the dome. Cleared when a menu settles.</summary>
    public bool MaxGuarded { get; internal set; }

    /// <summary>Seconds since its Dynamax pill began to tick down to a lower count (plan 25 · YD3; -1 = none), and the count it ticks from.</summary>
    public float TickAge { get; internal set; } = -1f;
    public int TickFrom { get; internal set; }

    /// <summary>Seconds since it began to fall as a giant that fainted (plan 25 · YD3; -1 = none): it sinks into its clouds before the faint plays.</summary>
    public float FallAge { get; internal set; } = -1f;
}

/// <summary>
/// A trainer's call into a swelling ball before their Pokémon grows giant (plan 25 · YD1; style guide, "The giant's
/// arrival"): who calls, from which side, and the Dynamax cue whose age times it. What it looks like at that age is
/// the renderer's (<c>BattleRenderer</c>), a function of the cue's age and the trainer's rig.
/// </summary>
public sealed class DynamaxCall
{
    public DynamaxCall(string look, BattleSide side, int slot, EffectCue cue)
    {
        Look = look;
        Side = side;
        Slot = slot;
        Cue = cue;
    }

    /// <summary>The trainer's look, as they stood on their platform.</summary>
    public string Look { get; }
    public BattleSide Side { get; }
    public int Slot { get; }

    /// <summary>The growth the call comes before: its <see cref="EffectCue.Age"/> is the call's clock.</summary>
    public EffectCue Cue { get; }

    /// <summary>Seconds since the call began.</summary>
    public float Age => Cue.Age;
}

/// <summary>
/// A Substitute's doll on one place (plan 25 · Y1; style guide, "The Substitute"): whether it stands, how long since
/// its last swap began, and the flinch and the step aside it is in the middle of. What it looks like at these ages is
/// <see cref="Graphics.SubstituteDoll.LookOf"/>, a pure function of them.
/// </summary>
public sealed class SubstituteView
{
    /// <summary>True while the doll stands (from the moment it drops in); false while it fades.</summary>
    public bool Up { get; internal set; }

    /// <summary>
    /// Seconds since it dropped in (<see cref="Up"/>) or began to fade. Below nought it waits: a doll passed by Baton
    /// Pass drops in once its new Pokémon is out of the ball.
    /// </summary>
    public float Age { get; internal set; }

    /// <summary>Seconds since a hit made it flinch (-1 = none).</summary>
    public float HitAge { get; internal set; } = -1f;

    /// <summary>Seconds since it began to step aside for its owner's own move (-1 = standing in front).</summary>
    public float AsideAge { get; internal set; } = -1f;

    /// <summary>From the animator's own count, never the battle's chance: varies the puff and the dust.</summary>
    public int Seed { get; internal set; }
}

/// <summary>What an effect cue shows: a move, something that happens to one Pokémon, the weather over the field, or a moment of the turn (plan 25 · Y3).</summary>
public enum CueKind { Move, StatUp, StatDown, Heal, Status, Seals, Weather, Moment, Mega, UltraBurst, ZPower, Dynamax, DynamaxEnd, Tera, Shiny }

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

    /// <summary>For <see cref="CueKind.Seals"/> (plan 06 · R17b): the Ball Capsule whose Seals burst as the Pokémon comes out of its ball.</summary>
    public Models.BallCapsule? Capsule { get; init; }

    /// <summary>For <see cref="CueKind.Weather"/> (plan 25 · Y1): the weather that begins or goes on.</summary>
    public Sim.BattleWeather Weather { get; init; }

    /// <summary>
    /// For <see cref="CueKind.Moment"/> (plan 25 · Y3): which moment of the turn. It plays on the Pokémon in the
    /// cue's "to" place; a moment between two places (Leech Seed) has its other end in the "from" place, and any
    /// other has both the same.
    /// </summary>
    public Sim.BattleMoment Moment { get; init; }

    /// <summary>For the held item's moment: the item that went off is a berry, eaten; anything else only glints.</summary>
    public bool Berry { get; init; }

    /// <summary>
    /// For <see cref="CueKind.Shiny"/> (plan 09 · L1): how long after the cue begins its sparkle does, while the
    /// Pokémon comes out of its ball (<see cref="BattleAnimator.ShinyFromBall"/>) or appears
    /// (<see cref="BattleAnimator.ShinyFromAppearing"/>). For <see cref="CueKind.Dynamax"/> (plan 25 · YD1): how
    /// long the trainer's call comes before the growth (<see cref="BattleAnimator.DynamaxCallTime"/>, or nought).
    /// </summary>
    public float Lead { get; init; }

    /// <summary>For <see cref="CueKind.Mega"/> (plan 25 · Y14): a Primal Reversion, in its own colour (<see cref="Type"/>: Water for Kyogre, Ground for Groudon).</summary>
    public bool Primal { get; init; }

    /// <summary>
    /// For <see cref="CueKind.Move"/> (plan 25 · Y14): a Z-Move, which plays its own set piece for
    /// <see cref="BattleAnimator.ZMoveTime"/>, landing at <see cref="BattleAnimator.ImpactTime"/> as every move does.
    /// </summary>
    public bool ZMove { get; init; }

    /// <summary>
    /// For <see cref="CueKind.Move"/> (plan 25 · Y15): a Max Move or a G-Max Move, which plays its own set piece for
    /// <see cref="BattleAnimator.MaxMoveTime"/>, landing at <see cref="BattleAnimator.ImpactTime"/>.
    /// </summary>
    public bool MaxMove { get; init; }

    /// <summary>For <see cref="CueKind.Dynamax"/> and <see cref="CueKind.DynamaxEnd"/> (plan 25 · Y15): a Gigantamax, with gold in its light.</summary>
    public bool Gigantamax { get; init; }

    /// <summary>For <see cref="CueKind.DynamaxEnd"/> (plan 25 · YD3): the giant fainted, and sinks into its clouds rather than shuddering where it stands.</summary>
    public bool Fell { get; init; }

    /// <summary>
    /// For <see cref="CueKind.Move"/> (plan 25 · YD3): a blow that came to nothing on a Pokémon whose Max Guard is up:
    /// it stops at the dome's surface and the dome rings (<see cref="CombatantView.MaxGuarded"/>).
    /// </summary>
    public bool OnGuard { get; init; }

    /// <summary>For <see cref="CueKind.Tera"/> (plan 25 · Y15): a Stellar Terastallization, in seven colours turning (<see cref="Type"/> is then the Pokémon's own).</summary>
    public bool Stellar { get; init; }

    /// <summary>
    /// For <see cref="CueKind.Move"/>: an attack sent ahead landing (Future Sight, Doom Desire), which plays its
    /// move's landing half from the place it was sent from, where nobody need stand any more.
    /// </summary>
    public bool Arriving { get; init; }

    /// <summary>
    /// For <see cref="CueKind.Move"/>: a move's charge half (plan 25 · Y5), played on its user through the charge
    /// turn: an aura of its type gathering round it. Each charge move's own comes with its batch of Y6 to Y10.
    /// </summary>
    public bool Charging { get; init; }

    /// <summary>
    /// For <see cref="CueKind.Move"/>: the ground the battle is fought on (plan 25 · Y6), for the looks that go by it:
    /// Secret Power's by what it does there, Camouflage's by the type it takes.
    /// </summary>
    public Sim.BattleTerrain Terrain { get; init; }

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

    /// <summary>
    /// The age the cue is drawn at (plan 25 · Y17's hit-stop): <see cref="Age"/> itself, but for a heavy signature
    /// move's landed strike, which holds at the impact a moment and then catches up to end on time
    /// (<see cref="Graphics.Directions.Remap"/>). The animator's clock and the line's impact keep <see cref="Age"/>.
    /// </summary>
    public float ShownAge => Graphics.Directions.ShownAge(this);

    /// <summary>A move the user aims at itself (Swords Dance, Harden).</summary>
    public bool OnSelf => FromSide == ToSide && FromSlot == ToSlot;

    /// <summary>The move hit and did something.</summary>
    public bool Landed => !Missed && !Blocked;

    /// <summary>How long this cue plays.</summary>
    public float Length => Kind switch
    {
        CueKind.Move => ZMove ? BattleAnimator.ZMoveTime : MaxMove ? BattleAnimator.MaxMoveTime : BattleAnimator.EffectTime,
        CueKind.Dynamax => Lead + BattleAnimator.DynamaxTime,
        CueKind.DynamaxEnd => BattleAnimator.DynamaxEndTime,
        CueKind.Tera => BattleAnimator.TeraTime,
        CueKind.Mega => BattleAnimator.MegaTime,
        CueKind.UltraBurst => BattleAnimator.UltraBurstTime,
        CueKind.ZPower => BattleAnimator.ZPowerTime,
        CueKind.Seals => BattleAnimator.SealTime,
        CueKind.Shiny => Lead + BattleAnimator.ShinyTime,
        CueKind.Weather => BattleAnimator.WeatherCueTime,
        CueKind.Moment => BattleAnimator.MomentTime,
        _ => BattleAnimator.MarkTime
    };

    /// <summary>A mark on one Pokémon: a stat's change, a heal, a condition given (what a doll hides).</summary>
    public bool OnOnePokemon => Kind is CueKind.StatUp or CueKind.StatDown or CueKind.Heal or CueKind.Status;
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

    /// <summary>A Ball Capsule's Seals: from the throw to the last piece of the last Seal (plan 06 · R17b; <see cref="Graphics.SealFx.Length"/> after the ball opens).</summary>
    public const float SealTime = SendOutBallTime + Graphics.SealFx.Length;

    /// <summary>A shiny Pokémon's sparkle (plan 09 · L1; style guide, "Shiny Pokémon"): 1.1 s from its first stars to its last.</summary>
    public const float ShinyTime = 1.1f;

    /// <summary>The sparkle begins once a Pokémon sent out has grown out of its ball…</summary>
    public const float ShinyFromBall = SendOutTime;

    /// <summary>…or half a second after a wild one appears.</summary>
    public const float ShinyFromAppearing = 0.5f;
    // A physical strike and a special move's release peak about when the damage lands (the engine's hit delay)
    public const float AttackTime = 0.65f;
    public const float HitTime = 0.55f;
    public const float FaintTime = 1.0f;
    public const float RecallTime = 0.9f;
    public const float CaptureTime = 0.45f;
    public const float TrainerExitTime = 0.9f;

    /// <summary>How long a move's effect plays; it lands <see cref="ImpactTime"/> in, when the damage does.</summary>
    public const float EffectTime = 1.3f;
    public const float ImpactTime = 0.35f;

    /// <summary>
    /// The later mechanics' set pieces (plan 25 · Y14; style guide, "Mega Evolution, Ultra Burst and Z-Moves"): a
    /// Mega Evolution plays this long and changes the Pokémon's shape <see cref="MegaSwapTime"/> in, inside its light;
    /// an Ultra Burst the same at its own times; the Z-Power's frame stands before its Z-Move, which lands at
    /// <see cref="ImpactTime"/> and grows for a second after.
    /// </summary>
    public const float MegaTime = 1.6f;
    public const float MegaSwapTime = 0.8f;
    public const float UltraBurstTime = 1.8f;
    public const float UltraBurstSwapTime = 0.85f;
    public const float ZPowerTime = 1.4f;
    public const float ZMoveTime = 1.5f;

    /// <summary>
    /// Dynamax and Terastallization (plan 25 · Y15; style guide, "Dynamax, Max Moves and Terastallization"): a Dynamax
    /// plays this long, grows the body from <see cref="DynamaxGrowStart"/> to <see cref="DynamaxGrown"/> and takes a
    /// Gigantamax form <see cref="DynamaxSwapTime"/> in; its end shrinks it back over <see cref="DynamaxShrinkTime"/>,
    /// the form going back <see cref="DynamaxEndSwapTime"/> in; a Max Move plays as long as a Z-Move; a
    /// Terastallization's shell shatters, and a Terapagos takes its form, <see cref="TeraSwapTime"/> in.
    /// </summary>
    public const float DynamaxTime = 1.6f;
    public const float DynamaxGrowStart = 0.3f;
    public const float DynamaxGrown = 0.9f;
    public const float DynamaxSwapTime = 0.6f;
    public const float DynamaxEndTime = 1.0f;

    /// <summary>
    /// The fall (plan 25 · YD3): an end shudders to <see cref="DynamaxFallStart"/>, falls in to
    /// <see cref="DynamaxFallLow"/> of its own size by <see cref="DynamaxShrunk"/>, and is back at its own size by
    /// <see cref="DynamaxShrinkTime"/>; its form goes back <see cref="DynamaxEndSwapTime"/> in.
    /// </summary>
    public const float DynamaxFallStart = 0.3f;
    public const float DynamaxShrunk = 0.75f;
    public const float DynamaxShrinkTime = 0.9f;
    public const float DynamaxFallLow = 0.95f;
    public const float DynamaxEndSwapTime = 0.5f;

    /// <summary>A giant that faints sinks this share of its height into its clouds, over this long, before the faint plays (plan 25 · YD3).</summary>
    public const float GiantFallSink = 0.25f, GiantFallTime = 0.45f;

    /// <summary>A Max Move's punch on the HP box (plan 25 · YD2): the box's jolt, and how long the lost share stays white before it drains.</summary>
    public const float PunchTime = 0.3f, GhostHold = 0.3f;

    /// <summary>How long the Dynamax pill takes to tick down (plan 25 · YD3).</summary>
    public const float TickTime = 0.4f;
    public const float MaxMoveTime = 1.5f;
    public const float TeraTime = 1.7f;
    public const float TeraSwapTime = 0.9f;

    /// <summary>How much bigger a Dynamaxed Pokémon stands: on the foe's platform, and on the player's own, which is near the lens.</summary>
    public const float DynamaxScaleFoe = 1.25f, DynamaxScaleOwn = 1.3f;

    /// <summary>
    /// The trainer's call before a growth (plan 25 · YD1; style guide, "The giant's arrival"): how long it comes first,
    /// when the Pokémon is drawn back into red light (from <see cref="CallDrawStart"/> to <see cref="CallDrawEnd"/>),
    /// when the ball in the hand has swollen (<see cref="CallSwellEnd"/>), when it is thrown (<see cref="CallThrow"/>),
    /// how long the Pokémon takes to come back to its own size out of the opened ball (<see cref="CallEmerge"/>, after
    /// the call) and when the trainer has walked off (<see cref="CallerGone"/>, from the call's start).
    /// </summary>
    public const float DynamaxCallTime = 1.0f;
    public const float CallDrawStart = 0.05f, CallDrawEnd = 0.35f, CallSwellEnd = 0.65f, CallThrow = 0.7f, CallEmerge = 0.3f, CallerGone = 1.4f;

    /// <summary>The trainer's call before a growth now playing, or null (plan 25 · YD1).</summary>
    public DynamaxCall? Call { get; private set; }

    /// <summary>How many Pokémon have Dynamaxed in this battle: only the first is called (plan 25 · YD1, question 3).</summary>
    public int Dynamaxes { get; private set; }

    /// <summary>
    /// Whose call a Dynamax here would have (plan 25 · YD1): the trainer's look, or null for a growth alone. Only the
    /// first Dynamax of a battle, in a single battle, never at the Frontier, about a Pokémon that stands in sight (not
    /// behind a doll, not out of sight, not on its way in or out), and only where the side has a trainer to call it.
    /// </summary>
    public string? CallerFor(BattleSide side, int slot, bool frontier)
    {
        if (frontier || Dynamaxes > 0 || SlotsOf(BattleSide.Player) != 1 || SlotsOf(BattleSide.Enemy) != 1) return null;
        var v = this[side, slot];
        if (v.Shown == null || !v.Present || v.Away || v.BehindDoll || v.SendOutAge >= 0f || v.FaintAge >= 0f || v.RecallAge >= 0f) return null;
        return side == BattleSide.Player ? PlayerTrainerLook : EnemyTrainerLook;
    }

    /// <summary>How long a stat change, a heal or a status condition shows on its Pokémon.</summary>
    public const float MarkTime = 1.1f;

    /// <summary>
    /// How long a moment of the turn plays (plan 25 · Y3): as long as any other mark on a Pokémon. It lands
    /// <see cref="ImpactTime"/> in, which is when the HP of a moment that hurts or heals begins to move.
    /// </summary>
    public const float MomentTime = MarkTime;

    /// <summary>
    /// The weather in battle (plan 25 · Y1): its cue plays this long as it begins and on each turn it goes on, and
    /// the layer that lasts eases in and out over <see cref="WeatherFadeTime"/>.
    /// </summary>
    public const float WeatherCueTime = 1.2f;
    public const float WeatherFadeTime = 1.0f;

    /// <summary>
    /// The doll's swaps (plan 25 · Y1): the Pokémon shrinks out as the doll drops in; the doll fades in a puff as the
    /// Pokémon grows back; a hit's flinch; a step aside for its owner's own move, held while the move plays.
    /// </summary>
    public const float SubstituteInTime = 0.7f;
    public const float SubstituteOutTime = 0.6f;
    public const float SubstituteHitTime = 0.45f;
    public const float SubstituteAsideTime = EffectTime + 0.2f;

    /// <summary>
    /// What lies on the field (plan 25 · Y2): a hazard, a screen, a room or something sent ahead comes in over
    /// <see cref="FieldArriveTime"/> from the line that tells of it, and fades over <see cref="FieldFadeTime"/> on
    /// the line that says it is gone.
    /// </summary>
    public const float FieldArriveTime = 0.6f;
    public const float FieldFadeTime = 0.5f;

    /// <summary>
    /// A condition that lasts on a Pokémon (plan 09 · L2): its mark eases in over <see cref="ConditionArriveTime"/>
    /// from the line that tells of it and out over <see cref="ConditionFadeTime"/> on the line that ends it; and
    /// while a moment or a mark plays on the Pokémon, the marks that last stand back, going over
    /// <see cref="ConditionStandBackTime"/> and returning over the cue's last <see cref="ConditionReturnTime"/>.
    /// </summary>
    public const float ConditionArriveTime = 0.4f;
    public const float ConditionFadeTime = 0.4f;
    public const float ConditionStandBackTime = 0.15f;
    public const float ConditionReturnTime = 0.25f;

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

    /// <summary>When the ball's wobble <paramref name="wobble"/> (0-based) begins, from the throw.</summary>
    public static float BallWobbleAt(int wobble) => BallArriveTime + BallOpenTime + BallDropTime + (BallWobbleTime + BallPauseTime) * wobble;

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

    /// <summary>
    /// The most places a side of a battle has: five, a horde's wild side (plan 14 · B4). Every look kept for a place
    /// (the views, what lasts on each Pokémon, what waits for it, the effects' places) is sized by it.
    /// </summary>
    public const int MostSlots = 5;

    private readonly CombatantView[,] views = new CombatantView[2, MostSlots];
    private readonly int[] slotCounts = { 1, 1 };

    /// <summary>The share of its HP at which a Pokémon is in the red: the bar's colour and the music's low-HP arrangement.</summary>
    public const float LowHpRatio = 0.2f;

    // A battle's answers (plan 25 · Y5; style guide, "Bodies that answer")

    /// <summary>Seconds of a dodge, furthest aside at <see cref="ImpactTime"/>, where a miss passes.</summary>
    public const float DodgeTime = 0.8f;

    /// <summary>Seconds of a cheer: two hops.</summary>
    public const float CheerTime = 1.2f;

    /// <summary>Seconds a charge takes to gather the body.</summary>
    public const float ChargeInTime = 0.4f;

    /// <summary>Seconds a stat's change shows in the eyes: squeezed for a fall, fierce for a rise.</summary>
    public const float StatEyesTime = 0.6f;

    /// <summary>Seconds tiredness takes to come on or go as the bar crosses into or out of the red.</summary>
    public const float TiredEaseTime = 0.6f;

    /// <summary>
    /// Pokémon on the player's side: 1 in a single battle and a horde, 2 in a double. Setting it sets both sides
    /// alike, as a single and a double have them; a horde's are set by <see cref="SetSlots"/>.
    /// </summary>
    public int Slots
    {
        get => slotCounts[0];
        internal set => SetSlots(value, value);
    }

    /// <summary>How many places a side has (plan 14 · B4): a horde's wild side five, or as many as came.</summary>
    public int SlotsOf(BattleSide side) => slotCounts[(int)side];

    /// <summary>The places each side has, as the battle's rules made them (<see cref="Battle.BattleFormats.PlacesOf"/>).</summary>
    internal void SetSlots(int player, int enemy)
    {
        slotCounts[0] = Math.Clamp(player, 1, MostSlots);
        slotCounts[1] = Math.Clamp(enemy, 1, MostSlots);
    }

    /// <summary>A place's slot kept within the places a look is kept for.</summary>
    internal static int Within(int slot) => Math.Clamp(slot, 0, MostSlots - 1);

    /// <summary>The first (or only) place on each side.</summary>
    public CombatantView Player => views[0, 0];
    public CombatantView Enemy => views[1, 0];

    /// <summary>Character type standing on each platform before the Pokémon are sent out (null = none).</summary>
    public string? PlayerTrainer { get => playerTrainer; internal set { playerTrainer = value; if (value != null) PlayerTrainerLook = value; } }
    private string? playerTrainer = PlayerIdentity.Character;

    /// <summary>The player's look, kept once they have stepped off their platform, so they can come back to call a Dynamax (plan 25 · YD1).</summary>
    public string? PlayerTrainerLook { get; private set; } = PlayerIdentity.Character;
    public string? EnemyTrainer { get => enemyTrainer; internal set { enemyTrainer = value; if (value != null) EnemyTrainerLook = value; } }
    private string? enemyTrainer;

    /// <summary>A second opposing trainer standing beside the first (two trainers battling together).</summary>
    public string? EnemyTrainer2 { get => enemyTrainer2; internal set { enemyTrainer2 = value; if (value != null) EnemyTrainer2Look = value; } }
    private string? enemyTrainer2;

    /// <summary>
    /// The looks of the trainers across the field, kept once they have stepped off their platform (when
    /// <see cref="EnemyTrainer"/> and <see cref="EnemyTrainer2"/> are cleared), so that one who speaks can step back
    /// (plan 11 · C13).
    /// </summary>
    public string? EnemyTrainerLook { get; private set; }
    public string? EnemyTrainer2Look { get; private set; }

    /// <summary>Seconds a trainer takes to step back onto their platform to speak, and to step off again once their line is read (plan 11 · C13).</summary>
    public const float TrainerBackTime = 0.4f;

    /// <summary>The trainer across the field who is back on their platform to say a line, or on their way there or off again; null for none (plan 11 · C13).</summary>
    public SpeakingTrainer? Speaking { get; private set; }

    /// <summary>
    /// A trainer across the field steps back onto their platform to say a line (plan 11 · C13), with the line's face
    /// and gesture: the first or only trainer (<paramref name="second"/> false) or the second of two, alone. Nothing
    /// while they still stand there, or with no look kept for them. One already back stays, and takes the new face.
    /// </summary>
    public void StepBack(bool second, Overworld.Mood mood, Overworld.Gesture? gesture)
    {
        string? look = second ? EnemyTrainer2Look : EnemyTrainerLook;
        if (look == null || (second ? EnemyTrainer2 : EnemyTrainer) != null) return;
        if (Speaking is { } back && back.Second == second && back.Look == look)
        {
            // Turned round on the way off: as far back as they had come, walking on from there
            back.Age = back.Back * TrainerBackTime;
            back.LeaveAge = -1f;
            back.Mood = mood;
            back.Gesture = gesture;
            return;
        }
        Speaking = new SpeakingTrainer(look, second) { Mood = mood, Gesture = gesture };
    }

    /// <summary>The trainer who spoke has had their line read and steps off their platform again (plan 11 · C13).</summary>
    public void SendOffSpeaker()
    {
        if (Speaking is { LeaveAge: < 0f } back) back.LeaveAge = 0f;
    }

    /// <summary>
    /// Whoever battles beside the player, standing on the platform with them until the Pokémon are sent out (a tag
    /// battle, plan 06 · R16c); they step off as the player does.
    /// </summary>
    public string? PartnerTrainer { get; internal set; }

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

    /// <summary>
    /// Two places of a side change Pokémon (Ally Switch, plan 06 · R25's leftovers): each view goes with its Pokémon,
    /// its HP box, its doll and what it is in the middle of. The marks that last are moved by
    /// <see cref="ConditionLooks.Tell"/>, and each view takes its new place's look.
    /// </summary>
    public void SwapPlaces(BattleSide side, int first, int second)
    {
        int s = (int)side;
        (views[s, first], views[s, second]) = (views[s, second], views[s, first]);
        views[s, first].Condition = Conditions.Of(side, first);
        views[s, second].Condition = Conditions.Of(side, second);
    }

    // ------------------------------------------------------------------ the weather (plan 25 · Y1)

    /// <summary>The weather in the air over the stage, as the lines have told of it (none: a clear sky).</summary>
    public Sim.BattleWeather Weather { get; private set; }

    /// <summary>Seconds since <see cref="Weather"/> began to show (its layer eases in over <see cref="WeatherFadeTime"/>).</summary>
    public float WeatherAge { get; private set; }

    /// <summary>The weather that is clearing away, and for how long it has been (-1: none is).</summary>
    public Sim.BattleWeather FadingWeather { get; private set; }
    public float FadingAge { get; private set; } = -1f;

    /// <summary>How far the weather's layer and light have come in, 0..1.</summary>
    public float WeatherStrength => Weather == Sim.BattleWeather.None ? 0f : Math.Clamp(WeatherAge / WeatherFadeTime, 0f, 1f);

    /// <summary>How much of the weather that is clearing is still there, 0..1.</summary>
    public float FadingStrength => FadingAge < 0f ? 0f : Math.Clamp(1f - FadingAge / WeatherFadeTime, 0f, 1f);

    /// <summary>A weather begins: its cue plays and its layer comes in (a weather already in the air is taken over at once).</summary>
    public EffectCue WeatherBegins(Sim.BattleWeather weather)
    {
        SetSky(weather, instantly: false);
        return Cue(new EffectCue { Kind = CueKind.Weather, Weather = weather });
    }

    /// <summary>The weather goes on at the end of a turn: its cue plays again over the layer that lasts.</summary>
    public EffectCue WeatherGoesOn(Sim.BattleWeather weather)
    {
        if (Weather != weather) SetSky(weather, instantly: true);
        return Cue(new EffectCue { Kind = CueKind.Weather, Weather = weather });
    }

    /// <summary>
    /// A weather begins with the battle's scene off (plan 08 · P1): its layer comes in as ever, and no cue plays over it.
    /// </summary>
    public void WeatherQuietly(Sim.BattleWeather weather) => SetSky(weather, instantly: false);

    /// <summary>The weather stops: its layer eases away.</summary>
    public void WeatherClears() => SetSky(Sim.BattleWeather.None, instantly: false);

    /// <summary>The sky as the rules have it once a menu opens, with no cue (a battle the screen fell behind).</summary>
    public void SettleWeather(Sim.BattleWeather weather)
    {
        if (Weather != weather) SetSky(weather, instantly: true);
    }

    private void SetSky(Sim.BattleWeather weather, bool instantly)
    {
        if (weather == Weather) return;
        if (Weather != Sim.BattleWeather.None && !instantly)
        {
            FadingWeather = Weather;
            // What had not finished easing in eases out from where it stood
            FadingAge = (1f - WeatherStrength) * WeatherFadeTime;
        }
        else if (instantly)
        {
            FadingWeather = Sim.BattleWeather.None;
            FadingAge = -1f;
        }
        Weather = weather;
        WeatherAge = instantly ? WeatherFadeTime : 0f;
    }

    // ------------------------------------------------------------------ what lies on the field (plan 25 · Y2)

    /// <summary>
    /// Each side's screens, veils and hazards, Trick Room and Gravity, and what waits for each place, as the lines
    /// have told of them: the engine sets it from the log's events and settles it with the rules once a menu opens.
    /// </summary>
    public FieldLook FieldLook { get; } = new();

    // ------------------------------------------------------------------ a condition that lasts (plan 09 · L2)

    /// <summary>
    /// What each Pokémon shows for as long as it has it, as the lines have told of it: a status condition, a
    /// confusion, a love. The engine hands it the log's events and settles it with the rules once a menu opens;
    /// each place's look is also its view's <see cref="CombatantView.Condition"/>.
    /// </summary>
    public ConditionLooks Conditions { get; } = new();

    public BattleAnimator()
    {
        for (int side = 0; side < 2; side++)
            for (int slot = 0; slot < MostSlots; slot++)
                views[side, slot] = new CombatantView { Condition = Conditions.Of((BattleSide)side, slot) };
    }

    /// <summary>
    /// How much of the marks that last about the Pokémon in a place is drawn, 0..1 (style guide, "A condition that
    /// lasts"). None about a Pokémon that can't be seen, as for every other mark on one (<see cref="Shows"/>, the
    /// original's <c>BattleSystem_ShouldShowStatusEffect</c>): behind a doll, out of sight in the middle of a move,
    /// still in its ball, fainting, being called back or drawn into a ball. And they stand back while a moment of
    /// the turn or a mark given plays on that Pokémon, so the two never fight over it. The body's own (shut eyes,
    /// the hold, the cast) isn't asked here: it shows wherever the body does.
    /// </summary>
    public float LastingShown(BattleSide side, int slot)
    {
        var v = this[side, Within(slot)];
        if (v.Shown == null || !v.Present || v.Away || v.BehindDoll) return 0f;
        if (v.FaintAge >= 0f || v.RecallAge >= 0f || (v.CaptureAge >= 0f && v.CaptureDelay <= 0f)) return 0f;
        float shown = 1f;
        // Out of its ball first: they come with the last of its growing
        if (v.SendOutAge >= 0f) shown = Math.Clamp(Emerge(v.SendOutAge) * 3f - 2f, 0f, 1f);
        // A doll that fades lets them back as its Pokémon grows back
        if (v.Substitute is { Up: false } fading) shown *= Math.Clamp(fading.Age / SubstituteOutTime, 0f, 1f);
        foreach (var cue in cues)
        {
            if (cue.Kind is not (CueKind.Moment or CueKind.Status or CueKind.StatUp or CueKind.StatDown or CueKind.Heal
                or CueKind.Dynamax or CueKind.DynamaxEnd or CueKind.Tera)) continue;
            if (cue.ToSide != side || cue.ToSlot != slot) continue;
            float back = Math.Min(cue.Age / ConditionStandBackTime, (cue.Length - cue.Age) / ConditionReturnTime);
            shown *= 1f - Math.Clamp(back, 0f, 1f);
        }
        return shown;
    }

    // ------------------------------------------------------------------ the Substitute (plan 25 · Y1)

    private int dollCount;

    /// <summary>A doll is put up in a place: the Pokémon shrinks out of sight as the doll drops in.</summary>
    public SubstituteView SubstituteUp(BattleSide side, int slot = 0)
    {
        var v = this[side, slot];
        var doll = new SubstituteView { Up = true, Seed = ++dollCount * 4099 + slot * 13 + (int)side };
        // A doll passed by Baton Pass waits for its new Pokémon to come out of the ball
        if (v.SendOutAge >= 0f) doll.Age = -(SendOutTime - v.SendOutAge);
        v.Substitute = doll;
        return doll;
    }

    /// <summary>The doll is gone: it fades in a puff and the Pokémon grows back.</summary>
    public void SubstituteDown(BattleSide side, int slot = 0)
    {
        var v = this[side, slot];
        if (v.Substitute is not { Up: true } doll) return;
        doll.Up = false;
        doll.Age = 0f;
        doll.HitAge = doll.AsideAge = -1f;
    }

    /// <summary>A hit lands on the doll: it flinches.</summary>
    public void SubstituteHit(BattleSide side, int slot = 0)
    {
        if (this[side, slot].Substitute is { Up: true } doll) doll.HitAge = 0f;
    }

    /// <summary>Its owner uses a move: the doll steps aside, and back once the move has played.</summary>
    public void SubstituteAside(BattleSide side, int slot = 0)
    {
        if (this[side, slot].Substitute is { Up: true } doll) doll.AsideAge = 0f;
    }

    /// <summary>The doll as the rules have it once a menu opens: standing or not, with no swap played.</summary>
    public void SettleSubstitute(BattleSide side, int slot, bool up)
    {
        var v = this[side, slot];
        if (v.BehindDoll == up) return;
        v.Substitute = up ? new SubstituteView { Up = true, Age = SubstituteInTime, Seed = ++dollCount * 4099 + slot * 13 + (int)side } : null;
    }

    /// <summary>
    /// Whether a cue shows on its Pokémon, as the original's <c>BattleSystem_ShouldShowStatusEffect</c> decides: on a
    /// Pokémon behind a doll or out of sight only the weather (and the doll's own swaps, which are no cue) shows; a
    /// stat's change, a heal or a condition given is hidden. A move's own effect always plays: it lands on the doll.
    /// </summary>
    public bool Shows(EffectCue cue)
    {
        if (cue.Kind == CueKind.Moment)
        {
            // The escape item is among the effects the original always shows, and a level's is played without the
            // check (SEQ_GET_EXP_CHECK_LEVEL_UP); one between two places plays only if both can be seen
            // (BtlCmd_PlayBattleAnimationOnMons)
            if (cue.Moment is Sim.BattleMoment.EscapeItem or Sim.BattleMoment.LevelUp) return true;
            return InSight(cue.ToSide, cue.ToSlot) && (cue.OnSelf || InSight(cue.FromSide, cue.FromSlot));
        }
        if (!cue.OnOnePokemon) return true;
        return InSight(cue.ToSide, cue.ToSlot);
    }

    /// <summary>The Pokémon in a place is neither behind a doll nor out of sight in the middle of a move.</summary>
    private bool InSight(BattleSide side, int slot)
    {
        var v = this[side, Within(slot)];
        return !v.BehindDoll && !v.Away;
    }

    /// <summary>A Pokémon already on the field when the battle opens (wild encounters).</summary>
    public void Appear(BattleSide side, Pokemon pokemon, int slot = 0)
    {
        var v = this[side, slot];
        Reset(v, pokemon);
        v.Present = true;
    }

    /// <summary>
    /// An ally that answered a call for help comes onto its place (plan 14 · B5; style guide, "Stage", "An ally answers"):
    /// it hops in from behind the platform and lands on its place over <see cref="ArriveTime"/>, and the first to come
    /// opens its side out from one place to two as it lands. With the scene off (<paramref name="hop"/> false) it is there at once.
    /// </summary>
    public void Arrive(BattleSide side, Pokemon pokemon, int slot = 0, bool hop = true)
    {
        var v = this[side, slot];
        Reset(v, pokemon);
        v.Present = true;
        v.ArriveAge = hop ? 0f : -1f;
        int s = (int)side;
        if (spread[s] < 1f && !spreading[s])
        {
            spreading[s] = true;
            if (!hop) spread[s] = 1f;
        }
    }

    /// <summary>Seconds of an ally's hop onto its place (plan 14 · B5).</summary>
    public const float ArriveTime = 0.7f;

    // How far each side is opened out to its places: 1 for every battle but an SOS battle's wild side, which stands as
    // one place until its first ally comes (plan 14 · B5)
    private readonly float[] spread = { 1f, 1f };
    private readonly bool[] spreading = new bool[2];

    /// <summary>How far a side stands opened out to its places (0: all on its one middle place; 1: on their own).</summary>
    public float Spread(BattleSide side) => spread[(int)side];

    /// <summary>A side stands as one place until an ally comes (<see cref="Arrive"/>): an SOS battle's wild side (plan 14 · B5).</summary>
    internal void Gather(BattleSide side)
    {
        spread[(int)side] = 0f;
        spreading[(int)side] = false;
    }

    // ---------------------------------------------------------------- a rotation battle's turntables (plan 14 · B7)

    /// <summary>Each side's platform is a turntable of three seats: a rotation battle (style guide, "Stage", "A rotation's turntable").</summary>
    public bool Turntables { get; internal set; }

    /// <summary>Seconds of a turntable's turn: the Pokémon brought forward hops to the front as the table turns a third.</summary>
    public const float RotationTime = 0.7f;

    private readonly float[] rotationAge = { -1f, -1f };
    private readonly int[] rotationWay = new int[2];
    private readonly int[] turned = new int[2];

    /// <summary>Seconds since a side's turntable began to turn (-1 = standing).</summary>
    public float RotationAge(BattleSide side) => rotationAge[(int)side];

    /// <summary>The way a side's turntable turned last: +1 or −1 (0 before it ever turned).</summary>
    public int RotationWay(BattleSide side) => rotationWay[(int)side];

    /// <summary>
    /// How far a side's turntable has turned, in thirds of a round from where it stood as the battle opened: whole once a
    /// turn is over, eased between while one plays. A turn of +1 adds one.
    /// </summary>
    public float TurntableTurn(BattleSide side)
    {
        int s = (int)side;
        float age = rotationAge[s];
        if (age < 0f) return turned[s];
        float t = Math.Clamp(age / RotationTime, 0f, 1f);
        float eased = t * t * (3f - 2f * t);
        return turned[s] - rotationWay[s] * (1f - eased);
    }

    /// <summary>
    /// A rotation battle's turntable turns <paramref name="way"/> (plan 14 · B7): the Pokémon brought forward stands at the
    /// front, hopping on from its seat behind over the turn as an ally answering a call hops in. With the scene off
    /// (<paramref name="scene"/> false) it is there at once and the table stands turned.
    /// </summary>
    public void Rotate(BattleSide side, Pokemon pokemon, int way, bool scene = true)
    {
        var v = this[side, 0];
        Reset(v, pokemon);
        v.Present = true;
        v.ArriveAge = scene ? 0f : -1f;
        int s = (int)side;
        turned[s] += Math.Sign(way);
        rotationWay[s] = Math.Sign(way);
        rotationAge[s] = scene ? 0f : -1f;
    }

    /// <summary>Thrown out of a Poké Ball: the trainer throws it and steps aside, and the Pokémon bursts out as it opens.</summary>
    public void SendOut(BattleSide side, Pokemon pokemon, int slot = 0)
    {
        var v = this[side, slot];
        Reset(v, pokemon);
        v.Present = true;
        v.SendOutAge = 0f;
        v.Ball = Graphics.BattleBall.NameFor(pokemon);
        if (side == BattleSide.Player && PlayerTrainerExit < 0f) PlayerTrainerExit = 0f;
        if (side == BattleSide.Enemy && EnemyTrainer != null && EnemyTrainerExit < 0f) EnemyTrainerExit = 0f;
    }

    public void Attack(BattleSide side, int slot = 0, MoveCategory category = MoveCategory.Physical)
    {
        var v = this[side, slot];
        v.AttackAge = 0f;
        v.AttackCategory = category;
        // A move let go ends the charge that got it ready (plan 25 · Y5)
        v.ChargeAge = -1f;
    }

    /// <param name="strength">How hard the camera shakes: 1 for a hit, up to 2 for a critical or super-effective one.</param>
    public void Hit(BattleSide side, int slot = 0, float strength = 1f)
    {
        this[side, slot].HitAge = 0f;
        ShakeAge = 0f;
        ShakeStrength = strength;
    }

    /// <summary>
    /// Starts an effect; it plays for <see cref="EffectCue.Length"/> seconds. A mark on a Pokémon that can't be seen
    /// (behind a doll, out of sight) isn't started (<see cref="Shows"/>), though it is counted and seeded as ever.
    /// </summary>
    public EffectCue Cue(EffectCue cue)
    {
        cue.Seed = ++cueCount * 7919 + cue.Move.Length;
        cue.Age = 0f;
        if (Shows(cue)) cues.Add(cue);
        // A move that spends its user collapses it until it faints (plan 25 · Y16)
        if (cue.Kind == CueKind.Move && !cue.Charging && !cue.Arriving && Graphics.BodyMotions.SpendsTheUser(cue.Move))
            this[cue.FromSide, Within(cue.FromSlot)].SpentAge = 0f;
        return cue;
    }

    /// <summary>
    /// The Pokémon here goes out of sight for its move's turn (plan 25 · Y16): it is drawn leaving, up, under the
    /// ground or into the dark by <paramref name="move"/>, and then not at all.
    /// </summary>
    public void GoAway(BattleSide side, int slot, string move)
    {
        var v = this[side, slot];
        v.Away = true;
        v.AwayAge = 0f;
        v.BackAge = -1f;
        if (move.Length > 0) v.AwayMove = move;
    }

    /// <summary>The Pokémon here comes back into sight: drawn coming back the way it went.</summary>
    public void ComeBack(BattleSide side, int slot)
    {
        var v = this[side, slot];
        if (!v.Away) return;
        v.Away = false;
        v.AwayAge = -1f;
        v.BackAge = 0f;
    }

    /// <summary>A stat going up or down on one Pokémon.</summary>
    public EffectCue StatChange(BattleSide side, int slot, bool up)
    {
        // The eyes answer it for a moment (plan 25 · Y5)
        var v = this[side, slot];
        v.StatAge = 0f;
        v.StatRose = up;
        return Cue(new EffectCue { Kind = up ? CueKind.StatUp : CueKind.StatDown, FromSide = side, FromSlot = slot, ToSide = side, ToSlot = slot });
    }

    /// <summary>HP restored to one Pokémon.</summary>
    public EffectCue Heal(BattleSide side, int slot) =>
        Cue(new EffectCue { Kind = CueKind.Heal, FromSide = side, FromSlot = slot, ToSide = side, ToSlot = slot, Type = PokemonType.Grass });

    /// <summary>A status condition given to one Pokémon.</summary>
    public EffectCue StatusGiven(BattleSide side, int slot, StatusCondition status) =>
        Cue(new EffectCue { Kind = CueKind.Status, Status = status, FromSide = side, FromSlot = slot, ToSide = side, ToSlot = slot });

    /// <summary>
    /// A moment of the turn on one Pokémon (plan 25 · Y3): what the turn's end takes or gives, a condition as it
    /// plays on its turn, an item going off, a level. <paramref name="from"/> is the other end of a moment between
    /// two places (Leech Seed's seeder). It isn't started on a Pokémon that can't be seen (<see cref="Shows"/>).
    /// </summary>
    public EffectCue Moment(BattleSide side, int slot, Sim.BattleMoment moment, (BattleSide Side, int Slot)? from = null, bool berry = false)
    {
        var cue = new EffectCue
        {
            Kind = CueKind.Moment, Moment = moment, Berry = berry,
            FromSide = from?.Side ?? side, FromSlot = from?.Slot ?? slot, ToSide = side, ToSlot = slot,
            // The moments count for themselves, so a battle's moves keep the seeds they had before there were any
            Seed = ++momentCount * 6007 + (int)moment * 131
        };
        if (Shows(cue)) cues.Add(cue);
        return cue;
    }

    private int momentCount;

    /// <summary>
    /// A Mega Evolution, a Primal Reversion or an Ultra Burst on the Pokémon here (plan 25 · Y14): its light gathers,
    /// its body turns to light and its shape changes inside it (the engine hands the new form over
    /// <see cref="MegaSwapTime"/> or <see cref="UltraBurstSwapTime"/> in). Seeded as the move cue it stands in for was,
    /// so a battle's later cues keep the seeds they had under the stand-in.
    /// </summary>
    public EffectCue Change(BattleSide side, int slot, CueKind kind, bool primal = false, PokemonType type = PokemonType.Normal) =>
        Cue(new EffectCue { Kind = kind, Primal = primal, Type = type, Move = "Transform", FromSide = side, FromSlot = slot, ToSide = side, ToSlot = slot });

    /// <summary>
    /// The Z-Power gathered round the Pokémon here before its Z-Move (plan 25 · Y14): it gathers itself (the charge
    /// pose, let go by its Z-Move's lunge) inside a frame of its Z-Move's type. The Z-Power counts its seeds for
    /// itself, so a battle's other cues keep theirs.
    /// </summary>
    public EffectCue ZPower(BattleSide side, int slot, PokemonType type)
    {
        var v = this[side, slot];
        v.ChargeAge = 0f;
        v.AttackAge = -1f;
        var cue = new EffectCue
        {
            Kind = CueKind.ZPower, Type = type, FromSide = side, FromSlot = slot, ToSide = side, ToSlot = slot,
            Seed = ++zPowerCount * 5233 + (int)type * 97
        };
        cues.Add(cue);
        return cue;
    }

    private int zPowerCount;

    // ------------------------------------------------------------------ Dynamax and Terastallization (plan 25 · Y15)

    /// <summary>
    /// The Pokémon here Dynamaxes (or Gigantamaxes): it stands giant from now on. <paramref name="played"/>, its cue
    /// plays and it grows through it (seeded as the move cue it stands in for was, so later cues keep their seeds);
    /// otherwise (the scene off) it is giant at once.
    /// </summary>
    /// <param name="caller">
    /// The trainer who calls it (plan 25 · YD1, <see cref="CallerFor"/>): their call comes first, for
    /// <see cref="DynamaxCallTime"/>, and the growth after it; null for the growth alone.
    /// </param>
    public EffectCue? Dynamax(BattleSide side, int slot, bool gigantamax, bool played = true, string? caller = null)
    {
        var v = this[side, slot];
        Dynamaxes++;
        v.Dynamaxed = true;
        v.Gigantamax = gigantamax;
        v.DynamaxTurnsLeft = 3;
        v.GiantAge = played ? 0f : -1f;
        float lead = played && caller != null ? DynamaxCallTime : 0f;
        v.GiantLead = lead;
        if (!played) return null;
        var cue = Cue(new EffectCue
        {
            Kind = CueKind.Dynamax, Gigantamax = gigantamax, Move = "Transform", Type = PokemonType.Normal,
            FromSide = side, FromSlot = slot, ToSide = side, ToSlot = slot, Lead = lead
        });
        if (caller != null) Call = new DynamaxCall(caller, side, slot, cue);
        return cue;
    }

    /// <summary>
    /// How much of the Pokémon here shows through its trainer's call (plan 25 · YD1): drawn back into red light, out
    /// of sight while the ball swells and flies, and grown back out of the opened ball. 1 and no flash with no call.
    /// </summary>
    public static (float Grow, float Flash) CalledBack(CombatantView v)
    {
        if (!v.Dynamaxed || v.GiantLead <= 0f || v.GiantAge < 0f) return (1f, 0f);
        float t = v.GiantAge, lead = v.GiantLead;
        if (t < CallDrawStart) return (1f, 0f);
        if (t < CallDrawEnd)
        {
            float k = (t - CallDrawStart) / (CallDrawEnd - CallDrawStart);
            k = k * k * (3f - 2f * k);
            return (1f - k, Math.Min(1f, k * 1.6f));
        }
        if (t < lead) return (0f, 1f);
        float e = Math.Clamp((t - lead) / CallEmerge, 0f, 1f);
        const float c1 = 1.70158f, c3 = c1 + 1f;
        float back = 1f + c3 * MathF.Pow(e - 1f, 3f) + c1 * MathF.Pow(e - 1f, 2f);
        return (e >= 1f ? 1f : 0.12f + 0.88f * back, 1f - e);
    }

    /// <summary>
    /// The Dynamax here ends: the body shrinks back through its cue, or at once with the scene off. Its cues count
    /// their seeds for themselves, so a battle's other cues keep theirs.
    /// </summary>
    public EffectCue? Undynamax(BattleSide side, int slot, bool played = true)
    {
        var v = this[side, slot];
        if (!v.Dynamaxed) return null;
        bool gigantamax = v.Gigantamax;
        v.Dynamaxed = v.Gigantamax = false;
        v.DynamaxTurnsLeft = 0;
        v.GiantLead = 0f;
        if (Call is { } call && call.Side == side && call.Slot == slot) Call = null;
        // A Pokémon going back into its ball shrinks back as it goes, with no cue of its own; one that fainted giant falls
        // into its clouds with one (plan 25 · YD3)
        bool fell = v.FallAge >= 0f && v.FaintAge >= 0f;
        bool leaving = !fell && (v.FaintAge >= 0f || v.RecallAge >= 0f || !v.Present);
        v.GiantAge = played ? 0f : -1f;
        v.TickAge = -1f;
        if (!played || leaving) return null;
        var cue = new EffectCue
        {
            Kind = CueKind.DynamaxEnd, Gigantamax = gigantamax, Fell = fell, FromSide = side, FromSlot = slot, ToSide = side, ToSlot = slot,
            Seed = ++giantCount * 4583 + slot * 17 + (int)side
        };
        cues.Add(cue);
        return cue;
    }

    private int giantCount;

    /// <summary>A Dynamax as the rules have it once a menu opens, with the turns it has left: nothing played.</summary>
    public void SettleDynamax(BattleSide side, int slot, bool dynamaxed, int turnsLeft, bool gigantamax)
    {
        var v = this[side, slot];
        if (v.Dynamaxed != dynamaxed)
        {
            v.Dynamaxed = dynamaxed;
            v.Gigantamax = dynamaxed && gigantamax;
            v.GiantAge = -1f;
            v.GiantLead = 0f;
        }
        int now = dynamaxed ? Math.Clamp(turnsLeft, 1, 3) : 0;
        // The pill ticks down as the menu settles a lower count, never up (plan 25 · YD3)
        if (now > 0 && v.DynamaxTurnsLeft > now)
        {
            v.TickAge = 0f;
            v.TickFrom = v.DynamaxTurnsLeft;
        }
        else if (now == 0) v.TickAge = -1f;
        v.DynamaxTurnsLeft = now;
    }

    /// <summary>
    /// A Max Move strikes the Pokémon here (plan 25 · YD2): its HP box jolts and the share it is about to lose stays
    /// white on the bar a moment (from the bar's own HP now, never the rules').
    /// </summary>
    public void Punch(BattleSide side, int slot)
    {
        var v = this[side, slot];
        // A second blow while the first's white still shows keeps the white where it is
        v.GhostHp = v.PunchAge >= 0f ? Math.Max(v.GhostHp, v.DisplayedHp) : v.DisplayedHp;
        v.PunchAge = 0f;
    }

    /// <summary>The Pokémon here put up its Max Guard this turn (plan 25 · YD3): a blow that comes to nothing on it rings the dome.</summary>
    public void GuardUp(BattleSide side, int slot) => this[side, slot].MaxGuarded = true;

    /// <summary>A menu opens: every Max Guard has come down with the turn (plan 25 · YD3).</summary>
    public void SettleGuard(BattleSide side, int slot) => this[side, slot].MaxGuarded = false;

    /// <summary>A Dynamaxed Pokémon on the field is on its last turn (plan 25 · YD3): the heartbeat under the music.</summary>
    public bool GiantOnItsLastTurn
    {
        get
        {
            for (int side = 0; side < 2; side++)
                for (int slot = 0; slot < SlotsOf((BattleSide)side); slot++)
                {
                    var v = views[side, slot];
                    if (v.Present && v.Dynamaxed && v.DynamaxTurnsLeft == 1 && v.FaintAge < 0f) return true;
                }
            return false;
        }
    }

    /// <summary>
    /// How much bigger the Pokémon here stands than its own size: <see cref="DynamaxScaleFoe"/> (or
    /// <see cref="DynamaxScaleOwn"/> on the player's side) while Dynamaxed, eased in as it grows and out as it shrinks.
    /// </summary>
    public static float GiantOf(CombatantView v, bool isPlayer)
    {
        float big = isPlayer ? DynamaxScaleOwn : DynamaxScaleFoe;
        if (v.Dynamaxed)
        {
            if (v.GiantAge < 0f) return big;
            // After the trainer's call, if there was one (plan 25 · YD1)
            float k = Math.Clamp((v.GiantAge - v.GiantLead - DynamaxGrowStart) / (DynamaxGrown - DynamaxGrowStart), 0f, 1f);
            return 1f + (big - 1f) * (1f - (1f - k) * (1f - k));
        }
        if (v.GiantAge < 0f) return 1f;
        return FallOf(v.GiantAge, big);
    }

    /// <summary>
    /// The fall's size (plan 25 · YD3): held giant while it shudders, falling in to <see cref="DynamaxFallLow"/> of its
    /// own size by <see cref="DynamaxShrunk"/>, and back up to its own by <see cref="DynamaxShrinkTime"/>.
    /// </summary>
    public static float FallOf(float age, float big)
    {
        static float Smooth(float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return t * t * (3f - 2f * t);
        }
        if (age < DynamaxFallStart) return big;
        if (age < DynamaxShrunk) return big + (DynamaxFallLow - big) * Smooth((age - DynamaxFallStart) / (DynamaxShrunk - DynamaxFallStart));
        if (age < DynamaxShrinkTime) return DynamaxFallLow + (1f - DynamaxFallLow) * Smooth((age - DynamaxShrunk) / (DynamaxShrinkTime - DynamaxShrunk));
        return 1f;
    }

    /// <summary>
    /// The Pokémon here Terastallizes into <paramref name="tera"/>: its crystal shell forms and shatters and its
    /// crown stays (the crown and the sheen that last are read from its Pokémon's own Tera Type). Seeded as the move
    /// cue it stands in for was.
    /// </summary>
    public EffectCue Terastallize(BattleSide side, int slot, Models.TeraType tera) =>
        Cue(new EffectCue
        {
            Kind = CueKind.Tera, Type = tera.Type, Stellar = tera.IsStellar, Move = "Transform",
            FromSide = side, FromSlot = slot, ToSide = side, ToSlot = slot
        });

    /// <summary>
    /// The Seals of a Ball Capsule bursting round a Pokémon as it is sent out (plan 06 · R17b). It begins with the
    /// throw (<see cref="SendOut"/>); the Seals go off once the ball has opened, each by its own delay.
    /// </summary>
    public EffectCue SealBurst(BattleSide side, int slot, Models.BallCapsule capsule) =>
        Cue(new EffectCue
        {
            Kind = CueKind.Seals, Capsule = capsule, FromSide = side, FromSlot = slot,
            ToSide = side == BattleSide.Player ? BattleSide.Enemy : BattleSide.Player, ToSlot = 0
        });

    /// <summary>
    /// A shiny Pokémon's sparkle as it comes out (plan 09 · L1): stars burst out of it once it has grown out of its
    /// ball, or a moment after it appears (<paramref name="fromBall"/> false). It counts for itself, so no move's
    /// seed moves, and it isn't started about a Pokémon that can't be seen; null then.
    /// </summary>
    public EffectCue? ShinySparkle(BattleSide side, int slot, bool fromBall)
    {
        var cue = new EffectCue
        {
            Kind = CueKind.Shiny, Lead = fromBall ? ShinyFromBall : ShinyFromAppearing,
            FromSide = side, FromSlot = slot, ToSide = side, ToSlot = slot,
            Seed = ++shinyCount * 3911 + slot * 29 + (int)side
        };
        if (!InSight(side, slot)) return null;
        cues.Add(cue);
        return cue;
    }

    private int shinyCount;

    // ------------------------------------------------------------------ a battle's answers (plan 25 · Y5)

    /// <summary>
    /// A move that missed passes the Pokémon here, sent from <paramref name="fromSide"/>'s <paramref name="fromSlot"/>:
    /// it dodges, away from the miss's path, furthest aside at <see cref="ImpactTime"/>. Not on a Pokémon that can't be
    /// seen (behind a doll, out of sight) or is going down.
    /// </summary>
    public void Dodge(BattleSide side, int slot, BattleSide fromSide, int fromSlot)
    {
        var v = this[side, slot];
        if (!InSight(side, slot) || v.FaintAge >= 0f || v.RecallAge >= 0f || v.CaptureAge >= 0f || !v.Present) return;
        v.DodgeAge = 0f;
        v.DodgeFromSide = fromSide;
        v.DodgeFromSlot = fromSlot;
    }

    /// <summary>Every Pokémon standing on <paramref name="side"/>'s platforms cheers: a battle won, a Pokémon caught, the player's loss.</summary>
    public void Cheer(BattleSide side)
    {
        for (int slot = 0; slot < SlotsOf(side); slot++)
        {
            var v = this[side, slot];
            if (!v.Present || v.Away || v.Shown == null || v.Shown.IsFainted || v.FaintAge >= 0f || v.RecallAge >= 0f || v.CaptureAge >= 0f) continue;
            v.CheerAge = 0f;
        }
    }

    /// <summary>
    /// The Pokémon here begins a charge turn: it gathers itself until its move is let go (the next lunge from it),
    /// and the move's charge half plays round it. The charges count their seeds for themselves, so a battle's other
    /// cues keep the seeds they had before there were any.
    /// </summary>
    public EffectCue Charge(BattleSide side, int slot, string move, PokemonType type, MoveCategory category)
    {
        var v = this[side, slot];
        v.ChargeAge = 0f;
        // The charge takes the place of the hop its line began
        v.AttackAge = -1f;
        var cue = new EffectCue
        {
            Kind = CueKind.Move, Charging = true, Move = move, Type = type, Category = category,
            FromSide = side, FromSlot = slot, ToSide = side, ToSlot = slot,
            Seed = ++chargeCount * 4111 + move.Length
        };
        if (Shows(cue)) cues.Add(cue);
        return cue;
    }

    private int chargeCount;

    /// <summary>A charge that ended with no move let go (the rules say so once a menu opens: put to sleep, the move gone).</summary>
    public void SettleCharge(BattleSide side, int slot, bool charging)
    {
        var v = this[side, slot];
        if (!charging) v.ChargeAge = -1f;
    }

    /// <summary>How far the Pokémon here has gathered itself through a charge turn, 0..1.</summary>
    public static float Gathered(CombatantView v) => v.ChargeAge < 0f ? 0f : Math.Clamp(v.ChargeAge / ChargeInTime, 0f, 1f);

    /// <summary>A stat's change as the eyes show it for <see cref="StatEyesTime"/>: above nought rising, below falling.</summary>
    public static float StatShown(CombatantView v) => v.StatAge < 0f ? 0f : v.StatRose ? 1f : -1f;

    /// <summary>Whether the bar's HP is in the red: a fifth or less, as the bar shows it, and not nought.</summary>
    public static bool InTheRed(CombatantView v) =>
        v.Shown is { MaxHP: > 0 } shown && v.DisplayedHp > 0f && v.DisplayedHp <= shown.MaxHP * LowHpRatio;

    /// <summary>Faints after <paramref name="delay"/> seconds (so the hit and the HP drain play first).</summary>
    public void Faint(BattleSide side, float delay = 0.6f, int slot = 0)
    {
        var v = this[side, slot];
        v.FaintDelay = delay;
        v.FaintAge = 0f;
        // A giant falls into its clouds first (plan 25 · YD3): the faint waits for it
        if (v.Dynamaxed && v.Present && fallsAsGiant)
        {
            v.FallAge = 0f;
            v.FaintDelay += GiantFallTime;
            // Its end begins with the fall, whenever the rules' line of it comes
            Undynamax(side, slot);
        }
    }

    /// <summary>Whether a giant that faints falls into its clouds first (the scene on; plan 25 · YD3). Set by the engine.</summary>
    public bool FallsAsGiant { get => fallsAsGiant; set => fallsAsGiant = value; }
    private bool fallsAsGiant = true;

    /// <summary>Called back into its ball: a doll standing in front of it goes as it does.</summary>
    public void Recall(BattleSide side, int slot = 0)
    {
        var v = this[side, slot];
        v.RecallAge = 0f;
        v.Ball = Graphics.BattleBall.NameFor(v.Shown);
        SubstituteDown(side, slot);
    }

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
        // It keeps the ball it broke out of (plan 25 · Y4)
        if (Ball != null) v.Ball = Ball.Ball;
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
        if (PlayerTrainerExit > TrainerExitTime) PlayerTrainer = PartnerTrainer = null;
        if (EnemyTrainerExit > TrainerExitTime) EnemyTrainer = EnemyTrainer2 = null;
        if (Speaking != null)
        {
            Speaking.Age += dt;
            if (Speaking.LeaveAge >= 0f && (Speaking.LeaveAge += dt) >= TrainerBackTime) Speaking = null;
        }

        for (int i = 0; i < cues.Count; i++)
        {
            var cue = cues[i];
            if ((cue.Age += dt) >= cue.Length) cues.RemoveAt(i--);
        }
        // The trainer who called a Dynamax has walked off (plan 25 · YD1)
        if (Call != null && (Call.Age >= CallerGone || !cues.Contains(Call.Cue))) Call = null;
        if (Weather != Sim.BattleWeather.None) WeatherAge += dt;
        if (FadingAge >= 0f && (FadingAge += dt) >= WeatherFadeTime)
        {
            FadingAge = -1f;
            FadingWeather = Sim.BattleWeather.None;
        }
        FieldLook.Advance(dt);
        Conditions.Advance(dt);
        // An SOS battle's wild side opens out from one place to two as the first ally comes (plan 14 · B5)
        for (int side = 0; side < 2; side++)
            if (spreading[side]) spread[side] = Math.Min(1f, spread[side] + dt / ArriveTime);
        // A rotation battle's turntable turns (plan 14 · B7)
        for (int side = 0; side < 2; side++)
            if (rotationAge[side] >= 0f && (rotationAge[side] += dt) >= RotationTime) rotationAge[side] = -1f;
        if (Ball != null)
        {
            Ball.Age += dt;
            if (!Ball.Caught && Ball.Age >= BallThrowTime(Ball.Shakes)) Ball = null;
        }

        for (int side = 0; side < 2; side++)
            for (int slot = 0; slot < SlotsOf((BattleSide)side); slot++)
                Advance(views[side, slot], dt, logical((BattleSide)side, slot));
    }

    private static void Advance(CombatantView v, float dt, Pokemon? logical)
    {
        v.SendOutAge = Tick(v.SendOutAge, dt, SendOutTime);
        v.ArriveAge = Tick(v.ArriveAge, dt, ArriveTime);
        if (v.Substitute is { } doll)
        {
            doll.Age += dt;
            doll.HitAge = Tick(doll.HitAge, dt, SubstituteHitTime);
            doll.AsideAge = Tick(doll.AsideAge, dt, SubstituteAsideTime);
            if (!doll.Up && doll.Age >= SubstituteOutTime) v.Substitute = null;
        }
        v.AttackAge = Tick(v.AttackAge, dt, AttackTime);
        v.HitAge = Tick(v.HitAge, dt, HitTime);
        v.DodgeAge = Tick(v.DodgeAge, dt, DodgeTime);
        v.CheerAge = Tick(v.CheerAge, dt, CheerTime);
        v.StatAge = Tick(v.StatAge, dt, StatEyesTime);
        if (v.ChargeAge >= 0f) v.ChargeAge += dt;
        v.AwayAge = Tick(v.AwayAge, dt, Graphics.BodyMotions.LeaveTime);
        v.BackAge = Tick(v.BackAge, dt, Graphics.BodyMotions.ReturnTime);
        // A spent body stays down while it lies fainted, and is let go once it stands again or is gone
        if (v.SpentAge >= 0f)
        {
            v.SpentAge += dt;
            if (v.SpentAge > EffectTime + 0.3f && v.Shown is { CurrentHP: > 0 }) v.SpentAge = -1f;
        }
        if (v.GiantAge >= 0f && (v.GiantAge += dt) >= (v.Dynamaxed ? v.GiantLead + DynamaxTime : DynamaxEndTime))
        {
            v.GiantAge = -1f;
            v.GiantLead = 0f;
        }
        // The Dynamax's finisher and fall (plan 25 · YD2, YD3)
        v.TickAge = Tick(v.TickAge, dt, TickTime);
        if (v.FallAge >= 0f) v.FallAge += dt;

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
        // A Max Move's white ghost of the bar (plan 25 · YD2): held a moment, then draining after the bar at its pace,
        // and never under it nor over the most
        if (v.PunchAge >= 0f)
        {
            v.PunchAge += dt;
            if (v.PunchAge > GhostHold) v.GhostHp = Toward(v.GhostHp, v.DisplayedHp, hpRate * dt);
            v.GhostHp = Math.Clamp(v.GhostHp, v.DisplayedHp, Math.Max(v.DisplayedHp, shown.MaxHP));
            if (v.PunchAge > Math.Max(PunchTime, GhostHold) && v.GhostHp <= v.DisplayedHp + 0.01f) v.PunchAge = -1f;
        }
        float exp = shown.ExpProgressRatio;
        v.DisplayedExp = exp < v.DisplayedExp ? exp : Toward(v.DisplayedExp, exp, 0.8f * dt);
        // Tiredness follows the bar, never the HP the rules have (plan 25 · Y5), coming on as the bar crosses into the red
        v.Tired = Toward(v.Tired, InTheRed(v) ? 1f : 0f, dt / TiredEaseTime);
    }

    private static void Reset(CombatantView v, Pokemon pokemon)
    {
        v.Shown = pokemon;
        v.Away = false;
        v.SendOutAge = v.AttackAge = v.HitAge = v.FaintAge = v.RecallAge = v.CaptureAge = -1f;
        v.ArriveAge = -1f;
        v.DodgeAge = v.CheerAge = v.ChargeAge = v.StatAge = -1f;
        v.AwayAge = v.BackAge = v.SpentAge = -1f;
        v.FaintDelay = v.CaptureDelay = 0f;
        // A new Pokémon on the place stands in front of nothing (a passed doll comes in after it, by its own line)
        v.Substitute = null;
        // ...and at its own size (a Dynamax ends as its Pokémon leaves: plan 25 · Y15)
        v.Dynamaxed = v.Gigantamax = false;
        v.GiantAge = -1f;
        v.GiantLead = 0f;
        v.DynamaxTurnsLeft = 0;
        v.PunchAge = v.TickAge = v.FallAge = -1f;
        v.MaxGuarded = false;
        v.DisplayedHp = pokemon.CurrentHP;
        v.GhostHp = pokemon.CurrentHP;
        v.DisplayedExp = pokemon.ExpProgressRatio;
        // One that comes in already in the red is tired from the start (plan 25 · Y5)
        v.Tired = InTheRed(v) ? 1f : 0f;
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

/// <summary>
/// A trainer across the field back on their platform to say a line (plan 11 · C13; style guide, "Camera (G8)" and
/// "Characters", "Faces in battle"): who they are, where they stand, and how far they have come or gone, as a
/// function of the animator's clock alone.
/// </summary>
public sealed class SpeakingTrainer
{
    public SpeakingTrainer(string look, bool second)
    {
        Look = look;
        Second = second;
    }

    /// <summary>The look they are drawn in: the one they stood on the platform in before the Pokémon were sent out.</summary>
    public string Look { get; }

    /// <summary>The second of two trainers battling together, who stands at the other edge of the platform.</summary>
    public bool Second { get; }

    /// <summary>The face their line is said with, and the gesture played once they are back.</summary>
    public Overworld.Mood Mood { get; internal set; }
    public Overworld.Gesture? Gesture { get; internal set; }

    /// <summary>Seconds since they set off back to their platform.</summary>
    public float Age { get; internal set; }

    /// <summary>Seconds since they set off again once their line was read (-1 while it is still being read).</summary>
    public float LeaveAge { get; internal set; } = -1f;

    /// <summary>How far onto the platform they stand: 0 off it, 1 at its edge.</summary>
    public float Back => Math.Min(Math.Clamp(Age / BattleAnimator.TrainerBackTime, 0f, 1f),
        LeaveAge < 0f ? 1f : 1f - Math.Clamp(LeaveAge / BattleAnimator.TrainerBackTime, 0f, 1f));

    /// <summary>Seconds into their gesture, which begins as they arrive (negative before).</summary>
    public float GestureAge => Age - BattleAnimator.TrainerBackTime;
}
