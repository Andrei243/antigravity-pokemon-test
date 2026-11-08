using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 23 · Z6: the rest of Akala (Route 6, Royal Avenue, Route 8, the Lush Jungle, Route 7, Wela Volcano Park,
/// Diglett's Tunnel, Route 9, Konikoni City, Memorial Hill, the Akala Outskirts with the Ruins of Life, and Hano Beach),
/// the trials of Kiawe and Mallow with their totems, Olivia's grand trial and its stamp, the island challenge's stamps on
/// the card, and the story from Royal Avenue to the Hano Grand Resort played headless. Floors, not counts, wherever a
/// later session draws more.
/// </summary>
[Collection("Shared clocks and dice")]
public class AlolaAkalaTrialsTests
{
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Alola"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static readonly Lazy<Func<string, Map>> Rooms = new(MapDatabase.Fresh);
    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : Rooms.Value(name);
    private static readonly Lazy<Dictionary<Map, HashSet<(int X, int Y)>>> Walked =
        new(() => WorldWalk.From(MapNamed, RegionDatabase.Get(RegionDatabase.Alola)!.Start!));

    private static readonly string[] Places =
    {
        "akala_route_6", "royal_avenue", "akala_route_8", "lush_jungle", "akala_route_7", "wela_volcano_park",
        "akala_route_9", "konikoni_city", "memorial_hill", "akala_outskirts", "hano_beach"
    };

    private static readonly string[] NewRooms =
    {
        "RoyalAvenue_PokemonCenter_1F", "RoyalAvenue_House", "AkalaRoute8_PokemonCenter_1F", "KonikoniCity_PokemonCenter_1F",
        "KonikoniCity_OliviasShop", "KonikoniCity_House", "AkalaOutskirts_RuinsOfLife", "HanoGrandResort"
    };

    [Fact]
    public void EveryNewPlaceOfAkalaIsReachedOnFootFromTheNewHome()
    {
        var walked = Walked.Value;
        foreach (string key in Places)
            Assert.True(walked.Any(p => p.Key.Name == "Akala" && p.Value.Any(t => p.Key.AreaAt(t.X, t.Y)?.Key == key)), $"{key} is never reached");
        Assert.Contains(walked.Keys, m => m.Name == "DiglettsTunnel");
        foreach (string room in NewRooms) Assert.Contains(walked.Keys, m => m.Name == room);
        // Each trial's site: Kiawe's crater, Mallow's clearing and the Ruins of Life's door
        var akala = walked.Single(p => p.Key.Name == "Akala").Value;
        Assert.Contains((331, 40), akala);
        Assert.Contains((207, 96), akala);
        Assert.Contains((361, 152), akala);
    }

    [Fact]
    public void TheNewSpawnsStandBeforeTheirPokemonCentersDoors()
    {
        var region = RegionDatabase.Get(RegionDatabase.Alola)!;
        foreach (int id in new[] { 20, 21, 22 })
        {
            var spawn = region.File!.Spawns.Single(s => s.Id == id);
            var map = MapNamed(spawn.Map ?? "Melemele");
            Assert.True(map.IsWalkable(spawn.X, spawn.Y), $"spawn {spawn.Id} stands on a blocked tile");
            Assert.Equal(spawn.Room, map.GetWarpAt(spawn.X, spawn.Y - 1)?.TargetMap);
        }
    }

    [Fact]
    public void AlolasBadgesAreTheIslandsStampsAndTheTableCapsByThem()
    {
        var alola = RegionDatabase.Get(RegionDatabase.Alola)!;
        Assert.True(alola.File!.Stamps);
        Assert.False(RegionDatabase.Get(RegionDatabase.Sinnoh)!.File!.Stamps);
        // Every stamp has a look of its own on the card
        foreach (string stamp in alola.File.Badges) Assert.True(PokemonPlatinumEngine.Graphics.BadgeLooks.ByName.ContainsKey(stamp), stamp);
        // The level cap counts stamps (plan 13 · V3's table for Alola)
        var story = new StoryState();
        var run = RunRules.Normal with { LevelCap = true };
        int? none = LevelCaps.For(run, RegionDatabase.Alola, story);
        story.GiveBadge(RegionDatabase.Alola, "Melemele");
        story.GiveBadge(RegionDatabase.Alola, "Akala");
        int? two = LevelCaps.For(run, RegionDatabase.Alola, story);
        Assert.NotNull(none);
        Assert.NotNull(two);
        Assert.True(two > none, $"two stamps cap at {two}, none at {none}");
    }

    [Fact]
    public void TheTrialsTotemsAreFoughtAsTotemsAndGiveTheirCrystalOnce()
    {
        var library = ScriptLibrary.Default;
        foreach (var (file, species) in new[] { ("verdant_cavern", "Gumshoos"), ("brooklet_hill", "Wishiwashi"), ("wela_volcano_park", "Marowak-Alola"), ("lush_jungle", "Lurantis") })
        {
            var totem = library.Find("Totem", file)!;
            var line = totem.Code.Single(i => i.Op == Op.WildBattle);
            Assert.Equal(species, line.Name);
            Assert.NotNull(line.Totem);
            Assert.True(line.Option, $"{file}'s totem can be run from");
        }

        // Kiawe's crystal, once: a second visit finds the totem gone and nothing more given
        var map = MapNamed("Akala");
        var story = new StoryState();
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Litten")!, 22));
        var bag = new Inventory();
        story.SetVar("VAR_ALOLA_AKALA", 2);
        story.Set("FLAG_ALOLA_WELA_TRIAL");
        story.Set("FLAG_ALOLA_WELA_TRIAL_1");
        story.Set("FLAG_ALOLA_WELA_TRIAL_2");
        HeadlessScriptHost Play(string script)
        {
            var host = new HeadlessScriptHost(story, party, bag) { Map = map, Region = RegionDatabase.Alola };
            var runner = new ScriptRunner(library, host);
            runner.Start(library.Find(script, "wela_volcano_park")!, map.Everyone.Single(n => n.Key == "totem" && map.AreaAt(n.GridX, n.GridY)?.Key == "wela_volcano_park"));
            runner.RunToEnd();
            return host;
        }
        var first = Play("Totem");
        Assert.Contains(first.Log, l => l.StartsWith("totembattle Marowak 22 Defense+1"));
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get("Firium Z")!));
        Assert.Equal(3, story.Var("VAR_ALOLA_AKALA"));
        Assert.True(story.Has("FLAG_ALOLA_WELA_TRIAL_DONE"));
        // The totem is hidden by the trial's flag from now on, and Kiawe with it
        var totemPerson = map.Everyone.Single(n => n.Key == "totem" && map.AreaAt(n.GridX, n.GridY)?.Key == "wela_volcano_park");
        Assert.Equal("FLAG_ALOLA_WELA_TRIAL_DONE", totemPerson.HiddenBy);
    }

    [Fact]
    public void TheStoryRunsFromRoyalAvenueToTheHanoGrandResortHeadless()
    {
        var story = new StoryState();
        var party = new Party();
        var bag = new Inventory();
        var library = ScriptLibrary.Default;
        var akala = MapNamed("Akala");
        void Play(string file, string script, string? who = null)
        {
            var host = new HeadlessScriptHost(story, party, bag) { Map = akala, Region = RegionDatabase.Alola };
            var runner = new ScriptRunner(library, host);
            var subject = who == null ? null : akala.Everyone.Single(n => n.Key == who && akala.AreaAt(n.GridX, n.GridY)?.Key == file);
            runner.Start(library.Find(script, file)!, subject);
            runner.RunToEnd();
        }
        string? Next() => Objectives.Current(story, RegionDatabase.Alola)?.Name;

        Play("common", "AlolaNewGame");
        party.Add(new Pokemon(PokemonDatabase.Get("Litten")!, 30));
        story.ChooseStarter("Litten");
        story.SetVar("VAR_ALOLA_STORY", 18);
        story.Set("FLAG_ALOLA_BROOKLET_TRIAL_DONE");
        Assert.Equal("alolaRoyal", Next());

        Play("royal_avenue", "RoyalScene");
        Assert.Equal(1, story.Var("VAR_ALOLA_AKALA"));
        Assert.True(story.Has("FLAG_ALOLA_ROYAL_DONE"));
        Assert.Equal("alolaMotel", Next());

        Play("akala_route_8", "MotelScene");
        Assert.Equal(2, story.Var("VAR_ALOLA_AKALA"));
        Assert.Equal("alolaWela", Next());

        // Mallow's trial waits for Kiawe's
        Play("lush_jungle", "Mallow", "mallow");
        Assert.False(story.Has("FLAG_ALOLA_LUSH_TRIAL"));

        Play("wela_volcano_park", "Kiawe", "kiawe");
        Assert.True(story.Has("FLAG_ALOLA_WELA_TRIAL"));
        // The totem waits for both dancers
        Play("wela_volcano_park", "Totem", "totem");
        Assert.Equal(2, story.Var("VAR_ALOLA_AKALA"));
        Play("wela_volcano_park", "Trial1", "trial_1");
        Play("wela_volcano_park", "Trial2", "trial_2");
        Play("wela_volcano_park", "Totem", "totem");
        Assert.Equal(3, story.Var("VAR_ALOLA_AKALA"));
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get("Firium Z")!));
        Assert.Equal("alolaJungle", Next());

        Play("lush_jungle", "Mallow", "mallow");
        Assert.True(story.Has("FLAG_ALOLA_LUSH_TRIAL"));
        foreach (string ingredient in new[] { "Ingredient1", "Ingredient2", "Ingredient3" }) Play("lush_jungle", ingredient);
        Assert.False(story.Has("FLAG_ALOLA_LUSH_TOTEM"));
        Play("lush_jungle", "Ingredient4");
        Assert.True(story.Has("FLAG_ALOLA_LUSH_TOTEM"));
        Play("lush_jungle", "Totem", "totem");
        Assert.Equal(4, story.Var("VAR_ALOLA_AKALA"));
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get("Grassium Z")!));
        Assert.True(story.Has("FLAG_ALOLA_LUSH_TRIAL_DONE"));
        Assert.Equal("alolaKonikoni", Next());

        Play("konikoni_city", "OliviaMeets");
        Assert.Equal(5, story.Var("VAR_ALOLA_AKALA"));
        // Olivia only sends the player on until the ruins have been visited
        Play("konikoni_city", "Olivia", "olivia");
        Assert.False(story.HasBadge(RegionDatabase.Alola, "Akala"));
        Play("akala_outskirts", "Lillie", "lillie");
        Assert.Equal(6, story.Var("VAR_ALOLA_AKALA"));
        Assert.Equal("alolaOlivia", Next());

        Play("konikoni_city", "Olivia", "olivia");
        Assert.Equal(7, story.Var("VAR_ALOLA_AKALA"));
        Assert.True(story.HasBadge(RegionDatabase.Alola, "Akala"));
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get("Rockium Z")!));
        Assert.True(story.Has("FLAG_ALOLA_RIDE_MACHAMP"));
        Assert.Contains(FieldMove.Strength, RidePager.Granted(story, RegionDatabase.Alola));
        Assert.Equal("alolaHano", Next());

        Play("hano_beach", "HanoScene");
        Assert.Equal(8, story.Var("VAR_ALOLA_AKALA"));
        Assert.True(story.Has("FLAG_ALOLA_AKALA_DONE"));
        Assert.Equal("alolaAether", Next());
    }
}
