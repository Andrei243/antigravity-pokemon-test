using PokemonPlatinumEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

/// <summary>
/// How a condition or a stat change comes to a Pokémon (the original's side-effect types): it decides what can
/// stop it. A move's own effect can be stopped by a Substitute and says so when it fails; a side effect of a hit
/// fails quietly; an ability's goes past a Substitute.
/// </summary>
internal enum By { Other, Move, SideEffect, Ability }

// What abilities, held items and moves change a battle through (IBattleContext): HP, stat stages, conditions, the
// weather. Each change is made at once and written into the log with the line that tells of it.
public sealed partial class BattleCore
{
    public void Announce(string text) => Say(text);

    public IEnumerable<Battler> ActiveFoes(Battler battler) => SlotsOf(Other(battler.Side)).Where(b => b.IsActive);

    public bool ChangeStat(Battler target, StatType stat, int amount, Battler? source, bool announceFailure = false) =>
        ChangeStat(target, stat, amount, source, announceFailure, By.Ability);

    /// <summary>
    /// <c>BtlCmd_ChangeStatStage</c>. A stat lowered by someone else is stopped, in this order, by a Mist over the
    /// target's side, by its ability (Clear Body, Keen Eye for accuracy), and by a Substitute.
    /// </summary>
    internal bool ChangeStat(Battler target, StatType stat, int amount, Battler? source, bool announceFailure, By how)
    {
        if (!target.IsActive || amount == 0) return false;
        var p = target.Pokemon!;
        bool byAnother = source != null && source != target;

        if (amount < 0 && byAnother)
        {
            if (Field.Side(target.Side).Mist)
            {
                if (announceFailure) Say(Text.Of("battle.isProtectedByThe", ("target", target.Name)));
                return false;
            }
            bool breaks = BattleEffects.Of(source!).Any(e => e.IgnoresTargetAbility);
            if (!breaks && target.Ability?.Effect is { } guard && guard.BlocksStatDrop(target, stat))
            {
                if (announceFailure) Say(Text.Of("battle.preventsItsFromBeing", ("target", target.Name), ("ability", AbilityOf(target)), ("stat", BattleText.StatName(stat))));
                return false;
            }
            // Mirror Armor turns it back on whoever tried (plan 06 · R27)
            if (!breaks && MirrorsTheDrop(target, source!, stat, amount, announceFailure, how)) return false;
            if (!breaks && GuardDogStands(target, source!, stat, how)) return false;
            // Flower Veil keeps it off a Grass type of its side (plan 06 · R27b)
            if (!breaks && FlowerVeiled(target, announceFailure)) return false;
        }
        // A berry eaten with Ripen raises twice as much (plan 06 · R27b)
        if (source == target && amount > 0) amount = Ripened(target, amount);

        // By the modern rules Simple doubles the change itself; under Platinum's its stages count double where they are read
        if (Rules.SimpleDoublesChanges) foreach (var e in BattleEffects.Of(target)) amount = e.ScaleStatChange(amount);
        int current = p.StatStages.GetValueOrDefault(stat);
        int next = Math.Clamp(current + amount, -6, 6);
        if (next == current)
        {
            if (announceFailure) Say(Text.Of(amount > 0 ? "battle.wontGoHigher" : "battle.wontGoLower", ("target", target.Name), ("stat", BattleText.StatName(stat))));
            return false;
        }

        if (amount < 0 && byAnother && target.HasSubstitute)
        {
            if (announceFailure) Say(Text.Of("battle.butItFailed"));
            return false;
        }

        p.StatStages[stat] = next;
        if (next > current) target.Turn.StatsRaised = true;
        else target.Turn.StatsLowered = true;
        Say(Text.Of("battle.statChange", ("pokemon", target.Name), ("stat", BattleText.StatName(stat)), ("stage", BattleText.StageChange(next - current))))
            .With(new StageChanged(target.Place, stat, next, next > current));
        // Defiant and Competitive answer a stage a foe took (plan 06 · R27)
        if (next < current && byAnother) AnswersTheDrop(target, source!);
        // Rattled answers an Intimidate, and Opportunist copies a foe's rise (plan 06 · R27b)
        if (next < current) RattledByIntimidate(target, source, stat, how);
        else Opportunism(target, stat, next - current);
        return true;
    }

