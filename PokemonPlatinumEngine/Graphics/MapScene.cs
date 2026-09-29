using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The 3D model of one map, built once and cached: textured ground, sunken water, trees, tall grass,
/// ledges, signs, buildings and interior furniture. One world unit is one tile; +Y is up and +Z is south,
/// so the field camera looks north like the DS games.
/// </summary>
internal sealed class MapScene
{
    // Field camera from Pokémon Platinum (pret/pokeplatinum, src/overlay005/field_camera.c):
    // CAMERA_TYPE_DEFAULT is a perspective camera 666.9 units away, pitched -59.05°, with an 8.09° half FOV.
    // Interiors use CAMERA_TYPE_INTERIOR_ORTHOGRAPHIC, pitched -50.09° and 12 tiles tall.
    public const float OutdoorPitchDeg = 59.05f;
    public const float OutdoorFovYDeg = 16.18f;
    public const float OutdoorDistance = 666.922f / 16f;
    public const float IndoorPitchDeg = 50.09f;
    public const float IndoorViewHeight = 12f;
    public const float IndoorDistance = 40f;

    public const int OutdoorMargin = 8;

    public Map Map { get; }
    public bool Indoors => Map.IsIndoors;
    public float PitchDeg { get; }

    /// <summary>World height of one screen row: vertical sizes are divided by cos(pitch) so they read at full size.</summary>
    public float VS { get; }
    public int Margin { get; }
    public Color Background { get; }
    public Vector3 RoomCenter { get; private set; }

    private readonly List<(Mesh Mesh, Material Material)> parts = new();
    private readonly List<(int X, int Y)> waterTiles = new();
    private const float WaterLevel = -0.24f;

    private MapScene(Map map)
    {
        Map = map;
        PitchDeg = map.IsIndoors ? IndoorPitchDeg : OutdoorPitchDeg;
        VS = 1f / MathF.Cos(PitchDeg * MathF.PI / 180f);
        Margin = map.IsIndoors ? 0 : OutdoorMargin;
        Background = map.IsIndoors ? Color.Black : new Color(38, 100, 66, 255);
    }

    // ------------------------------------------------------------------ drawing

    public void DrawStatic()
    {
        foreach (var (mesh, material) in parts)
        {
            Raylib.DrawMesh(mesh, material, Matrix4x4.Identity);
        }
    }

    /// <summary>Water is drawn every frame so its texture can drift.</summary>
    public void DrawWater(float time)
    {
        if (waterTiles.Count == 0) return;
        var tex = SceneTextures.Water;
        float du = time * 0.035f, dv = time * 0.02f;

        foreach (var (x, y) in waterTiles)
        {
            Rlgl.CheckRenderBatchLimit(4);
            Rlgl.SetTexture(tex.Id);
            Rlgl.Begin(DrawMode.Quads);
            Rlgl.Color4ub(255, 255, 255, 255);
            float u0 = x * 0.5f + du, u1 = u0 + 0.5f, v0 = y * 0.5f + dv, v1 = v0 + 0.5f;
            Rlgl.TexCoord2f(u0, v1); Rlgl.Vertex3f(x, WaterLevel, y + 1);
            Rlgl.TexCoord2f(u1, v1); Rlgl.Vertex3f(x + 1, WaterLevel, y + 1);
            Rlgl.TexCoord2f(u1, v0); Rlgl.Vertex3f(x + 1, WaterLevel, y);
            Rlgl.TexCoord2f(u0, v0); Rlgl.Vertex3f(x, WaterLevel, y);
            Rlgl.End();
        }
        Rlgl.SetTexture(0);
    }

    // ------------------------------------------------------------------ building the scene

    public static MapScene Build(Map map, Shader shader)
    {
        var scene = new MapScene(map);
        var batches = new Batches();
        var buildings = MapStructures.FindBuildings(map);

        var ground = GroundBaker.BakeGround(map, scene.Margin, buildings).ToTexture();
        scene.AddGround(batches.For(ground));

        if (map.IsIndoors)
        {
            scene.AddInterior(batches);
        }
        else
        {
            scene.AddWaterBanks(batches.For(SceneTextures.White));
            scene.AddTrees(batches.For(SceneTextures.White));
            scene.AddTallGrass(batches.For(SceneTextures.GrassBlades));
            scene.AddLedges(batches.For(SceneTextures.White));
            scene.AddSigns(batches);
            foreach (var b in buildings) scene.AddBuilding(b, batches);
        }

        foreach (var (tex, builder) in batches.All)
        {
            if (builder.VertexCount == 0) continue;
            var material = Raylib.LoadMaterialDefault();
            material.Shader = shader;
            Raylib.SetMaterialTexture(ref material, MaterialMapIndex.Albedo, tex);
            scene.parts.Add((builder.Upload(), material));
        }
        return scene;
    }

    /// <summary>One mesh per texture keeps draw calls low.</summary>
    private sealed class Batches
    {
        private readonly Dictionary<uint, (Texture2D Tex, MeshBuilder Builder)> map = new();

