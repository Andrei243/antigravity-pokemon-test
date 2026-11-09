using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 31 · MU2: the modern sound in the game. A region's file or a song's header chooses the sound, the mixer plays a
/// modern song on its own player at 48 kHz beside the resampled DS stage, hands over between the two sounds as it does
/// between two songs, switches the arrangements in the middle of a modern song and takes it through the handheld's
/// speakers and mono; the modern song's compressor keeps it in the music bus's window. Only Kalos's file names a sound
/// (since MU3), so the songs here are a test region's.
/// </summary>
public class ModernMixTests
{
    /// <summary>A region of the tests alone, whose file names the modern sound.</summary>
    private static readonly Dictionary<string, SynthSound> TestRegions = MusicLibrary.RegionSounds(new[]
    {
        new RegionFile { Id = "Testland", Folder = "testland", Sound = "modern" },
        new RegionFile { Id = "Oldland", Folder = "oldland", Sound = "ds" },
        new RegionFile { Id = "Plainland", Folder = "plainland" },
    });

    private static string TuneText()
    {
        // MU0's island route with its own `sound` line taken out, so only the region decides
        var lines = File.ReadAllLines(Path.Combine(Repository.Root, "tools", "MusicRender", "songs", "mu0_island_route.mml"));
        return string.Join("\n", lines.Where(l => !l.TrimStart().StartsWith("sound ")));
    }

    /// <summary>The test region's route: a song of the modern sound because its region says so.</summary>
    internal static Song ModernRoute(string id = "testland/island_route") =>
        Mml.Parse(id, TuneText(), MusicLibrary.SoundOfFolder(id, TestRegions));

    private const string Battle =
        "title Test battle\ntempo 150\nlowhptempo 1.1\ngianttempo 1.0\n" +
        "track lead trumpet lowhp=sawlead vol=0.6\n  L [ o5 c8 e8 g8 > c8 < g8 e8 c4 | ]8\n" +
        "track bass bass vol=0.7\n  L [ o3 c4 c4 g4 g4 | ]8\n" +
        "track pad strings unless=lowhp vol=0.4\n  L o4 [ (c e g)1 | ]8\n" +
        "track alarm square only=lowhp vol=0.3\n  L [ o6 c16 r16 c16 r16 r4 r2 | ]8\n" +
        "track giant choir only=giant vol=0.4\n  L o4 [ (e g)1 | ]8\n" +
        "track drums kit vol=0.5\n  L [ b8 h8 s8 h8 b8 b8 s8 h8 | ]8\n";

    /// <summary>A battle theme of the test region, with a low-HP arrangement and a giant layer.</summary>
    internal static Song ModernBattle() => Mml.Parse("testland/battle_wild", Battle, MusicLibrary.SoundOfFolder("testland/battle_wild", TestRegions));

    private static float[] Render(AudioMixer mixer, double seconds) => MixRateTests.Render(mixer, seconds);

    [Fact]
    public void TheSongsHeaderWinsOverItsRegionAndEverythingElseIsTheDsSound()
    {
        Assert.Equal(SynthSound.Modern, ModernRoute().PlaysIn);
        Assert.Null(ModernRoute().Sound);
        Assert.Equal(SynthSound.Modern, ModernRoute().RegionSound);

        // The header's `sound ds` keeps a song of a modern region in the DS sound, and `sound modern` brings one of
        // another region (or of common, a legendary's theme) into the modern one
        Assert.Equal(SynthSound.Ds, Mml.Parse("testland/old", "sound ds\n" + TuneText(), MusicLibrary.SoundOfFolder("testland/old", TestRegions)).PlaysIn);
        Assert.Equal(SynthSound.Modern, Mml.Parse("common/legendary/x", "sound modern\n" + TuneText(), MusicLibrary.SoundOfFolder("common/legendary/x", TestRegions)).PlaysIn);

        // A region that says ds or nothing, and common, are the DS sound
        Assert.Equal(SynthSound.Ds, Mml.Parse("oldland/a", TuneText(), MusicLibrary.SoundOfFolder("oldland/a", TestRegions)).PlaysIn);
        Assert.Equal(SynthSound.Ds, Mml.Parse("plainland/a", TuneText(), MusicLibrary.SoundOfFolder("plainland/a", TestRegions)).PlaysIn);
        Assert.Equal(SynthSound.Ds, Mml.Parse("common/a", TuneText(), MusicLibrary.SoundOfFolder("common/a", TestRegions)).PlaysIn);
        Assert.False(TestRegions.ContainsKey("plainland"));

        // A region's folder is its id when its file names none, and a misspelt sound is refused
        Assert.Equal(SynthSound.Modern, MusicLibrary.RegionSounds(new[] { new RegionFile { Id = "Elsewhere", Sound = "Modern" } })["elsewhere"]);
        Assert.Throws<FormatException>(() => MusicLibrary.RegionSounds(new[] { new RegionFile { Id = "Bad", Sound = "moden" } }));
    }

