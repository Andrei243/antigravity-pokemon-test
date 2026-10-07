using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Furniture and wall decorations for rooms (style guide, "Rooms"): boxes whose every visible face is painted
/// pixel art, with plants and vases as sprites on cards. Measured in texels from the north-west corner of the
/// prop's first tile: x east, z south, heights in screen rows. No GPU calls.
/// </summary>
internal static class PropModels
{
    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    private static readonly Tone Wood = Tone.Of(184, 126, 78);
    private static readonly Tone WoodLight = Tone.Of(214, 160, 104);
    private static readonly Tone WoodDark = Tone.Of(128, 86, 56);
    private static readonly Tone Enamel = Tone.Of(236, 238, 244, 250, 250, 252, 196, 200, 214);
    private static readonly Tone Cream = Tone.Of(236, 228, 208, 248, 242, 226, 200, 188, 164);
    private static readonly Tone Steel = Tone.Of(196, 200, 210, 226, 230, 238, 146, 152, 170);
    private static readonly Tone Casing = Tone.Of(186, 192, 210, 214, 220, 234, 138, 146, 170);
    private static readonly Tone Dark = Tone.Of(66, 68, 84, 98, 102, 122, 44, 44, 58);
    private static readonly Tone RedFabric = Tone.Of(214, 96, 84);
    private static readonly Tone BlueFabric = Tone.Of(116, 150, 216);
    private static readonly Tone CenterRed = Tone.Of(226, 92, 100);
    private static readonly Tone MartBlue = Tone.Of(80, 128, 216);
    private static readonly Tone LabBlue = Tone.Of(150, 160, 186);
    private static readonly Color Ink = Rgb(52, 44, 62);
    private static readonly Color ScreenBlue = Rgb(96, 166, 236);
    private static readonly Color ScreenDeep = Rgb(52, 110, 206);
    private static readonly Color ScreenLine = Rgb(190, 230, 252);
    private static readonly Color Leaf = Rgb(60, 146, 76);
    private static readonly Color LeafLight = Rgb(106, 196, 98);
    private static readonly Color LeafDark = Rgb(36, 110, 62);

    /// <summary>Height of a room's walls in texels.</summary>
    public const int WallHeight = 80;

    public static void Build(KitBuilder kit, Prop p, Map map)
    {
        kit.Origin = new Vector3(p.X, 0, p.Y);
        int w = p.Width * 32, d = p.Depth * 32;
        switch (p.Type)
        {
            case PropType.Table: Table(kit, w, map.Interior == InteriorStyle.House); break;
            case PropType.Chair: Chair(kit, p, map); break;
            case PropType.Sofa: Sofa(kit, p, map, w, d); break;
            case PropType.Bookshelf: Bookshelf(kit); break;
            case PropType.Television: Television(kit); break;
            case PropType.Plant: kit.Sprite(kit.Face("plant", 28, 46, PaintPlant), 16, 18); break;
            case PropType.Fridge: Fridge(kit); break;
            case PropType.KitchenCounter: Kitchen(kit, stove: false); break;
            case PropType.Stove: Kitchen(kit, stove: true); break;
            case PropType.Stairs: Stairs(kit, d); break;
            case PropType.Counter: Counter(kit, w, d, map.Interior == InteriorStyle.PokemonCenter); break;
            case PropType.HealingMachine: HealingMachine(kit); break;
            case PropType.Bench: Bench(kit, w); break;
            case PropType.StoreShelf: StoreShelf(kit, w, againstWall: p.Y <= 2); break;
            case PropType.LabDesk: LabDesk(kit, w); break;
            case PropType.Bed: Bed(kit, d); break;
            case PropType.Computer: BuildPc(kit, p.X, p.Y); break;
            case PropType.LabMachine: LabMachine(kit, d, facesEast: p.X <= 1, facesWest: p.X + p.Width >= map.Width - 1); break;
            case PropType.Rug: Rug(kit, w, d, map.Interior == InteriorStyle.PokemonCenter); break;
            case PropType.Window: Window(kit, w); break;
            case PropType.Painting: kit.Card(3, 29, 32.5f, 40, 60, kit.Face("painting", 26, 20, PaintPainting)); break;
            case PropType.Clock: kit.Card(8, 24, 32.5f, 50, 66, kit.Face("clock", 16, 16, PaintClock)); break;
            // A Gym's statues by its door (plan 02 · S5) are the statues the world's towns have
            case PropType.Statue: Landmarks.Add(kit, map, p, new MeshBuilder()); break;
            case PropType.WallEmblem:
                kit.Card(1, 31, 32.5f, 36, 66, kit.Face("emblem", 30, 30, c => BuildingArt.BallRoundel(c, 0, 0, 30)));
                break;
        }
    }

    // ------------------------------------------------------------------ kitchen

    private static void Fridge(KitBuilder kit)
    {
        var front = kit.Face("fridge.front", 26, 58, c =>
        {
            Pix.Raised(c, 0, 0, 26, 58, Enamel);
            // The freezer door above, the fridge door below, each with a handle
            c.HLine(1, 18, 24, Enamel.Dark);
            c.HLine(1, 19, 24, Enamel.Light);
            foreach (var (y, h) in new[] { (6, 8), (24, 14) })
            {
                c.VLine(20, y, h, Steel.Light);
                c.VLine(21, y, h, Steel.Dark);
            }
            c.Rect(1, 53, 24, 4, Steel.Dark);
            c.HLine(1, 53, 24, Steel.Base);
        });
        var top = kit.Face("fridge.top", 26, 22, c => TopFace(c, Enamel));
        var side = kit.Face("fridge.side", 22, 58, c => Pix.Raised(c, 0, 0, 22, 58, Tone.Of(Enamel.Dark)));
        kit.Box(3, 29, 0, 22, 0, 58, top, front, side, side);
    }

