using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Facilities;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// The Battle Tree, the power plant, Zygarde's cells and the rest of the islands (plan 23, the wave after Poni's last
/// places; rulings, "The Battle Tree, the power plant, Zygarde's cells and the rest of the islands"): the Battle Tree's
/// lobby and hall on plan 37 · FC2's frame with two Tree Masters of ours, the geothermal power plant inside Blush
/// Mountain, Hau'oli Cemetery and its ghost girl, Big Wave Beach, the Secluded Shore, the Zygarde Cube and its hundred
/// cells, Stoutland's buried items and the Island Scan. Played headless.
/// </summary>
[Collection("Shared clocks and dice")]
public class AlolaLastIslandsTests
{
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Alola"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static readonly Lazy<Func<string, Map>> Rooms = new(MapDatabase.Fresh);
    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : Rooms.Value(name);
    private static readonly Lazy<Dictionary<Map, HashSet<(int X, int Y)>>> Walked =
        new(() => WorldWalk.From(MapNamed, RegionDatabase.Get(RegionDatabase.Alola)!.Start!));

    [Fact]
    public void EveryNewPlaceAndRoomIsReachedFromTheNewHome()
    {
        var walked = Walked.Value;
        foreach (var (map, key) in new[] { ("Melemele", "hauoli_cemetery"), ("Melemele", "big_wave_beach"), ("Ulaula", "secluded_shore") })
        {
            Assert.True(walked.Any(p => p.Key.Name == map && p.Value.Any(t => p.Key.AreaAt(t.X, t.Y)?.Key == key)), $"{key} is never reached");
            Assert.Contains(MapNamed(map).Areas, a => a.Key == key && a.Open);
        }
        foreach (string room in new[] { "PoniGauntlet_BattleTree", "BlushMountain_PowerPlant" })
        {
            Assert.True(walked.Keys.Any(m => m.Name == room), $"{room} is never reached");
            Assert.Equal(RegionDatabase.Alola, RegionDatabase.RegionOfMap(room)?.Id);
        }
        Assert.Equal(RegionDatabase.Alola, RegionDatabase.RegionOfMap("PoniGauntlet_BattleTreeHall")?.Id);
    }

    [Fact]
    public void EveryZygardeCellAndCoreOfAlolaStandsWhereItCanBeSpokenToAndThereAreAHundred()
    {
        var walked = Walked.Value;
        var cells = BuiltMaps.Value.Values.Concat(new[] { MapNamed("BlushMountain_PowerPlant") })
            .SelectMany(m => m.Everyone.Where(n => n.NpcType == NPC.ZygardeCellType).Select(n => (Map: m, Cell: n))).ToList();
        Assert.Equal(100, cells.Count);
        Assert.Equal(5, cells.Count(c => c.Cell.Script == "ZygardeCore"));
        Assert.Equal(100, cells.Select(c => c.Cell.HiddenBy).Distinct().Count());
        Assert.All(cells, c => Assert.Equal(PropType.ZygardeCell, c.Cell.GymThing));
        // Each with a tile beside it the walk reaches
        foreach (var (map, cell) in cells)
        {
            var tiles = walked.FirstOrDefault(p => p.Key.Name == map.Name).Value;
            Assert.True(tiles != null && new[] { (0, 1), (0, -1), (1, 0), (-1, 0) }.Any(d => tiles.Contains((cell.GridX + d.Item1, cell.GridY + d.Item2))),
                $"{map.Name} {cell.Key} at {cell.GridX},{cell.GridY} can't be come to");
        }
    }

