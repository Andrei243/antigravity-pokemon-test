using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics.OpenWorld;

/// <summary>
/// How a building of an open map is laid out in the kit's tiles (plan 32 · OW9; style guide, "Open world",
/// "Buildings"): the kit draws at <see cref="OpenBuildingKit.MetresPerTile"/>, its front toward +z, so a building
/// facing another way is turned a quarter turn at a time about its box's middle. Its width is along its front and its
/// depth from its front back, in whole tiles: the box's, rounded, the kit's back wall standing a tile inside its
/// footprint as it always has (so the footprint is a tile deeper than the walls). Its front wall stands on the box's
/// front and its door in the bay of the map's door nearest the front. GPU-free.
/// </summary>
internal readonly record struct OpenBuildingPlan(int Width, int Depth, int DoorBay, float Turn, Vector2 FrontMiddle, Vector2 Out, Vector2 Along);

/// <summary>
/// The open world's buildings and solids (plan 32 · OW9): each building of the map is the kit's (<see cref="BuildingModels"/>,
/// <see cref="BuildingArt"/>) at <see cref="MetresPerTile"/>, seen from every side (its back wall and back fascia
/// drawn: <c>allSides</c>), on a plinth of stone down to the lowest ground under its walls; every other solid is a block
/// of cut stone. All of the map's art is painted into one sheet and its geometry made once a map on the window's
/// thread (a region of many buildings will want them a chunk at a time on the worker, as the grid's are), drawn by the
/// open scenery program through the three shadow maps, its lit glass glowing after dark and the light it throws on the
/// ground added on top. Every load through <see cref="Gpu"/>, under <c>openbuilding</c>.
/// </summary>
internal sealed class OpenBuildingKit
{
    /// <summary>How many metres a tile of the kit is drawn: a storey under a pitched roof 2.6 m, a door about 1.9 m, for a grown body of 1.6 m.</summary>
    public const float MetresPerTile = 1.5f;

    /// <summary>The town a block of stone's art is painted in, and the stone it is.</summary>
    private static Tone BlockStone => BuildingArt.StepStone;

    private sealed record Part(Mesh Mesh, Material Main, Material Depth);
    private readonly List<Part> parts = new();
    private SceneMeshes? lights;
    private Texture2D sheet;
    private bool sheetLoaded;
    private OpenMap? map;
    private (int Solids, int Buildings) madeOf;

    public int Buildings { get; private set; }
    public int Blocks { get; private set; }
    public int Triangles { get; private set; }

    /// <summary>
    /// The kit's plan of a building over its box (GPU-free): its size in tiles, which bay its door is in (the map's
    /// door whose middle is nearest its front, −1 for none) and how it is turned and where its front's middle is.
    /// </summary>
    public static OpenBuildingPlan PlanOf(OpenBuilding b, IReadOnlyList<OpenDoor> doors)
    {
        float x0 = b.Box.Min.X / (float)OpenPos.PerMetre, z0 = b.Box.Min.Z / (float)OpenPos.PerMetre;
        float x1 = b.Box.Max.X / (float)OpenPos.PerMetre, z1 = b.Box.Max.Z / (float)OpenPos.PerMetre;
        // The front's middle, the way out of it and the way along it, left to right as seen from in front
        var (front, outward, along, turn) = b.Faces switch
        {
            Direction.Up => (new Vector2((x0 + x1) / 2, z0), new Vector2(0, -1), new Vector2(-1, 0), MathF.PI),
            Direction.Right => (new Vector2(x1, (z0 + z1) / 2), new Vector2(1, 0), new Vector2(0, -1), MathF.PI / 2),
            Direction.Left => (new Vector2(x0, (z0 + z1) / 2), new Vector2(-1, 0), new Vector2(0, 1), -MathF.PI / 2),
            _ => (new Vector2((x0 + x1) / 2, z1), new Vector2(0, 1), new Vector2(1, 0), 0f)
        };
        bool sideways = b.Faces is Direction.Left or Direction.Right;
        float width = sideways ? z1 - z0 : x1 - x0, depth = sideways ? x1 - x0 : z1 - z0;
        int w = Math.Max(2, (int)MathF.Round(width / MetresPerTile));
        int d = Math.Max(2, (int)MathF.Round(depth / MetresPerTile)) + 1;
        int bay = -1;
        float best = 3f;
        foreach (var door in doors)
        {
            var mid = new Vector2((door.Shape.Min.X + door.Shape.Max.X) / 2f / OpenPos.PerMetre, (door.Shape.Min.Z + door.Shape.Max.Z) / 2f / OpenPos.PerMetre);
            float off = MathF.Abs(Vector2.Dot(mid - front, outward));
            float side = Vector2.Dot(mid - front, along);
            if (off >= best || MathF.Abs(side) > w * MetresPerTile / 2f) continue;
            best = off;
            bay = Math.Clamp((int)MathF.Floor(side / MetresPerTile + w / 2f), 0, w - 1);
        }
        return new OpenBuildingPlan(w, d, bay, turn, front, outward, along);
    }

