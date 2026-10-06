using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using static PokemonPlatinumEngine.Battle.Sim.Ai.MoveEffectId;

namespace PokemonPlatinumEngine.Battle.Sim.Ai;

/// <summary>
/// The first half of the script's Expert routines (<c>Expert_Main</c> through <c>Expert_Hail</c>, in
/// <c>src/battle/trainer_ai/script.s</c>): the dispatch on the move's battle effect and the judgement of each kind of
/// move, the same questions in the same order, the same rolls, the same scores. The rest are in
/// <c>AiThinking.Expert2.cs</c>.
/// </summary>
internal sealed partial class AiThinking
{
    /// <summary>
    /// <c>Expert_Main</c>: hands the move to the routine for its battle effect. A move aimed at the partner, or of an
    /// effect without a routine, is left as it is. Where an effect is listed twice the first entry wins.
    /// </summary>
    private void Expert()
    {
        if (TargetIsPartner) return;

        switch (Effect)
        {
            case StatusSleep: ExpertStatusSleep(); break;
            case RecoverHalfDamageDealt: ExpertDrainMove(); break;
            case HalveDefense: ExpertExplosion(); break;
            case RecoverDamageSleep: ExpertDreamEater(); break;
            case CopyMove: ExpertMirrorMove(); break;
            case AtkUp: ExpertStatusAttackUp(); break;
            case DefUp: ExpertStatusDefenseUp(); break;
            case SpeedUp: ExpertStatusSpeedUp(); break;
            case SpAtkUp: ExpertStatusSpAttackUp(); break;
            case SpDefUp: ExpertStatusSpDefenseUp(); break;
            case AccUp: ExpertStatusAccuracyUp(); break;
            case EvaUp: ExpertStatusEvasionUp(); break;
            case BypassAccuracy: ExpertBypassAccuracyMove(); break;
            case AtkDown: ExpertStatusAttackDown(); break;
            case DefDown: ExpertStatusDefenseDown(); break;
            case SpeedDown: ExpertStatusSpeedDown(); break;
            case SpAtkDown: ExpertStatusSpAttackDown(); break;
            case SpDefDown: ExpertStatusSpDefenseDown(); break;
            case AccDown: ExpertStatusAccuracyDown(); break;
            case EvaDown: ExpertStatusEvasionDown(); break;
            case ResetStatChanges: ExpertHaze(); break;
            case Bide: ExpertBide(); break;
            case ForceSwitch: ExpertForceSwitch(); break;
            case Conversion: ExpertConversion(); break;
            case RestoreHalfHp: ExpertRecovery(); break;
            case StatusBadlyPoison: ExpertToxicLeechSeed(); break;
            case SetLightScreen: ExpertLightScreen(); break;
            case Rest: ExpertRest(); break;
            case OneHitKo: ExpertOHKOMove(); break;
            case ChargeTurnHighCrit: ExpertChargeTurnNoInvuln(); break;
            case HalveHp: ExpertSuperFang(); break;
            case BindHit: ExpertBindingMove(); break;
            case HighCritical: ExpertHighCritical(); break;
            case RecoilQuarter: ExpertRecoilMove(); break;
            case StatusConfuse: ExpertStatusConfuse(); break;
            case AtkUp2: ExpertStatusAttackUp(); break;
            case DefUp2: ExpertStatusDefenseUp(); break;
            case SpeedUp2: ExpertStatusSpeedUp(); break;
            case SpAtkUp2: ExpertStatusSpAttackUp(); break;
            case SpDefUp2: ExpertStatusSpDefenseUp(); break;
            case AccUp2: ExpertStatusAccuracyUp(); break;
            case EvaUp2: ExpertStatusEvasionUp(); break;
            case AtkDown2: ExpertStatusAttackDown(); break;
            case DefDown2: ExpertStatusDefenseDown(); break;
            case SpeedDown2: ExpertStatusSpeedDown(); break;
            case SpAtkDown2: ExpertStatusSpAttackDown(); break;
            case SpDefDown2: ExpertStatusSpDefenseDown(); break;
            // The two-stage evasion and accuracy drops go to each other's routine, as in the original.
            case EvaDown2: ExpertStatusAccuracyDown(); break;
            case AccDown2: ExpertStatusEvasionDown(); break;
            case SetReflect: ExpertReflect(); break;
            case StatusPoison: ExpertStatusPoison(); break;
            case StatusParalyze: ExpertStatusParalyze(); break;
            case AtkUp2StatusConfusion: ExpertSwagger(); break;
            case LowerSpeedHit: ExpertSpeedDownOnHit(); break;
            case ChargeTurnHighCritFlinch: ExpertChargeTurnNoInvuln(); break;
            case PriorityNeg1BypassAccuracy: ExpertVitalThrow(); break;
            case SetSubstitute: ExpertSubstitute(); break;
            case RechargeAfter: ExpertRechargeTurn(); break;
            case StatusLeechSeed: ExpertToxicLeechSeed(); break;
            case Disable: ExpertDisable(); break;
            case Counter: ExpertCounter(); break;
            case Encore: ExpertEncore(); break;
            case AverageHp: ExpertPainSplit(); break;
            case DamageWhileAsleep: ExpertNightmare(); break;
            case NextAttackAlwaysHits: ExpertLockOn(); break;
            case UseRandomLearnedMoveSleep: ExpertSleepTalk(); break;
            case KoMonThatDefeatedUser: ExpertDestinyBond(); break;
            case IncreasePowerWithLessHp: ExpertReversal(); break;
            case CurePartyStatus: ExpertHealBell(); break;
            case StealHeldItem: ExpertThief(); break;
            case PreventEscape: ExpertBindingMove(); break;
            case EvaUp2Minimize: ExpertStatusEvasionUp(); break;
            case Curse: ExpertCurse(); break;
            case Protect: ExpertProtect(); break;
            case SetSpikes: ExpertSpikes(); break;
            case Foresight: ExpertForesight(); break;
            case SurviveWith1Hp: ExpertEndure(); break;
            case PassStatsAndStatus: ExpertBatonPass(); break;
            case HitBeforeSwitch: ExpertPursuit(); break;
            case HealHalfMoreInSun: ExpertSynthesis(); break;
            case Unused133: ExpertSynthesis(); break;
            case Unused134: ExpertSynthesis(); break;
            case WeatherRain: ExpertRainDance(); break;
            case WeatherSun: ExpertSunnyDay(); break;
            case MaxAtkLoseHalfMaxHp: ExpertBellyDrum(); break;
            case CopyStatChanges: ExpertPsychUp(); break;
            case MirrorCoat: ExpertMirrorCoat(); break;
            case ChargeTurnDefUp: ExpertChargeTurnNoInvuln(); break;
            case SkipChargeTurnInSun: ExpertChargeTurnNoInvuln(); break;
            // The original asks for SkipChargeTurnInSun a second time here and sends it to Expert_Thunder; it meant
            // Thunder's own effect. The first match above always wins, so that check is never reached: Thunder gets
            // nothing from this flag and ExpertThunder never runs.
            case Fly: ExpertChargeTurnWithInvuln(); break;
            case Unused157: ExpertRecovery(); break;
            case AlwaysFlinchFirstTurnOnly: ExpertFakeOut(); break;
            case SpitUp: ExpertSpitUp(); break;
            case Swallow: ExpertRecovery(); break;
            case WeatherHail: ExpertHail(); break;
            case SpAtkUpCauseConfusion: ExpertFlatter(); break;
            case FaintAndAtkSpAtkDown2: ExpertExplosion(); break;
            case DoublePowerWhenStatused: ExpertFacade(); break;
            case HitLastWhiffIfHit: ExpertFocusPunch(); break;
            case DoublePowerAndCureParalysis: ExpertSmellingSalts(); break;
            case SwitchHeldItems: ExpertTrick(); break;
            case CopyAbility: ExpertChangeUserAbility(); break;
            case GroundTrapUserContinuousHeal: ExpertIngrain(); break;
            case LowerOwnAtkAndDef: ExpertSuperpower(); break;
            case ApplyMagicCoat: ExpertMagicCoat(); break;
            case Recycle: ExpertRecycle(); break;
            case DoublePowerIfHit: ExpertRevenge(); break;
            case RemoveScreens: ExpertBrickBreak(); break;
            case RemoveHeldItem: ExpertKnockOff(); break;
            case SetHpEqualToUser: ExpertEndeavor(); break;
            case DecreasePowerWithLessUserHp: ExpertWaterSpout(); break;
            case SwitchAbilities: ExpertChangeUserAbility(); break;
            case MakeSharedMovesUnuseable: ExpertImprison(); break;
            case HealStatus: ExpertRefresh(); break;
            case StealStatusMove: ExpertSnatch(); break;
            case RecoilThird: ExpertRecoilMove(); break;
            case HighCriticalBurnHit: ExpertHighCritical(); break;
            case HalveElectricDamage: ExpertMudSport(); break;
            case UserSpAtkDown2: ExpertOverheat(); break;
            case AtkDefDown: ExpertStatusDefenseDown(); break;
            case DefSpdUp: ExpertStatusSpDefenseUp(); break;
            case AtkDefUp: ExpertStatusDefenseUp(); break;
            case HighCriticalPoisonHit: ExpertHighCritical(); break;
            case HalveFireDamage: ExpertWaterSport(); break;
            case SpAtkSpDefUp: ExpertStatusSpDefenseUp(); break;
            case AtkSpdUp: ExpertDragonDance(); break;
            case HealHalfRemoveFlyingType: ExpertRecovery(); break;
            case MoveEffectId.Gravity: ExpertGravity(); break;
            case IgnoreEvationRemoveDarkImmune: ExpertMiracleEye(); break;
            case DoublePowerHealSleep: ExpertWakeUpSlap(); break;
            case SpeedDownHit: ExpertHammerArm(); break;
            case PowerBasedOnLowSpeed: ExpertGyroBall(); break;
            case FaintAndFullHealNextMon: ExpertHealingWish(); break;
            case DoublePowerWhenBelowHalf: ExpertBrine(); break;
            case RemoveProtect: ExpertFeint(); break;
            case EatBerry: ExpertPluck(); break;
            case DoubleSpeed3Turns: ExpertTailwind(); break;
            case RandomStatUp2: ExpertAcupressure(); break;
            case MetalBurst: ExpertMetalBurst(); break;
            case SwitchHit: ExpertUTurn(); break;
            case DefSpdDownHit: ExpertCloseCombat(); break;
            case DoublePowerIfMovingSecond: ExpertPayback(); break;
            case DoublePowerIfTargetHit: ExpertAssurance(); break;
            case PreventItemUse: ExpertEmbargo(); break;
            case Fling: ExpertFling(); break;
            case TransferStatus: ExpertPsychoShift(); break;
            case HigherPowerWhenLowPp: ExpertTrumpCard(); break;
            case PreventHealing: ExpertHealBlock(); break;
            case IncreasePowerWithMoreHp: ExpertWringOut(); break;
            case SwapAtkDef: ExpertPowerTrick(); break;
            case SupressAbility: ExpertGastroAcid(); break;
            case PreventCrits: ExpertLuckyChant(); break;
            case UseMoveFirst: ExpertMeFirst(); break;
            case UseLastUsedMove: ExpertCopycat(); break;
            case SwapAtkSpAtkStatChanges: ExpertPowerSwap(); break;
            case SwapDefSpDefStatChanges: ExpertGuardSwap(); break;
            case IncreasePowerWithMoreStatUp: ExpertPunishment(); break;
            case FailIfNotUsedAllOtherMoves: ExpertLastResort(); break;
            case SetAbilityToInsomnia: ExpertWorrySeed(); break;
            case HitFirstIfTargetAttacking: ExpertSuckerPunch(); break;
            case ToxicSpikes: ExpertToxicSpikes(); break;
            case SwapStatChanges: ExpertHeartSwap(); break;
            case RestoreHpEveryTurn: ExpertAquaRing(); break;
            case GiveGroundImmunity: ExpertMagnetRise(); break;
            case RecoilBurnHit: ExpertRecoilMove(); break;
            case Dive: ExpertChargeTurnWithInvuln(); break;
            case Dig: ExpertChargeTurnWithInvuln(); break;
            case RemoveHazardsScreensEvaDown: ExpertDefog(); break;
            case MoveEffectId.TrickRoom: ExpertTrickRoom(); break;
            case Blizzard: ExpertBlizzard(); break;
            case RecoilParalyzeHit: ExpertRecoilMove(); break;
            case Bounce: ExpertChargeTurnWithInvuln(); break;
            case SpAtkDown2OppositeGender: ExpertCaptivate(); break;
            case StealthRock: ExpertStealthRock(); break;
            case RecoilHalf: ExpertRecoilMove(); break;
            case FaintFullRestoreNextMon: ExpertHealingWish(); break;
            case ShadowForce: ExpertShadowForce(); break;
        }
    }

