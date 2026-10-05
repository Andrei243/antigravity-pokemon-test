using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// The Pokémon at the head of the party, as far as the wild Pokémon care (the original's
/// <c>WildEncounters_FieldParams</c>): its ability works in the field whether it can fight or not, and so does
/// what it holds (<paramref name="HoldEffect"/>: a Cleanse Tag's <c>EncountersDown</c>).
/// </summary>
public readonly record struct WildLead(string? Ability, int Level, Nature Nature, Gender Gender, string? HoldEffect = null)
{
    public static WildLead? Of(Party party) =>
        party.Members.Count == 0 ? null : new WildLead(party.Members[0].AbilityName, party.Members[0].Level, party.Members[0].Nature, party.Members[0].Gender, party.Members[0].HeldItem?.HoldEffect);
}

/// <summary>
/// What the lead's ability does to the wild Pokémon met (plan 06 · R7), following the original's
/// <c>src/overlay006/wild_encounters.c</c>, with no drawing or input: how often one is met, which of the place's
/// table it is, its level, whether it is scared off, its nature and its gender. Each function names the one it
/// was read from.
/// </summary>
public static class WildEncounterRules
{
    /// <summary>
    /// <c>ModifyEncounterRateWithFieldParams</c>: twice the place's rate with Arena Trap, No Guard or Illuminate;
    /// half with White Smoke, Quick Feet or Stench, with Sand Veil in a sandstorm and with Snow Cloak in the snow;
    /// never over a hundred. Then <c>ModifyEncounterRateWithHeldItem</c>: two thirds of that with a Cleanse Tag or
    /// a Pure Incense in the lead's hands (plan 06 · R8).
    /// </summary>
    public static int Rate(int rate, WildLead? lead, FieldWeather weather)
    {
        if (lead is not { } first) return rate;
        switch (first.Ability)
        {
            case "Arena Trap" or "No Guard" or "Illuminate":
                rate *= 2;
                break;
            case "Sand Veil" when weather == FieldWeather.Sandstorm:
            case "Snow Cloak" when weather is FieldWeather.Snow or FieldWeather.HeavySnow or FieldWeather.Blizzard:
            case "White Smoke" or "Quick Feet" or "Stench":
                rate /= 2;
                break;
        }
        rate = Math.Min(rate, 100);
        if (first.HoldEffect == PokemonPlatinumEngine.Battle.Effects.HeldItemEffects.EncountersDown) rate = rate * 2 / 3;
        return rate;
    }

    /// <summary>
    /// The wild Pokémon a step has met, or null when the lead scared it off (<c>TryGenerateWildMon</c>): the slot
    /// of the table, its level, Keen Eye and Intimidate's say, then the gender and the nature the lead's ability
    /// chose for it. What comes back is a row of its own, with the level decided.
    /// </summary>
    public static WildEncounterEntry? Meet(IReadOnlyList<WildEncounterEntry> table, bool water, WildLead? lead, Random rng)
    {
        if (table.Count == 0) return null;
        var slot = Slot(table, water, lead, rng);
        int level = Level(table, slot, water, lead, rng);
        if (ScaredOff(lead, level, rng)) return null;
        return new WildEncounterEntry
        {
            SpeciesName = slot.SpeciesName,
            MinLevel = level,
            MaxLevel = level,
            Weight = slot.Weight,
            Gender = GenderFor(PokemonDatabase.Get(slot.SpeciesName), lead, rng),
            Nature = NatureFor(lead, rng)
        };
    }

    /// <summary>
    /// Which row of the table: with Magnet Pull one time in two a Steel type's row, with Static an Electric
    /// type's, each such row as likely as another, when the table has some and isn't made of them
    /// (<c>TryGetSlotForTypeMatchAbility</c>); otherwise a row by its weight. On the water only Static pulls: the
    /// original overwrites what Magnet Pull found there, and so does this.
    /// </summary>
    public static WildEncounterEntry Slot(IReadOnlyList<WildEncounterEntry> table, bool water, WildLead? lead, Random rng)
    {
        var pulled = lead?.Ability switch
        {
            "Magnet Pull" when !water => OfType(table, PokemonType.Steel, rng),
            "Static" => OfType(table, PokemonType.Electric, rng),
            _ => null
        };
        if (pulled != null) return pulled;

        int roll = rng.Next(table.Sum(e => e.Weight));
        foreach (var e in table)
        {
            roll -= e.Weight;
            if (roll < 0) return e;
        }
        return table[0];
    }

    private static WildEncounterEntry? OfType(IReadOnlyList<WildEncounterEntry> table, PokemonType type, Random rng)
    {
        if (rng.Next(2) != 0) return null;
        var matching = table.Where(e => PokemonDatabase.Get(e.SpeciesName) is { } s && (s.PrimaryType == type || s.SecondaryType == type)).ToList();
        return matching.Count == 0 || matching.Count == table.Count ? null : matching[rng.Next(matching.Count)];
    }

    /// <summary>
    /// The level: a row's own, or one of its range. With Hustle, Vital Spirit or Pressure one time in two it is
    /// the highest the place has for that species: the top of the row's range (<c>GetWildMonLevel</c>, the
    /// water's rows), or the row of the same species with the highest level (<c>TryFindHigherLevelSlot</c>, the
    /// land's, whose rows have one level each).
    /// </summary>
    public static int Level(IReadOnlyList<WildEncounterEntry> table, WildEncounterEntry slot, bool water, WildLead? lead, Random rng)
    {
        int low = Math.Min(slot.MinLevel, slot.MaxLevel), high = Math.Max(slot.MinLevel, slot.MaxLevel);
        int level = high > low ? low + rng.Next(high - low + 1) : low;
        if (lead?.Ability is not ("Hustle" or "Vital Spirit" or "Pressure") || rng.Next(2) == 0) return level;
        if (water) return high;
        return Math.Max(high, table.Where(e => e.SpeciesName == slot.SpeciesName).Max(e => Math.Max(e.MinLevel, e.MaxLevel)));
    }

    /// <summary><c>FirstMonAbilityPreventsEncounter</c>: with Keen Eye or Intimidate on a lead above level 5, a wild Pokémon five levels or more below it stays away one time in two.</summary>
    public static bool ScaredOff(WildLead? lead, int wildLevel, Random rng)
    {
        if (lead is not { Ability: "Keen Eye" or "Intimidate" } first || first.Level <= 5) return false;
        return wildLevel <= first.Level - 5 && rng.Next(2) == 0;
    }

    /// <summary><c>GetNatureForWildMon</c>: with Synchronize the lead's own nature one time in two; null leaves it to chance.</summary>
    public static Nature? NatureFor(WildLead? lead, Random rng) =>
        lead is { Ability: "Synchronize" } first && rng.Next(2) == 0 ? first.Nature : null;

    /// <summary><c>CreateWildMon</c>: with Cute Charm, two times in three, the gender the lead isn't, for a species that has both; null leaves it to chance.</summary>
    public static Gender? GenderFor(PokemonSpecies? species, WildLead? lead, Random rng)
    {
        if (lead is not { Ability: "Cute Charm" } first || species == null || species.GenderRatio is <= 0 or >= 8) return null;
        if (first.Gender == Gender.Genderless || rng.Next(3) == 0) return null;
        return first.Gender == Gender.Female ? Gender.Male : Gender.Female;
    }
}
