using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim.Ai;

/// <summary>Which Pokémon a question of the AI is about (the original's <c>AI_BATTLER_*</c>).</summary>
internal enum Who { Defender, Attacker, DefenderPartner, AttackerPartner }

/// <summary>What the damage comparison of the current move found (<c>FlagMoveDamageScore</c>).</summary>
internal enum DamageRank { NoComparison, NotHighest, Highest }

/// <summary>What the AI believes of an ability (<c>CheckBattlerAbility</c>: <c>AI_NOT_HAVE</c>, <c>AI_HAVE</c>, <c>AI_UNKNOWN</c>).</summary>
internal enum Knowledge { NotHave, Have, Unknown }

/// <summary>The attacker against the defender by the turn order (<c>COMPARE_SPEED_*</c>).</summary>
internal enum SpeedOrder { Faster, Slower, Tie }

/// <summary>The sky as the AI reads it (<c>AI_WEATHER_*</c>).</summary>
internal enum AiWeather { Clear, Sunny, Raining, Sandstorm, Hailing, DeepFog }

/// <summary>The major conditions the AI asks about, as masks (<c>MON_CONDITION_*</c>).</summary>
[Flags]
internal enum Cond
{
    None = 0,
    Sleep = 1,
    Poison = 2,
    Burn = 4,
    Freeze = 8,
    Paralysis = 16,
    Toxic = 32,
    Any = Sleep | Poison | Burn | Freeze | Paralysis | Toxic,
    AnyPoison = Poison | Toxic,
    /// <summary>What makes Facade stronger.</summary>
    FacadeBoost = Toxic | Poison | Burn | Paralysis
}

/// <summary>The passing conditions the AI asks about (<c>VOLATILE_CONDITION_*</c>).</summary>
internal enum Vol { Confusion, Attract, Curse, Substitute, Torment, Nightmare, MeanLook, Foresight, FocusEnergy, Bind }

/// <summary>What a move left on a Pokémon that the AI asks about (<c>MOVE_EFFECT_*</c>).</summary>
internal enum MonFx
{
    LeechSeed, PerishSong, MagnetRise, Yawn,
    /// <summary>Someone's next move can't miss it (Lock-On and Mind Reader mark the target).</summary>
    LockOn,
    MiracleEye, Ingrain,
    /// <summary>A foe sealed its moves away (Imprison marks both the user's foes).</summary>
    Imprisoned,
    HealBlock, Embargo, AquaRing, AbilitySuppressed, WaterSport, PowerTrick, MudSport,
    /// <summary>It used Imprison itself.</summary>
    Imprison,
    /// <summary>It used Camouflage.</summary>
    Camouflage
}

/// <summary>What a side has up that the AI asks about (<c>SIDE_CONDITION_*</c>).</summary>
internal enum SideFx { Safeguard, StealthRock, ToxicSpikes, Spikes, Reflect, LightScreen, FutureSight, Tailwind, Mist, LuckyChant }

/// <summary>
/// The numbers a move's type matchup comes out as for the AI (<c>TYPE_MULTI_*</c>): a base of 40 put through the
/// type chart. A move of the user's own type that hits normally comes out as <see cref="Stab"/> and so equals none
/// of the others, as in the original.
/// </summary>
internal static class Eff
{
    public const int Immune = 0, Quarter = 10, Half = 20, Neutral = 40, Stab = 60, Double = 80, Quadruple = 160;
}

/// <summary>
/// One look of the trainer AI at its choices (plan 06 · R9): the original's <c>AIContext</c> for one attacker
/// against one defender, with a score for each of the attacker's four moves, and the commands of its AI script
/// (<c>src/battle/trainer_ai/script.s</c>, run by <c>trainer_ai.c</c>) as methods, each answering as the original's
/// command does, its quirks kept. The routines of the script's flags are in <c>AiThinking.*.cs</c>, each a
/// translation of the original's routine; every roll they make is the battle's own (<see cref="RollKind.AiChoice"/>),
/// so a battle against the AI still replays.
/// <para>
/// What the AI may know of a Pokémon on the other side is what the original's lets it know (<see cref="AiMemory"/>):
/// the moves it has seen used, an ability or an item once a line has named it; anything else it guesses from the
/// species, as the original does.
/// </para>
/// </summary>
internal sealed partial class AiThinking
{
    public BattleCore Battle { get; }
    public Battler Attacker { get; }
    public Battler Defender { get; }
    public AiMemory Memory { get; }

    /// <summary>The flags whose routines are run, in their order: the trainer's, with the tag strategy in a double battle.</summary>
    public AiFlags Flags { get; }

    /// <summary>A score for each of the attacker's moves: 100 to begin with, 0 for a move it can't use.</summary>
    public int[] Scores { get; } = new int[4];

    /// <summary>The damage roll (85 to 100) each move is counted at when a command asks for one, drawn once per look.</summary>
    public int[] Rolls { get; } = new int[4];

    /// <summary>The slot of the move being scored, and the move (null for an empty slot or one with no PP left).</summary>
    public int Slot { get; private set; }
    private MoveData? move;

    /// <summary>The move being scored. The routines only run while there is one.</summary>
    public MoveData Move => move!;

    /// <summary>The script said to run (a roamer, the catching lesson's helper): the look ends at once.</summary>
    public bool Escaped { get; private set; }

    /// <summary>The script said the look is over for every move (<c>AI_STATUS_FLAG_BREAK</c>).</summary>
    private bool broken;

    private readonly Random rng;

    public AiThinking(BattleCore battle, AiMemory memory, Battler attacker, Battler defender, AiFlags flags, bool initialScores = true)
    {
        Battle = battle;
        Memory = memory;
        Attacker = attacker;
        Defender = defender;
        Flags = flags;
        rng = battle.Random;

        // TrainerAI_Init: every move 100 (or nothing), a move that can't be used 0, and a damage roll for each slot
        var moves = attacker.Pokemon!.Moves;
        var usable = battle.UsableMoves(attacker);
        for (int i = 0; i < 4; i++)
        {
            Scores[i] = initialScores ? 100 : 0;
            if (i >= moves.Count || !usable.Contains(moves[i])) Scores[i] = 0;
            Rolls[i] = 100 - rng.Roll(RollKind.AiChoice, 16);
        }
    }

