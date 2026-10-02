using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>Which strip of sprite frames a pose takes its frame from.</summary>
internal enum SpriteAnim { Idle, Walk, Run, Hop, Wave, Surprised, Nod, Cheer }

/// <summary>One baked sprite frame: a character type, facing, strip, frame, and its face.</summary>
internal readonly record struct SpriteKey(CharacterRig Rig, int Facing, SpriteAnim Anim, int Frame, bool Blink, Expression Expression);

/// <summary>
/// HD-2D characters: each 3D rig is rendered once per facing and animation frame into a small pixel-art sprite (at
/// the field's texel density, with a pixel face stamped on and a one-pixel outline), then drawn in the field as an
/// upright billboard that is lit by the scene and casts a shadow. Frames: two breaths standing, eight each for
/// the walk and the run, three for a ledge hop and six for each emote; blinking and expressions are their own
/// frames. A PNG in <c>overrides/sprites/&lt;TYPE&gt;/</c> next to the game replaces any baked frame
/// (<see cref="OverrideName"/>).
/// </summary>
internal static class CharacterSprites
{
    /// <summary>Sprite texels per world unit: the same density as the field's ground and building textures.</summary>
    public const int TexelsPerUnit = 32;

    public const int SpriteW = 40, SpriteH = 58;
    private const float FrameW = SpriteW / (float)TexelsPerUnit, FrameH = SpriteH / (float)TexelsPerUnit;

    // Seen a little from above, like hand-drawn overworld sprites
    private const float ViewPitchDeg = 24f;

    public const int WalkFrames = 8, IdleFrames = 2, HopFrames = 3, EmoteFrames = 6;

    /// <summary>Seconds per idle breath (in and out), as in the idle clip.</summary>
    private const float BreathPeriod = 3.4f;

    public static string OverrideFolder { get; set; } = Path.Combine(AppContext.BaseDirectory, "overrides", "sprites");

    private sealed record Baked(Texture2D Texture, Material Color, Material Depth);

    private static readonly Dictionary<SpriteKey, Baked> Cache = new();
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

    public static int FacingIndex(float yaw)
    {
        int i = (int)MathF.Round(yaw / (MathF.PI / 2f));
        return ((i % 4) + 4) % 4;
    }

    /// <summary>The frame a pose shows (shared by baking and drawing, so they always agree).</summary>
    public static SpriteKey KeyOf(CharacterRig rig, CharacterPose pose, float yaw)
    {
        int facing = FacingIndex(yaw);
        if (rig.Kind == RigKind.Rift)
            return new SpriteKey(rig, facing, SpriteAnim.Idle, (int)MathF.Floor(Fraction(pose.Time / 1.2f) * 6f), false, Expression.Neutral);
        if (rig.Kind != RigKind.Humanoid)
            return new SpriteKey(rig, facing, SpriteAnim.Idle, 0, false, Expression.Neutral);

        var expression = CharacterAnimation.ExpressionOf(pose);
        if (pose.Hop > 0f && pose.Hop < 1f)
            return new SpriteKey(rig, facing, SpriteAnim.Hop, Math.Clamp((int)(pose.Hop * HopFrames), 0, HopFrames - 1), pose.Blink, expression);

        float duration = CharacterAnimation.Duration(pose.Emote);
        if (pose.Emote != Emote.None && pose.EmoteTime < duration)
        {
            int f = Math.Clamp((int)(pose.EmoteTime / duration * EmoteFrames), 0, EmoteFrames - 1);
            var anim = pose.Emote switch { Emote.Wave => SpriteAnim.Wave, Emote.Surprised => SpriteAnim.Surprised, Emote.Nod => SpriteAnim.Nod, _ => SpriteAnim.Cheer };
            return new SpriteKey(rig, facing, anim, f, pose.Blink, expression);
        }

        if (pose.WalkBlend >= 0.5f)
        {
            int f = (int)MathF.Floor(Fraction(pose.Walk) * WalkFrames) % WalkFrames;
            return new SpriteKey(rig, facing, pose.Running ? SpriteAnim.Run : SpriteAnim.Walk, f, pose.Blink, expression);
        }

        int breath = MathF.Sin(pose.Time * MathF.Tau / BreathPeriod) > 0f ? 1 : 0;
        return new SpriteKey(rig, facing, SpriteAnim.Idle, breath, pose.Blink, expression);
    }

    private static float Fraction(float x) => ((x % 1f) + 1f) % 1f;

    /// <summary>The representative pose a frame is baked from: the middle of its slice of the clip.</summary>
    public static CharacterPose BakePose(SpriteKey key)
    {
        var pose = new CharacterPose { Blink = key.Blink, Expression = key.Expression };
        switch (key.Anim)
        {
            case SpriteAnim.Idle:
                // Fully out, or fully in; the Rift's frames step through its spin
                pose.Time = key.Rig.Kind == RigKind.Rift ? (key.Frame + 0.5f) / 6f * 1.2f : (key.Frame == 0 ? 0.75f : 0.25f) * BreathPeriod;
                break;
            case SpriteAnim.Walk:
            case SpriteAnim.Run:
                pose.Walk = (key.Frame + 0.5f) / WalkFrames;
                pose.WalkBlend = 1f;
                pose.Running = key.Anim == SpriteAnim.Run;
                pose.Time = 0.4f;
                break;
            case SpriteAnim.Hop:
                pose.Hop = (key.Frame + 0.5f) / HopFrames;
                pose.Time = 0.4f;
                break;
            default:
                pose.Emote = key.Anim switch { SpriteAnim.Wave => Emote.Wave, SpriteAnim.Surprised => Emote.Surprised, SpriteAnim.Nod => Emote.Nod, _ => Emote.Cheer };
                pose.EmoteTime = (key.Frame + 0.5f) / EmoteFrames * CharacterAnimation.Duration(pose.Emote);
                pose.Time = 0.4f;
                break;
        }
        return pose;
    }

