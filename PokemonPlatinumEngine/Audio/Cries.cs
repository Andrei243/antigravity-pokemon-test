using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Audio;

/// <summary>
/// How a cry is played, after the original's <c>POKECRY_*</c> modes (<c>src/sound_playback.c</c>): the same cry,
/// pitched as the DS pitches a sample (faster and higher, or slower and lower), cut short, or doubled by an echo.
/// </summary>
public enum CryMode
{
    /// <summary>As it is: a send-out, a wild Pokémon appearing, the party's summary, an evolution.</summary>
    Normal,
    /// <summary>A third of a second, fading over its last sixth.</summary>
    Half,
    /// <summary>A Pokémon met in the field by a script: a semitone higher, with a detuned echo beside it.</summary>
    FieldEvent,
    /// <summary>Fainting: three and a half semitones lower, and as much slower.</summary>
    Faint,
    /// <summary>Sent out in a pinch (a status condition, or its HP bar not green): a semitone and a half lower.</summary>
    Pinch,
    PinchHalf,
    /// <summary>The Pokédex's entry.</summary>
    Pokedex
}

/// <summary>
/// What makes a species' cry its own (plan 05 · A4), worked out from its data alone, the same on every run:
/// its pitch from its size (weight and height), the number of its syllables, their vowels and the shape of each
/// from its name, and its timbre from its type and its family, so that a line's cries sound related and grow
/// deeper as it evolves into something bigger. Our own design: no cry of the games is copied or imitated note for note.
/// </summary>
public sealed record CryVoice(
    string Name,
    float Pitch,
    float Seconds,
    CrySyllable[] Syllables,
    CryTimbre Timbre,
    bool Grand)
{
    /// <summary>The voice of a species, or of one of its forms (whose size and types it takes, and whose name tunes it).</summary>
    public static CryVoice Of(PokemonSpecies species, string? form = null)
    {
        var f = species.Form(form);
        float height = Math.Max(0.1f, f?.Height ?? species.Height);
        float weight = Math.Max(0.1f, f?.Weight ?? species.Weight);
        var types = f?.Types is { Count: > 0 } ft ? ft : new List<PokemonType> { species.PrimaryType }.Concat(species.SecondaryType is { } st ? new[] { st } : Array.Empty<PokemonType>()).ToList();
        string own = f?.Name ?? species.Name;
        bool grand = species.Legendary || species.Mythical;

        var (root, steps) = PokemonGenomes.Family(species);
        var mine = new CryRandom(Cries.Hash(own));
        var family = new CryRandom(Cries.Hash(root.Name));

        // Smaller is higher: a pebble of a Pokémon squeaks around 1 kHz, a whale booms under 150 Hz
        float pitch = 1150f * MathF.Pow(weight + 1f, -0.26f) * MathF.Pow(height + 0.3f, -0.18f);
        pitch *= 0.88f + 0.24f * mine.Next();
        if (grand) pitch *= 0.8f;
        pitch = Math.Clamp(pitch, 65f, 1500f);

        float seconds = 0.45f + 0.25f * MathF.Log10(weight + 1f) + 0.06f * steps + 0.1f * mine.Next();
        if (grand) seconds *= 1.15f;
        seconds = Math.Clamp(seconds, 0.35f, 1.45f);

        return new CryVoice(own, pitch, seconds, CrySyllable.Of(species.Name, mine), CryTimbre.Of(types, family, pitch), grand);
    }
}

/// <summary>How each part of a cry moves: its pitch from start to end (ratios of the voice's pitch), with a turn in between.</summary>
public enum CryContour { Rise, Fall, Arch, Dip, Trill }

