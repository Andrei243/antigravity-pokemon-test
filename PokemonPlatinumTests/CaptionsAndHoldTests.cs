using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 12 · Q9, sound and holding: the options' CAPTIONS and HOLD TO CONFIRM, which the whole process holds
/// (<see cref="Accessibility.Captions"/>, <see cref="ConfirmHold.Asked"/>), so the class runs with the other shared
/// state and puts both back, and the language with them.
/// </summary>
[Collection("Shared clocks and dice")]
public class CaptionsAndHoldTests : IDisposable
{
    public void Dispose()
    {
        Accessibility.Captions = false;
        Accessibility.CaptionsShown.Clear();
        ConfirmHold.Asked = false;
        Text.Use("en");
    }

    private static Pokemon Mon(string species, int level) => new(PokemonDatabase.Get(species)!, level, Gender.Male, Nature.Hardy, false);

    [Fact]
    public void BothRowsAreOffUnlessChosenAndStandAfterTheRunTimerBeforeControls()
    {
        var settings = new GameSettings();
        Assert.False(settings.Captions);
        Assert.False(settings.HoldToConfirm);
        var json = new JsonSerializerOptions { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };
        var old = JsonSerializer.Deserialize<GameSettings>("{ \"Quality\": \"Medium\" }", json)!;
        Assert.Equal((false, false), (old.Captions, old.HoldToConfirm));

        var rows = Enum.GetValues<OptionRow>();
        int captions = Array.IndexOf(rows, OptionRow.Captions);
        Assert.Equal(Array.IndexOf(rows, OptionRow.RunTimer) + 1, captions);
        Assert.Equal(captions + 1, Array.IndexOf(rows, OptionRow.HoldToConfirm));
        // (plan 12 · Q26's BOX TOOLS went in after them, before Controls)
        Assert.Equal(captions + 2, Array.IndexOf(rows, OptionRow.BoxTools));
        // (and plan 12 · Q12's AUTO-BATTLE after it)
        Assert.Equal(captions + 3, Array.IndexOf(rows, OptionRow.AutoBattle));
        Assert.Equal(captions + 4, Array.IndexOf(rows, OptionRow.Controls));

        foreach (var row in new[] { OptionRow.Captions, OptionRow.HoldToConfirm })
        {
            string before = OptionsScreen.Describe(settings, row).Help;
            OptionsScreen.Change(settings, row, 1);
            string after = OptionsScreen.Describe(settings, row).Help;
            Assert.NotEqual(before, after);
            Assert.DoesNotContain("[", after);
            OptionsScreen.Change(settings, row, 1);
        }
        Assert.Equal((false, false), (settings.Captions, settings.HoldToConfirm));
    }

    [Fact]
    public void EverySoundOfTheBankHasACaptionInEnglishAndRomanian()
    {
        foreach (var tag in new[] { "en", "ro" })
        {
            Text.Use(tag);
            foreach (var entry in SoundBank.Entries)
            {
                string caption = entry.Caption;
                Assert.False(string.IsNullOrWhiteSpace(caption), entry.Name);
                Assert.NotEqual(entry.Name, caption);
                Assert.DoesNotContain("[", caption);
                Assert.DoesNotContain("{", caption);
                Assert.True(caption.Length <= 48, $"{entry.Name}: \"{caption}\" is too long for its pill");
            }
            // And a cry of every species
            Assert.Equal(tag == "en" ? "Starly cries" : "Starly strigă", SoundBank.CryCaption(PokemonDatabase.Get("Starly")!));
        }
        // Romanian says every sound of its own in its own words
        Text.Use("ro");
        foreach (var entry in SoundBank.Entries.Where(e => Text.Has($"captions.sound.{SoundBank.CaptionKey(e.Name)}")))
            Assert.True(Text.HasOwn($"captions.sound.{SoundBank.CaptionKey(entry.Name)}"), entry.Name);
        Text.Use("en");
        // A caption of the table that names no sound of the bank is stale
        var names = SoundBank.Names.Select(SoundBank.CaptionKey).ToHashSet();
        foreach (var key in Text.EnglishKeys.Where(k => k.StartsWith("captions.sound.", StringComparison.Ordinal)))
            Assert.Contains(key["captions.sound.".Length..], names);
    }

