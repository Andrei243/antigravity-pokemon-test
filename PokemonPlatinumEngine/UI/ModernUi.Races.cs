using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Rides;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The riding race on screen (plan 42 · PS8; style guide, "The rides"): the track's briefing, four lanes seen from the
/// side with the player's rider held at a place across the screen while the ground, the rocks, the mud and the turns
/// slide by, the stamina and the standings, the results and the Battle Points (the surf's own points card).
/// </summary>
internal static partial class ModernUi
{
    /// <summary>The race's own colour: the briefing's band, the clock's bar and the finish's flag.</summary>
    public static readonly Color RaceGreen = new(88, 150, 70, 255);

    private static readonly Color RaceGrass = new(150, 200, 110, 255), RaceDirt = new(214, 186, 134, 255),
        RaceMud = new(110, 78, 52, 255), RaceRock = new(122, 126, 134, 255), RaceFlag = new(250, 250, 250, 255);

    /// <summary>How many screen units a track unit is, and where the player's rider stands across the screen.</summary>
    public const float RaceScale = 2.4f, RaceRiderX = 560f;

    public static string RaceTrackName(RaceTrack track)
    {
        string id = track.Id;
        return Text.Of($"rides.track.{id}");
    }

    public static string RaceRiderName(AthlonTeam team) => RideRiderName(team);

    private static void RaceBriefing(int sw, RideScreen screen, float rise)
    {
        var track = screen.Track!;
        ScreenTitle(Text.Of("rides.title"));
        Hints(sw - Margin, 44, Hint("shared.keys.z", "rides.start"), Hint("shared.keys.esc", "shared.hints.back"));
        var card = new Rectangle(sw / 2f - 620, ContentTop + 20 + rise, 1240, 780);
        Panel(card, 36);
        UiShapes.Fill(new Rectangle(card.X, card.Y, card.Width, 130), 36, RaceGreen);
        UiFonts.Draw(Text.Of("rides.racing"), card.X + 48, card.Y + 22, 26, Color.White, UiWeight.Black);
        UiFonts.Draw(RaceTrackName(track), card.X + 48, card.Y + 52, 60, Color.White, UiWeight.Black);
        string secs = Text.Of("rides.seconds", ("n", track.Seconds));
        UiFonts.Draw(secs, card.X + card.Width - 48 - UiFonts.Measure(secs, 28, UiWeight.Black), card.Y + 70, 28, Color.White, UiWeight.Black);

        float y = card.Y + 166;
        foreach (var line in Wrap(Text.Of("rides.raceHowTo"), card.Width - 96, 32, UiWeight.Bold))
        {
            UiFonts.Draw(line, card.X + 48, y, 32, Ink, UiWeight.Bold);
            y += 44;
        }

        var teams = RaceTracks.Teams(track, screen.Mount!);
        for (int t = 0; t < teams.Count; t++)
        {
            var c = new Vector2(card.X + 170 + t * 300, card.Y + 530);
            UiShapes.Circle(c, 80, Disc);
            AthleteIcon(teams[t].Athletes[0].Species, c);
            float w = Math.Max(160f, UiFonts.Measure(RaceRiderName(teams[t]), 26, UiWeight.Black) + 40);
            RideChip(new Rectangle(c.X - w / 2f, c.Y + 96, w, 48), teams[t], t);
        }

        string best = screen.Records.BestOf(RaceTracks.KeyOf(track)) > 0
            ? Text.Of("rides.best", ("score", screen.Records.BestOf(RaceTracks.KeyOf(track)).ToString("N0"))) : Text.Of("rides.notRidden");
        UiFonts.Draw(best, card.X + 48, card.Y + card.Height - 66, 30, Muted, UiWeight.ExtraBold);
        string pays = Text.Of("rides.racePays", ("most", track.Pay.DefaultIfEmpty(0).Max()));
        UiFonts.Draw(pays, card.X + card.Width - 48 - UiFonts.Measure(pays, 30, UiWeight.ExtraBold), card.Y + card.Height - 66, 30, Muted, UiWeight.ExtraBold);
    }

