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
/// The Pokéathlon on screen (plan 15 · E6; style guide, "The Pokéathlon"): the course choice, the team with its five
/// stats, an event's briefing, the three events drawn flat in layout units with the kit's shapes and the Pokémon's menu
/// icons at twice their size, an event's points and the scoreboard. Every team has its colour, and nothing else says
/// whose a thing is.
/// </summary>
internal static partial class ModernUi
{
    /// <summary>The four teams' colours: the player's in the selection's colour, the rivals blue, green and gold.</summary>
    // (a property: a static array made of the tokens of ModernUi's other files would be made before them, all four transparent)
    public static Color[] AthlonTeamColors => new[] { Selection, Blue, Green, Gold };

    /// <summary>The dome's own colour: the title band and the stars of a stat.</summary>
    public static readonly Color AthlonSky = new(58, 150, 214, 255);

    private const float AthlonStage = 154f, AthlonStageBottom = 948f;

    public static void DrawAthlon(int sw, int sh, AthlonScreen screen)
    {
        Backdrop(sw, sh);
        float rise = (1f - UiMotion.EaseOut(screen.Appear)) * 60f;
        switch (screen.Phase)
        {
            case AthlonPhase.Courses: AthlonCoursePage(sw, screen, rise); break;
            case AthlonPhase.Team: AthlonTeamPage(sw, screen, rise); break;
            case AthlonPhase.Briefing: AthlonBriefing(sw, screen, rise); break;
            case AthlonPhase.Playing: AthlonPlaying(sw, sh, screen); break;
            case AthlonPhase.EventPoints: AthlonEventPoints(sw, screen, rise); break;
            case AthlonPhase.Scoreboard: AthlonScoreboard(sw, screen, rise); break;
        }
    }

    // ------------------------------------------------------------------ words

    public static string AthlonEventName(AthlonEventKind kind) => kind switch
    {
        AthlonEventKind.HurdleDash => Text.Of("athlon.event.hurdleDash"),
        AthlonEventKind.BlockSmash => Text.Of("athlon.event.blockSmash"),
        AthlonEventKind.RingDrop => Text.Of("athlon.event.ringDrop"),
        _ => AthlonEventNameBatch2(kind)
    };

    private static string AthlonHowTo(AthlonEventKind kind) => kind switch
    {
        AthlonEventKind.HurdleDash => Text.Of("athlon.howTo.hurdleDash"),
        AthlonEventKind.BlockSmash => Text.Of("athlon.howTo.blockSmash"),
        AthlonEventKind.RingDrop => Text.Of("athlon.howTo.ringDrop"),
        _ => AthlonHowToBatch2(kind)
    };

    public static string AthlonCourseName(AthlonCourse course) => course.Key switch
    {
        _ => Text.Of("athlon.course.practice")
    };

    public static string AthlonStatName(AthleteStat stat) => stat switch
    {
        AthleteStat.Speed => Text.Of("athlon.stat.speed"),
        AthleteStat.Power => Text.Of("athlon.stat.power"),
        AthleteStat.Skill => Text.Of("athlon.stat.skill"),
        AthleteStat.Stamina => Text.Of("athlon.stat.stamina"),
        _ => Text.Of("athlon.stat.jump")
    };

    public static string AthlonTeamName(AthlonTeam team) => team.IsPlayer ? PlayerIdentity.Name : team.Name switch
    {
        "ace" => Text.Of("athlon.rivals.ace"),
        "bolt" => Text.Of("athlon.rivals.bolt"),
        "comet" => Text.Of("athlon.rivals.comet"),
        "dash" => Text.Of("athlon.rivals.dash"),
        "ember" => Text.Of("athlon.rivals.ember"),
        "flint" => Text.Of("athlon.rivals.flint"),
        "gale" => Text.Of("athlon.rivals.gale"),
        _ => Text.Of("athlon.rivals.hazel")
    };

    // ------------------------------------------------------------------ parts

