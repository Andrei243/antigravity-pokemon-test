using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Audio;

/// <summary>
/// The moods a voice is directed in (plan 26): a line's <c>direction</c> in <c>Data/voice/lines.json</c>, and the mood
/// of each of the user's samples, which a cloned line takes its delivery from. GPU-free.
/// </summary>
public static class VoiceMoods
{
    /// <summary>The six moods the samples cover (<c>docs/voice/samples.md</c>).</summary>
    public static IReadOnlyList<string> Samples { get; } = new[] { "calm", "excited", "shouting", "frustrated", "quiet", "laughing" };

    /// <summary>Every direction a line may be given: the samples' moods and three that clone from one of them.</summary>
    public static IReadOnlyList<string> Directions { get; } = new[] { "calm", "excited", "shouting", "frustrated", "quiet", "laughing", "whispering", "sad", "smug" };

    /// <summary>The mood of the sample a line of this direction clones from.</summary>
    public static string SampleFor(string direction) => direction switch
    {
        "whispering" or "sad" => "quiet",
        "smug" => "calm",
        _ when Samples.Contains(direction) => direction,
        _ => "calm"
    };

    /// <summary>
    /// A direction for a line nobody has directed yet, read from its words: a shout (a word in capitals, or "!!"),
    /// laughter ("Ha!"), a trailing off ("…" and nothing louder), excitement ("!"), or calm. A first guess only: what is
    /// written by hand in <c>lines.json</c> always wins.
    /// </summary>
    public static string Guess(string text)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        bool caps = words.Any(w => w.Count(char.IsLetter) >= 3 && w.Where(char.IsLetter).All(char.IsUpper) && !w.Contains("POK", StringComparison.Ordinal));
        if (caps || text.Contains("!!", StringComparison.Ordinal)) return "shouting";
        string lower = text.ToLowerInvariant();
        if (lower.StartsWith("ha", StringComparison.Ordinal) && (lower.StartsWith("ha!", StringComparison.Ordinal) || lower.StartsWith("haha", StringComparison.Ordinal) || lower.StartsWith("ha ha", StringComparison.Ordinal)))
            return "laughing";
        if (text.Contains('!')) return "excited";
        if (text.Contains("...", StringComparison.Ordinal) || text.Contains('…')) return "quiet";
        return "calm";
    }
}

/// <summary>One passage of the sample script: what is read, in what mood, and how.</summary>
public sealed class SamplePassage
{
    /// <summary>The passage's name, which is its file's: <c>04_excited_briefcase</c> is read into <c>04_excited_briefcase.wav</c>.</summary>
    public string Id { get; set; } = "";

    /// <summary>One of <see cref="VoiceMoods.Samples"/>.</summary>
    public string Mood { get; set; } = "calm";

    /// <summary>The words read, exactly (a cloning engine is handed them beside the sample).</summary>
    public string Text { get; set; } = "";

    /// <summary>How to read it: tone, pace, what to lean on.</summary>
    public string? Direction { get; set; }
}

/// <summary>
/// <c>Data/voice/samples/&lt;voice&gt;/samples.json</c> (plan 26 · V3): the passages the user reads for a cloned voice and
/// how each recording must be made. <c>docs/voice/samples.md</c> is the same script for a person to read.
/// </summary>
public sealed class SamplesFile
{
    public int Version { get; set; } = 1;

    /// <summary>The cast's voice the samples are of (<c>rival</c>).</summary>
    public string Voice { get; set; } = "";

    /// <summary>The rate the recordings are made at.</summary>
    public int Rate { get; set; } = 48000;

    public List<SamplePassage> Passages { get; set; } = new();

    public static SamplesFile Read(string path) => GameDataFiles.Deserialize<SamplesFile>(File.ReadAllText(path));
}

