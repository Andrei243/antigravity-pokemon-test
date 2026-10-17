using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapImporter.Gba;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The GBA reader (plan 18 · W6), on a small decompilation built here file by file and byte by byte: two maps joined
/// by a connection, a layout's <c>map.bin</c>, a tileset's attributes, the behaviours' and the labels' headers. No test
/// needs the checkout.
/// </summary>
public sealed class GbaImportTests : IDisposable
{
    private readonly string dir = Path.Combine(Path.GetTempPath(), "gba-import-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
    }

    private static readonly GbaGame TestGame = new("test", "kanto", "Kanto", 4, "MAP_TEST_TOWN");

    // The tileset's metatiles: 0 plain ground (labelled lawn), 1 tall grass, 2 the sea, 3 a ledge hopped north,
    // 4 ground closed to the north, 5 an arrow warp south, 6 a behaviour the table leaves to the art
    private static readonly int[] Behaviours = { 0x00, 0x02, 0x15, 0x3A, 0x32, 0x65, 0x84 };

    private const int Normal = 0, Grass = 1, Sea = 2, LedgeN = 3, ClosedN = 4, ArrowS = 5, Sign = 6;

    private static ushort Block(int metatile, int elevation, bool blocked = false) =>
        (ushort)(metatile | (blocked ? 1 << 10 : 0) | elevation << 12);

    private void Put(string path, string text)
    {
        string full = Path.Combine(dir, path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, text);
    }

    private void Put(string path, byte[] bytes)
    {
        string full = Path.Combine(dir, path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllBytes(full, bytes);
    }

    private static byte[] Blocks(params ushort[] blocks)
    {
        var bytes = new byte[blocks.Length * 2];
        for (int i = 0; i < blocks.Length; i++) BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(i * 2), blocks[i]);
        return bytes;
    }

    private static string MapJson(string id, string name, string layout, string type, string connection) => $$"""
        {
          "id": "{{id}}", "name": "{{name}}", "layout": "{{layout}}", "music": "MUS_TEST",
          "region_map_section": "MAPSEC_{{id[4..]}}", "weather": "WEATHER_SUNNY", "map_type": "{{type}}",
          "allow_cycling": true, "allow_escaping": false, "allow_running": true,
          "connections": [ {{connection}} ],
          "object_events": [], "warp_events": [], "coord_events": [], "bg_events": []
        }
        """;

