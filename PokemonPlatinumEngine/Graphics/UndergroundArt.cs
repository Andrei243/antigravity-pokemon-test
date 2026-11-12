using System;
using System.Numerics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using Raylib_cs;
using static PokemonPlatinumEngine.Graphics.Pix;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// What stands in a secret base (plan 06 · R16a; style guide, "The Underground"): the PC with its flag, the big
/// boulders, and the hundred and thirty goods, each built with the field's art kit as the rooms' furniture is.
/// A good that is a piece of furniture the rooms have already (a table, a chair, a shelf, a television, a plant, a
/// bed) is that piece; the rest are plain pieces of our own by their kind: a doll, a cushion, a crystal, a cup, a
/// tent, a crate, a drum, a tool's plate in the floor. No drawing calls, so tests build them.
/// </summary>
internal static class UndergroundArt
{
    /// <summary>The start of the model's name of a prop of the base: <c>ug:good:23</c>, <c>ug:boulder</c>, <c>ug:pc:Bronze</c>.</summary>
    public const string ModelPrefix = "ug:";

    /// <summary>The kinds of good there are to draw.</summary>
    public enum GoodKind
    {
        Table, Chair, Cushion, Shelf, Television, Fridge, Sink, Bed, TrashCan, Box, Drum, Beam, Plant, Machine, Bike, Globe, Statue, Tent, Block, Tool,
        Cup, Crystal, Gem, Trophy, Doll, Sofa, Other
    }

    public static GoodKind KindOf(int good)
    {
        string id = UndergroundDatabase.Good(good)?.Id ?? "";
        bool Has(string word) => id.Contains(word, StringComparison.Ordinal);
        if (Has("_TOOL")) return GoodKind.Tool;
        if (Has("_DOLL")) return GoodKind.Doll;
        if (Has("TABLE")) return GoodKind.Table;
        if (Has("CHAIR")) return GoodKind.Chair;
        if (Has("CUSHION")) return GoodKind.Cushion;
        if (Has("SOFA")) return GoodKind.Sofa;
        if (Has("SHELF") || Has("CUPBOARD") || Has("DRESSER")) return GoodKind.Shelf;
        if (Has("UG_GOOD_TV")) return GoodKind.Television;
        if (Has("REFRIGERATOR") || Has("VENDING")) return GoodKind.Fridge;
        if (Has("SINK")) return GoodKind.Sink;
        if (Has("BED")) return GoodKind.Bed;
        if (Has("TRASH")) return GoodKind.TrashCan;
        if (Has("CARDBOARD") || Has("CRATE") || Has("CONTAINER")) return GoodKind.Box;
        if (Has("DRUM")) return GoodKind.Drum;
        if (Has("BEAM")) return GoodKind.Beam;
        if (Has("PLANT") || Has("FLOWER") || Has("BONSAI")) return GoodKind.Plant;
        if (Has("MACHINE") || Has("GAME_SYSTEM")) return GoodKind.Machine;
        if (Has("BIKE")) return GoodKind.Bike;
        if (Has("GLOBE") || Has("BINOCULARS")) return GoodKind.Globe;
        if (Has("STATUE")) return GoodKind.Statue;
        if (Has("TENT")) return GoodKind.Tent;
        if (Has("MAZE_BLOCK")) return GoodKind.Block;
        if (Has("_CUP")) return GoodKind.Cup;
        if (Has("CRYSTAL")) return GoodKind.Crystal;
        if (Has("_GEM") || Has("ORNAMENT")) return GoodKind.Gem;
        if (Has("TROPHY")) return GoodKind.Trophy;
        return GoodKind.Other;
    }

