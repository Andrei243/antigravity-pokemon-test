using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace PokemonPlatinumEngine.Audio;

/// <summary>
/// A small polyphonic synthesiser in the spirit of the DS sound chip: up to <see cref="MaxVoices"/> voices playing
/// the instruments of <see cref="InstrumentBank"/>, panned into stereo and sent through a plate-like reverb. It runs
/// at the DS's output rate and needs no audio device, so tests and tools can render music offline.
/// </summary>
public sealed partial class Synthesizer
{
    /// <summary>
    /// The DS mixes its sound at about 32.7 kHz: the rate of the DS sound, and of every sound effect, cry and ambience
    /// bed, whose numbers are all at it. Since plan 31 · MU1 the game's mix runs at <see cref="AudioMixer.Rate"/> and
    /// takes all of it in through a <see cref="Resampler"/>.
    /// </summary>
    public const int DsRate = 32768;

    /// <summary>The DS sound's voices.</summary>
    public const int MaxVoices = 40;

    private const int TableSize = 2048;

    /// <summary>Which sound this synthesiser makes (plan 31): the DS one, or the modern one at its own rate and voices.</summary>
    public SynthSound Sound { get; }

    /// <summary>The rate this synthesiser renders at: <see cref="DsRate"/> for the DS sound, <see cref="ModernRate"/> for the modern one.</summary>
    public int Rate { get; }

    private readonly float Dt;
    private readonly Voice[] voices;
    private readonly Reverb reverb;
    private float[] sendBuffer = new float[1024];
    private uint noiseSeed = 0x2545F491;
    private long noteCounter;

    /// <summary>How much of the reverb returns to the mix.</summary>
    public float ReverbLevel { get; set; } = 0.3f;

    /// <summary>The DS sound, exactly as it always was.</summary>
    public Synthesizer() : this(SynthSound.Ds) { }

    public Synthesizer(SynthSound sound)
    {
        Sound = sound;
        Rate = sound == SynthSound.Modern ? ModernRate : DsRate;
        Dt = 1f / Rate;
        voices = new Voice[sound == SynthSound.Modern ? ModernVoices : MaxVoices];
        reverb = new Reverb(Rate);
        for (int i = 0; i < voices.Length; i++) voices[i] = new Voice();
        if (sound == SynthSound.Modern) room = new RoomReverb(Rate);
    }

    /// <summary>The voices this synthesiser has.</summary>
    public int VoiceCount => voices.Length;

    public int ActiveVoices
    {
        get
        {
            int n = 0;
            foreach (var v in voices) if (v.Active) n++;
            return n;
        }
    }

    /// <summary>Starts a note. <paramref name="key"/> is a MIDI note number, or a <see cref="Drum"/> for the kit.</summary>
    public void NoteOn(Instrument inst, int key, float velocity, int gateSamples, float pan, float volume, float send) =>
        NoteOn(inst, key, velocity, gateSamples, pan, volume, send, 1f);

