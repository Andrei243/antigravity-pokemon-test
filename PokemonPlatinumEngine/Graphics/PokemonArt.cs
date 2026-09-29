using System;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Battle and menu sprites for every species, drawn from cel-shaded shapes on a 64x64 grid.
/// Front sprites face left toward the player's Pokémon; back sprites are the same drawing
/// without facial details, mirrored to face the opponent.
/// </summary>
internal static class PokemonArt
{
    public const int Size = 64;

    private static readonly Color EyeDark = new(30, 28, 44, 255);
    private static readonly Color White = new(250, 250, 252, 255);

    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    public static PixelCanvas Draw(string name, bool back, float scale = 1f)
    {
        int size = (int)MathF.Round(Size * scale);
        var c = new PixelCanvas(size, size) { Scale = scale };
        bool front = !back;

        switch (name.ToUpperInvariant())
        {
            case "TURTWIG": Turtwig(c, front); break;
            case "GROTLE": Grotle(c, front); break;
            case "TORTERRA": Torterra(c, front); break;
            case "CHIMCHAR": Chimp(c, front, stage: 0); break;
            case "MONFERNO": Chimp(c, front, stage: 1); break;
            case "INFERNAPE": Infernape(c, front); break;
            case "PIPLUP": Piplup(c, front); break;
            case "PRINPLUP": Prinplup(c, front); break;
            case "EMPOLEON": Empoleon(c, front); break;
            case "STARLY": Bird(c, front, stage: 0); break;
            case "STARAVIA": Bird(c, front, stage: 1); break;
            case "STARAPTOR": Bird(c, front, stage: 2); break;
            case "BIDOOF": Bidoof(c, front); break;
            case "BIBAREL": Bibarel(c, front); break;
            case "SHINX": Cat(c, front, stage: 0); break;
            case "LUXIO": Cat(c, front, stage: 1); break;
            case "LUXRAY": Cat(c, front, stage: 2); break;
            case "RIOLU": Riolu(c, front); break;
            case "LUCARIO": Lucario(c, front); break;
            case "GIBLE": Gible(c, front); break;
            case "GABITE": Garchomp(c, front, big: false); break;
            case "GARCHOMP": Garchomp(c, front, big: true); break;
            case "GIRATINA": Giratina(c, front); break;
            default: Generic(c, front); break;
        }

        if (back) c.MirrorHorizontal();
        c.OutlinePass();
        return c;
    }

    private static void Eye(PixelCanvas c, float x, float y, float rx, float ry, Color? iris = null)
    {
        c.FlatEllipse(x, y, rx, ry, EyeDark);
        if (iris.HasValue)
        {
            c.FlatEllipse(x, y + ry * 0.2f, rx * 0.65f, ry * 0.6f, iris.Value);
            c.FlatEllipse(x, y + ry * 0.25f, rx * 0.3f, ry * 0.35f, EyeDark);
        }
        c.Dot(x - rx * 0.4f, y - ry * 0.45f, White);
    }

    // ------------------------------------------------------------------ grass starters

    private static void Turtwig(PixelCanvas c, bool front)
    {
        var body = Rgb(150, 208, 96);
        var legs = Rgb(118, 178, 78);
        var shell = Rgb(166, 114, 66);
        var rim = Rgb(226, 200, 126);
        var jaw = Rgb(240, 218, 130);
        var leaf = Rgb(92, 196, 82);
        var twig = Rgb(122, 84, 52);

        if (!front)
        {
            // From behind the shell dominates and the head sits beyond it
            c.Ball(22, 27, 14, 12, body);
            c.Part(); c.Limb(22, 16, 22, 9, 1.8f, 1.4f, twig);
            c.Part(); c.Ball(15.5f, 8, 6.5f, 3.8f, leaf);
            c.Part(); c.Ball(28.5f, 8, 6.5f, 3.8f, leaf);
            c.Part(); c.Ball(22, 56, 5.5f, 5, legs);
            c.Part(); c.Ball(50, 55, 5.5f, 5, legs);
            c.Part(); c.Ball(36, 42, 22, 15, shell);
            c.Part(); c.Poly(rim, 14, 46, 58, 44, 56, 53, 18, 55);
            return;
        }

        c.Ball(47, 55, 5, 5, legs);
        c.Part(); c.Ball(40, 42, 18, 13, shell);
        c.Part(); c.Poly(rim, 22, 47, 58, 44, 57, 51, 26, 55);
        c.Part(); c.Ball(24, 55, 5.5f, 5.5f, legs);
        c.Part(); c.Ball(36, 57, 5.5f, 4.5f, legs);
        c.Part(); c.Ball(24, 31, 16, 13.5f, body);
        c.Part(); c.Ball(21, 39.5f, 11, 5.5f, jaw);
        c.Part(); c.Limb(24, 19, 24, 10, 1.8f, 1.4f, twig);
        c.Part(); c.Ball(17.5f, 9, 6.5f, 3.8f, leaf);
        c.Part(); c.Ball(30.5f, 9, 6.5f, 3.8f, leaf);
        c.Line((int)(13 * c.Scale), (int)(9 * c.Scale), (int)(21 * c.Scale), (int)(9 * c.Scale), PixelCanvas.Shadow(leaf, 0.3f));

        if (front)
        {
            Eye(c, 16, 29, 2.6f, 3.6f);
            Eye(c, 29, 28, 2.6f, 3.6f);
            c.Dot(10, 33, EyeDark);
        }
    }

