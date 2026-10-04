using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The title screen's 3D scene: Giratina hovering in a dark violet void among drifting stones, lit from the upper
/// left with a strong rim light. It is drawn like a battle (cel shading, tinted outlines) into the shared scene
/// target and composited with bloom and a heavy vignette.
/// </summary>
internal sealed class TitleScene
{
    private const float Near = 0.5f, Far = 120f, FovY = 32f;

    private static readonly SceneLighting Light = new(Vector3.Normalize(new Vector3(-0.5f, 0.6f, 0.62f)),
        new Vector3(0.66f, 0.56f, 0.6f), new Vector3(0.3f, 0.24f, 0.46f), new Vector3(0.36f, 0.14f, 0.2f));

    private static readonly PostSettings Post = new(0f, 0f, 0.5f, 0.5f, 0.6f, 1.1f, 1.08f,
        new Vector3(0.9f, 0.86f, 1.15f), new Vector3(1.06f, 1.0f, 0.96f), 0.42f, AoStrength: 0.5f);

    private static readonly Color Silhouette = new(10, 6, 22, 255);

    // Giratina stays a shadow: fully revealed it is still mostly dark, picked out by the glow behind and its ink lines
    private const float Lit = 0.11f;

    private Mesh stone;
    private bool stoneLoaded;

    /// <summary>Renders the scene offscreen. Call outside any other texture mode.</summary>
    /// <param name="reveal">0 hides Giratina in the dark; 1 shows it as a shadow against the glow behind it.</param>
    /// <param name="offset">How far the view is shifted right (world units), which moves Giratina left on screen.</param>
    public void Render(RenderContext context, float time, float reveal, float offset)
    {
        context.EnsureLoaded();
        var shaders = context.Shaders;
        var model = PokemonModels.Get("Giratina");
        EnsureStone();

        // Giratina is sized so its sprite frame is 4.6 units tall, and seen from slightly below
        var framing = PokemonSprites.Framing(model, SpriteView.Front, PokemonSprites.Size);
        float scale = 4.6f / (framing.WorldPerPixel * PokemonSprites.Size);
        float sway = MathF.Sin(time * 0.22f) * 0.1f;
        var target = new Vector3(offset, 2.23f, 0);
        var camera = new Camera3D(target + new Vector3(MathF.Sin(sway) * 11.5f, -0.9f, MathF.Cos(sway) * 11.5f), target, Vector3.UnitY,
            FovY, CameraProjection.Perspective);

        shaders.SetTime(time);
        shaders.SetWind(0f);
        shaders.SetStudio();
        shaders.SetLighting(Matrix4x4.Identity, Light, camera.Position, 1f);
        shaders.SetCharacterStyle(shadowStrength: 0f, rimStrength: 1.3f * reveal);

        var target2D = context.Target;
        int tw = target2D.Texture.Width, th = target2D.Texture.Height;
        Raylib.BeginTextureMode(target2D);
        PaintVoid(time, reveal, tw, th, offset);

        Rlgl.SetClipPlanes(Near, Far);
        Raylib.BeginMode3D(camera);
        Rlgl.DisableBackfaceCulling();

        // Stones hanging in the void on either side, bobbing and turning slowly; dim so they stay in the background
        shaders.SetFlash(Silhouette, 0.45f + 0.5f * (1f - reveal));
        for (int i = 0; i < 10; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            var p = new Vector3(side * (5.6f + i * 53 % 7 * 1.15f), 0.2f + i * 37 % 11 * 0.6f + MathF.Sin(time * 0.4f + i) * 0.25f, -2f - i * 29 % 9);
            float s = 0.5f + i * 53 % 7 * 0.16f;
            var m = Matrix4x4.CreateScale(s * 1.6f, s * 0.8f, s * 1.2f) * Matrix4x4.CreateRotationY(i * 1.7f + time * 0.05f) *
                Matrix4x4.CreateRotationZ(0.3f * MathF.Sin(i)) * Matrix4x4.CreateTranslation(p);
            Raylib.DrawMesh(stone, context.Toon, Matrix4x4.Transpose(m));
        }

        var pose = new PokePose { Time = time, Blink = time % 4.3f < 0.12f ? 1f : 0f };
        var root = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateRotationY(framing.Yaw + 0.12f * MathF.Sin(time * 0.3f)) *
            Matrix4x4.CreateTranslation(0, 0.25f * MathF.Sin(time * 0.7f), 0);

        shaders.SetFlash(Silhouette, 1f - Lit * reveal);
        PokemonRenderer.Draw(context, model, pose, root, CharacterPass.Color);
        shaders.SetFlash(default, 0f);

        Rlgl.EnableBackfaceCulling();
        PokemonRenderer.Draw(context, model, pose, root, CharacterPass.Outline);

        Raylib.EndMode3D();
        Raylib.EndTextureMode();
        Rlgl.SetClipPlanes(0.01, 1000.0);
        context.PreparePost(Post, new DepthRange(Near, Far, FovY, (float)context.Width / context.Height), aoRadius: 0.5f);
    }

