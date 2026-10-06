using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Audio;

/// <summary>A sound effect: mono samples in [-1, 1] at the synthesiser's rate, ready for the mixer.</summary>
public sealed class SoundSample
{
    public string Name { get; }
    public float[] Samples { get; }

    public SoundSample(string name, float[] samples)
    {
        Name = name;
        Samples = samples;
    }

    public double Duration => Samples.Length / (double)Synthesizer.SampleRate;
}

/// <summary>Where in the game a sound belongs: what <c>docs/sound-effects.md</c> lists it under.</summary>
public enum SoundGroup { Menu, Field, Battle, Move }

/// <summary>
/// One sound of the bank: its name, its group, the original's sound effect it stands for (the name only, from the
/// decompilation's <c>pl_sound_data.json</c>, or null where the original has none or it isn't known), and where
/// this game plays it.
/// </summary>
public sealed record SoundEntry(string Name, SoundGroup Group, string? Original, string PlayedWhen);

/// <summary>
/// The game's sound effects (plan 05 · A3), synthesised in code the first time each is asked for with
/// <see cref="SoundDesign"/>: layered oscillators, bells, filtered noise, envelopes and pitch glides, our own
/// design after the kind of sound the original makes at the same moment. Everything here runs without an audio
/// device, so tests and <c>tools/MusicRender --sounds</c> can render and check every sound.
/// </summary>
public static partial class SoundBank
{
    /// <summary>Every sound, in the order <c>docs/sound-effects.md</c> lists them.</summary>
    public static readonly IReadOnlyList<SoundEntry> Entries = Catalogue();

    /// <summary>Every sound the bank makes, by name: what <c>AudioManager.PlaySound</c> and a script's <c>sound</c> may ask for.</summary>
    public static readonly string[] Names = Entries.Select(e => e.Name).ToArray();

    private static readonly ConcurrentDictionary<string, SoundSample> Made = new(StringComparer.OrdinalIgnoreCase);

    public static bool Exists(string name) => Array.FindIndex(Names, n => n.Equals(name, StringComparison.OrdinalIgnoreCase)) >= 0;

    /// <summary>The sound of that name, made on first use, or null for a name the bank doesn't know.</summary>
    public static SoundSample? Get(string name)
    {
        if (!Exists(name)) return null;
        return Made.GetOrAdd(name.ToLowerInvariant(), n => new SoundSample(n, Make(n)));
    }

    /// <summary>Makes every sound now, so the first blip in the game costs nothing on its frame.</summary>
    public static void Preload()
    {
        foreach (var name in Names) Get(name);
    }

    /// <summary>The sound a move of a type makes as it sets off (the hit has its own as it lands).</summary>
    public static string MoveSound(PokemonType type) => "move_" + type.ToString().ToLowerInvariant();

    /// <summary>
    /// The sound of a step onto a tile, as the original's <c>player_move.c</c> plays them: snow, a puddle, water
    /// ankle deep, mud that isn't deep, and very tall grass (ordinary tall grass makes none). Null for the rest.
    /// </summary>
    public static string? StepSound(TileBehavior onto) => onto switch
    {
        TileBehavior.ShallowSnow or TileBehavior.ShadedSnow or TileBehavior.DeepSnow or TileBehavior.DeeperSnow or TileBehavior.DeepestSnow => "step_snow",
        TileBehavior.Puddle => "step_puddle",
        TileBehavior.ShallowWater => "step_shallows",
        TileBehavior.Mud => "step_mud",
        TileBehavior.VeryTallGrass => "grass",
        _ => null
    };

    /// <summary>The sound of a status condition given, or null for none (a cure, a faint).</summary>
    public static string? StatusSound(StatusCondition status) => status switch
    {
        StatusCondition.Poison or StatusCondition.Toxic => "status_poison",
        StatusCondition.Burn => "status_burn",
        StatusCondition.Paralyze => "status_paralysis",
        StatusCondition.Sleep => "status_sleep",
        StatusCondition.Freeze => "status_freeze",
        _ => null
    };

    /// <summary>
    /// Brings a sound to its peak level and rounds its ends off (1.5 ms in, 4 ms out), so it never clicks, after
    /// taking out any offset its layers left.
    /// </summary>
    private static float[] Finish(float[] s, float peak)
    {
        float mean = s.Average();
        float max = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            s[i] -= mean;
            max = MathF.Max(max, MathF.Abs(s[i]));
        }
        float gain = max > 0f ? peak / max : 0f;
        int fadeIn = Math.Min(s.Length / 2, (int)(Synthesizer.SampleRate * 0.0015f));
        int fadeOut = Math.Min(s.Length / 2, (int)(Synthesizer.SampleRate * 0.004f));
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

    /// <summary>The pitch of a MIDI note (69 is A4, 440 Hz).</summary>
    private static float N(int midi) => 440f * MathF.Pow(2f, (midi - 69) / 12f);

    private static float[] Pitches(params int[] midi) => midi.Select(N).ToArray();
}
