using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The screens that keep the player's records: the Trainer Card, the Hall of Fame, the Journal and saving (the
/// Pokédex has a file of its own).
/// </summary>
internal static partial class ModernUi
{
    // ------------------------------------------------------------------ Hall of Fame (plan 06 · R12)

    /// <summary>
    /// The PC's Hall of Fame: the team under its number and day, its Pokémon as six cards in a row, and under them
    /// the one under the cursor told in full, or its moves.
    /// </summary>
    public static void DrawHallOfFame(int sw, int sh, HallOfFameScreen screen, HallOfFame records, float appear = 1f)
    {
        Backdrop(sw, sh);
        ScreenTitle("HALL OF FAME");
        Hints(sw - Margin, 44, ("Left / Right", "Team"), ("Up / Down", "Pokémon"), ("Z", screen.ShowingMoves ? "Profile" : "Moves"), ("Esc", "Back"));
        float rise = (1f - UiMotion.EaseOut(appear)) * 60f;
        var panel = new Rectangle(Margin, ContentTop + rise, sw - Margin * 2, ContentBottom - ContentTop);
        Panel(panel, 40);
        if (screen.Shown(records) is not var (number, entry))
        {
            EmptyNote(panel, "No team has entered the Hall of Fame yet.");
            return;
        }

        // ---- The head: the team's number, its day, which of the teams kept it is
        float hx = panel.X + 48;
        float tagW = Tag(hx, panel.Y + 34, $"No. {number}", Gold, 56);
        UiFonts.DrawCentered(entry.Date.ToString("d MMMM yyyy", CultureInfo.InvariantCulture), hx + tagW + 28, panel.Y + 62, 40, Ink, UiWeight.Black);
        string which = $"TEAM {screen.Entry + 1} OF {records.Entries.Count}";
        UiFonts.DrawCentered(which, panel.X + panel.Width - 48 - UiFonts.Measure(which, 24, UiWeight.Black), panel.Y + 62, 24, Muted, UiWeight.Black);

        // ---- The team, a card each
        const float gap = 20, cardH = 356;
        float cardW = (panel.Width - 96 - gap * 5) / 6f;
        int member = Math.Clamp(screen.Member, 0, entry.Team.Count - 1);
        for (int i = 0; i < Party.MaxSize; i++)
        {
            var r = new Rectangle(panel.X + 48 + i * (cardW + gap), panel.Y + 124, cardW, cardH);
            if (i >= entry.Team.Count)
            {
                UiShapes.Shape(r, 30, SlotTop, SlotBottom, Rule, 3);
                continue;
            }
            var m = entry.Team[i];
            bool selected = i == member;
            if (selected) UiShapes.Shadow(r, 30, 46, Vector2.Zero, Gold with { A = 120 });
            Card(r, 30, selected, Gold);
            var disc = new Vector2(r.X + cardW / 2f, r.Y + 22 + 108);
            BallDisc(disc, 108);
            PixelArt(PixelArtGenerator.GetPokemonSprite(m.Form ?? m.Species, isBack: false), disc, 2, Hop(selected) * 2);
            float size = 32;
            while (size > 22 && UiFonts.Measure(m.Nickname, size, UiWeight.Black) > cardW - 28) size -= 2;
            UiFonts.DrawCentered(m.Nickname, r.X + (cardW - UiFonts.Measure(m.Nickname, size, UiWeight.Black)) / 2f, r.Y + 286, size, Ink, UiWeight.Black);
            string level = $"Lv. {m.Level}";
            UiFonts.DrawCentered(level, r.X + (cardW - UiFonts.Measure(level, 26, UiWeight.ExtraBold)) / 2f, r.Y + 324, 26, Muted, UiWeight.ExtraBold);
        }

        // ---- The one chosen: who it is, or its moves
        var shown = entry.Team[member];
        float top = panel.Y + 124 + cardH + 36;
        Tabs(new Rectangle(hx, top, 520, 64), new[] { ("PROFILE", UiIcon.Trainer, Blue), ("MOVES", UiIcon.Disc, Red) }, screen.ShowingMoves ? 1 : 0);
        float y = top + 100;
        float wide = panel.Width - 96;
        if (!screen.ShowingMoves)
        {
            UiFonts.DrawCentered(shown.Nickname, hx, y + 34, 56, Ink, UiWeight.Black);
            float after = hx + UiFonts.Measure(shown.Nickname, 56, UiWeight.Black);
            if (shown.Gender is Gender.Male or Gender.Female)
            {
                UiIcons.GenderMark(new Vector2(after + 34, y + 34), 44, shown.Gender);
                after += 64;
            }
            if (shown.Shiny) Star(new Vector2(after + 34, y + 34), 20, Gold);
            float fx = hx;
            fx = Field("SPECIES", shown.Species, fx, y + 92, 36, 72);
            fx = Field("LEVEL", shown.Level.ToString(), fx, y + 92, 36, 72);
            fx = Field("ORIGINAL TRAINER", shown.TrainerName, fx, y + 92, 36, 72);
            fx = Field("ID No.", TrainerCardScreen.FormatId(shown.TrainerId), fx, y + 92, 36, 72);
            if (PokemonDatabase.Get(shown.Species) is { } species)
            {
                Label("TYPE", fx, y + 92);
                float px = fx + TypePill(fx, y + 120, species.PrimaryType, 40) + 10;
                if (species.SecondaryType is { } second) TypePill(px, y + 120, second, 40);
            }
        }
        else
        {
            const float pillGap = 24, pillH = 76;
            float pillW = (wide - pillGap) / 2f;
            for (int i = 0; i < 4; i++)
            {
                var r = new Rectangle(hx + i % 2 * (pillW + pillGap), y + i / 2 * (pillH + 20), pillW, pillH);
                if (i >= shown.Moves.Count)
                {
                    UiShapes.Shape(r, pillH / 2f, SlotTop, SlotBottom, Rule, 3);
                    continue;
                }
                var move = MoveDatabase.Get(shown.Moves[i]);
                var color = Palette.GetTypeColor(move.Type.ToString());
                UiShapes.Shadow(r, pillH / 2f, 14, new Vector2(0, 5), ShadowColor);
                UiShapes.Shape(r, pillH / 2f, Lighter(color, 0.12f), Darker(color, 0.1f), Darker(color, 0.32f), 3);
                UiFonts.DrawCentered(shown.Moves[i], r.X + 40, r.Y + pillH / 2f, 34, Color.White, UiWeight.Black);
                string type = move.Type.ToString().ToUpperInvariant();
                UiFonts.DrawCentered(type, r.X + r.Width - 40 - UiFonts.Measure(type, 22, UiWeight.Black), r.Y + pillH / 2f, 22,
                    new Color(255, 255, 255, 200), UiWeight.Black);
            }
        }
    }

