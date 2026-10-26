using System.Text.Json;
using System.Text.Json.Nodes;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 16 · T14, the data layers: the game's own data first and each pack after it. The fixture packs of
/// <c>Fixtures/packs</c> (alpha; beta, which loads after alpha; and broken, whose manifest has no credits) are handed
/// to the layers' reads by hand, never put in use for the whole process, so these tests run beside any other.
/// </summary>
public class DataLayerTests
{
    private static readonly string FixturePacks = Path.Combine(AppContext.BaseDirectory, "Fixtures", "packs");

    private static List<DataLayer> Discover(List<string> problems, params PackChoice[] chosen) =>
        DataLayers.Discover(new[] { FixturePacks }, chosen, problems);

    [Fact]
    public void WithNoPackEveryPathIsTheGamesOwn()
    {
        Assert.Empty(DataLayers.Packs);
        foreach (string file in new[] { "species.json", "scripts/common.txt", "music", "world/sinnoh/world.json" })
            Assert.Equal(Path.Combine(GameDataFiles.Directory, file), GameDataFiles.PathOf(file));
        Assert.Empty(DataLayers.PatchesOf("species.json"));
        // The scripts and the songs read across the layers are the folder's own files, in the order they always were
        var scripts = DataLayers.FilesIn(ScriptLibrary.Folder, "*.txt").Select(f => f.Path).ToList();
        Assert.Equal(Directory.GetFiles(Path.Combine(GameDataFiles.Directory, ScriptLibrary.Folder), "*.txt").OrderBy(p => p, StringComparer.Ordinal), scripts);
        Assert.Equal(File.ReadAllText(GameDataFiles.PathOf("species.json")), DataLayers.ReadPatched("species.json"));
    }

    [Fact]
    public void ThePacksLoadInTheOptionsOrderEachAfterThoseItNames()
    {
        var problems = new List<string>();
        // The options name beta first, but beta's manifest loads it after alpha
        var layers = Discover(problems, new PackChoice { Name = "beta" }, new PackChoice { Name = "alpha" });
        Assert.Equal(new[] { "alpha", "beta" }, layers.Select(l => l.Pack!.Name));
        Assert.Equal("2.1", layers[1].Pack!.Version);
        Assert.Equal(new[] { "alpha" }, layers[1].Pack!.After);

        // A pack the options turn off is left out
        var off = Discover(new List<string>(), new PackChoice { Name = "alpha", Enabled = false });
        Assert.Equal(new[] { "beta" }, off.Select(l => l.Pack!.Name));
    }

    [Fact]
    public void ABadManifestIsNamedAndLeftOut()
    {
        var problems = new List<string>();
        var layers = Discover(problems);
        Assert.DoesNotContain(layers, l => l.Pack!.Name == "broken");
        var problem = Assert.Single(problems);
        Assert.Contains(Path.Combine("broken", DataLayers.ManifestFile), problem);
        Assert.Contains("credits", problem);

        Assert.Throws<PackException>(() => DataLayers.ReadManifest(Path.Combine(FixturePacks, "no-such-pack")));
    }