    /// <summary>A kitchen unit a tile wide: cupboards under a worktop with a sink, or an oven under a hob.</summary>
    private static void Kitchen(KitBuilder kit, bool stove)
    {
        string name = stove ? "stove" : "kitchen";
        var front = kit.Face(name + ".front", 32, 27, c =>
        {
            Pix.Raised(c, 0, 0, 32, 27, Cream);
            if (stove)
            {
                // Knobs, a handle bar and the oven's dark glass
                foreach (int x in new[] { 5, 11, 19, 25 }) c.Rect(x, 2, 2, 2, Dark.Base);
                c.Rect(5, 6, 22, 2, Steel.Base);
                c.HLine(5, 6, 22, Steel.Light);
                Pix.Sunken(c, 4, 10, 24, 12, Dark);
                c.HLine(7, 13, 8, Dark.Light);
            }
            else
            {
                foreach (int x in new[] { 2, 17 })
                {
                    Pix.Raised(c, x, 3, 13, 19, Tone.Of(Cream.Dark));
                    Pix.Sunken(c, x + 2, 5, 9, 15, Tone.Of(Cream.Dark));
                }
                c.Rect(12, 10, 2, 5, Steel.Dark);
                c.Rect(18, 10, 2, 5, Steel.Dark);
            }
            c.Rect(1, 24, 30, 2, Cream.Dark);
        });
        var side = kit.Face("kitchen.side", 20, 27, c => Pix.Raised(c, 0, 0, 20, 27, Tone.Of(Cream.Dark)));
        kit.Box(0, 32, 0, 20, 0, 27, null, front, side, side);

        var top = kit.Face(name + ".top", 32, 22, c =>
        {
            TopFace(c, Steel);
            if (stove)
            {
                c.Rect(3, 3, 26, 15, Dark.Base);
                foreach (var (x, y) in new[] { (5, 4), (20, 4), (5, 11), (20, 11) })
                {
                    Pix.Disc(c, x, y, 6, Dark.Light);
                    Pix.Disc(c, x + 1, y + 1, 4, Dark.Dark);
                }
            }
            else
            {
                // A sink with its tap against the wall
                Pix.Sunken(c, 6, 5, 20, 12, Tone.Of(150, 160, 180));
                c.Rect(15, 9, 2, 2, Dark.Base);
                c.Rect(14, 1, 4, 3, Steel.Light);
                c.Rect(15, 4, 2, 4, Steel.Light);
            }
        });
        var edge = kit.Face("worktop.front", 32, 3, c => Edge(c, Steel));
        var edgeSide = kit.Face("worktop.side", 22, 3, c => Edge(c, Steel));
        kit.Box(0, 32, 0, 22, 27, 30, top, edge, edgeSide, edgeSide);
    }

    // ------------------------------------------------------------------ living room

    private static void Television(KitBuilder kit)
    {
        var stand = kit.Face("tv.stand", 26, 14, c =>
        {
            Pix.Raised(c, 0, 0, 26, 14, Wood);
            Pix.Sunken(c, 3, 3, 20, 8, Wood);
            c.Rect(12, 6, 2, 2, WoodLight.Light);
        });
        var standTop = kit.Face("tv.stand.top", 26, 16, c => TopFace(c, WoodLight));
        var standSide = kit.Face("tv.stand.side", 16, 14, c => Pix.Raised(c, 0, 0, 16, 14, WoodDark));
        kit.Box(3, 29, 2, 18, 0, 14, standTop, stand, standSide, standSide);

        var screen = kit.Face("tv.front", 22, 20, c =>
        {
            Pix.Raised(c, 0, 0, 22, 20, Dark);
            // A sunny hillside with something yellow sitting on it
            c.Rect(2, 2, 18, 8, Rgb(128, 196, 250));
            c.Rect(2, 10, 18, 5, Rgb(98, 184, 110));
            Pix.Disc(c, 8, 6, 6, Rgb(250, 214, 76));
            c.Rect(8, 4, 2, 3, Rgb(250, 214, 76));
            c.Rect(12, 4, 2, 3, Rgb(250, 214, 76));
            c.Rect(16, 17, 2, 1, Rgb(98, 220, 120));
            c.Rect(12, 17, 2, 1, Dark.Light);
        });
        var setTop = kit.Face("tv.top", 22, 10, c => TopFace(c, Dark));
        var setSide = kit.Face("tv.side", 10, 20, c => Pix.Raised(c, 0, 0, 10, 20, Dark));
        kit.Box(5, 27, 4, 14, 14, 34, setTop, screen, setSide, setSide);
    }

    private static void Bookshelf(KitBuilder kit)
    {
        var front = kit.Face("bookshelf.front", 30, 52, c =>
        {
            Pix.Raised(c, 0, 0, 30, 52, WoodDark);
            c.Rect(2, 2, 26, 48, Rgb(74, 52, 44));
            Color[] spines = { Rgb(200, 66, 66), Rgb(66, 114, 200), Rgb(76, 160, 96), Rgb(230, 190, 76), Rgb(150, 96, 176), Rgb(236, 232, 220) };
            int[] widths = { 3, 2, 4, 3, 3, 2, 4, 3, 2, 3 }, heights = { 12, 14, 11, 13, 12, 14, 10, 13 };
            for (int shelf = 0; shelf < 3; shelf++)
            {
                int floor = 17 + shelf * 16;
                c.HLine(2, floor, 26, WoodDark.Light);
                c.HLine(2, floor + 1, 26, WoodDark.Dark);
                int x = 3;
                for (int i = 0; x + widths[(i + shelf * 3) % widths.Length] <= 27; i++)
                {
                    int bw = widths[(i + shelf * 3) % widths.Length], bh = heights[(i * 3 + shelf) % heights.Length];
                    var col = spines[(i * 2 + shelf * 5 + i / 3) % spines.Length];
                    c.Rect(x, floor - bh, bw, bh, col);
                    c.VLine(x, floor - bh, bh, PixelCanvas.Light1(col, 0.35f));
                    c.HLine(x, floor - bh + 3, bw, PixelCanvas.Shadow(col, 0.3f));
                    // Now and then a gap where a book has been taken out
                    x += bw + ((i + shelf) % 4 == 3 ? 2 : 0);
                }
            }
        });
        var top = kit.Face("bookshelf.top", 30, 14, c => TopFace(c, Wood));
        var side = kit.Face("bookshelf.side", 14, 52, c => Pix.Raised(c, 0, 0, 14, 52, WoodDark));
        kit.Box(1, 31, 0, 14, 0, 52, top, front, side, side);
    }