    [Fact]
    public void DexioGivesTheCubeAndTheCellsArePutTogetherAtTenFiftyAndAHundred()
    {
        var game = new Game();
        // A cell before the Cube slips away
        var away = game.Play("kalae_bay", "ZygardeCell", "Melemele", "zygarde_cell_1");
        Assert.Equal(0, game.Story.Var("VAR_ALOLA_ZYGARDE_CELLS"));
        Assert.DoesNotContain(away.Log, l => l.StartsWith("setflag"));
        game.Play("ulaula_route_16", "Dexio", "Ulaula", "dexio");
        Assert.True(game.Bag.GetQuantity(ItemDatabase.Get("Zygarde Cube")!) > 0);
        var cell = game.Fresh("Melemele").Everyone.Single(n => n.Key == "zygarde_cell_1" && n.ScriptFile == "kalae_bay");
        game.Play("kalae_bay", "ZygardeCell", "Melemele", "zygarde_cell_1");
        Assert.Equal(1, game.Story.Var("VAR_ALOLA_ZYGARDE_CELLS"));
        Assert.True(game.Story.Has(cell.HiddenBy!));
        // Too few, then ten: the 10% Forme
        game.Play("ulaula_route_16", "Dexio", "Ulaula", "dexio");
        Assert.Equal(0, game.Party.Members.Count(p => p.Species.Name == "Zygarde"));
        game.Story.SetVar("VAR_ALOLA_ZYGARDE_CELLS", 10);
        game.Play("ulaula_route_16", "Dexio", "Ulaula", "dexio");
        Assert.Equal("Zygarde-10", game.Party.Members.Single(p => p.Species.Name == "Zygarde").Form);
        game.Story.SetVar("VAR_ALOLA_ZYGARDE_CELLS", 100);
        game.Play("ulaula_route_16", "Dexio", "Ulaula", "dexio");
        game.Play("ulaula_route_16", "Dexio", "Ulaula", "dexio");
        var zygarde = game.Party.Members.Where(p => p.Species.Name == "Zygarde").ToList();
        Assert.Equal(3, zygarde.Count);
        Assert.Equal("Power Construct", zygarde[2].Ability?.Name);
        Assert.True(game.Story.Has("FLAG_ALOLA_ZYGARDE_100_MADE"));
    }

    [Fact]
    public void TheIslandScanFindsTheDaysPokemonOnceADayAndItWaitsAtItsPlace()
    {
        var game = new Game();
        var host = game.Play("HeaheaCity_ResearchLab", "Scanner", "HeaheaCity_ResearchLab", "scanner", answers: new[] { 0 }, weekday: DayOfWeek.Monday);
        Assert.True(game.Story.Has("FLAG_DAILY_ALOLA_SCAN_TOTODILE"));
        Assert.True(game.Story.Has("FLAG_DAILY_ALOLA_ISLAND_SCAN"));
        // Spent for the day: no second scan
        game.Play("HeaheaCity_ResearchLab", "Scanner", "HeaheaCity_ResearchLab", "scanner", answers: new[] { 3 }, weekday: DayOfWeek.Monday);
        Assert.False(game.Story.Has("FLAG_DAILY_ALOLA_SCAN_CONKELDURR"));
        // The Totodile stands in Seaward Cave, shown by the day's flag, and battles at its level once
        var cave = game.Fresh("SeawardCave");
        var totodile = cave.Everyone.Single(n => n.Key == "scan_totodile");
        Assert.Equal("FLAG_DAILY_ALOLA_SCAN_TOTODILE", totodile.ShownBy);
        Assert.Contains(game.Play("seaward_cave", "ScanTotodile", "SeawardCave", "scan_totodile").Log, l => l.StartsWith("wildbattle Totodile 12"));
        Assert.True(game.Story.Has(totodile.HiddenBy!));
        // Every island and day has its Pokémon at a place the walk reaches
        var scans = BuiltMaps.Value.Values.SelectMany(m => m.Everyone.Where(n => n.Key.StartsWith("scan_", StringComparison.Ordinal)).Select(n => (m, n))).ToList();
        Assert.Equal(28, scans.Count);
        foreach (var (map, scan) in scans)
            Assert.True(Walked.Value.First(p => p.Key.Name == map.Name).Value.Any(t => Math.Abs(t.X - scan.GridX) + Math.Abs(t.Y - scan.GridY) == 1), $"{scan.Key} can't be come to");
    }

    [Fact]
    public void ABuriedItemIsDugUpOnlyOnStoutlandsBackAndItsNoseFindsIt()
    {
        var map = MapNamed("Melemele");
        var buried = map.HiddenItems.Where(h => h.Value.Buried).ToList();
        Assert.NotEmpty(buried);
        var ((x, y), item) = buried[0];
        var story = new StoryState();
        Assert.False(StoutlandSearch.CanFind(item, null));
        Assert.False(StoutlandSearch.CanFind(item, "Tauros"));
        Assert.True(StoutlandSearch.CanFind(item, "Stoutland"));
        Assert.Equal((x, y), StoutlandSearch.Nearest(map, x + 2, y + 1, story));
        Assert.Null(StoutlandSearch.Nearest(map, x + 3, y + 3, story));
        // Every island has some, fifteen in all
        Assert.Equal(15, BuiltMaps.Value.Values.Sum(m => m.HiddenItems.Count(h => h.Value.Buried)));
    }

