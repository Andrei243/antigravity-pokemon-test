using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Effects;

// What each ability does in battle, following Platinum (plan 06 · R7 completed the 123). AbilityDatabase pairs them
// with names and descriptions. Small building blocks first, then the abilities that need their own code. An ability
// whose whole effect is a bonus inside the damage formula (Technician, Guts, Thick Fat, Overgrow…) has no code here:
// DamageCalculator applies each by name in the original's own place and order. An ability that works only in the
// field (Pickup, Stench…) is a bare FlagEffect, so it counts as run; its field code goes by its name.

/// <summary>Can't be given one or more major status conditions (Immunity, Insomnia, Limber, Water Veil...).</summary>
internal sealed class StatusImmunity(params StatusCondition[] blocked) : BattleEffect
{
    public override bool BlocksStatus(Battler self, StatusCondition status) => blocked.Contains(status);
}

/// <summary>Moves of one type heal a quarter of its HP instead of hurting it (Volt Absorb, Water Absorb).</summary>
internal sealed class AbsorbType(PokemonType type, string abilityName) : BattleEffect
{
    public override bool AbsorbsMove(IBattleContext ctx, Battler self, Battler attacker, Move move, float effectiveness)
    {
        if (move.Type != type || attacker == self) return false;
        var p = self.Pokemon!;
        if (p.CurrentHP >= p.MaxHP) ctx.Announce($"{self.Name}'s {abilityName} made {move.Name} useless!");
        else ctx.RestoreHp(self, p.MaxHP / 4, $"{self.Name}'s {abilityName} restored its HP!");
        return true;
    }
}

/// <summary>Takes less from a super-effective hit, once the types have had their say (Filter, Solid Rock).</summary>
internal sealed class DamageShield(Func<Move, float, bool> applies, float factor) : BattleEffect
{
    public override float IncomingDamageMultiplier(Battler self, Battler attacker, Move move, float effectiveness) =>
        applies(move, effectiveness) ? factor : 1f;
}

/// <summary>A contact move against it may give the attacker a status (Static, Flame Body, Poison Point, Effect Spore).</summary>
internal sealed class ContactStatus(int chancePercent, params StatusCondition[] statuses) : BattleEffect
{
    public override void AfterHit(IBattleContext ctx, Battler self, Battler attacker, Move move, int damage, bool critical)
    {
        if (!move.Data.MakesContact || !attacker.IsActive || attacker.Pokemon!.Status != StatusCondition.None) return;
        if (ctx.Random.Roll(RollKind.AbilityChance, 100) >= chancePercent) return;
        ctx.TryInflictStatus(attacker, statuses[ctx.Random.Roll(RollKind.AbilityPick, statuses.Length)], self);
    }
}

/// <summary>Its stats can't be lowered by other Pokémon (all of them, or one).</summary>
internal sealed class StatGuard(StatType? only = null) : BattleEffect
{
    public override bool BlocksStatDrop(Battler self, StatType stat) => only == null || only == stat;
}

/// <summary>An ability with no effect beyond a flag the engine reads, and perhaps a line on entering.</summary>
internal sealed class FlagEffect : BattleEffect
{
    public bool NoCrits, NoRecoil, NoConfusion, NoFlinch, BreakAbilities, Unaware, Adaptability, Escapes, Anchored, NoRomance, FiveHits, KeepsHeldItem;
    public bool Loafs, Stalls, EatsEarly, PoisonHeals, Normalizes, Klutz, SlowStart, Unburden;
    public int CritBonus, SideEffectMultiplier = 1, PpPressure, SleepRate = 1;
    public float Accuracy = 1f;
    public PokemonType? Draws;

    /// <summary>Said as the holder comes in ("{0} breaks the mold!").</summary>
    public string? EntryLine;

