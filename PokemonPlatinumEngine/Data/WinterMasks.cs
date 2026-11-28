using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// One of the four winter masks of Carpathia's New Year dances (plan 46 · CP13, our own; <c>docs/mechanics/rulings.md</c>,
/// "The winter masks"): its id, the item it is in the bag (the bag's MASKS pocket), the second type and the ability it
/// gives whoever wears it, and the role its id names (<see cref="Battle.Sim.BattleCore"/> plays each role where the
/// field's rules already bite).
/// </summary>
public sealed record WinterMask(string Id, string Item, PokemonType Type, string Ability);

/// <summary>
/// The winter masks' table (plan 46 · CP13). Code, not a data file: the four roles are rules of the battle's core, as the
/// abilities' effects are, and the masks' items are rows of <c>items.json</c>, which <c>GameDataFiles.Version</c> already
/// hashes. No drawing or input.
/// </summary>
public static class WinterMasks
{
    public const string Bear = "bear", Goat = "goat", Horse = "horse", Stag = "stag";

    /// <summary>The four, in the dances' order: the bear, the goat, the horse and the stag.</summary>
    public static IReadOnlyList<WinterMask> All { get; } = new[]
    {
        // The bear (ursul): bulk, and the foes' single-target moves meant for an ally come to it
        new WinterMask(Bear, "Bear Mask", PokemonType.Ground, "Fur Coat"),
        // The goat (capra): mischief, and its damaging moves take the target's raised stats first
        new WinterMask(Goat, "Goat Mask", PokemonType.Dark, "Prankster"),
        // The horse (căluțul): its damaging move carries it out to whoever comes in, a free switch each turn
        new WinterMask(Horse, "Horse Mask", PokemonType.Fighting, "Regenerator"),
        // The stag (cerbul): the leader, raising its side a stat a turn
        new WinterMask(Stag, "Stag Mask", PokemonType.Grass, "Sap Sipper")
    };

    /// <summary>The stats the stag raises on its side, one a turn end, in this order (<see cref="StagRounds"/> of them at most).</summary>
    public static IReadOnlyList<StatType> StagStats { get; } = new[] { StatType.Attack, StatType.Defense, StatType.SpAttack, StatType.SpDefense, StatType.Speed };

    /// <summary>How many turn ends the stag leads the dance: once for each of <see cref="StagStats"/>.</summary>
    public static int StagRounds => StagStats.Count;

    public static WinterMask? Get(string? id) => id == null ? null : All.FirstOrDefault(m => m.Id == id);

    /// <summary>The mask an item is, by the item's name; null for any other item.</summary>
    public static WinterMask? OfItem(string? item) => item == null ? null : All.FirstOrDefault(m => m.Item == item);

    /// <summary>The masks a bag holds, in the dances' order (what a battle offers the player).</summary>
    public static IReadOnlyList<string> Held(Inventory bag) =>
        All.Where(m => ItemDatabase.Get(m.Item) is { } item && bag.GetQuantity(item) > 0).Select(m => m.Id).ToList();

    /// <summary>The mask's name in the chosen language: its item's.</summary>
    public static string NameOf(string id) => Get(id) is { } m && ItemDatabase.Get(m.Item) is { } item ? item.DisplayName : id;

    /// <summary>The role's word in the chosen language ("the bear"), for the lines that name it.</summary>
    public static string RoleOf(string id) => Text.Of($"masks.{id}.role");
}