    /// <summary>The file a hand-drawn frame is looked for under (in the character type's folder).</summary>
    public static string OverrideName(SpriteKey key) =>
        $"{(key.Facing switch { 0 => "down", 1 => "right", 2 => "up", _ => "left" })}_{key.Anim.ToString().ToLowerInvariant()}_{key.Frame}" +
        (key.Blink ? "_blink" : "") + (key.Expression != Expression.Neutral ? "_" + key.Expression.ToString().ToLowerInvariant() : "") + ".png";

    /// <summary>Bakes the sprite this pose needs, if it isn't cached yet. Call outside any texture mode.</summary>
    public static void Prepare(RenderContext context, CharacterRig rig, CharacterPose pose, float yaw) => Bake(context, KeyOf(rig, pose, yaw));

    private static Baked Bake(RenderContext context, SpriteKey key)
    {
        if (Cache.TryGetValue(key, out var baked)) return baked;
        var canvas = LoadOverride(key) ?? Render(context, key);
        var tex = canvas.ToTexture();
        Raylib.SetTextureWrap(tex, TextureWrap.Clamp);
        baked = new Baked(tex, RenderContext.MaterialFor(context.Shaders.Sprite, tex), RenderContext.MaterialFor(context.Shaders.Depth, tex));
        Cache[key] = baked;
        return baked;
    }

    private static PixelCanvas? LoadOverride(SpriteKey key)
    {
        string path = Path.Combine(OverrideFolder, key.Rig.Type, OverrideName(key));
        if (!File.Exists(path)) return null;
        var image = Raylib.LoadImage(path);
        try
        {
            if (image.Width != SpriteW || image.Height != SpriteH) return null;
            return PixelCanvas.FromImage(image);
        }
        finally
        {
            Raylib.UnloadImage(image);
        }
    }

    /// <summary>Renders the frame from the 3D rig, stamps the pixel face and outlines it.</summary>
    internal static PixelCanvas Render(RenderContext context, SpriteKey key)
    {
        EnsureLoaded();
        var rig = key.Rig;
        var pose = BakePose(key);
        float yaw = key.Facing * MathF.PI / 2f;
        var root = Matrix4x4.CreateRotationY(yaw);

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
        shaders.SetStudio();
        CharacterRenderer.Draw(context, rig, pose, root, CharacterPass.Color, drawFace: false);
        Raylib.EndMode3D();
        Raylib.EndTextureMode();
        Rlgl.SetClipPlanes(0.01, 1000.0);

        var image = Raylib.LoadImageFromTexture(target.Texture);
        Raylib.ImageFlipVertical(ref image);
        var canvas = PixelCanvas.FromImage(image);
        Raylib.UnloadImage(image);

        if (rig.Kind == RigKind.Humanoid)
        {
            // Where the point between the eyes landed: the rig was just posed for this frame
            var anchor = Vector3.Transform(Skeleton.Transform(rig.Face.Anchor, HumanBones.Head, rig.Skin), Matrix4x4.CreateScale(rig.Scale) * root);
            var d = anchor - look;
            var up = new Vector3(0, MathF.Cos(pitch), -MathF.Sin(pitch));
            var px = new Vector2(SpriteW / 2f + d.X * TexelsPerUnit, SpriteH / 2f - Vector3.Dot(d, up) * TexelsPerUnit);
            CharacterFaces.Stamp(canvas, px, key.Facing, rig.Style, key.Expression, key.Blink);
        }
        canvas.OutlinePass(innerSeams: false);
        return canvas;
    }

    /// <summary>
    /// Draws a prepared sprite as an upright card at <paramref name="feet"/>, stretched by the field's vertical
    /// scale so it reads at its true proportions from the steep camera.
    /// </summary>
    public static void DrawBillboard(RenderContext context, CharacterRig rig, CharacterPose pose, float yaw, Vector3 feet, float vs, CharacterPass pass)
    {
        if (pass == CharacterPass.Outline) return;
        if (!Cache.TryGetValue(KeyOf(rig, pose, yaw), out var baked)) return;

        // The sprite's bottom row is a little below the feet (the frame leaves a margin under the shoes)
        var at = new Vector3(WorldRenderer.SnapToTexel(feet.X), feet.Y, WorldRenderer.SnapToTexel(feet.Z));
        var m = Matrix4x4.CreateScale(FrameW, FrameH * vs, 1f) * Matrix4x4.CreateTranslation(at + new Vector3(0, -0.05f * vs, 0.02f));
        Raylib.DrawMesh(quad, pass == CharacterPass.Depth ? baked.Depth : baked.Color, Matrix4x4.Transpose(m));
    }

    /// <summary>
    /// Every frame of a strip for one facing, side by side, enlarged without filtering (for the harness's sprite
    /// sheets). Bakes what isn't baked yet; call outside any texture mode.
    /// </summary>
    internal static Image Sheet(RenderContext context, string npcType, int facing, SpriteAnim anim, int frames, int scale, bool blink = false, Expression expression = Expression.Neutral)
    {
        var rig = CharacterModels.Get(npcType, context.Shaders);
        var sheet = new PixelCanvas(SpriteW * frames, SpriteH);
        for (int f = 0; f < frames; f++)
        {
            var key = new SpriteKey(rig, facing, anim, f, blink, expression);
            Bake(context, key);
            sheet.Blit(LoadOverride(key) ?? Render(context, key), f * SpriteW, 0);
        }
        return sheet.ToImage(scale);
    }
}
