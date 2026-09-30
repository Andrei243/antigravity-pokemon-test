using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>Which way a Pokémon sprite looks: the opponent faces the camera, the player's Pokémon shows its back.</summary>
internal enum SpriteView { Front, Back, Icon }

/// <summary>Camera placement that frames one model for one view.</summary>
internal sealed class SpriteFraming
{
    public Camera3D Camera;
    public float Yaw;

    /// <summary>Row of the sprite (0 top .. 1 bottom) where the model's lowest point sits at rest.</summary>
    public float FeetRow;

    /// <summary>World units covered by one sprite pixel.</summary>
    public float WorldPerPixel;
}

/// <summary>
/// Renders the 3D Pokémon models into low-resolution pixel-art sprites for the menus (2D sprites, as in the main
/// games), baked once at start-up. Also holds the outline hulls battles use when they draw the models in 3D.
/// </summary>
internal static class PokemonSprites
{
    public const int Size = 128;
    public const int IconSize = 48;

    private static readonly Dictionary<(string, SpriteView, int), SpriteFraming> Framings = new();
    private static readonly Dictionary<string, Texture2D> Baked = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<PokeModel> Uploaded = new();
    private static readonly Matrix4x4[] BoneMatrices = new Matrix4x4[64];

    // A soft studio light from the upper left, independent of the field's sun
    private static readonly SceneLighting Light = new(Vector3.Normalize(new Vector3(-0.55f, 0.7f, 0.55f)),
        new Vector3(0.6f, 0.58f, 0.53f), new Vector3(0.6f, 0.62f, 0.7f), new Vector3(0.42f, 0.4f, 0.38f));

    private static readonly Color OutlineInk = new(30, 24, 40, 255);

    public static SpriteFraming Framing(PokeModel model, SpriteView view, int size)
    {
        if (Framings.TryGetValue((model.Species, view, size), out var f)) return f;

        float yaw = view == SpriteView.Back ? 2.55f : -0.5f;
        float pitch = (view == SpriteView.Back ? 20f : 10f) * MathF.PI / 180f;
        var up = new Vector3(0, MathF.Cos(pitch), -MathF.Sin(pitch));
        var forward = new Vector3(0, -MathF.Sin(pitch), -MathF.Cos(pitch));

        // Project the rest pose to find the silhouette's extent in this view
        var rest = new Matrix4x4[model.Bones.Count];
        model.BoneTransforms(default, rest);
        var turn = Matrix4x4.CreateRotationY(yaw);
        float minU = float.MaxValue, maxU = float.MinValue, minV = float.MaxValue, maxV = float.MinValue;
        for (int i = 0; i < model.Bones.Count; i++)
        {
            var m = rest[i] * turn;
            foreach (var p in model.Bones[i].Geometry.Vertices)
            {
                var q = Vector3.Transform(p, m);
                float u = q.X, v = Vector3.Dot(q, up);
                minU = MathF.Min(minU, u); maxU = MathF.Max(maxU, u);
                minV = MathF.Min(minV, v); maxV = MathF.Max(maxV, v);
            }
        }

        float fill = view == SpriteView.Icon ? 0.86f : model.Fill;
        float margin = model.Hovers ? 0.14f : 0.07f;

        // Leave head-room above tall models for crests, flames and the idle bounce
        float frame = MathF.Max((maxU - minU) / fill, (maxV - minV) / MathF.Min(fill, 0.86f - margin));
        float bottom = minV - margin * frame;
        float centerV = view == SpriteView.Icon ? (minV + maxV) / 2f : bottom + frame / 2f;
        var target = new Vector3((minU + maxU) / 2f, 0, 0) + up * centerV;

        f = new SpriteFraming
        {
            Camera = new Camera3D(target - forward * 40f, target, up, frame, CameraProjection.Orthographic),
            Yaw = yaw,
            FeetRow = view == SpriteView.Icon ? 0.5f + (centerV - minV) / frame : 1f - margin,
            WorldPerPixel = frame / size
        };
        Framings[(model.Species, view, size)] = f;
        return f;
    }

    internal static void EnsureUploaded(PokeModel model)
    {
        if (Uploaded.Contains(model)) return;

        // Outline hulls one pixel thick at battle-sprite size
        float thickness = Framing(model, SpriteView.Front, Size).WorldPerPixel * 0.9f;
        foreach (var bone in model.Bones)
        {
            if (bone.Geometry.VertexCount == 0) continue;
            bone.Mesh = bone.Geometry.Upload();
            bone.Outline = bone.Geometry.BuildOutline(thickness, c => PixelCanvas.Mix(c, OutlineInk, 0.78f)).Upload();
            bone.Uploaded = true;
        }
        Uploaded.Add(model);
    }