    public override bool PreventsCriticalHits => NoCrits;
    public override bool PreventsRecoil => NoRecoil;
    public override bool BlocksConfusion => NoConfusion;
    public override bool BlocksFlinch => NoFlinch;
    public override bool IgnoresTargetAbility => BreakAbilities;
    public override bool AlwaysEscapes => Escapes;
    public override bool HoldsItsGround => Anchored;
    public override bool AlwaysHitsFiveTimes => FiveHits;
    public override bool KeepsItem => KeepsHeldItem;
    public override bool BlocksInfatuation => NoRomance;
    public override bool IgnoresOthersStatStages => Unaware;
    public override float? StabOverride => Adaptability ? 2f : null;
    public override int CritStageBonus => CritBonus;
    public override int SideEffectChanceMultiplier => SideEffectMultiplier;
    public override int ExtraPpUsed => PpPressure;
    public override int SleepCountdownRate => SleepRate;
    public override float AccuracyMultiplier(Battler self, Move move) => Accuracy;
    public override bool LoafsEveryOtherTurn => Loafs;
    public override bool MovesLast => Stalls;
    public override bool EatsBerriesEarly => EatsEarly;
    public override bool HealsWithPoison => PoisonHeals;
    public override bool NormalizesMoves => Normalizes;
    public override bool IgnoresHeldItem => Klutz;
    public override bool StartsSlow => SlowStart;
    public override bool SpeedsUpUnburdened => Unburden;
    public override bool DrawsMovesOf(PokemonType type) => Draws == type;

    public override void OnEntry(IBattleContext ctx, Battler self)
    {
        if (EntryLine != null) ctx.Announce(string.Format(EntryLine, self.Name));
    }
}

// ---------------------------------------------------------------- abilities with their own code

internal sealed class Intimidate : BattleEffect
{
    public override void OnEntry(IBattleContext ctx, Battler self)
    {
        foreach (var foe in ctx.ActiveFoes(self).ToList())
        {
            ctx.Announce($"{self.Name}'s Intimidate cuts {foe.Name}'s Attack!");
            ctx.ChangeStat(foe, StatType.Attack, -1, self, announceFailure: true);
        }
    }
}

internal sealed class Download : BattleEffect
{
    public override void OnEntry(IBattleContext ctx, Battler self)
    {
        var foes = ctx.ActiveFoes(self).ToList();
        if (foes.Count == 0) return;
        int def = foes.Sum(f => f.Pokemon!.GetEffectiveStat(StatType.Defense));
        int spDef = foes.Sum(f => f.Pokemon!.GetEffectiveStat(StatType.SpDefense));
        ctx.ChangeStat(self, def < spDef ? StatType.Attack : StatType.SpAttack, 1, self);
    }
}

/// <summary>
/// Trace (<c>SWITCH_IN_CHECK_STATE_TRACE</c>): the holder takes a foe's ability as it comes in, drawn between two
/// foes that have one it can take, never Forecast, Trace or Multitype; the ability taken acts on entry as its own
/// would, and is given back when the holder leaves.
/// </summary>
internal sealed class Trace : BattleEffect
{
    /// <summary>The foes whose ability the holder could take (<c>ChooseTraceTarget</c>).</summary>
    internal static List<Battler> Candidates(IBattleContext ctx, Battler self) =>
        ctx.ActiveFoes(self).Where(f => f.Pokemon!.AbilityName is { } a && a is not ("Forecast" or "Trace" or "Multitype")).ToList();

    public override void OnEntry(IBattleContext ctx, Battler self)
    {
        if (self.Pokemon!.HeldItem?.Name == "Griseous Orb") return;
        var foes = Candidates(ctx, self);
        if (foes.Count == 0) return;
        var foe = foes.Count == 1 ? foes[0] : foes[ctx.Random.Roll(RollKind.AbilityPick, foes.Count)];
        var p = self.Pokemon;
        var o = self.Volatile.Original ??= new Original();
        if (!o.AbilityKept)
        {
            o.AbilityKept = true;
            o.AbilityName = p.AbilityName;
        }
        p.AbilityName = foe.Pokemon!.AbilityName;
        ctx.Announce($"{self.Name} traced {foe.Name}'s {p.AbilityName}!");
        // The ability taken acts on entry in its own phase of the same check, as the original's flags let it
    }
}

/// <summary>
/// Anticipation (<c>SWITCH_IN_CHECK_STATE_ANTICIPATION</c>): the holder shudders at a foe's damaging move that
/// would be super effective, or a one-hit knockout from a foe of its level or more, unless the move wouldn't
/// touch it at all; a few fixed-damage moves and the counters don't count (<c>sMovesCannotTriggerAnticipation</c>).
/// A move goes by the type its data gives it (Hidden Power is Normal), or by Normal for a foe with Normalize.
/// </summary>
internal sealed class Anticipation : BattleEffect
{
    private static readonly HashSet<string> Overlooked = new() { "40DamageFlat", "LevelDamageFlat", "RandomDamage1To150Level", "Counter", "MirrorCoat", "MetalBurst" };

