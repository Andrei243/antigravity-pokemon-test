using PokemonPlatinumEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Effects;

// The abilities of Generations 5 to 9 (plan 06 · R27), each by Pokémon Showdown's rule (data/abilities.ts at the
// pinned commit). As with Platinum's, a bonus inside the damage formula has no code here: DamageCalculator.Abilities
// applies it by name in the modern formula's chains. What the core decides for itself (the turn's order, a move's type,
// a move stopped before it lands, a stat lowered by another) is BattleCore.Abilities.cs, also by name.

/// <summary>Moves of one kind are taken in and raise a stat instead of hurting (Sap Sipper, Lightning Rod and Storm Drain by the later rules, Well-Baked Body, Wind Rider).</summary>
internal sealed class AbsorbRaises(Func<Move, bool> takes, StatType stat, int stages, PokemonType? draws = null) : BattleEffect
{
    public override bool DrawsMovesOf(PokemonType type) => draws == type;

    public override bool AbsorbsMove(IBattleContext ctx, Battler self, Battler attacker, Move move, float effectiveness)
    {
        if (attacker == self || !takes(move)) return false;
        // Lightning Rod and Storm Drain only draw the move under Platinum's rules (generation 5 made them immunities)
        if (draws != null && !ctx.Rules.ModernDamageFormula) return false;
        if (!ctx.ChangeStat(self, stat, stages, self))
            ctx.Announce(Text.Of("battle.madeUseless", ("pokemon", self.Name), ("ability", ctx.AbilityOf(self)), ("move", move.DisplayName)));
        return true;
    }
}

/// <summary>Earth Eater: Ground moves heal a quarter of its HP instead of hurting it.</summary>
internal sealed class EarthEater : BattleEffect
{
    private readonly AbsorbType ground = new(PokemonType.Ground);

    public override bool AbsorbsMove(IBattleContext ctx, Battler self, Battler attacker, Move move, float effectiveness) =>
        ground.AbsorbsMove(ctx, self, attacker, move, effectiveness);
}

/// <summary>Moves of one kind don't touch it at all (Bulletproof: balls and bombs; Overcoat: powders, and the weather's sting).</summary>
internal sealed class Proof(MoveFlags flag, bool weatherProof = false) : BattleEffect
{
    public override bool AbsorbsMove(IBattleContext ctx, Battler self, Battler attacker, Move move, float effectiveness)
    {
        if (attacker == self || (move.Data.Flags & flag) == 0) return false;
        ctx.Announce(Text.Of("battle.blocks", ("pokemon", self.Name), ("ability", ctx.AbilityOf(self)), ("move", move.DisplayName)));
        return true;
    }

    public override bool ShelteredFrom(BattleWeather weather) => weatherProof && weather is BattleWeather.Sandstorm or BattleWeather.Hail;
}

/// <summary>Sand Rush, Slush Rush, Surge Surfer: twice as fast in their weather or on their terrain (and the sand doesn't sting a Sand Rush).</summary>
internal sealed class FieldSpeed(BattleWeather? weather, FieldCondition? terrain = null) : BattleEffect
{
    public override float SpeedMultiplier(Battler self)
    {
        var field = self.Field;
        if (field == null) return 1f;
        if (weather is { } w && (field.WeatherInEffect == w || (w == BattleWeather.Snow && field.WeatherInEffect == BattleWeather.Hail))) return 2f;
        if (terrain is { } t && field.Has(t)) return 2f;
        return 1f;
    }

    public override bool ShelteredFrom(BattleWeather from) => weather == BattleWeather.Sandstorm && from == BattleWeather.Sandstorm;
}

/// <summary>Sand Force: the sand doesn't sting it (its bonus is in the damage formula by name).</summary>
internal sealed class SandForce : BattleEffect
{
    public override bool ShelteredFrom(BattleWeather weather) => weather == BattleWeather.Sandstorm;
}

/// <summary>Contrary: a stat change on it goes the other way.</summary>
internal sealed class Contrary : BattleEffect
{
    public override int ScaleStatChange(int amount) => -amount;
}

