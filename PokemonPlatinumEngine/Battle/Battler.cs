using System.Collections.Generic;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

/// <summary>Where a two-turn move has taken its user for the turn in between, out of most moves' reach.</summary>
public enum Elsewhere { No, InTheAir, Underground, Underwater, Vanished }

/// <summary>
/// One place on the field (a side and a slot: one per side in a single battle, two in a double) and the
/// Pokémon standing there, with the conditions that last only while it stays in (confusion, a Substitute, being
/// held to a move). Stat stages live on <see cref="Pokemon"/> and are cleared when it leaves.
/// </summary>
public sealed class Battler
{
    public BattleSide Side { get; }
    public int Slot { get; }

    /// <summary>The Pokémon in this place; it stays here after fainting until a replacement comes in (null = never filled).</summary>
    public Pokemon? Pokemon { get; internal set; }

    /// <summary>The party that sends Pokémon to this place (null for a wild Pokémon).</summary>
    internal Party? Roster { get; set; }

    /// <summary>The trainer standing behind this place, for "sent out" messages (null for the player and wild Pokémon).</summary>
    internal Trainer? Trainer { get; set; }

    /// <summary>The field it stands on: only the battle's own battlers have one, not the screen's.</summary>
    internal FieldState? Field { get; set; }

    internal Battler(BattleSide side, int slot)
    {
        Side = side;
        Slot = slot;
    }

    public bool IsPlayerSide => Side == BattleSide.Player;

    /// <summary>Where it stands, as the battle's log names places.</summary>
    public Place Place => new(Side, Slot);

    /// <summary>A Pokémon is standing here and can still fight.</summary>
    public bool IsActive => Pokemon != null && !Pokemon.IsFainted;

    /// <summary>How the battle's messages name it: "Turtwig" for the player's, "Foe Starly" for the other side's.</summary>
    public string Name => Pokemon == null ? "" : IsPlayerSide ? Pokemon.DisplayName : $"Foe {Pokemon.DisplayName}";

    // ---- Conditions that end when the Pokémon leaves the field

    public int ConfusionTurns { get; internal set; }
    public bool IsConfused => ConfusionTurns > 0;
    internal bool Flinched;

    /// <summary>The move a Choice item holds it to, until it leaves the field.</summary>
    public Move? ChoiceLock { get; internal set; }

    /// <summary>Flash Fire has absorbed a Fire move: its own Fire moves are stronger.</summary>
    internal bool FlashFire;

    /// <summary>It has already moved this turn (Flinch only works on a Pokémon that hasn't).</summary>
    internal bool MovedThisTurn;

    /// <summary>It was hit this turn (by a critical hit, for Anger Point; by anything, for Revenge-style checks later).</summary>
    internal bool TookCriticalHit;

    /// <summary>Everything else that lasts only while it stays in (plan 06 · R3).</summary>
    internal Volatiles Volatile = new();

    /// <summary>What holds for this turn only.</summary>
    internal TurnFlags Turn = new();

    /// <summary>The Pokémon of the other side that saw this one on the field, for sharing EXP.</summary>
    internal readonly HashSet<Pokemon> FoughtAgainst = new();

    public bool HasSubstitute => Volatile.SubstituteHp > 0;

    /// <summary>In the air, under the ground or the water, or gone from sight: most moves can't reach it.</summary>
    public bool IsElsewhere => Volatile.Elsewhere != Elsewhere.No;

    /// <summary>
    /// Nothing to choose this turn: it must recharge, is in a rampage or an uproar, or is in the middle of a move
    /// that takes two turns (the original's <c>Battler_CanPickCommand</c>).
    /// </summary>
    public bool IsHeldToItsMove => Volatile.Recharging || Volatile.RampageTurns > 0 || Volatile.UproarTurns > 0 || Volatile.Charging || Volatile.RolloutTurns > 0;

    /// <summary>The ability in force: none while Gastro Acid has suppressed it.</summary>
    public Ability? Ability => Volatile.AbilitySuppressed ? null : Pokemon?.Ability;

    /// <summary>Clears everything that only lasts while on the field (on switching out or in).</summary>
    internal void ClearVolatile()
    {
        ConfusionTurns = 0;
        Flinched = false;
        ChoiceLock = null;
        FlashFire = false;
        MovedThisTurn = false;
        TookCriticalHit = false;
        Volatile = new Volatiles();
        Turn = new TurnFlags();
    }