    /// <summary>A Pokémon's menu icon at twice its size, its middle at the point given.</summary>
    private static void AthleteIcon(string species, Vector2 c, float alpha = 1f, float scale = 2f)
    {
        var icon = PixelArtGenerator.GetPokemonIcon(species);
        float w = icon.Width * scale, h = icon.Height * scale;
        Raylib.DrawTexturePro(icon, new Rectangle(0, 0, icon.Width, icon.Height),
            new Rectangle(MathF.Round(c.X - w / 2f), MathF.Round(c.Y - h / 2f), w, h), Vector2.Zero, 0f,
            new Color(255, 255, 255, (int)(255 * Math.Clamp(alpha, 0f, 1f))));
    }

    /// <summary>A stat as five stars (discs here): filled in the dome's blue up to its value.</summary>
    private static void StatPips(float x, float cy, int value, float r = 11f)
    {
        for (int i = 0; i < AthleteStats.Most; i++)
        {
            var c = new Vector2(x + r + i * (2 * r + 8), cy);
            UiShapes.Circle(c, r, i < value ? AthlonSky : Rule);
        }
    }

    private static void TextMiddle(string text, float cx, float cy, float size, Color color, UiWeight weight) =>
        UiFonts.DrawCentered(text, cx - UiFonts.Measure(text, size, weight) / 2f, cy, size, color, weight);

    private static void TeamChip(Rectangle r, AthlonTeam team, int index)
    {
        UiShapes.Fill(r, r.Height / 2f, AthlonTeamColors[index]);
        UiFonts.DrawCentered(AthlonTeamName(team), r.X + 18, r.Y + r.Height / 2f, r.Height * 0.55f, Color.White, UiWeight.Black);
    }

    // ------------------------------------------------------------------ the course choice

    private static void AthlonCoursePage(int sw, AthlonScreen screen, float rise)
    {
        ScreenTitle(Text.Of("athlon.title"));
        Hints(sw - Margin, 44, Hint("shared.keys.z", "shared.hints.choose"), Hint("shared.keys.x", "shared.hints.back"));
        var list = new Rectangle(Margin, ContentTop + rise, 860, 600);
        Panel(list, 30);
        Label(Text.Of("athlon.courses"), list.X + 32, list.Y + 18);
        for (int i = 0; i < AthlonCoursesList.Count; i++)
        {
            var course = AthlonCoursesList[i];
            var row = RowRect(list, i, 52);
            var ink = ListRow(row, i == screen.Cursor, AthlonCourseName(course), nameX: 32);
            int best = screen.Records.BestOf(course.Key);
            RowValue(row, best > 0 ? Text.Of("athlon.best", ("score", best)) : Text.Of("athlon.noBest"), ink, 28);
        }

        // The course under the cursor: its three events, each with the stat it leans on
        var course0 = screen.Course;
        var right = new Rectangle(Margin + 860 + Gutter, ContentTop + rise, sw - Margin * 2 - 860 - Gutter, 600);
        Panel(right, 30);
        Label(Text.Of("athlon.events"), right.X + 32, right.Y + 18);
        for (int i = 0; i < course0.Events.Count; i++)
        {
            var card = new Rectangle(right.X + 28, right.Y + 60 + i * 170, right.Width - 56, 150);
            UiShapes.Fill(card, 24, Lighter(AthlonSky, 0.8f));
            UiFonts.Draw($"{i + 1}", card.X + 28, card.Y + 30, 64, AthlonSky, UiWeight.Black);
            UiFonts.Draw(AthlonEventName(course0.Events[i]), card.X + 110, card.Y + 30, 40, Ink, UiWeight.Black);
            var leans = AthlonEvent.Create(course0.Events[i], Placeholder, 0).Leans;
            UiFonts.Draw(Text.Of("athlon.leans", ("stat", AthlonStatName(leans))), card.X + 110, card.Y + 88, 26, Muted, UiWeight.ExtraBold);
        }

        var foot = new Rectangle(Margin, 940 + rise, sw - Margin * 2, 92);
        Panel(foot, 30);
        UiFonts.DrawCentered(Text.Of("athlon.points", ("points", screen.Records.Points.ToString("N0"))), foot.X + 44, foot.Y + foot.Height / 2f, 36, Ink, UiWeight.ExtraBold);
    }

