using System;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.Audio;

/// <summary>The drum sounds of the <c>kit</c> instrument. A drum note's key is one of these.</summary>
public enum Drum
{
    Kick, Snare, HiHat, OpenHat, Crash, HighTom, MidTom, LowTom, Clap, Shaker,

    // Hand percussion (plan 31 · MU0): open to any song, written for the later regions
    HighConga, LowConga, Bongo, WoodBlock
}

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

    // Plan 31 · MU0: what the later instruments add. Every field keeps the old instruments exactly as they were.

    /// <summary>How the voice makes its sound: a wavetable (every instrument before plan 31), a plucked string or struck modes.</summary>
    public VoiceKind Kind { get; init; } = VoiceKind.Wavetable;

    /// <summary>The modern sound: how many detuned voices a unison instrument plays (2 when 0 and <see cref="Detune"/> is set).</summary>
    public int Unison { get; init; }

    /// <summary>The modern sound: how far across the stereo field the unison voices spread (0 together, 1 hard left and right).</summary>
    public float Spread { get; init; } = 0.5f;

    /// <summary>A plucked string: how long it rings before falling 60 dB, in seconds, at middle C (higher notes ring a little less).</summary>
    public float Ring { get; init; } = 1.5f;

    /// <summary>A plucked string: how bright the pluck is (0 a thumb, 1 a pick).</summary>
    public float PluckBright { get; init; } = 0.6f;

    /// <summary>A plucked string: where along it the pluck lands (0 to 0.5), which hollows out the harmonics it would not excite.</summary>
    public float PluckPosition { get; init; } = 0.2f;

    /// <summary>A plucked string: how much the string's loop dulls the tone each round (0 keeps it bright, 1 dulls it at once).</summary>
    public float PluckDamp { get; init; } = 0.5f;

    /// <summary>Struck modes (steel drum, log drum): each mode's frequency as a ratio of the note's.</summary>
    public float[] Ratios { get; init; } = Array.Empty<float>();

    /// <summary>Struck modes: each mode's level.</summary>
    public float[] Amps { get; init; } = Array.Empty<float>();

    /// <summary>Struck modes: each mode's time constant, in seconds.</summary>
    public float[] Decays { get; init; } = Array.Empty<float>();

    /// <summary>Struck modes: a short burst of noise at the strike (the stick, the hand), as a level.</summary>
    public float Strike { get; init; }
}

/// <summary>How an instrument's voice makes its sound (plan 31 · MU0).</summary>
public enum VoiceKind
{
    /// <summary>A band-limited wavetable, crossfading from a bright spectrum to a mellow one: every instrument before plan 31.</summary>
    Wavetable,

    /// <summary>A plucked string (Karplus–Strong with a fractional delay, so it can slide): the ukulele and the slide guitar.</summary>
    Pluck,

