using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The Battle Arcade on screen (plan 06 · R18d; style guide, "The Battle Arcade"): the game board, its sixteen panels
/// with a sign for each event, and the two threes beside it.
/// </summary>
internal static partial class ModernUi
{
    /// <summary>The colour that says whom an event falls on: the trainer to come, the challenger, both, or neither (a bonus).</summary>
    public static Color ArcadeColour(ArcadeCategory category) => category switch
    {
        ArcadeCategory.Foe => Red,
        ArcadeCategory.Ally => Blue,
        ArcadeCategory.Field => Green,
        _ => Gold
    };

    /// <summary>
    /// The board as it stands: the key to the panels' colours over it, the sixteen panels (counting down, showing
    /// their events with the cursor on one, or all showing the event it stopped on), the challenger's three at the
    /// left and the trainer's at the right, and along the foot what is to be done, the cursor's speed, the battle of
    /// the round and the streak, under a bar that runs down with the cursor's time.
    /// </summary>
    public static void DrawArcadeBoard(int sw, int sh, BattleArcadeChallenge challenge, ArcadeBoard board, float time, float appear = 1f)
    {
        Backdrop(sw, sh);
        ScreenTitle("BATTLE ARCADE");
        Hints(sw - Margin, 44, ("Z", "Stop"));
        float rise = (1f - UiMotion.EaseOut(appear)) * 60f;

        const float panelW = 200, panelH = 154, gap = 16, top = 196;
        float boardW = panelW * ArcadeBoard.Columns + gap * (ArcadeBoard.Columns - 1), left = sw / 2f - boardW / 2f;

        // The key: whom a colour stands for
        (string, ArcadeCategory)[] key = { ("FOE", ArcadeCategory.Foe), ("YOU", ArcadeCategory.Ally), ("BOTH", ArcadeCategory.Field), ("BONUS", ArcadeCategory.Bonus) };
        float keyW = 0f;
        foreach (var (word, _) in key) keyW += UiFonts.Measure(word, 34 * 0.58f, UiWeight.Black) + 34 * 0.9f + 16;
        float kx = sw / 2f - (keyW - 16) / 2f;
        foreach (var (word, category) in key) kx += Tag(kx, 140 + rise, word, ArcadeColour(category)) + 16;

        bool counting = board.Step is ArcadeBoardStep.Three or ArcadeBoardStep.Two or ArcadeBoardStep.One;
        bool stopped = board.Step >= ArcadeBoardStep.Stopped;
        for (int i = 0; i < ArcadeBoard.Size; i++)
        {
            var r = new Rectangle(left + i % ArcadeBoard.Columns * (panelW + gap), top + rise + i / ArcadeBoard.Columns * (panelH + gap), panelW, panelH);
            bool under = board.Cursor == i && board.Step >= ArcadeBoardStep.Running;
            if (board.Step == ArcadeBoardStep.Opening) EmptySlot(r, 24);
            else if (counting) CountPanel(r, board.Step == ArcadeBoardStep.Three ? "3" : board.Step == ArcadeBoardStep.Two ? "2" : "1");
            else ArcadePanel(r, stopped ? board.Effect : board.Panels[i], under);
        }

        // The two threes
        float sideW = left - Margin - 52, rowH = panelH + gap;
        ArcadeSide(new Rectangle(Margin, top + rise, sideW, panelH), "YOU", "CHALLENGER", challenge.Team.Members, ArcadeColour(ArcadeCategory.Ally), rowH);
        if (challenge.Foe is { } foe)
            ArcadeSide(new Rectangle(sw - Margin - sideW, top + rise, sideW, panelH), $"{foe.Record.Class} {foe.Record.Name}", "NEXT TRAINER", foe.Pokemon, ArcadeColour(ArcadeCategory.Foe), rowH);

        // The foot: what is to be done, the cursor's speed, the battle of the round and the streak
        var foot = new Rectangle(Margin, 890 + rise, sw - Margin * 2, 116);
        Panel(foot, 34);
        float left30 = board.TimeLeft / (float)ArcadeBoard.TimeLimit;
        if (board.Step == ArcadeBoardStep.Running)
        {
            var track = new Rectangle(foot.X + 40, foot.Y + 10, foot.Width - 80, 8);
            UiShapes.Fill(track, 4, Rule);
            UiShapes.Fill(new Rectangle(track.X, track.Y, Math.Max(8, track.Width * left30), 8), 4, left30 < 0.2f ? Red : Selection);
        }
        string prompt = board.Step switch
        {
            ArcadeBoardStep.Opening => "The game board is starting up...",
            ArcadeBoardStep.Three or ArcadeBoardStep.Two or ArcadeBoardStep.One => "Get ready!",
            ArcadeBoardStep.Running => "Press Z to stop the cursor!",
            _ => ArcadeBoard.NameOf(board.Effect) + "!"
        };
        var said = stopped ? Darker(ArcadeColour(ArcadeBoard.CategoryOf(board.Effect)), 0.25f) : Ink;
        UiFonts.DrawCentered(prompt, foot.X + 48, foot.Y + foot.Height / 2f + 4, 38, said, UiWeight.ExtraBold);

        string streak = $"{challenge.Streak:N0}", battle = $"{challenge.Battle + 1} / {BattleArcadeChallenge.BattlesPerRound}";
        float sx = foot.X + foot.Width - 48 - Math.Max(UiFonts.Measure(streak, 44, UiWeight.Black), UiFonts.Measure("STREAK", 20, UiWeight.Black));
        Label("STREAK", sx, foot.Y + 24);
        UiFonts.Draw(streak, sx, foot.Y + 50, 44, Ink, UiWeight.Black);
        float bx = sx - 56 - Math.Max(UiFonts.Measure(battle, 44, UiWeight.Black), UiFonts.Measure("BATTLE", 20, UiWeight.Black));
        Label("BATTLE", bx, foot.Y + 24);
        UiFonts.Draw(battle, bx, foot.Y + 50, 44, Ink, UiWeight.Black);
        // Eight pips for the cursor's eight speeds
        const float pip = 16, between = 8;
        float cx = bx - 56 - (pip + between) * (ArcadeBoard.FastestSpeed + 1) + between;
        Label("CURSOR", cx, foot.Y + 24);
        for (int n = 0; n <= ArcadeBoard.FastestSpeed; n++)
        {
            var c = new Vector2(cx + n * (pip + between) + pip / 2f, foot.Y + 74);
            if (n <= board.Speed) UiShapes.Circle(c, pip / 2f, Selection);
            else UiShapes.Ring(c, pip / 2f, 3, Rule);
        }
        if (board.Randomized) Tag(cx + UiFonts.Measure("CURSOR", 20, UiWeight.Black) + 14, foot.Y + 22, "RANDOM", Gold, 26);
    }