    /// <summary>
    /// A town of 4 by 3 and a route of 4 by 2 joined up from it at an offset of 2, so the route lies over the town's
    /// east half. The town's rows: a level step (3, 4); a ramp between levels (3, 0, 4) and water off a shore; the far
    /// side of the ramp and more water. The route's: a bridge (4, 15, 4) over water between two banks of level 3.
    /// </summary>
    private GbaMaps Build()
    {
        Put("include/constants/metatile_behaviors.h", """
            #define MB_NORMAL 0x00
            #define MB_TALL_GRASS 0x02
            #define MB_OCEAN_WATER 0x15
            #define MB_IMPASSABLE_NORTH 0x32
            #define MB_JUMP_NORTH 0x3A
            #define MB_SOUTH_ARROW_WARP 0x65
            #define MB_SIGNPOST 0x84
            #define NUM_METATILE_BEHAVIORS 0xF0
            """);
        Put("include/constants/metatile_labels.h", "#define METATILE_General_Plain_Grass 0x000\n");
        Put("include/fieldmap.h", "#define NUM_METATILES_IN_PRIMARY 640\n");
        Put("src/data/tilesets/headers.h", "const struct Tileset gTileset_General =\n{\n    .metatileAttributes = gMetatileAttributes_General,\n};\n");
        Put("src/data/tilesets/metatiles.h", "const u32 gMetatileAttributes_General[] = INCBIN_U32(\"data/tilesets/primary/general/metatile_attributes.bin\");\n");
        // FireRed's four bytes a metatile: the behaviour in the low nine bits, other fields above it
        var attributes = new byte[Behaviours.Length * 4];
        for (int i = 0; i < Behaviours.Length; i++) BinaryPrimitives.WriteUInt32LittleEndian(attributes.AsSpan(i * 4), (uint)(Behaviours[i] | 0x2000_0000));
        Put("data/tilesets/primary/general/metatile_attributes.bin", attributes);

        Put("data/layouts/layouts.json", """
            { "layouts_table_label": "gMapLayouts", "layouts": [
              { "id": "LAYOUT_TEST_TOWN", "name": "TestTown_Layout", "width": 4, "height": 3, "primary_tileset": "gTileset_General",
                "secondary_tileset": "gTileset_None", "blockdata_filepath": "data/layouts/TestTown/map.bin" },
              {},
              { "id": "LAYOUT_TEST_ROUTE", "name": "TestRoute_Layout", "width": 4, "height": 2, "primary_tileset": "gTileset_General",
                "secondary_tileset": "gTileset_None", "blockdata_filepath": "data/layouts/TestRoute/map.bin" } ] }
            """);
        Put("data/layouts/TestTown/map.bin", Blocks(
            Block(Normal, 3), Block(Normal, 4), Block(LedgeN, 4), Block(ClosedN, 4),
            Block(Grass, 3), Block(Normal, 0), Block(Normal, 4), Block(Sea, 1),
            Block(Sign, 3, blocked: true), Block(Normal, 4), Block(ArrowS, 4), Block(Sea, 1)));
        Put("data/layouts/TestRoute/map.bin", Blocks(
            Block(Normal, 4), Block(Normal, 15), Block(Normal, 4), Block(Normal, 4),
            Block(Normal, 3), Block(Sea, 1), Block(Normal, 3), Block(Normal, 3)));

        Put("data/maps/map_groups.json", """{ "group_order": ["gMapGroup_TownsAndRoutes"], "gMapGroup_TownsAndRoutes": ["TestTown", "TestRoute"] }""");
        Put("data/maps/TestTown/map.json", MapJson("MAP_TEST_TOWN", "TestTown", "LAYOUT_TEST_TOWN", "MAP_TYPE_TOWN",
            """{ "map": "MAP_TEST_ROUTE", "offset": 2, "direction": "up" }"""));
        Put("data/maps/TestRoute/map.json", MapJson("MAP_TEST_ROUTE", "TestRoute", "LAYOUT_TEST_ROUTE", "MAP_TYPE_ROUTE",
            """{ "map": "MAP_TEST_TOWN", "offset": -2, "direction": "down" }"""));
        return new GbaMaps(dir, TestGame);
    }

    [Fact]
    public void TheMapsLayoutsAndBehavioursAreRead()
    {
        var maps = Build();
        Assert.Equal(new[] { "TestTown", "TestRoute" }, maps.Maps.Select(m => m.Name));
        Assert.Equal(2, maps.Layouts.Count);   // the table's empty place is passed over
        Assert.Equal(640, maps.PrimaryCount);
        Assert.Equal("MB_JUMP_NORTH", maps.BehaviourNames[0x3A]);
        Assert.Equal("Plain_Grass", maps.Labels[("gTileset_General", 0)]);

        var town = maps.LayoutOf(maps.ById["MAP_TEST_TOWN"]);
        ushort sign = town.Block(0, 2);
        Assert.Equal(Sign, GbaLayout.MetatileOf(sign));
        Assert.Equal(1, GbaLayout.CollisionOf(sign));
        Assert.Equal(3, GbaLayout.ElevationOf(sign));
        Assert.Equal(0x3A, maps.BehaviourOf(town, LedgeN));
        Assert.Equal(new GbaConnection("MAP_TEST_ROUTE", 2, "up"), maps.ById["MAP_TEST_TOWN"].Connections.Single());
        Assert.Equal("test_town", maps.ById["MAP_TEST_TOWN"].Key);
    }

