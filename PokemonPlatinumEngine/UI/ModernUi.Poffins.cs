using System;
using PokemonPlatinumEngine.Battle.Effects;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// Poffins on screen (plan 06 · R14c; style guide, "Poffins"): the pot at the Poffin House, the Poffin Case, and the
/// contest condition as a five-pointed chart with the sheen's sparkles, which the summary and the case share. Our own
/// pictures: a Poffin is a round puff in its flavours' colours, the pot an iron ring over a flame.
/// </summary>
internal static partial class ModernUi
{
    // ------------------------------------------------------------------ colours

    /// <summary>A flavour's colour: spicy red, dry blue, sweet pink, bitter green, sour yellow.</summary>
    public static Color FlavorColor(Flavor flavor) => flavor switch
    {
        Flavor.Spicy => new Color(230, 86, 74, 255),
        Flavor.Dry => new Color(74, 132, 228, 255),
        Flavor.Sweet => new Color(238, 118, 172, 255),
        Flavor.Bitter => new Color(76, 174, 96, 255),
        _ => new Color(232, 190, 50, 255)
    };

    /// <summary>A Poffin's colour and the colour of its top: its first flavour and its second; the rich, the overripe, the foul and the mild their own.</summary>
    public static (Color Body, Color Top) PoffinColors(PoffinType type) => type switch
    {
        PoffinType.Rich => (new Color(184, 128, 88, 255), new Color(228, 186, 132, 255)),
        PoffinType.Overripe => (new Color(124, 98, 126, 255), new Color(168, 140, 170, 255)),
        PoffinType.Foul => (new Color(84, 80, 92, 255), new Color(118, 112, 126, 255)),
        PoffinType.Mild => (new Color(240, 214, 150, 255), new Color(252, 242, 214, 255)),
        _ => (FlavorColor((Flavor)((int)type / 5)), Lighter(FlavorColor((Flavor)((int)type % 5)), (int)type / 5 == (int)type % 5 ? 0.45f : 0.12f))
    };

    /// <summary>The colour of the batter of these berries: their flavours mixed, a little cream to it.</summary>
    public static Color BatterColor(IReadOnlyList<ItemData> berries) =>
        PixelCanvas.Mix(FlavorMix(berries), new Color(250, 238, 214, 255), 0.42f);

    // The berries' flavours' colours, each as strong as the flavour
    private static Color FlavorMix(IReadOnlyList<ItemData> berries)
    {
        float r = 0, g = 0, b = 0, w = 0;
        foreach (var berry in berries)
        {
            if (berry.Berry is not { } data) continue;
            int[] f = { data.Spiciness, data.Dryness, data.Sweetness, data.Bitterness, data.Sourness };
            for (int i = 0; i < 5; i++)
            {
                if (f[i] == 0) continue;
                var c = FlavorColor((Flavor)i);
                r += c.R * f[i];
                g += c.G * f[i];
                b += c.B * f[i];
                w += f[i];
            }
        }
        return w > 0 ? new Color((byte)(r / w), (byte)(g / w), (byte)(b / w), (byte)255) : new Color(220, 200, 170, 255);
    }

    // ------------------------------------------------------------------ a Poffin

    /// <summary>A Poffin: a round puff in its colour with a lighter top, a shine, and for a foul one a wisp of smoke.</summary>
    public static void PoffinIcon(Vector2 c, float r, PoffinType type)
    {
        var (body, top) = PoffinColors(type);
        UiShapes.Shadow(new Rectangle(c.X - r, c.Y - r * 0.55f, r * 2, r * 1.5f), r, r * 0.3f, new Vector2(0, r * 0.18f), ShadowColor);
        // The base, a little wider than tall, and the domed top over it
        UiShapes.Shape(new Rectangle(c.X - r, c.Y - r * 0.3f, r * 2, r * 1.15f), r * 0.5f, Darker(body, 0.05f), Darker(body, 0.25f), Darker(body, 0.45f), MathF.Max(2f, r * 0.06f));
        UiShapes.Shape(new Rectangle(c.X - r * 0.86f, c.Y - r * 0.92f, r * 1.72f, r * 1.12f), r * 0.56f, Lighter(top, 0.18f), top, Darker(top, 0.35f), MathF.Max(2f, r * 0.05f));
        // A swirl on the dome and a shine
        UiShapes.Line(c + new Vector2(-r * 0.36f, -r * 0.42f), c + new Vector2(r * 0.18f, -r * 0.56f), r * 0.1f, Lighter(top, 0.5f));
        UiShapes.Circle(c + new Vector2(-r * 0.4f, -r * 0.62f), r * 0.12f, new Color(255, 255, 255, 170));
        if (type == PoffinType.Foul)
        {
            float t = (float)FrameClock.Now;
            for (int i = 0; i < 3; i++)
                UiShapes.Circle(c + new Vector2(r * (0.25f * MathF.Sin(t * 2f + i)), -r * (1.05f + i * 0.28f)), r * (0.14f - i * 0.03f), new Color(120, 116, 128, 150));
        }
    }

