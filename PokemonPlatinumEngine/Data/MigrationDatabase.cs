using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// The tables the conversion of a foreign Pokémon needs (plan 08 · P20), imported by <c>tools/DataImporter</c>
/// (<c>Importer.Migration.cs</c>) from the pinned pokeplatinum commit's <c>src/main_menu/gba_pokemon.c</c>,
/// <c>gba_migrator.c</c> and <c>gba_convert_string.c</c>: numbers and characters, never a sprite, an icon, a sound or
/// a line of the games.
/// </summary>
public sealed class MigrationData
{
    /// <summary>The GBA's species numbers that differ from the National Pokédex's (<c>sGBAToDSSpecies</c>): Hoenn's are 277 to 411 there.</summary>
    public Dictionary<int, int> GbaSpecies { get; set; } = new();

    /// <summary>The National Pokédex numbers of the species that gained a second ability in Generation 4 (<c>sSpeciesWithNewAbilities</c>), which a migrated Pokémon of them takes the first of.</summary>
    public List<int> NewAbilities { get; set; } = new();

    /// <summary>The moves that refuse a Pokémon (<c>sGBAHMMoves</c>), by name.</summary>
    public List<string> HmMoves { get; set; } = new();

    /// <summary>The GBA item numbers that refuse a Pokémon holding them (<c>sInvalidGBAItems</c>).</summary>
    public List<int> InvalidItems { get; set; } = new();

    /// <summary>The GBA's item numbers and the items they are here (<c>Item_FromGBAID</c>: each item's <c>gbaID</c>), by name.</summary>
    public Dictionary<int, string> GbaItems { get; set; } = new();

    /// <summary>The GBA's character table for each language (<c>sGBACharToDSChars</c> by <c>GBA_ConvertStringToDS</c>), by the language's number: the character a code stands for.</summary>
    public Dictionary<int, List<string>> GbaChars { get; set; } = new();

    /// <summary>The DS's character table (<c>tools/msgenc/charmap.txt</c>): the character a code stands for; codes that are commands or have no character are left out.</summary>
    public Dictionary<int, string> DsChars { get; set; } = new();
}

/// <summary>What <see cref="MigrationData"/> says, loaded once from <c>migration.json</c>.</summary>
public static class MigrationDatabase
{
    public const string FileName = "migration.json";

    private static readonly Lazy<MigrationData> Loaded = new(() => GameDataFiles.Load<MigrationData>(FileName));
    private static readonly Lazy<HashSet<int>> NewAbilitySet = new(() => Loaded.Value.NewAbilities.ToHashSet());
    private static readonly Lazy<HashSet<int>> InvalidItemSet = new(() => Loaded.Value.InvalidItems.ToHashSet());

    public static MigrationData Data => Loaded.Value;

    /// <summary>GBA language numbers (<c>LANGUAGE_*</c>): Japanese, English, French, Italian, German and Spanish.</summary>
    public const int Japanese = 1, English = 2, French = 3, Italian = 4, German = 5, Spanish = 7;

    /// <summary>
    /// A GBA species number as the DS games number it (<c>GBAPokemon_ConvertSpeciesToDS</c>): those below Treecko are
    /// the same, and the rest are looked up (one the table lacks stays as it is).
    /// </summary>
    public static int SpeciesFromGba(int gbaSpecies) =>
        gbaSpecies < 277 ? gbaSpecies : Data.GbaSpecies.TryGetValue(gbaSpecies, out int ds) ? ds : gbaSpecies;

    /// <summary>
    /// Whether a GBA species number is one the DS games have (<c>IsGBASpeciesInvalid</c>): Bulbasaur to Celebi and
    /// Treecko to Chimecho; the numbers between (Unown B to Z in the GBA's first numbering), Egg and beyond are not.
    /// </summary>
    public static bool IsGbaSpeciesReal(int gbaSpecies) => gbaSpecies is >= 1 and <= 251 or >= 277 and <= 411;

    public static bool GainedAnAbility(int nationalNumber) => NewAbilitySet.Value.Contains(nationalNumber);

    public static bool IsInvalidGbaItem(int gbaItem) => InvalidItemSet.Value.Contains(gbaItem);

    /// <summary>The item a GBA item number is here, or null for none or one this game has no item for.</summary>
    public static string? ItemFromGba(int gbaItem) => Data.GbaItems.TryGetValue(gbaItem, out var name) ? name : null;

    /// <summary>
    /// A GBA string as text (<c>GBA_ConvertStringToDS</c> and the DS's character table): up to the terminator
    /// (<c>0xFF</c>) or the length; a code the table doesn't have makes the whole string question marks, as in the original.
    /// </summary>
    public static string TextFromGba(ReadOnlySpan<byte> bytes, int language)
    {
        var table = Data.GbaChars.TryGetValue(language, out var own) ? own : Data.GbaChars[English];
        var text = new System.Text.StringBuilder();
        foreach (byte b in bytes)
        {
            if (b == 0xFF) break;
            if (b >= table.Count) return new string('?', Math.Min(bytes.Length, 10));
            text.Append(table[b]);
        }
        return text.ToString();
    }

    /// <summary>A DS string (16-bit codes, <c>0xFFFF</c> ends it) as text; a code with no character is left out.</summary>
    public static string TextFromDs(ReadOnlySpan<ushort> codes)
    {
        var text = new System.Text.StringBuilder();
        foreach (ushort code in codes)
        {
            if (code == 0xFFFF) break;
            if (Data.DsChars.TryGetValue(code, out var character)) text.Append(character);
        }
        return text.ToString();
    }
}
