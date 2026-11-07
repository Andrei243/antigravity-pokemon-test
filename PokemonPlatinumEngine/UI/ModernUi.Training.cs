using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The effort drills on screen (plan 36 · TR2; style guide, "Training"): the stat chosen, a drill's briefing, the three
/// drills drawn flat in layout units with the kit's shapes and the Pokémon's own menu sprite, the grade and the effort
/// before and after. The club's colour is a warm orange; a grade's medal is the Pokéathlon's.
/// </summary>
internal static partial class ModernUi
{
    /// <summary>The training club's own colour: the briefing's band, the drill's clock and its middle ring.</summary>
    public static readonly Color TrainingOrange = new(232, 112, 64, 255);

    /// <summary>The colour of a shot or a pellet of energy (the special drills').</summary>
    public static readonly Color TrainingEnergy = new(150, 110, 236, 255);

    private const float TrainingStageTop = 154f, TrainingStageBottom = 1000f;

    public static void DrawTraining(int sw, int sh, TrainingScreen screen)
    {
        Backdrop(sw, sh);
        float rise = (1f - UiMotion.EaseOut(screen.Appear)) * 60f;
        switch (screen.Phase)
        {
            case TrainingPhase.Stats: TrainingStats(sw, screen, rise); break;
            case TrainingPhase.Briefing: TrainingBriefing(sw, screen, rise); break;
            case TrainingPhase.Playing: TrainingPlaying(sw, sh, screen); break;
            case TrainingPhase.Grade: TrainingGrade(sw, screen, rise); break;
            case TrainingPhase.Effort: TrainingEffort(sw, screen, rise); break;
        }
    }

    // ------------------------------------------------------------------ words

    /// <summary>A drill's key in <c>ui.json</c>: the throw and its shot, the guard and its shield, the sprint and the long run.</summary>
    private static string DrillKey(DrillKind kind, bool special) => (kind, special) switch
    {
        (DrillKind.Throw, false) => "throw",
        (DrillKind.Throw, true) => "shot",
        (DrillKind.Guard, false) => "guard",
        (DrillKind.Guard, true) => "shield",
        (_, false) => "sprint",
        _ => "run"
    };

    public static string DrillName(StatType stat)
    {
        var (kind, special) = DrillKinds.PlanFor(stat)!.Value;
        string key = DrillKey(kind, special);
        return Text.Of($"training.drill.{key}");
    }

    private static string DrillHowTo(StatType stat)
    {
        var (kind, special) = DrillKinds.PlanFor(stat)!.Value;
        string key = DrillKey(kind, special);
        return Text.Of($"training.howTo.{key}");
    }

    public static string GradeName(DrillGrade grade) => grade switch
    {
        DrillGrade.Gold => Text.Of("training.grade.gold"),
        DrillGrade.Silver => Text.Of("training.grade.silver"),
        _ => Text.Of("training.grade.bronze")
    };

    /// <summary>A grade's medal colour: the Pokéathlon's gold, silver and bronze.</summary>
    public static Color GradeColour(DrillGrade grade) => grade switch
    {
        DrillGrade.Gold => new Color(232, 184, 48, 255),
        DrillGrade.Silver => new Color(184, 192, 204, 255),
        _ => new Color(196, 128, 72, 255)
    };

    // ------------------------------------------------------------------ parts

    /// <summary>The Pokémon's own menu sprite (128 texels) at a scale, its foot at the point given.</summary>
    private static void TraineeSprite(Pokemon p, Vector2 foot, float scale, float alpha = 1f)
    {
        var tex = PixelArtGenerator.SpriteOf(p);
        float w = tex.Width * scale, h = tex.Height * scale;
        Raylib.DrawTexturePro(tex, new Rectangle(0, 0, tex.Width, tex.Height),
            new Rectangle(MathF.Round(foot.X - w / 2f), MathF.Round(foot.Y - h), w, h), Vector2.Zero, 0f,
            new Color(255, 255, 255, (int)(255 * Math.Clamp(alpha, 0f, 1f))));
    }