    [Fact]
    public void EmeraldsBehavioursAreAnEnumCountedFromNought()
    {
        var names = GbaMaps.ParseBehaviours("enum {\n    MB_NORMAL,\n    MB_SECRET_BASE_WALL, // a comment\n    MB_TALL_GRASS,\n    MB_ICE = 0x20,\n    MB_SAND,\n    NUM_METATILE_BEHAVIORS\n};\n#define MB_INVALID UCHAR_MAX");
        Assert.Equal("MB_TALL_GRASS", names[2]);
        Assert.Equal("MB_ICE", names[0x20]);
        Assert.Equal("MB_SAND", names[0x21]);
        Assert.DoesNotContain("NUM_METATILE_BEHAVIORS", names.Values);
    }

    [Theory]
    [InlineData("PalletTown", "pallet_town")]
    [InlineData("Route1", "route_1")]
    [InlineData("Route21_North", "route_21_north")]
    [InlineData("SSAnne_1F_Corridor", "ss_anne_1f_corridor")]
    public void AMapsKeyIsItsNameInWords(string name, string key) => Assert.Equal(key, GbaMaps.KeyOf(name));

    [Fact]
    public void TheBehavioursAndTheCollisionBecomeTheWorldsOwn()
    {
        var world = new GbaWorld(Build());
        var m = world.Matrices.Single();
        var (town, route) = (Placed(m, "TestTown"), Placed(m, "TestRoute"));
        TileBehavior At(GbaPlaced p, int x, int y) => (TileBehavior)m.Behaviour[(p.Y + y) * m.Width + p.X + x];
        bool Solid(GbaPlaced p, int x, int y) => m.Solid[(p.Y + y) * m.Width + p.X + x];

        Assert.Equal(TileBehavior.LedgeNorth, At(town, 2, 0));
        Assert.Equal(TileBehavior.BlockNorth, At(town, 3, 0));
        Assert.Equal(TileBehavior.TallGrass, At(town, 0, 1));
        Assert.Equal(TileBehavior.Sea, At(town, 3, 1));
        Assert.Equal(TileBehavior.ExitSouth, At(town, 2, 2));
        Assert.Equal(TileBehavior.None, At(town, 0, 2));   // a signpost is the event's business
        Assert.True(Solid(town, 0, 2));
        Assert.False(Solid(town, 1, 2));
        Assert.Equal(TileBehavior.None, At(route, 1, 0));

        // The look: the behaviour where it shows, the label where there is one, and Unknown where neither says
        Assert.Equal(TerrainCover.TallGrass, m.Cover[(town.Y + 1) * m.Width + town.X]);
        Assert.Equal(TerrainCover.Water, m.Cover[(town.Y + 1) * m.Width + town.X + 3]);
        Assert.Equal(TerrainCover.Grass, m.Cover[town.Y * m.Width + town.X]);
        Assert.Equal(TerrainCover.Unknown, m.Cover[(town.Y + 2) * m.Width + town.X]);
        Assert.Equal(12, world.BehaviourUse["MB_NORMAL"]);   // five of the town's tiles and seven of the route's
    }

    [Fact]
    public void AConnectionPlacesTheMapAtItsOffset()
    {
        var world = new GbaWorld(Build());
        var m = world.Matrices.Single();
        Assert.Empty(world.Problems);
        // The route is two rows above the town and two tiles east: the town is moved down so the grid starts at nought
        Assert.Equal((0, 2), (Placed(m, "TestTown").X, Placed(m, "TestTown").Y));
        Assert.Equal((2, 0), (Placed(m, "TestRoute").X, Placed(m, "TestRoute").Y));
        Assert.Equal((32, 32), (m.Width, m.Height));
        Assert.False(m.Has(0, 0));   // nothing lies west of the route above the town
        Assert.True(m.Solid[0]);
    }

