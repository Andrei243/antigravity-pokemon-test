using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.SuperContests;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// A Super Contest on screen (plan 06 · R17a; style guide, "Super Contests"): the line-up, the dressing-up from the
/// Fashion Case, each contestant's look and its hearts, the dance's bar and its dancers, the acting's judges, moves
/// and voltage, and the results' board. Our own pictures: an accessory is a small sign in its own colour, a heart a
/// heart, a judge a name over a meter of five lights.
/// </summary>
internal static partial class ModernUi
{
    /// <summary>A quality's colour, the condition chart's: cool red, beauty blue, cute pink, smart green, tough yellow.</summary>
    public static Color QualityColor(ContestStat quality) => FlavorColor((Flavor)(int)quality);

    private static readonly Color HeartRed = new(236, 84, 110, 255);
    private static readonly Color StageDark = new(30, 34, 58, 255);

    // ------------------------------------------------------------------ small signs

    /// <summary>A heart about 2 × <paramref name="s"/> across.</summary>
    public static void Heart(Vector2 c, float s, Color color)
    {
        UiShapes.Circle(c + new Vector2(-s * 0.48f, -s * 0.3f), s * 0.56f, color);
        UiShapes.Circle(c + new Vector2(s * 0.48f, -s * 0.3f), s * 0.56f, color);
        UiShapes.Triangle(c + new Vector2(-s * 0.98f, -s * 0.02f), c + new Vector2(s * 0.98f, -s * 0.02f), c + new Vector2(0, s * 1.02f), color);
    }

    /// <summary>A row of hearts: so many won of so many, the first <paramref name="shown"/> filled (the rest of those won come up one by one).</summary>
    public static void HeartRow(float x, float centerY, int won, int of, float size, float shown = 99f)
    {
        for (int i = 0; i < of; i++)
        {
            var c = new Vector2(x + size + i * size * 2.5f, centerY);
            bool lit = i < won && i < shown;
            float pop = lit && shown - i < 1f ? 1f + 0.35f * (1f - (shown - i)) : 1f;
            Heart(c, size * pop, lit ? HeartRed : Rule);
        }
    }

    private static readonly (string Word, Color Color)[] AccessoryColors =
    {
        ("White", new Color(244, 246, 250, 255)), ("Yellow", new Color(244, 208, 70, 255)), ("Pink", new Color(244, 142, 184, 255)),
        ("Brown", new Color(150, 104, 72, 255)), ("Black", new Color(64, 62, 78, 255)), ("Orange", new Color(240, 148, 62, 255)),
        ("Blue", new Color(90, 150, 236, 255)), ("Green", new Color(92, 186, 108, 255)), ("Purple", new Color(160, 112, 214, 255)),
        ("Red", new Color(232, 82, 78, 255)), ("Gold", new Color(236, 190, 70, 255)), ("Glass", new Color(170, 220, 240, 255)),
        ("Silk", new Color(236, 230, 246, 255)), ("Snow", new Color(214, 236, 250, 255)), ("Glitter", new Color(240, 214, 120, 255)),
        ("Shiny", new Color(200, 226, 240, 255)), ("Fire", new Color(240, 120, 60, 255)), ("Leaf", new Color(98, 180, 96, 255)),
        ("Tree", new Color(78, 150, 86, 255)), ("Mushroom", new Color(214, 120, 92, 255)), ("Scale", new Color(120, 196, 206, 255)),
        ("Pebble", new Color(168, 170, 182, 255)), ("Boulder", new Color(150, 140, 132, 255)), ("Coin", new Color(236, 190, 70, 255)),
        ("Candy", new Color(244, 150, 190, 255)), ("Dewdrop", new Color(140, 200, 240, 255)), ("Claw", new Color(226, 214, 190, 255)),
        ("Hown", new Color(226, 214, 190, 255)), ("Stump", new Color(150, 110, 78, 255)), ("Sparks", new Color(246, 216, 80, 255)),
        ("Determination", new Color(232, 90, 80, 255)), ("Spoon", new Color(196, 204, 216, 255)), ("Smoke", new Color(176, 176, 190, 255)),
        ("Poison", new Color(160, 112, 214, 255)), ("Eerie", new Color(124, 108, 190, 255)), ("Spring", new Color(170, 180, 196, 255)),
        ("Seashell", new Color(246, 196, 176, 255)), ("Note", new Color(110, 196, 214, 255)), ("Confetti", new Color(244, 150, 120, 255)),
        ("Parasol", new Color(240, 130, 160, 255)), ("Umbrella", new Color(140, 120, 104, 255)), ("Spotlight", new Color(248, 224, 120, 255)),
        ("Cape", new Color(204, 70, 80, 255)), ("Mike", new Color(120, 124, 140, 255)), ("Surfboard", new Color(90, 170, 230, 255)),
        ("Carpet", new Color(196, 80, 100, 255)), ("Pipe", new Color(150, 110, 78, 255)), ("Bed", new Color(246, 196, 214, 255)),
        ("Mirror", new Color(196, 214, 230, 255)), ("Board", new Color(176, 140, 104, 255)), ("Lace", new Color(244, 240, 232, 255)),
        ("Top Hat", new Color(64, 62, 78, 255)), ("Heroic", new Color(232, 82, 78, 255)), ("Professor", new Color(140, 110, 84, 255)),
        ("Award", new Color(196, 204, 216, 255)), ("Cube", new Color(110, 190, 200, 255)), ("Turtwig", new Color(108, 190, 96, 255)),
        ("Chimchar", new Color(240, 140, 70, 255)), ("Piplup", new Color(90, 150, 236, 255)), ("Flag", new Color(232, 82, 78, 255)),
        ("Crown", new Color(236, 190, 70, 255)), ("Tiara", new Color(236, 200, 220, 255)), ("Comet", new Color(120, 170, 240, 255)),
        ("Specs", new Color(120, 124, 140, 255))

    };

