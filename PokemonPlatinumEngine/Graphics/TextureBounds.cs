using System.Collections.Generic;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Where in a texture anything is drawn: the smallest rectangle round its opaque texels, read back once per texture.
/// The Pokédex's size page stands sprites on a line by their feet and scales them by their real heights with it.
/// </summary>
internal static class TextureBounds
{
    private static readonly Dictionary<(uint Id, int Width, int Height), Rectangle> cache = new();

    /// <summary>The rectangle round every texel more than half opaque; the whole texture when nothing is.</summary>
    public static Rectangle Of(Texture2D texture)
    {
        var key = (texture.Id, texture.Width, texture.Height);
        if (cache.TryGetValue(key, out var bounds)) return bounds;
        var image = Raylib.LoadImageFromTexture(texture);
        var canvas = PixelCanvas.FromImage(image);
        Raylib.UnloadImage(image);

        int minX = canvas.Width, minY = canvas.Height, maxX = -1, maxY = -1;
        for (int y = 0; y < canvas.Height; y++)
            for (int x = 0; x < canvas.Width; x++)
            {
                if (canvas.Get(x, y).A <= 127) continue;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        bounds = maxX < 0 ? new Rectangle(0, 0, texture.Width, texture.Height) : new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
        cache[key] = bounds;
        return bounds;
    }
}
