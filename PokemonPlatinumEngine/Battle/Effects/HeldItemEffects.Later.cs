using PokemonPlatinumEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Effects;

// The later games' held items (plan 06 · R28a), by Pokémon Showdown's data/items.ts at the pinned commit. The data
// importer gives each a hold effect of our own (tools/DataImporter/Importer.LaterItems.cs: the Showdown id written out,
// "WeaknessPolicy", "GemFire"), and the engine runs them by it as it runs Platinum's: a hook here where the item answers
// something by itself, a Plain entry where its rule is a line of the core (Battle/Sim/BattleCore.LaterItems.cs) or of
// the modern damage formula (Battle/DamageCalculator.Items.cs). docs/mechanics/rulings.md, "The later games' held
// items, medicine and balls", says where each is cut down.
public static partial class HeldItemEffects
{
    static HeldItemEffects()
    {
        foreach (var (hold, make) in Later) ByHoldEffect[hold] = make;
        foreach (var type in Enum.GetValues<PokemonType>())
        {
            ByHoldEffect[LaterItems.Gem + type] = Plain;
            ByHoldEffect[LaterItems.Memory + type] = Plain;
        }
        foreach (var type in new[] { "Water", "Electric", "Fire", "Ice" }) ByHoldEffect[LaterItems.Drive + type] = Plain;
    }

    private static readonly Dictionary<string, Func<ItemData, BattleEffect>> Later = new()
    {
        // ---- Lines of the modern damage formula (DamageCalculator.Items.cs)
        ["Eviolite"] = Plain, ["AssaultVest"] = Plain, ["PunchingGlove"] = Plain, ["UtilityUmbrella"] = Plain,
        ["ArceusFairy"] = Plain, ["StrengthenFairy"] = Plain,

        // ---- Lines of the core (BattleCore.LaterItems.cs and where each rule is)
        ["FloatStone"] = Plain, ["RingTarget"] = Plain, ["BindingBand"] = Plain, ["ProtectivePads"] = Plain,
        ["ClearAmulet"] = Plain, ["LoadedDice"] = Plain, ["HeavyDutyBoots"] = Plain, ["TerrainExtender"] = Plain,
        ["RedCard"] = Plain, ["EjectButton"] = Plain, ["EjectPack"] = Plain, ["RockyHelmet"] = Plain,
        ["WeaknessPolicy"] = Plain, ["AbsorbBulb"] = Plain, ["CellBattery"] = Plain, ["LuminousMoss"] = Plain, ["Snowball"] = Plain,
        ["KeeBerry"] = Plain, ["MarangaBerry"] = Plain, ["ThroatSpray"] = Plain, ["BlunderPolicy"] = Plain,
        ["MirrorHerb"] = Plain, ["AdrenalineOrb"] = Plain,
        // The Roseli Berry against a Fairy hit, as Platinum's berries against a type (WeakenedByBerry)
        ["WeakenSeFairy"] = Plain,

        // ---- Hooks
        ["AirBalloon"] = _ => new AirBalloon(),
        ["CovertCloak"] = _ => new CovertCloak(),
        ["SafetyGoggles"] = _ => new SafetyGoggles(),
        ["RoomService"] = _ => new RoomService(),
        ["BoosterEnergy"] = _ => new BoosterEnergy(),
        ["ElectricSeed"] = _ => new TerrainSeed(FieldCondition.ElectricTerrain, StatType.Defense),
        ["GrassySeed"] = _ => new TerrainSeed(FieldCondition.GrassyTerrain, StatType.Defense),
        ["MistySeed"] = _ => new TerrainSeed(FieldCondition.MistyTerrain, StatType.SpDefense),
        ["PsychicSeed"] = _ => new TerrainSeed(FieldCondition.PsychicTerrain, StatType.SpDefense),
    };
}

/// <summary>What the core and the formula ask of the later held items.</summary>
internal static class LaterItems
{
    /// <summary>The start of the hold effects named for a type: a Gem's, a Memory's (Multi-Attack), a Drive's (Techno Blast).</summary>
    public const string Gem = "Gem", Memory = "Memory", Drive = "Drive";

    /// <summary>The holder's item in force has this hold effect (none under an Embargo, with Klutz or in Magic Room).</summary>
    public static bool Holds(Battler b, string hold) => BattleEffects.HoldEffectOf(b) == hold;

    /// <summary>The type a hold effect that starts with <paramref name="prefix"/> is named for, or null.</summary>
    public static PokemonType? TypeOf(string? hold, string prefix) =>
        hold != null && hold.StartsWith(prefix, StringComparison.Ordinal) && Enum.TryParse(hold[prefix.Length..], out PokemonType type) ? type : null;

    /// <summary>
    /// The items that raise a stat when a hit of one type lands on their holder (Showdown's <c>onDamagingHit</c>):
    /// an Absorb Bulb's Sp. Atk and a Luminous Moss's Sp. Def against Water, a Cell Battery's Attack against
    /// Electric, a Snowball's Attack against Ice.
    /// </summary>
    public static (PokemonType Type, StatType Stat)? RaisedByAHit(string hold) => hold switch
    {
        "AbsorbBulb" => (PokemonType.Water, StatType.SpAttack),
        "LuminousMoss" => (PokemonType.Water, StatType.SpDefense),
        "CellBattery" => (PokemonType.Electric, StatType.Attack),
        "Snowball" => (PokemonType.Ice, StatType.Attack),
        _ => null
    };