    // ------------------------------------------------------------------ the condition

    private static readonly string[] ConditionNames = { "COOL", "BEAUTY", "CUTE", "SMART", "TOUGH" };

    /// <summary>The colour of the condition's shape (the original's is a bright green).</summary>
    public static readonly Color ConditionGreen = new(72, 200, 112, 255);

    // Where each quality points: cool straight up, then round with the clock (beauty, cute, smart, tough)
    private static Vector2 ConditionAxis(int i)
    {
        float a = -MathF.PI / 2f + i * MathF.PI * 2f / 5f;
        return new Vector2(MathF.Cos(a), MathF.Sin(a));
    }

    // A quality's distance from the middle: nothing is a small pentagon, 255 the rim, in a straight line between
    // (SetConditionVecFromStat)
    private static float ConditionReach(int value, float radius) => radius * (0.14f + 0.86f * Math.Clamp(value, 0, Poffins.MaxCondition) / Poffins.MaxCondition);

    /// <summary>
    /// The contest condition as a five-pointed chart: three pentagon rules, a spoke to each quality with its name, and
    /// the Pokémon's own shape filled in green. <paramref name="before"/>, when given, is drawn as a pale outline under
    /// it, the shape growing out of it as <paramref name="grow"/> goes from 0 to 1 (the change a Poffin made).
    /// </summary>
    public static void ConditionChart(Vector2 c, float radius, IReadOnlyList<int> values, IReadOnlyList<int>? before = null, float grow = 1f)
    {
        UiShapes.Circle(c, radius * 1.06f, Disc);
        for (int ring = 3; ring >= 1; ring--)
        {
            float rr = radius * ring / 3f;
            for (int i = 0; i < 5; i++)
                UiShapes.Line(c + ConditionAxis(i) * rr, c + ConditionAxis((i + 1) % 5) * rr, ring == 3 ? 4f : 2.5f, Rule);
        }
        for (int i = 0; i < 5; i++) UiShapes.Line(c, c + ConditionAxis(i) * radius, 2.5f, Rule);

        var now = new Vector2[5];
        for (int i = 0; i < 5; i++)
        {
            float from = before != null ? ConditionReach(before[i], radius) : ConditionReach(0, radius);
            float to = ConditionReach(values[i], radius);
            now[i] = c + ConditionAxis(i) * (from + (to - from) * UiMotion.EaseOut(Math.Clamp(grow, 0f, 1f)));
        }
        for (int i = 0; i < 5; i++) UiShapes.Triangle(c, now[i], now[(i + 1) % 5], ConditionGreen with { A = 200 });
        for (int i = 0; i < 5; i++) UiShapes.Line(now[i], now[(i + 1) % 5], 4f, Darker(ConditionGreen, 0.3f));
        if (before != null)
            for (int i = 0; i < 5; i++)
                UiShapes.Line(c + ConditionAxis(i) * ConditionReach(before[i], radius), c + ConditionAxis((i + 1) % 5) * ConditionReach(before[(i + 1) % 5], radius), 3f, new Color(255, 255, 255, 220));

        for (int i = 0; i < 5; i++)
        {
            var at = c + ConditionAxis(i) * (radius + 44);
            string name = ConditionNames[i];
            float w = UiFonts.Measure(name, 26, UiWeight.Black);
            var chip = new Rectangle(at.X - w / 2f - 16, at.Y - 20, w + 32, 40);
            UiShapes.Fill(chip, 20, FlavorColor((Flavor)i));
            UiFonts.DrawCentered(name, at.X - w / 2f, at.Y, 26, Color.White, UiWeight.Black);
        }
    }

    /// <summary>The sheen: twelve sparkles in a row, as many lit as the sheen gives (<see cref="Poffins.SparklesOf"/>), the lit ones twinkling in turn.</summary>
    public static void SheenSparkles(float x, float y, float size, int sheen)
    {
        int lit = Poffins.SparklesOf(sheen);
        float t = (float)FrameClock.Now;
        for (int i = 0; i < Poffins.SheenSparkles; i++)
        {
            var c = new Vector2(x + i * size * 2.3f + size, y);
            if (i < lit)
            {
                float twinkle = 1f + 0.18f * MathF.Max(0f, MathF.Sin(t * 4f - i * 0.6f));
                Star(c, size * twinkle, new Color(250, 214, 92, 255));
                Star(c, size * 0.45f * twinkle, Color.White);
            }
            else Star(c, size * 0.7f, Rule);
        }
    }

    /// <summary>The condition's values in the chart's order.</summary>
    public static int[] ConditionOf(Pokemon p) => new[] { p.Cool, p.Beauty, p.Cute, p.Smart, p.Tough, p.Sheen };

