using System;
using Raylib_cs;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Which style a building gets (style guide, "Style per town" and "Kinds of building"): a house follows its
/// town, a public building looks the same wherever it stands, and a building of the imported world brings its
/// storeys, its sign and its porch with it.
/// </summary>
internal static partial class BuildingArt
{
    /// <summary>The roof "colour" that stands for snow lying on it: <see cref="RoofTiles"/> gives the snow sheet for it.</summary>
    public static readonly Color SnowRoof = Rgb(226, 234, 246);

    /// <summary>The roof "colour" of sheet metal: <see cref="RoofTiles"/> gives ribbed sheets for it.</summary>
    public static readonly Color MetalRoof = Rgb(150, 160, 176);

    private static readonly Color FlatGrey = Rgb(170, 176, 190);
    private static readonly Color Teal = Rgb(52, 166, 138), Red = Rgb(214, 82, 66);

    private static Color TileRoof(TileType roof) => roof switch
    {
        TileType.RoofRed => Red,
        TileType.RoofBlue => Rgb(70, 118, 214),
        _ => Teal
    };

    /// <summary>The colour of a Gym: its leader's type.</summary>
    public static Color GymColor(PokemonType? type) => type switch
    {
        PokemonType.Rock => Rgb(150, 128, 100),
        PokemonType.Grass => Rgb(92, 170, 96),
        PokemonType.Ghost => Rgb(124, 98, 170),
        PokemonType.Fighting => Rgb(204, 96, 72),
        PokemonType.Water => Rgb(72, 140, 214),
        PokemonType.Steel => Rgb(138, 150, 170),
        PokemonType.Ice => Rgb(126, 200, 220),
        PokemonType.Electric => Rgb(236, 196, 64),
        _ => Rgb(150, 128, 100)
    };

    /// <summary>The colour of a hall's roof and entrance, by what it is.</summary>
    private static Color HallColor(string? sign) => sign switch
    {
        "CONTEST" => Rgb(214, 92, 150),
        "PAL PARK" => Rgb(96, 168, 104),
        "MARSH" => Rgb(116, 156, 84),
        "RIBBONS" => Rgb(64, 170, 180),
        _ => Rgb(214, 170, 72)
    };

    /// <summary>A town's house. <paramref name="roof"/> is the roof a hand-made map gives it; null takes the town's own.</summary>
    private static BuildingStyle House(Architecture town, Color? roof)
    {
        BuildingStyle Pitched(WallKind wall, Color own, RoofShape shape = RoofShape.Gable) =>
            new() { Wall = wall, Roof = shape, RoofColor = roof ?? own, Accent = roof ?? own, Home = true };

        return town switch
        {
            Architecture.City => new BuildingStyle
            {
                Wall = WallKind.Brick, Roof = RoofShape.Flat, RoofColor = FlatGrey, Storeys = 2, Window = WindowKind.Sash,
                Home = true, Accent = roof ?? Rgb(74, 96, 150), Gear = RoofGear.Vents
            },
            Architecture.Plaster => Pitched(WallKind.Plaster, Red) with { Chimney = true, FlowerBoxes = true },
            Architecture.Clapboard => Pitched(WallKind.Clapboard, Red) with { Chimney = true, Shutters = true, FlowerBoxes = true },
            Architecture.Brick => Pitched(WallKind.Brick, Rgb(92, 94, 108)) with { Chimney = true },
            Architecture.Cottage => Pitched(WallKind.Clapboard, Rgb(226, 112, 140)) with { Shutters = true, FlowerBoxes = true },
            Architecture.HalfTimber => Pitched(WallKind.HalfTimber, Rgb(86, 128, 96)) with { Chimney = true },
            Architecture.Townhouse => Pitched(WallKind.Plaster, Rgb(150, 98, 150), RoofShape.Hip) with { Shutters = true },
            Architecture.Farm => Pitched(WallKind.Boards, Rgb(206, 170, 96)) with { Chimney = true },
            Architecture.Stone => Pitched(WallKind.Stone, Rgb(112, 124, 150)),
            Architecture.Marsh => Pitched(WallKind.Planks, Rgb(116, 156, 84), RoofShape.Hip) with { FlowerBoxes = true },
            Architecture.Harbour => Pitched(WallKind.Brick, Rgb(84, 104, 140)) with { Chimney = true },
            Architecture.Snow => Pitched(WallKind.Log, SnowRoof) with { Chimney = true, Accent = Rgb(96, 110, 140) },
            Architecture.Seaside => Pitched(WallKind.Stucco, Rgb(240, 150, 70), RoofShape.Hip) with { Gear = RoofGear.Solar },
            Architecture.Resort => Pitched(WallKind.Clapboard, Rgb(64, 170, 180), RoofShape.Hip) with { Shutters = true },
            _ => Pitched(WallKind.Planks, Teal) with { Chimney = true, Shutters = true, FlowerBoxes = true }
        };
    }

