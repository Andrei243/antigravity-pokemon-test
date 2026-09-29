using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Renders the overworld in 3D the way Pokémon Platinum does: 3D terrain and buildings under Platinum's field
/// camera, with characters as upright pixel-art sprites. The scene is drawn into a supersampled render target
/// that is filtered down when composited, which smooths polygon edges without blurring the pixel art.
/// </summary>
public sealed class WorldRenderer
{
    private const int SuperSample = 2;

    // Characters are 24 art pixels tall; the bottom two rows are empty space below the feet
    private const float SpriteRows = 1.5f;
    private const float SpriteFootGap = 2f / 24f;

    private readonly int width;
    private readonly int height;
    private RenderTexture2D target;
    private Shader cutoutShader;
    private bool loaded;
    private readonly Dictionary<string, MapScene> scenes = new(StringComparer.OrdinalIgnoreCase);

    public WorldRenderer(int width, int height)
    {
        this.width = width;
        this.height = height;
    }

    private const string VertexShader = @"#version 330
in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec4 vertexColor;
uniform mat4 mvp;
out vec2 fragTexCoord;
out vec4 fragColor;
void main()
{
    fragTexCoord = vertexTexCoord;
    fragColor = vertexColor;
    gl_Position = mvp * vec4(vertexPosition, 1.0);
}";

    // Pixel art is either fully opaque or fully transparent, so transparent texels are discarded instead of
    // blended. That keeps depth testing correct for sprites, tall grass and decals in any draw order.
    private const string FragmentShader = @"#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
uniform sampler2D texture0;
uniform vec4 colDiffuse;
out vec4 finalColor;
void main()
{
    vec4 texel = texture(texture0, fragTexCoord);
    if (texel.a < 0.5) discard;
    finalColor = vec4(texel.rgb, 1.0) * colDiffuse * fragColor;
}";

    private void EnsureLoaded()
    {
        if (loaded) return;
        target = Raylib.LoadRenderTexture(width * SuperSample, height * SuperSample);
        Raylib.SetTextureFilter(target.Texture, TextureFilter.Bilinear);
        cutoutShader = Raylib.LoadShaderFromMemory(VertexShader, FragmentShader);
        loaded = true;
    }

    private MapScene GetScene(Map map)
    {
        if (!scenes.TryGetValue(map.Name, out var scene))
        {
            scene = MapScene.Build(map, cutoutShader);
            scenes[map.Name] = scene;
        }
        return scene;
    }

    /// <summary>Renders the map and its characters into the offscreen target. Call outside any other texture mode.</summary>
    public void Render(Map map, Player player)
    {
        EnsureLoaded();
        var scene = GetScene(map);
        float time = (float)Raylib.GetTime();

        float px = player.PixelX / Player.TileSize + 0.5f;
        float pz = player.PixelY / Player.TileSize + 0.5f;
        var camera = BuildCamera(scene, px, pz);

        Raylib.BeginTextureMode(target);
        Raylib.ClearBackground(scene.Background);

        if (scene.Indoors) Rlgl.SetClipPlanes(1.0, 200.0);
        else Rlgl.SetClipPlanes(10.0, 140.0);

        Raylib.BeginMode3D(camera);
        Rlgl.DisableBackfaceCulling();

        scene.DrawStatic();

        Raylib.BeginShaderMode(cutoutShader);
        scene.DrawWater(time);
        Raylib.EndShaderMode();

        DrawShadows(map, px, pz, player.HopHeight / Player.TileSize);

        Raylib.BeginShaderMode(cutoutShader);
        foreach (var npc in map.NPCs)
        {
            if (npc.IsPCTerminal) continue;
            var tex = PixelArtGenerator.GetNpcSprite(npc.NpcType, npc.Facing);
            DrawSprite(scene, tex, new Rectangle(0, 0, tex.Width, tex.Height), npc.GridX + 0.5f, npc.GridY + 0.5f, 0f);
        }

        var sheet = PixelArtGenerator.GetPlayerSpriteSheet();
        int fw = PixelArtGenerator.CharacterFrameWidth, fh = PixelArtGenerator.CharacterFrameHeight;
        var frame = new Rectangle(player.AnimFrame * fw, (int)player.Facing * fh, fw, fh);
        DrawSprite(scene, sheet, frame, px, pz, player.HopHeight / Player.TileSize * scene.VS);

        // "!" bubble over trainers who have spotted the player, just above the head
        var bubble = SceneTextures.Exclamation;
        foreach (var npc in map.NPCs)
        {
            if (npc.HasSpottedPlayer && npc.ExclamationTimer > 0f)
            {
                float y0 = (SpriteRows - SpriteFootGap * SpriteRows + 0.1f) * scene.VS;
                DrawUpright(bubble, new Rectangle(0, 0, bubble.Width, bubble.Height), npc.GridX + 0.5f, npc.GridY + 0.55f, y0, 0.7f, 0.7f * scene.VS);
            }
        }
        Raylib.EndShaderMode();

        Rlgl.EnableBackfaceCulling();
        Raylib.EndMode3D();
        Raylib.EndTextureMode();
        Rlgl.SetClipPlanes(0.01, 1000.0);
    }