    /// <summary>Starts a note, with the track's stereo width (the modern sound's; the DS sound has none).</summary>
    public void NoteOn(Instrument inst, int key, float velocity, int gateSamples, float pan, float volume, float send, float width)
    {
        var v = FreeVoice();
        v.Active = true;
        v.Inst = inst;
        v.Order = ++noteCounter;
        v.Time = 0;
        v.Gate = Math.Max(1, gateSamples);
        v.Released = false;
        v.Level = 0f;
        v.Attacking = true;
        v.Amp = MathF.Pow(Math.Clamp(velocity, 0f, 1f), 1.4f) * volume * inst.Gain;
        v.Send = send;
        v.NoiseLp = 0f;
        v.NoiseHp = 0f;
        v.Phase0 = 0f;
        v.Phase1 = 0.37f;
        v.FmPhase = 0f;

        float p = Math.Clamp(pan, -1f, 1f);
        if (inst.IsKit)
        {
            v.Drum = (Drum)key;
            p = Math.Clamp(p + DrumPan(v.Drum), -1f, 1f);
            v.Freq = 0f;
        }
        else
        {
            int midi = key + 12 * inst.OctaveShift;
            v.Freq = 440f * MathF.Pow(2f, (midi - 69) / 12f);
            int band = Math.Clamp(midi / 12, 0, 10);
            float ceiling = Sound == SynthSound.Modern ? ModernCeiling : DsCeiling;
            v.BrightTable = Table(inst.Bright, band, ceiling);
            v.MellowTable = inst.Mellow != null ? Table(inst.Mellow, band, ceiling) : v.BrightTable;
        }

        // Equal-power pan; the second unison voice sits a little to the other side
        v.PanL = MathF.Cos((p + 1f) * MathF.PI / 4f);
        v.PanR = MathF.Sin((p + 1f) * MathF.PI / 4f);
        v.Velocity = Math.Clamp(velocity, 0f, 1f);
        if (Sound == SynthSound.Modern) StartModern(v, inst, p, v.Velocity, width);
        else if (!inst.IsKit && inst.Kind == VoiceKind.Pluck) StartString(v, inst);
        else if (!inst.IsKit && inst.Kind == VoiceKind.Modal) Array.Clear(v.ModePhase);
    }

    /// <summary>Releases every sounding note, or silences them at once.</summary>
    public void AllNotesOff(bool immediate)
    {
        foreach (var v in voices)
        {
            if (!v.Active) continue;
            if (immediate) v.Active = false;
            else v.Released = true;
        }
        if (immediate)
        {
            reverb.Clear();
            room?.Clear();
        }
    }

    /// <summary>How many notes have taken a voice that was still sounding (cut short, which can click): plan 31's measure of whether the voices suffice.</summary>
    public int Stolen { get; private set; }

    /// <summary>The most voices sounding at once when a note started.</summary>
    public int PeakVoices { get; private set; }

    private Voice FreeVoice()
    {
        Voice? oldest = null;
        int sounding = 0;
        Voice? free = null;
        foreach (var v in voices)
        {
            if (!v.Active)
            {
                free ??= v;
                continue;
            }
            sounding++;
            if (oldest == null || (v.Released && !oldest.Released) || (v.Released == oldest.Released && v.Order < oldest.Order))
                oldest = v;
        }
        PeakVoices = Math.Max(PeakVoices, sounding + 1);
        if (free != null) return free;
        Stolen++;
        return oldest!;
    }

    private static float DrumPan(Drum d) => d switch
    {
        Drum.HiHat or Drum.OpenHat or Drum.Shaker => 0.3f,
        Drum.HighTom => 0.25f,
        Drum.LowTom => -0.25f,
        Drum.Crash => -0.2f,
        _ => 0f
    };

    /// <summary>Renders interleaved stereo samples into <paramref name="output"/>, overwriting it.</summary>
    public void Render(Span<float> output)
    {
        if (Sound == SynthSound.Modern)
        {
            RenderModern(output);
            return;
        }
        int frames = output.Length / 2;
        output.Clear();
        if (sendBuffer.Length < frames) sendBuffer = new float[frames];
        Array.Clear(sendBuffer, 0, frames);

        foreach (var v in voices)
        {
            if (!v.Active) continue;
            if (v.Inst!.IsKit) RenderDrum(v, output, frames);
            else if (v.Inst.Kind == VoiceKind.Pluck) RenderString(v, output, frames, sendBuffer, sendBuffer);
            else if (v.Inst.Kind == VoiceKind.Modal) RenderModes(v, output, frames, sendBuffer, sendBuffer);
            else RenderTone(v, output, frames);
        }

        reverb.Process(sendBuffer.AsSpan(0, frames), output, ReverbLevel);
    }