    /// <summary><c>Expert_StatusSleep</c>: knowing a move that needs the target asleep (Dream Eater, Nightmare) makes putting it to sleep worth +1 half the time.</summary>
    private void ExpertStatusSleep()
    {
        if (KnowsEffect(Who.Attacker, RecoverDamageSleep) == true || KnowsEffect(Who.Attacker, StatusNightmare) == true)
        {
            if (!RandomBelow(128)) Score(1);
        }
    }

    /// <summary><c>Expert_DrainMove</c>: a draining hit the target resists or is immune to mostly gets −3.</summary>
    private void ExpertDrainMove()
    {
        int eff = Effectiveness();
        if ((eff is Eff.Immune or Eff.Half or Eff.Quarter) && !RandomBelow(50)) Score(-3);
    }

    /// <summary>
    /// <c>Expert_Explosion</c>: blowing up is worth less against a target that has raised its evasion, mostly worth
    /// less while the user is healthy (−3 when it also moves first), and a little more the lower its HP.
    /// </summary>
    private void ExpertExplosion()
    {
        if (Stage(Who.Defender, StatType.Evasion) >= 7)
        {
            Score(-1);
            if (Stage(Who.Defender, StatType.Evasion) >= 10 && !RandomBelow(128)) Score(-1);
        }

        if (HpPercent(Who.Attacker) >= 80 && SpeedCompare != SpeedOrder.Slower)
        {
            if (!RandomBelow(50)) Score(-3);
            return;
        }

        if (HpPercent(Who.Attacker) > 50)
        {
            if (!RandomBelow(50)) Score(-1);
            return;
        }
        if (!RandomBelow(128)) Score(1);
        if (HpPercent(Who.Attacker) > 30) return;
        if (!RandomBelow(50)) Score(1);
    }

    /// <summary><c>Expert_DreamEater</c>: −1 against a target that resists it or is immune; against a sleeping target, mostly +3.</summary>
    private void ExpertDreamEater()
    {
        int eff = Effectiveness();
        if (eff is Eff.Immune or Eff.Quarter or Eff.Half)
        {
            Score(-1);
            return;
        }
        if (HasStatus(Who.Defender, Cond.Sleep) && !RandomBelow(51)) Score(3);
    }

    /// <summary><c>Expert_MirrorMove_MoveTable</c>: the moves worth copying.</summary>
    private static readonly HashSet<string> MirrorMoveTable = new()
    {
        "Sleep Powder", "Lovely Kiss", "Spore", "Hypnosis", "Sing", "Grass Whistle", "Shadow Punch", "Sand Attack",
        "Smokescreen", "Toxic", "Guillotine", "Horn Drill", "Fissure", "Sheer Cold", "Cross Chop", "Aeroblast",
        "Confuse Ray", "Sweet Kiss", "Screech", "Cotton Spore", "Scary Face", "Fake Tears", "Metal Sound",
        "Thunder Wave", "Glare", "Poison Powder", "Shadow Ball", "Dynamic Punch", "Hyper Beam", "Extreme Speed",
        "Thief", "Covet", "Attract", "Swagger", "Torment", "Flatter", "Trick", "Superpower", "Skill Swap",
        "Psycho Shift", "Power Swap", "Guard Swap", "Sucker Punch", "Heart Swap", "Switcheroo", "Captivate", "Dark Void"
    };

    private static bool InMirrorMoveTable(MoveData? m) => m != null && MirrorMoveTable.Contains(m.Name);

    /// <summary>
    /// <c>Expert_MirrorMove</c>: when the user isn't the slower and the target's last move is one worth copying, +2
    /// half the time; when that move isn't worth copying, mostly −1.
    /// </summary>
    private void ExpertMirrorMove()
    {
        if (SpeedCompare != SpeedOrder.Slower && InMirrorMoveTable(PreviousMove(Who.Defender)))
        {
            if (!RandomBelow(128)) Score(2);
            return;
        }
        if (InMirrorMoveTable(PreviousMove(Who.Defender))) return;
        if (!RandomBelow(80)) Score(-1);
    }

    // The original's stat routines, Light Screen and Reflect keep tables of the types that were physical or special
    // before the split (Expert_*_PreSplit*Types); nothing reads them, so they are left out.

    /// <summary>
    /// <c>Expert_StatusAttackUp</c>: past +2 mostly −1, at full HP +2 half the time; above 70% HP nothing more, under
    /// 40% −2, between them mostly −2.
    /// </summary>
    private void ExpertStatusAttackUp()
    {
        if (Stage(Who.Attacker, StatType.Attack) >= 9)
        {
            if (!RandomBelow(100)) Score(-1);
        }
        else if (HpPercent(Who.Attacker) == 100)
        {
            if (!RandomBelow(128)) Score(2);
        }

        if (HpPercent(Who.Attacker) > 70) return;
        if (HpPercent(Who.Attacker) < 40)
        {
            Score(-2);
            return;
        }
        if (!RandomBelow(40)) Score(-2);
    }

