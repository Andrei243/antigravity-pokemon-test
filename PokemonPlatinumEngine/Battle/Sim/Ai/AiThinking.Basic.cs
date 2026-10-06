using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using static PokemonPlatinumEngine.Battle.Sim.Ai.MoveEffectId;

namespace PokemonPlatinumEngine.Battle.Sim.Ai;

internal sealed partial class AiThinking
{
    /// <summary>
    /// <c>Basic_Main</c>: what every trainer weighs. A move the target is immune to, by its types or by the ability the
    /// AI believes it has, gets −10 or −12; then the move goes to the check for its battle effect, which marks it down
    /// if it would fail or be wasted. A move whose damage the AI doesn't compare skips the immunity checks, except
    /// Fissure and Horn Drill (Guillotine and Sheer Cold aren't named, so they skip them).
    /// </summary>
    private void Basic()
    {
        if (TargetIsPartner) return;

        if (MoveIs("Fissure") || MoveIs("Horn Drill") || DamageScore(roll: false) != DamageRank.NoComparison)
        {
            BasicCheckForImmunity();
            return;
        }
        BasicCheckSoundproof();
    }

    // ================================================================ immunities

    /// <summary>
    /// <c>Basic_CheckForImmunity</c>: −10 for a move the target's types are immune to; then, unless the attacker has
    /// Mold Breaker, −12 for one the target's ability would take in or keep out.
    /// </summary>
    private void BasicCheckForImmunity()
    {
        if (Effectiveness() == Eff.Immune) { Score(-10); return; }
        if (AbilityOf(Who.Attacker) == "Mold Breaker") { BasicNoImmunityAbility(); return; }

        var ability = AbilityOf(Who.Defender);
        if (ability is "Volt Absorb" or "Motor Drive") { BasicCheckElectricAbsorption(); return; }
        if (ability == "Water Absorb") { BasicCheckWaterAbsorption(); return; }
        if (ability == "Flash Fire") { BasicCheckFireAbsorption(); return; }
        if (ability == "Wonder Guard") { BasicCheckWonderGuard(); return; }
        if (ability == "Levitate") { BasicCheckGroundAbsorption(); return; }
        // The original asks for Levitate a second time where Dry Skin was meant: this is never taken, and Dry Skin
        // never marks a Water move down
        if (ability == "Levitate") { BasicCheckWaterAbsorption2(); return; }
        BasicNoImmunityAbility();
    }

    /// <summary><c>Basic_CheckElectricAbsorption</c>: an Electric move into Volt Absorb or Motor Drive gets −12.</summary>
    private void BasicCheckElectricAbsorption() => BasicAbsorbs(PokemonType.Electric);

    /// <summary><c>Basic_CheckWaterAbsorption</c>: a Water move into Water Absorb gets −12.</summary>
    private void BasicCheckWaterAbsorption() => BasicAbsorbs(PokemonType.Water);

    /// <summary><c>Basic_CheckFireAbsorption</c>: a Fire move into Flash Fire gets −12.</summary>
    private void BasicCheckFireAbsorption() => BasicAbsorbs(PokemonType.Fire);

    /// <summary><c>Basic_CheckGroundAbsorption</c>: a Ground move against Levitate gets −12.</summary>
    private void BasicCheckGroundAbsorption() => BasicAbsorbs(PokemonType.Ground);

    /// <summary><c>Basic_CheckWaterAbsorption2</c>: a Water move into Dry Skin would get −12, but nothing leads here.</summary>
    private void BasicCheckWaterAbsorption2() => BasicAbsorbs(PokemonType.Water);

    /// <summary>The absorption checks' one shape: −12 for a move of the type in its data, else on as though nothing stood in the way.</summary>
    private void BasicAbsorbs(PokemonType type)
    {
        if (MoveType == type) { Score(-12); return; }
        BasicNoImmunityAbility();
    }

    /// <summary><c>Basic_CheckWonderGuard</c>: against Wonder Guard, anything but a super-effective move gets −12 (a status move too, judged by its type).</summary>
    private void BasicCheckWonderGuard()
    {
        if (Effectiveness() is Eff.Double or Eff.Quadruple) { BasicNoImmunityAbility(); return; }
        Score(-12);
    }

    /// <summary>
    /// <c>Basic_NoImmunityAbility</c>: the damage comparison once more, though both of its answers lead on to the
    /// sound check. It is still made, since working the damage out can roll.
    /// </summary>
    private void BasicNoImmunityAbility()
    {
        DamageScore(roll: false);
        BasicCheckSoundproof();
    }

    /// <summary>The sound moves <c>Basic_CheckSoundproof</c> names one by one (Hyper Voice and Perish Song aren't among them).</summary>
    private static readonly HashSet<string> BasicSoundMoves = new()
    {
        "Growl", "Roar", "Sing", "Supersonic", "Screech", "Snore", "Uproar", "Metal Sound", "Grass Whistle", "Bug Buzz",
        "Chatter"
    };

    /// <summary><c>Basic_CheckSoundproof</c>: against Soundproof, and with no Mold Breaker, a sound move gets −10.</summary>
    private void BasicCheckSoundproof()
    {
        // The ability loaded last goes on with the move: Magnitude's check reads it
        var loaded = AbilityOf(Who.Defender);
        if (loaded == "Soundproof")
        {
            loaded = AbilityOf(Who.Attacker);
            if (loaded != "Mold Breaker" && BasicSoundMoves.Contains(Move.Name)) { Score(-10); return; }
        }
        BasicScoreMoveEffect(loaded);
    }

    // ================================================================ by battle effect

