using System;

namespace PokemonPlatinumEngine.UI.Kit;

/// <summary>
/// A pair of opposite keys in a long list (up and down, left and right): a press is one step, and a key kept down
/// steps again after <see cref="Delay"/>, every <see cref="Interval"/> for its first <see cref="SlowSteps"/> repeats
/// and every <see cref="FastInterval"/> from then on. The steps follow from how long the key has been held,
/// whatever the frame rate. Free of input and GPU calls: the screen says what was pressed and what is down.
/// </summary>
public sealed class HeldKey
{
    public const float Delay = 0.32f, Interval = 0.08f, FastInterval = 0.04f;
    public const int SlowSteps = 10;

    /// <summary>A frame longer than this counts as this long, so a hitch doesn't run the list on by itself.</summary>
    private const float LongestFrame = 0.1f;

    private int direction, repeats;
    private float held, due;

    /// <summary>The steps last given were a held key's, not a press: a list stops at its ends for those.</summary>
    public bool Repeating { get; private set; }

    /// <summary>Forgets the key: nothing repeats until the next press.</summary>
    public void Release()
    {
        direction = 0;
        Repeating = false;
    }

    /// <summary>
    /// The steps to take this frame, signed. <paramref name="pressed"/> is the direction pressed this frame and
    /// <paramref name="down"/> the one held (-1, 0 or 1 each). Only a key that was pressed here repeats, so one
    /// still down from the screen before does nothing.
    /// </summary>
    public int Advance(float dt, int pressed, int down)
    {
        if (pressed != 0)
        {
            direction = Math.Sign(pressed);
            repeats = 0;
            held = 0f;
            due = Delay;
            Repeating = false;
            return direction;
        }

        if (direction == 0) return 0;
        if (down != direction)
        {
            Release();
            return 0;
        }

        held += Math.Clamp(dt, 0f, LongestFrame);
        int steps = 0;
        while (held >= due)
        {
            steps++;
            repeats++;
            due += repeats >= SlowSteps ? FastInterval : Interval;
        }
        if (steps > 0) Repeating = true;
        return steps * direction;
    }
}