    /// <summary>
    /// <c>Expert_StatusDefenseUp</c>: past +2 mostly −1, at full HP +2 half the time; from 70% HP up it mostly stops
    /// there. Otherwise −2 under 40% HP or when the target's last move was special, and mostly −2 for anything else
    /// (two rolls to escape it after a damaging move, one after a move of no power).
    /// </summary>
    private void ExpertStatusDefenseUp()
    {
        if (Stage(Who.Attacker, StatType.Defense) >= 9)
        {
            if (!RandomBelow(100)) Score(-1);
        }
        else if (HpPercent(Who.Attacker) == 100)
        {
            if (!RandomBelow(128)) Score(2);
        }

        if (HpPercent(Who.Attacker) >= 70 && RandomBelow(200)) return;

        if (HpPercent(Who.Attacker) < 40)
        {
            Score(-2);
            return;
        }
        if (PowerOf(PreviousMove(Who.Defender)) != 0)
        {
            if (DefenderLastMoveCategory == MoveCategory.Special)
            {
                Score(-2);
                return;
            }
            if (RandomBelow(60)) return;
            // and on into the roll for a move of no power
        }
        if (RandomBelow(60)) return;
        Score(-2);
    }

    /// <summary><c>Expert_StatusSpeedUp</c>: −3 unless the user is the slower, then mostly +3.</summary>
    private void ExpertStatusSpeedUp()
    {
        if (SpeedCompare != SpeedOrder.Slower)
        {
            Score(-3);
            return;
        }
        if (!RandomBelow(70)) Score(3);
    }

    /// <summary>
    /// <c>Expert_StatusSpAttackUp</c>: as for Attack (past +2 mostly −1, at full HP +2 half the time, nothing more
    /// above 70% HP, −2 under 40%), with a better chance to escape the −2 in between.
    /// </summary>
    private void ExpertStatusSpAttackUp()
    {
        if (Stage(Who.Attacker, StatType.SpAttack) >= 9)
        {
            if (!RandomBelow(100)) Score(-1);
        }
        else if (HpPercent(Who.Attacker) == 100)
        {
            if (!RandomBelow(128)) Score(2);
        }

        if (HpPercent(Who.Attacker) > 70) return;
        if (HpPercent(Who.Attacker) < 40)
        {
            Score(-2);
            return;
        }
        if (!RandomBelow(70)) Score(-2);
    }

    /// <summary>
    /// <c>Expert_StatusSpDefenseUp</c>: as for Defense, with a physical last move from the target where Defense has a
    /// special one.
    /// </summary>
    private void ExpertStatusSpDefenseUp()
    {
        if (Stage(Who.Attacker, StatType.SpDefense) >= 9)
        {
            if (!RandomBelow(100)) Score(-1);
        }
        else if (HpPercent(Who.Attacker) == 100)
        {
            if (!RandomBelow(128)) Score(2);
        }

        if (HpPercent(Who.Attacker) >= 70 && RandomBelow(200)) return;

        if (HpPercent(Who.Attacker) < 40)
        {
            Score(-2);
            return;
        }
        if (PowerOf(PreviousMove(Who.Defender)) != 0)
        {
            if (DefenderLastMoveCategory == MoveCategory.Physical)
            {
                Score(-2);
                return;
            }
            if (RandomBelow(60)) return;
            // and on into the roll for a move of no power
        }
        if (RandomBelow(60)) return;
        Score(-2);
    }

    /// <summary><c>Expert_StatusAccuracyUp</c>: past +2 mostly −2, and −2 at 70% HP or less.</summary>
    private void ExpertStatusAccuracyUp()
    {
        if (Stage(Who.Attacker, StatType.Accuracy) >= 9 && !RandomBelow(50)) Score(-2);
        if (HpPercent(Who.Attacker) > 70) return;
        Score(-2);
    }

    /// <summary>
    /// <c>Expert_StatusEvasionUp</c>: worth more at high HP and while the target wears down (Toxic, Leech Seed, a
    /// curse) or the user heals (Ingrain, Aqua Ring), less past +2; at 70% HP or less with evasion already moved, −2
    /// under 40% on either side and mostly −2 otherwise.
    /// </summary>
    private void ExpertStatusEvasionUp()
    {
        if (HpPercent(Who.Attacker) >= 90 && !RandomBelow(100)) Score(3);
        if (Stage(Who.Attacker, StatType.Evasion) >= 9 && !RandomBelow(128)) Score(-1);

        if (HasStatus(Who.Defender, Cond.Toxic) && (HpPercent(Who.Attacker) > 50 || !RandomBelow(80)))
        {
            if (!RandomBelow(50)) Score(3);
        }
        if (Has(Who.Defender, MonFx.LeechSeed) && !RandomBelow(70)) Score(3);
        if (Has(Who.Attacker, MonFx.Ingrain) || Has(Who.Attacker, MonFx.AquaRing))
        {
            if (!RandomBelow(128)) Score(2);
        }
        if (Has(Who.Defender, Vol.Curse) && !RandomBelow(70)) Score(3);

        if (HpPercent(Who.Attacker) > 70) return;
        if (Stage(Who.Attacker, StatType.Evasion) == 6) return;
        if (HpPercent(Who.Attacker) < 40 || HpPercent(Who.Defender) < 40)
        {
            Score(-2);
            return;
        }
        if (!RandomBelow(70)) Score(-2);
    }

    /// <summary>
    /// <c>Expert_BypassAccuracyMove</c>: a move that can't miss is worth +1 (and +1 more mostly) against evasion at +5
    /// or past, or the user's accuracy at −5 or below; mostly +1 from +3 or −3.
    /// </summary>
    private void ExpertBypassAccuracyMove()
    {
        if (Stage(Who.Defender, StatType.Evasion) > 10 || Stage(Who.Attacker, StatType.Accuracy) < 2)
        {
            Score(1);
            if (!RandomBelow(100)) Score(1);
            return;
        }
        if (Stage(Who.Defender, StatType.Evasion) > 8 || Stage(Who.Attacker, StatType.Accuracy) < 4)
        {
            if (!RandomBelow(100)) Score(1);
        }
    }

    /// <summary>
    /// <c>Expert_StatusAttackDown</c>: less worth it once the target's Attack has moved (−1, −1 more if the user isn't
    /// healthy, mostly −2 more at −3 or below), −2 against a target at 70% HP or less, and −2 half the time when its
    /// last move was special.
    /// </summary>
    private void ExpertStatusAttackDown()
    {
        if (Stage(Who.Defender, StatType.Attack) != 6)
        {
            Score(-1);
            if (HpPercent(Who.Attacker) <= 90) Score(-1);
            if (Stage(Who.Defender, StatType.Attack) <= 3 && !RandomBelow(50)) Score(-2);
        }
        if (HpPercent(Who.Defender) <= 70) Score(-2);
        if (DefenderLastMoveCategory != MoveCategory.Special) return;
        if (!RandomBelow(128)) Score(-2);
    }

    /// <summary>
    /// <c>Expert_StatusDefenseDown</c>: mostly −2 when the user is under 70% HP or the target's Defense is already at
    /// −3 or below, and −2 against a target at 70% HP or less.
    /// </summary>
    private void ExpertStatusDefenseDown()
    {
        if (HpPercent(Who.Attacker) < 70 || Stage(Who.Defender, StatType.Defense) <= 3)
        {
            if (!RandomBelow(50)) Score(-2);
        }
        if (HpPercent(Who.Defender) > 70) return;
        Score(-2);
    }

    /// <summary>
    /// <c>Expert_SpeedDownOnHit</c>: nothing for a hit the target resists or is immune to; Icy Wind, Rock Tomb and Mud
    /// Shot are judged as moves that lower Speed, every other such hit is left alone.
    /// </summary>
    private void ExpertSpeedDownOnHit()
    {
        int eff = Effectiveness();
        if (eff is Eff.Immune or Eff.Quarter or Eff.Half) return;
        if (MoveIs("Icy Wind") || MoveIs("Rock Tomb") || MoveIs("Mud Shot")) ExpertStatusSpeedDown();
    }

    /// <summary><c>Expert_StatusSpeedDown</c>: −3 unless the user is the slower, then mostly +2.</summary>
    private void ExpertStatusSpeedDown()
    {
        if (SpeedCompare != SpeedOrder.Slower)
        {
            Score(-3);
            return;
        }
        if (!RandomBelow(70)) Score(2);
    }

