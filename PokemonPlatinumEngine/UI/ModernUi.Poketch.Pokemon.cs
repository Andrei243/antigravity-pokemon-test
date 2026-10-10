using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.PoketchApps;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The Pokétch's apps about Pokémon (plan 06 · R14b; style guide, "The Pokétch"): the Friendship Checker, the Day-Care
/// Checker, the Pokémon History, the Move Tester and the Matchup Checker. Everything is in the LCD's three tones and
/// its blocks of 8; the Pokémon are their menu icons in the screen's tones (<c>LcdIconTexture</c>), as the team's app draws them, and every
/// other picture (hearts, the egg, the gender signs, the fish, the arrows, the marks) is a pattern of blocks of our own.
/// </summary>
internal static partial class ModernUi
{
    /// <summary>A Pokémon's menu icon on the LCD, its top left on the block grid; turned to face right when asked.</summary>
    private static void LcdIcon(Rectangle screen, string model, float x, float y, float size, bool faceRight = false)
    {
        var icon = LcdIconTexture(model);
        float sx = screen.X + MathF.Round(x / Block) * Block, sy = screen.Y + MathF.Round(y / Block) * Block;
        var source = new Rectangle(0, 0, faceRight ? -icon.Width : icon.Width, icon.Height);
        Raylib.DrawTexturePro(icon, source, new Rectangle(sx, sy, size, size), Vector2.Zero, 0f, Color.White);
    }

    /// <summary>A picture of blocks: an X is a block in the colour, anything else is left alone; mirrored when asked.</summary>
    private static void LcdPattern(Rectangle screen, int col, int row, string[] rows, Color color, bool mirror = false)
    {
        for (int r = 0; r < rows.Length; r++)
        {
            string line = rows[r];
            for (int c = 0; c < line.Length; c++)
                if (line[mirror ? line.Length - 1 - c : c] == 'X') LcdCells(screen, col + c, row + r, 1, 1, color);
        }
    }

    private static readonly string[] SmallHeart = { "X.X", "XXX", ".X." };
    private static readonly string[] BigHeart = { ".X.X.", "XXXXX", "XXXXX", ".XXX.", "..X.." };

    // A heart-shaped fish facing right: a forked tail, a heart on its side for a body, its point the snout
    private static readonly string[] HeartFish = { "X..XX..", ".XXXXX.", "..XXXXX", ".XXXXX.", "X..XX.." };
    private static readonly string[] HeartFishEye = { "", "....X" };

    private static readonly string[] MaleSign = { "...XXX", "....XX", ".XX.X.", "X..X..", "X..X..", ".XX..." };
    private static readonly string[] FemaleSign = { ".XX.", "X..X", "X..X", ".XX.", ".XX.", "XXXX", ".XX." };

    private static readonly string[] EggOutline = { "..XX..", ".X..X.", "X....X", "X....X", "X....X", ".X..X.", "..XX.." };
    private static readonly string[] EggFill = { "", "..XX..", ".XXXX.", ".XXXX.", ".XXXX.", "..XX.." };

    private static readonly string[] ArrowLeft = { "..X", ".XX", "XXX", ".XX", "..X" };
    private static readonly string[] Exclamation = { "X", "X", "X", "X", "", "X" };

    /// <summary>
    /// A small seven-segment figure, 4 blocks by 7, its segments two blocks long: lit in the ink, the others faint as
    /// the big figures' are.
    /// </summary>
    private static void LcdSmallDigit(Rectangle screen, int col, int row, int digit)
    {
        int[] lit = { 0b0111111, 0b0000110, 0b1011011, 0b1001111, 0b1100110, 0b1101101, 0b1111101, 0b0000111, 0b1111111, 0b1101111 };
        int mask = lit[Math.Clamp(digit, 0, 9)];
        Color On(int bit) => (mask & (1 << bit)) != 0 ? LcdInk : LcdMid with { A = 70 };
        LcdCells(screen, col + 1, row, 2, 1, On(0));
        LcdCells(screen, col + 3, row + 1, 1, 2, On(1));
        LcdCells(screen, col + 3, row + 4, 1, 2, On(2));
        LcdCells(screen, col + 1, row + 6, 2, 1, On(3));
        LcdCells(screen, col, row + 4, 1, 2, On(4));
        LcdCells(screen, col, row + 1, 1, 2, On(5));
        LcdCells(screen, col + 1, row + 3, 2, 1, On(6));
    }

