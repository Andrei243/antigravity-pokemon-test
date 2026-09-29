using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

public static class PixelArtGenerator
{
    private static readonly Dictionary<string, Texture2D> TextureCache = new(StringComparer.OrdinalIgnoreCase);

    public static Texture2D GetPokemonSprite(string name, bool isBack)
    {
        string key = $"pkmn_{name}_{(isBack ? "back" : "front")}_hd";
        if (TextureCache.TryGetValue(key, out var cached)) return cached;

        int size = 128; // High Definition Sprite
        Image img = Raylib.GenImageColor(size, size, new Color(0, 0, 0, 0));

        DrawPokemonPixelArtHD(ref img, name.ToUpperInvariant(), isBack, size);

        Texture2D tex = Raylib.LoadTextureFromImage(img);
        Raylib.UnloadImage(img);
        Raylib.SetTextureFilter(tex, TextureFilter.Bilinear);
        TextureCache[key] = tex;
        return tex;
    }

    public static Texture2D GetPokemonIcon(string name)
    {
        string key = $"pkmn_icon_{name}_hd";
        if (TextureCache.TryGetValue(key, out var cached)) return cached;

        int size = 48;
        Image img = Raylib.GenImageColor(size, size, new Color(0, 0, 0, 0));
        DrawPokemonIconHD(ref img, size / 2, size / 2, name.ToUpperInvariant());

        Texture2D tex = Raylib.LoadTextureFromImage(img);
        Raylib.UnloadImage(img);
        Raylib.SetTextureFilter(tex, TextureFilter.Bilinear);
        TextureCache[key] = tex;
        return tex;
    }

    public static Texture2D GetPlayerSpriteSheet()
    {
        string key = "player_spritesheet_hd";
        if (TextureCache.TryGetValue(key, out var cached)) return cached;

        // 4 directions x 4 frames, 48x48 each = 192x192
        int frameSize = 48;
        int width = frameSize * 4;
        int height = frameSize * 4;
        Image img = Raylib.GenImageColor(width, height, new Color(0, 0, 0, 0));

        for (int dir = 0; dir < 4; dir++)
        {
            for (int frame = 0; frame < 4; frame++)
            {
                int ox = frame * frameSize;
                int oy = dir * frameSize;
                DrawCharacterFrameHD(ref img, ox, oy, (Direction)dir, frame, isPlayer: true);
            }
        }

        Texture2D tex = Raylib.LoadTextureFromImage(img);
        Raylib.UnloadImage(img);
        Raylib.SetTextureFilter(tex, TextureFilter.Bilinear);
        TextureCache[key] = tex;
        return tex;
    }

    public static Texture2D GetNpcSprite(string npcType, Direction dir)
    {
        string key = $"npc_{npcType}_{(int)dir}_hd";
        if (TextureCache.TryGetValue(key, out var cached)) return cached;

        int size = 48;
        Image img = Raylib.GenImageColor(size, size, new Color(0, 0, 0, 0));
        DrawNpcFrameHD(ref img, 0, 0, dir, npcType);

        Texture2D tex = Raylib.LoadTextureFromImage(img);
        Raylib.UnloadImage(img);
        Raylib.SetTextureFilter(tex, TextureFilter.Bilinear);
        TextureCache[key] = tex;
        return tex;
    }

    public static Texture2D GetTilesetTexture()
    {
        string key = "sinnoh_tileset_hd";
        if (TextureCache.TryGetValue(key, out var cached)) return cached;

        // 32x32 tiles in a 512x512 atlas
        int size = 512;
        Image img = Raylib.GenImageColor(size, size, Palette.GrassGreen);

        DrawTilesetAtlasHD(ref img);

        Texture2D tex = Raylib.LoadTextureFromImage(img);
        Raylib.UnloadImage(img);
        Raylib.SetTextureFilter(tex, TextureFilter.Bilinear);
        TextureCache[key] = tex;
        return tex;
    }

    public static Texture2D GetBallTexture(string ballName)
    {
        string key = $"ball_{ballName}_hd";
        if (TextureCache.TryGetValue(key, out var cached)) return cached;

        int size = 40;
        Image img = Raylib.GenImageColor(size, size, new Color(0, 0, 0, 0));
        DrawPokeballHD(ref img, 20, 20, ballName);

        Texture2D tex = Raylib.LoadTextureFromImage(img);
        Raylib.UnloadImage(img);
        Raylib.SetTextureFilter(tex, TextureFilter.Bilinear);
        TextureCache[key] = tex;
        return tex;
    }

