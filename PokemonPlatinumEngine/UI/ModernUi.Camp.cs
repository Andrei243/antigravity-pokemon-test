using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The camp on screen (plan 10 · F6; style guide, "Menu screens (G10)", "The camp"): a clearing of our own drawn with
/// the kit's shapes (a sky, a line of trees, a lawn, a tent and a fire), the team standing on it as their menu sprites at
/// 2×, the hearts of a pat and the berry of a meal rising over whoever had them, and a panel at the foot with the one
/// under the cursor.
/// </summary>
internal static partial class ModernUi
{
    private static readonly Color CampSky = new(150, 206, 238, 255);
    private static readonly Color CampSkyLow = new(206, 232, 244, 255);
    private static readonly Color CampTrees = new(60, 128, 92, 255);
    private static readonly Color CampLawn = new(118, 186, 96, 255);
    private static readonly Color CampLawnNear = new(98, 168, 82, 255);
    private static readonly Color CampTent = new(226, 120, 64, 255);
    private static readonly Color CampHeart = new(240, 92, 128, 255);

    /// <summary>The clearing's rectangle, which every place on it is laid out in.</summary>
    public static Rectangle CampClearing => new(64, 132, 1792, 716);

    /// <summary>Where the Pokémon at a place stands on the clearing (its feet): two rows of three, the near row lower and further apart, or one row across the middle.</summary>
    public static Vector2 CampPlace(Rectangle clearing, int index, int count)
    {
        float horizon = clearing.Y + clearing.Height * 0.42f;
        if (count <= CampScreen.Columns)
        {
            float step = clearing.Width / (count + 1f);
            return new Vector2(clearing.X + step * (index + 1), horizon + clearing.Height * 0.38f);
        }
        int row = index / CampScreen.Columns, column = index % CampScreen.Columns;
        int inRow = row == 0 ? CampScreen.Columns : count - CampScreen.Columns;
        float spread = row == 0 ? 0.62f : 0.78f;
        float width = clearing.Width * spread;
        float left = clearing.X + (clearing.Width - width) / 2f;
        float x = left + width * (column + 0.5f) / inRow;
        float y = row == 0 ? horizon + clearing.Height * 0.2f : horizon + clearing.Height * 0.5f;
        return new Vector2(x, y);
    }

    /// <summary>How many of five hearts a Pokémon's friendship fills (one under 70, two to 150, three to 220, four to 255, five at 255).</summary>
    public static int FriendshipHearts(int friendship) =>
        friendship >= 255 ? 5 : friendship >= 220 ? 4 : friendship >= 150 ? 3 : friendship >= 70 ? 2 : 1;

