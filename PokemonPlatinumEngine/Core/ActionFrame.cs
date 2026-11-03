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

    /// <summary>
    /// The stick's tilt across, −127 (full left) to 127 (full right), after the dead zone and quantised where it is read
    /// (plan 30 · FM7, <see cref="StickReader.Tilt"/>): what a recording keeps (format 4), so the game sees exactly the
    /// same numbers played back. Nought for a frame with no stick (the keyboard's, a test's); then the direction bits say
    /// the way (<see cref="InputManager.GetStick"/>). Only free movement reads it.
    /// </summary>
    public sbyte MoveX { get; init; }

    /// <summary>The stick's tilt down, −127 (full up) to 127 (full down), as <see cref="MoveX"/>.</summary>
    public sbyte MoveY { get; init; }

    /// <summary>A frame of free movement's stick (plan 30 · FM7): the direction bits the stick's eight ways set, and the tilt itself.</summary>
    public static ActionFrame Stick(sbyte x, sbyte y)
    {
        var held = new System.Collections.Generic.List<GameAction>();
        var dirs = StickReader.Ways(x / 127f, y / 127f);
        if ((dirs & StickDirs.Left) != 0) held.Add(GameAction.Left);
        if ((dirs & StickDirs.Right) != 0) held.Add(GameAction.Right);
        if ((dirs & StickDirs.Up) != 0) held.Add(GameAction.Up);
        if ((dirs & StickDirs.Down) != 0) held.Add(GameAction.Down);
        return None.Next(held.ToArray()) with { MoveX = x, MoveY = y };
    }

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
