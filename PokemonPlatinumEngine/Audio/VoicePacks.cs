using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Audio;

/// <summary>
/// What a voice pack is held to and how one is put together (plan 26 · V2), GPU-free, shared by <c>tools/VoiceTool</c>
/// (<c>manifest</c>, <c>check</c>, <c>vowels</c>) and the tests: where a take's file goes, the checks a pack's files must
/// pass, a manifest written from the files a folder holds, and a stand-in take of synthesised vowels, which is what the
/// tests play and what a machine with no pack can try the voices with.
/// </summary>
public static class VoicePacks
{
    /// <summary>A file or folder name for a key or a voice: everything a file name may hold, a colon as <c>+</c>.</summary>
    public static string FileNameOf(string keyOrVoice)
    {
        var chars = keyOrVoice.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
            if (chars[i] == ':') chars[i] = '+';
            else if (Array.IndexOf(Path.GetInvalidFileNameChars(), chars[i]) >= 0 || chars[i] is '"' or '<' or '>' or '|' or '?' or '*' or '/' or '\\') chars[i] = '_';
        return new string(chars);
    }

    /// <summary>Where a take of a line by a voice is kept in its pack: <c>&lt;voice&gt;/&lt;key&gt;.wav</c>, both as <see cref="FileNameOf"/> writes them.</summary>
    public static string FileOf(string voice, string key, string extension = ".wav") =>
        FileNameOf(voice) + "/" + FileNameOf(key) + extension;

    /// <summary>Every key the catalogue voices, with the English and voice of its line: each line's own, and each variant's.</summary>
    public static IEnumerable<(string Key, string English, string Voice)> KeysOf(VoiceFile catalogue)
    {
        foreach (var line in catalogue.Lines)
        {
            if (line.Status != VoiceLine.StatusSpoken) continue;
            if (line.Variants is { Count: > 0 } variants)
                foreach (var v in variants) yield return (line.Key + v.Key, line.En, line.Voice);
            else yield return (line.Key, line.En, line.Voice);
        }
    }

