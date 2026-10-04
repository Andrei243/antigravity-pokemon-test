using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;
using Raylib_cs;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 01 · M4: the catalogue of the world's models, how a model becomes buildings and props on the map, and
/// that every kind of building and every prop is built and painted cleanly without a GPU.
/// </summary>
public class WorldModelTests
{
    private const float OutdoorVS = 1.944f;

    // ------------------------------------------------------------------ the catalogue

    [Fact]
    public void TheCatalogueSaysWhatEveryModelIs()
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var model in WorldModels.All)
        {
            Assert.True(names.Add(model.Name), $"{model.Name} is in the catalogue twice");
            Assert.False(string.IsNullOrWhiteSpace(model.What), $"{model.Name} has no description");
            Assert.Same(model, WorldModels.Of(model.Name));

            switch (model.Role)
            {
                case ModelRole.Scenery:
                    // Something that stands outdoors and blocks the way where the world says so
                    Assert.True(model.Prop == PropType.Bench || model.Prop >= PropType.Boulder && model.Prop < PropType.Rug, $"{model.Name} is a {model.Prop}, which is not an outdoor prop");
                    Assert.Null(model.Sign);
                    break;
                case ModelRole.Building:
                    Assert.InRange(model.Storeys, 0, 6);
                    if (model.Kind == BuildingKind.House) Assert.True(model.Town != null, $"the house {model.Name} belongs to no town");
                    if (model.Sign != null)
                    {
                        // A name fits an entrance, in the letters the sign alphabet has
                        Assert.InRange(model.Sign.Length, 3, 9);
                        Assert.All(model.Sign, letter => Assert.True(letter == ' ' || letter is >= 'A' and <= 'Z', $"{model.Name}: '{letter}' in {model.Sign}"));
                    }
                    break;
                default:
                    Assert.Null(model.Sign);
                    Assert.Null(model.Town);
                    break;
            }
            if (model.StandInUntil != null) Assert.Matches("^M([5-9]|1[0-2])$", model.StandInUntil);
        }

        // The eight Gyms are one model: the city says whose it is
        Assert.Equal(BuildingKind.Gym, WorldModels.Of("gym00")!.Kind);
        var themes = new[] { "oreburgh_city", "eterna_city", "hearthome_city", "veilstone_city", "pastoria_city", "canalave_city", "snowpoint_city", "sunyshore_city" }
            .Select(WorldModels.GymTheme).ToList();
        Assert.Equal(8, themes.Distinct().Count());
        Assert.DoesNotContain(null, themes);
        Assert.Null(WorldModels.GymTheme("twinleaf_town"));
        Assert.Equal(8, themes.Select(t => BuildingArt.GymColor(t)).Distinct().Count());
    }

    [Fact]
    public void EveryModelInTheGamesChunksIsInTheCatalogue()
    {
        var world = World.LoadAll().Single();
        int placed = 0;
        foreach (var entry in world.Index.Maps)
        {
            var matrix = world.Matrix(entry.Matrix);
            for (int cy = 0; cy < matrix.Height; cy++)
                for (int cx = 0; cx < matrix.Width; cx++)
                {
                    int id = matrix.ChunkAt(cx, cy);
                    if (id == WorldMatrixFile.NoChunk || world.Chunk(id) is not { } chunk) continue;
                    foreach (var prop in chunk.Props)
                    {
                        Assert.True(WorldModels.Of(prop.Name) != null, $"chunk {id:000} has the model '{prop.Name}', which the catalogue doesn't know");
                        placed++;
                    }
                }
        }
        Assert.True(placed >= 40, $"only {placed} models in the game's chunks");
    }

    // ------------------------------------------------------------------ from a model to buildings

    [Fact]
    public void BlockedTilesAreCutIntoRectanglesTheLargestFirst()
    {
        static IEnumerable<(int, int)> Rect(int x0, int z0, int x1, int z1)
        {
            for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++)
                    yield return (x, z);
        }

        // A house is one rectangle
        Assert.Equal(new[] { (2, 3, 6, 5) }, WorldMapBuilder.Rectangles(Rect(2, 3, 6, 5)));

        // A hall with a wing down its east side, and two posts before its door
        var shape = Rect(0, 0, 9, 3).Concat(Rect(7, 4, 9, 8)).Concat(new[] { (2, 4), (4, 4) }).ToList();
        var cut = WorldMapBuilder.Rectangles(shape);
        Assert.Equal((0, 0, 9, 3), cut[0]);
        Assert.Equal((7, 4, 9, 8), cut[1]);
        Assert.Equal(new[] { (2, 4, 2, 4), (4, 4, 4, 4) }, cut.Skip(2));

        // Whatever order the tiles come in, the cut is the same
        var shuffled = shape.OrderBy(t => (t.Item1 * 7 + t.Item2 * 13) % 11).ToList();
        Assert.Equal(cut, WorldMapBuilder.Rectangles(shuffled));
        Assert.Empty(WorldMapBuilder.Rectangles(Array.Empty<(int, int)>()));
    }

    /// <summary>A world of two chunks side by side, written to a folder of its own, with the models a test names.</summary>
    private static Map BuildWorld(Action<char[,], List<ChunkProp>[]> lay, IEnumerable<AreaWarp>? warps = null)
    {
        const int t = WorldChunkFile.Tiles;
        // '.' open lawn, '#' blocked, 'D' a door (blocked), over both chunks
        var grid = new char[t * 2, t];
        for (int z = 0; z < t; z++)
            for (int x = 0; x < t * 2; x++)
                grid[x, z] = '.';
        var props = new[] { new List<ChunkProp>(), new List<ChunkProp>() };
        lay(grid, props);

        string folder = Path.Combine(Path.GetTempPath(), "m4_world_" + Guid.NewGuid().ToString("N"));
        foreach (string sub in new[] { "chunks", "matrices", "areas" }) Directory.CreateDirectory(Path.Combine(folder, sub));
        for (int chunk = 0; chunk < 2; chunk++)
        {
            var file = new WorldChunkFile { Id = chunk, Props = props[chunk] };
            for (int z = 0; z < t; z++)
            {
                var behaviours = new System.Text.StringBuilder();
                var solid = new System.Text.StringBuilder();
                var cover = new System.Text.StringBuilder();
                for (int x = 0; x < t; x++)
                {
                    char tile = grid[chunk * t + x, z];
                    behaviours.Append(tile == 'D' ? "69" : "00");   // 0x69: a door
                    solid.Append(tile == '.' ? '.' : '#');
                    cover.Append('.');
                }
                file.Behaviours.Add(behaviours.ToString());
                file.Solid.Add(solid.ToString());
                file.Cover.Add(cover.ToString());
            }
            file.Heights.Add(new HeightPlate { Width = t, Depth = t, Height = 1 });
            File.WriteAllText(Path.Combine(folder, "chunks", $"{chunk:000}.json"), GameDataFiles.Serialize(file));
        }
        File.WriteAllText(Path.Combine(folder, "matrices", "000.json"), GameDataFiles.Serialize(new WorldMatrixFile
        {
            Id = 0, Width = 2, Height = 1, Chunks = new() { "0 1" }, Areas = new() { "0 0" }, AreaKeys = new() { "testville" }
        }));
        var area = new WorldAreaFile { Key = "testville", Name = "Testville", Matrix = 0, Kind = "Town" };
        area.Warps.AddRange(warps ?? Array.Empty<AreaWarp>());
        File.WriteAllText(Path.Combine(folder, "areas", "testville.json"), GameDataFiles.Serialize(area));

        var index = new WorldIndexFile { Region = "Test", Areas = { "testville" } };
        index.Maps.Add(new WorldMapEntry { Name = "Test", Matrix = 0 });
        try { return WorldMapBuilder.Build(World.Open(folder, index), index.Maps[0]); }
        finally { Directory.Delete(folder, recursive: true); }
    }

    private static void Block(char[,] grid, int x0, int z0, int x1, int z1, char tile = '#')
    {
        for (int z = z0; z <= z1; z++)
            for (int x = x0; x <= x1; x++)
                grid[x, z] = tile;
    }

    private static ChunkProp Model(string name, float x, float z, float width, float depth, float height = 4.5f) =>
        new() { Name = name, BoxX = x, BoxZ = z, Width = width, Depth = depth, Height = height, X = x + width / 2, Z = z + depth / 2, Y = 1 };

    [Fact]
    public void ABuildingStandsOnTheTilesItsModelBlocksAndKnowsItsDoors()
    {
        Assert.Equal(TileBehavior.Door, (TileBehavior)0x69);
        var map = BuildWorld((grid, props) =>
        {
            // A Twinleaf house, its door in its front wall
            Block(grid, 4, 4, 7, 6);
            grid[5, 6] = 'D';
            props[0].Add(Model("t1_h01", 3.9f, 3.8f, 4.2f, 2.9f, 4.4f));

            // A lab with its entrance built out in front, the door in the porch
            Block(grid, 12, 4, 19, 7);
            Block(grid, 12, 8, 16, 8);
            grid[14, 8] = 'D';
            props[0].Add(Model("t2_s01", 11.8f, 3.9f, 8.5f, 5.2f, 4.7f));

            // A market with an open porch: two sides, and the way in between them on open ground
            Block(grid, 4, 14, 9, 17);
            Block(grid, 5, 18, 6, 18);
            Block(grid, 8, 18, 9, 18);
            props[0].Add(Model("c8_s02", 3.8f, 13.9f, 6.6f, 5.2f, 3.7f));

            // A gate house across the border between the two chunks, entered through its ends
            Block(grid, 29, 20, 34, 25);
            props[0].Add(Model("gate_b", 28.8f, 19.9f, 6.4f, 6.2f, 3.9f));

            // A museum with a wing and two posts, and a fountain beside it
            Block(grid, 40, 4, 49, 7);
            Block(grid, 47, 8, 49, 12);
            grid[42, 8] = grid[44, 8] = '#';
            props[1].Add(Model("c3_s01", 7.9f, 3.9f, 10.2f, 9.3f, 5.9f));
            Block(grid, 52, 20, 55, 22);
            props[1].Add(Model("funsui", 20f, 19.9f, 4.1f, 3.6f, 2.9f));
        }, new[]
        {
            new AreaWarp { X = 5, Z = 6, To = "somewhere", ToWarp = 0 }, new AreaWarp { X = 14, Z = 8, To = "somewhere", ToWarp = 0 },
            new AreaWarp { X = 7, Z = 18, To = "somewhere", ToWarp = 0 }, new AreaWarp { X = 43, Z = 8, To = "somewhere", ToWarp = 0 },
            new AreaWarp { X = 28, Z = 22, To = "somewhere", ToWarp = 0 }, new AreaWarp { X = 35, Z = 22, To = "somewhere", ToWarp = 0 }
        });

        var buildings = MapStructures.BuildingsOf(map);
        Assert.Equal(map.PlacedBuildings, buildings);
        BuildingInfo At(int x, int y) => buildings.Single(b => x >= b.X0 && x <= b.X1 && y >= b.Y0 && y <= b.Y1);

        var house = At(5, 5);
        Assert.Equal(("t1_h01", BuildingKind.House, (Architecture?)Architecture.Timber), (house.Model, house.Kind, house.Town));
        Assert.Equal((4, 4, 7, 6), (house.X0, house.Y0, house.X1, house.Y1));
        Assert.Equal(new[] { 5 }, house.Doors.Select(d => d.X));
        Assert.Empty(house.Porch);
        Assert.Equal((TileType.Door, TileType.Wall, TileType.RoofRed), (map.GetGroundTile(5, 6), map.GetGroundTile(4, 6), map.GetGroundTile(5, 4)));

        // The lab: its block ends a row before the porch, which holds the door
        var lab = At(13, 5);
        Assert.Equal((BuildingKind.Lab, 12, 4, 19, 7), (lab.Kind, lab.X0, lab.Y0, lab.X1, lab.Y1));
        Assert.Equal(new[] { 12, 13, 14, 15, 16 }, lab.Porch);
        Assert.False(lab.PorchIsOpen);
        Assert.Equal(new[] { 14 }, lab.Doors.Select(d => d.X));
        Assert.True(map.IsSolid(17, 8) == false && map.GetGroundTile(16, 8) == TileType.Wall && map.GetGroundTile(14, 8) == TileType.Door);

        // The market: the door is in its wall, behind the open way between the porch's two sides
        var market = At(6, 15);
        Assert.Equal((BuildingKind.Shop, "MARKET"), (market.Kind, market.Sign));
        Assert.Equal(new[] { 5, 6, 8, 9 }, market.Porch);
        Assert.True(market.PorchIsOpen);
        Assert.Equal(new[] { 7 }, market.Doors.Select(d => d.X));
        Assert.False(map.IsSolid(7, 18));
        Assert.Equal(new[] { BuildingArt.BayKind.Window, BuildingArt.BayKind.Blank, BuildingArt.BayKind.Blank, BuildingArt.BayKind.Door, BuildingArt.BayKind.Blank, BuildingArt.BayKind.Blank },
            BuildingArt.BaysOf(market));

        // The gate house is one building though it lies in two chunks, and has its doors in its ends
        var gate = At(30, 22);
        Assert.Equal((BuildingKind.Gate, 29, 20, 34, 25), (gate.Kind, gate.X0, gate.Y0, gate.X1, gate.Y1));
        Assert.Equal(new[] { 22 }, gate.SideDoors);
        Assert.Empty(gate.Doors);
        Assert.DoesNotContain(BuildingArt.BayKind.Door, BuildingArt.BaysOf(gate));
        Assert.Single(Enumerable.Range(0, map.ChunkColumns), cx => MapScene.BuildingsIn(map, MapScene.ChunkWindow(map, cx, 0)).Contains(gate));

        // The museum is a main block with the way in and a plain wing; its posts are low walls of stone
        var museum = At(41, 5);
        Assert.Equal((BuildingKind.Museum, false, "MUSEUM"), (museum.Kind, museum.Annex, museum.Sign));
        Assert.Equal((40, 4, 49, 7), (museum.X0, museum.Y0, museum.X1, museum.Y1));
        Assert.Equal(new[] { 42, 44 }, museum.Porch);
        var wing = At(48, 10);
        Assert.Equal((BuildingKind.Museum, true, (string?)null, 1), (wing.Kind, wing.Annex, wing.Sign, wing.Storeys));
        Assert.Empty(wing.Doors);
        Assert.DoesNotContain(BuildingArt.BayKind.Door, BuildingArt.BaysOf(wing));

        // The fountain is a prop over its model's tiles, standing on lawn like the ground round it
        var fountain = map.Props.Single(p => p.Type == PropType.Fountain);
        Assert.Equal((52, 20, 4, 3, "funsui"), (fountain.X, fountain.Y, fountain.Width, fountain.Depth, fountain.Model));
        Assert.Equal((TileType.Grass, true), (map.GetGroundTile(53, 21), map.IsSolid(53, 21)));

        // Its houses decide how the town fences its yards
        Assert.Equal(Architecture.Timber, map.ArchitectureAt(5, 5));
    }

    // ------------------------------------------------------------------ styles

    private static BuildingInfo Sample(WorldModel model, int width = 5, int depth = 4, float height = 0f, bool annex = false, PokemonType? theme = null)
    {
        var b = new BuildingInfo
        {
            X0 = 10, Y0 = 10, X1 = 10 + width - 1, Y1 = 10 + depth - 1, Kind = model.Kind, RoofTile = TileType.RoofRed, Model = model.Name,
            Town = model.Town, Storeys = annex ? 1 : model.Storeys, Sign = annex ? null : model.Sign, Annex = annex, Height = height,
            Theme = model.Kind == BuildingKind.Gym ? theme ?? PokemonType.Rock : null
        };
        if (!annex) b.Doors.Add((10 + width / 2, "Somewhere"));
        return b;
    }

    [Fact]
    public void EveryTownBuildsItsOwnWay()
    {
        var houses = Enum.GetValues<Architecture>().ToDictionary(town => town,
            town => BuildingArt.StyleOf(new BuildingInfo { X0 = 0, Y0 = 0, X1 = 4, Y1 = 3, Kind = BuildingKind.House, Model = "a_house", Town = town }, Architecture.Timber));

        // No two towns' houses share both their walls and their roof
        Assert.Equal(houses.Count, houses.Values.Select(s => (s.Wall, s.RoofColor, s.Roof)).Distinct().Count());
        Assert.All(houses.Values, s => Assert.True(s.Home));
        Assert.Equal((WallKind.HalfTimber, RoofShape.Gable), (houses[Architecture.HalfTimber].Wall, houses[Architecture.HalfTimber].Roof));
        Assert.Equal(BuildingArt.SnowRoof, houses[Architecture.Snow].RoofColor);
        Assert.Equal(WallKind.Log, houses[Architecture.Snow].Wall);
        Assert.True(houses[Architecture.Seaside].Gear.HasFlag(RoofGear.Solar));
        Assert.Equal(RoofShape.Flat, houses[Architecture.City].Roof);

        // A building of the world follows its model's town, whatever the map round it is; a hand-made map's follows the map
        var eterna = new BuildingInfo { X0 = 0, Y0 = 0, X1 = 4, Y1 = 3, Kind = BuildingKind.House, Model = "c4_h01a", Town = Architecture.HalfTimber };
        Assert.Equal(WallKind.HalfTimber, BuildingArt.StyleOf(eterna, Architecture.City).Wall);
        var handMade = new BuildingInfo { X0 = 0, Y0 = 0, X1 = 4, Y1 = 3, Kind = BuildingKind.House, RoofTile = TileType.RoofBlue };
        Assert.Equal(new Color(70, 118, 214, 255), BuildingArt.StyleOf(handMade, Architecture.Plaster).RoofColor);

        // Storeys: a house grows one at 5.4 tiles; a block has one for every 1.95 tiles above 3.8
        BuildingStyle Tall(string model, float height) => BuildingArt.StyleOf(Sample(WorldModels.Of(model)!, height: height), Architecture.Timber);
        Assert.Equal(1, Tall("t1_h01", 4.4f).Storeys);
        Assert.Equal(2, Tall("t3_h01", 5.6f).Storeys);
        Assert.Equal(2, Tall("t1_s01", 5.7f).Storeys);
        Assert.Equal(new[] { 2, 3, 5 }, new[] { Tall("c1_b01c", 6.3f).Storeys, Tall("c1_b02a", 8.1f).Storeys, Tall("c1_b03", 12.1f).Storeys });
        Assert.Equal(BuildingArt.PitchedWall + BuildingArt.UpperStorey, Tall("t1_s01", 5.7f).WallHeight);
        Assert.Equal(3, Tall("c10_s01", 16.4f).Storeys);

        // A Gym is its leader's type; Snowpoint's Center keeps its sign under the snow
        var ghost = BuildingArt.StyleOf(Sample(WorldModels.Of("gym00")!, 7, 4, 4.9f, theme: PokemonType.Ghost), Architecture.Townhouse);
        Assert.Equal((BuildingArt.GymColor(PokemonType.Ghost), SignKind.Text, "GYM"), (ghost.RoofColor, ghost.Sign, ghost.SignText));
        var center = BuildingArt.StyleOf(Sample(WorldModels.Of("pc_01")!), Architecture.Timber);
        Assert.Equal((BuildingArt.SnowRoof, SignKind.Center), (center.RoofColor, center.Sign));

        // A shop is its town's house with its name on an entrance wide enough for it
        var flowers = BuildingArt.StyleOf(Sample(WorldModels.Of("t3_s01")!, 5, 3, 4.8f), Architecture.Timber);
        Assert.Equal((houses[Architecture.Cottage].Wall, SignKind.Text, "FLOWERS", false), (flowers.Wall, flowers.Sign, flowers.SignText, flowers.Home));
        Assert.True(flowers.Portal >= Pix.TextWidth("FLOWERS", 2) + 8);

        // A wing is plain
        var wing = BuildingArt.StyleOf(Sample(WorldModels.Of("c3_s01")!, annex: true), Architecture.Timber);
        Assert.Equal((1, SignKind.None, (string?)null, 0), (wing.Storeys, wing.Sign, wing.SignText, wing.Portal));
    }

    private static (int Opaque, int Colours, int Alone) Cleanliness(PixelCanvas c)
    {
        int isolated = 0, opaque = 0;
        var colours = new HashSet<(byte, byte, byte)>();
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
            {
                if (!c.IsOpaque(x, y)) continue;
                opaque++;
                var col = c.Get(x, y);
                colours.Add((col.R, col.G, col.B));
                bool alone = true;
                for (int dy = -1; dy <= 1 && alone; dy++)
                    for (int dx = -1; dx <= 1 && alone; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        var n = c.Get(x + dx, y + dy);
                        if (c.IsOpaque(x + dx, y + dy) && n.R == col.R && n.G == col.G && n.B == col.B) alone = false;
                    }
                if (alone) isolated++;
            }
        return (opaque, colours.Count, isolated);
    }

    private static void AssertClean(ArtSheet sheet, string what)
    {
        foreach (var (key, art) in sheet.Faces)
        {
            var (opaque, colours, alone) = Cleanliness(art);
            Assert.True(alone <= Math.Max(6, opaque / 50), $"{what} {key}: {alone} of its {opaque} texels stand alone");
            Assert.True(colours <= 96, $"{what} {key}: {colours} colours");
            // Nothing is outlined or shaded in near-black
            for (int y = 0; y < art.Height; y++)
                for (int x = 0; x < art.Width; x++)
                {
                    var col = art.Get(x, y);
                    Assert.False(col.A > 0 && col.R < 24 && col.G < 24 && col.B < 24, $"{what} {key}: near-black at ({x},{y})");
                }
        }
    }

    /// <summary>
    /// Every building in the catalogue, in each of the shapes the world gives it (a door in the wall, a closed
    /// porch, an open one, a wing), builds without a GPU and paints clean pixel art.
    /// </summary>
    [Fact]
    public void EveryBuildingOfTheCatalogueBuildsAndPaintsCleanly()
    {
        var typical = new Dictionary<BuildingKind, (int W, int D, float H)>
        {
            [BuildingKind.Tower] = (4, 3, 13.9f), [BuildingKind.Lighthouse] = (6, 5, 12.7f), [BuildingKind.League] = (11, 5, 16.4f),
            [BuildingKind.Galactic] = (13, 9, 13.9f), [BuildingKind.Apartments] = (5, 4, 8.1f), [BuildingKind.Temple] = (10, 5, 5.9f),
            [BuildingKind.Factory] = (7, 7, 7.1f), [BuildingKind.Gate] = (6, 6, 3.9f), [BuildingKind.Shrine] = (3, 3, 4.9f)
        };
        int built = 0;
        foreach (var model in WorldModels.All.Where(m => m.Role == ModelRole.Building))
        {
            var (w, d, h) = typical.GetValueOrDefault(model.Kind, (5, 4, 4.8f));
            foreach (string shape in new[] { "door", "closed porch", "open porch", "wing", "side doors" })
            {
                var b = Sample(model, w, d, h, annex: shape == "wing", theme: PokemonType.Electric);
                if (shape == "closed porch") b.Porch.AddRange(new[] { b.X0 + w / 2 - 1, b.X0 + w / 2, b.X0 + w / 2 + 1 });
                if (shape == "open porch")
                {
                    b.Porch.AddRange(new[] { b.X0 + w / 2 - 1, b.X0 + w / 2 + 1 });
                    b.PorchIsOpen = true;
                }
                if (shape == "side doors")
                {
                    b.Doors.Clear();
                    b.SideDoors.Add(b.Y0 + d / 2);
                }

                var style = BuildingArt.StyleOf(b, Architecture.Timber);
                var kit = new KitBuilder(new ArtSheet(), OutdoorVS);
                var roofs = new Dictionary<Color, MeshBuilder>();
                var targets = new BuildingTargets
                {
                    RoofTiles = color => roofs.TryGetValue(color, out var roof) ? roof : roofs[color] = new MeshBuilder(),
                    PublicLight = new MeshBuilder(), HomeLight = new MeshBuilder()
                };
                BuildingModels.Add(kit, b, style, targets);
                string what = $"{model.Name} ({shape})";

                Assert.True(kit.Solid.VertexCount > 0, $"{what} has no walls");
                AssertClean(kit.Sheet, what);
                var (min, max) = kit.Solid.Bounds();
                // It stands on the ground, within its tiles and a porch's depth, and its walls rise as high as its style says
                Assert.InRange(min.Y, -0.01f, 0.01f);
                Assert.True(min.X >= b.X0 - 0.6f && max.X <= b.X1 + 1.6f, $"{what} reaches from {min.X} to {max.X}");
                Assert.True(max.Y >= style.WallHeight * KitBuilder.Texel * OutdoorVS - 0.2f, $"{what} is only {max.Y} tall");
                if (model.Kind is BuildingKind.Tower or BuildingKind.Lighthouse && shape != "wing")
                    Assert.True(max.Y > 7f, $"{what} stands only {max.Y} tiles tall");

                // A roof that isn't flat is tiled in the style's own colour
                if (style.Pitched) Assert.Contains(style.RoofColor, roofs.Keys);
                built++;
            }

            // The name over the door is painted, and glows after dark
            if (model.Sign != null)
            {
                var b = Sample(model, w, d, h);
                var style = BuildingArt.StyleOf(b, Architecture.Timber);
                Assert.Equal((SignKind.Text, model.Sign), (style.Sign, style.SignText));
                var kit = new KitBuilder(new ArtSheet(), OutdoorVS);
                BuildingModels.Add(kit, b, style, new BuildingTargets { RoofTiles = _ => new MeshBuilder(), PublicLight = new MeshBuilder(), HomeLight = new MeshBuilder() });
                int lit = 0;
                foreach (var (key, art) in kit.Sheet.Faces)
                {
                    if (!key.EndsWith(".portal") && !key.EndsWith(".front")) continue;
                    for (int y = 0; y < Math.Min(art.Height, 26); y++)
                        for (int x = 0; x < art.Width; x++)
                            if (art.Get(x, y) is { A: ArtSheet.PublicLight, R: 255, G: 255, B: 255 }) lit++;
                }
                Assert.True(lit >= model.Sign.Count(char.IsLetter) * 9, $"{model.Name}: its sign {model.Sign} shows only {lit} lit texels");
            }
        }
        Assert.True(built >= 80 * 5);
    }

    [Fact]
    public void SnowAndSheetMetalAreRoofsOfTheirOwn()
    {
        var snow = BuildingArt.RoofTiles(BuildingArt.SnowRoof);
        var metal = BuildingArt.RoofTiles(BuildingArt.MetalRoof);
        var tiles = BuildingArt.RoofTiles(new Color(214, 82, 66, 255));
        Assert.Equal((64, 64), (snow.Width, snow.Height));
        Assert.Equal(BuildingArt.SnowRoof, snow.Get(10, 20));
        Assert.True(Cleanliness(snow).Colours <= 3);
        Assert.True(Cleanliness(metal).Colours <= 4);
        Assert.Equal(0, Cleanliness(snow).Alone);
        // Ribs run down a metal roof: a column is one shade all the way but for the sheets' joints
        Assert.Equal(metal.Get(3, 0), metal.Get(3, 20));
        Assert.NotEqual(metal.Get(0, 5), metal.Get(7, 5));
        Assert.NotEqual(tiles.Get(3, 3), snow.Get(3, 3));
    }

    // ------------------------------------------------------------------ props and ground

    [Fact]
    public void EveryPropOfTheCatalogueBuildsAndPaintsCleanly()
    {
        var types = WorldModels.All.Where(m => m.Role == ModelRole.Scenery).Select(m => m.Prop).Distinct().ToList();
        Assert.True(types.Count >= 14);
        foreach (var model in WorldModels.All.Where(m => m.Role == ModelRole.Scenery))
        {
            foreach (var (w, d) in new[] { (1, 2), (2, 2), (3, 3), (6, 9), (9, 5) })
            {
                var map = new Map(16, 16) { Name = "Props", Architecture = Architecture.Snow };
                map.Props.Add(new Prop { Type = model.Prop, X = 3, Y = 3, Width = w, Depth = d, Height = 8f, Model = model.Name });
                var kit = new KitBuilder(new ArtSheet(), OutdoorVS);
                var light = new MeshBuilder();
                OutdoorProps.Add(kit, map, light);
                string what = $"{model.Name} as a {model.Prop} of {w} by {d}";
                Assert.True(kit.Solid.VertexCount > 0, $"{what} draws nothing");
                AssertClean(kit.Sheet, what);
                var (min, max) = kit.Solid.Bounds();
                Assert.True(min.Y > -0.2f && max.Y > 0.3f, $"{what} lies between {min.Y} and {max.Y}");
                Assert.True(min.X > 3 - 1.6f && max.X < 3 + w + 1.6f, $"{what} reaches from {min.X} to {max.X}, far outside its tiles");
            }
        }

        // A honey tree is not the forest's green; a turbine and a mast stand as tall as their models
        var honey = new PixelCanvas(76, 96);
        Landmarks.PaintHoneyTree(honey);
        Assert.Contains(Enumerable.Range(0, 76 * 96), i => honey.Get(i % 76, i / 76) is { R: 236, G: 176, B: 64 });
        static float TopOf(PropType type, float height)
        {
            var map = new Map(16, 16) { Name = "Tall" };
            map.Props.Add(new Prop { Type = type, X = 3, Y = 3, Width = 3, Depth = 3, Height = height, Model = "x" });
            var kit = new KitBuilder(new ArtSheet(), OutdoorVS);
            OutdoorProps.Add(kit, map, new MeshBuilder());
            return kit.Solid.Bounds().Max.Y;
        }
        Assert.True(TopOf(PropType.WindTurbine, 8.3f) > 8f);
        Assert.True(TopOf(PropType.Mast, 10.3f) > TopOf(PropType.Mast, 6f) + 2f);
        Assert.True(TopOf(PropType.Column, 6.6f) > TopOf(PropType.Column, 4.1f) + 1.5f);
    }

    [Fact]
    public void FencesFollowTheTownAndLowWallsJoinUp()
    {
        Assert.Equal(OutdoorProps.FenceKind.Wood, OutdoorProps.FenceOf(Architecture.Timber));
        Assert.Equal(OutdoorProps.FenceKind.White, OutdoorProps.FenceOf(Architecture.Cottage));
        Assert.Equal(OutdoorProps.FenceKind.Iron, OutdoorProps.FenceOf(Architecture.City));
        Assert.Equal(OutdoorProps.FenceKind.Iron, OutdoorProps.FenceOf(Architecture.Harbour));

        static ArtSheet Fenced(Architecture town, PropType type)
        {
            var map = new Map(8, 8) { Name = "Fences", Architecture = town };
            map.AddProp(type, 2, 2, 3, 1);
            var kit = new KitBuilder(new ArtSheet(), OutdoorVS);
            OutdoorProps.Add(kit, map, new MeshBuilder());
            Assert.True(kit.Solid.VertexCount > 0);
            return kit.Sheet;
        }
        Assert.Contains(Fenced(Architecture.City, PropType.Fence).Faces, f => f.Key.StartsWith("fence.iron"));
        Assert.Contains(Fenced(Architecture.Cottage, PropType.Fence).Faces, f => f.Key.StartsWith("fence.white"));
        var wall = Fenced(Architecture.Timber, PropType.LowWall);
        Assert.Contains(wall.Faces, f => f.Key.StartsWith("lowwall.pier"));
        Assert.Contains(wall.Faces, f => f.Key.StartsWith("lowwall.arm"));
        Assert.DoesNotContain(wall.Faces, f => f.Key.StartsWith("fence."));
        AssertClean(wall, "a low wall");
    }

    [Fact]
    public void PavingIsSlabsWithAKerbWhereItEnds()
    {
        var joint = new Color(168, 172, 188, 255);
        var kerb = new Color(150, 154, 172, 255);
        var open = new PixelCanvas(32, 32);
        PixelGround.PaintPaving(open, 0, 0, 5, 7, west: true, east: true, north: true, south: true);
        // Four slabs: a joint down the east and along the south of each
        for (int i = 0; i < 32; i++)
        {
            Assert.Equal(joint, open.Get(15, i));
            Assert.Equal(joint, open.Get(31, i));
            Assert.Equal(joint, open.Get(i, 15));
        }
        Assert.NotEqual(joint, open.Get(7, 7));
        Assert.True(Cleanliness(open).Colours <= 3);
        Assert.Equal(0, Cleanliness(open).Alone);

        // The same tile where the paving ends to its west: a kerb two texels wide with a light line inside it
        var edge = new PixelCanvas(32, 32);
        PixelGround.PaintPaving(edge, 0, 0, 5, 7, west: false, east: true, north: true, south: true);
        for (int y = 0; y < 32; y++)
        {
            Assert.Equal(kerb, edge.Get(0, y));
            Assert.Equal(kerb, edge.Get(1, y));
            Assert.Equal(new Color(226, 228, 234, 255), edge.Get(2, y));
        }
        Assert.Equal(open.Get(20, 20), edge.Get(20, 20));

        // A walkway's deck carries a solar panel
        var deck = new PixelCanvas(32, 32);
        PixelGround.PaintWalkway(deck, 0, 0, west: true, east: true, north: true, south: false);
        Assert.Equal(new Color(52, 96, 170, 255), deck.Get(16, 14));
        Assert.Equal(new Color(124, 132, 150, 255), deck.Get(10, 31));

        // On a map, paving is paving, a lamp stands where the import says, and the ground at its foot is the street's
        Assert.Equal((TileType.Paving, false, null), WorldMapBuilder.Look(TerrainCover.Paving, TileBehavior.None, false));
        Assert.Equal((true, (PropType?)PropType.LampPost), (WorldMapBuilder.Look(TerrainCover.Lamp, TileBehavior.None, true).Solid, WorldMapBuilder.Look(TerrainCover.Lamp, TileBehavior.None, true).Prop));
        Assert.Equal(TileType.Tree, WorldMapBuilder.Look(TerrainCover.Broadleaf, TileBehavior.None, true).Type);
        Assert.Equal(TileType.Paving, TileCodes.Parse(TileCodes.CodeOf(TileType.Paving), "a map"));
        Assert.Equal(TileType.Walkway, TileCodes.Parse(TileCodes.CodeOf(TileType.Walkway), "a map"));
    }
}
