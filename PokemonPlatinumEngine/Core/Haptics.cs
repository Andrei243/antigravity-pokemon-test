using System.Collections.Generic;

namespace PokemonPlatinumEngine.Core;

/// <summary>How hard the pad trembles (plan 12 · Q24): not at all unless chosen, at half strength, or whole.</summary>
public enum Rumble { Off, Light, Full }

/// <summary>One tremble of the pad: a strength for each motor (0 to 1) and how long it lasts, in seconds.</summary>
public readonly record struct Pulse(float Left, float Right, float Seconds);

/// <summary>
/// The pad's rumble (plan 12 · Q24): a PC convenience of our own, not Platinum's Rumble Pak. A table of named pulses,
/// most named after the sound that plays at the same moment, so a pulse fires where <see cref="AudioManager.PlaySound"/>
/// fires that sound and is timed as the log is shown, never as the core works it out; a critical hit has a pulse of
/// its own (<see cref="Critical"/>), fired by the battle's face at the hit's impact. Pulses go to raylib for the pad
/// <see cref="InputManager.LastDevice"/> names and nowhere else, and raylib times them, so nothing here reads a clock
/// and no picture can move. Tests feel what was asked for through <see cref="Listen"/>, as they hear sounds.
/// GPU-free: the motors are reached through <see cref="InputManager.Vibrate"/>, which does nothing without a window.
/// </summary>
public static class Haptics
{
    /// <summary>The critical hit's own pulse, which no sound shares.</summary>
    public const string Critical = "critical";

    /// <summary>
    /// Every pulse by name, at full strength. How strong each feels is the user's to judge on a pad; these are the
    /// first guesses: a blow weighs more the more it hurts, a ball's wobble is a tap and its click a knock.
    /// </summary>
    public static IReadOnlyDictionary<string, Pulse> Pulses { get; } = new Dictionary<string, Pulse>
    {
        ["hit_weak"] = new(0.15f, 0.25f, 0.08f),
        ["hit_normal"] = new(0.35f, 0.5f, 0.12f),
        ["hit_super"] = new(0.6f, 0.9f, 0.2f),
        [Critical] = new(1f, 1f, 0.25f),
        ["faint"] = new(0.5f, 0.2f, 0.35f),
        ["ball_shake"] = new(0.2f, 0.1f, 0.08f),
        ["ball_click"] = new(0.6f, 0.3f, 0.15f),
        ["ball_break"] = new(0.4f, 0.4f, 0.12f),
        ["bump"] = new(0.25f, 0f, 0.06f),
        ["ledge"] = new(0.3f, 0.3f, 0.1f),
        ["boulder"] = new(0.7f, 0.3f, 0.25f),
        ["rock_smash"] = new(0.8f, 0.5f, 0.2f),
        ["fish_bite"] = new(0.5f, 0.8f, 0.25f),
        ["thunder"] = new(0.6f, 0.6f, 0.4f)
    };

    /// <summary>The options' row in force, set by the engine as it applies the settings; Off until then.</summary>
    public static Rumble Strength { get; set; }

    /// <summary>A pulse at the row's strength: half of it under <see cref="Rumble.Light"/>.</summary>
    public static Pulse Scaled(Pulse pulse, Rumble strength) => strength switch
    {
        Rumble.Full => pulse,
        Rumble.Light => pulse with { Left = pulse.Left * 0.5f, Right = pulse.Right * 0.5f },
        _ => new Pulse(0f, 0f, 0f)
    };

    [System.ThreadStatic] private static List<string>? felt;

    /// <summary>Starts noting every pulse asked for on this thread (that would reach a pad), in a list that fills as they are.</summary>
    public static List<string> Listen() => felt = new List<string>();

    public static void StopListening() => felt = null;

    /// <summary>
    /// The pulse a sound brings, if it has one: what <see cref="AudioManager.PlaySound"/> asks for every sound it plays.
    /// </summary>
    public static bool FromSound(string soundName) => Pulses.ContainsKey(soundName) && Play(soundName);

    /// <summary>
    /// Trembles the pad with the named pulse: only with the row on, and only while the pad is the device in hand.
    /// True when it was asked of the pad.
    /// </summary>
    public static bool Play(string name)
    {
        if (Strength == Rumble.Off || InputManager.LastDevice != InputDevice.Pad) return false;
        if (!Pulses.TryGetValue(name, out var pulse)) return false;
        felt?.Add(name);
        var scaled = Scaled(pulse, Strength);
        InputManager.Vibrate(scaled.Left, scaled.Right, scaled.Seconds);
        return true;
    }
}
