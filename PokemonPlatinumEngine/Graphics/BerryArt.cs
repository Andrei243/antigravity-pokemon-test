using System;
using Raylib_cs;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The berry plants on soft soil (plan 06 · R14a; style guide, "Berry plants"): a card for each stage, painted in our
/// own art and coloured by the berry's strongest flavour, the soil at its foot as wet as the patch is. The soil itself
/// is the ground's bare earth (<c>World.Look</c> gives soft soil the dirt's look). GPU-free, like the field's other
/// painters; <see cref="WorldRenderer"/> makes the cards.
/// </summary>
public static class BerryArt
{
    /// <summary>The size of a plant's card in texels: it stands on the middle of its tile.</summary>
    public const int Width = 24, Height = 34;

    /// <summary>A berry's flavour colour, in the contests' order: spicy, dry, sweet, bitter, sour.</summary>
    public enum Flavour { Spicy, Dry, Sweet, Bitter, Sour }

    /// <summary>
    /// The flavour a berry is coloured by: its strongest, the first in the contests' order where two are level.
    /// The berry table holds no colours, and its art is the original's, so the plant shows what the berry tastes of.
    /// </summary>
    public static Flavour FlavourOf(BerryData? berry)
    {
        if (berry == null) return Flavour.Spicy;
        int[] tastes = { berry.Spicy, berry.Dry, berry.Sweet, berry.Bitter, berry.Sour };
        int best = 0;
        for (int i = 1; i < tastes.Length; i++)
            if (tastes[i] > tastes[best]) best = i;
        return (Flavour)best;
    }

    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    /// <summary>The fruit's and the petals' three shades for a flavour: base, light and dark.</summary>
    public static (Color Base, Color Light, Color Dark) ColoursOf(Flavour flavour) => flavour switch
    {
        Flavour.Dry => (Rgb(74, 108, 212), Rgb(130, 164, 240), Rgb(48, 70, 150)),
        Flavour.Sweet => (Rgb(232, 112, 170), Rgb(248, 170, 206), Rgb(168, 70, 124)),
        Flavour.Bitter => (Rgb(164, 206, 64), Rgb(204, 232, 120), Rgb(110, 150, 42)),
        Flavour.Sour => (Rgb(240, 198, 58), Rgb(252, 230, 130), Rgb(186, 140, 36)),
        _ => (Rgb(214, 64, 58), Rgb(244, 128, 108), Rgb(150, 40, 48))
    };

    /// <summary>The soil's two shades at the plant's foot: dark and wet, the dirt's own when dry, pale and cracked when bone dry.</summary>
    public static (Color Base, Color Light) SoilOf(SoilMoisture moisture) => moisture switch
    {
        SoilMoisture.Moist => (Rgb(92, 64, 46), Rgb(118, 84, 60)),
        SoilMoisture.Dry => (Rgb(146, 108, 74), Rgb(170, 132, 94)),
        _ => (Rgb(182, 152, 112), Rgb(204, 178, 138))
    };

    private static readonly Color Leaf = Rgb(76, 150, 72), LeafLight = Rgb(126, 194, 96), LeafDark = Rgb(48, 104, 58);
    private static readonly Color Stem = Rgb(70, 128, 62), Heart = Rgb(250, 220, 90), Crack = Rgb(140, 110, 80);

