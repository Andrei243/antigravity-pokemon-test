using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>How a batch of static geometry is shaded.</summary>
internal enum MeshPass { Opaque, Ground, Water, SoftWater, Glow, Light }

/// <summary>Collects static geometry into one mesh per texture and pass, which keeps draw calls low.</summary>
internal sealed class MeshBatches
{
    private readonly Dictionary<(uint, MeshPass), (Texture2D Tex, MeshBuilder Builder)> map = new();

    public MeshBuilder For(Texture2D tex, MeshPass pass = MeshPass.Opaque)
    {
        if (!map.TryGetValue((tex.Id, pass), out var entry))
        {
            entry = (tex, new MeshBuilder());
            map[(tex.Id, pass)] = entry;
        }
        return entry.Builder;
    }

    public IEnumerable<(Texture2D Tex, MeshPass Pass, MeshBuilder Builder)> All
    {
        get { foreach (var (key, e) in map) yield return (e.Tex, key.Item2, e.Builder); }
    }
}

/// <summary>
/// Uploaded static scenery: each batch drawn with the lit scene shader (or the water shader), and the opaque
/// batches drawn again into the shadow map. Glazed batches (windows, glass doors) glow after dark.
/// </summary>
internal sealed class SceneMeshes
{
    private sealed record Part(Mesh Mesh, Material Main, Material? Depth, bool Glows);
    private readonly List<Part> parts = new();
    private readonly List<(Mesh Mesh, Material Material)> lights = new();
    private FieldShaders shaders = null!;

    public static SceneMeshes Upload(MeshBatches batches, FieldShaders shaders)
    {
        var result = new SceneMeshes { shaders = shaders };
        foreach (var (tex, pass, builder) in batches.All)
        {
            if (builder.VertexCount == 0) continue;
            if (pass == MeshPass.Light)
            {
                // Light pools are unlit and added on top of the scene, so they use raylib's default shader
                var glowing = Raylib.LoadMaterialDefault();
                Raylib.SetMaterialTexture(ref glowing, MaterialMapIndex.Albedo, tex);
                result.lights.Add((builder.Upload(), glowing));
                continue;
            }
            var shader = pass == MeshPass.Water ? shaders.Water : pass == MeshPass.SoftWater ? shaders.SoftWater : shaders.World;
            var main = RenderContext.MaterialFor(shader, tex);
            Material? depth = pass is MeshPass.Opaque or MeshPass.Glow ? RenderContext.MaterialFor(shaders.Depth, tex) : null;
            result.parts.Add(new Part(builder.Upload(), main, depth, pass == MeshPass.Glow));
        }
        return result;
    }

    /// <param name="glow">How brightly windows and glass doors are lit from inside (0 by day).</param>
    public void Draw(float glow = 0f)
    {
        foreach (var part in parts)
        {
            if (part.Glows) shaders.SetGlow(glow);
            Raylib.DrawMesh(part.Mesh, part.Main, Matrix4x4.Identity);
            if (part.Glows) shaders.SetGlow(0f);
        }
    }

    /// <summary>Adds the warm light that lit windows and doors throw on the ground, scaled by <paramref name="glow"/>.</summary>
    public unsafe void DrawLights(float glow)
    {
        if (glow <= 0.01f || lights.Count == 0) return;
        byte level = (byte)(255 * Math.Clamp(glow, 0f, 1f));
        Rlgl.DrawRenderBatchActive();
        Rlgl.SetBlendMode(BlendMode.Additive);
        Rlgl.DisableDepthMask();
        foreach (var (mesh, material) in lights)
        {
            material.Maps[(int)MaterialMapIndex.Albedo].Color = new Color(level, level, level, level);
            Raylib.DrawMesh(mesh, material, Matrix4x4.Identity);
        }
        Rlgl.DrawRenderBatchActive();
        Rlgl.SetBlendMode(BlendMode.Alpha);
        Rlgl.EnableDepthMask();
    }

    /// <summary>Draws everything that casts shadows, for the shadow-map pass.</summary>
    public void DrawDepth()
    {
        foreach (var part in parts)
        {
            if (part.Depth.HasValue) Raylib.DrawMesh(part.Mesh, part.Depth.Value, Matrix4x4.Identity);
        }
    }
}

/// <summary>Trees and foliage cards shared by the field maps and the battle stage.</summary>
internal static class TreeModels
{
    private static readonly Vector3 Up = Vector3.UnitY;

    private static float Rand(int x, int y, int salt) => GroundBaker.Rand01(x, y, salt);