    /// <summary>
    /// Runs every flag's routine over every move (<c>TrainerAI_EvalMoves</c>, for each flag in turn): a move with
    /// no PP left scores 0 whatever the routines say. Stops early once the script has said to run.
    /// </summary>
    public void Think()
    {
        foreach (var flag in Enum.GetValues<AiFlags>())
        {
            if (flag == AiFlags.None || (Flags & flag) == 0) continue;
            for (Slot = 0; Slot < 4; Slot++)
            {
                var moves = Attacker.Pokemon!.Moves;
                move = Slot < moves.Count && moves[Slot].CurrentPP > 0 ? moves[Slot].Data : null;
                if (move == null)
                {
                    Scores[Slot] = 0;
                    continue;
                }
                Run(flag);
                if (broken) break;
            }
            if (broken) break;
        }
        move = null;
    }

    private void Run(AiFlags flag)
    {
        switch (flag)
        {
            case AiFlags.Basic: Basic(); break;
            case AiFlags.EvalAttack: EvalAttack(); break;
            case AiFlags.Expert: Expert(); break;
            case AiFlags.SetupFirstTurn: SetupFirstTurn(); break;
            case AiFlags.Risky: Risky(); break;
            case AiFlags.PrioritizeExtremes: PrioritizeExtremes(); break;
            case AiFlags.BatonPass: BatonPass(); break;
            case AiFlags.TagStrategy: TagStrategy(); break;
            case AiFlags.CheckHp: CheckHp(); break;
            case AiFlags.Weather: Weather(); break;
            case AiFlags.Harassment: Harassment(); break;
            case AiFlags.Roaming: Roaming(); break;
            case AiFlags.Safari: Safari(); break;
            case AiFlags.CatchTutorial: CatchTutorial(); break;
        }
    }

    // ================================================================ the commands

    /// <summary><c>IfRandomLessThan n</c>: a roll of 0 to 255 came out under n (so true about n/256 of the time).</summary>
    public bool RandomBelow(int n) => rng.Roll(RollKind.AiChoice, 256) < n;

    /// <summary><c>AddToMoveScore</c>: changes the current move's score, never below 0.</summary>
    public void Score(int delta) => Scores[Slot] = Math.Max(0, Scores[Slot] + delta);

    /// <summary><c>Escape</c>: the AI runs (a roamer, the catching lesson), and no move is scored further.</summary>
    public void Escape()
    {
        Escaped = true;
        broken = true;
    }

    // ---------------------------------------------------------------- who is who

    /// <summary>The Pokémon a question names (<c>AIScript_Battler</c>); a partner in a single battle is an empty place.</summary>
    public Battler? Of(Who who) => who switch
    {
        Who.Attacker => Attacker,
        Who.Defender => Defender,
        Who.AttackerPartner => PartnerOf(Attacker),
        _ => PartnerOf(Defender)
    };

    private Battler? PartnerOf(Battler b) => Battle.IsDouble ? Battle.SlotsOf(b.Side)[1 - b.Slot] : null;

    /// <summary>A place with someone standing in it, fainted or not (an empty one answers as the original's zeroed data).</summary>
    private Pokemon? MonOf(Who who) => Of(who)?.Pokemon;

    /// <summary><c>IfTargetIsPartner</c>: the move is being scored against the attacker's own partner.</summary>
    public bool TargetIsPartner => Attacker.Side == Defender.Side;

    /// <summary><c>LoadBattleType</c> with <c>BATTLE_TYPE_DOUBLES</c>.</summary>
    public bool IsDouble => Battle.IsDouble;

    /// <summary><c>LoadBattleType</c> with <c>BATTLE_TYPE_FRONTIER</c>: never, until plan 06 · R18.</summary>
    public bool IsFrontier => false;

    // ---------------------------------------------------------------- the move being scored

    /// <summary><c>LoadCurrentMoveEffect</c> and <c>IfCurrentMoveEffectEqualTo</c>: the original's battle effect of the move.</summary>
    public MoveEffectId Effect => Move.EffectId;

    /// <summary><c>IfMoveEqualTo</c>: the move is this one, by name.</summary>
    public bool MoveIs(string name) => Move.Name == name;

    /// <summary><c>LoadMovePower</c>: the original's power (1 for every move of variable power, 0 for a status move).</summary>
    public int Power => PowerOf(Move);

    /// <summary><c>LoadTypeFrom LOAD_MOVE_TYPE</c>: the type in the move's data, whatever would change it.</summary>
    public PokemonType MoveType => Move.Type;

    /// <summary><c>LoadCurrentMoveClass</c>.</summary>
    public MoveCategory MoveCategory => Move.Category;

    /// <summary><c>LoadCurrentMovePP</c>.</summary>
    public int CurrentPp => Attacker.Pokemon!.Moves[Slot].CurrentPP;

    /// <summary>The original's power of a move: 0 for a status move, 1 for one whose power is worked out in battle (the importer writes 0 for those).</summary>
    public static int PowerOf(MoveData? m) => m == null || m.Category == MoveCategory.Status ? 0 : m.Power == 0 ? 1 : m.Power;

    /// <summary><c>LoadEffectOfLoadedMove</c>: the battle effect of a move found by another command (no move: <see cref="MoveEffectId.Hit"/>, as move 0's).</summary>
    public static MoveEffectId EffectOf(MoveData? m) => m?.EffectId ?? MoveEffectId.Hit;

    // ---------------------------------------------------------------- HP, conditions and stats

