using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The Battle Frontier on screen (plan 06 · R18a; style guide, "The Battle Frontier"): a facility's records
/// board and the count of Battle Points that stands over the field while they are spoken of.
/// </summary>
internal static partial class ModernUi
{
    private static readonly Color PrintSilver = new(168, 178, 198, 255), PrintGold = new(232, 180, 60, 255);

    /// <summary>
    /// A facility's board: its name, a row for each way its challenge is taken (the record streak large, the latest
    /// beside it, a mark while it still runs), and under them the Battle Points, the battles won and the print.
    /// </summary>
    public static void DrawFrontierBoard(int sw, int sh, FrontierBoard board, float appear = 1f)
    {
        Backdrop(sw, sh);
        ScreenTitle("BATTLE RECORDS");
        Hints(sw - Margin, 44, ("Z", "Close"), ("Esc", "Back"));
        float rise = (1f - UiMotion.EaseOut(appear)) * 60f;
        var panel = new Rectangle(Margin, ContentTop + rise, sw - Margin * 2, ContentBottom - ContentTop);
        Panel(panel, 40);

        float x = panel.X + 56;
        UiFonts.Draw(board.Title.ToUpperInvariant(), x, panel.Y + 36, 48, Ink, UiWeight.Black);
        // The print won, a medal at the head's right: silver, gold, or the place it will hang
        var medal = new Vector2(panel.X + panel.Width - 110, panel.Y + 66);
        Color metal = board.Print switch { FrontierPrint.Gold => PrintGold, FrontierPrint.Silver => PrintSilver, _ => Rule };
        UiShapes.Circle(medal, 38, metal);
        UiShapes.Ring(medal, 38, 5, board.Print == FrontierPrint.None ? Muted with { A = 90 } : Color.White with { A = 200 });
        string printed = board.Print switch { FrontierPrint.Gold => "GOLD PRINT", FrontierPrint.Silver => "SILVER PRINT", _ => "NO PRINT YET" };
        UiFonts.DrawCentered(printed, medal.X - 56 - UiFonts.Measure(printed, 26, UiWeight.Black), medal.Y, 26, board.Print == FrontierPrint.None ? Muted : Ink, UiWeight.Black);

        if (board.Rows.Count == 0)
        {
            EmptyNote(panel, "No challenge has been taken here yet.");
            return;
        }

        // The columns' heads
        float top = panel.Y + 132, rowH = board.Rows.Count > 3 ? 112 : 132, recordX = panel.X + panel.Width * 0.52f, latestX = panel.X + panel.Width * 0.76f;
        Label("CHALLENGE", x, top);
        Label("RECORD STREAK", recordX, top);
        Label("LATEST STREAK", latestX, top);
        for (int i = 0; i < board.Rows.Count; i++)
        {
            var row = board.Rows[i];
            var r = new Rectangle(panel.X + 32, top + 36 + i * rowH, panel.Width - 64, rowH - 16);
            Card(r, 28, false);
            UiFonts.DrawCentered(row.Label, r.X + 28, r.Y + r.Height / 2f, 40, Ink, UiWeight.ExtraBold);
            UiFonts.DrawCentered(row.Record.ToString("N0"), recordX, r.Y + r.Height / 2f, 56, Ink, UiWeight.Black);
            UiFonts.DrawCentered(row.Latest.ToString("N0"), latestX, r.Y + r.Height / 2f, 44, row.Running ? Ink : Muted, UiWeight.Black);
            // What a facility counts beside its streaks (the Battle Factory's trades), small after each
            if (board.CountLabel != null && row.RecordCount is { } recordCount && row.LatestCount is { } latestCount)
            {
                string kept = $"{recordCount:N0} {board.CountLabel}", last = $"{latestCount:N0} {board.CountLabel}";
                UiFonts.DrawCentered(kept, recordX + UiFonts.Measure(row.Record.ToString("N0"), 56, UiWeight.Black) + 20, r.Y + r.Height / 2f + 8, 22, Muted, UiWeight.Black);
                UiFonts.DrawCentered(last, latestX + UiFonts.Measure(row.Latest.ToString("N0"), 44, UiWeight.Black) + 20, r.Y + r.Height / 2f + 22, 20, Muted, UiWeight.Black);
            }
            // A streak still running is taken up again by the next challenge
            if (row.Running) Tag(latestX + UiFonts.Measure(row.Latest.ToString("N0"), 44, UiWeight.Black) + 24, r.Y + r.Height / 2f - (board.CountLabel != null ? 34 : 19), "RUNNING", Green, board.CountLabel != null ? 30 : 38);
        }

        // The species with records of their own (the Battle Hall's), eight to the board: a name, and its longest
        // streak alone and as a pair
        if (board.Species.Count > 0)
        {
            float listTop = top + 36 + board.Rows.Count * rowH + 4;
            Label(board.Species.Count > 8 ? $"BY POK\u00c9MON · THE FIRST 8 OF {board.Species.Count:N0}" : "BY POK\u00c9MON", x, listTop);
            float cellW = (panel.Width - 64 - 3 * 16) / 4f, cellH = 84;
            for (int i = 0; i < Math.Min(8, board.Species.Count); i++)
            {
                var kept = board.Species[i];
                var r = new Rectangle(panel.X + 32 + i % 4 * (cellW + 16), listTop + 34 + i / 4 * (cellH + 12), cellW, cellH);
                UiShapes.Fill(r, 22, Rule with { A = 70 });
                UiFonts.DrawCentered(kept.Species, r.X + 22, r.Y + r.Height / 2f, 28, Ink, UiWeight.ExtraBold);
                // Its two records, each under its own small caption at the cell's right
                string alone = kept.Single.ToString("N0"), twice = kept.Double.ToString("N0");
                float numbers = r.X + r.Width - 22, captions = numbers - 64 - UiFonts.Measure("DOUBLE", 18, UiWeight.Black);
                UiFonts.DrawCentered("SINGLE", captions, r.Y + 27, 18, Muted, UiWeight.Black);
                UiFonts.DrawCentered("DOUBLE", captions, r.Y + 59, 18, Muted, UiWeight.Black);
                UiFonts.DrawCentered(alone, numbers - UiFonts.Measure(alone, 26, UiWeight.Black), r.Y + 26, 26, kept.Single > 0 ? Ink : Muted, UiWeight.Black);
                UiFonts.DrawCentered(twice, numbers - UiFonts.Measure(twice, 26, UiWeight.Black), r.Y + 58, 26, kept.Double > 0 ? Ink : Muted, UiWeight.Black);
            }
        }

        // The foot: the Battle Points and the battles won here
        var foot = new Rectangle(panel.X + 32, panel.Y + panel.Height - 132, panel.Width - 64, 100);
        UiShapes.Fill(foot, 26, Rule with { A = 90 });
        Label("BATTLE POINTS", foot.X + 28, foot.Y + 16);
        UiFonts.Draw($"{board.BattlePoints:N0} BP", foot.X + 28, foot.Y + 42, 40, Ink, UiWeight.Black);
        Label("BATTLES WON HERE", foot.X + 420, foot.Y + 16);
        UiFonts.Draw(board.Victories.ToString("N0"), foot.X + 420, foot.Y + 42, 40, Ink, UiWeight.Black);
    }

    /// <summary>The Battle Points held, standing over the field while an Exchange Service Corner or a battle room speaks of them (where the Coin Case's count stands in the Game Corner).</summary>
    public static void PointsCount(int points)
    {
        var r = new Rectangle(32, 32, 300, 96);
        Panel(r, 24);
        Label("BATTLE POINTS", r.X + 28, r.Y + 18);
        UiFonts.Draw($"{points:N0} BP", r.X + 28, r.Y + 44, 36, Ink, UiWeight.Black);
    }
}
