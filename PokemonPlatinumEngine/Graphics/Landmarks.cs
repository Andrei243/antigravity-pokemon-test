using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// What stands in Sinnoh's towns besides buildings (style guide, "Props"): fountains, ships, wind turbines,
/// statues, honey trees, cargo, coal, hedges, columns and the rest, each placed by a model of the imported
/// world and as large as that model's tiles. Small things are sprites on upright cards like the street
/// furniture; ships, hedges and columns are boxes with every face painted. No GPU calls.
/// </summary>
internal static class Landmarks
{
    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    private static readonly Tone Stone = Tone.Of(176, 172, 170, 198, 194, 190, 124, 120, 130);
    private static readonly Tone PaleStone = Tone.Of(214, 210, 204, 232, 230, 224, 160, 156, 162);
    private static readonly Tone Wood = Tone.Of(188, 134, 84, 216, 166, 110, 140, 96, 64);
    private static readonly Tone DarkWood = Tone.Of(124, 84, 58, 156, 110, 76, 92, 62, 48);
    private static readonly Tone Iron = Tone.Of(72, 74, 92, 106, 110, 130, 48, 48, 64);
    private static readonly Tone Hull = Tone.Of(240, 242, 246, 252, 252, 255, 196, 202, 220);
    private static readonly Tone HullBand = Tone.Of(58, 108, 214, 110, 156, 240, 40, 76, 160);
    private static readonly Tone Funnel = Tone.Of(214, 72, 62, 240, 120, 100, 160, 48, 56);
    private static readonly Tone Bronze = Tone.Of(98, 150, 138, 140, 190, 172, 62, 104, 104);
    private static readonly Color Leaf = Rgb(60, 146, 76), LeafLight = Rgb(106, 196, 98), LeafDark = Rgb(36, 110, 62);
    private static readonly Color Water = Rgb(74, 144, 222), WaterLight = Rgb(150, 206, 246), WaterDark = Rgb(46, 104, 186);
    private static readonly Color Snow = Rgb(226, 234, 246), SnowShade = Rgb(188, 202, 230);
    private static readonly Color Glass = Rgb(170, 218, 246);
    private static readonly Tone Steel = Tone.Of(118, 128, 150, 160, 170, 190, 78, 86, 110);
    private static readonly Color Belt = Rgb(58, 56, 68), BeltJoint = Rgb(40, 38, 50), CoalLump = Rgb(30, 30, 40), CoalGlint = Rgb(120, 120, 136);

    /// <summary>Rows of art in a tile of real height: how tall something that stands <c>h</c> tiles is drawn.</summary>
    private static int Rows(KitBuilder kit, float tiles, int least, int most) =>
        Math.Clamp((int)MathF.Round(tiles * GroundBaker.ArtTile / kit.VS), least, most);