    /// <summary>
    /// <c>Basic_ScoreMoveEffect</c>: sends the move to the check for its battle effect (an effect not listed is left
    /// alone). <paramref name="loaded"/> is the ability the sound check loaded last, which Magnitude's check reads.
    /// </summary>
    private void BasicScoreMoveEffect(string? loaded)
    {
        switch (Effect)
        {
            case StatusSleep: BasicCheckCannotSleep(); break;
            case HalveDefense: BasicCheckCannotExplode(); break;
            case RecoverDamageSleep: BasicCheckDreamEater(); break;
            case AtkUp: BasicCheckHighStatStageAttack(); break;
            case DefUp: BasicCheckHighStatStageDefense(); break;
            case SpeedUp: BasicCheckHighStatStageSpeed(); break;
            case SpAtkUp: BasicCheckHighStatStageSpAttack(); break;
            case SpDefUp: BasicCheckHighStatStageSpDefense(); break;
            case AccUp: BasicCheckHighStatStageAccuracy(); break;
            case EvaUp: BasicCheckHighStatStageEvasion(); break;
            case AtkDown: BasicCheckLowStatStageAttack(); break;
            case DefDown: BasicCheckLowStatStageDefense(); break;
            case SpeedDown: BasicCheckLowStatStageSpeed(); break;
            case SpAtkDown: BasicCheckLowStatStageSpAttack(); break;
            case SpDefDown: BasicCheckLowStatStageSpDefense(); break;
            case AccDown: BasicCheckLowStatStageAccuracy(); break;
            case EvaDown: BasicCheckLowStatStageEvasion(); break;
            case ResetStatChanges: BasicCheckStatStageImbalance(); break;
            case Bide: BasicCheckNonStandardDamageOrChargeTurn(); break;
            case ForceSwitch: BasicCheckCanForceSwitch(); break;
            case RestoreHalfHp: BasicCheckCanRecoverHP(); break;
            case StatusBadlyPoison: BasicCheckCannotPoison(); break;
            case SetLightScreen: BasicCheckAlreadyUnderLightScreen(); break;
            case OneHitKo: BasicCheckOHKOWouldFail(); break;
            case ChargeTurnHighCrit: BasicCheckNonStandardDamageOrChargeTurn(); break;
            case HalveHp: BasicCheckNonStandardDamageOrChargeTurn(); break;
            case N40DamageFlat: BasicCheckNonStandardDamageOrChargeTurn(); break;
            case PreventStatReduction: BasicCheckAlreadyUnderMist(); break;
            case CritUp2: BasicCheckAlreadyPumpedUp(); break;
            case StatusConfuse: BasicCheckCannotConfuse(); break;
            case AtkUp2: BasicCheckHighStatStageAttack(); break;
            case DefUp2: BasicCheckHighStatStageDefense(); break;
            case SpeedUp2: BasicCheckHighStatStageSpeed(); break;
            case SpAtkUp2: BasicCheckHighStatStageSpAttack(); break;
            case SpDefUp2: BasicCheckHighStatStageSpDefense(); break;
            case AccUp2: BasicCheckHighStatStageAccuracy(); break;
            case EvaUp2: BasicCheckHighStatStageEvasion(); break;
            case AtkDown2: BasicCheckLowStatStageAttack(); break;
            case DefDown2: BasicCheckLowStatStageDefense(); break;
            case SpeedDown2: BasicCheckLowStatStageSpeed(); break;
            case SpAtkDown2: BasicCheckLowStatStageSpAttack(); break;
            case SpDefDown2: BasicCheckLowStatStageSpDefense(); break;
            // The original sends the two-stage evasion drop to the accuracy check and the accuracy drop to the evasion one
            case EvaDown2: BasicCheckLowStatStageAccuracy(); break;
            case AccDown2: BasicCheckLowStatStageEvasion(); break;
            case SetReflect: BasicCheckAlreadyUnderReflect(); break;
            case StatusPoison: BasicCheckCannotPoison(); break;
            case StatusParalyze: BasicCheckCannotParalyze(); break;
            case SetSubstitute: BasicCheckCannotSubstitute(); break;
            case RechargeAfter: BasicCheckNonStandardDamageOrChargeTurn(); break;
            case StatusLeechSeed: BasicCheckCannotLeechSeed(); break;
            case Disable: BasicCheckCannotDisable(); break;
            case LevelDamageFlat: BasicCheckNonStandardDamageOrChargeTurn(); break;
            case RandomDamage1To150Level: BasicCheckNonStandardDamageOrChargeTurn(); break;
            case Counter: BasicCheckNonStandardDamageOrChargeTurn(); break;
            case Encore: BasicCheckCannotEncore(); break;
            case DamageWhileAsleep: BasicCheckAttackerAsleep(); break;
            case NextAttackAlwaysHits: BasicCheckLockOn(); break;
            case UseRandomLearnedMoveSleep: BasicCheckAttackerAsleep(); break;
            case IncreasePowerWithLessHp: BasicCheckNonStandardDamageOrChargeTurn(); break;
            case PreventEscape: BasicCheckMeanLook(); break;
            case StatusNightmare: BasicCheckNightmare(); break;
            case EvaUp2Minimize: BasicCheckHighStatStageEvasion(); break;
            case Curse: BasicCheckCurse(); break;
            case SetSpikes: BasicCheckSpikes(); break;
            case Foresight: BasicCheckForesight(); break;
            case AllFaint3Turns: BasicCheckPerishSong(); break;
            case WeatherSandstorm: BasicCheckSandstorm(); break;
            case AtkUp2StatusConfusion: BasicCheckCannotConfuse(); break;
            case Infatuate: BasicCheckCannotAttract(); break;
            case PowerBasedOnFriendship: BasicCheckNonStandardDamageOrChargeTurn(); break;
            case RandomPowerMaybeHeal: BasicCheckNonStandardDamageOrChargeTurn(); break;
            case PowerBasedOnLowFriendship: BasicCheckNonStandardDamageOrChargeTurn(); break;
            case PreventStatus: BasicCheckAlreadyUnderSafeguard(); break;
            case Psywave: BasicCheckMagnitude(loaded); break;
            case PassStatsAndStatus: BasicCheckBatonPass(); break;
            case N20DamageFlat: BasicCheckNonStandardDamageOrChargeTurn(); break;
            case HealHalfMoreInSun: BasicCheckCanRecoverHP(); break;
            case Unused133: BasicCheckCanRecoverHP(); break;
            case Unused134: BasicCheckCanRecoverHP(); break;
            case RandomPowerBasedOnIvs: BasicCheckNonStandardDamageOrChargeTurn(); break;
            case WeatherRain: BasicCheckRainDance(); break;
            case WeatherSun: BasicCheckSunnyDay(); break;
            case MaxAtkLoseHalfMaxHp: BasicCheckBellyDrum(); break;
            case CopyStatChanges: BasicCheckStatStageImbalance(); break;
            case MirrorCoat: BasicCheckNonStandardDamageOrChargeTurn(); break;
            case ChargeTurnDefUp: BasicCheckNonStandardDamageOrChargeTurn(); break;
            case HitIn3Turns: BasicCheckFutureSight(); break;
            case FleeFromWildBattle: Score(-10); break;
            case DefUpDoubleRolloutPower: BasicCheckHighStatStageDefense(); break;
            case Unused157: BasicCheckCanRecoverHP(); break;
            case AlwaysFlinchFirstTurnOnly: BasicCheckFirstTurnInBattle(); break;
            case MoveEffectId.Stockpile: BasicCheckMaxStockpile(); break;
            case SpitUp: BasicCheckCanSpitUpOrSwallow(); break;
            case Swallow: BasicCheckCanSpitUpOrSwallow(); break;
            case WeatherHail: BasicCheckHail(); break;
            case Torment: BasicCheckTorment(); break;
            case SpAtkUpCauseConfusion: BasicCheckCannotConfuse(); break;
            case StatusBurn: BasicCheckCannotBurn(); break;
            case FaintAndAtkSpAtkDown2: BasicCheckMemento(); break;
            case HitLastWhiffIfHit: BasicCheckNonStandardDamageOrChargeTurn(); break;
            case BoostAllyPowerBy50Percent: BasicCheckHelpingHand(); break;
            case SwitchHeldItems: BasicCheckCanRemoveItem(); break;
            case GroundTrapUserContinuousHeal: BasicCheckAlreadyIngrained(); break;
            case LowerOwnAtkAndDef: BasicCheckNonStandardDamageOrChargeTurn(); break;
            case Recycle: BasicCheckCanRecycle(); break;
            case StatusSleepNextTurn: BasicCheckCannotSleep(); break;
            case RemoveHeldItem: BasicCheckCanRemoveItem(); break;
            case SetHpEqualToUser: BasicCheckNonStandardDamageOrChargeTurn(); break;
            case MakeSharedMovesUnuseable: BasicCheckCanImprison(); break;
            case HealStatus: BasicCheckCanRefreshStatus(); break;
            case IncreasePowerWithWeight: BasicCheckNonStandardDamageOrChargeTurn(); break;
            case HalveElectricDamage: BasicCheckCanMudSport(); break;
            case AtkDefDown: BasicCheckTickle(); break;
            case DefSpdUp: BasicCheckCosmicPower(); break;
            case AtkDefUp: BasicCheckBulkUp(); break;
            case HalveFireDamage: BasicCheckWaterSport(); break;
            case SpAtkSpDefUp: BasicCheckCalmMind(); break;
            case AtkSpdUp: BasicCheckDragonDance(); break;
            case Camouflage: BasicCheckCamouflage(); break;
            case HealHalfRemoveFlyingType: BasicCheckCanRecoverHP(); break;
            case MoveEffectId.Gravity: BasicCheckGravityActive(); break;
            case IgnoreEvationRemoveDarkImmune: BasicCheckMiracleEye(); break;
            case PowerBasedOnLowSpeed: BasicCheckNonStandardDamageOrChargeTurn(); break;
            case FaintAndFullHealNextMon: BasicCheckHealingWish(); break;
            case NaturalGift: BasicCheckNaturalGift(); break;
            case DoubleSpeed3Turns: BasicCheckTailwind(); break;
            case RandomStatUp2: BasicCheckAcupressure(); break;
            case MetalBurst: BasicCheckMetalBurst(); break;
            case PreventItemUse: BasicCheckEmbargo(); break;
            case Fling: BasicCheckFling(); break;
            case TransferStatus: BasicCheckCanPsychoShift(); break;
            case HigherPowerWhenLowPp: BasicCheckNonStandardDamageOrChargeTurn(); break;
            case PreventHealing: BasicCheckHealBlock(); break;
            case IncreasePowerWithMoreHp: BasicCheckNonStandardDamageOrChargeTurn(); break;
            case SwapAtkDef: BasicCheckPowerTrick(); break;
            case SupressAbility: BasicCheckGastroAcid(); break;
            case PreventCrits: BasicCheckLuckyChant(); break;
            case UseLastUsedMove: BasicCheckCopycat(); break;
            case SwapAtkSpAtkStatChanges: BasicCheckPowerSwap(); break;
            case SwapDefSpDefStatChanges: BasicCheckGuardSwap(); break;
            case IncreasePowerWithMoreStatUp: BasicCheckNonStandardDamageOrChargeTurn(); break;
            case FailIfNotUsedAllOtherMoves: BasicCheckLastResort(); break;
            case SetAbilityToInsomnia: BasicCheckWorrySeed(); break;
            case ToxicSpikes: BasicCheckToxicSpikes(); break;
            case SwapStatChanges: BasicCheckStatStageImbalance(); break;
            case RestoreHpEveryTurn: BasicCheckAquaRing(); break;
            case GiveGroundImmunity: BasicCheckMagnetRise(); break;
            case RemoveHazardsScreensEvaDown: BasicCheckDefog(); break;
            case MoveEffectId.TrickRoom: BasicCheckTrickRoom(); break;
            case SpAtkDown2OppositeGender: BasicCheckCaptivate(); break;
            case StealthRock: BasicCheckStealthRock(); break;
            case FaintFullRestoreNextMon: BasicCheckLunarDance(); break;
        }
    }

