using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.PoketchApps;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The Pokétch over the field (style guide, "The Pokétch"): a watch's body and an LCD of our own, its apps drawn in
/// blocks of 8 on the screen's 45 by 37 (<see cref="PoketchAppState.Columns"/>). Each app's drawing is a method of
/// its own, in this file or in <c>ModernUi.Poketch.Toys.cs</c>, <c>.Pokemon.cs</c>, <c>.Map.cs</c> and
/// <c>.Time.cs</c>; the cursor that touches its buttons is drawn here, over them.
/// </summary>
internal static partial class ModernUi
{
    // The LCD's three tones (paper, ink and the middle one between) in each of the Color Changer's eight colours:
    // green (the Pokétch's own), yellow, orange, red, purple, blue, teal and white
    private static readonly Color[][] LcdColors =
    {
        new Color[] { new(176, 204, 160, 255), new(40, 62, 48, 255), new(112, 146, 104, 255) },
        new Color[] { new(214, 206, 138, 255), new(70, 60, 24, 255), new(156, 146, 82, 255) },
        new Color[] { new(226, 180, 128, 255), new(82, 44, 20, 255), new(170, 118, 72, 255) },
        new Color[] { new(222, 152, 146, 255), new(84, 30, 32, 255), new(166, 92, 90, 255) },
        new Color[] { new(192, 166, 212, 255), new(56, 36, 78, 255), new(134, 108, 158, 255) },
        new Color[] { new(156, 186, 220, 255), new(28, 46, 84, 255), new(96, 124, 168, 255) },
        new Color[] { new(146, 204, 196, 255), new(24, 66, 64, 255), new(88, 146, 140, 255) },
        new Color[] { new(216, 218, 214, 255), new(48, 50, 52, 255), new(140, 142, 140, 255) },
    };

    // The tones the screen is drawn in now, set from the Pokétch's colour as each frame begins
    private static Color LcdPaper = LcdColors[0][0], LcdInk = LcdColors[0][1], LcdMid = LcdColors[0][2];
    private const int Block = 8;

    /// <summary>The paper, ink and middle tone of one of the eight colours (the Color Changer draws its swatches with them).</summary>
    internal static Color[] LcdTones(int color) => LcdColors[Math.Clamp(color, 0, LcdColors.Length - 1)];

    /// <param name="cursor">The button the cursor is on while the Pokétch is in the player's hand; null otherwise.</param>
    /// <param name="shown">0 put away to 1 out: the watch slides up from below the screen's edge.</param>
    public static void DrawPoketch(int sw, int sh, Poketch poketch, PoketchContext context, PoketchButton? cursor, float shown)
    {
        const float width = 456, height = 368;
        var body = new Rectangle(sw - 32 - width, sh - 32 - height + (1f - shown) * (height + 48), width, height);
        UiShapes.Shadow(body, 40, 24, new Vector2(0, 8), ShadowColor);
        UiShapes.Shape(body, 40, Lighter(Frame, 0.12f), Frame, Lighter(Frame, 0.3f), 4);
        // The side button that changes the app
        UiShapes.Shape(new Rectangle(body.X + body.Width - 6, body.Y + 120, 18, 92), 9, Lighter(Frame, 0.2f), Darker(Frame, 0.1f));

        var tones = LcdTones(poketch.ScreenColor);
        LcdPaper = tones[0];
        LcdInk = tones[1];
        LcdMid = tones[2];
        var state = poketch.State;
        // The digital watch's backlight: the paper lit up, a quarter of the way to white
        if (state is DigitalWatchApp { Backlight: true }) LcdPaper = Lighter(LcdPaper, 0.25f);

        var screen = new Rectangle(body.X + 40, body.Y + 36, 360, 296);
        Raylib.DrawRectangleRec(screen, LcdPaper);
        Raylib.DrawRectangleLinesEx(screen, 4, Darker(Frame, 0.25f));

        switch (state)
        {
            case DigitalWatchApp:
                DigitalWatch(screen, context.Now);
                break;
            case PedometerApp pedometer:
                Pedometer(screen, poketch.Steps, pedometer);
                break;
            case PartyStatusApp party:
                PartyStatus(screen, context.Party, party);
                break;
            case null:
                UiFonts.DrawCentered("No apps yet.", screen.X + 40, screen.Y + screen.Height / 2f, 28, LcdInk, UiWeight.Black);
                break;
            default:
                DrawPoketchApp(screen, state, context);
                break;
        }

        if (cursor is { } c) PoketchCursor(screen, c);
    }

