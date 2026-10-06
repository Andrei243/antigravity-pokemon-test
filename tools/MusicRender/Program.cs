// Renders the game's songs and sound effects to WAV files, the way the game plays them, and checks them without
// ears: levels, clipping, NaNs, clicks, notes that clash (a minor second between two parts on a beat), and a
// spectrogram picture of each sound (and of each song with --spectrogram) to look at.
//
//   dotnet run --project tools/MusicRender -- <out dir> [song id, folder or .mml file ...] [--night] [--stems] [--passes N] [--spectrogram]
//   dotnet run --project tools/MusicRender -- <out dir> --sounds [name ...]
//   dotnet run --project tools/MusicRender -- <out dir> --cries [species or form ...] [--all] [--modes]
//   dotnet run --project tools/MusicRender -- <out dir> --ambience [bed ...] [--handheld]
//
// Every report gives the loudness each bus is held to (Audio/Loudness: gated, in dBFS) and flags what falls outside
// its bus's window.
//   dotnet run --project tools/MusicRender -- --calibrate
using System.IO.Compression;
using System.Text;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Data;

if (args.Length < 1)
{
    Console.WriteLine("usage: MusicRender <out dir> [song id or folder ...] [--night] [--stems] [--passes N] [--spectrogram]");
    Console.WriteLine("       MusicRender <out dir> --sounds [name ...]");
    Console.WriteLine("       MusicRender <out dir> --cries [species or form ...] [--all] [--modes]");
    Console.WriteLine("       MusicRender <out dir> --ambience [bed ...] [--handheld]");
    Console.WriteLine("       MusicRender --calibrate");
    return 1;
}

if (args[0] == "--calibrate")
{
    // Each instrument playing a phrase of quarter notes around middle C, to balance their gains
    foreach (var name in InstrumentBank.Names.OrderBy(n => n))
    {
        var inst = InstrumentBank.Get(name)!;
        var synth = new Synthesizer { ReverbLevel = 0f };
        int n = Synthesizer.SampleRate * 4;
        var buf = new float[n * 2];
        int[] keys = inst.IsKit ? new[] { 0, 1, 2, 1 } : new[] { 60, 64, 67, 72 };
        int step = Synthesizer.SampleRate / 2;
        for (int done = 0, k = 0; done < n; done += step, k++)
        {
            synth.NoteOn(inst, keys[k % keys.Length], 100 / 127f, step * 7 / 8, 0f, 0.8f, 0f);
            synth.Render(buf.AsSpan(done * 2, Math.Min(step, n - done) * 2));
        }
        double ss = 0;
        foreach (float x in buf) ss += x * x;
        Console.WriteLine($"{name,-14} rms {Db((float)Math.Sqrt(ss / buf.Length)),6:0.0} dB   gain {inst.Gain}");
    }
    return 0;
}

string outDir = args[0];
bool night = args.Contains("--night");
bool stems = args.Contains("--stems");
bool spectrograms = args.Contains("--spectrogram");
bool lowHp = args.Contains("--lowhp");
int passes = 2;
var filters = new List<string>();
for (int i = 1; i < args.Length; i++)
{
    if (args[i] is "--night" or "--stems" or "--spectrogram" or "--sounds" or "--lowhp" or "--cries" or "--all" or "--modes" or "--ambience" or "--handheld") continue;
    if (args[i] == "--passes") { passes = int.Parse(args[++i]); continue; }
    filters.Add(args[i]);
}

Directory.CreateDirectory(outDir);
bool handheld = args.Contains("--handheld");

// The bus's window for a loudness, or a flag saying how far outside it is
string Target(double loudness, AudioBus bus)
{
    var (low, high) = Loudness.Target(bus);
    return loudness < low ? $"  QUIET by {low - loudness:0.0} dB" : loudness > high ? $"  LOUD by {loudness - high:0.0} dB" : "";
}

