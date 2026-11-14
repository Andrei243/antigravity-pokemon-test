using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Rides;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// Trick surfing on screen (plan 42 · PS7; style guide, "The rides"): the course's briefing on the Pokéathlon's card, the
/// player's wave from the side with the Mantine riding it and its tricks turning the icon, the club's standings at the
/// right, the results and the Battle Points. Whose a thing is goes by the Pokéathlon's team colours.
/// </summary>
internal static partial class ModernUi
{
    /// <summary>The surf's own colour: the briefing's band, the clock's bar and the wave's lip.</summary>
    public static readonly Color SurfTeal = new(34, 170, 186, 255);

    private static readonly Color SurfSky = new(186, 226, 238, 255), SurfSea = new(40, 120, 176, 255),
        SurfFaceTop = new(92, 196, 214, 255), SurfFoam = new(240, 250, 252, 255), SurfStreak = new(70, 150, 200, 255);

    /// <summary>How far apart the sea's streaks of foam run.</summary>
    private const float WaveStreakGap = 240f;

    /// <summary>The wave's trough, crest and where the crest stands across the screen, and the rider's place across it.</summary>
    public const float WaveTrough = 860f, WaveCrest = 470f, WaveCrestX = 1080f, WaveRepeat = 1440f, SeaBack = 520f;

    public static void DrawRide(int sw, int sh, RideScreen screen)
    {
        Backdrop(sw, sh);
        float rise = (1f - UiMotion.EaseOut(screen.Appear)) * 60f;
        switch (screen.Phase)
        {
            case RidePhase.Briefing: RideBriefing(sw, screen, rise); break;
            case RidePhase.Riding: RideRiding(sw, sh, screen); break;
            case RidePhase.Results: RideResults(sw, screen, rise); break;
            case RidePhase.Points: RidePoints(sw, sh, screen, rise); break;
        }
    }

    // ------------------------------------------------------------------ words

    public static string RideCourseName(SurfCourse course)
    {
        string id = course.Id;
        return Text.Of($"rides.course.{id}");
    }

    public static string RideRiderName(AthlonTeam team)
    {
        if (team.IsPlayer) return PlayerIdentity.Name;
        string name = team.Name;
        return Text.Of($"rides.club.{name}");
    }

    public static string SurfTrickName(SurfTrick trick) => trick switch
    {
        SurfTrick.Spin => Text.Of("rides.trick.spin"),
        SurfTrick.Roll => Text.Of("rides.trick.roll"),
        SurfTrick.Flip => Text.Of("rides.trick.flip"),
        SurfTrick.Dive => Text.Of("rides.trick.dive"),
        _ => ""
    };

    private static void RideChip(Rectangle r, AthlonTeam team, int index)
    {
        UiShapes.Fill(r, r.Height / 2f, AthlonTeamColors[index]);
        UiFonts.DrawCentered(RideRiderName(team), r.X + 18, r.Y + r.Height / 2f, r.Height * 0.55f, Color.White, UiWeight.Black);
    }

    // ------------------------------------------------------------------ the briefing