    /// <summary>
    /// An accessory's sign (our own): its kind from its name (a fluff, a pebble, a feather, a flower, a hat, a
    /// stage…), in the colour its name gives or its kind's own.
    /// </summary>
    public static void AccessoryIcon(Vector2 c, float r, int accessory)
    {
        string name = SuperContestDatabase.AccessoryName(accessory);
        Color color = new(196, 170, 226, 255);
        foreach (var (word, tint) in AccessoryColors)
            if (name.Contains(word, StringComparison.Ordinal))
            {
                color = tint;
                break;
            }
        Color dark = Darker(color, 0.35f), light = Lighter(color, 0.45f);
        bool Is(params string[] words) => words.Any(w => name.Contains(w, StringComparison.Ordinal));

        if (Is("Fluff", "Smoke", "Bed"))
        {
            UiShapes.Circle(c + new Vector2(-r * 0.42f, r * 0.12f), r * 0.52f, dark);
            UiShapes.Circle(c + new Vector2(r * 0.42f, r * 0.12f), r * 0.52f, dark);
            UiShapes.Circle(c + new Vector2(0, -r * 0.2f), r * 0.62f, dark);
            UiShapes.Circle(c + new Vector2(-r * 0.4f, r * 0.1f), r * 0.42f, color);
            UiShapes.Circle(c + new Vector2(r * 0.4f, r * 0.1f), r * 0.42f, color);
            UiShapes.Circle(c + new Vector2(0, -r * 0.2f), r * 0.52f, light);
        }
        else if (Is("Pebble", "Boulder", "Stump", "Coin", "Mirror Ball", "Candy", "Seashell"))
        {
            UiShapes.Fill(new Rectangle(c.X - r * 0.8f, c.Y - r * 0.6f, r * 1.6f, r * 1.3f), r * 0.5f, dark);
            UiShapes.Fill(new Rectangle(c.X - r * 0.66f, c.Y - r * 0.48f, r * 1.32f, r * 1.04f), r * 0.42f, color);
            UiShapes.Circle(c + new Vector2(-r * 0.25f, -r * 0.18f), r * 0.16f, light);
        }
        else if (Is("Feather", "Leaf", "Scale", "Surfboard", "Spoon"))
        {
            UiShapes.Turned(c, r * 0.38f, r * 0.9f, r * 0.38f, 35f, dark);
            UiShapes.Turned(c, r * 0.28f, r * 0.78f, r * 0.28f, 35f, color);
            UiShapes.Line(c + new Vector2(-r * 0.4f, r * 0.58f), c + new Vector2(r * 0.4f, -r * 0.58f), MathF.Max(2f, r * 0.08f), light);
        }
        else if (Is("Moustache", "Beard", "Specs", "Barrette", "Headband", "Cape", "Carpet", "Flag"))
        {
            UiShapes.Triangle(c, c + new Vector2(-r * 0.95f, -r * 0.55f), c + new Vector2(-r * 0.95f, r * 0.55f), dark);
            UiShapes.Triangle(c, c + new Vector2(r * 0.95f, -r * 0.55f), c + new Vector2(r * 0.95f, r * 0.55f), dark);
            UiShapes.Triangle(c, c + new Vector2(-r * 0.8f, -r * 0.4f), c + new Vector2(-r * 0.8f, r * 0.4f), color);
            UiShapes.Triangle(c, c + new Vector2(r * 0.8f, -r * 0.4f), c + new Vector2(r * 0.8f, r * 0.4f), color);
            UiShapes.Circle(c, r * 0.22f, light);
        }
        else if (Is("Flower", "Crystal", "Sparks", "Powder", "Confetti", "Comet", "Note", "Spotlight"))
        {
            for (int i = 0; i < 5; i++)
            {
                float a = -MathF.PI / 2f + i * MathF.PI * 2f / 5f;
                UiShapes.Circle(c + new Vector2(MathF.Cos(a), MathF.Sin(a)) * r * 0.5f, r * 0.36f, color);
            }
            UiShapes.Circle(c, r * 0.3f, Is("Flower") ? new Color(250, 222, 96, 255) : light);
        }
        else if (Is("Claw", "Hown", "Horn", "Fire", "Determination", "Dewdrop", "Extract", "Eerie", "Spring", "Pipe", "Mike"))
        {
            UiShapes.Triangle(c + new Vector2(0, -r * 0.95f), c + new Vector2(-r * 0.62f, r * 0.35f), c + new Vector2(r * 0.62f, r * 0.35f), dark);
            UiShapes.Circle(c + new Vector2(0, r * 0.3f), r * 0.62f, dark);
            UiShapes.Triangle(c + new Vector2(0, -r * 0.7f), c + new Vector2(-r * 0.46f, r * 0.3f), c + new Vector2(r * 0.46f, r * 0.3f), color);
            UiShapes.Circle(c + new Vector2(0, r * 0.3f), r * 0.48f, color);
            UiShapes.Circle(c + new Vector2(-r * 0.16f, r * 0.2f), r * 0.14f, light);
        }
        else if (Is("Mushroom", "Parasol", "Umbrella", "Balloon", "Tree"))
        {
            UiShapes.Fill(new Rectangle(c.X - r * 0.12f, c.Y - r * 0.1f, r * 0.24f, r * 1.0f), r * 0.1f, Darker(color, 0.55f));
            UiShapes.Circle(c + new Vector2(0, -r * 0.3f), r * 0.68f, dark);
            UiShapes.Circle(c + new Vector2(0, -r * 0.3f), r * 0.56f, color);
            UiShapes.Circle(c + new Vector2(-r * 0.2f, -r * 0.46f), r * 0.14f, light);
        }
        else if (Is("Hat", "Headdress", "Veil", "Crown", "Tiara", "Mask", "Board"))
        {
            UiShapes.Fill(new Rectangle(c.X - r * 0.9f, c.Y + r * 0.22f, r * 1.8f, r * 0.34f), r * 0.16f, dark);
            UiShapes.Fill(new Rectangle(c.X - r * 0.56f, c.Y - r * 0.8f, r * 1.12f, r * 1.1f), r * 0.2f, dark);
            UiShapes.Fill(new Rectangle(c.X - r * 0.44f, c.Y - r * 0.68f, r * 0.88f, r * 0.9f), r * 0.14f, color);
            UiShapes.Fill(new Rectangle(c.X - r * 0.44f, c.Y - r * 0.02f, r * 0.88f, r * 0.2f), 0, light);
        }
        else
        {
            // A stage, a pedestal, a podium: a block wider at its foot
            UiShapes.Fill(new Rectangle(c.X - r * 0.9f, c.Y + r * 0.1f, r * 1.8f, r * 0.6f), r * 0.14f, dark);
            UiShapes.Fill(new Rectangle(c.X - r * 0.66f, c.Y - r * 0.5f, r * 1.32f, r * 0.7f), r * 0.14f, color);
            UiShapes.Fill(new Rectangle(c.X - r * 0.66f, c.Y - r * 0.5f, r * 1.32f, r * 0.2f), r * 0.1f, light);
        }
    }

