using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The Pokéathlon's second batch of events on screen (plan 15 · E7; style guide, "The Pokéathlon"): Pennant Capture,
/// Snow Throw, Disc Catch and Lamp Jump, drawn flat in layout units as the first three are.
/// </summary>
internal static partial class ModernUi
{
    private static readonly Color AthlonRim = new(30, 40, 70, 255);
    private static readonly Color AthlonLawn = new(96, 168, 84, 255);
    private static readonly Color AthlonSnow = new(226, 236, 246, 255);
    private static readonly Color AthlonNight = new(30, 40, 70, 255);

    private static void DrawAthlonBatch2(int sw, AthlonEvent ev)
    {
        switch (ev)
        {
            case PennantCapture p: DrawPennantCapture(sw, p); break;
            case SnowThrow s: DrawSnowThrow(sw, s); break;
            case DiscCatch d: DrawDiscCatch(sw, d); break;
            case LampJump l: DrawLampJump(sw, l); break;
        }
    }

    /// <summary>Three gold sparks circling a dazed Pokémon.</summary>
    private static void DazedSparks(Vector2 c, float radius, int frame)
    {
        for (int k = 0; k < 3; k++)
        {
            float a = frame * 0.25f + k * MathF.Tau / 3f;
            UiShapes.Circle(c + new Vector2(MathF.Cos(a) * radius, -radius * 0.9f + MathF.Sin(a) * radius * 0.3f), 7, Gold);
        }
    }

    private static void BreathBar(Rectangle track, float left, Color fill)
    {
        UiShapes.Fill(track, track.Height / 2f, Rule);
        if (left > 0f) UiShapes.Fill(new Rectangle(track.X, track.Y, track.Width * Math.Clamp(left, 0f, 1f), track.Height), track.Height / 2f, fill);
    }

    // ------------------------------------------------------------------ Pennant Capture

    private static void DrawPennantCapture(int sw, PennantCapture ev)
    {
        const float scale = 5.6f;
        float w = PennantCapture.Width * scale, h = PennantCapture.Height * scale;
        var lawn = new Rectangle(sw / 2f - w / 2f, (AthlonStage + AthlonStageBottom) / 2f - h / 2f, w, h);
        UiShapes.Fill(new Rectangle(lawn.X - 14, lawn.Y - 14, lawn.Width + 28, lawn.Height + 28), 30, AthlonRim);
        UiShapes.Fill(lawn, 20, AthlonLawn);
        UiShapes.Fill(new Rectangle(lawn.X + lawn.Width / 2f - 3, lawn.Y + 10, 6, lawn.Height - 20), 3, new Color(255, 255, 255, 170));
        UiShapes.Ring(new Vector2(lawn.X + lawn.Width / 2f, lawn.Y + lawn.Height / 2f), 90, 6, new Color(255, 255, 255, 170));
        Vector2 At(Vector2 p) => new(lawn.X + p.X * scale, lawn.Y + p.Y * scale);

        foreach (var p in ev.Pennants)
        {
            var foot = At(p);
            UiShapes.Glow(foot + new Vector2(0, 4), 18, 6, new Color(0, 0, 0, 70));
            UiShapes.Fill(new Rectangle(foot.X - 3, foot.Y - 52, 6, 54), 3, Color.White);
            UiShapes.Triangle(new Vector2(foot.X + 3, foot.Y - 52), new Vector2(foot.X + 40, foot.Y - 40), new Vector2(foot.X + 3, foot.Y - 28), Gold);
        }

        for (int t = 0; t < AthlonRun.TeamCount; t++)
        {
            var c = ev.Chasers[t];
            var at = At(c.At);
            float body = PennantCapture.Body * scale;
            float alpha = c.Switching > 0 ? 0.4f : 1f;
            UiShapes.Circle(at, body, AthlonTeamColors[t] with { A = (byte)(255 * alpha) });
            UiShapes.Circle(at, body - 7, new Color(255, 255, 255, (int)(255 * alpha)));
            AthleteIcon(ev.ActiveOf(t).Species, at, alpha * (c.Dazed > 0 ? 0.7f : 1f));
            if (c.Dazed > 0) DazedSparks(at, body, ev.Frames);
        }

        // Each team's panel at its own corner, outside the lawn
        for (int t = 0; t < AthlonRun.TeamCount; t++)
        {
            var c = ev.Chasers[t];
            bool right = t is 2 or 3, low = t is 0 or 3;
            var box = new Rectangle(right ? sw - Margin - 200 : Margin, low ? AthlonStageBottom - 190 : AthlonStage, 200, 190);
            Panel(box, 24, frame: t == 0 ? AthlonTeamColors[0] : null);
            TeamChip(new Rectangle(box.X + 14, box.Y + 14, box.Width - 28, 38), ev.Teams[t], t);
            TextMiddle(c.Flags.ToString(), box.X + box.Width / 2f, box.Y + 98, 56, Ink, UiWeight.Black);
            float left = 1f - (float)c.Fatigue[c.Active] / PennantCapture.Tired;
            BreathBar(new Rectangle(box.X + 22, box.Y + 148, box.Width - 44, 18), left, ev.IsTired(t) ? Red : AthlonTeamColors[t]);
        }
    }

