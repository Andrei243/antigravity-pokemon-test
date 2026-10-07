using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>A field camera: how steeply it looks down, how wide its view is from top to bottom, and how far away it stands (in tiles).</summary>
internal readonly record struct FieldView(float PitchDeg, float FovYDeg, float Distance)
{
    public static readonly FieldView Outdoor = new(MapScene.OutdoorPitchDeg, MapScene.OutdoorFovYDeg, MapScene.OutdoorDistance);
}

/// <summary>
/// The 3D model of one map, or of one chunk of a map of the imported world: textured ground, water lying in it,
/// trees, tall grass, buildings, and for interiors a furnished room. One world unit is one tile; +Y is up and +Z
/// is south, so the field camera looks north like the DS games. A chunk's geometry is in the map's own
/// coordinates, so the chunks of a map are simply drawn together.
/// <para>
/// Building is split in two. <see cref="Prepare"/> bakes the art and lays out the meshes without touching the
/// GPU, so chunks can be prepared on other threads while the game runs; <see cref="Finish"/> uploads the
/// result on the thread that owns the window.
/// </para>
/// </summary>
internal sealed class MapScene
{
    // Field camera from Pokémon Platinum (pret/pokeplatinum, src/overlay005/field_camera.c):
    // CAMERA_TYPE_DEFAULT is a perspective camera 666.9 units away, pitched -59.05°, with an 8.09° half FOV.
    public const float OutdoorPitchDeg = 59.05f;
    public const float OutdoorFovYDeg = 16.18f;
    public const float OutdoorDistance = 666.922f / 16f;

    // Its other field cameras (sCameraTypes, the same file). CAMERA_TYPE_CAVE is 574.6 units away, pitched
    // 63.26° with a 9.50° half FOV: nearer and steeper, so a cave's walls hide less of its floor.
    // CAMERA_TYPE_ZOOMED_IN (Floaroma Meadow, Eterna Forest, Amity Square) is 515.5 units away, pitched
    // 54.66° with a 10.46° half FOV: nearer and lower. All three show the same twelve tiles from top to bottom.
    public const float CavePitchDeg = 63.26f;
    public const float CaveFovYDeg = 19.0f;
    public const float CaveDistance = 574.578f / 16f;
    public const float ZoomedPitchDeg = 54.66f;
    public const float ZoomedFovYDeg = 20.92f;
    public const float ZoomedDistance = 515.456f / 16f;

    // The outside of Mt. Coronet's south face: further off and looking down steeply (CAMERA_TYPE_MT_CORONET_EXT_SOUTH)
    public const float CoronetSouthPitchDeg = 73.11f;
    public const float CoronetSouthFovYDeg = 12.67f;
    public const float CoronetSouthDistance = 866.554f / 16f;

    // Rooms get a closer, wider perspective camera so their walls and furniture read as a 3D space
    public const float IndoorPitchDeg = 52f;
    public const float IndoorFovYDeg = 30f;
    public const float IndoorDistance = 18.5f;

    public const int OutdoorMargin = 8;
    private const float WaterLevel = 0.004f;

    public Map Map { get; }

    /// <summary>The chunk of a streamed map this scene is; null for the scene of a whole map.</summary>
    public TileWindow? Chunk { get; }
    public bool Indoors => Map.IsIndoors;
    public float PitchDeg { get; }

    /// <summary>World height of one screen row: vertical sizes are divided by cos(pitch) so they read at full size.</summary>
    public float VS { get; }
    public int Margin { get; }
    public Vector3 RoomCenter { get; private set; }

    /// <summary>
    /// How deep a room is for the camera, counted as a hand-made room's height is (its floor, the two rows of wall
    /// behind it and the front wall): a room rebuilt to the original's plan may have more wall behind its floor.
    /// </summary>
    public int RoomDepth => Map.Height - roomCorner.Back + 2;

    // Where a room's floor begins (Map.RoomCorner): its side wall stands west of it and its back wall north
    private readonly (int Left, int Back) roomCorner;

    /// <summary>The ground this scene can show something on: its tiles and what leans in from just outside them.</summary>
    public GroundRect Bounds => Reach(ground);

    /// <summary>
    /// The ground on which a view must fall for a scene of these tiles to be worth drawing. A building belongs
    /// to the chunk its north-west corner is in and may reach ten tiles into the next one east or south; and a
    /// tower stands so tall that it shows from eleven tiles of ground south of the view's bottom edge (four of
    /// which the view already allows for).
    /// </summary>
    public static GroundRect Reach(TileWindow tiles) => new(tiles.X - 3, tiles.Y - 8, tiles.Right + 10, tiles.Bottom + 10);

    // The tiles the ground mesh covers; the tiles whose grass, ledges, props and buildings are this scene's; and
    // the tiles whose trees are
    private readonly TileWindow ground, content, forest;

    private SceneMeshes meshes = null!;
    private readonly List<Texture2D> ownTextures = new();

    private MapScene(Map map, TileWindow? chunk)
    {
        Map = map;
        Chunk = chunk;
        PitchDeg = PitchOf(map);
        VS = VerticalScaleOf(map);
        Margin = map.IsIndoors || chunk != null ? 0 : OutdoorMargin;
        roomCorner = map.IsIndoors ? map.RoomCorner() : (1, 2);
        if (chunk is { } c)
        {
            ground = content = forest = c;
        }
        else
        {
            ground = new TileWindow(-Margin, -Margin, map.Width + Margin * 2, map.Height + Margin * 2);
            content = new TileWindow(0, 0, map.Width, map.Height);
            // The camera never shows more than a few tiles past the map (more to the south, where tall trees
            // poke up into view), so the forest beyond that is left out
            forest = map.IsIndoors ? content : new TileWindow(-5, -5, map.Width + 10, map.Height + 5 + Margin);
        }
    }

    /// <summary>How the field is looked at on a map: a room's own camera, or the one the place's header names.</summary>
    public static FieldView ViewOf(Map map)
    {
        if (map.IsIndoors) return new FieldView(IndoorPitchDeg, IndoorFovYDeg, IndoorDistance);
        return map.Camera switch
        {
            FieldCamera.Cave => new FieldView(CavePitchDeg, CaveFovYDeg, CaveDistance),
            FieldCamera.ZoomedIn => new FieldView(ZoomedPitchDeg, ZoomedFovYDeg, ZoomedDistance),
            FieldCamera.CoronetSouth => new FieldView(CoronetSouthPitchDeg, CoronetSouthFovYDeg, CoronetSouthDistance),
            _ => FieldView.Outdoor
        };
    }

