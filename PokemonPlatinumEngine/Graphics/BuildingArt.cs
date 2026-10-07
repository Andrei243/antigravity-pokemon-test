using System;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

internal enum WallKind { Planks, Clapboard, Plaster, Brick, Panel, Boards, HalfTimber, Stone, OldStone, PaleStone, Log, Stucco, Metal, DarkPanel }

internal enum RoofShape { Gable, Hip, Flat }

internal enum WindowKind { Cottage, Shop, Sash, Ribbon, Slit, Arched, None }

/// <summary>What a building's sign shows: one of the fixed ones, any name (<see cref="BuildingStyle.SignText"/>), or a mark.</summary>
internal enum SignKind { None, Center, Mart, Lab, School, Poketch, Tv, Globe, Text, Galactic }

[Flags]
internal enum RoofGear { None = 0, Vents = 1, Mast = 2, Dish = 4, Globe = 8, Skylight = 16, Stack = 32, Spikes = 64, Solar = 128, Spire = 256 }

/// <summary>How one building is put together: its wall material, roof, windows, door, sign and extras.</summary>
internal sealed record BuildingStyle
{
    public WallKind Wall { get; init; } = WallKind.Planks;
    public RoofShape Roof { get; init; } = RoofShape.Gable;
    public Color RoofColor { get; init; }
    public int Storeys { get; init; } = 1;
    public WindowKind Window { get; init; } = WindowKind.Cottage;
    public bool GlassDoor { get; init; }

    /// <summary>A sliding door of ribbed metal, two bays wide: works and warehouses. In a wall of logs, a pair of barn doors in boards.</summary>
    public bool SlidingDoor { get; init; }

    /// <summary>
    /// A loft door in each storey above the main door, under a beam with a rope and a hook to hoist goods in by: a
    /// harbour's storehouse (Snowpoint City).
    /// </summary>
    public bool Loft { get; init; }

    public SignKind Sign { get; init; }

    /// <summary>The name a <see cref="SignKind.Text"/> sign spells.</summary>
    public string? SignText { get; init; }

    /// <summary>Width in texels of the entrance block that stands proud of the wall and carries the sign; 0 for none.</summary>
    public int Portal { get; init; }

    public bool Chimney { get; init; }
    public bool Shutters { get; init; }
    public bool FlowerBoxes { get; init; }
    public bool Balconies { get; init; }

    /// <summary>The colour of sign headers, stripes and shutters.</summary>
    public Color Accent { get; init; }

    public RoofGear Gear { get; init; }

    /// <summary>Somebody lives here: the windows go dark late at night (in most homes).</summary>
    public bool Home { get; init; }

    /// <summary>A round window of coloured glass high in the front wall.</summary>
    public bool RoseWindow { get; init; }

    /// <summary>A band in the accent colour along the top of the walls.</summary>
    public bool Band { get; init; }

    /// <summary>A pitched roof steeper than a house's.</summary>
    public bool Steep { get; init; }

    /// <summary>Eaves twice as deep as a house's.</summary>
    public bool DeepEaves { get; init; }

    /// <summary>Storeys that step back on top of a flat roof, each smaller than the one below.</summary>
    public int Tiers { get; init; }

    public bool Pitched => Roof != RoofShape.Flat;

    /// <summary>Height of the walls in texels.</summary>
    public int WallHeight => Pitched
        ? BuildingArt.PitchedWall + (Storeys - 1) * BuildingArt.UpperStorey
        : BuildingArt.GroundStorey + (Storeys - 1) * BuildingArt.UpperStorey + BuildingArt.TopBand;
}

/// <summary>
/// The pixel art of buildings (style guide, "Buildings"): wall materials, windows, doors, signs and roof tiles,
/// and the composition of a whole wall face from them, bay by bay. Flat shades and one-texel bevels throughout;
/// glass that lights up after dark is marked in its alpha (<see cref="ArtSheet.PublicLight"/>,
/// <see cref="ArtSheet.HomeLight"/>). No GPU calls.
/// </summary>
internal static partial class BuildingArt
{
    /// <summary>One tile of wall.</summary>
    public const int Bay = 32;

    /// <summary>How far the walls stand inside the footprint on each side.</summary>
    public const int Inset = 2;

    public const int BaseHeight = 7;

    /// <summary>Wall height under a pitched roof; the top ten or so texels are hidden by the eave.</summary>
    public const int PitchedWall = 56;

    public const int GroundStorey = 44, UpperStorey = 32, TopBand = 18;

    /// <summary>The entrance block rises above the eave and stands this far in front of the wall.</summary>
    public const int PortalHeight = 74, PortalDepth = 10;

    private const int HeaderHeight = 24;

    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    private static readonly Tone Planks = Tone.Of(214, 170, 118, 232, 194, 146, 150, 106, 74);
    private static readonly Tone PlanksDark = Tone.Of(198, 152, 104, 218, 176, 128, 150, 106, 74);
    private static readonly Tone Clapboard = Tone.Of(240, 236, 226, 252, 250, 244, 196, 196, 210);
    private static readonly Tone Plaster = Tone.Of(242, 232, 208, 250, 244, 226, 220, 206, 180);
    private static readonly Tone Brick = Tone.Of(190, 106, 84, 210, 130, 100, 156, 82, 72);
    private static readonly Color Mortar = Rgb(226, 206, 184);
    private static readonly Tone Panel = Tone.Of(214, 222, 232, 234, 240, 246, 150, 162, 184);
    private static readonly Tone Stone = Tone.Of(158, 152, 150, 190, 186, 182, 104, 100, 112);
    private static readonly Tone StoneAlt = Tone.Of(172, 166, 160, 200, 196, 190, 112, 108, 118);
    private static readonly Tone Granite = Tone.Of(120, 124, 140, 150, 154, 168, 84, 86, 104);
    private static readonly Tone Timber = Tone.Of(124, 84, 58, 156, 110, 76, 92, 62, 48);
    private static readonly Tone DoorWood = Tone.Of(150, 98, 62, 178, 124, 82, 108, 70, 50);
    private static readonly Tone Steel = Tone.Of(200, 206, 218, 232, 236, 242, 140, 148, 168);
    private static readonly Tone Brass = Tone.Of(214, 172, 96, 244, 214, 140, 150, 112, 66);
    private static readonly Tone Iron = Tone.Of(72, 74, 92, 106, 110, 130, 48, 48, 64);

    /// <summary>The stone of doorsteps.</summary>
    public static Tone StepStone => StoneAlt;

    private static readonly Color Frame = Rgb(248, 246, 238);
    private static readonly Color FrameShade = Rgb(204, 204, 214);
    private static readonly Color GlassLight = Rgb(170, 218, 246);
    private static readonly Color GlassBase = Rgb(122, 190, 236);
    private static readonly Color GlassDark = Rgb(88, 156, 220);
    private static readonly Color GlassStreak = Rgb(232, 246, 255);
    private static readonly Color GlassShut = Rgb(74, 104, 150);
    private static readonly Color Curtain = Rgb(244, 236, 214);
    private static readonly Color LanternGlass = Rgb(252, 240, 190);
    private static readonly Color Leaf = Rgb(60, 146, 76);
    private static readonly Color LeafLight = Rgb(106, 196, 98);
    private static readonly Color Ink = Rgb(52, 44, 62);