        public MeshBuilder For(Texture2D tex)
        {
            if (!map.TryGetValue(tex.Id, out var entry))
            {
                entry = (tex, new MeshBuilder());
                map[tex.Id] = entry;
            }
            return entry.Builder;
        }

        public IEnumerable<(Texture2D, MeshBuilder)> All
        {
            get { foreach (var e in map.Values) yield return (e.Tex, e.Builder); }
        }
    }

    private TileType? TypeAt(int x, int y) => GroundBaker.TypeAt(Map, x, y);

    private static float Rand(int x, int y, int salt) => GroundBaker.Rand01(x, y, salt);

    /// <summary>Adds a quad, flipping its winding if needed so its lighting normal points along <paramref name="outward"/>.</summary>
    private static void OrientedQuad(MeshBuilder b, Vector3 a, Vector3 bb, Vector3 c, Vector3 d,
        Vector2 ta, Vector2 tb, Vector2 tc, Vector2 td, Color color, Vector3 outward, bool lit = true)
    {
        var n = MeshBuilder.Normal(a, bb, d);
        if (Vector3.Dot(n, outward) < 0f)
        {
            (a, bb) = (bb, a);
            (c, d) = (d, c);
            (ta, tb) = (tb, ta);
            (tc, td) = (td, tc);
            n = -n;
        }
        b.Quad(a, bb, c, d, ta, tb, tc, td, lit ? MeshBuilder.Lit(color, n) : color);
    }

    private static void OrientedTri(MeshBuilder b, Vector3 a, Vector3 bb, Vector3 c, Color color, Vector3 outward,
        bool banded = false, float variation = 0f)
    {
        var n = Vector3.Normalize(Vector3.Cross(bb - a, c - a));
        if (Vector3.Dot(n, outward) < 0f) n = -n;
        var col = banded ? MeshBuilder.LitBanded(color, n, variation) : MeshBuilder.Lit(color, n);
        b.Tri(a, bb, c, col);
    }

    // ------------------------------------------------------------------ ground & water

    private void AddGround(MeshBuilder b)
    {
        int tilesW = Map.Width + Margin * 2, tilesH = Map.Height + Margin * 2;
        for (int ty = -Margin; ty < Map.Height + Margin; ty++)
        {
            for (int tx = -Margin; tx < Map.Width + Margin; tx++)
            {
                var t = TypeAt(tx, ty);
                if (t == null) continue;
                if (t == TileType.Water)
                {
                    waterTiles.Add((tx, ty));
                    continue;
                }
                if (Map.IsIndoors && !IsInteriorFloor(tx, ty, t.Value)) continue;

                float u0 = (tx + Margin) / (float)tilesW, u1 = (tx + Margin + 1) / (float)tilesW;
                float v0 = (ty + Margin) / (float)tilesH, v1 = (ty + Margin + 1) / (float)tilesH;
                b.Quad(new(tx, 0, ty + 1), new(tx + 1, 0, ty + 1), new(tx + 1, 0, ty), new(tx, 0, ty),
                    new(u0, v1), new(u1, v1), new(u1, v0), new(u0, v0), Color.White);
            }
        }

        if (!Map.IsIndoors)
        {
            // Far skirt so nothing past the forest margin ever shows the clear color
            var skirt = new Color(52, 132, 86, 255);
            float x0 = -Margin, x1 = Map.Width + Margin, z0 = -Margin, z1 = Map.Height + Margin, far = 80f;
            b.Quad(new(x0 - far, -0.02f, z0), new(x1 + far, -0.02f, z0), new(x1 + far, -0.02f, z0 - far), new(x0 - far, -0.02f, z0 - far),
                Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, skirt);
            b.Quad(new(x0 - far, -0.02f, z1 + far), new(x1 + far, -0.02f, z1 + far), new(x1 + far, -0.02f, z1), new(x0 - far, -0.02f, z1),
                Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, skirt);
            b.Quad(new(x0 - far, -0.02f, z1), new(x0, -0.02f, z1), new(x0, -0.02f, z0), new(x0 - far, -0.02f, z0),
                Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, skirt);
            b.Quad(new(x1, -0.02f, z1), new(x1 + far, -0.02f, z1), new(x1 + far, -0.02f, z0), new(x1, -0.02f, z0),
                Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, skirt);
        }
    }

    /// <summary>The floor runs to the front edge of the room so the exit mat sits inside it.</summary>
    private bool IsInteriorFloor(int tx, int ty, TileType t)
    {
        if (ty < 2) return false;
        return t == TileType.Door || (tx > 0 && tx < Map.Width - 1);
    }

