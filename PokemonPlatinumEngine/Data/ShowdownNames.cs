using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// The few species and forms the data names otherwise than Pokémon Showdown does (plan 06 · R29b;
/// docs/mechanics/rulings.md, "Found on the way"), for whoever writes or reads a Pokémon by Showdown's name: the
/// export's text (<c>Models.ShowdownText</c>) and, later, plan 07's links. Every other name is the same in both. No
/// drawing or input.
/// </summary>
public static class ShowdownNames
{
    /// <summary>The data's name and Showdown's, a pair to each that differs. Where several of ours are one of Showdown's, the first is read back.</summary>
    private static readonly (string Ours, string Theirs)[] Pairs =
    {
        // A female's own form: Showdown's "-F", or the species alone where her looks are its only difference
        ("Meowstic-Female", "Meowstic-F"), ("Meowstic-Female-Mega", "Meowstic-F-Mega"), ("Meowstic-Male-Mega", "Meowstic-M-Mega"),
        ("Indeedee-Female", "Indeedee-F"), ("Basculegion-Female", "Basculegion-F"), ("Oinkologne-Female", "Oinkologne-F"),
        ("Pyroar-Female", "Pyroar"), ("Frillish-Female", "Frillish"), ("Jellicent-Female", "Jellicent"),
        // The data's Minior is its Meteor Form, and its Red Core is Showdown's Minior
        ("Minior", "Minior-Meteor"), ("Minior-Red", "Minior"),
        ("Necrozma-Dusk", "Necrozma-Dusk-Mane"), ("Necrozma-Dawn", "Necrozma-Dawn-Wings"),
        ("Darmanitan-Galar-Standard", "Darmanitan-Galar"),
        ("Tauros-Paldea-Combat-Breed", "Tauros-Paldea-Combat"), ("Tauros-Paldea-Blaze-Breed", "Tauros-Paldea-Blaze"), ("Tauros-Paldea-Aqua-Breed", "Tauros-Paldea-Aqua"),
        ("Ogerpon-Wellspring-Mask", "Ogerpon-Wellspring"), ("Ogerpon-Hearthflame-Mask", "Ogerpon-Hearthflame"), ("Ogerpon-Cornerstone-Mask", "Ogerpon-Cornerstone"),
        ("Zygarde-10", "Zygarde-10%"), ("Zygarde-10-Power-Construct", "Zygarde-10%"), ("Zygarde-50-Power-Construct", "Zygarde"),
        ("Squawkabilly-Blue-Plumage", "Squawkabilly-Blue"), ("Squawkabilly-Yellow-Plumage", "Squawkabilly-Yellow"), ("Squawkabilly-White-Plumage", "Squawkabilly-White"),
    };

    /// <summary>The data's names of the pairs, for the tests that hold them to the data.</summary>
    public static IEnumerable<string> OursThatDiffer => Pairs.Select(p => p.Ours);

    /// <summary>Showdown's name for one of the data's species or forms.</summary>
    public static string ToShowdown(string ours)
    {
        foreach (var (o, t) in Pairs) if (o == ours) return t;
        // Minior's meteors of every colour are one look to Showdown
        if (ours.StartsWith("Minior-", StringComparison.Ordinal) && ours.EndsWith("-Meteor", StringComparison.Ordinal)) return "Minior-Meteor";
        return ours;
    }

    /// <summary>
    /// Showdown's names that are a species of ours as well, read back as that species: a Pyroar, a Frillish or a
    /// Jellicent is a female only by its gender's line, and Showdown's Zygarde is the data's own.
    /// </summary>
    private static readonly HashSet<string> NotReadBack = new() { "Pyroar", "Frillish", "Jellicent", "Zygarde" };

    /// <summary>The data's name for one of Showdown's: the first of ours that is it, or the name as it is.</summary>
    public static string FromShowdown(string theirs)
    {
        if (NotReadBack.Contains(theirs)) return theirs;
        foreach (var (o, t) in Pairs) if (string.Equals(t, theirs, StringComparison.OrdinalIgnoreCase)) return o;
        return theirs;
    }
}