    public static float PitchOf(Map map) => ViewOf(map).PitchDeg;

    public static float VerticalScaleOf(Map map) => 1f / MathF.Cos(PitchOf(map) * MathF.PI / 180f);

    /// <summary>The chunk of a streamed map at a column and row of its grid, cut off at the map's edge.</summary>
    public static TileWindow ChunkWindow(Map map, int chunkX, int chunkY)
    {
        int x = chunkX * Map.ChunkTiles, y = chunkY * Map.ChunkTiles;
        return new TileWindow(x, y, Math.Min(Map.ChunkTiles, map.Width - x), Math.Min(Map.ChunkTiles, map.Height - y));
    }

    // ------------------------------------------------------------------ drawing

    /// <param name="view">The ground the camera can see, to skip the parts of the map outside it; null draws it all.</param>
    public void Draw(GroundRect? view = null) => meshes.Draw(view);

    /// <summary>
    /// Adds the light thrown on the ground: pools under lamps and lit doors, pools under the windows of homes,
    /// and in rooms the patches of daylight under the windows.
    /// </summary>
    public void DrawLights(float publicLevel, float homeLevel, Vector3 tint) => meshes.DrawLights(publicLevel, homeLevel, tint);

    /// <summary>Draws everything that casts shadows, for the shadow-map pass.</summary>
    /// <param name="casters">The ground whose shadows can reach the view; null draws every caster.</param>
    public void DrawDepth(GroundRect? casters = null) => meshes.DrawDepth(casters);

    // ------------------------------------------------------------------ building the scene

    /// <summary>The whole of a map, built in one go.</summary>
    public static MapScene Build(Map map, FieldShaders shaders) => Finish(Prepare(map), shaders);

    /// <summary>
    /// A scene that is baked, laid out and packed for the GPU but not yet on it: the art of its own as images,
    /// every mesh in native memory. What is left is the uploading itself.
    /// </summary>
    public sealed class Prepared
    {
        public required MapScene Scene { get; init; }

        /// <summary>The art baked for this scene (its ground, its water mask, its sheet of buildings and props), each with the meshes drawn with it.</summary>
        internal List<(Image Image, bool Clamp, List<(Mesh Mesh, MeshPass Pass)> Meshes)> Own { get; } = new();

        /// <summary>The meshes drawn with the field's shared textures.</summary>
        internal List<(Texture2D Tex, MeshPass Pass, Mesh Mesh, (Vector3 Min, Vector3 Max)? Bounds)> Shared { get; } = new();

        /// <summary>The faces of the sheet of buildings and props that move, each with its frames as images.</summary>
        internal List<(Art Region, Image[] Frames, float Rate)> Moving { get; } = new();

        /// <summary>Uploads in all: a texture or a mesh each.</summary>
        public int Steps => Own.Sum(o => 1 + o.Meshes.Count) + Shared.Count;
    }

    /// <summary>The buildings a scene of a map (or of one chunk of it) draws: those whose north-west corner is in it.</summary>
    public static IEnumerable<BuildingInfo> BuildingsIn(Map map, TileWindow? chunk)
    {
        foreach (var b in MapStructures.BuildingsOf(map))
            if (chunk == null || chunk.Value.Contains(b.X0, b.Y0)) yield return b;
    }

    /// <summary>
    /// Makes sure the shared textures a scene will look up exist, which only the thread that owns the window can
    /// do. Call it there before <see cref="Prepare"/> runs anywhere else.
    /// </summary>
    public static void Warm(Map map, TileWindow? chunk)
    {
        SceneTextures.Warm();
        if (map.IsIndoors) return;
        foreach (var b in BuildingsIn(map, chunk))
            _ = SceneTextures.RoofTiles(BuildingArt.StyleOf(b, map.ArchitectureAt(b.X0, b.Y0)).RoofColor);
    }

    /// <summary>
    /// Bakes and lays out a map, or one chunk of a streamed map, without touching the GPU (the shared textures
    /// it looks up must exist: see <see cref="Warm"/>).
    /// </summary>
    public static Prepared Prepare(Map map, TileWindow? chunk = null)
    {
        var scene = new MapScene(map, chunk);
        var batches = new MeshBatches();

        PixelCanvas? waterMask = null;
        PixelCanvas ground;
        if (map.IsIndoors) ground = GroundBaker.BakeInterior(map);
        else if (chunk is { } window) ground = PixelGround.Bake(map, window, PixelGround.ChunkPad, worldSeeds: true, out waterMask);
        else ground = PixelGround.Bake(map, scene.Margin, MapStructures.BuildingsOf(map), out waterMask);

        // Buildings and props are painted face by face into one sheet of art for the scene
        var prepared = new Prepared { Scene = scene };
        var kit = new KitBuilder(new ArtSheet(), scene.VS);
        MeshBuilder groundMesh = new(), waterMesh = new();
        scene.AddGround(groundMesh, batches.For(SceneTextures.White, MeshPass.Ground));

        if (map.IsIndoors)
        {
            scene.AddInterior(batches, kit);
        }
        else
        {
            if (waterMask != null) scene.AddWater(waterMesh, batches);
            scene.AddFaces(batches);
            scene.AddTrees(batches);
            scene.AddTallGrass(batches.For(SceneTextures.TallGrass, MeshPass.Ground));
            scene.AddLawnDetail(batches);
            scene.AddLedges(batches);

            var targets = new BuildingTargets
            {
                RoofTiles = color => batches.For(SceneTextures.RoofTiles(color)),
                PublicLight = batches.For(SceneTextures.LightPool, MeshPass.Light),
                HomeLight = batches.For(SceneTextures.LightPool, MeshPass.HomeLight)
            };
            // A building stands on the ground at the foot of its front wall
            foreach (var b in BuildingsIn(map, chunk))
                BuildingModels.Add(kit, b, BuildingArt.StyleOf(b, map.ArchitectureAt(b.X0, b.Y0)), targets, Relief.At(map, b.X0 + b.Width / 2f, b.Y1 + 0.5f));
            OutdoorProps.Add(kit, map, batches.For(SceneTextures.LampGlow, MeshPass.Light), chunk);
        }

        // Everything is packed here, so the thread that owns the window is left with the uploads alone
        static List<(Mesh, MeshPass)> Packed(params (MeshBuilder Builder, MeshPass Pass)[] meshes)
        {
            var packed = new List<(Mesh, MeshPass)>();
            foreach (var (builder, pass) in meshes)
                if (builder.VertexCount > 0) packed.Add((builder.Pack(), pass));
            return packed;
        }

        prepared.Own.Add((ground.ToImage(), false, Packed((groundMesh, MeshPass.Ground))));
        if (waterMask != null && waterMesh.VertexCount > 0)
            prepared.Own.Add((waterMask.ToImage(), false, Packed((waterMesh, MeshPass.Water))));
        if (kit.Solid.VertexCount + kit.Flat.VertexCount > 0)
        {
            var art = kit.Sheet.ToCanvas().ToImage();
            foreach (var (_, region, frames, rate) in kit.Sheet.Moving)
                prepared.Moving.Add((region, frames.Select(f => f.ToImage()).ToArray(), rate));
            kit.Finish();
            prepared.Own.Add((art, true, Packed((kit.Solid, MeshPass.Opaque), (kit.Flat, MeshPass.Ground))));
        }
        foreach (var (tex, pass, chunked, builder) in batches.All)
            if (builder.VertexCount > 0) prepared.Shared.Add((tex, pass, builder.Pack(), chunked ? builder.Bounds() : null));
        return prepared;
    }

