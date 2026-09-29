using System;
using System.Collections.Generic;
using Raylib_cs;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Entry point for every generated texture. Art is built once on a <see cref="PixelCanvas"/>,
/// uploaded with point filtering (so pixels stay crisp when scaled) and cached.
/// </summary>
public static class PixelArtGenerator
{
    private static readonly Dictionary<string, Texture2D> TextureCache = new(StringComparer.OrdinalIgnoreCase);

    public const int CharacterFrameWidth = CharacterArt.FrameW;
    public const int CharacterFrameHeight = CharacterArt.FrameH;

    private static Texture2D Cached(string key, Func<Texture2D> build)
    {
        if (TextureCache.TryGetValue(key, out var tex)) return tex;
        tex = build();
        TextureCache[key] = tex;
        return tex;
    }

    /// <summary>128x128 battle sprite (64x64 art at 2x). Draw at a scale that is a multiple of 0.5 to keep pixels even.</summary>
    public static Texture2D GetPokemonSprite(string name, bool isBack) =>
        Cached($"pkmn_{name}_{(isBack ? "back" : "front")}", () => PokemonArt.Draw(name, isBack).ToTexture(upscale: 2));

    /// <summary>48x48 menu icon: the front sprite redrawn at a smaller scale.</summary>
    public static Texture2D GetPokemonIcon(string name) =>
        Cached($"pkmn_icon_{name}", () => PokemonArt.Draw(name, back: false, scale: 0.75f).ToTexture());

    /// <summary>Player walk sheet: rows are <see cref="Direction"/> values, columns are 4 animation frames.</summary>
    public static Texture2D GetPlayerSpriteSheet() => Cached("player_sheet", () =>
    {
        var sheet = new PixelCanvas(CharacterArt.FrameW * 4, CharacterArt.FrameH * 4);
        for (int dir = 0; dir < 4; dir++)
            for (int frame = 0; frame < 4; frame++)
                sheet.Blit(CharacterArt.DrawFrame("PLAYER", (Direction)dir, frame), frame * CharacterArt.FrameW, dir * CharacterArt.FrameH);
        return sheet.ToTexture();
    });

    public static Texture2D GetNpcSprite(string npcType, Direction dir) =>
        Cached($"npc_{npcType}_{(int)dir}", () => CharacterArt.DrawFrame(npcType, dir, 0).ToTexture());

    /// <summary>40x40 ball (20x20 art at 2x).</summary>
    public static Texture2D GetBallTexture(string ballName) => Cached($"ball_{ballName}", () =>
    {
        Color top = ballName.ToUpperInvariant() switch
        {
            "GREAT BALL" => new Color(56, 124, 228, 255),
            "ULTRA BALL" => new Color(52, 52, 62, 255),
            "MASTER BALL" => new Color(164, 60, 184, 255),
            _ => new Color(228, 56, 56, 255)
        };

        // Shade each half as a full sphere, then keep the top of one and the bottom of the other
        var topHalf = new PixelCanvas(20, 20);
        topHalf.Ball(10, 10, 8.5f, 8.5f, top);
        var bottomHalf = new PixelCanvas(20, 20);
        bottomHalf.Ball(10, 10, 8.5f, 8.5f, new Color(244, 244, 248, 255));

        var c = new PixelCanvas(20, 20);
        for (int y = 0; y < 20; y++)
            for (int x = 0; x < 20; x++)
                c.Set(x, y, y < 10 ? topHalf.Get(x, y) : bottomHalf.Get(x, y));

        c.HLine(2, 9, 16, new Color(40, 36, 48, 255));
        c.HLine(2, 10, 16, new Color(40, 36, 48, 255));
        c.Disc(10, 10, 3.4f, new Color(40, 36, 48, 255));
        c.Disc(10, 10, 2.1f, new Color(248, 248, 252, 255));
        c.OutlinePass(innerSeams: false);
        return c.ToTexture(upscale: 2);
    });

