using System;
using PokemonPlatinumEngine.Overworld;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The furniture of plan 06 · R16b's rooms, built in the rooms' kit (style guide, "Fossils, the Game Corner, the
/// Villa and Pal Park"): the museum's display cases, the Game Corner's slot machines, and the pieces of the Villa no
/// house has. Boxes and cards whose faces are painted at their own size, with no GPU call, like <see cref="PropModels"/>.
/// </summary>
internal static class PastimeArt
{
    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    private static readonly Tone Wood = Tone.Of(184, 126, 78), WoodDark = Tone.Of(128, 86, 56), WoodLight = Tone.Of(214, 160, 104);
    private static readonly Tone Lacquer = Tone.Of(46, 44, 58), Plinth = Tone.Of(84, 76, 78), Cabinet = Tone.Of(52, 62, 128);
    private static readonly Tone Gold = Tone.Of(226, 182, 80), Bronze = Tone.Of(176, 118, 70), Silver = Tone.Of(188, 194, 208);
    private static readonly Tone Linen = Tone.Of(236, 228, 208), Fabric = Tone.Of(116, 150, 216), Dark = Tone.Of(66, 68, 84);
    private static readonly Color Glass = Rgb(190, 222, 232), GlassLight = Rgb(226, 242, 246), GlassEdge = Rgb(120, 150, 168);

    /// <summary>Whether a prop is built here: the kinds of plan 06 · R16b, and the Villa's wide bed and television.</summary>
    public static bool Builds(Prop p) => p.Type >= PropType.DisplayCase && p.Type <= PropType.PcDesk
        || (p.Type is PropType.Bed or PropType.Television && p.Width > 1);

    public static void Build(KitBuilder kit, Prop p, Map map)
    {
        int w = p.Width * 32, d = p.Depth * 32;
        switch (p.Type)
        {
            case PropType.DisplayCase: DisplayCase(kit, w, d, againstWall: p.Y <= map.RoomCorner().Back); break;
            case PropType.SlotMachine: SlotMachine(kit); break;
            case PropType.Piano: Piano(kit, w, d); break;
            case PropType.Rack: Rack(kit, w); break;
            case PropType.AudioSystem: AudioSystem(kit, w); break;
            case PropType.Bust: Bust(kit, Bronze); break;
            case PropType.SilverBust: Bust(kit, Silver); break;
            case PropType.GuestSet: GuestSet(kit, w, d); break;
            case PropType.NightTable: NightTable(kit, d); break;
            case PropType.MusicBox: kit.Sprite(kit.Face("villa.musicbox", 12, 9, PaintMusicBox), 16, 12, 40); break;
            case PropType.TeaSet: kit.Sprite(kit.Face("villa.teaset", 16, 8, PaintTeaSet), 16, 4, 15); break;
            case PropType.PcDesk: PcDesk(kit, w, d); break;
            case PropType.Bed: WideBed(kit, w, d); break;
            case PropType.Television: WideTelevision(kit, w); break;
        }
    }

    private static void Top(PixelCanvas c, Tone t)
    {
        c.Rect(0, 0, c.Width, c.Height, t.Base);
        c.HLine(0, c.Height - 1, c.Width, t.Light);
        c.VLine(0, 0, c.Height, t.Light);
        c.HLine(0, 0, c.Width, t.Dark);
        c.VLine(c.Width - 1, 0, c.Height, t.Dark);
    }

    // ------------------------------------------------------------------ the museum

    /// <summary>A dark plinth with a glass box on it and a few lumps of coal and amber inside; taller against the back wall.</summary>
    private static void DisplayCase(KitBuilder kit, int w, int d, bool againstWall)
    {
        int foot = againstWall ? 16 : 12, glass = againstWall ? 30 : 16;
        kit.Block("case.plinth", Plinth, 2, w - 2, 3, d - 3, 0, foot);
        int gw = w - 8, gd = d - 10;
        void Samples(PixelCanvas c, int width, int floor)
        {
            // Lumps of coal with a glint, and one of amber, on a strip of felt
            c.Rect(1, floor, width - 2, 2, Rgb(132, 60, 66));
            for (int x = 5, n = 0; x < width - 8; x += 11, n++)
            {
                var lump = n % 3 == 2 ? Rgb(226, 160, 62) : Rgb(48, 46, 58);
                c.Rect(x, floor - 5, 6, 5, lump);
                c.Rect(x + 1, floor - 6, 4, 1, lump);
                c.Rect(x + 1, floor - 4, 2, 1, PixelCanvas.Light1(lump, 0.5f));
            }
        }
        var front = kit.Face($"case.front.{gw}.{glass}", gw, glass, c =>
        {
            c.Rect(0, 0, gw, glass, Glass);
            Samples(c, gw, glass - 3);
            c.Line(3, glass - 6, 9, 2, GlassLight);
            c.Line(gw - 14, glass - 4, gw - 6, 3, GlassLight);
            Pix.Border(c, 0, 0, gw, glass, GlassEdge);
        });
        var top = kit.Face($"case.top.{gw}x{gd}", gw, gd, c =>
        {
            c.Rect(0, 0, gw, gd, Glass);
            if (gw >= 90 && gd >= 90)
            {
                // The hall's great exhibit: one lump of coal as big as a table, on a bed of felt
                c.Rect(10, 12, gw - 20, gd - 26, Rgb(132, 60, 66));
                Pix.Border(c, 10, 12, gw - 20, gd - 26, Rgb(104, 44, 52));
                c.FlatEllipse(gw / 2f, gd / 2f - 2, gw / 2f - 20, gd / 2f - 24, Rgb(48, 46, 58));
                c.FlatEllipse(gw / 2f - 8, gd / 2f - 10, gw / 2f - 34, gd / 2f - 36, Rgb(64, 62, 78));
                c.Line(gw / 2 - 22, gd / 2 - 18, gw / 2 - 6, gd / 2 - 24, Rgb(150, 150, 172));
                c.Line(gw / 2 + 6, gd / 2 + 2, gw / 2 + 18, gd / 2 - 4, Rgb(112, 112, 134));
                c.Line(gw / 2 - 14, gd / 2 + 8, gw / 2 - 4, gd / 2 + 12, Rgb(96, 96, 118));
            }
            else Samples(c, gw, gd - 5);
            c.Line(4, gd - 3, 12, 2, GlassLight);
            Pix.Border(c, 0, 0, gw, gd, GlassEdge);
        });
        var side = kit.Face($"case.side.{gd}.{glass}", gd, glass, c =>
        {
            c.Rect(0, 0, gd, glass, PixelCanvas.Shadow(Glass, 0.12f));
            Pix.Border(c, 0, 0, gd, glass, GlassEdge);
        });
        kit.Box(4, w - 4, 5, d - 5, foot, foot + glass, top, front, side, side);
    }

    // ------------------------------------------------------------------ the Game Corner

    /// <summary>
    /// A slot machine a tile wide: a dark blue cabinet whose sloping panel of three reel windows is seen from above, a
    /// gold tray, a red light on top. Its own front is turned to the aisle beside it, so the south face is its side.
    /// </summary>
    private static void SlotMachine(KitBuilder kit)
    {
        var side = kit.Face("slot.side", 26, 34, c =>
        {
            Pix.Raised(c, 0, 0, 26, 34, Cabinet);
            c.Rect(3, 4, 20, 3, Gold.Base);
            c.HLine(3, 6, 20, Gold.Dark);
            Pix.Sunken(c, 5, 11, 16, 12, Cabinet);
            c.Rect(8, 14, 10, 2, Rgb(226, 78, 84));
            c.Rect(8, 18, 10, 2, Rgb(96, 166, 236));
            c.Rect(3, 28, 20, 3, Gold.Dark);
        });
        var front = kit.Face("slot.front", 20, 34, c =>
        {
            Pix.Raised(c, 0, 0, 20, 34, Cabinet);
            c.Rect(2, 5, 16, 11, Rgb(244, 244, 236));
            c.VLine(7, 5, 11, Cabinet.Dark);
            c.VLine(12, 5, 11, Cabinet.Dark);
            c.Rect(3, 9, 3, 3, Rgb(226, 78, 84));
            c.Rect(8, 9, 3, 3, Gold.Base);
            c.Rect(13, 9, 3, 3, Rgb(96, 166, 236));
            c.Rect(2, 22, 16, 4, Gold.Base);
            c.HLine(2, 25, 16, Gold.Dark);
        });
        var top = kit.Face("slot.top", 26, 20, c =>
        {
            Top(c, Cabinet);
            // The panel as it is seen from above: three lit windows, each with its mark
            c.Rect(3, 4, 20, 12, Rgb(244, 244, 236));
            c.VLine(9, 4, 12, Cabinet.Dark);
            c.VLine(16, 4, 12, Cabinet.Dark);
            c.Rect(4, 8, 4, 4, Rgb(226, 78, 84));
            c.Rect(11, 8, 4, 4, Gold.Base);
            c.Rect(18, 8, 4, 4, Rgb(96, 166, 236));
            Pix.Border(c, 3, 4, 20, 12, Gold.Dark);
        });
        kit.Box(3, 29, 6, 26, 0, 34, top, side, front, front);
        // The lamp on top, lit like a window after dark
        var lamp = kit.Face("slot.lamp", 8, 6, c =>
        {
            c.Rect(1, 1, 6, 5, Rgb(236, 84, 90));
            c.Rect(2, 0, 4, 1, Rgb(236, 84, 90));
            c.Rect(2, 2, 2, 1, Rgb(252, 198, 196));
        });
        kit.Sprite(lamp, 16, 14, 34);
    }

    // ------------------------------------------------------------------ the Villa

    /// <summary>A grand piano: a dark lacquered case on three legs, its lid's edge catching the light, the keyboard along its south side.</summary>
    private static void Piano(KitBuilder kit, int w, int d)
    {
        foreach (var (x, z) in new[] { (6, 10), (w - 10, 10), (w / 2 - 2, d - 14) }) kit.Block("piano.leg", Lacquer, x, x + 4, z, z + 4, 0, 16);
        int tw = w - 6, td = d - 10;
        var top = kit.Face($"piano.top.{tw}x{td}", tw, td, c =>
        {
            Top(c, Lacquer);
            // The lid's curve and a line of light along it
            c.Line(4, td - 6, tw - 10, 6, Lacquer.Light);
            c.Line(5, td - 6, tw - 9, 6, PixelCanvas.Light1(Lacquer.Light, 0.4f));
            c.Rect(tw - 12, 5, 7, 2, Gold.Base);
        });
        var keys = kit.Face($"piano.keys.{tw}", tw, 10, c =>
        {
            Pix.Raised(c, 0, 0, tw, 10, Lacquer);
            c.Rect(2, 3, tw - 4, 5, Rgb(246, 244, 236));
            for (int x = 4; x < tw - 4; x += 3) c.VLine(x, 3, 5, Rgb(196, 196, 204));
            for (int x = 5; x < tw - 5; x += 6) c.Rect(x, 3, 2, 3, Rgb(34, 32, 44));
        });
        var side = kit.Face($"piano.side.{td}", td, 10, c => Pix.Raised(c, 0, 0, td, 10, Lacquer));
        kit.Box(3, w - 3, 5, d - 5, 16, 26, top, keys, side, side);
    }

    /// <summary>An open rack of the bookshelf's wood: three shelves with a few things on them.</summary>
    private static void Rack(KitBuilder kit, int w)
    {
        int fw = w - 4;
        var front = kit.Face($"rack.front.{fw}", fw, 40, c =>
        {
            Pix.Raised(c, 0, 0, fw, 40, WoodDark);
            c.Rect(2, 2, fw - 4, 36, Rgb(86, 62, 50));
            for (int shelf = 0; shelf < 3; shelf++)
            {
                int floor = 13 + shelf * 12;
                c.HLine(2, floor, fw - 4, WoodDark.Light);
                c.HLine(2, floor + 1, fw - 4, WoodDark.Dark);
                for (int x = 5 + shelf * 3; x < fw - 9; x += 13)
                {
                    var thing = ((x + shelf) % 3) switch { 0 => Rgb(226, 190, 96), 1 => Rgb(106, 166, 196), _ => Rgb(214, 108, 96) };
                    int h = 5 + (x + shelf * 2) % 4;
                    c.Rect(x, floor - h, 6, h, thing);
                    c.VLine(x, floor - h, h, PixelCanvas.Light1(thing, 0.4f));
                }
            }
        });
        var top = kit.Face($"rack.top.{fw}", fw, 12, c => Top(c, Wood));
        var side = kit.Face("rack.side", 12, 40, c => Pix.Raised(c, 0, 0, 12, 40, WoodDark));
        kit.Box(2, w - 2, 2, 14, 0, 40, top, front, side, side);
    }

    /// <summary>A long dark cabinet: a speaker cone at each end and the player's dials between them.</summary>
    private static void AudioSystem(KitBuilder kit, int w)
    {
        int fw = w - 6;
        var front = kit.Face($"audio.front.{fw}", fw, 28, c =>
        {
            Pix.Raised(c, 0, 0, fw, 28, Dark);
            foreach (int x in new[] { 4, fw - 22 })
            {
                Pix.Sunken(c, x, 4, 18, 20, Dark);
                Pix.Disc(c, x + 3, 8, 12, Rgb(40, 40, 52));
                Pix.Disc(c, x + 6, 11, 6, Rgb(96, 100, 122));
            }
            c.Rect(26, 6, fw - 52, 6, Rgb(52, 110, 206));
            c.HLine(28, 8, fw - 58, Rgb(190, 230, 252));
            for (int x = 28; x < fw - 30; x += 8) Pix.Disc(c, x, 17, 4, Rgb(196, 200, 210));
        });
        var top = kit.Face($"audio.top.{fw}", fw, 14, c => Top(c, Dark));
        var side = kit.Face("audio.side", 14, 28, c => Pix.Raised(c, 0, 0, 14, 28, Dark));
        kit.Box(3, w - 3, 2, 16, 0, 28, top, front, side, side);
    }

    /// <summary>A bust on a plinth: a round head with two ears and a tail of a crest, in bronze or in silver.</summary>
    private static void Bust(KitBuilder kit, Tone metal)
    {
        kit.Block("bust.plinth", Plinth, 9, 23, 9, 23, 0, 16);
        string name = metal == Silver ? "silver" : "bronze";
        var bust = kit.Face($"bust.{name}", 18, 22, c =>
        {
            // Shoulders, a neck, a round head, two pointed ears
            c.Rect(3, 16, 12, 6, metal.Base);
            c.Rect(5, 14, 8, 2, metal.Base);
            Pix.Disc(c, 4, 4, 10, metal.Base);
            c.Rect(4, 1, 3, 5, metal.Base);
            c.Rect(11, 1, 3, 5, metal.Base);
            c.Rect(5, 0, 1, 1, metal.Base);
            c.Rect(12, 0, 1, 1, metal.Base);
            c.Rect(6, 6, 2, 3, metal.Light);
            c.Rect(4, 17, 2, 4, metal.Light);
            c.Rect(11, 10, 2, 3, metal.Dark);
            c.Rect(12, 17, 2, 4, metal.Dark);
            c.Rect(7, 9, 1, 1, metal.Deep);
            c.Rect(10, 9, 1, 1, metal.Deep);
            Pix.Outline(c);
        });
        kit.Sprite(bust, 16, 17, 16);
    }

    /// <summary>A low table with a cushioned seat at each end.</summary>
    private static void GuestSet(KitBuilder kit, int w, int d)
    {
        foreach (int x in new[] { 4, w - 28 })
        {
            kit.Block("guest.seat.base", WoodDark, x, x + 24, 10, d - 10, 0, 8);
            kit.Block("guest.seat", Fabric, x + 1, x + 23, 11, d - 11, 8, 13);
        }
        int tw = w - 72, td = d - 20;
        var top = kit.Face($"guest.table.{tw}x{td}", tw, td, c =>
        {
            Top(c, WoodLight);
            c.Rect(6, td / 2 - 5, tw - 12, 10, Linen.Base);
            Pix.Border(c, 6, td / 2 - 5, tw - 12, 10, Linen.Dark);
        });
        var edge = kit.Face($"guest.edge.{tw}", tw, 3, c => Pix.Raised(c, 0, 0, tw, 3, WoodLight));
        var edgeSide = kit.Face($"guest.edge.side.{td}", td, 3, c => Pix.Raised(c, 0, 0, td, 3, Wood));
        kit.Box(36, w - 36, 10, d - 10, 11, 14, top, edge, edgeSide, edgeSide);
        foreach (int x in new[] { 38, w - 42 })
            foreach (int z in new[] { 12, d - 16 })
                kit.Block("guest.leg", WoodDark, x, x + 3, z, z + 3, 0, 11);
    }

    /// <summary>A small cabinet beside the bed, a lamp on it.</summary>
    private static void NightTable(KitBuilder kit, int d)
    {
        var front = kit.Face("night.front", 24, 18, c =>
        {
            Pix.Raised(c, 0, 0, 24, 18, Wood);
            Pix.Sunken(c, 3, 3, 18, 5, Wood);
            Pix.Sunken(c, 3, 10, 18, 5, Wood);
            c.Rect(11, 5, 2, 1, Gold.Base);
            c.Rect(11, 12, 2, 1, Gold.Base);
        });
        var top = kit.Face($"night.top.{d - 16}", 24, d - 16, c => Top(c, WoodLight));
        var side = kit.Face($"night.side.{d - 16}", d - 16, 18, c => Pix.Raised(c, 0, 0, d - 16, 18, WoodDark));
        kit.Box(4, 28, 8, d - 8, 0, 18, top, front, side, side);
        var lamp = kit.Face("night.lamp", 12, 16, c =>
        {
            c.Rect(5, 9, 2, 6, Gold.Dark);
            c.Rect(3, 14, 6, 2, Gold.Base);
            c.Rect(2, 2, 8, 7, Rgb(250, 232, 170));
            c.Rect(3, 1, 6, 1, Rgb(250, 232, 170));
            c.VLine(2, 2, 7, Rgb(255, 246, 214));
            c.HLine(2, 8, 8, Rgb(214, 186, 116));
            Pix.Outline(c);
        });
        kit.Sprite(lamp, 16, d / 2f, 18);
    }

    private static void PaintMusicBox(PixelCanvas c)
    {
        c.Rect(1, 3, 10, 6, Rgb(170, 98, 64));
        c.Rect(1, 3, 10, 1, Rgb(214, 150, 96));
        c.Rect(5, 5, 2, 2, Rgb(226, 182, 80));
        c.Rect(2, 0, 8, 3, Rgb(120, 70, 50));
        c.Rect(3, 1, 6, 1, Rgb(236, 196, 208));
        Pix.Outline(c);
    }

    private static void PaintTeaSet(PixelCanvas c)
    {
        // A pot and two cups on a tray
        c.Rect(0, 6, 16, 2, Rgb(196, 200, 210));
        c.Rect(5, 1, 6, 5, Rgb(246, 244, 236));
        c.Rect(11, 2, 2, 2, Rgb(246, 244, 236));
        c.Rect(7, 0, 2, 1, Rgb(116, 150, 216));
        c.Rect(5, 3, 6, 1, Rgb(116, 150, 216));
        c.Rect(1, 4, 3, 2, Rgb(246, 244, 236));
        c.Rect(13, 4, 2, 2, Rgb(246, 244, 236));
        Pix.Outline(c);
    }

    /// <summary>A writing desk along the wall with a computer on it: the screen lit, a keyboard before it.</summary>
    private static void PcDesk(KitBuilder kit, int w, int d)
    {
        int tw = w - 6, td = d - 10;
        var top = kit.Face($"pcdesk.top.{tw}x{td}", tw, td, c =>
        {
            Top(c, WoodLight);
            for (int y = 7; y < td - 2; y += 7) c.HLine(1, y, tw - 2, WoodLight.Dark);
            c.Rect(tw / 2 - 9, td / 2 + 8, 18, 6, Rgb(214, 220, 234));
            for (int x = tw / 2 - 8; x < tw / 2 + 8; x += 3) c.Rect(x, td / 2 + 9, 2, 1, Rgb(150, 160, 186));
            c.Rect(8, 8, 9, 7, Rgb(250, 250, 250));
        });
        var edge = kit.Face($"pcdesk.edge.{tw}", tw, 3, c => Pix.Raised(c, 0, 0, tw, 3, WoodLight));
        var edgeSide = kit.Face($"pcdesk.edge.side.{td}", td, 3, c => Pix.Raised(c, 0, 0, td, 3, Wood));
        kit.Box(3, w - 3, 5, d - 5, 20, 23, top, edge, edgeSide, edgeSide);
        foreach (int x in new[] { 5, w - 8 })
            foreach (int z in new[] { 7, d - 10 })
                kit.Block("pcdesk.leg", WoodDark, x, x + 3, z, z + 3, 0, 20);
        var screen = kit.Face("pcdesk.screen", 22, 18, c =>
        {
            Pix.Raised(c, 0, 0, 22, 14, Dark);
            c.Rect(2, 2, 18, 10, Rgb(96, 166, 236));
            c.HLine(4, 4, 9, Rgb(190, 230, 252));
            c.HLine(4, 7, 13, Rgb(190, 230, 252));
            c.Rect(9, 14, 4, 3, Dark.Dark);
            c.Rect(6, 17, 10, 1, Dark.Base);
        });
        kit.Sprite(screen, w / 2f, d / 2f - 4, 23);
    }

    /// <summary>A bed as wide as its tiles: two pillows at the head against the wall, a blanket folded back, a wooden foot.</summary>
    private static void WideBed(KitBuilder kit, int w, int d)
    {
        int bw = w - 6, length = d - 6;
        var top = kit.Face($"widebed.top.{bw}x{length}", bw, length, c =>
        {
            Top(c, Fabric);
            int pillow = (bw - 14) / 2;
            Pix.Raised(c, 5, 3, pillow, 10, Linen);
            Pix.Raised(c, bw - 5 - pillow, 3, pillow, 10, Linen);
            c.Rect(1, 16, bw - 2, 5, Linen.Light);
            c.HLine(1, 20, bw - 2, Linen.Dark);
            for (int y = 30; y < length - 3; y += 9) c.HLine(2, y, bw - 4, Fabric.Dark);
        });
        var foot = kit.Face($"widebed.foot.{bw}", bw, 14, c =>
        {
            Pix.Raised(c, 0, 0, bw, 14, Wood);
            c.Rect(1, 1, bw - 2, 6, Fabric.Base);
            c.HLine(1, 6, bw - 2, Fabric.Dark);
        });
        var side = kit.Face($"widebed.side.{length}", length, 14, c =>
        {
            Pix.Raised(c, 0, 0, length, 14, WoodDark);
            c.Rect(1, 1, length - 2, 6, Fabric.Dark);
        });
        kit.Box(3, w - 3, 3, 3 + length, 0, 14, top, foot, side, side);
        var head = kit.Face($"widebed.head.{bw + 2}", bw + 2, 26, c =>
        {
            Pix.Raised(c, 0, 0, bw + 2, 26, WoodDark);
            Pix.Sunken(c, 4, 4, bw - 6, 14, WoodDark);
        });
        kit.Card(2, w - 2, 3, 0, 26, head);
    }

    /// <summary>A wide flat television on a low stand.</summary>
    private static void WideTelevision(KitBuilder kit, int w)
    {
        int sw = w - 6;
        var stand = kit.Face($"widetv.stand.{sw}", sw, 12, c =>
        {
            Pix.Raised(c, 0, 0, sw, 12, Wood);
            Pix.Sunken(c, 3, 3, sw / 2 - 5, 6, Wood);
            Pix.Sunken(c, sw / 2 + 2, 3, sw / 2 - 5, 6, Wood);
        });
        var standTop = kit.Face($"widetv.stand.top.{sw}", sw, 16, c => Top(c, WoodLight));
        var standSide = kit.Face("widetv.stand.side", 16, 12, c => Pix.Raised(c, 0, 0, 16, 12, WoodDark));
        kit.Box(3, w - 3, 2, 18, 0, 12, standTop, stand, standSide, standSide);
        int tw = w - 14;
        var screen = kit.Face($"widetv.front.{tw}", tw, 26, c =>
        {
            Pix.Raised(c, 0, 0, tw, 26, Dark);
            // A blue sky over a green hill, and a sun
            c.Rect(2, 2, tw - 4, 13, Rgb(128, 196, 250));
            c.Rect(2, 15, tw - 4, 7, Rgb(98, 184, 110));
            Pix.Disc(c, tw - 16, 4, 7, Rgb(250, 214, 76));
            c.Rect(8, 12, 14, 3, Rgb(120, 196, 124));
            c.Rect(tw / 2 - 1, 23, 2, 1, Rgb(98, 220, 120));
        });
        var setTop = kit.Face($"widetv.top.{tw}", tw, 6, c => Top(c, Dark));
        var setSide = kit.Face("widetv.side", 6, 26, c => Pix.Raised(c, 0, 0, 6, 26, Dark));
        kit.Box(7, w - 7, 6, 12, 12, 38, setTop, screen, setSide, setSide);
    }
}
