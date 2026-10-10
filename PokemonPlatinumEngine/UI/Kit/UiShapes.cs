using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.UI.Kit;

/// <summary>
/// Anti-aliased interface shapes drawn from signed distances on the GPU: rounded panels and pills with vertical
/// gradients, borders, slanted sides and soft drop shadows, plus rings, lines and triangles for icons and arrows.
/// raylib's own shapes have jagged edges; these stay clean at any size.
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
uniform vec2 turn;
uniform vec4 colorTop;
uniform vec4 colorBottom;
uniform vec4 borderColor;
uniform float borderWidth;
uniform float softness;
uniform float aa;
out vec4 finalColor;

float RoundBox(vec2 p, vec2 b, float r)
{
    vec2 q = abs(p) - b + r;
    return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
}

void main()
{
    vec2 p = vec2(turn.x * local.x + turn.y * local.y, turn.x * local.y - turn.y * local.x);
    p.x += p.y * skew;
    float d = RoundBox(p, halfSize, min(radius, min(halfSize.x, halfSize.y)));
    float alpha = 1.0 - smoothstep(-softness * 0.5, softness * 0.5, d);
    if (alpha <= 0.0) discard;
    vec4 fill = mix(colorTop, colorBottom, clamp(p.y / (halfSize.y * 2.0) + 0.5, 0.0, 1.0));
    if (borderWidth > 0.0)
    {
        float b = smoothstep(-borderWidth - aa, -borderWidth + aa, d);
        fill = mix(fill, borderColor, b);
    }
    finalColor = vec4(fill.rgb, fill.a * alpha);
}";

    // Distance to a triangle (Inigo Quilez), with rounded corners
    private const string TriangleFragment = @"#version 330
in vec2 local;
uniform vec2 p0;
uniform vec2 p1;
uniform vec2 p2;
uniform float rounding;
uniform vec4 color;
uniform float softness;
out vec4 finalColor;

