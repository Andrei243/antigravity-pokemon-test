using System;

namespace PokemonPlatinumEngine.Audio;

/// <summary>Which sound a synthesiser makes (plan 31): the DS one every song has had, or the modern one of the later regions.</summary>
public enum SynthSound { Ds, Modern }

/// <summary>The kinds of room a modern song's reverb can be (plan 31 · MU0), each with its own pre-delay, damping and early reflections.</summary>
public enum RoomKind { Room, Chamber, Hall, Plate }

/// <summary>
/// A modern song's reverb (the header's <c>room hall 2.1</c>): its kind, how long it rings before falling 60 dB, the
/// gap before it answers and how quickly its highs die (0 bright to 1 dark). The DS sound has one reverb and ignores it.
/// </summary>
public readonly record struct RoomShape(RoomKind Kind, float Seconds, float PreDelay, float Damping)
{
    /// <summary>A room of a kind with its kind's own pre-delay and damping.</summary>
    public static RoomShape Of(RoomKind kind, float seconds) => kind switch
    {
        RoomKind.Room => new(kind, seconds, 0.006f, 0.5f),
        RoomKind.Chamber => new(kind, seconds, 0.012f, 0.4f),
        RoomKind.Hall => new(kind, seconds, 0.024f, 0.32f),
        _ => new(kind, seconds, 0f, 0.18f)
    };

    /// <summary>What a modern song without a <c>room</c> line plays in.</summary>
    public static RoomShape Default => Of(RoomKind.Hall, 1.6f);

    public bool HasEarlyReflections => Kind != RoomKind.Plate;
}

/// <summary>
/// The modern sound (plan 31 · MU0): 48 kHz and <see cref="ModernVoices"/> voices, wavetables with harmonics up to
/// 20 kHz, unison voices spread across the stereo field by the instrument's <see cref="Instrument.Spread"/> and the
/// track's width, a harder note brighter, plucked strings and struck modes, and a stereo room reverb with a size, a
/// pre-delay and damping. None of it is reached by the DS sound, which renders as it always has.
/// </summary>
public sealed partial class Synthesizer
{
    /// <summary>The modern sound's rate: the device's.</summary>
    public const int ModernRate = 48000;

    /// <summary>The modern sound's voices (plan 31, decision 2: 96 until it is measured on the Windows machine).</summary>
    public const int ModernVoices = 96;

    private const int MaxUnison = 4;
    private const int MaxModes = 6;
    private const int StringLength = 4096;

    private readonly RoomReverb? room;
    private float[] sendRight = new float[1024];
    private readonly float[] burstScratch = new float[StringLength / 2];

    /// <summary>
    /// A voice's reverb send: in stereo, following its pan, for the modern sound's room; into the one mono send for the
    /// DS sound's reverb (whose two buffers are then the same).
    /// </summary>
    private static void SendOf(float[] sendL, float[] sendR, int i, float a, Voice v)
    {
        if (ReferenceEquals(sendL, sendR)) sendL[i] += a * v.Send;
        else
        {
            sendL[i] += a * v.Send * v.PanL * 1.4f;
            sendR[i] += a * v.Send * v.PanR * 1.4f;
        }
    }

    /// <summary>The modern sound's room; the DS sound has none.</summary>
    public RoomShape Room
    {
        get => room?.Shape ?? RoomShape.Default;
        set => room?.Set(value);
    }

    /// <summary>
    /// A per-sample filter coefficient written for the DS's rate, moved to this synthesiser's rate so the filter keeps its
    /// corner. At the DS's rate it is the very number given, so the DS sound doesn't move a sample.
    /// </summary>
    private float K(float c) => Rate == SampleRate ? c : 1f - MathF.Pow(1f - c, SampleRate / (float)Rate);

    private sealed partial class Voice
    {
        public float Velocity, Width;
        public int Unison;
        public readonly float[] UPhase = new float[MaxUnison], URatio = new float[MaxUnison];
        public readonly float[] UPanL = new float[MaxUnison], UPanR = new float[MaxUnison];
        public float[]? String;
        public int Write;
        public float Last, Follow;
        public readonly float[] ModePhase = new float[MaxModes];
    }

