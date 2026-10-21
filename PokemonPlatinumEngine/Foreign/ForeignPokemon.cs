using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Foreign;

/// <summary>The console a foreign Pokémon was read from.</summary>
public enum ForeignConsole { Gba, Ds }

/// <summary>The GBA game a save came from, as far as a file tells (a Ruby and a Sapphire, or a FireRed and a LeafGreen, lay their saves out alike).</summary>
public enum GbaGame { RubySapphire, Emerald, FireRedLeafGreen, Unknown }

/// <summary>
/// A Pokémon read from a foreign file, as plain data (plan 08 · P20): what the original's Pokémon of that console keeps,
/// as numbers, before <see cref="ToSaved"/> turns it into what this game keeps. Nothing here is read back from the game's
/// own data: a GBA species is still the GBA's number (<see cref="GbaSpecies"/>), and an item is still the console's.
/// </summary>
public sealed class ForeignPokemon
{
    public ForeignConsole Console { get; init; }

    /// <summary>The game of the save it came out of, for a Pokémon of the GBA's: Deoxys's forme is the game it left.</summary>
    public GbaGame Game { get; init; } = GbaGame.Unknown;

    public uint Personality { get; init; }

    /// <summary>The original trainer's ID and secret ID, the low half the first.</summary>
    public uint OtId { get; init; }

    /// <summary>The species' number in the National Pokédex (<see cref="MigrationDatabase.SpeciesFromGba"/> for a GBA Pokémon); 0 for none.</summary>
    public int Species { get; init; }

    /// <summary>The number the GBA gave the species; 0 for a DS Pokémon.</summary>
    public int GbaSpecies { get; init; }

    /// <summary>A bad Egg: a Pokémon of the GBA whose checksum failed, which the game itself shows as one.</summary>
    public bool IsBadEgg { get; init; }
    public bool IsEgg { get; init; }

    public string Nickname { get; init; } = "";
    public bool HasNickname { get; init; }
    public string OtName { get; init; } = "";

    /// <summary>The original trainer's gender, 0 for a boy and 1 for a girl.</summary>
    public int OtGender { get; init; }

    /// <summary>The GBA's language number of the game it came from, or the DS's (they agree: 1 Japanese, 2 English, 3 French, 4 Italian, 5 German, 7 Spanish).</summary>
    public int Language { get; init; } = MigrationDatabase.English;

    /// <summary>The item it holds as its console numbered it (the GBA's, or Platinum's); 0 for none.</summary>
    public int HeldItemId { get; init; }

    public int Experience { get; init; }
    public int Friendship { get; init; }

    /// <summary>The GBA's ability bit (0 or 1), or the DS's ability number.</summary>
    public int AbilityNumber { get; init; }

    public int[] Moves { get; init; } = new int[4];
    public int[] Pp { get; init; } = new int[4];
    public int[] PpUps { get; init; } = new int[4];

    /// <summary>Hit points, Attack, Defense, Speed, Special Attack and Special Defense, the order both consoles keep them in.</summary>
    public int[] Ivs { get; init; } = new int[6];
    public int[] Evs { get; init; } = new int[6];

    public int Cool { get; init; }
    public int Beauty { get; init; }
    public int Cute { get; init; }
    public int Smart { get; init; }
    public int Tough { get; init; }
    public int Sheen { get; init; }

    public int Pokerus { get; init; }
    public int Markings { get; init; }
    public int Ball { get; init; }
    public int MetLevel { get; init; }

    /// <summary>The game it was met in (the GBA's version number, the DS's origin game).</summary>
    public int MetGame { get; init; }

    /// <summary>The form a DS Pokémon is in (its form field).</summary>
    public int FormNumber { get; init; }

    /// <summary>The day it was met, where the console keeps one (the DS's); null for the GBA's.</summary>
    public DateTime? MetDate { get; init; }

    // ------------------------------------------------------------------ what the original's checks ask of it

    /// <summary>Whether it knows one of the GBA's HM moves that Platinum refuses a Pokémon with (<c>BoxMonGBAHasHM</c>).</summary>
    public bool KnowsAnHm
    {
        get
        {
            var names = MigrationDatabase.Data.HmMoves;
            return Moves.Any(id => id > 0 && MoveName(id) is { } move && names.Contains(move));
        }
    }

    /// <summary>Whether it holds one of the GBA's key items or HMs (<c>BoxMonGBAHasInvalidItem</c>); only a GBA Pokémon can.</summary>
    public bool HoldsAnInvalidItem => Console == ForeignConsole.Gba && HeldItemId != 0 && MigrationDatabase.IsInvalidGbaItem(HeldItemId);

