using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
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
        ScreenTitle("POKÉMON");
        Hints(sw - 64, 44, ("Z", actions != null ? "Choose" : "Menu"), ("Shift", "Move"), ("Esc", "Back"));

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
        UiFonts.DrawCentered(prompt ?? (swapping.HasValue ? "Move to where?" : "Choose a Pokémon."), box.X + 52, box.Y + box.Height / 2f, 40, Ink, UiWeight.ExtraBold);

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
    /// The team as the target of an item from the bag. <paramref name="able"/> says whether the item would do
    /// anything for a Pokémon (null: nothing to say), shown as ABLE or NOT ABLE on its card, as in the games.
    /// </summary>
    public static void DrawPartyChoice(int sw, int sh, Party party, int selected, string prompt, Func<Pokemon, bool?> able, float appear = 1f,
        string action = "Use")
    {
        Backdrop(sw, sh);
        ScreenTitle("POKÉMON");
        Hints(sw - 64, 44, ("Z", action), ("Esc", "Back"));

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
            if (able(p) is { } can) Tag(r.X + 264, r.Y + 183, can ? "ABLE" : "NOT ABLE", can ? Green : Muted);
        }

        var box = new Rectangle(64, 920, 1792, 112);
        Panel(box, 30);
        UiFonts.DrawCentered(prompt, box.X + 52, box.Y + box.Height / 2f, 40, Ink, UiWeight.ExtraBold);
    }

    private static void PartyCard(Rectangle r, Pokemon p, bool selected, bool swapping, bool lead)
    {
        Card(r, 34, selected || swapping, swapping ? Gold : Selection);
        Portrait(new Vector2(r.X + 136, r.Y + r.Height / 2f), 96, p, 4, selected);

        float x = r.X + 264;
        NameWithGender(p, x, r.Y + 58, 42);
        Level(r.X + r.Width - 44, r.Y + 58, p.Level, 40);

        float tx = TypePills(x, r.Y + 94, p);
        float status = StatusPill(tx + 4, r.Y + 94, p.IsFainted ? StatusCondition.Faint : p.Status);
        PokerusTag(tx + 4 + (status > 0 ? status + 8 : 0), r.Y + 94, p, 34);

        HpBar(x, r.Y + 150, r.Width - 308, 26, (float)p.CurrentHP / Math.Max(1, p.MaxHP));
        string hpText = $"{p.CurrentHP} / {p.MaxHP}";
        float hw = UiFonts.Measure(hpText, 30, UiWeight.Black);
        UiFonts.DrawCentered(hpText, r.X + r.Width - 44 - hw, r.Y + 200, 30, Ink, UiWeight.Black);
        if (lead) UiFonts.DrawCentered("LEAD", x, r.Y + 200, 22, Muted, UiWeight.Black);

        if (p.IsFainted) UiShapes.Fill(r, 34, new Color(40, 40, 60, 90));
    }

    // ------------------------------------------------------------------ summary

    private static readonly string[] StatNames = { "HP", "Attack", "Defense", "Sp. Atk", "Sp. Def", "Speed" };

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
    public static void DrawSummary(int sw, int sh, Pokemon p, int index, int count, float appear = 1f)
    {
        Backdrop(sw, sh);
        ScreenTitle("SUMMARY");
        if (count > 1) Hints(sw - 64, 44, ("Up / Down", "Next Pokémon"), ("Esc", "Back"));
        else Hints(sw - 64, 44, ("Esc", "Back"));

        float slide = (1f - UiMotion.EaseOut(appear)) * 60f;

        // ---- Who it is
        var who = new Rectangle(64 - slide, 132, 600, 900);
        Panel(who, 34);
        UiFonts.DrawCentered($"No. {p.Species.DexNumber:D3}", who.X + 44, who.Y + 50, 30, Muted, UiWeight.Black);
        Level(who.X + who.Width - 44, who.Y + 50, p.Level, 40);

        var c = new Vector2(who.X + who.Width / 2f, who.Y + 300);
        UiShapes.Circle(c, 204, new Color(226, 234, 246, 255));
        UiShapes.Fill(new Rectangle(c.X - 204, c.Y - 4, 408, 8), 4, Rule);
        UiShapes.Circle(c, 56, Rule);
        UiShapes.Circle(c, 38, new Color(226, 234, 246, 255));
        var sprite = PixelArtGenerator.GetPokemonSprite(p.ModelName, isBack: false);
        const int scale = 3;
        Raylib.DrawTexturePro(sprite, new Rectangle(0, 0, sprite.Width, sprite.Height),
            new Rectangle(MathF.Round(c.X - sprite.Width * scale / 2f), MathF.Round(c.Y - sprite.Height * scale / 2f), sprite.Width * scale, sprite.Height * scale),
            Vector2.Zero, 0f, Color.White);
        if (count > 1)
        {
            string place = $"{index + 1} / {count}";
            UiFonts.DrawCentered(place, who.X + who.Width - 44 - UiFonts.Measure(place, 24, UiWeight.Black), who.Y + 494, 24, Muted, UiWeight.Black);
        }

        float x = who.X + 44;
        NameWithGender(p, x, who.Y + 568, 52);
        UiFonts.Draw($"{p.Species.Category} Pokémon", x, who.Y + 606, 28, Muted, UiWeight.ExtraBold);
        float tx = TypePills(x, who.Y + 662, p, 42);
        float status = StatusPill(tx + 4, who.Y + 662, p.IsFainted ? StatusCondition.Faint : p.Status, 42);
        PokerusTag(tx + 4 + (status > 0 ? status + 8 : 0), who.Y + 662, p, 42);

        UiShapes.Fill(new Rectangle(x, who.Y + 732, who.Width - 88, 3), 1.5f, Rule);
        Label("NATURE", x, who.Y + 752);
        UiFonts.Draw(p.Nature.ToString(), x, who.Y + 776, 34, Ink, UiWeight.Black);
        Label("EXP. POINTS", x + 260, who.Y + 752);
        UiFonts.Draw(p.CurrentExp.ToString("N0"), x + 260, who.Y + 776, 34, Ink, UiWeight.Black);

        bool top = p.Level >= 100;
        Label(top ? "TOP LEVEL" : $"TO Lv {p.Level + 1}", x, who.Y + 834);
        string toNext = top ? "" : Math.Max(0, p.ExpForNextLevel - p.CurrentExp).ToString("N0");
        UiFonts.Draw(toNext, who.X + who.Width - 44 - UiFonts.Measure(toNext, 24, UiWeight.Black), who.Y + 830, 24, Ink, UiWeight.Black);
        ExpBar(new Rectangle(x, who.Y + 864, who.Width - 88, 12), top ? 1f : p.ExpProgressRatio);

        // ---- Stats
        var stats = new Rectangle(696 + slide, 132, 1160, 400);
        Panel(stats, 34);
        Label("STATS", stats.X + 44, stats.Y + 30);
        int[] values = { p.MaxHP, p.Attack, p.Defense, p.SpAttack, p.SpDefense, p.Speed };
        int[] bases = { p.Species.BaseHP, p.Species.BaseAttack, p.Species.BaseDefense, p.Species.BaseSpAttack, p.Species.BaseSpDefense, p.Species.BaseSpeed };
        for (int i = 0; i < 6; i++)
        {
            float cy = stats.Y + 92 + i * 52;
            UiFonts.DrawCentered(StatNames[i], stats.X + 44, cy, 30, Ink, UiWeight.ExtraBold);
            string value = i == 0 ? $"{p.CurrentHP} / {p.MaxHP}" : values[i].ToString();
            UiFonts.DrawCentered(value, stats.X + 380 - UiFonts.Measure(value, 32, UiWeight.Black), cy, 32, Ink, UiWeight.Black);
            var track = new Rectangle(stats.X + 420, cy - 11, stats.Width - 420 - 44, 22);
            if (i == 0) Bar(track, (float)p.CurrentHP / Math.Max(1, p.MaxHP), HpColor((float)p.CurrentHP / Math.Max(1, p.MaxHP)));
            else Bar(track, bases[i] / 160f, StatColor(bases[i]));
        }

        // ---- Moves
        var moves = new Rectangle(696 + slide, 556, 1160, 476);
        Panel(moves, 34);
        Label("MOVES", moves.X + 44, moves.Y + 30);
        Label("POWER", moves.X + 664, moves.Y + 30);
        Label("ACCURACY", moves.X + 808, moves.Y + 30);
        Label("PP", moves.X + moves.Width - 44 - UiFonts.Measure("PP", 20, UiWeight.Black), moves.Y + 30);
        for (int i = 0; i < 4; i++)
        {
            var row = new Rectangle(moves.X + 28, moves.Y + 68 + i * 98, moves.Width - 56, 86);
            if (i >= p.Moves.Count)
            {
                UiShapes.Shape(row, 24, new Color(240, 244, 250, 255), new Color(232, 238, 246, 255), Rule, 3);
                UiFonts.DrawCentered("—", row.X + 30, row.Y + row.Height / 2f, 32, Muted, UiWeight.Black);
                continue;
            }

            var move = p.Moves[i];
            UiShapes.Shape(row, 24, new Color(248, 250, 253, 255), new Color(236, 241, 248, 255), Rule, 3);
            float cy = row.Y + row.Height / 2f;
            TypePill(row.X + 16, cy - 22, move.Type, 44, 150);
            UiFonts.DrawCentered(move.Name, row.X + 190, cy - 12, 32, Ink, UiWeight.Black);
            UiFonts.DrawCentered(move.Category.ToString().ToUpperInvariant(), row.X + 190, cy + 22, 18, Muted, UiWeight.Black);
            UiFonts.DrawCentered(move.Power > 0 ? move.Power.ToString() : "—", moves.X + 664, cy, 32, Ink, UiWeight.Black);
            UiFonts.DrawCentered(move.Accuracy > 0 ? move.Accuracy.ToString() : "—", moves.X + 808, cy, 32, Ink, UiWeight.Black);
            string pp = $"{move.CurrentPP} / {move.MaxPP}";
            UiFonts.DrawCentered(pp, moves.X + moves.Width - 44 - UiFonts.Measure(pp, 30, UiWeight.Black), cy, 30, move.CurrentPP > 0 ? Ink : Red, UiWeight.Black);
        }
    }
}
