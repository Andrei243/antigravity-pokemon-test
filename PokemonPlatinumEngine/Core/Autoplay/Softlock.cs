namespace PokemonPlatinumEngine.Core.Autoplay;

/// <summary>
/// The softlock detector of the playthrough bot (plan 16 · T7), GPU-free: told each frame what the game shows (a
/// signature of its state, place, line and menu) and whether the bot was acting on it, it fires once that has stood
/// unchanged for <see cref="Limit"/> seconds of game time while input went in. Free time in the field is the bot's
/// own to spend (a goal that fails is the policy's business), so the caller watches only what the game holds: a
/// script, a line, a battle, a screen or a fade.
/// </summary>
public sealed class Softlock
{
    /// <summary>Ten seconds of game time with nothing moving: a softlock.</summary>
    public const float Limit = 10f;

    private string? last;

    /// <summary>How long the signature has stood still, in seconds of game time.</summary>
    public float Still { get; private set; }

    /// <summary>The signature that stood still when the detector last fired.</summary>
    public string? Frozen { get; private set; }

    /// <summary>
    /// Notes a frame; true on the frame the signature has stood still for <paramref name="limit"/> seconds (once, until it
    /// changes). A frame that isn't watched starts the count again.
    /// </summary>
    public bool Observe(string signature, float dt, bool watched, float limit = Limit)
    {
        if (!watched || signature != last)
        {
            last = watched ? signature : null;
            Still = 0f;
            return false;
        }
        float before = Still;
        Still += dt;
        if (before < limit && Still >= limit)
        {
            Frozen = signature;
            return true;
        }
        return false;
    }
}
