using System;
using System.Numerics;

namespace PokemonPlatinumEngine.Core;

/// <summary>What the mouse did in a frame (plan 12 · Q6), beside the frame's actions.</summary>
[Flags]
public enum PointerButtons : byte
{
    None = 0,
    /// <summary>The pointer moved this frame.</summary>
    Moved = 1,
    /// <summary>The left button went down this frame.</summary>
    Click = 2,
    /// <summary>The right button went down this frame.</summary>
    RightClick = 4,
    /// <summary>The left button is held down now.</summary>
    Held = 8,
    /// <summary>The pointer stands on the picture, not on the bars round it.</summary>
    Inside = 16
}

/// <summary>
/// A frame of the mouse (plan 12 · Q6): where the pointer stands in the interface's own 1920×1080 units, what its
/// buttons did and how far the wheel turned (up is positive). Kept in layout units, so a recording plays the same in a
/// window of any size; the default is a mouse that did nothing.
/// </summary>
public readonly record struct PointerFrame(float X, float Y, PointerButtons Buttons, float Wheel)
{
    public Vector2 At => new(X, Y);
    public bool Moved => (Buttons & PointerButtons.Moved) != 0;
    public bool Click => (Buttons & PointerButtons.Click) != 0;
    public bool RightClick => (Buttons & PointerButtons.RightClick) != 0;
    public bool Held => (Buttons & PointerButtons.Held) != 0;
    public bool Inside => (Buttons & PointerButtons.Inside) != 0;

    /// <summary>Whether the mouse did anything this frame: moved, a button went down, the wheel turned.</summary>
    public bool Any => Moved || Click || RightClick || Wheel != 0f;

    /// <summary>A frame of a pointer standing still at a place (a tool's: the harness and the tests point with it).</summary>
    public static PointerFrame Over(Vector2 at,PointerButtons buttons = PointerButtons.Moved) => new(at.X, at.Y, buttons | PointerButtons.Inside, 0f);
}

/// <summary>
/// Where the picture stands in the window (plan 12 · Q6): the 1920×1080 layout scaled by <see cref="Scale"/> and set
/// at <see cref="X"/>, <see cref="Y"/> in the window's pixels, as <c>GameEngine.Draw</c> fits it, letterboxed or
/// pillarboxed. GPU-free: <see cref="ToLayout"/> is the transform turned round.
/// </summary>
public readonly record struct Viewport(float X, float Y, float Scale)
{
    public const float LayoutWidth = 1920f, LayoutHeight = 1080f;

    /// <summary>The picture filling a window of exactly its own size.</summary>
    public static readonly Viewport Identity = new(0f, 0f, 1f);

    /// <summary>How a window of this size shows the picture: as large as fits, in the middle, with bars where it doesn't.</summary>
    public static Viewport Fit(int windowWidth, int windowHeight)
    {
        float scale = MathF.Min(windowWidth / LayoutWidth, windowHeight / LayoutHeight);
        int w = (int)(LayoutWidth * scale), h = (int)(LayoutHeight * scale);
        return new Viewport((windowWidth - w) / 2, (windowHeight - h) / 2, scale);
    }

    /// <summary>A point of the window in the layout's units.</summary>
    public Vector2 ToLayout(Vector2 window) => Scale <= 0f ? window : (window - new Vector2(X, Y)) / Scale;

    /// <summary>Whether a point of the layout is on the picture.</summary>
    public static bool OnThePicture(Vector2 layout) => layout.X >= 0f && layout.Y >= 0f && layout.X < LayoutWidth && layout.Y < LayoutHeight;
}
