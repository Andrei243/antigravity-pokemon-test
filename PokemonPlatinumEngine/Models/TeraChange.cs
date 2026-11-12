using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>Why a Tera Type can't be changed (plan 36 · TR5), in the order it is asked.</summary>
public enum TeraRefusal
{
    None = 0,
    /// <summary>Nobody was chosen, or an Egg was.</summary>
    Nobody,
    /// <summary>Ogerpon's and Terapagos's are their own whatever is set (<see cref="TeraType.ForcedFor"/>).</summary>
    Forced,
    /// <summary>Its Tera Type is that one already.</summary>
    Already,
    /// <summary>Fewer than <see cref="TeraChange.Cost"/> shards of the type in the bag.</summary>
    TooFewShards
}

/// <summary>
/// A Tera Type changed for shards (plan 36 · TR5; <c>docs/mechanics/rulings.md</c>, "Tera Shards and the Tera keeper"):
/// fifty shards of a type, Scarlet and Violet's number, turn a Pokémon's Tera Type to that type at a training hall's Tera
/// keeper; the Stellar Tera Shard turns it to Stellar. Refused for an Egg, for Ogerpon and Terapagos, for the type it has
/// already and for too few shards. The type is <see cref="Pokemon.TeraType"/>, saved as it always was. No drawing or input.
/// </summary>
public static class TeraChange
{
    /// <summary>The shards one change takes.</summary>
    public const int Cost = 50;

    /// <summary>The twenty Tera Types a shard gives, in the chart's order (Sound, ours, plan 46 · CP2, last of the types) and Stellar last.</summary>
    public static IReadOnlyList<TeraType> Types { get; } =
        System.Enum.GetValues<PokemonType>().Select(TeraType.Of).Append(TeraType.Stellar).ToList();

    /// <summary>The shard of a Tera Type, as the data names it: "Fire Tera Shard", "Stellar Tera Shard".</summary>
    public static string ShardOf(TeraType type) => type.Name + " Tera Shard";

    /// <summary>The shard of a type: "Fire Tera Shard".</summary>
    public static string ShardOf(PokemonType type) => ShardOf(TeraType.Of(type));

    /// <summary>How many of a type's shards the bag holds.</summary>
    public static int Held(Inventory bag, TeraType type) =>
        ItemDatabase.Get(ShardOf(type)) is { } shard ? bag.GetQuantity(shard) : 0;

    /// <summary>Why <paramref name="pokemon"/>'s Tera Type can't be turned to <paramref name="type"/>; <see cref="TeraRefusal.None"/> when it can.</summary>
    public static TeraRefusal WhyNot(Pokemon? pokemon, TeraType type, Inventory bag)
    {
        if (pokemon is not { IsEgg: false }) return TeraRefusal.Nobody;
        if (TeraType.ForcedFor(pokemon) != null) return TeraRefusal.Forced;
        if (pokemon.TeraType == type) return TeraRefusal.Already;
        return Held(bag, type) < Cost ? TeraRefusal.TooFewShards : TeraRefusal.None;
    }

    /// <summary>
    /// Why the Pokémon can have no new Tera Type at all (nobody, forced, or no type it hasn't with fifty shards);
    /// <see cref="TeraRefusal.None"/> when some type is open to it.
    /// </summary>
    public static TeraRefusal WhyNotAny(Pokemon? pokemon, Inventory bag)
    {
        if (pokemon is not { IsEgg: false }) return TeraRefusal.Nobody;
        if (TeraType.ForcedFor(pokemon) != null) return TeraRefusal.Forced;
        return Open(pokemon, bag).Count > 0 ? TeraRefusal.None : TeraRefusal.TooFewShards;
    }

    /// <summary>The Tera Types the Pokémon may be turned to now, in <see cref="Types"/>' order.</summary>
    public static IReadOnlyList<TeraType> Open(Pokemon pokemon, Inventory bag) =>
        Types.Where(t => WhyNot(pokemon, t, bag) == TeraRefusal.None).ToList();

    /// <summary>The change made: <see cref="Cost"/> shards taken from the bag and the Tera Type set. False, and nothing taken, where it is refused.</summary>
    public static bool Change(Pokemon pokemon, TeraType type, Inventory bag)
    {
        if (WhyNot(pokemon, type, bag) != TeraRefusal.None || ItemDatabase.Get(ShardOf(type)) is not { } shard) return false;
        bag.RemoveItem(shard, Cost);
        pokemon.TeraType = type;
        return true;
    }
}