/// <summary>Mind's Eye: Normal and Fighting moves hit Ghost types, and its accuracy can't be lowered.</summary>
internal sealed class MindsEye : BattleEffect
{
    public override bool HitsGhosts => true;
    public override bool BlocksStatDrop(Battler self, StatType stat) => stat == StatType.Accuracy;
}

/// <summary>A status condition never takes hold of it, perhaps only while it is in a shape (Purifying Salt, Shields Down's shell).</summary>
internal sealed class NoStatus(Func<Battler, StatusCondition, bool> blocks) : BattleEffect
{
    public override bool BlocksStatus(Battler self, StatusCondition status) => blocks(self, status);
}

/// <summary>Gorilla Tactics: held to the first move it uses, as a Choice item holds it (its Attack's bonus is in the damage formula by name).</summary>
internal sealed class GorillaTactics : BattleEffect
{
    public override bool LocksMoveChoice => true;
}

/// <summary>An ability that carries a line as its holder comes in, and is otherwise read by name (Turboblaze, Teravolt, the auras, the Ruin abilities).</summary>
internal sealed class Announced(string key, bool breaksAbilities = false) : BattleEffect
{
    public override bool IgnoresTargetAbility => breaksAbilities;

    public override void OnEntry(IBattleContext ctx, Battler self) =>
        ctx.Announce(Text.Of(key, ("pokemon", self.Name), ("ability", ctx.AbilityOf(self))));
}

/// <summary>Electric, Grassy, Misty and Psychic Surge: the terrain comes in with the holder.</summary>
internal sealed class Surge(FieldCondition terrain) : BattleEffect
{
    public override void OnEntry(IBattleContext ctx, Battler self) => ctx.SetTerrain(terrain, self);
}

/// <summary>Orichalcum Pulse and Hadron Engine: the sun or Electric Terrain comes in with the holder (the Attack's or Sp. Atk's bonus under it is in the damage formula by name).</summary>
internal sealed class Pulse(bool sun) : BattleEffect
{
    public override void OnEntry(IBattleContext ctx, Battler self)
    {
        if (sun)
        {
            int turns = ctx.Rules.AbilityWeatherTurns;
            if (turns > 0) turns += BattleEffects.Of(self).Sum(e => e.ExtraTurns("Sun"));
            ctx.SetWeather(BattleWeather.Sun, turns, Text.Of("battle.orichalcumPulse", ("pokemon", self.Name), ("ability", ctx.AbilityOf(self))));
        }
        else ctx.SetTerrain(FieldCondition.ElectricTerrain, self, Text.Of("battle.hadronEngine", ("pokemon", self.Name), ("ability", ctx.AbilityOf(self))));
    }
}

/// <summary>
/// Protosynthesis and Quark Drive: in the sun or on Electric Terrain its best stat (by its stage, Showdown's
/// <c>getBestStat(false, true)</c>) is raised, by 5325/4096 or the Speed by half, in the damage formula and the turn's
/// order by name; it says so as it comes in under them.
/// </summary>
internal sealed class Paradox(bool sun) : BattleEffect
{
    public override float SpeedMultiplier(Battler self) => LaterAbilities.ParadoxStat(self) == StatType.Speed ? 1.5f : 1f;

    public override void OnEntry(IBattleContext ctx, Battler self)
    {
        if (LaterAbilities.ParadoxStat(self) is { } stat)
            ctx.Announce(Text.Of(sun ? "battle.protosynthesis" : "battle.quarkDrive", ("pokemon", self.Name), ("ability", ctx.AbilityOf(self)), ("stat", BattleText.StatName(stat))));
    }

    /// <summary>The terrain came or went: the stat is fixed as it takes hold, or let go (plan 06 · R27b).</summary>
    public override void OnTerrainChange(IBattleContext ctx, Battler self) => LaterAbilities.ParadoxStat(self);
}

