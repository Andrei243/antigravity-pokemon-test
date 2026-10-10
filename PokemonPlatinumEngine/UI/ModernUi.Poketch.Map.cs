using System;
using System.Collections.Generic;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models.PoketchApps;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The Pokétch's map apps (plan 06 · R14b; style guide, "The Pokétch"): the Dowsing Machine, the Berry Searcher, the
/// Marking Map and the Trainer Counter, in the LCD's three tones and blocks of 8. The map of Sinnoh is a block a chunk
/// (<see cref="PoketchMap"/>), its picture worked out once from the region's habitats file and drawn as runs of
/// blocks; whatever stands on it (the player, a berry, a marker, a roamer) is drawn in the tone that stands out from
/// the cell under it.
/// </summary>
internal static partial class ModernUi
{
    // ------------------------------------------------------------------ the map of Sinnoh

    // Each cell's tone on the map (0 sea or nothing, the paper; 1 land, the middle tone; 2 a town, the ink), and the
    // same as runs of one tone along each row, for drawing: both worked out once
    private static byte[,]? poketchMapTones;
    private static List<(int Col, int Row, int Length, byte Tone)>? poketchMapRuns;

    private static byte[,] PoketchMapTones()
    {
        if (poketchMapTones != null) return poketchMapTones;
        var tones = new byte[PoketchMap.Columns, PoketchMap.Rows];
        var runs = new List<(int, int, int, byte)>();
        var habitats = Habitats.Sinnoh;
        for (int r = 0; r < PoketchMap.Rows; r++)
        {
            for (int x = 0; x < PoketchMap.Columns; x++)
                tones[x, r] = (habitats?.LookAt(x, PoketchMap.FirstRow + r)) switch { '.' => 1, 'T' => 2, _ => 0 };
            for (int x = 0; x < PoketchMap.Columns;)
            {
                int start = x;
                byte tone = tones[x, r];
                while (x < PoketchMap.Columns && tones[x, r] == tone) x++;
                if (tone != 0) runs.Add((PoketchMap.Left + start, PoketchMap.Top + r, x - start, tone));
            }
        }
        poketchMapRuns = runs;
        return poketchMapTones = tones;
    }

    private static Color PoketchTone(int tone) => tone switch { 1 => LcdMid, 2 => LcdInk, _ => LcdPaper };

    /// <summary>The tone of a cell of the map as it is drawn now: a hidden place is sea until it is shown, land after.</summary>
    private static int PoketchMapToneAt(int x, int y, StoryState? story)
    {
        if (!PoketchMap.OnMap(x, y)) return 0;
        for (int h = 0; h < PoketchMap.HiddenLocations.Length; h++)
            if (PoketchMap.HiddenCell(h) == (x, y)) return PoketchMap.Shows(story, h) ? 1 : 0;
        return PoketchMapTones()[x, y - PoketchMap.FirstRow];
    }

    /// <summary>The tone that stands out on a cell: the paper on a town's ink, the ink anywhere else.</summary>
    private static Color PoketchStandOut(int x, int y, StoryState? story) => PoketchMapToneAt(x, y, story) == 2 ? LcdPaper : LcdInk;

    /// <summary>
    /// The map, sharp (<paramref name="mosaic"/> 1) or coarse, each square of <paramref name="mosaic"/> cells taking
    /// the tone of its first, as the Berry Searcher's picture comes back into focus.
    /// </summary>
    private static void PoketchRegionMap(Rectangle screen, StoryState? story, int mosaic = 1)
    {
        PoketchMapTones();
        if (mosaic <= 1)
        {
            foreach (var (col, row, length, tone) in poketchMapRuns!) LcdCells(screen, col, row, length, 1, PoketchTone(tone));
            for (int h = 0; h < PoketchMap.HiddenLocations.Length; h++)
            {
                var (hx, hy) = PoketchMap.HiddenCell(h);
                if (PoketchMap.OnMap(hx, hy))
                    LcdCells(screen, PoketchMap.Left + hx, PoketchMap.Top + hy - PoketchMap.FirstRow, 1, 1, PoketchTone(PoketchMapToneAt(hx, hy, story)));
            }
            return;
        }
        for (int r = 0; r < PoketchMap.Rows; r += mosaic)
            for (int x = 0; x < PoketchMap.Columns; x += mosaic)
            {
                int tone = PoketchMapToneAt(x, PoketchMap.FirstRow + r, story);
                if (tone == 0) continue;
                LcdCells(screen, PoketchMap.Left + x, PoketchMap.Top + r, Math.Min(mosaic, PoketchMap.Columns - x), Math.Min(mosaic, PoketchMap.Rows - r), PoketchTone(tone));
            }
    }