    [Fact]
    public void KalosAloneNamesASoundSoEveryOtherSongPlaysAsItDid()
    {
        // Kalos's file says "sound": "modern" since MU3, and so its folder's songs play in the modern sound; every other
        // region names none, and every song outside music/kalos/ is the DS sound as before the plan
        foreach (var id in new[] { "Sinnoh", "Kanto", "Johto", "Hoenn", "Unova" })
            Assert.Null(RegionFiles.Load(id)?.MusicSound);
        Assert.Equal(SynthSound.Modern, RegionFiles.Load("Kalos")?.MusicSound);
        Assert.Equal(SynthSound.Modern, RegionFiles.Load("Alola")?.MusicSound);
        Assert.Equal(SynthSound.Modern, RegionFiles.Load("Galar")?.MusicSound);
        // The later legendaries' themes of common/legendary name the modern sound in their own headers (MU7)
        Assert.All(MusicLibrary.All, s => Assert.Equal(s.Id.StartsWith("kalos/") || s.Id.StartsWith("alola/") || s.Id.StartsWith("galar/") || ModernMusicTests.IsLaterLegendary(s.Id)
            ? SynthSound.Modern : SynthSound.Ds, s.PlaysIn));
        Assert.Contains(MusicLibrary.All, s => s.Id.StartsWith("kalos/"));
    }

    [Fact]
    public void AModernSongPlaysThroughTheGamesMixerInTheMusicWindowAsItDoesAlone()
    {
        var song = ModernRoute();
        var mixer = new AudioMixer();
        mixer.Play(song, night: false);
        var mixed = Render(mixer, 12);
        Assert.Equal(SynthSound.Modern, mixer.MusicSound);
        Assert.Equal(song.Id, mixer.CurrentId);
        Assert.All(mixed, s => Assert.True(float.IsFinite(s)));
        Assert.True(mixed.Max(MathF.Abs) < 1f, "clips");

        var (low, high) = Loudness.Target(AudioBus.Music);
        double loud = Loudness.Of(mixed, 2, AudioMixer.Rate);
        Assert.True(loud >= low && loud <= high, $"measures {loud:0.0} dB, outside {low} to {high}");

        // The same song rendered with no mixer (the same last stage, at the same rate) measures the same: the mixer adds
        // nothing to a modern song and takes nothing from it
        double alone = Loudness.Of(OfflineMaster.Render(song, SynthSound.Modern, 12), 2, AudioMixer.Rate);
        Assert.True(Math.Abs(loud - alone) < 0.2, $"through the mixer {loud:0.00} dB, alone {alone:0.00} dB");
    }

    [Fact]
    public void AMixerAtTheDsRateHasNoModernPlayerAndPlaysAModernSongInTheDsSound()
    {
        var mixer = new AudioMixer(Synthesizer.DsRate);
        mixer.Play(ModernRoute(), night: false);
        var mixed = Render(mixer, 2);
        Assert.Equal(SynthSound.Ds, mixer.MusicSound);
        Assert.True(mixed.Max(MathF.Abs) > 0.01f);
    }