    public static Texture2D GetBattlePlatformTexture(bool isPlayer)
    {
        string key = $"platform_{(isPlayer ? "player" : "enemy")}_hd";
        if (TextureCache.TryGetValue(key, out var cached)) return cached;

        int width = 280;
        int height = 80;
        Image img = Raylib.GenImageColor(width, height, new Color(0, 0, 0, 0));

        // Draw rich Sinnoh grass oval base with gradient & shadow
        Color baseOuter = new(40, 110, 40, 255);
        Color baseMid = new(72, 168, 64, 255);
        Color baseInner = new(112, 216, 96, 255);
        Color baseHighlight = new(160, 240, 136, 255);
        Color dirtBorder = new(144, 112, 72, 255);
        Color dirtShadow = new(104, 78, 48, 255);

        // Ground shadow & dirt base
        Raylib.ImageDrawCircle(ref img, width / 2, height / 2 + 10, 110, dirtShadow);
        Raylib.ImageDrawCircle(ref img, width / 2, height / 2 + 6, 105, dirtBorder);

        // Grass Layers
        Raylib.ImageDrawCircle(ref img, width / 2, height / 2 + 2, 100, baseOuter);
        Raylib.ImageDrawCircle(ref img, width / 2, height / 2, 92, baseMid);
        Raylib.ImageDrawCircle(ref img, width / 2 - 15, height / 2 - 6, 68, baseInner);
        Raylib.ImageDrawCircle(ref img, width / 2 - 30, height / 2 - 10, 42, baseHighlight);

        // Grass blades detail
        for (int i = 30; i < width - 30; i += 6)
        {
            Raylib.ImageDrawLine(ref img, i, height / 2 + 14, i + 3, height / 2 + 6, baseInner);
            Raylib.ImageDrawLine(ref img, i + 2, height / 2 + 10, i + 4, height / 2 + 4, baseHighlight);
        }

        Texture2D tex = Raylib.LoadTextureFromImage(img);
        Raylib.UnloadImage(img);
        Raylib.SetTextureFilter(tex, TextureFilter.Bilinear);
        TextureCache[key] = tex;
        return tex;
    }

    // --- HD Pokemon Drawing ---

    private static void DrawPokemonPixelArtHD(ref Image img, string name, bool isBack, int size)
    {
        int cx = size / 2;
        int cy = isBack ? size / 2 + 12 : size / 2 + 4;

        switch (name)
        {
            case "TURTWIG":
                DrawTurtwigHD(ref img, cx, cy, isBack);
                break;
            case "GROTLE":
            case "TORTERRA":
                DrawTorterraHD(ref img, cx, cy, isBack);
                break;
            case "CHIMCHAR":
                DrawChimcharHD(ref img, cx, cy, isBack);
                break;
            case "MONFERNO":
            case "INFERNAPE":
                DrawInfernapeHD(ref img, cx, cy, isBack);
                break;
            case "PIPLUP":
                DrawPiplupHD(ref img, cx, cy, isBack);
                break;
            case "PRINPLUP":
            case "EMPOLEON":
                DrawEmpoleonHD(ref img, cx, cy, isBack);
                break;
            case "STARLY":
            case "STARAVIA":
            case "STARAPTOR":
                DrawStarlyHD(ref img, cx, cy, isBack);
                break;
            case "SHINX":
            case "LUXIO":
            case "LUXRAY":
                DrawShinxHD(ref img, cx, cy, isBack);
                break;
            case "BIDOOF":
            case "BIBAREL":
                DrawBidoofHD(ref img, cx, cy, isBack);
                break;
            case "RIOLU":
            case "LUCARIO":
                DrawLucarioHD(ref img, cx, cy, isBack);
                break;
            case "GIBLE":
            case "GABITE":
            case "GARCHOMP":
                DrawGarchompHD(ref img, cx, cy, isBack);
                break;
            case "GIRATINA":
                DrawGiratinaHD(ref img, cx, cy, isBack);
                break;
            default:
                DrawGenericPokemonHD(ref img, cx, cy, isBack);
                break;
        }
    }

    private static void DrawTurtwigHD(ref Image img, int cx, int cy, bool isBack)
    {
        Color bodyGreen = new(144, 216, 96, 255);
        Color bodyDark = new(80, 160, 56, 255);
        Color shellBrown = new(168, 120, 72, 255);
        Color shellDark = new(104, 68, 36, 255);
        Color leafGreen = new(96, 224, 72, 255);
        Color leafHighlight = new(160, 248, 120, 255);
        Color eyeBlack = new(24, 28, 36, 255);
        Color yellowJaw = new(248, 224, 104, 255);

        if (!isBack)
        {
            // Drop shadow
            Raylib.ImageDrawCircle(ref img, cx, cy + 28, 28, new Color(0, 0, 0, 70));

            // Feet
            Raylib.ImageDrawRectangle(ref img, cx - 26, cy + 12, 14, 18, bodyDark);
            Raylib.ImageDrawRectangle(ref img, cx + 12, cy + 12, 14, 18, bodyDark);

            // Shell
            Raylib.ImageDrawCircle(ref img, cx, cy, 32, shellDark);
            Raylib.ImageDrawCircle(ref img, cx, cy, 28, shellBrown);

            // Head
            Raylib.ImageDrawCircle(ref img, cx - 4, cy - 14, 26, bodyGreen);
            Raylib.ImageDrawCircle(ref img, cx - 8, cy - 18, 16, new Color(176, 240, 128, 255));

            // Lower jaw
            Raylib.ImageDrawRectangle(ref img, cx - 24, cy - 2, 38, 12, yellowJaw);

            // Big Eye
            Raylib.ImageDrawCircle(ref img, cx + 8, cy - 14, 7, eyeBlack);
            Raylib.ImageDrawCircle(ref img, cx + 10, cy - 16, 3, Color.White);

            // Twig & Leaves
            Raylib.ImageDrawRectangle(ref img, cx - 6, cy - 44, 8, 16, shellDark);
            Raylib.ImageDrawCircle(ref img, cx - 12, cy - 46, 11, leafGreen);
            Raylib.ImageDrawCircle(ref img, cx - 14, cy - 48, 5, leafHighlight);
            Raylib.ImageDrawCircle(ref img, cx + 8, cy - 46, 11, leafGreen);
            Raylib.ImageDrawCircle(ref img, cx + 6, cy - 48, 5, leafHighlight);
        }
        else
        {
            // Back View
            Raylib.ImageDrawCircle(ref img, cx, cy + 28, 32, new Color(0, 0, 0, 70));
            Raylib.ImageDrawRectangle(ref img, cx - 28, cy + 14, 16, 16, bodyDark);
            Raylib.ImageDrawRectangle(ref img, cx + 12, cy + 14, 16, 16, bodyDark);

            // Huge Shell Centerpiece
            Raylib.ImageDrawCircle(ref img, cx, cy + 4, 38, shellDark);
            Raylib.ImageDrawCircle(ref img, cx, cy + 4, 34, shellBrown);
            Raylib.ImageDrawRectangle(ref img, cx - 28, cy + 12, 56, 8, yellowJaw);

            // Back of Head
            Raylib.ImageDrawCircle(ref img, cx - 4, cy - 20, 24, bodyGreen);

            // Twig & Leaves
            Raylib.ImageDrawRectangle(ref img, cx - 6, cy - 50, 8, 18, shellDark);
            Raylib.ImageDrawCircle(ref img, cx - 14, cy - 52, 13, leafGreen);
            Raylib.ImageDrawCircle(ref img, cx + 10, cy - 52, 13, leafGreen);
        }
    }