    /// <summary>Vertical banks where land drops down to the water surface.</summary>
    private void AddWaterBanks(MeshBuilder b)
    {
        var stone = new Color(150, 140, 130, 255);
        var sand = new Color(214, 196, 150, 255);
        float bottom = WaterLevel - 0.06f;

        foreach (var (x, y) in waterTiles)
        {
            var col = Map.InBounds(x, y) ? stone : sand;
            if (TypeAt(x, y - 1) is { } n && n != TileType.Water)
                OrientedQuad(b, new(x, bottom, y), new(x + 1, bottom, y), new(x + 1, 0, y), new(x, 0, y),
                    Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, col, new(0, 0, 1));
            if (TypeAt(x, y + 1) is { } s && s != TileType.Water)
                OrientedQuad(b, new(x, bottom, y + 1), new(x + 1, bottom, y + 1), new(x + 1, 0, y + 1), new(x, 0, y + 1),
                    Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, col, new(0, 0, -1));
            if (TypeAt(x - 1, y) is { } w && w != TileType.Water)
                OrientedQuad(b, new(x, bottom, y), new(x, bottom, y + 1), new(x, 0, y + 1), new(x, 0, y),
                    Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, col, new(1, 0, 0));
            if (TypeAt(x + 1, y) is { } e && e != TileType.Water)
                OrientedQuad(b, new(x + 1, bottom, y), new(x + 1, bottom, y + 1), new(x + 1, 0, y + 1), new(x + 1, 0, y),
                    Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, col, new(-1, 0, 0));
        }
    }

    // ------------------------------------------------------------------ trees

    private void AddTrees(MeshBuilder b)
    {
        for (int ty = -Margin; ty < Map.Height + Margin; ty++)
        {
            for (int tx = -Margin; tx < Map.Width + Margin; tx++)
            {
                if (TypeAt(tx, ty) is not (TileType.Tree or TileType.TreeTrunk)) continue;
                float cx = tx + 0.5f + (Rand(tx, ty, 1) - 0.5f) * 0.1f;
                float cz = ty + 0.5f + (Rand(tx, ty, 2) - 0.5f) * 0.1f;
                if (Map.Trees == TreeStyle.Pine) AddPine(b, cx, cz, tx, ty);
                else AddRoundTree(b, cx, cz, tx, ty);
            }
        }
    }

    private static void AddPrism(MeshBuilder b, float cx, float cz, float r, float y0, float y1, int sides, Color color)
    {
        for (int i = 0; i < sides; i++)
        {
            float a0 = i * MathF.Tau / sides, a1 = (i + 1) * MathF.Tau / sides;
            var p0 = new Vector3(cx + MathF.Cos(a0) * r, y0, cz + MathF.Sin(a0) * r);
            var p1 = new Vector3(cx + MathF.Cos(a1) * r, y0, cz + MathF.Sin(a1) * r);
            var mid = new Vector3(MathF.Cos((a0 + a1) / 2), 0, MathF.Sin((a0 + a1) / 2));
            OrientedQuad(b, p0, p1, p1 with { Y = y1 }, p0 with { Y = y1 }, Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, color, mid);
        }
    }

    /// <summary>Sinnoh pine: three layered, star-shaped cones.</summary>
    private void AddPine(MeshBuilder b, float cx, float cz, int tx, int ty)
    {
        float s = 0.92f + Rand(tx, ty, 3) * 0.16f;
        var trunk = new Color(112, 76, 50, 255);
        var leaf = PixelCanvas.Mix(new Color(40, 128, 76, 255), new Color(56, 148, 86, 255), Rand(tx, ty, 4));
        AddPrism(b, cx, cz, 0.1f, 0f, 0.9f * s, 6, trunk);

        (float BaseY, float ApexY, float R)[] tiers =
        {
            (0.42f, 2.15f, 0.66f),
            (1.35f, 3.05f, 0.54f),
            (2.25f, 3.95f, 0.4f)
        };
        const int points = 12;
        float spin = Rand(tx, ty, 5) * MathF.Tau;
        foreach (var (baseY, apexY, radius) in tiers)
        {
            var apex = new Vector3(cx, apexY * s, cz);
            var ring = new Vector3[points];
            for (int i = 0; i < points; i++)
            {
                float a = spin + i * MathF.Tau / points;
                bool outer = i % 2 == 0;
                float r = radius * s * (outer ? 1f : 0.78f);
                float y = (baseY - (outer ? 0.12f : 0f)) * s;
                ring[i] = new Vector3(cx + MathF.Cos(a) * r, y, cz + MathF.Sin(a) * r);
            }
            for (int i = 0; i < points; i++)
            {
                var p0 = ring[i];
                var p1 = ring[(i + 1) % points];
                var outward = (p0 + p1) / 2f - new Vector3(cx, (p0.Y + p1.Y) / 2f, cz) + new Vector3(0, 0.4f, 0);
                OrientedTri(b, apex, p0, p1, leaf, outward, banded: true);
            }
        }
    }

    /// <summary>Round broadleaf tree: a lumpy low-poly crown on a short trunk.</summary>
    private void AddRoundTree(MeshBuilder b, float cx, float cz, int tx, int ty)
    {
        float s = 0.92f + Rand(tx, ty, 3) * 0.16f;
        var trunk = new Color(116, 80, 52, 255);
        var leaf = PixelCanvas.Mix(new Color(48, 140, 78, 255), new Color(66, 158, 88, 255), Rand(tx, ty, 4));
        AddPrism(b, cx, cz, 0.12f, 0f, 0.8f * s, 6, trunk);

        // Crown sits low on a short trunk, so rows of trees read as a wall of foliage
        AddBlob(b, new Vector3(cx, 1.62f * s, cz), new Vector3(0.68f, 1.0f, 0.62f) * s, 8, 5, leaf, tx, ty, 10);
        AddBlob(b, new Vector3(cx - 0.16f, 2.35f * s, cz + 0.06f), new Vector3(0.42f, 0.6f, 0.38f) * s, 6, 4,
            PixelCanvas.Light1(leaf, 0.1f), tx, ty, 20);
    }

