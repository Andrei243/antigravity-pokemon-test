using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The loop catch's screen (plan 44 · SD10; style guide, "The loop catch"): flat, drawn with the kit.</summary>
internal static partial class ModernUi
{
    private static readonly Color LoopGold = new(250, 196, 60, 255);

    public static void DrawLoop(int sw, int sh, LoopScreen screen)
    {
        if (screen.Game is not { } game) return;
        LoopGround(sw, sh);
        string species = LocalNames.Species(game.Task.Species);

        // The strike's warning lies under the Pokémon
        if (screen.Phase == LoopPhase.Playing && game.WindingUp)
        {
            UiShapes.Circle(game.At, game.Task.Reach, Red with { A = 36 });
            UiShapes.Ring(game.At, Math.Max(8f, game.Task.Reach * game.WindUpShare), 6, Red);
        }
        LoopPokemon(game);
        if (screen.SinceStrike < 0.25f)
            UiShapes.Ring(screen.StruckAt, game.Task.Reach, 8, Color.White with { A = Fade(screen.SinceStrike, 0.25f) });
        if (screen.SinceBreak < 0.35f)
            Stroke(screen.Broken, Red with { A = Fade(screen.SinceBreak, 0.35f) }, 10f);

        // The line and the disc
        if (game.Line.Count > 1)
        {
            Stroke(game.Line, LoopGold with { A = 64 }, 22f);
            Stroke(game.Line, LoopGold, 10f);
        }
        if (screen.Phase == LoopPhase.Playing)
        {
            if (game.Drawing)
            {
                UiShapes.Circle(game.Disc, 20, LoopGold);
                UiShapes.Ring(game.Disc, 20, 5, Color.White);
            }
            else UiShapes.Ring(game.Disc, 20, 7, Color.White);
        }
        if (screen.SinceLoop < 0.4f)
        {
            float t = screen.SinceLoop / 0.4f;
            UiShapes.Ring(game.At, LoopCatch.BodyRadius + (220 - LoopCatch.BodyRadius) * t, 10, Color.White with { A = Fade(screen.SinceLoop, 0.4f) });
        }
        if (screen.Phase == LoopPhase.Playing && game.WindingUp)
            UiFonts.Draw("!", game.At.X - UiFonts.Measure("!", 56, UiWeight.Black) / 2f, game.At.Y - 270, 56, Color.White, UiWeight.Black);

        // The cards
        var who = new Rectangle(32, 28, 400, 100);
        Panel(who, 24);
        UiFonts.Draw(species, who.X + 28, who.Y + 14, 34, Ink, UiWeight.Black);
        Label(Text.Of("loopCatch.calm"), who.X + 28, who.Y + 64);
        for (int i = 0; i < LoopCatch.CalmNeeded; i++)
            UiShapes.Circle(new Vector2(who.X + 220 + i * 48, who.Y + 74), 15, i < game.Calm ? Green : Rule);
        var disc = new Rectangle(sw - 32 - 300, 104, 300, 72);
        Panel(disc, 24);
        Label(Text.Of("loopCatch.disc"), disc.X + 28, disc.Y + 24);
        for (int i = 0; i < LoopCatch.Charges; i++)
            UiShapes.Circle(new Vector2(disc.X + 128 + i * 42, disc.Y + 36), 14, i < game.Strength ? LoopGold : Rule);

        switch (screen.Phase)
        {
            case LoopPhase.Brief:
            {
                Dim(sw, sh, 120);
                var r = new Rectangle(sw / 2f - 550, sh / 2f - 235, 1100, 470);
                Panel(r, 36);
                UiFonts.Draw(Text.Of("loopCatch.title"), r.X + 56, r.Y + 44, 44, Ink, UiWeight.Black);
                var middle = new Vector2(r.X + 130, r.Y + 170);
                UiShapes.Circle(middle, 72, new Color(226, 234, 246, 255));
                var icon = PixelArtGenerator.GetPokemonIcon(game.Task.Species);
                Raylib.DrawTexturePro(icon, new Rectangle(0, 0, icon.Width, icon.Height),
                    new Rectangle(middle.X - icon.Width, middle.Y - icon.Height, icon.Width * 2, icon.Height * 2), Vector2.Zero, 0, Color.White);
                UiFonts.Draw(species, r.X + 240, r.Y + 148, 40, Ink, UiWeight.Black);
                float y = r.Y + 280;
                foreach (var key in new[] { "loopCatch.how.draw", "loopCatch.how.loops", "loopCatch.how.strike" })
                {
                    UiFonts.Draw(Text.Of(key), r.X + 56, y, 30, Muted, UiWeight.Bold);
                    y += 46;
                }
                Hints(sw - 32, 32, Hint("shared.keys.z", "loopCatch.start"), Hint("shared.keys.x", "shared.hints.back"));
                break;
            }
            case LoopPhase.Playing:
                Hints(sw - 32, 32, Hint("shared.keys.z", "loopCatch.draw"), Hint("shared.keys.x", "loopCatch.giveUp"));
                break;
            case LoopPhase.Over:
            {
                Dim(sw, sh, 120);
                var r = new Rectangle(sw / 2f - 450, sh / 2f - 130, 900, 260);
                Panel(r, 36);
                string title = Text.Of(screen.Calmed ? "loopCatch.calmed" : "loopCatch.slipped");
                UiFonts.Draw(title, sw / 2f - UiFonts.Measure(title, 56, UiWeight.Black) / 2f, r.Y + 56, 56, screen.Calmed ? Green : Muted, UiWeight.Black);
                string line = Text.Of(screen.Calmed ? "loopCatch.calmedLine" : "loopCatch.slippedLine", ("pokemon", species));
                UiFonts.Draw(line, sw / 2f - UiFonts.Measure(line, 30, UiWeight.Bold) / 2f, r.Y + 158, 30, Ink, UiWeight.Bold);
                if (screen.PhaseTime >= LoopScreen.EndHold) Hints(sw - 32, 32, Hint("shared.keys.z", "loopCatch.goOn"));
                break;
            }
        }
    }

