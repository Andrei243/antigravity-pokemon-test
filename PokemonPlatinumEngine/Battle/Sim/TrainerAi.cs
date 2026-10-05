using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

/// <summary>
/// How the opponents choose, wild Pokémon and trainers alike, until plan 06 · R9 writes Platinum's own trainer AI:
/// the move that looks best against the Pokémon across the field (damage from power, type and STAB, with some
/// value on status and stat moves while they would still do something, and a little chance thrown in). It never
/// switches or uses an item, and sends its Pokémon out in the order of its party.
/// </summary>
public sealed class TrainerAi : IBattleController
{
    public static TrainerAi Instance { get; } = new();

    public BattleChoice ChooseAction(BattleCore battle, Battler mine)
    {
        var moves = battle.UsableMoves(mine);
        if (moves.Count == 0) return BattleChoice.Fight(mine.Place, -1);

        var foes = battle.ActiveFoes(mine).ToList();
        var ally = battle.SlotsOf(mine.Side).FirstOrDefault(b => b != mine && b.IsActive);

        BattleChoice? best = null;
        float bestScore = float.MinValue;
        foreach (var move in moves)
        {
            var aims = move.Target == MoveTarget.Selected ? foes.Select(t => (Battler?)t).ToList() : new List<Battler?> { null };
            foreach (var aim in aims)
            {
                float score = Score(battle, mine, move, aim, foes, ally) * (0.85f + 0.3f * (float)battle.Random.RollFraction(RollKind.AiChoice));
                if (score <= bestScore) continue;
                bestScore = score;
                best = BattleChoice.Fight(mine.Place, mine.Pokemon!.Moves.IndexOf(move), aim?.Place);
            }
        }
        return best!;
    }

    public int ChooseReplacement(BattleCore battle, Battler place) =>
        Enumerable.Range(0, battle.PlayerParty.Count).Where(battle.CanSendIn).DefaultIfEmpty(-1).First();

    private static float Score(BattleCore battle, Battler user, Move move, Battler? aim, List<Battler> foes, Battler? ally)
    {
        var data = move.Data;
        if (data.Support == MoveEffectSupport.None) return 1f; // does nothing yet: only when there is nothing else
        if (move.Category == MoveCategory.Status)
        {
            var target = aim ?? foes.FirstOrDefault();
            if (data.Effect != null && OfItsOwn(battle, user, data, target) is { } worth) return worth;
            if (data.HealPercent > 0) return user.Pokemon!.CurrentHP * 2 < user.Pokemon.MaxHP ? 90f : 0f;
            if (data.InflictStatus != StatusCondition.None)
                return target != null && target.Pokemon!.Status == StatusCondition.None ? 55f : 0f;
            if (data.ConfuseChancePercent > 0) return target != null && !target.IsConfused ? 40f : 0f;
            if (data.TargetStatChange is { } stat)
            {
                var who = data.StatChangeTargetSelf ? user : target;
                if (who == null) return 0f;
                int stage = who.Pokemon!.StatStages.GetValueOrDefault(stat);
                return data.StatChangeTargetSelf ? (stage < 2 ? 35f : 0f) : (stage > -2 ? 25f : 0f);
            }
            return 5f;
        }

        // Nothing to gain from a dream nobody is having, or from snoring while awake
        if (data.Effect == "RecoverDamageSleep" && foes.All(t => t.Pokemon!.Status != StatusCondition.Sleep)) return 0f;
        if (data.Effect == "DamageWhileAsleep" && user.Pokemon!.Status != StatusCondition.Sleep) return 0f;

        float Hit(Battler t) => PowerGuess(battle, user, move, t) * DamageCalculator.Effectiveness(user, t, move, battle.Rules) * (user.HasType(move.Type) ? 1.5f : 1f);

        float score;
        switch (move.Target)
        {
            case MoveTarget.Selected:
            case MoveTarget.RandomFoe:
                score = aim != null ? Hit(aim) : foes.Select(Hit).DefaultIfEmpty(0f).Max();
                break;
            default:
                score = foes.Sum(Hit) * (foes.Count > 1 ? 0.75f : 1f);
                if (move.Target == MoveTarget.AllOthers && ally != null) score -= Hit(ally);
                break;
        }
        return score * (data.Accuracy > 0 ? data.Accuracy / 100f : 1f);
    }

