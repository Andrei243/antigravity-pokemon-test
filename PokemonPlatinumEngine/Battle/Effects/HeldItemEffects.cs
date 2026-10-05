using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Effects;

/// <summary>
/// What held items and berries do in battle (plan 06 · R8), by the item table's <b>hold effect</b> (the name the
/// decompilation gives it: <c>HpRestoreGradual</c>, <c>PinchAtkUp</c>) and the number that goes with it
/// (<see cref="ItemData.HoldParam"/>: a Sitrus Berry's 25, Charcoal's 20), so every item that shares an effect
/// works, and an item added to the data works without a line here. An item is holdable when its hold effect has
/// an entry.
/// <para>
/// Not every effect is a hook: a bonus in the damage formula is a line of <see cref="DamageCalculator"/> in the
/// original's own place, Speed, the turn's order, accuracy, the critical-hit items, the berries against a type
/// and the items that answer a hit are lines of <see cref="BattleCore"/> where the original has them, and what
/// works only outside a battle (an Everstone, a Soothe Bell, a Cleanse Tag, an Amulet Coin's prize) is read by
/// the rule it belongs to. Those items have a <see cref="Plain"/> entry, which says what the rules may ask of it.
/// </para>
/// </summary>
public static class HeldItemEffects
{
    /// <summary>Hold effects the battle's own rules ask for by name, as <c>items.json</c> names them.</summary>
    public const string ExpShare = "ExpShare", ExpUp = "ExpUp", MoneyUp = "MoneyUp", EncountersDown = "EncountersDown";