// Renders one sound as the mixer plays it on its bus, with a tenth of a second after; prints its numbers and says whether it is sound
bool Check(string label, SoundSample sound, AudioBus bus, string file, bool write)
{
    var mixer = new AudioMixer { Handheld = handheld };
    mixer.PlaySound(sound, bus);
    int frames = sound.Samples.Length + Synthesizer.SampleRate / 10;
    var samples = new float[frames * 2];
    for (int done = 0; done < frames; done += 1024) mixer.Render(samples.AsSpan(done * 2, Math.Min(1024, frames - done) * 2));

    float peak = 0f;
    double sum = 0;
    int nans = 0;
    foreach (float x in samples)
    {
        if (!float.IsFinite(x)) { nans++; continue; }
        peak = MathF.Max(peak, MathF.Abs(x));
        sum += x * x;
    }
    double rms = Math.Sqrt(sum / samples.Length);
    // A click is a jump at either end: a sound has to start and stop at nothing
    float head = MathF.Abs(sound.Samples[0]), tail = MathF.Abs(sound.Samples[^1]);
    float dc = sound.Samples.Average();
    float loudest = 0f;
    if (write)
    {
        WriteWav(file + ".wav", samples);
        loudest = WriteSpectrogram(file + ".png", samples, 640, 256, 8192f);
    }
    bool clicks = head > 0.02f || tail > 0.02f;
    double loud = Loudness.Of(samples, 2);
    Console.WriteLine($"{label,-12} {sound.Duration * 1000,5:0} ms  peak {Db(peak),6:0.0} dB  rms {Db((float)rms),6:0.0} dB  loud {loud,6:0.0} dB  loudest {loudest,5:0} Hz  ends {head:0.000}/{tail:0.000}  dc {dc,6:0.000}{(clicks ? "  CLICKS" : "")}{(nans > 0 ? $"  NaN {nans}" : "")}{Target(loud, bus)}");
    return nans == 0 && peak < 0.999f && !clicks;
}

if (args.Contains("--sounds"))
{
    // Every sound effect (or the ones named)
    bool bad = false;
    foreach (var name in SoundBank.Names.Where(n => filters.Count == 0 || filters.Contains(n, StringComparer.OrdinalIgnoreCase)))
        bad |= !Check(name, SoundBank.Get(name)!, AudioBus.Sound, Path.Combine(outDir, "sound_" + name + (handheld ? "_handheld" : "")), true);
    return bad ? 2 : 0;
}

if (args.Contains("--ambience"))
{
    // Every ambience bed (or the ones named) as the mixer loops it: a second to fade in, then the loop twice; the
    // seam is where the loop starts again, and must be no bigger a step than the bed's own
    bool bad = false;
    foreach (var bed in Enum.GetValues<AmbienceBed>().Where(b => filters.Count == 0 || filters.Contains(b.ToString(), StringComparer.OrdinalIgnoreCase)))
    {
        var sample = Ambience.Get(bed);
        var mixer = new AudioMixer { Handheld = handheld };
        mixer.SetAmbience(new[] { new AmbienceLayer(bed, 1f, 0f) });
        double seconds = AudioMixer.AmbienceFade + sample.Duration * 2;
        int frames = (int)(seconds * Synthesizer.SampleRate);
        var samples = new float[frames * 2];
        for (int done = 0; done < frames; done += 1024) mixer.Render(samples.AsSpan(done * 2, Math.Min(1024, frames - done) * 2));
        int settled = (int)(AudioMixer.AmbienceFade * Synthesizer.SampleRate) + 1024;
        double loud = Loudness.Of(samples.AsSpan(settled * 2), 2);
        float peak = samples.Max(MathF.Abs);
        var data = sample.Samples;
        float seam = MathF.Abs(data[0] - data[^1]), step = 0f;
        for (int i = 1; i < data.Length; i++) step = MathF.Max(step, MathF.Abs(data[i] - data[i - 1]));
        string file = Path.Combine(outDir, "ambience_" + bed.ToString().ToLowerInvariant() + (handheld ? "_handheld" : ""));
        WriteWav(file + ".wav", samples);
        float loudest = WriteSpectrogram(file + ".png", samples, 900, 256, 8192f);
        bool broken = seam > step || peak >= 0.999f || samples.Any(x => !float.IsFinite(x));
        Console.WriteLine($"{bed,-12} {sample.Duration,5:0.0} s loop  peak {Db(peak),6:0.0} dB  loud {loud,6:0.0} dB  loudest {loudest,5:0} Hz  seam {seam:0.000} (largest step {step:0.000}){(broken ? "  BROKEN" : "")}{Target(loud, AudioBus.Ambience)}");
        bad |= broken;
    }
    return bad ? 2 : 0;
}

