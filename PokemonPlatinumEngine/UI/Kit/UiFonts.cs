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
    // Large enough that 60-unit text stays sharp when the layout is rendered at 4K
    internal const int AtlasSize = 160;
    private static readonly Dictionary<UiWeight, Font> Fonts = new();
    private static readonly Dictionary<UiWeight, Font> ExtraFonts = new();

    /// <summary>
    /// The characters the first face is loaded with: Latin-1 plus the dashes, the ellipsis and the arrows the menus use
    /// (the arrows are not in Nunito and are asked for all the same). Anything else is drawn as a question mark unless
    /// <see cref="ExtraCodepoints"/> has it; <see cref="TextMetrics.CanDraw"/> answers for one character, with no
    /// window (plan 16 · T8).
    /// </summary>
    internal static readonly int[] Codepoints = Enumerable.Range(32, 224).Concat(new[] { 0x2013, 0x2014, 0x2026, 0x2190, 0x2191, 0x2192, 0x2193 }).ToArray();

    /// <summary>
    /// The Latin half of plan 17 · N5: the second face, a second atlas of the same typeface and size that is made the
    /// first time a text needs a glyph from it, so English (and every text within Latin-1) pays nothing for it. It holds
    /// Latin Extended-A (Romanian ă â î, Polish, Czech, Turkish, Hungarian, the Baltic languages…), the comma-below
    /// letters Ș ș Ț ț (Romanian proper; the cedilla forms Ş ş Ţ ţ are in Extended-A), the curly and low quotation marks,
    /// the guillemets, the euro and the trade mark. A codepoint the typeface has no glyph for is left out when the atlas
    /// is made (raylib skips it) and measured and drawn as a question mark, like any other.
    /// </summary>
    internal static readonly int[] ExtraCodepoints = Enumerable.Range(0x100, 0x80)
        .Concat(new[] { 0x218, 0x219, 0x21A, 0x21B, 0x1E9E, 0x2018, 0x2019, 0x201A, 0x201C, 0x201D, 0x201E, 0x2039, 0x203A, 0x20AC, 0x2122 })
        .ToArray();

    private static readonly HashSet<int> ExtraSet = new(ExtraCodepoints);

    internal static string FileFor(UiWeight weight) => weight switch
    {
        UiWeight.Black => "Nunito-Black.ttf",
        UiWeight.ExtraBold => "Nunito-ExtraBold.ttf",
        _ => "Nunito-Bold.ttf"
    };

    public static Font Get(UiWeight weight) => Load(Fonts, weight, Codepoints);

    /// <summary>The second face of a weight (<see cref="ExtraCodepoints"/>), made on first use.</summary>
    public static Font GetExtra(UiWeight weight) => Load(ExtraFonts, weight, ExtraCodepoints);

    private static Font Load(Dictionary<UiWeight, Font> kept, UiWeight weight, int[] codepoints)
    {
        if (kept.TryGetValue(weight, out var font)) return font;
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", FileFor(weight));
        if (File.Exists(path))
        {
            font = PokemonPlatinumEngine.Graphics.Gpu.FontFrom(path, AtlasSize, codepoints, "font");
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
        kept[weight] = font;
        return font;
    }

    /// <summary>Letter spacing: Nunito is set a touch open at small sizes and tighter at display sizes.</summary>
    internal static float Tracking(float size) => size >= 40 ? 0f : size <= 20 ? 0.8f : 0.4f;

    /// <summary>
    /// Whether a character is drawn from the second face: one of <see cref="ExtraCodepoints"/> that the weight's file
    /// has a glyph for. Anything within Latin-1 and the dashes is the first face's, and so is a character neither has
    /// (which comes out as the question mark).
    /// </summary>
    internal static bool InExtra(int codepoint, UiWeight weight) =>
        codepoint > 0xFF && ExtraSet.Contains(codepoint) && TextMetrics.HasExtra(codepoint, weight);

    /// <summary>
    /// A text cut into runs by the face that has each glyph, in order (<c>Extra</c> says which): one run for the
    /// text of Latin-1, the common case, which is found with a scan of its characters and no allocation.
    /// </summary>
    internal static List<(string Text, bool Extra)> Runs(string text, UiWeight weight)
    {
        var runs = new List<(string, bool)>();
        int start = 0;
        bool extra = false;
        for (int i = 0; i < text.Length; i++)
        {
            bool here = text[i] > 0xFF && InExtra(text[i], weight);
            if (here == extra) continue;
            if (i > start) runs.Add((text[start..i], extra));
            start = i;
            extra = here;
        }
        if (text.Length > start) runs.Add((text[start..], extra));
        return runs;
    }

    private static bool NeedsExtra(string text, UiWeight weight)
    {
        foreach (char c in text)
            if (c > 0xFF && InExtra(c, weight)) return true;
        return false;
    }

    /// <summary>
    /// A text as it is drawn: the no-break and narrow no-break spaces (French sets one before ? ! : ; and inside «»;
    /// Nunito's glyph for them has no outline and no width, so they would close the gap up) are the plain space's width.
    /// They are no break either, since <see cref="TextMetrics.Wrap(string, float, Func{string, float})"/> breaks at the
    /// plain space alone.
    /// </summary>
    internal static string Spaced(string text) =>
        text.AsSpan().IndexOfAny(' ', ' ') < 0 ? text : text.Replace(' ', ' ').Replace(' ', ' ');

    public static void Draw(string text, float x, float y, float size, Color color, UiWeight weight = UiWeight.Bold)
    {
        text = Spaced(text);
        float tracking = Tracking(size);
        if (!NeedsExtra(text, weight))
        {
            Raylib.DrawTextEx(Get(weight), text, new Vector2(MathF.Round(x), MathF.Round(y)), size, tracking, color);
            return;
        }
        // Runs by face, each drawn where the one before ended: the tracking sits between two runs as between two letters
        float at = MathF.Round(x);
        y = MathF.Round(y);
        foreach (var (part, extra) in Runs(text, weight))
        {
            var font = extra ? GetExtra(weight) : Get(weight);
            Raylib.DrawTextEx(font, part, new Vector2(at, y), size, tracking, color);
            at += Raylib.MeasureTextEx(font, part, size, tracking).X + tracking;
        }
    }

    public static float Measure(string text, float size, UiWeight weight = UiWeight.Bold)
    {
        text = Spaced(text);
        float tracking = Tracking(size);
        if (!NeedsExtra(text, weight)) return Raylib.MeasureTextEx(Get(weight), text, size, tracking).X;
        float width = 0f;
        var runs = Runs(text, weight);
        foreach (var (part, extra) in runs)
            width += Raylib.MeasureTextEx(extra ? GetExtra(weight) : Get(weight), part, size, tracking).X;
        return width + (runs.Count - 1) * tracking;
    }

    /// <summary>Draws text with its cap height centred on <paramref name="centerY"/>.</summary>
    public static void DrawCentered(string text, float x, float centerY, float size, Color color, UiWeight weight = UiWeight.Bold) =>
        Draw(text, x, centerY - size * 0.56f, size, color, weight);

    // ------------------------------------------------------------------ display lettering

    private const int DisplaySize = 300;
    private static Font? display;
    private static Shader gradient;
    private static bool gradientLoaded;

    /// <summary>The Black weight rasterised very large, for title lettering.</summary>
    public static Font Display
    {
        get
        {
            if (display.HasValue) return display.Value;
            string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", FileFor(UiWeight.Black));
            Font font = Get(UiWeight.Black);
            if (File.Exists(path))
            {
                int[] codepoints = Enumerable.Range(32, 95).Concat(new[] { 0xE9 }).ToArray();
                var loaded = PokemonPlatinumEngine.Graphics.Gpu.FontFrom(path, DisplaySize, codepoints, "font");
                if (loaded.Texture.Id != 0)
                {
                    Raylib.GenTextureMipmaps(ref loaded.Texture);
                    Raylib.SetTextureFilter(loaded.Texture, TextureFilter.Trilinear);
                    font = loaded;
                }
            }
            display = font;
            return font;
        }
    }

    public static float MeasureDisplay(string text, float size, float spacing) =>
        Raylib.MeasureTextEx(Display, text, size, spacing).X;

    private const string GradientVertex = @"#version 330
in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec4 vertexColor;
uniform mat4 mvp;
out vec2 uv;
out vec2 pos;
out vec4 tint;
void main()
{
    uv = vertexTexCoord;
    pos = vertexPosition.xy;
    tint = vertexColor;
    gl_Position = mvp * vec4(vertexPosition, 1.0);
}";

    // Glyph coverage from the font atlas, coloured by a vertical gradient with a slanted band of light
    private const string GradientFragment = @"#version 330
in vec2 uv;
in vec2 pos;
in vec4 tint;
uniform sampler2D texture0;
uniform vec4 top;
uniform vec4 bottom;
uniform vec2 span;
uniform vec3 shine;
out vec4 finalColor;
void main()
{
    float coverage = texture(texture0, uv).a;
    float t = clamp((pos.y - span.x) / (span.y - span.x), 0.0, 1.0);
    vec4 c = mix(top, bottom, t);
    float band = exp(-pow((pos.x + (pos.y - span.x) * 0.5 - shine.x) / shine.y, 2.0)) * shine.z;
    finalColor = vec4(c.rgb + band, c.a * coverage * tint.a);
}";

    /// <summary>
    /// Display lettering filled with a top-to-bottom gradient. <paramref name="shineX"/> is where a slanted band
    /// of light crosses the text (layout units; NaN for none).
    /// </summary>
    public static void DrawDisplayGradient(string text, float x, float y, float size, float spacing, Color top, Color bottom,
        float alpha = 1f, float shineX = float.NaN)
    {
        if (!gradientLoaded)
        {
            gradient = PokemonPlatinumEngine.Graphics.Gpu.ShaderFrom(GradientVertex, GradientFragment, "shader");
            gradientLoaded = true;
        }
        static Vector4 V(Color c) => new(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
        Raylib.BeginShaderMode(gradient);
        Raylib.SetShaderValue(gradient, Graphics.FieldShaders.Location(gradient, "top"), V(top), ShaderUniformDataType.Vec4);
        Raylib.SetShaderValue(gradient, Graphics.FieldShaders.Location(gradient, "bottom"), V(bottom), ShaderUniformDataType.Vec4);
        Raylib.SetShaderValue(gradient, Graphics.FieldShaders.Location(gradient, "span"), new Vector2(y + size * 0.2f, y + size * 0.92f), ShaderUniformDataType.Vec2);
        Raylib.SetShaderValue(gradient, Graphics.FieldShaders.Location(gradient, "shine"),
            new Vector3(float.IsNaN(shineX) ? -1e6f : shineX, size * 0.35f, float.IsNaN(shineX) ? 0f : 0.55f), ShaderUniformDataType.Vec3);
        Raylib.DrawTextEx(Display, text, new Vector2(x, y), size, spacing, new Color(255, 255, 255, (int)(255 * Math.Clamp(alpha, 0f, 1f))));
        Raylib.EndShaderMode();
    }

    public static void DrawDisplay(string text, float x, float y, float size, float spacing, Color color) =>
        Raylib.DrawTextEx(Display, text, new Vector2(x, y), size, spacing, color);
}
