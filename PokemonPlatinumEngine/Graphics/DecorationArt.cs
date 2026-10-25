using System;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using Raylib_cs;
using static PokemonPlatinumEngine.Graphics.Pix;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// What stands in a secret base of Hoenn's (plan 21 · H10; style guide, "Secret bases"): Emerald's hundred and twenty
/// decorations, each built with the field's art kit as the rooms' furniture is, by its kind and its colour. A desk, a
/// chair, a plant and a television are the rooms' own pieces; bricks, tents, a slide, a tire and the rest are plain
/// pieces of our own; a mat lies flat on the floor, a poster hangs on the wall's face over the floor, and every doll and
/// cushion is one rounded figure in its species' or its pattern's colour (stand-ins, as the Underground's dolls are).
/// None of Emerald's art. No drawing calls, so tests build them.
/// </summary>
internal static class DecorationArt
{
    /// <summary>The start of the model's name of a decoration standing in a base: <c>dec:45</c> is the TV.</summary>
    public const string ModelPrefix = "dec:";

    public static bool Builds(Prop p) => p.Type == PropType.Decoration && p.Model.StartsWith(ModelPrefix, StringComparison.Ordinal);

    /// <summary>The decoration a prop of a base is; null for anything else.</summary>
    public static DecorationRecord? Of(Prop p) =>
        Builds(p) && int.TryParse(p.Model.AsSpan(ModelPrefix.Length), out int id) ? DecorationDatabase.Get(id) : null;

    /// <summary>A decoration's own colour, read from its name: its colour word, its pattern's element, its species'.</summary>
    public static Color ColourOf(DecorationRecord d)
    {
        string n = d.Name;
        bool Has(string word) => n.Contains(word, StringComparison.Ordinal);
        if (Has("Red") || Has("Fire") || Has("Charizard") || Has("Torchic") || Has("Cyndaquil")) return Rgb(220, 84, 70);
        if (Has("Blue") || Has("Water") || Has("Sea") || Has("Surf") || Has("Marill") || Has("Azurill") || Has("Mudkip") || Has("Totodile")
            || Has("Lapras") || Has("Blastoise") || Has("Wailmer") || Has("Swablu") || Has("Regice")) return Rgb(84, 140, 224);
        if (Has("Yellow") || Has("Pika") || Has("Pichu") || Has("Thunder") || Has("Gold") || Has("Sand")) return Rgb(236, 190, 64);
        if (Has("Green") || Has("Grass") || Has("Chikorita") || Has("Treecko") || Has("Venusaur") || Has("Gulpin") || Has("Kecleon")
            || Has("Lotad") || Has("Tropical") || Has("Colorful")) return Rgb(98, 178, 96);
        if (Has("Pretty") || Has("Cute") || Has("Kiss") || Has("Attract") || Has("Jigglypuff") || Has("Clefairy") || Has("Skitty")
            || Has("Togepi") || Has("Smoochum")) return Rgb(238, 140, 176);
        if (Has("Silver") || Has("Glass") || Has("Glitter") || Has("Powder") || Has("Spikes") || Has("Registeel")) return Rgb(196, 204, 220);
        if (Has("Ditto") || Has("Duskull") || Has("Wynaut") || Has("Spin")) return Rgb(150, 118, 200);
        if (Has("Brick") || Has("Mud") || Has("Ragged") || Has("Camp") || Has("Fissure") || Has("Baltoy") || Has("Seedot") || Has("Rhydon")
            || Has("Regirock") || Has("Meowth")) return Rgb(190, 132, 84);
        if (Has("Snorlax") || Has("Tire") || Has("Hard")) return Rgb(96, 104, 124);
        Color[] others = { Rgb(214, 170, 96), Rgb(120, 170, 196), Rgb(196, 124, 110), Rgb(140, 176, 130), Rgb(180, 150, 190), Rgb(204, 196, 160) };
        return others[d.Id % others.Length];
    }

