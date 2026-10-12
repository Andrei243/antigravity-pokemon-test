using System;
using PokemonPlatinumEngine.Graphics;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// What someone in the field does on a script's cue (plan 11 · C11; <c>act &lt;who&gt; &lt;gesture&gt; [times]</c>):
/// an emote played over the idle, or for <see cref="Jump"/> the ledge hop's clip played on the spot.
/// </summary>
public enum Gesture { Wave, Nod, Cheer, Surprised, Bow, Shrug, Point, Laugh, Jump }

/// <summary>The face someone holds on a script's cue (<c>mood &lt;who&gt; &lt;mood&gt;</c>) until it changes or the place is come to again.</summary>
public enum Mood { Neutral, Happy, Surprised, Sad, Angry }

/// <summary>
/// Gestures and moods (plan 11 · C11): how long a gesture's round lasts and how its clock runs. A gesture is aged by
/// the field's own <c>dt</c>, as a bubble is, so a tool that fixes the frame clock sees it the same every run. No
/// drawing or input.
/// </summary>
public static class Acting
{
    /// <summary>A jump on the spot, in seconds: the hop's clip played where the jumper stands.</summary>
    public const float JumpSeconds = 0.4f;

    /// <summary>How high a jump on the spot lifts its jumper at its height, in texels of the field (32 to a tile).</summary>
    public const float JumpTexels = 8f;

    /// <summary>The most times a script may ask a gesture to play in a row.</summary>
    public const int MostTimes = 9;

    /// <summary>One round of a gesture, in seconds: its emote's length, or a jump's.</summary>
    public static float Length(Gesture gesture) =>
        gesture == Gesture.Jump ? JumpSeconds : CharacterAnimation.Duration(CharacterAnimation.EmoteOf(gesture));

    /// <summary>How long a gesture played <paramref name="times"/> times in a row lasts.</summary>
    public static float Total(Gesture gesture, int times) => Length(gesture) * Math.Max(1, times);

    /// <summary>Seconds into the round under way of a gesture <paramref name="age"/> seconds old.</summary>
    public static float RoundAge(Gesture gesture, float age)
    {
        float length = Length(gesture);
        if (length <= 0f) return 0f;
        float round = age % length;
        return round < 0f ? round + length : round;
    }

    /// <summary>
    /// Ages a gesture by <paramref name="dt"/>: null once it has played as many times as it was asked to, which is
    /// how whoever plays it, and a script waiting for it, knows it is over.
    /// </summary>
    public static Gesture? Tick(Gesture? gesture, ref float age, int times, float dt)
    {
        if (gesture is not { } playing) return null;
        age += dt;
        if (age < Total(playing, times)) return playing;
        age = 0f;
        return null;
    }
}
