using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// HD-2D characters: each 3D rig is rendered once per facing and walk frame into a small pixel-art sprite (at the
/// field's texel density, with a one-pixel outline), then drawn in the field as an upright billboard that is lit
/// by the scene and casts a shadow.
/// </summary>
internal static class CharacterSprites
{
    /// <summary>Sprite texels per world unit: the same density as the field's ground and building textures.</summary>
    public const int TexelsPerUnit = 32;

    private const int SpriteW = 40, SpriteH = 58;
    private const float FrameW = SpriteW / (float)TexelsPerUnit, FrameH = SpriteH / (float)TexelsPerUnit;

    // Seen a little from above, like hand-drawn overworld sprites
    private const float ViewPitchDeg = 24f;
    private const int WalkFrames = 4;

    private sealed record Baked(Texture2D Texture, Material Color, Material Depth);

    private static readonly Dictionary<(CharacterRig, int, int, bool), Baked> Cache = new();
    private static RenderTexture2D target;
    private static Mesh quad;
    private static bool loaded;

    private static readonly SceneLighting StudioLight = new(Vector3.Normalize(new Vector3(-0.5f, 0.75f, 0.6f)),
        new Vector3(0.62f, 0.58f, 0.52f), new Vector3(0.6f, 0.62f, 0.7f), new Vector3(0.42f, 0.4f, 0.38f));

    private static void EnsureLoaded()
    {
        if (loaded) return;
        target = Raylib.LoadRenderTexture(SpriteW, SpriteH);

        // Unit quad standing on the origin, facing +Z (toward the field camera)
        var b = new MeshBuilder();
        b.Quad(new(-0.5f, 0, 0), new(0.5f, 0, 0), new(0.5f, 1, 0), new(-0.5f, 1, 0), new(0, 1), new(1, 1), new(1, 0), new(0, 0),
            Color.White, Vector3.UnitZ);
        quad = b.Upload();
        loaded = true;
    }

    private static int FacingIndex(float yaw)
    {
        int i = (int)MathF.Round(yaw / (MathF.PI / 2f));
        return ((i % 4) + 4) % 4;
    }

    private static int FrameIndex(CharacterPose pose) =>
        pose.WalkBlend < 0.5f ? 0 : 1 + (int)MathF.Floor(((pose.Walk % 1f + 1f) % 1f) * WalkFrames) % WalkFrames;

    /// <summary>Bakes the sprite this pose needs, if it isn't cached yet. Call outside any texture mode.</summary>
    public static void Prepare(RenderContext context, CharacterRig rig, CharacterPose pose, float yaw)
    {
        var key = (rig, FacingIndex(yaw), FrameIndex(pose), pose.Running && pose.WalkBlend >= 0.5f);
        if (Cache.ContainsKey(key)) return;
        EnsureLoaded();

        // A representative pose for the frame: idle at rest, or one quarter of the walk cycle
        var bakePose = new CharacterPose { Time = 0.4f, Running = key.Item4 };
        if (key.Item3 > 0)
        {
            bakePose.Walk = (key.Item3 - 1) / (float)WalkFrames + 0.125f;
            bakePose.WalkBlend = 1f;
        }
        float bakeYaw = key.Item2 * MathF.PI / 2f;

        var shaders = context.Shaders;
        float pitch = ViewPitchDeg * MathF.PI / 180f;
        var look = new Vector3(0, FrameH / 2f - 0.05f, 0);
        var camera = new Camera3D(look + new Vector3(0, MathF.Sin(pitch), MathF.Cos(pitch)) * 20f, look, Vector3.UnitY,
            FrameH, CameraProjection.Orthographic);

        Raylib.BeginTextureMode(target);
        Raylib.ClearBackground(new Color(0, 0, 0, 0));
        Rlgl.SetClipPlanes(1.0, 60.0);
        Raylib.BeginMode3D(camera);
        shaders.SetLighting(Matrix4x4.Identity, StudioLight, camera.Position, 1f);
        shaders.SetCharacterStyle(shadowStrength: 0f, rimStrength: 0.2f);
        CharacterRenderer.Draw(context, rig, bakePose, Matrix4x4.CreateRotationY(bakeYaw), CharacterPass.Color, trueProportions: true);
        Raylib.EndMode3D();
        Raylib.EndTextureMode();
        Rlgl.SetClipPlanes(0.01, 1000.0);

        var image = Raylib.LoadImageFromTexture(target.Texture);
        Raylib.ImageFlipVertical(ref image);
        var canvas = PixelCanvas.FromImage(image);
        Raylib.UnloadImage(image);
        canvas.OutlinePass(innerSeams: false);
        var tex = canvas.ToTexture();
        Raylib.SetTextureWrap(tex, TextureWrap.Clamp);

        Cache[key] = new Baked(tex, RenderContext.MaterialFor(shaders.Sprite, tex), RenderContext.MaterialFor(shaders.Depth, tex));
    }

    /// <summary>
    /// Draws a prepared sprite as an upright card at <paramref name="feet"/>, stretched by the field's vertical
    /// scale so it reads at its true proportions from the steep camera.
    /// </summary>
    public static void DrawBillboard(RenderContext context, CharacterRig rig, CharacterPose pose, float yaw, Vector3 feet, float vs, CharacterPass pass)
    {
        if (pass == CharacterPass.Outline) return;
        var key = (rig, FacingIndex(yaw), FrameIndex(pose), pose.Running && pose.WalkBlend >= 0.5f);
        if (!Cache.TryGetValue(key, out var baked)) return;

        // The sprite's bottom row is a little below the feet (the frame leaves a margin under the shoes)
        var m = Matrix4x4.CreateScale(FrameW, FrameH * vs, 1f) * Matrix4x4.CreateTranslation(feet + new Vector3(0, -0.05f * vs, 0.02f));
        Raylib.DrawMesh(quad, pass == CharacterPass.Depth ? baked.Depth : baked.Color, Matrix4x4.Transpose(m));
    }
}