    /// <summary>
    /// The uploads a prepared scene needs, one texture or one mesh to a step, in the order they must run. They
    /// must run on the thread that owns the window; the scene can be drawn once the last has.
    /// </summary>
    public static Queue<Action> Uploads(Prepared prepared, FieldShaders shaders)
    {
        var scene = prepared.Scene;
        scene.meshes = new SceneMeshes();
        var steps = new Queue<Action>();

        foreach (var (image, clamp, meshes) in prepared.Own)
        {
            // The steps that upload the meshes use the texture the step before them makes
            Texture2D texture = default;
            steps.Enqueue(() =>
            {
                texture = Raylib.LoadTextureFromImage(image);
                Raylib.UnloadImage(image);
                Raylib.SetTextureFilter(texture, TextureFilter.Point);
                scene.ownTextures.Add(texture);
                if (!clamp) return;
                // The sheet of buildings and props: its moving faces will be redrawn into it frame by frame
                Raylib.SetTextureWrap(texture, TextureWrap.Clamp);
                foreach (var (region, frames, rate) in prepared.Moving)
                    scene.moving.Add(new MovingArt(texture, new Rectangle(region.X, region.Y, region.Width, region.Height), frames, rate));
            });
            foreach (var (packed, pass) in meshes)
                steps.Enqueue(() => scene.meshes.Add(OnGpu(packed), texture, pass, null, shaders));
        }

        foreach (var (tex, pass, packed, bounds) in prepared.Shared)
            steps.Enqueue(() => scene.meshes.Add(OnGpu(packed), tex, pass, bounds, shaders));
        return steps;
    }

    /// <summary>
    /// Uploads a packed mesh and lets go of its copy in main memory: the GPU has it now, and a streamed world
    /// would otherwise keep every chunk's geometry twice.
    /// </summary>
    private static unsafe Mesh OnGpu(Mesh mesh)
    {
        Raylib.UploadMesh(ref mesh, false);
        Raylib.MemFree(mesh.Vertices); mesh.Vertices = null;
        Raylib.MemFree(mesh.TexCoords); mesh.TexCoords = null;
        Raylib.MemFree(mesh.Normals); mesh.Normals = null;
        Raylib.MemFree(mesh.Colors); mesh.Colors = null;
        return mesh;
    }

    /// <summary>Uploads a prepared scene in one go. Must run on the thread that owns the window.</summary>
    public static MapScene Finish(Prepared prepared, FieldShaders shaders)
    {
        var steps = Uploads(prepared, shaders);
        while (steps.Count > 0) steps.Dequeue()();
        return prepared.Scene;
    }

    /// <summary>Frees the scene's meshes and the textures baked for it, when its chunk of the world is left behind.</summary>
    public void Unload()
    {
        meshes.Unload();
        foreach (var texture in ownTextures) Raylib.UnloadTexture(texture);
        ownTextures.Clear();
        foreach (var art in moving)
            foreach (var frame in art.Frames) Raylib.UnloadImage(frame);
        moving.Clear();
    }

    private sealed class MovingArt(Texture2D texture, Rectangle rect, Image[] frames, float rate)
    {
        public Texture2D Texture { get; } = texture;
        public Rectangle Rect { get; } = rect;
        public Image[] Frames { get; } = frames;
        public float Rate { get; } = rate;
        public int Shown { get; set; }
    }

    private readonly List<MovingArt> moving = new();

    /// <summary>
    /// Shows the frame each moving face of the scene's art has reached (a fountain's water, a turbine's blades)
    /// by redrawing its rectangle of the sheet. Call on the thread that owns the window, before drawing.
    /// </summary>
    public unsafe void Animate(double time)
    {
        foreach (var art in moving)
        {
            int frame = (int)((long)(time * art.Rate) % art.Frames.Length);
            if (frame == art.Shown) continue;
            art.Shown = frame;
            Raylib.UpdateTextureRec(art.Texture, art.Rect, art.Frames[frame].Data);
        }
    }

    private TileType? TypeAt(int x, int y) => GroundBaker.TypeAt(Map, x, y);

    private static float Rand(int x, int y, int salt) => GroundBaker.Rand01(x, y, salt);

    private static readonly Vector3 Up = Vector3.UnitY;
    private static readonly Vector3 South = Vector3.UnitZ;

    // ------------------------------------------------------------------ ground & water