    /// <summary>Draws the last rendered frame into the current target, filtered down to the given size.</summary>
    public void DrawToScreen(int destWidth, int destHeight)
    {
        if (!loaded) return;
        var src = new Rectangle(0, 0, target.Texture.Width, -target.Texture.Height);
        Raylib.DrawTexturePro(target.Texture, src, new Rectangle(0, 0, destWidth, destHeight), Vector2.Zero, 0f, Color.White);
    }

    public void Unload()
    {
        if (!loaded) return;
        Raylib.UnloadRenderTexture(target);
        Raylib.UnloadShader(cutoutShader);
        loaded = false;
    }

    // ------------------------------------------------------------------ camera

    private Camera3D BuildCamera(MapScene scene, float px, float pz)
    {
        float pitch = scene.PitchDeg * MathF.PI / 180f;
        var dir = new Vector3(0, MathF.Sin(pitch), MathF.Cos(pitch));

        if (scene.Indoors)
        {
            var roomTarget = scene.RoomCenter;
            return new Camera3D(roomTarget + dir * MapScene.IndoorDistance, roomTarget, Vector3.UnitY,
                MapScene.IndoorViewHeight, CameraProjection.Orthographic);
        }

        // Aim at the player's middle so they sit near the centre of the screen, then keep the view
        // inside the map plus a few tiles of the surrounding forest
        var t = new Vector3(px, 0.6f * scene.VS, pz);
        var (farOff, nearOff, halfW) = VisibleGround(t.Y);
        const float overscan = 4f;
        var map = scene.Map;
        t.X = ClampOrCenter(t.X, -overscan + halfW, map.Width + overscan - halfW);
        t.Z = ClampOrCenter(t.Z, -overscan - farOff, map.Height + overscan - nearOff);

        return new Camera3D(t + dir * MapScene.OutdoorDistance, t, Vector3.UnitY, MapScene.OutdoorFovYDeg, CameraProjection.Perspective);
    }

    private static float ClampOrCenter(float v, float min, float max) =>
        min > max ? (min + max) / 2f : Math.Clamp(v, min, max);

    /// <summary>
    /// Where the top and bottom screen edges meet the ground (as Z offsets from the target) and the
    /// half-width of the view at the far edge, for the outdoor perspective camera.
    /// </summary>
    private (float FarOffset, float NearOffset, float HalfWidth) VisibleGround(float targetHeight)
    {
        float pitch = MapScene.OutdoorPitchDeg * MathF.PI / 180f;
        float half = MapScene.OutdoorFovYDeg / 2f * MathF.PI / 180f;
        float d = MapScene.OutdoorDistance;
        float camHeight = targetHeight + d * MathF.Sin(pitch);
        float camBack = d * MathF.Cos(pitch);

        float farAngle = pitch - half, nearAngle = pitch + half;
        float farOffset = camBack - camHeight / MathF.Tan(farAngle);
        float nearOffset = camBack - camHeight / MathF.Tan(nearAngle);
        float farDistance = camHeight / MathF.Sin(farAngle) * MathF.Cos(half);
        float halfWidth = farDistance * MathF.Tan(half) * width / height;
        return (farOffset, nearOffset, halfWidth);
    }