    [Fact]
    public void ALineASongAndAStatAreReadFromThePacksInOrder()
    {
        var layers = Discover(new List<string>());

        // The line: the dolls' script is the last pack's, beta's
        var scripts = DataLayers.FilesIn(ScriptLibrary.Folder, "*.txt", over: layers);
        var dolls = scripts.Single(f => f.Key == "rustboro_city_flat_1_2f.txt");
        Assert.StartsWith(Path.Combine(FixturePacks, "beta"), dolls.Path);
        var library = ScriptLibrary.FromSources(scripts.Select(f => (Path.GetFileNameWithoutExtension(f.Path), File.ReadAllText(f.Path))).ToArray());
        Assert.Contains("The beta pack's doll bows.", string.Join("\n", library.Find("rustboro_city_flat_1_2f.Doll")!.Code.SelectMany(i => i.Lines)));
        // Every other script is the game's
        Assert.Equal(DataLayers.FilesIn(ScriptLibrary.Folder, "*.txt").Count, scripts.Count);

        // The song: only alpha has one, so alpha's
        var lake = DataLayers.FilesIn("music", "*.mml", recursive: true, over: layers).Single(f => f.Key == "sinnoh/lake.mml");
        Assert.StartsWith(Path.Combine(FixturePacks, "alpha"), lake.Path);
        Assert.Equal("Pack Lake", Mml.Parse("sinnoh/lake", File.ReadAllText(lake.Path)).Title);

        // The stats: alpha's HP and beta's Attack, laid over the game's Bulbasaur in turn; Ivysaur's category taken away
        var species = JsonSerializer.Deserialize<List<PokemonSpecies>>(DataLayers.ReadPatched("species.json", layers), GameDataFiles.Json)!;
        var game = GameDataFiles.Load<List<PokemonSpecies>>("species.json");
        var bulbasaur = species.Single(s => s.Name == "Bulbasaur");
        var own = game.Single(s => s.Name == "Bulbasaur");
        Assert.Equal(99, bulbasaur.BaseHP);
        Assert.Equal(77, bulbasaur.BaseAttack);
        Assert.Equal(own.BaseDefense, bulbasaur.BaseDefense);
        Assert.Equal(game.Count, species.Count);
        Assert.NotEqual(own.BaseHP, bulbasaur.BaseHP);
    }

    [Fact]
    public void APatchChangesFieldsTakesThemAwayAndAddsEntries()
    {
        var file = JsonNode.Parse("""[ { "name": "A", "x": 1, "y": 2 }, { "name": "B", "x": 3 } ]""")!;
        var patch = JsonNode.Parse("""[ { "name": "A", "x": 5, "y": null }, { "name": "C", "x": 7 } ]""")!;
        var patched = DataLayers.Patch(file, patch, "name").AsArray();
        Assert.Equal(3, patched.Count);
        Assert.Equal(5, (int)patched[0]!["x"]!);
        Assert.Null(patched[0]!["y"]);
        Assert.Equal(3, (int)patched[1]!["x"]!);
        Assert.Equal("C", (string)patched[2]!["name"]!);

        // An object (characters.json) is merged key by key, all the way down
        var looks = JsonNode.Parse("""{ "looks": { "player": { "hat": "red", "hair": "brown" } } }""")!;
        var change = JsonNode.Parse("""{ "looks": { "player": { "hat": null, "hair": "black" }, "new": { "hat": "blue" } } }""")!;
        var merged = DataLayers.Patch(looks, change, "name");
        Assert.Null(merged["looks"]!["player"]!["hat"]);
        Assert.Equal("black", (string)merged["looks"]!["player"]!["hair"]!);
        Assert.Equal("blue", (string)merged["looks"]!["new"]!["hat"]!);

        // An entry with no name can't be patched, and says so
        Assert.Throws<InvalidDataException>(() => DataLayers.Patch(JsonNode.Parse("[]")!, JsonNode.Parse("""[ { "x": 1 } ]""")!, "name"));
    }

    [Fact]
    public void AWorldsIndexIsAlwaysTheGamesOwn()
    {
        var layers = Discover(new List<string>());
        Assert.True(DataLayers.IsFixed("world/sinnoh/world.json"));
        Assert.Equal(Path.Combine(GameDataFiles.Directory, "world/sinnoh/world.json"), DataLayers.PathOf("world/sinnoh/world.json", layers));
    }

    [Fact]
    public void PacksNoneIsASwitch()
    {
        Assert.True(CommandLine.Parse(new[] { "--packs", "none" }).NoPacks);
        Assert.False(CommandLine.Parse(new[] { "--packs", "some" }).IsValid);
        Assert.False(CommandLine.Parse(Array.Empty<string>()).NoPacks);
    }
}
