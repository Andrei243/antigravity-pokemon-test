using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The Battle Castle on screen (plan 06 · R18c; style guide, "The Battle Hall and the Battle Castle"): the valet's
/// two counters, and the count of Castle Points that stands over the field while they are spoken of.
/// </summary>
internal static partial class ModernUi
{
    /// <summary>
    /// A counter as it stands: the three Pokémon it is about in a row (one's own with their HP, PP and what they
    /// hold; the trainer's as far as they have been paid for), under them the Castle Points and the counter's ranks
    /// with the way out, and beside them the menu that is up with each thing's price, the list of what is lent, or a
    /// Pokémon's moves; a prompt over it all for a yes or a no and for what came of it.
    /// </summary>
    public static void DrawCastle(int sw, int sh, CastleScreen screen, float appear = 1f)
    {
        if (screen.Challenge is not { } challenge || screen.Records is not { } records) return;
        Backdrop(sw, sh);
        ScreenTitle(screen.Own ? "BATTLE CASTLE · YOUR POKÉMON" : "BATTLE CASTLE · THE NEXT TRAINER");
        Hints(sw - Margin, 44, ("Z", "Choose"), ("Esc", "Back"));
        float rise = (1f - UiMotion.EaseOut(appear)) * 60f;

        const float gap = 24, cardW = 368, cardH = 520, sideX = Margin + 3 * cardW + 2 * gap + Gutter;
        bool browsing = screen.Step == CastleStep.Browse;
        var shown = screen.Shown;
        for (int i = 0; i < BattleCastleChallenge.TeamSize; i++)
        {
            var r = new Rectangle(Margin + i * (cardW + gap), ContentTop + rise, cardW, cardH);
            bool under = screen.Cursor == i;
            if (i >= shown.Count) { EmptySlot(r, 30); continue; }
            if (screen.Own) OwnCastleCard(r, shown[i], under);
            else FoeCastleCard(r, challenge, shown[i], i, under);
        }

        // Under the cards: the Castle Points, the counter's ranks, and the way out
        var foot = new Rectangle(Margin, ContentTop + cardH + gap + rise, 3 * cardW + 2 * gap, ContentBottom - ContentTop - cardH - gap);
        Panel(foot, 34);
        Label("CASTLE POINTS", foot.X + 44, foot.Y + 36);
        UiFonts.Draw($"{screen.Points:N0} CP", foot.X + 44, foot.Y + 66, 64, Ink, UiWeight.Black);
        float rx = foot.X + 420;
        foreach (var rank in screen.Own ? new[] { CastleRank.Healing, CastleRank.Rentals } : new[] { CastleRank.Summary })
        {
            string name = rank switch { CastleRank.Healing => "HEALING", CastleRank.Rentals => "RENTALS", _ => "INFO" };
            Label(name + " RANK", rx, foot.Y + 36);
            int held = challenge.Rank(records, rank), most = BattleCastleChallenge.HighestRank[(int)rank];
            for (int n = 0; n < most; n++)
            {
                var c = new Vector2(rx + 22 + n * 56, foot.Y + 104);
                if (n < held) UiShapes.Circle(c, 22, Gold);
                else UiShapes.Ring(c, 22, 4, Rule);
                string digit = (n + 1).ToString();
                UiFonts.DrawCentered(digit, c.X - UiFonts.Measure(digit, 26, UiWeight.Black) / 2f, c.Y, 26, n < held ? Color.White : Muted, UiWeight.Black);
            }
            rx += 230;
        }
        Button(new Rectangle(foot.X + foot.Width - 244, foot.Y + foot.Height / 2f - 44, 200, 88), 44, Muted, "EXIT", 32, browsing && screen.Cursor == CastleScreen.ExitPlace);
        string battle = $"Battle {challenge.Battle + 1} of {BattleCastleChallenge.BattlesPerRound}";
        UiFonts.DrawCentered(battle, foot.X + 44, foot.Y + foot.Height - 44, 26, Muted, UiWeight.Black);

        // Beside them: the menu, the list, the moves, or what the counter is for
        var side = new Rectangle(sideX, ContentTop + rise, sw - Margin - sideX, ContentBottom - ContentTop);
        Panel(side, 34);
        switch (screen.Step == CastleStep.Ask || screen.Step == CastleStep.Note ? CastleStep.Menu : screen.Step)
        {
            case CastleStep.List:
                CastleList(side, screen);
                break;
            case CastleStep.Moves when screen.Subject is { } subject:
                CastleMoves(side, screen, subject);
                break;
            case CastleStep.Summary when screen.Subject is { } subject:
                CastleStats(side, screen, subject);
                break;
            case CastleStep.Menu when screen.Options.Count > 0 && (screen.Step == CastleStep.Menu || !browsing):
                CastleMenu(side, screen);
                break;
            default:
                UiFonts.Draw(screen.Own ? "THE VALET'S COUNTER" : "WHAT THE VALET KNOWS", side.X + 40, side.Y + 36, 36, Ink, UiWeight.Black);
                DrawWrapped(screen.Own
                        ? "Nothing is restored between battles here. Castle Points buy HP and PP back, and berries and items for your Pokémon to hold. A higher rank opens more of each."
                        : "For Castle Points the valet tells which Pokémon come next, moves their levels up or down by five, and tells their stats and moves. A Pokémon raised five levels is worth more Castle Points when it is beaten.",
                    side.X + 40, side.Y + 104, side.Width - 80, 28, Muted, 40, UiWeight.ExtraBold);
                break;
        }

        float asked = Math.Clamp(screen.StepAge / 0.2f, 0f, 1f);
        if (screen.Step == CastleStep.Ask) Prompt(sw, sh, screen.Title, screen.Line, new[] { ("YES", Green), ("NO", Red) }, screen.MenuCursor, asked);
        else if (screen.Step == CastleStep.Note) Prompt(sw, sh, screen.Title, screen.Line, new[] { ("OK", Blue) }, 0, asked);
    }

