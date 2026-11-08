using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Facilities;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Kalos's last places (plan 23, after Z4b): the Chamber of Emptiness and Yveltal, Kiloude's station and Battle Maison,
/// the Poké Ball Factory in Laverre City, the Battle Château on Route 7, Lumiose's stone shop, Herboriste and Poké Ball
/// Boutique, and the Mega Stones' evening hour, played headless.
/// </summary>
public class KalosLastPlacesTests
{
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Kalos"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static readonly Lazy<Func<string, Map>> Rooms = new(MapDatabase.Fresh);
    private static readonly Lazy<Dictionary<Map, HashSet<(int X, int Y)>>> Walked =
        new(() => WorldWalk.From(MapNamed, RegionDatabase.Get(RegionDatabase.Kalos)!.Start!));

    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : Rooms.Value(name);

    private static readonly string[] NewRooms =
    {
        "KiloudeCity_Station", "KiloudeCity_BattleMaison", "LaverreCity_PokeBallFactory", "KalosRoute7_BattleChateau",
        "LumioseCity_Herboriste", "LumioseCity_BallBoutique"
    };

    [Fact]
    public void TheNewPlacesAreKalossAndTheirDoorsLandOnOpenGround()
    {
        var kalos = RegionDatabase.Get(RegionDatabase.Kalos)!;
        foreach (string room in NewRooms.Append("KiloudeCity_BattleMaisonHall").Append("ChamberOfEmptiness")) Assert.Contains(room, kalos.Maps);
        Assert.True(BuiltMaps.Value["ChamberOfEmptiness"].IsStreamed);
        foreach (var (map, x, y, target) in new[]
                 {
                     ("Kalos", 320, 498, "ChamberOfEmptiness"), ("Kalos", 551, 348, "KiloudeCity_Station"), ("Kalos", 592, 348, "KiloudeCity_BattleMaison"),
                     ("Kalos", 383, 190, "LaverreCity_PokeBallFactory"), ("Kalos", 188, 394, "KalosRoute7_BattleChateau"),
                     ("Kalos", 377, 317, "LumioseCity_Herboriste"), ("Kalos", 331, 397, "LumioseCity_BallBoutique")
                 })
        {
            var from = MapNamed(map);
            var warp = from.GetWarpAt(x, y);
            Assert.Equal(target, warp?.TargetMap);
            var inside = MapNamed(target);
            Assert.True(inside.IsWalkable(warp!.TargetX, warp.TargetY), $"{target}: the way in lands on a blocked tile");
            var back = inside.Warps.Single(w => w.TargetMap == map);
            Assert.True(from.IsWalkable(back.TargetX, back.TargetY), $"{target}: the way out lands on {back.TargetX},{back.TargetY} of {map}, blocked");
        }
        Assert.Equal("Chamber of Emptiness", BuiltMaps.Value["ChamberOfEmptiness"].DisplayNameAt(16, 20));
    }

    [Fact]
    public void FromTheBedroomTheWayReachesTheChamberAndEveryNewRoom()
    {
        var walked = Walked.Value;
        // The chamber's far end, where Yveltal sleeps, and its stone
        Assert.Contains(walked, p => p.Key.Name == "ChamberOfEmptiness" && p.Value.Contains((16, 8)));
        Assert.Contains(walked, p => p.Key.Name == "ChamberOfEmptiness" && p.Value.Contains((11, 10)));
        foreach (string room in NewRooms) Assert.Contains(walked, p => p.Key.Name == room);
    }

    [Fact]
    public void TheDoorsOpenWithTheStory()
    {
        var map = LoadedWorld.Value.BuildMaps().Single(m => m.Name == "Kalos");
        var story = new StoryState();
        map.ApplyDoors(story.Has);
        Assert.True(map.IsSolid(383, 190), "the factory is shut before the Fairy Badge");
        Assert.True(map.IsSolid(592, 348), "the Battle Maison is shut before the Hall of Fame");
        story.Set("FLAG_KALOS_DEFEATED_LAVERRE_GYM");
        story.Set("KalosHallOfFame");
        map.ApplyDoors(story.Has);
        Assert.False(map.IsSolid(383, 190));
        Assert.False(map.IsSolid(592, 348));
    }

