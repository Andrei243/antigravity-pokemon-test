using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.UI.Kit;

public enum UiIcon { Dex, Ball, Bag, Trainer, Save, Options, Close, Quit, Pouch, Cross, Disc, Berry, Letter, Boost, Key, Pin, Size, Hat, Shirt, Trousers, Shoe }

/// <summary>
/// Small vector icons built from <see cref="UiShapes"/>: the start menu's entries, gender marks and arrows.
/// They are our own simple signs, drawn in one colour on a coloured chip.
/// </summary>
internal static class UiIcons
{
    public static readonly Color Male = new(56, 120, 240, 255);
    public static readonly Color Female = new(240, 88, 136, 255);

    /// <summary>Draws an icon about 2 × <paramref name="s"/> across. <paramref name="back"/> is the colour behind it, used for cut-outs.</summary>
    public static void Draw(UiIcon icon, Vector2 c, float s, Color ink, Color back)
    {
        switch (icon)
        {
            case UiIcon.Dex:
                UiShapes.Fill(new Rectangle(c.X - 0.72f * s, c.Y - 0.9f * s, 1.44f * s, 1.8f * s), 0.24f * s, ink);
                UiShapes.Circle(c + new Vector2(-0.2f, -0.36f) * s, 0.3f * s, back);
                UiShapes.Circle(c + new Vector2(-0.2f, -0.36f) * s, 0.15f * s, ink);
                UiShapes.Line(c + new Vector2(-0.4f, 0.26f) * s, c + new Vector2(0.4f, 0.26f) * s, 0.16f * s, back);
                UiShapes.Line(c + new Vector2(-0.4f, 0.56f) * s, c + new Vector2(0.1f, 0.56f) * s, 0.16f * s, back);
                break;
            case UiIcon.Ball:
                UiShapes.Ring(c, 0.92f * s, 0.2f * s, ink);
                UiShapes.Line(c + new Vector2(-0.78f, 0) * s, c + new Vector2(0.78f, 0) * s, 0.2f * s, ink);
                UiShapes.Circle(c, 0.38f * s, back);
                UiShapes.Circle(c, 0.25f * s, ink);
                break;
            case UiIcon.Bag:
                UiShapes.Ring(c + new Vector2(0, -0.38f) * s, 0.46f * s, 0.18f * s, ink);
                UiShapes.Fill(new Rectangle(c.X - 0.84f * s, c.Y - 0.36f * s, 1.68f * s, 1.26f * s), 0.34f * s, ink);
                UiShapes.Line(c + new Vector2(-0.5f, 0.02f) * s, c + new Vector2(0.5f, 0.02f) * s, 0.14f * s, back);
                UiShapes.Circle(c + new Vector2(0, 0.3f) * s, 0.15f * s, back);
                break;
            case UiIcon.Trainer:
                UiShapes.Fill(new Rectangle(c.X - 0.95f * s, c.Y - 0.66f * s, 1.9f * s, 1.32f * s), 0.22f * s, ink);
                UiShapes.Circle(c + new Vector2(-0.45f, -0.14f) * s, 0.22f * s, back);
                UiShapes.Fill(new Rectangle(c.X - 0.75f * s, c.Y + 0.16f * s, 0.6f * s, 0.26f * s), 0.13f * s, back);
                UiShapes.Line(c + new Vector2(0.12f, -0.2f) * s, c + new Vector2(0.62f, -0.2f) * s, 0.14f * s, back);
                UiShapes.Line(c + new Vector2(0.12f, 0.14f) * s, c + new Vector2(0.46f, 0.14f) * s, 0.14f * s, back);
                break;
            case UiIcon.Save:
                UiShapes.Fill(new Rectangle(c.X - 0.72f * s, c.Y - 0.9f * s, 1.44f * s, 1.8f * s), 0.2f * s, ink);
                for (int i = 0; i < 3; i++)
                    UiShapes.Line(c + new Vector2(-0.38f, -0.42f + i * 0.42f) * s, c + new Vector2(i == 2 ? 0.08f : 0.38f, -0.42f + i * 0.42f) * s, 0.15f * s, back);
                break;
            case UiIcon.Options:
                for (int i = 0; i < 2; i++)
                {
                    float y = (i == 0 ? -0.42f : 0.42f) * s, knob = (i == 0 ? 0.34f : -0.34f) * s;
                    UiShapes.Line(c + new Vector2(-0.82f * s, y), c + new Vector2(0.82f * s, y), 0.2f * s, ink);
                    UiShapes.Circle(c + new Vector2(knob, y), 0.38f * s, back);
                    UiShapes.Circle(c + new Vector2(knob, y), 0.26f * s, ink);
                }
                break;
            case UiIcon.Close:
                UiShapes.Line(c + new Vector2(-0.6f, -0.6f) * s, c + new Vector2(0.6f, 0.6f) * s, 0.28f * s, ink);
                UiShapes.Line(c + new Vector2(-0.6f, 0.6f) * s, c + new Vector2(0.6f, -0.6f) * s, 0.28f * s, ink);
                break;

            // The bag's pockets
            case UiIcon.Pouch:
                UiShapes.Circle(c + new Vector2(0, 0.2f) * s, 0.72f * s, ink);
                UiShapes.Fill(new Rectangle(c.X - 0.34f * s, c.Y - 0.94f * s, 0.68f * s, 0.6f * s), 0.14f * s, ink);
                UiShapes.Line(c + new Vector2(-0.34f, -0.42f) * s, c + new Vector2(0.34f, -0.42f) * s, 0.13f * s, back);
                break;
            case UiIcon.Cross:
                UiShapes.Fill(new Rectangle(c.X - 0.3f * s, c.Y - 0.86f * s, 0.6f * s, 1.72f * s), 0.14f * s, ink);
                UiShapes.Fill(new Rectangle(c.X - 0.86f * s, c.Y - 0.3f * s, 1.72f * s, 0.6f * s), 0.14f * s, ink);
                break;
            case UiIcon.Disc:
                UiShapes.Circle(c, 0.92f * s, ink);
                UiShapes.Circle(c, 0.36f * s, back);
                UiShapes.Circle(c, 0.14f * s, ink);
                break;
            case UiIcon.Berry:
                UiShapes.Circle(c + new Vector2(-0.04f, 0.22f) * s, 0.68f * s, ink);
                UiShapes.Line(c + new Vector2(0.02f, -0.42f) * s, c + new Vector2(0.46f, -0.84f) * s, 0.3f * s, ink);
                UiShapes.Circle(c + new Vector2(-0.26f, 0.04f) * s, 0.14f * s, back);
                break;
            case UiIcon.Letter:
                UiShapes.Fill(new Rectangle(c.X - 0.92f * s, c.Y - 0.64f * s, 1.84f * s, 1.28f * s), 0.18f * s, ink);
                UiShapes.Line(c + new Vector2(-0.68f, -0.4f) * s, c + new Vector2(0, 0.12f) * s, 0.14f * s, back);
                UiShapes.Line(c + new Vector2(0.68f, -0.4f) * s, c + new Vector2(0, 0.12f) * s, 0.14f * s, back);
                break;
            case UiIcon.Boost:
                UiShapes.Triangle(c + new Vector2(0, -0.86f) * s, c + new Vector2(-0.8f, 0.06f) * s, c + new Vector2(0.8f, 0.06f) * s, ink, 0.08f * s);
                UiShapes.Fill(new Rectangle(c.X - 0.3f * s, c.Y - 0.06f * s, 0.6f * s, 0.94f * s), 0.12f * s, ink);
                break;
            case UiIcon.Key:
                UiShapes.Ring(c + new Vector2(-0.44f, 0) * s, 0.5f * s, 0.22f * s, ink);
                UiShapes.Line(c + new Vector2(0.04f, 0) * s, c + new Vector2(0.86f, 0) * s, 0.22f * s, ink);
                UiShapes.Line(c + new Vector2(0.5f, 0) * s, c + new Vector2(0.5f, 0.36f) * s, 0.2f * s, ink);
                UiShapes.Line(c + new Vector2(0.84f, 0) * s, c + new Vector2(0.84f, 0.3f) * s, 0.2f * s, ink);
                break;
            case UiIcon.Pin:
                // A map pin: a round head with a hole, tapering to a point
                UiShapes.Triangle(c + new Vector2(-0.52f, -0.18f) * s, c + new Vector2(0.52f, -0.18f) * s, c + new Vector2(0, 0.98f) * s, ink, 0.06f * s);
                UiShapes.Circle(c + new Vector2(0, -0.3f) * s, 0.6f * s, ink);
                UiShapes.Circle(c + new Vector2(0, -0.3f) * s, 0.24f * s, back);
                break;
            case UiIcon.Size:
                // Two figures side by side, one tall and one short
                UiShapes.Circle(c + new Vector2(-0.42f, -0.62f) * s, 0.26f * s, ink);
                UiShapes.Fill(new Rectangle(c.X - 0.72f * s, c.Y - 0.3f * s, 0.6f * s, 1.22f * s), 0.24f * s, ink);
                UiShapes.Circle(c + new Vector2(0.46f, 0.08f) * s, 0.22f * s, ink);
                UiShapes.Fill(new Rectangle(c.X + 0.2f * s, c.Y + 0.34f * s, 0.52f * s, 0.58f * s), 0.2f * s, ink);
                break;
            // The wardrobe's tabs (plan 11 · C10); the bag's is Bag
            case UiIcon.Hat:
                // A cap seen from the side: the crown and the brim
                UiShapes.Fill(new Rectangle(c.X - 0.72f * s, c.Y - 0.62f * s, 1.2f * s, 1.0f * s), 0.5f * s, ink);
                UiShapes.Fill(new Rectangle(c.X - 0.72f * s, c.Y + 0.08f * s, 1.62f * s, 0.34f * s), 0.17f * s, ink);
                UiShapes.Line(c + new Vector2(-0.5f, -0.04f) * s, c + new Vector2(0.28f, -0.04f) * s, 0.12f * s, back);
                break;
            case UiIcon.Shirt:
                // A tee: the body and the two sleeves, the neck cut out
                UiShapes.Fill(new Rectangle(c.X - 0.5f * s, c.Y - 0.7f * s, 1.0f * s, 1.5f * s), 0.16f * s, ink);
                UiShapes.Line(c + new Vector2(-0.42f, -0.56f) * s, c + new Vector2(-0.92f, -0.08f) * s, 0.42f * s, ink);
                UiShapes.Line(c + new Vector2(0.42f, -0.56f) * s, c + new Vector2(0.92f, -0.08f) * s, 0.42f * s, ink);
                UiShapes.Circle(c + new Vector2(0, -0.74f) * s, 0.24f * s, back);
                break;
            case UiIcon.Trousers:
                // The waistband and the two legs, apart below the crotch
                UiShapes.Fill(new Rectangle(c.X - 0.66f * s, c.Y - 0.9f * s, 1.32f * s, 0.6f * s), 0.12f * s, ink);
                UiShapes.Line(c + new Vector2(-0.42f, -0.4f) * s, c + new Vector2(-0.48f, 0.82f) * s, 0.46f * s, ink);
                UiShapes.Line(c + new Vector2(0.42f, -0.4f) * s, c + new Vector2(0.48f, 0.82f) * s, 0.46f * s, ink);
                break;
            case UiIcon.Shoe:
                // A sneaker from the side: the ankle, the foot and a sole
                UiShapes.Fill(new Rectangle(c.X - 0.8f * s, c.Y - 0.66f * s, 0.7f * s, 1.0f * s), 0.24f * s, ink);
                UiShapes.Fill(new Rectangle(c.X - 0.8f * s, c.Y - 0.06f * s, 1.7f * s, 0.62f * s), 0.3f * s, ink);
                UiShapes.Line(c + new Vector2(-0.66f, 0.36f) * s, c + new Vector2(0.72f, 0.36f) * s, 0.1f * s, back);
                break;
            default:
                UiShapes.Ring(c + new Vector2(0, 0.08f) * s, 0.82f * s, 0.22f * s, ink);
                UiShapes.Fill(new Rectangle(c.X - 0.34f * s, c.Y - 1.0f * s, 0.68f * s, 0.8f * s), 0f, back);
                UiShapes.Line(c + new Vector2(0, -0.88f) * s, c + new Vector2(0, -0.06f) * s, 0.22f * s, ink);
                break;
        }
    }