    private static void CastleSprite(Rectangle r, Pokemon p)
    {
        var c = new Vector2(r.X + r.Width / 2f, r.Y + 150);
        UiShapes.Circle(c, 118, new Color(226, 234, 246, 255));
        var sprite = PixelArtGenerator.SpriteOf(p);
        int scale = sprite.Height * 2 <= 280 ? 2 : 1;
        Raylib.DrawTexturePro(sprite, new Rectangle(0, 0, sprite.Width, sprite.Height),
            new Rectangle(MathF.Round(c.X - sprite.Width * scale / 2f), MathF.Round(c.Y - sprite.Height * scale / 2f), sprite.Width * scale, sprite.Height * scale),
            Vector2.Zero, 0f, Color.White);
    }

    // One of one's own three: its picture, name and level, its HP, the PP its moves have left between them, and what it holds
    private static void OwnCastleCard(Rectangle r, Pokemon p, bool selected)
    {
        Card(r, 30, selected);
        CastleSprite(r, p);
        float x = r.X + 32, w = r.Width - 64;
        NameWithGender(p, x, r.Y + 304, 36);
        LevelRight(r.X + r.Width - 32, r.Y + 304, p.Level);
        HpBar(x, r.Y + 340, w, 22, p.MaxHP > 0 ? p.CurrentHP / (float)p.MaxHP : 0f);
        Label("HP", x, r.Y + 376);
        string hp = $"{p.CurrentHP} / {p.MaxHP}";
        UiFonts.Draw(hp, r.X + r.Width - 32 - UiFonts.Measure(hp, 28, UiWeight.Black), r.Y + 370, 28, Ink, UiWeight.Black);
        Label("PP", x, r.Y + 420);
        string pp = $"{p.Moves.Sum(m => m.CurrentPP)} / {p.Moves.Sum(m => m.MaxPP)}";
        UiFonts.Draw(pp, r.X + r.Width - 32 - UiFonts.Measure(pp, 28, UiWeight.Black), r.Y + 414, 28, Ink, UiWeight.Black);
        Label("HOLDING", x, r.Y + 464);
        string item = p.HeldItem?.Name ?? "Nothing";
        UiFonts.Draw(item, r.X + r.Width - 32 - UiFonts.Measure(item, 28, UiWeight.Black), r.Y + 458, 28, p.HeldItem != null ? Ink : Muted, UiWeight.Black);
    }

    private static void LevelRight(float rightX, float centerY, int level)
    {
        string text = $"Lv. {level}";
        UiFonts.DrawCentered(text, rightX - UiFonts.Measure(text, 28, UiWeight.Black), centerY, 28, Muted, UiWeight.Black);
    }

