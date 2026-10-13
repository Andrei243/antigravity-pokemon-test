using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The Battle Factory on screen (plan 06 · R18b; style guide, "The Battle Factory"): the six rentals on offer with
/// the three chosen in their order, and a trade's two rows, one's own three over the beaten trainer's.
/// </summary>
internal static partial class ModernUi
{
    /// <summary>
    /// The screen as it stands: a card for each Pokémon (its sprite, name and gender, as the original shows no more
    /// until the summary is asked for), the place it was chosen at, the question of the moment along the foot with
    /// the menu of the Pokémon under the cursor, and a prompt over it all for a yes or a no.
    /// </summary>
    public static void DrawFactory(int sw, int sh, FactoryScreen screen, float appear = 1f)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("factoryScreen.title"));
        bool trade = screen.Mode == FactoryScreenMode.Trade;
        if (trade) Hints(sw - Margin, 44, Hint("shared.keys.z", "shared.hints.choose"), Hint("shared.keys.esc", "shared.hints.leave"));
        else Hints(sw - Margin, 44, Hint("shared.keys.z", "shared.hints.choose"), Hint("shared.keys.esc", "factoryScreen.takeBack"));
        float rise = (1f - UiMotion.EaseOut(appear)) * 60f;

        if (trade) TradeRows(sw, screen, rise);
        else RentalRow(sw, screen, rise);

        // The foot: what is asked, and the menu of the Pokémon under the cursor
        var foot = new Rectangle(Margin, 892 + rise, sw - Margin * 2, 140);
        Panel(foot, 34);
        // Under a prompt the foot keeps the question the prompt was come to from
        string line = screen.Step switch
        {
            FactoryStep.Confirm => Text.Of("factoryScreen.chooseThree"),
            FactoryStep.Accept or FactoryStep.Traded => Text.Of("factoryScreen.prompt.receive"),
            FactoryStep.Quit => screen.Giving >= 0 ? Text.Of("factoryScreen.prompt.receive") : Text.Of("factoryScreen.prompt.give"),
            _ => screen.Prompt
        };
        UiFonts.DrawCentered(line, foot.X + 48, foot.Y + foot.Height / 2f, 40, Ink, UiWeight.ExtraBold);
        if (screen.Step == FactoryStep.Options)
        {
            var options = screen.Options;
            const float bw = 250, gap = 24;
            float x = foot.X + foot.Width - 40 - options.Count * bw - (options.Count - 1) * gap;
            for (int i = 0; i < options.Count; i++)
            {
                Color colour = options[i] switch
                {
                    FactoryOption.Rent or FactoryOption.Exchange => Green,
                    FactoryOption.Remove => Selection,
                    FactoryOption.Cancel => Muted,
                    _ => Blue
                };
                var option = options[i];
                Button(new Rectangle(x + i * (bw + gap), foot.Y + 32, bw, 76), 38, colour, Text.Of($"factoryScreen.option.{option}"), 30, i == screen.MenuCursor);
            }
        }

        // A yes or a no, over everything
        (string, Color)[] yesNo = { (Text.Of("records.answer.yes"), Green), (Text.Of("records.answer.no"), Red) };
        float asked = Math.Clamp(screen.StepAge / 0.2f, 0f, 1f);
        switch (screen.Step)
        {
            case FactoryStep.Confirm:
                Prompt(sw, sh, Text.Of("factoryScreen.confirm.title"),
                    Text.Of("factoryScreen.confirm.body", ("names", string.Join(", ", screen.Chosen.Select(c => screen.Mine[c].DisplayName))), ("prompt", screen.Prompt)), yesNo, screen.MenuCursor, asked);
                break;
            case FactoryStep.Accept:
                Prompt(sw, sh, Text.Of("factoryScreen.accept.title"),
                    Text.Of("factoryScreen.accept.body", ("name", screen.Mine[screen.Giving].DisplayName), ("prompt", screen.Prompt)), yesNo, screen.MenuCursor, asked);
                break;
            case FactoryStep.Quit:
                Prompt(sw, sh, Text.Of("factoryScreen.quit.title"), Text.Of("factoryScreen.quit.body", ("prompt", screen.Prompt)), yesNo, screen.MenuCursor, asked);
                break;
            case FactoryStep.Traded:
                Prompt(sw, sh, Text.Of("factoryScreen.traded"), screen.Prompt, new[] { (Text.Of("shared.ok"), Blue) }, 0, asked);
                break;
        }
    }

    // The six on offer in a row, and under them the three places they are chosen into
    private static void RentalRow(int sw, FactoryScreen screen, float rise)
    {
        int count = Math.Max(1, screen.Mine.Count);
        const float gap = 24;
        float width = (sw - Margin * 2 - gap * (count - 1)) / count;
        Label(Text.Of("factoryScreen.onOffer"), Margin + 8, 132);
        for (int i = 0; i < screen.Mine.Count; i++)
        {
            int order = screen.Chosen.ToList().IndexOf(i);
            bool under = i == screen.Cursor && screen.Step is FactoryStep.Browse or FactoryStep.Options;
            var card = new Rectangle(Margin + i * (width + gap), 166 + rise - (i == screen.Cursor ? 10 : 0), width, 440);
            RentalCard(card, screen.Mine[i], under, order);
        }

        // The places the three are chosen into, in the order they will go into battle
        var places = new Rectangle(Margin, 636 + rise, sw - Margin * 2, 232);
        Panel(places, 34);
        Label(Text.Of("factoryScreen.places"), places.X + 44, places.Y + 28);
        float slot = (places.Width - 88 - gap * (screen.Needed - 1)) / screen.Needed;
        for (int n = 0; n < screen.Needed; n++)
        {
            var r = new Rectangle(places.X + 44 + n * (slot + gap), places.Y + 72, slot, 128);
            bool filled = n < screen.Chosen.Count;
            UiShapes.Shape(r, 28, filled ? new Color(248, 250, 253, 255) : new Color(240, 244, 250, 255), new Color(232, 238, 246, 255), Rule, 3);
            OrderBadge(new Vector2(r.X + 62, r.Y + r.Height / 2f), n + 1, filled);
            if (!filled)
            {
                UiFonts.DrawCentered(Text.Of("factoryScreen.notChosen"), r.X + 120, r.Y + r.Height / 2f, 30, Muted, UiWeight.ExtraBold);
                continue;
            }
            var p = screen.Mine[screen.Chosen[n]];
            NameWithGender(p, r.X + 120, r.Y + r.Height / 2f - 14, 38);
            TypePills(r.X + 120, r.Y + r.Height / 2f + 14, p, 30);
        }
    }

    // A trade: one's own three over the trainer's three, each row with its ways back, and the trade as it stands beside them
    private static void TradeRows(int sw, FactoryScreen screen, float rise)
    {
        const float gap = 24, width = 300, height = 330;
        bool mineLive = screen.Step is FactoryStep.Browse or FactoryStep.Options;
        bool theirsLive = screen.Step is FactoryStep.Receive or FactoryStep.Accept;
        Label(Text.Of("factoryScreen.yourRentals"), Margin + 8, 132);
        for (int i = 0; i < screen.Mine.Count; i++)
        {
            var card = new Rectangle(Margin + i * (width + gap), 162 + rise, width, height);
            RentalCard(card, screen.Mine[i], mineLive && i == screen.Cursor, i == screen.Giving ? 0 : -1, giving: true);
        }
        float bx = Margin + screen.Mine.Count * (width + gap) + 16;
        Button(new Rectangle(bx, 162 + rise + height / 2f - 42, 240, 84), 42, Muted, Text.Of("factoryScreen.option.Cancel"), 30, mineLive && screen.Cursor == screen.CancelPlace);

        string theirs = screen.TheirTrainer.Length > 0 ? Text.Of("factoryScreen.theirs", ("trainer", screen.TheirTrainer.ToUpperInvariant())) : Text.Of("factoryScreen.beaten");
        Label(theirs, Margin + 8, 514);
        for (int i = 0; i < screen.Theirs.Count; i++)
        {
            var card = new Rectangle(Margin + i * (width + gap), 544 + rise, width, height);
            RentalCard(card, screen.Theirs[i], theirsLive && i == screen.TheirCursor, -1, dim: !theirsLive && screen.Step != FactoryStep.Traded);
        }
        Button(new Rectangle(bx, 544 + rise + height / 2f - 96, 240, 84), 42, Blue, Text.Of("factoryScreen.back"), 30, theirsLive && screen.TheirCursor == screen.BackPlace);
        Button(new Rectangle(bx, 544 + rise + height / 2f + 12, 240, 84), 42, Muted, Text.Of("factoryScreen.option.Cancel"), 30, theirsLive && screen.TheirCursor == screen.TheirCancelPlace);

        // The trade as it stands
        var side = new Rectangle(bx + 240 + 40, 162 + rise, sw - Margin - (bx + 240 + 40), 712);
        Panel(side, 34);
        UiFonts.Draw(Text.Of("factoryScreen.trade"), side.X + 40, side.Y + 36, 40, Ink, UiWeight.Black);
        Label(Text.Of("factoryScreen.give"), side.X + 40, side.Y + 124);
        string give = screen.Giving >= 0 ? screen.Mine[screen.Giving].DisplayName : "—";
        UiFonts.Draw(give, side.X + 40, side.Y + 152, 44, screen.Giving >= 0 ? Ink : Muted, UiWeight.Black);
        UiShapes.Triangle(new Vector2(side.X + side.Width / 2f - 26, side.Y + 262), new Vector2(side.X + side.Width / 2f + 26, side.Y + 262), new Vector2(side.X + side.Width / 2f, side.Y + 300), Rule);
        Label(Text.Of("factoryScreen.take"), side.X + 40, side.Y + 344);
        bool taking = theirsLive && screen.TheirCursor < screen.Theirs.Count || screen.Step == FactoryStep.Traded;
        string take = screen.Step == FactoryStep.Traded && screen.Outcome.Count == 2 ? screen.Theirs[screen.Outcome[1]].DisplayName
            : taking ? screen.Theirs[screen.TheirCursor].DisplayName : "—";
        UiFonts.Draw(take, side.X + 40, side.Y + 372, 44, taking ? Ink : Muted, UiWeight.Black);
        DrawWrapped(Text.Of("factoryScreen.readNote"), side.X + 40, side.Y + 520, side.Width - 80, 26, Muted, 36, UiWeight.ExtraBold);
    }

    // One rental: its sprite on a disc, its name and gender, its level; the place it was chosen at in a badge
    private static void RentalCard(Rectangle r, Pokemon p, bool selected, int order, bool giving = false, bool dim = false)
    {
        Card(r, 30, selected, order >= 0 ? Green : null);
        if (order >= 0 && !selected) UiShapes.Shape(r, 30, PanelTop, PanelBottom, Green, 6f);
        var c = new Vector2(r.X + r.Width / 2f, r.Y + (r.Height - 96) / 2f + 8);
        float disc = Math.Min(r.Width, r.Height - 96) / 2f - 14;
        UiShapes.Circle(c, disc, new Color(226, 234, 246, 255));
        var sprite = PixelArtGenerator.SpriteOf(p);
        int scale = sprite.Height * 2 <= disc * 2 + 60 ? 2 : 1;
        Raylib.DrawTexturePro(sprite, new Rectangle(0, 0, sprite.Width, sprite.Height),
            new Rectangle(MathF.Round(c.X - sprite.Width * scale / 2f), MathF.Round(c.Y - sprite.Height * scale / 2f), sprite.Width * scale, sprite.Height * scale),
            Vector2.Zero, 0f, dim ? new Color(255, 255, 255, 150) : Color.White);
        float nameWidth = UiFonts.Measure(p.DisplayName, 32, UiWeight.Black) + (p.Gender == Gender.Genderless ? 0 : 36);
        NameWithGender(p, r.X + Math.Max(16, (r.Width - nameWidth) / 2f), r.Y + r.Height - 62, 32);
        string level = Text.Of("factoryScreen.level", ("level", p.Level));
        UiFonts.DrawCentered(level, r.X + (r.Width - UiFonts.Measure(level, 22, UiWeight.Black)) / 2f, r.Y + r.Height - 26, 22, Muted, UiWeight.Black);
        if (order >= 0 && !giving) OrderBadge(new Vector2(r.X + 44, r.Y + 44), order + 1, true);
        else if (order >= 0) Tag(r.X + 20, r.Y + 20, Text.Of("factoryScreen.givingUp"), Selection, 34);
    }

    private static void OrderBadge(Vector2 c, int number, bool filled)
    {
        UiShapes.Circle(c, 30, filled ? Green : Rule);
        string text = number.ToString();
        UiFonts.DrawCentered(text, c.X - UiFonts.Measure(text, 34, UiWeight.Black) / 2f, c.Y, 34, filled ? Color.White : Muted, UiWeight.Black);
    }
}