    [Fact]
    public void TheGhostGirlAppearsAtNightAndGoesAfterHerFriendOnceHerTagIsFound()
    {
        var game = new Game();
        game.Play("hauoli_cemetery", "OnEnter", "Melemele", time: TimeOfDay.Morning);
        Assert.False(game.Story.Has("FLAG_ALOLA_CEMETERY_NIGHT"));
        game.Play("hauoli_cemetery", "OnEnter", "Melemele", time: TimeOfDay.Night);
        Assert.True(game.Story.Has("FLAG_ALOLA_CEMETERY_NIGHT"));
        game.Play("hauoli_cemetery", "GhostGirl", "Melemele", "ghost_girl");
        Assert.Equal(1, game.Story.Var("VAR_ALOLA_CEMETERY_GIRL"));
        game.Play("hauoli_cemetery", "LampFlowers", "Melemele");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Spell Tag")!));
        game.Play("hauoli_cemetery", "GhostGirl", "Melemele", "ghost_girl");
        Assert.True(game.Story.Has("FLAG_ALOLA_CEMETERY_GIRL_GONE"));
        Assert.Equal(3, game.Story.Var("VAR_ALOLA_CEMETERY_GIRL"));
    }

    [Fact]
    public void ThePowerPlantsChiefGivesOnceAndItsTwoBattleOnce()
    {
        var game = new Game();
        game.Play("BlushMountain_PowerPlant", "Chief", "BlushMountain_PowerPlant", "chief");
        game.Play("BlushMountain_PowerPlant", "Chief", "BlushMountain_PowerPlant", "chief");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Electric Seed")!));
        Assert.Contains(game.Play("BlushMountain_PowerPlant", "Kanoa", "BlushMountain_PowerPlant", "kanoa").Log, l => l.StartsWith("battle alola_worker_kanoa"));
        Assert.DoesNotContain(game.Play("BlushMountain_PowerPlant", "Kanoa", "BlushMountain_PowerPlant", "kanoa").Log, l => l.StartsWith("battle"));
        Assert.NotNull(TrainerDatabase.Get("alola_scientist_ola", RegionDatabase.Alola));
    }

    // ---- The Battle Tree

    private static Party Team(int count)
    {
        var party = new Party();
        foreach (string species in new[] { "Decidueye", "Incineroar", "Primarina", "Lycanroc" }.Take(count))
            party.Add(new Pokemon(PokemonDatabase.Get(species)!, 60, new Random(party.Count + 1)));
        return party;
    }

    private static Func<int> Draws(int seed)
    {
        var rng = new Random(seed);
        return () => rng.Next(0x10000);
    }

    [Theory]
    [InlineData("single", 3, BattleFormat.Single)]
    [InlineData("double", 4, BattleFormat.Double)]
    public void EachRoomOfTheTreeHasSunAndMoonsCountAndItsTrainersBringThatMany(string way, int count, BattleFormat format)
    {
        Assert.Contains(BattleTree.Place, FacilityPlaces.Names);
        foreach (bool super in new[] { false, true })
        {
            var room = FacilityPlaces.RoomOf(BattleTree.Place, way, super);
            Assert.Equal(count, room.Entered);
            Assert.Equal(format, room.Format);
            Assert.Equal(super ? 50 : 20, room.HostAt);
            var records = new FrontierRecords();
            var challenge = StreakChallenge.Begin(records, room, Enumerable.Range(0, count).ToList(), Team(count), Draws(41));
            var foe = challenge.Opponent(records);
            Assert.Equal(count, foe.Pokemon.Count);
            Assert.All(foe.Pokemon, p => Assert.Equal(FrontierRules.Level, p.Level));
        }
    }

    [Fact]
    public void TheTreeMastersAreOursAtFiftyWithAZCrystalAndEveryMoveTheirOwn()
    {
        foreach (string way in BattleTree.Ways)
        {
            var host = BattleTree.Hosts[way];
            Assert.True(CharacterStyles.Default.Has(host.Look), $"no look {host.Look}");
            foreach (string id in new[] { host.Normal, host.Hard })
            {
                var record = TrainerDatabase.Get(id) ?? throw new Xunit.Sdk.XunitException($"{id} is no trainer");
                Assert.Contains(record, TrainerDatabase.Ours);
                Assert.True(record.ZRing, $"{id} wears no Z-Ring");
                Assert.Equal(BattleTree.EnteredIn(way), record.Party.Count);
                var party = TrainerDatabase.PartyOf(record);
                Assert.All(party.Members, p => Assert.Equal(50, p.Level));
                Assert.Contains(party.Members, p => p.HeldItem is { } item && item.Name.EndsWith("ium Z", StringComparison.Ordinal));
                foreach (var p in party.Members) Assert.Empty(Legality.Unlearnable(p));
                Assert.Equal(party.Count, party.Members.Select(p => p.HeldItem?.Name).Distinct().Count());
            }
        }
    }