    private void AddGround(MeshBuilder b, MeshBuilder flat)
    {
        float tilesW = ground.Width, tilesH = ground.Height;
        for (int ty = ground.Y; ty < ground.Bottom; ty++)
        {
            for (int tx = ground.X; tx < ground.Right; tx++)
            {
                var t = TypeAt(tx, ty);
                if (t == null) continue;
                if (Map.IsIndoors && !IsInteriorFloor(tx, ty, t.Value)) continue;
                // Under the Distortion World's islands there is no ground: the void shows through
                if (t == TileType.Void) continue;

                float u0 = (tx - ground.X) / tilesW, u1 = (tx - ground.X + 1) / tilesW;
                float v0 = (ty - ground.Y) / tilesH, v1 = (ty - ground.Y + 1) / tilesH;
                if (!Map.HasRelief)
                {
                    b.Quad(new(tx, 0, ty + 1), new(tx + 1, 0, ty + 1), new(tx + 1, 0, ty), new(tx, 0, ty),
                        new(u0, v1), new(u1, v1), new(u1, v0), new(u0, v0), Color.White, Up);
                    continue;
                }

                // With relief each tile lies at its own corners' heights: flat, a slope, or a bank up to a higher neighbour
                var (nw, ne, sw, se) = Relief.Corners(Map, tx, ty);
                if (Relief.Deck(Map, tx, ty) is { } deck)
                {
                    // A bridge: its boards at the deck's height. Water runs on under it (the water's own mesh
                    // draws that); over dry ground the ground under it is drawn in shade. A boardwalk lies a
                    // hair above the water it rests on.
                    if (!Map.IsDeepWater(tx, ty) && deck - MathF.Max(MathF.Max(nw, ne), MathF.Max(sw, se)) > 0.3f)
                        flat.Quad(new(tx, sw, ty + 1), new(tx + 1, se, ty + 1), new(tx + 1, ne, ty), new(tx, nw, ty), default, default, default, default, new Color(70, 96, 78, 255), Up);
                    nw = ne = sw = se = deck + WaterLevel * 3f;
                }

                // Stairs and ramps catch the light as slopes, though less than they would in life, so every flight
                // keeps its treads readable; a bank that only meets its neighbour is lit as flat ground
                var (slopeX, slopeZ) = Map.SlopeAt(tx, ty);
                var normal = slopeX == 0f && slopeZ == 0f ? Up : Vector3.Normalize(new Vector3(-slopeX, 2f, -slopeZ));
                b.Quad(new(tx, sw, ty + 1), new(tx + 1, se, ty + 1), new(tx + 1, ne, ty), new(tx, nw, ty),
                    new(u0, v1), new(u1, v1), new(u1, v0), new(u0, v0), Color.White, normal);
            }
        }

        // A chunk has its neighbours all round it; a whole map ends, and needs something beyond its edge
        if (!Map.IsIndoors && Chunk == null)
        {
            // Far skirt so nothing past the forest margin ever shows the clear color
            var skirt = new Color(56, 134, 88, 255);
            float x0 = -Margin, x1 = Map.Width + Margin, z0 = -Margin, z1 = Map.Height + Margin, far = 80f, y = -0.02f;
            void Skirt(float ax, float az, float bx, float bz) =>
                flat.Quad(new(ax, y, bz), new(bx, y, bz), new(bx, y, az), new(ax, y, az), default, default, default, default, skirt, Up);
            Skirt(x0 - far, z0 - far, x1 + far, z0);
            Skirt(x0 - far, z1, x1 + far, z1 + far);
            Skirt(x0 - far, z0, x0, z1);
            Skirt(x1, z0, x1 + far, z1);
        }
    }

    // ------------------------------------------------------------------ relief

    /// <summary>The drawn height of the ground at a point of the map (0 on a map without relief).</summary>
    private float Y(float x, float z) => Relief.At(Map, x, z);

    private static bool IsRocky(TileType type) => type is TileType.Rock or TileType.Snow or TileType.Ice or TileType.CaveFloor or TileType.CaveWall or TileType.CaveMouth
        or TileType.DistortionGround or TileType.DistortionSlab;

    /// <summary>
    /// The faces between levels (style guide, "Relief"): wherever a tile stands higher than its neighbour to the
    /// south, east or west by more than a slope takes up, a wall from the lower ground to the higher, in earth
    /// under grass and in rock under rock. They cast shadows; the flat ground does not.
    /// </summary>
    private void AddFaces(MeshBatches batches)
    {
        if (!Map.HasRelief) return;
        var earth = batches.For(SceneTextures.BankFace);
        var rock = batches.For(SceneTextures.RockFace);
        var beams = batches.For(SceneTextures.White);
        var beam = new Color(104, 74, 54, 255);
        // In the Distortion World an island's edge over the void is its underside, ending in points of rock
        var under = Map.IsVoid ? batches.For(SceneTextures.IslandUnderside) : null;
        bool Over(int x, int y) => under != null && TypeAt(x, y) == TileType.Void;

        for (int ty = ground.Y; ty < ground.Bottom; ty++)
            for (int tx = ground.X; tx < ground.Right; tx++)
            {
                if (!Map.InBounds(tx, ty)) continue;
                // The void has no ground, so no edge of its own: only an island's edge over it shows
                if (under != null && TypeAt(tx, ty) == TileType.Void) continue;
                var (nw, ne, sw, se) = Relief.Corners(Map, tx, ty);
                var face = IsRocky(Map.GetGroundTile(tx, ty)) ? rock : earth;

                // Each edge against the matching edge of the tile beyond it; a face that would look north is never seen
                float wide = face == rock ? NatureArt.RockFaceTiles : 1f;
                bool island = under != null && TypeAt(tx, ty) != TileType.Void;
                if (Map.InBounds(tx, ty + 1))
                {
                    var s = Relief.Corners(Map, tx, ty + 1);
                    if (island && Over(tx, ty + 1)) Underside(under!, new(tx, 0, ty + 1), sw, new(tx + 1, 0, ty + 1), se, South, tx);
                    else Face(face, new(tx, 0, ty + 1), sw, s.NW, new(tx + 1, 0, ty + 1), se, s.NE, South, tx, wide);
                }
                if (Map.InBounds(tx + 1, ty))
                {
                    var e = Relief.Corners(Map, tx + 1, ty);
                    if (island && Over(tx + 1, ty)) Underside(under!, new(tx + 1, 0, ty + 1), se, new(tx + 1, 0, ty), ne, Vector3.UnitX, ty);
                    else Face(face, new(tx + 1, 0, ty + 1), se, e.SW, new(tx + 1, 0, ty), ne, e.NW, Vector3.UnitX, ty, wide);
                }
                if (Map.InBounds(tx - 1, ty))
                {
                    var w = Relief.Corners(Map, tx - 1, ty);
                    if (island && Over(tx - 1, ty)) Underside(under!, new(tx, 0, ty), nw, new(tx, 0, ty + 1), sw, -Vector3.UnitX, ty);
                    else Face(face, new(tx, 0, ty), nw, w.NE, new(tx, 0, ty + 1), sw, w.SE, -Vector3.UnitX, ty, wide);
                }

                // A bridge's deck has a beam along each side that is open
                if (Relief.Deck(Map, tx, ty) is { } deck)
                {
                    const float depth = 0.22f;
                    void Beam(Vector3 a, Vector3 c, Vector3 normal) =>
                        beams.Quad(a with { Y = deck - depth }, c with { Y = deck - depth }, c with { Y = deck }, a with { Y = deck }, default, default, default, default, beam, normal);
                    if (Relief.Deck(Map, tx, ty + 1) == null) Beam(new(tx, 0, ty + 1), new(tx + 1, 0, ty + 1), South);
                    if (Relief.Deck(Map, tx + 1, ty) == null) Beam(new(tx + 1, 0, ty + 1), new(tx + 1, 0, ty), Vector3.UnitX);
                    if (Relief.Deck(Map, tx - 1, ty) == null) Beam(new(tx, 0, ty), new(tx, 0, ty + 1), -Vector3.UnitX);
                }
            }
    }

