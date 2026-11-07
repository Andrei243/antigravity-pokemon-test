using PokemonPlatinumEngine.Overworld;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The furniture of plan 36 · TR2's training hall, built in the rooms' kit (style guide, "Rooms", "A training hall"): a
/// drill target hung on the back wall and a training mat laid on the floor. Cards and decals whose faces are painted at
/// their own size, with no GPU call, like <see cref="PropModels"/>.
/// </summary>
internal static class TrainingArt
{
    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    /// <summary>Whether a prop is built here: a target or a mat.</summary>
    public static bool Builds(Prop p) => p.Type is PropType.DrillTarget or PropType.TrainingMat;

    public static void Build(KitBuilder kit, Prop p, Map map)
    {
        int w = p.Width * 32, d = p.Depth * 32;
        if (p.Type == PropType.DrillTarget) Target(kit, w);
        else Mat(kit, w, d);
    }

    /// <summary>
    /// A target on the back wall: a square frame of pale wood 28 texels across, and in it a round board of three rings,
    /// cream at the rim, blue between and the club's orange in the middle, a dark rim between each, with two nicks where
    /// shots have struck. Its foot 30 texels over the floor.
    /// </summary>
    private static void Target(KitBuilder kit, int w)
    {
        const int size = 28;
        var art = kit.Face("drilltarget", size, size, c =>
        {
            var frame = Tone.Of(196, 146, 96);
            Pix.Raised(c, 0, 0, size, size, frame);
            c.Rect(2, 2, size - 4, size - 4, Rgb(226, 214, 192));
            var cream = Rgb(250, 242, 222);
            var blue = Rgb(70, 120, 210);
            var orange = Rgb(232, 112, 64);
            var rim = Rgb(120, 96, 74);
            float m = (size - 1) / 2f;
            for (int y = 2; y < size - 2; y++)
                for (int x = 2; x < size - 2; x++)
                {
                    float dx = x - m, dy = y - m, r2 = dx * dx + dy * dy;
                    if (r2 > 11.5f * 11.5f) continue;
                    var colour = r2 > 10.5f * 10.5f ? rim : r2 > 8 * 8 ? cream : r2 > 7 * 7 ? rim : r2 > 4.5f * 4.5f ? blue : r2 > 3.5f * 3.5f ? rim : orange;
                    c.Set(x, y, colour);
                }
            // Two nicks where shots struck
            c.Rect(9, 10, 2, 2, Rgb(80, 60, 48));
            c.Rect(17, 16, 2, 2, Rgb(80, 60, 48));
        });
        float x0 = (w - size) / 2f;
        kit.Card(x0, x0 + size, 32.5f, 30, 30 + size, art);
    }

    /// <summary>
    /// A mat on the floor: padded blue (two blues in panels 32 texels square, a seam between), a white border two texels
    /// wide three in from its edge, and a white lane line across its middle; its edge a darker blue.
    /// </summary>
    private static void Mat(KitBuilder kit, int w, int d)
    {
        int mw = w - 4, md = d - 4;
        var art = kit.Face($"trainingmat.{mw}x{md}", mw, md, c =>
        {
            var blue = Rgb(84, 132, 200);
            var blue2 = Rgb(92, 142, 210);
            var seam = Rgb(64, 104, 168);
            var edge = Rgb(52, 86, 146);
            var white = Rgb(236, 240, 246);
            for (int y = 0; y < md; y++)
                for (int x = 0; x < mw; x++)
                    c.Set(x, y, ((x / 32) + (y / 32)) % 2 == 0 ? blue : blue2);
            for (int x = 32; x < mw; x += 32) c.VLine(x, 0, md, seam);
            for (int y = 32; y < md; y += 32) c.HLine(0, y, mw, seam);
            Pix.Border(c, 0, 0, mw, md, edge);
            c.Rect(3, 3, mw - 6, 2, white);
            c.Rect(3, md - 5, mw - 6, 2, white);
            c.Rect(3, 3, 2, md - 6, white);
            c.Rect(mw - 5, 3, 2, md - 6, white);
            c.Rect(5, md / 2 - 1, mw - 10, 2, white);
        });
        kit.Decal(2, w - 2, 2, d - 2, 0.006f, art);
    }
}
