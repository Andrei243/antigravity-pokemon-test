using System.Numerics;
using PokemonPlatinumEngine.Core;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The 2D pass's two spaces (rulings, "Other shapes of screen, and the resolution"): the interface's 1920×1080 safe area,
/// where every screen draws as it always has, and the whole canvas round it, where the scene and what lies over it are
/// drawn. <see cref="Layout"/> is set by <c>GameEngine.Draw</c> alone, once a frame; on a 16:9 picture (and in every
/// test and tool that sets nothing) the two spaces are one and nothing here changes a pixel or a batch.
/// </summary>
internal static class Canvas2D
{
    /// <summary>The picture's shape this frame.</summary>
    public static AspectLayout Layout { get; set; } = AspectLayout.Standard;

    /// <summary>Whether a <see cref="Draw"/> is under way: the 2D camera is the canvas's.</summary>
    public static bool OnTheCanvas { get; private set; }

    /// <summary>The safe area's camera: the interface's layout units at the virtual screen's scale, set in the canvas's middle.</summary>
    public static Camera2D Safe => new()
    {
        Zoom = GameEngine.RenderScale,
        Offset = new Vector2(Layout.PadX * GameEngine.RenderScale, Layout.PadY * GameEngine.RenderScale),
    };

    /// <summary>The canvas's camera: its top left corner at the origin.</summary>
    private static Camera2D Whole => new() { Zoom = GameEngine.RenderScale };

    /// <summary>
    /// Draws over the whole canvas, given its width and height in layout units from its own corner, and goes back to the
    /// safe area after. On a 16:9 picture it is the call itself at 1920×1080.
    /// </summary>
    public static void Draw(Action<int, int> draw)
    {
        if (Layout.IsStandard || OnTheCanvas)
        {
            draw(Layout.CanvasWidth, Layout.CanvasHeight);
            return;
        }
        Raylib.EndMode2D();
        Raylib.BeginMode2D(Whole);
        OnTheCanvas = true;
        try { draw(Layout.CanvasWidth, Layout.CanvasHeight); }
        finally
        {
            OnTheCanvas = false;
            Raylib.EndMode2D();
            Raylib.BeginMode2D(Safe);
        }
    }

    /// <summary>
    /// A screen's full-screen fill (a menu's backdrop, a dim behind a prompt) spread over the whole canvas, in the safe
    /// area's units: the rectangle from the canvas's corner where the screen fills the safe area and a canvas lies round
    /// it, the screen's own rectangle otherwise (16:9, or something smaller than the screen).
    /// </summary>
    public static (int X, int Y, int Width, int Height) Bled(int screenWidth, int screenHeight) =>
        Layout.IsStandard || OnTheCanvas || screenWidth != GameEngine.VirtualWidth || screenHeight != GameEngine.VirtualHeight
            ? (0, 0, screenWidth, screenHeight)
            : (-Layout.PadX, -Layout.PadY, Layout.CanvasWidth, Layout.CanvasHeight);

    /// <summary>
    /// <c>BeginScissorMode</c> for a rectangle given in the virtual screen's pixels of the safe area (layout units times
    /// <see cref="GameEngine.RenderScale"/>, as every screen gives it): moved by the safe area's place on the canvas.
    /// </summary>
    public static void Scissor(int x, int y, int width, int height)
    {
        int dx = OnTheCanvas ? 0 : Layout.PadX * GameEngine.RenderScale, dy = OnTheCanvas ? 0 : Layout.PadY * GameEngine.RenderScale;
        Raylib.BeginScissorMode(x + dx, y + dy, width, height);
    }
}