    /// <summary>
    /// The apps drawn in the other files, by their state: each group's file has a method for each of its apps.
    /// </summary>
    private static void DrawPoketchApp(Rectangle screen, PoketchAppState state, PoketchContext context)
    {
        switch (state)
        {
            case CalculatorApp app: PoketchCalculator(screen, app, context); break;
            case MemoPadApp app: PoketchMemoPad(screen, app, context); break;
            case CounterApp app: PoketchCounter(screen, app, context); break;
            case CoinTossApp app: PoketchCoinToss(screen, app, context); break;
            case RouletteApp app: PoketchRoulette(screen, app, context); break;
            case DotArtApp app: PoketchDotArt(screen, app, context); break;
            case ColorChangerApp app: PoketchColorChanger(screen, app, context); break;
            case KitchenTimerApp app: PoketchKitchenTimer(screen, app, context); break;
            case FriendshipCheckerApp app: PoketchFriendshipChecker(screen, app, context); break;
            case DayCareCheckerApp app: PoketchDayCareChecker(screen, app, context); break;
            case PokemonHistoryApp app: PoketchPokemonHistory(screen, app, context); break;
            case MoveTesterApp app: PoketchMoveTester(screen, app, context); break;
            case MatchupCheckerApp app: PoketchMatchupChecker(screen, app, context); break;
            case DowsingMachineApp app: PoketchDowsingMachine(screen, app, context); break;
            case BerrySearcherApp app: PoketchBerrySearcher(screen, app, context); break;
            case MarkingMapApp app: PoketchMarkingMap(screen, app, context); break;
            case TrainerCounterApp app: PoketchTrainerCounter(screen, app, context); break;
            case AnalogWatchApp app: PoketchAnalogWatch(screen, app, context); break;
            case CalendarApp app: PoketchCalendar(screen, app, context); break;
            case LinkSearcherApp app: PoketchLinkSearcher(screen, app, context); break;
        }
    }

    /// <summary>
    /// The cursor that stands for the stylus: four corners of ink a block thick round the button it is on, a
    /// block outside it (inside the screen's edge), so whatever is drawn on the button still shows.
    /// </summary>
    private static void PoketchCursor(Rectangle screen, PoketchButton b)
    {
        int x0 = Math.Max(0, b.X - 1), y0 = Math.Max(0, b.Y - 1);
        int x1 = Math.Min(PoketchAppState.Columns - 1, b.X + b.W), y1 = Math.Min(PoketchAppState.Rows - 1, b.Y + b.H);
        float X(int c) => screen.X + c * Block;
        float Y(int r) => screen.Y + r * Block;
        int arm = Math.Max(1, Math.Min(3, Math.Min(x1 - x0, y1 - y0) / 3));
        LcdBlock(X(x0), Y(y0), arm, 1, LcdInk);
        LcdBlock(X(x0), Y(y0), 1, arm, LcdInk);
        LcdBlock(X(x1 - arm + 1), Y(y0), arm, 1, LcdInk);
        LcdBlock(X(x1), Y(y0), 1, arm, LcdInk);
        LcdBlock(X(x0), Y(y1), arm, 1, LcdInk);
        LcdBlock(X(x0), Y(y1 - arm + 1), 1, arm, LcdInk);
        LcdBlock(X(x1 - arm + 1), Y(y1), arm, 1, LcdInk);
        LcdBlock(X(x1), Y(y1 - arm + 1), 1, arm, LcdInk);
    }

    /// <summary>A rectangle of blocks at a place on the screen given in blocks.</summary>
    // A menu icon in the LCD's tones, by each pixel's brightness (the original's
    // PoketchTask_LoadPokemonIconLuminancePalette): dark to the ink, the middle to the middle tone, light to the
    // middle tone faintly, clear left clear; made once for each icon and set of tones
    private static readonly System.Collections.Generic.Dictionary<(string, uint, uint), Texture2D> lcdIcons = new();