    /// <summary>A good's own colour, read from its name where it has one and from its place in the list where it hasn't.</summary>
    public static Color ColourOf(int good)
    {
        string id = UndergroundDatabase.Good(good)?.Id ?? "";
        bool Has(string word) => id.Contains(word, StringComparison.Ordinal);
        if (Has("YELLOW") || Has("GOLD") || Has("PIKACHU") || Has("CHIMCHAR") || Has("TORCHIC")) return Rgb(236, 190, 64);
        if (Has("BLUE") || Has("PIPLUP") || Has("SQUIRTLE") || Has("MUDKIP") || Has("TOTODILE") || Has("COOL") || Has("MANTYKE") || Has("WAILORD")) return Rgb(84, 140, 224);
        if (Has("RED") || Has("CHARMANDER") || Has("TOUGH") || Has("FIRE") || Has("EMBER")) return Rgb(220, 84, 70);
        if (Has("GREEN") || Has("TURTWIG") || Has("BULBASAUR") || Has("TREECKO") || Has("CHIKORITA") || Has("CLEVER") || Has("LEAF")) return Rgb(98, 178, 96);
        if (Has("PINK") || Has("CUTE") || Has("CLEFAIRY") || Has("JIGGLYPUFF") || Has("SKITTY") || Has("HAPPINY") || Has("FLOWER")) return Rgb(238, 140, 176);
        if (Has("SILVER") || Has("CLEAR") || Has("GLITTER") || Has("PLATINUM")) return Rgb(196, 204, 220);
        if (Has("BRONZE") || Has("BUNEARY") || Has("BONSLY")) return Rgb(190, 132, 84);
        if (Has("MYSTIC") || Has("DRIFLOON") || Has("BEAUTY") || Has("SMOKE")) return Rgb(150, 118, 200);
        Color[] others = { Rgb(214, 170, 96), Rgb(120, 170, 196), Rgb(196, 124, 110), Rgb(140, 176, 130), Rgb(180, 150, 190), Rgb(204, 196, 160) };
        return others[good % others.Length];
    }

    /// <summary>How a piece is drawn on the decorating screen's plan: its colour and a mark to tell it by.</summary>
    public static (Color Colour, string Mark) PlanLook(int good)
    {
        string id = UndergroundDatabase.Good(good)?.Id ?? "";
        if (UndergroundDatabase.IsStatue(good)) return (Rgb(166, 162, 158), "");
        if (id.StartsWith("UG_GOOD_PC", StringComparison.Ordinal)) return (Rgb(96, 118, 170), "PC");
        if (id == "UG_GOOD_BIG_BOULDER") return (Rgb(112, 104, 116), "");
        var kind = KindOf(good);
        var colour = kind switch
        {
            GoodKind.Table or GoodKind.Chair or GoodKind.Shelf or GoodKind.Box => Rgb(176, 128, 84),
            GoodKind.Plant => Rgb(96, 168, 96),
            GoodKind.Machine or GoodKind.Television or GoodKind.Fridge or GoodKind.Sink or GoodKind.Drum or GoodKind.Beam or GoodKind.TrashCan => Rgb(132, 148, 170),
            _ => ColourOf(good)
        };
        return (colour, "");
    }