    [Fact]
    public void ATreeMasterComesAtTheTwentiethAndOpensTheSuperRoomWhoseTrophyIsKeptOnce()
    {
        var records = new FrontierRecords();
        var party = Team(3);
        var challenge = StreakChallenge.Begin(records, BattleTree.Room("single", false), new[] { 0, 1, 2 }, party, Draws(5));
        for (int battle = 1; battle < 20; battle++) Assert.False(challenge.Won(records, false).OpenedHard);
        Assert.True(challenge.HostComes(records));
        Assert.Equal("tree_master_kaimana", challenge.OpponentAsTrainer(records).Id);
        var won = challenge.Won(records, false);
        Assert.True(won.Host && won.OpenedHard);
        Assert.Equal(20, won.Points);
        records.Facility(BattleTree.Place, "single", true).Streak = 49;
        var super = StreakChallenge.Begin(records, BattleTree.Room("single", true), new[] { 0, 1, 2 }, party, Draws(6));
        Assert.Equal("tree_master_kaimana_super", super.OpponentAsTrainer(records).Id);
        Assert.True(super.Won(records, trophyHeld: false).Trophy);
        // The board has a row for each room and its print is the trophies'
        var story = new StoryState();
        story.SetVar(BattleTree.TrophyVariable("single"), 1);
        var board = FrontierBoard.ForFacility(BattleTree.Place, records, story);
        Assert.Equal(FrontierPrint.Silver, board.Print);
        Assert.Equal(3, board.Rows.Count);
    }

    [Fact]
    public void TheTreesDeskBeginsAChallengeAndItsCounterSellsZCrystals()
    {
        Assert.Contains(FacilityPrizes.Of(FacilityPrizes.Tree), p => p.Item == "Firium Z");
        Assert.All(FacilityPrizes.Of(FacilityPrizes.Tree), p => Assert.NotNull(ItemDatabase.Get(p.Item)));
        var library = ScriptLibrary.Default;
        Assert.NotNull(library.Find("Desk", "PoniGauntlet_BattleTree"));
        Assert.NotNull(library.Find("OnEnter", "PoniGauntlet_BattleTreeHall"));
        var gauntlet = MapNamed("Poni");
        Assert.Contains(gauntlet.Warps, w => w.TargetMap == "PoniGauntlet_BattleTree" && !w.Shut);
    }

    private sealed class Game
    {
        public readonly StoryState Story = new();
        public readonly Party Party = new();
        public readonly Inventory Bag = new();
        private readonly Dictionary<string, Map> built = LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name);
        private readonly Func<string, Map> rooms = MapDatabase.Fresh();
        private readonly Dictionary<string, Map> cache = new();

        public Game()
        {
            Story.ChooseStarter("Litten");
            Story.SetVar("VAR_ALOLA_STORY", 54);
            Story.SetVar("VAR_ALOLA_AKALA", 8);
            Story.Set("AlolaHallOfFame");
            Party.Add(new Pokemon(PokemonDatabase.Get("Litten")!, 60));
        }

        public Map Fresh(string name) => cache.TryGetValue(name, out var m) ? m : cache[name] = built.TryGetValue(name, out var b) ? b : rooms(name);

        public HeadlessScriptHost Play(string file, string script, string map, string? person = null, int[]? answers = null,
            DayOfWeek weekday = DayOfWeek.Monday, TimeOfDay time = TimeOfDay.Day)
        {
            var at = Fresh(map);
            var host = new HeadlessScriptHost(Story, Party, Bag) { Map = at, MapNamed = Fresh, Region = RegionDatabase.Alola, Money = 50000, Weekday = weekday, TimeOfDay = time };
            foreach (int a in answers ?? Array.Empty<int>()) host.Answers.Enqueue(a);
            var library = ScriptLibrary.Default;
            var runner = new ScriptRunner(library, host);
            runner.Start(library.Find(script, file) ?? throw new Xunit.Sdk.XunitException($"{file}.{script} is missing"),
                person == null ? null : at.Everyone.Single(n => n.Key == person && (at.AreaAt(n.GridX, n.GridY)?.Key ?? file) == file));
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            return host;
        }
    }
}
