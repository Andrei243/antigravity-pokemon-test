using System.Diagnostics;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Data;

/// <summary>
/// Plan 31 · MU1: <c>MusicRender &lt;out dir&gt; --mix</c>. Measures the resampler that takes the DS sound into the
/// mix's 48 kHz (its response, its images and its THD+N on sines across the band, for the mixer's filter and a few
/// others), what the audio thread spends (the resampler alone, the mixer before and after it on a busy scene, and the
/// modern synthesiser's voices for decision 2), and writes each bus's before (the mixer at the DS's rate, as it was)
/// and after (the mix at 48 kHz) as WAVs, with a null test of each against the before taken to 48 kHz by a far longer
/// filter.
/// </summary>
static class MixCheck
{
    public static int Run(string outDir, Action<string, float[], int> writeWav)
    {
        Console.WriteLine("== The resampler: 32,768 Hz into 48,000 Hz, a sine of −6 dBFS at each frequency");
        Console.WriteLine("   response: the tone's level out over in; THD+N: everything but the tone; image: the tone's image");
        var configs = new (string Name, int Taps, float Beta, float Cutoff)[]
        {
            ("linear-ish 8", 8, 4f, 15_000f),
            ("32 taps", 32, 8f, 15_000f),
            ("48 taps", 48, 9f, 15_500f),
            ("64 taps", 64, 9.6f, 15_900f),
            ("96 taps", 96, 10f, 16_100f),
            ($"mixer: {Resampler.MixTaps} taps", Resampler.MixTaps, Resampler.MixBeta, Resampler.MixCutoff),
            ("192 taps", 192, 12f, 16_250f),
        };
        double[] freqs = { 20, 100, 440, 1000, 2000, 4000, 6000, 8000, 10000, 12000, 13000, 14000, 14500, 15000, 15500, 16000 };
        CostOf(Resampler.ForMix()); // the JIT's first pass, not counted
        foreach (var c in configs)
        {
            Console.WriteLine($"-- {c.Name} (β {c.Beta}, cutoff {c.Cutoff:0} Hz)");
            double worstRipple14 = 0, worstImage = -999, worstThd = -999;
            foreach (double f in freqs)
            {
                var (resp, thd, image) = Tone(new Resampler(Synthesizer.DsRate, AudioMixer.Rate, c.Taps, c.Beta, c.Cutoff), f);
                if (f <= 14000) { worstRipple14 = Math.Max(worstRipple14, Math.Abs(resp)); worstImage = Math.Max(worstImage, image); worstThd = Math.Max(worstThd, thd); }
                if (c.Name.StartsWith("mixer") || f >= 14500) Console.WriteLine($"   {f,6:0} Hz  response {resp,8:0.000} dB  THD+N {thd,7:0.0} dB  image {image,7:0.0} dB");
            }
            Console.WriteLine($"   to 14 kHz: flat within ±{worstRipple14:0.0000} dB, worst THD+N {worstThd:0.0} dB, worst image {worstImage:0.0} dB");
            Console.WriteLine($"   cost: {CostOf(new Resampler(Synthesizer.DsRate, AudioMixer.Rate, c.Taps, c.Beta, c.Cutoff)):0.00} ms of CPU a second of stereo out");
        }

        Console.WriteLine();
        Console.WriteLine("== The audio thread: a busy scene (a battle theme in the red, eight sounds, two cries, six beds), 30 s, best of 3");
        double before = Best(() => Busy(new AudioMixer(Synthesizer.DsRate), 30));
        double after = Best(() => Busy(new AudioMixer(), 30));
        Console.WriteLine($"   before (DS rate, no resampling): {before * 1000 / 30:0.0} ms of CPU a second, {before / 30 * 100:0.00}% of a core");
        Console.WriteLine($"   after (48 kHz, resampled):       {after * 1000 / 30:0.0} ms of CPU a second, {after / 30 * 100:0.00}% of a core");
        Console.WriteLine($"   the resampler's share: {(after - before) * 1000 / 30:0.0} ms a second");

        Console.WriteLine();
        Console.WriteLine("== Voices (decision 2): the modern synthesiser at 48 kHz, every voice sounding, 10 s, best of 3");
        foreach (var inst in new[] { "strings", "warmpad", "ukulele", "steeldrum", "flute" })
        {
            var line = new List<string>();
            double perVoice = 0;
            foreach (int voices in new[] { 24, 48, 96 })
            {
                double s = Best(() => Voices(SynthSound.Modern, inst, voices, 10));
                line.Add($"{voices} voices {s / 10 * 100,5:0.0}%");
                if (voices == 96) perVoice = s / 10 / 96;
            }
            Console.WriteLine($"   {inst,-10} {string.Join("  ", line)}   per voice {perVoice * 100:0.000}% of a core: {0.5 / perVoice:0} voices in half a core");
        }
        double ds40 = Best(() => Voices(SynthSound.Ds, "strings", 40, 10));
        Console.WriteLine($"   DS strings, 40 voices at 32,768 Hz: {ds40 / 10 * 100:0.0}% of a core");
        var tune = Mml.Parse("mu0_island_route", File.ReadAllText(Path.Combine("tools", "MusicRender", "songs", "mu0_island_route.mml")));
        foreach (var (night, low) in new[] { (false, false), (true, false), (false, true) })
        {
            var clock = Stopwatch.StartNew();
            OfflineMaster.Render(tune, SynthSound.Modern, 30, out var player, night, low);
            Console.WriteLine($"   MU0's tune in the modern sound{(night ? " at night" : low ? " in the red" : "")}: {clock.Elapsed.TotalSeconds / 30 * 100:0.0}% of a core, voices at once {player.PeakVoices}");
        }

        Console.WriteLine();
        Console.WriteLine("== The null test: each item alone, from the start, through the mixer before (DS rate) and after (48 kHz)");
        Console.WriteLine("   stage: the after against the before taken to 48 kHz by the mixer's own filter (what the last stage at the new rate changes)");
        Console.WriteLine("   ideal: the after against the before taken to 48 kHz by a 1,024-tap filter, the whole band and under 15 kHz");
        Console.WriteLine("   all in dB under the before's own power");
        foreach (var (name, item) in Items())
        {
            var oldMix = item(new AudioMixer(Synthesizer.DsRate), Synthesizer.DsRate);
            var newMix = item(new AudioMixer(), AudioMixer.Rate);
            var ideal = Resample(oldMix, new Resampler(Synthesizer.DsRate, AudioMixer.Rate, 1024, 14f, 16_300f));
            var same = Resample(oldMix, Resampler.ForMix());
            int n = Math.Min(ideal.Length, newMix.Length);
            double level = Loudness.Of(oldMix, 2, Synthesizer.DsRate), levelNew = Loudness.Of(newMix, 2, AudioMixer.Rate);
            Console.WriteLine($"   {name,-22} level {level,6:0.00} → {levelNew,6:0.00} dB;  stage {Null(same, newMix, n, 0),6:0.0};  ideal {Null(ideal, newMix, n, 0),6:0.0}, under 15 kHz {Null(ideal, newMix, n, 15_000),6:0.0}");
        }

        Console.WriteLine();
        Console.WriteLine("== Each bus before and after, to listen to");
        foreach (var (name, scene) in Scenes())
        {
            var oldMix = scene(new AudioMixer(Synthesizer.DsRate), Synthesizer.DsRate);
            var newMix = scene(new AudioMixer(), AudioMixer.Rate);
            double level = Loudness.Of(oldMix, 2, Synthesizer.DsRate), levelNew = Loudness.Of(newMix, 2, AudioMixer.Rate);
            Console.WriteLine($"   {name,-10} {oldMix.Length / 2.0 / Synthesizer.DsRate,5:0.0} s, level before {level,6:0.00} dB, after {levelNew,6:0.00} dB");
            writeWav(Path.Combine(outDir, $"{name}_before_32768.wav"), oldMix, Synthesizer.DsRate);
            // The before at 48 kHz too, by the 1,024-tap filter, so the two are encoded to MP3 at one rate (an MP3 has no
            // 32,768 Hz, and an encoder's own resampling would change the before alone)
            writeWav(Path.Combine(outDir, $"{name}_before_48000.wav"), Resample(oldMix, new Resampler(Synthesizer.DsRate, AudioMixer.Rate, 1024, 14f, 16_300f)), AudioMixer.Rate);
            writeWav(Path.Combine(outDir, $"{name}_after_48000.wav"), newMix, AudioMixer.Rate);
        }
        return 0;
    }