    public bool TryInflictStatus(Battler target, StatusCondition status, Battler? source, bool announceFailure = false) =>
        TryInflictStatus(target, status, source, announceFailure, By.Ability);

    bool IBattleContext.Confuse(Battler target, string? line) => Confuse(target, null, false, By.Other, line);

    /// <summary>
    /// Gives a major status condition (the original's <c>subscript_poison</c>, <c>_burn</c>, <c>_paralyze</c>,
    /// <c>_freeze</c>, <c>_fall_asleep</c>). Stopped by a condition it already has, its types, its ability, the
    /// sun (a freeze), an uproar (sleep), and, when it comes from another Pokémon, by a Substitute and by a
    /// Safeguard over its side.
    /// </summary>
    internal bool TryInflictStatus(Battler target, StatusCondition status, Battler? source, bool announceFailure, By how)
    {
        if (!target.IsActive) return false;
        var p = target.Pokemon!;
        bool byAnother = source != null && source != target;

        if (p.Status != StatusCondition.None)
        {
            if (announceFailure) Say(p.Status == status || (status == StatusCondition.Toxic && p.Status == StatusCondition.Poison)
                ? BattleText.AlreadyHas(target.Name, p.Status) : Text.Of("battle.butItFailed"));
            return false;
        }

        bool typeImmune = status switch
        {
            StatusCondition.Burn => target.HasType(PokemonType.Fire),
            StatusCondition.Freeze => target.HasType(PokemonType.Ice),
            // (Corrosion poisons them all the same: plan 06 · R27)
            StatusCondition.Poison or StatusCondition.Toxic => (target.HasType(PokemonType.Poison) || target.HasType(PokemonType.Steel)) && !(byAnother && Corrodes(source)),
            StatusCondition.Paralyze => Rules.ElectricTypesCantBeParalyzed && target.HasType(PokemonType.Electric),
            _ => false
        };
        if (typeImmune)
        {
            if (announceFailure) Say(Text.Of("battle.itDoesntAffect", ("target", target.Name)));
            return false;
        }

        // A terrain keeps conditions off whoever stands on it (plan 06 · R19b)
        if (TerrainKeepsOff(target, status, announceFailure)) return false;

        // Nothing freezes under a strong sun
        if (status == StatusCondition.Freeze && Field.WeatherInEffect == BattleWeather.Sun)
        {
            if (announceFailure) Say(Text.Of("battle.butItFailed"));
            return false;
        }

        bool breaks = byAnother && BattleEffects.Of(source!).Any(e => e.IgnoresTargetAbility);
        if (!breaks && target.Ability?.Effect is { } guard && guard.BlocksStatus(target, status))
        {
            if (announceFailure) Say(Text.Of("battle.prevents", ("target", target.Name), ("ability", AbilityOf(target)), ("condition", BattleText.StatusName(status))));
            return false;
        }
        // An ally's veil: Sweet Veil, Pastel Veil, Flower Veil (plan 06 · R27b)
        if (VeiledFrom(target, status, source, announceFailure)) return false;

        if (status == StatusCondition.Sleep && UproarIsOn && !Has(target, "Soundproof"))
        {
            if (announceFailure) Say(Text.Of("battle.butTheUproarKept", ("target", target.Name)));
            return false;
        }

        if (byAnother && how != By.Ability && target.HasSubstitute)
        {
            if (announceFailure) Say(Text.Of("battle.butItFailed"));
            return false;
        }

        if (byAnother && Field.Side(target.Side).Safeguard)
        {
            if (announceFailure) Say(Text.Of("battle.isProtectedBySafeguard", ("target", target.Name)));
            return false;
        }

        p.Status = status;
        if (status == StatusCondition.Sleep) p.SleepTurns = Rules.SleepCounterBase + rng.Roll(RollKind.SleepTurns, Rules.SleepLengths);
        if (status == StatusCondition.Toxic) p.ToxicCounter = 0;
        Say(BattleText.Inflicted(target.Name, status)).With(new StatusChanged(target.Place, status));

        // A Pokémon put to sleep or frozen in the middle of a move comes out of it
        if (status is StatusCondition.Sleep or StatusCondition.Freeze) Unlock(target);
        // Shaymin's Sky Forme can't stand the cold: frozen, it is back in its Land Forme for good
        // (subscript_check_shaymin_form, after the hit; plan 06 · R10)
        if (status == StatusCondition.Freeze && !target.HasSubstitute && FormRules.BackToLand(p))
            Say(Text.Of("battle.changedBackIntoIts", ("target", target.Name))).With(new Reshaped(target.Place));
        CheckConditionHooks(target, source);
        // Poison Puppeteer confuses whoever it poisons (plan 06 · R27)
        // (only Pecharunt's, and only by a move: plan 06 · R27b's audit, Showdown's poisonpuppeteer onAnyAfterSetStatus)
        if (byAnother && status is StatusCondition.Poison or StatusCondition.Toxic && Has(source!, "Poison Puppeteer") && source!.Pokemon!.Species.Name == "Pecharunt"
            && how is By.Move or By.SideEffect) Confuse(target, source, false, By.Ability);
        return true;
    }