    public override void OnEntry(IBattleContext ctx, Battler self)
    {
        foreach (var foe in ctx.ActiveFoes(self))
        {
            bool normalizes = foe.Ability?.Effect is { NormalizesMoves: true };
            foreach (var move in foe.Pokemon!.Moves)
            {
                if (move.Category == MoveCategory.Status || Overlooked.Contains(move.Data.Effect ?? "")) continue;
                var typed = normalizes && move.Data.Type != PokemonType.Normal ? new Move(move.Data.OfType(PokemonType.Normal)) : new Move(move.Data);
                float effectiveness = DamageCalculator.Effectiveness(foe, self, typed, ctx.Rules);
                if (effectiveness == 0f) continue;
                bool ohko = move.Data.Effect == "OneHitKo" && self.Pokemon!.Level <= foe.Pokemon.Level;
                if (ohko || effectiveness > 1f)
                {
                    ctx.Announce($"{self.Name} shuddered!");
                    return;
                }
            }
        }
    }
}

/// <summary>
/// Forewarn (<c>SWITCH_IN_CHECK_STATE_FOREWARN</c>): the holder is told the foes' strongest move. A move of variable
/// power counts as 80, a counter as 120 and a one-hit knockout as 150; a tie is a coin; with no damaging move among
/// them, any move of a foe.
/// </summary>
internal sealed class Forewarn : BattleEffect
{
    public override void OnEntry(IBattleContext ctx, Battler self)
    {
        var foes = ctx.ActiveFoes(self).ToList();
        if (foes.Count == 0) return;
        Move? strongest = null;
        int best = 0;
        foreach (var foe in foes)
        {
            foreach (var move in foe.Pokemon!.Moves)
            {
                int power = move.Power;
                if (power == 0 && move.Category != MoveCategory.Status)
                    power = move.Data.Effect switch { "OneHitKo" => 150, "Counter" or "MirrorCoat" or "MetalBurst" => 120, _ => 80 };
                if (power > best || (power == best && power > 0 && ctx.Random.Roll(RollKind.AbilityPick, 2) == 1))
                {
                    best = power;
                    strongest = move;
                }
            }
        }
        if (strongest == null)
        {
            var foe = foes[ctx.Random.Roll(RollKind.AbilityPick, foes.Count)];
            var moves = foe.Pokemon!.Moves;
            if (moves.Count == 0) return;
            strongest = moves[ctx.Random.Roll(RollKind.AbilityPick, moves.Count)];
        }
        ctx.Announce($"{self.Name}'s Forewarn alerted it to {strongest.Name}!");
    }
}

/// <summary>Frisk (<c>SWITCH_IN_CHECK_STATE_FRISK</c>): the holder names a foe's item as it comes in, one of two at a coin.</summary>
internal sealed class Frisk : BattleEffect
{
    public override void OnEntry(IBattleContext ctx, Battler self)
    {
        var holders = ctx.ActiveFoes(self).Where(f => f.Pokemon!.HeldItem != null).ToList();
        if (holders.Count == 0) return;
        var foe = holders.Count == 1 ? holders[0] : holders[ctx.Random.Roll(RollKind.AbilityPick, 2)];
        ctx.Announce($"{self.Name} frisked {foe.Name} and found its {foe.Pokemon!.HeldItem!.Name}!");
    }
}

/// <summary>Slow Start: the holder's Attack and Speed are halved for its first five turns, which the battle counts (<see cref="BattleEffect.StartsSlow"/>).</summary>
internal sealed class SlowStart : BattleEffect
{
    public override bool StartsSlow => true;
    public override void OnEntry(IBattleContext ctx, Battler self) => ctx.Announce($"{self.Name} can't get going yet!");
}

/// <summary>Color Change (<c>BattleSystem_TriggerAbilityOnHit</c>): the holder becomes the type of the damaging move that hit it, while it stands and isn't that type already; not Struggle.</summary>
internal sealed class ColorChange : BattleEffect
{
    public override void AfterHit(IBattleContext ctx, Battler self, Battler attacker, Move move, int damage, bool critical)
    {
        if (!self.IsActive || damage <= 0 || move.Category == MoveCategory.Status || move.Data == BattleCore.StruggleData || self.HasType(move.Type)) return;
        self.Volatile.Types = new List<PokemonType> { move.Type };
        ctx.Announce($"{self.Name} transformed into the {move.Type} type!");
    }
}

