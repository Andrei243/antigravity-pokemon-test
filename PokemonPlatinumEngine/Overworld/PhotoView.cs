using System;
using System.Numerics;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>A photo's filter (style guide, "Light, shadow and grading (day)", "Photo mode's filters").</summary>
public enum PhotoFilter { None, Vivid, Mono, Sepia, Soft }

/// <summary>What a photo is framed in: nothing, a mat round the picture, or the camera kit's cinema bars.</summary>
public enum PhotoFrame { None, Mat, Cinema }

/// <summary>A weather a photo may borrow; <see cref="Now"/> is the place's own.</summary>
public enum PhotoWeather { Now, Clear, Rain, Snow, Fog }

/// <summary>The rows of photo mode's panel, in their order.</summary>
public enum PhotoRow { Zoom, Time, Weather, Filter, Frame }

/// <summary>
/// Photo mode in the field (plan 12 · Q16; style guide, "Field menus and notices", "Photo mode"): where the camera has
/// been sent from the player, its zoom, and the hour, weather, filter and frame the picture borrows. GPU-free and
/// changes nothing of the game: the renderer reads <see cref="Hour"/>, <see cref="Weather"/> and <see cref="Filter"/>
/// for the picture alone, and the engine hands <see cref="Spot"/> and <see cref="Zoom"/> to the camera kit.
/// </summary>
public sealed class PhotoView
{
    /// <summary>How far the camera may go from the player, in tiles, east or west and north or south.</summary>
    public const float ReachX = 10f, ReachZ = 8f;

    /// <summary>How fast the arrows move it, in tiles a second.</summary>
    public const float PanSpeed = 6f;

    /// <summary>The zoom's bounds (inside <see cref="CameraRig.WidestZoom"/> and <see cref="CameraRig.TightestZoom"/>) and its step.</summary>
    public const float WidestZoom = 0.75f, TightestZoom = 2.5f, ZoomStep = 1.25f;

    /// <summary>The middle of the player's tile as the photo began: the point the camera is sent from.</summary>
    public Vector2 Origin { get; }

    /// <summary>Whether a weather may be borrowed here (the open country; not a room, a cave or the Distortion World).</summary>
    public bool WeatherAllowed { get; }

    public PhotoView(Vector2 origin, bool weatherAllowed)
    {
        Origin = origin;
        WeatherAllowed = weatherAllowed;
    }

    /// <summary>Where the camera has been sent from <see cref="Origin"/>, within the reach.</summary>
    public Vector2 Offset { get; private set; }

    /// <summary>The point the camera looks at.</summary>
    public Vector2 Spot => Origin + Offset;

    /// <summary>The lens's factor over the place's own, <see cref="WidestZoom"/> to <see cref="TightestZoom"/>.</summary>
    public float Zoom { get; private set; } = 1f;

    /// <summary>The time of day borrowed; null is the clock's.</summary>
    public TimeOfDay? Time { get; private set; }

    public PhotoWeather BorrowedWeather { get; private set; }

    public PhotoFilter Filter { get; private set; }

    public PhotoFrame Frame { get; private set; }

    /// <summary>Whether the panel is open (its rows chosen and changed) or shut (the arrows move the camera).</summary>
    public bool PanelOpen { get; set; }

    /// <summary>The row under the cursor while the panel is open.</summary>
    public PhotoRow Row { get; private set; }

    /// <summary>Whether the panel and the hint are hidden while a picture is composed (the Item button).</summary>
    public bool InterfaceHidden { get; set; }

    /// <summary>The hour the picture is lit at: the middle of the borrowed period, as <see cref="GameClock.Hour"/> has it, or null for the clock's.</summary>
    public float? Hour => Time switch
    {
        TimeOfDay.LateNight => 2f,
        TimeOfDay.Morning => 7f,
        TimeOfDay.Day => 13.5f,
        TimeOfDay.Twilight => 18.5f,
        TimeOfDay.Night => 22f,
        _ => null
    };

    /// <summary>The weather the picture is drawn under, or null for the place's own.</summary>
    public FieldWeather? Weather => !WeatherAllowed ? null : BorrowedWeather switch
    {
        PhotoWeather.Clear => FieldWeather.Clear,
        PhotoWeather.Rain => FieldWeather.Rain,
        PhotoWeather.Snow => FieldWeather.Snow,
        PhotoWeather.Fog => FieldWeather.Fog,
        _ => null
    };

    /// <summary>Moves the camera by a direction held for <paramref name="seconds"/>, kept within the reach.</summary>
    public void Pan(Vector2 direction, float seconds)
    {
        if (direction == Vector2.Zero || seconds <= 0f) return;
        var moved = Offset + Vector2.Normalize(direction) * PanSpeed * seconds;
        Offset = new Vector2(Math.Clamp(moved.X, -ReachX, ReachX), Math.Clamp(moved.Y, -ReachZ, ReachZ));
    }

    /// <summary>Zooms a step in (positive) or out (negative), within the bounds.</summary>
    public void ZoomBy(int steps)
    {
        float next = Zoom * MathF.Pow(ZoomStep, steps);
        // Round to a hundredth, so steps in and back out land on the same factor
        Zoom = Math.Clamp(MathF.Round(next * 100f) / 100f, WidestZoom, TightestZoom);
    }

    /// <summary>Moves the panel's cursor a row up (negative) or down, wrapping.</summary>
    public void MoveRow(int by)
    {
        int count = Enum.GetValues<PhotoRow>().Length;
        Row = (PhotoRow)((((int)Row + by) % count + count) % count);
    }

    /// <summary>Changes the row under the cursor a value on (positive) or back. Returns false where nothing changed (a weather not here).</summary>
    public bool Change(int by)
    {
        switch (Row)
        {
            case PhotoRow.Zoom:
                float before = Zoom;
                ZoomBy(by);
                return Zoom != before;
            case PhotoRow.Time:
                // Now, then the five periods in the day's order
                TimeOfDay?[] times = { null, TimeOfDay.Morning, TimeOfDay.Day, TimeOfDay.Twilight, TimeOfDay.Night, TimeOfDay.LateNight };
                Time = times[Step(Array.IndexOf(times, Time), by, times.Length)];
                return true;
            case PhotoRow.Weather:
                if (!WeatherAllowed) return false;
                BorrowedWeather = Cycle(BorrowedWeather, by);
                return true;
            case PhotoRow.Filter:
                Filter = Cycle(Filter, by);
                return true;
            default:
                Frame = Cycle(Frame, by);
                return true;
        }
    }

    private static int Step(int at, int by, int count) => (((at + by) % count) + count) % count;

    private static T Cycle<T>(T value, int by) where T : struct, Enum
    {
        var all = Enum.GetValues<T>();
        return all[Step(Array.IndexOf(all, value), by, all.Length)];
    }
}