    /// <summary>
    /// A mark of three blocks by three centred on a cell of the map (<paramref name="shape"/>: three rows of '#' and
    /// '.'), each block in the tone that stands out from the cell under it; blocks off the map are left out.
    /// </summary>
    private static void PoketchMapMark(Rectangle screen, int x, int y, string shape, StoryState? story)
    {
        for (int i = 0; i < 9; i++)
        {
            if (shape[i] != '#') continue;
            int cx = x + i % 3 - 1, cy = y + i / 3 - 1;
            if (!PoketchMap.OnMap(cx, cy)) continue;
            LcdCells(screen, PoketchMap.Left + cx, PoketchMap.Top + cy - PoketchMap.FirstRow, 1, 1, PoketchStandOut(cx, cy, story));
        }
    }

    // The player: a square of three by three that blinks as a cursor does; the markers: six shapes of their own
    // (a ring, a square, a triangle, a heart, a cross and a bar-bell); a roamer: a saltire
    private const string PoketchPlayerMark = "#########";
    private const string PoketchRoamerMark = "#.#.#.#.#";
    private static readonly string[] PoketchMarkerMarks = { ".#.#.#.#.", "####.####", ".#.#.####", "#.####.#.", ".#.###.#.", "###.#.###" };

    /// <summary>The player's cell, if they are on the map of Sinnoh, blinking: shown 0.6 of each second.</summary>
    private static void PoketchMapPlayer(Rectangle screen, PoketchContext context)
    {
        if (PoketchMap.PlayerCell(context) is not var (x, y)) return;
        if (FrameClock.Now % 1.0 < 0.6) PoketchMapMark(screen, x, y, PoketchPlayerMark, context.Story);
    }

    // ------------------------------------------------------------------ the Dowsing Machine

    /// <summary>
    /// The tiles round the player as a dotted grid, the player's own tile in the ink; a ring spreading from the tile
    /// touched to the furthest range (48 pixels, 4.4 tiles); then the items found blinking where they lie, those
    /// noticed from further off ringed.
    /// </summary>
    private static void PoketchDowsingMachine(Rectangle screen, DowsingMachineApp app, PoketchContext context)
    {
        const int cell = DowsingMachineApp.Cell;
        int Col(int dx) => DowsingMachineApp.GridX + (dx + DowsingMachineApp.Left) * cell;
        int Row(int dy) => DowsingMachineApp.GridY + (dy + DowsingMachineApp.Up) * cell;

        // The grid: a dot at the corner of every tile, and its edge
        int w = DowsingMachineApp.Across * cell, h = DowsingMachineApp.Tall * cell;
        for (int dy = -DowsingMachineApp.Up + 1; dy <= DowsingMachineApp.Down; dy++)
            for (int dx = -DowsingMachineApp.Left + 1; dx <= DowsingMachineApp.Right; dx++)
                LcdCells(screen, Col(dx), Row(dy), 1, 1, LcdMid);
        LcdCells(screen, DowsingMachineApp.GridX - 1, DowsingMachineApp.GridY - 1, w + 2, 1, LcdMid);
        LcdCells(screen, DowsingMachineApp.GridX - 1, DowsingMachineApp.GridY + h, w + 2, 1, LcdMid);
        LcdCells(screen, DowsingMachineApp.GridX - 1, DowsingMachineApp.GridY, 1, h, LcdMid);
        LcdCells(screen, DowsingMachineApp.GridX + w, DowsingMachineApp.GridY, 1, h, LcdMid);

        // The player in the middle
        LcdCells(screen, Col(0), Row(0), cell, cell, LcdInk);

        if (!app.Pinging) return;
        var (tx, ty) = app.Touch;
        if (!app.ShowingItems)
        {
            // The ring: the blocks whose middles lie within half a block of a circle round the middle of the tile
            // touched, growing to the furthest range
            float reach = DowsingMachineApp.RangePixels[^1] / (float)DowsingMachineApp.TilePixels * cell;
            float radius = Math.Clamp(app.Age / DowsingMachineApp.PingSeconds, 0f, 1f) * reach;
            float cx = Col(tx) + cell / 2f, cy = Row(ty) + cell / 2f;
            int r = (int)MathF.Ceiling(radius) + 1;
            for (int by = Math.Max(DowsingMachineApp.GridY, (int)cy - r); by < Math.Min(DowsingMachineApp.GridY + h, (int)cy + r); by++)
                for (int bx = Math.Max(DowsingMachineApp.GridX, (int)cx - r); bx < Math.Min(DowsingMachineApp.GridX + w, (int)cx + r); bx++)
                {
                    float d = MathF.Sqrt((bx + 0.5f - cx) * (bx + 0.5f - cx) + (by + 0.5f - cy) * (by + 0.5f - cy));
                    if (MathF.Abs(d - radius) < 0.5f) LcdCells(screen, bx, by, 1, 1, LcdInk);
                }
            return;
        }

        // The items, blinking four times a second; one noticed from further off has a ring of the middle tone round it,
        // a block out for each step of its range
        if ((int)(app.Age * 8f) % 2 == 1) return;
        foreach (var item in app.Items)
        {
            int ix = Col(item.Dx), iy = Row(item.Dy);
            for (int ring = item.Range; ring >= 1; ring--)
            {
                int x0 = ix - ring, y0 = iy - ring, size = cell + 2 * ring;
                LcdCells(screen, x0, y0, size, 1, LcdMid);
                LcdCells(screen, x0, y0 + size - 1, size, 1, LcdMid);
                LcdCells(screen, x0, y0, 1, size, LcdMid);
                LcdCells(screen, x0 + size - 1, y0, 1, size, LcdMid);
            }
            LcdCells(screen, ix, iy, cell, cell, LcdInk);
        }
    }

