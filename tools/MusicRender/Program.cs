// Renders the game's songs to WAV files, the way the game plays them, and checks them without ears:
// levels, clipping, NaNs, and notes that clash (a minor second between two parts on a beat).
//
//   dotnet run --project tools/MusicRender -- <out dir> [song id or folder ...] [--night] [--passes N]
using System.Text;
using PokemonPlatinumEngine.Audio;

if (args.Length < 1)
{
    Console.WriteLine("usage: MusicRender <out dir> [song id or folder ...] [--night] [--passes N]");
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
int passes = 2;
var filters = new List<string>();
for (int i = 1; i < args.Length; i++)
{
    if (args[i] == "--night" || args[i] == "--stems") continue;
    if (args[i] == "--passes") { passes = int.Parse(args[++i]); continue; }
    filters.Add(args[i]);
}

Directory.CreateDirectory(outDir);
var songs = MusicLibrary.All
    .Where(s => filters.Count == 0 || filters.Any(f => s.Id.Equals(f, StringComparison.OrdinalIgnoreCase) || s.Id.StartsWith(f + "/", StringComparison.OrdinalIgnoreCase)))
    .OrderBy(s => s.Id)
    .ToList();

bool problems = false;
foreach (var song in songs)
{
    var mixer = new MusicMixer { Volume = 0.8f };
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
    string name = song.Id.Replace('/', '_') + (night ? "_night" : "") + ".wav";
    WriteWav(Path.Combine(outDir, name), samples);

    var clashes = Clashes(song);
    Console.WriteLine($"{song.Id,-24} {song.Duration(night),6:0.0}s loop {song.LoopDuration(night),5:0.0}s  peak {Db(peak),6:0.0} dB  rms {Db((float)rms),6:0.0} dB  clashes {clashes.Count}  cpu {cpu:0.0}%{(nans > 0 ? $"  NaN {nans}" : "")}");
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
static List<string> Clashes(Song song)
{
    var result = new List<string>();
    var pitched = song.Tracks.Where(t => !t.IsDrums).ToList();
    int beat = Song.TicksPerQuarter;
    foreach (var a in pitched)
    {
        foreach (var na in a.Notes)
        {
            if (na.Tick % beat != 0 || na.Length < Song.TicksPerWhole / 8) continue;
            foreach (var b in pitched)
            {
                if (ReferenceEquals(a, b)) continue;
                foreach (var nb in b.Notes)
                {
                    if (nb.Tick > na.Tick) break;
                    if (nb.Tick + nb.Gate <= na.Tick) continue;
                    if (nb.Tick == na.Tick && string.CompareOrdinal(a.Name, b.Name) > 0) continue;
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