    // ------------------------------------------------------------------ Journal (plan 06 · R12)

    private static readonly Color PaperTop = new(252, 248, 236, 255), PaperBottom = new(244, 237, 218, 255);
    private static readonly Color PaperEdge = new(222, 210, 184, 255), PaperRule = new(196, 214, 234, 255);
    private static readonly Color PaperMargin = new(236, 170, 162, 255), Spiral = new(150, 160, 182, 255);

    /// <summary>How far apart the rules of a notebook page are.</summary>
    public const float NotebookRule = 70;

    /// <summary>
    /// A page of a notebook: cream paper, ruled in pale blue from <paramref name="firstRule"/> down, a red margin line
    /// and the spiral's rings down its left edge.
    /// </summary>
    public static void NotebookPage(Rectangle page, float firstRule, float marginX)
    {
        UiShapes.Shadow(page, 28, 24, new Vector2(0, 10), ShadowColor);
        UiShapes.Shape(page, 28, PaperTop, PaperBottom, PaperEdge, 3);
        for (float y = firstRule; y < page.Y + page.Height - 70; y += NotebookRule)
            UiShapes.Fill(new Rectangle(page.X + 96, y - 1, page.Width - 136, 2), 1, PaperRule);
        UiShapes.Fill(new Rectangle(marginX - 1.5f, page.Y + 3, 3, page.Height - 6), 1.5f, PaperMargin);
        for (float y = page.Y + 56; y < page.Y + page.Height - 30; y += 62)
        {
            UiShapes.Circle(new Vector2(page.X + 46, y), 9, PaperEdge);
            UiShapes.Line(new Vector2(page.X - 16, y - 4), new Vector2(page.X + 46, y), 11, Spiral);
            UiShapes.Line(new Vector2(page.X - 12, y - 6), new Vector2(page.X + 40, y - 3), 3, new Color(214, 220, 232, 255));
        }
    }