    // ================================================================ conditions

    /// <summary><c>Basic_CheckCannotSleep</c>: −10 if the target has a condition, Safeguard, Insomnia or Vital Spirit.</summary>
    private void BasicCheckCannotSleep()
    {
        if (HasStatus(Who.Defender, Cond.Any) || SideHas(Who.Defender, SideFx.Safeguard)) { Score(-10); return; }
        if (AbilityOf(Who.Defender) is "Insomnia" or "Vital Spirit") Score(-10);
    }

    /// <summary>
    /// <c>Basic_CheckCannotPoison</c>: −10 if the target is Poison or Steel, has Immunity, Magic Guard or Poison Heal,
    /// Leaf Guard in the sun or Hydration in the rain, a condition already, or Safeguard.
    /// </summary>
    private void BasicCheckCannotPoison()
    {
        if (Type1(Who.Defender) is PokemonType.Steel or PokemonType.Poison
            || Type2(Who.Defender) is PokemonType.Steel or PokemonType.Poison) { Score(-10); return; }

        var ability = AbilityOf(Who.Defender);
        if (ability is "Immunity" or "Magic Guard" or "Poison Heal") { Score(-10); return; }
        if (ability == "Leaf Guard" && CurrentWeather == AiWeather.Sunny) { Score(-10); return; }
        // The target's ability is loaded again for Hydration: a second guess, where the AI has to guess
        if (AbilityOf(Who.Defender) == "Hydration" && CurrentWeather == AiWeather.Raining) { Score(-10); return; }
        if (HasStatus(Who.Defender, Cond.Any) || SideHas(Who.Defender, SideFx.Safeguard)) Score(-10);
    }

    /// <summary>
    /// <c>Basic_CheckCannotParalyze</c>: −10 if the target is immune to the move, has Limber or Magic Guard, a
    /// condition already or Safeguard; Thunder Wave also into Motor Drive or Volt Absorb, unless the attacker has Mold Breaker.
    /// </summary>
    private void BasicCheckCannotParalyze()
    {
        if (Effectiveness() == Eff.Immune) { Score(-10); return; }
        if (AbilityOf(Who.Defender) is "Limber" or "Magic Guard") { Score(-10); return; }
        if (AbilityOf(Who.Attacker) != "Mold Breaker" && MoveIs("Thunder Wave")
            && AbilityOf(Who.Defender) is "Motor Drive" or "Volt Absorb") { Score(-10); return; }
        if (HasStatus(Who.Defender, Cond.Any) || SideHas(Who.Defender, SideFx.Safeguard)) Score(-10);
    }

    /// <summary><c>Basic_CheckCannotBurn</c>: −10 if the target has Water Veil or Magic Guard, a condition already, is Fire, or has Safeguard.</summary>
    private void BasicCheckCannotBurn()
    {
        if (AbilityOf(Who.Defender) is "Water Veil" or "Magic Guard") { Score(-10); return; }
        if (HasStatus(Who.Defender, Cond.Any)) { Score(-10); return; }
        if (Type1(Who.Defender) == PokemonType.Fire || Type2(Who.Defender) == PokemonType.Fire) { Score(-10); return; }
        if (SideHas(Who.Defender, SideFx.Safeguard)) Score(-10);
    }

    /// <summary><c>Basic_CheckCannotConfuse</c>: −5 if the target is confused already; −10 if it has Own Tempo or Safeguard.</summary>
    private void BasicCheckCannotConfuse()
    {
        if (Has(Who.Defender, Vol.Confusion)) { Score(-5); return; }
        if (AbilityOf(Who.Defender) == "Own Tempo" || SideHas(Who.Defender, SideFx.Safeguard)) Score(-10);
    }

    /// <summary><c>Basic_CheckCannotAttract</c>: −10 if the target is in love already, has Oblivious, or isn't of the attacker's opposite gender.</summary>
    private void BasicCheckCannotAttract()
    {
        if (Has(Who.Defender, Vol.Attract)) { Score(-10); return; }
        if (AbilityOf(Who.Defender) == "Oblivious") { Score(-10); return; }
        if (!BasicOppositeGenders()) Score(-10);
    }

    /// <summary>The gender test of Attract and Captivate: the attacker male and the target female, or the other way round.</summary>
    private bool BasicOppositeGenders() => GenderOf(Who.Attacker) switch
    {
        Gender.Male => GenderOf(Who.Defender) == Gender.Female,
        Gender.Female => GenderOf(Who.Defender) == Gender.Male,
        _ => false
    };

    /// <summary><c>Basic_CheckNightmare</c>: −10 if the target has a nightmare already or Magic Guard; −8 if it isn't asleep.</summary>
    private void BasicCheckNightmare()
    {
        if (Has(Who.Defender, Vol.Nightmare)) { Score(-10); return; }
        if (!HasStatus(Who.Defender, Cond.Sleep)) { Score(-8); return; }
        if (AbilityOf(Who.Defender) == "Magic Guard") Score(-10);
    }

    /// <summary><c>Basic_CheckDreamEater</c>: −8 if the target isn't asleep; −10 if it is immune.</summary>
    private void BasicCheckDreamEater()
    {
        if (!HasStatus(Who.Defender, Cond.Sleep)) { Score(-8); return; }
        if (Effectiveness() == Eff.Immune) Score(-10);
    }

    /// <summary><c>Basic_CheckAttackerAsleep</c>: Snore and Sleep Talk get −8 unless the attacker is asleep.</summary>
    private void BasicCheckAttackerAsleep()
    {
        if (!HasStatus(Who.Attacker, Cond.Sleep)) Score(-8);
    }

    /// <summary><c>Basic_CheckCanRefreshStatus</c>: Refresh gets −10 unless the attacker is poisoned, burned or paralyzed.</summary>
    private void BasicCheckCanRefreshStatus()
    {
        if (!HasStatus(Who.Attacker, Cond.FacadeBoost)) Score(-10);
    }

    /// <summary>
    /// <c>Basic_CheckCanPsychoShift</c>: −10 unless the attacker has a condition to pass on to a target with none and
    /// no Safeguard; −10 too if the target couldn't take that condition, or the attacker is poisoned and has Poison Heal.
    /// </summary>
    private void BasicCheckCanPsychoShift()
    {
        if (!HasStatus(Who.Attacker, Cond.Any)) { Score(-10); return; }
        if (HasStatus(Who.Defender, Cond.Any)) { Score(-10); return; }
        if (SideHas(Who.Defender, SideFx.Safeguard)) { Score(-10); return; }

        if (HasStatus(Who.Attacker, Cond.AnyPoison))
        {
            if (AbilityOf(Who.Attacker) == "Poison Heal") { Score(-10); return; }
            if (Type1(Who.Defender) is PokemonType.Poison or PokemonType.Steel
                || Type2(Who.Defender) is PokemonType.Poison or PokemonType.Steel) { Score(-10); return; }
            if (AbilityOf(Who.Defender) is "Immunity" or "Poison Heal" or "Magic Guard") Score(-10);
            return;
        }
        if (HasStatus(Who.Attacker, Cond.Burn))
        {
            if (Type1(Who.Defender) == PokemonType.Fire || Type2(Who.Defender) == PokemonType.Fire) { Score(-10); return; }
            if (AbilityOf(Who.Defender) is "Magic Guard" or "Water Veil") Score(-10);
            return;
        }
        if (HasStatus(Who.Attacker, Cond.Paralysis) && AbilityOf(Who.Defender) == "Limber") Score(-10);
    }

    // ================================================================ stat stages