    private static void Grotle(PixelCanvas c, bool front)
    {
        var body = Rgb(116, 170, 84);
        var face = Rgb(232, 206, 112);
        var shell = Rgb(150, 104, 62);
        var bush = Rgb(70, 170, 76);

        if (!front)
        {
            c.Ball(18, 36, 11, 10, body);
            c.Part(); c.Ball(22, 56, 6, 5, body);
            c.Part(); c.Ball(50, 56, 6, 5, body);
            c.Part(); c.Ball(36, 44, 23, 13, body);
            c.Part(); c.Ball(37, 36, 21, 11, shell);
            c.Part(); c.Ball(28, 25, 9, 7.5f, bush);
            c.Part(); c.Ball(44, 23, 9, 8, bush);
            return;
        }

        c.Ball(48, 55, 6, 5, body);
        c.Part(); c.Ball(38, 43, 22, 13, body);
        c.Part(); c.Ball(40, 34, 19, 10, shell);
        c.Part(); c.Ball(32, 24, 8, 7, bush);
        c.Part(); c.Ball(45, 22, 8, 7.5f, bush);
        c.Part(); c.Ball(24, 55, 6, 5.5f, body);
        c.Part(); c.Ball(37, 57, 6, 4.5f, body);
        c.Part(); c.Ball(18, 40, 12, 11, body);
        c.Part(); c.Ball(15, 45, 9, 5.5f, face);
        c.Part(); c.Poly(Rgb(96, 60, 40), 10, 32, 28, 30, 26, 35, 12, 36);

        if (front)
        {
            Eye(c, 12, 39, 2.2f, 2.8f);
            Eye(c, 22, 38, 2.2f, 2.8f);
        }
    }

    private static void Torterra(PixelCanvas c, bool front)
    {
        var body = Rgb(104, 136, 80);
        var shell = Rgb(134, 96, 58);
        var rock = Rgb(176, 180, 190);
        var leaves = Rgb(64, 160, 76);
        var jaw = Rgb(226, 198, 110);

        if (!front)
        {
            c.Ball(12, 44, 10, 9, body);
            c.Part(); c.Ball(20, 57, 7, 5, body);
            c.Part(); c.Ball(52, 57, 7, 5, body);
            c.Part(); c.Ball(36, 46, 26, 12, body);
            c.Part(); c.Ball(37, 37, 23, 10, shell);
            c.Part(); c.Poly(rock, 16, 38, 22, 22, 28, 38);
            c.Part(); c.Poly(rock, 48, 38, 54, 24, 60, 40);
            c.Part(); c.Limb(38, 36, 38, 18, 2.5f, 2, Rgb(122, 84, 52));
            c.Part(); c.Ball(38, 14, 12, 8, leaves);
            c.Part(); c.Ball(32, 10, 6, 4, PixelCanvas.Light1(leaves, 0.2f));
            return;
        }

        c.Ball(50, 55, 7, 6, body);
        c.Part(); c.Ball(36, 45, 25, 12, body);
        c.Part(); c.Ball(38, 36, 22, 9, shell);
        c.Part(); c.Poly(rock, 18, 36, 24, 20, 30, 36);
        c.Part(); c.Poly(rock, 50, 36, 56, 24, 60, 38);
        c.Part(); c.Limb(40, 34, 40, 18, 2.5f, 2, Rgb(122, 84, 52));
        c.Part(); c.Ball(40, 14, 12, 8, leaves);
        c.Part(); c.Ball(34, 10, 6, 4, PixelCanvas.Light1(leaves, 0.2f));
        c.Part(); c.Ball(22, 56, 7, 6, body);
        c.Part(); c.Ball(38, 58, 7, 5, body);
        c.Part(); c.Ball(12, 45, 10, 9, body);
        c.Part(); c.Ball(10, 50, 8, 4.5f, jaw);
        c.Part(); c.Poly(Rgb(74, 96, 60), 4, 38, 20, 37, 20, 42, 6, 43);

        if (front)
        {
            Eye(c, 9, 44, 1.8f, 2.2f, Rgb(250, 214, 90));
            Eye(c, 17, 44, 1.8f, 2.2f, Rgb(250, 214, 90));
        }
    }