    /// <summary><c>Expert_StatusSpAttackDown</c>: as for Attack, with a physical last move from the target where Attack has a special one.</summary>
    private void ExpertStatusSpAttackDown()
    {
        if (Stage(Who.Defender, StatType.SpAttack) != 6)
        {
            Score(-1);
            if (HpPercent(Who.Attacker) <= 90) Score(-1);
            if (Stage(Who.Defender, StatType.SpAttack) <= 3 && !RandomBelow(50)) Score(-2);
        }
        if (HpPercent(Who.Defender) <= 70) Score(-2);
        if (DefenderLastMoveCategory != MoveCategory.Physical) return;
        if (!RandomBelow(128)) Score(-2);
    }

    /// <summary><c>Expert_StatusSpDefenseDown</c>: as for Defense, by the target's Sp. Def.</summary>
    private void ExpertStatusSpDefenseDown()
    {
        if (HpPercent(Who.Attacker) < 70 || Stage(Who.Defender, StatType.SpDefense) <= 3)
        {
            if (!RandomBelow(50)) Score(-2);
        }
        if (HpPercent(Who.Defender) > 70) return;
        Score(-2);
    }

    /// <summary>
    /// <c>Expert_StatusAccuracyDown</c>: less worth it as the HP of either side runs down or once the user's own
    /// accuracy is at −2 or below; more while the target wears down or the user heals. At 70% HP or less, unless the
    /// target's accuracy is untouched, −2 under 40% on either side and mostly −2 otherwise.
    /// </summary>
    private void ExpertStatusAccuracyDown()
    {
        if (HpPercent(Who.Attacker) < 70 || HpPercent(Who.Defender) <= 70)
        {
            if (!RandomBelow(100)) Score(-1);
        }
        if (Stage(Who.Attacker, StatType.Accuracy) <= 4 && !RandomBelow(80)) Score(-2);

        if (HasStatus(Who.Defender, Cond.Toxic) && !RandomBelow(70)) Score(2);
        if (Has(Who.Defender, MonFx.LeechSeed) && !RandomBelow(70)) Score(2);
        if (Has(Who.Attacker, MonFx.Ingrain) || Has(Who.Attacker, MonFx.AquaRing))
        {
            if (!RandomBelow(128)) Score(1);
        }
        if (Has(Who.Defender, Vol.Curse) && !RandomBelow(70)) Score(2);

        if (HpPercent(Who.Attacker) > 70) return;
        if (Stage(Who.Defender, StatType.Accuracy) == 6) return;
        if (HpPercent(Who.Attacker) < 40 || HpPercent(Who.Defender) < 40)
        {
            Score(-2);
            return;
        }
        if (!RandomBelow(70)) Score(-2);
    }

    /// <summary><c>Expert_StatusEvasionDown</c>: as for Defense, by the target's evasion.</summary>
    private void ExpertStatusEvasionDown()
    {
        if (HpPercent(Who.Attacker) < 70 || Stage(Who.Defender, StatType.Evasion) <= 3)
        {
            if (!RandomBelow(50)) Score(-2);
        }
        if (HpPercent(Who.Defender) > 70) return;
        Score(-2);
    }

    /// <summary>
    /// <c>Expert_Haze</c>: mostly −3 when the user is raised past +2 or the target lowered to −3 (the user's evasion,
    /// the target's accuracy); then mostly +3 when it is the other way round, and mostly −1 when it isn't.
    /// </summary>
    private void ExpertHaze()
    {
        if (Stage(Who.Attacker, StatType.Attack) > 8 || Stage(Who.Attacker, StatType.Defense) > 8
            || Stage(Who.Attacker, StatType.SpAttack) > 8 || Stage(Who.Attacker, StatType.SpDefense) > 8
            || Stage(Who.Attacker, StatType.Evasion) > 8
            || Stage(Who.Defender, StatType.Attack) < 4 || Stage(Who.Defender, StatType.Defense) < 4
            || Stage(Who.Defender, StatType.SpAttack) < 4 || Stage(Who.Defender, StatType.SpDefense) < 4
            || Stage(Who.Defender, StatType.Accuracy) < 4)
        {
            if (!RandomBelow(50)) Score(-3);
        }

        if (Stage(Who.Defender, StatType.Attack) > 8 || Stage(Who.Defender, StatType.Defense) > 8
            || Stage(Who.Defender, StatType.SpAttack) > 8 || Stage(Who.Defender, StatType.SpDefense) > 8
            || Stage(Who.Defender, StatType.Evasion) > 8
            || Stage(Who.Attacker, StatType.Attack) < 4 || Stage(Who.Attacker, StatType.Defense) < 4
            || Stage(Who.Attacker, StatType.SpAttack) < 4 || Stage(Who.Attacker, StatType.SpDefense) < 4
            || Stage(Who.Attacker, StatType.Accuracy) < 4)
        {
            if (!RandomBelow(50)) Score(3);
            return;
        }
        if (!RandomBelow(50)) Score(-1);
    }

    /// <summary><c>Expert_Bide</c>: −2 at 90% HP or less.</summary>
    private void ExpertBide()
    {
        if (HpPercent(Who.Attacker) > 90) return;
        Score(-2);
    }

    /// <summary>
    /// <c>Expert_ForceSwitch</c>: a target in for more than three turns gives two chances at +2; one with hazards on
    /// its side or a stat past +2 gives one; anything else −3.
    /// </summary>
    private void ExpertForceSwitch()
    {
        if (TurnsIn(Who.Defender) > 3)
        {
            if (!RandomBelow(64)) Score(2);
            // the original runs on into the next chance whether or not this one paid
            if (!RandomBelow(128)) Score(2);
            return;
        }
        if (SideHas(Who.Defender, SideFx.Spikes) || SideHas(Who.Defender, SideFx.StealthRock)
            || SideHas(Who.Defender, SideFx.ToxicSpikes)
            || Stage(Who.Defender, StatType.Attack) > 8 || Stage(Who.Defender, StatType.Defense) > 8
            || Stage(Who.Defender, StatType.SpAttack) > 8 || Stage(Who.Defender, StatType.SpDefense) > 8
            || Stage(Who.Defender, StatType.Evasion) > 8)
        {
            if (!RandomBelow(128)) Score(2);
            return;
        }
        Score(-3);
    }

    /// <summary><c>Expert_Conversion</c>: −2 at 90% HP or less, and mostly −2 after the battle's first turn.</summary>
    private void ExpertConversion()
    {
        if (HpPercent(Who.Attacker) <= 90) Score(-2);
        if (TurnCount == 0) return;
        if (RandomBelow(200)) Score(-2);
    }

    /// <summary>
    /// <c>Expert_Synthesis</c>: the moves that heal by the sun are judged as any healing move, after −2 in hail, rain
    /// or a sandstorm (not in fog).
    /// </summary>
    private void ExpertSynthesis()
    {
        var weather = CurrentWeather;
        if (weather is AiWeather.Hailing or AiWeather.Raining or AiWeather.Sandstorm) Score(-2);
        ExpertRecovery();
    }

    /// <summary>
    /// <c>Expert_Recovery</c>: −3 at full HP, −8 unless the user is the slower, mostly −3 from 70% HP up; otherwise
    /// mostly +2, less often when the target has shown Snatch. (The original's routine also holds a few lines no
    /// jump ever reaches; they are left out.)
    /// </summary>
    private void ExpertRecovery()
    {
        if (HpPercent(Who.Attacker) == 100)
        {
            Score(-3);
            return;
        }
        if (SpeedCompare != SpeedOrder.Slower)
        {
            Score(-8);
            return;
        }
        if (HpPercent(Who.Attacker) >= 70 && !RandomBelow(30))
        {
            Score(-3);
            return;
        }

        if (KnowsEffect(Who.Defender, StealStatusMove) != false && RandomBelow(100)) return;
        if (RandomBelow(20)) return;
        Score(2);
    }

    /// <summary>
    /// <c>Expert_ToxicLeechSeed</c>: with damaging moves to fall back on, mostly −3 for each side at half HP or less;
    /// then mostly +2 if the user knows a move of Sp. Def +1's effect or Protect's.
    /// </summary>
    private void ExpertToxicLeechSeed()
    {
        if (HasDamagingMoves)
        {
            if (HpPercent(Who.Attacker) <= 50 && !RandomBelow(50)) Score(-3);
            if (HpPercent(Who.Defender) <= 50 && !RandomBelow(50)) Score(-3);
        }
        if (KnowsEffect(Who.Attacker, SpDefUp) == true || KnowsEffect(Who.Attacker, Protect) == true)
        {
            if (!RandomBelow(60)) Score(2);
        }
    }

    /// <summary>
    /// <c>Expert_LightScreen</c>: −2 under half HP; otherwise +1 half the time from 90% up, and mostly +1 when the
    /// target's last move was special.
    /// </summary>
    private void ExpertLightScreen()
    {
        if (HpPercent(Who.Attacker) < 50)
        {
            Score(-2);
            return;
        }
        if (HpPercent(Who.Attacker) >= 90 && !RandomBelow(128)) Score(1);
        if (DefenderLastMoveCategory != MoveCategory.Special) return;
        if (!RandomBelow(64)) Score(1);
    }