    private static void DrawChimcharHD(ref Image img, int cx, int cy, bool isBack)
    {
        Color orange = new(248, 144, 48, 255);
        Color orangeDark = new(192, 88, 24, 255);
        Color bellyTan = new(255, 236, 184, 255);
        Color flameYellow = new(255, 248, 96, 255);
        Color flameRed = new(248, 64, 48, 255);
        Color eyeBlack = new(40, 32, 32, 255);

        if (!isBack)
        {
            // Tail flame
            Raylib.ImageDrawCircle(ref img, cx - 30, cy + 12, 16, flameRed);
            Raylib.ImageDrawCircle(ref img, cx - 30, cy + 12, 10, flameYellow);
            Raylib.ImageDrawCircle(ref img, cx - 30, cy + 12, 4, Color.White);

            // Legs
            Raylib.ImageDrawRectangle(ref img, cx - 16, cy + 16, 12, 16, orangeDark);
            Raylib.ImageDrawRectangle(ref img, cx + 6, cy + 16, 12, 16, orangeDark);

            // Body
            Raylib.ImageDrawCircle(ref img, cx, cy + 4, 22, orange);
            Raylib.ImageDrawCircle(ref img, cx + 2, cy + 4, 14, bellyTan);

            // Head
            Raylib.ImageDrawCircle(ref img, cx, cy - 20, 24, orange);
            Raylib.ImageDrawCircle(ref img, cx + 4, cy - 16, 16, bellyTan);

            // Ears
            Raylib.ImageDrawCircle(ref img, cx - 22, cy - 20, 10, orange);
            Raylib.ImageDrawCircle(ref img, cx - 22, cy - 20, 6, flameRed);
            Raylib.ImageDrawCircle(ref img, cx + 22, cy - 20, 10, orange);
            Raylib.ImageDrawCircle(ref img, cx + 22, cy - 20, 6, flameRed);

            // Tuft hair
            Raylib.ImageDrawTriangle(
                ref img,
                new Vector2(cx - 6, cy - 38),
                new Vector2(cx + 12, cy - 50),
                new Vector2(cx + 6, cy - 38),
                orange);

            // Eye
            Raylib.ImageDrawCircle(ref img, cx + 8, cy - 18, 6, eyeBlack);
            Raylib.ImageDrawCircle(ref img, cx + 10, cy - 20, 2, Color.White);
        }
        else
        {
            // Giant Back Flame
            Raylib.ImageDrawCircle(ref img, cx + 16, cy + 4, 24, flameRed);
            Raylib.ImageDrawCircle(ref img, cx + 16, cy + 4, 16, flameYellow);
            Raylib.ImageDrawCircle(ref img, cx + 16, cy + 4, 8, Color.White);

            Raylib.ImageDrawCircle(ref img, cx, cy + 8, 24, orangeDark);
            Raylib.ImageDrawCircle(ref img, cx, cy - 24, 26, orange);

            // Ears
            Raylib.ImageDrawCircle(ref img, cx - 24, cy - 24, 10, orangeDark);
            Raylib.ImageDrawCircle(ref img, cx + 24, cy - 24, 10, orangeDark);

            // Swirl tuft
            Raylib.ImageDrawTriangle(
                ref img,
                new Vector2(cx - 8, cy - 44),
                new Vector2(cx + 16, cy - 56),
                new Vector2(cx + 8, cy - 44),
                orange);
        }
    }

