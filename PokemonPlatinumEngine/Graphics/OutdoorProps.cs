using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Street furniture (style guide, "Props"): lamps, mailboxes, planters, benches and signposts as pixel-art
/// sprites on upright cards, and fences as real posts and rails that join up from tile to tile. The sprites are
/// drawn seen from a little above with a one-texel outline, like the characters. No GPU calls.
/// </summary>
internal static class OutdoorProps
{
    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    private static readonly Tone Iron = Tone.Of(62, 84, 86, 100, 128, 124, 40, 54, 62);
    private static readonly Tone Wood = Tone.Of(188, 134, 84, 216, 166, 110, 140, 96, 64);
    private static readonly Tone DarkWood = Tone.Of(124, 84, 58, 156, 110, 76, 92, 62, 48);
    private static readonly Tone FenceWood = Tone.Of(176, 128, 84, 208, 162, 110, 130, 90, 62);
    private static readonly Tone FenceWhite = Tone.Of(240, 236, 226, 252, 250, 244, 196, 196, 210);
    private static readonly Tone WallStone = Tone.Of(176, 172, 170, 198, 194, 190, 124, 120, 130);
    private static readonly Tone WallCap = Tone.Of(214, 210, 204, 232, 230, 224, 160, 156, 162);
    private static readonly Tone Red = Tone.Of(214, 72, 62, 240, 120, 100, 160, 48, 56);
    private static readonly Color LampGlass = Rgb(252, 240, 190);
    private static readonly Color Leaf = Rgb(60, 146, 76);
    private static readonly Color LeafLight = Rgb(106, 196, 98);
    private static readonly Color LeafDark = Rgb(36, 110, 62);

    /// <summary>Height of a street lamp in texels.</summary>
    public const int LampHeight = 78;

    /// <param name="lamplight">Where the lamps' pools and halos go: drawn additively after dark.</param>
    /// <param name="window">The tiles whose props are wanted: a chunk of a streamed map; null for the whole map.</param>
    public static void Add(KitBuilder kit, Map map, MeshBuilder lamplight, TileWindow? window = null)
    {
        var within = window ?? new TileWindow(0, 0, map.Width, map.Height);
        // Fences and low walls join up with each other; a tile that is both is a wall
        var fenced = new Dictionary<(int, int), bool>();
        foreach (var prop in map.Props)
        {
            if (prop.Type is not (PropType.Fence or PropType.LowWall)) continue;
            for (int y = prop.Y; y < prop.Y + prop.Depth; y++)
                for (int x = prop.X; x < prop.X + prop.Width; x++)
                    fenced[(x, y)] = prop.Type == PropType.LowWall || fenced.GetValueOrDefault((x, y));
        }
        foreach (var ((x, y), wall) in fenced)
            if (within.Contains(x, y)) Fence(kit, map, fenced, x, y, wall);

        foreach (var prop in map.Props)
        {
            if (!within.Contains(prop.X, prop.Y)) continue;
            kit.Origin = new Vector3(prop.X, Relief.At(map, prop.X + prop.Width / 2f, prop.Y + prop.Depth / 2f), prop.Y);
            switch (prop.Type)
            {
                case PropType.LampPost:
                    kit.Sprite(kit.Face("lamp", 16, LampHeight, PaintLamp), 16, 16);
                    LampLight(kit, lamplight);
                    break;
                case PropType.Mailbox:
                    kit.Sprite(kit.Face("mailbox", 16, 30, PaintMailbox), 16, 16);
                    break;
                case PropType.Planter:
                    kit.Sprite(kit.Face("planter", 30, 28, PaintPlanter), 16, 18);
                    break;
                case PropType.Bench when prop.Depth > prop.Width:
                    // Lying north and south: seen from its end
                    kit.Sprite(kit.Face("bench.side", 24, 62, PaintBenchSide), 16, prop.Depth * 32 - 4);
                    break;
                case PropType.Bench:
                    kit.Sprite(kit.Face("bench", 60, 32, PaintBench), prop.Width * 16, 14);
                    break;
                case PropType.CutTree:
                    kit.Sprite(kit.Face("cut_tree", 26, 40, PaintCutTree), 16, 20);
                    break;
                case PropType.CrackedRock:
                    kit.Sprite(kit.Face("cracked_rock", 30, 26, PaintCrackedRock), 16, 19);
                    break;
                case PropType.StrengthBoulder:
                    kit.Sprite(kit.Face("strength_boulder", 30, 30, PaintStrengthBoulder), 16, 20);
                    break;
                case PropType.Boulder:
                {
                    // In water a boulder stands on the bed, a little lower, inside its ring of foam
                    bool inWater = map.GetGroundTile(prop.X, prop.Y) == TileType.Water;
                    bool mirrored = (prop.X * 7 + prop.Y * 3) % 2 == 1;
                    var art = kit.Face(mirrored ? "boulder.mirrored" : "boulder", 36, 30, c => PaintBoulder(c, mirrored));
                    kit.Sprite(art, 16, 19, inWater ? -3 : 0);
                    break;
                }
                default:
                    Landmarks.Add(kit, map, prop, lamplight);
                    break;
            }
        }

        for (int ty = within.Y; ty < within.Bottom; ty++)
            for (int tx = within.X; tx < within.Right; tx++)
            {
                if (map.GetGroundTile(tx, ty) != TileType.Signpost || MapStructures.IsWallSign(map, tx, ty)) continue;
                kit.Origin = new Vector3(tx, Relief.At(map, tx + 0.5f, ty + 0.5f), ty);
                kit.Sprite(kit.Face("signpost", 30, 30, PaintSignpost), 16, 16);
            }
    }