    /// <summary>
    /// A manifest of what a pack's folder holds: every file under it named as <see cref="FileOf"/> names a take of the
    /// catalogue, stamped with the hash of its line's English now (so a file made for the line as it is) and of its bytes.
    /// A line said by whoever is spoken to (<c>@self</c>) takes a file in any voice's folder for that voice.
    /// </summary>
    public static VoicePackFile Manifest(string folder, VoiceFile catalogue, string language)
    {
        var pack = new VoicePackFile { Language = language };
        var byFile = new Dictionary<string, (string Key, string English)>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, english, _) in KeysOf(catalogue)) byFile.TryAdd(FileNameOf(key), (key, english));
        if (!Directory.Exists(folder)) return pack;
        foreach (string voiceDir in Directory.GetDirectories(folder).OrderBy(d => d, StringComparer.Ordinal))
            foreach (string path in Directory.GetFiles(voiceDir).OrderBy(p => p, StringComparer.Ordinal))
            {
                string ext = Path.GetExtension(path);
                if (!VoiceLibrary.Decoders.ContainsKey(ext) || !byFile.TryGetValue(Path.GetFileNameWithoutExtension(path), out var line)) continue;
                string voice = Path.GetFileName(voiceDir).Replace('+', ':');
                pack.Lines.Add(new VoicePackEntry
                {
                    Key = line.Key, Voice = voice, File = Path.GetFileName(voiceDir) + "/" + Path.GetFileName(path),
                    TextHash = VoiceCatalogue.HashOf(line.English), FileHash = VoiceLibrary.FileHashOf(File.ReadAllBytes(path))
                });
            }
        return pack;
    }

    /// <summary>What <see cref="Check"/> found of one take: its key and what is wrong with it.</summary>
    public sealed record Finding(string Key, string What);

    /// <summary>
    /// A pack held to the game (plan 26 · V2's "Files fit the game"): every take's key is a line of the catalogue (or a
    /// variant's), its text hash is the line's English now (or it is stale, which is said but is no fault: the game only
    /// never plays it), its file is there, its bytes are what the manifest says, it decodes, it measures within the voice
    /// bus's loudness window with its peak under −1 dB, and it starts and ends at nothing.
    /// </summary>
    public static (List<Finding> Wrong, List<string> Stale) Check(VoiceLibrary pack, VoiceFile catalogue)
    {
        var wrong = new List<Finding>();
        var stale = new List<string>();
        var keys = KeysOf(catalogue).GroupBy(k => k.Key).ToDictionary(g => g.Key, g => g.First().English, StringComparer.Ordinal);
        var (low, high) = Loudness.Target(AudioBus.Voice);
        foreach (var take in pack.Entries)
        {
            if (!keys.TryGetValue(take.Key, out var english)) { wrong.Add(new(take.Key, "no line of the catalogue has this key (run VoiceTool catalogue, or the line is gone)")); continue; }
            if (take.TextHash != VoiceCatalogue.HashOf(english)) stale.Add(take.Key);
            string path = Path.Combine(pack.Folder, take.File);
            if (!File.Exists(path)) { wrong.Add(new(take.Key, $"its file {take.File} is not there")); continue; }
            byte[] bytes = File.ReadAllBytes(path);
            if (take.FileHash != null && take.FileHash != VoiceLibrary.FileHashOf(bytes)) wrong.Add(new(take.Key, "its file is not the one the manifest names (run VoiceTool manifest)"));
            if (!VoiceLibrary.Decoders.TryGetValue(Path.GetExtension(take.File), out var decoder)) { wrong.Add(new(take.Key, $"the game has no decoder for {Path.GetExtension(take.File)} files yet")); continue; }
            float[] samples;
            try { samples = decoder.Decode(bytes); }
            catch (Exception e) when (e is InvalidDataException or ArgumentException or IndexOutOfRangeException) { wrong.Add(new(take.Key, "it does not decode: " + e.Message)); continue; }
            if (samples.Length == 0) { wrong.Add(new(take.Key, "it is empty")); continue; }
            double level = Loudness.Of(samples);
            if (level < low || level > high) wrong.Add(new(take.Key, $"it measures {level:0.0} dB, outside the voice bus's window of {low} to {high}"));
            double peak = Loudness.Db(samples.Max(MathF.Abs));
            if (peak > Loudness.VoicePeak) wrong.Add(new(take.Key, $"its peak is {peak:0.0} dB, over {Loudness.VoicePeak} dB"));
            const float Nothing = 0.01f;
            if (MathF.Abs(samples[0]) > Nothing || MathF.Abs(samples[^1]) > Nothing) wrong.Add(new(take.Key, "it does not start and end at nothing (it would click)"));
        }
        return (wrong, stale);
    }

    /// <summary>
    /// A stand-in take: a few synthesised vowels, a syllable to about every three letters of the line, on a voice's own
    /// pitch, faded in and out, brought to the middle of the voice bus's window. Nobody's voice: what tests play, and what
    /// <c>VoiceTool vowels</c> writes so the voices can be tried in the game before a line is recorded or generated.
    /// </summary>
    public static float[] Vowels(string text, string voice)
    {
        int rate = Synthesizer.DsRate;
        uint seed = VoiceCast.Hash32(voice);
        float pitch = 110f + seed % 120;
        int syllables = Math.Clamp(text.Count(char.IsLetter) / 3, 1, 60);
        int syllable = (int)(0.16f * rate), gap = (int)(0.04f * rate), edge = (int)(0.05f * rate);
        var samples = new float[edge * 2 + syllables * (syllable + gap)];
        // The first two formants of a, e, i, o, u
        (float F1, float F2)[] vowels = { (730, 1090), (530, 1840), (270, 2290), (570, 840), (300, 870) };
        int at = edge;
        double phase = 0;
        for (int s = 0; s < syllables; s++)
        {
            var (f1, f2) = vowels[(int)((seed >> (s % 8 * 4)) + s) % vowels.Length];
            float bend = 1f + 0.06f * MathF.Sin(s * 1.7f);
            for (int i = 0; i < syllable; i++)
            {
                float env = MathF.Sin(MathF.PI * i / syllable);
                phase += Math.Tau * pitch * bend / rate;
                // A buzz at the pitch, its harmonics near the formants lifted
                float x = 0f;
                for (int h = 1; h <= 12; h++)
                {
                    float f = pitch * bend * h;
                    float weight = 1f / h + 1.2f * Near(f, f1, 120f) + 0.8f * Near(f, f2, 180f);
                    x += weight * MathF.Sin((float)(phase * h));
                }
                samples[at + i] = x * env;
            }
            at += syllable + gap;
        }
        // Brought to the middle of the voice bus's window, with its peak kept under −1 dB
        var (low, high) = Loudness.Target(AudioBus.Voice);
        double level = Loudness.Of(samples);
        float gain = Loudness.Linear((low + high) / 2 - level);
        float peak = samples.Max(MathF.Abs) * gain;
        float most = Loudness.Linear(Loudness.VoicePeak - 0.5);
        if (peak > most) gain *= most / peak;
        for (int i = 0; i < samples.Length; i++) samples[i] *= gain;
        return samples;
    }

    private static float Near(float f, float centre, float width)
    {
        float d = (f - centre) / width;
        return MathF.Exp(-d * d);
    }

    /// <summary>
    /// Writes a pack of stand-in takes into <paramref name="folder"/>: one for each line (and variant) of the catalogue that
    /// <paramref name="keep"/> lets through, and its <c>pack.json</c>. Returns how many it wrote.
    /// </summary>
    public static int WriteStandIns(string folder, VoiceFile catalogue, string language, Func<string, string, bool> keep)
    {
        var pack = new VoicePackFile { Language = language };
        foreach (var (key, english, voice) in KeysOf(catalogue))
        {
            if (!keep(key, voice)) continue;
            string file = FileOf(voice, key);
            string path = Path.Combine(folder, file);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            byte[] bytes = WavDecoder.Encode(Vowels(english, voice));
            File.WriteAllBytes(path, bytes);
            pack.Lines.Add(new VoicePackEntry { Key = key, Voice = voice, File = file, TextHash = VoiceCatalogue.HashOf(english), FileHash = VoiceLibrary.FileHashOf(bytes), Engine = "stand-in vowels" });
        }
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, VoiceLibrary.PackFileName), GameDataFiles.Serialize(pack));
        return pack.Lines.Count;
    }
}
