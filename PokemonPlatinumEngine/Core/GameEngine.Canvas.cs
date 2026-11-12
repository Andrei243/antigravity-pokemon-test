using Raylib_cs;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Graphics;

namespace PokemonPlatinumEngine.Core;

// Other shapes of screen (rulings, "Other shapes of screen, and the resolution"; style guide, "Quality presets"): the
// picture's canvas follows the window's shape, the interface keeps its 1920×1080 safe area in the canvas's middle, and
// a full-screen RESOLUTION below the monitor's caps the scene's share of its target. On a 16:9 window nothing here
// changes a target, a camera or a pixel.
public partial class GameEngine
{
    /// <summary>The picture's shape this frame: <see cref="AspectLayout.Standard"/> on a 16:9 window.</summary>
    public AspectLayout Canvas => Canvas2D.Layout;

    /// <summary>
    /// Gives the picture the window's shape: the virtual screen and the scene's targets are made again at the canvas's
    /// size once it changes (never on 16:9, where they are what they always were).
    /// </summary>
    private void FitTheCanvas(int windowWidth, int windowHeight)
    {
        var layout = AspectLayout.For(windowWidth, windowHeight);
        if (layout != Canvas2D.Layout)
        {
            Canvas2D.Layout = layout;
            Gpu.Unload(virtualScreen);
            virtualScreen = Gpu.Target(layout.CanvasWidth * RenderScale, layout.CanvasHeight * RenderScale, "screen");
            Raylib.SetTextureFilter(virtualScreen.Texture, TextureFilter.Bilinear);
            renderContext.SetCanvas(layout.CanvasWidth, layout.CanvasHeight);
            Log.Info($"The picture's canvas: {layout.CanvasWidth}×{layout.CanvasHeight} for a window of {windowWidth}×{windowHeight}.");
        }
        // A full-screen RESOLUTION below the monitor's: the scene drawn at most that many rows
        renderContext.ResolutionCap = Settings.Fullscreen
            ? AspectLayout.CapFor(Settings.FullscreenHeight, (int)(renderContext.Height * renderContext.Quality.SceneScale))
            : 1f;
    }

    /// <summary>The virtual screen's safe area, in its own pixels: what a capture of the interface alone (the card's print) cuts out.</summary>
    internal Rectangle SafePixels => new(Canvas2D.Layout.PadX * RenderScale, Canvas2D.Layout.PadY * RenderScale,
        VirtualWidth * RenderScale, VirtualHeight * RenderScale);
}