    // ------------------------------------------------------------------ Snow Throw

    private static void DrawSnowThrow(int sw, SnowThrow ev)
    {
        const float scale = 5f;
        var middle = new Vector2(sw / 2f, (AthlonStage + AthlonStageBottom) / 2f);
        float half = (SnowThrow.Side + 8f) * scale;
        var square = new Rectangle(middle.X - half, middle.Y - half, 2 * half, 2 * half);
        UiShapes.Fill(new Rectangle(square.X - 14, square.Y - 14, square.Width + 28, square.Height + 28), 34, AthlonRim);
        UiShapes.Fill(square, 24, AthlonSnow);
        UiShapes.Ring(middle, half * 0.5f, 6, Lighter(AthlonSky, 0.55f));
        Vector2 At(Vector2 p) => middle + p * scale;

        // The balls' shadows on the snow, then the throwers, then the balls over them
        foreach (var b in ev.Balls)
        {
            var ground = Vector2.Lerp(At(SnowThrow.PlaceOf(b.From)), At(SnowThrow.PlaceOf(b.To)), b.Progress);
            UiShapes.Glow(ground, 18, 7, new Color(0, 0, 0, 60));
        }

        for (int t = 0; t < AthlonRun.TeamCount; t++)
        {
            var th = ev.Throwers[t];
            var at = At(SnowThrow.PlaceOf(t));
            if (th.Target >= 0)
            {
                var way = Vector2.Normalize(SnowThrow.PlaceOf(th.Target) - SnowThrow.PlaceOf(t));
                var tip = at + way * 110f;
                var side = new Vector2(-way.Y, way.X) * 18f;
                UiShapes.Triangle(tip, tip - way * 30f + side, tip - way * 30f - side, AthlonTeamColors[t]);
            }
            float alpha = th.Switching > 0 ? 0.4f : th.Dazed > 0 ? 0.6f : 1f;
            UiShapes.Circle(at, 62, AthlonTeamColors[t] with { A = (byte)(255 * alpha) });
            UiShapes.Circle(at, 54, new Color(255, 255, 255, (int)(255 * alpha)));
            if (th.Ducking) AthleteIcon(ev.ActiveOf(t).Species, at + new Vector2(0, 18), alpha, 1.4f);
            else AthleteIcon(ev.ActiveOf(t).Species, at, alpha);
            if (th.Dazed > 0) DazedSparks(at, 62, ev.Frames);
            float made = 1f - (float)th.Reload / SnowThrow.ReloadFrames(ev.ActiveOf(t).Stats);
            BreathBar(new Rectangle(at.X - 50, at.Y + 72, 100, 12), made, AthlonTeamColors[t]);
        }

        foreach (var b in ev.Balls)
        {
            var ground = Vector2.Lerp(At(SnowThrow.PlaceOf(b.From)), At(SnowThrow.PlaceOf(b.To)), b.Progress);
            var ball = ground - new Vector2(0, MathF.Sin(MathF.PI * b.Progress) * 80f);
            UiShapes.Circle(ball, 15, new Color(170, 200, 232, 255));
            UiShapes.Circle(ball, 12, Color.White);
        }

        for (int t = 0; t < AthlonRun.TeamCount; t++)
        {
            var th = ev.Throwers[t];
            bool rightSide = t is 0 or 3, low = t is 0 or 1;
            var box = new Rectangle(rightSide ? sw - Margin - 380 : Margin, low ? AthlonStageBottom - 200 : AthlonStage, 380, 200);
            Panel(box, 26, frame: t == 0 ? AthlonTeamColors[0] : null);
            TeamChip(new Rectangle(box.X + 18, box.Y + 16, box.Width - 36, 40), ev.Teams[t], t);
            UiFonts.Draw(Text.Of("athlon.batch2.hits", ("n", th.Hits)), box.X + 28, box.Y + 76, 30, Ink, UiWeight.Black);
            UiFonts.Draw(Text.Of("athlon.batch2.ducked", ("n", th.Ducked)), box.X + 28, box.Y + 120, 26, Muted, UiWeight.Black);
        }
    }