    /// <summary><c>IfHPPercent…</c>: HP as a whole percentage of the most, rounded down (0 for an empty place).</summary>
    public int HpPercent(Who who) => MonOf(who) is { MaxHP: > 0 } p ? p.CurrentHP * 100 / p.MaxHP : 0;

    /// <summary><c>IfStatus</c>: the Pokémon has one of these conditions.</summary>
    public bool HasStatus(Who who, Cond mask) => (CondOf(MonOf(who)) & mask) != 0;

    private static Cond CondOf(Pokemon? p) => p?.Status switch
    {
        StatusCondition.Sleep => Cond.Sleep,
        StatusCondition.Poison => Cond.Poison,
        StatusCondition.Burn => Cond.Burn,
        StatusCondition.Freeze => Cond.Freeze,
        StatusCondition.Paralyze => Cond.Paralysis,
        StatusCondition.Toxic => Cond.Toxic,
        _ => Cond.None
    };

    /// <summary><c>IfVolatileStatus</c>.</summary>
    public bool Has(Who who, Vol condition)
    {
        if (Of(who) is not { Pokemon: not null } b) return false;
        var v = b.Volatile;
        return condition switch
        {
            Vol.Confusion => b.IsConfused,
            Vol.Attract => v.InLoveWith != null,
            Vol.Curse => v.Cursed,
            Vol.Substitute => b.HasSubstitute,
            Vol.Torment => v.Tormented,
            Vol.Nightmare => v.Nightmare,
            Vol.MeanLook => v.TrappedBy != null,
            Vol.Foresight => v.Identified,
            Vol.FocusEnergy => v.FocusEnergy,
            Vol.Bind => v.BindTurns > 0,
            _ => false
        };
    }

    /// <summary><c>IfMoveEffect</c>.</summary>
    public bool Has(Who who, MonFx effect)
    {
        if (Of(who) is not { Pokemon: not null } b) return false;
        var v = b.Volatile;
        return effect switch
        {
            MonFx.LeechSeed => v.SeededBy != null,
            MonFx.PerishSong => v.PerishCount >= 0,
            MonFx.MagnetRise => v.MagnetRiseTurns > 0,
            MonFx.Yawn => v.YawnTurns > 0,
            MonFx.LockOn => v.LockOnTurns > 0,
            MonFx.MiracleEye => v.MiracleEye,
            MonFx.Ingrain => v.Ingrained,
            MonFx.Imprisoned => v.Imprisoned,
            MonFx.HealBlock => v.HealBlockTurns > 0,
            MonFx.Embargo => v.EmbargoTurns > 0,
            MonFx.AquaRing => v.AquaRing,
            MonFx.AbilitySuppressed => v.AbilitySuppressed,
            MonFx.WaterSport => v.WaterSport,
            MonFx.PowerTrick => v.PowerTrick,
            MonFx.MudSport => v.MudSport,
            MonFx.Imprison => v.Imprisoning,
            MonFx.Camouflage => v.Camouflaged,
            _ => false
        };
    }

    /// <summary><c>IfSideCondition</c>: the side the Pokémon stands on has this up.</summary>
    public bool SideHas(Who who, SideFx condition)
    {
        var side = Battle.Field.Side((Of(who) ?? Defender).Side);
        return condition switch
        {
            SideFx.Safeguard => side.Safeguard,
            SideFx.StealthRock => side.StealthRock,
            SideFx.ToxicSpikes => side.ToxicSpikes > 0,
            SideFx.Spikes => side.Spikes > 0,
            SideFx.Reflect => side.Reflect,
            SideFx.LightScreen => side.LightScreen,
            SideFx.FutureSight => Battle.SlotsOf((Of(who) ?? Defender).Side).Any(b => Battle.Field.At(b.Place).DoomTurns > 0),
            SideFx.Tailwind => side.Tailwind,
            SideFx.Mist => side.Mist,
            SideFx.LuckyChant => side.LuckyChant,
            _ => false
        };
    }

    /// <summary><c>LoadSpikesLayers … SIDE_CONDITION_SPIKES</c>.</summary>
    public int SpikesLayers(Who who) => Battle.Field.Side((Of(who) ?? Defender).Side).Spikes;

    /// <summary><c>LoadSpikesLayers … SIDE_CONDITION_TOXIC_SPIKES</c>.</summary>
    public int ToxicSpikesLayers(Who who) => Battle.Field.Side((Of(who) ?? Defender).Side).ToxicSpikes;

    /// <summary><c>IfFieldConditionsMask FIELD_CONDITION_TRICK_ROOM</c>.</summary>
    public bool TrickRoom => Battle.Field.TrickRoom;

    /// <summary><c>IfFieldConditionsMask FIELD_CONDITION_GRAVITY</c>.</summary>
    public bool Gravity => Battle.Field.Gravity;

    /// <summary>
    /// <c>IfStatStage…</c>: a stage as the original keeps it, from 0 (−6) through 6 (unchanged) to 12 (+6), so a
    /// routine's numbers read as the script's. An empty place's are 0, as zeroed data are.
    /// </summary>
    public int Stage(Who who, StatType stat) => MonOf(who) is { } p ? 6 + p.StatStages.GetValueOrDefault(stat) : 0;

    /// <summary><c>SumPositiveStatStages</c>: how many stages above the start its stats are, all told.</summary>
    public int PositiveStages(Who who) =>
        MonOf(who) is { } p ? Enum.GetValues<StatType>().Sum(s => Math.Max(0, p.StatStages.GetValueOrDefault(s))) : 0;

    /// <summary><c>DiffStatStages</c>: its stage of a stat less the attacker's.</summary>
    public int StageDiff(Who who, StatType stat) => Stage(who, stat) - Stage(Who.Attacker, stat);

    /// <summary>
    /// The stat <c>IfBattlerHasHigherStat</c> and its kin compare (<c>TrainerAI_GetStats</c>): the stat itself, before
    /// its stage; HP is the HP it has now.
    /// </summary>
    public int StatValue(Who who, StatType stat)
    {
        if (MonOf(who) is not { } p) return 0;
        return stat switch
        {
            StatType.HP => p.CurrentHP,
            StatType.Attack => p.Attack,
            StatType.Defense => p.Defense,
            StatType.SpAttack => p.SpAttack,
            StatType.SpDefense => p.SpDefense,
            StatType.Speed => p.Speed,
            _ => 0
        };
    }

