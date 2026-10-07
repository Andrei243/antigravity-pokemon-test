using System;
using System.Globalization;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The screens that keep the player's records: the Trainer Card and saving (the Pokédex has a file of its own).</summary>
internal static partial class ModernUi
{
    // ------------------------------------------------------------------ Hall of Fame (plan 06 · R12)

    /// <summary>
    /// The PC's Hall of Fame: the team shown under its number and day, its six Pokémon in a row, and the one under the
    /// cursor told in full, or its moves.
    /// </summary>
    public static void DrawHallOfFame(int sw, int sh, HallOfFameScreen screen, HallOfFame records, float appear = 1f)
    {
        Backdrop(sw, sh);
        ScreenTitle("HALL OF FAME");
        Hints(sw - Margin, 44, ("Left / Right", "Team"), ("Z", screen.ShowingMoves ? "Who" : "Moves"), ("Esc", "Back"));
        float rise = (1f - UiMotion.EaseOut(appear)) * 60f;
        var panel = new Rectangle(Margin, ContentTop + rise, sw - Margin * 2, ContentBottom - ContentTop);
        Panel(panel, 40);
        if (screen.Shown(records) is not var (number, entry))
        {
            const string none = "No team has entered the Hall of Fame yet.";
            UiFonts.DrawCentered(none, panel.X + (panel.Width - UiFonts.Measure(none, 30, UiWeight.ExtraBold)) / 2f, panel.Y + panel.Height / 2f, 30, Muted, UiWeight.ExtraBold);
            return;
        }
        string title = $"No. {number}    {entry.Date.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}";
        UiFonts.DrawCentered(title, panel.X + 48, panel.Y + 56, 40, Ink, UiWeight.Black);
        float cell = (panel.Width - 96) / 6f;
        for (int i = 0; i < entry.Team.Count; i++)
        {
            var m = entry.Team[i];
            var c = new Vector2(panel.X + 48 + cell * (i + 0.5f), panel.Y + 230);
            bool selected = i == screen.Member;
            if (selected) UiShapes.Circle(c, 96, Selection with { A = 90 });
            UiShapes.Circle(c, 84, Disc);
            PixelArt(PixelArtGenerator.GetPokemonSprite(m.Form ?? m.Species, isBack: false), c, 2, Hop(selected) * 2);
        }
        var shown = entry.Team[Math.Clamp(screen.Member, 0, entry.Team.Count - 1)];
        float x = panel.X + 72, y = panel.Y + 420;
        if (!screen.ShowingMoves)
        {
            UiFonts.DrawCentered($"{shown.Nickname}", x, y, 44, Ink, UiWeight.Black);
            UiFonts.DrawCentered($"{shown.Species}   Lv. {shown.Level}", x, y + 64, 32, Muted, UiWeight.ExtraBold);
            UiFonts.DrawCentered($"OT  {shown.TrainerName}   ID No. {TrainerCardScreen.FormatId(shown.TrainerId)}", x, y + 120, 30, Ink, UiWeight.ExtraBold);
        }
        else
            for (int i = 0; i < shown.Moves.Count; i++)
                UiFonts.DrawCentered(shown.Moves[i], x, y + i * 56, 34, Ink, UiWeight.ExtraBold);
    }

    // ------------------------------------------------------------------ Journal (plan 06 · R12)

    /// <summary>A page of the Journal: its day, where the game was taken up, and its lines in our own words.</summary>
    public static void DrawJournal(int sw, int sh, JournalScreen screen, Journal journal, float appear = 1f)
    {
        Backdrop(sw, sh);
        ScreenTitle("JOURNAL");
        Hints(sw - Margin, 44, ("Left / Right", "Turn the page"), ("Esc", "Close"));
        float rise = (1f - UiMotion.EaseOut(appear)) * 60f;
        var page = new Rectangle(sw / 2f - 620, ContentTop + rise, 1240, ContentBottom - ContentTop);
        Panel(page, 40);
        if (screen.Page >= journal.All.Count)
        {
            const string empty = "Nothing has been written yet.";
            UiFonts.DrawCentered(empty, page.X + (page.Width - UiFonts.Measure(empty, 30, UiWeight.ExtraBold)) / 2f, page.Y + page.Height / 2f, 30, Muted, UiWeight.ExtraBold);
            return;
        }
        var day = journal.All[screen.Page];
        UiFonts.DrawCentered(day.Date.ToString("dddd d MMMM yyyy", CultureInfo.InvariantCulture), page.X + 64, page.Y + 64, 40, Ink, UiWeight.Black);
        float y = page.Y + 150;
        foreach (string line in Journal.Lines(day))
        {
            UiFonts.DrawCentered(line, page.X + 64, y, 32, Ink, UiWeight.ExtraBold);
            y += 64;
        }
        string where = $"{screen.Page + 1} / {journal.All.Count}";
        UiFonts.DrawCentered(where, page.X + page.Width - 64 - UiFonts.Measure(where, 26, UiWeight.Black), page.Y + page.Height - 48, 26, Muted, UiWeight.Black);
    }

    // ------------------------------------------------------------------ Trainer Card