    /// <summary>A medal: a disc in the grade's colour with a darker rim and a white star.</summary>
    private static void GradeMedal(Vector2 c, float r, DrillGrade grade)
    {
        var colour = GradeColour(grade);
        UiShapes.Circle(c, r + 6, new Color((int)(colour.R * 0.6f), (int)(colour.G * 0.6f), (int)(colour.B * 0.6f), 255));
        UiShapes.Circle(c, r, colour);
        Star(c, r * 0.55f, Color.White);
    }

    /// <summary>An effort bar: the track, the effort before in the selection's colour and what a drill added in gold.</summary>
    private static void EffortBar(Rectangle track, int before, int after, int cap)
    {
        UiShapes.Fill(track, track.Height / 2f, Rule);
        float b = track.Width * Math.Clamp(before / (float)cap, 0f, 1f), a = track.Width * Math.Clamp(after / (float)cap, 0f, 1f);
        if (a > 0) UiShapes.Fill(new Rectangle(track.X, track.Y, Math.Max(a, track.Height), track.Height), track.Height / 2f, after >= cap ? Gold : TrainingOrange);
        if (b > 0) UiShapes.Fill(new Rectangle(track.X, track.Y, Math.Max(b, track.Height), track.Height), track.Height / 2f, before >= cap ? Gold : Selection);
    }

    private static Color Alpha(Color c, int alpha) => new(c.R, c.G, c.B, (byte)Math.Clamp(alpha, 0, 255));

    /// <summary>A small arrow pointing right, drawn (the typeface has none).</summary>
    private static void ArrowRight(float x, float cy, float size, Color colour) =>
        UiShapes.Triangle(new Vector2(x, cy - size / 2f), new Vector2(x, cy + size / 2f), new Vector2(x + size * 0.8f, cy), colour, 2f);

    // ------------------------------------------------------------------ the stat chosen

    private static void TrainingStats(int sw, TrainingScreen screen, float rise)
    {
        var p = screen.Pokemon!;
        ScreenTitle(Text.Of("training.title"));
        Hints(sw - Margin, 44, Hint("shared.keys.upDown", "shared.hints.move"), Hint("shared.keys.z", "shared.hints.choose"), Hint("shared.keys.x", "shared.hints.back"));
        int cap = (screen.Rules ?? Ruleset.Current).EvStatCap;

        // The trainee: its sprite on a disc, its name and level and its effort in all
        var left = new Rectangle(Margin - rise, ContentTop, 560, 780);
        Panel(left, 34);
        var disc = new Vector2(left.X + left.Width / 2f, left.Y + 230);
        UiShapes.Circle(disc, 170, Disc);
        TraineeSprite(p, disc + new Vector2(0, 128), 2f);
        TextMiddle(p.DisplayName, disc.X, left.Y + 460, 48, Ink, UiWeight.Black);
        TextMiddle(Text.Of("training.level", ("level", p.Level)), disc.X, left.Y + 516, 30, Muted, UiWeight.ExtraBold);
        Label(Text.Of("training.effort"), left.X + 44, left.Y + 590);
        int total = EffortRules.Total(p);
        string all = Text.Of("training.ofMost", ("n", total), ("most", EffortRules.MaxTotal));
        UiFonts.Draw(all, left.X + left.Width - 44 - UiFonts.Measure(all, 34, UiWeight.Black), left.Y + 580, 34, Ink, UiWeight.Black);
        EffortBar(new Rectangle(left.X + 44, left.Y + 636, left.Width - 88, 14), total, total, EffortRules.MaxTotal);
        DrawWrapped(Text.Of("training.free"), left.X + 44, left.Y + 676, left.Width - 88, 26, Muted, 34, UiWeight.ExtraBold);

        // The six stats, each with the drill that trains it, its effort and its best score
        var list = new Rectangle(Margin + 560 + Gutter + rise, ContentTop, sw - Margin * 2 - 560 - Gutter, 780);
        Panel(list, 34);
        for (int i = 0; i < DrillKinds.Stats.Length; i++)
        {
            var stat = DrillKinds.Stats[i];
            var row = new Rectangle(list.X + 24, list.Y + 24 + i * 122, list.Width - 48, 112);
            bool on = i == screen.Cursor;
            bool refused = screen.WhyNot(stat) != null;
            if (on)
            {
                UiShapes.Fill(row, 22, Lighter(Selection, 0.85f));
                UiShapes.Shape(row, 22, Color.Blank, Color.Blank, Selection, 4f);
            }
            var ink = refused ? Muted : Ink;
            UiFonts.Draw(Words.Stat(stat), row.X + 32, row.Y + 16, 36, ink, UiWeight.Black);
            UiFonts.Draw(DrillName(stat), row.X + 32, row.Y + 64, 24, Muted, UiWeight.ExtraBold);
            int effort = EffortRules.Get(p, stat);
            var track = new Rectangle(row.X + 400, row.Y + 50, 420, 14);
            EffortBar(track, effort, effort, cap);
            string n = Text.Of("training.ofMost", ("n", effort), ("most", cap));
            UiFonts.Draw(n, track.X + track.Width + 24, row.Y + 38, 28, ink, UiWeight.Black);
            int best = screen.Records.Best(stat);
            string bestText = best > 0 ? Text.Of("training.best", ("score", best)) : Text.Of("training.noBest");
            UiFonts.Draw(bestText, row.X + row.Width - 32 - UiFonts.Measure(bestText, 24, UiWeight.ExtraBold), row.Y + 42, 24, Muted, UiWeight.ExtraBold);
            if (screen.Records.HasGold(stat)) GradeMedal(new Vector2(row.X + row.Width - 32 - UiFonts.Measure(bestText, 24, UiWeight.ExtraBold) - 34, row.Y + 56), 16, DrillGrade.Gold);
        }

        // The foot: why the stat chosen can't be trained, or what to do
        var foot = new Rectangle(Margin, 940, sw - Margin * 2, 92);
        Panel(foot, 30);
        UiFonts.DrawCentered(screen.Refusal ?? Text.Of("training.chooseStat"), foot.X + 44, foot.Y + foot.Height / 2f, 34,
            screen.Refusal != null ? Red : Ink, UiWeight.ExtraBold);
    }

