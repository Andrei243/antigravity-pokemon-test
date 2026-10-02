using System;
using System.Numerics;
using PokemonPlatinumEngine.Data;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Look-dev: renders a Pokémon's 3D model on its own under a studio light, as battles show it (decals, outline,
/// materials). The harness uses it for turntables, clip strips and eye close-ups. Call outside any texture mode.
/// </summary>
internal static class PokemonStudio
{
    private static readonly SceneLighting Light = new(Vector3.Normalize(new Vector3(-0.5f, 0.8f, 0.55f)),
        new Vector3(0.52f, 0.48f, 0.4f), new Vector3(0.56f, 0.6f, 0.72f), new Vector3(0.48f, 0.47f, 0.42f));

    /// <summary>A species, or "sample Serpent" (and so on) for a body plan's sample model.</summary>
    private static PokeModel Model(string name) =>
        name.StartsWith("sample ", StringComparison.OrdinalIgnoreCase)
            ? PokemonModels.Sample(Enum.Parse<BodyPlan>(name.Substring(7), true))
            : PokemonModels.Get(name);

    /// <summary>The pose for one frame of a clip: "idle", "physical", "special", "status", "hit", "faint" or "entry" at progress <paramref name="t"/>.</summary>
    private static PokePose PoseOf(string clip, float t, bool blink = false)
    {
        var pose = new PokePose { Time = 0.4f, Blink = blink ? 1f : 0f };
        switch (clip.ToLowerInvariant())
        {
            case "idle": pose.Time = t; break;
            case "physical": pose.Attack = t; pose.Kind = MoveCategory.Physical; break;
            case "special": pose.Attack = t; pose.Kind = MoveCategory.Special; break;
            case "status": pose.Attack = t; pose.Kind = MoveCategory.Status; break;
            case "hit": pose.Hurt = t; break;
            case "faint": pose.Faint = t; break;
            case "entry": pose.Entry = t; break;
        }
        return pose;
    }

    /// <summary>
    /// One view per frame, side by side, each <paramref name="w"/> × <paramref name="h"/> pixels: the model turned by
    /// <paramref name="yaws"/>[i] in <paramref name="clip"/> at progress <paramref name="times"/>[i]. The view frames
    /// <paramref name="zoom"/> times the model's largest dimension, centred at <paramref name="lookY"/> of its height.
    /// </summary>
    public static Image Strip(RenderContext context, string species, string clip, float[] times, float[] yaws, int w, int h,
        float zoom = 1.5f, float lookY = 0.5f, bool blink = false)
    {
        context.EnsureLoaded();
        var model = Model(species);
        var shaders = context.Shaders;
        int frames = Math.Max(times.Length, yaws.Length);
        var sheet = Raylib.GenImageColor(w * frames, h, new Color(206, 218, 232, 255));
        var target = Raylib.LoadRenderTexture(w, h);

        // Framed by the model's largest dimension, so long and wide models fit from every side
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var p in model.Mesh.Positions)
        {
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }
        float span = Math.Max(max.Y, Math.Max(max.X - min.X, max.Z - min.Z) * 0.85f);

        for (int i = 0; i < frames; i++)
        {
            var look = new Vector3(0, lookY * model.Height, 0);
            float pitch = 10f * MathF.PI / 180f;
            var camera = new Camera3D(look + new Vector3(0, MathF.Sin(pitch), MathF.Cos(pitch)) * 8f, look, Vector3.UnitY, zoom * span, CameraProjection.Orthographic);
            var root = Matrix4x4.CreateRotationY(yaws[Math.Min(i, yaws.Length - 1)]);
            var pose = PoseOf(clip, times[Math.Min(i, times.Length - 1)], blink);

            Raylib.BeginTextureMode(target);
            Raylib.ClearBackground(new Color(206, 218, 232, 255));
            Rlgl.SetClipPlanes(0.5, 30.0);
            Raylib.BeginMode3D(camera);
            shaders.SetLighting(Matrix4x4.Identity, Light, camera.Position, 1f);
            shaders.SetCharacterStyle(shadowStrength: 0f, rimStrength: 0.3f);
            shaders.SetStudio();
            Rlgl.DisableBackfaceCulling();
            PokemonRenderer.Draw(context, model, pose, root, CharacterPass.Color);
            PokemonRenderer.Draw(context, model, pose, root, CharacterPass.Outline);
            Rlgl.EnableBackfaceCulling();
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
