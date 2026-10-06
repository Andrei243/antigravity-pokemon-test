using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using static PokemonPlatinumEngine.Battle.Sim.Ai.MoveEffectId;

namespace PokemonPlatinumEngine.Battle.Sim.Ai;

/// <summary>
/// The second half of the expert's routines, one for each move effect from Facade on (<c>Expert_Facade</c> to
/// <c>Expert_HealingWish</c> in <c>src/battle/trainer_ai/script.s</c>), each a translation of the original's: the
/// same questions in the same order, the same rolls, the same scores. <c>Expert_Main</c> sends each move here by its
/// effect. A table of hold effects, abilities, items or moves holds names; nothing (null) is in none of them, as the
/// original's 0 is in none of its tables.
/// </summary>
internal sealed partial class AiThinking
{
    /// <summary><c>Expert_Facade</c>: +1 when Facade would be stronger.</summary>
    private void ExpertFacade()
    {
        // The original asks whether the target has the condition, where it means the user
        if (HasStatus(Who.Defender, Cond.FacadeBoost)) Score(1);
    }

    /// <summary>
    /// <c>Expert_FocusPunch</c>: −1 against a resisting or immune target; +5 from behind a Substitute; +1 against a
    /// sleeping target, mostly against a confused or infatuated one, and now and then after the user's first turn.
    /// </summary>
    private void ExpertFocusPunch()
    {
        if (Effectiveness() is Eff.Immune or Eff.Quarter or Eff.Half)
        {
            Score(-1);
            return;
        }
        if (Has(Who.Attacker, Vol.Substitute))
        {
            Score(5);
            return;
        }
        if (HasStatus(Who.Defender, Cond.Sleep))
        {
            Score(1);
            return;
        }
        if (Has(Who.Defender, Vol.Attract) || Has(Who.Defender, Vol.Confusion))
        {
            if (RandomBelow(100)) return;
            Score(1);
            return;
        }
        if (FirstTurnIn(Who.Attacker)) return;
        if (RandomBelow(200)) return;
        Score(1);
    }

    /// <summary><c>Expert_SmellingSalts</c>: +1 against a paralysed target.</summary>
    private void ExpertSmellingSalts()
    {
        if (HasStatus(Who.Defender, Cond.Paralysis)) Score(1);
    }

    /// <summary><c>Expert_Trick_FlavorBerries</c>: the berries that confuse a Pokémon that dislikes their taste.</summary>
    private static readonly HashSet<string?> TrickFlavorBerries = new()
    {
        "HpRestoreSpicy", "HpRestoreDry", "HpRestoreSweet", "HpRestoreBitter", "HpRestoreSour"
    };

    /// <summary>
    /// <c>Expert_Trick_DisruptiveItems</c>: the items that hinder whoever holds them. The Macho Brace is missing, as
    /// in the original (which also lists the Power Belt's effect twice).
    /// </summary>
    private static readonly HashSet<string?> TrickDisruptiveItems = new()
    {
        "ChoiceAtk", "ChoiceSpatk", "ChoiceSpeed", "SpeedDownGrounded", "PriorityDown", "DmgUserContactXfr",
        "LvlupAtkEvUp", "LvlupDefEvUp", "LvlupSpatkEvUp", "LvlupSpdefEvUp", "LvlupSpeedEvUp", "LvlupHpEvUp"
    };

    /// <summary><c>Expert_Trick_BadOpponentItemsAndFlavorBerries</c>.</summary>
    private static readonly HashSet<string?> TrickBadOpponentItemsAndFlavorBerries = new()
    {
        "HpRestoreSpicy", "HpRestoreDry", "HpRestoreSweet", "HpRestoreBitter", "HpRestoreSour", "EvsUpSpeedDown",
        "ChoiceAtk", "ChoiceSpatk", "ChoiceSpeed", "SpeedDownGrounded", "PriorityDown", "DmgUserContactXfr",
        "LvlupAtkEvUp", "LvlupDefEvUp", "LvlupSpatkEvUp", "LvlupSpdefEvUp", "LvlupSpeedEvUp", "LvlupHpEvUp",
        "PsnUser", "BrnUser", "HpRestorePsnType"
    };

    /// <summary><c>Expert_Trick_BadOpponentItems</c>: what the user would rather not be handed back.</summary>
    private static readonly HashSet<string?> TrickBadOpponentItems = new()
    {
        "EvsUpSpeedDown", "ChoiceAtk", "ChoiceSpatk", "ChoiceSpeed", "SpeedDownGrounded", "PriorityDown",
        "DmgUserContactXfr", "LvlupAtkEvUp", "LvlupDefEvUp", "LvlupSpatkEvUp", "LvlupSpdefEvUp", "LvlupSpeedEvUp",
        "LvlupHpEvUp", "PsnUser", "BrnUser", "HpRestorePsnType"
    };