    // ------------------------------------------------------------------ fire starters

    private static void Flame(PixelCanvas c, float x, float y, float s)
    {
        var red = Rgb(236, 72, 48);
        var orange = Rgb(250, 150, 56);
        var yellow = Rgb(255, 232, 110);
        c.Part(); c.FlatPoly(red, x, y - 14 * s, x + 7 * s, y - 2 * s, x + 5 * s, y + 6 * s, x - 5 * s, y + 6 * s, x - 7 * s, y - 3 * s, x - 3 * s, y - 6 * s);
        c.FlatPoly(orange, x + 1 * s, y - 9 * s, x + 5 * s, y, x + 3 * s, y + 5 * s, x - 3 * s, y + 5 * s, x - 5 * s, y);
        c.FlatPoly(yellow, x, y - 3 * s, x + 3 * s, y + 2 * s, x + 1 * s, y + 5 * s, x - 2 * s, y + 5 * s, x - 3 * s, y + 2 * s);
    }

    private static void Chimp(PixelCanvas c, bool front, int stage)
    {
        var orange = Rgb(242, 138, 54);
        var cream = Rgb(250, 224, 176);
        var brown = Rgb(170, 98, 52);

        Flame(c, 52, 40, stage == 0 ? 0.9f : 1.2f);
        c.Part(); c.Ball(28, 57, 4.5f, 4.5f, stage == 0 ? orange : brown);
        c.Part(); c.Ball(38, 57, 4.5f, 4.5f, stage == 0 ? orange : brown);
        c.Part(); c.Ball(33, 46, 11, stage == 0 ? 11 : 12, orange);
        c.Part(); c.Ball(31, 48, 6.5f, 7.5f, cream);
        c.Part(); c.Limb(25, 40, 19, 49, 2.8f, 2.4f, orange);
        c.Part(); c.Limb(41, 40, 46, 48, 2.8f, 2.4f, orange);
        c.Part(); c.Ball(16, 25, 4.5f, 5.5f, cream);
        c.Part(); c.Ball(44, 23, 4.5f, 5.5f, cream);
        c.Part(); c.Ball(30, 25, 14.5f, 13.5f, orange);
        c.Part(); c.Poly(orange, 26, 14, 32, 5, 38, 9, 33, 10, 34, 15);

        if (front)
        {
            c.Part(); c.Ball(28, 29, 10.5f, 8.5f, cream);
            if (stage == 1)
            {
                // Monferno's blue face paint
                c.FlatPoly(Rgb(80, 140, 230), 19, 24, 24, 22, 26, 26, 21, 27);
                c.FlatPoly(Rgb(80, 140, 230), 33, 22, 38, 23, 36, 27, 32, 26);
            }

            Eye(c, 23, 27, 2.4f, 3.4f);
            Eye(c, 33, 27, 2.4f, 3.4f);
            c.Dot(27, 32, EyeDark);
            c.Dot(26, 34, Rgb(200, 90, 70));
            c.Dot(27, 34, Rgb(200, 90, 70));
            c.Dot(28, 34, Rgb(200, 90, 70));
        }
    }

