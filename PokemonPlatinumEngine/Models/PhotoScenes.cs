using System;
using System.Collections.Generic;
using Raylib_cs;

namespace PokemonPlatinumEngine.Models;

/// <summary>What kind of place a photograph of Cameron's was taken in (plan 20, part t): the scene its print is drawn on.</summary>
public enum PhotoSetting
{
    Town,
    Route,
    Forest,
    Shore,
    Mountain,
    Cave,
    Room
}

/// <summary>One shape of a photograph's scene, in shares of the print (0 to 1 across and down).</summary>
public enum PhotoShapeKind { Rect, Circle, Triangle }

/// <summary>
/// A shape of a scene: a rectangle (<see cref="X"/>, <see cref="Y"/> its top left, <see cref="W"/>, <see cref="H"/> its size),
/// a circle (<see cref="X"/>, <see cref="Y"/> its middle, <see cref="W"/> its radius as a share of the print's width) or a
/// triangle standing on its base (<see cref="X"/>, <see cref="Y"/> the base's left end, <see cref="W"/> its width,
/// <see cref="H"/> its height upward).
/// </summary>
public readonly record struct PhotoShape(PhotoShapeKind Kind, float X, float Y, float W, float H, Color Color);

/// <summary>A photograph's scene: the sky, the ground from <see cref="Horizon"/> down, and what stands in it.</summary>
public sealed record PhotoSceneLook(PhotoSetting Setting, Color Sky, Color Ground, float Horizon, IReadOnlyList<PhotoShape> Shapes);

/// <summary>
/// The scenes Cameron's photographs are printed on (plan 20, part t; style guide, "Menu screens", "The photo album"):
/// which kind of place an area is (<see cref="Of"/>) and the plain shapes its print is drawn with (<see cref="Look"/>),
/// all our own. The lead stands on the horizon in front of it, the player's figure beside it. No drawing or input:
/// the page draws what this says.
/// </summary>
public static class PhotoScenes
{
    /// <summary>The places by the sea that are no town (HeartGold's water routes and the ports Cameron could stand in).</summary>
    private static readonly HashSet<string> SeaPlaces = new(StringComparer.OrdinalIgnoreCase)
    {
        "route_21", "route_40", "route_41", "johto_route_19", "johto_route_20", "ss_aqua_olivine_port_exterior",
        "ss_aqua_vermilion_port_exterior"
    };

    /// <summary>
    /// The kind of place an area is: a room where its map is indoors, a cave where the area or its map is one, a
    /// mountain for Mt. Silver and snowy ground, a forest by its name, the shore for the sea's routes, a town where the
    /// area's header calls it one and a route otherwise. An unknown place (a photograph of a place no longer open) is a
    /// route.
    /// </summary>
    public static PhotoSetting Of(string place, bool town, bool cave, bool indoors, bool snowbound)
    {
        string key = place ?? "";
        if (indoors) return PhotoSetting.Room;
        if (key.Contains("mount_silver", StringComparison.OrdinalIgnoreCase) || snowbound) return PhotoSetting.Mountain;
        if (cave) return PhotoSetting.Cave;
        if (key.Contains("forest", StringComparison.OrdinalIgnoreCase)) return PhotoSetting.Forest;
        if (SeaPlaces.Contains(key)) return PhotoSetting.Shore;
        return town ? PhotoSetting.Town : PhotoSetting.Route;
    }

    private static Color C(int r, int g, int b) => new((byte)r, (byte)g, (byte)b, (byte)255);

