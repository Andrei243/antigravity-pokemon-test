using System;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The art that Sinnoh's other towns and its landmarks need beyond the first towns' (plan 01 · M4): more wall
/// materials, windows for stone and for Team Galactic, porches, roofs under snow and of sheet metal, and the
/// pieces of towers and lighthouses. The same rules as the rest: flat shades, one-texel bevels, no noise.
/// </summary>
internal static partial class BuildingArt
{
    private static readonly Tone Boards = Tone.Of(188, 144, 98, 210, 168, 120, 136, 98, 70);
    private static readonly Tone Beam = Tone.Of(110, 78, 58, 140, 102, 74, 82, 58, 48);
    private static readonly Color BeamPlaster = Rgb(240, 230, 206);
    private static readonly Tone Ashlar = Tone.Of(176, 172, 170, 198, 194, 190, 124, 120, 130);
    private static readonly Tone AshlarAlt = Tone.Of(160, 156, 158, 184, 180, 178, 116, 112, 124);
    private static readonly Tone OldStone = Tone.Of(150, 150, 160, 174, 174, 182, 104, 104, 122);
    private static readonly Tone OldStoneAlt = Tone.Of(138, 140, 152, 162, 164, 174, 98, 98, 116);
    private static readonly Tone PaleStone = Tone.Of(214, 210, 204, 232, 230, 224, 160, 156, 162);
    private static readonly Tone PaleStoneAlt = Tone.Of(202, 198, 194, 222, 220, 214, 152, 148, 156);
    private static readonly Tone Log = Tone.Of(156, 110, 72, 188, 140, 94, 108, 74, 56);
    private static readonly Tone Stucco = Tone.Of(244, 242, 236, 252, 252, 248, 206, 208, 220);
    private static readonly Tone Metal = Tone.Of(150, 160, 176, 178, 188, 200, 112, 122, 144);
    private static readonly Tone DarkPanel = Tone.Of(104, 116, 130, 132, 144, 156, 70, 80, 98);
    private static readonly Color SlitGlass = Rgb(120, 230, 220);
    private static readonly Color SnowDrift = Rgb(204, 216, 238);
    private static readonly Color SnowShade = Rgb(188, 202, 230);

    /// <summary>How far above the ground an open porch's canopy hangs: clear of the door under it.</summary>
    public static int CanopyUnder(int porchHeight) => porchHeight - 22;

    /// <summary>What a porch is built of: plaster where the style has an entrance block of its own, else the building's walls.</summary>
    private static WallKind PorchWall(BuildingStyle s) => s.Pitched && s.Portal > 0 ? WallKind.Plaster : s.Wall;

    /// <summary>The height of a porch: an entrance block's under a pitched roof, one storey under a flat one.</summary>
    public static int PorchHeight(BuildingStyle s) => s.Pitched ? PortalHeight : GroundStorey + TopBand;

    // ------------------------------------------------------------------ materials