    /// <summary>Each hold effect's code, made for the item that has it (its parameter).</summary>
    private static readonly Dictionary<string, Func<ItemData, BattleEffect>> ByHoldEffect = new()
    {
        // ---- Lines of the damage formula (DamageCalculator.Calculate, the original's block of items)
        ["StrengthenNormal"] = Plain, ["StrengthenFire"] = Plain, ["StrengthenWater"] = Plain, ["StrengthenElectric"] = Plain,
        ["StrengthenGrass"] = Plain, ["StrengthenIce"] = Plain, ["StrengthenFight"] = Plain, ["StrengthenPoison"] = Plain,
        ["StrengthenGround"] = Plain, ["StrengthenFlying"] = Plain, ["StrengthenPsychic"] = Plain, ["StrengthenBug"] = Plain,
        ["StrengthenRock"] = Plain, ["StrengthenGhost"] = Plain, ["StrengthenDragon"] = Plain, ["StrengthenDark"] = Plain,
        ["StrengthenSteel"] = Plain,
        ["ArceusFire"] = Plain, ["ArceusWater"] = Plain, ["ArceusElectric"] = Plain, ["ArceusGrass"] = Plain, ["ArceusIce"] = Plain,
        ["ArceusFighting"] = Plain, ["ArceusPoison"] = Plain, ["ArceusGround"] = Plain, ["ArceusFlying"] = Plain, ["ArceusPsychic"] = Plain,
        ["ArceusBug"] = Plain, ["ArceusRock"] = Plain, ["ArceusGhost"] = Plain, ["ArceusDragon"] = Plain, ["ArceusDark"] = Plain,
        ["ArceusSteel"] = Plain,
        ["ChoiceAtk"] = _ => new Plain { Locks = true },
        ["ChoiceSpatk"] = _ => new Plain { Locks = true },
        ["ChoiceSpeed"] = _ => new Plain { Locks = true },
        ["LatiSpecial"] = Plain, ["ClamperlSpatk"] = Plain, ["ClamperlSpdef"] = Plain, ["PikaSpatkUp"] = Plain,
        ["DittoDefUp"] = Plain, ["CuboneAtkUp"] = Plain, ["DialgaBoost"] = Plain, ["PalkiaBoost"] = Plain, ["GiratinaBoost"] = Plain,
        ["PowerUpPhys"] = Plain, ["PowerUpSpec"] = Plain, ["PowerUpSe"] = Plain, ["BoostRepeated"] = Plain,
        ["HpDrainOnAtk"] = i => new LifeOrb(),

        // ---- Lines of the core: critical hits, accuracy, Speed and the turn's order, a berry against a type, a hit answered
        ["CritrateUp"] = Plain, ["ChanseyCritrateUp"] = Plain, ["FarfetchdCritrateUp"] = Plain,
        ["AccReduce"] = Plain, ["AccuracyUp"] = Plain, ["AccuracyUpSlower"] = Plain,
        ["SometimesPriority"] = Plain, ["PinchPriority"] = Plain, ["PriorityDown"] = Plain,
        ["DittoSpeedUp"] = Plain, ["EvsUpSpeedDown"] = Plain,
        ["LvlupHpEvUp"] = Plain, ["LvlupAtkEvUp"] = Plain, ["LvlupDefEvUp"] = Plain, ["LvlupSpeedEvUp"] = Plain, ["LvlupSpatkEvUp"] = Plain, ["LvlupSpdefEvUp"] = Plain,
        ["SpeedDownGrounded"] = _ => new Plain { Grounds = true },
        ["WeakenNormal"] = Plain, ["WeakenSeFire"] = Plain, ["WeakenSeWater"] = Plain, ["WeakenSeElectric"] = Plain, ["WeakenSeGrass"] = Plain,
        ["WeakenSeIce"] = Plain, ["WeakenSeFight"] = Plain, ["WeakenSePoison"] = Plain, ["WeakenSeGround"] = Plain, ["WeakenSeFlying"] = Plain,
        ["WeakenSePsychic"] = Plain, ["WeakenSeBug"] = Plain, ["WeakenSeRock"] = Plain, ["WeakenSeGhost"] = Plain, ["WeakenSeDragon"] = Plain,
        ["WeakenSeDark"] = Plain, ["WeakenSeSteel"] = Plain,
        ["HpRestoreSe"] = Plain, ["RecoilPhysical"] = Plain, ["RecoilSpecial"] = Plain, ["ReciprocateInfat"] = Plain,

        // ---- Read by the rules outside the formula
        [ExpShare] = Plain, [ExpUp] = Plain, [MoneyUp] = Plain,
        ["Flee"] = _ => new Plain { Escapes = true },
        ["ExtendTrapping"] = _ => new Plain { Grips = true },
        ["ChargeSkip"] = _ => new Plain { ChargesAtOnce = true },
        ["Switch"] = _ => new Plain { Slips = true },
        ["LeechBoost"] = i => new Plain { Drains = 100 + i.HoldParam },
        ["ExtendScreens"] = i => new Lengthens("Screens", i.HoldParam),
        ["ExtendRain"] = i => new Lengthens("Rain", i.HoldParam),
        ["ExtendSun"] = i => new Lengthens("Sun", i.HoldParam),
        ["ExtendSandstorm"] = i => new Lengthens("Sandstorm", i.HoldParam),
        ["ExtendHail"] = i => new Lengthens("Hail", i.HoldParam),

        // ---- The field: evolution (Models/Evolution.cs), friendship (Models/Friendship.cs), wild Pokémon (Overworld/WildEncounterRules.cs)
        ["NoEvolve"] = Plain, ["EvolveSeadra"] = Plain, ["EvolvePorygon"] = Plain, ["EvolvePorygon2"] = Plain, ["EvolveRhydon"] = Plain,
        ["EvolveElectabuzz"] = Plain, ["EvolveMagmar"] = Plain, ["EvolveDusclops"] = Plain,
        ["FriendshipUp"] = Plain, [EncountersDown] = Plain,

        // ---- Hooks: the end of a turn
        ["HpRestoreGradual"] = _ => new Leftovers(),
        ["HpRestorePsnType"] = _ => new BlackSludge(),
        ["PsnUser"] = _ => new StatusOrb(StatusCondition.Toxic),
        ["BrnUser"] = _ => new StatusOrb(StatusCondition.Burn),
        ["DmgUserContactXfr"] = i => new StickyBarb(i.HoldParam),

        // ---- Hooks: after a move
        ["HpRestoreOnDmg"] = i => new ShellBell(i.HoldParam),
        ["SometimesFlinch"] = i => new FlinchItem(i.HoldParam),
        ["Endure"] = _ => new FocusSash(),
        ["MaybeEndure"] = i => new FocusBand(i.HoldParam),

        // ---- Berries and herbs, eaten when their holder's HP or condition calls for them
        ["HpRestore"] = i => new HealBerry(p => i.HoldParam),
        ["HpPctRestore"] = i => new HealBerry(p => Formulas.Divide(p.MaxHP * i.HoldParam, 100)),
        ["HpRestoreSpicy"] = i => new FlavorBerry(Flavor.Spicy, i.HoldParam),
        ["HpRestoreDry"] = i => new FlavorBerry(Flavor.Dry, i.HoldParam),
        ["HpRestoreSweet"] = i => new FlavorBerry(Flavor.Sweet, i.HoldParam),
        ["HpRestoreBitter"] = i => new FlavorBerry(Flavor.Bitter, i.HoldParam),
        ["HpRestoreSour"] = i => new FlavorBerry(Flavor.Sour, i.HoldParam),
        ["PrzRestore"] = _ => new CureBerry(StatusCondition.Paralyze),
        ["SlpRestore"] = _ => new CureBerry(StatusCondition.Sleep),
        ["PsnRestore"] = _ => new CureBerry(StatusCondition.Poison, StatusCondition.Toxic),
        ["BrnRestore"] = _ => new CureBerry(StatusCondition.Burn),
        ["FrzRestore"] = _ => new CureBerry(StatusCondition.Freeze),
        ["ConfuseRestore"] = _ => new CureBerry(curesConfusion: true),
        ["StatusRestore"] = _ => new CureBerry(curesConfusion: true, StatusCondition.Paralyze, StatusCondition.Sleep, StatusCondition.Poison,
            StatusCondition.Toxic, StatusCondition.Burn, StatusCondition.Freeze),
        ["PpRestore"] = i => new LeppaBerry(i.HoldParam),
        ["PinchAtkUp"] = i => new PinchStatBerry(StatType.Attack, i.HoldParam),
        ["PinchDefUp"] = i => new PinchStatBerry(StatType.Defense, i.HoldParam),
        ["PinchSpeedUp"] = i => new PinchStatBerry(StatType.Speed, i.HoldParam),
        ["PinchSpatkUp"] = i => new PinchStatBerry(StatType.SpAttack, i.HoldParam),
        ["PinchSpdefUp"] = i => new PinchStatBerry(StatType.SpDefense, i.HoldParam),
        ["PinchCritrateUp"] = i => new LansatBerry(i.HoldParam),
        ["PinchRandomUp"] = i => new StarfBerry(i.HoldParam),
        ["PinchAccUp"] = i => new MicleBerry(i.HoldParam),
        ["StatdownRestore"] = _ => new WhiteHerb(),
        ["HealInfatuation"] = _ => new MentalHerb(),
    };