    /// <summary>
    /// Adrenaline Orb (Showdown's <c>onAfterBoost</c>): its holder is a step faster once an Intimidate has tried its
    /// Attack, unless its Speed is as high as it goes or its Attack was as low already. Intimidate calls it.
    /// </summary>
    public static void Intimidated(IBattleContext ctx, Battler holder, int attackBefore)
    {
        if (!holder.IsActive || !Holds(holder, "AdrenalineOrb") || attackBefore <= -6) return;
        if (holder.Pokemon!.StatStages.GetValueOrDefault(StatType.Speed) >= 6) return;
        ctx.Announce(Text.Of("battle.usedItsItem", ("pokemon", holder.Name), ("item", ctx.ItemAtWork(holder))));
        ctx.ConsumeItem(holder);
        ctx.ChangeStat(holder, StatType.Speed, 1, holder);
    }

    /// <summary>One of its own items in force uses itself up to raise its holder's stat (a Seed, Room Service the other way).</summary>
    public static void GoesOff(IBattleContext ctx, Battler self, StatType stat, int stages)
    {
        ctx.Announce(Text.Of("battle.usedItsItem", ("pokemon", self.Name), ("item", ctx.ItemAtWork(self))));
        ctx.ConsumeItem(self);
        ctx.ChangeStat(self, stat, stages, self);
    }
}

/// <summary>
/// Air Balloon: its holder floats as it comes in, and says so (not under Gravity), out of reach of Ground moves and
/// of what lies on the ground, until a damaging hit pops it (the core: <c>LaterItemOnHit</c>).
/// </summary>
internal sealed class AirBalloon : BattleEffect
{
    public override void OnEntry(IBattleContext ctx, Battler self)
    {
        if (ctx.Field.Gravity) return;
        ctx.Announce(Text.Of("battle.floatsWithItsBalloon", ("pokemon", self.Name), ("item", ctx.ItemOf(self))));
    }

    public override bool Levitates => true;

    public override bool AbsorbsMove(IBattleContext ctx, Battler self, Battler attacker, Move move, float effectiveness)
    {
        if (move.Type != PokemonType.Ground || move.Category == MoveCategory.Status) return false;
        if (DamageCalculator.IsHeldDown(self) || DamageCalculator.HitsWhatFloats(move.Data)) return false;
        ctx.Announce(Text.Of("battle.itDoesntAffect", ("target", self.Name)));
        return true;
    }
}

/// <summary>Covert Cloak: the side effects of moves don't reach its holder (Showdown's <c>onModifySecondaries</c>, as Shield Dust).</summary>
internal sealed class CovertCloak : BattleEffect
{
    public override bool BlocksSideEffects => true;
}

/// <summary>Safety Goggles: no harm from a sandstorm or hail; the powders are the core's (<c>Arrives</c>).</summary>
internal sealed class SafetyGoggles : BattleEffect
{
    public override bool ShelteredFrom(BattleWeather weather) => weather is BattleWeather.Sandstorm or BattleWeather.Hail;
}

/// <summary>Room Service: a step slower once Trick Room is up, as it comes in under it or as the room is twisted (the core calls it then).</summary>
internal sealed class RoomService : BattleEffect
{
    public override void OnEntry(IBattleContext ctx, Battler self)
    {
        if (ctx.Field.TrickRoom) LaterItems.GoesOff(ctx, self, StatType.Speed, -1);
    }
}

/// <summary>A terrain's Seed: a stage of Defense (Electric, Grassy) or Sp. Def (Misty, Psychic) as it comes in on its terrain or the terrain comes.</summary>
internal sealed class TerrainSeed(FieldCondition terrain, StatType stat) : BattleEffect
{
    public override void OnEntry(IBattleContext ctx, Battler self) => Check(ctx, self);

    public override void OnTerrainChange(IBattleContext ctx, Battler self) => Check(ctx, self);

    private void Check(IBattleContext ctx, Battler self)
    {
        if (!self.IsActive || ctx.Field.Terrain != terrain || !ctx.Field.Has(terrain)) return;
        LaterItems.GoesOff(ctx, self, stat, 1);
    }
}

/// <summary>
/// Booster Energy: Protosynthesis or Quark Drive without the sun or the terrain, for as long as its holder stays in
/// (Showdown's <c>onUpdate</c>, as it comes in; <see cref="Volatiles.BoosterEnergy"/>).
/// </summary>
internal sealed class BoosterEnergy : BattleEffect
{
    public override void OnEntry(IBattleContext ctx, Battler self)
    {
        if (self.Ability?.Name is not ("Protosynthesis" or "Quark Drive") || self.Volatile.BoosterEnergy || LaterAbilities.ParadoxStat(self) != null) return;
        string item = ctx.ItemAtWork(self);
        ctx.ConsumeItem(self);
        self.Volatile.BoosterEnergy = true;
        var stat = LaterAbilities.ParadoxStat(self) ?? StatType.Attack;
        ctx.Announce(Text.Of("battle.boosterEnergy", ("pokemon", self.Name), ("item", item), ("ability", ctx.AbilityOf(self)), ("stat", BattleText.StatName(stat))));
    }
}
