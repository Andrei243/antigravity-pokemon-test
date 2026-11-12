using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The Town Map's report page (plan 38 · EX2; style guide, "The Town Map", "The day's report"): a ring with the species'
/// menu icon over each cell of a place with an outbreak (Gold) or the visitor (Blue), and in the place's panel the day's
/// list with the counts, the visitor's minutes and what the scan says.
/// </summary>
internal static partial class ModernUi
{
    /// <summary>The cells of a region's map an area's blocks stand on.</summary>
    private static List<(int X, int Y)> CellsOf(TownMapState state, string area) =>
        state.Region.Blocks.Where(b => b.Area.Key == area && state.BlockAt(b.X, b.Y) != null).Select(b => (b.X, b.Y)).ToList();

    /// <summary>The marks of the day's report over the map (drawn inside the map's scissor).</summary>
    private static void ReportMarks(TownMapState state, FieldReport report, Func<float, float, Vector2> centreOf, float cell, float breath)
    {
        void Mark(string area, string species, Color colour)
        {
            var cells = CellsOf(state, area);
            if (cells.Count == 0) return;
            foreach (var (x, y) in cells)
            {
                var c = centreOf(x, y);
                UiShapes.Ring(c, cell * 0.5f + 1f, 4f, Color.White);
                UiShapes.Ring(c, cell * 0.5f - 2f, 3f, colour with { A = (byte)(170 + 85 * breath) });
            }
            // The species' menu icon over the middle of its place, on a disc of the mark's colour
            var middle = centreOf((float)cells.Average(p => p.X), (float)cells.Average(p => p.Y));
            float r = Math.Max(22f, cell * 0.9f);
            UiShapes.Circle(middle + new Vector2(0, 3), r + 3f, new Color(14, 22, 46, 110));
            UiShapes.Circle(middle, r + 3f, Color.White);
            UiShapes.Circle(middle, r, colour);
            ReportIcon(species, middle, r * 1.9f);
        }
        foreach (var told in report.Outbreaks) Mark(told.Outbreak.Area, told.Outbreak.Species, Gold);
        if (report.Visitor is { } visitor) Mark(visitor.Area, visitor.Species, Blue);
    }

    /// <summary>A species' menu icon fitted into a square of the given size, its middle at the point.</summary>
    private static void ReportIcon(string species, Vector2 c, float size)
    {
        var icon = PixelArtGenerator.GetPokemonIcon(species);
        float scale = MathF.Max(1f, MathF.Floor(size / Math.Max(1, icon.Width)));
        float w = icon.Width * scale, h = icon.Height * scale;
        Raylib.DrawTexturePro(icon, new Rectangle(0, 0, icon.Width, icon.Height),
            new Rectangle(MathF.Round(c.X - w / 2f), MathF.Round(c.Y - h / 2f), w, h), Vector2.Zero, 0f, Color.White);
    }

    /// <summary>The report's list in the place's panel: the day's outbreaks with their counts, then the visitor or the scan.</summary>
    private static void ReportPanel(Rectangle info, FieldReport report)
    {
        float x0 = info.X + 44, wide = info.Width - 88, y = info.Y + 44;
        Tag(x0, y, Text.Of("townMap.report.tag"), Gold);
        string region = RegionDatabase.Get(report.Region)?.Name ?? report.Region;
        UiFonts.Draw(Text.Of("townMap.report.title", ("region", region)), x0, y + 50, 46, Ink, UiWeight.Black);
        UiShapes.Fill(new Rectangle(x0, y + 120, wide, 3), 1.5f, Rule);
        y += 146;
        Label(Text.Of("townMap.report.outbreaks"), x0, y);
        y += 36;
        if (report.Outbreaks.Count == 0)
        {
            UiFonts.Draw(Text.Of("townMap.report.noOutbreaks"), x0, y, 30, Muted, UiWeight.ExtraBold);
            y += 52;
        }
        foreach (var told in report.Outbreaks)
        {
            Row(told.Outbreak.Species, FieldReport.PlaceName(report.Region, told.Outbreak.Area), Gold,
                Text.Of("townMap.report.met", ("count", told.Met)), y);
            y += 92;
        }
        y += 14;
        UiShapes.Fill(new Rectangle(x0, y, wide, 3), 1.5f, Rule);
        y += 26;
        Label(Text.Of("townMap.report.visitor"), x0, y);
        y += 36;
        if (report.Visitor is { } visitor)
        {
            int minutes = Math.Max(1, (int)MathF.Ceiling(visitor.SecondsLeft / 60f));
            Row(visitor.Species, FieldReport.PlaceName(report.Region, visitor.Area), Blue, Text.Of("townMap.report.minutes", ("count", minutes)), y);
            return;
        }
        string say = Text.Of($"townMap.report.scan.{report.Scan}");
        DrawWrapped(say, x0, y + 6, wide, 30, report.Scan == ScanState.Ready ? Ink : Muted, 42, UiWeight.ExtraBold);

        void Row(string species, string place, Color colour, string count, float top)
        {
            var c = new Vector2(x0 + 34, top + 34);
            UiShapes.Circle(c, 34, colour);
            UiShapes.Circle(c, 29, Color.White);
            ReportIcon(species, c, 56);
            float tx = x0 + 84;
            UiFonts.Draw(LocalNames.Species(species), tx, top, 34, Ink, UiWeight.Black);
            UiFonts.Draw(place, tx, top + 42, 26, Muted, UiWeight.ExtraBold);
            float tagWidth = UiFonts.Measure(count, 22, UiWeight.Black) + 28;
            Tag(x0 + wide - tagWidth, top + 14, count, colour);
        }
    }

    /// <summary>The legend under the map on the report's page: the two marks.</summary>
    private static void ReportLegend(Rectangle strip)
    {
        float sy0 = strip.Y + strip.Height / 2f, lx = strip.X + 36;
        UiShapes.Ring(new Vector2(lx, sy0), 10, 4f, Gold);
        lx = Legend(Text.Of("townMap.report.legendOutbreak"), lx + 20, sy0);
        UiShapes.Ring(new Vector2(lx, sy0), 10, 4f, Blue);
        Legend(Text.Of("townMap.report.legendVisitor"), lx + 20, sy0);
    }
}
