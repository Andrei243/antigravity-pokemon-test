using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Renders the field in 3D with Pokémon Platinum's camera: lit, shadowed terrain, buildings and rooms, with the
/// characters as upright pixel-art sprites. Each frame renders a shadow map from the sun, then the scene into
/// a supersampled target, which is composited with a tilt-shift and colour-grading pass.
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
    private readonly FieldShaders shaders = new();
    private readonly ShadowMap shadowMap = new();
    private bool loaded;
    private bool lastWasIndoors;
    private readonly Dictionary<string, MapScene> scenes = new(StringComparer.OrdinalIgnoreCase);

    public WorldRenderer(int width, int height)
    {
        this.width = width;
        this.height = height;
    }

    private void EnsureLoaded()
    {
        if (loaded) return;
        target = Raylib.LoadRenderTexture(width * SuperSample, height * SuperSample);
        Raylib.SetTextureFilter(target.Texture, TextureFilter.Bilinear);
        shaders.Load();
        shadowMap.Load();
        loaded = true;
    }

    private MapScene GetScene(Map map)
    {
        if (!scenes.TryGetValue(map.Name, out var scene))
        {
            scene = MapScene.Build(map, shaders);
            scenes[map.Name] = scene;
        }
        return scene;
    }

    /// <summary>Renders the map and its characters into the offscreen target. Call outside any other texture mode.</summary>
    public void Render(Map map, Player player)
    {
        EnsureLoaded();
        var scene = GetScene(map);
        lastWasIndoors = scene.Indoors;
        var light = scene.Lighting;

        float px = player.PixelX / Player.TileSize + 0.5f;
        float pz = player.PixelY / Player.TileSize + 0.5f;
        float lift = player.HopHeight / Player.TileSize * scene.VS;
        var camera = BuildCamera(scene, px, pz);
        shaders.SetTime((float)Raylib.GetTime());

        // 1. Shadow map: depth of everything that casts shadows, seen from the sun
        var focus = scene.Indoors ? scene.RoomCenter with { Y = 0 } : new Vector3(camera.Target.X, 0, camera.Target.Z - 2f);
        var lightCamera = ShadowMap.LightCamera(focus, light.SunDirection, scene.Indoors ? 18f : 40f);

        Raylib.BeginTextureMode(shadowMap.Target);
        Raylib.ClearBackground(Color.White);
        Rlgl.SetClipPlanes(1.0, 200.0);
        Raylib.BeginMode3D(lightCamera);
        var lightView = Rlgl.GetMatrixModelview();
        var lightProjection = Rlgl.GetMatrixProjection();
        Rlgl.DisableBackfaceCulling();
        scene.DrawDepth();
        Raylib.BeginShaderMode(shaders.Depth);
        DrawCharacters(scene, map, player, px, pz, lift, 1f / scene.VS);
        Raylib.EndShaderMode();
        Rlgl.EnableBackfaceCulling();
        Raylib.EndMode3D();
        Raylib.EndTextureMode();

        shaders.SetLighting(Raymath.MatrixMultiply(lightView, lightProjection), light, camera.Position, shadowMap.Texel);

        // 2. The scene itself, sampling the shadow map
        Raylib.BeginTextureMode(target);
        Raylib.ClearBackground(scene.Background);
        if (scene.Indoors) Rlgl.SetClipPlanes(1.0, 100.0);
        else Rlgl.SetClipPlanes(10.0, 140.0);

        Raylib.BeginMode3D(camera);
        Rlgl.DisableBackfaceCulling();
        Rlgl.ActiveTextureSlot(FieldShaders.ShadowMapSlot);
        Rlgl.EnableTexture(shadowMap.DepthTextureId);
        Rlgl.ActiveTextureSlot(0);

        scene.Draw();
        DrawContactShadows(map, px, pz, lift);

        Raylib.BeginShaderMode(shaders.Sprite);
        DrawCharacters(scene, map, player, px, pz, lift, 1f);
        DrawSpottedBubbles(scene, map);
        Raylib.EndShaderMode();

        Rlgl.EnableBackfaceCulling();
        Raylib.EndMode3D();
        Raylib.EndTextureMode();

        Rlgl.ActiveTextureSlot(FieldShaders.ShadowMapSlot);
        Rlgl.DisableTexture();
        Rlgl.ActiveTextureSlot(0);
        Rlgl.SetClipPlanes(0.01, 1000.0);
    }

    /// <summary>Composites the last rendered frame into the current target with tilt-shift blur and grading.</summary>
    public void DrawToScreen(int destWidth, int destHeight)
    {
        if (!loaded) return;
        shaders.SetPost(new Vector2(1f / target.Texture.Width, 1f / target.Texture.Height), lastWasIndoors ? 3.5f : 6f);
        Raylib.BeginShaderMode(shaders.Post);
        var src = new Rectangle(0, 0, target.Texture.Width, -target.Texture.Height);
        Raylib.DrawTexturePro(target.Texture, src, new Rectangle(0, 0, destWidth, destHeight), Vector2.Zero, 0f, Color.White);
        Raylib.EndShaderMode();
    }

    public void Unload()
    {
        if (!loaded) return;
        Raylib.UnloadRenderTexture(target);
        shaders.Unload();
        shadowMap.Unload();
        loaded = false;
    }

    // ------------------------------------------------------------------ camera

    private Camera3D BuildCamera(MapScene scene, float px, float pz)
    {
        float pitch = scene.PitchDeg * MathF.PI / 180f;
        var dir = new Vector3(0, MathF.Sin(pitch), MathF.Cos(pitch));

        if (scene.Indoors)
        {
            // Deeper rooms pull the camera back a little so the whole room stays in frame
            var roomTarget = scene.RoomCenter;
            float distance = MapScene.IndoorDistance * Math.Max(1f, (scene.Map.Height - 1) / 8f);
            return new Camera3D(roomTarget + dir * distance, roomTarget, Vector3.UnitY,
                MapScene.IndoorFovYDeg, CameraProjection.Perspective);
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

    /// <param name="heightScale">
    /// 1 for the visible sprites. The shadow pass uses 1 / VS: characters are stretched upward to look right
    /// from the tilted camera, and casting shadows from that stretched height would make them far too long.
    /// </param>
    private static void DrawCharacters(MapScene scene, Map map, Player player, float px, float pz, float lift, float heightScale)
    {
        foreach (var npc in map.NPCs)
        {
            if (npc.IsPCTerminal) continue;
            var tex = PixelArtGenerator.GetNpcSprite(npc.NpcType, npc.Facing);
            DrawSprite(scene, tex, new Rectangle(0, 0, tex.Width, tex.Height), npc.GridX + 0.5f, npc.GridY + 0.5f, 0f, heightScale);
        }

        var sheet = PixelArtGenerator.GetPlayerSpriteSheet();
        int fw = PixelArtGenerator.CharacterFrameWidth, fh = PixelArtGenerator.CharacterFrameHeight;
        var frame = new Rectangle(player.AnimFrame * fw, (int)player.Facing * fh, fw, fh);
        DrawSprite(scene, sheet, frame, px, pz, lift, heightScale);
    }

    /// <summary>"!" bubble over trainers who have spotted the player, just above the head.</summary>
    private static void DrawSpottedBubbles(MapScene scene, Map map)
    {
        var bubble = SceneTextures.Exclamation;
        foreach (var npc in map.NPCs)
        {
            if (npc.HasSpottedPlayer && npc.ExclamationTimer > 0f)
            {
                float y0 = (SpriteRows - SpriteFootGap * SpriteRows + 0.1f) * scene.VS;
                DrawUpright(bubble, new Rectangle(0, 0, bubble.Width, bubble.Height), npc.GridX + 0.5f, npc.GridY + 0.55f, y0, 0.7f, 0.7f * scene.VS);
            }
        }
    }

    /// <summary>
    /// Characters stand upright on their tile. Their height is divided by cos(pitch) so the sprite keeps its
    /// true proportions on screen, and upright quads never lean into walls behind them.
    /// </summary>
    private static void DrawSprite(MapScene scene, Texture2D tex, Rectangle src, float cx, float cz, float lift, float heightScale)
    {
        float h = SpriteRows * scene.VS * heightScale;
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
        Rlgl.Normal3f(0, 0, 1);
        Rlgl.TexCoord2f(u0, v1); Rlgl.Vertex3f(x0, y0, cz);
        Rlgl.TexCoord2f(u1, v1); Rlgl.Vertex3f(x1, y0, cz);
        Rlgl.TexCoord2f(u1, v0); Rlgl.Vertex3f(x1, y1, cz);
        Rlgl.TexCoord2f(u0, v0); Rlgl.Vertex3f(x0, y1, cz);
        Rlgl.End();
        Rlgl.SetTexture(0);
    }

    /// <summary>
    /// A faint dark patch right under each character's feet. The shadow map casts their real shadow; this keeps
    /// them grounded when the sun is high or they are standing in shade.
    /// </summary>
    private static void DrawContactShadows(Map map, float px, float pz, float lift)
    {
        var tex = SceneTextures.ShadowBlob;
        Rlgl.DisableDepthMask();

        void Blob(float cx, float cz, float scale)
        {
            float rx = 0.36f * scale, rz = 0.24f * scale, y = 0.015f;
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
        Blob(px, pz + 0.02f, 1f - Math.Clamp(lift * 0.3f, 0f, 0.5f));

        Rlgl.SetTexture(0);
        Rlgl.DrawRenderBatchActive();
        Rlgl.EnableDepthMask();
    }
}