if (args.Contains("--cries"))
{
    // The cries of the species named (a form by its name: Giratina-Origin), of a few of every size and type, or
    // with --all of every species and form (numbers only); --modes adds each of the original's modes
    bool bad = false;
    bool all = args.Contains("--all"), modes = args.Contains("--modes");
    var names = filters.Count > 0 ? filters
        : all ? PokemonDatabase.GetAll().OrderBy(sp => sp.DexNumber).SelectMany(sp => new[] { sp.Name }.Concat(sp.Forms?.Select(f => f.Name) ?? Enumerable.Empty<string>())).ToList()
        : new List<string> { "Turtwig", "Grotle", "Torterra", "Chimchar", "Infernape", "Piplup", "Empoleon", "Starly", "Bidoof", "Shinx", "Pikachu",
            "Geodude", "Onix", "Zubat", "Gastly", "Magikarp", "Gyarados", "Happiny", "Snorlax", "Wailord", "Unown", "Dialga", "Palkia", "Giratina", "Giratina-Origin", "Arceus" };
    foreach (var name in names)
    {
        var species = PokemonDatabase.Get(name) ?? PokemonDatabase.SpeciesOfForm(name);
        if (species == null)
        {
            Console.WriteLine($"{name}: no such species or form");
            bad = true;
            continue;
        }
        string? form = species.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ? null : name;
        foreach (var mode in modes ? Enum.GetValues<CryMode>() : new[] { CryMode.Normal })
        {
            string label = mode == CryMode.Normal ? name : $"{name} {mode}";
            bad |= !Check(label, Cries.Get(species, form, mode), AudioBus.Cry, Path.Combine(outDir, "cry_" + label.Replace(' ', '_').ToLowerInvariant() + (handheld ? "_handheld" : "")), !all);
        }
    }
    return bad ? 2 : 0;
}

var songs = MusicLibrary.All
    .Where(s => filters.Count == 0 || filters.Any(f => s.Id.Equals(f, StringComparison.OrdinalIgnoreCase) || s.Id.StartsWith(f + "/", StringComparison.OrdinalIgnoreCase)))
    .OrderBy(s => s.Id)
    .ToList();

bool problems = false;

