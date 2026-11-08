using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Lumiose's restaurants (plan 37 · FC5) and the rooms opened beside them: the Bistro des Deux's doubles, La Table à
/// Trois's triples, Le Carrousel's rotations and La Grande Table's paid menu of one of each, each three courses against
/// its staff once a day with a dish by the turns they took; and the grooming salon, the Gazette, the gallery, the
/// television's lobby and the trade society that gives the Linking Cord. Played headless.
/// </summary>
[Collection("Shared clocks and dice")]
public class LumioseRestaurantTests
{
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Kalos"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static readonly Lazy<Func<string, Map>> Rooms = new(MapDatabase.Fresh);

    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : Rooms.Value(name);

    /// <summary>A restaurant: its room, its daily flag, its staff by their person and trainer, their format, and its dishes by band of turns.</summary>
    private sealed record Restaurant(string Room, string Flag, (string Who, string Id, string Format)[] Staff, (int Turns, string Item, int Count)[] Dishes, int Price = 0);

    private static readonly Restaurant[] Restaurants =
    {
        new("LumioseCity_BistroDeux", "FLAG_DAILY_KALOS_RESTAURANT_1",
            new[] { ("odette", "kalos_restaurant_bistro_odette", "double"), ("fabien", "kalos_restaurant_bistro_fabien", "double"), ("bertrand", "kalos_restaurant_bistro_bertrand", "double") },
            new[] { (4, "Rare Candy", 1), (8, "Lum Berry", 2), (12, "Sitrus Berry", 2), (13, "Oran Berry", 3) }),
        new("LumioseCity_TableATrois", "FLAG_DAILY_KALOS_RESTAURANT_2",
            new[] { ("raoul", "kalos_restaurant_trois_raoul", "triple"), ("agnes", "kalos_restaurant_trois_agnes", "triple"), ("heloise", "kalos_restaurant_trois_heloise", "triple") },
            new[] { (6, "PP Up", 1), (10, "Rare Candy", 1), (15, "Leppa Berry", 2), (16, "Oran Berry", 3) }),
        new("LumioseCity_Carrousel", "FLAG_DAILY_KALOS_RESTAURANT_3",
            new[] { ("marius", "kalos_restaurant_carrousel_marius", "rotation"), ("capucine", "kalos_restaurant_carrousel_capucine", "rotation"), ("ferdinand", "kalos_restaurant_carrousel_ferdinand", "rotation") },
            new[] { (6, "Rare Candy", 2), (10, "Lum Berry", 2), (15, "Sitrus Berry", 2), (16, "Leppa Berry", 1) }),
        new("LumioseCity_GrandeTable", "FLAG_DAILY_KALOS_RESTAURANT_4",
            new[] { ("severine", "kalos_restaurant_grande_severine", "double"), ("evrard", "kalos_restaurant_grande_evrard", "triple"), ("hippolyte", "kalos_restaurant_grande_hippolyte", "rotation") },
            new[] { (6, "Ability Capsule", 1), (10, "PP Max", 1), (15, "PP Up", 2), (16, "Rare Candy", 2) }, Price: 15000),
    };

    private static readonly (int X, int Z, string Room)[] Doors =
    {
        (342, 377, "LumioseCity_BistroDeux"), (344, 347, "LumioseCity_TableATrois"), (407, 377, "LumioseCity_Carrousel"),
        (379, 327, "LumioseCity_GrandeTable"), (330, 387, "LumioseCity_GroomingSalon"), (380, 397, "LumioseCity_Gazette"),
        (342, 327, "LumioseCity_Gallery"), (406, 347, "LumioseCity_TelevisionLobby"), (340, 397, "LumioseCity_TradeSociety")
    };