    /// <summary>A panel with the condition's chart and the sheen under it.</summary>
    public static void ConditionPanel(Rectangle r, int[] values, int[]? before = null, float grow = 1f, string title = "CONDITION")
    {
        Panel(r, 34);
        Label(title, r.X + 44, r.Y + 30);
        float radius = MathF.Min(r.Width * 0.3f, (r.Height - 220) / 2.4f);
        var c = new Vector2(r.X + r.Width / 2f, r.Y + 70 + radius * 1.25f);
        ConditionChart(c, radius, values, before, grow);
        Label("SHEEN", r.X + 44, r.Y + r.Height - 74);
        float size = 15f;
        float width = Poffins.SheenSparkles * size * 2.3f;
        SheenSparkles(r.X + r.Width - 44 - width, r.Y + r.Height - 62, size, values[5]);
    }

    // ------------------------------------------------------------------ the Poffin Case

    private static readonly (string Label, UiIcon Icon, Color Color)[] CaseTabs =
    {
        ("SPICY", UiIcon.Berry, FlavorColor(Flavor.Spicy)),
        ("DRY", UiIcon.Berry, FlavorColor(Flavor.Dry)),
        ("SWEET", UiIcon.Berry, FlavorColor(Flavor.Sweet)),
        ("BITTER", UiIcon.Berry, FlavorColor(Flavor.Bitter)),
        ("SOUR", UiIcon.Berry, FlavorColor(Flavor.Sour)),
        ("ALL", UiIcon.Pouch, new Color(110, 120, 148, 255))
    };

    /// <summary>How tall the case's prompt is under its list (and under the feeding), which leaves the list seven rows.</summary>
    public const float CasePrompt = 80;

    private static readonly string[] Raises = { "Raises coolness", "Raises beauty", "Raises cuteness", "Raises smartness", "Raises toughness" };

    public static void DrawPoffinCase(int sw, int sh, PoffinCaseScreen screen, PoffinCase poffins, Party party, float appear)
    {
        switch (screen.Step)
        {
            case CaseStep.Choose:
                DrawPartyChoice(sw, sh, party, screen.Target, screen.Prompt, _ => null, Math.Clamp(screen.StepAge / 0.35f, 0f, 1f), "Give");
                return;
            case CaseStep.Condition or CaseStep.Eating or CaseStep.After:
                Feeding(sw, sh, screen, party.Members[screen.Target]);
                return;
        }

        Backdrop(sw, sh);
        ScreenTitle("POFFIN CASE");
        Hints(sw - Margin, 44, ("Left / Right", "Flavour"), ("Z", "Choose"), ("Esc", "Back"));
        Tabs(new Rectangle(Margin, ContentTop, sw - Margin * 2, 76), CaseTabs, screen.Tab);

        float slide = (1f - UiMotion.EaseOut(appear)) * 60f;
        const float top = ContentTop + 76 + 24;
        var listed = screen.Listed;

        // ---- The list
        var list = new Rectangle(Margin - slide, top, 920, ContentBottom - top - CasePrompt - Gutter);
        Panel(list, 34);
        bool scrolls = listed.Count > PoffinCaseScreen.VisibleRows;
        if (listed.Count == 0) EmptyNote(list, "No Poffins of this flavour.");
        for (int row = 0; row < PoffinCaseScreen.VisibleRows && screen.First + row < listed.Count; row++)
        {
            int index = screen.First + row;
            var poffin = poffins.All[listed[index]];
            var r = RowRect(list, row, 26, scrolls);
            bool selected = index == screen.Cursor;
            var ink = ListRow(r, selected, poffin.Name);
            var disc = new Vector2(r.X + 52, r.Y + r.Height / 2f);
            UiShapes.Circle(disc, 34, selected ? Color.White : Disc);
            PoffinIcon(disc + new Vector2(0, 4), 22, poffin.Type);
            RowValue(r, $"Lv. {poffin.Level}", ink);
        }
        ScrollBar(list, screen.First, PoffinCaseScreen.VisibleRows, listed.Count, 26);

        // ---- The prompt
        var box = new Rectangle(Margin - slide, ContentBottom - CasePrompt, 920, CasePrompt);
        Panel(box, 30);
        UiFonts.DrawCentered(screen.Prompt, box.X + 52, box.Y + box.Height / 2f, 36, Ink, UiWeight.ExtraBold);

        // ---- The chosen Poffin
        var detail = new Rectangle(Margin + 920 + Gutter + slide, top, sw - Margin * 2 - 920 - Gutter, ContentBottom - top);
        Panel(detail, 34);
        string count = $"{poffins.Count} / {PoffinCase.Capacity}";
        float cw = UiFonts.Measure(count, 24, UiWeight.Black) + 34;
        Tag(detail.X + detail.Width - 44 - cw, detail.Y + 30, count, poffins.IsFull ? Red : Frame, 38);
        if (screen.Current is { } chosen) PoffinDetail(detail, chosen);

        if (screen.Step == CaseStep.Actions)
        {
            var rows = PoffinCaseScreen.ActionNames;
            float height = MenuPanelHeight(rows.Length);
            MenuPanel(detail.X + detail.Width - 360 - 28, detail.Y + detail.Height - height - 28, rows, screen.ActionIndex, 360);
        }
        if (screen.Step == CaseStep.Trash)
            Prompt(sw, sh, "Throw it away?", $"The {screen.Current?.Name} will be gone for good.", new[] { ("YES", Red), ("NO", Blue) }, screen.TrashChoice);
    }