    // ------------------------------------------------------------------ fences

    /// <summary>How a town fences its yards: in wood, in white-painted wood, or with iron railings.</summary>
    internal enum FenceKind { Wood, White, Iron }

    internal static FenceKind FenceOf(Architecture town) => town switch
    {
        Architecture.Clapboard or Architecture.Cottage or Architecture.Resort => FenceKind.White,
        Architecture.City or Architecture.Brick or Architecture.Stone or Architecture.Townhouse or Architecture.Harbour
            or Architecture.Seaside or Architecture.Snow => FenceKind.Iron,
        _ => FenceKind.Wood
    };

    /// <summary>
    /// One tile of fence: a post in the middle and a pair of rails toward each neighbouring tile that is fenced
    /// too, so runs, corners and ends all come from the same rule. A low wall is a pier with an arm toward each
    /// such neighbour.
    /// </summary>
    private static void Fence(KitBuilder kit, Map map, Dictionary<(int, int), bool> fenced, int x, int y, bool wall)
    {
        kit.Origin = new Vector3(x, Relief.At(map, x + 0.5f, y + 0.5f), y);
        bool east = Joins(fenced, x, y, 1, 0), west = Joins(fenced, x, y, -1, 0), south = Joins(fenced, x, y, 0, 1), north = Joins(fenced, x, y, 0, -1);
        if (wall)
        {
            kit.Block("lowwall.pier", WallStone, 11, 21, 11, 21, 0, 12);
            kit.Block("lowwall.cap", WallCap, 10, 22, 10, 22, 12, 14);
            if (east) { kit.Block("lowwall.arm", WallStone, 21, 32, 12, 20, 0, 10); kit.Block("lowwall.top", WallCap, 21, 32, 11, 21, 10, 12); }
            if (west) { kit.Block("lowwall.arm", WallStone, 0, 11, 12, 20, 0, 10); kit.Block("lowwall.top", WallCap, 0, 11, 11, 21, 10, 12); }
            if (south) { kit.Block("lowwall.arm", WallStone, 12, 20, 21, 32, 0, 10); kit.Block("lowwall.top", WallCap, 11, 21, 21, 32, 10, 12); }
            if (north) { kit.Block("lowwall.arm", WallStone, 12, 20, 0, 11, 0, 10); kit.Block("lowwall.top", WallCap, 11, 21, 0, 11, 10, 12); }
            return;
        }

        var kind = FenceOf(map.ArchitectureAt(x, y));
        var tone = kind switch { FenceKind.White => FenceWhite, FenceKind.Iron => Iron, _ => FenceWood };
        string name = kind switch { FenceKind.White => "fence.white", FenceKind.Iron => "fence.iron", _ => "fence.wood" };
        // Iron railings are slighter than a wooden fence, and a little taller
        int half = kind == FenceKind.Iron ? 1 : 2, top = kind == FenceKind.Iron ? 20 : 17;
        int[] rails = kind == FenceKind.Iron ? new[] { 4, 16 } : new[] { 5, 11 };

        kit.Block(name + ".post", tone, 16 - half, 16 + half, 16 - half, 16 + half, 0, top);
        foreach (int railY in rails)
        {
            if (east) kit.Block(name + ".rail", tone, 16 + half, 32, 15, 17, railY, railY + 2);
            if (west) kit.Block(name + ".rail", tone, 0, 16 - half, 15, 17, railY, railY + 2);
            if (south) kit.Block(name + ".rail", tone, 15, 17, 16 + half, 32, railY, railY + 2);
            if (north) kit.Block(name + ".rail", tone, 15, 17, 0, 16 - half, railY, railY + 2);
        }
        if (kind != FenceKind.Iron) return;
        // Bars between the rails, every eight texels along each run
        foreach (int bar in new[] { 4, 12, 20, 28 })
        {
            if (bar > 16 ? east : west) kit.Block(name + ".bar", tone, bar - 1, bar + 1, 15, 17, 6, 16);
            if (bar > 16 ? south : north) kit.Block(name + ".bar", tone, 15, 17, bar - 1, bar + 1, 6, 16);
        }
    }