    /// <summary>Renders one frame of a Pokémon into <paramref name="target"/>. Call outside any other texture mode.</summary>
    public static void Render(RenderContext context, PokeModel model, SpriteView view, PokePose pose, RenderTexture2D target, bool hullOutline = true)
    {
        context.EnsureLoaded();
        EnsureUploaded(model);
        int size = target.Texture.Width;
        var framing = Framing(model, view, size);
        var shaders = context.Shaders;

        Raylib.BeginTextureMode(target);
        Raylib.ClearBackground(new Color(0, 0, 0, 0));
        Rlgl.SetClipPlanes(1.0, 100.0);
        Raylib.BeginMode3D(framing.Camera);

        shaders.SetLighting(Matrix4x4.Identity, Light, framing.Camera.Position, 1f);
        shaders.SetCharacterStyle(shadowStrength: 0f, rimStrength: 0.32f);

        model.BoneTransforms(pose, BoneMatrices);
        var turn = Matrix4x4.CreateRotationY(framing.Yaw);
        for (int i = 0; i < model.Bones.Count; i++)
        {
            var bone = model.Bones[i];
            if (bone.Uploaded) Raylib.DrawMesh(bone.Mesh, context.Toon, Matrix4x4.Transpose(BoneMatrices[i] * turn));
        }
        if (hullOutline)
        {
            for (int i = 0; i < model.Bones.Count; i++)
            {
                var bone = model.Bones[i];
                if (bone.Uploaded) Raylib.DrawMesh(bone.Outline, context.Outline, Matrix4x4.Transpose(BoneMatrices[i] * turn));
            }
        }

        Raylib.EndMode3D();
        Raylib.EndTextureMode();
        Rlgl.SetClipPlanes(0.01, 1000.0);
    }


    /// <summary>
    /// Outline hulls for drawing the model straight into the 3D battle scene: about half a sprite pixel thick
    /// and tinted from the surface colour instead of near-black.
    /// </summary>
    public static void EnsureSceneOutline(PokeModel model)
    {
        EnsureUploaded(model);
        float thickness = Framing(model, SpriteView.Front, Size).WorldPerPixel * 0.55f;
        foreach (var bone in model.Bones)
        {
            if (!bone.Uploaded || bone.SceneOutlineUploaded) continue;
            bone.SceneOutline = bone.Geometry.BuildOutline(thickness, c => PixelCanvas.Mix(c, new Color(40, 30, 56, 255), 0.5f)).Upload();
            bone.SceneOutlineUploaded = true;
        }
    }

    // ------------------------------------------------------------------ static sprites for menus

    /// <summary>Name under which the generic stand-in model is baked, for species without a sprite.</summary>
    public const string Fallback = "?";

    /// <summary>Bakes the front, back and icon sprite of every species. Call once, outside any texture mode.</summary>
    public static void BakeAll(RenderContext context, IEnumerable<string> species)
    {
        var big = Raylib.LoadRenderTexture(Size, Size);
        var small = Raylib.LoadRenderTexture(IconSize, IconSize);
        foreach (var name in new List<string>(species) { Fallback })
        {
            var model = PokemonModels.Get(name);
            Baked[Key(name, SpriteView.Front)] = Bake(context, model, SpriteView.Front, big, hull: true);
            Baked[Key(name, SpriteView.Back)] = Bake(context, model, SpriteView.Back, big, hull: true);
            Baked[Key(name, SpriteView.Icon)] = Bake(context, model, SpriteView.Icon, small, hull: false);
        }
        Raylib.UnloadRenderTexture(big);
        Raylib.UnloadRenderTexture(small);
    }

    private static string Key(string name, SpriteView view) => $"{name}|{view}";

    private static Texture2D Bake(RenderContext context, PokeModel model, SpriteView view, RenderTexture2D target, bool hull)
    {
        Render(context, model, view, new PokePose { Time = 0.35f }, target, hull);
        var image = Raylib.LoadImageFromTexture(target.Texture);
        Raylib.ImageFlipVertical(ref image);
        var canvas = PixelCanvas.FromImage(image);
        Raylib.UnloadImage(image);
        canvas.OutlinePass(innerSeams: false);
        return canvas.ToTexture();
    }

    /// <summary>A baked sprite for the menus (null if it hasn't been baked).</summary>
    public static Texture2D? GetBaked(string name, SpriteView view) =>
        Baked.TryGetValue(Key(name, view), out var tex) ? tex : null;
}

