using System;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.Audio;

/// <summary>The drum sounds of the <c>kit</c> instrument. A drum note's key is one of these.</summary>
public enum Drum { Kick, Snare, HiHat, OpenHat, Crash, HighTom, MidTom, LowTom, Clap, Shaker }

/// <summary>
/// One sound of the instrument bank: a wavetable voice (a bright and a mellow spectrum, crossfaded by a brightness
/// envelope) with optional FM, breath noise, vibrato, detuned unison and an ADSR envelope. Every field is a plain
/// number so instruments can be tuned without touching the synthesiser.
/// </summary>
public sealed class Instrument
{
    public required string Name { get; init; }

    /// <summary>Harmonic amplitudes (index 0 is the fundamental) heard at the start of a note.</summary>
    public float[] Bright { get; init; } = { 1f };

    /// <summary>The spectrum the note settles into; the bright one when null.</summary>
    public float[]? Mellow { get; init; }

    /// <summary>How long the bright spectrum takes to fade into the mellow one (time constant, seconds).</summary>
    public float BrightDecay { get; init; } = 0.2f;

    /// <summary>How much of the bright spectrum is left once the note has settled (0 to 1).</summary>
    public float BrightSustain { get; init; }

    public float Attack { get; init; } = 0.005f;

    /// <summary>Decay time constant towards <see cref="Sustain"/>, in seconds.</summary>
    public float Decay { get; init; } = 0.3f;

    public float Sustain { get; init; } = 0.8f;

    /// <summary>Release time constant, in seconds.</summary>
    public float Release { get; init; } = 0.08f;

    /// <summary>Vibrato depth in semitones, reached after <see cref="VibratoDelay"/>.</summary>
    public float Vibrato { get; init; }
    public float VibratoRate { get; init; } = 5.5f;
    public float VibratoDelay { get; init; } = 0.25f;

    /// <summary>A second voice detuned by this many cents and spread across the stereo field; 0 for one voice.</summary>
    public float Detune { get; init; }

    /// <summary>Frequency modulation: the modulator runs at this ratio of the note's frequency.</summary>
    public float FmRatio { get; init; }

    /// <summary>Modulation index at the start of the note; it decays to <see cref="FmSustain"/> × itself.</summary>
    public float FmIndex { get; init; }
    public float FmDecay { get; init; } = 0.3f;
    public float FmSustain { get; init; }

    /// <summary>Breath or bow noise mixed into the tone.</summary>
    public float Noise { get; init; }

    /// <summary>The note starts this many semitones off and glides to pitch over <see cref="PitchTime"/>.</summary>
    public float PitchStart { get; init; }
    public float PitchTime { get; init; } = 0.03f;

    /// <summary>Notes sound this many octaves away from where they are written (bass and piccolo parts).</summary>
    public int OctaveShift { get; init; }

    public float Gain { get; init; } = 1f;

    /// <summary>True for the drum kit, whose notes are <see cref="Drum"/> values rather than pitches.</summary>
    public bool IsKit { get; init; }
}