    /// <summary>Grassy battle platform, 96x28 art pixels. Scale it by an integer when drawing.</summary>
    public static Texture2D GetBattlePlatformTexture(bool isPlayer) => Cached($"platform_{(isPlayer ? "player" : "enemy")}", () =>
    {
        var c = new PixelCanvas(96, 28);
        var dirt = new Color(176, 140, 96, 255);
        var grass = new Color(116, 196, 96, 255);

        c.FlatEllipse(48, 15, 47, 12.5f, PixelCanvas.Shadow(dirt, 0.35f));
        c.FlatEllipse(48, 13.5f, 47, 12f, dirt);
        c.FlatEllipse(48, 12, 46, 10.5f, PixelCanvas.Shadow(grass, 0.25f));
        c.FlatEllipse(48, 11, 44, 9.5f, grass);
        c.FlatEllipse(42, 9, 30, 5.5f, PixelCanvas.Light1(grass, 0.25f));
        c.FlatEllipse(36, 7.5f, 14, 2.5f, PixelCanvas.Light1(grass, 0.5f));

        // Tufts along the rim
        var rng = new Random(isPlayer ? 3 : 7);
        for (int i = 0; i < 22; i++)
        {
            float a = (float)(rng.NextDouble() * Math.PI);
            int x = (int)(48 + Math.Cos(a) * 42);
            int y = (int)(11 + Math.Sin(a) * 8.5f);
            c.Set(x, y, PixelCanvas.Shadow(grass, 0.35f));
            c.Set(x + 1, y - 1, PixelCanvas.Shadow(grass, 0.35f));
            c.Set(x + 2, y, PixelCanvas.Shadow(grass, 0.35f));
        }
        c.OutlinePass(innerSeams: false);
        return c.ToTexture();
    });

    /// <summary>Battle backdrop, 240x135 art pixels (1/8 of the 1920x1080 virtual screen).</summary>
    public static Texture2D GetBattleBackground() => Cached("battle_bg", () =>
    {
        const int w = 240, h = 135;
        var c = new PixelCanvas(w, h);
        var skyTop = new Color(120, 184, 244, 255);
        var skyBottom = new Color(214, 238, 252, 255);
        int horizon = 62;

        // Sky gradient, one tone per art row
        for (int y = 0; y < horizon; y++)
        {
            c.HLine(0, y, w, PixelCanvas.Mix(skyTop, skyBottom, y / (float)horizon));
        }

        // Clouds
        var cloud = new Color(250, 252, 255, 255);
        foreach (var (cx, cy, s) in new[] { (40, 16, 1f), (150, 10, 1.3f), (208, 30, 0.8f), (96, 34, 0.7f) })
        {
            c.FlatEllipse(cx, cy, 14 * s, 4 * s, cloud);
            c.FlatEllipse(cx - 6 * s, cy - 2 * s, 7 * s, 4 * s, cloud);
            c.FlatEllipse(cx + 5 * s, cy - 3 * s, 8 * s, 5 * s, cloud);
            c.HLine((int)(cx - 12 * s), (int)(cy + 3 * s), (int)(24 * s), new Color(214, 230, 246, 255));
        }

        // Distant hills and a tree line on the horizon
        var hill = new Color(122, 184, 132, 255);
        for (int x = 0; x < w; x++)
        {
            int hy = horizon - 10 + (int)(Math.Sin(x * 0.045) * 4 + Math.Sin(x * 0.11) * 2);
            c.VLine(x, hy, horizon - hy, hill);
        }
        var trees = new Color(72, 140, 92, 255);
        for (int x = -4; x < w; x += 7)
        {
            int th = 7 + (x * 7919 % 5 + 5) % 5;
            c.FlatEllipse(x + 3, horizon - th / 2f, 4.5f, th / 2f + 1, trees);
        }

        // Ground
        var ground = new Color(146, 208, 118, 255);
        var groundFar = new Color(124, 190, 108, 255);
        for (int y = horizon; y < h; y++)
        {
            float t = (y - horizon) / (float)(h - horizon);
            var row = PixelCanvas.Mix(groundFar, ground, Math.Min(1f, t * 1.6f));
            c.HLine(0, y, w, row);
        }
        for (int i = 0; i < 160; i++)
        {
            int x = (i * 7717) % w, y = horizon + 2 + (i * 3593) % (h - horizon - 2);
            c.Set(x, y, PixelCanvas.Shadow(ground, 0.2f));
            c.Set(x + 1, y - 1, PixelCanvas.Light1(ground, 0.3f));
        }
        c.HLine(0, horizon, w, PixelCanvas.Shadow(groundFar, 0.15f));
        return c.ToTexture();
    });
}