    private static void DrawPiplupHD(ref Image img, int cx, int cy, bool isBack)
    {
        Color blueLight = new(104, 184, 248, 255);
        Color blueCape = new(24, 64, 152, 255);
        Color whiteChest = new(248, 248, 255, 255);
        Color yellowBeak = new(255, 216, 56, 255);
        Color eyeBlack = new(24, 32, 48, 255);

        if (!isBack)
        {
            // Feet
            Raylib.ImageDrawRectangle(ref img, cx - 16, cy + 24, 12, 8, yellowBeak);
            Raylib.ImageDrawRectangle(ref img, cx + 6, cy + 24, 12, 8, yellowBeak);

            // Body
            Raylib.ImageDrawCircle(ref img, cx, cy + 8, 22, blueLight);
            // Cape wings
            Raylib.ImageDrawRectangle(ref img, cx - 26, cy, 10, 20, blueCape);
            Raylib.ImageDrawRectangle(ref img, cx + 18, cy, 10, 20, blueCape);

            // White belly with 2 buttons
            Raylib.ImageDrawCircle(ref img, cx, cy + 10, 14, whiteChest);
            Raylib.ImageDrawCircle(ref img, cx - 6, cy + 8, 4, Color.White);
            Raylib.ImageDrawCircle(ref img, cx + 6, cy + 8, 4, Color.White);

            // Head
            Raylib.ImageDrawCircle(ref img, cx, cy - 16, 26, blueLight);
            Raylib.ImageDrawCircle(ref img, cx, cy - 12, 16, whiteChest);
            Raylib.ImageDrawRectangle(ref img, cx - 12, cy - 28, 24, 12, blueCape);

            // Beak
            Raylib.ImageDrawTriangle(
                ref img,
                new Vector2(cx - 8, cy - 12),
                new Vector2(cx, cy - 2),
                new Vector2(cx + 8, cy - 12),
                yellowBeak);

            // Eye
            Raylib.ImageDrawCircle(ref img, cx + 8, cy - 16, 6, eyeBlack);
            Raylib.ImageDrawCircle(ref img, cx + 8, cy - 18, 2, Color.White);
        }
        else
        {
            Raylib.ImageDrawCircle(ref img, cx, cy + 8, 26, blueCape);
            Raylib.ImageDrawRectangle(ref img, cx - 6, cy + 24, 12, 8, blueLight);
            Raylib.ImageDrawCircle(ref img, cx, cy - 20, 28, blueLight);
            Raylib.ImageDrawRectangle(ref img, cx - 20, cy - 28, 40, 12, blueCape);
        }
    }

    private static void DrawTorterraHD(ref Image img, int cx, int cy, bool isBack)
    {
        Color shellBrown = new(136, 96, 56, 255);
        Color treeGreen = new(56, 152, 64, 255);
        Color foliageHighlight = new(112, 208, 96, 255);
        Color bodyGreen = new(96, 144, 88, 255);
        Color mountainGray = new(176, 184, 192, 255);

        // Huge tank body
        Raylib.ImageDrawRectangle(ref img, cx - 48, cy - 8, 96, 48, shellBrown);
        Raylib.ImageDrawRectangle(ref img, cx - 44, cy + 28, 20, 20, bodyGreen);
        Raylib.ImageDrawRectangle(ref img, cx + 24, cy + 28, 20, 20, bodyGreen);

        // Bonsai tree
        Raylib.ImageDrawRectangle(ref img, cx + 8, cy - 48, 12, 44, shellBrown);
        Raylib.ImageDrawCircle(ref img, cx + 14, cy - 48, 24, treeGreen);
        Raylib.ImageDrawCircle(ref img, cx + 10, cy - 52, 16, foliageHighlight);

        // Mountain rock spikes
        Raylib.ImageDrawTriangle(
            ref img,
            new Vector2(cx - 32, cy - 8),
            new Vector2(cx - 20, cy - 36),
            new Vector2(cx - 8, cy - 8),
            mountainGray);

        if (!isBack)
        {
            Raylib.ImageDrawRectangle(ref img, cx - 60, cy, 28, 24, bodyGreen);
        }
    }

    private static void DrawInfernapeHD(ref Image img, int cx, int cy, bool isBack)
    {
        Color orange = new(232, 120, 32, 255);
        Color goldArmor = new(240, 200, 48, 255);
        Color whiteFur = new(248, 248, 252, 255);
        Color flameYellow = new(255, 240, 80, 255);
        Color flameRed = new(240, 56, 40, 255);

        Raylib.ImageDrawRectangle(ref img, cx - 20, cy - 4, 40, 44, orange);
        Raylib.ImageDrawRectangle(ref img, cx - 16, cy + 4, 32, 24, whiteFur);

        Raylib.ImageDrawCircle(ref img, cx - 20, cy - 4, 10, goldArmor);
        Raylib.ImageDrawCircle(ref img, cx + 20, cy - 4, 10, goldArmor);

        Raylib.ImageDrawCircle(ref img, cx, cy - 28, 22, orange);
        Raylib.ImageDrawCircle(ref img, cx, cy - 48, 24, flameRed);
        Raylib.ImageDrawCircle(ref img, cx, cy - 48, 14, flameYellow);
        Raylib.ImageDrawPixel(ref img, cx, cy - 52, Color.White);
    }

