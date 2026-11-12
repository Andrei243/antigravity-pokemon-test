using System;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.StageShow;
using PokemonPlatinumEngine.UI.Kit;
using Raylib_cs;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The stage show on screen (plan 42 · PS1, PS2; style guide, "Interface (vector)", "The stage show"): the dressing room
/// and the stage. Every piece is ours.
/// </summary>
internal static partial class ModernUi
{
    private static readonly Color StageCurtain = new(140, 28, 44, 255), StageCurtainDeep = new(84, 14, 28, 255);
    private static readonly Color StageBoards = new(132, 86, 52, 255), StageBoardsDeep = new(86, 54, 32, 255);
    private static readonly Color StageBack = new(30, 22, 44, 255), StageBackDeep = new(14, 10, 24, 255);
    private static readonly Color StageLamp = new(255, 226, 150, 255), StageCrowd = new(10, 8, 18, 255);

    /// <summary>The colour of a mood (the style guide's): bright yellow, dashing red, gentle mint, eerie violet.</summary>
    public static Color MoodColour(PropMood mood) => mood switch
    {
        PropMood.Bright => new Color(242, 190, 60, 255),
        PropMood.Dashing => new Color(200, 52, 62, 255),
        PropMood.Gentle => new Color(120, 196, 172, 255),
        _ => new Color(132, 92, 196, 255)
    };

    // ------------------------------------------------------------------ the dressing room