    /// <summary>
    /// Takes on another battler's passing conditions. The two stand for the same place with different copies of
    /// the Pokémon (the battle's own and the one on screen), so a Choice item's lock is found again by the move's
    /// position; everything else is plain values.
    /// </summary>
    internal void CopyVolatileFrom(Battler other)
    {
        ConfusionTurns = other.ConfusionTurns;
        Flinched = other.Flinched;
        FlashFire = other.FlashFire;
        MovedThisTurn = other.MovedThisTurn;
        TookCriticalHit = other.TookCriticalHit;
        int locked = other.ChoiceLock == null || other.Pokemon == null ? -1 : other.Pokemon.Moves.IndexOf(other.ChoiceLock);
        ChoiceLock = locked >= 0 && Pokemon != null && locked < Pokemon.Moves.Count ? Pokemon.Moves[locked] : null;
        Volatile = other.Volatile.Copy();
        Turn = other.Turn.Copy();
    }

    /// <summary>The types the Pokémon has right now: its own, unless a move changed them for the battle (Conversion, Transform).</summary>
    public bool HasType(PokemonType type) =>
        Volatile.Types != null ? Volatile.Types.Contains(type) : Pokemon != null && Pokemon.HasType(type);

    /// <summary>Its types right now, one or two, the first its own first.</summary>
    public IReadOnlyList<PokemonType> Types
    {
        get
        {
            if (Volatile.Types != null) return Volatile.Types;
            if (Pokemon == null) return System.Array.Empty<PokemonType>();
            return Pokemon.SecondaryType is { } second && second != Pokemon.PrimaryType
                ? new[] { Pokemon.PrimaryType, second }
                : new[] { Pokemon.PrimaryType };
        }
    }

    public override string ToString() => $"{Side} {Slot}: {Pokemon?.DisplayName ?? "empty"}";
}

/// <summary>
/// A Pokémon's passing conditions beyond the oldest few (which <see cref="Battler"/> keeps itself): gone when it
/// leaves the field, except what Baton Pass hands on. Plain values only (a place, a move's data, a count), so the
/// screen's battler can hold a copy. Counters count as the original's do; each says how.
/// </summary>
internal sealed class Volatiles
{
    // ---- Given by others

    /// <summary>Infatuated with whoever stands in this place.</summary>
    public Place? InLoveWith;

    /// <summary>Seeded: its HP goes to whoever stands in this place.</summary>
    public Place? SeededBy;

    public bool Cursed, Nightmare, Tormented;

    /// <summary>Foresight or Odor Sleuth: raised evasion counts for nothing, and a Ghost can be hit by Normal and Fighting moves.</summary>
    public bool Identified;

    /// <summary>Miracle Eye: raised evasion counts for nothing, and a Dark type can be hit by Psychic moves.</summary>
    public bool MiracleEye;

    /// <summary>The perish count; −1 when no song was heard.</summary>
    public int PerishCount = -1;

    /// <summary>Turn ends left (each of these ends when its count reaches 0 at a turn's end).</summary>
    public int TauntTurns, HealBlockTurns, EmbargoTurns, MagnetRiseTurns, ChargeTurns;

    /// <summary>Asleep when this reaches 0 at a turn's end (Yawn sets 2).</summary>
    public int YawnTurns;

    public MoveData? Encored;
    public int EncoreTurns;
    public MoveData? Disabled;
    public int DisableTurns;

    /// <summary>Wrapped, clamped or caught in a whirl: hurt each turn end while the count lasts, by whoever stands in <see cref="BoundBy"/>.</summary>
    public int BindTurns;
    public Place? BoundBy;
    public string BindingMove = "";

    /// <summary>Mean Look, Block or Spider Web: can't leave while the one that did it stays.</summary>
    public Place? TrappedBy;

    /// <summary>Lock-On or Mind Reader: the next move of whoever stands there can't miss this one. Counts turn ends (2 when set).</summary>
    public Place? LockedOnBy;
    public int LockOnTurns;

    public bool AbilitySuppressed;

    // ---- Its own

    public int SubstituteHp;
    public bool FocusEnergy, Ingrained, AquaRing, Minimized, DefenseCurl, DestinyBond, Grudge, Rage, Imprisoning, MudSport, WaterSport;

    /// <summary>Protect, Detect and Endure used with success in a row: each makes the next less likely.</summary>
    public int ProtectChain;

    // ---- Moves that take more than a turn

    /// <summary>The move it is held to: charged, rampaging, uproaring, biding or recharging.</summary>
    public MoveData? LockedMove;