    /// <summary>
    /// How this building's windows are lit after dark: shops and offices all night, homes only in the evening,
    /// except about one home in three where someone stays up.
    /// </summary>
    public static byte WindowLight(BuildingInfo b, BuildingStyle s) =>
        !s.Home || (b.X0 * 7 + b.Y0 * 13) % 3 == 0 ? ArtSheet.PublicLight : ArtSheet.HomeLight;

    // ------------------------------------------------------------------ bays

    public enum BayKind { Blank, Door, Plaque, Window }

    /// <summary>
    /// What stands in each tile of the front wall: the doors and plaques the map gives, windows in the other bays
    /// except those right beside a door. A building without a door gets a closed entrance in a middle bay.
    /// </summary>
    public static BayKind[] BaysOf(BuildingInfo b)
    {
        var bays = new BayKind[b.Width];
        for (int i = 0; i < b.Width; i++)
        {
            int x = b.X0 + i;
            // Behind a porch's walls the front wall is blank: a closed porch has the doors in its own front
            bays[i] = b.Porch.Contains(x) ? BayKind.Blank
                : b.Doors.Exists(d => d.X == x) ? BayKind.Door
                : b.Plaques.Contains(x) ? BayKind.Plaque
                : b.Doors.Exists(d => Math.Abs(d.X - x) == 1) ? BayKind.Blank
                : BayKind.Window;
        }
        // A wing has no entrance, and a gate house on a road east to west has its doors in its ends
        if (b.Doors.Count == 0 && !b.Annex && b.SideDoors.Count == 0)
        {
            int entrance = ClosedEntrance(b);
            bays[entrance] = BayKind.Door;
            foreach (int beside in new[] { entrance - 1, entrance + 1 })
                if (beside >= 0 && beside < bays.Length && bays[beside] == BayKind.Window) bays[beside] = BayKind.Blank;
        }
        return bays;
    }

    /// <summary>The bay (0 = the building's west end) where a building without a door shows its closed entrance.</summary>
    public static int ClosedEntrance(BuildingInfo b)
    {
        int middle = b.Width / 2;
        foreach (int i in new[] { middle, middle + 1, middle - 1 })
            if (i >= 0 && i < b.Width && !b.Plaques.Contains(b.X0 + i)) return i;
        return middle;
    }

    /// <summary>The middle of bay <paramref name="i"/> in a front wall's art.</summary>
    public static int BayCenter(int i) => i * Bay + Bay / 2 - Inset;

    public static int FrontWidth(BuildingInfo b) => b.Width * Bay - 2 * Inset;

    // ------------------------------------------------------------------ whole faces

    /// <summary>Paints a building's front wall; the canvas is <see cref="FrontWidth"/> by the style's wall height.</summary>
    public static void PaintFront(PixelCanvas c, BuildingInfo b, BuildingStyle s)
    {
        int w = c.Width, h = c.Height;
        int seed = b.X0 * 7 + b.Y0 * 13;
        byte light = WindowLight(b, s);
        var bays = BaysOf(b);

        Wall(c, s.Wall, 0, 0, w, h - BaseHeight, seed);
        Base(c, s, 0, h - BaseHeight, w);
        if (s.Wall is WallKind.Planks or WallKind.Boards)
        {
            Post(c, 0, 0, h - BaseHeight);
            Post(c, w - 4, 0, h - BaseHeight);
        }

        // Where the door goes when it is a sliding one: it takes the bay to its east as well
        bool sliding = s.SlidingDoor && bays.Length >= 2;

        if (s.Pitched)
        {
            for (int i = 0; i < bays.Length; i++)
            {
                int cx = BayCenter(i);
                switch (bays[i])
                {
                    case BayKind.Door when sliding:
                        SlidingDoor(c, Math.Clamp(cx, 30, w - 30), h, timber: s.Wall == WallKind.Log);
                        break;
                    case BayKind.Door when s.Portal == 0:
                        Door(c, cx, h, s, DoorLight(b, i, light));
                        Lantern(c, cx + 13, h - 33);
                        break;
                    case BayKind.Plaque:
                        Plaque(c, cx, h - 32);
                        break;
                    case BayKind.Window:
                        // The end bays keep their shutters clear of the corner posts
                        Window(c, cx + (i == 0 ? 4 : i == bays.Length - 1 ? -4 : 0), h - 17, s, light);
                        break;
                }
                // Every storey above has a window in every bay, or over a storehouse's door its loft door
                for (int k = 1; k < s.Storeys; k++)
                {
                    if (s.Loft && bays[i] == BayKind.Door) LoftDoor(c, sliding ? Math.Clamp(cx, 30, w - 30) : cx, h - 17 - k * UpperStorey, top: k == s.Storeys - 1);
                    else Window(c, cx + (i == 0 ? 4 : i == bays.Length - 1 ? -4 : 0), h - 17 - k * UpperStorey, s with { FlowerBoxes = false }, light);
                }
            }
            if (s.RoseWindow) RoseWindow(c, w / 2, 30);
            EaveShade(c, 0, w);
            return;
        }

        // A block of storeys under a flat roof: a cornice band with the sign, then a row of windows per storey
        int groundTop = h - GroundStorey;
        Cornice(c, s, 0, 0, w);
        if (s.Sign != SignKind.None) Sign(c, s, w / 2, 2, TopBand - 4, w - 8);
        if (s.Band) AccentBand(c, s, TopBand, w);
        for (int k = 1; k < s.Storeys; k++)
        {
            int top = groundTop - k * UpperStorey;
            if (s.Window == WindowKind.Ribbon) Ribbon(c, 6, top + 6, w - 12, 18, light);
            else
                for (int i = 0; i < bays.Length; i++)
                {
                    UpperWindow(c, BayCenter(i), top + 26, s, light);
                    if (s.Balconies) Balcony(c, BayCenter(i), top + 28);
                }
            StoreyLine(c, s, top + UpperStorey - 2, w);
        }
        if (s.Sign == SignKind.Tv) Screen(c, w / 2 - 40, groundTop - (s.Storeys - 1) * UpperStorey + 4, 80, 24);

        for (int i = 0; i < bays.Length; i++)
        {
            int cx = BayCenter(i);
            switch (bays[i])
            {
                case BayKind.Door when sliding:
                    SlidingDoor(c, Math.Clamp(cx, 30, w - 30), h);
                    break;
                case BayKind.Door:
                    Door(c, cx, h, s, DoorLight(b, i, light));
                    break;
                case BayKind.Plaque:
                    Plaque(c, cx, h - 30);
                    break;
                case BayKind.Window:
                    GroundWindow(c, cx, h, s, light);
                    break;
            }
        }
    }