    // ------------------------------------------------------------------ characters

    /// <summary>
    /// Characters stand upright on their tile. Their height is divided by cos(pitch) so the sprite keeps its
    /// true proportions on screen, and upright quads never lean into walls behind them.
    /// </summary>
    private static void DrawSprite(MapScene scene, Texture2D tex, Rectangle src, float cx, float cz, float lift)
    {
        float h = SpriteRows * scene.VS;
        DrawUpright(tex, src, cx, cz, lift - SpriteFootGap * h, 1f, h);
    }

    /// <summary>An upright textured quad facing the camera's direction, centred on x and standing at y0.</summary>
    private static void DrawUpright(Texture2D tex, Rectangle src, float cx, float cz, float y0, float w, float h)
    {
        float y1 = y0 + h;
        float x0 = cx - w / 2f, x1 = cx + w / 2f;
        float u0 = src.X / tex.Width, u1 = (src.X + src.Width) / tex.Width;
        float v0 = src.Y / tex.Height, v1 = (src.Y + src.Height) / tex.Height;

        Rlgl.CheckRenderBatchLimit(4);
        Rlgl.SetTexture(tex.Id);
        Rlgl.Begin(DrawMode.Quads);
        Rlgl.Color4ub(255, 255, 255, 255);
        Rlgl.TexCoord2f(u0, v1); Rlgl.Vertex3f(x0, y0, cz);
        Rlgl.TexCoord2f(u1, v1); Rlgl.Vertex3f(x1, y0, cz);
        Rlgl.TexCoord2f(u1, v0); Rlgl.Vertex3f(x1, y1, cz);
        Rlgl.TexCoord2f(u0, v0); Rlgl.Vertex3f(x0, y1, cz);
        Rlgl.End();
        Rlgl.SetTexture(0);
    }

    /// <summary>Soft blob shadows under every character, alpha-blended without writing depth.</summary>
    private static void DrawShadows(Map map, float px, float pz, float hopTiles)
    {
        var tex = SceneTextures.ShadowBlob;
        Rlgl.DisableDepthMask();

        void Blob(float cx, float cz, float scale)
        {
            float rx = 0.42f * scale, rz = 0.3f * scale, y = 0.015f;
            Rlgl.CheckRenderBatchLimit(4);
            Rlgl.SetTexture(tex.Id);
            Rlgl.Begin(DrawMode.Quads);
            Rlgl.Color4ub(255, 255, 255, 255);
            Rlgl.TexCoord2f(0, 1); Rlgl.Vertex3f(cx - rx, y, cz + rz);
            Rlgl.TexCoord2f(1, 1); Rlgl.Vertex3f(cx + rx, y, cz + rz);
            Rlgl.TexCoord2f(1, 0); Rlgl.Vertex3f(cx + rx, y, cz - rz);
            Rlgl.TexCoord2f(0, 0); Rlgl.Vertex3f(cx - rx, y, cz - rz);
            Rlgl.End();
        }

        foreach (var npc in map.NPCs)
        {
            if (!npc.IsPCTerminal) Blob(npc.GridX + 0.5f, npc.GridY + 0.52f, 1f);
        }
        Blob(px, pz + 0.02f, 1f - Math.Clamp(hopTiles, 0f, 0.5f));

        Rlgl.SetTexture(0);
        Rlgl.DrawRenderBatchActive();
        Rlgl.EnableDepthMask();
    }
}
