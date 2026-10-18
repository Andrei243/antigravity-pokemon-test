using PokemonPlatinumEngine.Core;
using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

/// <summary>Where a two-turn move has taken its user for the turn in between, out of most moves' reach.</summary>
public enum Elsewhere { No, InTheAir, Underground, Underwater, Vanished }

/// <summary>What a Protect of the later games does to whoever touches it (plan 06 · R25): nothing but Protect's own, or a shield's answer.</summary>
public enum Shield { None, KingsShield, SpikyShield, BanefulBunker, MaxGuard, Obstruct, SilkTrap, BurningBulwark }

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
    public string Name => Pokemon == null ? "" : IsPlayerSide ? Shown : Text.Of("battle.foe", ("pokemon", Shown));

    /// <summary>The name it goes by: its own, or under an Illusion the one it looks like (plan 06 · R27b).</summary>
    private string Shown => Volatile.Illusion ?? Pokemon!.DisplayName;

    // ---- Conditions that end when the Pokémon leaves the field

    public int ConfusionTurns { get; internal set; }
    public bool IsConfused => ConfusionTurns > 0;
    /// <summary>It flinched this turn; a Dynamaxed Pokémon never does (plan 06 · R22; Showdown's dynamax onTryAddVolatile).</summary>
    internal bool Flinched { get => flinched; set => flinched = value && !Volatile.Dynamaxed; }
    private bool flinched;

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
    public Ability? Ability => Volatile.AbilitySuppressed || (Field != null && Effects.LaterAbilities.Neutralized(this)) ? null : Pokemon?.Ability;

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

    /// <summary>
    /// Arceus with Multitype is the type of the plate it holds, and Normal with none, whatever else would say:
    /// the original reads its type from the item every time it asks (<c>BattleMon_Get</c>), so a Ditto in its
    /// shape goes by what Ditto holds.
    /// </summary>
    private PokemonType? PlateType
    {
        get
        {
            // Silvally with RKS System is the type of the memory it holds, and Normal with none (plan 06 · R27b; Showdown's silvally condition)
            if (Pokemon is { AbilityName: "RKS System" } s && s.Species.Name == "Silvally" && !Volatile.Transformed)
                return s.HeldItem?.Name is { } memory && memory.EndsWith(" Memory") && System.Enum.TryParse(memory[..^" Memory".Length], out PokemonType kind) ? kind : PokemonType.Normal;
            if (Pokemon is not { AbilityName: "Multitype" } p || p.Species.Name != "Arceus") return null;
            return p.HeldItem?.HoldEffect is { } hold && hold.StartsWith("Arceus") && System.Enum.TryParse(hold["Arceus".Length..], out PokemonType type)
                ? type : PokemonType.Normal;
        }
    }

    /// <summary>The types the Pokémon has right now: its own, unless a move changed them for the battle (Conversion, Transform).</summary>
    public bool HasType(PokemonType type) =>
        TeraShown is { } tera ? type == tera : HasTypeBeforeTera(type);

    /// <summary>A Terastallized Pokémon is its Tera Type alone (plan 06 · R23; Showdown's <c>getTypes</c>), but a Stellar one keeps its own.</summary>
    private PokemonType? TeraShown => Pokemon?.Terastallized is { IsStellar: false } tera ? tera.Type : null;

    /// <summary>Its types as they would be without a Terastallization (Showdown's <c>getTypes(false, true)</c>), what the same-type bonus still counts.</summary>
    public bool HasTypeBeforeTera(PokemonType type) =>
        PlateType is { } plate ? type == plate : Volatile.Types != null ? Volatile.Types.Contains(type) : Pokemon != null && Pokemon.HasType(type);

    /// <summary>Its types right now, one or two, the first its own first.</summary>
    public IReadOnlyList<PokemonType> Types
    {
        get
        {
            if (TeraShown is { } tera) return new[] { tera };
            if (PlateType is { } plate) return new[] { plate };
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

    /// <summary>A foe used Imprison: what it knows is sealed from this one (the original's <c>MOVE_EFFECT_IMPRISONED</c>, which only the AI reads).</summary>
    public bool Imprisoned;

    // ---- Its own

    public int SubstituteHp;
    public bool FocusEnergy, Ingrained, AquaRing, Minimized, DefenseCurl, DestinyBond, Grudge, Rage, Imprisoning, MudSport, WaterSport;

    /// <summary>It used Camouflage (the original's <c>MOVE_EFFECT_CAMOUFLAGE</c>, which only the AI reads).</summary>
    public bool Camouflaged;

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

    /// <summary>
    /// The last move that landed on it from someone else since it last moved, and whose it was (the original's
    /// <c>moveHit</c> and <c>moveHitBattler</c>): what the trainer AI weighs when it thinks of switching.
    /// </summary>
    public MoveData? LastHitMove;
    public Place? LastHitMoveBy;

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

    // ---- The later moves (plan 06 · R24)

    /// <summary>Smack Down brought it down: it is on the ground whatever its type, ability or Magnet Rise say.</summary>
    public bool SmackedDown;

    /// <summary>Autotomize's uses: each takes 100 kg off its weight, to no less than 0.1 kg.</summary>
    public int Autotomized;

    // ---- The later moves, batch 2 (plan 06 · R25)

    /// <summary>The type Trick-or-Treat or Forest's Curse added to its own (one at a time: a second takes the first's place).</summary>
    public PokemonType? AddedType;

    /// <summary>Telekinesis: turn ends left of being held up in the air (every move but a one-hit knockout finds it, and no Ground move).</summary>
    public int TelekinesisTurns;

    /// <summary>The last turn Throat Chop keeps its sound moves from it, and the last turn Laser Focus makes its hits critical (−1: none).</summary>
    public int ThroatChopUntil = -1, LaserFocusUntil = -1;

    // ---- The later moves, R25's leftovers

    /// <summary>Sky Drop: whoever it has carried into the sky (its own), and whoever carried it up (the one carried).</summary>
    public Place? Lifting, LiftedBy;

    /// <summary>Ally Switch: the last turn it worked, and one chance in this many that it works again the turn after (3, then ×3 to 729).</summary>
    public int AllySwitchTurn = -1, AllySwitchOdds = 3;

    /// <summary>The place its last move was aimed at, which Instruct aims it at again (Showdown's <c>lastMoveTargetLoc</c>).</summary>
    public Place? LastMoveTarget;

    /// <summary>Every place it remembers, as another place's (Ally Switch: who stood where follows the Pokémon).</summary>
    public void Remap(Func<Place?, Place?> to)
    {
        InLoveWith = to(InLoveWith);
        SeededBy = to(SeededBy);
        BoundBy = to(BoundBy);
        TrappedBy = to(TrappedBy);
        LockedOnBy = to(LockedOnBy);
        LastHitBy = to(LastHitBy);
        LastHitMoveBy = to(LastHitMoveBy);
        Conversion2By = to(Conversion2By);
        Lifting = to(Lifting);
        LiftedBy = to(LiftedBy);
        OctolockedBy = to(OctolockedBy);
        Commanding = to(Commanding);
        CommandedBy = to(CommandedBy);
    }

    // ---- Abilities (plan 06 · R7)

    /// <summary>Unburden: it came in holding an item, so losing it doubles its Speed (the original's <c>canUnburden</c>).</summary>
    public bool CanUnburden;

    /// <summary>A Micle Berry was eaten: the next move's accuracy is a fifth more, once (the original's <c>micleBerry</c>).</summary>
    public bool MicleBerry;

    /// <summary>
    /// Its Quick Claw or Custap Berry went off this turn and the move it moved first with hasn't begun (the
    /// original's <c>quickClaw</c> and <c>custapBerry</c> flags): until then an Embargo, Knock Off, Trick and
    /// Switcheroo fail against it, so the item that went off is still there to be eaten.
    /// </summary>
    public bool ItemWentOff;

    /// <summary>A Metronome's count of the same move used in a row, up to 10 (the original's <c>metronomeTurns</c>), and which move it counts.</summary>
    public int MetronomeCount;
    public MoveData? MetronomeMove;

    /// <summary>The turn Slow Start counts its five from (the original's <c>slowStartTurnNumber</c>): its first turn, or the one after a Transform.</summary>
    public int SlowStartTurn;

    /// <summary>Slow Start has run its five turns and said so (the original's <c>slowStartFinished</c>).</summary>
    public bool SlowStartEnded;

    /// <summary>Truant acts on the turns of this parity and loafs on the others (the original's <c>truant</c> bit).</summary>
    public int TruantParity;

    /// <summary>What has acted on entry since it came in (the original's <c>*Announced</c> flags, cleared by a Transform): each ability acts once per stay on the field.</summary>
    public EntryCheck Announced;

    // ---- Dynamax (plan 06 · R22)

    /// <summary>It is Dynamaxed: its moves are Max Moves, its HP is raised, and it can't flinch, be Encored, Tormented or dragged out.</summary>
    public bool Dynamaxed;

    /// <summary>The turn ends it has been Dynamaxed through (Showdown's <c>turns</c>): at the third it ends.</summary>
    public int DynamaxTurns;

    /// <summary>Its most HP before the Dynamax, and with it (what the Dynamax's end gives back, and by what share).</summary>
    public int MaxHpBeforeDynamax, MaxHpDynamaxed;

    /// <summary>It took its Gigantamax form, and the form and ability it had before (given back as the Dynamax ends).</summary>
    public bool Gigantamaxed;
    public string? FormBeforeGigantamax, AbilityBeforeGigantamax;

    /// <summary>A layer of G-Max Chi Strike for each time it was given (up to three): so much higher its critical-hit stage.</summary>
    public int ChiStrike;

    // ---- The later moves, batch 3 (plan 06 · R26)

    /// <summary>No Retreat: it can't leave the field (and can't use the move again) while it stays in.</summary>
    public bool NoRetreat;

    /// <summary>Tar Shot: Fire moves are twice as strong against it while it stays in.</summary>
    public bool TarShot;

    /// <summary>Octolock: held by whoever stands there, it can't leave and loses a stage of Defense and Sp. Def at each turn's end.</summary>
    public Place? OctolockedBy;

    // ---- The later moves, batch 3, its second half (plan 06 · R26b)

    /// <summary>Salt Cure: cured with salt, it loses HP at each turn's end while it stays in.</summary>
    public bool SaltCured;

    /// <summary>Syrup Bomb: covered by whoever stands there, it loses a stage of Speed at each turn's end until the count is done.</summary>
    public Place? SyrupBombBy;
    public int SyrupBombTurns;

    /// <summary>Glaive Rush: until it next tries to move, every move finds it and hurts it twice as much.</summary>
    public bool GlaiveRush;

    /// <summary>Dragon Cheer: so many stages likelier to land a critical hit (two for a Dragon type as it was cheered).</summary>
    public int DragonCheer;

    // ---- The later abilities (plan 06 · R27)

    /// <summary>Protean or Libero has changed its type since it came in (generation 9: once a stay on the field).</summary>
    public bool ProteanUsed;

    // ---- The later abilities, the rest (plan 06 · R27b)

    /// <summary>Illusion: the name of the party member it looks like, which the battle's lines call it by until a hit breaks it; null with none.</summary>
    public string? Illusion;

    /// <summary>The stat Protosynthesis or Quark Drive raised as it took hold (Showdown's <c>bestStat</c>, fixed then); null while it isn't in force.</summary>
    public StatType? ParadoxBest;

    /// <summary>Supreme Overlord: how many of its side had fainted as it came in, five at most (Showdown's <c>fallen</c>).</summary>
    public int Fallen;

    /// <summary>Cud Chew: the berry it ate and will chew again, and the turn ends left until it does (Showdown's <c>counter</c>).</summary>
    public ItemData? CudChewBerry;
    public int CudChewTurns;

    /// <summary>Commander: the Tatsugiri is in its Dondozo's mouth (the Dondozo's place), and the Dondozo is commanded by it (the Tatsugiri's place).</summary>
    public Place? Commanding, CommandedBy;
    // ---- The later held items (plan 06 · R28a)

    /// <summary>Booster Energy has set its Protosynthesis or Quark Drive going for as long as it stays in.</summary>
    public bool BoosterEnergy;

    /// <summary>What a binding move takes of it at each turn's end, as a share of its HP: six from a Binding Band in the binder's hands, nought for the rules' own.</summary>
    public int BindDivisor;

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
    Item = 2048,
    /// <summary>The shape a later ability gives on entry (plan 06 · R19b: Schooling, Shields Down, Zero to Hero, Ice Face).</summary>
    Shape = 4096
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
    public List<(MoveData Data, int Pp, int Ups)>? Moves;

    /// <summary>One slot's move and PP, for each slot Mimic filled.</summary>
    public Dictionary<int, (MoveData Data, int Pp, int Ups)>? MoveSlots;
}

/// <summary>What is true of a Pokémon for the turn being played and no longer.</summary>
internal sealed class TurnFlags
{
    public bool Protecting, Enduring;

    /// <summary>The shield its Protect is (plan 06 · R25): King's Shield, Spiky Shield or Baneful Bunker answer whoever touches it.</summary>
    public Shield Shield;

    /// <summary>
    /// The later moves' marks for the turn (plan 06 · R25): Powder on it (its Fire move explodes), Electrify (its move
    /// is Electric), its beak heating up (Beak Blast burns who touches it), its shell trap set.
    /// </summary>
    public bool Powdered, Electrified, HeatingBeak, ShellTrap;

    /// <summary>A foe's physical move struck it while its shell trap was set: the trap goes off next.</summary>
    public bool ShellTrapSprung;

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

    /// <summary>A stage of its rose, or fell, this turn (plan 06 · R26; Showdown's <c>statsRaisedThisTurn</c>, <c>statsLoweredThisTurn</c>): Burning Jealousy and Lash Out ask.</summary>
    public bool StatsRaised, StatsLowered;

    /// <summary>
    /// The later abilities' marks on the move it is using (plan 06 · R27): a -ate ability made it of another type (a
    /// fifth again), Sheer Force took its side effects away (power 5325/4096), and the hit being worked out is Parental
    /// Bond's second (a quarter as strong).
    /// </summary>
    public bool AteBoosted, SheerForced, ParentalChild;

    /// <summary>A Gem went off for the move it is using (plan 06 · R28a): its power is 5325/4096 of itself.</summary>
    public bool GemBoosted;

    /// <summary>
    /// The move it is using touches, but its Protective Pads took the touch out (plan 06 · R28b): Tough Claws and Unseen
    /// Fist, which Showdown reads off the move's own flag, still count it.
    /// </summary>
    public bool PaddedTouch;

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