    [Fact]
    public void YveltalSleepsInTheChamberAfterTheHallOfFameAndIsGoneOnceBeatenOrCaught()
    {
        var chamber = BuiltMaps.Value["ChamberOfEmptiness"];
        var story = new StoryState();
        chamber.ApplyPresence(story.Has);
        Assert.DoesNotContain(chamber.NPCs, n => n.Key == "yveltal");
        story.Set("KalosHallOfFame");
        chamber.ApplyPresence(story.Has);
        Assert.Contains(chamber.NPCs, n => n.Key == "yveltal" && (n.GridX, n.GridY) == (16, 7));
        // Run from, it stays; beaten, it is gone
        var ran = Play(story, "chamber_of_emptiness", "Yveltal", chamber, chamber.NPCs.Single(n => n.Key == "yveltal"), result: 2);
        Assert.Contains(ran.Log, l => l.StartsWith("wildbattle Yveltal 70", StringComparison.Ordinal));
        Assert.False(story.Has("FLAG_KALOS_YVELTAL_GONE"));
        Play(story, "chamber_of_emptiness", "Yveltal", chamber, chamber.NPCs.Single(n => n.Key == "yveltal"));
        Assert.True(story.Has("FLAG_KALOS_YVELTAL_GONE"));
    }

    [Fact]
    public void TheMegaStonesGlintOnlyInTheEveningHourOnceTheRingIsUpgraded()
    {
        var story = new StoryState();
        Assert.False(ClockFlags.Keep(story, ClockFlags.StonesHour));
        Assert.False(story.Has(ClockFlags.KalosStonesGlint));
        story.Set("FLAG_KALOS_MEGA_RING_UPGRADED");
        for (int hour = 0; hour < 24; hour++)
        {
            ClockFlags.Keep(story, hour);
            Assert.Equal(hour == 20, story.Has(ClockFlags.KalosStonesGlint));
        }
        // Written only when it changes: twice an evening
        int changes = Enumerable.Range(0, 48).Count(h => ClockFlags.Keep(story, h % 24));
        Assert.Equal(4, changes);
    }