    /// <summary>A Poffin large: its puff on a disc, name and level, smoothness, and its five flavours as bars with what each raises.</summary>
    private static void PoffinDetail(Rectangle detail, Poffin poffin)
    {
        var disc = new Vector2(detail.X + 44 + 96, detail.Y + 44 + 96 + 40);
        UiShapes.Circle(disc, 96, Disc);
        PoffinIcon(disc + new Vector2(0, 14), 62, poffin.Type);
        float x = detail.X + 44 + 192 + 36;
        float size = 44, room = detail.X + detail.Width - 44 - x;
        float nameW = UiFonts.Measure(poffin.Name, size, UiWeight.Black);
        if (nameW > room) size = MathF.Floor(size * room / nameW);
        UiFonts.DrawCentered(poffin.Name, x, detail.Y + 124, size, Ink, UiWeight.Black);
        float tx = x + Tag(x, detail.Y + 166, $"Lv. {poffin.Level}", PoffinColors(poffin.Type).Body, 38) + 12;
        Tag(tx, detail.Y + 166, $"SMOOTH {poffin.Smoothness}", Frame, 38);

        float y = detail.Y + 300;
        UiShapes.Fill(new Rectangle(detail.X + 44, y, detail.Width - 88, 3), 1.5f, Rule);
        y += 34;
        for (int i = 0; i < 5; i++)
        {
            var flavor = (Flavor)i;
            int value = poffin[flavor];
            float cy = y + i * 74;
            UiFonts.DrawCentered(Poffins.NameOf(flavor), detail.X + 44, cy, 28, value > 0 ? Ink : Muted, UiWeight.Black);
            UiFonts.DrawCentered(Raises[i], detail.X + 44, cy + 28, 20, Muted, UiWeight.ExtraBold);
            string n = value.ToString();
            UiFonts.DrawCentered(n, detail.X + 330 - UiFonts.Measure(n, 30, UiWeight.Black), cy + 8, 30, Ink, UiWeight.Black);
            Bar(new Rectangle(detail.X + 350, cy - 4, detail.Width - 350 - 44, 24), value / 60f, FlavorColor(flavor));
        }
    }

    // The team member being fed: its card on the left, its condition (and the change) and the Poffin on the right
    private static void Feeding(int sw, int sh, PoffinCaseScreen screen, Pokemon p)
    {
        Backdrop(sw, sh);
        ScreenTitle("POFFIN CASE");
        Hints(sw - Margin, 44, ("Z", screen.Step == CaseStep.Condition ? "Give" : "Next"), ("Esc", "Back"));
        const float top = ContentTop;

        var who = new Rectangle(Margin, top, 600, 640);
        Panel(who, 34);
        var c = new Vector2(who.X + who.Width / 2f, who.Y + 230);
        BallDisc(c, 180);
        var sprite = PixelArtGenerator.GetPokemonSprite(p.ModelName, isBack: false);
        const int scale = 3;
        float hop = screen.Step == CaseStep.Eating ? MathF.Abs(MathF.Sin(screen.StepAge * 9f)) * 14f : 0f;
        Raylib.DrawTexturePro(sprite, new Rectangle(0, 0, sprite.Width, sprite.Height),
            new Rectangle(MathF.Round(c.X - sprite.Width * scale / 2f), MathF.Round(c.Y - sprite.Height * scale / 2f - hop), sprite.Width * scale, sprite.Height * scale),
            Vector2.Zero, 0f, Color.White);
        NameWithGender(p, who.X + 44, who.Y + 470, 48);
        Level(who.X + who.Width - 44, who.Y + 470, p.Level, 40);
        Label("NATURE", who.X + 44, who.Y + 528);
        UiFonts.Draw(p.Nature.ToString(), who.X + 44, who.Y + 552, 34, Ink, UiWeight.Black);

        // The Poffin it is given
        if (screen.Chosen is { } poffin)
        {
            var card = new Rectangle(Margin, top + 640 + Gutter, 600, ContentBottom - CasePrompt - Gutter - (top + 640 + Gutter));
            Panel(card, 34);
            var disc = new Vector2(card.X + 44 + 50, card.Y + card.Height / 2f);
            UiShapes.Circle(disc, 50, Disc);
            if (screen.Step == CaseStep.Condition) PoffinIcon(disc + new Vector2(0, 6), 32, poffin.Type);
            UiFonts.DrawCentered(poffin.Name, card.X + 170, card.Y + card.Height / 2f - 16, 32, Ink, UiWeight.Black);
            UiFonts.DrawCentered($"Lv. {poffin.Level}  ·  Smooth {poffin.Smoothness}", card.X + 170, card.Y + card.Height / 2f + 22, 24, Muted, UiWeight.ExtraBold);
        }

        var chart = new Rectangle(Margin + 600 + Gutter, top, sw - Margin * 2 - 600 - Gutter, ContentBottom - CasePrompt - Gutter - top);
        float grow = screen.Step == CaseStep.Condition ? 1f : Math.Clamp(screen.StepAge / 0.8f, 0f, 1f);
        var before = screen.Step == CaseStep.Condition ? null : screen.Before;
        ConditionPanel(chart, ConditionOf(p), before, grow);

        var box = new Rectangle(Margin, ContentBottom - CasePrompt, sw - Margin * 2, CasePrompt);
        Panel(box, 30);
        UiFonts.DrawCentered(screen.Prompt, box.X + 52, box.Y + box.Height / 2f, 36, Ink, UiWeight.ExtraBold);
        if (screen.Step != CaseStep.Condition || screen.Message != null) AdvanceArrow(box.X + box.Width - 80, box.Y + box.Height / 2f - 10);
    }