    /// <summary>The texel of one of the materials added for Sinnoh's other towns; rows count up from the foot of the wall.</summary>
    private static Color TownWallTexel(WallKind kind, int x, int up, int seed)
    {
        switch (kind)
        {
            case WallKind.Boards:
            {
                int row = up / 8, r = up % 8;
                int j = (x + (row * 17 + seed * 5) % 44) % 44;
                return r == 0 ? Boards.Dark : r == 7 ? Boards.Light : j == 0 ? Boards.Dark : j == 1 ? Boards.Light : Boards.Base;
            }
            case WallKind.HalfTimber:
            {
                // Posts three texels wide every sixteen, and a rail at window height in every storey
                int col = (x + Inset) % 16, r = up % UpperStorey;
                if (col < 3) return col == 0 ? Beam.Light : col == 2 ? Beam.Dark : Beam.Base;
                if (r >= 24 && r < 27) return r == 26 ? Beam.Light : r == 24 ? Beam.Dark : Beam.Base;
                return BeamPlaster;
            }
            case WallKind.Stone or WallKind.OldStone or WallKind.PaleStone:
            {
                var (main, alt) = kind switch
                {
                    WallKind.OldStone => (OldStone, OldStoneAlt),
                    WallKind.PaleStone => (PaleStone, PaleStoneAlt),
                    _ => (Ashlar, AshlarAlt)
                };
                // Blocks 16 by 8, every other course half a block along; one block in five a shade darker
                int row = up / 8, r = up % 8;
                int along = x + (row % 2) * 8, block = along / 16, bx = along % 16;
                var t = (block * 3 + row * 7 + seed) % 5 == 0 ? alt : main;
                return r == 0 ? t.Dark : r == 7 ? t.Light : bx == 15 ? t.Dark : bx == 0 ? t.Light : t.Base;
            }
            case WallKind.Log:
            {
                // Logs 8 tall: lit on top, dark underneath, butt ends every 48 staggered row by row
                int row = up / 8, r = up % 8;
                int j = (x + (row * 19 + seed * 7) % 48) % 48;
                return r <= 1 ? Log.Dark : r == 7 ? Log.Light : j == 0 ? Log.Dark : j == 1 ? Log.Light : Log.Base;
            }
            case WallKind.Metal:
            {
                // Upright ribs four texels wide and a seam a bay apart
                if ((x + Inset) % Bay == Bay - 1) return Metal.Deep;
                int rib = x % 4;
                return rib == 0 ? Metal.Light : rib == 3 ? Metal.Dark : Metal.Base;
            }
            case WallKind.DarkPanel:
            {
                int r = up % 18, col = (x + Inset) % Bay;
                return r == 0 || col == Bay - 1 ? DarkPanel.Dark : r == 17 || col == 0 ? DarkPanel.Light : DarkPanel.Base;
            }
            default:
                return Stucco.Base;
        }
    }

    /// <summary>The braces of a half-timbered wall: one across the lower part of each end bay, two texels thick.</summary>
    private static void Braces(PixelCanvas c, int x, int y, int w, int h)
    {
        if (w < 48 || h < 26) return;
        int foot = y + h - 1;
        // Rising toward the middle of the wall from the foot of each corner post, up to the rail
        for (int i = 0; i <= 12; i++)
        {
            int rise = i * 23 / 12;
            c.Rect(x + 3 + i, foot - rise - 1, 1, 2, Beam.Base);
            c.Rect(x + w - 4 - i, foot - rise - 1, 1, 2, Beam.Base);
        }
    }

    // ------------------------------------------------------------------ windows and doors

    /// <summary>A slit of lit glass in a dark frame, 6 by 20: a tower's window, or one of Team Galactic's.</summary>
    private static void SlitWindow(PixelCanvas c, int cx, int sillY, byte light, bool teal)
    {
        int x0 = cx - 3, y0 = sillY - 20;
        c.Rect(x0, y0, 6, 20, Iron.Base);
        c.VLine(x0, y0, 20, Iron.Light);
        c.VLine(x0 + 5, y0, 20, Iron.Dark);
        if (teal)
        {
            for (int py = 0; py < 16; py++)
                for (int px = 0; px < 2; px++)
                    c.SetRaw(x0 + 2 + px, y0 + 2 + py, Flag(px == 0 ? SlitGlass : PixelCanvas.Shadow(SlitGlass, 0.2f), light == 255 ? ArtSheet.PublicLight : light));
        }
        else Glass(c, x0 + 2, y0 + 2, 2, 16, light);
    }

    /// <summary>A window with a round head in a stone surround, 16 by 26, for walls of stone and plaster.</summary>
    private static void ArchedWindow(PixelCanvas c, int cx, int sillY, byte light)
    {
        int x0 = cx - 8, y0 = sillY - 26;
        // The surround narrows by a texel at each of its top two rows
        Pix.Raised(c, x0, y0 + 3, 16, 23, StoneAlt);
        c.Rect(x0 + 1, y0 + 1, 14, 2, StoneAlt.Base);
        c.Rect(x0 + 3, y0, 10, 1, StoneAlt.Light);
        c.HLine(x0 + 1, y0 + 1, 14, StoneAlt.Light);
        Glass(c, x0 + 3, y0 + 5, 10, 19, light, curtains: false);
        c.Rect(x0 + 4, y0 + 3, 8, 2, Flag(light == 255 ? GlassShut : GlassLight, light));
        c.VLine(x0 + 7, y0 + 3, 21, Frame);
        c.VLine(x0 + 8, y0 + 3, 21, FrameShade);
        c.HLine(x0 + 3, y0 + 13, 10, Frame);
        Sill(c, x0 - 1, sillY, 18);
    }

