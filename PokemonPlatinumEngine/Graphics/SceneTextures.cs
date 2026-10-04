using System;
using System.Collections.Generic;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The field's shared textures at 32 texels per tile: repeating plant, rock and roof-tile art (point-filtered
/// pixel art drawn in <see cref="NatureArt"/> and <see cref="BuildingArt"/>), and the soft, filtered shapes that
/// are added as light and shadow. Buildings and props are painted per map into an <see cref="ArtSheet"/> instead.
/// </summary>
internal static class SceneTextures
{
    // Read from the threads that prepare chunks of the world, written only by the thread that owns the window
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Texture2D> Cache = new();
    private static int ownerThread;

    /// <summary>
    /// Makes every texture the field's scenery can ask for, on the thread that owns the window. After this the
    /// chunks of the world can be prepared on other threads: they only look textures up.
    /// </summary>
    public static void Warm()
    {
        ownerThread = Environment.CurrentManagedThreadId;
        _ = White; _ = Bark; _ = Leaves; _ = LeafShell; _ = Needles; _ = TallGrass; _ = LawnTuft; _ = Flowers; _ = LedgeFace;
        _ = BankFace; _ = RockFace; _ = Waterfall;
        _ = LightPool; _ = LampGlow; _ = WindowLight;
    }

    // A texture is made by OpenGL, which belongs to one thread. Asking for a new one from another is a mistake
    // in the code that prepares scenes: whatever it needs must be warmed first.
    private static void MustOwn(string key)
    {
        if (ownerThread != 0 && Environment.CurrentManagedThreadId != ownerThread)
            throw new InvalidOperationException($"The texture '{key}' was first asked for off the main thread; warm it before preparing scenes in the background.");
    }

    private static Texture2D Get(string key, Func<PixelCanvas> build, bool repeat)
    {
        if (Cache.TryGetValue(key, out var tex)) return tex;
        MustOwn(key);
        tex = build().ToTexture();
        Raylib.SetTextureWrap(tex, repeat ? TextureWrap.Repeat : TextureWrap.Clamp);
        Cache[key] = tex;
        return tex;
    }

    /// <summary>A soft shape drawn with bilinear filtering: the alpha of each texel comes from <paramref name="alpha"/> (u, v in 0..1).</summary>
    private static Texture2D Soft(string key, Color color, Func<float, float, float> alpha)
    {
        if (Cache.TryGetValue(key, out var cached)) return cached;
        MustOwn(key);
        const int s = 64;
        var c = new PixelCanvas(s, s);
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
                c.SetRaw(x, y, new Color(color.R, color.G, color.B, (byte)Math.Clamp((int)(alpha((x + 0.5f) / s, (y + 0.5f) / s) * 255f), 0, 255)));
        var tex = c.ToTexture();
        Raylib.SetTextureFilter(tex, TextureFilter.Bilinear);
        Raylib.SetTextureWrap(tex, TextureWrap.Clamp);
        Cache[key] = tex;
        return tex;
    }

