using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The Pokéathlon's third batch on screen (plan 15 · E8; style guide, "The Pokéathlon", "Events, batch 3", "Medals",
/// "The records board", "The Athlete Shop"): Relay Run, Circle Push and Goal Roll, a course's medal, the records board
/// and the shop's prices in Athlete Points.
/// </summary>
internal static partial class ModernUi
{
    public static readonly Color MedalGold = new(232, 184, 48, 255);
    public static readonly Color MedalSilver = new(184, 192, 204, 255);
    public static readonly Color MedalBronze = new(196, 128, 72, 255);

    public static Color MedalColor(AthlonMedal medal) => medal switch
    {
        AthlonMedal.Gold => MedalGold,
        AthlonMedal.Silver => MedalSilver,
        _ => MedalBronze
    };

    public static string AthlonMedalName(AthlonMedal medal) => medal switch
    {
        AthlonMedal.Gold => Text.Of("athlon.medal.gold"),
        AthlonMedal.Silver => Text.Of("athlon.medal.silver"),
        AthlonMedal.Bronze => Text.Of("athlon.medal.bronze"),
        _ => Text.Of("athlon.medal.none")
    };

    /// <summary>A medal: a disc of its metal with a darker rim and a white star; an empty ring where none is won.</summary>
    private static void MedalDisc(Vector2 c, float r, AthlonMedal medal)
    {
        if (medal == AthlonMedal.None)
        {
            UiShapes.Ring(c, r - 3, 6, Rule);
            return;
        }
        var metal = MedalColor(medal);
        UiShapes.Circle(c, r, Darker(metal, 0.3f));
        UiShapes.Circle(c, r - 6, metal);
        UiIcons.ShinyStar(c, r * 1.1f);
    }

    /// <summary>The scoreboard's foot: the course's medal and its name, at the left of NEW RECORD! (nothing under bronze).</summary>
    private static void AthlonMedalOnFoot(Rectangle foot, AthlonMedal medal, bool newRecord)
    {
        if (medal == AthlonMedal.None) return;
        string name = AthlonMedalName(medal);
        float nameWidth = UiFonts.Measure(name, 28, UiWeight.Black);
        float right = foot.X + foot.Width - 32 - (newRecord ? UiFonts.Measure(Text.Of("athlon.newRecord"), 28, UiWeight.Black) + 48 + 24 : 0f);
        float x = right - nameWidth;
        UiFonts.DrawCentered(name, x, foot.Y + foot.Height / 2f, 28, Ink, UiWeight.Black);
        MedalDisc(new Vector2(x - 40, foot.Y + foot.Height / 2f), 30, medal);
    }

    // ------------------------------------------------------------------ the records board

    private static void AthlonRecordsBoard(int sw, AthlonScreen screen, float rise)
    {
        ScreenTitle(Text.Of("athlon.records"));
        Hints(sw - Margin, 44, Hint("shared.keys.z", "shared.hints.close"));
        var courses = AthlonCourses.All;
        var board = new Rectangle(Margin, ContentTop + rise, sw - Margin * 2, courses.Count * 108 + 48);
        Panel(board, 30);
        for (int i = 0; i < courses.Count; i++)
        {
            var c = courses[i];
            var row = new Rectangle(board.X + 24, board.Y + 24 + i * 108, board.Width - 48, 96);
            UiShapes.Fill(row, 24, i % 2 == 0 ? PanelBottom : Lighter(AthlonSky, 0.88f));
            UiFonts.DrawCentered(AthlonCourseName(c), row.X + 36, row.Y + row.Height / 2f, 40, Ink, UiWeight.Black);
            string events = string.Join(" · ", c.Events.Select(AthlonEventName));
            UiFonts.DrawCentered(events, row.X + 560, row.Y + row.Height / 2f, 24, Muted, UiWeight.ExtraBold);
            int best = screen.Records.BestOf(c.Key);
            string score = best > 0 ? best.ToString() : Text.Of("athlon.noBest");
            float size = best > 0 ? 48 : 28;
            float w = UiFonts.Measure(score, size, UiWeight.Black);
            UiFonts.DrawCentered(score, row.X + row.Width - 36 - w, row.Y + row.Height / 2f, size, best > 0 ? Ink : Muted, UiWeight.Black);
            MedalDisc(new Vector2(row.X + row.Width - 36 - 260, row.Y + row.Height / 2f), 32, screen.Records.MedalOf(c.Key));
        }

        var foot = new Rectangle(Margin, 940 + rise, sw - Margin * 2, 92);
        Panel(foot, 30);
        UiFonts.DrawCentered(Text.Of("athlon.recordsFoot", ("runs", screen.Records.Runs), ("wins", screen.Records.Wins),
            ("points", screen.Records.Points.ToString("N0"))), foot.X + 44, foot.Y + foot.Height / 2f, 36, Ink, UiWeight.ExtraBold);
    }