    /// <summary>
    /// The Foreign Building's rose window, 22 across: a stone ring round eight panes of coloured glass that
    /// glow all night.
    /// </summary>
    private static void RoseWindow(PixelCanvas c, int cx, int cy)
    {
        const int size = 22;
        int x0 = cx - size / 2, y0 = cy - size / 2;
        Pix.Disc(c, x0, y0, size, StoneAlt.Dark);
        Pix.Disc(c, x0 + 1, y0 + 1, size - 2, StoneAlt.Light);
        Color[] panes = { Rgb(226, 84, 96), Rgb(250, 206, 84), Rgb(84, 150, 232), Rgb(98, 200, 122) };
        float r = (size - 6) / 2f;
        for (int py = 0; py < size - 6; py++)
            for (int px = 0; px < size - 6; px++)
            {
                float dx = px + 0.5f - r, dy = py + 0.5f - r;
                if (dx * dx + dy * dy > r * r) continue;
                // Four panes round the middle, split again along the diagonals
                int quarter = (dx >= 0 ? 1 : 0) + (dy >= 0 ? 2 : 0);
                bool outer = MathF.Abs(dx) > MathF.Abs(dy);
                var col = panes[(quarter + (outer ? 1 : 0)) % panes.Length];
                c.SetRaw(x0 + 3 + px, y0 + 3 + py, Flag(col, ArtSheet.PublicLight));
            }
        // The tracery: a cross and a hub
        c.VLine(cx - 1, y0 + 3, size - 6, StoneAlt.Base);
        c.VLine(cx, y0 + 3, size - 6, StoneAlt.Dark);
        c.HLine(x0 + 3, cy - 1, size - 6, StoneAlt.Base);
        c.HLine(x0 + 3, cy, size - 6, StoneAlt.Dark);
        Pix.Disc(c, cx - 3, cy - 3, 6, StoneAlt.Light);
    }

    /// <summary>A sliding door of ribbed metal on a rail, 52 by 40: works and warehouses.</summary>
    private static void SlidingDoor(PixelCanvas c, int cx, int groundY, bool timber = false)
    {
        int x0 = cx - 26, y0 = groundY - 40;
        if (timber)
        {
            // A pair of barn doors in boards under a beam, each with two rails and a brace from its foot to its far rail
            c.Rect(x0 - 2, y0 - 4, 56, 4, Beam.Base);
            c.HLine(x0 - 2, y0 - 4, 56, Beam.Light);
            Pix.Raised(c, x0, y0, 52, 40, Boards);
            for (int px = 6; px < 52; px += 6) c.VLine(x0 + px, y0 + 1, 38, Boards.Dark);
            foreach (int leaf in new[] { x0, x0 + 26 })
            {
                c.Rect(leaf + 1, y0 + 6, 24, 3, Beam.Base);
                c.Rect(leaf + 1, y0 + 31, 24, 3, Beam.Base);
                c.HLine(leaf + 1, y0 + 6, 24, Beam.Light);
                c.HLine(leaf + 1, y0 + 31, 24, Beam.Light);
                for (int i = 0; i < 22; i++) c.Rect(leaf + 2 + i, y0 + 30 - i * 21 / 22, 2, 1, Beam.Base);
            }
            c.VLine(x0 + 25, y0, 40, Beam.Dark);
            c.Rect(x0 + 21, y0 + 18, 2, 5, Iron.Dark);
            c.Rect(x0 + 29, y0 + 18, 2, 5, Iron.Dark);
            return;
        }
        c.Rect(x0 - 2, y0 - 3, 56, 3, Iron.Base);
        c.HLine(x0 - 2, y0 - 3, 56, Iron.Light);
        var door = Tone.Of(PixelCanvas.Shadow(Metal.Base, 0.18f));
        Pix.Raised(c, x0, y0, 52, 40, door);
        for (int px = 4; px < 52; px += 6) c.VLine(x0 + px, y0 + 2, 36, door.Dark);
        c.VLine(x0 + 25, y0, 40, door.Deep);
        c.VLine(x0 + 26, y0, 40, door.Light);
        c.Rect(x0 + 21, y0 + 18, 2, 6, Iron.Dark);
        c.Rect(x0 + 29, y0 + 18, 2, 6, Iron.Dark);
    }

