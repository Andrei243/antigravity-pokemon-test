using System;
using Raylib_cs;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Pixel art for rooms at the field's 32 texels per tile (style guide, "Rooms"): the floor (planks or tiles, with
/// the walls' contact shade in two flat steps) and the walls (crown moulding, wallpaper, chair rail, panelled
/// wainscot, skirting). Outdoor ground is <see cref="PixelGround"/>. Also the shared tile lookup and hash noise
/// for the field builders. No GPU calls.
/// </summary>
internal static class GroundBaker
{
    public const int ArtTile = 32;

    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    // ------------------------------------------------------------------ floors

    private static readonly Color Plank = Rgb(210, 160, 108);
    private static readonly Color PlankLight = Rgb(220, 172, 120);
    private static readonly Color PlankDark = Rgb(198, 148, 98);
    private static readonly Color PlankGroove = Rgb(156, 112, 76);

    /// <summary>Paints a room's floor into one texture laid over the 3D floor.</summary>
    public static PixelCanvas BakeInterior(Map map)
    {
        PixelCanvas c;
        if (map.Interior == InteriorStyle.Gym)
        {
            // A Gym's floor is the ground its tiles say, as the field's is (lawn, flowers, paths, earth), and its hall's
            // own tiles where its tiles are floor, wall or door (a wall's shows at the foot of an inner wall with no face
            // to the south, a door's round the exit mat; style guide, "Gyms")
            c = PixelGround.Bake(map, new TileWindow(0, 0, map.Width, map.Height), pad: 0, worldSeeds: false, out _);
            var hall = HallTiles(map.ArenaType);
            for (int ty = 0; ty < map.Height; ty++)
                for (int tx = 0; tx < map.Width; tx++)
                {
                    if (map.GetGroundTile(tx, ty) is not (TileType.Floor or TileType.Wall or TileType.Door)) continue;
                    for (int y = 0; y < ArtTile; y++)
                        for (int x = 0; x < ArtTile; x++)
                            c.SetRaw(tx * ArtTile + x, ty * ArtTile + y, HallTexel(hall, tx * ArtTile + x, ty * ArtTile + y));
                }
            // A slope of ice is streaked down its fall, so a ramp reads as one (the Snowpoint Gym's bowl)
            for (int ty = 0; ty < map.Height; ty++)
                for (int tx = 0; tx < map.Width; tx++)
                {
                    var (slopeX, slopeZ) = map.SlopeAt(tx, ty);
                    if (map.GetGroundTile(tx, ty) == TileType.Ice && (slopeX != 0f || slopeZ != 0f))
                        IceStreaks(c, tx, ty, alongX: MathF.Abs(slopeX) > MathF.Abs(slopeZ));
                }
        }
        else
        {
            c = new PixelCanvas(map.Width * ArtTile, map.Height * ArtTile);
            for (int y = 0; y < c.Height; y++)
                for (int x = 0; x < c.Width; x++)
                    c.SetRaw(x, y, FloorTexel(map.Interior, x, y));
        }

        // The walls shade the floor beside them: two flat steps
        var (lx, by) = map.RoomCorner();
        int left = lx * ArtTile, right = (map.Width - 1) * ArtTile, top = by * ArtTile, bottom = (map.Height - 1) * ArtTile;
        foreach (var (from, depth, amount) in new[] { (0, 6, 0.22f), (6, 6, 0.11f) })
        {
            Pix.Shade(c, left, top + from, right - left, depth, amount);
            Pix.Shade(c, left + from, top + 12, depth, bottom - top - 12, amount);
            Pix.Shade(c, right - from - depth, top + 12, depth, bottom - top - 12, amount);
        }

        for (int tx = 0; tx < map.Width; tx++)
            if (map.GetGroundTile(tx, map.Height - 1) == TileType.Door) ExitMat(c, tx * ArtTile, (map.Height - 1) * ArtTile, map.Interior);
        // A gate house's doors are in its side walls: the mat lies on the floor inside each, pointing at it
        for (int ty = 0; ty < map.Height - 1; ty++)
        {
            if (map.GetGroundTile(0, ty) == TileType.Door) ExitMat(c, ArtTile, ty * ArtTile, map.Interior, -1);
            if (map.GetGroundTile(map.Width - 1, ty) == TileType.Door) ExitMat(c, (map.Width - 2) * ArtTile, ty * ArtTile, map.Interior, 1);
        }
        return c;
    }