    // ------------------------------------------------------------------ the briefing

    private static void TrainingBriefing(int sw, TrainingScreen screen, float rise)
    {
        var drill = screen.Drill!;
        ScreenTitle(Text.Of("training.title"));
        Hints(sw - Margin, 44, Hint("shared.keys.z", "training.start"), Hint("shared.keys.x", "shared.hints.back"));
        var card = new Rectangle(sw / 2f - 620, ContentTop + 20 + rise, 1240, 820);
        Panel(card, 36);
        UiShapes.Fill(new Rectangle(card.X, card.Y, card.Width, 130), 36, TrainingOrange);
        UiFonts.Draw(Text.Of("training.trains", ("stat", Words.Stat(drill.Stat))), card.X + 48, card.Y + 22, 26, Color.White, UiWeight.Black);
        UiFonts.Draw(DrillName(drill.Stat), card.X + 48, card.Y + 52, 60, Color.White, UiWeight.Black);
        string length = Text.Of("training.seconds", ("n", TrainingDrill.Length / TrainingDrill.FramesPerSecond));
        UiFonts.Draw(length, card.X + card.Width - 48 - UiFonts.Measure(length, 28, UiWeight.Black), card.Y + 70, 28, Color.White, UiWeight.Black);

        float y = card.Y + 170;
        foreach (var line in Wrap(DrillHowTo(drill.Stat), card.Width - 96, 34, UiWeight.Bold))
        {
            UiFonts.Draw(line, card.X + 48, y, 34, Ink, UiWeight.Bold);
            y += 48;
        }

        // The three grades: the score each wants and the effort each gives
        for (int g = 0; g < 3; g++)
        {
            var grade = (DrillGrade)g;
            var c = new Vector2(card.X + 230 + g * 390, card.Y + 560);
            GradeMedal(c, 70, grade);
            TextMiddle(GradeName(grade), c.X, c.Y + 116, 32, Ink, UiWeight.Black);
            int from = grade switch { DrillGrade.Gold => DrillBands.Gold(drill.Kind, drill.Special), DrillGrade.Silver => DrillBands.Silver(drill.Kind, drill.Special), _ => 0 };
            TextMiddle(Text.Of("training.from", ("score", from)), c.X, c.Y + 160, 26, Muted, UiWeight.ExtraBold);
            TextMiddle(Text.Of("training.gives", ("n", EffortDrills.PointsFor(grade))), c.X, c.Y + 200, 26, TrainingOrange, UiWeight.Black);
        }
    }

