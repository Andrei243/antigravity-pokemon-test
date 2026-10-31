using System;
using System.IO;
using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Overworld;

partial class Harness
{
    // ---------------------------------------------------------------- photo mode in the field (plan 12 · Q16)

    // Twinleaf Town at noon through photo mode (photo_*; not part of "all"): the mode opened with its hint and its
    // panel shut, the panel open, a pan, a zoom in and out, the night and the rain borrowed at noon, each filter, the
    // two frames, a picture written through the screenshot key's own CaptureScreen, and the camera back at rest after.
    // It prints the clock's hour, the date and the place's weather before and after, which a photo never changes.
    public void PhotoMode()
    {
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        GoTo("TwinleafTown", 11, 8, Direction.Down);
        Frames(8);
        var weatherBefore = game.Map.WeatherAt(game.Player.GridX, game.Player.GridY);
        float hourBefore = GameClock.Hour;
        var dateBefore = GameClock.Today;
        Console.WriteLine($"before: hour {hourBefore:0.00}, date {dateBefore:yyyy-MM-dd}, weather {weatherBefore}");

        if (!game.OpenPhoto()) Console.WriteLine("!! photo mode would not open");
        var p = game.Photo!;
        Frames(4); Shot("photo_01_open");

        p.PanelOpen = true;
        Frames(2); Shot("photo_02_panel");
        p.PanelOpen = false;

        p.Pan(new Vector2(1, -0.6f), 1.2f);
        Frames(20); Shot("photo_03_pan");
        Console.WriteLine($"pan: offset {p.Offset.X:0.00}, {p.Offset.Y:0.00}; the camera looks at {p.Spot.X:0.00}, {p.Spot.Y:0.00}");

        p.ZoomBy(3);
        Frames(20); Shot("photo_04_zoom_in");
        Console.WriteLine($"zoom: photo {p.Zoom:0.00}, rig {game.World.Camera.Zoom:0.00}");
        p.ZoomBy(-6);
        Frames(20); Shot("photo_05_zoom_out");
        Console.WriteLine($"zoom: photo {p.Zoom:0.00}, rig {game.World.Camera.Zoom:0.00}");
        p.ZoomBy(2);

        // The night borrowed at noon
        SetRow(p, PhotoRow.Time, 4);
        p.PanelOpen = true;
        Frames(4); Shot("photo_06_night_panel");
        p.PanelOpen = false;
        Frames(2); Shot("photo_06b_night");
        Console.WriteLine($"night: the picture's hour {p.Hour:0.00}, the clock's {GameClock.Hour:0.00}");
        SetRow(p, PhotoRow.Time, -4);

        // The rain borrowed
        SetRow(p, PhotoRow.Weather, 2);
        Frames(30); Shot("photo_07_rain");
        Console.WriteLine($"rain: the picture's {p.Weather}, the place's {game.Map.WeatherAt(game.Player.GridX, game.Player.GridY)}");
        SetRow(p, PhotoRow.Weather, -2);

        foreach (var filter in new[] { PhotoFilter.Vivid, PhotoFilter.Mono, PhotoFilter.Sepia, PhotoFilter.Soft })
        {
            SetRow(p, PhotoRow.Filter, 1);
            Frames(2); Shot($"photo_08_filter_{filter.ToString().ToLowerInvariant()}");
        }
        SetRow(p, PhotoRow.Filter, 1);

        SetRow(p, PhotoRow.Frame, 1);
        Frames(2); Shot("photo_09_frame_mat");
        SetRow(p, PhotoRow.Frame, 1);
        Frames(20); Shot("photo_10_frame_cinema");

        // A picture: the screenshot key's own capture, with nothing of the interface in it
        SetRow(p, PhotoRow.Frame, 1);
        SetRow(p, PhotoRow.Frame, 1);
        SetRow(p, PhotoRow.Time, 3);
        game.PressScreenshot();
        Frames(1);
        string? picture = game.LastScreenshot;
        Console.WriteLine(picture == null ? "!! no photo was written" : $"the photo: {Path.GetFileName(picture)}, {new FileInfo(picture).Length} bytes");
        if (picture != null) File.Copy(picture, Path.Combine(outDir, "photo_11_written.png"), overwrite: true);
        Frames(2); Shot("photo_11b_after_toast");

        game.ClosePhoto();
        Frames(30); Shot("photo_12_closed");
        Console.WriteLine($"after: hour {GameClock.Hour:0.00}, date {GameClock.Today:yyyy-MM-dd}, weather {game.Map.WeatherAt(game.Player.GridX, game.Player.GridY)}; state {game.State}; camera at rest {game.World.Camera.AtRest}");
        if (GameClock.Today != dateBefore || game.Map.WeatherAt(game.Player.GridX, game.Player.GridY) != weatherBefore)
            Console.WriteLine("!! the photo changed the date or the place's weather");
    }

    // Takes a row of the photo's panel a number of values on (or back)
    static void SetRow(PhotoView p, PhotoRow row, int by)
    {
        while (p.Row != row) p.MoveRow(1);
        int step = Math.Sign(by);
        for (int i = 0; i < Math.Abs(by); i++) p.Change(step);
    }
}