/// <summary>One syllable of a cry: its share of the cry's length, its vowel (two formants, Hz) and its contour.</summary>
public sealed record CrySyllable(float Share, float Formant1, float Formant2, CryContour Contour, float Span)
{
    /// <summary>The name's vowel groups (one to four), each with the vowel that leads it; the letters around a group shape it.</summary>
    public static CrySyllable[] Of(string name, CryRandom random)
    {
        string n = new string(name.ToLowerInvariant().Where(char.IsLetter).ToArray());
        var groups = new List<(char Vowel, int Start, int End)>();
        for (int i = 0; i < n.Length; i++)
        {
            if (!IsVowel(n[i])) continue;
            int start = i;
            while (i + 1 < n.Length && IsVowel(n[i + 1])) i++;
            groups.Add((n[start], start, i));
        }
        if (groups.Count == 0) groups.Add(('a', 0, 0));
        // One group can be two calls; more than four run together
        if (groups.Count == 1 && random.Next() < 0.45f) groups.Add(groups[0]);
        while (groups.Count > 4)
        {
            int k = groups.Count - 2;
            groups[k] = (groups[k].Vowel, groups[k].Start, groups[k + 1].End);
            groups.RemoveAt(k + 1);
        }

        var parts = new CrySyllable[groups.Count];
        float total = 0f;
        var shares = new float[groups.Count];
        for (int k = 0; k < groups.Count; k++)
        {
            // A syllable closed by consonants is clipped; the last is drawn out
            var (vowel, start, end) = groups[k];
            int tail = 0;
            for (int i = end + 1; i < n.Length && !IsVowel(n[i]); i++) tail++;
            shares[k] = (0.7f + 0.6f * random.Next()) * (tail >= 2 ? 0.75f : 1f) * (k == groups.Count - 1 ? 1.5f : 1f);
            total += shares[k];
        }
        for (int k = 0; k < groups.Count; k++)
        {
            var (vowel, start, end) = groups[k];
            var (f1, f2) = Formants(vowel);
            // The letters around it choose its shape: sharp consonants rise, soft ones fall
            char before = start > 0 ? n[start - 1] : n[0];
            int shape = (before + n[end] * 7 + k * 13) % 5;
            var contour = (CryContour)shape;
            if (k == groups.Count - 1 && contour == CryContour.Rise && random.Next() < 0.6f) contour = CryContour.Fall;
            parts[k] = new CrySyllable(shares[k] / total, f1, f2, contour, 1.12f + 0.45f * random.Next());
        }
        return parts;
    }

    private static bool IsVowel(char c) => c is 'a' or 'e' or 'i' or 'o' or 'u' or 'y';

    /// <summary>The first two formants of a vowel as a voice sings it.</summary>
    private static (float, float) Formants(char vowel) => vowel switch
    {
        'a' => (800f, 1250f),
        'e' => (500f, 1850f),
        'i' or 'y' => (320f, 2300f),
        'o' => (520f, 900f),
        _ => (340f, 780f)
    };
}

