using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Alola after its Hall of Fame (plan 23 · Z8; docs/mechanics/rulings.md, "Ultra Space and the Battle Royal Dome"): the
/// Lightseekers' visit, the Ultra Wormhole over the Altar's dais, the seven Ultra Beasts' worlds, Ultra Megalopolis and
/// Necrozma at the top of Megalo Tower, Stakataka and Blacephalon after it; the Battle Royal Dome's show and its ranks;
/// and the Champion's title defended. Every Ultra Beast can be caught, which is the session's "done when".
/// </summary>
[Collection("Shared clocks and dice")]
public class AlolaUltraSpaceTests
{
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Alola"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static readonly Lazy<Func<string, Map>> Rooms = new(MapDatabase.Fresh);
    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : Rooms.Value(name);

    /// <summary>Each world's map, its Ultra Beast and the answers that take the Altar's wormhole there.</summary>
    private static readonly (string Map, string Beast, int[] Answers)[] Worlds =
    {
        ("UltraDeepSea", "Nihilego", new[] { 0 }), ("UltraJungle", "Buzzwole", new[] { 1 }), ("UltraDesert", "Pheromosa", new[] { 2 }),
        ("UltraPlant", "Xurkitree", new[] { 3 }), ("UltraCrater", "Celesteela", new[] { 4, 0 }), ("UltraForest", "Kartana", new[] { 4, 1 }),
        ("UltraRuin", "Guzzlord", new[] { 4, 2 })
    };

    private static readonly string[] UltraBeasts =
        { "Nihilego", "Buzzwole", "Pheromosa", "Xurkitree", "Celesteela", "Kartana", "Guzzlord", "Poipole", "Naganadel", "Stakataka", "Blacephalon" };

    [Fact]
    public void EveryWorldIsWalkedFromItsWormholeToItsUltraBeast()
    {
        foreach (var (name, beast, _) in Worlds.Append(("UltraMegalopolis", "", Array.Empty<int>())))
        {
            var map = MapNamed(name);
            Assert.True(map.IsWalkable(32, 54), $"{name}: the arrival is blocked");
            Assert.Contains(map.TileScripts, t => t.Key == (32, 55) && t.Value == "UltraWayHome");
            // Everything open from the arrival, the way people walk
            var reached = new HashSet<(int, int)> { (32, 54) };
            var open = new Queue<(int X, int Y)>(reached);
            while (open.Count > 0)
            {
                var (x, y) = open.Dequeue();
                foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                    if (map.InBounds(x + dx, y + dy) && map.IsWalkable(x + dx, y + dy) && reached.Add((x + dx, y + dy))) open.Enqueue((x + dx, y + dy));
            }
            if (beast.Length > 0)
            {
                var it = map.Everyone.Single(n => n.Key == "beast");
                Assert.Equal((beast, 32, 16), (it.Species, it.GridX, it.GridY));
                Assert.Equal($"FLAG_ALOLA_CAUGHT_{beast.ToUpperInvariant()}", it.HiddenBy);
                Assert.Contains((32, 17), reached);
            }
            else
            {
                // Ultra Megalopolis: the tower's door is come to from the wormhole, and it leads up into Megalo Tower
                Assert.Contains((32, 22), reached);
                Assert.Equal("MegaloTower", map.GetWarpAt(32, 21)?.TargetMap);
            }
        }
    }

    [Fact]
    public void EveryUltraBeastCanBeCaught()
    {
        // Met in a script as a wild Pokémon, or given; Naganadel by its own evolution from the Poipole given
        var met = ScriptLibrary.Default.All.SelectMany(s => s.Everything())
            .Where(i => i.Op is Op.WildBattle or Op.GivePokemon).Select(i => i.Name).ToHashSet();
        foreach (string beast in UltraBeasts.Where(b => b != "Naganadel"))
            Assert.True(met.Contains(beast), $"{beast} is met nowhere");
        Assert.Contains(PokemonDatabase.Get("Poipole")!.Evolutions!, e => e.TargetSpecies == "Naganadel");
        // A Beast Ball takes an Ultra Beast five times as readily, and the Lightseekers give ten
        Assert.Contains(ScriptLibrary.Default.Find("OnEnter", "AlolaRoute1_PlayersHouse")!.Everything(), i => i.Op == Op.Give && i.Name == "Beast Ball");
    }

