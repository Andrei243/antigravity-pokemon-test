using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.Frontier;

/// <summary>Why the Pokémon chosen for a challenge can't go in together (<c>BattleTower_CheckDuplicateSpeciesAndHeldItems</c>).</summary>
public enum FrontierRefusal
{
    None,
    /// <summary>Two of them are of one species.</summary>
    SameSpecies,
    /// <summary>Two of them hold the same item.</summary>
    SameItem
}

/// <summary>
/// What a facility hands a Frontier battle beside its two teams (plan 06 · R18d; the original's <c>fieldWeather</c> of
/// a <c>FieldBattleDTO</c>): the weather the battle opens under, which stays, or the Trick Room it opens in. Only the
/// Battle Arcade's board gives either.
/// </summary>
public sealed record FrontierConditions(Battle.Sim.BattleWeather Weather = Battle.Sim.BattleWeather.None, bool TrickRoom = false);

/// <summary>
/// The rules every facility of the Battle Frontier holds its entrants to (plan 06 · R18a): which Pokémon may enter,
/// that no two of a team are of one species or hold the same item, and the level a battle is fought at. Ported from
/// <c>src/unk_02049D08.c</c> and <c>src/pokemon.c</c>. No drawing or input.
/// </summary>
public static class FrontierRules
{
    /// <summary>The level the Battle Tower's battles are fought at: its trainers' Pokémon are made at it, and a Pokémon of the player's above it is brought down to it.</summary>
    public const int Level = 50;

    /// <summary>An Egg, or a species on the banlist, enters nothing (<c>sub_02049EC4</c>, <c>Pokemon_IsBannedFromBattleFrontier</c>).</summary>
    public static bool MayEnter(Pokemon pokemon) => !pokemon.IsEgg && !FrontierDatabase.IsBanned(pokemon.Species.Name);

    /// <summary>
    /// Whether the team holds <paramref name="needed"/> Pokémon that could go in together (<c>sub_02049EC4</c>):
    /// allowed ones, no two of one species and no two holding the same item. The original first drops whoever is of a
    /// species and holds an item both already counted, then tries every choice of that many.
    /// </summary>
    public static bool EnoughToEnter(Party party, int needed)
    {
        if (party.Count < needed) return false;
        var counted = new List<(string Species, string? Item)>();
        foreach (var p in party.Members)
        {
            if (!MayEnter(p)) continue;
            string species = p.Species.Name;
            string? item = p.HeldItem?.Name;
            if (item != null && counted.Any(c => c.Species == species && c.Item == item)) continue;
            counted.Add((species, item));
        }
        return counted.Count >= needed && AnyChoice(counted, needed, 0, new List<(string, string?)>());
    }

    // sub_02049DB4 over sub_02049D64: some choice of that many with every species and every item held different
    private static bool AnyChoice(List<(string Species, string? Item)> all, int needed, int from, List<(string Species, string? Item)> chosen)
    {
        if (chosen.Count == needed) return true;
        for (int i = from; i < all.Count; i++)
        {
            var next = all[i];
            if (chosen.Any(c => c.Species == next.Species || (c.Item != null && c.Item == next.Item))) continue;
            chosen.Add(next);
            if (AnyChoice(all, needed, i + 1, chosen)) return true;
            chosen.RemoveAt(chosen.Count - 1);
        }
        return false;
    }

    /// <summary>
    /// <c>BattleTower_CheckDuplicateSpeciesAndHeldItems</c>, over the Pokémon chosen in their order: the first pair
    /// found decides what is said, a shared species before a shared item for each newcomer.
    /// </summary>
    public static FrontierRefusal WhyNot(IReadOnlyList<Pokemon> chosen)
    {
        for (int i = 1; i < chosen.Count; i++)
            for (int j = 0; j < i; j++)
            {
                if (chosen[i].Species.Name == chosen[j].Species.Name) return FrontierRefusal.SameSpecies;
                if (chosen[i].HeldItem != null && chosen[i].HeldItem!.Name == chosen[j].HeldItem?.Name) return FrontierRefusal.SameItem;
            }
        return FrontierRefusal.None;
    }

    /// <summary>The species of the banlist the Pokédex has seen, which the attendant names when too few may enter (<c>BattleFrontier_MakeSeenBanlistSpeciesMsg</c>).</summary>
    public static List<string> BannedSeen(Func<string, bool> seen) => FrontierDatabase.Banned.Where(seen).ToList();

    /// <summary>
    /// A Pokémon as it goes into a Frontier battle (<c>FieldBattleDTO_NewBattleTower</c>): a copy, so that nothing the
    /// battle does reaches the Pokémon itself, brought down to <paramref name="level"/> if it is above it (its EXP set
    /// to that level's, its stats worked out again) and left at its own level if it is under. The copy goes in whole:
    /// the lobby heals the team before a challenge and the battle's own copies are thrown away after each battle.
    /// </summary>
    public static Pokemon ForBattle(Pokemon own, int level = Level)
    {
        var copy = own.Clone();
        // A copy the Frontier fights with (and keeps, while a challenge is put down) is nobody's Pokémon: it carries
        // no id, so it is never taken for the one it was made from (plan 07 · O2)
        copy.Id = Guid.Empty;
        if (copy.Level > level)
        {
            copy.LevelTo(level);
            copy.CurrentExp = Pokemon.GetExpForLevel(level, copy.Species.GrowthRate);
        }
        copy.RecalculateStats();
        copy.CurrentHP = copy.MaxHP;
        copy.Status = StatusCondition.None;
        copy.SleepTurns = 0;
        copy.ToxicCounter = 0;
        foreach (var move in copy.Moves) move.CurrentPP = move.MaxPP;
        copy.ResetStatStages();
        return copy;
    }

    /// <summary>The team a challenge is fought with: copies of the Pokémon chosen, in the order they were chosen.</summary>
    public static Party TeamFor(Party party, IEnumerable<int> slots, int level = Level)
    {
        var team = new Party();
        foreach (int slot in slots)
            if (slot >= 0 && slot < party.Count) team.Add(ForBattle(party.Members[slot], level));
        return team;
    }
}
