using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Audio;

/// <summary>The Pokédex's cry page's cries and its scope (plan 08 · P18). GPU-free: it makes numbers.</summary>
public static partial class Cries
{
    /// <summary>How many shaped cries are kept; a page being turned through its controls makes a few and needs no more.</summary>
    public const int ShapedKept = 12;

    private static readonly Dictionary<(string, CryShape), SoundSample> shaped = new();
    private static readonly LinkedList<(string, CryShape)> shapedOrder = new();

    /// <summary>
    /// A species' cry (or one of its forms') as the Pokédex's entry plays it, bent by a shape: pitched, with a second
    /// voice beside it, rung in a hall or heard through a wall, and brought back to the cry bus's level. The same on
    /// every run. A plain shape is <see cref="Get(PokemonSpecies, string?, CryMode)"/>'s own Pokédex cry, sample for
    /// sample. A pan is not in the samples: it is the place the cry is played at (<see cref="CryShape.PanPosition"/>).
    /// </summary>
    public static SoundSample Shaped(PokemonSpecies species, string? form, CryShape shape)
    {
        shape = shape.Clamped();
        var plain = Get(species, form, CryMode.Pokedex);
        // A pan moves the cry from one side to the other without changing it
        var made = shape with { Stereo = CryStereo.Chorus, StereoAmount = shape.Stereo == CryStereo.Chorus ? shape.StereoAmount : 0 };
        if (made.IsPlain) return plain;

        var key = ((species.Form(form)?.Name ?? species.Name).ToLowerInvariant(), made);
        lock (Gate)
        {
            if (shaped.TryGetValue(key, out var kept))
            {
                shapedOrder.Remove(key);
                shapedOrder.AddFirst(key);
                return kept;
            }
        }
        var sample = new SoundSample($"cry {key.Item1} {made.Key}", Bend((float[])plain.Samples.Clone(), made));
        lock (Gate)
        {
            if (shaped.TryAdd(key, sample))
            {
                shapedOrder.AddFirst(key);
                while (shapedOrder.Count > ShapedKept)
                {
                    shaped.Remove(shapedOrder.Last!.Value);
                    shapedOrder.RemoveLast();
                }
            }
            return shaped[key];
        }
    }

    /// <summary>The pitch, then the chorus, the filter and the reverb, in that order, and the finish every cry gets.</summary>
    private static float[] Bend(float[] cry, CryShape shape)
    {
        float[] s = shape.Pitch == 0 ? cry : Pitched(cry, shape.Pitch);
        if (shape.Stereo == CryStereo.Chorus && shape.StereoAmount > 0)
        {
            // A second voice a hair above the first, as loud as the chorus is turned up (at most as loud as FieldEvent's)
            var voice = Pitched(s, 0.3125f);
            float level = shape.StereoAmount / (float)CryShape.MostChorus;
            var mixed = new float[Math.Max(s.Length, voice.Length)];
            for (int i = 0; i < mixed.Length; i++)
                mixed[i] = (i < s.Length ? s[i] : 0f) + (i < voice.Length ? voice[i] * 0.6f * level : 0f);
            s = mixed;
        }
        if (shape.Room == CryRoom.Filter && shape.RoomAmount > 0) s = LowPassed(s, 9000.0 * Math.Pow(0.72, shape.RoomAmount));
        if (shape.Room == CryRoom.Reverb && shape.RoomAmount > 0) s = Echoed(s, 0.075f, Math.Min(0.66f, 0.06f * shape.RoomAmount));
        return Finish(s);
    }

    /// <summary>Everything above <paramref name="cutoff"/> hertz softened (a one-pole filter, run twice for a steeper wall).</summary>
    private static float[] LowPassed(float[] s, double cutoff)
    {
        float a = (float)(1.0 - Math.Exp(-2.0 * Math.PI * cutoff / Rate));
        var o = new float[s.Length];
        float y1 = 0f, y2 = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            y1 += a * (s[i] - y1);
            y2 += a * (y1 - y2);
            o[i] = y2;
        }
        return o;
    }

    /// <summary>Makes a shaped cry on a worker, so that none is made on the frame that plays it. Nothing waits for it.</summary>
    public static void RequestShaped(PokemonSpecies species, string? form, CryShape shape) =>
        System.Threading.Tasks.Task.Run(() => Shaped(species, form, shape));

    // ------------------------------------------------------------------ the scope

    /// <summary>
    /// The cry's wave as a scope's columns (plan 08 · P18): the samples cut into <paramref name="columns"/> equal runs,
    /// each with the lowest and the highest of its samples, and whether the playhead (a place in the cry, 0 to 1) has
    /// gone past it. Pure numbers: the same samples and playhead give the same columns on every run.
    /// </summary>
    public static ScopeColumn[] Scope(float[] samples, float playhead, int columns)
    {
        var o = new ScopeColumn[Math.Max(0, columns)];
        if (samples.Length == 0) return o;
        for (int c = 0; c < o.Length; c++)
        {
            int from = (int)((long)c * samples.Length / o.Length), to = Math.Max(from + 1, (int)((long)(c + 1) * samples.Length / o.Length));
            float low = 0f, high = 0f;
            for (int i = from; i < to && i < samples.Length; i++)
            {
                low = Math.Min(low, samples[i]);
                high = Math.Max(high, samples[i]);
            }
            o[c] = new ScopeColumn(low, high, (c + 0.5f) / o.Length <= playhead);
        }
        return o;
    }
}