    // The player's accessories go on in a ring round the Pokémon; an opponent's stand where its dressing puts them
    // (the original's picture of 256 by 192, its middle on the Pokémon)
    private static void Dressed(Vector2 c, int scale, Contestant who, IReadOnlyList<int> worn, float hop = 0f)
    {
        float unit = PokemonSprites.Size * scale / 128f;
        var placed = who.Dressing?.Placed.ToList();
        void Accessory(int i, bool behind)
        {
            Vector2 at;
            bool back;
            if (placed != null && i < placed.Count)
            {
                at = c + new Vector2(placed[i].X - 128, placed[i].Y - 96) * unit * 0.9f;
                back = placed[i].Priority < who.Dressing!.PokemonPriority;
            }
            else
            {
                float a = -MathF.PI / 2f + (i % 10) * MathF.PI * 2f / 10f + (i / 10) * 0.31f;
                at = c + new Vector2(MathF.Cos(a) * 62f, MathF.Sin(a) * 54f - 6f) * unit * (1f + 0.22f * (i / 10));
                back = MathF.Sin(a) < -0.3f;
            }
            if (back == behind) AccessoryIcon(at, 17f * unit, worn[i]);
        }
        for (int i = 0; i < worn.Count; i++) Accessory(i, true);
        PixelArt(PixelArtGenerator.SpriteOf(who.Pokemon), c, scale, hop);
        for (int i = 0; i < worn.Count; i++) Accessory(i, false);
    }

    // ------------------------------------------------------------------ the frame every phase shares

    private static void ContestFrame(int sw, int sh, SuperContestScreen screen, params (string Key, string Label)[] hints)
    {
        var contest = screen.Contest!;
        Backdrop(sw, sh);
        ScreenTitle("SUPER CONTEST");
        float x = 64 + UiFonts.Measure("SUPER CONTEST", 52, UiWeight.Black) + 32;
        x += Tag(x, 50, contest.IsPractice ? "PRACTICE" : $"{contest.Rank.ToString().ToUpperInvariant()} RANK", Frame, 42) + 12;
        Tag(x, 50, contest.Category.ToString().ToUpperInvariant(), QualityColor(contest.Category), 42);
        if (hints.Length > 0) Hints(sw - Margin, 44, hints);
    }

    /// <summary>How tall the announcer's box is, at the foot of every phase.</summary>
    public const float ContestBox = 148;

    private static void ContestLine(int sw, string line, bool arrow)
    {
        var r = new Rectangle(Margin, ContentBottom - ContestBox, sw - Margin * 2, ContestBox);
        Panel(r, 30);
        if (line.Length > 0) DrawWrapped(line, r.X + 52, r.Y + 30, r.Width - 150, 34, Ink, 46, UiWeight.ExtraBold);
        if (arrow) AdvanceArrow(r.X + r.Width - 70, r.Y + r.Height - 52);
    }

    private static string EntryTag(int contestant) => $"No. {SuperContest.EntryNumber(contestant)}";

    // A contestant's card: the entry number, the Pokémon on its disc, its name and its trainer's
    private static void ContestantCard(Rectangle r, SuperContestScreen screen, int contestant, bool selected, float hop = 0f, bool dressed = false)
    {
        var who = screen.Contest!.Contestants[contestant];
        Card(r, 30, selected, QualityColor(screen.Contest.Category));
        Tag(r.X + 24, r.Y + 22, EntryTag(contestant), who.IsPlayer ? Selection : Frame, 38);
        var c = new Vector2(r.X + r.Width / 2f, r.Y + 60 + (r.Height - 190) / 2f);
        float radius = MathF.Min(r.Width * 0.38f, (r.Height - 190) / 2f);
        BallDisc(c, radius);
        int scale = radius >= 150 ? 2 : 1;
        if (dressed) Dressed(c, scale, who, who.IsPlayer && screen.Phase == SuperContestPhase.Dressing ? screen.Worn : who.Accessories, hop);
        else PixelArt(PixelArtGenerator.SpriteOf(who.Pokemon), c, scale, hop);
        float size = 34;
        string name = who.Pokemon.DisplayName;
        float w = UiFonts.Measure(name, size, UiWeight.Black);
        if (w > r.Width - 48) size = MathF.Floor(size * (r.Width - 48) / w);
        UiFonts.DrawCentered(name, r.X + (r.Width - UiFonts.Measure(name, size, UiWeight.Black)) / 2f, r.Y + r.Height - 96, size, Ink, UiWeight.Black);
        float tw = UiFonts.Measure(who.Trainer, 24, UiWeight.ExtraBold);
        UiFonts.DrawCentered(who.Trainer, r.X + (r.Width - tw) / 2f, r.Y + r.Height - 54, 24, Muted, UiWeight.ExtraBold);
    }

    // The four cards across, entry number 1 at the left (the fourth of the four) to the player at the right
    private static Rectangle CardRect(int sw, int contestant, float top, float height)
    {
        float w = (sw - Margin * 2 - Gutter * 3) / 4f;
        int column = ContestRules.Contestants - 1 - contestant;
        return new Rectangle(Margin + column * (w + Gutter), top, w, height);
    }

    // ------------------------------------------------------------------ the screen