    /// <summary>A plant at a stage, of a flavour, on soil this wet; nothing for a patch with nothing in it.</summary>
    public static PixelCanvas? Paint(BerryStage stage, Flavour flavour, SoilMoisture moisture)
    {
        if (stage == BerryStage.None) return null;
        var c = new PixelCanvas(Width, Height);
        var (fruit, fruitLight, fruitDark) = ColoursOf(flavour);

        // The soil heaped round the plant's foot, lit from the upper left
        var (soil, soilLight) = SoilOf(moisture);
        Mound(c, soil, soilLight);
        if (moisture == SoilMoisture.VeryDry)
        {
            c.Set(9, 31, Crack); c.Set(10, 32, Crack); c.Set(15, 30, Crack); c.Set(16, 31, Crack);
        }

        switch (stage)
        {
            case BerryStage.Planted:
                // A seed showing in a dimple at the top of the heap
                c.HLine(10, 28, 4, soil);
                c.Rect(11, 28, 2, 1, fruitDark);
                c.Set(11, 27, fruit);
                c.Set(12, 27, fruitLight);
                break;
            case BerryStage.Sprouted:
                c.VLine(12, 22, 7, Stem);
                LeafAt(c, 9, 21, 3, 2, left: true);
                LeafAt(c, 15, 20, 3, 2, left: false);
                break;
            case BerryStage.Growing:
                c.VLine(12, 12, 17, Stem);
                LeafAt(c, 8, 24, 4, 2, left: true);
                LeafAt(c, 16, 21, 4, 2, left: false);
                LeafAt(c, 8, 17, 4, 2, left: true);
                LeafAt(c, 16, 14, 3, 2, left: false);
                // A bud at the top in the flavour's colour
                c.Rect(12, 10, 1, 2, fruitDark);
                c.Set(11, 11, fruit);
                break;
            case BerryStage.Blooming:
                Bush(c);
                Flower(c, 7, 12, fruitLight, fruit);
                Flower(c, 17, 10, fruitLight, fruit);
                Flower(c, 12, 5, fruitLight, fruit);
                break;
            default:
                Bush(c);
                Fruit(c, 7, 13, fruit, fruitLight, fruitDark);
                Fruit(c, 17, 12, fruit, fruitLight, fruitDark);
                Fruit(c, 12, 6, fruit, fruitLight, fruitDark);
                Fruit(c, 9, 20, fruit, fruitLight, fruitDark);
                Fruit(c, 16, 19, fruit, fruitLight, fruitDark);
                break;
        }
        Pix.Outline(c);
        return c;
    }

    // The heap of soil: 18 texels wide and 6 tall on the card's last rows
    private static void Mound(PixelCanvas c, Color soil, Color light)
    {
        for (int y = 28; y < Height; y++)
            for (int x = 3; x < 21; x++)
            {
                float u = (x + 0.5f - 12f) / 9f, v = (y + 0.5f - 33f) / 5.5f;
                if (u * u + v * v > 1f) continue;
                c.Set(x, y, u + v < -0.9f ? light : soil);
            }
    }

    // A leaf: a flat oval, lit on its upper side, its dark edge underneath, joined to the stem
    private static void LeafAt(PixelCanvas c, int cx, int cy, int rx, int ry, bool left)
    {
        for (int y = cy - ry; y <= cy + ry; y++)
            for (int x = cx - rx; x <= cx + rx; x++)
            {
                float u = (x + 0.5f - (cx + 0.5f)) / (rx + 0.5f), v = (y + 0.5f - (cy + 0.5f)) / (ry + 0.5f);
                if (u * u + v * v > 1f) continue;
                c.Set(x, y, v < -0.3f ? LeafLight : v > 0.45f ? LeafDark : Leaf);
            }
        c.HLine(left ? cx + rx : 12, cy + 1, Math.Max(1, left ? 12 - (cx + rx) : cx - rx - 12), Stem);
    }

    // A grown plant: a stem and its leaves, broad at the foot
    private static void Bush(PixelCanvas c)
    {
        c.VLine(12, 8, 21, Stem);
        LeafAt(c, 7, 25, 4, 2, left: true);
        LeafAt(c, 17, 24, 4, 2, left: false);
        LeafAt(c, 6, 17, 4, 2, left: true);
        LeafAt(c, 18, 16, 4, 2, left: false);
        LeafAt(c, 8, 10, 3, 2, left: true);
        LeafAt(c, 16, 8, 3, 2, left: false);
    }

    // A flower of five petals round a yellow heart
    private static void Flower(PixelCanvas c, int cx, int cy, Color petal, Color shade)
    {
        c.Rect(cx - 1, cy - 2, 3, 1, petal);
        c.Rect(cx - 2, cy - 1, 5, 2, petal);
        c.Rect(cx - 2, cy + 1, 2, 1, shade);
        c.Rect(cx + 1, cy + 1, 2, 1, shade);
        c.Set(cx, cy, Heart);
        c.Set(cx, cy - 1, Heart);
    }

    // A berry: round, lit on its upper left, a glint and a short stalk
    private static void Fruit(PixelCanvas c, int cx, int cy, Color fruit, Color light, Color dark)
    {
        for (int y = cy - 3; y <= cy + 3; y++)
            for (int x = cx - 3; x <= cx + 3; x++)
            {
                float u = (x + 0.5f - (cx + 0.5f)) / 3.5f, v = (y + 0.5f - (cy + 0.5f)) / 3.5f;
                if (u * u + v * v > 1f) continue;
                c.Set(x, y, u + v < -0.8f ? light : u + v > 0.7f ? dark : fruit);
            }
        c.Set(cx - 1, cy - 1, Rgb(255, 246, 226));
        c.Set(cx, cy - 4, Stem);
    }
}
