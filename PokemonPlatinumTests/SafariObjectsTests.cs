using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The Johto Safari Zone's objects (plan 38 · EX8; <c>docs/mechanics/rulings.md</c>, "The Johto Safari Zone's objects"):
/// HeartGold's slots and counts in the twelve areas, our objects' points and days, the slots laid into the tables, the
/// save and the Area Customizer's script.
/// </summary>
public class SafariObjectsTests
{
    private static readonly Lazy<Map> Zone =
        new(() => World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Johto).BuildMaps().Single(m => m.Name == SafariObjects.ZoneMap));

    private static MapArea Peak => Zone.Value.Areas.Single(a => a.Key == "safari_zone_04");

    private static PlacedSafariObject Placed(string key, int day, int x = 0, int y = 0) =>
        new() { Type = SafariObjects.Catalogue.Single(t => t.Key == key).Id, X = x, Y = y, Day = day };

    [Fact]
    public void EveryAreaHasHeartGoldsSlotsForObjects()
    {
        foreach (string key in SafariObjects.AreaKeys)
        {
            var area = Zone.Value.Areas.Single(a => a.Key == key);
            // Ten for the grass, three for the water and two for each rod, each asking a count of a kind or two
            Assert.Equal(10, area.SafariSlots.Count(s => s.Way == SafariWay.Land));
            Assert.Equal(3, area.SafariSlots.Count(s => s.Way == SafariWay.Water));
            Assert.All(new[] { SafariWay.OldRod, SafariWay.GoodRod, SafariWay.SuperRod }, w => Assert.Equal(2, area.SafariSlots.Count(s => s.Way == w)));
            Assert.All(area.SafariSlots, s => Assert.InRange(s.Needs.Count, 1, 2));
            Assert.All(area.SafariSlots, s => Assert.NotNull(PokemonDatabase.Get(s.Species)));
        }
        // The Peak's first: Linoone at 46 for five points of plains
        var first = Peak.SafariSlots[0];
        Assert.Equal(("Linoone", 46), (first.Species, first.Level));
        Assert.Equal((SafariObjectKind.Plains, 5), first.Needs.Single());
    }

    [Fact]
    public void AnObjectCountsFromTheDayAfterAndTwiceAfterTenDays()
    {
        var objects = new[] { Placed("haystack", 100), Placed("mushroomRing", 100) };
        Assert.Equal(0, SafariObjects.Points(objects, SafariObjectKind.Plains, 100));
        Assert.Equal(2, SafariObjects.Points(objects, SafariObjectKind.Plains, 101));
        Assert.Equal(1, SafariObjects.Points(objects, SafariObjectKind.Forest, 101));
        Assert.Equal(1, SafariObjects.Points(objects, SafariObjectKind.Water, 109));
        Assert.Equal(2, SafariObjects.Points(objects, SafariObjectKind.Water, 110));
        Assert.Equal(4, SafariObjects.Points(objects, SafariObjectKind.Plains, 110));
        Assert.Equal(0, SafariObjects.Points(objects, SafariObjectKind.Peak, 200));
    }

    [Fact]
    public void ASlotOpensAtItsCountAndDayAndNotBefore()
    {
        var linoone = Peak.SafariSlots[0];
        // Two haystacks are four points: not five until they have stood ten days
        var two = new[] { Placed("haystack", 50), Placed("haystack", 50) };
        Assert.False(SafariObjects.Opens(linoone, two, 51));
        Assert.False(SafariObjects.Opens(linoone, two, 59));
        Assert.True(SafariObjects.Opens(linoone, two, 60));
        // Three are six: open the day after they are set down, not on it
        var three = two.Append(Placed("haystack", 50)).ToArray();
        Assert.False(SafariObjects.Opens(linoone, three, 50));
        Assert.True(SafariObjects.Opens(linoone, three, 51));
        // A slot asking two kinds wants both (the Peak's Vigoroth: forest 56 and plains 28)
        var vigoroth = Peak.SafariSlots.Single(s => s.Species == "Vigoroth");
        var stumps = Enumerable.Range(0, 28).Select(_ => Placed("oldStump", 0)).ToList();
        Assert.False(SafariObjects.Opens(vigoroth, stumps, 10));
        stumps.AddRange(Enumerable.Range(0, 2).Select(_ => Placed("haystack", 0)));
        Assert.False(SafariObjects.Opens(vigoroth, stumps, 10));
        Assert.True(SafariObjects.Opens(vigoroth, Enumerable.Range(0, 28).Select(_ => Placed("oldStump", 0)).Concat(Enumerable.Range(0, 14).Select(_ => Placed("haystack", 0))).ToList(), 10));
        // No objects, nothing open
        Assert.False(SafariObjects.Opens(linoone, Array.Empty<PlacedSafariObject>(), 1000));
    }

    [Fact]
    public void TheOpenSlotsAreLaidIntoTheTablesAndNothingElseMoves()
    {
        var placed = new SafariObjects();
        // Nothing placed: the tables are the area's own, the water's the same list
        var plain = new EncounterMoment { Time = TimeOfDay.Day, SafariDay = 60 };
        var land = EncounterSlots.Grass(Peak, plain, Peak.WildEncounters);
        Assert.Equal(Peak.WildEncounters.Select(e => e.DaySpecies ?? e.SpeciesName), land.Select(e => e.SpeciesName));
        Assert.Same(Peak.WaterEncounters, EncounterSlots.Surf(Peak, Peak.WaterEncounters, plain with { SafariObjects = placed }));

        // Three haystacks of day 55 and a pond (four of water, short of the Slowbro's five), met on day 60: six of plains bring Linoone into slot 0 of the grass
        placed.Set("safari_zone_04", new[] { Placed("haystack", 55), Placed("haystack", 55), Placed("haystack", 55), Placed("pond", 55) });
        var moment = plain with { SafariObjects = placed };
        land = EncounterSlots.Grass(Peak, moment, Peak.WildEncounters);
        Assert.Equal(("Linoone", 46, 46), (land[0].SpeciesName, land[0].MinLevel, land[0].MaxLevel));
        Assert.Equal(Peak.WildEncounters.Skip(1).Select(e => e.DaySpecies ?? e.SpeciesName), land.Skip(1).Select(e => e.SpeciesName));
        // The pond's four of water are short of the water's first count, ten; three ponds of ten days are twenty-four
        Assert.Same(Peak.WaterEncounters, EncounterSlots.Surf(Peak, Peak.WaterEncounters, moment));
        placed.Set("safari_zone_04", Enumerable.Range(0, 3).Select(_ => Placed("pond", 40)));
        var water = EncounterSlots.Surf(Peak, Peak.WaterEncounters, moment);
        Assert.NotSame(Peak.WaterEncounters, water);
        Assert.Equal("Quagsire", water[0].SpeciesName);
        // The objects of another area bring nothing here
        placed.Set("safari_zone_04", Array.Empty<PlacedSafariObject>());
        placed.Set("safari_zone_01", Enumerable.Range(0, 3).Select(_ => Placed("pond", 40)));
        Assert.Same(Peak.WaterEncounters, EncounterSlots.Surf(Peak, Peak.WaterEncounters, moment));
    }

    [Fact]
    public void TheObjectsAreSavedAndReadBackAndNothingIsSavedWhileNoneStands()
    {
        var placed = new SafariObjects();
        Assert.Null(placed.ToSave());
        placed.Set("safari_zone_04", new[] { Placed("boulder", 7, 100, 50), Placed("pond", 9, 102, 52) });
        var json = System.Text.Json.JsonSerializer.Serialize(placed.ToSave());
        var back = new SafariObjects();
        back.Load(System.Text.Json.JsonSerializer.Deserialize<SafariObjectsSave>(json));
        Assert.Equal(placed.In("safari_zone_04").Select(o => (o.Type, o.X, o.Y, o.Day)), back.In("safari_zone_04").Select(o => (o.Type, o.X, o.Y, o.Day)));
        back.Set("safari_zone_04", Array.Empty<PlacedSafariObject>());
        Assert.Null(back.ToSave());
    }

    [Fact]
    public void TheAreaIsARoomOfItsOpenFloorAndKeepsWhatWasSetDown()
    {
        var map = Zone.Value;
        var bounds = map.AreaBounds("safari_zone_04")!.Value;
        var before = new List<PlacedSafariObject>();
        var store = SafariObjects.StoreOf(before);
        Assert.Equal(SafariObjects.Catalogue.Count * SafariObjects.CopiesOfEach, store.Count);
        var room = SafariObjects.Room(map, bounds, before, store);
        Assert.Equal(SafariObjects.MostInAnArea, room.Limit);
        // The way in and the area's edge are no floor; the cursor starts on floor
        Assert.Equal(RoomTile.Floor, room.TileAt!(room.Start.X, room.Start.Y));
        Assert.Equal(RoomTile.Rock, room.TileAt(bounds.X, bounds.Y));
        Assert.True(Enumerable.Range(bounds.Y, bounds.Height).Sum(y => Enumerable.Range(bounds.X, bounds.Width).Count(x => room.TileAt(x, y) == RoomTile.Floor)) > 100);

        // A boulder set down through the decorating, kept with the day
        var session = new DecoratingSession(room, store, SafariObjects.KindOf);
        session.Confirm(); // DECORATE
        int boulder = store.FindIndex(s => s.Kind == SafariObjects.Catalogue.Single(t => t.Key == "boulder").Id);
        for (int i = 0; i < boulder; i++) session.Move(0, 1);
        session.Confirm(); // the boulder in hand
        session.Confirm(); // set down at the start
        Assert.Single(room.Pieces);
        var kept = SafariObjects.Keep(room, before, 33);
        Assert.Equal((SafariObjects.Catalogue.Single(t => t.Key == "boulder").Id, room.Start.X, room.Start.Y, 33), (kept[0].Type, kept[0].X, kept[0].Y, kept[0].Day));
        // Opened again later, the boulder keeps its day
        var again = SafariObjects.Room(map, bounds, kept, SafariObjects.StoreOf(kept));
        Assert.Equal(33, SafariObjects.Keep(again, kept, 90).Single().Day);
    }

    [Fact]
    public void TheAreaCustomizerArrangesOnlyForTheOwner()
    {
        var scripts = ScriptLibrary.Default;
        var story = new StoryState();
        HeadlessScriptHost Run(params int[] answers)
        {
            var host = new HeadlessScriptHost(story, new Party(), new Inventory());
            foreach (int answer in answers) host.Answers.Enqueue(answer);
            var runner = new ScriptRunner(scripts, host);
            runner.Start(scripts.Find("Machine", "safari_zone_entrance_interior")!);
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            return host;
        }
        // Before the second test is passed, the machine only shows itself
        story.SetVar(SafariObjects.OwnerVariable, 5);
        var early = Run(3);
        Assert.Empty(early.Asked);
        Assert.Empty(early.Arranged);
        // The Owner chooses the Peak and arranges it
        story.SetVar(SafariObjects.OwnerVariable, 6);
        Assert.True(SafariObjects.IsOwner(story));
        Assert.Equal(new[] { 3 }, Run(0, 3).Arranged);
        // the Swamp from the second group, the Desert from the third
        Assert.Equal(new[] { 7 }, Run(1, 3).Arranged);
        Assert.Equal(new[] { 11 }, Run(2, 3).Arranged);
        // and backing out arranges nothing
        Assert.Empty(Run(3).Arranged);
        Assert.Empty(Run(0, 4).Arranged);
    }
}
