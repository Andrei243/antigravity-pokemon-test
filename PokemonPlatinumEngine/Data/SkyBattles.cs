using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Data;

/// <summary>What <c>Data/sky-battle.json</c> holds (plan 14 · B2): the moves a sky battle refuses and the Flying types that can't fly.</summary>
public sealed class SkyBattleFile
{
    /// <summary>The moves that need the ground, a body thrown down on it or a field laid over it, by their names in <c>moves.json</c>.</summary>
    public List<string> GroundedMoves { get; set; } = new();

    /// <summary>The species that are Flying types and still may not take part: they flap rather than fly (X and Y's own list, as we transcribe it).</summary>
    public List<string> GroundedSpecies { get; set; } = new();
}

/// <summary>
/// Sky battles (plan 14 · B2, X and Y's; <c>docs/mechanics/rulings.md</c>, "Sky battles"): fought in the air, by Pokémon that
/// fly or float alone, with the moves that need the ground refused. GPU-free. Who may take part is a Flying type or a
/// Pokémon whose ability is Levitate, not an Egg, and not one of the file's grounded species; what is refused is the file's
/// grounded moves. Showdown keeps no flag for either since it dropped sky battles, so both lists are written by hand.
/// </summary>
public static class SkyBattles
{
    public const string FileName = "sky-battle.json";

    private static SkyBattleFile? file;
    private static HashSet<string>? moves, species;

    /// <summary>The file as read (an empty one where there is none).</summary>
    public static SkyBattleFile File => file ??= System.IO.File.Exists(GameDataFiles.PathOf(FileName))
        ? GameDataFiles.Load<SkyBattleFile>(FileName) : new SkyBattleFile();

    private static HashSet<string> Moves => moves ??= new HashSet<string>(File.GroundedMoves, StringComparer.Ordinal);
    private static HashSet<string> Species => species ??= new HashSet<string>(File.GroundedSpecies, StringComparer.Ordinal);

    /// <summary>Whether a move is refused in a sky battle: it needs the ground (Earthquake, Dig), a body thrown on it (Seismic Toss) or a field laid over it.</summary>
    public static bool Grounded(MoveData move) => Moves.Contains(move.Name);

    /// <summary>Whether a species is one of the Flying types that may not take part.</summary>
    public static bool GroundedSpecies(string species) => Species.Contains(species);

    /// <summary>
    /// Whether a Pokémon may take part in a sky battle: a Flying type (of its form, as it stands) or one whose ability
    /// is Levitate, that is no Egg and is not one of the grounded species.
    /// </summary>
    public static bool CanFly(Pokemon p) =>
        !p.IsEgg && !GroundedSpecies(p.Species.Name) && (p.HasType(PokemonType.Flying) || p.AbilityName == "Levitate");

    /// <summary>The members of a team that may take part, in its order: the same Pokémon, on a party of their own.</summary>
    public static Party Fliers(Party team)
    {
        var party = new Party();
        foreach (var p in team.Members.Where(CanFly)) party.Add(p);
        return party;
    }

    /// <summary>Whether a team has a Pokémon able to fight that may take part.</summary>
    public static bool CanTakePart(Party team) => team.Members.Any(p => CanFly(p) && !p.IsFainted);
}