    /// <summary>
    /// Whether a fenced tile's rails run on to its fenced neighbour. Where the world fences a band two tiles
    /// thick (the edge of Jubilife City's terrace), the two rows run side by side along the band and are not
    /// tied to each other at every tile: rails across a square of four fenced tiles are left out, unless the
    /// band is the longer that way.
    /// </summary>
    internal static bool Joins(Dictionary<(int, int), bool> fenced, int x, int y, int dx, int dy)
    {
        if (!fenced.ContainsKey((x + dx, y + dy))) return false;
        // The side step across the way the rail would run: is there a second row beside this one?
        int sx = dy != 0 ? 1 : 0, sy = dx != 0 ? 1 : 0;
        bool square = fenced.ContainsKey((x + sx, y + sy)) && fenced.ContainsKey((x + dx + sx, y + dy + sy))
            || fenced.ContainsKey((x - sx, y - sy)) && fenced.ContainsKey((x + dx - sx, y + dy - sy));
        if (!square) return true;

        int Run(int ax, int ay)
        {
            int n = 1;
            for (int i = 1; fenced.ContainsKey((x + ax * i, y + ay * i)); i++) n++;
            for (int i = 1; fenced.ContainsKey((x - ax * i, y - ay * i)); i++) n++;
            return n;
        }
        // The band runs the way its fenced tiles go on longest
        return Run(Math.Abs(dx), Math.Abs(dy)) > Run(sx, sy);
    }

    // ------------------------------------------------------------------ light

    /// <summary>A round pool of light at the lamp's foot and a soft halo round its lantern.</summary>
    private static void LampLight(KitBuilder kit, MeshBuilder lamplight)
    {
        // The vertex colour scales the light: the pool is gentler than the glow texture's full strength
        const float pool = 1.9f, halo = 0.62f;
        var poolLevel = new Color(140, 140, 140, 255);
        var haloLevel = new Color(150, 150, 150, 255);
        var foot = kit.Origin + new Vector3(0.5f, 0.03f, 0.5f);
        lamplight.Quad(foot + new Vector3(-pool, 0, pool), foot + new Vector3(pool, 0, pool), foot + new Vector3(pool, 0, -pool), foot + new Vector3(-pool, 0, -pool),
            new(0, 1), new(1, 1), new(1, 0), new(0, 0), poolLevel, Vector3.UnitY);

        var lantern = kit.At(16, LampHeight - 9, 17);
        float up = halo * kit.VS;
        lamplight.Quad(lantern + new Vector3(-halo, -up, 0), lantern + new Vector3(halo, -up, 0), lantern + new Vector3(halo, up, 0), lantern + new Vector3(-halo, up, 0),
            new(0, 1), new(1, 1), new(1, 0), new(0, 0), haloLevel, Vector3.UnitZ);
    }

    // ------------------------------------------------------------------ sprites