    public static void DrawCamp(int sw, int sh, CampScreen screen, float appear)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("camp.title"));
        Hints(sw - Margin, 44, Hint("shared.keys.arrows", "shared.hints.move"), Hint("shared.keys.z", "camp.hint.pet"),
            Hint("shared.keys.c", "camp.hint.feed"), Hint("shared.keys.esc", "shared.hints.back"));

        float rise = (1f - UiMotion.EaseOut(appear)) * 60f;
        var clearing = CampClearing;
        clearing.Y += rise;
        CampGround(clearing);

        var party = screen.Party;
        int count = screen.Count;
        float t = screen.Age;
        // The far row first, so that the near one stands over it
        for (int i = 0; i < count; i++)
        {
            var p = party!.Members[i];
            var feet = CampPlace(clearing, i, count);
            bool near = count > CampScreen.Columns && i >= CampScreen.Columns || count <= CampScreen.Columns;
            float size = near ? 1f : 0.86f;
            bool selected = i == screen.Cursor;
            // Its shadow, and under the one chosen a Selection ring
            UiShapes.Fill(new Rectangle(feet.X - 92 * size, feet.Y - 16 * size, 184 * size, 32 * size), 16 * size, new Color(30, 70, 40, 70));
            if (selected) UiShapes.Shape(new Rectangle(feet.X - 110, feet.Y - 22, 220, 44), 22, new Color(0, 0, 0, 0), new Color(0, 0, 0, 0), Selection, 5);

            // A pat or a meal hops it; the one chosen breathes a little; an Egg sits still
            float petted = screen.PettedFor(i), fed = screen.FedFor(i);
            float cheer = Math.Max(Cheer(petted), Cheer(fed));
            float hop = p.IsEgg ? 0f : cheer > 0f ? MathF.Abs(MathF.Sin(cheer * MathF.PI * 3f)) * 22f : selected ? (MathF.Sin(t * 4f) + 1f) * 3f : 0f;
            var sprite = PixelArtGenerator.SpriteOf(p);
            var middle = new Vector2(feet.X, feet.Y - sprite.Height + 8);
            PixelArt(sprite, middle, 2, hop);

            // The hearts of a pat, rising and fading; the berry of a meal, the same
            if (petted >= 0f && petted < CampScreen.CheerTime) Rising(feet, sprite.Height * 2, petted, true);
            if (fed >= 0f && fed < CampScreen.CheerTime) Rising(feet, sprite.Height * 2, fed, false);
        }

        // ---- The one under the cursor, and what the last thing done came to
        var box = new Rectangle(64, 872 + rise, 1792, 160);
        Panel(box, 30);
        if (screen.Selected is { } chosen)
        {
            float x = box.X + 44;
            NameWithGender(chosen, x, box.Y + 46, 40);
            if (!chosen.IsEgg)
            {
                Label(Text.Of("camp.friendship"), x, box.Y + 84);
                int hearts = FriendshipHearts(chosen.Friendship);
                for (int h = 0; h < 5; h++) Heart(new Vector2(x + 16 + h * 40, box.Y + 128), 14, h < hearts ? CampHeart : Rule);
                float tx = x + 236;
                tx += Tag(tx, box.Y + 112, Text.Of("camp.petted"), screen.Visit.WasPetted(chosen) ? Green : Rule, 32) + 10;
                Tag(tx, box.Y + 112, Text.Of("camp.fed"), screen.Visit.WasFed(chosen) ? Green : Rule, 32);
            }
        }
        DrawWrapped(screen.Note, box.X + 760, box.Y + 54, box.Width - 760 - 44, 30, Ink, 42, UiWeight.ExtraBold);
    }

    // A cheer's progress, 0 to 1, or 0 when none plays
    private static float Cheer(float age) => age >= 0f && age < CampScreen.CheerTime ? age / CampScreen.CheerTime : 0f;

    // Three hearts (a pat) or a berry (a meal) rising from over the head and fading as they go
    private static void Rising(Vector2 feet, float height, float age, bool hearts)
    {
        float k = age / CampScreen.CheerTime;
        int alpha = (int)(255 * Math.Clamp(1.4f - k * 1.4f, 0f, 1f));
        float y = feet.Y - height - 10 - k * 70;
        if (hearts)
        {
            for (int h = 0; h < 3; h++)
            {
                float lag = h * 0.12f;
                float dy = Math.Max(0f, k - lag) * 40f;
                Heart(new Vector2(feet.X + (h - 1) * 40, y - dy + (h == 1 ? -16 : 0)), 13, CampHeart with { A = (byte)alpha });
            }
        }
        else
        {
            var c = new Vector2(feet.X, y);
            UiShapes.Circle(c, 18, new Color(196, 52, 86, alpha));
            UiShapes.Circle(c + new Vector2(-5, -6), 5, new Color(255, 255, 255, (int)(alpha * 0.7f)));
            UiShapes.Line(c + new Vector2(0, -18), c + new Vector2(6, -30), 4, new Color(76, 140, 70, alpha));
        }
    }

    // The clearing: a sky, a line of trees on the horizon, the lawn in two greens, a tent at the left and a fire at the right
    private static void CampGround(Rectangle r)
    {
        float horizon = r.Y + r.Height * 0.42f;
        UiShapes.Shape(r, 34, CampSky, CampLawnNear);
        UiShapes.Shape(new Rectangle(r.X, r.Y, r.Width, horizon - r.Y), 34, CampSky, CampSkyLow);
        // The trees: a row of round crowns along the horizon
        for (int i = 0; i < 16; i++)
        {
            float x = r.X + 40 + i * (r.Width - 80) / 15f;
            float s = 46 + (i * 37 % 5) * 6;
            UiShapes.Circle(new Vector2(x, horizon - s * 0.4f), s, CampTrees);
        }
        UiShapes.Shape(new Rectangle(r.X, horizon, r.Width, r.Y + r.Height - horizon), 34, CampLawn, CampLawnNear);
        // The tent, at the back on the left
        var foot = new Vector2(r.X + 230, horizon + 70);
        UiShapes.Triangle(foot + new Vector2(0, -150), foot + new Vector2(-130, 0), foot + new Vector2(130, 0), CampTent, 4);
        UiShapes.Triangle(foot + new Vector2(0, -150), foot + new Vector2(30, 0), foot + new Vector2(130, 0), Darker(CampTent, 0.2f));
        UiShapes.Triangle(foot + new Vector2(0, -70), foot + new Vector2(-34, 0), foot + new Vector2(34, 0), new Color(80, 44, 30, 255));
        // The fire, at the back on the right: stones round three flames
        var fire = new Vector2(r.X + r.Width - 230, horizon + 70);
        for (int s = 0; s < 6; s++)
            UiShapes.Circle(fire + new Vector2(-60 + s * 24, 8 + (s % 2) * 4), 14, new Color(140, 140, 150, 255));
        UiShapes.Triangle(fire + new Vector2(0, -70), fire + new Vector2(-34, 0), fire + new Vector2(34, 0), new Color(242, 120, 40, 255));
        UiShapes.Triangle(fire + new Vector2(0, -42), fire + new Vector2(-18, 0), fire + new Vector2(18, 0), new Color(252, 214, 80, 255));
    }
}
