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
    private const int AtlasSize = 160;
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
                var loaded = Raylib.LoadFontEx(path, DisplaySize, codepoints, codepoints.Length);
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
            gradient = Raylib.LoadShaderFromMemory(GradientVertex, GradientFragment);
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