    /// <summary>Each item by itself on a fresh mixer from its first sample, so the two paths start it on the same sample.</summary>
    private static IEnumerable<(string, Func<AudioMixer, int, float[]>)> Items()
    {
        foreach (var (id, night, low) in new[] { ("sinnoh/twinleaf", false, false), ("sinnoh/twinleaf", true, false), ("common/battle_wild", false, true), ("common/legendary/dialga", false, false) })
            yield return ($"{id.Split('/')[^1]}{(night ? " night" : low ? " lowhp" : "")}", (m, rate) =>
            {
                m.LowHp = low;
                m.Play(MusicLibrary.Get(id)!, night, immediate: true);
                return Render(m, 15, rate);
            });
        yield return ("fanfare_heal", (m, rate) => { m.PlayFanfare(MusicLibrary.Get("common/fanfare_heal")!); return Render(m, 4, rate); });
        foreach (var s in new[] { "select", "hit_super", "ball_throw", "exp_gain" })
            if (SoundBank.Get(s) is { } sound)
                yield return ($"sound {s}", (m, rate) => { m.PlaySound(sound, AudioBus.Sound); return Render(m, sound.Duration + 0.2, rate); });
        foreach (var c in new[] { "Piplup", "Gyarados", "Giratina" })
            yield return ($"cry {c}", (m, rate) => { m.PlaySound(Cries.Get(PokemonDatabase.Get(c)!), AudioBus.Cry); return Render(m, 1.6, rate); });
        foreach (var bed in new[] { AmbienceBed.Rain, AmbienceBed.Waves, AmbienceBed.Cave })
            yield return ($"bed {bed}", (m, rate) => { m.SetAmbience(new[] { new AmbienceLayer(bed, 1f, 0f) }); return Render(m, 5, rate); });
    }