    private static void Table(KitBuilder kit, int w, bool home)
    {
        int tw = w - 6;
        var top = kit.Face($"table.top.{tw}.{home}", tw, 22, c =>
        {
            TopFace(c, WoodLight);
            for (int y = 7; y < 20; y += 7) c.HLine(1, y, tw - 2, WoodLight.Dark);
            if (home)
            {
                // A lace runner down the middle
                c.Rect(8, 5, tw - 16, 12, Rgb(250, 246, 236));
                Pix.Border(c, 8, 5, tw - 16, 12, Rgb(226, 214, 196));
            }
            else
            {
                // An open book and a sheet of notes
                c.Rect(7, 6, 12, 9, Rgb(250, 250, 250));
                c.VLine(13, 6, 9, Rgb(200, 204, 216));
                c.HLine(8, 8, 4, Rgb(160, 166, 184));
                c.HLine(8, 11, 4, Rgb(160, 166, 184));
                c.HLine(15, 9, 3, Rgb(160, 166, 184));
                c.Rect(tw - 17, 8, 9, 8, Rgb(244, 238, 216));
                c.HLine(tw - 15, 11, 5, Rgb(170, 160, 140));
            }
        });
        var edge = kit.Face($"table.edge.{tw}", tw, 3, c => Edge(c, WoodLight));
        var edgeSide = kit.Face("table.edge.side", 22, 3, c => Edge(c, Wood));
        kit.Box(3, w - 3, 5, 27, 20, 23, top, edge, edgeSide, edgeSide);
        foreach (int x in new[] { 5, w - 8 })
            foreach (int z in new[] { 7, 22 })
                kit.Block("table.leg", WoodDark, x, x + 3, z, z + 3, 0, 20);
        if (home) kit.Sprite(kit.Face("vase", 12, 16, PaintVase), w / 2f, 16, 23);
    }

    private static void Chair(KitBuilder kit, Prop p, Map map)
    {
        kit.Block("chair.seat", WoodLight, 9, 23, 10, 24, 12, 14);
        foreach (int x in new[] { 9, 21 })
            foreach (int z in new[] { 10, 22 })
                kit.Block("chair.leg", WoodDark, x, x + 2, z, z + 2, 0, 12);

        // The back is on the side away from the table next to it
        bool TableAt(int x, int y) => map.Props.Exists(o => o.Type == PropType.Table && o.Covers(x, y));
        if (TableAt(p.X + 1, p.Y)) kit.Block("chair.back", Wood, 9, 11, 10, 24, 14, 28);
        else if (TableAt(p.X - 1, p.Y)) kit.Block("chair.back", Wood, 21, 23, 10, 24, 14, 28);
        else if (TableAt(p.X, p.Y - 1)) kit.Block("chair.back", Wood, 9, 23, 22, 24, 14, 28);
        else kit.Block("chair.back", Wood, 9, 23, 10, 12, 14, 28);
    }

    private static void Sofa(KitBuilder kit, Prop p, Map map, int w, int d)
    {
        var shade = Tone.Of(RedFabric.Dark);
        kit.Block("sofa.base", shade, 3, w - 3, 3, d - 3, 0, 9);

        // Two seat cushions with a seam between them
        int cw = w - 6, cd = d - 6;
        var cushions = kit.Face($"sofa.cushions.{cw}x{cd}", cw, cd, c =>
        {
            TopFace(c, RedFabric);
            if (cd > cw) { c.HLine(1, cd / 2, cw - 2, RedFabric.Dark); c.HLine(1, cd / 2 + 1, cw - 2, RedFabric.Light); }
            else { c.VLine(cw / 2, 1, cd - 2, RedFabric.Dark); c.VLine(cw / 2 + 1, 1, cd - 2, RedFabric.Light); }
        });
        var cushionFront = kit.Face($"sofa.cushion.front.{cw}", cw, 4, c => Edge(c, RedFabric));
        var cushionSide = kit.Face($"sofa.cushion.side.{cd}", cd, 4, c => Edge(c, RedFabric));
        kit.Box(3, w - 3, 3, d - 3, 9, 13, cushions, cushionFront, cushionSide, cushionSide);

        // The back stands against the wall the sofa is beside; the arms close its two ends
        bool west = p.X <= 1, east = p.X + p.Width >= map.Width - 1;
        if (west) kit.Block("sofa.back", RedFabric, 3, 9, 3, d - 3, 0, 26);
        else if (east) kit.Block("sofa.back", RedFabric, w - 9, w - 3, 3, d - 3, 0, 26);
        else kit.Block("sofa.back", RedFabric, 3, w - 3, 3, 9, 0, 26);
        if (west || east)
        {
            kit.Block("sofa.arm", RedFabric, 3, w - 3, 3, 8, 0, 18);
            kit.Block("sofa.arm", RedFabric, 3, w - 3, d - 8, d - 3, 0, 18);
        }
        else
        {
            kit.Block("sofa.arm", RedFabric, 3, 8, 3, d - 3, 0, 18);
            kit.Block("sofa.arm", RedFabric, w - 8, w - 3, 3, d - 3, 0, 18);
        }
    }