    public static void DrawSuperContest(int sw, int sh, SuperContestScreen screen)
    {
        if (screen.Contest == null) return;
        switch (screen.Phase)
        {
            case SuperContestPhase.Lineup or SuperContestPhase.Theme:
                ContestLineup(sw, sh, screen);
                break;
            case SuperContestPhase.Dressing:
                ContestDressing(sw, sh, screen);
                break;
            case SuperContestPhase.Looks:
                ContestLooks(sw, sh, screen);
                break;
            case SuperContestPhase.DanceLead or SuperContestPhase.Dance or SuperContestPhase.DanceScores:
                ContestDance(sw, sh, screen);
                break;
            case SuperContestPhase.ActingStart or SuperContestPhase.ActingMove or SuperContestPhase.ActingJudge or SuperContestPhase.ActingShow or SuperContestPhase.ActingScores:
                ContestActing(sw, sh, screen);
                break;
            // A contest that is over keeps its board up while the game fades back to the field
            case SuperContestPhase.Results or SuperContestPhase.Award or SuperContestPhase.PracticeResult or SuperContestPhase.Done:
                ContestResults(sw, sh, screen);
                break;
        }
    }

    private static void ContestLineup(int sw, int sh, SuperContestScreen screen)
    {
        ContestFrame(sw, sh, screen, ("Z", "Next"));
        float top = ContentTop, height = ContentBottom - ContestBox - Gutter - top;
        float t = (float)FrameClock.Now;
        for (int c = 0; c < ContestRules.Contestants; c++)
        {
            bool on = screen.Phase == SuperContestPhase.Lineup && screen.Showing == c;
            ContestantCard(CardRect(sw, c, top, height), screen, c, on, on ? MathF.Abs(MathF.Sin(t * 7f)) * 14f : 0f);
        }
        if (screen.Phase == SuperContestPhase.Theme)
        {
            // The theme on a band across the cards, which stand back
            Dim(sw, sh, 110);
            var band = new Rectangle(Margin + 160, top + height / 2f - 110, sw - Margin * 2 - 320, 220);
            Panel(band, 40, 0f, 6f, QualityColor(screen.Contest!.Category));
            Label("THE THEME", band.X + 56, band.Y + 34);
            string theme = SuperContestDatabase.Themes[screen.Contest.Theme].Name;
            UiFonts.DrawCentered(theme, band.X + 56, band.Y + 118, 76, Ink, UiWeight.Black);
            string limit = $"{screen.Limit} ACCESSORIES  ·  {ContestRules.DressingSeconds} SECONDS";
            Tag(band.X + band.Width - 56 - UiFonts.Measure(limit, 24, UiWeight.Black) - 38, band.Y + 150, limit, Frame, 42);
        }
        ContestLine(sw, screen.Line, true);
    }

    private static void ContestDressing(int sw, int sh, SuperContestScreen screen)
    {
        var contest = screen.Contest!;
        ContestFrame(sw, sh, screen, ("Z", "Put on"), ("X", "Take off"), ("Enter", "Done"));
        float top = ContentTop, bottom = ContentBottom;

        // ---- The Pokémon and what it wears
        var stage = new Rectangle(Margin, top, 700, bottom - top);
        Panel(stage, 34);
        Label("THEME", stage.X + 44, stage.Y + 30);
        UiFonts.Draw(SuperContestDatabase.Themes[contest.Theme].Name, stage.X + 44, stage.Y + 54, 46, Ink, UiWeight.Black);
        var c = new Vector2(stage.X + stage.Width / 2f, stage.Y + 420);
        BallDisc(c, 250);
        Dressed(c, 3, contest.Player, screen.Worn);
        string worn = $"{screen.Worn.Count} / {screen.Limit}";
        Tag(stage.X + stage.Width - 44 - UiFonts.Measure(worn, 24, UiWeight.Black) - 38, stage.Y + 34, worn, screen.Worn.Count >= screen.Limit ? Red : Frame, 42);
        // The clock: a bar that runs down, red for the last ten seconds
        Label("TIME", stage.X + 44, stage.Y + stage.Height - 96);
        string seconds = $"{(int)MathF.Ceiling(screen.TimeLeft)}";
        UiFonts.DrawCentered(seconds, stage.X + stage.Width - 44 - UiFonts.Measure(seconds, 40, UiWeight.Black), stage.Y + stage.Height - 86, 40, screen.TimeLeft <= 10 ? Red : Ink, UiWeight.Black);
        Bar(new Rectangle(stage.X + 44, stage.Y + stage.Height - 60, stage.Width - 88, 24), screen.TimeLeft / ContestRules.DressingSeconds, screen.TimeLeft <= 10 ? Red : Green);

        // ---- The Fashion Case
        var box = new Rectangle(Margin + 700 + Gutter, top, sw - Margin * 2 - 700 - Gutter, bottom - top);
        Panel(box, 34);
        Label("FASHION CASE", box.X + 44, box.Y + 30);
        var held = screen.InCase;
        if (held.Count == 0) EmptyNote(new Rectangle(box.X, box.Y, box.Width, box.Height - 120), "No Accessories yet.");
        float cell = (box.Width - 88 - (SuperContestScreen.CaseColumns - 1) * 16) / SuperContestScreen.CaseColumns;
        float cellH = 114;
        int rows = (int)((box.Height - 70 - 132) / (cellH + 16));
        int firstRow = Math.Max(0, screen.Cursor / SuperContestScreen.CaseColumns - rows + 1);
        for (int i = firstRow * SuperContestScreen.CaseColumns; i < held.Count && i < (firstRow + rows) * SuperContestScreen.CaseColumns; i++)
        {
            int col = i % SuperContestScreen.CaseColumns, row = i / SuperContestScreen.CaseColumns - firstRow;
            var r = new Rectangle(box.X + 44 + col * (cell + 16), box.Y + 70 + row * (cellH + 16), cell, cellH);
            bool selected = i == screen.Cursor;
            Card(r, 22, selected);
            var at = new Vector2(r.X + r.Width / 2f, r.Y + r.Height / 2f - 6);
            if (held[i].Count > 0) AccessoryIcon(at, 34, held[i].Accessory);
            else UiShapes.Circle(at, 30, Rule);
            string count = $"×{held[i].Count}";
            UiFonts.DrawCentered(count, r.X + r.Width - 16 - UiFonts.Measure(count, 22, UiWeight.Black), r.Y + r.Height - 22, 22, held[i].Count > 0 ? Ink : Muted, UiWeight.Black);
        }
        // What the cursor is on, and what it is worth is the judges' to say
        var foot = new Rectangle(box.X + 28, box.Y + box.Height - 120, box.Width - 56, 92);
        UiShapes.Fill(foot, 26, Disc);
        string name = held.Count > 0 ? SuperContestDatabase.AccessoryName(held[screen.Cursor].Accessory) : screen.Line;
        UiFonts.DrawCentered(name, foot.X + 36, foot.Y + foot.Height / 2f, 36, Ink, UiWeight.Black);
    }

