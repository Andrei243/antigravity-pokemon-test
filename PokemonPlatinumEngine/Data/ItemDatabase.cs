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

        // Items to hold in battle; what they do is in Battle/Effects/HeldItemEffects.cs
        HeldItem(200, "Leftovers", ItemPocket.Items, 200, "Held: restores a little HP at the end of every turn.");
        HeldItem(201, "Black Sludge", ItemPocket.Items, 200, "Held: restores a little HP each turn for Poison types, and hurts anyone else.");
        HeldItem(202, "Shell Bell", ItemPocket.Items, 200, "Held: restores HP by an eighth of the damage its holder deals.");
        HeldItem(203, "Life Orb", ItemPocket.Items, 200, "Held: boosts the power of moves, but every attack costs the holder some HP.");
        HeldItem(204, "Expert Belt", ItemPocket.Items, 200, "Held: super-effective moves hit a little harder.");
        HeldItem(205, "Muscle Band", ItemPocket.Items, 200, "Held: physical moves hit a little harder.");
        HeldItem(206, "Wise Glasses", ItemPocket.Items, 200, "Held: special moves hit a little harder.");
        HeldItem(207, "Choice Band", ItemPocket.Items, 200, "Held: boosts Attack, but the holder can only use the first move it picks.");
        HeldItem(208, "Choice Specs", ItemPocket.Items, 200, "Held: boosts Sp. Atk, but the holder can only use the first move it picks.");
        HeldItem(209, "Choice Scarf", ItemPocket.Items, 200, "Held: boosts Speed, but the holder can only use the first move it picks.");
        HeldItem(210, "Scope Lens", ItemPocket.Items, 200, "Held: raises the holder's critical-hit ratio.");
        HeldItem(211, "Razor Claw", ItemPocket.Items, 2100, "Held: raises the holder's critical-hit ratio.");
        HeldItem(212, "Wide Lens", ItemPocket.Items, 200, "Held: raises the accuracy of the holder's moves a little.");
        HeldItem(213, "Bright Powder", ItemPocket.Items, 10, "Held: its glare makes the holder a little harder to hit.");
        HeldItem(214, "Quick Claw", ItemPocket.Items, 100, "Held: sometimes lets the holder move first.");
        HeldItem(215, "King's Rock", ItemPocket.Items, 100, "Held: the holder's attacks may make the target flinch.");
        HeldItem(216, "Razor Fang", ItemPocket.Items, 2100, "Held: the holder's attacks may make the target flinch.");
        HeldItem(217, "Focus Sash", ItemPocket.Items, 200, "Held: at full HP, the holder survives a knockout blow with 1 HP. Used up once.");
        HeldItem(218, "Focus Band", ItemPocket.Items, 200, "Held: the holder may hang on with 1 HP when it would faint.");
        HeldItem(219, "Flame Orb", ItemPocket.Items, 200, "Held: burns its holder at the end of a turn.");
        HeldItem(220, "Toxic Orb", ItemPocket.Items, 200, "Held: badly poisons its holder at the end of a turn.");
        HeldItem(221, "Silk Scarf", ItemPocket.Items, 100, "Held: powers up Normal-type moves.");
        HeldItem(222, "Charcoal", ItemPocket.Items, 9800, "Held: powers up Fire-type moves.");
        HeldItem(223, "Mystic Water", ItemPocket.Items, 100, "Held: powers up Water-type moves.");
        HeldItem(224, "Miracle Seed", ItemPocket.Items, 100, "Held: powers up Grass-type moves.");
        HeldItem(225, "Magnet", ItemPocket.Items, 100, "Held: powers up Electric-type moves.");
        HeldItem(226, "Never-Melt Ice", ItemPocket.Items, 100, "Held: powers up Ice-type moves.");
        HeldItem(227, "Black Belt", ItemPocket.Items, 100, "Held: powers up Fighting-type moves.");
        HeldItem(228, "Poison Barb", ItemPocket.Items, 100, "Held: powers up Poison-type moves.");
        HeldItem(229, "Soft Sand", ItemPocket.Items, 100, "Held: powers up Ground-type moves.");
        HeldItem(230, "Sharp Beak", ItemPocket.Items, 100, "Held: powers up Flying-type moves.");
        HeldItem(231, "Twisted Spoon", ItemPocket.Items, 100, "Held: powers up Psychic-type moves.");
        HeldItem(232, "Silver Powder", ItemPocket.Items, 100, "Held: powers up Bug-type moves.");
        HeldItem(233, "Hard Stone", ItemPocket.Items, 100, "Held: powers up Rock-type moves.");
        HeldItem(234, "Spell Tag", ItemPocket.Items, 100, "Held: powers up Ghost-type moves.");
        HeldItem(235, "Dragon Fang", ItemPocket.Items, 100, "Held: powers up Dragon-type moves.");
        HeldItem(236, "Black Glasses", ItemPocket.Items, 100, "Held: powers up Dark-type moves.");
        HeldItem(237, "Metal Coat", ItemPocket.Items, 100, "Held: powers up Steel-type moves.");
        HeldItem(238, "Oran Berry", ItemPocket.Berries, 20, "Held: eaten at half HP or less to restore 10 HP.");
        HeldItem(239, "Sitrus Berry", ItemPocket.Berries, 20, "Held: eaten at half HP or less to restore a quarter of max HP.");
        HeldItem(240, "Cheri Berry", ItemPocket.Berries, 20, "Held: eaten to cure paralysis.");
        HeldItem(241, "Chesto Berry", ItemPocket.Berries, 20, "Held: eaten to wake up from sleep.");
        HeldItem(242, "Pecha Berry", ItemPocket.Berries, 20, "Held: eaten to cure poison.");
        HeldItem(243, "Rawst Berry", ItemPocket.Berries, 20, "Held: eaten to heal a burn.");
        HeldItem(244, "Aspear Berry", ItemPocket.Berries, 20, "Held: eaten to thaw out.");
        HeldItem(245, "Persim Berry", ItemPocket.Berries, 20, "Held: eaten to snap out of confusion.");
        HeldItem(246, "Lum Berry", ItemPocket.Berries, 20, "Held: eaten to cure any status problem or confusion.");
        HeldItem(247, "Liechi Berry", ItemPocket.Berries, 20, "Held: eaten at a quarter of HP to raise Attack.");
        HeldItem(248, "Ganlon Berry", ItemPocket.Berries, 20, "Held: eaten at a quarter of HP to raise Defense.");
        HeldItem(249, "Salac Berry", ItemPocket.Berries, 20, "Held: eaten at a quarter of HP to raise Speed.");
        HeldItem(250, "Petaya Berry", ItemPocket.Berries, 20, "Held: eaten at a quarter of HP to raise Sp. Atk.");
        HeldItem(251, "Apicot Berry", ItemPocket.Berries, 20, "Held: eaten at a quarter of HP to raise Sp. Def.");
    }

    private static void HeldItem(int id, string name, ItemPocket pocket, int price, string description) => Register(new ItemData
    {
        Id = id,
        Name = name,
        Pocket = pocket,
        EffectType = ItemEffectType.None,
        Price = price,
        CanUseInBattle = false,
        CanUseInOverworld = false,
        Description = description
    });

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