    [Fact]
    public void TheStoneShopSellsEveryEvolutionStoneAndTheOwnerGivesTheKantoStoneOnce()
    {
        var shop = Rooms.Value("LumioseCity_StoneEmporium");
        var keeper = shop.Everyone.Single(n => n.Key == "keeper");
        Assert.True(keeper.IsPokeMartClerk);
        var stock = MartDatabase.Specialties[keeper.Mart!];
        foreach (string stone in new[] { "Fire Stone", "Water Stone", "Leaf Stone", "Thunder Stone", "Moon Stone", "Sun Stone", "Shiny Stone", "Dusk Stone", "Dawn Stone" })
            Assert.Contains(stone, stock);
        // The two shops behind Lumiose's last doors sell what their names say
        Assert.Contains("Luxury Ball", MartDatabase.Specialties[Rooms.Value("LumioseCity_BallBoutique").Everyone.Single(n => n.IsPokeMartClerk).Mart!]);
        Assert.Contains("Revival Herb", MartDatabase.Specialties[Rooms.Value("LumioseCity_Herboriste").Everyone.Single(n => n.IsPokeMartClerk).Mart!]);

        var story = new StoryState();
        story.Set("FLAG_KALOS_RECEIVED_KANTO_STARTER");
        var bag = new Inventory();
        Play(story, "LumioseCity_StoneEmporium", "Owner", shop, shop.Everyone.Single(n => n.Key == "owner"), bag: bag);
        Assert.Equal(0, bag.GetQuantity(ItemDatabase.Get("Charizardite Y")!));
        story.Set("KalosHallOfFame");
        Play(story, "LumioseCity_StoneEmporium", "Owner", shop, shop.Everyone.Single(n => n.Key == "owner"), bag: bag, answers: new[] { 2 });
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get("Charizardite Y")!));
        Play(story, "LumioseCity_StoneEmporium", "Owner", shop, shop.Everyone.Single(n => n.Key == "owner"), bag: bag, answers: new[] { 0 });
        Assert.Equal(0, bag.GetQuantity(ItemDatabase.Get("Venusaurite")!));
    }

    [Fact]
    public void TheFactorysPresidentGivesTheMasterBallOnceTheThreeGruntsAreBeaten()
    {
        var factory = Rooms.Value("LaverreCity_PokeBallFactory");
        var story = new StoryState();
        var bag = new Inventory();
        Play(story, "LaverreCity_PokeBallFactory", "President", factory, factory.Everyone.Single(n => n.Key == "president"), bag: bag);
        Assert.Equal(0, bag.GetQuantity(ItemDatabase.Get("Master Ball")!));
        for (int i = 1; i <= 3; i++)
        {
            Assert.NotNull(TrainerDatabase.Get($"kalos_flare_grunt_factory_{i}"));
            story.Defeat($"kalos_flare_grunt_factory_{i}");
        }
        Play(story, "LaverreCity_PokeBallFactory", "President", factory, factory.Everyone.Single(n => n.Key == "president"), bag: bag);
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get("Master Ball")!));
        Assert.True(story.Has("FLAG_KALOS_FACTORY_DONE"));
        Play(story, "LaverreCity_PokeBallFactory", "President", factory, factory.Everyone.Single(n => n.Key == "president"), bag: bag);
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get("Master Ball")!));
        factory.ApplyPresence(story.Has);
        Assert.DoesNotContain(factory.NPCs, n => n.Key.StartsWith("kalos_flare_grunt_factory", StringComparison.Ordinal));
    }

    [Fact]
    public void TheChateauDrawsItsGuestsByTheTitleAndTheDayAndHandsOutTitles()
    {
        var hall = Rooms.Value("KalosRoute7_BattleChateau");
        foreach (var n in hall.Everyone.Where(n => n.Script == "Guest")) Assert.NotNull(TrainerDatabase.Get("kalos_chateau_" + n.Key));
        var story = new StoryState();
        // A Visitor's day: the west place drawn, nobody in the east, no Leader before the Hall of Fame
        var host = Play(story, "KalosRoute7_BattleChateau", "OnEnter", hall, rolls: new[] { 1, 2 }, day: DayOfWeek.Tuesday);
        Assert.Equal(1, story.Var("VAR_KALOS_CHATEAU_GUEST_1"));
        Assert.True(story.Has("FLAG_KALOS_CHATEAU_HAS_TRISTAN"));
        Assert.False(story.Has("FLAG_KALOS_CHATEAU_HAS_CORENTIN"));
        Assert.False(story.Has("FLAG_KALOS_CHATEAU_HAS_GRANT"));
        // A friend of the house after the Hall of Fame, on a Tuesday: the east place and Grant too (the draw holds all day)
        story.SetVar("VAR_KALOS_CHATEAU_TITLE", 3);
        story.Set("KalosHallOfFame");
        Play(story, "KalosRoute7_BattleChateau", "OnEnter", hall, day: DayOfWeek.Tuesday);
        Assert.True(story.Has("FLAG_KALOS_CHATEAU_HAS_TRISTAN"));
        Assert.True(story.Has("FLAG_KALOS_CHATEAU_HAS_CORENTIN"));
        Assert.True(story.Has("FLAG_KALOS_CHATEAU_HAS_GRANT"));
        Assert.False(story.Has("FLAG_KALOS_CHATEAU_HAS_VIOLA"));

        // A win counts, once a day; the title comes at its threshold
        var fresh = new StoryState();
        var tristan = hall.Everyone.Single(n => n.Key == "tristan");
        var won = Play(fresh, "KalosRoute7_BattleChateau", "Guest", hall, tristan);
        Assert.Contains(won.Log, l => l.StartsWith("battle", StringComparison.Ordinal));
        Assert.Equal(1, fresh.Var("VAR_KALOS_CHATEAU_POINTS"));
        Play(fresh, "KalosRoute7_BattleChateau", "Guest", hall, tristan);
        Assert.Equal(1, fresh.Var("VAR_KALOS_CHATEAU_POINTS"));
        fresh.SetVar("VAR_KALOS_CHATEAU_POINTS", 2);
        fresh.Unset("FLAG_DAILY_KALOS_CHATEAU_BEAT_TRISTAN");
        Play(fresh, "KalosRoute7_BattleChateau", "Guest", hall, tristan);
        Assert.Equal(1, fresh.Var("VAR_KALOS_CHATEAU_TITLE"));
        // A loss costs nothing but the team is seen to
        var lost = new StoryState();
        var losing = Play(lost, "KalosRoute7_BattleChateau", "Guest", hall, tristan, fight: _ => BattleOutcome.Lost);
        Assert.Equal(0, lost.Var("VAR_KALOS_CHATEAU_POINTS"));
        Assert.Contains(losing.Transcript, l => l.Text.Contains("Nobody leaves the Château hurt", StringComparison.Ordinal));
    }

    [Fact]
    public void ARoundOfTheBattleMaisonIsSevenTrainersOfTheLaterSetsGrowingWithTheStreak()
    {
        var records = new FrontierRecords();
        var rng = new Random(23);
        int Draw() => rng.Next(0x10000);
        var round = BattleVenueChallenge.BeginMaison(records, new[] { 0, 1, 2 }, Draw);
        Assert.Equal(AnnexVenue.Maison, round.Venue);
        Assert.Equal(7, round.Foes.Count);
        Assert.Equal(7, round.Foes.Select(f => f.Dress!.Id).Distinct().Count());
        var (low, high) = BattleVenueChallenge.MaisonTiers(0);
        foreach (var foe in round.Foes)
        {
            Assert.Equal(3, foe.Pokemon.Count);
            Assert.All(foe.Pokemon, p => Assert.Equal(FrontierRules.Level, p.Level));
            Assert.All(foe.Pokemon, p => Assert.InRange(FacilitySetDatabase.Of(p.Species.Name).Min(s => s.Tier), 1, high));
        }
        Assert.True(records.Maison.InProgress);
        var trainer = round.OpponentAsTrainer();
        Assert.Equal(round.Foes[0].Dress!.Name, trainer.Name);
        Assert.False(round.Conditions.Palace);
        for (int i = 0; i < 7; i++) round.Won(records);
        round.Finish(records, won: true);
        Assert.Equal(7, records.Maison.Streak);
        Assert.Equal(1, records.Maison.Rounds);
        Assert.True(round.PointsEarned > 0);
        // The next round draws from stronger tiers, and the counter's prizes are the data's items
        Assert.True(BattleVenueChallenge.MaisonTiers(1).Highest > high || BattleVenueChallenge.MaisonTiers(1).Lowest > low);
        Assert.NotEmpty(FacilityPrizes.Of(FacilityPrizes.Maison));
        Assert.All(FacilityPrizes.Of(FacilityPrizes.Maison), p => Assert.NotNull(ItemDatabase.Get(p.Item)));
        Assert.Same(FacilityPrizes.Of(FacilityPrizes.Maison), FrontierExchange.Stock(FacilityPrizes.Maison));
    }

    [Fact]
    public void TheLostHotelsBinsShakeOnceADayWithTrubbishGarbodorAndOnTuesdaysRotom()
    {
        var hotel = BuiltMaps.Value["LostHotel"];
        foreach (var (x, y) in new[] { (23, 46), (27, 32), (19, 16) })
        {
            Assert.True(hotel.TileScripts.ContainsKey((x, y)), $"no bin at {x},{y}");
            Assert.True(hotel.IsSolid(x, y));
        }
        var story = new StoryState();
        Assert.Contains(Play(story, "lost_hotel", "BinLobby", hotel, rolls: new[] { 0 }).Log, l => l.StartsWith("wildbattle Trubbish 35", StringComparison.Ordinal));
        // ...once a day
        Assert.DoesNotContain(Play(story, "lost_hotel", "BinLobby", hotel).Log, l => l.StartsWith("wildbattle", StringComparison.Ordinal));
        Assert.Contains(Play(story, "lost_hotel", "BinHall", hotel, rolls: new[] { 4 }).Log, l => l.StartsWith("wildbattle Garbodor 37", StringComparison.Ordinal));
        Assert.Contains(Play(story, "lost_hotel", "BinGallery", hotel, rolls: new[] { 0 }, day: DayOfWeek.Tuesday).Log, l => l.StartsWith("wildbattle Rotom 38", StringComparison.Ordinal));
    }

    [Fact]
    public void TheTrainRunsBetweenTheTwoStations()
    {
        var story = new StoryState();
        story.Set("FLAG_KALOS_TMV_PASS");
        var station = Rooms.Value("KiloudeCity_Station");
        var host = Play(story, "KiloudeCity_Station", "Tmv", station, station.Everyone.Single(n => n.Key == "tmv"));
        Assert.Contains(host.Log, l => l.StartsWith("warp LumioseCity_Station 12 5", StringComparison.Ordinal));
        var none = Play(new StoryState(), "KiloudeCity_Station", "Tmv", station, station.Everyone.Single(n => n.Key == "tmv"));
        Assert.DoesNotContain(none.Log, l => l.StartsWith("warp", StringComparison.Ordinal));
    }

    private static HeadlessScriptHost Play(StoryState story, string file, string script, Map at, NPC? who = null, Inventory? bag = null,
        int[]? answers = null, int[]? rolls = null, DayOfWeek day = DayOfWeek.Monday, Func<NPC?, BattleOutcome>? fight = null, int? result = null)
    {
        at.ForgetForced();
        at.ApplyPresence(story.Has);
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Greninja")!, 80));
        var host = new HeadlessScriptHost(story, party, bag ?? new Inventory())
        {
            Map = at, Region = RegionDatabase.Kalos, MapNamed = MapNamed, Weekday = day,
            Fight = fight ?? (result == 2 ? _ => BattleOutcome.Fled : null)
        };
        foreach (int a in answers ?? Array.Empty<int>()) host.Answers.Enqueue(a);
        foreach (int r in rolls ?? Array.Empty<int>()) host.Rolls.Enqueue(r);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script, file) ?? throw new Xunit.Sdk.XunitException($"{file}.{script} is missing"), who);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }
}
