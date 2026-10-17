using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests.Golden;

/// <summary>
/// Saves of every version the game has written (plan 24 · X3): <c>Fixtures/saves</c>, a file or more for each
/// <see cref="SaveData.WorldVersion"/> and each <see cref="SaveData.StoryVersion"/> from 0 to today's, named
/// <c>w&lt;world&gt;-s&lt;story&gt;-&lt;moment&gt;.json</c>. The older ones were written by hand from the saves the tests
/// once built inline, with the fields their versions had; today's come from the harness's <c>saves</c> mode. Each is
/// read as <see cref="SaveManager.LoadGame"/> reads it, put on today's maps as the game puts it
/// (<see cref="SaveData.PlaceOn"/>), its Pokémon made as the game makes them and its story brought up to date as the
/// game brings it (<see cref="StoryMigration.Upgrade"/>), and what each comes to is held to <c>saves.txt</c>, so a
/// change to the migration shows in <c>git diff</c> what it does to every old save.
/// </summary>
public class SaveCorpusTests
{
    /// <summary>Where the saves are: the test project's <c>Fixtures/saves</c>, copied beside the tests.</summary>
    private static string Folder => Path.Combine(AppContext.BaseDirectory, "Fixtures", "saves");

    private static readonly Regex FileName = new(@"^w(\d+)-s(\d+)-[a-z0-9]+(-[a-z0-9]+)*\.json$");

    /// <summary>Every save of the corpus by its file's name, in order.</summary>
    private static List<(string Name, SaveData Save)> Corpus()
    {
        if (!Directory.Exists(Folder)) return new();
        return Directory.GetFiles(Folder, "*.json").Order(StringComparer.Ordinal)
            .Select(path => (Path.GetFileName(path), SaveManager.Read(File.ReadAllText(path)) ?? throw new InvalidDataException($"{Path.GetFileName(path)} reads as nothing")))
            .ToList();
    }

    [Fact]
    public void EveryVersionHasASave()
    {
        var saves = Corpus();
        foreach (var (name, save) in saves)
        {
            var match = FileName.Match(name);
            Assert.True(match.Success, $"{name} isn't named w<world>-s<story>-<moment>.json");
            Assert.True(int.Parse(match.Groups[1].Value) == save.WorldVersion, $"{name} says WorldVersion {save.WorldVersion}");
            Assert.True(int.Parse(match.Groups[2].Value) == save.StoryVersion, $"{name} says StoryVersion {save.StoryVersion}");
            Assert.InRange(save.WorldVersion, 0, SaveData.CurrentWorld);
            Assert.InRange(save.StoryVersion, 0, StoryState.CurrentVersion);
        }
        // A version that rose brings its saves (the harness's `saves` writes today's into <out dir>/saves)
        for (int world = 0; world <= SaveData.CurrentWorld; world++)
            Assert.True(saves.Any(s => s.Save.WorldVersion == world),
                $"no save of WorldVersion {world} in PokemonPlatinumTests/Fixtures/saves: run the harness's `saves` mode and copy its files there");
        for (int story = 0; story <= StoryState.CurrentVersion; story++)
            Assert.True(saves.Any(s => s.Save.StoryVersion == story),
                $"no save of StoryVersion {story} in PokemonPlatinumTests/Fixtures/saves: run the harness's `saves` mode and copy its files there");
    }

    [Fact]
    public void EverySaveWakesOnATileOfAMapOfToday()
    {
        foreach (var (name, save) in Corpus())
        {
            var spot = save.PlaceOn(MapDatabase.Get);
            Assert.True(MapDatabase.MapNames.Contains(spot.Map, StringComparer.OrdinalIgnoreCase), $"{name} wakes on {spot.Map}, which is no map of today");
            var map = MapDatabase.Get(spot.Map);
            Assert.True(WhyNotStandable(map, spot.X, spot.Y, save.Travel) is null, $"{name} wakes at {spot.X},{spot.Y} of {spot.Map} {WhyNotStandable(map, spot.X, spot.Y, save.Travel)}");
        }
    }

