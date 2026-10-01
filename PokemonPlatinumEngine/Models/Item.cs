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
