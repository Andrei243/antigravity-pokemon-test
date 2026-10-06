using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static PokemonPlatinumEngine.Audio.SoundDesign;

namespace PokemonPlatinumEngine.Audio;

/// <summary>A loop of the field's background sound: weather, water, a cave (plan 05 · A7).</summary>
public enum AmbienceBed { Rain, HeavyRain, Hail, Wind, Blizzard, Sandstorm, Waterfall, River, Waves, Cave }

/// <summary>One bed heard at a moment: how loud (0 to 1, the bed's own level at 1) and from where (-1 left, 1 right).</summary>
public readonly record struct AmbienceLayer(AmbienceBed Bed, float Gain, float Pan);

/// <summary>
/// One bed of the set: the original's sound it stands for (its name only, from the decompilation's
/// <c>pl_sound_data.json</c>, or null where the original has none) and when this game plays it.
/// </summary>
public sealed record AmbienceEntry(AmbienceBed Bed, string? Original, string HeardWhen);

/// <summary>
/// The ambience beds, synthesised in code with <see cref="SoundDesign"/> like the sound effects, each a loop that
/// joins itself without a seam: it is made a second longer than it lasts and its last second is crossfaded into
/// its first. Every bed is brought to the ambience bus's own level (<see cref="Loudness"/>), so a layer's gain
/// says only how near or how strong it is. Beds are made on a worker the first time they are wanted
/// (<see cref="TryGet"/>) or all at once at start-up (<see cref="RequestAll"/>).
/// </summary>
public static class Ambience
{
    /// <summary>The crossfade that joins a bed's end to its start.</summary>
    public const float JoinSeconds = 1f;

    public static readonly IReadOnlyList<AmbienceEntry> Entries = new[]
    {
        new AmbienceEntry(AmbienceBed.Rain, "SEQ_SE_DP_T_AME", "It rains where the player stands."),
        new AmbienceEntry(AmbienceBed.HeavyRain, "SEQ_SE_DP_T_OOAME", "Heavy rain or a thunderstorm."),
        new AmbienceEntry(AmbienceBed.Hail, null, "Hail."),
        new AmbienceEntry(AmbienceBed.Wind, "SEQ_SE_DP_KAZE", "Snow falls (softly in light snow, harder in heavy snow)."),
        new AmbienceEntry(AmbienceBed.Blizzard, "SEQ_SE_DP_KAZE2", "A blizzard."),
        new AmbienceEntry(AmbienceBed.Sandstorm, null, "A sandstorm."),
        new AmbienceEntry(AmbienceBed.Waterfall, null, "A waterfall is near: louder the nearer, from its side of the screen."),
        new AmbienceEntry(AmbienceBed.River, null, "Running water is near."),
        new AmbienceEntry(AmbienceBed.Waves, "SEQ_SE_DP_NAMI", "The sea or a lake is near: louder the more water is round the player."),
        new AmbienceEntry(AmbienceBed.Cave, null, "In a cave: a low rumble and drips.")
    };

    private static readonly ConcurrentDictionary<AmbienceBed, SoundSample> Made = new();
    private static readonly ConcurrentDictionary<AmbienceBed, Task> Making = new();

    /// <summary>How long a bed lasts before it starts again.</summary>
    public static float LoopSeconds(AmbienceBed bed) => bed is AmbienceBed.Cave or AmbienceBed.River ? 12f : 8f;

    /// <summary>The bed, made now on this thread if it isn't made yet.</summary>
    public static SoundSample Get(AmbienceBed bed) => Made.GetOrAdd(bed, b => new SoundSample("ambience_" + b.ToString().ToLowerInvariant(), Make(b)));

    /// <summary>The bed if it is made; otherwise null, and it is made on a worker meanwhile.</summary>
    public static SoundSample? TryGet(AmbienceBed bed)
    {
        if (Made.TryGetValue(bed, out var sample)) return sample;
        Making.GetOrAdd(bed, b => Task.Run(() => Get(b)));
        return null;
    }

    /// <summary>Starts making every bed on a worker, so none is missing when the field first asks for it.</summary>
    public static void RequestAll()
    {
        foreach (var bed in Enum.GetValues<AmbienceBed>()) TryGet(bed);
    }