    private static void ContestLooks(int sw, int sh, SuperContestScreen screen)
    {
        var contest = screen.Contest!;
        ContestFrame(sw, sh, screen, ("Z", "Next"));
        float top = ContentTop, bottom = ContentBottom - ContestBox - Gutter;
        int showing = Math.Clamp(screen.Showing, 0, ContestRules.Contestants - 1);
        var who = contest.Contestants[showing];

        var stage = new Rectangle(Margin, top, 760, bottom - top);
        Panel(stage, 34);
        Tag(stage.X + 36, stage.Y + 30, EntryTag(showing), who.IsPlayer ? Selection : Frame, 42);
        var c = new Vector2(stage.X + stage.Width / 2f, stage.Y + stage.Height / 2f + 10);
        BallDisc(c, 250);
        Dressed(c, 3, who, who.Accessories);

        var card = new Rectangle(Margin + 760 + Gutter, top, sw - Margin * 2 - 760 - Gutter, bottom - top);
        Panel(card, 34);
        NameWithGender(who.Pokemon, card.X + 52, card.Y + 70, 56);
        UiFonts.DrawCentered(who.Trainer, card.X + 52, card.Y + 124, 30, Muted, UiWeight.ExtraBold);
        string theme = SuperContestDatabase.Themes[contest.Theme].Name;
        Tag(card.X + card.Width - 52 - UiFonts.Measure(theme, 24, UiWeight.Black) - 38, card.Y + 50, theme, Frame, 42);
        UiShapes.Fill(new Rectangle(card.X + 52, card.Y + 170, card.Width - 104, 3), 1.5f, Rule);

        // The hearts come up one after another: the condition's first, then the dressing-up's
        float shown = screen.PhaseTime * 6f;
        Label("CONDITION", card.X + 52, card.Y + 206);
        HeartRow(card.X + 52, card.Y + 270, who.ConditionHearts, 8, 22, shown);
        Label("DRESSING UP", card.X + 52, card.Y + 336);
        HeartRow(card.X + 52, card.Y + 400, who.DressHearts, 4, 22, shown - who.ConditionHearts);
        if (who.Accessories.Count == 0) UiFonts.DrawCentered("Nothing on.", card.X + 52 + 4 * 55 + 24, card.Y + 400, 26, Muted, UiWeight.ExtraBold);
        Label("THE VISUAL COMPETITION", card.X + 52, card.Y + 470);
        string total = who.VisualHearts.ToString();
        UiFonts.Draw(total, card.X + 52, card.Y + 492, 84, Ink, UiWeight.Black);
        Heart(new Vector2(card.X + 52 + UiFonts.Measure(total, 84, UiWeight.Black) + 44, card.Y + 540), 26, HeartRed);
        ContestLine(sw, screen.Line, true);
    }

    // ------------------------------------------------------------------ the dance

    private static void StepArrow(Vector2 c, float size, DanceStep step, Color color)
    {
        switch (step)
        {
            case DanceStep.Jump: UiIcons.ArrowUp(c, size, color); break;
            case DanceStep.Forward: UiIcons.ArrowDown(c, size, color); break;
            case DanceStep.Left: UiIcons.ArrowH(c, size, -1, color); break;
            default: UiIcons.ArrowH(c, size, 1, color); break;
        }
    }

    private static Color GradeColor(DanceMark mark) => mark.Missed ? Muted : mark.Excellent ? Gold : Green;