    /// <summary>Why someone travelling so can't stand on a tile; null when they can (whoever else stands there is the story's).</summary>
    private static string? WhyNotStandable(Map map, int x, int y, TravelMode travel)
    {
        if (!map.InBounds(x, y)) return "off the map";
        if (map.IsSolid(x, y)) return "in something solid";
        bool water = map.IsDeepWater(x, y);
        if (travel == TravelMode.Surfing) return water ? null : "surfing on dry ground";
        if (map.IsLedge(x, y)) return "on a ledge";
        if (water && map.DeckAt(x, y) == null) return "on foot on the water";
        return null;
    }

    /// <summary>Every Pokémon a save keeps: the team, the boxes (in places or, from before, in one list) and the Day Care.</summary>
    private static IEnumerable<(string Where, SavedPokemonData Pokemon)> Owned(SaveData save)
    {
        for (int i = 0; i < save.Party.Count; i++) yield return ($"team #{i}", save.Party[i]);
        for (int i = 0; i < save.BoxStorage.Count; i++) yield return ($"PC #{i}", save.BoxStorage[i]);
        if (save.Boxes != null)
            for (int b = 0; b < save.Boxes.Boxes.Count; b++)
                foreach (var boxed in save.Boxes.Boxes[b].Pokemon) yield return ($"box {b + 1} #{boxed.Slot}", boxed.Pokemon);
        if (save.DayCare != null)
            for (int i = 0; i < save.DayCare.Places.Count; i++)
                if (save.DayCare.Places[i]?.Pokemon is { } left) yield return ($"Day Care #{i}", left);
    }

    [Fact]
    public void EveryPokemonOfEverySaveIsOneTheDataHas()
    {
        foreach (var (name, save) in Corpus())
            foreach (var (where, saved) in Owned(save))
            {
                string who = $"{name}, {where} ({saved.SpeciesName})";
                var species = PokemonDatabase.Get(saved.SpeciesName);
                Assert.True(species != null, $"{who}: no such species");
                if (saved.Form != null)
                    Assert.True(PokemonDatabase.SpeciesOfForm(saved.Form)?.Name == species!.Name, $"{who}: {saved.Form} is no form of it");
                Assert.True(saved.Ability == null || AbilityDatabase.Get(saved.Ability) != null, $"{who}: no ability is called {saved.Ability}");
                Assert.True(saved.HeldItem == null || ItemDatabase.Get(saved.HeldItem) != null, $"{who}: no item is called {saved.HeldItem}");
                Assert.True(saved.Moves.Count <= 4, $"{who}: {saved.Moves.Count} moves");
                foreach (var move in saved.Moves)
                    Assert.True(string.Equals(MoveDatabase.Get(move.MoveName).Name, move.MoveName, StringComparison.OrdinalIgnoreCase), $"{who}: no move is called {move.MoveName}");
                foreach (int iv in new[] { saved.IvHP, saved.IvAttack, saved.IvDefense, saved.IvSpAttack, saved.IvSpDefense, saved.IvSpeed })
                    Assert.True(iv is >= 0 and <= 31, $"{who}: an IV of {iv}");

                var pokemon = saved.ToPokemon();
                Assert.Equal(species!.Name, pokemon.Species.Name);
                Assert.InRange(pokemon.Level, 1, 100);
                Assert.True(pokemon.AbilityName == null || AbilityDatabase.Get(pokemon.AbilityName) != null, $"{who}: made with the ability {pokemon.AbilityName}");
                Assert.InRange(pokemon.CurrentHP, 0, pokemon.MaxHP);
                Assert.InRange(pokemon.Moves.Count, 1, 4);
            }
    }

    /// <summary>A save's story and bag as the game brings them up to date when it loads the save (<c>GameEngine.ApplySaveData</c>).</summary>
    private static (StorySnapshot Before, StorySnapshot After, Inventory Bag) Upgraded(SaveData save)
    {
        var story = new StoryState();
        story.Restore(save.ToStory());
        var before = story.Snapshot();
        // The team and the boxes; a save with nobody on the team is given a Turtwig, as the game gives it
        var owned = save.Party.Concat(save.BoxStorage).Concat(save.Boxes?.Boxes.SelectMany(b => b.Pokemon.Select(p => p.Pokemon)) ?? Enumerable.Empty<SavedPokemonData>())
            .Select(p => p.ToPokemon()).ToList();
        if (save.Party.Count == 0) owned.Insert(0, new Pokemon(PokemonDatabase.Get("Turtwig")!, 5, new Random(0)));
        var bag = BagOf(save);
        StoryMigration.Upgrade(story, save.StoryVersion, owned, ScriptLibrary.Default, bag);
        return (before, story.Snapshot(), bag);
    }