    /// <summary>
    /// <c>Expert_Rest</c>: unless the user is the slower, −8 at full HP and −3 above half (mostly −3 from 40%); when
    /// slower, −3 above 70% and mostly −3 from 60%. Otherwise almost always +3, less often when the target has shown
    /// Snatch.
    /// </summary>
    private void ExpertRest()
    {
        if (SpeedCompare != SpeedOrder.Slower)
        {
            if (HpPercent(Who.Attacker) == 100)
            {
                Score(-8);
                return;
            }
            if (HpPercent(Who.Attacker) >= 40 && (HpPercent(Who.Attacker) > 50 || !RandomBelow(70)))
            {
                Score(-3);
                return;
            }
        }
        else if (HpPercent(Who.Attacker) >= 60 && (HpPercent(Who.Attacker) > 70 || !RandomBelow(50)))
        {
            Score(-3);
            return;
        }

        if (KnowsEffect(Who.Defender, StealStatusMove) != false && RandomBelow(50)) return;
        if (RandomBelow(10)) return;
        Score(3);
    }

    /// <summary><c>Expert_OHKOMove</c>: +1 a quarter of the time.</summary>
    private void ExpertOHKOMove()
    {
        if (RandomBelow(192)) return;
        Score(1);
    }

    /// <summary><c>Expert_SuperFang</c>: −1 against a target at half HP or less.</summary>
    private void ExpertSuperFang()
    {
        if (HpPercent(Who.Defender) > 50) return;
        Score(-1);
    }

    /// <summary><c>Expert_BindingMove</c>: +1 half the time against a target that is badly poisoned, cursed, doomed by Perish Song or in love.</summary>
    private void ExpertBindingMove()
    {
        if (HasStatus(Who.Defender, Cond.Toxic) || Has(Who.Defender, Vol.Curse) || Has(Who.Defender, MonFx.PerishSong)
            || Has(Who.Defender, Vol.Attract))
        {
            if (!RandomBelow(128)) Score(1);
        }
    }

    /// <summary>
    /// <c>Expert_HighCritical</c>: nothing against a target that resists or is immune; +1 half the time when the hit is
    /// super effective, a quarter of the time otherwise.
    /// </summary>
    private void ExpertHighCritical()
    {
        int eff = Effectiveness();
        if (eff is Eff.Immune or Eff.Quarter or Eff.Half) return;
        if (eff is not (Eff.Double or Eff.Quadruple) && RandomBelow(128)) return;
        if (RandomBelow(128)) return;
        Score(1);
    }

    /// <summary>
    /// <c>Expert_Swagger</c>: with Psych Up to follow, −5 unless the target's Attack is already at −3 or below, else
    /// +3 (+5 on the battle's first turn); without it, judged as Flatter.
    /// </summary>
    private void ExpertSwagger()
    {
        if (Knows(Who.Attacker, "Psych Up") == true)
        {
            if (Stage(Who.Defender, StatType.Attack) > 3)
            {
                Score(-5);
                return;
            }
            Score(3);
            if (TurnCount != 0) return;
            Score(2);
            return;
        }
        ExpertFlatter();
    }

    /// <summary><c>Expert_Flatter</c>: +1 half the time, then judged as any move that confuses.</summary>
    private void ExpertFlatter()
    {
        if (!RandomBelow(128)) Score(1);
        ExpertStatusConfuse();
    }

    /// <summary>
    /// <c>Expert_StatusConfuse</c>: against a target at 70% HP or less, −1 half the time, then −1 at half HP or less
    /// and −1 more at 30% or less.
    /// </summary>
    private void ExpertStatusConfuse()
    {
        if (HpPercent(Who.Defender) > 70) return;
        if (!RandomBelow(128)) Score(-1);
        if (HpPercent(Who.Defender) > 50) return;
        Score(-1);
        if (HpPercent(Who.Defender) > 30) return;
        Score(-1);
    }

    /// <summary><c>Expert_Reflect</c>: as Light Screen, by a physical last move from the target.</summary>
    private void ExpertReflect()
    {
        if (HpPercent(Who.Attacker) < 50)
        {
            Score(-2);
            return;
        }
        if (HpPercent(Who.Attacker) >= 90 && !RandomBelow(128)) Score(1);
        if (DefenderLastMoveCategory != MoveCategory.Physical) return;
        if (!RandomBelow(64)) Score(1);
    }

    /// <summary><c>Expert_StatusPoison</c>: −1 when the user is under half HP or the target at half or less.</summary>
    private void ExpertStatusPoison()
    {
        if (HpPercent(Who.Attacker) < 50 || HpPercent(Who.Defender) <= 50) Score(-1);
    }

    /// <summary><c>Expert_StatusParalyze</c>: mostly +3 when the user is the slower; otherwise −1 at 70% HP or less.</summary>
    private void ExpertStatusParalyze()
    {
        if (SpeedCompare == SpeedOrder.Slower)
        {
            if (!RandomBelow(20)) Score(3);
            return;
        }
        if (HpPercent(Who.Attacker) > 70) return;
        Score(-1);
    }

    /// <summary>
    /// <c>Expert_VitalThrow</c>: a hit that goes last but can't miss: when the user isn't the slower and is at 60% HP
    /// or less, mostly −1 under 40%, sometimes −1 otherwise.
    /// </summary>
    private void ExpertVitalThrow()
    {
        if (SpeedCompare == SpeedOrder.Slower) return;
        if (HpPercent(Who.Attacker) > 60) return;
        if (HpPercent(Who.Attacker) >= 40 && RandomBelow(180)) return;
        if (RandomBelow(50)) return;
        Score(-1);
    }

    /// <summary>
    /// <c>Expert_Substitute</c>: +1 sometimes with Focus Punch to follow; at 90% HP or less one to three chances of −1
    /// the lower the HP; and, unless the user is the slower, sometimes +1 when the target's last move was one that
    /// poisons, burns, paralyses, puts to sleep, confuses or seeds and the target itself isn't under it.
    /// </summary>
    private void ExpertSubstitute()
    {
        if (Knows(Who.Attacker, "Focus Punch") != false && !RandomBelow(96)) Score(1);

        if (HpPercent(Who.Attacker) <= 90)
        {
            if (HpPercent(Who.Attacker) <= 50 && !RandomBelow(100)) Score(-1);
            if (HpPercent(Who.Attacker) <= 70 && !RandomBelow(100)) Score(-1);
            if (!RandomBelow(100)) Score(-1);
        }

        if (SpeedCompare == SpeedOrder.Slower) return;
        // The original asks whether the target (the one that used the move) is under its effect, not the user
        switch (EffectOf(PreviousMove(Who.Defender)))
        {
            case StatusSleep or StatusBadlyPoison or StatusPoison or StatusParalyze or StatusBurn:
                if (HasStatus(Who.Defender, Cond.Any)) return;
                break;
            case StatusConfuse:
                if (Has(Who.Defender, Vol.Confusion)) return;
                break;
            case StatusLeechSeed:
                if (Has(Who.Defender, MonFx.LeechSeed)) return;
                break;
            default:
                return;
        }
        if (RandomBelow(100)) return;
        Score(1);
    }

    /// <summary>
    /// <c>Expert_RechargeTurn</c>: −1 against a target that resists or is immune; with Truant mostly +1; otherwise −1
    /// when the slower user is at 60% HP or more, or the faster above 40%.
    /// </summary>
    private void ExpertRechargeTurn()
    {
        int eff = Effectiveness();
        if (eff is Eff.Immune or Eff.Quarter or Eff.Half)
        {
            Score(-1);
            return;
        }
        if (AbilityOf(Who.Attacker) == "Truant")
        {
            if (!RandomBelow(80)) Score(1);
            return;
        }
        if (SpeedCompare == SpeedOrder.Slower)
        {
            if (HpPercent(Who.Attacker) < 60) return;
            Score(-1);
            return;
        }
        if (HpPercent(Who.Attacker) > 40) Score(-1);
    }

    /// <summary>
    /// <c>Expert_Disable</c>: unless the user is the slower, +1 when the target's last move had power, and sometimes
    /// −1 when it had none.
    /// </summary>
    private void ExpertDisable()
    {
        if (SpeedCompare == SpeedOrder.Slower) return;
        if (PowerOf(PreviousMove(Who.Defender)) == 0)
        {
            if (!RandomBelow(100)) Score(-1);
            return;
        }
        Score(1);
    }

    /// <summary><c>Expert_Counter_PhysicalTypes</c>: the types whose moves were all physical before the split.</summary>
    private static readonly HashSet<PokemonType> CounterPhysicalTypes = new()
    {
        PokemonType.Normal, PokemonType.Fighting, PokemonType.Flying, PokemonType.Poison, PokemonType.Ground,
        PokemonType.Rock, PokemonType.Bug, PokemonType.Ghost, PokemonType.Steel
    };