    /// <summary>
    /// <c>Expert_Trick</c>: +5 for handing over an item that hinders (a Choice item, an Iron Ball…), a Toxic Orb, a
    /// Flame Orb or Black Sludge to a target it would hurt, unless the user would be hurt by it in turn; −3 when the
    /// target's known item is no better, or the user holds nothing worth giving; mostly +2 for a flavour berry.
    /// </summary>
    private void ExpertTrick()
    {
        var mine = HoldEffect(Who.Attacker);
        if (TrickDisruptiveItems.Contains(mine))
        {
            if (TrickBadOpponentItems.Contains(HoldEffect(Who.Defender)))
            {
                Score(-3);
                return;
            }
            Score(5);
            return;
        }

        // Expert_Trick_PoisoningItems: a Toxic Orb
        if (mine == "PsnUser")
        {
            if (TrickBadOpponentItems.Contains(HoldEffect(Who.Defender)))
            {
                Score(-3);
                return;
            }
            if (HasStatus(Who.Defender, Cond.Any) || SideHas(Who.Defender, SideFx.Safeguard)
                || Type1(Who.Defender) is PokemonType.Steel or PokemonType.Poison
                || Type2(Who.Defender) is PokemonType.Steel or PokemonType.Poison
                || AbilityOf(Who.Defender) is "Immunity" or "Magic Guard" or "Poison Heal")
            {
                AttackerForPoison();
                return;
            }
            Score(5);
            return;
        }

        // Expert_Trick_BurningItems: a Flame Orb
        if (mine == "BrnUser")
        {
            if (TrickBadOpponentItems.Contains(HoldEffect(Who.Defender)))
            {
                Score(-3);
                return;
            }
            if (AbilityOf(Who.Defender) is "Water Veil" or "Magic Guard"
                || HasStatus(Who.Defender, Cond.Any) || SideHas(Who.Defender, SideFx.Safeguard)
                || Type1(Who.Defender) == PokemonType.Fire || Type2(Who.Defender) == PokemonType.Fire)
            {
                AttackerForBurn();
                return;
            }
            Score(5);
            return;
        }

        // Expert_Trick_BlackSludge
        if (mine == "HpRestorePsnType")
        {
            if (TrickBadOpponentItems.Contains(HoldEffect(Who.Defender)))
            {
                Score(-3);
                return;
            }
            if (Type1(Who.Defender) == PokemonType.Poison || Type2(Who.Defender) == PokemonType.Poison)
            {
                AttackerForSludge();
                return;
            }
            // The original checks the user as for a Toxic Orb here, not as for Black Sludge
            if (AbilityOf(Who.Defender) == "Magic Guard")
            {
                AttackerForPoison();
                return;
            }
            Score(5);
            return;
        }

        if (TrickFlavorBerries.Contains(mine))
        {
            if (TrickBadOpponentItemsAndFlavorBerries.Contains(HoldEffect(Who.Defender)))
            {
                Score(-3);
                return;
            }
            if (RandomBelow(50)) return;
            Score(2);
            return;
        }

        // Nothing worth giving (no item, or any other)
        Score(-3);

        // Expert_Trick_CheckAttackerForPoison: −3 if the user would be safe from the orb itself, else +5
        void AttackerForPoison()
        {
            if (HasStatus(Who.Attacker, Cond.Any) || SideHas(Who.Attacker, SideFx.Safeguard)
                || Type1(Who.Attacker) is PokemonType.Steel or PokemonType.Poison
                || Type2(Who.Attacker) is PokemonType.Steel or PokemonType.Poison
                || AbilityOf(Who.Attacker) is "Immunity" or "Magic Guard" or "Poison Heal" or "Klutz")
            {
                Score(-3);
                return;
            }
            Score(5);
        }

        // Expert_Trick_CheckAttackerForBurn
        void AttackerForBurn()
        {
            var ability = AbilityOf(Who.Attacker);
            if (ability is "Water Veil" or "Magic Guard")
            {
                Score(-3);
                return;
            }
            // The original sends Klutz to the shared −5, where every other case here is −3
            if (ability == "Klutz")
            {
                Score(-5);
                return;
            }
            if (HasStatus(Who.Attacker, Cond.Any) || SideHas(Who.Attacker, SideFx.Safeguard)
                || Type1(Who.Attacker) == PokemonType.Fire || Type2(Who.Attacker) == PokemonType.Fire)
            {
                Score(-3);
                return;
            }
            Score(5);
        }

        // Expert_Trick_CheckAttackerForSludge
        void AttackerForSludge()
        {
            if (Type1(Who.Attacker) == PokemonType.Poison || Type2(Who.Attacker) == PokemonType.Poison
                || AbilityOf(Who.Attacker) is "Magic Guard" or "Klutz")
            {
                Score(-3);
                return;
            }
            Score(5);
        }
    }

    /// <summary><c>Expert_ChangeUserAbility_DesirableAbilities</c>: abilities worth having.</summary>
    private static readonly HashSet<string?> ChangeUserAbilityDesirableAbilities = new()
    {
        "Speed Boost", "Battle Armor", "Sand Veil", "Static", "Flash Fire", "Wonder Guard", "Effect Spore",
        "Swift Swim", "Huge Power", "Rain Dish", "Cute Charm", "Shed Skin", "Marvel Scale", "Pure Power",
        "Chlorophyll", "Shield Dust", "Adaptability", "Magic Guard", "Mold Breaker", "Super Luck", "Unaware",
        "Tinted Lens", "Filter", "Solid Rock", "Reckless"
    };

    /// <summary>
    /// <c>Expert_ChangeUserAbility</c>: mostly +2 for taking a good ability from a target that has one, when the
    /// user's own isn't one; −1 otherwise.
    /// </summary>
    private void ExpertChangeUserAbility()
    {
        if (ChangeUserAbilityDesirableAbilities.Contains(AbilityOf(Who.Attacker)))
        {
            Score(-1);
            return;
        }
        if (ChangeUserAbilityDesirableAbilities.Contains(AbilityOf(Who.Defender)))
        {
            if (RandomBelow(50)) return;
            Score(2);
            return;
        }
        // The original falls through into its −1 when neither ability is worth having
        Score(-1);
    }

    /// <summary><c>Expert_Ingrain</c>: nothing.</summary>
    private void ExpertIngrain()
    {
    }