    /// <summary>Sinnoh pine: four tiers of needled cones with serrated, cut-out rims.</summary>
    public static void Pine(MeshBatches batches, float cx, float cz, int seedX, int seedY, float scale = 1f, bool soft = false)
    {
        if (soft)
        {
            SoftFoliage.Pine(batches, cx, cz, seedX, seedY, scale);
            return;
        }
        float s = (0.92f + Rand(seedX, seedY, 3) * 0.16f) * scale;
        batches.For(SceneTextures.Bark).Cylinder(new(cx, 0, cz), 0.12f * scale, 0.08f * scale, 1.1f * s, 7, Color.White, cap: false);

        var tint = PixelCanvas.Mix(new Color(104, 178, 118, 255), new Color(126, 196, 128, 255), Rand(seedX, seedY, 4));
        var b = batches.For(SceneTextures.Needles);
        (float BaseY, float ApexY, float R)[] tiers =
        {
            (0.5f, 2.0f, 0.7f), (1.25f, 2.7f, 0.6f), (2.0f, 3.4f, 0.48f), (2.75f, 4.1f, 0.34f)
        };
        const int segments = 14;
        float spin = Rand(seedX, seedY, 5) * MathF.Tau;
        for (int k = 0; k < tiers.Length; k++)
        {
            var (baseY, apexY, radius) = tiers[k];
            float h = (apexY - baseY) * s, r = radius * s;
            var apex = new Vector3(cx, apexY * s, cz);
            // Lower tiers are a touch darker; everything sways a little, more toward the top
            var tierColor = MeshBuilder.Scale(tint, 0.86f + k * 0.06f);
            float sway = 0.12f + k * 0.08f;

            for (int i = 0; i < segments; i++)
            {
                float a0 = spin + i * MathF.Tau / segments, a1 = spin + (i + 1) * MathF.Tau / segments;
                var d0 = new Vector3(MathF.Cos(a0), 0, MathF.Sin(a0));
                var d1 = new Vector3(MathF.Cos(a1), 0, MathF.Sin(a1));
                var rim0 = new Vector3(cx, baseY * s, cz) + d0 * r;
                var rim1 = new Vector3(cx, baseY * s, cz) + d1 * r;
                var n0 = Vector3.Normalize(d0 * h + Up * r);
                var n1 = Vector3.Normalize(d1 * h + Up * r);
                var na = Vector3.Normalize(n0 + n1);
                float u0 = i / (float)segments * 3f, u1 = (i + 1) / (float)segments * 3f;
                b.Tri(apex, rim0, rim1, new((u0 + u1) / 2f, 0), new(u0, 1), new(u1, 1), na, n0, n1,
                    MeshBuilder.Sway(PixelCanvas.Light1(tierColor, 0.15f), sway), MeshBuilder.Sway(tierColor, sway * 0.6f), MeshBuilder.Sway(tierColor, sway * 0.6f));
            }
        }
    }

    /// <summary>Round broadleaf tree: a few leafy clumps on a short trunk.</summary>
    public static void Round(MeshBatches batches, float cx, float cz, int seedX, int seedY, float scale = 1f, bool soft = false)
    {
        if (soft)
        {
            SoftFoliage.Round(batches, cx, cz, seedX, seedY, scale);
            return;
        }
        float s = (0.92f + Rand(seedX, seedY, 3) * 0.16f) * scale;
        batches.For(SceneTextures.Bark).Cylinder(new(cx, 0, cz), 0.14f * scale, 0.1f * scale, 0.9f * s, 7, Color.White, cap: false);

        // The crown hangs low over a short trunk, so trees cut off by the top of the screen still read as foliage
        var tint = PixelCanvas.Mix(new Color(96, 176, 104, 255), new Color(118, 192, 108, 255), Rand(seedX, seedY, 4));
        PropModels.LeafBall(tex => batches.For(tex), new Vector3(cx, 1.45f * s, cz), 0.74f * s, 0.86f * s, tint, 0.25f);
        PropModels.LeafBall(tex => batches.For(tex), new Vector3(cx - 0.22f * scale, 2.05f * s, cz + 0.06f * scale), 0.5f * s, 0.58f * s, PixelCanvas.Light1(tint, 0.08f), 0.4f);
        PropModels.LeafBall(tex => batches.For(tex), new Vector3(cx + 0.26f * scale, 1.85f * s, cz - 0.1f * scale), 0.46f * s, 0.52f * s, tint, 0.35f);
    }

    /// <summary>
    /// A boulder with a smaller stone beside it. Heights are stretched by <paramref name="vs"/> like everything
    /// upright in the field; in water it sits lower, as if standing on the bed.
    /// </summary>
    public static void Rock(MeshBatches batches, float cx, float cz, float vs, int seedX, int seedY, bool inWater)
    {
        var b = batches.For(SceneTextures.Rock);
        float s = 0.86f + Rand(seedX, seedY, 6) * 0.28f;
        float sink = inWater ? 0.1f : 0.04f;
        b.Ellipsoid(new Vector3(cx, (0.2f - sink) * s * vs, cz), new Vector3(0.4f * s, 0.3f * s * vs, 0.33f * s), Color.White, 10, 6);

        float side = Rand(seedX, seedY, 7) < 0.5f ? -1f : 1f;
        float r = 0.17f + Rand(seedX, seedY, 8) * 0.06f;
        b.Ellipsoid(new Vector3(cx + side * 0.36f * s, (r * 0.7f - sink) * vs, cz + 0.16f), new Vector3(r, r * 0.8f * vs, r * 0.9f), new Color(232, 232, 232, 255), 8, 5);
    }

    /// <summary>
    /// An upright card facing the camera (which always looks north), swaying at the top: a grass tuft or a flower.
    /// <paramref name="x"/> is its left edge; <paramref name="u0"/> and <paramref name="u1"/> pick a frame of the texture.
    /// </summary>
    public static void Card(MeshBuilder b, float x, float z, float w, float h, float u0, float u1)
    {
        var normal = Vector3.Normalize(new Vector3(0, 0.8f, 0.6f));
        b.Quad(new(x, 0, z), new(x + w, 0, z), new(x + w, h, z), new(x, h, z), new(u0, 1), new(u1, 1), new(u1, 0), new(u0, 0),
            Color.White, MeshBuilder.Sway(Color.White, 1f), normal);
    }
}