    /// <summary>It has spent a turn on a move that strikes on its second (or is biding): the strike is owed.</summary>
    public bool Charging;

    public Elsewhere Elsewhere;

    /// <summary>It must spend its next turn recharging; <see cref="RechargeTurn"/> is the turn the move was used.</summary>
    public bool Recharging;
    public int RechargeTurn;

    /// <summary>Turn ends left of Thrash, Outrage or Petal Dance; confusion follows the last.</summary>
    public int RampageTurns;
    public int UproarTurns;

    /// <summary>Bide: its own turns left before it strikes (2 when begun), and what it has taken meanwhile.</summary>
    public int BideTurns;
    public int BideDamage;

    /// <summary>Rollout or Ice Ball: uses left of the five it is held to.</summary>
    public int RolloutTurns;

    /// <summary>Fury Cutter: uses in a row so far (up to 5), until something stops its user.</summary>
    public int FuryCutterCount;

    /// <summary>Stockpiles held (up to 3), and how many stages of Defense and Sp. Def they raised, which Spit Up and Swallow take back.</summary>
    public int Stockpile, StockpileDef, StockpileSpDef;

    // ---- What it remembers

    /// <summary>The last move it got to use (Disable, Encore and Torment go by it).</summary>
    public MoveData? LastMove;

    /// <summary>Whoever hit it last (Bide strikes back there).</summary>
    public Place? LastHitBy;

    /// <summary>The turn it came in on, and the first turn it gets to act in (Fake Out works on that one alone).</summary>
    public int EnteredOnTurn, FirstTurn;

    /// <summary>Which of its moves it has used, by position (a bit each), for Last Resort.</summary>
    public int UsedMoveSlots;

    /// <summary>The last move aimed at it that Mirror Move can copy (the original's <c>moveCopied</c>).</summary>
    public MoveData? MirrorMove;

    /// <summary>The last move that hit it, with its type and whose it was, for Conversion 2 (the original's <c>conversion2Move</c>).</summary>
    public MoveData? Conversion2Move;
    public PokemonType Conversion2Type;
    public Place? Conversion2By;

    // ---- What a move changed for the battle (plan 06 · R5)

    /// <summary>Its types for the rest of its time on the field (Conversion, Conversion 2, Transform); null for its own.</summary>
    public List<PokemonType>? Types;

    /// <summary>It has taken a target's shape with Transform.</summary>
    public bool Transformed;

    /// <summary>Its Attack and Defense are swapped (Power Trick); using it again swaps them back.</summary>
    public bool PowerTrick;

    /// <summary>What the Pokémon was before a move changed it for the battle; put back when it leaves. Null while nothing has.</summary>
    public Original? Original;

    /// <summary>The held item it used up or threw, which Recycle brings back.</summary>
    public ItemData? ConsumedItem;

    // ---- Abilities (plan 06 · R7)

    /// <summary>Unburden: it came in holding an item, so losing it doubles its Speed (the original's <c>canUnburden</c>).</summary>
    public bool CanUnburden;

    /// <summary>The turn Slow Start counts its five from (the original's <c>slowStartTurnNumber</c>): its first turn, or the one after a Transform.</summary>
    public int SlowStartTurn;

    /// <summary>Slow Start has run its five turns and said so (the original's <c>slowStartFinished</c>).</summary>
    public bool SlowStartEnded;

    /// <summary>Truant acts on the turns of this parity and loafs on the others (the original's <c>truant</c> bit).</summary>
    public int TruantParity;

    /// <summary>What has acted on entry since it came in (the original's <c>*Announced</c> flags, cleared by a Transform): each ability acts once per stay on the field.</summary>
    public EntryCheck Announced;

    public Volatiles Copy() => (Volatiles)MemberwiseClone();

    /// <summary>What Baton Pass hands on to the Pokémon that takes its place (the original's two <c>BATON_PASSED</c> masks).</summary>
    public Volatiles Passed() => new()
    {
        FocusEnergy = FocusEnergy,
        TrappedBy = TrappedBy,
        Cursed = Cursed,
        SubstituteHp = SubstituteHp,
        SeededBy = SeededBy,
        LockedOnBy = LockedOnBy,
        LockOnTurns = LockedOnBy != null ? 2 : 0,
        PerishCount = PerishCount,
        Ingrained = Ingrained,
        MudSport = MudSport,
        WaterSport = WaterSport,
        AquaRing = AquaRing,
        AbilitySuppressed = AbilitySuppressed,
        EmbargoTurns = EmbargoTurns,
        HealBlockTurns = HealBlockTurns,
        MagnetRiseTurns = MagnetRiseTurns
    };
}