    /// <summary>
    /// <c>Expert_Counter</c>: −1 against a target asleep, in love or confused; chances of −1 at half HP or less and at
    /// 30% or less. With Mirror Coat known, sometimes +4. After a damaging last move from the target, sometimes +1 if
    /// it is taunted, then −1 unless that move was physical and else sometimes +1; after one of no power, sometimes +1
    /// if taunted, and against a target of no physical type a chance at +4.
    /// </summary>
    private void ExpertCounter()
    {
        if (HasStatus(Who.Defender, Cond.Sleep) || Has(Who.Defender, Vol.Attract) || Has(Who.Defender, Vol.Confusion))
        {
            Score(-1);
            return;
        }
        if (HpPercent(Who.Attacker) <= 30 && !RandomBelow(10)) Score(-1);
        if (HpPercent(Who.Attacker) <= 50 && !RandomBelow(100)) Score(-1);

        if (Knows(Who.Attacker, "Mirror Coat") != true)
        {
            if (PowerOf(PreviousMove(Who.Defender)) != 0)
            {
                if (Taunted(Who.Defender) && !RandomBelow(100)) Score(1);
                if (DefenderLastMoveCategory != MoveCategory.Physical)
                {
                    Score(-1);
                    return;
                }
                if (RandomBelow(100)) return;
                Score(1);
                return;
            }

            if (Taunted(Who.Defender) && !RandomBelow(100)) Score(1);
            if (CounterPhysicalTypes.Contains(Type1(Who.Defender))) return;
            if (CounterPhysicalTypes.Contains(Type2(Who.Defender))) return;
            if (RandomBelow(50)) return;
            // and on into the chance at +4
        }
        if (RandomBelow(100)) return;
        Score(4);
    }

    /// <summary><c>Expert_Encore_EncouragedMoveEffects</c>: the effects worth locking the target into.</summary>
    private static readonly HashSet<MoveEffectId> EncoreEncouragedMoveEffects = new()
    {
        // The original lists SwitchAbilities twice
        RecoverDamageSleep, AtkUp, DefUp, SpeedUp, SpAtkUp, ResetStatChanges, ForceSwitch, Conversion,
        StatusBadlyPoison, SetLightScreen, Rest, HalveHp, SpDefUp2, StatusConfuse, StatusPoison, StatusParalyze,
        StatusLeechSeed, DoNothing, AtkUp2, Encore, Conversion2, NextAttackAlwaysHits, CurePartyStatus, PreventEscape,
        StatusNightmare, Protect, SwitchAbilities, Foresight, AllFaint3Turns, WeatherSandstorm,
        SurviveWith1Hp, AtkUp2StatusConfusion, Infatuate, PreventStatus, WeatherRain, WeatherSun, MaxAtkLoseHalfMaxHp,
        CopyStatChanges, HitIn3Turns, AlwaysFlinchFirstTurnOnly, MoveEffectId.Stockpile, SpitUp, Swallow, WeatherHail,
        Torment, StatusBurn, MakeGlobalTarget, SpDefUpDoubleElectricPower, SwitchHeldItems, CopyAbility,
        GroundTrapUserContinuousHeal, Recycle, RemoveHeldItem, MakeSharedMovesUnuseable, HealStatus,
        RemoveAllPpOnDefeat, ConfuseAll, HalveElectricDamage, HalveFireDamage, AtkSpdUp, Camouflage,
        MoveEffectId.Gravity, IgnoreEvationRemoveDarkImmune, FaintAndFullHealNextMon, NaturalGift, RemoveProtect,
        DoubleSpeed3Turns, RandomStatUp2, Fling, TransferStatus, PreventHealing, SwapAtkDef, SupressAbility,
        PreventCrits, SwapAtkSpAtkStatChanges, SwapDefSpDefStatChanges, SetAbilityToInsomnia, SwapStatChanges,
        RestoreHpEveryTurn, GiveGroundImmunity, MoveEffectId.TrickRoom
    };

    /// <summary>
    /// <c>Expert_Encore</c>: mostly +3 against a disabled target, or when the user isn't the slower and the target's
    /// last move is one worth repeating; otherwise −2.
    /// </summary>
    private void ExpertEncore()
    {
        if (!Disabled(Who.Defender))
        {
            if (SpeedCompare == SpeedOrder.Slower || !EncoreEncouragedMoveEffects.Contains(EffectOf(PreviousMove(Who.Defender))))
            {
                Score(-2);
                return;
            }
        }
        if (RandomBelow(30)) return;
        Score(3);
    }

    /// <summary>
    /// <c>Expert_PainSplit</c>: −1 against a target under 80% HP; otherwise +1 when the user is low (60% or less if the
    /// slower, 40% or less if not) and −1 when it isn't.
    /// </summary>
    private void ExpertPainSplit()
    {
        if (HpPercent(Who.Defender) < 80)
        {
            Score(-1);
            return;
        }
        if (SpeedCompare == SpeedOrder.Slower)
        {
            if (HpPercent(Who.Attacker) > 60)
            {
                Score(-1);
                return;
            }
            Score(1);
            return;
        }
        if (HpPercent(Who.Attacker) > 40)
        {
            Score(-1);
            return;
        }
        Score(1);
    }

    /// <summary><c>Expert_Nightmare</c>: +2.</summary>
    private void ExpertNightmare() => Score(2);

    /// <summary><c>Expert_LockOn</c>: +2 half the time.</summary>
    private void ExpertLockOn()
    {
        if (RandomBelow(128)) return;
        Score(2);
    }

    /// <summary><c>Expert_SleepTalk</c>: +10 while the user is asleep, −5 otherwise.</summary>
    private void ExpertSleepTalk()
    {
        if (HasStatus(Who.Attacker, Cond.Sleep))
        {
            Score(10);
            return;
        }
        Score(-5);
    }

    /// <summary>
    /// <c>Expert_DestinyBond</c>: −1; then, unless the user is the slower, a chance of +1 at 70% HP or less, another at
    /// half or less, and a chance of +2 at 30% or less.
    /// </summary>
    private void ExpertDestinyBond()
    {
        Score(-1);
        if (SpeedCompare == SpeedOrder.Slower) return;
        if (HpPercent(Who.Attacker) > 70) return;
        if (!RandomBelow(128)) Score(1);
        if (HpPercent(Who.Attacker) > 50) return;
        if (!RandomBelow(128)) Score(1);
        if (HpPercent(Who.Attacker) > 30) return;
        if (RandomBelow(100)) return;
        Score(2);
    }

    /// <summary>
    /// <c>Expert_Reversal</c>: −1 while the user is healthy (above 60% HP if the slower, above 33% if not), nothing a
    /// little lower, then mostly +1 (+1 more under 8% when not the slower).
    /// </summary>
    private void ExpertReversal()
    {
        if (SpeedCompare == SpeedOrder.Slower)
        {
            if (HpPercent(Who.Attacker) > 60)
            {
                Score(-1);
                return;
            }
            if (HpPercent(Who.Attacker) > 40) return;
        }
        else
        {
            if (HpPercent(Who.Attacker) > 33)
            {
                Score(-1);
                return;
            }
            if (HpPercent(Who.Attacker) > 20) return;
            if (HpPercent(Who.Attacker) < 8) Score(1);
        }
        if (RandomBelow(100)) return;
        Score(1);
    }

    /// <summary><c>Expert_HealBell</c>: −5 when neither the user nor anyone on its bench has a condition to cure.</summary>
    private void ExpertHealBell()
    {
        if (HasStatus(Who.Attacker, Cond.Any)) return;
        if (BenchHasStatus(Who.Attacker, Cond.Any)) return;
        Score(-5);
    }

    /// <summary><c>Expert_Thief_EncouragedItemEffects</c>: the hold effects worth taking (berries that cure or heal, the Bright Powder, Leftovers, the species' own boosters).</summary>
    private static readonly HashSet<string> ThiefEncouragedItemEffects = new()
    {
        "SlpRestore", "StatusRestore", "HpRestore", "AccReduce", "HpRestoreGradual", "PikaSpatkUp", "CuboneAtkUp",
        "WeakenSeFire", "WeakenSeWater", "WeakenSeElectric", "WeakenSeGrass", "WeakenSeIce", "WeakenSeFight",
        "WeakenSePoison", "WeakenSeGround", "WeakenSeFlying", "WeakenSePsychic", "WeakenSeBug", "WeakenSeRock",
        "WeakenSeGhost", "WeakenSeDragon", "WeakenSeDark", "WeakenSeSteel", "WeakenNormal", "HpRestorePsnType"
    };

    /// <summary><c>Expert_Thief</c>: −2 unless the target is known to hold an item worth taking, then mostly +1.</summary>
    private void ExpertThief()
    {
        if (HoldEffect(Who.Defender) is not { } held || !ThiefEncouragedItemEffects.Contains(held))
        {
            Score(-2);
            return;
        }
        if (RandomBelow(50)) return;
        Score(1);
    }