    private void StartModern(Voice v, Instrument inst, float pan, float velocity, float width)
    {
        v.Velocity = velocity;
        v.Width = Math.Clamp(width, 0f, 1f);
        if (inst.IsKit) return;

        switch (inst.Kind)
        {
            case VoiceKind.Pluck:
                StartString(v, inst);
                break;
            case VoiceKind.Modal:
                Array.Clear(v.ModePhase);
                break;
            default:
            {
                int n = inst.Detune > 0 ? Math.Clamp(inst.Unison > 0 ? inst.Unison : 2, 1, MaxUnison) : 1;
                v.Unison = n;
                for (int k = 0; k < n; k++)
                {
                    // Spread evenly from one side to the other, detuned as far apart as the DS's pair is
                    float at = n == 1 ? 0f : k / (n - 1f) * 2f - 1f;
                    v.URatio[k] = MathF.Pow(2f, at * inst.Detune * 0.5f / 1200f);
                    v.UPhase[k] = k * 0.37f % 1f;
                    float p = Math.Clamp(pan + at * inst.Spread * v.Width, -1f, 1f);
                    v.UPanL[k] = MathF.Cos((p + 1f) * MathF.PI / 4f);
                    v.UPanR[k] = MathF.Sin((p + 1f) * MathF.PI / 4f);
                }
                break;
            }
        }
    }

    private void RenderModern(Span<float> output)
    {
        int frames = output.Length / 2;
        output.Clear();
        if (sendBuffer.Length < frames) sendBuffer = new float[frames];
        if (sendRight.Length < frames) sendRight = new float[frames];
        Array.Clear(sendBuffer, 0, frames);
        Array.Clear(sendRight, 0, frames);

        foreach (var v in voices)
        {
            if (!v.Active) continue;
            var inst = v.Inst!;
            if (inst.IsKit) RenderDrum(v, output, frames, sendBuffer, sendRight);
            else if (inst.Kind == VoiceKind.Pluck) RenderString(v, output, frames, sendBuffer, sendRight);
            else if (inst.Kind == VoiceKind.Modal) RenderModes(v, output, frames, sendBuffer, sendRight);
            else RenderToneModern(v, output, frames);
        }

        room!.Process(sendBuffer.AsSpan(0, frames), sendRight.AsSpan(0, frames), output, ReverbLevel);
    }

    /// <summary>The release stage shared by the modern voices; false once the voice has died away.</summary>
    private bool Envelope(Voice v, Instrument inst, float attackStep, float decayCoef, float releaseCoef)
    {
        if (!v.Released && v.Time >= v.Gate) v.Released = true;
        if (v.Released)
        {
            v.Level *= releaseCoef;
            if (v.Level < 0.0005f) { v.Active = false; return false; }
        }
        else if (v.Attacking)
        {
            v.Level += attackStep;
            if (v.Level >= 1f) { v.Level = 1f; v.Attacking = false; }
        }
        else
        {
            v.Level = inst.Sustain + (v.Level - inst.Sustain) * decayCoef;
            if (inst.Sustain <= 0f && v.Level < 0.0005f) { v.Active = false; return false; }
        }
        return true;
    }

    private float Bend(Instrument inst, float t)
    {
        float semis = 0f;
        if (inst.PitchStart != 0f) semis += inst.PitchStart * MathF.Exp(-t / inst.PitchTime);
        if (inst.Vibrato > 0f && t > inst.VibratoDelay)
        {
            float depth = inst.Vibrato * MathF.Min(1f, (t - inst.VibratoDelay) / 0.3f);
            semis += depth * MathF.Sin(MathF.Tau * inst.VibratoRate * t);
        }
        return semis == 0f ? 1f : MathF.Pow(2f, semis / 12f);
    }