/// <summary>Intrepid Sword, Dauntless Shield, Supersweet Syrup: once a battle, as the holder first comes in (Showdown's generation 9 <c>swordBoost</c>, <c>shieldBoost</c>, <c>syrupTriggered</c>).</summary>
internal sealed class OnceABattle(StatType stat, int stages, bool onFoes = false) : BattleEffect
{
    public override void OnEntry(IBattleContext ctx, Battler self)
    {
        if (ctx is not BattleCore core || !core.FirstTimeThisBattle(self)) return;
        if (!onFoes)
        {
            ctx.ChangeStat(self, stat, stages, self);
            return;
        }
        ctx.Announce(Text.Of("battle.supersweetSyrup", ("pokemon", self.Name), ("ability", ctx.AbilityOf(self))));
        foreach (var foe in ctx.ActiveFoes(self).ToList()) ctx.ChangeStat(foe, stat, stages, self, announceFailure: true);
    }
}

/// <summary>Regenerator: a third of its HP back as it leaves the field (said nothing of, as Showdown says nothing).</summary>
internal sealed class Regenerator : BattleEffect
{
    public override void OnWithdraw(Battler self)
    {
        var p = self.Pokemon!;
        if (p.CurrentHP > 0) p.CurrentHP = Math.Min(p.MaxHP, p.CurrentHP + p.MaxHP / 3);
    }
}

/// <summary>A stat raised when a move of one kind hits it (Justified, Rattled, Stamina, Water Compaction, Steam Engine, Thermal Exchange).</summary>
internal sealed class HitRaises(Func<Move, bool> by, StatType stat, int stages, bool burnProof = false) : BattleEffect
{
    public override void AfterHit(IBattleContext ctx, Battler self, Battler attacker, Move move, int damage, bool critical)
    {
        if (!self.IsActive || attacker == self || move.Category == MoveCategory.Status || !by(move)) return;
        ctx.ChangeStat(self, stat, stages, self);
    }

    public override bool BlocksStatus(Battler self, StatusCondition status) => burnProof && status == StatusCondition.Burn;
}

/// <summary>Weak Armor: a physical hit lowers its Defense a stage and raises its Speed two (generation 7 on).</summary>
internal sealed class WeakArmor : BattleEffect
{
    public override void AfterHit(IBattleContext ctx, Battler self, Battler attacker, Move move, int damage, bool critical)
    {
        if (!self.IsActive || move.Category != MoveCategory.Physical) return;
        ctx.ChangeStat(self, StatType.Defense, -1, self);
        ctx.ChangeStat(self, StatType.Speed, 2, self);
    }
}

/// <summary>Berserk and Anger Shell: a hit that takes it from over half its HP to half or under (Showdown's <c>checkShouldTrigger</c>).</summary>
internal sealed class HalfwayAnger(bool shell) : BattleEffect
{
    public override void AfterHit(IBattleContext ctx, Battler self, Battler attacker, Move move, int damage, bool critical)
    {
        var p = self.Pokemon!;
        if (!self.IsActive || attacker == self || (p.CurrentHP + damage) * 2 <= p.MaxHP || p.CurrentHP * 2 > p.MaxHP) return;
        if (!shell)
        {
            ctx.ChangeStat(self, StatType.SpAttack, 1, self);
            return;
        }
        foreach (var down in new[] { StatType.Defense, StatType.SpDefense }) ctx.ChangeStat(self, down, -1, self);
        foreach (var up in new[] { StatType.Attack, StatType.SpAttack, StatType.Speed }) ctx.ChangeStat(self, up, 1, self);
    }
}

/// <summary>Electromorphosis, Wind Power: being hit (by a wind move, for Wind Power) charges it, so its next Electric move is twice as strong.</summary>
internal sealed class Charged(bool windOnly) : BattleEffect
{
    public override void AfterHit(IBattleContext ctx, Battler self, Battler attacker, Move move, int damage, bool critical)
    {
        if (!self.IsActive || (windOnly && !LaterAbilities.IsWind(move.Data))) return;
        self.Volatile.ChargeTurns = 2;
        ctx.Announce(Text.Of("battle.chargedWithPower", ("pokemon", self.Name), ("ability", ctx.AbilityOf(self))));
    }
}

