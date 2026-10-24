using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumTests;

/// <summary>Plan 09 · L3: the text box's markup, the player's coloured name, pauses, the backlog.</summary>
public class TextBoxMarkupTests
{
    [Fact]
    public void EveryCodeParsesAndStripsToPlainText()
    {
        var line = RichText.Parse("A {red:red} and {blue:blue} {name:Name}.{pause 0.4} {speed 2}Fast{br}row {money 1500}{sfx select}!");
        Assert.Equal("A red and blue Name. Fast\nrow P1,500!", line.Plain);
        Assert.Equal("A red and blue Name. Fast\nrow P1,500!", RichText.Plain("A {red:red} and {blue:blue} {name:Name}.{pause 0.4} {speed 2}Fast{br}row {money 1500}{sfx select}!"));
        Assert.Contains(line.Events, e => e.Kind == TextEventKind.Pause && e.Amount == 0.4f);
        Assert.Contains(line.Events, e => e.Kind == TextEventKind.Speed && e.Amount == 2f);
        Assert.Contains(line.Events, e => e.Kind == TextEventKind.Sound && e.Sound == "select");
        Assert.Equal(RunColour.Red, line.ColourAt(2));
        Assert.Equal(RunColour.Plain, line.ColourAt(1));
        Assert.Empty(RichText.Problems("A {red:red} {pause 0.4}{br}{money 3}{sfx select}"));
    }

    [Fact]
    public void WhatIsNoMarkupStaysAsWritten()
    {
        // Placeholders some other part fills, including the money one without a number
        Assert.Equal("You got {money} and {var:COUNT}.", RichText.Plain("You got {money} and {var:COUNT}."));
        Assert.Empty(RichText.Problems("You got {money} and {lead}."));
    }

    [Fact]
    public void BadCodesAreReported()
    {
        Assert.NotEmpty(RichText.Problems("{pause}"));
        Assert.NotEmpty(RichText.Problems("{pause soon}"));
        Assert.NotEmpty(RichText.Problems("{speed 0}"));
        Assert.NotEmpty(RichText.Problems("{sfx}"));
        Assert.NotEmpty(RichText.Problems("{money many}"));
        Assert.NotEmpty(RichText.Problems("{red:}"));
        Assert.NotEmpty(RichText.Problems("never {closed"));
    }

    [Fact]
    public void ThePlayersNameIsBlueForABoyAndRedForAGirl()
    {
        var boy = DialogueManager.Prepare("Hello, {player}!", "", "Lucas", PlayerLook.Boy);
        Assert.Equal("Hello, Lucas!", boy.Plain);
        Assert.Equal(new RunSpan(7, 12, RunColour.Blue), Assert.Single(boy.Spans));
        var girl = DialogueManager.Prepare("Hello, {player}!", "", "Dawn", PlayerLook.Girl);
        Assert.Equal(new RunSpan(7, 11, RunColour.Red), Assert.Single(girl.Spans));
        // The rival and the assistant stay plain
        Assert.Empty(DialogueManager.Prepare("Hi, {rival}!", "", "Lucas", PlayerLook.Boy).Spans);
    }

    [Fact]
    public void ASpeakersPrefixIsDroppedWithTheCodesKeptInStep()
    {
        var line = DialogueManager.Prepare("Barry: {red:Hey}!", "Barry", "Lucas", PlayerLook.Boy);
        Assert.Equal("Hey!", line.Plain);
        Assert.Equal(new RunSpan(0, 3, RunColour.Red), Assert.Single(line.Spans));
    }

    [Fact]
    public void ALinesTypingTimeCountsItsPausesAndSpeeds()
    {
        var plain = RichText.Parse("0123456789");
        var paused = RichText.Parse("01234{pause 0.5}56789");
        var fast = RichText.Parse("01234{speed 2}56789");
        Assert.Equal(1.0f, plain.TypingSeconds(10f), 3);
        Assert.Equal(1.5f, paused.TypingSeconds(10f), 3);
        Assert.Equal(0.75f, fast.TypingSeconds(10f), 3);
    }