    /// <summary>The largest step between two samples within <paramref name="reach"/> frames after <paramref name="from"/>.</summary>
    private static float LargestStep(float[] stereo, int from, int reach)
    {
        float largest = 0f;
        for (int i = Math.Max(1, from); i < Math.Min(stereo.Length / 2, from + reach); i++)
            for (int ch = 0; ch < 2; ch++) largest = MathF.Max(largest, MathF.Abs(stereo[i * 2 + ch] - stereo[(i - 1) * 2 + ch]));
        return largest;
    }

    private static double Loud(float[] stereo, double from, double to) =>
        Loudness.Of(stereo.AsSpan((int)(from * AudioMixer.Rate) * 2, (int)((to - from) * AudioMixer.Rate) * 2), 2, AudioMixer.Rate);

    public static IEnumerable<object[]> Handovers() => new[]
    {
        new object[] { "ds", "modern", false },
        new object[] { "modern", "ds", false },
        new object[] { "modern", "modern", false },
        new object[] { "ds", "modern", true },
        new object[] { "modern", "ds", true },
    };

    [Theory]
    [MemberData(nameof(Handovers))]
    public void ASongOfOneSoundHandsOverToOneOfTheOtherAsTwoSongsOfOneSoundDo(string first, string second, bool cut)
    {
        Song Of(string sound, string role) => (sound, role) switch
        {
            ("ds", "field") => MusicLibrary.Get("sinnoh/route201")!,
            ("ds", _) => MusicLibrary.Get("common/battle_wild")!,
            (_, "field") => ModernRoute(),
            _ => ModernBattle(),
        };
        var a = Of(first, "field");
        var b = Of(second, "battle");
        var mixer = new AudioMixer();
        mixer.Play(a, night: false);
        var before = Render(mixer, 4);
        Assert.Equal(a.PlaysIn, mixer.MusicSound);
        mixer.Play(b, night: false, immediate: cut);
        Assert.Equal(b.Id, mixer.CurrentId);
        var after = Render(mixer, 4);
        Assert.Equal(b.PlaysIn, mixer.MusicSound);

        var all = before.Concat(after).ToArray();
        Assert.All(all, s => Assert.True(float.IsFinite(s)));
        Assert.True(all.Max(MathF.Abs) < 1f, "clips");
        int at = before.Length / 2;
        if (!cut)
        {
            // The first song fades out over its 0.45 s before the second begins: quieter in its last tenth than before it
            Assert.True(Loud(all, 4.3, 4.42) < Loud(all, 3.0, 4.0) - 6, "the first song didn't fade");
            // And a change of sound clicks no more than the songs themselves step: nothing near the handover is larger a
            // step than the largest of either song's own
            float own = MathF.Max(LargestStep(all, AudioMixer.Rate, at - AudioMixer.Rate), LargestStep(all, at + AudioMixer.Rate, AudioMixer.Rate * 3));
            float handover = LargestStep(all, at, AudioMixer.Rate);
            Assert.True(handover <= own * 1.5f + 0.02f, $"handover {handover:0.000}, the songs' own {own:0.000}");
        }
        // The second song is heard, in its window
        var (low, high) = Loudness.Target(AudioBus.Music);
        double heard = Loud(all, 5.5, 8);
        Assert.True(heard >= low - 2 && heard <= high + 1, $"the second song measures {heard:0.0} dB");
    }