    /// <summary>A window of an upper storey under a flat roof, standing on a sill at <paramref name="sillY"/>.</summary>
    private static void UpperWindow(PixelCanvas c, int cx, int sillY, BuildingStyle s, byte light)
    {
        switch (s.Window)
        {
            case WindowKind.None: break;
            case WindowKind.Slit: SlitWindow(c, cx, sillY - 2, light, teal: s.Wall == WallKind.DarkPanel); break;
            case WindowKind.Arched: ArchedWindow(c, cx, sillY, light); break;
            default: Sash(c, cx, sillY, light); break;
        }
    }

    /// <summary>A window of the ground storey under a flat roof.</summary>
    private static void GroundWindow(PixelCanvas c, int cx, int groundY, BuildingStyle s, byte light)
    {
        switch (s.Window)
        {
            case WindowKind.None: break;
            case WindowKind.Slit: SlitWindow(c, cx, groundY - 16, light, teal: s.Wall == WallKind.DarkPanel); break;
            case WindowKind.Arched: ArchedWindow(c, cx, groundY - 13, light); break;
            case WindowKind.Sash: Sash(c, cx, groundY - 14, light); break;
            default: ShopWindow(c, cx, groundY - 13, light); break;
        }
    }

    /// <summary>A door that leads nowhere (a building the player can't enter yet) is shut and dark.</summary>
    private static byte DoorLight(BuildingInfo b, int bay, byte light) =>
        b.Doors.Exists(d => d.X == b.X0 + bay) ? light : (byte)255;

    /// <summary>
    /// Paints a side wall, <paramref name="wallHeight"/> texels tall at the bottom of the canvas; whatever is
    /// above that is the gable end under a pitched roof.
    /// </summary>
    /// <param name="doorsAt">
    /// Where the doors of this wall are, in texels along it from its left as it is seen from outside; empty for a
    /// wall without any (every wall but a gate house's ends).
    /// </param>
    public static void PaintSide(PixelCanvas c, BuildingInfo b, BuildingStyle s, int wallHeight, IReadOnlyList<int>? doorsAt = null)
    {
        int w = c.Width, h = c.Height, gable = h - wallHeight;
        int seed = b.X0 * 7 + b.Y0 * 13 + 3;
        byte light = WindowLight(b, s);

        Wall(c, s.Wall, 0, 0, w, h - BaseHeight, seed);
        Base(c, s, 0, h - BaseHeight, w);
        if (s.Wall is WallKind.Planks or WallKind.Boards)
        {
            Post(c, 0, gable, wallHeight - BaseHeight);
            Post(c, w - 4, gable, wallHeight - BaseHeight);
        }

        int bays = Math.Max(1, w / Bay);
        bool HasDoor(int cx) => doorsAt != null && doorsAt.Any(d => Math.Abs(d - cx) < Bay);
        foreach (int at in doorsAt ?? Array.Empty<int>()) Door(c, Math.Clamp(at, 20, w - 20), h, s, ArtSheet.PublicLight);

        if (s.Pitched)
        {
            var plain = s with { Shutters = false, FlowerBoxes = false };
            for (int k = 0; k < s.Storeys; k++)
                if (k > 0 || !HasDoor(w / 2)) Window(c, w / 2, h - 17 - k * UpperStorey, plain, light);
            if (gable >= 16) AtticWindow(c, w / 2 - 5, gable - 12, light);
            if (s.Roof == RoofShape.Hip) EaveShade(c, 0, w);
            return;
        }

        Cornice(c, s with { Sign = SignKind.None }, 0, 0, w);
        if (s.Band) AccentBand(c, s, TopBand, w);
        int groundTop = h - GroundStorey;
        for (int k = 1; k < s.Storeys; k++)
        {
            int top = groundTop - k * UpperStorey;
            if (s.Window == WindowKind.Ribbon) Ribbon(c, 6, top + 6, w - 12, 18, light);
            else
                for (int i = 0; i < bays; i++) UpperWindow(c, i * Bay + Bay / 2, top + 26, s, light);
            StoreyLine(c, s, top + UpperStorey - 2, w);
        }
        for (int i = 0; i < bays; i++)
            if (!HasDoor(i * Bay + Bay / 2)) GroundWindow(c, i * Bay + Bay / 2, h, s, light);
    }

    /// <summary>Paints the front of the entrance block: a header in the accent colour with the sign, and the door under it.</summary>
    public static void PaintPortal(PixelCanvas c, BuildingInfo b, BuildingStyle s)
    {
        int w = c.Width, h = c.Height;
        Wall(c, WallKind.Plaster, 0, 0, w, h - BaseHeight, b.X0 * 3 + b.Y0);
        Base(c, s, 0, h - BaseHeight, w);

        var accent = Tone.Of(s.Accent);
        Pix.Raised(c, 0, 0, w, HeaderHeight, accent);
        c.HLine(1, HeaderHeight - 2, w - 2, accent.Dark);
        c.HLine(0, HeaderHeight, w, Plaster.Dark);
        Sign(c, s, w / 2, 3, HeaderHeight - 7, w - 6);

        // Pilasters either side of the door
        Pix.Raised(c, 0, HeaderHeight + 1, 3, h - HeaderHeight - 1 - BaseHeight, Plaster);
        Pix.Raised(c, w - 3, HeaderHeight + 1, 3, h - HeaderHeight - 1 - BaseHeight, Plaster);
        Door(c, w / 2, h, s, ArtSheet.PublicLight);
        if (!s.GlassDoor)
        {
            Lantern(c, w / 2 - 28, h - 34);
            Lantern(c, w / 2 + 23, h - 34);
        }
    }

    /// <summary>The plain side of the entrance block.</summary>
    public static void PaintPortalSide(PixelCanvas c, BuildingStyle s)
    {
        int w = c.Width, h = c.Height;
        Wall(c, WallKind.Plaster, 0, 0, w, h - BaseHeight, 5);
        Base(c, s, 0, h - BaseHeight, w);
        var accent = Tone.Of(s.Accent);
        Pix.Raised(c, 0, 0, w, HeaderHeight, accent);
        c.HLine(0, HeaderHeight, w, Plaster.Dark);
    }

    // ------------------------------------------------------------------ materials

    private static void Wall(PixelCanvas c, WallKind kind, int x, int y, int w, int h, int seed)
    {
        bool own = kind <= WallKind.Panel;
        for (int py = 0; py < h; py++)
        {
            // Rows are counted up from the foot of the wall, so courses meet at the corners whatever the height
            int up = h - 1 - py;
            for (int px = 0; px < w; px++)
                c.SetRaw(x + px, y + py, own ? WallTexel(kind, px, up, seed) : TownWallTexel(kind, px, up, seed));
        }
        if (kind == WallKind.Plaster) TrowelMarks(c, x, y, w, h, seed, Plaster);
        if (kind == WallKind.Stucco) TrowelMarks(c, x, y, w, h, seed, Stucco);
        if (kind == WallKind.HalfTimber) Braces(c, x, y, w, h);
    }

