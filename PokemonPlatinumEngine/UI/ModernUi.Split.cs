using System.Numerics;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

internal static partial class ModernUi
{
    /// <summary>
    /// The split's band (plan 45 · BC3; style guide, "The battle's manners", "The split"): along each half's edge while
    /// the halves come in or go out, one band down the middle once they meet. A rim of the frame colour either side of
    /// a white core with a line of the selection colour down its middle, in layout units scaled by
    /// <paramref name="sx"/> and <paramref name="sy"/>.
    /// </summary>
    public static void SplitBands(float amount, float sx = 1f, float sy = 1f)
    {
        float edge = SplitBand.EdgeOffset(amount);
        Band(edge, sx, sy);
        if (edge > 0.5f) Band(-edge, sx, sy);
    }

    /// <summary>
    /// A Battle Royal's bands (plan 45 · BC7): the halves' band, upright, along each quarter's two edges while the
    /// quarters come in or go out (the picture's own edges left bare), three bands between the four once they meet.
    /// </summary>
    public static void RoyalBands(float amount, float sx = 1f, float sy = 1f)
    {
        if (amount >= 1f)
        {
            for (int i = 1; i < RoyalBand.Camps; i++) UprightBand(RoyalBand.Left(i), sx, sy);
            return;
        }
        for (int i = 0; i < RoyalBand.Camps; i++)
        {
            var region = RoyalBand.Region(i, amount);
            if (region.Count == 0) continue;
            foreach (float x in new[] { region[0].X, region[1].X })
                if (x > 1f && x < RoyalBand.Width - 1f) UprightBand(x, sx, sy);
        }
    }

    private static void UprightBand(float x, float sx, float sy)
    {
        var a = new Vector2(x * sx, -20f * sy);
        var b = new Vector2(x * sx, (RoyalBand.Height + 20f) * sy);
        UiShapes.Line(a, b, SplitBand.BandWidth * sx, Frame);
        UiShapes.Line(a, b, SplitBand.CoreWidth * sx, PanelTop);
        UiShapes.Line(a, b, SplitBand.LineWidth * sx, Selection);
    }

    private static void Band(float across, float sx, float sy)
    {
        var middle = SplitBand.Middle + SplitBand.TowardFoe * across;
        var a = middle - SplitBand.Along * 1400f;
        var b = middle + SplitBand.Along * 1400f;
        var scale = new Vector2(sx, sy);
        a *= scale;
        b *= scale;
        UiShapes.Line(a, b, SplitBand.BandWidth * sx, Frame);
        UiShapes.Line(a, b, SplitBand.CoreWidth * sx, PanelTop);
        UiShapes.Line(a, b, SplitBand.LineWidth * sx, Selection);
    }
}