    /// <summary>Eight wooden steps climbing toward the back wall, with a newel post at the foot.</summary>
    private static void Stairs(KitBuilder kit, int d)
    {
        const int steps = 8;
        int run = d / steps;
        var tread = kit.Face("stairs.tread", 26, run, c =>
        {
            c.Rect(0, 0, 26, run, WoodLight.Base);
            c.HLine(0, run - 1, 26, WoodLight.Light);
            c.HLine(0, 0, 26, WoodLight.Dark);
        });
        var riser = kit.Face("stairs.riser", 26, 9, c => Pix.Raised(c, 0, 0, 26, 9, Wood));
        for (int i = 0; i < steps; i++)
        {
            int z1 = d - i * run, top = (i + 1) * 9;
            var side = kit.Face($"stairs.side.{top}", run, top, c => Pix.Raised(c, 0, 0, run, top, WoodDark));
            kit.Box(3, 29, z1 - run, z1, 0, top, tread, null, side, null);
            kit.Box(3, 29, z1 - run, z1, top - 9, top, null, riser);
            // A banister that climbs with the steps (and stops under the top of the wall)
            kit.Block("stairs.rail", WoodDark, 29, 31, z1 - run, z1, top, Math.Min(top + 16, WallHeight - 2));
        }
        kit.Block("stairs.newel", WoodDark, 1, 4, d - 4, d - 1, 0, 30);
    }

    // ------------------------------------------------------------------ shops and Centers

    /// <summary>A service counter: a panelled body in the building's colour under a pale top with a tray on it.</summary>
    private static void Counter(KitBuilder kit, int w, int d, bool center)
    {
        var accent = center ? CenterRed : MartBlue;
        string name = center ? "counter.center" : "counter.mart";
        bool lengthwise = d > w;
        int x0 = lengthwise ? 5 : 1, x1 = w - x0, z0 = lengthwise ? 1 : 5, z1 = d - z0;

        Art Panel(int width, bool emblem) => kit.Face($"{name}.panel.{width}.{emblem}", width, 24, c =>
        {
            Pix.Raised(c, 0, 0, width, 24, accent);
            c.Rect(1, 13, width - 2, 2, Rgb(250, 250, 252));
            c.Rect(1, 21, width - 2, 2, accent.Dark);
            for (int x = 16; x < width - 4; x += 32) { c.VLine(x, 1, 20, accent.Dark); c.VLine(x + 1, 1, 20, accent.Light); }
            if (emblem) BuildingArt.BallRoundel(c, width / 2 - 8, 3, 16);
        });
        kit.Box(x0, x1, z0, z1, 0, 24, null, Panel(x1 - x0, center && !lengthwise), Panel(z1 - z0, false), Panel(z1 - z0, false));

        int tw = x1 - x0 + 4, td = z1 - z0 + 4;
        var top = kit.Face($"{name}.top.{tw}x{td}", tw, td, c =>
        {
            TopFace(c, Enamel);
            // A tray where things are handed over
            if (lengthwise) Pix.Sunken(c, 6, td / 2 - 10, tw - 12, 20, Tone.Of(Enamel.Dark));
            else Pix.Sunken(c, tw / 2 - 16, 6, 32, td - 12, Tone.Of(Enamel.Dark));
        });
        var edge = kit.Face($"{name}.edge.{tw}", tw, 3, c => Edge(c, Enamel));
        var edgeSide = kit.Face($"{name}.edge.{td}", td, 3, c => Edge(c, Enamel));
        kit.Box(x0 - 2, x1 + 2, z0 - 2, z1 + 2, 24, 27, top, edge, edgeSide, edgeSide);
    }

    /// <summary>The machine behind a Center's counter: six Poké Balls in a tray over a console.</summary>
    private static void HealingMachine(KitBuilder kit)
    {
        var front = kit.Face("healer.front", 28, 28, c =>
        {
            Pix.Raised(c, 0, 0, 28, 28, Casing);
            Screen(c, 3, 4, 14, 10);
            c.Rect(20, 5, 2, 2, Rgb(236, 70, 70));
            c.Rect(23, 5, 2, 2, Rgb(250, 208, 70));
            c.Rect(20, 9, 2, 2, Rgb(98, 220, 120));
            Pix.Sunken(c, 3, 18, 22, 6, Casing);
        });
        var top = kit.Face("healer.top", 28, 22, c =>
        {
            TopFace(c, Casing);
            Pix.Sunken(c, 2, 2, 24, 18, Tone.Of(Casing.Dark));
            for (int i = 0; i < 6; i++)
            {
                int x = 4 + i % 3 * 7, y = 4 + i / 3 * 8;
                Pix.Disc(c, x, y, 6, Ink);
                c.Rect(x + 1, y + 1, 4, 2, Rgb(236, 64, 56));
                c.Rect(x + 1, y + 3, 4, 2, Rgb(250, 250, 252));
            }
        });
        var side = kit.Face("healer.side", 22, 28, c => Pix.Raised(c, 0, 0, 22, 28, Tone.Of(Casing.Dark)));
        kit.Box(2, 30, 0, 22, 0, 28, top, front, side, side);
    }

    /// <summary>A waiting-room bench: a cushioned seat and back on steel legs.</summary>
    private static void Bench(KitBuilder kit, int w)
    {
        foreach (int x in new[] { 6, w - 9 }) kit.Block("bench.leg", Steel, x, x + 3, 12, 20, 0, 9);
        int sw = w - 6;
        var seat = kit.Face($"bench.seat.{sw}", sw, 16, c =>
        {
            TopFace(c, BlueFabric);
            c.VLine(sw / 2, 1, 14, BlueFabric.Dark);
            c.VLine(sw / 2 + 1, 1, 14, BlueFabric.Light);
        });
        var seatFront = kit.Face($"bench.seat.front.{sw}", sw, 4, c => Edge(c, BlueFabric));
        var seatSide = kit.Face("bench.seat.side", 16, 4, c => Edge(c, BlueFabric));
        kit.Box(3, w - 3, 8, 24, 9, 13, seat, seatFront, seatSide, seatSide);

        var back = kit.Face($"bench.back.{sw}", sw, 14, c =>
        {
            Pix.Raised(c, 0, 0, sw, 14, BlueFabric);
            c.VLine(sw / 2, 1, 12, BlueFabric.Dark);
            c.VLine(sw / 2 + 1, 1, 12, BlueFabric.Light);
        });
        var backTop = kit.Face($"bench.back.top.{sw}", sw, 3, c => TopFace(c, BlueFabric));
        var backSide = kit.Face("bench.back.side", 3, 14, c => Pix.Raised(c, 0, 0, 3, 14, BlueFabric));
        kit.Box(3, w - 3, 5, 8, 13, 27, backTop, back, backSide, backSide);
    }

