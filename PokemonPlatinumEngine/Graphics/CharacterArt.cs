using System;
using Raylib_cs;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Overworld character sprites (player and NPCs), built from simple pixel shapes at DS-style
/// 16x24 resolution and outlined automatically. Each NPC type is a palette/outfit variation.
/// </summary>
internal static class CharacterArt
{
    public const int FrameW = 16;
    public const int FrameH = 24;

    private enum Headwear { None, Beret, Cap, NurseCap }
    private enum Hair { Short, Spiky, Long, Swept }

    private sealed class Style
    {
        public Color Skin = new(252, 218, 184, 255);
        public Color HairColor = new(72, 52, 44, 255);
        public Hair HairStyle = Hair.Short;
        public Headwear Hat = Headwear.None;
        public Color HatColor = Color.White;
        public Color HatBand = Color.White;
        public Color Top = new(80, 160, 96, 255);
        public Color Accent = new(240, 240, 240, 255);
        public Color Bottom = new(60, 64, 88, 255);
        public Color Shoes = new(72, 56, 52, 255);
        public bool Skirt, Shorts, Mustache, Stripes, Coat;
        public Color? Bag;
    }

    private static Style StyleFor(string npcType) => npcType.ToUpperInvariant() switch
    {
        "PLAYER" or "TRAINER" or "LUCAS" => new Style
        {
            HairColor = new(70, 56, 78, 255),
            Hat = Headwear.Beret, HatColor = new(220, 56, 60, 255), HatBand = new(246, 246, 250, 255),
            Top = new(58, 78, 138, 255), Accent = new(236, 70, 70, 255),
            Bottom = new(48, 50, 70, 255), Shoes = new(200, 70, 60, 255),
            Bag = new(242, 196, 70, 255)
        },
        "RIVAL" => new Style
        {
            HairColor = new(250, 212, 80, 255), HairStyle = Hair.Spiky,
            Top = new(244, 132, 52, 255), Accent = new(72, 176, 104, 255), Stripes = true,
            Bottom = new(58, 70, 108, 255), Shoes = new(236, 236, 240, 255)
        },
        "ROWAN" => new Style
        {
            HairColor = new(236, 236, 242, 255), HairStyle = Hair.Swept, Mustache = true, Coat = true,
            Top = new(126, 92, 66, 255), Accent = new(236, 236, 242, 255),
            Bottom = new(84, 66, 56, 255), Shoes = new(52, 42, 40, 255)
        },
        "NURSE" => new Style
        {
            HairColor = new(248, 150, 190, 255), HairStyle = Hair.Long,
            Hat = Headwear.NurseCap, HatColor = Color.White, HatBand = new(232, 72, 96, 255),
            Top = new(252, 252, 255, 255), Accent = new(248, 170, 200, 255),
            Bottom = new(248, 170, 200, 255), Skirt = true, Shoes = new(248, 248, 252, 255)
        },
        "MOM" => new Style
        {
            HairColor = new(176, 84, 60, 255), HairStyle = Hair.Long,
            Top = new(246, 166, 120, 255), Accent = new(252, 244, 232, 255),
            Bottom = new(120, 96, 176, 255), Skirt = true, Shoes = new(120, 70, 60, 255)
        },
        "LADY" => new Style
        {
            HairColor = new(244, 206, 104, 255), HairStyle = Hair.Long,
            Top = new(118, 186, 132, 255), Accent = new(252, 248, 236, 255),
            Bottom = new(96, 140, 110, 255), Skirt = true, Shoes = new(110, 76, 60, 255)
        },
        "CLERK" => new Style
        {
            HairColor = new(96, 64, 48, 255),
            Top = new(76, 132, 222, 255), Accent = new(246, 246, 250, 255), Stripes = true,
            Bottom = new(56, 60, 80, 255)
        },
        "YOUNGSTER" => new Style
        {
            HairColor = new(84, 56, 44, 255),
            Hat = Headwear.Cap, HatColor = new(250, 200, 60, 255), HatBand = new(60, 110, 200, 255),
            Top = new(250, 250, 252, 255), Accent = new(60, 110, 200, 255),
            Bottom = new(64, 104, 190, 255), Shorts = true, Shoes = new(220, 72, 64, 255)
        },
        "LASS" => new Style
        {
            HairColor = new(96, 60, 52, 255), HairStyle = Hair.Long,
            Top = new(236, 104, 132, 255), Accent = new(252, 240, 244, 255),
            Bottom = new(72, 92, 168, 255), Skirt = true, Shoes = new(84, 56, 52, 255)
        },
        _ => new Style()
    };