    [Fact]
    public void TheLowHpArrangementAndTheGiantLayerSwitchInTheMiddleOfAModernSong()
    {
        var song = ModernBattle();
        Assert.True(song.HasLowHp && song.HasGiant);
        float[] Run(bool lowHpAt2, bool giantAt2)
        {
            var mixer = new AudioMixer();
            mixer.Play(song, night: false, immediate: true);
            var a = Render(mixer, 2);
            mixer.LowHp = lowHpAt2;
            mixer.Giant = giantAt2;
            var b = Render(mixer, 3);
            Assert.Equal(lowHpAt2, mixer.LowHp);
            mixer.LowHp = false;
            mixer.Giant = false;
            return [.. a, .. b];
        }
        var calm = Run(false, false);
        var red = Run(true, false);
        var giant = Run(false, true);
        int split = 2 * AudioMixer.Rate * 2;

        // Before the switch the song is the same to the sample (nothing random, no clock); after it the arrangement plays
        Assert.True(calm.AsSpan(0, split).SequenceEqual(red.AsSpan(0, split)));
        Assert.True(calm.AsSpan(0, split).SequenceEqual(giant.AsSpan(0, split)));
        Assert.False(calm.AsSpan(split).SequenceEqual(red.AsSpan(split)));
        Assert.False(calm.AsSpan(split).SequenceEqual(giant.AsSpan(split)));

        // The song keeps its place: no restart, and no click at the switch
        foreach (var run in new[] { red, giant })
        {
            float own = LargestStep(run, 0, 2 * AudioMixer.Rate);
            Assert.True(LargestStep(run, 2 * AudioMixer.Rate - 200, AudioMixer.Rate / 2) <= own * 1.5f + 0.02f);
        }
    }

    [Fact]
    public void TheNightArrangementOfAModernSongComesInAfterAFadeAsADsSongsDoes()
    {
        var song = ModernRoute();
        var mixer = new AudioMixer();
        mixer.Play(song, night: false);
        Render(mixer, 2);
        // The same song by day carries on; asked for at night it fades and starts again in the night arrangement
        mixer.Play(song, night: false);
        Assert.Equal(song.Id, mixer.CurrentId);
        mixer.Play(song, night: true);
        var fading = Render(mixer, 0.3);
        Assert.True(fading.Max(MathF.Abs) > 0f);
        var night = Render(mixer, 4);
        Assert.Equal(SynthSound.Modern, mixer.MusicSound);
        Assert.True(Loudness.Of(night, 2, AudioMixer.Rate) > -40, "the night arrangement is heard");
    }

    [Fact]
    public void AFanfareOfTheModernSoundPausesTheDsMusicAndLetsItBack()
    {
        var fanfare = Mml.Parse("testland/fanfare_item", "title Item\ntempo 120\ntrack a trumpet vol=0.6\n  o5 c8 e8 g8 > c8 < g2 |\n",
            MusicLibrary.SoundOfFolder("testland/fanfare_item", TestRegions));
        Assert.Equal(SynthSound.Modern, fanfare.PlaysIn);
        var mixer = new AudioMixer();
        mixer.Play(MusicLibrary.Get("sinnoh/route201")!, night: false);
        Render(mixer, 2);
        mixer.PlayFanfare(fanfare);
        Assert.True(mixer.FanfarePlaying);
        var jingle = Render(mixer, 1);
        Assert.True(Loudness.Of(jingle, 2, AudioMixer.Rate) > -30, "the fanfare is heard");
        Render(mixer, 3);
        Assert.False(mixer.FanfarePlaying);
        Assert.Equal(SynthSound.Ds, mixer.MusicSound);
        Assert.True(Loudness.Of(Render(mixer, 2), 2, AudioMixer.Rate) > -30, "the music came back");
    }

    [Fact]
    public void TheHandheldsSpeakersAndMonoTakeTheModernSoundAsTheyTakeTheDsOne()
    {
        var mixer = new AudioMixer { Mono = true };
        mixer.Play(ModernRoute(), night: false);
        var mono = Render(mixer, 3);
        for (int i = 0; i < mono.Length; i += 2) Assert.Equal(mono[i], mono[i + 1]);
        Assert.True(mono.Max(MathF.Abs) > 0.05f);

        var handheld = new AudioMixer { Handheld = true };
        handheld.Play(ModernRoute(), night: false);
        var small = Render(handheld, 3);
        // The handheld's ten bits: every sample a whole step of 1/512
        Assert.All(small, s => Assert.Equal(MathF.Round(s * 512f), s * 512f, 3));
        // And its band: far less energy in the lows than the stereo mix has
        var plain = new AudioMixer();
        plain.Play(ModernRoute(), night: false);
        var full = Render(plain, 3);
        Assert.True(Low(small) < Low(full) * 0.5, $"handheld lows {Low(small):0.0000}, stereo {Low(full):0.0000}");

        // The energy of a one-pole low-pass at about 150 Hz, over the whole
        static double Low(float[] s)
        {
            double y = 0, low = 0, all = 0, k = 1 - Math.Exp(-Math.Tau * 150 / AudioMixer.Rate);
            for (int i = 0; i < s.Length; i += 2)
            {
                y += k * (s[i] - y);
                low += y * y;
                all += s[i] * s[i];
            }
            return low / all;
        }
    }

