using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Alola's last part (plan 23 · Z7): the Aether Foundation's tour, Ula'ula Island (Malie City to Po Town), the rescue at
/// Aether Paradise, Poni Island, the Altar, Mount Lanakila and the League, played headless from the Akala stamp to the
/// Hall of Fame; the region-bound evolutions. Floors, not counts, wherever a later session draws more.
/// </summary>
[Collection("Shared clocks and dice")]
public class AlolaUlaulaTests
{
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Alola"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static readonly Lazy<Func<string, Map>> Rooms = new(MapDatabase.Fresh);
    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : Rooms.Value(name);
    private static readonly Lazy<Dictionary<Map, HashSet<(int X, int Y)>>> Walked =
        new(() => WorldWalk.From(MapNamed, RegionDatabase.Get(RegionDatabase.Alola)!.Start!));

    private static readonly string[] Ulaula = { "malie_city", "ulaula_route_10", "mount_hokulani", "ulaula_route_12", "tapu_village", "ulaula_route_13", "lanakila_foot", "ulaula_route_16", "po_town" };
    private static readonly string[] Poni = { "seafolk_village", "poni_wilds", "poni_breakers", "exeggutor_island" };

    [Fact]
    public void EveryNewPlaceIsReachedFromTheNewHome()
    {
        var walked = Walked.Value;
        foreach (string key in Ulaula)
            Assert.True(walked.Any(p => p.Key.Name == "Ulaula" && p.Value.Any(t => p.Key.AreaAt(t.X, t.Y)?.Key == key)), $"{key} is never reached");
        foreach (string key in Poni)
            Assert.True(walked.Any(p => p.Key.Name == "Poni" && p.Value.Any(t => p.Key.AreaAt(t.X, t.Y)?.Key == key)), $"{key} is never reached");
        foreach (string map in new[] { "AetherParadise", "AncientPoniPath", "VastPoniCanyon", "PoniAltar", "MountLanakila", "HokulaniObservatory", "ThriftyMegamart",
                                       "ShadyHouse", "SkullManor", "AetherParadise_Office", "AlolaLeague_Lobby", "AlolaLeague_HalaRoom", "AlolaLeague_OliviaRoom",
                                       "AlolaLeague_AcerolaRoom", "AlolaLeague_KahiliRoom", "AlolaLeague_KukuiRoom", "AlolaLeague_HallOfFame" })
            Assert.Contains(walked.Keys, m => m.Name == map);
        // The strait to Exeggutor Island is crossed by water; the island's middle is walked
        var poni = walked.Single(p => p.Key.Name == "Poni").Value;
        Assert.Contains((240, 90), poni);
    }

    [Fact]
    public void TheSpawnsOfTheNewPokemonCentersStandBeforeTheirDoors()
    {
        var region = RegionDatabase.Get(RegionDatabase.Alola)!;
        foreach (var spawn in region.File!.Spawns.Where(s => s.Id >= 30))
        {
            var map = MapNamed(spawn.Map ?? "Melemele");
            Assert.True(map.IsWalkable(spawn.X, spawn.Y), $"spawn {spawn.Id} stands on a blocked tile");
            Assert.Equal(spawn.Room, map.GetWarpAt(spawn.X, spawn.Y - 1)?.TargetMap);
        }
        Assert.True(region.File.Spawns.Count(s => s.Id >= 30) >= 4);
    }

    [Fact]
    public void TheLeaguesDoorsOpenOneRoomAtATime()
    {
        var story = new StoryState();
        var rooms = new[] { ("AlolaLeague_HalaRoom", "FLAG_ALOLA_DEFEATED_HALA", "AlolaLeague_OliviaRoom"), ("AlolaLeague_OliviaRoom", "FLAG_ALOLA_DEFEATED_OLIVIA", "AlolaLeague_AcerolaRoom"),
                            ("AlolaLeague_AcerolaRoom", "FLAG_ALOLA_DEFEATED_ACEROLA", "AlolaLeague_KahiliRoom"), ("AlolaLeague_KahiliRoom", "FLAG_ALOLA_DEFEATED_KAHILI", "AlolaLeague_KukuiRoom"),
                            ("AlolaLeague_KukuiRoom", "FLAG_ALOLA_DEFEATED_KUKUI", "AlolaLeague_HallOfFame") };
        foreach (var (room, flag, next) in rooms)
        {
            var map = Rooms.Value(room);
            map.ApplyDoors(story.Has);
            Assert.Null(map.GetWarpAt(map.Width / 2, 3));
            story.Set(flag);
            map.ApplyDoors(story.Has);
            Assert.Equal(next, map.GetWarpAt(map.Width / 2, 3)?.TargetMap);
        }
    }