    /// <summary>
    /// One face along a tile's edge from <paramref name="a"/> to <paramref name="b"/> (their heights are filled
    /// in here): from the lower ground beyond the edge up to this tile's own, wherever this tile is the higher.
    /// </summary>
    /// <param name="along">Where the edge starts along the face, in tiles, and <paramref name="wide"/> how many tiles one width of its art covers.</param>
    private void Face(MeshBuilder mesh, Vector3 a, float topA, float bottomA, Vector3 b, float topB, float bottomB, Vector3 normal, float along, float wide)
    {
        const float least = 0.02f;
        if (topA - bottomA < least && topB - bottomB < least) return;
        bottomA = MathF.Min(bottomA, topA);
        bottomB = MathF.Min(bottomB, topB);

        // The art keeps square texels on screen: 32 columns to the tile and, seen from this steep, about 16 rows
        float rows = GroundBaker.ArtTile / VS, sheet = NatureArt.FaceCap + NatureArt.FaceBody;
        float u0 = along / wide, u1 = (along + 1f) / wide;
        void Piece(float fromA, float toA, float fromB, float toB, float v0A, float v1A, float v0B, float v1B) =>
            mesh.Quad(a with { Y = toA }, b with { Y = toB }, b with { Y = fromB }, a with { Y = fromA },
                new(u0, v1A / sheet), new(u1, v1B / sheet), new(u1, v0B / sheet), new(u0, v0A / sheet), Color.White, normal);

        bool level = MathF.Abs(topA - topB) < least && MathF.Abs(bottomA - bottomB) < least;
        float height = topA - bottomA;
        if (!level || height * rows <= sheet)
        {
            // One piece: the art from its top down as far as the face goes
            Piece(topA, bottomA, topB, bottomB, 0f, (topA - bottomA) * rows, 0f, (topB - bottomB) * rows);
            return;
        }

        // A tall face: the cap once, then the body as often as it takes
        float capHeight = NatureArt.FaceCap / rows, bodyHeight = NatureArt.FaceBody / rows;
        Piece(topA, topA - capHeight, topB, topB - capHeight, 0f, NatureArt.FaceCap, 0f, NatureArt.FaceCap);
        for (float y = topA - capHeight; y > bottomA + 0.0001f; y -= bodyHeight)
        {
            float to = MathF.Max(bottomA, y - bodyHeight), v1 = NatureArt.FaceCap + (y - to) * rows;
            Piece(y, to, y, to, NatureArt.FaceCap, v1, NatureArt.FaceCap, v1);
        }
    }

    /// <summary>
    /// The underside of one of the Distortion World's islands along a tile's edge over the void: the art stretched
    /// once from the island's edge down <see cref="Data.WorldMapBuilder.VoidDrop"/>, so its points of rock end where
    /// the face does and nothing shows below them, however deep the void beside it lies (an upper stone of B2F
    /// floats over a void that is lower under the floor beside it).
    /// </summary>
    private static void Underside(MeshBuilder mesh, Vector3 a, float topA, Vector3 b, float topB, Vector3 normal, float along)
    {
        float u0 = along / NatureArt.UndersideTiles, u1 = (along + 1f) / NatureArt.UndersideTiles;
        float drop = Data.WorldMapBuilder.VoidDrop;
        mesh.Quad(a with { Y = topA - drop }, b with { Y = topB - drop }, b with { Y = topB }, a with { Y = topA },
            new(u0, 1f), new(u1, 1f), new(u1, 0f), new(u0, 0f), Color.White, normal);
    }

    private bool IsInteriorFloor(int tx, int ty, TileType t)
    {
        if (ty < roomCorner.Back) return false;
        if (ty == Map.Height - 1) return t == TileType.Door;
        return tx >= roomCorner.Left && tx < Map.Width - 1;
    }