    // ------------------------------------------------------------------ a drill under way

    private static void TrainingPlaying(int sw, int sh, TrainingScreen screen)
    {
        var drill = screen.Drill!;
        var p = screen.Pokemon!;

        // The bar at the top: the drill's name, its score and its clock
        var bar = new Rectangle(Margin, 36, sw - Margin * 2, 92);
        Panel(bar, 28);
        UiFonts.DrawCentered(DrillName(drill.Stat), bar.X + 36, bar.Y + bar.Height / 2f, 40, Ink, UiWeight.Black);
        string score = Text.Of("training.score", ("score", drill.Score));
        var standing = drill.Grade;
        float scoreX = bar.X + bar.Width / 2f - UiFonts.Measure(score, 40, UiWeight.Black) / 2f;
        UiFonts.DrawCentered(score, scoreX, bar.Y + bar.Height / 2f, 40, Ink, UiWeight.Black);
        GradeMedal(new Vector2(scoreX - 40, bar.Y + bar.Height / 2f), 18, standing);
        int secs = (int)MathF.Ceiling(drill.SecondsLeft);
        string clock = $"{secs / 60}:{secs % 60:D2}";
        UiFonts.DrawCentered(clock, bar.X + bar.Width - 36 - UiFonts.Measure(clock, 44, UiWeight.Black), bar.Y + bar.Height / 2f, 44,
            drill.SecondsLeft <= 5f ? Red : Ink, UiWeight.Black);
        float share = 1f - (float)drill.Frames / TrainingDrill.Length;
        UiShapes.Fill(new Rectangle(bar.X + 24, bar.Y + bar.Height - 10, (bar.Width - 48) * share, 6), 3, TrainingOrange);

        var stage = new Rectangle(Margin, TrainingStageTop, sw - Margin * 2, TrainingStageBottom - TrainingStageTop);
        switch (drill)
        {
            case ThrowDrill t: DrawThrowDrill(stage, t, p); break;
            case GuardDrill g: DrawGuardDrill(stage, g, p); break;
            case DashDrill d: DrawDashDrill(stage, d, p); break;
        }

        // The count before it starts, and GO as it does
        if (screen.CountLeft > 0f || screen.PhaseAge < TrainingScreen.CountIn + 0.6f)
        {
            string word = screen.CountLeft > 0f ? ((int)MathF.Ceiling(screen.CountLeft)).ToString() : Text.Of("training.go");
            float within = screen.CountLeft > 0f ? screen.CountLeft % 1f : 1f - (screen.PhaseAge - TrainingScreen.CountIn) / 0.6f;
            float size = 200f + 80f * Math.Clamp(within, 0f, 1f);
            var middle = new Vector2(sw / 2f, sh / 2f);
            UiShapes.Circle(middle, 190, new Color(14, 22, 46, 150));
            TextMiddle(word, middle.X, middle.Y, size * 0.7f, Color.White, UiWeight.Black);
        }
    }