    /// <summary>A sine at <paramref name="f"/> through the resampler: its level out over in, everything but it, and its image, each in dB.</summary>
    public static (double Response, double ThdN, double Image) Tone(Resampler r, double f)
    {
        int seconds = 1;
        var input = new float[Synthesizer.DsRate * seconds * 2];
        for (int i = 0; i < input.Length / 2; i++)
        {
            float x = (float)(0.5 * Math.Sin(2 * Math.PI * f * i / Synthesizer.DsRate));
            input[i * 2] = x;
            input[i * 2 + 1] = x;
        }
        var output = Resample(input, r);
        // The middle of it, away from where the input starts and stops
        int from = AudioMixer.Rate / 5, to = AudioMixer.Rate * 4 / 5;
        var mono = new double[to - from];
        for (int i = 0; i < mono.Length; i++) mono[i] = output[(from + i) * 2];
        var (a, fit) = Fit(mono, f, from);
        double resid = 0, power = 0;
        for (int i = 0; i < mono.Length; i++) { double d = mono[i] - fit[i]; resid += d * d; power += fit[i] * fit[i]; }
        double imageF = Synthesizer.DsRate - f;
        if (imageF > AudioMixer.Rate / 2.0) imageF = AudioMixer.Rate - imageF;
        var residual = new double[mono.Length];
        for (int i = 0; i < mono.Length; i++) residual[i] = mono[i] - fit[i];
        var (ai, _) = Fit(residual, imageF, from);
        return (20 * Math.Log10(a / 0.5), 10 * Math.Log10(Math.Max(resid, 1e-30) / power), 20 * Math.Log10(Math.Max(ai, 1e-15) / a));
    }

    /// <summary>The least-squares sine at <paramref name="f"/> (48 kHz samples, the first at <paramref name="offset"/>): its amplitude and the fit.</summary>
    private static (double Amplitude, double[] Fit) Fit(double[] x, double f, int offset)
    {
        double ss = 0, cc = 0, sc = 0, xs = 0, xc = 0;
        for (int i = 0; i < x.Length; i++)
        {
            double t = 2 * Math.PI * f * (i + offset) / AudioMixer.Rate, s = Math.Sin(t), c = Math.Cos(t);
            ss += s * s; cc += c * c; sc += s * c; xs += x[i] * s; xc += x[i] * c;
        }
        double det = ss * cc - sc * sc;
        double a = (xs * cc - xc * sc) / det, b = (xc * ss - xs * sc) / det;
        var fit = new double[x.Length];
        for (int i = 0; i < x.Length; i++)
        {
            double t = 2 * Math.PI * f * (i + offset) / AudioMixer.Rate;
            fit[i] = a * Math.Sin(t) + b * Math.Cos(t);
        }
        return (Math.Sqrt(a * a + b * b), fit);
    }