    [Fact]
    public void TheBoxHoldsAtThePauseAndGoesOnAfterIt()
    {
        var box = new DialogueManager { CharactersPerSecond = 10f };
        box.ShowDialogue("", "01234{pause 0.5}56789");
        box.Type(0.5f);
        Assert.Equal("01234", box.VisibleText);
        box.Type(0.4f);
        Assert.Equal("01234", box.VisibleText);
        box.Type(0.25f); // the pause ends 0.1 s in, one and a half characters after
        Assert.Equal("012345", box.VisibleText);
        box.Type(1f);
        Assert.True(box.IsCurrentLineComplete);
        Assert.Equal("0123456789", box.CurrentLine);
    }

    [Fact]
    public void WithNoCodesTheTypingIsWhatItWas()
    {
        var box = new DialogueManager { CharactersPerSecond = 30f };
        box.ShowDialogue("", "Hello there, traveller.");
        box.Type(0.1f);
        Assert.Equal("Hel", box.VisibleText);
        box.Type(0.1f);
        Assert.Equal("Hello ", box.VisibleText);
    }

    [Fact]
    public void FinishingALineSkipsItsPauses()
    {
        var box = new DialogueManager { CharactersPerSecond = 10f };
        box.ShowDialogue("", "ab{pause 5}cd");
        box.FinishLine();
        Assert.True(box.IsCurrentLineComplete);
        Assert.Equal("abcd", box.VisibleText);
    }

    [Fact]
    public void ThePiecesOfARowFollowTheRuns()
    {
        var line = RichText.Parse("Go, {blue:Lucas} now");
        var rows = TextMetrics.Wrap(line.Plain, 10000f, 40f, UiWeight.ExtraBold);
        var pieces = RichText.Pieces(line.Plain, line.Spans, rows);
        var row = Assert.Single(pieces);
        Assert.Equal(new[] { ("Go, ", RunColour.Plain), ("Lucas", RunColour.Blue), (" now", RunColour.Plain) }, row);
    }

    [Fact]
    public void ThePiecesSurviveAWrapThatDropsASpace()
    {
        var line = RichText.Parse("one two {red:three} four");
        var rows = new List<string> { "one two", "three four" };
        var pieces = RichText.Pieces(line.Plain, line.Spans, rows);
        Assert.Equal(2, pieces.Count);
        Assert.Equal(("three", RunColour.Red), pieces[1][0]);
        Assert.Equal((" four", RunColour.Plain), pieces[1][1]);
    }

    [Fact]
    public void TheBacklogKeepsFiftyLinesAndTheOldestGoFirst()
    {
        var log = new DialogueLog();
        for (int i = 0; i < 60; i++) log.Add("Barry", $"line {i}");
        Assert.Equal(DialogueLog.Capacity, log.Count);
        Assert.Equal("line 10", log.Entries[0].Text);
        Assert.Equal("line 59", log.Entries[^1].Text);
        log.Add("", "  ");
        Assert.Equal(DialogueLog.Capacity, log.Count);
    }

    [Fact]
    public void TheBoxKeepsEachLineItWritesInTheBacklogWithoutItsCodes()
    {
        var box = new DialogueManager();
        box.ShowDialogue("Barry", new[] { "First {red:line}.", "Second{br}line." });
        box.Advance();
        // Not complete: a line must be whole before it goes on
        box.FinishLine();
        box.Advance();
        Assert.Equal(new[] { "First line.", "Second\nline." }, box.Log.Entries.Select(e => e.Text));
        Assert.All(box.Log.Entries, e => Assert.Equal("Barry", e.Speaker));
    }

    [Fact]
    public void TheBacklogOpensOnlyOverAWholePlainLine()
    {
        var box = new DialogueManager();
        box.ShowDialogue("", new[] { "One.", "Two." });
        box.FinishLine();
        box.Advance();
        box.FinishLine();
        box.OpenBacklog();
        Assert.True(box.BacklogOpen);
        box.CloseBacklog();

        var ask = new DialogueManager();
        ask.ShowQuestion("", "Well?");
        ask.FinishLine();
        ask.OpenBacklog();
        Assert.False(ask.BacklogOpen);
    }

    [Fact]
    public void EveryLineTheGameWritesHasSoundMarkup()
    {
        var problems = new List<string>();
        foreach (var script in PokemonPlatinumEngine.Story.ScriptLibrary.Default.All)
            foreach (var i in script.Everything())
                foreach (string line in i.Lines)
                    problems.AddRange(RichText.Problems(line).Select(p => $"{script.File}.txt({i.Line}): {p}"));
        Assert.Empty(problems);
    }
}
