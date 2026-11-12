using System;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 40 · GE9: festivals on the calendar (Models/Festivals.cs, Data/festivals.json; rulings, "Festivals on the
/// calendar"). Each window keeps its days and no others, one crosses the year's end, Easter follows the Gregorian
/// reckoning, the flags are set and cleared as the day turns, and the harness's day is touched by none.
/// </summary>
public class FestivalTests
{
    private static Festival F(string id) => Festivals.Get(id)!;
    private static bool On(string id, int y, int m, int d) => Festivals.Keeps(F(id), new DateTime(y, m, d));

    [Fact]
    public void EachRegionHasItsFestivalsAndEveryRegionTheGreatDays()
    {
        string[] sinnoh = { "lanterns", "midwinter", "blossom", "summer" };
        Assert.All(sinnoh, id => Assert.Equal(RegionDatabase.Sinnoh, F(id).Region));
        Assert.Equal(RegionDatabase.Kanto, F("harvest_moon").Region);
        Assert.Equal(RegionDatabase.Johto, F("bellfall").Region);
        Assert.Equal(RegionDatabase.Hoenn, F("tide_day").Region);
        Assert.Equal(RegionDatabase.Unova, F("bridgelight").Region);
        // The three great days and the quiet fourth belong to no one region: every region's Gift Runners keep them
        Assert.All(new[] { "new_year", "easter", "christmas", "valentine" }, id => Assert.Null(F(id).Region));
        // A town's festival has a stall of its own in marts.json
        Assert.All(Festivals.All.Where(f => f.Town != null), f =>
            Assert.True(MartDatabase.Specialties.ContainsKey(f.Stall!), $"{f.Id}: no stall {f.Stall}"));
    }

    [Fact]
    public void EachWindowKeepsItsDaysAndNoOthers()
    {
        Assert.False(On("lanterns", 2026, 10, 9));
        Assert.True(On("lanterns", 2026, 10, 10));
        Assert.True(On("lanterns", 2026, 10, 16));
        Assert.False(On("lanterns", 2026, 10, 17));
        Assert.True(On("tide_day", 2026, 6, 20));
        Assert.False(On("tide_day", 2026, 6, 21));
        Assert.True(On("valentine", 2027, 2, 14));
        Assert.False(On("valentine", 2027, 2, 15));
        // Across the year's end: Snowpoint's Midwinter Lights, 18 December to 2 January
        Assert.False(On("midwinter", 2026, 12, 17));
        Assert.True(On("midwinter", 2026, 12, 18));
        Assert.True(On("midwinter", 2026, 12, 31));
        Assert.True(On("midwinter", 2027, 1, 1));
        Assert.True(On("midwinter", 2027, 1, 2));
        Assert.False(On("midwinter", 2027, 1, 3));
        Assert.True(On("new_year", 2026, 12, 31) && On("new_year", 2027, 1, 1) && !On("new_year", 2027, 1, 2));
    }

    [Fact]
    public void EasterIsTheGregorianReckoningFromGoodFridayToEasterMonday()
    {
        Assert.Equal(new DateTime(2025, 4, 20), Festivals.EasterSunday(2025));
        Assert.Equal(new DateTime(2026, 4, 5), Festivals.EasterSunday(2026));
        Assert.Equal(new DateTime(2027, 3, 28), Festivals.EasterSunday(2027));
        Assert.Equal(new DateTime(2038, 4, 25), Festivals.EasterSunday(2038));
        Assert.False(On("easter", 2026, 4, 2));
        Assert.True(On("easter", 2026, 4, 3));
        Assert.True(On("easter", 2026, 4, 6));
        Assert.False(On("easter", 2026, 4, 7));
    }

    [Fact]
    public void TheFlagIsSetAndClearedAsTheDayTurns()
    {
        var story = new StoryState();
        Assert.True(Festivals.Keep(story, new DateTime(2026, 10, 12)));
        Assert.True(story.Has(Festivals.FlagOf("lanterns")));
        Assert.Equal("FLAG_FESTIVAL_LANTERNS", Festivals.FlagOf("lanterns"));
        // The same day again changes nothing
        Assert.False(Festivals.Keep(story, new DateTime(2026, 10, 12)));
        Assert.True(Festivals.Keep(story, new DateTime(2026, 10, 17)));
        Assert.False(story.Has(Festivals.FlagOf("lanterns")));
        Assert.True(Festivals.Keep(story, new DateTime(2026, 12, 25)));
        Assert.True(story.Has(Festivals.FlagOf("christmas")) && story.Has(Festivals.FlagOf("midwinter")));
    }

    [Fact]
    public void TheHarnesssDayIsTouchedByNone()
    {
        // The harness fixes 1 June (GameClock.FixedDate): no festival may keep it in any year, or every mode's shots move
        for (int year = 2020; year <= 2060; year++)
            Assert.Empty(Festivals.On(new DateTime(year, 6, 1)));
        // ...nor does any gift by date wait on it
        var box = new GiftBox();
        Assert.Empty(box.Due(new GiftFacts { Today = new DateTime(2026, 6, 1) }).Where(g => g.When.Dates != null || g.When.Festival != null));
    }
}