    // ------------------------------------------------------------------ the shop in points

    // Whether the shop being drawn is paid in Athlete Points (set by DrawShop as it begins)
    private static bool shopInPoints;

    /// <summary>A price or a purse: money with its sign, or Athlete Points with PTS after them. Returns its width.</summary>
    private static float ShopAmount(float x, float centerY, int amount, float size, Color color)
    {
        if (!shopInPoints) return Money(x, centerY, amount, size, color);
        string text = Text.Of("athlon.shop.amount", ("n", amount.ToString("N0")));
        UiFonts.DrawCentered(text, x, centerY, size, color, UiWeight.Black);
        return UiFonts.Measure(text, size, UiWeight.Black);
    }

    private static void ShopAmountRight(float rightX, float centerY, int amount, float size, Color color)
    {
        if (!shopInPoints)
        {
            MoneyRight(rightX, centerY, amount, size, color);
            return;
        }
        string text = Text.Of("athlon.shop.amount", ("n", amount.ToString("N0")));
        UiFonts.DrawCentered(text, rightX - UiFonts.Measure(text, size, UiWeight.Black), centerY, size, color, UiWeight.Black);
    }

    // ------------------------------------------------------------------ Relay Run

    private static void DrawRelayRun(int sw, RelayRun ev)
    {
        // The oval: two half circles joined by straights, 1500 by 600 at the stage's middle, four lanes
        var middle = new Vector2(sw / 2f, (AthlonStage + AthlonStageBottom) / 2f + 10);
        const float width = 1500f, height = 600f, lane = 44f;
        float rOuter = height / 2f, straight = width - height;
        void Oval(float r, Color c)
        {
            UiShapes.Fill(new Rectangle(middle.X - straight / 2f - r, middle.Y - r, straight + 2 * r, 2 * r), r, c);
        }
        Oval(rOuter + 14, new Color(30, 40, 70, 255));
        Oval(rOuter, new Color(206, 112, 82, 255));
        Oval(rOuter - 4 * lane, Lighter(Green, 0.25f));
        for (int i = 1; i < 4; i++)
        {
            float r = rOuter - i * lane;
            var box = new Rectangle(middle.X - straight / 2f - r, middle.Y - r, straight + 2 * r, 2 * r);
            UiShapes.Shape(box, r, Color.Blank, Color.Blank, new Color(255, 255, 255, 170), 3);
        }
        // The line where the laps are counted, across the bottom straight
        UiShapes.Fill(new Rectangle(middle.X - 3, middle.Y + rOuter - 4 * lane, 6, 4 * lane), 3, Color.White);

        for (int t = 0; t < AthlonRun.TeamCount; t++)
        {
            var r = ev.Relays[t];
            float radius = rOuter - lane * (t + 0.5f);
            var at = OvalPoint(middle, straight, radius, r.Round);
            float body = 30f;
            if (r.Spurt > 0) UiShapes.Glow(at, body * 1.6f, body * 1.2f, AthlonTeamColors[t] with { A = 120 });
            UiShapes.Circle(at, body, AthlonTeamColors[t]);
            UiShapes.Circle(at, body - 7, Color.White);
            AthleteIcon(ev.RunnerOf(t).Species, at, r.Switching > 0 ? 0.4f : 1f, 1.5f);
        }

        // The infield: each team's laps beside its chip, and its three's breath
        float rowY = middle.Y - rOuter + 4 * lane + 30;
        float colW = (straight + 2 * (rOuter - 4 * lane) - 160) / 2f;
        for (int t = 0; t < AthlonRun.TeamCount; t++)
        {
            var r = ev.Relays[t];
            float x = middle.X - straight / 2f - (rOuter - 4 * lane) + 80 + (t % 2) * colW + 40;
            float y = rowY + (t / 2) * 120;
            TeamChip(new Rectangle(x, y, 230, 40), ev.Teams[t], t);
            UiFonts.DrawCentered(Text.Of("athlon.laps", ("n", r.Laps)), x + 250, y + 20, 34, Ink, UiWeight.Black);
            for (int a = 0; a < AthlonRun.TeamSize; a++)
            {
                var track = new Rectangle(x + a * 110, y + 56, 96, 18);
                UiShapes.Fill(track, 9, Rule);
                float left = 1f - Math.Min(1f, r.Fatigue[a] / RelayRun.Tired);
                var fill = r.Fatigue[a] >= RelayRun.Tired ? Red : a == r.Active ? AthlonTeamColors[t] : Muted;
                if (left > 0f) UiShapes.Fill(new Rectangle(track.X, track.Y, track.Width * left, track.Height), 9, fill);
            }
        }
    }