    private static void RideBriefing(int sw, RideScreen screen, float rise)
    {
        var course = screen.Course!;
        ScreenTitle(Text.Of("rides.title"));
        Hints(sw - Margin, 44, Hint("shared.keys.z", "rides.start"), Hint("shared.keys.esc", "shared.hints.back"));
        var card = new Rectangle(sw / 2f - 620, ContentTop + 20 + rise, 1240, 780);
        Panel(card, 36);
        UiShapes.Fill(new Rectangle(card.X, card.Y, card.Width, 130), 36, SurfTeal);
        UiFonts.Draw(Text.Of("rides.surfing"), card.X + 48, card.Y + 22, 26, Color.White, UiWeight.Black);
        UiFonts.Draw(RideCourseName(course), card.X + 48, card.Y + 52, 60, Color.White, UiWeight.Black);
        string secs = Text.Of("rides.seconds", ("n", course.Seconds));
        UiFonts.Draw(secs, card.X + card.Width - 48 - UiFonts.Measure(secs, 28, UiWeight.Black), card.Y + 70, 28, Color.White, UiWeight.Black);

        float y = card.Y + 166;
        foreach (var line in Wrap(Text.Of("rides.howTo"), card.Width - 96, 32, UiWeight.Bold))
        {
            UiFonts.Draw(line, card.X + 48, y, 32, Ink, UiWeight.Bold);
            y += 44;
        }

        var riders = SurfCourses.Riders(course);
        for (int t = 0; t < riders.Count; t++)
        {
            var c = new Vector2(card.X + 170 + t * 300, card.Y + 530);
            UiShapes.Circle(c, 80, Disc);
            AthleteIcon(SurfCourses.Mount, c);
            float w = Math.Max(160f, UiFonts.Measure(RideRiderName(riders[t]), 26, UiWeight.Black) + 40);
            RideChip(new Rectangle(c.X - w / 2f, c.Y + 96, w, 48), riders[t], t);
        }

        string best = screen.Records.BestOf(course.Id) is int b and > 0 ? Text.Of("rides.best", ("score", b.ToString("N0"))) : Text.Of("rides.notRidden");
        UiFonts.Draw(best, card.X + 48, card.Y + card.Height - 66, 30, Muted, UiWeight.ExtraBold);
        string pays = Text.Of("rides.pays", ("most", course.Pay.Max(p => p.Points)));
        UiFonts.Draw(pays, card.X + card.Width - 48 - UiFonts.Measure(pays, 30, UiWeight.ExtraBold), card.Y + card.Height - 66, 30, Muted, UiWeight.ExtraBold);
    }

    // ------------------------------------------------------------------ the ride

    /// <summary>The wave's height at a point across the screen: a half sine from the trough to the crest at <see cref="WaveCrestX"/>, repeating.</summary>
    public static float WaveY(float x)
    {
        float u = ((x - WaveCrestX) % WaveRepeat + WaveRepeat) % WaveRepeat / WaveRepeat;
        float lift = 0.5f + 0.5f * MathF.Cos(u * MathF.Tau);
        return WaveTrough - (WaveTrough - WaveCrest) * lift * lift;
    }

    /// <summary>Where across the screen the wave's front face stands at a height (0 the trough, 1 the crest): left of the crest.</summary>
    public static float FaceX(float height)
    {
        float lift = MathF.Sqrt(Math.Clamp(height, 0f, 1f));
        float u = MathF.Acos(Math.Clamp(2f * lift - 1f, -1f, 1f)) / MathF.Tau;
        return WaveCrestX - u * WaveRepeat;
    }