    /// <summary>Returns a 16x24 frame. Frames 0/2 stand, 1/3 are opposite steps.</summary>
    public static PixelCanvas DrawFrame(string npcType, Direction dir, int frame)
    {
        string type = npcType.ToUpperInvariant();
        if (type == "STARTERBRIEFCASE") return DrawBriefcase();
        if (type == "RIFT") return DrawRift();

        var s = StyleFor(type);
        var c = new PixelCanvas(FrameW, FrameH);
        bool mirror = dir == Direction.Left;
        if (mirror) dir = Direction.Right;

        if (dir == Direction.Right) DrawSide(c, s, frame);
        else DrawFrontOrBack(c, s, frame, back: dir == Direction.Up);

        if (mirror) c.MirrorHorizontal();
        c.OutlinePass(innerSeams: false);
        return c;
    }

    private static void DrawFrontOrBack(PixelCanvas c, Style s, int frame, bool back)
    {
        var skinShade = PixelCanvas.Shadow(s.Skin, 0.25f);
        var topShade = PixelCanvas.Shadow(s.Top, 0.3f);
        var hairShade = PixelCanvas.Shadow(s.HairColor, 0.3f);
        int leftLift = frame == 3 ? 1 : 0;
        int rightLift = frame == 1 ? 1 : 0;

        // Long hair falls behind the shoulders
        if (s.HairStyle == Hair.Long)
        {
            c.Rect(2, 5, 12, 8, back ? s.HairColor : hairShade);
        }

        // Legs
        if (s.Skirt)
        {
            c.Rect(5, 19, 2, 2 - leftLift, s.Skin);
            c.Rect(9, 19, 2, 2 - rightLift, s.Skin);
        }
        else
        {
            var leg = s.Shorts ? s.Skin : s.Bottom;
            c.Rect(5, 17, 2, 4 - leftLift, leg);
            c.Rect(9, 17, 2, 4 - rightLift, leg);
            c.VLine(6, 17, 4 - leftLift, PixelCanvas.Shadow(leg, 0.2f));
        }
        c.Rect(5, 21 - leftLift, 2, 1, s.Shoes);
        c.Rect(9, 21 - rightLift, 2, 1, s.Shoes);

        // Torso and waist
        c.Rect(4, 11, 8, 6, s.Top);
        c.VLine(11, 11, 6, topShade);
        c.VLine(4, 12, 4, PixelCanvas.Light1(s.Top, 0.2f));
        if (s.Stripes)
        {
            c.HLine(4, 13, 8, PixelCanvas.Light1(s.Top, 0.55f));
            c.HLine(4, 15, 8, PixelCanvas.Light1(s.Top, 0.55f));
        }
        if (s.Skirt)
        {
            c.Rect(4, 16, 8, 3, s.Bottom);
            c.HLine(3, 18, 10, PixelCanvas.Shadow(s.Bottom, 0.25f));
        }
        else if (s.Shorts)
        {
            c.Rect(4, 16, 8, 2, s.Bottom);
        }
        else if (!s.Coat)
        {
            c.Rect(4, 16, 8, 1, s.Bottom);
        }
        if (s.Coat)
        {
            c.Rect(4, 16, 8, 2, s.Top);
            c.VLine(7, 12, 6, topShade);
        }

        // Collar / scarf, and a backpack from behind
        if (!back)
        {
            c.HLine(5, 11, 6, s.Accent);
            c.Rect(7, 12, 2, 1, s.Accent);
        }
        else if (s.Bag.HasValue)
        {
            c.Rect(5, 12, 6, 4, s.Bag.Value);
            c.HLine(5, 12, 6, PixelCanvas.Light1(s.Bag.Value, 0.3f));
            c.HLine(5, 15, 6, PixelCanvas.Shadow(s.Bag.Value, 0.3f));
        }

        // Arms swing opposite to the legs
        c.Rect(3, 12 + rightLift, 1, 4, topShade);
        c.Set(3, 16 + rightLift, s.Skin);
        c.Rect(12, 12 + leftLift, 1, 4, topShade);
        c.Set(12, 16 + leftLift, s.Skin);

        // Head
        c.Rect(3, 2, 10, 9, s.Skin);
        c.Rect(4, 1, 8, 1, s.Skin);
        c.VLine(12, 4, 6, skinShade);
        c.HLine(4, 10, 8, skinShade);

        if (back)
        {
            c.Rect(3, 1, 10, 9, s.HairColor);
            c.Rect(4, 0, 8, 1, s.HairColor);
            c.HLine(4, 9, 8, hairShade);
            c.Rect(6, 10, 4, 1, skinShade);

            // A sheen band and ears so the back of the head doesn't read as a flat block
            c.HLine(5, 5, 6, PixelCanvas.Light1(s.HairColor, 0.25f));
            c.Set(3, 6, skinShade);
            c.Set(12, 6, skinShade);
        }
        else
        {
            // Hair frame
            c.Rect(3, 1, 10, 3, s.HairColor);
            c.Rect(4, 0, 8, 1, s.HairColor);
            c.VLine(3, 4, 4, s.HairColor);
            c.VLine(12, 4, 4, hairShade);
            c.Rect(5, 4, 3, 1, s.HairColor);
            c.HLine(4, 1, 5, PixelCanvas.Light1(s.HairColor, 0.3f));

            // Eyes
            var eye = new Color(36, 34, 56, 255);
            c.VLine(5, 6, 2, eye);
            c.VLine(10, 6, 2, eye);
            c.Set(4, 8, PixelCanvas.Mix(s.Skin, new Color(250, 140, 140, 255), 0.35f));
            c.Set(11, 8, PixelCanvas.Mix(s.Skin, new Color(250, 140, 140, 255), 0.35f));

            if (s.Mustache)
            {
                c.HLine(6, 9, 4, s.HairColor);
                c.Set(5, 9, hairShade);
                c.Set(10, 9, hairShade);
            }
        }

        if (s.HairStyle == Hair.Spiky)
        {
            c.Set(4, 0, s.HairColor);
            c.Set(3, 0, s.HairColor);
            c.Set(7, 0, s.HairColor);
            c.Set(8, 0, s.HairColor);
            c.Set(12, 0, s.HairColor);
            c.Set(13, 1, s.HairColor);
            c.Set(2, 2, s.HairColor);
        }
        else if (s.HairStyle == Hair.Swept)
        {
            c.Rect(3, 0, 9, 2, s.HairColor);
            c.Set(12, 1, s.HairColor);
        }

        DrawHat(c, s, back, side: false);
    }