    private static BattleEffect Plain(ItemData item) => new Plain();

    private static readonly ConditionalWeakTable<ItemData, BattleEffect> Made = new();

    /// <summary>The battle effect of a held item, or null if it does nothing while held.</summary>
    public static BattleEffect? For(ItemData? item)
    {
        if (item?.HoldEffect == null || !ByHoldEffect.TryGetValue(item.HoldEffect, out var make)) return null;
        return Made.GetValue(item, i => make(i));
    }

    public static bool IsHoldable(ItemData item) => item.HoldEffect != null && ByHoldEffect.ContainsKey(item.HoldEffect);

    /// <summary>The hold effects the engine runs.</summary>
    public static IEnumerable<string> HoldEffects => ByHoldEffect.Keys;

    /// <summary>Every item of the data that does something held.</summary>
    public static IEnumerable<string> Names => ItemDatabase.GetAll().Where(IsHoldable).Select(i => i.Name);

    /// <summary>The type a type-boosting item or a plate strengthens (<c>sTypeBoostingItems</c>), or null for any other effect.</summary>
    public static PokemonType? TypeBoostedBy(string holdEffect)
    {
        string? name = holdEffect.StartsWith("Strengthen") ? holdEffect["Strengthen".Length..] : holdEffect.StartsWith("Arceus") ? holdEffect["Arceus".Length..] : null;
        if (name == null) return null;
        if (name == "Fight") name = "Fighting";
        return Enum.TryParse(name, out PokemonType type) ? type : null;
    }

    /// <summary>
    /// The type a berry weakens (<c>subscript_type_resist_berry</c>): a super-effective hit of it, or any Normal hit
    /// for the Chilan Berry (<see cref="WeakensAnyNormalHit"/>). Null for any other effect.
    /// </summary>
    public static PokemonType? TypeWeakenedBy(string holdEffect)
    {
        if (holdEffect == "WeakenNormal") return PokemonType.Normal;
        if (!holdEffect.StartsWith("WeakenSe")) return null;
        string name = holdEffect["WeakenSe".Length..];
        if (name == "Fight") name = "Fighting";
        return Enum.TryParse(name, out PokemonType type) ? type : null;
    }