    [Fact]
    public void TheDoorsLeadIntoTheRoomsOfKalosAndBackOntoOpenGround()
    {
        var kalos = RegionDatabase.Get(RegionDatabase.Kalos)!;
        foreach (var (x, z, room) in Doors)
        {
            Assert.Contains(room, kalos.Maps);
            var from = MapNamed("Kalos");
            var warp = from.GetWarpAt(x, z);
            Assert.Equal(room, warp?.TargetMap);
            var inside = MapNamed(room);
            Assert.True(inside.IsWalkable(warp!.TargetX, warp.TargetY), $"{room}: the way in lands on a blocked tile");
            var back = inside.Warps.Single(w => w.TargetMap == "Kalos");
            Assert.Equal((x, z + 1), (back.TargetX, back.TargetY));
            Assert.True(from.IsWalkable(back.TargetX, back.TargetY), $"{room}: the way out lands on a blocked tile");
        }
    }

    [Fact]
    public void TheStaffAreKalossTrainersInTheirRestaurantsFormatAStepAboveTheStreets()
    {
        foreach (var r in Restaurants)
        {
            var room = Rooms.Value(r.Room);
            foreach (var (who, id, format) in r.Staff)
            {
                Assert.Contains(room.Everyone, n => n.Key == who);
                var record = TrainerDatabase.Get(id, "kalos");
                Assert.NotNull(record);
                Assert.Contains(record!, TrainerDatabase.OfRegion("kalos"));
                var trainer = new Trainer { Id = id };
                TrainerDatabase.Fill(trainer, record);
                switch (format)
                {
                    case "double": Assert.True(trainer.DoubleBattle); Assert.Null(trainer.AskedFormat); break;
                    case "triple": Assert.Equal(BattleFormat.Triple, trainer.AskedFormat); break;
                    case "rotation": Assert.Equal(BattleFormat.Rotation, trainer.AskedFormat); break;
                }
                Assert.True(trainer.Party.Count >= (format == "double" ? 2 : 3), $"{id} can't field its format");
                // A step above Lumiose's streets and the routes round them (Routes 4 and 5 to 22, 13 and 14 to 33), under
                // Clemont's 37; the paid table a step higher again
                int lowest = r.Price > 0 ? 36 : 30, highest = r.Price > 0 ? 38 : 34;
                Assert.All(trainer.Party.Members, p => Assert.InRange(p.Level, lowest, highest));
            }
        }
    }

    [Fact]
    public void EachMenuIsThreeCoursesAndGivesItsDishByTheTurnsTheyTook()
    {
        foreach (var r in Restaurants)
            foreach (var (turns, item, count) in r.Dishes)
            {
                var story = new StoryState();
                var seen = new List<NPC?>();
                // The three courses share the turns out between them
                int course = 0;
                var host = Play(story, r, money: 20000, fight: (h, foe) =>
                {
                    seen.Add(foe);
                    h.BattleTurns = turns / 3 + (course++ < turns % 3 ? 1 : 0);
                    return BattleOutcome.Won;
                });
                Assert.Equal(r.Staff.Select(s => $"battle {s.Id} Won"), host.Log.Where(l => l.StartsWith("battle", StringComparison.Ordinal)));
                Assert.Equal(r.Staff.Select(s => s.Who), seen.Select(n => n!.Key));
                Assert.Equal(count, host.Bag.GetQuantity(ItemDatabase.Get(item)!));
                Assert.True(story.Has(r.Flag), $"{r.Room}: the menu was served and isn't marked for the day");
                foreach (var (_, other, _) in r.Dishes.Where(d => d.Item != item))
                    Assert.Equal(0, host.Bag.GetQuantity(ItemDatabase.Get(other)!));
                Assert.Equal(20000 - r.Price, host.Money);
            }
    }