/// <summary>Aftermath: whoever knocks the holder out with a contact move loses a quarter of its HP, unless anyone on the field has Damp or it has Magic Guard.</summary>
internal sealed class Aftermath : BattleEffect
{
    public override void AfterHit(IBattleContext ctx, Battler self, Battler attacker, Move move, int damage, bool critical)
    {
        if (self.Pokemon!.CurrentHP > 0 || !move.Data.MakesContact || !attacker.IsActive || attacker == self || ctx.AnyoneHas("Damp")) return;
        ctx.LoseHp(attacker, Formulas.Divide(attacker.Pokemon!.MaxHP, 4), $"{attacker.Name} is hurt by {self.Name}'s Aftermath!");
    }
}

/// <summary>Cute Charm: three times in ten a contact move's user falls in love with the holder, if the genders allow.</summary>
internal sealed class CuteCharm : BattleEffect
{
    public override void AfterHit(IBattleContext ctx, Battler self, Battler attacker, Move move, int damage, bool critical)
    {
        if (!move.Data.MakesContact || !attacker.IsActive || !self.IsActive || attacker == self || attacker.Volatile.InLoveWith != null) return;
        if (ctx.Random.Roll(RollKind.AbilityChance, 100) >= 30) return;
        ctx.Infatuate(attacker, self);
    }
}

internal sealed class Levitate : BattleEffect
{
    public override bool AbsorbsMove(IBattleContext ctx, Battler self, Battler attacker, Move move, float effectiveness)
    {
        if (move.Type != PokemonType.Ground || move.Category == MoveCategory.Status) return false;
        // Gravity, its own roots or an Iron Ball bring it down
        if (DamageCalculator.IsHeldDown(self)) return false;
        ctx.Announce($"{self.Name} makes Ground moves miss with Levitate!");
        return true;
    }

    public override bool Levitates => true;
}

internal sealed class FlashFire : BattleEffect
{
    public override bool AbsorbsMove(IBattleContext ctx, Battler self, Battler attacker, Move move, float effectiveness)
    {
        if (move.Type != PokemonType.Fire || attacker == self || self.Pokemon!.Status == StatusCondition.Freeze) return false;
        if (!self.FlashFire)
        {
            self.FlashFire = true;
            ctx.Announce($"{self.Name}'s Flash Fire raised its Fire power!");
        }
        else ctx.Announce($"{self.Name}'s Flash Fire made {move.Name} useless!");
        return true;
    }
}

internal sealed class MotorDrive : BattleEffect
{
    public override bool AbsorbsMove(IBattleContext ctx, Battler self, Battler attacker, Move move, float effectiveness)
    {
        if (move.Type != PokemonType.Electric || attacker == self) return false;
        ctx.Announce($"{self.Name}'s Motor Drive took in the electricity!");
        ctx.ChangeStat(self, StatType.Speed, 1, self, announceFailure: true);
        return true;
    }
}

internal sealed class WonderGuard : BattleEffect
{
    public override bool AbsorbsMove(IBattleContext ctx, Battler self, Battler attacker, Move move, float effectiveness)
    {
        if (move.Category == MoveCategory.Status || effectiveness > 1f || attacker == self) return false;
        ctx.Announce($"{self.Name} avoided damage with Wonder Guard!");
        return true;
    }
}

internal sealed class Soundproof : BattleEffect
{
    public override bool AbsorbsMove(IBattleContext ctx, Battler self, Battler attacker, Move move, float effectiveness)
    {
        if ((move.Data.Flags & MoveFlags.Sound) == 0 || attacker == self) return false;
        ctx.Announce($"{self.Name}'s Soundproof blocks {move.Name}!");
        return true;
    }
}

/// <summary>Guts: a burn doesn't halve its attacks; the Attack bonus is in the damage formula by name.</summary>
internal sealed class Guts : BattleEffect
{
    public override bool IgnoresBurnPenalty => true;
}