    /// <summary><c>IfLevel</c>: the attacker's level against the defender's.</summary>
    public int LevelOf(Who who) => MonOf(who)?.Level ?? 0;

    /// <summary><c>LoadGender</c>.</summary>
    public Gender GenderOf(Who who) => MonOf(who)?.Gender ?? Gender.Male;

    /// <summary>
    /// <c>LoadTypeFrom</c>: its first or second type in battle (a single-typed Pokémon has the same type twice, as in
    /// the original; an empty place is Normal). Asked of the defender's partner, the original gives the first type both
    /// times: <see cref="DefenderPartnerType2"/> keeps that.
    /// </summary>
    public PokemonType Type1(Who who) => Of(who) is { Pokemon: not null } b ? b.Types[0] : PokemonType.Normal;
    public PokemonType Type2(Who who) => Of(who) is { Pokemon: not null } b ? b.Types[^1] : PokemonType.Normal;

    /// <summary><c>LoadTypeFrom LOAD_DEFENDER_PARTNER_TYPE_2</c>, which reads the first type (the original's slip).</summary>
    public PokemonType DefenderPartnerType2 => Type1(Who.DefenderPartner);

    /// <summary><c>FlagBattlerIsType</c>: the Pokémon has this type in battle.</summary>
    public bool HasType(Who who, PokemonType type) => Of(who) is { Pokemon: not null } b && b.HasType(type);

    /// <summary><c>IfBattlerFainted</c> (asked of a partner): its place is waiting for someone to come in after a faint.</summary>
    public bool Fainted(Who who) => Of(who) is { Pokemon: { } p } && p.IsFainted;

    /// <summary><c>IfActivatedFlashFire</c>.</summary>
    public bool FlashFire(Who who) => Of(who)?.FlashFire == true;

    /// <summary><c>IfTargetIsTaunted</c>.</summary>
    public bool Taunted(Who who) => Of(who)?.Volatile.TauntTurns > 0;

    /// <summary><c>IfBattlerUnderEffect … CHECK_DISABLE</c>.</summary>
    public bool Disabled(Who who) => Of(who)?.Volatile.DisableTurns > 0;

    /// <summary><c>IfBattlerUnderEffect … CHECK_ENCORE</c>.</summary>
    public bool Encored(Who who) => Of(who)?.Volatile.EncoreTurns > 0;

    /// <summary><c>IfCurrentMoveMatchesEffect CHECK_DISABLE</c>: the move being scored is the attacker's disabled one.</summary>
    public bool MoveIsDisabled => Attacker.Volatile.Disabled == Move;

    /// <summary><c>IfCurrentMoveMatchesEffect CHECK_ENCORE</c>.</summary>
    public bool MoveIsEncored => Attacker.Volatile.Encored == Move;

    /// <summary><c>LoadStockpileCount</c>.</summary>
    public int Stockpile(Who who) => Of(who)?.Volatile.Stockpile ?? 0;

    /// <summary><c>LoadProtectChain</c>: Protect, Detect and Endure used with success in a row.</summary>
    public int ProtectChain(Who who) => Of(who)?.Volatile.ProtectChain ?? 0;

    /// <summary><c>LoadRecycleItem</c>: the item Recycle would bring back (null for none).</summary>
    public ItemData? RecycleItem(Who who) => Of(who)?.Volatile.ConsumedItem;

    /// <summary><c>IfCanUseLastResort</c>: it has used every other move it knows, and knows more than one.</summary>
    public bool CanUseLastResort(Who who)
    {
        if (Of(who) is not { Pokemon: { } p } b) return false;
        int known = p.Moves.Count;
        int used = Enumerable.Range(0, known).Count(i => (b.Volatile.UsedMoveSlots & (1 << i)) != 0);
        return used >= known - 1 && known > 1;
    }

    /// <summary><c>LoadFlingPower</c>: what its item would hit for if thrown (0 under an Embargo, with Klutz, or with none).</summary>
    public int FlingPower(Who who) => Of(who) is { } b && BattleEffects.ItemInHand(b) is { } item ? item.FlingPower : 0;

    // ---------------------------------------------------------------- the turn

    /// <summary><c>LoadTurnCount</c>: turns played to their end.</summary>
    public int TurnCount => Battle.Turn;

    /// <summary><c>LoadIsFirstTurnInBattle</c>: this is its first turn on the field.</summary>
    public bool FirstTurnIn(Who who) => Of(who) is { } b && b.Volatile.FirstTurn >= Battle.Turn;

    /// <summary><c>LoadBattlerTurnCount</c>: turns since its first on the field.</summary>
    public int TurnsIn(Who who) => Of(who) is { } b ? Battle.Turn - b.Volatile.FirstTurn : Battle.Turn;

    /// <summary><c>LoadCurrentWeather</c>.</summary>
    public AiWeather CurrentWeather => Battle.Field.Weather switch
    {
        BattleWeather.Rain => AiWeather.Raining,
        BattleWeather.Sandstorm => AiWeather.Sandstorm,
        BattleWeather.Sun => AiWeather.Sunny,
        BattleWeather.Hail => AiWeather.Hailing,
        BattleWeather.Fog => AiWeather.DeepFog,
        _ => AiWeather.Clear
    };

    /// <summary>
    /// <c>IfSpeedCompareEqualTo</c>: the attacker against the defender as the turn order would put them before any
    /// move is chosen (<c>BattleSystem_CompareBattlerSpeed</c> with its Quick Claw left alone).
    /// </summary>
    public SpeedOrder SpeedCompare => Battle.CompareSpeed(Attacker, Defender);