    /// <summary>A shop shelf: three rows of goods in a steel frame.</summary>
    private static void StoreShelf(KitBuilder kit, int w, bool againstWall)
    {
        int sw = w - 2, z0 = againstWall ? 0 : 6, z1 = againstWall ? 20 : 26;
        var front = kit.Face($"shelf.front.{sw}", sw, 44, c =>
        {
            Pix.Raised(c, 0, 0, sw, 44, Steel);
            c.Rect(2, 2, sw - 4, 40, Rgb(86, 92, 112));
            Color[] goods = { Rgb(232, 84, 84), Rgb(84, 150, 232), Rgb(250, 206, 76), Rgb(120, 200, 110), Rgb(236, 236, 240), Rgb(170, 104, 200) };
            for (int shelf = 0; shelf < 3; shelf++)
            {
                int floor = 14 + shelf * 13;
                c.HLine(2, floor, sw - 4, Steel.Light);
                c.HLine(2, floor + 1, sw - 4, Steel.Dark);
                for (int i = 0; 3 + i * 6 + 5 <= sw - 3; i++)
                {
                    // Boxes, tins and bottles in clusters of a kind
                    int kind = (i / 2 + shelf) % 3, x = 3 + i * 6;
                    var col = goods[(i / 2 * 2 + shelf * 3) % goods.Length];
                    int h = kind == 0 ? 9 : kind == 1 ? 6 : 10, bw = kind == 2 ? 3 : 5;
                    c.Rect(x, floor - h, bw, h, col);
                    c.HLine(x, floor - h, bw, PixelCanvas.Light1(col, 0.4f));
                    c.VLine(x + bw - 1, floor - h + 1, h - 1, PixelCanvas.Shadow(col, 0.3f));
                    if (kind == 2) c.Rect(x + 1, floor - h - 2, 1, 2, PixelCanvas.Shadow(col, 0.3f));
                    else if (h >= 9) c.Rect(x + 1, floor - h + 3, bw - 2, 3, Rgb(250, 250, 252));
                }
            }
        });
        var top = kit.Face($"shelf.top.{sw}", sw, z1 - z0, c => TopFace(c, Steel));
        var side = kit.Face($"shelf.side.{z1 - z0}", z1 - z0, 44, c => Pix.Raised(c, 0, 0, z1 - z0, 44, Tone.Of(Steel.Dark)));
        kit.Box(1, w - 1, z0, z1, 0, 44, top, front, side, side);
    }

    // ------------------------------------------------------------------ laboratories

    /// <summary>A long desk: drawers either side of a knee hole, a pale top with a monitor, a keyboard and papers.</summary>
    private static void LabDesk(KitBuilder kit, int w)
    {
        int bw = w - 2;
        var front = kit.Face($"labdesk.front.{bw}", bw, 22, c =>
        {
            Pix.Raised(c, 0, 0, bw, 22, LabBlue);
            foreach (int x in new[] { 2, bw - 30 })
                for (int row = 0; row < 3; row++)
                {
                    Pix.Sunken(c, x, 2 + row * 6, 28, 6, LabBlue);
                    c.Rect(x + 12, 4 + row * 6, 4, 2, Steel.Light);
                }
            if (bw > 80) c.Rect(34, 3, bw - 68, 18, LabBlue.Deep);
        });
        var side = kit.Face("labdesk.side", 22, 22, c => Pix.Raised(c, 0, 0, 22, 22, Tone.Of(LabBlue.Dark)));
        kit.Box(1, w - 1, 5, 27, 0, 22, null, front, side, side);

        int tw = w + 2;
        var top = kit.Face($"labdesk.top.{tw}", tw, 26, c =>
        {
            TopFace(c, Enamel);
            // A blotter, loose papers, a book, and a keyboard in front of the monitor
            Pix.Raised(c, 10, 8, 26, 14, Tone.Of(96, 168, 120));
            c.Rect(42, 6, 10, 13, Rgb(250, 250, 250));
            c.HLine(44, 9, 6, Rgb(160, 166, 184));
            c.HLine(44, 12, 6, Rgb(160, 166, 184));
            c.HLine(44, 15, 4, Rgb(160, 166, 184));
            if (tw > 100)
            {
                Pix.Raised(c, 58, 9, 12, 9, Tone.Of(200, 66, 66));
                Pix.Raised(c, tw - 46, 16, 26, 7, Steel);
                for (int x = tw - 44; x < tw - 22; x += 3) c.Rect(x, 18, 2, 3, Steel.Dark);
            }
        });
        var edge = kit.Face($"labdesk.edge.{tw}", tw, 3, c => Edge(c, Enamel));
        var edgeSide = kit.Face("labdesk.edge.side", 26, 3, c => Edge(c, Enamel));
        kit.Box(-1, w + 1, 3, 29, 22, 25, top, edge, edgeSide, edgeSide);

        if (w > 100) Monitor(kit, w - 44, 7, 25);
    }