/// <summary>
/// The user's recordings held to what cloning needs (plan 26 · V3's <c>VoiceTool check-samples</c>), GPU-free: each
/// passage's file is there and is a WAVE file of 48 kHz, one channel, 24 bits; it doesn't clip, isn't too quiet, has a
/// quiet room behind it and half a second of that quiet at each end; its length fits its words; and together the takes
/// make two to three minutes and cover every mood. An error is something a clone would copy (a clip, the room's hum,
/// a passage cut short); a warning is something to look at.
/// </summary>
public static class VoiceSamples
{
    public enum Severity { Warning, Error }

    public sealed record Finding(string Passage, Severity Severity, string What);

    /// <summary>What was measured of one recording.</summary>
    public sealed record Measure(string Passage, double Seconds, int Rate, int Channels, int Bits, double PeakDb, double FloorDb, double LeadSeconds, double TailSeconds, double WordsPerSecond);

    public sealed record Report(List<Finding> Findings, List<Measure> Measures, List<string> Missing, List<string> Loose)
    {
        public int Errors => Findings.Count(f => f.Severity == Severity.Error);
        public double TotalSeconds => Measures.Sum(m => m.Seconds);
    }

    /// <summary>The rate, depth and channels the script asks for.</summary>
    public const int Rate = 48000, Bits = 24;

    /// <summary>A passage shorter than this gives the clone too little to go on; longer, it should have been two.</summary>
    public const double ShortestSeconds = 3, LongestSeconds = 35;

    /// <summary>The quiet each end should have, and more than this much is waiting about.</summary>
    public const double QuietAtEachEnd = 0.3, MostQuietAtAnEnd = 3;

    /// <summary>The peak: under −0.5 dB is no clip, but over −3 dB is too hot; under −24 dB the voice is lost in the noise.</summary>
    public const double HottestPeak = -3, QuietestPeak = -24;

    /// <summary>The room's own noise (the quietest tenth of the recording): over −50 dB is heard, over −40 dB is copied by the clone.</summary>
    public const double NoisyFloor = -50, TooNoisyFloor = -40;

    /// <summary>Words a second while speaking: slower than this the take has a long gap in it, faster it is cut short.</summary>
    public const double SlowestWords = 1.0, FastestWords = 5.5;

    /// <summary>All the takes together: under this the clone has too little, and the script asks for about two to three minutes.</summary>
    public const double LeastTotal = 90, MostTotal = 300;

    /// <summary>The length of a window the recording's level is read in.</summary>
    public const double Window = 0.05;

    /// <summary>Holds every passage of <paramref name="script"/> to the WAVE file of its name in <paramref name="folder"/>.</summary>
    public static Report Check(string folder, SamplesFile script)
    {
        var findings = new List<Finding>();
        var measures = new List<Measure>();
        var missing = new List<string>();
        var ids = script.Passages.Select(p => p.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var passage in script.Passages)
        {
            string path = Path.Combine(folder, passage.Id + ".wav");
            if (!File.Exists(path)) { missing.Add(passage.Id); continue; }
            var (measure, found) = CheckOne(passage, File.ReadAllBytes(path), script.Rate);
            findings.AddRange(found);
            if (measure != null) measures.Add(measure);
        }
        var loose = Directory.Exists(folder)
            ? Directory.GetFiles(folder, "*.wav").Select(Path.GetFileNameWithoutExtension).Where(n => n != null && !ids.Contains(n)).Select(n => n!).OrderBy(n => n, StringComparer.Ordinal).ToList()
            : new List<string>();
        foreach (string name in loose) findings.Add(new(name, Severity.Warning, "no passage of the script has this name, so nothing uses it (a passage's take is <id>.wav)"));

        if (missing.Count == 0 && measures.Count > 0)
        {
            double total = measures.Sum(m => m.Seconds);
            if (total < LeastTotal) findings.Add(new("(all)", Severity.Warning, $"the takes make {total:0} seconds: the clone wants about two to three minutes"));
            if (total > MostTotal) findings.Add(new("(all)", Severity.Warning, $"the takes make {total:0} seconds: more than five minutes is more than the clone needs"));
        }
        var moods = measures.Select(m => script.Passages.First(p => p.Id == m.Passage).Mood).ToHashSet();
        foreach (string mood in VoiceMoods.Samples)
            if (script.Passages.Any(p => p.Mood == mood) && !moods.Contains(mood))
                findings.Add(new("(all)", Severity.Warning, $"no take of a {mood} passage yet: lines in that mood will clone from another"));
        return new Report(findings, measures, missing, loose);
    }