/// <summary>Gooey, Tangling Hair: whoever touches it loses a stage of Speed.</summary>
internal sealed class Slows : BattleEffect
{
    public override void AfterHit(IBattleContext ctx, Battler self, Battler attacker, Move move, int damage, bool critical)
    {
        if (!move.Data.MakesContact || !attacker.IsActive || attacker == self) return;
        ctx.ChangeStat(attacker, StatType.Speed, -1, self);
    }
}

/// <summary>Cotton Down: hit by anything, it lowers the Speed of everyone else on the field a stage.</summary>
internal sealed class CottonDown : BattleEffect
{
    public override void AfterHit(IBattleContext ctx, Battler self, Battler attacker, Move move, int damage, bool critical)
    {
        if (attacker == self) return;
        ctx.Announce(Text.Of("battle.cottonDown", ("pokemon", self.Name), ("ability", ctx.AbilityOf(self))));
        foreach (var other in ctx.Everyone.Where(b => b != self).ToList()) ctx.ChangeStat(other, StatType.Speed, -1, self);
    }
}

/// <summary>Sand Spit and Seed Sower: being hit brings a sandstorm or Grassy Terrain.</summary>
internal sealed class HitBrings(bool sand) : BattleEffect
{
    public override void AfterHit(IBattleContext ctx, Battler self, Battler attacker, Move move, int damage, bool critical)
    {
        if (attacker == self) return;
        if (!sand)
        {
            ctx.SetTerrain(FieldCondition.GrassyTerrain, self);
            return;
        }
        int turns = 5 + BattleEffects.Of(self).Sum(e => e.ExtraTurns("Sandstorm"));
        ctx.SetWeather(BattleWeather.Sandstorm, turns, Text.Of("battle.sandSpit", ("pokemon", self.Name), ("ability", ctx.AbilityOf(self))));
    }
}

/// <summary>Spicy Spray: whoever damages it with a move is burned.</summary>
internal sealed class SpicySpray : BattleEffect
{
    public override void AfterHit(IBattleContext ctx, Battler self, Battler attacker, Move move, int damage, bool critical)
    {
        if (!attacker.IsActive || attacker == self || attacker.Pokemon!.Status != StatusCondition.None) return;
        ctx.TryInflictStatus(attacker, StatusCondition.Burn, self);
    }
}

/// <summary>Cursed Body: three times in ten the move that hit it is disabled.</summary>
internal sealed class CursedBody : BattleEffect
{
    public override void AfterHit(IBattleContext ctx, Battler self, Battler attacker, Move move, int damage, bool critical)
    {
        if (!attacker.IsActive || attacker == self || attacker.Volatile.Disabled != null || move.Data == BattleCore.StruggleData) return;
        var known = attacker.Pokemon!.Moves.FirstOrDefault(m => m.Data.Name == move.Data.Name);
        if (known == null || ctx.Random.Roll(RollKind.AbilityChance, 100) >= 30) return;
        attacker.Volatile.Disabled = known.Data;
        attacker.Volatile.DisableTurns = 4;
        ctx.Announce(Text.Of("battle.cursedBody", ("target", attacker.Name), ("move", known.DisplayName), ("pokemon", self.Name), ("ability", ctx.AbilityOf(self))));
    }
}

/// <summary>Mummy, Lingering Aroma, Wandering Spirit: whoever touches it has its ability changed, or swapped for its own (Showdown's <c>cantsuppress</c> and <c>failskillswap</c> abilities keep theirs).</summary>
internal sealed class Catching(bool swaps) : BattleEffect
{
    public override void AfterHit(IBattleContext ctx, Battler self, Battler attacker, Move move, int damage, bool critical)
    {
        if (!move.Data.MakesContact || !attacker.IsActive || attacker == self || ctx is not BattleCore core) return;
        core.CatchAbility(self, attacker, swaps);
    }
}