    /// <summary>A street lamp: an iron post on a stepped foot with a four-sided lantern, 16 by 78.</summary>
    public static void PaintLamp(PixelCanvas c)
    {
        // Finial and the lantern's roof
        c.Rect(7, 1, 2, 2, Iron.Base);
        c.HLine(6, 3, 4, Iron.Light);
        c.HLine(5, 4, 6, Iron.Base);
        c.HLine(4, 5, 8, Iron.Dark);

        // Glass between two iron uprights; it burns all night
        c.Rect(5, 6, 6, 8, LampGlass);
        c.VLine(5, 6, 8, Color.White);
        Pix.Lit(c, 5, 6, 6, 8, ArtSheet.PublicLight);
        c.VLine(4, 6, 8, Iron.Base);
        c.VLine(11, 6, 8, Iron.Dark);
        c.HLine(4, 14, 8, Iron.Base);
        c.HLine(5, 15, 6, Iron.Dark);
        c.HLine(6, 16, 4, Iron.Dark);

        // Post with two collars, then the foot
        c.VLine(7, 17, 49, Iron.Light);
        c.VLine(8, 17, 49, Iron.Base);
        foreach (int y in new[] { 26, 58 })
        {
            c.HLine(6, y, 4, Iron.Light);
            c.HLine(6, y + 1, 4, Iron.Dark);
        }
        c.Rect(6, 66, 4, 4, Iron.Base);
        c.VLine(6, 66, 4, Iron.Light);
        c.Rect(5, 70, 6, 4, Iron.Base);
        c.VLine(5, 70, 4, Iron.Light);
        c.VLine(10, 70, 4, Iron.Dark);
        c.Rect(4, 74, 8, 4, Iron.Base);
        c.VLine(4, 74, 4, Iron.Light);
        c.VLine(11, 74, 4, Iron.Dark);
        c.HLine(4, 77, 8, Iron.Dark);
        Pix.Outline(c);
    }

    /// <summary>A red mailbox on a wooden post, with its flag up, 16 by 30.</summary>
    public static void PaintMailbox(PixelCanvas c)
    {
        // Post
        c.Rect(7, 14, 3, 16, DarkWood.Base);
        c.VLine(7, 14, 16, DarkWood.Light);
        c.VLine(9, 14, 16, DarkWood.Dark);

        // The box, rounded on top, with its door and slot
        c.Rect(3, 4, 10, 1, Red.Light);
        c.Rect(2, 5, 12, 9, Red.Base);
        c.VLine(2, 5, 9, Red.Light);
        c.VLine(13, 5, 9, Red.Dark);
        c.HLine(2, 13, 12, Red.Dark);
        Pix.Border(c, 4, 6, 8, 6, Red.Dark);
        c.HLine(5, 8, 6, Rgb(52, 44, 62));

        // Flag
        c.VLine(13, 1, 4, DarkWood.Dark);
        c.Rect(10, 1, 3, 2, Rgb(250, 210, 76));
        Pix.Outline(c);
    }

    /// <summary>A wooden tub with a clipped bush in flower, 30 by 28.</summary>
    public static void PaintPlanter(PixelCanvas c)
    {
        // The bush: a round clump in three greens, lightest on the upper left
        const float cx = 15f, cy = 9f, rx = 12f, ry = 8f;
        for (int y = 1; y <= 17; y++)
            for (int x = 2; x <= 27; x++)
            {
                float u = (x + 0.5f - cx) / rx, v = (y + 0.5f - cy) / ry;
                if (u * u + v * v > 1f) continue;
                c.Set(x, y, u + v < -0.45f ? LeafLight : u + v > 0.5f ? LeafDark : Leaf);
            }
        (int X, int Y, Color Col)[] blossoms =
        {
            (7, 5, Rgb(236, 84, 96)), (13, 3, Rgb(252, 252, 252)), (19, 5, Rgb(250, 210, 76)), (10, 9, Rgb(252, 252, 252)),
            (22, 10, Rgb(236, 84, 96)), (16, 11, Rgb(250, 210, 76)), (5, 10, Rgb(250, 210, 76))
        };
        foreach (var (x, y, col) in blossoms) c.Rect(x, y, 2, 2, col);

        // The tub: staves narrowing toward the foot, a pale rim and two iron bands
        for (int y = 16; y <= 26; y++)
        {
            int inset = (y - 16) / 4;
            for (int x = 4 + inset; x <= 25 - inset; x++)
                c.Set(x, y, (x - 4) % 4 == 3 ? Wood.Dark : x == 4 + inset ? Wood.Light : Wood.Base);
        }
        c.HLine(3, 15, 24, Wood.Light);
        c.HLine(4, 18, 22, Iron.Dark);
        c.HLine(5, 24, 20, Iron.Dark);
        Pix.Outline(c);
    }

