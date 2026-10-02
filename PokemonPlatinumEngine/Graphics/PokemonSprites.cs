using System;
using System.Collections.Concurrent;
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
/// games), baked once at start-up, and frames each model the way its sprite does (battles size the 3D models by
/// that frame).
/// </summary>
internal static class PokemonSprites
{
    public const int Size = 128;
    public const int IconSize = 48;

    // Framings are worked out as models are built, which can happen on several threads at once
    private static readonly ConcurrentDictionary<(string, SpriteView, int), SpriteFraming> Framings = new();
    private static readonly Dictionary<string, Texture2D> Baked = new(StringComparer.OrdinalIgnoreCase);

    // A soft studio light from the upper left, independent of the field's sun
    private static readonly SceneLighting Light = new(Vector3.Normalize(new Vector3(-0.55f, 0.7f, 0.55f)),
        new Vector3(0.6f, 0.58f, 0.53f), new Vector3(0.6f, 0.62f, 0.7f), new Vector3(0.42f, 0.4f, 0.38f));

    /// <summary>How a view of the model fills a sprite of <paramref name="size"/> pixels (no GPU calls).</summary>
    public static SpriteFraming Framing(PokeModel model, SpriteView view, int size)
    {
        if (Framings.TryGetValue((model.Species, view, size), out var f)) return f;

        float yaw = view == SpriteView.Back ? 2.55f : -0.5f;
        float pitch = (view == SpriteView.Back ? 20f : 10f) * MathF.PI / 180f;
        var up = new Vector3(0, MathF.Cos(pitch), -MathF.Sin(pitch));
        var forward = new Vector3(0, -MathF.Sin(pitch), -MathF.Cos(pitch));

        // Project the sculpted (rest) pose to find the silhouette's extent in this view
        var turn = Matrix4x4.CreateRotationY(yaw);
        float minU = float.MaxValue, maxU = float.MinValue, minV = float.MaxValue, maxV = float.MinValue;
        foreach (var p in model.Mesh.Positions)
        {
            var q = Vector3.Transform(p, turn);
            float u = q.X, v = Vector3.Dot(q, up);
            minU = MathF.Min(minU, u); maxU = MathF.Max(maxU, u);
            minV = MathF.Min(minV, v); maxV = MathF.Max(maxV, v);
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
        return Framings.GetOrAdd((model.Species, view, size), f);
    }

    /// <summary>Renders one frame of a Pokémon into <paramref name="target"/>. Call outside any other texture mode.</summary>
    /// <param name="flash">Blends the body toward a flat colour by this much (the white of an evolving Pokémon).</param>
    public static void Render(RenderContext context, PokeModel model, SpriteView view, PokePose pose, RenderTexture2D target, bool hullOutline = true,
        (Color Color, float Amount)? flash = null)
    {
        context.EnsureLoaded();
        int size = target.Texture.Width;
        var framing = Framing(model, view, size);
        var shaders = context.Shaders;

        Raylib.BeginTextureMode(target);
        Raylib.ClearBackground(new Color(0, 0, 0, 0));
        Rlgl.SetClipPlanes(1.0, 100.0);
        Raylib.BeginMode3D(framing.Camera);

        shaders.SetLighting(Matrix4x4.Identity, Light, framing.Camera.Position, 1f);
        shaders.SetCharacterStyle(shadowStrength: 0f, rimStrength: 0.32f);
        shaders.SetStudio();
        if (flash is { } f) shaders.SetFlash(f.Color, f.Amount);

        var turn = Matrix4x4.CreateRotationY(framing.Yaw);
        Rlgl.DisableBackfaceCulling();
        PokemonRenderer.Draw(context, model, pose, turn, CharacterPass.Color);
        if (flash != null) shaders.SetFlash(default, 0f);
        if (hullOutline) PokemonRenderer.Draw(context, model, pose, turn, CharacterPass.Outline);
        Rlgl.EnableBackfaceCulling();

        Raylib.EndMode3D();
        Raylib.EndTextureMode();
        Rlgl.SetClipPlanes(0.01, 1000.0);
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