    private void RenderToneModern(Voice v, Span<float> output, int frames)
    {
        var inst = v.Inst!;
        float attackStep = Dt / Math.Max(inst.Attack, 0.0005f);
        float decayCoef = MathF.Exp(-Dt / Math.Max(inst.Decay, 0.001f));
        float releaseCoef = MathF.Exp(-Dt / Math.Max(inst.Release, 0.001f));
        var bright = v.BrightTable!;
        var mellow = v.MellowTable!;
        bool crossfade = !ReferenceEquals(bright, mellow);
        int n = Math.Max(1, v.Unison);
        // Detuned voices add as power, not as amplitude: each at 0.9/√n keeps the DS pair's level whatever their number
        float each = n == 1 ? 1f : 0.9f / MathF.Sqrt(n);
        float noiseK = K(0.25f);
        // A harder note is brighter: its bright spectrum lasts longer and its FM goes deeper
        float tone = 0.55f + 0.7f * v.Velocity;

        float freq = v.Freq, fmIndex = 0f, mix = 1f;
        for (int i = 0; i < frames; i++)
        {
            float t = v.Time * Dt;
            if (!Envelope(v, inst, attackStep, decayCoef, releaseCoef)) break;

            if ((i & 15) == 0)
            {
                freq = v.Freq * Bend(inst, t);
                if (inst.FmIndex > 0f)
                    fmIndex = tone * inst.FmIndex * (inst.FmSustain + (1f - inst.FmSustain) * MathF.Exp(-t / inst.FmDecay)) / MathF.Tau;
                if (crossfade) mix = MathF.Min(1f, tone * (inst.BrightSustain + (1f - inst.BrightSustain) * MathF.Exp(-t / inst.BrightDecay)));
            }

            float phaseMod = 0f;
            if (fmIndex > 0f)
            {
                phaseMod = fmIndex * MathF.Sin(MathF.Tau * v.FmPhase);
                v.FmPhase += freq * inst.FmRatio * Dt;
                if (v.FmPhase >= 1f) v.FmPhase -= MathF.Floor(v.FmPhase);
            }

            float l = 0f, r = 0f;
            for (int k = 0; k < n; k++)
            {
                float s = Lookup(bright, mellow, v.UPhase[k] + phaseMod, mix, crossfade) * each;
                l += s * v.UPanL[k];
                r += s * v.UPanR[k];
                v.UPhase[k] += freq * v.URatio[k] * Dt;
                if (v.UPhase[k] >= 1f) v.UPhase[k] -= 1f;
            }

            if (inst.Noise > 0f)
            {
                float nz = NextNoise();
                v.NoiseLp += (nz - v.NoiseLp) * noiseK;
                float breath = (nz - v.NoiseLp) * inst.Noise * 1.5f;
                l += breath * v.PanL;
                r += breath * v.PanR;
            }

            float a = v.Level * v.Amp;
            output[2 * i] += l * a;
            output[2 * i + 1] += r * a;
            sendBuffer[i] += l * a * v.Send;
            sendRight[i] += r * a * v.Send;
            v.Time++;
        }
    }

    /// <summary>
    /// Lays the pluck into the string: a burst of noise a period long, as bright as the instrument and the note's
    /// velocity make it, hollowed by where the pluck lands and with its average taken out, so the string starts at rest.
    /// </summary>
    private void StartString(Voice v, Instrument inst)
    {
        v.String ??= new float[StringLength];
        var s = v.String;
        Array.Clear(s);
        v.Write = 0;
        v.Last = 0f;
        v.Follow = 1f;
        v.Level = 1f;
        v.Attacking = false;

        int period = Math.Clamp((int)(Rate / v.Freq), 2, StringLength / 2);
        float cutoff = 500f + 11000f * inst.PluckBright * (0.35f + 0.65f * v.Velocity);
        float c = 1f - MathF.Exp(-MathF.Tau * cutoff / Rate);
        var burst = burstScratch;
        float lp = 0f, mean = 0f;
        for (int j = 0; j < period; j++)
        {
            lp += (NextNoise() - lp) * c;
            burst[j] = lp;
        }
        int pick = Math.Max(1, (int)(period * Math.Clamp(inst.PluckPosition, 0f, 0.5f)));
        for (int j = period - 1; j >= pick; j--) burst[j] -= burst[j - pick];
        for (int j = 0; j < period; j++) mean += burst[j];
        mean /= period;
        // Brought to a level by its power, not its peak, and its loudest moments rounded off, so a pluck's first
        // instant stands no higher over the ringing string than a real one does
        double power = 1e-9;
        for (int j = 0; j < period; j++)
        {
            burst[j] -= mean;
            power += burst[j] * burst[j];
        }
        float scale = 0.35f / (float)Math.Sqrt(power / period);
        for (int j = 0; j < period; j++) s[(StringLength - period + j) & (StringLength - 1)] = MathF.Tanh(burst[j] * scale * 3f) / 3f;
    }