    private void RenderTone(Voice v, Span<float> output, int frames)
    {
        var inst = v.Inst!;
        float attackStep = Dt / Math.Max(inst.Attack, 0.0005f);
        float decayCoef = MathF.Exp(-Dt / Math.Max(inst.Decay, 0.001f));
        float releaseCoef = MathF.Exp(-Dt / Math.Max(inst.Release, 0.001f));
        float detune = inst.Detune > 0 ? MathF.Pow(2f, inst.Detune / 1200f) : 1f;
        bool unison = inst.Detune > 0;
        var bright = v.BrightTable!;
        var mellow = v.MellowTable!;
        bool crossfade = !ReferenceEquals(bright, mellow);
        float sideL = unison ? 0.8f : 1f, sideR = unison ? 0.8f : 1f;

        float freq = v.Freq, fmIndex = 0f, mix = 1f;
        for (int i = 0; i < frames; i++)
        {
            float t = v.Time * Dt;

            // Envelope
            if (!v.Released && v.Time >= v.Gate) v.Released = true;
            if (v.Released)
            {
                v.Level *= releaseCoef;
                if (v.Level < 0.0005f) { v.Active = false; break; }
            }
            else if (v.Attacking)
            {
                v.Level += attackStep;
                if (v.Level >= 1f) { v.Level = 1f; v.Attacking = false; }
            }
            else
            {
                v.Level = inst.Sustain + (v.Level - inst.Sustain) * decayCoef;
                if (inst.Sustain <= 0f && v.Level < 0.0005f) { v.Active = false; break; }
            }

            // Pitch, FM depth and brightness change slowly, so they are worked out every 16 samples
            if ((i & 15) == 0)
            {
                float semis = 0f;
                if (inst.PitchStart != 0f) semis += inst.PitchStart * MathF.Exp(-t / inst.PitchTime);
                if (inst.Vibrato > 0f && t > inst.VibratoDelay)
                {
                    float depth = inst.Vibrato * MathF.Min(1f, (t - inst.VibratoDelay) / 0.3f);
                    semis += depth * MathF.Sin(MathF.Tau * inst.VibratoRate * t);
                }
                freq = semis == 0f ? v.Freq : v.Freq * MathF.Pow(2f, semis / 12f);
                if (inst.FmIndex > 0f)
                    fmIndex = inst.FmIndex * (inst.FmSustain + (1f - inst.FmSustain) * MathF.Exp(-t / inst.FmDecay)) / MathF.Tau;
                if (crossfade) mix = inst.BrightSustain + (1f - inst.BrightSustain) * MathF.Exp(-t / inst.BrightDecay);
            }

            float phaseMod = 0f;
            if (fmIndex > 0f)
            {
                phaseMod = fmIndex * MathF.Sin(MathF.Tau * v.FmPhase);
                v.FmPhase += freq * inst.FmRatio * Dt;
                if (v.FmPhase >= 1f) v.FmPhase -= MathF.Floor(v.FmPhase);
            }

            float s0 = Lookup(bright, mellow, v.Phase0 + phaseMod, mix, crossfade);
            float l, r;
            if (unison)
            {
                float s1 = Lookup(bright, mellow, v.Phase1 + phaseMod, mix, crossfade);
                l = s0 * sideL + s1 * (1f - sideL + 0.2f);
                r = s1 * sideR + s0 * (1f - sideR + 0.2f);
                v.Phase1 += freq * detune * Dt;
                if (v.Phase1 >= 1f) v.Phase1 -= 1f;
            }
            else
            {
                l = r = s0;
            }
            v.Phase0 += freq * Dt;
            if (v.Phase0 >= 1f) v.Phase0 -= 1f;

            if (inst.Noise > 0f)
            {
                float n = NextNoise();
                v.NoiseLp += (n - v.NoiseLp) * 0.25f;
                float breath = (n - v.NoiseLp) * inst.Noise * 1.5f;
                l += breath;
                r += breath;
            }

            float a = v.Level * v.Amp;
            output[2 * i] += l * a * v.PanL;
            output[2 * i + 1] += r * a * v.PanR;
            sendBuffer[i] += (l + r) * 0.5f * a * v.Send;
            v.Time++;
        }
    }

    private void RenderDrum(Voice v, Span<float> output, int frames) => RenderDrum(v, output, frames, sendBuffer, sendBuffer);

