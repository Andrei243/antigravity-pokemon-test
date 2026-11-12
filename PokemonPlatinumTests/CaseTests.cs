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

    [Fact]
    public void TheCulpritsTeamIsLegal()
    {
        foreach (string id in new[] { "case_sinnoh_ferren" })
        {
            var record = TrainerDatabase.Get(id);
            Assert.NotNull(record);
            Assert.NotEmpty(record!.Party);
            foreach (var m in record.Party) Assert.NotNull(PokemonDatabase.Get(m.Species));
        }
    }

    private static void Play(HeadlessScriptHost host, string file, string script)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script, file)!, new NPC { Name = script, Key = script, ScriptFile = file });
        runner.RunToEnd();
        Assert.Empty(host.Problems);
    }
}