    /// <summary>Whether its species is one the games both have (<c>IsGBASpeciesInvalid</c>); a DS Pokémon's is any in the data.</summary>
    public bool HasARealSpecies => Console == ForeignConsole.Gba ? MigrationDatabase.IsGbaSpeciesReal(GbaSpecies) : PokemonDatabase.GetByDex(Species) != null;

    /// <summary>The species as the data has it, or null where it has none.</summary>
    public PokemonSpecies? SpeciesData => PokemonDatabase.GetByDex(Species);

    // ------------------------------------------------------------------ the conversion

    /// <summary>What a Pokémon of the GBA's has its friendship put back to (<c>BASE_FRIENDSHIP_VALUE</c>).</summary>
    public const int BaseFriendship = 70;

    private static readonly string[] BallNames =
    {
        "", "Master Ball", "Ultra Ball", "Great Ball", "Poké Ball", "Safari Ball", "Net Ball", "Dive Ball", "Nest Ball",
        "Repeat Ball", "Timer Ball", "Luxury Ball", "Premier Ball", "Dusk Ball", "Heal Ball", "Quick Ball", "Cherish Ball"
    };

    private static readonly Lazy<Dictionary<int, string>> MoveNames = new(() => MoveDatabase.GetAll().GroupBy(m => m.Id).ToDictionary(g => g.Key, g => g.First().Name));
    private static readonly Lazy<Dictionary<int, string>> ItemNames = new(() => ItemDatabase.GetAll().GroupBy(i => i.Id).ToDictionary(g => g.Key, g => g.First().Name));

    /// <summary>The move a number is (Platinum's numbers, which the GBA's share up to Psycho Boost), or null.</summary>
    public static string? MoveName(int id) => MoveNames.Value.TryGetValue(id, out var name) ? name : null;

    /// <summary>The held item as this game names it: the GBA's through <c>Item_FromGBAID</c>, Platinum's by its number.</summary>
    public string? HeldItemName => HeldItemId == 0 ? null
        : Console == ForeignConsole.Gba ? MigrationDatabase.ItemFromGba(HeldItemId)
        : ItemNames.Value.GetValueOrDefault(HeldItemId);

    /// <summary>The level its EXP is at on its species' curve (1 for a species the data lacks).</summary>
    public int Level
    {
        get
        {
            if (SpeciesData is not { } species) return 1;
            int level = 1;
            while (level < 100 && Experience >= Pokemon.GetExpForLevel(level + 1, species.GrowthRate)) level++;
            return level;
        }
    }

    /// <summary>
    /// What <c>BoxMonGBAToBoxMon</c> makes of a GBA Pokémon (a DS Pokémon is already one): friendship back to the base
    /// value, the ability by the GBA's bit unless its species gained one, Unown's letter from the personality,
    /// Deoxys's forme by the game it left, and nature, gender and shininess from the personality as Platinum's follow it.
    /// What this game has no field for (the ribbons, the contest ranks, the game of origin) is dropped. Hit points and
    /// PP are full: nothing of the GBA's party data is kept.
    /// </summary>
    public SavedPokemonData ToSaved(Ruleset? rules = null)
    {
        var species = SpeciesData ?? throw new InvalidOperationException($"no species {Species}");
        string? form = FormName(species);
        var formData = form != null ? species.Forms?.FirstOrDefault(f => f.Name == form) : null;
        var abilities = formData?.Abilities ?? species.Abilities;
        string? ability = Console == ForeignConsole.Ds ? AbilityDatabase.NameOf(AbilityNumber) ?? abilities.FirstOrDefault() : GbaAbility(species, abilities);

        string? nickname = Nickname;
        if (!HasNickname || string.Equals(Nickname, species.Name, StringComparison.OrdinalIgnoreCase)) nickname = "";

        var saved = new SavedPokemonData
        {
            SpeciesName = species.Name,
            DexNumber = species.DexNumber,
            IsEgg = IsEgg,
            Form = form,
            Nickname = nickname,
            Level = Level,
            Gender = Models.Personality.GenderOf(species, Personality),
            Nature = Models.Personality.NatureOf(Personality),
            IsShiny = Models.Personality.IsShiny(OtId, Personality, (rules ?? Ruleset.Current).ShinyOdds),
            CurrentHP = 999,
            IvHP = Ivs[0], IvAttack = Ivs[1], IvDefense = Ivs[2], IvSpeed = Ivs[3], IvSpAttack = Ivs[4], IvSpDefense = Ivs[5],
            EvHP = Evs[0], EvAttack = Evs[1], EvDefense = Evs[2], EvSpeed = Evs[3], EvSpAttack = Evs[4], EvSpDefense = Evs[5],
            CurrentExp = Experience,
            Ability = ability,
            HeldItem = HeldItemName,
            Friendship = Console == ForeignConsole.Gba ? BaseFriendship : Friendship,
            Cool = Cool, Beauty = Beauty, Cute = Cute, Smart = Smart, Tough = Tough, Sheen = Sheen,
            Personality = Personality,
            Ball = Ball is > 0 and < 17 ? BallNames[Ball] : null,
            OriginalTrainer = new TrainerMark(OtName, unchecked((int)OtId), OtGender == 1 ? PlayerLook.Girl : PlayerLook.Boy),
            Pokerus = Pokerus,
            Marks = Markings,
            MetLevel = MetLevel,
            MetDate = MetDate,
            Language = LanguageName(Language)
        };
        for (int i = 0; i < 4; i++)
        {
            if (MoveName(Moves[i]) is not { } move) continue;
            saved.Moves.Add(new SavedMoveData { MoveName = move, CurrentPP = 99, PPUps = PpUps[i] });
        }
        return saved;
    }