    /// <summary><c>Basic_CheckBellyDrum</c>: −10 at half HP or less, else as a move that raises Attack.</summary>
    private void BasicCheckBellyDrum()
    {
        if (HpPercent(Who.Attacker) < 51) { Score(-10); return; }
        BasicCheckHighStatStageAttack();
    }

    /// <summary><c>Basic_CheckHighStatStage_Attack</c>: −10 if the attacker's Attack can't usefully go higher.</summary>
    private void BasicCheckHighStatStageAttack() => BasicStatTooHigh(AbilityOf(Who.Attacker), StatType.Attack);

    /// <summary><c>Basic_CheckHighStatStage_Defense</c>.</summary>
    private void BasicCheckHighStatStageDefense() => BasicStatTooHigh(AbilityOf(Who.Attacker), StatType.Defense);

    /// <summary><c>Basic_CheckHighStatStage_Speed</c>: −10 under Trick Room too.</summary>
    private void BasicCheckHighStatStageSpeed()
    {
        if (TrickRoom) { Score(-10); return; }
        BasicStatTooHigh(AbilityOf(Who.Attacker), StatType.Speed);
    }

    /// <summary><c>Basic_CheckHighStatStage_SpAttack</c>.</summary>
    private void BasicCheckHighStatStageSpAttack() => BasicStatTooHigh(AbilityOf(Who.Attacker), StatType.SpAttack);

    /// <summary><c>Basic_CheckHighStatStage_SpDefense</c>.</summary>
    private void BasicCheckHighStatStageSpDefense() => BasicStatTooHigh(AbilityOf(Who.Attacker), StatType.SpDefense);

    /// <summary><c>Basic_CheckHighStatStage_Accuracy</c>: −10 if either side has No Guard too.</summary>
    private void BasicCheckHighStatStageAccuracy()
    {
        if (AbilityOf(Who.Defender) == "No Guard") { Score(-10); return; }
        var mine = AbilityOf(Who.Attacker);
        if (mine == "No Guard") { Score(-10); return; }
        BasicStatTooHigh(mine, StatType.Accuracy);
    }

    /// <summary><c>Basic_CheckHighStatStage_Evasion</c>: −10 if either side has No Guard too.</summary>
    private void BasicCheckHighStatStageEvasion()
    {
        if (AbilityOf(Who.Defender) == "No Guard") { Score(-10); return; }
        var mine = AbilityOf(Who.Attacker);
        if (mine == "No Guard") { Score(-10); return; }
        BasicStatTooHigh(mine, StatType.Evasion);
    }

    /// <summary>The end of the raising checks: −10 with Simple for a stage above +2 already, without it for one at +6.</summary>
    private void BasicStatTooHigh(string? ability, StatType stat)
    {
        if (ability == "Simple" ? Stage(Who.Attacker, stat) > 8 : Stage(Who.Attacker, stat) == 12) Score(-10);
    }

    /// <summary>
    /// The end of the checks of moves that raise two stats: with Simple, −10 if either is above +2 already; without
    /// it, −10 if the first is at +6, else −8 if the second is.
    /// </summary>
    private void BasicTwoStatsTooHigh(StatType first, StatType second)
    {
        if (AbilityOf(Who.Attacker) == "Simple")
        {
            if (Stage(Who.Attacker, first) > 8 || Stage(Who.Attacker, second) > 8) Score(-10);
            return;
        }
        if (Stage(Who.Attacker, first) == 12) { Score(-10); return; }
        if (Stage(Who.Attacker, second) == 12) Score(-8);
    }

    /// <summary>
    /// <c>Basic_CheckCurse</c>: a Ghost's Curse gets −10 if the target is cursed already or has Magic Guard; anyone
    /// else's is a move that raises Attack and Defense.
    /// </summary>
    private void BasicCheckCurse()
    {
        if (Type1(Who.Attacker) == PokemonType.Ghost || Type2(Who.Attacker) == PokemonType.Ghost)
        {
            if (Has(Who.Defender, Vol.Curse)) { Score(-10); return; }
            if (AbilityOf(Who.Defender) == "Magic Guard") Score(-10);
            return;
        }
        BasicTwoStatsTooHigh(StatType.Attack, StatType.Defense);
    }

    /// <summary><c>Basic_CheckCosmicPower</c>: as a move that raises Defense and Sp. Def.</summary>
    private void BasicCheckCosmicPower() => BasicTwoStatsTooHigh(StatType.Defense, StatType.SpDefense);

    /// <summary><c>Basic_CheckBulkUp</c>: as a move that raises Attack and Defense.</summary>
    private void BasicCheckBulkUp() => BasicTwoStatsTooHigh(StatType.Attack, StatType.Defense);

    /// <summary><c>Basic_CheckCalmMind</c>: as a move that raises Sp. Atk and Sp. Def.</summary>
    private void BasicCheckCalmMind() => BasicTwoStatsTooHigh(StatType.SpAttack, StatType.SpDefense);

    /// <summary><c>Basic_CheckDragonDance</c>: −10 under Trick Room, else as a move that raises Attack and Speed.</summary>
    private void BasicCheckDragonDance()
    {
        if (TrickRoom) { Score(-10); return; }
        BasicTwoStatsTooHigh(StatType.Attack, StatType.Speed);
    }

    /// <summary>The stats the stage checks go through: all but HP.</summary>
    private static readonly StatType[] BasicStageStats =
    {
        StatType.Attack, StatType.Defense, StatType.Speed, StatType.SpAttack, StatType.SpDefense, StatType.Accuracy, StatType.Evasion
    };

    /// <summary><c>Basic_CheckAcupressure</c>: −10 if any stat is at +6 already (with Simple, above +2).</summary>
    private void BasicCheckAcupressure()
    {
        bool simple = AbilityOf(Who.Attacker) == "Simple";
        foreach (var stat in BasicStageStats)
        {
            if (simple ? Stage(Who.Attacker, stat) > 8 : Stage(Who.Attacker, stat) == 12)
            {
                Score(-10);
                return;
            }
        }
    }

    /// <summary><c>Basic_CheckLowStatStage_Attack</c>: −10 if the target's Attack is at −6 or it has Hyper Cutter, Clear Body or White Smoke.</summary>
    private void BasicCheckLowStatStageAttack()
    {
        if (Stage(Who.Defender, StatType.Attack) == 0) { Score(-10); return; }
        if (AbilityOf(Who.Defender) == "Hyper Cutter") { Score(-10); return; }
        BasicCheckClearBodyEffect();
    }

    /// <summary><c>Basic_CheckLowStatStage_Defense</c>: −10 if the target's Defense is at −6 or it has Clear Body or White Smoke.</summary>
    private void BasicCheckLowStatStageDefense()
    {
        if (Stage(Who.Defender, StatType.Defense) == 0) { Score(-10); return; }
        BasicCheckClearBodyEffect();
    }

    /// <summary><c>Basic_CheckLowStatStage_Speed</c>: −10 under Trick Room, or if the target's Speed is at −6, or it surely has Speed Boost, Clear Body or White Smoke.</summary>
    private void BasicCheckLowStatStageSpeed()
    {
        if (TrickRoom) { Score(-10); return; }
        if (Stage(Who.Defender, StatType.Speed) == 0) { Score(-10); return; }
        if (CheckAbility(Who.Defender, "Speed Boost") == Knowledge.Have) { Score(-10); return; }
        BasicCheckClearBodyEffect();
    }

    /// <summary><c>Basic_CheckLowStatStage_SpAttack</c>.</summary>
    private void BasicCheckLowStatStageSpAttack()
    {
        if (Stage(Who.Defender, StatType.SpAttack) == 0) { Score(-10); return; }
        BasicCheckClearBodyEffect();
    }

    /// <summary><c>Basic_CheckLowStatStage_SpDefense</c>.</summary>
    private void BasicCheckLowStatStageSpDefense()
    {
        if (Stage(Who.Defender, StatType.SpDefense) == 0) { Score(-10); return; }
        BasicCheckClearBodyEffect();
    }

    /// <summary><c>Basic_CheckLowStatStage_Accuracy</c>: −10 too if either side has No Guard or the target has Keen Eye.</summary>
    private void BasicCheckLowStatStageAccuracy()
    {
        if (Stage(Who.Defender, StatType.Accuracy) == 0) { Score(-10); return; }
        if (AbilityOf(Who.Attacker) == "No Guard") { Score(-10); return; }
        if (AbilityOf(Who.Defender) is "Keen Eye" or "No Guard") { Score(-10); return; }
        BasicCheckClearBodyEffect();
    }