    [Fact]
    public void TwoConnectionsThatDisagreeAreReportedAndTheFirstPlacedWins()
    {
        Build();
        Put("data/maps/TestRoute/map.json", MapJson("MAP_TEST_ROUTE", "TestRoute", "LAYOUT_TEST_ROUTE", "MAP_TYPE_ROUTE",
            """{ "map": "MAP_TEST_TOWN", "offset": -1, "direction": "down" }"""));
        var world = new GbaWorld(new GbaMaps(dir, TestGame));
        Assert.Contains(world.Problems, p => p.Contains("TestRoute puts TestTown down", StringComparison.Ordinal));
        Assert.Equal((2, 0), (Placed(world.Matrices[0], "TestRoute").X, Placed(world.Matrices[0], "TestRoute").Y));
    }

    [Fact]
    public void TheElevationsBecomePlatesOfHeight()
    {
        var world = new GbaWorld(Build());
        var m = world.Matrices.Single();
        var (town, route) = (Placed(m, "TestTown"), Placed(m, "TestRoute"));
        int I(GbaPlaced p, int x, int y) => (p.Y + y) * m.Width + p.X + x;

        // A level is 1.4 tiles above elevation 3, which is the world's usual ground: a straight step is a cliff
        Assert.Equal(1f, m.Ground[I(town, 0, 0)]);
        Assert.Equal(2.4f, m.Ground[I(town, 1, 0)], 3);
        Assert.True(m.Ground[I(town, 1, 0)] - m.Ground[I(town, 0, 0)] >= FieldMovement.StepLimit);

        // Elevation 0 between levels 3 and 4 is a ramp, its middle 0.7 from either side: under the weld's limit
        int ramp = I(town, 1, 1);
        Assert.Equal(1.4f, m.SlopeX[ramp], 3);
        Assert.Equal(0f, m.SlopeZ[ramp]);
        float middle = m.Ground[ramp] + m.SlopeX[ramp] * 0.5f;
        Assert.Equal(1.7f, middle, 3);
        Assert.True(middle - m.Ground[I(town, 0, 1)] < Relief.WeldLimit);
        Assert.True(m.Ground[I(town, 2, 1)] - middle < Relief.WeldLimit);

        // The water's elevation 1 is the level of the land beside it: a shore is a level step
        Assert.Equal(m.Ground[I(town, 2, 1)], m.Ground[I(town, 3, 1)]);
        Assert.Equal(m.Ground[I(route, 0, 1)], m.Ground[I(route, 1, 1)]);

        // Elevation 15 is a deck at its ends' level over the ground of the water it crosses
        int bridge = I(route, 1, 0);
        Assert.Equal(2.4f, m.Deck[bridge], 3);
        Assert.Equal(1f, m.Ground[bridge], 3);
        Assert.True(float.IsNaN(m.Deck[I(route, 0, 0)]));

        // And the chunk keeps them as plates the game's builder reads: a deck plate over the ground's
        var chunk = GbaWorld.Chunk(m, 0, 0, 0);
        Assert.Equal(1f, chunk.HeightAt(route.X + 1.5f, route.Y + 0.5f, from: 0f)!.Value, 3);
        Assert.Equal(2.4f, chunk.HeightAt(route.X + 1.5f, route.Y + 0.5f, from: 3f)!.Value, 3);
        Assert.Equal(1.7f, chunk.HeightAt(town.X + 1.5f, town.Y + 1.5f)!.Value, 3);
    }

    [Fact]
    public void AChunkOfTwoMapsSaysWhichTilesAreTheOthers()
    {
        var world = new GbaWorld(Build());
        var m = world.Matrices.Single();
        var chunk = GbaWorld.Chunk(m, 0, 0, 0);
        chunk.Validate();
        // The town has more of the chunk's tiles than the route: the cell is the town's, the route's tiles say so
        Assert.Equal(0, GbaWorld.Owner(m, 0, 0));
        Assert.NotNull(chunk.Areas);
        Assert.Equal(1, chunk.AreaIndexAt(2, 0));
        Assert.Null(chunk.AreaIndexAt(0, 2));
        Assert.Null(chunk.AreaIndexAt(20, 20));   // nobody's tile: the cell's
        var file = GbaWorld.MatrixFile(m, (_, _) => 0);
        Assert.Equal(new[] { "test_town", "test_route" }, file.AreaKeys);
        Assert.Equal("test_town", file.AreaAt(0, 0));
    }

