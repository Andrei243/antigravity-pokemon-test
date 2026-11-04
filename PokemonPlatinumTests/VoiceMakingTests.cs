using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 26 · V3 to V5's making of the voices, with no recording and no engine: the sample script and its page, the
/// checks of the user's recordings (fed stand-ins made here of synthesised vowels and room noise, nobody's voice), the
/// bake, the generation's plan, seeds and choice of takes (a stand-in engine), the trials' lines, the reel, the
/// pronunciation list and the cast's briefs.
/// </summary>
public sealed class VoiceMakingTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "voice-making-" + Guid.NewGuid().ToString("N"));

    public VoiceMakingTests() => Directory.CreateDirectory(folder);

    public void Dispose()
    {
        if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
    }

    private static string Repo(string relative) => Path.Combine(Repository.Root, relative.Replace('/', Path.DirectorySeparatorChar));

    private static SamplesFile Script => SamplesFile.Read(Repo("PokemonPlatinumEngine/Data/voice/samples/rival/samples.json"));

    private static VoiceFile Catalogue => VoiceCatalogue.Parse(File.ReadAllText(Repo("PokemonPlatinumEngine/Data/voice/lines.json")));

    private void Record(SamplePassage passage, float[] samples, int rate = VoiceSamples.Rate, int bits = 24, int channels = 1) =>
        File.WriteAllBytes(Path.Combine(folder, passage.Id + ".wav"), WavDecoder.Encode(samples, rate, bits, channels));

    private void RecordAll(Func<SamplePassage, bool>? except = null)
    {
        foreach (var p in Script.Passages)
            if (except == null || !except(p)) Record(p, VoiceSamples.StandIn(p));
    }

    // ------------------------------------------------------------------ the script

    [Fact]
    public void TheSampleScriptCoversEveryMoodTwiceInAboutTwentyPassages()
    {
        var script = Script;
        Assert.Equal(VoiceCast.Rival, script.Voice);
        Assert.Equal(48000, script.Rate);
        Assert.InRange(script.Passages.Count, 18, 25);
        Assert.Equal(script.Passages.Count, script.Passages.Select(p => p.Id).Distinct().Count());
        foreach (var p in script.Passages)
        {
            Assert.Matches(new Regex("^[0-9]{2}_[a-z_]+$"), p.Id);
            Assert.Contains(p.Mood, VoiceMoods.Samples);
            Assert.DoesNotContain("{", p.Text);
            Assert.False(string.IsNullOrWhiteSpace(p.Direction), $"{p.Id} has no direction");
        }
        foreach (string mood in VoiceMoods.Samples)
            Assert.True(script.Passages.Count(p => p.Mood == mood) >= 2, $"only one passage is {mood}");
        // Questions and exclamations for their rising and falling ends
        Assert.True(script.Passages.Count(p => p.Text.Contains('?')) >= 4);
        // The names the game says most
        string all = string.Join(" ", script.Passages.Select(p => p.Text));
        foreach (string name in new[] { "Pokémon", "Professor Rowan", "Twinleaf", "Turtwig", "Chimchar", "Piplup", "Sinnoh" })
            Assert.Contains(name, all);
    }

    [Fact]
    public void TheScriptsPageHoldsEveryPassageAsTheFileHasIt()
    {
        string page = File.ReadAllText(Repo("docs/voice/samples.md")).Replace("\r\n", "\n");
        foreach (var p in Script.Passages)
        {
            Assert.Contains($"### {p.Id} ({p.Mood})\n\n> {p.Text}\n", page);
        }
        // Its pronunciation guide is the list's
        var lexicon = VoiceLexicon.Default;
        foreach (Match row in Regex.Matches(page, @"^\| ([^|]+) \| ([^|]+) \|$", RegexOptions.Multiline))
        {
            string name = row.Groups[1].Value.Trim(), say = row.Groups[2].Value.Trim();
            if (name is "Name" or "---" || name.StartsWith("Setting") || name is "Format" or "Rate" or "Depth" or "Channels" or "Level" or "Effects") continue;
            Assert.True(lexicon.Of(name)?.Say == say, $"samples.md says {name} is '{say}', the list says '{lexicon.Of(name)?.Say}'");
        }
        Assert.True(File.Exists(Repo("docs/voice/recording.md")));
        Assert.True(File.Exists(Repo("tools/VoiceBooth/booth.html")));
    }

    // ------------------------------------------------------------------ the checks of the recordings

    [Fact]
    public void RecordingsMadeAsTheGuideSaysPassWithNothingToSay()
    {
        RecordAll();
        var report = VoiceSamples.Check(folder, Script);
        Assert.Empty(report.Missing);
        Assert.True(report.Findings.Count == 0, string.Join("\n", report.Findings.Select(f => $"{f.Passage}: {f.What}")));
        Assert.Equal(Script.Passages.Count, report.Measures.Count);
        foreach (var m in report.Measures)
        {
            Assert.InRange(m.PeakDb, -13, -11);
            Assert.True(m.FloorDb < VoiceSamples.NoisyFloor, $"{m.Passage}'s floor {m.FloorDb}");
            Assert.InRange(m.LeadSeconds, 0.35, 0.65);
        }
    }

    [Fact]
    public void ARecordingsFaultsAreNamed()
    {
        var script = Script;
        var p = script.Passages;
        RecordAll(q => q.Id == p[0].Id);
        // Missing: the first. Clipped: the second. Wrong rate: the third. Too quiet: the fourth. Noisy: the fifth.
        Record(p[1], VoiceSamples.StandIn(p[1]).Select(x => x * 30f).Select(x => Math.Clamp(x, -1f, 1f)).ToArray());
        Record(p[2], VoiceSamples.StandIn(p[2], rate: 44100), rate: 44100);
        Record(p[3], VoiceSamples.StandIn(p[3]).Select(x => x * 0.01f).ToArray());
        Record(p[4], VoiceSamples.StandIn(p[4], noiseDb: -30));
        // Cut short: the sixth, two seconds of it. No quiet before the voice: the seventh. Stereo and 16-bit: the eighth.
        Record(p[5], VoiceSamples.StandIn(p[5])[..(2 * VoiceSamples.Rate)]);
        Record(p[6], VoiceSamples.StandIn(p[6], lead: 0.02));
        var stereo = VoiceSamples.StandIn(p[7]).SelectMany(x => new[] { x, x }).ToArray();
        Record(p[7], stereo, bits: 16, channels: 2);
        File.WriteAllBytes(Path.Combine(folder, "my_extra_take.wav"), WavDecoder.Encode(VoiceSamples.StandIn(p[8]), 48000, 24, 1));

        var report = VoiceSamples.Check(folder, script);
        string Said(string id) => string.Join(" | ", report.Findings.Where(f => f.Passage == id).Select(f => f.Severity + ": " + f.What));
        bool Has(string id, VoiceSamples.Severity severity, string words) => report.Findings.Any(f => f.Passage == id && f.Severity == severity && f.What.Contains(words));

        Assert.Equal(new[] { p[0].Id }, report.Missing);
        Assert.True(Has(p[1].Id, VoiceSamples.Severity.Error, "clips"), Said(p[1].Id));
        Assert.True(Has(p[2].Id, VoiceSamples.Severity.Error, "44100 Hz"), Said(p[2].Id));
        Assert.True(Has(p[3].Id, VoiceSamples.Severity.Error, "too quiet"), Said(p[3].Id));
        Assert.True(Has(p[4].Id, VoiceSamples.Severity.Error, "room's noise"), Said(p[4].Id));
        Assert.True(Has(p[5].Id, VoiceSamples.Severity.Error, "too little to clone"), Said(p[5].Id));
        Assert.True(Has(p[6].Id, VoiceSamples.Severity.Warning, "quiet before it"), Said(p[6].Id));
        Assert.True(Has(p[7].Id, VoiceSamples.Severity.Warning, "two channels"), Said(p[7].Id));
        Assert.True(Has(p[7].Id, VoiceSamples.Severity.Warning, "16-bit"), Said(p[7].Id));
        Assert.Contains("my_extra_take", report.Loose);
        Assert.True(report.Errors >= 5);
        // What wasn't spoiled says nothing
        Assert.Empty(report.Findings.Where(f => f.Passage == p[9].Id));
    }

    [Fact]
    public void AFileThatIsNoWaveIsAnErrorNotACrash()
    {
        var p = Script.Passages[0];
        File.WriteAllBytes(Path.Combine(folder, p.Id + ".wav"), new byte[] { 1, 2, 3, 4, 5 });
        var (_, findings) = VoiceSamples.CheckOne(p, File.ReadAllBytes(Path.Combine(folder, p.Id + ".wav")));
        Assert.Contains(findings, f => f.Severity == VoiceSamples.Severity.Error && f.What.Contains("not a WAVE file"));
    }

    // ------------------------------------------------------------------ the bake

    [Fact]
    public void TheBakeTrimsLevelsLimitsAndFadesATakeToWhatThePackChecksHold()
    {
        var p = Script.Passages[3];
        float[] raw = VoiceSamples.StandIn(p, lead: 1.5);
        var baked = VoiceBake.Clean(raw, VoiceSamples.Rate, p.Text);
        Assert.Empty(baked.Findings);
        Assert.Equal(Synthesizer.DsRate, baked.Rate);
        // A second and a half of quiet each end is trimmed to a little
        double rawSeconds = raw.Length / (double)VoiceSamples.Rate;
        Assert.InRange(baked.Seconds, rawSeconds - 3.0, rawSeconds - 2.4);
        var (low, high) = Loudness.Target(AudioBus.Voice);
        Assert.InRange(Loudness.Of(baked.Samples), low, high);
        Assert.True(Loudness.Db(baked.Samples.Max(MathF.Abs)) <= Loudness.VoicePeak);
        Assert.Equal(0f, baked.Samples[0]);
        Assert.Equal(0f, baked.Samples[^1]);
    }

    [Fact]
    public void TheBakeTakesOutTheRumbleAndSoftensAThump()
    {
        int rate = 48000;
        var p = Script.Passages[0];
        float[] voice = VoiceSamples.StandIn(p);
        // A hum at 30 Hz under it
        float[] hummed = voice.Select((x, i) => x + 0.1f * MathF.Sin(2 * MathF.PI * 30 * i / rate)).ToArray();
        static double At(float[] x, int rate, float hz)
        {
            double re = 0, im = 0;
            for (int i = 0; i < x.Length; i++) { re += x[i] * Math.Cos(2 * Math.PI * hz * i / rate); im += x[i] * Math.Sin(2 * Math.PI * hz * i / rate); }
            return Math.Sqrt(re * re + im * im) / x.Length;
        }
        var cleaned = (float[])hummed.Clone();
        VoiceBake.HighPassInPlace(cleaned, rate, VoiceBake.HighPass);
        Assert.True(At(cleaned, rate, 30) < At(hummed, rate, 30) / 4, "the hum at 30 Hz is not cut");
        Assert.True(At(cleaned, rate, 1000) > At(hummed, rate, 1000) * 0.9, "the voice's body is cut with it");

        // A thump: forty milliseconds of 60 Hz, loud, in a quiet stretch
        var thump = new float[rate];
        for (int i = rate / 2; i < rate / 2 + rate / 25; i++) thump[i] = 0.5f * MathF.Sin(2 * MathF.PI * 60 * i / rate);
        static double Rms(float[] x) => Math.Sqrt(x.Average(v => (double)v * v));
        double before = Rms(thump), peak = thump.Max(MathF.Abs);
        VoiceBake.SoftenPlosives(thump, rate);
        Assert.True(Rms(thump) < before * 0.6,$"the thump is still {Rms(thump)} of {before}");
        Assert.True(thump.Max(MathF.Abs) < peak * 0.7, $"the thump's first swing is still {thump.Max(MathF.Abs)} of {peak}");
    }

    [Fact]
    public void ALineThatTakesTooLongOrTooLittleForItsWordsIsSaid()
    {
        Assert.True(VoiceBake.Fits("Come on, hurry up, we're going to be late!", 3.0, out _));
        Assert.False(VoiceBake.Fits("Come on, hurry up, we're going to be late for the boat to Canalave City!", 0.8, out _));
        Assert.False(VoiceBake.Fits("Come on, hurry up!", 12.0, out _));
        Assert.True(VoiceBake.Fits("Hey!", 0.6, out _));
    }

    // ------------------------------------------------------------------ generating

    /// <summary>
    /// An engine of synthesised vowels: every third seed makes a take cut to a third (a word skipped, as cloning engines
    /// sometimes do), so the generation must choose another.
    /// </summary>
    private sealed class Vowels : IVoiceEngine
    {
        public string Name => "stand-in vowels 1";
        public List<(string Key, int Seed)> Asked { get; } = new();

        public (float[] Samples, int Rate)? Speak(VoiceJob job, int seed)
        {
            Asked.Add((job.Key, seed));
            var p = new SamplePassage { Id = job.Key.Replace(':', '_'), Mood = "calm", Text = job.Text };
            float[] take = VoiceSamples.StandIn(p, 24000);
            if (seed % 3 == 0) take = take[..(take.Length / 3)];
            return (take, 24000);
        }
    }

    private static VoiceFile Few(params (string Key, string Voice, string En, string? Direction)[] lines) => new()
    {
        Lines = lines.Select(l => new VoiceLine
        {
            Key = l.Key, Voice = l.Voice, En = l.En, Hash = VoiceCatalogue.HashOf(l.En), Spoken = l.En, Status = VoiceLine.StatusSpoken, Direction = l.Direction
        }).ToList()
    };

    [Fact]
    public void APlanTakesAVoicesLinesWithTheirDirectionsSamplesAndSeeds()
    {
        var catalogue = Few(
            ("script:t.A#0.0", "rival", "Hey! Over here! The lake is this way!", null),
            ("script:t.A#1.0", "rival", "The lakes... something feels wrong.", "quiet"),
            ("script:t.A#2.0", "rowan", "So you came. Good.", null),
            ("script:t.A#3.0", "look:lass#2", "Do you like my dress?", null),
            ("script:t.A#4.0", "@self", "Welcome!", null));
        catalogue.Lines.Add(new VoiceLine { Key = "script:t.A#5.0", Voice = "rival", En = "Hey, {player}!", Hash = VoiceCatalogue.HashOf("Hey, {player}!"), Status = VoiceLine.StatusShownOnly });

        // Samples: one excited, one quiet, none calm recorded
        var script = new SamplesFile { Voice = "rival", Passages = { new() { Id = "a_excited", Mood = "excited", Text = "Go go go!" }, new() { Id = "b_quiet", Mood = "quiet", Text = "Shh." }, new() { Id = "c_calm", Mood = "calm", Text = "Hi." } } };
        File.WriteAllBytes(Path.Combine(folder, "a_excited.wav"), new byte[] { 0 });
        File.WriteAllBytes(Path.Combine(folder, "b_quiet.wav"), new byte[] { 0 });
        var samples = VoiceGeneration.SamplesOf(folder, script, folder);

        var plan = VoiceGeneration.Plan(catalogue, "rival", null, VoiceGeneration.Which.Missing, samples: samples, lexicon: VoiceLexicon.Default);
        Assert.Equal(new[] { "script:t.A#0.0", "script:t.A#1.0" }, plan.Jobs.Select(j => j.Key));
        var (excited, quiet) = (plan.Jobs[0], plan.Jobs[1]);
        Assert.Equal("excited", excited.Direction);
        Assert.True(excited.Guessed);
        Assert.Equal("a_excited.wav", excited.Sample);
        Assert.Equal("Go go go!", excited.SampleText);
        Assert.Equal("quiet", quiet.Direction);
        Assert.False(quiet.Guessed);
        Assert.Equal("b_quiet.wav", quiet.Sample);
        Assert.Equal(VoiceGeneration.Takes, excited.Seeds.Count);
        Assert.Equal(Enumerable.Range(0, 3).Select(t => VoiceGeneration.SeedOf(excited.Key, 0, t)), excited.Seeds);
        // The same on every run, and different for every line and take
        Assert.Equal(excited.Seeds, VoiceGeneration.Plan(catalogue, "rival", null, VoiceGeneration.Which.Missing, samples: samples).Jobs[0].Seeds);
        Assert.Equal(6, excited.Seeds.Concat(quiet.Seeds).Distinct().Count());
        // An archetype's voices by its name, and everyone
        Assert.Single(VoiceGeneration.Plan(catalogue, "look:lass", null, VoiceGeneration.Which.All).Jobs);
        Assert.Equal(4, VoiceGeneration.Plan(catalogue, "all", null, VoiceGeneration.Which.All).Jobs.Count);
    }

    [Fact]
    public void GeneratingKeepsTheBestTakeStampsItAndThePackPassesItsChecks()
    {
        var catalogue = Few(
            ("script:t.A#0.0", "rival", "Hey! Over here! The lake is this way, come and look!", null),
            ("script:t.A#1.0", "rival", "Did you see that? All three of them, right there in the briefcase!", "excited"));
        string pack = Path.Combine(folder, "pack");
        var plan = VoiceGeneration.Plan(catalogue, "rival", null, VoiceGeneration.Which.Missing);
        var engine = new Vowels();
        var (kept, failed) = VoiceGeneration.Generate(plan, engine, pack);

        Assert.Equal(2, kept.Count);
        Assert.Empty(failed);
        Assert.Equal(6, engine.Asked.Count);
        foreach (var entry in kept)
        {
            Assert.Equal("stand-in vowels 1", entry.Engine);
            Assert.NotEqual(0, entry.Seed!.Value % 3);
            Assert.Contains(entry.Seed!.Value, plan.Jobs.First(j => j.Key == entry.Key).Seeds);
        }
        var library = VoiceLibrary.Open(pack)!;
        var (wrong, stale) = VoicePacks.Check(library, catalogue);
        Assert.True(wrong.Count == 0, string.Join("; ", wrong.Select(w => w.Key + ": " + w.What)));
        Assert.Empty(stale);
        Assert.Equal(VoiceState.Current, library.StateOf("script:t.A#0.0", catalogue.Lines[0].En, "rival").State);

        // Missing now asks for nothing; a rewritten line is stale and asked for again
        Assert.Empty(VoiceGeneration.Plan(catalogue, "rival", library, VoiceGeneration.Which.Missing).Jobs);
        catalogue.Lines[0].En = catalogue.Lines[0].Spoken = "Hey! Over here! The lake's this way!";
        var again = VoiceGeneration.Plan(catalogue, "rival", library, VoiceGeneration.Which.Stale);
        Assert.Equal(new[] { "script:t.A#0.0" }, again.Jobs.Select(j => j.Key));
    }

    [Fact]
    public void ARerollTakesTheNextRoundsSeeds()
    {
        var catalogue = Few(("script:t.A#0.0", "rival", "Hey! Over here! The lake is this way, come and look!", null));
        string pack = Path.Combine(folder, "pack");
        VoiceGeneration.Generate(VoiceGeneration.Plan(catalogue, "rival", null, VoiceGeneration.Which.All), new Vowels(), pack);
        var library = VoiceLibrary.Open(pack)!;
        int kept = library.Entries[0].Seed!.Value;
        Assert.Equal(0, VoiceGeneration.RoundOf("script:t.A#0.0", kept));

        var reroll = VoiceGeneration.Reroll(catalogue, "script:t.A#0.0", library);
        Assert.Equal(Enumerable.Range(0, 3).Select(t => VoiceGeneration.SeedOf("script:t.A#0.0", 1, t)), reroll.Jobs.Single().Seeds);
        VoiceGeneration.Generate(reroll, new Vowels(), pack);
        library = VoiceLibrary.Open(pack)!;
        Assert.Single(library.Entries);
        Assert.Equal(1, VoiceGeneration.RoundOf("script:t.A#0.0", library.Entries[0].Seed!.Value));
        Assert.Throws<ArgumentException>(() => VoiceGeneration.Reroll(catalogue, "script:t.Nobody#0.0", library));
    }

    [Fact]
    public void BakingFilesMadeByHandFillsThePackLikeAnEngine()
    {
        var catalogue = Few(("script:t.A#0.0", "rival", "Hey! Over here! The lake is this way, come and look!", null));
        string takes = Path.Combine(folder, "takes");
        Directory.CreateDirectory(takes);
        var p = new SamplePassage { Id = "x", Text = catalogue.Lines[0].En };
        File.WriteAllBytes(Path.Combine(takes, VoicePacks.FileNameOf("script:t.A#0.0") + ".wav"), WavDecoder.Encode(VoiceSamples.StandIn(p), 48000, 24, 1));
        var engine = new VoiceGeneration.FolderEngine(takes, "by hand");
        Assert.True(engine.Has("script:t.A#0.0"));
        var plan = VoiceGeneration.Plan(catalogue, "rival", null, VoiceGeneration.Which.All, takes: 1);
        var (kept, _) = VoiceGeneration.Generate(plan, engine, Path.Combine(folder, "pack"));
        Assert.Equal("by hand", kept.Single().Engine);
        Assert.Empty(VoicePacks.Check(VoiceLibrary.Open(Path.Combine(folder, "pack"))!, catalogue).Wrong);
    }

    [Fact]
    public void TheTrialsLinesAreTwentyOfTheVoicesOwnTheSameEveryRun()
    {
        var catalogue = Catalogue;
        var rival = VoiceGeneration.TrialLines(catalogue, new[] { "rival" });
        Assert.Equal(20, rival.Count);
        Assert.All(rival, l => Assert.Equal("rival", l.Voice));
        Assert.All(rival, l => Assert.DoesNotContain("{", l.Spoken));
        Assert.Equal(20, rival.Select(l => l.En).Distinct().Count());
        Assert.True(rival.Select(l => l.Direction ?? VoiceMoods.Guess(l.En)).Distinct().Count() >= 3, "the rival's trial is all one mood");
        Assert.Equal(rival.Select(l => l.Key), VoiceGeneration.TrialLines(catalogue, new[] { "rival" }).Select(l => l.Key));

        string[] cast = { "rowan", "mom", "rival_mom", "assistant_boy", "assistant_girl", "looker", "cheryl", "roark", "mars" };
        var trial = VoiceGeneration.TrialLines(catalogue, cast);
        Assert.InRange(trial.Count, 15, 20);
        Assert.True(trial.Select(l => l.Voice).Distinct().Count() >= 7, "the cast's trial leaves out most of the cast: " + string.Join(", ", trial.Select(l => l.Voice).Distinct()));
    }

    [Fact]
    public void AReelPlaysATownsCurrentTakesOneAfterAnother()
    {
        var catalogue = Few(
            ("world:town.a.dialog.0", "look:lass#1", "Hello there, trainer! Nice day for it.", null),
            ("world:town.b.dialog.0", "look:man#2", "The sea is calm today.", null),
            ("world:other.c.dialog.0", "look:man#1", "Somewhere else entirely.", null));
        string pack = Path.Combine(folder, "pack");
        VoicePacks.WriteStandIns(pack, catalogue, "en", (_, _) => true);
        var library = VoiceLibrary.Open(pack)!;
        var (samples, keys) = VoiceGeneration.Reel(library, catalogue, "world:town.", gap: 0.5);
        Assert.Equal(2, keys.Count);
        int a = library.Load(library.Entries[0])!.Samples.Length, b = library.Load(library.Entries[1])!.Samples.Length;
        Assert.Equal(a + b + 2 * (int)(0.5 * Synthesizer.DsRate), samples.Length);
    }

    // ------------------------------------------------------------------ the pronunciation list and the cast's briefs

    [Fact]
    public void ThePronunciationListIsWellMadeAndSaysTheNamesOfTheFirstChapter()
    {
        var lexicon = VoiceLexicon.Default;
        Assert.Equal(lexicon.File.Entries.Count, lexicon.File.Entries.Select(e => e.Name).Distinct().Count());
        foreach (var e in lexicon.File.Entries)
        {
            Assert.True(VoiceLexicon.WellMade(e.Say), $"{e.Name}: '{e.Say}'");
            Assert.Contains(e.Kind, new[] { "word", "place", "person", "species" });
        }
        foreach (string name in new[] { "Pokémon", "Twinleaf", "Sandgem", "Jubilife", "Oreburgh", "Rowan", "Turtwig", "Chimchar", "Piplup", "Sinnoh" })
            Assert.NotNull(lexicon.Of(name));
        Assert.Equal("I'm taking PIP-lup to SAND-jem, with a POH-kay bawl!", lexicon.Respell("I'm taking Piplup to Sandgem, with a Poké Ball!"));
        Assert.Equal("pip-lup's", lexicon.Respell("Piplup's", lowerCase: true));
        // A name inside another word is no name
        Assert.Equal("Sinnohian", lexicon.Respell("Sinnohian"));
        var missing = lexicon.Missing(Few(("k", "v", "Bidoof and Kricketot and Kricketot.", null), ("j", "v", "Kricketot again.", null)), new[] { "Bidoof", "Kricketot", "Shinx" });
        Assert.Equal(new[] { ("Kricketot", 2) }, missing);
    }

    [Fact]
    public void TheFirstChaptersNamedCastHaveBriefs()
    {
        var voices = VoiceCast.Default.File.Voices;
        foreach (string id in new[] { "rowan", "mom", "rival_mom", "assistant_boy", "assistant_girl", "looker", "cheryl", "roark", "mars", "galactic_grunts" })
        {
            var brief = voices[id].Brief;
            Assert.True(brief != null, $"{id} has no brief");
            Assert.Contains(brief!.Register, CastBrief.Registers);
            Assert.Contains(brief.Age, CastBrief.Ages);
            Assert.Contains(brief.Pitch, CastBrief.Pitches);
            Assert.Contains(brief.Pace, CastBrief.Paces);
            Assert.False(string.IsNullOrWhiteSpace(brief.Manner));
        }
        // The rival is the user's, from the samples, never a designed voice
        Assert.Null(voices[VoiceCast.Rival].Brief);
    }

    [Fact]
    public void EveryDirectionWrittenIsOneTheSamplesCanCloneAndGuessesReadTheWords()
    {
        foreach (var line in Catalogue.Lines.Where(l => l.Direction != null))
            Assert.Contains(line.Direction!, VoiceMoods.Directions);
        foreach (string d in VoiceMoods.Directions) Assert.Contains(VoiceMoods.SampleFor(d), VoiceMoods.Samples);
        Assert.Equal("shouting", VoiceMoods.Guess("WAIT for me!"));
        Assert.Equal("shouting", VoiceMoods.Guess("Stop!! Right there!"));
        Assert.Equal("laughing", VoiceMoods.Guess("Ha ha! You fell in!"));
        Assert.Equal("excited", VoiceMoods.Guess("Let's go!"));
        Assert.Equal("quiet", VoiceMoods.Guess("I see... So that's how it is."));
        Assert.Equal("calm", VoiceMoods.Guess("The lake is north of town."));
        Assert.Equal("excited", VoiceMoods.Guess("I caught a POKéMON!"));
    }
}