    /// <summary><c>Basic_CheckLowStatStage_Evasion</c>: −10 too if either side has No Guard.</summary>
    private void BasicCheckLowStatStageEvasion()
    {
        if (Stage(Who.Defender, StatType.Evasion) == 0) { Score(-10); return; }
        if (AbilityOf(Who.Attacker) == "No Guard") { Score(-10); return; }
        if (AbilityOf(Who.Defender) == "No Guard") { Score(-10); return; }
        BasicCheckClearBodyEffect();
    }

    /// <summary><c>Basic_CheckClearBodyEffect</c>: a move that lowers the target's stats gets −10 against Clear Body or White Smoke.</summary>
    private void BasicCheckClearBodyEffect()
    {
        if (AbilityOf(Who.Defender) is "Clear Body" or "White Smoke") Score(-10);
    }

    /// <summary>
    /// <c>Basic_CheckStatStageImbalance</c>: Haze, Psych Up and Heart Swap get −10 unless the attacker has a stat
    /// lowered or the target one raised.
    /// </summary>
    private void BasicCheckStatStageImbalance()
    {
        foreach (var stat in BasicStageStats)
            if (Stage(Who.Attacker, stat) < 6) return;
        foreach (var stat in BasicStageStats)
            if (Stage(Who.Defender, stat) > 6) return;
        Score(-10);
    }

    /// <summary>
    /// <c>Basic_CheckMemento</c>: −10 against Clear Body or White Smoke (unless the attacker has Mold Breaker), or if
    /// the target's Attack is at −6 already; −8 if its Sp. Atk is; −10 if the attacker is the last of its party.
    /// </summary>
    private void BasicCheckMemento()
    {
        if (AbilityOf(Who.Attacker) != "Mold Breaker" && AbilityOf(Who.Defender) is "Clear Body" or "White Smoke") { Score(-10); return; }
        if (Stage(Who.Defender, StatType.Attack) == 0) { Score(-10); return; }
        if (Stage(Who.Defender, StatType.SpAttack) == 0) { Score(-8); return; }
        if (AliveBench(Who.Attacker) == 0) Score(-10);
    }

    /// <summary>
    /// <c>Basic_CheckTickle</c>: −10 against Clear Body or White Smoke (unless the attacker has Mold Breaker), or if the
    /// target's Attack is at −6 already; −8 if its Defense is.
    /// </summary>
    private void BasicCheckTickle()
    {
        if (AbilityOf(Who.Attacker) != "Mold Breaker" && AbilityOf(Who.Defender) is "Clear Body" or "White Smoke") { Score(-10); return; }
        if (Stage(Who.Defender, StatType.Attack) == 0) { Score(-10); return; }
        if (Stage(Who.Defender, StatType.Defense) == 0) Score(-8);
    }

    /// <summary>
    /// <c>Basic_CheckCaptivate</c>: −10 against Oblivious, Clear Body or White Smoke (unless the attacker has Mold
    /// Breaker), against a target not of the opposite gender, or one whose Sp. Atk is at −6 already.
    /// </summary>
    private void BasicCheckCaptivate()
    {
        if (AbilityOf(Who.Attacker) != "Mold Breaker" && AbilityOf(Who.Defender) is "Oblivious" or "Clear Body" or "White Smoke") { Score(-10); return; }
        if (!BasicOppositeGenders()) { Score(-10); return; }
        if (Stage(Who.Defender, StatType.SpAttack) < 1) Score(-10);
    }

    /// <summary><c>Basic_CheckPowerSwap</c>: −10 unless the target's Attack or Sp. Atk stage is above the attacker's.</summary>
    private void BasicCheckPowerSwap()
    {
        if (StageDiff(Who.Defender, StatType.Attack) >= 1) return;
        if (StageDiff(Who.Defender, StatType.SpAttack) < 1) Score(-10);
    }

    /// <summary><c>Basic_CheckGuardSwap</c>: −10 unless the target's Defense or Sp. Def stage is above the attacker's.</summary>
    private void BasicCheckGuardSwap()
    {
        if (StageDiff(Who.Defender, StatType.Defense) >= 1) return;
        if (StageDiff(Who.Defender, StatType.SpDefense) < 1) Score(-10);
    }

    // ================================================================ damage and failure

    /// <summary>
    /// <c>Basic_CheckCannotExplode</c>: Explosion and Self-Destruct get −10 against a target immune to them, or with Damp
    /// (unless the attacker has Mold Breaker); then as <see cref="BasicCheckLastMon"/>.
    /// </summary>
    private void BasicCheckCannotExplode()
    {
        if (Effectiveness() == Eff.Immune) { Score(-10); return; }
        if (AbilityOf(Who.Attacker) != "Mold Breaker" && AbilityOf(Who.Defender) == "Damp") { Score(-10); return; }
        BasicCheckLastMon();
    }

    /// <summary><c>Basic_CheckLastMon</c>: the attacker's last Pokémon standing gets −10 for fainting itself, only −1 if the target is the last of its own.</summary>
    private void BasicCheckLastMon()
    {
        if (AliveBench(Who.Attacker) != 0) return;
        if (AliveBench(Who.Defender) != 0) { Score(-10); return; }
        Score(-1);
    }

    /// <summary><c>Basic_CheckOHKOWouldFail</c>: −10 against an immune target, Sturdy (unless the attacker has Mold Breaker) or a target of a higher level.</summary>
    private void BasicCheckOHKOWouldFail()
    {
        if (Effectiveness() == Eff.Immune) { Score(-10); return; }
        if (AbilityOf(Who.Attacker) != "Mold Breaker" && AbilityOf(Who.Defender) == "Sturdy") { Score(-10); return; }
        if (LevelOf(Who.Attacker) < LevelOf(Who.Defender)) Score(-10);
    }

    /// <summary>
    /// <c>Basic_CheckMagnitude</c> (Magnitude's effect is the one the decompilation calls Psywave's): −10 against
    /// Levitate, then as a move of unusual damage.
    /// </summary>
    private void BasicCheckMagnitude(string? loaded)
    {
        // The original asks whether the ability loaded last is Mold Breaker without loading the attacker's: that is
        // the target's from the sound check (the attacker's only when the target has Soundproof)
        if (loaded != "Mold Breaker" && AbilityOf(Who.Defender) == "Levitate") { Score(-10); return; }
        BasicCheckNonStandardDamageOrChargeTurn();
    }

    /// <summary>
    /// <c>Basic_CheckNonStandardDamageOrChargeTurn</c>: a move of fixed, variable or delayed damage gets −10 against a
    /// target immune by type, or with Wonder Guard (and no Mold Breaker) unless it is super effective.
    /// </summary>
    private void BasicCheckNonStandardDamageOrChargeTurn()
    {
        if (Effectiveness() == Eff.Immune) { Score(-10); return; }
        if (AbilityOf(Who.Defender) != "Wonder Guard") return;
        if (AbilityOf(Who.Attacker) == "Mold Breaker") return;
        if (Effectiveness() is Eff.Double or Eff.Quadruple) return;
        Score(-10);
    }

    /// <summary><c>Basic_CheckFutureSight</c>: −12 while a Future Sight is on its way to either side.</summary>
    private void BasicCheckFutureSight()
    {
        if (SideHas(Who.Defender, SideFx.FutureSight) || SideHas(Who.Attacker, SideFx.FutureSight)) Score(-12);
    }

    /// <summary><c>Basic_CheckFirstTurnInBattle</c>: Fake Out gets −10 after the attacker's first turn on the field.</summary>
    private void BasicCheckFirstTurnInBattle()
    {
        if (!FirstTurnIn(Who.Attacker)) Score(-10);
    }

    /// <summary><c>Basic_CheckMaxStockpile</c>: Stockpile gets −10 at three already.</summary>
    private void BasicCheckMaxStockpile()
    {
        if (Stockpile(Who.Attacker) == 3) Score(-10);
    }

    /// <summary>
    /// <c>Basic_CheckCanSpitUpOrSwallow</c>: −10 against a target immune by type (Swallow too, though it hits nobody)
    /// or with nothing stockpiled; Swallow then as a move that heals.
    /// </summary>
    private void BasicCheckCanSpitUpOrSwallow()
    {
        if (Effectiveness() == Eff.Immune) { Score(-10); return; }
        if (Stockpile(Who.Attacker) == 0) { Score(-10); return; }
        if (Effect == Swallow) BasicCheckCanRecoverHP();
    }