    /// <summary>
    /// The throw: the back wall a pale panel across the stage's upper part with the target sliding along it (a cream board,
    /// a blue ring and an orange middle, shrinking with the drill), the floor under it with a line where the aim stands
    /// and the Pokémon over the aim; a shot a disc from the Pokémon to the wall, the last one's mark and points on the wall.
    /// </summary>
    private static void DrawThrowDrill(Rectangle stage, ThrowDrill t, Pokemon p)
    {
        var wall = new Rectangle(stage.X, stage.Y + 16, stage.Width, 520);
        UiShapes.Fill(wall, 26, new Color(226, 214, 192, 255));
        for (float x = wall.X + 120; x < wall.X + wall.Width - 40; x += 160)
            UiShapes.Fill(new Rectangle(x, wall.Y + 12, 4, wall.Height - 24), 2, new Color(210, 196, 172, 255));
        var floor = new Rectangle(stage.X, wall.Y + wall.Height + 16, stage.Width, stage.Y + stage.Height - wall.Y - wall.Height - 16);
        UiShapes.Fill(floor, 26, new Color(110, 150, 196, 255));
        UiShapes.Fill(new Rectangle(floor.X + 24, floor.Y + 40, floor.Width - 48, 4), 2, new Color(255, 255, 255, 120));

        float span = stage.Width - 240;
        float X(int x) => stage.X + 120 + x / (float)ThrowDrill.Width * span;
        float scale = span / ThrowDrill.Width;
        float cy = wall.Y + wall.Height / 2f;
        int f = t.Frames;
        float r = ThrowDrill.RadiusAt(f) * scale;
        var target = new Vector2(X(t.TargetAt(f)), cy);
        UiShapes.Circle(target + new Vector2(0, 8), r + 4, new Color(120, 100, 80, 90));
        UiShapes.Circle(target, r, new Color(250, 242, 222, 255));
        UiShapes.Ring(target, r, 4, new Color(150, 120, 90, 255));
        UiShapes.Circle(target, r * 2f / 3f, new Color(70, 120, 210, 255));
        UiShapes.Circle(target, r / 3f, TrainingOrange);

        // The last shot to land: a mark where it struck and its points, for two thirds of a second
        if (t.Last is { } last && f - last.Frame < 20)
        {
            var at = new Vector2(X(last.X), cy);
            float age = (f - last.Frame) / 20f;
            UiShapes.Circle(at, 12, new Color(60, 44, 36, (int)(220 * (1 - age))));
            string said = last.Points > 0 ? $"+{last.Points}" : Text.Of("training.miss");
            TextMiddle(said, at.X, cy - r - 40 - age * 30, 44, last.Points > 0 ? TrainingOrange : Muted, UiWeight.Black);
        }

        // The aim and the Pokémon over it
        float aim = X(t.AimX);
        float ground = floor.Y + floor.Height - 40;
        UiShapes.Fill(new Rectangle(aim - 3, floor.Y + 12, 6, floor.Height - 24), 3, new Color(255, 255, 255, t.CanFire ? 200 : 90));
        UiShapes.Triangle(new Vector2(aim - 16, floor.Y + 8), new Vector2(aim + 16, floor.Y + 8), new Vector2(aim, floor.Y + 30), Color.White, 2f);
        TraineeSprite(p, new Vector2(aim, ground), 1.5f);

        // Shots in flight, from the Pokémon to the wall
        foreach (var shot in t.InFlight)
        {
            float k = 1f - (shot.LandsAt - f) / (float)ThrowDrill.Flight;
            var from = new Vector2(aim, ground - 120);
            var to = new Vector2(X(shot.X), cy);
            var at = Vector2.Lerp(from, to, Math.Clamp(k, 0f, 1f));
            float size = 26f - 12f * k;
            if (t.Special)
            {
                UiShapes.Circle(at, size + 10, Alpha(TrainingEnergy, 90));
                UiShapes.Circle(at, size, TrainingEnergy);
                UiShapes.Circle(at, size * 0.5f, new Color(236, 226, 255, 255));
            }
            else
            {
                UiShapes.Circle(at, size, new Color(214, 160, 104, 255));
                UiShapes.Ring(at, size, 3, new Color(128, 86, 56, 255));
            }
        }
    }

