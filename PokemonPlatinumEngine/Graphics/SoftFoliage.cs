using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Foliage for the 3D battles, modelled instead of textured: tiered pines, round trees whose clustered
/// canopies share one set of softened normals (so they shade like a single soft mass) and blade tufts.
/// Colour comes from vertex gradients; nothing uses a noise texture.
/// </summary>
internal static class SoftFoliage
{
    private static float Rand(int x, int y, int salt) => GroundBaker.Rand01(x, y, salt);

    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    private static Color Lerp(Color a, Color b, float t) => PixelCanvas.Mix(a, b, Math.Clamp(t, 0f, 1f));

    /// <summary>Sinnoh pine: four soft tiers with drooping rims, dark undersides and sunlit tips.</summary>
    public static void Pine(MeshBatches batches, float cx, float cz, int seedX, int seedY, float scale = 1f)
    {
        float s = (0.92f + Rand(seedX, seedY, 3) * 0.16f) * scale;
        var white = batches.For(SceneTextures.White);
        white.Cylinder(new(cx, 0, cz), 0.12f * scale, 0.08f * scale, 1.1f * s, 10, Rgb(116, 82, 62), cap: false);

        var tint = Lerp(Rgb(52, 132, 104), Rgb(74, 152, 108), Rand(seedX, seedY, 4));
        (float BaseY, float ApexY, float R)[] tiers =
        {
            (0.5f, 2.0f, 0.74f), (1.22f, 2.7f, 0.62f), (1.94f, 3.4f, 0.5f), (2.66f, 4.1f, 0.36f)
        };
        float spin = Rand(seedX, seedY, 5) * MathF.Tau;
        for (int k = 0; k < tiers.Length; k++)
        {
            var (baseY, apexY, radius) = tiers[k];
            float h = (apexY - baseY) * s, r = radius * s;
            var tierTint = MeshBuilder.Scale(tint, 0.9f + k * 0.06f);
            float sway = 0.1f + k * 0.08f;
            var dark = MeshBuilder.Scale(tierTint, 0.58f);
            var tier = new MeshBuilder();
            tier.Lathe(new Vector3(cx, baseY * s, cz), new[]
            {
                new Vector2(0, 0.14f * h), new Vector2(0.86f * r, -0.03f * h), new Vector2(r, 0.03f * h),
                new Vector2(0.72f * r, 0.28f * h), new Vector2(0.38f * r, 0.64f * h), new Vector2(0, h)
            }, 16, i => i switch
            {
                0 or 1 => MeshBuilder.Sway(dark, sway * 0.5f),
                2 => MeshBuilder.Sway(MeshBuilder.Scale(tierTint, 0.84f), sway * 0.7f),
                3 => MeshBuilder.Sway(tierTint, sway * 0.8f),
                4 => MeshBuilder.Sway(PixelCanvas.Light1(tierTint, 0.08f), sway),
                _ => MeshBuilder.Sway(PixelCanvas.Light1(tierTint, 0.18f), sway)
            });
            // A slight twist per tier so the facets of neighbouring tiers don't line up
            white.Append(tier, Matrix4x4.CreateTranslation(-cx, 0, -cz) * Matrix4x4.CreateRotationY(spin + k * 0.7f) * Matrix4x4.CreateTranslation(cx, 0, cz));
        }
    }

    /// <summary>Round broadleaf tree: a short trunk under a canopy of overlapping blobs that shades as one mass.</summary>
    public static void Round(MeshBatches batches, float cx, float cz, int seedX, int seedY, float scale = 1f)
    {
        float s = (0.92f + Rand(seedX, seedY, 3) * 0.16f) * scale;
        var white = batches.For(SceneTextures.White);
        white.Cylinder(new(cx, 0, cz), 0.15f * scale, 0.1f * scale, 1.2f * s, 10, Rgb(124, 88, 64), cap: false);

        var center = new Vector3(cx, 1.72f * s, cz);
        float R = 0.86f * s;
        var canopy = new MeshBuilder();
        canopy.Ellipsoid(center, new Vector3(0.72f, 0.66f, 0.72f) * s, Color.White, 16, 10);
        int ring = 6;
        float spin = Rand(seedX, seedY, 6) * MathF.Tau;
        for (int i = 0; i < ring; i++)
        {
            float a = spin + i * MathF.Tau / ring;
            float rr = (0.44f + Rand(seedX + i, seedY, 7) * 0.12f) * s;
            var off = new Vector3(MathF.Cos(a) * 0.5f * s, (-0.12f + Rand(seedX + i, seedY, 8) * 0.14f) * s, MathF.Sin(a) * 0.5f * s);
            canopy.Ellipsoid(center + off, new Vector3(rr, rr * 0.9f, rr), Color.White, 14, 9);
        }
        for (int i = 0; i < 2; i++)
        {
            float a = spin + 1.3f + i * MathF.PI;
            var off = new Vector3(MathF.Cos(a) * 0.22f * s, 0.46f * s, MathF.Sin(a) * 0.22f * s);
            canopy.Ellipsoid(center + off, new Vector3(0.44f, 0.4f, 0.44f) * s, Color.White, 14, 9);
        }

        var dark = Lerp(Rgb(48, 116, 70), Rgb(60, 124, 70), Rand(seedX, seedY, 9));
        var light = Lerp(Rgb(128, 196, 98), Rgb(146, 204, 96), Rand(seedX, seedY, 10));
        var core = center - new Vector3(0, 0.2f * s, 0);
        canopy.MapVertices((p, n, _) =>
        {
            // Normals lean toward the canopy's centre-out direction: the blobs keep their silhouette but shade
            // like one rounded crown
            var radial = Vector3.Normalize(p - core);
            var soft = Vector3.Normalize(Vector3.Lerp(n, radial, 0.72f));
            float t = Math.Clamp((p.Y - (center.Y - R)) / (2f * R), 0f, 1f);
            float inward = Math.Clamp(Vector3.Dot(n, radial), 0f, 1f);
            var col = Lerp(dark, light, MathF.Pow(t, 0.85f) * (0.7f + 0.3f * inward));
            return (soft, MeshBuilder.Sway(col, 0.12f + 0.25f * t));
        });
        white.Append(canopy, Matrix4x4.Identity);
    }