    private static void DrawEmpoleonHD(ref Image img, int cx, int cy, bool isBack)
    {
        Color deepBlue = new(24, 56, 120, 255);
        Color steelGray = new(184, 192, 208, 255);
        Color goldCrown = new(248, 208, 48, 255);

        Raylib.ImageDrawRectangle(ref img, cx - 28, cy - 8, 56, 52, deepBlue);
        Raylib.ImageDrawRectangle(ref img, cx - 12, cy - 4, 24, 44, steelGray);

        Raylib.ImageDrawRectangle(ref img, cx - 44, cy - 4, 16, 48, steelGray);
        Raylib.ImageDrawRectangle(ref img, cx + 28, cy - 4, 16, 48, steelGray);

        Raylib.ImageDrawCircle(ref img, cx, cy - 28, 24, deepBlue);
        Raylib.ImageDrawRectangle(ref img, cx - 4, cy - 56, 8, 28, goldCrown);
        Raylib.ImageDrawRectangle(ref img, cx - 16, cy - 48, 8, 20, goldCrown);
        Raylib.ImageDrawRectangle(ref img, cx + 8, cy - 48, 8, 20, goldCrown);
    }

    private static void DrawStarlyHD(ref Image img, int cx, int cy, bool isBack)
    {
        Color brownGray = new(120, 112, 104, 255);
        Color whiteSpot = new(248, 248, 248, 255);
        Color orangeBeak = new(248, 144, 32, 255);
        Color blackTip = new(32, 32, 40, 255);

        Raylib.ImageDrawCircle(ref img, cx, cy + 4, 24, brownGray);
        Raylib.ImageDrawCircle(ref img, cx - 4, cy - 16, 18, brownGray);
        Raylib.ImageDrawCircle(ref img, cx - 8, cy - 16, 10, whiteSpot);

        Raylib.ImageDrawRectangle(ref img, cx - 6, cy - 36, 8, 16, blackTip);

        if (!isBack)
        {
            Raylib.ImageDrawRectangle(ref img, cx + 8, cy - 16, 14, 8, orangeBeak);
            Raylib.ImageDrawRectangle(ref img, cx + 18, cy - 16, 6, 8, blackTip);
        }
    }

    private static void DrawShinxHD(ref Image img, int cx, int cy, bool isBack)
    {
        Color blue = new(72, 160, 232, 255);
        Color blackHind = new(40, 40, 48, 255);
        Color yellowStar = new(255, 232, 48, 255);

        Raylib.ImageDrawRectangle(ref img, cx - 28, cy - 4, 28, 32, blue);
        Raylib.ImageDrawRectangle(ref img, cx, cy - 4, 28, 32, blackHind);

        Raylib.ImageDrawCircle(ref img, cx - 12, cy - 20, 24, blue);
        Raylib.ImageDrawCircle(ref img, cx - 28, cy - 28, 12, blackHind);
        Raylib.ImageDrawCircle(ref img, cx - 28, cy - 28, 6, yellowStar);
        Raylib.ImageDrawCircle(ref img, cx + 4, cy - 28, 12, blackHind);
        Raylib.ImageDrawCircle(ref img, cx + 4, cy - 28, 6, yellowStar);

        Raylib.ImageDrawLine(ref img, cx + 24, cy + 8, cx + 40, cy - 12, blackHind);
        Raylib.ImageDrawCircle(ref img, cx + 40, cy - 12, 8, yellowStar);
    }

    private static void DrawBidoofHD(ref Image img, int cx, int cy, bool isBack)
    {
        Color brown = new(160, 112, 64, 255);
        Color lightBrown = new(208, 168, 120, 255);
        Color noseRed = new(208, 72, 72, 255);

        Raylib.ImageDrawCircle(ref img, cx, cy + 4, 32, brown);
        Raylib.ImageDrawCircle(ref img, cx, cy - 12, 26, brown);
        Raylib.ImageDrawCircle(ref img, cx - 20, cy - 8, 14, lightBrown);
        Raylib.ImageDrawCircle(ref img, cx + 20, cy - 8, 14, lightBrown);

        if (!isBack)
        {
            Raylib.ImageDrawRectangle(ref img, cx - 8, cy - 4, 16, 12, Color.White);
            Raylib.ImageDrawCircle(ref img, cx, cy - 12, 6, noseRed);
        }
    }

    private static void DrawLucarioHD(ref Image img, int cx, int cy, bool isBack)
    {
        Color blue = new(88, 144, 216, 255);
        Color black = new(40, 48, 56, 255);
        Color creamTorso = new(240, 232, 176, 255);
        Color steelSpike = new(224, 232, 240, 255);

        Raylib.ImageDrawRectangle(ref img, cx - 20, cy + 16, 40, 28, blue);
        Raylib.ImageDrawRectangle(ref img, cx - 12, cy - 4, 24, 24, creamTorso);
        Raylib.ImageDrawRectangle(ref img, cx - 16, cy - 20, 32, 16, black);

        Raylib.ImageDrawCircle(ref img, cx, cy + 4, 6, steelSpike);

        Raylib.ImageDrawCircle(ref img, cx, cy - 32, 18, blue);
        Raylib.ImageDrawRectangle(ref img, cx - 20, cy - 36, 40, 10, black);
        Raylib.ImageDrawRectangle(ref img, cx - 24, cy - 24, 8, 28, black);
        Raylib.ImageDrawRectangle(ref img, cx + 16, cy - 24, 8, 28, black);
    }