    /// <summary>
    /// The guard: three lanes from the top of the stage to the guard's line, pellets falling down them (a dark ball, a
    /// glowing one in the special drill), the Pokémon at the foot of its lane with a shield raised over it and its lane
    /// lit; a pellet caught says its points over its lane, one missed says MISS, and a stunned guard is faded.
    /// </summary>
    private static void DrawGuardDrill(Rectangle stage, GuardDrill g, Pokemon p)
    {
        float laneW = stage.Width / GuardDrill.Lanes;
        float top = stage.Y + 16, line = stage.Y + stage.Height - 220;
        int f = g.Frames;
        for (int l = 0; l < GuardDrill.Lanes; l++)
        {
            var lane = new Rectangle(stage.X + l * laneW + 8, top, laneW - 16, stage.Height - 16);
            UiShapes.Fill(lane, 26, l == g.Lane ? Lighter(TrainingOrange, 0.78f) : new Color(226, 220, 204, 255));
            UiShapes.Fill(new Rectangle(lane.X + 20, line, lane.Width - 40, 6), 3, new Color(150, 120, 90, 160));
        }
        foreach (var pellet in g.Pellets)
        {
            int until = pellet.Arrives - f;
            if (pellet.Settled || until > GuardDrill.Fall || until < -GuardDrill.Window) continue;
            float k = 1f - until / (float)GuardDrill.Fall;
            var at = new Vector2(stage.X + (pellet.Lane + 0.5f) * laneW, top + 40 + (line - top - 40) * k);
            if (g.Special)
            {
                UiShapes.Circle(at, 34, Alpha(TrainingEnergy, 90));
                UiShapes.Circle(at, 22, TrainingEnergy);
            }
            else
            {
                UiShapes.Circle(at, 22, new Color(70, 64, 80, 255));
                UiShapes.Circle(at + new Vector2(-6, -6), 6, new Color(255, 255, 255, 120));
            }
        }
        float cx = stage.X + (g.Lane + 0.5f) * laneW;
        bool stunned = g.Stun > 0;
        TraineeSprite(p, new Vector2(cx, stage.Y + stage.Height - 24), 1.5f, stunned ? 0.45f : 1f);
        if (g.GuardUp)
        {
            var shield = new Rectangle(cx - 110, line - 22, 220, 30);
            UiShapes.Fill(shield, 15, g.Special ? TrainingEnergy : new Color(70, 120, 210, 255));
            UiShapes.Fill(new Rectangle(shield.X + 16, shield.Y + 6, shield.Width - 32, 6), 3, new Color(255, 255, 255, 150));
        }
        if (stunned)
            for (int i = 0; i < 3; i++)
            {
                float a = f * 0.25f + i * MathF.Tau / 3;
                UiShapes.Circle(new Vector2(cx + MathF.Cos(a) * 60, stage.Y + stage.Height - 220 + MathF.Sin(a) * 16), 8, Gold);
            }
        if (g.Last is { } last && f - last.Frame < 20)
        {
            float age = (f - last.Frame) / 20f;
            string said = last.Points > 0 ? $"+{last.Points}" : Text.Of("training.miss");
            TextMiddle(said, stage.X + (last.Lane + 0.5f) * laneW, line - 90 - age * 30, 44, last.Points > 0 ? TrainingOrange : Red, UiWeight.Black);
        }
    }