    private static System.Collections.Generic.IReadOnlyList<AthlonCourse> AthlonCoursesList => Models.AthlonCourses.All;

    // Four teams of one stand-in each, only to ask an event what it leans on
    private static readonly AthlonTeam[] Placeholder = Enumerable.Range(0, AthlonRun.TeamCount)
        .Select(i => new AthlonTeam("", i == 0, new[] { new Athlete("", new AthleteStats(1, 1, 1, 1, 1)), new Athlete("", new AthleteStats(1, 1, 1, 1, 1)), new Athlete("", new AthleteStats(1, 1, 1, 1, 1)) }, 0.5f))
        .ToArray();

    // ------------------------------------------------------------------ the team

    private static void AthlonTeamPage(int sw, AthlonScreen screen, float rise)
    {
        ScreenTitle(Text.Of("athlon.title"));
        Hints(sw - Margin, 44, Hint("shared.keys.z", "shared.hints.choose"), Hint("shared.keys.x", "shared.hints.back"));
        var list = new Rectangle(Margin, ContentTop + rise, 860, 6 * RowPitch + 48);
        Panel(list, 30);
        for (int i = 0; i < screen.Members.Count; i++)
        {
            var p = screen.Members[i];
            var row = RowRect(list, i);
            bool on = i == screen.Cursor;
            var ink = ListRow(row, on, p.DisplayName, enabled: !p.IsEgg);
            RowIcon(row, PixelArtGenerator.IconOf(p), 1f, on, faded: p.IsEgg);
            int place = screen.Chosen.ToList().IndexOf(i);
            if (place >= 0)
            {
                var tag = new Rectangle(row.X + row.Width - 96, row.Y + 20, 64, 44);
                UiShapes.Fill(tag, 22, Green);
                TextMiddle($"{place + 1}", tag.X + tag.Width / 2f, tag.Y + tag.Height / 2f, 30, Color.White, UiWeight.Black);
            }
            else if (p.IsEgg) RowValue(row, Text.Of("athlon.egg"), Muted, 26);
        }

        // The five stats of the one under the cursor
        var right = new Rectangle(Margin + 860 + Gutter, ContentTop + rise, sw - Margin * 2 - 860 - Gutter, 6 * RowPitch + 48);
        Panel(right, 30);
        if (screen.Cursor < screen.Members.Count && !screen.Members[screen.Cursor].IsEgg)
        {
            var p = screen.Members[screen.Cursor];
            var stats = AthleteStats.Of(p);
            UiShapes.Circle(new Vector2(right.X + 110, right.Y + 110), 76, Disc);
            AthleteIcon(AthleteStats.SpeciesOf(p).Name, new Vector2(right.X + 110, right.Y + 110));
            UiFonts.Draw(p.DisplayName, right.X + 214, right.Y + 64, 44, Ink, UiWeight.Black);
            UiFonts.Draw(Text.Of("athlon.performance"), right.X + 214, right.Y + 124, 24, Muted, UiWeight.Black);
            foreach (var stat in Enum.GetValues<AthleteStat>())
            {
                float y = right.Y + 230 + (int)stat * 72;
                UiFonts.DrawCentered(AthlonStatName(stat), right.X + 48, y, 32, Ink, UiWeight.ExtraBold);
                StatPips(right.X + 320, y, stats[stat], 15f);
            }
        }

        var foot = new Rectangle(Margin, 940 + rise, sw - Margin * 2, 92);
        Panel(foot, 30);
        UiFonts.DrawCentered(Text.Of("athlon.chooseThree", ("n", screen.Chosen.Count)), foot.X + 44, foot.Y + foot.Height / 2f, 36, Ink, UiWeight.ExtraBold);
    }