    private static void Infernape(PixelCanvas c, bool front)
    {
        var orange = Rgb(236, 124, 44);
        var white = Rgb(246, 244, 240);
        var gold = Rgb(244, 200, 64);
        var blue = Rgb(70, 130, 220);
        var brown = Rgb(150, 84, 44);

        c.Limb(26, 44, 22, 60, 4, 3.5f, brown);
        c.Part(); c.Limb(38, 44, 42, 60, 4, 3.5f, brown);
        c.Part(); c.Ball(32, 38, 12, 13, white);
        c.Part(); c.Ball(32, 44, 9, 6, orange);
        c.Part(); c.Limb(21, 32, 13, 44, 4, 3.5f, white);
        c.Part(); c.Limb(43, 32, 51, 44, 4, 3.5f, white);
        c.Part(); c.Ball(19, 29, 5, 4, gold);
        c.Part(); c.Ball(45, 29, 5, 4, gold);
        Flame(c, 32, 12, 1.4f);
        c.Part(); c.Ball(32, 22, 10, 9, orange);

        if (front)
        {
            c.Part(); c.Ball(30, 25, 7.5f, 6, white);
            c.FlatPoly(blue, 22, 20, 28, 18, 29, 22, 23, 24);
            c.FlatPoly(blue, 34, 18, 40, 20, 38, 24, 33, 22);
            c.FlatPoly(gold, 22, 17, 29, 16, 28, 18, 23, 19);
            c.FlatPoly(gold, 34, 16, 41, 17, 40, 19, 34, 18);
            Eye(c, 26, 22, 1.8f, 2.4f);
            Eye(c, 35, 22, 1.8f, 2.4f);
            c.Dot(29, 27, EyeDark);
            c.Dot(31, 27, EyeDark);
        }
    }

    // ------------------------------------------------------------------ water starters

    private static void Piplup(PixelCanvas c, bool front)
    {
        var dark = Rgb(46, 90, 176);
        var light = Rgb(118, 188, 242);
        var beak = Rgb(250, 206, 64);

        c.Ball(26, 59, 5.5f, 2.5f, beak);
        c.Part(); c.Ball(38, 59, 5.5f, 2.5f, beak);
        c.Part(); c.Ball(32, 46, 13, 13, light);
        if (front) { c.Part(); c.Ball(32, 49, 8, 8.5f, White); }
        else { c.Part(); c.Ball(32, 44, 11, 12, dark); }
        c.Part(); c.Limb(20, 40, 14, 51, 3, 2.2f, dark);
        c.Part(); c.Limb(44, 40, 50, 51, 3, 2.2f, dark);
        c.Part(); c.Ball(32, 24, 15, 14, dark);

        if (front)
        {
            c.Part(); c.Ball(31, 29, 11, 9, light);
            c.Part(); c.Ball(24.5f, 22, 3.5f, 2.8f, White);
            c.Part(); c.Ball(37.5f, 22, 3.5f, 2.8f, White);
            Eye(c, 26, 28, 2.2f, 3);
            Eye(c, 36, 28, 2.2f, 3);
            c.Part(); c.Poly(beak, 26, 32, 36, 32, 31, 38);
            c.FlatEllipse(28, 45, 1.6f, 1.6f, light);
            c.FlatEllipse(36, 45, 1.6f, 1.6f, light);
        }
    }

    private static void Prinplup(PixelCanvas c, bool front)
    {
        var dark = Rgb(40, 76, 158);
        var light = Rgb(96, 164, 232);
        var beak = Rgb(250, 200, 60);

        c.Ball(26, 60, 6, 2.5f, beak);
        c.Part(); c.Ball(38, 60, 6, 2.5f, beak);
        c.Part(); c.Ball(32, 44, 13, 15, light);
        if (front) { c.Part(); c.Ball(32, 46, 8, 11, White); }
        else { c.Part(); c.Ball(32, 42, 11, 13, dark); }
        c.Part(); c.Limb(19, 36, 10, 50, 3.5f, 2.5f, dark);
        c.Part(); c.Limb(45, 36, 54, 50, 3.5f, 2.5f, dark);
        c.Part(); c.Ball(32, 22, 12, 11, light);
        c.Part(); c.Poly(beak, 24, 20, 32, 4, 40, 20, 36, 18, 32, 12, 28, 18);
        if (front)
        {
            c.Part(); c.Ball(32, 26, 6, 5, beak);
            Eye(c, 26, 22, 2, 2.6f);
            Eye(c, 38, 22, 2, 2.6f);
        }
    }