    /// <summary>A park bench two tiles wide: slatted back and seat on iron ends, 60 by 32.</summary>
    public static void PaintBench(PixelCanvas c)
    {
        // Iron uprights behind the back, then three slats
        c.Rect(8, 2, 2, 14, Iron.Base);
        c.Rect(50, 2, 2, 14, Iron.Base);
        foreach (int y in new[] { 3, 7, 11 })
        {
            c.Rect(4, y, 52, 3, Wood.Base);
            c.HLine(4, y, 52, Wood.Light);
            c.HLine(4, y + 2, 52, Wood.Dark);
        }

        // The seat seen from above: two planks, lit, with a dark front edge
        foreach (int y in new[] { 16, 19 })
        {
            c.Rect(3, y, 54, 3, Wood.Light);
            c.HLine(3, y + 2, 54, Wood.Base);
        }
        c.HLine(3, 22, 54, Wood.Dark);

        // Arm rests and legs
        foreach (int x in new[] { 2, 55 })
        {
            c.Rect(x, 13, 3, 3, Iron.Light);
            c.Rect(x, 16, 3, 7, Iron.Base);
        }
        foreach (int x in new[] { 6, 51 })
        {
            c.Rect(x, 23, 3, 7, Iron.Base);
            c.VLine(x, 23, 7, Iron.Light);
            c.HLine(x - 1, 30, 5, Iron.Dark);
        }
        Pix.Outline(c);
    }

    /// <summary>
    /// A park bench lying north and south, seen from above its southern end, 24 by 62: the seat's slats running
    /// away from the eye, the back along its west side, iron ends.
    /// </summary>
    public static void PaintBenchSide(PixelCanvas c)
    {
        // The back: a dark rail along the west side
        c.Rect(2, 4, 4, 50, Wood.Dark);
        c.VLine(2, 4, 50, Wood.Base);
        // The seat: three slats lengthwise
        foreach (int x in new[] { 7, 11, 15 })
        {
            c.Rect(x, 6, 3, 48, Wood.Light);
            c.VLine(x + 2, 6, 48, Wood.Base);
        }
        c.VLine(18, 6, 48, Wood.Dark);
        // Iron ends and legs
        foreach (int y in new[] { 3, 52 })
        {
            c.Rect(2, y, 18, 3, Iron.Base);
            c.HLine(2, y, 18, Iron.Light);
        }
        foreach (int x in new[] { 3, 16 })
        {
            c.Rect(x, 55, 3, 6, Iron.Base);
            c.HLine(x - 1, 61, 5, Iron.Dark);
        }
        Pix.Outline(c);
    }

    /// <summary>The card an item's ball is painted on, and the ball's width on it (style guide, "Props").</summary>
    public const int ItemBallCard = 20, ItemBallSize = 18;

    /// <summary>
    /// An item lying on the ground: a Poké Ball 18 texels across on a card of 20 by 20. Red above with a lighter
    /// crescent on the upper left and a darker edge on the right, white below with its lower right in shade, a
    /// band two texels high across the middle and on it a white button in a ring of the band's colour.
    /// </summary>
    public static void PaintItemBall(PixelCanvas c)
    {
        var red = Rgb(218, 62, 58);
        var redLight = Rgb(244, 120, 104);
        var redDark = Rgb(170, 42, 56);
        var white = Rgb(240, 240, 236);
        var whiteShade = Rgb(198, 200, 214);
        var band = Rgb(58, 52, 72);

        const float middle = ItemBallCard / 2f, radius = ItemBallSize / 2f;
        for (int y = 0; y < ItemBallCard; y++)
            for (int x = 0; x < ItemBallCard; x++)
            {
                float u = (x + 0.5f - middle) / radius, v = (y + 0.5f - middle) / radius;
                if (u * u + v * v > 1f) continue;
                // The band is the two rows either side of the middle
                if (y == ItemBallCard / 2 - 1 || y == ItemBallCard / 2) c.Set(x, y, band);
                else if (v < 0f) c.Set(x, y, u + v * 1.1f < -0.72f ? redLight : u > 0.55f ? redDark : red);
                else c.Set(x, y, u + v > 0.62f ? whiteShade : white);
            }

        // The button: a ring of the band's colour, six texels across, round a white middle of four
        Pix.Disc(c, ItemBallCard / 2 - 3, ItemBallCard / 2 - 3, 6, band);
        Pix.Disc(c, ItemBallCard / 2 - 2, ItemBallCard / 2 - 2, 4, white);
        Pix.Outline(c);
    }

