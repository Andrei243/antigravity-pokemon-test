using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 21 · H9, part c: Scott's way to the Battle Frontier (a crossing, as Route 40's guide is Johto's) and the
/// rematches of the Elite Four, Wallace and Wally over the PokéNav's Match Call (docs/mechanics/rulings.md, "Scott's way
/// to the Battle Frontier and the League's rematches").
/// </summary>
public class HoennPartCTests
{
    private const string Harbor = "lilycove_city_harbor";
    private static readonly DateTime Monday = new(2026, 10, 19);

    private static readonly Lazy<Dictionary<string, Map>> Maps = new(() =>
        World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Hoenn).BuildMaps().ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase));

    private static void Run(string name, string place, HeadlessScriptHost host, string? map = null)
    {
        if (map != null) host.Map = Maps.Value[map];
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(name, place)!, new NPC { Name = "Guide" });
        runner.RunToEnd();
    }

    [Fact]
    public void ScottGivesThePassOnceAndSailsWithTheWholeTeamToTheBattleFrontier()
    {
        var story = new StoryState();
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Swampert")!, 60));
        var host = new HeadlessScriptHost(story, party, new Inventory()) { Region = RegionDatabase.Hoenn, PlayerTile = (12, 12) };
        // A no keeps the player on the quay, and the pass is given all the same, once
        host.Answers.Enqueue(1);
        Run("Scott", Harbor, host);
        Assert.True(story.Has("FLAG_RECEIVED_FRONTIER_PASS"));
        Assert.Empty(host.Crossings);
        Run("Scott", Harbor, host);
        Assert.Equal(new[] { RegionDatabase.Sinnoh }, host.Crossings);
        Assert.Contains(host.Log, l => l == $"travel to {RegionDatabase.Sinnoh} BattleFrontier 47 51");
        Assert.Equal((47, 51), host.PlayerTile);
        Assert.All(party.Members, p => Assert.Equal(RegionDatabase.Hoenn, p.Region));
        var records = host.Frontier;
        records.GivePoints(7);
        // And back from the Hoenn guide on the grounds to the harbour beside Scott
        Run("HoennGuide", "battle_frontier", host);
        Assert.Equal(new[] { RegionDatabase.Sinnoh, RegionDatabase.Hoenn }, host.Crossings);
        Assert.Equal(RegionDatabase.Hoenn, host.Region);
        Assert.Equal((12, 12), host.PlayerTile);
        Assert.Same(records, host.Frontier);
        Assert.Equal(7, host.Frontier.BattlePoints);
    }

    [Fact]
    public void BothEndsOfTheFerryAreThereOnlyForAChampionOfHoenn()
    {
        Assert.Equal(RegionDatabase.Hoenn, RegionDatabase.RegionOfMap("LilycoveCity_Harbor")!.Id);
        var scott = World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Hoenn).Overlay(Harbor)!.Npcs!.Single(n => n.Id == "lilycove_city_harbor.Scott");
        Assert.Equal("HoennHallOfFame", scott.ShownBy);
        var guide = World.LoadAll().Single(w => w.Index.Region == "Sinnoh").Overlay("battle_frontier")!.Npcs!.Single(n => n.Id == "battle_frontier.HoennGuide");
        Assert.Equal("HoennHallOfFame", guide.ShownBy);
        // ...and the guide stands off the tile the crossings arrive on and off the Johto guide's
        Assert.NotEqual((47, 51), (guide.X, guide.Y));
        Assert.NotEqual((46, 51), (guide.X, guide.Y));
    }

    [Theory]
    [InlineData("sidney", "hoenn_sidney")]
    [InlineData("phoebe", "hoenn_phoebe")]
    [InlineData("glacia", "hoenn_glacia")]
    [InlineData("drake", "hoenn_drake")]
    [InlineData("wallace", "hoenn_wallace")]
    [InlineData("hoenn_wally", "hoenn_wally_vr_2")]
    public void ARematchIsAskedForOnlyOfAChampionWhoHasBeatenAndRegisteredThem(string contactId, string trainer)
    {
        var contact = PhoneBook.Get(contactId)!;
        Assert.Equal("hoenn", contact.Region);
        Assert.Equal(trainer, contact.Trainer);
        var record = TrainerDatabase.Get(trainer)!;
        Assert.Single(record.Rematches!);
        var team = TrainerDatabase.Get(record.Rematches![0]!)!;
        // The stronger team has the same Pokémon, each at a higher level
        Assert.Equal(record.Party.Select(p => p.Species), team.Party.Select(p => p.Species));
        Assert.All(record.Party.Zip(team.Party), pair => Assert.True(pair.Second.Level > pair.First.Level));

        var story = new StoryState();
        string day = contact.RematchDay!;
        var date = Enumerable.Range(0, 7).Select(i => Monday.AddDays(i)).First(d => d.DayOfWeek.ToString() == day);
        var time = contact.RematchTime == "Morning" ? TimeOfDay.Morning : contact.RematchTime == "Day" ? TimeOfDay.Day : TimeOfDay.Night;
        Assert.False(PhoneRematch.Wants(contact, story, date, time));
        story.Defeat(trainer);
        Assert.False(PhoneRematch.Wants(contact, story, date, time));
        story.Set(Pokegear.ContactFlag(contact.Id));
        // A Champion of Hoenn only
        Assert.False(PhoneRematch.Wants(contact, story, date, time));
        story.Set(RegionDatabase.Get(RegionDatabase.Hoenn)!.StoryCompleteFlag);
        Assert.True(PhoneRematch.Wants(contact, story, date, time));
        Assert.Equal(Pokegear.CallRematch, PhoneRematch.Call(contact, story, date, time, incoming: false));
        Assert.True(story.Has(PhoneRematch.WaitingFlag(contact.Id)));
        Assert.Contains(trainer, PhoneRematch.Waiting(story));
        // The call's own line says so, and sets the flag the League's room reads
        story.SetVar(Pokegear.CallVar, Pokegear.CallRematch);
        story.Unset(PhoneRematch.WaitingFlag(contact.Id));
        var host = new HeadlessScriptHost(story, new Party(), new Inventory()) { Region = RegionDatabase.Hoenn };
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(contact.Script, GameEngine.PhoneScripts)!, null);
        runner.RunToEnd();
        Assert.True(story.Has(PhoneRematch.WaitingFlag(contact.Id)));
        PhoneRematch.Fought(story, trainer);
        Assert.False(story.Has(PhoneRematch.WaitingFlag(contact.Id)));
    }

    [Fact]
    public void AWaitingRematchIsFoughtInTheRoomWithTheStrongerTeam()
    {
        foreach (var (place, script, flag, team, map) in new[]
        {
            ("ever_grande_city_sidneys_room", "Sidney", "FLAG_PHONE_REMATCH_SIDNEY", "hoenn_sidney_rematch", "EverGrandeCity_SidneysRoom"),
            ("ever_grande_city_phoebes_room", "Phoebe", "FLAG_PHONE_REMATCH_PHOEBE", "hoenn_phoebe_rematch", "EverGrandeCity_PhoebesRoom"),
            ("ever_grande_city_glacias_room", "Glacia", "FLAG_PHONE_REMATCH_GLACIA", "hoenn_glacia_rematch", "EverGrandeCity_GlaciasRoom"),
            ("ever_grande_city_drakes_room", "Drake", "FLAG_PHONE_REMATCH_DRAKE", "hoenn_drake_rematch", "EverGrandeCity_DrakesRoom"),
            ("hoenn_victory_road_1f", "ExitWally", "FLAG_PHONE_REMATCH_HOENN_WALLY", "hoenn_wally_rematch", "HoennVictoryRoad_1F"),
        })
        {
            var story = new StoryState();
            story.Set(flag);
            var party = new Party();
            party.Add(new Pokemon(PokemonDatabase.Get("Swampert")!, 70));
            var host = new HeadlessScriptHost(story, party, new Inventory()) { Region = RegionDatabase.Hoenn, Fight = _ => BattleOutcome.Won };
            Run(script, place, host, map);
            Assert.Contains(host.Log, l => l.StartsWith($"battle {team}", StringComparison.Ordinal));
            Assert.False(story.Has(flag), place);
        }
    }

    [Fact]
    public void WallacesRematchIsFoughtInHisScene()
    {
        var story = new StoryState();
        story.Set("FLAG_PHONE_REMATCH_WALLACE");
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Swampert")!, 70));
        var host = new HeadlessScriptHost(story, party, new Inventory()) { Region = RegionDatabase.Hoenn, Fight = _ => BattleOutcome.Won };
        Run("OnEnter", "ever_grande_city_champions_room", host, "EverGrandeCity_ChampionsRoom");
        Assert.Contains(host.Log, l => l.StartsWith("battle hoenn_wallace_rematch", StringComparison.Ordinal));
        Assert.DoesNotContain(host.Log, l => l.StartsWith("battle hoenn_wallace ", StringComparison.Ordinal));
        Assert.False(story.Has("FLAG_PHONE_REMATCH_WALLACE"));
        Assert.True(story.Has("FLAG_HOENN_DEFEATED_WALLACE"));
    }
}
