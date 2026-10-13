using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// Which moves a species can know (plan 14 · B14, the legality check plan 14 · B11's tournament and plan 13 · V13's
/// gauntlet share; no drawing and no input): what its own data can teach it, and what the species it evolved from
/// could, since a Pokémon keeps the moves it knew as it evolves. The data's ways are its level-up learnset (at any
/// level), its Egg moves, Platinum's move tutors' list, the TMs and HMs it takes (each machine read for the move it
/// holds), Grandma Wilma's Draco Meteor for a Dragon type and the strongest move of a starter fully grown. Nothing
/// else: an event's move, a move of a later game's tutor or machine the data doesn't list, a Sketch.
/// </summary>
public static class Legality
{
    /// <summary>Every move the species can know, by the game's own data.</summary>
    public static HashSet<string> Learnable(PokemonSpecies species) =>
        Learnable(species, PokemonDatabase.GetAll(), machine => ItemDatabase.Get(machine)?.TeachesMove);

    /// <summary>
    /// Every move the species can know, with the species to look for its forebears among and how a machine's name
    /// (<c>TM24</c>) gives the move it teaches: for a tool that has the data in hand before the files are written
    /// (<c>tools/DataImporter</c>).
    /// </summary>
    public static HashSet<string> Learnable(PokemonSpecies species, IEnumerable<PokemonSpecies> all, Func<string, string?> machineMove)
    {
        var everyone = all as IReadOnlyCollection<PokemonSpecies> ?? all.ToList();
        var moves = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>();
        var line = new Queue<PokemonSpecies>();
        line.Enqueue(species);
        while (line.Count > 0)
        {
            var s = line.Dequeue();
            if (!seen.Add(s.Name)) continue;
            foreach (var m in s.Learnset) moves.Add(m.MoveName);
            foreach (var m in s.EggMoves ?? new()) moves.Add(m);
            foreach (var m in s.TutorMoves ?? new()) moves.Add(m);
            foreach (var machine in s.TmMoves ?? new())
                if (machineMove(machine) is { } taught) moves.Add(taught);
            // Whoever evolves into it, and so on back to the first of the line
            foreach (var forebear in everyone.Where(f => f.Evolutions?.Any(e => e.TargetSpecies == s.Name) == true)) line.Enqueue(forebear);
        }
        // The two tutors whose moves are on no list (MoveTutors): Draco Meteor for a Dragon type, the starters' three
        if (species.PrimaryType == PokemonType.Dragon || species.SecondaryType == PokemonType.Dragon) moves.Add(MoveTutors.DracoMeteor);
        if (MoveTutors.UltimateMoveOf(species.Name) is { } ultimate) moves.Add(ultimate);
        return moves;
    }

    /// <summary>Whether a species can know a move.</summary>
    public static bool CanLearn(PokemonSpecies species, string move) => Learnable(species).Contains(move);

    /// <summary>The moves a Pokémon knows that its species can't, by name: none for a Pokémon that is what the data says it may be.</summary>
    public static List<string> Unlearnable(Pokemon pokemon)
    {
        var learnable = Learnable(pokemon.Species);
        // A form with a learnset of its own (plan 03 · D11) can know that too
        foreach (var m in pokemon.Learnset) learnable.Add(m.MoveName);
        return pokemon.Moves.Select(m => m.Name).Where(m => !learnable.Contains(m)).ToList();
    }
}