    /// <summary>A page of the Journal: its day, its lines in our own words on the rules, and the day's Pokémon as a photograph.</summary>
    public static void DrawJournal(int sw, int sh, JournalScreen screen, Journal journal, float appear = 1f)
    {
        Backdrop(sw, sh);
        ScreenTitle("JOURNAL");
        Hints(sw - Margin, 44, ("Left / Right", "Turn the page"), ("Esc", "Close"));
        float rise = (1f - UiMotion.EaseOut(appear)) * 60f;
        var page = new Rectangle(sw / 2f - 760, ContentTop + 6 + rise, 1520, ContentBottom - ContentTop - 6);
        float textX = page.X + 196, firstRule = page.Y + 250;
        NotebookPage(page, firstRule, page.X + 168);
        if (screen.Page >= journal.All.Count)
        {
            EmptyNote(page, "Nothing has been written yet.");
            return;
        }
        var day = journal.All[screen.Page];

        // ---- Its day
        UiFonts.Draw(day.Date.ToString("dddd", CultureInfo.InvariantCulture).ToUpperInvariant(), textX, page.Y + 60, 24, Muted, UiWeight.Black);
        UiFonts.Draw(day.Date.ToString("d MMMM yyyy", CultureInfo.InvariantCulture), textX, page.Y + 90, 48, Ink, UiWeight.Black);

        // ---- The day's Pokémon, pinned at the top right
        float textRight = page.X + page.Width - 60;
        if (day.Pokemon is { } mon)
        {
            var photo = new Rectangle(page.X + page.Width - 380, page.Y + 48, 300, 344);
            UiShapes.Shadow(photo, 10, 16, new Vector2(0, 6), ShadowColor);
            UiShapes.Shape(photo, 10, Color.White, new Color(246, 246, 250, 255), new Color(220, 222, 232, 255), 2);
            var picture = new Rectangle(photo.X + 18, photo.Y + 18, photo.Width - 36, 250);
            UiShapes.Shape(picture, 6, new Color(214, 232, 246, 255), new Color(196, 222, 200, 255));
            PixelArt(PixelArtGenerator.GetPokemonSprite(mon.Species, isBack: false), new Vector2(picture.X + picture.Width / 2f, picture.Y + picture.Height / 2f + 6), 2);
            string name = mon.Species.ToUpperInvariant();
            UiFonts.DrawCentered(name, photo.X + (photo.Width - UiFonts.Measure(name, 28, UiWeight.Black)) / 2f, photo.Y + photo.Height - 38, 28, Ink, UiWeight.Black);
            UiShapes.Fill(new Rectangle(photo.X + photo.Width / 2f - 64, photo.Y - 16, 128, 38), 4, new Color(246, 236, 196, 210), skew: 0.25f);
            textRight = photo.X - 40;
        }

        // ---- Its lines, each on a rule, a long one carried on to the next
        int row = 0;
        float lastRule = page.Y + page.Height - 70;
        foreach (string line in Journal.Lines(day))
            foreach (string part in Wrap(line, (row < 3 ? textRight : page.X + page.Width - 60) - textX, 32, UiWeight.ExtraBold))
            {
                float rule = firstRule + row * NotebookRule;
                if (rule >= lastRule) break;
                UiFonts.DrawCentered(part, textX, rule - 24, 32, Ink, UiWeight.ExtraBold);
                row++;
            }

        // ---- Which page, between arrows where there are more
        int count = journal.All.Count;
        string where = $"{screen.Page + 1} / {count}";
        float cx = page.X + page.Width / 2f + 84, cy = page.Y + page.Height - 40;
        UiFonts.DrawCentered(where, cx - UiFonts.Measure(where, 28, UiWeight.Black) / 2f, cy, 28, Muted, UiWeight.Black);
        if (screen.Page < count - 1) UiIcons.ArrowH(new Vector2(cx - 90, cy), 18, -1, Selection);
        if (screen.Page > 0) UiIcons.ArrowH(new Vector2(cx + 90, cy), 18, 1, Selection);
    }