    /// <summary>Builds a prop of the base where it stands.</summary>
    public static void Build(KitBuilder kit, Prop p, Map map)
    {
        string model = p.Model[ModelPrefix.Length..];
        kit.Origin = new Vector3(p.X, Relief.At(map, p.X + p.Width / 2f, p.Y + p.Depth / 2f), p.Y);
        int w = p.Width * 32, d = p.Depth * 32;
        if (model == "boulder")
        {
            // A big boulder: two rocks behind and one before them, each the field's own
            foreach (var (x, z, mirrored) in new[] { (17f, 24f, false), (46f, 26f, true), (32f, 46f, false) })
                kit.Sprite(kit.Face(mirrored ? "boulder.mirrored" : "boulder", 36, 30, c => OutdoorProps.PaintBoulder(c, mirrored)), x, z);
            return;
        }
        if (model.StartsWith("pc:", StringComparison.Ordinal))
        {
            // The PC on its desk, and beside it the flag the base has earned
            PropModels.BuildPc(kit, p.X, p.Y);
            kit.Origin = new Vector3(p.X, Relief.At(map, p.X + 1f, p.Y + 0.5f), p.Y);
            string flag = model[3..];
            var cloth = flag switch
            {
                "Bronze" => Rgb(190, 124, 72),
                "Silver" => Rgb(196, 204, 220),
                "Gold" => Rgb(240, 196, 64),
                "Platinum" => Rgb(170, 226, 226),
                _ => Rgb(220, 84, 70)
            };
            kit.Sprite(kit.Face($"ug.flag.{flag}", 22, 44, c => PaintFlag(c, cloth)), 48, 18);
            return;
        }
        if (!model.StartsWith("good:", StringComparison.Ordinal) || !int.TryParse(model[5..], out int good)) return;

        // A statue of the hideaways (plan 38 · EX6, part b): its species' figure in stone on a plinth with a plaque
        if (UndergroundDatabase.Statue(good) is { } statue)
        {
            var (shape, plaque) = StatueLook(statue.Species);
            kit.Sprite(kit.Face($"ug.statue.{good}", 24, 36, c => PaintStatue(c, shape, plaque)), w / 2f, d - 12);
            return;
        }
        var colour = ColourOf(good);
        var tone = Tone.Of(colour);
        var kind = KindOf(good);
        // The rooms' own furniture, where a good is one
        PropType? furniture = kind switch
        {
            GoodKind.Table => PropType.Table,
            GoodKind.Chair => PropType.Chair,
            GoodKind.Sofa => PropType.Sofa,
            GoodKind.Shelf => PropType.Bookshelf,
            GoodKind.Television => PropType.Television,
            GoodKind.Fridge => PropType.Fridge,
            GoodKind.Sink => PropType.KitchenCounter,
            GoodKind.Bed => PropType.Bed,
            GoodKind.TrashCan => PropType.TrashCan,
            GoodKind.Plant => PropType.Plant,
            GoodKind.Bike => PropType.Bicycle,
            _ => null
        };
        if (furniture is { } type)
        {
            // A piece made for one tile is stood on each tile of a wider good
            bool whole = type is PropType.Table or PropType.Sofa or PropType.Bed or PropType.Bicycle;
            if (whole) PropModels.Build(kit, new Prop { Type = type, X = p.X, Y = p.Y, Width = p.Width, Depth = p.Depth }, map);
            else
                for (int tx = 0; tx < p.Width; tx++)
                    PropModels.Build(kit, new Prop { Type = type, X = p.X + tx, Y = p.Y + p.Depth - 1 }, map);
            return;
        }
        switch (kind)
        {
            case GoodKind.Cushion:
                kit.Block($"ug.cushion.{good}", tone, 5, 27, 7, 27, 0, 5);
                break;
            case GoodKind.Box:
                kit.Block($"ug.box.{good}", Tone.Of(196, 150, 96), 3, w - 3, 5, d - 3, 0, 22);
                break;
            case GoodKind.Drum:
                for (int tx = 0; tx < p.Width; tx++)
                    kit.Sprite(kit.Face("ug.drum", 24, 34, c => PaintDrum(c, Rgb(84, 128, 150))), tx * 32 + 16, d - 12);
                break;
            case GoodKind.Beam:
                kit.Block("ug.beam", Tone.Of(150, 96, 84), 2, w - 2, 10, 22, 0, 8);
                break;
            case GoodKind.Machine:
                kit.Block($"ug.machine.{good}", Tone.Of(150, 164, 186), 3, w - 3, 6, d - 4, 0, 30);
                kit.Sprite(kit.Face($"ug.panel.{good}", 18, 10, c => PaintPanel(c, colour)), w / 2f, d - 3.5f, 14);
                break;
            case GoodKind.Tent:
                kit.Sprite(kit.Face($"ug.tent.{good}", Math.Min(w, 92), 60, c => PaintTent(c, tone)), w / 2f, d - 8);
                break;
            case GoodKind.Block:
                kit.Block("ug.maze", Tone.Of(150, 138, 150), 0, w, 0, d, 0, 30);
                break;
            case GoodKind.Tool:
                // A tool lies flat in the floor: a plate with a warning stripe
                kit.Decal(3, w - 3, 3, d - 3, 0.012f, kit.Face($"ug.tool.{good}", 26, 26, c => PaintPlate(c, colour)));
                break;
            case GoodKind.Statue:
                kit.Sprite(kit.Face("ug.statue", 22, 30, c => PaintOnStand(c, Tone.Of(150, 150, 164), round: false)), w / 2f, d - 12);
                break;
            case GoodKind.Globe:
                kit.Sprite(kit.Face($"ug.globe.{good}", 22, 34, c => PaintOnStand(c, Tone.Of(84, 140, 224), round: true)), 16, 20);
                break;
            case GoodKind.Cup or GoodKind.Trophy:
                kit.Sprite(kit.Face($"ug.cup.{good}", 20, 30, c => PaintCup(c, tone)), 16, 20);
                break;
            case GoodKind.Crystal or GoodKind.Gem:
                kit.Sprite(kit.Face($"ug.crystal.{good}", 22, 32, c => PaintCrystal(c, tone)), 16, 20);
                break;
            case GoodKind.Doll:
                kit.Sprite(kit.Face($"ug.doll.{good}", 24, 30, c => PaintDoll(c, tone)), w / 2f, d - 12);
                break;
            default:
                kit.Sprite(kit.Face($"ug.thing.{good}", 22, 30, c => PaintOnStand(c, tone, round: false)), w / 2f, d - 12);
                break;
        }
    }

