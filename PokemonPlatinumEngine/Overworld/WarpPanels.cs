using System;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// Warp panels as the original's field plays them (plan 02 · S11, part b: the floors of Team Galactic's
/// headquarters; <c>src/overlay005/field_control.c</c>, <c>Field_CheckTransition</c>, and
/// <c>src/field_map_change.c</c>, <c>FieldSystem_StartMapChangeWarpTask</c>). A panel is a tile of
/// <see cref="TileBehavior.WarpPanel"/> with a warp on it. Whoever ends a step on one is carried to the tile its warp
/// names, which is the panel at the far end, and stands on that panel facing as they did when they stepped on: the
/// original hands the player's own facing to the place they come to, where a door or a flight of stairs puts its
/// traveller a step beyond itself and turns them the way they come out. Arriving is not a step, so the far panel does
/// not carry them straight back: they step off it and on again for that. A tile with a panel's look and no behaviour
/// carries nobody (the green pad on the ground floor that the way out of the headquarters lands on), and a panel
/// whose warp leads to such a tile is a way with no way back.
/// On the way the traveller turns on the spot (<see cref="Spin"/>): the original's warp animation, which the game
/// plays out before the picture closes and again as it opens. No drawing and no input.
/// </summary>
public static class WarpPanels
{
    /// <summary>How long the traveller turns before the picture closes, and after it has opened on the far panel.</summary>
    public const float SpinUp = 0.5f, SpinDown = 0.3f;

    /// <summary>How long each quarter turn of the spin takes.</summary>
    public const float QuarterTurn = 1f / 14f;

    /// <summary>The ways someone turns through, clockwise seen from above.</summary>
    private static readonly Direction[] Turn = { Direction.Down, Direction.Left, Direction.Up, Direction.Right };

    /// <summary>Whether a tile is a warp panel: the behaviour, whatever stands on it.</summary>
    public static bool IsPanel(Map map, int x, int y) => map.InBounds(x, y) && map.BehaviourAt(x, y) == TileBehavior.WarpPanel;

    /// <summary>The warp a panel carries by; null for a tile that is no panel, or a panel that leads nowhere.</summary>
    public static Warp? At(Map map, int x, int y) => IsPanel(map, x, y) ? map.GetWarpAt(x, y) : null;

    /// <summary>Whether a warp is a panel's: taken from a tile of the panels' behaviour.</summary>
    public static bool Carries(Map map, Warp warp) => IsPanel(map, warp.SourceX, warp.SourceY);

    /// <summary>
    /// Where someone who takes a warp of a map is put down, and which way they face there: on the far panel, facing
    /// as they did, for a panel's warp; for any other, where and how the warp itself says.
    /// </summary>
    public static (int X, int Y, Direction Facing) Arrival(Map map, Warp warp, Direction facing) =>
        (warp.TargetX, warp.TargetY, Carries(map, warp) ? facing : warp.TargetFacing);

    /// <summary>
    /// The way a traveller faces a moment into the spin, having faced <paramref name="from"/> as it began: a quarter
    /// turn every <see cref="QuarterTurn"/>, so the spin always comes round through all four ways.
    /// </summary>
    public static Direction Spin(Direction from, float seconds)
    {
        int start = Array.IndexOf(Turn, from);
        int quarters = (int)MathF.Floor(MathF.Max(0f, seconds) / QuarterTurn);
        return Turn[(start + quarters) % Turn.Length];
    }
}