/// <summary>Perish Body: whoever touches it, and it, faint in three turns unless they leave.</summary>
internal sealed class PerishBody : BattleEffect
{
    public override void AfterHit(IBattleContext ctx, Battler self, Battler attacker, Move move, int damage, bool critical)
    {
        if (!move.Data.MakesContact || !attacker.IsActive || attacker == self || attacker.Volatile.PerishCount >= 0) return;
        attacker.Volatile.PerishCount = 3;
        if (self.IsActive && self.Volatile.PerishCount < 0) self.Volatile.PerishCount = 3;
        ctx.Announce(Text.Of("battle.perishBody", ("pokemon", self.Name), ("ability", ctx.AbilityOf(self))));
    }
}

/// <summary>Pickpocket: whoever touches it while it has no item has theirs taken.</summary>
internal sealed class Pickpocket : BattleEffect
{
    public override void AfterHit(IBattleContext ctx, Battler self, Battler attacker, Move move, int damage, bool critical)
    {
        if (!move.Data.MakesContact || !self.IsActive || attacker == self || ctx is not BattleCore core) return;
        core.TakeItem(self, attacker);
    }
}

/// <summary>Innards Out: knocked out, it hurts whoever did it by the HP it had left.</summary>
internal sealed class InnardsOut : BattleEffect
{
    public override void AfterHit(IBattleContext ctx, Battler self, Battler attacker, Move move, int damage, bool critical)
    {
        if (self.Pokemon!.CurrentHP > 0 || !attacker.IsActive || attacker == self) return;
        ctx.LoseHp(attacker, damage, Text.Of("battle.isHurtBy2", ("attacker", attacker.Name), ("pokemon", self.Name), ("ability", ctx.AbilityOf(self))));
    }
}

/// <summary>Toxic Debris: a physical hit scatters a layer of Toxic Spikes round the attacker's side.</summary>
internal sealed class ToxicDebris : BattleEffect
{
    public override void AfterHit(IBattleContext ctx, Battler self, Battler attacker, Move move, int damage, bool critical)
    {
        if (move.Category != MoveCategory.Physical || attacker == self || attacker.Side == self.Side || ctx is not BattleCore core) return;
        core.ScatterToxicSpikes(self, attacker.Side);
    }
}

/// <summary>Poison Touch, Toxic Chain: three times in ten a target it hits is poisoned (by a touch) or badly poisoned (by anything).</summary>
internal sealed class Poisoner(bool chain) : BattleEffect
{
    public override void AfterAttacking(IBattleContext ctx, Battler self, Move move, IReadOnlyList<(Battler Target, int Damage)> hits)
    {
        if (!chain && !move.Data.MakesContact) return;
        foreach (var (target, damage) in hits)
        {
            if (damage <= 0 || !target.IsActive || target == self || target.Pokemon!.Status != StatusCondition.None) continue;
            // Shield Dust keeps it off (plan 06 · R27b's audit; Showdown's poisontouch onSourceDamagingHit)
            if (target.Ability?.Name == "Shield Dust") continue;
            if (ctx.Random.Roll(RollKind.AbilityChance, 100) >= 30) continue;
            ctx.TryInflictStatus(target, chain ? StatusCondition.Toxic : StatusCondition.Poison, self);
        }
    }
}

/// <summary>Magician: a hit takes its target's item, if its user has none.</summary>
internal sealed class Magician : BattleEffect
{
    public override void AfterAttacking(IBattleContext ctx, Battler self, Move move, IReadOnlyList<(Battler Target, int Damage)> hits)
    {
        if (ctx is not BattleCore core) return;
        foreach (var (target, damage) in hits)
            if (damage > 0 && target.IsActive && target != self && core.TakeItem(self, target)) return;
    }
}

/// <summary>Moxie, Chilling Neigh, Grim Neigh, Beast Boost, Eelevate: a stat raised for each Pokémon its move knocks out (the best one, unstaged, for Beast Boost and Eelevate).</summary>
internal sealed class KnockOutRaises(StatType? stat, bool floats = false) : BattleEffect
{
    private readonly Levitate levitate = new();