    // ------------------------------------------------------------------ the pot

    // The pot screen's pixels (256 by 192) to the layout's, and the pot's middle on the layout
    private const float PotScale = 3.6f;
    private static readonly Vector2 PotMiddle = new(960, 590);

    private static Vector2 OnPot(float x, float y) => PotMiddle + new Vector2(x - PoffinPot.MiddleX, y - PoffinPot.MiddleY) * PotScale;

    // The batter at each stage: runny and glossy, thickening, then a dough
    private static Color BatterAt(Color batter, int stage) => stage switch
    {
        0 => Lighter(batter, 0.25f),
        1 => batter,
        _ => Darker(PixelCanvas.Mix(batter, new Color(214, 170, 112, 255), 0.35f), 0.08f)
    };

    public static void DrawPoffinCooking(int sw, int sh, PoffinCookingScreen screen)
    {
        var pot = screen.Pot;
        Backdrop(sw, sh);
        ScreenTitle("POFFIN COOKING");
        if (screen.Phase == CookingPhase.Stir) Hints(sw - Margin, 44, ("Left / Right", "Stir"));

        // The table under the pot
        UiShapes.Shadow(new Rectangle(PotMiddle.X - 470, PotMiddle.Y - 430, 940, 900), 450, 40, new Vector2(0, 14), ShadowColor);
        UiShapes.Shape(new Rectangle(PotMiddle.X - 470, PotMiddle.Y - 430, 940, 900), 450, new Color(250, 240, 222, 255), new Color(236, 220, 196, 255), new Color(196, 168, 134, 255), 5);
        for (int i = 0; i < 24; i++)
        {
            var dir = new Vector2(MathF.Cos(i * MathF.PI / 12f), MathF.Sin(i * MathF.PI / 12f));
            UiShapes.Circle(PotMiddle + new Vector2(0, 20) + dir * 430, 7, new Color(226, 120, 112, 255));
        }

        if (pot != null) Pot(screen, pot);

        // What is going on, to the left: the stage, the time, the burns and spills
        if (pot != null && screen.Phase is CookingPhase.Pour or CookingPhase.Stir or CookingPhase.Finish)
        {
            var side = new Rectangle(Margin, ContentTop + 40, 340, 520);
            Panel(side, 30);
            Label("STAGE", side.X + 36, side.Y + 30);
            for (int i = 0; i < PoffinPot.Stages; i++)
            {
                var pip = new Vector2(side.X + 60 + i * 70, side.Y + 96);
                bool past = i < pot.Phase, now = i == Math.Min(pot.Phase, 2) && !pot.Done;
                UiShapes.Circle(pip, 26, past ? Green : now ? Gold : Rule);
                UiFonts.DrawCentered((i + 1).ToString(), pip.X - UiFonts.Measure((i + 1).ToString(), 26, UiWeight.Black) / 2f, pip.Y, 26, past || now ? Color.White : Muted, UiWeight.Black);
            }
            var (m, s, h) = pot.Time;
            Field("TIME", $"{m}:{s:D2}.{h:D2}", side.X + 36, side.Y + 150, 40);
            Field("BURNED", pot.Burns.ToString(), side.X + 36, side.Y + 260, 40);
            Field("OVERFLOWED", pot.Spills.ToString(), side.X + 36, side.Y + 370, 40);
            SpeedGauge(new Rectangle(sw - Margin - 200, ContentTop + 40, 200, 700), pot);
        }

        if (screen.Phase is CookingPhase.Results or CookingPhase.PutAway or CookingPhase.Again or CookingPhase.Notice) Results(sw, sh, screen);

        if (screen.Line is { } line && screen.Phase != CookingPhase.Again)
        {
            if (screen.Phase is CookingPhase.Pour or CookingPhase.Stir or CookingPhase.Finish) Banner(sw, line, screen.Phase == CookingPhase.Stir);
            else DrawDialogue(sw, sh, "", line, true);
        }
        if (screen.Phase == CookingPhase.Again)
        {
            DrawDialogue(sw, sh, "", screen.Line ?? "", false);
            DrawChoices(sw, sh, new[] { "YES", "NO" }, screen.Choice, 1f);
        }
    }

