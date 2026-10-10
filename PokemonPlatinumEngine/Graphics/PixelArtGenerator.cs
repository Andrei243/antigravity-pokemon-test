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

    /// <summary>Whether the species has a hand-built model; the others have generated ones (plan 03 · D5).</summary>
    public static bool HasOwnModel(string species) => PokemonModels.HasModel(species);

    /// <summary>48x48 menu icon rendered from the Pokémon's 3D model.</summary>
    public static Texture2D GetPokemonIcon(string name) =>
        PokemonSprites.GetBaked(name, SpriteView.Icon) ?? PokemonSprites.GetBaked(PokemonSprites.Fallback, SpriteView.Icon) ?? SceneTextures.White;

    /// <summary>A Pokémon's menu icon, or an Egg's while it is one (plan 06 · R15).</summary>
    public static Texture2D IconOf(Pokemon p) => p.IsEgg ? EggIcon : GetPokemonIcon(p.ModelName);

    /// <summary>An Egg's menu icon (<see cref="EggArt"/>).</summary>
    public static Texture2D EggIcon => Cached("egg_icon", () => EggArt(PokemonSprites.IconSize).ToTexture());

    /// <summary>A Pokémon's front sprite, or an Egg's while it is one (plan 06 · R15).</summary>
    public static Texture2D SpriteOf(Pokemon p, bool isBack = false) =>
        p.IsEgg ? Cached("egg_sprite", () => EggArt(PokemonSprites.Size).ToTexture()) : GetPokemonSprite(p.ModelName, isBack);

    /// <summary>
    /// An Egg in pixels (plan 06 · R15; style guide, "Eggs"), at the size of the menu sprites and icons: a cream shell
    /// with green spots and a shine, outlined like the baked Pokémon, standing on the canvas's floor.
    /// </summary>
    internal static PixelCanvas EggArt(int size)
    {
        var c = new PixelCanvas(size, size);
        float h = size * 0.62f, w = h * 0.74f;
        float cx = size / 2f, foot = size * 0.84f;
        var shell = new Color(246, 236, 210, 255);
        var shade = new Color(222, 204, 168, 255);
        var spot = new Color(104, 178, 136, 255);
        var spotShade = new Color(84, 150, 116, 255);
        // Whether a texel is in the shell's shade: a crescent along its lower right, its edge following the outline
        // (the shell is lit from the upper left), or null outside the shell
        bool? Shaded(int x, int y)
        {
            // The egg's measure: u across from its middle, v up from its foot, both in heights
            float u = (x + 0.5f - cx) / h, v = (foot - (y + 0.5f)) / h;
            float up = (v - 0.5f) * 2f;
            if (up is < -1f or > 1f) return null;
            float half = w / h / 2f * MathF.Sqrt(1f - up * up) * (1f - 0.17f * up);
            if (MathF.Abs(u) > half) return null;
            float across = half > 0f ? u / half : 0f;
            return -across * 0.75f + up * 0.45f < -0.42f;
        }
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                if (Shaded(x, y) is { } dark) c.SetRaw(x, y, dark ? shade : shell);
        foreach (var (sx, sy, sr) in new[] { (-0.17f, 0.30f, 0.075f), (0.12f, 0.22f, 0.06f), (0.02f, 0.46f, 0.085f), (-0.08f, 0.62f, 0.055f), (0.16f, 0.58f, 0.05f) })
        {
            float px = cx + sx * h, py = foot - sy * h, r = Math.Max(1.5f, sr * h);
            for (int y = (int)(py - r - 1); y <= (int)(py + r + 1); y++)
                for (int x = (int)(px - r - 1); x <= (int)(px + r + 1); x++)
                    if (Shaded(x, y) is { } dark && (x + 0.5f - px) * (x + 0.5f - px) + (y + 0.5f - py) * (y + 0.5f - py) <= r * r)
                        c.SetRaw(x, y, dark ? spotShade : spot);
        }
        // The shine
        float shineX = cx - 0.16f * h, shineY = foot - 0.78f * h, shineR = Math.Max(1f, 0.055f * h);
        for (int y = (int)(shineY - shineR); y <= (int)(shineY + shineR); y++)
            for (int x = (int)(shineX - shineR); x <= (int)(shineX + shineR); x++)
                if (c.IsOpaque(x, y) && (x + 0.5f - shineX) * (x + 0.5f - shineX) + (y + 0.5f - shineY) * (y + 0.5f - shineY) <= shineR * shineR)
                    c.SetRaw(x, y, new Color(255, 255, 250, 255));
        c.OutlinePass(innerSeams: false);
        return c;
    }

    /// <summary>
    /// 40x40 icon of an item (20x20 art at 2x), like the bag icons of the main games: the ball itself for Poké
    /// Balls, a spray bottle for medicine (its colour says which), a crystal for a Revive, a disc in its move's
    /// type colour for a TM, a berry, an envelope, a key, a tonic bottle for battle items, a gem for an
    /// evolution stone, and a pouch for anything else.
    /// </summary>
    public static Texture2D GetItemIcon(ItemData item) => item.Pocket == ItemPocket.PokeBalls
        ? GetBallTexture(item.Name)
        : Cached($"item_{item.Name}", () => ItemIcon(item).ToTexture(upscale: 2));

    /// <summary>The 20x20 art of a non-ball item's icon.</summary>
    internal static PixelCanvas ItemIcon(ItemData item)
    {
        var c = new PixelCanvas(20, 20);
        var white = new Color(244, 244, 248, 255);
        if (item.EffectType == ItemEffectType.LevelUp)
        {
            // A sweet in its wrapper
            var wrap = new Color(96, 150, 236, 255);
            c.Ball(10, 10, 5.5f, 5f, wrap);
            c.Poly(wrap, 4, 10, 1, 6, 1, 14);
            c.Poly(wrap, 16, 10, 19, 6, 19, 14);
            c.Line(8, 7, 12, 13, white);
        }
        else if (item.EffectType == ItemEffectType.Revive)
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
            switch (item.Pocket)
            {
                case ItemPocket.TMsAndHMs:
                {
                    // A disc in the colour of the move's type; an HM's has a pale rim
                    var move = string.IsNullOrEmpty(item.TeachesMove) ? null : MoveDatabase.Get(item.TeachesMove);
                    var color = move != null ? Palette.GetTypeColor(move.Type.ToString()) : new Color(150, 150, 170, 255);
                    if (item.Name.StartsWith("HM", StringComparison.Ordinal)) c.Disc(10, 10, 9f, white);
                    // Flat, with a groove: a disc, not a ball
                    c.Disc(10, 10, 8f, color);
                    c.Disc(10, 10, 5.6f, PixelCanvas.Mix(color, Color.Black, 0.2f));
                    c.Disc(10, 10, 4.6f, color);
                    c.Disc(10, 10, 2.6f, white);
                    c.Disc(10, 10, 1.1f, new Color(70, 70, 90, 255));
                    c.Line(5, 7, 7, 5, PixelCanvas.Mix(color, Color.White, 0.55f));
                    break;
                }
                case ItemPocket.Berries:
                {
                    c.Ball(10, 12, 6.5f, 6f, FromName(item.Name, BerryColors));
                    c.Poly(new Color(92, 176, 84, 255), 9, 7, 12, 2, 16, 4, 12, 8);
                    c.Rect(9, 5, 1, 2, new Color(96, 70, 44, 255));
                    break;
                }
                case ItemPocket.Mail:
                {
                    c.Box(2, 5, 16, 11, new Color(248, 244, 232, 255));
                    var fold = new Color(196, 188, 170, 255);
                    c.Line(2, 5, 9, 11, fold);
                    c.Line(17, 5, 10, 11, fold);
                    c.Disc(10, 11, 1.8f, FromName(item.Name, BerryColors));
                    break;
                }
                case ItemPocket.KeyItems:
                {
                    var gold = new Color(244, 196, 70, 255);
                    c.Ball(6, 10, 4.6f, 4.6f, gold);
                    c.Disc(6, 10, 1.5f, new Color(150, 110, 40, 255));
                    c.Box(10, 9, 8, 3, gold);
                    c.Rect(13, 12, 2, 3, gold);
                    c.Rect(16, 12, 2, 2, gold);
                    break;
                }
                case ItemPocket.BattleItems:
                {
                    // A tonic bottle with an arrow up its label
                    c.Box(6, 7, 9, 11, new Color(238, 134, 60, 255));
                    c.Box(8, 3, 5, 4, new Color(190, 196, 210, 255));
                    c.Rect(10, 12, 1, 4, white);
                    c.Rect(9, 12, 3, 1, white);
                    c.Rect(8, 13, 5, 1, white);
                    c.Dot(10, 11, white);
                    break;
                }
                default:
                {
                    if (item.Name.EndsWith("Stone", StringComparison.Ordinal) && Evolution.IsUsedToEvolve(item))
                    {
                        // A cut gem in the stone's own colour
                        var gem = item.Name.Split(' ')[0] switch
                        {
                            "Fire" => new Color(236, 96, 60, 255),
                            "Water" => new Color(80, 140, 236, 255),
                            "Thunder" => new Color(246, 206, 70, 255),
                            "Leaf" => new Color(96, 190, 96, 255),
                            "Moon" => new Color(130, 120, 170, 255),
                            "Sun" => new Color(246, 150, 60, 255),
                            "Shiny" => new Color(240, 232, 180, 255),
                            "Dusk" => new Color(96, 76, 128, 255),
                            "Dawn" => new Color(90, 200, 190, 255),
                            "Ice" => new Color(150, 220, 240, 255),
                            _ => new Color(190, 160, 220, 255)
                        };
                        c.Poly(gem, 10, 2, 17, 8, 13, 18, 7, 18, 3, 8);
                        c.Line(6, 8, 14, 8, PixelCanvas.Mix(gem, Color.White, 0.5f));
                        c.Line(8, 5, 6, 8, PixelCanvas.Mix(gem, Color.White, 0.5f));
                    }
                    else
                    {
                        var cloth = new Color(196, 150, 96, 255);
                        c.Ball(10, 12, 7, 6.5f, cloth);
                        c.Box(8, 3, 4, 4, cloth);
                        c.Rect(7, 7, 6, 1, new Color(120, 84, 52, 255));
                    }
                    break;
                }
            }
        }
        c.OutlinePass(innerSeams: false);
        return c;
    }

    private static readonly Color[] BerryColors =
    {
        new(232, 84, 84, 255), new(80, 130, 220, 255), new(246, 196, 70, 255), new(236, 130, 170, 255),
        new(110, 190, 100, 255), new(150, 100, 190, 255), new(240, 150, 70, 255)
    };

    /// <summary>One of a few colours, always the same one for a name.</summary>
    private static Color FromName(string name, Color[] colors)
    {
        int sum = 0;
        foreach (char ch in name) sum += ch;
        return colors[sum % colors.Length];
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