    // ------------------------------------------------------------------ Trainer Card

    /// <summary>The card's colour for each step of what the player has done (style guide, "Trainer Card"; our own palette).</summary>
    public static Color CardColor(TrainerCardRules.CardColour colour) => colour switch
    {
        TrainerCardRules.CardColour.NoPokedex => new Color(128, 138, 158, 255),
        TrainerCardRules.CardColour.Normal => new Color(34, 156, 142, 255),
        TrainerCardRules.CardColour.Cobalt => new Color(50, 96, 212, 255),
        TrainerCardRules.CardColour.Bronze => new Color(178, 108, 60, 255),
        TrainerCardRules.CardColour.Silver => new Color(134, 146, 170, 255),
        TrainerCardRules.CardColour.Gold => new Color(206, 156, 36, 255),
        _ => new Color(46, 48, 66, 255)
    };

    private static bool Metallic(TrainerCardRules.CardColour colour) =>
        colour is TrainerCardRules.CardColour.Bronze or TrainerCardRules.CardColour.Silver or TrainerCardRules.CardColour.Gold;

    /// <summary>A star of the card: gold with a dark edge when done, faint when not.</summary>
    private static void CardStar(Vector2 c, float radius, bool done, Color edge, Color faint)
    {
        if (!done)
        {
            Star(c, radius, faint);
            return;
        }
        Star(c, radius + 4, edge);
        Star(c, radius, new Color(255, 220, 104, 255));
    }

    /// <summary>
    /// The Trainer Card: one wide card that rises into place, in the colour of what the player has done: its band
    /// carries the name, the stars and the ID number; the front has the player's records, their field sprite and the
    /// badges, the back the Hall of Fame debut, the link battles and trades and the day the adventure began.
    /// <paramref name="flip"/> runs from 0 to 1 as the card is turned over to the side <paramref name="back"/> says.
    /// </summary>
    public static void DrawTrainerCard(int sw, int sh, TrainerCardInfo info, Texture2D? portrait, float appear = 1f, bool back = false, float flip = 1f)
    {
        Backdrop(sw, sh);
        ScreenTitle("TRAINER CARD");
        Hints(sw - Margin, 44, ("Z", "Turn over"), ("Esc", "Back"));

        float rise = (1f - UiMotion.EaseOut(appear)) * 60f;
        var card = new Rectangle(200, 150 + rise, sw - 400, 840);

        // Turning over: the card narrows to its edge showing the side it had, then opens on the other
        bool turning = flip < 1f;
        bool showBack = turning && flip < 0.5f ? !back : back;
        if (turning)
        {
            float cx = card.X + card.Width / 2f;
            Rlgl.PushMatrix();
            Rlgl.Translatef(cx, 0f, 0f);
            Rlgl.Scalef(Math.Max(0.02f, MathF.Abs(MathF.Cos(MathF.PI * flip))), 1f, 1f);
            Rlgl.Translatef(-cx, 0f, 0f);
        }

        var colour = CardColor(info.Colour);
        Panel(card, 44, border: 6, frame: Darker(colour, 0.25f));
        var clear = colour with { A = 0 };
        UiShapes.Shape(new Rectangle(card.X + 12, card.Y + 12, card.Width - 24, card.Height - 24), 34, clear, clear, Lighter(colour, 0.55f), 3);

        // ---- The band: name, stars, ID number
        var band = new Rectangle(card.X + 26, card.Y + 26, card.Width - 52, 108);
        UiShapes.Shape(band, 30, Lighter(colour, 0.16f), Darker(colour, 0.1f), Darker(colour, 0.32f), 3);
        if (Metallic(info.Colour))
        {
            UiShapes.Fill(new Rectangle(band.X + band.Width * 0.56f, band.Y + 6, 86, band.Height - 12), 6, new Color(255, 255, 255, 46), skew: -0.45f);
            UiShapes.Fill(new Rectangle(band.X + band.Width * 0.56f + 104, band.Y + 6, 26, band.Height - 12), 6, new Color(255, 255, 255, 34), skew: -0.45f);
        }
        float bandY = band.Y + band.Height / 2f;
        UiFonts.DrawCentered(info.Name, band.X + 44, bandY + 3, 52, new Color(0, 0, 0, 60), UiWeight.Black);
        UiFonts.DrawCentered(info.Name, band.X + 44, bandY, 52, Color.White, UiWeight.Black);
        string id = $"ID No. {TrainerCardScreen.FormatId(info.TrainerId)}";
        float idX = band.X + band.Width - 44 - UiFonts.Measure(id, 32, UiWeight.Black);
        UiFonts.DrawCentered(id, idX, bandY, 32, Color.White, UiWeight.Black);
        for (int k = 0; k < 5; k++)
            CardStar(new Vector2(idX - 70 - (4 - k) * 52, bandY), 20, k < info.Stars, Darker(colour, 0.5f), Lighter(colour, 0.32f));

        if (showBack) CardBack(card, info, colour);
        else CardFront(card, info, portrait, colour);

        if (turning) Rlgl.PopMatrix();
    }

