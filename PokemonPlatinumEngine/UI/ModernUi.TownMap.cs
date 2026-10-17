using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The map of a region (plan 01 · M12; style guide, "The Town Map"): the painted picture in its frame, which the
/// Town Map, Fly, the Pokédex's area page and anything else that shows the region draw, and the Town Map's own screen.
/// </summary>
internal static partial class ModernUi
{
    /// <summary>The frame round a picture of the map: the notice colour, a shade deeper than a panel's border.</summary>
    private static readonly Color MapFrame = new(36, 44, 68, 255);

    /// <summary>How far the frame stands out round the picture: enough for its round corners to cover the picture's square ones.</summary>
    public const float MapFrameWidth = 8f;

    /// <summary>
    /// A region's map in a rectangle: its picture inside a dark frame. <paramref name="cells"/> is the part of the
    /// region's view shown, in cells from the view's top left (all of it when null); <paramref name="small"/> draws
    /// the smoothed half-size picture, for a map shown at less than a unit to a tile. Where there is no picture (a
    /// tool without the world) the cells are the habitats' plain squares of sea, land and town.
    /// </summary>
    public static void RegionPicture(Rectangle picture, RegionMap region, bool small = false, Rectangle? cells = null)
    {
        var view = region.View;
        var part = cells ?? new Rectangle(0, 0, view.Width, view.Height);
        var frame = new Rectangle(picture.X - MapFrameWidth, picture.Y - MapFrameWidth, picture.Width + 2 * MapFrameWidth, picture.Height + 2 * MapFrameWidth);
        UiShapes.Fill(frame, 18, MapFrame);
        if (RegionMapPictures.Get(region) is var (full, half))
        {
            var texture = small ? half : full;
            float texels = texture.Width / (float)view.Width;
            var source = new Rectangle(part.X * texels, part.Y * texels, part.Width * texels, part.Height * texels);
            Raylib.DrawTexturePro(texture, source, picture, Vector2.Zero, 0f, Color.White);
            return;
        }
        // No picture: one square to a cell
        Raylib.DrawRectangleRec(picture, RegionMapArt.SeaMid);
        if (Habitats.For(region.Region) is not { } habitats) return;
        float cell = picture.Width / part.Width;
        for (int y = 0; y < view.Height; y++)
            for (int x = 0; x < view.Width; x++)
            {
                char look = habitats.LookAt(view.X + x, view.Y + y);
                if (look is not ('.' or 'T')) continue;
                var r = new Rectangle(picture.X + (x - part.X) * cell, picture.Y + (y - part.Y) * cell, cell, cell);
                if (r.X + cell <= picture.X || r.Y + cell <= picture.Y || r.X >= picture.X + picture.Width || r.Y >= picture.Y + picture.Height) continue;
                Raylib.DrawRectangleRec(r, look == 'T' ? Color.White : Disc);
            }
    }

    /// <summary>
    /// The region's map in a rectangle, as large as whole cells of <paramref name="cell"/> allow and centred in it,
    /// with the cells <paramref name="lit"/> breathing in the Selection colour and the one at <paramref name="ring"/>
    /// ringed in white. Used by the Pokédex's area page.
    /// </summary>
    public static void RegionMap(Rectangle map, float cell, IReadOnlySet<(int X, int Y)> lit, (int X, int Y)? ring = null, Region? of = null)
    {
        if ((of == null ? Data.RegionMap.Sinnoh : Data.RegionMap.Of(of.Folder)) is not { } region)
            return;
        var view = region.View;
        var picture = new Rectangle(map.X + (map.Width - view.Width * cell) / 2f, map.Y + (map.Height - view.Height * cell) / 2f, view.Width * cell, view.Height * cell);
        RegionPicture(picture, region, small: cell < RegionMapArt.CellTexels);
        float pulse = 0.5f + 0.5f * MathF.Sin((float)FrameClock.Now * 3.2f);
        foreach (var (x, y) in lit)
        {
            if (!region.InView(x, y)) continue;
            var r = new Rectangle(picture.X + (x - view.X) * cell + 1.5f, picture.Y + (y - view.Y) * cell + 1.5f, cell - 3, cell - 3);
            UiShapes.Shape(r, 5, Selection with { A = (byte)(120 + 90 * pulse) }, Selection with { A = (byte)(120 + 90 * pulse) }, Color.White, 2.5f);
        }
        if (ring is var (rx, ry) && region.InView(rx, ry))
        {
            var c = new Vector2(picture.X + (rx - view.X + 0.5f) * cell, picture.Y + (ry - view.Y + 0.5f) * cell);
            UiShapes.Ring(c, cell * 0.62f, 4f, Color.White);
            UiShapes.Ring(c, cell * 0.62f + 3f, 2f, Ink);
        }
    }

