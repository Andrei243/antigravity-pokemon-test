using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The gathering spots of the field (plan 39 · CR2; style guide, "Props", "Gathering spots"): cards of our own in the
/// scenery's 32 texels a tile, each outlined like every card and standing at the middle of its tile, in five kinds, each
/// bearing (something loose on it to take) or bare (picked today): a rock with loose stones and a glint of ore, a fallen
/// log with chips and a bead of resin, a herb bush with sprigs, a heap of shells on sand, and a drift of snow with ice
/// crystals. Painted in code, no GPU calls.
/// </summary>
internal static class GatherArt
{
    /// <summary>Every kind's card, in texels.</summary>
    public const int Width = 30, Height = 26;

    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    /// <summary>A rock: a grey boulder; bearing, loose stones at its foot and a glint of ore in its face.</summary>
    public static void PaintRock(PixelCanvas c, bool bearing)
    {
        c.Ball(15, 14, 11, 9, Rgb(132, 128, 136));
        c.Ball(9, 18, 5, 4, Rgb(118, 114, 124));
        c.Line(12, 9, 16, 13, Rgb(92, 88, 100));
        if (bearing)
        {
            c.Ball(4, 22, 3, 2.5f, Rgb(150, 146, 152));
            c.Ball(25, 22, 3.5f, 2.5f, Rgb(140, 136, 144));
            c.Ball(20, 23, 2.5f, 2, Rgb(156, 150, 158));
            // A glint of ore: a few warm texels in the face
            c.Rect(18, 11, 2, 2, Rgb(214, 170, 78));
            c.Set(18, 11, Rgb(250, 222, 140));
            c.Rect(13, 15, 2, 1, Rgb(196, 150, 70));
        }
        Pix.Outline(c);
    }

    /// <summary>A fallen log: a short trunk on its side; bearing, chips of wood round it and a bead of amber resin.</summary>
    public static void PaintWood(PixelCanvas c, bool bearing)
    {
        var bark = Rgb(112, 78, 52);
        var barkLit = Rgb(146, 104, 70);
        var core = Rgb(214, 176, 120);
        c.Rect(4, 13, 20, 8, bark);
        c.HLine(4, 13, 20, barkLit);
        c.HLine(4, 16, 20, Rgb(96, 66, 44));
        c.Ball(24, 17, 4, 5, core);
        c.Set(24, 17, Rgb(170, 128, 82));
        c.Set(23, 16, Rgb(170, 128, 82));
        if (bearing)
        {
            c.Rect(2, 22, 3, 2, core);
            c.Rect(9, 23, 2, 2, core);
            c.Rect(16, 22, 3, 2, Rgb(196, 156, 100));
            // A bead of resin on the bark
            c.Rect(11, 11, 3, 3, Rgb(222, 150, 42));
            c.Set(11, 11, Rgb(252, 214, 120));
            // A twig with a leaf
            c.Line(8, 13, 6, 8, bark);
            c.Ball(5.5f, 7, 2, 1.5f, Rgb(96, 150, 70));
        }
        Pix.Outline(c);
    }

    /// <summary>A herb bush: a low round bush; bearing, sprigs standing out of it with pale flowers.</summary>
    public static void PaintPlant(PixelCanvas c, bool bearing)
    {
        var leaf = Rgb(78, 140, 70);
        c.Ball(15, 17, 11, 7, leaf);
        c.Ball(9, 15, 5, 4, Rgb(90, 156, 78));
        c.Ball(21, 15, 5, 4, Rgb(70, 128, 64));
        if (bearing)
        {
            var stem = Rgb(70, 118, 56);
            foreach (var (x, top) in new[] { (7, 6), (13, 4), (19, 5), (24, 8) })
            {
                c.VLine(x, top, 9, stem);
                c.Rect(x - 1, top, 3, 2, Rgb(232, 228, 196));
                c.Set(x, top, Rgb(214, 160, 196));
            }
        }
        Pix.Outline(c);
    }

    /// <summary>A heap on the shore: a low mound of sand; bearing, shells and a piece of salt crust on it.</summary>
    public static void PaintShore(PixelCanvas c, bool bearing)
    {
        c.Ball(15, 19, 12, 5, Rgb(214, 196, 150));
        c.HLine(5, 22, 20, Rgb(184, 164, 120));
        if (bearing)
        {
            // A fan shell, a spiral shell and a crust of salt
            c.Ball(9, 15, 4, 3.5f, Rgb(236, 172, 160));
            c.Line(9, 18, 7, 13, Rgb(196, 128, 118));
            c.Line(9, 18, 11, 13, Rgb(196, 128, 118));
            c.Ball(19, 15, 3.5f, 3, Rgb(232, 220, 196));
            c.Set(19, 15, Rgb(170, 150, 120));
            c.Set(20, 14, Rgb(170, 150, 120));
            c.Rect(23, 16, 4, 2, Rgb(244, 244, 236));
        }
        Pix.Outline(c);
    }

    /// <summary>A drift of snow: a soft mound kept well below white; bearing, two crystals of ice standing in it.</summary>
    public static void PaintSnow(PixelCanvas c, bool bearing)
    {
        c.Ball(15, 18, 12, 6, Rgb(206, 216, 232));
        c.HLine(5, 22, 20, Rgb(176, 188, 210));
        if (bearing)
        {
            var ice = Rgb(150, 200, 232);
            var iceLit = Rgb(206, 236, 248);
            c.Tri(9, 16, 12, 4, 15, 16, ice);
            c.VLine(12, 7, 8, iceLit);
            c.Tri(17, 17, 20, 8, 23, 17, ice);
            c.VLine(20, 10, 6, iceLit);
        }
        Pix.Outline(c);
    }
}
