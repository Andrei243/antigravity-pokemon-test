using System;

namespace PokemonPlatinumEngine.Audio;

/// <summary>
/// The toolkit the sound effects are built with: a buffer of mono samples at the synthesiser's rate that layers
/// are added into, each at its own moment. A layer is an oscillator whose pitch glides (sine, pulse, triangle, saw,
/// all band-limited), a two-operator FM bell, or noise through a filter whose cutoff glides. Every layer has an
/// envelope that rises over its attack and falls to nothing by its end, so a layer never clicks. Noise comes from a
/// seeded generator: a sound is the same every time it is made.
/// </summary>
internal sealed class SoundDesign
{
    public const int Rate = Synthesizer.SampleRate;

    public enum Wave { Sine, Pulse, Triangle, Saw }

    public enum Filter { Low, Band, High }

    public readonly float[] S;
    private uint seed;

    public SoundDesign(float seconds, uint seed = 0x9E3779B9u)
    {
        S = new float[Math.Max(1, (int)(Rate * seconds))];
        this.seed = seed == 0 ? 1u : seed;
    }

    /// <summary>
    /// An envelope at <paramref name="u"/> (0..1) through a layer: a straight rise over the first
    /// <paramref name="attack"/> of it, then a fall to nothing shaped by <paramref name="power"/> (1 straight, higher
    /// sooner, like a struck thing; below 1 held, like a blown one).
    /// </summary>
    private static float Envelope(float u, float attack, float power)
    {
        if (u < attack) return u / attack;
        float fall = (u - attack) / Math.Max(1e-6f, 1f - attack);
        return MathF.Pow(Math.Max(0f, 1f - fall), power);
    }

    /// <summary>A pitch from <paramref name="from"/> to <paramref name="to"/> at <paramref name="u"/>, by equal ratios (as the ear hears a glide).</summary>
    private static float Glide(float from, float to, float u) => from * MathF.Pow(to / from, u);

    private int Start(float at) => (int)(at * Rate);

    private int Length(float at, float seconds) => Math.Min((int)(seconds * Rate), S.Length - Start(at));

    /// <summary>
    /// An oscillator from <paramref name="at"/> for <paramref name="seconds"/>, its pitch gliding from
    /// <paramref name="from"/> to <paramref name="to"/>. <paramref name="duty"/> is a pulse's width;
    /// <paramref name="vibratoHz"/> and <paramref name="vibratoDepth"/> (a share of the pitch) wobble it.
    /// </summary>
    public SoundDesign Tone(float at, float seconds, Wave wave, float from, float to, float gain,
        float attack = 0.02f, float power = 1.5f, float duty = 0.5f, float vibratoHz = 0f, float vibratoDepth = 0f)
    {
        int start = Start(at), n = Length(at, seconds);
        double phase = 0;
        for (int i = 0; i < n; i++)
        {
            float u = i / (float)n;
            float t = i / (float)Rate;
            float f = Glide(from, to, u) * (1f + vibratoDepth * MathF.Sin(MathF.Tau * vibratoHz * t));
            float dt = Math.Min(0.45f, f / Rate);
            phase += dt;
            phase -= Math.Floor(phase);
            float p = (float)phase;
            float x;
            switch (wave)
            {
                case Wave.Sine:
                    x = MathF.Sin(MathF.Tau * p);
                    break;
                case Wave.Saw:
                    x = 2f * p - 1f - Blep(p, dt);
                    break;
                case Wave.Pulse:
                    x = (p < duty ? 1f : -1f) + Blep(p, dt) - Blep((p - duty + 1f) % 1f, dt);
                    x -= 2f * duty - 1f; // the pulse's own offset taken out
                    break;
                default:
                    // Its harmonics fall away fast enough that it needs no band-limiting at these pitches
                    x = 4f * MathF.Abs(p - 0.5f) - 1f;
                    break;
            }
            S[start + i] += x * gain * Envelope(u, attack, power);
        }
        return this;
    }

    /// <summary>Notes one after another, each <paramref name="step"/> after the last and ringing <paramref name="length"/>.</summary>
    public SoundDesign Notes(float at, float step, float length, Wave wave, float[] pitches, float gain,
        float attack = 0.03f, float power = 2f, float duty = 0.5f)
    {
        for (int k = 0; k < pitches.Length; k++)
            Tone(at + k * step, length, wave, pitches[k], pitches[k], gain, attack, power, duty);
        return this;
    }

    /// <summary>
    /// A bell: a sine carrier modulated by another at <paramref name="ratio"/> times its pitch, the modulation
    /// (<paramref name="index"/>) dying faster than the tone, so it strikes bright and rings out pure.
    /// </summary>
    public SoundDesign Bell(float at, float seconds, float pitch, float ratio, float index, float gain, float power = 2.5f)
    {
        int start = Start(at), n = Length(at, seconds);
        for (int i = 0; i < n; i++)
        {
            float u = i / (float)n;
            float t = i / (float)Rate;
            float env = Envelope(u, 0.004f, power);
            float mod = index * env * env * MathF.Sin(MathF.Tau * pitch * ratio * t);
            S[start + i] += MathF.Sin(MathF.Tau * pitch * t + mod) * gain * env;
        }
        return this;
    }

