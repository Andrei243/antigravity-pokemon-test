using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Effects;

/// <summary>
/// What held items and berries do in battle, by item name (Generation 4 values). ItemDatabase lists the items
/// themselves; an item is holdable when it has an entry here.
/// </summary>
public static class HeldItemEffects
{
    private static readonly Dictionary<string, BattleEffect> Effects = new(StringComparer.OrdinalIgnoreCase)
    {
        // Recovery
        ["Leftovers"] = new Leftovers(),
        ["Black Sludge"] = new BlackSludge(),
        ["Shell Bell"] = new ShellBell(),

        // Damage
        ["Life Orb"] = new LifeOrb(),
        ["Expert Belt"] = new DamageItem((_, e) => e > 1f, 1.2f),
        ["Muscle Band"] = new PowerItem(m => m.Category == MoveCategory.Physical, 1.1f),
        ["Wise Glasses"] = new PowerItem(m => m.Category == MoveCategory.Special, 1.1f),
        ["Choice Band"] = new ChoiceItem(MoveCategory.Physical),
        ["Choice Specs"] = new ChoiceItem(MoveCategory.Special),
        ["Choice Scarf"] = new ChoiceItem(null),

        // Accuracy, critical hits, moving first, flinching
        ["Scope Lens"] = new CritItem(),
        ["Razor Claw"] = new CritItem(),
        ["Wide Lens"] = new AccuracyItem(1.1f),
        ["Bright Powder"] = new EvasionItem(0.9f),
        ["Quick Claw"] = new QuickClaw(),
        ["King's Rock"] = new FlinchItem(),
        ["Razor Fang"] = new FlinchItem(),

        // Surviving
        ["Focus Sash"] = new FocusSash(),
        ["Focus Band"] = new FocusBand(),

        // Orbs that give their holder a status at the end of a turn
        ["Flame Orb"] = new StatusOrb(StatusCondition.Burn),
        ["Toxic Orb"] = new StatusOrb(StatusCondition.Toxic),

        // Type boosters (1.2× in Generation 4)
        ["Silk Scarf"] = new PowerItem(m => m.Type == PokemonType.Normal, 1.2f),
        ["Charcoal"] = new PowerItem(m => m.Type == PokemonType.Fire, 1.2f),
        ["Mystic Water"] = new PowerItem(m => m.Type == PokemonType.Water, 1.2f),
        ["Miracle Seed"] = new PowerItem(m => m.Type == PokemonType.Grass, 1.2f),
        ["Magnet"] = new PowerItem(m => m.Type == PokemonType.Electric, 1.2f),
        ["Never-Melt Ice"] = new PowerItem(m => m.Type == PokemonType.Ice, 1.2f),
        ["Black Belt"] = new PowerItem(m => m.Type == PokemonType.Fighting, 1.2f),
        ["Poison Barb"] = new PowerItem(m => m.Type == PokemonType.Poison, 1.2f),
        ["Soft Sand"] = new PowerItem(m => m.Type == PokemonType.Ground, 1.2f),
        ["Sharp Beak"] = new PowerItem(m => m.Type == PokemonType.Flying, 1.2f),
        ["Twisted Spoon"] = new PowerItem(m => m.Type == PokemonType.Psychic, 1.2f),
        ["Silver Powder"] = new PowerItem(m => m.Type == PokemonType.Bug, 1.2f),
        ["Hard Stone"] = new PowerItem(m => m.Type == PokemonType.Rock, 1.2f),
        ["Spell Tag"] = new PowerItem(m => m.Type == PokemonType.Ghost, 1.2f),
        ["Dragon Fang"] = new PowerItem(m => m.Type == PokemonType.Dragon, 1.2f),
        ["Black Glasses"] = new PowerItem(m => m.Type == PokemonType.Dark, 1.2f),
        ["Metal Coat"] = new PowerItem(m => m.Type == PokemonType.Steel, 1.2f),

        // Berries: healing in a pinch
        ["Oran Berry"] = new HealBerry(_ => 10),
        ["Sitrus Berry"] = new HealBerry(p => p.MaxHP / 4),

        // Berries: curing a condition
        ["Cheri Berry"] = new CureBerry(StatusCondition.Paralyze),
        ["Chesto Berry"] = new CureBerry(StatusCondition.Sleep),
        ["Pecha Berry"] = new CureBerry(StatusCondition.Poison, StatusCondition.Toxic),
        ["Rawst Berry"] = new CureBerry(StatusCondition.Burn),
        ["Aspear Berry"] = new CureBerry(StatusCondition.Freeze),
        ["Persim Berry"] = new CureBerry(curesConfusion: true),
        ["Lum Berry"] = new CureBerry(curesConfusion: true, StatusCondition.Paralyze, StatusCondition.Sleep, StatusCondition.Poison,
            StatusCondition.Toxic, StatusCondition.Burn, StatusCondition.Freeze),

        // Berries: a stat boost at a quarter of HP
        ["Liechi Berry"] = new PinchStatBerry(StatType.Attack),
        ["Ganlon Berry"] = new PinchStatBerry(StatType.Defense),
        ["Salac Berry"] = new PinchStatBerry(StatType.Speed),
        ["Petaya Berry"] = new PinchStatBerry(StatType.SpAttack),
        ["Apicot Berry"] = new PinchStatBerry(StatType.SpDefense),

        // What the battle's own rules read off the holder: EXP and getting away
        ["Exp. Share"] = new ReadByTheRules(),
        ["Lucky Egg"] = new ReadByTheRules(),
        ["Smoke Ball"] = new ReadByTheRules { Escapes = true },

        // The field (plan 06 · R3): longer screens and weather, binding, charging, leaving, draining, weight
        ["Light Clay"] = new Lengthens("Screens"),
        ["Damp Rock"] = new Lengthens("Rain"),
        ["Heat Rock"] = new Lengthens("Sun"),
        ["Smooth Rock"] = new Lengthens("Sandstorm"),
        ["Icy Rock"] = new Lengthens("Hail"),
        ["Grip Claw"] = new ReadByTheRules { Grips = true },
        ["Power Herb"] = new ReadByTheRules { ChargesAtOnce = true },
        ["Shed Shell"] = new ReadByTheRules { Slips = true },
        ["Big Root"] = new ReadByTheRules { Drains = 130 },
        ["Iron Ball"] = new IronBall(),
    };

