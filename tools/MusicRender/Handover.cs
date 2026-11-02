using System;
using System.Collections.Generic;
using System.IO;
using PokemonPlatinumEngine.Audio;

/// <summary>
/// Plan 31 · MU2: songs played through the game's mixer as the game changes them, for the ear and for the numbers.
/// <code>
/// MusicRender &lt;out dir&gt; --handover &lt;first&gt; &lt;second&gt; [--at 20] [--cut] [--seconds 40] [--night]
/// MusicRender &lt;out dir&gt; --switch &lt;song&gt; [--lowhp 20-35] [--night-at 45] [--giant 20-35] [--seconds 60]
/// </code>
/// A song is named by its id (<c>sinnoh/route201</c>) or its <c>.mml</c> file, with <c>@modern</c> or <c>@ds</c> after
/// it to play it in that sound whatever its header and region say (a <c>sound</c> line read before its own). The
/// handover asks for the second song at <c>--at</c> seconds, faded across as a new area's theme is, or cut in as a
/// battle's with <c>--cut</c>; the switch turns the low-HP arrangement or the giant layer on and off at those seconds and
/// asks for the night arrangement at <c>--night-at</c>. Each prints the loudness on either side of every change, the peak,
/// and the largest step between two samples at each change against the largest anywhere else, so a change that clicks
/// shows; it ends with 2 on a click, a clip or a sample that isn't a number.
/// </summary>
internal static class Handover
{
    public static int Run(string outDir, string[] args, Action<string, float[], int> writeWav)
    {
        Directory.CreateDirectory(outDir);
        string mode = args[0];
        var named = new List<string>();
        double at = 20, seconds = mode == "--handover" ? 40 : 60, nightAt = -1;
        (double On, double Off)? lowHp = null, giant = null;
        bool cut = false, night = false;
        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--at": at = double.Parse(args[++i]); break;
                case "--seconds": seconds = double.Parse(args[++i]); break;
                case "--cut": cut = true; break;
                case "--night": night = true; break;
                case "--night-at": nightAt = double.Parse(args[++i]); break;
                case "--lowhp": lowHp = Span(args[++i]); break;
                case "--giant": giant = Span(args[++i]); break;
                default: named.Add(args[i]); break;
            }
        }

        var mixer = new AudioMixer { Volume = 0.8f };
        int rate = AudioMixer.Rate;
        int frames = (int)(seconds * rate);
        var samples = new float[frames * 2];
        // The moments the mix is changed at, in frames, and what is done then
        var changes = new List<(int Frame, string What, Action Do)>();
        string name;
        if (mode == "--handover")
        {
            if (named.Count != 2) throw new ArgumentException("--handover takes two songs");
            var first = Load(named[0]);
            var second = Load(named[1]);
            mixer.Play(first, night);
            changes.Add(((int)(at * rate), $"{second.Id} in the {second.PlaysIn} sound {(cut ? "cut in" : "faded across")}", () => mixer.Play(second, night, immediate: cut)));
            name = $"handover_{Short(first)}_to_{Short(second)}{(cut ? "_cut" : "")}{(night ? "_night" : "")}";
            Console.WriteLine($"{first.Id} in the {first.PlaysIn} sound, then {second.Id} in the {second.PlaysIn} sound at {at:0.0} s");
        }
        else
        {
            if (named.Count != 1) throw new ArgumentException("--switch takes one song");
            var song = Load(named[0]);
            mixer.Play(song, night);
            if (lowHp is { } l)
            {
                changes.Add(((int)(l.On * rate), "the low-HP arrangement on", () => mixer.LowHp = true));
                changes.Add(((int)(l.Off * rate), "the low-HP arrangement off", () => mixer.LowHp = false));
            }
            if (giant is { } g)
            {
                changes.Add(((int)(g.On * rate), "the giant layer on", () => mixer.Giant = true));
                changes.Add(((int)(g.Off * rate), "the giant layer off", () => mixer.Giant = false));
            }
            if (nightAt >= 0) changes.Add(((int)(nightAt * rate), "the night arrangement asked for", () => mixer.Play(song, true)));
            name = $"switch_{Short(song)}";
            Console.WriteLine($"{song.Id} in the {song.PlaysIn} sound");
        }
        changes.Sort((a, b) => a.Frame.CompareTo(b.Frame));

        // Rendered in the audio thread's own blocks, each change made between two
        const int block = 1536;
        int next = 0;
        for (int done = 0; done < frames; done += block)
        {
            while (next < changes.Count && changes[next].Frame <= done) changes[next++].Do();
            mixer.Render(samples.AsSpan(done * 2, Math.Min(block, frames - done) * 2));
        }

        // A fade at the end, for listening
        int fade = Math.Min(frames, 2 * rate);
        for (int i = 0; i < fade; i++)
        {
            float k = 1f - i / (float)fade;
            samples[(frames - fade + i) * 2] *= k;
            samples[(frames - fade + i) * 2 + 1] *= k;
        }

        bool problems = false;
        float peak = 0f;
        int bad = 0;
        foreach (float s in samples)
        {
            if (!float.IsFinite(s)) { bad++; continue; }
            peak = MathF.Max(peak, MathF.Abs(s));
        }
        // The largest step anywhere but within 50 ms of a change: what a change's own step is held against
        var near = new bool[frames];
        foreach (var c in changes)
            for (int i = Math.Max(0, c.Frame - rate / 20); i < Math.Min(frames, c.Frame + rate / 2); i++) near[i] = true;
        float elsewhere = 0f;
        for (int i = 1; i < frames - fade; i++)
            if (!near[i])
                for (int ch = 0; ch < 2; ch++) elsewhere = MathF.Max(elsewhere, MathF.Abs(samples[i * 2 + ch] - samples[(i - 1) * 2 + ch]));

        int from = 0;
        foreach (var c in changes)
        {
            float step = 0f;
            for (int i = Math.Max(1, c.Frame - rate / 20); i < Math.Min(frames, c.Frame + rate / 2); i++)
                for (int ch = 0; ch < 2; ch++) step = MathF.Max(step, MathF.Abs(samples[i * 2 + ch] - samples[(i - 1) * 2 + ch]));
            double before = Loudness.Of(samples.AsSpan(from * 2, (c.Frame - from) * 2), 2, rate);
            bool clicks = step > elsewhere * 1.5f + 0.02f;
            if (clicks) problems = true;
            Console.WriteLine($"  {c.Frame / (double)rate,6:0.0} s  {c.What}: loud {before:0.0} dB before; largest step {step:0.000} (elsewhere {elsewhere:0.000}){(clicks ? "  CLICKS" : "")}");
            from = c.Frame;
        }
        double after = Loudness.Of(samples.AsSpan(from * 2, (frames - fade - from) * 2), 2, rate);
        Console.WriteLine($"  loud {after:0.0} dB after the last change; peak {20 * Math.Log10(Math.Max(peak, 1e-9)):0.0} dB{(bad > 0 ? $"  not a number {bad}" : "")}");
        if (bad > 0 || peak >= 0.999f) problems = true;

        string path = Path.Combine(outDir, name + ".wav");
        writeWav(path, samples, rate);
        Console.WriteLine($"  {path}");
        return problems ? 2 : 0;
    }

    private static (double, double) Span(string text)
    {
        var parts = text.Split('-');
        return (double.Parse(parts[0]), double.Parse(parts[1]));
    }

    private static string Short(Song song) => song.Id.Replace('/', '_') + "_" + song.PlaysIn.ToString().ToLowerInvariant();

    /// <summary>A song by id or file, perhaps with <c>@modern</c> or <c>@ds</c> to play it in that sound.</summary>
    private static Song Load(string spec)
    {
        string? sound = null;
        int at = spec.LastIndexOf('@');
        if (at > 0)
        {
            sound = spec[(at + 1)..];
            spec = spec[..at];
            if (sound is not ("modern" or "ds")) throw new ArgumentException($"{spec}: @{sound} is not a sound (modern or ds)");
        }
        string id, text;
        if (spec.EndsWith(".mml", StringComparison.OrdinalIgnoreCase))
        {
            id = Path.GetFileNameWithoutExtension(spec);
            text = File.ReadAllText(spec);
        }
        else
        {
            var song = MusicLibrary.Get(spec) ?? throw new ArgumentException($"no song '{spec}'");
            if (sound == null) return song;
            id = song.Id;
            text = File.ReadAllText(Path.Combine(MusicLibrary.Directory, id + ".mml"));
        }
        // A header line read before the song's own: the song's own sound, if it names one, comes after and wins
        return Mml.Parse(id, sound == null ? text : $"sound {sound}\n" + text);
    }
}
