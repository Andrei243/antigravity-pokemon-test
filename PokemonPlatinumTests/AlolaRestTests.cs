using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// The rest of the islands (plan 23, the wave after the loose ends): Malie Garden, Routes 14, 15 and 17, Ula'ula Meadow
/// and Haina Desert on Ula'ula, Kala'e Bay and Seaward Cave on Melemele, the doors Z5 to Z7 left shut, the Fossil
/// Restoration Center, Hapu's beats, the guardians in their ruins and the second totems after the Hall of Fame, and
/// Type: Null; played headless. Floors, not counts, wherever a later session draws more.
/// </summary>
[Collection("Shared clocks and dice")]
public class AlolaRestTests
{
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Alola"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static readonly Lazy<Func<string, Map>> Rooms = new(MapDatabase.Fresh);
    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : Rooms.Value(name);
    private static readonly Lazy<Dictionary<Map, HashSet<(int X, int Y)>>> Walked =
        new(() => WorldWalk.From(MapNamed, RegionDatabase.Get(RegionDatabase.Alola)!.Start!));

    private static readonly string[] Ulaula = { "malie_garden", "ulaula_route_14", "ulaula_route_15", "ulaula_meadow", "ulaula_route_17", "haina_desert" };

    private static readonly string[] NewRooms =
    {
        "SeawardCave", "HauoliCity_CityHall", "HauoliCity_PoliceStation", "HauoliCity_ApparelShop", "AlolaRoute2_Motel",
        "MahaloTrail_RuinsOfConflict", "HeaheaCity_TouristBureau", "HeaheaCity_Hotel", "HeaheaCity_ResearchLab",
        "KonikoniCity_Restaurant", "PaniolaRanch_Nursery", "PaniolaTown_KiawesHouse", "RoyalAvenue_Megamart", "RoyalAvenue_Shop",
        "AkalaRoute8_Motel", "AkalaRoute8_FossilCenter", "AkalaRoute9_PoliceStation", "UlaulaRoute15_AetherHouse",
        "UlaulaRoute17_PoliceStation", "HainaDesert_RuinsOfAbundance",
    };

    [Fact]
    public void EveryNewPlaceAndRoomIsReachedFromTheNewHome()
    {
        var walked = Walked.Value;
        foreach (string key in Ulaula)
            Assert.True(walked.Any(p => p.Key.Name == "Ulaula" && p.Value.Any(t => p.Key.AreaAt(t.X, t.Y)?.Key == key)), $"{key} is never reached");
        Assert.True(walked.Any(p => p.Key.Name == "Melemele" && p.Value.Any(t => p.Key.AreaAt(t.X, t.Y)?.Key == "kalae_bay")), "Kala'e Bay is never reached");
        foreach (string map in NewRooms)
            Assert.Contains(walked.Keys, m => m.Name == map);
    }

    [Fact]
    public void EveryRoomOfTheNewPlacesIsAlolasAndNoDoorIsLeftShut()
    {
        foreach (string room in NewRooms)
            Assert.Equal(RegionDatabase.Alola, RegionDatabase.RegionOfMap(room)?.Id);
        // The Battle Royal Dome's door, the last one shut, is plan 23 · Z8's, opened by the Hall of Fame
        var world = LoadedWorld.Value;
        var shut = world.Index.Areas.Where(k => world.Overlay(k)?.Locked is { Count: > 0 })
            .SelectMany(k => world.Overlay(k)!.Locked!.Select(w => (k, w))).ToList();
        // (Ultra Megalopolis's houses other than Megalo Tower stay shut: the city is no place to visit door to door; and since
        // the wave after Ultra Space the Battle Tree's doors at Poni Gauntlet's head and Blush Mountain's power plant's)
        Assert.True(shut.All(s => s.Item1 is "ultra_megalopolis" or "poni_gauntlet" or "blush_mountain"), string.Join(", ", shut));
    }