    [Fact]
    public void TheMachampAndMudsdaleRidesAreRegisteredByFlags()
    {
        var story = new StoryState();
        story.Set("FLAG_ALOLA_RIDE_MACHAMP");
        Assert.Equal(new[] { FieldMove.Strength }, RidePager.Granted(story, RegionDatabase.Alola));
        Assert.Equal("Machamp", FieldHelp.HelperFor(FieldMove.Strength, story, RegionDatabase.Alola).Species.Name);
        // Mudsdale's Gallop is no move of the grid's: its flag alone, which grants nothing
        story.Set(RidePager.MudsdaleFlag);
        Assert.Equal(new[] { FieldMove.Strength }, RidePager.Granted(story, RegionDatabase.Alola));
    }

    [Fact]
    public void APikachuExeggcuteAndCuboneEvolveIntoTheirAlolanFormsInAlolaAndOtherwiseElsewhere()
    {
        Pokemon Mon(string species, int level) => new(PokemonDatabase.Get(species)!, level);
        ItemData Item(string name) => ItemDatabase.Get(name)!;

        // Pikachu with a Thunder Stone: Raichu in Kanto's way, an Alolan Raichu in Alola
        var stone = new EvolutionContext { Item = Item("Thunder Stone"), Region = "Alola" };
        var raichu = Mon("Pikachu", 30);
        Evolution.Evolve(raichu, Evolution.Find(raichu, EvolutionTrigger.UseItem, stone)!, stone);
        Assert.Equal(("Raichu", "Raichu-Alola"), (raichu.Species.Name, raichu.Form));
        Assert.Null(Evolution.Find(Mon("Pikachu", 30), EvolutionTrigger.UseItem, new EvolutionContext { Item = Item("Thunder Stone"), Region = "Sinnoh" })!.TargetForm);

        // Exeggcute with a Leaf Stone
        var leaf = new EvolutionContext { Item = Item("Leaf Stone"), Region = "Alola" };
        var exeggcute = Mon("Exeggcute", 30);
        Evolution.Evolve(exeggcute, Evolution.Find(exeggcute, EvolutionTrigger.UseItem, leaf)!, leaf);
        Assert.Equal(("Exeggutor", "Exeggutor-Alola"), (exeggcute.Species.Name, exeggcute.Form));
        Assert.Null(Evolution.Find(Mon("Exeggcute", 30), EvolutionTrigger.UseItem, new EvolutionContext { Item = Item("Leaf Stone"), Region = "Kanto" })!.TargetForm);

        // Cubone at level 28 by night: a Marowak, and in Alola the fire-and-ghost one
        var night = new EvolutionContext { IsNight = true, Region = "Alola" };
        var cubone = Mon("Cubone", 28);
        Evolution.Evolve(cubone, Evolution.Find(cubone, EvolutionTrigger.LevelUp, night)!, night);
        Assert.Equal(("Marowak", "Marowak-Alola"), (cubone.Species.Name, cubone.Form));
        var kanto = Evolution.Find(Mon("Cubone", 28), EvolutionTrigger.LevelUp, new EvolutionContext { IsNight = true, Region = "Kanto" });
        Assert.Equal("Marowak", PokemonDatabase.Get("Marowak")!.Name);
        Assert.Null(kanto?.TargetForm);
    }

