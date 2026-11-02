using System;
using System.Numerics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Contest;
using PokemonPlatinumEngine.UI.Kit;
using Raylib_cs;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The contest's stage as a backdrop (plan 21 · H12, part o; style guide, "Hoenn's contests", "The stage behind"): the hall
/// a Hoenn contest is held in, laid out on <see cref="ContestHallStage"/>'s grid of fifteen tiles by eleven and drawn
/// behind the contest's panels in the quality's colour. Curtains at the back, a desk where the judge sits with the host
/// beside it, four marks on the boards where the contestants stand, rows of plain rounds for the audience and an easel
/// where the painter waits. Our own shapes throughout; nobody on it is drawn as a person.
/// </summary>
internal static partial class ModernUi
{
    /// <summary>The stage behind a contest of <paramref name="quality"/>, filling the screen.</summary>
    public static void ContestHallBackdrop(int sw, int sh, ContestStat quality)
    {
        var tint = QualityColor(quality);
        Raylib.DrawRectangleGradientV(0, 0, sw, sh, Darker(tint, 0.62f), new Color(16, 14, 30, 255));
        float tile = MathF.Min(sw / (float)ContestHallStage.Width, sh / (float)ContestHallStage.Height);
        var origin = new Vector2((sw - tile * ContestHallStage.Width) / 2f, (sh - tile * ContestHallStage.Height) / 2f);
        Vector2 At(float x, float y) => origin + new Vector2((x + 0.5f) * tile, (y + 0.5f) * tile);

        // The curtains along the back wall, in folds of the quality's colour, and their pelmet
        var back = Darker(tint, 0.42f);
        for (int fold = 0; fold < ContestHallStage.Width * 2; fold++)
        {
            float x = origin.X + fold * tile / 2f;
            UiShapes.Shape(new Rectangle(x, origin.Y, tile / 2f, tile * 3.2f), 0, (fold & 1) == 0 ? back : Darker(back, 0.18f), Darker(back, 0.35f));
        }
        UiShapes.Fill(new Rectangle(origin.X, origin.Y, tile * ContestHallStage.Width, tile * 0.45f), 0, Darker(tint, 0.25f));

        // The boards: a raised stage across the hall's middle, with its edge lit
        var boards = new Rectangle(origin.X + tile, origin.Y + tile * 3.2f, tile * (ContestHallStage.Width - 2), tile * 5.6f);
        UiShapes.Shape(boards, tile * 0.3f, new Color(120, 86, 56, 255), new Color(88, 60, 40, 255));
        UiShapes.Line(new Vector2(boards.X + tile * 0.3f, boards.Y + boards.Height), new Vector2(boards.X + boards.Width - tile * 0.3f, boards.Y + boards.Height),
            tile * 0.08f, Lighter(tint, 0.3f));

        foreach (var spot in ContestHallStage.Spots)
        {
            var c = At(spot.X, spot.Y);
            switch (spot.Place)
            {
                case StagePlace.Judge:
                    // The judge's desk, with the lens the judge watches through
                    UiShapes.Fill(new Rectangle(c.X - tile * 0.7f, c.Y - tile * 0.15f, tile * 1.4f, tile * 0.55f), tile * 0.1f, new Color(70, 48, 30, 255));
                    UiShapes.Ring(c - new Vector2(0, tile * 0.4f), tile * 0.22f, tile * 0.06f, Lighter(tint, 0.4f));
                    break;
                case StagePlace.Host:
                    // The host's stand
                    UiShapes.Fill(new Rectangle(c.X - tile * 0.18f, c.Y - tile * 0.1f, tile * 0.36f, tile * 0.5f), tile * 0.08f, new Color(60, 60, 78, 255));
                    UiShapes.Circle(c - new Vector2(0, tile * 0.22f), tile * 0.12f, new Color(200, 200, 214, 255));
                    break;
                case StagePlace.Contestant:
                    // A mark on the boards for each contestant
                    UiShapes.Ring(c + new Vector2(0, tile * 0.2f), tile * 0.36f, tile * 0.07f, new Color(255, 255, 255, 90));
                    break;
                case StagePlace.Audience:
                    // A plain round in a seat
                    UiShapes.Fill(new Rectangle(c.X - tile * 0.32f, c.Y, tile * 0.64f, tile * 0.4f), tile * 0.12f, new Color(40, 36, 60, 200));
                    UiShapes.Circle(c, tile * 0.24f, new Color(90, 84, 120, 200));
                    break;
                case StagePlace.Prize:
                    // The prize's stand, empty until it is handed over
                    UiShapes.Fill(new Rectangle(c.X - tile * 0.2f, c.Y, tile * 0.4f, tile * 0.3f), tile * 0.06f, new Color(200, 170, 90, 200));
                    break;
                case StagePlace.Artist:
                    // The painter's easel
                    UiShapes.Line(c + new Vector2(-tile * 0.25f, tile * 0.45f), c + new Vector2(0, -tile * 0.4f), tile * 0.06f, new Color(150, 110, 70, 230));
                    UiShapes.Line(c + new Vector2(tile * 0.25f, tile * 0.45f), c + new Vector2(0, -tile * 0.4f), tile * 0.06f, new Color(150, 110, 70, 230));
                    UiShapes.Fill(new Rectangle(c.X - tile * 0.3f, c.Y - tile * 0.35f, tile * 0.6f, tile * 0.45f), tile * 0.04f, new Color(240, 234, 220, 230));
                    break;
            }
        }

        // Dimmed, so the contest's panels read over it
        Raylib.DrawRectangle(0, 0, sw, sh, new Color(10, 10, 24, 120));
    }
}