    private static void AddBlob(MeshBuilder b, Vector3 center, Vector3 radii, int slices, int stacks, Color color, int tx, int ty, int salt)
    {
        // Latitude/longitude ellipsoid with jittered radii, shaded per face in a few flat bands
        var pts = new Vector3[stacks + 1, slices];
        for (int i = 0; i <= stacks; i++)
        {
            float phi = MathF.PI * i / stacks;
            for (int j = 0; j < slices; j++)
            {
                float theta = MathF.Tau * j / slices + i * 0.3f;
                float jitter = 1f + (GroundBaker.Rand01(tx * 31 + i, ty * 17 + j, salt) - 0.5f) * 0.18f;
                if (i == 0 || i == stacks) jitter = 1f;
                pts[i, j] = center + new Vector3(
                    MathF.Sin(phi) * MathF.Cos(theta) * radii.X * jitter,
                    MathF.Cos(phi) * radii.Y * jitter,
                    MathF.Sin(phi) * MathF.Sin(theta) * radii.Z * jitter);
            }
        }

        for (int i = 0; i < stacks; i++)
        {
            for (int j = 0; j < slices; j++)
            {
                int jn = (j + 1) % slices;
                var a = pts[i, j];
                var bb = pts[i, jn];
                var c = pts[i + 1, jn];
                var d = pts[i + 1, j];
                var faceCenter = (a + bb + c + d) / 4f;
                var outward = faceCenter - center;
                float variation = (GroundBaker.Rand01(tx + j, ty + i, salt + 5) - 0.5f) * 0.25f;
                if (i > 0) OrientedTri(b, a, bb, c, color, outward, banded: true, variation);
                if (i < stacks - 1) OrientedTri(b, a, c, d, color, outward, banded: true, variation);
            }
        }
    }

    // ------------------------------------------------------------------ tall grass, ledges, signs

    private void AddTallGrass(MeshBuilder b)
    {
        float h = 0.42f * VS;
        float[] rows = { 0.18f, 0.46f, 0.8f };
        for (int ty = 0; ty < Map.Height; ty++)
        {
            for (int tx = 0; tx < Map.Width; tx++)
            {
                if (Map.GetGroundTile(tx, ty) != TileType.TallGrass) continue;
                for (int i = 0; i < rows.Length; i++)
                {
                    float z = ty + rows[i];
                    float top = h * (0.9f + Rand(tx, ty, 40 + i) * 0.2f);
                    float x0 = tx - 0.06f, x1 = tx + 1.06f;
                    float u0 = x0 + i * 0.37f, u1 = x1 + i * 0.37f;
                    var bottomCol = new Color(170, 170, 170, 255);
                    var topCol = i == rows.Length - 1 ? Color.White : new Color(222, 222, 222, 255);
                    b.Quad(new(x0, 0, z), new(x1, 0, z), new(x1, top, z), new(x0, top, z),
                        new(u0, 1), new(u1, 1), new(u1, 0), new(u0, 0), bottomCol, topCol);
                }
            }
        }
    }

    private void AddLedges(MeshBuilder b)
    {
        float h = 0.22f * VS;
        var grassTop = new Color(136, 226, 144, 255);
        var face = new Color(178, 138, 90, 255);
        for (int ty = 0; ty < Map.Height; ty++)
        {
            for (int tx = 0; tx < Map.Width; tx++)
            {
                if (Map.GetGroundTile(tx, ty) != TileType.LedgeDown) continue;
                bool left = Map.InBounds(tx - 1, ty) && Map.GetGroundTile(tx - 1, ty) == TileType.LedgeDown;
                bool right = Map.InBounds(tx + 1, ty) && Map.GetGroundTile(tx + 1, ty) == TileType.LedgeDown;
                float x0 = tx + (left ? 0f : 0.1f), x1 = tx + 1 - (right ? 0f : 0.1f);
                float zBack = ty + 0.12f, zTop = ty + 0.42f, zFront = ty + 0.62f;

                // Grassy slope up from the north, flat top, then a dirt face dropping to the south
                OrientedQuad(b, new(x0, 0, zBack), new(x1, 0, zBack), new(x1, h, zTop), new(x0, h, zTop),
                    Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, grassTop, new(0, 1, -0.5f));
                OrientedQuad(b, new(x0, h, zTop), new(x1, h, zTop), new(x1, h, zFront), new(x0, h, zFront),
                    Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, PixelCanvas.Light1(grassTop, 0.15f), new(0, 1, 0));
                var faceTop = MeshBuilder.Lit(face, new(0, 0, 1));
                var faceBottom = MeshBuilder.Scale(faceTop, 0.72f);
                b.Quad(new(x0, 0, zFront), new(x1, 0, zFront), new(x1, h, zFront), new(x0, h, zFront),
                    Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, faceBottom, faceTop);

                if (!left)
                {
                    b.Tri(new(x0, 0, zBack), new(x0, h, zTop), new(x0, 0, zTop), MeshBuilder.Lit(face, new(-1, 0, 0)));
                    OrientedQuad(b, new(x0, 0, zTop), new(x0, 0, zFront), new(x0, h, zFront), new(x0, h, zTop),
                        Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, face, new(-1, 0, 0));
                }
                if (!right)
                {
                    b.Tri(new(x1, 0, zBack), new(x1, h, zTop), new(x1, 0, zTop), MeshBuilder.Lit(face, new(1, 0, 0)));
                    OrientedQuad(b, new(x1, 0, zTop), new(x1, 0, zFront), new(x1, h, zFront), new(x1, h, zTop),
                        Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, face, new(1, 0, 0));
                }
            }
        }
    }