    /// <summary>A boulder: a squat rounded mass with a smaller one against it, shaded from a dark foot to a pale top.</summary>
    public static void Rock(MeshBuilder b, float cx, float cz, float size, int seed, Color? footColor = null, Color? bodyColor = null, Color? topColor = null)
    {
        var foot = footColor ?? Rgb(98, 96, 112);
        var body = bodyColor ?? Rgb(146, 142, 148);
        var top = topColor ?? Rgb(196, 192, 190);
        Color Tone(int ring, int rings) => Lerp(ring < rings / 2 ? foot : body, ring < rings / 2 ? body : top, ring < rings / 2 ? ring / (rings / 2f) : (ring - rings / 2) / (rings / 2f));

        float sx = 0.9f + Rand(seed, 1, 41) * 0.3f, sz = 0.8f + Rand(seed, 2, 41) * 0.25f;
        b.Ellipsoid(new Vector3(cx, 0.36f * size, cz), new Vector3(0.62f * sx, 0.5f, 0.56f * sz) * size, i => Tone(i, 8), 14, 8);
        float side = Rand(seed, 3, 41) < 0.5f ? -1f : 1f;
        b.Ellipsoid(new Vector3(cx + side * 0.6f * size, 0.16f * size, cz + 0.22f * size), new Vector3(0.3f, 0.24f, 0.28f) * size, i => Tone(i, 6), 12, 6);
    }

    /// <summary>A tuft of curved grass blades, dark at the root and light at the tips, swaying in the wind.</summary>
    public static void Tuft(MeshBuilder b, float cx, float cz, float width, float height, int seed, Color? rootColor = null, Color? tipColor = null)
    {
        var root = rootColor ?? Rgb(78, 150, 74);
        var tip = tipColor ?? Rgb(160, 214, 112);
        var normal = Vector3.Normalize(new Vector3(0, 0.9f, 0.44f));
        int blades = 7;
        for (int i = 0; i < blades; i++)
        {
            float a = (i + Rand(seed, i, 31) * 0.6f) * MathF.Tau / blades;
            var dir = new Vector3(MathF.Cos(a), 0, MathF.Sin(a) * 0.6f);
            float h = height * (0.6f + Rand(seed, i, 32) * 0.4f);
            float lean = width * (0.25f + Rand(seed, i, 33) * 0.3f);
            float bw = width * 0.09f;
            var side = new Vector3(-dir.Z, 0, dir.X) * bw;
            var basePt = new Vector3(cx, 0, cz) + dir * width * 0.1f;
            var mid = basePt + dir * lean * 0.35f + new Vector3(0, h * 0.55f, 0);
            var top = basePt + dir * lean + new Vector3(0, h, 0);
            var midCol = Lerp(root, tip, 0.55f);
            b.Tri(basePt - side, basePt + side, mid + side * 0.6f, default, default, default, normal, normal, normal,
                root, root, MeshBuilder.Sway(midCol, 0.4f));
            b.Tri(basePt - side, mid + side * 0.6f, mid - side * 0.6f, default, default, default, normal, normal, normal,
                root, MeshBuilder.Sway(midCol, 0.4f), MeshBuilder.Sway(midCol, 0.4f));
            b.Tri(mid - side * 0.6f, mid + side * 0.6f, top, default, default, default, normal, normal, normal,
                MeshBuilder.Sway(midCol, 0.4f), MeshBuilder.Sway(midCol, 0.4f), MeshBuilder.Sway(tip, 1f));
        }
    }
}