    /// <summary>
    /// <c>Expert_Curse</c>: a Ghost user's curse is −1 at 80% HP or less. Otherwise nothing past Defense +3; else
    /// chances of +1 (one more with Gyro Ball or Trick Room known), and more chances while its Defense is at +1 or
    /// below and again at +0 or below.
    /// </summary>
    private void ExpertCurse()
    {
        if (Type1(Who.Attacker) == PokemonType.Ghost || Type2(Who.Attacker) == PokemonType.Ghost)
        {
            if (HpPercent(Who.Attacker) > 80) return;
            Score(-1);
            return;
        }
        if (Stage(Who.Attacker, StatType.Defense) > 9) return;

        if (Knows(Who.Attacker, "Gyro Ball") == true || Knows(Who.Attacker, "Trick Room") == true)
        {
            if (!RandomBelow(32))
            {
                Score(1);
                // the original runs on into the plain chance after this +1
                if (!RandomBelow(128)) Score(1);
            }
        }
        else if (!RandomBelow(128)) Score(1);

        if (Stage(Who.Attacker, StatType.Defense) > 7) return;
        if (!RandomBelow(128)) Score(1);
        if (Stage(Who.Attacker, StatType.Defense) > 6) return;
        if (!RandomBelow(128)) Score(1);
    }

    /// <summary>
    /// <c>Expert_Protect</c>: −2 half the time if the target has shown Feint or Shadow Force; −2 after more than one
    /// protection in a row, or (unless locked onto) while the user wears down or the target has shown a way to heal or
    /// Defense Curl. Otherwise +2 while the target wears down, in a double battle, when locked onto, or sometimes; then
    /// −1 half the time, and −1 and maybe −1 more after a protection last turn.
    /// </summary>
    private void ExpertProtect()
    {
        if (Knows(Who.Defender, "Feint") == true || Knows(Who.Defender, "Shadow Force") == true)
        {
            if (!RandomBelow(128)) Score(-2);
        }

        if (ProtectChain(Who.Attacker) > 1)
        {
            Score(-2);
            return;
        }
        if (HasStatus(Who.Attacker, Cond.Toxic) || Has(Who.Attacker, Vol.Curse) || Has(Who.Attacker, MonFx.PerishSong)
            || Has(Who.Attacker, Vol.Attract) || Has(Who.Attacker, MonFx.LeechSeed) || Has(Who.Attacker, MonFx.Yawn)
            || KnowsEffect(Who.Defender, RestoreHalfHp) == true
            || KnowsEffect(Who.Defender, DefUpDoubleRolloutPower) == true)
        {
            if (Has(Who.Attacker, MonFx.LockOn)) return;
            Score(-2);
            return;
        }
        if (HasStatus(Who.Defender, Cond.Toxic) || Has(Who.Defender, Vol.Curse) || Has(Who.Defender, MonFx.PerishSong)
            || Has(Who.Defender, Vol.Attract) || Has(Who.Defender, MonFx.LeechSeed) || Has(Who.Defender, MonFx.Yawn)
            || IsDouble || Has(Who.Attacker, MonFx.LockOn) || RandomBelow(85))
        {
            Score(2);
        }

        if (!RandomBelow(128)) Score(-1);
        if (ProtectChain(Who.Attacker) == 0) return;
        Score(-1);
        if (RandomBelow(128)) return;
        Score(-1);
    }

    /// <summary><c>Expert_Spikes</c>: +1 half the time, then mostly +1 more with Roar or Whirlwind known.</summary>
    private void ExpertSpikes()
    {
        if (RandomBelow(128)) return;
        Score(1);
        if (Knows(Who.Attacker, "Roar") == true || Knows(Who.Attacker, "Whirlwind") == true)
        {
            if (!RandomBelow(64)) Score(1);
        }
    }

    /// <summary>
    /// <c>Expert_Foresight</c>: +2 at two rolls' chance for a Ghost user, at one against a target with evasion past +2;
    /// −2 otherwise.
    /// </summary>
    private void ExpertForesight()
    {
        // BUG (kept): the Ghost check reads the user's types, where the target's were meant
        if (Type1(Who.Attacker) == PokemonType.Ghost || Type2(Who.Attacker) == PokemonType.Ghost)
        {
            if (RandomBelow(80)) return;
            if (RandomBelow(80)) return;
            Score(2);
            return;
        }
        if (Stage(Who.Defender, StatType.Evasion) > 8)
        {
            if (RandomBelow(80)) return;
            Score(2);
            return;
        }
        Score(-2);
    }

    /// <summary><c>Expert_Endure</c>: mostly +1 from 4% to under 35% HP, −1 otherwise.</summary>
    private void ExpertEndure()
    {
        if (HpPercent(Who.Attacker) >= 4 && HpPercent(Who.Attacker) < 35)
        {
            if (!RandomBelow(70)) Score(1);
            return;
        }
        Score(-1);
    }

    /// <summary>
    /// <c>Expert_BatonPass</c>: with a stat past +2, mostly +2 once the user is worn down (70% HP or less if the
    /// slower, 60% or less if not); with one at +2, −2 while it isn't (from 70% if the slower, above 60% if not);
    /// with nothing raised, −2.
    /// </summary>
    private void ExpertBatonPass()
    {
        if (Stage(Who.Attacker, StatType.Attack) > 8 || Stage(Who.Attacker, StatType.Defense) > 8
            || Stage(Who.Attacker, StatType.SpAttack) > 8 || Stage(Who.Attacker, StatType.SpDefense) > 8
            || Stage(Who.Attacker, StatType.Evasion) > 8)
        {
            if (SpeedCompare == SpeedOrder.Slower)
            {
                if (HpPercent(Who.Attacker) > 70) return;
            }
            else if (HpPercent(Who.Attacker) > 60) return;
            if (RandomBelow(80)) return;
            Score(2);
            return;
        }

        if (Stage(Who.Attacker, StatType.Attack) > 7 || Stage(Who.Attacker, StatType.Defense) > 7
            || Stage(Who.Attacker, StatType.SpAttack) > 7 || Stage(Who.Attacker, StatType.SpDefense) > 7
            || Stage(Who.Attacker, StatType.Evasion) > 7)
        {
            if (SpeedCompare == SpeedOrder.Slower)
            {
                if (HpPercent(Who.Attacker) < 70) return;
            }
            else if (HpPercent(Who.Attacker) <= 60) return;
        }
        Score(-2);
    }

    /// <summary>
    /// <c>Expert_Pursuit</c>: +1 half the time on the user's first turn in, or against a Ghost or Psychic target; then
    /// +1 half the time if the target has shown U-turn.
    /// </summary>
    private void ExpertPursuit()
    {
        if (FirstTurnIn(Who.Attacker)
            || Type1(Who.Defender) == PokemonType.Ghost || Type1(Who.Defender) == PokemonType.Psychic
            || Type2(Who.Defender) == PokemonType.Ghost || Type2(Who.Defender) == PokemonType.Psychic)
        {
            if (!RandomBelow(128)) Score(1);
        }
        if (Knows(Who.Defender, "U-turn") == false) return;
        if (RandomBelow(128)) return;
        Score(1);
    }

    /// <summary>
    /// <c>Expert_RainDance</c>: +1 for a Swift Swim user that isn't the faster; otherwise −1 under 40% HP, +1 to wash
    /// out hail, sun or sand, and +1 for Rain Dish, or Hydration with a condition to cure.
    /// </summary>
    private void ExpertRainDance()
    {
        if (SpeedCompare != SpeedOrder.Faster && AbilityOf(Who.Attacker) == "Swift Swim")
        {
            Score(1);
            return;
        }
        if (HpPercent(Who.Attacker) < 40)
        {
            Score(-1);
            return;
        }
        var weather = CurrentWeather;
        if (weather is AiWeather.Hailing or AiWeather.Sunny or AiWeather.Sandstorm)
        {
            Score(1);
            return;
        }
        var ability = AbilityOf(Who.Attacker);
        if (ability == "Rain Dish")
        {
            Score(1);
            return;
        }
        if (ability != "Hydration") return;
        if (HasStatus(Who.Attacker, Cond.Any)) Score(1);
    }

    /// <summary>
    /// <c>Expert_SunnyDay</c>: −1 under 40% HP; +1 to clear hail, rain or sand, and +1 for Flower Gift, or Leaf Guard
    /// with a condition already on.
    /// </summary>
    private void ExpertSunnyDay()
    {
        if (HpPercent(Who.Attacker) < 40)
        {
            Score(-1);
            return;
        }
        var weather = CurrentWeather;
        if (weather is AiWeather.Hailing or AiWeather.Raining or AiWeather.Sandstorm)
        {
            Score(1);
            return;
        }
        var ability = AbilityOf(Who.Attacker);
        if (ability == "Flower Gift")
        {
            Score(1);
            return;
        }
        if (ability != "Leaf Guard") return;
        // BUG (kept): Leaf Guard only keeps a condition off, yet it is rewarded when the user already has one
        if (HasStatus(Who.Attacker, Cond.Any)) Score(1);
    }

