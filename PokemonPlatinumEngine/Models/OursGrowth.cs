using System;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The stage a Pokémon not yet anyone's is met at once a region's levels have lifted it past its own (plan 18 · W4b-2,
/// with plan 03 · D15-A, "grown as they are met"; <c>docs/mechanics/rulings.md</c>, "Stages as they are met"). GPU-free.
///
/// <para><b>A trainer's Pokémon</b> (<see cref="GrowTrainers"/>), in a region the game crossed into, goes on through the
/// games' own stages (<see cref="GamesStageAt"/>) and then into a species of our own (<see cref="OursAt"/>), as the games
/// evolve a trainer's Pokémon by level whatever the evolution asks for: every trainer, the named ones too (the user's
/// answer of 2026-10-26), never into a legendary set's end. Not a Master Trainer's, the first battle's, the Battle
/// Frontier's or the Battle Subway's, which keep levels of their own.</para>
///
/// <para><b>A wild Pokémon</b> (<see cref="GrowWild"/>) keeps the games' stage the table gives it (so every regional
/// Pokédex can still be completed) and is met as its species of our own one time in four where its level reaches one,
/// chosen by its own personality, so nothing new is drawn and nothing seeded moves.</para>
/// </summary>
public static class OursGrowth
{
    /// <summary>The share of wild Pokémon met at or past an evolution of ours that are met as it: a personality whose remainder by this is nought.</summary>
    public const int WildShare = 4;

    /// <summary>
    /// The games' own next stage of a Pokémon of its level, or null (W4b's section 4, piece 1): among its species' ways
    /// into a species of the games that fit its form (a regional form by its own alone) and ask for no region, the
    /// first way with a level its level reaches, where any way has a level (a way decided by the Pokémon itself, its
    /// gender, its stats or its personality, only where it holds: Burmy, Tyrogue, Wurmple); where none has, the one way
    /// there is at <see cref="DifficultyRules.FriendshipStage"/> for friendship and <see cref="DifficultyRules.UnleveledStage"/>
    /// for the rest, as <see cref="DifficultyRules.StageAt"/> reads them going down; a line of several ways and no level
    /// (Eevee, Gloom, Poliwhirl) stays as it is.
    /// </summary>
    public static EvolutionData? GamesStageAt(Pokemon p)
    {
        var ways = (p.Species.Evolutions ?? new()).Where(e => e.Region == null && e.Method != EvolutionMethod.LevelShedinja
            && PokemonDatabase.Get(e.TargetSpecies) is { IsOurs: false } && Evolution.FitsForm(p, e)).ToList();
        if (ways.Count == 0) return null;
        if (ways.Any(e => e.Level > 0))
            return ways.FirstOrDefault(e => e.Level > 0 && e.Level <= p.Level && Inherent(p, e));
        if (ways.Count != 1) return null;
        var only = ways[0];
        int needs = only.Method is EvolutionMethod.Friendship or EvolutionMethod.FriendshipDay or EvolutionMethod.FriendshipNight
            ? DifficultyRules.FriendshipStage : DifficultyRules.UnleveledStage;
        return p.Level >= needs && Inherent(p, only) ? only : null;
    }

    /// <summary>What a way asks of the Pokémon itself, which is never taken as met: its gender, its stats, its personality.</summary>
    private static bool Inherent(Pokemon p, EvolutionData e) => e.Method switch
    {
        EvolutionMethod.LevelMale or EvolutionMethod.UseItemMale => p.Gender == Gender.Male,
        EvolutionMethod.LevelFemale or EvolutionMethod.UseItemFemale => p.Gender == Gender.Female,
        EvolutionMethod.LevelAttackHigher => p.Attack > p.Defense,
        EvolutionMethod.LevelAttackEqual => p.Attack == p.Defense,
        EvolutionMethod.LevelDefenseHigher => p.Attack < p.Defense,
        EvolutionMethod.LevelPersonalityLow => (p.Personality >> 16) % 10 < 5,
        EvolutionMethod.LevelPersonalityHigh => (p.Personality >> 16) % 10 >= 5,
        _ => true
    };

    /// <summary>
    /// The first evolution of ours (in <c>species.json</c>'s order) its level reaches and its form fits, its condition
    /// taken as met (the user's answer S2), never one into a legendary or mythical set's end; null for none.
    /// </summary>
    public static EvolutionData? OursAt(Pokemon p) =>
        (p.Species.Evolutions ?? new()).FirstOrDefault(e => e.Level > 0 && e.Level <= p.Level
            && PokemonDatabase.Get(e.TargetSpecies) is { IsOurs: true, Legendary: false, Mythical: false } && Evolution.FitsForm(p, e));

    /// <summary>
    /// A trainer's Pokémon at the stage its level allows: the games' stages first and then ours, as far as the level
    /// goes. Its moves are <see cref="Pokemon.GrowInto"/>'s, taught up to <paramref name="learnsUpTo"/>. True when it changed.
    /// </summary>
    public static bool GrowTrainers(Pokemon p, int learnsUpTo = int.MaxValue)
    {
        bool grew = false;
        for (int guard = 0; guard < 6 && (GamesStageAt(p) ?? OursAt(p)) is { } way; guard++)
        {
            Grow(p, way, learnsUpTo);
            grew = true;
        }
        return grew;
    }

    /// <summary>
    /// A wild Pokémon met at or past an evolution of ours is met as it one time in <see cref="WildShare"/>, by its
    /// personality; its games' stage is the table's. True when it changed.
    /// </summary>
    public static bool GrowWild(Pokemon p)
    {
        if (p.Personality % WildShare != 0) return false;
        bool grew = false;
        for (int guard = 0; guard < 6 && OursAt(p) is { } way; guard++)
        {
            Grow(p, way, int.MaxValue);
            grew = true;
        }
        return grew;
    }

    private static void Grow(Pokemon p, EvolutionData way, int learnsUpTo)
    {
        // The form the games choose by the Pokémon itself (Toxtricity's nature, a Maushold of three)
        var chosen = Evolution.Chosen(p, way, new EvolutionContext());
        p.GrowInto(PokemonDatabase.Get(chosen.TargetSpecies)!, chosen.TargetForm, learnsUpTo);
    }
}