    private static Color WallTexel(WallKind kind, int x, int up, int seed)
    {
        switch (kind)
        {
            case WallKind.Planks:
            {
                // Boards 8 tall: groove at the foot, light line on top; butt joints staggered row by row
                int row = up / 8, r = up % 8;
                int along = x + (row * 17 + seed * 5) % 44, board = along / 44, j = along % 44;
                var t = (board * 3 + row * 7 + seed) % 5 == 0 ? PlanksDark : Planks;
                return r == 0 ? t.Dark : r == 7 ? t.Light : j == 0 ? t.Dark : j == 1 ? t.Light : t.Base;
            }
            case WallKind.Clapboard:
            {
                int r = up % 6;
                return r == 0 ? Clapboard.Dark : r == 5 ? Clapboard.Light : Clapboard.Base;
            }
            case WallKind.Brick:
            {
                // Courses 4 tall (mortar at the foot), bricks 8 wide, every other course half a brick along
                int row = up / 4, r = up % 4;
                if (r == 0) return Mortar;
                int along = x + (row % 2) * 4, brick = along / 8;
                if (along % 8 == 7) return Mortar;
                return (brick * 5 + row * 3 + seed) % 7 == 0 ? Brick.Dark : (brick * 3 + row * 7 + seed) % 11 == 0 ? Brick.Light : Brick.Base;
            }
            case WallKind.Panel:
            {
                // Panels a bay wide and 18 tall: joint on the right and at the foot, a light line on the left and top
                int r = up % 18, col = (x + Inset) % Bay;
                return r == 0 || col == Bay - 1 ? Panel.Dark : r == 17 || col == 0 ? Panel.Light : Panel.Base;
            }
            default:
                return Plaster.Base;
        }
    }

    /// <summary>A few short marks where the trowel caught the plaster, on a coarse grid.</summary>
    private static void TrowelMarks(PixelCanvas c, int x, int y, int w, int h, int seed, Tone tone)
    {
        for (int cy = 0; cy * 12 + 8 < h; cy++)
            for (int cx = 0; cx * 16 + 12 < w; cx++)
            {
                float pick = GroundBaker.Rand01(cx + seed, cy, 301);
                if (pick > 0.4f) continue;
                int len = 3 + (int)(GroundBaker.Rand01(cx + seed, cy, 302) * 3);
                int mx = x + cx * 16 + 2 + (int)(GroundBaker.Rand01(cx + seed, cy, 303) * 8);
                int my = y + cy * 12 + 2 + (int)(GroundBaker.Rand01(cx + seed, cy, 304) * 7);
                c.HLine(mx, my, Math.Min(len, x + w - mx), pick < 0.2f ? tone.Light : tone.Dark);
            }
    }

    /// <summary>The base course: stone blocks under timber and plaster, a granite plinth under city blocks.</summary>
    private static void Base(PixelCanvas c, BuildingStyle s, int x, int y, int w)
    {
        bool city = s.Wall is WallKind.Panel or WallKind.Brick or WallKind.Stone or WallKind.OldStone or WallKind.PaleStone or WallKind.Metal or WallKind.DarkPanel;
        for (int bx = 0; bx < w; bx += 16)
        {
            var tone = city ? Granite : bx / 16 % 3 == 1 ? StoneAlt : Stone;
            Pix.Raised(c, x + bx, y, Math.Min(16, w - bx), BaseHeight, tone);
        }
    }

    private static void Post(PixelCanvas c, int x, int y, int h)
    {
        c.Rect(x, y, 4, h, Timber.Base);
        c.VLine(x, y, h, Timber.Light);
        c.VLine(x + 3, y, h, Timber.Dark);
    }

    /// <summary>The wall just under an eave sits in its shadow: two flat steps.</summary>
    private static void EaveShade(PixelCanvas c, int x, int w)
    {
        Pix.Shade(c, x, 0, w, 14, 0.3f);
        Pix.Shade(c, x, 14, w, 4, 0.15f);
    }

    /// <summary>
    /// The band along the top of a block: a pale cap, then a fascia (in the accent colour where the building
    /// carries a sign, so the name reads like a shop front), then a dark line and its shadow on the wall.
    /// </summary>
    private static void Cornice(PixelCanvas c, BuildingStyle s, int x, int y, int w)
    {
        var cap = s.Wall switch
        {
            WallKind.Brick => Tone.Of(Mortar),
            WallKind.Stone or WallKind.OldStone => StoneAlt,
            WallKind.PaleStone => PaleStone,
            WallKind.Metal => Metal,
            WallKind.DarkPanel => DarkPanel,
            WallKind.Stucco or WallKind.Plaster => Stucco,
            _ => Panel
        };
        c.HLine(x, y, w, cap.Light);
        c.HLine(x, y + 1, w, cap.Base);
        Pix.Raised(c, x, y + 2, w, TopBand - 4, s.Sign != SignKind.None ? Tone.Of(s.Accent) : cap);
        c.HLine(x, y + TopBand - 2, w, cap.Deep);
        Pix.Shade(c, x, y + TopBand - 1, w, 2, 0.2f);
    }

    private static void StoreyLine(PixelCanvas c, BuildingStyle s, int y, int w)
    {
        // Sheet metal runs on from storey to storey without a line
        if (s.Wall == WallKind.Metal) return;
        var tone = s.Wall switch
        {
            WallKind.Brick => Tone.Of(Mortar),
            WallKind.Stone or WallKind.OldStone => StoneAlt,
            WallKind.PaleStone => PaleStone,
            WallKind.DarkPanel => DarkPanel,
            WallKind.Stucco or WallKind.Plaster => Stucco,
            _ => Panel
        };
        c.HLine(0, y, w, tone.Light);
        c.HLine(0, y + 1, w, tone.Dark);
    }

    // ------------------------------------------------------------------ glass

    private static Color Flag(Color col, byte light) => new(col.R, col.G, col.B, light);

    /// <summary>
    /// A pane in three flat blues split along the diagonal, with the frame's shadow along its top and left and
    /// one bright streak; <paramref name="light"/> marks it as lit after dark (255 for glass that stays dark).
    /// </summary>
    private static void Glass(PixelCanvas c, int x, int y, int w, int h, byte light, bool curtains = false)
    {
        bool shut = light == 255;
        for (int py = 0; py < h; py++)
            for (int px = 0; px < w; px++)
            {
                int d = px * h + py * w;
                var col = shut ? (d < w * h * 0.8f ? GlassShut : PixelCanvas.Shadow(GlassShut, 0.2f))
                    : px == 0 || py == 0 ? GlassDark
                    : d < w * h * 0.7f ? GlassLight
                    : d > w * h * 1.35f ? GlassDark
                    : GlassBase;
                c.SetRaw(x + px, y + py, Flag(col, light));
            }

        // The streak sits in the upper left of a bare pane, and low on the right where curtains hang
        int streak = Math.Min(5, Math.Min(w, h) - 3);
        if (streak < 3) streak = 0;
        int sx = curtains ? w / 2 + 1 : 2, sy = curtains ? h - 3 : 2 + streak;
        for (int i = 0; i < streak; i++)
        {
            c.SetRaw(x + sx + i, y + sy - i, Flag(shut ? GlassBase : GlassStreak, light));
            if (w >= 12 && !shut && !curtains) c.SetRaw(x + sx + 3 + i, y + sy - i, Flag(GlassStreak, light));
        }

        if (!curtains) return;
        // Short curtains hanging straight at each side of the upper panes, narrower on narrow windows
        int widest = w >= 14 ? 3 : 2;
        for (int r = 0; r < Math.Min(9, h); r++)
        {
            int span = r == 8 ? widest - 1 : widest;
            for (int i = 0; i < span; i++)
            {
                c.SetRaw(x + i, y + r, Flag(Curtain, light));
                c.SetRaw(x + w - 1 - i, y + r, Flag(Curtain, light));
            }
        }
    }