    /// <summary>
    /// Builds one prop of the world's models; the kit's origin is the north-west corner of its tiles, on the
    /// ground. False for a type that isn't one of these.
    /// </summary>
    public static bool Add(KitBuilder kit, Map map, Prop prop, MeshBuilder lamplight)
    {
        int w = prop.Width * 32, d = prop.Depth * 32;
        bool snowy = map.ArchitectureAt(prop.X, prop.Y) == Architecture.Snow;
        switch (prop.Type)
        {
            case PropType.Fountain:
            {
                int size = Math.Min(w - 8, 112);
                kit.Sprite(kit.Frames($"fountain.{size}", size, size * 3 / 4 + 22, MovingFrames, 6f, (c, frame) => PaintFountain(c, size, frame)), w / 2f, d - 10);
                return true;
            }
            case PropType.Boat:
                Boat(kit, w, d);
                return true;
            case PropType.WindTurbine:
            {
                int rows = Rows(kit, prop.Height, 110, 150);
                kit.Sprite(kit.Frames($"turbine.{rows}", 96, rows + 44, MovingFrames, 6f, (c, frame) => PaintTurbine(c, rows, frame)), w / 2f, d / 2f + 6);
                return true;
            }
            case PropType.Statue:
            {
                bool grand = prop.Width >= 3;
                float cx = w / 2f, cz = d / 2f + 4;
                kit.Block("statue.plinth", Stone, cx - 22, cx + 22, cz - 16, cz + 16, 0, 8);
                kit.Block("statue.plinth2", PaleStone, cx - 15, cx + 15, cz - 10, cz + 10, 8, grand ? 22 : 14);
                kit.Sprite(kit.Face(grand ? "statue.figure" : "statue.small", grand ? 40 : 28, grand ? 52 : 36, c => PaintStatue(c)), cx, cz, grand ? 22 : 14);
                return true;
            }
            case PropType.HoneyTree:
                kit.Sprite(kit.Face("honeytree", 76, 96, PaintHoneyTree), w / 2f, d / 2f + 8);
                return true;
            case PropType.Crates:
                kit.Sprite(kit.Face("crates", 44, 40, PaintCrates), w / 2f, d / 2f + 8);
                return true;
            case PropType.CoalHeap:
            {
                int width = Math.Clamp(w - 8, 40, 88);
                kit.Sprite(kit.Face($"coal.{width}", width, 34, c => PaintCoal(c)), w / 2f, d / 2f + 10);
                return true;
            }
            case PropType.Hedge:
            {
                var top = kit.Face($"hedge.top.{w - 6}x{d - 10}{(snowy ? ".snow" : "")}", w - 6, Math.Max(8, d - 10), c => PaintHedge(c, top: true, snowy));
                var front = kit.Face($"hedge.front.{w - 6}", w - 6, 20, c => PaintHedge(c, top: false, snow: false));
                var side = kit.Face($"hedge.side.{Math.Max(8, d - 10)}", Math.Max(8, d - 10), 20, c => PaintHedge(c, top: false, snow: false));
                kit.Box(3, w - 3, 6, Math.Max(14, d - 4), 0, 20, top, front, side, side);
                return true;
            }
            case PropType.FlowerBed:
            {
                // A kerb of pale stone round a bed of flowers, knee high
                int bw = w - 6, bd = Math.Max(10, d - 10);
                var top = kit.Face($"flowerbed.top.{bw}x{bd}", bw, bd, PaintFlowerBed);
                var front = kit.Face($"flowerbed.front.{bw}", bw, 12, c => Pix.Raised(c, 0, 0, c.Width, c.Height, PaleStone));
                var side = kit.Face($"flowerbed.side.{bd}", bd, 12, c => Pix.Raised(c, 0, 0, c.Width, c.Height, PaleStone));
                kit.Box(3, w - 3, 6, 6 + bd, 0, 12, top, front, side, side);
                return true;
            }
            case PropType.Column:
                Column(kit, prop, w, d);
                return true;
            case PropType.Topiary:
                kit.Sprite(kit.Face("topiary", 30, 58, PaintTopiary), w / 2f, d / 2f + 6);
                return true;
            case PropType.Cairn:
                kit.Sprite(kit.Face("cairn", 34, 38, PaintCairn), w / 2f, d / 2f + 8);
                return true;
            case PropType.Billboard:
                kit.Sprite(kit.Face("billboard", 48, 60, PaintBillboard), w / 2f, d / 2f + 4);
                return true;
            case PropType.Outcrop:
            {
                int width = Math.Min(w - 6, 480), height = Rows(kit, MathF.Min(prop.Height, 8f), 60, 130);
                kit.Sprite(kit.Face($"outcrop.{width}x{height}", width, height, c => PaintOutcrop(c)), w / 2f, d - 12);
                return true;
            }
            case PropType.Mast:
            {
                int rows = Rows(kit, prop.Height, 90, 170);
                kit.Sprite(kit.Face($"mast.{rows}", 30, rows, c => PaintLatticeMast(c)), w / 2f, d / 2f + 6);
                return true;
            }
            case PropType.Drums:
                kit.Sprite(kit.Face("drums", 42, 40, PaintDrums), w / 2f, d / 2f + 8);
                return true;
            case PropType.Conveyor:
                Conveyor(kit, map, prop);
                return true;
            case PropType.Gantry:
                Gantry(kit, prop);
                return true;
        }
        return false;
    }

    // ------------------------------------------------------------------ boxes

    /// <summary>
    /// A ship lying along its box: a white hull with a blue band, a planked deck inside a rail, a cabin with
    /// a row of windows and a red funnel.
    /// </summary>
    private static void Boat(KitBuilder kit, int w, int d)
    {
        bool northSouth = d > w;
        int x0 = 10, x1 = w - 10, z0 = 10, z1 = d - 8, deckY = 16;
        var deck = kit.Face($"boat.deck.{x1 - x0}x{z1 - z0}", x1 - x0, z1 - z0, c => PaintDeck(c, northSouth));
        var stern = kit.Face($"boat.hull.{x1 - x0}", x1 - x0, deckY, PaintHull);
        var flank = kit.Face($"boat.hull.{z1 - z0}", z1 - z0, deckY, PaintHull);
        kit.Box(x0, x1, z0, z1, 0, deckY, deck, stern, flank, flank);

        // The cabin stands toward the stern on a ship lying north and south, amidships otherwise; a boat too
        // small for one is an open hull
        int cw = northSouth ? x1 - x0 - 36 : (x1 - x0) / 2, cd = northSouth ? (z1 - z0) / 3 : z1 - z0 - 44;
        if (cw < 28 || cd < 24) return;
        int cx0 = (x0 + x1 - cw) / 2, cz0 = northSouth ? z1 - cd - 44 : (z0 + z1 - cd) / 2 - 4;
        var roof = kit.Face($"boat.roof.{cw}x{cd}", cw, cd, c => { Pix.Raised(c, 0, 0, c.Width, c.Height, Hull); c.Rect(3, 3, c.Width - 6, c.Height - 6, Hull.Dark); });
        var cabin = kit.Face($"boat.cabin.{cw}", cw, 22, PaintCabin);
        var cabinSide = kit.Face($"boat.cabin.{cd}", cd, 22, PaintCabin);
        kit.Box(cx0, cx0 + cw, cz0, cz0 + cd, deckY, deckY + 22, roof, cabin, cabinSide, cabinSide);

        var funnel = kit.Face("boat.funnel", 14, 24, c =>
        {
            Pix.Raised(c, 0, 0, 14, 24, Funnel);
            Pix.Raised(c, 0, 0, 14, 5, Iron);
            c.Rect(0, 9, 14, 3, Hull.Base);
        });
        var funnelTop = kit.Face("boat.funnel.top", 14, 14, c => { Pix.Raised(c, 0, 0, 14, 14, Iron); c.Rect(3, 3, 8, 8, Rgb(40, 40, 54)); });
        float fx = cx0 + cw / 2f - 7, fz = cz0 + cd / 2f - 7;
        kit.Box(fx, fx + 14, fz, fz + 14, deckY + 22, deckY + 46, funnelTop, funnel, funnel, funnel);
    }

