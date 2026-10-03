using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

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

    /// <summary>The ground this scene can show something on: its tiles and what leans in from just outside them.</summary>
    public GroundRect Bounds => new(ground.X - 3, ground.Y - 3, ground.Right + 3, ground.Bottom + 5);

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

    public static float PitchOf(Map map) => map.IsIndoors ? IndoorPitchDeg : OutdoorPitchDeg;

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
            if (waterMask != null) scene.AddWater(waterMesh);
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
            foreach (var b in BuildingsIn(map, chunk))
                BuildingModels.Add(kit, b, BuildingArt.StyleOf(b, map.ArchitectureAt(b.X0, b.Y0)), targets);
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
                if (clamp) Raylib.SetTextureWrap(texture, TextureWrap.Clamp);
                scene.ownTextures.Add(texture);
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

                float u0 = (tx - ground.X) / tilesW, u1 = (tx - ground.X + 1) / tilesW;
                float v0 = (ty - ground.Y) / tilesH, v1 = (ty - ground.Y + 1) / tilesH;
                b.Quad(new(tx, 0, ty + 1), new(tx + 1, 0, ty + 1), new(tx + 1, 0, ty), new(tx, 0, ty),
                    new(u0, v1), new(u1, v1), new(u1, v0), new(u0, v0), Color.White, Up);
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

    private bool IsInteriorFloor(int tx, int ty, TileType t)
    {
        if (ty < 2) return false;
        if (ty == Map.Height - 1) return t == TileType.Door;
        return tx > 0 && tx < Map.Width - 1;
    }

    /// <summary>
    /// The water's surface lies just above the ground over every water tile and its neighbours; the water shader
    /// keeps only the texels the mask marks as water, so the shore is as round as the baked ground's.
    /// </summary>
    private void AddWater(MeshBuilder water)
    {
        float tilesW = ground.Width, tilesH = ground.Height;
        bool NearWater(int x, int y)
        {
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                    if (TypeAt(x + dx, y + dy) == TileType.Water) return true;
            return false;
        }

        for (int ty = ground.Y; ty < ground.Bottom; ty++)
        {
            for (int tx = ground.X; tx < ground.Right; tx++)
            {
                if (!NearWater(tx, ty)) continue;
                float u0 = (tx - ground.X) / tilesW, u1 = (tx - ground.X + 1) / tilesW;
                float v0 = (ty - ground.Y) / tilesH, v1 = (ty - ground.Y + 1) / tilesH;
                water.Quad(new(tx, WaterLevel, ty + 1), new(tx + 1, WaterLevel, ty + 1), new(tx + 1, WaterLevel, ty), new(tx, WaterLevel, ty),
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
                if (StyleOfTree(tx, ty) == TreeStyle.Pine) TreeModels.Pine(batches, cx, cz, tx, ty);
                else TreeModels.Round(batches, cx, cz, tx, ty);
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
                        b.Quad(new(x0, 0, cz), new(x1, 0, cz), new(x1, h, cz), new(x0, h, cz),
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
                        TreeModels.Card(flowers, x, z, 0.5f, 0.5f * VS, kind / 3f, (kind + 1) / 3f);
                    }
                }
                else if (t == TileType.Grass && Rand(tx, ty, 80) < 0.35f)
                {
                    float x = tx + (int)(Rand(tx, ty, 81) * 16) * texel;
                    TreeModels.Card(tufts, x, ty + 0.2f + Rand(tx, ty, 82) * 0.6f, 0.5f, 0.375f * VS, 0f, 1f);
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
        var lawn = new Color(104, 190, 98, 255);
        var dirt = new Color(146, 108, 72, 255);
        var flat = batches.For(SceneTextures.White);
        var face = batches.For(SceneTextures.LedgeFace);
        for (int ty = content.Y; ty < content.Bottom; ty++)
        {
            for (int tx = content.X; tx < content.Right; tx++)
            {
                if (Map.GetGroundTile(tx, ty) != TileType.LedgeDown) continue;
                bool left = Map.InBounds(tx - 1, ty) && Map.GetGroundTile(tx - 1, ty) == TileType.LedgeDown;
                bool right = Map.InBounds(tx + 1, ty) && Map.GetGroundTile(tx + 1, ty) == TileType.LedgeDown;
                float x0 = tx + (left ? 0f : 0.125f), x1 = tx + 1 - (right ? 0f : 0.125f);
                float zBack = ty + 0.12f, zTop = ty + 0.36f, zFront = ty + 0.62f;

                flat.Quad(new(x0, 0, zBack), new(x1, 0, zBack), new(x1, h, zTop), new(x0, h, zTop), default, default, default, default,
                    lawn, Vector3.Normalize(new Vector3(0, 1, -0.7f)));
                flat.Quad(new(x0, h, zFront), new(x1, h, zFront), new(x1, h, zTop), new(x0, h, zTop), default, default, default, default, lawn, Up);
                face.Quad(new(x0, 0, zFront), new(x1, 0, zFront), new(x1, h, zFront), new(x0, h, zFront),
                    new(x0, 1), new(x1, 1), new(x1, 0), new(x0, 0), Color.White, South);
                if (!left)
                {
                    flat.Tri(new(x0, 0, zBack), new(x0, h, zTop), new(x0, 0, zTop), default, default, default, dirt, -Vector3.UnitX);
                    flat.Quad(new(x0, 0, zFront), new(x0, 0, zTop), new(x0, h, zTop), new(x0, h, zFront), default, default, default, default, dirt, -Vector3.UnitX);
                }
                if (!right)
                {
                    flat.Tri(new(x1, 0, zBack), new(x1, h, zTop), new(x1, 0, zTop), default, default, default, dirt, Vector3.UnitX);
                    flat.Quad(new(x1, 0, zTop), new(x1, 0, zFront), new(x1, h, zFront), new(x1, h, zTop), default, default, default, default, dirt, Vector3.UnitX);
                }
            }
        }
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
        int left = T, right = (w - 1) * T, back = 2 * T, front = (h - 1) * T;
        var style = Map.Interior;
        kit.Origin = Vector3.Zero;

        var backWall = kit.Face("wall.back", right - left, wallH, c => GroundBaker.PaintWall(c, style));
        var sideWall = kit.Face("wall.side", front - back, wallH, c => GroundBaker.PaintWall(c, style));
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

        var daylight = batches.For(SceneTextures.WindowLight, MeshPass.Light);
        foreach (var prop in Map.Props)
        {
            PropModels.Build(kit, prop, Map);
            if (prop.Type != PropType.Window) continue;

            // Light from the window falls across the floor, slanting the way the room's shadows do
            const float y = 0.012f, reach = 2.3f, slant = 0.8f;
            float x0 = prop.X + 0.2f, x1 = prop.X + prop.Width - 0.2f, z0 = 2.02f;
            daylight.Quad(new(x0 + slant, y, z0 + reach), new(x1 + slant, y, z0 + reach), new(x1, y, z0), new(x0, y, z0),
                new(0, 1), new(1, 1), new(1, 0), new(0, 0), Color.White, Up);
        }
        for (int ty = 2; ty < h - 1; ty++)
            for (int tx = 1; tx < w - 1; tx++)
                if (Map.GetGroundTile(tx, ty) == TileType.PC) PropModels.BuildPc(kit, tx, ty);

        RoomCenter = new Vector3(w / 2f, 0.6f * VS, (back + front) / (2f * T) + 0.2f);
    }
}