    /// <summary>
    /// The Friendship Checker: the team wandering, each icon facing the way it goes, with one to three hearts over a
    /// Pokémon that shows it likes the player, a shadow under one in the air, and a dot of the middle tone on each
    /// spot of ground that can be touched.
    /// </summary>
    private static void PoketchFriendshipChecker(Rectangle screen, FriendshipCheckerApp app, PoketchContext context)
    {
        foreach (var b in app.Buttons(context))
            if (b.Id >= FriendshipCheckerApp.GroundId) LcdCells(screen, b.X + 1, b.Y + 1, 1, 1, LcdMid);

        if (app.Team.Count == 0)
        {
            LcdText(screen, "NO POKéMON", PoketchAppState.Columns / 2f, 16, 24);
            return;
        }

        const float size = FriendshipCheckerApp.IconSize;
        foreach (var w in app.Team)
        {
            float left = w.Position.X - size / 2f, top = w.Position.Y - size / 2f;
            int col = (int)MathF.Round(left / Block), row = (int)MathF.Round(top / Block);
            // The shadow: a flat oval of the middle tone where its feet were
            if (w.Shadow) LcdPattern(screen, col + 2, row + 6, new[] { ".XX.", "XXXX" }, LcdMid);
            int lift = (int)MathF.Round(w.Lift / Block);
            LcdIcon(screen, w.Pokemon.ModelName, col * Block, (row - lift) * Block, size, w.FacingRight);

            int hearts = w.Hearts;
            if (hearts > 0)
            {
                int width = hearts * 3 + (hearts - 1);
                int hx = col + 4 - width / 2;
                for (int h = 0; h < hearts; h++) LcdPattern(screen, hx + h * 4, row - lift - 3, SmallHeart, LcdInk);
            }
        }
    }

    /// <summary>
    /// The Day-Care Checker: each Pokémon's level in small figures with its gender's sign after them across the top,
    /// the two facing one another over the yard's fence, and an egg between them when there is one. Read again, the
    /// picture dissolves back in from paper.
    /// </summary>
    private static void PoketchDayCareChecker(Rectangle screen, DayCareCheckerApp app, PoketchContext context)
    {
        app.EnsureRead(context);

        // The yard's fence: rails along the bottom, a post every four blocks
        LcdCells(screen, 0, 31, PoketchAppState.Columns, 1, LcdMid);
        LcdCells(screen, 0, 34, PoketchAppState.Columns, 1, LcdMid);
        for (int c = 1; c < PoketchAppState.Columns; c += 4) LcdCells(screen, c, 30, 1, 6, LcdMid);

        for (int i = 0; i < app.Shown.Count; i++)
        {
            var p = app.Shown[i];
            int baseCol = i == 0 ? 1 : 23;
            string figures = DayCareCheckerApp.Figures(i < app.Levels.Count ? app.Levels[i] : DayCareCheckerApp.LevelOf(p));
            // Right-aligned in three places, as the original hides the hundreds and tens it doesn't need
            int start = baseCol + (3 - figures.Length) * 5;
            for (int d = 0; d < figures.Length; d++) LcdSmallDigit(screen, start + d * 5, 3, figures[d] - '0');
            if (p.Gender == Gender.Male) LcdPattern(screen, baseCol + 15, 4, MaleSign, LcdInk);
            else if (p.Gender == Gender.Female) LcdPattern(screen, baseCol + 16, 3, FemaleSign, LcdInk);

            // The left one faces right, toward the egg, and the right one left
            float x = i == 0 ? 40 : 248;
            LcdIcon(screen, p.ModelName, x, 168, 80, faceRight: i == 0);
        }
        if (app.HasEgg)
        {
            LcdPattern(screen, 20, 21, EggFill, LcdMid);
            LcdPattern(screen, 20, 21, EggOutline, LcdInk);
        }

        // The mosaic as the picture comes back: cells of two blocks covered in paper, fewer each step
        if (app.Mosaic > 0)
        {
            float covered = app.Mosaic / (float)DayCareCheckerApp.MosaicStart;
            for (int r = 0; r < PoketchAppState.Rows; r += 2)
                for (int c = 0; c < PoketchAppState.Columns; c += 2)
                {
                    uint h = (uint)(c * 73856093) ^ (uint)(r * 19349663);
                    h = (h ^ (h >> 13)) * 0x5bd1e995u;
                    if ((h >> 8) % 1000 < covered * 1000) LcdCells(screen, c, r, 2, 2, LcdPaper);
                }
        }
    }

    /// <summary>The Pokémon History: twelve places, four to a row, the oldest at the top left; an empty one a short dash.</summary>
    private static void PoketchPokemonHistory(Rectangle screen, PokemonHistoryApp app, PoketchContext context)
    {
        var history = context.Poketch.History;
        for (int i = 0; i < Poketch.HistoryLength; i++)
        {
            var cell = PokemonHistoryApp.Cell(i);
            if (i >= history.Count)
            {
                LcdCells(screen, cell.X + 3, cell.Y + 5, 3, 1, LcdMid);
                continue;
            }
            LcdIcon(screen, history[i], cell.X * Block + 4, (cell.Y + 1) * Block, 64);
        }
    }