/// <summary>The sound of a voice: what its source is made of and what roughens it. A family shares one, nudged by its own types.</summary>
public sealed record CryTimbre(
    float Saw, float Pulse, float Sine, float Duty,
    float FmRatio, float FmIndex,
    float Breath, float BreathHigh,
    float GrowlHz, float Growl,
    float VibratoHz, float Vibrato,
    float Formants, float Echo)
{
    public static CryTimbre Of(IReadOnlyList<PokemonType> types, CryRandom family, float pitch)
    {
        var t = Base(types[0]);
        if (types.Count > 1) t = Blend(t, Base(types[1]), 0.35f);
        float N(float spread) => 1f + spread * (family.Next() * 2f - 1f);
        // Small voices sit their vowels higher, as a child's do
        float formants = Math.Clamp(MathF.Pow(pitch / 300f, 0.3f), 0.75f, 1.6f) * N(0.1f);
        return t with
        {
            Saw = t.Saw * N(0.2f), Pulse = t.Pulse * N(0.2f), Sine = t.Sine * N(0.2f),
            Duty = Math.Clamp(t.Duty * N(0.25f), 0.12f, 0.5f),
            GrowlHz = t.GrowlHz * N(0.25f), VibratoHz = t.VibratoHz * N(0.25f), Vibrato = t.Vibrato * N(0.3f),
            Breath = t.Breath * N(0.25f), Formants = formants
        };
    }

    private static CryTimbre Blend(CryTimbre a, CryTimbre b, float w) => new(
        L(a.Saw, b.Saw), L(a.Pulse, b.Pulse), L(a.Sine, b.Sine), L(a.Duty, b.Duty),
        w > 0.5f ? b.FmRatio : a.FmRatio, L(a.FmIndex, b.FmIndex),
        L(a.Breath, b.Breath), L(a.BreathHigh, b.BreathHigh),
        L(a.GrowlHz, b.GrowlHz), L(a.Growl, b.Growl),
        L(a.VibratoHz, b.VibratoHz), L(a.Vibrato, b.Vibrato), 1f, L(a.Echo, b.Echo));

    private static float L(float a, float b) => a + (b - a) * 0.35f;

    //                                                  saw  pulse sine duty  fmR  fmI  breath high  growl  depth vibHz vib   form echo
    private static CryTimbre Base(PokemonType type) => type switch
    {
        PokemonType.Normal => new(0.60f, 0.20f, 0.30f, 0.40f, 2.0f, 0.0f, 0.10f, 0.0f, 28f, 0.10f, 6.0f, 0.020f, 1f, 0.00f),
        PokemonType.Fire => new(0.70f, 0.10f, 0.15f, 0.35f, 2.0f, 0.0f, 0.35f, 0.3f, 32f, 0.30f, 5.0f, 0.020f, 1f, 0.00f),
        PokemonType.Water => new(0.15f, 0.10f, 0.80f, 0.50f, 2.0f, 0.6f, 0.05f, 0.0f, 12f, 0.25f, 7.0f, 0.050f, 1f, 0.05f),
        PokemonType.Grass => new(0.20f, 0.10f, 0.70f, 0.50f, 2.0f, 0.2f, 0.25f, 0.8f, 20f, 0.05f, 5.0f, 0.020f, 1f, 0.00f),
        PokemonType.Electric => new(0.20f, 0.70f, 0.20f, 0.25f, 2.0f, 0.0f, 0.10f, 0.9f, 50f, 0.30f, 10f, 0.030f, 1f, 0.00f),
        PokemonType.Ice => new(0.10f, 0.10f, 0.80f, 0.50f, 3.5f, 1.5f, 0.10f, 1.0f, 20f, 0.00f, 6.0f, 0.015f, 1f, 0.15f),
        PokemonType.Fighting => new(0.40f, 0.60f, 0.10f, 0.30f, 2.0f, 0.0f, 0.15f, 0.2f, 30f, 0.15f, 4.0f, 0.015f, 1f, 0.00f),
        PokemonType.Poison => new(0.60f, 0.20f, 0.30f, 0.40f, 1.5f, 0.4f, 0.10f, 0.3f, 9f, 0.20f, 4.0f, 0.060f, 1f, 0.00f),
        PokemonType.Ground => new(0.80f, 0.20f, 0.20f, 0.40f, 2.0f, 0.0f, 0.20f, 0.0f, 22f, 0.40f, 4.0f, 0.015f, 1f, 0.00f),
        PokemonType.Flying => new(0.10f, 0.10f, 0.90f, 0.50f, 2.0f, 0.2f, 0.10f, 1.0f, 20f, 0.00f, 9.0f, 0.040f, 1f, 0.05f),
        PokemonType.Psychic => new(0.10f, 0.10f, 0.80f, 0.50f, 1.5f, 1.0f, 0.05f, 0.5f, 20f, 0.00f, 5.0f, 0.040f, 1f, 0.20f),
        PokemonType.Bug => new(0.70f, 0.30f, 0.10f, 0.25f, 2.0f, 0.0f, 0.10f, 0.6f, 35f, 0.50f, 12f, 0.020f, 1f, 0.00f),
        PokemonType.Rock => new(0.40f, 0.50f, 0.20f, 0.35f, 2.0f, 0.0f, 0.35f, 0.0f, 18f, 0.30f, 3.0f, 0.010f, 1f, 0.00f),
        PokemonType.Ghost => new(0.10f, 0.10f, 0.70f, 0.50f, 1.5f, 0.6f, 0.20f, 0.4f, 6f, 0.15f, 3.0f, 0.080f, 1f, 0.40f),
        PokemonType.Dragon => new(0.90f, 0.20f, 0.10f, 0.40f, 2.0f, 0.0f, 0.30f, 0.2f, 25f, 0.45f, 4.0f, 0.020f, 1f, 0.10f),
        PokemonType.Steel => new(0.20f, 0.30f, 0.50f, 0.30f, 1.41f, 2.5f, 0.05f, 0.8f, 20f, 0.00f, 5.0f, 0.010f, 1f, 0.15f),
        PokemonType.Dark => new(0.40f, 0.40f, 0.20f, 0.35f, 2.0f, 0.0f, 0.20f, 0.0f, 24f, 0.30f, 4.0f, 0.020f, 1f, 0.05f),
        _ => new(0.10f, 0.10f, 0.80f, 0.50f, 2.0f, 0.8f, 0.05f, 1.0f, 20f, 0.00f, 8.0f, 0.030f, 1f, 0.10f) // Fairy
    };
}