    // ------------------------------------------------------------------ an event's briefing

    private static void AthlonBriefing(int sw, AthlonScreen screen, float rise)
    {
        var run = screen.Run!;
        var kind = run.Course.Events[run.EventIndex];
        ScreenTitle(AthlonCourseName(run.Course));
        Hints(sw - Margin, 44, Hint("shared.keys.z", "athlon.start"));
        var card = new Rectangle(sw / 2f - 620, ContentTop + 20 + rise, 1240, 780);
        Panel(card, 36);
        UiShapes.Fill(new Rectangle(card.X, card.Y, card.Width, 130), 36, AthlonSky);
        UiFonts.Draw(Text.Of("athlon.eventOf", ("n", run.EventIndex + 1), ("of", run.Course.Events.Count)), card.X + 48, card.Y + 22, 26, Color.White, UiWeight.Black);
        UiFonts.Draw(AthlonEventName(kind), card.X + 48, card.Y + 52, 60, Color.White, UiWeight.Black);
        var leans = AthlonEvent.Create(kind, Placeholder, 0).Leans;
        string lean = Text.Of("athlon.leans", ("stat", AthlonStatName(leans)));
        UiFonts.Draw(lean, card.X + card.Width - 48 - UiFonts.Measure(lean, 28, UiWeight.Black), card.Y + 70, 28, Color.White, UiWeight.Black);

        // How it is played, wrapped to the card
        float y = card.Y + 170;
        foreach (var line in Wrap(AthlonHowTo(kind), card.Width - 96, 34, UiWeight.Bold))
        {
            UiFonts.Draw(line, card.X + 48, y, 34, Ink, UiWeight.Bold);
            y += 48;
        }

        // The player's three, each with its stat in what the event leans on
        var mine = run.Teams[0];
        for (int i = 0; i < mine.Athletes.Count; i++)
        {
            var a = mine.Athletes[i];
            var c = new Vector2(card.X + 230 + i * 390, card.Y + 560);
            UiShapes.Circle(c, 80, Disc);
            AthleteIcon(a.Species, c);
            TextMiddle(LocalNames.Species(a.Species), c.X, c.Y + 116, 30, Ink, UiWeight.Black);
            StatPips(c.X - 82, c.Y + 162, a.Stats[leans], 12f);
        }
    }

    // ------------------------------------------------------------------ an event under way

    private static void AthlonPlaying(int sw, int sh, AthlonScreen screen)
    {
        var run = screen.Run!;
        var ev = run.Current;
        if (ev == null) return;

        // The bar at the top: the event's name and its clock
        var bar = new Rectangle(Margin, 36, sw - Margin * 2, 92);
        Panel(bar, 28);
        UiFonts.DrawCentered(AthlonEventName(ev.Kind), bar.X + 36, bar.Y + bar.Height / 2f, 40, Ink, UiWeight.Black);
        int secs = (int)MathF.Ceiling(ev.SecondsLeft);
        string clock = $"{secs / 60}:{secs % 60:D2}";
        UiFonts.DrawCentered(clock, bar.X + bar.Width - 36 - UiFonts.Measure(clock, 44, UiWeight.Black), bar.Y + bar.Height / 2f, 44,
            ev.SecondsLeft <= 5f ? Red : Ink, UiWeight.Black);
        float share = ev.Length > 0 ? 1f - (float)ev.Frames / ev.Length : 0f;
        UiShapes.Fill(new Rectangle(bar.X + 24, bar.Y + bar.Height - 10, (bar.Width - 48) * share, 6), 3, AthlonSky);

        switch (ev)
        {
            case HurdleDash h: DrawHurdleDash(sw, h); break;
            case BlockSmash b: DrawBlockSmash(sw, b); break;
            case RingDrop r: DrawRingDrop(sw, r); break;
            default: DrawAthlonBatch2(sw, ev); break;
        }

        // The count before it starts, and GO as it does
        if (screen.CountLeft > 0f || screen.PhaseAge < AthlonScreen.CountIn + 0.6f)
        {
            string word = screen.CountLeft > 0f ? ((int)MathF.Ceiling(screen.CountLeft)).ToString() : Text.Of("athlon.go");
            float within = screen.CountLeft > 0f ? screen.CountLeft % 1f : 1f - (screen.PhaseAge - AthlonScreen.CountIn) / 0.6f;
            float size = 200f + 80f * Math.Clamp(within, 0f, 1f);
            var middle = new Vector2(sw / 2f, sh / 2f);
            UiShapes.Circle(middle, 190, new Color(14, 22, 46, 150));
            TextMiddle(word, middle.X, middle.Y, size * 0.7f, Color.White, UiWeight.Black);
        }
    }