    private static void ContestDance(int sw, int sh, SuperContestScreen screen)
    {
        var contest = screen.Contest!;
        var dance = screen.Dance;
        if (screen.Phase == SuperContestPhase.Dance) ContestFrame(sw, sh, screen, ("Arrows", "Step"));
        else ContestFrame(sw, sh, screen, ("Z", "Next"));
        float top = ContentTop;

        // ---- The bar: the lead's half and the others', the beats ruled, the frame's hand moving across
        var bar = new Rectangle(Margin, top, sw - Margin * 2, 220);
        Panel(bar, 34);
        var track = new Rectangle(bar.X + 44, bar.Y + 86, bar.Width - 88, 96);
        UiShapes.Fill(track, 22, StageDark);
        UiShapes.Fill(new Rectangle(track.X + track.Width / 2f, track.Y, track.Width / 2f, track.Height), 22, new Color(44, 50, 84, 255));
        Label("THE LEAD'S STEPS", track.X, bar.Y + 34);
        Label("REPEAT THEM HERE", track.X + track.Width / 2f + 12, bar.Y + 34);
        if (dance != null && !dance.IsOver)
        {
            var song = dance.Song;
            string turn = $"LEAD {dance.Rotation + 1} / {SuperContestDance.Rotations}  ·  BAR {dance.Bar + 1} / {song.Bars}";
            Tag(bar.X + bar.Width - 44 - UiFonts.Measure(turn, 20, UiWeight.Black) - 32, bar.Y + 28, turn, Frame, 36);
            float perSlot = track.Width / song.Slots;
            for (int beat = 0; beat <= song.BeatsPerBar; beat++)
            {
                float x = track.X + beat * 2 * perSlot;
                UiShapes.Fill(new Rectangle(x - 1.5f, track.Y + (beat % 4 == 0 ? 8 : 26), 3, track.Height - (beat % 4 == 0 ? 16 : 52)), 1.5f, new Color(255, 255, 255, beat % 4 == 0 ? 90 : 40));
            }
            // The lead's steps where they fell, and where each is to be repeated
            foreach (var (slot, step) in dance.Led)
            {
                StepArrow(new Vector2(track.X + slot * perSlot, track.Y + track.Height / 2f), 26, step, Color.White);
                var echo = new Vector2(track.X + (slot + song.Slots / 2) * perSlot, track.Y + track.Height / 2f);
                UiShapes.Ring(echo, 30, 4, new Color(255, 255, 255, 110));
                StepArrow(echo, 20, step, new Color(255, 255, 255, 120));
            }
            // The player's own steps of the bar, in the colour of how they were judged
            foreach (var mark in dance.BarMarks.Where(m => m.Contestant == 0))
                UiShapes.Circle(new Vector2(track.X + track.Width * mark.Frame / song.BarFrames, track.Y + track.Height - 12), 9, GradeColor(mark));
            float hand = track.X + track.Width * (dance.Frame + screen.Between) / song.BarFrames;
            if (screen.Phase == SuperContestPhase.Dance)
                UiShapes.Fill(new Rectangle(hand - 3, track.Y - 10, 6, track.Height + 20), 3, Selection);
        }

        // ---- The dancers: the lead in front, the rest behind, each with its points
        float stageTop = top + 220 + Gutter, stageHeight = ContentBottom - ContestBox - Gutter - stageTop;
        var floor = new Rectangle(Margin, stageTop, sw - Margin * 2, stageHeight);
        Panel(floor, 34);
        for (int c = 0; c < ContestRules.Contestants; c++)
        {
            var who = contest.Contestants[c];
            int column = ContestRules.Contestants - 1 - c;
            float cx = floor.X + floor.Width * (column + 0.5f) / 4f;
            bool leads = dance != null && !dance.IsOver && dance.Lead == c;
            var at = new Vector2(cx, floor.Y + 216);
            UiShapes.Circle(at, leads ? 140 : 104, leads ? Lighter(QualityColor(contest.Category), 0.55f) : Disc);
            // A step danced lifts the dancer for a moment
            var last = screen.Marks.LastOrDefault(m => m.Mark.Contestant == c);
            float hop = last.Age > 0f && last.Age < 0.3f ? MathF.Sin(last.Age / 0.3f * MathF.PI) * 30f : 0f;
            PixelArt(PixelArtGenerator.SpriteOf(who.Pokemon), at, leads ? 2 : 1, hop);
            if (last.Age > 0f && last.Age < 0.9f)
            {
                var sign = at + new Vector2(leads ? 150 : 112, -(leads ? 80 : 60) - last.Age * 30f);
                UiShapes.Circle(sign, 30, GradeColor(last.Mark));
                StepArrow(sign, 20, last.Mark.Step, Color.White);
            }
            Tag(floor.X + column * floor.Width / 4f + 28, floor.Y + 24, EntryTag(c), who.IsPlayer ? Selection : Frame, 36);
            if (leads) Tag(floor.X + column * floor.Width / 4f + 28 + 110, floor.Y + 24, "LEAD", QualityColor(contest.Category), 36);
            string name = who.Pokemon.DisplayName;
            UiFonts.DrawCentered(name, cx - UiFonts.Measure(name, 28, UiWeight.Black) / 2f, floor.Y + floor.Height - 76, 28, Ink, UiWeight.Black);
            string points = $"{(dance != null ? dance.Score[c] : 0)} pts";
            UiFonts.DrawCentered(points, cx - UiFonts.Measure(points, 26, UiWeight.ExtraBold) / 2f, floor.Y + floor.Height - 40, 26, Muted, UiWeight.ExtraBold);
        }
        ContestLine(sw, screen.Line, screen.Phase != SuperContestPhase.Dance && !(screen.Phase == SuperContestPhase.DanceLead && dance != null));
    }

    // ------------------------------------------------------------------ the acting

