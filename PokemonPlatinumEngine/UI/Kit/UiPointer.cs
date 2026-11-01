using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.UI.Kit;

/// <summary>
/// Something the pointer can be used on (plan 12 · Q6): a screen, or the part of one that is open. GPU-free: what
/// lies under a point is worked out from the same rectangles the screen draws with.
/// </summary>
public interface IPointable
{
    /// <summary>
    /// Which card, row, button or answer lies under a point of the layout; null for nothing,
    /// <see cref="UiPointer.Anywhere"/> where a click anywhere is the screen's Confirm (a line read on).
    /// </summary>
    int? HitTest(Vector2 at);

    /// <summary>The cursor to that index, as the screen's own moves would put it there (a hover).</summary>
    void PointAt(int index);

    /// <summary>A click on that index that does something of its own (a tab, an arrow): true when it did, and the click is then no Confirm.</summary>
    bool Press(int index) => false;
}

/// <summary>An <see cref="IPointable"/> made of two functions, for a screen whose hit test needs what the engine holds.</summary>
public sealed class Pointable : IPointable
{
    private readonly Func<Vector2, int?> hit;
    private readonly Action<int> point;
    private readonly Func<int, bool> press;

    public Pointable(Func<Vector2, int?> hit, Action<int>? point = null, Func<int, bool>? press = null)
    {
        this.hit = hit;
        this.point = point ?? (_ => { });
        this.press = press ?? (_ => false);
    }

    public int? HitTest(Vector2 at) => hit(at);
    public void PointAt(int index) => point(index);
    public bool Press(int index) => press(index);

    /// <summary>A screen where a click anywhere reads a line on, and nothing else is pointed at.</summary>
    public static readonly Pointable ReadsOn = new(_ => UiPointer.Anywhere);
}

/// <summary>
/// The pointer (plan 12 · Q6; style guide, "Shapes and components", "The pointer"): where it stands and whether its
/// arrow is shown, which is only after it moved, until it has stood still for <see cref="HideAfter"/> seconds or a key
/// is pressed, so the keyboard owns the screen while the mouse is still. GPU-free, as are its hit tests.
/// </summary>
public sealed class UiPointer
{
    /// <summary>A hit test's answer where a click anywhere is the screen's Confirm.</summary>
    public const int Anywhere = int.MinValue;

    /// <summary>How long the pointer stands still before its arrow hides.</summary>
    public const float HideAfter = 2f;

    /// <summary>Where it stands, in the layout's units.</summary>
    public Vector2 At { get; private set; }

    /// <summary>Whether its arrow is drawn.</summary>
    public bool Shown { get; private set; }

    private float still;

    /// <summary>One frame: a move or a click shows it where it stands, a key or two seconds still hide it.</summary>
    public void Update(PointerFrame frame, bool keyPressed, float dt)
    {
        if (frame.Moved || frame.Click || frame.RightClick || frame.Wheel != 0f)
        {
            At = frame.At;
            Shown = frame.Inside || Viewport.OnThePicture(frame.At);
            still = 0f;
            return;
        }
        still += dt;
        if (keyPressed || still >= HideAfter) Shown = false;
    }

    /// <summary>Hides it at once (a tool's, or a new scene's).</summary>
    public void Hide() => Shown = false;

    /// <summary>A point of the window in the layout's units, through the picture's place in it (the transform <c>GameEngine.Draw</c> fits it by, turned round).</summary>
    public static Vector2 ToLayout(Viewport viewport, Vector2 window) => viewport.ToLayout(window);

    /// <summary>Whether a point lies in a rectangle (its left and top edges in, its right and bottom out).</summary>
    public static bool Inside(Rectangle r, Vector2 p) => p.X >= r.X && p.Y >= r.Y && p.X < r.X + r.Width && p.Y < r.Y + r.Height;

    /// <summary>The first of the rectangles a point lies in; null when it is in none.</summary>
    public static int? Hit(IReadOnlyList<Rectangle> rects, Vector2 p)
    {
        for (int i = 0; i < rects.Count; i++)
            if (Inside(rects[i], p)) return i;
        return null;
    }
}
