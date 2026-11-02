using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The post-game of Hoenn, part k (plan 21 · H12; docs/mechanics/rulings.md, "The Department Store's lift, Altering
/// Cave, the S.S. Tidal's halfway, Mew's hiding and the Berry Master's wife's sayings"): the Lilycove Department Store's
/// lift, Altering Cave behind the hiker of ours with its nine tables by the day, the S.S. Tidal halfway and in by its
/// steps, Mew unseen in Faraway Island's grass until found, and the five sayings that bring a rare berry once. Each scene
/// starts as the game starts it, on maps of this class's own.
/// </summary>
public class HoennPartKTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;
    private static readonly Lazy<World> Hoenn = new(() => World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Hoenn));

    private sealed class Game
    {
        private readonly Dictionary<string, Map> maps = new(StringComparer.OrdinalIgnoreCase);
        public StoryState Story { get; } = new();
        public Party Party { get; } = new();
        public Inventory Bag { get; } = new();
        public Map Map { get; private set; } = null!;
        public (int X, int Y) Tile { get; set; }
        public IReadOnlyList<string>? Phrase { get; set; } = HeadlessScriptHost.DefaultPhrase;
        public HeadlessScriptHost Last { get; private set; } = null!;

        public Game(string map, int x, int y, bool champion = true)
        {
            foreach (var m in Hoenn.Value.BuildMaps()) maps[m.Name] = m;
            StoryMigration.BeginNewGame(Story, Scripts);
            if (champion) Story.Set("HoennHallOfFame");
            Story.SetBadges("Hoenn", 0xFF);
            Party.Add(new Pokemon(PokemonDatabase.Get("Swampert")!, 70, new Random(1)));
            Arrive(map, x, y);
        }

        public Map MapOf(string name) => maps[name];

        public void Arrive(string name, int x, int y)
        {
            Map = maps[name];
            Map.ForgetForced();
            Map.ApplyPresence(Story.Has);
            Map.ApplyDoors(Story.Has);
            Tile = (x, y);
            if (Scripts.In(Map.ScriptFileAt(x, y), ScriptLibrary.OnEnter) is { } script) Play(script);
        }

        public HeadlessScriptHost Talk(string key, params int[] answers)
        {
            var npc = Map.Everyone.First(n => n.Key == key);
            Tile = (npc.GridX, npc.GridY + 1);
            return Play(Scripts.Find(FieldScripts.For(npc)!, npc.ScriptFile ?? Map.ScriptFileAt(npc.GridX, npc.GridY))!, npc, answers);
        }

        public HeadlessScriptHost Run(string name, params int[] answers) =>
            Play(Scripts.Find(name, Map.ScriptFileAt(Tile.X, Tile.Y))!, null, answers);

        public HeadlessScriptHost Play(Script script, NPC? subject = null, params int[] answers)
        {
            var host = new HeadlessScriptHost(Story, Party, Bag)
            {
                Map = Map,
                PlayerTile = Tile,
                MapNamed = name => maps.GetValueOrDefault(name),
                Fight = _ => BattleOutcome.Won,
                PhraseChoice = Phrase,
                NeedsPokemon = true
            };
            foreach (int answer in answers) host.Answers.Enqueue(answer);
            var runner = new ScriptRunner(Scripts, host);
            runner.Start(script, subject);
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            foreach (var map in maps.Values.Distinct())
            {
                map.ApplyPresence(Story.Has);
                map.ApplyDoors(Story.Has);
            }
            Tile = host.PlayerTile;
            if (host.Map is { } there && there != Map) Arrive(there.Name, host.PlayerTile.X, host.PlayerTile.Y);
            return Last = host;
        }
    }

    [Fact]
    public void TheLiftAndAlteringCaveAreHoennsAndJoined()
    {
        var hoenn = RegionDatabase.Get(RegionDatabase.Hoenn)!;
        Assert.Contains("LilycoveCity_DepartmentStoreElevator", hoenn.Maps);
        Assert.Contains("AlteringCave", hoenn.Maps);
        var built = Hoenn.Value.BuildMaps().ToDictionary(m => m.Name);
        // Every floor's lift door leads into the car, and before it there is floor to be put out on
        foreach (string floor in new[] { "1F", "2F", "3F", "4F", "5F" })
        {
            var map = built["LilycoveCity_DepartmentStore_" + floor];
            Assert.Equal("LilycoveCity_DepartmentStoreElevator", map.GetWarpAt(2, 1)?.TargetMap);
            Assert.True(map.IsWalkable(2, 2), floor);
        }
        Assert.Equal("Hoenn", built["AlteringCave"].GetWarpAt(18, 22)?.TargetMap);
        Assert.True(built["AlteringCave"].IsWalkable(18, 21));
    }

    [Fact]
    public void TheLiftCarriesThePlayerToTheFloorChosenAndOutOnIt()
    {
        var game = new Game("LilycoveCity_DepartmentStore_3F", 2, 2);
        Assert.Equal(3, game.Story.Var("VAR_DEPT_STORE_FLOOR"));
        game.Arrive("LilycoveCity_DepartmentStoreElevator", 1, 5);
        // Out at once: back onto the floor it was got into from
        game.Run("Out");
        Assert.Equal(("LilycoveCity_DepartmentStore_3F", 2, 2), (game.Map.Name, game.Tile.X, game.Tile.Y));
        // Up to the fifth (the first answer), and out there
        game.Arrive("LilycoveCity_DepartmentStoreElevator", 1, 5);
        var ride = game.Talk("teala", 0);
        Assert.Equal(5, game.Story.Var("VAR_DEPT_STORE_FLOOR"));
        Assert.Contains(ride.Log, l => l.Contains("elevator", StringComparison.OrdinalIgnoreCase));
        game.Tile = (1, 5);
        game.Run("Out");
        Assert.Equal("LilycoveCity_DepartmentStore_5F", game.Map.Name);
        // Down to the ground floor (the fifth answer); the floor it stands at is said and not ridden to
        game.Arrive("LilycoveCity_DepartmentStoreElevator", 1, 5);
        game.Talk("teala", 4);
        Assert.Equal(1, game.Story.Var("VAR_DEPT_STORE_FLOOR"));
        game.Tile = (1, 5);
        game.Run("Out");
        Assert.Equal("LilycoveCity_DepartmentStore_1F", game.Map.Name);
        // "Exit" changes nothing
        game.Arrive("LilycoveCity_DepartmentStoreElevator", 1, 5);
        game.Talk("teala", 5);
        Assert.Equal(1, game.Story.Var("VAR_DEPT_STORE_FLOOR"));
    }

    [Fact]
    public void AlteringCaveIsShutUntilTheHikerBreaksTheWayForAChampion()
    {
        var route = Hoenn.Value.BuildMaps().First(m => m.Name == "Hoenn");
        var before = new StoryState();
        route.ApplyPresence(before.Has);
        route.ApplyDoors(before.Has);
        // (the mouth is a rock wall, Emerald's own, until the door is opened)
        Assert.False(route.IsWalkable(165, 226));
        Assert.DoesNotContain(route.NPCs, n => n.Key == "altering_cave_hiker");

        var game = new Game("Hoenn", 165, 227);
        Assert.Contains(game.Map.NPCs, n => n.Key == "altering_cave_hiker");
        game.Talk("altering_cave_hiker");
        Assert.True(game.Story.Has("FLAG_HOENN_ALTERING_CAVE_OPEN"));
        Assert.True(game.Map.IsWalkable(165, 226));
        Assert.Equal("AlteringCave", game.Map.GetWarpAt(165, 226)?.TargetMap);
        // Said again, he only talks
        game.Talk("altering_cave_hiker");
        Assert.Contains(game.Last.Transcript, l => l.Text.Contains("day to day", StringComparison.Ordinal));
    }

    [Fact]
    public void AlteringCavesGrassIsOneOfNineTablesByTheDaysDraw()
    {
        var cave = Hoenn.Value.BuildMaps().First(m => m.Name == "AlteringCave");
        var area = cave.AreaAt(18, 21)!;
        Assert.Equal(8, area.LandSets.Count);
        string[] species = { "Zubat", "Mareep", "Pineco", "Houndour", "Teddiursa", "Aipom", "Shuckle", "Stantler", "Smeargle" };
        for (int set = 0; set < species.Length; set++)
        {
            var (table, rate) = cave.WildAt(18, 21, moment: new EncounterMoment { LandSet = set });
            Assert.All(table, e => Assert.Equal(species[set], e.SpeciesName));
            Assert.Equal(7, rate);
        }
        // With no moment (or a set it doesn't have) the cave's own
        Assert.All(cave.WildAt(18, 21).Table, e => Assert.Equal("Zubat", e.SpeciesName));
        Assert.All(cave.WildAt(18, 21, moment: new EncounterMoment { LandSet = 40 }).Table, e => Assert.Equal("Zubat", e.SpeciesName));

        // The first arrival of the day draws the set; later ones that day keep it
        var game = new Game("AlteringCave", 18, 21);
        Assert.True(game.Story.Has("FLAG_DAILY_ALTERING_CAVE_DRAWN"));
        int drawn = game.Story.Var(EncounterMoment.LandSetVariable);
        Assert.InRange(drawn, 0, 8);
        game.Story.SetVar(EncounterMoment.LandSetVariable, 7);
        game.Arrive("AlteringCave", 18, 21);
        Assert.Equal(7, game.Story.Var(EncounterMoment.LandSetVariable));
        // Every species of the nine is told of where the region's sources are counted
        var sources = SpeciesSources.Hoenn;
        foreach (string s in species) Assert.True(sources.Has(s), s);
    }

    [Fact]
    public void TheSSTidalIsHalfwayAfterOneRestAndInAfterTwoOrItsSteps()
    {
        var game = new Game("SSTidalRooms", 0, 0);
        var bed = Scripts.Find("Bed", "ss_tidal_rooms")!;
        game.Story.SetVar(GameEngine.CruiseStateVariable, 1);
        game.Play(bed, null, 0);
        Assert.Equal(5, game.Story.Var(GameEngine.CruiseStateVariable));
        Assert.True(GameEngine.UnderWay(5));
        game.Play(bed, null, 0);
        Assert.Equal(3, game.Story.Var(GameEngine.CruiseStateVariable));
        Assert.False(GameEngine.UnderWay(3));
        // Out of Lilycove the same, bound for Slateport
        game.Story.SetVar(GameEngine.CruiseStateVariable, 2);
        game.Play(bed, null, 0);
        Assert.Equal(6, game.Story.Var(GameEngine.CruiseStateVariable));
        // The sailor says halfway and lets nobody off
        game.Arrive("SSTidalCorridor", 1, 10);
        var sailor = game.Map.Everyone.First(n => n.Script == "ExitSailor");
        game.Talk(sailor.Key!);
        Assert.Equal("SSTidalCorridor", game.Map.Name);
        Assert.Contains(game.Last.Transcript, l => l.Text.Contains("halfway", StringComparison.OrdinalIgnoreCase));
        // The voyage's last step brings her in
        game.Story.SetVar(GameEngine.CruiseStepVariable, GameEngine.CruiseMaxSteps);
        game.Play(Scripts.Find(GameEngine.CruiseArrives, "ss_tidal_corridor")!);
        Assert.Equal(4, game.Story.Var(GameEngine.CruiseStateVariable));
        Assert.Equal(0, game.Story.Var(GameEngine.CruiseStepVariable));
        Assert.Equal(205, GameEngine.CruiseMaxSteps);
    }

    [Fact]
    public void MewHidesInTheGrassUntilItIsFound()
    {
        var game = new Game("FarawayIsland_Interior", 13, 20);
        var mew = game.Map.NPCs.First(n => n.Key == "faraway_island_mew");
        Assert.Equal("FLAG_HOENN_MEW_FOUND", mew.SeenBy);
        Assert.True(mew.OutOfSight);
        // Still in the way while unseen
        Assert.Same(mew, game.Map.GetNpcAt(mew.GridX, mew.GridY));
        // Found: seen, and met; run from, hidden again on the next visit
        game.Story.Set("FLAG_HOENN_MEW_FOUND");
        game.Map.ApplyPresence(game.Story.Has);
        Assert.False(mew.OutOfSight);
        game.Story.Unset("FLAG_HOENN_MEW_FOUND");
        game.Story.Unset("FLAG_HIDE_MEW");
        game.Arrive("FarawayIsland_Interior", 13, 20);
        Assert.True(game.Map.NPCs.First(n => n.Key == "faraway_island_mew").OutOfSight);
    }

    [Theory]
    [InlineData("feelings:great", "people:rival", "Spelon Berry", "FLAG_RECEIVED_SPELON_BERRY")]
    [InlineData("feelings:cute", "people:partner", "Pamtre Berry", "FLAG_RECEIVED_PAMTRE_BERRY")]
    [InlineData("feelings:amazing", "people:champion", "Watmel Berry", "FLAG_RECEIVED_WATMEL_BERRY")]
    [InlineData("feelings:cool", "pokemon:Latias", "Durin Berry", "FLAG_RECEIVED_DURIN_BERRY")]
    [InlineData("small:really", "feelings:determined", "Belue Berry", "FLAG_RECEIVED_BELUE_BERRY")]
    public void TheBerryMastersWifeGivesARareBerryOnceForEachOfHerSayings(string first, string second, string berry, string flag)
    {
        var game = new Game("Route123_BerryMastersHouse", 5, 5) { Phrase = new[] { first, second } };
        game.Talk("old_woman");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get(berry)!));
        Assert.True(game.Story.Has(flag));
        // The next day the same saying brings a common berry, not a second rare one
        game.Story.Unset("FLAG_DAILY_BERRY_MASTERS_WIFE");
        game.Talk("old_woman");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get(berry)!));
        // Any other saying a common one
        game.Story.Unset("FLAG_DAILY_BERRY_MASTERS_WIFE");
        game.Phrase = HeadlessScriptHost.DefaultPhrase;
        game.Talk("old_woman");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get(berry)!));
    }
}