    public override void AfterAttacking(IBattleContext ctx, Battler self, Move move, IReadOnlyList<(Battler Target, int Damage)> hits)
    {
        foreach (var (target, _) in hits)
        {
            if (!self.IsActive || target == self || target.Pokemon is not { IsFainted: true }) continue;
            ctx.ChangeStat(self, stat ?? LaterAbilities.BestStat(self.Pokemon!), 1, self);
        }
    }

    public override bool Levitates => floats;

    public override bool AbsorbsMove(IBattleContext ctx, Battler self, Battler attacker, Move move, float effectiveness) =>
        floats && levitate.AbsorbsMove(ctx, self, attacker, move, effectiveness);
}

/// <summary>Moody: at each turn's end a stat it can raise goes up two stages and another it can lower down one (generation 8 on: neither accuracy nor evasion).</summary>
internal sealed class Moody : BattleEffect
{
    private static readonly StatType[] Stats = { StatType.Attack, StatType.Defense, StatType.SpAttack, StatType.SpDefense, StatType.Speed };

    public override void AtEndOfTurn(IBattleContext ctx, Battler self)
    {
        var stages = self.Pokemon!.StatStages;
        var up = Stats.Where(s => stages.GetValueOrDefault(s) < 6).ToList();
        StatType? raised = up.Count == 0 ? null : up[ctx.Random.Roll(RollKind.AbilityPick, up.Count)];
        var down = Stats.Where(s => s != raised && stages.GetValueOrDefault(s) > -6).ToList();
        if (raised is { } r) ctx.ChangeStat(self, r, 2, self);
        if (down.Count > 0) ctx.ChangeStat(self, down[ctx.Random.Roll(RollKind.AbilityPick, down.Count)], -1, self);
    }
}

/// <summary>Harvest: at each turn's end a berry it ate grows back one time in two, every time under the sun.</summary>
internal sealed class Harvest : BattleEffect
{
    public override void AtEndOfTurn(IBattleContext ctx, Battler self)
    {
        var p = self.Pokemon!;
        if (p.HeldItem != null || self.Volatile.ConsumedItem is not { Pocket: ItemPocket.Berries } berry) return;
        bool sunny = ctx.Field.WeatherInEffect is BattleWeather.Sun or BattleWeather.HarshSun;
        if (!sunny && ctx.Random.Roll(RollKind.AbilityChance, 2) != 0) return;
        p.HeldItem = berry;
        self.Volatile.ConsumedItem = null;
        ctx.Announce(Text.Of("battle.harvested", ("pokemon", self.Name), ("item", ctx.ItemOf(self))));
    }
}

/// <summary>Healer: at each turn's end, three times in ten, an ally's condition is cured.</summary>
internal sealed class Healer : BattleEffect
{
    public override void AtEndOfTurn(IBattleContext ctx, Battler self)
    {
        foreach (var ally in ctx.Everyone.Where(b => b != self && b.Side == self.Side).ToList())
        {
            var status = ally.Pokemon!.Status;
            if (status is StatusCondition.None or StatusCondition.Faint || ctx.Random.Roll(RollKind.AbilityChance, 100) >= 30) continue;
            ctx.CureStatus(ally, Text.Of("battle.curedIts", ("pokemon", ally.Name), ("item", ctx.AbilityOf(self)), ("condition", BattleText.StatusName(status))));
        }
    }
}

/// <summary>Mimicry: on a terrain it takes the terrain's type, and its own back when the terrain goes.</summary>
internal sealed class Mimicry : BattleEffect
{
    public override void OnEntry(IBattleContext ctx, Battler self) => OnTerrainChange(ctx, self);

