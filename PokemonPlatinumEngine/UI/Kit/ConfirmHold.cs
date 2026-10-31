using System;

namespace PokemonPlatinumEngine.UI.Kit;

/// <summary>
/// Holding to confirm (plan 12 · Q9; style guide, "Field menus and notices", "Holding to confirm"): a dangerous answer
/// (deleting a save, resetting the options, releasing a Pokémon) is taken only once the confirm button has been held on
/// it for <see cref="Seconds"/>, while the options' HOLD TO CONFIRM row is on. GPU-free and free of input: a screen
/// hands it the press and, every frame after, whether the button is still down.
/// </summary>
public sealed class ConfirmHold
{
    /// <summary>How long the button is held for a dangerous answer to be taken.</summary>
    public const float Seconds = 0.4f;

    /// <summary>The options' HOLD TO CONFIRM row, for the whole process (set by <c>GameEngine.ApplySettings</c>): off unless chosen.</summary>
    public static bool Asked { get; set; }

    /// <summary>Whether the button is being held on the dangerous answer now.</summary>
    public bool Holding { get; private set; }

    /// <summary>How long it has been held.</summary>
    public float Held { get; private set; }

    /// <summary>The share of the hold done, 0 to 1: what the ring shows.</summary>
    public float Fill => Holding ? Math.Clamp(Held / Seconds, 0f, 1f) : 0f;

    /// <summary>
    /// The confirm button pressed on a dangerous answer: true where the answer is to be given now (nobody asked for the
    /// hold); otherwise the hold begins and false is returned.
    /// </summary>
    public bool Press()
    {
        if (!Asked) return true;
        Holding = true;
        Held = 0f;
        return false;
    }

    /// <summary>
    /// A frame of the hold: <paramref name="down"/> is whether the confirm button is still down. True on the frame the
    /// hold is complete (the answer is given then, and the hold is over); let go early, the hold ends with nothing done.
    /// </summary>
    public bool Advance(float dt, bool down)
    {
        if (!Holding) return false;
        if (!down)
        {
            Stop();
            return false;
        }
        Held += Math.Max(0f, dt);
        if (Held < Seconds) return false;
        Stop();
        return true;
    }

    /// <summary>Puts the hold down with nothing done (the answer changed, the question put away).</summary>
    public void Stop()
    {
        Holding = false;
        Held = 0f;
    }
}