    /// <summary>The game's mixer at 48 kHz on DS scenes, as MixRateTests' scenes and two more, rendered in pieces as the audio thread asks.</summary>
    private static float[] DsScene(string name)
    {
        var mixer = new AudioMixer();
        var twinleaf = MusicLibrary.Get("sinnoh/twinleaf")!;
        switch (name)
        {
            case "music": mixer.Play(twinleaf, false); return Render(mixer, 4);
            case "everything":
            {
                mixer.Play(MusicLibrary.Get("common/battle_wild")!, false, immediate: true);
                mixer.SetAmbience(new[] { new AmbienceLayer(AmbienceBed.Rain, 1f, -0.3f) });
                var a = Render(mixer, 0.5);
                mixer.PlaySound(SoundBank.Get("select")!, AudioBus.Sound, 1f, 0.5f);
                var b = Render(mixer, 0.7);
                mixer.PlaySound(Cries.Get(PokemonDatabase.Get("Piplup")!), AudioBus.Cry);
                var c = Render(mixer, 2.8);
                return [.. a, .. b, .. c];
            }
            case "handheld": mixer.Handheld = true; mixer.Mono = true; mixer.Play(twinleaf, true); return Render(mixer, 3);
            case "fanfare":
            {
                mixer.Play(twinleaf, false);
                var a = Render(mixer, 1);
                mixer.PlayFanfare(MusicLibrary.Get("common/fanfare_heal")!);
                var b = Render(mixer, 4);
                return [.. a, .. b];
            }
            case "lowhp":
            {
                mixer.Play(MusicLibrary.Get("common/battle_wild")!, false, immediate: true);
                var a = Render(mixer, 1.5);
                mixer.LowHp = true;
                var b = Render(mixer, 2);
                mixer.Giant = true;
                var c = Render(mixer, 1.5);
                return [.. a, .. b, .. c];
            }
            case "handover":
            {
                mixer.Play(twinleaf, false);
                var a = Render(mixer, 1.5);
                mixer.Play(MusicLibrary.Get("sinnoh/route201")!, false);
                var b = Render(mixer, 2.5);
                return [.. a, .. b];
            }
            default:
            {
                mixer.Play(twinleaf, false);
                var a = Render(mixer, 0.5);
                mixer.Away = true;
                var b = Render(mixer, 1);
                mixer.Away = false;
                var c = Render(mixer, 1);
                return [.. a, .. b, .. c];
            }
        }
    }

    // The game's mixer at 48 kHz, measured on the Windows machine (x64, .NET 10) with the code of main before MU2's first
    // line (MU1's mixer), by a scratch program outside the repository
    private static readonly (string Scene, string Hash)[] DsGoldens =
    {
        ("music", "8FB87E6D4B1F67EB"),
        ("everything", "DA5611722B03C339"),
        ("handheld", "A880CE1A2E333A50"),
        ("fanfare", "ED30815FD99B95B7"),
        ("away", "8436E0A104322A3A"),
        ("lowhp", "0C9E846B5C10B5AF"),
        ("handover", "888EBAAC7ABC3320"),
    };

