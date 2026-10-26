using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Raylib_cs;
using Gesture = PokemonPlatinumEngine.Overworld.Gesture;
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
public sealed partial class WorldRenderer
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
        gymPieces = new GymPieces(context.Shaders);
    }

    // What moves in a Gym's puzzle (plan 01 · M9): the clock's hands, the fountains' water
    private readonly GymPieces gymPieces;
    private float viewer;

    private MapScene GetScene(Map map)
    {
        string key = map.Name;
        var season = map.IsIndoors ? null : SeasonOf(map);
        if (scenes.TryGetValue(key, out var stale) && stale.Season != season)
        {
            scenes.Remove(key);
            stale.Unload();
        }
        if (!scenes.TryGetValue(key, out var scene))
        {
            scene = MapScene.Build(map, shaders, season);
            scenes[key] = scene;
        }
        return scene;
    }

    /// <summary>
    /// The season a map's field is drawn in (plan 22 · U3; style guide, "Seasons"): null for a map of a region without
    /// seasons, which is every map unless the engine says otherwise. The engine answers with the season it holds for
    /// the field (<c>GameEngine.SeasonHere</c>'s), which moves only behind a fade; a scene baked in another season is let
    /// go and made again as it is next drawn.
    /// </summary>
    public Func<Map, Core.Season?> SeasonOf { get; set; } = _ => null;

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
        // The season it is baked in (plan 22 · U3)
        public Core.Season? Season;
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

        /// <summary>What the last frame drawn spent uploading and waiting (plan 16 · T21, for the soak run's causes).</summary>
        public double LastFrameMs { get; internal set; }

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

    /// <summary>
    /// The chunks of a map that can show something on a rectangle of ground (<see cref="MapScene.Reach"/>: their own
    /// tiles and what stands on them and leans out), cut off at the map's edge. A lens zoomed out asks it (plan 09 · L4).
    /// </summary>
    internal static IEnumerable<(int X, int Y)> ChunksUnder(Map map, GroundRect ground)
    {
        // (A chunk's reach is ten tiles past its tiles at the most: no chunk further off than that can touch the ground)
        int x0 = Math.Max(0, (int)MathF.Floor((ground.MinX - 10f) / Map.ChunkTiles)), x1 = Math.Min(map.ChunkColumns - 1, (int)MathF.Floor((ground.MaxX + 10f) / Map.ChunkTiles));
        int y0 = Math.Max(0, (int)MathF.Floor((ground.MinZ - 10f) / Map.ChunkTiles)), y1 = Math.Min(map.ChunkRows - 1, (int)MathF.Floor((ground.MaxZ + 10f) / Map.ChunkTiles));
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                var reach = MapScene.Reach(MapScene.ChunkWindow(map, x, y));
                if (ground.Touches(new Vector3(reach.MinX, 0, reach.MinZ), new Vector3(reach.MaxX, 0, reach.MaxZ))) yield return (x, y);
            }
    }

    /// <summary>The scenes to draw for a map this frame: its one scene, or the chunks ready round the camera.</summary>
    /// <param name="ahead">The ground a lens zoomed out shows or will show, when that is more than the block round the camera holds; null at the place's own lens.</param>
    private List<MapScene> ScenesFor(Map map, float focusX, float focusZ, GroundRect? view, GroundRect? ahead = null)
    {
        drawn.Clear();
        Streaming.LastFrameMs = 0;
        if (!map.IsStreamed)
        {
            drawn.Add(GetScene(map));
            return drawn;
        }
        // A season that has turned (Unova's, plan 22 · U3) makes every chunk of the map again
        DropOtherSeasons(map);

        // Whatever of the block isn't there yet starts baking on other threads: first the chunks the camera
        // sees, which the frame will wait for, then the rest
        for (int pass = 0; pass < 2; pass++)
            foreach (var (x, y) in ChunksAround(map, focusX, focusZ, ChunkReach))
            {
                if (chunks.ContainsKey((map, x, y))) continue;
                var window = MapScene.ChunkWindow(map, x, y);
                if (pass == 0 && view != null && !InView(window, view.Value)) continue;
                MapScene.Warm(map, window);
                chunks[(map, x, y)] = StartBaking(map, window);
            }
        // ... and so does whatever lies past the block and reaches into a wider lens's view
        if (ahead is { } wide)
            foreach (var (x, y) in ChunksUnder(map, wide))
            {
                if (chunks.ContainsKey((map, x, y))) continue;
                var window = MapScene.ChunkWindow(map, x, y);
                MapScene.Warm(map, window);
                chunks[(map, x, y)] = StartBaking(map, window);
            }

        // What the camera sees must be there now: on arriving somewhere this waits for the bake and uploads it
        // whole, behind the fade. The rest is uploaded a few steps a frame, as far as the frame's budget goes,
        // and is ready well before the player can walk into sight of it.
        streamClock.Restart();
        waitedThisFrame.Clear();

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
                    waitedThisFrame.Add(key);
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
        Streaming.LastFrameMs = spent;
        Streaming.WorstFrameMs = Math.Max(Streaming.WorstFrameMs, spent);

        Evict(map, focusX, focusZ);
        foreach (var (key, slot) in chunks)
            if (key.Map == map && slot.Scene != null) drawn.Add(slot.Scene);
        return drawn;
    }

    /// <summary>
    /// Starts making ready the chunks a camera over a spot of a streamed map will need, without drawing anything:
    /// they bake on other threads meanwhile, and are uploaded when the camera gets there. The title asks it before
    /// its opening plays again (plan 09 · L4), so the first shot doesn't begin by waiting for its ground to be baked.
    /// </summary>
    internal void MakeReady(Map map, float focusX, float focusZ)
    {
        if (!map.IsStreamed) return;
        DropOtherSeasons(map);
        foreach (var (x, y) in ChunksAround(map, focusX, focusZ, ChunkReach))
        {
            if (chunks.ContainsKey((map, x, y))) continue;
            var window = MapScene.ChunkWindow(map, x, y);
            MapScene.Warm(map, window);
            chunks[(map, x, y)] = StartBaking(map, window);
        }
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
    private ChunkSlot StartBaking(Map map, TileWindow window)
    {
        var season = SeasonOf(map);
        return new ChunkSlot { Season = season, Pending = Task.Run(() => Bake(map, window, season)) };
    }

    /// <summary>
    /// Lets go of every chunk of a map baked in another season than the one it is drawn in now (plan 22 · U3): they
    /// are made again as they come into view. The engine moves a season only behind a fade, so this is a load.
    /// </summary>
    private void DropOtherSeasons(Map map)
    {
        var season = SeasonOf(map);
        List<(Map, int, int)>? stale = null;
        foreach (var (key, slot) in chunks)
            if (key.Map == map && slot.Season != season) (stale ??= new()).Add(key);
        if (stale == null) return;
        foreach (var key in stale)
        {
            var slot = chunks[key];
            slot.Pending?.Wait();
            slot.Arriving?.Unload();
            slot.Scene?.Unload();
            chunks.Remove(key);
        }
    }

    private static MapScene.Prepared Bake(Map map, TileWindow window, Core.Season? season)
    {
        var thread = System.Threading.Thread.CurrentThread;
        var priority = thread.Priority;
        thread.Priority = System.Threading.ThreadPriority.BelowNormal;
        try
        {
            return MapScene.Prepare(map, window, season);
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
    /// as they come into view. A room is one scene, made again whole the next time it is drawn (the Villa as its
    /// furniture arrives, plan 06 · R16b).
    /// </summary>
    public void Forget(Map map, int x, int y, int width, int height)
    {
        if (!map.IsStreamed)
        {
            if (scenes.Remove(map.Name, out var whole)) whole.Unload();
            return;
        }
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

    /// <summary>
    /// Lets go of every scene and chunk drawn so far (plan 16 · T17): a scenario that rebuilds the maps begins on none
    /// of the last one's, so what it draws is what it would draw alone.
    /// </summary>
    public void ForgetAll()
    {
        foreach (var scene in scenes.Values) scene.Unload();
        scenes.Clear();
        foreach (var slot in chunks.Values)
        {
            slot.Pending?.Wait();
            slot.Arriving?.Unload();
            slot.Scene?.Unload();
        }
        chunks.Clear();
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

    /// <summary>
    /// The set pieces playing (plan 09 · L12): a machine at work, a PC switched on, a lift on its ride, someone shaken
    /// or flickering. The game tells it what begins, by <see cref="FieldLife.Now"/>; what is drawn asks it here: the
    /// room's held faces, the picture's shudder, and where each person stands on the picture or whether they do.
    /// </summary>
    public SetPieceStage Pieces { get; } = new();

    /// <summary>The frame each held face of a room's art should show now, by what the set pieces have come to.</summary>
    private void HoldFaces(MapScene room)
    {
        var (balls, lit) = Pieces.Machine(SetPiece.Heal, Life.Now);
        room.Hold(PropModels.HealerTray, SetPieces.TrayFrame(balls, lit));
        room.Hold(PropModels.HealerFront, Pieces.MachineAtWork(Life.Now) ? 1 : 0);
        room.Hold(PropModels.LiftLights, Pieces.Indicator(Life.Now));
        // Every PC that is used is dark but the one switched on
        if (Pieces.Screen(Life.Now) is { } on) room.HoldEach(PropModels.PcScreenPrefix, SetPieces.PcDark, PropModels.PcScreen(on.X, on.Y), on.Frame);
        else room.HoldEach(PropModels.PcScreenPrefix, SetPieces.PcDark);
    }

    /// <summary>
    /// Tiles of grass that something unseen shakes (the Poké Radar's patches, plan 06 · R13), and whether hard: the
    /// grass there parts as round a walker's feet, from a point that jumps from side to side on the field's clock.
    /// The game sets it every frame.
    /// </summary>
    internal List<(int X, int Y, bool Hard)> Stirring { get; } = new();

    // How far the light has gone over to snow country's (0 to 1), the field clock when it was last eased, and the map
    private float snowMix;
    private double snowClock;
    private Map? snowMap;

    // The same for a place's climate (a volcano's, a temple of ice's), with the climate last stood in
    private float climateMix;
    private double climateClock;
    private Map? climateMap;
    private AreaClimate climateNow;

    // The weather of the scene last rendered and how bright a pale thing is in its light, for the layer drawn over the picture
    private FieldWeather weather;
    private float weatherLight = 1f;

    // ------------------------------------------------------------------ the camera's own moves

    /// <summary>
    /// Where the camera has been sent to look instead of at the player, its lens, the cinema bars and the field's
    /// pace (plan 09 · L4): one state, moved on each frame by the camera's clock and read here for the target, the
    /// lens and everything that follows from them (what is in view, the chunks kept ready, the people in sight,
    /// the shadow map's reach). A script drives it, and so may anything else that holds the field.
    /// </summary>
    public CameraRig Camera { get; } = new();

    // The camera's clock, which the game runs while the field is on screen: at the clock's own pace whatever the
    // field's is, so slow motion slows nothing a scene's camera does
    private double cameraClock, lastCameraTime;
    private float easedGround = float.NaN;
    private Map? easedMap;

    /// <summary>Runs the camera's clock: the game does it beside the field's own (<see cref="FieldLife.Advance"/>).</summary>
    internal void RunCamera(float dt) => cameraClock += dt;

    /// <summary>
    /// Sends the camera to look at a spot (in tiles) over <paramref name="seconds"/>, easing out of where it is
    /// and into where it goes; it stays there until <see cref="ReleaseCamera"/>.
    /// </summary>
    public void PanCamera(float x, float z, float seconds) => Camera.Pan(x, z, seconds);

    /// <summary>Brings the camera back to the player over <paramref name="seconds"/>, and with it whatever else the rig was told.</summary>
    public void ReleaseCamera(float seconds) => Camera.Release(seconds);

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
    public float PanEase => Camera.PanEase;

    /// <summary>
    /// The lens a map is looked through this frame: the place's own (<see cref="MapScene.ViewOf"/>) under the rig's
    /// zoom and a held lens (<see cref="CameraRig.Lens"/>), which change its field of view and nothing else. Whatever
    /// asks what is in view asks this.
    /// </summary>
    private FieldView LensOf(Map? map) => LensOf(map, Camera.Lens);

    private static FieldView LensOf(Map? map, float zoom)
    {
        var lens = map != null ? MapScene.ViewOf(map) : FieldView.Outdoor;
        return lens with { FovYDeg = CameraRig.FovAt(lens.FovYDeg, zoom) };
    }

    /// <summary>How much of a room a zoom in has taken out of the picture, 0 to 1: nothing at the place's own lens or a wider one.</summary>
    private float ZoomSlack => MathF.Max(0f, 1f - 1f / Camera.Lens);

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
        bool snowbound = player != null && !indoors && !map.IsCave && !map.IsVoid && map.AreaAt(player.GridX, player.GridY)?.Snowbound == true;
        double now = Life.Now;
        snowMix = map != snowMap ? (snowbound ? 1f : 0f)
            : Math.Clamp(snowMix + (snowbound ? 1f : -1f) * (float)Math.Clamp(now - snowClock, 0, 1), 0f, 1f);
        snowClock = now;
        snowMap = map;
        if (snowMix > 0f) rig = LightRig.Lerp(rig, ArtLook.Snowbound(rig), snowMix);
        // A volcano's ash and heat and a temple's cold (style guide, "Climates") grade the light the same way, in the open
        // and in a cave alike
        var climate = player != null && !indoors && !map.IsVoid ? map.AreaAt(player.GridX, player.GridY)?.Climate ?? AreaClimate.Temperate : AreaClimate.Temperate;
        bool graded = climate != AreaClimate.Temperate;
        if (graded) climateNow = climate;
        climateMix = map != climateMap ? (graded ? 1f : 0f)
            : Math.Clamp(climateMix + (graded ? 1f : -1f) * (float)Math.Clamp(now - climateClock, 0, 1), 0f, 1f);
        climateClock = now;
        climateMap = map;
        if (climateMix > 0f) rig = LightRig.Lerp(rig, ArtLook.InClimate(rig, climateNow, map.IsCave), climateMix);
        rig = ArtLook.Weathered(rig, weather);
        // A season's light (Unova's, plan 22 · U3), in the open alone; every other region's map has none
        if (!indoors && !map.IsCave && !map.IsVoid) rig = ArtLook.Seasoned(rig, SeasonOf(map));
        var light = rig.Light;
        weatherLight = Brightness(rig);

        float time = (float)FrameClock.Now;
        float lift = player != null ? player.HopHeight / Player.TileSize : 0f;
        // The ground under the view's middle: the deck or the hillside the player is on (zero on flat maps)
        float groundY = player != null ? Relief.Under(map, px, pz, player.HeightOn(map)) : Relief.At(map, px, pz);
        // The height the player stands at: a Gym's floors above it are hidden (the Canalave Gym's)
        viewer = player?.HeightOn(map) ?? 0f;
        // A room is one scene and the camera looks at its middle; outdoors the camera follows the player
        var room = indoors ? GetScene(map) : null;

        // The camera's own time: it rises and falls after the player smoothly (a jump of more than a tile and
        // a half is an arrival somewhere else, and is taken at once), and goes where it is sent
        float step = (float)Math.Clamp(cameraClock - lastCameraTime, 0.0, 0.1);
        lastCameraTime = cameraClock;
        if (map != easedMap || float.IsNaN(easedGround) || MathF.Abs(groundY - easedGround) > 1.5f) easedGround = groundY;
        else easedGround += (groundY - easedGround) * MathF.Min(1f, step * 10f);
        easedMap = map;
        // The rig's own moves: where it has been sent, its lens, the bars and the field's pace (plan 09 · L4)
        Camera.Advance(step);
        var look = Camera.Look(new Vector2(px, pz));
        var camera = BuildCamera(map, room, look.X, look.Y, easedGround);
        // A held lens's step (a mythical Pokémon's way in, plan 09 · L14) moves the camera and what it looks at
        // together, in whole texels, in a room as in the open
        if (Camera.HeldStep is var held && held != Vector2.Zero)
        {
            var by = new Vector3(held.X, 0f, held.Y);
            camera.Position += by;
            camera.Target += by;
        }
        // A scene's shake and a lift's shudder throw the picture up and down by whole texels (plan 09 · L12)
        if (Shake(step) + Pieces.Shudder(Life.Now) / (float)CharacterSprites.TexelsPerUnit is var jolt && jolt != 0f)
        {
            camera.Position.Z += jolt;
            camera.Target.Z += jolt;
        }

        // In a cave nobody has lit, where the player stands on the picture and how large a tile is there
        dark = player != null && Darkness.Covers(map);
        darkReach = map.DarkReach;
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
        // On the sea floor, where the floor's origin would be on the picture: the waves' light is pinned to it (plan 21 · H5)
        underwater = player != null && map.IsUnderwater;
        if (underwater)
        {
            OnPicture(camera.Target, camera, out var size);
            seaPin = new Vector2(-camera.Target.X * size.X * 1920f, -camera.Target.Z * size.Y * 1080f);
        }
        // Where the player's middle is on the picture, for whatever closes on them (a cave's way into a battle)
        PlayerOnPicture = player != null ? OnPicture(new Vector3(px, groundY, pz), camera, out _) : new Vector2(0.5f, 0.5f);

        // In a dark room of the Hearthome Gym, where each light is on the picture: the player's and every trainer's
        gymLights.Clear();
        gymFog = 0f;
        if (player != null && map.Puzzle is HearthomeDoors rooms)
        {
            gymFog = rooms.Fog;
            gymLights.Add(OnPicture(new Vector3(px, groundY, pz), camera, out gymTile));
            foreach (var npc in HearthomeDoors.LightBearers(map))
            {
                float nx = npc.DrawX + 0.5f, nz = npc.DrawY + 0.5f;
                gymLights.Add(OnPicture(new Vector3(nx, Relief.At(map, nx, nz), nz), camera, out _));
            }
        }

        // Outdoors, only the part of the map near the view is drawn (rooms are small enough to draw whole)
        GroundRect? view = null, casters = null;
        if (!indoors) (view, casters) = VisibleRects(map, camera.Target, groundY, light.SunDirection);
        // A lens zoomed out, or on its way out, sees past the block of chunks kept ready round the camera: what it
        // will show is made ready as soon as the zoom is told (plan 09 · L4)
        GroundRect? ahead = !indoors && map.IsStreamed && Camera.WidestAhead < 1f
            ? VisibleRects(map, camera.Target, groundY, light.SunDirection, LensOf(map, Camera.WidestAhead)).View
            : null;
        var scenes = ScenesFor(map, camera.Target.X, camera.Target.Z, view, ahead);

        shaders.SetTime(time);
        shaders.SetWind(Weathers.Wind(weather));
        if (!indoors) SceneTextures.AnimateWaterfall(Life.Now);
        // A room's machines show what the set pieces have come to (plan 09 · L12)
        if (room != null) HoldFaces(room);
        foreach (var scene in scenes) scene.Animate(Life.Now);
        // On a map of the whole region only the people near the view are drawn
        sight = map.IsStreamed ? view : null;
        VerticalScale = vs;
        GatherActors(map, player, px, pz, groundY, lift, time);
        foreach (var actor in actors) CharacterSprites.Prepare(context, actor.Rig, actor.Pose, actor.Yaw);
        foreach (var p in pokemon) FieldSprites.Prepare(context, p.Name, p.Facing, ref fieldBakes);
        fieldBakes = 1;

        // Grass leans away from everyone on the map (set after the sprite bakes, which clear it)
        walkerFeet.Clear();
        // Shaking grass first, so the people are among the last eight the shader keeps
        int shake = (int)(Life.Now * 12.0) % 2 == 0 ? 1 : -1;
        foreach (var (sx, sy, hard) in Stirring)
        {
            float cx = sx + 0.5f + shake * (hard ? 7f : 4f) / 32f, cz = sy + 0.5f;
            walkerFeet.Add(new Vector3(cx, Relief.At(map, sx + 0.5f, cz), cz));
        }
        foreach (var actor in actors) walkerFeet.Add(actor.Feet);
        shaders.SetWalkers(walkerFeet);

        // 1. Shadow map: depth of everything that casts shadows, seen from the sun (or moon)
        // (a room's shadows are cast round its middle; a hall followed across its width takes them along)
        var focus = room != null ? room.RoomCenter with { X = camera.Target.X, Y = 0 } : new Vector3(camera.Target.X, groundY, camera.Target.Z - 2f);
        // (A lens zoomed out shows more ground than the map covers at its own reach: it is spread over the wider view)
        float shadowReach = indoors ? 18f : 40f;
        if (!indoors && Camera.Lens < 1f) shadowReach /= Camera.Lens;
        var lightCamera = context.Shadows.LightCamera(focus, light.SunDirection, shadowReach);

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
        gymPieces.DrawDepth(map, viewer);
        DrawActors(CharacterPass.Depth);
        DrawThings(CharacterPass.Depth);
        DrawPokemon(CharacterPass.Depth);
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

        // 1b. The mirror (plan 09 · L10): what stands by the water, drawn once more through the view mirrored about the
        // water's plane, for the water to read. Nothing at all where no water is in view or the preset doesn't reflect
        float? plane = !indoors && view is { } seen ? Reflections.PlaneIn(map, seen, new Vector2(camera.Target.X, camera.Target.Z)) : null;
        bool mirroring = context.HasMirror && Reflections.Wanted(context.Quality, map, plane);
        if (mirroring) DrawMirror(scenes, camera, view!.Value, plane!.Value, pitchDeg);
        else shaders.SetWaterMirror(false, Matrix4x4.Identity, 0f, pitchDeg);
        foreach (var scene in scenes) scene.MirrorWater(mirroring ? context.Mirror.Texture : default);
        Mirrored = mirroring ? plane : null;
        FrameProfiler.Lap(FrameSection.Reflections);

        // 2. The scene itself, sampling the shadow map
        float near = indoors ? 1f : 10f, far = indoors ? 100f : 140f;
        context.BeginScene();
        Raylib.ClearBackground(rig.Background);
        Rlgl.SetClipPlanes(near, far);

        Raylib.BeginMode3D(camera);
        Rlgl.DisableBackfaceCulling();
        context.BindShadowMap();

        foreach (var scene in scenes)
            if (Reaches(scene, view)) scene.Draw(view);
        gymPieces.Draw(map, viewer);
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
        // Where the place's light is strongly coloured, people and Pokémon keep their own colours (style guide,
        // "Climates"); the things of the map, drawn before them, are lit as the rock they stand among is
        if (rig.PeopleWash > 0f) shaders.SetPeopleLight(ArtLook.ForPeople(light, rig.PeopleWash));
        DrawPokemon(CharacterPass.Color);
        DrawActors(CharacterPass.Color);
        DrawLife(map, camera, rig, upright: true);
        DrawBubbles(map, player, camera, rig, indoors, pitchDeg, vs);
        // The field's rules as layers, when a developer has turned any on (plan 16 · T18)
        if (Layers != DebugLayer.None || LastLayers != null) DrawDebugLayers(map, camera, view);

        Rlgl.EnableBackfaceCulling();
        Raylib.EndMode3D();
        Raylib.EndTextureMode();

        context.UnbindShadowMap();
        Rlgl.SetClipPlanes(0.01, 1000.0);
        // The programs are shared: a battle's water (or the title's) must not read the field's mirror
        if (mirroring) shaders.SetWaterMirror(false, Matrix4x4.Identity, 0f, pitchDeg);
        FrameProfiler.Lap(FrameSection.Scene);
        context.PreparePost(rig.Post, new DepthRange(near, far, camera.FovY, (float)width / height), aoRadius: 0.45f);
    }

    /// <summary>
    /// Composites the last rendered frame into the current target with tilt-shift blur and grading, and draws
    /// the weather over it.
    /// </summary>
    /// <param name="wavers">A ripple from outside the field's own life (the way into a battle on water, plan 09 · L13): the further-reaching of it and a scene's own plays.</param>
    public void DrawToScreen(int destWidth, int destHeight, SceneRipple wavers = default)
    {
        // A scene's ripple is read by the composite's own pass, and a burst's flash whitens everything over it
        var ripple = Life.Ripple;
        context.Composite(new Rectangle(0, 0, destWidth, destHeight), wavers.Reach > ripple.Reach ? wavers : ripple);
        if (underwater) DrawCaustics(destWidth, destHeight);
        DrawWeather(destWidth, destHeight);
        if (dark) DrawDarkness(destWidth, destHeight);
        if (gymFog > 0f) DrawGymFog(destWidth, destHeight);
        if (Life.Flash is > 0f and var flash)
            Raylib.DrawRectangle(0, 0, destWidth, destHeight, new Color(255, 255, 255, (int)(flash * 255f)));
        DrawLayerLabels(destWidth, destHeight);
    }

    /// <summary>
    /// Where the player's middle was on the picture the last time the field was rendered, in shares of its width and
    /// height: the picture's own middle where nobody is followed.
    /// </summary>
    public Vector2 PlayerOnPicture { get; private set; } = new(0.5f, 0.5f);

    private bool dark;
    private float darkReach = Darkness.Reach;
    private Vector2 darkAt, darkTile;

    /// <summary>
    /// The dark of an unlit cave (style guide, "Caves"): the picture falls to the dark's colour everywhere but in
    /// a circle of the floor round the player, which lies on the ground and so is wider than it is tall.
    /// </summary>
    private void DrawDarkness(int destWidth, int destHeight)
    {
        float cx = darkAt.X * destWidth, cy = darkAt.Y * destHeight;
        float halfW = darkReach * darkTile.X * destWidth, halfH = darkReach * darkTile.Y * destHeight;
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

    private float gymFog;
    private Vector2 gymTile;
    private readonly List<Vector2> gymLights = new();

    /// <summary>Where a point of the ground is on the picture (0 to 1 each way), round a person's middle, and how large a tile is there.</summary>
    private static Vector2 OnPicture(Vector3 feet, Camera3D camera, out Vector2 tile)
    {
        var at = Raylib.GetWorldToScreenEx(feet, camera, 1920, 1080);
        var east = Raylib.GetWorldToScreenEx(feet + Vector3.UnitX, camera, 1920, 1080);
        var south = Raylib.GetWorldToScreenEx(feet + Vector3.UnitZ, camera, 1920, 1080);
        tile = new Vector2((east.X - at.X) / 1920f, (south.Y - at.Y) / 1080f);
        return new Vector2(at.X / 1920f, (at.Y - 0.5f * (south.Y - at.Y)) / 1080f);
    }

    /// <summary>
    /// The dark of the Hearthome Gym's rooms (style guide, "Gyms"): a black fog over the whole picture, not quite
    /// opaque (<see cref="HearthomeDoors.Fog"/>), lifted round the player and round each trainer, each light a circle
    /// of the floor. Drawn as a grid whose corners take the fog of the nearest light, so any number of lights join.
    /// </summary>
    private void DrawGymFog(int destWidth, int destHeight)
    {
        const int cell = 20;
        int cols = (destWidth + cell - 1) / cell, rows = (destHeight + cell - 1) / cell;
        if (gymCorners.Length < (cols + 1) * (rows + 1)) gymCorners = new byte[(cols + 1) * (rows + 1)];
        for (int j = 0; j <= rows; j++)
            for (int i = 0; i <= cols; i++)
            {
                float u = i * cell / (float)destWidth, v = j * cell / (float)destHeight, fog = 1f;
                foreach (var light in gymLights)
                {
                    float dx = (u - light.X) / gymTile.X, dy = (v - light.Y) / gymTile.Y;
                    fog = MathF.Min(fog, HearthomeDoors.FogAt(MathF.Sqrt(dx * dx + dy * dy)));
                }
                gymCorners[j * (cols + 1) + i] = (byte)MathF.Round(fog * gymFog * 255f);
            }

        var (r, g, b) = Darkness.Colour;
        for (int j = 0; j < rows; j++)
        {
            Rlgl.CheckRenderBatchLimit(4 * cols);
            Rlgl.Begin(DrawMode.Quads);
            float y0 = j * cell, y1 = MathF.Min(destHeight, y0 + cell);
            for (int i = 0; i < cols; i++)
            {
                float x0 = i * cell, x1 = MathF.Min(destWidth, x0 + cell);
                int k = j * (cols + 1) + i;
                Rlgl.Color4ub(r, g, b, gymCorners[k]); Rlgl.Vertex2f(x0, y0);
                Rlgl.Color4ub(r, g, b, gymCorners[k + cols + 1]); Rlgl.Vertex2f(x0, y1);
                Rlgl.Color4ub(r, g, b, gymCorners[k + cols + 2]); Rlgl.Vertex2f(x1, y1);
                Rlgl.Color4ub(r, g, b, gymCorners[k + 1]); Rlgl.Vertex2f(x1, y0);
            }
            Rlgl.End();
        }
    }

    private byte[] gymCorners = Array.Empty<byte>();

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

    private bool underwater;
    private Vector2 seaPin;
    private readonly List<HazeLayer> causticLayers = new();

    /// <summary>
    /// The light of the waves over the sea floor (plan 21 · H5; style guide, "The sea floor"): the caustic net added
    /// over the picture in two layers, point-filtered and held to the field's texels, pinned to the floor
    /// (<see cref="WeatherFx.Caustics"/>) and dimmed by the scene's light as the mist is.
    /// </summary>
    private void DrawCaustics(int destWidth, int destHeight)
    {
        causticLayers.Clear();
        float wide = destWidth / 1920f, tall = destHeight / 1080f;
        WeatherFx.Caustics(Life.Now, seaPin.X * wide, seaPin.Y * tall, causticLayers);
        var texture = SceneTextures.Caustics;
        Raylib.BeginBlendMode(BlendMode.Additive);
        foreach (var layer in causticLayers)
        {
            var source = new Rectangle(-layer.OffsetX / layer.Scale, -layer.OffsetY / layer.Scale, destWidth / layer.Scale, destHeight / layer.Scale);
            Raylib.DrawTexturePro(texture, source, new Rectangle(0, 0, destWidth, destHeight), Vector2.Zero, 0f, Dimmed(layer.Tint, weatherLight));
        }
        Raylib.EndBlendMode();
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
            // A room with relief (a Gym's tiers) is followed up and down them as the field is
            if (map.HasRelief) roomTarget.Y += groundY;
            int depth = room.RoomDepth;
            float distance = MapScene.IndoorDistance * Math.Max(1f, (Math.Min(depth, DeepestRoom) - 1) / 8f);
            // A zoom in takes a share of the room out of the picture, and by that share the camera may look where it
            // is sent (or at the player) instead of at the room's middle (style guide, "The camera kit")
            float slack = ZoomSlack;
            if (depth > DeepestRoom || slack > 0f)
            {
                float reach = MathF.Max(0f, depth - DeepestRoom) / 2f + slack * Math.Min(depth, DeepestRoom) / 2f;
                roomTarget.Z = SnapToTexel(Math.Clamp(pz, roomTarget.Z - reach, roomTarget.Z + reach));
            }
            // A hall wider than the picture (the floors of Team Galactic's headquarters, plan 02 · S11) is followed
            // across its width the same way, between its side walls
            if (room.RoomWidth > WidestRoom || slack > 0f)
            {
                float reach = (room.RoomWidth > WidestRoom ? (room.RoomWidth - HallInView) / 2f : 0f) + slack * Math.Min(room.RoomWidth, HallInView) / 2f;
                roomTarget.X = SnapToTexel(Math.Clamp(px, roomTarget.X - reach, roomTarget.X + reach));
            }
            return new Camera3D(roomTarget + dir * distance, roomTarget, Vector3.UnitY,
                Camera.Fov(MapScene.IndoorFovYDeg), CameraProjection.Perspective);
        }

        // Aim at the player's middle so they sit near the centre of the screen, then keep the view
        // inside the map plus a few tiles of the surrounding forest
        var t = new Vector3(px, groundY + 0.6f * MapScene.VerticalScaleOf(map), pz);
        var lens = LensOf(map);
        var (farOff, nearOff, halfW) = VisibleGround(t.Y - groundY, lens);
        // A cave ends at its rock and the Distortion World at its edge: the view stays inside them. The open country
        // shows a few tiles of forest past its edge
        float overscan = map.IsCave || map.IsVoid ? 0f : 4f;
        t.X = ClampOrCenter(t.X, -overscan + halfW, map.Width + overscan - halfW);
        t.Z = ClampOrCenter(t.Z, -overscan - farOff, map.Height + overscan - nearOff);

        // Scroll in whole texels of the pixel art, so textures and sprites don't shimmer as the view moves
        t.X = SnapToTexel(t.X);
        t.Z = SnapToTexel(t.Z);

        return new Camera3D(t + dir * lens.Distance, t, Vector3.UnitY, lens.FovYDeg, CameraProjection.Perspective);
    }

    /// <summary>The deepest room the camera frames whole: a deeper hall is followed along its length.</summary>
    public const int DeepestRoom = 14;

    /// <summary>The widest room the camera frames whole, in tiles of floor: a wider hall is followed across its width.</summary>
    public const int WidestRoom = 30;

    /// <summary>How much of such a hall's width the picture holds at the deepest room's distance, in tiles.</summary>
    public const int HallInView = 26;

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
    internal (GroundRect View, GroundRect Casters) VisibleRects(Map? map, Vector3 target, float groundY, Vector3 sunDirection) =>
        VisibleRects(map, target, groundY, sunDirection, LensOf(map));

    /// <param name="lens">The lens looked through: the place's own under the rig's zoom, or under the zoom it is on its way to.</param>
    private (GroundRect View, GroundRect Casters) VisibleRects(Map? map, Vector3 target, float groundY, Vector3 sunDirection, FieldView lens)
    {
        const float tallest = 5f;
        var (farOff, nearOff, halfW) = VisibleGround(target.Y - groundY, lens);
        // Things are a tile or two wide, and a tall tree south of the bottom edge still shows its top
        var view = new GroundRect(target.X - halfW - 2f, target.Z + farOff - 2f, target.X + halfW + 2f, target.Z + nearOff + 4f);
        if (map is { HasRelief: true })
        {
            // A tile of height shows as far up the screen as six tenths of a tile of ground (the camera's pitch). The
            // sight line along the view's top edge goes on north and down, so only lower ground north of that edge
            // shows more of itself; the one along its bottom edge comes from the south and above, so only higher ground
            // south of that edge rises into it. Each side asks only its own ground: high ground in the north must not
            // widen the view to the south (the Battle Frontier's sunken plaza, between the Tower's terrace and the
            // gate's, drew the terrace under the camera when the two were asked as one, plan 01 · M10)
            const int look = 10;
            float perTile = 1f / MathF.Tan(lens.PitchDeg * MathF.PI / 180f);
            var (low, _) = Relief.Range(map, (int)view.MinX, (int)view.MinZ - look, (int)view.MaxX + 1, (int)view.MinZ + 1);
            var (_, high) = Relief.Range(map, (int)view.MinX, (int)view.MaxZ, (int)view.MaxX + 1, (int)view.MaxZ + look);
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

    // Whoever may be in sight this frame: a list of the renderer's own, so a frame makes none
    private readonly List<NPC> peopleInSight = new();

    /// <summary>
    /// The people of a map who may be in sight (plan 01 · M12): on a streamed map those standing on the ground drawn
    /// this frame, a tile more than <see cref="InSight"/> allows for (someone in the middle of a step is drawn up to
    /// a tile from where they stand), found by where they stand and never by going through everyone the map of
    /// Sinnoh holds; anywhere else, everyone. In the map's own order.
    /// </summary>
    private List<NPC> PeopleInSight(Map map)
    {
        if (sight is { } s)
            return map.NPCs.In((int)MathF.Floor(s.MinX) - 3, (int)MathF.Floor(s.MinZ) - 3, (int)MathF.Ceiling(s.MaxX) + 3, (int)MathF.Ceiling(s.MaxZ) + 3, peopleInSight);
        peopleInSight.Clear();
        foreach (var npc in map.NPCs) peopleInSight.Add(npc);
        return peopleInSight;
    }

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

    // The Pokémon the player rides on water, when there is one to draw: the swimmer every Pokémon shares, or the
    // carrier's own field sprite (plan 10 · F1)
    private (Vector3 At, float Yaw, FieldSprites.Strip? Own)? mount;

    // The Pokémon standing in the field in view (plan 10 · F1), and how many field sprites a frame may still bake
    private readonly List<(Vector3 At, string Name, int Facing, int Frame)> pokemon = new();
    private int fieldBakes = 1;

    /// <summary>Whether someone is drawn this frame (nobody is the player): not at the moments a flicker has them unseen (plan 09 · L12).</summary>
    private bool Seen(NPC? who) => !Pieces.AnyoneMoved || Pieces.Seen(who, Life.Now);

    /// <summary>How far east of their tile someone is drawn this frame, in tiles: the whole texels a shake throws them (plan 09 · L12).</summary>
    private float NudgeOf(NPC? who) => Pieces.AnyoneMoved ? Pieces.OffsetOf(who, Life.Now) / (float)CharacterSprites.TexelsPerUnit : 0f;

    // Where on the field's clock the swimmers' strokes were last looked for (plan 11 · C8)
    private double strokesSince;

    private void GatherActors(Map map, Player? player, float px, float pz, float groundY, float lift, float time)
    {
        actors.Clear();
        mount = null;
        things.Clear();
        berryPlants.Clear();
        apricornTrees.Clear();
        pokemon.Clear();
        // The strokes swimmers have taken since the last frame drawn are looked for in what has passed of the field's
        // clock since then (plan 11 · C8); a clock that has gone back (a new place) starts again from now
        if (strokesSince > Life.Now) strokesSince = Life.Now;
        foreach (var npc in PeopleInSight(map))
        {
            // (Something in the way that nobody sees is drawn as nothing: plan 21 · H6's Kecleon)
            if (npc.IsPCTerminal || npc.IsUnseen || !InSight(npc)) continue;
            if (map.Puzzle?.Hides(npc.Level ?? map.HeightAt(npc.GridX, npc.GridY), viewer) == true) continue;
            // Whoever a scene shakes is drawn a few texels east or west of their tile, and whoever flickers is not
            // drawn at the moments they don't show (plan 09 · L12; style guide, "Life", "Set pieces")
            if (!Seen(npc)) continue;
            float nudge = NudgeOf(npc);
            if (npc is { IsPokemon: true, Species: { } species })
            {
                // A Pokémon of the map stands on its tile as a person does; on the move it hops a texel or two, a hop to
                // each tile, and shows its second frame while it is off the ground (style guide, "Pokémon in the field")
                float sx = npc.DrawX + 0.5f, sz = npc.DrawY + 0.5f;
                bool moving = npc.WalkBlend >= 0.5f, up = moving && MathF.Sin(npc.WalkCycle * MathF.Tau * 2f) > 0f;
                float hop = up ? Rows(2f) : 0f;
                float ground = (npc.Level is { } deck ? Relief.Under(map, sx, sz, deck) : Relief.At(map, sx, sz)) - SinkAt(map, sx, sz);
                pokemon.Add((new Vector3(sx + nudge, ground + hop, sz), species, CharacterSprites.FacingIndex(Player.YawOf(npc.Facing)),
                    moving ? (up ? 1 : 0) : FieldSprites.FrameAt(time + SeedOf(npc.Name + npc.Key) * 10f)));
                continue;
            }
            if (npc.IsBerrySoil)
            {
                // A patch of soft soil and what grows in it, as the game last told it (plan 06 · R14a)
                float bx = npc.DrawX + 0.5f, bz = npc.DrawY + 0.5f;
                var (stage, berry, mulched) = npc.BerryLook;
                berryPlants.Add((new Vector3(bx, Relief.At(map, bx, bz), bz), stage, berry, mulched));
                continue;
            }
            if (npc.IsApricornTree)
            {
                // An Apricorn tree with its fruit on or off, as the game last told it (plan 20 · J6)
                float ax = npc.DrawX + 0.5f, az = npc.DrawY + 0.5f;
                apricornTrees.Add((new Vector3(ax, Relief.At(map, ax, az), az), npc.ApricornColour, npc.ApricornInFruit));
                continue;
            }
            // A snowball of the Snowpoint Gym is a card of the scenery, drawn with the room's pieces (GymPieces)
            if (npc.IsSnowball || npc.IsLeagueDoor) continue;
            if (npc.IsThing)
            {
                // An item in its ball and an obstacle are cards, not people: an obstacle stands a little south of
                // its tile's middle, as the props it once was did
                var kind = npc.Obstacle ?? npc.GymThing ?? PropType.Mailbox;
                float bx = npc.DrawX + 0.5f, bz = npc.DrawY + ThingCards.FootOf(kind);
                // A boulder over a pit of the Distortion World sinks into its mouth (plan 02 · S12)
                bool overPit = map.BoulderPits.ContainsKey((npc.GridX, npc.GridY)) && map.GetGroundTile(npc.GridX, npc.GridY) == TileType.Void;
                float ground = overPit ? Relief.InPit(map, npc.GridX, npc.GridY, MathF.Abs(npc.StepOffsetX) + MathF.Abs(npc.StepOffsetY)) : Relief.At(map, bx, bz);
                // A thing laid on furniture stands that far over the floor (plan 19 · K3: Oak's three Poké Balls on his table)
                if (!overPit) ground += npc.RaisedBy;
                things.Add((new Vector3(bx + nudge, ground, bz), kind));
                continue;
            }
            float seed = SeedOf(npc.Name);
            var pose = new CharacterPose { Walk = npc.WalkCycle, WalkBlend = npc.WalkBlend, Time = time + seed * 10f, Blink = IsBlinking(time, seed) };
            // A gesture and a face a script gave them (plan 11 · C11)
            float jump = Acted(ref pose, npc.Gesture, npc.GestureAge, npc.Mood);
            if (npc.HasSpottedPlayer && npc.ExclamationTimer > 0f)
            {
                // A trainer who has just spotted the player starts, under the "!"
                pose.Emote = Emote.Surprised;
                pose.EmoteTime = TrainerApproach.ExclaimTime - npc.ExclamationTimer;
                pose.Hop = 0f;
                pose.Blink = false;
                jump = 0f;
            }
            float nx = npc.DrawX + 0.5f, nz = npc.DrawY + 0.5f;
            var rig = CharacterModels.Get(PlayerIdentity.CharacterFor(npc.NpcType), shaders);
            var at = new Vector3(nx + nudge, (npc.Level is { } level ? Relief.Under(map, nx, nz, level) : Relief.At(map, nx, nz)) - SinkAt(map, nx, nz) - Rows(npc.Wading) + Rows(jump), nz);
            // Sitting or lying on their furniture, never in it (plan 11 · C7; style guide, "Characters", "Stances")
            pose.Stance = map.StanceOf(npc);
            if (pose.Stance == Overworld.Stance.Swim)
            {
                // A swimmer's strokes go by the field's own clock, a ring behind each (plan 11 · C8)
                float phase = seed * Seats.StrokeSeconds;
                pose.Time = (float)(Life.Now - phase);
                Life.Strokes(map, nx + nudge, nz, npc.Facing, strokesSince, Life.Now, phase);
            }
            if (pose.Stance != Overworld.Stance.Stand) at = Seated(rig, pose.Stance, at, npc.Facing, map.SeatUnder(npc), npc.Level != null, MapScene.PitchOf(map));
            actors.Add(new Actor(rig, at, Player.YawOf(npc.Facing), pose));
        }
        strokesSince = Life.Now;

        // The player too may be shaken or flicker (nobody stands for them)
        if (player == null || !Seen(null)) return;

        var playerPose = new CharacterPose
        {
            Walk = player.WalkCycle,
            WalkBlend = player.WalkBlend,
            Running = player.IsRunning,
            Hop = player.HopProgress,
            Time = time,
            Blink = IsBlinking(time, 0.37f)
        };
        // A gesture and a face a script gave the player (plan 11 · C11); a ledge's hop goes before a jump on the spot
        float playerJump = Acted(ref playerPose, player.HopProgress > 0f ? null : player.Gesture, player.GestureAge, player.Mood);
        // On the water the player sits on a Pokémon's back, and the two bob together, a texel up and a texel down.
        // The Pokémon is the one whose Surf it is, once its field sprite is ready (plan 10 · F1); the swimmer every
        // Pokémon shares stands in meanwhile
        float saddle = player.Saddle;
        float bob = player.Mount != null && MathF.Floor(time * 2.4f) % 2f == 0f ? Rows(1f) : 0f;
        FieldSprites.Strip? carrier = null;
        if (player.Mount != null && player.Carrier is { } carried)
        {
            int facing = CharacterSprites.FacingIndex(player.Yaw);
            FieldSprites.Prepare(context, carried, facing, ref fieldBakes);
            carrier = FieldSprites.Get(carried, facing);
        }
        float seat = carrier != null ? CarrierSeat(carrier) : SurfMount.Seat;
        float feet = groundY + lift + Rows(seat) * saddle + bob * saddle - SinkAt(map, px, pz) * (1f - saddle) + Rows(playerJump) - Rows(Life.PlankRows(map, px, pz)) * (1f - saddle);
        // Fishing, the player holds the rod (plan 11 · C8): their own look holding one
        bool fishing = player.Posed is Overworld.Stance.Fish or Overworld.Stance.Tug;
        // (built in the background the first time a cast is made; the plain look stands in until it is ready)
        var playerRig = (fishing ? CharacterModels.TryGet(CharacterStyle.Holding(PlayerIdentity.Character, HeldProp.Rod), shaders) : null)
            ?? CharacterModels.Get(PlayerIdentity.Character, shaders);
        var playerAt = new Vector3(px + NudgeOf(null), feet, pz);
        // A stance a script set them in, while they stand still on foot (plan 11 · C7): a step stands them up again
        if (player.Posed is { } posed && player.Mount == null && player.WalkBlend < 0.5f && player.HopProgress <= 0f)
        {
            playerPose.Stance = posed;
            playerAt = Seated(playerRig, posed, playerAt, player.Facing, map.SeatAt(player.GridX, player.GridY), false, MapScene.PitchOf(map));
        }
        actors.Add(new Actor(playerRig, playerAt, player.Yaw, playerPose));

        if (player.Mount is { } ridden)
        {
            float mx = ridden.X + 0.5f, mz = ridden.Y + 0.5f;
            // A little nearer the camera than its rider, so its back covers the rider's shoes
            float sunk = carrier != null ? CarrierSunk(carrier) + FieldSprites.FootRows : 2f;
            mount = (new Vector3(mx, Relief.At(map, mx, mz) + bob - Rows(sunk), mz + 0.06f), player.Yaw, carrier);
        }
    }

    /// <summary>How many rows of a Pokémon's field sprite are under the water while it carries someone: a fifth of it.</summary>
    internal static int CarrierSunk(FieldSprites.Strip carrier) => (int)MathF.Round(carrier.Rows * 0.2f);

    /// <summary>How many rows above the water its rider's feet are lifted: a little over half of what shows of it.</summary>
    internal static int CarrierSeat(FieldSprites.Strip carrier) => (int)MathF.Round((carrier.Rows - CarrierSunk(carrier)) * 0.55f);

    private static bool IsBlinking(float time, float seed) => (time + seed * 7.3f) % 4.1f < 0.13f;

    /// <summary>
    /// Where the card of someone in a stance stands (plan 11 · C7; style guide, "Characters", "Stances"), from where
    /// they would stand: a sitter's lifted until the hips, at the knees' height once seated, are at the seat's and
    /// moved along the way they face so the legs hang in front of its edge; someone on the ground lifted onto the
    /// cushion and moved forward as a sitter is; a lier in a bed under its covers, their card on the pillow sunk until
    /// the neck is at the mattress's top (the camera's <paramref name="pitchDeg"/> says how far upright cards are
    /// stretched); a lier on the floor where they are. Whoever already stands at a height of their own
    /// (<paramref name="raised"/>, a <c>level</c>) is lifted no more.
    /// </summary>
    internal static Vector3 Seated(CharacterRig rig, Overworld.Stance stance, Vector3 at, Data.Direction facing, Prop? seat, bool raised, float pitchDeg)
    {
        var (dx, dz) = FieldMovement.Delta(facing);
        float height = raised || seat == null ? 0f : Seats.HeightOf(seat.Type);
        switch (stance)
        {
            case Overworld.Stance.Sit:
            {
                float hips = rig.Skeleton[HumanBones.ShinL].Joint.Y * rig.Scale;
                float forward = Seats.ForwardOn(seat?.Type);
                return at + new Vector3(dx * forward, Math.Max(0f, height - hips), dz * forward);
            }
            case Overworld.Stance.SitGround:
                return at + new Vector3(dx * Seats.ForwardOn(seat?.Type), height, dz * Seats.ForwardOn(seat?.Type));
            case Overworld.Stance.Lie:
            {
                // In a bed, under its covers: the card stands on the pillow, sunk until the neck is at the mattress's
                // top, which hides the body in front of it and leaves the head on the pillow. The neck stands as high
                // on the card as the sprite's camera, 24° above, sees it behind the feet, stretched as every upright
                // card is (1 / cos of the field's pitch)
                if (seat == null) return at;
                float neck = rig.Skeleton[HumanBones.Head].Joint.Y * rig.Scale;
                float onCard = (CharacterAnimation.LieLift * MathF.Cos(CharacterSprites.ViewPitchDeg * MathF.PI / 180f) + neck * MathF.Sin(CharacterSprites.ViewPitchDeg * MathF.PI / 180f)) / MathF.Cos(pitchDeg * MathF.PI / 180f);
                return at + new Vector3(-dx * 0.2f, height - onCard + 0.02f, -dz * 0.2f);
            }
            case Overworld.Stance.Swim:
            {
                // Sunk until the sprite's water line is at the water's surface: the rows under it on the card,
                // stretched as every upright card is, and the margin the card leaves under the feet (plan 11 · C8)
                float vs = 1f / MathF.Cos(pitchDeg * MathF.PI / 180f);
                float onCard = ((CharacterSprites.SpriteH - CharacterSprites.WaterLineRow(rig)) / (float)CharacterSprites.TexelsPerUnit - 0.05f) * vs;
                return at - new Vector3(0, onCard, 0);
            }
            default:
                return at;
        }
    }

    /// <summary>
    /// Poses someone by the gesture and the mood a script gave them (plan 11 · C11): the gesture's round as an emote
    /// over the idle, or the hop played on the spot; the mood as the face. Gives how many texels a jump lifts them.
    /// </summary>
    internal static float Acted(ref CharacterPose pose, Gesture? gesture, float age, Mood mood)
    {
        pose.Expression = CharacterAnimation.ExpressionOf(mood);
        if (gesture is not { } playing) return 0f;
        float round = Acting.RoundAge(playing, age);
        CharacterAnimation.Act(ref pose, playing, round);
        return playing == Gesture.Jump ? CharacterAnimation.JumpLift(round) : 0f;
    }

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

    // The berry patches in view, and a card for each look one has had (plan 06 · R14a)
    private readonly List<(Vector3 At, BerryStage Stage, string? Berry, bool Mulched)> berryPlants = new();
    private readonly Dictionary<string, CharacterSprites.Card> berryCards = new();

    // The Apricorn trees in view, and a card for each colour with its fruit on and one bare (plan 20 · J6)
    private readonly List<(Vector3 At, Models.Apricorn? Colour, bool InFruit)> apricornTrees = new();
    private readonly Dictionary<string, CharacterSprites.Card> apricornCards = new();

    /// <summary>The things on the ground: a card for each kind, lit and casting like the people.</summary>
    private void DrawThings(CharacterPass pass)
    {
        foreach (var (at, kind) in things)
        {
            if (!thingCards.TryGetValue(kind, out var card)) thingCards[kind] = card = CharacterSprites.MakeCard(context, ThingCards.Paint(kind));
            CharacterSprites.DrawCard(card, at, VerticalScale, pass);
        }
        foreach (var (at, stage, berry, mulched) in berryPlants)
        {
            string key = BerryArt.KeyOf(stage, berry, mulched);
            if (!berryCards.TryGetValue(key, out var card)) berryCards[key] = card = CharacterSprites.MakeCard(context, BerryArt.Paint(stage, berry, mulched));
            CharacterSprites.DrawCard(card, at, VerticalScale, pass);
        }
        foreach (var (at, colour, inFruit) in apricornTrees)
        {
            string key = ApricornArt.KeyOf(colour, inFruit);
            if (!apricornCards.TryGetValue(key, out var card)) apricornCards[key] = card = CharacterSprites.MakeCard(context, ApricornArt.Paint(colour, inFruit));
            CharacterSprites.DrawCard(card, at, VerticalScale, pass);
        }
    }

    private void DrawActors(CharacterPass pass)
    {
        foreach (var actor in actors)
            CharacterSprites.DrawBillboard(context, actor.Rig, actor.Pose, actor.Yaw, actor.Feet, VerticalScale, pass);
        if (mount is { Own: { } own } carried)
            CharacterSprites.DrawCard(own.Frames[0], carried.At, VerticalScale, pass);
        else if (mount is { } m) SurfMount.Draw(context, m.At, m.Yaw, VerticalScale, pass);
    }

    /// <summary>
    /// The Pokémon standing in the field: each its field sprite, its lowest point on the ground, or a Poké Ball while
    /// the sprite isn't ready (plan 10 · F1).
    /// </summary>
    private void DrawPokemon(CharacterPass pass)
    {
        foreach (var (at, name, facing, frame) in pokemon)
        {
            if (FieldSprites.Get(name, facing) is { } strip)
            {
                CharacterSprites.DrawCard(strip.Frames[frame], at - new Vector3(0, Rows(FieldSprites.FootRows), 0), VerticalScale, pass);
                continue;
            }
            if (!thingCards.TryGetValue(PropType.Mailbox, out var ball)) thingCards[PropType.Mailbox] = ball = CharacterSprites.MakeCard(context, ThingCards.Paint(PropType.Mailbox));
            CharacterSprites.DrawCard(ball, at, VerticalScale, pass);
        }
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

        foreach (var npc in PeopleInSight(map))
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
            sceneQuads.Clear();
            Life.SceneQuads(sceneQuads);
        }
        var quads = upright ? lifeUpright : lifeFlat;
        float pitchDeg = MapScene.PitchOf(map), vs = MapScene.VerticalScaleOf(map);
        Camera3D? straight = map.IsIndoors ? null : camera;
        DrawSceneEffects(map, camera, upright, pitchDeg, vs, straight);
        if (quads.Count == 0) return;

        var atlas = SceneTextures.Life;
        float lit = Brightness(rig);
        const float cell = FieldLife.Cell;
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

    private readonly List<SceneQuad> sceneQuads = new();

    /// <summary>
    /// The story's scene effects (style guide, "Life", "Scene effects"): cells of their own atlas, lying on the
    /// ground or standing up, drawn undimmed because they are light (the shadow is dark by its own tint).
    /// </summary>
    private void DrawSceneEffects(Map map, Camera3D camera, bool upright, float pitchDeg, float vs, Camera3D? straight)
    {
        if (sceneQuads.Count == 0) return;
        var atlas = SceneTextures.Effects;
        const float cell = SceneFx.Cell;
        Rlgl.DisableDepthMask();
        foreach (var q in sceneQuads)
        {
            if (q.Flat == upright) continue;
            var src = new Rectangle(q.Cell % SceneFx.Columns * cell, q.Cell / SceneFx.Columns * cell, cell, cell);
            if (upright)
            {
                DrawUpright(atlas, src, SnapToTexel(q.At.X), q.At.Z, q.At.Y + q.Lift * vs, q.Width, q.Height * vs, q.Tint, straight, pitchDeg);
                continue;
            }

            float cx = q.At.X;
            if (!map.IsIndoors && q.At.Y > 0.05f) cx = Straighten(camera, pitchDeg, cx, q.At.Y, q.At.Z);
            float hw = q.Width / 2f, hd = q.Height / 2f;
            float u0 = src.X / atlas.Width, u1 = (src.X + src.Width) / atlas.Width, v0 = src.Y / atlas.Height, v1 = (src.Y + src.Height) / atlas.Height;
            // Turned by quarter turns, as a footprint is, so a shadow heads the way it is sent
            (float U, float V)[] uv = { (u0, v1), (u1, v1), (u1, v0), (u0, v0) };
            (float X, float Z)[] corner = { (cx - hw, q.At.Z + hd), (cx + hw, q.At.Z + hd), (cx + hw, q.At.Z - hd), (cx - hw, q.At.Z - hd) };
            Rlgl.CheckRenderBatchLimit(4);
            Rlgl.SetTexture(atlas.Id);
            Rlgl.Begin(DrawMode.Quads);
            Rlgl.Color4ub(q.Tint.R, q.Tint.G, q.Tint.B, q.Tint.A);
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
    /// The height of the water the last frame mirrored in, or null where it mirrored nothing (plan 09 · L10): what a
    /// tool reads to tell whether the pass ran.
    /// </summary>
    public float? Mirrored { get; private set; }

    /// <summary>
    /// The mirror pass (plan 09 · L10; style guide, "Water", "Reflection"): what stands within the view and a little
    /// north of it (<see cref="Reflections.Reach"/>) is drawn again through the camera mirrored about the water's plane,
    /// straightened as the field's camera straightens its reflection and cut off under the plane, into the context's
    /// mirror target, cleared to nothing. The scenery's standing meshes, the things of the map, the Pokémon and the
    /// people: never the ground, grass, lights or the field's life. The water then reads it through the same camera.
    /// </summary>
    private void DrawMirror(List<MapScene> scenes, Camera3D camera, GroundRect view, float plane, float pitchDeg)
    {
        var mirrored = Reflections.Mirror(camera, plane);
        var reach = Reflections.Reach(view);
        Raylib.BeginTextureMode(context.Mirror);
        Raylib.ClearBackground(new Color(0, 0, 0, 0));
        Rlgl.SetClipPlanes(10.0, 140.0);
        Raylib.BeginMode3D(mirrored);
        var mirrorView = Rlgl.GetMatrixModelview();
        var mirrorProjection = Rlgl.GetMatrixProjection();
        Rlgl.DisableBackfaceCulling();
        context.BindShadowMap();
        shaders.BeginMirrorPass(ArtLook.FieldUpright, pitchDeg, plane);

        foreach (var scene in scenes)
            if (Reaches(scene, reach)) scene.DrawStanding(reach);
        DrawThings(CharacterPass.Color);
        DrawPokemon(CharacterPass.Color);
        DrawActors(CharacterPass.Color);

        shaders.EndMirrorPass();
        context.UnbindShadowMap();
        Rlgl.EnableBackfaceCulling();
        Raylib.EndMode3D();
        Raylib.EndTextureMode();
        shaders.SetUpright(ArtLook.FieldUpright, pitchDeg);
        shaders.SetWaterMirror(true, Raymath.MatrixMultiply(mirrorView, mirrorProjection), plane, pitchDeg);
    }

    /// <summary>
    /// Whoever stands in a puddle or on a polished floor is mirrored in it (style guide, "Puddles"): their sprite
    /// upside down on the ground to the south of their feet, as far as the puddle or the floor runs that way, in the
    /// tint of the tile under their feet (plan 09 · L10: <see cref="Reflections.TintOf"/>).
    /// </summary>
    private void DrawReflections(Map map, Camera3D camera)
    {
        float pitchDeg = MapScene.PitchOf(map);
        Camera3D? straight = map.IsIndoors ? null : camera;
        bool any = false;
        foreach (var actor in actors)
        {
            if (Reflections.WalkerReach(map, actor.Feet) is not { } reach) continue;
            var tint = Reflections.TintOf(map.BehaviourAt((int)MathF.Floor(actor.Feet.X), (int)MathF.Floor(actor.Feet.Z)));
            if (!any) Rlgl.DisableDepthMask();
            any = true;
            CharacterSprites.DrawReflection(actor.Rig, actor.Pose, actor.Yaw, actor.Feet, pitchDeg, reach, straight, tint);
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

        foreach (var npc in PeopleInSight(map))
        {
            // (An item's ball has a smaller patch under it than a person, and an obstacle one a little smaller)
            // (Whoever is shaken takes their patch with them, and whoever flickers has none while they are unseen)
            if (!npc.IsPCTerminal && !npc.IsUnseen && InSight(npc) && Seen(npc))
                Blob(npc.DrawX + 0.5f + NudgeOf(npc), npc.DrawY + 0.52f, npc.Level is { } level ? Relief.Under(map, npc.DrawX + 0.5f, npc.DrawY + 0.5f, level) : Relief.At(map, npc.DrawX + 0.5f, npc.DrawY + 0.5f),
                    npc.IsItemBall ? 0.6f : npc.IsObstacle ? 0.85f : npc.IsPokemon ? 0.75f : 1f);
        }
        if (withPlayer && Seen(null)) Blob(px + NudgeOf(null), pz + 0.02f, playerGround, 1f - Math.Clamp(lift * 0.8f, 0f, 0.5f));

        Rlgl.SetTexture(0);
        Rlgl.DrawRenderBatchActive();
        Rlgl.EnableDepthMask();
    }
}
