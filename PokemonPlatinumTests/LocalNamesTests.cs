using System.Text.Json;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 17 · N3: the names of species, moves, abilities, items, types and natures in every language PokeAPI names
/// them in (<c>Data/text/&lt;tag&gt;/names.json</c>, from the importer), the display names that read them, a Pokémon
/// without a nickname, the Pokédex entry composed at run time and the search's groups by language. Changing the
/// language is the whole process's, so the class runs alone and puts English back.
/// </summary>
[Collection("Shared clocks and dice")]
public class LocalNamesTests
{
    private static readonly string[] Written = { "de", "es", "fr", "it", "ja", "ko", "zh-Hans", "zh-Hant" };

    private static T InLanguage<T>(string tag, Func<T> read)
    {
        Text.Use(tag);
        try { return read(); }
        finally { Text.Use(Text.English); }
    }

    // ------------------------------------------------------------------ the files

    [Fact]
    public void EveryLanguagePokeApiNamesThingsInHasANamesFileAndEveryNameInItIsSomethingTheDataHas()
    {
        var species = PokemonDatabase.GetAll().Select(s => s.Name).ToHashSet();
        var forms = PokemonDatabase.GetAll().SelectMany(s => s.Forms ?? new()).Select(f => f.Name).ToHashSet();
        var moves = MoveDatabase.GetAll().Select(m => m.Name).ToHashSet();
        var abilities = AbilityDatabase.GetAll().Select(a => a.Name).ToHashSet();
        var items = ItemDatabase.GetAll().Select(i => i.Name).ToHashSet();
        var types = Enum.GetNames<PokemonType>().ToHashSet();
        var natures = Enum.GetNames<Nature>().ToHashSet();

        foreach (string tag in Written)
        {
            Assert.True(LocalNames.Has(tag), $"{tag} has no names file");
            Assert.NotNull(Language.Of(tag));
            void AllIn(string section, HashSet<string> known)
            {
                var unknown = LocalNames.Section(tag, section).Keys.Where(k => !known.Contains(k)).ToList();
                Assert.True(unknown.Count == 0, $"{tag}/{section} names what the data hasn't: {string.Join(", ", unknown.Take(5))}");
            }
            AllIn("species", species);
            AllIn("categories", species);
            AllIn("forms", forms);
            AllIn("moves", moves);
            AllIn("abilities", abilities);
            AllIn("items", items);
            AllIn("types", types);
            AllIn("natures", natures);
            AllIn("descriptions.moves", moves);
            AllIn("descriptions.abilities", abilities);
            AllIn("descriptions.items", items);

            // Every species and every type and nature has a name in every language PokeAPI has them in
            Assert.Equal(species.Count, LocalNames.Section(tag, "species").Count);
            Assert.Equal(18, LocalNames.Section(tag, "types").Count);
            Assert.Equal(25, LocalNames.Section(tag, "natures").Count);
        }
    }

    [Fact]
    public void TheCountsOfFrenchAreWhatTheImporterFound()
    {
        // PokeAPI's French is the one language with prose for moves, abilities and items (docs/mechanics/coverage.md has every language)
        Assert.Equal((1025, 901, 310, 899), (LocalNames.Section("fr", "species").Count, LocalNames.Section("fr", "moves").Count,
            LocalNames.Section("fr", "abilities").Count, LocalNames.Section("fr", "items").Count));
        // (plan 06 · R29 brought eleven items in: the key items that change a form and Legends: Arceus's three)
        Assert.Equal((764, 305, 536), (LocalNames.Section("fr", "descriptions.moves").Count, LocalNames.Section("fr", "descriptions.abilities").Count,
            LocalNames.Section("fr", "descriptions.items").Count));
        Assert.Equal(191, LocalNames.Section("de", "descriptions.abilities").Count);
        Assert.Empty(LocalNames.Section("es", "descriptions.moves"));
    }

    [Fact]
    public void RomanianHasNoNamesOfPokeApisButHandWrittenOnesOfItsOwnAndKeepsTheSpeciesEnglishNames()
    {
        Assert.NotNull(Language.Of("ro"));
        // PokeAPI has no Romanian, so the importer writes no names.json for it; its names are hand-written names.<kind>.json (plan 17 · N6)
        Assert.True(LocalNames.Has("ro"));
        Assert.DoesNotContain(LocalNames.FilesOf("ro"), f => Path.GetFileName(f) == LocalNames.FileName);
        Assert.Empty(LocalNames.Section("ro", "species"));
        Assert.Contains(Language.Offered, l => l.Tag == "ro");
        // A species keeps its English name; a move takes the name made up for it
        Assert.Equal(("Bulbasaur", "Izbitură"), InLanguage("ro", () => (PokemonDatabase.Get("Bulbasaur")!.DisplayName, MoveDatabase.Get("Tackle")!.DisplayName)));
    }