    private static void CardFront(Rectangle card, TrainerCardInfo info, Texture2D? portrait, Color colour)
    {
        // ---- Records
        bool dex = info.Colour != TrainerCardRules.CardColour.NoPokedex;
        float x = card.X + 72, y = card.Y + 176;
        Label("MONEY", x, y);
        Money(x, y + 24 + 40 * 0.56f, info.Money, 40, Ink);
        Field("TIME PLAYED", TitleScreen.FormatPlayTime(info.PlayTimeSeconds), x + 440, y);
        Field("POKÉMON CAUGHT", dex ? info.Caught.ToString() : "—", x, y + 124);
        Field("POKÉMON SEEN", dex ? info.Seen.ToString() : "—", x + 440, y + 124);
        Field("SCORE", info.Score.ToString("N0", CultureInfo.InvariantCulture), x, y + 248);

        // ---- The player's own field sprite, on a pale plate
        var plate = new Rectangle(card.X + card.Width - 72 - 340, card.Y + 160, 340, 392);
        var tint = PixelCanvas.Mix(Disc, colour, 0.1f);
        UiShapes.Shape(plate, 30, tint, tint, Rule, 4);
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

    private static void CardBack(Rectangle card, TrainerCardInfo info, Color colour)
    {
        float x = card.X + 72, right = card.X + card.Width - 72, top = card.Y + 166;
        const float rowH = 108;
        var rows = new (string Caption, string Value)[]
        {
            ("HALL OF FAME DEBUT", info.HallOfFameDebut is { } debut ? debut.ToString("d MMMM yyyy   HH:mm", CultureInfo.InvariantCulture) : "—"),
            ("LINK BATTLES", $"{info.LinkWins} won,  {info.LinkLosses} lost"),
            ("LINK TRADES", info.LinkTrades.ToString()),
            ("ADVENTURE STARTED", info.Started is { } day ? day.ToString("d MMMM yyyy", CultureInfo.InvariantCulture) : "—")
        };
        for (int i = 0; i < rows.Length; i++)
        {
            float cy = top + i * rowH + rowH / 2f;
            if (i > 0) UiShapes.Fill(new Rectangle(x, top + i * rowH - 1.5f, right - x, 3), 1.5f, Rule);
            UiFonts.DrawCentered(rows[i].Caption, x, cy, 28, Muted, UiWeight.Black);
            UiFonts.DrawCentered(rows[i].Value, right - UiFonts.Measure(rows[i].Value, 40, UiWeight.Black), cy, 40, Ink, UiWeight.Black);
        }

        // ---- The stars again, at the foot
        float by = card.Y + 630;
        UiShapes.Fill(new Rectangle(x, by, right - x, 3), 1.5f, Rule);
        Label("STARS", x, by + 26);
        for (int k = 0; k < 5; k++)
            CardStar(new Vector2(x + 36 + k * 88, by + 118), 32, k < info.Stars, Darker(colour, 0.5f), Rule);
        string count = $"{info.Stars} / 5";
        UiFonts.DrawCentered(count, x + 36 + 4 * 88 + 72, by + 118, 40, Ink, UiWeight.Black);
        const string note = "A star for each of five great feats.";
        UiFonts.DrawCentered(note, right - UiFonts.Measure(note, 26, UiWeight.ExtraBold), by + 118, 26, Muted, UiWeight.ExtraBold);
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