    /// <summary>How a decoration is drawn on the decorating screen's plan: its colour, and nothing more (the plan writes the last word of its name).</summary>
    public static (Color Colour, string Mark) PlanLook(int decoration)
    {
        if (DecorationDatabase.Get(decoration) is not { } d) return (Rgb(150, 160, 190), "");
        var colour = d.Category switch
        {
            DecorationCategory.Desk or DecorationCategory.Chair => Rgb(176, 128, 84),
            DecorationCategory.Plant => Rgb(96, 168, 96),
            _ => ColourOf(d)
        };
        return (colour, "");
    }

    /// <summary>Builds a decoration where it stands in its base.</summary>
    public static void Build(KitBuilder kit, Prop p, Map map)
    {
        if (Of(p) is not { } d) return;
        kit.Origin = new System.Numerics.Vector3(p.X, Relief.At(map, p.X + p.Width / 2f, p.Y + p.Depth / 2f), p.Y);
        int w = p.Width * 32, depth = p.Depth * 32;
        var colour = ColourOf(d);
        var tone = Tone.Of(colour);
        switch (d.Category)
        {
            case DecorationCategory.Desk:
                // The rooms' own table, as long and as deep as the desk
                PropModels.Build(kit, new Prop { Type = PropType.Table, X = p.X, Y = p.Y, Width = p.Width, Depth = p.Depth }, map);
                break;
            case DecorationCategory.Chair:
                PropModels.Build(kit, new Prop { Type = PropType.Chair, X = p.X, Y = p.Y }, map);
                break;
            case DecorationCategory.Plant:
                // A pot on each tile of its front row, its leaves rising before the wall
                for (int tx = 0; tx < p.Width; tx++)
                    PropModels.Build(kit, new Prop { Type = PropType.Plant, X = p.X + tx, Y = p.Y + p.Depth - 1 }, map);
                break;
            case DecorationCategory.Mat:
                kit.Decal(2, w - 2, 2, depth - 2, 0.012f, kit.Face($"dec.mat.{d.Id}", w - 4, depth - 4, c => PaintMat(c, d, tone)));
                break;
            case DecorationCategory.Poster:
                // On the wall's face over the floor, as a picture hangs in a room
                kit.Card(4, w - 4, depth + 0.5f, 22, 22 + 26, kit.Face($"dec.poster.{d.Id}", w - 8, 26, c => PaintPoster(c, d, tone)));
                break;
            case DecorationCategory.Doll:
            {
                bool big = d.Depth > 1;
                kit.Sprite(kit.Face($"dec.doll.{d.Id}", big ? 30 : 24, big ? 46 : 30, c => PaintFigure(c, tone, big)), 16, 20);
                break;
            }
            case DecorationCategory.Cushion:
                kit.Block($"dec.cushion.{d.Id}", tone, 5, 27, 7, 27, 0, 5);
                kit.Decal(9, 23, 11, 23, 5.2f / 32f, kit.Face($"dec.cushion.mark.{d.Id}", 14, 12, c => PaintCushionMark(c, d, tone)));
                break;
            default:
                Ornament(kit, p, map, d, w, depth, tone);
                break;
        }
    }

