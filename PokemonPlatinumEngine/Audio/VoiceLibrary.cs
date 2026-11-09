using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Audio;

/// <summary>
/// A voice pack's <c>pack.json</c> (plan 26 · V2): what lines a pack of one language holds, each file beside it with the
/// hash of the English it was made from. <c>docs/data-files.md</c>, "Voice packs", has the format.
/// </summary>
public sealed class VoicePackFile
{
    public int Version { get; set; } = 1;

    /// <summary>The language the pack speaks (<c>en</c>): the folder's own name.</summary>
    public string Language { get; set; } = "en";

    public List<VoicePackEntry> Lines { get; set; } = new();
}

/// <summary>One file of a voice pack: a line's take, by the line's key (a variant's key, <c>…~starter=Piplup</c>, for a variant's take).</summary>
public sealed class VoicePackEntry
{
    /// <summary>The line's key (plan 17 · N4's), or a variant's: what <c>Data/voice/lines.json</c> lists.</summary>
    public string Key { get; set; } = "";

    /// <summary>The cast's voice that speaks it (<c>rival</c>, <c>look:lass#2</c>): a line said by whoever is spoken to has a take for each voice.</summary>
    public string Voice { get; set; } = "";

    /// <summary>The file, relative to the pack's folder: <c>rival/script_Twinleaf.Rival_3.0.wav</c>.</summary>
    public string File { get; set; } = "";

    /// <summary>The hash of the English the take was made from (<see cref="VoiceCatalogue.HashOf"/>): a line whose English has changed since is stale and is never played.</summary>
    public string TextHash { get; set; } = "";

    /// <summary>Eight hexadecimal digits of the file's bytes, which <c>VoiceTool check</c> holds the file to.</summary>
    public string? FileHash { get; set; }

    /// <summary>For a generated take (V3 on): the engine and its version, the seed and the sample it cloned from. Kept, never read by the game.</summary>
    public string? Engine { get; set; }
    public int? Seed { get; set; }
    public string? Sample { get; set; }
}

/// <summary>What a pack has of a line, as the game asks it.</summary>
public enum VoiceState { Missing, Stale, Current }

/// <summary>Turns a file of a voice pack into mono samples at the DS's rate. One to an extension; <see cref="VoiceLibrary.Decoders"/> holds them.</summary>
public interface IVoiceDecoder
{
    float[] Decode(byte[] file);
}

/// <summary>
/// The voices of one language (plan 26 · V2): a pack's <c>pack.json</c> read once, the question of whether a line has a
/// take that is current (its text hash the English's now, or the line is stale and silent), and the takes decoded into
/// samples, the last <see cref="Kept"/> held and those of a talk decoded ahead on a worker. Free of the GPU and of the
/// audio device, so tests decode and play through it without either. A missing or stale line is silent: the game runs
/// with no pack at all, and with a pack that lags the scripts.
/// </summary>
public sealed class VoiceLibrary
{
    /// <summary>How many decoded takes are held: the last ones asked for.</summary>
    public const int Kept = 48;

    public const string PackFileName = "pack.json";

    /// <summary>The language whose pack a language with none may speak under its translation (the options' ENGLISH VOICES).</summary>
    public const string English = "en";

    /// <summary>
    /// The decoders by extension. WAV (16- or 24-bit PCM or 32-bit float, any rate, mono or stereo) and Ogg Vorbis
    /// (<see cref="OggVoiceDecoder"/>, NVorbis) are built in. A take whose extension has no decoder counts as missing.
    /// </summary>
    public static Dictionary<string, IVoiceDecoder> Decoders { get; } = new(StringComparer.OrdinalIgnoreCase) { [".wav"] = new WavDecoder(), [".ogg"] = new OggVoiceDecoder() };

    public string Folder { get; }
    public string Language { get; }
    public IReadOnlyList<VoicePackEntry> Entries { get; }

    private readonly Dictionary<string, List<VoicePackEntry>> byKey = new(StringComparer.Ordinal);
    private readonly object gate = new();
    private readonly Dictionary<string, SoundSample> cache = new(StringComparer.Ordinal);
    private readonly LinkedList<string> order = new();
    private readonly Dictionary<string, Task<SoundSample?>> decoding = new(StringComparer.Ordinal);

    public VoiceLibrary(string folder, VoicePackFile pack)
    {
        Folder = folder;
        Language = pack.Language;
        Entries = pack.Lines;
        foreach (var e in pack.Lines)
        {
            if (!byKey.TryGetValue(e.Key, out var list)) byKey[e.Key] = list = new List<VoicePackEntry>();
            list.Add(e);
        }
    }