    private static void DrawHurdleDash(int sw, HurdleDash ev)
    {
        // Four lanes; the view follows the player's runner, a unit of the track 1.2 of the layout
        const float scale = 1.2f, laneHeight = 190f;
        float left = Margin, width = sw - Margin * 2;
        var me = ev.Runners[0];
        float myAt = me.Finished ? HurdleDash.Legs * HurdleDash.LegLength : me.Leg * HurdleDash.LegLength + me.X;
        float view = myAt - 320f / scale;
        float total = HurdleDash.Legs * HurdleDash.LegLength;

        for (int t = 0; t < AthlonRun.TeamCount; t++)
        {
            var lane = new Rectangle(left, AthlonStage + t * laneHeight, width, laneHeight - 14);
            UiShapes.Fill(lane, 22, t % 2 == 0 ? new Color(206, 112, 82, 255) : new Color(196, 102, 74, 255));
            float ground = lane.Y + lane.Height - 34;
            UiShapes.Fill(new Rectangle(lane.X + 12, ground + 4, lane.Width - 24, 4), 2, new Color(255, 255, 255, 140));

            // Hurdles, the hand-overs and the finish, where they are in view
            for (int leg = 0; leg < HurdleDash.Legs; leg++)
            {
                for (int i = 0; i < HurdleDash.HurdlesPerLeg; i++)
                {
                    float x = lane.X + (leg * HurdleDash.LegLength + HurdleDash.HurdleAt(i) - view) * scale;
                    if (x < lane.X + 8 || x > lane.X + lane.Width - 8) continue;
                    UiShapes.Fill(new Rectangle(x - 4, ground - 52, 8, 56), 3, Color.White);
                    UiShapes.Fill(new Rectangle(x - 16, ground - 56, 32, 10), 4, Gold);
                }
                float hand = lane.X + ((leg + 1) * HurdleDash.LegLength - view) * scale;
                if (hand > lane.X && hand < lane.X + lane.Width)
                    UiShapes.Fill(new Rectangle(hand - 3, lane.Y + 10, 6, lane.Height - 20), 3, leg + 1 == HurdleDash.Legs ? Color.White : new Color(255, 255, 255, 120));
            }

            // The runner: up in a jump, flat and faded while it gets up from a trip
            var r = ev.Runners[t];
            float at = r.Finished ? total : r.Leg * HurdleDash.LegLength + r.X;
            float rx = lane.X + (at - view) * scale;
            float lift = 0f;
            if (r.Air > 0 && r.AirLength > 0)
            {
                float p = 1f - (float)r.Air / r.AirLength;
                lift = 4f * p * (1f - p) * 90f;
            }
            float clampedX = Math.Clamp(rx, lane.X + 60, lane.X + lane.Width - 60);
            bool off = clampedX != rx;
            UiShapes.Glow(new Vector2(clampedX, ground + 2), 40, 10, new Color(0, 0, 0, 70));
            if (!off) AthleteIcon(ev.RunnerOf(t).Species, new Vector2(rx, ground - 40 - lift), r.Down > 0 ? 0.5f : 1f);
            else
            {
                // Out of the view: an arrow at the lane's edge the way it is
                bool ahead = rx > clampedX;
                float ax = ahead ? lane.X + lane.Width - 30 : lane.X + 30, dir = ahead ? 1f : -1f, my = lane.Y + lane.Height / 2f;
                UiShapes.Triangle(new Vector2(ax + 18 * dir, my), new Vector2(ax - 12 * dir, my - 22), new Vector2(ax - 12 * dir, my + 22), AthlonTeamColors[t]);
            }

            // Whose lane it is, and which leg or the time it finished in
            TeamChip(new Rectangle(lane.X + 16, lane.Y + 12, 240, 40), ev.Teams[t], t);
            string where = r.Finished ? Text.Of("athlon.finished", ("time", (r.FinishedAt * AthlonEvent.Frame).ToString("0.0")))
                : Text.Of("athlon.leg", ("n", r.Leg + 1), ("of", HurdleDash.Legs));
            UiFonts.DrawCentered(where, lane.X + 276, lane.Y + 32, 26, Color.White, UiWeight.Black);
        }
    }