    /// <summary>
    /// A loft door of boards standing on <paramref name="sillY"/>, 22 by 24 in a frame of beams; over the top one a
    /// beam juts out with a rope hanging from it to a hook beside the door.
    /// </summary>
    private static void LoftDoor(PixelCanvas c, int cx, int sillY, bool top)
    {
        int x0 = cx - 11, y0 = sillY - 24;
        c.Rect(x0 - 2, y0 - 2, 26, 28, Beam.Base);
        c.HLine(x0 - 2, y0 - 2, 26, Beam.Light);
        Pix.Raised(c, x0, y0, 22, 24, Boards);
        for (int px = 5; px < 22; px += 5) c.VLine(x0 + px, y0 + 1, 22, Boards.Dark);
        c.Rect(x0 + 1, y0 + 10, 20, 2, Beam.Base);
        if (!top) return;
        // The hoist: a beam's end over the door, the rope down beside it and the hook at its foot
        c.Rect(cx - 4, y0 - 9, 8, 6, Beam.Base);
        c.HLine(cx - 4, y0 - 9, 8, Beam.Light);
        c.Rect(cx - 4, y0 - 4, 8, 1, Beam.Dark);
        c.VLine(x0 + 25, y0 - 6, 18, Rgb(198, 176, 132));
        c.Rect(x0 + 24, y0 + 12, 3, 2, Iron.Base);
        c.Rect(x0 + 26, y0 + 14, 1, 2, Iron.Base);
    }

    /// <summary>Team Galactic's mark as this game draws it: a yellow lozenge with a dark core, <paramref name="size"/> across.</summary>
    private static void GalacticMark(PixelCanvas c, int cx, int y, int size, Color yellow)
    {
        int half = size / 2;
        for (int py = 0; py < size; py++)
        {
            int span = half - Math.Abs(py - half);
            c.Rect(cx - span, y + py, Math.Max(1, span * 2), 1, yellow);
        }
        c.Rect(cx - 2, y + half - 2, 4, 4, Ink);
        Pix.Lit(c, cx - half, y, size, size, ArtSheet.PublicLight);
        c.Rect(cx - 2, y + half - 2, 4, 4, Ink);
    }

    /// <summary>A band in the accent colour right across a wall, four texels tall, with a dark line under it.</summary>
    private static void AccentBand(PixelCanvas c, BuildingStyle s, int y, int w)
    {
        var t = Tone.Of(s.Accent);
        c.Rect(0, y, w, 4, t.Base);
        c.HLine(0, y, w, t.Light);
        c.HLine(0, y + 4, w, t.Deep);
    }

    // ------------------------------------------------------------------ porches

    /// <summary>
    /// The front of a closed porch: the entrance block as wide as its tiles, with the building's doors in it and,
    /// under a pitched roof, the sign on its header. <paramref name="firstTile"/> is the x of its westmost tile.
    /// </summary>
    public static void PaintPorch(PixelCanvas c, BuildingInfo b, BuildingStyle s, int firstTile)
    {
        int w = c.Width, h = c.Height;
        Wall(c, PorchWall(s), 0, 0, w, h - BaseHeight, b.X0 * 3 + b.Y0);
        Base(c, s, 0, h - BaseHeight, w);

        int top = 0;
        if (s.Pitched)
        {
            var accent = Tone.Of(s.Accent);
            Pix.Raised(c, 0, 0, w, HeaderHeight, accent);
            c.HLine(1, HeaderHeight - 2, w - 2, accent.Dark);
            c.HLine(0, HeaderHeight, w, Plaster.Dark);
            Sign(c, s, w / 2, 3, HeaderHeight - 7, w - 8);
            top = HeaderHeight + 1;
        }
        else Cornice(c, s with { Sign = SignKind.None }, 0, 0, w);

        // The art starts where the porch does: inside the wall's inset only when it starts at the building's corner
        int shift = firstTile == b.X0 ? Inset : 0;
        byte light = WindowLight(b, s);
        foreach (var (x, target) in b.Doors)
        {
            int cx = (x - firstTile) * Bay + Bay / 2 - shift;
            Door(c, cx, h, s, target != null ? ArtSheet.PublicLight : (byte)255);
        }
        // Windows in the bays that have no door and none beside them
        for (int i = 0; i * Bay < w; i++)
        {
            int tile = firstTile + i;
            if (b.Doors.Exists(d => Math.Abs(d.X - tile) <= 1) || s.Window is WindowKind.None or WindowKind.Slit) continue;
            int cx = i * Bay + Bay / 2 - shift;
            if (cx - 14 < 2 || cx + 14 > w - 2 || h - top < 44) continue;
            ShopWindow(c, cx, h - 13, light);
        }
    }