/// <summary>Hustle: physical moves hit less often; the Attack bonus is in the damage formula by name.</summary>
internal sealed class Hustle : BattleEffect
{
    public override float AccuracyMultiplier(Battler self, Move move) => move.Category == MoveCategory.Physical ? 0.8f : 1f;
}

internal sealed class TintedLens : BattleEffect
{
    public override float DamageMultiplier(Battler self, Battler target, Move move, float effectiveness) =>
        effectiveness > 0f && effectiveness < 1f ? 2f : 1f;
}

/// <summary>Quick Feet: half as fast again with a condition, and paralysis doesn't slow it; both in the turn order by name.</summary>
internal sealed class QuickFeet : BattleEffect
{
    public override bool IgnoresParalysisSlowdown => true;
}

internal sealed class TangledFeet : BattleEffect
{
    public override float EvasionMultiplier(Battler self) => self.IsConfused ? 0.5f : 1f;
}

/// <summary>Simple: stat changes on the holder count double, in the formula by name and here for the rest.</summary>
internal sealed class SimpleEffect : BattleEffect
{
    public override int ScaleStatChange(int amount) => amount * 2;
}

internal sealed class RoughSkin : BattleEffect
{
    public override void AfterHit(IBattleContext ctx, Battler self, Battler attacker, Move move, int damage, bool critical)
    {
        if (!move.Data.MakesContact || !attacker.IsActive || attacker == self) return;
        ctx.LoseHp(attacker, Math.Max(1, attacker.Pokemon!.MaxHP / 8), $"{self.Name}'s Rough Skin hurt {attacker.Name}!");
    }
}

internal sealed class AngerPoint : BattleEffect
{
    public override void AfterHit(IBattleContext ctx, Battler self, Battler attacker, Move move, int damage, bool critical)
    {
        if (!critical || !self.IsActive) return;
        self.Pokemon!.StatStages[StatType.Attack] = 6;
        ctx.Announce($"{self.Name}'s Anger Point maxed its Attack!");
    }
}

internal sealed class Steadfast : BattleEffect
{
    public override void OnFlinch(IBattleContext ctx, Battler self) => ctx.ChangeStat(self, StatType.Speed, 1, self);
}

internal sealed class SpeedBoost : BattleEffect
{
    public override void AtEndOfTurn(IBattleContext ctx, Battler self)
    {
        if (self.Pokemon!.StatStages.GetValueOrDefault(StatType.Speed) < 6) ctx.ChangeStat(self, StatType.Speed, 1, self);
    }
}

internal sealed class ShedSkin : BattleEffect
{
    public override void AtEndOfTurn(IBattleContext ctx, Battler self)
    {
        if (self.Pokemon!.Status != StatusCondition.None && ctx.Random.Roll(RollKind.AbilityChance, 100) < 30)
            ctx.CureStatus(self, $"{self.Name}'s Shed Skin cured its {BattleText.StatusName(self.Pokemon.Status)}!");
    }
}

internal sealed class NaturalCure : BattleEffect
{
    public override void OnWithdraw(Battler self)
    {
        if (self.Pokemon!.Status != StatusCondition.Faint) self.Pokemon.Status = StatusCondition.None;
    }
}

internal sealed class Synchronize : BattleEffect
{
    public override void OnConditionChanged(IBattleContext ctx, Battler self, Battler? cause)
    {
        var status = self.Pokemon!.Status;
        if (cause == null || cause == self || !cause.IsActive || cause.Pokemon!.Status != StatusCondition.None) return;
        if (status is StatusCondition.Burn or StatusCondition.Paralyze or StatusCondition.Poison or StatusCondition.Toxic)
        {
            ctx.Announce($"{self.Name}'s Synchronize shares its condition!");
            ctx.TryInflictStatus(cause, status == StatusCondition.Toxic ? StatusCondition.Toxic : status, self, announceFailure: true);
        }
    }
}

internal sealed class NoGuard : BattleEffect
{
    public override bool MovesNeverMiss => true;
}

internal sealed class Scrappy : BattleEffect
{
    public override bool HitsGhosts => true;
}

internal sealed class ShieldDust : BattleEffect
{
    public override bool BlocksSideEffects => true;
}

internal sealed class LiquidOoze : BattleEffect
{
    public override bool HurtsDrainers => true;
}

internal sealed class MagicGuard : BattleEffect
{
    public override bool PreventsIndirectDamage => true;
}

