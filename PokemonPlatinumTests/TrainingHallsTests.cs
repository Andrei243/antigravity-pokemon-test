using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// The training halls of the other regions, TR4 (plan 36; <c>docs/mechanics/rulings.md</c>, "The training halls of the
/// other regions"): the training club's people in a corner of Kanto's, Johto's, Hoenn's and Unova's department stores and
/// Kalos's hall in Laverre City, each region's Bottle Cap in its post-game ground, Unova's feathers on the Driftveil
/// Drawbridge, and every tool to be had in each region after its Hall of Fame; with plan 41 · EG2's Champion ribbons by
/// region beside them.
/// </summary>
[Collection("Shared clocks and dice")]
public class TrainingHallsTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;
    private const string LaverreHall = "LaverreCity_TrainingHall";

    /// <summary>Each region's hall: the map it stands on and the place its scripts are found by.</summary>
    public static readonly TheoryData<string, string, string> Halls = new()
    {
        { RegionDatabase.Kanto, "CeladonCity_DepartmentStore_1F", "celadon_city_department_store_1f" },
        { RegionDatabase.Johto, "Goldenrod_DeptStore_1F", "goldenrod_department_store_1f" },
        { RegionDatabase.Hoenn, "LilycoveCity_DepartmentStore_1F", "lilycove_city_department_store_1f" },
        { RegionDatabase.Unova, "ShoppingMallNine_1F", "shopping_mall" },
        { RegionDatabase.Kalos, LaverreHall, LaverreHall },
    };

    private static NPC Person(Map map, string script) => map.Everyone.Single(n => n.Script == script);

    private static StoryState ChampionOf(string region)
    {
        var story = new StoryState();
        story.Set(RegionDatabase.Get(region)!.StoryCompleteFlag);
        return story;
    }

    private static HeadlessScriptHost Talk(string script, string place, StoryState? story, NPC? self)
    {
        var host = new HeadlessScriptHost(story, new Party());
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.Find(script, place)!, self);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    [Theory]
    [MemberData(nameof(Halls))]
    public void EachRegionsHallHasTheClubsPeopleOnOpenFloorFacingOpenFloor(string region, string mapName, string place)
    {
        var map = MapDatabase.Get(mapName);
        Assert.Equal(mapName, map.Name);
        Assert.Equal(region, RegionDatabase.RegionOfMap(mapName)!.Id);
        foreach (string script in new[] { "TrainingCoach", "TrainingClerk", "TrainingTutor" })
        {
            var who = Person(map, script);
            Assert.False(map.IsSolid(who.GridX, who.GridY), $"{script} of {mapName} stands in a wall");
            // Spoken to from the tile before them (across the counter in Laverre's hall, as in Sunyshore's)
            var (dx, dy) = who.Facing switch
            {
                Direction.Up => (0, -1), Direction.Down => (0, 1), Direction.Left => (-1, 0), _ => (1, 0)
            };
            int reach = mapName == LaverreHall ? 2 : 1;
            Assert.True(map.IsWalkable(who.GridX + dx * reach, who.GridY + dy * reach), $"{script} of {mapName} can't be spoken to");
            Assert.NotNull(Scripts.Find(script, place));
        }
        // The coach welcomes anyone to the drills; the clerk opens her stall for a Champion of the region
        var coach = Talk("TrainingCoach", place, null, Person(map, "TrainingCoach"));
        Assert.Contains(coach.Asked, q => q.Question.Contains("What'll it be today?"));
        Assert.DoesNotContain(Talk("TrainingClerk", place, null, Person(map, "TrainingClerk")).Log, l => l.StartsWith("open Shop"));
        Assert.Contains("open Shop training_hall", Talk("TrainingClerk", place, ChampionOf(region), Person(map, "TrainingClerk")).Log);
    }

    [Fact]
    public void EveryRegionHasAClubClerkForBattlePointsWithinIt()
    {
        foreach (string region in new[] { RegionDatabase.Sinnoh, RegionDatabase.Kanto, RegionDatabase.Johto, RegionDatabase.Hoenn, RegionDatabase.Unova, RegionDatabase.Kalos })
            Assert.True(ItemSources.In(region).Of("Gold Bottle Cap").Any(s => s.Kind == ItemSourceKind.Prize && s.Where.Contains("TrainingExchange")),
                $"no club clerk trades Battle Points in {region}");
    }

    // ---------------------------------------------------------------- Kalos's hall in Laverre City

    [Fact]
    public void LaverresHallIsOpenFromTheStartAndLeadsInAndOut()
    {
        var kalos = MapDatabase.Get("Kalos");
        var way = kalos.Warps.Single(w => w.TargetMap == LaverreHall);
        Assert.Null(way.OpenedBy);
        Assert.Equal((347, 217), (way.SourceX, way.SourceY));
        kalos.ApplyDoors(_ => false);
        Assert.True(kalos.IsWalkable(way.SourceX, way.SourceY + 1));
        var step = FieldMovement.Step(kalos, way.SourceX, way.SourceY + 1, Direction.Up, new Walker(Height: kalos.HeightAt(way.SourceX, way.SourceY + 1)));
        Assert.NotEqual(StepKind.Blocked, step.Kind);

        var room = MapDatabase.Get(LaverreHall);
        var back = room.Warps.Single();
        Assert.Equal("Kalos", back.TargetMap);
        Assert.Equal((way.SourceX, way.SourceY + 1), (back.TargetX, back.TargetY));
        Assert.True(room.IsWalkable(way.TargetX, way.TargetY));
        // On the hall's one plan: the targets, the mats and the counter of Sunyshore's
        var sunyshore = MapDatabase.Get(GameEngine.SunyshoreTrainingHall);
        Assert.Equal(sunyshore.Props.Select(p => (p.Type, p.X, p.Y)), room.Props.Select(p => (p.Type, p.X, p.Y)));
    }

    [Fact]
    public void AWalkFromLaverreCityReachesItsHallAndItsCoach()
    {
        var walked = WorldWalk.From(MapDatabase.Get, new[] { new MapSpot("Kalos", 347, 220, Direction.Down) });
        var room = walked.Keys.SingleOrDefault(m => m.Name == LaverreHall);
        Assert.NotNull(room);
        var coach = Person(room!, "TrainingCoach");
        Assert.Contains((coach.GridX, coach.GridY + 2), walked[room!]);
        var club = Person(room!, "TrainingExchange");
        Assert.Contains((club.GridX - 1, club.GridY), walked[room!]);
    }

    // ---------------------------------------------------------------- the Bottle Caps and the feathers

    public static readonly TheoryData<string, string> Caps = new()
    {
        { RegionDatabase.Kanto, "five_island_meadow.BottleCap" },
        { RegionDatabase.Johto, "route_28.BottleCap" },
        { RegionDatabase.Hoenn, "battle_frontier_outside_west.BottleCap" },
        { RegionDatabase.Unova, "nature_preserve.BottleCap" },
        { RegionDatabase.Kalos, "kiloude_city.BottleCap" },
    };

    [Theory]
    [MemberData(nameof(Caps))]
    public void EachRegionHasABottleCapOnOpenGroundInItsPostGame(string region, string key)
    {
        var found = MapDatabase.MapNames.Where(n => RegionDatabase.RegionOfMap(n)?.Id == region)
            .Select(MapDatabase.Get).SelectMany(m => m.Everyone.Where(n => n.Key == key).Select(n => (Map: m, Ball: n))).ToList();
        var (map, ball) = Assert.Single(found);
        Assert.True(ball.IsItemBall);
        Assert.Equal("Bottle Cap", ball.Item);
        Assert.False(map.IsSolid(ball.GridX, ball.GridY), $"{key} lies in a wall of {map.Name}");
        Assert.True(new[] { (0, 1), (0, -1), (1, 0), (-1, 0) }.Any(d => map.IsWalkable(ball.GridX + d.Item1, ball.GridY + d.Item2)),
            $"{key} can't be reached on {map.Name}");
        Assert.Contains(ItemSources.In(region).Of("Bottle Cap"), s => s.Kind == ItemSourceKind.Ball);
    }

    [Fact]
    public void TheDrawbridgesFeathersAreFoundOnceADayAndComeBackTheNext()
    {
        var bridge = MapDatabase.Get("DriftveilDrawbridge");
        var places = new[] { ("FeatherWest", (130, 47)), ("FeatherMiddle", (155, 45)), ("FeatherEast", (175, 48)) };
        foreach (var (_, tile) in places) Assert.True(bridge.TileScripts.ContainsKey(tile));

        var story = new StoryState();
        var host = new HeadlessScriptHost(story, new Party());
        int Feathers() => new[] { "Health", "Muscle", "Resist", "Genius", "Clever", "Swift" }
            .Sum(f => host.Bag.GetQuantity(ItemDatabase.Get(f + " Feather")!));
        void Look(string script)
        {
            var runner = new ScriptRunner(Scripts, host);
            runner.Start(Scripts.Find(script, "driftveil_drawbridge")!, null);
            runner.RunToEnd();
            Assert.Empty(host.Problems);
        }

        // The day's die chooses which: a 5 is a Swift Feather, a 0 a Health Feather
        host.Rolls.Enqueue(5);
        Look("FeatherWest");
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("Swift Feather")!));
        Look("FeatherMiddle");
        Look("FeatherEast");
        Assert.Equal(3, Feathers());
        // Looked at again the same day, nothing is there
        foreach (var (script, _) in places) Look(script);
        Assert.Equal(3, Feathers());
        // A new day clears the daily flags, and each lies there again
        story.ClearDaily();
        foreach (var (script, _) in places) Look(script);
        Assert.Equal(6, Feathers());
    }

    // ---------------------------------------------------------------- every tool in every region

    [Theory]
    [InlineData(RegionDatabase.Kanto)]
    [InlineData(RegionDatabase.Johto)]
    [InlineData(RegionDatabase.Hoenn)]
    [InlineData(RegionDatabase.Unova)]
    [InlineData(RegionDatabase.Kalos)]
    public void EveryToolCanBeHadInEachRegionAfterItsHallOfFame(string region)
    {
        var sources = ItemSources.In(region);
        var missing = TrainingToolsTests.Tools().Where(t => !sources.Has(t) && !TrainingToolsTests.Owed.ContainsKey(t) && !OwedIn(region).ContainsKey(t)).ToList();
        Assert.True(missing.Count == 0, $"no way of {region} gives: " + string.Join(", ", missing));
        var stale = OwedIn(region).Keys.Where(sources.Has).ToList();
        Assert.True(stale.Count == 0, $"owed in {region}, yet given there now (take their lines off): " + string.Join(", ", stale));
    }

    /// <summary>What a region beyond Sinnoh still owes of the tools Sinnoh gives, with the plan that owes it.</summary>
    private static IReadOnlyDictionary<string, string> OwedIn(string region) => region switch
    {
        _ => new Dictionary<string, string>()
    };

    // ---------------------------------------------------------------- plan 41 · EG2: the Champion ribbons by region

    [Fact]
    public void EachRegionsHallOfFameHandsItsOwnChampionRibbon()
    {
        Assert.Equal(RibbonRules.SinnohChamp, RibbonRules.ChampionOf(RegionDatabase.Sinnoh));
        Assert.Equal(RibbonRules.SinnohChamp, RibbonRules.ChampionOf(RegionDatabase.Kanto));
        Assert.Equal(RibbonRules.HoennChamp, RibbonRules.ChampionOf(RegionDatabase.Hoenn));
        Assert.Equal(RibbonRules.KalosChamp, RibbonRules.ChampionOf(RegionDatabase.Kalos));
        Assert.Equal(RibbonRules.AlolaChamp, RibbonRules.ChampionOf(RegionDatabase.Alola));
        foreach (string ribbon in new[] { RibbonRules.KalosChamp, RibbonRules.AlolaChamp, RibbonRules.GalarChamp, RibbonRules.PaldeaChamp })
        {
            Assert.NotNull(RibbonDatabase.Get(ribbon));
            Assert.Equal(RibbonKind.Champion, RibbonRules.Kind(ribbon));
            Assert.NotEqual("A ribbon.", RibbonRules.Describe(ribbon));
        }
        Assert.Null(RibbonRules.WaitsFor(RibbonRules.KalosChamp));
        Assert.Null(RibbonRules.WaitsFor(RibbonRules.HoennChamp));
        Assert.NotNull(RibbonRules.WaitsFor(RibbonRules.GalarChamp));

        foreach (var (region, ribbon) in new[] { (RegionDatabase.Kalos, RibbonRules.KalosChamp), (RegionDatabase.Hoenn, RibbonRules.HoennChamp), (RegionDatabase.Unova, RibbonRules.SinnohChamp) })
        {
            var library = ScriptLibrary.FromSources(("test", "script Enter\n  halloffame\n"), ("common", ""));
            var host = new HeadlessScriptHost { Region = region };
            host.Party.Add(new Pokemon(PokemonDatabase.Get("Bidoof")!, 50));
            var runner = new ScriptRunner(library, host);
            runner.Start(library.All.First(), null);
            runner.RunToEnd();
            var worn = host.Party.Members[0].Ribbons;
            Assert.Contains(ribbon, worn);
            // No Pokémon wears a ribbon of a region it hasn't finished
            Assert.Single(worn, r => RibbonRules.Kind(r) == RibbonKind.Champion);
        }
    }
}