    private static Color FloorTexel(InteriorStyle style, int x, int y)
    {
        if (style == InteriorStyle.House)
        {
            // Planks 8 wide running east-west, their joints staggered row by row; whole planks vary in tone
            int row = y / 8, along = x + row * 23 % 56, plank = along / 56, j = along % 56;
            if (y % 8 == 7 || j == 0) return PlankGroove;
            uint pick = Hash(plank, row, 601) % 5;
            var tone = pick == 0 ? PlankLight : pick == 1 ? PlankDark : Plank;
            // A short line of grain on about one plank in three
            int grain = 12 + (int)(Hash(plank, row, 602) % 24);
            if (Hash(plank, row, 603) % 3 == 0 && y % 8 == 3 && j >= grain && j < grain + 7) return PixelCanvas.Shadow(tone, 0.1f);
            return tone;
        }

        var (a, b) = style switch
        {
            InteriorStyle.PokemonCenter => (Rgb(250, 242, 232), Rgb(240, 214, 212)),
            InteriorStyle.PokeMart => (Rgb(230, 238, 246), Rgb(206, 220, 236)),
            _ => (Rgb(240, 242, 246), Rgb(222, 226, 234))
        };
        // Tiles 16 square in two close tones: a grout line on the right and at the foot, a light edge on the left and top
        var tile = (x / 16 + y / 16) % 2 == 0 ? a : b;
        int lx = x % 16, ly = y % 16;
        return lx == 15 || ly == 15 ? PixelCanvas.Shadow(b, 0.15f) : lx == 0 || ly == 0 ? PixelCanvas.Light1(tile, 0.35f) : tile;
    }

    /// <summary>The two tones of a Gym hall's floor tiles, in its leader's colours (style guide, "Gyms").</summary>
    private static (Color A, Color B, Color Grout) HallTiles(PokemonType? theme) => theme switch
    {
        // Veilstone's dojo: boards of pale wood; the Pastoria Gym's pool deck: white and pale blue
        PokemonType.Fighting => (Rgb(214, 172, 118), Rgb(200, 156, 104), Rgb(150, 108, 72)),
        PokemonType.Water => (Rgb(234, 242, 248), Rgb(206, 226, 240), Rgb(150, 182, 210)),
        // The Hearthome Gym's halls: dark slate in purple and plum
        PokemonType.Ghost => (Rgb(92, 78, 118), Rgb(80, 66, 104), Rgb(52, 42, 70)),
        PokemonType.Rock or PokemonType.Steel => (Rgb(176, 160, 142), Rgb(162, 146, 128), Rgb(118, 104, 92)),
        // The Snowpoint Gym's: frosted tiles of pale blue
        PokemonType.Ice => (Rgb(222, 234, 246), Rgb(206, 222, 240), Rgb(150, 176, 212)),
        // The Sunyshore Gym's: plates of steel, a power station's floor
        PokemonType.Electric => (Rgb(150, 160, 182), Rgb(138, 148, 170), Rgb(92, 100, 124)),
        _ => (Rgb(236, 232, 214), Rgb(222, 216, 196), Rgb(172, 164, 140))
    };

    private static readonly Color Streak = Rgb(206, 232, 250), StreakShade = Rgb(124, 176, 222);