    private static void DrawGarchompHD(ref Image img, int cx, int cy, bool isBack)
    {
        Color darkBlue = new(48, 64, 112, 255);
        Color redBelly = new(208, 56, 56, 255);
        Color yellowStar = new(248, 216, 48, 255);

        Raylib.ImageDrawRectangle(ref img, cx - 28, cy - 4, 56, 44, darkBlue);
        Raylib.ImageDrawRectangle(ref img, cx - 16, cy, 32, 32, redBelly);

        Raylib.ImageDrawTriangle(
            ref img,
            new Vector2(cx - 28, cy + 8),
            new Vector2(cx - 56, cy - 16),
            new Vector2(cx - 28, cy + 28),
            darkBlue);
        Raylib.ImageDrawTriangle(
            ref img,
            new Vector2(cx + 28, cy + 8),
            new Vector2(cx + 56, cy - 16),
            new Vector2(cx + 28, cy + 28),
            darkBlue);

        Raylib.ImageDrawCircle(ref img, cx, cy - 32, 24, darkBlue);
        Raylib.ImageDrawRectangle(ref img, cx - 36, cy - 36, 72, 12, darkBlue);
        Raylib.ImageDrawCircle(ref img, cx, cy - 32, 8, yellowStar);
    }

    private static void DrawGiratinaHD(ref Image img, int cx, int cy, bool isBack)
    {
        Color darkGray = new(48, 48, 56, 255);
        Color goldArmor = new(240, 200, 48, 255);
        Color redStripe = new(208, 48, 48, 255);
        Color ghostWing = new(32, 32, 40, 255);

        // Ghostly Wings
        Raylib.ImageDrawRectangle(ref img, cx - 60, cy - 48, 20, 64, ghostWing);
        Raylib.ImageDrawRectangle(ref img, cx + 40, cy - 48, 20, 64, ghostWing);
        Raylib.ImageDrawCircle(ref img, cx - 52, cy - 40, 8, redStripe);
        Raylib.ImageDrawCircle(ref img, cx + 52, cy - 40, 8, redStripe);

        // Dragon Body
        Raylib.ImageDrawRectangle(ref img, cx - 36, cy - 16, 72, 52, darkGray);
        Raylib.ImageDrawRectangle(ref img, cx - 40, cy - 8, 80, 8, goldArmor);
        Raylib.ImageDrawRectangle(ref img, cx - 40, cy + 12, 80, 8, goldArmor);

        Raylib.ImageDrawRectangle(ref img, cx - 16, cy - 44, 32, 32, redStripe);
        Raylib.ImageDrawRectangle(ref img, cx - 16, cy - 40, 32, 8, Color.Black);
        Raylib.ImageDrawCircle(ref img, cx, cy - 48, 20, darkGray);
        Raylib.ImageDrawRectangle(ref img, cx - 28, cy - 60, 56, 12, goldArmor);
    }

    private static void DrawGenericPokemonHD(ref Image img, int cx, int cy, bool isBack)
    {
        Raylib.ImageDrawCircle(ref img, cx, cy, 36, Palette.GetTypeColor("NORMAL"));
        Raylib.ImageDrawCircle(ref img, cx, cy - 24, 24, Palette.GetTypeColor("NORMAL"));
    }

    private static void DrawPokemonIconHD(ref Image img, int cx, int cy, string name)
    {
        Color c = name switch
        {
            "TURTWIG" or "GROTLE" or "TORTERRA" => Palette.GetTypeColor("GRASS"),
            "CHIMCHAR" or "MONFERNO" or "INFERNAPE" => Palette.GetTypeColor("FIRE"),
            "PIPLUP" or "PRINPLUP" or "EMPOLEON" => Palette.GetTypeColor("WATER"),
            "STARLY" or "STARAVIA" or "STARAPTOR" => Palette.GetTypeColor("FLYING"),
            "SHINX" or "LUXIO" or "LUXRAY" => Palette.GetTypeColor("ELECTRIC"),
            "BIDOOF" or "BIBAREL" => Palette.GetTypeColor("NORMAL"),
            "RIOLU" or "LUCARIO" => Palette.GetTypeColor("FIGHTING"),
            "GIBLE" or "GABITE" or "GARCHOMP" => Palette.GetTypeColor("DRAGON"),
            "GIRATINA" => Palette.GetTypeColor("GHOST"),
            _ => Palette.GetTypeColor("NORMAL")
        };

        Raylib.ImageDrawCircle(ref img, cx, cy, 18, c);
        Raylib.ImageDrawCircle(ref img, cx - 4, cy - 4, 6, Color.White);
    }

    private static void DrawPokeballHD(ref Image img, int cx, int cy, string ballType)
    {
        Color topColor = ballType.ToUpperInvariant() switch
        {
            "GREAT BALL" => new Color(48, 120, 224, 255),
            "ULTRA BALL" => new Color(48, 48, 56, 255),
            "MASTER BALL" => new Color(160, 48, 176, 255),
            _ => new Color(224, 48, 48, 255)
        };

        Raylib.ImageDrawCircle(ref img, cx, cy, 18, new Color(40, 40, 48, 255));
        Raylib.ImageDrawRectangle(ref img, cx - 15, cy - 15, 30, 15, topColor);
        Raylib.ImageDrawRectangle(ref img, cx - 15, cy, 30, 15, Color.White);

        Raylib.ImageDrawRectangle(ref img, cx - 16, cy - 2, 32, 4, new Color(40, 40, 48, 255));
        Raylib.ImageDrawCircle(ref img, cx, cy, 7, new Color(40, 40, 48, 255));
        Raylib.ImageDrawCircle(ref img, cx, cy, 4, Color.White);
    }

