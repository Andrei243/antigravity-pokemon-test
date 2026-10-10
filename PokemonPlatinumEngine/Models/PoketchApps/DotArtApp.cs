using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Models.PoketchApps;

/// <summary>
/// The dot art (<c>dot_artist/main.c</c>): a picture of <see cref="Width"/> by <see cref="Height"/> dots in four
/// shades, 1 the paper to 4 the ink, each dot touched going one shade darker and from the darkest back to the
/// paper (<c>State_UpdateApp</c>). The picture is the one thing of the app the original saves
/// (<c>Poketch_ModifyDotArtData</c>, two bits a dot), so it is kept in the Pokétch's memory as each dot changes
/// and comes back the next time; until the player first touches it, the app shows a picture of its own (ours, a
/// Poké Ball, where the original has its own).
/// </summary>
public sealed class DotArtApp : PoketchAppState
{
    /// <summary>The picture's size in dots (<c>CANVAS_WIDTH</c>, <c>CANVAS_HEIGHT</c>).</summary>
    public const int Width = 24, Height = 20;

    /// <summary>The shades, 1 the paper to 4 the ink.</summary>
    public const int Shades = 4;

    /// <summary>Where the picture's top left dot is on the screen, in blocks: a dot to a block, in the middle.</summary>
    public const int Left = (Columns - Width) / 2, Top = (Rows - Height) / 2;

    private static readonly IReadOnlyList<PoketchButton> buttons = MakeButtons();

    private readonly int[,] dots = new int[Height, Width];

    public override PoketchApp App => PoketchApp.DotArt;

    /// <summary>The shade of a dot, 1 to 4.</summary>
    public int Dot(int x, int y) => dots[y, x];

    /// <summary>A dot's button number: its place, row by row.</summary>
    public static int IdOf(int x, int y) => y * Width + x;

    // Every dot a button, the one in the middle first so the cursor starts there
    private static IReadOnlyList<PoketchButton> MakeButtons()
    {
        var all = new List<PoketchButton>();
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                all.Add(new PoketchButton(IdOf(x, y), Left + x, Top + y, 1, 1));
        int middle = IdOf(Width / 2, Height / 2);
        return all.OrderBy(b => b.Id == middle ? 0 : 1).ToList();
    }

    public override IReadOnlyList<PoketchButton> Buttons(PoketchContext context) => buttons;

    // SetupDotArtGrid: the saved picture, or the app's own until the player has touched it
    protected override void Opened()
    {
        var kept = Owner.Recall(PoketchApp.DotArt);
        if (kept is { Count: Width * Height })
        {
            for (int i = 0; i < Width * Height; i++) dots[i / Width, i % Width] = Math.Clamp(kept[i] + 1, 1, Shades);
            return;
        }
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                dots[y, x] = DefaultDot(x, y);
    }

    public override void Press(int button, PoketchContext context)
    {
        if (button < 0 || button >= Width * Height) return;
        int x = button % Width, y = button / Width;
        if (++dots[y, x] > Shades) dots[y, x] = 1;
        // SaveDotArtGrid: each dot as its shade less one, row by row (the original packs four to a byte)
        var kept = new List<int>(Width * Height);
        for (int j = 0; j < Height; j++)
            for (int i = 0; i < Width; i++)
                kept.Add(dots[j, i] - 1);
        Owner.Keep(PoketchApp.DotArt, kept);
    }

    /// <summary>
    /// The picture the app shows until the player first touches it: a Poké Ball of our own, its top half in the
    /// middle shade, its bottom the paper, a band of ink across it and a button in the middle.
    /// </summary>
    public static int DefaultDot(int x, int y)
    {
        // Measured in dots from the picture's middle, which lies between four dots
        float dx = x + 0.5f - Width / 2f, dy = y + 0.5f - Height / 2f;
        float r = MathF.Sqrt(dx * dx + dy * dy);
        if (r > 8.6f) return 1;
        if (r > 7.4f) return 4;
        if (r < 1.6f) return 2;
        if (r < 3.0f) return 4;
        if (MathF.Abs(dy) < 1f) return 4;
        return dy < 0 ? 3 : 1;
    }
}