    /// <summary>
    /// The power a chooser counts a damaging move at when its data says none or not enough: the families whose
    /// power the rules work out from the battle's numbers (plan 06 · R4), roughly as they will.
    /// </summary>
    private static float PowerGuess(BattleCore battle, Battler user, Move move, Battler t)
    {
        var p = user.Pokemon!;
        var q = t.Pokemon!;
        int power = move.Power;
        switch (move.Data.Effect)
        {
            case "20DamageFlat": return 20f;
            case "40DamageFlat": return 40f;
            case "LevelDamageFlat" or "RandomDamage1To150Level": return p.Level;
            case "HalveHp": return q.CurrentHP / 2f;
            case "SetHpEqualToUser": return Math.Max(0, q.CurrentHP - p.CurrentHP);
            case "OneHitKo": return p.Level >= q.Level ? 150f : 0f;
            case "IncreasePowerWithLessHp": return BattleCore.FlailPower(p);
            case "IncreasePowerWithMoreHp": return 1 + 120 * q.CurrentHP / q.MaxHP;
            case "DecreasePowerWithLessUserHp": return Math.Max(1, power * p.CurrentHP / p.MaxHP);
            case "IncreasePowerWithWeight": return BattleCore.WeightPower(q);
            case "PowerBasedOnLowSpeed": return Math.Min(150, 1 + 25 * battle.EffectiveSpeed(t) / Math.Max(1, battle.EffectiveSpeed(user)));
            case "PowerBasedOnFriendship": return p.Friendship * 10 / 25;
            case "PowerBasedOnLowFriendship": return (255 - p.Friendship) * 10 / 25;
            case "IncreasePowerWithMoreStatUp": return Math.Min(200, 60 + 20 * q.StatStages.Values.Where(s => s > 0).Sum());
            case "HigherPowerWhenLowPp": return move.CurrentPP <= 1 ? 200f : move.CurrentPP == 2 ? 80f : 50f;
            case "RandomPowerBasedOnIvs" or "RandomPowerMaybeHeal": return 50f;
            case "DoublePowerWhenBelowHalf": return q.CurrentHP <= q.MaxHP / 2 ? power * 2 : power;
            case "DoublePowerEachTurn": return Math.Min(160, power << user.Volatile.FuryCutterCount);
            case "DoublePowerEachTurnLockInto": return power * 2;
            case "MultiHit" or "BeatUp": return power * 3;
            case "HitTwice" or "PoisonMultiHit": return power * 2;
            case "HitThreeTimes": return 60f;
            case "Psywave": return 70f;
            case "SpitUp": return 100f * user.Volatile.Stockpile;
            // It never blows itself up on purpose, until R9's trainers know when to
            case "HalveDefense": return 0f;
            case "Counter" or "MirrorCoat" or "MetalBurst": return 40f;
            case "HitLastWhiffIfHit": return power / 2f;
            case "HitFirstIfTargetAttacking": return power * 0.7f;
            case "AlwaysFlinchFirstTurnOnly": return battle.Turn == user.Volatile.FirstTurn ? power * 1.5f : 0f;
            case "FailIfNotUsedAllOtherMoves":
            {
                var moves = p.Moves;
                int mine = moves.IndexOf(move);
                int others = Enumerable.Range(0, moves.Count).Count(i => i != mine && (user.Volatile.UsedMoveSlots & (1 << i)) != 0);
                return moves.Count >= 2 && others >= moves.Count - 1 ? power : 0f;
            }
            default: return power;
        }
    }