    private void RenderString(Voice v, Span<float> output, int frames, float[] sendL, float[] sendR)
    {
        var inst = v.Inst!;
        var s = v.String!;
        const int mask = StringLength - 1;
        float releaseCoef = MathF.Exp(-Dt / Math.Max(inst.Release, 0.001f));
        float blend = Math.Clamp(inst.PluckDamp, 0f, 1f) * 0.5f;
        float ring = inst.Ring * MathF.Pow(261.6f / v.Freq, 0.3f);
        float follow = MathF.Exp(-Dt / 0.05f);
        float delay = Rate / v.Freq, loopGain = 1f;
        float hpState = 0f, hpLast = 0f;

        for (int i = 0; i < frames; i++)
        {
            float t = v.Time * Dt;
            if (!v.Released && v.Time >= v.Gate) v.Released = true;
            if (v.Released)
            {
                v.Level *= releaseCoef;
                if (v.Level < 0.0005f) { v.Active = false; break; }
            }

            if ((i & 15) == 0)
            {
                float freq = v.Freq * Bend(inst, t);
                delay = Math.Clamp(Rate / freq - blend, 2f, StringLength - 2f);
                // One trip round the loop takes a period, and a ring of "ring" seconds falls 60 dB over that many periods
                loopGain = MathF.Pow(10f, -3f / (ring * freq));
            }

            float read = v.Write - delay;
            float floor = MathF.Floor(read);
            int i0 = (int)floor;
            float frac = read - floor;
            float x = s[i0 & mask] + (s[(i0 + 1) & mask] - s[i0 & mask]) * frac;
            float y = (1f - blend) * x + blend * v.Last;
            v.Last = x;
            s[v.Write & mask] = y * loopGain;
            v.Write++;

            // What leaves the string through the bridge: its lowest rumble taken off
            hpState = 0.995f * (hpState + y - hpLast);
            hpLast = y;
            float outS = hpState;

            v.Follow = MathF.Max(MathF.Abs(outS), v.Follow * follow);
            if (t > 0.05f && v.Follow < 0.0002f) { v.Active = false; break; }

            // A rise of two milliseconds, so the pluck is a pluck and not a click
            float a = v.Level * v.Amp * outS * MathF.Min(1f, t / 0.002f);
            output[2 * i] += a * v.PanL;
            output[2 * i + 1] += a * v.PanR;
            SendOf(sendL, sendR, i, a, v);
            v.Time++;
        }
    }

    private static readonly float[] Sine = BuildSine();

    private static float[] BuildSine()
    {
        var table = new float[TableSize];
        for (int i = 0; i < TableSize; i++) table[i] = MathF.Sin(MathF.Tau * i / TableSize);
        return table;
    }