    // ------------------------------------------------------------------ the pieces of our own, lit from the upper left

    private static void Disc(PixelCanvas c, float cx, float cy, float rx, float ry, Tone tone)
    {
        for (int y = (int)(cy - ry); y <= (int)(cy + ry); y++)
            for (int x = (int)(cx - rx); x <= (int)(cx + rx); x++)
            {
                float u = (x + 0.5f - cx) / rx, v = (y + 0.5f - cy) / ry;
                if (u * u + v * v > 1f) continue;
                c.Set(x, y, u + v < -0.5f ? tone.Light : u + v > 0.6f ? tone.Dark : tone.Base);
            }
    }

    /// <summary>The figures a statue may be (style guide, "The Underground", "A statue"): one to a kind of the Pokédex's shapes.</summary>
    public enum StatueFigure { FourLegs, Upright, Bird, Fish, Coil, Round, Tentacles }

    /// <summary>A statue's figure by its species' Pokédex shape, and its plaque in its first type's colour, darkened a third.</summary>
    public static (StatueFigure Figure, Color Plaque) StatueLook(string species)
    {
        var s = PokemonDatabase.Get(species);
        var figure = (s?.Shape ?? "").ToLowerInvariant() switch
        {
            "quadruped" or "armor" => StatueFigure.FourLegs,
            "wings" or "bug wings" => StatueFigure.Bird,
            "fish" => StatueFigure.Fish,
            "squiggle" => StatueFigure.Coil,
            "ball" or "blob" or "heads" => StatueFigure.Round,
            "tentacles" => StatueFigure.Tentacles,
            _ => StatueFigure.Upright
        };
        var type = s != null ? Models.Underground.Hideaways.TypesOf(s)[0] : PokemonType.Normal;
        var plaque = PixelCanvas.Shadow(Palette.GetTypeColor(type.ToString()), 0.33f);
        return (figure, plaque);
    }

    /// <summary>The stone's four greys and the plinth's slate (style guide, "A statue").</summary>
    private static readonly Tone Stone = new(Rgb(166, 162, 158), Rgb(206, 204, 198), Rgb(120, 116, 120), Rgb(84, 80, 90));
    private static readonly Color Slate = Rgb(96, 92, 104);

