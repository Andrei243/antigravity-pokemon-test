using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 38 · EX5: rifts in the field. An hour's rift is worked out from the day's number, the hour and the region, never drawn:
/// the same hour the same rift, others on other hours, none before the Hall of Fame; its table within its reach and not past
/// it; its three finds on walkable ground of its place, under daily flags.
/// </summary>
[Collection("Shared clocks and dice")]
public class RiftTests
{
    private static readonly string[] Regions = { RegionDatabase.Sinnoh, RegionDatabase.Kanto, RegionDatabase.Johto, RegionDatabase.Hoenn, RegionDatabase.Unova };

    private static Map OverworldOf(string region) => MapDatabase.Get(RegionDatabase.Get(region)!.OverworldMap!);

    [Fact]
    public void TheTablesHoldSpeciesAndItemsTheDataHasAtLevelsOfAPokemon()
    {
        foreach (string region in Regions)
        {
            var table = Rifts.TableOf(region);
            Assert.NotNull(table);
            Assert.NotEmpty(table!.Grass);
            Assert.All(table.Grass.Concat(table.Water), s =>
            {
                var species = PokemonDatabase.Get(s);
                Assert.NotNull(species);
                Assert.False(species!.Legendary || species.Mythical || species.IsOurs, s);
            });
            Assert.All(table.Finds, i => Assert.NotNull(ItemDatabase.Get(i)));
            Assert.InRange(table.MinLevel, 1, 100);
            Assert.InRange(table.MaxLevel, table.MinLevel, 100);
        }
        Assert.Null(Rifts.TableOf("Nowhere"));
    }

    [Fact]
    public void AnHoursRiftIsWorkedOutTheSameEveryTimeAndOtherHoursGiveOthersOrNone()
    {
        var map = OverworldOf(RegionDatabase.Sinnoh);
        uint daily = 0x5EEDu;
        var day = Outbreaks.Today(RegionDatabase.Sinnoh, daily);
        var seen = new List<Rift?>();
        for (int hour = 0; hour < 24; hour++)
        {
            var once = Rifts.Of(RegionDatabase.Sinnoh, daily, hour, map, day);
            var twice = Rifts.Of(RegionDatabase.Sinnoh, daily, hour, map, day);
            Assert.Equal(once?.Area, twice?.Area);
            Assert.Equal(once?.X, twice?.X);
            Assert.Equal(once?.Finds, twice?.Finds);
            seen.Add(once);
        }
        int open = seen.Count(r => r != null);
        Assert.InRange(open, 3, 16);
        Assert.True(seen.Where(r => r != null).Select(r => r!.Area).Distinct().Count() >= 2);
        foreach (var rift in seen.OfType<Rift>())
        {
            // Never at one of the day's outbreaks, its centre in its own tall grass, its finds on walkable ground of its place
            Assert.Null(Outbreaks.At(day, rift.Area));
            Assert.Equal(rift.Area, map.AreaAt(rift.X, rift.Y)!.Key);
            Assert.Equal(Rifts.FindCount, rift.Finds.Count);
            Assert.Equal(Rifts.FindCount, rift.Finds.Select(f => (f.X, f.Y)).Distinct().Count());
            Assert.All(rift.Finds, f =>
            {
                Assert.True(map.IsWalkable(f.X, f.Y));
                Assert.Equal(rift.Area, map.AreaAt(f.X, f.Y)!.Key);
                Assert.Contains(f.Item, Rifts.TableOf(RegionDatabase.Sinnoh)!.Finds);
            });
        }
    }

    [Fact]
    public void EveryRegionWithATableHasRiftsOnItsOwnGround()
    {
        foreach (string region in Regions)
        {
            var map = OverworldOf(region);
            int open = 0;
            uint daily = 7;
            for (int day = 0; day < 4; day++, daily = SpecialEncounters.Next(daily))
                for (int hour = 0; hour < 24; hour++)
                    if (Rifts.Of(region, daily, hour, map, Outbreaks.Today(region, daily)) is { } rift)
                    {
                        open++;
                        Assert.Equal(rift.Area, map.AreaAt(rift.X, rift.Y)!.Key);
                    }
            Assert.True(open >= 10, $"{region}: only {open} rifts in four days");
        }
    }

    [Fact]
    public void NoneOpensBeforeTheHallOfFame()
    {
        var sinnoh = RegionDatabase.Get(RegionDatabase.Sinnoh)!;
        var story = new StoryState();
        Assert.False(Rifts.Open(sinnoh, story));
        story.Set(sinnoh.StoryCompleteFlag);
        Assert.True(Rifts.Open(sinnoh, story));
    }