    public const string WeakensAnyNormalHit = "WeakenNormal";

    /// <summary>
    /// The items that halve their holder's Speed (<c>sSpeedHalvingItemEffects</c>): the Macho Brace, the Iron Ball
    /// and the six Power items. The original reads them from the item itself, so neither an Embargo nor Klutz undoes it.
    /// </summary>
    public static bool HalvesSpeed(string holdEffect) =>
        holdEffect is "EvsUpSpeedDown" or "SpeedDownGrounded" || holdEffect.StartsWith("Lvlup") && holdEffect.EndsWith("EvUp");

    /// <summary>The flavour a nature dislikes (<c>Pokemon_GetFlavorAffinityOf</c>): that of the stat it lowers. A neutral nature dislikes none.</summary>
    public static Flavor? DislikedBy(Nature nature) => nature switch
    {
        Nature.Bold or Nature.Timid or Nature.Modest or Nature.Calm => Flavor.Spicy,        // Attack lowered
        Nature.Lonely or Nature.Hasty or Nature.Mild or Nature.Gentle => Flavor.Sour,        // Defense lowered
        Nature.Brave or Nature.Relaxed or Nature.Quiet or Nature.Sassy => Flavor.Sweet,      // Speed lowered
        Nature.Adamant or Nature.Impish or Nature.Jolly or Nature.Careful => Flavor.Dry,     // Sp. Atk lowered
        Nature.Naughty or Nature.Lax or Nature.Naive or Nature.Rash => Flavor.Bitter,        // Sp. Def lowered
        _ => null
    };

    /// <summary>The flavour of a berry that heals by flavour (<c>HpRestoreSpicy</c>…), or null for any other effect.</summary>
    public static Flavor? FlavorOf(string holdEffect) => holdEffect switch
    {
        "HpRestoreSpicy" => Flavor.Spicy, "HpRestoreDry" => Flavor.Dry, "HpRestoreSweet" => Flavor.Sweet,
        "HpRestoreBitter" => Flavor.Bitter, "HpRestoreSour" => Flavor.Sour, _ => null
    };

    /// <summary>What a pinch berry's parameter comes to for its holder: Gluttony halves it, so the berry is eaten at half HP instead of a quarter.</summary>
    public static int PinchParam(Battler holder, int param) => holder.Ability?.Effect is { EatsBerriesEarly: true } ? Math.Max(1, param / 2) : Math.Max(1, param);
}

/// <summary>The five flavours of a berry, as a nature likes or dislikes them.</summary>
public enum Flavor { Spicy, Dry, Sweet, Bitter, Sour }

/// <summary>An item with no hook of its own: its rule is a line of the formula or the core, or outside the battle (see <see cref="HeldItemEffects"/>).</summary>
internal sealed class Plain : BattleEffect
{
    public bool Locks, Escapes, Grips, ChargesAtOnce, Slips, Grounds;
    public int Drains = 100;
    public override bool LocksMoveChoice => Locks;
    public override bool AlwaysEscapes => Escapes;
    public override bool BindsToTheEnd => Grips;
    public override bool SkipsChargeTurn => ChargesAtOnce;
    public override bool SlipsAway => Slips;
    public override bool GroundsHolder => Grounds;
    public override int DrainHundredths => Drains;
}

/// <summary>Light Clay and the four weather rocks: more turns of what their holder puts up (the item's parameter, three).</summary>
internal sealed class Lengthens(string what, int turns) : BattleEffect
{
    public override int ExtraTurns(string of) => of == what ? turns : 0;
}

/// <summary><c>BattleSystem_TriggerLeftovers</c>: a sixteenth back at the turn's end, below full HP.</summary>
internal sealed class Leftovers : BattleEffect
{
    public override void AtEndOfTurn(IBattleContext ctx, Battler self)
    {
        var p = self.Pokemon!;
        if (p.CurrentHP < p.MaxHP) ctx.RestoreHp(self, Formulas.Divide(p.MaxHP, 16), $"{self.Name} restored a little HP using its {p.HeldItem!.Name}!");
    }
}

