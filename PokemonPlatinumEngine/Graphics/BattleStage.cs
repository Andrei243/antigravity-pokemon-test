using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The 3D battle field: a meadow with two grassy platforms, the local forest behind the opponent and hills on the
/// horizon, lit and shadowed like the overworld. The opponent's platform sits at the origin; the player's is
/// closer to the camera, so it appears lower-left and larger, as in the DS games.
/// </summary>
internal sealed class BattleStage
{
    public static readonly Vector3 EnemySpot = Vector3.Zero;
    public static readonly Vector3 PlayerSpot = new(-6.7f, 0, 15.6f);
    public const float EnemyPlatformRadius = 2.4f;
    public const float PlayerPlatformRadius = 1.2f;
    private const float PlatformHeight = 0.16f;

    // Final camera: pitched 9° down with a 24° field of view, which puts the opponent's feet at about
    // (1430, 400) and the player's at (470, 800) on the 1920x1080 screen
    private static readonly Vector3 CameraPosition = new(-4.72f, 2.6f, 25.4f);
    private const float PitchDeg = 9f;
    public const float FovYDeg = 24f;

    public SceneLighting Lighting { get; } = new(Vector3.Normalize(new Vector3(-0.5f, 0.85f, 0.45f)), new Vector3(0.5f, 0.48f, 0.43f),
        new Vector3(0.6f, 0.64f, 0.72f), new Vector3(0.5f, 0.48f, 0.42f));

    private readonly SceneMeshes meshes;

    private BattleStage(SceneMeshes meshes) => this.meshes = meshes;

    public void Draw() => meshes.Draw();
    public void DrawDepth() => meshes.DrawDepth();

    /// <summary>
    /// Camera for the battle: a slow sweep in from the side while the battle opens, then a gentle drift.
    /// <paramref name="shake"/> jolts it after a strong hit.
    /// </summary>
    public static Camera3D Camera(float time, float shake)
    {
        float pitch = PitchDeg * MathF.PI / 180f;
        var forward = new Vector3(0, -MathF.Sin(pitch), -MathF.Cos(pitch));
        var target = CameraPosition + forward * 20f;

        // Orbit about the middle of the field
        var pivot = (EnemySpot + PlayerSpot) / 2f;
        float intro = 1f - EaseOut(Math.Clamp(time / 1.8f, 0f, 1f));
        float orbit = 0.55f * intro + 0.012f * MathF.Sin(time * 0.35f);
        float zoom = 1f + 0.35f * intro;
        var offset = Vector3.Transform(CameraPosition - pivot, Matrix4x4.CreateRotationY(orbit)) * zoom;
        var position = pivot + offset;
        var look = pivot + Vector3.Transform(target - pivot, Matrix4x4.CreateRotationY(orbit));

        if (shake > 0f)
        {
            var jolt = new Vector3(MathF.Sin(time * 71f), MathF.Sin(time * 53f + 1f), 0) * 0.06f * shake;
            position += jolt;
            look += jolt;
        }
        return new Camera3D(position, look, Vector3.UnitY, FovYDeg, CameraProjection.Perspective);
    }

    private static float EaseOut(float t) => 1f - (1f - t) * (1f - t) * (1f - t);