    // A panel while the board counts down: the digit and nothing else
    private static void CountPanel(Rectangle r, string digit)
    {
        Card(r, 24, false);
        UiFonts.DrawCentered(digit, r.X + (r.Width - UiFonts.Measure(digit, 96, UiWeight.Black)) / 2f, r.Y + r.Height / 2f + 4, 96, Rule, UiWeight.Black);
    }

    // A panel with its event: a band in the colour of whom it falls on with the event's sign, and its name under it
    private static void ArcadePanel(Rectangle r, ArcadeEffect effect, bool under)
    {
        var colour = ArcadeColour(ArcadeBoard.CategoryOf(effect));
        if (under) UiShapes.Shadow(new Rectangle(r.X - 6, r.Y - 6, r.Width + 12, r.Height + 12), 30, 30, Vector2.Zero, Selection with { A = 230 });
        Card(r, 24, false);
        var band = new Rectangle(r.X + 10, r.Y + 10, r.Width - 20, 96);
        UiShapes.Shape(band, 16, Lighter(colour, 0.14f), Darker(colour, 0.12f));
        ArcadeSign(effect, new Vector2(band.X + band.Width / 2f, band.Y + band.Height / 2f), 34, Color.White, colour);
        string name = ArcadeBoard.NameOf(effect).ToUpperInvariant();
        UiFonts.DrawCentered(name, r.X + (r.Width - UiFonts.Measure(name, 24, UiWeight.Black)) / 2f, r.Y + 130, 24, Ink, UiWeight.Black);
        if (under)
        {
            var clear = Selection with { A = 0 };
            UiShapes.Shape(new Rectangle(r.X - 4, r.Y - 4, r.Width + 8, r.Height + 8), 28, clear, clear, Selection, 8);
        }
    }

