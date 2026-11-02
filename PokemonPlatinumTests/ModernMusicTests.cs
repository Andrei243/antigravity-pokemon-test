using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Xunit;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 31 · MU0: the modern sound (a synthesiser at 48 kHz with more voices, a room reverb, stereo width and the
/// Alola instruments) beside the DS sound, which must not move a sample. The DS songs are held to what they rendered
/// before the plan by a hash of their samples; the modern renderer is held to the music bus's loudness window and the
/// clash and click checks, on the one test tune (<c>tools/MusicRender/songs/mu0_island_route.mml</c>).
/// </summary>
public class ModernMusicTests
{
    /// <summary>The first six seconds of a song through its own player in the DS sound, as 16-bit samples hashed.</summary>
    private static string DsHash(string id, bool night = false, bool lowHp = false)
    {
        var player = new SongPlayer { Agitated = lowHp };
        player.Start(MusicLibrary.Get(id)!, night);
        var buffer = new float[Synthesizer.DsRate * 2 * 6];
        for (int done = 0; done < buffer.Length; done += 2048) player.Render(buffer.AsSpan(done, Math.Min(2048, buffer.Length - done)), 1f);
        return Hash(buffer);
    }

    private static string Hash(float[] samples)
    {
        var bytes = new byte[samples.Length * 4];
        Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
        return Convert.ToHexString(SHA256.HashData(bytes))[..16];
    }

    // Measured on the Windows machine (x64, .NET 10) before plan 31's first line was written. The maths library's
    // last bit may differ on another system, where the test says so and stops.
    private static readonly (string Id, bool Night, bool LowHp, string Hash)[] DsGoldens =
    {
        ("sinnoh/twinleaf", false, false, "E586F9622B92E55B"),
        ("sinnoh/twinleaf", true, false, "9BFF3DCA01034B38"),
        ("common/battle_wild", false, true, "38498E611AA8E7AA"),
        ("johto/battle_wild", false, false, "FC18D579BF508B52"),
        ("common/legendary/dialga", false, false, "B7050E938E36FEDF"),
        ("unova/battle_trainer", false, false, "E4B22568A87613CF"),
    };

    [Fact]
    public void TestTheDsSongsRenderToTheSampleAsBeforeThePlan()
    {
        if (!OperatingSystem.IsWindows()) Assert.Skip("the golden samples were measured on Windows");
        var wrong = new List<string>();
        foreach (var (id, night, lowHp, hash) in DsGoldens)
        {
            string now = DsHash(id, night, lowHp);
            if (now != hash) wrong.Add($"{id}{(night ? " night" : "")}{(lowHp ? " lowhp" : "")}: {now} (was {hash})");
        }
        Assert.True(wrong.Count == 0, string.Join("\n", wrong));
    }

    [Fact]
    public void TestTheDsSoundKeepsItsRateAndVoicesAndTheModernOneHasItsOwn()
    {
        var ds = new Synthesizer();
        Assert.Equal(SynthSound.Ds, ds.Sound);
        Assert.Equal(32768, ds.Rate);
        Assert.Equal(40, ds.VoiceCount);
        var modern = new Synthesizer(SynthSound.Modern);
        Assert.Equal(48000, modern.Rate);
        Assert.Equal(Synthesizer.ModernVoices, modern.VoiceCount);
        Assert.True(modern.VoiceCount > ds.VoiceCount);
        Assert.Equal(48000, new SongPlayer(SynthSound.Modern).Rate);
        Assert.Equal(Synthesizer.DsRate, new SongPlayer().Rate);
    }

    private static Song TestTune() =>
        Mml.Parse("mu0_island_route", File.ReadAllText(Path.Combine(Repository.Root, "tools", "MusicRender", "songs", "mu0_island_route.mml")));

    [Fact]
    public void TestTheHeaderNamesItsSoundAndItsRoomAndATrackItsWidth()
    {
        var song = TestTune();
        Assert.Equal(SynthSound.Modern, song.Sound);
        Assert.Equal(new RoomShape(RoomKind.Hall, 1.8f, RoomShape.Of(RoomKind.Hall, 1f).PreDelay, RoomShape.Of(RoomKind.Hall, 1f).Damping), song.Room);
        Assert.True(song.HasNight && song.HasLowHp);

        var made = Mml.Parse("t", "sound ds\nroom plate 1.2 predelay=0.01 damp=0.6\ntrack a strings width=0.4\n  c1 |\ntrack d kit\n  g4 d4 n4 w4 |\n");
        Assert.Equal(SynthSound.Ds, made.Sound);
        Assert.Equal(new RoomShape(RoomKind.Plate, 1.2f, 0.01f, 0.6f), made.Room);
        Assert.Equal(0.4f, made.Tracks[0].Width);
        Assert.Equal(new[] { Drum.HighConga, Drum.LowConga, Drum.Bongo, Drum.WoodBlock }, made.Tracks[1].Notes.Select(n => (Drum)n.Key));

        // A song that names neither is today's: no sound, no room, every track at full width
        var plain = MusicLibrary.Get("sinnoh/twinleaf")!;
        Assert.Null(plain.Sound);
        Assert.Null(plain.Room);
        Assert.All(plain.Tracks, t => Assert.Equal(1f, t.Width));

        Assert.Throws<FormatException>(() => Mml.Parse("t", "sound loud\ntrack a flute\n  c1 |\n"));
        Assert.Throws<FormatException>(() => Mml.Parse("t", "room cave 2\ntrack a flute\n  c1 |\n"));
        Assert.Throws<FormatException>(() => Mml.Parse("t", "room hall\ntrack a flute\n  c1 |\n"));
        Assert.Throws<FormatException>(() => Mml.Parse("t", "room hall 2 size=3\ntrack a flute\n  c1 |\n"));
    }

    [Fact]
    public void TestNoExistingSongNamesASoundYet()
    {
        // MU0 changes no song of the game: the modern sound is heard only through MusicRender until MU2
        Assert.All(MusicLibrary.All, s => Assert.Null(s.Sound));
    }

    [Theory]
    [InlineData(SynthSound.Ds, false, false)]
    [InlineData(SynthSound.Modern, false, false)]
    [InlineData(SynthSound.Ds, true, false)]
    [InlineData(SynthSound.Modern, true, false)]
    [InlineData(SynthSound.Ds, false, true)]
    [InlineData(SynthSound.Modern, false, true)]
    public void TestTheTestTuneRendersCleanlyInTheMusicBusWindowInBothSounds(SynthSound sound, bool night, bool lowHp)
    {
        var song = TestTune();
        var samples = OfflineMaster.Render(song, sound, 12, out var player, night, lowHp);
        int rate = sound == SynthSound.Modern ? 48000 : 32768;
        Assert.Equal(rate * 12 * 2, samples.Length);
        Assert.All(samples, s => Assert.True(float.IsFinite(s)));
        Assert.True(samples.Max(MathF.Abs) < 1f, "clips");
        var (low, high) = Loudness.Target(AudioBus.Music);
        double loud = Loudness.Of(samples, 2, rate);
        Assert.True(loud >= low && loud <= high, $"{sound} measures {loud:0.0} dB, outside {low} to {high}");
        // Enough voices: no note cut another short
        Assert.Equal(0, player.VoicesStolen);
    }

    [Fact]
    public void TestTheModernSoundMeasuresAsLoudAsTheDsOneAndIsWider()
    {
        var song = TestTune();
        var ds = OfflineMaster.Render(song, SynthSound.Ds, 12);
        var modern = OfflineMaster.Render(song, SynthSound.Modern, 12);
        double a = Loudness.Of(ds, 2, 32768), b = Loudness.Of(modern, 2, 48000);
        Assert.True(Math.Abs(a - b) < 1.5, $"DS {a:0.0} dB, modern {b:0.0} dB");
        Assert.True(Side(modern) > Side(ds), $"modern side {Side(modern):0.000}, DS side {Side(ds):0.000}");

        // The share of the sound that differs between the two sides
        static double Side(float[] s)
        {
            double mid = 0, side = 0;
            for (int i = 0; i < s.Length; i += 2)
            {
                double m = (s[i] + s[i + 1]) * 0.5, d = (s[i] - s[i + 1]) * 0.5;
                mid += m * m;
                side += d * d;
            }
            return side / (mid + side);
        }
    }

    [Theory]
    [InlineData(SynthSound.Ds)]
    [InlineData(SynthSound.Modern)]
    public void TestTheLoopsJoinIsNoLouderAStepThanTheLoopsOwnStart(SynthSound sound)
    {
        // MusicRender's join check: the largest step between two samples round the wrap, against the same where the
        // loop starts the first time through
        var song = TestTune();
        double perTick = song.SecondsPerTick(false);
        var samples = OfflineMaster.Render(song, sound, song.Duration() + 1.0);
        int rate = sound == SynthSound.Modern ? 48000 : 32768;
        float join = LargestStep(samples, (int)(song.EndTick * perTick * rate), rate);
        float start = LargestStep(samples, (int)(song.LoopTick * perTick * rate), rate);
        Assert.True(join <= start * 1.5f + 0.02f, $"join {join:0.000}, loop start {start:0.000}");
    }

    private static float LargestStep(float[] stereo, int at, int rate)
    {
        int reach = rate * 3 / 1000, frames = stereo.Length / 2;
        float largest = 0f;
        for (int i = Math.Max(1, at - reach); i < Math.Min(frames, at + reach); i++)
            for (int ch = 0; ch < 2; ch++) largest = MathF.Max(largest, MathF.Abs(stereo[i * 2 + ch] - stereo[(i - 1) * 2 + ch]));
        return largest;
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void TestTheTestTuneHasNoClashes(bool lowHp, bool giant)
    {
        // MusicRender's clash check: two parts a minor second or ninth apart where one starts on a beat and lasts an eighth
        var song = TestTune();
        var pitched = song.Tracks.Where(t => !t.IsDrums && t.When != (lowHp ? TrackWhen.NotLowHp : TrackWhen.LowHp)
            && t.When != (giant ? TrackWhen.NotGiant : TrackWhen.Giant)).ToList();
        var found = new List<string>();
        foreach (var a in pitched)
            foreach (var na in a.Notes.Where(n => n.Tick % Song.TicksPerQuarter == 0 && n.Length >= Song.TicksPerWhole / 8))
                foreach (var b in pitched.Where(b => !ReferenceEquals(a, b)))
                    foreach (var nb in b.Notes.Where(n => n.Tick <= na.Tick && n.Tick + n.Gate > na.Tick))
                    {
                        int iv = Math.Abs(na.Key + 12 * a.Instrument.OctaveShift - nb.Key - 12 * b.Instrument.OctaveShift);
                        if (iv % 12 == 1 && iv < 24) found.Add($"{a.Name} vs {b.Name} at tick {na.Tick}");
                    }
        Assert.Empty(found);
    }

    [Theory]
    [InlineData("ukulele")]
    [InlineData("slideguitar")]
    [InlineData("steeldrum")]
    [InlineData("logdrum")]
    [InlineData("warmpad")]
    public void TestEachAlolaInstrumentPlaysCleanlyAtTheBanksLevelInBothSounds(string name)
    {
        // As MusicRender --calibrate measures: quarter notes round middle C, dry; every instrument of the bank sits near −21 dB
        var inst = InstrumentBank.Get(name)!;
        foreach (var sound in new[] { SynthSound.Ds, SynthSound.Modern })
        {
            var synth = new Synthesizer(sound) { ReverbLevel = 0f };
            int n = synth.Rate * 4, step = synth.Rate / 2;
            var buf = new float[n * 2];
            int[] keys = { 60, 64, 67, 72 };
            for (int done = 0, k = 0; done < n; done += step, k++)
            {
                synth.NoteOn(inst, keys[k % keys.Length], 100 / 127f, step * 7 / 8, 0f, 0.8f, 0f);
                synth.Render(buf.AsSpan(done * 2, Math.Min(step, n - done) * 2));
            }
            Assert.All(buf, s => Assert.True(float.IsFinite(s)));
            double ss = 0;
            foreach (float x in buf) ss += x * x;
            double db = 20 * Math.Log10(Math.Sqrt(ss / buf.Length));
            Assert.InRange(db, -25.0, -17.0);
            // A note starts and ends at nothing: the first sample is silent and the last ones have died away
            Assert.True(MathF.Abs(buf[0]) < 0.02f && MathF.Abs(buf[1]) < 0.02f, $"{name} in {sound} starts at {buf[0]}");
        }
    }

    [Fact]
    public void TestHandDrumsPlayAndFallSilent()
    {
        foreach (var sound in new[] { SynthSound.Ds, SynthSound.Modern })
            foreach (var drum in new[] { Drum.HighConga, Drum.LowConga, Drum.Bongo, Drum.WoodBlock })
            {
                var synth = new Synthesizer(sound) { ReverbLevel = 0f };
                synth.NoteOn(InstrumentBank.Get("kit")!, (int)drum, 0.8f, synth.Rate / 4, 0f, 0.8f, 0f);
                var buf = new float[synth.Rate * 2];
                synth.Render(buf);
                Assert.True(buf.Max(MathF.Abs) > 0.05f, $"{drum} in {sound} is silent");
                Assert.True(buf.Max(MathF.Abs) < 1f, $"{drum} in {sound} clips");
                Assert.Equal(0, synth.ActiveVoices);
            }
    }

    [Fact]
    public void TestAHarderNoteIsBrighterInTheModernSoundOnly()
    {
        // Velocity reaches tone in the modern sound: the share of a note's energy above 2 kHz rises with it
        var trumpet = InstrumentBank.Get("trumpet")!;
        double Bright(SynthSound sound, float velocity)
        {
            var synth = new Synthesizer(sound) { ReverbLevel = 0f };
            synth.NoteOn(trumpet, 60, velocity, synth.Rate / 2, 0f, 0.8f, 0f);
            var buf = new float[synth.Rate / 4 * 2];
            synth.Render(buf);
            // A first difference is a crude high-pass: its energy over the signal's
            double all = 0, high = 0;
            for (int i = 2; i < buf.Length; i += 2)
            {
                all += buf[i] * buf[i];
                double d = buf[i] - buf[i - 2];
                high += d * d;
            }
            return high / all;
        }
        Assert.True(Bright(SynthSound.Modern, 1f) > Bright(SynthSound.Modern, 0.3f) * 1.1);
        Assert.Equal(Bright(SynthSound.Ds, 1f), Bright(SynthSound.Ds, 0.3f), 6);
    }
}