    private static void Empoleon(PixelCanvas c, bool front)
    {
        var navy = Rgb(36, 60, 128);
        var light = Rgb(90, 150, 220);
        var steel = Rgb(196, 204, 220);
        var gold = Rgb(246, 204, 64);

        c.Ball(26, 60, 6, 2.5f, gold);
        c.Part(); c.Ball(38, 60, 6, 2.5f, gold);
        c.Part(); c.Ball(32, 40, 15, 19, navy);
        if (front) { c.Part(); c.Ball(32, 42, 8, 15, White); }
        c.Part(); c.Limb(17, 30, 6, 52, 4.5f, 3, steel);
        c.Part(); c.Limb(47, 30, 58, 52, 4.5f, 3, steel);
        c.Part(); c.Ball(32, 18, 10, 9, light);
        c.Part(); c.Poly(gold, 22, 16, 32, 2, 42, 16, 38, 14, 32, 8, 26, 14);
        c.Part(); c.Poly(gold, 20, 22, 14, 10, 24, 17);
        c.Part(); c.Poly(gold, 44, 22, 50, 10, 40, 17);
        if (front)
        {
            c.Part(); c.Poly(gold, 28, 22, 36, 22, 32, 30);
            Eye(c, 27, 17, 1.8f, 2.4f);
            Eye(c, 37, 17, 1.8f, 2.4f);
        }
    }

    // ------------------------------------------------------------------ Starly line

    private static void Bird(PixelCanvas c, bool front, int stage)
    {
        var body = Rgb(118, 104, 100);
        var dark = Rgb(60, 54, 60);
        var beak = Rgb(246, 150, 48);
        float s = stage switch { 0 => 1f, 1 => 1.15f, _ => 1.3f };

        c.Poly(dark, 44, 40, 60, 34, 58, 46, 46, 50);
        c.Part(); c.Limb(28, 54, 27, 61, 1.4f, 1.2f, beak);
        c.Part(); c.Limb(36, 54, 37, 61, 1.4f, 1.2f, beak);
        c.Part(); c.Ball(34, 43, 14 * s * 0.9f, 12, body);
        c.Part(); c.Ball(30, 48, 8, 7, White);
        c.Part(); c.Poly(dark, 36, 36, 52, 38, 48, 52, 34, 48);
        c.Part(); c.Ball(26, 27, 11, 10, dark);
        c.Part(); c.Ball(22, 31, 7, 5, White);

        // Crest grows with each stage; Staraptor's is tipped red
        if (stage == 0) { c.Part(); c.Poly(dark, 25, 19, 31, 11, 31, 20); }
        else
        {
            c.Part(); c.Poly(dark, 23, 19, 36, 4, 33, 12, 38, 8, 32, 20);
            if (stage == 2) { c.FlatPoly(Rgb(220, 64, 60), 33, 8, 36, 4, 35, 9); }
        }

        if (front)
        {
            Eye(c, 21, 27, 2, 2.4f, stage == 2 ? Rgb(236, 90, 60) : Rgb(246, 150, 48));
            c.Part(); c.Poly(beak, 16, 29, 7, 32, 16, 34);
        }
    }

    // ------------------------------------------------------------------ Bidoof line

    private static void Bidoof(PixelCanvas c, bool front)
    {
        var brown = Rgb(172, 118, 70);
        var tan = Rgb(228, 198, 146);
        var dark = Rgb(112, 74, 46);

        c.Ball(52, 50, 6, 5, dark);
        c.Part(); c.Ball(24, 58, 5, 3, dark);
        c.Part(); c.Ball(40, 58, 5, 3, dark);
        c.Part(); c.Ball(33, 46, 17, 13, brown);
        c.Part(); c.Ball(18, 18, 4, 4, dark);
        c.Part(); c.Ball(40, 17, 4, 4, dark);
        c.Part(); c.Ball(29, 30, 16, 13, brown);
        if (front)
        {
            c.Part(); c.Ball(29, 28, 12, 4, tan);
            c.Part(); c.Ball(27, 35, 9, 6, tan);
            c.Part(); c.Ball(27, 31, 3, 2.4f, Rgb(206, 82, 70));
            c.Part(); c.Box(24.5f, 39, 2.5f, 4, White, shaded: false);
            c.Box(27.5f, 39, 2.5f, 4, White, shaded: false);
            Eye(c, 20, 27, 1.8f, 2.2f);
            Eye(c, 35, 27, 1.8f, 2.2f);
        }
    }