    // A line over the foot of the pot: the berry going in, a warning (red), "Done!"
    private static void Banner(int sw, string line, bool warning)
    {
        float w = UiFonts.Measure(line, 40, UiWeight.Black) + 96;
        var r = new Rectangle(sw / 2f - w / 2f, ContentBottom - 96, w, 84);
        var color = warning ? Red : Frame;
        UiShapes.Shadow(r, 42, 18, new Vector2(0, 6), ShadowColor);
        UiShapes.Shape(r, 42, Lighter(color, 0.12f), Darker(color, 0.1f), Darker(color, 0.35f), 3);
        UiFonts.DrawCentered(line, r.X + 48, r.Y + r.Height / 2f, 40, Color.White, UiWeight.Black);
    }

    // The batter's speed against what burns it and what spills it: a tall gauge, its good band green
    private static void SpeedGauge(Rectangle r, PoffinPot pot)
    {
        Panel(r, 30);
        Label("SPEED", r.X + 36, r.Y + 28);
        var track = new Rectangle(r.X + r.Width / 2f - 26, r.Y + 80, 52, r.Height - 130);
        UiShapes.Fill(track, 26, Track);
        float Y(int speed) => track.Y + track.Height - 6 - (track.Height - 12) * speed / (float)PoffinPot.Top;
        bool spills = pot.Phase < 2;
        // Too slow at the foot (it burns), a good band, and the very top too fast (it spills) but in the last stage
        UiShapes.Fill(new Rectangle(track.X + 6, Y(PoffinPot.Slow), track.Width - 12, track.Y + track.Height - 6 - Y(PoffinPot.Slow)), 20, new Color(240, 140, 60, 255));
        UiShapes.Fill(new Rectangle(track.X + 6, Y(spills ? PoffinPot.Top - 260 : PoffinPot.Top), track.Width - 12, Y(PoffinPot.Slow) - Y(spills ? PoffinPot.Top - 260 : PoffinPot.Top)), 6, Green);
        if (spills) UiShapes.Fill(new Rectangle(track.X + 6, Y(PoffinPot.Top), track.Width - 12, Y(PoffinPot.Top - 260) - Y(PoffinPot.Top)), 20, new Color(80, 150, 240, 255));
        float at = Y(Math.Abs(pot.Speed));
        var marker = new Rectangle(track.X - 18, at - 9, track.Width + 36, 18);
        UiShapes.Shadow(marker, 9, 8, new Vector2(0, 3), ShadowColor);
        UiShapes.Shape(marker, 9, Color.White, PanelBottom, Frame, 3);
        string mark = spills ? "SPILLS" : "FAST";
        UiFonts.DrawCentered(mark, r.X + r.Width / 2f - UiFonts.Measure(mark, 18, UiWeight.Black) / 2f, r.Y + 64, 18, Muted, UiWeight.Black);
        UiFonts.DrawCentered("BURNS", r.X + r.Width / 2f - UiFonts.Measure("BURNS", 18, UiWeight.Black) / 2f, r.Y + r.Height - 30, 18, Muted, UiWeight.Black);
    }