    /// <summary><c>ConvertBoxMonGBAtoDSAbility</c>: the second ability only where the species has one, did not gain one in Generation 4 and the bit is set.</summary>
    private string? GbaAbility(PokemonSpecies species, IReadOnlyList<string> abilities)
    {
        if (abilities.Count < 2) return abilities.FirstOrDefault();
        if (MigrationDatabase.GainedAnAbility(species.DexNumber)) return abilities[0];
        return (AbilityNumber & 1) == 0 ? abilities[0] : abilities[1];
    }

    private string? FormName(PokemonSpecies species)
    {
        if (Console == ForeignConsole.Gba)
        {
            // Unown's letter is made of two bits of each byte of the personality (twenty-eight of them); Deoxys's forme is the game's
            if (species.Name == "Unown")
            {
                int letter = (((int)(Personality & 0x3000000) >> 18) | ((int)(Personality & 0x30000) >> 12) | ((int)(Personality & 0x300) >> 6) | (int)(Personality & 0x3)) % 28;
                return DsForms.Unown(letter);
            }
            if (species.Name == "Deoxys")
            {
                int forme = Game switch
                {
                    GbaGame.Emerald => 3,
                    GbaGame.FireRedLeafGreen => MetGame == 5 ? 2 : 1,
                    GbaGame.RubySapphire => 0,
                    _ => MetGame switch { 3 => 3, 4 => 1, 5 => 2, _ => 0 }
                };
                return DsForms.Of(species.Name, forme);
            }
            return null;
        }
        return DsForms.Of(species.Name, FormNumber);
    }

    private static string? LanguageName(int language) => language switch
    {
        MigrationDatabase.Japanese => "Japanese",
        MigrationDatabase.English => null,
        MigrationDatabase.French => "French",
        MigrationDatabase.Italian => "Italian",
        MigrationDatabase.German => "German",
        MigrationDatabase.Spanish => "Spanish",
        _ => null
    };
}

/// <summary>The forms a species' form number names in the DS games' Pokémon, as this game's data names them.</summary>
internal static class DsForms
{
    private static readonly string[] Arceus =
    {
        "", "Fighting", "Flying", "Poison", "Ground", "Rock", "Bug", "Ghost", "Steel", "", "Fire", "Water", "Grass", "Electric", "Psychic", "Ice", "Dragon", "Dark"
    };

    /// <summary>Unown's letter: A to Z, then the exclamation and the question mark; A is the species' own.</summary>
    public static string? Unown(int letter) => letter switch
    {
        0 => null,
        <= 25 => $"Unown-{(char)('A' + letter)}",
        26 => "Unown-Exclamation",
        27 => "Unown-Question",
        _ => null
    };

    /// <summary>The form of a species by its form number, null for the species' own or for one this game has no form of.</summary>
    public static string? Of(string species, int form)
    {
        if (form <= 0 && species != "Arceus") return null;
        string? name = species switch
        {
            "Unown" => Unown(form),
            "Deoxys" => form switch { 1 => "Deoxys-Attack", 2 => "Deoxys-Defense", 3 => "Deoxys-Speed", _ => null },
            "Burmy" or "Wormadam" => form switch { 1 => $"{species}-Sandy", 2 => $"{species}-Trash", _ => null },
            "Shellos" or "Gastrodon" => form == 1 ? $"{species}-East" : null,
            "Rotom" => form switch { 1 => "Rotom-Heat", 2 => "Rotom-Wash", 3 => "Rotom-Frost", 4 => "Rotom-Fan", 5 => "Rotom-Mow", _ => null },
            "Giratina" => form == 1 ? "Giratina-Origin" : null,
            "Shaymin" => form == 1 ? "Shaymin-Sky" : null,
            "Arceus" => form is > 0 and < 18 && Arceus[form].Length > 0 ? $"Arceus-{Arceus[form]}" : null,
            _ => null
        };
        return name != null && PokemonDatabase.Get(species)?.Forms?.Any(f => f.Name == name) == true ? name : null;
    }
}