    /// <summary>The map's buildings and blocks made and put on the card (once a map; the window's thread, which the roofs' shared textures need).</summary>
    public void Use(OpenMap? next, RenderContext context, OpenShaders shaders)
    {
        // Made again when the map is another or has had solids laid on it since (a tool's)
        var made = (next?.Solids.Count ?? 0, next?.Buildings.Count ?? 0);
        if (ReferenceEquals(next, map) && made == madeOf) return;
        Unload();
        map = next;
        madeOf = made;
        if (next == null) return;
        var ground = next.Ground;
        var art = new ArtSheet();
        var solid = new MeshBuilder();
        var roofs = new MeshBatches();
        var pools = new MeshBatches();
        var buildingBoxes = new HashSet<OpenBox>();
        Buildings = Blocks = 0;

        foreach (var b in next.Buildings)
        {
            buildingBoxes.Add(b.Box);
            var plan = PlanOf(b, next.Doors);
            // Seeds of the art go by where the building stands, so two buildings of a town are never painted alike
            int sx = b.Box.Min.X / OpenPos.PerMetre, sz = b.Box.Min.Z / OpenPos.PerMetre;
            var info = new BuildingInfo
            {
                X0 = sx, Y0 = sz, X1 = sx + plan.Width - 1, Y1 = sz + plan.Depth - 1, Kind = b.Kind, Town = b.Town,
                Storeys = b.Storeys, Sign = b.Sign, TownsRoof = true
            };
            if (plan.DoorBay >= 0) info.Doors.Add((sx + plan.DoorBay, "open"));
            var style = BuildingArt.StyleOf(info, b.Town);
            if (b.Storeys > 0) style = style with { Storeys = b.Storeys };

            // The ground at the front's middle, just outside it, and the lowest under the walls
            float baseY = HeightAt(ground, plan.FrontMiddle + plan.Out * 0.6f, b) ?? b.Box.Min.Y / (float)OpenPos.PerMetre;
            float lowest = baseY;
            foreach (var corner in Corners(b)) if (HeightAt(ground, corner, b) is { } y) lowest = MathF.Min(lowest, y);

            var kit = new KitBuilder(art, 1f);
            var own = new MeshBatches();
            var lit = new MeshBatches();
            var targets = new BuildingTargets
            {
                RoofTiles = color => own.For(SceneTextures.RoofTiles(color)),
                PublicLight = lit.For(SceneTextures.LightPool, MeshPass.Light),
                HomeLight = lit.For(SceneTextures.LightPool, MeshPass.HomeLight)
            };
            BuildingModels.Add(kit, info, style, targets, 0f, allSides: true);
            // A plinth of cut stone from the lowest ground under the walls up to the floor
            float drop = (baseY - lowest) / MetresPerTile * GroundBaker.ArtTile;
            if (drop > 1f)
                kit.Block("open.plinth", BlockStone, BuildingArt.Inset, plan.Width * BuildingArt.Bay - BuildingArt.Inset,
                    BuildingArt.Bay, plan.Depth * BuildingArt.Bay, -drop - 2f, 0f);

            // The kit's tiles to the world's metres: from the footprint's own corner, scaled, turned and stood on the front's middle
            var toWorld = Matrix4x4.CreateTranslation(-sx - plan.Width / 2f, 0, -sz - plan.Depth)
                * Matrix4x4.CreateScale(MetresPerTile)
                * Matrix4x4.CreateRotationY(plan.Turn)
                * Matrix4x4.CreateTranslation(plan.FrontMiddle.X, baseY, plan.FrontMiddle.Y);
            solid.Append(kit.Solid, toWorld);
            solid.Append(kit.Flat, toWorld);
            foreach (var (tex, pass, _, builder) in own.All) roofs.For(tex, pass).Append(builder, toWorld);
            foreach (var (tex, pass, _, builder) in lit.All) pools.For(tex, pass).Append(builder, toWorld);
            Buildings++;
        }

        // Every other solid: a block of cut stone over its box
        foreach (var box in next.Solids)
        {
            if (buildingBoxes.Contains(box)) continue;
            var kit = new KitBuilder(art, 1f);
            float w = (box.Max.X - box.Min.X) / (float)OpenPos.PerMetre, d = (box.Max.Z - box.Min.Z) / (float)OpenPos.PerMetre;
            float h = (box.Max.Y - box.Min.Y) / (float)OpenPos.PerMetre;
            float t = GroundBaker.ArtTile / MetresPerTile;
            kit.Block("open.block", BlockStone, 0, w * t, 0, d * t, 0, h * t);
            var toWorld = Matrix4x4.CreateScale(MetresPerTile)
                * Matrix4x4.CreateTranslation(box.Min.X / (float)OpenPos.PerMetre, box.Min.Y / (float)OpenPos.PerMetre, box.Min.Z / (float)OpenPos.PerMetre);
            solid.Append(kit.Solid, toWorld);
            Blocks++;
        }

        if (solid.VertexCount == 0 && !HasAny(roofs)) return;
        var fs = context.Shaders;
        Triangles = 0;
        if (solid.VertexCount > 0)
        {
            solid.ScaleUVs(1f / ArtSheet.Width, 1f / art.Height);
            var canvas = art.ToCanvas();
            sheet = canvas.ToTexture(1, "openbuilding");
            Raylib.SetTextureWrap(sheet, TextureWrap.Clamp);
            sheetLoaded = true;
            Add(solid, sheet, shaders, fs);
        }
        foreach (var (tex, _, _, builder) in roofs.All) Add(builder, tex, shaders, fs);
        if (HasAny(pools))
        {
            lights = new SceneMeshes();
            foreach (var (tex, pass, _, builder) in pools.All)
                if (builder.VertexCount > 0) lights.Add(builder.Upload("openbuilding"), tex, pass, null, fs);
        }
    }