    /// <summary>A statue, 24 by 36: the figure of its shape in stone, standing on a slate plinth with a plaque across its front.</summary>
    public static void PaintStatue(PixelCanvas c, StatueFigure figure, Color plaque)
    {
        // The plinth: rows 27 to 35, two rows of light on its top, the plaque across its front
        c.Rect(3, 27, 18, 9, Slate);
        c.Rect(3, 27, 18, 2, PixelCanvas.Light1(Slate, 0.3f));
        c.Rect(19, 29, 2, 7, PixelCanvas.Shadow(Slate, 0.3f));
        c.Rect(6, 31, 12, 3, plaque);
        c.HLine(6, 31, 12, PixelCanvas.Light1(plaque, 0.25f));
        var t = Stone;
        switch (figure)
        {
            case StatueFigure.FourLegs:
                Disc(c, 12, 18, 8, 5, t);
                Disc(c, 18, 11, 4, 4, t);
                foreach (int x in new[] { 6, 9, 14, 17 }) c.Rect(x, 21, 2, 6, x < 12 ? t.Light : t.Dark);
                c.Rect(3, 14, 2, 3, t.Base);
                break;
            case StatueFigure.Bird:
                Disc(c, 12, 19, 5, 6, t);
                Disc(c, 12, 10, 4, 4, t);
                for (int i = 0; i < 6; i++)
                {
                    c.Rect(6 - i, 12 + i, 3, 2, t.Light);
                    c.Rect(15 + i, 12 + i, 3, 2, t.Dark);
                }
                c.Rect(15, 9, 3, 2, t.Dark);
                c.Rect(10, 24, 2, 3, t.Base);
                c.Rect(13, 24, 2, 3, t.Dark);
                break;
            case StatueFigure.Fish:
                // arched over the plinth, its tail high
                for (int x = 3; x < 20; x++)
                {
                    float u = (x - 11.5f) / 8.5f;
                    int top = (int)(14 + 8 * u * u), thick = (int)(7 - 4 * MathF.Abs(u));
                    c.Rect(x, top, 1, Math.Max(2, thick), x < 10 ? t.Light : x < 15 ? t.Base : t.Dark);
                }
                c.Rect(19, 16, 3, 6, t.Dark);
                c.Rect(5, 16, 2, 2, t.Deep);
                c.Rect(11, 21, 2, 6, t.Dark);
                break;
            case StatueFigure.Coil:
                Disc(c, 12, 23, 8, 4, t);
                Disc(c, 12, 18, 6, 3, t);
                Disc(c, 12, 13, 4, 3, t);
                Disc(c, 15, 7, 4, 3, t);
                break;
            case StatueFigure.Round:
                Disc(c, 12, 17, 9, 9, t);
                c.Rect(8, 14, 2, 2, t.Deep);
                c.Rect(14, 14, 2, 2, t.Deep);
                break;
            case StatueFigure.Tentacles:
                Disc(c, 12, 12, 8, 7, t);
                foreach (int x in new[] { 5, 9, 13, 17 })
                    for (int y = 17; y < 27; y++) c.Rect(x + ((y / 3) % 2), y, 2, 1, x < 11 ? t.Light : t.Dark);
                break;
            default:
                // upright: a body, a head, two arms and two legs
                Disc(c, 12, 18, 5, 6, t);
                Disc(c, 12, 8, 5, 5, t);
                c.Rect(5, 15, 3, 6, t.Light);
                c.Rect(16, 15, 3, 6, t.Dark);
                c.Rect(9, 23, 2, 4, t.Base);
                c.Rect(13, 23, 2, 4, t.Dark);
                break;
        }
        Outline(c);
    }

    public static void PaintFlag(PixelCanvas c, Color cloth)
    {
        var pole = Tone.Of(150, 150, 164);
        c.Rect(3, 2, 2, 40, pole.Base);
        c.VLine(3, 2, 40, pole.Light);
        c.Rect(1, 41, 6, 3, pole.Dark);
        var t = Tone.Of(cloth);
        for (int y = 0; y < 14; y++)
        {
            int len = 15 - Math.Abs(y - 7) / 2;
            c.Rect(5, 3 + y, len, 1, y < 3 ? t.Light : y > 10 ? t.Dark : t.Base);
        }
        Outline(c);
    }

    public static void PaintDrum(PixelCanvas c, Color colour)
    {
        var t = Tone.Of(colour);
        c.Rect(2, 4, 20, 28, t.Base);
        c.VLine(2, 4, 28, t.Light);
        c.Rect(18, 4, 4, 28, t.Dark);
        Disc(c, 12, 5, 10, 4, Tone.Of(PixelCanvas.Light1(colour, 0.2f)));
        c.HLine(2, 14, 20, t.Dark);
        c.HLine(2, 23, 20, t.Dark);
        Outline(c);
    }

    public static void PaintPanel(PixelCanvas c, Color light)
    {
        c.Rect(0, 0, 18, 10, Rgb(40, 48, 66));
        c.Rect(2, 2, 8, 6, light);
        c.Rect(12, 2, 4, 2, Rgb(236, 196, 80));
        c.Rect(12, 6, 4, 2, Rgb(110, 210, 140));
    }