    public static void DrawDressingRoom(int sw, int sh, DressingRoomScreen screen, float appear)
    {
        Raylib.DrawRectangleGradientV(0, 0, sw, sh, new Color(64, 36, 54, 255), new Color(28, 16, 30, 255));
        // Bulbs round the mirror
        for (int i = 0; i < 14; i++)
            UiShapes.Circle(new Vector2(560 + i * 62, 84), 11, StageLamp);
        ScreenTitle(Text.Of("stageShow.dressing.title"));
        float drop = (1f - UiMotion.EaseOut(appear)) * 50f;

        // ---- the dais in the middle, the Pokémon turning on it
        var c = new Vector2(sw / 2f, 520 + drop);
        UiShapes.Shape(new Rectangle(c.X - 260, c.Y + 230, 520, 70), 35, new Color(196, 160, 110, 255), new Color(120, 90, 58, 255), Darker(Gold, 0.3f), 4);
        if (screen.HasPicture) EvolutionFigure(screen.Picture, c, 620);
        else UiFonts.Draw(Text.Of("stageShow.dressing.wait"), c.X - 120, c.Y, 30, Color.White, UiWeight.ExtraBold);

        if (screen.Phase == DressingPhase.ChoosePokemon)
        {
            var list = new Rectangle(64, 160 + drop, 440, 90 + screen.Team.Count * 76);
            Panel(list, 26, border: 5, frame: Gold);
            Label(Text.Of("stageShow.dressing.choose"), list.X + 28, list.Y + 24);
            for (int i = 0; i < screen.Team.Count; i++)
            {
                var row = new Rectangle(list.X + 20, list.Y + 62 + i * 76, list.Width - 40, 64);
                bool on = i == screen.TeamCursor;
                UiShapes.Fill(row, 18, on ? Lighter(Gold, 0.4f) : new Color(240, 236, 228, 255));
                UiFonts.Draw(screen.Team[i].DisplayName, row.X + 22, row.Y + 14, 32, Ink, UiWeight.ExtraBold);
            }
            Hints(sw - Margin, 1040, Hint("shared.keys.z", "stageShow.hint.choose"), Hint("shared.keys.esc", "stageShow.hint.leave"));
            return;
        }

        // ---- the slots down the left
        var slots = new Rectangle(64, 160 + drop, 440, 100 + screen.Slots.Count * 108);
        Panel(slots, 26, border: 5, frame: Gold);
        Label(screen.Dressed?.DisplayName ?? "", slots.X + 28, slots.Y + 24);
        for (int i = 0; i < screen.Slots.Count; i++)
        {
            var slot = screen.Slots[i];
            var row = new Rectangle(slots.X + 20, slots.Y + 64 + i * 108, slots.Width - 40, 96);
            bool on = i == screen.SlotCursor;
            UiShapes.Fill(row, 20, on && screen.Phase == DressingPhase.Slots ? Lighter(Gold, 0.4f) : on ? Lighter(Gold, 0.7f) : new Color(240, 236, 228, 255));
            string slotName = slot.ToString().ToLowerInvariant();
            UiFonts.Draw(Text.Of($"stageShow.slot.{slotName}"), row.X + 22, row.Y + 10, 22, Muted, UiWeight.Black);
            var worn = screen.Dressing.In(slot) is { } id ? ShowPropDatabase.Get(id) : null;
            if (worn != null) PropChip(new Vector2(row.X + 22, row.Y + 42), worn, 30);
            else UiFonts.Draw(Text.Of("stageShow.dressing.nothing"), row.X + 22, row.Y + 44, 30, Muted, UiWeight.Bold);
        }

        // ---- the trunk's pieces at the right
        var trunk = new Rectangle(sw - 64 - 520, 160 + drop, 520, 620);
        Panel(trunk, 26, border: 5, frame: Gold);
        Label(Text.Of("stageShow.dressing.trunk"), trunk.X + 28, trunk.Y + 24);
        var pieces = screen.Pieces;
        int top = Math.Clamp(screen.PieceCursor - 5, 0, Math.Max(0, pieces.Count - 7));
        for (int k = top; k < Math.Min(pieces.Count, top + 7); k++)
        {
            var row = new Rectangle(trunk.X + 20, trunk.Y + 64 + (k - top) * 76, trunk.Width - 40, 66);
            bool on = screen.Phase == DressingPhase.Pieces && k == screen.PieceCursor;
            UiShapes.Fill(row, 18, on ? Lighter(Gold, 0.4f) : new Color(240, 236, 228, 255));
            if (pieces[k] is { } piece)
            {
                PropChip(new Vector2(row.X + 18, row.Y + 16), piece, 30);
                UiFonts.Draw("x" + (screen.Records?.Trunk.CountOf(piece.Id) ?? 0), row.X + row.Width - 64, row.Y + 18, 28, Muted, UiWeight.Black);
            }
            else UiFonts.Draw(Text.Of("stageShow.dressing.takeOff"), row.X + 18, row.Y + 16, 30, Muted, UiWeight.ExtraBold);
        }

        // ---- the moods worn
        var moods = new Rectangle(sw - 64 - 520, 800 + drop, 520, 150);
        Panel(moods, 26, border: 5, frame: Gold);
        Label(Text.Of("stageShow.dressing.moods"), moods.X + 28, moods.Y + 20);
        int m = 0;
        foreach (var mood in Enum.GetValues<PropMood>())
        {
            var at = new Vector2(moods.X + 40 + m * 118, moods.Y + 92);
            UiShapes.Circle(at, 26, MoodColour(mood));
            UiFonts.Draw(screen.MoodCount(mood).ToString(), at.X - 9, at.Y - 18, 32, Color.White, UiWeight.Black);
            string moodName = mood.ToString().ToLowerInvariant();
            UiFonts.Draw(Text.Of($"stageShow.mood.{moodName}"), at.X + 34, at.Y - 12, 20, Ink, UiWeight.Black);
            m++;
        }
        Hints(sw - Margin, 1040, Hint("shared.keys.z", "stageShow.hint.wear"), Hint("shared.keys.esc", "stageShow.hint.back"));
    }

    /// <summary>A piece's name on a chip of its mood's colour.</summary>
    private static void PropChip(Vector2 at, ShowProp piece, float size)
    {
        UiShapes.Circle(new Vector2(at.X + size / 2f, at.Y + size / 2f + 2), size / 2f, MoodColour(piece.Mood));
        var (r, g, b) = ShowPropDatabase.ColourOf(piece.Colors[0]);
        UiShapes.Circle(new Vector2(at.X + size / 2f, at.Y + size / 2f + 2), size / 3.2f, new Color(r, g, b, (byte)255));
        UiFonts.Draw(Text.Of(piece.NameKey), at.X + size + 14, at.Y, size, Ink, UiWeight.ExtraBold);
    }

    // ------------------------------------------------------------------ the stage