    /// <summary>
    /// Noise through a resonant filter whose cutoff glides from <paramref name="from"/> to <paramref name="to"/>
    /// (Hz); <paramref name="q"/> is its resonance. <paramref name="flutterHz"/> chops it into a rustle or a rattle.
    /// </summary>
    public SoundDesign Noise(float at, float seconds, Filter filter, float from, float to, float gain,
        float q = 0.7f, float attack = 0.01f, float power = 1.5f, float flutterHz = 0f, float flutterDepth = 0f)
    {
        int start = Start(at), n = Length(at, seconds);
        float ic1 = 0f, ic2 = 0f;
        for (int i = 0; i < n; i++)
        {
            float u = i / (float)n;
            float t = i / (float)Rate;
            float cutoff = Math.Clamp(Glide(from, to, u), 20f, Rate * 0.45f);
            // A state-variable filter in its topology-preserving form: stable at any cutoff
            float g = MathF.Tan(MathF.PI * cutoff / Rate);
            float k = 1f / q;
            float a1 = 1f / (1f + g * (g + k));
            float v0 = White();
            float v3 = v0 - ic2;
            float v1 = a1 * ic1 + g * a1 * v3;
            float v2 = ic2 + g * v1;
            ic1 = 2f * v1 - ic1;
            ic2 = 2f * v2 - ic2;
            float x = filter switch { Filter.Low => v2, Filter.Band => v1 * k, _ => v0 - k * v1 - v2 };
            float flutter = flutterDepth > 0f ? 1f - flutterDepth * (0.5f + 0.5f * MathF.Sin(MathF.Tau * flutterHz * t)) : 1f;
            S[start + i] += x * gain * flutter * Envelope(u, attack, power);
        }
        return this;
    }

    /// <summary>A short knock: a sine dropping in pitch under a fast fall, with a tick of noise on top.</summary>
    public SoundDesign Thump(float at, float seconds, float from, float to, float gain, float click = 0.5f)
    {
        Tone(at, seconds, Wave.Sine, from, to, gain, 0.03f, 3f);
        if (click > 0f) Noise(at, Math.Min(seconds, 0.03f), Filter.Band, 2400f, 1400f, gain * click, 1.2f, 0.05f, 3f);
        return this;
    }

    /// <summary>A rising pop of a bubble.</summary>
    public SoundDesign Bubble(float at, float pitch, float gain) =>
        Tone(at, 0.05f, Wave.Sine, pitch, pitch * 2.2f, gain, 0.1f, 2f);

    /// <summary>Several bubbles scattered over a span, placed by the sound's own noise.</summary>
    public SoundDesign Bubbles(float at, float span, int count, float low, float high, float gain)
    {
        for (int k = 0; k < count; k++)
        {
            float when = at + span * (k + 0.5f * (Unit() + 0.5f)) / count;
            Bubble(when, low + (high - low) * Unit(), gain * (0.6f + 0.4f * Unit()));
        }
        return this;
    }

    /// <summary>Clicks scattered over a span (gravel, a crackle, a ratchet), each a tick of filtered noise.</summary>
    public SoundDesign Crackle(float at, float span, int count, float pitch, float gain, bool even = false)
    {
        for (int k = 0; k < count; k++)
        {
            float when = at + span * (even ? k / (float)count : Unit());
            float tone = pitch * (0.7f + 0.6f * Unit());
            Noise(when, 0.012f, Filter.Band, tone, tone, gain * (0.5f + 0.5f * Unit()), 3f, 0.1f, 2f);
        }
        return this;
    }

    /// <summary>An echo of everything so far: <paramref name="delay"/> later, <paramref name="feedback"/> as loud each time.</summary>
    public SoundDesign Echo(float delay, float feedback)
    {
        int d = (int)(delay * Rate);
        for (int i = d; i < S.Length; i++) S[i] += S[i - d] * feedback;
        return this;
    }

    /// <summary>White noise in [-1, 1].</summary>
    private float White()
    {
        seed ^= seed << 13;
        seed ^= seed >> 17;
        seed ^= seed << 5;
        return (seed & 0xFFFFFF) / 8388608f - 1f;
    }

    /// <summary>A number in [0, 1) from the sound's own generator.</summary>
    private float Unit() => (White() + 1f) * 0.5f;

    /// <summary>The polynomial step that takes the alias out of a waveform's jump.</summary>
    private static float Blep(float t, float dt)
    {
        if (t < dt)
        {
            t /= dt;
            return t + t - t * t - 1f;
        }
        if (t > 1f - dt)
        {
            t = (t - 1f) / dt;
            return t * t + t + t + 1f;
        }
        return 0f;
    }
}
