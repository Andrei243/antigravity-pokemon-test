namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The game's actions as one frame has them (plan 16 · T11c): which are down and which went down this frame. It is
/// what a tool, a test or a recording hands the game in place of the keys (<see cref="InputManager.Feed"/>), so a game
/// with no window is steered through the same reads every screen already makes. GPU-free.
/// </summary>
public readonly record struct ActionFrame(uint Down, uint Pressed)
{
    /// <summary>A frame with nothing held.</summary>
    public static readonly ActionFrame None = new(0, 0);

    private static uint Bit(GameAction action) => 1u << (int)action;

    /// <summary>Whether the action is held this frame.</summary>
    public bool IsDown(GameAction action) => (Down & Bit(action)) != 0;

    /// <summary>Whether the action went down this frame.</summary>
    public bool IsPressed(GameAction action) => (Pressed & Bit(action)) != 0;

    /// <summary>The frame that follows this one with <paramref name="held"/> down: pressed is what is down now and wasn't before.</summary>
    public ActionFrame Next(params GameAction[] held)
    {
        uint down = 0;
        foreach (var action in held) down |= Bit(action);
        return new ActionFrame(down, down & ~Down);
    }

    /// <summary>A frame in which the actions go down (as the first frame they are held).</summary>
    public static ActionFrame Press(params GameAction[] actions) => None.Next(actions);
}