    /// <summary>
    /// A boulder with a smaller stone at its foot, 36 by 30: three flat shades, lightest on the upper left, with a
    /// crack running down from the top.
    /// </summary>
    public static void PaintBoulder(PixelCanvas c, bool mirrored)
    {
        var light = Rgb(196, 192, 190);
        var mid = Rgb(150, 146, 150);
        var dark = Rgb(104, 100, 112);

        void Stone(float cx, float cy, float rx, float ry, float flatBelow)
        {
            for (int y = (int)(cy - ry); y <= (int)MathF.Min(cy + ry, flatBelow); y++)
                for (int x = (int)(cx - rx); x <= (int)(cx + rx); x++)
                {
                    float u = (x + 0.5f - cx) / rx, v = (y + 0.5f - cy) / ry;
                    if (u * u + v * v > 1f) continue;
                    c.Set(x, y, u + v * 1.2f < -0.4f ? light : u + v * 0.8f > 0.5f || v > 0.62f ? dark : mid);
                }
        }
        Stone(15f, 15f, 13f, 12f, 27f);
        Stone(28f, 23f, 6f, 5f, 27f);

        // The crack: short straight runs stepping to one side
        var crack = Rgb(74, 70, 86);
        int x0 = 17;
        for (int y = 5; y <= 17; y++)
        {
            if (y is 9 or 13) x0++;
            c.Set(x0, y, crack);
            if (y is 9 or 13) c.Set(x0 - 1, y, crack);
        }
        if (mirrored) c.MirrorHorizontal();
        Pix.Outline(c);
    }

    /// <summary>
    /// The small tree that Cut removes, 26 by 40: a thin trunk and a round crown in the tall grass's greens, so
    /// it reads as a sapling beside the forest's trees.
    /// </summary>
    public static void PaintCutTree(PixelCanvas c)
    {
        var bark = Rgb(116, 80, 54);
        var barkDark = Rgb(86, 58, 42);
        var deep = Rgb(36, 110, 62);
        var leaf = Rgb(62, 150, 78);
        var light = Rgb(106, 196, 98);
        var tip = Rgb(170, 232, 130);

        c.Rect(11, 24, 4, 15, bark);
        c.VLine(14, 24, 15, barkDark);
        c.HLine(9, 38, 8, barkDark);

        // The crown: three clumps, each lit from the upper left
        void Clump(float cx, float cy, float rx, float ry)
        {
            for (int y = (int)(cy - ry); y <= (int)(cy + ry); y++)
                for (int x = (int)(cx - rx); x <= (int)(cx + rx); x++)
                {
                    float u = (x + 0.5f - cx) / rx, v = (y + 0.5f - cy) / ry;
                    if (u * u + v * v > 1f) continue;
                    c.Set(x, y, u + v < -0.7f ? light : u + v > 0.55f ? deep : leaf);
                }
        }
        Clump(13f, 16f, 11f, 10f);
        Clump(8f, 11f, 6f, 6f);
        Clump(17f, 9f, 7f, 7f);
        c.HLine(5, 7, 3, tip);
        c.HLine(14, 3, 4, tip);
        Pix.Outline(c);
    }

    /// <summary>
    /// The rock that Rock Smash breaks, 30 by 26: brown where the boulders are grey, and split by cracks that
    /// run right across it.
    /// </summary>
    public static void PaintCrackedRock(PixelCanvas c)
    {
        var light = Rgb(204, 168, 126);
        var mid = Rgb(168, 128, 92);
        var dark = Rgb(122, 90, 70);
        var crack = Rgb(78, 56, 50);
        for (int y = 2; y <= 23; y++)
            for (int x = 1; x <= 28; x++)
            {
                float u = (x + 0.5f - 15f) / 14f, v = (y + 0.5f - 14f) / 12f;
                if (u * u + v * v > 1f || y > 23) continue;
                c.Set(x, y, u + v * 1.2f < -0.4f ? light : u + v * 0.8f > 0.5f || v > 0.6f ? dark : mid);
            }

        // Two cracks from the top that meet, and one branching off toward the foot
        int x0 = 11;
        for (int y = 3; y <= 13; y++)
        {
            if (y is 6 or 10) x0++;
            c.Set(x0, y, crack);
        }
        int x1 = 20;
        for (int y = 4; y <= 13; y++)
        {
            if (y is 7 or 9 or 12) x1--;
            c.Set(x1, y, crack);
        }
        for (int y = 13; y <= 21; y++) c.Set(15 + (y - 13) / 3, y, crack);
        c.HLine(7, 16, 4, crack);
        Pix.Outline(c);
    }