    /// <summary>
    /// <c>Basic_CheckMetalBurst</c>: −10 against an immune target, or where the attacker would move first: the target
    /// has Stall, or the attacker is faster and hasn't Stall itself.
    /// </summary>
    private void BasicCheckMetalBurst()
    {
        if (Effectiveness() == Eff.Immune) { Score(-10); return; }
        // Moving last was meant to be read from Stall and a Lagging Tail's hold effect; the original asks for a
        // Shiny Stone instead
        if (AbilityOf(Who.Defender) == "Stall") { Score(-10); return; }
        if (Holds(Who.Defender, "Shiny Stone")) { Score(-10); return; }
        if (AbilityOf(Who.Attacker) == "Stall") return;
        if (Holds(Who.Attacker, "Shiny Stone")) return;
        if (SpeedCompare == SpeedOrder.Faster) Score(-10);
    }

    /// <summary><c>Basic_CheckLastResort</c>: −10 until the attacker has used its other moves.</summary>
    private void BasicCheckLastResort()
    {
        if (!CanUseLastResort(Who.Attacker)) Score(-10);
    }

    // ================================================================ what is up already

    /// <summary><c>Basic_CheckAlreadyUnderLightScreen</c>: −8 while the attacker's side has Light Screen.</summary>
    private void BasicCheckAlreadyUnderLightScreen()
    {
        if (SideHas(Who.Attacker, SideFx.LightScreen)) Score(-8);
    }

    /// <summary><c>Basic_CheckAlreadyUnderReflect</c>: −8 while the attacker's side has Reflect.</summary>
    private void BasicCheckAlreadyUnderReflect()
    {
        if (SideHas(Who.Attacker, SideFx.Reflect)) Score(-8);
    }

    /// <summary><c>Basic_CheckAlreadyUnderMist</c>: −8 while the attacker's side has Mist.</summary>
    private void BasicCheckAlreadyUnderMist()
    {
        if (SideHas(Who.Attacker, SideFx.Mist)) Score(-8);
    }

    /// <summary><c>Basic_CheckAlreadyUnderSafeguard</c>: −8 while the attacker's side has Safeguard.</summary>
    private void BasicCheckAlreadyUnderSafeguard()
    {
        if (SideHas(Who.Attacker, SideFx.Safeguard)) Score(-8);
    }

    /// <summary><c>Basic_CheckAlreadyPumpedUp</c>: Focus Energy gets −10 once it is in force.</summary>
    private void BasicCheckAlreadyPumpedUp()
    {
        if (Has(Who.Attacker, Vol.FocusEnergy)) Score(-10);
    }

    /// <summary><c>Basic_CheckCannotSubstitute</c>: −8 behind a substitute already; −10 under a quarter of its HP.</summary>
    private void BasicCheckCannotSubstitute()
    {
        if (Has(Who.Attacker, Vol.Substitute)) { Score(-8); return; }
        if (HpPercent(Who.Attacker) < 26) Score(-10);
    }

    /// <summary><c>Basic_CheckCannotLeechSeed</c>: −10 if the target is seeded already, is Grass, or has Magic Guard.</summary>
    private void BasicCheckCannotLeechSeed()
    {
        if (Has(Who.Defender, MonFx.LeechSeed)) { Score(-10); return; }
        if (Type1(Who.Defender) == PokemonType.Grass || Type2(Who.Defender) == PokemonType.Grass) { Score(-10); return; }
        if (AbilityOf(Who.Defender) == "Magic Guard") Score(-10);
    }

    /// <summary><c>Basic_CheckCannotDisable</c>: −8 if the target has a move disabled already.</summary>
    private void BasicCheckCannotDisable()
    {
        if (Disabled(Who.Defender)) Score(-8);
    }

    /// <summary><c>Basic_CheckCannotEncore</c>: −8 if the target is under an Encore already.</summary>
    private void BasicCheckCannotEncore()
    {
        if (Encored(Who.Defender)) Score(-8);
    }

    /// <summary><c>Basic_CheckLockOn</c>: −10 if the target is locked on to already, or either side has No Guard.</summary>
    private void BasicCheckLockOn()
    {
        if (Has(Who.Defender, MonFx.LockOn)) { Score(-10); return; }
        if (AbilityOf(Who.Attacker) == "No Guard") { Score(-10); return; }
        if (AbilityOf(Who.Defender) == "No Guard") Score(-10);
    }

    /// <summary><c>Basic_CheckMeanLook</c>: −10 if the target can't flee already.</summary>
    private void BasicCheckMeanLook()
    {
        if (Has(Who.Defender, Vol.MeanLook)) Score(-10);
    }

    /// <summary><c>Basic_CheckForesight</c>: −10 if the target is identified already.</summary>
    private void BasicCheckForesight()
    {
        if (Has(Who.Defender, Vol.Foresight)) Score(-10);
    }

    /// <summary><c>Basic_CheckPerishSong</c>: −10 if the target hears a Perish Song already.</summary>
    private void BasicCheckPerishSong()
    {
        if (Has(Who.Defender, MonFx.PerishSong)) Score(-10);
    }

    /// <summary><c>Basic_CheckTorment</c>: −10 if the target is tormented already.</summary>
    private void BasicCheckTorment()
    {
        if (Has(Who.Defender, Vol.Torment)) Score(-10);
    }

    /// <summary><c>Basic_CheckAlreadyIngrained</c>: −10 once the attacker has its roots down.</summary>
    private void BasicCheckAlreadyIngrained()
    {
        if (Has(Who.Attacker, MonFx.Ingrain)) Score(-10);
    }

    /// <summary><c>Basic_CheckCanImprison</c>: −10 if the attacker has used Imprison already or the target's moves are sealed.</summary>
    private void BasicCheckCanImprison()
    {
        if (Has(Who.Attacker, MonFx.Imprison) || Has(Who.Defender, MonFx.Imprisoned)) Score(-10);
    }

    /// <summary><c>Basic_CheckCanMudSport</c>: −10 once the attacker's Mud Sport is in force.</summary>
    private void BasicCheckCanMudSport()
    {
        if (Has(Who.Attacker, MonFx.MudSport)) Score(-10);
    }

    /// <summary><c>Basic_CheckWaterSport</c>: −10 once the attacker's Water Sport is in force.</summary>
    private void BasicCheckWaterSport()
    {
        if (Has(Who.Attacker, MonFx.WaterSport)) Score(-10);
    }

    /// <summary><c>Basic_CheckCamouflage</c>: −10 once the attacker has used Camouflage.</summary>
    private void BasicCheckCamouflage()
    {
        if (Has(Who.Attacker, MonFx.Camouflage)) Score(-10);
    }

    /// <summary><c>Basic_CheckGravityActive</c>: −10 while Gravity is in force.</summary>
    private void BasicCheckGravityActive()
    {
        if (Gravity) Score(-10);
    }

    /// <summary><c>Basic_CheckMiracleEye</c>: −10 if the target is under Miracle Eye already.</summary>
    private void BasicCheckMiracleEye()
    {
        if (Has(Who.Defender, MonFx.MiracleEye)) Score(-10);
    }

    /// <summary><c>Basic_CheckTailwind</c>: −10 under Trick Room or while the attacker's side has a Tailwind.</summary>
    private void BasicCheckTailwind()
    {
        if (TrickRoom || SideHas(Who.Attacker, SideFx.Tailwind)) Score(-10);
    }

    /// <summary><c>Basic_CheckHealBlock</c>: −10 if the target is under Heal Block already.</summary>
    private void BasicCheckHealBlock()
    {
        if (Has(Who.Defender, MonFx.HealBlock)) Score(-10);
    }

    /// <summary><c>Basic_CheckPowerTrick</c>: −10 once the attacker's Power Trick is in force.</summary>
    private void BasicCheckPowerTrick()
    {
        if (Has(Who.Attacker, MonFx.PowerTrick)) Score(-10);
    }

    /// <summary><c>Basic_CheckLuckyChant</c>: −10 while the attacker's side has Lucky Chant.</summary>
    private void BasicCheckLuckyChant()
    {
        if (SideHas(Who.Attacker, SideFx.LuckyChant)) Score(-10);
    }

    /// <summary><c>Basic_CheckAquaRing</c>: −10 once the attacker has its Aqua Ring.</summary>
    private void BasicCheckAquaRing()
    {
        if (Has(Who.Attacker, MonFx.AquaRing)) Score(-10);
    }