    private void AddSigns(Batches batches)
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
                white.Box(new(cx - 0.06f, 0, cz - 0.06f), new(cx + 0.06f, boardBottom + 0.1f, cz + 0.06f), PixelCanvas.Shadow(wood, 0.2f));
                white.Box(new(cx - 0.44f, boardBottom, cz - 0.05f), new(cx + 0.44f, boardTop, cz + 0.05f), wood, south: false);
                boards.Quad(new(cx - 0.44f, boardBottom, cz + 0.05f), new(cx + 0.44f, boardBottom, cz + 0.05f),
                    new(cx + 0.44f, boardTop, cz + 0.05f), new(cx - 0.44f, boardTop, cz + 0.05f),
                    new(0, 1), new(1, 1), new(1, 0), new(0, 0), MeshBuilder.Lit(Color.White, new(0, 0, 1)));
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

    private void AddBuilding(BuildingInfo b, Batches batches)
    {
        bool house = b.Kind == BuildingKind.House;
        float wallH = (house ? 1.55f : 1.45f) * VS;
        float xL = b.X0 + 0.04f, xR = b.X1 + 1 - 0.04f;

        // The back wall sits a tile inside the footprint so the roof doesn't swallow the whole block
        float zF = b.Y1 + 1f, zB = b.Y0 + 1f;
        var roof = RoofColor(b);

        // Walls: log siding for homes, plaster for public buildings. Textures repeat once per tile / screen row.
        var walls = batches.For(house ? SceneTextures.WoodSiding : SceneTextures.Plaster);
        AddWallFace(walls, new(xL, 0, zF), new(xR, 0, zF), wallH, new(0, 0, 1));
        AddWallFace(walls, new(xR, 0, zB), new(xL, 0, zB), wallH, new(0, 0, -1));
        AddWallFace(walls, new(xL, 0, zB), new(xL, 0, zF), wallH, new(-1, 0, 0));
        AddWallFace(walls, new(xR, 0, zF), new(xR, 0, zB), wallH, new(1, 0, 0));

        // Stone foundation band and a trim line where the roof meets the wall
        var white = batches.For(SceneTextures.White);
        var foundation = house ? new Color(158, 150, 146, 255) : new Color(128, 134, 150, 255);
        float fH = 0.14f * VS;
        white.Box(new(xL - 0.02f, 0, zB - 0.02f), new(xR + 0.02f, fH, zF + 0.012f), foundation, top: false);

        if (house) AddGableRoof(batches, b, xL, xR, zF, zB, wallH, roof);
        else AddHipRoof(batches, b, xL, xR, zF, zB, wallH, roof);

        AddFrontDetails(batches, b, zF, wallH);
    }

    private void AddWallFace(MeshBuilder b, Vector3 start, Vector3 end, float height, Vector3 outward)
    {
        float length = Vector3.Distance(start, end);
        float v = height / VS;
        OrientedQuad(b, start, end, end with { Y = height }, start with { Y = height },
            new(0, v), new(length, v), new(length, 0), new(0, 0), Color.White, outward);
    }