    // One side's three: its heading where the board's first row is, and a card for each Pokémon beside the other three rows
    private static void ArcadeSide(Rectangle head, string name, string caption, IReadOnlyList<Pokemon> three, Color colour, float rowH)
    {
        UiFonts.Draw(caption, head.X + 8, head.Y + 50, 20, new Color(255, 255, 255, 190), UiWeight.Black);
        float size = 34;
        while (size > 22 && UiFonts.Measure(name, size, UiWeight.Black) > head.Width - 16) size -= 2;
        UiFonts.Draw(name, head.X + 8, head.Y + 78, size, Color.White, UiWeight.Black);
        UiShapes.Fill(new Rectangle(head.X + 8, head.Y + 128, head.Width - 16, 6), 3, colour);
        for (int i = 0; i < three.Count && i < BattleArcadeChallenge.TeamSize; i++)
        {
            var p = three[i];
            var r = new Rectangle(head.X, head.Y + (i + 1) * rowH, head.Width, head.Height);
            Card(r, 24, false);
            var c = new Vector2(r.X + 78, r.Y + r.Height / 2f);
            UiShapes.Circle(c, 56, new Color(226, 234, 246, 255));
            var sprite = PixelArtGenerator.SpriteOf(p);
            float scale = Math.Min(1f, 108f / Math.Max(sprite.Width, sprite.Height));
            Raylib.DrawTexturePro(sprite, new Rectangle(0, 0, sprite.Width, sprite.Height),
                new Rectangle(MathF.Round(c.X - sprite.Width * scale / 2f), MathF.Round(c.Y - sprite.Height * scale / 2f), MathF.Round(sprite.Width * scale), MathF.Round(sprite.Height * scale)),
                Vector2.Zero, 0f, Color.White);
            float tx = r.X + 152;
            UiFonts.Draw(p.Species.Name, tx, r.Y + 24, 28, Ink, UiWeight.ExtraBold);
            UiFonts.Draw($"Lv. {p.Level}", tx, r.Y + 60, 22, Muted, UiWeight.Black);
            if (p.Status != StatusCondition.None) Tag(tx + UiFonts.Measure($"Lv. {p.Level}", 22, UiWeight.Black) + 14, r.Y + 58, StatusWord(p.Status), Selection, 28);
            if (p.HeldItem != null) Tag(tx, r.Y + 98, p.HeldItem.Name.ToUpperInvariant(), colour, 32);
            else UiFonts.Draw("NOTHING HELD", tx, r.Y + 102, 20, Muted, UiWeight.Black);
        }
    }

    private static string StatusWord(StatusCondition status) => status switch
    {
        StatusCondition.Poison or StatusCondition.Toxic => "PSN",
        StatusCondition.Burn => "BRN",
        StatusCondition.Paralyze => "PAR",
        StatusCondition.Sleep => "SLP",
        StatusCondition.Freeze => "FRZ",
        _ => ""
    };