    private static void RideRiding(int sw, int sh, RideScreen screen)
    {
        var ride = screen.Ride!;
        var course = screen.Course!;
        var me = ride.Riders[0];

        // The stage: the sky, the sea's back with streaks of foam running left with the ground ridden, the wave's face
        // in strips with its lip of foam (the wave stands still: the rider rides its face)
        UiShapes.Fill(new Rectangle(0, AthlonStage, sw, SeaBack - AthlonStage), 0, SurfSky);
        UiShapes.Fill(new Rectangle(0, SeaBack, sw, AthlonStageBottom - SeaBack), 0, SurfSea);
        float scroll = MathF.Floor(me.Distance);
        for (int row = 0; row < 5; row++)
        {
            float y = SeaBack + 24 + row * 48;
            float shift = ((scroll * (0.4f + 0.15f * row) + row * 97) % WaveStreakGap + WaveStreakGap) % WaveStreakGap;
            for (float x = -shift; x < sw; x += WaveStreakGap)
                UiShapes.Fill(new Rectangle(MathF.Round(x), y, 48 + 16 * row, 6), 3, SurfStreak);
        }
        for (float x = 0; x < sw; x += 8)
        {
            float top = WaveY(x + 4);
            float share = (WaveTrough - top) / (WaveTrough - WaveCrest);
            var face = SurfMix(SurfSea, SurfFaceTop, Math.Clamp(share, 0f, 1f));
            UiShapes.Fill(new Rectangle(x, top, 8, AthlonStageBottom - top), 0, face);
            if (share > 0.82f) UiShapes.Fill(new Rectangle(x, top, 8, 14), 0, SurfFoam);
        }

        // The rider: on the face at its height, over the crest through its jump, bobbing in the trough after a fall
        float flown = me.AirLength > 0 ? 1f - (float)me.Air / me.AirLength : 0f;
        var at = me.Stance switch
        {
            TrickSurf.Stance.Air => new Vector2(FaceX(TrickSurf.CrestLine) + (WaveCrestX - FaceX(TrickSurf.CrestLine)) * flown,
                WaveY(FaceX(TrickSurf.CrestLine)) - 40f - Math.Min(300f, me.AirLength * 9f) * 4f * flown * (1f - flown)),
            TrickSurf.Stance.Down => new Vector2(FaceX(0.05f), WaveTrough - 30f + 6f * MathF.Sin(ride.Frames * 0.35f)),
            _ => new Vector2(FaceX(me.Face), WaveY(FaceX(me.Face)) - 40f)
        };
        at = new Vector2(MathF.Round(at.X), MathF.Round(at.Y));
        if (me.Stance == TrickSurf.Stance.Air && me.Air <= ride.Window && me.Air > 0)
        {
            float closing = 1f - (float)me.Air / Math.Max(1, ride.Window);
            UiShapes.Ring(at, 120f - 60f * closing, 6f, Selection);
        }
        SurfRider(at, me, ride);
        if (me.Stance == TrickSurf.Stance.Down)
        {
            for (int d = 0; d < 6; d++)
            {
                float a = d * MathF.Tau / 6f + ride.Frames * 0.05f;
                UiShapes.Circle(at + new Vector2(MathF.Cos(a) * 70f, MathF.Sin(a) * 26f - 10f), 7f, SurfFoam);
            }
        }

        // What the last landing said, and the trick's points rising from the rider
        int since = ride.Frames - me.LastLandingFrame;
        if (since is >= 0 and < 24 && me.LastLanding is SurfLanding.Straight or SurfLanding.Crooked)
        {
            bool straight = me.LastLanding == SurfLanding.Straight;
            string word = Text.Of(straight ? "rides.straight" : "rides.crooked");
            float ww = UiFonts.Measure(word, 40, UiWeight.Black) + 48;
            UiShapes.Fill(new Rectangle(at.X - ww / 2f, at.Y + 78, ww, 64), 32, Color.White);
            TextMiddle(word, at.X, at.Y + 110, 40, straight ? Green : Red, UiWeight.Black);
        }
        int popped = ride.Frames - me.LastTrickFrame;
        if (popped is >= 0 and < 24)
        {
            string pop = Text.Of("rides.pop", ("trick", SurfTrickName(me.LastTrick)), ("points", me.LastTrickPoints.ToString("N0")));
            float w = UiFonts.Measure(pop, 40, UiWeight.Black) + 48;
            float y = at.Y - 150 - popped * 3f;
            UiShapes.Fill(new Rectangle(at.X - w / 2f, y - 32, w, 64), 32, new Color(14, 22, 46, 200));
            TextMiddle(pop, at.X, y, 40, Color.White, UiWeight.Black);
        }

        // The bar at the top: the course's name and its clock
        var bar = new Rectangle(Margin, 36, sw - Margin * 2, 92);
        Panel(bar, 28);
        UiFonts.DrawCentered(RideCourseName(course), bar.X + 36, bar.Y + bar.Height / 2f, 40, Ink, UiWeight.Black);
        int secs = (int)MathF.Ceiling(ride.SecondsLeft);
        string clock = $"{secs / 60}:{secs % 60:D2}";
        UiFonts.DrawCentered(clock, bar.X + bar.Width - 36 - UiFonts.Measure(clock, 44, UiWeight.Black), bar.Y + bar.Height / 2f, 44,
            ride.SecondsLeft <= 5f ? Red : Ink, UiWeight.Black);
        float share2 = ride.Length > 0 ? 1f - (float)ride.Frames / ride.Length : 0f;
        UiShapes.Fill(new Rectangle(bar.X + 24, bar.Y + bar.Height - 10, (bar.Width - 48) * share2, 6), 3, SurfTeal);

        // The combo under the bar
        if (me.Combo >= 2)
            UiFonts.Draw(Text.Of("rides.combo", ("n", me.Combo)), Margin + 12, 160, 56, Gold, UiWeight.Black);

        // The club's standings at the right
        var panel = new Rectangle(sw - Margin - 360, 168, 360, 4 * 86 + 24);
        Panel(panel, 26);
        var order = ride.Ranking;
        for (int i = 0; i < order.Count; i++)
        {
            int t = order[i];
            var row = new Rectangle(panel.X + 16, panel.Y + 12 + i * 86, panel.Width - 32, 76);
            if (t == 0) UiShapes.Fill(row, 20, Lighter(AthlonTeamColors[0], 0.82f));
            float w = Math.Min(190f, UiFonts.Measure(RideRiderName(ride.Teams[t]), 22, UiWeight.Black) + 36);
            RideChip(new Rectangle(row.X + 8, row.Y + 8, w, 40), ride.Teams[t], t);
            string score = ride.Riders[t].Score.ToString("N0");
            UiFonts.DrawCentered(score, row.X + row.Width - 12 - UiFonts.Measure(score, 40, UiWeight.Black), row.Y + 30, 40, Ink, UiWeight.Black);
            if (ride.Riders[t].Combo >= 2)
                UiFonts.Draw(Text.Of("rides.combo", ("n", ride.Riders[t].Combo)), row.X + 12, row.Y + 50, 22, Gold, UiWeight.Black);
        }

        // The count before it starts, and GO as it does
        if (screen.CountLeft > 0f || screen.PhaseAge < RideScreen.CountIn + 0.6f)
        {
            string word = screen.CountLeft > 0f ? ((int)MathF.Ceiling(screen.CountLeft)).ToString() : Text.Of("rides.go");
            float within = screen.CountLeft > 0f ? screen.CountLeft % 1f : 1f - (screen.PhaseAge - RideScreen.CountIn) / 0.6f;
            float size = 200f + 80f * Math.Clamp(within, 0f, 1f);
            var middle = new Vector2(sw / 2f, sh / 2f);
            UiShapes.Circle(middle, 190, new Color(14, 22, 46, 150));
            TextMiddle(word, middle.X, middle.Y, size * 0.7f, Color.White, UiWeight.Black);
        }
    }