    /// <summary>Confuses (<c>subscript_confuse</c>) unless already confused, kept clear by Own Tempo, behind a Substitute or under a Safeguard.</summary>
    internal bool Confuse(Battler target, Battler? source, bool announceFailure, By how, string? line = null)
    {
        // What the line names is said with it alone, never with a line of failure
        var naming = named.ToList();
        Unsaid();
        if (!target.IsActive) return false;
        bool byAnother = source != null && source != target;
        if (target.IsConfused)
        {
            if (announceFailure) Say(Text.Of("battle.isAlreadyConfused", ("target", target.Name)));
            return false;
        }
        // Misty Terrain keeps any confusion off whoever stands on it, a rampage's too (plan 06 · R19b)
        if (Field.Has(FieldCondition.MistyTerrain) && DamageCalculator.OnTheTerrain(target))
        {
            if (announceFailure) Say(Text.Of("battle.surroundsItselfWithMist", ("target", target.Name)));
            return false;
        }
        bool breaks = byAnother && BattleEffects.Of(source!).Any(e => e.IgnoresTargetAbility);
        if (BattleEffects.Of(target, includeAbility: !breaks).Any(e => e.BlocksConfusion))
        {
            if (announceFailure) Say(Text.Of("battle.preventsConfusion", ("target", target.Name), ("ability", AbilityOf(target))));
            return false;
        }
        if (byAnother && how != By.Ability && target.HasSubstitute)
        {
            if (announceFailure) Say(Text.Of("battle.butItFailed"));
            return false;
        }
        if (byAnother && Field.Side(target.Side).Safeguard)
        {
            if (announceFailure) Say(Text.Of("battle.isProtectedBySafeguard", ("target", target.Name)));
            return false;
        }
        target.ConfusionTurns = 2 + rng.Roll(RollKind.ConfusionTurns, 4);
        named.AddRange(naming);
        Say(line ?? Text.Of("battle.becameConfused", ("target", target.Name))).With(new Confused(target.Place)).With(Passing(target));
        CheckConditionHooks(target, source);
        return true;
    }

    public void RestoreHp(Battler target, int amount, string message)
    {
        if (!target.IsActive)
        {
            Unsaid();
            return;
        }
        var p = target.Pokemon!;
        // A berry eaten with Ripen gives twice as much (plan 06 · R27b)
        p.CurrentHP = Math.Min(p.MaxHP, p.CurrentHP + Ripened(target, amount));
        Say(message).With(new HpChanged(target.Place, p.CurrentHP, Healed: true));
    }

    public void LoseHp(Battler target, int amount, string message) => LoseHp(target, amount, message, direct: false);

