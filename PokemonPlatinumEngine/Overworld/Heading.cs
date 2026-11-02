using System;
using System.Numerics;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// The eight ways the player may step (plan 30 · FM1, stage 1), clockwise from north. Only the player's own steering
/// speaks it: <see cref="Direction"/> stays the grid's four, and <c>Player.Facing</c> the cardinal last faced, which
/// talking, sight, scripts and the save read. GPU-free.
/// </summary>
public enum Heading { N, NE, E, SE, S, SW, W, NW }

/// <summary>What a <see cref="Heading"/> is on the grid: its cardinal, its two parts, its step and its angle.</summary>
public static class Headings
{
    /// <summary>Whether the heading is one of the four diagonals.</summary>
    public static bool IsDiagonal(this Heading heading) => ((int)heading & 1) == 1;

    /// <summary>The <see cref="Direction"/> a cardinal heading is; null for a diagonal.</summary>
    public static Direction? Cardinal(this Heading heading) => heading switch
    {
        Heading.N => Direction.Up,
        Heading.E => Direction.Right,
        Heading.S => Direction.Down,
        Heading.W => Direction.Left,
        _ => null
    };

    /// <summary>The heading a cardinal <see cref="Direction"/> is.</summary>
    public static Heading Of(Direction direction) => direction switch
    {
        Direction.Up => Heading.N,
        Direction.Right => Heading.E,
        Direction.Left => Heading.W,
        _ => Heading.S
    };

    /// <summary>A heading's two cardinal parts, the vertical one first; for a cardinal, the cardinal twice.</summary>
    public static (Direction Vertical, Direction Horizontal) Parts(this Heading heading) => heading switch
    {
        Heading.NE => (Direction.Up, Direction.Right),
        Heading.SE => (Direction.Down, Direction.Right),
        Heading.SW => (Direction.Down, Direction.Left),
        Heading.NW => (Direction.Up, Direction.Left),
        _ => (heading.Cardinal()!.Value, heading.Cardinal()!.Value)
    };

    /// <summary>The tiles a step this way moves by: x east, y south.</summary>
    public static (int Dx, int Dy) Delta(this Heading heading) => heading switch
    {
        Heading.N => (0, -1),
        Heading.NE => (1, -1),
        Heading.E => (1, 0),
        Heading.SE => (1, 1),
        Heading.S => (0, 1),
        Heading.SW => (-1, 1),
        Heading.W => (-1, 0),
        _ => (-1, -1)
    };

    /// <summary>
    /// The angle the body faces stepping this way, as <c>Player.YawOf</c> gives a cardinal's (south 0, east a quarter
    /// turn, north half a turn, west minus a quarter), the diagonals between.
    /// </summary>
    public static float Yaw(this Heading heading)
    {
        var (dx, dy) = heading.Delta();
        return MathF.Atan2(dx, dy);
    }

    /// <summary>
    /// The heading a movement vector holds (x east, y south, as <c>InputManager.GetMovementVector</c> gives it): the
    /// eight ways by the signs of its axes; null for none.
    /// </summary>
    public static Heading? FromVector(Vector2 vector)
    {
        int x = Math.Sign(vector.X), y = Math.Sign(vector.Y);
        return (x, y) switch
        {
            (0, -1) => Heading.N,
            (1, -1) => Heading.NE,
            (1, 0) => Heading.E,
            (1, 1) => Heading.SE,
            (0, 1) => Heading.S,
            (-1, 1) => Heading.SW,
            (-1, 0) => Heading.W,
            (-1, -1) => Heading.NW,
            _ => null
        };
    }

    /// <summary>
    /// The heading a stick's held directions are (<see cref="Core.StickReader"/> reads eight ways already); null for
    /// none, or for two opposite ways at once, which hold no heading.
    /// </summary>
    public static Heading? FromStick(Core.StickDirs held)
    {
        int x = (held.HasFlag(Core.StickDirs.Right) ? 1 : 0) - (held.HasFlag(Core.StickDirs.Left) ? 1 : 0);
        int y = (held.HasFlag(Core.StickDirs.Down) ? 1 : 0) - (held.HasFlag(Core.StickDirs.Up) ? 1 : 0);
        return FromVector(new Vector2(x, y));
    }

    /// <summary>
    /// The heading a frame of actions holds (the four direction actions, from the keys, the d-pad or the stick as
    /// <see cref="Core.InputBindings.Resolve"/> made them): the same bits a recording keeps, so stage 1 needs nothing
    /// more in a movie.
    /// </summary>
    public static Heading? Of(Core.ActionFrame frame)
    {
        int x = (frame.IsDown(Core.GameAction.Right) ? 1 : 0) - (frame.IsDown(Core.GameAction.Left) ? 1 : 0);
        int y = (frame.IsDown(Core.GameAction.Down) ? 1 : 0) - (frame.IsDown(Core.GameAction.Up) ? 1 : 0);
        return FromVector(new Vector2(x, y));
    }

    /// <summary>
    /// The grid's four-way answer to a movement vector, as the player has always been steered by the keys: the
    /// stronger axis wins and a tie (a diagonal) is the vertical one. Null for none.
    /// </summary>
    public static Direction? FourWay(Vector2 vector)
    {
        if (vector == Vector2.Zero) return null;
        return Math.Abs(vector.X) > Math.Abs(vector.Y)
            ? (vector.X > 0 ? Direction.Right : Direction.Left)
            : (vector.Y > 0 ? Direction.Down : Direction.Up);
    }
}
