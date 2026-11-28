using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The picnic basket (plan 34 · PD7; <c>docs/mechanics/rulings.md</c>, "Tera Raids and picnics"): where Paldea's Eggs are
/// found, the Day Care being Sinnoh's and none in Paldea. While the team picnics, the first two of it who would get on at a
/// Day Care (<see cref="Breeding.Compatibility"/>) leave an Egg in the basket now and then: one try a minute of the picnic,
/// each taking an Egg one time in a hundred by as much as the Day Care's own chance for the pair (twenty, fifty or seventy)
/// and the Egg Power's strength more, never more than <see cref="Most"/> a picnic. An Egg is the Day Care's
/// (<see cref="Breeding.MakeEgg"/>: its species, personality, moves and inheritance). Everything is drawn on a generator of
/// its own seeded by the picnic, so a test or a replay finds the same Eggs and no other draw of the game moves; a picnic of
/// under a minute, or a team with no couple, draws nothing. GPU-free.
/// </summary>
public static class PicnicBasket
{
    /// <summary>The seconds of picnic a try takes, and the most Eggs one picnic leaves.</summary>
    public const int SecondsPerTry = 60, Most = 10;

    /// <summary>The first two of the team who would get on at a Day Care, in the team's order; null where no two would.</summary>
    public static (Pokemon First, Pokemon Second)? Couple(Party party)
    {
        var members = party.Members.Where(p => !p.IsEgg).ToList();
        for (int i = 0; i < members.Count; i++)
            for (int j = i + 1; j < members.Count; j++)
                if (Breeding.Compatibility(members[i], members[j]) != Compatibility.None) return (members[i], members[j]);
        return null;
    }

    /// <summary>The chance in a hundred a try takes an Egg: the pair's own Day Care chance and the Egg Power's strength, at most a hundred.</summary>
    public static int ChanceOf(Compatibility compatibility, int eggPower) =>
        compatibility == Compatibility.None ? 0 : Math.Min(100, (int)compatibility + Math.Max(0, eggPower));

    /// <summary>
    /// The Eggs a picnic of so many seconds leaves in the basket for a team, with the Egg Power's strength in force: one try
    /// for every whole <see cref="SecondsPerTry"/>, at most <see cref="Most"/> Eggs, each made as the Day Care makes one for
    /// the couple (the Shiny Charm's rolls where <paramref name="shinyCharm"/>). Empty, drawing nothing, with no couple or
    /// under a minute.
    /// </summary>
    public static List<Pokemon> Find(Party party, float seconds, int eggPower, uint seed, uint trainer, Ruleset rules, bool shinyCharm = false)
    {
        var found = new List<Pokemon>();
        int tries = (int)Math.Floor(Math.Max(0f, seconds) / SecondsPerTry);
        if (tries == 0 || Couple(party) is not { } couple) return found;
        int chance = ChanceOf(Breeding.Compatibility(couple.First, couple.Second), eggPower);
        var rng = new Random(unchecked((int)SplitMix.Mix(seed, 0xBA5CE7u, (uint)tries)));
        for (int i = 0; i < tries && found.Count < Most; i++)
        {
            if (rng.Next(100) >= chance) continue;
            uint offspring = Breeding.OffspringPersonality(couple.First, couple.Second, rng, rules);
            found.Add(Breeding.MakeEgg(couple.First, couple.Second, offspring, trainer, rng, rules, shinyCharm));
        }
        return found;
    }
}
