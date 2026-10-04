using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Look-dev: renders a character's 3D model on its own, large and under a studio light, as battles show it
/// (smooth face, outline, materials). The harness uses it for turntables. Call outside any texture mode.
/// </summary>
internal static class CharacterStudio
{
    private static readonly SceneLighting Light = new(Vector3.Normalize(new Vector3(-0.5f, 0.8f, 0.55f)),
        new Vector3(0.52f, 0.48f, 0.4f), new Vector3(0.56f, 0.6f, 0.72f), new Vector3(0.48f, 0.47f, 0.42f));

    /// <summary>
    /// Renders a character into <paramref name="target"/> over nothing (the interface lays it over its own
    /// backdrop): the whole figure, <paramref name="viewHeight"/> model units tall round
    /// <paramref name="lookY"/> (both times the character's scale). Call outside any other texture mode.
    /// </summary>
    public static void Render(RenderContext context, string npcType, RenderTexture2D target, CharacterPose pose, float yaw = 0f,
        float viewHeight = 1.9f, float lookY = 0.8f)
    {
        context.EnsureLoaded();
        var rig = CharacterModels.Get(npcType, context.Shaders);
        var shaders = context.Shaders;
        var look = new Vector3(0, lookY * rig.Scale, 0);
        float pitch = 8f * MathF.PI / 180f;
        var camera = new Camera3D(look + new Vector3(0, MathF.Sin(pitch), MathF.Cos(pitch)) * 6f, look, Vector3.UnitY, viewHeight * rig.Scale, CameraProjection.Orthographic);

        Raylib.BeginTextureMode(target);
        Raylib.ClearBackground(new Color(0, 0, 0, 0));
        Rlgl.SetClipPlanes(0.5, 30.0);
        Raylib.BeginMode3D(camera);
        shaders.SetLighting(Matrix4x4.Identity, Light, camera.Position, 1f);
        shaders.SetCharacterStyle(shadowStrength: 0f, rimStrength: 0.3f);
        shaders.SetStudio();
        Rlgl.DisableBackfaceCulling();
        var root = Matrix4x4.CreateRotationY(yaw);
        CharacterRenderer.Draw(context, rig, pose, root, CharacterPass.Color);
        CharacterRenderer.Draw(context, rig, pose, root, CharacterPass.Outline);
        Rlgl.EnableBackfaceCulling();
        Raylib.EndMode3D();
        Raylib.EndTextureMode();
        Rlgl.SetClipPlanes(0.01, 1000.0);
    }

    /// <summary>
    /// Renders a Poké Ball into <paramref name="target"/> over nothing, as battles show one: its button turned
    /// to the eye by <paramref name="yaw"/>, its lid <paramref name="open"/> from 0 (shut) to 1. Call outside
    /// any other texture mode.
    /// </summary>
    public static void RenderBall(RenderContext context, RenderTexture2D target, string ball, float open, float yaw)
    {
        context.EnsureLoaded();
        var shaders = context.Shaders;
        var model = BattleBall.Get(ball);
        var look = Vector3.Zero;
        float pitch = 14f * MathF.PI / 180f;
        var camera = new Camera3D(look + new Vector3(0, MathF.Sin(pitch), MathF.Cos(pitch)) * 6f, look, Vector3.UnitY, 1f, CameraProjection.Orthographic);

        Raylib.BeginTextureMode(target);
        Raylib.ClearBackground(new Color(0, 0, 0, 0));
        Rlgl.SetClipPlanes(0.5, 30.0);
        Raylib.BeginMode3D(camera);
        shaders.SetLighting(Matrix4x4.Identity, Light, camera.Position, 1f);
        shaders.SetCharacterStyle(shadowStrength: 0f, rimStrength: 0.3f);
        shaders.SetStudio();
        Rlgl.DisableBackfaceCulling();
        // Half the picture across, so the lid has room to swing back
        var world = BattleBall.Place(new Vector3(0, -0.1f, 0), 0.5f, yaw);
        BattleBall.Draw(context, model, world, open, CharacterPass.Color);
        BattleBall.Draw(context, model, world, open, CharacterPass.Outline);
        Rlgl.EnableBackfaceCulling();
        Raylib.EndMode3D();
        Raylib.EndTextureMode();
        Rlgl.SetClipPlanes(0.01, 1000.0);
    }

    /// <summary>
    /// One view per yaw, side by side, each <paramref name="w"/> × <paramref name="h"/> pixels, framing
    /// <paramref name="viewHeight"/> model units round <paramref name="lookY"/> (both times the character's scale).
    /// </summary>
    public static Image Turntable(RenderContext context, string npcType, float[] yaws, int w, int h, CharacterPose pose, float viewHeight = 1.7f, float lookY = 0.7f)
    {
        context.EnsureLoaded();
        var rig = CharacterModels.Get(npcType, context.Shaders);
        var shaders = context.Shaders;
        var sheet = Raylib.GenImageColor(w * yaws.Length, h, new Color(206, 218, 232, 255));
        var target = Raylib.LoadRenderTexture(w, h);

        for (int i = 0; i < yaws.Length; i++)
        {
            var look = new Vector3(0, lookY * rig.Scale, 0);
            float pitch = 12f * MathF.PI / 180f;
            var camera = new Camera3D(look + new Vector3(0, MathF.Sin(pitch), MathF.Cos(pitch)) * 6f, look, Vector3.UnitY, viewHeight * rig.Scale, CameraProjection.Orthographic);
            var root = Matrix4x4.CreateRotationY(yaws[i]);

            Raylib.BeginTextureMode(target);
            Raylib.ClearBackground(new Color(206, 218, 232, 255));
            Rlgl.SetClipPlanes(0.5, 30.0);
            Raylib.BeginMode3D(camera);
            shaders.SetLighting(Matrix4x4.Identity, Light, camera.Position, 1f);
            shaders.SetCharacterStyle(shadowStrength: 0f, rimStrength: 0.3f);
            shaders.SetStudio();
            Rlgl.DisableBackfaceCulling();
            CharacterRenderer.Draw(context, rig, pose, root, CharacterPass.Color);
            CharacterRenderer.Draw(context, rig, pose, root, CharacterPass.Outline);
            Raylib.EndMode3D();
            Raylib.EndTextureMode();

            var view = Raylib.LoadImageFromTexture(target.Texture);
            Raylib.ImageFlipVertical(ref view);
            Raylib.ImageDraw(ref sheet, view, new Rectangle(0, 0, w, h), new Rectangle(i * w, 0, w, h), Color.White);
            Raylib.UnloadImage(view);
        }
        Rlgl.SetClipPlanes(0.01, 1000.0);
        Raylib.UnloadRenderTexture(target);
        return sheet;
    }
}
