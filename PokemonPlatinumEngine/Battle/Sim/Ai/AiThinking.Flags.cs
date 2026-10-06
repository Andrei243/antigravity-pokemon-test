using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using static PokemonPlatinumEngine.Battle.Sim.Ai.MoveEffectId;

namespace PokemonPlatinumEngine.Battle.Sim.Ai;

/// <summary>
/// The shorter routines of the AI script, each a translation of the original's (<c>EvalAttack_Main</c> and the rest,
/// in <c>src/battle/trainer_ai/script.s</c>): the same questions in the same order, the same rolls, the same scores.
/// </summary>
internal sealed partial class AiThinking
{
    /// <summary>
    /// <c>EvalAttack_Main</c>: a move that knocks the target out at the strongest roll gets +4 (+6 for a move of
    /// priority's effect; a move that strikes later only a third of the time), one that isn't the strongest −1,
    /// Explosion, Focus Punch and Sucker Punch mostly −2, and a four-times hit mostly +2.
    /// </summary>
    private void EvalAttack()
    {
        if (TargetIsPartner) return;

        if (Kills(roll: false) == true)
        {
            // Explosion's own knockouts are left to Expert
            if (Effect == HalveDefense) return;
            if (Effect is HitLastWhiffIfHit or HitFirstIfTargetAttacking or HitIn3Turns)
            {
                if (RandomBelow(170)) return;
                Score(4);
                return;
            }
            if (Effect == Priority1) Score(2);
            Score(4);
            return;
        }

        if (DamageScore(roll: false) == DamageRank.NotHighest)
        {
            Score(-1);
            return;
        }

        if ((Effect is HalveDefense or HitLastWhiffIfHit or HitFirstIfTargetAttacking) && !RandomBelow(51)) Score(-2);

        if (Effectiveness() == Eff.Quadruple && !RandomBelow(80)) Score(2);
    }

    /// <summary><c>SetupFirstTurn_SetupEffects</c>.</summary>
    private static readonly HashSet<MoveEffectId> SetupEffects = new()
    {
        AtkUp, DefUp, SpeedUp, SpAtkUp, SpDefUp, AccUp, EvaUp, AtkDown, DefDown, SpeedDown, SpAtkDown, SpDefDown,
        AccDown, EvaDown, Conversion, SetLightScreen, SpDefUp2, CritUp2, StatusConfuse, AtkUp2, DefUp2, SpeedUp2,
        SpAtkUp2, SpDefUp2, AccUp2, EvaUp2, AtkDown2, DefDown2, SpeedDown2, SpAtkDown2, SpDefDown2, EvaDown2,
        AccDown2, SetReflect, StatusPoison, StatusParalyze, SetSubstitute, StatusLeechSeed, EvaUp2Minimize, Curse,
        AtkUp2StatusConfusion, Camouflage, StatusSleepNextTurn, DefUpDoubleRolloutPower, Torment,
        SpAtkUpCauseConfusion, StatusBurn, GroundTrapUserContinuousHeal, MakeSharedMovesUnuseable, ConfuseAll,
        AtkDefDown, DefSpdUp, AtkDefUp, SpAtkSpDefUp, DoubleSpeed3Turns, RandomStatUp2, PreventCrits,
        GiveGroundImmunity, RemoveHazardsScreensEvaDown, Whirlpool
    };

    /// <summary><c>SetupFirstTurn_Main</c>: on the battle's first turn, a move that sets something up mostly gets +2.</summary>
    private void SetupFirstTurn()
    {
        if (TargetIsPartner) return;
        if (TurnCount != 0) return;
        if (!SetupEffects.Contains(Effect)) return;
        if (RandomBelow(80)) return;
        Score(2);
    }

    /// <summary>
    /// <c>PrioritizeExtremes_Main</c>: a move whose damage the AI doesn't compare (a status move, a variable or fixed
    /// amount, a charged or reckless hit) mostly gets +2.
    /// </summary>
    private void PrioritizeExtremes()
    {
        if (TargetIsPartner) return;
        if (DamageScore(roll: false) != DamageRank.NoComparison) return;
        if (RandomBelow(100)) return;
        Score(2);
    }