    /// <param name="direct">Damage Magic Guard doesn't stop (Struggle's recoil, a Curse's price).</param>
    private void LoseHp(Battler target, int amount, string message, bool direct)
    {
        if (!target.IsActive || (!direct && BattleEffects.Of(target).Any(e => e.PreventsIndirectDamage)))
        {
            Unsaid();
            return;
        }
        var p = target.Pokemon!;
        p.CurrentHP = Math.Max(0, p.CurrentHP - amount);
        target.Turn.TookDamage = true;
        Say(message).With(new HpChanged(target.Place, p.CurrentHP, Healed: false));
        CheckConditionHooks(target, null);
    }

    public void CureStatus(Battler target, string message)
    {
        var p = target.Pokemon!;
        p.Status = StatusCondition.None;
        p.SleepTurns = 0;
        p.ToxicCounter = 0;
        target.Volatile.Nightmare = false;
        Say(message).With(new StatusChanged(target.Place, StatusCondition.None));
    }

    /// <summary>Uses up the held item, remembering it for Recycle (the original's <c>recycleItem</c>).</summary>
    public void ConsumeItem(Battler holder)
    {
        if (holder.Pokemon?.HeldItem is not { } item) return;
        // A berry eaten lets Belch be used, for the rest of the battle (plan 06 · R25; Showdown's ateBerry)
        if (item.Pocket == ItemPocket.Berries) ateBerry.Add(holder.Pokemon);
        holder.Volatile.ConsumedItem = item;
        holder.Pokemon.HeldItem = null;
        // Cheek Pouch, Ripen, Cud Chew and Symbiosis answer it (plan 06 · R27b)
        Ate(holder, item);
    }

    public bool AnyoneHas(string ability) => AllBattlers.Any(b => b.IsActive && b.Ability?.Name == ability);

    public IEnumerable<Battler> Everyone => BySpeed().Where(b => b.IsActive);

    /// <summary>
    /// An ability's infatuation (Cute Charm; <c>subscript_infatuate</c> as an ability's side effect): Oblivious
    /// says so, the wrong gender or a love already there goes unsaid, and the ability is named when it works.
    /// </summary>
    bool IBattleContext.Infatuate(Battler target, Battler with)
    {
        if (!target.IsActive || !with.IsActive) return false;
        if (target.Ability is { Effect.BlocksInfatuation: true })
        {
            Say(Text.Of("battle.preventsRomance", ("target", target.Name), ("ability", AbilityOf(target))));
            return false;
        }
        var mine = with.Pokemon!.Gender;
        var theirs = target.Pokemon!.Gender;
        if (mine == theirs || mine == Gender.Genderless || theirs == Gender.Genderless || target.Volatile.InLoveWith != null) return false;
        target.Volatile.InLoveWith = with.Place;
        Say(Text.Of("battle.infatuated", ("pokemon", with.Name), ("ability", AbilityOf(with)), ("target", target.Name))).With(Passing(target));
        Reciprocate(target, with);
        return true;
    }

    /// <summary>
    /// Changes the weather. A move's weather has its turns; an ability's has none under Platinum's rules (it
    /// lasts), and there the same weather already lasting is left alone and said nothing of.
    /// </summary>
    public bool SetWeather(BattleWeather weather, int turns, string line)
    {
        // Under the modern rules hail is snow (plan 06 · R19b): the caller asks for hail and says hail's line
        if (Rules.Sky(weather) != weather)
        {
            if (line == Starts(weather)) line = Starts(Rules.Sky(weather));
            weather = Rules.Sky(weather);
        }
        // No other weather can take a strong one's place (Showdown's setWeather: STRONG_WEATHERS)
        if (Field.Weather == weather && (turns > 0 || Field.WeatherLasts) || Field.WeatherIsStrong)
        {
            Unsaid();
            return false;
        }
        Field.StrongWeatherBy = null;
        Field.Weather = weather;
        Field.WeatherTurns = turns;
        Field.WeatherLasts = turns == 0 && weather != BattleWeather.None;
        Say(line).With(new WeatherChanged(weather));
        // Snow mends a broken icy face (plan 06 · R19b)
        IceFaceInTheSnow();
        // The sun wakes Protosynthesis, its stat fixed now (plan 06 · R27b)
        ParadoxesWake();
        return true;
    }
}