    private void RenderModes(Voice v, Span<float> output, int frames, float[] sendL, float[] sendR)
    {
        var inst = v.Inst!;
        float releaseCoef = MathF.Exp(-Dt / Math.Max(inst.Release, 0.001f));
        int modes = Math.Min(MaxModes, Math.Min(inst.Ratios.Length, Math.Min(inst.Amps.Length, inst.Decays.Length)));
        float longest = 0f;
        for (int m = 0; m < modes; m++) longest = MathF.Max(longest, inst.Decays[m]);
        Span<float> coef = stackalloc float[MaxModes];
        Span<float> amp = stackalloc float[MaxModes];
        for (int m = 0; m < modes; m++)
        {
            coef[m] = MathF.Exp(-Dt / inst.Decays[m]);
            // A harder strike wakes the upper modes more
            float wake = m == 0 ? 1f : 0.35f + 0.9f * v.Velocity;
            amp[m] = inst.Amps[m] * wake * MathF.Exp(-v.Time * Dt / inst.Decays[m]);
        }
        float strikeK = K(0.5f), bend = 1f;

        for (int i = 0; i < frames; i++)
        {
            float t = v.Time * Dt;
            if (!v.Released && v.Time >= v.Gate) v.Released = true;
            if (v.Released)
            {
                v.Level *= releaseCoef;
                if (v.Level < 0.0005f) { v.Active = false; break; }
            }
            else v.Level = 1f;
            if (t > longest * 7f) { v.Active = false; break; }
            if ((i & 15) == 0) bend = Bend(inst, t);

            float sum = 0f;
            for (int m = 0; m < modes; m++)
            {
                float f = v.Freq * inst.Ratios[m] * bend;
                if (f < Rate * 0.45f)
                {
                    float phase = v.ModePhase[m];
                    float pos = phase * TableSize;
                    int j = (int)pos;
                    float fr = pos - j;
                    sum += (Sine[j & (TableSize - 1)] + (Sine[(j + 1) & (TableSize - 1)] - Sine[j & (TableSize - 1)]) * fr) * amp[m];
                    phase += f * Dt;
                    v.ModePhase[m] = phase - MathF.Floor(phase);
                }
                amp[m] *= coef[m];
            }

            if (inst.Strike > 0f)
            {
                float nz = NextNoise();
                v.NoiseLp += (nz - v.NoiseLp) * strikeK;
                sum += (nz - v.NoiseLp) * inst.Strike * MathF.Exp(-t * 260f);
            }

            // A millisecond's rise, so the strike never clicks
            float a = v.Level * v.Amp * sum * MathF.Min(1f, t / 0.001f);
            output[2 * i] += a * v.PanL;
            output[2 * i + 1] += a * v.PanR;
            SendOf(sendL, sendR, i, a, v);
            v.Time++;
        }
    }

    /// <summary>
    /// The modern sound's reverb: a pre-delay with a few early reflections tapped from it, then eight damped combs
    /// and four all-passes a side (Freeverb's shape), the combs' feedback set from how long the room rings and their
    /// lengths from how big it is. The send is high-passed first, so the bass stays dry and the room stays clear.
    /// </summary>
    private sealed class RoomReverb
    {
        private static readonly int[] CombLengths = { 1116, 1188, 1277, 1356, 1422, 1491, 1557, 1617 };
        private static readonly int[] AllLengths = { 556, 441, 341, 225 };
        private const int Spread = 23;
        // Early reflections: (seconds after the pre-delay, gain left, gain right)
        private static readonly (float At, float L, float R)[] Reflections =
        {
            (0.0043f, 0.42f, 0.22f), (0.0115f, 0.18f, 0.36f), (0.0170f, 0.30f, 0.12f), (0.0240f, 0.10f, 0.26f),
            (0.0310f, 0.20f, 0.08f), (0.0420f, 0.07f, 0.16f)
        };

        private readonly int rate;
        private readonly Comb[] combsL = new Comb[CombLengths.Length], combsR = new Comb[CombLengths.Length];
        private readonly AllPass[] allL, allR;
        private readonly float[] preL, preR;
        private int preWrite, preDelay;
        private readonly int[] reflectionAt = new int[Reflections.Length];
        private float hpL, hpR, lastL, lastR;
        private readonly float hpCoef;

        public RoomShape Shape { get; private set; }