    [Fact]
    public void TheDsSongsThroughTheMixerAt48kHzRenderToTheSampleAsBeforeMU2()
    {
        if (!OperatingSystem.IsWindows()) Assert.Skip("the golden samples were measured on Windows");
        var wrong = new List<string>();
        foreach (var (scene, hash) in DsGoldens)
        {
            var samples = DsScene(scene);
            var bytes = new byte[samples.Length * 4];
            Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
            string now = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes))[..16];
            if (now != hash) wrong.Add($"{scene}: {now} (was {hash})");
        }
        Assert.True(wrong.Count == 0, string.Join("\n", wrong));
    }

    [Fact]
    public void TheDsSongsThroughTheMixerAt48kHzAreNotMovedByTheModernPlayers()
    {
        // The modern players add nothing while no modern song plays: a DS scene renders the same with the modern sound
        // played and gone before it as with no modern sound ever played, once the modern song's last notes are out
        var plain = new AudioMixer();
        plain.Play(MusicLibrary.Get("sinnoh/twinleaf")!, night: false, immediate: true);
        var a = Render(plain, 3);

        var used = new AudioMixer();
        used.Play(ModernRoute(), night: false, immediate: true);
        Render(used, 1);
        used.Play(MusicLibrary.Get("sinnoh/twinleaf")!, night: false, immediate: true);
        var b = Render(used, 3);
        Assert.Equal(SynthSound.Ds, used.MusicSound);
        // The modern song was cut on a chunk's edge, so what is still queued of it ends within a few ms; after that the
        // two mixes differ only by the DS stage's grid having started a second later, which a song's own samples show
        Assert.Equal(Loudness.Of(a, 2, AudioMixer.Rate), Loudness.Of(b, 2, AudioMixer.Rate), 0.3);
    }

    [Fact]
    public void TheCompressorIsGentleAndOnlyTheModernSoundsOwn()
    {
        // The curve: nothing under the knee, the ratio's slope over it, continuous at both edges, never more than its most
        Assert.Equal(0f, Compressor.ReductionFor(Compressor.ThresholdDb - Compressor.KneeDb / 2f - 3f));
        Assert.Equal(0f, Compressor.ReductionFor(Compressor.ThresholdDb - Compressor.KneeDb / 2f), 4);
        float top = Compressor.ThresholdDb + Compressor.KneeDb / 2f;
        Assert.Equal(Compressor.ReductionFor(top - 0.001f), Compressor.ReductionFor(top + 0.001f), 2);
        Assert.Equal(-6f * (1f - 1f / Compressor.Ratio), Compressor.ReductionFor(Compressor.ThresholdDb + 6f), 4);
        Assert.Equal(-Compressor.MostReductionDb, Compressor.ReductionFor(40f));

        // A quiet sine passes at the make-up gain; a loud one is turned down by the curve
        float Gain(float amplitude)
        {
            var c = new Compressor(48000);
            var buf = new float[48000 * 2];
            for (int i = 0; i < 48000; i++) buf[i * 2] = buf[i * 2 + 1] = amplitude * MathF.Sin(i * 0.05f);
            for (int done = 0; done < buf.Length; done += 64) c.Process(buf.AsSpan(done, 64));
            float peak = 0f;
            for (int i = 40000; i < 48000; i++) peak = MathF.Max(peak, MathF.Abs(buf[i * 2]));
            return peak / amplitude;
        }
        Assert.Equal(MathF.Pow(10f, Compressor.MakeUpDb / 20f), Gain(0.05f), 3);
        Assert.True(Gain(0.9f) < MathF.Pow(10f, (Compressor.MakeUpDb - 1.5f) / 20f), $"a loud sine kept {Gain(0.9f):0.000}");

        // The DS sound's player has none; the modern one's works gently on a whole arrangement
        Assert.Equal((0f, 0f), new SongPlayer().Compression);
        OfflineMaster.Render(ModernRoute(), SynthSound.Modern, 12, out var player);
        Assert.InRange(player.Compression.Mean, -1.5f, 0f);
        Assert.InRange(player.Compression.Most, -Compressor.MostReductionDb, -0.1f);
    }

    [Fact]
    public void AModernSongRendersTheSameEveryTime()
    {
        string Once()
        {
            var mixer = new AudioMixer();
            mixer.Play(ModernBattle(), night: false, immediate: true);
            var a = Render(mixer, 1);
            mixer.LowHp = true;
            var b = Render(mixer, 1);
            return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Runtime.InteropServices.MemoryMarshal.AsBytes<float>([.. a, .. b])));
        }
        Assert.Equal(Once(), Once());
    }
}
