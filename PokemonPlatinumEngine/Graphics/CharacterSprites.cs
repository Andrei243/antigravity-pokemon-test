using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    private static Mesh quad, wallQuad;
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

        // The same quad lit as a building's front wall is, for cards that are part of one (an open door)
        var w = new MeshBuilder();
        w.Quad(new(-0.5f, 0, 0), new(0.5f, 0, 0), new(0.5f, 1, 0), new(-0.5f, 1, 0), new(0, 1), new(1, 1), new(1, 0), new(0, 0),
            Color.White, KitBuilder.FrontNormal);
        wallQuad = w.Upload();
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

    /// <summary>Frees every frame baked from this rig (<see cref="CharacterModels.Trim"/>): a rig built again is another rig, whose frames are baked afresh.</summary>
    public static unsafe void Forget(CharacterRig rig)
    {
        foreach (var key in Cache.Keys.Where(k => k.Rig == rig).ToList())
        {
            var baked = Cache[key];
            Cache.Remove(key);
            Raylib.UnloadTexture(baked.Texture);
            // Never UnloadMaterial: it would free the shared shaders the materials point at
            Raylib.MemFree(baked.Color.Maps);
            Raylib.MemFree(baked.Depth.Maps);
        }
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
        FrameProfiler.Count(2);
    }

    /// <summary>
    /// A character's reflection in a puddle (style guide, "Puddles"): the baked sprite upside down on the ground
    /// under the feet, as long as it looks tall (its height over the sine of the camera's pitch), tinted and fading
    /// to nothing, and cut off <paramref name="reach"/> tiles south of the feet, where the puddle ends. Drawn with
    /// rlgl and the default shader inside the scene pass, as the blob shadows are; given the field's camera, each
    /// corner on raised ground is moved where the scenery's shader puts the ground (<see cref="WorldRenderer.Straighten"/>).
    /// </summary>
    public static void DrawReflection(CharacterRig rig, CharacterPose pose, float yaw, Vector3 feet, float pitchDeg, float reach, Camera3D? straight)
    {
        if (!Cache.TryGetValue(KeyOf(rig, pose, yaw), out var baked)) return;
        float sin = MathF.Sin(pitchDeg * MathF.PI / 180f);
        float length = FrameH / sin;
        float shown = MathF.Min(length, reach);
        if (shown <= 1f / TexelsPerUnit) return;

        // The feet stand a little above the sprite's bottom row (DrawBillboard's margin): the mirror starts at them
        float footV = 1f - 0.05f / FrameH;
        float endV = MathF.Max(0f, footV - shown * sin / FrameH);
        float x0 = WorldRenderer.SnapToTexel(feet.X) - FrameW / 2f, x1 = x0 + FrameW;
        float z0 = WorldRenderer.SnapToTexel(feet.Z), z1 = z0 + MathF.Round(shown * TexelsPerUnit) / TexelsPerUnit;
        float y = feet.Y + 0.012f;
        byte near = 115, far = (byte)(near * Math.Clamp(1f - shown / length, 0f, 1f));

        float X(float x, float z) => straight is { } cam && feet.Y != 0f ? WorldRenderer.Straighten(cam, pitchDeg, x, y, z) : x;
        Rlgl.CheckRenderBatchLimit(4);
        Rlgl.SetTexture(baked.Texture.Id);
        Rlgl.Begin(DrawMode.Quads);
        Rlgl.Color4ub(150, 170, 196, near);
        Rlgl.TexCoord2f(0, footV); Rlgl.Vertex3f(X(x0, z0), y, z0);
        Rlgl.Color4ub(150, 170, 196, far);
        Rlgl.TexCoord2f(0, endV); Rlgl.Vertex3f(X(x0, z1), y, z1);
        Rlgl.TexCoord2f(1, endV); Rlgl.Vertex3f(X(x1, z1), y, z1);
        Rlgl.Color4ub(150, 170, 196, near);
        Rlgl.TexCoord2f(1, footV); Rlgl.Vertex3f(X(x1, z0), y, z0);
        Rlgl.End();
        FrameProfiler.Count(2);
    }

    /// <summary>
    /// A character's field sprite standing still and facing the camera, for the interface to show large (the
    /// Trainer Card, the name entry). Bakes it if it isn't baked yet, so call outside any texture mode.
    /// </summary>
    public static Texture2D Portrait(RenderContext context, string npcType)
    {
        EnsureLoaded();
        var rig = CharacterModels.Get(npcType, context.Shaders);
        return Bake(context, new SpriteKey(rig, 0, SpriteAnim.Idle, 0, false, Expression.Neutral)).Texture;
    }

    // ------------------------------------------------------------------ painted cards

    /// <summary>A sprite painted by hand that stands in the field the way a character's does: lit, shadowed, drawn upright.</summary>
    internal sealed record Card(Texture2D Texture, Material Color, Material Depth, float Width, float Height, bool Wall = false);

    /// <summary>
    /// Makes a card of a piece of pixel art, at the field's 32 texels to the tile. A card that is
    /// <paramref name="partOfAWall"/> is shaded as the scenery is, so it matches the wall it lies on and its
    /// marked glass lights up after dark; any other is shaded as the people are.
    /// </summary>
    public static Card MakeCard(RenderContext context, PixelCanvas art, bool partOfAWall = false)
    {
        EnsureLoaded();
        var tex = art.ToTexture();
        Raylib.SetTextureWrap(tex, TextureWrap.Clamp);
        var shader = partOfAWall ? context.Shaders.World : context.Shaders.Sprite;
        return new Card(tex, RenderContext.MaterialFor(shader, tex), RenderContext.MaterialFor(context.Shaders.Depth, tex),
            art.Width / (float)TexelsPerUnit, art.Height / (float)TexelsPerUnit, partOfAWall);
    }

    /// <summary>Stands a card with the middle of its foot on a point of the ground.</summary>
    public static void DrawCard(Card card, Vector3 foot, float vs, CharacterPass pass)
    {
        if (pass == CharacterPass.Outline) return;
        var at = new Vector3(WorldRenderer.SnapToTexel(foot.X), foot.Y, WorldRenderer.SnapToTexel(foot.Z));
        var m = Matrix4x4.CreateScale(card.Width, card.Height * vs, 1f) * Matrix4x4.CreateTranslation(at);
        Raylib.DrawMesh(card.Wall ? wallQuad : quad, pass == CharacterPass.Depth ? card.Depth : card.Color, Matrix4x4.Transpose(m));
        FrameProfiler.Count(2);
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