// A song named by its file is read from there, so a song being written is checked without building the game
foreach (var file in filters.Where(f => f.EndsWith(".mml", StringComparison.OrdinalIgnoreCase)))
{
    if (!File.Exists(file))
    {
        Console.WriteLine($"{file}: no such file");
        problems = true;
        continue;
    }
    string full = Path.GetFullPath(file).Replace('\\', '/');
    int at = full.IndexOf("/Data/music/", StringComparison.Ordinal);
    string id = at >= 0 ? Path.ChangeExtension(full[(at + "/Data/music/".Length)..], null) : Path.GetFileNameWithoutExtension(file);
    try
    {
        songs.Add(Mml.Parse(id, File.ReadAllText(file)));
    }
    catch (FormatException e)
    {
        Console.WriteLine($"{id}: {e.Message}");
        problems = true;
    }
}
foreach (var song in songs)
{
    var mixer = new AudioMixer { Volume = 0.8f, LowHp = lowHp, Handheld = handheld };
    double seconds;
    if (song.Loops)
    {
        mixer.Play(song, night);
        // The intro, then the loop enough times to hear the seam, then a fade
        seconds = song.Duration(night) + song.LoopDuration(night) * (passes - 1) + 2.5;
    }
    else
    {
        mixer.PlayFanfare(song);
        seconds = song.Duration(night) + 1.5;
    }

    int frames = (int)(seconds * Synthesizer.SampleRate);
    var samples = new float[frames * 2];
    const int chunk = 1024;
    var clock = System.Diagnostics.Stopwatch.StartNew();
    for (int done = 0; done < frames; done += chunk)
    {
        int n = Math.Min(chunk, frames - done);
        mixer.Render(samples.AsSpan(done * 2, n * 2));
    }
    double cpu = clock.Elapsed.TotalSeconds / seconds * 100;
    if (song.Loops)
    {
        int fade = (int)(2.5 * Synthesizer.SampleRate);
        for (int i = 0; i < fade; i++)
        {
            float g = 1f - i / (float)fade;
            samples[(frames - fade + i) * 2] *= g;
            samples[(frames - fade + i) * 2 + 1] *= g;
        }
    }

    float peak = 0f;
    double sum = 0;
    int nans = 0;
    foreach (float s in samples)
    {
        if (float.IsNaN(s) || float.IsInfinity(s)) { nans++; continue; }
        peak = MathF.Max(peak, MathF.Abs(s));
        sum += s * s;
    }
    double rms = Math.Sqrt(sum / samples.Length);
    string name = song.Id.Replace('/', '_') + (night ? "_night" : "") + (lowHp ? "_lowhp" : "") + (handheld ? "_handheld" : "");
    WriteWav(Path.Combine(outDir, name + ".wav"), samples);
    if (spectrograms) WriteSpectrogram(Path.Combine(outDir, name + ".png"), samples, 1200, 256, 8192f);

    var clashes = Clashes(song, lowHp);
    // Loudness is measured before the fade, as the game plays it
    var bus = song.Loops ? AudioBus.Music : AudioBus.Fanfare;
    double loud = Loudness.Of(samples.AsSpan(0, (song.Loops ? frames - (int)(2.5 * Synthesizer.SampleRate) : frames) * 2), 2);
    Console.WriteLine($"{song.Id,-24} {song.Duration(night),6:0.0}s loop {song.LoopDuration(night),5:0.0}s  peak {Db(peak),6:0.0} dB  rms {Db((float)rms),6:0.0} dB  loud {loud,6:0.0} dB  clashes {clashes.Count}  cpu {cpu:0.0}%{(nans > 0 ? $"  NaN {nans}" : "")}{Target(loud, bus)}");
    foreach (var c in clashes.Take(12)) Console.WriteLine("    " + c);
    if (stems)
    {
        foreach (var track in song.Tracks)
        {
            var solo = new Song
            {
                Id = song.Id, Tempo = song.Tempo, NightTempo = song.NightTempo, TicksPerBar = song.TicksPerBar,
                LoopTick = song.LoopTick, EndTick = song.EndTick, Reverb = song.Reverb
            };
            solo.Tracks.Add(track);
            var player = new SongPlayer();
            player.Start(solo, night);
            int n = (int)(song.Duration(night) * Synthesizer.SampleRate);
            var buf = new float[n * 2];
            for (int done = 0; done < n; done += 1024) player.Render(buf.AsSpan(done * 2, Math.Min(1024, n - done) * 2), 0.8f);
            double ss = 0;
            foreach (float x in buf) ss += x * x;
            Console.WriteLine($"    {track.Name,-12} rms {Db((float)Math.Sqrt(ss / buf.Length)),6:0.0} dB");
        }
    }
    if (nans > 0 || peak >= 0.999f) problems = true;
}
return problems ? 2 : 0;

static double Db(float x) => x <= 0 ? -120 : 20 * Math.Log10(x);