    /// <summary>The ♂ or ♀ mark as shapes (Nunito has no such glyphs), about <paramref name="size"/> tall. Draws nothing for genderless Pokémon.</summary>
    public static void GenderMark(Vector2 c, float size, Gender gender)
    {
        float t = size * 0.13f, r = size * 0.3f;
        if (gender == Gender.Male)
        {
            var ring = c + new Vector2(-0.1f, 0.14f) * size;
            var tip = c + new Vector2(0.36f, -0.36f) * size;
            UiShapes.Ring(ring, r, t, Male);
            UiShapes.Line(ring + new Vector2(0.7f, -0.7f) * r, tip, t, Male);
            UiShapes.Line(tip, tip + new Vector2(-0.26f, 0) * size, t, Male);
            UiShapes.Line(tip, tip + new Vector2(0, 0.26f) * size, t, Male);
        }
        else if (gender == Gender.Female)
        {
            var ring = c + new Vector2(0, -0.16f) * size;
            UiShapes.Ring(ring, r, t, Female);
            UiShapes.Line(ring + new Vector2(0, r), c + new Vector2(0, 0.5f) * size, t, Female);
            UiShapes.Line(c + new Vector2(-0.17f, 0.32f) * size, c + new Vector2(0.17f, 0.32f) * size, t, Female);
        }
    }

    /// <summary>A small solid arrowhead pointing left (-1) or right (1), its tip rounded.</summary>
    public static void ArrowH(Vector2 c, float size, int direction, Color color) =>
        UiShapes.Triangle(c + new Vector2(0.55f * direction, 0) * size, c + new Vector2(-0.4f * direction, -0.75f) * size,
            c + new Vector2(-0.4f * direction, 0.75f) * size, color, size * 0.12f);

    /// <summary>A small solid arrowhead pointing up.</summary>
    public static void ArrowUp(Vector2 c, float size, Color color) =>
        UiShapes.Triangle(c + new Vector2(0, -0.5f) * size, c + new Vector2(-0.7f, 0.4f) * size, c + new Vector2(0.7f, 0.4f) * size,
            color, size * 0.12f);

    /// <summary>A small solid arrowhead pointing down.</summary>
    public static void ArrowDown(Vector2 c, float size, Color color) =>
        UiShapes.Triangle(c + new Vector2(0, 0.5f) * size, c + new Vector2(-0.7f, -0.4f) * size, c + new Vector2(0.7f, -0.4f) * size,
            color, size * 0.12f);
}
