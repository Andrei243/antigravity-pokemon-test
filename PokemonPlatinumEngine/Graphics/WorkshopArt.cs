using PokemonPlatinumEngine.Overworld;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The furniture of plan 39 · CR1's workshop, built in the rooms' kit (style guide, "Rooms"): a workbench, a plank top
/// on two trestles with a vice screwed to its end, a mallet and a row of chisels laid out, a heap of shavings and a
/// coil of cord. Boxes and cards whose faces are painted at their own size, with no GPU call, like <see cref="PropModels"/>.
/// </summary>
internal static class WorkshopArt
{
    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    private static readonly Tone Wood = Tone.Of(184, 126, 78), WoodDark = Tone.Of(128, 86, 56), WoodLight = Tone.Of(214, 160, 104);
    private static readonly Tone Iron = Tone.Of(110, 116, 132, 150, 156, 172, 74, 78, 94);

    /// <summary>Whether a prop is built here: the workbench.</summary>
    public static bool Builds(Prop p) => p.Type == PropType.Workbench;

    public static void Build(KitBuilder kit, Prop p, Map map)
    {
        int w = p.Width * 32, d = p.Depth * 32;
        Workbench(kit, w, d);
    }

    private static void Top(PixelCanvas c, Tone t)
    {
        c.Rect(0, 0, c.Width, c.Height, t.Base);
        c.HLine(0, c.Height - 1, c.Width, t.Light);
        c.VLine(0, 0, c.Height, t.Light);
        c.HLine(0, 0, c.Width, t.Dark);
        c.VLine(c.Width - 1, 0, c.Height, t.Dark);
    }

    private static void Edge(PixelCanvas c, Tone t)
    {
        for (int y = 0; y < c.Height; y++)
            c.HLine(0, y, c.Width, y == 0 ? t.Light : y == c.Height - 1 ? t.Dark : t.Base);
    }

    /// <summary>
    /// The bench: a thick top of planks (their seams across it, a nick or two worked into the wood), two trestles at
    /// its ends, and on it from the left a heap of pale shavings, a mallet, three chisels side by side with their
    /// handles toward the front and a vice at the right end.
    /// </summary>
    private static void Workbench(KitBuilder kit, int w, int d)
    {
        int tw = w - 6, td = d - 12;
        var top = kit.Face($"workbench.top.{tw}x{td}", tw, td, c =>
        {
            Top(c, WoodLight);
            for (int x = 14; x < tw - 2; x += 14) c.VLine(x, 1, td - 2, WoodLight.Dark);
            // A few nicks and a stain where the work is done
            c.Rect(tw / 2 - 5, td / 2, 8, 2, WoodDark.Base);
            c.Rect(tw / 2 + 4, td / 2 - 3, 2, 2, WoodDark.Dark);
            // A heap of shavings at the left end
            foreach (var (x, y) in new[] { (5, 4), (8, 7), (11, 5), (6, 9), (13, 9), (9, 11) })
                c.Rect(x, y, 5, 2, Rgb(240, 222, 178));
            c.Rect(7, 5, 3, 1, Rgb(206, 178, 126));
            // A mallet lying across the top: a head of dark wood on a haft
            int mx = tw / 2 - 18;
            c.Rect(mx, td / 2 + 2, 22, 3, Wood.Base);
            c.Rect(mx + 14, td / 2 - 1, 9, 9, WoodDark.Base);
            c.HLine(mx + 14, td / 2 - 1, 9, WoodDark.Light);
            c.HLine(mx + 14, td / 2 + 7, 9, WoodDark.Dark);
            // Three chisels side by side: a steel blade and a red handle
            for (int i = 0; i < 3; i++)
            {
                int x = tw / 2 + 12 + i * 6;
                c.Rect(x, 5, 2, 8, Iron.Light);
                c.Rect(x, 13, 2, 6, Rgb(200, 66, 66));
                c.Rect(x, 5, 2, 1, Rgb(236, 240, 248));
            }
        });
        var edge = kit.Face($"workbench.edge.{tw}", tw, 5, c => Edge(c, WoodLight));
        var edgeSide = kit.Face($"workbench.edge.side.{td}", td, 5, c => Edge(c, Wood));
        kit.Box(3, w - 3, 6, d - 6, 21, 26, top, edge, edgeSide, edgeSide);

        // Two trestles: legs splayed by a crossbar
        foreach (int x in new[] { 6, w - 12 })
        {
            foreach (int z in new[] { 8, d - 12 })
                kit.Block("workbench.leg", WoodDark, x, x + 5, z, z + 4, 0, 21);
            kit.Block("workbench.bar", Wood, x, x + 5, 8, d - 8, 7, 10);
        }

        // The vice screwed to the right end, its jaws above the top and its screw handle in front
        float vx = w - 24;
        kit.Block("workbench.vice", Iron, vx, vx + 14, d / 2f - 6, d / 2f + 6, 26, 33);
        kit.Block("workbench.vice.jaw", Iron, vx + 14, vx + 18, d / 2f - 6, d / 2f + 6, 26, 35);
        kit.Block("workbench.vice.handle", Iron, vx - 2, vx + 2, d / 2f + 6, d / 2f + 16, 29, 31);
        // A coil of cord hung on the near trestle
        kit.Sprite(kit.Face("workbench.cord", 10, 10, c =>
        {
            c.Rect(1, 1, 8, 8, Rgb(214, 186, 128));
            c.Rect(3, 3, 4, 4, Rgb(128, 86, 56));
            c.HLine(1, 4, 8, Rgb(176, 146, 92));
            Pix.Outline(c);
        }), 14, d - 9, 8);
    }
}