    /// <summary>A point a share of the way round an oval, clockwise from the middle of its bottom straight.</summary>
    private static Vector2 OvalPoint(Vector2 middle, float straight, float r, float share)
    {
        float half = straight / 2f, perimeter = 2f * straight + 2f * MathF.PI * r;
        float d = share * perimeter;
        // Bottom straight, from the middle leftward
        if (d < half) return new Vector2(middle.X - d, middle.Y + r);
        d -= half;
        float arc = MathF.PI * r;
        if (d < arc)
        {
            float a = MathF.PI / 2f + d / r;
            return new Vector2(middle.X - half + MathF.Cos(a) * r, middle.Y + MathF.Sin(a) * r);
        }
        d -= arc;
        if (d < straight) return new Vector2(middle.X - half + d, middle.Y - r);
        d -= straight;
        if (d < arc)
        {
            float a = -MathF.PI / 2f + d / r;
            return new Vector2(middle.X + half + MathF.Cos(a) * r, middle.Y + MathF.Sin(a) * r);
        }
        d -= arc;
        return new Vector2(middle.X + half - d, middle.Y + r);
    }

    // ------------------------------------------------------------------ Circle Push

    private static void DrawCirclePush(int sw, CirclePush ev)
    {
        const float scale = 2.7f;
        var middle = new Vector2(sw / 2f, (AthlonStage + AthlonStageBottom) / 2f);
        float half = CirclePush.Half * scale;
        UiShapes.Fill(new Rectangle(middle.X - half - 14, middle.Y - half - 14, 2 * half + 28, 2 * half + 28), 24, new Color(30, 40, 70, 255));
        UiShapes.Fill(new Rectangle(middle.X - half, middle.Y - half, 2 * half, 2 * half), 14, new Color(226, 220, 204, 255));

        int inside = 0;
        for (int t = 0; t < AthlonRun.TeamCount; t++) if (ev.InCircle(t)) inside++;
        float circle = CirclePush.CircleRadius * scale;
        UiShapes.Circle(middle, circle, Lighter(Gold, inside == 1 ? 0.45f : 0.75f));
        UiShapes.Ring(middle, circle, 6, Gold);

        for (int t = 0; t < AthlonRun.TeamCount; t++)
        {
            var p = ev.Pushers[t];
            if (p.Switching > 0) continue;
            var c = middle + p.At * scale;
            float body = AthlonBodies.Body * scale;
            if (p.Lunge > 0) UiShapes.Glow(c - p.Facing * body * 0.8f, body * 1.2f, body * 0.8f, AthlonTeamColors[t] with { A = 120 });
            UiShapes.Circle(c, body, AthlonTeamColors[t]);
            UiShapes.Circle(c, body - 7, Color.White);
            AthleteIcon(ev.ActiveOf(t).Species, c, 1f, 1.5f);
        }

        for (int t = 0; t < AthlonRun.TeamCount; t++)
        {
            bool rightSide = t is 0 or 3, low = t is 0 or 1;
            var box = new Rectangle(rightSide ? sw - Margin - 380 : Margin, low ? AthlonStageBottom - 160 : AthlonStage, 380, 160);
            Panel(box, 26, frame: t == 0 ? AthlonTeamColors[0] : null);
            TeamChip(new Rectangle(box.X + 18, box.Y + 16, box.Width - 36, 40), ev.Teams[t], t);
            UiFonts.Draw(Text.Of("athlon.held", ("n", ev.Points(t))), box.X + 28, box.Y + 76, 30, Ink, UiWeight.Black);
            if (ev.Pushers[t].Switching > 0)
            {
                var next = ev.ActiveOf(t);
                AthleteIcon(next.Species, new Vector2(box.X + box.Width - 70, box.Y + 110), 0.6f);
            }
        }
    }