/// <summary>A small generator seeded by a name: the same numbers on every run and every machine.</summary>
public sealed class CryRandom
{
    private uint state;

    public CryRandom(uint seed) => state = seed == 0 ? 0x9E3779B9u : seed;

    /// <summary>A number in [0, 1).</summary>
    public float Next()
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return (state & 0xFFFFFF) / 16777216f;
    }
}

/// <summary>
/// Every Pokémon's cry, synthesised from its <see cref="CryVoice"/> the first time it is asked for: a source of
/// saw, pulse, sine and FM at the voice's pitch, sung through two formant filters for each syllable's vowel, with
/// breath, a growl and a vibrato, then played in one of the original's modes. The last few dozen are kept.
/// </summary>
public static class Cries
{
    private const int Rate = Synthesizer.SampleRate;

    /// <summary>The peak every cry is brought to: louder than any sound effect, as the original's are.</summary>
    public const float Peak = 0.6f;

    /// <summary>How many cries are kept made; a Pokédex scrolled through all of them would otherwise keep every one.</summary>
    public const int Kept = 48;

    private static readonly object Gate = new();
    private static readonly Dictionary<(string, CryMode), SoundSample> made = new();
    private static readonly LinkedList<(string, CryMode)> order = new();

    /// <summary>A Pokémon's cry in a mode (its form's, if it is in one).</summary>
    public static SoundSample Get(Pokemon pokemon, CryMode mode = CryMode.Normal) => Get(pokemon.Species, pokemon.Form, mode);

    /// <summary>A species' cry (or one of its forms') in a mode, made now if it isn't kept.</summary>
    public static SoundSample Get(PokemonSpecies species, string? form = null, CryMode mode = CryMode.Normal)
    {
        var key = ((species.Form(form)?.Name ?? species.Name).ToLowerInvariant(), mode);
        lock (Gate)
        {
            if (made.TryGetValue(key, out var kept))
            {
                order.Remove(key);
                order.AddFirst(key);
                return kept;
            }
        }
        var sample = new SoundSample("cry " + key.Item1, Play(Make(CryVoice.Of(species, form)), mode));
        lock (Gate)
        {
            if (made.TryAdd(key, sample))
            {
                order.AddFirst(key);
                while (order.Count > Kept)
                {
                    made.Remove(order.Last!.Value);
                    order.RemoveLast();
                }
            }
            return made[key];
        }
    }

    /// <summary>How many cries are made and kept now.</summary>
    internal static int KeptCount
    {
        get { lock (Gate) return made.Count; }
    }

    /// <summary>Makes the cries a scene is about to need on a worker, so none is made on the frame that plays it.</summary>
    public static void Request(IEnumerable<Pokemon> pokemon)
    {
        var list = pokemon.ToList();
        Task.Run(() =>
        {
            foreach (var p in list)
            {
                Get(p);
                if (p.IsFainted == false) Get(p, CryMode.Faint);
            }
        });
    }

    /// <summary>
    /// The mode a Pokémon cries in as it is sent out: in a pinch when it has a status condition or its HP bar
    /// isn't green (half its HP or less, on the original's 48-pixel bar), unless its HP is full.
    /// </summary>
    public static CryMode SendOutMode(Pokemon p)
    {
        if (p.Status is not StatusCondition.None and not StatusCondition.Faint) return CryMode.Pinch;
        if (p.CurrentHP >= p.MaxHP) return CryMode.Normal;
        int pixels = p.CurrentHP * 48 / Math.Max(1, p.MaxHP);
        return pixels > 24 ? CryMode.Normal : CryMode.Pinch;
    }