    private static void Bibarel(PixelCanvas c, bool front)
    {
        var brown = Rgb(150, 100, 60);
        var tan = Rgb(226, 196, 142);
        var dark = Rgb(98, 64, 40);

        c.Ball(52, 54, 8, 5, dark);
        c.Part(); c.Ball(24, 59, 6, 3, dark);
        c.Part(); c.Ball(42, 59, 6, 3, dark);
        c.Part(); c.Ball(33, 45, 19, 15, brown);
        c.Part(); c.Ball(33, 48, 12, 10, tan);
        c.Part(); c.Ball(16, 14, 4, 4, dark);
        c.Part(); c.Ball(40, 13, 4, 4, dark);
        c.Part(); c.Ball(28, 26, 15, 12, brown);
        if (front)
        {
            c.Part(); c.Ball(26, 31, 10, 6, tan);
            c.Part(); c.Ball(26, 27, 3, 2.4f, dark);
            c.Part(); c.Box(23.5f, 35, 2.5f, 4, White, shaded: false);
            c.Box(26.5f, 35, 2.5f, 4, White, shaded: false);
            Eye(c, 19, 23, 1.8f, 1.6f);
            Eye(c, 34, 23, 1.8f, 1.6f);
        }
    }

    // ------------------------------------------------------------------ Shinx line

    private static void Star(PixelCanvas c, float x, float y, float r, Color col)
    {
        c.Part();
        c.Poly(col, x, y - r, x + r * 0.3f, y - r * 0.3f, x + r, y, x + r * 0.3f, y + r * 0.3f,
            x, y + r, x - r * 0.3f, y + r * 0.3f, x - r, y, x - r * 0.3f, y - r * 0.3f);
    }

    private static void Cat(PixelCanvas c, bool front, int stage)
    {
        var blue = Rgb(86, 164, 236);
        var black = Rgb(54, 56, 78);
        var yellow = Rgb(250, 214, 64);
        float s = stage switch { 0 => 1f, 1 => 1.1f, _ => 1.2f };

        c.Limb(46, 44, 56, 30, 1.6f, 1.2f, black);
        Star(c, 57, 28, 5 * s, yellow);
        c.Part(); c.Ball(46, 56, 5, 5, black);
        c.Part(); c.Ball(38, 46, 14 * s, 10, blue);
        c.Part(); c.Ball(47, 45, 9, 9.5f, black);
        c.Part(); c.Ball(26, 57, 4.5f, 4.5f, blue);
        c.Part(); c.Ball(35, 58, 4.5f, 4, blue);
        c.FlatEllipse(26, 60, 3.5f, 1.5f, black);
        c.FlatEllipse(35, 60.5f, 3.5f, 1.3f, black);

        c.Part(); c.Ball(14, 19, 5, 6.5f, blue);
        c.Part(); c.Ball(14, 20, 3, 4, black);
        c.Part(); c.Ball(34, 17, 5, 6.5f, blue);
        c.Part(); c.Ball(34, 18, 3, 4, black);
        c.Part(); c.Ball(24, 31, 13, 12, blue);

        // Mane: a tuft on Shinx, a full collar on Luxray
        if (stage == 0) { c.Part(); c.Poly(black, 18, 20, 24, 14, 30, 20, 26, 23, 22, 23); }
        else
        {
            c.Part(); c.Poly(black, 12, 20, 22, 12, 34, 16, 38, 28, 34, 38, 28, 30, 18, 26);
            if (stage == 2) { c.Part(); c.Ball(38, 36, 7, 9, black); }
        }

        if (front)
        {
            Eye(c, 18, 31, 2.6f, 3.2f, yellow);
            Eye(c, 29, 31, 2.6f, 3.2f, yellow);
            c.FlatEllipse(22, 37, 1.6f, 1, Rgb(210, 70, 70));
        }
    }

    // ------------------------------------------------------------------ Riolu line

