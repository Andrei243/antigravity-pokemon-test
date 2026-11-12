using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The photo tour's viewfinder and its judged panel (plan 44 · SD5; style guide, "The photo tour").</summary>
internal static partial class ModernUi
{
    private const float TourBracket = 64, TourBracketThick = 6, TourInsetX = 96, TourInsetY = 64;

    /// <summary>Over the course's picture: the viewfinder while riding, the dark of the opening and the end, the judged panel.</summary>
    public static void DrawPhotoTour(int sw, int sh, PhotoTourScreen screen)
    {
        if (screen.Taking || screen.Ride is not { } ride) return;
        if (screen.Phase is TourPhase.Opening or TourPhase.Riding or TourPhase.Closing) Viewfinder(sw, sh, ride);
        if (screen.Dark > 0f) Raylib.DrawRectangle(0, 0, sw, sh, new Color(0, 0, 0, (int)(255 * screen.Dark)));
        if (screen.Phase == TourPhase.Judged) TourJudged(sw, sh, screen);
    }

    private static void Viewfinder(int sw, int sh, TourRide ride)
    {
        var white = new Color(255, 255, 255, 204);
        float x0 = TourInsetX, y0 = TourInsetY, x1 = sw - TourInsetX, y1 = sh - TourInsetY;
        foreach (var (cx, cy, dx, dy) in new[] { (x0, y0, 1f, 1f), (x1, y0, -1f, 1f), (x0, y1, 1f, -1f), (x1, y1, -1f, -1f) })
        {
            UiShapes.Line(new Vector2(cx, cy), new Vector2(cx + dx * TourBracket, cy), TourBracketThick, white);
            UiShapes.Line(new Vector2(cx, cy), new Vector2(cx, cy + dy * TourBracket), TourBracketThick, white);
        }
        var mid = new Vector2(sw / 2f, sh / 2f);
        UiShapes.Line(mid - new Vector2(12, 0), mid + new Vector2(12, 0), 3, white);
        UiShapes.Line(mid - new Vector2(0, 12), mid + new Vector2(0, 12), 3, white);

        // The whistle's rings
        if (ride.WhistleAge is >= 0f and < 0.6f)
            for (int i = 0; i < 3; i++)
            {
                float t = Math.Clamp(ride.WhistleAge / 0.6f - i * 0.15f, 0f, 1f);
                if (t <= 0f) continue;
                UiShapes.Ring(mid, 40 + t * 360, 4, new Color(255, 255, 255, (int)(200 * (1f - t))));
            }

        // The foot: the rail's way, the film and the fruit, the zoom
        float footY = sh - 40;
        var bar = new Rectangle(sw / 2f - 300, footY - 8, 600, 16);
        UiShapes.Fill(bar, 8, new Color(PanelTop.R, PanelTop.G, PanelTop.B, (byte)150));
        UiShapes.Fill(bar with { Width = Math.Max(16, bar.Width * ride.Progress) }, 8, Gold);
        UiFonts.Draw(Text.Of("photoTour.film", ("count", ride.FilmLeft)), TourInsetX, footY - 18, 30, Color.White, UiWeight.Black);
        UiFonts.Draw(Text.Of("photoTour.fruit", ("count", ride.FruitLeft)), TourInsetX + 260, footY - 18, 30, Color.White, UiWeight.Black);
        string zoom = Text.Of("photoTour.zoom", ("zoom", ride.Zoom.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)));
        UiFonts.Draw(zoom, sw - TourInsetX - UiFonts.Measure(zoom, 30, UiWeight.Black), footY - 18, 30, Color.White, UiWeight.Black);

        // The shutter's flash
        if (ride.ShotAge is >= 0f and < 0.18f)
            Raylib.DrawRectangle(0, 0, sw, sh, new Color(255, 255, 255, (int)(200 * (1f - ride.ShotAge / 0.18f))));
    }

    private static void TourJudged(int sw, int sh, PhotoTourScreen screen)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("photoTour.heading"));
        Hints(sw - Margin, 44, Hint("shared.keys.c", "photoTour.hints.keep"), Hint("shared.keys.z", "photoTour.hints.leave"));
        var panel = new Rectangle(Margin, ContentTop + 8, sw - 2 * Margin, 760);
        Panel(panel, 34);
        // Eight rows at once; past eight the list scrolls with the cursor (plan 44 · SD6)
        int rows = Math.Min(screen.Shown.Count - screen.FirstShown, PhotoTourScreen.RowsShown);
        for (int k = 0; k < rows; k++)
        {
            int i = screen.FirstShown + k;
            var best = screen.Shown[i];
            var r = new Rectangle(panel.X + 24, panel.Y + 24 + k * 92, panel.Width - 72, 84);
            var ink = ListRow(r, i == screen.Cursor, LocalNames.Species(best.Species), nameX: 30, size: 34);
            for (int s = 0; s < best.Stars; s++) Star(new Vector2(r.X + 560 + s * 44, r.Y + r.Height / 2f), 18, Gold);
            string points = Text.Of("photoTour.points", ("points", best.Points));
            UiFonts.DrawCentered(points, r.X + r.Width - 30 - UiFonts.Measure(points, 34, UiWeight.Black), r.Y + r.Height / 2f, 34, ink, UiWeight.Black);
        }
        ScrollBar(panel, screen.FirstShown, PhotoTourScreen.RowsShown, screen.Shown.Count);
        if (screen.Shown.Count == 0)
            UiFonts.Draw(Text.Of("photoTour.none"), panel.X + 40, panel.Y + 40, 34, Muted, UiWeight.Black);
        string total = Text.Of("photoTour.total", ("points", screen.Total));
        UiFonts.Draw(total, panel.X + 40, panel.Y + panel.Height + 24, 40, Color.White, UiWeight.Black);
        if (screen.Kept) UiFonts.Draw(Text.Of("photoBook.kept"), panel.X + panel.Width - 420, panel.Y + panel.Height + 30, 30, Gold, UiWeight.Black);
    }
}