        public RoomReverb(int rate)
        {
            this.rate = rate;
            float scale = rate / 44100f;
            // Combs long enough for the biggest room
            for (int c = 0; c < CombLengths.Length; c++)
            {
                combsL[c] = new Comb((int)(CombLengths[c] * scale * 1.6f) + 1);
                combsR[c] = new Comb((int)((CombLengths[c] + Spread) * scale * 1.6f) + 1);
            }
            allL = Array.ConvertAll(AllLengths, n => new AllPass((int)(n * scale)));
            allR = Array.ConvertAll(AllLengths, n => new AllPass((int)((n + Spread) * scale)));
            preL = new float[rate / 5];
            preR = new float[rate / 5];
            hpCoef = 1f / (1f + MathF.Tau * 140f / rate);
            Set(RoomShape.Default);
        }

        public void Set(RoomShape shape)
        {
            Shape = shape;
            float seconds = Math.Clamp(shape.Seconds, 0.2f, 8f);
            // A bigger room: longer combs, so its echoes thin out more slowly and it sounds larger, not only longer
            float size = Math.Clamp(0.7f + 0.22f * seconds, 0.75f, 1.6f);
            float scale = rate / 44100f;
            float damping = Math.Clamp(shape.Damping, 0f, 0.95f);
            for (int c = 0; c < CombLengths.Length; c++)
            {
                combsL[c].Set((int)(CombLengths[c] * scale * size), seconds, rate, damping);
                combsR[c].Set((int)((CombLengths[c] + Spread) * scale * size), seconds, rate, damping);
            }
            preDelay = Math.Clamp((int)(shape.PreDelay * rate), 0, preL.Length - (int)(0.05f * rate) - 2);
            for (int r = 0; r < Reflections.Length; r++) reflectionAt[r] = (int)(Reflections[r].At * rate * size);
        }

        public void Clear()
        {
            foreach (var c in combsL) c.Clear();
            foreach (var c in combsR) c.Clear();
            foreach (var a in allL) a.Clear();
            foreach (var a in allR) a.Clear();
            Array.Clear(preL);
            Array.Clear(preR);
            hpL = hpR = lastL = lastR = 0f;
        }

        public void Process(ReadOnlySpan<float> sendL, ReadOnlySpan<float> sendR, Span<float> output, float level)
        {
            int length = preL.Length;
            bool early = Shape.HasEarlyReflections;
            for (int i = 0; i < sendL.Length; i++)
            {
                hpL = hpCoef * (hpL + sendL[i] - lastL);
                lastL = sendL[i];
                hpR = hpCoef * (hpR + sendR[i] - lastR);
                lastR = sendR[i];
                preL[preWrite] = hpL;
                preR[preWrite] = hpR;

                int at = preWrite - preDelay;
                if (at < 0) at += length;
                float inL = preL[at], inR = preR[at];
                float input = (inL + inR) * 0.035f;

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

                if (early)
                {
                    for (int e = 0; e < Reflections.Length; e++)
                    {
                        int tap = at - reflectionAt[e];
                        if (tap < 0) tap += length;
                        l += preL[tap] * Reflections[e].L * 0.5f + preR[tap] * Reflections[e].L * 0.15f;
                        r += preR[tap] * Reflections[e].R * 0.5f + preL[tap] * Reflections[e].R * 0.15f;
                    }
                }

                output[2 * i] += l * level;
                output[2 * i + 1] += r * level;
                if (++preWrite >= length) preWrite = 0;
            }
        }

        private sealed class Comb
        {
            private readonly float[] buffer;
            private int index, length;
            private float store, feedback, damp;

            public Comb(int capacity) => buffer = new float[capacity];

            public void Set(int samples, float seconds, int rate, float damping)
            {
                length = Math.Clamp(samples, 1, buffer.Length);
                if (index >= length) index = 0;
                feedback = MathF.Pow(10f, -3f * length / (rate * seconds));
                damp = damping;
            }

            public void Clear()
            {
                Array.Clear(buffer);
                store = 0f;
            }

            public float Process(float input)
            {
                float output = buffer[index];
                store = output * (1f - damp) + store * damp;
                buffer[index] = input + store * feedback;
                if (++index >= length) index = 0;
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