    /// <summary>A house window standing on a sill at <paramref name="sillY"/>: four panes, with shutters and a flower box where the style has them.</summary>
    private static void Window(PixelCanvas c, int cx, int sillY, BuildingStyle s, byte light)
    {
        if (s.Window == WindowKind.None) return;
        if (s.Window == WindowKind.Shop) { ShopWindow(c, cx, sillY + 4, light); return; }
        if (s.Window == WindowKind.Sash) { Sash(c, cx, sillY + 3, light); return; }
        if (s.Window == WindowKind.Arched) { ArchedWindow(c, cx, sillY + 3, light); return; }
        if (s.Window == WindowKind.Slit) { SlitWindow(c, cx, sillY, light, teal: s.Wall == WallKind.DarkPanel); return; }

        int x0 = cx - 10, y0 = sillY - 22;
        if (s.Shutters)
        {
            Shutter(c, x0 - 4, y0, Tone.Of(PixelCanvas.Shadow(s.Accent, 0.12f)));
            Shutter(c, x0 + 20, y0, Tone.Of(PixelCanvas.Shadow(s.Accent, 0.12f)));
        }
        c.Rect(x0, y0, 20, 22, Frame);
        c.HLine(x0, y0 + 21, 20, FrameShade);
        c.VLine(x0 + 19, y0, 22, FrameShade);
        Glass(c, x0 + 2, y0 + 2, 16, 18, light, curtains: true);
        c.VLine(x0 + 9, y0 + 2, 18, Frame);
        c.VLine(x0 + 10, y0 + 2, 18, FrameShade);
        c.HLine(x0 + 2, y0 + 10, 16, Frame);
        c.HLine(x0 + 2, y0 + 11, 16, FrameShade);
        Sill(c, x0 - 2, sillY, 24);
        if (s.FlowerBoxes) FlowerBox(c, x0, sillY + 2, 20);
    }

    private static void Sill(PixelCanvas c, int x, int y, int w)
    {
        c.HLine(x, y, w, Frame);
        c.HLine(x, y + 1, w, FrameShade);
        Pix.Shade(c, x + 1, y + 2, w - 1, 1, 0.3f);
    }

    private static void Shutter(PixelCanvas c, int x, int y, Tone t)
    {
        Pix.Raised(c, x, y, 4, 22, t);
        for (int r = 3; r < 20; r += 3) c.HLine(x + 1, y + r, 2, t.Dark);
    }

    /// <summary>A wooden box hung under a window, with leaves and blossoms showing over its rim.</summary>
    private static void FlowerBox(PixelCanvas c, int x, int y, int w)
    {
        Pix.Raised(c, x, y + 2, w, 5, DoorWood);
        c.HLine(x + 1, y + 1, w - 2, Leaf);
        Color[] blossoms = { Rgb(236, 84, 96), Rgb(252, 252, 252), Rgb(250, 210, 76) };
        for (int i = 0; i * 5 + 3 < w; i++)
        {
            int fx = x + 1 + i * 5;
            c.Rect(fx, y, 2, 1, LeafLight);
            c.Rect(fx + 1, y - 2, 2, 2, blossoms[i % blossoms.Length]);
        }
    }

    /// <summary>A wide shop window: two panes in a white frame over a sill.</summary>
    private static void ShopWindow(PixelCanvas c, int cx, int sillY, byte light)
    {
        int x0 = cx - 13, y0 = sillY - 22;
        c.Rect(x0, y0, 26, 22, Frame);
        c.HLine(x0, y0 + 21, 26, FrameShade);
        c.VLine(x0 + 25, y0, 22, FrameShade);
        Glass(c, x0 + 2, y0 + 2, 22, 18, light);
        c.VLine(x0 + 12, y0 + 2, 18, Frame);
        c.VLine(x0 + 13, y0 + 2, 18, FrameShade);
        Sill(c, x0 - 1, sillY, 28);
    }

    /// <summary>A tall sash window with a stone lintel, for brick walls.</summary>
    private static void Sash(PixelCanvas c, int cx, int sillY, byte light)
    {
        int x0 = cx - 8, y0 = sillY - 22;
        Pix.Raised(c, x0 - 1, y0 - 3, 18, 3, StoneAlt);
        c.Rect(x0, y0, 16, 22, Frame);
        c.HLine(x0, y0 + 21, 16, FrameShade);
        c.VLine(x0 + 15, y0, 22, FrameShade);
        Glass(c, x0 + 2, y0 + 2, 12, 18, light, curtains: true);
        c.HLine(x0 + 2, y0 + 10, 12, Frame);
        c.HLine(x0 + 2, y0 + 11, 12, FrameShade);
        Sill(c, x0 - 1, sillY, 18);
    }

    /// <summary>A band of glass running the width of a storey, with a mullion every half bay.</summary>
    private static void Ribbon(PixelCanvas c, int x, int y, int w, int h, byte light)
    {
        Pix.Raised(c, x, y, w, h, Steel);
        for (int px = 2; px < w - 2; px += 16)
        {
            int pane = Math.Min(14, w - 2 - px);
            if (pane >= 6) Glass(c, x + px, y + 2, pane, h - 4, light);
        }
        Pix.Shade(c, x + 1, y + h, w - 1, 1, 0.25f);
    }

    private static void Balcony(PixelCanvas c, int cx, int y)
    {
        int x0 = cx - 11;
        c.HLine(x0, y, 22, Iron.Light);
        c.HLine(x0, y + 1, 22, Iron.Base);
        for (int i = 0; i < 22; i += 3) c.VLine(x0 + i, y + 2, 5, Iron.Base);
        c.VLine(x0 + 21, y + 2, 5, Iron.Base);
        c.HLine(x0, y + 7, 22, Iron.Dark);
    }

    /// <summary>A small round window in a gable end.</summary>
    private static void AtticWindow(PixelCanvas c, int x, int y, byte light)
    {
        Pix.Disc(c, x, y, 10, Frame);
        for (int py = 0; py < 6; py++)
            for (int px = 0; px < 6; px++)
            {
                float dx = px - 2.5f, dy = py - 2.5f;
                if (dx * dx + dy * dy <= 9.5f) c.SetRaw(x + 2 + px, y + 2 + py, Flag(px + py < 5 ? GlassLight : GlassBase, light));
            }
    }