    private static float Smooth(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    private static float FromCenter(float u, float v) => MathF.Sqrt((u * 2f - 1f) * (u * 2f - 1f) + (v * 2f - 1f) * (v * 2f - 1f));

    // ------------------------------------------------------------------ repeating art

    public static Texture2D White => Get("white", () =>
    {
        var c = new PixelCanvas(4, 4);
        c.Fill(Color.White);
        return c;
    }, repeat: true);

    /// <summary>Roof tiles in one colour (<see cref="BuildingArt.RoofTiles"/>): one repeating texture per colour.</summary>
    public static Texture2D RoofTiles(Color color) =>
        Get($"roof_{color.R}_{color.G}_{color.B}", () => BuildingArt.RoofTiles(color), repeat: true);

    // Plants and rocks: the art is drawn in NatureArt (grey for trees, tinted per tree)
    public static Texture2D Bark => Get("bark", NatureArt.Bark, repeat: true);

    /// <summary>Dense leaf clumps for the solid core of round tree crowns.</summary>
    public static Texture2D Leaves => Get("leaves", () => NatureArt.Leaves(cutout: false), repeat: true);

    /// <summary>Leaf clumps with gaps, for the outer shell that gives crowns a leafy silhouette.</summary>
    public static Texture2D LeafShell => Get("leaf_shell", () => NatureArt.Leaves(cutout: true), repeat: true);

    /// <summary>A pine tier: U wraps around the tree, V runs from the tip (0) to the toothed rim (1).</summary>
    public static Texture2D Needles => Get("needles", NatureArt.Needles, repeat: true);

    /// <summary>Clumps of tall grass, repeating along a row (cut out by the field shader).</summary>
    public static Texture2D TallGrass => Get("tall_grass", NatureArt.TallGrass, repeat: true);

    /// <summary>A small clump of grass for scattering over lawns.</summary>
    public static Texture2D LawnTuft => Get("lawn_tuft", NatureArt.LawnTuft, repeat: false);

    /// <summary>Three flowers side by side (white, red, yellow), one frame each.</summary>
    public static Texture2D Flowers => Get("flowers", NatureArt.Flowers, repeat: false);

    public static Texture2D LedgeFace => Get("ledge_face", NatureArt.LedgeFace, repeat: true);

    /// <summary>The face of a step in the ground under grass (style guide, "Relief"): its top rows once, the rest repeating downward.</summary>
    public static Texture2D BankFace => Get("bank_face", NatureArt.BankFace, repeat: true);

    /// <summary>The face of a step in the ground under rock, snow or a cave's floor.</summary>
    public static Texture2D RockFace => Get("rock_face", NatureArt.RockFace, repeat: true);

    /// <summary>The sheet of a waterfall, repeating down it.</summary>
    public static Texture2D Waterfall => Get("waterfall", NatureArt.Waterfall, repeat: true);

    private static Image[]? waterfallFrames;
    private static int waterfallFrame;

    /// <summary>
    /// Lets the waterfalls fall: the sheet's art moves down four texels eight times a second, in whole texels,
    /// by rewriting the one texture every waterfall shares. Call on the thread that owns the window.
    /// </summary>
    public static unsafe void AnimateWaterfall(double time)
    {
        if (!Cache.TryGetValue("waterfall", out var texture)) return;
        int frame = (int)((long)(time * 8.0) % 8);
        if (frame == waterfallFrame) return;
        waterfallFrame = frame;
        if (waterfallFrames == null)
        {
            var art = NatureArt.Waterfall();
            waterfallFrames = new Image[8];
            for (int f = 0; f < 8; f++)
            {
                var shifted = new PixelCanvas(art.Width, art.Height);
                for (int y = 0; y < art.Height; y++)
                    for (int x = 0; x < art.Width; x++)
                        shifted.SetRaw(x, (y + f * 4) % art.Height, art.Get(x, y));
                waterfallFrames[f] = shifted.ToImage();
            }
        }
        Raylib.UpdateTexture(texture, waterfallFrames[frame].Data);
    }

    /// <summary>The cells of the field's small effects: prints, dust, leaves, drops, rings (<see cref="LifeArt.Atlas"/>).</summary>
    public static Texture2D Life => Get("life", LifeArt.Atlas, repeat: false);

    /// <summary>Mist, drawn large and smooth over the picture in drifting layers (<see cref="LifeArt.Haze"/>).</summary>
    public static Texture2D Haze
    {
        get
        {
            if (Cache.TryGetValue("haze", out var cached)) return cached;
            var tex = Get("haze", LifeArt.Haze, repeat: true);
            Raylib.SetTextureFilter(tex, TextureFilter.Bilinear);
            return tex;
        }
    }

    /// <summary>The bubble over someone's head, by what it shows.</summary>
    public static Texture2D Bubble(EmoteBubble kind) => Get("bubble_" + kind, () => LifeArt.Bubble(kind), repeat: false);

    /// <summary>Battle meadow and platform tops are smooth, filtered textures (see <see cref="SoftTextures"/>).</summary>
    public static Texture2D Meadow => SoftTextures.Meadow;

    public static Texture2D PlatformTop => SoftTextures.PlatformTop;

    // ------------------------------------------------------------------ light and shadow

    /// <summary>Soft round shadow (alpha-blended, not cut out).</summary>
    public static Texture2D ShadowBlob =>
        Soft("shadow_blob", Color.Black, (u, v) => 90f / 255f * Math.Clamp((1f - FromCenter(u, v)) * 2.2f, 0f, 1f));

    /// <summary>
    /// Warm light spilling onto the ground from a lit window or door (added after dark): brightest at the wall
    /// (top edge, centre) and fading out in a half ellipse.
    /// </summary>
    public static Texture2D LightPool => Soft("light_pool", new Color(255, 184, 104, 255),
        (u, v) => 105f / 255f * Smooth(1f - MathF.Sqrt((u * 2f - 1f) * (u * 2f - 1f) + v * v)));

    /// <summary>
    /// Lamplight fading out evenly from its middle (added after dark): the pool at a street lamp's foot and the
    /// halo round its lantern.
    /// </summary>
    public static Texture2D LampGlow => Soft("lamp_glow", new Color(255, 190, 110, 255), (u, v) => 120f / 255f * Smooth(1f - FromCenter(u, v)));

    /// <summary>
    /// Daylight through a window lying on a room's floor: soft at its edges and fading away from the wall (the
    /// top of the texture).
    /// </summary>
    public static Texture2D WindowLight => Soft("window_light", new Color(255, 248, 226, 255), (u, v) =>
        46f / 255f * Smooth(Math.Min(u, 1f - u) / 0.12f) * Smooth((1f - v) / 0.16f) * Smooth(v / 0.05f) * (1f - 0.45f * v));

    /// <summary>A soft white dot that fades out evenly, for glows and drifting motes (drawn additively).</summary>
    public static Texture2D SoftGlow => Soft("soft_glow", Color.White, (u, v) =>
    {
        float fall = Math.Clamp(1f - FromCenter(u, v), 0f, 1f);
        return fall * fall;
    });
}
