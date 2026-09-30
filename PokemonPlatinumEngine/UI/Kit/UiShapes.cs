using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.UI.Kit;

/// <summary>
/// Anti-aliased interface shapes drawn from signed distances on the GPU: rounded panels and pills with vertical
/// gradients, borders, slanted sides and soft drop shadows. raylib's own rounded rectangles have jagged edges
/// at 1080p; these stay clean at any size.
/// </summary>
public static class UiShapes
{
    private const string Vertex = @"#version 330
in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec4 vertexColor;
uniform mat4 mvp;
out vec2 local;
void main()
{
    local = vertexTexCoord;
    gl_Position = mvp * vec4(vertexPosition, 1.0);
}";

    // local = pixel offset from the shape's centre
    private const string Fragment = @"#version 330
in vec2 local;
uniform vec2 halfSize;
uniform float radius;
uniform float skew;
uniform vec4 colorTop;
uniform vec4 colorBottom;
uniform vec4 borderColor;
uniform float borderWidth;
uniform float softness;
out vec4 finalColor;

float RoundBox(vec2 p, vec2 b, float r)
{
    vec2 q = abs(p) - b + r;
    return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
}

void main()
{
    vec2 p = local;
    p.x += p.y * skew;
    float d = RoundBox(p, halfSize, min(radius, min(halfSize.x, halfSize.y)));
    float alpha = 1.0 - smoothstep(-softness * 0.5, softness * 0.5, d);
    if (alpha <= 0.0) discard;
    vec4 fill = mix(colorTop, colorBottom, clamp(local.y / (halfSize.y * 2.0) + 0.5, 0.0, 1.0));
    if (borderWidth > 0.0)
    {
        float b = smoothstep(-borderWidth - 0.6, -borderWidth + 0.6, d);
        fill = mix(fill, borderColor, b);
    }
    finalColor = vec4(fill.rgb, fill.a * alpha);
}";

    private static Shader shader;
    private static bool loaded;
    private static int locHalf, locRadius, locSkew, locTop, locBottom, locBorder, locBorderWidth, locSoft;

    private static void EnsureLoaded()
    {
        if (loaded) return;
        shader = Raylib.LoadShaderFromMemory(Vertex, Fragment);
        locHalf = Raylib.GetShaderLocation(shader, "halfSize");
        locRadius = Raylib.GetShaderLocation(shader, "radius");
        locSkew = Raylib.GetShaderLocation(shader, "skew");
        locTop = Raylib.GetShaderLocation(shader, "colorTop");
        locBottom = Raylib.GetShaderLocation(shader, "colorBottom");
        locBorder = Raylib.GetShaderLocation(shader, "borderColor");
        locBorderWidth = Raylib.GetShaderLocation(shader, "borderWidth");
        locSoft = Raylib.GetShaderLocation(shader, "softness");
        loaded = true;
    }

    private static Vector4 V(Color c) => new(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);

    /// <summary>
    /// Draws one shape. <paramref name="skew"/> slants the sides (x shifts by skew per pixel below the centre);
    /// <paramref name="softness"/> is the edge width in pixels (1 for crisp anti-aliasing, more for shadows).
    /// </summary>
    public static void Shape(Rectangle r, float radius, Color top, Color bottom, Color border = default, float borderWidth = 0f,
        float skew = 0f, float softness = 1f)
    {
        EnsureLoaded();
        float hw = r.Width / 2f, hh = r.Height / 2f;
        float cx = r.X + hw, cy = r.Y + hh;
        float m = softness + MathF.Abs(skew) * hh + 1f;

        Raylib.BeginShaderMode(shader);
        Raylib.SetShaderValue(shader, locHalf, new Vector2(hw, hh), ShaderUniformDataType.Vec2);
        Raylib.SetShaderValue(shader, locRadius, radius, ShaderUniformDataType.Float);
        Raylib.SetShaderValue(shader, locSkew, skew, ShaderUniformDataType.Float);
        Raylib.SetShaderValue(shader, locTop, V(top), ShaderUniformDataType.Vec4);
        Raylib.SetShaderValue(shader, locBottom, V(bottom), ShaderUniformDataType.Vec4);
        Raylib.SetShaderValue(shader, locBorder, V(border), ShaderUniformDataType.Vec4);
        Raylib.SetShaderValue(shader, locBorderWidth, borderWidth, ShaderUniformDataType.Float);
        Raylib.SetShaderValue(shader, locSoft, Math.Max(0.5f, softness), ShaderUniformDataType.Float);

        Rlgl.Begin(DrawMode.Quads);
        Rlgl.Color4ub(255, 255, 255, 255);
        Rlgl.TexCoord2f(-hw - m, -hh - m); Rlgl.Vertex2f(cx - hw - m, cy - hh - m);
        Rlgl.TexCoord2f(-hw - m, hh + m); Rlgl.Vertex2f(cx - hw - m, cy + hh + m);
        Rlgl.TexCoord2f(hw + m, hh + m); Rlgl.Vertex2f(cx + hw + m, cy + hh + m);
        Rlgl.TexCoord2f(hw + m, -hh - m); Rlgl.Vertex2f(cx + hw + m, cy - hh - m);
        Rlgl.End();
        Rlgl.DrawRenderBatchActive();
        Raylib.EndShaderMode();
    }

    public static void Fill(Rectangle r, float radius, Color color, float skew = 0f) => Shape(r, radius, color, color, skew: skew);

    /// <summary>A soft drop shadow: the shape offset and blurred by <paramref name="blur"/> pixels.</summary>
    public static void Shadow(Rectangle r, float radius, float blur, Vector2 offset, Color color, float skew = 0f) =>
        Shape(new Rectangle(r.X + offset.X, r.Y + offset.Y, r.Width, r.Height), radius, color, color, skew: skew, softness: blur);

    public static void Circle(Vector2 center, float r, Color color) =>
        Shape(new Rectangle(center.X - r, center.Y - r, r * 2, r * 2), r, color, color);
}
