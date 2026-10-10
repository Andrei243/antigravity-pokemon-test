using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// What is Team Galactic's own inside their rooms (plan 02 · S11, part b; style guide, "Rooms": Team Galactic's
/// rooms): the warp panels of the headquarters' floors, the shutters of its locked doors and the laboratory's tanks,
/// all in the colours of their buildings outside (the dark panel, the teal of the slit windows, the yellow band).
/// Painters only: the room's furniture builder (<see cref="PropModels"/>) and the cards of the things on the ground
/// (<see cref="ThingCards"/>) lay them out. No GPU calls.
/// </summary>
internal static class GalacticArt
{
    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    /// <summary>The dark panel of Team Galactic's buildings (style guide, "Buildings").</summary>
    public static readonly Tone Panel = Tone.Of(104, 116, 130, 132, 144, 156, 70, 80, 98);

    /// <summary>The teal of their slit windows, and the paler middle of a light of that colour.</summary>
    public static readonly Color Teal = Rgb(120, 230, 220), TealCore = Rgb(204, 250, 244);

    /// <summary>The green of the panel that leads out, and its paler middle.</summary>
    public static readonly Color Green = Rgb(110, 220, 120), GreenCore = Rgb(206, 250, 206);

    /// <summary>The yellow band along the top of their walls.</summary>
    public static readonly Color Band = Rgb(232, 204, 76);

    /// <summary>Steel: a tank's foot and cap.</summary>
    public static readonly Tone Steel = Tone.Of(150, 160, 176, 186, 196, 210, 104, 112, 132);

    /// <summary>The fluid of a tank: its body, the light down its left, the depth at its right and foot.</summary>
    public static readonly Color Fluid = Rgb(96, 206, 140), FluidLight = Rgb(150, 232, 178), FluidDeep = Rgb(58, 150, 104);

    /// <summary>What hangs in a tank, and its bubbles.</summary>
    public static readonly Color Specimen = Rgb(40, 104, 80), Bubble = Rgb(206, 250, 226);

    // ------------------------------------------------------------------ warp panels

    /// <summary>The side of a panel's plate, in texels: it lies in the middle of its tile.</summary>
    public const int PanelSize = 28;