    /// <summary>The television station's big screen: a test card of colour bars that glows after dark.</summary>
    private static void Screen(PixelCanvas c, int x, int y, int w, int h)
    {
        Pix.Raised(c, x, y, w, h, Iron);
        Color[] bars = { Rgb(240, 240, 236), Rgb(250, 214, 76), Rgb(96, 206, 220), Rgb(98, 200, 110), Rgb(226, 110, 190), Rgb(230, 84, 80), Rgb(80, 120, 226) };
        int inner = w - 6, bar = inner / bars.Length;
        for (int i = 0; i < bars.Length; i++)
        {
            int bw = i == bars.Length - 1 ? inner - bar * i : bar;
            for (int py = 0; py < h - 6; py++)
                for (int px = 0; px < bw; px++)
                    c.SetRaw(x + 3 + i * bar + px, y + 3 + py, Flag(py >= h - 12 ? PixelCanvas.Shadow(bars[i], 0.3f) : bars[i], ArtSheet.PublicLight));
        }
    }

    // ------------------------------------------------------------------ doors, plaques, lanterns

    private static void Door(PixelCanvas c, int cx, int groundY, BuildingStyle s, byte light)
    {
        if (s.SlidingDoor) SlidingDoor(c, cx, groundY);
        else if (s.GlassDoor) GlassDoor(c, cx, groundY, light);
        else if (s.Portal > 0) DoubleDoor(c, cx, groundY, light);
        else WoodDoor(c, cx, groundY, light);
    }

    /// <summary>A house door, 22 by 39: pale frame, a small window, two rows of sunken panels and a brass knob.</summary>
    private static void WoodDoor(PixelCanvas c, int cx, int groundY, byte light)
    {
        int x0 = cx - 11, y0 = groundY - 39;
        c.Rect(x0, y0, 22, 39, Frame);
        c.VLine(x0 + 21, y0, 39, FrameShade);
        Leaf18(c, x0 + 2, y0 + 2, light, knobOnRight: true);
    }

    /// <summary>A pair of wooden doors, 40 by 39, for the school.</summary>
    private static void DoubleDoor(PixelCanvas c, int cx, int groundY, byte light)
    {
        int x0 = cx - 20, y0 = groundY - 39;
        c.Rect(x0, y0, 40, 39, Frame);
        c.VLine(x0 + 39, y0, 39, FrameShade);
        Leaf18(c, x0 + 2, y0 + 2, light, knobOnRight: true);
        Leaf18(c, x0 + 20, y0 + 2, light, knobOnRight: false);
    }

    private static void Leaf18(PixelCanvas c, int x, int y, byte light, bool knobOnRight)
    {
        Pix.Raised(c, x, y, 18, 37, DoorWood);
        c.Rect(x + 3, y + 2, 12, 10, DoorWood.Dark);
        Glass(c, x + 4, y + 3, 10, 8, light);
        c.VLine(x + 9, y + 3, 8, DoorWood.Dark);
        c.HLine(x + 4, y + 7, 10, DoorWood.Dark);
        foreach (int px in new[] { x + 2, x + 10 })
        {
            Pix.Sunken(c, px, y + 14, 6, 9, DoorWood);
            Pix.Sunken(c, px, y + 25, 6, 10, DoorWood);
        }
        int kx = knobOnRight ? x + 14 : x + 2;
        c.Rect(kx, y + 23, 2, 2, Brass.Light);
        c.HLine(kx, y + 24, 2, Brass.Base);
    }

    /// <summary>Double glass doors in a steel frame, 36 by 40, under a strip of transom glass.</summary>
    private static void GlassDoor(PixelCanvas c, int cx, int groundY, byte light)
    {
        int x0 = cx - 18, y0 = groundY - 40;
        Pix.Raised(c, x0, y0, 36, 40, Steel);
        Glass(c, x0 + 2, y0 + 2, 32, 4, light);
        c.HLine(x0 + 2, y0 + 6, 32, Steel.Dark);
        Glass(c, x0 + 2, y0 + 7, 15, 30, light);
        Glass(c, x0 + 19, y0 + 7, 15, 30, light);
        c.VLine(x0 + 17, y0 + 7, 30, Steel.Light);
        c.VLine(x0 + 18, y0 + 7, 30, Steel.Dark);
        c.Rect(x0 + 14, y0 + 19, 2, 8, Steel.Dark);
        c.Rect(x0 + 20, y0 + 19, 2, 8, Steel.Dark);
        c.Rect(x0 + 2, y0 + 37, 32, 3, Steel.Base);
        c.HLine(x0 + 2, y0 + 37, 32, Steel.Dark);
    }

    // What shows through an open door: the dark of the room, a little lighter along the sill where daylight falls in
    private static readonly Color Doorway = Rgb(40, 34, 48), DoorwaySill = Rgb(74, 62, 76);

    private static void Room(PixelCanvas c, int x, int y, int w, int h, byte light)
    {
        for (int py = 0; py < h; py++)
            for (int px = 0; px < w; px++)
                c.SetRaw(x + px, y + py, Flag(py >= h - 4 ? DoorwaySill : Doorway, light));
    }

    /// <summary>
    /// A door standing ajar (<paramref name="frame"/> 1) or open (2), exactly the size of the shut door it is
    /// drawn over: glass doors slide apart in their steel frame, a wooden leaf (or a <paramref name="pair"/>)
    /// swings in against its hinges. The room behind is marked like a window (<paramref name="light"/>), so
    /// after dark it is the room's light that shows in the doorway.
    /// </summary>
    internal static PixelCanvas OpenDoor(bool glass, bool pair, int frame, byte light)
    {
        if (glass)
        {
            var g = new PixelCanvas(36, 40);
            GlassDoor(g, 18, 40, light);
            // The panes have slid a third of the way apart, then nearly all of it
            int gap = frame == 1 ? 10 : 26, x = 18 - gap / 2;
            Room(g, x, 7, gap, 30, light);
            g.VLine(x - 1, 7, 30, Steel.Dark);
            g.VLine(x + gap, 7, 30, Steel.Light);
            return g;
        }

        int width = pair ? 40 : 22;
        var c = new PixelCanvas(width, 39);
        c.Rect(0, 0, width, 39, Frame);
        c.VLine(width - 1, 0, 39, FrameShade);
        Room(c, 2, 2, width - 4, 37, light);
        // The leaf seen at a slant while it swings, and edge-on against its hinges once it is open
        int leaf = frame == 1 ? 9 : 3;
        Pix.Raised(c, 2, 2, leaf, 37, DoorWood);
        Pix.Shade(c, 2, 2, leaf, 37, 0.22f);
        if (pair)
        {
            Pix.Raised(c, width - 2 - leaf, 2, leaf, 37, DoorWood);
            Pix.Shade(c, width - 2 - leaf, 2, leaf, 37, 0.22f);
        }
        return c;
    }

    /// <summary>A brass name plate in a dark wooden frame, 16 by 11.</summary>
    private static void Plaque(PixelCanvas c, int cx, int y)
    {
        int x0 = cx - 8;
        Pix.Raised(c, x0, y, 16, 11, Timber);
        c.Rect(x0 + 2, y + 2, 12, 7, Brass.Base);
        c.HLine(x0 + 2, y + 2, 12, Brass.Light);
        c.HLine(x0 + 4, y + 4, 8, Brass.Dark);
        c.HLine(x0 + 4, y + 6, 5, Brass.Dark);
        Pix.Shade(c, x0 + 1, y + 11, 15, 1, 0.3f);
    }