    public static void DrawStageShow(int sw, int sh, StageShowScreen screen)
    {
        // The stage: a back wall, the boards, the curtains at either side and the lamps along the foot
        Raylib.DrawRectangleGradientV(0, 0, sw, sh, StageBack, StageBackDeep);
        Raylib.DrawRectangleGradientV(0, 700, sw, 220, StageBoards, StageBoardsDeep);
        for (int x = 0; x < sw; x += 160) Raylib.DrawRectangle(x, 700, 4, 220, StageBoardsDeep);
        for (int k = 0; k < 6; k++)
        {
            UiShapes.Fill(new Rectangle(k * 36 - 20, -20, 60, 940), 30, k % 2 == 0 ? StageCurtain : StageCurtainDeep);
            UiShapes.Fill(new Rectangle(sw - k * 36 - 40, -20, 60, 940), 30, k % 2 == 0 ? StageCurtain : StageCurtainDeep);
        }
        UiShapes.Fill(new Rectangle(-20, -40, sw + 40, 110), 40, StageCurtain);
        for (int i = 0; i < 24; i++) UiShapes.Circle(new Vector2(40 + i * 80, 912), 9, StageLamp);

        var run = screen.Run;
        if (run != null)
        {
            int spot = run.Spotlight;
            for (int i = 0; i < 4; i++)
            {
                var foot = new Vector2(330 + i * 420, 790);
                if (i == spot)
                {
                    // The spotlight: a cone of warm light down on whoever has the cue
                    var top = new Vector2(foot.X, 60);
                    Raylib.DrawTriangle(top, new Vector2(foot.X - 190, foot.Y + 40), new Vector2(foot.X + 190, foot.Y + 40), new Color(255, 236, 170, 46));
                    UiShapes.Fill(new Rectangle(foot.X - 190, foot.Y - 10, 380, 70), 35, new Color(255, 236, 170, 70));
                }
                if (screen.Drawn(i)) EvolutionFigure(screen.PictureOf(i), new Vector2(foot.X, foot.Y - 190), 400);
                // The share of the crowd's eyes over each head
                var bar = new Rectangle(foot.X - 150, 110, 300, 26);
                UiShapes.Fill(bar, 13, new Color(0, 0, 0, 140));
                UiShapes.Fill(new Rectangle(bar.X, bar.Y, bar.Width * run.Eyes[i] / 100f, bar.Height), 13, i == 0 ? Gold : Lighter(Blue, 0.2f));
                UiFonts.Draw(run.Eyes[i] + "%", bar.X + bar.Width + 10, bar.Y - 4, 28, Color.White, UiWeight.Black);
                string who = i == 0 ? Text.Of("stageShow.stage.you") : PokemonPlatinumEngine.Data.LocalNames.Species(run.Performers[i].Species);
                UiFonts.Draw(who, bar.X, bar.Y + 32, 24, Color.White, UiWeight.ExtraBold);
            }

            // The crowd's heads along the foot, in silhouette
            for (int i = 0; i < 30; i++)
            {
                float bob = run.Spotlight >= 0 ? MathF.Abs(MathF.Sin(run.Frame * 0.2f + i)) * 6f : 0f;
                UiShapes.Circle(new Vector2(30 + i * 66, 1000 - bob), 36, StageCrowd);
                UiShapes.Fill(new Rectangle(i * 66 - 10, 1010 - bob, 80, 90), 30, StageCrowd);
            }

            // The beat along the foot: a tick for every beat of the bar, the one under way lit
            if (screen.Phase == StagePhase.Show && run.Frame < run.MusicFrames)
            {
                int beats = run.Show.BeatsPerBar;
                for (int b = 0; b < beats; b++)
                {
                    bool lit = run.Beat % beats == b;
                    var at = new Vector2(sw / 2f - (beats - 1) * 40 + b * 80, 950);
                    UiShapes.Circle(at, lit ? 22 : 14, lit ? (run.Spotlight == 0 ? Gold : Color.White) : new Color(255, 255, 255, 90));
                }
                if (run.Spotlight == 0)
                    UiFonts.Draw(Text.Of("stageShow.stage.yourCue"), sw / 2f - UiFonts.Measure(Text.Of("stageShow.stage.yourCue"), 34, UiWeight.Black) / 2f, 860, 34, Gold, UiWeight.Black);
            }
        }

        switch (screen.Phase)
        {
            case StagePhase.ChooseShow:
                StageList(sw, Text.Of("stageShow.stage.chooseShow"), screen.Shows.Select(s => Text.Of(s.NameKey)).ToList(), screen.ShowCursor,
                    screen.Shows.Select(s => MoodColour(s.Mood)).ToList());
                break;
            case StagePhase.ChoosePokemon:
                StageList(sw, Text.Of("stageShow.stage.choosePerformer"), screen.Team.Select(p => p.DisplayName).ToList(), screen.TeamCursor, null);
                break;
            case StagePhase.Curtain:
            {
                // The curtain rising
                float up = Math.Clamp(screen.PhaseAge / StageShowScreen.CurtainTime, 0f, 1f);
                UiShapes.Fill(new Rectangle(-20, -20 - up * 1000, sw + 40, 1100), 0, StageCurtain);
                string title = run != null ? Text.Of(run.Show.NameKey) : "";
                UiFonts.DrawDisplay(title, sw / 2f - UiFonts.Measure(title, 84, UiWeight.Black) / 2f, 460 - up * 1000, 84, 0, Color.White);
                break;
            }
            case StagePhase.Results when run != null:
            {
                var panel = new Rectangle(sw / 2f - 480, 180, 960, 560);
                Panel(panel, 30, border: 6, frame: Gold);
                UiFonts.Draw(Text.Of("stageShow.stage.results"), panel.X + 40, panel.Y + 30, 44, Ink, UiWeight.Black);
                var places = run.Places();
                for (int k = 0; k < 4; k++)
                {
                    int p = places[k];
                    var row = new Rectangle(panel.X + 36, panel.Y + 110 + k * 100, panel.Width - 72, 84);
                    UiShapes.Fill(row, 20, p == 0 ? Lighter(Gold, 0.4f) : new Color(240, 236, 228, 255));
                    string who = p == 0 ? Text.Of("stageShow.stage.you") : PokemonPlatinumEngine.Data.LocalNames.Species(run.Performers[p].Species);
                    UiFonts.Draw((k + 1) + ".", row.X + 22, row.Y + 18, 40, Ink, UiWeight.Black);
                    UiFonts.Draw(who, row.X + 90, row.Y + 20, 36, Ink, UiWeight.ExtraBold);
                    UiFonts.Draw(Text.Of("stageShow.stage.eyes", ("eyes", run.Eyes[p].ToString())), row.X + 470, row.Y + 24, 28, Muted, UiWeight.Black);
                    UiFonts.Draw(run.ScoreOf(p).ToString(), row.X + row.Width - 100, row.Y + 18, 40, Ink, UiWeight.Black);
                }
                Hints(sw - Margin, 1040, Hint("shared.keys.z", "stageShow.hint.bow"));
                break;
            }
        }
    }

    private static void StageList(int sw, string title, System.Collections.Generic.IReadOnlyList<string> rows, int cursor, System.Collections.Generic.IReadOnlyList<Color>? colours)
    {
        var panel = new Rectangle(sw / 2f - 360, 220, 720, 110 + rows.Count * 86);
        Panel(panel, 28, border: 6, frame: Gold);
        UiFonts.Draw(title, panel.X + 36, panel.Y + 28, 34, Ink, UiWeight.Black);
        for (int i = 0; i < rows.Count; i++)
        {
            var row = new Rectangle(panel.X + 28, panel.Y + 84 + i * 86, panel.Width - 56, 72);
            UiShapes.Fill(row, 20, i == cursor ? Lighter(Gold, 0.4f) : new Color(240, 236, 228, 255));
            if (colours != null) UiShapes.Circle(new Vector2(row.X + 34, row.Y + 36), 16, colours[i]);
            UiFonts.Draw(rows[i], row.X + (colours != null ? 66 : 24), row.Y + 18, 34, Ink, UiWeight.ExtraBold);
        }
        Hints(sw - Margin, 1040, Hint("shared.keys.z", "stageShow.hint.choose"), Hint("shared.keys.esc", "stageShow.hint.back"));
    }
}