    [Fact]
    public void TheStoryRunsFromTheAkalaStampToTheHallOfFameHeadless()
    {
        var story = new StoryState();
        story.GiveBadge(RegionDatabase.Alola, "Melemele");
        story.GiveBadge(RegionDatabase.Alola, "Akala");
        story.SetVar("VAR_ALOLA_STORY", 30);
        story.ChooseStarter("Litten");
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Litten")!, 30));
        var bag = new Inventory();
        // Fresh copies of the maps, since the scripts hide and show their people
        var built = LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name);
        var rooms = MapDatabase.Fresh();
        var cache = new Dictionary<string, Map>();
        Map Fresh(string name) => cache.TryGetValue(name, out var m) ? m : cache[name] = built.TryGetValue(name, out var b) ? b : rooms(name);
        var library = ScriptLibrary.Default;
        HeadlessScriptHost Play(string file, string script, string map, string? person = null, (int X, int Y)? stand = null, int[]? answers = null)
        {
            var at = Fresh(map);
            var host = new HeadlessScriptHost(story, party, bag) { Map = at, MapNamed = Fresh, Region = RegionDatabase.Alola };
            if (stand is { } tile) host.PlayerTile = tile;
            foreach (int a in answers ?? Array.Empty<int>()) host.Answers.Enqueue(a);
            var runner = new ScriptRunner(library, host);
            runner.Start(library.Find(script, file) ?? throw new Xunit.Sdk.XunitException($"{file}.{script} is missing"),
                person == null ? null : at.Everyone.Single(n => n.Key == person && (at.AreaAt(n.GridX, n.GridY)?.Key ?? file) == file));
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            return host;
        }
        int Story() => story.Var("VAR_ALOLA_STORY");
        string? Next() => Objectives.Current(story, RegionDatabase.Alola)?.Name;
        int Count(string item) => bag.GetQuantity(ItemDatabase.Get(item)!);