    /// <summary>Interleaved stereo at the DS's rate taken whole to 48 kHz, pulled in the audio thread's blocks.</summary>
    public static float[] Resample(float[] stereo, Resampler r)
    {
        int outFrames = (int)((long)(stereo.Length / 2) * r.To / r.From);
        var output = new float[outFrames * 2];
        int pushed = 0;
        for (int done = 0; done < outFrames; done += 1536)
        {
            int n = Math.Min(1536, outFrames - done);
            int need = r.InputFor(n);
            int have = Math.Min(need, stereo.Length / 2 - pushed);
            r.Push(stereo.AsSpan(pushed * 2, have * 2));
            pushed += have;
            if (have < need) r.Push(new float[(need - have) * 2]);
            r.Pull(output.AsSpan(done * 2, n * 2));
        }
        return output;
    }

    /// <summary>The power of <paramref name="test"/> minus <paramref name="reference"/> under the reference's, both kept under <paramref name="under"/> Hz first when it isn't nought.</summary>
    private static double Null(float[] reference, float[] test, int n, double under)
    {
        var r = new double[n];
        var d = new double[n];
        for (int i = 0; i < n; i++) { r[i] = reference[i]; d[i] = test[i] - (double)reference[i]; }
        if (under > 0) { r = LowPass(r, under); d = LowPass(d, under); }
        double signal = 0, diff = 0;
        for (int i = 0; i < n; i++) { signal += r[i] * r[i]; diff += d[i] * d[i]; }
        return 10 * Math.Log10(Math.Max(diff, 1e-30) / signal);
    }

    /// <summary>Interleaved stereo at 48 kHz through a 511-tap Kaiser-windowed sinc low-pass at <paramref name="cutoff"/> Hz (zero phase).</summary>
    private static double[] LowPass(double[] stereo, double cutoff)
    {
        const int taps = 511, half = taps / 2;
        double fc = cutoff / (AudioMixer.Rate / 2.0), beta = 12, i0 = I0(beta), sum = 0;
        var h = new double[taps];
        for (int j = 0; j < taps; j++)
        {
            double t = j - half, x = fc * t, w = t / (half + 1);
            h[j] = fc * (x == 0 ? 1 : Math.Sin(Math.PI * x) / (Math.PI * x)) * I0(beta * Math.Sqrt(1 - w * w)) / i0;
            sum += h[j];
        }
        int frames = stereo.Length / 2;
        var output = new double[stereo.Length];
        for (int i = 0; i < frames; i++)
            for (int ch = 0; ch < 2; ch++)
            {
                double acc = 0;
                for (int j = 0; j < taps; j++)
                {
                    int k = i + j - half;
                    if (k >= 0 && k < frames) acc += h[j] * stereo[k * 2 + ch];
                }
                output[i * 2 + ch] = acc / sum;
            }
        return output;

        static double I0(double x)
        {
            double s = 1, term = 1, q = x * x / 4;
            for (int k = 1; k < 64; k++) { term *= q / (k * k); s += term; }
            return s;
        }
    }

    private static double CostOf(Resampler r)
    {
        var input = new float[r.InputFor(1536) * 2 + 64];
        var output = new float[1536 * 2];
        for (int i = 0; i < input.Length; i++) input[i] = (i % 97) / 97f - 0.5f;
        return Best(() =>
        {
            var clock = Stopwatch.StartNew();
            for (int done = 0; done < AudioMixer.Rate * 10; done += 1536)
            {
                int need = r.InputFor(1536);
                r.Push(input.AsSpan(0, need * 2));
                r.Pull(output);
            }
            return clock.Elapsed.TotalSeconds;
        }) * 1000 / 10;
    }

    private static double Best(Func<double> run)
    {
        double best = double.MaxValue;
        for (int i = 0; i < 3; i++) best = Math.Min(best, run());
        return best;
    }