    // The pot from above: flames round it, the iron ring, the batter turning with its swirls, the spoon, the arrow
    private static void Pot(PoffinCookingScreen screen, PoffinPot pot)
    {
        float t = screen.PhaseTime;
        int stage = Math.Min(pot.Phase, 2);
        bool cooking = screen.Phase is CookingPhase.Pour or CookingPhase.Stir;

        // Flames peeking out under the rim, more and taller each stage
        if (cooking)
        {
            int count = 12 + stage * 4;
            float tall = 34 + stage * 22;
            for (int i = 0; i < count; i++)
            {
                float a = i * MathF.PI * 2f / count + 0.13f;
                float flicker = 0.75f + 0.25f * MathF.Sin(t * 13f + i * 1.7f);
                var dir = new Vector2(MathF.Cos(a), MathF.Sin(a));
                var side = new Vector2(-dir.Y, dir.X);
                var foot = PotMiddle + dir * 330;
                var tip = PotMiddle + dir * (340 + tall * flicker);
                UiShapes.Triangle(foot - side * 26, foot + side * 26, tip, new Color(246, 140, 52, 230), 4);
                UiShapes.Triangle(foot - side * 13, foot + side * 13, PotMiddle + dir * (336 + tall * 0.6f * flicker), new Color(254, 222, 96, 240), 3);
            }
        }

        // The iron ring of the pot
        UiShapes.Circle(PotMiddle + new Vector2(0, 10), 344, new Color(20, 20, 30, 90));
        UiShapes.Circle(PotMiddle, 340, new Color(70, 72, 86, 255));
        UiShapes.Ring(PotMiddle, 340, 10, new Color(42, 42, 54, 255));
        UiShapes.Circle(PotMiddle, 322, new Color(44, 46, 58, 255));

        // The batter, as wide as the stylus may stir it (72 to 88 pixels), turned to where the pot has it
        var batter = BatterColor(screen.Berries);
        var color = BatterAt(batter, stage);
        if (pot.Fade < 31 && stage < 2) color = PixelCanvas.Mix(BatterAt(batter, stage + 1), color, pot.Fade / 31f);
        float over = Math.Max(0, Math.Abs(pot.Speed) - PoffinPot.Slow) / (float)(PoffinPot.Top - PoffinPot.Slow);
        float radius = (72 + 16 * over) * PotScale;
        UiShapes.Shape(new Rectangle(PotMiddle.X - radius, PotMiddle.Y - radius, radius * 2, radius * 2), radius, Lighter(color, 0.12f), Darker(color, 0.1f), Darker(color, 0.28f), 4);

        float angle = (screen.LastAngle + (pot.Angle - screen.LastAngle) * screen.Between) / 65536f * MathF.PI * 2f;
        var swirl = Darker(color, 0.16f);
        var gloss = Lighter(color, 0.35f);
        for (int arm = 0; arm < 3; arm++)
        {
            float a0 = angle + arm * MathF.PI * 2f / 3f;
            for (int k = 0; k < 9; k++)
            {
                float r0 = radius * (0.18f + k * 0.085f), r1 = radius * (0.18f + (k + 1) * 0.085f);
                float b0 = a0 + k * 0.32f, b1 = a0 + (k + 1) * 0.32f;
                UiShapes.Line(PotMiddle + new Vector2(MathF.Cos(b0), MathF.Sin(b0)) * r0, PotMiddle + new Vector2(MathF.Cos(b1), MathF.Sin(b1)) * r1, 9 - k * 0.6f, stage == 2 ? swirl : gloss);
            }
        }
        if (stage < 2) UiShapes.Circle(PotMiddle + new Vector2(-radius * 0.35f, -radius * 0.4f), radius * 0.08f, new Color(255, 255, 255, 90));

        // The berry falling in, and the ripple it leaves
        if (screen.Phase == CookingPhase.Pour)
        {
            float fall = Math.Clamp(t / 0.6f, 0f, 1f);
            if (fall < 1f && screen.Berries.Count > 0)
                DrawBerry(PotMiddle + new Vector2(0, -420 * (1f - fall)), 34, Darker(FlavorMix(new[] { screen.Berries[^1] }), 0.12f));
            else
            {
                float ring = Math.Clamp((t - 0.6f) / 0.8f, 0f, 1f);
                UiShapes.Ring(PotMiddle, 40 + ring * 200, 6, Lighter(color, 0.5f) with { A = (byte)(200 * (1f - ring)) });
            }
        }

        // Smoke where it burned, and batter slopping over the rim where it spilled
        if (screen.SinceBurn < 1.2f || screen.SinceWarning < 0.8f)
        {
            float s = screen.SinceBurn < 1.2f ? screen.SinceBurn / 1.2f : screen.SinceWarning / 0.8f;
            bool burnt = screen.SinceBurn < 1.2f;
            for (int i = 0; i < 6; i++)
            {
                var at = PotMiddle + new Vector2(MathF.Cos(i * 1.1f) * radius * 0.5f, MathF.Sin(i * 1.7f) * radius * 0.4f - s * 120);
                UiShapes.Circle(at, 26 + s * 30, (burnt ? new Color(60, 56, 60, 255) : new Color(150, 146, 150, 255)) with { A = (byte)(170 * (1f - s)) });
            }
        }
        if (screen.SinceSpill < 0.9f)
        {
            float s = screen.SinceSpill / 0.9f;
            for (int i = 0; i < 8; i++)
            {
                float a = i * MathF.PI / 4f + 0.4f;
                var dir = new Vector2(MathF.Cos(a), MathF.Sin(a));
                UiShapes.Circle(PotMiddle + dir * (radius + 10 + s * 90), 22 * (1f - s * 0.5f), Lighter(color, 0.1f) with { A = (byte)(255 * (1f - s)) });
            }
        }

        // The spoon at the stylus, its handle out over the rim
        if (screen.Phase == CookingPhase.Stir)
        {
            var bowl = OnPot(screen.Stylus.X, screen.Stylus.Y);
            var outward = bowl - PotMiddle;
            outward = outward.LengthSquared() > 1 ? Vector2.Normalize(outward) : new Vector2(0, -1);
            var end = bowl + outward * 300;
            UiShapes.Line(bowl, end, 30, new Color(138, 92, 56, 255));
            UiShapes.Line(bowl, end, 18, new Color(196, 142, 92, 255));
            UiShapes.Circle(bowl, 40, new Color(138, 92, 56, 255));
            UiShapes.Circle(bowl, 30, new Color(206, 154, 102, 255));
        }

        // The arrow round the pot: large and pulsing as it is drawn again, then a smaller one; red while the batter
        // goes the other way
        if (screen.Phase == CookingPhase.Stir)
        {
            bool fresh = screen.SinceArrow < 1.2f;
            bool wrong = pot.WrongWay && pot.Speed != 0;
            float pulse = fresh ? 1f + 0.08f * MathF.Sin(screen.SinceArrow * 18f) : 1f;
            var c = wrong && (int)(t * 4) % 2 == 0 ? Red : fresh ? Gold : Lighter(Gold, 0.2f) with { A = 210 };
            CurvedArrow(PotMiddle, 400 * pulse, -MathF.PI / 2f - 0.9f, 1.8f, pot.Backward, fresh ? 26 : 18, c);
        }
    }