    /// <summary>
    /// The dash: a track across the stage, its marks and hurdles going by as the Pokémon runs, the Pokémon a third of the
    /// way along it rising through a jump and faded while it gets up from a stumble; over the track the beat (a ring that
    /// closes on a disc at each beat), the pace as a bar, the streak and the hurdles cleared, and in the long run the
    /// stamina as a second bar.
    /// </summary>
    private static void DrawDashDrill(Rectangle stage, DashDrill d, Pokemon p)
    {
        var sky = new Rectangle(stage.X, stage.Y + 16, stage.Width, 360);
        UiShapes.Fill(sky, 26, new Color(196, 222, 244, 255));
        var track = new Rectangle(stage.X, sky.Y + sky.Height + 16, stage.Width, stage.Y + stage.Height - sky.Y - sky.Height - 16);
        UiShapes.Fill(track, 26, new Color(206, 112, 82, 255));
        float ground = track.Y + track.Height - 90;
        UiShapes.Fill(new Rectangle(track.X + 12, ground + 4, track.Width - 24, 4), 2, new Color(255, 255, 255, 140));
        const float scale = 1f;
        float runner = track.X + 360;
        float dist = d.Distance;

        // Marks along the track every hundred units, and the hurdles
        for (int m = (int)((dist - 400) / 100); m * 100 < dist + track.Width; m++)
        {
            float x = runner + (m * 100 - dist) * scale;
            if (x < track.X + 12 || x > track.X + track.Width - 12) continue;
            UiShapes.Fill(new Rectangle(x - 2, ground + 18, 4, 30), 2, new Color(255, 255, 255, 90));
        }
        for (int h = 0; h < DashDrill.HurdleCount; h++)
        {
            float x = runner + (DashDrill.HurdleAt(h) - dist) * scale;
            if (x < track.X + 12 || x > track.X + track.Width - 12) continue;
            UiShapes.Fill(new Rectangle(x - 4, ground - 70, 8, 74), 3, Color.White);
            UiShapes.Fill(new Rectangle(x - 20, ground - 74, 40, 12), 4, Gold);
        }

        // The runner, up through a jump
        float lift = 0f;
        if (d.AirLeft > 0)
        {
            float k = 1f - d.AirLeft / (float)DashDrill.AirFrames;
            lift = 4f * k * (1f - k) * 170f;
        }
        TraineeSprite(p, new Vector2(runner, ground - lift), 1.5f, d.StunLeft > 0 ? 0.45f : 1f);

        // The beat: a ring closing on a disc
        var beat = new Vector2(sky.X + sky.Width - 140, sky.Y + 130);
        float phase = (d.Frames % d.BeatEvery) / (float)d.BeatEvery;
        UiShapes.Circle(beat, 40, TrainingOrange);
        UiShapes.Ring(beat, 40 + 70 * (1f - phase), 6, Alpha(TrainingOrange, (int)(220 * phase)));
        TextMiddle(Text.Of("training.beat"), beat.X, beat.Y + 90, 24, Muted, UiWeight.Black);

        // Pace, stamina, the streak and the hurdles
        float x0 = sky.X + 48, y0 = sky.Y + 40;
        Label(Text.Of("training.pace"), x0, y0);
        var paceTrack = new Rectangle(x0, y0 + 34, 640, 18);
        UiShapes.Fill(paceTrack, 9, Rule);
        UiShapes.Fill(new Rectangle(paceTrack.X, paceTrack.Y, Math.Max(18, paceTrack.Width * Math.Clamp(d.Pace / 7.5f, 0f, 1f)), 18), 9, TrainingOrange);
        if (d.Special)
        {
            Label(Text.Of("training.stamina"), x0, y0 + 80);
            var staminaTrack = new Rectangle(x0, y0 + 114, 640, 18);
            float share = d.Stamina / (float)DashDrill.StartStamina;
            UiShapes.Fill(staminaTrack, 9, Rule);
            UiShapes.Fill(new Rectangle(staminaTrack.X, staminaTrack.Y, Math.Max(18, staminaTrack.Width * share), 18), 9, share < 0.25f ? Red : Green);
        }
        UiFonts.Draw(Text.Of("training.streak", ("n", d.Streak)), x0, y0 + 170, 32, Ink, UiWeight.Black);
        UiFonts.Draw(Text.Of("training.hurdles", ("n", d.Cleared), ("of", DashDrill.HurdleCount)), x0 + 340, y0 + 170, 32, Ink, UiWeight.Black);
        string run = Text.Of("training.distance", ("n", (int)dist));
        UiFonts.Draw(run, x0, y0 + 230, 28, Muted, UiWeight.ExtraBold);
    }

    // ------------------------------------------------------------------ the grade

