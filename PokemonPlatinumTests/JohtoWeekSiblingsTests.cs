using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 20 · J13: the week's seven siblings of Johto, each on their own weekday in their own place, each with a gift
/// given once and counted in VAR_NUM_MET_WEEKDAY_SIBLINGS, and once all seven are met the ribbon of the day, one a
/// day between them (HeartGold's FLAG_DAILY_GOT_SHOCK_RIBBON).
/// </summary>
public class JohtoWeekSiblingsTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;
    private static readonly Lazy<World> Johto = new(() => World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Johto));
    private static readonly Lazy<List<Map>> Maps = new(() => Johto.Value.BuildMaps().ToList());

    // Who, where, the day, the gift, the flag, and the story that has to be there before they come
    public static readonly TheoryData<string, string, string, DayOfWeek, string, string> Siblings = new()
    {
        { "w40_gswoman1", "route_40", "Monica", DayOfWeek.Monday, "Sharp Beak", "FLAG_GOT_SHARP_BEAK_FROM_MONICA" },
        { "r29_gswoman2_2", "route_29", "Tuscany", DayOfWeek.Tuesday, "Twisted Spoon", "FLAG_GOT_TWISTEDSPOON_FROM_TUSCANY" },
        { "t29_gsman1_2", "lake_of_rage", "Wesley", DayOfWeek.Wednesday, "Black Belt", "FLAG_GOT_BLACK_BELT_FROM_WESLEY" },
        { "r36_gsboy1_2", "route_36", "Arthur", DayOfWeek.Thursday, "Hard Stone", "FLAG_GOT_HARD_STONE_FROM_ARTHUR" },
        { "r32_gsgirl1", "route_32", "Frieda", DayOfWeek.Friday, "Poison Barb", "FLAG_GOT_POISON_BARB_FROM_FRIEDA" },
        { "t30_gsboy2", "blackthorn", "Santos", DayOfWeek.Saturday, "Soft Sand", "FLAG_GOT_SOFT_SAND_FROM_SANTOS" },
        { "r37_gsbabyboy1", "route_37", "Sunny", DayOfWeek.Sunday, "Magnet", "FLAG_GOT_MAGNET_FROM_SUNNY" },
    };

    private static Map MapOf(string key) => Maps.Value.First(m => m.Everyone.Any(n => n.Key == key));

    private static StoryState Ready()
    {
        var story = new StoryState();
        story.GiveBadge(RegionDatabase.Johto, "Zephyr");
        story.Set("FLAG_GOT_RED_SCALE");
        return story;
    }

    private static HeadlessScriptHost Play(Map map, Script script, StoryState story, DayOfWeek day, Party? party = null, NPC? subject = null)
    {
        var host = new HeadlessScriptHost(story, party ?? new Party(), new Inventory()) { Map = map, Weekday = day, Region = RegionDatabase.Johto };
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(script, subject);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        map.ApplyPresence(story.Has);
        return host;
    }

    private static bool There(string key, string area, DayOfWeek day, StoryState story)
    {
        var map = MapOf(key);
        Play(map, Scripts.In(area, ScriptLibrary.OnEnter)!, story, day);
        return map.NPCs.Any(n => n.Key == key);
    }

    [Theory]
    [MemberData(nameof(Siblings))]
    public void EachSiblingIsThereOnTheirDayAloneAndGivesTheirGiftOnce(string key, string area, string name, DayOfWeek day, string gift, string flag)
    {
        var story = Ready();
        foreach (var d in Enum.GetValues<DayOfWeek>())
            Assert.Equal(d == day, There(key, area, d, story));

        Assert.True(There(key, area, day, story));
        var map = MapOf(key);
        var sibling = map.NPCs.First(n => n.Key == key);
        Assert.Equal(name, sibling.Name);
        var script = Scripts.Find(FieldScripts.For(sibling)!, area)!;
        var first = Play(map, script, story, day, subject: sibling);
        Assert.Contains(first.Transcript, l => l.Text.Contains(gift, StringComparison.Ordinal));
        Assert.True(story.Has(flag));
        Assert.Equal(1, story.Var("VAR_NUM_MET_WEEKDAY_SIBLINGS"));
        var again = Play(map, script, story, day, subject: sibling);
        Assert.DoesNotContain(again.Transcript, l => l.Text.Contains(gift, StringComparison.Ordinal));
        Assert.Equal(1, story.Var("VAR_NUM_MET_WEEKDAY_SIBLINGS"));
    }

    [Fact]
    public void TuscanyWaitsForTheZephyrBadgeAndWesleyForTheRedScale()
    {
        var story = new StoryState();
        Assert.False(There("r29_gswoman2_2", "route_29", DayOfWeek.Tuesday, story));
        Assert.False(There("t29_gsman1_2", "lake_of_rage", DayOfWeek.Wednesday, story));
        story.GiveBadge(RegionDatabase.Johto, "Zephyr");
        Assert.True(There("r29_gswoman2_2", "route_29", DayOfWeek.Tuesday, story));
        story.Set("FLAG_GOT_RED_SCALE");
        Assert.True(There("t29_gsman1_2", "lake_of_rage", DayOfWeek.Wednesday, story));
    }

    [Fact]
    public void OnceAllSevenAreMetTheSiblingOfTheDayGivesTheDaysRibbonOneADay()
    {
        var story = Ready();
        story.SetVar("VAR_NUM_MET_WEEKDAY_SIBLINGS", 7);
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Togepi")!, 20));
        party.Add(new Pokemon(PokemonDatabase.Get("Pidgey")!, 20));

        // Monday's Monica puts the Alert Ribbon on the first of the team
        var map = MapOf("w40_gswoman1");
        Assert.True(There("w40_gswoman1", "route_40", DayOfWeek.Monday, story));
        var monica = map.NPCs.First(n => n.Key == "w40_gswoman1");
        var given = Play(map, Scripts.Find("Monica", "route_40")!, story, DayOfWeek.Monday, party, monica);
        Assert.Contains(given.Transcript, l => l.Text.Contains("Ribbon", StringComparison.Ordinal));
        Assert.Contains("RIBBON_ALERT", party.Members[0].Ribbons);
        Assert.True(story.Has("FLAG_DAILY_GOT_SHOCK_RIBBON"));

        // Spoken to again, it already has it; a second Pokémon at the head gets none the same day
        var again = Play(map, Scripts.Find("Monica", "route_40")!, story, DayOfWeek.Monday, party, monica);
        Assert.Contains(again.Transcript, l => l.Text.Contains("already wearing", StringComparison.Ordinal));
        party.Swap(0, 1);
        var sameDay = Play(map, Scripts.Find("Monica", "route_40")!, story, DayOfWeek.Monday, party, monica);
        Assert.Contains(sameDay.Transcript, l => l.Text.Contains("One ribbon a day", StringComparison.Ordinal));
        Assert.DoesNotContain("RIBBON_ALERT", party.Members[0].Ribbons);

        // A new day clears the daily flag, and the next sibling's day brings the next ribbon
        story.Unset("FLAG_DAILY_GOT_SHOCK_RIBBON");
        var thursday = MapOf("r36_gsboy1_2");
        Assert.True(There("r36_gsboy1_2", "route_36", DayOfWeek.Thursday, story));
        var arthur = thursday.NPCs.First(n => n.Key == "r36_gsboy1_2");
        Play(thursday, Scripts.Find("Arthur", "route_36")!, story, DayOfWeek.Thursday, party, arthur);
        Assert.Contains("RIBBON_CARELESS", party.Members[0].Ribbons);
    }
}
