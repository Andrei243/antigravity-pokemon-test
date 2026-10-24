using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The Pokémon menu: the team as six cards, and the summary of one of them.</summary>
internal static partial class ModernUi
{
    /// <param name="appear">0 as the screen opens to 1 once it has settled: the cards rise into place one after another.</param>
    /// <param name="actions">The chosen Pokémon's menu while it is open (style guide, "Menu screens"): a panel beside its card.</param>
    /// <param name="giver">The Pokémon giving HP with Milk Drink or Soft-Boiled, whose card is marked while another is chosen.</param>
    public static void DrawParty(int sw, int sh, Party party, int selected, int? swapping, float appear = 1f, string? prompt = null,
        IReadOnlyList<string>? actions = null, int actionIndex = 0, int? giver = null)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("party.title"));
        Hints(sw - 64, 44, Hint("shared.keys.z", actions != null ? "shared.hints.choose" : "shared.hints.menu"), Hint("shared.keys.shift", "shared.hints.move"), Hint("shared.keys.esc", "shared.hints.back"));

        Rectangle chosen = default;
        for (int i = 0; i < Party.MaxSize; i++)
        {
            float rise = (1f - UiMotion.EaseOut(appear * 1.6f - i * 0.12f)) * 40f;
            var r = new Rectangle(64 + (i % 2) * 912, 132 + (i / 2) * 256 + rise, 880, 232);
            if (i == selected) chosen = r;
            if (i >= party.Count) EmptySlot(r, 34);
            else PartyCard(r, party.Members[i], i == selected, i == swapping || i == giver, i == 0);
        }

        var box = new Rectangle(64, 920, 1792, 112);
        Panel(box, 30);
        UiFonts.DrawCentered(prompt ?? Text.Of(swapping.HasValue ? "party.prompt.moveTo" : "party.prompt.choose"), box.X + 52, box.Y + box.Height / 2f, 40, Ink, UiWeight.ExtraBold);

        if (actions != null) PartyActions(sw, chosen, box, actions, actionIndex);
    }

    /// <summary>
    /// A Pokémon's menu: a panel standing on the prompt, over the half of the screen its card isn't on, a row to
    /// an entry, the field moves among them in the field-move colour.
    /// </summary>
    private static void PartyActions(int sw, Rectangle card, Rectangle box, IReadOnlyList<string> actions, int selected)
    {
        const float width = 400, row = 70;
        float height = 20 + actions.Count * row + 12;
        bool onTheLeft = card.X > sw / 2f;
        var panel = new Rectangle(onTheLeft ? 64 + 880 - width : sw - 64 - width, box.Y - 22 - height, width, height);
        Panel(panel, 30);
        for (int i = 0; i < actions.Count; i++)
        {
            var r = new Rectangle(panel.X + 14, panel.Y + 16 + i * row, width - 28, row - 8);
            ListRow(r, i == selected, actions[i], nameX: 40, size: 32);
        }
    }

    /// <summary>
    /// The team as the target of an item from the bag. <paramref name="tag"/> says what the item would come to for
    /// a Pokémon (null: nothing to say), shown on its card as the games do: ABLE, NOT ABLE, and for a TM LEARNED.
    /// </summary>
    /// <param name="waiting">The prompt is a line that waits for a press, with the arrow that says so and no way back (a level's panel, plan 08 · P5).</param>
    public static void DrawPartyChoice(int sw, int sh, Party party, int selected, string prompt, Func<Pokemon, string?> tag, float appear = 1f,
        string? action = null, bool waiting = false, bool greyRefused = false)
    {
        Backdrop(sw, sh);
        action ??= Text.Of("shared.hints.use");
        ScreenTitle(Text.Of("party.title"));
        const string z = "shared.keys.z";
        if (waiting) Hints(sw - 64, 44, HintOf(z, action));
        else Hints(sw - 64, 44, HintOf(z, action), Hint("shared.keys.esc", "shared.hints.back"));
        string able = Text.Of("party.tag.able"), notAble = Text.Of("party.tag.notAble");

        for (int i = 0; i < Party.MaxSize; i++)
        {
            float rise = (1f - UiMotion.EaseOut(appear * 1.6f - i * 0.12f)) * 40f;
            var r = new Rectangle(64 + (i % 2) * 912, 132 + (i / 2) * 256 + rise, 880, 232);
            if (i >= party.Count)
            {
                EmptySlot(r, 34);
                continue;
            }
            var p = party.Members[i];
            PartyCard(r, p, i == selected, false, lead: false);
            // ABLE, and a place among those entered (1ST, 2ND: plan 06 · R18a), in the colour of what can be done
            if (tag(p) is { } said) Tag(r.X + 264, r.Y + 183, said, said == able || char.IsDigit(said[0]) ? Green : Muted);
            // A Pokémon that may not be chosen is greyed (plan 14 · B14; a fainted one is greyed by its card already)
            if (greyRefused && !p.IsFainted && tag(p) == notAble) UiShapes.Fill(r, 34, new Color(40, 40, 60, 90));
        }

        var box = new Rectangle(64, 920, 1792, 112);
        Panel(box, 30);
        UiFonts.DrawCentered(prompt, box.X + 52, box.Y + box.Height / 2f, 40, Ink, UiWeight.ExtraBold);
        if (waiting) AdvanceArrow(box.X + box.Width - 64, box.Y + box.Height / 2f - 10);
    }

    private static void PartyCard(Rectangle r, Pokemon p, bool selected, bool swapping, bool lead)
    {
        Card(r, 34, selected || swapping, swapping ? Gold : Selection);
        Portrait(new Vector2(r.X + 136, r.Y + r.Height / 2f), 96, p, 4, selected);
        // An Egg has no level, types or hit points to show (plan 06 · R15)
        if (p.IsEgg)
        {
            NameWithGender(p, r.X + 264, r.Y + 58, 42);
            UiFonts.DrawCentered(Text.Of("party.waitingToHatch"), r.X + 264, r.Y + 112, 30, Muted, UiWeight.ExtraBold);
            return;
        }

        float x = r.X + 264;
        NameWithGender(p, x, r.Y + 58, 42);
        Level(r.X + r.Width - 44, r.Y + 58, p.Level, 40);

        float tx = TypePills(x, r.Y + 94, p);
        float status = StatusPill(tx + 4, r.Y + 94, p.IsFainted ? StatusCondition.Faint : p.Status);
        PokerusTag(tx + 4 + (status > 0 ? status + 8 : 0), r.Y + 94, p, 34);

        HpBar(x, r.Y + 150, r.Width - 308, 26, (float)p.CurrentHP / Math.Max(1, p.MaxHP));
        string hpText = Text.Of("party.hpValue", ("hp", p.CurrentHP), ("max", p.MaxHP));
        float hw = UiFonts.Measure(hpText, 30, UiWeight.Black);
        UiFonts.DrawCentered(hpText, r.X + r.Width - 44 - hw, r.Y + 200, 30, Ink, UiWeight.Black);
        if (lead) UiFonts.DrawCentered(Text.Of("party.lead"), x, r.Y + 200, 22, Muted, UiWeight.Black);

        if (p.IsFainted) UiShapes.Fill(r, 34, new Color(40, 40, 60, 90));
    }

    // ------------------------------------------------------------------ summary

    private static readonly StatType[] SummaryStats = { StatType.HP, StatType.Attack, StatType.Defense, StatType.SpAttack, StatType.SpDefense, StatType.Speed };

    /// <summary>The colour of a stat bar by how high the species' base stat is: red, amber, green, then teal.</summary>
    public static Color StatColor(int baseStat) =>
        baseStat < 50 ? new Color(240, 110, 84, 255)
        : baseStat < 80 ? new Color(246, 190, 60, 255)
        : baseStat < 110 ? new Color(96, 204, 110, 255)
        : new Color(60, 196, 206, 255);

    /// <summary>
    /// One Pokémon in full: its 2D sprite, name, types and nature on the left; stats and experience, then its moves,
    /// on the right.
    /// </summary>
    /// <param name="page">-1 for a summary of one page; else 0 for the stats and moves, 1 for the condition (plan 06 · R14c) and 2 for the ribbons (plan 06 · R17b).</param>
    /// <param name="ribbonCursor">On the ribbons' page, the ribbon looked at; -1 while the page only shows them.</param>
    /// <param name="rental">A rental Pokémon of the Battle Factory (plan 06 · R18b): its ability and what it holds stand where a Pokémon of one's own shows its experience.</param>
    /// <param name="title">The screen's title where it is neither the team's nor a rental's (the Battle Castle's counters, plan 06 · R18c).</param>
    public static void DrawSummary(int sw, int sh, Pokemon p, int index, int count, float appear = 1f, int page = -1, int ribbonCursor = -1, int markCursor = -1, int moveCursor = -1, bool rental = false, string? title = null, bool canMark = true)
    {
        Backdrop(sw, sh);
        ScreenTitle(title ?? Text.Of(rental ? "summary.rental" : "summary.title"));
        var esc = Hint("shared.keys.esc", "shared.hints.back");
        var page2 = Hint("shared.keys.leftRight", "shared.hints.page");
        var next = Hint("shared.keys.upDown", "shared.hints.nextPokemon");
        if (page == PartyScreen.StatsPage && markCursor >= 0) Hints(sw - 64, 44, Hint("shared.keys.leftRight", "shared.hints.mark"), Hint("shared.keys.z", "shared.hints.mark"), esc);
        else if (page == PartyScreen.StatsPage && !p.IsEgg && !rental && canMark) Hints(sw - 64, 44, Hint("shared.keys.z", "shared.hints.marks"), page2, next, esc);
        else if (page is PartyScreen.BattleMovesPage or PartyScreen.ContestMovesPage && !p.IsEgg && moveCursor >= 0) Hints(sw - 64, 44, Hint("shared.keys.upDown", "shared.hints.moves"), esc);
        else if (page is PartyScreen.BattleMovesPage or PartyScreen.ContestMovesPage && !p.IsEgg) Hints(sw - 64, 44, Hint("shared.keys.z", "shared.hints.look"), page2, next, esc);
        else if (page == PartyScreen.RibbonsPage && ribbonCursor >= 0) Hints(sw - 64, 44, Hint("shared.keys.arrows", "shared.hints.ribbon"), esc);
        else if (page == PartyScreen.RibbonsPage && !p.IsEgg && p.Ribbons.Count > 0) Hints(sw - 64, 44, Hint("shared.keys.z", "shared.hints.look"), page2, esc);
        else if (page >= 0 && !p.IsEgg && count > 1) Hints(sw - 64, 44, page2, next, esc);
        else if (page >= 0 && !p.IsEgg) Hints(sw - 64, 44, page2, esc);
        else if (count > 1) Hints(sw - 64, 44, next, esc);
        else Hints(sw - 64, 44, esc);

        float slide = (1f - UiMotion.EaseOut(appear)) * 60f;
        if (p.IsEgg)
        {
            EggSummary(p, index, count, slide);
            return;
        }

        // ---- Who it is
        var who = new Rectangle(64 - slide, 132, 600, 900);
        Panel(who, 34);
        UiFonts.DrawCentered(Text.Of("summary.number", ("number", p.Species.DexNumber.ToString("D3"))), who.X + 44, who.Y + 50, 30, Muted, UiWeight.Black);
        Level(who.X + who.Width - 44, who.Y + 50, p.Level, 40);

        var c = new Vector2(who.X + who.Width / 2f, who.Y + 300);
        UiShapes.Circle(c, 204, new Color(226, 234, 246, 255));
        UiShapes.Fill(new Rectangle(c.X - 204, c.Y - 4, 408, 8), 4, Rule);
        UiShapes.Circle(c, 56, Rule);
        UiShapes.Circle(c, 38, new Color(226, 234, 246, 255));
        // The Pokémon looked at breathes (plan 09 · L5), its still standing in until its strip is baked
        Breathing(PixelArtGenerator.SpriteOf(p), PixelArtGenerator.StripOf(p), c, 3);
        if (count > 1)
        {
            string place = Text.Of("summary.place", ("index", index + 1), ("count", count));
            UiFonts.DrawCentered(place, who.X + who.Width - 44 - UiFonts.Measure(place, 24, UiWeight.Black), who.Y + 494, 24, Muted, UiWeight.Black);
        }

        float x = who.X + 44;
        NameWithGender(p, x, who.Y + 568, 52);
        UiFonts.Draw(Text.Of("summary.category", ("category", p.Species.Category)), x, who.Y + 606, 28, Muted, UiWeight.ExtraBold);
        float tx = TypePills(x, who.Y + 662, p, 42);
        float status = StatusPill(tx + 4, who.Y + 662, p.IsFainted ? StatusCondition.Faint : p.Status, 42);
        PokerusTag(tx + 4 + (status > 0 ? status + 8 : 0), who.Y + 662, p, 42);

        UiShapes.Fill(new Rectangle(x, who.Y + 732, who.Width - 88, 3), 1.5f, Rule);
        Label(Text.Of("summary.nature"), x, who.Y + 752);
        // A mint's nature beside its own (plan 06 · R28b): the stats below are the mint's
        string nature = p.MintNature is { } mint ? Text.Of("summary.minted", ("nature", Words.Nature(p.Nature)), ("mint", Words.Nature(mint))) : Words.Nature(p.Nature);
        UiFonts.Draw(nature, x, who.Y + 776, p.MintNature != null ? 26 : 34, Ink, UiWeight.Black);
        if (rental)
        {
            Label(Text.Of("summary.ability"), x + 260, who.Y + 752);
            UiFonts.Draw(p.AbilityName is { } shownAbility ? LocalNames.Ability(shownAbility) : "", x + 260, who.Y + 776, 34, Ink, UiWeight.Black);
            Label(Text.Of("summary.heldItem"), x, who.Y + 824);
            UiFonts.Draw(p.HeldItem?.DisplayName ?? Text.Of("summary.none"), x, who.Y + 848, 34, Ink, UiWeight.Black);
        }
        else
        {
            Label(Text.Of("summary.expPoints"), x + 260, who.Y + 752);
            UiFonts.Draw(p.CurrentExp.ToString("N0"), x + 260, who.Y + 776, 34, Ink, UiWeight.Black);

            // At the game's level cap (plan 18 · W4: 100 in Platinum, 260 by the modern rules)
            bool top = p.AtTheCap;
            Label(top ? Text.Of("summary.topLevel") : Text.Of("summary.toLevel", ("level", p.Level + 1)), x, who.Y + 834);
            string toNext = top ? "" : Math.Max(0, p.ExpForNextLevel - p.CurrentExp).ToString("N0");
            UiFonts.Draw(toNext, who.X + who.Width - 44 - UiFonts.Measure(toNext, 24, UiWeight.Black), who.Y + 830, 24, Ink, UiWeight.Black);
            ExpBar(new Rectangle(x, who.Y + 864, who.Width - 88, 12), top ? 1f : p.ExpProgressRatio);
        }

        // ---- The condition, on its own page
        if (page == 1)
        {
            ConditionPanel(new Rectangle(696 + slide, 132, 1160, 900), ConditionOf(p));
            return;
        }

        // ---- The trainer memo, on its own (plan 08 · P3)
        if (page == PartyScreen.MemoPage)
        {
            MemoPanel(new Rectangle(696 + slide, 132, 1160, 900), p);
            return;
        }

        // ---- The ribbons, on theirs
        if (page == PartyScreen.RibbonsPage)
        {
            RibbonsPanel(new Rectangle(696 + slide, 132, 1160, 900), p, ribbonCursor);
            return;
        }

        // ---- The skills, on their own page (plan 08 · P3): the stats at ease, then the ability and what it does
        if (page == PartyScreen.SkillsPage)
        {
            var skills = new Rectangle(696 + slide, 132, 1160, 900);
            Panel(skills, 34);
            StatRows(skills, p, 92, 66);
            UiShapes.Fill(new Rectangle(skills.X + 44, skills.Y + 536, skills.Width - 88, 3), 1.5f, Rule);
            Label(Text.Of("summary.ability"), skills.X + 44, skills.Y + 566);
            if (p.Ability is { } ability)
            {
                UiFonts.Draw(ability.DisplayName, skills.X + 44, skills.Y + 596, 40, Ink, UiWeight.Black);
                DrawWrapped(ability.DisplayDescription, skills.X + 44, skills.Y + 660, skills.Width - 88, 30, Muted, 40, UiWeight.Bold);
            }
            return;
        }

        // ---- The moves, on two pages of their own (plan 08 · P4): as a battle has them, and as the contest stage does
        if (page is PartyScreen.BattleMovesPage or PartyScreen.ContestMovesPage)
        {
            bool contest = page == PartyScreen.ContestMovesPage;
            MovesPanel(new Rectangle(696 + slide, 132, 1160, 476), p, moveCursor, contest);
            MoveTextPanel(new Rectangle(696 + slide, 632, 1160, 400), p, Math.Max(0, moveCursor), contest);
            return;
        }

        // ---- The first page: the held item and, under it, the six marks
        if (page == PartyScreen.StatsPage)
        {
            var item = new Rectangle(696 + slide, 132, 1160, 400);
            Panel(item, 34);
            Label(Text.Of("summary.heldItem"), item.X + 44, item.Y + 30);
            UiFonts.Draw(p.HeldItem?.DisplayName ?? Text.Of("summary.none"), item.X + 44, item.Y + 62, 40, Ink, UiWeight.Black);
            if (p.HeldItem is { } held) DrawWrapped(held.DisplayDescription, item.X + 44, item.Y + 118, item.Width - 88, 30, Muted, 40, UiWeight.Bold);

            var marks = new Rectangle(696 + slide, 556, 1160, 476);
            Panel(marks, 34);
            Label(Text.Of("summary.marks"), marks.X + 44, marks.Y + 30);
            for (int m = 0; m < MarkCount; m++)
            {
                var at = new Vector2(marks.X + 44 + 52 + m * 120, marks.Y + 250);
                if (m == markCursor) UiShapes.Shape(new Rectangle(at.X - 48, at.Y - 48, 96, 96), 24, Lighter(Selection, 0.88f), Lighter(Selection, 0.8f), Selection, 4);
                MarkShape(at, 52, m, SummaryView.HasMark(p, m) ? Ink : Rule);
            }
            return;
        }

        // ---- A summary of one page (a rental's, the Battle Hall's): the stats above the moves
        var upper = new Rectangle(696 + slide, 132, 1160, 400);
        Panel(upper, 34);
        StatRows(upper, p, 92, 52);
        MovesPanel(new Rectangle(696 + slide, 556, 1160, 476), p, -1, false);
    }

    /// <summary>
    /// A Pokémon's four moves as rows in a panel (plan 08 · P4): type, name and category, and power, accuracy and PP; or,
    /// for the contest stage's page, its quality and the hearts it earns by itself. The row under the cursor has a
    /// Selection rim.
    /// </summary>
    private static void MovesPanel(Rectangle moves, Pokemon p, int cursor, bool contest)
    {
        Panel(moves, 34);
        Label(Text.Of("summary.moves"), moves.X + 44, moves.Y + 30);
        if (contest)
        {
            Label(Text.Of("summary.contest.stat"), moves.X + 664, moves.Y + 30);
            string appeal = Text.Of("summary.contest.appeal");
            Label(appeal, moves.X + moves.Width - 44 - UiFonts.Measure(appeal, 20, UiWeight.Black), moves.Y + 30);
        }
        else
        {
            Label(Text.Of("summary.power"), moves.X + 664, moves.Y + 30);
            Label(Text.Of("summary.accuracy"), moves.X + 808, moves.Y + 30);
            string ppLabel = Text.Of("summary.pp");
            Label(ppLabel, moves.X + moves.Width - 44 - UiFonts.Measure(ppLabel, 20, UiWeight.Black), moves.Y + 30);
        }
        for (int i = 0; i < 4; i++)
        {
            var row = new Rectangle(moves.X + 28, moves.Y + 68 + i * 98, moves.Width - 56, 86);
            bool chosen = i == cursor;
            if (i >= p.Moves.Count)
            {
                UiShapes.Shape(row, 24, new Color(240, 244, 250, 255), new Color(232, 238, 246, 255), Rule, 3);
                UiFonts.DrawCentered("—", row.X + 30, row.Y + row.Height / 2f, 32, Muted, UiWeight.Black);
                continue;
            }

            var move = p.Moves[i];
            if (chosen) UiShapes.Shape(row, 24, Lighter(Selection, 0.9f), Lighter(Selection, 0.82f), Selection, 5);
            else UiShapes.Shape(row, 24, new Color(248, 250, 253, 255), new Color(236, 241, 248, 255), Rule, 3);
            float cy = row.Y + row.Height / 2f;
            TypePill(row.X + 16, cy - 22, move.Type, 44, 150);
            UiFonts.DrawCentered(move.DisplayName, row.X + 190, cy - 12, 32, Ink, UiWeight.Black);
            if (contest)
            {
                var quality = move.Data.Contest;
                UiFonts.DrawCentered(Words.Category(move.Category), row.X + 190, cy + 22, 18, Muted, UiWeight.Black);
                UiFonts.DrawCentered(quality != null ? Text.Of($"condition.quality.{quality.Type}") : "—", moves.X + 664, cy, 30, Ink, UiWeight.Black);
                int hearts = quality?.Hearts ?? 0;
                for (int h = 0; h < 3; h++)
                    MarkShape(new Vector2(moves.X + moves.Width - 44 - 26 - (2 - h) * 52, cy), 40, 3, h < hearts ? Red : Rule);
                continue;
            }
            UiFonts.DrawCentered(Words.Category(move.Category), row.X + 190, cy + 22, 18, Muted, UiWeight.Black);
            UiFonts.DrawCentered(move.Power > 0 ? move.Power.ToString() : "—", moves.X + 664, cy, 32, Ink, UiWeight.Black);
            UiFonts.DrawCentered(move.Accuracy > 0 ? move.Accuracy.ToString() : "—", moves.X + 808, cy, 32, Ink, UiWeight.Black);
            string pp = Text.Of("summary.ppValue", ("pp", move.CurrentPP), ("max", move.MaxPP));
            UiFonts.DrawCentered(pp, moves.X + moves.Width - 44 - UiFonts.Measure(pp, 30, UiWeight.Black), cy, 30, move.CurrentPP > 0 ? Ink : Red, UiWeight.Black);
        }
    }

    /// <summary>The move chosen, in words (plan 08 · P4): its name and what it does, or what its effect is on the contest stage.</summary>
    private static void MoveTextPanel(Rectangle r, Pokemon p, int index, bool contest)
    {
        Panel(r, 34);
        if (index >= p.Moves.Count) return;
        var move = p.Moves[index];
        UiFonts.Draw(move.DisplayName, r.X + 44, r.Y + 34, 44, Ink, UiWeight.Black);
        if (contest)
        {
            var quality = move.Data.Contest;
            string line = quality == null ? Text.Of("summary.contest.noContest") : Text.Of($"summary.contest.effect.{quality.Effect}");
            DrawWrapped(line, r.X + 44, r.Y + 112, r.Width - 88, 32, Ink, 44, UiWeight.ExtraBold);
            return;
        }
        TypePill(r.X + 44, r.Y + 106, move.Type, 44, 150);
        float after = r.X + 44 + 150 + 20;
        UiFonts.Draw(Words.Category(move.Category), after, r.Y + 116, 26, Muted, UiWeight.Black);
        DrawWrapped(move.DisplayDescription, r.X + 44, r.Y + 188, r.Width - 88, 32, Ink, 44, UiWeight.ExtraBold);
    }
    /// <summary>The six stats as rows in a panel: the value, and a bar whose length and colour follow the species' base stat (HP shows its current share).</summary>
    private static void StatRows(Rectangle panel, Pokemon p, float first, float pitch)
    {
        Label(Text.Of("summary.stats"), panel.X + 44, panel.Y + 30);
        int[] values = { p.MaxHP, p.Attack, p.Defense, p.SpAttack, p.SpDefense, p.Speed };
        int[] bases = { p.Species.BaseHP, p.Species.BaseAttack, p.Species.BaseDefense, p.Species.BaseSpAttack, p.Species.BaseSpDefense, p.Species.BaseSpeed };
        for (int i = 0; i < 6; i++)
        {
            float cy = panel.Y + first + i * pitch;
            UiFonts.DrawCentered(Words.Stat(SummaryStats[i]), panel.X + 44, cy, 30, Ink, UiWeight.ExtraBold);
            string value = i == 0 ? Text.Of("party.hpValue", ("hp", p.CurrentHP), ("max", p.MaxHP)) : values[i].ToString();
            UiFonts.DrawCentered(value, panel.X + 380 - UiFonts.Measure(value, 32, UiWeight.Black), cy, 32, Ink, UiWeight.Black);
            var track = new Rectangle(panel.X + 420, cy - 11, panel.Width - 420 - 44, 22);
            if (i == 0) Bar(track, (float)p.CurrentHP / Math.Max(1, p.MaxHP), HpColor((float)p.CurrentHP / Math.Max(1, p.MaxHP)));
            else Bar(track, bases[i] / 160f, StatColor(bases[i]));
        }
    }

    /// <summary>
    /// The trainer memo (plan 08 · P3): its trainer and number, when and where it was met and at what level, its nature
    /// and characteristic, and its ability with what it does. A Pokémon of the player's own carries no mark and shows the
    /// player's name and number.
    /// </summary>
    private static void MemoPanel(Rectangle r, Pokemon p)
    {
        Panel(r, 34);
        float x = r.X + 44, wide = r.Width - 88;
        var (name, id) = p.OriginalTrainer is { } mark ? (mark.Name, mark.Id) : (PlayerIdentity.Name, (int)(PlayerIdentity.Number & 0xFFFF));
        Label(Text.Of("summary.memo.trainer"), x, r.Y + 34);
        UiFonts.Draw(name, x, r.Y + 62, 40, Ink, UiWeight.Black);
        string idLabel = Text.Of("summary.memo.id");
        Label(idLabel, x + 640, r.Y + 34);
        UiFonts.Draw(id.ToString("D5"), x + 640, r.Y + 62, 40, Ink, UiWeight.Black);
        UiShapes.Fill(new Rectangle(x, r.Y + 140, wide, 3), 1.5f, Rule);

        Label(p.MetDate != null ? Text.Of("summary.memo.metOn") : Text.Of("summary.memo.metOnUnknown"), x, r.Y + 166);
        if (p.MetDate is { } day) UiFonts.Draw(Words.Date(day), x, r.Y + 194, 34, Ink, UiWeight.Black);
        string where = p.MetLocation is { Length: > 0 } place ? LocalNames.PlaceShown(null, place) : "";
        string met = where.Length == 0 ? Text.Of("summary.memo.unknownLine")
            : p.MetLevel == 0 ? Text.Of("summary.memo.hatchedLine", ("where", where))
            : Text.Of("summary.memo.metLine", ("where", where), ("level", p.MetLevel));
        float metY = p.MetDate != null ? r.Y + 250 : r.Y + 200;
        DrawWrapped(met, x, metY, wide, 30, Ink, 40, UiWeight.ExtraBold);
        DrawWrapped(Text.Of("summary.memo.nature", ("nature", Words.Nature(p.Nature))), x, metY + 80, wide, 30, Ink, 40, UiWeight.ExtraBold);
        UiShapes.Fill(new Rectangle(x, r.Y + 410, wide, 3), 1.5f, Rule);

        Label(Text.Of("summary.memo.characteristic"), x, r.Y + 436);
        DrawWrapped(Characteristics.Line(p), x, r.Y + 466, wide, 34, Ink, 44, UiWeight.Black);
        UiShapes.Fill(new Rectangle(x, r.Y + 560, wide, 3), 1.5f, Rule);

        Label(Text.Of("summary.memo.ability"), x, r.Y + 586);
        if (p.Ability is { } ability)
        {
            UiFonts.Draw(ability.DisplayName, x, r.Y + 614, 36, Ink, UiWeight.Black);
            DrawWrapped(ability.DisplayDescription, x, r.Y + 668, wide, 30, Muted, 40, UiWeight.Bold);
        }
    }

    /// <summary>
    /// An Egg's summary (plan 06 · R15): no number, level, stats or moves, as the original shows none, but the Egg, where
    /// it came from, and the Egg Watch's word on how near it is to hatching.
    /// </summary>
    private static void EggSummary(Pokemon egg, int index, int count, float slide)
    {
        var who = new Rectangle(64 - slide, 132, 600, 900);
        Panel(who, 34);
        var c = new Vector2(who.X + who.Width / 2f, who.Y + 300);
        UiShapes.Circle(c, 204, new Color(226, 234, 246, 255));
        var sprite = PixelArtGenerator.SpriteOf(egg);
        const int scale = 3;
        Raylib.DrawTexturePro(sprite, new Rectangle(0, 0, sprite.Width, sprite.Height),
            new Rectangle(MathF.Round(c.X - sprite.Width * scale / 2f), MathF.Round(c.Y - sprite.Height * scale / 2f), sprite.Width * scale, sprite.Height * scale),
            Vector2.Zero, 0f, Color.White);
        if (count > 1)
        {
            string place = Text.Of("summary.place", ("index", index + 1), ("count", count));
            UiFonts.DrawCentered(place, who.X + who.Width - 44 - UiFonts.Measure(place, 24, UiWeight.Black), who.Y + 494, 24, Muted, UiWeight.Black);
        }
        float x = who.X + 44;
        NameWithGender(egg, x, who.Y + 568, 52);
        UiFonts.Draw(Text.Of("summary.egg"), x, who.Y + 606, 28, Muted, UiWeight.ExtraBold);
        UiShapes.Fill(new Rectangle(x, who.Y + 662, who.Width - 88, 3), 1.5f, Rule);
        if (egg.MetLocation != null)
        {
            Label(Text.Of("summary.receivedFrom"), x, who.Y + 692);
            UiFonts.Draw(egg.MetLocation, x, who.Y + 716, 34, Ink, UiWeight.Black);
        }
        if (egg.MetDate is { } day)
        {
            Label(Text.Of("summary.on"), x, who.Y + 784);
            UiFonts.Draw(Words.Date(day), x, who.Y + 808, 34, Ink, UiWeight.Black);
        }

        var watch = new Rectangle(696 + slide, 132, 1160, 400);
        Panel(watch, 34);
        EggWatch(watch, egg);
    }
}