    /// <summary>
    /// Streaks down a slope of ice (style guide, "Gyms"): five light lines a texel wide along the way the slope falls,
    /// six to twelve texels long, each with a blue line of shade beside it, placed by the tile's own seed.
    /// </summary>
    private static void IceStreaks(PixelCanvas c, int tx, int ty, bool alongX)
    {
        for (int i = 0; i < 5; i++)
        {
            int across = 3 + (int)(Hash(tx, ty, 700 + i) % 26), start = 2 + (int)(Hash(tx, ty, 710 + i) % 14);
            int length = 6 + (int)(Hash(tx, ty, 720 + i) % 7);
            for (int d = 0; d < length && start + d < ArtTile - 2; d++)
            {
                int ox = tx * ArtTile, oy = ty * ArtTile;
                if (alongX)
                {
                    c.SetRaw(ox + start + d, oy + across, Streak);
                    c.SetRaw(ox + start + d, oy + across + 1, StreakShade);
                }
                else
                {
                    c.SetRaw(ox + across, oy + start + d, Streak);
                    c.SetRaw(ox + across + 1, oy + start + d, StreakShade);
                }
            }
        }
    }

    private static Color HallTexel((Color A, Color B, Color Grout) hall, int x, int y)
    {
        // Squares of 16 in two tones, a grout line at the right and the foot, a light edge at the left and the top
        var tile = (x / 16 + y / 16) % 2 == 0 ? hall.A : hall.B;
        int lx = x % 16, ly = y % 16;
        return lx == 15 || ly == 15 ? hall.Grout : lx == 0 || ly == 0 ? PixelCanvas.Light1(tile, 0.3f) : tile;
    }

    /// <summary>The mat at a room's door, with an arrow pointing out: down through the front wall, or west (-1) or
    /// east (1) through a side wall.</summary>
    private static void ExitMat(PixelCanvas c, int ox, int oy, InteriorStyle style, int sideways = 0)
    {
        var mat = Tone.Of(style == InteriorStyle.PokeMart ? Rgb(80, 132, 220) : Rgb(212, 76, 76));
        c.Rect(ox + 2, oy + 3, 28, 24, mat.Deep);
        Pix.Raised(c, ox + 3, oy + 4, 26, 22, mat);
        if (sideways == 0)
        {
            c.Rect(ox + 14, oy + 8, 4, 7, Color.White);
            for (int i = 0; i < 5; i++) c.HLine(ox + 11 + i, oy + 15 + i, 10 - i * 2, Color.White);
            return;
        }
        // The same arrow turned on its side: a shaft of 7 by 4 and a head of five columns narrowing to its point
        int shaft = sideways < 0 ? ox + 17 : ox + 8;
        c.Rect(shaft, oy + 13, 7, 4, Color.White);
        for (int i = 0; i < 5; i++)
            c.Rect(sideways < 0 ? ox + 16 - i : ox + 15 + i, oy + 10 + i, 1, 10 - i * 2, Color.White);
    }

    // ------------------------------------------------------------------ walls

    /// <summary>
    /// Paints one wall of a room, the full height of the canvas (80 texels) and as wide as the wall: crown
    /// moulding, wallpaper with a small motif, a chair rail, panelled wainscot and a skirting board.
    /// </summary>
    public static void PaintWall(PixelCanvas c, InteriorStyle style) => PaintWall(c, style, 0);

    /// <summary>
    /// A room's wall strip from <paramref name="from"/> rows below its top down to the skirting: a wall cut away
    /// part of the way up (a room's inner walls) shows only the lower part of it.
    /// </summary>
    public static void PaintWall(PixelCanvas c, InteriorStyle style, int from) => PaintWall(c, style, from, null);