    private static void DrawBlockSmash(int sw, BlockSmash ev)
    {
        float width = (sw - Margin * 2 - 3 * Gutter) / AthlonRun.TeamCount;
        for (int t = 0; t < AthlonRun.TeamCount; t++)
        {
            var s = ev.Smashers[t];
            var col = new Rectangle(Margin + t * (width + Gutter), AthlonStage, width, AthlonStageBottom - AthlonStage);
            Panel(col, 28, frame: t == 0 ? AthlonTeamColors[0] : null);
            TeamChip(new Rectangle(col.X + 20, col.Y + 18, col.Width - 40, 44), ev.Teams[t], t);
            TextMiddle(s.Broken.ToString(), col.X + col.Width / 2f, col.Y + 120, 72, Ink, UiWeight.Black);
            TextMiddle(Text.Of("athlon.blocks"), col.X + col.Width / 2f, col.Y + 172, 22, Muted, UiWeight.Black);

            // The stack, the block being smashed on top cracking with the damage, shaking a moment as a hit lands
            float shake = s.SinceHit < 4 ? (s.SinceHit % 2 == 0 ? 4f : -4f) : 0f;
            float bx = col.X + col.Width / 2f - 90 + shake, top = col.Y + 220;
            for (int i = 0; i < 5; i++)
            {
                var block = new Rectangle(bx, top + i * 64, 180, 56);
                UiShapes.Shape(block, 10, new Color(178, 150, 118, 255), new Color(146, 118, 90, 255), new Color(104, 82, 62, 255), 3);
                if (i == 0 && s.Damage > 0)
                {
                    int cracks = 1 + s.Damage * 4 / BlockSmash.Toughness;
                    for (int k = 0; k < cracks; k++)
                    {
                        float cx = block.X + 30 + k * 36;
                        UiShapes.Line(new Vector2(cx, block.Y + 6), new Vector2(cx + 14, block.Y + 30), 3, new Color(70, 52, 38, 255));
                        UiShapes.Line(new Vector2(cx + 14, block.Y + 30), new Vector2(cx + 4, block.Y + 50), 3, new Color(70, 52, 38, 255));
                    }
                }
            }

            // The one at the stack, and the three's breath
            var active = ev.ActiveOf(t);
            AthleteIcon(active.Species, new Vector2(col.X + col.Width / 2f, top + 5 * 64 + 50), s.Switching > 0 ? 0.4f : 1f);
            for (int a = 0; a < AthlonRun.TeamSize; a++)
            {
                var track = new Rectangle(col.X + 28, col.Y + col.Height - 130 + a * 36, col.Width - 56, 22);
                UiShapes.Fill(track, 11, Rule);
                float left = 1f - (float)s.Fatigue[a] / BlockSmash.Tired;
                var fill = s.Fatigue[a] >= BlockSmash.Tired ? Red : a == s.Active ? AthlonTeamColors[t] : Muted;
                if (left > 0f) UiShapes.Fill(new Rectangle(track.X, track.Y, track.Width * left, track.Height), 11, fill);
            }
        }
    }