    /// <summary>A wall lantern beside a door, 5 by 9; its glass burns all night.</summary>
    private static void Lantern(PixelCanvas c, int x, int y)
    {
        c.Rect(x, y, 5, 2, Iron.Base);
        c.HLine(x + 1, y - 1, 3, Iron.Dark);
        c.Rect(x + 1, y + 2, 3, 5, LanternGlass);
        Pix.Lit(c, x + 1, y + 2, 3, 5, ArtSheet.PublicLight);
        c.VLine(x, y + 2, 5, Iron.Base);
        c.VLine(x + 4, y + 2, 5, Iron.Base);
        c.Rect(x, y + 7, 5, 2, Iron.Dark);
    }

    // ------------------------------------------------------------------ signs

    /// <summary>
    /// The style's sign centred on <paramref name="cx"/> in a band <paramref name="bandHeight"/> texels tall
    /// starting at <paramref name="y"/>. A name is written at double size where the band is tall enough and
    /// <paramref name="room"/> texels wide enough, else at single size, and left out if even that doesn't fit.
    /// </summary>
    private static void Sign(PixelCanvas c, BuildingStyle s, int cx, int y, int bandHeight, int room)
    {
        var kind = s.Sign;
        string? text = kind switch
        {
            SignKind.Mart => "MART",
            SignKind.Lab => "LAB",
            SignKind.School => "SCHOOL",
            SignKind.Poketch => "POKETCH",
            SignKind.Tv => "TV",
            SignKind.Text => s.SignText,
            _ => null
        };
        if (kind == SignKind.Text && text == null) return;
        if (text != null)
        {
            int scale = bandHeight >= 14 && Pix.TextWidth(text, 2) <= room ? 2 : 1;
            if (Pix.TextWidth(text, scale) > room) return;
            int tw = Pix.TextWidth(text, scale), tx = cx - tw / 2, ty = y + (bandHeight - Pix.TextHeight * scale) / 2;
            Pix.Text(c, tx + 1, ty + 1, text, PixelCanvas.Shadow(s.Accent, 0.45f), scale);
            Pix.Text(c, tx, ty, text, Color.White, scale);
            // The letters glow after dark, not the band behind them
            for (int py = ty; py < ty + Pix.TextHeight * scale; py++)
                for (int px = tx; px < tx + tw; px++)
                {
                    var col = c.Get(px, py);
                    if (col.R == 255 && col.G == 255 && col.B == 255) c.SetRaw(px, py, Flag(col, ArtSheet.PublicLight));
                }
            return;
        }

        int size = Math.Min(18, bandHeight / 2 * 2);
        int x0 = cx - size / 2, y0 = y + (bandHeight - size) / 2;
        if (kind == SignKind.Center) BallRoundel(c, x0, y0, size);
        else if (kind == SignKind.Globe) GlobeRoundel(c, x0, y0, size);
        else if (kind == SignKind.Galactic) GalacticMark(c, cx, y0 + 2, size - 4, s.Accent);
    }

    /// <summary>Our own Poké Ball sign: red over white with a dark band and a button, in flat shades.</summary>
    public static void BallRoundel(PixelCanvas c, int x, int y, int size)
    {
        var red = Rgb(236, 64, 56);
        var white = Rgb(250, 250, 252);
        Pix.Disc(c, x, y, size, Ink);
        float r = (size - 2) / 2f;
        int half = size / 2, band = size >= 24 ? 2 : 1;
        for (int py = 0; py < size - 2; py++)
            for (int px = 0; px < size - 2; px++)
            {
                float dx = px + 0.5f - r, dy = py + 0.5f - r;
                if (dx * dx + dy * dy > r * r) continue;
                int ay = y + 1 + py;
                Color col = ay < y + half - band ? (dx + dy < -r * 0.75f ? Rgb(250, 132, 116) : red)
                    : ay < y + half + band ? Ink
                    : dx + dy > r * 0.75f ? Rgb(206, 210, 226) : white;
                c.Set(x + 1 + px, ay, col);
            }
        int b = size >= 24 ? 10 : size >= 16 ? 6 : 4;
        Pix.Disc(c, x + (size - b) / 2, y + (size - b) / 2, b, Ink);
        Pix.Disc(c, x + (size - b) / 2 + 1, y + (size - b) / 2 + 1, b - 2, white);

        // After dark the white half and the button glow; the red half and the dark lines stay as they are
        for (int py = y; py < y + size; py++)
            for (int px = x; px < x + size; px++)
            {
                var col = c.Get(px, py);
                if (col.A == 255 && col.R > 200 && col.G > 200) c.SetRaw(px, py, Flag(col, ArtSheet.PublicLight));
            }
    }

    /// <summary>A globe for the Global Terminal: blue sea, green land in clusters, an equator and a meridian.</summary>
    private static void GlobeRoundel(PixelCanvas c, int x, int y, int size)
    {
        Pix.Disc(c, x, y, size, Rgb(30, 60, 130));
        Pix.Disc(c, x + 1, y + 1, size - 2, Rgb(84, 156, 240));
        var land = Rgb(98, 200, 110);
        c.Rect(x + 4, y + 4, 4, 3, land); c.Rect(x + 5, y + 7, 2, 2, land);
        c.Rect(x + size - 8, y + 5, 4, 2, land); c.Rect(x + size - 7, y + 7, 3, 4, land);
        c.Rect(x + 5, y + size - 6, 3, 2, land);
        c.HLine(x + 1, y + size / 2, size - 2, Rgb(200, 226, 250));
        c.VLine(x + size / 2, y + 1, size - 2, Rgb(200, 226, 250));
        Pix.Lit(c, x, y, size, size, ArtSheet.PublicLight);
    }

    // ------------------------------------------------------------------ roofs

