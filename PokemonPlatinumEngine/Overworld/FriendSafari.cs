using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>One of a Trainer's Friend Safari grounds: its type and the three species that live in its grass.</summary>
public sealed record FriendSafariGrounds(PokemonType Type, IReadOnlyList<string> Species);

/// <summary>
/// Kalos's Friend Safari (plan 23, Kalos finished; docs/mechanics/rulings.md, "The Friend Safari"). X and Y draw a
/// friend's grounds (a type and two or three species) from the friend's own code; this game has no friend list (plan
/// 07 · O7), so plan 23's decision stands: each Trainer has three grounds drawn from their own trainer number, three
/// types and three species of each, always the same for the same Trainer, worked out whenever they are asked (nothing is
/// saved, and nothing is drawn from <see cref="Dice"/>). The species of each type are a table of ours, written by hand,
/// each of the type under the modern rules. GPU-free.
/// </summary>
public static class FriendSafari
{
    /// <summary>The level the grounds' Pokémon are met at, X and Y's (Kalos's own levels, mapped as any of the region's).</summary>
    public const int Level = 30;

    /// <summary>How many grounds a Trainer has, and how many species each holds.</summary>
    public const int GroundsCount = 3, SpeciesPerGrounds = 3;

    /// <summary>The Friend Safari's gate in Kiloude City and its grounds, by their maps' names.</summary>
    public const string Gate = "KiloudeCity_FriendSafari", Grounds = "KiloudeCity_FriendSafariGrounds";

    /// <summary>The story's variable that names the grounds the guide sent the player to (0 to 2).</summary>
    public const string ChosenVar = "VAR_KALOS_FRIEND_SAFARI";

    /// <summary>The species each type's grounds may hold: six each, ours.</summary>
    public static readonly IReadOnlyDictionary<PokemonType, string[]> SpeciesOfType = new Dictionary<PokemonType, string[]>
    {
        [PokemonType.Normal] = new[] { "Teddiursa", "Dunsparce", "Aipom", "Loudred", "Kecleon", "Eevee" },
        [PokemonType.Fire] = new[] { "Growlithe", "Ponyta", "Magmar", "Slugma", "Larvesta", "Braixen" },
        [PokemonType.Water] = new[] { "Krabby", "Poliwhirl", "Floatzel", "Octillery", "Frogadier", "Marill" },
        [PokemonType.Grass] = new[] { "Oddish", "Tangela", "Sunkern", "Petilil", "Swadloon", "Quilladin" },
        [PokemonType.Electric] = new[] { "Electabuzz", "Pachirisu", "Emolga", "Stunfisk", "Galvantula", "Dedenne" },
        [PokemonType.Ice] = new[] { "Sneasel", "Snorunt", "Delibird", "Spheal", "Snover", "Lapras" },
        [PokemonType.Fighting] = new[] { "Mankey", "Machoke", "Meditite", "Mienfoo", "Throh", "Sawk" },
        [PokemonType.Poison] = new[] { "Kakuna", "Swalot", "Grimer", "Ariados", "Garbodor", "Seviper" },
        [PokemonType.Ground] = new[] { "Sandile", "Diggersby", "Phanpy", "Trapinch", "Camerupt", "Marowak" },
        [PokemonType.Flying] = new[] { "Pidgey", "Spearow", "Farfetch'd", "Doduo", "Tranquill", "Swablu" },
        [PokemonType.Psychic] = new[] { "Abra", "Drowzee", "Grumpig", "Munna", "Sigilyph", "Espurr" },
        [PokemonType.Bug] = new[] { "Paras", "Venomoth", "Combee", "Ledian", "Beautifly", "Dwebble" },
        [PokemonType.Rock] = new[] { "Nosepass", "Boldore", "Pupitar", "Shuckle", "Sudowoodo", "Larvitar" },
        [PokemonType.Ghost] = new[] { "Shuppet", "Phantump", "Pumpkaboo", "Lampent", "Dusclops", "Duskull" },
        [PokemonType.Dragon] = new[] { "Gible", "Fraxure", "Noibat", "Dratini", "Druddigon", "Shelgon" },
        [PokemonType.Steel] = new[] { "Klang", "Magneton", "Mawile", "Ferroseed", "Skarmory", "Bronzor" },
        [PokemonType.Dark] = new[] { "Sableye", "Nuzleaf", "Pawniard", "Vullaby", "Inkay", "Murkrow" },
        [PokemonType.Fairy] = new[] { "Snubbull", "Togepi", "Spritzee", "Swirlix", "Clefairy", "Jigglypuff" },
    };

    // A salt of the safari's own, so its draw is no other rule's
    private const uint Salt = 0x5AFA21u;

    /// <summary>
    /// A Trainer's three grounds: three types told apart and three species of each, by the trainer number alone, in the
    /// order the guide offers them.
    /// </summary>
    public static IReadOnlyList<FriendSafariGrounds> Of(uint trainerNumber)
    {
        var types = Enum.GetValues<PokemonType>().ToList();
        var grounds = new List<FriendSafariGrounds>(GroundsCount);
        for (int g = 0; g < GroundsCount; g++)
        {
            var pick = new SplitMix(SplitMix.Mix(trainerNumber, Salt, (uint)g));
            var type = types[pick.Int(types.Count)];
            types.Remove(type);
            var pool = SpeciesOfType[type].ToList();
            var species = new List<string>(SpeciesPerGrounds);
            for (int s = 0; s < SpeciesPerGrounds; s++)
            {
                string one = pool[pick.Int(pool.Count)];
                pool.Remove(one);
                species.Add(one);
            }
            grounds.Add(new FriendSafariGrounds(type, species));
        }
        return grounds;
    }

    /// <summary>The grounds a variable's value names (0 to 2; anything else the first).</summary>
    public static FriendSafariGrounds Chosen(uint trainerNumber, int chosen) =>
        Of(trainerNumber)[chosen >= 0 && chosen < GroundsCount ? chosen : 0];

    /// <summary>The grass's table of a Trainer's grounds: each of the three species as likely as the others.</summary>
    public static List<WildEncounterEntry> Table(FriendSafariGrounds grounds) =>
        grounds.Species.Select(s => new WildEncounterEntry { SpeciesName = s, MinLevel = Level, MaxLevel = Level, Weight = 10 }).ToList();
}