    /// <summary>
    /// The drum kit. Its noise filters are per-sample coefficients written for the DS's rate; at another rate each is
    /// moved by <see cref="K"/> so the drum keeps its colour, and at the DS's rate each is the very number it was.
    /// </summary>
    private void RenderDrum(Voice v, Span<float> output, int frames, float[] sendL, float[] sendR)
    {
        float k55 = K(0.55f), k08 = K(0.08f), k45 = K(0.45f), k50 = K(0.5f), k10 = K(0.1f);
        bool stereoSend = !ReferenceEquals(sendL, sendR);
        for (int i = 0; i < frames; i++)
        {
            float t = v.Time * Dt;
            float n = NextNoise();
            float s;
            float lp, hp;
            switch (v.Drum)
            {
                case Drum.Kick:
                {
                    float f = 52f + 110f * MathF.Exp(-t * 30f);
                    v.Phase0 += f * Dt;
                    s = MathF.Sin(MathF.Tau * v.Phase0) * MathF.Exp(-t * 13f) * 1.1f + n * MathF.Exp(-t * 260f) * 0.25f;
                    if (t > 0.35f) v.Active = false;
                    break;
                }
                case Drum.Snare:
                {
                    v.Phase0 += 190f * Dt;
                    v.NoiseLp += (n - v.NoiseLp) * k55;
                    hp = v.NoiseLp - v.NoiseHp;
                    v.NoiseHp += (v.NoiseLp - v.NoiseHp) * k08;
                    s = MathF.Sin(MathF.Tau * v.Phase0) * MathF.Exp(-t * 28f) * 0.55f + hp * MathF.Exp(-t * 15f) * 0.95f;
                    if (t > 0.4f) v.Active = false;
                    break;
                }
                case Drum.HiHat:
                case Drum.OpenHat:
                case Drum.Crash:
                case Drum.Shaker:
                {
                    v.NoiseLp += (n - v.NoiseLp) * k45;
                    hp = n - v.NoiseLp;
                    float decay = v.Drum switch { Drum.HiHat => 45f, Drum.OpenHat => 8f, Drum.Crash => 2.2f, _ => 28f };
                    float attack = v.Drum == Drum.Shaker ? MathF.Min(1f, t / 0.012f) : 1f;
                    float gain = v.Drum switch { Drum.HiHat => 1.0f, Drum.OpenHat => 0.6f, Drum.Crash => 0.6f, _ => 0.8f };
                    s = hp * MathF.Exp(-t * decay) * attack * gain;
                    float limit = v.Drum switch { Drum.HiHat => 0.12f, Drum.OpenHat => 0.7f, Drum.Crash => 2.4f, _ => 0.25f };
                    if (t > limit) v.Active = false;
                    break;
                }
                case Drum.Clap:
                {
                    v.NoiseLp += (n - v.NoiseLp) * k50;
                    hp = v.NoiseLp - v.NoiseHp;
                    v.NoiseHp += (v.NoiseLp - v.NoiseHp) * k10;
                    float burst = t < 0.03f ? MathF.Exp(-(t % 0.01f) * 300f) : MathF.Exp(-(t - 0.03f) * 18f);
                    s = hp * burst * 0.8f;
                    if (t > 0.3f) v.Active = false;
                    break;
                }
                case Drum.HighConga:
                case Drum.LowConga:
                case Drum.Bongo:
                {
                    // A hand on a skin: a tone that settles a little in pitch, and the slap of the fingers
                    float baseF = v.Drum switch { Drum.HighConga => 330f, Drum.LowConga => 222f, _ => 440f };
                    float ring = v.Drum == Drum.Bongo ? 22f : 11f;
                    v.Phase0 += baseF * (1f + 0.12f * MathF.Exp(-t * 40f)) * Dt;
                    v.NoiseLp += (n - v.NoiseLp) * k45;
                    hp = n - v.NoiseLp;
                    s = MathF.Sin(MathF.Tau * v.Phase0) * MathF.Exp(-t * ring) * 0.85f + hp * MathF.Exp(-t * 160f) * 0.5f;
                    if (t > 0.5f) v.Active = false;
                    break;
                }
                case Drum.WoodBlock:
                {
                    // Two hollow modes of a block of wood, gone in a moment
                    v.Phase0 += 920f * Dt;
                    v.Phase1 += 2470f * Dt;
                    if (v.Phase1 >= 1f) v.Phase1 -= 1f;
                    s = (MathF.Sin(MathF.Tau * v.Phase0) * 0.8f + MathF.Sin(MathF.Tau * v.Phase1) * 0.35f) * MathF.Exp(-t * 55f);
                    if (t > 0.15f) v.Active = false;
                    break;
                }
                default:
                {
                    float baseF = v.Drum switch { Drum.HighTom => 210f, Drum.MidTom => 160f, _ => 115f };
                    v.Phase0 += baseF * (1f + 0.45f * MathF.Exp(-t * 18f)) * Dt;
                    lp = n * MathF.Exp(-t * 90f) * 0.15f;
                    s = MathF.Sin(MathF.Tau * v.Phase0) * MathF.Exp(-t * 7f) * 0.9f + lp;
                    if (t > 0.6f) v.Active = false;
                    break;
                }
            }

            if (v.Phase0 >= 1f) v.Phase0 -= 1f;
            float a = s * v.Amp;
            output[2 * i] += a * v.PanL;
            output[2 * i + 1] += a * v.PanR;
            if (stereoSend)
            {
                sendL[i] += a * v.Send * v.PanL * 0.7f;
                sendR[i] += a * v.Send * v.PanR * 0.7f;
            }
            else sendL[i] += a * v.Send * 0.5f;
            v.Time++;
            if (!v.Active) break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Lookup(float[] bright, float[] mellow, float phase, float mix, bool crossfade)
    {
        phase -= MathF.Floor(phase);
        float pos = phase * TableSize;
        int i0 = (int)pos;
        float frac = pos - i0;
        int i1 = (i0 + 1) & (TableSize - 1);
        i0 &= TableSize - 1;
        float b = bright[i0] + (bright[i1] - bright[i0]) * frac;
        if (!crossfade) return b;
        float m = mellow[i0] + (mellow[i1] - mellow[i0]) * frac;
        return m + (b - m) * mix;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float NextNoise()
    {
        noiseSeed ^= noiseSeed << 13;
        noiseSeed ^= noiseSeed >> 17;
        noiseSeed ^= noiseSeed << 5;
        return (noiseSeed & 0xFFFFFF) / (float)0x800000 - 1f;
    }

    // Band-limited wavetables: one per spectrum and octave, with only the harmonics that stay under ~14 kHz (the DS
    // sound) or ~20 kHz (the modern sound, whose rate leaves room for them)
    private const float DsCeiling = 14000f;
    private const float ModernCeiling = 20000f;
    private static readonly Dictionary<(float[] Spectrum, int Band), float[]> tables = new(new TableKeyComparer());
    private static readonly Dictionary<(float[] Spectrum, int Band), float[]> modernTables = new(new TableKeyComparer());
    private static readonly object tableLock = new();

    private static float[] Table(float[] spectrum, int band, float ceiling)
    {
        lock (tableLock)
        {
            var cache = ceiling == DsCeiling ? tables : modernTables;
            if (cache.TryGetValue((spectrum, band), out var cached)) return cached;
            float top = 440f * MathF.Pow(2f, (band * 12 + 11 + 1 - 69) / 12f) * 1.03f;
            int maxHarmonic = Math.Max(1, (int)(ceiling / top));
            var table = new float[TableSize];
            float peak = 0f;
            for (int i = 0; i < TableSize; i++)
            {
                float x = MathF.Tau * i / TableSize;
                float s = 0f;
                for (int h = 0; h < spectrum.Length && h < maxHarmonic; h++)
                {
                    if (spectrum[h] != 0f) s += spectrum[h] * MathF.Sin(x * (h + 1));
                }
                table[i] = s;
                peak = MathF.Max(peak, MathF.Abs(s));
            }
            // Normalise so every timbre plays at a similar loudness
            float rms = 0f;
            foreach (var s in table) rms += s * s;
            rms = MathF.Sqrt(rms / TableSize);
            float scale = rms > 0 ? 0.5f / rms : 0f;
            if (peak * scale > 1.2f) scale = 1.2f / peak;
            for (int i = 0; i < TableSize; i++) table[i] *= scale;
            cache[(spectrum, band)] = table;
            return table;
        }
    }

    private sealed class TableKeyComparer : IEqualityComparer<(float[] Spectrum, int Band)>
    {
        public bool Equals((float[] Spectrum, int Band) a, (float[] Spectrum, int Band) b) =>
            ReferenceEquals(a.Spectrum, b.Spectrum) && a.Band == b.Band;

        public int GetHashCode((float[] Spectrum, int Band) k) => RuntimeHelpers.GetHashCode(k.Spectrum) * 31 + k.Band;
    }

    private sealed partial class Voice
    {
        public bool Active;
        public Instrument? Inst;
        public long Order;
        public int Time;
        public int Gate;
        public bool Released;
        public bool Attacking;
        public float Level;
        public float Amp;
        public float Send;
        public float Freq;
        public float Phase0, Phase1, FmPhase;
        public float PanL, PanR;
        public float NoiseLp, NoiseHp;
        public float[]? BrightTable, MellowTable;
        public Drum Drum;
    }

    /// <summary>A Freeverb-style reverb: parallel damped combs into series all-passes, slightly different per side.</summary>
    private sealed class Reverb
    {
        private readonly Comb[] combsL, combsR;
        private readonly AllPass[] allL, allR;

        public Reverb(int rate)
        {
            int[] combLengths = { 1116, 1188, 1277, 1356, 1422, 1491 };
            int[] allLengths = { 556, 441, 341 };
            float scale = rate / 44100f;
            combsL = Array.ConvertAll(combLengths, n => new Comb((int)(n * scale)));
            combsR = Array.ConvertAll(combLengths, n => new Comb((int)((n + 23) * scale)));
            allL = Array.ConvertAll(allLengths, n => new AllPass((int)(n * scale)));
            allR = Array.ConvertAll(allLengths, n => new AllPass((int)((n + 23) * scale)));
        }

        public void Clear()
        {
            foreach (var c in combsL) c.Clear();
            foreach (var c in combsR) c.Clear();
            foreach (var a in allL) a.Clear();
            foreach (var a in allR) a.Clear();
        }

        public void Process(Span<float> send, Span<float> output, float level)
        {
            for (int i = 0; i < send.Length; i++)
            {
                float input = send[i] * 0.12f;
                float l = 0f, r = 0f;
                for (int c = 0; c < combsL.Length; c++)
                {
                    l += combsL[c].Process(input);
                    r += combsR[c].Process(input);
                }
                for (int a = 0; a < allL.Length; a++)
                {
                    l = allL[a].Process(l);
                    r = allR[a].Process(r);
                }
                output[2 * i] += l * level;
                output[2 * i + 1] += r * level;
            }
        }

        private sealed class Comb
        {
            private readonly float[] buffer;
            private int index;
            private float store;

            public Comb(int length) => buffer = new float[length];

            public void Clear()
            {
                Array.Clear(buffer);
                store = 0f;
            }

            public float Process(float input)
            {
                float output = buffer[index];
                store = output * 0.7f + store * 0.3f;
                buffer[index] = input + store * 0.82f;
                if (++index >= buffer.Length) index = 0;
                return output;
            }
        }

        private sealed class AllPass
        {
            private readonly float[] buffer;
            private int index;

            public AllPass(int length) => buffer = new float[length];

            public void Clear() => Array.Clear(buffer);

            public float Process(float input)
            {
                float buffered = buffer[index];
                float output = buffered - input;
                buffer[index] = input + buffered * 0.5f;
                if (++index >= buffer.Length) index = 0;
                return output;
            }
        }
    }
}