    /// <summary>One side of an open porch: a short block of the building's wall on its base.</summary>
    public static void PaintPorchFlank(PixelCanvas c, BuildingStyle s, int seed)
    {
        Wall(c, PorchWall(s), 0, 0, c.Width, c.Height - BaseHeight, seed);
        Base(c, s, 0, c.Height - BaseHeight, c.Width);
    }

    /// <summary>The front of the canopy between an open porch's sides: the accent colour, with the building's sign.</summary>
    public static void PaintCanopy(PixelCanvas c, BuildingStyle s, bool sign)
    {
        var accent = Tone.Of(s.Accent);
        Pix.Raised(c, 0, 0, c.Width, c.Height, accent);
        c.HLine(1, c.Height - 2, c.Width - 2, accent.Dark);
        if (sign) Sign(c, s, c.Width / 2, 3, c.Height - 6, c.Width - 8);
    }

    // ------------------------------------------------------------------ roofs of snow and of metal

    /// <summary>
    /// Snow lying on a roof, 64 by 64 and repeating: a pale sheet with drift lines three to six texels long, and
    /// a blue shade over the two rows above each eighth row, where the tiles' feet would be.
    /// </summary>
    private static PixelCanvas SnowSheet()
    {
        var c = new PixelCanvas(64, 64);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
                c.SetRaw(x, y, y % 32 >= 30 ? SnowShade : SnowRoof);
        for (int row = 0; row < 8; row++)
            for (int i = 0; i < 3; i++)
            {
                int len = 3 + (row * 5 + i * 3) % 4;
                int x = (row * 23 + i * 21 + 5) % (64 - len), y = row * 8 + 2 + (i * 2 + row) % 4;
                if (y % 32 >= 29) continue;
                c.Rect(x, y, len, 1, SnowDrift);
            }
        return c;
    }