    // ------------------------------------------------------------------ the Berry Searcher

    /// <summary>
    /// The map with a block on each cell where a patch is in fruit and the player's cell; just touched, the picture
    /// comes back into focus from squares of three cells, then two, then one.
    /// </summary>
    private static void PoketchBerrySearcher(Rectangle screen, BerrySearcherApp app, PoketchContext context)
    {
        int mosaic = 1 + (int)MathF.Ceiling(app.Blur * 2f);
        PoketchRegionMap(screen, context.Story, mosaic);
        foreach (var (x, y) in app.Ready(context))
        {
            int mx = x / mosaic * mosaic, my = PoketchMap.FirstRow + (y - PoketchMap.FirstRow) / mosaic * mosaic;
            if (!PoketchMap.OnMap(mx, my)) continue;
            LcdCells(screen, PoketchMap.Left + mx, PoketchMap.Top + my - PoketchMap.FirstRow, 1, 1, PoketchStandOut(x, y, context.Story));
        }
        PoketchMapPlayer(screen, context);
    }

    // ------------------------------------------------------------------ the Marking Map

    /// <summary>
    /// The map with the roamers on the loose, the six markers (the one moved last on top; one picked up blinks four
    /// times a second until it is put down) and the player's cell over everything.
    /// </summary>
    private static void PoketchMarkingMap(Rectangle screen, MarkingMapApp app, PoketchContext context)
    {
        PoketchRegionMap(screen, context.Story);
        foreach (var (_, x, y) in PoketchMap.RoamerCells(context.Encounters)) PoketchMapMark(screen, x, y, PoketchRoamerMark, context.Story);
        for (int i = app.Order.Count - 1; i >= 0; i--)
        {
            int m = app.Order[i];
            if (app.Carried == m && (int)(FrameClock.Now * 8.0) % 2 == 1) continue;
            var (x, y) = app.Positions[m];
            PoketchMapMark(screen, x, y, PoketchMarkerMarks[m], context.Story);
        }
        PoketchMapPlayer(screen, context);
    }

    // ------------------------------------------------------------------ the Trainer Counter

    /// <summary>
    /// The chain going now along the top (its species' icon and its count in three figures, the leading noughts not
    /// shown), and the three best chains as a podium: the best in the middle and highest, the second to its right,
    /// the third to its left, each a button with its species' icon over its count in small figures.
    /// </summary>
    private static void PoketchTrainerCounter(Rectangle screen, TrainerCounterApp app, PoketchContext context)
    {
        LcdText(screen, "CHAIN", 8f, 1, 22);
        if (TrainerCounterApp.Active(context.Radar) is var (species, count))
        {
            PoketchIcon(screen, species, 3, 4);
            string figures = Math.Min(count, 999).ToString();
            for (int i = 0; i < figures.Length; i++)
                SegmentDigit(screen.X + (17 + (3 - figures.Length + i) * 8) * Block, screen.Y + Block, figures[i] - '0');
        }
        else
            for (int i = 0; i < 3; i++) LcdCells(screen, 18 + i * 8, 7, 5, 1, LcdMid);
        LcdCells(screen, 1, 15, PoketchAppState.Columns - 2, 1, LcdMid);

        var best = TrainerCounterApp.Best(context.Poketch);
        for (int i = 0; i < TrainerCounterApp.Records; i++)
        {
            var b = TrainerCounterApp.RecordButtons[i];
            LcdButton(screen, b);
            if (best[i].Count == 0 || best[i].Species == null)
            {
                LcdCells(screen, b.X + 4, b.Y + 8, b.W - 8, 1, LcdMid);
                continue;
            }
            // A Pokémon touched hops: 24 of the original's pixels, five blocks, up and down
            int hop = (int)MathF.Round(5f * MathF.Sin(MathF.PI * app.Hop(i)));
            PoketchIcon(screen, best[i].Species!, b.X + 2, b.Y + 1 - hop);
            string figures = Math.Min(best[i].Count, 999).ToString();
            int left = b.X + (b.W - (figures.Length * 4 - 1)) / 2;
            for (int d = 0; d < figures.Length; d++) PoketchSmallDigit(screen, left + d * 4, b.Y + b.H - 6, figures[d] - '0');
        }
    }