    private static void Riolu(PixelCanvas c, bool front)
    {
        var blue = Rgb(80, 140, 216);
        var black = Rgb(48, 50, 66);

        c.Ball(44, 48, 3.5f, 4.5f, blue);
        c.Part(); c.Limb(28, 48, 26, 60, 3.2f, 3, black);
        c.Part(); c.Limb(37, 48, 39, 60, 3.2f, 3, black);
        c.Part(); c.Ball(32, 43, 9, 9, blue);
        c.Part(); c.Box(24, 46, 16, 4, black, shaded: false);
        c.Part(); c.Limb(24, 38, 19, 47, 2.6f, 2.2f, blue);
        c.Part(); c.Ball(18.5f, 48, 2.6f, 2.6f, black);
        c.Part(); c.Limb(40, 38, 45, 47, 2.6f, 2.2f, blue);
        c.Part(); c.Ball(45.5f, 48, 2.6f, 2.6f, black);
        c.Part(); c.Poly(blue, 23, 21, 18, 5, 29, 17);
        c.Part(); c.Poly(blue, 41, 21, 46, 5, 35, 17);
        c.Part(); c.Ball(32, 26, 11, 10, blue);
        if (front)
        {
            c.Part(); c.Poly(black, 21, 23, 43, 23, 42, 29, 32, 27, 22, 29);
            Eye(c, 27, 26, 2, 2.2f, Rgb(220, 60, 60));
            Eye(c, 37, 26, 2, 2.2f, Rgb(220, 60, 60));
            c.Part(); c.Ball(32, 31, 4, 2.5f, Rgb(236, 226, 190));
        }
    }

    private static void Lucario(PixelCanvas c, bool front)
    {
        var blue = Rgb(72, 128, 208);
        var black = Rgb(44, 46, 62);
        var cream = Rgb(240, 226, 170);
        var steel = Rgb(222, 228, 240);

        c.Limb(28, 44, 25, 61, 3.5f, 3, black);
        c.Part(); c.Limb(36, 44, 39, 61, 3.5f, 3, black);
        c.Part(); c.Ball(45, 44, 3.5f, 5, blue);
        c.Part(); c.Ball(32, 38, 9, 11, blue);
        c.Part(); c.Ball(31, 34, 6, 6, cream);
        c.Part(); c.Poly(steel, 29, 36, 31, 30, 33, 36);
        c.Part(); c.Limb(23, 30, 16, 42, 2.8f, 2.4f, blue);
        c.Part(); c.Limb(41, 30, 48, 42, 2.8f, 2.4f, blue);
        c.Part(); c.Ball(15.5f, 43, 3, 3, black);
        c.Part(); c.Ball(48.5f, 43, 3, 3, black);
        c.Part(); c.Limb(40, 20, 50, 30, 1.6f, 1.2f, black);
        c.Part(); c.Limb(40, 22, 48, 34, 1.6f, 1.2f, black);
        c.Part(); c.Poly(blue, 25, 16, 21, 2, 30, 13);
        c.Part(); c.Poly(blue, 39, 16, 43, 2, 34, 13);
        c.Part(); c.Ball(32, 20, 9, 8, blue);
        if (front)
        {
            c.Part(); c.Poly(black, 23, 17, 41, 17, 40, 22, 32, 21, 24, 22);
            Eye(c, 28, 19, 1.8f, 2, Rgb(220, 60, 60));
            Eye(c, 36, 19, 1.8f, 2, Rgb(220, 60, 60));
            c.Part(); c.Ball(32, 24, 3.5f, 2.2f, black);
        }
    }

    // ------------------------------------------------------------------ Gible line

    private static void Gible(PixelCanvas c, bool front)
    {
        var navy = Rgb(78, 98, 152);
        var red = Rgb(222, 80, 72);

        c.Ball(48, 52, 5, 4, navy);
        c.Part(); c.Ball(26, 58, 5, 3.5f, navy);
        c.Part(); c.Ball(40, 58, 5, 3.5f, navy);
        c.Part(); c.Ball(34, 45, 13, 13, navy);
        c.Part(); c.Ball(31, 48, 8, 8, red);
        c.Part(); c.Limb(22, 44, 17, 49, 2.4f, 2, navy);
        c.Part(); c.Poly(navy, 26, 14, 34, 2, 40, 14, 36, 12, 34, 8, 31, 14);
        c.Part(); c.Ball(28, 26, 17, 14, navy);
        if (front)
        {
            c.Part(); c.Poly(red, 12, 28, 40, 30, 34, 39, 16, 37);
            c.FlatTri(16, 29, 19, 29, 17.5f, 32, White);
            c.FlatTri(22, 29.5f, 25, 29.5f, 23.5f, 32.5f, White);
            c.FlatTri(28, 30, 31, 30, 29.5f, 33, White);
            Eye(c, 19, 22, 2.4f, 2.8f, Rgb(250, 206, 70));
            c.FlatEllipse(34, 20, 2, 2, Rgb(250, 206, 70));
        }
    }