/// <summary><c>BattleSystem_TriggerLeftovers</c>: a sixteenth back for a Poison type, an eighth taken from anyone else (not past Magic Guard).</summary>
internal sealed class BlackSludge : BattleEffect
{
    public override void AtEndOfTurn(IBattleContext ctx, Battler self)
    {
        var p = self.Pokemon!;
        if (self.HasType(PokemonType.Poison))
        {
            if (p.CurrentHP < p.MaxHP) ctx.RestoreHp(self, Formulas.Divide(p.MaxHP, 16), $"{self.Name} restored a little HP using its {p.HeldItem!.Name}!");
        }
        else ctx.LoseHp(self, Formulas.Divide(p.MaxHP, 8), $"{self.Name} is hurt by its {p.HeldItem!.Name}!");
    }
}

/// <summary><c>BattleSystem_TriggerHeldItemOnPivotMove</c>: what the move took, over the item's parameter (an eighth), back to its user.</summary>
internal sealed class ShellBell(int param) : BattleEffect
{
    public override void AfterAttacking(IBattleContext ctx, Battler self, Move move, IReadOnlyList<(Battler Target, int Damage)> hits)
    {
        int total = hits.Sum(h => h.Damage);
        if (total > 0 && self.IsActive && self.Pokemon!.CurrentHP < self.Pokemon.MaxHP)
            ctx.RestoreHp(self, Formulas.Divide(total, param), $"{self.Name} restored a little HP using its {self.Pokemon.HeldItem!.Name}!");
    }
}

/// <summary>
/// The Life Orb's price (<c>BattleSystem_TriggerHeldItemOnPivotMove</c>): a tenth of its holder's HP after a damaging
/// move that hit, unless Magic Guard. Its bonus is a line of the damage formula, before the roll.
/// </summary>
internal sealed class LifeOrb : BattleEffect
{
    public override void AfterAttacking(IBattleContext ctx, Battler self, Move move, IReadOnlyList<(Battler Target, int Damage)> hits)
    {
        if (hits.Any(h => h.Damage > 0) && self.IsActive)
            ctx.LoseHp(self, Formulas.Divide(self.Pokemon!.MaxHP, 10), $"{self.Name} lost some of its HP!");
    }
}

/// <summary>
/// King's Rock and Razor Fang (<c>BattleControllerPlayer_CheckExtraFlinch</c>): a damaging move that can carry
/// them (the table's flag) makes its target flinch, the item's parameter in a hundred.
/// </summary>
internal sealed class FlinchItem(int param) : BattleEffect
{
    public override void AfterAttacking(IBattleContext ctx, Battler self, Move move, IReadOnlyList<(Battler Target, int Damage)> hits)
    {
        if ((move.Data.Flags & MoveFlags.KingsRock) == 0) return;
        foreach (var (target, damage) in hits)
        {
            if (damage > 0 && target.IsActive && ctx.Random.Roll(RollKind.ItemChance, 100) < param) target.Flinched = true;
        }
    }
}

/// <summary>At full HP its holder hangs on with 1 HP, and the sash is gone (<c>HOLD_EFFECT_ENDURE</c>).</summary>
internal sealed class FocusSash : BattleEffect
{
    public override bool EnduresHit(IBattleContext ctx, Battler self, int damage)
    {
        var p = self.Pokemon!;
        if (p.CurrentHP != p.MaxHP || damage < p.CurrentHP) return false;
        ctx.Announce($"{self.Name} hung on using its {p.HeldItem!.Name}!");
        ctx.ConsumeItem(self);
        return true;
    }
}

/// <summary>Its holder hangs on with 1 HP, the item's parameter in a hundred (<c>HOLD_EFFECT_MAYBE_ENDURE</c>).</summary>
internal sealed class FocusBand(int param) : BattleEffect
{
    public override bool EnduresHit(IBattleContext ctx, Battler self, int damage)
    {
        if (damage < self.Pokemon!.CurrentHP || ctx.Random.Roll(RollKind.ItemChance, 100) >= param) return false;
        ctx.Announce($"{self.Name} hung on using its {self.Pokemon.HeldItem!.Name}!");
        return true;
    }
}