    /// <summary>Front-facing gable like Twinleaf's houses: the ridge runs north-south and the gable faces the camera.</summary>
    private void AddGableRoof(Batches batches, BuildingInfo b, float xL, float xR, float zF, float zB, float wallH, Color roof)
    {
        var shingles = batches.For(SceneTextures.Shingles);
        var white = batches.For(SceneTextures.White);
        var wood = batches.For(SceneTextures.WoodSiding);

        float ov = 0.3f, ovF = 0.32f, ovB = 0.2f;
        float xm = (xL + xR) / 2f;
        float eaveY = wallH - 0.12f;
        float ridgeY = wallH + 0.85f * VS;
        float zFront = zF + ovF, zBack = zB - ovB;
        float slope = MathF.Sqrt((xm - xL + ov) * (xm - xL + ov) + (ridgeY - eaveY) * (ridgeY - eaveY));

        // Two slopes; shingle rows run parallel to the ridge
        OrientedQuad(shingles, new(xL - ov, eaveY, zFront), new(xL - ov, eaveY, zBack), new(xm, ridgeY, zBack), new(xm, ridgeY, zFront),
            new(zFront, slope), new(zBack, slope), new(zBack, 0), new(zFront, 0), roof, new(-1, 1, 0));
        OrientedQuad(shingles, new(xR + ov, eaveY, zBack), new(xR + ov, eaveY, zFront), new(xm, ridgeY, zFront), new(xm, ridgeY, zBack),
            new(zBack, slope), new(zFront, slope), new(zFront, 0), new(zBack, 0), roof, new(1, 1, 0));

        // Gable triangle above the front wall, in the same siding
        float gv = (ridgeY - wallH) / VS;
        wood.Tri(new(xL, wallH, zF), new(xR, wallH, zF), new(xm, ridgeY - 0.1f, zF),
            new(0, gv), new(xR - xL, gv), new((xR - xL) / 2f, 0),
            MeshBuilder.Lit(Color.White, new(0, 0, 1)), MeshBuilder.Lit(Color.White, new(0, 0, 1)), MeshBuilder.Lit(Color.White, new(0, 0, 1)));
        wood.Tri(new(xL, wallH, zB), new(xR, wallH, zB), new(xm, ridgeY - 0.1f, zB),
            new(0, gv), new(xR - xL, gv), new((xR - xL) / 2f, 0),
            MeshBuilder.Lit(Color.White, new(0, 0, -1)), MeshBuilder.Lit(Color.White, new(0, 0, -1)), MeshBuilder.Lit(Color.White, new(0, 0, -1)));

        // White bargeboards along the front edges of the roof and dark fascia under the eaves
        var trim = new Color(240, 236, 226, 255);
        float t = 0.16f;
        OrientedQuad(white, new(xL - ov, eaveY - t, zFront), new(xm, ridgeY - t, zFront), new(xm, ridgeY, zFront), new(xL - ov, eaveY, zFront),
            Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, trim, new(0, 0, 1));
        OrientedQuad(white, new(xm, ridgeY - t, zFront), new(xR + ov, eaveY - t, zFront), new(xR + ov, eaveY, zFront), new(xm, ridgeY, zFront),
            Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, trim, new(0, 0, 1));
        var fascia = PixelCanvas.Shadow(roof, 0.35f);
        OrientedQuad(white, new(xL - ov, eaveY - t, zBack), new(xL - ov, eaveY - t, zFront), new(xL - ov, eaveY, zFront), new(xL - ov, eaveY, zBack),
            Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, fascia, new(-1, 0, 0));
        OrientedQuad(white, new(xR + ov, eaveY - t, zFront), new(xR + ov, eaveY - t, zBack), new(xR + ov, eaveY, zBack), new(xR + ov, eaveY, zFront),
            Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, fascia, new(1, 0, 0));

        // Round window in the gable and a stone chimney on the left slope
        float gw = 0.46f;
        batches.For(SceneTextures.GableWindow).Quad(
            new(xm - gw / 2, wallH + 0.3f * VS - gw * VS / 2, zF + 0.02f), new(xm + gw / 2, wallH + 0.3f * VS - gw * VS / 2, zF + 0.02f),
            new(xm + gw / 2, wallH + 0.3f * VS + gw * VS / 2, zF + 0.02f), new(xm - gw / 2, wallH + 0.3f * VS + gw * VS / 2, zF + 0.02f),
            new(0, 1), new(1, 1), new(1, 0), new(0, 0), Color.White);

        float chX = xL + 0.55f, chZ = zBack + 0.7f;
        var stone = new Color(150, 146, 146, 255);
        white.Box(new(chX, eaveY, chZ), new(chX + 0.42f, ridgeY + 0.1f * VS, chZ + 0.42f), stone);
        white.Box(new(chX - 0.04f, ridgeY + 0.1f * VS, chZ - 0.04f), new(chX + 0.46f, ridgeY + 0.18f * VS, chZ + 0.46f), PixelCanvas.Shadow(stone, 0.3f));
    }

