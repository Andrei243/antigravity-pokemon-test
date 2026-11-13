using System;
using System.Numerics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using Raylib_cs;
using static PokemonPlatinumEngine.Graphics.Pix;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The Johto Safari Zone's objects where an Owner has set them down (plan 38 · EX8; style guide, "Props", "The Safari
/// Zone's objects"): our own drawings, each a card standing on its tile (a stone pillar and a pond over two by two), in
/// the field's palette, outlined as every prop is. No drawing or input of their own beyond the kit's painters.
/// </summary>
internal static class SafariArt
{
    /// <summary>The start of the model's name of an object standing in the zone: <c>safari:boulder</c>.</summary>
    public const string ModelPrefix = "safari:";

    public static bool Builds(Prop p) => p.Type == PropType.SafariObject && p.Model.StartsWith(ModelPrefix, StringComparison.Ordinal);

    /// <summary>The colour each kind stands in on the Area Customizer's plan.</summary>
    public static Color KindColour(SafariObjectKind kind) => kind switch
    {
        SafariObjectKind.Plains => Rgb(214, 190, 96),
        SafariObjectKind.Forest => Rgb(96, 164, 92),
        SafariObjectKind.Peak => Rgb(150, 140, 136),
        _ => Rgb(92, 150, 214)
    };

    /// <summary>How an object is drawn on the Area Customizer's plan: the colour of the kind it gives most of.</summary>
    public static (Color Colour, string Mark) PlanLook(int type)
    {
        if (SafariObjects.TypeOf(type) is not { } t) return (Rgb(150, 160, 190), "");
        var best = SafariObjectKind.Plains;
        foreach (var kind in Enum.GetValues<SafariObjectKind>())
            if (t.PointsOf(kind) > t.PointsOf(best)) best = kind;
        return (KindColour(best), "");
    }

    /// <summary>Builds an object where it stands.</summary>
    public static void Build(KitBuilder kit, Prop p, Map map)
    {
        string key = p.Model[ModelPrefix.Length..];
        kit.Origin = new Vector3(p.X, Relief.At(map, p.X + p.Width / 2f, p.Y + p.Depth / 2f), p.Y);
        switch (key)
        {
            case "pond":
                kit.Decal(4, 60, 6, 60, 0.012f, kit.Face("safari.pond", 56, 54, PaintPond));
                break;
            case "puddle":
                kit.Decal(6, 26, 10, 26, 0.012f, kit.Face("safari.puddle", 20, 16, PaintPuddle));
                break;
            case "flowerPatch":
                kit.Decal(3, 29, 6, 28, 0.012f, kit.Face("safari.flowers", 26, 22, PaintFlowers));
                break;
            case "pebbles":
                kit.Decal(4, 28, 8, 28, 0.012f, kit.Face("safari.pebbles", 24, 20, PaintPebbles));
                break;
            case "stonePillar":
                kit.Sprite(kit.Face("safari.pillar", 30, 58, PaintPillar), 32, 40);
                break;
            case "haystack":
                kit.Sprite(kit.Face("safari.haystack", 28, 24, PaintHaystack), 16, 18);
                break;
            case "fencePost":
                kit.Sprite(kit.Face("safari.post", 12, 26, PaintPost), 16, 18);
                break;
            case "sapling":
                kit.Sprite(kit.Face("safari.sapling", 22, 38, PaintSapling), 16, 18);
                break;
            case "oldStump":
                kit.Sprite(kit.Face("safari.stump", 24, 18, PaintStump), 16, 18);
                break;
            case "mushroomRing":
                kit.Sprite(kit.Face("safari.mushrooms", 26, 14, PaintMushrooms), 16, 18);
                break;
            case "boulder":
                kit.Sprite(kit.Face("safari.boulder", 26, 22, PaintBoulder), 16, 18);
                break;
            case "reeds":
                kit.Sprite(kit.Face("safari.reeds", 22, 30, PaintReeds), 16, 18);
                break;
        }
    }

    private static readonly Tone Straw = Tone.Of(214, 182, 92);
    private static readonly Tone Bark = Tone.Of(120, 84, 56);
    private static readonly Tone Leaf = Tone.Of(84, 150, 80);
    private static readonly Tone Stone = Tone.Of(150, 142, 138);
    private static readonly Tone Pool = Tone.Of(82, 140, 200);

    public static void PaintPond(PixelCanvas c)
    {
        c.FlatEllipse(28, 27, 27, 26, Stone.Dark);
        c.FlatEllipse(28, 27, 25, 24, Pool.Dark);
        c.FlatEllipse(27, 26, 22, 21, Pool.Base);
        c.HLine(14, 18, 10, Pool.Light);
        c.HLine(30, 30, 8, Pool.Light);
        c.FlatEllipse(40, 16, 4, 3, Leaf.Base);
    }

    public static void PaintPuddle(PixelCanvas c)
    {
        c.FlatEllipse(10, 8, 9, 7, Pool.Dark);
        c.FlatEllipse(10, 7, 8, 6, Pool.Base);
        c.HLine(5, 6, 5, Pool.Light);
    }

