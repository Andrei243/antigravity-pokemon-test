using System;
using Raylib_cs;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

public partial class GameEngine
{
    /// <summary>
    /// Whether the start-up draws its bar in the window while it loads (style guide, "Opening and title screen", "The
    /// loading bar"). Only the game itself sets it (<c>Program.Main</c>): a tool's or a test's engine draws nothing
    /// before its first frame, as before.
    /// </summary>
    public bool ShowsLoading { get; set; }

    /// <summary>
    /// Whether the regions' maps are built several at once as the game starts (<see cref="Data.MapDatabase.Initialize(bool)"/>).
    /// Only the game itself asks for it, and a replay of a recording made that way: a tool's or a test's engine builds
    /// them one after another from the one stream of chance, so its seeded runs draw what they always drew.
    /// </summary>
    public bool RegionsTogether { get; init; }

    /// <summary>
    /// The window as the start-up stands: black, with a thin bar a third of its width, filled to
    /// <paramref name="share"/>. Drawn straight into the window, since nothing of the game's own picture is made yet;
    /// it says nothing, so it needs no typeface and no language.
    /// </summary>
    private void ShowLoading(float share)
    {
        if (!ShowsLoading || IsHeadless) return;
        int width = Raylib.GetScreenWidth(), height = Raylib.GetScreenHeight();
        int barWidth = width / 3, barHeight = Math.Max(4, height / 180);
        int x = (width - barWidth) / 2, y = height * 5 / 6;
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);
        Raylib.DrawRectangle(x, y, barWidth, barHeight, ModernUi.Ink);
        Raylib.DrawRectangle(x, y, (int)(barWidth * Math.Clamp(share, 0f, 1f)), barHeight, ModernUi.Gold);
        Raylib.EndDrawing();
    }
}