    /// <summary>
    /// A warp panel seen from above: a bevelled plate of the dark panel with a ring of light twenty across, a core
    /// eight across and a yellow tick at the middle of each side. <paramref name="wayOut"/> is the green one.
    /// </summary>
    public static void PaintPanel(PixelCanvas c, bool wayOut)
    {
        const int s = PanelSize;
        var light = wayOut ? Green : Teal;
        var core = wayOut ? GreenCore : TealCore;
        Pix.Raised(c, 0, 0, s, s, Tone.Of(Panel.Dark));
        Pix.Sunken(c, 2, 2, s - 4, s - 4, Tone.Of(PixelCanvas.Shadow(Panel.Dark, 0.25f)));
        // The ring: two texels wide, twenty across; the core inside it
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = x + 0.5f - s / 2f, dy = y + 0.5f - s / 2f;
                float d = System.MathF.Sqrt(dx * dx + dy * dy);
                if (d is >= 8f and < 10f) c.Set(x, y, light);
                else if (d < 2f) c.Set(x, y, core);
                else if (d < 4f) c.Set(x, y, light);
            }
        // A tick at the middle of each side
        c.Rect(s / 2 - 1, 1, 2, 3, Band);
        c.Rect(s / 2 - 1, s - 4, 2, 3, Band);
        c.Rect(1, s / 2 - 1, 3, 2, Band);
        c.Rect(s - 4, s / 2 - 1, 3, 2, Band);
    }

    /// <summary>The thin edge of a panel's plate, seen from the south.</summary>
    public static void PaintPanelEdge(PixelCanvas c) => c.Rect(0, 0, c.Width, c.Height, PixelCanvas.Shadow(Panel.Dark, 0.35f));

    // ------------------------------------------------------------------ locked doors

    /// <summary>A leaf of a locked door: a tile wide and as high as a room's inner walls are cut.</summary>
    public const int DoorWidth = 32, DoorHeight = PropModels.InnerWallHeight;

    /// <summary>
    /// A leaf of a shutter: slats of the dark panel four texels deep under the yellow band, with a slit of teal light
    /// at its middle and a dark line at its foot.
    /// </summary>
    public static void PaintDoor(PixelCanvas c)
    {
        int w = c.Width, h = c.Height;
        c.Rect(0, 0, w, h, Panel.Base);
        // The band along the top, a dark line under it
        c.Rect(0, 0, w, 4, Band);
        c.HLine(0, 0, w, PixelCanvas.Light1(Band, 0.4f));
        c.HLine(0, 4, w, Panel.Dark);
        // Slats: a light line on each one's top, a dark one at its foot
        for (int y = 5; y + 4 <= h - 1; y += 4)
        {
            c.HLine(0, y, w, Panel.Light);
            c.HLine(0, y + 3, w, Panel.Dark);
        }
        c.HLine(0, h - 1, w, PixelCanvas.Shadow(Panel.Dark, 0.3f));
        // The posts at either side, and the slit of light at the middle
        c.VLine(0, 4, h - 4, Panel.Dark);
        c.VLine(w - 1, 4, h - 4, Panel.Dark);
        c.Rect(w / 2 - 6, h / 2, 12, 4, Panel.Dark);
        c.Rect(w / 2 - 5, h / 2 + 1, 10, 2, Teal);
    }

    /// <summary>A doorway in a room's back wall: the opening and its frame.</summary>
    public const int DoorwayWidth = 28, DoorwayHeight = 54;

    /// <summary>
    /// A way on through a back wall: the dark of the room beyond in a frame of the dark panel three texels wide, the
    /// yellow band across its head and a pale line on its threshold.
    /// </summary>
    public static void PaintDoorway(PixelCanvas c)
    {
        int w = c.Width, h = c.Height;
        var beyond = Rgb(20, 18, 28);
        Pix.Raised(c, 0, 0, w, h, Panel);
        c.Rect(3, 7, w - 6, h - 7, beyond);
        // Two steps of shade at the jambs and the head, so the opening has depth
        c.Rect(3, 7, w - 6, 2, PixelCanvas.Light1(beyond, 0.18f));
        c.VLine(3, 9, h - 9, PixelCanvas.Light1(beyond, 0.18f));
        c.VLine(w - 4, 9, h - 9, PixelCanvas.Light1(beyond, 0.1f));
        c.Rect(1, 1, w - 2, 4, Band);
        c.HLine(1, 1, w - 2, PixelCanvas.Light1(Band, 0.4f));
        c.HLine(3, h - 1, w - 6, Panel.Light);
    }

    // ------------------------------------------------------------------ the laboratory's tanks

    /// <summary>A tank's glass as wide as it is seen from one side: 52 texels across, 46 high.</summary>
    public const int TankWidth = 52, TankGlass = 46, TankFoot = 10, TankCap = 8;

    /// <summary>
    /// One face of a tank's glass: the fluid lighter down its left and deeper at its right and foot, and on the
    /// face that looks at the camera something dark hanging in the middle of it, with a few bubbles above.
    /// </summary>
    public static void PaintTankGlass(PixelCanvas c, bool front)
    {
        int w = c.Width, h = c.Height;
        c.Rect(0, 0, w, h, Fluid);
        c.Rect(2, 0, 5, h, FluidLight);
        c.Rect(w - 8, 0, 8, h, FluidDeep);
        c.Rect(0, h - 6, w, 6, FluidDeep);
        // The glass's own edges
        c.VLine(0, 0, h, Steel.Dark);
        c.VLine(w - 1, 0, h, Steel.Dark);
        if (!front) return;
        // What hangs in it: a rounded body, tapering, with two stubs
        int cx = w / 2, cy = h / 2 + 2;
        for (int y = -12; y <= 12; y++)
        {
            int half = y < -4 ? 6 + (y + 12) / 2 : y < 6 ? 10 : 10 - (y - 6);
            if (half > 0) c.Rect(cx - half, cy + y, half * 2, 1, Specimen);
        }
        c.Rect(cx - 13, cy - 2, 4, 3, Specimen);
        c.Rect(cx + 9, cy - 2, 4, 3, Specimen);
        // Bubbles rising past it
        c.Rect(cx + 12, 8, 2, 2, Bubble);
        c.Rect(cx - 14, 14, 2, 2, Bubble);
        c.Set(cx + 6, 4, Bubble);
        c.Set(cx - 8, 6, Bubble);
    }

    /// <summary>A steel band of a tank (its foot or its cap) seen from the side, with a row of rivets.</summary>
    public static void PaintTankBand(PixelCanvas c)
    {
        Pix.Raised(c, 0, 0, c.Width, c.Height, Steel);
        for (int x = 4; x < c.Width - 2; x += 8) c.Set(x, c.Height / 2, Steel.Dark);
    }

    /// <summary>The top of a tank's cap: steel, with a sunken hatch and a teal lamp.</summary>
    public static void PaintTankTop(PixelCanvas c)
    {
        int w = c.Width, h = c.Height;
        c.Rect(0, 0, w, h, Steel.Base);
        Pix.Border(c, 0, 0, w, h, Steel.Dark);
        c.HLine(1, h - 2, w - 2, Steel.Light);
        c.VLine(1, 1, h - 2, Steel.Light);
        Pix.Sunken(c, w / 2 - 10, h / 2 - 8, 20, 16, Tone.Of(Steel.Dark));
        c.Rect(w / 2 - 2, h / 2 - 2, 4, 4, Teal);
    }
}