    /// <summary>A tall cabinet of instruments two tiles deep; its console faces into the room.</summary>
    private static void LabMachine(KitBuilder kit, int d, bool facesEast, bool facesWest)
    {
        int cd = d - 4;
        Art Console(int width) => kit.Face($"machine.console.{width}", width, 56, c =>
        {
            Pix.Raised(c, 0, 0, width, 56, Casing);
            // A screen of readings, rows of lamps, then a keyboard shelf and vents
            Pix.Sunken(c, 4, 4, width - 8, 18, Tone.Of(40, 52, 70));
            int[] bars = { 14, 9, 17, 6, 12 };
            for (int i = 0; i < bars.Length; i++) c.HLine(7, 7 + i * 3, Math.Min(bars[i], width - 14), Rgb(110, 230, 150));
            Color[] lamps = { Rgb(236, 70, 70), Rgb(250, 208, 70), Rgb(98, 220, 120), Rgb(96, 160, 240) };
            for (int i = 0; 5 + i * 5 + 3 <= width - 4; i++) c.Rect(5 + i * 5, 26, 3, 2, lamps[i % lamps.Length]);
            Pix.Raised(c, 4, 32, width - 8, 7, Steel);
            for (int x = 6; x + 3 <= width - 5; x += 4) c.Rect(x, 34, 3, 3, Steel.Dark);
            for (int y = 43; y < 52; y += 3) c.HLine(5, y, width - 10, Casing.Dark);
        });
        Art Plain(int width) => kit.Face($"machine.plain.{width}", width, 56, c =>
        {
            Pix.Raised(c, 0, 0, width, 56, Casing);
            for (int y = 8; y < 20; y += 3) c.HLine(4, y, width - 8, Casing.Dark);
        });
        var top = kit.Face($"machine.top.{cd}", 26, cd, c =>
        {
            TopFace(c, Casing);
            Pix.Sunken(c, 5, 6, 16, cd - 12, Tone.Of(Casing.Dark));
        });

        Art south = facesEast || facesWest ? Plain(26) : Console(26);
        Art west = facesWest ? Console(cd) : Plain(cd), east = facesEast ? Console(cd) : Plain(cd);
        kit.Box(3, 29, 2, d - 2, 0, 56, top, south, west, east);
    }

    /// <summary>A computer on a small desk, for PC tiles.</summary>
    /// <summary>
    /// A bed (style guide, "Rooms"): a wooden frame and headboard, a cream pillow, the sheet turned back over a blue
    /// blanket quilted in lines, its head against the north wall.
    /// </summary>
    private static void Bed(KitBuilder kit, int d)
    {
        int length = d - 4;
        var top = kit.Face($"bed.top.{length}", 26, length, c =>
        {
            TopFace(c, BlueFabric);
            Pix.Raised(c, 4, 3, 18, 9, Cream);
            c.Rect(1, 14, 24, 4, Cream.Light);
            c.HLine(1, 17, 24, Cream.Dark);
            for (int y = 25; y < length - 3; y += 8) c.HLine(2, y, 22, BlueFabric.Dark);
        });
        var foot = kit.Face("bed.foot", 26, 14, c =>
        {
            Pix.Raised(c, 0, 0, 26, 14, Wood);
            c.Rect(1, 1, 24, 6, BlueFabric.Base);
            c.HLine(1, 6, 24, BlueFabric.Dark);
        });
        var side = kit.Face($"bed.side.{length}", length, 14, c =>
        {
            Pix.Raised(c, 0, 0, length, 14, WoodDark);
            c.Rect(1, 1, length - 2, 6, BlueFabric.Dark);
        });
        kit.Box(3, 29, 3, 3 + length, 0, 14, top, foot, side, side);
        var head = kit.Face("bed.head", 28, 26, c =>
        {
            Pix.Raised(c, 0, 0, 28, 26, WoodDark);
            Pix.Sunken(c, 4, 4, 20, 14, WoodDark);
        });
        var headTop = kit.Face("bed.head.top", 28, 3, c => TopFace(c, Wood));
        var headSide = kit.Face("bed.head.side", 3, 26, c => Pix.Raised(c, 0, 0, 3, 26, WoodDark));
        kit.Box(2, 30, 0, 3, 0, 26, headTop, head, headSide, headSide);
    }

    public static void BuildPc(KitBuilder kit, int tx, int ty)
    {
        kit.Origin = new Vector3(tx, 0, ty);
        var front = kit.Face("pc.desk.front", 28, 15, c => Pix.Raised(c, 0, 0, 28, 15, Wood));
        var top = kit.Face("pc.desk.top", 28, 24, c =>
        {
            TopFace(c, WoodLight);
            Pix.Raised(c, 6, 14, 16, 7, Steel);
            for (int x = 8; x < 20; x += 3) c.Rect(x, 16, 2, 3, Steel.Dark);
        });
        var side = kit.Face("pc.desk.side", 24, 15, c => Pix.Raised(c, 0, 0, 24, 15, WoodDark));
        kit.Box(2, 30, 0, 24, 0, 15, top, front, side, side);
        Monitor(kit, 7, 3, 15);
    }

    private static void Monitor(KitBuilder kit, float x, float z, float y)
    {
        var front = kit.Face("monitor.front", 18, 16, c =>
        {
            Pix.Raised(c, 0, 0, 18, 16, Casing);
            Screen(c, 2, 2, 14, 10);
            c.Rect(14, 13, 2, 2, Rgb(98, 220, 120));
        });
        var top = kit.Face("monitor.top", 18, 8, c => TopFace(c, Casing));
        var side = kit.Face("monitor.side", 8, 16, c => Pix.Raised(c, 0, 0, 8, 16, Tone.Of(Casing.Dark)));
        kit.Box(x, x + 18, z, z + 8, y, y + 16, top, front, side, side);
    }

    // ------------------------------------------------------------------ floor and walls

