using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.UI.Kit;

/// <summary>Weights of the interface typeface (Nunito, SIL Open Font License; see docs/art/CREDITS.md).</summary>
public enum UiWeight { Bold, ExtraBold, Black }

/// <summary>
/// The interface typeface, loaded once per weight from Assets/Fonts next to the executable. Glyphs are rasterised
/// large and mipmapped, so text stays smooth from captions to titles.
/// </summary>
public static class UiFonts
{
    private const int AtlasSize = 96;
    private static readonly Dictionary<UiWeight, Font> Fonts = new();

    private static string FileFor(UiWeight weight) => weight switch
    {
        UiWeight.Black => "Nunito-Black.ttf",
        UiWeight.ExtraBold => "Nunito-ExtraBold.ttf",
        _ => "Nunito-Bold.ttf"
    };

    public static Font Get(UiWeight weight)
    {
        if (Fonts.TryGetValue(weight, out var font)) return font;
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", FileFor(weight));
        if (File.Exists(path))
        {
            // Latin-1 plus the arrows and symbols the menus use
            int[] codepoints = Enumerable.Range(32, 224).Concat(new[] { 0x2013, 0x2014, 0x2026, 0x2190, 0x2191, 0x2192, 0x2193 }).ToArray();
            font = Raylib.LoadFontEx(path, AtlasSize, codepoints, codepoints.Length);
            if (font.Texture.Id != 0)
            {
                Raylib.GenTextureMipmaps(ref font.Texture);
                Raylib.SetTextureFilter(font.Texture, TextureFilter.Trilinear);
            }
        }
        else
        {
            font = Raylib.GetFontDefault();
        }
        Fonts[weight] = font;
        return font;
    }

    /// <summary>Letter spacing: Nunito is set a touch open at small sizes and tighter at display sizes.</summary>
    private static float Tracking(float size) => size >= 40 ? 0f : size <= 20 ? 0.8f : 0.4f;

    public static void Draw(string text, float x, float y, float size, Color color, UiWeight weight = UiWeight.Bold) =>
        Raylib.DrawTextEx(Get(weight), text, new Vector2(MathF.Round(x), MathF.Round(y)), size, Tracking(size), color);

    public static float Measure(string text, float size, UiWeight weight = UiWeight.Bold) =>
        Raylib.MeasureTextEx(Get(weight), text, size, Tracking(size)).X;

    /// <summary>Draws text with its cap height centred on <paramref name="centerY"/>.</summary>
    public static void DrawCentered(string text, float x, float centerY, float size, Color color, UiWeight weight = UiWeight.Bold) =>
        Draw(text, x, centerY - size * 0.56f, size, color, weight);
}
