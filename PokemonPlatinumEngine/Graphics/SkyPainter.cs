using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Battle sky for the new looks: a three-stop gradient that pales toward a hazy horizon, and soft cumulus clouds
/// with sunlit tops and cool, flat undersides drifting across it.
/// </summary>
internal static class SkyPainter
{
    private static Texture2D? cloud;

    private static readonly Color Zenith = new(78, 146, 226, 255);
    private static readonly Color Middle = new(140, 194, 244, 255);
    private static readonly Color Horizon = new(224, 238, 250, 255);

    public static void Draw(float time, int width, int height)
    {
        Raylib.ClearBackground(Horizon);
        int mid = (int)(height * 0.22f), low = (int)(height * 0.46f);
        Raylib.DrawRectangleGradientV(0, 0, width, mid, Zenith, Middle);
        Raylib.DrawRectangleGradientV(0, mid, width, low - mid, Middle, Horizon);

        var tex = Cloud;
        float unit = width / 3840f;
        (float X, float Y, float W, float Speed)[] clouds =
        {
            (0.06f, 0.012f, 620f, 10f), (0.4f, 0.0f, 780f, 7f), (0.74f, 0.03f, 520f, 13f),
            (0.26f, 0.05f, 360f, 16f), (0.92f, 0.055f, 330f, 18f), (0.58f, 0.065f, 300f, 20f)
        };
        foreach (var (cx, cy, cw, speed) in clouds)
        {
            float w = cw * unit, h = w * tex.Height / tex.Width;
            float span = width + w * 2f;
            float x = ((cx * width + time * speed * unit) % span + span) % span - w;
            var dest = new Rectangle(x, cy * height, w, h);
            // Farther (higher, smaller) clouds sit a little deeper in the haze
            byte alpha = (byte)(cw > 500f ? 245 : 215);
            Raylib.DrawTexturePro(tex, new Rectangle(0, 0, tex.Width, tex.Height), dest, Vector2.Zero, 0f, new Color((byte)255, (byte)255, (byte)255, alpha));
        }
    }

    /// <summary>A cumulus built from overlapping discs with a flat base, shaded from a lit crown to a cool underside.</summary>
    private static Texture2D Cloud
    {
        get
        {
            if (cloud.HasValue) return cloud.Value;
            const int w = 512, h = 224;
            var lit = new Vector3(255, 255, 255);
            var shade = new Vector3(196, 212, 236);
            (float X, float Y, float R)[] puffs =
            {
                (0.2f, 0.66f, 0.15f), (0.34f, 0.5f, 0.2f), (0.52f, 0.4f, 0.25f), (0.7f, 0.52f, 0.19f),
                (0.83f, 0.66f, 0.13f), (0.44f, 0.66f, 0.17f), (0.62f, 0.68f, 0.16f)
            };
            float baseY = 0.8f * h;
            var canvas = new PixelCanvas(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    // Signed distance (in texels) to the union of puffs, cut flat along the base
                    float d = float.MaxValue, shadeAmount = 0f;
                    foreach (var (px, py, pr) in puffs)
                    {
                        float dx = x - px * w, dy = y - py * h, r = pr * w;
                        float pd = MathF.Sqrt(dx * dx + dy * dy) - r;
                        if (pd < d)
                        {
                            d = pd;
                            // Each puff is lit from the upper left
                            shadeAmount = Math.Clamp((dy * 0.8f + dx * 0.3f) / r * 0.5f + 0.5f, 0f, 1f);
                        }
                    }
                    d = MathF.Max(d, y - baseY);
                    float alpha = Math.Clamp(0.5f - d / 3f, 0f, 1f);
                    float t = MathF.Max(shadeAmount * 0.7f, Math.Clamp((y - 0.45f * h) / (baseY - 0.45f * h), 0f, 1f));
                    var c = Vector3.Lerp(lit, shade, t * t * 0.9f + t * 0.1f);
                    // Transparent texels keep the cloud colour so bilinear filtering doesn't darken the rim
                    canvas.SetRaw(x, y, new Color((int)c.X, (int)c.Y, (int)c.Z, (int)(alpha * 255)));
                }
            var tex = canvas.ToTexture();
            Raylib.GenTextureMipmaps(ref tex);
            Raylib.SetTextureFilter(tex, TextureFilter.Trilinear);
            cloud = tex;
            return tex;
        }
    }
}