// Two pitched parts a minor second (or minor ninth) apart, where one of them starts on a beat and lasts at least an eighth
// Two parts a minor second or ninth apart on a beat, in the arrangement rendered: the low-HP one's tracks
// (only=lowhp) or the normal one's (unless=lowhp), never both
static List<string> Clashes(Song song, bool lowHp)
{
    var result = new List<string>();
    var pitched = song.Tracks.Where(t => !t.IsDrums && t.When != (lowHp ? TrackWhen.NotLowHp : TrackWhen.LowHp)).ToList();
    int beat = Song.TicksPerQuarter;
    bool Checked(NoteEvent n) => n.Tick % beat == 0 && n.Length >= Song.TicksPerWhole / 8;
    foreach (var a in pitched)
    {
        foreach (var na in a.Notes)
        {
            if (!Checked(na)) continue;
            foreach (var b in pitched)
            {
                if (ReferenceEquals(a, b)) continue;
                foreach (var nb in b.Notes)
                {
                    if (nb.Tick > na.Tick) break;
                    if (nb.Tick + nb.Gate <= na.Tick) continue;
                    // Two notes struck together are told once, unless the other is too short to be checked itself (an alarm's pip)
                    if (nb.Tick == na.Tick && Checked(nb) && string.CompareOrdinal(a.Name, b.Name) > 0) continue;
                    int ka = na.Key + 12 * a.Instrument.OctaveShift, kb = nb.Key + 12 * b.Instrument.OctaveShift;
                    int iv = Math.Abs(ka - kb);
                    if (iv % 12 == 1 && iv < 24)
                    {
                        int bar = na.Tick / song.TicksPerBar + 1;
                        int bt = na.Tick % song.TicksPerBar / beat + 1;
                        result.Add($"bar {bar} beat {bt}: {a.Name} {NoteName(ka)} vs {b.Name} {NoteName(kb)}");
                    }
                }
            }
        }
    }
    return result;
}

static string NoteName(int key)
{
    string[] names = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
    return names[((key % 12) + 12) % 12] + (key / 12 - 1);
}

static void WriteWav(string path, float[] samples)
{
    using var w = new BinaryWriter(File.Create(path));
    int dataBytes = samples.Length * 2;
    w.Write(Encoding.ASCII.GetBytes("RIFF"));
    w.Write(36 + dataBytes);
    w.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
    w.Write(16);
    w.Write((short)1);
    w.Write((short)2);
    w.Write(Synthesizer.SampleRate);
    w.Write(Synthesizer.SampleRate * 4);
    w.Write((short)4);
    w.Write((short)16);
    w.Write(Encoding.ASCII.GetBytes("data"));
    w.Write(dataBytes);
    foreach (float s in samples) w.Write((short)(Math.Clamp(float.IsFinite(s) ? s : 0f, -1f, 1f) * 32767f));
}

// A spectrogram of interleaved stereo (mixed to mono): time left to right, 0 Hz at the bottom and maxHz at the top,
// black through blue, magenta and orange to white over 80 dB. Returns the frequency with the most energy overall.
static float WriteSpectrogram(string path, float[] stereo, int width, int height, float maxHz)
{
    const int window = 1024;
    int n = stereo.Length / 2;
    var mono = new float[Math.Max(n, window)];
    for (int i = 0; i < n; i++) mono[i] = (stereo[i * 2] + stereo[i * 2 + 1]) * 0.5f;

    int bins = Math.Min(window / 2, (int)(maxHz * window / Synthesizer.SampleRate));
    int hop = Math.Max(1, (mono.Length - window) / width);
    var hann = new float[window];
    for (int i = 0; i < window; i++) hann[i] = 0.5f - 0.5f * MathF.Cos(MathF.Tau * i / window);
    var cos = new float[window];
    var sin = new float[window];
    for (int i = 0; i < window; i++) { cos[i] = MathF.Cos(MathF.Tau * i / window); sin[i] = MathF.Sin(MathF.Tau * i / window); }

    var frame = new float[window];
    var energy = new double[bins];
    var pixels = new byte[width * height * 3];
    for (int c = 0; c < width; c++)
    {
        int start = Math.Min(c * hop, mono.Length - window);
        for (int i = 0; i < window; i++) frame[i] = mono[start + i] * hann[i];
        for (int k = 0; k < bins; k++)
        {
            float re = 0f, im = 0f;
            for (int i = 0; i < window; i++)
            {
                int t = (int)((long)k * i % window);
                re += frame[i] * cos[t];
                im -= frame[i] * sin[t];
            }
            // A full-scale sine under a Hann window comes to window / 4
            float mag = MathF.Sqrt(re * re + im * im) / (window / 4f);
            energy[k] += mag;
            float db = 20f * MathF.Log10(mag + 1e-6f);
            float t01 = Math.Clamp((db + 80f) / 80f, 0f, 1f);
            var (r, g, b) = Heat(t01);
            int y = height - 1 - (int)((long)k * height / bins);
            for (int yy = y; yy > y - Math.Max(1, height / bins); yy--)
            {
                if (yy < 0) break;
                int p = (yy * width + c) * 3;
                pixels[p] = r; pixels[p + 1] = g; pixels[p + 2] = b;
            }
        }
    }
    WritePng(path, width, height, pixels);

    int loudest = 0;
    for (int k = 1; k < bins; k++) if (energy[k] > energy[loudest]) loudest = k;
    return loudest * Synthesizer.SampleRate / (float)window;
}