    [Fact]
    public void TheBuilderGivesEachTileItsOwnArea()
    {
        var world = new GbaWorld(Build());
        string folder = Path.Combine(dir, "out", "world");
        GbaImport.WriteWorld(world, new GbaTables(), folder);
        var index = GameDataFiles.Load<WorldIndexFile>(Path.Combine(folder, World.IndexFile));
        var map = WorldMapBuilder.Build(World.Open(folder, index), index.Maps.Single());

        Assert.Equal("Kanto", map.Name);
        Assert.Equal("test_town", map.AreaAt(1, 3)!.Key);
        Assert.Equal("test_route", map.AreaAt(2, 0)!.Key);
        Assert.Equal("test_route", map.AreaAt(5, 1)!.Key);
        Assert.Equal("test_town", map.AreaAt(20, 20)!.Key);   // past every map the cell's own area
        Assert.Equal("Test Route", map.DisplayNameAt(3, 0));
        Assert.Equal(TileBehavior.LedgeNorth, map.BehaviourAt(2, 2));
        Assert.Equal(2.4f, map.HeightAt(1, 2), 3);
        Assert.Equal((0, 0, 32, 32), map.AreaBounds("test_town"));
        Assert.Equal((2, 0, 4, 2), map.AreaBounds("test_route"));
    }

    private static GbaPlaced Placed(GbaMatrix m, string name) => m.Maps.Single(p => p.Map.Name == name);

    // ---------------------------------------------------------------- the table of behaviours

    [Fact]
    public void EveryValueTheTableMakesIsNamedAndAccountedFor()
    {
        foreach (var (name, behaviour) in GbaBehaviours.Table)
        {
            Assert.True(TileBehaviors.IsKnown((byte)behaviour), $"{name} becomes 0x{(int)behaviour:X2}, which TileBehavior doesn't name");
            Assert.EndsWith(".", TileBehaviors.Meaning(behaviour));
            Assert.False(string.IsNullOrWhiteSpace(TileBehaviors.InGame(behaviour).Note));
        }
        // The GBA's own values lie among those Platinum leaves unused at the top of its byte
        foreach (var behaviour in Enum.GetValues<TileBehavior>().Where(b => (int)b > (int)TileBehavior.ShopShelf))
            Assert.InRange((int)behaviour, 0xED, 0xFE);
        Assert.Equal(0x3A, (int)GbaBehaviours.Of("MB_JUMP_NORTH"));
        Assert.Equal(TileBehavior.None, GbaBehaviours.Of("MB_SIGNPOST", out bool known));
        Assert.False(known);
    }

    [Fact]
    public void TheDocsSectionIsWhatTheImporterWrites()
    {
        string doc = File.ReadAllText(Path.Combine(Repository.Root, "docs", "tile-behaviours.md"));
        string section = GbaImport.SectionOf(doc);
        Assert.StartsWith(GbaImport.SectionTitle, section);
        foreach (var behaviour in GbaBehaviours.Table.Values.Distinct().Where(b => (int)b >= 0xED))
            Assert.Contains($"| `0x{(int)behaviour:X2}` | `{behaviour}` | {TileBehaviors.Meaning(behaviour)} |", section);
        // The Sinnoh run keeps the section where it is: splicing it back changes nothing
        Assert.Equal(doc.ReplaceLineEndings("\n").TrimEnd(), GbaImport.Splice(doc, section).ReplaceLineEndings("\n").TrimEnd());
    }
}