    /// <summary>The scene a setting is printed on.</summary>
    public static PhotoSceneLook Look(PhotoSetting setting) => setting switch
    {
        PhotoSetting.Town => new(setting, C(150, 196, 232), C(196, 186, 164), 0.62f, new PhotoShape[]
        {
            new(PhotoShapeKind.Rect, 0.06f, 0.30f, 0.22f, 0.32f, C(232, 222, 200)),
            new(PhotoShapeKind.Triangle, 0.04f, 0.30f, 0.26f, 0.14f, C(188, 84, 72)),
            new(PhotoShapeKind.Rect, 0.14f, 0.48f, 0.06f, 0.14f, C(120, 84, 60)),
            new(PhotoShapeKind.Rect, 0.72f, 0.26f, 0.22f, 0.36f, C(224, 214, 194)),
            new(PhotoShapeKind.Triangle, 0.70f, 0.26f, 0.26f, 0.14f, C(76, 112, 168)),
            new(PhotoShapeKind.Rect, 0.80f, 0.48f, 0.06f, 0.14f, C(120, 84, 60)),
            new(PhotoShapeKind.Circle, 0.86f, 0.12f, 0.05f, 0f, C(252, 236, 170))
        }),
        PhotoSetting.Route => new(setting, C(150, 196, 232), C(126, 170, 102), 0.62f, new PhotoShape[]
        {
            new(PhotoShapeKind.Circle, 0.14f, 0.50f, 0.09f, 0f, C(76, 128, 72)),
            new(PhotoShapeKind.Rect, 0.13f, 0.52f, 0.02f, 0.10f, C(110, 80, 56)),
            new(PhotoShapeKind.Circle, 0.88f, 0.48f, 0.08f, 0f, C(84, 138, 78)),
            new(PhotoShapeKind.Rect, 0.87f, 0.50f, 0.02f, 0.12f, C(110, 80, 56)),
            new(PhotoShapeKind.Rect, 0.40f, 0.62f, 0.20f, 0.38f, C(200, 182, 136)),
            new(PhotoShapeKind.Circle, 0.20f, 0.14f, 0.07f, 0f, C(246, 248, 252)),
            new(PhotoShapeKind.Circle, 0.27f, 0.15f, 0.05f, 0f, C(246, 248, 252))
        }),
        PhotoSetting.Forest => new(setting, C(128, 172, 150), C(84, 122, 70), 0.64f, new PhotoShape[]
        {
            new(PhotoShapeKind.Triangle, 0.00f, 0.64f, 0.22f, 0.50f, C(44, 86, 56)),
            new(PhotoShapeKind.Triangle, 0.14f, 0.64f, 0.20f, 0.42f, C(56, 100, 62)),
            new(PhotoShapeKind.Triangle, 0.66f, 0.64f, 0.20f, 0.44f, C(56, 100, 62)),
            new(PhotoShapeKind.Triangle, 0.78f, 0.64f, 0.22f, 0.54f, C(44, 86, 56)),
            new(PhotoShapeKind.Circle, 0.50f, 0.06f, 0.10f, 0f, C(200, 226, 170))
        }),
        PhotoSetting.Shore => new(setting, C(160, 208, 238), C(226, 210, 160), 0.70f, new PhotoShape[]
        {
            new(PhotoShapeKind.Rect, 0.00f, 0.50f, 1.00f, 0.20f, C(70, 136, 196)),
            new(PhotoShapeKind.Rect, 0.00f, 0.50f, 1.00f, 0.02f, C(128, 186, 226)),
            new(PhotoShapeKind.Circle, 0.80f, 0.16f, 0.06f, 0f, C(252, 236, 170)),
            new(PhotoShapeKind.Triangle, 0.10f, 0.50f, 0.08f, 0.10f, C(246, 244, 236))
        }),
        PhotoSetting.Mountain => new(setting, C(176, 200, 222), C(220, 226, 232), 0.66f, new PhotoShape[]
        {
            new(PhotoShapeKind.Triangle, 0.00f, 0.66f, 0.50f, 0.46f, C(124, 134, 150)),
            new(PhotoShapeKind.Triangle, 0.10f, 0.40f, 0.24f, 0.20f, C(240, 244, 248)),
            new(PhotoShapeKind.Triangle, 0.42f, 0.66f, 0.58f, 0.54f, C(108, 118, 136)),
            new(PhotoShapeKind.Triangle, 0.65f, 0.34f, 0.24f, 0.22f, C(240, 244, 248))
        }),
        PhotoSetting.Cave => new(setting, C(70, 62, 66), C(112, 98, 86), 0.62f, new PhotoShape[]
        {
            new(PhotoShapeKind.Triangle, 0.06f, 0.62f, 0.14f, 0.20f, C(92, 82, 80)),
            new(PhotoShapeKind.Triangle, 0.80f, 0.62f, 0.16f, 0.24f, C(92, 82, 80)),
            new(PhotoShapeKind.Rect, 0.20f, 0.00f, 0.06f, 0.10f, C(92, 82, 80)),
            new(PhotoShapeKind.Rect, 0.70f, 0.00f, 0.05f, 0.14f, C(92, 82, 80)),
            new(PhotoShapeKind.Circle, 0.50f, 0.30f, 0.16f, 0f, C(98, 88, 84))
        }),
        _ => new(setting, C(232, 220, 196), C(176, 140, 104), 0.66f, new PhotoShape[]
        {
            new(PhotoShapeKind.Rect, 0.00f, 0.56f, 1.00f, 0.10f, C(210, 196, 170)),
            new(PhotoShapeKind.Rect, 0.10f, 0.16f, 0.18f, 0.22f, C(150, 196, 232)),
            new(PhotoShapeKind.Rect, 0.10f, 0.26f, 0.18f, 0.012f, C(246, 244, 236)),
            new(PhotoShapeKind.Rect, 0.72f, 0.14f, 0.16f, 0.20f, C(196, 150, 104)),
            new(PhotoShapeKind.Rect, 0.74f, 0.16f, 0.12f, 0.16f, C(126, 170, 102))
        })
    };

    /// <summary>The colours of the player's figure on a print: a boy's cap blue, a girl's red.</summary>
    public static (Color Hat, Color Coat) FigureOf(string? look) =>
        string.Equals(look, "Girl", StringComparison.OrdinalIgnoreCase)
            ? (C(206, 66, 76), C(236, 108, 96))
            : (C(56, 88, 168), C(82, 116, 196));
}