    private static Texture2D LcdIconTexture(string model)
    {
        var icon = PixelArtGenerator.GetPokemonIcon(model);
        uint tones = (uint)(LcdInk.R << 24 | LcdInk.G << 16 | LcdMid.R << 8 | LcdMid.B);
        if (lcdIcons.TryGetValue((model, icon.Id, tones), out var known)) return known;
        var image = Raylib.LoadImageFromTexture(icon);
        Raylib.ImageFormat(ref image, PixelFormat.UncompressedR8G8B8A8);
        var faint = new Color(LcdMid.R, LcdMid.G, LcdMid.B, (byte)90);
        for (int y = 0; y < image.Height; y++)
            for (int x = 0; x < image.Width; x++)
            {
                var c = Raylib.GetImageColor(image, x, y);
                float light = (0.3f * c.R + 0.59f * c.G + 0.11f * c.B) / 255f;
                var tone = c.A < 128 ? new Color(0, 0, 0, 0) : light < 0.3f ? LcdInk : light < 0.62f ? LcdMid : faint;
                Raylib.ImageDrawPixel(ref image, x, y, tone);
            }
        var texture = Raylib.LoadTextureFromImage(image);
        Raylib.UnloadImage(image);
        lcdIcons[(model, icon.Id, tones)] = texture;
        return texture;
    }

    private static void LcdCells(Rectangle screen, int col, int row, int w, int h, Color c) =>
        LcdBlock(screen.X + col * Block, screen.Y + row * Block, w, h, c);

    /// <summary>A button's frame drawn in blocks: an ink outline a block thick, the middle tone inside while pressed.</summary>
    private static void LcdButton(Rectangle screen, PoketchButton b, bool pressed = false)
    {
        LcdCells(screen, b.X, b.Y, b.W, 1, LcdInk);
        LcdCells(screen, b.X, b.Y + b.H - 1, b.W, 1, LcdInk);
        LcdCells(screen, b.X, b.Y, 1, b.H, LcdInk);
        LcdCells(screen, b.X + b.W - 1, b.Y, 1, b.H, LcdInk);
        if (pressed && b.W > 2 && b.H > 2) LcdCells(screen, b.X + 1, b.Y + 1, b.W - 2, b.H - 2, LcdMid);
    }

    /// <summary>Text on the LCD, centred on a column of blocks, in the ink.</summary>
    private static void LcdText(Rectangle screen, string text, float centreCol, int row, int size = 28, Color? color = null)
    {
        float w = UiFonts.Measure(text, size, UiWeight.Black);
        UiFonts.DrawCentered(text, screen.X + centreCol * Block - w / 2f, screen.Y + row * Block + size / 2f, size, color ?? LcdInk, UiWeight.Black);
    }

    private static void LcdBlock(float x, float y, int w, int h, Color c) => Raylib.DrawRectangleRec(new Rectangle(x, y, w * Block, h * Block), c);

    /// <summary>
    /// A seven-segment figure, 7 blocks by 13: the segments lit in the ink, the others faintly in the middle green,
    /// as an LCD's unlit segments show.
    /// </summary>
    private static void SegmentDigit(float x, float y, int digit)
    {
        // Segments a to g: top, upper right, lower right, bottom, lower left, upper left, middle
        int[] lit = { 0b0111111, 0b0000110, 0b1011011, 0b1001111, 0b1100110, 0b1101101, 0b1111101, 0b0000111, 0b1111111, 0b1101111 };
        int mask = lit[Math.Clamp(digit, 0, 9)];
        Color On(int bit) => (mask & (1 << bit)) != 0 ? LcdInk : LcdMid with { A = 70 };
        LcdBlock(x + Block, y, 5, 1, On(0));
        LcdBlock(x + 6 * Block, y + Block, 1, 5, On(1));
        LcdBlock(x + 6 * Block, y + 7 * Block, 1, 5, On(2));
        LcdBlock(x + Block, y + 12 * Block, 5, 1, On(3));
        LcdBlock(x, y + 7 * Block, 1, 5, On(4));
        LcdBlock(x, y + Block, 1, 5, On(5));
        LcdBlock(x + Block, y + 6 * Block, 5, 1, On(6));
    }

    private static readonly string[] Days = { "SUNDAY", "MONDAY", "TUESDAY", "WEDNESDAY", "THURSDAY", "FRIDAY", "SATURDAY" };