    // A berry in the colour of its flavours
    private static void DrawBerry(Vector2 c, float r, Color color)
    {
        UiShapes.Circle(c, r, Darker(color, 0.25f));
        UiShapes.Circle(c, r - 4, color);
        UiShapes.Circle(c + new Vector2(-r * 0.3f, -r * 0.3f), r * 0.3f, new Color(255, 255, 255, 120));
        UiShapes.Line(c + new Vector2(0, -r), c + new Vector2(r * 0.4f, -r * 1.5f), 6, new Color(70, 140, 70, 255));
    }

    /// <summary>An arrow along an arc round <paramref name="c"/>: from <paramref name="start"/> over <paramref name="sweep"/> radians, its head with the clock or against it.</summary>
    private static void CurvedArrow(Vector2 c, float radius, float start, float sweep, bool backward, float thickness, Color color)
    {
        const int steps = 14;
        for (int i = 0; i < steps; i++)
        {
            float a0 = start + sweep * i / steps, a1 = start + sweep * (i + 1) / steps;
            UiShapes.Line(c + new Vector2(MathF.Cos(a0), MathF.Sin(a0)) * radius, c + new Vector2(MathF.Cos(a1), MathF.Sin(a1)) * radius, thickness, color);
        }
        float tipAngle = backward ? start : start + sweep;
        var tip = c + new Vector2(MathF.Cos(tipAngle), MathF.Sin(tipAngle)) * radius;
        var along = new Vector2(-MathF.Sin(tipAngle), MathF.Cos(tipAngle)) * (backward ? -1f : 1f);
        var outward = new Vector2(MathF.Cos(tipAngle), MathF.Sin(tipAngle));
        UiShapes.Triangle(tip + along * thickness * 2.2f, tip - along * thickness * 0.4f + outward * thickness * 1.6f, tip - along * thickness * 0.4f - outward * thickness * 1.6f, color, 3);
    }

    // What came out: the time, the spills and burns, then the Poffin
    private static void Results(int sw, int sh, PoffinCookingScreen screen)
    {
        var pot = screen.Pot!;
        float appear = screen.Phase == CookingPhase.Results ? Math.Clamp(screen.PhaseTime / 0.3f, 0f, 1f) : 1f;
        Dim(sw, sh, (int)(140 * appear));
        var r = new Rectangle(sw / 2f - 560, 170 + (1f - UiMotion.EaseOut(appear)) * 50f, 1120, 600);
        Panel(r, 36);
        UiFonts.Draw("RESULTS", r.X + 56, r.Y + 40, 48, Ink, UiWeight.Black);

        var (m, s, h) = pot.Time;
        float y = r.Y + 140;
        void Row(string caption, string value)
        {
            UiFonts.DrawCentered(caption, r.X + 56, y, 32, Muted, UiWeight.ExtraBold);
            UiFonts.DrawCentered(value, r.X + 520 - UiFonts.Measure(value, 36, UiWeight.Black), y, 36, Ink, UiWeight.Black);
            UiShapes.Fill(new Rectangle(r.X + 56, y + 34, 464, 3), 1.5f, Rule);
            y += 86;
        }
        Row("Cooking time", $"{m}:{s:D2}.{h:D2}");
        Row("Overflowed", pot.Spills == 1 ? "1 time" : $"{pot.Spills} times");
        Row("Burned", pot.Burns == 1 ? "1 time" : $"{pot.Burns} times");
        if (screen.Made is { } made) Row("Smoothness", made.Smoothness.ToString());

        bool shown = screen.Phase != CookingPhase.Results || screen.PhaseTime >= PoffinCookingScreen.PoffinShown;
        if (shown && screen.Made is { } poffin)
        {
            float pop = screen.Phase == CookingPhase.Results ? Math.Clamp((screen.PhaseTime - PoffinCookingScreen.PoffinShown) / 0.35f, 0f, 1f) : 1f;
            var disc = new Vector2(r.X + 830, r.Y + 270);
            UiShapes.Circle(disc, 150, Disc);
            PoffinIcon(disc + new Vector2(0, 20), 96 * (0.6f + 0.4f * UiMotion.EaseOut(pop)), poffin.Type);
            string name = poffin.Name;
            UiFonts.DrawCentered(name, disc.X - UiFonts.Measure(name, 38, UiWeight.Black) / 2f, r.Y + 470, 38, Ink, UiWeight.Black);
            string level = screen.Cooks == 1 ? $"Lv. {poffin.Level}" : $"Lv. {poffin.Level}  ×{screen.Cooks}";
            UiFonts.DrawCentered(level, disc.X - UiFonts.Measure(level, 30, UiWeight.Black) / 2f, r.Y + 518, 30, Muted, UiWeight.Black);
        }
        if (screen.Phase == CookingPhase.Results && screen.PhaseTime >= PoffinCookingScreen.CardShown) AdvanceArrow(r.X + r.Width - 90, r.Y + r.Height - 60);
    }
}
