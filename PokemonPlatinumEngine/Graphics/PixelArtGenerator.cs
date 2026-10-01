using System;
using System.Collections.Generic;
using Raylib_cs;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Entry point for every generated texture. Art is built once on a <see cref="PixelCanvas"/>,
/// uploaded with point filtering (so pixels stay crisp when scaled) and cached.
/// </summary>
public static class PixelArtGenerator
{
    private static readonly Dictionary<string, Texture2D> TextureCache = new(StringComparer.OrdinalIgnoreCase);

    private static Texture2D Cached(string key, Func<Texture2D> build)
    {
        if (TextureCache.TryGetValue(key, out var tex)) return tex;
        tex = build();
        TextureCache[key] = tex;
        return tex;
    }

    /// <summary>
    /// 128x128 pixel-art sprite rendered from the Pokémon's 3D model at start-up (see <see cref="PokemonSprites"/>).
    /// Draw at a whole or half-integer scale to keep pixels even.
    /// </summary>
    public static Texture2D GetPokemonSprite(string name, bool isBack)
    {
        var view = isBack ? SpriteView.Back : SpriteView.Front;
        return PokemonSprites.GetBaked(name, view) ?? PokemonSprites.GetBaked(PokemonSprites.Fallback, view) ?? SceneTextures.White;
    }

    /// <summary>48x48 menu icon rendered from the Pokémon's 3D model.</summary>
    public static Texture2D GetPokemonIcon(string name) =>
        PokemonSprites.GetBaked(name, SpriteView.Icon) ?? PokemonSprites.GetBaked(PokemonSprites.Fallback, SpriteView.Icon) ?? SceneTextures.White;

    /// <summary>2D pixel-art figure of a character (the Trainer Card portrait); the field uses the 3D models.</summary>
    public static Texture2D GetNpcSprite(string npcType, Direction dir) =>
        Cached($"npc_{npcType}_{(int)dir}", () => CharacterArt.DrawFrame(npcType, dir, 0).ToTexture());

    /// <summary>
    /// 40x40 icon of an item (20x20 art at 2x), like the bag icons of the main games: the ball itself for Poké
    /// Balls, a spray bottle for medicine (its colour says which), a star for a Revive, a pouch for anything else.
    /// </summary>
    public static Texture2D GetItemIcon(ItemData item) => item.Pocket == ItemPocket.PokeBalls
        ? GetBallTexture(item.Name)
        : Cached($"item_{item.Name}", () => ItemIcon(item).ToTexture(upscale: 2));

    /// <summary>The 20x20 art of a non-ball item's icon.</summary>
    internal static PixelCanvas ItemIcon(ItemData item)
    {
        var c = new PixelCanvas(20, 20);
        var white = new Color(244, 244, 248, 255);
        if (item.EffectType == ItemEffectType.Revive)
        {
            // A four-pointed crystal
            var gold = new Color(250, 204, 70, 255);
            c.Poly(gold, 10, 2, 13, 7, 18, 10, 13, 13, 10, 18, 7, 13, 2, 10, 7, 7);
            c.Disc(9, 9, 1.6f, new Color(255, 244, 190, 255));
        }
        else if (item.EffectType is ItemEffectType.HealHP or ItemEffectType.HealStatus or ItemEffectType.FullRestore)
        {
            Color body = item.Name switch
            {
                "Potion" => new Color(168, 98, 214, 255),
                "Super Potion" => new Color(238, 120, 66, 255),
                "Hyper Potion" => new Color(232, 86, 150, 255),
                "Max Potion" => new Color(70, 134, 226, 255),
                "Full Restore" => new Color(70, 190, 110, 255),
                "Antidote" => new Color(226, 196, 70, 255),
                "Paralyze Heal" => new Color(236, 214, 80, 255),
                _ => new Color(96, 196, 214, 255)
            };
            // Spray bottle: body, white label band, grey neck and a nozzle pointing left
            c.Box(6, 8, 9, 10, body);
            c.Rect(6, 11, 9, 3, white);
            c.Box(8, 5, 5, 3, new Color(170, 176, 192, 255));
            c.Box(7, 2, 7, 3, new Color(206, 210, 222, 255));
            c.Rect(4, 3, 3, 1, new Color(170, 176, 192, 255));
        }
        else
        {
            var cloth = new Color(196, 150, 96, 255);
            c.Ball(10, 12, 7, 6.5f, cloth);
            c.Box(8, 3, 4, 4, cloth);
            c.Rect(7, 7, 6, 1, new Color(120, 84, 52, 255));
        }
        c.OutlinePass(innerSeams: false);
        return c;
    }

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
}