    /// <summary>
    /// Roof tiles in <paramref name="color"/>: a 64 by 64 texture that repeats. Tiles are 8 by 8 in rows half a
    /// tile out of step, each with a light upper-left edge, a darker lower right and a dark rounded foot; one in
    /// eleven is a shade lighter.
    /// </summary>
    public static PixelCanvas RoofTiles(Color color)
    {
        // Two roofs aren't tiled: snow lies on one, the other is sheet metal
        if (color.Equals(SnowRoof)) return SnowSheet();
        if (color.Equals(MetalRoof)) return MetalSheet();
        var tone = Tone.Of(color);
        var pale = Tone.Of(PixelCanvas.Light1(color, 0.14f));
        var c = new PixelCanvas(64, 64);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                int row = y / 8, r = y % 8;
                int along = (x + row % 2 * 4) % 64, tile = along / 8, tx = along % 8;
                var t = (tile * 3 + row * 5) % 11 == 0 ? pale : tone;
                Color col = r == 7 ? tone.Deep
                    : r == 6 ? (tx == 0 || tx >= 6 ? tone.Deep : t.Dark)
                    : tx == 7 ? t.Dark
                    : r == 1 && tx < 5 || r == 2 && tx == 0 ? t.Light
                    : r == 5 && tx >= 5 || r == 4 && tx == 6 ? t.Dark
                    : t.Base;
                c.SetRaw(x, y, col);
            }
        return c;
    }

    /// <summary>The ridge cap seen from the front: rounded cap tiles 12 wide.</summary>
    public static void PaintRidge(PixelCanvas c, Color color)
    {
        var t = Tone.Of(PixelCanvas.Shadow(color, 0.1f));
        for (int x = 0; x < c.Width; x++)
            for (int y = 0; y < c.Height; y++)
                c.SetRaw(x, y, x % 12 == 11 ? t.Deep : y == 0 ? t.Light : y == c.Height - 1 ? t.Dark : t.Base);
    }

    /// <summary>The board along an eave: pale on houses, a dark shade of the roof on shops, with a gutter line on top.</summary>
    public static void PaintFascia(PixelCanvas c, Color color)
    {
        var t = Tone.Of(color);
        for (int y = 0; y < c.Height; y++)
            c.HLine(0, y, c.Width, y == 0 ? t.Light : y == c.Height - 1 ? t.Dark : t.Base);
    }

    /// <summary>A flat roof seen from above: sheets a bay wide with a seam between them, grey unless snow lies on it.</summary>
    public static void PaintFlatRoof(PixelCanvas c, bool snow = false)
    {
        var t = snow ? Tone.Of(SnowRoof) : Tone.Of(170, 176, 190);
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
            {
                int col = (x + Inset) % Bay;
                c.SetRaw(x, y, col == Bay - 1 ? t.Dark : col == 0 ? t.Light : t.Base);
            }
    }

    /// <summary>A chimney's side in stone blocks, or its top with the flue.</summary>
    public static void PaintChimney(PixelCanvas c, bool top)
    {
        if (top)
        {
            Pix.Raised(c, 0, 0, c.Width, c.Height, Stone);
            c.Rect(3, 3, c.Width - 6, c.Height - 6, Ink);
            return;
        }
        for (int y = 0, row = 0; y < c.Height; y += 6, row++)
            for (int x = -(row % 2) * 4; x < c.Width; x += 8)
            {
                int bx = Math.Max(0, x), bw = Math.Min(x + 8, c.Width) - bx;
                Pix.Raised(c, bx, y, bw, Math.Min(6, c.Height - y), ((x + 8) / 8 + row) % 3 == 0 ? StoneAlt : Stone);
            }
        Pix.Raised(c, 0, 0, c.Width, 3, StoneAlt);
    }

    /// <summary>The cap or inner face of a parapet.</summary>
    public static void PaintParapet(PixelCanvas c, bool cap)
    {
        var t = cap ? Panel : Tone.Of(Panel.Dark);
        Pix.Raised(c, 0, 0, c.Width, c.Height, t);
    }

    /// <summary>The flat top of an entrance block, in the accent colour.</summary>
    public static void PaintPortalTop(PixelCanvas c, BuildingStyle s)
    {
        // A shallow tray: a rim lit along its south and west edges, the middle a step darker
        var t = Tone.Of(PixelCanvas.Light1(s.Accent, 0.12f));
        c.Rect(0, 0, c.Width, c.Height, t.Base);
        c.HLine(0, c.Height - 1, c.Width, t.Light);
        c.VLine(0, 0, c.Height, t.Light);
        c.VLine(c.Width - 1, 0, c.Height, t.Dark);
        c.HLine(0, 0, c.Width, t.Dark);
        if (c.Width > 8 && c.Height > 8) Pix.Sunken(c, 3, 3, c.Width - 6, c.Height - 6, Tone.Of(s.Accent));
    }

    // ------------------------------------------------------------------ rooftop equipment

    /// <summary>A ventilation unit: its top with a round fan grille, or a side with louvres.</summary>
    public static void PaintVent(PixelCanvas c, bool top)
    {
        Pix.Raised(c, 0, 0, c.Width, c.Height, Steel);
        if (top)
        {
            int size = Math.Min(c.Width, c.Height) - 4;
            Pix.Disc(c, (c.Width - size) / 2, (c.Height - size) / 2, size, Steel.Dark);
            Pix.Disc(c, (c.Width - size) / 2 + 1, (c.Height - size) / 2 + 1, size - 2, Iron.Base);
            c.HLine((c.Width - size) / 2 + 1, c.Height / 2, size - 2, Steel.Dark);
            c.VLine(c.Width / 2, (c.Height - size) / 2 + 1, size - 2, Steel.Dark);
        }
        else
            for (int y = 2; y < c.Height - 2; y += 2) c.HLine(2, y, c.Width - 4, Steel.Dark);
    }

    /// <summary>A skylight seen from above: panes that glow after dark.</summary>
    public static void PaintSkylight(PixelCanvas c)
    {
        Pix.Raised(c, 0, 0, c.Width, c.Height, Steel);
        for (int x = 2; x + 8 <= c.Width - 2; x += 10) Glass(c, x, 2, 8, c.Height - 4, ArtSheet.PublicLight);
    }

    /// <summary>A lattice broadcast mast with a red light on top: a sprite 16 wide.</summary>
    public static void PaintMast(PixelCanvas c)
    {
        int w = c.Width, h = c.Height, mid = w / 2;
        for (int y = 4; y < h; y++)
        {
            int half = 1 + (y - 4) * (mid - 2) / (h - 4);
            c.Set(mid - half, y, Iron.Light);
            c.Set(mid + half - 1, y, Iron.Base);
            if (y % 6 == 0) c.HLine(mid - half, y, half * 2, Iron.Base);
        }
        c.Rect(mid - 1, 0, 2, 4, Rgb(236, 70, 70));
        Pix.Lit(c, mid - 1, 0, 2, 4, ArtSheet.PublicLight);
        Pix.Outline(c);
    }

    /// <summary>A satellite dish on a short stand: a sprite.</summary>
    public static void PaintDish(PixelCanvas c)
    {
        int w = c.Width, h = c.Height;
        c.Rect(w / 2 - 1, h - 7, 3, 7, Iron.Base);
        for (int y = 0; y < h - 6; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = (x + 0.5f - w / 2f) / (w / 2f), dy = (y + 0.5f - (h - 6) / 2f) / ((h - 6) / 2f);
                if (dx * dx + dy * dy > 1f) continue;
                c.Set(x, y, dx + dy < -0.5f ? Steel.Light : dx + dy > 0.6f ? Steel.Dark : Steel.Base);
            }
        c.Rect(w / 2 - 1, (h - 6) / 2 - 1, 3, 3, Iron.Dark);
        Pix.Outline(c);
    }

    /// <summary>The Global Terminal's globe on its stand: a sprite.</summary>
    public static void PaintGlobe(PixelCanvas c)
    {
        int w = c.Width, h = c.Height, size = w;
        Pix.Raised(c, w / 2 - 5, h - 6, 10, 6, Steel);
        GlobeRoundel(c, 0, 0, size);
        Pix.Outline(c);
    }
}