    [Fact]
    public void AHandWrittenFileIsReadAfterTheImportersAndItsEntriesWin()
    {
        // FilesOf puts names.json first and the hand-written ones after it by name, which is the order Read takes them in
        foreach (var tag in Written.Append("ro"))
        {
            var files = LocalNames.FilesOf(tag).Select(Path.GetFileName).ToList();
            Assert.All(files.Skip(1), f => Assert.Matches("^names\\..+\\.json$", f!));
            if (files.Contains(LocalNames.FileName)) Assert.Equal(LocalNames.FileName, files[0]);
            Assert.Equal(files.Skip(1).OrderBy(f => f, StringComparer.Ordinal), files.Skip(1));
        }
        Assert.Equal("names.moves.json", LocalNames.OwnFileName("moves"));
    }

    [Fact]
    public void ANameThatTheFileLacksIsTheEnglishOne()
    {
        // ko has no item prose at all and fewer item names than there are items
        Text.Use("ko");
        try
        {
            var missing = ItemDatabase.GetAll().First(i => !LocalNames.Section("ko", "items").ContainsKey(i.Name));
            Assert.Equal(missing.Name, missing.DisplayName);
            Assert.Equal(missing.Description, missing.DisplayDescription);
            Assert.Equal(PokemonDatabase.Get("Pikachu")!.DisplayName, LocalNames.Section("ko", "species")["Pikachu"]);
        }
        finally { Text.Use(Text.English); }
    }

    // ------------------------------------------------------------------ what is shown

    [Fact]
    public void FrenchNamesTheSpeciesTheMoveTheItemTheAbilityTheTypeAndTheNatureWithNoCodeWrittenForFrench()
    {
        Assert.Equal(("Bulbasaur", "Tackle", "Fire", "Hardy"), (PokemonDatabase.Get("Bulbasaur")!.DisplayName, MoveDatabase.Get("Tackle")!.DisplayName, Words.Type(PokemonType.Fire), Words.Nature(Nature.Hardy)));
        var seen = InLanguage("fr", () => (
            PokemonDatabase.Get("Bulbasaur")!.DisplayName, MoveDatabase.Get("Tackle")!.DisplayName, ItemDatabase.Get("Potion")!.DisplayName,
            AbilityDatabase.Get("Overgrow")!.DisplayName, Words.Type(PokemonType.Fire), Words.Nature(Nature.Hardy),
            MoveDatabase.Get("Tackle")!.DisplayDescription, PokemonDatabase.Get("Bulbasaur")!.DisplayKind));
        Assert.Equal(("Bulbizarre", "Charge", "Potion", "Engrais", "Feu", "Hardi"), (seen.Item1, seen.Item2, seen.Item3, seen.Item4, seen.Item5, seen.Item6));
        Assert.NotEqual(MoveDatabase.Get("Tackle")!.Description, seen.Item7);
        Assert.Equal("Pokémon Graine", seen.Item8);
        Assert.Equal("Seed Pokémon", PokemonDatabase.Get("Bulbasaur")!.DisplayKind);
    }

    [Fact]
    public void AFormWithoutANameOfItsOwnPutsTheSpeciesLocalNameBeforeTheEnglishWords()
    {
        var charizard = PokemonDatabase.Get("Charizard")!;
        var form = charizard.Form("Charizard-Mega-X")!;
        Assert.Equal("Charizard-Mega-X", form.DisplayName(charizard.Name));
        string french = InLanguage("fr", () => form.DisplayName(charizard.Name));
        Assert.StartsWith("Méga-Dracaufeu", french);
    }

    [Fact]
    public void ThePseudoLanguageBracketsEveryNameAsItBracketsEveryString()
    {
        var seen = InLanguage(Text.Pseudo, () => (
            PokemonDatabase.Get("Bulbasaur")!.DisplayName, MoveDatabase.Get("Tackle")!.DisplayName, ItemDatabase.Get("Potion")!.DisplayName,
            AbilityDatabase.Get("Overgrow")!.DisplayName, new Pokemon(PokemonDatabase.Get("Bulbasaur")!, 5).DisplayName));
        Assert.Equal(("[Bulbasaur]", "[Tackle]", "[Potion]", "[Overgrow]", "[Bulbasaur]"), seen);
    }

    // ------------------------------------------------------------------ nicknames

    [Fact]
    public void APokemonWithoutANicknameIsCalledWhatItsSpeciesIsCalledInTheChosenLanguage()
    {
        var bulbasaur = new Pokemon(PokemonDatabase.Get("Bulbasaur")!, 5);
        Assert.Equal("", bulbasaur.Nickname);
        Assert.Equal("Bulbasaur", bulbasaur.DisplayName);
        Assert.Equal("Bulbizarre", InLanguage("fr", () => bulbasaur.DisplayName));
        bulbasaur.Nickname = "Sprout";
        Assert.Equal("Sprout", InLanguage("fr", () => bulbasaur.DisplayName));
    }