    private static void DrawSide(PixelCanvas c, Style s, int frame)
    {
        var skinShade = PixelCanvas.Shadow(s.Skin, 0.25f);
        var topShade = PixelCanvas.Shadow(s.Top, 0.3f);
        var hairShade = PixelCanvas.Shadow(s.HairColor, 0.3f);
        bool stepping = frame % 2 == 1;

        if (s.HairStyle == Hair.Long) c.Rect(3, 5, 5, 8, hairShade);

        // Legs: together when standing, apart mid-stride
        var leg = s.Shorts || s.Skirt ? s.Skin : s.Bottom;
        int legTop = s.Skirt ? 19 : 17;
        if (stepping)
        {
            c.Rect(5, legTop, 2, 21 - legTop, PixelCanvas.Shadow(leg, 0.2f));
            c.Rect(9, legTop, 2, 21 - legTop, leg);
            c.Rect(4, 21, 3, 1, s.Shoes);
            c.Rect(9, 21, 3, 1, s.Shoes);
        }
        else
        {
            c.Rect(7, legTop, 3, 21 - legTop, leg);
            c.Rect(7, 21, 4, 1, s.Shoes);
        }

        // Torso
        c.Rect(5, 11, 6, 6, s.Top);
        c.VLine(5, 11, 6, topShade);
        if (s.Stripes)
        {
            c.HLine(5, 13, 6, PixelCanvas.Light1(s.Top, 0.55f));
            c.HLine(5, 15, 6, PixelCanvas.Light1(s.Top, 0.55f));
        }
        if (s.Skirt) { c.Rect(4, 16, 8, 3, s.Bottom); c.HLine(4, 18, 8, PixelCanvas.Shadow(s.Bottom, 0.25f)); }
        else if (s.Shorts) c.Rect(5, 16, 6, 2, s.Bottom);
        else if (s.Coat) { c.Rect(5, 16, 6, 2, s.Top); c.VLine(5, 16, 2, topShade); }
        else c.Rect(5, 16, 6, 1, s.Bottom);
        c.HLine(6, 11, 5, s.Accent);
        if (s.Bag.HasValue)
        {
            c.Rect(3, 12, 2, 4, s.Bag.Value);
            c.VLine(3, 12, 4, PixelCanvas.Shadow(s.Bag.Value, 0.3f));
        }

        // Arm swings forward/back
        int armX = frame == 1 ? 9 : frame == 3 ? 6 : 7;
        c.Rect(armX, 12, 2, 4, topShade);
        c.Rect(armX, 16, 2, 1, s.Skin);

        // Head, facing right
        c.Rect(4, 2, 9, 9, s.Skin);
        c.Rect(5, 1, 7, 1, s.Skin);
        c.HLine(5, 10, 7, skinShade);
        c.Set(13, 7, s.Skin);

        c.Rect(4, 1, 8, 3, s.HairColor);
        c.Rect(5, 0, 6, 1, s.HairColor);
        c.Rect(4, 4, 3, 4, s.HairColor);
        c.Rect(4, 8, 2, 1, hairShade);
        c.HLine(5, 1, 5, PixelCanvas.Light1(s.HairColor, 0.3f));
        c.Set(7, 4, s.HairColor);
        c.Set(8, 4, s.HairColor);

        // Ear where hair meets face
        c.Set(7, 6, skinShade);
        c.Set(7, 7, skinShade);

        c.VLine(10, 6, 2, new Color(36, 34, 56, 255));
        c.Set(11, 8, PixelCanvas.Mix(s.Skin, new Color(250, 140, 140, 255), 0.35f));
        if (s.Mustache) c.HLine(10, 9, 3, s.HairColor);

        if (s.HairStyle == Hair.Spiky)
        {
            c.Set(3, 1, s.HairColor);
            c.Set(2, 3, s.HairColor);
            c.Set(6, 0, s.HairColor);
            c.Set(9, 0, s.HairColor);
            c.Set(3, 5, s.HairColor);
        }
        else if (s.HairStyle == Hair.Swept)
        {
            c.Rect(4, 0, 8, 2, s.HairColor);
            c.Set(12, 1, s.HairColor);
        }

        DrawHat(c, s, back: false, side: true);
    }