/// <summary>
/// The instruments songs can name, modelled on the sampled bank of the DS games: flutes and reeds, brass, strings,
/// piano, bells and mallets, plucked strings, a few synth leads, basses and a drum kit.
/// </summary>
public static class InstrumentBank
{
    private static readonly Dictionary<string, Instrument> instruments = new(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyCollection<string> Names => instruments.Keys;

    public static Instrument? Get(string name) => instruments.TryGetValue(name, out var i) ? i : null;

    /// <summary>Amplitudes falling as 1/n^power: power 1 is a sawtooth, 2 is soft.</summary>
    private static float[] Falling(int count, float power, bool oddOnly = false)
    {
        var h = new float[count];
        for (int n = 1; n <= count; n++)
        {
            if (oddOnly && n % 2 == 0) continue;
            h[n - 1] = 1f / MathF.Pow(n, power);
        }
        return h;
    }

    private static void Add(Instrument i) => instruments[i.Name] = i;

    static InstrumentBank()
    {
        // Woodwinds
        Add(new Instrument
        {
            Name = "flute", Bright = new[] { 1f, 0.35f, 0.12f, 0.05f }, Mellow = new[] { 1f, 0.22f, 0.06f },
            BrightDecay = 0.15f, Attack = 0.035f, Decay = 0.4f, Sustain = 0.85f, Release = 0.09f,
            Vibrato = 0.18f, VibratoRate = 5.2f, VibratoDelay = 0.22f, Noise = 0.05f, PitchStart = -0.25f, Gain = 0.69f
        });
        Add(new Instrument
        {
            Name = "ocarina", Bright = new[] { 1f, 0.08f, 0.04f }, Attack = 0.05f, Decay = 0.5f, Sustain = 0.8f,
            Release = 0.12f, Vibrato = 0.14f, VibratoRate = 4.8f, VibratoDelay = 0.3f, Noise = 0.03f, Gain = 0.69f
        });
        Add(new Instrument
        {
            Name = "piccolo", Bright = new[] { 1f, 0.25f, 0.08f }, Attack = 0.02f, Decay = 0.3f, Sustain = 0.8f,
            Release = 0.06f, Vibrato = 0.15f, VibratoRate = 6f, VibratoDelay = 0.18f, Noise = 0.04f, OctaveShift = 1, Gain = 0.57f
        });
        Add(new Instrument
        {
            Name = "clarinet", Bright = Falling(13, 1.1f, oddOnly: true), Mellow = Falling(9, 1.5f, oddOnly: true),
            BrightDecay = 0.12f, BrightSustain = 0.3f, Attack = 0.025f, Decay = 0.5f, Sustain = 0.85f, Release = 0.07f,
            Vibrato = 0.08f, VibratoRate = 5f, VibratoDelay = 0.3f, Noise = 0.015f, Gain = 0.61f
        });
        Add(new Instrument
        {
            Name = "oboe", Bright = new[] { 0.6f, 1f, 0.8f, 0.55f, 0.35f, 0.22f, 0.12f, 0.06f }, Attack = 0.03f,
            Decay = 0.4f, Sustain = 0.8f, Release = 0.07f, Vibrato = 0.12f, VibratoRate = 5.5f, VibratoDelay = 0.2f, Gain = 0.58f
        });
        Add(new Instrument
        {
            Name = "accordion", Bright = new[] { 1f, 0.7f, 0.5f, 0.4f, 0.3f, 0.2f, 0.12f }, Detune = 9f, Attack = 0.03f,
            Decay = 0.5f, Sustain = 0.85f, Release = 0.07f, Gain = 0.55f
        });

        // Brass
        Add(new Instrument
        {
            Name = "trumpet", Bright = Falling(16, 0.9f), Mellow = Falling(10, 1.4f), BrightDecay = 0.25f, BrightSustain = 0.45f,
            Attack = 0.02f, Decay = 0.4f, Sustain = 0.8f, Release = 0.07f, Vibrato = 0.12f, VibratoDelay = 0.25f,
            PitchStart = -0.6f, PitchTime = 0.035f, Gain = 0.64f
        });
        Add(new Instrument
        {
            Name = "brass", Bright = Falling(16, 1f), Mellow = Falling(10, 1.6f), BrightDecay = 0.18f, BrightSustain = 0.3f,
            Detune = 7f, Attack = 0.03f, Decay = 0.5f, Sustain = 0.75f, Release = 0.09f, PitchStart = -0.4f, Gain = 0.66f
        });
        Add(new Instrument
        {
            Name = "horn", Bright = Falling(8, 1.5f), Mellow = Falling(6, 2.2f), BrightDecay = 0.3f, BrightSustain = 0.3f,
            Attack = 0.05f, Decay = 0.6f, Sustain = 0.85f, Release = 0.12f, Vibrato = 0.06f, Gain = 0.61f
        });

        // Strings
        Add(new Instrument
        {
            Name = "strings", Bright = Falling(14, 1.2f), Mellow = Falling(10, 1.7f), BrightDecay = 0.4f, BrightSustain = 0.4f,
            Detune = 8f, Attack = 0.12f, Decay = 0.8f, Sustain = 0.9f, Release = 0.22f, Vibrato = 0.1f, VibratoRate = 5.4f,
            VibratoDelay = 0.2f, Noise = 0.01f, Gain = 0.52f
        });
        Add(new Instrument
        {
            Name = "violin", Bright = Falling(14, 1.05f), Mellow = Falling(10, 1.5f), BrightDecay = 0.2f, BrightSustain = 0.45f,
            Attack = 0.05f, Decay = 0.6f, Sustain = 0.85f, Release = 0.12f, Vibrato = 0.17f, VibratoRate = 5.8f,
            VibratoDelay = 0.15f, Noise = 0.012f, Gain = 0.54f
        });
        Add(new Instrument
        {
            Name = "pizzicato", Bright = Falling(10, 1.3f), Mellow = new[] { 1f, 0.2f }, BrightDecay = 0.05f,
            Attack = 0.002f, Decay = 0.18f, Sustain = 0f, Release = 0.06f, Gain = 1.05f
        });

        // Keys and mallets
        Add(new Instrument
        {
            Name = "piano", Bright = new[] { 1f, 0.55f, 0.35f, 0.22f, 0.15f, 0.1f, 0.06f, 0.04f }, Mellow = new[] { 1f, 0.25f, 0.06f },
            BrightDecay = 0.35f, BrightSustain = 0.1f, Attack = 0.002f, Decay = 0.9f, Sustain = 0f, Release = 0.12f,
            Detune = 3f, Gain = 0.99f
        });
        Add(new Instrument
        {
            Name = "epiano", Bright = new[] { 1f, 0.1f }, FmRatio = 1f, FmIndex = 1.6f, FmDecay = 0.25f, FmSustain = 0.15f,
            Attack = 0.002f, Decay = 1f, Sustain = 0f, Release = 0.15f, Gain = 0.67f
        });
        Add(new Instrument
        {
            Name = "bell", Bright = new[] { 1f }, FmRatio = 3.5f, FmIndex = 2.2f, FmDecay = 0.5f, FmSustain = 0.1f,
            Attack = 0.001f, Decay = 1.1f, Sustain = 0f, Release = 0.4f, Gain = 0.45f
        });
        Add(new Instrument
        {
            Name = "glockenspiel", Bright = new[] { 1f }, FmRatio = 4.7f, FmIndex = 1.0f, FmDecay = 0.12f, FmSustain = 0.05f,
            Attack = 0.001f, Decay = 0.55f, Sustain = 0f, Release = 0.25f, OctaveShift = 1, Gain = 0.64f
        });
        Add(new Instrument
        {
            Name = "celesta", Bright = new[] { 1f, 0.05f }, FmRatio = 2f, FmIndex = 0.9f, FmDecay = 0.1f,
            Attack = 0.001f, Decay = 0.5f, Sustain = 0f, Release = 0.2f, Gain = 0.59f
        });
        Add(new Instrument
        {
            Name = "marimba", Bright = new[] { 1f, 0f, 0f, 0.35f }, Mellow = new[] { 1f }, BrightDecay = 0.04f,
            Attack = 0.001f, Decay = 0.28f, Sustain = 0f, Release = 0.08f, Gain = 0.96f
        });
        Add(new Instrument
        {
            Name = "vibraphone", Bright = new[] { 1f, 0f, 0f, 0.2f }, Mellow = new[] { 1f }, BrightDecay = 0.1f,
            Attack = 0.002f, Decay = 1.2f, Sustain = 0f, Release = 0.3f, Vibrato = 0.08f, VibratoRate = 6f, VibratoDelay = 0f,
            Gain = 0.57f
        });
        Add(new Instrument
        {
            Name = "harp", Bright = Falling(8, 1.6f), Mellow = new[] { 1f, 0.2f, 0.05f }, BrightDecay = 0.08f,
            Attack = 0.002f, Decay = 0.7f, Sustain = 0f, Release = 0.2f, Gain = 0.67f
        });
        Add(new Instrument
        {
            Name = "guitar", Bright = Falling(12, 1.2f), Mellow = new[] { 1f, 0.35f, 0.1f }, BrightDecay = 0.12f,
            Attack = 0.002f, Decay = 0.6f, Sustain = 0f, Release = 0.1f, Detune = 2f, Gain = 1.06f
        });
        Add(new Instrument
        {
            Name = "organ", Bright = new[] { 1f, 0.8f, 0.5f, 0.45f, 0f, 0.3f, 0f, 0.2f }, Attack = 0.01f, Decay = 1f,
            Sustain = 1f, Release = 0.05f, Detune = 4f, Gain = 0.51f
        });

        // Synths: the square and triangle leads give the battle themes their handheld edge
        Add(new Instrument
        {
            Name = "square", Bright = Falling(15, 1f, oddOnly: true), Mellow = Falling(9, 1.15f, oddOnly: true),
            BrightDecay = 0.1f, BrightSustain = 0.5f, Attack = 0.003f, Decay = 0.4f, Sustain = 0.75f, Release = 0.04f,
            Vibrato = 0.12f, VibratoRate = 6f, VibratoDelay = 0.25f, Gain = 0.51f
        });
        Add(new Instrument
        {
            Name = "pulse", Bright = new[] { 1f, 0.9f, 0.6f, 0.3f, 0.1f, 0.12f, 0.2f, 0.25f, 0.18f, 0.08f, 0.02f, 0.06f },
            Attack = 0.003f, Decay = 0.3f, Sustain = 0.7f, Release = 0.04f, Vibrato = 0.1f, VibratoDelay = 0.25f, Gain = 0.48f
        });
        Add(new Instrument
        {
            Name = "sawlead", Bright = Falling(18, 1f), Mellow = Falling(12, 1.25f), BrightDecay = 0.15f, BrightSustain = 0.5f,
            Detune = 10f, Attack = 0.004f, Decay = 0.4f, Sustain = 0.75f, Release = 0.05f, Vibrato = 0.12f,
            VibratoDelay = 0.2f, Gain = 0.52f
        });
        Add(new Instrument
        {
            Name = "pad", Bright = new[] { 1f, 0.4f, 0.2f, 0.1f }, Mellow = new[] { 1f, 0.25f, 0.08f }, BrightDecay = 0.8f,
            Detune = 11f, Attack = 0.35f, Decay = 1.2f, Sustain = 0.85f, Release = 0.5f, Vibrato = 0.04f, VibratoDelay = 0f,
            Gain = 0.53f
        });
        Add(new Instrument
        {
            Name = "choir", Bright = new[] { 1f, 0.5f, 0.6f, 0.2f, 0.12f }, Detune = 7f, Attack = 0.18f, Decay = 1f,
            Sustain = 0.85f, Release = 0.3f, Vibrato = 0.1f, VibratoRate = 5f, VibratoDelay = 0.1f, Noise = 0.02f, Gain = 0.52f
        });

        // Basses (written an octave up, as bass parts are)
        Add(new Instrument
        {
            Name = "bass", Bright = new[] { 1f, 0.5f, 0.25f, 0.12f, 0.06f }, Mellow = new[] { 1f, 0.3f, 0.08f }, BrightDecay = 0.08f,
            Attack = 0.004f, Decay = 0.5f, Sustain = 0.45f, Release = 0.06f, OctaveShift = -1, Gain = 0.57f
        });
        Add(new Instrument
        {
            Name = "synthbass", Bright = Falling(12, 1f), Mellow = Falling(5, 1.6f), BrightDecay = 0.07f, BrightSustain = 0.15f,
            Attack = 0.003f, Decay = 0.35f, Sustain = 0.6f, Release = 0.04f, OctaveShift = -1, Gain = 0.56f
        });
        Add(new Instrument
        {
            Name = "tuba", Bright = Falling(8, 1.6f), Mellow = Falling(5, 2.2f), BrightDecay = 0.15f, BrightSustain = 0.3f,
            Attack = 0.03f, Decay = 0.5f, Sustain = 0.7f, Release = 0.08f, OctaveShift = -1, Gain = 0.51f
        });
        Add(new Instrument
        {
            Name = "contrabass", Bright = Falling(10, 1.4f), Mellow = Falling(6, 2f), BrightDecay = 0.2f, BrightSustain = 0.3f,
            Attack = 0.06f, Decay = 0.8f, Sustain = 0.8f, Release = 0.15f, Vibrato = 0.05f, OctaveShift = -1, Gain = 0.48f
        });
        Add(new Instrument
        {
            Name = "timpani", Bright = new[] { 1f, 0.3f, 0.15f }, Mellow = new[] { 1f }, BrightDecay = 0.05f, Attack = 0.002f,
            Decay = 0.5f, Sustain = 0f, Release = 0.3f, Noise = 0.25f, PitchStart = 0.8f, PitchTime = 0.05f, OctaveShift = -1,
            Gain = 0.87f
        });

        Add(new Instrument { Name = "kit", IsKit = true, Gain = 1f });
    }
}