    public override void OnTerrainChange(IBattleContext ctx, Battler self)
    {
        if (!self.IsActive || self.Pokemon!.Terastallized != null) return;
        PokemonType? type = ctx.Field.Terrain switch
        {
            FieldCondition.ElectricTerrain => PokemonType.Electric,
            FieldCondition.GrassyTerrain => PokemonType.Grass,
            FieldCondition.MistyTerrain => PokemonType.Fairy,
            FieldCondition.PsychicTerrain => PokemonType.Psychic,
            _ => null
        };
        if (type is { } t)
        {
            if (self.Volatile.Types is [var only] && only == t) return;
            self.Volatile.Types = new List<PokemonType> { t };
            ctx.Announce(Text.Of("battle.transformedIntoTheType", ("user", self.Name), ("pick", t)));
        }
        else if (self.Volatile.Types != null)
        {
            self.Volatile.Types = null;
            ctx.Announce(Text.Of("battle.returnedToItsTypes", ("pokemon", self.Name)));
        }
    }
}

/// <summary>Screen Cleaner: as it comes in, every Reflect, Light Screen and Aurora Veil on both sides is gone.</summary>
internal sealed class ScreenCleaner : BattleEffect
{
    public override void OnEntry(IBattleContext ctx, Battler self)
    {
        if (ctx is BattleCore core) core.CleanTheScreens(self);
    }
}

/// <summary>Curious Medicine: as it comes in, its allies' stat changes are gone.</summary>
internal sealed class CuriousMedicine : BattleEffect
{
    public override void OnEntry(IBattleContext ctx, Battler self)
    {
        foreach (var ally in ctx.Everyone.Where(b => b != self && b.Side == self.Side).ToList())
        {
            if (ally.Pokemon!.StatStages.Values.All(v => v == 0)) continue;
            if (ctx is BattleCore core) core.ClearStages(ally, Text.Of("battle.curiousMedicine", ("pokemon", ally.Name)));
        }
    }
}

/// <summary>Costar: as it comes in, it takes its ally's stat changes (and its critical-hit stages).</summary>
internal sealed class Costar : BattleEffect
{
    public override void OnEntry(IBattleContext ctx, Battler self)
    {
        var ally = ctx.Everyone.FirstOrDefault(b => b != self && b.Side == self.Side);
        if (ally == null || ctx is not BattleCore core) return;
        core.CopyStages(self, ally, Text.Of("battle.costar", ("pokemon", self.Name), ("ally", ally.Name)));
    }
}

/// <summary>Hospitality: as it comes in, its ally gets a quarter of its HP back.</summary>
internal sealed class Hospitality : BattleEffect
{
    public override void OnEntry(IBattleContext ctx, Battler self)
    {
        foreach (var ally in ctx.Everyone.Where(b => b != self && b.Side == self.Side).ToList())
            if (ally.Pokemon!.CurrentHP < ally.Pokemon.MaxHP)
                ctx.RestoreHp(ally, Formulas.Divide(ally.Pokemon.MaxHP, 4), Text.Of("battle.hospitality", ("pokemon", ally.Name)));
    }
}

/// <summary>Guard Dog: Roar, Whirlwind and their kin can't move it (an Intimidate raising its Attack is the core's, <c>GuardDogStands</c>).</summary>
internal sealed class GuardDog : BattleEffect
{
    public override bool HoldsItsGround => true;
}

/// <summary>Comatose: it is always half asleep, so no status condition takes hold of it; it says so as it comes in.</summary>
internal sealed class Comatose : BattleEffect
{
    public override bool BlocksStatus(Battler self, StatusCondition status) => status != StatusCondition.None;

    public override void OnEntry(IBattleContext ctx, Battler self) => ctx.Announce(Text.Of("battle.comatose", ("pokemon", self.Name)));
}

/// <summary>What the later abilities read of moves and stats: Showdown's lists of slicing, wind and healing moves, Beast Boost's stat and the paradox Pokémon's.</summary>
internal static partial class LaterAbilities
{
    /// <summary>Showdown's slicing moves (Sharpness).</summary>
    public static readonly HashSet<string> Slicing = new()
    {
        "Aerial Ace", "Air Cutter", "Air Slash", "Aqua Cutter", "Behemoth Blade", "Bitter Blade", "Ceaseless Edge", "Cross Poison", "Cut", "Fury Cutter",
        "Kowtow Cleave", "Leaf Blade", "Mighty Cleave", "Night Slash", "Population Bomb", "Psyblade", "Psycho Cut", "Razor Leaf", "Razor Shell",
        "Sacred Sword", "Secret Sword", "Slash", "Solar Blade", "Stone Axe", "Tachyon Cutter", "X-Scissor"
    };