    // ------------------------------------------------------------------ Disc Catch

    private static void DrawDiscCatch(int sw, DiscCatch ev)
    {
        float width = (sw - Margin * 2 - 3 * Gutter) / AthlonRun.TeamCount;
        for (int t = 0; t < AthlonRun.TeamCount; t++)
        {
            var c = ev.Catchers[t];
            var col = new Rectangle(Margin + t * (width + Gutter), AthlonStage, width, AthlonStageBottom - AthlonStage);
            UiShapes.Fill(col, 28, Lighter(AthlonSky, 0.72f));
            UiShapes.Fill(new Rectangle(col.X, col.Y + col.Height - 70, col.Width, 70), 28, AthlonLawn);
            TeamChip(new Rectangle(col.X + 20, col.Y + 18, col.Width - 40, 44), ev.Teams[t], t);
            TextMiddle(c.Caught.ToString(), col.X + col.Width / 2f, col.Y + 110, 56, Ink, UiWeight.Black);
            TextMiddle(Text.Of("athlon.batch2.catches"), col.X + col.Width / 2f, col.Y + 152, 22, Muted, UiWeight.Black);

            float inner = col.Width - 40, ground = col.Y + col.Height - 70, sx = inner / DiscCatch.LaneWidth;
            const float sy = 4f;
            float X(float x) => col.X + 20 + x * sx;

            // The discs in the air, their shadows first
            for (int d = c.Next; d < ev.Discs.Count; d++)
            {
                var disc = ev.Discs[d];
                if (disc.ThrownAt > ev.Frames) break;
                if (!ev.Open(t, d) || !disc.InTheAir(ev.Frames)) continue;
                float x = X(disc.XAt(ev.Frames)), y = ground - disc.HeightAt(ev.Frames) * sy;
                UiShapes.Glow(new Vector2(x, ground + 6), 26, 8, new Color(0, 0, 0, 70));
                UiShapes.Turned(new Vector2(x, y), 24, 10, 10, 0f, Color.White);
                UiShapes.Turned(new Vector2(x, y), 22, 8, 8, 0f, Gold);
            }

            // The catcher, rising with its jump
            var me = new Vector2(X(c.X), ground - 30 - c.Lift * sy);
            UiShapes.Glow(new Vector2(X(c.X), ground + 6), 30, 9, new Color(0, 0, 0, 80));
            if (c.SinceCatch < 8) UiShapes.Glow(me, 70, 70, AthlonTeamColors[t] with { A = 140 });
            AthleteIcon(ev.ActiveOf(t).Species, me, c.Switching > 0 ? 0.4f : 1f);
        }
    }

    // ------------------------------------------------------------------ Lamp Jump

    private static void DrawLampJump(int sw, LampJump ev)
    {
        const float scale = 1.8f;
        float width = (sw - Margin * 2 - 3 * Gutter) / AthlonRun.TeamCount;
        for (int t = 0; t < AthlonRun.TeamCount; t++)
        {
            var j = ev.Jumpers[t];
            var col = new Rectangle(Margin + t * (width + Gutter), AthlonStage, width, AthlonStageBottom - AthlonStage);
            UiShapes.Fill(col, 28, AthlonNight);
            TeamChip(new Rectangle(col.X + 20, col.Y + 18, col.Width - 40, 44), ev.Teams[t], t);
            float ground = col.Y + col.Height - 66;

            for (int i = 0; i < LampJump.Lamps; i++)
            {
                var lamp = new Vector2(col.X + col.Width - 46, ground - (i + 1) * LampJump.LampGap * scale);
                bool lit = i < j.LitNow;
                if (lit) UiShapes.Glow(lamp, 34, 34, Gold with { A = 120 });
                UiShapes.Circle(lamp, 14, lit ? Gold : new Color(78, 84, 100, 255));
            }

            var board = new Rectangle(col.X + 40, ground, col.Width - 140, 16);
            UiShapes.Fill(board, 8, AthlonTeamColors[t]);
            float cx = board.X + board.Width / 2f;
            AthleteIcon(ev.ActiveOf(t).Species, new Vector2(cx, ground - 32 - j.H * scale));
            TextMiddle(Text.Of("athlon.batch2.lamps", ("n", j.Lit)), col.X + col.Width / 2f, ground + 40, 32, Color.White, UiWeight.Black);
        }
    }
}
