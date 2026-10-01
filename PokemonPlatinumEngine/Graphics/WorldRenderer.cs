using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Renders the HD-2D field with Pokémon Platinum's camera: pixel-art terrain, buildings and rooms in 3D, and
/// characters as baked pixel sprites, lit by the time of day's light rig. Each frame renders a shadow map from the
/// sun (or moon), then the scene into the scene target, which is composited with depth of field, bloom and grading.
/// </summary>
public sealed class WorldRenderer
{
    private readonly RenderContext context;
    private bool lastWasIndoors;
    private readonly Dictionary<string, MapScene> scenes = new(StringComparer.OrdinalIgnoreCase);

    private FieldShaders shaders => context.Shaders;
    private int width => context.Width;
    private int height => context.Height;

    public WorldRenderer(RenderContext context)
    {
        this.context = context;
    }

    private MapScene GetScene(Map map)
    {
        string key = map.Name;
        if (!scenes.TryGetValue(key, out var scene))
        {
            scene = MapScene.Build(map, shaders);
            scenes[key] = scene;
        }
        return scene;
    }

    /// <summary>Renders the map and its characters into the offscreen target. Call outside any other texture mode.</summary>
    public void Render(Map map, Player player) =>
        RenderScene(map, player, player.PixelX / Player.TileSize + 0.5f, player.PixelY / Player.TileSize + 0.5f, GameClock.Hour);

    /// <summary>
    /// Renders the map without the player, looking at a point (in tiles) at a given hour: the fly-over shots of
    /// the opening. Call outside any other texture mode.
    /// </summary>
    public void RenderCinematic(Map map, float focusX, float focusZ, float hour) => RenderScene(map, null, focusX, focusZ, hour);

    private void RenderScene(Map map, Player? player, float px, float pz, float hour)
    {
        context.EnsureLoaded();
        var scene = GetScene(map);
        lastWasIndoors = scene.Indoors;
        var rig = ArtLook.FieldRig(hour, scene.Indoors);
        var light = rig.Light;

        float time = (float)Raylib.GetTime();
        float lift = player != null ? player.HopHeight / Player.TileSize : 0f;
        var camera = BuildCamera(scene, px, pz);
        shaders.SetTime(time);
        GatherActors(map, player, px, pz, lift, time);
        VerticalScale = scene.VS;
        foreach (var actor in actors) CharacterSprites.Prepare(context, actor.Rig, actor.Pose, actor.Yaw);

        // 1. Shadow map: depth of everything that casts shadows, seen from the sun (or moon)
        var focus = scene.Indoors ? scene.RoomCenter with { Y = 0 } : new Vector3(camera.Target.X, 0, camera.Target.Z - 2f);
        var lightCamera = context.Shadows.LightCamera(focus, light.SunDirection, scene.Indoors ? 18f : 40f);

        Raylib.BeginTextureMode(context.Shadows.Target);
        Raylib.ClearBackground(Color.White);
        Rlgl.SetClipPlanes(1.0, 200.0);
        Raylib.BeginMode3D(lightCamera);
        var lightView = Rlgl.GetMatrixModelview();
        var lightProjection = Rlgl.GetMatrixProjection();
        Rlgl.DisableBackfaceCulling();
        scene.DrawDepth();
        DrawActors(CharacterPass.Depth);
        Rlgl.EnableBackfaceCulling();
        Raylib.EndMode3D();
        Raylib.EndTextureMode();

        shaders.SetLighting(Raymath.MatrixMultiply(lightView, lightProjection), light, camera.Position, context.Shadows.Texel);
        shaders.SetCharacterStyle(shadowStrength: 1f, rimStrength: rig.Rim);
        shaders.SetWorldRamp(0f);
        shaders.SetFog(rig.FogColor, rig.FogAmount, rig.FogNear, rig.FogFar);
        shaders.SetCloudShade(rig.CloudShade);

        // 2. The scene itself, sampling the shadow map
        float near = scene.Indoors ? 1f : 10f, far = scene.Indoors ? 100f : 140f;
        Raylib.BeginTextureMode(context.Target);
        Raylib.ClearBackground(rig.Background);
        Rlgl.SetClipPlanes(near, far);

        Raylib.BeginMode3D(camera);
        Rlgl.DisableBackfaceCulling();
        context.BindShadowMap();

        scene.Draw(rig.WindowGlow);
        scene.DrawLights(rig.WindowGlow);
        DrawContactShadows(map, player != null, px, pz, lift);
        DrawActors(CharacterPass.Color);
        DrawSpottedBubbles(scene, map);

        Rlgl.EnableBackfaceCulling();
        Raylib.EndMode3D();
        Raylib.EndTextureMode();

        context.UnbindShadowMap();
        Rlgl.SetClipPlanes(0.01, 1000.0);
        context.PreparePost(rig.Post, new DepthRange(near, far, camera.FovY, (float)width / height), aoRadius: 0.45f);
    }