    [Fact]
    public void TheFamiliesAreNamedByTheirMoveOrType()
    {
        Assert.Equal("Tackle is used", SoundBank.CaptionOf("use_tackle"));
        Assert.Equal("A Fire move sets off", SoundBank.CaptionOf(SoundBank.MoveSound(PokemonType.Fire)));
        Assert.Equal("A Water Z-Move strikes", SoundBank.CaptionOf(SoundBank.ZMoveSound(PokemonType.Water)));
        Assert.Equal("A Ghost Max Move strikes", SoundBank.CaptionOf(SoundBank.MaxMoveSound(PokemonType.Ghost)));
        Assert.Equal("G-Max Wildfire strikes", SoundBank.CaptionOf(SoundBank.GMaxSound("G-Max Wildfire")));
        Assert.Equal("A door opens", SoundBank.CaptionOf("door_open"));
    }

    [Fact]
    public void AsoundOrACryPlayedIsCaptionedOnlyWithTheRowOn()
    {
        Accessibility.CaptionsShown.Clear();
        AudioManager.PlaySound("door_open");
        AudioManager.PlayCry(PokemonDatabase.Get("Starly")!);
        Assert.Empty(Accessibility.CaptionsShown.Lines);

        Accessibility.Captions = true;
        AudioManager.PlaySound("door_open");
        AudioManager.PlayCry(PokemonDatabase.Get("Starly")!);
        Assert.Equal(new[] { ("A door opens", CaptionKind.Sound), ("Starly cries", CaptionKind.Cry) },
            Accessibility.CaptionsShown.Lines.Select(l => (l.Text, l.Kind)));

        // A sound dropped unheard, as a skipped scene's are, is named nowhere
        AudioManager.Hushed = true;
        try { AudioManager.PlaySound("ball_throw"); }
        finally { AudioManager.Hushed = false; }
        Assert.Equal(2, Accessibility.CaptionsShown.Lines.Count);
    }

    [Fact]
    public void TheQueueCountsARepeatHoldsThreeAndLetsEachGoAfterItsTime()
    {
        var queue = new CaptionQueue();
        queue.Push("A ball wobbles", CaptionKind.Sound);
        queue.Advance(1f);
        queue.Push("A ball wobbles", CaptionKind.Sound);
        queue.Push("A ball wobbles", CaptionKind.Sound);
        var one = Assert.Single(queue.Lines);
        Assert.Equal(3, one.Times);
        // Renewed, it stands at full strength and its time starts again from there
        Assert.Equal(1f, CaptionQueue.Strength(one.Age));

        queue.Push("A", CaptionKind.Sound);
        queue.Push("B", CaptionKind.Sound);
        queue.Push("C", CaptionKind.Cry);
        Assert.Equal(new[] { "A", "B", "C" }, queue.Lines.Select(l => l.Text));

        Assert.Equal(0f, CaptionQueue.Strength(0f));
        queue.Advance(CaptionQueue.Seconds - 0.01f);
        Assert.Equal(3, queue.Lines.Count);
        Assert.InRange(CaptionQueue.Strength(queue.Lines[0].Age), 0f, 0.1f);
        queue.Advance(0.02f);
        Assert.Empty(queue.Lines);
    }

    [Fact]
    public void AHoldIsAPressWithTheRowOffAndPointFourSecondsWithItOn()
    {
        var hold = new ConfirmHold();
        Assert.True(hold.Press());
        Assert.False(hold.Holding);

        ConfirmHold.Asked = true;
        Assert.False(hold.Press());
        Assert.True(hold.Holding);
        Assert.False(hold.Advance(0.2f, true));
        Assert.InRange(hold.Fill, 0.49f, 0.51f);
        // Let go early: nothing, and the ring is gone
        Assert.False(hold.Advance(0.1f, false));
        Assert.False(hold.Holding);
        Assert.Equal(0f, hold.Fill);

        Assert.False(hold.Press());
        Assert.False(hold.Advance(0.39f, true));
        Assert.True(hold.Advance(0.02f, true));
        Assert.False(hold.Holding);
    }