    private static void DrawRingDrop(int sw, RingDrop ev)
    {
        // The platform at the stage's middle, a unit 3.6 of the layout
        const float scale = 3.6f;
        var middle = new Vector2(sw / 2f, (AthlonStage + AthlonStageBottom) / 2f);
        float radius = RingDrop.Radius * scale;
        UiShapes.Circle(middle, radius + 22, new Color(30, 40, 70, 255));
        UiShapes.Circle(middle, radius, new Color(232, 222, 196, 255));
        UiShapes.Ring(middle, radius - 18, 8, AthlonSky);
        UiShapes.Ring(middle, radius * 0.45f, 6, Lighter(AthlonSky, 0.5f));

        for (int t = 0; t < AthlonRun.TeamCount; t++)
        {
            var p = ev.Pushers[t];
            if (!p.On) continue;
            var c = middle + p.At * scale;
            float body = RingDrop.Body * scale;
            if (p.Lunge > 0) UiShapes.Glow(c - p.Facing * body * 0.8f, body * 1.2f, body * 0.8f, AthlonTeamColors[t] with { A = 120 });
            UiShapes.Circle(c, body, AthlonTeamColors[t]);
            UiShapes.Circle(c, body - 8, Color.White);
            AthleteIcon(ev.ActiveOf(t).Species, c);
        }

        // Each team's corner: its knock-offs and falls, and who comes on next while one is out
        for (int t = 0; t < AthlonRun.TeamCount; t++)
        {
            var p = ev.Pushers[t];
            bool rightSide = t is 0 or 3, low = t is 0 or 1;
            var box = new Rectangle(rightSide ? sw - Margin - 380 : Margin, low ? AthlonStageBottom - 200 : AthlonStage, 380, 200);
            Panel(box, 26, frame: t == 0 ? AthlonTeamColors[0] : null);
            TeamChip(new Rectangle(box.X + 18, box.Y + 16, box.Width - 36, 40), ev.Teams[t], t);
            UiFonts.Draw(Text.Of("athlon.knocks", ("n", p.Knocks)), box.X + 28, box.Y + 76, 30, Ink, UiWeight.Black);
            UiFonts.Draw(Text.Of("athlon.falls", ("n", p.Falls)), box.X + 28, box.Y + 120, 26, Muted, UiWeight.Black);
            if (!p.On)
            {
                var next = ev.Teams[t].Athletes[(p.Active + 1) % AthlonRun.TeamSize];
                AthleteIcon(next.Species, new Vector2(box.X + box.Width - 70, box.Y + 130), 0.6f);
            }
        }
    }

    // ------------------------------------------------------------------ an event's points

    private static void AthlonEventPoints(int sw, AthlonScreen screen, float rise)
    {
        var run = screen.Run!;
        int e = run.EventIndex - 1;
        ScreenTitle(AthlonEventName(run.Course.Events[e]));
        Hints(sw - Margin, 44, Hint("shared.keys.z", "shared.hints.next"));
        var board = new Rectangle(Margin, ContentTop + rise, sw - Margin * 2, 4 * 168 + 48);
        Panel(board, 30);
        int most = Math.Max(1, Enumerable.Range(0, AthlonRun.TeamCount).Max(t => run.PointsOf(e, t)));
        float grow = UiMotion.EaseOut(Math.Clamp(screen.PhaseAge / 0.8f, 0f, 1f));
        for (int t = 0; t < AthlonRun.TeamCount; t++)
        {
            var row = new Rectangle(board.X + 24, board.Y + 24 + t * 168, board.Width - 48, 152);
            UiShapes.Fill(row, 24, t == 0 ? Lighter(AthlonTeamColors[0], 0.82f) : PanelBottom);
            TeamChip(new Rectangle(row.X + 20, row.Y + 20, 300, 48), run.Teams[t], t);
            for (int a = 0; a < AthlonRun.TeamSize; a++) AthleteIcon(run.Teams[t].Athletes[a].Species, new Vector2(row.X + 70 + a * 100, row.Y + 112));
            int pts = run.PointsOf(e, t);
            var track = new Rectangle(row.X + 380, row.Y + 52, row.Width - 600, 48);
            UiShapes.Fill(track, 24, Rule);
            UiShapes.Fill(new Rectangle(track.X, track.Y, Math.Max(48, track.Width * pts / most * grow), track.Height), 24, AthlonTeamColors[t]);
            RowValue(row, ((int)MathF.Round(pts * grow)).ToString(), Ink, 56);
        }
    }

