using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 41 · EG7, EG8: the Open Door Agency's cases. A clue found takes its step, a deduction offers only the clues found
/// and answers right and wrong, a case is never lost, the requests page knows it, and Sinnoh's case is played headless from
/// Looker's desk to its close.
/// </summary>
public class CaseTests
{
    [Fact]
    public void EveryCaseIsARequestWhoseFlagsTheScriptsSet()
    {
        foreach (var @case in Cases.All)
        {
            var request = Requests.Find(@case.RequestId);
            Assert.NotNull(request);
            Assert.Equal(@case.Clues.Count + 3, request!.Steps.Count);
            // every deduction's answer is one of its clues
            foreach (var d in @case.Deductions) Assert.Contains(@case.Clues, c => c.Key == d.Answer);
            // and every clue's words, and each deduction's question, are in the table
            foreach (var c in @case.Clues) Assert.False(string.IsNullOrEmpty(Text.Of($"cases.{@case.Id}.clue.{c.Key}")));
        }
    }

    [Fact]
    public void ADeductionOffersOnlyTheCluesFoundAndAnswersRightAndWrong()
    {
        var @case = Cases.Find("sinnoh")!;
        var story = new StoryState();
        Assert.Empty(Cases.Answers(@case, story));
        story.Set(@case.Clues[0].Flag);
        story.Set(@case.Clues[1].Flag);
        var offered = Cases.Answers(@case, story);
        Assert.Equal(2, offered.Count);
        int right = offered.ToList().FindIndex(c => c.Key == @case.Deductions[0].Answer);
        Assert.True(Cases.Answers(@case, 1, story, right));
        Assert.False(Cases.Answers(@case, 1, story, 1 - right));
        // "let me think", past the clues, is never right
        Assert.False(Cases.Answers(@case, 1, story, offered.Count));
    }

    [Fact]
    public void AClueFoundTakesItsStepAndTheRequestIsPinnedOnTheNext()
    {
        var @case = Cases.Find("sinnoh")!;
        var request = Requests.Find(@case.RequestId)!;
        var story = new StoryState();
        Assert.Equal(RequestState.Unknown, Requests.State(request, story));
        story.Set(@case.TakenFlag);
        Assert.Equal(RequestState.Open, Requests.State(request, story));
        Assert.Equal("clue_label", Requests.Next(request, story)!.Name);
        story.Set(@case.Clues[0].Flag);
        Assert.Equal("clue_bite", Requests.Next(request, story)!.Name);
        story.Set(@case.ClosedFlag);
        Assert.Equal(RequestState.Done, Requests.State(request, story));
    }

    [Fact]
    public void SinnohsCaseIsPlayedFromLookersDeskToItsCloseAndAWrongAnswerLosesNothing()
    {
        var host = new HeadlessScriptHost();
        host.Party.Add(new Pokemon(PokemonDatabase.Get("Garchomp")!, 70));
        host.Story.Set(RegionDatabase.Get(RegionDatabase.Sinnoh)!.StoryCompleteFlag);
        var @case = Cases.Find("sinnoh")!;

        Play(host, "Hub_Office", "Looker");
        Assert.True(host.Story.Has("FLAG_AGENCY_MET"));
        Assert.True(host.Story.Has(@case.TakenFlag));

        // The four clues where they lie
        foreach (string person in new[] { "VillaCaretaker", "GibleGirl" }) Play(host, "resort_area", person);
        foreach (string person in new[] { "HarbourClerk", "Deckhand" }) Play(host, "fight_area", person);
        Assert.All(@case.Clues, c => Assert.True(host.Story.Has(c.Flag), c.Key));

        // A wrong deduction costs a line and nothing more: Looker asks again next time
        host.Answers.Enqueue(0); // the crate label: wrong
        Play(host, "Hub_Office", "Looker");
        Assert.Equal(0, host.Story.Var(@case.DeducedVariable));
        // The right ones: the bite marks (the second clue), then the log (the third)
        host.Answers.Enqueue(1);
        host.Answers.Enqueue(2);
        Play(host, "Hub_Office", "Looker");
        Assert.Equal(2, host.Story.Var(@case.DeducedVariable));
        Assert.True(host.Story.Has("FLAG_CASE_SINNOH_FERREN_WAITS"));

        // The culprit faced on the pier, then the case closed at the desk
        Play(host, "fight_area", "Ferren");
        Assert.True(host.Story.Has(@case.CulpritFlag));
        Assert.Contains(host.Log, l => l.Contains("case_sinnoh_ferren"));
        Play(host, "Hub_Office", "Looker");
        Assert.True(host.Story.Has(@case.ClosedFlag));
        Assert.Equal(RequestState.Done, Requests.State(Requests.Find(@case.RequestId)!, host.Story));
    }