    /// <summary><c>LoadBattlerSpeedRank</c>: where the Pokémon stands among everyone on the field by that order, 0 the fastest.</summary>
    public int SpeedRank(Who who)
    {
        var all = Battle.AllBattlers.OrderBy(b => b.Place.Number).ToList();
        for (int i = 0; i < all.Count - 1; i++)
            for (int j = i + 1; j < all.Count; j++)
                if (Battle.CompareSpeed(all[i], all[j]) != SpeedOrder.Faster) (all[i], all[j]) = (all[j], all[i]);
        return Of(who) is { } b ? all.IndexOf(b) : 0;
    }

    // ---------------------------------------------------------------- moves known

    /// <summary>
    /// <c>IfMoveKnown</c> (and <c>IfMoveNotKnown</c> as <c>== false</c>): its own moves for the attacker and its
    /// partner (nothing to say of a fainted partner), the moves it has seen used for the defender, and nothing at all
    /// for the defender's partner: null where the original jumps neither way.
    /// </summary>
    public bool? Knows(Who who, string moveName)
    {
        switch (who)
        {
            case Who.Attacker:
                return Attacker.Pokemon!.Moves.Any(m => m.Name == moveName);
            case Who.AttackerPartner:
                if (Of(who) is not { Pokemon: { CurrentHP: > 0 } p }) return null;
                return p.Moves.Any(m => m.Name == moveName);
            case Who.Defender:
                return Memory.MovesSeen(Defender).Any(m => m.Name == moveName);
            default:
                return null;
        }
    }

    /// <summary><c>IfMoveEffectKnown</c> (<c>IfMoveEffectNotKnown</c> as <c>== false</c>): as <see cref="Knows"/>, by the moves' battle effect; null for either partner.</summary>
    public bool? KnowsEffect(Who who, MoveEffectId effect) => who switch
    {
        Who.Attacker => Attacker.Pokemon!.Moves.Any(m => m.Data.EffectId == effect),
        Who.Defender => Memory.MovesSeen(Defender).Any(m => m.EffectId == effect),
        _ => null
    };

    /// <summary><c>IfAttackerHasDamagingMoves</c> (<c>IfAttackerHasNoDamagingMoves</c> as its opposite).</summary>
    public bool HasDamagingMoves => Attacker.Pokemon!.Moves.Any(m => PowerOf(m.Data) != 0);

    /// <summary><c>LoadBattlerPreviousMove</c>: the last move it got to use (null for none).</summary>
    public MoveData? PreviousMove(Who who) => Of(who)?.Volatile.LastMove;

    /// <summary><c>LoadDefenderLastUsedMoveClass</c>: the category of the defender's last move; with none, physical (move 0's).</summary>
    public MoveCategory DefenderLastMoveCategory => Defender.Volatile.LastMove?.Category ?? MoveCategory.Physical;

    // ---------------------------------------------------------------- abilities and items

    /// <summary>
    /// <c>LoadBattlerAbility</c>: the attacker's own ability and its partner's; for the other side, the one a line
    /// has shown, or else Shadow Tag, Magnet Pull or Arena Trap (which it can tell from being held), or else a guess
    /// from the species: one of its two abilities at random (a roll each time it is asked). None under Gastro Acid.
    /// Load it once into a local and compare that, as the script compares the loaded value.
    /// </summary>
    public string? AbilityOf(Who who)
    {
        if (Of(who) is not { Pokemon: { } p } b) return null;
        if (b.Volatile.AbilitySuppressed) return null;
        if (b == Attacker || who == Who.AttackerPartner) return p.AbilityName;
        if (Memory.AbilityShown(b) is { } shown) return shown;
        if (p.AbilityName is "Shadow Tag" or "Magnet Pull" or "Arena Trap") return p.AbilityName;
        var own = p.Abilities.Take(2).ToList();
        if (own.Count == 2) return rng.Roll(RollKind.AiChoice, 2) == 1 ? own[0] : own[1];
        return own.FirstOrDefault();
    }

    /// <summary>
    /// <c>CheckBattlerAbility</c>: whether the Pokémon has this ability. For the defender and its partner the shown
    /// one, or the giveaways, or the species: with two abilities neither of which is the one asked for, it doesn't;
    /// with two of which one is, nobody can tell.
    /// </summary>
    public Knowledge CheckAbility(Who who, string ability)
    {
        string? held;
        if (Of(who) is not { Pokemon: { } p } b) held = null;
        else if (b.Volatile.AbilitySuppressed) held = null;
        else if (who is Who.Defender or Who.DefenderPartner)
        {
            if (Memory.AbilityShown(b) is { } shown) held = shown;
            else if (p.AbilityName is "Shadow Tag" or "Magnet Pull" or "Arena Trap") held = p.AbilityName;
            else
            {
                var own = p.Abilities.Take(2).ToList();
                held = own.Count == 2 ? (own[0] != ability && own[1] != ability ? own[0] : null) : own.FirstOrDefault();
            }
        }
        else held = p.AbilityName;

        if (held == null) return Knowledge.Unknown;
        return held == ability ? Knowledge.Have : Knowledge.NotHave;
    }

    /// <summary><c>LoadAbility</c>: the ability in force, known or not (<c>Battler_Ability</c>).</summary>
    public string? RealAbility(Who who) => Of(who)?.Ability?.Name;

    /// <summary><c>LoadHeldItem</c>: the item it holds, known or not.</summary>
    public ItemData? HeldItem(Who who) => Of(who)?.Pokemon?.HeldItem;

    /// <summary>
    /// <c>LoadHeldItemEffect</c>: the hold effect of the attacker's own item, or of the item a line has shown of
    /// anyone else (null for none known).
    /// </summary>
    public string? HoldEffect(Who who)
    {
        if (Of(who) is not { Pokemon: { } p } b) return null;
        return b == Attacker ? p.HeldItem?.HoldEffect : Memory.ItemShown(b)?.HoldEffect;
    }

