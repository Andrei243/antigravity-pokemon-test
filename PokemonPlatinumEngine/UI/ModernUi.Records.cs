using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
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
        ScreenTitle(Text.Of("hallOfFame.title"));
        Hints(sw - Margin, 44, Hint("shared.keys.leftRight", "shared.hints.team"), Hint("shared.keys.upDown", "shared.hints.pokemon"),
            Hint("shared.keys.z", screen.ShowingMoves ? "shared.hints.profile" : "shared.hints.moves"), Hint("shared.keys.esc", "shared.hints.back"));
        float rise = (1f - UiMotion.EaseOut(appear)) * 60f;
        var panel = new Rectangle(Margin, ContentTop + rise, sw - Margin * 2, ContentBottom - ContentTop);
        Panel(panel, 40);
        if (screen.Shown(records) is not var (number, entry))
        {
            EmptyNote(panel, Text.Of("hallOfFame.empty"));
            return;
        }

        // ---- The head: the team's number, its day, which of the teams kept it is
        float hx = panel.X + 48;
        float tagW = Tag(hx, panel.Y + 34, Text.Of("hallOfFame.number", ("number", number)), Gold, 56);
        UiFonts.DrawCentered(Words.Date(entry.Date), hx + tagW + 28, panel.Y + 62, 40, Ink, UiWeight.Black);
        string which = Text.Of("hallOfFame.which", ("entry", screen.Entry + 1), ("count", records.Entries.Count));
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
            PixelArt(PixelArtGenerator.GetPokemonSprite(PokemonLook.Of(m.Form ?? m.Species, m.Shiny), isBack: false), disc, 2, Hop(selected) * 2);
            float size = 32;
            while (size > 22 && UiFonts.Measure(m.ShownName, size, UiWeight.Black) > cardW - 28) size -= 2;
            UiFonts.DrawCentered(m.ShownName, r.X + (cardW - UiFonts.Measure(m.ShownName, size, UiWeight.Black)) / 2f, r.Y + 286, size, Ink, UiWeight.Black);
            string level = Text.Of("hallOfFame.level", ("level", m.Level));
            UiFonts.DrawCentered(level, r.X + (cardW - UiFonts.Measure(level, 26, UiWeight.ExtraBold)) / 2f, r.Y + 324, 26, Muted, UiWeight.ExtraBold);
        }

        // ---- The one chosen: who it is, or its moves
        var shown = entry.Team[member];
        float top = panel.Y + 124 + cardH + 36;
        Tabs(new Rectangle(hx, top, 520, 64), new[] { (Text.Of("hallOfFame.profile"), UiIcon.Trainer, Blue), (Text.Of("hallOfFame.moves"), UiIcon.Disc, Red) }, screen.ShowingMoves ? 1 : 0);
        float y = top + 100;
        float wide = panel.Width - 96;
        if (!screen.ShowingMoves)
        {
            UiFonts.DrawCentered(shown.ShownName, hx, y + 34, 56, Ink, UiWeight.Black);
            float after = hx + UiFonts.Measure(shown.ShownName, 56, UiWeight.Black);
            if (shown.Gender is Gender.Male or Gender.Female)
            {
                UiIcons.GenderMark(new Vector2(after + 34, y + 34), 44, shown.Gender);
                after += 64;
            }
            if (shown.Shiny) Star(new Vector2(after + 34, y + 34), 20, Gold);
            float fx = hx;
            fx = Field(Text.Of("hallOfFame.species"), shown.Species, fx, y + 92, 36, 72);
            fx = Field(Text.Of("hallOfFame.levelCaption"), shown.Level.ToString(), fx, y + 92, 36, 72);
            fx = Field(Text.Of("hallOfFame.trainer"), shown.TrainerName, fx, y + 92, 36, 72);
            fx = Field(Text.Of("hallOfFame.id"), TrainerCardScreen.FormatId(shown.TrainerId), fx, y + 92, 36, 72);
            if (PokemonDatabase.Get(shown.Species) is { } species)
            {
                Label(Text.Of("hallOfFame.type"), fx, y + 92);
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
                UiFonts.DrawCentered(LocalNames.Move(shown.Moves[i]), r.X + 40, r.Y + pillH / 2f, 34, Color.White, UiWeight.Black);
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
        ScreenTitle(Text.Of("journal.title"));
        Hints(sw - Margin, 44, Hint("shared.keys.leftRight", "shared.hints.turnPage"), Hint("shared.keys.esc", "shared.hints.close"));
        float rise = (1f - UiMotion.EaseOut(appear)) * 60f;
        var page = new Rectangle(sw / 2f - 760, ContentTop + 6 + rise, 1520, ContentBottom - ContentTop - 6);
        float textX = page.X + 196, firstRule = page.Y + 250;
        NotebookPage(page, firstRule, page.X + 168);
        if (screen.Page >= journal.All.Count)
        {
            EmptyNote(page, Text.Of("journal.empty"));
            return;
        }
        var day = journal.All[screen.Page];

        // ---- Its day
        UiFonts.Draw(Words.Weekday(day.Date).ToUpperInvariant(), textX, page.Y + 60, 24, Muted, UiWeight.Black);
        UiFonts.Draw(Words.Date(day.Date), textX, page.Y + 90, 48, Ink, UiWeight.Black);

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
    public static void DrawTrainerCard(int sw, int sh, TrainerCardInfo info, Texture2D? portrait, float appear = 1f, bool back = false, float flip = 1f) =>
        DrawTrainerCard(sw, sh, info, portrait, appear, new CardSide(back ? CardPage.Back : CardPage.Front),
            new CardSide(back ? CardPage.Front : CardPage.Back), flip);

    /// <summary>The card on any of its pages (plan 18 · W2): <paramref name="shown"/> as it turns from <paramref name="from"/>.</summary>
    public static void DrawTrainerCard(int sw, int sh, TrainerCardInfo info, Texture2D? portrait, float appear, CardSide shown, CardSide from, float flip)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("card.title"));
        if (info.RegionPages.Count > 0 || info.Run != null)
            Hints(sw - Margin, 44, Hint("shared.keys.leftRight", "card.page"), Hint("shared.keys.z", "shared.hints.turnOver"), Hint("shared.keys.esc", "shared.hints.back"));
        else Hints(sw - Margin, 44, Hint("shared.keys.z", "shared.hints.turnOver"), Hint("shared.keys.esc", "shared.hints.back"));

        float rise = (1f - UiMotion.EaseOut(appear)) * 60f;
        var card = new Rectangle(200, 150 + rise, sw - 400, 840);

        // Turning over: the card narrows to its edge showing the side it had, then opens on the other
        bool turning = flip < 1f;
        var side = turning && flip < 0.5f ? from : shown;
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
        string id = Text.Of("card.id", ("id", TrainerCardScreen.FormatId(info.TrainerId)));
        float idX = band.X + band.Width - 44 - UiFonts.Measure(id, 32, UiWeight.Black);
        UiFonts.DrawCentered(id, idX, bandY, 32, Color.White, UiWeight.Black);
        for (int k = 0; k < 5; k++)
            CardStar(new Vector2(idX - 70 - (4 - k) * 52, bandY), 20, k < info.Stars, Darker(colour, 0.5f), Lighter(colour, 0.32f));

        switch (side.Page)
        {
            case CardPage.Run when info.Run != null:
                CardRun(card, info.Run);
                break;
            case CardPage.Back:
                CardBack(card, info, colour);
                break;
            case CardPage.Region when side.Region >= 0 && side.Region < info.RegionPages.Count:
                CardRegionPage(card, info.RegionPages[side.Region]);
                break;
            default:
                CardFront(card, info, portrait, colour);
                break;
        }

        if (turning) Rlgl.PopMatrix();
    }

    private static void CardFront(Rectangle card, TrainerCardInfo info, Texture2D? portrait, Color colour)
    {
        // ---- Records
        bool dex = info.Colour != TrainerCardRules.CardColour.NoPokedex;
        float x = card.X + 72, y = card.Y + 176;
        Label(Text.Of("card.money"), x, y);
        Money(x, y + 24 + 40 * 0.56f, info.Money, 40, Ink);
        Field(Text.Of("card.timePlayed"), TitleScreen.FormatPlayTime(info.PlayTimeSeconds), x + 440, y);
        Field(Text.Of("card.caught"), dex ? info.Caught.ToString() : "—", x, y + 124);
        Field(Text.Of("card.seen"), dex ? info.Seen.ToString() : "—", x + 440, y + 124);
        Field(Text.Of("card.score"), info.Score.ToString("N0", CultureInfo.InvariantCulture), x, y + 248);
        // What to do next (plan 12 · Q14), in the free corner between the score and the portrait's plate
        if (info.NextObjective.Length > 0)
            NextLine(new Rectangle(x + 440, y + 252, card.Width - 948, 4 * NextLineHeight + 30), info.NextObjective);

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
        // The badges of the region the player is in (plan 18 · W2), at the eight places' spacing from the left
        var here = info.Here;
        Label(Text.Of("card.badges", ("count", here.Count), ("of", here.BadgeNames.Count)), card.X + 72, by + 24);
        if (info.RegionPages.Count > 0 || info.Run != null)
        {
            string region = RegionName(here);
            UiFonts.Draw(region, card.X + card.Width - 72 - UiFonts.Measure(region, 26, UiWeight.Black), by + 24, 26, Muted, UiWeight.Black);
        }
        float step = (card.Width - 144 - 88) / 7f;
        for (int b = 0; b < here.BadgeNames.Count && b < 8; b++)
        {
            bool earned = here.Has(b);
            var c = new Vector2(card.X + 72 + 44 + b * step, by + 116);
            Badge(c, 44, b, earned, here.BadgeNames[b]);
            string name = Words.Badge(here.BadgeNames[b]);
            UiFonts.DrawCentered(name, c.X - UiFonts.Measure(name, 22, UiWeight.Black) / 2f, c.Y + 76, 22, earned ? Ink : Muted, UiWeight.Black);
        }
    }

    /// <summary>A region's name as the card writes it: in capitals.</summary>
    private static string RegionName(CardRegion region)
    {
        int generation = region.Generation;
        return Text.Of($"dexText.region.{generation}").ToUpperInvariant();
    }

    /// <summary>
    /// A region's page of the card (plan 18 · W2, decision 1): its name and how many of its badges are won, CHAMPION once
    /// its Hall of Fame is entered, and its medallions in a row with their names.
    /// </summary>
    private static void CardRegionPage(Rectangle card, CardRegion region)
    {
        float x = card.X + 72, right = card.X + card.Width - 72, top = card.Y + 176;
        string name = RegionName(region);
        UiFonts.Draw(name, x, top, 64, Ink, UiWeight.Black);
        float after = x + UiFonts.Measure(name, 64, UiWeight.Black) + 28;
        string count = Text.Of("card.outOf", ("count", region.Count), ("of", region.BadgeNames.Count));
        UiFonts.Draw(count, after, top + 22, 36, Muted, UiWeight.Black);
        if (region.Champion)
        {
            string champion = Text.Of("card.champion");
            Tag(right - UiFonts.Measure(champion, 44 * 0.58f, UiWeight.Black) - 44 * 0.9f, top + 18, champion, Gold, 44);
        }
        UiShapes.Fill(new Rectangle(x, top + 110, right - x, 3), 1.5f, Rule);

        int n = Math.Min(region.BadgeNames.Count, 8);
        if (n == 0) UiFonts.Draw(Text.Of("card.noBadges"), x, top + 150, 30, Muted, UiWeight.ExtraBold);
        float step = (right - x - 112) / 7f;
        for (int b = 0; b < n; b++)
        {
            bool earned = region.Has(b);
            var c = new Vector2(x + 56 + b * step, top + 230);
            Badge(c, 56, b, earned, region.BadgeNames[b]);
            string badge = Words.Badge(region.BadgeNames[b]);
            UiFonts.DrawCentered(badge, c.X - UiFonts.Measure(badge, 24, UiWeight.Black) / 2f, c.Y + 96, 24, earned ? Ink : Muted, UiWeight.Black);
        }

        // The region's own Pokédex, once the player has it (plan 18 · W3)
        if (region.Pokedex is not { } dex) return;
        float y = top + 420;
        UiShapes.Fill(new Rectangle(x, y, right - x, 3), 1.5f, Rule);
        Field(Text.Of("card.regionSeen"), Text.Of("card.outOf", ("count", dex.Seen), ("of", dex.Of)), x, y + 40);
        Field(Text.Of("card.regionCaught"), Text.Of("card.outOf", ("count", dex.Caught), ("of", dex.Of)), x + 440, y + 40);
    }

    /// <summary>
    /// The page for how the game is played (plan 13 · V1; only a game that isn't Normal has one): the preset's name and
    /// every choice that differs from Platinum's, two columns of a caption and its value.
    /// </summary>
    private static void CardRun(Rectangle card, RunRules run)
    {
        float x = card.X + 72, right = card.X + card.Width - 72, top = card.Y + 176;
        UiFonts.Draw(Text.Of($"run.preset.{run.PresetKey}"), x, top, 64, Ink, UiWeight.Black);
        UiFonts.Draw(Text.Of("card.runSub"), x, top + 82, 28, Muted, UiWeight.ExtraBold);
        UiShapes.Fill(new Rectangle(x, top + 128, right - x, 3), 1.5f, Rule);
        var shown = RunPanel.Showing(run);
        var plain = RunPanel.Showing(RunRules.Normal);
        int n = 0;
        foreach (var row in shown.Rows)
        {
            if (row is RunRow.Preset or RunRow.Start) continue;
            string value = shown.ValueText(row);
            if (value == plain.ValueText(row)) continue;
            float cx = x + (n % 2) * 640, cy = top + 160 + (n / 2) * 120;
            Field(RunRowName(row).ToUpperInvariant(), value, cx, cy, 36);
            n++;
        }
    }

    private static void CardBack(Rectangle card, TrainerCardInfo info, Color colour)
    {
        float x = card.X + 72, right = card.X + card.Width - 72, top = card.Y + 166;
        const float rowH = 88;
        var rows = new (string Caption, string Value)[]
        {
            (Text.Of("card.debut"), info.HallOfFameDebut is { } debut
                ? Text.Of("card.debutWhen", ("date", Words.Date(debut)), ("time", $"{debut.Hour:D2}:{debut.Minute:D2}")) : "—"),
            (Text.Of("card.linkBattles"), Text.Of("card.linkBattlesValue", ("won", info.LinkWins), ("lost", info.LinkLosses))),
            (Text.Of("card.linkTrades"), info.LinkTrades.ToString()),
            (Text.Of("card.contests"), Text.Of("card.contestsValue", ("won", info.ContestsWon), ("entered", info.ContestsEntered))),
            (Text.Of("card.started"), info.Started is { } day ? Words.Date(day) : "—")
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
        Label(Text.Of("card.stars"), x, by + 26);
        for (int k = 0; k < 5; k++)
            CardStar(new Vector2(x + 36 + k * 88, by + 118), 32, k < info.Stars, Darker(colour, 0.5f), Rule);
        string count = $"{info.Stars} / 5";
        UiFonts.DrawCentered(count, x + 36 + 4 * 88 + 72, by + 118, 40, Ink, UiWeight.Black);
        string note = Text.Of("card.starsNote");
        UiFonts.DrawCentered(note, right - UiFonts.Measure(note, 26, UiWeight.ExtraBold), by + 118, 26, Muted, UiWeight.ExtraBold);
    }

    // ------------------------------------------------------------------ saving

    /// <summary>
    /// Saving: the save as it will be, the question under it with SAVE and CANCEL, and once it is done, a line
    /// saying so. <paramref name="shown"/> slides the panel up into place. The slot the game is saved in is named
    /// after the heading and in the question (plan 12 · Q2).
    /// </summary>
    public static void DrawSave(int sw, int sh, SaveData save, int slot, int answer, bool saved, float shown, string next = "")
    {
        Dim(sw, sh, (int)(150 * shown));
        // What to do next (plan 12 · Q14) takes a band of its own under the summary, and the panel grows by it
        float band = next.Length > 0 ? NextPanelHeight(next, 1080 - 96) - 12 : 0;
        var r = new Rectangle(sw / 2f - 540, 140 + (1f - shown) * 60f, 1080, 780 + band);
        Panel(r, 36);
        SaveSummary(new Rectangle(r.X, r.Y, r.Width, SaveSummaryHeight), save, Text.Of("saving.heading"), slot);
        if (band > 0) NextLine(new Rectangle(r.X + 48, r.Y + SaveSummaryHeight - 8, r.Width - 96, band), next);
        UiShapes.Fill(new Rectangle(r.X + 48, r.Y + SaveSummaryHeight + 12 + band, r.Width - 96, 3), 1.5f, Rule);

        float cy = r.Y + SaveSummaryHeight + 82 + band;
        if (saved)
        {
            float tw = Tag(r.X + 56, cy + 34, Text.Of("saving.saved"), Green, 52);
            UiFonts.DrawCentered(Text.Of("saving.savedLine", ("name", save.PlayerName)), r.X + 56 + tw + 28, cy + 60, 40, Ink, UiWeight.ExtraBold);
            return;
        }

        UiFonts.DrawCentered(SaveScreen.Question(slot), r.X + 56, cy, 44, Ink, UiWeight.Black);
        const float gap = 32;
        float bw = (r.Width - 112 - gap) / 2f;
        Button(new Rectangle(r.X + 56, r.Y + r.Height - 56 - 84, bw, 84), 42, Green, SaveScreen.Answers[0], 32, answer == 0);
        Button(new Rectangle(r.X + 56 + bw + gap, r.Y + r.Height - 56 - 84, bw, 84), 42, Blue, SaveScreen.Answers[1], 32, answer == 1);
    }

    // ------------------------------------------------------------------ the Vs. Recorder (plan 08 · P11)

    /// <summary>
    /// The Vs. Recorder's list: the recordings as list rows on the left (the first foe's icon, whom it was against,
    /// the day), the one under the cursor told on the right, and a file that can't be played as a Muted row with its
    /// notice.
    /// </summary>
    public static void DrawReplayList(int sw, int sh, ReplayListScreen screen, float appear = 1f)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("replays.title"));
        Hints(sw - Margin, 44, Hint("shared.keys.upDown", "shared.hints.choose"), Hint("shared.keys.z", "replays.watch"),
            Hint("shared.keys.esc", "shared.hints.back"));
        float rise = (1f - UiMotion.EaseOut(appear)) * 60f;
        var entries = screen.Entries;
        if (entries.Count == 0)
        {
            var whole = new Rectangle(Margin, ContentTop + rise, sw - Margin * 2, ContentBottom - ContentTop);
            Panel(whole, 40);
            EmptyNote(whole, Text.Of("replays.empty"));
            return;
        }

        // ---- The list
        var list = new Rectangle(Margin, ContentTop + rise, 1040, ContentBottom - ContentTop);
        Panel(list, 40);
        int visible = ReplayListScreen.VisibleRows;
        bool scrolls = entries.Count > visible;
        for (int row = 0; row < visible && screen.FirstRow + row < entries.Count; row++)
        {
            int index = screen.FirstRow + row;
            var entry = entries[index];
            bool selected = index == screen.Cursor;
            var r = RowRect(list, row, scrolls: scrolls);
            if (entry.File is { } file && entry.Playable)
            {
                var ink = ListRow(r, selected, Fitted(file.Foe, r.Width - 104 - 260, 34));
                if (ReplayLead(file) is { } lead) RowIcon(r, PixelArtGenerator.GetPokemonIcon(lead), 1, selected, hop: Hop(selected));
                if (file.Day is { } day) RowValue(r, Words.ShortDate(day), ink, 26);
            }
            else
            {
                var ink = ListRow(r, selected, Fitted(entry.FileName, r.Width - 104 - 260, 34), enabled: false);
                UiShapes.Circle(new Vector2(r.X + 52, r.Y + r.Height / 2f), 34, selected ? Color.White : Disc);
                RowValue(r, Text.Of("replays.unreadable"), selected ? ink : Red, 26);
            }
        }
        ScrollBar(list, screen.FirstRow, visible, entries.Count);

        // ---- The one under the cursor
        var detail = new Rectangle(list.X + list.Width + Gutter, ContentTop + rise, sw - Margin * 2 - list.Width - Gutter, ContentBottom - ContentTop);
        Panel(detail, 40);
        if (screen.Selected is not { } shown) return;
        float x = detail.X + 44, wide = detail.Width - 88, y = detail.Y + 48;
        if (shown.File is not { } chosen || !shown.Playable)
        {
            Label(Text.Of("replays.file"), x, y);
            UiFonts.Draw(Fitted(shown.FileName, wide, 36), x, y + 30, 36, Ink, UiWeight.Black);
            Tag(x, y + 90, Text.Of("replays.unreadable"), Red, 44);
            UiShapes.Fill(new Rectangle(x, y + 162, wide, 3), 1.5f, Rule);
            DrawWrapped(shown.Notice, x, y + 194, wide, 30, Ink, 42);
            return;
        }

        UiFonts.Draw(Fitted(chosen.Foe, wide, 44), x, y, 44, Ink, UiWeight.Black);
        if (chosen.Day is { } fought) UiFonts.Draw(Words.Date(fought), x, y + 60, 28, Muted, UiWeight.ExtraBold);
        var (word, colour) = ReplayResult(chosen.Result);
        float tagW = Tag(x, y + 112, word, colour, 44);
        string format = Text.Of(chosen.Format == BattleFormat.Double ? "replays.double" : "replays.single");
        string turns = chosen.Turns == 1 ? Text.Of("replays.oneTurn") : Text.Of("replays.turns", ("turns", chosen.Turns));
        UiFonts.DrawCentered(format + "  ·  " + turns, x + tagW + 20, y + 134, 28, Ink, UiWeight.ExtraBold);
        UiShapes.Fill(new Rectangle(x, y + 190, wide, 3), 1.5f, Rule);

        float teamY = y + 224;
        Label(Text.Of("replays.theirs"), x, teamY);
        ReplayTeam(chosen.Trainers.Count > 0 ? chosen.Trainers.SelectMany(t => t.Party) : chosen.Wild, x, teamY + 32, wide);
        Label(Text.Of("replays.yours"), x, teamY + 220);
        ReplayTeam(chosen.Player, x, teamY + 252, wide);
    }

    /// <summary>The first foe a recording shows: the trainer's lead, or the wild Pokémon.</summary>
    private static string? ReplayLead(BattleRecordFile file)
    {
        var first = file.Trainers.Count > 0 ? file.Trainers[0].Party.FirstOrDefault() : file.Wild.FirstOrDefault();
        return first?.Pokemon is { } p ? p.Form ?? p.SpeciesName : null;
    }

    /// <summary>A team of a recording as its icons at twice their size, in a row on pale discs.</summary>
    private static void ReplayTeam(IEnumerable<RecordedPokemon> team, float x, float y, float wide)
    {
        const float step = 116, radius = 52;
        int i = 0;
        foreach (var member in team)
        {
            if (i * step + radius * 2 > wide) break;
            var c = new Vector2(x + radius + i * step, y + radius + 20);
            UiShapes.Circle(c, radius, Disc);
            var icon = member.Pokemon.IsEgg ? PixelArtGenerator.EggIcon : PixelArtGenerator.GetPokemonIcon(PokemonLook.Of(member.Pokemon.Form ?? member.Pokemon.SpeciesName, member.Pokemon.IsShiny));
            PixelArt(icon, c, 2);
            i++;
        }
    }

    /// <summary>How a recorded battle ended, as its tag says it.</summary>
    private static (string Word, Color Colour) ReplayResult(BattleResult result) => result switch
    {
        BattleResult.PlayerVictory => (Text.Of("replays.result.won"), Green),
        BattleResult.PlayerDefeat => (Text.Of("replays.result.lost"), Red),
        BattleResult.EnemyCaught => (Text.Of("replays.result.caught"), Frame),
        BattleResult.PlayerRan => (Text.Of("replays.result.ran"), Frame),
        BattleResult.EnemyFled => (Text.Of("replays.result.fled"), Frame),
        _ => (Text.Of("replays.result.none"), Frame)
    };

    /// <summary>A text cut with an ellipsis where it is wider than the room it has.</summary>
    private static string Fitted(string text, float room, float size)
    {
        if (UiFonts.Measure(text, size, UiWeight.Black) <= room) return text;
        while (text.Length > 1 && UiFonts.Measure(text + "…", size, UiWeight.Black) > room) text = text[..^1];
        return text.TrimEnd() + "…";
    }

    /// <summary>
    /// Over a battle watched: a Blue REPLAY pill with whom it was against beside it, a Gold ×3 while Run is held,
    /// and the two hints under it, at the top right where nothing of the battle stands.
    /// </summary>
    public static void ReplayTag(int sw, BattleRecordFile file, bool fast)
    {
        const float pill = 44;
        float right = sw - 56, top = 52;
        string foe = Fitted(file.Foe, 520, 28);
        float foeW = UiFonts.Measure(foe, 28, UiWeight.Bold);
        string banner = Text.Of("replays.banner"), faster = Text.Of("replays.fast");
        float bannerW = UiFonts.Measure(banner, pill * 0.58f, UiWeight.Black) + pill * 0.9f;
        float fastW = fast ? UiFonts.Measure(faster, pill * 0.58f, UiWeight.Black) + pill * 0.9f + 12 : 0f;
        float width = bannerW + 16 + foeW + 28 + fastW;
        var plate = new Rectangle(right - width - 12, top - 8, width + 24, pill + 16);
        UiShapes.Fill(plate, plate.Height / 2f, new Color(12, 20, 40, 170));
        float x = plate.X + 12;
        x += Tag(x, top, banner, Blue, pill) + 16;
        UiFonts.DrawCentered(foe, x, top + pill / 2f, 28, Color.White, UiWeight.Bold);
        x += foeW + 28;
        if (fast) Tag(x, top, faster, Gold, pill);
        Hints(right, top + pill + 28, Hint("shared.keys.enter", "shared.hints.leave"), Hint("shared.keys.shift", "replays.hintFast"));
    }
}