    private static void TrainingGrade(int sw, TrainingScreen screen, float rise)
    {
        var drill = screen.Drill!;
        ScreenTitle(Text.Of("training.title"));
        Hints(sw - Margin, 44, Hint("shared.keys.z", "shared.hints.next"));
        var card = new Rectangle(sw / 2f - 520, ContentTop + 40 + rise, 1040, 780);
        Panel(card, 36);
        TextMiddle(DrillName(drill.Stat), card.X + card.Width / 2f, card.Y + 70, 40, Muted, UiWeight.Black);
        var grade = drill.Grade;
        var c = new Vector2(card.X + card.Width / 2f, card.Y + 280);
        UiShapes.Glow(c, 260, 260, Alpha(GradeColour(grade), 90));
        GradeMedal(c, 140, grade);
        TextMiddle(GradeName(grade), c.X, card.Y + 486, 72, Ink, UiWeight.Black);
        TextMiddle(Text.Of("training.score", ("score", drill.Score)), c.X, card.Y + 566, 40, Ink, UiWeight.Black);
        string gain = screen.Gained > 0
            ? Text.Of("training.gain", ("n", screen.Gained), ("stat", Words.Stat(drill.Stat)))
            : Text.Of("training.noGain", ("stat", Words.Stat(drill.Stat)));
        TextMiddle(gain, c.X, card.Y + 636, 34, screen.Gained > 0 ? TrainingOrange : Muted, UiWeight.Black);
        if (screen.NewBest)
        {
            string best = Text.Of("training.newBest");
            float w = UiFonts.Measure(best, 28, UiWeight.Black) + 48;
            var pill = new Rectangle(c.X - w / 2f, card.Y + 690, w, 50);
            UiShapes.Fill(pill, 25, Gold);
            TextMiddle(best, c.X, pill.Y + 25, 28, Color.White, UiWeight.Black);
        }
    }

    // ------------------------------------------------------------------ the effort before and after

    private static void TrainingEffort(int sw, TrainingScreen screen, float rise)
    {
        var p = screen.Pokemon!;
        ScreenTitle(Text.Of("training.title"));
        Hints(sw - Margin, 44, Hint("shared.keys.z", "shared.hints.next"));
        int cap = (screen.Rules ?? Ruleset.Current).EvStatCap;
        var panel = new Rectangle(Margin, ContentTop + rise, sw - Margin * 2, 800);
        Panel(panel, 34);
        TraineeSprite(p, new Vector2(panel.X + 170, panel.Y + 260), 2f);
        TextMiddle(p.DisplayName, panel.X + 170, panel.Y + 310, 40, Ink, UiWeight.Black);
        Label(Text.Of("training.effort"), panel.X + 400, panel.Y + 28);
        for (int i = 0; i < 6; i++)
        {
            var stat = DrillKinds.Stats[i];
            float y = panel.Y + 80 + i * 112;
            bool trained = screen.Drill?.Stat == stat;
            UiFonts.Draw(Words.Stat(stat), panel.X + 400, y, 34, trained ? TrainingOrange : Ink, UiWeight.Black);
            EffortBar(new Rectangle(panel.X + 640, y + 16, 520, 16), screen.EffortBefore[i], screen.EffortAfter[i], cap);
            string before = Text.Of("training.ofMost", ("n", screen.EffortBefore[i]), ("most", cap));
            float x = panel.X + 1190;
            UiFonts.Draw(before, x, y + 2, 28, Muted, UiWeight.Black);
            if (screen.EffortAfter[i] != screen.EffortBefore[i])
            {
                float ax = x + UiFonts.Measure(before, 28, UiWeight.Black) + 16;
                ArrowRight(ax, y + 22, 22, TrainingOrange);
                UiFonts.Draw($"{screen.EffortAfter[i]}", ax + 30, y + 2, 28, TrainingOrange, UiWeight.Black);
            }
            string value = screen.ValueAfter[i] != screen.ValueBefore[i]
                ? Text.Of("training.valueRose", ("before", screen.ValueBefore[i]), ("after", screen.ValueAfter[i]))
                : $"{screen.ValueAfter[i]}";
            UiFonts.Draw(value, panel.X + panel.Width - 44 - UiFonts.Measure(value, 30, UiWeight.Black), y + 2, 30,
                screen.ValueAfter[i] != screen.ValueBefore[i] ? TrainingOrange : Ink, UiWeight.Black);
        }
        int total = EffortRules.Total(p);
        string all = Text.Of("training.ofMost", ("n", total), ("most", EffortRules.MaxTotal));
        Label(Text.Of("training.effortInAll"), panel.X + 400, panel.Y + 750);
        UiFonts.Draw(all, panel.X + 640, panel.Y + 740, 30, Ink, UiWeight.Black);
    }
}
