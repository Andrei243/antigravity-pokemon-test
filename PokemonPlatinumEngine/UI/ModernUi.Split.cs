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