    /// <summary><c>Basic_CheckMagnetRise</c>: −10 if the attacker floats already: by Magnet Rise, Levitate or a Flying type.</summary>
    private void BasicCheckMagnetRise()
    {
        if (Has(Who.Attacker, MonFx.MagnetRise)) { Score(-10); return; }
        if (AbilityOf(Who.Attacker) == "Levitate") { Score(-10); return; }
        if (Type1(Who.Attacker) == PokemonType.Flying || Type2(Who.Attacker) == PokemonType.Flying) Score(-10);
    }

    /// <summary><c>Basic_CheckEmbargo</c>: −10 if the target is under an Embargo already; in the Battle Frontier, also if it has an item to recycle.</summary>
    private void BasicCheckEmbargo()
    {
        if (Has(Who.Defender, MonFx.Embargo)) { Score(-10); return; }
        if (RecycleItem(Who.Defender) == null) return;
        if (IsFrontier) Score(-10);
    }

    /// <summary><c>Basic_CheckGastroAcid</c>: −10 if the target's ability is suppressed already, or is one not worth suppressing.</summary>
    private void BasicCheckGastroAcid()
    {
        if (Has(Who.Defender, MonFx.AbilitySuppressed)) { Score(-10); return; }
        if (AbilityOf(Who.Defender) is "Multitype" or "Truant" or "Slow Start" or "Stench" or "Run Away" or "Pickup" or "Honey Gather")
            Score(-10);
    }

    /// <summary>
    /// <c>Basic_CheckWorrySeed</c>: −10 against Truant, Insomnia, Vital Spirit or Multitype, or against a sleeping target
    /// not seen to know Sleep Talk or Snore.
    /// </summary>
    private void BasicCheckWorrySeed()
    {
        if (AbilityOf(Who.Defender) is "Truant" or "Insomnia" or "Vital Spirit" or "Multitype") { Score(-10); return; }
        if (!HasStatus(Who.Defender, Cond.Sleep)) return;
        if (Knows(Who.Defender, "Sleep Talk") == true) return;
        if (Knows(Who.Defender, "Snore") == true) return;
        Score(-10);
    }

    // ================================================================ weather

    /// <summary><c>Basic_CheckSandstorm</c>: −8 in a sandstorm already.</summary>
    private void BasicCheckSandstorm()
    {
        if (CurrentWeather == AiWeather.Sandstorm) Score(-8);
    }

    /// <summary>
    /// <c>Basic_CheckRainDance</c>: −8 against a target with Hydration and a condition to wash off (unless the attacker
    /// has Swift Swim or Hydration itself), then as <see cref="BasicCheckCurrentWeatherIsRain"/>.
    /// </summary>
    private void BasicCheckRainDance()
    {
        if (AbilityOf(Who.Attacker) is not ("Swift Swim" or "Hydration")
            && AbilityOf(Who.Defender) == "Hydration" && HasStatus(Who.Defender, Cond.Any))
        {
            Score(-8);
            return;
        }
        BasicCheckCurrentWeatherIsRain();
    }

    /// <summary><c>Basic_CheckCurrentWeatherIsRain</c>: −8 in the rain already.</summary>
    private void BasicCheckCurrentWeatherIsRain()
    {
        if (CurrentWeather == AiWeather.Raining) Score(-8);
    }

    /// <summary>
    /// <c>Basic_CheckSunnyDay</c>: −10 against a target with Hydration and a condition (unless the attacker has Flower
    /// Gift, Leaf Guard or Solar Power), then as <see cref="BasicCheckCurrentWeatherIsSun"/>.
    /// </summary>
    private void BasicCheckSunnyDay()
    {
        // Hydration does nothing in the sun: the check was copied from Rain Dance's, as the original has it
        if (AbilityOf(Who.Attacker) is not ("Flower Gift" or "Leaf Guard" or "Solar Power")
            && AbilityOf(Who.Defender) == "Hydration" && HasStatus(Who.Defender, Cond.Any))
        {
            Score(-10);
            return;
        }
        BasicCheckCurrentWeatherIsSun();
    }

    /// <summary><c>Basic_CheckCurrentWeatherIsSun</c>: −8 in the sun already.</summary>
    private void BasicCheckCurrentWeatherIsSun()
    {
        if (CurrentWeather == AiWeather.Sunny) Score(-8);
    }

    /// <summary><c>Basic_CheckHail</c>: −8 in a hailstorm already, and −8 against Ice Body unless the attacker has Ice Body too.</summary>
    private void BasicCheckHail()
    {
        if (CurrentWeather == AiWeather.Hailing) { Score(-8); return; }
        if (AbilityOf(Who.Defender) != "Ice Body") return;
        Score(-8);
        // An attacker's own Ice Body only takes back the −8 (where it was meant to make Hail worth more)
        if (AbilityOf(Who.Attacker) != "Ice Body") return;
        Score(8);
    }

    // ================================================================ the party

    /// <summary><c>Basic_CheckCanForceSwitch</c>: Roar and Whirlwind get −10 if the target has nobody to come in, or has Suction Cups (unless the attacker has Mold Breaker).</summary>
    private void BasicCheckCanForceSwitch()
    {
        if (AliveBench(Who.Defender) == 0) { Score(-10); return; }
        if (AbilityOf(Who.Attacker) == "Mold Breaker") return;
        if (AbilityOf(Who.Defender) == "Suction Cups") Score(-10);
    }

    /// <summary><c>Basic_CheckCanRecoverHP</c>: a move that heals the attacker gets −8 at full HP.</summary>
    private void BasicCheckCanRecoverHP()
    {
        if (HpPercent(Who.Attacker) != 100) return;
        Score(-8);
    }

    /// <summary><c>Basic_CheckSpikes</c>: −10 at three layers already, or if the target has nobody to switch in.</summary>
    private void BasicCheckSpikes()
    {
        if (SpikesLayers(Who.Defender) == 3) { Score(-10); return; }
        if (AliveBench(Who.Defender) == 0) Score(-10);
    }

    /// <summary><c>Basic_CheckToxicSpikes</c>: −10 at two layers already, or if the target has nobody to switch in.</summary>
    private void BasicCheckToxicSpikes()
    {
        if (ToxicSpikesLayers(Who.Defender) == 2) { Score(-10); return; }
        if (AliveBench(Who.Defender) == 0) Score(-10);
    }

    /// <summary><c>Basic_CheckStealthRock</c>: −10 if the rocks are out already, or if the target has nobody to switch in.</summary>
    private void BasicCheckStealthRock()
    {
        if (SideHas(Who.Defender, SideFx.StealthRock)) { Score(-10); return; }
        if (AliveBench(Who.Defender) == 0) Score(-10);
    }

    /// <summary>
    /// <c>Basic_CheckDefog</c>: unless the target's evasion can still fall, a screen is up on its side or there is fog,
    /// −10 if it has nobody to switch in or no hazards on its side to blow away.
    /// </summary>
    private void BasicCheckDefog()
    {
        if (Stage(Who.Defender, StatType.Evasion) != 0) return;
        if (SideHas(Who.Defender, SideFx.LightScreen)) return;
        if (SideHas(Who.Defender, SideFx.Reflect)) return;
        if (CurrentWeather == AiWeather.DeepFog) return;
        if (AliveBench(Who.Defender) == 0) { Score(-10); return; }
        if (SideHas(Who.Defender, SideFx.Spikes) || SideHas(Who.Defender, SideFx.StealthRock) || SideHas(Who.Defender, SideFx.ToxicSpikes)) return;
        Score(-10);
    }

    /// <summary><c>Basic_CheckBatonPass</c>: −10 with nobody to pass to.</summary>
    private void BasicCheckBatonPass()
    {
        if (AliveBench(Who.Attacker) == 0) Score(-10);
    }

    /// <summary>
    /// <c>Basic_CheckHealingWish</c>: −20 always; another −10 with nobody to come in, or if nobody on the bench has a
    /// condition or has lost HP.
    /// </summary>
    private void BasicCheckHealingWish()
    {
        Score(-20);
        if (AliveBench(Who.Attacker) == 0) { Score(-10); return; }
        if (BenchHasStatus(Who.Attacker, Cond.Any)) return;
        if (AnyPartyWounded(Who.Attacker)) return;
        Score(-10);
    }

