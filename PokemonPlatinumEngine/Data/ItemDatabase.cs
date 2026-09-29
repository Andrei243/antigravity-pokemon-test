using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Data;

public static class ItemDatabase
{
    private static readonly Dictionary<string, ItemData> Items = new(StringComparer.OrdinalIgnoreCase);

    public static void Initialize() { }

    static ItemDatabase()
    {
        // Poké Balls
        Register(new ItemData
        {
            Id = 1,
            Name = "Poké Ball",
            Pocket = ItemPocket.PokeBalls,
            EffectType = ItemEffectType.CatchPokemon,
            EffectValue = 10, // 1.0x
            Price = 200,
            CanUseInOverworld = false,
            Description = "A device for catching wild Pokémon. It is thrown like a ball at the target."
        });

        Register(new ItemData
        {
            Id = 2,
            Name = "Great Ball",
            Pocket = ItemPocket.PokeBalls,
            EffectType = ItemEffectType.CatchPokemon,
            EffectValue = 15, // 1.5x
            Price = 600,
            CanUseInOverworld = false,
            Description = "A good, high-performance Ball that provides a higher Pokémon catch rate than a standard Poké Ball."
        });

        Register(new ItemData
        {
            Id = 3,
            Name = "Ultra Ball",
            Pocket = ItemPocket.PokeBalls,
            EffectType = ItemEffectType.CatchPokemon,
            EffectValue = 20, // 2.0x
            Price = 1200,
            CanUseInOverworld = false,
            Description = "An ultra-performance Ball that provides a higher Pokémon catch rate than a Great Ball."
        });

        Register(new ItemData
        {
            Id = 4,
            Name = "Master Ball",
            Pocket = ItemPocket.PokeBalls,
            EffectType = ItemEffectType.CatchPokemon,
            EffectValue = 9999, // Guaranteed
            Price = 0,
            CanUseInOverworld = false,
            Description = "The best Ball with the ultimate level of performance. It will catch any wild Pokémon without fail."
        });

        // Medicine
        Register(new ItemData
        {
            Id = 10,
            Name = "Potion",
            Pocket = ItemPocket.Medicine,
            EffectType = ItemEffectType.HealHP,
            EffectValue = 20,
            Price = 300,
            Description = "A spray-type medicine for treating wounds. It can be used to restore 20 HP to an injured Pokémon."
        });

        Register(new ItemData
        {
            Id = 11,
            Name = "Super Potion",
            Pocket = ItemPocket.Medicine,
            EffectType = ItemEffectType.HealHP,
            EffectValue = 50,
            Price = 700,
            Description = "A spray-type medicine for treating wounds. It can be used to restore 50 HP to an injured Pokémon."
        });

        Register(new ItemData
        {
            Id = 12,
            Name = "Hyper Potion",
            Pocket = ItemPocket.Medicine,
            EffectType = ItemEffectType.HealHP,
            EffectValue = 200,
            Price = 1200,
            Description = "A spray-type medicine for treating wounds. It can be used to restore 200 HP to an injured Pokémon."
        });

        Register(new ItemData
        {
            Id = 13,
            Name = "Max Potion",
            Pocket = ItemPocket.Medicine,
            EffectType = ItemEffectType.HealHP,
            EffectValue = 9999,
            Price = 2500,
            Description = "A spray-type medicine for treating wounds. It can be used to completely restore the max HP of a single Pokémon."
        });

        Register(new ItemData
        {
            Id = 14,
            Name = "Full Restore",
            Pocket = ItemPocket.Medicine,
            EffectType = ItemEffectType.FullRestore,
            EffectValue = 9999,
            Price = 3000,
            Description = "A medicine that fully restores the HP and heals any status problems of a single Pokémon."
        });

        Register(new ItemData
        {
            Id = 15,
            Name = "Revive",
            Pocket = ItemPocket.Medicine,
            EffectType = ItemEffectType.Revive,
            EffectValue = 50, // 50% max HP
            Price = 1500,
            Description = "A medicine that can revive Pokémon that have fainted. It also restores half of a Pokémon's maximum HP."
        });

        Register(new ItemData
        {
            Id = 16,
            Name = "Antidote",
            Pocket = ItemPocket.Medicine,
            EffectType = ItemEffectType.HealStatus,
            HealsStatus = StatusCondition.Poison,
            Price = 100,
            Description = "A spray-type medicine. It can be used to cure a single Pokémon from poisoning."
        });

        Register(new ItemData
        {
            Id = 17,
            Name = "Paralyze Heal",
            Pocket = ItemPocket.Medicine,
            EffectType = ItemEffectType.HealStatus,
            HealsStatus = StatusCondition.Paralyze,
            Price = 200,
            Description = "A spray-type medicine. It can be used to cure a single Pokémon from paralysis."
        });

        Register(new ItemData
        {
            Id = 18,
            Name = "Awakening",
            Pocket = ItemPocket.Medicine,
            EffectType = ItemEffectType.HealStatus,
            HealsStatus = StatusCondition.Sleep,
            Price = 250,
            Description = "A spray-type medicine. It can be used to rouse a single Pokémon from the clutches of sleep."
        });

        // General Items
        Register(new ItemData
        {
            Id = 30,
            Name = "Rare Candy",
            Pocket = ItemPocket.Items,
            EffectType = ItemEffectType.LevelUp,
            EffectValue = 1,
            Price = 4800,
            CanUseInBattle = false,
            Description = "A candy that is packed with energy. When consumed, it raises the level of a single Pokémon by one."
        });

        Register(new ItemData
        {
            Id = 31,
            Name = "Repel",
            Pocket = ItemPocket.Items,
            EffectType = ItemEffectType.None,
            Price = 350,
            CanUseInBattle = false,
            Description = "An item that prevents weak wild Pokémon from appearing for 100 steps."
        });

        // Key Items
        Register(new ItemData
        {
            Id = 50,
            Name = "Town Map",
            Pocket = ItemPocket.KeyItems,
            EffectType = ItemEffectType.KeyItem,
            Price = 0,
            CanUseInBattle = false,
            Description = "A very convenient map that can be viewed anytime. It even shows your present location in Sinnoh."
        });

        Register(new ItemData
        {
            Id = 51,
            Name = "Running Shoes",
            Pocket = ItemPocket.KeyItems,
            EffectType = ItemEffectType.KeyItem,
            Price = 0,
            CanUseInBattle = false,
            Description = "Iconic running shoes! Hold the B / X button or toggle running mode to sprint at high speed."
        });

        Register(new ItemData
        {
            Id = 52,
            Name = "Distortion Orb",
            Pocket = ItemPocket.KeyItems,
            EffectType = ItemEffectType.KeyItem,
            Price = 0,
            CanUseInBattle = false,
            Description = "A mysterious glowing orb that resonates with the Distortion World and legendary power."
        });
    }

    private static void Register(ItemData item)
    {
        Items[item.Name] = item;
    }

    public static ItemData? Get(string name)
    {
        Items.TryGetValue(name, out var item);
        return item;
    }

    public static IEnumerable<ItemData> GetAll() => Items.Values;
}
