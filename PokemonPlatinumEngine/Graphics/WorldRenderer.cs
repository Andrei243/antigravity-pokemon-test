using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Renders the HD-2D field with Pokémon Platinum's camera: pixel-art terrain, buildings and rooms in 3D, and
/// characters as baked pixel sprites, lit by the time of day's light rig. Each frame renders a shadow map from the
/// sun (or moon), then the scene into the scene target, which is composited with depth of field, bloom and grading.
/// <para>
/// A small map is one scene, built when first seen and kept. A map of the imported world is streamed: the block
/// of chunks round the camera is kept ready, each chunk baked on another thread as it comes within reach and
/// uploaded here, and chunks left behind are freed.
/// </para>
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

    // ------------------------------------------------------------------ streamed maps

    /// <summary>How many chunks to each side of the camera's are kept ready: 1 makes the block of three by three.</summary>
    public const int ChunkReach = 1;

    /// <summary>
    /// A chunk is freed once the camera is further than this many tiles from it. Half a chunk more than the
    /// block needs, so walking back and forth across a border doesn't free and bake the same chunks over and over.
    /// </summary>
    public const int ChunkKeepTiles = Map.ChunkTiles + Map.ChunkTiles / 4;

    /// <summary>The time a frame may spend uploading chunks that aren't in view yet, in milliseconds.</summary>
    public const double UploadBudgetMs = 2.5;

    /// <summary>Chunks of maps other than the one in view are kept (the town outside a house) until there are this many in all.</summary>
    private const int ChunkBudget = 24;

    // A chunk is baking on another thread (Pending), then being uploaded a step at a time (Uploading), then
    // ready to draw (Scene)
    private sealed class ChunkSlot
    {
        public Task<MapScene.Prepared>? Pending;
        public Queue<Action>? Uploading;
        public MapScene? Arriving;
        public MapScene? Scene;
    }

    private readonly Dictionary<(Map Map, int X, int Y), ChunkSlot> chunks = new();
    private readonly List<MapScene> drawn = new();

    /// <summary>What streaming has cost so far, for the harness: a chunk that isn't ready in time shows up as a wait.</summary>
    public sealed class StreamingStats
    {
        /// <summary>Chunks that became ready to draw.</summary>
        public int Chunks { get; internal set; }
        public double TotalUploadMs { get; internal set; }

        /// <summary>The most any one frame spent uploading.</summary>
        public double WorstFrameMs { get; internal set; }

        /// <summary>Times a frame had to stop for a chunk that came into view before it was ready.</summary>
        public int Waits { get; internal set; }
        public double WorstWaitMs { get; internal set; }

        public override string ToString() =>
            $"{Chunks} chunks made ready ({(Chunks > 0 ? TotalUploadMs / Chunks : 0):F1} ms of uploads each, at most {WorstFrameMs:F1} ms in a frame), {Waits} waited for (worst {WorstWaitMs:F1} ms)";
    }

    public StreamingStats Streaming { get; private set; } = new();

    public void ResetStreamingStats() => Streaming = new StreamingStats();

    private readonly System.Diagnostics.Stopwatch streamClock = new();

    /// <summary>How many chunks of streamed maps are on the GPU now.</summary>
    public int LoadedChunks
    {
        get
        {
            int count = 0;
            foreach (var slot in chunks.Values)
                if (slot.Scene != null) count++;
            return count;
        }
    }

    /// <summary>The chunks a camera over a tile needs: the block round its own, cut off at the map's edge.</summary>
    internal static IEnumerable<(int X, int Y)> ChunksAround(Map map, float focusX, float focusZ, int reach)
    {
        int cx = Math.Clamp((int)MathF.Floor(focusX / Map.ChunkTiles), 0, map.ChunkColumns - 1);
        int cy = Math.Clamp((int)MathF.Floor(focusZ / Map.ChunkTiles), 0, map.ChunkRows - 1);
        for (int y = Math.Max(0, cy - reach); y <= Math.Min(map.ChunkRows - 1, cy + reach); y++)
            for (int x = Math.Max(0, cx - reach); x <= Math.Min(map.ChunkColumns - 1, cx + reach); x++)
                yield return (x, y);
    }

    /// <summary>The scenes to draw for a map this frame: its one scene, or the chunks ready round the camera.</summary>
    private List<MapScene> ScenesFor(Map map, float focusX, float focusZ, GroundRect? view)
    {
        drawn.Clear();
        if (!map.IsStreamed)
        {
            drawn.Add(GetScene(map));
            return drawn;
        }

        // Whatever of the block isn't there yet starts baking on other threads: first the chunks the camera
        // sees, which the frame will wait for, then the rest
        for (int pass = 0; pass < 2; pass++)
            foreach (var (x, y) in ChunksAround(map, focusX, focusZ, ChunkReach))
            {
                if (chunks.ContainsKey((map, x, y))) continue;
                var window = MapScene.ChunkWindow(map, x, y);
                if (pass == 0 && view != null && !InView(window, view.Value)) continue;
                MapScene.Warm(map, window);
                chunks[(map, x, y)] = new ChunkSlot { Pending = Task.Run(() => Bake(map, window)) };
            }

        // What the camera sees must be there now: on arriving somewhere this waits for the bake and uploads it
        // whole, behind the fade. The rest is uploaded a few steps a frame, as far as the frame's budget goes,
        // and is ready well before the player can walk into sight of it.
        streamClock.Restart();

        foreach (var (key, slot) in chunks)
        {
            if (key.Map != map || slot.Scene != null) continue;
            var window = MapScene.ChunkWindow(map, key.X, key.Y);
            bool seen = view == null || InView(window, view.Value);

            if (slot.Pending != null)
            {
                if (!slot.Pending.IsCompleted)
                {
                    if (!seen) continue;
                    double before = streamClock.Elapsed.TotalMilliseconds;
                    slot.Pending.Wait();
                    Streaming.Waits++;
                    Streaming.WorstWaitMs = Math.Max(Streaming.WorstWaitMs, streamClock.Elapsed.TotalMilliseconds - before);
                }
                var prepared = slot.Pending.GetAwaiter().GetResult();
                slot.Pending = null;
                slot.Arriving = prepared.Scene;
                slot.Uploading = MapScene.Uploads(prepared, shaders);
            }

            while (slot.Uploading!.Count > 0 && (seen || streamClock.Elapsed.TotalMilliseconds < UploadBudgetMs))
                slot.Uploading.Dequeue()();
            if (slot.Uploading.Count > 0) continue;

            slot.Scene = slot.Arriving;
            slot.Arriving = null;
            slot.Uploading = null;
            Streaming.Chunks++;
        }
        double spent = streamClock.Elapsed.TotalMilliseconds;
        Streaming.TotalUploadMs += spent;
        Streaming.WorstFrameMs = Math.Max(Streaming.WorstFrameMs, spent);

        Evict(map, focusX, focusZ);
        foreach (var (key, slot) in chunks)
            if (key.Map == map && slot.Scene != null) drawn.Add(slot.Scene);
        return drawn;
    }

    /// <summary>
    /// Whether a chunk is in the camera's view: its own tiles and a little round them (the wider reach a scene is
    /// drawn within would rush chunks that are not in view yet).
    /// </summary>
    private static bool InView(TileWindow window, GroundRect view) =>
        view.Touches(new Vector3(window.X - 3, 0, window.Y - 3), new Vector3(window.Right + 3, 0, window.Bottom + 5));

    /// <summary>
    /// Bakes a chunk on a worker thread, which gives way while it does: the thread that draws, and the graphics
    /// driver's own, must not wait for a core behind nine chunks baking at once.
    /// </summary>
    private static MapScene.Prepared Bake(Map map, TileWindow window)
    {
        var thread = System.Threading.Thread.CurrentThread;
        var priority = thread.Priority;
        thread.Priority = System.Threading.ThreadPriority.BelowNormal;
        try
        {
            return MapScene.Prepare(map, window);
        }
        finally
        {
            thread.Priority = priority;
        }
    }

    private readonly List<(Map Map, int X, int Y)> leaving = new();

    /// <summary>
    /// Lets go of whatever was drawn of a part of a map whose ground has changed (a place the story revealed or
    /// hid again, plan 01 · M8): its chunks and their neighbours, whose edges were baked with it, are made again
    /// as they come into view.
    /// </summary>
    public void Forget(Map map, int x, int y, int width, int height)
    {
        var stale = chunks.Keys.Where(k => k.Map == map
            && k.X >= x / Map.ChunkTiles - 1 && k.X <= (x + width - 1) / Map.ChunkTiles + 1
            && k.Y >= y / Map.ChunkTiles - 1 && k.Y <= (y + height - 1) / Map.ChunkTiles + 1).ToList();
        foreach (var key in stale)
        {
            var slot = chunks[key];
            slot.Pending?.Wait();
            slot.Arriving?.Unload();
            slot.Scene?.Unload();
            chunks.Remove(key);
        }
    }

    private void Evict(Map map, float focusX, float focusZ)
    {
        leaving.Clear();
        foreach (var (key, slot) in chunks)
        {
            // A chunk still baking or half uploaded is left to finish and goes on a later frame
            if (slot.Scene == null) continue;
            bool far;
            if (key.Map == map)
            {
                var window = MapScene.ChunkWindow(map, key.X, key.Y);
                float dx = MathF.Max(0f, MathF.Max(window.X - focusX, focusX - window.Right));
                float dz = MathF.Max(0f, MathF.Max(window.Y - focusZ, focusZ - window.Bottom));
                far = MathF.Max(dx, dz) > ChunkKeepTiles;
            }
            else far = chunks.Count - leaving.Count > ChunkBudget;
            if (far) leaving.Add(key);
        }
        foreach (var key in leaving)
        {
            chunks[key].Scene?.Unload();
            chunks.Remove(key);
        }
    }

    /// <summary>
    /// What moves in the field besides the people in it: footprints, dust, leaves, rings on water, the door
    /// someone is going through. The game tells it what happens and runs its clock.
    /// </summary>
    internal FieldLife Life { get; } = new();

    // How far the light has gone over to snow country's (0 to 1), the field clock when it was last eased, and the map
    private float snowMix;
    private double snowClock;
    private Map? snowMap;

    // The weather of the scene last rendered and how bright a pale thing is in its light, for the layer drawn over the picture
    private FieldWeather weather;
    private float weatherLight = 1f;

    // ------------------------------------------------------------------ the camera's own moves

    // Where the camera has been sent to look instead of at the player, and how far it has got (0..1)
    private Vector2? panTarget;
    private float panAmount, panRate;
    private float easedGround = float.NaN;
    private double lastLifeTime;
    private Map? easedMap;

    /// <summary>
    /// Sends the camera to look at a spot (in tiles) over <paramref name="seconds"/>, easing out of where it is
    /// and into where it goes; it stays there until <see cref="ReleaseCamera"/>.
    /// </summary>
    public void PanCamera(float x, float z, float seconds)
    {
        panTarget = new Vector2(x, z);
        panRate = 1f / MathF.Max(0.01f, seconds);
    }

    /// <summary>Brings the camera back to the player over <paramref name="seconds"/>.</summary>
    public void ReleaseCamera(float seconds) => panRate = -1f / MathF.Max(0.01f, seconds);

    // A shake: how long it has left, how long it was given and how far it throws the picture
    private float shakeLeft, shakeLength = 1f, shakeReach;

    /// <summary>
    /// Shakes the picture up and down for a time, dying away: the ground trembling in a story scene. It moves in
    /// whole texels, thirty times a second, like everything else in the field.
    /// </summary>
    public void ShakeCamera(float seconds, float tiles = 0.19f)
    {
        shakeLeft = shakeLength = MathF.Max(0.01f, seconds);
        shakeReach = tiles;
    }

    /// <summary>How far the shake throws the camera this frame, in tiles; nought when nothing shakes.</summary>
    private float Shake(float step)
    {
        if (shakeLeft <= 0f) return 0f;
        shakeLeft = MathF.Max(0f, shakeLeft - step);
        int beat = (int)(Life.Now * 30.0);
        return SnapToTexel(((beat & 1) == 0 ? 1f : -1f) * shakeReach * (shakeLeft / shakeLength));
    }

    /// <summary>How far along a pan the camera is, 0 to 1, eased at both ends.</summary>
    public float PanEase => panAmount * panAmount * (3f - 2f * panAmount);

    /// <summary>A character's field sprite facing the camera, for the interface to show large. Call outside any texture mode.</summary>
    public Texture2D Portrait(string characterType)
    {
        context.EnsureLoaded();
        return CharacterSprites.Portrait(context, characterType);
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
        bool indoors = map.IsIndoors;
        float pitchDeg = MapScene.PitchOf(map), vs = MapScene.VerticalScaleOf(map);
        lastWasIndoors = indoors;
        // The weather where the player stands changes the light; the opening's fly-overs are always fair
        weather = player != null ? map.WeatherAt(player.GridX, player.GridY) : FieldWeather.Clear;
        var rig = ArtLook.FieldRig(hour, map);
        // Snow country grades the light colder (style guide, the areas' table), easing in and out at its border
        bool snowbound = player != null && !indoors && !map.IsCave && map.AreaAt(player.GridX, player.GridY)?.Snowbound == true;
        double now = Life.Now;
        snowMix = map != snowMap ? (snowbound ? 1f : 0f)
            : Math.Clamp(snowMix + (snowbound ? 1f : -1f) * (float)Math.Clamp(now - snowClock, 0, 1), 0f, 1f);
        snowClock = now;
        snowMap = map;
        if (snowMix > 0f) rig = LightRig.Lerp(rig, ArtLook.Snowbound(rig), snowMix);
        rig = ArtLook.Weathered(rig, weather);
        var light = rig.Light;
        weatherLight = Brightness(rig);

        float time = (float)FrameClock.Now;
        float lift = player != null ? player.HopHeight / Player.TileSize : 0f;
        // The ground under the view's middle: the deck or the hillside the player is on (zero on flat maps)
        float groundY = player != null ? Relief.Under(map, px, pz, player.HeightOn(map)) : Relief.At(map, px, pz);
        // A room is one scene and the camera looks at its middle; outdoors the camera follows the player
        var room = indoors ? GetScene(map) : null;

        // The camera's own time: it rises and falls after the player smoothly (a jump of more than a tile and
        // a half is an arrival somewhere else, and is taken at once), and goes where it is sent
        float step = (float)Math.Clamp(Life.Now - lastLifeTime, 0.0, 0.1);
        lastLifeTime = Life.Now;
        if (map != easedMap || float.IsNaN(easedGround) || MathF.Abs(groundY - easedGround) > 1.5f) easedGround = groundY;
        else easedGround += (groundY - easedGround) * MathF.Min(1f, step * 10f);
        easedMap = map;
        panAmount = Math.Clamp(panAmount + panRate * step, 0f, 1f);
        if (panAmount <= 0f && panRate < 0f) panTarget = null;
        float lookX = px, lookZ = pz;
        if (panTarget is { } sent)
        {
            lookX += (sent.X - px) * PanEase;
            lookZ += (sent.Y - pz) * PanEase;
        }
        var camera = BuildCamera(map, room, lookX, lookZ, easedGround);
        if (Shake(step) is var jolt && jolt != 0f)
        {
            camera.Position.Z += jolt;
            camera.Target.Z += jolt;
        }

        // In a cave nobody has lit, where the player stands on the picture and how large a tile is there
        dark = player != null && Darkness.Covers(map);
        if (dark)
        {
            var feet = new Vector3(px, groundY, pz);
            var at = Raylib.GetWorldToScreenEx(feet, camera, 1920, 1080);
            var east = Raylib.GetWorldToScreenEx(feet + Vector3.UnitX, camera, 1920, 1080);
            var south = Raylib.GetWorldToScreenEx(feet + Vector3.UnitZ, camera, 1920, 1080);
            // Round the player's middle, not their feet
            darkAt = new Vector2(at.X / 1920f, (at.Y - 0.5f * (south.Y - at.Y)) / 1080f);
            darkTile = new Vector2((east.X - at.X) / 1920f, (south.Y - at.Y) / 1080f);
        }

        // Outdoors, only the part of the map near the view is drawn (rooms are small enough to draw whole)
        GroundRect? view = null, casters = null;
        if (!indoors) (view, casters) = VisibleRects(map, camera.Target, groundY, light.SunDirection);
        var scenes = ScenesFor(map, camera.Target.X, camera.Target.Z, view);

        shaders.SetTime(time);
        shaders.SetWind(Weathers.Wind(weather));
        if (!indoors) SceneTextures.AnimateWaterfall(Life.Now);
        foreach (var scene in scenes) scene.Animate(Life.Now);
        // On a map of the whole region only the people near the view are drawn
        sight = map.IsStreamed ? view : null;
        VerticalScale = vs;
        GatherActors(map, player, px, pz, groundY, lift, time);
        foreach (var actor in actors) CharacterSprites.Prepare(context, actor.Rig, actor.Pose, actor.Yaw);

        // Grass leans away from everyone on the map (set after the sprite bakes, which clear it)
        walkerFeet.Clear();
        foreach (var actor in actors) walkerFeet.Add(actor.Feet);
        shaders.SetWalkers(walkerFeet);

        // 1. Shadow map: depth of everything that casts shadows, seen from the sun (or moon)
        var focus = room != null ? room.RoomCenter with { Y = 0 } : new Vector3(camera.Target.X, groundY, camera.Target.Z - 2f);
        var lightCamera = context.Shadows.LightCamera(focus, light.SunDirection, indoors ? 18f : 40f);

        FrameProfiler.Lap(FrameSection.Prepare);
        Raylib.BeginTextureMode(context.Shadows.Target);
        Raylib.ClearBackground(Color.White);
        Rlgl.SetClipPlanes(1.0, 200.0);
        Raylib.BeginMode3D(lightCamera);
        var lightView = Rlgl.GetMatrixModelview();
        var lightProjection = Rlgl.GetMatrixProjection();
        Rlgl.DisableBackfaceCulling();
        foreach (var scene in scenes)
            if (Reaches(scene, casters)) scene.DrawDepth(casters);
        DrawActors(CharacterPass.Depth);
        DrawThings(CharacterPass.Depth);
        Rlgl.EnableBackfaceCulling();
        Raylib.EndMode3D();
        Raylib.EndTextureMode();
        FrameProfiler.Lap(FrameSection.Shadows);

        shaders.SetLighting(Raymath.MatrixMultiply(lightView, lightProjection), light, camera.Position, context.Shadows.Texel);
        shaders.SetCharacterStyle(shadowStrength: 1f, rimStrength: rig.Rim);
        shaders.SetWorldRamp(0f);
        shaders.SetFog(rig.FogColor, rig.FogAmount, rig.FogNear, rig.FogFar);
        shaders.SetCloudShade(rig.CloudShade);
        // Outdoors, lamps and windows light up after dark; in a room only the window glass changes, to the sky's colour
        shaders.SetGlow(indoors ? 0f : rig.LampGlow, rig.HomeGlow, rig.GlowColor);
        // Rooms keep their perspective: their side walls are what makes them a doll's house
        shaders.SetUpright(indoors ? 0f : ArtLook.FieldUpright, pitchDeg);

        // 2. The scene itself, sampling the shadow map
        float near = indoors ? 1f : 10f, far = indoors ? 100f : 140f;
        Raylib.BeginTextureMode(context.Target);
        Raylib.ClearBackground(rig.Background);
        Rlgl.SetClipPlanes(near, far);

        Raylib.BeginMode3D(camera);
        Rlgl.DisableBackfaceCulling();
        context.BindShadowMap();

        foreach (var scene in scenes)
            if (Reaches(scene, view)) scene.Draw(view);
        // In a room the light on the floor is daylight; outdoors it is lamplight, which only shows once it is dark
        // (squared, so pools stay faint while the lamps are coming on at twilight)
        foreach (var scene in scenes)
        {
            if (!Reaches(scene, view)) continue;
            if (indoors) scene.DrawLights(rig.LampGlow, 0f, rig.LightTint);
            else scene.DrawLights(rig.LampGlow * rig.LampGlow, rig.HomeGlow * rig.HomeGlow, rig.LightTint);
        }
        DrawReflections(map, camera);
        DrawContactShadows(map, camera, player != null, px, pz, groundY, lift);
        DrawLife(map, camera, rig, upright: false);
        DrawDoor(map);
        DrawThings(CharacterPass.Color);
        DrawActors(CharacterPass.Color);
        DrawLife(map, camera, rig, upright: true);
        DrawBubbles(map, player, camera, rig, indoors, pitchDeg, vs);

        Rlgl.EnableBackfaceCulling();
        Raylib.EndMode3D();
        Raylib.EndTextureMode();

        context.UnbindShadowMap();
        Rlgl.SetClipPlanes(0.01, 1000.0);
        FrameProfiler.Lap(FrameSection.Scene);
        context.PreparePost(rig.Post, new DepthRange(near, far, camera.FovY, (float)width / height), aoRadius: 0.45f);
    }

    /// <summary>
    /// Composites the last rendered frame into the current target with tilt-shift blur and grading, and draws
    /// the weather over it.
    /// </summary>
    public void DrawToScreen(int destWidth, int destHeight)
    {
        context.Composite(new Rectangle(0, 0, destWidth, destHeight));
        DrawWeather(destWidth, destHeight);
        if (dark) DrawDarkness(destWidth, destHeight);
    }

    private bool dark;
    private Vector2 darkAt, darkTile;

    /// <summary>
    /// The dark of an unlit cave (style guide, "Caves"): the picture falls to the dark's colour everywhere but in
    /// a circle of the floor round the player, which lies on the ground and so is wider than it is tall.
    /// </summary>
    private void DrawDarkness(int destWidth, int destHeight)
    {
        float cx = darkAt.X * destWidth, cy = darkAt.Y * destHeight;
        float halfW = Darkness.Reach * darkTile.X * destWidth, halfH = Darkness.Reach * darkTile.Y * destHeight;
        var texture = SceneTextures.Darkness;
        var lit = new Rectangle(MathF.Round(cx - halfW), MathF.Round(cy - halfH), MathF.Round(halfW * 2f), MathF.Round(halfH * 2f));
        Raylib.DrawTexturePro(texture, new Rectangle(0, 0, texture.Width, texture.Height), lit, Vector2.Zero, 0f, Color.White);

        // Past the circle's square there is only the dark
        var colour = new Color(Darkness.Colour.R, Darkness.Colour.G, Darkness.Colour.B, (byte)255);
        float right = lit.X + lit.Width, bottom = lit.Y + lit.Height;
        if (lit.Y > 0) Raylib.DrawRectangleRec(new Rectangle(0, 0, destWidth, lit.Y), colour);
        if (bottom < destHeight) Raylib.DrawRectangleRec(new Rectangle(0, bottom, destWidth, destHeight - bottom), colour);
        float top = MathF.Max(0, lit.Y), height = MathF.Min(destHeight, bottom) - top;
        if (lit.X > 0 && height > 0) Raylib.DrawRectangleRec(new Rectangle(0, top, lit.X, height), colour);
        if (right < destWidth && height > 0) Raylib.DrawRectangleRec(new Rectangle(right, top, destWidth - right, height), colour);
    }

    private void DrawWeather(int destWidth, int destHeight)
    {
        if (weather == FieldWeather.Clear) return;

        // Mist takes the light of the scene it lies in; what falls catches a little more of it
        hazeLayers.Clear();
        WeatherFx.Haze(weather, Life.Now, hazeLayers);
        foreach (var layer in hazeLayers)
        {
            var source = new Rectangle(-layer.OffsetX / layer.Scale, -layer.OffsetY / layer.Scale, destWidth / layer.Scale, destHeight / layer.Scale);
            Raylib.DrawTexturePro(SceneTextures.Haze, source, new Rectangle(0, 0, destWidth, destHeight), Vector2.Zero, 0f, Dimmed(layer.Tint, weatherLight));
        }

        // Lightning whitens everything under it, the rain included
        if (WeatherFx.Lightning(weather, Life.Now) is > 0f and var flash)
            Raylib.DrawRectangle(0, 0, destWidth, destHeight, new Color(255, 255, 255, (int)(flash * 255f)));

        weatherBlocks.Clear();
        WeatherFx.Build(weather, Life.Now, destWidth, destHeight, weatherBlocks);
        float falling = 0.55f + 0.45f * weatherLight;
        foreach (var block in weatherBlocks)
            Raylib.DrawRectangleRec(new Rectangle(block.X, block.Y, block.Width, block.Height), Dimmed(block.Color, falling));
    }

    private readonly List<WeatherBlock> weatherBlocks = new();
    private readonly List<HazeLayer> hazeLayers = new();

    /// <summary>What the scene's light makes of a pale thing drawn without the scenery's shader: the sun and the sky's share of it.</summary>
    private static float Brightness(LightRig rig)
    {
        var lit = rig.Light.SunColor + rig.Light.SkyAmbient * 0.7f;
        return Math.Clamp((lit.X + lit.Y + lit.Z) / 3f, 0.3f, 1f);
    }

    private static Color Dimmed(Color c, float light) => new((byte)(c.R * light), (byte)(c.G * light), (byte)(c.B * light), c.A);

    // ------------------------------------------------------------------ camera

    /// <summary>Whether a scene has anything on a rectangle of ground: a chunk outside it is skipped whole.</summary>
    private static bool Reaches(MapScene scene, GroundRect? rect)
    {
        if (rect == null || scene.Chunk == null) return true;
        var bounds = scene.Bounds;
        return rect.Value.Touches(new Vector3(bounds.MinX, 0, bounds.MinZ), new Vector3(bounds.MaxX, 0, bounds.MaxZ));
    }

    /// <param name="room">The scene of an indoor map, whose middle the camera looks at; null outdoors.</param>
    /// <param name="groundY">The drawn height of the ground the camera follows.</param>
    private Camera3D BuildCamera(Map map, MapScene? room, float px, float pz, float groundY = 0f)
    {
        float pitch = MapScene.PitchOf(map) * MathF.PI / 180f;
        var dir = new Vector3(0, MathF.Sin(pitch), MathF.Cos(pitch));

        if (room != null)
        {
            // Deeper rooms pull the camera back a little so the whole room stays in frame; a hall deeper than
            // that (a Gym) keeps the deepest room's distance and follows the player along its length (style guide,
            // "A room is seen whole; a hall is followed")
            var roomTarget = room.RoomCenter;
            int depth = room.RoomDepth;
            float distance = MapScene.IndoorDistance * Math.Max(1f, (Math.Min(depth, DeepestRoom) - 1) / 8f);
            if (depth > DeepestRoom)
            {
                float reach = (depth - DeepestRoom) / 2f;
                roomTarget.Z = SnapToTexel(Math.Clamp(pz, roomTarget.Z - reach, roomTarget.Z + reach));
            }
            return new Camera3D(roomTarget + dir * distance, roomTarget, Vector3.UnitY,
                MapScene.IndoorFovYDeg, CameraProjection.Perspective);
        }

        // Aim at the player's middle so they sit near the centre of the screen, then keep the view
        // inside the map plus a few tiles of the surrounding forest
        var t = new Vector3(px, groundY + 0.6f * MapScene.VerticalScaleOf(map), pz);
        var lens = MapScene.ViewOf(map);
        var (farOff, nearOff, halfW) = VisibleGround(t.Y - groundY, lens);
        // A cave ends at its rock: the view stays inside it. The open country shows a few tiles of forest past its edge
        float overscan = map.IsCave ? 0f : 4f;
        t.X = ClampOrCenter(t.X, -overscan + halfW, map.Width + overscan - halfW);
        t.Z = ClampOrCenter(t.Z, -overscan - farOff, map.Height + overscan - nearOff);

        // Scroll in whole texels of the pixel art, so textures and sprites don't shimmer as the view moves
        t.X = SnapToTexel(t.X);
        t.Z = SnapToTexel(t.Z);

        return new Camera3D(t + dir * lens.Distance, t, Vector3.UnitY, lens.FovYDeg, CameraProjection.Perspective);
    }

    /// <summary>The deepest room the camera frames whole: a deeper hall is followed along its length.</summary>
    public const int DeepestRoom = 14;

    internal static float SnapToTexel(float v) => MathF.Round(v * CharacterSprites.TexelsPerUnit) / CharacterSprites.TexelsPerUnit;

    private static float ClampOrCenter(float v, float min, float max) =>
        min > max ? (min + max) / 2f : Math.Clamp(v, min, max);

    /// <summary>
    /// Where the top and bottom screen edges meet the ground (as Z offsets from the target) and the
    /// half-width of the view at the far edge, for the outdoor perspective camera.
    /// </summary>
    private (float FarOffset, float NearOffset, float HalfWidth) VisibleGround(float targetHeight, FieldView lens)
    {
        float pitch = lens.PitchDeg * MathF.PI / 180f;
        float half = lens.FovYDeg / 2f * MathF.PI / 180f;
        float d = lens.Distance;
        float camHeight = targetHeight + d * MathF.Sin(pitch);
        float camBack = d * MathF.Cos(pitch);

        float farAngle = pitch - half, nearAngle = pitch + half;
        float farOffset = camBack - camHeight / MathF.Tan(farAngle);
        float nearOffset = camBack - camHeight / MathF.Tan(nearAngle);
        float farDistance = camHeight / MathF.Sin(farAngle) * MathF.Cos(half);
        float halfWidth = farDistance * MathF.Tan(half) * width / height;
        return (farOffset, nearOffset, halfWidth);
    }

    /// <summary>
    /// The ground the outdoor camera can see when it looks at <paramref name="target"/>, with room for what stands
    /// just outside and still reaches in, and the wider ground from which a shadow can fall into that view.
    /// </summary>
    internal (GroundRect View, GroundRect Casters) VisibleRects(Vector3 target, Vector3 sunDirection) => VisibleRects(null, target, 0f, sunDirection);

    /// <param name="map">The map, where its relief matters: higher ground south of the view rises into it, lower ground north of it too.</param>
    /// <param name="groundY">The drawn height of the ground the camera looks at.</param>
    internal (GroundRect View, GroundRect Casters) VisibleRects(Map? map, Vector3 target, float groundY, Vector3 sunDirection)
    {
        const float tallest = 5f;
        var lens = map != null ? MapScene.ViewOf(map) : FieldView.Outdoor;
        var (farOff, nearOff, halfW) = VisibleGround(target.Y - groundY, lens);
        // Things are a tile or two wide, and a tall tree south of the bottom edge still shows its top
        var view = new GroundRect(target.X - halfW - 2f, target.Z + farOff - 2f, target.X + halfW + 2f, target.Z + nearOff + 4f);
        if (map is { HasRelief: true })
        {
            // A tile of height shows as far up the screen as six tenths of a tile of ground (the camera's pitch)
            const int look = 10;
            float perTile = 1f / MathF.Tan(lens.PitchDeg * MathF.PI / 180f);
            var (low, high) = Relief.Range(map, (int)view.MinX, (int)view.MinZ - look, (int)view.MaxX + 1, (int)view.MaxZ + look);
            view = view with { MinZ = view.MinZ - MathF.Max(0f, groundY - low) * perTile, MaxZ = view.MaxZ + MathF.Max(0f, high - groundY) * perTile };
        }
        // A shadow is as long as its caster is tall, times how low the sun is, and falls away from the sun
        float reach = tallest / MathF.Max(0.25f, sunDirection.Y);
        var casters = new GroundRect(
            view.MinX - MathF.Max(0f, -sunDirection.X) * reach - 1f, view.MinZ - MathF.Max(0f, -sunDirection.Z) * reach - 1f,
            view.MaxX + MathF.Max(0f, sunDirection.X) * reach + 1f, view.MaxZ + MathF.Max(0f, sunDirection.Z) * reach + 1f);
        return (view, casters);
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
    private readonly List<Vector3> walkerFeet = new();

    /// <summary>The current scene's vertical stretch, for HD-2D sprites.</summary>
    private float VerticalScale = 1f;

    /// <summary>On a streamed map, the ground people are drawn on this frame; null where everyone is drawn.</summary>
    private GroundRect? sight;

    private bool InSight(NPC npc) =>
        sight is not { } s || (npc.DrawX >= s.MinX - 2f && npc.DrawX <= s.MaxX + 2f && npc.DrawY >= s.MinZ - 2f && npc.DrawY <= s.MaxZ + 2f);

    /// <summary>
    /// How many rows of their sprite someone sinks into the ground they stand in: deep snow and marsh mud take
    /// them to the shin or the knee. The ground hides what is under it.
    /// </summary>
    internal static int SinkRows(TileBehavior underfoot) => underfoot switch
    {
        TileBehavior.DeepSnow => 4,
        TileBehavior.DeeperSnow => 7,
        TileBehavior.DeepestSnow => 10,
        TileBehavior.Mud or TileBehavior.MarshGrass => 2,
        TileBehavior.DeepMud or TileBehavior.DeepMarshGrass => 6,
        _ => 0
    };

    // A row of a sprite, as a height in the scene
    private float Rows(float rows) => rows / CharacterSprites.TexelsPerUnit * VerticalScale;

    private float SinkAt(Map map, float x, float z) => Rows(SinkRows(map.BehaviourAt((int)MathF.Floor(x), (int)MathF.Floor(z))));

    // The Pokémon the player rides on water, when there is one to draw
    private (Vector3 At, float Yaw)? mount;

    private void GatherActors(Map map, Player? player, float px, float pz, float groundY, float lift, float time)
    {
        actors.Clear();
        mount = null;
        things.Clear();
        foreach (var npc in map.NPCs)
        {
            if (npc.IsPCTerminal || !InSight(npc)) continue;
            if (npc.IsThing)
            {
                // An item in its ball and an obstacle are cards, not people: an obstacle stands a little south of
                // its tile's middle, as the props it once was did
                var kind = npc.Obstacle ?? PropType.Mailbox;
                float bx = npc.DrawX + 0.5f, bz = npc.DrawY + ThingCards.FootOf(kind);
                things.Add((new Vector3(bx, Relief.At(map, bx, bz), bz), kind));
                continue;
            }
            float seed = SeedOf(npc.Name);
            var pose = new CharacterPose { Walk = npc.WalkCycle, WalkBlend = npc.WalkBlend, Time = time + seed * 10f, Blink = IsBlinking(time, seed) };
            if (npc.HasSpottedPlayer && npc.ExclamationTimer > 0f)
            {
                // A trainer who has just spotted the player starts, under the "!"
                pose.Emote = Emote.Surprised;
                pose.EmoteTime = TrainerApproach.ExclaimTime - npc.ExclamationTimer;
                pose.Blink = false;
            }
            float nx = npc.DrawX + 0.5f, nz = npc.DrawY + 0.5f;
            actors.Add(new Actor(CharacterModels.Get(PlayerIdentity.CharacterFor(npc.NpcType), shaders),
                new Vector3(nx, (npc.Level is { } level ? Relief.Under(map, nx, nz, level) : Relief.At(map, nx, nz)) - SinkAt(map, nx, nz), nz), Player.YawOf(npc.Facing), pose));
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
        // On the water the player sits on a Pokémon's back, and the two bob together, a texel up and a texel down
        float saddle = player.Saddle;
        float bob = player.Mount != null && MathF.Floor(time * 2.4f) % 2f == 0f ? Rows(1f) : 0f;
        float feet = groundY + lift + Rows(SurfMount.Seat) * saddle + bob * saddle - SinkAt(map, px, pz) * (1f - saddle);
        actors.Add(new Actor(CharacterModels.Get(PlayerIdentity.Character, shaders), new Vector3(px, feet, pz), player.Yaw, playerPose));

        if (player.Mount is { } ridden)
        {
            float mx = ridden.X + 0.5f, mz = ridden.Y + 0.5f;
            // A little nearer the camera than its rider, so its back covers the rider's shoes
            mount = (new Vector3(mx, Relief.At(map, mx, mz) + bob - Rows(2f), mz + 0.06f), player.Yaw);
        }
    }

    private static bool IsBlinking(float time, float seed) => (time + seed * 7.3f) % 4.1f < 0.13f;

    /// <summary>
    /// A number from 0 to 1 that is a name's own, the same every run (a string's hash code changes from run to
    /// run): it sets each person's breathing and blinking apart from the next one's.
    /// </summary>
    internal static float SeedOf(string name)
    {
        uint hash = 2166136261;
        foreach (char c in name) hash = (hash ^ c) * 16777619;
        return (hash & 0xFFFF) / 65536f;
    }

    /// <summary>Each character is a pixel-art sprite baked from its 3D model, standing upright like the walls.</summary>
    // Things on the ground in view (items' balls and the obstacles field moves clear), and the card of each kind;
    // an item's ball goes under the key of the mailbox, which is never a thing
    private readonly List<(Vector3 At, PropType Kind)> things = new();
    private readonly Dictionary<PropType, CharacterSprites.Card> thingCards = new();

    /// <summary>The things on the ground: a card for each kind, lit and casting like the people.</summary>
    private void DrawThings(CharacterPass pass)
    {
        foreach (var (at, kind) in things)
        {
            if (!thingCards.TryGetValue(kind, out var card)) thingCards[kind] = card = CharacterSprites.MakeCard(context, ThingCards.Paint(kind));
            CharacterSprites.DrawCard(card, at, VerticalScale, pass);
        }
    }

    private void DrawActors(CharacterPass pass)
    {
        foreach (var actor in actors)
            CharacterSprites.DrawBillboard(context, actor.Rig, actor.Pose, actor.Yaw, actor.Feet, VerticalScale, pass);
        if (mount is { } m) SurfMount.Draw(context, m.At, m.Yaw, VerticalScale, pass);
    }

    /// <summary>
    /// The bubbles over people's heads: the "!" of a trainer who has spotted the player, and whatever anyone,
    /// the player included, has been given to show (<see cref="NPC.ShowBubble"/>). A bubble pops up in three
    /// frames: half size, four fifths, whole.
    /// </summary>
    private void DrawBubbles(Map map, Player? player, Camera3D camera, LightRig rig, bool indoors, float pitchDeg, float vs)
    {
        // A bubble is paper, not a lamp: after dark it dims some of the way with the scene, so it doesn't glow
        var paper = Dimmed(Color.White, 0.35f + 0.65f * Brightness(rig));
        Camera3D? straight = indoors ? null : camera;
        void Bubble(EmoteBubble kind, float age, float x, float z, float ground)
        {
            var art = SceneTextures.Bubble(kind);
            float size = age < 0.04f ? 0.5f : age < 0.08f ? 0.8f : 1f;
            float w = LifeArt.BubbleWidth / 32f * size, h = LifeArt.BubbleHeight / 32f * size * vs;
            DrawUpright(art, new Rectangle(0, 0, art.Width, art.Height), x + 0.5f, z + 0.15f, ground + 1.95f, w, h, paper, straight, pitchDeg);
        }

        foreach (var npc in map.NPCs)
        {
            float ground = npc.Level is { } level ? Relief.Under(map, npc.DrawX + 0.5f, npc.DrawY + 0.5f, level) : Relief.At(map, npc.DrawX + 0.5f, npc.DrawY + 0.5f);
            if (npc.HasSpottedPlayer && npc.ExclamationTimer > 0f)
                Bubble(EmoteBubble.Exclaim, TrainerApproach.ExclaimTime - npc.ExclamationTimer, npc.DrawX, npc.DrawY, ground);
            else if (npc.BubbleTimer > 0f && npc.Bubble != EmoteBubble.None && InSight(npc))
                Bubble(npc.Bubble, npc.BubbleAge, npc.DrawX, npc.DrawY, ground);
        }
        if (player is { BubbleTimer: > 0f } && player.Bubble != EmoteBubble.None)
            Bubble(player.Bubble, player.BubbleAge, player.PixelX / Player.TileSize, player.PixelY / Player.TileSize,
                Relief.Under(map, player.PixelX / Player.TileSize + 0.5f, player.PixelY / Player.TileSize + 0.5f, player.HeightOn(map)));
        Rlgl.DrawRenderBatchActive();
    }

    // ------------------------------------------------------------------ life

    private readonly List<LifeQuad> lifeFlat = new(), lifeUpright = new();

    /// <summary>
    /// Footprints and rings lying on the ground, or dust, leaves and drops standing up: cells of the life atlas
    /// drawn without the scenery's shader, dimmed by hand as the light goes.
    /// </summary>
    private void DrawLife(Map map, Camera3D camera, LightRig rig, bool upright)
    {
        if (!upright)
        {
            lifeFlat.Clear();
            lifeUpright.Clear();
            Life.Quads(lifeFlat, lifeUpright);
        }
        var quads = upright ? lifeUpright : lifeFlat;
        if (quads.Count == 0) return;

        var atlas = SceneTextures.Life;
        float pitchDeg = MapScene.PitchOf(map), vs = MapScene.VerticalScaleOf(map);
        float lit = Brightness(rig);
        const float cell = FieldLife.Cell;
        Camera3D? straight = map.IsIndoors ? null : camera;
        Rlgl.DisableDepthMask();
        foreach (var q in quads)
        {
            // Water thrown up catches the sky: it dims only half as far as the ground does
            bool wet = q.Cell >= FieldLife.Ring;
            var tint = Dimmed(q.Tint, wet ? 0.5f + 0.5f * lit : lit);
            var src = new Rectangle(q.Cell % FieldLife.Columns * cell, q.Cell / FieldLife.Columns * cell, cell, cell);
            float cx = q.At.X;
            if (upright)
            {
                DrawUpright(atlas, src, SnapToTexel(cx), q.At.Z, q.At.Y, q.Width, q.Height * vs, tint, straight, pitchDeg);
                continue;
            }

            if (!map.IsIndoors && q.At.Y > 0.05f) cx = Straighten(camera, pitchDeg, cx, q.At.Y, q.At.Z);
            float hw = q.Width / 2f, hd = q.Height / 2f;
            float u0 = src.X / atlas.Width, u1 = (src.X + src.Width) / atlas.Width, v0 = src.Y / atlas.Height, v1 = (src.Y + src.Height) / atlas.Height;
            // The cell's four corners, turned by quarter turns so a print points the way it was walked
            (float U, float V)[] uv = { (u0, v1), (u1, v1), (u1, v0), (u0, v0) };
            Rlgl.CheckRenderBatchLimit(4);
            Rlgl.SetTexture(atlas.Id);
            Rlgl.Begin(DrawMode.Quads);
            Rlgl.Color4ub(tint.R, tint.G, tint.B, tint.A);
            (float X, float Z)[] corner = { (cx - hw, q.At.Z + hd), (cx + hw, q.At.Z + hd), (cx + hw, q.At.Z - hd), (cx - hw, q.At.Z - hd) };
            for (int i = 0; i < 4; i++)
            {
                var (u, v) = uv[(i + q.Turn) % 4];
                Rlgl.TexCoord2f(u, v);
                Rlgl.Vertex3f(corner[i].X, q.At.Y, corner[i].Z);
            }
            Rlgl.End();
        }
        Rlgl.SetTexture(0);
        Rlgl.DrawRenderBatchActive();
        Rlgl.EnableDepthMask();
    }

    private readonly Dictionary<(bool Glass, bool Pair, byte Light, int Frame), CharacterSprites.Card> doorCards = new();

    /// <summary>
    /// The door someone is going through, standing ajar or open: a card of the open door's art over the shut
    /// one painted on the wall, exactly where the building's art has it.
    /// </summary>
    private void DrawDoor(Map map)
    {
        if (Life.MovingDoor is not { } moving || moving.Map != map || map.IsIndoors) return;
        int frame = Life.DoorFrame(map, moving.X, moving.Y);
        if (frame == 0 || DoorPlace(map, moving.X, moving.Y) is not { } place) return;

        var key = (place.Glass, place.Pair, place.Light, frame);
        if (!doorCards.TryGetValue(key, out var card))
            doorCards[key] = card = CharacterSprites.MakeCard(context, BuildingArt.OpenDoor(place.Glass, place.Pair, frame, place.Light), partOfAWall: true);
        CharacterSprites.DrawCard(card, place.Foot, VerticalScale, CharacterPass.Color);
    }

    /// <summary>
    /// Where the door of a warp's tile is painted, and how large: in the building's front wall, in the entrance
    /// block that stands proud of it, or in a porch's front. Null if no building has a door there, or its door
    /// is one that doesn't open by itself (a works' sliding door).
    /// </summary>
    internal static (Vector3 Foot, bool Glass, bool Pair, byte Light)? DoorPlace(Map map, int x, int y)
    {
        foreach (var b in MapStructures.BuildingsOf(map))
        {
            if (b.Annex || x < b.X0 || x > b.X1 || (y != b.Y1 && y != b.Y1 + 1) || !b.Doors.Exists(d => d.X == x)) continue;
            var style = BuildingArt.StyleOf(b, map.ArchitectureAt(b.X0, b.Y0));
            if (style.SlidingDoor) return null;

            // Texels south of the building's north edge: the wall's face, or the front of what stands before it
            bool closedPorch = b.Porch.Contains(x);
            bool portal = style.Portal > 0 && style.Pitched && b.Porch.Count == 0;
            float z = b.Depth * BuildingArt.Bay + (closedPorch ? BuildingArt.Bay - BuildingArt.Inset : portal ? BuildingArt.PortalDepth : 0);
            bool pair = !style.GlassDoor && (style.Portal > 0 || closedPorch);
            float ground = Relief.At(map, b.X0 + b.Width / 2f, b.Y1 + 0.5f);
            // A texel in front of the wall's art, so it wins over it
            return (new Vector3(x + 0.5f, ground, b.Y0 + (z + 1f) / BuildingArt.Bay), style.GlassDoor, pair, BuildingArt.WindowLight(b, style));
        }
        return null;
    }

    /// <summary>
    /// Where something at height <paramref name="y"/> over (x, z) has to be drawn to appear straight above that
    /// point, as the field's vertex shader does for scenery (<see cref="FieldShaders.SetUpright"/>).
    /// </summary>
    internal static float Straighten(Camera3D camera, float pitchDeg, float x, float y, float z)
    {
        var forward = Vector3.Normalize(camera.Target - camera.Position);
        float footDepth = Vector3.Dot(new Vector3(x, 0, z) - camera.Position, forward);
        float lift = y * MathF.Sin(pitchDeg * MathF.PI / 180f);
        float k = (footDepth - lift) / (footDepth - lift + lift * ArtLook.FieldUpright);
        return camera.Position.X + (x - camera.Position.X) * k;
    }

    /// <summary>
    /// An upright textured quad facing the camera's direction, centred on x and standing at y0. Given the field's
    /// camera, each corner is moved to where the scenery's shader would have put it (<see cref="Straighten"/>),
    /// so the quad stands as straight on the screen as the sprites beside it.
    /// </summary>
    private static void DrawUpright(Texture2D tex, Rectangle src, float cx, float cz, float y0, float w, float h, Color tint,
        Camera3D? straight = null, float pitchDeg = 0f)
    {
        float y1 = y0 + h;
        float x0 = cx - w / 2f, x1 = cx + w / 2f;
        float u0 = src.X / tex.Width, u1 = (src.X + src.Width) / tex.Width;
        float v0 = src.Y / tex.Height, v1 = (src.Y + src.Height) / tex.Height;
        float bx0 = x0, bx1 = x1, tx0 = x0, tx1 = x1;
        if (straight is { } camera)
        {
            bx0 = Straighten(camera, pitchDeg, x0, y0, cz);
            bx1 = Straighten(camera, pitchDeg, x1, y0, cz);
            tx0 = Straighten(camera, pitchDeg, x0, y1, cz);
            tx1 = Straighten(camera, pitchDeg, x1, y1, cz);
        }

        Rlgl.CheckRenderBatchLimit(4);
        Rlgl.SetTexture(tex.Id);
        Rlgl.Begin(DrawMode.Quads);
        Rlgl.Color4ub(tint.R, tint.G, tint.B, tint.A);
        Rlgl.TexCoord2f(u0, v1); Rlgl.Vertex3f(bx0, y0, cz);
        Rlgl.TexCoord2f(u1, v1); Rlgl.Vertex3f(bx1, y0, cz);
        Rlgl.TexCoord2f(u1, v0); Rlgl.Vertex3f(tx1, y1, cz);
        Rlgl.TexCoord2f(u0, v0); Rlgl.Vertex3f(tx0, y1, cz);
        Rlgl.End();
        Rlgl.SetTexture(0);
    }

    /// <summary>
    /// Whoever stands in a puddle is mirrored in it (style guide, "Puddles"): their sprite upside down on the ground
    /// to the south of their feet, as far as the puddle runs that way.
    /// </summary>
    private void DrawReflections(Map map, Camera3D camera)
    {
        static bool Mirrors(TileBehavior b) => b is TileBehavior.Puddle or TileBehavior.StillPuddle;
        float pitchDeg = MapScene.PitchOf(map);
        Camera3D? straight = map.IsIndoors ? null : camera;
        bool any = false;
        foreach (var actor in actors)
        {
            int tx = (int)MathF.Floor(actor.Feet.X), tz = (int)MathF.Floor(actor.Feet.Z);
            if (!map.InBounds(tx, tz) || !Mirrors(map.BehaviourAt(tx, tz))) continue;
            int end = tz + 1;
            while (end < tz + 4 && map.InBounds(tx, end) && Mirrors(map.BehaviourAt(tx, end))) end++;
            if (!any) Rlgl.DisableDepthMask();
            any = true;
            CharacterSprites.DrawReflection(actor.Rig, actor.Pose, actor.Yaw, actor.Feet, pitchDeg, end - actor.Feet.Z, straight);
        }
        if (!any) return;
        Rlgl.SetTexture(0);
        Rlgl.DrawRenderBatchActive();
        Rlgl.EnableDepthMask();
    }

    /// <summary>
    /// A faint dark patch right under each character's feet. The shadow map casts their real shadow; this keeps
    /// them grounded when the sun is high or they are standing in shade.
    /// </summary>
    private void DrawContactShadows(Map map, Camera3D camera, bool withPlayer, float px, float pz, float playerGround, float lift)
    {
        var tex = SceneTextures.ShadowBlob;
        float pitchDeg = MapScene.PitchOf(map);
        Rlgl.DisableDepthMask();

        void Blob(float cx, float cz, float ground, float scale)
        {
            float rx = 0.3f * scale, rz = 0.2f * scale, y = ground + 0.015f;
            // Drawn without the scenery's shader: on raised ground it is moved by hand to lie where that ground is drawn
            if (!map.IsIndoors && ground != 0f) cx = Straighten(camera, pitchDeg, cx, y, cz);
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
            // (An item's ball has a smaller patch under it than a person, and an obstacle one a little smaller)
            if (!npc.IsPCTerminal && InSight(npc))
                Blob(npc.DrawX + 0.5f, npc.DrawY + 0.52f, npc.Level is { } level ? Relief.Under(map, npc.DrawX + 0.5f, npc.DrawY + 0.5f, level) : Relief.At(map, npc.DrawX + 0.5f, npc.DrawY + 0.5f),
                    npc.IsItemBall ? 0.6f : npc.IsObstacle ? 0.85f : 1f);
        }
        if (withPlayer) Blob(px, pz + 0.02f, playerGround, 1f - Math.Clamp(lift * 0.8f, 0f, 0.5f));

        Rlgl.SetTexture(0);
        Rlgl.DrawRenderBatchActive();
        Rlgl.EnableDepthMask();
    }
}