    /// <summary>
    /// <c>Expert_Superpower</c>: −1 against a resisting or immune target, with the user's Attack already lowered, or
    /// while the user is healthy enough to wait (above 40% when it moves first, at 60% or more when it moves last).
    /// </summary>
    private void ExpertSuperpower()
    {
        if (Effectiveness() is Eff.Immune or Eff.Quarter or Eff.Half)
        {
            Score(-1);
            return;
        }
        if (Stage(Who.Attacker, StatType.Attack) < 6)
        {
            Score(-1);
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
    /// <c>Expert_MagicCoat</c>: mostly −1 against a target at 30% or less; on the user's first turn +1 two times in
    /// five, after it −1 most of the time.
    /// </summary>
    private void ExpertMagicCoat()
    {
        if (HpPercent(Who.Defender) <= 30 && !RandomBelow(100)) Score(-1);

        if (FirstTurnIn(Who.Attacker))
        {
            if (RandomBelow(150)) return;
            Score(1);
            // (a roll of the original's after this point is never reached)
            return;
        }
        if (RandomBelow(30)) return;
        Score(-1);
    }

    /// <summary><c>Expert_Recycle_DesirableItems</c>.</summary>
    private static readonly HashSet<string?> RecycleDesirableItems = new() { "Chesto Berry", "Lum Berry", "Starf Berry" };

    /// <summary><c>Expert_Recycle</c>: −2 unless the item it would bring back is a Chesto, Lum or Starf Berry; then mostly +1.</summary>
    private void ExpertRecycle()
    {
        if (!RecycleDesirableItems.Contains(RecycleItem(Who.Attacker)?.Name))
        {
            Score(-2);
            return;
        }
        if (RandomBelow(50)) return;
        Score(1);
    }

    /// <summary>
    /// <c>Expert_Revenge</c>: −2 against a target that may not attack (asleep, infatuated, confused); otherwise −2
    /// most of the time and +2 the rest.
    /// </summary>
    private void ExpertRevenge()
    {
        if (HasStatus(Who.Defender, Cond.Sleep) || Has(Who.Defender, Vol.Attract) || Has(Who.Defender, Vol.Confusion)
            || RandomBelow(180))
        {
            Score(-2);
            return;
        }
        Score(2);
    }

    /// <summary><c>Expert_BrickBreak</c>: +1 against a side behind Reflect or Light Screen.</summary>
    private void ExpertBrickBreak()
    {
        if (SideHas(Who.Defender, SideFx.Reflect) || SideHas(Who.Defender, SideFx.LightScreen)) Score(1);
    }

    /// <summary><c>Expert_KnockOff</c>: now and then +1 against a target at 30% or more, after the user's first turn.</summary>
    private void ExpertKnockOff()
    {
        if (HpPercent(Who.Defender) < 30) return;
        if (FirstTurnIn(Who.Attacker)) return;
        if (RandomBelow(180)) return;
        Score(1);
    }

    /// <summary>
    /// <c>Expert_Endeavor</c>: −1 against a target under 70%; otherwise +1 if the user is low (40% or less when it
    /// moves first, 50% or less when it moves last), −1 if not.
    /// </summary>
    private void ExpertEndeavor()
    {
        if (HpPercent(Who.Defender) < 70)
        {
            Score(-1);
            return;
        }
        if (SpeedCompare == SpeedOrder.Slower)
        {
            if (HpPercent(Who.Attacker) > 50)
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

    /// <summary>
    /// <c>Expert_WaterSpout</c>: −1 against a resisting or immune target, or when HP has run down (50% or less
    /// moving first, 70% or less moving last).
    /// </summary>
    private void ExpertWaterSpout()
    {
        if (Effectiveness() is Eff.Immune or Eff.Quarter or Eff.Half)
        {
            Score(-1);
            return;
        }
        // The original weighs the target's HP, where the move's power follows the user's
        if (SpeedCompare == SpeedOrder.Slower)
        {
            if (HpPercent(Who.Defender) > 70) return;
            Score(-1);
            return;
        }
        if (HpPercent(Who.Defender) > 50) return;
        Score(-1);
    }

    /// <summary><c>Expert_Imprison</c>: after the user's first turn, +2 three times in five.</summary>
    private void ExpertImprison()
    {
        if (FirstTurnIn(Who.Attacker)) return;
        if (RandomBelow(100)) return;
        Score(2);
    }

    /// <summary><c>Expert_Refresh</c>: −1 when the target (not the user, as the original asks) is under 50%.</summary>
    private void ExpertRefresh()
    {
        if (HpPercent(Who.Defender) < 50) Score(-1);
    }

    /// <summary>
    /// <c>Expert_Snatch</c>: on the user's first turn, +2 two times in five; otherwise, unless a roll lets it be,
    /// mostly −2, with a chance of +2 or +1 for a slower user against a weakened target that may heal or curl up.
    /// </summary>
    private void ExpertSnatch()
    {
        if (FirstTurnIn(Who.Attacker))
        {
            TryPlus2();
            return;
        }
        if (RandomBelow(30)) return;
        if (SpeedCompare == SpeedOrder.Slower)
        {
            if (HpPercent(Who.Defender) > 25)
            {
                TryMinus2();
                return;
            }
            if (KnowsEffect(Who.Defender, RestoreHalfHp) == true || KnowsEffect(Who.Defender, DefUpDoubleRolloutPower) == true)
            {
                TryPlus2();
                return;
            }
            if (RandomBelow(230))
            {
                TryMinus2();
                return;
            }
            Score(1);
            return;
        }
        if (HpPercent(Who.Attacker) != 100 || HpPercent(Who.Defender) < 70)
        {
            TryMinus2();
            return;
        }
        if (RandomBelow(60)) return;
        TryMinus2();

        void TryPlus2()
        {
            if (RandomBelow(150)) return;
            Score(2);
        }

        void TryMinus2()
        {
            if (RandomBelow(30)) return;
            Score(-2);
        }
    }

    /// <summary><c>Expert_MudSport</c>: +1 against an Electric target while the user is at 50% or more, −1 otherwise.</summary>
    private void ExpertMudSport()
    {
        if (HpPercent(Who.Attacker) < 50)
        {
            Score(-1);
            return;
        }
        if (Type1(Who.Defender) == PokemonType.Electric || Type2(Who.Defender) == PokemonType.Electric)
        {
            Score(1);
            return;
        }
        Score(-1);
    }

    /// <summary>
    /// <c>Expert_Overheat</c>: −1 against a resisting or immune target, or once the user is down to 60% (moving
    /// first) or 80% (moving last).
    /// </summary>
    private void ExpertOverheat()
    {
        if (Effectiveness() is Eff.Immune or Eff.Quarter or Eff.Half)
        {
            Score(-1);
            return;
        }
        if (SpeedCompare == SpeedOrder.Slower)
        {
            if (HpPercent(Who.Attacker) > 80) return;
            Score(-1);
            return;
        }
        if (HpPercent(Who.Attacker) > 60) return;
        Score(-1);
    }

    /// <summary><c>Expert_WaterSport</c>: +1 against a Fire target while the user is at 50% or more, −1 otherwise.</summary>
    private void ExpertWaterSport()
    {
        if (HpPercent(Who.Attacker) < 50)
        {
            Score(-1);
            return;
        }
        if (Type1(Who.Defender) == PokemonType.Fire || Type2(Who.Defender) == PokemonType.Fire)
        {
            Score(1);
            return;
        }
        Score(-1);
    }

    /// <summary><c>Expert_DragonDance</c>: +1 half the time for a slower user; otherwise mostly −1 once it is at 50% or less.</summary>
    private void ExpertDragonDance()
    {
        if (SpeedCompare == SpeedOrder.Slower)
        {
            if (RandomBelow(128)) return;
            Score(1);
            return;
        }
        if (HpPercent(Who.Attacker) > 50) return;
        if (RandomBelow(70)) return;
        Score(-1);
    }

    /// <summary>
    /// <c>Expert_Gravity</c>: mostly +1 against a target that floats (Levitate, Magnet Rise, a Flying type);
    /// otherwise, with the user at 60% or more, +1 three times in eight.
    /// </summary>
    private void ExpertGravity()
    {
        bool floats = AbilityOf(Who.Defender) == "Levitate" || Has(Who.Defender, MonFx.MagnetRise)
            || Type1(Who.Defender) == PokemonType.Flying || Type2(Who.Defender) == PokemonType.Flying;
        if (!floats)
        {
            if (HpPercent(Who.Attacker) < 60) return;
            if (!RandomBelow(128)) return;
        }
        if (RandomBelow(64)) return;
        Score(1);
    }

    /// <summary>
    /// <c>Expert_MiracleEye</c>: a chance of +2 against a Dark target or one whose evasion is up three stages or
    /// more (the Dark one rolls twice); −2 otherwise.
    /// </summary>
    private void ExpertMiracleEye()
    {
        if (Type1(Who.Defender) == PokemonType.Dark || Type2(Who.Defender) == PokemonType.Dark)
        {
            if (RandomBelow(80)) return;
        }
        else if (!(Stage(Who.Defender, StatType.Evasion) > 8))
        {
            Score(-2);
            return;
        }
        if (RandomBelow(80)) return;
        Score(2);
    }

    /// <summary><c>Expert_WakeUpSlap</c>: −1 against a resisting or immune target; +1 against a sleeping one.</summary>
    private void ExpertWakeUpSlap()
    {
        if (Effectiveness() is Eff.Immune or Eff.Half or Eff.Quarter)
        {
            Score(-1);
            return;
        }
        if (HasStatus(Who.Defender, Cond.Sleep)) Score(1);
    }

    /// <summary><c>Expert_HammerArm</c>: −1 against a resisting or immune target; +1 for a user that moves last anyway.</summary>
    private void ExpertHammerArm()
    {
        if (Effectiveness() is Eff.Immune or Eff.Half or Eff.Quarter)
        {
            Score(-1);
            return;
        }
        if (SpeedCompare == SpeedOrder.Slower) Score(1);
    }

    /// <summary><c>Expert_GyroBall</c>: nothing.</summary>
    private void ExpertGyroBall()
    {
    }

    /// <summary><c>Expert_Brine</c>: −1 against a resisting or immune target; +1, half the time +2, against one at 50% or less.</summary>
    private void ExpertBrine()
    {
        if (Effectiveness() is Eff.Immune or Eff.Half or Eff.Quarter)
        {
            Score(-1);
            return;
        }
        if (HpPercent(Who.Defender) > 50) return;
        Score(1);
        if (RandomBelow(128)) return;
        Score(1);
    }

    /// <summary><c>Expert_Feint_GradualRecoveryItems</c>: Leftovers and Black Sludge.</summary>
    private static readonly HashSet<string?> FeintGradualRecoveryItems = new() { "HpRestoreGradual", "HpRestorePsnType" };

    /// <summary>
    /// <c>Expert_Feint</c>: against a target that isn't known to protect itself, three times in four nothing. Else
    /// a chance of +1 when time is against the user (badly poisoned, cursed, doomed, infatuated, seeded, drowsy) or the
    /// wounded target heals each turn by its item; then by how often the target has protected itself in a row, a
    /// chance of +1 or −2.
    /// </summary>
    private void ExpertFeint()
    {
        if (KnowsEffect(Who.Defender, MoveEffectId.Protect) != true && !RandomBelow(64)) return;

        if (HasStatus(Who.Attacker, Cond.Toxic) || Has(Who.Attacker, Vol.Curse) || Has(Who.Attacker, MonFx.PerishSong)
            || Has(Who.Attacker, Vol.Attract) || Has(Who.Attacker, MonFx.LeechSeed) || Has(Who.Attacker, MonFx.Yawn)
            || (HpPercent(Who.Defender) != 100 && FeintGradualRecoveryItems.Contains(HoldEffect(Who.Defender))))
        {
            if (!RandomBelow(128)) Score(1);
        }

        int chain = ProtectChain(Who.Defender);
        if (chain == 1)
        {
            if (RandomBelow(192)) return;
            Score(1);
            return;
        }
        if (chain > 2)
        {
            Score(-2);
            return;
        }
        // None in a row, and (the original's slip) exactly two
        if (RandomBelow(128)) return;
        Score(1);
    }

    /// <summary>
    /// <c>Expert_Pluck</c>: −1 against a resisting or immune target; otherwise +1 half the time, and on the user's
    /// first turn mostly another +1.
    /// </summary>
    private void ExpertPluck()
    {
        if (Effectiveness() is Eff.Immune or Eff.Half or Eff.Quarter)
        {
            Score(-1);
            return;
        }
        if (FirstTurnIn(Who.Attacker) && !RandomBelow(64)) Score(1);
        if (RandomBelow(128)) return;
        Score(1);
    }

    /// <summary>
    /// <c>Expert_Tailwind</c>: unless a roll lets it be, −1 for a user that moves first already or is at 30% or
    /// less; +1 above 75%, mostly +1 between.
    /// </summary>
    private void ExpertTailwind()
    {
        if (RandomBelow(64)) return;
        if (SpeedCompare == SpeedOrder.Faster)
        {
            Score(-1);
            return;
        }
        if (HpPercent(Who.Attacker) < 31)
        {
            Score(-1);
            return;
        }
        if (!(HpPercent(Who.Attacker) > 75) && RandomBelow(64)) return;
        Score(1);
    }

    /// <summary><c>Expert_Acupressure</c>: −1 at 50% or less; above 90% mostly +1, between +1 three times in eight.</summary>
    private void ExpertAcupressure()
    {
        if (HpPercent(Who.Attacker) < 51)
        {
            Score(-1);
            return;
        }
        if (!(HpPercent(Who.Attacker) > 90) && RandomBelow(128)) return;
        if (RandomBelow(64)) return;
        Score(1);
    }

    /// <summary>
    /// <c>Expert_MetalBurst</c>: −1 against a target that may not attack or is known to strike back itself
    /// (Avalanche and Revenge, Focus Punch, Vital Throw); otherwise chances of −1 at 30% or less and at 50% or less, a
    /// chance of +1, and two more chances of +1 when the target is taunted into attacking.
    /// </summary>
    private void ExpertMetalBurst()
    {
        if (HasStatus(Who.Defender, Cond.Sleep) || Has(Who.Defender, Vol.Attract) || Has(Who.Defender, Vol.Confusion)
            || KnowsEffect(Who.Defender, DoublePowerIfHit) == true || KnowsEffect(Who.Defender, HitLastWhiffIfHit) == true
            || KnowsEffect(Who.Defender, PriorityNeg1BypassAccuracy) == true)
        {
            Score(-1);
            return;
        }
        if (!(HpPercent(Who.Attacker) > 30) && !RandomBelow(10)) Score(-1);
        if (!(HpPercent(Who.Attacker) > 50) && !RandomBelow(100)) Score(-1);
        // Whatever the HP, though the original's label speaks of high HP
        if (!RandomBelow(192)) Score(1);

        // The target's last move was one that hits, and it is taunted
        if (PowerOf(PreviousMove(Who.Defender)) != 0 && Taunted(Who.Defender) && !RandomBelow(100)) Score(1);

        if (!Taunted(Who.Defender)) return;
        if (RandomBelow(100)) return;
        Score(1);
    }

    /// <summary>
    /// <c>Expert_UTurn</c>: −1 against a resisting or immune target; nothing more with nobody to switch to. Else a
    /// likely −2 if the user has a super-effective move, a likely −2 that ends it if nobody on the bench hits harder,
    /// then chances of +1 by the target's HP and +1 for moving first (half the time when not).
    /// </summary>
    private void ExpertUTurn()
    {
        if (Effectiveness() is Eff.Immune or Eff.Quarter or Eff.Half)
        {
            Score(-1);
            return;
        }
        if (AliveBench(Who.Attacker) == 0) return;
        if (HasSuperEffectiveMove() && !RandomBelow(64)) Score(-2);

        if (!BenchDealsMoreDamage(roll: false) && !RandomBelow(64))
        {
            Score(-2);
            return;
        }

        bool coinForPlus1;
        if (HpPercent(Who.Defender) > 70)
        {
            if (!RandomBelow(64)) Score(1);
            coinForPlus1 = true;
        }
        else if (HpPercent(Who.Defender) > 30) coinForPlus1 = true;
        else coinForPlus1 = !RandomBelow(128);
        if (coinForPlus1 && !RandomBelow(128)) Score(1);

        if (SpeedCompare == SpeedOrder.Faster || !RandomBelow(128)) Score(1);
    }

    /// <summary>
    /// <c>Expert_CloseCombat</c>: −1 against a resisting or immune target, or once the user is down to 60% (moving
    /// first) or 80% (moving last).
    /// </summary>
    private void ExpertCloseCombat()
    {
        if (Effectiveness() is Eff.Immune or Eff.Quarter or Eff.Half)
        {
            Score(-1);
            return;
        }
        if (SpeedCompare == SpeedOrder.Slower)
        {
            if (HpPercent(Who.Attacker) > 80) return;
            Score(-1);
            return;
        }
        if (HpPercent(Who.Attacker) > 60) return;
        Score(-1);
    }

    /// <summary><c>Expert_Payback</c>: −1 against a resisting or immune target; mostly +1 for a user that moves last at 30% or more.</summary>
    private void ExpertPayback()
    {
        if (Effectiveness() is Eff.Immune or Eff.Half or Eff.Quarter)
        {
            Score(-1);
            return;
        }
        if (SpeedCompare == SpeedOrder.Faster) return;
        if (HpPercent(Who.Attacker) < 30) return;
        if (RandomBelow(64)) return;
        Score(1);
    }

    /// <summary>
    /// <c>Expert_Assurance</c>: −1 against a resisting or immune target; for a user that moves last, +1 half the
    /// time with Rough Skin, a quarter of the time otherwise.
    /// </summary>
    private void ExpertAssurance()
    {
        if (Effectiveness() is Eff.Immune or Eff.Half or Eff.Quarter)
        {
            Score(-1);
            return;
        }
        if (SpeedCompare == SpeedOrder.Faster) return;
        if (AbilityOf(Who.Attacker) != "Rough Skin")
        {
            // Expert_Assurance_RecoilBerries: the original looks the user's hold effect up in a table of items (the
            // Jaboca and Rowap Berries), whose numbers no hold effect has, so it never matches
            if (!RandomBelow(128)) return;
        }
        if (RandomBelow(128)) return;
        Score(1);
    }

    /// <summary><c>Expert_Embargo</c>: +1 half the time.</summary>
    private void ExpertEmbargo()
    {
        if (RandomBelow(128)) return;
        Score(1);
    }

    /// <summary><c>Expert_Fling_DesirableFlingEffects</c>: items whose throw does something besides damage.</summary>
    private static readonly HashSet<string?> FlingDesirableFlingEffects = new()
    {
        "SometimesFlinch", "StrengthenPoison", "PsnUser", "BrnUser", "PikaSpatkUp"
    };

    /// <summary>
    /// <c>Expert_Fling</c>: against a resisting or immune target −1 unless the item has an effect of its own; else by
    /// the throw's power: −2 under 30, half the time −1 up to 60, mostly +1 above 60, and above 90 more (+4 more
    /// against a weak target, otherwise half the time +1 more).
    /// </summary>
    private void ExpertFling()
    {
        if (Effectiveness() is Eff.Immune or Eff.Half or Eff.Quarter)
        {
            if (FlingDesirableFlingEffects.Contains(HoldEffect(Who.Attacker))) return;
            Score(-1);
            return;
        }

        int power = FlingPower(Who.Attacker);
        if (power < 30)
        {
            Score(-2);
            return;
        }
        if (power > 90)
        {
            if (Effectiveness() is Eff.Double or Eff.Quadruple) Score(4);
            else if (!RandomBelow(128)) Score(1);
        }
        else if (!(power > 60))
        {
            if (RandomBelow(128)) return;
            Score(-1);
            return;
        }
        if (RandomBelow(64)) return;
        Score(1);
    }

    /// <summary><c>Expert_PsychoShift</c>: −10 with no condition to pass on; otherwise +1 half the time against a target at 30% or more.</summary>
    private void ExpertPsychoShift()
    {
        if (!HasStatus(Who.Attacker, Cond.Any))
        {
            Score(-10);
            return;
        }
        if (RandomBelow(128)) return;
        if (HpPercent(Who.Defender) < 30) return;
        Score(1);
    }

    /// <summary>
    /// <c>Expert_TrumpCard</c>: −1 against a resisting or immune target; with few PP left (and the move stronger for
    /// it) +3, +1 or +2, or a likely +1; otherwise a likely +1 against Pressure, and +1 or +2 when its other moves
    /// would likely miss (the target's evasion or the user's accuracy far from even).
    /// </summary>
    private void ExpertTrumpCard()
    {
        if (Effectiveness() is Eff.Immune or Eff.Half or Eff.Quarter)
        {
            Score(-1);
            return;
        }

        int pp = CurrentPp;
        if (pp == 1)
        {
            Score(3);
            return;
        }
        if (pp == 2)
        {
            Score(1);
            TryPlus1();
            return;
        }
        if (pp == 3)
        {
            TryPlus1();
            return;
        }

        if (AbilityOf(Who.Defender) == "Pressure" && !RandomBelow(30)) Score(1);

        if (Stage(Who.Defender, StatType.Evasion) > 10 || Stage(Who.Attacker, StatType.Accuracy) < 2)
        {
            Score(1);
            TryPlus1();
            return;
        }
        if (Stage(Who.Defender, StatType.Evasion) > 8 || Stage(Who.Attacker, StatType.Accuracy) < 4) TryPlus1();

        void TryPlus1()
        {
            if (RandomBelow(100)) return;
            Score(1);
        }
    }

    /// <summary>What a target may heal itself with, in the order <c>Expert_HealBlock</c> asks.</summary>
    private static readonly MoveEffectId[] HealBlockHealingEffects =
    {
        RecoverDamageSleep, RestoreHalfHp, HealHalfRemoveFlyingType, Unused157, HealHalfMoreInSun, Rest, Swallow,
        RecoverHalfDamageDealt, GroundTrapUserContinuousHeal, RestoreHpEveryTurn, StatusLeechSeed,
        FaintAndFullHealNextMon, FaintFullRestoreNextMon
    };

    /// <summary>
    /// <c>Expert_HealBlock</c>: +1 most of the time against a target known to heal itself (by a move, Ingrain or Aqua
    /// Ring) or while the user is seeded; otherwise +1 a little over half the time.
    /// </summary>
    private void ExpertHealBlock()
    {
        if (HealBlockHealingEffects.Any(e => KnowsEffect(Who.Defender, e) == true)
            || Has(Who.Attacker, MonFx.LeechSeed) || Has(Who.Defender, MonFx.AquaRing) || Has(Who.Defender, MonFx.Ingrain)
            || RandomBelow(96))
        {
            if (RandomBelow(25)) return;
            Score(1);
        }
    }

    /// <summary>
    /// <c>Expert_WringOut</c>: −1 against a resisting or immune target or one under 50%; against an unhurt target
    /// +2 (+1 moving last) and mostly +1 more; above 85% mostly +1.
    /// </summary>
    private void ExpertWringOut()
    {
        if (Effectiveness() is Eff.Immune or Eff.Half or Eff.Quarter)
        {
            Score(-1);
            return;
        }
        if (HpPercent(Who.Defender) < 50)
        {
            Score(-1);
            return;
        }
        if (HpPercent(Who.Defender) == 100)
        {
            if (SpeedCompare != SpeedOrder.Slower) Score(1);
            Score(1);
        }
        else if (!(HpPercent(Who.Defender) > 85)) return;
        if (RandomBelow(25)) return;
        Score(1);
    }

    /// <summary><c>Expert_PowerTrick</c>: a chance of +1, the better the user's HP; −2 at 30% or less.</summary>
    private void ExpertPowerTrick()
    {
        if (HpPercent(Who.Attacker) > 90)
        {
            if (RandomBelow(96)) return;
            Score(1);
            return;
        }
        if (HpPercent(Who.Attacker) > 60)
        {
            if (RandomBelow(128)) return;
            Score(1);
            return;
        }
        if (HpPercent(Who.Attacker) > 30)
        {
            if (RandomBelow(164)) return;
            Score(1);
            return;
        }
        Score(-2);
    }

    /// <summary>
    /// <c>Expert_GastroAcid</c>: unless a roll lets it be, +1, then less the lower the target's HP: half the time −1
    /// at 70% or less, −1 more at 50% or less and again at 30% or less.
    /// </summary>
    private void ExpertGastroAcid()
    {
        if (RandomBelow(64)) return;
        Score(1);
        if (HpPercent(Who.Defender) > 70) return;
        if (!RandomBelow(128)) Score(-1);
        if (HpPercent(Who.Defender) > 50) return;
        Score(-1);
        if (HpPercent(Who.Defender) > 30) return;
        Score(-1);
    }

    /// <summary>
    /// <c>Expert_LuckyChant</c>: −1 with the user under 70%; +1 against a target known to aim for critical hits, a
    /// quarter of the time otherwise.
    /// </summary>
    private void ExpertLuckyChant()
    {
        if (HpPercent(Who.Attacker) < 70)
        {
            Score(-1);
            return;
        }
        if (KnowsEffect(Who.Defender, HighCritical) == true || KnowsEffect(Who.Defender, HighCriticalBurnHit) == true
            || KnowsEffect(Who.Defender, HighCriticalPoisonHit) == true || RandomBelow(64))
        {
            Score(1);
        }
    }

    /// <summary>
    /// <c>Expert_MeFirst</c>: −2 for a user that moves last; otherwise likely +1 if the target's last move would hit
    /// harder than the user's best, half the time +1 if that move hit at all, and mostly +1.
    /// </summary>
    private void ExpertMeFirst()
    {
        if (SpeedCompare == SpeedOrder.Slower)
        {
            Score(-2);
            return;
        }
        if (DealsMoreDamage(Who.Defender, roll: false) && !RandomBelow(32)) Score(1);
        if (DefenderLastMoveCategory != MoveCategory.Status)
        {
            if (RandomBelow(128)) return;
            Score(1);
        }
        if (RandomBelow(64)) return;
        Score(1);
    }

    /// <summary><c>Expert_Copycat_EncouragedMoves</c>: moves worth copying.</summary>
    private static readonly HashSet<string?> CopycatEncouragedMoves = new()
    {
        "Sleep Powder", "Lovely Kiss", "Spore", "Hypnosis", "Sing", "Grass Whistle", "Shadow Punch", "Sand Attack",
        "Smokescreen", "Toxic", "Guillotine", "Horn Drill", "Fissure", "Sheer Cold", "Cross Chop", "Aeroblast",
        "Confuse Ray", "Sweet Kiss", "Screech", "Cotton Spore", "Scary Face", "Fake Tears", "Metal Sound",
        "Thunder Wave", "Glare", "Poison Powder", "Shadow Ball", "Dynamic Punch", "Hyper Beam", "Extreme Speed",
        "Thief", "Covet", "Attract", "Swagger", "Torment", "Flatter", "Trick", "Superpower", "Skill Swap",
        "Psycho Shift", "Power Swap", "Guard Swap", "Sucker Punch", "Heart Swap", "Switcheroo", "Captivate",
        "Dark Void"
    };

    /// <summary>
    /// <c>Expert_Copycat</c>: for a user that moves first, likely +2 if the target's last move would hit harder than
    /// its best, half the time +2 if that move is worth copying; otherwise, when it neither hits harder nor is worth
    /// copying, −1 two times in three.
    /// </summary>
    private void ExpertCopycat()
    {
        if (SpeedCompare != SpeedOrder.Slower)
        {
            if (DealsMoreDamage(Who.Defender, roll: false))
            {
                if (RandomBelow(32)) return;
                Score(2);
                return;
            }
            if (CopycatEncouragedMoves.Contains(PreviousMove(Who.Defender)?.Name))
            {
                if (RandomBelow(128)) return;
                Score(2);
                return;
            }
        }
        // Asked again, as the original does: a damage that rolls (Psywave, Magnitude) may come out otherwise
        if (DealsMoreDamage(Who.Defender, roll: false)) return;
        if (CopycatEncouragedMoves.Contains(PreviousMove(Who.Defender)?.Name)) return;
        if (RandomBelow(80)) return;
        Score(-1);
    }

    /// <summary>
    /// <c>Expert_PowerSwap</c>: the more the target's Attack and Sp. Atk stages are above the user's, the higher
    /// the bonus it tries for (see <see cref="ScoreStageSwap"/>).
    /// </summary>
    private void ExpertPowerSwap() => ScoreStageSwap(StatType.Attack, StatType.SpAttack);

    /// <summary><c>Expert_GuardSwap</c>: as Power Swap, by Defense and Sp. Def.</summary>
    private void ExpertGuardSwap() => ScoreStageSwap(StatType.Defense, StatType.SpDefense);

    /// <summary>
    /// The body of <c>Expert_PowerSwap</c> and <c>Expert_GuardSwap</c>: how far the target's stages of two stats
    /// are above the user's picks the best bonus (+1 to +5), and each roll that fails tries the next lower one. A gap
    /// of exactly one in the second stat counts for nothing unless the first is level, as in the original.
    /// </summary>
    private void ScoreStageSwap(StatType first, StatType second)
    {
        int top;
        int diff = StageDiff(Who.Defender, first);
        if (diff > 3)
        {
            int other = StageDiff(Who.Defender, second);
            top = other > 3 ? 5 : other > 1 ? 4 : other == 0 ? 3 : 0;
        }
        else if (diff > 1)
        {
            int other = StageDiff(Who.Defender, second);
            top = other > 3 ? 4 : other > 1 ? 3 : other == 0 ? 2 : 0;
        }
        else if (diff > 0)
        {
            int other = StageDiff(Who.Defender, second);
            top = other > 3 ? 3 : other > 1 ? 2 : other == 0 ? 1 : 0;
        }
        else if (diff == 0)
        {
            int other = StageDiff(Who.Defender, second);
            top = other > 3 ? 3 : other > 1 ? 2 : other > 0 ? 1 : 0;
        }
        else return;

        for (int bonus = top; bonus >= 1; bonus--)
        {
            if (RandomBelow(128)) continue;
            Score(bonus);
            return;
        }
    }

    /// <summary>
    /// <c>Expert_Punishment</c>: nothing against a resisting or immune target; otherwise, the more stages the target
    /// has raised, the more chances of a bonus.
    /// </summary>
    private void ExpertPunishment()
    {
        if (Effectiveness() is Eff.Immune or Eff.Half or Eff.Quarter) return;
        int raised = PositiveStages(Who.Defender);
        int top = raised > 6 ? 4 : raised > 5 ? 3 : raised > 4 ? 2 : raised > 2 ? 1 : 0;
        // Unlike the swaps' ladder, each step falls into the next: every step rolls, and each won adds its own bonus
        for (int bonus = top; bonus >= 1; bonus--)
            if (!RandomBelow(128)) Score(bonus);
    }

    /// <summary><c>Expert_LastResort</c>: −1 against a resisting or immune target; +1 once it can be used.</summary>
    private void ExpertLastResort()
    {
        if (Effectiveness() is Eff.Immune or Eff.Half or Eff.Quarter)
        {
            Score(-1);
            return;
        }
        if (CanUseLastResort(Who.Attacker)) Score(1);
    }

    /// <summary>
    /// <c>Expert_WorrySeed</c>: +1 against a target that may know Rest, half the time +1 with the user at 50% or
    /// more, and mostly +1.
    /// </summary>
    private void ExpertWorrySeed()
    {
        if (Knows(Who.Defender, "Rest") != false) Score(1);
        if (HpPercent(Who.Attacker) >= 50 && !RandomBelow(128)) Score(1);
        if (RandomBelow(64)) return;
        Score(1);
    }

    /// <summary><c>Expert_SuckerPunch</c>: −1 against a resisting or immune target; otherwise mostly +1.</summary>
    private void ExpertSuckerPunch()
    {
        if (Effectiveness() is Eff.Immune or Eff.Half or Eff.Quarter)
        {
            Score(-1);
            return;
        }
        if (RandomBelow(64)) return;
        Score(1);
    }

    /// <summary><c>Expert_ToxicSpikes</c>: half the time +1, and then mostly +1 more if the user knows Roar or Whirlwind.</summary>
    private void ExpertToxicSpikes()
    {
        if (RandomBelow(128)) return;
        Score(1);
        if (Knows(Who.Attacker, "Roar") == true || Knows(Who.Attacker, "Whirlwind") == true)
        {
            if (RandomBelow(64)) return;
            Score(1);
        }
    }

    /// <summary>
    /// <c>Expert_HeartSwap</c>: −2 unless the target has a stat up two stages or more (Attack, Defense, Sp. Atk,
    /// Sp. Def, evasion) or is pumped up; then +1 if one of the user's four stats isn't up, +2 if its evasion isn't,
    /// +1 if it isn't pumped up, and −2 most of the time if it has nothing to gain.
    /// </summary>
    private void ExpertHeartSwap()
    {
        if (!(Stage(Who.Defender, StatType.Attack) > 7 || Stage(Who.Defender, StatType.Defense) > 7
            || Stage(Who.Defender, StatType.SpAttack) > 7 || Stage(Who.Defender, StatType.SpDefense) > 7
            || Stage(Who.Defender, StatType.Evasion) > 7 || Has(Who.Defender, Vol.FocusEnergy)))
        {
            Score(-2);
            return;
        }
        if (Stage(Who.Attacker, StatType.Attack) < 7 || Stage(Who.Attacker, StatType.Defense) < 7
            || Stage(Who.Attacker, StatType.SpAttack) < 7 || Stage(Who.Attacker, StatType.SpDefense) < 7)
        {
            Score(1);
            return;
        }
        if (Stage(Who.Attacker, StatType.Evasion) < 7)
        {
            Score(2);
            return;
        }
        if (!Has(Who.Attacker, Vol.FocusEnergy))
        {
            Score(1);
            return;
        }
        if (RandomBelow(50)) return;
        Score(-2);
    }

    /// <summary><c>Expert_AquaRing</c>: +1 half the time with the user at 30% or more.</summary>
    private void ExpertAquaRing()
    {
        if (HpPercent(Who.Attacker) < 30) return;
        if (RandomBelow(128)) return;
        Score(1);
    }

    /// <summary>
    /// <c>Expert_MagnetRise</c>: with the user at 50% or more, +1 against a target known to use Earthquake, Earth
    /// Power or Fissure, then +1 against a Ground target, half the time against any other.
    /// </summary>
    private void ExpertMagnetRise()
    {
        if (HpPercent(Who.Attacker) < 50) return;
        if (Knows(Who.Defender, "Earthquake") == true || Knows(Who.Defender, "Earth Power") == true
            || Knows(Who.Defender, "Fissure") == true)
        {
            Score(1);
        }
        if (Type1(Who.Defender) != PokemonType.Ground && Type2(Who.Defender) != PokemonType.Ground && RandomBelow(128)) return;
        Score(1);
    }

    /// <summary>
    /// <c>Expert_Defog</c>: against the target's screens +1 (and half the time −1 if that would also clear the
    /// hazards laid on a side with someone left to send in), unless the user is at 30% or less with nobody behind it;
    /// −2 for clearing those hazards alone; then likely −2 unless the user is at 70% or more and the target's evasion
    /// is down two stages at most; and −2 against a target at 70% or less.
    /// </summary>
    private void ExpertDefog()
    {
        bool tryMinus2;
        if (SideHas(Who.Defender, SideFx.LightScreen) || SideHas(Who.Defender, SideFx.Reflect))
        {
            if (HpPercent(Who.Attacker) <= 30 && AliveBench(Who.Attacker) == 0) tryMinus2 = true;
            else
            {
                Score(1);
                if (AliveBench(Who.Defender) == 0) return;
                if (Hazards() && !RandomBelow(128)) Score(-1);
                tryMinus2 = UserLowOrEvasionDown();
            }
        }
        else
        {
            if (Hazards()) Score(-2);
            tryMinus2 = UserLowOrEvasionDown();
        }

        if (tryMinus2 && !RandomBelow(50)) Score(-2);
        if (HpPercent(Who.Defender) > 70) return;
        Score(-2);

        bool Hazards() => SideHas(Who.Defender, SideFx.Spikes) || SideHas(Who.Defender, SideFx.StealthRock)
            || SideHas(Who.Defender, SideFx.ToxicSpikes);

        bool UserLowOrEvasionDown() => HpPercent(Who.Attacker) < 70 || !(Stage(Who.Defender, StatType.Evasion) > 3);
    }

    /// <summary>
    /// <c>Expert_TrickRoom</c>: in a single battle, unless the user is low with nobody behind it, mostly +3 for a
    /// user that moves last and −1 for one that doesn't.
    /// </summary>
    private void ExpertTrickRoom()
    {
        if (IsDouble) return;
        if (HpPercent(Who.Attacker) <= 30 && AliveBench(Who.Attacker) == 0) return;
        if (SpeedCompare == SpeedOrder.Slower)
        {
            if (RandomBelow(64)) return;
            Score(3);
            return;
        }
        Score(-1);
    }

    /// <summary><c>Expert_Blizzard</c>: mostly −3 against a resisting or immune target; +1 in a hailstorm.</summary>
    private void ExpertBlizzard()
    {
        if (Effectiveness() is Eff.Immune or Eff.Half or Eff.Quarter)
        {
            if (RandomBelow(50)) return;
            Score(-3);
            return;
        }
        if (CurrentWeather != AiWeather.Hailing) return;
        Score(1);
    }

    /// <summary>
    /// <c>Expert_Captivate</c>: against a target whose Sp. Atk has moved, −1, −1 more with the user at 90% or less and
    /// mostly −2 more once it is down three stages; −2 against a target at 70% or less; mostly −1 if the target's last
    /// move was physical.
    /// </summary>
    private void ExpertCaptivate()
    {
        if (Stage(Who.Defender, StatType.SpAttack) != 6)
        {
            Score(-1);
            if (!(HpPercent(Who.Attacker) > 90)) Score(-1);
            if (!(Stage(Who.Defender, StatType.SpAttack) > 3) && !RandomBelow(50)) Score(-2);
        }
        if (!(HpPercent(Who.Defender) > 70)) Score(-2);
        if (DefenderLastMoveCategory != MoveCategory.Physical) return;
        if (RandomBelow(64)) return;
        Score(-1);
    }

    /// <summary><c>Expert_StealthRock</c>: word for word Toxic Spikes' routine.</summary>
    private void ExpertStealthRock() => ExpertToxicSpikes();

    /// <summary><c>Expert_RecoilMove</c>: unless the target resists or is immune, +1 for a user that takes no recoil (Rock Head, Magic Guard).</summary>
    private void ExpertRecoilMove()
    {
        if (Effectiveness() is Eff.Immune or Eff.Half or Eff.Quarter) return;
        if (AbilityOf(Who.Attacker) is "Rock Head" or "Magic Guard") Score(1);
    }

    /// <summary>
    /// <c>Expert_HealingWish</c>: a quarter of the time −5 for a healthy user (80% or more) that moves first; mostly
    /// −1 above 50%; else a chance of +1, more if it has no super-effective move or someone on the bench hits harder,
    /// and at 30% or less +1 half the time.
    /// </summary>
    private void ExpertHealingWish()
    {
        if (HpPercent(Who.Attacker) >= 80 && SpeedCompare != SpeedOrder.Slower)
        {
            if (RandomBelow(192)) return;
            Score(-5);
            return;
        }
        if (HpPercent(Who.Attacker) > 50)
        {
            if (RandomBelow(50)) return;
            Score(-1);
            return;
        }
        if (!RandomBelow(192))
        {
            Score(1);
            if (!HasSuperEffectiveMove() && !RandomBelow(192)) Score(1);
            if (BenchDealsMoreDamage(roll: false) && !RandomBelow(128)) Score(1);
        }
        if (HpPercent(Who.Attacker) > 30) return;
        if (RandomBelow(128)) return;
        Score(1);
    }
}
