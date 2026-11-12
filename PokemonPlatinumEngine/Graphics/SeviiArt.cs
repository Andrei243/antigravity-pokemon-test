using System;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The Sevii Islands' two things of their own look (plan 19's last notes; style guide, "The Seagallop" and "Celio's
/// network machine"): the fast ferry at every harbour, three tiles across, and the machine in One Island's Pokémon
/// Center, two tiles across. Cards with the one-texel outline every card has, painted in code, no GPU calls.
/// </summary>
internal static class SeviiArt
{
    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    public const int SeagallopWidth = 96, SeagallopHeight = 44;
    public const int MachineWidth = 64, MachineHeight = 56;

    private static readonly Color Hull = Rgb(236, 238, 242), HullLit = Rgb(250, 250, 252), HullShade = Rgb(188, 194, 206);
    private static readonly Color Navy = Rgb(38, 52, 104), Red = Rgb(206, 58, 52);
    private static readonly Color Pane = Rgb(44, 58, 84), Glint = Rgb(170, 206, 232);
    private static readonly Color Wake = Rgb(46, 86, 140), Foam = Rgb(226, 240, 248);

    /// <summary>
    /// The Seagallop, 96 by 44: a long low white hull whose bow rises to the left in a point, a navy band along its
    /// waterline under a red stripe, a cabin set back from both ends with six dark windows, a raked mast with a red flag
    /// at its back, and a line of wake with foam at the stern.
    /// </summary>
    public static void PaintSeagallop(PixelCanvas c)
    {
        // The hull: from row 24 to the waterline at row 38, its bow slanting up to a point at the left
        // (the bow raked back from its point at the top left; the stern square, tucked in a little under the water)
        for (int y = 22; y <= 38; y++)
        {
            int start = 2 + (y - 22) * 3 / 4;
            int end = 92 - (y >= 34 ? y - 34 : 0);
            for (int x = start; x <= end; x++) c.Set(x, y, Hull);
        }
        // A lit gunwale along the top of the hull, and its shaded flank low down at the stern
        c.HLine(2, 22, 91, HullLit);
        for (int x = 60; x <= 91; x++) c.Set(x, 33, HullShade);
        // The red stripe and the navy band along the waterline
        for (int x = 0; x < SeagallopWidth; x++)
        {
            if (c.Get(x, 31).A > 0) c.Set(x, 31, Red);
            if (c.Get(x, 32).A > 0) c.Set(x, 32, Red);
            for (int y = 35; y <= 38; y++)
                if (c.Get(x, y).A > 0) c.Set(x, y, Navy);
        }
        // The cabin, set back from both ends, with its six windows
        c.Rect(24, 10, 56, 12, Hull);
        c.HLine(24, 10, 56, HullLit);
        c.VLine(78, 11, 11, HullShade);
        c.VLine(79, 11, 11, HullShade);
        // a raked front to the cabin
        for (int i = 0; i < 6; i++)
            for (int y = 10 + i * 2; y < 22; y++) c.Set(22 - i, y, Hull);
        for (int w = 0; w < 6; w++)
        {
            int wx = 28 + w * 8;
            c.Rect(wx, 13, 6, 5, Pane);
            c.Set(wx, 13, Glint);
            c.Set(wx + 1, 13, Glint);
            c.Set(wx, 14, Glint);
        }
        // The mast at the cabin's back, raked astern, and its red flag
        for (int i = 0; i < 9; i++) c.Set(72 + i / 3, 9 - i, HullShade);
        c.Rect(75, 1, 5, 3, Red);
        // The wake at its foot, foam at the stern
        c.HLine(4, 40, 90, Wake);
        c.HLine(8, 41, 86, Wake);
        foreach (int x in new[] { 86, 89, 92, 90, 94 }) c.Set(x, x % 2 == 0 ? 40 : 41, Foam);
        c.Rect(88, 39, 4, 1, Foam);
        Pix.Outline(c);
    }

    private static readonly Color Steel = Rgb(196, 202, 212), SteelLit = Rgb(226, 230, 236), SteelShade = Rgb(150, 156, 170);
    private static readonly Color Screen = Rgb(32, 44, 60), Reading = Rgb(110, 214, 150);
    private static readonly Color[] Lamps = { Rgb(214, 72, 66), Rgb(232, 176, 64), Rgb(110, 214, 150) };
    private static readonly Color Cable = Rgb(46, 56, 96), Slot = Rgb(84, 88, 100);

    /// <summary>
    /// Celio's network machine, 64 by 56: a cabinet of pale grey steel with two dark screens under its top edge, a line of
    /// green readings across each, a band of red, amber and green lamps under them, a dark slot with two round sockets
    /// below, and a navy cable out of its foot to the right.
    /// </summary>
    public static void PaintNetworkMachine(PixelCanvas c)
    {
        // The cable first, so the cabinet stands over its end
        for (int x = 50; x < 62; x++) c.Set(x, 52 + (x > 56 ? 1 : 0), Cable);
        c.Rect(58, 53, 4, 2, Cable);
        // The cabinet: a lit top, a shaded right side and a shaded foot
        c.Rect(4, 4, 52, 50, Steel);
        c.HLine(4, 4, 52, SteelLit);
        c.HLine(4, 5, 52, SteelLit);
        c.Rect(54, 5, 2, 49, SteelShade);
        c.HLine(4, 53, 52, SteelShade);
        // Two screens side by side, each with a line of readings
        foreach (int sx in new[] { 8, 31 })
        {
            c.Rect(sx, 9, 20, 14, Screen);
            for (int x = sx + 2; x < sx + 18; x++) c.Set(x, 15 + ((x * 3) % 5 == 0 ? -1 : 0) + ((x * 7) % 11 == 0 ? 1 : 0), Reading);
            c.HLine(sx + 2, 19, 6, Reading);
        }
        // The band of lamps
        c.Rect(8, 26, 43, 5, SteelShade);
        for (int i = 0; i < 10; i++) c.Rect(10 + i * 4, 27, 2, 3, Lamps[i % 3]);
        // The slot for the two gems, with its sockets
        c.Rect(14, 35, 30, 10, Slot);
        c.HLine(14, 35, 30, Screen);
        foreach (int gx in new[] { 22, 34 })
            for (int y = 37; y <= 43; y++)
                for (int x = gx - 3; x <= gx + 3; x++)
                {
                    float u = (x + 0.5f - (gx + 0.5f)) / 3.5f, v = (y + 0.5f - 40.5f) / 3.5f;
                    if (u * u + v * v <= 1f) c.Set(x, y, u + v < -0.5f ? SteelLit : Screen);
                }
        Pix.Outline(c);
    }
}