    /// <summary><c>Expert_BellyDrum</c>: −2 under 90% HP.</summary>
    private void ExpertBellyDrum()
    {
        if (HpPercent(Who.Attacker) < 90) Score(-2);
    }

    /// <summary>
    /// <c>Expert_PsychUp</c>: when the target has a stat past +2, +1 if the user's Attack, Defense, Sp. Atk or Sp. Def
    /// is at +0 or below, else +2 if its evasion is, else mostly −2; −2 when the target has nothing to copy.
    /// </summary>
    private void ExpertPsychUp()
    {
        if (Stage(Who.Defender, StatType.Attack) > 8 || Stage(Who.Defender, StatType.Defense) > 8
            || Stage(Who.Defender, StatType.SpAttack) > 8 || Stage(Who.Defender, StatType.SpDefense) > 8
            || Stage(Who.Defender, StatType.Evasion) > 8)
        {
            if (Stage(Who.Attacker, StatType.Attack) < 7 || Stage(Who.Attacker, StatType.Defense) < 7
                || Stage(Who.Attacker, StatType.SpAttack) < 7 || Stage(Who.Attacker, StatType.SpDefense) < 7)
            {
                Score(1);
                return;
            }
            if (Stage(Who.Attacker, StatType.Evasion) < 7)
            {
                // the original's +1 running into the +1 above
                Score(2);
                return;
            }
            if (RandomBelow(50)) return;
        }
        Score(-2);
    }

    /// <summary><c>Expert_MirrorCoat_SpecialTypes</c>: the types whose moves were all special before the split.</summary>
    private static readonly HashSet<PokemonType> MirrorCoatSpecialTypes = new()
    {
        PokemonType.Fire, PokemonType.Water, PokemonType.Grass, PokemonType.Electric, PokemonType.Psychic,
        PokemonType.Ice, PokemonType.Dragon, PokemonType.Dark
    };

    /// <summary><c>Expert_MirrorCoat</c>: as Counter, with Counter as the partner move, a special last move and the special types.</summary>
    private void ExpertMirrorCoat()
    {
        if (HasStatus(Who.Defender, Cond.Sleep) || Has(Who.Defender, Vol.Attract) || Has(Who.Defender, Vol.Confusion))
        {
            Score(-1);
            return;
        }
        if (HpPercent(Who.Attacker) <= 30 && !RandomBelow(10)) Score(-1);
        if (HpPercent(Who.Attacker) <= 50 && !RandomBelow(100)) Score(-1);

        if (Knows(Who.Attacker, "Counter") != true)
        {
            if (PowerOf(PreviousMove(Who.Defender)) != 0)
            {
                if (Taunted(Who.Defender) && !RandomBelow(100)) Score(1);
                if (DefenderLastMoveCategory != MoveCategory.Special)
                {
                    Score(-1);
                    return;
                }
                if (RandomBelow(100)) return;
                Score(1);
                return;
            }

            if (Taunted(Who.Defender) && !RandomBelow(100)) Score(1);
            if (MirrorCoatSpecialTypes.Contains(Type1(Who.Defender))) return;
            if (MirrorCoatSpecialTypes.Contains(Type2(Who.Defender))) return;
            if (RandomBelow(50)) return;
            // and on into the chance at +4
        }
        if (RandomBelow(100)) return;
        Score(4);
    }

    /// <summary>
    /// <c>Expert_ChargeTurnNoInvuln</c>: −2 against a target that resists or is immune; +2 for a sun move in the sun,
    /// or with a Power Herb; −2 if the target has shown Protect's effect, and −1 at 38% HP or less.
    /// </summary>
    private void ExpertChargeTurnNoInvuln()
    {
        int eff = Effectiveness();
        if (eff is Eff.Immune or Eff.Quarter or Eff.Half)
        {
            Score(-2);
            return;
        }
        if (Effect == SkipChargeTurnInSun && CurrentWeather == AiWeather.Sunny)
        {
            Score(2);
            return;
        }
        if (Holds(Who.Attacker, "Power Herb"))
        {
            Score(2);
            return;
        }
        if (KnowsEffect(Who.Defender, Protect) == true)
        {
            Score(-2);
            return;
        }
        if (HpPercent(Who.Attacker) > 38) return;
        Score(-1);
    }

    /// <summary>
    /// <c>Expert_Thunder</c>: mostly −3 against a target that resists or is immune, or in the sun; +1 in the rain.
    /// (Never run: <see cref="Expert"/> keeps the original's dispatch, which never sends Thunder here.)
    /// </summary>
    private void ExpertThunder()
    {
        int eff = Effectiveness();
        if (eff is not (Eff.Immune or Eff.Half or Eff.Quarter))
        {
            var weather = CurrentWeather;
            if (weather != AiWeather.Sunny)
            {
                if (weather == AiWeather.Raining) Score(1);
                return;
            }
        }
        if (RandomBelow(50)) return;
        Score(-3);
    }

    /// <summary>
    /// <c>Expert_ChargeTurnWithInvuln</c>: +2 with a Power Herb; −1 if the target has shown Protect's effect; otherwise
    /// judged as Shadow Force.
    /// </summary>
    private void ExpertChargeTurnWithInvuln()
    {
        // the original jumps into ChargeTurnNoInvuln's +2
        if (Holds(Who.Attacker, "Power Herb"))
        {
            Score(2);
            return;
        }
        if (KnowsEffect(Who.Defender, Protect) == false)
        {
            ExpertShadowForce();
            return;
        }
        Score(-1);
    }

    /// <summary><c>Expert_ChargeTurnWithInvuln_SandImmuneTypes</c>.</summary>
    private static readonly HashSet<PokemonType> ChargeTurnWithInvulnSandImmuneTypes = new()
    {
        PokemonType.Ground, PokemonType.Rock, PokemonType.Steel
    };

    /// <summary>
    /// <c>Expert_ShadowForce</c>: +1 against a target that resists or is immune (as the original has it) or with a
    /// Power Herb; otherwise mostly +1 while the target wears down (Toxic, a curse, Leech Seed), while the user is
    /// spared the sandstorm or hail, or when it isn't the slower and the target's last move wasn't Lock-On's.
    /// </summary>
    private void ExpertShadowForce()
    {
        int eff = Effectiveness();
        // Quirk kept: a hit the target resists or is immune to is rewarded here, not marked down
        if (eff is Eff.Immune or Eff.Quarter or Eff.Half)
        {
            Score(1);
            return;
        }
        if (Holds(Who.Attacker, "Power Herb"))
        {
            Score(1);
            return;
        }
        if (!WorthHiding()) return;
        if (RandomBelow(80)) return;
        Score(1);

        bool WorthHiding()
        {
            if (HasStatus(Who.Defender, Cond.Toxic) || Has(Who.Defender, Vol.Curse) || Has(Who.Defender, MonFx.LeechSeed))
                return true;
            var weather = CurrentWeather;
            if (weather == AiWeather.Sandstorm)
            {
                if (ChargeTurnWithInvulnSandImmuneTypes.Contains(Type1(Who.Attacker))) return true;
                if (ChargeTurnWithInvulnSandImmuneTypes.Contains(Type2(Who.Attacker))) return true;
            }
            else if (weather == AiWeather.Hailing)
            {
                if (Type1(Who.Attacker) == PokemonType.Ice) return true;
                if (Type2(Who.Attacker) == PokemonType.Ice) return true;
            }
            if (SpeedCompare == SpeedOrder.Slower) return false;
            return EffectOf(PreviousMove(Who.Defender)) != NextAttackAlwaysHits;
        }
    }

    /// <summary><c>Expert_FakeOut</c>: +2.</summary>
    private void ExpertFakeOut() => Score(2);

    /// <summary><c>Expert_SpitUp</c>: mostly +2 with two or more stockpiled.</summary>
    private void ExpertSpitUp()
    {
        if (Stockpile(Who.Attacker) < 2) return;
        if (RandomBelow(80)) return;
        Score(2);
    }

    /// <summary>
    /// <c>Expert_Hail</c>: −1 under 40% HP; +1 to replace sun, rain or sand, with +2 more if the user knows Blizzard
    /// and +2 more for Ice Body (both only when replacing another weather).
    /// </summary>
    private void ExpertHail()
    {
        if (HpPercent(Who.Attacker) < 40)
        {
            Score(-1);
            return;
        }
        var weather = CurrentWeather;
        if (weather is not (AiWeather.Sunny or AiWeather.Raining or AiWeather.Sandstorm)) return;
        Score(1);
        if (Knows(Who.Attacker, "Blizzard") != false) Score(2);
        if (AbilityOf(Who.Attacker) != "Ice Body") return;
        Score(2);
    }
}