    /// <summary>
    /// What a status move with an effect of its own is worth right now: something while it would still do what it
    /// is for, nothing once it wouldn't. Null for an effect it has no opinion on.
    /// </summary>
    private static float? OfItsOwn(BattleCore battle, Battler user, MoveData data, Battler? target)
    {
        var field = battle.Field;
        var mine = field.Side(user.Side);
        var theirs = field.Side(user.Side == BattleSide.Player ? BattleSide.Enemy : BattleSide.Player);
        var me = user.Pokemon!;
        var v = user.Volatile;
        var tv = target?.Volatile;
        bool healthy = me.CurrentHP * 2 > me.MaxHP;
        bool foeHasBench = target?.Roster != null && target.Roster.Members.Count(p => !p.IsFainted) > 1;

        switch (data.Effect)
        {
            case "Protect" or "SurviveWith1Hp": return v.ProtectChain == 0 ? 14f : 0f;
            case "SetSubstitute": return !user.HasSubstitute && healthy ? 30f : 0f;
            case "WeatherRain": return field.Weather != BattleWeather.Rain ? 30f : 0f;
            case "WeatherSun": return field.Weather != BattleWeather.Sun ? 30f : 0f;
            case "WeatherSandstorm": return field.Weather != BattleWeather.Sandstorm ? 30f : 0f;
            case "WeatherHail": return field.Weather != BattleWeather.Hail ? 30f : 0f;
            case "SetReflect": return !mine.Reflect ? 35f : 0f;
            case "SetLightScreen": return !mine.LightScreen ? 35f : 0f;
            case "PreventStatus": return !mine.Safeguard ? 15f : 0f;
            case "PreventStatReduction": return !mine.Mist ? 10f : 0f;
            case "PreventCrits": return !mine.LuckyChant ? 10f : 0f;
            case "DoubleSpeed3Turns": return !mine.Tailwind ? 25f : 0f;
            case "TrickRoom": return !field.TrickRoom && target != null && battle.EffectiveSpeed(user) < battle.EffectiveSpeed(target) ? 35f : 0f;
            case "Gravity": return !field.Gravity ? 8f : 0f;
            case "SetSpikes": return theirs.Spikes < 3 && foeHasBench ? 28f : 0f;
            case "ToxicSpikes": return theirs.ToxicSpikes < 2 && foeHasBench ? 26f : 0f;
            case "StealthRock": return !theirs.StealthRock && foeHasBench ? 32f : 0f;
            case "HealIn3Turns": return field.At(user.Place).WishTurns == 0 && !healthy ? 40f : 0f;
            case "HealHalfMoreInSun" or "HealHalfRemoveFlyingType": return healthy ? 0f : 90f;
            case "Rest": return me.CurrentHP * 3 < me.MaxHP ? 80f : 0f;
            case "FaintAndFullHealNextMon" or "FaintFullRestoreNextMon": return 0f;
            case "PassStatsAndStatus": return me.StatStages.Values.Sum() >= 2 ? 30f : 0f;
            case "FleeFromWildBattle" or "DoNothing": return 0f;
            case "ForceSwitch": return target != null && target.Pokemon!.StatStages.Values.Sum() >= 2 ? 40f : 0f;
            case "Infatuate": return tv is { InLoveWith: null } ? 25f : 0f;
            case "StatusLeechSeed": return tv is { SeededBy: null } && !target!.HasType(PokemonType.Grass) && !target.HasSubstitute ? 40f : 0f;
            case "StatusNightmare": return tv is { Nightmare: false } && target!.Pokemon!.Status == StatusCondition.Sleep ? 45f : 0f;
            case "Curse":
                return user.HasType(PokemonType.Ghost)
                    ? (tv is { Cursed: false } && healthy ? 35f : 0f)
                    : (me.StatStages.GetValueOrDefault(StatType.Attack) < 2 ? 30f : 0f);
            case "AllFaint3Turns": return v.PerishCount < 0 && tv is { PerishCount: < 0 } ? 12f : 0f;
            case "Taunt": return tv is { TauntTurns: 0 } ? 22f : 0f;
            case "Torment": return tv is { Tormented: false } ? 15f : 0f;
            case "Encore": return tv is { Encored: null, LastMove: not null } ? 25f : 0f;
            case "Disable": return tv is { Disabled: null, LastMove: not null } ? 22f : 0f;
            case "StatusSleepNextTurn": return tv is { YawnTurns: 0 } && target!.Pokemon!.Status == StatusCondition.None ? 40f : 0f;
            case "PreventHealing": return tv is { HealBlockTurns: 0 } ? 12f : 0f;
            case "PreventItemUse": return tv is { EmbargoTurns: 0 } ? 10f : 0f;
            case "PreventEscape": return tv is { TrappedBy: null } && foeHasBench ? 15f : 0f;
            case "NextAttackAlwaysHits": return tv is { LockOnTurns: 0 } ? 12f : 0f;
            case "Foresight": return tv is { Identified: false } && target!.HasType(PokemonType.Ghost) ? 20f : 0f;
            case "IgnoreEvationRemoveDarkImmune": return tv is { MiracleEye: false } && target!.HasType(PokemonType.Dark) ? 20f : 0f;
            case "SupressAbility": return tv is { AbilitySuppressed: false } ? 15f : 0f;
            case "TransferStatus": return me.Status != StatusCondition.None && target?.Pokemon!.Status == StatusCondition.None ? 50f : 0f;
            case "CritUp2": return !v.FocusEnergy ? 20f : 0f;
            case "GroundTrapUserContinuousHeal": return !v.Ingrained ? 22f : 0f;
            case "RestoreHpEveryTurn": return !v.AquaRing ? 22f : 0f;
            case "GiveGroundImmunity": return v.MagnetRiseTurns == 0 ? 12f : 0f;
            case "KoMonThatDefeatedUser": return me.CurrentHP * 4 < me.MaxHP ? 35f : 0f;
            case "RemoveAllPpOnDefeat": return me.CurrentHP * 4 < me.MaxHP && !v.Grudge ? 15f : 0f;
            case "MakeSharedMovesUnuseable": return !v.Imprisoning ? 10f : 0f;
            case "HalveElectricDamage": return !v.MudSport ? 6f : 0f;
            case "HalveFireDamage": return !v.WaterSport ? 6f : 0f;
            case "HealStatus": return me.Status is StatusCondition.Poison or StatusCondition.Toxic or StatusCondition.Burn or StatusCondition.Paralyze ? 50f : 0f;
            case "CurePartyStatus": return user.Roster != null && user.Roster.Members.Any(p => !p.IsFainted && p.Status != StatusCondition.None) ? 45f : 0f;
            case "ResetStatChanges": return target != null && target.Pokemon!.StatStages.Values.Sum() >= 2 ? 35f : 0f;
            case "RemoveHazardsScreensEvaDown": return theirs.Reflect || theirs.LightScreen || field.Weather == BattleWeather.Fog ? 30f : 8f;
            case "Bide": return healthy ? 20f : 0f;
            case "MaxAtkLoseHalfMaxHp": return healthy && me.StatStages.GetValueOrDefault(StatType.Attack) < 6 ? 45f : 0f;
            case "Stockpile": return v.Stockpile < 3 ? 25f : 0f;
            case "Swallow": return v.Stockpile > 0 && !healthy ? 35f * v.Stockpile : 0f;
            case "AverageHp": return target != null && me.CurrentHP < target.Pokemon!.CurrentHP ? 40f : 0f;
            case "RandomStatUp2": return 20f;
            case "CopyStatChanges": return target != null && target.Pokemon!.StatStages.Values.Sum() >= 2 ? 30f : 0f;
            case "SpAtkDown2OppositeGender":
                return target != null && target.Pokemon!.StatStages.GetValueOrDefault(StatType.SpAttack) > -2 && me.Gender != Gender.Genderless
                    && target.Pokemon.Gender != Gender.Genderless && me.Gender != target.Pokemon.Gender ? 25f : 0f;
            case "FaintAndAtkSpAtkDown2": return 0f;
            default: return null;
        }
    }
}