/// <summary>The checks a Pokémon has been through since it came in (<c>BattleSystem_TriggerEffectOnSwitch</c>'s phases), so each acts once.</summary>
[System.Flags]
internal enum EntryCheck
{
    None = 0,
    Trace = 1,
    WeatherAbility = 2,
    Intimidate = 4,
    Download = 8,
    Anticipation = 16,
    Forewarn = 32,
    Frisk = 64,
    SlowStart = 128,
    MoldBreaker = 256,
    Pressure = 512,
    /// <summary>An ability of a later generation that acts on entry.</summary>
    Other = 1024,
    /// <summary>Its held item's say on entry, and the conditions it can't keep.</summary>
    Item = 2048
}

/// <summary>
/// What a Pokémon was before a move changed it for the battle (plan 06 · R5): the original keeps a Pokémon's party
/// data apart from its battle data and rebuilds the latter when it comes in again, so everything a Transform, a
/// Mimic, Role Play, Skill Swap, Worry Seed or Power Trick changed is put back when it leaves the field or the
/// battle ends. Each part is kept the first time a move changes it, and only then.
/// </summary>
internal sealed class Original
{
    public PokemonSpecies? Species;
    public string? Form;
    public bool NicknameGiven;
    public string? Nickname;

    public bool StatsKept;
    public int Attack, Defense, SpAttack, SpDefense, Speed;

    public bool AbilityKept;
    public string? AbilityName;

    /// <summary>The whole move list, with each move's PP (Transform).</summary>
    public List<(MoveData Data, int Pp)>? Moves;

    /// <summary>One slot's move and PP, for each slot Mimic filled.</summary>
    public Dictionary<int, (MoveData Data, int Pp)>? MoveSlots;
}

/// <summary>What is true of a Pokémon for the turn being played and no longer.</summary>
internal sealed class TurnFlags
{
    public bool Protecting, Enduring;

    /// <summary>Magic Coat is up: a status move that can be reflected bounces back to whoever used it.</summary>
    public bool MagicCoat;

    /// <summary>Snatch is waiting: the next move that can be snatched is taken and used on itself.</summary>
    public bool Snatching;

    /// <summary>Its ally's Helping Hand makes its move half as strong again.</summary>
    public bool HelpingHand;

    /// <summary>Its Lightning Rod or Storm Drain drew the move coming at it, which is told as the move arrives.</summary>
    public bool DrewTheMove;

    /// <summary>Roost: its Flying type doesn't count until the turn ends.</summary>
    public bool Roosting;

    /// <summary>It lost its move to paralysis, love, a flinch, confusion, a taunt… (a rampage or an uproar ends on it).</summary>
    public bool MoveFailed;

    /// <summary>The hit just taken went into its Substitute: side effects don't reach it.</summary>
    public bool SubstituteHit;

    /// <summary>Its action for the turn is done (Payback is twice as strong against it then).</summary>
    public bool Acted;

    /// <summary>It has lost HP this turn, to anything (Assurance).</summary>
    public bool TookDamage;

    /// <summary>
    /// What moves took from it this turn, by the place they came from (the original's turn flags): what Counter,
    /// Mirror Coat, Metal Burst and Revenge give back. A hit into its Substitute isn't counted.
    /// </summary>
    public int[] PhysicalFrom = new int[4], SpecialFrom = new int[4];
    public Place? LastPhysicalFrom, LastSpecialFrom, LastHitFrom;
    public int LastDamage;

    /// <summary>The last physical and special hit it took this turn, whoever from (Focus Punch loses its focus on either).</summary>
    public int PhysicalTaken, SpecialTaken;

    public void Took(Place from, MoveCategory category, int damage)
    {
        TookDamage = true;
        LastDamage = damage;
        LastHitFrom = from;
        if (category == MoveCategory.Physical)
        {
            PhysicalFrom[from.Number] = damage;
            LastPhysicalFrom = from;
            PhysicalTaken = damage;
        }
        else if (category == MoveCategory.Special)
        {
            SpecialFrom[from.Number] = damage;
            LastSpecialFrom = from;
            SpecialTaken = damage;
        }
    }

    public TurnFlags Copy()
    {
        var copy = (TurnFlags)MemberwiseClone();
        copy.PhysicalFrom = (int[])PhysicalFrom.Clone();
        copy.SpecialFrom = (int[])SpecialFrom.Clone();
        return copy;
    }
}