    // One of the trainer's three: nobody until it is identified, with its level as the counter has moved it and what has been told of it
    private static void FoeCastleCard(Rectangle r, BattleCastleChallenge challenge, Pokemon p, int place, bool selected)
    {
        Card(r, 30, selected);
        bool known = challenge.Identified[place];
        float x = r.X + 32;
        if (known)
        {
            CastleSprite(r, p);
            NameWithGender(p, x, r.Y + 304, 36);
        }
        else
        {
            var c = new Vector2(r.X + r.Width / 2f, r.Y + 150);
            UiShapes.Circle(c, 118, new Color(226, 234, 246, 255));
            UiShapes.Ring(c, 86, 10, Rule);
            UiFonts.DrawCentered("?", c.X - UiFonts.Measure("?", 110, UiWeight.Black) / 2f, c.Y, 110, Muted, UiWeight.Black);
            UiFonts.DrawCentered("? ? ?", x, r.Y + 304, 36, Muted, UiWeight.Black);
        }
        LevelRight(r.X + r.Width - 32, r.Y + 304, p.Level);
        float ty = r.Y + 348;
        var level = challenge.Levels[place];
        if (level != CastleLevel.Same)
        {
            Tag(x, ty, level == CastleLevel.Up ? "FIVE LEVELS UP" : "FIVE LEVELS DOWN", level == CastleLevel.Up ? Selection : Blue, 36);
            ty += 48;
        }
        if (challenge.StatsShown[place]) { Tag(x, ty, "STATS TOLD", Green, 36); ty += 48; }
        if (challenge.MovesShown[place]) Tag(x, ty, "MOVES TOLD", Green, 36);
        if (!known && level == CastleLevel.Same && !challenge.StatsShown[place] && !challenge.MovesShown[place])
            UiFonts.Draw("Nothing told yet", x, ty + 4, 26, Muted, UiWeight.ExtraBold);
    }

    // The menu that is up: whose it is, and each thing with what it costs, or the rank it waits for
    private static void CastleMenu(Rectangle side, CastleScreen screen)
    {
        var challenge = screen.Challenge!;
        var records = screen.Records!;
        string head = screen.Opened switch
        {
            CastleOption.Heal => "HEAL",
            CastleOption.Rental => "RENTAL",
            CastleOption.Level => "LEVEL",
            CastleOption.Info => "INFO",
            _ => screen.NameOf(screen.Cursor).ToUpperInvariant()
        };
        UiFonts.Draw(head, side.X + 40, side.Y + 36, 40, Ink, UiWeight.Black);
        bool live = screen.Step == CastleStep.Menu;
        float y = side.Y + 116;
        for (int i = 0; i < screen.Options.Count; i++)
        {
            var option = screen.Options[i];
            var r = new Rectangle(side.X + 32, y, side.Width - 64, 92);
            int rankHeld = challenge.Rank(records, CastleScreen.RankOf(screen.Opened));
            bool shut = screen.RankNeeded(option) > rankHeld
                || option == CastleOption.RankUp && challenge.RankPrice(records, CastleScreen.RankOf(screen.Opened)) == 0;
            Color colour = option switch
            {
                CastleOption.Cancel => Muted,
                CastleOption.RankUp => Gold,
                _ when shut => Muted,
                _ => Blue
            };
            Button(r, 46, colour, "", 30, live && i == screen.MenuCursor);
            UiFonts.DrawCentered(CastleScreen.LabelOf(option), r.X + 36, r.Y + r.Height / 2f, 32, Color.White, UiWeight.Black);
            int price = screen.PriceOf(option);
            string right = shut && option != CastleOption.RankUp ? $"RANK {screen.RankNeeded(option)}" : shut ? "HIGHEST" : price > 0 ? $"{price} CP" : "";
            if (right.Length > 0) UiFonts.DrawCentered(right, r.X + r.Width - 36 - UiFonts.Measure(right, 28, UiWeight.Black), r.Y + r.Height / 2f, 28, Color.White, UiWeight.Black);
            y += 108;
        }
    }