    /// <summary>
    /// The Trainer Card: one wide card that rises into place, a Blue band across its top with the name and ID
    /// number, the player's records on the left, their field sprite on the right and the badges along the bottom.
    /// </summary>
    public static void DrawTrainerCard(int sw, int sh, TrainerCardInfo info, Texture2D? portrait, float appear = 1f)
    {
        Backdrop(sw, sh);
        ScreenTitle("TRAINER CARD");
        Hints(sw - Margin, 44, ("Esc", "Back"));

        float rise = (1f - UiMotion.EaseOut(appear)) * 60f;
        var card = new Rectangle(200, 150 + rise, sw - 400, 840);
        Panel(card, 44);

        var band = new Rectangle(card.X + 18, card.Y + 18, card.Width - 36, 108);
        UiShapes.Shape(band, 30, Lighter(Blue, 0.16f), Darker(Blue, 0.1f), Darker(Blue, 0.32f), 3);
        UiFonts.DrawCentered(info.Name, band.X + 44, band.Y + band.Height / 2f, 52, Color.White, UiWeight.Black);
        string id = $"ID No. {TrainerCardScreen.FormatId(info.TrainerId)}";
        UiFonts.DrawCentered(id, band.X + band.Width - 44 - UiFonts.Measure(id, 32, UiWeight.Black), band.Y + band.Height / 2f, 32, Color.White, UiWeight.Black);

        // ---- Records
        float x = card.X + 72, y = card.Y + 168;
        Label("MONEY", x, y);
        Money(x, y + 24 + 40 * 0.56f, info.Money, 40, Ink);
        Field("TIME PLAYED", TitleScreen.FormatPlayTime(info.PlayTimeSeconds), x + 440, y);
        Field("POKÉMON CAUGHT", info.Caught.ToString(), x, y + 124);
        Field("POKÉMON SEEN", info.Seen.ToString(), x + 440, y + 124);
        Field("ADVENTURE STARTED", info.Started is { } day ? day.ToString("d MMMM yyyy", CultureInfo.InvariantCulture) : "—", x, y + 248);

        // ---- The player's own field sprite, on a pale plate
        var plate = new Rectangle(card.X + card.Width - 72 - 340, card.Y + 152, 340, 400);
        UiShapes.Shape(plate, 30, Disc, Disc, Rule, 4);
        UiShapes.Fill(new Rectangle(plate.X + 28, plate.Y + plate.Height - 64, plate.Width - 56, 16), 8, Rule);
        if (portrait is { } sprite)
        {
            const int scale = 6;
            float w = sprite.Width * scale, h = sprite.Height * scale;
            Raylib.DrawTexturePro(sprite, new Rectangle(0, 0, sprite.Width, sprite.Height),
                new Rectangle(MathF.Round(plate.X + (plate.Width - w) / 2f), MathF.Round(plate.Y + plate.Height - 40 - h), w, h), Vector2.Zero, 0f, Color.White);
        }

        // ---- Badges
        float by = card.Y + 590;
        UiShapes.Fill(new Rectangle(card.X + 72, by, card.Width - 144, 3), 1.5f, Rule);
        Label($"BADGES  {TitleScreen.CountBadges(info.Badges)} / 8", card.X + 72, by + 24);
        float step = (card.Width - 144 - 88) / 7f;
        for (int b = 0; b < 8; b++)
        {
            bool earned = (info.Badges & (1 << b)) != 0;
            var c = new Vector2(card.X + 72 + 44 + b * step, by + 116);
            Badge(c, 44, b, earned);
            string name = BadgeNames[b];
            UiFonts.DrawCentered(name, c.X - UiFonts.Measure(name, 22, UiWeight.Black) / 2f, c.Y + 76, 22, earned ? Ink : Muted, UiWeight.Black);
        }
    }

    // ------------------------------------------------------------------ saving

    /// <summary>
    /// Saving: the save as it will be, the question under it with SAVE and CANCEL, and once it is done, a line
    /// saying so. <paramref name="shown"/> slides the panel up into place.
    /// </summary>
    public static void DrawSave(int sw, int sh, SaveData save, int answer, bool saved, float shown)
    {
        Dim(sw, sh, (int)(150 * shown));
        var r = new Rectangle(sw / 2f - 540, 140 + (1f - shown) * 60f, 1080, 780);
        Panel(r, 36);
        SaveSummary(new Rectangle(r.X, r.Y, r.Width, SaveSummaryHeight), save, "SAVE");
        UiShapes.Fill(new Rectangle(r.X + 48, r.Y + SaveSummaryHeight + 12, r.Width - 96, 3), 1.5f, Rule);

        float cy = r.Y + SaveSummaryHeight + 82;
        if (saved)
        {
            float tw = Tag(r.X + 56, cy + 34, "SAVED", Green, 52);
            UiFonts.DrawCentered($"{save.PlayerName} saved the game.", r.X + 56 + tw + 28, cy + 60, 40, Ink, UiWeight.ExtraBold);
            return;
        }

        UiFonts.DrawCentered("Save the game?", r.X + 56, cy, 44, Ink, UiWeight.Black);
        const float gap = 32;
        float bw = (r.Width - 112 - gap) / 2f;
        Button(new Rectangle(r.X + 56, r.Y + r.Height - 56 - 84, bw, 84), 42, Green, SaveScreen.Answers[0], 32, answer == 0);
        Button(new Rectangle(r.X + 56 + bw + gap, r.Y + r.Height - 56 - 84, bw, 84), 42, Blue, SaveScreen.Answers[1], 32, answer == 1);
    }
}