void main()
{
    vec2 e0 = p1 - p0, e1 = p2 - p1, e2 = p0 - p2;
    vec2 v0 = local - p0, v1 = local - p1, v2 = local - p2;
    vec2 q0 = v0 - e0 * clamp(dot(v0, e0) / dot(e0, e0), 0.0, 1.0);
    vec2 q1 = v1 - e1 * clamp(dot(v1, e1) / dot(e1, e1), 0.0, 1.0);
    vec2 q2 = v2 - e2 * clamp(dot(v2, e2) / dot(e2, e2), 0.0, 1.0);
    float s = sign(e0.x * e2.y - e0.y * e2.x);
    vec2 d = min(min(vec2(dot(q0, q0), s * (v0.x * e0.y - v0.y * e0.x)),
                     vec2(dot(q1, q1), s * (v1.x * e1.y - v1.y * e1.x))),
                     vec2(dot(q2, q2), s * (v2.x * e2.y - v2.y * e2.x)));
    float dist = -sqrt(d.x) * sign(d.y) - rounding;
    float alpha = 1.0 - smoothstep(-softness * 0.5, softness * 0.5, dist);
    if (alpha <= 0.0) discard;
    finalColor = vec4(color.rgb, color.a * alpha);
}";

    /// <summary>Real pixels per layout unit (2 when the 1920x1080 layout is rendered at 4K); keeps edges one real pixel soft.</summary>
    public static float PixelScale { get; set; } = 1f;

    private static Shader shader, triangle;
    private static bool loaded;
    private static int locHalf, locRadius, locSkew, locTurn, locTop, locBottom, locBorder, locBorderWidth, locSoft, locAa;
    private static int locP0, locP1, locP2, locRounding, locTriColor, locTriSoft;

    private static void EnsureLoaded()
    {
        if (loaded) return;
        shader = Raylib.LoadShaderFromMemory(Vertex, Fragment);
        locHalf = Graphics.FieldShaders.Location(shader, "halfSize");
        locRadius = Graphics.FieldShaders.Location(shader, "radius");
        locSkew = Graphics.FieldShaders.Location(shader, "skew");
        locTurn = Graphics.FieldShaders.Location(shader, "turn");
        locTop = Graphics.FieldShaders.Location(shader, "colorTop");
        locBottom = Graphics.FieldShaders.Location(shader, "colorBottom");
        locBorder = Graphics.FieldShaders.Location(shader, "borderColor");
        locBorderWidth = Graphics.FieldShaders.Location(shader, "borderWidth");
        locSoft = Graphics.FieldShaders.Location(shader, "softness");
        locAa = Graphics.FieldShaders.Location(shader, "aa");

        triangle = Raylib.LoadShaderFromMemory(Vertex, TriangleFragment);
        locP0 = Graphics.FieldShaders.Location(triangle, "p0");
        locP1 = Graphics.FieldShaders.Location(triangle, "p1");
        locP2 = Graphics.FieldShaders.Location(triangle, "p2");
        locRounding = Graphics.FieldShaders.Location(triangle, "rounding");
        locTriColor = Graphics.FieldShaders.Location(triangle, "color");
        locTriSoft = Graphics.FieldShaders.Location(triangle, "softness");
        loaded = true;
    }

    private static Vector4 V(Color c) => new(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);

    /// <summary>A quad around <paramref name="center"/> whose texture coordinates are the offset from it.</summary>
    private static void Quad(Vector2 center, float hx, float hy)
    {
        Rlgl.Begin(DrawMode.Quads);
        Rlgl.Color4ub(255, 255, 255, 255);
        Rlgl.TexCoord2f(-hx, -hy); Rlgl.Vertex2f(center.X - hx, center.Y - hy);
        Rlgl.TexCoord2f(-hx, hy); Rlgl.Vertex2f(center.X - hx, center.Y + hy);
        Rlgl.TexCoord2f(hx, hy); Rlgl.Vertex2f(center.X + hx, center.Y + hy);
        Rlgl.TexCoord2f(hx, -hy); Rlgl.Vertex2f(center.X + hx, center.Y - hy);
        Rlgl.End();
        Rlgl.DrawRenderBatchActive();
        Graphics.FrameProfiler.Count(2);
    }

    private static void Box(Vector2 center, float hw, float hh, float radius, Color top, Color bottom, Color border, float borderWidth,
        float skew, float softness, Vector2 turn)
    {
        EnsureLoaded();
        Raylib.BeginShaderMode(shader);
        Raylib.SetShaderValue(shader, locHalf, new Vector2(hw, hh), ShaderUniformDataType.Vec2);
        Raylib.SetShaderValue(shader, locRadius, radius, ShaderUniformDataType.Float);
        Raylib.SetShaderValue(shader, locSkew, skew, ShaderUniformDataType.Float);
        Raylib.SetShaderValue(shader, locTurn, turn, ShaderUniformDataType.Vec2);
        Raylib.SetShaderValue(shader, locTop, V(top), ShaderUniformDataType.Vec4);
        Raylib.SetShaderValue(shader, locBottom, V(bottom), ShaderUniformDataType.Vec4);
        Raylib.SetShaderValue(shader, locBorder, V(border), ShaderUniformDataType.Vec4);
        Raylib.SetShaderValue(shader, locBorderWidth, borderWidth, ShaderUniformDataType.Float);
        // Crisp shapes get an edge one real pixel wide; shadows keep their blur in layout units
        float edge = softness <= 1f ? softness / PixelScale : softness;
        Raylib.SetShaderValue(shader, locSoft, Math.Max(0.25f, edge), ShaderUniformDataType.Float);
        Raylib.SetShaderValue(shader, locAa, 0.6f / PixelScale, ShaderUniformDataType.Float);

        float m = softness + MathF.Abs(skew) * hh + 1f;
        if (turn.Y == 0f) Quad(center, hw + m, hh + m);
        else
        {
            float reach = MathF.Sqrt(hw * hw + hh * hh) + m;
            Quad(center, reach, reach);
        }
        Raylib.EndShaderMode();
    }

    /// <summary>
    /// Draws one shape. <paramref name="skew"/> slants the sides (x shifts by skew per pixel below the centre);
    /// <paramref name="softness"/> is the edge width in pixels (1 for crisp anti-aliasing, more for shadows).
    /// </summary>
    public static void Shape(Rectangle r, float radius, Color top, Color bottom, Color border = default, float borderWidth = 0f,
        float skew = 0f, float softness = 1f) =>
        Box(new Vector2(r.X + r.Width / 2f, r.Y + r.Height / 2f), r.Width / 2f, r.Height / 2f, radius, top, bottom, border, borderWidth,
            skew, softness, Vector2.UnitX);

    public static void Fill(Rectangle r, float radius, Color color, float skew = 0f) => Shape(r, radius, color, color, skew: skew);

    /// <summary>A soft drop shadow: the shape offset and blurred by <paramref name="blur"/> pixels.</summary>
    public static void Shadow(Rectangle r, float radius, float blur, Vector2 offset, Color color, float skew = 0f) =>
        Shape(new Rectangle(r.X + offset.X, r.Y + offset.Y, r.Width, r.Height), radius, color, color, skew: skew, softness: blur);

    /// <summary>
    /// A soft ellipse of light: the colour at its middle, fading to nothing at its rim. It is a circle that is
    /// all edge, drawn flattened, so the fade is as even across its short side as along its long one.
    /// </summary>
    public static void Glow(Vector2 center, float rx, float ry, Color color)
    {
        if (rx <= 0f || ry <= 0f) return;
        Rlgl.PushMatrix();
        Rlgl.Translatef(center.X, center.Y, 0f);
        Rlgl.Scalef(1f, ry / rx, 1f);
        Box(Vector2.Zero, rx * 0.5f, rx * 0.5f, rx * 0.5f, color, color, default, 0f, 0f, rx, Vector2.UnitX);
        Rlgl.PopMatrix();
    }

    public static void Circle(Vector2 center, float r, Color color) =>
        Shape(new Rectangle(center.X - r, center.Y - r, r * 2, r * 2), r, color, color);

    /// <summary>A circle outline, <paramref name="thickness"/> wide, inside radius <paramref name="r"/>.</summary>
    public static void Ring(Vector2 center, float r, float thickness, Color color)
    {
        var clear = color with { A = 0 };
        Shape(new Rectangle(center.X - r, center.Y - r, r * 2, r * 2), r, clear, clear, color, thickness);
    }

    /// <summary>A rounded box turned by <paramref name="degrees"/> about its middle (a diamond is a square turned by 45).</summary>
    public static void Turned(Vector2 center, float halfWidth, float halfHeight, float radius, float degrees, Color color)
    {
        float a = degrees * MathF.PI / 180f;
        Box(center, halfWidth, halfHeight, radius, color, color, default, 0f, 0f, 1f, new Vector2(MathF.Cos(a), MathF.Sin(a)));
    }

    /// <summary>A line with round ends.</summary>
    public static void Line(Vector2 a, Vector2 b, float thickness, Color color)
    {
        var d = b - a;
        float length = d.Length();
        if (length < 0.001f)
        {
            Circle(a, thickness / 2f, color);
            return;
        }
        Box((a + b) / 2f, (length + thickness) / 2f, thickness / 2f, thickness / 2f, color, color, default, 0f, 0f, 1f, d / length);
    }

    /// <summary>A filled triangle; <paramref name="rounding"/> grows it outward with round corners.</summary>
    public static void Triangle(Vector2 a, Vector2 b, Vector2 c, Color color, float rounding = 0f)
    {
        EnsureLoaded();
        var min = Vector2.Min(a, Vector2.Min(b, c));
        var max = Vector2.Max(a, Vector2.Max(b, c));
        var center = (min + max) / 2f;
        Raylib.BeginShaderMode(triangle);
        Raylib.SetShaderValue(triangle, locP0, a - center, ShaderUniformDataType.Vec2);
        Raylib.SetShaderValue(triangle, locP1, b - center, ShaderUniformDataType.Vec2);
        Raylib.SetShaderValue(triangle, locP2, c - center, ShaderUniformDataType.Vec2);
        Raylib.SetShaderValue(triangle, locRounding, rounding, ShaderUniformDataType.Float);
        Raylib.SetShaderValue(triangle, locTriColor, V(color), ShaderUniformDataType.Vec4);
        Raylib.SetShaderValue(triangle, locTriSoft, 1f / PixelScale, ShaderUniformDataType.Float);
        var half = (max - min) / 2f + new Vector2(rounding + 2f);
        Quad(center, half.X, half.Y);
        Raylib.EndShaderMode();
    }
}