    /// <summary>
    /// The boulder that Strength pushes, 30 by 30: one round stone, smooth where the others are broken, with a
    /// ring of highlight on its upper left and a single dimple.
    /// </summary>
    public static void PaintStrengthBoulder(PixelCanvas c)
    {
        var light = Rgb(214, 210, 204);
        var mid = Rgb(164, 160, 162);
        var dark = Rgb(112, 108, 122);
        for (int y = 1; y <= 28; y++)
            for (int x = 1; x <= 28; x++)
            {
                float u = (x + 0.5f - 15f) / 14f, v = (y + 0.5f - 15f) / 14f;
                float d = u * u + v * v;
                if (d > 1f) continue;
                c.Set(x, y, u + v < -0.75f ? light : u + v > 0.5f || d > 0.86f && u + v > -0.2f ? dark : mid);
            }
        // The dimple, and the glint that makes it round
        c.Rect(17, 16, 3, 2, dark);
        c.HLine(17, 18, 3, light);
        c.Rect(8, 7, 3, 2, Rgb(244, 242, 238));
        Pix.Outline(c);
    }

    /// <summary>A wooden notice board on two posts, 30 by 30.</summary>
    public static void PaintSignpost(PixelCanvas c)
    {
        foreach (int x in new[] { 6, 21 })
        {
            c.Rect(x, 16, 3, 14, DarkWood.Base);
            c.VLine(x, 16, 14, DarkWood.Light);
            c.VLine(x + 2, 16, 14, DarkWood.Dark);
        }
        Pix.Raised(c, 1, 2, 28, 16, DarkWood);
        Pix.Raised(c, 3, 4, 24, 12, Tone.Of(226, 196, 140));
        var ink = Rgb(124, 84, 58);
        c.HLine(6, 7, 18, ink);
        c.HLine(6, 10, 14, ink);
        c.HLine(6, 13, 9, ink);
        Pix.Outline(c);
    }
}

/// <summary>
/// The cards the things on the ground are drawn with (plan 02 · S2): an item's ball, and the three obstacles that
/// field moves clear, which are objects of the map like the ball (<see cref="Overworld.NPC.IsThing"/>) and no longer
/// part of a chunk's scenery, so one that is cut down or pushed along needs nothing rebuilt.
/// </summary>
public static class ThingCards
{
    /// <summary>The art of a kind of thing: an obstacle's, or an item's ball for anything else.</summary>
    public static PixelCanvas Paint(PropType kind)
    {
        var (w, h, paint) = kind switch
        {
            PropType.CutTree => (26, 40, (Action<PixelCanvas>)OutdoorProps.PaintCutTree),
            PropType.CrackedRock => (30, 26, OutdoorProps.PaintCrackedRock),
            PropType.StrengthBoulder => (30, 30, OutdoorProps.PaintStrengthBoulder),
            PropType.PunchingBag => (24, 44, GymArt.PaintPunchingBag),
            PropType.TireStack => (30, 30, GymArt.PaintTireStack),
            PropType.Bollard => (16, 28, GymArt.PaintBollard),
            PropType.Snowball => (30, 30, GymArt.PaintSnowball),
            _ => (OutdoorProps.ItemBallCard, OutdoorProps.ItemBallCard, OutdoorProps.PaintItemBall)
        };
        var art = new PixelCanvas(w, h);
        paint(art);
        return art;
    }

    /// <summary>
    /// How far into its tile, from the north, a thing stands: an item's ball at the middle, an obstacle a little
    /// south of it, where it stood as a prop.
    /// </summary>
    public static float FootOf(PropType kind) => kind switch
    {
        PropType.CutTree or PropType.StrengthBoulder => 20f / 32f,
        PropType.CrackedRock => 19f / 32f,
        PropType.PunchingBag or PropType.TireStack or PropType.Bollard or PropType.Snowball => 18f / 32f,
        _ => 0.5f
    };
}