    private static void DrawHat(PixelCanvas c, Style s, bool back, bool side)
    {
        var shade = PixelCanvas.Shadow(s.HatColor, 0.3f);
        switch (s.Hat)
        {
            case Headwear.Beret:
                c.Rect(3, 0, 10, 4, s.HatColor);
                c.Rect(2, 1, 12, 2, s.HatColor);
                c.HLine(4, 0, 5, PixelCanvas.Light1(s.HatColor, 0.3f));
                c.HLine(2, 3, 12, shade);
                c.HLine(3, 4, 10, s.HatBand);
                if (side) c.Rect(12, 2, 2, 1, s.HatColor);
                else if (!back) c.Rect(6, 1, 3, 2, s.HatBand);
                break;
            case Headwear.Cap:
                c.Rect(3, 0, 10, 4, s.HatColor);
                c.HLine(4, 0, 6, PixelCanvas.Light1(s.HatColor, 0.3f));
                if (side) c.Rect(11, 3, 4, 1, s.HatBand);
                else if (!back) c.HLine(3, 4, 10, s.HatBand);
                else c.HLine(5, 3, 6, shade);
                break;
            case Headwear.NurseCap:
                c.Rect(5, 0, 6, 2, s.HatColor);
                c.Rect(4, 1, 8, 1, s.HatColor);
                if (!back) { c.Set(7, 0, s.HatBand); c.Set(8, 0, s.HatBand); c.Set(7, 1, s.HatBand); c.Set(8, 1, s.HatBand); }
                break;
        }
    }

    private static PixelCanvas DrawBriefcase()
    {
        var c = new PixelCanvas(FrameW, FrameH);
        var leather = new Color(170, 112, 64, 255);
        c.Rect(6, 12, 4, 2, PixelCanvas.Shadow(leather, 0.4f));
        c.Part();
        c.Box(2, 14, 12, 8, leather);
        c.HLine(2, 17, 12, PixelCanvas.Shadow(leather, 0.35f));
        c.Rect(7, 16, 2, 3, new Color(250, 210, 80, 255));
        c.Set(7, 16, Color.White);
        c.OutlinePass();
        return c;
    }

    private static PixelCanvas DrawRift()
    {
        var c = new PixelCanvas(FrameW, FrameH);
        c.FlatEllipse(8f, 13f, 7.5f, 10.5f, new Color(40, 20, 56, 255));
        c.FlatEllipse(8f, 13f, 5.5f, 8f, new Color(90, 40, 120, 255));
        c.FlatEllipse(8.5f, 13.5f, 3.5f, 5.5f, new Color(200, 60, 80, 255));
        c.FlatEllipse(8.5f, 13.5f, 1.8f, 3f, new Color(16, 8, 24, 255));
        c.Line(3, 6, 6, 9, new Color(150, 90, 200, 255));
        c.Line(13, 20, 10, 17, new Color(150, 90, 200, 255));
        c.OutlinePass(innerSeams: false);
        return c;
    }
}