    /// <summary>The ornaments, by what each is.</summary>
    private static void Ornament(KitBuilder kit, Prop p, Map map, DecorationRecord d, int w, int depth, Tone tone)
    {
        string n = d.Name;
        bool Has(string word) => n.Contains(word, StringComparison.Ordinal);
        if (Has("TV"))
            PropModels.Build(kit, new Prop { Type = PropType.Television, X = p.X, Y = p.Y }, map);
        else if (Has("Brick"))
        {
            // Two blocks one on the other, the front one lower
            kit.Block($"dec.brick.{d.Id}", tone, 4, 28, 36, 60, 0, 22);
            kit.Block($"dec.brick.{d.Id}", tone, 4, 28, 8, 34, 0, 40);
        }
        else if (Has("Balloon"))
            kit.Sprite(kit.Face($"dec.balloon.{d.Id}", 20, 40, c => PaintBalloon(c, tone)), 16, 16);
        else if (Has("Tent"))
            kit.Sprite(kit.Face($"dec.tent.{d.Id}", 92, 60, c => UndergroundArt.PaintTent(c, tone)), w / 2f, depth - 8);
        else if (Has("Board"))
            kit.Decal(3, w - 3, 3, depth - 3, 0.012f, kit.Face("dec.board", w - 6, depth - 6, PaintBoard));
        else if (Has("Slide"))
        {
            // A ladder's platform at the back, the slide's run down to the front
            kit.Block("dec.slide.steps", Tone.Of(150, 160, 178), 4, w - 4, 4, 34, 0, 40);
            kit.Block($"dec.slide.run.{d.Id}", tone, 10, w - 10, 34, depth - 4, 0, 10);
        }
        else if (Has("Fence"))
        {
            bool across = Has("Width");
            kit.Block("dec.fence", Tone.Of(196, 160, 104), across ? 0 : 13, across ? 32 : 19, across ? 13 : 0, across ? 19 : 32, 0, 18);
        }
        else if (Has("Tire"))
            kit.Sprite(kit.Face("dec.tire", 52, 40, c => PaintTire(c)), w / 2f, depth - 12);
        else if (Has("Stand"))
            kit.Block("dec.stand", Tone.Of(176, 128, 84), 2, w - 2, 6, depth - 2, 0, 12);
        else if (Has("Mud Ball"))
            kit.Sprite(kit.Face("dec.mud", 20, 18, c => PaintBall(c, Tone.Of(150, 104, 70))), 16, 18);
        else if (Has("Door"))
            kit.Sprite(kit.Face("dec.door", 26, 54, c => PaintDoor(c)), 16, depth - 10);
        else if (Has("Glass"))
            kit.Sprite(kit.Face("dec.glass", 22, 32, c => UndergroundArt.PaintCrystal(c, Tone.Of(170, 226, 236))), 16, depth - 12);
        else if (Has("Shield"))
            kit.Sprite(kit.Face($"dec.shield.{d.Id}", 22, 30, c => UndergroundArt.PaintCup(c, tone)), 16, depth - 12);
        else
            kit.Sprite(kit.Face($"dec.thing.{d.Id}", 22, 30, c => UndergroundArt.PaintOnStand(c, tone, round: false)), 16, depth - 12);
    }

    // ------------------------------------------------------------------ the pieces of our own, lit from the upper left

    private static void Ellipse(PixelCanvas c, float cx, float cy, float rx, float ry, Tone tone)
    {
        for (int y = (int)(cy - ry); y <= (int)(cy + ry); y++)
            for (int x = (int)(cx - rx); x <= (int)(cx + rx); x++)
            {
                float u = (x + 0.5f - cx) / rx, v = (y + 0.5f - cy) / ry;
                if (u * u + v * v > 1f) continue;
                c.Set(x, y, u + v < -0.5f ? tone.Light : u + v > 0.6f ? tone.Dark : tone.Base);
            }
    }

