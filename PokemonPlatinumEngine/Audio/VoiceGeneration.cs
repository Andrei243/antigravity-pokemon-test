using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Audio;

/// <summary>One line to be spoken by an engine: what it says, how, from which sample, under which seeds.</summary>
public sealed class VoiceJob
{
    /// <summary>The take's key: a line's, or a variant's (<c>…~starter=Piplup</c>).</summary>
    public string Key { get; set; } = "";
    public string Voice { get; set; } = "";

    /// <summary>The spoken text, exactly as the engine is to say it.</summary>
    public string Text { get; set; } = "";

    /// <summary>The same with the names written as the pronunciation list says (for an engine that takes plain text).</summary>
    public string? Respelled { get; set; }

    /// <summary>The line's English as the catalogue has it now: what the take is stamped with.</summary>
    public string English { get; set; } = "";

    /// <summary>The direction written by hand, or the one guessed from the words (<see cref="Guessed"/>).</summary>
    public string Direction { get; set; } = "calm";
    public bool Guessed { get; set; }

    /// <summary>For a cloned voice: the sample (relative to the repository) the line takes its voice and delivery from.</summary>
    public string? Sample { get; set; }

    /// <summary>The sample's transcript, which some engines want beside it.</summary>
    public string? SampleText { get; set; }

    /// <summary>One seed to a take; the take that passes the bake best is kept.</summary>
    public List<int> Seeds { get; set; } = new();
}

/// <summary><c>voice/jobs/&lt;voice&gt;.json</c>: what <c>VoiceTool jobs</c> hands an engine and <c>generate</c> runs.</summary>
public sealed class VoiceJobFile
{
    public int Version { get; set; } = 1;
    public string Voice { get; set; } = "";
    public string Language { get; set; } = "en";
    public List<VoiceJob> Jobs { get; set; } = new();
}

/// <summary>
/// A text-to-speech or cloning engine as <c>VoiceTool generate</c> runs it: one take of a job under a seed, as mono samples
/// at the engine's own rate, or null where it failed. The engine is chosen by ear in plan 26's trials (V3 for the rival,
/// V4 for the rest); <c>tools/VoiceGen</c> runs it as a command line, and the tests run a stand-in of synthesised vowels.
/// </summary>
public interface IVoiceEngine
{
    /// <summary>The engine and its version, written into each take's entry of <c>pack.json</c>.</summary>
    string Name { get; }

    (float[] Samples, int Rate)? Speak(VoiceJob job, int seed);
}

/// <summary>
/// Plan 26 · V3 and V4's generation, GPU-free and engine-free: which lines a voice still needs (<see cref="Plan"/>), with
/// their spoken texts, directions, samples and seeds; a seed that is the same on every machine and every run for a line
/// and a round (<see cref="SeedOf"/>), so a pack can be made again as it was and a reroll gets new ones; the engine's
/// takes baked and the best kept (<see cref="Generate"/>), stamped in the pack with the engine, the seed and the sample.
/// </summary>
public static class VoiceGeneration
{
    /// <summary>Takes of each line, the best kept.</summary>
    public const int Takes = 3;

    /// <summary>Which lines a plan takes: those with no take, those whose take is stale, or all of them.</summary>
    public enum Which { Missing, Stale, All }

    /// <summary>The seed of a line's <paramref name="take"/>th take in <paramref name="round"/> (round 0 first, a reroll the next).</summary>
    public static int SeedOf(string key, int round, int take) => (int)(VoiceCast.Hash32($"{key}#{round}.{take}") & 0x7FFFFFFF);

    /// <summary>The round a seed of a line belongs to, or null (a seed made some other way).</summary>
    public static int? RoundOf(string key, int seed)
    {
        for (int round = 0; round < 1000; round++)
            for (int take = 0; take < 16; take++)
                if (SeedOf(key, round, take) == seed) return round;
        return null;
    }