    /// <summary>The pack in <paramref name="folder"/>, or null where it holds no readable <c>pack.json</c>.</summary>
    public static VoiceLibrary? Open(string folder)
    {
        string path = Path.Combine(folder, PackFileName);
        if (!System.IO.File.Exists(path)) return null;
        try
        {
            var pack = JsonSerializer.Deserialize<VoicePackFile>(System.IO.File.ReadAllText(path), GameDataFiles.Json);
            return pack == null ? null : new VoiceLibrary(folder, pack);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Where the game looks for a language's pack: <c>voice/&lt;language&gt;</c> beside the executable, then in the folder
    /// the game is started in (the repository, while it is developed). Both are ignored by git, like <c>overrides/</c>.
    /// </summary>
    public static IEnumerable<string> FoldersFor(string language)
    {
        // The pack the game was told to speak with (--voices): a folder of languages, else the folder itself
        if (PokemonPlatinumEngine.Core.CommandLine.Launched.VoicePack is { Length: > 0 } given)
        {
            yield return Path.Combine(given, language);
            yield return given;
        }
        yield return Path.Combine(AppContext.BaseDirectory, "voice", language);
        yield return Path.Combine(Directory.GetCurrentDirectory(), "voice", language);
        // Then up from both, so a game started from Visual Studio's project folder or from bin/ still finds the pack kept at
        // the repository's root (voice/<language>, ignored by git)
        foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            for (var dir = new DirectoryInfo(start).Parent; dir != null; dir = dir.Parent)
                if (File.Exists(Path.Combine(dir.FullName, "voice", language, "pack.json")))
                {
                    yield return Path.Combine(dir.FullName, "voice", language);
                    break;
                }
    }

    /// <summary>
    /// The pack a language speaks with: its own, else (with the options' ENGLISH VOICES) English's, whose takes are found
    /// by the same keys and English hashes under the translated text; null for none.
    /// </summary>
    public static VoiceLibrary? Choose(string language, bool englishVoices, Func<string, VoiceLibrary?> find) =>
        find(language) ?? (englishVoices && language != English ? find(English) : null);

    /// <summary>The first pack of <paramref name="language"/> found where the game looks, or null.</summary>
    public static VoiceLibrary? Find(string language)
    {
        string? given = PokemonPlatinumEngine.Core.CommandLine.Launched.VoicePack;
        foreach (string folder in FoldersFor(language))
            // (the folder --voices names itself is the pack of the one language its pack.json says)
            if (Open(folder) is { } pack && (folder != given || string.Equals(pack.Language, language, StringComparison.OrdinalIgnoreCase))) return pack;
        return null;
    }

    /// <summary>
    /// What the pack has of a line: its take for <paramref name="voice"/> (or its only take, where the voice is unknown or
    /// the pack has one), current when its text hash is the hash of <paramref name="english"/>.
    /// </summary>
    public (VoiceState State, VoicePackEntry? Entry) StateOf(string key, string english, string? voice = null)
    {
        if (!byKey.TryGetValue(key, out var takes)) return (VoiceState.Missing, null);
        var take = voice != null ? takes.FirstOrDefault(t => t.Voice == voice) : null;
        take ??= voice == null || takes.Count == 1 ? takes[0] : null;
        if (take == null) return (VoiceState.Missing, null);
        if (!Decoders.ContainsKey(Path.GetExtension(take.File))) return (VoiceState.Missing, take);
        return (take.TextHash == VoiceCatalogue.HashOf(english) ? VoiceState.Current : VoiceState.Stale, take);
    }

    /// <summary>The samples of a take: decoded the first time (or taken from what a worker decoded ahead), then held among the last <see cref="Kept"/>. Null where the file can't be read.</summary>
    public SoundSample? Load(VoicePackEntry take)
    {
        Task<SoundSample?>? ahead;
        lock (gate)
        {
            if (cache.TryGetValue(take.File, out var held))
            {
                order.Remove(take.File);
                order.AddLast(take.File);
                return held;
            }
            decoding.TryGetValue(take.File, out ahead);
        }
        var sample = ahead != null ? ahead.Result : Decode(take);
        if (sample != null) Keep(take.File, sample);
        return sample;
    }

    /// <summary>Decodes a talk's takes ahead on a worker, so none is decoded on the frame that plays it.</summary>
    public void Prefetch(IEnumerable<VoicePackEntry> takes)
    {
        foreach (var take in takes)
            lock (gate)
            {
                if (cache.ContainsKey(take.File) || decoding.ContainsKey(take.File)) continue;
                var file = take;
                decoding[take.File] = Task.Run(() =>
                {
                    var sample = Decode(file);
                    if (sample != null) Keep(file.File, sample);
                    lock (gate) decoding.Remove(file.File);
                    return sample;
                });
            }
    }

    /// <summary>Waits for the takes a worker is decoding ahead, so that nothing still has a take's file open (a pack's folder can then be deleted).</summary>
    public void Settle()
    {
        Task[] pending;
        lock (gate) pending = decoding.Values.ToArray<Task>();
        Task.WaitAll(pending);
    }

    /// <summary>How many takes are held decoded now.</summary>
    public int Held
    {
        get { lock (gate) return cache.Count; }
    }

    private void Keep(string file, SoundSample sample)
    {
        lock (gate)
        {
            if (cache.ContainsKey(file)) return;
            cache[file] = sample;
            order.AddLast(file);
            while (cache.Count > Kept)
            {
                cache.Remove(order.First!.Value);
                order.RemoveFirst();
            }
        }
    }

    private SoundSample? Decode(VoicePackEntry take)
    {
        string path = Path.Combine(Folder, take.File);
        if (!Decoders.TryGetValue(Path.GetExtension(take.File), out var decoder) || !System.IO.File.Exists(path)) return null;
        try
        {
            return new SoundSample("voice " + take.Key, decoder.Decode(System.IO.File.ReadAllBytes(path)));
        }
        catch (Exception e) when (e is InvalidDataException or IOException or ArgumentException or IndexOutOfRangeException or NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>Eight hexadecimal digits of a file's bytes (FNV-1a): a pack's <see cref="VoicePackEntry.FileHash"/>.</summary>
    public static string FileHashOf(byte[] bytes)
    {
        uint h = 2166136261;
        foreach (byte b in bytes) h = (h ^ b) * 16777619;
        return h.ToString("x8");
    }
}

/// <summary>
/// The built-in decoder: a RIFF WAVE file of 16- or 24-bit PCM or 32-bit float, at any rate and in one or two channels,
/// into mono samples at <see cref="Synthesizer.DsRate"/> (two channels are averaged; another rate is taken to the DS's
/// by straight interpolation, which the bake makes unneeded by writing the DS's rate itself).
/// </summary>
public sealed class WavDecoder : IVoiceDecoder
{
    public float[] Decode(byte[] file)
    {
        var wave = Read(file);
        return wave.Rate == Synthesizer.DsRate ? wave.Mono : Resample(wave.Mono, wave.Rate, Synthesizer.DsRate);
    }

    /// <summary>
    /// A WAVE file as it was recorded (plan 26 · V3, the user's samples): its format, channels, rate and bit depth, its
    /// two channels averaged into one at its own rate, and the loudest any channel reached (a clip shows there, which
    /// the average can hide).
    /// </summary>
    public sealed record Wave(int Format, int Channels, int Rate, int Bits, float[] Mono, float Peak, int ClippedSamples)
    {
        public double Seconds => Mono.Length / (double)Rate;
    }

    /// <summary>Reads a WAVE file without changing its rate (<see cref="Decode"/> takes it to the DS's).</summary>
    public static Wave Read(byte[] file)
    {
        if (file.Length < 12 || file[0] != 'R' || file[1] != 'I' || file[2] != 'F' || file[3] != 'F' || file[8] != 'W' || file[9] != 'A' || file[10] != 'V' || file[11] != 'E')
            throw new InvalidDataException("not a RIFF WAVE file");
        int format = 0, channels = 0, rate = 0, bits = 0;
        int at = 12;
        float[]? mono = null;
        float peak = 0f;
        int clipped = 0;
        while (at + 8 <= file.Length)
        {
            string id = System.Text.Encoding.ASCII.GetString(file, at, 4);
            int size = BitConverter.ToInt32(file, at + 4);
            int body = at + 8;
            if (size < 0 || body + size > file.Length) size = file.Length - body;
            if (id == "fmt ")
            {
                format = BitConverter.ToUInt16(file, body);
                channels = BitConverter.ToUInt16(file, body + 2);
                rate = BitConverter.ToInt32(file, body + 4);
                bits = BitConverter.ToUInt16(file, body + 14);
                if (format == 0xFFFE && size >= 26) format = BitConverter.ToUInt16(file, body + 24);
            }
            else if (id == "data")
            {
                if (channels is < 1 or > 2 || rate <= 0) throw new InvalidDataException("a WAVE file's data before its format, or of no rate");
                int width = bits / 8;
                if (!((format == 1 && bits is 16 or 24) || (format == 3 && bits == 32))) throw new InvalidDataException($"WAVE format {format} of {bits} bits is not read");
                int frames = size / (width * channels);
                mono = new float[frames];
                for (int f = 0; f < frames; f++)
                {
                    float sum = 0f;
                    for (int c = 0; c < channels; c++)
                    {
                        int p = body + (f * channels + c) * width;
                        float x = format == 3 ? BitConverter.ToSingle(file, p)
                            : bits == 16 ? BitConverter.ToInt16(file, p) / 32768f
                            : ((file[p] | file[p + 1] << 8 | (sbyte)file[p + 2] << 16) / 8388608f);
                        float a = MathF.Abs(x);
                        if (a > peak) peak = a;
                        if (a >= ClipLevel) clipped++;
                        sum += x;
                    }
                    mono[f] = sum / channels;
                }
            }
            at = body + size + (size & 1);
        }
        if (mono == null) throw new InvalidDataException("a WAVE file with no data");
        return new Wave(format, channels, rate, bits, mono, peak, clipped);
    }

    /// <summary>A sample this close to full scale counts as clipped (about −0.03 dBFS).</summary>
    public const float ClipLevel = 0.9965f;

    /// <summary>Straight interpolation from one rate to another.</summary>
    public static float[] Resample(float[] input, int from, int to)
    {
        if (input.Length == 0) return input;
        int length = (int)((long)input.Length * to / from);
        var output = new float[length];
        double step = from / (double)to;
        for (int i = 0; i < length; i++)
        {
            double x = i * step;
            int a = (int)x;
            int b = Math.Min(a + 1, input.Length - 1);
            float t = (float)(x - a);
            output[i] = input[a] + (input[b] - input[a]) * t;
        }
        return output;
    }

    /// <summary>Writes mono samples as a 16-bit PCM WAVE file at <paramref name="rate"/> (a test's stand-in take, the tool's bake).</summary>
    public static byte[] Encode(ReadOnlySpan<float> samples, int rate = Synthesizer.DsRate) => Encode(samples, rate, 16, 1);

    /// <summary>Writes samples as a PCM WAVE file of 16 or 24 bits, interleaved when <paramref name="channels"/> is 2 (the tests' stand-ins for the user's recordings).</summary>
    public static byte[] Encode(ReadOnlySpan<float> samples, int rate, int bits, int channels)
    {
        if (bits is not (16 or 24)) throw new ArgumentException("16 or 24 bits", nameof(bits));
        int width = bits / 8, data = samples.Length * width;
        var bytes = new byte[44 + data];
        void Ascii(int at, string s) { for (int i = 0; i < 4; i++) bytes[at + i] = (byte)s[i]; }
        Ascii(0, "RIFF");
        BitConverter.TryWriteBytes(bytes.AsSpan(4), 36 + data);
        Ascii(8, "WAVE");
        Ascii(12, "fmt ");
        BitConverter.TryWriteBytes(bytes.AsSpan(16), 16);
        BitConverter.TryWriteBytes(bytes.AsSpan(20), (short)1);
        BitConverter.TryWriteBytes(bytes.AsSpan(22), (short)channels);
        BitConverter.TryWriteBytes(bytes.AsSpan(24), rate);
        BitConverter.TryWriteBytes(bytes.AsSpan(28), rate * width * channels);
        BitConverter.TryWriteBytes(bytes.AsSpan(32), (short)(width * channels));
        BitConverter.TryWriteBytes(bytes.AsSpan(34), (short)bits);
        Ascii(36, "data");
        BitConverter.TryWriteBytes(bytes.AsSpan(40), data);
        for (int i = 0; i < samples.Length; i++)
        {
            int at = 44 + i * width;
            if (bits == 16) BitConverter.TryWriteBytes(bytes.AsSpan(at), (short)Math.Clamp((int)MathF.Round(samples[i] * 32767f), -32768, 32767));
            else
            {
                int v = Math.Clamp((int)MathF.Round(samples[i] * 8388607f), -8388608, 8388607);
                bytes[at] = (byte)v;
                bytes[at + 1] = (byte)(v >> 8);
                bytes[at + 2] = (byte)(v >> 16);
            }
        }
        return bytes;
    }
}