    private static void ContestActing(int sw, int sh, SuperContestScreen screen)
    {
        var contest = screen.Contest!;
        var acting = screen.Acting!;
        switch (screen.Phase)
        {
            case SuperContestPhase.ActingMove: ContestFrame(sw, sh, screen, ("Up / Down", "Move"), ("Z", "Choose")); break;
            case SuperContestPhase.ActingJudge: ContestFrame(sw, sh, screen, ("Left / Right", "Judge"), ("Z", "Choose"), ("X", "Back")); break;
            default: ContestFrame(sw, sh, screen, ("Z", "Next")); break;
        }
        float top = ContentTop;
        const float left = 1040;

        // ---- The judges: a name over five lights of voltage, and who performed for them this turn
        float judgeW = (left - Gutter * 2) / 3f;
        for (int j = 0; j < ContestRules.Judges; j++)
        {
            var r = new Rectangle(Margin + j * (judgeW + Gutter), top, judgeW, 196);
            bool chosen = screen.Phase == SuperContestPhase.ActingJudge && screen.JudgeCursor == j;
            bool speaking = screen.Phase == SuperContestPhase.ActingShow && screen.Event is { Judge: var ej } && ej == j;
            Card(r, 28, chosen || speaking, chosen ? Selection : QualityColor(contest.Category));
            string name = contest.Judges[j].Name;
            UiFonts.DrawCentered(name, r.X + 28, r.Y + 44, 34, Ink, UiWeight.Black);
            if (j == ContestRules.MainJudge) Tag(r.X + r.Width - 28 - 96, r.Y + 26, "HEAD", Gold, 36);
            Label("VOLTAGE", r.X + 28, r.Y + 78);
            int lit = screen.ShownVoltage[j] / 10;
            for (int v = 0; v < 5; v++)
                Star(new Vector2(r.X + 46 + v * 46, r.Y + 122), 18, v < lit ? Gold : Rule);
            // Whoever has performed for this judge so far
            int n = 0;
            for (int c = 0; c < ContestRules.Contestants; c++)
                if (screen.Chosen[c] is { } choice && choice.Judge == j)
                {
                    var icon = PixelArtGenerator.IconOf(contest.Contestants[c].Pokemon);
                    PixelArt(icon, new Vector2(r.X + 40 + n * 56, r.Y + 166), 1);
                    n++;
                }
        }

        // ---- The four in the turn's order: what each showed, the hearts of the turn, the hearts so far
        float rowsTop = top + 196 + Gutter, rowsBottom = ContentBottom - ContestBox - Gutter;
        float pitch = (rowsBottom - rowsTop + 12) / 4f;
        for (int place = 0; place < screen.ShownOrder.Length; place++)
        {
            int c = screen.ShownOrder[place];
            var who = contest.Contestants[c];
            var r = new Rectangle(Margin, rowsTop + place * pitch, left, pitch - 12);
            bool acts = screen.Phase == SuperContestPhase.ActingShow && screen.Event is { Contestant: var ec } && ec == c;
            Card(r, 24, acts, QualityColor(contest.Category));
            var disc = new Vector2(r.X + 56, r.Y + r.Height / 2f);
            UiShapes.Circle(disc, 40, who.IsPlayer ? Lighter(Selection, 0.6f) : Disc);
            PixelArt(PixelArtGenerator.IconOf(who.Pokemon), disc + new Vector2(0, -4), 1, acts ? MathF.Abs(MathF.Sin((float)FrameClock.Now * 9f)) * 8f : 0f);
            UiFonts.DrawCentered(who.Pokemon.DisplayName, r.X + 112, r.Y + r.Height / 2f - 16, 30, Ink, UiWeight.Black);
            UiFonts.DrawCentered(who.Trainer, r.X + 112, r.Y + r.Height / 2f + 18, 22, Muted, UiWeight.ExtraBold);
            if (screen.Chosen[c] is { } choice)
            {
                float mx = r.X + 370;
                mx += Tag(mx, r.Y + r.Height / 2f - 40, choice.Move.Contest!.Type.ToString().ToUpperInvariant(), QualityColor(choice.Move.Contest.Type), 34) + 12;
                UiFonts.DrawCentered(choice.Move.Name, r.X + 370, r.Y + r.Height / 2f + 18, 28, Ink, UiWeight.Black);
                UiFonts.DrawCentered($"for {contest.Judges[choice.Judge].Name}", mx, r.Y + r.Height / 2f - 23, 22, Muted, UiWeight.ExtraBold);
            }
            else if (acting.IsDoubled(c) && screen.Phase is SuperContestPhase.ActingMove or SuperContestPhase.ActingJudge)
                Tag(r.X + 370, r.Y + r.Height / 2f - 17, "DOUBLE THIS TURN", Gold, 34);
            // The turn's hearts, and the total at the right
            int turn = screen.TurnPoints[c] / ContestRules.PointsPerHeart;
            if (turn > 0 || screen.Chosen[c] != null)
            {
                Heart(new Vector2(r.X + 700, r.Y + r.Height / 2f), 15, HeartRed);
                UiFonts.DrawCentered($"+{turn}", r.X + 726, r.Y + r.Height / 2f, 32, Ink, UiWeight.Black);
            }
            string total = (screen.ShownTotal[c] / ContestRules.PointsPerHeart).ToString();
            UiFonts.DrawCentered(total, r.X + r.Width - 36 - UiFonts.Measure(total, 44, UiWeight.Black), r.Y + r.Height / 2f, 44, Ink, UiWeight.Black);
            Label("TOTAL", r.X + r.Width - 36 - 150, r.Y + r.Height / 2f - 10);
        }

        // ---- The player's moves, to choose from or to keep in view
        var moves = new Rectangle(Margin + left + Gutter, top, sw - Margin * 2 - left - Gutter, rowsBottom - top);
        Panel(moves, 34);
        string turnTag = screen.Phase == SuperContestPhase.ActingScores ? "OVER" : $"TURN {screen.ShownTurn + 1} / {SuperContestActing.Turns}";
        Label($"{contest.Player.Pokemon.DisplayName.ToUpperInvariant()}'S MOVES", moves.X + 40, moves.Y + 30);
        Tag(moves.X + moves.Width - 40 - UiFonts.Measure(turnTag, 20, UiWeight.Black) - 32, moves.Y + 22, turnTag, Frame, 36);
        var list = contest.Player.Pokemon.Moves;
        float moveH = (moves.Height - 72 - 24) / 4f;
        for (int m = 0; m < list.Count && m < 4; m++)
        {
            var move = list[m].Data;
            var r = new Rectangle(moves.X + 28, moves.Y + 72 + m * moveH, moves.Width - 56, moveH - 14);
            bool usable = screen.Phase != SuperContestPhase.ActingScores && screen.Usable[m];
            bool selected = screen.Phase is SuperContestPhase.ActingMove or SuperContestPhase.ActingJudge && screen.MoveCursor == m;
            Card(r, 22, selected);
            var ink = usable ? Ink : Muted;
            UiFonts.DrawCentered(move.Name, r.X + 28, r.Y + 38, 32, ink, UiWeight.Black);
            if (move.Contest is { } stage)
            {
                Tag(r.X + r.Width - 28 - UiFonts.Measure(stage.Type.ToString().ToUpperInvariant(), 20, UiWeight.Black) - 32, r.Y + 20, stage.Type.ToString().ToUpperInvariant(), usable ? QualityColor(stage.Type) : Muted, 36);
                HeartRow(r.X + 28, r.Y + 84, stage.Hearts, Math.Max(stage.Hearts, 3), 12);
                int rise = ContestRules.Voltage(contest.Category, stage.Type);
                string voltage = rise > 0 ? "VOLTAGE UP" : rise < 0 ? "VOLTAGE DOWN" : "";
                if (voltage.Length > 0) UiFonts.DrawCentered(voltage, r.X + 150, r.Y + 84, 20, rise > 0 ? Green : Red, UiWeight.Black);
                string what = usable ? ContestRules.Describe(stage.Effect) : "Shown last turn: not twice running.";
                DrawWrapped(what, r.X + 28, r.Y + 106, r.Width - 56, 22, Muted, 28, UiWeight.ExtraBold);
            }
        }
        ContestLine(sw, screen.Line, screen.Phase is SuperContestPhase.ActingStart or SuperContestPhase.ActingShow or SuperContestPhase.ActingScores);
    }

    // ------------------------------------------------------------------ the results

    private static readonly Color[] RoundColors = { new(238, 118, 172, 255), new(74, 132, 228, 255), new(240, 176, 56, 255) };
    private static readonly string[] RoundNames = { "VISUAL", "DANCE", "ACTING" };