    /// <summary>A roof of ribbed metal sheets, 64 by 64 and repeating: ribs eight texels apart running down the slope.</summary>
    private static PixelCanvas MetalSheet()
    {
        var c = new PixelCanvas(64, 64);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                int rib = x % 8;
                c.SetRaw(x, y, y % 32 == 31 ? Metal.Deep : rib == 0 ? Metal.Light : rib == 7 ? Metal.Dark : Metal.Base);
            }
        return c;
    }

    // ------------------------------------------------------------------ on the roof

    /// <summary>A smokestack's side: brick, with a dark rim and two pale bands near the top.</summary>
    public static void PaintStack(PixelCanvas c)
    {
        Wall(c, WallKind.Brick, 0, 0, c.Width, c.Height, 3);
        c.VLine(0, 0, c.Height, Brick.Light);
        c.VLine(c.Width - 1, 0, c.Height, Brick.Dark);
        Pix.Raised(c, 0, 0, c.Width, 4, Iron);
        foreach (int y in new[] { 9, 17 }) Pix.Raised(c, 0, y, c.Width, 4, Tone.Of(Mortar));
    }

    /// <summary>One of the spikes on Team Galactic's roofs: a sprite of grey steel coming to a point.</summary>
    public static void PaintSpike(PixelCanvas c)
    {
        int w = c.Width, h = c.Height;
        for (int y = 0; y < h; y++)
        {
            int half = Math.Max(1, (y + 1) * (w / 2) / h);
            for (int x = w / 2 - half; x < w / 2 + half; x++)
                c.Set(x, y, x < w / 2 - half / 3 ? Steel.Light : x >= w / 2 + half / 3 ? Steel.Dark : Steel.Base);
        }
        Pix.Outline(c);
    }

    /// <summary>A solar panel lying on a roof: dark blue cells in a pale frame.</summary>
    public static void PaintSolar(PixelCanvas c)
    {
        Pix.Raised(c, 0, 0, c.Width, c.Height, Steel);
        for (int y = 2; y + 6 <= c.Height - 1; y += 7)
            for (int x = 2; x + 8 <= c.Width - 1; x += 9)
            {
                c.Rect(x, y, 8, 6, Rgb(52, 96, 170));
                c.HLine(x, y, 8, Rgb(110, 160, 224));
            }
    }

    /// <summary>The small spire on the Foreign Building's ridge: a sprite, slate over a stone foot.</summary>
    public static void PaintSpire(PixelCanvas c)
    {
        int w = c.Width, h = c.Height, mid = w / 2;
        var slate = Tone.Of(92, 104, 140);
        Pix.Raised(c, mid - 5, h - 12, 10, 12, StoneAlt);
        c.Rect(mid - 2, h - 9, 4, 6, Ink);
        for (int y = 2; y < h - 12; y++)
        {
            int half = 1 + (y - 2) * 5 / (h - 14);
            for (int x = mid - half; x < mid + half; x++) c.Set(x, y, x < mid ? slate.Light : slate.Base);
        }
        c.Rect(mid - 1, 0, 2, 3, Brass.Light);
        Pix.Outline(c);
    }

    // ------------------------------------------------------------------ tiers, towers and lighthouses

    /// <summary>
    /// One face of an upper tier (a tower's stage, the League's stepped storeys): the wall material with a ledge
    /// along its top, a band in the accent colour where the style has one, and windows a bay apart.
    /// </summary>
    public static void PaintTier(PixelCanvas c, BuildingStyle s, int seed, byte light)
    {
        int w = c.Width, h = c.Height;
        Wall(c, s.Wall, 0, 0, w, h, seed);
        var ledge = s.Wall is WallKind.Stucco ? Stucco : s.Wall == WallKind.PaleStone ? PaleStone : StoneAlt;
        Pix.Raised(c, 0, 0, w, 4, ledge);
        Pix.Shade(c, 0, 4, w, 2, 0.2f);
        if (s.Band) AccentBand(c, s, 7, w);

        if (h < 30) return;
        int count = Math.Max(1, w / Bay);
        for (int i = 0; i < count; i++)
        {
            int cx = w * (2 * i + 1) / (2 * count);
            if (s.Window == WindowKind.Arched && h >= 40) ArchedWindow(c, cx, h - 6, light);
            else SlitWindow(c, cx, h - 6, light, teal: false);
        }
    }

    /// <summary>The red band round a lighthouse's shaft: painted over a tier's face.</summary>
    public static void PaintTierBand(PixelCanvas c, Color color)
    {
        var t = Tone.Of(color);
        int y = c.Height / 2 - 7;
        c.Rect(0, y, c.Width, 14, t.Base);
        c.HLine(0, y, c.Width, t.Light);
        c.HLine(0, y + 13, c.Width, t.Dark);
    }

    /// <summary>One face of a lighthouse's lantern room: glass that burns all night between iron uprights.</summary>
    public static void PaintLantern(PixelCanvas c)
    {
        int w = c.Width, h = c.Height;
        c.Rect(0, 0, w, h, Iron.Base);
        for (int x = 2; x + 6 <= w - 1; x += 8)
        {
            c.Rect(x, 2, 6, h - 4, LanternGlass);
            c.VLine(x, 2, h - 4, Color.White);
            Pix.Lit(c, x, 2, 6, h - 4, ArtSheet.PublicLight);
        }
        c.HLine(0, 0, w, Iron.Light);
        c.HLine(0, h - 1, w, Iron.Dark);
    }

    /// <summary>The top of a tier or a gallery seen from above: the ledge's stone with a light south and west edge.</summary>
    public static void PaintTierTop(PixelCanvas c, BuildingStyle s)
    {
        var t = s.RoofColor.Equals(SnowRoof) ? Tone.Of(SnowRoof) : s.Wall is WallKind.Stucco ? Stucco : s.Wall == WallKind.PaleStone ? PaleStone : StoneAlt;
        c.Rect(0, 0, c.Width, c.Height, t.Base);
        c.HLine(0, c.Height - 1, c.Width, t.Light);
        c.VLine(0, 0, c.Height, t.Light);
        c.VLine(c.Width - 1, 0, c.Height, t.Dark);
    }
}