    /// <summary>
    /// A mat lying flat: a bordered rug in its colour; a note mat with its note's letter in the middle; the big mats of
    /// a move with their move's sign (a wave, a bolt, a flame, a flake, a heart, a crack, three spikes).
    /// </summary>
    public static void PaintMat(PixelCanvas c, DecorationRecord d, Tone tone)
    {
        int w = c.Width, h = c.Height;
        c.Rect(0, 0, w, h, tone.Dark);
        c.Rect(2, 2, w - 4, h - 4, tone.Base);
        c.HLine(2, 2, w - 4, tone.Light);
        c.VLine(2, 2, h - 4, tone.Light);
        string n = d.Name;
        var ink = tone.Deep;
        if (n.EndsWith("Note Mat", StringComparison.Ordinal))
        {
            // The note's letter, a dot under the high C
            string letter = n[..1];
            Text(c, w / 2 - TextWidth(letter) / 2, h / 2 - 3, letter, ink);
            if (n.Contains("High", StringComparison.Ordinal)) c.Rect(w / 2 - 1, h / 2 + 6, 2, 2, ink);
            return;
        }
        int cx = w / 2, cy = h / 2;
        bool Has(string word) => n.Contains(word, StringComparison.Ordinal);
        if (Has("Surf"))
            for (int row = -1; row <= 1; row++)
                for (int x = 6; x < w - 6; x++) c.Set(x, cy + row * 12 + (int)(MathF.Sin(x * 0.35f) * 3), ink);
        else if (Has("Thunder"))
            for (int i = 0; i < 24; i++) c.Rect(cx - 6 + (i % 8 < 4 ? i % 8 : 8 - i % 8) * 3, cy - 18 + i * 3 / 2, 3, 2, ink);
        else if (Has("Fire"))
            Ellipse(c, cx, cy + 4, 14, 18, Tone.Of(250, 158, 50));
        else if (Has("Powder"))
            for (int i = 0; i < 4; i++) { c.Rect(cx - 16 + i * 8, cy - 1, 4, 2, ink); c.Rect(cx - 1, cy - 16 + i * 8, 2, 4, ink); }
        else if (Has("Attract"))
        {
            Ellipse(c, cx - 7, cy - 4, 8, 8, Tone.Of(226, 84, 120));
            Ellipse(c, cx + 7, cy - 4, 8, 8, Tone.Of(226, 84, 120));
            for (int y = 0; y < 14; y++) c.Rect(cx - 14 + y, cy + y, 28 - y * 2, 1, Rgb(226, 84, 120));
        }
        else if (Has("Fissure"))
            for (int y = 6; y < h - 6; y++) c.Rect(cx + (int)(MathF.Sin(y * 0.4f) * 6), y, 3, 1, ink);
        else if (Has("Spikes"))
            for (int s = -1; s <= 1; s++)
                for (int y = 0; y < 12; y++) c.Rect(cx + s * 16 - y / 2, cy + 6 - y, y + 1, 1, ink);
        else if (Has("Spin"))
            for (int r = 4; r < Math.Min(w, h) / 2 - 3; r += 5)
                for (int a = 0; a < 40; a++) c.Set(cx + (int)(MathF.Cos(a * 0.157f) * r), cy + (int)(MathF.Sin(a * 0.157f) * r), ink);
        else if (Has("Jump"))
            for (int y = 0; y < 8; y++) c.Rect(cx - y, cy - 6 + y, y * 2 + 1, 1, ink);
        else
            for (int i = 0; i < 6; i++) c.Rect(4 + (i * 7) % (w - 8), 4 + (i * 11) % (h - 8), 2, 2, Rgb(255, 250, 222));
    }

    /// <summary>A poster: a pale sheet in a frame of its colour with a picture of its own (a ball, a wave, a sky, a heart, a face).</summary>
    public static void PaintPoster(PixelCanvas c, DecorationRecord d, Tone tone)
    {
        int w = c.Width, h = c.Height;
        c.Rect(0, 0, w, h, tone.Dark);
        c.Rect(2, 2, w - 4, h - 4, Rgb(236, 232, 220));
        string n = d.Name;
        bool Has(string word) => n.Contains(word, StringComparison.Ordinal);
        int cx = w / 2, cy = h / 2;
        if (Has("Ball")) { Ellipse(c, cx, cy, 7, 7, Tone.Of(218, 62, 58)); c.Rect(cx - 7, cy, 14, 1, Rgb(58, 52, 72)); }
        else if (Has("Sea")) for (int x = 3; x < w - 3; x++) c.Rect(x, cy + (int)(MathF.Sin(x * 0.4f) * 2), 1, h / 2 - 3, Rgb(84, 140, 224));
        else if (Has("Sky")) { c.Rect(3, 3, w - 6, h - 6, Rgb(150, 200, 240)); Ellipse(c, cx, cy, 6, 3, Tone.Of(250, 250, 250)); }
        else if (Has("Kiss") || Has("Cute")) { Ellipse(c, cx - 3, cy - 2, 4, 4, Tone.Of(226, 84, 120)); Ellipse(c, cx + 3, cy - 2, 4, 4, Tone.Of(226, 84, 120)); for (int y = 0; y < 6; y++) c.Rect(cx - 6 + y, cy + y, 12 - y * 2, 1, Rgb(226, 84, 120)); }
        else if (Has("Pika")) { Ellipse(c, cx, cy + 1, 7, 6, Tone.Of(236, 190, 64)); c.Rect(cx - 3, cy, 2, 2, Rgb(40, 36, 52)); c.Rect(cx + 2, cy, 2, 2, Rgb(40, 36, 52)); }
        else c.Rect(5, 5, w - 10, h - 10, tone.Base);
        Outline(c);
    }