    private static void ContestResults(int sw, int sh, SuperContestScreen screen)
    {
        var contest = screen.Contest!;
        ContestFrame(sw, sh, screen, ("Z", "Next"));
        float top = ContentTop, bottom = ContentBottom - ContestBox - Gutter;
        var board = new Rectangle(Margin, top, sw - Margin * 2, bottom - top);
        Panel(board, 34);
        bool practice = contest.IsPractice;
        bool decided = practice || screen.Phase is SuperContestPhase.Award or SuperContestPhase.Done || screen.ResultStage >= 3;
        float x = board.X + 44;
        Label(practice ? "PRACTICE" : "THE RESULTS", x, board.Y + 30);
        if (!practice)
        {
            float kx = board.X + board.Width - 44;
            for (int round = 2; round >= 0; round--)
            {
                kx -= UiFonts.Measure(RoundNames[round], 20, UiWeight.Black) + 32;
                Tag(kx, board.Y + 22, RoundNames[round], RoundColors[round], 36);
                kx -= 12;
            }
        }
        float pitch = (board.Height - 80) / 4f;
        const float barLeft = 560;
        float barWidth = board.Width - barLeft - 150;
        int round1 = practice ? (contest.Mode == ContestMode.PracticeVisual ? 0 : contest.Mode == ContestMode.PracticeDance ? 1 : 2) : 0;
        int[] raw = contest.RoundScores(round1);
        int best = Math.Max(1, raw.Max());
        for (int c = ContestRules.Contestants - 1; c >= 0; c--)
        {
            var who = contest.Contestants[c];
            int row = ContestRules.Contestants - 1 - c;
            var r = new Rectangle(board.X + 28, board.Y + 72 + row * pitch, board.Width - 56, pitch - 14);
            bool wins = decided && !practice && who.Placement == 0;
            Card(r, 24, wins, Gold);
            Tag(r.X + 20, r.Y + r.Height / 2f - 19, EntryTag(c), who.IsPlayer ? Selection : Frame, 38);
            var disc = new Vector2(r.X + 176, r.Y + r.Height / 2f);
            UiShapes.Circle(disc, MathF.Min(52, r.Height / 2f - 8), Disc);
            PixelArt(PixelArtGenerator.IconOf(who.Pokemon), disc + new Vector2(0, -4), 1, wins ? MathF.Abs(MathF.Sin((float)FrameClock.Now * 8f)) * 10f : 0f);
            UiFonts.DrawCentered(who.Pokemon.DisplayName, r.X + 244, r.Y + r.Height / 2f - 16, 32, Ink, UiWeight.Black);
            UiFonts.DrawCentered(who.Trainer, r.X + 244, r.Y + r.Height / 2f + 20, 22, Muted, UiWeight.ExtraBold);
            var track = new Rectangle(board.X + barLeft, r.Y + r.Height / 2f - 20, barWidth, 40);
            UiShapes.Fill(track, 20, Track);
            if (practice)
            {
                float grow = UiMotion.EaseOut(Math.Clamp(screen.PhaseTime / SuperContestScreen.BarTime, 0f, 1f));
                float w = (track.Width - 8) * raw[c] / best * grow;
                if (w > 1f) UiShapes.Fill(new Rectangle(track.X + 4, track.Y + 4, MathF.Max(w, 32), 32), 16, RoundColors[round1]);
                string score = round1 == 2 ? (raw[c] / ContestRules.PointsPerHeart).ToString() : raw[c].ToString();
                UiFonts.DrawCentered(score, track.X + track.Width + 24, r.Y + r.Height / 2f, 36, Ink, UiWeight.Black);
                continue;
            }
            // Each round's bar after the one before, a third of the track each at its best (the original's board of 192 dots)
            float at = track.X + 4;
            for (int round = 0; round < 3; round++)
            {
                float grow = round < screen.ResultStage || decided ? 1f : round == screen.ResultStage ? UiMotion.EaseOut(Math.Clamp(screen.PhaseTime / SuperContestScreen.BarTime, 0f, 1f)) : 0f;
                float w = (track.Width - 8) * who.Bars[round] / 192f * grow;
                if (w > 1f) UiShapes.Fill(new Rectangle(at, track.Y + 4, w, 32), round == 0 ? 16 : 6, RoundColors[round]);
                at += w;
            }
            if (decided)
            {
                string place = who.Placement switch { 0 => "1st", 1 => "2nd", 2 => "3rd", _ => "4th" };
                UiFonts.DrawCentered(place, track.X + track.Width + 24, r.Y + r.Height / 2f, 36, wins ? Darker(Gold, 0.25f) : Ink, UiWeight.Black);
            }
        }
        ContestLine(sw, screen.Line, true);
        // The winner's ribbon beside the line that gives it
        if (screen.Phase == SuperContestPhase.Award && screen.Outcome is { NewRibbon: not null })
            Rosette(new Vector2(sw - Margin - 210, ContentBottom - ContestBox / 2f - 14), 40, QualityColor(contest.Category));

    }

    /// <summary>A ribbon as a rosette in a quality's colour: a ring of loops round a gold middle, two tails under it.</summary>
    public static void Rosette(Vector2 c, float r, Color color)
    {
        UiShapes.Triangle(c + new Vector2(-r * 0.5f, r * 0.2f), c + new Vector2(-r * 0.75f, r * 1.5f), c + new Vector2(-r * 0.05f, r * 1.1f), Darker(color, 0.2f));
        UiShapes.Triangle(c + new Vector2(r * 0.5f, r * 0.2f), c + new Vector2(r * 0.75f, r * 1.5f), c + new Vector2(r * 0.05f, r * 1.1f), Darker(color, 0.3f));
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.PI * 2f / 12f;
            UiShapes.Circle(c + new Vector2(MathF.Cos(a), MathF.Sin(a)) * r * 0.72f, r * 0.26f, i % 2 == 0 ? color : Lighter(color, 0.25f));
        }
        UiShapes.Circle(c, r * 0.7f, Darker(color, 0.12f));
        UiShapes.Circle(c, r * 0.5f, Gold);
        Star(c, r * 0.3f, Color.White);
    }
}
