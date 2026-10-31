using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>Plan 12 · Q15: the jukebox lists everything the game plays, unlocks a song once it has been heard, and offers an arrangement only where the song has it.</summary>
[Collection("Shared clocks and dice")]
public class JukeboxTests
{
    private static readonly IReadOnlySet<string> Nothing = new HashSet<string>();

    private static JukeboxScreen Opened(IEnumerable<string>? heard = null)
    {
        var screen = new JukeboxScreen();
        screen.Open(heard ?? Array.Empty<string>(), PokemonDatabase.GetAll().OrderBy(s => s.DexNumber).Take(5).ToList());
        return screen;
    }

    [Fact]
    public void EverySongSoundCryModeAndBedIsListed()
    {
        Assert.Equal(MusicLibrary.Ids.OrderBy(i => i, StringComparer.Ordinal), JukeboxScreen.Songs(Nothing).Select(e => e.Id));
        Assert.Equal(SoundBank.Names, JukeboxScreen.Sounds().Select(e => e.Id));
        Assert.Equal(Enum.GetNames<CryMode>(), JukeboxScreen.Cries().Select(e => e.Id));
        Assert.Equal(Enum.GetNames<AmbienceBed>(), JukeboxScreen.Beds().Select(e => e.Id));
        Assert.All(JukeboxScreen.Sounds().Concat(JukeboxScreen.Cries()).Concat(JukeboxScreen.Beds()), e => Assert.True(e.Unlocked));
    }

    [Fact]
    public void EveryLabelIsWordsAndEveryCryAndBedHasItsText()
    {
        foreach (var e in JukeboxScreen.Cries())
        {
            Assert.DoesNotContain("jukebox.", JukeboxScreen.LabelOf(JukeboxTab.Cries, e));
            Assert.DoesNotContain("jukebox.", Text.Of($"jukebox.cryNote.{e.Id}"));
        }
        foreach (var e in JukeboxScreen.Beds()) Assert.DoesNotContain("jukebox.", JukeboxScreen.LabelOf(JukeboxTab.Beds, e));
        foreach (var a in Enum.GetValues<JukeboxArrangement>()) Assert.DoesNotContain("jukebox.", JukeboxScreen.ArrangementName(a));
        foreach (var g in Enum.GetValues<SoundGroup>()) Assert.DoesNotContain("jukebox.", Text.Of($"jukebox.group.{g}"));
    }

    [Fact]
    public void ASongPlayedIsHeardAndOnlyAHeardSongIsUnlocked()
    {
        string id = MusicLibrary.Ids.OrderBy(i => i, StringComparer.Ordinal).First();
        AudioManager.SetHeard(null);
        Assert.DoesNotContain(id, AudioManager.Heard);
        AudioManager.PlayMusic(id);
        Assert.Contains(id, AudioManager.Heard);

        var songs = JukeboxScreen.Songs(new HashSet<string>(AudioManager.Heard));
        Assert.True(songs.Single(e => e.Id == id).Unlocked);
        Assert.Equal(1, songs.Count(e => e.Unlocked));
        AudioManager.SetHeard(null);
    }

    [Fact]
    public void AFanfareAndASongIdThatIsNoSongAreHandledToo()
    {
        AudioManager.SetHeard(new[] { "no/such/song", MusicLibrary.Ids.First() });
        Assert.Single(AudioManager.Heard);
        AudioManager.SetHeard(null);
    }

    [Fact]
    public void ALockedSongSaysNoAndAnUnlockedOneIsPlayed()
    {
        string id = JukeboxScreen.Songs(Nothing)[0].Id;
        var locked = Opened();
        var heard = AudioManager.Listen();
        locked.Confirm();
        AudioManager.StopListening();
        Assert.DoesNotContain(heard, h => h.StartsWith("music "));
        Assert.Contains("error", heard);
        Assert.Equal("???", JukeboxScreen.LabelOf(JukeboxTab.Songs, locked.Selected!));

        var open = Opened(new[] { id });
        heard = AudioManager.Listen();
        open.Confirm();
        Assert.Equal(id, open.PlayingId);
        open.Confirm(); // played again: stopped
        AudioManager.StopListening();
        Assert.Contains($"music {id}", heard);
        Assert.Null(open.PlayingId);
    }

    [Fact]
    public void NightAndLowHpAreOfferedOnlyWhereTheSongHasThem()
    {
        var all = MusicLibrary.All.Select(s => s.Id).ToHashSet();
        foreach (var entry in JukeboxScreen.Songs(all))
        {
            var song = MusicLibrary.Get(entry.Id)!;
            Assert.Equal(song.HasNight, entry.Arrangements.Contains(JukeboxArrangement.Night));
            Assert.Equal(song.HasLowHp, entry.Arrangements.Contains(JukeboxArrangement.LowHp));
            Assert.Equal(song.HasGiant, entry.Arrangements.Contains(JukeboxArrangement.Giant));
            Assert.Equal(JukeboxArrangement.Day, entry.Arrangements[0]);
        }
        // Somewhere there is a song with each, and one with neither (or the offer would prove nothing)
        var offered = JukeboxScreen.Songs(all);
        Assert.Contains(offered, e => e.Arrangements.Contains(JukeboxArrangement.LowHp));
        Assert.Contains(offered, e => e.Arrangements.Contains(JukeboxArrangement.Giant));
        Assert.Contains(offered, e => e.Arrangements.Contains(JukeboxArrangement.Night));
        Assert.Contains(offered, e => e.Arrangements.Count == 1);

        // A song not heard yet offers the day alone: it doesn't tell what it has
        Assert.All(JukeboxScreen.Songs(Nothing), e => Assert.Single(e.Arrangements));
    }