    private static bool HasAny(MeshBatches batches)
    {
        foreach (var (_, _, _, b) in batches.All) if (b.VertexCount > 0) return true;
        return false;
    }

    private void Add(MeshBuilder builder, Texture2D texture, OpenShaders shaders, FieldShaders fs)
    {
        if (builder.VertexCount == 0) return;
        var mesh = builder.Upload("openbuilding");
        parts.Add(new Part(mesh, RenderContext.MaterialFor(shaders.Scenery, texture), RenderContext.MaterialFor(fs.Depth, texture)));
        Triangles += mesh.TriangleCount;
    }

    /// <summary>The ground's height at a point near a building: the highest ground facing up there, no higher than a metre over its box's bottom's top.</summary>
    private static float? HeightAt(OpenGround ground, Vector2 at, OpenBuilding b) =>
        ground.Footing(at.X, at.Y, b.Box.Max.Y / (double)OpenPos.PerMetre, b.Box.Min.Y / (double)OpenPos.PerMetre - 4) is { } f ? (float)f.Y : null;

    /// <summary>A box's four corners on the ground and its middle, a hair inside it.</summary>
    private static IEnumerable<Vector2> Corners(OpenBuilding b)
    {
        float x0 = b.Box.Min.X / (float)OpenPos.PerMetre + 0.1f, z0 = b.Box.Min.Z / (float)OpenPos.PerMetre + 0.1f;
        float x1 = b.Box.Max.X / (float)OpenPos.PerMetre - 0.1f, z1 = b.Box.Max.Z / (float)OpenPos.PerMetre - 0.1f;
        yield return new(x0, z0);
        yield return new(x1, z0);
        yield return new(x0, z1);
        yield return new(x1, z1);
        yield return new((x0 + x1) / 2, (z0 + z1) / 2);
    }

    /// <summary>The buildings into a shadow map (from either side, as the shadow pass draws everything).</summary>
    public int DrawDepth()
    {
        int triangles = 0;
        foreach (var part in parts)
        {
            Raylib.DrawMesh(part.Mesh, part.Depth, Matrix4x4.Identity);
            triangles += part.Mesh.TriangleCount;
        }
        return triangles;
    }

    /// <summary>
    /// The buildings in the scene's pass (the scenery's uniforms set), from either side: the kit's faces are made to be
    /// seen from the south and above, and an orbiting camera sees under an eave and behind a gable's verge too.
    /// </summary>
    public void Draw()
    {
        if (parts.Count == 0) return;
        Rlgl.DrawRenderBatchActive();
        Rlgl.DisableBackfaceCulling();
        foreach (var part in parts)
        {
            Raylib.DrawMesh(part.Mesh, part.Main, Matrix4x4.Identity);
            FrameProfiler.Count(part.Mesh.TriangleCount);
        }
        Rlgl.EnableBackfaceCulling();
    }

    /// <summary>The light the buildings' lamps and windows throw on the ground after dark, added on top.</summary>
    public void DrawLights(LightRig rig) => lights?.DrawLights(rig.LampGlow, rig.HomeGlow, rig.GlowColor);

    public unsafe void Unload()
    {
        foreach (var part in parts)
        {
            Gpu.Unload(part.Mesh);
            Raylib.MemFree(part.Main.Maps);
            Raylib.MemFree(part.Depth.Maps);
        }
        parts.Clear();
        lights?.Unload();
        lights = null;
        if (sheetLoaded) Gpu.Unload(sheet);
        sheetLoaded = false;
        map = null;
        Buildings = Blocks = Triangles = 0;
    }
}