    public static void PaintFlowers(PixelCanvas c)
    {
        Color[] petals = { Rgb(236, 98, 104), Rgb(250, 214, 82), Rgb(244, 244, 236), Rgb(204, 120, 214) };
        int[,] at = { { 4, 4 }, { 13, 3 }, { 21, 6 }, { 7, 12 }, { 16, 11 }, { 3, 18 }, { 12, 18 }, { 21, 16 } };
        for (int i = 0; i < at.GetLength(0); i++)
        {
            int x = at[i, 0], y = at[i, 1];
            var col = petals[i % petals.Length];
            c.Rect(x - 1, y, 3, 1, col);
            c.Rect(x, y - 1, 1, 3, col);
            c.Set(x, y, Rgb(250, 200, 60));
            c.Set(x, y + 2, Leaf.Dark);
        }
    }

    public static void PaintPebbles(PixelCanvas c)
    {
        int[,] at = { { 4, 5, 3 }, { 12, 3, 2 }, { 19, 7, 3 }, { 8, 13, 2 }, { 16, 15, 3 }, { 3, 16, 2 } };
        for (int i = 0; i < at.GetLength(0); i++)
        {
            c.Disc(at[i, 0], at[i, 1], at[i, 2], Stone.Dark);
            c.Disc(at[i, 0] - 0.5f, at[i, 1] - 0.5f, at[i, 2] - 1, Stone.Base);
            c.Set(at[i, 0] - 1, at[i, 1] - 1, Stone.Light);
        }
    }

    public static void PaintPillar(PixelCanvas c)
    {
        c.Rect(6, 6, 18, 50, Stone.Base);
        c.VLine(6, 6, 50, Stone.Light);
        c.VLine(23, 6, 50, Stone.Dark);
        c.Rect(3, 2, 24, 5, Stone.Light);
        c.HLine(3, 6, 24, Stone.Dark);
        c.Rect(3, 52, 24, 5, Stone.Dark);
        for (int y = 14; y < 50; y += 9) c.HLine(7, y, 16, Stone.Dark);
        c.Rect(9, 20, 4, 6, Leaf.Dark);
        Outline(c);
    }

    public static void PaintHaystack(PixelCanvas c)
    {
        c.FlatEllipse(14, 15, 13, 9, Straw.Base);
        c.FlatEllipse(14, 12, 10, 7, Straw.Light);
        c.HLine(2, 21, 24, Straw.Dark);
        for (int x = 5; x < 24; x += 4) c.VLine(x, 14, 6, Straw.Dark);
        Outline(c);
    }

    public static void PaintPost(PixelCanvas c)
    {
        c.Rect(3, 4, 6, 22, Bark.Base);
        c.VLine(3, 4, 22, Bark.Light);
        c.VLine(8, 4, 22, Bark.Dark);
        c.Rect(2, 2, 8, 3, Bark.Light);
        c.HLine(3, 12, 6, Bark.Dark);
        Outline(c);
    }

    public static void PaintSapling(PixelCanvas c)
    {
        c.Rect(10, 22, 3, 16, Bark.Base);
        c.VLine(10, 22, 16, Bark.Light);
        c.FlatEllipse(11, 13, 10, 11, Leaf.Dark);
        c.FlatEllipse(10, 11, 8, 9, Leaf.Base);
        c.FlatEllipse(8, 8, 4, 4, Leaf.Light);
        Outline(c);
    }

    public static void PaintStump(PixelCanvas c)
    {
        c.Rect(3, 6, 18, 11, Bark.Base);
        c.VLine(3, 6, 11, Bark.Light);
        c.VLine(20, 6, 11, Bark.Dark);
        c.FlatEllipse(12, 5, 9, 4, Rgb(196, 160, 112));
        c.FlatEllipse(12, 5, 5, 2, Rgb(170, 132, 90));
        c.Rect(0, 15, 4, 2, Bark.Dark);
        c.Rect(20, 15, 4, 2, Bark.Dark);
        Outline(c);
    }

    public static void PaintMushrooms(PixelCanvas c)
    {
        int[] xs = { 3, 10, 17, 22 };
        for (int i = 0; i < xs.Length; i++)
        {
            int x = xs[i], top = i % 2 == 0 ? 3 : 6;
            c.Rect(x, top + 3, 2, 13 - top - 3, Rgb(236, 228, 210));
            c.FlatEllipse(x + 1, top + 2, 3, 2, i % 2 == 0 ? Rgb(212, 72, 64) : Rgb(196, 142, 84));
            c.Set(x, top + 1, Rgb(250, 244, 232));
        }
        Outline(c);
    }

    public static void PaintBoulder(PixelCanvas c)
    {
        c.FlatEllipse(13, 13, 12, 9, Stone.Dark);
        c.FlatEllipse(12, 12, 11, 8, Stone.Base);
        c.FlatEllipse(9, 9, 5, 3, Stone.Light);
        c.HLine(6, 17, 12, Stone.Dark);
        Outline(c);
    }

    public static void PaintReeds(PixelCanvas c)
    {
        int[] xs = { 3, 7, 11, 15, 18 };
        for (int i = 0; i < xs.Length; i++)
        {
            int top = 2 + (i * 5) % 9;
            c.VLine(xs[i], top + 4, 30 - top - 4, Leaf.Dark);
            c.VLine(xs[i] + 1, top + 6, 30 - top - 6, Leaf.Base);
            c.Rect(xs[i], top, 2, 5, Bark.Base);
        }
        c.HLine(1, 29, 20, Pool.Base);
        Outline(c);
    }
}