    /// <summary>The Mantine at three times its icon's size, turned by the trick under way.</summary>
    private static void SurfRider(Vector2 c, TrickSurf.Rider me, TrickSurf ride)
    {
        var icon = PixelArtGenerator.GetPokemonIcon(SurfCourses.Mount);
        float scale = 3f, w = icon.Width * scale, h = icon.Height * scale;
        float turn = 0f, squash = 1f;
        float done = me.TrickDone;
        switch (me.Trick)
        {
            case SurfTrick.Spin: turn = done * 360f; break;
            case SurfTrick.Roll: turn = MathF.Sin(done * MathF.Tau) * 180f; break;
            case SurfTrick.Flip: squash = MathF.Abs(MathF.Cos(done * MathF.PI)); break;
            case SurfTrick.Dive: turn = MathF.Sin(done * MathF.PI) * 90f; break;
        }
        float alpha = me.Stance == TrickSurf.Stance.Down ? 0.5f : 1f;
        float hh = Math.Max(2f, h * squash);
        Raylib.DrawTexturePro(icon, new Rectangle(0, 0, icon.Width, icon.Height),
            new Rectangle(MathF.Round(c.X), MathF.Round(c.Y), w, hh), new Vector2(w / 2f, hh / 2f), MathF.Round(turn),
            new Color(255, 255, 255, (int)(255 * alpha)));
    }

    private static Color SurfMix(Color a, Color b, float t) => new(
        (int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t), 255);

    // ------------------------------------------------------------------ the results and the points