    private static void DrawCharacterFrameHD(ref Image img, int ox, int oy, Direction dir, int frame, bool isPlayer)
    {
        Color hatWhite = new(248, 248, 252, 255);
        Color beretBlue = new(56, 120, 216, 255);
        Color skin = new(255, 220, 184, 255);
        Color coatDark = new(48, 56, 72, 255);
        Color scarfRed = new(208, 48, 48, 255);
        Color jeansBlue = new(40, 64, 112, 255);
        Color shoesBrown = new(120, 80, 48, 255);

        int cx = ox + 24;
        int cy = oy + 24;
        int stepOffset = (frame == 1) ? -2 : (frame == 3) ? 2 : 0;

        Raylib.ImageDrawCircle(ref img, cx, cy + 18, 11, new Color(0, 0, 0, 80));

        // Legs
        Raylib.ImageDrawRectangle(ref img, cx - 7 + stepOffset, cy + 10, 5, 10, jeansBlue);
        Raylib.ImageDrawRectangle(ref img, cx + 2 - stepOffset, cy + 10, 5, 10, jeansBlue);
        Raylib.ImageDrawRectangle(ref img, cx - 7 + stepOffset, cy + 16, 6, 5, shoesBrown);
        Raylib.ImageDrawRectangle(ref img, cx + 2 - stepOffset, cy + 16, 6, 5, shoesBrown);

        // Torso / Jacket
        Raylib.ImageDrawRectangle(ref img, cx - 8, cy - 1, 16, 13, coatDark);
        Raylib.ImageDrawRectangle(ref img, cx - 9, cy - 3, 18, 5, scarfRed);

        // Head
        Raylib.ImageDrawCircle(ref img, cx, cy - 10, 10, skin);

        if (dir == Direction.Down)
        {
            Raylib.ImageDrawPixel(ref img, cx - 3, cy - 10, Color.Black);
            Raylib.ImageDrawPixel(ref img, cx + 3, cy - 10, Color.Black);
        }
        else if (dir == Direction.Left)
        {
            Raylib.ImageDrawPixel(ref img, cx - 5, cy - 10, Color.Black);
        }
        else if (dir == Direction.Right)
        {
            Raylib.ImageDrawPixel(ref img, cx + 5, cy - 10, Color.Black);
        }

        // Beret Cap
        Raylib.ImageDrawCircle(ref img, cx, cy - 16, 10, hatWhite);
        Raylib.ImageDrawRectangle(ref img, cx - 10, cy - 19, 20, 6, beretBlue);
    }

    private static void DrawNpcFrameHD(ref Image img, int ox, int oy, Direction dir, string npcType)
    {
        int cx = ox + 24;
        int cy = oy + 24;

        Color coatColor = npcType.ToUpperInvariant() switch
        {
            "ROWAN" => new Color(220, 224, 230, 255),
            "RIVAL" => new Color(240, 120, 48, 255),
            "NURSE" => new Color(248, 240, 248, 255),
            "CLERK" => new Color(48, 120, 224, 255),
            _ => new Color(80, 160, 96, 255)
        };

        Color hairColor = npcType.ToUpperInvariant() switch
        {
            "ROWAN" => new Color(220, 220, 220, 255),
            "RIVAL" => new Color(248, 224, 80, 255),
            "NURSE" => new Color(240, 144, 184, 255),
            _ => new Color(96, 64, 48, 255)
        };

        Raylib.ImageDrawCircle(ref img, cx, cy + 18, 11, new Color(0, 0, 0, 80));

        Raylib.ImageDrawRectangle(ref img, cx - 6, cy + 10, 5, 10, new Color(48, 48, 64, 255));
        Raylib.ImageDrawRectangle(ref img, cx + 1, cy + 10, 5, 10, new Color(48, 48, 64, 255));

        Raylib.ImageDrawRectangle(ref img, cx - 8, cy - 1, 16, 13, coatColor);
        Raylib.ImageDrawCircle(ref img, cx, cy - 10, 10, new Color(255, 220, 184, 255));
        Raylib.ImageDrawCircle(ref img, cx, cy - 16, 10, hairColor);

        if (npcType.Equals("ROWAN", StringComparison.OrdinalIgnoreCase))
        {
            Raylib.ImageDrawRectangle(ref img, cx - 5, cy - 7, 10, 3, hairColor);
        }
    }