    private static byte Fade(float age, float length) => (byte)(255 * Math.Clamp(1f - age / length, 0f, 1f));

    /// <summary>The meadow from straight above: a gradient and tufts at places fixed by the screen's own hash.</summary>
    private static void LoopGround(int sw, int sh)
    {
        var (x, y, w, h) = Canvas2D.Bled(sw, sh);
        Raylib.DrawRectangleGradientV(x, y, w, h, new Color(96, 160, 92, 255), new Color(62, 122, 76, 255));
        for (int i = 0; i < 220; i++)
        {
            uint n = SplitMix.Hash(i, 0x4C4F4F50u);
            var at = new Vector2(x + (int)(n % (uint)w), y + (int)((n >> 12) % (uint)h));
            var color = (n & 1) == 0 ? new Color(132, 190, 112, 150) : new Color(46, 100, 62, 130);
            UiShapes.Line(at, at + new Vector2(-8, -12), 4, color);
            UiShapes.Line(at, at + new Vector2(0, -16), 4, color);
            UiShapes.Line(at, at + new Vector2(8, -12), 4, color);
        }
    }

    private static void LoopPokemon(LoopCatch game)
    {
        float time = game.Ticks * LoopCatch.Tick;
        bool airborne = game.Gait is LoopGait.Soar or LoopGait.Drift;
        bool moving = game.Velocity.LengthSquared() > 100f;
        float lift = airborne ? 24f + MathF.Sin(time * 3f) * 6f
            : moving ? MathF.Abs(MathF.Sin(time * 9f)) * (game.Gait == LoopGait.Dart ? 14f : 6f) : 0f;
        UiShapes.Glow(game.At + new Vector2(0, 58), 96, 26, new Color(14, 36, 24, 130));
        UiShapes.Glow(game.At + new Vector2(0, 58), 60, 16, new Color(14, 36, 24, 110));
        var sprite = PixelArtGenerator.GetPokemonSprite(game.Task.Species, false);
        // (a front sprite looks to the left, so one going right is mirrored)
        float source = game.Velocity.X > 1f ? -sprite.Width : sprite.Width;
        Raylib.DrawTexturePro(sprite, new Rectangle(0, 0, source, sprite.Height),
            new Rectangle(MathF.Round(game.At.X - 128), MathF.Round(game.At.Y - 192 - lift), 256, 256), Vector2.Zero, 0, Color.White);
    }

    /// <summary>The line's stretches, its oldest fifth fading out.</summary>
    private static void Stroke(IReadOnlyList<Vector2> points, Color color, float width)
    {
        int count = points.Count - 1;
        for (int i = 0; i < count; i++)
        {
            float share = Math.Clamp((i + 1f) / Math.Max(1f, count * 0.2f), 0f, 1f);
            UiShapes.Line(points[i], points[i + 1], width, color with { A = (byte)(color.A * share) });
        }
    }
}