    private static void Garchomp(PixelCanvas c, bool front, bool big)
    {
        var navy = Rgb(62, 78, 136);
        var red = Rgb(214, 70, 64);
        var yellow = Rgb(250, 206, 70);
        float s = big ? 1f : 0.85f;

        c.Poly(navy, 40, 46, 60, 58, 44, 56);
        c.Part(); c.Limb(28, 46, 24, 60, 4.5f * s, 3.5f, navy);
        c.Part(); c.Limb(38, 46, 42, 60, 4.5f * s, 3.5f, navy);
        c.Part(); c.Ball(33, 38, 11 * s, 14, navy);
        c.Part(); c.Ball(31, 42, 6.5f, 10, red);
        c.Part(); c.Poly(navy, 22, 30, 4, 22, 12, 38, 22, 40);
        c.Part(); c.Poly(navy, 44, 30, 60, 20, 54, 38, 44, 40);
        c.FlatTri(6, 23, 10, 27, 7, 30, White);
        c.FlatTri(58, 21, 55, 26, 58, 29, White);
        c.Part(); c.Poly(navy, 18, 20, 30, 8, 46, 12, 44, 22, 30, 26);
        c.Part(); c.Poly(navy, 34, 12, 40, 2, 44, 12);
        if (front)
        {
            c.Part(); c.Poly(red, 18, 21, 32, 22, 30, 27, 20, 25);
            Star(c, 24, 14, 3, yellow);
            Eye(c, 28, 16, 1.8f, 2, yellow);
        }
    }

    // ------------------------------------------------------------------ Giratina

    private static void Giratina(PixelCanvas c, bool front)
    {
        var gray = Rgb(160, 160, 176);
        var black = Rgb(40, 36, 50);
        var red = Rgb(214, 56, 62);
        var gold = Rgb(236, 196, 72);

        // Shadowy wings with red spikes
        c.Poly(black, 34, 22, 62, 6, 60, 22, 64, 30, 54, 40, 40, 36);
        c.FlatTri(56, 10, 62, 6, 60, 14, red);
        c.FlatTri(58, 24, 64, 30, 58, 30, red);
        c.Part(); c.Poly(black, 30, 22, 2, 8, 6, 22, 0, 30, 10, 40, 24, 36);
        c.FlatTri(6, 12, 2, 8, 4, 16, red);
        c.FlatTri(4, 26, 0, 30, 6, 30, red);

        c.Part(); c.Limb(24, 44, 18, 60, 3, 2.5f, gray);
        c.Part(); c.Limb(40, 44, 46, 60, 3, 2.5f, gray);
        c.Part(); c.Limb(32, 46, 32, 61, 3, 2.5f, gray);
        c.Part(); c.Ball(32, 38, 16, 11, gray);
        for (int i = 0; i < 3; i++)
        {
            float y = 33 + i * 4;
            c.FlatPoly(black, 18, y, 46, y, 45, y + 2, 19, y + 2);
            c.FlatPoly(red, 24, y + 2, 40, y + 2, 39, y + 3, 25, y + 3);
        }
        c.Part(); c.Ball(24, 22, 9, 8, gray);
        c.Part(); c.Poly(gold, 14, 18, 24, 12, 34, 16, 32, 22, 26, 19, 18, 22);
        c.Part(); c.Poly(gold, 16, 24, 10, 30, 18, 28);
        if (front)
        {
            Eye(c, 21, 22, 1.8f, 2, red);
            c.FlatPoly(black, 14, 26, 24, 27, 22, 29, 16, 28);
        }
    }

    private static void Generic(PixelCanvas c, bool front)
    {
        var col = Rgb(168, 168, 120);
        c.Ball(32, 44, 16, 14, col);
        c.Part(); c.Ball(30, 26, 12, 11, col);
        if (front)
        {
            Eye(c, 25, 25, 2, 2.6f);
            Eye(c, 34, 25, 2, 2.6f);
        }
    }
}