    /// <summary>The void: near-black violet deepening to a dull crimson below, with slow glows behind Giratina.</summary>
    private static void PaintVoid(float time, float reveal, int tw, int th, float offset)
    {
        Raylib.ClearBackground(new Color(6, 5, 16, 255));
        int split = th * 58 / 100;
        Raylib.DrawRectangleGradientV(0, 0, tw, split, new Color(6, 5, 18, 255), new Color(30, 15, 58, 255));
        Raylib.DrawRectangleGradientV(0, split, tw, th - split, new Color(30, 15, 58, 255), new Color(70, 22, 54, 255));

        var glow = SceneTextures.SoftGlow;
        var src = new Rectangle(0, 0, glow.Width, glow.Height);
        float cx = tw * (0.5f - offset * 0.0855f), cy = th * 0.46f;
        void Glow(float x, float y, float size, Color c) =>
            Raylib.DrawTexturePro(glow, src, new Rectangle(x - size / 2, y - size / 2, size, size), Vector2.Zero, 0f, c);

        Raylib.BeginBlendMode(BlendMode.Additive);
        float pulse = 0.85f + 0.15f * MathF.Sin(time * 0.9f);
        Glow(cx, cy, th * 1.5f, new Color(110, 60, 210, (int)(200 * reveal * pulse)));
        Glow(cx, cy + th * 0.28f, th * 1.1f, new Color(210, 50, 76, (int)(120 * reveal)));
        Glow(cx, cy + th * 0.06f, th * 0.75f, new Color(255, 206, 170, (int)(110 * reveal * pulse)));
        // Slow drifting haze
        for (int i = 0; i < 6; i++)
        {
            float x = tw * ((i * 0.19f + time * 0.006f * (i % 2 == 0 ? 1 : -1)) % 1.2f - 0.1f);
            float y = th * (0.2f + (i * 37 % 10) * 0.07f);
            Glow(x, y, th * (0.6f + i % 3 * 0.25f), new Color(70, 40, 130, 34));
        }
        Raylib.EndBlendMode();
    }

    private void EnsureStone()
    {
        if (stoneLoaded) return;
        var b = new MeshBuilder();
        // A lumpy boulder: a few overlapping ellipsoids, lighter on top
        Color Tone(int k) => PixelCanvas.Mix(new Color(44, 34, 66, 255), new Color(96, 82, 128, 255), k / 9f);
        b.Ellipsoid(Vector3.Zero, new Vector3(1f, 0.7f, 0.9f), Tone, 14, 9);
        b.Ellipsoid(new Vector3(0.55f, 0.2f, 0.1f), new Vector3(0.6f, 0.5f, 0.6f), Tone, 12, 9);
        b.Ellipsoid(new Vector3(-0.5f, -0.1f, -0.2f), new Vector3(0.7f, 0.45f, 0.6f), Tone, 12, 9);
        stone = b.Upload();
        stoneLoaded = true;
    }
}