    /// <summary>Showdown's wind moves (Wind Rider, Wind Power).</summary>
    public static readonly HashSet<string> Wind = new()
    {
        "Aeroblast", "Air Cutter", "Bleakwind Storm", "Blizzard", "Fairy Wind", "Gust", "Heat Wave", "Hurricane", "Icy Wind", "Petal Blizzard",
        "Sandsear Storm", "Sandstorm", "Springtide Storm", "Tailwind", "Twister", "Whirlwind", "Wildbolt Storm"
    };

    /// <summary>Showdown's healing moves (Triage).</summary>
    public static readonly HashSet<string> Healing = new()
    {
        "Absorb", "Bitter Blade", "Bouncy Bubble", "Draining Kiss", "Drain Punch", "Dream Eater", "Floral Healing", "Giga Drain", "Healing Wish",
        "Heal Order", "Heal Pulse", "Horn Leech", "Jungle Healing", "Leech Life", "Life Dew", "Lunar Blessing", "Lunar Dance", "Matcha Gotcha",
        "Mega Drain", "Milk Drink", "Moonlight", "Morning Sun", "Oblivion Wing", "Parabolic Charge", "Purify", "Recover", "Rest", "Revival Blessing",
        "Roost", "Shore Up", "Slack Off", "Soft-Boiled", "Strength Sap", "Swallow", "Synthesis", "Wish"
    };

    public static bool IsWind(MoveData move) => Wind.Contains(move.Name);

    /// <summary>The stat Beast Boost raises: the highest unstaged, Attack first among equals (Showdown's <c>getBestStat(true, true)</c>).</summary>
    public static StatType BestStat(Pokemon p)
    {
        var best = StatType.Attack;
        int value = p.Attack;
        foreach (var (stat, v) in new[] { (StatType.Defense, p.Defense), (StatType.SpAttack, p.SpAttack), (StatType.SpDefense, p.SpDefense), (StatType.Speed, p.Speed) })
            if (v > value)
            {
                best = stat;
                value = v;
            }
        return best;
    }

    /// <summary>
    /// The stat Protosynthesis or Quark Drive raises now, or null while it isn't in force (no sun, no Electric Terrain).
    /// The stat is the best by its stage as the ability takes hold, and stays that one while it holds (Showdown's
    /// <c>bestStat</c>, worked out in the condition's onStart; plan 06 · R27b's audit: R27 worked it out afresh at every read).
    /// </summary>
    public static StatType? ParadoxStat(Battler b)
    {
        if (b.Pokemon is not { } p || b.Field is not { } field) return null;
        string? ability = b.Ability?.Name;
        bool on = ability switch
        {
            "Protosynthesis" => field.WeatherInEffect is BattleWeather.Sun or BattleWeather.HarshSun,
            "Quark Drive" => field.Has(FieldCondition.ElectricTerrain),
            _ => false
        };
        if (!on)
        {
            b.Volatile.ParadoxBest = null;
            return null;
        }
        return b.Volatile.ParadoxBest ??= BestStaged(p);
    }

    /// <summary>The highest stat by its stage, Attack first among equals (Showdown's <c>getBestStat(false, true)</c>).</summary>
    private static StatType BestStaged(Pokemon p)
    {
        var best = StatType.Attack;
        int value = Formulas.Staged(p.Attack, p.StatStages.GetValueOrDefault(StatType.Attack));
        foreach (var (stat, raw) in new[] { (StatType.Defense, p.Defense), (StatType.SpAttack, p.SpAttack), (StatType.SpDefense, p.SpDefense), (StatType.Speed, p.Speed) })
        {
            int v = Formulas.Staged(raw, p.StatStages.GetValueOrDefault(stat));
            if (v > value)
            {
                best = stat;
                value = v;
            }
        }
        return best;
    }
}
