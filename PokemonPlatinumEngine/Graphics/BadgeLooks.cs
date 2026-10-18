using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>The marks a region's own badges carry (style guide, "Badges").</summary>
public enum BadgeMark { Stone, Drop, Bolt, Rings, Heart, Twins, Flame, Leaf }

/// <summary>
/// The badges with a look of their own (plan 19 · K4; style guide, "Badges"), by the name their region's file gives them:
/// a colour for the medallion's enamel and a mark of ours. A badge not named here takes the row's colour and mark
/// (<c>ModernUi.Badge</c>). The names are the data's, never words on screen.
/// </summary>
public static class BadgeLooks
{
    public static readonly IReadOnlyDictionary<string, (Color Color, BadgeMark Mark)> ByName =
        new Dictionary<string, (Color, BadgeMark)>(StringComparer.OrdinalIgnoreCase)
        {
            // Kanto's eight
            ["Boulder"] = (new Color(150, 146, 140, 255), BadgeMark.Stone),
            ["Cascade"] = (new Color(64, 150, 222, 255), BadgeMark.Drop),
            ["Thunder"] = (new Color(236, 160, 52, 255), BadgeMark.Bolt),
            ["Rainbow"] = (new Color(156, 110, 214, 255), BadgeMark.Rings),
            ["Soul"] = (new Color(222, 92, 140, 255), BadgeMark.Heart),
            ["Marsh"] = (new Color(214, 178, 68, 255), BadgeMark.Twins),
            ["Volcano"] = (new Color(220, 72, 54, 255), BadgeMark.Flame),
            ["Earth"] = (new Color(82, 160, 86, 255), BadgeMark.Leaf)
        };
}