    /// <summary>A few decaying sine modes struck together: the steel drum and the log drum.</summary>
    Modal
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
            VibratoDelay = 0.2f, Noise = 0.01f, Gain = 0.52f, Unison = 3, Spread = 0.6f
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
            Sustain = 0.85f, Release = 0.3f, Vibrato = 0.1f, VibratoRate = 5f, VibratoDelay = 0.1f, Noise = 0.02f, Gain = 0.52f,
            Unison = 3, Spread = 0.6f
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
            Attack = 0.06f, Decay = 0.8f, Sustain = 0.8f, Release = 0.15f, Vibrato = 0.05f, OctaveShift = -1, Gain = 0.62f
        });
        Add(new Instrument
        {
            Name = "timpani", Bright = new[] { 1f, 0.3f, 0.15f }, Mellow = new[] { 1f }, BrightDecay = 0.05f, Attack = 0.002f,
            Decay = 0.5f, Sustain = 0f, Release = 0.3f, Noise = 0.25f, PitchStart = 0.8f, PitchTime = 0.05f, OctaveShift = -1,
            Gain = 0.87f
        });

        Add(new Instrument { Name = "kit", IsKit = true, Gain = 1f });

        // Plan 31 · MU0: Alola's instruments, open to any song, written for the modern sound. Plucked strings and struck
        // modes are voices of their own kinds (VoiceKind); the warm pad is a wavetable spread wide by its unison.
        Add(new Instrument
        {
            Name = "ukulele", Kind = VoiceKind.Pluck, Ring = 1.1f, PluckBright = 0.35f, PluckPosition = 0.18f, PluckDamp = 0.4f,
            Release = 0.06f, Gain = 4.6f
        });
        Add(new Instrument
        {
            Name = "slideguitar", Kind = VoiceKind.Pluck, Ring = 2.6f, PluckBright = 0.3f, PluckPosition = 0.12f, PluckDamp = 0.6f,
            PitchStart = -1.6f, PitchTime = 0.07f, Vibrato = 0.14f, VibratoRate = 4.6f, VibratoDelay = 0.35f, Release = 0.18f, Gain = 3.7f
        });
        Add(new Instrument
        {
            // A pan's note: the fundamental, its octave and its twelfth, each ringing a little less, and the stick's tick
            Name = "steeldrum", Kind = VoiceKind.Modal, Ratios = new[] { 1f, 2f, 3.01f, 4.05f }, Amps = new[] { 1f, 0.55f, 0.32f, 0.12f },
            Decays = new[] { 0.55f, 0.32f, 0.16f, 0.07f }, Strike = 0.12f, PitchStart = 0.12f, PitchTime = 0.02f, Release = 0.15f, Gain = 0.42f
        });
        Add(new Instrument
        {
            // A slit log: a woody fundamental and two modes far from its harmonics, gone quickly, with the mallet's knock
            Name = "logdrum", Kind = VoiceKind.Modal, Ratios = new[] { 1f, 2.76f, 5.4f }, Amps = new[] { 1f, 0.35f, 0.12f },
            Decays = new[] { 0.22f, 0.07f, 0.03f }, Strike = 0.2f, Release = 0.08f, OctaveShift = -1, Gain = 0.62f
        });
        Add(new Instrument
        {
            Name = "warmpad", Bright = new[] { 1f, 0.45f, 0.22f, 0.12f, 0.06f }, Mellow = new[] { 1f, 0.2f, 0.05f }, BrightDecay = 1.4f,
            BrightSustain = 0.25f, Detune = 14f, Unison = 4, Spread = 0.8f, Attack = 0.6f, Decay = 1.5f, Sustain = 0.9f, Release = 0.9f,
            Vibrato = 0.03f, VibratoRate = 0.8f, VibratoDelay = 0f, Gain = 0.62f
        });

        // Plan 31 · MU3: Kalos's instruments, open to any song, written for the modern sound
        Add(new Instrument
        {
            // Three reeds a few cents apart, the middle one true: the musette's shimmer, fuller and wider than the accordion
            Name = "musette", Bright = new[] { 1f, 0.75f, 0.55f, 0.45f, 0.35f, 0.25f, 0.16f, 0.1f }, Mellow = new[] { 1f, 0.6f, 0.4f, 0.28f, 0.18f, 0.1f },
            BrightDecay = 0.25f, BrightSustain = 0.5f, Detune = 13f, Unison = 3, Spread = 0.45f, Attack = 0.025f, Decay = 0.5f,
            Sustain = 0.85f, Release = 0.08f, Noise = 0.01f, Gain = 0.56f
        });
        Add(new Instrument
        {
            // A quill plucking near the string's end: bright, thin and quick to fade
            Name = "harpsichord", Kind = VoiceKind.Pluck, Ring = 1.4f, PluckBright = 0.95f, PluckPosition = 0.07f, PluckDamp = 0.25f,
            Release = 0.05f, Gain = 5.8f
        });
        Add(new Instrument
        {
            // Nylon strings under the thumb: warm and round, the light guitar of a café
            Name = "nylonguitar", Kind = VoiceKind.Pluck, Ring = 1.8f, PluckBright = 0.3f, PluckPosition = 0.22f, PluckDamp = 0.55f,
            Release = 0.1f, Gain = 3.6f
        });
        Add(new Instrument
        {
            // One bowed voice, darker than the violin, with a slower vibrato that comes in late
            Name = "cello", Bright = Falling(14, 1.1f), Mellow = Falling(9, 1.6f), BrightDecay = 0.25f, BrightSustain = 0.4f,
            Attack = 0.07f, Decay = 0.7f, Sustain = 0.88f, Release = 0.16f, Vibrato = 0.16f, VibratoRate = 5.1f,
            VibratoDelay = 0.22f, Noise = 0.014f, Gain = 0.48f
        });
        Add(new Instrument
        {
            // A full string orchestra: five voices spread wide, slower to speak than the section
            Name = "orchestra", Bright = Falling(14, 1.15f), Mellow = Falling(10, 1.65f), BrightDecay = 0.45f, BrightSustain = 0.4f,
            Detune = 10f, Unison = 5, Spread = 0.85f, Attack = 0.15f, Decay = 0.9f, Sustain = 0.9f, Release = 0.28f,
            Vibrato = 0.09f, VibratoRate = 5.2f, VibratoDelay = 0.2f, Noise = 0.01f, Gain = 0.6f
        });

        // Plan 31 · MU4: Alola's last instrument, beside MU0's
        Add(new Instrument
        {
            // A bamboo flute: hollower and breathier than the concert flute, sliding up into its notes, a slow wide vibrato
            Name = "bambooflute", Bright = new[] { 1f, 0.18f, 0.1f, 0.03f }, Mellow = new[] { 1f, 0.1f, 0.03f },
            BrightDecay = 0.2f, Attack = 0.06f, Decay = 0.5f, Sustain = 0.8f, Release = 0.14f,
            Vibrato = 0.22f, VibratoRate = 4.4f, VibratoDelay = 0.3f, Noise = 0.11f, PitchStart = -0.6f, PitchTime = 0.06f, Gain = 0.5f
        });

        // Plan 31 · MU5: Galar's instruments
        Add(new Instrument
        {
            // A brass band's cornets: rounder than the trumpet, three players a few cents apart, a band's slow vibrato
            Name = "cornet", Bright = Falling(14, 1.05f), Mellow = Falling(9, 1.6f), BrightDecay = 0.22f, BrightSustain = 0.35f,
            Detune = 6f, Unison = 3, Spread = 0.4f, Attack = 0.025f, Decay = 0.45f, Sustain = 0.8f, Release = 0.09f,
            Vibrato = 0.1f, VibratoRate = 5f, VibratoDelay = 0.3f, PitchStart = -0.5f, PitchTime = 0.03f, Gain = 0.68f
        });
        Add(new Instrument
        {
            // A chanter: a hard, reedy tone that starts at once and never decays, two reeds a few cents apart
            Name = "bagpipe", Bright = new[] { 1f, 0.9f, 0.75f, 0.6f, 0.55f, 0.4f, 0.32f, 0.22f, 0.16f, 0.1f },
            Mellow = new[] { 1f, 0.8f, 0.62f, 0.5f, 0.42f, 0.3f, 0.22f, 0.14f }, BrightDecay = 0.1f, BrightSustain = 0.6f,
            Detune = 5f, Spread = 0.25f, Attack = 0.012f, Decay = 1f, Sustain = 1f, Release = 0.05f, Noise = 0.02f, Gain = 0.57f
        });
        Add(new Instrument
        {
            // The pipes' drones: a buzzing held tone an octave below where it is written, slow to swell
            Name = "pipedrone", Bright = new[] { 1f, 0.7f, 0.55f, 0.45f, 0.35f, 0.28f, 0.2f, 0.14f }, Detune = 3f, Spread = 0.6f,
            Attack = 0.25f, Decay = 1f, Sustain = 1f, Release = 0.3f, Noise = 0.01f, OctaveShift = -1, Gain = 0.62f
        });
        Add(new Instrument
        {
            // A folk fiddle: brighter and scratchier than the violin, quick to speak, little vibrato
            Name = "fiddle", Bright = Falling(16, 0.95f), Mellow = Falling(12, 1.3f), BrightDecay = 0.12f, BrightSustain = 0.55f,
            Attack = 0.025f, Decay = 0.5f, Sustain = 0.82f, Release = 0.08f, Vibrato = 0.08f, VibratoRate = 6.2f,
            VibratoDelay = 0.3f, Noise = 0.03f, Gain = 0.5f
        });
        Add(new Instrument
        {
            // A tin whistle: thin and breathy, flicking up into each note
            Name = "tinwhistle", Bright = new[] { 1f, 0.28f, 0.12f, 0.05f }, Mellow = new[] { 1f, 0.16f, 0.05f },
            BrightDecay = 0.1f, Attack = 0.02f, Decay = 0.4f, Sustain = 0.82f, Release = 0.07f, Vibrato = 0.1f, VibratoRate = 5.6f,
            VibratoDelay = 0.3f, Noise = 0.08f, PitchStart = 0.8f, PitchTime = 0.025f, Gain = 0.52f
        });
        Add(new Instrument
        {
            // A frame drum: a deep thud that sinks a little and the tipper's slap, an octave below where it is written
            Name = "bodhran", Kind = VoiceKind.Modal, Ratios = new[] { 1f, 1.59f, 2.14f, 2.65f }, Amps = new[] { 1f, 0.45f, 0.25f, 0.12f },
            Decays = new[] { 0.18f, 0.08f, 0.05f, 0.03f }, Strike = 0.3f, PitchStart = 0.4f, PitchTime = 0.04f, Release = 0.08f,
            OctaveShift = -1, Gain = 0.66f
        });
        Add(new Instrument
        {
            // A stadium organ: every drawbar out, three voices and a quick rotating-speaker shimmer
            Name = "stadiumorgan", Bright = new[] { 1f, 0.85f, 0.6f, 0.55f, 0.2f, 0.4f, 0.1f, 0.3f, 0.1f, 0.15f },
            Detune = 6f, Unison = 3, Spread = 0.55f, Attack = 0.008f, Decay = 1f, Sustain = 1f, Release = 0.06f,
            Vibrato = 0.06f, VibratoRate = 6.8f, VibratoDelay = 0f, Gain = 0.53f
        });
        Add(new Instrument
        {
            // An overdriven electric guitar for power chords: the harsh, odd-leaning spectrum distortion gives, as a wavetable
            Name = "driveguitar", Bright = new[] { 1f, 0.75f, 0.85f, 0.5f, 0.6f, 0.38f, 0.45f, 0.28f, 0.32f, 0.2f, 0.24f, 0.14f, 0.16f, 0.1f, 0.1f, 0.06f },
            Mellow = new[] { 1f, 0.6f, 0.65f, 0.38f, 0.42f, 0.25f, 0.28f, 0.15f, 0.15f, 0.08f }, BrightDecay = 0.3f, BrightSustain = 0.5f,
            Detune = 8f, Spread = 0.5f, Attack = 0.004f, Decay = 0.9f, Sustain = 0.7f, Release = 0.07f, Noise = 0.025f, Gain = 0.63f
        });
    }
}