static (byte, byte, byte) Heat(float t)
{
    // black → blue → magenta → orange → white
    (float r, float g, float b) = t < 0.25f ? (0f, 0f, t * 2.4f)
        : t < 0.5f ? ((t - 0.25f) * 3.2f, 0f, 0.6f + (t - 0.25f) * 1.2f)
        : t < 0.75f ? (0.8f + (t - 0.5f) * 0.8f, (t - 0.5f) * 2f, 0.9f - (t - 0.5f) * 3.4f)
        : (1f, 0.5f + (t - 0.75f) * 2f, (t - 0.75f) * 3.6f);
    return ((byte)(Math.Clamp(r, 0f, 1f) * 255), (byte)(Math.Clamp(g, 0f, 1f) * 255), (byte)(Math.Clamp(b, 0f, 1f) * 255));
}

// An RGB PNG written by hand: one IHDR, one IDAT (zlib), IEND
static void WritePng(string path, int width, int height, byte[] rgb)
{
    var raw = new byte[height * (width * 3 + 1)];
    for (int y = 0; y < height; y++)
    {
        raw[y * (width * 3 + 1)] = 0;
        Buffer.BlockCopy(rgb, y * width * 3, raw, y * (width * 3 + 1) + 1, width * 3);
    }
    using var zipped = new MemoryStream();
    using (var z = new ZLibStream(zipped, CompressionLevel.Optimal, leaveOpen: true)) z.Write(raw);

    using var file = new BinaryWriter(File.Create(path));
    file.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
    var header = new byte[13];
    BigEndian(header, 0, width);
    BigEndian(header, 4, height);
    header[8] = 8; header[9] = 2; header[10] = 0; header[11] = 0; header[12] = 0;
    Chunk(file, "IHDR", header);
    Chunk(file, "IDAT", zipped.ToArray());
    Chunk(file, "IEND", Array.Empty<byte>());

    static void BigEndian(byte[] b, int at, int value)
    {
        b[at] = (byte)(value >> 24); b[at + 1] = (byte)(value >> 16); b[at + 2] = (byte)(value >> 8); b[at + 3] = (byte)value;
    }

    static void Chunk(BinaryWriter w, string type, byte[] data)
    {
        var length = new byte[4];
        BigEndian(length, 0, data.Length);
        w.Write(length);
        var typeBytes = Encoding.ASCII.GetBytes(type);
        w.Write(typeBytes);
        w.Write(data);
        uint crc = Crc32(data, Crc32(typeBytes, 0xFFFFFFFFu)) ^ 0xFFFFFFFFu;
        var crcBytes = new byte[4];
        BigEndian(crcBytes, 0, (int)crc);
        w.Write(crcBytes);
    }

    static uint Crc32(byte[] data, uint c)
    {
        // One CRC over the type and then the data, so the type's pass is carried on by the data's
        foreach (byte b in data)
        {
            c ^= b;
            for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
        }
        return c;
    }
}