    public static void PaintTent(PixelCanvas c, Tone tone)
    {
        int w = c.Width, h = c.Height;
        for (int y = 4; y < h; y++)
        {
            int half = (int)((y - 4) / (float)(h - 4) * (w / 2f - 2)) + 2;
            for (int x = w / 2 - half; x < w / 2 + half; x++)
                c.Set(x, y, x < w / 2 - half / 3 ? tone.Light : x > w / 2 + half / 3 ? tone.Dark : tone.Base);
        }
        // The way in: a dark flap in the middle
        for (int y = h - 26; y < h; y++)
        {
            int half = (y - (h - 26)) / 3 + 1;
            c.Rect(w / 2 - half, y, half * 2, 1, Rgb(54, 44, 60));
        }
        Outline(c);
    }

    public static void PaintPlate(PixelCanvas c, Color colour)
    {
        var steel = Tone.Of(120, 124, 140);
        c.Rect(0, 0, 26, 26, steel.Base);
        c.HLine(0, 0, 26, steel.Light);
        c.VLine(0, 0, 26, steel.Light);
        c.HLine(0, 25, 26, steel.Dark);
        c.VLine(25, 0, 26, steel.Dark);
        for (int i = 0; i < 26; i++)
            if (i / 4 % 2 == 0) c.Rect(i, 11, 1, 4, colour);
        foreach (var (x, y) in new[] { (2, 2), (22, 2), (2, 22), (22, 22) }) c.Rect(x, y, 2, 2, steel.Deep);
    }

    public static void PaintOnStand(PixelCanvas c, Tone tone, bool round)
    {
        var stand = Tone.Of(132, 100, 76);
        c.Rect(9, 22, 4, 6, stand.Base);
        c.Rect(5, 27, 12, 3, stand.Dark);
        c.HLine(5, 27, 12, stand.Light);
        if (round) Disc(c, 11, 12, 10, 10, tone);
        else
        {
            c.Rect(3, 4, 16, 18, tone.Base);
            c.VLine(3, 4, 18, tone.Light);
            c.HLine(3, 4, 16, tone.Light);
            c.Rect(16, 5, 3, 17, tone.Dark);
        }
        Outline(c);
    }

    public static void PaintCup(PixelCanvas c, Tone tone)
    {
        for (int y = 2; y < 16; y++)
        {
            int half = 8 - (y - 2) / 3;
            c.Rect(10 - half, y, half * 2, 1, tone.Base);
            c.Set(10 - half, y, tone.Light);
            c.Set(10 + half - 1, y, tone.Dark);
        }
        c.HLine(2, 2, 16, tone.Light);
        c.Rect(9, 16, 2, 7, tone.Dark);
        c.Rect(5, 23, 10, 3, tone.Base);
        c.HLine(5, 23, 10, tone.Light);
        c.Rect(4, 26, 12, 3, tone.Dark);
        Outline(c);
    }

    public static void PaintCrystal(PixelCanvas c, Tone tone)
    {
        // Three shards rising from a foot of rock
        foreach (var (cx, top, half) in new[] { (11, 2, 5), (5, 12, 3), (17, 10, 3) })
            for (int y = top; y < 26; y++)
            {
                int wide = Math.Min(half, (y - top) / 2 + 1);
                for (int x = cx - wide; x <= cx + wide; x++)
                    c.Set(x, y, x < cx ? tone.Light : x == cx ? tone.Base : tone.Dark);
            }
        var rock = Tone.Of(110, 104, 116);
        c.Rect(2, 26, 18, 5, rock.Base);
        c.HLine(2, 26, 18, rock.Light);
        c.HLine(2, 30, 18, rock.Dark);
        Outline(c);
    }

    public static void PaintDoll(PixelCanvas c, Tone tone)
    {
        // A stuffed toy: a round body, a round head with two ears, two dots for eyes
        Disc(c, 12, 21, 9, 8, tone);
        Disc(c, 12, 10, 8, 7, tone);
        Disc(c, 5, 4, 3, 3, tone);
        Disc(c, 19, 4, 3, 3, tone);
        var eye = Rgb(40, 36, 52);
        c.Rect(8, 9, 2, 2, eye);
        c.Rect(14, 9, 2, 2, eye);
        c.Rect(10, 19, 5, 4, PixelCanvas.Light1(tone.Base, 0.45f));
        Outline(c);
    }
}