    /// <summary>Hold effects the battle's own rules ask for by name (the EXP a foe leaves), as <c>items.json</c> names them.</summary>
    public const string ExpShare = "ExpShare", ExpUp = "ExpUp";

    /// <summary>The battle effect of a held item, or null if it does nothing while held.</summary>
    public static BattleEffect? For(ItemData? item) => item != null && Effects.TryGetValue(item.Name, out var e) ? e : null;

    public static bool IsHoldable(ItemData item) => Effects.ContainsKey(item.Name);

    public static IEnumerable<string> Names => Effects.Keys;
}

internal sealed class Leftovers : BattleEffect
{
    public override void AtEndOfTurn(IBattleContext ctx, Battler self)
    {
        var p = self.Pokemon!;
        if (p.CurrentHP < p.MaxHP) ctx.RestoreHp(self, Math.Max(1, p.MaxHP / 16), $"{self.Name} restored a little HP using its Leftovers!");
    }
}

internal sealed class BlackSludge : BattleEffect
{
    public override void AtEndOfTurn(IBattleContext ctx, Battler self)
    {
        var p = self.Pokemon!;
        if (self.HasType(PokemonType.Poison))
        {
            if (p.CurrentHP < p.MaxHP) ctx.RestoreHp(self, Math.Max(1, p.MaxHP / 16), $"{self.Name} restored a little HP using its Black Sludge!");
        }
        else ctx.LoseHp(self, Math.Max(1, p.MaxHP / 8), $"{self.Name} is hurt by its Black Sludge!");
    }
}