    /// <summary>A name's own seed (FNV-1a over its letters): the same on every run, unlike a string's hash code.</summary>
    public static uint Hash(string name)
    {
        uint h = 2166136261u;
        foreach (char c in name.ToLowerInvariant())
        {
            h ^= c;
            h *= 16777619u;
        }
        return h;
    }

    // ------------------------------------------------------------------ the voice

    /// <summary>The cry as the voice sings it, before any mode.</summary>
    public static float[] Make(CryVoice voice)
    {
        int n = (int)(voice.Seconds * Rate);
        var s = new float[n];
        var t = voice.Timbre;
        var noise = new CryRandom(Hash(voice.Name) ^ 0xA5A5A5A5u);
        var f1 = new Svf();
        var f2 = new Svf();
        var breath = new Svf();
        double phase = 0, fmPhase = 0;

        // Where each syllable starts and ends, with a short breath between
        var starts = new float[voice.Syllables.Length + 1];
        for (int k = 0; k < voice.Syllables.Length; k++) starts[k + 1] = starts[k] + voice.Syllables[k].Share;
        float gap = Math.Min(0.04f, 0.25f / voice.Syllables.Length) / voice.Seconds;

        for (int i = 0; i < n; i++)
        {
            float u = i / (float)n;
            float time = i / (float)Rate;
            int k = 0;
            while (k < voice.Syllables.Length - 1 && u >= starts[k + 1]) k++;
            var syl = voice.Syllables[k];
            float within = (u - starts[k]) / Math.Max(1e-4f, syl.Share);

            // The syllable's envelope: a quick onset, a hold that sags, a release before the next
            float onset = Math.Min(1f, within * syl.Share * voice.Seconds / 0.018f);
            float release = Math.Clamp((1f - within) * syl.Share / Math.Max(1e-4f, gap), 0f, 1f);
            float sag = k == voice.Syllables.Length - 1 ? MathF.Pow(1f - within, 0.7f) : 1f - 0.25f * within;
            float env = onset * release * sag;

            // The pitch along the syllable's contour, with the voice's vibrato
            float shape = syl.Contour switch
            {
                CryContour.Rise => within,
                CryContour.Fall => 1f - within,
                CryContour.Arch => MathF.Sin(MathF.PI * within),
                CryContour.Dip => 1f - MathF.Sin(MathF.PI * within),
                _ => 0.5f + 0.5f * MathF.Sin(MathF.Tau * 9f * within)
            };
            float f = voice.Pitch * MathF.Pow(syl.Span, shape - 0.5f) * (1f + t.Vibrato * MathF.Sin(MathF.Tau * t.VibratoHz * time));
            float dt = Math.Min(0.45f, f / Rate);
            fmPhase += f * t.FmRatio / Rate;
            fmPhase -= Math.Floor(fmPhase);
            float fm = t.FmIndex * env * MathF.Sin(MathF.Tau * (float)fmPhase);
            phase += dt;
            phase -= Math.Floor(phase);
            float p = (float)phase;

            float saw = 2f * p - 1f - Blep(p, dt);
            float pulse = (p < t.Duty ? 1f : -1f) + Blep(p, dt) - Blep((p - t.Duty + 1f) % 1f, dt) - (2f * t.Duty - 1f);
            float sine = MathF.Sin(MathF.Tau * p + fm);
            float source = t.Saw * saw + t.Pulse * pulse + t.Sine * sine;

            // Sung through the vowel: two formants, and a little of the source as it is so the pitch stays clear
            float fa = syl.Formant1 * t.Formants, fb = syl.Formant2 * t.Formants;
            float voiced = 0.35f * source + f1.Band(source, fa, 5f) * 1.1f + f2.Band(source, fb, 6f) * 0.8f;

            // Breath: noise low through the first formant, or high and hissing
            float white = noise.Next() * 2f - 1f;
            float air = t.BreathHigh > 0.5f ? breath.High(white, 2400f * t.Formants) : breath.Band(white, fb, 1.5f);
            float growl = 1f - t.Growl * (0.5f + 0.5f * MathF.Sin(MathF.Tau * t.GrowlHz * time));
            s[i] = (voiced + t.Breath * air) * growl * env;
        }

        // A grand voice, and a ghost's, ring in the hall they cry in
        float echo = voice.Grand ? Math.Max(t.Echo, 0.3f) : t.Echo;
        if (echo > 0f)
        {
            int d = (int)(0.09f * Rate);
            for (int i = d; i < n; i++) s[i] += s[i - d] * echo * 0.6f;
        }
        return s;
    }

