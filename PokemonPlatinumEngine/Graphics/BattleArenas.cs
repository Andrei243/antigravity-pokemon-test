using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>Everything that decides how a battle's stage is built and lit (plan 04 · G8).</summary>
internal readonly record struct ArenaSpec(BattleArena Kind, PokemonType? Theme = null, TreeStyle Trees = TreeStyle.Round, bool Lakeside = false)
{
    /// <summary>The stage for a battle that starts with the player on (<paramref name="x"/>, <paramref name="y"/>) of <paramref name="map"/>.</summary>
    public static ArenaSpec For(Map map, int x, int y)
    {
        var kind = map.ArenaAt(x, y);
        return new ArenaSpec(kind, kind is BattleArena.Gym or BattleArena.League ? map.ArenaType : null, map.TreesAt(x, y),
            kind == BattleArena.Grass && map.HasLakeNear(x, y));
    }

    /// <summary>Under the open sky, lit by the time of day.</summary>
    public bool Outdoors => Kind is BattleArena.Grass or BattleArena.Forest or BattleArena.Water or BattleArena.Snow or BattleArena.Sand;

    /// <summary>A gym or a room of the League.</summary>
    public bool Hall => Kind is BattleArena.Gym or BattleArena.League;
}

/// <summary>
/// Builds each arena's scenery (plan 04 · G8): the ground, the two platforms, and what surrounds them — the
/// local trees and hills, a forest closing in, cave walls with crystals, the open sea, snowy pines under
/// mountains, dunes and mesas, a room, or a gym or League hall themed on its type. All of it is static geometry
/// in the smooth battle style: vertex gradients and soft textures, GPU-free until it is uploaded.
/// </summary>
internal static class BattleArenas
{
    private static readonly Vector3 Enemy = BattleStage.EnemySpot, Player = BattleStage.PlayerSpot;
    private const float EnemyRadius = BattleStage.EnemyPlatformRadius, PlayerRadius = BattleStage.PlayerPlatformRadius;
    private const float Top = BattleStage.PlatformHeight;