    private static readonly string[] EffectWords =
    {
        "IT HAS NO EFFECT", "IT BARELY WORKS", "NOT VERY EFFECTIVE", "IT HITS NORMALLY", "SUPER EFFECTIVE", "EXTREMELY EFFECTIVE"
    };

    /// <summary>
    /// The Move Tester: the target's two types at the top right between their arrows, the move's type at the bottom
    /// left, five marks at the top left lit as far as the move works, and the words for it along the bottom.
    /// </summary>
    private static void PoketchMoveTester(Rectangle screen, MoveTesterApp app, PoketchContext context)
    {
        foreach (var b in app.Buttons(context))
        {
            bool pressed = app.Pressed == b.Id;
            LcdButton(screen, b, pressed);
            bool down = b.Id % 2 == 0;
            LcdPattern(screen, b.X + 1, b.Y + (b.H - 5) / 2, ArrowLeft, LcdInk, mirror: !down);
        }

        LcdText(screen, TypeName(app.First), 31.5f, 3, 22);
        LcdText(screen, app.Second is { } second ? TypeName(second) : "NONE", 31.5f, 11, 22);
        LcdText(screen, "MOVE", 13.5f, 18, 20, LcdMid);
        LcdText(screen, TypeName(app.Attack), 13.5f, 24, 22);

        int marks = app.Marks;
        for (int i = 0; i < MoveTesterApp.MostMarks; i++)
            LcdPattern(screen, 4 + i * 2, 3, Exclamation, i < marks ? LcdInk : LcdMid with { A = 70 });

        LcdText(screen, EffectWords[Math.Clamp(marks, 0, EffectWords.Length - 1)], PoketchAppState.Columns / 2f, 32, 22);
    }

    private static string TypeName(PokemonType type) => type.ToString().ToUpperInvariant();

    /// <summary>
    /// The Matchup Checker: the meter's three hearts at the top, the two heart-shaped fish swimming toward one another,
    /// each side's Pokémon in its button at the bottom and the check button between them.
    /// </summary>
    private static void PoketchMatchupChecker(Rectangle screen, MatchupCheckerApp app, PoketchContext context)
    {
        // The meter: hearts lit in the ink as far as the pair earns, the rest faint; the best flashes
        bool flash = app.Kissing && (app.Frames / 8) % 2 == 0;
        for (int h = 0; h < 3; h++)
        {
            bool lit = h < app.Hearts;
            var tone = lit ? (flash ? LcdMid : LcdInk) : LcdMid with { A = 70 };
            LcdPattern(screen, 14 + h * 6, 2, BigHeart, tone);
        }

        // The fish: the original's 16 pixels a step, on our screen a little under four blocks
        int step = (int)MathF.Round(app.Offset * FriendshipCheckerApp.Scale / Block);
        int leftCol = 4 + step, rightCol = PoketchAppState.Columns - 4 - 7 - step;
        bool away = app.TurnedAway;
        LcdPattern(screen, leftCol, 14, HeartFish, LcdInk, mirror: away);
        LcdPattern(screen, leftCol, 14, away ? new[] { "", "..X" } : HeartFishEye, LcdPaper);
        LcdPattern(screen, rightCol, 14, HeartFish, LcdInk, mirror: !away);
        LcdPattern(screen, rightCol, 14, away ? HeartFishEye : new[] { "", "..X" }, LcdPaper);
        if (app.Kissing) LcdPattern(screen, 21, 10, SmallHeart, LcdInk);

        LcdButton(screen, MatchupCheckerApp.LeftButton);
        LcdButton(screen, MatchupCheckerApp.RightButton);
        if (app.LeftPokemon(context) is { } left)
            LcdIcon(screen, left.ModelName, (MatchupCheckerApp.LeftButton.X + 3) * Block, (MatchupCheckerApp.LeftButton.Y + 1) * Block, 48, faceRight: true);
        if (app.RightPokemon(context) is { } right)
            LcdIcon(screen, right.ModelName, (MatchupCheckerApp.RightButton.X + 3) * Block, (MatchupCheckerApp.RightButton.Y + 1) * Block, 48);

        var check = MatchupCheckerApp.CheckButton;
        LcdButton(screen, check, app.CheckPressed(context));
        LcdPattern(screen, check.X + 2, check.Y + 2, BigHeart, LcdInk);
    }
}
