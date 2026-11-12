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
        switch (course.Look)
        {
            case "wood": Wood(batches, course, zStart, zEnd); break;
            case "reef": Reef(batches, course, zStart, zEnd); break;
            case "cavern": Cavern(batches, course, zStart, zEnd); break;
            case "pass": Pass(batches, course, zStart, zEnd); break;
            case "garden": Garden(batches, course, zStart, zEnd); break;
            default: River(batches, zStart, zEnd); break;
        }
    }

    /// <summary>The kind of battle ground whose light a course's look is lit by (plan 44 · SD6).</summary>
    public static ArenaSpec ArenaOf(string look) => look switch
    {
        "wood" => new ArenaSpec(Overworld.BattleArena.Forest),
        "reef" => new ArenaSpec(Overworld.BattleArena.Water),
        "cavern" => new ArenaSpec(Overworld.BattleArena.Cave),
        "pass" => new ArenaSpec(Overworld.BattleArena.Snow),
        "garden" => new ArenaSpec(Overworld.BattleArena.Grass),
        _ => new ArenaSpec(Overworld.BattleArena.Grass, Lakeside: true)
    };

    /// <summary>Whether a spot is clear of the lens's way (x 6 to 11) and of every resident's ground (within 1.6 of where it stands).</summary>
    private static bool Clear(TourCourseData course, float x, float z, float margin = 1.6f)
    {
        if (x > 5.5f && x < 11.5f) return false;
        foreach (var r in course.Residents)
            if (MathF.Abs(r.At[0] - x) < margin + 1f && MathF.Abs(r.At[1] - z) < margin + 4f) return false;
        return true;
    }

    /// <summary>A strip of soft ground the length of the course, tiled every four units.</summary>
    private static void Strip(MeshBatches batches, Texture2D tex, float x0, float x1, float zStart, float zEnd, float y = 0f)
    {
        var ground = batches.For(tex, MeshPass.Ground);
        for (float z = zStart; z > zEnd; z -= 8f)
            for (float x = x0; x < x1; x += 8f)
            {
                float z1 = Math.Max(zEnd, z - 8f), xb = Math.Min(x1, x + 8f);
                ground.Quad(new(x, y, z), new(xb, y, z), new(xb, y, z1), new(x, y, z1),
                    new(x / 4f, z / 4f), new(xb / 4f, z / 4f), new(xb / 4f, z1 / 4f), new(x / 4f, z1 / 4f), Color.White, Vector3.UnitY);
            }
    }

    /// <summary>The Deep Wood: a forest floor under trees closing in on both sides, ferns, logs, mushrooms and bushes for screens.</summary>
    private static void Wood(MeshBatches batches, TourCourseData course, float zStart, float zEnd)
    {
        Strip(batches, SoftTextures.Ground("forest", Rgb(86, 146, 80), Rgb(112, 166, 88), Rgb(66, 118, 70), Rgb(132, 112, 76), 0.35f), -Wide, Wide, zStart, zEnd);
        var white = batches.For(SceneTextures.White);
        // Tall trees on both sides, the near side thinner so the lens sees between them
        int tree = 0;
        for (float z = zStart; z > zEnd; z -= 3.6f)
            foreach (float x0 in new[] { -13f, -19f, -26f, 14f, 21f })
            {
                tree++;
                float x = x0 + Rand(tree, 1, 81) * 3f;
                if (x0 > 0 && Rand(tree, 4, 81) < 0.35f) continue;
                TreeModels.Round(batches, x, z + Rand(tree, 2, 81) * 2f, tree, 11, 1.5f + Rand(tree, 3, 81) * 0.5f, soft: true);
            }
        // Ferns and mushrooms on the floor, a few logs
        for (int i = 0; i < 220; i++)
        {
            float x = -12f + Rand(i, 5, 82) * 24f, z = zEnd + Rand(i, 6, 82) * (zStart - zEnd);
            if (!Clear(course, x, z, 0.4f)) continue;
            SoftFoliage.Tuft(white, x, z, 1.0f, 0.6f, 2000 + i, Rgb(54, 116, 60), Rgb(120, 182, 92));
        }
        for (int i = 0; i < 14; i++)
        {
            float x = -11f + Rand(i, 7, 83) * 4f, z = zEnd + 20f + i * 11f;
            if (Clear(course, x, z, 1f)) BattleArenas.Log(white, new Vector3(x, 0, z), 2.6f + Rand(i, 8, 83) * 1.5f, 0.36f, Rand(i, 9, 83) * 3f);
            float mx = -8f + Rand(i, 10, 83) * 13f, mz = zEnd + 14f + i * 11f;
            if (Clear(course, mx, mz, 0.6f)) BattleArenas.Mushroom(white, new Vector3(mx, 0, mz), 0.32f + Rand(i, 11, 83) * 0.1f);
        }
        // The screens: round bushes
        foreach (var s in course.Screens) BattleArenas.Bush(white, new Vector3(s.At[0], 0, s.At[1]), s.Height * 0.85f, (int)(s.At[1] * 3));
    }

    /// <summary>The Reef Shore: a beach the rail runs along, the sea to the west, rocks, coral and islands on the horizon.</summary>
    private static void Reef(MeshBatches batches, TourCourseData course, float zStart, float zEnd)
    {
        const float shoreX = -1.5f;
        Strip(batches, SoftTextures.Ground("tour_sand", Rgb(232, 218, 176), Rgb(244, 232, 196), Rgb(214, 198, 152), Rgb(206, 190, 150), 0.2f), shoreX - 1f, Wide, zStart, zEnd);
        // The sea, out to the horizon, whitening toward the shore
        var water = batches.For(SoftTextures.Water, MeshPass.SoftWater);
        for (float z = zStart; z > zEnd; z -= 6f)
        {
            float z1 = Math.Max(zEnd, z - 6f);
            for (float x = -90f; x < shoreX; x += 6f)
            {
                float xb = Math.Min(shoreX, x + 6f);
                Color F(float px) => new((int)(255 * Math.Clamp((px - (shoreX - 2.5f)) / 2.5f, 0f, 1f)), 0, 0, 255);
                var p00 = new Vector3(x, 0.03f, z); var p10 = new Vector3(xb, 0.03f, z);
                var p11 = new Vector3(xb, 0.03f, z1); var p01 = new Vector3(x, 0.03f, z1);
                water.Tri(p00, p11, p10, default, default, default, Vector3.UnitY, Vector3.UnitY, Vector3.UnitY, F(x), F(xb), F(xb));
                water.Tri(p00, p01, p11, default, default, default, Vector3.UnitY, Vector3.UnitY, Vector3.UnitY, F(x), F(x), F(xb));
            }
        }
        var white = batches.For(SceneTextures.White);
        // Coral in the shallows: clumps of rounded fingers in warm colours
        Color[] corals = { Rgb(236, 120, 120), Rgb(244, 164, 96), Rgb(200, 110, 190), Rgb(250, 210, 120) };
        for (int i = 0; i < 40; i++)
        {
            float x = -14f + Rand(i, 1, 84) * 11f, z = zEnd + Rand(i, 2, 84) * (zStart - zEnd);
            if (!Clear(course, x, z, 0.8f)) continue;
            var c = corals[i % corals.Length];
            for (int k = 0; k < 4; k++)
                white.Ellipsoid(new Vector3(x + (k - 1.5f) * 0.22f, 0.18f + Rand(i, k, 85) * 0.2f, z + Rand(i, k, 86) * 0.4f), new Vector3(0.12f, 0.32f, 0.12f), PixelCanvas.Light1(c, 0.1f * k), 8, 6);
        }
        // Rocks on the beach for screens, and a few more along the tideline
        foreach (var s in course.Screens) SoftFoliage.Rock(white, s.At[0], s.At[1], s.Height * 0.8f, (int)(s.At[1] * 5), Rgb(108, 104, 110), Rgb(150, 142, 136), Rgb(190, 182, 170));
        for (int i = 0; i < 14; i++)
        {
            float x = 13f + Rand(i, 3, 87) * 14f, z = zEnd + Rand(i, 4, 87) * (zStart - zEnd);
            SoftFoliage.Rock(white, x, z, 0.6f + Rand(i, 5, 87) * 0.9f, 900 + i);
        }
        // Palm-less dunes behind the beach, islands far out on the sea
        var land = batches.For(SceneTextures.White);
        for (int i = 0; i < 14; i++)
            land.Ellipsoid(new Vector3(30f + Rand(i, 6, 87) * 14f, -0.5f, zStart - i * (zStart - zEnd) / 13f), new Vector3(9f, 2.5f + Rand(i, 7, 87) * 2f, 7f), Mix(Rgb(220, 206, 160), Rgb(150, 180, 110), Rand(i, 8, 87)), 16, 6);
        for (int i = 0; i < 6; i++)
            land.Ellipsoid(new Vector3(-60f - Rand(i, 9, 87) * 20f, -1f, zStart - 20f - i * 32f), new Vector3(12f, 4f + Rand(i, 10, 87) * 4f, 6f), Mix(Rgb(100, 150, 110), Rgb(130, 176, 124), Rand(i, 11, 87)), 18, 8);
    }

    /// <summary>The Cavern: a cave's floor between walls of rock, stalagmites, and crystals glowing in the dark.</summary>
    private static void Cavern(MeshBatches batches, TourCourseData course, float zStart, float zEnd)
    {
        Strip(batches, SoftTextures.Ground("cave", Rgb(122, 106, 94), Rgb(146, 128, 112), Rgb(96, 84, 78), Rgb(84, 76, 74), 0.3f), -Wide, Wide, zStart, zEnd);
        var walls = batches.For(SceneTextures.White, MeshPass.Ground);
        var foot = Rgb(62, 56, 58);
        var mid = Rgb(112, 100, 94);
        var lit = Rgb(150, 136, 122);
        int seed = 0;
        for (float z = zStart; z > zEnd; z -= 4.5f)
            foreach (float x0 in new[] { -15f, 17f })
            {
                seed++;
                float x = x0 + (x0 < 0 ? -1f : 1f) * Rand(seed, 1, 88) * 3f;
                BattleArenas.RockMass(walls, new Vector3(x, 0, z), new Vector3(4f, 10f + Rand(seed, 2, 88) * 8f, 3.6f), foot, mid, lit, seed);
                BattleArenas.RockMass(walls, new Vector3(x + (x0 < 0 ? -5f : 5f), 0, z - 2f), new Vector3(5f, 16f + Rand(seed, 3, 88) * 8f, 4f), foot, mid, lit, seed + 500);
            }
        // A roof of rock low over the far end, so the cave closes ahead
        for (int i = 0; i < 9; i++)
            BattleArenas.RockMass(walls, new Vector3(-24f + i * 7f, 0, zEnd - 10f - Rand(i, 4, 88) * 6f), new Vector3(6f, 22f, 5f), foot, mid, lit, 900 + i);
        var rock = batches.For(SceneTextures.White);
        // Stalagmites beyond the residents or behind the rail, never in the lane between: the judge counts only the
        // course's own screens as cover, so nothing drawn may hide a resident it doesn't know of
        for (int i = 0; i < 30; i++)
        {
            float u = Rand(i, 5, 89);
            float x = i % 3 == 0 ? 12f + u * 2.5f : -14f + u * 6f, z = zEnd + Rand(i, 6, 89) * (zStart - zEnd);
            if (Clear(course, x, z, 1f)) BattleArenas.Stalagmite(rock, new Vector3(x, 0, z), 0.4f + Rand(i, 7, 89) * 0.5f, 1.4f + Rand(i, 8, 89) * 2.4f);
        }
        foreach (var s in course.Screens) SoftFoliage.Rock(rock, s.At[0], s.At[1], s.Height * 0.75f, (int)(s.At[1] * 7), foot, mid, lit);
        for (int i = 0; i < 12; i++)
        {
            float x = (i % 2 == 0 ? -12.5f : 13f) + Rand(i, 9, 89) * 1.5f, z = zStart - 8f - i * (zStart - zEnd - 16f) / 11f;
            BattleArenas.Crystals(batches, new Vector3(x, 0, z), 0.9f + Rand(i, 10, 89) * 0.6f, i % 3 == 0 ? Rgb(176, 130, 255) : Rgb(110, 190, 255), i);
        }
    }

    /// <summary>The High Pass: a stony mountain road, snow in its hollows, snowy pines and peaks all round.</summary>
    private static void Pass(MeshBatches batches, TourCourseData course, float zStart, float zEnd)
    {
        Strip(batches, SoftTextures.Ground("tour_pass", Rgb(150, 142, 128), Rgb(172, 164, 148), Rgb(124, 116, 106), Rgb(226, 232, 242), 0.3f), -Wide, Wide, zStart, zEnd);
        var white = batches.For(SceneTextures.White);
        // Drifts of snow and boulders between the residents
        for (int i = 0; i < 24; i++)
        {
            float x = -14f + Rand(i, 1, 90) * 28f, z = zEnd + Rand(i, 2, 90) * (zStart - zEnd);
            if (!Clear(course, x, z, 1.2f)) continue;
            float s = 0.8f + Rand(i, 3, 90) * 1.4f;
            white.Ellipsoid(new Vector3(x, 0, z), new Vector3(s * 1.6f, s * 0.4f, s), ring => Mix(Rgb(214, 224, 240), Rgb(252, 253, 255), ring / 8f), 14, 8);
        }
        foreach (var s in course.Screens)
        {
            float size = s.Height * 0.8f;
            SoftFoliage.Rock(white, s.At[0], s.At[1], size, (int)(s.At[1] * 7), Rgb(96, 102, 122), Rgb(140, 146, 160), Rgb(184, 190, 204));
            white.Ellipsoid(new Vector3(s.At[0], size * 0.78f, s.At[1]), new Vector3(0.55f * size, 0.16f * size, 0.5f * size), Rgb(248, 250, 255), 12, 6);
        }
        // Snowy pines up the slopes, rising to either side
        int seed = 0;
        for (float z = zStart; z > zEnd; z -= 4.2f)
            foreach (float x0 in new[] { -14f, -21f, 15f, 22f })
            {
                seed++;
                if (Rand(seed, 4, 90) < 0.3f) continue;
                BattleArenas.SnowPine(white, x0 + Rand(seed, 5, 90) * 3f, z + Rand(seed, 6, 90) * 2f, seed, 1.0f + Rand(seed, 7, 90) * 0.5f);
            }
        // Peaks white above the tree line, ahead and to both sides
        for (int i = 0; i < 16; i++)
        {
            bool side = i < 12;
            float x = side ? (i % 2 == 0 ? -1f : 1f) * (48f + Rand(i, 8, 90) * 20f) : -60f + (i - 12) * 40f;
            float z = side ? zStart - (i / 2) * (zStart - zEnd) / 5f : zEnd - 50f - Rand(i, 9, 90) * 20f;
            float h = 22f + Rand(i, 10, 90) * 20f, r = 18f + Rand(i, 11, 90) * 12f;
            white.Lathe(new Vector3(x, -2f, z), new[] { new Vector2(r, 0f), new Vector2(r * 0.62f, h * 0.45f), new Vector2(r * 0.3f, h * 0.8f), new Vector2(0f, h) }, 16,
                k => k < 1 ? Rgb(120, 136, 164) : k < 2 ? Rgb(150, 166, 194) : Rgb(236, 242, 250));
        }
    }

    /// <summary>The Night Garden: lawns between pale paths, hedges for screens, flowerbeds and lamps glowing along the way.</summary>
    private static void Garden(MeshBatches batches, TourCourseData course, float zStart, float zEnd)
    {
        Strip(batches, SceneTextures.Meadow, -Wide, Wide, zStart, zEnd);
        // A pale gravel path along the rail and one across the lawns now and then
        var path = batches.For(SceneTextures.White, MeshPass.Ground);
        var gravel = Rgb(206, 198, 180);
        for (float z = zStart; z > zEnd; z -= 8f)
        {
            float z1 = Math.Max(zEnd, z - 8f);
            path.Quad(new(6.5f, 0.012f, z), new(10.5f, 0.012f, z), new(10.5f, 0.012f, z1), new(6.5f, 0.012f, z1), default, default, default, default, gravel, Vector3.UnitY);
        }
        for (float z = zStart - 20f; z > zEnd; z -= 34f)
            path.Quad(new(-12f, 0.012f, z), new(6.5f, 0.012f, z), new(6.5f, 0.012f, z - 2f), new(-12f, 0.012f, z - 2f), default, default, default, default, gravel, Vector3.UnitY);
        var white = batches.For(SceneTextures.White);
        // Hedges: long low walls of leaves on both sides, and clipped ones for the screens
        var hedgeDark = Rgb(40, 96, 58);
        var hedgeLight = Rgb(92, 150, 84);
        for (float z = zStart; z > zEnd; z -= 6f)
            foreach (float x in new[] { -13f, 13.5f })
                white.Ellipsoid(new Vector3(x, 0.7f, z - 3f), new Vector3(0.9f, 0.9f, 3.2f), ring => Mix(hedgeDark, hedgeLight, ring / 8f), 14, 8);
        foreach (var s in course.Screens)
            white.Ellipsoid(new Vector3(s.At[0], s.Height / 2f, s.At[1]), new Vector3(s.Width / 2f, s.Height / 2f, 0.7f), ring => Mix(hedgeDark, hedgeLight, ring / 8f), 14, 8);
        // Flowerbeds of small blooms
        Color[] blooms = { Rgb(240, 120, 150), Rgb(250, 220, 120), Rgb(170, 140, 240), Rgb(255, 255, 255) };
        for (int i = 0; i < 260; i++)
        {
            float x = -12f + Rand(i, 1, 91) * 24f, z = zEnd + Rand(i, 2, 91) * (zStart - zEnd);
            if (!Clear(course, x, z, 0.6f)) continue;
            white.Ellipsoid(new Vector3(x, 0.12f, z), new Vector3(0.1f, 0.08f, 0.1f), blooms[i % blooms.Length], 6, 4);
        }
        // Lamps along the path: a post, a lantern and a pool of light round it
        for (float z = zStart - 6f; z > zEnd; z -= 14f)
        {
            float x = 11.5f;
            white.Cylinder(new Vector3(x, 0, z), 0.08f, 0.07f, 2.4f, 8, Rgb(52, 56, 66));
            white.Ellipsoid(new Vector3(x, 2.55f, z), new Vector3(0.2f, 0.26f, 0.2f), Rgb(255, 226, 160), 10, 8);
            BattleArenas.Halo(batches, SceneTextures.LampGlow, new Vector3(x, 2.55f, z + 0.3f), 1.4f, new Color(255, 210, 140, 200));
            BattleArenas.Halo(batches, SceneTextures.LightPool, new Vector3(x, 0.02f, z), 3f, new Color(255, 200, 130, 150), flat: true);
        }
        // Round trees beyond the hedges
        int tree = 0;
        foreach (float side in new[] { -1f, 1f })
            for (float z = zStart; z > zEnd; z -= 7f)
            {
                tree++;
                TreeModels.Round(batches, side * (19f + Rand(tree, 1, 92) * 5f), z + Rand(tree, 2, 92) * 3f, tree, 7, 1.3f + Rand(tree, 3, 92) * 0.4f, soft: true);
            }
    }

    private static Color Mix(Color a, Color b, float t) => PixelCanvas.Mix(a, b, Math.Clamp(t, 0f, 1f));

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