/// <summary>A Toxic Orb or Flame Orb (<c>BattleSystem_TriggerDetrimentalHeldItem</c>): the last thing of its holder's turn end, so its harm starts the turn after.</summary>
internal sealed class StatusOrb(StatusCondition status) : BattleEffect
{
    public override void AfterTheTurn(IBattleContext ctx, Battler self)
    {
        if (self.Pokemon!.Status == StatusCondition.None) ctx.TryInflictStatus(self, status, null);
    }
}

/// <summary>
/// The Sticky Barb's own harm (<c>BattleSystem_TriggerDetrimentalHeldItem</c>): an eighth of its holder's HP at the
/// turn's end, unless Magic Guard. Its move to whoever touches the holder is a line of the core (<c>ItemsOnHit</c>).
/// </summary>
internal sealed class StickyBarb(int param) : BattleEffect
{
    public override void AfterTheTurn(IBattleContext ctx, Battler self) =>
        ctx.LoseHp(self, Formulas.Divide(self.Pokemon!.MaxHP, param), $"{self.Name} is hurt by its {self.Pokemon.HeldItem!.Name}!");
}

/// <summary>Eaten at half HP or less to restore HP (<c>HpRestore</c>: the parameter; <c>HpPctRestore</c>: that share of its HP in a hundred).</summary>
internal sealed class HealBerry(Func<Pokemon, int> amount) : BattleEffect
{
    public override void OnConditionChanged(IBattleContext ctx, Battler self, Battler? cause)
    {
        var p = self.Pokemon!;
        if (p.IsFainted || p.CurrentHP > p.MaxHP / 2) return;
        string berry = p.HeldItem!.Name;
        ctx.ConsumeItem(self);
        ctx.RestoreHp(self, Math.Max(1, amount(p)), $"{self.Name} restored its health using its {berry}!");
    }
}

/// <summary>
/// Figy, Wiki, Mago, Aguav and Iapapa (<c>HpRestoreSpicy</c>…): at half HP or less its holder's HP over the parameter
/// (an eighth) back, and a Pokémon whose nature dislikes the flavour is confused by it (<c>subscript_held_item_dislike_flavor</c>).
/// </summary>
internal sealed class FlavorBerry(Flavor flavor, int param) : BattleEffect
{
    public override void OnConditionChanged(IBattleContext ctx, Battler self, Battler? cause)
    {
        var p = self.Pokemon!;
        if (p.IsFainted || p.CurrentHP > p.MaxHP / 2) return;
        string berry = p.HeldItem!.Name;
        ctx.ConsumeItem(self);
        ctx.RestoreHp(self, Formulas.Divide(p.MaxHP, Math.Max(1, param)), $"{self.Name} restored its health using its {berry}!");
        if (HeldItemEffects.DislikedBy(p.Nature) == flavor)
        {
            ctx.Announce($"For {self.Name}, the {berry} was too {flavor.ToString().ToLowerInvariant()}!");
            ctx.Confuse(self, null);
        }
    }
}

/// <summary>Eaten to cure a status condition (or confusion) as soon as its holder has it; a Lum Berry cures whichever it finds.</summary>
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

/// <summary><c>HOLD_EFFECT_PP_RESTORE</c>: the first of its holder's moves with no PP left gets the parameter back.</summary>
internal sealed class LeppaBerry(int param) : BattleEffect
{
    public override void OnConditionChanged(IBattleContext ctx, Battler self, Battler? cause)
    {
        var p = self.Pokemon!;
        if (p.IsFainted) return;
        var move = p.Moves.FirstOrDefault(m => m.CurrentPP == 0);
        if (move == null) return;
        string berry = p.HeldItem!.Name;
        ctx.ConsumeItem(self);
        move.CurrentPP = Math.Min(move.MaxPP, move.CurrentPP + param);
        ctx.Announce($"{self.Name}'s {berry} restored {move.Name}'s PP!");
    }
}

/// <summary>Liechi, Ganlon, Salac, Petaya, Apicot (<c>PinchAtkUp</c>…): at HP over the parameter (a quarter; half with Gluttony) or less, one stage of the stat, while it can still rise.</summary>
internal sealed class PinchStatBerry(StatType stat, int param) : BattleEffect
{
    public override void OnConditionChanged(IBattleContext ctx, Battler self, Battler? cause)
    {
        var p = self.Pokemon!;
        if (p.IsFainted || p.CurrentHP > p.MaxHP / HeldItemEffects.PinchParam(self, param) || p.StatStages.GetValueOrDefault(stat) >= 6) return;
        string berry = p.HeldItem!.Name;
        ctx.ConsumeItem(self);
        ctx.Announce($"{self.Name} ate its {berry}!");
        ctx.ChangeStat(self, stat, 1, self);
    }
}

