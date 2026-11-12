using System.Numerics;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The picture's shape for a window of any shape (rulings, "Other shapes of screen, and the resolution"; style guide,
/// "Quality presets"). GPU-free. The interface lays out in its 1920×1080 <b>safe area</b> as it always has; the
/// <b>canvas</b> round it, in the same layout units, takes the window's shape: 1080 tall and wider where the window is
/// wider than 16:9, 1920 wide and taller where it is narrower, held between 4:3 and 32:9. A window within 1 % of 16:9
/// is <see cref="Standard"/>, the picture to the unit it always was.
/// </summary>
public readonly record struct AspectLayout(int CanvasWidth, int CanvasHeight)
{
    public const int SafeWidth = 1920, SafeHeight = 1080;

    /// <summary>The narrowest and the widest canvas: a window beyond shows the rest in bars.</summary>
    public const float Narrowest = 4f / 3f, Widest = 32f / 9f;

    /// <summary>How near 16:9 a window must be to be 16:9 to the unit (a share of the ratio).</summary>
    public const float Snap = 0.01f;

    public static readonly AspectLayout Standard = new(SafeWidth, SafeHeight);

    /// <summary>Whether this is the 16:9 picture, safe area and canvas one.</summary>
    public bool IsStandard => CanvasWidth == SafeWidth && CanvasHeight == SafeHeight;

    /// <summary>How far the safe area stands in from the canvas's left edge, in layout units.</summary>
    public int PadX => (CanvasWidth - SafeWidth) / 2;

    /// <summary>How far the safe area stands down from the canvas's top edge, in layout units.</summary>
    public int PadY => (CanvasHeight - SafeHeight) / 2;

    public float Aspect => (float)CanvasWidth / CanvasHeight;

    /// <summary>The canvas for a window (or a monitor) of this many pixels.</summary>
    public static AspectLayout For(int width, int height)
    {
        if (width <= 0 || height <= 0) return Standard;
        float aspect = (float)width / height;
        const float standard = 16f / 9f;
        if (MathF.Abs(aspect / standard - 1f) < Snap) return Standard;
        aspect = Math.Clamp(aspect, Narrowest, Widest);
        if (aspect > standard) return new AspectLayout(Math.Max(SafeWidth, Even(SafeHeight * aspect)), SafeHeight);
        return new AspectLayout(SafeWidth, Math.Max(SafeHeight, Even(SafeWidth / aspect)));
    }

    private static int Even(float value) => (int)MathF.Round(value / 2f) * 2;

    /// <summary>
    /// The vertical field of view a 3D camera takes on this canvas so that the safe area shows exactly what a 16:9
    /// picture showed with <paramref name="fovYDegrees"/>: the same where the canvas is 1080 tall (wider sees more to the
    /// sides), wider where it is taller (narrower sees more above and below). The very number given on 16:9.
    /// </summary>
    public float FovYFor(float fovYDegrees)
    {
        if (CanvasHeight == SafeHeight) return fovYDegrees;
        double half = Math.Tan(fovYDegrees * Math.PI / 360.0) * CanvasHeight / SafeHeight;
        return (float)(Math.Atan(half) * 360.0 / Math.PI);
    }

    /// <summary>How the canvas is shown in a window: as large as fits, in the middle (bars only past the canvas's own shape).</summary>
    public (float X, float Y, float Scale, int Width, int Height) FitIn(int windowWidth, int windowHeight)
    {
        float scale = MathF.Min((float)windowWidth / CanvasWidth, (float)windowHeight / CanvasHeight);
        int w = (int)(CanvasWidth * scale), h = (int)(CanvasHeight * scale);
        return ((windowWidth - w) / 2, (windowHeight - h) / 2, scale, w, h);
    }

    /// <summary>
    /// Where the safe area stands in the window: what the mouse is read by (<see cref="Viewport"/>, whose
    /// <see cref="Viewport.ToLayout"/> then gives safe-area units as it always did).
    /// </summary>
    public Viewport ViewportIn(int windowWidth, int windowHeight)
    {
        var (x, y, scale, _, _) = FitIn(windowWidth, windowHeight);
        return new Viewport(x + PadX * scale, y + PadY * scale, scale);
    }

    /// <summary>A point of the canvas in the safe area's units.</summary>
    public Vector2 ToSafe(Vector2 canvas) => canvas - new Vector2(PadX, PadY);

    /// <summary>The monitor's own size, or 1920×1080 where the system said nothing.</summary>
    public static (int Width, int Height) NativeOr(int width, int height) => width > 0 && height > 0 ? (width, height) : (1920, 1080);

    /// <summary>The heights the resolution's choices are made at, the monitor's own added.</summary>
    public static readonly int[] Heights = { 720, 900, 1080, 1200, 1440, 1600, 1800, 2160 };

    /// <summary>
    /// The sizes offered for a monitor: its own ratio at each of <see cref="Heights"/> it can show, and its own size
    /// last. Widths are even; a ratio near 16:9 offers 16:9's own widths (1366×768 offers 1280×720).
    /// </summary>
    public static IReadOnlyList<(int Width, int Height)> SizesFor(int nativeWidth, int nativeHeight)
    {
        var (nw, nh) = NativeOr(nativeWidth, nativeHeight);
        float aspect = (float)nw / nh;
        if (MathF.Abs(aspect / (16f / 9f) - 1f) < Snap) aspect = 16f / 9f;
        var sizes = new List<(int, int)>();
        foreach (int h in Heights)
        {
            if (h >= nh) break;
            int w = Even(h * aspect);
            if (w >= nw) break;
            sizes.Add((w, h));
        }
        sizes.Add((nw, nh));
        return sizes;
    }

    /// <summary>
    /// The largest window of a monitor's own ratio that it shows with its title bar and the taskbar (80 pixels): a
    /// 21:9 monitor's window is 21:9. A screen of exactly 1280×800 (the Steam Deck's) takes the whole of it.
    /// </summary>
    public static (int Width, int Height) LargestWindowFor(int monitorWidth, int monitorHeight)
    {
        var (nw, nh) = NativeOr(monitorWidth, monitorHeight);
        if ((nw, nh) == (1280, 800)) return (1280, 800);
        var best = (1280, 720);
        foreach (var (w, h) in SizesFor(nw, nh))
            if (w <= nw && h <= nh - 80) best = (w, h);
        return best;
    }

    /// <summary>
    /// The most of its target a scene is drawn at for a full-screen RESOLUTION of <paramref name="chosenHeight"/> rows
    /// (nought for NATIVE) when the target is <paramref name="targetHeight"/> rows: 1 at NATIVE or where the target is no
    /// taller, else the share that brings it to the chosen height.
    /// </summary>
    public static float CapFor(int chosenHeight, int targetHeight) =>
        chosenHeight <= 0 || targetHeight <= chosenHeight ? 1f : (float)chosenHeight / targetHeight;

    /// <summary>A name for a size's ratio: 16:9, 16:10, 21:9, 32:9, 4:3, 5:4 or 3:4 where it is near one, else the size's own.</summary>
    public static string RatioName(int width, int height)
    {
        if (width <= 0 || height <= 0) return "16:9";
        float aspect = (float)width / height;
        foreach (var (name, value) in Ratios)
            if (MathF.Abs(aspect / value - 1f) < 0.03f) return name;
        int g = Gcd(width, height);
        return $"{width / g}:{height / g}";
    }

    private static readonly (string, float)[] Ratios =
    {
        ("16:9", 16f / 9f), ("16:10", 16f / 10f), ("21:9", 64f / 27f), ("32:9", 32f / 9f), ("4:3", 4f / 3f), ("5:4", 5f / 4f),
        ("3:4", 3f / 4f), ("9:16", 9f / 16f),
    };

    private static int Gcd(int a, int b) => b == 0 ? a : Gcd(b, a % b);
}