    /// <summary>One recording held to its passage.</summary>
    public static (Measure? Measure, List<Finding> Findings) CheckOne(SamplePassage passage, byte[] file, int rate = Rate)
    {
        var f = new List<Finding>();
        void Error(string what) => f.Add(new(passage.Id, Severity.Error, what));
        void Warn(string what) => f.Add(new(passage.Id, Severity.Warning, what));
        WavDecoder.Wave wave;
        try { wave = WavDecoder.Read(file); }
        catch (Exception e) when (e is InvalidDataException or ArgumentException or IndexOutOfRangeException)
        {
            Error("it is not a WAVE file the game reads: " + e.Message + " (export as WAV, PCM, 24-bit)");
            return (null, f);
        }
        if (wave.Rate != rate) Error($"recorded at {wave.Rate} Hz: the script asks {rate} Hz (set the recorder, don't convert afterwards)");
        if (wave.Channels != 1) Warn("two channels: they are averaged into one; record in mono");
        if (wave.Bits != Bits) Warn($"{wave.Bits}-bit: 24-bit keeps the quiet ends cleaner");
        if (wave.Mono.Length == 0) { Error("it holds no sound"); return (null, f); }

        double seconds = wave.Seconds;
        if (seconds < ShortestSeconds) Error($"{seconds:0.0} s long: a take shorter than {ShortestSeconds} s is too little to clone from (was it cut off?)");
        if (seconds > LongestSeconds) Error($"{seconds:0.0} s long: over {LongestSeconds} s, split it or read only the passage");

        double peak = Loudness.Db(wave.Peak);
        if (wave.ClippedSamples > 0) Error($"it clips ({wave.ClippedSamples} samples at full scale): turn the input down and read it again");
        else if (peak > HottestPeak) Warn($"its peak is {peak:0.0} dB: aim for about −12 dB, it is close to clipping");
        if (peak < QuietestPeak) Error($"its peak is {peak:0.0} dB: too quiet, the noise would be raised with the voice (bring the microphone closer or the input up)");

        double mean = wave.Mono.Average();
        if (Math.Abs(mean) > 0.02) Warn($"a DC offset of {mean:0.000}: the microphone's input is off centre");

        var levels = Levels(wave.Mono, wave.Rate);
        double floor = Percentile(levels, 0.1);
        // A take with no quiet in it has no room's noise to measure: its quietest windows are the voice's own, which rise and
        // fall from one to the next where a room's noise holds steady
        bool noQuiet = floor > levels.Max() - 25 && Unsteady(levels);
        if (noQuiet) Warn("there is no quiet in it to tell the room's noise by: leave half a second before the voice and after it");
        else if (floor > TooNoisyFloor) Error($"the room's noise is {floor:0} dB: the clone copies it into every line (a quieter room, or the microphone closer)");
        else if (floor > NoisyFloor) Warn($"the room's noise is {floor:0} dB: under {NoisyFloor} dB is better");

        double threshold = noQuiet ? double.NegativeInfinity : Math.Max(floor + 12, peak - 40);
        int first = levels.FindIndex(l => l > threshold), last = levels.FindLastIndex(l => l > threshold);
        double lead = first < 0 ? seconds : first * Window, tail = last < 0 ? seconds : seconds - (last + 1) * Window;
        if (first >= 0)
        {
            if (lead < QuietAtEachEnd) Warn($"the voice starts {lead:0.00} s in: leave about half a second of quiet before it");
            if (tail < QuietAtEachEnd) Warn($"the voice ends {tail:0.00} s before the end: leave about half a second of quiet after it");
            if (lead > MostQuietAtAnEnd) Warn($"{lead:0.0} s of quiet before the voice: the bake trims it, but it may hold a breath or a click");
            if (tail > MostQuietAtAnEnd) Warn($"{tail:0.0} s of quiet after the voice: the bake trims it, but it may hold a breath or a click");
        }
        else Error("no voice could be told from the room's noise");

        int words = passage.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Count(w => w.Any(char.IsLetterOrDigit));
        double speaking = Math.Max(0.1, seconds - lead - tail);
        double rateOfWords = words / speaking;
        if (first >= 0 && rateOfWords < SlowestWords) Warn($"{rateOfWords:0.0} words a second: slower than the passage would take (a long gap, or more than the passage read?)");
        if (first >= 0 && rateOfWords > FastestWords) Warn($"{rateOfWords:0.0} words a second: faster than the passage would take (was part of it left out?)");

        return (new Measure(passage.Id, seconds, wave.Rate, wave.Channels, wave.Bits, peak, floor, lead, tail, rateOfWords), f);
    }