    // ------------------------------------------------------------------ the modes

    /// <summary>A cry played in one of the original's modes, finished: at its peak, its ends rounded.</summary>
    public static float[] Play(float[] cry, CryMode mode)
    {
        float[] s = mode switch
        {
            CryMode.Faint => Pitched(cry, -3.5f),
            CryMode.Pinch => Pitched(cry, -1.5f),
            CryMode.PinchHalf => Shortened(Pitched(cry, -1.5f)),
            CryMode.Half => Shortened((float[])cry.Clone()),
            CryMode.FieldEvent => Chorus(Pitched(cry, 1f), Pitched(cry, 0.3125f)),
            _ => (float[])cry.Clone()
        };
        return Finish(s);
    }

    /// <summary>Played faster or slower, as the DS pitches a sample: up a semitone is a sixteenth shorter.</summary>
    private static float[] Pitched(float[] s, float semitones)
    {
        double speed = Math.Pow(2, semitones / 12.0);
        int n = (int)(s.Length / speed);
        var o = new float[n];
        for (int i = 0; i < n; i++)
        {
            double at = i * speed;
            int a = (int)at;
            float frac = (float)(at - a);
            float x0 = s[Math.Min(a, s.Length - 1)], x1 = s[Math.Min(a + 1, s.Length - 1)];
            o[i] = x0 + (x1 - x0) * frac;
        }
        return o;
    }

    /// <summary>Cut to twenty frames of the original's 60, the last ten fading out.</summary>
    private static float[] Shortened(float[] s)
    {
        int n = Math.Min(s.Length, Rate * 20 / 60), fade = Rate * 10 / 60;
        var o = new float[n];
        for (int i = 0; i < n; i++)
        {
            int left = n - i;
            o[i] = s[i] * (left < fade ? left / (float)fade : 1f);
        }
        return o;
    }

    /// <summary>The cry with its echo played beside it, half as loud.</summary>
    private static float[] Chorus(float[] main, float[] echo)
    {
        var o = new float[Math.Max(main.Length, echo.Length)];
        for (int i = 0; i < o.Length; i++)
            o[i] = (i < main.Length ? main[i] : 0f) + (i < echo.Length ? echo[i] * 0.5f : 0f);
        return o;
    }

    /// <summary>Takes out any offset, brings the cry to its peak and rounds its ends (2 ms in, 12 ms out).</summary>
    private static float[] Finish(float[] s)
    {
        if (s.Length == 0) return s;
        float mean = s.Average(), max = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            s[i] -= mean;
            max = MathF.Max(max, MathF.Abs(s[i]));
        }
        float gain = max > 0f ? Peak / max : 0f;
        int fadeIn = Math.Min(s.Length / 2, (int)(Rate * 0.002f)), fadeOut = Math.Min(s.Length / 2, (int)(Rate * 0.012f));
        for (int i = 0; i < s.Length; i++)
        {
            float g = gain;
            if (i < fadeIn) g *= i / (float)fadeIn;
            int left = s.Length - 1 - i;
            if (left < fadeOut) g *= left / (float)fadeOut;
            s[i] *= g;
        }
        return s;
    }

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

    /// <summary>A state-variable filter in its topology-preserving form (stable at any cutoff).</summary>
    private sealed class Svf
    {
        private float ic1, ic2;

        private (float Low, float Band, float High) Run(float x, float cutoff, float q)
        {
            float g = MathF.Tan(MathF.PI * Math.Clamp(cutoff, 20f, Rate * 0.45f) / Rate);
            float k = 1f / q;
            float a1 = 1f / (1f + g * (g + k));
            float v3 = x - ic2;
            float v1 = a1 * ic1 + g * a1 * v3;
            float v2 = ic2 + g * v1;
            ic1 = 2f * v1 - ic1;
            ic2 = 2f * v2 - ic2;
            return (v2, v1, x - k * v1 - v2);
        }

        public float Band(float x, float cutoff, float q) => Run(x, cutoff, q).Band;

        public float High(float x, float cutoff) => Run(x, cutoff, 0.7f).High;
    }
}
