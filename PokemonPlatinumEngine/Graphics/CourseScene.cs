using System;
using System.Numerics;
using PokemonPlatinumEngine.Models;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// A photo tour's course as scenery (plan 44 · SD5; style guide, "The photo tour"): built without the GPU in the
/// battle's smooth style from the arena kit's pieces (the meadow's ground, the soft water, soft trees, rocks, tufts and
/// hills), along the course's rail. Nothing of it moves but the water and the swaying grass.
/// </summary>
internal static class CourseScene
{
    /// <summary>How far the ground runs from the rail's ends and to each side of the river.</summary>
    public const float Wide = 60f, Before = 30f, After = 50f;

    /// <summary>The Reed River's water: x from −4 to 4 the whole length.</summary>
    public const float RiverHalf = 4f;

    private static float Rand(int x, int y, int salt) => GroundBaker.Rand01(x, y, salt);
    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    public static void Build(MeshBatches batches, TourCourseData course)
    {
        float zStart = Before, zEnd = -After;
        foreach (var p in course.Rail)
        {
            zStart = Math.Max(zStart, p[2] + Before);
            zEnd = Math.Min(zEnd, p[2] - After);
        }
        River(batches, zStart, zEnd);
    }

    /// <summary>The Reed River: meadow on both banks, a slow river down the middle, reeds at its edges and round trees beyond.</summary>
    private static void River(MeshBatches batches, float zStart, float zEnd)
    {
        // The meadow, in strips of four along and across
        var ground = batches.For(SceneTextures.Meadow, MeshPass.Ground);
        for (float z = zStart; z > zEnd; z -= 8f)
            for (float x = -Wide; x < Wide; x += 8f)
            {
                float z1 = Math.Max(zEnd, z - 8f), x1 = Math.Min(Wide, x + 8f);
                // (the riverbed under the water is the meadow too: the water lies over it)
                ground.Quad(new(x, 0, z), new(x1, 0, z), new(x1, 0, z1), new(x, 0, z1),
                    new(x / 4f, z / 4f), new(x1 / 4f, z / 4f), new(x1 / 4f, z1 / 4f), new(x / 4f, z1 / 4f), Color.White, Vector3.UnitY);
            }

        // A pale band of shore on each side
        var shore = batches.For(SceneTextures.White, MeshPass.Ground);
        var sand = Rgb(214, 204, 170);
        foreach (float side in new[] { -1f, 1f })
            for (float z = zStart; z > zEnd; z -= 8f)
            {
                float z1 = Math.Max(zEnd, z - 8f);
                float inner = side * (RiverHalf - 0.2f), outer = side * (RiverHalf + 0.9f);
                shore.Quad(new(inner, 0.012f, z), new(outer, 0.012f, z), new(outer, 0.012f, z1), new(inner, 0.012f, z1),
                    default, default, default, default, sand, Vector3.UnitY);
            }

        // The water, its red the share toward the shore (the soft water's foam)
        var water = batches.For(SoftTextures.Water, MeshPass.SoftWater);
        Color Foam(float t) => new((int)(255 * Math.Clamp((t - 0.75f) / 0.25f, 0f, 1f)), 0, 0, 255);
        const int bands = 6;
        for (float z = zStart; z > zEnd; z -= 4f)
        {
            float z1 = Math.Max(zEnd, z - 4f);
            for (int i = 0; i < bands; i++)
            {
                float x0 = -RiverHalf + 2f * RiverHalf * i / bands, x1 = -RiverHalf + 2f * RiverHalf * (i + 1) / bands;
                Color c0 = Foam(MathF.Abs(x0) / RiverHalf), c1 = Foam(MathF.Abs(x1) / RiverHalf);
                var p00 = new Vector3(x0, 0.03f, z); var p10 = new Vector3(x1, 0.03f, z);
                var p11 = new Vector3(x1, 0.03f, z1); var p01 = new Vector3(x0, 0.03f, z1);
                water.Tri(p00, p11, p10, default, default, default, Vector3.UnitY, Vector3.UnitY, Vector3.UnitY, c0, c1, c1);
                water.Tri(p00, p01, p11, default, default, default, Vector3.UnitY, Vector3.UnitY, Vector3.UnitY, c0, c0, c1);
            }
        }

        // Reeds along both edges, tall tufts of a darker green, in clumps
        var reeds = batches.For(SceneTextures.White);
        int seed = 0;
        foreach (float side in new[] { -1f, 1f })
            for (float z = zStart - 2f; z > zEnd; z -= 2.2f)
            {
                seed++;
                if (Rand(seed, 1, 71) < 0.35f) continue;
                float x = side * (RiverHalf + 0.1f + Rand(seed, 2, 71) * 0.6f);
                SoftFoliage.Tuft(reeds, x, z + Rand(seed, 3, 71) * 1.4f, 0.55f, 1.1f + Rand(seed, 4, 71) * 0.5f, 900 + seed,
                    Rgb(66, 120, 70), Rgb(170, 190, 104));
            }

        // Short grass about the meadow
        for (int i = 0; i < 260; i++)
        {
            float x = -Wide * 0.6f + Rand(i, 5, 72) * Wide * 1.2f;
            float z = zEnd + Rand(i, 6, 72) * (zStart - zEnd);
            if (MathF.Abs(x) < RiverHalf + 1.2f || MathF.Abs(x - 8.5f) < 1.4f) continue;
            SoftFoliage.Tuft(reeds, x, z, 0.7f, 0.42f, 1200 + i);
        }

        // Boulders here and there
        var rocks = batches.For(SceneTextures.White);
        for (int i = 0; i < 18; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            float x = side * (12f + Rand(i, 7, 73) * 10f);
            float z = zEnd + 10f + Rand(i, 8, 73) * (zStart - zEnd - 20f);
            SoftFoliage.Rock(rocks, x, z, 0.6f + Rand(i, 9, 73) * 0.8f, 400 + i);
        }

        // Round trees beyond the banks, two rows a side
        int tree = 0;
        foreach (float side in new[] { -1f, 1f })
            for (int row = 0; row < 2; row++)
                for (float z = zStart; z > zEnd; z -= 5.5f + row)
                {
                    tree++;
                    float x = side * (20f + row * 7f + Rand(tree, 1, 74) * 4f);
                    TreeModels.Round(batches, x, z + Rand(tree, 2, 74) * 3f, tree, row, 1.2f + Rand(tree, 3, 74) * 0.5f, soft: true);
                }

        // Hills on the horizon ahead and to the sides
        var hills = batches.For(SceneTextures.White);
        var a = Rgb(112, 170, 140);
        var b = Rgb(138, 188, 156);
        for (int i = 0; i < 9; i++)
        {
            float x = -90f + i * 22f + Rand(i, 0, 75) * 8f;
            float h = 6f + Rand(i, 1, 75) * 6f;
            hills.Ellipsoid(new Vector3(x, -1f, zEnd - 40f - Rand(i, 3, 75) * 15f), new Vector3(22f, h, 10f), PixelCanvas.Mix(a, b, Rand(i, 2, 75)), 20, 8);
        }
        for (int i = 0; i < 12; i++)
            foreach (float side in new[] { -1f, 1f })
            {
                float z = zStart - i * (zStart - zEnd) / 11f;
                hills.Ellipsoid(new Vector3(side * (Wide + 6f), -1f, z), new Vector3(10f, 5f + Rand(i, 4, 75) * 5f, 14f), PixelCanvas.Mix(a, b, Rand(i, 5, 75)), 16, 6);
            }
    }
}