    private static void Rug(KitBuilder kit, int w, int d, bool center)
    {
        int rw = w - 6, rd = d - 6;
        var art = kit.Face($"rug.{center}.{rw}x{rd}", rw, rd, c =>
        {
            var field = center ? Rgb(236, 120, 130) : Rgb(196, 76, 60);
            var trim = center ? Rgb(250, 236, 238) : Rgb(236, 196, 110);
            var deep = PixelCanvas.Shadow(field, 0.25f);
            c.Rect(0, 0, rw, rd, field);
            Pix.Border(c, 0, 0, rw, rd, deep);
            c.Rect(3, 3, rw - 6, 2, trim);
            c.Rect(3, rd - 5, rw - 6, 2, trim);
            c.Rect(3, 3, 2, rd - 6, trim);
            c.Rect(rw - 5, 3, 2, rd - 6, trim);
            if (center)
            {
                BuildingArt.BallRoundel(c, rw / 2 - 13, rd / 2 - 13, 26);
                return;
            }
            // A stepped diamond in the middle and a small one in each corner
            for (int i = 0; i < 12; i += 2)
            {
                c.Rect(rw / 2 - 2 - i * 2, rd / 2 - 12 + i, 4 + i * 4, 2, trim);
                c.Rect(rw / 2 - 2 - i * 2, rd / 2 + 10 - i, 4 + i * 4, 2, trim);
            }
            for (int i = 0; i < 6; i += 2)
            {
                c.Rect(rw / 2 - 2 - i * 2, rd / 2 - 6 + i, 4 + i * 4, 2, field);
                c.Rect(rw / 2 - 2 - i * 2, rd / 2 + 4 - i, 4 + i * 4, 2, field);
            }
            foreach (int x in new[] { 9, rw - 13 })
                foreach (int y in new[] { 9, rd - 13 })
                {
                    c.Rect(x + 1, y, 2, 4, trim);
                    c.Rect(x, y + 1, 4, 2, trim);
                }
        });
        kit.Decal(3, w - 3, 3, d - 3, 0.006f, art);
    }

    /// <summary>
    /// A window in the back wall: two panes that show the sky (and turn to its colour as the day goes), curtains
    /// tied back at the sides, a rod above and a sill below.
    /// </summary>
    private static void Window(KitBuilder kit, int w)
    {
        int fw = w - 12;
        var art = kit.Face($"window.{fw}", fw, 38, c =>
        {
            var frame = Rgb(250, 248, 240);
            var shade = Rgb(206, 206, 216);
            c.Rect(0, 3, fw, 35, frame);
            c.VLine(fw - 1, 3, 35, shade);
            int pane = (fw - 6) / 2;
            foreach (int x in new[] { 2, 4 + pane })
            {
                for (int py = 0; py < 31; py++)
                    for (int px = 0; px < pane; px++)
                    {
                        // The sky by day: paler toward the horizon, with the frame's shadow at the top
                        var sky = py == 0 || px == 0 ? Rgb(120, 176, 232) : py < 12 ? Rgb(150, 204, 246) : py < 22 ? Rgb(178, 220, 250) : Rgb(206, 234, 252);
                        c.SetRaw(x + px, 5 + py, new Color(sky.R, sky.G, sky.B, ArtSheet.HomeLight));
                    }
                c.HLine(x, 19, pane, frame);
                c.HLine(x, 20, pane, shade);
            }

            // Curtains gathered to each side, and the rod they hang from
            var cloth = Tone.Of(236, 208, 168);
            for (int py = 0; py < 31; py++)
            {
                int span = py < 20 ? 9 - py / 4 : 4 + (py - 20) / 3;
                for (int i = 0; i < span; i++)
                {
                    var col = i == span - 1 ? cloth.Dark : i % 3 == 1 ? cloth.Light : cloth.Base;
                    c.SetRaw(2 + i, 5 + py, col);
                    c.SetRaw(fw - 3 - i, 5 + py, col);
                }
            }
            c.Rect(0, 0, fw, 2, WoodDark.Base);
            c.HLine(0, 0, fw, WoodDark.Light);
        });
        kit.Card(6, w - 6, 32.5f, 28, 66, art);
        kit.Block("window.sill", Tone.Of(250, 248, 240), 4, w - 4, 32, 35, 26, 28);
    }

    // ------------------------------------------------------------------ shared painting

    /// <summary>A face seen from above: the material with a light line where it meets the front and the left side.</summary>
    private static void TopFace(PixelCanvas c, Tone t)
    {
        c.Rect(0, 0, c.Width, c.Height, t.Base);
        c.HLine(0, c.Height - 1, c.Width, t.Light);
        c.VLine(0, 0, c.Height, t.Light);
        c.HLine(0, 0, c.Width, t.Dark);
        c.VLine(c.Width - 1, 0, c.Height, t.Dark);
    }

    /// <summary>The thin front of a slab: light above, dark below.</summary>
    private static void Edge(PixelCanvas c, Tone t)
    {
        for (int y = 0; y < c.Height; y++)
            c.HLine(0, y, c.Width, y == 0 ? t.Light : y == c.Height - 1 ? t.Dark : t.Base);
    }

    /// <summary>A computer screen: blue, deeper toward the foot, with three lines of text.</summary>
    private static void Screen(PixelCanvas c, int x, int y, int w, int h)
    {
        c.Rect(x - 1, y - 1, w + 2, h + 2, Dark.Base);
        c.Rect(x, y, w, h / 2, ScreenBlue);
        c.Rect(x, y + h / 2, w, h - h / 2, ScreenDeep);
        c.HLine(x + 2, y + 2, w - 6, ScreenLine);
        c.HLine(x + 2, y + 4, w - 4, ScreenLine);
        c.HLine(x + 2, y + 6, w - 8, ScreenLine);
    }

    /// <summary>A leafy plant in a terracotta pot: a sprite 28 by 46.</summary>
    public static void PaintPlant(PixelCanvas c)
    {
        // Leaves: three overlapping clumps, lightest on the upper left
        foreach (var (cx, cy, rx, ry) in new[] { (14f, 20f, 12f, 11f), (9f, 11f, 7f, 8f), (19f, 9f, 7f, 8f) })
            for (int y = (int)(cy - ry); y <= (int)(cy + ry); y++)
                for (int x = (int)(cx - rx); x <= (int)(cx + rx); x++)
                {
                    float u = (x + 0.5f - cx) / rx, v = (y + 0.5f - cy) / ry;
                    if (u * u + v * v > 1f) continue;
                    c.Set(x, y, u + v < -0.4f ? LeafLight : u + v > 0.55f ? LeafDark : Leaf);
                }
        foreach (var (x, y) in new[] { (7, 22), (15, 14), (20, 24), (11, 27) })
        {
            c.Rect(x, y, 3, 1, LeafDark);
            c.Rect(x + 1, y + 1, 3, 1, LeafDark);
        }

        // The pot, narrowing toward its foot, with a rim
        var clay = Tone.Of(196, 110, 72);
        for (int y = 32; y <= 44; y++)
        {
            int inset = (y - 32) / 5;
            for (int x = 6 + inset; x <= 21 - inset; x++)
                c.Set(x, y, x == 6 + inset ? clay.Light : x >= 19 - inset ? clay.Dark : clay.Base);
        }
        c.Rect(5, 30, 18, 3, clay.Base);
        c.HLine(5, 30, 18, clay.Light);
        c.HLine(5, 32, 18, clay.Dark);
        Pix.Outline(c);
    }