    /// <summary>The level of each <see cref="Window"/> of the samples, in dB of full scale (RMS).</summary>
    public static List<double> Levels(float[] samples, int rate)
    {
        int size = Math.Max(1, (int)(Window * rate));
        var levels = new List<double>(samples.Length / size + 1);
        for (int at = 0; at + size <= samples.Length; at += size)
        {
            double sum = 0;
            for (int i = 0; i < size; i++) sum += samples[at + i] * (double)samples[at + i];
            levels.Add(Loudness.Db(Math.Sqrt(sum / size)));
        }
        if (levels.Count == 0) levels.Add(Loudness.Db(0));
        return levels;
    }

    /// <summary>Whether the quietest windows differ by more than 2 dB from one another (the voice's own), not steady like a room's noise.</summary>
    private static bool Unsteady(List<double> levels)
    {
        var quietest = levels.OrderBy(v => v).Take(Math.Max(2, levels.Count * 8 / 100)).ToList();
        double mean = quietest.Average();
        return Math.Sqrt(quietest.Average(v => (v - mean) * (v - mean))) > 2;
    }

    private static double Percentile(List<double> values, double share)
    {
        var sorted = values.OrderBy(v => v).ToList();
        return sorted[Math.Clamp((int)(share * (sorted.Count - 1)), 0, sorted.Count - 1)];
    }

    /// <summary>
    /// A stand-in for a recording (the tests' and nobody's voice): quiet room noise round the synthesised vowels of the
    /// passage's words, at the script's rate, with its peak at about −12 dB. What <c>check-samples</c> passes.
    /// </summary>
    public static float[] StandIn(SamplePassage passage, int rate = Rate, double noiseDb = -62, double lead = 0.5)
    {
        float[] vowels = VoicePacks.Vowels(passage.Text, "sample:" + passage.Mood);
        float[] voice = WavDecoder.Resample(vowels, Synthesizer.DsRate, rate);
        // Vowels make three letters a syllable at 0.2 s; a passage is read at about three words a second
        float peak = voice.Max(MathF.Abs);
        float gain = Loudness.Linear(-12) / Math.Max(peak, 1e-6f);
        int edge = (int)(lead * rate);
        var samples = new float[voice.Length + edge * 2];
        float noise = Loudness.Linear(noiseDb) * 1.7f;
        uint state = VoiceCast.Hash32(passage.Id) | 1;
        for (int i = 0; i < samples.Length; i++)
        {
            state ^= state << 13; state ^= state >> 17; state ^= state << 5;
            samples[i] = noise * ((state & 0xFFFF) / 32768f - 1f);
        }
        for (int i = 0; i < voice.Length; i++) samples[edge + i] += voice[i] * gain;
        return samples;
    }
}
