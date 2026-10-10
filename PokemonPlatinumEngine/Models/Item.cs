using System.Collections.Generic;
using System.Text.Json.Serialization;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

public enum ItemEffectType
{
    None,
    HealHP,
    Revive,
    HealStatus,
    FullRestore,
    CatchPokemon,
    LevelUp,
    KeyItem
}

public class ItemData
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ItemPocket Pocket { get; set; }
    public ItemEffectType EffectType { get; set; }
    public int EffectValue { get; set; } // e.g. 20 HP, 50 HP, or Ball catch multiplier * 10
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public StatusCondition HealsStatus { get; set; } = StatusCondition.None;
    public int Price { get; set; } = 200;
    public bool CanUseInBattle { get; set; } = true;
    public bool CanUseInOverworld { get; set; } = true;
    public string Description { get; set; } = string.Empty;

    /// <summary>What it does when held, as the decompilation names it (<c>HpRestoreGradual</c>); null for none.
    /// The ones the battle engine runs are in <c>Battle/Effects/HeldItemEffects.cs</c>.</summary>
    public string? HoldEffect { get; set; }

    /// <summary>The move a TM or HM teaches.</summary>
    public string? TeachesMove { get; set; }

    // ---- The rest of Platinum's item table (plan 06 · R1), with the decompilation's own names. Nothing reads
    // these yet: they are what the sessions on held items (R8) and on the bag (R11) write their rules from.

    /// <summary>The number that goes with the hold effect: a Sitrus Berry's 25 (per cent of HP), Charcoal's 20 (per cent more power).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int HoldParam { get; set; }

    /// <summary>Fling's power with this item (0: it can't be thrown), and what else the throw does (<c>Flinch</c>, <c>Burn</c>).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int FlingPower { get; set; }
    public string? FlingEffect { get; set; }

    /// <summary>Natural Gift's power and type with this berry.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int NaturalGiftPower { get; set; }
    public PokemonType? NaturalGiftType { get; set; }

    /// <summary>What a Pokémon that eats it off its holder gets (Pluck, Bug Bite).</summary>
    public string? PluckEffect { get; set; }

    /// <summary>It can't be thrown away; it can be registered to a button.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool CantBeTossed { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool CanBeRegistered { get; set; }

    /// <summary>What using it from the bag does outside a battle (<c>Healing</c>, <c>TmHm</c>, <c>EvoStone</c>, <c>OldRod</c>, <c>Bicycle</c>), and in one (<c>Healing</c>, <c>PokeBall</c>, <c>Escaping</c>).</summary>
    public string? FieldUse { get; set; }
    public string? BattleUse { get; set; }

    /// <summary>
    /// Where the battle's bag shows it (plan 06 · R11), as the original's battle pocket mask names it:
    /// <c>RecoverHp</c>, <c>RecoverPp</c>, <c>RecoverStatus</c>, <c>RecoverHpStatus</c> (in both of the first
    /// two tabs), <c>PokeBalls</c>, <c>BattleItems</c>; null for an item the battle's bag never shows.
    /// </summary>
    public string? BattlePocket { get; set; }

    /// <summary>
    /// The item's own number when it holds nothing (a held item's is <see cref="HoldParam"/>): the steps a Repel
    /// lasts, a flute's share of the encounter rate.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int EffectParam { get; set; }

    /// <summary>
    /// What using it changes: <c>hpRestored</c> 20, <c>healPoison</c> 1, <c>atkEVs</c> 10, <c>friendshipLow</c> 5…
    /// A flag is 1; <c>hpRestored</c> and <c>ppRestored</c> use the original's codes below zero (all, half, a quarter).
    /// </summary>
    public Dictionary<string, int>? Use { get; set; }

    /// <summary>A berry's growing and its taste (plan 06 · R14a), from the original's <c>berryData</c>; null for anything else.</summary>
    public BerryData? Berry { get; set; }

    // ---- The later mechanics' items

    /// <summary>A Mega Stone: the species it belongs to and the form it brings out.</summary>
    public MegaStone? MegaStone { get; set; }

    /// <summary>A Z-Crystal: the type it is for, or the one species' move it turns into a Z-Move of its own.</summary>
    public ZCrystal? ZCrystal { get; set; }
}

/// <summary>
/// What a berry is as a plant and as food (the original's <c>BerryData</c>): how long each stage of its growing takes
/// in hours, how much water its soil loses an hour, how many berries a plant gives before its rating, its size in
/// millimetres and firmness, and its five flavours and smoothness, which Poffins are made from (plan 06 · R14c).
/// </summary>
public class BerryData
{
    public int Size { get; set; }
    public string Firmness { get; set; } = string.Empty;
    public int BaseYield { get; set; }
    public int StageDuration { get; set; }
    public int MoistureDrainRate { get; set; }
    public int Spiciness { get; set; }
    public int Dryness { get; set; }
    public int Sweetness { get; set; }
    public int Bitterness { get; set; }
    public int Sourness { get; set; }
    public int Smoothness { get; set; }
}

public class MegaStone
{
    public string Species { get; set; } = string.Empty;
    public string Form { get; set; } = string.Empty;

    /// <summary>The one form of the species the stone works for, when it isn't any of them (Floette's Eternal Flower).</summary>
    public string? HeldByForm { get; set; }
}

public class ZCrystal
{
    /// <summary>A type's crystal: every damaging move of the type becomes that type's Z-Move.</summary>
    public PokemonType? Type { get; set; }

    /// <summary>One species' crystal: the Z-Move, the move it is made from, and who can use it.</summary>
    public string? Move { get; set; }
    public string? From { get; set; }
    public List<string>? Users { get; set; }
}

public class ItemStack
{
    public ItemData Data { get; }
    public int Quantity { get; set; }

    public string Name => Data.Name;
    public ItemPocket Pocket => Data.Pocket;

    public ItemStack(ItemData data, int quantity = 1)
    {
        Data = data;
        Quantity = quantity;
    }
}