    /// <summary>A stuffed figure: the Underground's doll, or a bigger one for the big dolls, two tiles tall on its one.</summary>
    public static void PaintFigure(PixelCanvas c, Tone tone, bool big)
    {
        if (!big)
        {
            UndergroundArt.PaintDoll(c, tone);
            return;
        }
        Ellipse(c, 15, 32, 13, 13, tone);
        Ellipse(c, 15, 14, 11, 10, tone);
        Ellipse(c, 6, 5, 4, 4, tone);
        Ellipse(c, 24, 5, 4, 4, tone);
        var eye = Rgb(40, 36, 52);
        c.Rect(10, 12, 3, 3, eye);
        c.Rect(18, 12, 3, 3, eye);
        c.Rect(11, 28, 9, 7, PixelCanvas.Light1(tone.Base, 0.45f));
        Outline(c);
    }

    /// <summary>What lies on a cushion's top: its pattern's sign (a zigzag, a ball's band, a diamond, a leaf, a flame, a drop, a heart, a ring).</summary>
    public static void PaintCushionMark(PixelCanvas c, DecorationRecord d, Tone tone)
    {
        var ink = tone.Deep;
        string n = d.Name;
        if (n.StartsWith("Zigzag", StringComparison.Ordinal)) for (int x = 0; x < 14; x++) c.Set(x, 4 + (x % 4 < 2 ? x % 4 : 4 - x % 4) * 2, ink);
        else if (n.StartsWith("Ball", StringComparison.Ordinal)) c.HLine(0, 6, 14, ink);
        else if (n.StartsWith("Diamond", StringComparison.Ordinal)) for (int y = 0; y < 6; y++) { c.HLine(7 - y, y, y * 2 + 1, ink); c.HLine(7 - y, 11 - y, y * 2 + 1, ink); }
        else if (n.StartsWith("Round", StringComparison.Ordinal) || n.StartsWith("Spin", StringComparison.Ordinal)) Ellipse(c, 7, 6, 5, 4, Tone.Of(ink));
        else Ellipse(c, 7, 6, 4, 3, Tone.Of(PixelCanvas.Light1(tone.Base, 0.5f)));
    }

    public static void PaintBalloon(PixelCanvas c, Tone tone)
    {
        Ellipse(c, 10, 11, 9, 11, tone);
        c.Rect(9, 22, 2, 2, tone.Dark);
        for (int y = 24; y < 40; y++) c.Set(10 + (y / 4 % 2), y, Rgb(110, 104, 116));
        Outline(c);
    }

    public static void PaintBoard(PixelCanvas c)
    {
        var wood = Tone.Of(186, 140, 92);
        c.Rect(0, 0, c.Width, c.Height, wood.Base);
        for (int y = 7; y < c.Height; y += 8) c.HLine(0, y, c.Width, wood.Dark);
        c.HLine(0, 0, c.Width, wood.Light);
    }

    public static void PaintTire(PixelCanvas c)
    {
        Ellipse(c, 26, 22, 25, 17, Tone.Of(70, 72, 86));
        Ellipse(c, 26, 22, 11, 7, Tone.Of(150, 140, 120));
        Outline(c);
    }

    public static void PaintBall(PixelCanvas c, Tone tone)
    {
        Ellipse(c, 10, 9, 9, 8, tone);
        Outline(c);
    }

    public static void PaintDoor(PixelCanvas c)
    {
        var wood = Tone.Of(176, 128, 84);
        c.Rect(1, 1, 24, 52, wood.Base);
        Raised(c, 1, 1, 24, 52, wood);
        Raised(c, 5, 6, 16, 18, wood);
        Raised(c, 5, 28, 16, 20, wood);
        // A crack across it: it breaks
        for (int i = 0; i < 12; i++) c.Set(6 + i, 22 + (i % 3), wood.Deep);
        c.Rect(19, 27, 3, 3, Rgb(236, 196, 80));
        Outline(c);
    }
}