    private static void RaceRiding(int sw, int sh, RideScreen screen)
    {
        var race = screen.Race!;
        var track = screen.Track!;
        var me = race.Racers[0];

        float top = AthlonStage + 150, bottom = AthlonStageBottom - 30;
        float lane = (bottom - top) / 4f;
        UiShapes.Fill(new Rectangle(0, AthlonStage, sw, AthlonStageBottom - AthlonStage), 0, RaceGrass);
        float Across(float distance) => RaceRiderX + (distance - me.Distance) * RaceScale;

        for (int t = 0; t < 4; t++)
        {
            float y = top + t * lane;
            UiShapes.Fill(new Rectangle(0, y + 6, sw, lane - 12), 0, RaceDirt);
            // The ground's marks, sliding with the player's distance
            float shift = ((me.Distance * RaceScale) % 160f + 160f) % 160f;
            for (float x = -shift; x < sw; x += 160f)
                UiShapes.Fill(new Rectangle(MathF.Round(x), y + lane / 2f, 40, 6), 3, Lighter(RaceDirt, 0.35f));
            for (int i = 0; i < track.Obstacles.Count; i++)
            {
                var o = track.Obstacles[i];
                float x0 = Across(o.At);
                switch (o.Kind)
                {
                    case RaceObstacleKind.Mud:
                        UiShapes.Fill(new Rectangle(x0, y + 6, o.Length * RaceScale, lane - 12), 24, RaceMud);
                        break;
                    case RaceObstacleKind.Rock:
                        UiShapes.Circle(new Vector2(x0, y + lane / 2f + 14), lane * 0.28f, RaceRock);
                        UiShapes.Circle(new Vector2(x0 - 8, y + lane / 2f + 4), lane * 0.12f, Lighter(RaceRock, 0.3f));
                        break;
                    case RaceObstacleKind.Turn:
                        float reach = RidingRace.TurnReach * RaceScale;
                        bool leant = race.Racers[t].Leant[i];
                        UiShapes.Fill(new Rectangle(x0 - reach, y + 6, reach * 2f, lane - 12), 20, new Color(255, 255, 255, leant ? 70 : 38));
                        var tip = new Vector2(x0 + o.Dir * 30f, y + lane / 2f);
                        var side = new Vector2(0, 26f);
                        UiShapes.Triangle(tip, tip - new Vector2(o.Dir * 42f, 0) + side, tip - new Vector2(o.Dir * 42f, 0) - side, leant ? Green : Selection);
                        break;
                }
            }
            // The finish
            float fin = Across(track.Length);
            if (fin > -60 && fin < sw + 60)
                for (int k = 0; k < 6; k++)
                    UiShapes.Fill(new Rectangle(fin - 12 + (k % 2) * 12, y + 6 + k * ((lane - 12) / 6f), 12, (lane - 12) / 6f), 0, (k % 2) == 0 ? Color.Black : RaceFlag);
        }

        // The riders: each on its lane at its place across the screen, up in the air through a jump
        for (int t = 3; t >= 0; t--)
        {
            var r = race.Racers[t];
            float x = Math.Clamp(Across(r.Distance), -60f, sw + 60f);
            float y = top + t * lane + lane / 2f;
            float air = r.AirLength > 0 && r.Air > 0 ? (float)r.Air / r.AirLength : 0f;
            float lift = 4f * air * (1f - air) * lane * 0.7f;
            float bob = r.Speed > 0.5f && r.Air == 0 ? MathF.Sin(race.Frames * 0.9f + t) * 3f : 0f;
            var c = new Vector2(MathF.Round(x), MathF.Round(y - lift + bob));
            UiShapes.Circle(new Vector2(c.X, y + lane * 0.3f), lane * 0.22f, new Color(0, 0, 0, 50));
            UiShapes.Circle(c, lane * 0.36f, AthlonTeamColors[t] with { A = 230 });
            AthleteIcon(race.Teams[t].Athletes[0].Species, c, r.Stun > 0 && race.Frames % 6 < 3 ? 0.55f : 1f, 2f);
            if (r.Boosting) UiShapes.Fill(new Rectangle(c.X - 80, c.Y + 4, 40, 6), 3, Lighter(AthlonTeamColors[t], 0.5f));
        }

        // What the player's last rock or turn said
        int since = race.Frames - me.LastStumbleFrame;
        string? word = null;
        Color wordColor = Red;
        if (since is >= 0 and < 24) word = Text.Of("rides.stumble");
        else if (race.Frames - me.LastClearFrame is >= 0 and < 24) { word = Text.Of("rides.cleared"); wordColor = Green; }
        else if (race.Frames - me.LastTurnFrame is >= 0 and < 24) { word = Text.Of(me.LastTurnLeant ? "rides.leaned" : "rides.swung"); wordColor = me.LastTurnLeant ? Green : Red; }
        if (word != null)
        {
            float ww = UiFonts.Measure(word, 40, UiWeight.Black) + 48;
            UiShapes.Fill(new Rectangle(RaceRiderX - ww / 2f, top - 70, ww, 64), 32, Color.White);
            TextMiddle(word, RaceRiderX, top - 38, 40, wordColor, UiWeight.Black);
        }

        // The bar: the track and its clock
        var bar = new Rectangle(Margin, 36, sw - Margin * 2, 92);
        Panel(bar, 28);
        UiFonts.DrawCentered(RaceTrackName(track), bar.X + 36, bar.Y + bar.Height / 2f, 40, Ink, UiWeight.Black);
        int secs = (int)MathF.Ceiling(race.SecondsLeft);
        string clock = $"{secs / 60}:{secs % 60:D2}";
        UiFonts.DrawCentered(clock, bar.X + bar.Width - 36 - UiFonts.Measure(clock, 44, UiWeight.Black), bar.Y + bar.Height / 2f, 44,
            race.SecondsLeft <= 5f ? Red : Ink, UiWeight.Black);
        float shareLeft = race.Length > 0 ? 1f - (float)race.Frames / race.Length : 0f;
        UiShapes.Fill(new Rectangle(bar.X + 24, bar.Y + bar.Height - 10, (bar.Width - 48) * shareLeft, 6), 3, RaceGreen);

        // The stamina and the way along, under the bar
        UiFonts.Draw(Text.Of("rides.raceStamina"), Margin + 12, 150, 24, Muted, UiWeight.Black);
        var sb = new Rectangle(Margin + 12, 182, 360, 22);
        UiShapes.Fill(sb, 11, PanelBottom);
        UiShapes.Fill(new Rectangle(sb.X, sb.Y, sb.Width * Math.Clamp(me.Stamina, 0f, 1f), sb.Height), 11, me.Stamina < 0.25f ? Red : Green);
        var way = new Rectangle(Margin + 12, 218, 360, 12);
        UiShapes.Fill(way, 6, PanelBottom);
        for (int t = 0; t < 4; t++)
        {
            float share = Math.Clamp(race.Racers[t].Distance / Math.Max(1, track.Length), 0f, 1f);
            UiShapes.Circle(new Vector2(way.X + way.Width * share, way.Y + way.Height / 2f), 9, AthlonTeamColors[t]);
        }

        // The standings at the right
        var panel = new Rectangle(sw - Margin - 320, 150, 320, 4 * 56 + 24);
        Panel(panel, 26);
        var order = race.Ranking;
        for (int i = 0; i < order.Count; i++)
        {
            int t = order[i];
            var row = new Rectangle(panel.X + 16, panel.Y + 12 + i * 56, panel.Width - 32, 48);
            if (t == 0) UiShapes.Fill(row, 18, Lighter(AthlonTeamColors[0], 0.82f));
            TextMiddle($"{i + 1}", row.X + 24, row.Y + row.Height / 2f, 32, Muted, UiWeight.Black);
            float w = Math.Min(200f, UiFonts.Measure(RaceRiderName(race.Teams[t]), 22, UiWeight.Black) + 36);
            RideChip(new Rectangle(row.X + 52, row.Y + 4, w, 40), race.Teams[t], t);
            if (race.Racers[t].Finished) UiShapes.Circle(new Vector2(row.X + row.Width - 24, row.Y + row.Height / 2f), 9, Green);
        }

        if (screen.CountLeft > 0f || screen.PhaseAge < RideScreen.CountIn + 0.6f)
        {
            string count = screen.CountLeft > 0f ? ((int)MathF.Ceiling(screen.CountLeft)).ToString() : Text.Of("rides.go");
            float within = screen.CountLeft > 0f ? screen.CountLeft % 1f : 1f - (screen.PhaseAge - RideScreen.CountIn) / 0.6f;
            float size = 200f + 80f * Math.Clamp(within, 0f, 1f);
            var middle = new Vector2(sw / 2f, sh / 2f);
            UiShapes.Circle(middle, 190, new Color(14, 22, 46, 150));
            TextMiddle(count, middle.X, middle.Y, size * 0.7f, Color.White, UiWeight.Black);
        }
    }