    /// <summary><c>IfHeldItemEqualTo</c>: its own side's items as they are, the other side's as shown.</summary>
    public bool Holds(Who who, string itemName)
    {
        if (Of(who) is not { Pokemon: { } p } b) return false;
        var item = b.Side == Attacker.Side ? p.HeldItem : Memory.ItemShown(b);
        return item?.Name == itemName;
    }

    // ---------------------------------------------------------------- the party

    /// <summary>
    /// <c>CountAlivePartyBattlers</c>: party members that can fight besides the one in this place (and, in a double
    /// battle, its partner).
    /// </summary>
    public int AliveBench(Who who) => Bench(who).Count(p => !p.IsFainted);

    /// <summary><c>IfPartyMemberStatus</c>: a party member on the bench that can fight has one of these conditions.</summary>
    public bool BenchHasStatus(Who who, Cond mask) => Bench(who).Any(p => !p.IsFainted && (CondOf(p) & mask) != 0);

    /// <summary><c>IfPartyMemberNotStatus</c>.</summary>
    public bool BenchLacksStatus(Who who, Cond mask) => Bench(who).Any(p => !p.IsFainted && (CondOf(p) & mask) == 0);

    /// <summary><c>IfAnyPartyMemberIsWounded</c>: anyone but the Pokémon itself has lost HP (fainted ones too, as the original counts them).</summary>
    public bool AnyPartyWounded(Who who) => Roster(who) is { } r && r.Members.Any(p => p != MonOf(who) && p.CurrentHP != p.MaxHP);

    /// <summary><c>IfAnyPartyMemberUsedPP</c>: anyone but the Pokémon itself has a move with PP spent.</summary>
    public bool AnyPartyUsedPp(Who who) => Roster(who) is { } r && r.Members.Any(p => p != MonOf(who) && p.Moves.Any(m => m.CurrentPP != m.MaxPP));

    private Party? Roster(Who who) => Of(who)?.Roster;

    private IEnumerable<Pokemon> Bench(Who who)
    {
        if (Of(who) is not { } b || b.Roster == null) return Enumerable.Empty<Pokemon>();
        var standing = Battle.SlotsOf(b.Side).Where(o => o.Roster == b.Roster && (o == b || Battle.IsDouble)).Select(o => o.Pokemon).ToHashSet();
        return b.Roster.Members.Where(p => !standing.Contains(p));
    }

    // ---------------------------------------------------------------- type matchups

    /// <summary>
    /// <c>IfMoveEffectivenessEquals</c>: the current move's matchup against the defender as one of <see cref="Eff"/>'s
    /// numbers (STAB counted in, so a normal hit of its own type is <see cref="Eff.Stab"/>).
    /// </summary>
    public int Effectiveness() => Matchup(Attacker, Defender, Move);

    /// <summary><c>CalcMaxEffectiveness</c>: the best of the attacker's moves' matchups against the defender.</summary>
    public int MaxEffectiveness() =>
        Attacker.Pokemon!.Moves.Select(m => Matchup(Attacker, Defender, m.Data)).DefaultIfEmpty(Eff.Immune).Max();

    /// <summary>
    /// <c>BattleSystem_ApplyTypeChart</c> on a base of 40 with the move's type for the AI (<c>TrainerAI_MoveType</c>:
    /// Natural Gift, Judgment, Hidden Power and Weather Ball worked out, the rest by their data), then the original's
    /// mapping of a doubled or halved STAB number back onto the plain ones.
    /// </summary>
    internal int Matchup(Battler attacker, Battler defender, MoveData data)
    {
        if (data == BattleCore.StruggleData || defender.Pokemon == null) return Eff.Neutral;
        var type = AiMoveType(attacker, data);
        int damage = TypeChart(attacker, defender, data, type, Eff.Neutral, out bool immune, out _, out _);
        if (immune) return Eff.Immune;
        return damage switch
        {
            Eff.Stab * 2 => Eff.Double,
            Eff.Stab * 4 => Eff.Quadruple,
            Eff.Stab / 2 => Eff.Half,
            Eff.Stab / 4 => Eff.Quarter,
            _ => damage
        };
    }

    /// <summary>
    /// The original's type chart on a number: the user's own type (Adaptability doubles), each of the target's types
    /// by its tenths, then Filter or Solid Rock, an Expert Belt and Tinted Lens on what the matchup came out as.
    /// Levitate and Magnet Rise make a Ground move miss rather than count as immune, as in the original.
    /// </summary>
    internal int TypeChart(Battler attacker, Battler defender, MoveData data, PokemonType type, int damage, out bool immune, out bool superEffective, out bool notVeryEffective)
    {
        immune = superEffective = notVeryEffective = false;
        string? mine = attacker.Ability?.Name;
        bool breaks = mine == "Mold Breaker";
        string? theirs = breaks ? null : defender.Ability?.Name;
        if (mine == "Normalize") type = PokemonType.Normal;
        int power = PowerOf(data);

        if (attacker.HasType(type)) damage = mine == "Adaptability" ? damage * 2 : damage * 15 / 10;

        string? theirHold = BattleEffects.HoldEffectOf(defender);
        bool grounded = theirHold == "SpeedDownGrounded";
        if (type == PokemonType.Ground && !grounded && (theirs == "Levitate" || (defender.Volatile.MagnetRiseTurns > 0 && !defender.Volatile.Ingrained)))
        {
            immune = true;
            return damage;
        }

        bool seesGhosts = defender.Volatile.Identified || mine == "Scrappy";
        foreach (var t in defender.Types.Distinct())
        {
            float multiplier = Data.TypeChart.GetEffectiveness(type, t, null, Battle.Rules);
            if (multiplier == 1f) continue;
            if (multiplier == 0f)
            {
                if (t == PokemonType.Ghost && seesGhosts) continue;
                if (t == PokemonType.Flying && type == PokemonType.Ground && (grounded || Battle.Field.Gravity)) continue;
                if (t == PokemonType.Dark && type == PokemonType.Psychic && defender.Volatile.MiracleEye) continue;
                immune = true;
                superEffective = notVeryEffective = false;
                damage = 0;
                continue;
            }
            int tenths = (int)MathF.Round(multiplier * 10f);
            if (damage != 0) damage = Formulas.Divide(damage * tenths, 10);
            if (power == 0) continue;
            if (tenths > 10)
            {
                if (notVeryEffective) notVeryEffective = false;
                else superEffective = true;
            }
            else
            {
                if (superEffective) superEffective = false;
                else notVeryEffective = true;
            }
        }

        // Wonder Guard: only a super-effective hit gets through
        if (theirs == "Wonder Guard" && power != 0 && !superEffective)
        {
            immune = true;
            return damage;
        }
        if (superEffective && power != 0)
        {
            if (theirs is "Filter" or "Solid Rock") damage = Formulas.Divide(damage * 3, 4);
            if (BattleEffects.HoldEffectOf(attacker) == "PowerUpSe") damage = damage * (100 + BattleEffects.HoldParamOf(attacker)) / 100;
        }
        if (notVeryEffective && power != 0 && mine == "Tinted Lens") damage *= 2;
        return damage;
    }