    // The icons as blocks: each icon's picture read once into ten by ten blocks, a block the ink where the icon is
    // dark, the middle tone where it is light, nothing where it is clear, as the original turns its icons into the
    // LCD's shades by their brightness (PoketchTask_LoadPokemonIconLuminancePalette)
    private static readonly Dictionary<string, (uint Texture, byte[] Blocks)> poketchIcons = new();
    private const int PoketchIconBlocks = 10;

    private static byte[]? PoketchIconBlocksOf(string species)
    {
        var texture = PixelArtGenerator.GetPokemonIcon(species);
        if (texture.Width < PoketchIconBlocks) return null;
        if (poketchIcons.TryGetValue(species, out var known) && known.Texture == texture.Id) return known.Blocks;
        var image = Raylib.LoadImageFromTexture(texture);
        var blocks = new byte[PoketchIconBlocks * PoketchIconBlocks];
        float step = image.Width / (float)PoketchIconBlocks;
        for (int by = 0; by < PoketchIconBlocks; by++)
            for (int bx = 0; bx < PoketchIconBlocks; bx++)
            {
                int x0 = (int)(bx * step), x1 = Math.Max(x0 + 1, (int)((bx + 1) * step));
                int y0 = (int)(by * step), y1 = Math.Max(y0 + 1, (int)((by + 1) * step));
                int opaque = 0, all = 0;
                float light = 0f;
                for (int y = y0; y < y1 && y < image.Height; y++)
                    for (int x = x0; x < x1 && x < image.Width; x++)
                    {
                        all++;
                        var c = Raylib.GetImageColor(image, x, y);
                        if (c.A < 128) continue;
                        opaque++;
                        light += (0.3f * c.R + 0.59f * c.G + 0.11f * c.B) / 255f;
                    }
                if (all == 0 || opaque * 5 < all * 2) continue;
                blocks[by * PoketchIconBlocks + bx] = (byte)(light / opaque < 0.45f ? 2 : 1);
            }
        Raylib.UnloadImage(image);
        poketchIcons[species] = (texture.Id, blocks);
        return blocks;
    }

    /// <summary>A species' icon in blocks, its top left at a block of the screen (a block above the screen's top is left out).</summary>
    private static void PoketchIcon(Rectangle screen, string species, int col, int row)
    {
        if (PoketchIconBlocksOf(species) is not { } blocks) return;
        for (int by = 0; by < PoketchIconBlocks; by++)
            for (int bx = 0; bx < PoketchIconBlocks; bx++)
            {
                byte tone = blocks[by * PoketchIconBlocks + bx];
                if (tone == 0 || row + by < 0) continue;
                LcdCells(screen, col + bx, row + by, 1, 1, tone == 2 ? LcdInk : LcdMid);
            }
    }

    /// <summary>A small figure, three blocks by five, for the records' counts: the segments lit in the ink.</summary>
    private static void PoketchSmallDigit(Rectangle screen, int col, int row, int digit)
    {
        // Rows of the figure, three bits each, the left one highest
        int[][] rows =
        {
            new[] { 7, 5, 5, 5, 7 }, new[] { 2, 6, 2, 2, 7 }, new[] { 7, 1, 7, 4, 7 }, new[] { 7, 1, 7, 1, 7 }, new[] { 5, 5, 7, 1, 1 },
            new[] { 7, 4, 7, 1, 7 }, new[] { 7, 4, 7, 5, 7 }, new[] { 7, 1, 1, 1, 1 }, new[] { 7, 5, 7, 5, 7 }, new[] { 7, 5, 7, 1, 7 }
        };
        var figure = rows[Math.Clamp(digit, 0, 9)];
        for (int r = 0; r < 5; r++)
            for (int c = 0; c < 3; c++)
                if ((figure[r] & (4 >> c)) != 0) LcdCells(screen, col + c, row + r, 1, 1, LcdInk);
    }
}