    private sealed class Play
    {
        public readonly StoryState Story = new();
        public readonly Party Party = new();
        public readonly Inventory Bag = new();
        private readonly Dictionary<string, Map> built = LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name);
        private readonly Func<string, Map> rooms = MapDatabase.Fresh();
        private readonly Dictionary<string, Map> cache = new();
        public BattleOutcome Fight = BattleOutcome.Won;
        public Map Fresh(string name) => cache.TryGetValue(name, out var m) ? m : cache[name] = built.TryGetValue(name, out var b) ? b : rooms(name);

        public HeadlessScriptHost Run(string file, string script, string map, string? person = null, int[]? answers = null, int[]? rolls = null)
        {
            var at = Fresh(map);
            var host = new HeadlessScriptHost(Story, Party, Bag) { Map = at, MapNamed = Fresh, Region = RegionDatabase.Alola, Fight = _ => Fight };
            foreach (int a in answers ?? Array.Empty<int>()) host.Answers.Enqueue(a);
            foreach (int r in rolls ?? Array.Empty<int>()) host.Rolls.Enqueue(r);
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(ScriptLibrary.Default.Find(script, file) ?? throw new Xunit.Sdk.XunitException($"{file}.{script} is missing"),
                person == null ? null : at.Everyone.Single(n => n.Key == person));
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            return host;
        }
    }

    [Fact]
    public void TheHuntRunsFromTheLightseekersVisitToNecrozmaHeadless()
    {
        var game = new Play();
        game.Story.Set("AlolaHallOfFame");
        game.Story.SetVar("VAR_ALOLA_STORY", 54);
        game.Story.ChooseStarter("Rowlet");
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Decidueye")!, 62));
        game.Fight = BattleOutcome.Caught;
        string? Next() => Objectives.Current(game.Story, RegionDatabase.Alola)?.Name;
        int Ultra() => game.Story.Var("VAR_ALOLA_ULTRA");

        // Before the Hall of Fame the visitors don't call, and the dais is only warm stone
        var nobody = new Play();
        nobody.Run("AlolaRoute1_PlayersHouse", "OnEnter", "AlolaRoute1_PlayersHouse");
        Assert.Equal(0, nobody.Story.Var("VAR_ALOLA_ULTRA"));
        Assert.DoesNotContain(nobody.Run("poni_altar", "Wormhole", "PoniAltar").Log, l => l.StartsWith("warp "));

        // Home: the Lightseekers, ten Beast Balls and Poipole
        game.Run("AlolaRoute1_PlayersHouse", "OnEnter", "AlolaRoute1_PlayersHouse");
        Assert.Equal(1, Ultra());
        Assert.Equal(10, game.Bag.GetQuantity(ItemDatabase.Get("Beast Ball")!));
        Assert.Contains(game.Party.Members, p => p.Species.Name == "Poipole" && p.Level == 40);
        Assert.True(game.Story.Has("FLAG_ALOLA_ULTRA_AT_ALTAR"));
        Assert.Equal("alolaUltraAltar", Next());
        // Played once: the next arrival home is quiet
        game.Run("AlolaRoute1_PlayersHouse", "OnEnter", "AlolaRoute1_PlayersHouse");
        Assert.Equal(10, game.Bag.GetQuantity(ItemDatabase.Get("Beast Ball")!));
        Assert.Equal(1, game.Party.Members.Count(p => p.Species.Name == "Poipole"));

        // The Altar: the wormhole opened over the dais
        Assert.Contains(game.Run("poni_altar", "Lightseekers", "PoniAltar").Log, l => l.StartsWith("effect Rift 32 14"));
        Assert.Equal(2, Ultra());
        Assert.Equal("alolaUltraHunt", Next());
        // Only the seven worlds while any of their beasts is still to meet
        var page = game.Run("poni_altar", "Wormhole", "PoniAltar", answers: new[] { 4, 3 });
        Assert.DoesNotContain(page.Log, l => l.StartsWith("warp "));
        game.Run("poni_altar", "OnEnter", "PoniAltar");
        Assert.Equal(2, Ultra());

        foreach (var (map, beast, answers) in Worlds)
        {
            Assert.Contains($"warp {map} 32 54", game.Run("poni_altar", "Wormhole", "PoniAltar", answers: answers).Log);
            game.Run(MapFile(map), "OnEnter", map);
            Assert.Contains(game.Run(MapFile(map), "Beast", map, "beast").Log, l => l.StartsWith($"wildbattle {beast} 60 "));
            Assert.True(game.Story.Has($"FLAG_ALOLA_CAUGHT_{beast.ToUpperInvariant()}"));
            Assert.True(game.Story.Has($"FLAG_ALOLA_MET_{beast.ToUpperInvariant()}"));
            Assert.Contains("warp PoniAltar 32 15", game.Run("common", "UltraWayHome", map, answers: new[] { 0 }).Log);
        }
        Assert.Equal(2, Ultra());
        // Back at the Altar with all seven met: the way to Ultra Megalopolis
        game.Run("poni_altar", "OnEnter", "PoniAltar");
        Assert.Equal(3, Ultra());
        Assert.True(game.Story.Has("FLAG_ALOLA_ULTRA_AT_TOWER"));
        Assert.Equal("alolaMegalopolis", Next());
        Assert.Contains("warp UltraMegalopolis 32 54", game.Run("poni_altar", "Wormhole", "PoniAltar", answers: new[] { 4, 3 }).Log);

        // Megalo Tower: Necrozma, and the light comes back
        var tower = game.Run("MegaloTower", "Necrozma", "MegaloTower", "necrozma");
        Assert.Contains(tower.Log, l => l.StartsWith("wildbattle Necrozma 65 nofleeing"));
        Assert.Equal(4, Ultra());
        Assert.True(game.Story.Has("FLAG_ALOLA_LIGHT_RETURNED"));
        Assert.True(game.Story.Has("FLAG_ALOLA_CAUGHT_NECROZMA"));
        Assert.Equal("alolaRoyalDome", Next());
        // and the two the light drew
        Assert.Contains(game.Run("ultra_megalopolis", "Blacephalon", "UltraMegalopolis", "blacephalon").Log, l => l.StartsWith("wildbattle Blacephalon 60"));
        Assert.Contains(game.Run("poni_altar", "Stakataka", "PoniAltar", "stakataka").Log, l => l.StartsWith("wildbattle Stakataka 60"));
        Assert.True(game.Story.Has("FLAG_ALOLA_CAUGHT_BLACEPHALON") && game.Story.Has("FLAG_ALOLA_CAUGHT_STAKATAKA"));
    }

    private static string MapFile(string map) => map switch
    {
        "UltraDeepSea" => "ultra_deep_sea", "UltraJungle" => "ultra_jungle", "UltraDesert" => "ultra_desert", "UltraPlant" => "ultra_plant",
        "UltraCrater" => "ultra_crater", "UltraForest" => "ultra_forest", "UltraRuin" => "ultra_ruin", _ => "ultra_megalopolis"
    };

    [Fact]
    public void AnUltraBeastNotCaughtIsThereAgainNextTime()
    {
        var game = new Play { Fight = BattleOutcome.Won };
        game.Story.Set("AlolaHallOfFame");
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Decidueye")!, 62));
        game.Run("ultra_jungle", "Beast", "UltraJungle", "beast");
        Assert.True(game.Story.Has("FLAG_ALOLA_MET_BUZZWOLE"));
        Assert.False(game.Story.Has("FLAG_ALOLA_CAUGHT_BUZZWOLE"));
        // Its hiding flag is the caught one alone, so a world come to again has it back
        var map = new Play().Fresh("UltraJungle");
        map.ApplyPresence(game.Story.Has);
        Assert.Contains(map.NPCs, n => n.Key == "beast");
    }

    [Fact]
    public void TheDomeOpensToTheChampionAndItsShowIsARoyalOfThree()
    {
        var akala = MapNamed("Akala");
        var story = new StoryState();
        Assert.Equal("RoyalAvenue_BattleRoyalDome", akala.GetWarpAt(265, 207)?.TargetMap);
        akala.ApplyDoors(story.Has);
        Assert.True(akala.IsSolid(265, 207));
        story.Set("AlolaHallOfFame");
        akala.ApplyDoors(story.Has);
        Assert.False(akala.IsSolid(265, 207));

        var game = new Play();
        game.Story.Set("AlolaHallOfFame");
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Decidueye")!, 62));
        var show = game.Run("RoyalAvenue_BattleRoyalDome", "TheShow", "RoyalAvenue_BattleRoyalDome");
        Assert.Contains("royal royal_dome_hau royal_dome_gladion royal_dome_masked Won", show.Log);
        Assert.True(game.Story.Has("FLAG_ALOLA_ROYAL_SHOW_DONE"));
        Assert.False(game.Story.Has("FLAG_ALOLA_ROYAL_RING_SHOW"));
        Assert.Equal(1, game.Story.Var("VAR_ALOLA_ROYAL_SHOW"));
    }

    [Fact]
    public void EachRankOfTheDomeOpensTheNextAndFirstPlaceGivesBattlePoints()
    {
        var game = new Play();
        game.Story.Set("AlolaHallOfFame");
        game.Story.Set("FLAG_ALOLA_ROYAL_SHOW_DONE");
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Decidueye")!, 62));
        var ranks = new[] { ("royal_dome_kalei", 0), ("royal_dome_pua", 1), ("royal_dome_ikaika", 2), ("royal_dome_kealoha", 3) };
        foreach (var (first, answer) in ranks)
        {
            var desk = game.Run("RoyalAvenue_BattleRoyalDome", "RoyalDesk", "RoyalAvenue_BattleRoyalDome", "receptionist", new[] { answer });
            Assert.Contains(desk.Log, l => l.StartsWith($"royal {first} ") && l.EndsWith(" Won"));
            Assert.Equal(answer + 1, desk.Frontier.BattlePoints);
        }
        Assert.Equal(3, game.Story.Var("VAR_ALOLA_ROYAL_RANK"));
        Assert.Equal(4, game.Story.Var("VAR_ALOLA_ROYAL_WINS"));
        // A placing other than first opens nothing and gives nothing
        var placed = new Play { Fight = BattleOutcome.None };
        placed.Story.Set("FLAG_ALOLA_ROYAL_SHOW_DONE");
        placed.Party.Add(new Pokemon(PokemonDatabase.Get("Decidueye")!, 62));
        placed.Run("RoyalAvenue_BattleRoyalDome", "RoyalDesk", "RoyalAvenue_BattleRoyalDome", "receptionist", new[] { 0 });
        Assert.Equal(0, placed.Story.Var("VAR_ALOLA_ROYAL_RANK"));
    }

    [Fact]
    public void TheDomesMasterRankIsFoughtToAPlacing()
    {
        Trainer TrainerOf(string id)
        {
            var t = new Trainer { Id = id };
            TrainerDatabase.Fill(t, TrainerDatabase.Get(id)!);
            return t;
        }
        var mine = new Party();
        foreach (string species in new[] { "Decidueye", "Lycanroc", "Primarina" }) mine.Add(new Pokemon(PokemonDatabase.Get(species)!, 66, new Random(5)));
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = mine, Trainers = new[] { "royal_dome_kealoha", "royal_dome_pono", "royal_dome_malia" }.Select(TrainerOf).ToList(),
            Format = BattleFormat.Royal, Random = new BattleRandom(23), Rules = Ruleset.Modern, PlayerController = TrainerAi.Instance,
            NoRewards = true, NoBattleItems = true, PlayerName = "Elio"
        });
        core.Start();
        Assert.Null(core.Request);
        Assert.True(core.Result is BattleResult.PlayerVictory or BattleResult.Placed, $"ended as {core.Result}");
        Assert.Single(core.Log.OfType<RoyalPlaced>());
    }

    [Theory]
    [InlineData(0, "defence_hau")]
    [InlineData(1, "defence_gladion")]
    [InlineData(2, "defence_hapu")]
    public void TheChampionDefendsTheTitleAgainstAChallengerDrawn(int roll, string challenger)
    {
        var game = new Play();
        game.Story.Set("AlolaHallOfFame");
        game.Story.SetVar("VAR_ALOLA_STORY", 54);
        game.Story.SetVar("VAR_ALOLA_AKALA", 8);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Decidueye")!, 62));
        var room = game.Run("AlolaLeague_KukuiRoom", "Kukui", "AlolaLeague_KukuiRoom", "kukui", rolls: new[] { roll });
        Assert.Contains(room.Log, l => l.StartsWith("battle ") && l.Contains(challenger));
        Assert.Contains("warp AlolaLeague_HallOfFame 6 10", room.Log);
        Assert.True(game.Story.Has("FLAG_ALOLA_DEFEATED_KUKUI"));
        // The Hall of Fame records the team again, with no credits, and leaves the story where it stood
        game.Story.SetVar("VAR_ALOLA_ULTRA", 2);
        var hall = game.Run("AlolaLeague_HallOfFame", "OnEnter", "AlolaLeague_HallOfFame");
        Assert.DoesNotContain(hall.Log, l => l.StartsWith("credits"));
        Assert.False(game.Story.Has("FLAG_ALOLA_DEFEATED_KUKUI"));
        Assert.Equal("alolaUltraHunt", Objectives.Current(game.Story, RegionDatabase.Alola)?.Name);
    }
}