    /// <summary>
    /// An event's sign (our own, drawn from the kit's shapes): <paramref name="s"/> is half its height,
    /// <paramref name="back"/> the colour it stands on, for what is cut out of it.
    /// </summary>
    public static void ArcadeSign(ArcadeEffect effect, Vector2 c, float s, Color ink, Color back)
    {
        Vector2 P(float x, float y) => new(c.X + x * s, c.Y + y * s);
        float line = s * 0.2f;
        void Words(string text, float size, float dx = 0f)
        {
            float w = UiFonts.Measure(text, size, UiWeight.Black);
            UiFonts.DrawCentered(text, c.X + dx * s - w / 2f, c.Y, size, ink, UiWeight.Black);
        }
        void Cloud()
        {
            UiShapes.Circle(P(-0.42f, -0.28f), s * 0.34f, ink);
            UiShapes.Circle(P(0.04f, -0.46f), s * 0.44f, ink);
            UiShapes.Circle(P(0.52f, -0.24f), s * 0.3f, ink);
            UiShapes.Fill(new Rectangle(c.X - s * 0.76f, c.Y - s * 0.36f, s * 1.58f, s * 0.42f), s * 0.2f, ink);
        }
        switch (effect)
        {
            case ArcadeEffect.LowerFoeHp or ArcadeEffect.LowerAllyHp:
            {
                // A bar with its last part gone, and an arrow down
                var bar = new Rectangle(c.X - s * 1.15f, c.Y - s * 0.3f, s * 1.5f, s * 0.6f);
                var clear = ink with { A = 0 };
                UiShapes.Shape(bar, s * 0.3f, clear, clear, ink, 5);
                UiShapes.Fill(new Rectangle(bar.X + 9, bar.Y + 9, bar.Width * 0.55f, bar.Height - 18), s * 0.12f, ink);
                UiIcons.ArrowDown(P(0.86f, 0.02f), s * 0.62f, ink);
                break;
            }
            case ArcadeEffect.PoisonFoe or ArcadeEffect.PoisonAlly:
                // A drop, and two bubbles
                UiShapes.Circle(P(-0.1f, 0.28f), s * 0.46f, ink);
                UiShapes.Triangle(P(-0.5f, 0.1f), P(0.3f, 0.1f), P(-0.1f, -0.8f), ink, 2);
                UiShapes.Circle(P(0.62f, -0.4f), s * 0.15f, ink);
                UiShapes.Circle(P(0.78f, 0.1f), s * 0.1f, ink);
                break;
            case ArcadeEffect.ParalyzeFoe or ArcadeEffect.ParalyzeAlly:
                // A bolt
                UiShapes.Triangle(P(0.3f, -0.9f), P(-0.5f, 0.12f), P(0.12f, 0.12f), ink, 2);
                UiShapes.Triangle(P(-0.3f, 0.9f), P(0.5f, -0.12f), P(-0.12f, -0.12f), ink, 2);
                break;
            case ArcadeEffect.BurnFoe or ArcadeEffect.BurnAlly:
                // A flame with a hollow heart
                UiShapes.Circle(P(0f, 0.3f), s * 0.52f, ink);
                UiShapes.Triangle(P(-0.5f, 0.2f), P(0.5f, 0.2f), P(0.12f, -0.9f), ink, 2);
                UiShapes.Circle(P(0f, 0.42f), s * 0.22f, back);
                break;
            case ArcadeEffect.SleepFoe or ArcadeEffect.SleepAlly:
                Words("Zzz", s * 1.15f);
                break;
            case ArcadeEffect.FreezeFoe or ArcadeEffect.FreezeAlly:
                // A flake: three bars through one point
                UiShapes.Line(P(-0.8f, 0f), P(0.8f, 0f), line, ink);
                UiShapes.Line(P(-0.4f, -0.7f), P(0.4f, 0.7f), line, ink);
                UiShapes.Line(P(0.4f, -0.7f), P(-0.4f, 0.7f), line, ink);
                UiShapes.Circle(c, s * 0.2f, ink);
                break;
            case ArcadeEffect.FoeGetBerry or ArcadeEffect.AllyGetBerry:
                // A berry with its leaf
                UiShapes.Circle(P(0f, 0.22f), s * 0.56f, ink);
                UiShapes.Turned(P(0.3f, -0.58f), s * 0.34f, s * 0.16f, s * 0.16f, -32f, ink);
                UiShapes.Line(P(0f, -0.3f), P(0.04f, -0.62f), line * 0.7f, ink);
                UiShapes.Circle(P(-0.2f, 0.06f), s * 0.12f, back);
                break;
            case ArcadeEffect.FoeGetItem or ArcadeEffect.AllyGetItem:
                // A box with its lid and its ribbon
                UiShapes.Fill(new Rectangle(c.X - s * 0.62f, c.Y - s * 0.2f, s * 1.24f, s * 0.92f), s * 0.12f, ink);
                UiShapes.Fill(new Rectangle(c.X - s * 0.74f, c.Y - s * 0.66f, s * 1.48f, s * 0.36f), s * 0.1f, ink);
                UiShapes.Fill(new Rectangle(c.X - s * 0.09f, c.Y - s * 0.66f, s * 0.18f, s * 1.38f), 0, back);
                break;
            case ArcadeEffect.FoeLevelUp or ArcadeEffect.AllyLevelUp:
                UiIcons.ArrowUp(P(-0.62f, 0f), s * 0.66f, ink);
                Words("+3", s * 1.1f, 0.42f);
                break;
            case ArcadeEffect.Sunny:
                UiShapes.Circle(c, s * 0.38f, ink);
                for (int ray = 0; ray < 8; ray++)
                {
                    float a = ray * MathF.PI / 4f;
                    var d = new Vector2(MathF.Cos(a), MathF.Sin(a));
                    UiShapes.Line(c + d * s * 0.58f, c + d * s * 0.86f, line * 0.8f, ink);
                }
                break;
            case ArcadeEffect.Rainy:
                Cloud();
                for (int drop = -1; drop <= 1; drop++) UiShapes.Line(P(drop * 0.46f + 0.1f, 0.3f), P(drop * 0.46f - 0.08f, 0.76f), line * 0.7f, ink);
                break;
            case ArcadeEffect.Hail:
                Cloud();
                for (int stone = -1; stone <= 1; stone++) UiShapes.Circle(P(stone * 0.46f, stone == 0 ? 0.72f : 0.46f), s * 0.14f, ink);
                break;
            case ArcadeEffect.Sandy:
                // Drifts blown one way, with grains ahead of them
                UiShapes.Line(P(-0.85f, -0.45f), P(0.35f, -0.45f), line, ink);
                UiShapes.Line(P(-0.45f, 0f), P(0.85f, 0f), line, ink);
                UiShapes.Line(P(-0.85f, 0.45f), P(0.15f, 0.45f), line, ink);
                UiShapes.Circle(P(0.72f, -0.45f), s * 0.1f, ink);
                UiShapes.Circle(P(-0.8f, 0f), s * 0.1f, ink);
                UiShapes.Circle(P(0.52f, 0.45f), s * 0.1f, ink);
                break;
            case ArcadeEffect.Foggy:
                // Three thick bars, each fainter than the last
                UiShapes.Line(P(-0.75f, -0.5f), P(0.75f, -0.5f), s * 0.3f, ink);
                UiShapes.Line(P(-0.75f, 0f), P(0.75f, 0f), s * 0.3f, ink with { A = 190 });
                UiShapes.Line(P(-0.75f, 0.5f), P(0.75f, 0.5f), s * 0.3f, ink with { A = 130 });
                break;
            case ArcadeEffect.TrickRoom:
                // A room turned on its corner
                UiShapes.Turned(c, s * 0.62f, s * 0.62f, s * 0.1f, 45f, ink);
                UiShapes.Turned(c, s * 0.4f, s * 0.4f, s * 0.06f, 45f, back);
                UiShapes.Circle(c, s * 0.14f, ink);
                break;
            case ArcadeEffect.SpeedUp:
                UiShapes.Triangle(P(-0.8f, -0.6f), P(-0.8f, 0.6f), P(-0.05f, 0f), ink, 2);
                UiShapes.Triangle(P(0.05f, -0.6f), P(0.05f, 0.6f), P(0.8f, 0f), ink, 2);
                break;
            case ArcadeEffect.SlowDown:
                UiShapes.Triangle(P(0.8f, -0.6f), P(0.8f, 0.6f), P(0.05f, 0f), ink, 2);
                UiShapes.Triangle(P(-0.05f, -0.6f), P(-0.05f, 0.6f), P(-0.8f, 0f), ink, 2);
                break;
            case ArcadeEffect.RandomizeCursor:
                // A die showing five
                UiShapes.Fill(new Rectangle(c.X - s * 0.66f, c.Y - s * 0.66f, s * 1.32f, s * 1.32f), s * 0.22f, ink);
                foreach (var (x, y) in new[] { (-0.34f, -0.34f), (0.34f, -0.34f), (0f, 0f), (-0.34f, 0.34f), (0.34f, 0.34f) }) UiShapes.Circle(P(x, y), s * 0.12f, back);
                break;
            case ArcadeEffect.SwapMons:
                // Two arrows passing one another
                UiShapes.Line(P(-0.7f, -0.38f), P(0.3f, -0.38f), line, ink);
                UiIcons.ArrowH(P(0.52f, -0.38f), s * 0.46f, 1, ink);
                UiShapes.Line(P(0.7f, 0.38f), P(-0.3f, 0.38f), line, ink);
                UiIcons.ArrowH(P(-0.52f, 0.38f), s * 0.46f, -1, ink);
                break;
            case ArcadeEffect.Get1Bp or ArcadeEffect.Get3Bp:
            {
                // A coin with its number
                UiShapes.Circle(c, s * 0.78f, ink);
                UiShapes.Ring(c, s * 0.62f, 3, back);
                string number = effect == ArcadeEffect.Get1Bp ? "1" : "3";
                UiFonts.DrawCentered(number, c.X - UiFonts.Measure(number, s * 1.05f, UiWeight.Black) / 2f, c.Y, s * 1.05f, back, UiWeight.Black);
                break;
            }
            case ArcadeEffect.NoBattle:
                // A ring with a bar across it
                UiShapes.Ring(c, s * 0.78f, s * 0.2f, ink);
                UiShapes.Line(P(-0.48f, 0.48f), P(0.48f, -0.48f), line, ink);
                break;
            default:
                // Nothing: a dash
                UiShapes.Line(P(-0.5f, 0f), P(0.5f, 0f), s * 0.24f, ink);
                break;
        }
    }
}