    private static float Rand(int x, int y, int salt) => GroundBaker.Rand01(x, y, salt);
    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);
    private static Color Mix(Color a, Color b, float t) => PixelCanvas.Mix(a, b, Math.Clamp(t, 0f, 1f));

    public static void Build(MeshBatches b, ArenaSpec spec)
    {
        switch (spec.Kind)
        {
            case BattleArena.Forest: Forest(b, spec); break;
            case BattleArena.Cave: Cave(b); break;
            case BattleArena.Water: Sea(b); break;
            case BattleArena.Snow: Snow(b); break;
            case BattleArena.Sand: Desert(b); break;
            case BattleArena.Indoors: Room(b); break;
            case BattleArena.Gym: Hall(b, spec.Theme, league: false); break;
            case BattleArena.League: Hall(b, spec.Theme, league: true); break;
            default: Meadow(b, spec); break;
        }
    }

    /// <summary>Whether a point is clear of both platforms by <paramref name="margin"/>.</summary>
    private static bool Clear(float x, float z, float margin) =>
        Vector2.Distance(new(x, z), new(Enemy.X, Enemy.Z)) > EnemyRadius + margin &&
        Vector2.Distance(new(x, z), new(Player.X, Player.Z)) > PlayerRadius + margin;

    // ------------------------------------------------------------------ shared pieces

    /// <summary>One big textured disc of ground, tiled every four units.</summary>
    private static void GroundDisc(MeshBatches batches, Texture2D tex, float radius = 90f, MeshPass pass = MeshPass.Ground)
    {
        var ground = batches.For(tex, pass);
        const int rings = 12, segments = 48;
        for (int r = 0; r < rings; r++)
        {
            float r0 = radius * r / rings, r1 = radius * (r + 1) / rings;
            for (int s = 0; s < segments; s++)
            {
                float a0 = s * MathF.Tau / segments, a1 = (s + 1) * MathF.Tau / segments;
                Vector3 P(float rr, float a) => new(MathF.Cos(a) * rr, 0, MathF.Sin(a) * rr - 4f);
                Vector2 T(Vector3 p) => new(p.X / 4f, p.Z / 4f);
                var p00 = P(r0, a0); var p01 = P(r0, a1); var p10 = P(r1, a0); var p11 = P(r1, a1);
                ground.Tri(p00, p11, p10, T(p00), T(p11), T(p10), Color.White, Vector3.UnitY);
                ground.Tri(p00, p01, p11, T(p00), T(p01), T(p11), Color.White, Vector3.UnitY);
            }
        }
    }

    /// <summary>A flat textured floor over a rectangle (rooms and halls).</summary>
    private static void Floor(MeshBatches batches, Texture2D tex, float x0, float z0, float x1, float z1, float tile = 4f)
    {
        var floor = batches.For(tex, MeshPass.Ground);
        const int n = 8;
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                float ax = x0 + (x1 - x0) * i / n, bx = x0 + (x1 - x0) * (i + 1) / n;
                float az = z0 + (z1 - z0) * j / n, bz = z0 + (z1 - z0) * (j + 1) / n;
                floor.Quad(new(ax, 0, bz), new(bx, 0, bz), new(bx, 0, az), new(ax, 0, az),
                    new(ax / tile, bz / tile), new(bx / tile, bz / tile), new(bx / tile, az / tile), new(ax / tile, az / tile), Color.White, Vector3.UnitY);
            }
    }

    /// <summary>A raised platform: a rim of <paramref name="rim"/> under a textured top, like the DS battle platforms.</summary>
    private static void Platform(MeshBatches batches, Vector3 center, float radius, Texture2D top, Color rim, float height = Top, float baseY = -0.05f)
    {
        batches.For(SceneTextures.White).Lathe(center, new[]
        {
            new Vector2(radius * 1.02f, baseY), new Vector2(radius * 1.02f, height * 0.55f),
            new Vector2(radius, height), new Vector2(radius * 0.97f, height)
        }, 48, i => i <= 1 ? MeshBuilder.Scale(rim, 0.8f) : rim);

        var t = batches.For(top, MeshPass.Ground);
        const int segments = 48;
        for (int s = 0; s < segments; s++)
        {
            float a0 = s * MathF.Tau / segments, a1 = (s + 1) * MathF.Tau / segments;
            var c = center + new Vector3(0, height, 0);
            var p0 = center + new Vector3(MathF.Cos(a0) * radius * 0.97f, height, MathF.Sin(a0) * radius * 0.97f);
            var p1 = center + new Vector3(MathF.Cos(a1) * radius * 0.97f, height, MathF.Sin(a1) * radius * 0.97f);
            Vector2 T(float a, float rr) => new(0.5f + MathF.Cos(a) * rr * 0.5f, 0.5f + MathF.Sin(a) * rr * 0.5f);
            t.Tri(c, p1, p0, new(0.5f, 0.5f), T(a1, 1f), T(a0, 1f), Color.White, Vector3.UnitY);
        }
    }

    private static void Platforms(MeshBatches batches, Texture2D top, Color rim, float height = Top, float baseY = -0.05f)
    {
        Platform(batches, Enemy, EnemyRadius, top, rim, height, baseY);
        Platform(batches, Player, PlayerRadius, top, rim, height, baseY);
    }

    /// <summary>
    /// Tall scenery round the field (walls, rock faces, columns): shaded and shadowed like everything else, but it
    /// casts no shadow, so it can never throw the platforms into shade.
    /// </summary>
    private static MeshBuilder Backdrop(MeshBatches batches) => batches.For(SceneTextures.White, MeshPass.Ground);

    /// <summary>A flat quad of added light (halos, glowing rings), drawn by the stage's light pass.</summary>
    private static void Halo(MeshBatches batches, Texture2D tex, Vector3 center, float size, Color color, bool flat = false)
    {
        var b = batches.For(tex, MeshPass.Light);
        if (flat)
            b.Quad(center + new Vector3(-size, 0, size), center + new Vector3(size, 0, size), center + new Vector3(size, 0, -size), center + new Vector3(-size, 0, -size),
                new(0, 1), new(1, 1), new(1, 0), new(0, 0), color, Vector3.UnitY);
        else
            b.Quad(center + new Vector3(-size, -size, 0), center + new Vector3(size, -size, 0), center + new Vector3(size, size, 0), center + new Vector3(-size, size, 0),
                new(0, 1), new(1, 1), new(1, 0), new(0, 0), color, Vector3.UnitZ);
    }

    /// <summary>A ring of trees in the given style: rows behind the field and a few framing the left.</summary>
    private static void TreeRows(MeshBatches batches, TreeStyle style, int rows, float firstZ, float rowGap, float scale, Func<float, float, bool> skip, int seedBase = 0)
    {
        int seed = seedBase;
        for (int row = 0; row < rows; row++)
        {
            float z = firstZ - row * rowGap;
            for (float x = -38f - row * 4f; x <= 26f + row * 4f; x += 2.6f - Math.Min(row, 3) * 0.2f)
            {
                seed++;
                float jx = (Rand(seed, row, 1) - 0.5f) * 1.6f, jz = (Rand(seed, row, 2) - 0.5f) * 2.4f;
                float s = scale * (1.0f + row * 0.2f + Rand(seed, row, 3) * 0.3f);
                if (skip(x + jx, z + jz)) continue;
                if (style == TreeStyle.Pine) TreeModels.Pine(batches, x + jx, z + jz, seed, row, s * 0.8f, soft: true);
                else TreeModels.Round(batches, x + jx, z + jz, seed, row, s, soft: true);
            }
        }
    }

    // ------------------------------------------------------------------ grass (routes and towns)

    // The lake of a lakeside arena: an oval behind and to the right of the opponent, short of the forest
    private static readonly Vector2 LakeCenter = new(13f, -8.6f);
    private static readonly Vector2 LakeRadii = new(17f, 4.6f);

    /// <summary>How far inside the lake a point is: under 1 is water, 1 is the shoreline.</summary>
    private static float LakeDistance(float x, float z)
    {
        float u = (x - LakeCenter.X) / LakeRadii.X, v = (z - LakeCenter.Y) / LakeRadii.Y;
        return MathF.Sqrt(u * u + v * v);
    }

    /// <summary>The meadow of routes and towns: the map's trees behind, hills on the horizon, a lake if the map has one.</summary>
    private static void Meadow(MeshBatches batches, ArenaSpec spec)
    {
        bool lakeside = spec.Lakeside;
        bool InLake(float x, float z, float margin) => lakeside && LakeDistance(x, z) < 1f + margin;

        GroundDisc(batches, SceneTextures.Meadow);
        Platforms(batches, SceneTextures.PlatformTop, Rgb(170, 134, 92));
        if (lakeside) AddLake(batches);

        // Forest behind the opponent, far enough back to leave a band of sky above it
        TreeRows(batches, spec.Trees, 3, -15f, 6f, 1f, (x, z) => InLake(x, z, 0.25f));
        // A few trees framing the left side of the field
        for (int i = 0; i < 6; i++)
        {
            float x = -17f - i * 1.8f, z = 4f - i * 3.2f;
            if (spec.Trees == TreeStyle.Pine) TreeModels.Pine(batches, x, z, 100 + i, 7, 1.3f, soft: true);
            else TreeModels.Round(batches, x, z, 100 + i, 7, 1.3f, soft: true);
        }

        Hills(batches, Rgb(112, 170, 140), Rgb(138, 188, 156));

        // Boulders at the edges of the meadow, clear of the platforms; by a lake two of them stand on its shore
        var rocks = batches.For(SceneTextures.White);
        (float X, float Z, float Size)[] boulders =
        {
            (-13.5f, -3.5f, 0.9f), (-21f, 6f, 1.3f), (-9.5f, -11.5f, 1.0f), (5.8f, 9.5f, 0.55f), (9.5f, 6.2f, 0.8f),
            (lakeside ? -4.6f : 9.8f, lakeside ? -7.6f : -6.2f, 1.1f), (lakeside ? 5.4f : 12f, lakeside ? -3.4f : -4.8f, 0.6f),
            (15.5f, 2f, 0.7f), (-2.5f, 20.5f, 0.6f)
        };
        for (int i = 0; i < boulders.Length; i++)
        {
            var (x, z, size) = boulders[i];
            if (!InLake(x, z, -0.02f)) SoftFoliage.Rock(rocks, x, z, size, 300 + i);
        }

        // Tufts of grass scattered over the meadow and around the platforms
        for (int i = 0; i < 140; i++)
        {
            float x = -26f + Rand(i, 5, 11) * 44f;
            float z = -9f + Rand(i, 6, 11) * 28f;
            if (!Clear(x, z, 0.2f) || InLake(x, z, 0.12f)) continue;
            SoftFoliage.Tuft(batches.For(SceneTextures.White), x, z, 0.7f, 0.42f, i);
        }
    }

    /// <summary>Rolling hills on the horizon.</summary>
    private static void Hills(MeshBatches batches, Color a, Color b, float height = 1f)
    {
        var hills = batches.For(SceneTextures.White);
        for (int i = 0; i < 11; i++)
        {
            float x = -110f + i * 22f + Rand(i, 0, 9) * 8f;
            float h = (5f + Rand(i, 1, 9) * 5f) * height;
            hills.Ellipsoid(new Vector3(x, -1f, -95f - Rand(i, 3, 9) * 12f), new Vector3(20f, h, 9f), Mix(a, b, Rand(i, 2, 9)), 20, 8);
        }
    }

    /// <summary>
    /// A lake in the smooth style of the battles: a pale shore ringing calm water, with foam where they meet
    /// (the vertex red of the water carries how close each vertex is to the shore).
    /// </summary>
    private static void AddLake(MeshBatches batches)
    {
        const int segments = 56, rings = 6;
        Vector3 P(float t, float a, float y) => new(LakeCenter.X + MathF.Cos(a) * LakeRadii.X * t, y, LakeCenter.Y + MathF.Sin(a) * LakeRadii.Y * t);

        var shore = batches.For(SceneTextures.White, MeshPass.Ground);
        var sand = Rgb(214, 204, 170);
        var meadow = Rgb(120, 190, 100);
        for (int s = 0; s < segments; s++)
        {
            float a0 = s * MathF.Tau / segments, a1 = (s + 1) * MathF.Tau / segments;
            shore.Tri(P(0.96f, a0, 0.012f), P(1.1f, a1, 0.012f), P(1.1f, a0, 0.012f), default, default, default,
                Vector3.UnitY, Vector3.UnitY, Vector3.UnitY, sand, meadow, meadow);
            shore.Tri(P(0.96f, a0, 0.012f), P(0.96f, a1, 0.012f), P(1.1f, a1, 0.012f), default, default, default,
                Vector3.UnitY, Vector3.UnitY, Vector3.UnitY, sand, sand, meadow);
        }

        var water = batches.For(SoftTextures.Water, MeshPass.SoftWater);
        Color Foam(float t) => new((int)(255 * Math.Clamp((t - 0.8f) / 0.2f, 0f, 1f)), 0, 0, 255);
        for (int r = 0; r < rings; r++)
        {
            float t0 = r / (float)rings, t1 = (r + 1) / (float)rings;
            for (int s = 0; s < segments; s++)
            {
                float a0 = s * MathF.Tau / segments, a1 = (s + 1) * MathF.Tau / segments;
                var p00 = P(t0, a0, 0.02f); var p01 = P(t0, a1, 0.02f); var p10 = P(t1, a0, 0.02f); var p11 = P(t1, a1, 0.02f);
                water.Tri(p00, p11, p10, default, default, default, Vector3.UnitY, Vector3.UnitY, Vector3.UnitY, Foam(t0), Foam(t1), Foam(t1));
                if (r > 0) water.Tri(p00, p01, p11, default, default, default, Vector3.UnitY, Vector3.UnitY, Vector3.UnitY, Foam(t0), Foam(t0), Foam(t1));
            }
        }
    }

    // ------------------------------------------------------------------ forest

    /// <summary>Deep in a forest: trees close in on every side, with bushes, ferns, fallen logs and mushrooms.</summary>
    private static void Forest(MeshBatches batches, ArenaSpec spec)
    {
        GroundDisc(batches, SoftTextures.Ground("forest", Rgb(86, 146, 80), Rgb(112, 166, 88), Rgb(66, 118, 70), Rgb(132, 112, 76), 0.35f));
        Platforms(batches, SoftTextures.Top("forest", Rgb(124, 186, 100), Rgb(84, 146, 78), Rgb(66, 118, 66)), Rgb(122, 92, 64));

        var style = spec.Trees;
        TreeRows(batches, style, 5, -11f, 5f, 1.25f, (x, z) => false, 500);

        // Trees closing in at the sides, and big ones framing the left
        int seed = 700;
        for (float z = -8f; z <= 22f; z += 3.4f)
            for (int k = 0; k < 2; k++)
            {
                seed++;
                float x = -15.5f - k * 3.2f - Rand(seed, 1, 3) * 1.5f;
                if (style == TreeStyle.Pine) TreeModels.Pine(batches, x, z + Rand(seed, 2, 3), seed, 9, 1.45f, soft: true);
                else TreeModels.Round(batches, x, z + Rand(seed, 2, 3), seed, 9, 1.6f, soft: true);
            }
        for (float z = -8f; z <= 8f; z += 3.6f)
        {
            seed++;
            float x = 13f + Rand(seed, 3, 3) * 2f;
            if (style == TreeStyle.Pine) TreeModels.Pine(batches, x, z, seed, 10, 1.4f, soft: true);
            else TreeModels.Round(batches, x, z, seed, 10, 1.5f, soft: true);
        }

        // Bushes along the edge of the trees
        var white = batches.For(SceneTextures.White);
        for (int i = 0; i < 26; i++)
        {
            float x = -28f + i * 2.1f + Rand(i, 1, 21) * 1.2f;
            float z = -8.5f - Rand(i, 2, 21) * 2f;
            float s = 0.8f + Rand(i, 3, 21) * 0.6f;
            Bush(white, new Vector3(x, 0, z), s, i);
        }
        foreach (var (x, z, s) in new[] { (-12.2f, 6f, 1.1f), (-11.5f, 13f, 0.9f), (7.5f, 4f, 0.9f), (9.5f, -3f, 1.2f), (-10f, -4f, 1f) })
            Bush(white, new Vector3(x, 0, z), s, (int)(x * 10));

        // A fallen log with mushrooms, and ferns in the undergrowth
        Log(white, new Vector3(6.5f, 0, -5.5f), 4.2f, 0.42f, 0.25f);
        Log(white, new Vector3(-11f, 0, 1.5f), 3.2f, 0.36f, -0.6f);
        foreach (var (x, z) in new[] { (5.2f, -4.6f), (5.7f, -4.4f), (8.4f, -5.2f), (-10.2f, 2.6f) })
            Mushroom(white, new Vector3(x, 0, z), 0.32f + Rand((int)(x * 10), (int)(z * 10), 7) * 0.12f);
        for (int i = 0; i < 90; i++)
        {
            float x = -22f + Rand(i, 5, 13) * 36f;
            float z = -8f + Rand(i, 6, 13) * 26f;
            if (!Clear(x, z, 0.3f)) continue;
            SoftFoliage.Tuft(white, x, z, 1.0f, 0.62f, i + 500, Rgb(54, 116, 60), Rgb(120, 182, 92));
        }
    }

    /// <summary>A rounded bush: a few overlapping blobs, dark underneath and light on top.</summary>
    private static void Bush(MeshBuilder b, Vector3 at, float size, int seed)
    {
        var dark = Rgb(46, 104, 60);
        var light = Rgb(110, 172, 92);
        for (int k = 0; k < 3; k++)
        {
            var off = new Vector3((k - 1) * 0.55f * size, 0.45f * size + Rand(seed, k, 31) * 0.15f * size, Rand(seed, k, 32) * 0.3f * size);
            float r = (0.55f + Rand(seed, k, 33) * 0.2f) * size;
            b.Ellipsoid(at + off, new Vector3(r, r * 0.85f, r), ring => MeshBuilder.Sway(Mix(dark, light, ring / 8f), 0.08f), 14, 8);
        }
    }

    /// <summary>A fallen log lying on its side, turned <paramref name="yaw"/> radians, with mossy ends.</summary>
    private static void Log(MeshBuilder b, Vector3 at, float length, float radius, float yaw)
    {
        var log = new MeshBuilder();
        log.Cylinder(new Vector3(0, -length / 2f, 0), radius, radius * 0.92f, length, 14, Rgb(116, 84, 60), cap: true, capColor: Rgb(176, 140, 96));
        log.Ellipsoid(new Vector3(0, -length / 2f, 0), new Vector3(radius * 1.02f, 0.05f, radius * 1.02f), Rgb(176, 140, 96), 14, 4);
        b.Append(log, Matrix4x4.CreateRotationZ(MathF.PI / 2f) * Matrix4x4.CreateRotationY(yaw) * Matrix4x4.CreateTranslation(at + new Vector3(0, radius * 0.9f, 0)));
        b.Ellipsoid(at + new Vector3(0, radius * 1.7f, 0), new Vector3(length * 0.3f, 0.06f, radius * 0.6f), Rgb(96, 150, 70), 12, 4);
    }

    private static void Mushroom(MeshBuilder b, Vector3 at, float size)
    {
        b.Cylinder(at, 0.08f * size, 0.07f * size, 0.5f * size, 8, Rgb(236, 226, 206));
        b.Ellipsoid(at + new Vector3(0, 0.5f * size, 0), new Vector3(0.32f, 0.2f, 0.32f) * size, ring => ring < 5 ? Rgb(230, 220, 200) : Rgb(214, 72, 60), 12, 8);
    }

    // ------------------------------------------------------------------ cave

    /// <summary>A cave: walls of rock all round, stalagmites, and crystals that glow in the dark.</summary>
    private static void Cave(MeshBatches batches)
    {
        GroundDisc(batches, SoftTextures.Ground("cave", Rgb(122, 106, 94), Rgb(146, 128, 112), Rgb(96, 84, 78), Rgb(84, 76, 74), 0.3f));
        Platforms(batches, SoftTextures.Top("cave", Rgb(160, 150, 136), Rgb(124, 114, 104), Rgb(100, 92, 86)), Rgb(98, 88, 82), Top + 0.06f);

        var rock = batches.For(SceneTextures.White);
        var walls = Backdrop(batches);
        var foot = Rgb(62, 56, 58);
        var mid = Rgb(112, 100, 94);
        var lit = Rgb(150, 136, 122);

        // The back wall, curving round behind the field, and the side walls
        int seed = 0;
        for (float x = -72f; x <= 64f; x += 4.2f)
        {
            seed++;
            float z = -24f - 0.004f * x * x - Rand(seed, 1, 41) * 4f;
            float h = 9f + Rand(seed, 2, 41) * 9f;
            float w = 3.6f + Rand(seed, 3, 41) * 2.4f;
            RockMass(walls, new Vector3(x, 0, z), new Vector3(w, h, w * 0.8f), foot, mid, lit, seed);
            RockMass(walls, new Vector3(x + 2f, 0, z - 6f), new Vector3(w * 1.3f, h * 1.5f, w), foot, mid, lit, seed + 400);
        }
        for (float z = -26f; z <= 34f; z += 4f)
        {
            seed++;
            float x = -22f - 0.01f * (z - 4f) * (z - 4f) - Rand(seed, 1, 42) * 2f;
            RockMass(walls, new Vector3(x, 0, z), new Vector3(3.6f, 8f + Rand(seed, 2, 42) * 6f, 3.2f), foot, mid, lit, seed);
            float xr = 17f + 0.012f * z * z + Rand(seed, 3, 42) * 2f;
            if (z < 14f) RockMass(walls, new Vector3(xr, 0, z), new Vector3(3.6f, 8f + Rand(seed, 4, 42) * 6f, 3.2f), foot, mid, lit, seed + 200);
        }

        // Stalagmites and boulders on the floor
        (float X, float Z, float R, float H)[] spikes =
        {
            (-12f, -6f, 0.7f, 2.6f), (-13.5f, -4.4f, 0.45f, 1.6f), (8.5f, -9f, 0.9f, 3.4f), (10.2f, -7.4f, 0.5f, 1.8f),
            (-16f, 8f, 0.8f, 3f), (13f, 2f, 0.7f, 2.4f), (-5f, -15f, 1f, 4f), (3f, -16f, 0.7f, 2.8f), (-11.5f, 15f, 0.5f, 1.7f)
        };
        foreach (var (x, z, r, h) in spikes) Stalagmite(rock, new Vector3(x, 0, z), r, h);
        for (int i = 0; i < 7; i++)
        {
            float x = -18f + Rand(i, 1, 43) * 34f, z = -12f + Rand(i, 2, 43) * 26f;
            if (Clear(x, z, 1.2f) && MathF.Abs(x + 5f) > 3f) SoftFoliage.Rock(rock, x, z, 0.6f + Rand(i, 3, 43) * 0.6f, 600 + i, foot, mid, lit);
        }

        // Crystals glowing blue and violet
        (float X, float Z, float S, bool Violet)[] crystals = { (-10f, -8f, 1.2f, false), (11f, -11f, 1.5f, true), (-14.5f, 4f, 1f, true), (6.5f, -14f, 0.9f, false) };
        for (int i = 0; i < crystals.Length; i++)
        {
            var (x, z, s, violet) = crystals[i];
            Crystals(batches, new Vector3(x, 0, z), s, violet ? Rgb(176, 130, 255) : Rgb(110, 190, 255), i);
        }
    }

    /// <summary>A lumpy mass of rock: a few overlapping blobs, dark at the foot and lit on top.</summary>
    private static void RockMass(MeshBuilder b, Vector3 at, Vector3 size, Color foot, Color mid, Color lit, int seed)
    {
        for (int k = 0; k < 3; k++)
        {
            var off = new Vector3((Rand(seed, k, 51) - 0.5f) * size.X, 0, (Rand(seed, k, 52) - 0.5f) * size.Z * 0.6f);
            var r = new Vector3(size.X * (0.6f + Rand(seed, k, 53) * 0.4f), size.Y * (0.6f + Rand(seed, k, 54) * 0.5f), size.Z * (0.6f + Rand(seed, k, 55) * 0.4f));
            b.Ellipsoid(at + off, r, ring => ring < 5 ? Mix(foot, mid, ring / 5f) : Mix(mid, lit, (ring - 5) / 5f), 12, 10);
        }
    }

    private static void Stalagmite(MeshBuilder b, Vector3 at, float r, float h)
    {
        b.Lathe(at, new[] { new Vector2(r, -0.1f), new Vector2(r * 0.8f, h * 0.3f), new Vector2(r * 0.4f, h * 0.75f), new Vector2(0.02f, h) }, 12,
            i => Mix(Rgb(84, 74, 70), Rgb(150, 136, 124), i / 3f));
    }

    /// <summary>A cluster of six-sided crystals leaning out from a point, with a glow round them.</summary>
    private static void Crystals(MeshBatches batches, Vector3 at, float size, Color color, int seed)
    {
        var b = batches.For(SceneTextures.White);
        for (int k = 0; k < 5; k++)
        {
            float h = size * (0.9f + Rand(seed, k, 61) * 1.1f), r = size * (0.16f + Rand(seed, k, 62) * 0.08f);
            var c = new MeshBuilder();
            c.Lathe(Vector3.Zero, new[] { new Vector2(0f, 0f), new Vector2(r, 0.1f * h), new Vector2(r, 0.75f * h), new Vector2(0f, h) }, 6,
                i => i >= 2 ? PixelCanvas.Light1(color, 0.35f) : MeshBuilder.Scale(color, 0.75f));
            float tilt = (k - 2) * 0.32f + (Rand(seed, k, 63) - 0.5f) * 0.3f;
            b.Append(c, Matrix4x4.CreateRotationZ(tilt) * Matrix4x4.CreateRotationY(Rand(seed, k, 64) * MathF.Tau) * Matrix4x4.CreateTranslation(at));
        }
        Halo(batches, SceneTextures.SoftGlow, at + new Vector3(0, size * 0.9f, 0.6f), Math.Min(size * 2.4f, 2.6f), new Color(color.R, color.G, color.B, (byte)150));
    }

    // ------------------------------------------------------------------ water

    /// <summary>Out on the water: the platforms are rocky islets ringed with foam, islands on the horizon.</summary>
    private static void Sea(MeshBatches batches)
    {
        var water = batches.For(SoftTextures.Water, MeshPass.SoftWater);
        const int rings = 12, segments = 48;
        const float radius = 120f;
        var calm = new Color(0, 0, 0, 255);
        for (int r = 0; r < rings; r++)
        {
            float r0 = radius * r / rings, r1 = radius * (r + 1) / rings;
            for (int s = 0; s < segments; s++)
            {
                float a0 = s * MathF.Tau / segments, a1 = (s + 1) * MathF.Tau / segments;
                Vector3 P(float rr, float a) => new(MathF.Cos(a) * rr, 0f, MathF.Sin(a) * rr - 4f);
                water.Tri(P(r0, a0), P(r1, a1), P(r1, a0), default, default, default, Vector3.UnitY, Vector3.UnitY, Vector3.UnitY, calm, calm, calm);
                water.Tri(P(r0, a0), P(r0, a1), P(r1, a1), default, default, default, Vector3.UnitY, Vector3.UnitY, Vector3.UnitY, calm, calm, calm);
            }
        }

        // The islets: rock rising out of the water under a sandy top, with foam round them
        var top = SoftTextures.Top("islet", Rgb(232, 220, 178), Rgb(206, 190, 146), Rgb(176, 160, 124));
        foreach (var (center, rad) in new[] { (Enemy, EnemyRadius), (Player, PlayerRadius) })
        {
            batches.For(SceneTextures.White).Lathe(center, new[]
            {
                new Vector2(rad * 1.18f, -0.6f), new Vector2(rad * 1.1f, 0.02f), new Vector2(rad * 1.02f, Top * 0.6f), new Vector2(rad * 0.97f, Top)
            }, 48, i => i <= 1 ? Rgb(112, 108, 112) : Rgb(150, 142, 132));
            Platform(batches, center, rad, top, Rgb(150, 142, 132));
            Foam(water, center, rad * 1.1f, rad * 1.55f);
        }

        // Islands on the horizon and a couple of sea stacks
        var land = batches.For(SceneTextures.White);
        for (int i = 0; i < 6; i++)
        {
            float x = -90f + i * 34f + Rand(i, 1, 71) * 10f;
            float z = -85f - Rand(i, 2, 71) * 20f;
            float w = 10f + Rand(i, 3, 71) * 12f, h = 3f + Rand(i, 4, 71) * 6f;
            land.Ellipsoid(new Vector3(x, -1f, z), new Vector3(w, h, w * 0.5f), ring => ring < 5 ? Rgb(122, 128, 136) : Mix(Rgb(100, 150, 110), Rgb(130, 176, 124), (ring - 5) / 5f), 18, 10);
        }
        foreach (var (x, z, h) in new[] { (19f, -24f, 7f), (-27f, -34f, 9f), (24f, -40f, 5f) })
        {
            land.Lathe(new Vector3(x, -0.5f, z), new[] { new Vector2(2.6f, 0f), new Vector2(2.2f, h * 0.5f), new Vector2(1.8f, h), new Vector2(0f, h + 0.4f) }, 12,
                i => i < 2 ? Rgb(120, 116, 120) : Rgb(160, 150, 140));
            land.Ellipsoid(new Vector3(x, h, z), new Vector3(1.9f, 0.6f, 1.9f), Rgb(110, 160, 104), 12, 6);
            Foam(water, new Vector3(x, 0, z), 2.6f, 3.6f);
        }
    }

    /// <summary>A ring of foam on the water round something standing in it (the water shader whitens by vertex red).</summary>
    private static void Foam(MeshBuilder water, Vector3 center, float inner, float outer)
    {
        const int segments = 40;
        var white = new Color(255, 0, 0, 255);
        var clear = new Color(0, 0, 0, 255);
        for (int s = 0; s < segments; s++)
        {
            float a0 = s * MathF.Tau / segments, a1 = (s + 1) * MathF.Tau / segments;
            Vector3 P(float r, float a) => new(center.X + MathF.Cos(a) * r, 0.012f, center.Z + MathF.Sin(a) * r);
            water.Tri(P(inner, a0), P(outer, a1), P(outer, a0), default, default, default, Vector3.UnitY, Vector3.UnitY, Vector3.UnitY, white, clear, clear);
            water.Tri(P(inner, a0), P(inner, a1), P(outer, a1), default, default, default, Vector3.UnitY, Vector3.UnitY, Vector3.UnitY, white, white, clear);
        }
    }

    // ------------------------------------------------------------------ snow

    /// <summary>A snowfield: snow-laden pines, drifts and capped rocks under white mountains.</summary>
    private static void Snow(MeshBatches batches)
    {
        GroundDisc(batches, SoftTextures.Ground("snow", Rgb(234, 240, 248), Rgb(250, 252, 255), Rgb(210, 220, 236), Rgb(220, 228, 242)));
        Platforms(batches, SoftTextures.Top("snow", Rgb(252, 253, 255), Rgb(226, 234, 246), Rgb(198, 210, 228)), Rgb(178, 190, 210));

        var white = batches.For(SceneTextures.White);
        int seed = 0;
        for (int row = 0; row < 3; row++)
        {
            float z = -15f - row * 6f;
            for (float x = -40f; x <= 28f; x += 2.6f - row * 0.2f)
            {
                seed++;
                float jx = (Rand(seed, row, 1) - 0.5f) * 1.6f, jz = (Rand(seed, row, 2) - 0.5f) * 2.4f;
                SnowPine(white, x + jx, z + jz, seed, (1.0f + row * 0.2f + Rand(seed, row, 3) * 0.3f) * 0.8f);
            }
        }
        for (int i = 0; i < 6; i++) SnowPine(white, -17f - i * 1.8f, 4f - i * 3.2f, 100 + i, 1.05f);

        // Mountains on the horizon, white above the tree line
        for (int i = 0; i < 7; i++)
        {
            float x = -120f + i * 40f + Rand(i, 1, 81) * 14f;
            float z = -115f - Rand(i, 2, 81) * 20f;
            float h = 26f + Rand(i, 3, 81) * 18f, r = 28f + Rand(i, 4, 81) * 14f;
            white.Lathe(new Vector3(x, -2f, z), new[] { new Vector2(r, 0f), new Vector2(r * 0.62f, h * 0.45f), new Vector2(r * 0.3f, h * 0.8f), new Vector2(0f, h) }, 16,
                k => k < 1 ? Rgb(120, 136, 164) : k < 2 ? Rgb(150, 166, 194) : Rgb(236, 242, 250));
        }

        // Drifts and capped rocks
        for (int i = 0; i < 12; i++)
        {
            float x = -24f + Rand(i, 1, 82) * 42f, z = -10f + Rand(i, 2, 82) * 26f;
            if (!Clear(x, z, 1.5f)) continue;
            float s = 0.8f + Rand(i, 3, 82) * 1.4f;
            white.Ellipsoid(new Vector3(x, 0, z), new Vector3(s * 1.6f, s * 0.5f, s), ring => Mix(Rgb(214, 224, 240), Rgb(252, 253, 255), ring / 8f), 14, 8);
        }
        foreach (var (x, z, s) in new[] { (-13.5f, -3.5f, 0.9f), (9.5f, 6.2f, 0.8f), (12f, -4.8f, 0.7f), (-9.5f, -11.5f, 1.1f) })
        {
            SoftFoliage.Rock(white, x, z, s, (int)(x * 7), Rgb(96, 102, 122), Rgb(140, 146, 160), Rgb(184, 190, 204));
            white.Ellipsoid(new Vector3(x, s * 0.78f, z), new Vector3(0.55f * s, 0.16f * s, 0.5f * s), Rgb(248, 250, 255), 12, 6);
        }
    }

    /// <summary>A pine under snow: dark tiers, white on their upper faces.</summary>
    private static void SnowPine(MeshBuilder white, float cx, float cz, int seed, float scale)
    {
        float s = (0.92f + Rand(seed, 0, 3) * 0.16f) * scale;
        white.Cylinder(new(cx, 0, cz), 0.12f * scale, 0.08f * scale, 1.1f * s, 10, Rgb(104, 80, 66), cap: false);
        var needles = Mix(Rgb(40, 84, 76), Rgb(56, 100, 86), Rand(seed, 0, 4));
        var snow = Rgb(240, 245, 252);
        (float BaseY, float ApexY, float R)[] tiers = { (0.5f, 2.0f, 0.74f), (1.22f, 2.7f, 0.62f), (1.94f, 3.4f, 0.5f), (2.66f, 4.1f, 0.36f) };
        float spin = Rand(seed, 0, 5) * MathF.Tau;
        for (int k = 0; k < tiers.Length; k++)
        {
            var (baseY, apexY, radius) = tiers[k];
            float h = (apexY - baseY) * s, r = radius * s;
            var tier = new MeshBuilder();
            tier.Lathe(new Vector3(cx, baseY * s, cz), new[]
            {
                new Vector2(0, 0.14f * h), new Vector2(0.86f * r, -0.03f * h), new Vector2(r, 0.03f * h),
                new Vector2(0.72f * r, 0.28f * h), new Vector2(0.38f * r, 0.64f * h), new Vector2(0, h)
            }, 16, i => i switch
            {
                0 or 1 => MeshBuilder.Scale(needles, 0.6f),
                2 => needles,
                3 => Mix(needles, snow, 0.75f),
                _ => snow
            });
            white.Append(tier, Matrix4x4.CreateTranslation(-cx, 0, -cz) * Matrix4x4.CreateRotationY(spin + k * 0.7f) * Matrix4x4.CreateTranslation(cx, 0, cz));
        }
    }

    // ------------------------------------------------------------------ sand

    /// <summary>A desert: rippled sand, dunes and banded mesas, dry shrubs and bleached rocks.</summary>
    private static void Desert(MeshBatches batches)
    {
        GroundDisc(batches, SoftTextures.Ground("sand", Rgb(228, 200, 148), Rgb(242, 220, 172), Rgb(206, 174, 122), Rgb(214, 184, 134), 0f, ripples: 1f));
        Platforms(batches, SoftTextures.Top("sand", Rgb(242, 224, 182), Rgb(216, 190, 142), Rgb(190, 160, 116)), Rgb(178, 138, 96));

        var white = batches.For(SceneTextures.White);
        // Dunes behind the field
        for (int i = 0; i < 12; i++)
        {
            float x = -70f + i * 13f + Rand(i, 1, 91) * 6f;
            float z = -22f - Rand(i, 2, 91) * 40f;
            float w = 12f + Rand(i, 3, 91) * 10f, h = 2.5f + Rand(i, 4, 91) * 4.5f;
            white.Ellipsoid(new Vector3(x, -0.6f, z), new Vector3(w, h, w * 0.55f), ring => Mix(Rgb(206, 166, 112), Rgb(246, 220, 168), ring / 10f), 20, 10);
        }
        // Mesas on the horizon
        foreach (var (x, z, r, h) in new[] { (-60f, -95f, 16f, 14f), (-14f, -110f, 22f, 18f), (40f, -100f, 14f, 11f), (78f, -90f, 18f, 15f) })
            white.Lathe(new Vector3(x, -1f, z), new[]
            {
                new Vector2(r, 0f), new Vector2(r * 0.94f, h * 0.3f), new Vector2(r * 0.9f, h * 0.55f), new Vector2(r * 0.86f, h * 0.95f), new Vector2(r * 0.82f, h), new Vector2(0f, h)
            }, 18, k => k switch { 0 => Rgb(170, 100, 72), 1 => Rgb(206, 132, 92), 2 => Rgb(186, 112, 80), 3 => Rgb(220, 150, 104), _ => Rgb(232, 176, 124) });

        // Rocks and dry shrubs
        foreach (var (x, z, s) in new[] { (-13.5f, -3.5f, 1f), (-9.5f, -11.5f, 1.2f), (10f, -6f, 0.9f), (14f, 1f, 0.7f), (-2.5f, 20.5f, 0.6f) })
            SoftFoliage.Rock(white, x, z, s, (int)(x * 9), Rgb(170, 120, 90), Rgb(208, 164, 124), Rgb(236, 206, 166));
        for (int i = 0; i < 18; i++)
        {
            float x = -22f + Rand(i, 1, 92) * 38f, z = -9f + Rand(i, 2, 92) * 26f;
            if (!Clear(x, z, 0.6f)) continue;
            float s = 0.35f + Rand(i, 3, 92) * 0.3f;
            for (int k = 0; k < 3; k++)
                white.Ellipsoid(new Vector3(x + (k - 1) * 0.3f * s, 0.25f * s, z + Rand(i, k, 93) * 0.2f), new Vector3(0.35f, 0.3f, 0.35f) * s,
                    ring => Mix(Rgb(120, 116, 70), Rgb(170, 160, 96), ring / 6f), 10, 6);
        }
    }

    // ------------------------------------------------------------------ indoors

    /// <summary>A room: floorboards, panelled walls with windows, shelves and plants, and a rug under each Pokémon.</summary>
    private static void Room(MeshBatches batches)
    {
        Floor(batches, SoftTextures.Planks("room", Rgb(206, 160, 112), Rgb(156, 112, 76)), -44f, -20f, 40f, 40f);
        var rug = SoftTextures.Top("rug", Rgb(206, 96, 84), Rgb(176, 66, 64), Rgb(236, 206, 146), rings: 3);
        Platforms(batches, rug, Rgb(150, 56, 56), 0.05f, 0f);

        var white = batches.For(SceneTextures.White);
        var walls = Backdrop(batches);
        var plaster = Rgb(238, 228, 208);
        var panel = Rgb(176, 138, 102);
        var trim = Rgb(120, 86, 62);
        // Back wall and the side walls
        Wall(walls, new Vector3(-44f, 0, -16f), new Vector3(40f, 0, -16f), 14f, plaster, panel, trim);
        Wall(walls, new Vector3(-21f, 0, 40f), new Vector3(-21f, 0, -16f), 14f, plaster, panel, trim);
        Wall(walls, new Vector3(19f, 0, -16f), new Vector3(19f, 0, 40f), 14f, plaster, panel, trim);

        // Windows on the back wall: dark glass that the light outside fills (the rig's window colour: daylight,
        // an orange dusk, the dark blue night)
        var glass = batches.For(SceneTextures.White, MeshPass.Light);
        foreach (float x in new[] { -15f, -1f, 13f })
        {
            walls.Box(new Vector3(x - 2.6f, 2.8f, -16.05f), new Vector3(x + 2.6f, 7.2f, -15.7f), trim, BoxFaces.All);
            walls.Box(new Vector3(x - 2.3f, 3.1f, -15.75f), new Vector3(x + 2.3f, 6.9f, -15.6f), Rgb(44, 54, 80), BoxFaces.All);
            glass.Quad(new Vector3(x - 2.3f, 3.1f, -15.58f), new Vector3(x + 2.3f, 3.1f, -15.58f), new Vector3(x + 2.3f, 6.9f, -15.58f), new Vector3(x - 2.3f, 6.9f, -15.58f),
                new(0, 1), new(1, 1), new(1, 0), new(0, 0), Rgb(236, 244, 255), Vector3.UnitZ);
            walls.Box(new Vector3(x - 0.1f, 3.1f, -15.56f), new Vector3(x + 0.1f, 6.9f, -15.45f), trim, BoxFaces.All);
            walls.Box(new Vector3(x - 2.3f, 4.9f, -15.56f), new Vector3(x + 2.3f, 5.1f, -15.45f), trim, BoxFaces.All);
            Halo(batches, SceneTextures.SoftGlow, new Vector3(x, 5f, -15.3f), 3.6f, new Color(255, 250, 236, 110));
        }

        // Shelves of books and potted plants
        Bookshelf(white, new Vector3(-9f, 0, -15.2f), 4.5f);
        Bookshelf(white, new Vector3(6.2f, 0, -15.2f), 3.8f);
        Bookshelf(white, new Vector3(-20.2f, 0, 4f), 5f, side: true);
        foreach (var (x, z) in new[] { (-18.5f, -13.5f), (16.5f, -13.5f), (-18.5f, 14f), (9f, -12.5f) }) PottedPlant(white, new Vector3(x, 0, z), 1.2f);
    }

    /// <summary>A wall from <paramref name="a"/> to <paramref name="b"/>: plaster above a wooden panel, with trim.</summary>
    private static void Wall(MeshBuilder b, Vector3 a, Vector3 c, float height, Color plaster, Color panel, Color trim)
    {
        var dir = Vector3.Normalize(c - a);
        var inward = new Vector3(-dir.Z, 0, dir.X);
        void Band(float y0, float y1, Color col, float depth)
        {
            var o = inward * depth;
            b.Quad(a + o + Vector3.UnitY * y0, c + o + Vector3.UnitY * y0, c + o + Vector3.UnitY * y1, a + o + Vector3.UnitY * y1,
                default, default, default, default, col, inward);
        }
        Band(0f, 2.4f, panel, 0.12f);
        Band(2.4f, 2.6f, trim, 0.18f);
        Band(2.6f, height, plaster, 0f);
        Band(height - 0.4f, height, trim, 0.06f);
    }

    private static void Bookshelf(MeshBuilder b, Vector3 at, float width, bool side = false)
    {
        var wood = Rgb(132, 94, 66);
        var sh = new MeshBuilder();
        sh.Box(new Vector3(-width / 2f, 0, -0.5f), new Vector3(width / 2f, 5f, 0.5f), wood, BoxFaces.All);
        Color[] books = { Rgb(196, 70, 64), Rgb(70, 116, 190), Rgb(226, 190, 90), Rgb(90, 160, 110), Rgb(150, 90, 170) };
        for (int shelf = 0; shelf < 4; shelf++)
        {
            float y = 0.3f + shelf * 1.2f;
            float x = -width / 2f + 0.25f;
            int k = shelf * 7;
            while (x < width / 2f - 0.4f)
            {
                float w = 0.18f + Rand(k, shelf, 101) * 0.16f, h = 0.7f + Rand(k, shelf, 102) * 0.25f;
                sh.Box(new Vector3(x, y, 0.42f), new Vector3(x + w, y + h, 0.56f), books[k % books.Length], BoxFaces.All);
                x += w + 0.02f;
                k++;
            }
        }
        b.Append(sh, (side ? Matrix4x4.CreateRotationY(MathF.PI / 2f) : Matrix4x4.Identity) * Matrix4x4.CreateTranslation(at));
    }

    private static void PottedPlant(MeshBuilder b, Vector3 at, float size)
    {
        b.Cylinder(at, 0.45f * size, 0.55f * size, 0.8f * size, 14, Rgb(196, 112, 76));
        for (int k = 0; k < 4; k++)
        {
            var off = new Vector3((k % 2 - 0.5f) * 0.5f * size, (1.3f + k * 0.25f) * size, (k / 2 - 0.5f) * 0.4f * size);
            b.Ellipsoid(at + off, new Vector3(0.55f, 0.5f, 0.55f) * size, ring => MeshBuilder.Sway(Mix(Rgb(56, 120, 66), Rgb(126, 188, 100), ring / 8f), 0.1f), 12, 8);
        }
    }

    // ------------------------------------------------------------------ gyms and the League

    /// <summary>The colours of a hall themed on <paramref name="theme"/> (none: the Champion's white and gold).</summary>
    public static (Color Floor, Color FloorAlt, Color Wall, Color Trim, Color Accent, Color Glow) HallColors(PokemonType? theme, bool league)
    {
        if (theme == null) return (Rgb(236, 232, 222), Rgb(214, 206, 190), Rgb(232, 228, 220), Rgb(212, 176, 92), Rgb(226, 190, 96), Rgb(255, 236, 170));
        var c = MoveFx.ColorsOf(theme.Value);
        var dark = league ? Rgb(42, 38, 52) : Rgb(96, 92, 104);
        return (Mix(dark, c.Dark, 0.35f), Mix(dark, c.Main, 0.22f), league ? Rgb(52, 48, 62) : Rgb(212, 206, 196),
            league ? Mix(Rgb(70, 64, 80), c.Light, 0.25f) : Mix(Rgb(150, 146, 156), c.Main, 0.3f), c.Main, c.Light);
    }

    /// <summary>
    /// A gym hall or a room of the League: a polished floor with the type's emblem, raised platforms ringed with
    /// light, columns and banners, and props of the type.
    /// </summary>
    private static void Hall(MeshBatches batches, PokemonType? theme, bool league)
    {
        var (floor, floorAlt, wall, trim, accent, glow) = HallColors(theme, league);
        string key = (theme?.ToString() ?? "champion") + (league ? "_league" : "_gym");
        Floor(batches, SoftTextures.Tiles(key, floor, floorAlt, MeshBuilder.Scale(floor, 0.7f)), -44f, -24f, 40f, 40f, 8f);

        // The emblem between the platforms, laid over the tiles
        var emblem = batches.For(SoftTextures.Emblem(key, accent, glow), MeshPass.Ground);
        var mid = (Enemy + Player) / 2f + new Vector3(0, 0.006f, 0);
        const float er = 5.2f;
        emblem.Quad(mid + new Vector3(-er, 0, er), mid + new Vector3(er, 0, er), mid + new Vector3(er, 0, -er), mid + new Vector3(-er, 0, -er),
            new(0, 1), new(1, 1), new(1, 0), new(0, 0), Color.White, Vector3.UnitY);

        // Platforms: a metal drum under a top in the type's colours, ringed with light
        var top = SoftTextures.Top(key, Mix(floor, Rgb(255, 255, 255), 0.5f), Mix(floorAlt, accent, 0.25f), accent, rings: 2);
        Platforms(batches, top, Rgb(150, 154, 168), 0.3f, 0f);
        Halo(batches, SoftTextures.GlowRing, Enemy + new Vector3(0, 0.02f, 0), EnemyRadius * 1.25f, new Color(glow.R, glow.G, glow.B, (byte)200), flat: true);
        Halo(batches, SoftTextures.GlowRing, Player + new Vector3(0, 0.02f, 0), PlayerRadius * 1.25f, new Color(glow.R, glow.G, glow.B, (byte)200), flat: true);

        var white = batches.For(SceneTextures.White);
        var walls = Backdrop(batches);
        // Walls: a dark plinth, the wall, a band of the type's colour and a cornice
        Wall(walls, new Vector3(-46f, 0, -22f), new Vector3(42f, 0, -22f), 18f, wall, MeshBuilder.Scale(wall, 0.7f), trim);
        Wall(walls, new Vector3(-24f, 0, 40f), new Vector3(-24f, 0, -22f), 18f, wall, MeshBuilder.Scale(wall, 0.7f), trim);
        Wall(walls, new Vector3(21f, 0, -22f), new Vector3(21f, 0, 40f), 18f, wall, MeshBuilder.Scale(wall, 0.7f), trim);

        // Banners down the back wall
        foreach (float x in new[] { -26f, -13f, 0f, 13f, 26f })
        {
            walls.Box(new Vector3(x - 1.6f, 4f, -21.9f), new Vector3(x + 1.6f, 15f, -21.7f), accent, BoxFaces.All);
            walls.Box(new Vector3(x - 1.2f, 4.6f, -21.72f), new Vector3(x + 1.2f, 14.4f, -21.6f), Mix(accent, glow, 0.35f), BoxFaces.All);
            walls.Box(new Vector3(x - 1.8f, 15f, -21.95f), new Vector3(x + 1.8f, 15.4f, -21.5f), trim, BoxFaces.All);
        }

        // Columns along both sides with lamps
        foreach (float x in new[] { -18.5f, 15.5f })
            for (float z = -18f; z <= 30f; z += 9f)
            {
                walls.Cylinder(new Vector3(x, 0, z), 1.25f, 1.2f, 0.8f, 16, trim);
                walls.Cylinder(new Vector3(x, 0.8f, z), 0.85f, 0.8f, 13f, 16, league ? Mix(wall, accent, 0.2f) : Mix(wall, Rgb(255, 255, 255), 0.3f));
                walls.Cylinder(new Vector3(x, 13.8f, z), 1.2f, 1.3f, 0.7f, 16, trim);
                Halo(batches, SceneTextures.SoftGlow, new Vector3(x, 9f, z + 1.2f), 1.8f, new Color(glow.R, glow.G, glow.B, (byte)170));
                walls.Ellipsoid(new Vector3(x, 9f, z + 0.9f), new Vector3(0.35f, 0.45f, 0.35f), Mix(glow, Rgb(255, 255, 255), 0.5f), 10, 6);
            }

        HallProps(batches, white, theme, league, accent, glow);
    }

    /// <summary>What sets each type's hall apart.</summary>
    private static void HallProps(MeshBatches batches, MeshBuilder white, PokemonType? theme, bool league, Color accent, Color glow)
    {
        switch (theme)
        {
            case PokemonType.Rock:
            case PokemonType.Ground:
                foreach (var (x, z, s) in new[] { (-13f, -10f, 1.6f), (-11f, -14f, 1.1f), (9.5f, -12f, 1.4f), (12f, -6f, 1f), (-14f, 5f, 1.2f), (-3f, -17f, 1.8f) })
                    SoftFoliage.Rock(white, x, z, s, (int)(x * 13), Rgb(110, 92, 80), Rgb(156, 134, 112), Rgb(196, 176, 150));
                break;
            case PokemonType.Grass:
            case PokemonType.Bug:
                foreach (float x in new[] { -20f, -10f, 0f, 10f })
                {
                    white.Box(new Vector3(x - 3f, 0, -20.6f), new Vector3(x + 3f, 1.2f, -18.6f), Rgb(150, 110, 80), BoxFaces.All);
                    TreeModels.Round(batches, x, -19.6f, (int)x + 50, 3, 1.4f, soft: true);
                }
                for (int i = 0; i < 40; i++)
                {
                    float x = -20f + Rand(i, 1, 111) * 34f, z = -16f + Rand(i, 2, 111) * 24f;
                    if (Clear(x, z, 1.4f) && MathF.Abs(x + 3.3f) > 7f) SoftFoliage.Tuft(white, x, z, 0.8f, 0.5f, i + 900);
                }
                break;
            case PokemonType.Water:
            {
                // Pools along the sides
                var water = batches.For(SoftTextures.Water, MeshPass.SoftWater);
                foreach (var (x0, x1) in new[] { (-17.5f, -12f), (9f, 14.5f) })
                {
                    white.Box(new Vector3(x0 - 0.4f, 0, -20f), new Vector3(x1 + 0.4f, 0.35f, 12f), Rgb(170, 196, 214), BoxFaces.All);
                    var calm = new Color(0, 0, 0, 255);
                    water.Quad(new Vector3(x0, 0.38f, 12f), new Vector3(x1, 0.38f, 12f), new Vector3(x1, 0.38f, -20f), new Vector3(x0, 0.38f, -20f),
                        default, default, default, default, calm, Vector3.UnitY);
                }
                break;
            }
            case PokemonType.Ghost:
            case PokemonType.Dark:
            case PokemonType.Psychic:
                // Candles on tall stands, burning with a soft light
                foreach (var (x, z) in new[] { (-12f, -12f), (-14f, -2f), (10f, -13f), (12.5f, -4f), (-3f, -18f), (4f, -18f) })
                {
                    white.Cylinder(new Vector3(x, 0, z), 0.4f, 0.18f, 3.2f, 12, Rgb(80, 70, 90));
                    white.Cylinder(new Vector3(x, 3.2f, z), 0.16f, 0.15f, 0.7f, 10, Rgb(236, 228, 210));
                    Halo(batches, SceneTextures.SoftGlow, new Vector3(x, 4.2f, z + 0.2f), 1.1f, new Color(glow.R, glow.G, glow.B, (byte)210));
                }
                break;
            case PokemonType.Steel:
                foreach (float x in new[] { -16f, -8f, 0f, 8f })
                {
                    white.Box(new Vector3(x - 0.4f, 0, -21.5f), new Vector3(x + 0.4f, 17f, -20.7f), Rgb(150, 158, 172), BoxFaces.All);
                    white.Box(new Vector3(x - 3.6f, 11f, -21.5f), new Vector3(x + 3.6f, 11.6f, -20.7f), Rgb(126, 132, 146), BoxFaces.All);
                }
                break;
            case PokemonType.Ice:
                foreach (var (x, z, s) in new[] { (-12f, -10f, 2.4f), (10f, -12f, 2.8f), (-14f, 4f, 1.8f), (12.5f, 0f, 2f), (0f, -17f, 3f) })
                    Crystals(batches, new Vector3(x, 0, z), s, Rgb(170, 220, 255), (int)(x * 3));
                break;
            case PokemonType.Electric:
                foreach (var (x, z) in new[] { (-12f, -11f), (10.5f, -11f), (-13f, 3f), (11.5f, 1f) })
                {
                    white.Cylinder(new Vector3(x, 0, z), 0.9f, 0.6f, 1f, 14, Rgb(70, 70, 80));
                    for (int k = 0; k < 5; k++) white.Torus(new Vector3(x, 1.4f + k * 0.55f, z), 0.45f - k * 0.04f, 0.1f, Rgb(200, 150, 80), 16, 8);
                    white.Cylinder(new Vector3(x, 1f, z), 0.2f, 0.2f, 3f, 10, Rgb(110, 110, 120));
                    white.Ellipsoid(new Vector3(x, 4.3f, z), new Vector3(0.5f, 0.5f, 0.5f), Rgb(255, 246, 180), 12, 8);
                    Halo(batches, SceneTextures.SoftGlow, new Vector3(x, 4.3f, z + 0.5f), 1.6f, new Color(255, 236, 120, 220));
                }
                break;
            case PokemonType.Fire:
            case PokemonType.Dragon:
                // Braziers
                foreach (var (x, z) in new[] { (-12f, -10f), (10f, -12f), (-13.5f, 3f), (11.5f, -1f) })
                {
                    white.Lathe(new Vector3(x, 0, z), new[] { new Vector2(0.5f, 0f), new Vector2(0.3f, 1.6f), new Vector2(1.1f, 2.2f), new Vector2(1.2f, 2.5f), new Vector2(0f, 2.3f) }, 16,
                        k => k < 2 ? Rgb(80, 70, 70) : Rgb(140, 110, 90));
                    Halo(batches, SceneTextures.SoftGlow, new Vector3(x, 3.2f, z + 0.4f), 2.2f, new Color(255, 160, 80, 220));
                }
                break;
            case PokemonType.Fighting:
                foreach (var (x, z) in new[] { (-12f, -9f), (-14f, 0f), (11f, -10f), (12.5f, -2f) })
                {
                    white.Cylinder(new Vector3(x, 0, z), 0.5f, 0.5f, 2.8f, 14, Rgb(166, 120, 84));
                    white.Cylinder(new Vector3(x, 2.8f, z), 0.55f, 0.5f, 0.25f, 14, accent);
                }
                break;
            default:
                if (theme == null)
                    // The Champion's room: golden urns and pale pillars of light
                    foreach (var (x, z) in new[] { (-12f, -12f), (10f, -12f), (-14f, 2f), (12f, 0f) })
                    {
                        white.Lathe(new Vector3(x, 0, z), new[] { new Vector2(0.4f, 0f), new Vector2(0.9f, 0.6f), new Vector2(0.5f, 1.6f), new Vector2(0.7f, 2f), new Vector2(0f, 2f) }, 16,
                            _ => Rgb(222, 186, 96));
                        Halo(batches, SceneTextures.SoftGlow, new Vector3(x, 3f, z + 0.3f), 2f, new Color(255, 240, 190, 160));
                    }
                break;
        }
    }
}
