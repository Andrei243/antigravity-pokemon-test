using System;
using System.Collections.Generic;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>A rectangle of painted art inside an <see cref="ArtSheet"/>, in texels.</summary>
internal readonly record struct Art(int X, int Y, int Width, int Height);

/// <summary>
/// One texture holding every painted face of a map's buildings and props, so they all draw in one batch. Each
/// face is painted at its exact size (32 texels per tile) into its own canvas and packed on shelves, with a
/// one-texel border repeated around it so neighbouring faces never bleed in. No GPU calls: the map scene uploads
/// <see cref="ToCanvas"/> once everything is painted, and tests can read it.
/// </summary>
internal sealed class ArtSheet
{
    public const int Width = 2048;

    /// <summary>Alpha of a texel that is lit from inside after dark and stays lit all night (lamps, shops, signs).</summary>
    public const byte PublicLight = 204;

    /// <summary>Alpha of a texel lit in the evening but dark late at night (the windows of homes).</summary>
    public const byte HomeLight = 153;

    private readonly List<(string Key, Art Region, PixelCanvas Canvas)> faces = new();
    private readonly Dictionary<string, Art> byKey = new();
    private int shelfY, shelfHeight, cursorX;

    public int Height => Math.Max(4, shelfY + shelfHeight);

    /// <summary>Every painted face with its key, in the order they were painted.</summary>
    public IEnumerable<(string Key, PixelCanvas Canvas)> Faces
    {
        get { foreach (var (key, _, canvas) in faces) yield return (key, canvas); }
    }

    /// <summary>
    /// The face called <paramref name="key"/>, painted on first use into a transparent canvas of the given size.
    /// Asking again for the same key returns the same art without painting.
    /// </summary>
    public Art Paint(string key, int width, int height, Action<PixelCanvas> paint)
    {
        if (byKey.TryGetValue(key, out var found)) return found;
        if (width < 1 || height < 1 || width + 2 > Width)
            throw new ArgumentException($"Art '{key}' is {width}x{height}; a face must be 1 to {Width - 2} texels wide.");

        var canvas = new PixelCanvas(width, height);
        paint(canvas);

        if (cursorX + width + 2 > Width)
        {
            shelfY += shelfHeight;
            shelfHeight = 0;
            cursorX = 0;
        }
        var art = new Art(cursorX + 1, shelfY + 1, width, height);
        cursorX += width + 2;
        shelfHeight = Math.Max(shelfHeight, height + 2);
        faces.Add((key, art, canvas));
        byKey[key] = art;
        return art;
    }

    /// <summary>The whole sheet: every face in its place, its edge texels repeated once all round.</summary>
    public PixelCanvas ToCanvas()
    {
        var sheet = new PixelCanvas(Width, Height);
        foreach (var (_, r, c) in faces)
            for (int y = -1; y <= r.Height; y++)
                for (int x = -1; x <= r.Width; x++)
                    sheet.SetRaw(r.X + x, r.Y + y, c.Get(Math.Clamp(x, 0, r.Width - 1), Math.Clamp(y, 0, r.Height - 1)));
        return sheet;
    }
}
