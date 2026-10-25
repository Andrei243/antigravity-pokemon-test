using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Contest;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// A Hoenn contest winner's painting (plan 21 · H11, part b; style guide, "Hoenn's contests and Pokéblocks"): the
/// Pokémon that won a Master Rank contest of 800 points or more, as the painter of the Contest Lobby made it, hung in
/// a gilt frame of the colour of its contest, with the plaque beneath. Ours: Emerald's paintings are metatiles of the
/// museum's wall, and the player's winner is not in them.
/// </summary>
internal static partial class ModernUi
{
    /// <summary>The painting's frame on the screen, so that tests and the harness can find the canvas.</summary>
    public static Rectangle PaintingFrame(int sw) => new(sw / 2f - 430, ContentTop + 6, 860, 700);

    public static void DrawContestPainting(int sw, int sh, ContestPainting painting, float appear)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("painting.title"));
        Hints(sw - Margin, 44, Hint("shared.keys.z", "shared.hints.close"));
        float rise = (1f - UiMotion.EaseOut(appear)) * 50f;
        var frame = PaintingFrame(sw);
        frame.Y += rise;
        var quality = painting.Category;
        Color tone = QualityColor(quality);
        UiShapes.Shadow(frame, 10, 34, new Vector2(0, 12), new Color(6, 14, 34, 120));
        UiShapes.Fill(frame, 10, new Color(196, 160, 82, 255));
        UiShapes.Fill(new Rectangle(frame.X + 14, frame.Y + 14, frame.Width - 28, frame.Height - 28), 6, new Color(238, 214, 142, 255));
        var canvas = new Rectangle(frame.X + 34, frame.Y + 34, frame.Width - 68, 470);
        // The canvas: the colour of its contest from the top, a floor of the same under the Pokémon
        UiShapes.Fill(canvas, 0, Color.Lerp(tone, new Color(250, 246, 236, 255), 0.72f));
        UiShapes.Fill(new Rectangle(canvas.X, canvas.Y + canvas.Height * 0.68f, canvas.Width, canvas.Height * 0.32f), 0, Color.Lerp(tone, new Color(60, 52, 70, 255), 0.35f) with { A = 150 });

        string model = PokemonLook.Of(painting.Species, false, false);
        var sprite = PixelArtGenerator.GetPokemonSprite(model, false);
        const int scale = 5;
        PixelArt(sprite, new Vector2(canvas.X + canvas.Width / 2f, canvas.Y + canvas.Height * 0.62f - sprite.Height * scale * 0.28f), scale);
        UiShapes.Shape(canvas, 0, default, default, new Color(90, 70, 40, 255), 4);
        Rosette(new Vector2(canvas.X + canvas.Width - 70, canvas.Y + 70), 40, tone);

        // The plaque
        var plaque = new Rectangle(frame.X + 150, frame.Y + frame.Height - 168, frame.Width - 300, 124);
        UiShapes.Fill(plaque, 8, new Color(52, 44, 36, 255));
        string name = painting.Nickname.Length > 0 ? painting.Nickname : LocalNames.Species(painting.Species);
        UiFonts.DrawCentered(name, plaque.X + (plaque.Width - UiFonts.Measure(name, 44, UiWeight.Black)) / 2f, plaque.Y + 34, 44, new Color(244, 230, 190, 255), UiWeight.Black);
        string contest = Text.Of("painting.contest", ("category", Text.Of($"contest.category.{quality}")), ("rank", Text.Of($"appeal.rank.{painting.Rank}")));
        UiFonts.DrawCentered(contest, plaque.X + (plaque.Width - UiFonts.Measure(contest, 26, UiWeight.ExtraBold)) / 2f, plaque.Y + 78, 26, new Color(214, 198, 158, 255), UiWeight.ExtraBold);
        string by = Text.Of("painting.by", ("trainer", PlayerIdentity.Fill(painting.Trainer)), ("points", painting.Points.ToString()));
        UiFonts.DrawCentered(by, plaque.X + (plaque.Width - UiFonts.Measure(by, 24, UiWeight.ExtraBold)) / 2f, plaque.Y + 104, 24, new Color(214, 198, 158, 255), UiWeight.ExtraBold);
    }
}