    /// <summary><c>TrainerAI_MoveType</c>: Natural Gift by the berry, Judgment by the plate, Hidden Power by the IVs, Weather Ball by the sky; the rest by their data.</summary>
    internal PokemonType AiMoveType(Battler user, MoveData data)
    {
        var p = user.Pokemon!;
        switch (data.Name)
        {
            case "Natural Gift":
                return BattleEffects.ItemInHand(user)?.NaturalGiftType ?? PokemonType.Normal;
            case "Judgment":
                return BattleEffects.HoldEffectOf(user) is { } hold && hold.StartsWith("Arceus") && Enum.TryParse(hold["Arceus".Length..], out PokemonType plate)
                    ? plate : PokemonType.Normal;
            case "Hidden Power":
                return BattleCore.HiddenPowerType(p);
            case "Weather Ball":
                return Battle.Field.WeatherInEffect switch
                {
                    BattleWeather.Rain => PokemonType.Water,
                    BattleWeather.Sandstorm => PokemonType.Rock,
                    BattleWeather.Sun => PokemonType.Fire,
                    BattleWeather.Hail => PokemonType.Ice,
                    _ => data.Type
                };
            default:
                return data.Type;
        }
    }

    // ---------------------------------------------------------------- damage

    /// <summary>The moves whose damage the AI never works out (<c>sNoDamageCalcMoveEffects</c>).</summary>
    private static readonly HashSet<MoveEffectId> NoDamageCalc = new()
    {
        MoveEffectId.HalveDefense, MoveEffectId.RecoverDamageSleep, MoveEffectId.ChargeTurnHighCrit,
        MoveEffectId.ChargeTurnHighCritFlinch, MoveEffectId.RechargeAfter, MoveEffectId.ChargeTurnDefUp,
        MoveEffectId.SkipChargeTurnInSun, MoveEffectId.SpitUp, MoveEffectId.HitLastWhiffIfHit,
        MoveEffectId.LowerOwnAtkAndDef, MoveEffectId.DecreasePowerWithLessUserHp, MoveEffectId.HitFirstIfTargetAttacking,
        MoveEffectId.RecoilHalf
    };

    /// <summary>The moves whose damage it works out although their power is variable (<c>sAltPowerMoveEffects</c>).</summary>
    private static readonly HashSet<MoveEffectId> AltPower = new()
    {
        MoveEffectId.RandomPowerBasedOnIvs, MoveEffectId.PowerBasedOnLowSpeed, MoveEffectId.NaturalGift,
        MoveEffectId.Judgement, MoveEffectId.N40DamageFlat, MoveEffectId.LevelDamageFlat,
        MoveEffectId.RandomDamage1To150Level, MoveEffectId.PowerBasedOnFriendship,
        MoveEffectId.PowerBasedOnLowFriendship, MoveEffectId.N20DamageFlat, MoveEffectId.IncreasePowerWithWeight
    };

    /// <summary>Whether the AI works out a move's damage at all: one of the variable powers it knows, or a power above 1 and none of the moves it leaves alone.</summary>
    public static bool Counted(MoveData? m) =>
        m != null && (AltPower.Contains(m.EffectId) || (PowerOf(m) > 1 && !NoDamageCalc.Contains(m.EffectId)));

    /// <summary>
    /// <c>FlagMoveDamageScore</c>: whether the current move does the most damage of the attacker's moves against the
    /// defender (a tie counts as the most), at the damage rolls of this look or at the strongest; no comparison for
    /// a move whose damage the AI doesn't work out.
    /// </summary>
    public DamageRank DamageScore(bool roll)
    {
        if (!Counted(Move)) return DamageRank.NoComparison;
        var all = AllDamage(Attacker, Attacker.Pokemon!.Moves.Select(m => m.Data).ToList(), roll);
        return all.Any(d => d > all[Slot]) ? DamageRank.NotHighest : DamageRank.Highest;
    }

    /// <summary>
    /// <c>IfCurrentMoveKills</c> (<c>IfCurrentMoveDoesNotKill</c> as <c>== false</c>): whether the move's damage at
    /// this look's roll (or the strongest) is at least the defender's HP; null for a move whose damage it doesn't work out.
    /// </summary>
    public bool? Kills(bool roll)
    {
        if (!Counted(Move)) return null;
        return Defender.Pokemon!.CurrentHP <= DamageOf(Attacker, Move, roll ? Rolls[Slot] : 100);
    }

    /// <summary><c>IfHasSuperEffectiveMove</c>: one of the attacker's moves hits the Pokémon across from it, or its partner, super effectively.</summary>
    public bool HasSuperEffectiveMove() => TrainerAi.HasSuperEffectiveMove(Battle, Attacker, always: true, rng);

