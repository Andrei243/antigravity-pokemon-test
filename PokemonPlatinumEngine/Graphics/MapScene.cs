using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The 3D model of one map, built once and cached: textured ground, water lying in it, trees, tall grass,
/// buildings, and for interiors a furnished room. One world unit is one tile; +Y is up and +Z is south,
/// so the field camera looks north like the DS games.
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
    public bool Indoors => Map.IsIndoors;
    public float PitchDeg { get; }

    /// <summary>World height of one screen row: vertical sizes are divided by cos(pitch) so they read at full size.</summary>
    public float VS { get; }
    public int Margin { get; }
    public Vector3 RoomCenter { get; private set; }

    private SceneMeshes meshes = null!;

    private MapScene(Map map)
    {
        Map = map;
        PitchDeg = map.IsIndoors ? IndoorPitchDeg : OutdoorPitchDeg;
        VS = 1f / MathF.Cos(PitchDeg * MathF.PI / 180f);
        Margin = map.IsIndoors ? 0 : OutdoorMargin;
    }

    // ------------------------------------------------------------------ drawing

    /// <param name="glow">How brightly windows and glass doors are lit from inside.</param>
    public void Draw(float glow) => meshes.Draw(glow);

    /// <summary>Adds the light pools under lit windows and doors (after dark).</summary>
    public void DrawLights(float glow) => meshes.DrawLights(glow);

    /// <summary>Draws everything that casts shadows, for the shadow-map pass.</summary>
    public void DrawDepth() => meshes.DrawDepth();

    // ------------------------------------------------------------------ building the scene

    public static MapScene Build(Map map, FieldShaders shaders)
    {
        var scene = new MapScene(map);
        var batches = new MeshBatches();
        var buildings = MapStructures.FindBuildings(map);

        PixelCanvas? waterMask = null;
        var ground = (map.IsIndoors ? GroundBaker.BakeInterior(map) : PixelGround.Bake(map, scene.Margin, buildings, out waterMask)).ToTexture();
        scene.AddGround(batches.For(ground, MeshPass.Ground), batches.For(SceneTextures.White, MeshPass.Ground));

        if (map.IsIndoors)
        {
            scene.AddInterior(batches);
        }
        else
        {
            if (waterMask != null) scene.AddWater(batches.For(waterMask.ToTexture(), MeshPass.Water));
            scene.AddRocks(batches);
            scene.AddTrees(batches);
            scene.AddTallGrass(batches.For(SceneTextures.TallGrass, MeshPass.Ground));
            scene.AddLawnDetail(batches);
            scene.AddLedges(batches);
            scene.AddSigns(batches);
            foreach (var b in buildings) scene.AddBuilding(b, batches);
        }

        scene.meshes = SceneMeshes.Upload(batches, shaders);
        return scene;
    }

    private TileType? TypeAt(int x, int y) => GroundBaker.TypeAt(Map, x, y);

    private static float Rand(int x, int y, int salt) => GroundBaker.Rand01(x, y, salt);

    private static readonly Vector3 Up = Vector3.UnitY;
    private static readonly Vector3 South = Vector3.UnitZ;

    // ------------------------------------------------------------------ ground & water

    private void AddGround(MeshBuilder b, MeshBuilder flat)
    {
        int tilesW = Map.Width + Margin * 2, tilesH = Map.Height + Margin * 2;
        for (int ty = -Margin; ty < Map.Height + Margin; ty++)
        {
            for (int tx = -Margin; tx < Map.Width + Margin; tx++)
            {
                var t = TypeAt(tx, ty);
                if (t == null) continue;
                if (Map.IsIndoors && !IsInteriorFloor(tx, ty, t.Value)) continue;

                float u0 = (tx + Margin) / (float)tilesW, u1 = (tx + Margin + 1) / (float)tilesW;
                float v0 = (ty + Margin) / (float)tilesH, v1 = (ty + Margin + 1) / (float)tilesH;
                b.Quad(new(tx, 0, ty + 1), new(tx + 1, 0, ty + 1), new(tx + 1, 0, ty), new(tx, 0, ty),
                    new(u0, v1), new(u1, v1), new(u1, v0), new(u0, v0), Color.White, Up);
            }
        }

        if (!Map.IsIndoors)
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
        int tilesW = Map.Width + Margin * 2, tilesH = Map.Height + Margin * 2;
        bool NearWater(int x, int y)
        {
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                    if (TypeAt(x + dx, y + dy) == TileType.Water) return true;
            return false;
        }

        for (int ty = -Margin; ty < Map.Height + Margin; ty++)
        {
            for (int tx = -Margin; tx < Map.Width + Margin; tx++)
            {
                if (!NearWater(tx, ty)) continue;
                float u0 = (tx + Margin) / (float)tilesW, u1 = (tx + Margin + 1) / (float)tilesW;
                float v0 = (ty + Margin) / (float)tilesH, v1 = (ty + Margin + 1) / (float)tilesH;
                water.Quad(new(tx, WaterLevel, ty + 1), new(tx + 1, WaterLevel, ty + 1), new(tx + 1, WaterLevel, ty), new(tx, WaterLevel, ty),
                    new(u0, v1), new(u1, v1), new(u1, v0), new(u0, v0), Color.White, Up);
            }
        }
    }

    /// <summary>Boulders: on land they sit on the ground, in water they stand in it with foam around them.</summary>
    private void AddRocks(MeshBatches batches)
    {
        foreach (var prop in Map.Props)
        {
            if (prop.Type != PropType.Boulder) continue;
            bool inWater = TypeAt(prop.X, prop.Y) == TileType.Water;
            TreeModels.Rock(batches, prop.X + prop.Width / 2f, prop.Y + prop.Depth / 2f, VS, prop.X, prop.Y, inWater);
        }
    }

    // ------------------------------------------------------------------ trees

    private void AddTrees(MeshBatches batches)
    {
        for (int ty = -Margin; ty < Map.Height + Margin; ty++)
        {
            for (int tx = -Margin; tx < Map.Width + Margin; tx++)
            {
                if (TypeAt(tx, ty) is not (TileType.Tree or TileType.TreeTrunk)) continue;

                // The camera never shows more than a few tiles past the map (more to the south, where
                // tall trees poke up into view), so skip the forest beyond that
                if (tx < -5 || tx >= Map.Width + 5 || ty < -5) continue;

                float cx = tx + 0.5f + (Rand(tx, ty, 1) - 0.5f) * 0.12f;
                float cz = ty + 0.5f + (Rand(tx, ty, 2) - 0.5f) * 0.12f;
                if (Map.Trees == TreeStyle.Pine) TreeModels.Pine(batches, cx, cz, tx, ty);
                else TreeModels.Round(batches, cx, cz, tx, ty);
            }
        }
    }

    // ------------------------------------------------------------------ tall grass, lawns, ledges, signs

    /// <summary>
    /// Two rows of clumps per tile, the second half a clump out of step with the first. The tops sway, and lean
    /// away from anyone walking through (see the field shaders).
    /// </summary>
    private void AddTallGrass(MeshBuilder b)
    {
        float h = 0.5f * VS;
        var normal = Vector3.Normalize(new Vector3(0, 0.7f, 0.7f));
        var top = MeshBuilder.Sway(Color.White, 1f);
        for (int ty = 0; ty < Map.Height; ty++)
        {
            for (int tx = 0; tx < Map.Width; tx++)
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
        for (int ty = 0; ty < Map.Height; ty++)
        {
            for (int tx = 0; tx < Map.Width; tx++)
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
        for (int ty = 0; ty < Map.Height; ty++)
        {
            for (int tx = 0; tx < Map.Width; tx++)
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

    private void AddSigns(MeshBatches batches)
    {
        var wood = new Color(170, 118, 72, 255);
        var white = batches.For(SceneTextures.White);
        var boards = batches.For(SceneTextures.SignBoard);
        for (int ty = 0; ty < Map.Height; ty++)
        {
            for (int tx = 0; tx < Map.Width; tx++)
            {
                if (Map.GetGroundTile(tx, ty) != TileType.Signpost || MapStructures.IsWallSign(Map, tx, ty)) continue;
                float cx = tx + 0.5f, cz = ty + 0.5f;
                float boardBottom = 0.26f * VS, boardTop = 0.62f * VS;
                white.Box(new(cx - 0.06f, 0, cz - 0.06f), new(cx + 0.06f, boardBottom + 0.1f, cz + 0.06f), MeshBuilder.Scale(wood, 0.8f), BoxFaces.Sides);
                white.Box(new(cx - 0.44f, boardBottom, cz - 0.05f), new(cx + 0.44f, boardTop, cz + 0.05f), wood, BoxFaces.Visible & ~BoxFaces.South);
                boards.Decal(new(cx - 0.44f, boardBottom, cz + 0.05f), new(cx + 0.44f, boardBottom, cz + 0.05f),
                    new(cx + 0.44f, boardTop, cz + 0.05f), new(cx - 0.44f, boardTop, cz + 0.05f), Color.White, South);
            }
        }
    }

    // ------------------------------------------------------------------ buildings

    private static Color RoofColor(BuildingInfo b) => b.Kind switch
    {
        BuildingKind.PokemonCenter => new Color(238, 104, 58, 255),
        BuildingKind.PokeMart => new Color(66, 122, 222, 255),
        BuildingKind.Lab => new Color(48, 178, 198, 255),
        _ => b.RoofTile switch
        {
            TileType.RoofRed => new Color(214, 82, 66, 255),
            TileType.RoofBlue => new Color(70, 118, 214, 255),
            _ => new Color(52, 166, 138, 255)
        }
    };

    private void AddBuilding(BuildingInfo b, MeshBatches batches)
    {
        bool house = b.Kind == BuildingKind.House;
        float wallH = (house ? 1.55f : 1.45f) * VS;
        float xL = b.X0 + 0.04f, xR = b.X1 + 1 - 0.04f;

        // The back wall sits a tile inside the footprint so the roof doesn't swallow the whole block
        float zF = b.Y1 + 1f, zB = b.Y0 + 1f;
        var roof = RoofColor(b);

        // Walls: log siding for homes, plaster for public buildings (textures repeat per tile and per screen row)
        batches.For(house ? SceneTextures.WoodSiding : SceneTextures.Plaster).Box(new(xL, 0, zB), new(xR, wallH, zF), Color.White, BoxFaces.Sides, 1f, VS);
        batches.For(SceneTextures.Stone).Box(new(xL - 0.03f, 0, zB - 0.03f), new(xR + 0.03f, 0.16f * VS, zF + 0.03f), Color.White, BoxFaces.Visible, 1f, VS);

        if (house) AddGableRoof(batches, xL, xR, zF, zB, wallH, roof);
        else AddHipRoof(batches, b, xL, xR, zF, zB, wallH, roof);

        AddFrontDetails(batches, b, zF, wallH);
    }

    /// <summary>Front-facing gable like Twinleaf's houses: the ridge runs north-south and the gable faces the camera.</summary>
    private void AddGableRoof(MeshBatches batches, float xL, float xR, float zF, float zB, float wallH, Color roof)
    {
        var shingles = batches.For(SceneTextures.Shingles);
        var white = batches.For(SceneTextures.White);
        var wood = batches.For(SceneTextures.WoodSiding);

        float ov = 0.3f, ovF = 0.34f, ovB = 0.2f, t = 0.12f;
        float xm = (xL + xR) / 2f;
        float eaveY = wallH - 0.12f;
        float ridgeY = wallH + 0.85f * VS;
        float zFront = zF + ovF, zBack = zB - ovB;
        float slope = MathF.Sqrt((xm - xL + ov) * (xm - xL + ov) + (ridgeY - eaveY) * (ridgeY - eaveY));
        var leftN = Vector3.Normalize(new Vector3(-(ridgeY - eaveY), xm - xL + ov, 0));
        var rightN = leftN with { X = -leftN.X };

        // Two slopes with shingle rows parallel to the ridge, and a slab edge showing the roof's thickness
        shingles.Quad(new(xL - ov, eaveY, zFront), new(xL - ov, eaveY, zBack), new(xm, ridgeY, zBack), new(xm, ridgeY, zFront),
            new(zFront, slope), new(zBack, slope), new(zBack, 0), new(zFront, 0), roof, leftN);
        shingles.Quad(new(xR + ov, eaveY, zBack), new(xR + ov, eaveY, zFront), new(xm, ridgeY, zFront), new(xm, ridgeY, zBack),
            new(zBack, slope), new(zFront, slope), new(zFront, 0), new(zBack, 0), roof, rightN);
        white.Box(new(xm - 0.1f, ridgeY - 0.04f, zBack), new(xm + 0.1f, ridgeY + 0.06f, zFront), MeshBuilder.Scale(roof, 0.8f), BoxFaces.Visible);

        // Gable triangle above the front wall, in the same siding
        float gv = (ridgeY - wallH) / VS;
        wood.Tri(new(xL, wallH, zF), new(xR, wallH, zF), new(xm, ridgeY - 0.1f, zF),
            new(0, gv), new(xR - xL, gv), new((xR - xL) / 2f, 0), South, South, South, Color.White, Color.White, Color.White);
        wood.Tri(new(xR, wallH, zB), new(xL, wallH, zB), new(xm, ridgeY - 0.1f, zB),
            new(0, gv), new(xR - xL, gv), new((xR - xL) / 2f, 0), -South, -South, -South, Color.White, Color.White, Color.White);

        // White bargeboards along the front edges of the roof, dark fascia under the side eaves
        var trim = new Color(244, 240, 230, 255);
        white.Quad(new(xL - ov, eaveY - t, zFront), new(xm, ridgeY - t, zFront), new(xm, ridgeY + 0.02f, zFront), new(xL - ov, eaveY + 0.02f, zFront),
            default, default, default, default, trim, South);
        white.Quad(new(xm, ridgeY - t, zFront), new(xR + ov, eaveY - t, zFront), new(xR + ov, eaveY + 0.02f, zFront), new(xm, ridgeY + 0.02f, zFront),
            default, default, default, default, trim, South);
        var fascia = PixelCanvas.Shadow(roof, 0.35f);
        white.Quad(new(xL - ov, eaveY - t, zBack), new(xL - ov, eaveY - t, zFront), new(xL - ov, eaveY, zFront), new(xL - ov, eaveY, zBack),
            default, default, default, default, fascia, -Vector3.UnitX);
        white.Quad(new(xR + ov, eaveY - t, zFront), new(xR + ov, eaveY - t, zBack), new(xR + ov, eaveY, zBack), new(xR + ov, eaveY, zFront),
            default, default, default, default, fascia, Vector3.UnitX);

        // Round window in the gable and a stone chimney on the left slope
        float gw = 0.46f, gy = wallH + 0.3f * VS;
        batches.For(SceneTextures.GableWindow, MeshPass.Glow).Decal(new(xm - gw / 2, gy - gw * VS / 2, zF + 0.02f), new(xm + gw / 2, gy - gw * VS / 2, zF + 0.02f),
            new(xm + gw / 2, gy + gw * VS / 2, zF + 0.02f), new(xm - gw / 2, gy + gw * VS / 2, zF + 0.02f), Color.White, South);

        float chX = xL + 0.55f, chZ = zBack + 0.6f;
        var stone = batches.For(SceneTextures.Stone);
        stone.Box(new(chX, eaveY, chZ), new(chX + 0.44f, ridgeY + 0.1f * VS, chZ + 0.44f), Color.White, BoxFaces.Sides, 1f, VS);
        white.Box(new(chX - 0.05f, ridgeY + 0.1f * VS, chZ - 0.05f), new(chX + 0.49f, ridgeY + 0.17f * VS, chZ + 0.49f), new Color(96, 92, 96, 255), BoxFaces.Visible);
    }

    /// <summary>Hip roof for Pokémon Centers, Marts and the lab: four slopes meeting at a short ridge.</summary>
    private void AddHipRoof(MeshBatches batches, BuildingInfo b, float xL, float xR, float zF, float zB, float wallH, Color roof)
    {
        var shingles = batches.For(SceneTextures.Shingles);
        var white = batches.For(SceneTextures.White);

        float ov = 0.28f, t = 0.16f;
        float eaveY = wallH - 0.1f;
        float ridgeY = wallH + 0.62f * VS;
        float x0 = xL - ov, x1 = xR + ov, z0 = zB - ov, z1 = zF + ov;
        float zm = (z0 + z1) / 2f;
        float inset = Math.Min((x1 - x0) / 2f - 0.05f, (z1 - z0) / 2f);
        float rx0 = x0 + inset, rx1 = x1 - inset;

        Vector3 N(Vector3 a, Vector3 bb, Vector3 d, Vector3 outward)
        {
            var n = MeshBuilder.Normal(a, bb, d);
            return Vector3.Dot(n, outward) < 0 ? -n : n;
        }

        var fa = new Vector3(x0, eaveY, z1); var fb = new Vector3(x1, eaveY, z1); var fc = new Vector3(rx1, ridgeY, zm); var fd = new Vector3(rx0, ridgeY, zm);
        shingles.Quad(fa, fb, fc, fd, new(x0, z1), new(x1, z1), new(rx1, zm), new(rx0, zm), roof, N(fa, fb, fd, new(0, 1, 1)));
        var ba = new Vector3(x1, eaveY, z0); var bb2 = new Vector3(x0, eaveY, z0); var bc = new Vector3(rx0, ridgeY, zm); var bd = new Vector3(rx1, ridgeY, zm);
        shingles.Quad(ba, bb2, bc, bd, new(x1, z0), new(x0, z0), new(rx0, zm), new(rx1, zm), roof, N(ba, bb2, bd, new(0, 1, -1)));
        shingles.Tri(new(x0, eaveY, z0), new(x0, eaveY, z1), new(rx0, ridgeY, zm), new(z0, x0), new(z1, x0), new(zm, rx0), roof, new Vector3(-1, 1, 0));
        shingles.Tri(new(x1, eaveY, z1), new(x1, eaveY, z0), new(rx1, ridgeY, zm), new(z1, x1), new(z0, x1), new(zm, rx1), roof, new Vector3(1, 1, 0));
        white.Box(new(rx0, ridgeY - 0.04f, zm - 0.08f), new(rx1, ridgeY + 0.05f, zm + 0.08f), MeshBuilder.Scale(roof, 0.8f), BoxFaces.Visible);

        // Eave fascia all round
        var fascia = PixelCanvas.Shadow(roof, 0.35f);
        white.Quad(new(x0, eaveY - t, z1), new(x1, eaveY - t, z1), new(x1, eaveY, z1), new(x0, eaveY, z1), default, default, default, default, fascia, South);
        white.Quad(new(x0, eaveY - t, z0), new(x0, eaveY - t, z1), new(x0, eaveY, z1), new(x0, eaveY, z0), default, default, default, default, fascia, -Vector3.UnitX);
        white.Quad(new(x1, eaveY - t, z1), new(x1, eaveY - t, z0), new(x1, eaveY, z0), new(x1, eaveY, z1), default, default, default, default, fascia, Vector3.UnitX);

        // Coloured band under the eaves (red for Centers, blue for Marts) like Platinum's buildings
        if (b.Kind != BuildingKind.Lab)
        {
            var band = PixelCanvas.Shadow(roof, 0.1f);
            float by0 = wallH - 0.3f * VS;
            white.Quad(new(xL - 0.01f, by0, zF + 0.01f), new(xR + 0.01f, by0, zF + 0.01f), new(xR + 0.01f, eaveY - t, zF + 0.01f), new(xL - 0.01f, eaveY - t, zF + 0.01f),
                default, default, default, default, band, South);
        }
    }

    /// <summary>A patch of ground lit through a window or door after dark: brightest at the wall, fading to the south.</summary>
    private static void AddLightPool(MeshBatches batches, float cx, float zWall, float width, float depth)
    {
        const float y = 0.03f;
        batches.For(SceneTextures.LightPool, MeshPass.Light).Quad(
            new(cx - width / 2, y, zWall + depth), new(cx + width / 2, y, zWall + depth), new(cx + width / 2, y, zWall), new(cx - width / 2, y, zWall),
            new(0, 1), new(1, 1), new(1, 0), new(0, 0), Color.White, Up);
    }

    private void AddFrontDetails(MeshBatches batches, BuildingInfo b, float zF, float wallH)
    {
        bool house = b.Kind == BuildingKind.House;
        var white = batches.For(SceneTextures.White);
        var frame = new Color(248, 246, 240, 255);
        var stoneStep = new Color(176, 170, 164, 255);

        foreach (var (x, _) in b.Doors)
        {
            float cx = x + 0.5f;
            float w = house ? 0.66f : 0.98f, h = (house ? 1.08f : 1.05f) * VS;
            var decal = batches.For(house ? SceneTextures.DoorWood : SceneTextures.DoorGlass, MeshPass.Glow);
            decal.Decal(new(cx - w / 2, 0, zF + 0.012f), new(cx + w / 2, 0, zF + 0.012f), new(cx + w / 2, h, zF + 0.012f), new(cx - w / 2, h, zF + 0.012f), Color.White, South);
            AddLightPool(batches, cx, zF, w * 3.2f, 2.8f);
            // Door frame and a stone step in front
            white.Box(new(cx - w / 2 - 0.07f, 0, zF), new(cx - w / 2, h + 0.06f, zF + 0.06f), frame, BoxFaces.Visible);
            white.Box(new(cx + w / 2, 0, zF), new(cx + w / 2 + 0.07f, h + 0.06f, zF + 0.06f), frame, BoxFaces.Visible);
            white.Box(new(cx - w / 2 - 0.07f, h, zF), new(cx + w / 2 + 0.07f, h + 0.08f, zF + 0.08f), frame, BoxFaces.Visible);
            white.Box(new(cx - w / 2 - 0.12f, 0, zF), new(cx + w / 2 + 0.12f, 0.06f, zF + 0.22f), stoneStep, BoxFaces.Visible);
        }

        foreach (var x in b.Plaques)
        {
            float cx = x + 0.5f, w = 0.6f, h = 0.4f * VS, y0 = 0.62f * VS;
            batches.For(SceneTextures.Plaque).Decal(new(cx - w / 2, y0, zF + 0.015f), new(cx + w / 2, y0, zF + 0.015f),
                new(cx + w / 2, y0 + h, zF + 0.015f), new(cx - w / 2, y0 + h, zF + 0.015f), Color.White, South);
        }

        for (int x = b.X0; x <= b.X1; x++)
        {
            bool nearDoor = b.Doors.Exists(d => Math.Abs(d.X - x) <= 1);
            if (nearDoor || b.Plaques.Contains(x)) continue;
            float cx = x + 0.5f, w = 0.66f, h = 0.5f * VS, y0 = 0.55f * VS;
            batches.For(SceneTextures.Window, MeshPass.Glow).Decal(new(cx - w / 2, y0, zF + 0.01f), new(cx + w / 2, y0, zF + 0.01f),
                new(cx + w / 2, y0 + h, zF + 0.01f), new(cx - w / 2, y0 + h, zF + 0.01f), Color.White, South);
            AddLightPool(batches, cx, zF, 2.2f, 2.2f);
            // Frame and sill stand proud of the wall
            white.Box(new(cx - w / 2 - 0.05f, y0 + h, zF), new(cx + w / 2 + 0.05f, y0 + h + 0.05f * VS, zF + 0.05f), frame, BoxFaces.Visible);
            white.Box(new(cx - w / 2 - 0.05f, y0 - 0.04f * VS, zF), new(cx - w / 2, y0 + h, zF + 0.05f), frame, BoxFaces.Visible);
            white.Box(new(cx + w / 2, y0 - 0.04f * VS, zF), new(cx + w / 2 + 0.05f, y0 + h, zF + 0.05f), frame, BoxFaces.Visible);
            white.Box(new(cx - w / 2 - 0.08f, y0 - 0.06f * VS, zF), new(cx + w / 2 + 0.08f, y0, zF + 0.12f), frame, BoxFaces.Visible);
            if (house)
            {
                // Window box full of flowers under each window
                float fy0 = y0 - 0.24f * VS, fy1 = y0 - 0.06f * VS;
                white.Box(new(cx - w / 2, fy0, zF), new(cx + w / 2, fy1, zF + 0.16f), new Color(150, 98, 60, 255), BoxFaces.Top | BoxFaces.West | BoxFaces.East);
                batches.For(SceneTextures.FlowerBox).Decal(new(cx - w / 2, fy0, zF + 0.161f), new(cx + w / 2, fy0, zF + 0.161f),
                    new(cx + w / 2, fy1 + 0.1f * VS, zF + 0.161f), new(cx - w / 2, fy1 + 0.1f * VS, zF + 0.161f), Color.White, South);
            }
        }

        // Emblems stand on the roof edge above the main door
        if (b.Doors.Count > 0 && !house)
        {
            float cx = b.Doors[0].X + 0.5f;
            if (b.Kind == BuildingKind.PokemonCenter)
            {
                float w = 1.25f, h = w * VS, y0 = wallH - 0.25f * VS;
                batches.For(SceneTextures.CenterEmblem).Decal(new(cx - w / 2, y0, zF + 0.34f), new(cx + w / 2, y0, zF + 0.34f),
                    new(cx + w / 2, y0 + h, zF + 0.34f), new(cx - w / 2, y0 + h, zF + 0.34f), Color.White, South);
            }
            else if (b.Kind == BuildingKind.PokeMart)
            {
                float w = 2.0f, h = 0.8f * VS, y0 = wallH - 0.1f * VS;
                batches.For(SceneTextures.MartSign, MeshPass.Glow).Decal(new(cx - w / 2, y0, zF + 0.34f), new(cx + w / 2, y0, zF + 0.34f),
                    new(cx + w / 2, y0 + h, zF + 0.34f), new(cx - w / 2, y0 + h, zF + 0.34f), Color.White, South);
            }
        }
    }

    // ------------------------------------------------------------------ interiors

    /// <summary>
    /// A room seen like a doll's house: tall back and side walls with wallpaper and wainscot, a low cut-away
    /// front wall with a gap for the door, and 3D furniture.
    /// </summary>
    private void AddInterior(MeshBatches batches)
    {
        int w = Map.Width, h = Map.Height;
        var white = batches.For(SceneTextures.White);
        var wallTex = GroundBaker.BakeWallStrip(Map.Interior).ToTexture();
        Raylib.SetTextureWrap(wallTex, TextureWrap.Repeat);
        var walls = batches.For(wallTex);

        float wallH = 2.5f * VS;
        float left = 1f, right = w - 1f, back = 2f, front = h - 1f;
        var cap = new Color(62, 56, 74, 255);

        walls.Quad(new(left, 0, back), new(right, 0, back), new(right, wallH, back), new(left, wallH, back),
            new(0, 1), new(right - left, 1), new(right - left, 0), new(0, 0), Color.White, South);
        walls.Quad(new(left, 0, front), new(left, 0, back), new(left, wallH, back), new(left, wallH, front),
            new(0, 1), new(front - back, 1), new(front - back, 0), new(0, 0), MeshBuilder.Scale(Color.White, 0.92f), Vector3.UnitX);
        walls.Quad(new(right, 0, back), new(right, 0, front), new(right, wallH, front), new(right, wallH, back),
            new(0, 1), new(front - back, 1), new(front - back, 0), new(0, 0), MeshBuilder.Scale(Color.White, 0.92f), -Vector3.UnitX);

        // Dark wall tops frame the room
        white.Box(new(left - 0.5f, wallH, back - 0.5f), new(right + 0.5f, wallH + 0.04f, back), cap, BoxFaces.Top);
        white.Box(new(left - 0.5f, wallH, back), new(left, wallH + 0.04f, front + 0.5f), cap, BoxFaces.Top);
        white.Box(new(right, wallH, back), new(right + 0.5f, wallH + 0.04f, front + 0.5f), cap, BoxFaces.Top);

        // Low front wall, cut away so the camera can see in, with a gap for the door
        float stub = 0.32f;
        for (int tx = 1; tx < w - 1; tx++)
        {
            if (Map.GetGroundTile(tx, h - 1) == TileType.Door) continue;
            white.Box(new(tx, 0, front), new(tx + 1, stub, front + 0.5f), cap, BoxFaces.Top | BoxFaces.South | BoxFaces.North);
        }

        foreach (var prop in Map.Props)
        {
            PropModels.Build(prop, Map, VS, tex => batches.For(tex));
        }
        for (int ty = 2; ty < h - 1; ty++)
            for (int tx = 1; tx < w - 1; tx++)
                if (Map.GetGroundTile(tx, ty) == TileType.PC) PropModels.BuildPc(tx, ty, VS, tex => batches.For(tex));

        RoomCenter = new Vector3(w / 2f, 0.6f * VS, (back + front) / 2f + 0.2f);
    }
}