    private static void RideResults(int sw, RideScreen screen, float rise)
    {
        var ride = screen.Ride!;
        ScreenTitle(Text.Of("rides.results"));
        Hints(sw - Margin, 44, Hint("shared.keys.z", "shared.hints.next"));
        var board = new Rectangle(Margin, ContentTop + rise, sw - Margin * 2, 4 * 160 + 96);
        Panel(board, 30);
        float scoreX = board.X + 820, comboX = board.X + 1160, tricksX = board.X + 1460;
        TextMiddle(Text.Of("rides.score"), scoreX, board.Y + 40, 22, Muted, UiWeight.Black);
        TextMiddle(Text.Of("rides.bestCombo"), comboX, board.Y + 40, 22, Muted, UiWeight.Black);
        TextMiddle(Text.Of("rides.tricks"), tricksX, board.Y + 40, 22, Muted, UiWeight.Black);
        var ranking = ride.Ranking;
        for (int place = 0; place < ranking.Count; place++)
        {
            int t = ranking[place];
            var r = ride.Riders[t];
            var row = new Rectangle(board.X + 24, board.Y + 72 + place * 160, board.Width - 48, 144);
            UiShapes.Fill(row, 24, t == 0 ? Lighter(AthlonTeamColors[0], 0.82f) : PanelBottom);
            TextMiddle($"{place + 1}", row.X + 56, row.Y + row.Height / 2f, 64, place == 0 ? Gold : Muted, UiWeight.Black);
            RideChip(new Rectangle(row.X + 120, row.Y + 18, 300, 48), ride.Teams[t], t);
            AthleteIcon(SurfCourses.Mount, new Vector2(row.X + 170, row.Y + 104));
            TextMiddle(r.Score.ToString("N0"), scoreX, row.Y + row.Height / 2f, 56, Ink, UiWeight.Black);
            TextMiddle(r.BestCombo.ToString(), comboX, row.Y + row.Height / 2f, 44, Ink, UiWeight.Black);
            TextMiddle(r.TricksLanded.ToString(), tricksX, row.Y + row.Height / 2f, 44, Ink, UiWeight.Black);
        }
        var foot = new Rectangle(Margin, 940 + rise, sw - Margin * 2, 92);
        Panel(foot, 30);
        UiFonts.DrawCentered(Text.Of("rides.yourScore", ("score", screen.Result.Score.ToString("N0"))), foot.X + 44, foot.Y + foot.Height / 2f, 36, Ink, UiWeight.ExtraBold);
        if (screen.Result.NewRecord)
        {
            string tag = Text.Of("rides.newRecord");
            float w = UiFonts.Measure(tag, 28, UiWeight.Black) + 48;
            var pill = new Rectangle(foot.X + foot.Width - 32 - w, foot.Y + 22, w, 48);
            UiShapes.Fill(pill, 24, Gold);
            TextMiddle(tag, pill.X + pill.Width / 2f, pill.Y + pill.Height / 2f, 28, Color.White, UiWeight.Black);
        }
    }

    private static void RidePoints(int sw, int sh, RideScreen screen, float rise)
    {
        ScreenTitle(Text.Of("rides.title"));
        Hints(sw - Margin, 44, Hint("shared.keys.z", "shared.hints.close"));
        var card = new Rectangle(sw / 2f - 450, sh / 2f - 210 + rise, 900, 420);
        Panel(card, 36);
        UiShapes.Fill(new Rectangle(card.X, card.Y, card.Width, 100), 36, SurfTeal);
        TextMiddle(Text.Of("rides.battlePoints"), card.X + card.Width / 2f, card.Y + 50, 40, Color.White, UiWeight.Black);
        int paid = screen.Result.Paid;
        if (paid > 0) TextMiddle($"+{paid}", card.X + card.Width / 2f, card.Y + 210, 120, Gold, UiWeight.Black);
        else TextMiddle(Text.Of("rides.none"), card.X + card.Width / 2f, card.Y + 210, 56, Muted, UiWeight.Black);
        TextMiddle(Text.Of("rides.held", ("points", screen.PointsHeld.ToString("N0"))), card.X + card.Width / 2f, card.Y + 340, 34, Ink, UiWeight.ExtraBold);
    }
}