    // ------------------------------------------------------------------ Goal Roll

    private static void DrawGoalRoll(int sw, GoalRoll ev)
    {
        const float scale = 2.53f;
        var middle = new Vector2(sw / 2f, (AthlonStage + AthlonStageBottom) / 2f);
        float half = GoalRoll.Half * scale, mouth = GoalRoll.Mouth * scale;
        UiShapes.Fill(new Rectangle(middle.X - half - 14, middle.Y - half - 14, 2 * half + 28, 2 * half + 28), 24, new Color(30, 40, 70, 255));
        // The pitch in bands of two greens
        const int bands = 8;
        float bandW = 2 * half / bands;
        for (int b = 0; b < bands; b++)
            UiShapes.Fill(new Rectangle(middle.X - half + b * bandW, middle.Y - half, bandW + 1, 2 * half), 0,
                b % 2 == 0 ? new Color(98, 168, 92, 255) : new Color(88, 156, 84, 255));
        UiShapes.Ring(middle, 60 * scale / 2.53f, 4, new Color(255, 255, 255, 190));
        UiShapes.Fill(new Rectangle(middle.X - half, middle.Y - 2, 2 * half, 4), 2, new Color(255, 255, 255, 140));
        UiShapes.Fill(new Rectangle(middle.X - 2, middle.Y - half, 4, 2 * half), 2, new Color(255, 255, 255, 140));

        // The four goals, each a mouth of its team's colour on its own side, flashing white for a moment after a goal
        for (int t = 0; t < AthlonRun.TeamCount; t++)
        {
            var g = middle + GoalRoll.GoalOf(t) * scale;
            bool flash = ev.LastGoalInto == t && ev.Frames - ev.LastGoalAt < 20;
            var c = flash ? Color.White : AthlonTeamColors[t];
            var r = t is 0 or 2
                ? new Rectangle(g.X - mouth, g.Y - 10, 2 * mouth, 20)
                : new Rectangle(g.X - 10, g.Y - mouth, 20, 2 * mouth);
            UiShapes.Fill(r, 10, c);
        }

        for (int t = 0; t < AthlonRun.TeamCount; t++)
        {
            var p = ev.Players[t];
            if (p.Switching > 0) continue;
            var c = middle + p.At * scale;
            float body = AthlonBodies.Body * scale;
            if (p.Lunge > 0) UiShapes.Glow(c - p.Facing * body * 0.8f, body * 1.2f, body * 0.8f, AthlonTeamColors[t] with { A = 120 });
            UiShapes.Circle(c, body, AthlonTeamColors[t]);
            UiShapes.Circle(c, body - 7, Color.White);
            AthleteIcon(ev.ActiveOf(t).Species, c, 1f, 1.5f);
        }

        var ball = middle + ev.Ball * scale;
        UiShapes.Glow(ball + new Vector2(0, 8), 26, 10, new Color(0, 0, 0, 70));
        UiShapes.Circle(ball, 22, new Color(40, 44, 60, 255));
        UiShapes.Circle(ball, 18, Color.White);

        for (int t = 0; t < AthlonRun.TeamCount; t++)
        {
            bool rightSide = t is 0 or 3, low = t is 0 or 1;
            var box = new Rectangle(rightSide ? sw - Margin - 380 : Margin, low ? AthlonStageBottom - 160 : AthlonStage, 380, 160);
            Panel(box, 26, frame: t == 0 ? AthlonTeamColors[0] : null);
            TeamChip(new Rectangle(box.X + 18, box.Y + 16, box.Width - 36, 40), ev.Teams[t], t);
            UiFonts.Draw(Text.Of("athlon.goals", ("n", ev.Players[t].Goals)), box.X + 28, box.Y + 76, 30, Ink, UiWeight.Black);
            if (ev.Players[t].OwnGoals > 0)
                UiFonts.Draw(Text.Of("athlon.ownGoals", ("n", ev.Players[t].OwnGoals)), box.X + 28, box.Y + 116, 24, Muted, UiWeight.Black);
        }
    }
}