    /// <summary>Composites the last rendered frame into the current target with tilt-shift blur and grading.</summary>
    public void DrawToScreen(int destWidth, int destHeight) =>
        context.Composite(new Rectangle(0, 0, destWidth, destHeight));

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

        // Scroll in whole texels of the pixel art, so textures and sprites don't shimmer as the view moves
        t.X = SnapToTexel(t.X);
        t.Z = SnapToTexel(t.Z);

        return new Camera3D(t + dir * MapScene.OutdoorDistance, t, Vector3.UnitY, MapScene.OutdoorFovYDeg, CameraProjection.Perspective);
    }

    internal static float SnapToTexel(float v) => MathF.Round(v * CharacterSprites.TexelsPerUnit) / CharacterSprites.TexelsPerUnit;

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


    private readonly struct Actor
    {
        public readonly CharacterRig Rig;
        public readonly Vector3 Feet;
        public readonly float Yaw;
        public readonly CharacterPose Pose;

        public Actor(CharacterRig rig, Vector3 feet, float yaw, CharacterPose pose)
        {
            Rig = rig;
            Feet = feet;
            Yaw = yaw;
            Pose = pose;
        }
    }

    private readonly List<Actor> actors = new();

    /// <summary>The current scene's vertical stretch, for HD-2D sprites.</summary>
    private float VerticalScale = 1f;

    private void GatherActors(Map map, Player? player, float px, float pz, float lift, float time)
    {
        actors.Clear();
        foreach (var npc in map.NPCs)
        {
            if (npc.IsPCTerminal) continue;
            float seed = (npc.Name.GetHashCode() & 0xFFFF) / 65536f;
            var pose = new CharacterPose { Walk = npc.WalkCycle, WalkBlend = npc.WalkBlend, Time = time + seed * 10f, Blink = IsBlinking(time, seed) };
            actors.Add(new Actor(CharacterModels.Get(npc.NpcType, shaders.Character),
                new Vector3(npc.DrawX + 0.5f, 0, npc.DrawY + 0.5f), Player.YawOf(npc.Facing), pose));
        }

        if (player == null) return;

        var playerPose = new CharacterPose
        {
            Walk = player.WalkCycle,
            WalkBlend = player.WalkBlend,
            Running = player.IsRunning,
            Hop = player.HopProgress,
            Time = time,
            Blink = IsBlinking(time, 0.37f)
        };
        actors.Add(new Actor(CharacterModels.Get("PLAYER", shaders.Character), new Vector3(px, lift, pz), player.Yaw, playerPose));
    }

    private static bool IsBlinking(float time, float seed) => (time + seed * 7.3f) % 4.1f < 0.13f;

    /// <summary>Each character is a pixel-art sprite baked from its 3D model, standing upright like the walls.</summary>
    private void DrawActors(CharacterPass pass)
    {
        foreach (var actor in actors)
            CharacterSprites.DrawBillboard(context, actor.Rig, actor.Pose, actor.Yaw, actor.Feet, VerticalScale, pass);
    }

    /// <summary>"!" bubble over trainers who have spotted the player, just above the head.</summary>
    private static void DrawSpottedBubbles(MapScene scene, Map map)
    {
        var bubble = SceneTextures.Exclamation;
        foreach (var npc in map.NPCs)
        {
            if (npc.HasSpottedPlayer && npc.ExclamationTimer > 0f)
            {
                DrawUpright(bubble, new Rectangle(0, 0, bubble.Width, bubble.Height), npc.DrawX + 0.5f, npc.DrawY + 0.15f, 1.95f, 0.7f, 0.7f * scene.VS);
            }
        }
        Rlgl.DrawRenderBatchActive();
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

    /// <summary>
    /// A faint dark patch right under each character's feet. The shadow map casts their real shadow; this keeps
    /// them grounded when the sun is high or they are standing in shade.
    /// </summary>
    private static void DrawContactShadows(Map map, bool withPlayer, float px, float pz, float lift)
    {
        var tex = SceneTextures.ShadowBlob;
        Rlgl.DisableDepthMask();

        void Blob(float cx, float cz, float scale)
        {
            float rx = 0.3f * scale, rz = 0.2f * scale, y = 0.015f;
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
            if (!npc.IsPCTerminal) Blob(npc.DrawX + 0.5f, npc.DrawY + 0.52f, 1f);
        }
        if (withPlayer) Blob(px, pz + 0.02f, 1f - Math.Clamp(lift * 0.8f, 0f, 0.5f));

        Rlgl.SetTexture(0);
        Rlgl.DrawRenderBatchActive();
        Rlgl.EnableDepthMask();
    }
}