    [Fact]
    public void ASaveWithAPokemonNamedAfterItsSpeciesLoadsWithNoNicknameAndNeverBecomesOneAnotherOnEvolving()
    {
        var saved = SavedPokemonData.FromPokemon(new Pokemon(PokemonDatabase.Get("Turtwig")!, 8));
        saved.Nickname = "Turtwig"; // what every older save wrote in for a Pokémon nobody had named
        var loaded = saved.ToPokemon(Ruleset.Platinum);
        Assert.Equal("", loaded.Nickname);
        Assert.Equal("Turtwig", loaded.DisplayName);
        loaded.EvolveInto(PokemonDatabase.Get("Grotle")!);
        Assert.Equal("", loaded.Nickname);
        Assert.Equal("Grotle", loaded.DisplayName);

        var named = SavedPokemonData.FromPokemon(new Pokemon(PokemonDatabase.Get("Turtwig")!, 8) { Nickname = "Moss" }).ToPokemon(Ruleset.Platinum);
        Assert.Equal("Moss", named.Nickname);
        named.EvolveInto(PokemonDatabase.Get("Grotle")!);
        Assert.Equal("Moss", named.DisplayName);
    }

    // ------------------------------------------------------------------ the Pokédex entry

    [Fact]
    public void TheEntryComposedInEnglishIsTheSentenceTheImporterWroteForAll1025()
    {
        var all = PokemonDatabase.GetAll().ToList();
        Assert.Equal(1025, all.Count);
        var wrong = all.Where(s => DexText.Compose(s, all) != s.DexEntry).Select(s => s.Name).ToList();
        Assert.True(wrong.Count == 0, $"{wrong.Count} entries differ, the first: {wrong.FirstOrDefault()}: {(wrong.Count > 0 ? DexText.Compose(all.First(s => s.Name == wrong[0])) + " | " + all.First(s => s.Name == wrong[0]).DexEntry : "")}");
    }

    [Fact]
    public void TheEntryUnderAnotherLanguageUsesItsNamesAndItsDecimalMark()
    {
        var bulbasaur = PokemonDatabase.Get("Bulbasaur")!;
        string french = InLanguage("fr", () => DexText.Compose(bulbasaur));
        Assert.Contains("Bulbizarre", french);
        Assert.Contains("Herbizarre", french);
        Assert.Contains("0,7 m", french);
        string pseudo = InLanguage(Text.Pseudo, () => DexText.Compose(bulbasaur));
        Assert.StartsWith("[", pseudo);
    }

    // ------------------------------------------------------------------ plurals and groups

    [Fact]
    public void RomanianChoosesOneFewAndOther()
    {
        var romanian = Language.Of("ro")!;
        Assert.Equal("one", romanian.FormFor(1));
        Assert.All(new long[] { 0, 2, 7, 19, 101, 102, 119 }, n => Assert.Equal("few", romanian.FormFor(n)));
        Assert.All(new long[] { 20, 21, 100, 120, 1000 }, n => Assert.Equal("other", romanian.FormFor(n)));
    }

    [Fact]
    public void TheSearchFilesKatakanaUnderTheHiraganaRowAndHangulUnderItsInitialConsonant()
    {
        var japanese = Language.Of("ja")!;
        Assert.Equal(10, japanese.NameGroups!.Length);
        Assert.Equal('ふ', PokedexSearch.GroupLetter("フシギダネ"));
        Assert.Equal('か', PokedexSearch.GroupLetter("ガブリアス"));
        Assert.Equal('ㅎ', PokedexSearch.GroupLetter("한카리아스"));
        Assert.Equal('N', PokedexSearch.GroupLetter("Nidoran"));
        Assert.Equal(("あいうえおぁぃぅぇぉ", 14), InLanguage("ja", () => (PokedexSearch.NameGroups[0], 14)));
        Assert.Equal(9, PokedexSearch.NameGroups.Count);
        Assert.Equal(14, InLanguage("ko", () => PokedexSearch.NameGroups.Count));
        // Every species of the Japanese names falls in some group, and the alphabetical order is the culture's
        Text.Use("ja");
        try
        {
            var kana = LocalNames.Section("ja", "species").Values.Where(n => n[0] is >= 'ァ' and <= 'ヶ').ToList();
            Assert.NotEmpty(kana);
            Assert.All(kana, n => Assert.Contains(PokedexSearch.NameGroups, g => g.Contains(PokedexSearch.GroupLetter(n))));
            Assert.NotNull(PokedexSearch.Collation());
        }
        finally { Text.Use(Text.English); }
    }
}
