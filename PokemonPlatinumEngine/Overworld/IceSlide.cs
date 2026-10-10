using System;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// The field's ice as the original's code slides on it (plan 01 · M9, part 2b; <c>src/player_move.c</c>,
/// <c>PlayerAvatar_TileMove_Ice</c>). Once a step has brought someone onto ice, each next step takes itself the way
/// they were going, until something is in the way or they come onto ground that isn't ice. Where the ice slopes (the
/// Snowpoint Gym's bowl) the slide gathers speed going down and loses it going up, by the tilt of the tile underfoot
/// (<c>PlayerAvatar_CheckIceHeightChange</c>, <c>PlayerAvatar_UpdateIceSpeedFromHeightChange</c>): a speed of nought to
/// three, which sets the pace of the next step (<c>PlayerAvatar_SetIceMovement</c>). With no speed left to climb, or
/// stopped by something on the way up, the slider slips a tile back down the way they came, slowly, still facing up
/// the slope, and stands. Stopped anywhere, the speed is gone. On flat ice nothing of this happens: the slide keeps
/// the run's pace, as it always did. A slider going at a speed of one or more breaks a snowball in front of them and
/// slides on through where it stood (<c>ov5_021E06A8</c>); slower, the snowball stops them like anything else. No
/// drawing and no input: the player plays it out.
/// </summary>
public static class IceSlide
{
    /// <summary>The fastest a slide goes (<c>AVATAR_MOVE_SPEED_3</c>).</summary>
    public const int MaxSpeed = 3;

    /// <summary>Which way the ice underfoot tilts along the way someone goes.</summary>
    public enum Incline { Level, Down, Up }

    /// <summary>What the next step of a slide comes to.</summary>
    public enum Outcome
    {
        /// <summary>On a tile, at the speed given.</summary>
        Slide,
        /// <summary>The slider stands where they are, the speed gone.</summary>
        Stop,
        /// <summary>A tile back down the slope, slowly, and then they stand.</summary>
        SlipBack
    }

    /// <summary>The next step of a slide: what it comes to, the step itself (a walk, or the refused one) and the speed after it.</summary>
    public readonly record struct Next(Outcome Outcome, FieldStep Step, int Speed);

    /// <summary>
    /// Which way the ice under someone tilts along the way they go (<c>PlayerAvatar_CheckIceHeightChange</c>): the
    /// ground a quarter of a tile ahead of the tile's middle against the middle, so it is the slope of the tile they
    /// stand on, not of the one they are going onto.
    /// </summary>
    public static Incline InclineOf(Map map, int x, int y, Direction dir)
    {
        var (dx, dy) = FieldMovement.Delta(dir);
        float here = map.HeightAt(x, y, 0.5f, 0.5f), ahead = map.HeightAt(x, y, 0.5f + dx * 0.25f, 0.5f + dy * 0.25f);
        if (MathF.Abs(ahead - here) < 0.01f) return Incline.Level;
        return ahead < here ? Incline.Down : Incline.Up;
    }

    /// <summary>
    /// The next step of someone sliding on the ice of a tile, going a way at a speed (<c>PlayerAvatar_TileMove_Ice</c>):
    /// in the way, they stop, or slip back if they were going up a slope; free, the tile's tilt takes a speed away going
    /// up (with none left, they slip back) or adds one going down, up to three, and they slide on.
    /// </summary>
    public static Next From(Map map, int x, int y, Direction dir, int speed, Walker walker)
    {
        var incline = InclineOf(map, x, y, dir);
        var step = FieldMovement.Step(map, x, y, dir, walker);
        if (!step.Moves) return incline == Incline.Up ? SlipBack(map, x, y, dir, walker, step) : new Next(Outcome.Stop, step, 0);
        if (incline == Incline.Up && --speed < 0) return SlipBack(map, x, y, dir, walker, step);
        if (incline == Incline.Down) speed = Math.Min(MaxSpeed, speed + 1);
        return new Next(Outcome.Slide, step, speed);
    }

    // A tile back the way the slider came, if that way is open; where it isn't, they stand
    private static Next SlipBack(Map map, int x, int y, Direction dir, Walker walker, FieldStep refused)
    {
        var back = FieldMovement.Step(map, x, y, FieldMovement.Opposite(dir), walker);
        return back.Kind == StepKind.Walk ? new Next(Outcome.SlipBack, back, 0) : new Next(Outcome.Stop, refused, 0);
    }

    /// <summary>
    /// The original's frames a tile at each speed of a slide (<c>PlayerAvatar_SetIceMovement</c>: a fast walk of four
    /// frames at nought, three at one, two at two and three) and of the slip back (a slow walk, sixteen).
    /// </summary>
    public static int FramesATile(int speed) => speed <= 0 ? 4 : speed == 1 ? 3 : 2;

    public const int SlipFrames = 16;

    /// <summary>
    /// Tiles a second at a speed: the ice's own pace at nought (a run's, which is what the slide always kept), and as
    /// much faster again as the original's frames are fewer.
    /// </summary>
    public static float TilesPerSecond(int speed) => FieldMovement.TilesPerSecond(Pace.Fast) * FramesATile(0) / FramesATile(speed);

    /// <summary>Tiles a second of the slip back down a slope: a quarter of the ice's own pace.</summary>
    public static float SlipTilesPerSecond => FieldMovement.TilesPerSecond(Pace.Fast) * FramesATile(0) / SlipFrames;

    /// <summary>Whether a slide at a speed breaks a snowball in front of the slider (<c>ov5_021E06A8</c>: one or more).</summary>
    public static bool Breaks(int speed) => speed >= 1;

    /// <summary>
    /// The snowball on the tile ahead of a slider, or null: the first thing standing there, as the original looks
    /// (<c>sub_0206326C</c>), if it is a snowball.
    /// </summary>
    public static NPC? SnowballAhead(Map map, int x, int y, Direction dir)
    {
        var (dx, dy) = FieldMovement.Delta(dir);
        return map.GetNpcAt(x + dx, y + dy) is { IsSnowball: true } ball ? ball : null;
    }

    /// <summary>
    /// A snowball breaks (<c>MapObject_Delete</c>): it is off the map until the player next comes in, when the map's
    /// people and things are laid out afresh (<c>Map.ForgetForced</c>), as the original makes its objects again from
    /// the map's events. A battle doesn't bring it back.
    /// </summary>
    public static void Break(Map map, NPC snowball)
    {
        snowball.Forced = false;
        map.NPCs.Remove(snowball);
        if (!map.Absent.Contains(snowball)) map.Absent.Add(snowball);
    }
}