    /// <summary>
    /// The same for a Gym, whose walls are in its leader's colours (<paramref name="theme"/>, the type its stage is
    /// themed on; style guide, "Gyms"): a greenhouse's green, a dark hall's violet, a dojo's wood, a pool's blue.
    /// </summary>
    public static void PaintWall(PixelCanvas c, InteriorStyle style, int from, PokemonType? theme)
    {
        var (paper, motif, wainscot, trim, skirting) = style == InteriorStyle.Gym ? GymWall(theme) : style switch
        {
            InteriorStyle.PokemonCenter => (Rgb(250, 242, 240), Rgb(242, 214, 216), Tone.Of(232, 108, 116), Tone.Of(248, 248, 250), Tone.Of(150, 70, 84)),
            InteriorStyle.PokeMart => (Rgb(234, 242, 250), Rgb(214, 226, 242), Tone.Of(76, 128, 216), Tone.Of(248, 248, 250), Tone.Of(52, 84, 150)),
            InteriorStyle.Lab => (Rgb(238, 240, 244), Rgb(224, 228, 236), Tone.Of(150, 160, 186), Tone.Of(250, 250, 252), Tone.Of(96, 104, 128)),
            _ => (Rgb(246, 232, 200), Rgb(232, 212, 172), Tone.Of(178, 124, 82), Tone.Of(250, 244, 226), Tone.Of(120, 84, 58))
        };

        const int crown = 6, rail = 48, panels = 52, skirt = 74;
        int w = c.Width, h = c.Height;
        for (int ry = 0; ry < h; ry++)
            for (int x = 0; x < w; x++)
            {
                int y = ry + from;
                Color col;
                if (y < crown) col = y == 0 ? trim.Light : y == crown - 1 ? trim.Dark : trim.Base;
                else if (y < rail)
                {
                    // Wallpaper: a four-texel motif on a grid 16 by 14, every other row of them half a step along
                    int row = (y - crown) / 14, my = (y - crown) % 14, mx = (x + row % 2 * 8) % 16;
                    col = (mx is 7 or 8) && (my is 6 or 7) ? motif : y == crown ? PixelCanvas.Shadow(paper, 0.14f) : paper;
                }
                else if (y < panels) col = y == rail ? trim.Light : y == panels - 1 ? trim.Dark : trim.Base;
                else if (y < skirt)
                {
                    // Wainscot: a sunken panel in every 16 texels, between a joint and its light edge
                    int px = x % 16, py = y - panels;
                    col = px == 15 ? wainscot.Dark : px == 0 ? wainscot.Light
                        : py == 0 ? PixelCanvas.Shadow(wainscot.Base, 0.18f)
                        : px >= 3 && px <= 12 && py >= 3 && py <= 18
                            ? (px == 3 || py == 3 ? wainscot.Dark : px == 12 || py == 18 ? wainscot.Light : wainscot.Base)
                            : wainscot.Base;
                }
                else col = y == skirt ? skirting.Light : ry == h - 1 ? skirting.Dark : skirting.Base;
                c.SetRaw(x, ry, col);
            }
    }

    private static (Color Paper, Color Motif, Tone Wainscot, Tone Trim, Tone Skirting) GymWall(PokemonType? theme) => theme switch
    {
        // Eterna's greenhouse: pale green glass between white frames, over a wainscot of green-stained boards
        PokemonType.Grass => (Rgb(214, 238, 214), Rgb(190, 224, 192), Tone.Of(96, 156, 96), Tone.Of(250, 252, 248), Tone.Of(66, 112, 70)),
        PokemonType.Ghost => (Rgb(74, 58, 96), Rgb(96, 76, 124), Tone.Of(60, 44, 80), Tone.Of(148, 120, 176), Tone.Of(40, 30, 56)),
        PokemonType.Fighting => (Rgb(240, 226, 196), Rgb(224, 206, 170), Tone.Of(150, 98, 60), Tone.Of(196, 146, 96), Tone.Of(104, 66, 44)),
        PokemonType.Water => (Rgb(214, 234, 248), Rgb(188, 218, 242), Tone.Of(70, 130, 200), Tone.Of(248, 250, 254), Tone.Of(44, 88, 150)),
        PokemonType.Rock => (Rgb(196, 186, 170), Rgb(178, 168, 152), Tone.Of(132, 108, 86), Tone.Of(214, 206, 192), Tone.Of(94, 76, 62)),
        // Canalave's mine shaft: steel sheeting over a wainscot painted red
        PokemonType.Steel => (Rgb(164, 170, 182), Rgb(146, 152, 166), Tone.Of(176, 58, 50), Tone.Of(214, 220, 228), Tone.Of(90, 94, 106)),
        // Snowpoint's: frost white over a wainscot of glacier blue
        PokemonType.Ice => (Rgb(226, 236, 248), Rgb(204, 220, 242), Tone.Of(92, 142, 204), Tone.Of(244, 248, 254), Tone.Of(56, 92, 150)),
        // Sunyshore's: night blue with a motif and a rail of electric yellow, over a wainscot of steel
        PokemonType.Electric => (Rgb(54, 64, 104), Rgb(236, 206, 84), Tone.Of(118, 126, 148), Tone.Of(236, 206, 84), Tone.Of(40, 46, 70)),
        _ => (Rgb(238, 240, 244), Rgb(224, 228, 236), Tone.Of(150, 160, 186), Tone.Of(250, 250, 252), Tone.Of(96, 104, 128))
    };