    private sealed class Game
    {
        public readonly StoryState Story = new();
        public readonly Party Party = new();
        public readonly Inventory Bag = new();
        public int Money = 50000;
        private readonly Dictionary<string, Map> built = LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name);
        private readonly Func<string, Map> rooms = MapDatabase.Fresh();
        private readonly Dictionary<string, Map> cache = new();

        public Game()
        {
            Story.ChooseStarter("Litten");
            // Past the whole story but the time after the Hall of Fame, so that the objectives tell of what comes after it
            Story.SetVar("VAR_ALOLA_STORY", 54);
            Story.SetVar("VAR_ALOLA_AKALA", 8);
            Party.Add(new Pokemon(PokemonDatabase.Get("Litten")!, 40));
        }

        public Map Fresh(string name) => cache.TryGetValue(name, out var m) ? m : cache[name] = built.TryGetValue(name, out var b) ? b : rooms(name);

        public HeadlessScriptHost Play(string file, string script, string map, string? person = null, (int X, int Y)? stand = null, int[]? answers = null)
        {
            var at = Fresh(map);
            var host = new HeadlessScriptHost(Story, Party, Bag) { Map = at, MapNamed = Fresh, Region = RegionDatabase.Alola, Money = Money };
            if (stand is { } tile) host.PlayerTile = tile;
            foreach (int a in answers ?? Array.Empty<int>()) host.Answers.Enqueue(a);
            var library = ScriptLibrary.Default;
            var runner = new ScriptRunner(library, host);
            runner.Start(library.Find(script, file) ?? throw new Xunit.Sdk.XunitException($"{file}.{script} is missing"),
                person == null ? null : at.Everyone.Single(n => n.Key == person && (at.AreaAt(n.GridX, n.GridY)?.Key ?? file) == file));
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            Money = host.Money;
            return host;
        }
    }

    [Fact]
    public void HapuRegistersHerMudsdaleOnRoute12AndWaitsBeforeTheRuinsOfAbundance()
    {
        var game = new Game();
        game.Story.SetVar("VAR_ALOLA_STORY", 43);
        game.Play("ulaula_route_12", "HapuMudsdale", "Ulaula", stand: (181, 227));
        Assert.True(game.Story.Has(RidePager.MudsdaleFlag));
        Assert.True(game.Story.Has("FLAG_ALOLA_HAPU_ROUTE_12"));
        Assert.Equal(1, game.Story.Var("VAR_ALOLA_HAPU"));
        // The rancher of Tapu Village knows she has one already
        game.Story.SetVar("VAR_ALOLA_STORY", 44);
        Assert.Contains(game.Play("tapu_village", "Rancher", "Ulaula", "mudsdale").Transcript, l => l.Text.Contains("not the least bit clever"));
        game.Play("haina_desert", "Hapu", "Ulaula", "hapu");
        Assert.True(game.Story.Has("FLAG_ALOLA_HAPU_RUINS_DONE"));
        Assert.Equal(2, game.Story.Var("VAR_ALOLA_HAPU"));
    }

    [Fact]
    public void TheGuardiansShowThemselvesBeforeTheHallOfFameAndAreBattledAfterIt()
    {
        var game = new Game();
        var koko = game.Play("MahaloTrail_RuinsOfConflict", "TapuKoko", "MahaloTrail_RuinsOfConflict", "legend");
        Assert.DoesNotContain(koko.Log, l => l.StartsWith("wildbattle"));
        Assert.False(game.Story.Has("FLAG_ALOLA_TAPU_KOKO_DONE"));
        game.Story.Set("AlolaHallOfFame");
        Assert.Equal("alolaTapuKoko", Objectives.Current(game.Story, RegionDatabase.Alola)?.Name);
        foreach (var (room, name, flag) in new[] {
                     ("MahaloTrail_RuinsOfConflict", "Tapu Koko", "FLAG_ALOLA_TAPU_KOKO_DONE"),
                     ("AkalaOutskirts_RuinsOfLife", "Tapu Lele", "FLAG_ALOLA_TAPU_LELE_DONE"),
                     ("HainaDesert_RuinsOfAbundance", "Tapu Bulu", "FLAG_ALOLA_TAPU_BULU_DONE") })
        {
            var host = game.Play(room, name.Replace(" ", ""), room, "legend");
            Assert.Contains($"wildbattle {name} 60 nofleeing Won", host.Log);
            Assert.True(game.Story.Has(flag));
        }
        Assert.Equal("alolaTapuFini", Objectives.Current(game.Story, RegionDatabase.Alola)?.Name);
    }

    [Fact]
    public void TheSecondTotemsComeAfterTheHallOfFameAndAreFoughtAsTotems()
    {
        var game = new Game();
        foreach (string flag in new[] { "FLAG_ALOLA_TAPU_KOKO_DONE", "FLAG_ALOLA_TAPU_LELE_DONE", "FLAG_ALOLA_TAPU_BULU_DONE", "FLAG_ALOLA_TAPU_FINI_DONE", "AlolaHallOfFame" })
            game.Story.Set(flag);
        Assert.Equal("alolaTotems", Objectives.Current(game.Story, RegionDatabase.Alola)?.Name);
        foreach (var (file, map, start) in new[] {
                     ("verdant_cavern", "VerdantCavern", "totembattle Raticate 50 Attack+1"),
                     ("brooklet_hill", "Akala", "totembattle Araquanid 51 SpDefense+1 ally Dewpider"),
                     ("wela_volcano_park", "Akala", "totembattle Salazzle 52 SpDefense+1 ally Salandit"),
                     ("HokulaniObservatory", "HokulaniObservatory", "totembattle Vikavolt 53 SpAttack+1 ally Charjabug") })
        {
            var host = game.Play(file, "SecondTotem", map, "second_totem");
            Assert.Contains(host.Log, l => l.StartsWith(start));
        }
        // and then the Lightseekers' hunt through Ultra Space (plan 23 · Z8), which begins at home after the Hall of Fame
        Assert.Equal("alolaUltraAltar", Objectives.Current(game.Story, RegionDatabase.Alola)?.Name);
    }

    [Fact]
    public void TheFossilRestorationCenterSellsOneOfEachFossilAndRevivesIt()
    {
        var game = new Game();
        game.Play("AkalaRoute8_FossilCenter", "Seller", "AkalaRoute8_FossilCenter", "seller", answers: new[] { 0 });
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Skull Fossil")!));
        Assert.Equal(43000, game.Money);
        // Only one of a kind
        game.Play("AkalaRoute8_FossilCenter", "Seller", "AkalaRoute8_FossilCenter", "seller", answers: new[] { 0 });
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Skull Fossil")!));
        Assert.Equal(43000, game.Money);
        // Revived by the restorer, and handed over once the player has been out
        game.Play("AkalaRoute8_FossilCenter", "Restorer", "AkalaRoute8_FossilCenter", "restorer", answers: new[] { 0 });
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("Skull Fossil")!));
        game.Story.ClearLocal();
        game.Play("AkalaRoute8_FossilCenter", "Restorer", "AkalaRoute8_FossilCenter", "restorer");
        Assert.Contains(game.Party.Members, p => p.Species.Name == "Cranidos" && p.Level == 20);
    }

    [Fact]
    public void GladionHandsTheChampionTypeNullOnce()
    {
        var game = new Game();
        game.Story.Set("AlolaHallOfFame");
        game.Play("aether_paradise", "Gladion", "AetherParadise", "gladion");
        Assert.Contains(game.Party.Members, p => p.Species.Name == "Type: Null" && p.Level == 40);
        Assert.True(game.Story.Has("FLAG_ALOLA_TYPE_NULL_GIVEN"));
    }
}