    private static float[] Make(AmbienceBed bed)
    {
        float loop = LoopSeconds(bed);
        var d = new SoundDesign(loop + JoinSeconds, 0x5EED0000u + (uint)bed * 7919u);
        float all = loop + JoinSeconds;
        switch (bed)
        {
            case AmbienceBed.Rain:
                // A steady hiss with a body under it, and drops patting on everything
                d.Noise(0, all, Filter.High, 3200f, 3200f, 0.5f, 0.7f, 0.001f, 0f);
                d.Noise(0, all, Filter.Band, 1300f, 1300f, 0.3f, 0.8f, 0.001f, 0f);
                d.Crackle(0, all, (int)(all * 90), 4200f, 0.22f);
                break;
            case AmbienceBed.HeavyRain:
                // Broader and lower: a downpour, with a rumble of water running off
                d.Noise(0, all, Filter.High, 2400f, 2400f, 0.55f, 0.7f, 0.001f, 0f);
                d.Noise(0, all, Filter.Band, 850f, 850f, 0.45f, 0.7f, 0.001f, 0f);
                d.Noise(0, all, Filter.Low, 320f, 320f, 0.35f, 0.7f, 0.001f, 0f);
                d.Crackle(0, all, (int)(all * 220), 3400f, 0.2f);
                break;
            case AmbienceBed.Hail:
                // Hard little stones: sharp ticks over a thin hiss
                d.Noise(0, all, Filter.High, 4200f, 4200f, 0.18f, 0.7f, 0.001f, 0f);
                d.Crackle(0, all, (int)(all * 130), 2800f, 0.55f);
                d.Crackle(0, all, (int)(all * 60), 5200f, 0.35f);
                break;
            case AmbienceBed.Wind:
                // Gusts that swell and die, and a low breath under them
                Gusts(d, all, 420f, 900f, 1.4f, 0.55f);
                d.Noise(0, all, Filter.Low, 240f, 240f, 0.3f, 0.7f, 0.001f, 0f, flutterHz: 0.13f, flutterDepth: 0.5f);
                break;
            case AmbienceBed.Blizzard:
                // A gale, and the high whistle of wind round corners
                Gusts(d, all, 600f, 1500f, 1.6f, 0.6f);
                d.Noise(0, all, Filter.Low, 300f, 300f, 0.4f, 0.7f, 0.001f, 0f, flutterHz: 0.21f, flutterDepth: 0.4f);
                d.Noise(0, all, Filter.Band, 1500f, 1500f, 0.22f, 7f, 0.001f, 0f, flutterHz: 0.37f, flutterDepth: 0.8f);
                break;
            case AmbienceBed.Sandstorm:
                // A dry wind with grit in it
                Gusts(d, all, 500f, 1100f, 1.2f, 0.5f);
                d.Noise(0, all, Filter.High, 5200f, 5200f, 0.16f, 0.7f, 0.001f, 0f, flutterHz: 0.29f, flutterDepth: 0.5f);
                d.Crackle(0, all, (int)(all * 400), 6400f, 0.12f);
                break;
            case AmbienceBed.Waterfall:
                // A roar: deep, wide and without a break
                d.Noise(0, all, Filter.Low, 900f, 900f, 0.6f, 0.7f, 0.001f, 0f);
                d.Noise(0, all, Filter.Band, 320f, 320f, 0.5f, 0.6f, 0.001f, 0f);
                d.Noise(0, all, Filter.High, 3000f, 3000f, 0.14f, 0.7f, 0.001f, 0f);
                break;
            case AmbienceBed.River:
                // Water running over stones: a soft rush and a babble of bubbles
                d.Noise(0, all, Filter.Band, 900f, 900f, 0.25f, 0.9f, 0.001f, 0f, flutterHz: 0.5f, flutterDepth: 0.3f);
                d.Bubbles(0, all, (int)(all * 26), 300f, 900f, 0.28f);
                d.Bubbles(0, all, (int)(all * 9), 900f, 1600f, 0.16f);
                break;
            case AmbienceBed.Waves:
                // A wave every four seconds: it gathers, breaks into foam and draws back over a low hush
                for (float at = -2f; at < all; at += 4f)
                {
                    float start = Math.Max(0f, at), cut = start - at;
                    d.Noise(start, 4f - cut, Filter.Band, 380f, 1500f, 0.55f, 0.7f, 0.45f, 1.6f);
                    float foam = Math.Max(0f, at + 1.6f);
                    if (foam < all) d.Noise(foam, 2.2f, Filter.High, 4200f, 2600f, 0.3f, 0.7f, 0.1f, 2f);
                }
                d.Noise(0, all, Filter.Low, 200f, 200f, 0.12f, 0.7f, 0.001f, 0f);
                break;
            case AmbienceBed.Cave:
                // A low rumble of the rock, and a drip now and then that rings round the walls
                d.Noise(0, all, Filter.Low, 110f, 110f, 0.4f, 0.7f, 0.001f, 0f, flutterHz: 0.08f, flutterDepth: 0.3f);
                d.Bubbles(0, all, 9, 1100f, 2300f, 0.3f);
                d.Echo(0.23f, 0.35f);
                break;
        }
        return Join(d.S, (int)(loop * Rate));
    }

    /// <summary>Wind in gusts: overlapping swells of noise, each a band that rises and falls as it blows.</summary>
    private static void Gusts(SoundDesign d, float all, float low, float high, float spacing, float gain)
    {
        int k = 0;
        for (float at = -spacing; at < all; at += spacing, k++)
        {
            float length = spacing * 2.2f;
            float start = Math.Max(0f, at);
            float from = k % 3 == 0 ? low : low * 1.15f, to = k % 2 == 0 ? high : high * 0.8f;
            d.Noise(start, length - (start - at), Filter.Band, from, to, gain, 1.3f, 0.4f, 0.9f);
        }
    }

    /// <summary>
    /// Folds the last <see cref="JoinSeconds"/> into the first with an equal-power crossfade, so the loop of
    /// <paramref name="loop"/> samples runs on from its end into its start without a seam, takes out any offset and
    /// brings the bed to the ambience bus's level.
    /// </summary>
    private static float[] Join(float[] s, int loop)
    {
        int join = s.Length - loop;
        var o = new float[loop];
        for (int i = 0; i < loop; i++)
        {
            if (i < join)
            {
                float u = i / (float)join;
                o[i] = s[i] * MathF.Sqrt(u) + s[loop + i] * MathF.Sqrt(1f - u);
            }
            else o[i] = s[i];
        }
        float mean = o.Average();
        double sum = 0;
        for (int i = 0; i < o.Length; i++)
        {
            o[i] -= mean;
            sum += o[i] * o[i];
        }
        float rms = (float)Math.Sqrt(sum / o.Length);
        float gain = rms > 0f ? Loudness.Linear(Loudness.AmbienceBedLevel) / rms : 0f;
        for (int i = 0; i < o.Length; i++) o[i] *= gain;
        return o;
    }
}