    /// <summary><c>Risky_RiskyEffects</c>.</summary>
    private static readonly HashSet<MoveEffectId> RiskyEffects = new()
    {
        StatusSleep, HalveDefense, CopyMove, OneHitKo, HighCritical, StatusConfuse, CallRandomMove,
        RandomDamage1To150Level, Counter, KoMonThatDefeatedUser, AtkUp2StatusConfusion, Infatuate,
        RandomPowerMaybeHeal, RaiseAllStatsHit, MaxAtkLoseHalfMaxHp, MirrorCoat, HitLastWhiffIfHit, DoublePowerIfHit,
        ConfuseAll, PowerBasedOnLowSpeed, RandomStatUp2, MetalBurst, DoublePowerIfMovingSecond, UseMoveFirst,
        HitFirstIfTargetAttacking
    };

    /// <summary><c>Risky_Main</c>: a move that might go wrong gets +2 half the time.</summary>
    private void Risky()
    {
        if (TargetIsPartner) return;
        if (!RiskyEffects.Contains(Effect)) return;
        if (RandomBelow(128)) return;
        Score(2);
    }

    /// <summary>
    /// <c>BatonPass_Main</c>: with someone to pass to, a move that sets up is worth more, Baton Pass itself more for
    /// each stage of Attack or Sp. Atk raised. (No trainer of the game thinks this way.)
    /// </summary>
    private void BatonPass()
    {
        if (TargetIsPartner) return;
        if (AliveBench(Who.Attacker) == 0) return;
        if (DamageScore(roll: false) != DamageRank.NoComparison) return;
        if (KnowsEffect(Who.Attacker, PassStatsAndStatus) != true && RandomBelow(80)) return;

        if (MoveIs("Swords Dance") || MoveIs("Dragon Dance") || MoveIs("Calm Mind") || MoveIs("Nasty Plot"))
        {
            SetupAtHighHp();
            return;
        }
        if (Effect == MoveEffectId.Protect)
        {
            if (PreviousMove(Who.Attacker)?.Name is "Protect" or "Detect")
            {
                Score(-2);
                return;
            }
            Score(2);
            return;
        }
        if (MoveIs("Baton Pass"))
        {
            if (TurnCount == 0)
            {
                Score(-2);
                return;
            }
            // +1 to +3 by the Attack stage first, else by the Sp. Atk stage
            foreach (var stat in new[] { StatType.Attack, StatType.SpAttack })
            {
                if (Stage(Who.Attacker, stat) > 8) { Score(3); return; }
                if (Stage(Who.Attacker, stat) > 7) { Score(2); return; }
                if (Stage(Who.Attacker, stat) > 6) { Score(1); return; }
            }
            return;
        }
        if (RandomBelow(20)) return;
        Score(3);
        // The original falls through into the boosting moves' scoring after its +3
        SetupAtHighHp();

        void SetupAtHighHp()
        {
            if (TurnCount == 0)
            {
                Score(5);
                return;
            }
            if (HpPercent(Who.Attacker) < 60)
            {
                Score(-10);
                return;
            }
            Score(1);
        }
    }

    /// <summary><c>CheckHP_DiscourageAtHighHP</c>: what isn't worth it while the user is healthy.</summary>
    private static readonly HashSet<MoveEffectId> DiscourageAtHighHp = new()
    {
        HalveDefense, RestoreHalfHp, Rest, KoMonThatDefeatedUser, IncreasePowerWithLessHp, SurviveWith1Hp,
        HealHalfMoreInSun, FaintAndAtkSpAtkDown2, RemoveAllPpOnDefeat, HealHalfRemoveFlyingType,
        FaintAndFullHealNextMon, FaintFullRestoreNextMon
    };

    /// <summary><c>CheckHP_DiscourageAtMediumHP</c>.</summary>
    private static readonly HashSet<MoveEffectId> DiscourageAtMediumHp = new()
    {
        HalveDefense, AtkUp, DefUp, SpeedUp, SpAtkUp, SpDefUp, AccUp, EvaUp, AtkDown, DefDown, SpeedDown, SpAtkDown,
        SpDefDown, AccDown, EvaDown, Bide, Conversion, SetLightScreen, PreventStatReduction, CritUp2, AtkUp2, DefUp2,
        SpeedUp2, SpAtkUp2, SpDefUp2, AccUp2, EvaUp2, AtkDown2, DefDown2, SpeedDown2, SpAtkDown2, SpDefDown2,
        EvaDown2, AccDown2, Conversion2, PreventStatus, MaxAtkLoseHalfMaxHp, AtkDefDown, DefSpdUp, AtkDefUp,
        SpAtkSpDefUp, AtkSpdUp, PreventCrits, SwapAtkSpAtkStatChanges, SwapDefSpDefStatChanges,
        SpAtkDown2OppositeGender
    };