internal sealed class ShellBell : BattleEffect
{
    public override void AfterAttacking(IBattleContext ctx, Battler self, Move move, IReadOnlyList<(Battler Target, int Damage)> hits)
    {
        int total = hits.Sum(h => h.Damage);
        if (total > 0 && self.IsActive && self.Pokemon!.CurrentHP < self.Pokemon.MaxHP)
            ctx.RestoreHp(self, Math.Max(1, total / 8), $"{self.Name} restored a little HP using its Shell Bell!");
    }
}

internal sealed class LifeOrb : BattleEffect
{
    // Before the roll and the types, where the original puts it
    public override float DamageBeforeTheRoll(Battler self, Move move) => 1.3f;

    public override void AfterAttacking(IBattleContext ctx, Battler self, Move move, IReadOnlyList<(Battler Target, int Damage)> hits)
    {
        if (hits.Any(h => h.Damage > 0) && self.IsActive)
            ctx.LoseHp(self, Math.Max(1, self.Pokemon!.MaxHP / 10), $"{self.Name} lost some of its HP!");
    }
}

internal sealed class PowerItem(Func<Move, bool> applies, float factor) : BattleEffect
{
    public override float PowerMultiplier(Battler self, Battler target, Move move) => applies(move) ? factor : 1f;
}

internal sealed class DamageItem(Func<Move, float, bool> applies, float factor) : BattleEffect
{
    public override float DamageMultiplier(Battler self, Battler target, Move move, float effectiveness) =>
        applies(move, effectiveness) ? factor : 1f;
}

/// <summary>Choice Band, Specs and Scarf: one stat ×1.5, but only the first move chosen can be used until it leaves.</summary>
internal sealed class ChoiceItem(MoveCategory? boosts) : BattleEffect
{
    public override bool LocksMoveChoice => true;
    public override float AttackMultiplier(Battler self, Move move) => boosts != null && move.Category == boosts ? 1.5f : 1f;
    public override float SpeedMultiplier(Battler self) => boosts == null ? 1.5f : 1f;
}

internal sealed class CritItem : BattleEffect
{
    public override int CritStageBonus => 1;
}

internal sealed class AccuracyItem(float factor) : BattleEffect
{
    public override float AccuracyMultiplier(Battler self, Move move) => factor;
}

internal sealed class EvasionItem(float factor) : BattleEffect
{
    public override float EvasionMultiplier(Battler self) => factor;
}

/// <summary>An item with no hook of its own: a rule of the battle asks whether it is held (Exp. Share, Lucky Egg).</summary>
internal sealed class ReadByTheRules : BattleEffect
{
    public bool Escapes, Grips, ChargesAtOnce, Slips;
    public int Drains = 100;
    public override bool AlwaysEscapes => Escapes;
    public override bool BindsToTheEnd => Grips;
    public override bool SkipsChargeTurn => ChargesAtOnce;
    public override bool SlipsAway => Slips;
    public override int DrainHundredths => Drains;
}

/// <summary>Light Clay and the four weather rocks: three more turns of what their holder puts up.</summary>
internal sealed class Lengthens(string what) : BattleEffect
{
    public override int ExtraTurns(string of) => of == what ? 3 : 0;
}

/// <summary>Halves its holder's Speed and keeps it on the ground, whatever its type or ability.</summary>
internal sealed class IronBall : BattleEffect
{
    public override bool GroundsHolder => true;
    public override float SpeedMultiplier(Battler self) => 0.5f;
}

internal sealed class QuickClaw : BattleEffect
{
    // One turn in five, as the original has it: the turn's number for the place leaves nothing over 5
    public override bool MovesFirstInBracket(int roll) => roll % 5 == 0;
}