    /// <summary>
    /// The water's surface lies just above the ground over every water tile and its neighbours; the water shader
    /// keeps only the texels the mask marks as water, so the shore is as round as the baked ground's.
    /// </summary>
    private void AddWater(MeshBuilder water, MeshBatches batches)
    {
        var falls = batches.For(SceneTextures.Waterfall, MeshPass.Ground);
        var spray = batches.For(SceneTextures.White, MeshPass.Ground);
        var foam = new Color(236, 246, 255, 255);
        float tilesW = ground.Width, tilesH = ground.Height;
        bool NearWater(int x, int y)
        {
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                    if (GroundBaker.IsWaterAt(Map, x + dx, y + dy)) return true;
            return false;
        }

        for (int ty = ground.Y; ty < ground.Bottom; ty++)
        {
            for (int tx = ground.X; tx < ground.Right; tx++)
            {
                if (!NearWater(tx, ty)) continue;
                float u0 = (tx - ground.X) / tilesW, u1 = (tx - ground.X + 1) / tilesW;
                float v0 = (ty - ground.Y) / tilesH, v1 = (ty - ground.Y + 1) / tilesH;
                // The surface follows the ground it lies on (level with its banks: see Relief)
                var (nw, ne, sw, se) = Relief.Corners(Map, tx, ty);
                if (Map.InBounds(tx, ty) && Map.BehaviourAt(tx, ty) == TileBehavior.Waterfall)
                {
                    // Falling water: a sheet of streaks down the slope, with a line of foam where it goes over the
                    // edge and a wider one where it lands
                    const float lift = WaterLevel * 2f;
                    falls.Quad(new(tx, sw + lift, ty + 1), new(tx + 1, se + lift, ty + 1), new(tx + 1, ne + lift, ty), new(tx, nw + lift, ty),
                        new(tx, ty + 1), new(tx + 1, ty + 1), new(tx + 1, ty), new(tx, ty), Color.White, Up);
                    // Foam in clumps six texels wide with gaps between, the gaps at different places in each row of it
                    void Foam(float z0, float z1, int phase)
                    {
                        float Yat(float x, float z) => Relief.At(Map, x, Math.Clamp(z, ty + 0.001f, ty + 0.999f)) + lift * 1.5f;
                        for (int clump = 0; clump < 4; clump++)
                        {
                            if ((clump + tx + phase) % 4 == 3) continue;
                            float x0 = tx + clump * 0.25f + (phase % 2) * (2f / 32f), x1 = MathF.Min(tx + 1f, x0 + 6f / 32f);
                            spray.Quad(new(x0, Yat(x0, z1), z1), new(x1, Yat(x1 - 0.001f, z1), z1), new(x1, Yat(x1 - 0.001f, z0), z0), new(x0, Yat(x0, z0), z0),
                                default, default, default, default, foam, Up);
                        }
                    }
                    if (Map.BehaviourAt(tx, ty - 1) != TileBehavior.Waterfall) Foam(ty, ty + 2f / 32f, 0);
                    if (Map.BehaviourAt(tx, ty + 1) != TileBehavior.Waterfall)
                    {
                        Foam(ty + 1 - 6f / 32f, ty + 1 - 3f / 32f, 1);
                        Foam(ty + 1 - 3f / 32f, ty + 1, 2);
                    }
                    continue;
                }
                water.Quad(new(tx, sw + WaterLevel, ty + 1), new(tx + 1, se + WaterLevel, ty + 1), new(tx + 1, ne + WaterLevel, ty), new(tx, nw + WaterLevel, ty),
                    new(u0, v1), new(u1, v1), new(u1, v0), new(u0, v0), Color.White, Up);
            }
        }
    }

    // ------------------------------------------------------------------ trees

    private void AddTrees(MeshBatches batches)
    {
        // From the south, nearest the camera, to the north: each row then hides the one behind it before that
        // row is shaded, which in a forest saves most of the work
        for (int ty = forest.Bottom - 1; ty >= forest.Y; ty--)
        {
            for (int tx = forest.X; tx < forest.Right; tx++)
            {
                if (TypeAt(tx, ty) is not (TileType.Tree or TileType.TreeTrunk)) continue;

                float cx = tx + 0.5f + (Rand(tx, ty, 1) - 0.5f) * 0.12f;
                float cz = ty + 0.5f + (Rand(tx, ty, 2) - 0.5f) * 0.12f;
                // Trees are most of a map's geometry, so they are kept in chunks and only those in view are drawn
                batches.Chunk = MeshBatches.ChunkOf(tx, ty);
                if (StyleOfTree(tx, ty) == TreeStyle.Pine) TreeModels.Pine(batches, cx, cz, tx, ty, y0: Y(cx, cz), snow: Map.AreaAt(tx, ty)?.Snowbound == true);
                else TreeModels.Round(batches, cx, cz, tx, ty, y0: Y(cx, cz));
            }
        }
        batches.Chunk = 0;
    }


    /// <summary>
    /// The kind of tree on a tile. Where two areas of the world meet, each tree takes the kind of a tile a few
    /// steps off in a direction of its own, so one forest thins into the other instead of ending on a ruled line.
    /// </summary>
    private TreeStyle StyleOfTree(int tx, int ty)
    {
        if (!Map.IsStreamed) return Map.Trees;
        const float reach = 9f;
        int jx = (int)MathF.Round((Rand(tx, ty, 7) - 0.5f) * reach), jy = (int)MathF.Round((Rand(tx, ty, 8) - 0.5f) * reach);
        return Map.TreesAt(Math.Clamp(tx + jx, 0, Map.Width - 1), Math.Clamp(ty + jy, 0, Map.Height - 1));
    }

    // ------------------------------------------------------------------ tall grass, lawns, ledges

    /// <summary>
    /// Two rows of clumps per tile, the second half a clump out of step with the first. The tops sway, and lean
    /// away from anyone walking through (see the field shaders).
    /// </summary>
    private void AddTallGrass(MeshBuilder b)
    {
        float h = 0.5f * VS;
        var normal = Vector3.Normalize(new Vector3(0, 0.7f, 0.7f));
        var top = MeshBuilder.Sway(Color.White, 1f);
        for (int ty = content.Y; ty < content.Bottom; ty++)
        {
            for (int tx = content.X; tx < content.Right; tx++)
            {
                if (Map.GetGroundTile(tx, ty) != TileType.TallGrass) continue;
                for (int row = 0; row < 2; row++)
                {
                    float z = ty + 0.26f + row * 0.5f;
                    // Four pieces to a row, so the tops can bend smoothly round a walker
                    for (int piece = 0; piece < 4; piece++)
                    {
                        float u0 = piece * 0.25f, u1 = u0 + 0.25f;
                        float x0 = tx - row * 0.25f + u0, x1 = x0 + 0.25f;
                        // Every other clump stands a little further back, which breaks the rows up
                        float cz = z + ((piece / 2 + row + tx) % 2 == 0 ? 0f : 0.16f);
                        float y0 = Y(tx + 0.5f, cz);
                        b.Quad(new(x0, y0, cz), new(x1, y0, cz), new(x1, y0 + h, cz), new(x0, y0 + h, cz),
                            new(u0, 1), new(u1, 1), new(u1, 0), new(u0, 0), Color.White, top, normal);
                    }
                }
            }
        }
    }