    /// <summary>
    /// EG8's cases, each played headless from Looker's desk to its close with only its own region's Hall of Fame entered:
    /// the clue people (file, script) in the case's order, the two right answers' places among the clues offered, a wrong
    /// one, and the culprit's script.
    /// </summary>
    public static TheoryData<string, string, string[], int, int, int, string, string> EveryOtherRegionsCase => new()
    {
        { "kanto", RegionDatabase.Kanto, new[] { "vermilion_city:CaseKantoClerk", "two_island:CaseKantoKeeper", "vermilion_city:CaseKantoFisherman" }, 0, 2, 1, "vermilion_city:CaseKantoDov", "FLAG_CASE_KANTO_DOV_WAITS" },
        { "johto", RegionDatabase.Johto, new[] { "ruins_of_alph:CaseJohtoResearcher", "ruins_of_alph:CaseJohtoCamper", "ruins_of_alph:CaseJohtoPupil" }, 0, 2, 1, "ruins_of_alph:CaseJohtoMirelle", "FLAG_CASE_JOHTO_MIRELLE_WAITS" },
        { "hoenn", RegionDatabase.Hoenn, new[] { "dewford_town:CaseHoennCustoms", "dewford_town:CaseHoennSailor", "dewford_town:CaseHoennFisherman" }, 0, 1, 2, "dewford_town:CaseHoennOttoline", "FLAG_CASE_HOENN_OTTOLINE_WAITS" },
        { "unova", RegionDatabase.Unova, new[] { "pwt:CaseUnovaOrganiser", "pwt:CaseUnovaRival", "pwt:CaseUnovaJanitor" }, 1, 2, 0, "pwt:CaseUnovaIsidore", "FLAG_CASE_UNOVA_ISIDORE_WAITS" },
        { "kalos", RegionDatabase.Kalos, new[] { "lumiose_city:CaseKalosGuard", "kiloude_city:CaseKalosPorter", "kiloude_city:CaseKalosPainter" }, 0, 1, 2, "kiloude_city:CaseKalosOdile", "FLAG_CASE_KALOS_ODILE_WAITS" },
    };

    [Theory]
    [MemberData(nameof(EveryOtherRegionsCase))]
    public void EachOtherRegionsCaseIsPlayedFromLookersDeskToItsClose(string id, string region, string[] clues, int first, int second, int wrong, string culprit, string waits)
    {
        var host = new HeadlessScriptHost();
        host.Party.Add(new Pokemon(PokemonDatabase.Get("Garchomp")!, 80));
        host.Story.Set(RegionDatabase.Get(region)!.StoryCompleteFlag);
        var @case = Cases.Find(id)!;

        // Clue people say only their own line before the case is taken
        Play(host, clues[0]);
        Assert.False(host.Story.Has(@case.Clues[0].Flag));

        Play(host, "Hub_Office:Looker");
        Assert.True(host.Story.Has(@case.TakenFlag));
        Assert.False(host.Story.Has(Cases.All[0].TakenFlag)); // Sinnoh's case is never offered without Sinnoh's Hall of Fame

        foreach (string clue in clues) Play(host, clue);
        Assert.All(@case.Clues, c => Assert.True(host.Story.Has(c.Flag), c.Key));

        host.Answers.Enqueue(wrong);
        Play(host, "Hub_Office:Looker");
        Assert.Equal(0, host.Story.Var(@case.DeducedVariable));
        host.Answers.Enqueue(first);
        host.Answers.Enqueue(second);
        Play(host, "Hub_Office:Looker");
        Assert.Equal(2, host.Story.Var(@case.DeducedVariable));
        Assert.True(host.Story.Has(waits));

        Play(host, culprit);
        Assert.True(host.Story.Has(@case.CulpritFlag));
        Assert.False(host.Story.Has(waits));
        Assert.Contains(host.Log, l => l.Contains($"case_{id}_"));
        Play(host, "Hub_Office:Looker");
        Assert.True(host.Story.Has(@case.ClosedFlag));
        Assert.Equal(RequestState.Done, Requests.State(Requests.Find(@case.RequestId)!, host.Story));
    }