    /// <summary>The digital watch: hours and minutes, a colon that blinks each second, the day under them.</summary>
    private static void DigitalWatch(Rectangle screen, DateTime now)
    {
        const float digit = 7 * Block, gap = Block;
        float total = 4 * digit + 2 * gap + 3 * Block;
        float x = screen.X + MathF.Round((screen.Width - total) / 2f / Block) * Block, y = screen.Y + 72;
        SegmentDigit(x, y, now.Hour / 10);
        SegmentDigit(x + digit + gap, y, now.Hour % 10);
        float colon = x + 2 * digit + gap + Block;
        if (FrameClock.Now % 1.0 < 0.5)
        {
            LcdBlock(colon, y + 3 * Block, 1, 1, LcdInk);
            LcdBlock(colon, y + 9 * Block, 1, 1, LcdInk);
        }
        float minutes = colon + 2 * Block;
        SegmentDigit(minutes, y, now.Minute / 10);
        SegmentDigit(minutes + digit + gap, y, now.Minute % 10);

        string day = Days[(int)now.DayOfWeek];
        float w = UiFonts.Measure(day, 28, UiWeight.Black);
        UiFonts.DrawCentered(day, screen.X + (screen.Width - w) / 2f, screen.Y + 240, 28, LcdInk, UiWeight.Black);
    }

    /// <summary>The pedometer: the steps taken, in five figures, and the button that resets them.</summary>
    private static void Pedometer(Rectangle screen, int steps, PedometerApp app)
    {
        const float digit = 7 * Block, gap = Block;
        float total = 5 * digit + 4 * gap;
        float x = screen.X + MathF.Round((screen.Width - total) / 2f / Block) * Block, y = screen.Y + 64;
        string figures = Math.Clamp(steps, 0, Poketch.MostSteps).ToString("D5");
        for (int i = 0; i < figures.Length; i++) SegmentDigit(x + i * (digit + gap), y, figures[i] - '0');
        float w = UiFonts.Measure("STEPS", 28, UiWeight.Black);
        UiFonts.DrawCentered("STEPS", screen.X + (screen.Width - w) / 2f, screen.Y + 196, 28, LcdInk, UiWeight.Black);
        LcdButton(screen, PedometerApp.Reset, app.PressedFor > 0f);
        LcdText(screen, "RESET", PedometerApp.Reset.CentreX, PedometerApp.Reset.Y + 1, 24);
    }

    /// <summary>The team: two rows of three, each icon over a bar of its HP; a fainted one dark, a status marked.</summary>
    private static void PartyStatus(Rectangle screen, Party party, PartyStatusApp app)
    {
        for (int i = 0; i < Party.MaxSize; i++)
        {
            var slot = PartyStatusApp.Slot(i);
            float cx = screen.X + slot.X * Block, cy = screen.Y + slot.Y * Block;
            // A Pokémon touched hops, two blocks up and down, in whole blocks
            cy -= Block * MathF.Round(2f * MathF.Sin(MathF.PI * app.Hop(i)));
            if (i >= party.Count)
            {
                LcdBlock(cx + 32, cy + 40, 4, 1, LcdMid);
                continue;
            }
            var p = party.Members[i];
            var icon = LcdIconTexture(p.ModelName);
            // A fainted Pokémon is drawn darker
            var tint = p.IsFainted ? new Color(120, 120, 120, 255) : Color.White;
            Raylib.DrawTexturePro(icon, new Rectangle(0, 0, icon.Width, icon.Height), new Rectangle(cx + 8, cy, 80, 80), Vector2.Zero, 0f, tint);
            if (p.Status != StatusCondition.None && !p.IsFainted) LcdBlock(cx + 80, cy + 4, 1, 1, LcdInk);

            // The bar: an ink frame of blocks, filled in the middle green as far as the HP goes
            float bx = cx + 8, by = cy + 96;
            LcdBlock(bx, by, 10, 1, LcdInk);
            LcdBlock(bx, by + 2 * Block, 10, 1, LcdInk);
            LcdBlock(bx, by + Block, 1, 1, LcdInk);
            LcdBlock(bx + 9 * Block, by + Block, 1, 1, LcdInk);
            int filled = p.MaxHP <= 0 ? 0 : (int)MathF.Ceiling(8f * p.CurrentHP / p.MaxHP);
            if (filled > 0) LcdBlock(bx + Block, by + Block, filled, 1, LcdMid);
        }
    }
}
