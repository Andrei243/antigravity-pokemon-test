using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// A stone panel of the Ruins of Alph on screen (plan 20 · J5; style guide, "Menu screens", "The Ruins of Alph's
/// panels"): a slab of sandstone, six squares by six, its middle four by four sunk as the picture's frame, the corners
/// plain; each piece a tile of paler stone with its part of the picture cut into it (the species' own menu sprite,
/// darkened to an engraving), turned as it lies; the piece in hand lifted over the cursor with its shadow under it.
/// </summary>
internal static partial class ModernUi
{
    private static readonly Color AlphSlab = new(150, 128, 98, 255), AlphSlabDeep = new(118, 98, 74, 255),
        AlphFrame = new(92, 76, 58, 255), AlphTile = new(214, 194, 158, 255), AlphTileDeep = new(186, 164, 128, 255),
        AlphCarving = new(78, 58, 40, 235), AlphGlow = new(255, 232, 160, 255);

    /// <summary>A square's side on the panel, in layout units.</summary>
    public const float AlphSquare = 120;

    public static void DrawAlphPuzzle(int sw, int sh, AlphPuzzleScreen screen, float appear = 1f)
    {
        if (screen.Puzzle is not { } puzzle) return;
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("alphPuzzle.title"));
        Hints(sw - Margin, 44, Hint("shared.keys.z", "alphPuzzle.hint.take"), Hint("pokegear.keys.shoulders", "alphPuzzle.hint.turn"),
            Hint("shared.keys.esc", "shared.hints.back"));
        float rise = (1f - UiMotion.EaseOut(appear)) * 60f;

        const float side = AlphPuzzle.Side * AlphSquare;
        float left = (sw - side) / 2f, top = ContentTop + 20 + rise;
        var slab = new Rectangle(left - 28, top - 28, side + 56, side + 56);
        UiShapes.Shadow(slab, 36, 30, new Vector2(0, 14), new Color(0, 0, 0, 90));
        UiShapes.Shape(slab, 36, AlphSlab, AlphSlabDeep, AlphFrame, 6);
        var frame = new Rectangle(left + AlphSquare - 8, top + AlphSquare - 8, AlphPuzzle.Frame * AlphSquare + 16, AlphPuzzle.Frame * AlphSquare + 16);
        UiShapes.Shape(frame, 14, AlphFrame, Darker(AlphFrame, 0.2f));

        float glow = screen.SolvedAge is { } solved ? Math.Clamp(solved / 0.5f, 0f, 1f) * (0.75f + 0.25f * MathF.Sin(solved * 9f)) : 0f;
        var picture = PixelArtGenerator.GetPokemonSprite(puzzle.Species, false);
        for (int y = 0; y < AlphPuzzle.Side; y++)
            for (int x = 0; x < AlphPuzzle.Side; x++)
            {
                var r = new Rectangle(left + x * AlphSquare + 6, top + y * AlphSquare + 6, AlphSquare - 12, AlphSquare - 12);
                if (AlphPuzzle.IsCorner(x, y)) { UiShapes.Fill(r, 12, Darker(AlphSlab, 0.08f)); continue; }
                if (!AlphPuzzle.InFrame(x, y)) UiShapes.Fill(r, 12, Darker(AlphSlab, 0.16f));
                int piece = puzzle.PieceAt(x, y);
                if (piece >= 0) AlphPiece(r, picture, piece, puzzle.TurnsOf(piece), glow);
            }

        // The cursor, and the piece in hand lifted over it
        var at = new Rectangle(left + puzzle.CursorX * AlphSquare, top + puzzle.CursorY * AlphSquare, AlphSquare, AlphSquare);
        if (screen.SolvedAge == null)
        {
            if (puzzle.Held is { } held)
            {
                var lifted = new Rectangle(at.X + 2, at.Y - 18, AlphSquare - 4, AlphSquare - 4);
                UiShapes.Shadow(lifted, 14, 18, new Vector2(10, 26), new Color(0, 0, 0, 110));
                AlphPiece(lifted, picture, held, puzzle.TurnsOf(held), 0f);
            }
            UiShapes.Shape(new Rectangle(at.X - 2, at.Y - 2, at.Width + 4, at.Height + 4), 16, new Color(0, 0, 0, 0), new Color(0, 0, 0, 0), Color.White, 6);
        }