    /// <summary>
    /// The jobs of the voices <paramref name="voice"/> names (a named id, <c>rival</c>; an archetype, <c>look:lass</c>,
    /// for all its voices; <c>all</c>): every spoken line and variant that <paramref name="which"/> asks for, given the pack
    /// as it is (null for none). A line said by whoever is spoken to (<c>@self</c>) is left out: its voice is decided at
    /// run time, so it is generated once per voice the people who say it have (V5). <paramref name="samples"/> gives a
    /// cloned voice's sample for a mood (and the line's key, to spread the lines over a mood's samples). A line whose voice
    /// chooses at run time (<c>assistant</c>: <c>assistant_boy</c> or <c>assistant_girl</c>) is a job in each voice it
    /// chooses from, by <paramref name="cast"/> (the game's when none is given), since the game plays the chosen one's take.
    /// </summary>
    public static VoiceJobFile Plan(VoiceFile catalogue, string voice, VoiceLibrary? pack, Which which, string language = "en",
        Func<string, string, (string Path, string Text)?>? samples = null, VoiceLexicon? lexicon = null, int round = 0, int takes = Takes, VoiceCast? cast = null)
    {
        var file = new VoiceJobFile { Voice = voice, Language = language };
        foreach (var line in catalogue.Lines)
        {
            if (line.Status != VoiceLine.StatusSpoken || line.Spoken == null || line.Voice == VoiceCast.Self) continue;
            // A voice chosen at run time (the assistant, by the player's look) is made in each of the voices it chooses from
            var speakers = (cast ?? VoiceCast.Default).File.Voices.TryGetValue(line.Voice, out var entry) && entry.Chooses is { Count: > 0 } chooses
                ? chooses.Where(c => Matches(line.Voice, voice) || Matches(c, voice)).ToList()
                : Matches(line.Voice, voice) ? new List<string> { line.Voice } : new List<string>();
            var keyed = line.Variants is { Count: > 0 } variants
                ? variants.Select(v => (Key: line.Key + v.Key, Text: v.Spoken))
                : new[] { (Key: line.Key, Text: line.Spoken) };
            foreach (string speaker in speakers)
            foreach (var (key, text) in keyed)
            {
                var state = pack?.StateOf(key, line.En, speaker).State ?? VoiceState.Missing;
                if (which == Which.Missing && state != VoiceState.Missing) continue;
                if (which == Which.Stale && state == VoiceState.Current) continue;
                string direction = line.Direction is { Length: > 0 } d ? d : VoiceMoods.Guess(line.En);
                var job = new VoiceJob
                {
                    Key = key, Voice = speaker, Text = text, English = line.En, Direction = direction, Guessed = line.Direction is not { Length: > 0 },
                    Respelled = lexicon?.Respell(text) is { } r && r != text ? r : null
                };
                if (samples?.Invoke(VoiceMoods.SampleFor(direction), key) is { } sample)
                {
                    job.Sample = sample.Path;
                    job.SampleText = sample.Text;
                }
                for (int t = 0; t < takes; t++) job.Seeds.Add(SeedOf(key, round, t));
                file.Jobs.Add(job);
            }
        }
        return file;
    }

    /// <summary>Whether a line's voice is one the filter names: the voice itself, an archetype's voices, or <c>all</c>.</summary>
    public static bool Matches(string lineVoice, string filter) =>
        filter == "all" || lineVoice == filter || lineVoice.StartsWith(filter + "#", StringComparison.Ordinal);

    /// <summary>
    /// The sample of a cloned voice for a mood: among the passages of that mood whose recording is there, one picked by
    /// the line's key (so a mood's lines spread over its samples); if the mood has none recorded, a calm one; null when
    /// nothing is recorded. Paths are as <paramref name="relativeTo"/> sees them.
    /// </summary>
    public static Func<string, string, (string Path, string Text)?> SamplesOf(string folder, SamplesFile script, string relativeTo)
    {
        var recorded = script.Passages.Where(p => File.Exists(Path.Combine(folder, p.Id + ".wav"))).ToList();
        return (mood, key) =>
        {
            var pool = recorded.Where(p => p.Mood == mood).ToList();
            if (pool.Count == 0) pool = recorded.Where(p => p.Mood == "calm").ToList();
            if (pool.Count == 0) pool = recorded;
            if (pool.Count == 0) return null;
            var chosen = pool[(int)(VoiceCast.Hash32(key) % (uint)pool.Count)];
            return (Path.GetRelativePath(relativeTo, Path.Combine(folder, chosen.Id + ".wav")).Replace('\\', '/'), chosen.Text);
        };
    }

    /// <summary>A take the engine made, baked, and how good it is (lower is better).</summary>
    public sealed record Take(int Seed, VoiceBake.Result Baked, double Score);