    private static void PaintHull(PixelCanvas c)
    {
        Pix.Raised(c, 0, 0, c.Width, c.Height, Hull);
        c.Rect(0, c.Height - 6, c.Width, 6, HullBand.Base);
        c.HLine(0, c.Height - 6, c.Width, HullBand.Light);
        c.HLine(0, c.Height - 1, c.Width, HullBand.Dark);
        // Portholes a tile apart
        for (int x = 14; x + 5 < c.Width; x += 32)
        {
            Pix.Disc(c, x, 3, 5, Iron.Base);
            Pix.Disc(c, x + 1, 4, 3, Glass);
        }
    }

    /// <summary>The deck seen from above: planks running the ship's length inside a white rail, the corners rounded off.</summary>
    private static void PaintDeck(PixelCanvas c, bool northSouth)
    {
        int w = c.Width, h = c.Height;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                // Cut the four corners off: eight texels at the bow's end, four at the stern's
                int cut = (northSouth ? y < h / 2 : x < w / 2) ? 8 : 4;
                int fromX = Math.Min(x, w - 1 - x), fromY = Math.Min(y, h - 1 - y);
                if (fromX + fromY < cut) continue;
                bool rail = fromX < 3 || fromY < 3 || fromX + fromY < cut + 3;
                int plank = northSouth ? x : y;
                c.SetRaw(x, y, rail ? (fromX == 2 || fromY == 2 ? Hull.Dark : Hull.Base) : plank % 8 == 7 ? Wood.Dark : plank / 8 % 3 == 1 ? Wood.Light : Wood.Base);
            }
    }

    private static void PaintCabin(PixelCanvas c)
    {
        Pix.Raised(c, 0, 0, c.Width, c.Height, Hull);
        for (int x = 6; x + 10 <= c.Width - 4; x += 14)
        {
            c.Rect(x, 5, 10, 8, Iron.Base);
            for (int py = 0; py < 6; py++)
                for (int px = 0; px < 8; px++)
                    c.SetRaw(x + 1 + px, 6 + py, new Color(Glass.R, Glass.G, Glass.B, ArtSheet.PublicLight));
        }
        c.Rect(0, c.Height - 4, c.Width, 2, HullBand.Base);
    }

    // ------------------------------------------------------------------ the mine's conveyors

    /// <summary>The rows from the ground to the underside of a conveyor's deck: clear of whoever walks beneath.</summary>
    public const int BeltUnder = 62;

    /// <summary>The rows from the ground to the belt itself.</summary>
    public const int BeltTop = 68;

    /// <summary>
    /// A conveyor, tile by tile along its run: a steel deck twenty texels wide with the belt moving on it, and
    /// under each tile that is blocked a lattice pier down to the ground. Over open tiles it is a span.
    /// </summary>
    private static void Conveyor(KitBuilder kit, Map map, Prop prop)
    {
        bool northSouth = prop.Depth >= prop.Width;
        int tiles = northSouth ? prop.Depth : prop.Width;
        var belt = kit.Frames(northSouth ? "conveyor.belt.ns" : "conveyor.belt.ew", northSouth ? 20 : 32, northSouth ? 32 : 20, MovingFrames, 6f,
            (c, frame) => PaintBelt(c, northSouth, frame));
        var beam = kit.Face("conveyor.beam.32", 32, BeltTop - BeltUnder, PaintBeam);
        var end = kit.Face("conveyor.beam.20", 20, BeltTop - BeltUnder, PaintBeam);
        var pier = kit.Face("conveyor.pier", 16, BeltUnder, PaintPier);
        for (int i = 0; i < tiles; i++)
        {
            int at = i * 32;
            bool first = i == 0, last = i == tiles - 1;
            if (northSouth)
            {
                kit.Box(6, 26, at, at + 32, BeltUnder, BeltTop, belt, last ? end : null, beam, beam, first ? end : null);
                if (map.InBounds(prop.X, prop.Y + i) && map.IsSolid(prop.X, prop.Y + i)) kit.Box(8, 24, at + 8, at + 24, 0, BeltUnder, null, pier, pier, pier);
            }
            else
            {
                kit.Box(at, at + 32, 6, 26, BeltUnder, BeltTop, belt, beam, first ? end : null, last ? end : null);
                if (map.InBounds(prop.X + i, prop.Y) && map.IsSolid(prop.X + i, prop.Y)) kit.Box(at + 8, at + 24, 8, 24, 0, BeltUnder, null, pier, pier, pier);
            }
        }
    }

    /// <summary>
    /// What carries a conveyor over open ground: a row of frames, each two lattice legs with a beam across
    /// under the belt. A prop one tile each way is a pier standing alone, with a steel cap.
    /// </summary>
    private static void Gantry(KitBuilder kit, Prop prop)
    {
        var pier = kit.Face("conveyor.pier", 16, BeltUnder, PaintPier);
        if (prop.Width == 1 && prop.Depth == 1)
        {
            kit.Box(8, 24, 8, 24, 0, BeltUnder, null, pier, pier, pier);
            kit.Block("conveyor.cap", Steel, 6, 26, 6, 26, BeltUnder, BeltTop);
            return;
        }

        // Legs east and west of the way through (three tiles wide), or north and south of it (three deep)
        bool across = prop.Width == 3;
        int frames = across ? prop.Depth : prop.Width;
        var beam = kit.Face("conveyor.cross.80", 80, 8, PaintBeam);
        var beamEnd = kit.Face("conveyor.cross.8", 8, 8, PaintBeam);
        for (int i = 0; i < frames; i++)
        {
            int at = i * 32;
            if (across)
            {
                kit.Box(8, 24, at + 8, at + 24, 0, BeltUnder, null, pier, pier, pier);
                kit.Box(72, 88, at + 8, at + 24, 0, BeltUnder, null, pier, pier, pier);
                kit.Box(8, 88, at + 12, at + 20, BeltUnder - 8, BeltUnder, beam, beam, beamEnd, beamEnd);
            }
            else
            {
                kit.Box(at + 8, at + 24, 8, 24, 0, BeltUnder, null, pier, pier, pier);
                kit.Box(at + 8, at + 24, 72, 88, 0, BeltUnder, null, pier, pier, pier);
                kit.Box(at + 12, at + 20, 8, 88, BeltUnder - 8, BeltUnder, null, beamEnd, beam, beam);
            }
        }
    }

    /// <summary>
    /// A conveyor's belt from above, running along the canvas's longer side: steel edges, dark rubber with a
    /// joint every sixteen texels and a lump of coal riding between the joints, all moving four texels a frame.
    /// </summary>
    public static void PaintBelt(PixelCanvas c, bool northSouth, int frame = 0)
    {
        int across = northSouth ? c.Width : c.Height, along = northSouth ? c.Height : c.Width;
        void Set(int a, int b, Color col) => c.SetRaw(northSouth ? b : a, northSouth ? a : b, col);
        for (int a = 0; a < along; a++)
        {
            int moved = ((a - frame * 4) % 16 + 16) % 16;
            for (int b = 0; b < across; b++)
            {
                bool edge = b < 3 || b >= across - 3;
                Set(a, b, edge ? (b == 0 || b == across - 1 ? Steel.Dark : b == 1 || b == across - 2 ? Steel.Light : Steel.Base) : moved == 0 ? BeltJoint : Belt);
            }
            // The coal: a lump five texels long between each pair of joints
            if (moved is >= 6 and <= 10)
            {
                int width = moved is 6 or 10 ? 4 : 6, from = across / 2 - width / 2;
                for (int b = from; b < from + width; b++) Set(a, b, CoalLump);
                if (moved == 7) Set(a, from + 1, CoalGlint);
            }
        }
    }

    /// <summary>A steel beam from the side: light along its top, dark along its foot, a rivet every eight texels.</summary>
    public static void PaintBeam(PixelCanvas c)
    {
        Pix.Raised(c, 0, 0, c.Width, c.Height, Steel);
        if (c.Height >= 5)
            for (int x = 3; x < c.Width - 1; x += 8) c.SetRaw(x, c.Height / 2, Steel.Dark);
    }

    /// <summary>
    /// A lattice pier, sixteen texels wide: two steel posts with braces crossing between them every sixteen rows.
    /// What lies between is cut out, so the ground shows through.
    /// </summary>
    public static void PaintPier(PixelCanvas c)
    {
        int w = c.Width, h = c.Height;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                c.SetRaw(x, y, x == 0 ? Steel.Light : x == 2 ? Steel.Dark : Steel.Base);
                c.SetRaw(w - 1 - x, y, x == 0 ? Steel.Dark : x == 2 ? Steel.Light : Steel.Base);
            }
            // Braces: a rung every sixteen rows and a diagonal either way between rungs
            int row = (h - 1 - y) % 16;
            if (row == 0 || y == 0)
                for (int x = 3; x < w - 3; x++) c.SetRaw(x, y, Steel.Base);
            else
            {
                int d = 3 + row * (w - 7) / 15;
                c.SetRaw(d, y, Steel.Dark);
                c.SetRaw(w - 1 - d, y, Steel.Dark);
            }
        }
    }

    /// <summary>Three steel drums, two before and one behind, 42 by 40: blue, rust and blue, each with two bands.</summary>
    public static void PaintDrums(PixelCanvas c)
    {
        var blue = Tone.Of(78, 112, 168, 122, 156, 206, 52, 76, 126);
        var rust = Tone.Of(170, 96, 64, 206, 134, 96, 122, 66, 52);
        void Drum(int x, int y, Tone tone)
        {
            const int w = 18, h = 22;
            // The lid, seen from a little above, then the side with a band under the lid and one above the foot
            c.Rect(x + 2, y, w - 4, 6, tone.Light);
            c.Rect(x, y + 2, w, 3, tone.Light);
            c.Rect(x, y + 5, w, h - 5, tone.Base);
            c.Rect(x, y + 5, 2, h - 5, tone.Light);
            c.Rect(x + w - 3, y + 5, 3, h - 5, tone.Dark);
            c.HLine(x, y + 9, w, tone.Dark);
            c.HLine(x, y + h - 5, w, tone.Dark);
            c.HLine(x + 1, y + h - 1, w - 2, tone.Dark);
            c.Rect(x + 6, y + 2, 3, 2, tone.Dark);
        }
        Drum(12, 1, blue);
        Drum(1, 17, rust);
        Drum(22, 17, blue);
        Pix.Outline(c);
    }

    /// <summary>A column on a square base, as tall as its model; a fallen one lies along its tiles.</summary>
    private static void Column(KitBuilder kit, Prop prop, int w, int d)
    {
        if (prop.Width >= 4)
        {
            // Fallen: a drum lying east to west
            var along = kit.Face($"column.fallen.{w - 12}", w - 12, 22, c => PaintShaft(c, lying: true));
            var end = kit.Face("column.end", 24, 22, c => { Pix.Raised(c, 0, 0, 24, 22, PaleStone); Pix.Disc(c, 6, 5, 12, PaleStone.Dark); });
            var over = kit.Face($"column.over.{w - 12}", w - 12, 24, c => PaintShaft(c, lying: true));
            kit.Box(6, w - 6, d / 2f - 12, d / 2f + 12, 0, 22, over, along, end, end);
            return;
        }

        int rows = Rows(kit, prop.Height, 44, 190);
        float cx = w / 2f, cz = d / 2f + 2;
        kit.Block("column.base", PaleStone, cx - 15, cx + 15, cz - 15, cz + 15, 0, 7);
        var shaft = kit.Face($"column.shaft.{rows}", 22, rows, c => PaintShaft(c, lying: false));
        // A broken column ends in the break; a whole one in a capital
        bool broken = prop.Model.Contains("colum05") || prop.Model.Contains("colum06");
        var top = kit.Face(broken ? "column.break" : "column.top", 22, 22, c =>
        {
            Pix.Raised(c, 0, 0, 22, 22, PaleStone);
            if (broken) { c.Rect(4, 4, 14, 14, PaleStone.Dark); c.Rect(6, 6, 6, 5, PaleStone.Base); }
        });
        kit.Box(cx - 11, cx + 11, cz - 11, cz + 11, 7, 7 + rows, top, shaft, shaft, shaft);
        if (!broken) kit.Block("column.capital", PaleStone, cx - 15, cx + 15, cz - 15, cz + 15, 7 + rows, 13 + rows);
    }

    /// <summary>A fluted shaft: flutes three texels wide with a light and a dark edge, and a joint every 24 rows.</summary>
    private static void PaintShaft(PixelCanvas c, bool lying)
    {
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
            {
                int across = lying ? y : x, along = lying ? x : y;
                int flute = across % 4;
                var col = along % 24 == 23 ? PaleStone.Dark : flute == 0 ? PaleStone.Light : flute == 3 ? PaleStone.Dark : PaleStone.Base;
                c.SetRaw(x, y, col);
            }
    }

    private static void PaintHedge(PixelCanvas c, bool top, bool snow)
    {
        int w = c.Width, h = c.Height;
        c.Rect(0, 0, w, h, top ? (snow ? Snow : Leaf) : LeafDark);
        if (top)
        {
            // Leaf clumps on a grid, each lit on its upper left; under snow the same clumps are drifts
            for (int y = 2; y + 4 <= h; y += 6)
                for (int x = 2 + (y / 6 % 2) * 4; x + 5 <= w; x += 8)
                {
                    c.Rect(x, y, 5, 3, snow ? Color.White : LeafLight);
                    c.Rect(x + 1, y + 3, 4, 1, snow ? SnowShade : LeafDark);
                }
            c.HLine(0, h - 1, w, snow ? SnowShade : LeafLight);
            c.VLine(0, 0, h, snow ? Color.White : LeafLight);
            return;
        }
        for (int y = 3; y + 3 <= h - 2; y += 6)
            for (int x = 1 + (y / 6 % 2) * 4; x + 5 <= w; x += 8)
                c.Rect(x, y, 5, 2, Leaf);
        c.HLine(0, 0, w, LeafLight);
        c.HLine(0, h - 1, w, Rgb(30, 84, 56));
    }

    /// <summary>The top of a flower bed: a rim of the kerb round dark soil set with flowers in rows, red, yellow, white and pink by turns.</summary>
    private static void PaintFlowerBed(PixelCanvas c)
    {
        int w = c.Width, h = c.Height;
        c.Rect(0, 0, w, h, PaleStone.Base);
        c.Rect(2, 2, w - 4, h - 4, Rgb(96, 70, 52));
        c.HLine(0, 0, w, PaleStone.Light);
        c.HLine(0, h - 1, w, PaleStone.Dark);
        Color[] petals = { Rgb(232, 76, 86), Rgb(246, 210, 76), Rgb(248, 246, 240), Rgb(240, 140, 186) };
        int row = 0;
        for (int y = 4; y + 3 <= h - 2; y += 5, row++)
            for (int x = 4 + (row % 2) * 3; x + 4 <= w - 2; x += 6)
            {
                // Each flower two texels square, its leaves two wide under it: nothing a texel on its own
                var petal = petals[(x / 6 + row) % petals.Length];
                c.Rect(x, y, 2, 2, petal);
                c.Rect(x + 1, y + 2, 2, 1, Leaf);
            }
    }

    // ------------------------------------------------------------------ sprites

    /// <summary>A three-toned lump: light on the upper left, dark on the lower right and along its foot.</summary>
    private static void Lump(PixelCanvas c, float cx, float cy, float rx, float ry, Color light, Color mid, Color dark, float flatBelow = float.MaxValue)
    {
        for (int y = (int)(cy - ry); y <= (int)MathF.Min(cy + ry, flatBelow); y++)
            for (int x = (int)(cx - rx); x <= (int)(cx + rx); x++)
            {
                float u = (x + 0.5f - cx) / rx, v = (y + 0.5f - cy) / ry;
                if (u * u + v * v > 1f) continue;
                c.Set(x, y, u + v * 1.2f < -0.4f ? light : u + v * 0.8f > 0.5f || v > 0.62f ? dark : mid);
            }
    }

    /// <summary>
    /// A fountain seen from a little above, <paramref name="size"/> across: a round basin of pale stone with
    /// water in the pond's blues, two ripple rings, and a jet on a pedestal in the middle.
    /// </summary>
    /// <summary>How many frames a fountain and a turbine move in (style guide, "Life").</summary>
    public const int MovingFrames = 4;

    /// <summary>
    /// A fountain, <paramref name="frame"/> of four: the rings in its basin travel outward, the top of its jet
    /// bobs a texel, and drops fall away from it to both sides.
    /// </summary>
    public static void PaintFountain(PixelCanvas c, int size, int frame = 0)
    {
        int w = c.Width, h = c.Height;
        float cx = w / 2f, ry = size * 0.3f, rx = size / 2f - 1, cy = h - ry - 12;

        // The basin's wall: the rim's ellipse dropped by ten rows
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f - cx) / rx;
                if (MathF.Abs(u) > 1f) continue;
                float edge = cy + ry * MathF.Sqrt(1f - u * u);
                if (y + 0.5f > edge && y + 0.5f <= edge + 10f)
                    c.Set(x, y, y + 0.5f > edge + 8f ? PaleStone.Dark : u < -0.55f ? PaleStone.Light : u > 0.55f ? PaleStone.Dark : PaleStone.Base);
            }

        // The rim, the water inside it, and the rings round where the jet falls
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f - cx) / rx, v = (y + 0.5f - cy) / ry;
                float dist = u * u + v * v;
                if (dist > 1f) continue;
                float inner = (x + 0.5f - cx) / (rx - 6), innerV = (y + 0.5f - cy) / (ry - 4);
                float within = inner * inner + innerV * innerV;
                if (within > 1f) { c.Set(x, y, v < -0.2f ? PaleStone.Light : PaleStone.Base); continue; }
                // Three rings a third of the way apart, each a quarter of that further out with every frame
                float phase = (within - 0.08f - 0.085f * frame) / 0.34f;
                bool ring = within > 0.08f && within < 0.86f && phase - MathF.Floor(phase) < 0.24f;
                c.Set(x, y, ring ? WaterLight : within > 0.86f && v < 0 ? WaterDark : Water);
            }

        // The pedestal and the jet: a column of white with pale blue sides, spreading at the top
        int px = (int)cx;
        c.Rect(px - 4, (int)cy - 10, 8, 12, PaleStone.Base);
        c.VLine(px - 4, (int)cy - 10, 12, PaleStone.Light);
        c.VLine(px + 3, (int)cy - 10, 12, PaleStone.Dark);
        c.Rect(px - 6, (int)cy - 12, 12, 3, PaleStone.Light);
        int jetTop = Math.Max(3, (int)cy - 44) + frame % 2;
        for (int y = jetTop; y < (int)cy - 12; y++)
        {
            int spread = y < jetTop + 8 ? 4 - (y - jetTop) / 3 : 1;
            c.Rect(px - spread, y, spread * 2, 1, WaterLight);
            c.Rect(px - 1, y, 2, 1, Color.White);
        }
        // Two drops to each side, half a cycle apart, falling away from the jet's top
        for (int drop = 0; drop < 2; drop++)
        {
            int fallen = (frame + drop * 2) % MovingFrames;
            c.Rect(px - 7 - fallen, jetTop + 5 + fallen * 5, 3, 2, WaterLight);
            c.Rect(px + 5 + fallen, jetTop + 7 + fallen * 5, 3, 2, WaterLight);
        }
        Pix.Outline(c);
    }

    /// <summary>
    /// A wind turbine: a white tower <paramref name="rows"/> tall narrowing to its top, a nacelle and three
    /// blades, turned thirty degrees with each of its four frames (the fourth step brings the next blade round).
    /// </summary>
    public static void PaintTurbine(PixelCanvas c, int rows, int frame = 0)
    {
        int w = c.Width, h = c.Height, mid = w / 2, hub = h - rows;
        var white = Tone.Of(214, 218, 228, 234, 236, 242, 170, 178, 200);
        for (int y = hub; y < h; y++)
        {
            int half = 3 + (y - hub) * 2 / rows;
            c.Rect(mid - half, y, half * 2, 1, white.Base);
            c.Set(mid - half, y, white.Light);
            c.Rect(mid + half - 2, y, 2, 1, white.Dark);
        }
        c.Rect(mid - 7, h - 4, 14, 4, Stone.Base);
        c.HLine(mid - 7, h - 4, 14, Stone.Light);

        // Blades a third of a turn apart, the first straight up in the first frame; each tapers from five texels to two
        void Blade(float degrees)
        {
            float angle = degrees * MathF.PI / 180f, dx = MathF.Sin(angle), dy = -MathF.Cos(angle);
            for (int i = 4; i < 43; i++)
            {
                int thick = i < 12 ? 5 : i < 30 ? 4 : 2;
                int x = (int)MathF.Round(mid + dx * i - thick / 2f), y = (int)MathF.Round(hub + dy * i - thick / 2f);
                c.Rect(x, y, thick, thick, white.Base);
                // Its upper edge catches the light
                c.Set(x, y, white.Light);
            }
        }
        for (int blade = 0; blade < 3; blade++) Blade(frame * 30f + blade * 120f);
        Pix.Disc(c, mid - 5, hub - 5, 10, white.Dark);
        Pix.Disc(c, mid - 3, hub - 3, 6, white.Light);
        Pix.Outline(c);
    }

    /// <summary>
    /// A statue's figure in green bronze, our own: a long-necked creature rearing on its hind legs, wings
    /// half open, in three flat shades.
    /// </summary>
    public static void PaintStatue(PixelCanvas c)
    {
        float s = c.Width / 40f;
        void Part(float cx, float cy, float rx, float ry) => Lump(c, cx * s, cy * s, rx * s, ry * s, Bronze.Light, Bronze.Base, Bronze.Dark);
        Part(20, 36, 9, 10);        // haunches
        Part(21, 26, 7, 9);         // chest
        Part(24, 15, 4, 8);         // neck
        Part(27, 8, 6, 4);          // head
        Part(10, 22, 7, 5);         // a wing
        Part(31, 25, 5, 4);         // the other, behind
        Part(13, 44, 4, 6);         // hind legs
        Part(26, 45, 4, 5);
        Part(8, 40, 6, 3);          // tail
        c.Rect((int)(31 * s), (int)(6 * s), 2, 2, Bronze.Dark);
        Pix.Outline(c);
    }

    /// <summary>
    /// A honey tree, 76 by 96: a broad crown in a warmer green than the forest's, on a thick trunk with a patch
    /// of honey.
    /// </summary>
    public static void PaintHoneyTree(PixelCanvas c)
    {
        var bark = Rgb(116, 80, 54);
        var barkDark = Rgb(86, 58, 42);
        var leaf = Rgb(150, 190, 84);
        var light = Rgb(196, 220, 104);
        var deep = Rgb(104, 150, 70);
        var honey = Rgb(236, 176, 64);

        c.Rect(31, 58, 14, 36, bark);
        c.VLine(31, 58, 36, Rgb(146, 104, 70));
        c.Rect(42, 58, 3, 36, barkDark);
        c.Rect(27, 92, 22, 3, barkDark);
        c.Rect(34, 70, 7, 9, honey);
        c.Rect(35, 79, 3, 4, honey);
        c.VLine(34, 70, 9, Rgb(250, 214, 110));

        void Clump(float cx, float cy, float rx, float ry) => Lump(c, cx, cy, rx, ry, light, leaf, deep);
        Clump(38, 34, 30, 26);
        Clump(18, 40, 16, 15);
        Clump(58, 42, 16, 15);
        Clump(28, 18, 18, 15);
        Clump(50, 20, 17, 15);
        Clump(38, 50, 22, 12);
        Pix.Outline(c);
    }

    /// <summary>Two wooden crates and one on top, 44 by 40.</summary>
    public static void PaintCrates(PixelCanvas c)
    {
        void Crate(int x, int y, int w, int h)
        {
            Pix.Raised(c, x, y, w, h, Wood);
            Pix.Border(c, x + 2, y + 2, w - 4, h - 4, DarkWood.Base);
            for (int i = 0; i < Math.Min(w, h) - 6; i += 2) c.Rect(x + 3 + i, y + 3 + i, 2, 1, DarkWood.Base);
        }
        Crate(1, 20, 20, 19);
        Crate(22, 18, 21, 21);
        Crate(11, 2, 19, 18);
        Pix.Outline(c);
    }

    /// <summary>A heap of coal: one mound and a shoulder in three flat darks with glints of two texels.</summary>
    public static void PaintCoal(PixelCanvas c)
    {
        var light = Rgb(104, 102, 112);
        var mid = Rgb(74, 72, 84);
        var dark = Rgb(54, 52, 66);
        int w = c.Width, h = c.Height;
        Lump(c, w * 0.45f, h - 2f, w * 0.44f, h - 6f, light, mid, dark, h - 2f);
        Lump(c, w * 0.76f, h - 1f, w * 0.22f, h * 0.5f, light, mid, dark, h - 2f);
        foreach (var (x, y) in new[] { (0.3f, 0.5f), (0.5f, 0.3f), (0.62f, 0.62f), (0.2f, 0.75f), (0.8f, 0.8f) })
            c.Rect((int)(w * x), (int)(h * y), 2, 1, Rgb(150, 150, 164));
        Pix.Outline(c);
    }

    /// <summary>A clipped cone of leaves on a short trunk, 30 by 58.</summary>
    public static void PaintTopiary(PixelCanvas c)
    {
        c.Rect(13, 48, 4, 9, DarkWood.Base);
        c.VLine(16, 48, 9, DarkWood.Dark);
        for (int y = 2; y < 50; y++)
        {
            int half = 1 + (y - 2) * 13 / 48;
            for (int x = 15 - half; x < 15 + half; x++)
                c.Set(x, y, x < 15 - half / 3 ? LeafLight : x >= 15 + half / 3 ? LeafDark : Leaf);
        }
        for (int y = 12; y < 48; y += 9) c.Rect(11, y, 5, 1, LeafDark);
        Pix.Outline(c);
    }

    /// <summary>Three worked stones one on another, 34 by 38.</summary>
    public static void PaintCairn(PixelCanvas c)
    {
        Pix.Raised(c, 2, 24, 30, 13, Stone);
        Pix.Raised(c, 6, 12, 22, 12, PaleStone);
        Pix.Raised(c, 10, 2, 14, 10, Stone);
        c.Rect(12, 16, 10, 1, Stone.Dark);
        c.Rect(12, 19, 7, 1, Stone.Dark);
        Pix.Outline(c);
    }

    /// <summary>A board 44 wide on two posts, pale with three dark lines, 48 by 60.</summary>
    public static void PaintBillboard(PixelCanvas c)
    {
        foreach (int x in new[] { 8, 37 })
        {
            c.Rect(x, 30, 3, 30, DarkWood.Base);
            c.VLine(x, 30, 30, DarkWood.Light);
        }
        Pix.Raised(c, 2, 2, 44, 30, DarkWood);
        Pix.Raised(c, 5, 5, 38, 24, Tone.Of(236, 226, 196));
        c.Rect(10, 11, 28, 2, DarkWood.Base);
        c.Rect(10, 16, 22, 2, DarkWood.Base);
        c.Rect(10, 21, 16, 2, DarkWood.Base);
        Pix.Outline(c);
    }

    /// <summary>
    /// A mass of rock filling its canvas: three peaks of different heights in the boulders' shades, with a
    /// ledge line every ten rows.
    /// </summary>
    public static void PaintOutcrop(PixelCanvas c)
    {
        var light = Rgb(196, 192, 190);
        var mid = Rgb(150, 146, 150);
        var dark = Rgb(104, 100, 112);
        int w = c.Width, h = c.Height;
        Lump(c, w * 0.5f, h - 1f, w * 0.49f, h * 0.62f, light, mid, dark, h - 1f);
        Lump(c, w * 0.3f, h * 0.55f, w * 0.22f, h * 0.5f, light, mid, dark, h - 1f);
        Lump(c, w * 0.62f, h * 0.45f, w * 0.2f, h * 0.44f, light, mid, dark, h - 1f);
        for (int y = h - 10; y > h / 3; y -= 10)
            for (int x = 6; x < w - 14; x += 22)
            {
                int at = x + (y / 10 % 2) * 9;
                if (c.IsOpaque(at, y) && c.IsOpaque(at + 9, y)) c.Rect(at, y, 9, 1, Rgb(74, 70, 86));
            }
        Pix.Outline(c);
    }

    /// <summary>A lattice mast filling its canvas, with a red light on top that burns all night.</summary>
    public static void PaintLatticeMast(PixelCanvas c)
    {
        int w = c.Width, h = c.Height, mid = w / 2;
        for (int y = 6; y < h; y++)
        {
            int half = 2 + (y - 6) * (mid - 3) / (h - 6);
            c.Rect(mid - half, y, 2, 1, Iron.Light);
            c.Rect(mid + half - 2, y, 2, 1, Iron.Base);
            if (y % 10 == 0) c.Rect(mid - half, y, half * 2, 2, Iron.Base);
        }
        c.Rect(mid - 2, 0, 4, 6, Rgb(236, 70, 70));
        Pix.Lit(c, mid - 2, 0, 4, 6, ArtSheet.PublicLight);
        Pix.Outline(c);
    }
}