    private static double Busy(AudioMixer mixer, int seconds)
    {
        mixer.LowHp = true;
        mixer.Play(MusicLibrary.Get("common/battle_wild")!, night: false, immediate: true);
        mixer.SetAmbience(Enum.GetValues<AmbienceBed>().Take(AudioMixer.MaxBeds).Select((b, i) => new AmbienceLayer(b, 0.5f, i / 3f - 1f)).ToArray());
        var sounds = new[] { "select", "hit_normal", "hit_super", "ball_throw", "exp_gain", "menu_open", "cursor", "faint" }.Select(SoundBank.Get).Where(s => s != null).ToArray();
        var cry = Cries.Get(PokemonDatabase.Get("Gyarados")!);
        var buffer = new float[OutBlock(mixer) * 2];
        var clock = Stopwatch.StartNew();
        int rate = mixer.OutputRate, block = OutBlock(mixer);
        for (int done = 0, k = 0; done < rate * seconds; done += block, k++)
        {
            if (k % 8 == 0) foreach (var s in sounds) mixer.PlaySound(s!, AudioBus.Sound);
            if (k % 32 == 0) { mixer.PlaySound(cry, AudioBus.Cry); mixer.PlaySound(cry, AudioBus.Cry, 0.8f, 0.5f); }
            mixer.Render(buffer);
        }
        return clock.Elapsed.TotalSeconds;
    }

    // The audio thread's block: 1,024 frames at the DS's rate, 1,536 at the mix's (the same 32 ms)
    private static int OutBlock(AudioMixer mixer) => mixer.OutputRate == AudioMixer.Rate ? 1536 : 1024;

    private static double Voices(SynthSound sound, string name, int count, int seconds)
    {
        var synth = new Synthesizer(sound) { ReverbLevel = 0.3f };
        var inst = InstrumentBank.Get(name)!;
        for (int v = 0; v < count; v++) synth.NoteOn(inst, 48 + v % 36, 0.7f, synth.Rate * seconds * 2, (v % 7) / 3f - 1f, 0.3f, 0.2f, 1f);
        var buffer = new float[1536 * 2];
        var clock = Stopwatch.StartNew();
        for (int done = 0; done < synth.Rate * seconds; done += 1536)
        {
            // Plucked and struck voices die away: they are struck again so every voice is always sounding
            if (synth.ActiveVoices < count)
                for (int v = synth.ActiveVoices; v < count; v++) synth.NoteOn(inst, 48 + v % 36, 0.7f, synth.Rate * seconds, 0f, 0.3f, 0.2f, 1f);
            synth.Render(buffer);
        }
        return clock.Elapsed.TotalSeconds;
    }

    /// <summary>What each bus is heard with: a mixer and its rate in, the samples out.</summary>
    private static IEnumerable<(string, Func<AudioMixer, int, float[]>)> Scenes()
    {
        yield return ("music", (m, rate) =>
        {
            m.Play(MusicLibrary.Get("sinnoh/twinleaf")!, night: false);
            var a = Render(m, 20, rate);
            m.Play(MusicLibrary.Get("common/battle_wild")!, night: false, immediate: true);
            return [.. a, .. Render(m, 20, rate)];
        });
        yield return ("effects", (m, rate) =>
        {
            var all = new List<float>();
            foreach (var name in new[] { "select", "menu_open", "cursor", "hit_normal", "hit_super", "hit_weak", "ball_throw", "ball_shake", "catch", "exp_gain", "level_up_jingle", "faint", "door_open", "bump", "heal", "save" })
            {
                var s = SoundBank.Get(name);
                if (s == null) continue;
                m.PlaySound(s, AudioBus.Sound);
                all.AddRange(Render(m, s.Duration + 0.35, rate));
            }
            return all.ToArray();
        });
        yield return ("cries", (m, rate) =>
        {
            var all = new List<float>();
            foreach (var name in new[] { "Turtwig", "Piplup", "Chimchar", "Pikachu", "Gyarados", "Snorlax", "Dialga", "Giratina" })
            {
                var s = Cries.Get(PokemonDatabase.Get(name)!);
                m.PlaySound(s, AudioBus.Cry);
                all.AddRange(Render(m, s.Duration + 0.4, rate));
            }
            return all.ToArray();
        });
        yield return ("ambience", (m, rate) =>
        {
            var all = new List<float>();
            foreach (var bed in new[] { AmbienceBed.Rain, AmbienceBed.Waves, AmbienceBed.Cave, AmbienceBed.Waterfall })
            {
                m.SetAmbience(new[] { new AmbienceLayer(bed, 1f, 0f) });
                all.AddRange(Render(m, 6, rate));
            }
            return all.ToArray();
        });
    }

    private static float[] Render(AudioMixer mixer, double seconds, int rate)
    {
        int block = rate == AudioMixer.Rate ? 1536 : 1024;
        var buffer = new float[(int)(seconds * rate) * 2];
        for (int done = 0; done < buffer.Length; done += block * 2)
            mixer.Render(buffer.AsSpan(done, Math.Min(block * 2, buffer.Length - done)));
        return buffer;
    }
}