    /// <summary>Hip roof for Pokémon Centers, Marts and the lab: four slopes meeting at a short ridge.</summary>
    private void AddHipRoof(Batches batches, BuildingInfo b, float xL, float xR, float zF, float zB, float wallH, Color roof)
    {
        var shingles = batches.For(SceneTextures.Shingles);
        var white = batches.For(SceneTextures.White);

        float ov = 0.28f;
        float eaveY = wallH - 0.1f;
        float ridgeY = wallH + 0.62f * VS;
        float x0 = xL - ov, x1 = xR + ov, z0 = zB - ov, z1 = zF + ov;
        float zm = (z0 + z1) / 2f;
        float inset = Math.Min((x1 - x0) / 2f - 0.05f, (z1 - z0) / 2f);
        float rx0 = x0 + inset, rx1 = x1 - inset;

        OrientedQuad(shingles, new(x0, eaveY, z1), new(x1, eaveY, z1), new(rx1, ridgeY, zm), new(rx0, ridgeY, zm),
            new(x0, z1), new(x1, z1), new(rx1, zm), new(rx0, zm), roof, new(0, 1, 1));
        OrientedQuad(shingles, new(x1, eaveY, z0), new(x0, eaveY, z0), new(rx0, ridgeY, zm), new(rx1, ridgeY, zm),
            new(x1, z0), new(x0, z0), new(rx0, zm), new(rx1, zm), roof, new(0, 1, -1));
        var leftN = new Vector3(-1, 1, 0);
        var rightN = new Vector3(1, 1, 0);
        var leftCol = MeshBuilder.Lit(roof, leftN);
        var rightCol = MeshBuilder.Lit(roof, rightN);
        shingles.Tri(new(x0, eaveY, z0), new(x0, eaveY, z1), new(rx0, ridgeY, zm), new(z0, x0), new(z1, x0), new(zm, rx0), leftCol, leftCol, leftCol);
        shingles.Tri(new(x1, eaveY, z1), new(x1, eaveY, z0), new(rx1, ridgeY, zm), new(z1, x1), new(z0, x1), new(zm, rx1), rightCol, rightCol, rightCol);

        // Eave fascia all round
        var fascia = PixelCanvas.Shadow(roof, 0.35f);
        float t = 0.18f;
        OrientedQuad(white, new(x0, eaveY - t, z1), new(x1, eaveY - t, z1), new(x1, eaveY, z1), new(x0, eaveY, z1),
            Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, fascia, new(0, 0, 1));
        OrientedQuad(white, new(x0, eaveY - t, z0), new(x0, eaveY - t, z1), new(x0, eaveY, z1), new(x0, eaveY, z0),
            Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, fascia, new(-1, 0, 0));
        OrientedQuad(white, new(x1, eaveY - t, z1), new(x1, eaveY - t, z0), new(x1, eaveY, z0), new(x1, eaveY, z1),
            Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, fascia, new(1, 0, 0));

        // Coloured band under the eaves (red for Centers, blue for Marts) like Platinum's buildings
        if (b.Kind != BuildingKind.Lab)
        {
            var band = PixelCanvas.Shadow(roof, 0.1f);
            float by0 = wallH - 0.3f * VS;
            OrientedQuad(white, new(xL - 0.01f, by0, zF + 0.01f), new(xR + 0.01f, by0, zF + 0.01f), new(xR + 0.01f, eaveY - t, zF + 0.01f), new(xL - 0.01f, eaveY - t, zF + 0.01f),
                Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, band, new(0, 0, 1));
        }
    }

    private void AddFrontDetails(Batches batches, BuildingInfo b, float zF, float wallH)
    {
        bool house = b.Kind == BuildingKind.House;
        float z = zF + 0.015f;

        foreach (var (x, _) in b.Doors)
        {
            float cx = x + 0.5f;
            if (house)
            {
                float w = 0.64f, h = 1.08f * VS;
                Decal(batches.For(SceneTextures.DoorWood), cx - w / 2, 0, cx + w / 2, h, z);
            }
            else
            {
                float w = 0.96f, h = 1.05f * VS;
                Decal(batches.For(SceneTextures.DoorGlass), cx - w / 2, 0, cx + w / 2, h, z);
            }
        }

        foreach (var x in b.Plaques)
        {
            float cx = x + 0.5f, w = 0.6f, h = 0.4f * VS, y0 = 0.62f * VS;
            Decal(batches.For(SceneTextures.Plaque), cx - w / 2, y0, cx + w / 2, y0 + h, z);
        }

        for (int x = b.X0; x <= b.X1; x++)
        {
            bool nearDoor = b.Doors.Exists(d => Math.Abs(d.X - x) <= 1);
            if (nearDoor || b.Plaques.Contains(x)) continue;
            float cx = x + 0.5f, w = 0.66f, h = 0.5f * VS, y0 = 0.55f * VS;
            Decal(batches.For(SceneTextures.Window), cx - w / 2, y0, cx + w / 2, y0 + h, z);
        }

        // Emblems stand on the roof edge above the main door
        if (b.Doors.Count > 0 && !house)
        {
            float cx = b.Doors[0].X + 0.5f;
            if (b.Kind == BuildingKind.PokemonCenter)
            {
                float w = 1.25f, h = w * VS;
                Decal(batches.For(SceneTextures.CenterEmblem), cx - w / 2, wallH - 0.25f * VS, cx + w / 2, wallH - 0.25f * VS + h, zF + 0.34f);
            }
            else if (b.Kind == BuildingKind.PokeMart)
            {
                float w = 2.0f, h = 0.8f * VS;
                Decal(batches.For(SceneTextures.MartSign), cx - w / 2, wallH - 0.1f * VS, cx + w / 2, wallH - 0.1f * VS + h, zF + 0.34f);
            }
        }
    }

    private static void Decal(MeshBuilder b, float x0, float y0, float x1, float y1, float z)
    {
        var lit = MeshBuilder.Lit(Color.White, new(0, 0, 1));
        b.Quad(new(x0, y0, z), new(x1, y0, z), new(x1, y1, z), new(x0, y1, z), new(0, 1), new(1, 1), new(1, 0), new(0, 0), lit);
    }