    /// <summary>A small vase of flowers for a table: a sprite 12 by 16.</summary>
    public static void PaintVase(PixelCanvas c)
    {
        var glaze = Tone.Of(96, 146, 214);
        c.Rect(4, 9, 4, 6, glaze.Base);
        c.VLine(4, 9, 6, glaze.Light);
        c.VLine(7, 9, 6, glaze.Dark);
        c.Rect(3, 11, 6, 3, glaze.Base);
        c.VLine(3, 11, 3, glaze.Light);
        c.Rect(5, 5, 2, 4, Leaf);
        c.Rect(2, 4, 2, 2, Leaf);
        c.Rect(8, 5, 2, 2, Leaf);
        foreach (var (x, y, col) in new[] { (4, 1, Rgb(236, 90, 110)), (1, 2, Rgb(250, 210, 76)), (7, 2, Rgb(252, 252, 252)) })
            c.Rect(x, y, 3, 3, col);
        Pix.Outline(c);
    }

    /// <summary>A landscape in a gilt frame, 26 by 20.</summary>
    public static void PaintPainting(PixelCanvas c)
    {
        Pix.Raised(c, 0, 0, 26, 20, Tone.Of(198, 156, 86));
        c.Rect(2, 2, 22, 16, Rgb(150, 208, 248));
        c.Rect(2, 9, 22, 3, Rgb(206, 232, 250));
        Pix.Disc(c, 16, 3, 5, Rgb(250, 232, 130));
        // Two hills, the nearer one darker
        for (int x = 2; x < 24; x++)
        {
            int far = 12 - Math.Max(0, 5 - Math.Abs(x - 16)), near = 15 - Math.Max(0, 4 - Math.Abs(x - 7));
            c.VLine(x, far, 18 - far, Rgb(112, 174, 122));
            c.VLine(x, near, 18 - near, Rgb(78, 142, 96));
        }
        Pix.Shade(c, 2, 2, 22, 1, 0.2f);
    }

    /// <summary>A round wall clock, 16 across.</summary>
    public static void PaintClock(PixelCanvas c)
    {
        Pix.Disc(c, 0, 0, 16, WoodDark.Base);
        Pix.Disc(c, 2, 2, 12, Rgb(250, 248, 240));
        foreach (var (x, y, w, h) in new[] { (7, 2, 2, 2), (7, 12, 2, 2), (2, 7, 2, 2), (12, 7, 2, 2) }) c.Rect(x, y, w, h, Ink);
        c.Rect(7, 4, 2, 5, Ink);
        c.Rect(8, 8, 4, 1, Ink);
    }
}

/// <summary>Unit icosphere triangles, used for foliage.</summary>
internal static class Icosphere
{
    private static readonly Vector3[][] Levels = Build();

    private static Vector3[][] Build()
    {
        float t = (1f + MathF.Sqrt(5f)) / 2f;
        var v = new[]
        {
            new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
            new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
            new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
        };
        int[] f =
        {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
        };
        var level0 = new Vector3[f.Length];
        for (int i = 0; i < f.Length; i++) level0[i] = Vector3.Normalize(v[f[i]]);

        var level1 = new System.Collections.Generic.List<Vector3>();
        for (int i = 0; i < level0.Length; i += 3)
        {
            var a = level0[i]; var b = level0[i + 1]; var c = level0[i + 2];
            var ab = Vector3.Normalize(a + b); var bc = Vector3.Normalize(b + c); var ca = Vector3.Normalize(c + a);
            level1.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
        }
        return new[] { level0, level1.ToArray() };
    }

    /// <summary>
    /// Adds an ellipsoid with smooth normals. Texture coordinates are projected along the dominant axis of each
    /// face (tileable foliage textures hide the seams); <paramref name="sway"/> lets the top move in the wind.
    /// </summary>
    public static void Add(MeshBuilder b, Vector3 center, Vector3 radii, Color color, int level, float uvScale, float sway, float jitter = 0.08f)
    {
        var tris = Levels[level];
        for (int i = 0; i < tris.Length; i += 3)
        {
            var n0 = tris[i]; var n1 = tris[i + 1]; var n2 = tris[i + 2];
            Vector3 P(Vector3 n) => center + n * radii * (1f + (Noise(n) - 0.5f) * jitter * 2f);
            var p0 = P(n0); var p1 = P(n1); var p2 = P(n2);

            var faceN = n0 + n1 + n2;
            var an = Vector3.Abs(faceN);
            Vector2 UV(Vector3 p) => an.X >= an.Y && an.X >= an.Z ? new(p.Z * uvScale, -p.Y * uvScale)
                : an.Y >= an.Z ? new(p.X * uvScale, p.Z * uvScale)
                : new(p.X * uvScale, -p.Y * uvScale);

            // Darker underneath (less skylight), and sway weighted toward the top of the clump
            Color C(Vector3 n) => MeshBuilder.Sway(MeshBuilder.Scale(color, 0.78f + 0.22f * (n.Y * 0.5f + 0.5f)), sway * (n.Y * 0.5f + 0.5f));
            b.Tri(p0, p1, p2, UV(p0), UV(p1), UV(p2), n0, n1, n2, C(n0), C(n1), C(n2));
        }
    }

    private static float Noise(Vector3 n)
    {
        int x = (int)MathF.Round(n.X * 50f), y = (int)MathF.Round(n.Y * 50f), z = (int)MathF.Round(n.Z * 50f);
        return GroundBaker.Rand01(x * 31 + z, y, 77);
    }
}