    /// <summary>The Town Map's screen (style guide, "The Town Map"): the map on the left, the place under the cursor on the right.</summary>
    public static void DrawTownMap(int sw, int sh, TownMapScreen screen, float appear = 1f)
    {
        var state = screen.State!;
        var region = state.Region;
        var view = region.View;
        bool fly = screen.Mode == TownMapMode.Fly;
        Backdrop(sw, sh);
        var mode = screen.Mode;
        ScreenTitle(Text.Of($"townMap.title.{mode}"));
        string zoom = screen.Zoomed ? "shared.hints.wholeMap" : "shared.hints.zoomIn";
        if (fly) Hints(sw - Margin, 44, Hint("shared.keys.z", "shared.hints.fly"), Hint("shared.keys.enter", zoom), Hint("shared.keys.esc", "shared.hints.back"));
        else Hints(sw - Margin, 44, Hint("shared.keys.z", zoom), Hint("shared.keys.esc", "shared.hints.close"));
        float slide = (1f - UiMotion.EaseOut(appear)) * 60f;

        // ---- The map
        const float MapWidth = 960, MapHeight = 768, StripHeight = 56, Pad = 32;
        var panel = new Rectangle(Margin - slide, ContentTop, MapWidth + 2 * Pad, MapHeight + 2 * Pad);
        Panel(panel, 34);
        float whole = MathF.Floor(Math.Min(MapWidth / view.Width, MapHeight / view.Height));
        var picture = new Rectangle(panel.X + Pad + (MapWidth - view.Width * whole) / 2f, panel.Y + Pad + (MapHeight - view.Height * whole) / 2f, view.Width * whole, view.Height * whole);
        var (zx, zy) = screen.ZoomShown;
        var shown = screen.Zoomed
            ? new Rectangle(zx, zy, view.Width / (float)TownMapScreen.Zoom, view.Height / (float)TownMapScreen.Zoom)
            : new Rectangle(0, 0, view.Width, view.Height);
        float cell = picture.Width / shown.Width;
        RegionPicture(picture, region, cells: shown);

        Vector2 CentreOf(float cx, float cy) => new(picture.X + (cx - view.X - shown.X + 0.5f) * cell, picture.Y + (cy - view.Y - shown.Y + 0.5f) * cell);

        Raylib.BeginScissorMode((int)(picture.X * GameEngine.RenderScale), (int)(picture.Y * GameEngine.RenderScale),
            (int)(picture.Width * GameEngine.RenderScale), (int)(picture.Height * GameEngine.RenderScale));
        float clock = (float)FrameClock.Now;
        float breath = 0.5f + 0.5f * MathF.Sin(clock * 3.2f);
        var hoveredSpot = screen.HoveredSpot;

        // The way come: a dot on each cell, the bright one with the way faced
        if (screen.Mode != TownMapMode.Wall)
            for (int i = state.Trail.Count - 1; i >= 0; i--)
            {
                var step = state.Trail[i];
                var c = CentreOf(step.X, step.Y);
                bool bright = i == screen.TrailShown;
                TrailDot(c, bright ? 7f : 4.5f, bright ? step.Facing : null, bright ? 255 : 150);
            }

        // Fly's places: a marker in the middle of each, lit where it is open
        foreach (var spot in state.Spots)
        {
            if (!spot.Drawn || spot.Spot.Cells.Any(cellOf => state.BlockAt(cellOf.X, cellOf.Y) == null)) continue;
            float mx = (float)spot.Spot.Cells.Average(p => p.X), my = (float)spot.Spot.Cells.Average(p => p.Y);
            var c = CentreOf(mx, my);
            // Choosing where to fly, the whole of the place under the cursor is lit, breathing, where it is open
            if (fly && hoveredSpot == spot && spot.Open)
                foreach (var (x, y) in spot.Spot.Cells)
                {
                    var corner = CentreOf(x, y) - new Vector2(cell / 2f, cell / 2f);
                    UiShapes.Fill(new Rectangle(corner.X + 2, corner.Y + 2, cell - 4, cell - 4), 7, Selection with { A = (byte)(60 + 70 * breath) });
                }
            float r = screen.Zoomed ? 13f : 9f;
            UiShapes.Circle(c + new Vector2(0, 2), r + 2.5f, new Color(14, 22, 46, 110));
            UiShapes.Circle(c, r + 2.5f, spot.Open ? Color.White : new Color(214, 220, 232, 255));
            UiShapes.Circle(c, r, spot.Open ? (fly ? Lighter(Gold, 0.25f * breath) : Gold) : Muted);
            UiShapes.Circle(c, r * 0.36f, spot.Open ? Color.White : new Color(214, 220, 232, 255));
        }

        // The player
        if (state.Here is var (hx, hy))
        {
            var c = CentreOf(hx, hy) - new Vector2(0, 6);
            float ringAt = clock * 0.9f % 1f;
            UiShapes.Ring(c + new Vector2(0, 6), 10 + 16 * ringAt, 3f, Red with { A = (byte)(200 * (1f - ringAt)) });
            UiShapes.Triangle(c + new Vector2(-8, 5), c + new Vector2(8, 5), c + new Vector2(0, 18), Color.White);
            UiShapes.Circle(c, 13, Color.White);
            UiShapes.Triangle(c + new Vector2(-5, 6), c + new Vector2(5, 6), c + new Vector2(0, 14), Red);
            UiShapes.Circle(c, 9.5f, Red);
            UiShapes.Circle(c, 3.6f, Color.White);
        }

        // The cursor: four corners round its cell, settling as it lands
        {
            var c = CentreOf(screen.CursorX, screen.CursorY);
            float settle = 1f - UiMotion.EaseOut(Math.Clamp(screen.SinceMove / 0.14f, 0f, 1f));
            float half = cell / 2f + 3f + 5f * settle + 1.5f * MathF.Sin(clock * 5f);
            float arm = cell * 0.3f;
            foreach (var (sx, sy) in new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) })
            {
                var corner = c + new Vector2(sx * half, sy * half);
                foreach (var (ink, thick) in new[] { (Color.White, 8f), (Selection, 4.5f) })
                {
                    UiShapes.Line(corner, corner - new Vector2(sx * arm, 0), thick, ink);
                    UiShapes.Line(corner, corner - new Vector2(0, sy * arm), thick, ink);
                }
            }
        }
        Raylib.EndScissorMode();

        // ---- Under the map: what the buttons are for, or what the marks mean
        var strip = new Rectangle(Margin - slide, ContentBottom - StripHeight, panel.Width, StripHeight);
        Panel(strip, 26);
        float sy0 = strip.Y + StripHeight / 2f;
        if (fly) UiFonts.DrawCentered(Text.Of("townMap.flyTo"), strip.X + 32, sy0, 30, Ink, UiWeight.ExtraBold);
        else
        {
            float lx = strip.X + 36;
            UiShapes.Circle(new Vector2(lx, sy0), 9, Red);
            UiShapes.Circle(new Vector2(lx, sy0), 3.4f, Color.White);
            lx = Legend(Text.Of("townMap.legend.you"), lx + 20, sy0);
            UiShapes.Circle(new Vector2(lx, sy0), 9, Gold);
            UiShapes.Circle(new Vector2(lx, sy0), 3.2f, Color.White);
            lx = Legend(Text.Of("townMap.legend.visited"), lx + 20, sy0);
            UiShapes.Circle(new Vector2(lx, sy0), 9, Muted);
            UiShapes.Circle(new Vector2(lx, sy0), 3.2f, new Color(214, 220, 232, 255));
            lx = Legend(Text.Of("townMap.legend.notVisited"), lx + 20, sy0);
            if (screen.Mode != TownMapMode.Wall)
            {
                TrailDot(new Vector2(lx, sy0), 6f, null, 255, onPanel: true);
                Legend(Text.Of("townMap.legend.trail"), lx + 20, sy0);
            }
        }

        // ---- The place under the cursor
        var info = new Rectangle(Margin + panel.Width + Gutter + slide, ContentTop, sw - Margin * 2 - panel.Width - Gutter, ContentBottom - ContentTop);
        Panel(info, 34);
        float x0 = info.X + 44, wide = info.Width - 88, ty = info.Y + 44;
        var block = screen.Hovered;
        if (block == null)
        {
            UiFonts.Draw(RegionDatabase.Get(region.Region)?.Name ?? region.Region, x0, ty + 46, 52, Ink, UiWeight.Black);
            DrawWrapped(Text.Of("townMap.hover"), x0, ty + 150, wide, 30, Muted, 42, UiWeight.ExtraBold);
        }
        else
        {
            var (label, colour) = block.Kind switch
            {
                RegionBlockKind.Route => (Text.Of("townMap.tag.route"), Green),
                RegionBlockKind.Town => (block.Name.EndsWith("City", StringComparison.Ordinal) ? Text.Of("townMap.tag.city") : Text.Of("townMap.tag.town"), Gold),
                _ => (Text.Of("townMap.tag.landmark"), Blue)
            };
            float tx = x0 + Tag(x0, ty, label, colour) + 10;
            if (hoveredSpot != null)
                Tag(tx, ty, Text.Of(hoveredSpot.Open ? (fly ? "townMap.tag.flyHere" : "townMap.tag.visited") : "townMap.tag.notVisited"), hoveredSpot.Open ? (fly ? Selection : Frame) : Muted);
            float size = UiFonts.Measure(block.Name, 52, UiWeight.Black) <= wide ? 52 : 42;
            UiFonts.Draw(block.Name, x0, ty + 50, size, Ink, UiWeight.Black);
            UiShapes.Fill(new Rectangle(x0, ty + 128, wide, 3), 1.5f, Rule);
            float y = ty + 154;
            if (state.Tells(block.Area)) y = Paragraph(block.Area.About, x0, y, wide, Ink);
            else y = Paragraph(Text.Of("townMap.notBeen"), x0, y, wide, Muted);
            if (block.Landmark is { } landmark && state.TellsLandmark(block))
            {
                y += 22;
                UiShapes.Circle(new Vector2(x0 + 12, y + 20), 12, Blue);
                UiShapes.Circle(new Vector2(x0 + 12, y + 20), 4.6f, Color.White);
                UiFonts.Draw(landmark.Name ?? "", x0 + 36, y, 36, Ink, UiWeight.Black);
                Paragraph(landmark.About, x0, y + 56, wide, Ink);
            }
        }

        // At the panel's foot: a window on the place from near (from near, where on the whole map that is), and under
        // it where the player is
        float foot = info.Y + info.Height - 44;
        const float WindowHeight = 256;
        float windowTop = foot - 108 - 24 - MapFrameWidth - WindowHeight;
        if (screen.Zoomed)
        {
            float lcell = Math.Min((wide - 2 * MapFrameWidth) / view.Width, WindowHeight / view.Height);
            var locator = new Rectangle(x0 + MapFrameWidth, windowTop, view.Width * lcell, view.Height * lcell);
            RegionPicture(locator, region, small: true);
            var seen = new Rectangle(locator.X + shown.X * lcell, locator.Y + shown.Y * lcell, shown.Width * lcell, shown.Height * lcell);
            UiShapes.Shape(seen, 6, new Color(255, 255, 255, 46), new Color(255, 255, 255, 46), Color.White, 3f);
            if (state.Here is var (px, py))
            {
                var c = new Vector2(locator.X + (px - view.X + 0.5f) * lcell, locator.Y + (py - view.Y + 0.5f) * lcell);
                UiShapes.Circle(c, 7, Color.White);
                UiShapes.Circle(c, 4.6f, Red);
            }
            float nx = locator.X + locator.Width + MapFrameWidth + 28;
            Label(Text.Of("townMap.whereThisIs"), nx, locator.Y + 4);
            DrawWrapped(Text.Of("townMap.frame"), nx, locator.Y + 40, x0 + wide - nx, 24, Muted, 34, UiWeight.ExtraBold);
        }
        else
        {
            // Four units to a tile, the cursor's cell in the middle, held inside the map
            const float Near = 4f;
            float windowWidth = MathF.Floor((wide - 2 * MapFrameWidth) / (Near * 2f)) * Near * 2f;
            var window = new Rectangle(x0 + (wide - windowWidth) / 2f, windowTop, windowWidth, WindowHeight);
            float cellsWide = windowWidth / (Near * RegionMapArt.CellTexels), cellsHigh = WindowHeight / (Near * RegionMapArt.CellTexels);
            var near = new Rectangle(
                Math.Clamp(screen.CursorX - view.X + 0.5f - cellsWide / 2f, 0f, view.Width - cellsWide),
                Math.Clamp(screen.CursorY - view.Y + 0.5f - cellsHigh / 2f, 0f, view.Height - cellsHigh), cellsWide, cellsHigh);
            RegionPicture(window, region, cells: near);
            // The cursor's own cell, marked by its corners
            float big = Near * RegionMapArt.CellTexels;
            var c = new Vector2(window.X + (screen.CursorX - view.X - near.X + 0.5f) * big, window.Y + (screen.CursorY - view.Y - near.Y + 0.5f) * big);
            Raylib.BeginScissorMode((int)(window.X * GameEngine.RenderScale), (int)(window.Y * GameEngine.RenderScale),
                (int)(window.Width * GameEngine.RenderScale), (int)(window.Height * GameEngine.RenderScale));
            foreach (var (sx, sy) in new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) })
            {
                var corner = c + new Vector2(sx * (big / 2f - 4f), sy * (big / 2f - 4f));
                UiShapes.Line(corner, corner - new Vector2(sx * 22f, 0), 5f, Color.White);
                UiShapes.Line(corner, corner - new Vector2(0, sy * 22f), 5f, Color.White);
            }
            Raylib.EndScissorMode();
        }
        UiShapes.Fill(new Rectangle(x0, foot - 108, wide, 3), 1.5f, Rule);
        Label(Text.Of(state.Here == null ? "townMap.youAre" : "townMap.youAreIn"), x0, foot - 84);
        UiShapes.Circle(new Vector2(x0 + 14, foot - 26), 13, Red);
        UiShapes.Circle(new Vector2(x0 + 14, foot - 26), 5, Color.White);
        UiFonts.DrawCentered(state.HereName ?? Text.Of(state.Here == null ? "townMap.offTheMap" : "townMap.wilds"), x0 + 40, foot - 26, 36, Ink, UiWeight.Black);
    }

    /// <summary>A paragraph of a place's words, Bold 30 on 42 leading; returns where the next line would begin.</summary>
    private static float Paragraph(string text, float x, float y, float width, Color color)
    {
        var lines = Wrap(text, width, 30);
        for (int i = 0; i < lines.Count; i++) UiFonts.Draw(lines[i], x, y + i * 42, 30, color);
        return y + lines.Count * 42;
    }

    /// <summary>A word of the legend under the map; returns where the next mark goes.</summary>
    private static float Legend(string text, float x, float centreY)
    {
        UiFonts.DrawCentered(text, x, centreY, 24, Muted, UiWeight.ExtraBold);
        return x + UiFonts.Measure(text, 24, UiWeight.ExtraBold) + 52;
    }

    /// <summary>A cell of the way come: a white dot with a dark rim, and an arrow the way the player faced when it is the bright one.</summary>
    private static void TrailDot(Vector2 c, float r, Direction? facing, int alpha, bool onPanel = false)
    {
        var rim = onPanel ? Frame : new Color(14, 22, 46, alpha * 150 / 255);
        UiShapes.Circle(c, r + 2.5f, rim);
        UiShapes.Circle(c, r, new Color(255, 255, 255, alpha));
        if (facing is not { } way) return;
        var (dx, dy) = way switch { Direction.Up => (0f, -1f), Direction.Down => (0f, 1f), Direction.Left => (-1f, 0f), _ => (1f, 0f) };
        var tip = c + new Vector2(dx, dy) * (r + 12f);
        var back = c + new Vector2(dx, dy) * (r + 3f);
        var side = new Vector2(-dy, dx) * 6.5f;
        UiShapes.Triangle(tip + new Vector2(dx, dy) * 3f, back + side * 1.4f - new Vector2(dx, dy) * 2f, back - side * 1.4f - new Vector2(dx, dy) * 2f, rim);
        UiShapes.Triangle(tip, back + side, back - side, Color.White);
    }
}