    public static BattleStage Build(FieldShaders shaders, TreeStyle trees)
    {
        var batches = new MeshBatches();

        // Meadow: one big textured disc, tiled every four units
        var ground = batches.For(SceneTextures.Meadow, MeshPass.Ground);
        const int rings = 12, segments = 48;
        const float radius = 90f;
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

        AddPlatform(batches, EnemySpot, EnemyPlatformRadius);
        AddPlatform(batches, PlayerSpot, PlayerPlatformRadius);

        // Forest behind the opponent, far enough back to leave a band of sky above it
        int seed = 0;
        for (int row = 0; row < 3; row++)
        {
            float z = -15f - row * 6f;
            for (float x = -38f; x <= 26f; x += 2.6f - row * 0.2f)
            {
                seed++;
                float jitterX = (GroundBaker.Rand01(seed, row, 1) - 0.5f) * 1.6f;
                float jitterZ = (GroundBaker.Rand01(seed, row, 2) - 0.5f) * 2.4f;
                float scale = 1.0f + row * 0.2f + GroundBaker.Rand01(seed, row, 3) * 0.3f;
                // Pines are taller than the round trees, so they are scaled down to keep the sky in view
                if (trees == TreeStyle.Pine) TreeModels.Pine(batches, x + jitterX, z + jitterZ, seed, row, scale * 0.8f);
                else TreeModels.Round(batches, x + jitterX, z + jitterZ, seed, row, scale);
            }
        }
        // A few trees framing the left side of the field
        for (int i = 0; i < 6; i++)
        {
            float x = -17f - i * 1.8f, z = 4f - i * 3.2f;
            if (trees == TreeStyle.Pine) TreeModels.Pine(batches, x, z, 100 + i, 7, 1.3f);
            else TreeModels.Round(batches, x, z, 100 + i, 7, 1.3f);
        }

        // Rolling hills on the horizon
        var hills = batches.For(SceneTextures.White);
        for (int i = 0; i < 11; i++)
        {
            float x = -110f + i * 22f + GroundBaker.Rand01(i, 0, 9) * 8f;
            float h = 5f + GroundBaker.Rand01(i, 1, 9) * 5f;
            var tone = PixelCanvas.Mix(new Color(112, 170, 140, 255), new Color(138, 188, 156, 255), GroundBaker.Rand01(i, 2, 9));
            hills.Ellipsoid(new Vector3(x, -1f, -95f - GroundBaker.Rand01(i, 3, 9) * 12f), new Vector3(20f, h, 9f), tone, 20, 8);
        }

        // Tufts of grass scattered over the meadow and around the platforms
        var tufts = batches.For(SceneTextures.GrassTuft);
        for (int i = 0; i < 140; i++)
        {
            float x = -26f + GroundBaker.Rand01(i, 5, 11) * 44f;
            float z = -9f + GroundBaker.Rand01(i, 6, 11) * 28f;
            if (Vector2.Distance(new(x, z), new(EnemySpot.X, EnemySpot.Z)) < EnemyPlatformRadius + 0.2f) continue;
            if (Vector2.Distance(new(x, z), new(PlayerSpot.X, PlayerSpot.Z)) < PlayerPlatformRadius + 0.2f) continue;
            TreeModels.Crossed(tufts, x, z, 0.7f, 0.45f, i);
        }

        return new BattleStage(SceneMeshes.Upload(batches, shaders));
    }

    /// <summary>Raised oval of lush grass with a dirt rim, like the DS battle platforms.</summary>
    private static void AddPlatform(MeshBatches batches, Vector3 center, float radius)
    {
        var dirt = new Color(170, 134, 92, 255);
        batches.For(SceneTextures.White).Lathe(center, new[]
        {
            new Vector2(radius * 1.02f, -0.05f), new Vector2(radius * 1.02f, PlatformHeight * 0.55f),
            new Vector2(radius, PlatformHeight), new Vector2(radius * 0.97f, PlatformHeight)
        }, 48, i => i <= 1 ? MeshBuilder.Scale(dirt, 0.8f) : dirt);

        var top = batches.For(SceneTextures.PlatformTop, MeshPass.Ground);
        const int segments = 48;
        float y = PlatformHeight;
        for (int s = 0; s < segments; s++)
        {
            float a0 = s * MathF.Tau / segments, a1 = (s + 1) * MathF.Tau / segments;
            var c = center + new Vector3(0, y, 0);
            var p0 = center + new Vector3(MathF.Cos(a0) * radius * 0.97f, y, MathF.Sin(a0) * radius * 0.97f);
            var p1 = center + new Vector3(MathF.Cos(a1) * radius * 0.97f, y, MathF.Sin(a1) * radius * 0.97f);
            Vector2 T(float a, float rr) => new(0.5f + MathF.Cos(a) * rr * 0.5f, 0.5f + MathF.Sin(a) * rr * 0.5f);
            top.Tri(c, p1, p0, new(0.5f, 0.5f), T(a1, 1f), T(a0, 1f), Color.White, Vector3.UnitY);
        }
    }
}