    // ------------------------------------------------------------------ interiors

    private void AddInterior(Batches batches)
    {
        int w = Map.Width, h = Map.Height;
        var white = batches.For(SceneTextures.White);

        // Only the back wall is modelled, like Platinum's forced-perspective interiors
        float wallH = 2.0f * VS;
        var wallTex = GroundBaker.BakeBackWall(Map).ToTexture();
        batches.For(wallTex).Quad(new(1, 0, 2), new(w - 1, 0, 2), new(w - 1, wallH, 2), new(1, wallH, 2),
            new(0, 1), new(1, 1), new(1, 0), new(0, 0), MeshBuilder.Lit(Color.White, new(0, 0, 1)));
        white.Box(new(1, wallH, 1.55f), new(w - 1, wallH + 0.02f, 2), new Color(58, 54, 78, 255), north: false, south: false, west: false, east: false);

        (Color Top, Color Front, float Height) counter = Map.Interior switch
        {
            InteriorStyle.PokemonCenter => (new Color(250, 250, 252, 255), new Color(226, 94, 102, 255), 0.62f),
            InteriorStyle.PokeMart => (new Color(206, 214, 226, 255), new Color(124, 136, 160, 255), 0.95f),
            InteriorStyle.Lab => (new Color(236, 238, 246, 255), new Color(134, 144, 172, 255), 0.6f),
            _ => (new Color(222, 172, 116, 255), new Color(166, 116, 78, 255), 0.45f)
        };

        for (int ty = 2; ty < h - 1; ty++)
        {
            for (int tx = 1; tx < w - 1; tx++)
            {
                var t = Map.GetGroundTile(tx, ty);
                if (t == TileType.Wall) AddCounter(batches, tx, ty, counter.Top, counter.Front, counter.Height * VS);
                else if (t == TileType.PC) AddPc(batches, tx, ty);
            }
        }

        // The ortho camera frames the whole room: floor plus the back wall
        RoomCenter = new Vector3(w / 2f, 0.5f * VS, (2f + h) / 2f - 0.6f);
    }

    private bool IsCounter(int tx, int ty) =>
        Map.InBounds(tx, ty) && ty >= 2 && ty < Map.Height - 1 && tx > 0 && tx < Map.Width - 1 &&
        Map.GetGroundTile(tx, ty) == TileType.Wall;

    private void AddCounter(Batches batches, int tx, int ty, Color top, Color front, float height)
    {
        var b = batches.For(SceneTextures.White);
        bool up = IsCounter(tx, ty - 1), down = IsCounter(tx, ty + 1), left = IsCounter(tx - 1, ty), right = IsCounter(tx + 1, ty);
        var min = new Vector3(tx, 0, ty);
        var max = new Vector3(tx + 1, height, ty + 1);
        b.Box(min, max, top, top: true, north: false, south: false, west: !left, east: !right);
        if (!down)
        {
            if (Map.Interior == InteriorStyle.PokeMart)
            {
                batches.For(SceneTextures.MartGoods).Quad(new(tx, 0, ty + 1), new(tx + 1, 0, ty + 1), new(tx + 1, height, ty + 1), new(tx, height, ty + 1),
                    new(0, 1), new(1, 1), new(1, 0), new(0, 0), MeshBuilder.Lit(Color.White, new(0, 0, 1)));
            }
            else
            {
                var lit = MeshBuilder.Lit(front, new(0, 0, 1));
                b.Quad(new(tx, 0, ty + 1), new(tx + 1, 0, ty + 1), new(tx + 1, height, ty + 1), new(tx, height, ty + 1),
                    Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, MeshBuilder.Scale(lit, 0.8f), lit);
                // Light lip along the counter's front edge
                b.Box(new(tx, height - 0.06f, ty + 1f), new(tx + 1, height, ty + 1.03f), top, north: false, west: !left, east: !right);
            }
        }
    }

    private void AddPc(Batches batches, int tx, int ty)
    {
        var b = batches.For(SceneTextures.White);
        var desk = new Color(184, 142, 100, 255);
        var casing = new Color(214, 218, 230, 255);
        float deskH = 0.42f * VS;
        b.Box(new(tx + 0.06f, 0, ty + 0.15f), new(tx + 0.94f, deskH, ty + 0.95f), desk);
        float monY1 = deskH + 0.48f * VS;
        b.Box(new(tx + 0.2f, deskH, ty + 0.3f), new(tx + 0.8f, monY1, ty + 0.72f), casing, south: false);
        batches.For(SceneTextures.PcScreen).Quad(new(tx + 0.2f, deskH, ty + 0.72f), new(tx + 0.8f, deskH, ty + 0.72f),
            new(tx + 0.8f, monY1, ty + 0.72f), new(tx + 0.2f, monY1, ty + 0.72f),
            new(0, 1), new(1, 1), new(1, 0), new(0, 0), MeshBuilder.Lit(Color.White, new(0, 0, 1)));
    }
}