/// <summary>King's Rock, Razor Fang: a 10% chance of flinching on damaging moves that don't already flinch.</summary>
internal sealed class FlinchItem : BattleEffect
{
    public override void AfterAttacking(IBattleContext ctx, Battler self, Move move, IReadOnlyList<(Battler Target, int Damage)> hits)
    {
        if (move.Data.FlinchChancePercent > 0) return;
        foreach (var (target, damage) in hits)
        {
            if (damage > 0 && target.IsActive && !target.MovedThisTurn && ctx.Random.Roll(RollKind.ItemChance, 100) < 10) target.Flinched = true;
        }
    }
}

internal sealed class FocusSash : BattleEffect
{
    public override bool EnduresHit(IBattleContext ctx, Battler self, int damage)
    {
        var p = self.Pokemon!;
        if (p.CurrentHP != p.MaxHP || damage < p.CurrentHP) return false;
        ctx.Announce($"{self.Name} hung on using its Focus Sash!");
        ctx.ConsumeItem(self);
        return true;
    }
}

internal sealed class FocusBand : BattleEffect
{
    public override bool EnduresHit(IBattleContext ctx, Battler self, int damage)
    {
        if (damage < self.Pokemon!.CurrentHP || ctx.Random.Roll(RollKind.ItemChance, 100) >= 10) return false;
        ctx.Announce($"{self.Name} hung on using its Focus Band!");
        return true;
    }
}

internal sealed class StatusOrb(StatusCondition status) : BattleEffect
{
    // After everything else of its turn end, as the original has it: the orb's harm starts the turn after
    public override void AfterTheTurn(IBattleContext ctx, Battler self)
    {
        if (self.Pokemon!.Status == StatusCondition.None) ctx.TryInflictStatus(self, status, null);
    }
}

/// <summary>Eaten to restore HP once its holder is at half HP or less.</summary>
internal sealed class HealBerry(Func<Pokemon, int> amount) : BattleEffect
{
    public override void OnConditionChanged(IBattleContext ctx, Battler self, Battler? cause)
    {
        var p = self.Pokemon!;
        if (p.IsFainted || p.CurrentHP * 2 > p.MaxHP) return;
        string berry = p.HeldItem!.Name;
        ctx.ConsumeItem(self);
        ctx.RestoreHp(self, Math.Max(1, amount(p)), $"{self.Name} restored its health using its {berry}!");
    }
}

/// <summary>Eaten to cure a status condition (or confusion) as soon as its holder gets it.</summary>
internal sealed class CureBerry(bool curesConfusion, params StatusCondition[] cures) : BattleEffect
{
    public CureBerry(params StatusCondition[] cures) : this(false, cures) { }

    public override void OnConditionChanged(IBattleContext ctx, Battler self, Battler? cause)
    {
        var p = self.Pokemon!;
        if (p.IsFainted) return;
        bool status = cures.Contains(p.Status);
        bool confusion = curesConfusion && self.IsConfused;
        if (!status && !confusion) return;

        string berry = p.HeldItem!.Name;
        ctx.ConsumeItem(self);
        if (confusion) self.ConfusionTurns = 0;
        if (status) ctx.CureStatus(self, $"{self.Name}'s {berry} cured its {BattleText.StatusName(p.Status)}!");
        else ctx.Announce($"{self.Name}'s {berry} snapped it out of its confusion!");
    }
}

internal sealed class PinchStatBerry(StatType stat) : BattleEffect
{
    public override void OnConditionChanged(IBattleContext ctx, Battler self, Battler? cause)
    {
        var p = self.Pokemon!;
        if (p.IsFainted || p.CurrentHP * 4 > p.MaxHP || p.StatStages.GetValueOrDefault(stat) >= 6) return;
        string berry = p.HeldItem!.Name;
        ctx.ConsumeItem(self);
        ctx.Announce($"{self.Name} ate its {berry}!");
        ctx.ChangeStat(self, stat, 1, self);
    }
}