    private static Inventory BagOf(SaveData save)
    {
        var bag = new Inventory();
        foreach (var item in save.Inventory)
            if (ItemDatabase.Get(item.ItemName) is { } data) bag.AddItem(data, item.Quantity);
        return bag;
    }

    private static string Items(Inventory bag) => string.Join(", ", bag.AllItems.Select(i => $"{i.Name} x{i.Quantity}"));

    [Fact]
    public void EverySavesStoryIsBroughtUpToDateTheSameEachTime()
    {
        foreach (var (name, save) in Corpus())
        {
            var first = Upgraded(save);
            var again = Upgraded(save);
            Assert.True(JsonSerializer.Serialize(first.After) == JsonSerializer.Serialize(again.After), $"{name}: its story comes up to date differently the second time");
            Assert.True(Items(first.Bag) == Items(again.Bag), $"{name}: its bag comes up to date differently the second time");
            // And one of today is left as it is
            if (save.StoryVersion == StoryState.CurrentVersion)
                Assert.True(JsonSerializer.Serialize(first.Before) == JsonSerializer.Serialize(first.After), $"{name}: a save of today was changed as it loaded");
        }
    }

    [Fact]
    public void EverySaveLoadsAsItDid()
    {
        var corpus = Corpus();
        var text = new StringBuilder("# Every save of PokemonPlatinumTests/Fixtures/saves as the game loads it (plan 24 · X3), made by SaveCorpusTests. " +
            $"Rewritten by: {GoldenFile.AcceptCommand}\n" +
            "# Where it wakes, its team and boxes, its bag, and what bringing its story up to date changed ('+'/'-' a flag, '=' a variable).\n");
        foreach (var (name, save) in corpus)
        {
            text.Append($"{GoldenFile.BlockMark}{name}\n");
            text.Append($"world {save.WorldVersion}, story {save.StoryVersion}, {save.Rules} rules, {save.Look} {save.PlayerName}, rival {save.RivalName}, ${save.Money}, badges {Convert.ToString(save.Badges, 2)}\n");
            var spot = save.PlaceOn(MapDatabase.Get);
            string height = save.PlayerHeight is { } h ? $" at height {h:0.##}" : "";
            text.Append($"wakes on {spot.Map} {spot.X},{spot.Y} facing {spot.Facing}, {save.Travel}{height}" +
                (save.Safari is { } safari ? $", a Safari Game with {safari.Balls} balls and {safari.Steps} steps left" : "") + "\n");
            foreach (var (where, saved) in Owned(save))
            {
                var p = saved.ToPokemon();
                string form = p.Form != null ? $" ({p.Form})" : "";
                string moves = string.Join(", ", p.Moves.Select(m => $"{m.Name} {m.CurrentPP}/{m.MaxPP}"));
                text.Append($"  {where}: {p.Species.Name}{form} Lv{p.Level} \"{p.DisplayName}\" {p.Gender} {p.Nature}{(p.IsEgg ? " Egg" : "")}{(p.IsShiny ? " shiny" : "")}, " +
                    $"{p.AbilityName ?? "no ability"}, holds {p.HeldItem?.Name ?? "nothing"}, HP {p.CurrentHP}/{p.MaxHP}, {moves}\n");
            }
            var (before, after, bag) = Upgraded(save);
            string had = Items(BagOf(save)), has = Items(bag);
            text.Append($"bag: {(had.Length > 0 ? had : "nothing")}\n");
            if (has != had) text.Append($"bag after: {(has.Length > 0 ? has : "nothing")}\n");
            var changes = GoldenScriptTests.Changes(before, after).ToList();
            text.Append(changes.Count == 0 ? "upgrade: nothing\n" : $"upgrade from story {save.StoryVersion}: {changes.Count} changes\n");
            foreach (string change in changes) text.Append($"  {change}\n");
        }

        GoldenFile.Check("saves.txt", text.ToString());
    }
}