    /// <summary>The dark top of a cut-away wall, with a paler line along its inner edge.</summary>
    public static void PaintWallTop(PixelCanvas c)
    {
        var cap = Tone.Of(62, 56, 74);
        c.Rect(0, 0, c.Width, c.Height, cap.Base);
        Pix.Border(c, 0, 0, c.Width, c.Height, cap.Light);
    }

    /// <summary>
    /// The dark top of a tile of a room's inner wall: the paler line only along the sides that look onto the floor,
    /// so a wall of many tiles reads as one.
    /// </summary>
    public static void PaintWallTop(PixelCanvas c, bool north, bool south, bool west, bool east)
    {
        var cap = Tone.Of(62, 56, 74);
        c.Rect(0, 0, c.Width, c.Height, cap.Base);
        if (north) c.HLine(0, 0, c.Width, cap.Light);
        if (south) c.HLine(0, c.Height - 1, c.Width, cap.Light);
        if (west) c.VLine(0, 0, c.Height, cap.Light);
        if (east) c.VLine(c.Width - 1, 0, c.Height, cap.Light);
    }

    // ------------------------------------------------------------------ shared lookups

    /// <summary>Tile type used for scenery outside the map: roads and water continue, everything else is forest.</summary>
    public static TileType? TypeAt(Map map, int x, int y)
    {
        if (map.InBounds(x, y)) return map.GetGroundTile(x, y);
        if (map.IsIndoors) return null;

        var edge = map.GetGroundTile(Math.Clamp(x, 0, map.Width - 1), Math.Clamp(y, 0, map.Height - 1));
        return edge switch
        {
            TileType.Path => TileType.Path,
            TileType.Water => TileType.Water,
            // Past its edge a cave is rock, as the open country is forest, and the Distortion World is nothing
            _ => map.IsCave ? TileType.CaveWall : map.IsVoid ? TileType.Void : TileType.Tree
        };
    }

    /// <summary>
    /// Whether water lies at a tile: open water, or the water a bridge's deck crosses, which runs on under the
    /// boards (the deck is drawn over it, and the river's shore is not broken by the bridge).
    /// </summary>
    public static bool IsWaterAt(Map map, int x, int y)
    {
        var type = TypeAt(map, x, y);
        return type == TileType.Water || (type is TileType.Planks or TileType.Walkway && map.InBounds(x, y) && map.IsDeepWater(x, y));
    }

    private static uint Hash(int x, int y, int salt)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + salt * 1442695041);
            h = (h ^ (h >> 13)) * 1274126177u;
            return h ^ (h >> 16);
        }
    }

    public static float Rand01(int x, int y, int salt) => (Hash(x, y, salt) & 0xFFFF) / 65536f;
}