    /// <summary>Where EG8's case people stand: on open ground of their map, with open ground beside them to speak from.</summary>
    [Theory]
    [InlineData("Kanto", "case_kanto_clerk", 290, 229)]
    [InlineData("Kanto", "case_kanto_fisherman", 299, 229)]
    [InlineData("Kanto", "case_kanto_dov", 293, 229)]
    [InlineData("TwoIsland", "case_kanto_keeper", 13, 46)]
    [InlineData("Johto", "case_johto_researcher", 432, 268)]
    [InlineData("Johto", "case_johto_camper", 436, 280)]
    [InlineData("Johto", "case_johto_pupil", 429, 291)]
    [InlineData("Johto", "case_johto_mirelle", 433, 291)]
    [InlineData("Hoenn", "case_hoenn_customs", 69, 373)]
    [InlineData("Hoenn", "case_hoenn_sailor", 76, 373)]
    [InlineData("Hoenn", "case_hoenn_fisherman", 63, 375)]
    [InlineData("Hoenn", "case_hoenn_ottoline", 73, 374)]
    [InlineData("Unova", "case_unova_organiser", 205, 458)]
    [InlineData("Unova", "case_unova_rival", 215, 459)]
    [InlineData("Unova", "case_unova_janitor", 213, 455)]
    [InlineData("Unova", "case_unova_isidore", 200, 467)]
    [InlineData("Kalos", "case_kalos_guard", 368, 318)]
    [InlineData("Kalos", "case_kalos_porter", 548, 351)]
    [InlineData("Kalos", "case_kalos_painter", 583, 352)]
    [InlineData("Kalos", "case_kalos_odile", 560, 354)]
    public void ACasePersonStandsOnOpenGround(string mapName, string key, int x, int y)
    {
        var map = MapDatabase.Get(mapName);
        Assert.Contains(map.Everyone, n => n.Key == key && n.GridX == x && n.GridY == y);
        Assert.False(map.IsSolid(x, y));
        Assert.Contains(new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) }, t => map.IsWalkable(t.Item1, t.Item2));
    }

    [Fact]
    public void TheCulpritsTeamIsLegal()
    {
        foreach (string id in new[] { "case_sinnoh_ferren", "case_kanto_dov", "case_johto_mirelle", "case_hoenn_ottoline", "case_unova_isidore", "case_kalos_odile" })
        {
            var record = TrainerDatabase.Get(id);
            Assert.NotNull(record);
            Assert.NotEmpty(record!.Party);
            foreach (var m in record.Party) Assert.NotNull(PokemonDatabase.Get(m.Species));
        }
    }

    private static void Play(HeadlessScriptHost host, string fileAndScript)
    {
        int colon = fileAndScript.IndexOf(':');
        Play(host, fileAndScript[..colon], fileAndScript[(colon + 1)..]);
    }

    private static void Play(HeadlessScriptHost host, string file, string script)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script, file)!, new NPC { Name = script, Key = script, ScriptFile = file });
        runner.RunToEnd();
        Assert.Empty(host.Problems);
    }
}