    /// <summary>The width of an entrance block that carries a name: room for the letters at double size.</summary>
    private static int PortalFor(string? text, int least = 52) => text == null ? least : Math.Max(least, Pix.TextWidth(text, 2) + 12);

    /// <summary>The style of a building: public buildings look the same in every town, houses follow the town.</summary>
    public static BuildingStyle StyleOf(BuildingInfo b, Architecture mapTown)
    {
        var town = b.Town ?? mapTown;
        bool placed = b.Model.Length > 0;
        // A hand-made map colours its houses' roofs by its roof tiles; a building of the world follows its town
        var house = House(town, placed ? null : TileRoof(b.RoofTile));
        bool snowy = placed && town == Architecture.Snow;

        var shop = new BuildingStyle { Wall = WallKind.Plaster, Roof = RoofShape.Hip, Window = WindowKind.Shop, GlassDoor = true, Portal = 52 };
        var block = new BuildingStyle { Wall = WallKind.Panel, Roof = RoofShape.Flat, RoofColor = FlatGrey, Storeys = 2, Window = WindowKind.Ribbon, GlassDoor = true };
        var brickBlock = new BuildingStyle
        {
            Wall = WallKind.Brick, Roof = RoofShape.Flat, RoofColor = FlatGrey, Storeys = 3, Window = WindowKind.Sash,
            Balconies = true, Home = true, Accent = Rgb(74, 96, 150), Gear = RoofGear.Vents
        };

        var style = b.Kind switch
        {
            BuildingKind.PokemonCenter => shop with { RoofColor = snowy ? SnowRoof : Rgb(238, 104, 58), Sign = SignKind.Center, Accent = Rgb(226, 72, 62) },
            BuildingKind.PokeMart => shop with { RoofColor = snowy ? SnowRoof : Rgb(66, 122, 222), Sign = SignKind.Mart, Accent = Rgb(58, 108, 214) },
            BuildingKind.Lab => shop with { RoofColor = Rgb(48, 178, 198), Sign = SignKind.Lab, Accent = Rgb(36, 150, 172) },
            BuildingKind.School => new BuildingStyle
            {
                Wall = WallKind.Brick, Roof = RoofShape.Gable, RoofColor = Rgb(92, 110, 156), Window = WindowKind.Sash,
                Sign = SignKind.School, Portal = 84, Accent = Rgb(74, 96, 150), Chimney = true
            },
            BuildingKind.Office => block with { Sign = SignKind.Poketch, Accent = Rgb(40, 150, 150), Gear = RoofGear.Vents | RoofGear.Skylight },
            BuildingKind.TvStation => block with { Storeys = 3, Sign = SignKind.Tv, Accent = Rgb(214, 72, 96), Gear = RoofGear.Vents | RoofGear.Mast | RoofGear.Dish },
            BuildingKind.Terminal => block with { Sign = SignKind.Globe, Accent = Rgb(70, 136, 232), Gear = RoofGear.Globe | RoofGear.Skylight },
            BuildingKind.Apartments => town switch
            {
                Architecture.Brick => brickBlock with { Balconies = false, Accent = Rgb(92, 94, 108) },
                Architecture.Townhouse => brickBlock with { Wall = WallKind.Plaster, Accent = Rgb(150, 98, 150) },
                _ => brickBlock
            },

            BuildingKind.Gym => shop with
            {
                RoofColor = snowy ? SnowRoof : GymColor(b.Theme), Accent = PixelCanvas.Shadow(GymColor(b.Theme), 0.12f),
                Sign = SignKind.Text, SignText = "GYM", Portal = 84
            },
            BuildingKind.Gate => new BuildingStyle
            {
                Wall = WallKind.Stone, Roof = RoofShape.Hip, RoofColor = Rgb(104, 146, 122), Window = WindowKind.Shop,
                GlassDoor = true, Accent = Rgb(84, 124, 104)
            },
            BuildingKind.Museum => block with { Wall = WallKind.Stone, Window = WindowKind.Arched, Accent = Rgb(120, 98, 84), Gear = RoofGear.Skylight },
            BuildingKind.Library => brickBlock with { Balconies = false, Home = false, GlassDoor = true, Accent = Rgb(84, 104, 140) },
            // A shop is its town's house with its name over the door; a store of several storeys is a block
            BuildingKind.Shop => b.Storeys >= 3
                ? block with { Accent = Rgb(214, 96, 72), Gear = RoofGear.Vents | RoofGear.Skylight }
                : house.Pitched ? house with { Home = false, Portal = 52, Chimney = false } : block with { Accent = house.Accent },
            BuildingKind.Hotel => House(Architecture.Resort, null) with { Home = false, Portal = 52, GlassDoor = true },
            BuildingKind.Hall => shop with { RoofColor = HallColor(b.Sign), Accent = PixelCanvas.Shadow(HallColor(b.Sign), 0.1f), Portal = 84 },
            BuildingKind.Chapel => new BuildingStyle
            {
                Wall = WallKind.Stone, Roof = RoofShape.Gable, RoofColor = Rgb(92, 104, 140), Storeys = 2, Window = WindowKind.Arched,
                RoseWindow = true, Steep = true, Gear = RoofGear.Spire, Accent = Rgb(92, 104, 140)
            },
            BuildingKind.Temple => new BuildingStyle
            {
                Wall = WallKind.OldStone, Roof = RoofShape.Flat, RoofColor = SnowRoof, Storeys = 2, Window = WindowKind.Arched,
                Accent = Rgb(120, 124, 142)
            },
            BuildingKind.Shrine => new BuildingStyle
            {
                Wall = WallKind.Boards, Roof = RoofShape.Hip, RoofColor = Rgb(120, 74, 66), Window = WindowKind.None,
                DeepEaves = true, Accent = Rgb(120, 74, 66)
            },
            BuildingKind.Tower => new BuildingStyle { Wall = WallKind.Stone, Roof = RoofShape.Flat, RoofColor = Rgb(92, 104, 140), Window = WindowKind.Slit, Accent = Rgb(92, 104, 140) },
            BuildingKind.Lighthouse => new BuildingStyle { Wall = WallKind.Stucco, Roof = RoofShape.Flat, RoofColor = Red, Window = WindowKind.Slit, Accent = Rgb(214, 72, 62) },
            BuildingKind.Factory => new BuildingStyle
            {
                Wall = WallKind.Metal, Roof = RoofShape.Flat, RoofColor = FlatGrey, Window = WindowKind.None, SlidingDoor = true,
                Accent = Rgb(88, 104, 132), Gear = b.Model == "d4_s01" ? RoofGear.Vents | RoofGear.Stack : RoofGear.Vents
            },
            // Snowpoint's harbour has a storehouse of logs under snow, with barn doors and a loft door with a hoist over them
            BuildingKind.Warehouse => new BuildingStyle
            {
                Wall = snowy ? WallKind.Log : WallKind.Metal, Roof = RoofShape.Gable, RoofColor = snowy ? SnowRoof : MetalRoof,
                Window = WindowKind.None, SlidingDoor = true, Loft = snowy, Accent = Rgb(88, 104, 132)
            },
            BuildingKind.Mansion => new BuildingStyle
            {
                // The Old Chateau in Eterna Forest is the same house gone to ruin: a roof of dark slate
                Wall = WallKind.Plaster, Roof = RoofShape.Hip, RoofColor = b.Model == "d3_s01" ? Rgb(62, 58, 76) : Rgb(88, 98, 126), Storeys = 2,
                Window = WindowKind.Arched, Portal = 84, Accent = b.Model == "d3_s01" ? Rgb(84, 70, 92) : Rgb(88, 98, 126)
            },
            BuildingKind.Galactic => new BuildingStyle
            {
                Wall = WallKind.DarkPanel, Roof = RoofShape.Flat, RoofColor = FlatGrey, Storeys = 3, Window = WindowKind.Slit, GlassDoor = true,
                Band = true, Sign = SignKind.Galactic, Accent = Rgb(232, 204, 76),
                Gear = b.Model == "c7_s03" ? RoofGear.Spikes | RoofGear.Dish : RoofGear.Spikes
            },
            BuildingKind.League => new BuildingStyle
            {
                Wall = WallKind.PaleStone, Roof = RoofShape.Flat, RoofColor = FlatGrey, Storeys = 3, Window = WindowKind.Arched, GlassDoor = true,
                Band = true, Sign = SignKind.Center, Accent = Rgb(196, 60, 70), Tiers = 2
            },
            _ => house
        };

        // Storeys: what the model's catalogue entry says; else by how tall the model stands; else the style's own
        // (a tower's stages stand on a ground stage of one storey)
        int storeys = b.Kind is BuildingKind.Tower or BuildingKind.Lighthouse ? 1
            : b.Storeys > 0 ? b.Storeys
            : b.Height <= 0f ? style.Storeys
            : style.Pitched ? (b.Height >= 5.4f ? Math.Max(2, style.Storeys) : style.Storeys)
            : Math.Clamp(1 + (int)MathF.Round((b.Height - 3.8f) / 1.95f), 1, 6);

        // A wing is the same walls and roof, plain: no entrance, no name, nothing on its roof but vents
        if (b.Annex)
            return style with
            {
                Storeys = 1, Sign = SignKind.None, SignText = null, Portal = 0, Chimney = false, RoseWindow = false, Tiers = 0,
                Gear = style.Gear & RoofGear.Vents, SlidingDoor = false
            };

        string? text = b.Sign ?? style.SignText;
        var sign = text != null && style.Sign == SignKind.None ? SignKind.Text : style.Sign;
        int portal = style.Portal;
        if (portal > 0 && sign == SignKind.Text) portal = Math.Min(PortalFor(text, portal), FrontWidth(b) - 8);
        return style with { Storeys = storeys, SignText = text, Sign = sign, Portal = portal };
    }
}