        // Along the foot: what the panel shows, or that it is whole
        var foot = new Rectangle(Margin, 940 + rise, sw - Margin * 2, 92);
        Panel(foot, 30);
        string line = screen.SolvedAge != null ? Text.Of("alphPuzzle.solved")
            : puzzle.Held != null ? Text.Of("alphPuzzle.holding") : Text.Of("alphPuzzle.prompt");
        UiFonts.DrawCentered(line, foot.X + 44, foot.Y + foot.Height / 2f, 36, Ink, UiWeight.ExtraBold);
    }

    // The square of a picture its Pokémon fills, by the texture: the sprite's opaque texels and a texel round them, so
    // the picture takes the whole frame and few pieces are bare stone. Read back once for each texture.
    private static readonly System.Collections.Generic.Dictionary<uint, Rectangle> AlphPictureBounds = new();

    private static Rectangle AlphBounds(Texture2D picture)
    {
        if (AlphPictureBounds.TryGetValue(picture.Id, out var known)) return known;
        var image = Raylib.LoadImageFromTexture(picture);
        int minX = image.Width, minY = image.Height, maxX = -1, maxY = -1;
        unsafe
        {
            var colours = Raylib.LoadImageColors(image);
            for (int y = 0; y < image.Height; y++)
                for (int x = 0; x < image.Width; x++)
                    if (colours[y * image.Width + x].A > 16)
                    {
                        minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
                        minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
                    }
            Raylib.UnloadImageColors(colours);
        }
        Raylib.UnloadImage(image);
        Rectangle bounds;
        if (maxX < 0) bounds = new Rectangle(0, 0, picture.Width, picture.Height);
        else
        {
            // A square round the opaque texels, a texel wider each way, kept inside the texture
            float side = Math.Min(Math.Max(maxX - minX, maxY - minY) + 3, Math.Min(picture.Width, picture.Height));
            float cx = (minX + maxX + 1) / 2f, cy = (minY + maxY + 1) / 2f;
            bounds = new Rectangle(Math.Clamp(cx - side / 2f, 0, picture.Width - side), Math.Clamp(cy - side / 2f, 0, picture.Height - side), side, side);
        }
        AlphPictureBounds[picture.Id] = bounds;
        return bounds;
    }

    // A piece: a tile of pale stone with its quarter of a quarter of the picture cut into it, turned as it lies
    private static void AlphPiece(Rectangle r, Texture2D picture, int piece, int turns, float glow)
    {
        UiShapes.Shape(r, 12, glow > 0 ? PixelCanvas.Mix(AlphTile, AlphGlow, glow) : AlphTile, AlphTileDeep, Darker(AlphTileDeep, 0.25f), 3);
        var whole = AlphBounds(picture);
        float cell = whole.Width / AlphPuzzle.Frame, cellH = whole.Height / AlphPuzzle.Frame;
        var source = new Rectangle(whole.X + piece % AlphPuzzle.Frame * cell, whole.Y + piece / AlphPuzzle.Frame * cellH, cell, cellH);
        float inner = r.Width - 12;
        var centre = new Vector2(r.X + r.Width / 2f, r.Y + r.Height / 2f);
        Raylib.DrawTexturePro(picture, source, new Rectangle(MathF.Round(centre.X), MathF.Round(centre.Y), inner, inner),
            new Vector2(inner / 2f, inner / 2f), turns * 90f, glow > 0 ? PixelCanvas.Mix(AlphCarving, new Color(120, 84, 40, 255), glow) : AlphCarving);
    }
}