    // What is lent: a window of rows with their prices, the one under the cursor told of at the foot
    private static void CastleList(Rectangle side, CastleScreen screen)
    {
        bool berries = screen.Listed.Count > 0 && Data.FrontierDatabase.Castle.Berries.Contains(screen.Listed[0]);
        UiFonts.Draw(berries ? "BERRIES" : "ITEMS", side.X + 40, side.Y + 36, 40, Ink, UiWeight.Black);
        const float rowH = 76;
        float top = side.Y + 108;
        for (int n = 0; n < CastleScreen.VisibleRows && screen.ListTop + n < screen.Listed.Count; n++)
        {
            int index = screen.ListTop + n;
            var lent = screen.Listed[index];
            var r = new Rectangle(side.X + 28, top + n * (rowH + 8), side.Width - 72, rowH);
            var ink = ListRow(r, index == screen.MenuCursor, lent.Item, nameX: 28, size: 30);
            RowValue(r, $"{lent.Price} CP", ink, 28);
        }
        // Where the window is, when there is more than fits: a track at the panel's right edge with a thumb
        if (screen.Listed.Count > CastleScreen.VisibleRows)
        {
            float trackH = CastleScreen.VisibleRows * (rowH + 8) - 8;
            var track = new Rectangle(side.X + side.Width - 32, top, 10, trackH);
            UiShapes.Fill(track, 5, Rule);
            float thumbH = trackH * CastleScreen.VisibleRows / screen.Listed.Count;
            float thumbY = top + (trackH - thumbH) * screen.ListTop / (screen.Listed.Count - CastleScreen.VisibleRows);
            UiShapes.Fill(new Rectangle(track.X, thumbY, 10, thumbH), 5, Muted);
        }
        if (screen.MenuCursor < screen.Listed.Count && Data.ItemDatabase.Get(screen.Listed[screen.MenuCursor].Item) is { } item)
            DrawWrapped(item.Description, side.X + 40, top + CastleScreen.VisibleRows * (rowH + 8) + 12, side.Width - 80, 24, Muted, 32, UiWeight.ExtraBold);
    }

    // A Pokémon's moves: each with its type and the PP it has left
    private static void CastleMoves(Rectangle side, CastleScreen screen, Pokemon p)
    {
        UiFonts.Draw(screen.NameOf(screen.Cursor).ToUpperInvariant() + "'S MOVES", side.X + 40, side.Y + 36, 36, Ink, UiWeight.Black);
        float y = side.Y + 116;
        foreach (var move in p.Moves)
        {
            var r = new Rectangle(side.X + 32, y, side.Width - 64, 108);
            Card(r, 26, false);
            UiFonts.DrawCentered(move.Name, r.X + 28, r.Y + 36, 32, Ink, UiWeight.Black);
            TypePill(r.X + 28, r.Y + 62, move.Type, 32);
            string pp = $"PP {move.CurrentPP} / {move.MaxPP}";
            UiFonts.DrawCentered(pp, r.X + r.Width - 28 - UiFonts.Measure(pp, 28, UiWeight.Black), r.Y + r.Height / 2f, 28, move.CurrentPP == 0 ? Red : Ink, UiWeight.Black);
            y += 124;
        }
    }

    // One of the trainer's Pokémon as two Castle Points tell it: its six stats, its nature, its ability and what it
    // holds. Its moves are another purchase, so they are not here
    private static void CastleStats(Rectangle side, CastleScreen screen, Pokemon p)
    {
        UiFonts.Draw(screen.NameOf(screen.Cursor).ToUpperInvariant() + "'S STATS", side.X + 40, side.Y + 36, 36, Ink, UiWeight.Black);
        float y = side.Y + 112;
        foreach (var (name, value) in new[] { ("HP", p.MaxHP), ("Attack", p.Attack), ("Defense", p.Defense), ("Sp. Atk", p.SpAttack), ("Sp. Def", p.SpDefense), ("Speed", p.Speed) })
        {
            var r = new Rectangle(side.X + 32, y, side.Width - 64, 64);
            UiShapes.Fill(r, 20, Rule with { A = 70 });
            UiFonts.DrawCentered(name, r.X + 28, r.Y + r.Height / 2f, 30, Ink, UiWeight.ExtraBold);
            string number = value.ToString();
            UiFonts.DrawCentered(number, r.X + r.Width - 28 - UiFonts.Measure(number, 32, UiWeight.Black), r.Y + r.Height / 2f, 32, Ink, UiWeight.Black);
            y += 74;
        }
        y += 14;
        foreach (var (caption, told) in new[] { ("NATURE", p.Nature.ToString()), ("ABILITY", p.AbilityName ?? "None"), ("HOLDING", p.HeldItem?.Name ?? "Nothing") })
        {
            Label(caption, side.X + 40, y);
            UiFonts.Draw(told, side.X + 40, y + 24, 32, Ink, UiWeight.Black);
            y += 86;
        }
    }

    /// <summary>The Castle Points held, standing over the field while the valet speaks of them (where the Battle Points' count stands elsewhere in the Frontier).</summary>
    public static void CastlePointsCount(int points)
    {
        var r = new Rectangle(32, 32, 300, 96);
        Panel(r, 24);
        Label("CASTLE POINTS", r.X + 28, r.Y + 18);
        UiFonts.Draw($"{points:N0} CP", r.X + 28, r.Y + 44, 36, Ink, UiWeight.Black);
    }
}