    [Fact]
    public void ResettingTheOptionsWaitsForTheHold()
    {
        ConfirmHold.Asked = true;
        var settings = new GameSettings { Captions = true };
        var options = new OptionsScreen();
        options.Open();
        options.AskReset();
        // NO is a press as ever
        Assert.False(options.ConfirmReset(settings));
        Assert.False(options.ResetAsking);

        options.AskReset();
        options.ToggleReset();
        Assert.True(options.ResetYes);
        Assert.False(options.ConfirmReset(settings));
        Assert.True(options.ResetAsking);
        Assert.True(settings.Captions);
        Assert.False(options.HoldReset(settings, 0.3f, true));
        Assert.True(settings.Captions);
        // Let go: the question stands and nothing is reset
        Assert.False(options.HoldReset(settings, 0.05f, false));
        Assert.True(options.ResetAsking);

        Assert.False(options.ConfirmReset(settings));
        Assert.False(options.HoldReset(settings, 0.3f, true));
        Assert.True(options.HoldReset(settings, 0.15f, true));
        Assert.False(settings.Captions);
        Assert.False(options.ResetAsking);
    }

    [Fact]
    public void DeletingASaveWaitsForTheHold()
    {
        ConfirmHold.Asked = true;
        var title = new TitleScreen(new SaveData { PlayerName = "Lucas" });
        title.PressConfirm();
        title.PressConfirm();
        while (title.Entries[title.SelectedIndex] != TitleChoice.Delete) title.Move(1);
        title.PressConfirm();
        Assert.Equal(TitlePhase.ConfirmDelete, title.Phase);
        title.Move(1);
        Assert.True(title.ConfirmYes);

        title.PressConfirm();
        Assert.Equal(TitlePhase.ConfirmDelete, title.Phase);
        Assert.Null(title.TakeDeletion());
        title.HoldDelete(0.2f, true);
        title.HoldDelete(0.1f, false);
        Assert.Null(title.TakeDeletion());
        Assert.Equal(TitlePhase.ConfirmDelete, title.Phase);

        title.PressConfirm();
        title.HoldDelete(0.25f, true);
        Assert.Null(title.TakeDeletion());
        title.HoldDelete(0.2f, true);
        Assert.Equal(1, title.TakeDeletion());
        Assert.Equal(TitlePhase.Menu, title.Phase);
    }

    [Fact]
    public void ReleasingAPokemonWaitsForTheHold()
    {
        ConfirmHold.Asked = true;
        var turtwig = Mon("Turtwig", 5);
        var starly = Mon("Starly", 4);
        var party = new Party();
        party.Add(turtwig);
        party.Add(starly);
        var pc = new PcBoxes();
        var notes = new List<string>();
        var screen = new PCScreen();
        screen.Open(pc);
        screen.Confirm(party, pc, notes.Add);
        screen.Choose(PcAction.Release, party, pc, notes.Add);
        Assert.True(screen.AskingRelease);

        screen.Confirm(party, pc, notes.Add);
        Assert.True(screen.AskingRelease);
        Assert.Equal(2, party.Count);
        screen.HoldRelease(party, pc, notes.Add, 0.3f, true);
        screen.HoldRelease(party, pc, notes.Add, 0.05f, false);
        Assert.True(screen.AskingRelease);
        Assert.Equal(2, party.Count);

        screen.Confirm(party, pc, notes.Add);
        screen.HoldRelease(party, pc, notes.Add, 0.45f, true);
        Assert.False(screen.AskingRelease);
        Assert.Equal(new[] { starly }, party.Members);
    }
}