internal sealed class Sniper : BattleEffect
{
    public override float CriticalBoost => 1.5f;
}

/// <summary>Unburden: once the item it came in with is gone, its Speed doubles (<see cref="BattleEffect.SpeedsUpUnburdened"/>).</summary>
internal sealed class Unburden : BattleEffect
{
    public override bool SpeedsUpUnburdened => true;
}

// ---------------------------------------------------------------- shapes (plan 06 · R7)

/// <summary>Forecast (<c>BattleSystem_TriggerFormChange</c>): Castform takes the weather's shape and type in the sun, the rain and the hail, and its own back under any other sky or one that is ignored.</summary>
internal sealed class Forecast : BattleEffect
{
    public override string? ChangeShape(IBattleContext ctx, Battler self)
    {
        var p = self.Pokemon!;
        if (p.Species.Name != "Castform") return null;
        string? wanted = ctx.Field.WeatherInEffect switch
        {
            BattleWeather.Sun => "Castform-Sunny",
            BattleWeather.Rain => "Castform-Rainy",
            BattleWeather.Hail => "Castform-Snowy",
            _ => null
        };
        return Shapes.Take(self, wanted);
    }
}

/// <summary>
/// Multitype: Arceus is the type of the plate it holds, whatever is done to the plate's use (the original reads
/// the item itself). Its shape stays after the battle, as the original keeps a held plate's form in the party.
/// </summary>
internal sealed class Multitype : BattleEffect
{
    public override string? ChangeShape(IBattleContext ctx, Battler self)
    {
        var p = self.Pokemon!;
        if (p.Species.Name != "Arceus") return null;
        string? wanted = null;
        if (p.HeldItem?.HoldEffect is { } hold && hold.StartsWith("Arceus") && p.Species.Form("Arceus-" + hold["Arceus".Length..]) != null)
            wanted = "Arceus-" + hold["Arceus".Length..];
        if (p.Form == wanted) return null;
        // The shapes differ in type alone, so nothing is worked out again (a Ditto in Arceus's shape keeps its own HP)
        p.Form = wanted;
        return $"{self.Name} transformed!";
    }
}

/// <summary>
/// The shapes the weather gives (<c>BattleSystem_TriggerFormChange</c>). They differ from the species' own in
/// type and look alone, so the form is set and nothing is worked out again; a shape taken for the battle is kept
/// the first time (<c>Volatiles.Original</c>) and given back when the Pokémon leaves.
/// </summary>
internal static class Shapes
{
    public static string? Take(Battler self, string? form)
    {
        var p = self.Pokemon!;
        if (p.Form == form) return null;
        var o = self.Volatile.Original ??= new Original();
        if (o.Species == null)
        {
            o.Species = p.Species;
            o.Form = p.Form;
        }
        p.Form = form;
        return $"{self.Name} transformed!";
    }

    /// <summary>
    /// Cherrim blooms in the sun and closes under any other sky. The original asks only the species, not the
    /// ability: a Cherrim whose Flower Gift was taken or suppressed still changes with the weather.
    /// </summary>
    public static string? Cherrim(IBattleContext ctx, Battler self) =>
        Take(self, ctx.Field.WeatherInEffect == BattleWeather.Sun ? "Cherrim-Sunshine" : null);
}

// ---------------------------------------------------------------- the weather (plan 06 · R3)

/// <summary>
/// Drizzle, Drought, Sand Stream, Snow Warning: the weather comes in with the holder. Under Platinum's rules it
/// has no end of its own; under the modern ones it lasts as a move's does, longer with the weather's rock.
/// </summary>
internal sealed class WeatherBringer(BattleWeather weather, string rock, string line) : BattleEffect
{
    public override void OnEntry(IBattleContext ctx, Battler self)
    {
        int turns = ctx.Rules.AbilityWeatherTurns;
        if (turns > 0) turns += BattleEffects.Of(self).Sum(e => e.ExtraTurns(rock));
        ctx.SetWeather(weather, turns, string.Format(line, self.Name));
    }
}

/// <summary>Cloud Nine, Air Lock: while the holder is out the weather is there and does nothing.</summary>
internal sealed class WeatherBlind : BattleEffect
{
    public override bool IgnoresWeather => true;
}