        // The ferry from Heahea, the first time: the tour of Aether Paradise
        Assert.Contains("warp AetherParadise 48 90", Play("heahea_city", "AetherFerry", "Akala", "aether_sailor", answers: new[] { 0 }).Log);
        Assert.Equal(40, Story());
        Play("aether_paradise", "Tour", "AetherParadise", stand: (48, 72));
        Assert.Equal(41, Story());
        Assert.Equal("alolaMalie", Next());
        // Malie City: the ferry from the Foundation, Hau on the plaza
        Assert.Contains("warp Ulaula 59 366", Play("aether_paradise", "Ferry", "AetherParadise", "sailor", answers: new[] { 1 }).Log);
        Assert.True(story.Has("FLAG_ALOLA_AT_MALIE"));
        Play("malie_city", "Arrival", "Ulaula", stand: (59, 350));
        Assert.Equal(42, Story());
        // Hokulani Observatory: three Charjabug, then the totem Togedemaru
        Play("HokulaniObservatory", "OnEnter", "HokulaniObservatory");
        Assert.True(story.Has("FLAG_ALOLA_HOKULANI_TRIAL"));
        Play("HokulaniObservatory", "Totem", "HokulaniObservatory", "totem");
        Assert.Equal(42, Story());
        foreach (string den in new[] { "Den1", "Den2", "Den3" })
            Play("HokulaniObservatory", den, "HokulaniObservatory", "den_" + den[^1]);
        // Fought as a totem since wave 91 (plan 23 · Z6's totem battles): the aura, a Charjabug at its first call
        Assert.Contains(Play("HokulaniObservatory", "Totem", "HokulaniObservatory", "totem").Log, l => l.StartsWith("totembattle Togedemaru 24 Defense+1 ally Charjabug"));
        Assert.Equal(43, Story());
        Assert.Equal(1, Count("Electrium Z"));
        // Tapu Village: Acerola, the Megamart's trial, Mudsdale
        Play("tapu_village", "AcerolaArrival", "Ulaula", stand: (204, 226));
        Assert.Equal(44, Story());
        Assert.True(story.Has("FLAG_ALOLA_MEGAMART_OPEN"));
        Play("tapu_village", "Rancher", "Ulaula", "mudsdale");
        Assert.True(story.Has(RidePager.MudsdaleFlag));
        Play("ThriftyMegamart", "OnEnter", "ThriftyMegamart");
        foreach (string den in new[] { "Den1", "Den2", "Den3" })
            Play("ThriftyMegamart", den, "ThriftyMegamart", "den_" + den[^1]);
        Assert.Contains(Play("ThriftyMegamart", "Totem", "ThriftyMegamart", "totem").Log, l => l.StartsWith("totembattle Mimikyu 25 ") && l.Contains("ally Haunter"));
        Assert.Equal(45, Story());
        Assert.Equal(1, Count("Ghostium Z"));
        // Po Town: Plumeria, the manor, Guzma, Nanu's grand trial
        Play("po_town", "Arrival", "Ulaula", stand: (238, 128));
        Assert.True(story.Has("FLAG_ALOLA_PLUMERIA_DONE"));
        Play("SkullManor", "ManorGrunt1", "SkullManor", "grunt_1");
        Play("SkullManor", "ManorGrunt2", "SkullManor", "grunt_2");
        Play("SkullManor", "GuzmaInside", "SkullManor", "guzma");
        Assert.Equal(46, Story());
        Play("po_town", "NanuOutside", "Ulaula", "nanu");
        Assert.True(story.Has("FLAG_ALOLA_SHADY_HOUSE_OPEN"));
        Play("ShadyHouse", "NanuInside", "ShadyHouse", "nanu");
        Assert.Equal(47, Story());
        Assert.True(story.HasBadge(RegionDatabase.Alola, "UlaUla"));
        Assert.Equal(1, Count("Darkinium Z"));
        Assert.Equal("alolaRescue", Next());
        // Aether Paradise again: the head office opens, Lusamine is beaten
        Play("aether_paradise", "Rescue", "AetherParadise", stand: (48, 72));
        Assert.True(story.Has("FLAG_ALOLA_AETHER_HQ_OPEN"));
        Play("AetherParadise_Office", "LusamineInside", "AetherParadise_Office", "lusamine");
        Assert.Equal(48, Story());
        Assert.Contains("warp Poni 49 300", Play("aether_paradise", "Ferry", "AetherParadise", "sailor", answers: new[] { 2 }).Log);
        // Poni Island: the arrival, the Machamp, Dulse's trial in Vast Poni Canyon
        Play("seafolk_village", "Arrival", "Poni", stand: (49, 282));
        Assert.Equal(49, Story());
        Play("seafolk_village", "MachampRide", "Poni", "machamp");
        Assert.True(story.Has("FLAG_ALOLA_RIDE_MACHAMP"));
        Assert.Contains(FieldMove.Strength, RidePager.Granted(story, RegionDatabase.Alola));
        Play("poni_wilds", "Dulse", "Poni", "dulse");
        Assert.True(story.Has("FLAG_ALOLA_CANYON_TRIAL"));
        foreach (string den in new[] { "Den1", "Den2", "Den3" })
            Play("vast_poni_canyon", den, "VastPoniCanyon", "den_" + den[^1]);
        Assert.Contains(Play("vast_poni_canyon", "Totem", "VastPoniCanyon", "totem").Log, l => l.StartsWith("totembattle Kommo-o 47 ") && l.Contains("ally Hakamo-o"));
        Assert.Equal(50, Story());
        Assert.Equal(1, Count("Dragonium Z"));
        // Hapu's grand trial across the strait, and the Altar
        Play("poni_breakers", "HapuShore", "Poni", "hapu");
        Play("exeggutor_island", "HapuGrand", "Poni", "hapu");
        Assert.Equal(51, Story());
        Assert.True(story.HasBadge(RegionDatabase.Alola, "Poni"));
        Assert.True(story.Has("FLAG_ALOLA_ALTAR_OPEN"));
        Assert.Equal(1, Count("Groundium Z"));
        Play("poni_altar", "OnEnter", "PoniAltar");
        Assert.True(story.Has("FLAG_ALOLA_AT_ALTAR"));
        Play("poni_altar", "Legend", "PoniAltar", "legend");
        Assert.Equal(52, Story());
        Assert.True(story.Has("FLAG_ALOLA_LANAKILA_OPEN"));
        Assert.Equal("alolaLanakila", Next());
        // The League: the Elite Four in turn, the Champion, the Hall of Fame
        foreach (var (room, who) in new[] { ("AlolaLeague_HalaRoom", "hala"), ("AlolaLeague_OliviaRoom", "olivia"), ("AlolaLeague_AcerolaRoom", "acerola"), ("AlolaLeague_KahiliRoom", "kahili") })
            Play(room, "League", room, who);
        Assert.Contains("warp AlolaLeague_HallOfFame 6 10", Play("AlolaLeague_KukuiRoom", "Kukui", "AlolaLeague_KukuiRoom", "kukui").Log);
        var hall = Play("AlolaLeague_HallOfFame", "OnEnter", "AlolaLeague_HallOfFame");
        Assert.Contains("warp AlolaRoute1_PlayersHouse 6 6", hall.Log);
        Assert.True(story.Has("AlolaHallOfFame"));
        Assert.Equal(54, Story());
        Assert.Equal("alolaTapuKoko", Next());
        Assert.False(story.Has("FLAG_ALOLA_DEFEATED_KUKUI"));
    }
}
