using System;
using Raylib_cs;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// What a season changes of the field's look (plan 22 · U3; style guide, "Seasons"): the lawn's two tones, the
/// tufts' and the tall grass's four shades, the forest floor and the tall grass's ground, the trees' tints, the
/// flowers and whether snow lies. No GPU calls, so the numbers can be held to the style guide.
/// <para><see cref="Default"/> is the field as it has always been, which is what every region without seasons is
/// drawn with and what Unova's spring is, texel for texel.</para>
/// </summary>
internal sealed record SeasonPalette(
    Color Grass, Color GrassLight,
    Color[] Tufts, Color[] TallGrass,
    Color Forest, Color TallGround,
    Color RoundFrom, Color RoundTo,
    Color PineFrom, Color PineTo,
    (Color Petal, Color Shade)[] Flowers,
    bool Snow)
{
    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    /// <summary>The field's own look: no season, or spring.</summary>
    public static readonly SeasonPalette Default = new(
        Rgb(104, 190, 98), Rgb(120, 200, 102),
        new[] { Rgb(70, 150, 82), Rgb(88, 172, 90), Rgb(120, 200, 102), Rgb(160, 222, 122) },
        new[] { Rgb(36, 110, 62), Rgb(62, 150, 78), Rgb(106, 196, 98), Rgb(170, 232, 130) },
        Rgb(58, 126, 82), Rgb(48, 124, 70),
        Rgb(96, 176, 104), Rgb(118, 192, 108),
        Rgb(104, 178, 118), Rgb(126, 196, 128),
        new[] { (Rgb(252, 252, 252), Rgb(200, 214, 236)), (Rgb(236, 84, 96), Rgb(186, 56, 72)), (Rgb(250, 210, 76), Rgb(214, 160, 48)) },
        Snow: false);

    public static readonly SeasonPalette Summer = new(
        Rgb(86, 176, 84), Rgb(102, 188, 90),
        new[] { Rgb(56, 134, 70), Rgb(72, 156, 76), Rgb(102, 188, 90), Rgb(146, 212, 108) },
        new[] { Rgb(28, 98, 54), Rgb(50, 138, 68), Rgb(92, 184, 86), Rgb(156, 224, 116) },
        Rgb(50, 116, 74), Rgb(40, 114, 62),
        Rgb(80, 164, 90), Rgb(100, 180, 94),
        Rgb(92, 168, 106), Rgb(112, 186, 116),
        new[] { (Rgb(252, 214, 72), Rgb(214, 164, 44)), (Rgb(244, 140, 52), Rgb(196, 98, 36)), (Rgb(252, 252, 252), Rgb(200, 214, 236)) },
        Snow: false);

    public static readonly SeasonPalette Autumn = new(
        Rgb(176, 170, 92), Rgb(192, 182, 104),
        new[] { Rgb(128, 112, 58), Rgb(156, 138, 70), Rgb(192, 182, 104), Rgb(222, 206, 130) },
        new[] { Rgb(112, 92, 46), Rgb(150, 122, 58), Rgb(196, 166, 82), Rgb(232, 206, 120) },
        Rgb(120, 92, 58), Rgb(116, 96, 52),
        Rgb(214, 120, 58), Rgb(226, 164, 70),
        Rgb(96, 150, 100), Rgb(112, 164, 108),
        new[] { (Rgb(236, 120, 60), Rgb(188, 84, 44)), (Rgb(196, 70, 64), Rgb(150, 48, 48)), (Rgb(232, 190, 96), Rgb(190, 146, 64)) },
        Snow: false);

    public static readonly SeasonPalette Winter = new(
        Rgb(186, 204, 232), Rgb(196, 212, 238),
        new[] { Rgb(132, 138, 150), Rgb(156, 164, 178), Rgb(196, 206, 224), Rgb(226, 232, 244) },
        new[] { Rgb(92, 104, 96), Rgb(128, 140, 124), Rgb(176, 190, 196), Rgb(226, 232, 244) },
        Rgb(150, 168, 200), Rgb(120, 140, 150),
        Rgb(208, 218, 236), Rgb(222, 230, 242),
        Rgb(104, 178, 118), Rgb(126, 196, 128),
        new[] { (Rgb(252, 252, 252), Rgb(200, 214, 236)), (Rgb(196, 214, 240), Rgb(150, 172, 214)), (Rgb(252, 252, 252), Rgb(200, 214, 236)) },
        Snow: true);

    /// <summary>The palette a season's field is drawn with; none (a region without seasons) or spring is the field's own.</summary>
    public static SeasonPalette For(Season? season) => season switch
    {
        Core.Season.Summer => Summer,
        Core.Season.Autumn => Autumn,
        Core.Season.Winter => Winter,
        _ => Default
    };

    /// <summary>
    /// The key a shared sheet of the season's plants is kept under: the plain name for the field's own look, so
    /// nothing a region without seasons draws is another texture than it was.
    /// </summary>
    public static string SheetKey(string name, Season? season) =>
        ReferenceEquals(For(season), Default) ? name : $"{name}_{season!.Value.ToString().ToLowerInvariant()}";
}