    [Fact]
    public void TheShoulderKeysChooseTheArrangementAndItIsPlayed()
    {
        var all = MusicLibrary.All.Select(s => s.Id).ToHashSet();
        var withLowHp = JukeboxScreen.Songs(all).First(e => e.Arrangements.Contains(JukeboxArrangement.LowHp));
        var screen = Opened(all);
        while (screen.Selected!.Id != withLowHp.Id) screen.Move(1);
        Assert.Equal(JukeboxArrangement.Day, screen.Arrangement);
        screen.ChangeVariant(-1); // wraps to the last
        Assert.Equal(withLowHp.Arrangements[^1], screen.Arrangement);

        var heard = AudioManager.Listen();
        screen.Confirm();
        AudioManager.StopListening();
        string want = $"music {withLowHp.Id}{(screen.Arrangement == JukeboxArrangement.Night ? " night" : "")}{(screen.Arrangement == JukeboxArrangement.LowHp ? " lowhp" : "")}{(screen.Arrangement == JukeboxArrangement.Giant ? " giant" : "")}";
        Assert.Contains(want, heard);

        // Moving to another song starts again from its day
        screen.Move(1);
        Assert.Equal(JukeboxArrangement.Day, screen.Arrangement);
    }

    [Fact]
    public void EachTabPlaysItsOwnKind()
    {
        var screen = Opened();
        var heard = AudioManager.Listen();

        screen.ChangeTab(1);
        Assert.Equal(JukeboxTab.Sounds, screen.Tab);
        screen.Confirm();
        Assert.Contains(SoundBank.Names[0], heard);

        screen.ChangeTab(1);
        screen.Move(1); // the Half cry
        screen.ChangeVariant(1);
        var species = screen.CryOf!;
        screen.Confirm();
        Assert.Contains($"cry {species.Name} Half", heard);

        screen.ChangeTab(1);
        screen.Confirm();
        Assert.Contains($"bed {AmbienceBed.Rain}", heard);
        Assert.Equal(nameof(AmbienceBed.Rain), screen.PlayingId);
        screen.Confirm(); // again: the bed is stopped
        Assert.Null(screen.PlayingId);
        AudioManager.StopListening();

        screen.ChangeTab(1);
        Assert.Equal(JukeboxTab.Songs, screen.Tab);
    }

    [Fact]
    public void ACursorKeepsItsPlacePerTabAndWraps()
    {
        var screen = Opened();
        screen.Move(-1);
        Assert.Equal(screen.Entries.Count - 1, screen.Cursor);
        screen.ChangeTab(1);
        Assert.Equal(0, screen.Cursor);
        screen.ChangeTab(-1);
        Assert.Equal(screen.Entries.Count - 1, screen.Cursor);
        Assert.InRange(screen.First, 0, screen.Entries.Count);
    }

    [Fact]
    public void ASaveKeepsTheSongsHeardAndAnOldOneHasNone()
    {
        var save = new SaveData { Heard = new List<string> { "common/a", "sinnoh/b" } };
        var back = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(save))!;
        Assert.Equal(save.Heard, back.Heard);
        Assert.Empty(JsonSerializer.Deserialize<SaveData>("{}")!.Heard);
    }

    [Fact]
    public void TheOptionsHaveAJukeboxRowBeforeHelpThatAsksForTheScreenOnce()
    {
        var rows = Enum.GetValues<OptionRow>();
        Assert.Equal(OptionRow.ResetDefaults, rows[^1]);
        Assert.Equal(OptionRow.Help, rows[^2]);
        Assert.Equal(OptionRow.Packs, rows[^3]);
        Assert.Equal(OptionRow.Jukebox, rows[^4]);

        var options = new OptionsScreen();
        options.Open();
        while (Enum.GetValues<OptionRow>()[options.SelectedIndex] != OptionRow.Jukebox) options.Move(1);
        Assert.False(options.TakeJukeboxRequest());
        options.Confirm(new GameSettings());
        Assert.True(options.TakeJukeboxRequest());
        Assert.False(options.TakeJukeboxRequest());
        Assert.True(options.IsActive);
    }

    [Fact]
    public void TheTitleOffersTheJukeboxBeforeOptionsAndAsksForItAtOnce()
    {
        var title = new TitleScreen(null);
        Assert.Equal(new[] { TitleChoice.NewGame, TitleChoice.Jukebox, TitleChoice.Options, TitleChoice.Credits, TitleChoice.Quit }, title.Entries);
        title.PressConfirm();
        title.PressConfirm();
        Assert.Equal(TitlePhase.Menu, title.Phase);
        title.Move(1);
        title.PressConfirm();
        Assert.Equal(TitleChoice.Jukebox, title.TakeChoice());
        Assert.Equal(TitleChoice.None, title.TakeChoice());
        Assert.Equal(TitlePhase.Menu, title.Phase);
    }
}