    /// <summary>
    /// Scores a baked take: a finding counts far more than anything else (a clip, a length that doesn't fit), then how far
    /// its pace is from an easy 2.8 words a second. Lower is better. A speech-to-text check (whisper.cpp, V3's) is to add
    /// its mismatches here once it is approved.
    /// </summary>
    public static double Score(VoiceBake.Result baked, string text)
    {
        if (baked.Samples.Length == 0) return double.MaxValue;
        VoiceBake.Fits(text, baked.Seconds, out double pace);
        return baked.Findings.Count * 100 + Math.Abs(pace - 2.8);
    }

    /// <summary>
    /// Runs the engine over the jobs, takes the best of each line's takes, writes it into the pack's folder as
    /// <c>&lt;voice&gt;/&lt;key&gt;.wav</c> and its entry into <c>pack.json</c> (an entry for the same key and voice is replaced;
    /// every other entry is kept). Returns what was kept, and the lines whose every take failed.
    /// </summary>
    public static (List<VoicePackEntry> Kept, List<(string Key, List<string> Findings)> Failed) Generate(VoiceJobFile jobs, IVoiceEngine engine, string packFolder,
        Action<string>? progress = null)
    {
        var kept = new List<VoicePackEntry>();
        var failed = new List<(string, List<string>)>();
        string packPath = Path.Combine(packFolder, VoiceLibrary.PackFileName);
        var pack = File.Exists(packPath) ? GameDataFiles.Deserialize<VoicePackFile>(File.ReadAllText(packPath)) : new VoicePackFile { Language = jobs.Language };
        foreach (var job in jobs.Jobs)
        {
            var made = new List<Take>();
            foreach (int seed in job.Seeds)
            {
                var spoken = engine.Speak(job, seed);
                if (spoken is not { } s || s.Samples.Length == 0) continue;
                var baked = VoiceBake.Clean(s.Samples, s.Rate, job.Text);
                made.Add(new Take(seed, baked, Score(baked, job.Text)));
            }
            var best = made.OrderBy(t => t.Score).ThenBy(t => job.Seeds.IndexOf(t.Seed)).FirstOrDefault();
            if (best == null || best.Baked.Samples.Length == 0)
            {
                failed.Add((job.Key, best?.Baked.Findings ?? new List<string> { "the engine made nothing" }));
                continue;
            }
            string relative = VoicePacks.FileOf(job.Voice, job.Key);
            string path = Path.Combine(packFolder, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            byte[] bytes = WavDecoder.Encode(best.Baked.Samples, best.Baked.Rate);
            File.WriteAllBytes(path, bytes);
            var entry = new VoicePackEntry
            {
                Key = job.Key, Voice = job.Voice, File = relative, TextHash = VoiceCatalogue.HashOf(job.English), FileHash = VoiceLibrary.FileHashOf(bytes),
                Engine = engine.Name, Seed = best.Seed, Sample = job.Sample
            };
            pack.Lines.RemoveAll(e => e.Key == job.Key && e.Voice == job.Voice);
            pack.Lines.Add(entry);
            kept.Add(entry);
            if (best.Baked.Findings.Count > 0) failed.Add((job.Key, best.Baked.Findings));
            progress?.Invoke($"{job.Key}: seed {best.Seed} of {made.Count} takes" + (best.Baked.Findings.Count > 0 ? " (with findings)" : ""));
        }
        pack.Lines.Sort((a, b) => string.CompareOrdinal(a.Voice + "\n" + a.Key, b.Voice + "\n" + b.Key));
        Directory.CreateDirectory(packFolder);
        File.WriteAllText(packPath, GameDataFiles.Serialize(pack));
        return (kept, failed);
    }

    /// <summary>
    /// The jobs of a reroll (<c>VoiceTool reroll &lt;key&gt;</c>): a line's own job with the next round's seeds, the round
    /// after the one its kept take's seed came from (round 1 when none is kept).
    /// </summary>
    public static VoiceJobFile Reroll(VoiceFile catalogue, string key, VoiceLibrary? pack, Func<string, string, (string Path, string Text)?>? samples = null, VoiceLexicon? lexicon = null)
    {
        string lineKey = key.Contains('~') ? key[..key.IndexOf('~')] : key;
        var line = catalogue.Lines.FirstOrDefault(l => l.Key == lineKey) ?? throw new ArgumentException($"no line is keyed {lineKey}");
        int? kept = pack?.Entries.FirstOrDefault(e => e.Key == key)?.Seed;
        int round = kept is { } seed && RoundOf(key, seed) is { } r ? r + 1 : 1;
        var plan = Plan(new VoiceFile { Lines = { line } }, line.Voice, null, Which.All, samples: samples, lexicon: lexicon, round: round);
        plan.Jobs.RemoveAll(j => j.Key != key);
        if (plan.Jobs.Count == 0) throw new ArgumentException($"{key} is no take of a spoken line");
        return plan;
    }

    /// <summary>
    /// The lines of a trial (plan 26 · V3's twenty of the rival's, V4's twenty of the named cast's): spread over the voices
    /// named, and within each voice over its directions (written or guessed), lines of five to thirty words with no
    /// placeholder, the same ones every run. The same lines go to every engine, so the user compares like with like.
    /// </summary>
    public static List<VoiceLine> TrialLines(VoiceFile catalogue, IReadOnlyList<string> voices, int count = 20)
    {
        var picked = new List<VoiceLine>();
        for (int v = 0; v < voices.Count; v++)
        {
            int want = count / voices.Count + (v < count % voices.Count ? 1 : 0);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var byDirection = catalogue.Lines
                .Where(l => l.Status == VoiceLine.StatusSpoken && l.Spoken is { } s && !s.Contains('{') && Matches(l.Voice, voices[v]))
                .Where(l => l.Spoken!.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length is >= 5 and <= 30)
                .Where(l => seen.Add(l.En))
                .GroupBy(l => l.Direction is { Length: > 0 } d ? d : VoiceMoods.Guess(l.En))
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => new Queue<VoiceLine>(g.OrderBy(l => VoiceCast.Hash32(l.Key)).ThenBy(l => l.Key, StringComparer.Ordinal)))
                .ToList();
            int taken = 0;
            while (taken < want && byDirection.Any(q => q.Count > 0))
                foreach (var q in byDirection)
                {
                    if (taken >= want || q.Count == 0) continue;
                    picked.Add(q.Dequeue());
                    taken++;
                }
        }
        return picked;
    }