/// <summary>Swift Swim, Chlorophyll: twice the Speed in their weather.</summary>
internal sealed class WeatherSpeed(BattleWeather weather) : BattleEffect
{
    public override float SpeedMultiplier(Battler self) => self.Field?.WeatherInEffect == weather ? 2f : 1f;
}

/// <summary>Sand Veil, Snow Cloak: harder to hit in their weather, and not hurt by it.</summary>
internal sealed class WeatherCloak(BattleWeather weather) : BattleEffect
{
    public override float EvasionMultiplier(Battler self) => self.Field?.WeatherInEffect == weather ? 0.8f : 1f;
    public override bool ShelteredFrom(BattleWeather from) => from == weather;
}

/// <summary>Rain Dish, Ice Body: a sixteenth of its HP back at each turn's end in their weather (and hail doesn't hurt Ice Body).</summary>
internal sealed class WeatherHealer(BattleWeather weather, string name) : BattleEffect
{
    public override bool ShelteredFrom(BattleWeather from) => from == weather;

    public override void UnderTheWeather(IBattleContext ctx, Battler self, BattleWeather over)
    {
        var p = self.Pokemon!;
        if (over != weather || p.CurrentHP >= p.MaxHP) return;
        ctx.RestoreHp(self, Formulas.Divide(p.MaxHP, 16), $"{self.Name}'s {name} restored a little HP!");
    }
}

/// <summary>Water moves heal it by a quarter and rain by an eighth each turn; the sun takes an eighth each turn. Fire hurting it a quarter more is in the damage formula by name.</summary>
internal sealed class DrySkin : BattleEffect
{
    private readonly AbsorbType water = new(PokemonType.Water, "Dry Skin");

    public override bool AbsorbsMove(IBattleContext ctx, Battler self, Battler attacker, Move move, float effectiveness) =>
        water.AbsorbsMove(ctx, self, attacker, move, effectiveness);

    public override void UnderTheWeather(IBattleContext ctx, Battler self, BattleWeather over)
    {
        var p = self.Pokemon!;
        if (over == BattleWeather.Rain && p.CurrentHP < p.MaxHP)
            ctx.RestoreHp(self, Formulas.Divide(p.MaxHP, 8), $"{self.Name}'s Dry Skin took in the rain!");
        else if (over == BattleWeather.Sun)
            ctx.LoseHp(self, Formulas.Divide(p.MaxHP, 8), $"{self.Name}'s Dry Skin suffers in the sun!");
    }
}

/// <summary>In the sun it loses an eighth of its HP each turn; its special moves being half as strong again is in the damage formula by name.</summary>
internal sealed class SolarPower : BattleEffect
{
    public override void UnderTheWeather(IBattleContext ctx, Battler self, BattleWeather over)
    {
        if (over == BattleWeather.Sun)
            ctx.LoseHp(self, Formulas.Divide(self.Pokemon!.MaxHP, 8), $"{self.Name} is hurt by its Solar Power!");
    }
}

/// <summary>Rain washes its status condition away at each turn's end.</summary>
internal sealed class Hydration : BattleEffect
{
    public override void UnderTheWeather(IBattleContext ctx, Battler self, BattleWeather over)
    {
        var status = self.Pokemon!.Status;
        if (over == BattleWeather.Rain && status is not (StatusCondition.None or StatusCondition.Faint))
            ctx.CureStatus(self, $"{self.Name}'s Hydration cured its {BattleText.StatusName(status)}!");
    }
}

/// <summary>No status condition takes hold of it in the sun.</summary>
internal sealed class LeafGuard : BattleEffect
{
    public override bool BlocksStatus(Battler self, StatusCondition status) => self.Field?.WeatherInEffect == BattleWeather.Sun;
}

/// <summary>
/// Shadow Tag (anyone but another with Shadow Tag), Arena Trap (anyone on the ground), Magnet Pull (Steel types):
/// a foe can't be switched out or run while the holder is on the field.
/// </summary>
internal sealed class Trapper(string ability) : BattleEffect
{
    public override bool Traps(Battler self, Battler foe, bool foeOnTheGround) => ability switch
    {
        "Shadow Tag" => foe.Ability?.Name != "Shadow Tag",
        "Arena Trap" => foeOnTheGround,
        _ => foe.HasType(PokemonType.Steel)
    };
}