    /// <summary><c>CheckHP_DiscourageAtLowHP</c>.</summary>
    private static readonly HashSet<MoveEffectId> DiscourageAtLowHp = new()
    {
        AtkUp, DefUp, SpeedUp, SpAtkUp, SpDefUp, AccUp, EvaUp, AtkDown, DefDown, SpeedDown, SpAtkDown, SpDefDown,
        AccDown, EvaDown, Bide, Conversion, SetLightScreen, PreventStatReduction, CritUp2, AtkUp2, DefUp2, SpeedUp2,
        SpAtkUp2, SpDefUp2, AccUp2, EvaUp2, AtkDown2, DefDown2, SpeedDown2, SpAtkDown2, SpDefDown2, EvaDown2,
        AccDown2, RaiseAtkWhenHit, Conversion2, NextAttackAlwaysHits, PreventStatus, MaxAtkLoseHalfMaxHp,
        CopyStatChanges, MirrorCoat, DecreasePowerWithLessUserHp, AtkDefDown, DefSpdUp, AtkDefUp, SpAtkSpDefUp,
        AtkSpdUp, HalveElectricDamage, HalveFireDamage, RandomStatUp2, MetalBurst, SpAtkDown2OppositeGender
    };

    /// <summary><c>CheckHP_Target_DiscourageAtHighHP</c>: nothing.</summary>
    private static readonly HashSet<MoveEffectId> TargetDiscourageAtHighHp = new();

    /// <summary><c>CheckHP_Target_DiscourageAtMediumHP</c>.</summary>
    private static readonly HashSet<MoveEffectId> TargetDiscourageAtMediumHp = new()
    {
        AtkUp, DefUp, SpeedUp, SpAtkUp, SpDefUp, AccUp, EvaUp, AtkDown, DefDown, SpeedDown, SpAtkDown, SpDefDown,
        AccDown, EvaDown, PreventStatReduction, CritUp2, AtkUp2, DefUp2, SpeedUp2, SpAtkUp2, SpDefUp2, AccUp2, EvaUp2,
        AtkDown2, DefDown2, SpeedDown2, SpAtkDown2, SpDefDown2, EvaDown2, AccDown2, StatusPoison, AverageHp,
        AllFaint3Turns, PreventStatus, AtkDefDown, DefSpdUp, AtkDefUp, SpAtkSpDefUp, AtkSpdUp, RandomStatUp2,
        IncreasePowerWithMoreHp, SpAtkDown2OppositeGender
    };

    /// <summary><c>CheckHP_Target_DiscourageAtLowHP</c>.</summary>
    private static readonly HashSet<MoveEffectId> TargetDiscourageAtLowHp = new()
    {
        StatusSleep, HalveDefense, AtkUp, DefUp, SpeedUp, SpAtkUp, SpDefUp, AccUp, EvaUp, AtkDown, DefDown, SpeedDown,
        SpAtkDown, SpDefDown, AccDown, EvaDown, Bide, Conversion, StatusBadlyPoison, SetLightScreen, OneHitKo, HalveHp,
        PreventStatReduction, CritUp2, StatusConfuse, AtkUp2, DefUp2, SpeedUp2, SpAtkUp2, SpDefUp2, AccUp2, EvaUp2,
        AtkDown2, DefDown2, SpeedDown2, SpAtkDown2, SpDefDown2, EvaDown2, AccDown2, StatusPoison, StatusParalyze,
        AverageHp, Conversion2, NextAttackAlwaysHits, DecreaseLastMovePp, AllFaint3Turns, AtkUp2StatusConfusion,
        DoublePowerEachTurn, Infatuate, PreventStatus, CopyStatChanges, MirrorCoat, StatusBurn, AtkDefDown, DefSpdUp,
        AtkDefUp, SpAtkSpDefUp, AtkSpdUp, RandomStatUp2, IncreasePowerWithMoreHp, SpAtkDown2OppositeGender
    };