    private static void DrawTilesetAtlasHD(ref Image img)
    {
        // 32x32 Rich High-Definition Tiles
        int tile = 32;

        // Tile 0: Normal Grass (Rich multi-tone with subtle blades)
        DrawTileRect(ref img, 0, 0, Palette.GrassGreen, tile);
        Raylib.ImageDrawPixel(ref img, 8, 8, Palette.GrassDark);
        Raylib.ImageDrawPixel(ref img, 9, 8, Palette.GrassDark);
        Raylib.ImageDrawPixel(ref img, 24, 20, Palette.GrassDark);
        Raylib.ImageDrawPixel(ref img, 25, 20, Palette.GrassDark);

        // Tile 1: Flower Grass
        DrawTileRect(ref img, 1 * tile, 0, Palette.GrassGreen, tile);
        Raylib.ImageDrawCircle(ref img, 1 * tile + 16, 16, 6, new Color(248, 120, 160, 255));
        Raylib.ImageDrawCircle(ref img, 1 * tile + 16, 16, 3, Color.Yellow);

        // Tile 2: Tall Grass
        DrawTileRect(ref img, 2 * tile, 0, Palette.TallGrass, tile);
        for (int x = 4; x < 28; x += 6)
        {
            Raylib.ImageDrawLine(ref img, 2 * tile + x, 28, 2 * tile + x, 8, Palette.TallGrassTip);
            Raylib.ImageDrawLine(ref img, 2 * tile + x + 1, 28, 2 * tile + x + 1, 8, Palette.TallGrassTip);
        }

        // Tile 3: Dirt Path (Textured Sinnoh cobblestone)
        DrawTileRect(ref img, 3 * tile, 0, Palette.DirtPath, tile);
        Raylib.ImageDrawRectangle(ref img, 3 * tile + 4, 4, 10, 8, Palette.DirtPathDark);
        Raylib.ImageDrawRectangle(ref img, 3 * tile + 18, 16, 10, 8, Palette.DirtPathDark);

        // Tile 4: Water (Shimmering blue)
        DrawTileRect(ref img, 4 * tile, 0, Palette.WaterBlue, tile);
        Raylib.ImageDrawRectangle(ref img, 4 * tile + 4, 8, 16, 3, Palette.WaterDeep);
        Raylib.ImageDrawRectangle(ref img, 4 * tile + 12, 22, 16, 3, Palette.WaterDeep);
        Raylib.ImageDrawRectangle(ref img, 4 * tile + 6, 9, 12, 1, Color.White);

        // Tile 5: Ledge Down
        DrawTileRect(ref img, 5 * tile, 0, Palette.GrassGreen, tile);
        Raylib.ImageDrawRectangle(ref img, 5 * tile, 12, tile, 12, Palette.DirtPathDark);
        Raylib.ImageDrawLine(ref img, 5 * tile, 12, 5 * tile + tile, 12, Color.Black);

        // Tile 6: Tree Top
        DrawTileRect(ref img, 6 * tile, 0, new Color(40, 110, 48, 255), tile);
        Raylib.ImageDrawCircle(ref img, 6 * tile + 16, 16, 12, new Color(72, 176, 80, 255));
        Raylib.ImageDrawCircle(ref img, 6 * tile + 12, 12, 8, new Color(112, 216, 96, 255));

        // Tile 7: Tree Trunk
        DrawTileRect(ref img, 7 * tile, 0, Palette.GrassGreen, tile);
        Raylib.ImageDrawRectangle(ref img, 7 * tile + 10, 0, 12, 24, Palette.WoodBrown);

        // Tile 8: Red Roof (Pokemon Center)
        DrawTileRect(ref img, 8 * tile, 0, Palette.RoofRed, tile);
        Raylib.ImageDrawLine(ref img, 8 * tile, 0, 8 * tile + tile, 0, new Color(248, 128, 128, 255));

        // Tile 9: Blue Roof (Poke Mart)
        DrawTileRect(ref img, 9 * tile, 0, Palette.RoofBlue, tile);
        Raylib.ImageDrawLine(ref img, 9 * tile, 0, 9 * tile + tile, 0, new Color(128, 176, 248, 255));

        // Tile 10: Building Wall
        DrawTileRect(ref img, 10 * tile, 0, Palette.WallBeige, tile);
        Raylib.ImageDrawRectangle(ref img, 10 * tile + 4, 8, 24, 16, Palette.WoodBrown);

        // Tile 11: Door / Warp
        DrawTileRect(ref img, 11 * tile, 0, Palette.WallBeige, tile);
        Raylib.ImageDrawRectangle(ref img, 11 * tile + 6, 4, 20, 28, new Color(40, 40, 48, 255));
        Raylib.ImageDrawCircle(ref img, 11 * tile + 20, 18, 2, Color.Gold);

        // Tile 12: Interior Floor
        DrawTileRect(ref img, 12 * tile, 0, new Color(216, 168, 120, 255), tile);
        Raylib.ImageDrawLine(ref img, 12 * tile, 0, 12 * tile + tile, 0, new Color(184, 136, 88, 255));

        // Tile 13: Signpost
        DrawTileRect(ref img, 13 * tile, 0, Palette.GrassGreen, tile);
        Raylib.ImageDrawRectangle(ref img, 13 * tile + 6, 6, 20, 16, Palette.WoodBrown);
        Raylib.ImageDrawRectangle(ref img, 13 * tile + 14, 22, 4, 10, Palette.WoodBrown);

        // Tile 14: PC Terminal
        DrawTileRect(ref img, 14 * tile, 0, new Color(184, 192, 208, 255), tile);
        Raylib.ImageDrawRectangle(ref img, 14 * tile + 4, 4, 24, 16, new Color(48, 112, 208, 255));
    }

    private static void DrawTileRect(ref Image img, int x, int y, Color c, int size = 32)
    {
        Raylib.ImageDrawRectangle(ref img, x, y, size, size, c);
    }
}