    private static void RaceResults(int sw, RideScreen screen, float rise)
    {
        var race = screen.Race!;
        ScreenTitle(Text.Of("rides.results"));
        Hints(sw - Margin, 44, Hint("shared.keys.z", "shared.hints.next"));
        var board = new Rectangle(Margin, ContentTop + rise, sw - Margin * 2, 4 * 160 + 96);
        Panel(board, 30);
        float timeX = board.X + 820, stumbleX = board.X + 1160, turnX = board.X + 1460;
        TextMiddle(Text.Of("rides.raceTime"), timeX, board.Y + 40, 22, Muted, UiWeight.Black);
        TextMiddle(Text.Of("rides.raceStumbles"), stumbleX, board.Y + 40, 22, Muted, UiWeight.Black);
        TextMiddle(Text.Of("rides.raceTurns"), turnX, board.Y + 40, 22, Muted, UiWeight.Black);
        var ranking = race.Ranking;
        for (int place = 0; place < ranking.Count; place++)
        {
            int t = ranking[place];
            var r = race.Racers[t];
            var row = new Rectangle(board.X + 24, board.Y + 72 + place * 160, board.Width - 48, 144);
            UiShapes.Fill(row, 24, t == 0 ? Lighter(AthlonTeamColors[0], 0.82f) : PanelBottom);
            TextMiddle($"{place + 1}", row.X + 56, row.Y + row.Height / 2f, 64, place == 0 ? Gold : Muted, UiWeight.Black);
            RideChip(new Rectangle(row.X + 120, row.Y + 18, 300, 48), race.Teams[t], t);
            AthleteIcon(race.Teams[t].Athletes[0].Species, new Vector2(row.X + 170, row.Y + 104));
            string time = race.TimeOf(t) is { } s ? Text.Of("rides.raceHome", ("time", s.ToString("0.0"))) : Text.Of("rides.raceOut");
            TextMiddle(time, timeX, row.Y + row.Height / 2f, 52, Ink, UiWeight.Black);
            TextMiddle(r.Stumbles.ToString(), stumbleX, row.Y + row.Height / 2f, 44, Ink, UiWeight.Black);
            TextMiddle(r.TurnsLeant.ToString(), turnX, row.Y + row.Height / 2f, 44, Ink, UiWeight.Black);
        }
        var foot = new Rectangle(Margin, 940 + rise, sw - Margin * 2, 92);
        Panel(foot, 30);
        string yours = race.TimeOf(0) is { } mine
            ? Text.Of("rides.raceYourTime", ("time", mine.ToString("0.0")))
            : Text.Of("rides.raceYourPlace", ("place", screen.Outcome));
        UiFonts.DrawCentered(yours, foot.X + 44, foot.Y + foot.Height / 2f, 36, Ink, UiWeight.ExtraBold);
        if (screen.Result.NewRecord)
        {
            string tag = Text.Of("rides.newRecord");
            float w = UiFonts.Measure(tag, 28, UiWeight.Black) + 48;
            var pill = new Rectangle(foot.X + foot.Width - 32 - w, foot.Y + 22, w, 48);
            UiShapes.Fill(pill, 24, Gold);
            TextMiddle(tag, pill.X + pill.Width / 2f, pill.Y + pill.Height / 2f, 28, Color.White, UiWeight.Black);
        }
    }
}