    // ------------------------------------------------------------------ the scoreboard

    private static void AthlonScoreboard(int sw, AthlonScreen screen, float rise)
    {
        var run = screen.Run!;
        ScreenTitle(Text.Of("athlon.scoreboard"));
        Hints(sw - Margin, 44, Hint("shared.keys.z", "shared.hints.close"));
        var board = new Rectangle(Margin, ContentTop + rise, sw - Margin * 2, 4 * 160 + 96);
        Panel(board, 30);

        // The events' names over their columns
        float firstColumn = board.X + 900, column = 220;
        for (int e = 0; e < run.Course.Events.Count; e++)
            TextMiddle(AthlonEventName(run.Course.Events[e]), firstColumn + e * column + column / 2f, board.Y + 40, 22, Muted, UiWeight.Black);
        TextMiddle(Text.Of("athlon.total"), board.X + board.Width - 130, board.Y + 40, 22, Muted, UiWeight.Black);

        var ranking = run.Ranking;
        for (int place = 0; place < ranking.Count; place++)
        {
            int t = ranking[place];
            var row = new Rectangle(board.X + 24, board.Y + 72 + place * 160, board.Width - 48, 144);
            UiShapes.Fill(row, 24, t == 0 ? Lighter(AthlonTeamColors[0], 0.82f) : PanelBottom);
            TextMiddle($"{place + 1}", row.X + 56, row.Y + row.Height / 2f, 64, place == 0 ? Gold : Muted, UiWeight.Black);
            TeamChip(new Rectangle(row.X + 120, row.Y + 18, 300, 48), run.Teams[t], t);
            for (int a = 0; a < AthlonRun.TeamSize; a++) AthleteIcon(run.Teams[t].Athletes[a].Species, new Vector2(row.X + 170 + a * 100, row.Y + 104));
            for (int e = 0; e < run.Course.Events.Count; e++)
                TextMiddle(run.PointsOf(e, t).ToString(), firstColumn + e * column + column / 2f, row.Y + row.Height / 2f, 40, Ink, UiWeight.Black);
            TextMiddle(run.Total(t).ToString(), board.X + board.Width - 130, row.Y + row.Height / 2f, 56, Ink, UiWeight.Black);
        }

        var foot = new Rectangle(Margin, 940 + rise, sw - Margin * 2, 92);
        Panel(foot, 30);
        UiFonts.DrawCentered(Text.Of("athlon.earned", ("points", run.Earned), ("all", screen.Records.Points.ToString("N0"))),
            foot.X + 44, foot.Y + foot.Height / 2f, 36, Ink, UiWeight.ExtraBold);
        if (screen.NewRecord)
        {
            string tag = Text.Of("athlon.newRecord");
            float w = UiFonts.Measure(tag, 28, UiWeight.Black) + 48;
            var pill = new Rectangle(foot.X + foot.Width - 32 - w, foot.Y + 22, w, 48);
            UiShapes.Fill(pill, 24, Gold);
            TextMiddle(tag, pill.X + pill.Width / 2f, pill.Y + pill.Height / 2f, 28, Color.White, UiWeight.Black);
        }
    }
}