    /// <summary>Grass tufts scattered on lawns and flowers standing in the flower beds, placed on the texel grid.</summary>
    private void AddLawnDetail(MeshBatches batches)
    {
        // Like the tall grass, these are in the ground pass: they take shadows but cast none (theirs would be specks)
        var tufts = batches.For(SceneTextures.LawnTuft, MeshPass.Ground);
        var flowers = batches.For(SceneTextures.Flowers, MeshPass.Ground);
        const float texel = 1f / GroundBaker.ArtTile;
        for (int ty = content.Y; ty < content.Bottom; ty++)
        {
            for (int tx = content.X; tx < content.Right; tx++)
            {
                var t = Map.GetGroundTile(tx, ty);
                if (t == TileType.FlowerGrass)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        float x = tx + (2 + i * 10 + (int)(Rand(tx, ty, 60 + i) * 3)) * texel;
                        float z = ty + 0.25f + Rand(tx, ty, 70 + i) * 0.6f;
                        float pick = Rand(tx, ty, 75 + i);
                        int kind = pick < 0.6f ? 0 : pick < 0.82f ? 1 : 2;
                        TreeModels.Card(flowers, x, z, 0.5f, 0.5f * VS, kind / 3f, (kind + 1) / 3f, Y(tx + 0.5f, z));
                    }
                }
                else if (t == TileType.Grass && Rand(tx, ty, 80) < 0.35f)
                {
                    float x = tx + (int)(Rand(tx, ty, 81) * 16) * texel;
                    float z = ty + 0.2f + Rand(tx, ty, 82) * 0.6f;
                    TreeModels.Card(tufts, x, z, 0.5f, 0.375f * VS, 0f, 1f, Y(tx + 0.5f, z));
                }
            }
        }
    }

    /// <summary>
    /// A ledge is a low ridge: the lawn rises gently from the north to a flat top, then drops in a face of dirt
    /// under a grass lip. It can be jumped down, not climbed.
    /// </summary>
    private void AddLedges(MeshBatches batches)
    {
        float h = 0.375f * VS;
        // In a cave the ridge is the floor's own rock (style guide, "Caves"), and in the Distortion World its islands' stone
        var lawn = Map.IsVoid ? new Color(136, 126, 152, 255) : Map.IsCave ? new Color(130, 118, 120, 255) : new Color(104, 190, 98, 255);
        var dirt = Map.IsVoid ? new Color(84, 74, 100, 255) : Map.IsCave ? new Color(82, 72, 84, 255) : new Color(146, 108, 72, 255);
        var flat = batches.For(SceneTextures.White);
        var face = batches.For(Map.IsCave || Map.IsVoid ? SceneTextures.RockLedgeFace : SceneTextures.LedgeFace);
        for (int ty = content.Y; ty < content.Bottom; ty++)
        {
            for (int tx = content.X; tx < content.Right; tx++)
            {
                var type = Map.GetGroundTile(tx, ty);
                if (type is TileType.LedgeLeft or TileType.LedgeRight)
                {
                    AddSideLedge(flat, face, tx, ty, type, h, lawn, dirt);
                    continue;
                }
                if (type != TileType.LedgeDown) continue;
                bool left = Map.InBounds(tx - 1, ty) && Map.GetGroundTile(tx - 1, ty) == TileType.LedgeDown;
                bool right = Map.InBounds(tx + 1, ty) && Map.GetGroundTile(tx + 1, ty) == TileType.LedgeDown;
                float x0 = tx + (left ? 0f : 0.125f), x1 = tx + 1 - (right ? 0f : 0.125f);
                float zBack = ty + 0.12f, zTop = ty + 0.36f, zFront = ty + 0.62f;
                float g = Y(tx + 0.5f, ty + 0.5f), top = g + h;

                flat.Quad(new(x0, g, zBack), new(x1, g, zBack), new(x1, top, zTop), new(x0, top, zTop), default, default, default, default,
                    lawn, Vector3.Normalize(new Vector3(0, 1, -0.7f)));
                flat.Quad(new(x0, top, zFront), new(x1, top, zFront), new(x1, top, zTop), new(x0, top, zTop), default, default, default, default, lawn, Up);
                face.Quad(new(x0, g, zFront), new(x1, g, zFront), new(x1, top, zFront), new(x0, top, zFront),
                    new(x0, 1), new(x1, 1), new(x1, 0), new(x0, 0), Color.White, South);
                if (!left)
                {
                    flat.Tri(new(x0, g, zBack), new(x0, top, zTop), new(x0, g, zTop), default, default, default, dirt, -Vector3.UnitX);
                    flat.Quad(new(x0, g, zFront), new(x0, g, zTop), new(x0, top, zTop), new(x0, top, zFront), default, default, default, default, dirt, -Vector3.UnitX);
                }
                if (!right)
                {
                    flat.Tri(new(x1, g, zBack), new(x1, top, zTop), new(x1, g, zTop), default, default, default, dirt, Vector3.UnitX);
                    flat.Quad(new(x1, g, zTop), new(x1, g, zFront), new(x1, top, zFront), new(x1, top, zTop), default, default, default, default, dirt, Vector3.UnitX);
                }
            }
        }
    }

    /// <summary>
    /// A ledge that is hopped westward or eastward: the same ridge turned to run north and south. The lawn rises
    /// from the side one comes from to a flat top, then drops in a face toward the side one lands on. The
    /// camera sees that face edge on, so what shows is the top, its grass lip, and the ridge's end where the run stops.
    /// </summary>
    private void AddSideLedge(MeshBuilder flat, MeshBuilder face, int tx, int ty, TileType type, float h, Color lawn, Color dirt)
    {
        bool north = Map.InBounds(tx, ty - 1) && Map.GetGroundTile(tx, ty - 1) == type;
        bool south = Map.InBounds(tx, ty + 1) && Map.GetGroundTile(tx, ty + 1) == type;
        float z0 = ty + (north ? 0f : 0.125f), z1 = ty + 1 - (south ? 0f : 0.125f);
        float g = Y(tx + 0.5f, ty + 0.5f), top = g + h;

        // Measured from the side one comes from: a westward ledge is come at from the east
        bool westward = type == TileType.LedgeLeft;
        float X(float fromBack) => westward ? tx + 1 - fromBack : tx + fromBack;
        float xBack = X(0.12f), xTop = X(0.36f), xFront = X(0.62f);
        var outward = westward ? -Vector3.UnitX : Vector3.UnitX;
        var slope = Vector3.Normalize(new Vector3(westward ? 0.7f : -0.7f, 1, 0));
        // (in the Distortion World a paler line of its stone)
        var lip = Map.IsVoid ? new Color(170, 160, 190, 255) : new Color(160, 222, 122, 255);

        flat.Quad(new(xBack, g, z1), new(xTop, top, z1), new(xTop, top, z0), new(xBack, g, z0), default, default, default, default, lawn, slope);
        flat.Quad(new(xTop, top, z1), new(xFront, top, z1), new(xFront, top, z0), new(xTop, top, z0), default, default, default, default, lawn, Up);
        // The bright edge along the top of the drop: a strip two texels wide
        float edge = westward ? xFront + 2f / 32f : xFront - 2f / 32f;
        flat.Quad(new(edge, top + 0.001f, z1), new(xFront, top + 0.001f, z1), new(xFront, top + 0.001f, z0), new(edge, top + 0.001f, z0),
            default, default, default, default, lip, Up);
        face.Quad(new(xFront, g, z1), new(xFront, g, z0), new(xFront, top, z0), new(xFront, top, z1),
            new(z1, 1), new(z0, 1), new(z0, 0), new(z1, 0), Color.White, outward);

        // The ridge's end, where the run stops: its profile in dirt (the south end is the one the camera sees)
        void End(float z, Vector3 normal)
        {
            flat.Tri(new(xBack, g, z), new(xTop, top, z), new(xTop, g, z), default, default, default, dirt, normal);
            flat.Quad(new(xTop, g, z), new(xFront, g, z), new(xFront, top, z), new(xTop, top, z), default, default, default, default, dirt, normal);
        }
        if (!south) End(z1, South);
        if (!north) End(z0, -South);
    }

    // ------------------------------------------------------------------ interiors

    /// <summary>
    /// A room seen like a doll's house: a back wall and two side walls painted for this room, cut away at the
    /// front with a gap for the door, furniture, and a patch of daylight on the floor under each window.
    /// </summary>
    private void AddInterior(MeshBatches batches, KitBuilder kit)
    {
        const int T = GroundBaker.ArtTile, wallH = PropModels.WallHeight, cap = 16;
        int w = Map.Width, h = Map.Height;
        var (lx, by) = roomCorner;
        int left = lx * T, right = (w - 1) * T, back = by * T, front = (h - 1) * T;
        var style = Map.Interior;
        kit.Origin = Vector3.Zero;

        var theme = Map.ArenaType;
        var backWall = kit.Face("wall.back", right - left, wallH, c => GroundBaker.PaintWall(c, style, 0, theme));
        var sideWall = kit.Face("wall.side", front - back, wallH, c => GroundBaker.PaintWall(c, style, 0, theme));
        kit.Quad(kit.At(left, 0, back), kit.At(right, 0, back), kit.At(right, wallH, back), kit.At(left, wallH, back), backWall, KitBuilder.FrontNormal);
        kit.Quad(kit.At(left, 0, front), kit.At(left, 0, back), kit.At(left, wallH, back), kit.At(left, wallH, front), sideWall, Vector3.UnitX);
        kit.Quad(kit.At(right, 0, back), kit.At(right, 0, front), kit.At(right, wallH, front), kit.At(right, wallH, back), sideWall, -Vector3.UnitX);

        // The dark tops of the cut-away walls frame the room
        Art Top(int tw, int td) => kit.Face($"wall.top.{tw}x{td}", tw, td, GroundBaker.PaintWallTop);
        kit.Box(left - cap, right + cap, back - cap, back, wallH - 1, wallH, top: Top(right - left + 2 * cap, cap));
        kit.Box(left - cap, left, back, front + cap, wallH - 1, wallH, top: Top(cap, front - back + cap));
        kit.Box(right, right + cap, back, front + cap, wallH - 1, wallH, top: Top(cap, front - back + cap));

        // The front wall is cut down to a low kerb so the camera can see in, with a gap for the door
        for (int tx = 1; tx < w - 1; tx++)
        {
            if (Map.GetGroundTile(tx, h - 1) == TileType.Door) continue;
            int end = tx;
            while (end + 1 < w - 1 && Map.GetGroundTile(end + 1, h - 1) != TileType.Door) end++;
            int run = (end - tx + 1) * T;
            kit.Box(tx * T, tx * T + run, front, front + cap, 0, 8, Top(run, cap), kit.Face($"wall.kerb.{run}", run, 8, GroundBaker.PaintWallTop));
            tx = end;
        }

        // The room's own inner walls (a room rebuilt to the original's plan: the Valley Windworks' hall and the
        // corridor below it) are cut away as the front wall is, so the camera sees over them, but only down to the
        // wainscot: each face that looks onto the floor is the lower part of the room's wall, under the walls' dark top
        const int innerH = PropModels.InnerWallHeight;
        bool Floor(int x, int y) => x >= lx && x < w - 1 && y >= by && y < h - 1 && Map.GetGroundTile(x, y) != TileType.Wall;
        Art? inner = null;
        for (int ty = by; ty < h - 1; ty++)
            for (int tx = lx; tx < w - 1; tx++)
            {
                if (Map.GetGroundTile(tx, ty) != TileType.Wall) continue;
                inner ??= kit.Face("wall.inner", T, innerH, c => GroundBaker.PaintWall(c, style, wallH - innerH, theme));
                bool n = Floor(tx, ty - 1), s = Floor(tx, ty + 1), wst = Floor(tx - 1, ty), e = Floor(tx + 1, ty);
                var top = kit.Face($"wall.top.inner.{(n ? "n" : "")}{(s ? "s" : "")}{(wst ? "w" : "")}{(e ? "e" : "")}", T, T,
                    c => GroundBaker.PaintWallTop(c, n, s, wst, e));
                kit.Box(tx * T, tx * T + T, ty * T, ty * T + T, 0, innerH, top, s ? inner : null, wst ? inner : null, e ? inner : null);
            }

        var daylight = batches.For(SceneTextures.WindowLight, MeshPass.Light);
        foreach (var prop in Map.Props)
        {
            PropModels.Build(kit, prop, Map);
            if (prop.Type != PropType.Window) continue;

            // Light from the window falls across the floor, slanting the way the room's shadows do
            const float y = 0.012f, reach = 2.3f, slant = 0.8f;
            float x0 = prop.X + 0.2f, x1 = prop.X + prop.Width - 0.2f, z0 = by + 0.02f;
            daylight.Quad(new(x0 + slant, y, z0 + reach), new(x1 + slant, y, z0 + reach), new(x1, y, z0), new(x0, y, z0),
                new(0, 1), new(1, 1), new(1, 0), new(0, 0), Color.White, Up);
        }
        for (int ty = by; ty < h - 1; ty++)
            for (int tx = lx; tx < w - 1; tx++)
                if (Map.GetGroundTile(tx, ty) == TileType.PC) PropModels.BuildPc(kit, tx, ty);

        RoomCenter = new Vector3((lx + w - 1) / 2f, 0.6f * VS, (back + front) / (2f * T) + 0.2f);
    }
}