/// <summary><c>HOLD_EFFECT_PINCH_CRITRATE_UP</c> (Lansat): the focus of Focus Energy, in a pinch, unless it already has it.</summary>
internal sealed class LansatBerry(int param) : BattleEffect
{
    public override void OnConditionChanged(IBattleContext ctx, Battler self, Battler? cause)
    {
        var p = self.Pokemon!;
        if (p.IsFainted || p.CurrentHP > p.MaxHP / HeldItemEffects.PinchParam(self, param) || self.Volatile.FocusEnergy) return;
        string berry = p.HeldItem!.Name;
        ctx.ConsumeItem(self);
        self.Volatile.FocusEnergy = true;
        ctx.Announce($"{self.Name} used its {berry} to get pumped!");
    }
}

/// <summary><c>HOLD_EFFECT_PINCH_RANDOM_UP</c> (Starf): one of the five stats that can still rise, drawn by lot, sharply raised in a pinch.</summary>
internal sealed class StarfBerry(int param) : BattleEffect
{
    private static readonly StatType[] Stats = { StatType.Attack, StatType.Defense, StatType.Speed, StatType.SpAttack, StatType.SpDefense };

    public override void OnConditionChanged(IBattleContext ctx, Battler self, Battler? cause)
    {
        var p = self.Pokemon!;
        if (p.IsFainted || p.CurrentHP > p.MaxHP / HeldItemEffects.PinchParam(self, param)) return;
        var open = Stats.Where(s => p.StatStages.GetValueOrDefault(s) < 6).ToList();
        if (open.Count == 0) return;
        string berry = p.HeldItem!.Name;
        ctx.ConsumeItem(self);
        ctx.Announce($"{self.Name} ate its {berry}!");
        ctx.ChangeStat(self, open[ctx.Random.Roll(RollKind.Pick, open.Count)], 2, self);
    }
}

/// <summary><c>HOLD_EFFECT_PINCH_ACC_UP</c> (Micle): its holder's next move is a fifth more accurate (the core reads and clears <see cref="Volatiles.MicleBerry"/>).</summary>
internal sealed class MicleBerry(int param) : BattleEffect
{
    public override void OnConditionChanged(IBattleContext ctx, Battler self, Battler? cause)
    {
        var p = self.Pokemon!;
        if (p.IsFainted || p.CurrentHP > p.MaxHP / HeldItemEffects.PinchParam(self, param)) return;
        string berry = p.HeldItem!.Name;
        ctx.ConsumeItem(self);
        self.Volatile.MicleBerry = true;
        ctx.Announce($"{self.Name} boosted the accuracy of its next move using its {berry}!");
    }
}

/// <summary><c>HOLD_EFFECT_STATDOWN_RESTORE</c>: every stage below its own back to it, as soon as one is lowered.</summary>
internal sealed class WhiteHerb : BattleEffect
{
    public override void OnConditionChanged(IBattleContext ctx, Battler self, Battler? cause)
    {
        var p = self.Pokemon!;
        if (p.IsFainted) return;
        var lowered = p.StatStages.Where(s => s.Value < 0).Select(s => s.Key).ToList();
        if (lowered.Count == 0) return;
        string herb = p.HeldItem!.Name;
        ctx.ConsumeItem(self);
        foreach (var stat in lowered) p.StatStages[stat] = 0;
        ctx.Announce($"{self.Name} returned its stats to normal using its {herb}!");
    }
}

/// <summary><c>HOLD_EFFECT_HEAL_INFATUATION</c>: its holder's love is over as soon as it falls into it.</summary>
internal sealed class MentalHerb : BattleEffect
{
    public override void OnConditionChanged(IBattleContext ctx, Battler self, Battler? cause)
    {
        var p = self.Pokemon!;
        if (p.IsFainted || self.Volatile.InLoveWith == null) return;
        string herb = p.HeldItem!.Name;
        ctx.ConsumeItem(self);
        self.Volatile.InLoveWith = null;
        ctx.Announce($"{self.Name} cured its infatuation using its {herb}!");
    }
}
