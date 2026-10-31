using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Mirage Island (plan 21 · H12, part h; docs/mechanics/rulings.md, "Mirage Island"): Emerald's IsMirageIslandPresent
/// read from the day's number and the team's personalities, the island's layout swapped into Route 130 by the flag the
/// routes round it set, its berry tree and grass, and the old man of Pacifidlog's fifth house who watches for it.
/// </summary>
public class HoennMirageIslandTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;
    private static readonly Lazy<World> Hoenn = new(() => World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Hoenn));
    private static readonly Lazy<Map> Overworld = new(() => Hoenn.Value.BuildMaps().Single(m => m.Name == "Hoenn"));

    // The island's four chunks of Route 130, and its berry tree's tile (Emerald's 52, 9 of Route 130)
    private static readonly (int X, int Y)[] Cells = { (18, 8), (19, 8), (18, 9), (19, 9) };
    private const int TreeX = 612, TreeY = 269;

    private static SpecialEncounters Day(uint number) => new() { DailyNumber = number };

    private static Pokemon Mon(string species, uint personality)
    {
        var p = new Pokemon(PokemonDatabase.Get(species)!, 30, new Random(1)) { Personality = personality };
        return p;
    }

    [Fact]
    public void TheIslandStandsWhenAPersonalitysLowHalfIsTheDaysNumber()
    {
        var day = Day(0xBEEF_1234);
        Assert.Equal(0xBEEF, MirageIsland.NumberOf(day));
        var party = new Party();
        party.Add(Mon("Swampert", 0x0000_1234));
        Assert.False(MirageIsland.Present(party, day));
        // The low half of the personality is what is compared, not the high
        party.Add(Mon("Wailord", 0xBEEF_0000));
        Assert.False(MirageIsland.Present(party, day));
        party.Add(Mon("Wingull", 0x1234_BEEF));
        Assert.True(MirageIsland.Present(party, day));
        // The next day's number is another
        day.DaysPass(1);
        Assert.False(MirageIsland.Present(party, day));
        // An Egg counts, as Emerald reads every slot that holds a species
        var egg = Mon("Wynaut", (uint)MirageIsland.NumberOf(day));
        egg.IsEgg = true;
        party.Add(egg);
        Assert.True(MirageIsland.Present(party, day));
    }

    [Fact]
    public void Route130sSwapLaysTheIslandAndTakesItAway()
    {
        var map = Overworld.Value;
        var place = map.SwappedPlaces.Single(p => p.Flag == MirageIsland.Flag);
        Assert.Equal(Cells.OrderBy(c => c).ToList(), place.Cells.OrderBy(c => c).ToList());
        try
        {
            Assert.False(map.IsWalkable(TreeX, TreeY + 1));
            int landBefore = Land(map);
            map.ApplySwappedPlaces(_ => 0, f => f == MirageIsland.Flag);
            int landAfter = Land(map);
            Assert.True(landAfter > landBefore + 40, $"{landBefore} tiles of land before, {landAfter} after");
            // ...with tall grass for its Wynaut
            Assert.Contains(Tiles(), t => map.IsTallGrass(t.X, t.Y));
            Assert.Contains(map.AreaAt(TreeX, TreeY)!.WildEncounters, e => e.SpeciesName == "Wynaut");
            map.ApplySwappedPlaces(_ => 0, _ => false);
            Assert.Equal(landBefore, Land(map));
        }
        finally { map.ApplySwappedPlaces(_ => 0, _ => false); }
    }

    private static IEnumerable<(int X, int Y)> Tiles() =>
        Cells.SelectMany(c => Enumerable.Range(0, 32).SelectMany(dy => Enumerable.Range(0, 32).Select(dx => (c.X * 32 + dx, c.Y * 32 + dy))));

    private static int Land(Map map) => Tiles().Count(t => map.IsWalkable(t.X, t.Y));

    [Theory]
    [InlineData("route_129")]
    [InlineData("route_130")]
    [InlineData("route_131")]
    [InlineData("pacifidlog_town")]
    public void ComingToThePlacesRoundItDecidesTheIsland(string place)
    {
        var script = Scripts.In(place, ScriptLibrary.OnEnter);
        Assert.NotNull(script);
        var day = Day(0x4242_0000);

        var story = new StoryState();
        var party = new Party();
        party.Add(Mon("Swampert", 0x0000_4242));
        Play(script!, story, party, day);
        Assert.True(story.Has(MirageIsland.Flag));
        Assert.False(story.Has(MirageIsland.BerryTreeFlag));

        // Another day: the island sinks and its tree is hidden
        day.DaysPass(1);
        Play(script!, story, party, day);
        Assert.False(story.Has(MirageIsland.Flag));
        Assert.True(story.Has(MirageIsland.BerryTreeFlag));
    }

    [Fact]
    public void TheBerryTreeStandsOnlyWithTheIsland()
    {
        var near = Overworld.Value.Everyone.Where(n => Math.Abs(n.GridX - TreeX) <= 2 && Math.Abs(n.GridY - TreeY) <= 2).ToList();
        var tree = Assert.Single(near, n => n.IsBerrySoil);
        Assert.Equal((TreeX, TreeY), (tree.GridX, tree.GridY));
        Assert.Equal(MirageIsland.BerryTreeFlag, tree.HiddenBy);
    }

    [Fact]
    public void TheOldManAtTheWindowSaysWhetherItIsThere()
    {
        var script = Scripts.Find("Watcher", "pacifidlog_town_house_5");
        Assert.NotNull(script);
        var day = Day(0x0007_0000);
        var party = new Party();
        party.Add(Mon("Swampert", 1));
        var not = Play(script!, new StoryState(), party, day);
        Assert.Contains(not.Transcript, l => l.Text.Contains("not today"));
        party.Add(Mon("Wailmer", 7));
        var seen = Play(script!, new StoryState(), party, day);
        Assert.Contains(seen.Transcript, l => l.Text.Contains("risen"));
    }

    private static HeadlessScriptHost Play(Script script, StoryState story, Party party, SpecialEncounters day)
    {
        var host = new HeadlessScriptHost(story, party, new Inventory()) { Encounters = day };
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(script);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }
}