    [Fact]
    public void AMenuIsServedOnceADay()
    {
        foreach (var r in Restaurants)
        {
            var story = new StoryState();
            Play(story, r, money: 20000);
            var again = Play(story, r, money: 20000);
            Assert.DoesNotContain(again.Log, l => l.StartsWith("battle", StringComparison.Ordinal));
            Assert.Equal(20000, again.Money);
            Assert.Contains(again.Transcript, l => l.Text.Contains("tomorrow", StringComparison.Ordinal));
            // The next day the kitchen is open again
            story.Unset(r.Flag);
            Assert.Contains(Play(story, r, money: 20000).Log, l => l.StartsWith("battle", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void AMealLostIsTheDaysMealWithNoDishAndTheTeamSeenTo()
    {
        foreach (var r in Restaurants)
        {
            var story = new StoryState();
            var host = Play(story, r, money: 20000, fight: (_, foe) => foe!.Key == r.Staff[1].Who ? BattleOutcome.Lost : BattleOutcome.Won);
            Assert.Equal(2, host.Log.Count(l => l.StartsWith("battle", StringComparison.Ordinal)));
            Assert.True(story.Has(r.Flag));
            Assert.All(r.Dishes, d => Assert.Equal(0, host.Bag.GetQuantity(ItemDatabase.Get(d.Item)!)));
            Assert.Contains(host.Transcript, l => l.Text.Contains("seen to", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void ThePaidMenuIsRefusedWithoutTheMoneyAndKeptForTheDay()
    {
        var grande = Restaurants.Single(r => r.Price > 0);
        var story = new StoryState();
        var poor = Play(story, grande, money: grande.Price - 1);
        Assert.DoesNotContain(poor.Log, l => l.StartsWith("battle", StringComparison.Ordinal));
        Assert.Equal(grande.Price - 1, poor.Money);
        Assert.False(story.Has(grande.Flag));
        var paid = Play(story, grande, money: grande.Price);
        Assert.Equal(3, paid.Log.Count(l => l.StartsWith("battle", StringComparison.Ordinal)));
        Assert.True(story.Has(grande.Flag));
        // No for an answer takes nothing either
        var declined = Play(new StoryState(), grande, money: 20000, answers: new[] { 1 });
        Assert.Equal(20000, declined.Money);
        // And the free three never ask for money
        foreach (var r in Restaurants.Where(r => r.Price == 0))
            Assert.Equal(3, Play(new StoryState(), r, money: 0).Log.Count(l => l.StartsWith("battle", StringComparison.Ordinal)));
    }

    [Fact]
    public void NoLineOfTheRestaurantsNamesASpecies()
    {
        // The staff are relevelled and grown in a crossed region (plan 18 · W4b-2), so what they bring is named by nobody
        var names = PokemonDatabase.Everyone().Select(s => s.Name).Where(n => n.Length > 2).ToArray();
        foreach (var r in Restaurants)
        {
            var host = Play(new StoryState(), r, money: 20000);
            var room = Rooms.Value(r.Room);
            var lines = host.Transcript.Select(t => t.Text)
                .Concat(room.Everyone.SelectMany(n => n.DialogLines))
                .Concat(room.Everyone.Where(n => n.Script == "Staff").SelectMany(n => Play(new StoryState(), r, money: 20000, who: n, script: "Staff").Transcript.Select(t => t.Text)));
            foreach (string line in lines)
                foreach (string name in names)
                    Assert.False(Regex.IsMatch(line, $@"(?<!\p{{L}}){Regex.Escape(name)}(?!\p{{L}})"), $"{r.Room}: \"{line}\" names {name}");
        }
    }

    [Fact]
    public void TheTradeSocietyGivesTheLinkingCordOnceAndItsCounterSellsMore()
    {
        var room = Rooms.Value("LumioseCity_TradeSociety");
        var story = new StoryState();
        var bag = new Inventory();
        var host = Play(story, "LumioseCity_TradeSociety", "Host", room, room.Everyone.Single(n => n.Key == "host"), bag);
        var cord = ItemDatabase.Get(Evolution.LinkingCord)!;
        Assert.Equal(1, bag.GetQuantity(cord));
        Assert.True(story.Has("FLAG_KALOS_TRADE_SOCIETY_CORD"));
        Play(story, "LumioseCity_TradeSociety", "Host", room, room.Everyone.Single(n => n.Key == "host"), bag);
        Assert.Equal(1, bag.GetQuantity(cord));
        var clerk = room.Everyone.Single(n => n.IsPokeMartClerk);
        Assert.Equal("lumiose_trades", clerk.Mart);
        Assert.Contains(Evolution.LinkingCord, MartDatabase.Specialties["lumiose_trades"]);
        // Kalos itself now has the cord, for its Pokémon that grow when traded
        Assert.True(SpeciesSources.Of(RegionDatabase.Kalos).Items.ContainsKey(Evolution.LinkingCord));
    }

    [Fact]
    public void ARestaurantsTripleAndRotationAreFoughtInTheirFormatInAGame()
    {
        // As the menus' `battle <who> as` hands the engine a person carrying the record (TrainerDatabase.Fill), so here:
        // a triple, a rotation and a double, each from its staff's record, begun as the field begins a trainer's battle
        foreach (var (id, format) in new[]
                 {
                     ("kalos_restaurant_trois_raoul", BattleFormat.Triple), ("kalos_restaurant_carrousel_marius", BattleFormat.Rotation),
                     ("kalos_restaurant_bistro_odette", BattleFormat.Double)
                 })
        {
            InAGame(engine =>
            {
                var game = engine.Drive;
                var trainer = new Trainer { Id = id };
                TrainerDatabase.Fill(trainer, TrainerDatabase.Get(id)!);
                game.StartTrainerBattle(new NPC { NpcType = "waiter", TrainerData = trainer });
                Until(engine, () => game.State == GameState.Battle, "the battle");
                Assert.Equal(format, game.Battle!.Format);
            });
        }
    }

    // ------------------------------------------------------------------ helpers

    private static HeadlessScriptHost Play(StoryState story, Restaurant r, int money, Func<HeadlessScriptHost, NPC?, BattleOutcome>? fight = null,
        int[]? answers = null, NPC? who = null, string script = "Host")
    {
        var room = Rooms.Value(r.Room);
        return Play(story, r.Room, script, room, who ?? room.Everyone.Single(n => n.Key == "host"), null, answers, money, fight);
    }

    private static HeadlessScriptHost Play(StoryState story, string file, string script, Map at, NPC? who = null, Inventory? bag = null,
        int[]? answers = null, int money = 3000, Func<HeadlessScriptHost, NPC?, BattleOutcome>? fight = null)
    {
        at.ForgetForced();
        at.ApplyPresence(story.Has);
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Greninja")!, 60));
        party.Add(new Pokemon(PokemonDatabase.Get("Talonflame")!, 60));
        party.Add(new Pokemon(PokemonDatabase.Get("Aegislash")!, 60));
        var host = new HeadlessScriptHost(story, party, bag ?? new Inventory())
        {
            Map = at, Region = RegionDatabase.Kalos, MapNamed = MapNamed, Money = money
        };
        // A refused menu would pay nothing: the staff's prize money is kept out of the purse's count
        host.Fight = foe =>
        {
            var outcome = fight?.Invoke(host, foe) ?? BattleOutcome.Won;
            if (outcome == BattleOutcome.Won && foe?.TrainerData is { } t) host.Money -= t.PrizeMoney;
            return outcome;
        };
        foreach (int a in answers ?? Array.Empty<int>()) host.Answers.Enqueue(a);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script, file) ?? throw new Xunit.Sdk.XunitException($"{file}.{script} is missing"), who);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    private const float Frame = 1f / 60f;

    private static void InAGame(Action<GameEngine> test)
    {
        string folder = Path.Combine(Path.GetTempPath(), "restaurant-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(37);
            var engine = new GameEngine(new GameSettings { Muted = true });
            engine.InitializeHeadless();
            engine.PeopleStayPut = true;
            var game = engine.Drive;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
            foreach (string s in new[] { "Greninja", "Talonflame", "Aegislash" }) game.Party.Members.Insert(0, new Pokemon(PokemonDatabase.Get(s)!, 60));
            game.State = GameState.Overworld;
            game.TrainersLookOnArrival = false;
            test(engine);
        }
        finally
        {
            InputManager.Feed(null);
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }

    private static void Until(GameEngine engine, Func<bool> done, string what, int most = 60 * 30)
    {
        for (int f = 0; f < most; f++)
        {
            if (done()) return;
            InputManager.Feed(ActionFrame.None);
            engine.Update(Frame);
        }
        Assert.Fail($"Waited {most / 60} seconds of game time for {what}; the game is in {engine.Drive.State}.");
    }
}
