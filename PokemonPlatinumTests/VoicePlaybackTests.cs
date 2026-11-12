using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 26 · V2: playing a voice. A pack of stand-in takes (synthesised vowels, <see cref="VoicePacks.Vowels"/>) is made
/// in the test itself, in a folder of its own, and played through the text box, the script runner and the mixer, none of
/// which needs a window or an audio device: what the game asks for is heard through <see cref="AudioManager.Listen"/>.
/// </summary>
public sealed class VoicePlaybackTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "voice-pack-" + Guid.NewGuid().ToString("N"));

    /// <summary>Every pack opened, so that Dispose can wait for the takes a worker decodes ahead before it deletes their files.</summary>
    private readonly List<VoiceLibrary> opened = new();

    public void Dispose()
    {
        AudioManager.StopListening();
        foreach (var library in opened) library.Settle();
        if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
    }

    /// <summary>A pack of the lines named, each a stand-in take made from the English given (a hash of other English makes it stale).</summary>
    private VoiceLibrary Pack(params (string Key, string English, string Voice, string MadeFrom)[] lines)
    {
        var pack = new VoicePackFile();
        foreach (var (key, english, voice, madeFrom) in lines)
        {
            string file = VoicePacks.FileOf(voice, key);
            string path = Path.Combine(folder, file);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, WavDecoder.Encode(VoicePacks.Vowels(english, voice)));
            pack.Lines.Add(new VoicePackEntry { Key = key, Voice = voice, File = file, TextHash = VoiceCatalogue.HashOf(madeFrom) });
        }
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, VoiceLibrary.PackFileName), GameDataFiles.Serialize(pack));
        var library = VoiceLibrary.Open(folder)!;
        opened.Add(library);
        return library;
    }

    private static DialogueManager Box(VoiceLibrary? pack, VoiceMode mode = VoiceMode.All, bool letFinish = false)
    {
        var box = new DialogueManager();
        box.Voice.Library = pack;
        box.Voice.Mode = mode;
        box.Voice.LetFinish = letFinish;
        return box;
    }

    private const string One = "Hey! Over here! The lake is this way!", Two = "Come on, hurry up!";

    private VoiceLibrary TwoLines() => Pack(("script:test.A#0.0", One, "rival", One), ("script:test.A#0.1", Two, "rival", Two));

    private static SpokenLine?[] TwoVoices() =>
        new SpokenLine?[] { new("script:test.A#0.0", One, "rival"), new("script:test.A#0.1", Two, "rival") };

    // ------------------------------------------------------------------ the pack

    [Fact]
    public void AStandInTakeIsAWaveFileTheGameReadsBackAtTheDsRate()
    {
        var samples = VoicePacks.Vowels(One, "rival");
        var back = new WavDecoder().Decode(WavDecoder.Encode(samples));
        Assert.Equal(samples.Length, back.Length);
        Assert.True(back.Zip(samples).Max(p => MathF.Abs(p.First - p.Second)) < 1f / 16000f);
        // Another rate is taken to the DS's
        var at48 = new WavDecoder().Decode(WavDecoder.Encode(samples, 48000));
        Assert.InRange(at48.Length, samples.Length * Synthesizer.DsRate / 48000 - 2, samples.Length * Synthesizer.DsRate / 48000 + 2);
        // And it sits in the voice bus's window, its peak under -1 dB, starting and ending at nothing
        var (low, high) = Loudness.Target(AudioBus.Voice);
        Assert.InRange(Loudness.Of(samples), low, high);
        Assert.True(Loudness.Db(samples.Max(MathF.Abs)) <= Loudness.VoicePeak);
        Assert.Equal(0f, samples[0]);
        Assert.Equal(0f, samples[^1]);
    }

    /// <summary>Encodes mono samples as Ogg Vorbis with the test-only encoder, entirely in memory.</summary>
    private static byte[] OggOf(float[] samples, int rate)
    {
        var info = OggVorbisEncoder.VorbisInfo.InitVariableBitRate(1, rate, 0.5f);
        var stream = new OggVorbisEncoder.OggStream(1);
        var comments = new OggVorbisEncoder.Comments();
        stream.PacketIn(OggVorbisEncoder.HeaderPacketBuilder.BuildInfoPacket(info));
        stream.PacketIn(OggVorbisEncoder.HeaderPacketBuilder.BuildCommentsPacket(comments));
        stream.PacketIn(OggVorbisEncoder.HeaderPacketBuilder.BuildBooksPacket(info));
        var output = new MemoryStream();
        void Pages(bool flush)
        {
            while (stream.PageOut(out var page, flush))
            {
                output.Write(page.Header, 0, page.Header.Length);
                output.Write(page.Body, 0, page.Body.Length);
            }
        }
        Pages(true);
        var processing = OggVorbisEncoder.ProcessingState.Create(info);
        processing.WriteData(new[] { samples }, samples.Length);
        processing.WriteEndOfStream();
        while (!stream.Finished && processing.PacketOut(out var packet))
        {
            stream.PacketIn(packet);
            Pages(false);
        }
        Pages(true);
        return output.ToArray();
    }

    [Fact]
    public void AnOggTakeIsDecodedToMonoAtTheDsRateAndPlaysFromAPack()
    {
        var samples = VoicePacks.Vowels(One, "rival");
        var ogg = OggOf(samples, Synthesizer.DsRate);
        var back = new OggVoiceDecoder().Decode(ogg);
        Assert.InRange(back.Length, samples.Length - 4096, samples.Length + 4096);
        Assert.True(back.Max(MathF.Abs) > 0.05f);
        // Another rate is taken to the DS's
        var at44 = new OggVoiceDecoder().Decode(OggOf(WavDecoder.Resample(samples, Synthesizer.DsRate, 44100), 44100));
        Assert.InRange(at44.Length, samples.Length - 4096, samples.Length + 4096);

        // And a pack of an .ogg file is found, current and loaded like a .wav
        Directory.CreateDirectory(folder);
        File.WriteAllBytes(Path.Combine(folder, "take.ogg"), ogg);
        File.WriteAllText(Path.Combine(folder, "bad.ogg"), "this is not an Ogg file");
        var file = new VoicePackFile();
        file.Lines.Add(new VoicePackEntry { Key = "k:ogg", Voice = "rival", File = "take.ogg", TextHash = VoiceCatalogue.HashOf(One) });
        file.Lines.Add(new VoicePackEntry { Key = "k:bad", Voice = "rival", File = "bad.ogg", TextHash = VoiceCatalogue.HashOf(One) });
        File.WriteAllText(Path.Combine(folder, VoiceLibrary.PackFileName), GameDataFiles.Serialize(file));
        var pack = VoiceLibrary.Open(folder)!;
        var (state, take) = pack.StateOf("k:ogg", One, "rival");
        Assert.Equal(VoiceState.Current, state);
        Assert.NotNull(pack.Load(take!));
        // A damaged file is a take that can't be read, never a crash
        Assert.Null(pack.Load(pack.StateOf("k:bad", One, "rival").Entry!));
    }

    [Fact]
    public void ATakeMadeFromOtherEnglishIsStaleAndOneThePackLacksIsMissing()
    {
        var pack = Pack(("k:current", One, "rival", One), ("k:stale", One, "rival", "An older line"));
        Assert.Equal(VoiceState.Current, pack.StateOf("k:current", One, "rival").State);
        Assert.Equal(VoiceState.Stale, pack.StateOf("k:stale", One, "rival").State);
        Assert.Equal(VoiceState.Missing, pack.StateOf("k:none", One, "rival").State);
        // A line of the person spoken to with one take serves whoever speaks it; with takes by voice, only the voice's own
        Assert.Equal(VoiceState.Current, pack.StateOf("k:current", One, "look:lass#2").State);
    }

    [Fact]
    public void ThePackHoldsOnlyTheLastTakesDecodedAndDecodesATalkAhead()
    {
        var lines = Enumerable.Range(0, VoiceLibrary.Kept + 5).Select(n => ($"k:{n}", "Hi.", "rival", "Hi.")).ToArray();
        var pack = Pack(lines);
        pack.Prefetch(pack.Entries.Take(3));
        foreach (var take in pack.Entries) Assert.NotNull(pack.Load(take));
        Assert.Equal(VoiceLibrary.Kept, pack.Held);
    }

    [Fact]
    public void AManifestListsTheFilesNamedForTheCatalogueAndTheCheckFindsNothingWrongWithThem()
    {
        var catalogue = new VoiceFile { Lines = { new VoiceLine { Key = "script:test.A#0.0", Voice = "rival", En = One, Hash = VoiceCatalogue.HashOf(One), Spoken = One } } };
        Assert.Equal(1, VoicePacks.WriteStandIns(folder, catalogue, "en", (_, _) => true));
        File.WriteAllText(Path.Combine(folder, "rival", "stray.wav"), "");
        var manifest = VoicePacks.Manifest(folder, catalogue, "en");
        var entry = Assert.Single(manifest.Lines);
        Assert.Equal("rival/script+test.A#0.0.wav", entry.File);
        var (wrong, stale) = VoicePacks.Check(VoiceLibrary.Open(folder)!, catalogue);
        Assert.Empty(wrong);
        Assert.Empty(stale);

        // The line rewritten: its take is stale, and the check says so without calling it wrong
        catalogue.Lines[0].En = Two;
        var (_, nowStale) = VoicePacks.Check(VoiceLibrary.Open(folder)!, catalogue);
        Assert.Single(nowStale);
    }

    // ------------------------------------------------------------------ the text box

    [Fact]
    public void ATalkAsksForItsLinesVoicesInOrderAsEachIsShown()
    {
        var heard = AudioManager.Listen();
        var box = Box(TwoLines());
        box.ShowDialogue("Barry", new[] { One, Two }, TwoVoices());
        Assert.Equal("script:test.A#0.0", box.Voice.Speaking?.Key);
        Assert.True(box.Voice.Left > 1f);
        box.FinishLine();
        box.Advance();
        Assert.Equal("script:test.A#0.1", box.Voice.Speaking?.Key);
        Assert.Equal(new[] { "voice script:test.A#0.0 rival", "voice play", "voice cut", "voice script:test.A#0.1 rival", "voice play" },
            heard.Where(h => h.StartsWith("voice", StringComparison.Ordinal)));
    }

    [Fact]
    public void AAtAHalfWrittenLineFinishesTheTextAndLeavesTheVoiceSpeaking()
    {
        var box = Box(TwoLines());
        box.ShowDialogue("Barry", new[] { One, Two }, TwoVoices());
        box.Voice.Advance(0.1f);
        box.FinishLine();
        Assert.True(box.IsCurrentLineComplete);
        Assert.True(box.Voice.IsSpeaking);
        Assert.Equal("script:test.A#0.0", box.Voice.Speaking!.Key);
    }

    [Fact]
    public void AAtAFinishedLineCutsTheVoiceUnlessVoicesAreLetFinish()
    {
        var heard = AudioManager.Listen();
        var cutting = Box(TwoLines());
        cutting.ShowDialogue("Barry", new[] { One }, TwoVoices().Take(1).ToArray());
        cutting.FinishLine();
        cutting.Advance();
        Assert.False(cutting.IsActive);
        Assert.False(cutting.Voice.IsSpeaking);
        Assert.Contains("voice cut", heard);

        var waiting = Box(TwoLines(), letFinish: true);
        waiting.ShowDialogue("Barry", new[] { One }, TwoVoices().Take(1).ToArray());
        waiting.FinishLine();
        waiting.Advance();
        Assert.True(waiting.IsActive);
        Assert.True(waiting.HeldForVoice);
        waiting.Update(waiting.Voice.Left + 0.01f);
        Assert.False(waiting.Voice.IsSpeaking);
        waiting.Advance();
        Assert.False(waiting.IsActive);
    }

    [Fact]
    public void AutoAdvanceCountsItsPauseFromTheEndOfTheVoice()
    {
        var box = Box(TwoLines());
        box.AutoAdvanceAfter = 0.5f;
        box.ShowDialogue("Barry", new[] { One, Two }, TwoVoices());
        box.FinishLine();
        float voice = box.Voice.Left;
        box.Update(0.6f);
        Assert.Equal(One.Replace("{player}", ""), box.CurrentLine);
        // The voice runs out, and the pause is counted from then
        for (float t = 0; t < voice; t += 0.25f) box.Update(0.25f);
        box.Update(0.6f);
        Assert.Equal(Two, box.CurrentLine);
    }

    [Fact]
    public void VoicesOffAskForNothingAndStoryOnlySpeaksTheNamedCastAlone()
    {
        var heard = AudioManager.Listen();
        var pack = Pack(("k:rival", One, "rival", One), ("k:lass", One, "look:lass#2", One));
        var off = Box(pack, VoiceMode.Off);
        off.ShowDialogue("", new[] { One }, new SpokenLine?[] { new("k:rival", One, "rival") });
        Assert.DoesNotContain(heard, h => h.StartsWith("voice", StringComparison.Ordinal));
        Assert.False(off.Voice.IsSpeaking);

        var story = Box(pack, VoiceMode.Story);
        story.ShowDialogue("", new[] { One, One }, new SpokenLine?[] { new("k:lass", One, "look:lass#2"), new("k:rival", One, "rival") });
        Assert.False(story.Voice.IsSpeaking);
        story.FinishLine();
        story.Advance();
        Assert.True(story.Voice.IsSpeaking);
        Assert.Equal(new[] { "voice k:rival rival", "voice play" }, heard.Where(h => h.StartsWith("voice", StringComparison.Ordinal)));
    }

    [Fact]
    public void AStaleOrMissingLineAndAGameWithNoPackPlayNothingAndShowTheText()
    {
        var heard = AudioManager.Listen();
        var stale = Box(Pack(("k:a", One, "rival", "Words since rewritten")));
        stale.ShowDialogue("", new[] { One }, new SpokenLine?[] { new("k:a", One, "rival") });
        Assert.Equal(One, stale.CurrentLine);
        Assert.False(stale.Voice.IsSpeaking);

        var none = Box(null);
        none.ShowDialogue("", new[] { One }, new SpokenLine?[] { new("k:a", One, "rival") });
        Assert.Equal(One, none.CurrentLine);
        Assert.False(none.Voice.IsSpeaking);
        // Asked for both times, played neither
        Assert.Equal(2, heard.Count(h => h == "voice k:a rival"));
        Assert.DoesNotContain("voice play", heard);
    }

    [Fact]
    public void AQuestionSpeaksAsItShowsAndClosingTheBoxCutsItsVoice()
    {
        var box = Box(TwoLines());
        box.ShowQuestion("Barry", One, TwoVoices()[0]);
        Assert.True(box.Voice.IsSpeaking);
        box.Close();
        Assert.False(box.Voice.IsSpeaking);
    }

    // ------------------------------------------------------------------ the mixer

    private static float[] Render(AudioMixer mixer, int frames)
    {
        var out_ = new float[frames * 2];
        mixer.Render(out_);
        return out_;
    }

    private static SoundSample Tone(float hz, float amp, float seconds) =>
        new("tone", Enumerable.Range(0, (int)(seconds * Synthesizer.DsRate)).Select(i => amp * MathF.Sin(MathF.Tau * hz * i / Synthesizer.DsRate)).ToArray());

    [Fact]
    public void TheMusicAndTheAmbienceDipUnderAVoiceAndComeBackAfter()
    {
        var mixer = new AudioMixer(Synthesizer.DsRate);
        Assert.Equal(1f, mixer.VoiceDuck);
        mixer.PlayVoice(Tone(200, 0.1f, 0.5f));
        Render(mixer, Synthesizer.DsRate / 5);
        Assert.True(mixer.VoicePlaying);
        Assert.Equal(AudioMixer.VoiceDuckLevel, mixer.VoiceDuck, 3);
        Render(mixer, Synthesizer.DsRate);
        Assert.False(mixer.VoicePlaying);
        Assert.Equal(1f, mixer.VoiceDuck);
    }

    [Fact]
    public void AVoiceLevelledToItsTakeWindowPlaysInsideTheVoicesPlayedWindow()
    {
        var (takeLow, takeHigh) = Loudness.Target(AudioBus.Voice);
        var (low, high) = Loudness.VoicePlayed;
        foreach (double mono in new[] { takeLow, (takeLow + takeHigh) / 2, takeHigh })
        {
            var mixer = new AudioMixer(Synthesizer.DsRate) { Volume = 1f }; // the bus as it stands before the mix's 0.8 trim, as the windows are
            float amp = (float)(Math.Pow(10, mono / 20) * Math.Sqrt(2)); // a sine's loudness is its peak less 3 dB
            mixer.PlayVoice(Tone(200, amp, 1.5f));
            var o = Render(mixer, (int)(Synthesizer.DsRate * 1.4));
            double played = Loudness.Of(o, 2, Synthesizer.DsRate);
            Assert.True(played >= low && played <= high, $"take {mono:0.0} played {played:0.00} window {low}..{high}");
            // The voice stands over the music's window dipped under it
            Assert.True(played > Loudness.Target(AudioBus.Music).High + 20 * Math.Log10(AudioMixer.VoiceDuckLevel) - 3, $"{played:0.0} dB");
        }
    }

    [Fact]
    public void TheVoiceVolumeRunsToTwoAndNeverClips()
    {
        var mixer = new AudioMixer(Synthesizer.DsRate);
        mixer.SetVolume(AudioBus.Voice, 5f);
        Assert.Equal(AudioMixer.VoiceMaxVolume, mixer.VolumeOf(AudioBus.Voice));
        mixer.SetVolume(AudioBus.Music, 5f);
        Assert.Equal(1f, mixer.VolumeOf(AudioBus.Music));
        // A take at the file check's peak ceiling, at the loudest setting
        mixer.PlayVoice(Tone(200, Loudness.Linear(Loudness.VoicePeak), 0.5f));
        var o = Render(mixer, Synthesizer.DsRate / 3);
        Assert.True(o.Max(MathF.Abs) <= 1f);
        // Louder at 200 than at 100
        var a = new AudioMixer(Synthesizer.DsRate); a.SetVolume(AudioBus.Voice, 1f);
        var b = new AudioMixer(Synthesizer.DsRate); b.SetVolume(AudioBus.Voice, 2f);
        a.PlayVoice(Tone(200, 0.1f, 0.5f)); b.PlayVoice(Tone(200, 0.1f, 0.5f));
        Assert.True(Loudness.Of(Render(b, Synthesizer.DsRate / 3), 2, Synthesizer.DsRate) > Loudness.Of(Render(a, Synthesizer.DsRate / 3), 2, Synthesizer.DsRate) + 5.5);
        // Soft limit: transparent below its knee, bounded above
        Assert.Equal(0.5f, AudioMixer.VoiceLimit(0.5f));
        Assert.InRange(AudioMixer.VoiceLimit(4f), 0.99f, 1f);
        Assert.Equal(-AudioMixer.VoiceLimit(0.9f), AudioMixer.VoiceLimit(-0.9f));
    }

    [Fact]
    public void ACryDipsNoVoice()
    {
        // The mix is linear below its limiter: a voice heard with a cry is the voice alone and the cry alone, added
        var voice = Tone(220, 0.1f, 0.3f);
        var cry = new SoundSample("cry", Tone(660, 0.1f, 0.3f).Samples);
        var both = new AudioMixer(Synthesizer.DsRate);
        both.PlayVoice(voice);
        both.PlaySound(cry, AudioBus.Cry);
        var alone = new AudioMixer(Synthesizer.DsRate);
        alone.PlayVoice(voice);
        var cryAlone = new AudioMixer(Synthesizer.DsRate);
        cryAlone.PlaySound(cry, AudioBus.Cry);
        var a = Render(both, 4096);
        var b = Render(alone, 4096);
        var c = Render(cryAlone, 4096);
        for (int i = 0; i < a.Length; i++) Assert.True(MathF.Abs(b[i] + c[i] - a[i]) < 1e-5f, $"sample {i}: {a[i]} with the cry, {b[i] + c[i]} apart");
    }

    [Fact]
    public void ANewLineCutsTheOneBeforeWithAFadeThatNeverClicks()
    {
        var mixer = new AudioMixer(Synthesizer.DsRate);
        mixer.PlayVoice(Tone(200, 0.3f, 2f));
        Render(mixer, 1000);
        mixer.PlayVoice(new SoundSample("silence", new float[Synthesizer.DsRate]));
        var after = Render(mixer, 2048);
        // Each step of the fade is no bigger than the tone's own
        float largestStep = 0f;
        for (int i = 2; i < after.Length; i += 2) largestStep = MathF.Max(largestStep, MathF.Abs(after[i] - after[i - 2]));
        Assert.True(largestStep < 0.02f, $"a step of {largestStep}");
        // And it is over within the cut's 30 ms
        int fade = (int)(AudioMixer.VoiceCutTime * Synthesizer.DsRate) + 64;
        Assert.True(after.Skip(fade * 2).All(x => MathF.Abs(x) < 0.002f));
    }

    [Fact]
    public void AMixerWithNoVoiceRendersWhatItAlwaysDid()
    {
        var with = new AudioMixer(Synthesizer.DsRate);
        var without = new AudioMixer(Synthesizer.DsRate);
        var cry = new SoundSample("cry", Tone(440, 0.2f, 0.2f).Samples);
        with.PlaySound(cry, AudioBus.Cry);
        without.PlaySound(cry, AudioBus.Cry);
        // A voice played and over long before leaves the mix as it was
        with.PlayVoice(new SoundSample("nothing", new float[1]));
        Assert.Equal(Render(without, 2048), Render(with, 2048));
    }

    // ------------------------------------------------------------------ the scripts

    [Fact]
    public void AScriptHandsEachLineItsKeyAndVoiceAndTheGamesOwnVoiceNone()
    {
        var library = ScriptLibrary.FromSources(("test", """
            script A
              speaker "Prof. Rowan"
              say "Hello there." "Welcome."
              speaker none
              say "Received a Potion!"
              text "A sign."
              speaker "Prof. Rowan"
              ask "Ready?"
              waitvoice
            """), ("common", ""));
        var host = new HeadlessScriptHost();
        var runner = new ScriptRunner(library, host);
        runner.Start(library.All.First(s => s.File == "test"));
        runner.RunToEnd();
        Assert.Equal(new[] { "Hello there.", "Welcome.", "Received a Potion!", "A sign.", "Ready?" }, host.Transcript.Select(t => t.Text));
        string rowan = VoiceCast.Default.ForSpeaker("", "Prof. Rowan", "test.A/Prof. Rowan")!;
        Assert.Equal(new[] { (0, "script:test.A#1.0", rowan), (1, "script:test.A#1.1", rowan), (4, "script:test.A#5.0", rowan) },
            host.Voices.Select(v => (v.Line, v.Voice.Key, v.Voice.Voice!)));
    }

    [Fact]
    public void TheKeysTheGamesScriptsSpeakByAreTheCataloguesKeys()
    {
        // Every line a script of the game says by a speaker is keyed as the catalogue keys it, and voiced as it voices it
        var catalogue = VoiceCatalogue.Parse(File.ReadAllText(Path.Combine(Repository.Root, "PokemonPlatinumEngine", "Data", "voice", "lines.json")))
            .Lines.ToDictionary(l => l.Key, StringComparer.Ordinal);
        var library = ScriptLibrary.Default;
        int checkedLines = 0, sameVoice = 0, voiced = 0;
        foreach (var script in library.All.Where(s => s.File != ScriptLibrary.Common).Take(400))
        {
            var host = new HeadlessScriptHost();
            var runner = new ScriptRunner(library, host);
            runner.Start(script);
            // (A script played with nobody spoken to and nothing given may stop where the game never would: what it said till then counts)
            try { runner.RunToEnd(); } catch (Exception) { }
            foreach (var (_, voice) in host.Voices.Where(v => v.Voice.Key.StartsWith("script:", StringComparison.Ordinal)))
            {
                string key = voice.Key.Split('~')[0];
                Assert.True(catalogue.TryGetValue(key, out var line), $"{key} is no key of the catalogue");
                Assert.Equal(line!.En, voice.English);
                checkedLines++;
                // The catalogue reads a script top to bottom, the runner as it goes: a speaker named in a branch can part them
                if (voice.Voice == null || line.Voice == VoiceCast.Self || line.Voice.StartsWith("assistant", StringComparison.Ordinal)) continue;
                voiced++;
                if (line.Voice == voice.Voice) sameVoice++;
            }
        }
        Assert.True(checkedLines > 100, $"only {checkedLines} lines were played");
        Assert.True(sameVoice >= voiced * 9 / 10, $"{sameVoice} of {voiced} lines spoke in the catalogue's voice");
    }

    // ------------------------------------------------------------------ the options

    [Fact]
    public void TheVoicesThreeRowsComeAfterTheSettingsAndStepRound()
    {
        var rows = OptionsScreen.AllRows.ToList();
        Assert.Equal(rows.IndexOf(OptionRow.Movement) + 1, rows.IndexOf(OptionRow.Voices));
        Assert.Equal(rows.IndexOf(OptionRow.Voices) + 1, rows.IndexOf(OptionRow.VoiceVolume));
        Assert.Equal(rows.IndexOf(OptionRow.VoiceVolume) + 1, rows.IndexOf(OptionRow.LetVoicesFinish));
        Assert.Equal(rows.IndexOf(OptionRow.LetVoicesFinish) + 1, rows.IndexOf(OptionRow.EnglishVoices));
        Assert.Equal(rows.IndexOf(OptionRow.EnglishVoices) + 1, rows.IndexOf(OptionRow.Controls));

        var s = new GameSettings();
        Assert.Equal(VoiceMode.All, s.Voices); // everyone's unless chosen (the user's answer of 2026-11-10)
        Assert.False(s.LetVoicesFinish);
        var seen = new List<VoiceMode>();
        for (int i = 0; i < 3; i++) { OptionsScreen.Change(s, OptionRow.Voices, 1); seen.Add(s.Voices); }
        Assert.Equal(new[] { VoiceMode.Off, VoiceMode.Story, VoiceMode.All }, seen);
        OptionsScreen.Change(s, OptionRow.VoiceVolume, -1);
        Assert.Equal(100 - GameSettings.VolumeStep, s.VoiceVolume);
        for (int i = 0; i < 30; i++) OptionsScreen.Change(s, OptionRow.VoiceVolume, 1);
        Assert.Equal(GameSettings.MaxVoiceVolume, s.VoiceVolume);
        Assert.Equal(100, new GameSettings().MusicVolume);
        OptionsScreen.Change(s, OptionRow.MusicVolume, 1);
        Assert.Equal(100, s.MusicVolume); // the music's row still ends at 100: the voice row is its own
        OptionsScreen.Change(s, OptionRow.LetVoicesFinish, 1);
        Assert.True(s.LetVoicesFinish);
        foreach (var row in new[] { OptionRow.Voices, OptionRow.VoiceVolume, OptionRow.LetVoicesFinish })
            Assert.False(string.IsNullOrWhiteSpace(OptionsScreen.Describe(s, row).Help));
    }

    /// <summary>A pack beside the game (the user's machine, a release build) is held to it; with none the check says so and passes.</summary>
    [Fact]
    public void APackBesideTheGameFitsIt()
    {
        var pack = VoiceLibrary.FoldersFor("en").Select(VoiceLibrary.Open).FirstOrDefault(p => p != null)
            ?? VoiceLibrary.Open(Path.Combine(Repository.Root, "voice", "en"));
        if (pack == null) return; // no pack here: nothing to hold to the game (V3 to V5 make one)
        var catalogue = VoiceCatalogue.Parse(File.ReadAllText(Path.Combine(Repository.Root, "PokemonPlatinumEngine", "Data", "voice", "lines.json")));
        var (wrong, _) = VoicePacks.Check(pack, catalogue);
        Assert.True(wrong.Count == 0, string.Join("\n", wrong.Take(20).Select(w => $"{w.Key}: {w.What}")));
    }
}
