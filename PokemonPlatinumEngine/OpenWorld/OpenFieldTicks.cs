using System;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>
/// A frame of an open field with a stick asked for at each tick and a look after each (plan 32 · OW5): what a script's
/// walk to a marker steers by, and what lets a trigger or a door crossed in the middle of a frame end that frame's
/// walking. GPU-free.
/// </summary>
public static class OpenFieldTicks
{
    /// <summary>
    /// The ticks the frame brings, each stepping the player by <paramref name="input"/>'s stick for it and then asking
    /// <paramref name="after"/>, which ends the frame there when it says so (the rest of the frame's ticks are dropped,
    /// as a hitch's are: the clock has counted them, so the same frames give the same ticks anywhere). Returns the
    /// ticks run. With the same stick every tick and nothing after, it is <see cref="OpenField.Advance(double, OpenInput, Action{OpenField})"/>.
    /// </summary>
    public static int Run(this OpenField field, double seconds, Func<OpenField, OpenInput> input, Func<OpenField, bool>? after = null)
    {
        int ticks = field.Clock.Advance(seconds);
        for (int i = 0; i < ticks; i++)
        {
            field.Player.Step(field.Map, input(field), field.Grants);
            if (after?.Invoke(field) == true) return i + 1;
        }
        return ticks;
    }

    /// <summary>
    /// A walk to a marker taken to its end with no clock (plan 32 · OW5): a skipped scene's, ticks no picture shows,
    /// as the grid's <c>FinishTheWalks</c> walks frames. It ends on the marker or gives up by itself (at most
    /// <paramref name="most"/> ticks, and then it is put on its marker). Returns the ticks stepped.
    /// </summary>
    public static int Finish(this OpenField field, OpenMarkerWalk walk, int most = OpenClock.TicksPerSecond * 120)
    {
        int ticks = 0;
        while (!walk.Done && ticks < most)
        {
            field.Player.Step(field.Map, walk.InputFor(field.Player), field.Grants);
            walk.After(field.Player);
            ticks++;
        }
        if (!walk.Done) walk.Finish(field.Player);
        return ticks;
    }
}