    /// <summary>
    /// <c>IfPartyMemberDealsMoreDamage</c>: a party member that can fight would hit harder with its best move than the
    /// attacker with its own (both worked out as though used by the attacker against the defender).
    /// </summary>
    public bool BenchDealsMoreDamage(bool roll)
    {
        int mine = AllDamage(Attacker, Attacker.Pokemon!.Moves.Select(m => m.Data).ToList(), roll).Max();
        if (Attacker.Roster == null) return false;
        foreach (var p in Attacker.Roster.Members)
        {
            if (p == Attacker.Pokemon || p.IsFainted) continue;
            var stand = new Battler(Attacker.Side, Attacker.Slot) { Pokemon = p, Field = Attacker.Field };
            if (AllDamage(stand, p.Moves.Select(m => m.Data).ToList(), roll).DefaultIfEmpty(0).Max() > mine) return true;
        }
        return false;
    }

    /// <summary>
    /// <c>IfBattlerDealsMoreDamage</c>: the last move of the Pokémon named would hit the defender harder than the
    /// attacker's best (the original works the other's damage out with the attacker's IVs).
    /// </summary>
    public bool DealsMoreDamage(Who who, bool roll)
    {
        int mine = AllDamage(Attacker, Attacker.Pokemon!.Moves.Select(m => m.Data).ToList(), roll).Max();
        if (Of(who) is not { Pokemon: not null } other || other.Volatile.LastMove is not { } last) return false;
        return DamageOf(other, last, roll ? Rolls[Slot] : 100) > mine;
    }

    /// <summary>
    /// <c>CheckIfHighestDamageWithPartner</c>: the current move hits the defender at least as hard as any move of the
    /// attacker's and of its partner's.
    /// </summary>
    public DamageRank HighestWithPartner(bool roll)
    {
        if (!Counted(Move)) return DamageRank.NoComparison;
        int mine = AllDamage(Attacker, Attacker.Pokemon!.Moves.Select(m => m.Data).ToList(), roll)[Slot];
        foreach (var b in new[] { Attacker, PartnerOf(Attacker) })
        {
            if (b?.Pokemon == null) continue;
            if (AllDamage(b, b.Pokemon.Moves.Select(m => m.Data).ToList(), roll).Any(d => d > mine)) return DamageRank.NotHighest;
        }
        return DamageRank.Highest;
    }

    /// <summary><c>TrainerAI_CalcAllDamage</c>: each of four moves' damage against the defender, 0 for one it doesn't work out.</summary>
    private int[] AllDamage(Battler user, List<MoveData> moves, bool roll)
    {
        var damage = new int[4];
        for (int i = 0; i < moves.Count && i < 4; i++)
            if (Counted(moves[i])) damage[i] = DamageOf(user, moves[i], roll ? Rolls[i] : 100);
        return damage;
    }

    /// <summary>
    /// <c>TrainerAI_CalcDamage</c>: one move's damage against the defender at a roll of 85 to 100, with no critical
    /// hit. The moves of a fixed amount are that amount (Psywave rolls for it, as does Magnitude's power); Hidden
    /// Power, Gyro Ball, Return, Frustration, Low Kick and Grass Knot have their power worked out; an immune target
    /// takes nothing.
    /// </summary>
    internal int DamageOf(Battler user, MoveData data, int variance)
    {
        var p = user.Pokemon!;
        var target = Defender;
        if (target.Pokemon == null) return 0;
        int? power = null;
        int fixedDamage = 0;
        PokemonType? type = null;
        switch (data.Name)
        {
            case "Natural Gift":
                if (user.Ability?.Name != "Klutz" && user.Volatile.EmbargoTurns == 0 && p.HeldItem is { NaturalGiftPower: > 0 } berry)
                {
                    power = berry.NaturalGiftPower;
                    type = berry.NaturalGiftType;
                }
                break;
            case "Judgment":
                type = AiMoveType(user, data);
                break;
            case "Hidden Power":
                power = BattleCore.HiddenPowerPower(p, Battle.Rules);
                type = BattleCore.HiddenPowerType(p);
                break;
            case "Gyro Ball":
                power = Math.Min(150, 1 + 25 * Battle.EffectiveSpeed(target) / Math.Max(1, Battle.EffectiveSpeed(user)));
                break;
            case "Dragon Rage": fixedDamage = 40; break;
            case "Seismic Toss" or "Night Shade": fixedDamage = p.Level; break;
            case "Psywave": fixedDamage = p.Level * (rng.Roll(RollKind.AiChoice, 11) + 5) / 10; break;
            case "Return": power = p.Friendship * 10 / 25; break;
            case "Frustration": power = (255 - p.Friendship) * 10 / 25; break;
            case "Magnitude":
                int m = rng.Roll(RollKind.AiChoice, 100);
                power = m < 5 ? 10 : m < 15 ? 30 : m < 35 ? 50 : m < 65 ? 70 : m < 85 ? 90 : m < 95 ? 110 : 150;
                break;
            case "Sonic Boom": fixedDamage = 20; break;
            case "Low Kick" or "Grass Knot": power = BattleCore.WeightPower(target.Pokemon); break;
        }

        var use = type is { } t && t != data.Type ? data.OfType(t) : data;
        // An immunity (Levitate, Magnet Rise, Wonder Guard and the type chart's) leaves nothing
        TypeChart(user, target, use, use.Type, Eff.Neutral, out bool immune, out _, out _);
        if (immune) return 0;
        int damage = fixedDamage;
        if (damage == 0)
        {
            var result = DamageCalculator.Calculate(user, target, new Move(use), NoDraws, spread: false, rules: Battle.Rules,
                noCrit: true, noVariance: true, basePower: power);
            if (result.IsImmune) return 0;
            damage = result.Damage;
        }
        return Formulas.Divide(damage * variance, 100);
    }

    /// <summary>The damage worked out for the AI draws nothing (no critical hit, no roll): this stands in for a generator it never asks.</summary>
    private static readonly Random NoDraws = new(0);
}