    /// <summary>
    /// <c>CheckHP_Main</c>: a move out of place at the user's HP (above 70%, 31 to 70%, or less) mostly gets −2; then
    /// the same against the target's HP. (No trainer of the game thinks this way.) The original sends a move aimed at
    /// the partner to the tag strategy's partner routine.
    /// </summary>
    private void CheckHp()
    {
        if (TargetIsPartner)
        {
            TagStrategyPartner();
            return;
        }

        var mine = HpPercent(Who.Attacker) > 70 ? DiscourageAtHighHp : HpPercent(Who.Attacker) > 30 ? DiscourageAtMediumHp : DiscourageAtLowHp;
        if (mine.Contains(Effect) && !RandomBelow(50)) Score(-2);

        var theirs = HpPercent(Who.Defender) > 70 ? TargetDiscourageAtHighHp : HpPercent(Who.Defender) > 30 ? TargetDiscourageAtMediumHp : TargetDiscourageAtLowHp;
        if (theirs.Contains(Effect) && !RandomBelow(50)) Score(-2);
    }

    /// <summary>
    /// <c>Weather_Main</c>: on the battle's first turn, a weather move gets +5 for the user's own first turn unless that
    /// weather is up already. (The original's dispatch falls through into the sun's check for any other move.)
    /// </summary>
    private void Weather()
    {
        if (TargetIsPartner) return;
        if (TurnCount != 0) return;

        var already = Effect switch
        {
            WeatherRain => AiWeather.Raining,
            WeatherSandstorm => AiWeather.Sandstorm,
            WeatherHail => AiWeather.Hailing,
            _ => AiWeather.Sunny
        };
        if (CurrentWeather == already) return;
        if (!FirstTurnIn(Who.Attacker)) return;
        Score(5);
    }

    /// <summary><c>Harrassment_Effects</c>.</summary>
    private static readonly HashSet<MoveEffectId> HarassmentEffects = new()
    {
        StatusSleep, AtkDown, DefDown, AccDown, EvaDown, StatusConfuse, AtkDown2, DefDown2, SpeedDown2, SpDefDown2,
        StatusPoison, StatusParalyze, StatusLeechSeed, Encore, DecreaseLastMovePp, SetSpikes, AtkUp2StatusConfusion,
        Infatuate, Torment, SpAtkUpCauseConfusion, StatusBurn, NaturePower, StatusSleepNextTurn, RemoveHeldItem,
        MakeSharedMovesUnuseable, SecretPower, ConfuseAll, AtkDefDown, Camouflage, PreventItemUse, TransferStatus,
        ToxicSpikes, RemoveHazardsScreensEvaDown, SpAtkDown2OppositeGender
    };

    /// <summary><c>Harrassment_Main</c>: a move that gets in the way gets +2 half the time. (No trainer of the game thinks this way.)</summary>
    private void Harassment()
    {
        if (TargetIsPartner) return;
        if (!HarassmentEffects.Contains(Effect)) return;
        if (RandomBelow(128)) return;
        Score(2);
    }

    /// <summary>
    /// <c>RoamingPokemon_Main</c>: a roaming Pokémon runs, unless it is bound, held by Mean Look, or held by the
    /// other's Shadow Tag or Arena Trap (Levitate floats above the trap).
    /// </summary>
    private void Roaming()
    {
        if (Has(Who.Attacker, Vol.Bind) || Has(Who.Attacker, Vol.MeanLook)) return;
        if (RealAbility(Who.Defender) == "Shadow Tag") return;
        if (RealAbility(Who.Attacker) != "Levitate" && RealAbility(Who.Defender) == "Arena Trap") return;
        Escape();
    }

    /// <summary><c>Safari_Main</c>: two commands that do nothing, then away (the Great Marsh decides its Pokémon's flight elsewhere).</summary>
    private void Safari() => Escape();

    /// <summary><c>CatchTutorial_Main</c>: the lesson's helper stops attacking once the wild Pokémon is down to a fifth of its HP.</summary>
    private void CatchTutorial()
    {
        if (HpPercent(Who.Defender) <= 20) Escape();
    }
}