    /// <summary>
    /// <c>Basic_CheckLunarDance</c>: −20 always; another −10 with nobody to come in, or if nobody in the party has lost
    /// HP or PP or has a condition.
    /// </summary>
    private void BasicCheckLunarDance()
    {
        Score(-20);
        if (AliveBench(Who.Attacker) == 0) { Score(-10); return; }
        if (AnyPartyWounded(Who.Attacker)) return;
        if (BenchHasStatus(Who.Attacker, Cond.Any)) return;
        if (AnyPartyUsedPp(Who.Attacker)) return;
        Score(-10);
    }

    /// <summary><c>Basic_CheckHelpingHand</c>: −10 outside a double battle.</summary>
    private void BasicCheckHelpingHand()
    {
        if (!IsDouble) Score(-10);
    }

    /// <summary><c>Basic_CheckCopycat</c>: −10 on the battle's first turn if the attacker would move first (nothing to copy yet).</summary>
    private void BasicCheckCopycat()
    {
        if (TurnCount != 0) return;
        if (SpeedCompare == SpeedOrder.Faster) Score(-10);
    }

    /// <summary><c>Basic_CheckTrickRoom</c>: −10 if the attacker would move first, or ties.</summary>
    private void BasicCheckTrickRoom()
    {
        // Two questions, as in the original: a tie may be settled by a roll each time it is asked
        if (SpeedCompare == SpeedOrder.Faster || SpeedCompare == SpeedOrder.Tie) Score(-10);
    }

    // ================================================================ items

    /// <summary><c>Basic_CheckCanRemoveItem</c>: Trick and Knock Off get −10 against Sticky Hold or a target holding nothing.</summary>
    private void BasicCheckCanRemoveItem()
    {
        if (AbilityOf(Who.Defender) == "Sticky Hold") { Score(-10); return; }
        if (HeldItem(Who.Defender) == null) Score(-10);
    }

    /// <summary><c>Basic_CheckCanRecycle</c>: −10 with nothing to recycle.</summary>
    private void BasicCheckCanRecycle()
    {
        if (RecycleItem(Who.Attacker) == null) Score(-10);
    }

    /// <summary><c>Basic_NaturalGiftBerries</c>: the berries Natural Gift can throw.</summary>
    private static readonly HashSet<string> BasicNaturalGiftBerries = new()
    {
        "Cheri Berry", "Chesto Berry", "Pecha Berry", "Rawst Berry", "Aspear Berry", "Leppa Berry", "Oran Berry",
        "Persim Berry", "Lum Berry", "Sitrus Berry", "Figy Berry", "Wiki Berry", "Mago Berry", "Aguav Berry",
        "Iapapa Berry", "Razz Berry", "Bluk Berry", "Nanab Berry", "Wepear Berry", "Pinap Berry", "Pomeg Berry",
        "Kelpsy Berry", "Qualot Berry", "Hondew Berry", "Grepa Berry", "Tamato Berry", "Cornn Berry", "Magost Berry",
        "Rabuta Berry", "Nomel Berry", "Spelon Berry", "Pamtre Berry", "Watmel Berry", "Durin Berry", "Belue Berry",
        "Occa Berry", "Passho Berry", "Wacan Berry", "Rindo Berry", "Yache Berry", "Chople Berry", "Kebia Berry",
        "Shuca Berry", "Coba Berry", "Payapa Berry", "Tanga Berry", "Charti Berry", "Kasib Berry", "Haban Berry",
        "Colbur Berry", "Babiri Berry", "Chilan Berry", "Liechi Berry", "Ganlon Berry", "Salac Berry", "Petaya Berry",
        "Apicot Berry", "Lansat Berry", "Starf Berry", "Enigma Berry", "Micle Berry", "Custap Berry", "Jaboca Berry",
        "Rowap Berry"
    };

    /// <summary><c>Basic_CheckNaturalGift</c>: −10 without a berry to throw, or against a target immune to it.</summary>
    private void BasicCheckNaturalGift()
    {
        if (HeldItem(Who.Attacker) is not { } item || !BasicNaturalGiftBerries.Contains(item.Name)) { Score(-10); return; }
        if (Effectiveness() == Eff.Immune) Score(-10);
    }

    /// <summary><c>Basic_FlingItems_Poison</c>: the hold effects of the Toxic Orb and the Poison Barb.</summary>
    private static readonly HashSet<string> BasicFlingItemsPoison = new() { "PsnUser", "StrengthenPoison" };

    /// <summary><c>Basic_FlingItems_Burn</c>: the Flame Orb's.</summary>
    private static readonly HashSet<string> BasicFlingItemsBurn = new() { "BrnUser" };

    /// <summary><c>Basic_FlingItems_Paralyze</c>: the Light Ball's.</summary>
    private static readonly HashSet<string> BasicFlingItemsParalyze = new() { "PikaSpatkUp" };

    /// <summary>
    /// <c>Basic_CheckFling</c>: −10 against a target immune by type, with an item too light to throw (or none), or with
    /// Multitype; an item that poisons, burns or paralyzes is weighed by <see cref="BasicFlingPoison"/> and the rest.
    /// </summary>
    private void BasicCheckFling()
    {
        if (Effectiveness() == Eff.Immune) { Score(-10); return; }
        if (FlingPower(Who.Attacker) < 10) { Score(-10); return; }
        if (AbilityOf(Who.Attacker) == "Multitype") { Score(-10); return; }

        var hold = HoldEffect(Who.Attacker);
        if (hold == null) return;
        if (BasicFlingItemsPoison.Contains(hold)) { BasicFlingPoison(); return; }
        if (BasicFlingItemsBurn.Contains(hold)) { BasicFlingBurn(); return; }
        if (BasicFlingItemsParalyze.Contains(hold)) BasicFlingParalyze();
    }

    /// <summary>
    /// <c>Basic_FlingPoison</c>: a poisoning item thrown at a target that could be poisoned is left alone; where it
    /// couldn't, the throw is worth +3 for ridding the attacker of it, or −5 if the attacker is safe from it anyway
    /// (or glad of it).
    /// </summary>
    private void BasicFlingPoison()
    {
        if (!(SideHas(Who.Defender, SideFx.Safeguard)
              || HasStatus(Who.Defender, Cond.Any)
              // the attacker's own Poison Heal is asked among the target's checks
              || AbilityOf(Who.Attacker) == "Poison Heal"
              || Type1(Who.Defender) is PokemonType.Poison or PokemonType.Steel
              || Type2(Who.Defender) is PokemonType.Poison or PokemonType.Steel
              || AbilityOf(Who.Defender) is "Immunity" or "Poison Heal" or "Magic Guard"))
            return;

        if (SideHas(Who.Attacker, SideFx.Safeguard)
            || HasStatus(Who.Attacker, Cond.Any)
            || Type1(Who.Attacker) is PokemonType.Poison or PokemonType.Steel
            || Type2(Who.Attacker) is PokemonType.Poison or PokemonType.Steel
            || AbilityOf(Who.Attacker) is "Klutz" or "Immunity" or "Poison Heal" or "Magic Guard" or "Guts")
        {
            Score(-5);
            return;
        }
        Score(3);
    }

    /// <summary><c>Basic_FlingBurn</c>: the same for a burning item: +3 or −5 where the target couldn't be burned.</summary>
    private void BasicFlingBurn()
    {
        if (!(SideHas(Who.Defender, SideFx.Safeguard)
              || HasStatus(Who.Defender, Cond.Any)
              || Type1(Who.Defender) == PokemonType.Fire
              || Type2(Who.Defender) == PokemonType.Fire
              || AbilityOf(Who.Defender) is "Magic Guard" or "Water Veil"))
            return;

        if (SideHas(Who.Attacker, SideFx.Safeguard)
            || HasStatus(Who.Attacker, Cond.Any)
            || Type1(Who.Attacker) == PokemonType.Fire
            || Type2(Who.Attacker) == PokemonType.Fire
            || AbilityOf(Who.Attacker) is "Klutz" or "Magic Guard" or "Water Veil" or "Guts")
        {
            Score(-5);
            return;
        }
        Score(3);
    }

    /// <summary><c>Basic_FlingParalyze</c>: a Light Ball thrown gets −5 at a target that couldn't be paralyzed.</summary>
    private void BasicFlingParalyze()
    {
        if (SideHas(Who.Defender, SideFx.Safeguard) || HasStatus(Who.Defender, Cond.Any) || AbilityOf(Who.Defender) == "Limber")
            Score(-5);
    }
}
