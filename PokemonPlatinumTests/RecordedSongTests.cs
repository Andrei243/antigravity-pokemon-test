using System;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// The recorded songs (2026-11-09): each region's credits song, generated with Lyria 3.5 and kept as Ogg Vorbis in
/// <c>Data/music/&lt;region&gt;/credits.ogg</c>, streamed by a modern player and timed against by the credits' roll.
/// </summary>
public class RecordedSongTests
{
    /// <summary>The regions whose credits have a song of their own on record.</summary>
    public static readonly string[] Regions = { "sinnoh", "kanto", "johto", "hoenn", "unova", "kalos", "alola" };

    [Fact]
    public void EveryBuiltRegionHasItsCreditsSongOnRecord()
    {
        foreach (var region in Regions)
        {
            Assert.True(MusicLibrary.HasRecording($"{region}/credits"), region);
            Assert.Equal($"{region}/credits", AudioManager.SceneSongOf(region, "credits"));
        }
    }

    [Fact]
    public void ARecordingIsAStereoSongOfAboutTwoMinutesAtTheMixRateAndNoneOfTheSongsOfNotes()
    {
        foreach (var id in MusicLibrary.RecordingIds)
        {
            var song = MusicLibrary.GetRecording(id);
            Assert.NotNull(song);
            Assert.True(song!.IsRecorded);
            Assert.Empty(song.Tracks);
            Assert.InRange(song.RecordedSeconds, 100, 170);
            using var reader = new NVorbis.VorbisReader(new System.IO.MemoryStream(song.Recording!), true);
            Assert.Equal(Synthesizer.ModernRate, reader.SampleRate);
            Assert.Equal(2, reader.Channels);
            // The checks of the music hold the songs of notes: a recording is never among them
            Assert.DoesNotContain(MusicLibrary.All, s => s.IsRecorded);
        }
    }

    [Fact]
    public void AModernPlayerStreamsARecordingWithinTheMusicBussWindowAndADsPlayerSoundsNothing()
    {
        var song = MusicLibrary.GetRecording("sinnoh/credits")!;
        var player = new SongPlayer(SynthSound.Modern);
        player.Start(song, atNight: false);
        Assert.True(player.Playing);
        // Thirty seconds from the first chorus on, a block at a time as the mixer asks
        var skip = new float[1024 * 2];
        for (int i = 0; i < 45 * Synthesizer.ModernRate / 1024; i++) player.Render(skip.AsSpan().ToArray(), 1f);
        var out30 = new float[30 * Synthesizer.ModernRate * 2];
        for (int at = 0; at < out30.Length; at += skip.Length) player.Render(out30.AsSpan(at, Math.Min(skip.Length, out30.Length - at)), 1f);
        Assert.InRange(player.Position, 74.9, 75.1);
        var (low, high) = Loudness.Target(AudioBus.Music);
        Assert.InRange(Loudness.Of(out30, 2, Synthesizer.ModernRate), low, high + 1);
        player.Stop();
        Assert.True(player.Silent);

        var ds = new SongPlayer();
        ds.Start(song, atNight: false);
        Assert.False(ds.Playing);
        var none = new float[2048];
        ds.Render(none, 1f);
        Assert.All(none, v => Assert.Equal(0f, v));
    }

    [Fact]
    public void ARecordingPlaysToItsEndAndStops()
    {
        var song = MusicLibrary.GetRecording("kanto/credits")!;
        var player = new SongPlayer(SynthSound.Modern);
        player.Start(song, atNight: false);
        var block = new float[48000 * 2];
        int seconds = 0;
        while (player.Playing && seconds < 200) { player.Render(block, 1f); seconds++; }
        Assert.False(player.Playing);
        Assert.InRange(player.Position, song.RecordedSeconds - 0.1, song.RecordedSeconds + 0.1);
    }

    [Fact]
    public void TheRollIsTimedSoItsLastCardComesInJustBeforeTheSongEnds()
    {
        Assert.Equal(CreditsScreen.Pace, CreditsScreen.PaceFor(4000f, null));
        // A song of 120 s rolls for 120 - 5 - 4 = 111 s
        Assert.Equal(4000f / 111f, CreditsScreen.PaceFor(4000f, 120), 3);
        Assert.Equal(CreditsScreen.Pace / 2f, CreditsScreen.PaceFor(100f, 120));
        Assert.Equal(CreditsScreen.Pace * 3f, CreditsScreen.PaceFor(100000f, 120));
        Assert.Equal(CreditsScreen.Pace * 3f, CreditsScreen.PaceFor(4000f, 2));
    }
}