    [Fact]
    public void TheRiftsTableIsMetWithinItsReachAndNotPastIt()
    {
        var rift = new Rift(RegionDatabase.Sinnoh, "route_225", 100, 100, Array.Empty<(int, int, string)>());
        Assert.True(rift.Holds(112, 88));
        Assert.False(rift.Holds(113, 100));
        Assert.False(rift.Holds(100, 87));

        var area = new MapArea { Key = "route_225", Open = true };
        foreach (int weight in WorldAreaFile.LandSlotWeights)
            area.WildEncounters.Add(new WildEncounterEntry { SpeciesName = "Bidoof", MinLevel = 20, MaxLevel = 24, Weight = weight });
        var table = Rifts.TableOf(RegionDatabase.Sinnoh)!;
        var slots = EncounterSlots.Grass(area, new EncounterMoment { Rift = table });
        for (int i = 0; i < 12; i++)
        {
            Assert.Equal(i < Rifts.GrassSlots, slots[i].Rift);
            if (i < Rifts.GrassSlots)
            {
                Assert.Equal(table.Grass[i % table.Grass.Count], slots[i].SpeciesName);
                Assert.Equal(table.MinLevel, slots[i].MinLevel);
            }
            else Assert.Equal("Bidoof", slots[i].SpeciesName);
        }
        Assert.All(EncounterSlots.Grass(area, new EncounterMoment()), s => Assert.False(s.Rift));
        var rod = Rifts.Water(new[] { new WildEncounterEntry { SpeciesName = "Magikarp", Weight = 70 }, new WildEncounterEntry { SpeciesName = "Magikarp", Weight = 30 } }, table);
        Assert.Equal(table.Water[0], rod[0].SpeciesName);
        Assert.Equal(70, rod[0].Weight);
        Assert.True(rod[1].Rift);
    }

    [Fact]
    public void TheFindsFlagsAreDailyAndOneEach()
    {
        Assert.Equal(Rifts.FindCount, Enumerable.Range(0, Rifts.FindCount).Select(Rifts.FindFlag).Distinct().Count());
        Assert.All(Enumerable.Range(0, Rifts.FindCount).Select(Rifts.FindFlag), f => Assert.StartsWith("FLAG_DAILY_", f));
        var story = new StoryState();
        story.Set(Rifts.FindFlag(0));
        story.ClearDaily();
        Assert.False(story.Has(Rifts.FindFlag(0)));
    }
}

/// <summary>Plan 38 · EX5, the engine's part: the hour's rift in a game, its finds in the ground and its table in the moment.</summary>
[Collection("Shared clocks and dice")]
public class RiftGameTests
{
    [Fact]
    public void AGameLaysTheHoursRiftAndASaveMadeInItLoadsIntoItWithinItsHour()
    {
        var engine = new GameEngine(new GameSettings { Muted = true });
        engine.InitializeHeadless();
        engine.PeopleStayPut = true;
        engine.NewGameRegion = "Sinnoh";
        engine.StartNewGame();
        var game = engine.Drive;
        var before = GameClock.Fixed;
        try
        {
            Assert.Null(engine.RiftNow());
            game.Story.Set("SinnohHallOfFame");
            // Find an hour of the day with a rift
            Rift? rift = null;
            for (uint daily = 1; daily < 40 && rift == null; daily++)
                foreach (var time in Enum.GetValues<TimeOfDay>())
                {
                    game.Encounters.DailyNumber = daily;
                    GameClock.Fixed = time;
                    if ((rift = engine.RiftNow()) != null) break;
                }
            Assert.NotNull(rift);
            // The same day's number and hour give the same rift: what a save made in it loads into
            var again = Rifts.Of(RegionDatabase.Sinnoh, game.Encounters.DailyNumber, (int)GameClock.Hour, game.Map.Name == "Sinnoh" ? game.Map : MapDatabase.Get("Sinnoh"),
                Outbreaks.Today(RegionDatabase.Sinnoh, game.Encounters.DailyNumber, Swarms.PlaceToday(game.Encounters, RegionDatabase.Sinnoh)?.Area));
            Assert.Equal(rift!.Area, again!.Area);
            Assert.Equal((rift.X, rift.Y), (again.X, again.Y));
            // The report tells it
            Assert.Equal(rift.Area, engine.FieldReportNow()!.Rift!.Area);
        }
        finally
        {
            GameClock.Fixed = before;
            Ruleset.Use(RulesPreset.Platinum);
        }
    }
}