    /// <summary>
    /// An engine whose takes are files already made (<c>VoiceTool bake</c>: an engine run by hand, or a person's
    /// recordings): a job's take is <c>&lt;key&gt;.wav</c> in the folder, named as <see cref="VoicePacks.FileNameOf"/> writes a key.
    /// </summary>
    public sealed class FolderEngine : IVoiceEngine
    {
        private readonly string folder;
        public string Name { get; }

        public FolderEngine(string folder, string name)
        {
            this.folder = folder;
            Name = name;
        }

        public bool Has(string key) => File.Exists(Path.Combine(folder, VoicePacks.FileNameOf(key) + ".wav"));

        public (float[] Samples, int Rate)? Speak(VoiceJob job, int seed)
        {
            string path = Path.Combine(folder, VoicePacks.FileNameOf(job.Key) + ".wav");
            if (!File.Exists(path)) return null;
            try
            {
                var wave = WavDecoder.Read(File.ReadAllBytes(path));
                return (wave.Mono, wave.Rate);
            }
            catch (InvalidDataException)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// A reel for spot listening (V5): the current takes whose keys begin with <paramref name="prefix"/> (a town's
    /// <c>world:jubilife_city.</c>, a script's <c>script:Twinleaf.</c>), in the catalogue's order, one after another with
    /// <paramref name="gap"/> seconds between, at the DS's rate. Returns the samples and the keys in it.
    /// </summary>
    public static (float[] Samples, List<string> Keys) Reel(VoiceLibrary pack, VoiceFile catalogue, string prefix, double gap = 0.5, string? voice = null)
    {
        var parts = new List<float[]>();
        var keys = new List<string>();
        foreach (var (key, english, _) in VoicePacks.KeysOf(catalogue))
        {
            if (!key.StartsWith(prefix, StringComparison.Ordinal)) continue;
            foreach (var entry in pack.Entries.Where(e => e.Key == key && (voice == null || Matches(e.Voice, voice))))
            {
                if (entry.TextHash != VoiceCatalogue.HashOf(english)) continue;
                if (pack.Load(entry) is not { } take) continue;
                parts.Add(take.Samples);
                keys.Add(key + " (" + entry.Voice + ")");
            }
        }
        int pause = (int)(gap * Synthesizer.DsRate);
        var reel = new float[parts.Sum(p => p.Length + pause)];
        int at = 0;
        foreach (var p in parts)
        {
            Array.Copy(p, 0, reel, at, p.Length);
            at += p.Length + pause;
        }
        return (reel, keys);
    }
}
