using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using MapImporter;
using MapImporter.Bw2;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// The map importer (plan 01 · M1) and the world files it writes. The readers are tested on small files built
/// here byte by byte, in the layout the game's own loader reads, so no test needs the decompilation.
/// </summary>
public class MapImportTests
{
    // ---------------------------------------------------------------- building test files

    private static byte[] Le(params int[] values)
    {
        var bytes = new byte[values.Length * 4];
        for (int i = 0; i < values.Length; i++) BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(i * 4), values[i]);
        return bytes;
    }

    private static byte[] Le16(params int[] values)
    {
        var bytes = new byte[values.Length * 2];
        for (int i = 0; i < values.Length; i++) BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(i * 2), (ushort)values[i]);
        return bytes;
    }

    private static int Fx(float value) => (int)MathF.Round(value * 4096f);

    private static byte[] Join(params byte[][] parts) => parts.SelectMany(p => p).ToArray();

    /// <summary>Height data: every plate is two corners, a normal and a constant, looked up by index.</summary>
    private static byte[] Bdhc(params (float X0, float Z0, float X1, float Z1, Vector3 Normal, float Constant)[] plates)
    {
        var points = plates.SelectMany(p => Le(Fx(p.X0), Fx(p.Z0), Fx(p.X1), Fx(p.Z1))).ToArray();
        var normals = plates.SelectMany(p => Le(Fx(p.Normal.X), Fx(p.Normal.Y), Fx(p.Normal.Z))).ToArray();
        var constants = plates.SelectMany(p => Le(Fx(p.Constant))).ToArray();
        var records = plates.SelectMany((_, i) => Le16(i * 2, i * 2 + 1, i, i)).ToArray();
        // One strip listing every plate: the importer ignores it, the counts must still add up
        var strips = Join(Le(Fx(256)), Le16(plates.Length, 0));
        var access = Le16(Enumerable.Range(0, plates.Length).ToArray());
        return Join(Encoding.ASCII.GetBytes("BDHC"), Le16(plates.Length * 2, plates.Length, plates.Length, plates.Length, 1, plates.Length),
            points, normals, constants, records, strips, access);
    }

    private static readonly Vector3 Up = new(0, 1, 0);

    private static byte[] Land(ushort[] attributes, byte[] props, byte[] heights, byte[]? model = null)
    {
        model ??= Array.Empty<byte>();
        return Join(Le(attributes.Length * 2, props.Length, model.Length, heights.Length), Le16(attributes.Select(a => (int)a).ToArray()), props, model, heights);
    }

    private static byte[] Prop(int model, float x, float y, float z) =>
        Join(Le(model, Fx(x), Fx(y), Fx(z)), Le(0, 0, 0), Le(Fx(1), Fx(1), Fx(1)), Le(0, 0));

    // ---------------------------------------------------------------- chunks

    [Fact]
    public void AChunkIsBehavioursCollisionPropsAndHeights()
    {
        var attributes = new ushort[1024];
        attributes[0] = 0x8000;                      // a plain blocked tile in the north-west corner
        attributes[1] = 0x0002;                      // tall grass beside it
        attributes[5 * 32 + 7] = 0x8069;             // a door at (7, 5): blocked, as doors are
        attributes[31 * 32 + 31] = 0x0015;           // sea in the south-east corner

        var file = Land(attributes, Prop(4, -96, 0, 32), Bdhc((-256, -256, 256, 256, Up, 0)));
        var land = LandData.Parse(file);

        Assert.True(land.Solid(0, 0));
        Assert.Equal(0x00, land.Behaviour(0, 0));
        Assert.False(land.Solid(1, 0));
        Assert.Equal((byte)TileBehavior.TallGrass, land.Behaviour(1, 0));
        Assert.True(land.Solid(7, 5));
        Assert.Equal((byte)TileBehavior.Door, land.Behaviour(7, 5));
        Assert.Equal((byte)TileBehavior.Sea, land.Behaviour(31, 31));
        Assert.Equal(0, land.OtherBits(7, 5));

        // Props are measured from the chunk's centre in units of a sixteenth of a tile
        var prop = Assert.Single(land.Props);
        Assert.Equal(4, prop.ModelId);
        Assert.Equal(10f, prop.TileX);
        Assert.Equal(18f, prop.TileZ);
        Assert.Equal(Vector3.One, prop.Scale);

        Assert.Single(land.Heights.Plates);
        Assert.Equal("", land.ModelName);
    }

    [Fact]
    public void StrayBytesAfterTheLastPropAreDroppedAsTheGameDropsThem()
    {
        // map_data_506 carries 146 bytes of props: three props and two bytes over
        var props = Join(Prop(1, 0, 0, 0), Prop(2, 16, 0, 0), Prop(3, 32, 0, 0), new byte[] { 0xAB, 0xCD });
        var land = LandData.Parse(Land(new ushort[1024], props, Bdhc((-256, -256, 256, 256, Up, 0))));
        Assert.Equal(new[] { 1, 2, 3 }, land.Props.Select(p => p.ModelId));
    }

    [Fact]
    public void AChunkWhoseSectionsDoNotAddUpIsRefused()
    {
        var file = Land(new ushort[1024], Array.Empty<byte>(), Bdhc((-256, -256, 256, 256, Up, 0)));
        Assert.Throws<InvalidDataException>(() => LandData.Parse(file.AsSpan(0, file.Length - 1)));
        Assert.Throws<InvalidDataException>(() => LandData.Parse(Land(new ushort[512], Array.Empty<byte>(), Array.Empty<byte>())));
        Assert.Throws<InvalidDataException>(() => LandData.Parse(new byte[8]));
    }

    // ---------------------------------------------------------------- heights

    [Fact]
    public void AFlatPlateIsItsConstantAndASlopeFollowsItsNormal()
    {
        // Flat ground 32 units up: the plane y = 32 has normal (0, 1, 0) and constant -32
        // A ramp rising one unit per unit southward through y = 0 at z = 0: normal (0, 1, -1) / √2, constant 0
        float r = 1f / MathF.Sqrt(2f);
        var heights = HeightPlates.Parse(Bdhc((-256, -256, 0, 256, Up, -32), (0, 0, 256, 64, new Vector3(0, r, -r), 0)));

        Assert.True(heights.Plates[0].Flat);
        Assert.Equal(32f, heights.Plates[0].HeightAt(-100, 50), 2);
        Assert.False(heights.Plates[1].Flat);
        Assert.Equal(0f, heights.Plates[1].HeightAt(100, 0), 2);
        Assert.Equal(48f, heights.Plates[1].HeightAt(100, 48), 1);

        Assert.Equal(new[] { 32f }, heights.HeightsAt(-8, 8).Select(h => MathF.Round(h)));
        Assert.Empty(heights.HeightsAt(100, 200));
    }

    [Fact]
    public void WhereABridgeCrossesAPathTheWalkerKeepsTheirOwnLevel()
    {
        var heights = HeightPlates.Parse(Bdhc((-256, -256, 256, 256, Up, 0), (-64, -256, 64, 256, Up, -48)));

        Assert.Equal(new[] { 0f, 48f }, heights.HeightsAt(0, 0));
        Assert.Equal(0f, heights.HeightAt(0, 0, from: 2f));
        Assert.Equal(48f, heights.HeightAt(0, 0, from: 40f));
        Assert.Equal(0f, heights.HeightAt(200, 0, from: 40f));   // beside the bridge only the ground is left
        Assert.Null(heights.HeightAt(999, 0, from: 0f));
    }

    [Fact]
    public void HeightDataMustBeWhatItSaysItIs()
    {
        Assert.Throws<InvalidDataException>(() => HeightPlates.Parse(Encoding.ASCII.GetBytes("NOPE000000000000")));
        var data = Bdhc((-256, -256, 256, 256, Up, 0));
        Assert.Throws<InvalidDataException>(() => HeightPlates.Parse(data.AsSpan(0, data.Length - 4)));
    }

    [Fact]
    public void PlatesAreWrittenInTilesFromTheChunksCorner()
    {
        float r = 1f / MathF.Sqrt(2f);
        var heights = HeightPlates.Parse(Bdhc((-256, -256, 256, 256, Up, -16), (0, 0, 64, 32, new Vector3(0, r, -r), 0)));

        var flat = WorldWriter.ToPlate(heights.Plates[0]);
        Assert.Equal((0f, 0f, 32f, 32f, 1f, 0f, 0f), (flat.X, flat.Z, flat.Width, flat.Depth, flat.Height, flat.SlopeX, flat.SlopeZ));

        // The ramp: four tiles wide, two deep, level with the ground at its north edge, one tile up per tile south
        var ramp = WorldWriter.ToPlate(heights.Plates[1]);
        Assert.Equal((16f, 16f, 4f, 2f), (ramp.X, ramp.Z, ramp.Width, ramp.Depth));
        Assert.Equal(0f, ramp.Height, 3);
        Assert.Equal(1f, ramp.SlopeZ, 3);
        Assert.Equal(2f, ramp.HeightAt(17, 18), 3);
    }

    // ---------------------------------------------------------------- the terrain model's polygons

    private static byte[] DisplayList(params (int Command, uint[] Parameters)[] commands)
    {
        var bytes = new List<byte>();
        for (int i = 0; i < commands.Length; i += 4)
        {
            var group = commands.Skip(i).Take(4).ToList();
            bytes.AddRange(Enumerable.Range(0, 4).Select(c => (byte)(c < group.Count ? group[c].Command : 0)));
            foreach (var (_, parameters) in group)
                foreach (uint p in parameters) bytes.AddRange(BitConverter.GetBytes(p));
        }
        return bytes.ToArray();
    }

    private static uint Pack16(int low, int high) => (uint)((low & 0xFFFF) | ((high & 0xFFFF) << 16));
    private static uint Pack10(int x, int y, int z) => (uint)((x & 0x3FF) | ((y & 0x3FF) << 10) | ((z & 0x3FF) << 20));

    [Fact]
    public void VerticesAreReadInEveryFormTheModelsUse()
    {
        // One quad on the ground, its corners given four ways: all three coordinates, then keeping one from the
        // vertex before (x and z; x and y), then as a small step from the last
        var list = DisplayList(
            (0x40, new uint[] { 1 }),                                             // begin quads
            (0x23, new uint[] { Pack16(Fx(-1), 0), Pack16(Fx(-1), 0) }),          // (-1, 0, -1)
            (0x26, new uint[] { Pack16(Fx(1), Fx(-1)) }),                         // x and z: (1, 0, -1)
            (0x27, new uint[] { Pack16(0, Fx(1)) }),                              // y and z: (1, 0, 1)
            (0x24, new uint[] { Pack10(-64, 0, 64) }),                            // ten bits each, six fractional: (-1, 0, 1)
            (0x41, Array.Empty<uint>()));

        var polygons = TerrainModel.ReadDisplayList(list, posScale: 16f, "ngrass");

        Assert.Equal(2, polygons.Count);   // a quad is two triangles
        Assert.All(polygons, p => Assert.Equal("ngrass", p.Texture));
        Assert.All(polygons, p => Assert.True(p.Level));
        var corners = polygons.SelectMany(p => new[] { p.A, p.B, p.C }).Distinct().OrderBy(v => v.Z).ThenBy(v => v.X).ToList();
        Assert.Equal(new[] { new Vector3(-16, 0, -16), new Vector3(16, 0, -16), new Vector3(-16, 0, 16), new Vector3(16, 0, 16) }, corners);
    }

    [Fact]
    public void RelativeVerticesStepFromTheOneBeforeAndStripsShareEdges()
    {
        var list = DisplayList(
            (0x40, new uint[] { 2 }),                                             // begin a triangle strip
            (0x23, new uint[] { Pack16(0, 0), Pack16(0, 0) }),
            (0x28, new uint[] { Pack10(100, 0, 0) }),                             // 100/4096 east
            (0x28, new uint[] { Pack10(0, 0, 100) }),
            (0x28, new uint[] { Pack10(-100, 0, 0) }),
            (0x41, Array.Empty<uint>()));

        var polygons = TerrainModel.ReadDisplayList(list, posScale: 4096f);

        Assert.Equal(2, polygons.Count);   // four vertices of a strip make two triangles
        Assert.Equal(new Vector3(100, 0, 0), polygons[0].B);
        Assert.Equal(new Vector3(100, 0, 100), polygons[0].C);
        Assert.Contains(new Vector3(0, 0, 100), new[] { polygons[1].A, polygons[1].B, polygons[1].C });
    }

    [Fact]
    public void CommandsThatAreNotVerticesAreSteppedOverWithTheirParameters()
    {
        // A colour, a normal and a texture coordinate between the vertices must not shift what follows
        var list = DisplayList(
            (0x40, new uint[] { 0 }),
            (0x20, new uint[] { 0x7FFF }),
            (0x23, new uint[] { Pack16(0, 0), Pack16(0, 0) }),
            (0x21, new uint[] { 0x200 }),
            (0x22, new uint[] { 0x12345678 }),
            (0x23, new uint[] { Pack16(Fx(1), 0), Pack16(0, 0) }),
            (0x23, new uint[] { Pack16(0, 0), Pack16(Fx(1), 0) }),
            (0x41, Array.Empty<uint>()));

        var polygon = Assert.Single(TerrainModel.ReadDisplayList(list, posScale: 1f));
        Assert.Equal((Vector3.Zero, new Vector3(1, 0, 0), new Vector3(0, 0, 1)), (polygon.A, polygon.B, polygon.C));
    }

    private static TerrainModel.Polygon Flat(string texture, float x0, float z0, float x1, float z1, float y, bool second = false) => second
        ? new TerrainModel.Polygon(texture, "", new Vector3(x0, y, z0), new Vector3(x1, y, z1), new Vector3(x0, y, z1))
        : new TerrainModel.Polygon(texture, "", new Vector3(x0, y, z0), new Vector3(x1, y, z0), new Vector3(x1, y, z1));

    [Fact]
    public void PolygonsOnTheWalkingSurfaceAreGroundAndTheRestIsAboveIt()
    {
        var heights = HeightPlates.Parse(Bdhc((-256, -256, 256, 256, Up, 0)));
        var model = new TerrainModel();
        // Lawn over the whole chunk, a path across it, and the crown of a tree two tiles up over one corner
        model.Polygons.Add(Flat("ngrass", -256, -256, 256, 256, 0));
        model.Polygons.Add(Flat("ngrass", -256, -256, 256, 256, 0, second: true));
        model.Polygons.Add(Flat("nsand", -256, -16, 256, 16, 0.5f));
        model.Polygons.Add(Flat("nsand", -256, -16, 256, 16, 0.5f, second: true));
        model.Polygons.Add(Flat("tree01", -256, -256, -224, -224, 32));
        model.Polygons.Add(Flat("tree01", -256, -256, -224, -224, 32, second: true));
        // A wall standing on edge covers no tile
        model.Polygons.Add(new TerrainModel.Polygon("criff", "", new Vector3(0, 0, 100), new Vector3(64, 0, 100), new Vector3(64, 64, 100)));

        var layers = model.LayersOver(heights);

        Assert.Equal("ngrass", layers.Ground[3 * 32 + 3]);
        Assert.Equal("nsand", layers.Ground[15 * 32 + 3]);    // the path lies over the lawn: the topmost ground wins
        Assert.Equal("nsand", layers.Ground[16 * 32 + 20]);
        Assert.Equal("tree01", layers.Above[0]);
        Assert.Equal("ngrass", layers.Ground[0]);              // the lawn is still there under the tree
        Assert.Null(layers.Above[3 * 32 + 3]);
        Assert.DoesNotContain("criff", layers.Ground);
        Assert.DoesNotContain("criff", layers.Above);
    }

    // ---------------------------------------------------------------- what a tile looks like

    [Theory]
    [InlineData("ngrass", TerrainCover.Grass)]
    [InlineData("lgreenp", TerrainCover.Grass)]
    [InlineData("t3_fl_g", TerrainCover.Grass)]
    [InlineData("nectgr", TerrainCover.TallGrass)]
    [InlineData("nhana", TerrainCover.Flowers)]
    [InlineData("t3_fl_p.1", TerrainCover.Flowers)]
    [InlineData("nsand", TerrainCover.Path)]
    [InlineData("nsandp", TerrainCover.Path)]
    [InlineData("blueglay", TerrainCover.Path)]
    [InlineData("c1_r1_ud", TerrainCover.Paving)]
    [InlineData("c07_base_k2", TerrainCover.Paving)]
    [InlineData("c3_grand", TerrainCover.Paving)]
    [InlineData("r206_cy2", TerrainCover.Paving)]
    [InlineData("dun08_chip_a", TerrainCover.Paving)]
    [InlineData("colum_b", TerrainCover.Paving)]
    [InlineData("tree01", TerrainCover.Tree)]
    [InlineData("conttree2_b", TerrainCover.Tree)]
    [InlineData("g1_treeb", TerrainCover.Tree)]
    [InlineData("imped", TerrainCover.Tree)]
    [InlineData("criffp2", TerrainCover.Cliff)]
    [InlineData("allpeak", TerrainCover.Cliff)]
    [InlineData("wcliff", TerrainCover.Rock)]
    [InlineData("enccliff", TerrainCover.Rock)]
    [InlineData("searock", TerrainCover.Boulder)]
    [InlineData("sea", TerrainCover.Water)]
    [InlineData("lakep.1", TerrainCover.Water)]
    [InlineData("beach", TerrainCover.Sand)]
    [InlineData("hamabe", TerrainCover.Sand)]
    [InlineData("s_snow04", TerrainCover.Snow)]
    [InlineData("s_sonwp", TerrainCover.Snow)]
    [InlineData("c09_ice2", TerrainCover.Ice)]
    [InlineData("numa_a2", TerrainCover.Marsh)]
    [InlineData("nbridge", TerrainCover.Bridge)]
    [InlineData("newstep", TerrainCover.Steps)]
    [InlineData("c1_lamp02", TerrainCover.Lamp)]
    [InlineData("c5_light", TerrainCover.Lamp)]
    [InlineData("dun_light", TerrainCover.Fence)]
    [InlineData("a8_sora_a", TerrainCover.Walkway)]
    [InlineData("a8_sora_f", TerrainCover.Fence)]
    [InlineData("tree3_02", TerrainCover.Broadleaf)]
    [InlineData("bf_tree01", TerrainCover.Broadleaf)]
    [InlineData("t03_gate", TerrainCover.Fence)]
    [InlineData("area07_hei_h3", TerrainCover.Fence)]
    public void ATexturesNameSaysWhatTheGroundIs(string texture, TerrainCover expected)
    {
        Assert.Equal(expected, Cover.OfTexture(texture, out bool known));
        Assert.True(known);
    }

    [Fact]
    public void ShadowsSayNothingAndUnknownNamesAreReported()
    {
        Assert.Null(Cover.OfTexture("tshadow", out bool shadowKnown));
        Assert.True(shadowKnown);
        Assert.Null(Cover.OfTexture("puddle_b", out bool puddleKnown));
        Assert.True(puddleKnown);

        Assert.Null(Cover.OfTexture("kao_kanban", out bool known));   // Pastoria's sign: sorted when its town is built
        Assert.False(known);
        // A forest's way in has been sorted since the first forest was built (plan 01 · M6), and so has a Bicycle's ramp
        Assert.Equal(TerrainCover.ForestMouth, Cover.OfTexture("fenter", out bool forestKnown));
        Assert.True(forestKnown);
        Assert.Equal(TerrainCover.Steps, Cover.OfTexture("dun_jump", out _));
        // A cave's mouth has been sorted since the first cave was built (plan 01 · M5)
        Assert.Equal(TerrainCover.CaveMouth, Cover.OfTexture("dhole", out bool mouthKnown));
        Assert.True(mouthKnown);
        Assert.Equal(TerrainCover.CaveMouth, Cover.OfTexture("dhole_05", out _));
        Assert.Null(Cover.OfTexture(null, out _));
    }

    [Fact]
    public void ABehaviourThatShowsDecidesTheLookBeforeAnyTexture()
    {
        Assert.Equal(TerrainCover.TallGrass, Cover.OfBehaviour(TileBehavior.TallGrass));
        Assert.Equal(TerrainCover.TallGrass, Cover.OfBehaviour(TileBehavior.VeryTallGrass));
        Assert.Equal(TerrainCover.Water, Cover.OfBehaviour(TileBehavior.Sea));
        Assert.Equal(TerrainCover.Water, Cover.OfBehaviour(TileBehavior.Waterfall));
        Assert.Equal(TerrainCover.Snow, Cover.OfBehaviour(TileBehavior.DeepestSnow));
        Assert.Equal(TerrainCover.Marsh, Cover.OfBehaviour(TileBehavior.DeepMarshGrass));
        Assert.Equal(TerrainCover.Bridge, Cover.OfBehaviour(TileBehavior.BikeBridgeEastWestOverWater));
        Assert.Equal(TerrainCover.CaveFloor, Cover.OfBehaviour(TileBehavior.CaveFloor));
        Assert.Equal(TerrainCover.Puddle, Cover.OfBehaviour(TileBehavior.Puddle));
        Assert.Equal(TerrainCover.Puddle, Cover.OfBehaviour(TileBehavior.StillPuddle));
        Assert.Null(Cover.OfBehaviour(TileBehavior.None));
        Assert.Null(Cover.OfBehaviour(TileBehavior.Door));
        Assert.Null(Cover.OfBehaviour(TileBehavior.LedgeSouth));
    }

    [Fact]
    public void OnlyBlockedTilesUnderALargePropBecomeBuilding()
    {
        var attributes = new ushort[1024];
        for (int z = 10; z < 13; z++)
            for (int x = 8; x < 12; x++) attributes[z * 32 + x] = 0x8000;   // the house's walls
        attributes[13 * 32 + 9] = 0x0000;                                     // open ground under its porch roof
        attributes[0] = 0x8000;                                               // a blocked tile far from any prop

        // The house's origin is at the middle of its front edge: tile (10, 13)
        var land = LandData.Parse(Land(attributes, Prop(22, -96, 0, -48), Bdhc((-256, -256, 256, 256, Up, 0))));
        var house = new ModelInfo("t1_h01", new Vector3(-32, 0, -48), new Vector3(64, 70, 64));
        var sign = new ModelInfo("board_a", new Vector3(-9, 1, -3), new Vector3(18, 32, 8));

        Assert.True(Cover.IsBuilding(house));
        Assert.False(Cover.IsBuilding(sign));
        Assert.False(Cover.IsBuilding(new ModelInfo("l_lake", new Vector3(-256, -8, -256), new Vector3(512, 0, 512))));

        var cover = Cover.Of(land, id => id == 22 ? house : null);
        Assert.Equal(TerrainCover.Building, cover[10 * 32 + 8]);
        Assert.Equal(TerrainCover.Building, cover[12 * 32 + 11]);
        Assert.NotEqual(TerrainCover.Building, cover[13 * 32 + 9]);   // open: a walker stands there
        Assert.NotEqual(TerrainCover.Building, cover[0]);
    }

    // ---------------------------------------------------------------- the lists

    [Fact]
    public void AMatrixIsAGridOfChunksWithAreasAndAltitudesWhenItHasThem()
    {
        var overworld = Matrix.Parse(0, """
            {
                "name": "map",
                "headers": [["MAP_HEADER_EVERYWHERE", "MAP_HEADER_TWINLEAF_TOWN"], ["MAP_HEADER_ROUTE_201", "MAP_HEADER_ROUTE_201"]],
                "altitudes": [[0, 2], [0, 0]],
                "maps": [["MAP_NONE", "MAP_000"], ["MAP_003", "MAP_172"]]
            }
            """);
        Assert.Equal((2, 2), (overworld.Width, overworld.Height));
        Assert.Equal(Matrix.NoLand, overworld.LandAt(0, 0));
        Assert.Equal(0, overworld.LandAt(1, 0));
        Assert.Equal(172, overworld.LandAt(1, 1));
        Assert.Equal("MAP_HEADER_TWINLEAF_TOWN", overworld.HeaderAt(1, 0));
        Assert.Equal(2, overworld.AltitudeAt(1, 0));

        var room = Matrix.Parse(1, """{ "name": "single", "headers": [], "altitudes": [], "maps": [["MAP_001"]] }""");
        Assert.Equal((1, 1), (room.Width, room.Height));
        Assert.Null(room.HeaderAt(0, 0));
        Assert.Equal(0, room.AltitudeAt(0, 0));

        Assert.Throws<InvalidDataException>(() => Matrix.Parse(2, """{ "name": "x", "headers": [], "altitudes": [[0]], "maps": [["MAP_001", "MAP_002"]] }"""));
    }

    [Fact]
    public void TheHeaderTableIsReadFromItsSource()
    {
        const string source = """
            static const MapHeader sMapHeaders[] = {
                [MAP_HEADER_EVERYWHERE] = {
                    .areaDataArchiveID = area_data_000,
                    .mapMatrixID = map_matrix_000,
                    .scriptsArchiveID = scripts_empty,
                    .wildEncountersArchiveID = ENCOUNTERS_NONE,
                    .eventsArchiveID = events_empty,
                    .dayMusicID = SEQ_DUMMY_sseq,
                    .nightMusicID = SEQ_DUMMY_sseq,
                    .mapLabelTextID = LocationNames_Text_MysteryZone,
                    .mapLabelWindowID = MAP_LABEL_WINDOW_WATER,
                    .weather = OVERWORLD_WEATHER_CLEAR,
                    .cameraType = CAMERA_TYPE_DEFAULT,
                    .mapType = MAP_TYPE_OUTDOORS,
                    .battleBG = BACKGROUND_FOREST,
                    .isBikeAllowed = TRUE,
                    .isRunningAllowed = TRUE,
                    .isEscapeRopeAllowed = TRUE,
                    .isFlyAllowed = FALSE,
                },
                [MAP_HEADER_ROUTE_216] = {
                    .areaDataArchiveID = area_data_012,
                    .mapMatrixID = map_matrix_000,
                    .scriptsArchiveID = scripts_route_216,
                    .wildEncountersArchiveID = encounters_route_216,
                    .eventsArchiveID = events_route_216,
                    .dayMusicID = SEQ_ROAD_SNOW_D_sseq,
                    .nightMusicID = SEQ_ROAD_SNOW_N_sseq,
                    .mapLabelTextID = LocationNames_Text_Route216,
                    .mapLabelWindowID = MAP_LABEL_WINDOW_ROUTE,
                    .weather = OVERWORLD_WEATHER_ROUTE_216,
                    .cameraType = CAMERA_TYPE_DEFAULT,
                    .mapType = MAP_TYPE_OUTDOORS,
                    .battleBG = BACKGROUND_SNOW,
                    .isBikeAllowed = TRUE,
                    .isRunningAllowed = TRUE,
                    .isEscapeRopeAllowed = FALSE,
                    .isFlyAllowed = TRUE,
                },
            };
            """;
        var headers = DecompMaps.ParseHeaders(source, new[] { "MAP_HEADER_EVERYWHERE", "MAP_HEADER_ROUTE_216" });

        var route = headers["MAP_HEADER_ROUTE_216"];
        Assert.Equal("route_216", route.Key);
        Assert.Equal(1, route.Index);
        Assert.Equal(0, route.Matrix);
        Assert.Equal("events_route_216", route.Events);
        Assert.Equal("encounters_route_216", route.Encounters);
        Assert.Equal("ROUTE_216", route.Weather);
        Assert.Equal("OUTDOORS", route.MapType);
        Assert.Equal("SNOW", route.BattleBackground);
        Assert.True(route.Fly);
        Assert.False(route.EscapeRope);
        Assert.Null(headers["MAP_HEADER_EVERYWHERE"].Encounters);

        Assert.Throws<InvalidDataException>(() => DecompMaps.ParseHeaders(source, new[] { "MAP_HEADER_EVERYWHERE" }));
    }

    [Fact]
    public void AnItemBallsScriptSaysWhatLiesInIt()
    {
        // The file lists its scripts in order (the nth is script 7000 + n), and each sets the item and how many
        var items = DecompMaps.ParseVisibleItems("""
            #include "macros/scrcmd.inc"

                ScriptEntry VisibleItems_Route202_Potion
                ScriptEntry VisibleItems_Route203_PokeBall
                ScriptEntry VisibleItems_Somewhere_Nothing
                ScriptEntry VisibleItems_Somewhere_ThreeNuggets
                ScriptEntryEnd

            VisibleItems_Route202_Potion:
                SetVar VAR_0x8008, ITEM_POTION
                SetVar VAR_0x8009, 1
                GoTo VisibleItems_TryGiveItem
                End

            VisibleItems_Somewhere_ThreeNuggets:
                SetVar VAR_0x8008, ITEM_NUGGET
                SetVar VAR_0x8009, 3
                GoTo VisibleItems_TryGiveItem
                End

            VisibleItems_Route203_PokeBall:
                SetVar VAR_0x8008, ITEM_POKE_BALL
                SetVar VAR_0x8009, 1
                GoTo VisibleItems_TryGiveItem
                End

            VisibleItems_Somewhere_Nothing:
                End

            VisibleItems_TryGiveItem:
                SetVar VAR_0x8004, VAR_0x8008
                SetVar VAR_0x8005, VAR_0x8009
                End
            """);

        Assert.Equal(new[] { ("ITEM_POTION", 1), ("ITEM_POKE_BALL", 1), ("", 0), ("ITEM_NUGGET", 3) }, items);
    }

    [Fact]
    public void AHiddenItemsNumberIsItsFlagsPlaceAmongTheFlags()
    {
        // The flags have a gap the table lacks: the third entry is the hidden item numbered 3, not 2
        const string flags = """
            FLAG_UNUSED_0x02D9
            HIDDEN_ITEM_FLAGS_START
            FLAG_OBTAINED_HIDDEN_VALLEY_WINDWORKS_OUTSIDE_MAX_ELIXIR = HIDDEN_ITEM_FLAGS_START
            FLAG_OBTAINED_HIDDEN_ETERNA_FOREST_INSECT_PLATE
            FLAG_UNUSED_0x03BF
            FLAG_OBTAINED_HIDDEN_SOMEWHERE_STARDUST
            HIDDEN_ITEM_FLAGS_END = FLAG_OBTAINED_HIDDEN_SOMEWHERE_STARDUST
            FLAG_SOMETHING_ELSE
            """;
        var hidden = DecompMaps.ParseHiddenItems("""
            #define HIDDEN_ITEM_ENTRY(item_in, qty_in, range_in, script_in)                                                    \
                {                                                                                                              \
                    .item = item_in, .qty = qty_in, .range = range_in, .pad = 0, .script = script_in - HIDDEN_ITEM_FLAGS_START \
                }

            const HiddenItem gHiddenItems[] = {
                HIDDEN_ITEM_ENTRY(ITEM_MAX_ELIXIR,   1, 2, FLAG_OBTAINED_HIDDEN_VALLEY_WINDWORKS_OUTSIDE_MAX_ELIXIR),
                HIDDEN_ITEM_ENTRY(ITEM_INSECT_PLATE, 1, 0, FLAG_OBTAINED_HIDDEN_ETERNA_FOREST_INSECT_PLATE),
                HIDDEN_ITEM_ENTRY(ITEM_STARDUST, 3, 2, FLAG_OBTAINED_HIDDEN_SOMEWHERE_STARDUST),
            };
            """, flags);

        Assert.Equal(new[] { 0, 1, 3 }, hidden.Keys.Order());
        Assert.Equal(new HiddenItemEntry("ITEM_MAX_ELIXIR", 1, 2, "FLAG_OBTAINED_HIDDEN_VALLEY_WINDWORKS_OUTSIDE_MAX_ELIXIR"), hidden[0]);
        Assert.Equal(("ITEM_INSECT_PLATE", 0), (hidden[1].Item, hidden[1].Range));
        Assert.Equal(3, hidden[3].Count);
        // A flag the list lacks is a table that has moved on without it
        Assert.Throws<InvalidDataException>(() => DecompMaps.ParseHiddenItems("HIDDEN_ITEM_ENTRY(ITEM_NUGGET, 1, 2, FLAG_OBTAINED_HIDDEN_NOWHERE_NUGGET),", flags));
    }

    [Fact]
    public void TheWorldsFilesSayWhatEachBallAndEachHiddenPlaceHolds()
    {
        // The areas the game ships: every ball's script is an item ball's and has its item, every hidden item its flag
        var world = World.LoadAll().Single(w => w.Index.Region == "Sinnoh");
        int balls = 0, hidden = 0;
        foreach (string key in world.Index.Areas)
        {
            var area = world.Area(key)!;
            foreach (var o in area.Objects.Where(o => int.TryParse(o.Script, out int script) && script >= DecompMaps.FirstVisibleItemScript && script < DecompMaps.FirstHiddenItemScript))
            {
                balls++;
                Assert.True(o.Item != null && ItemDatabase.Get(o.Item) != null, $"{key}: the ball {o.Id} holds '{o.Item}'");
                Assert.Equal("pokeball", o.Looks);
                Assert.StartsWith("FLAG_", o.HiddenBy);
            }
            Assert.All(area.Objects.Where(o => o.Item == null), o => Assert.Null(o.Count));
            foreach (var s in area.Signs.Where(s => s.Type == AreaSign.HiddenItem))
            {
                hidden++;
                Assert.True(s.Item != null && ItemDatabase.Get(s.Item) != null, $"{key}: the hidden item at {s.X},{s.Z} is '{s.Item}'");
                Assert.StartsWith("FLAG_OBTAINED_HIDDEN_", s.Flag);
            }
        }
        Assert.True(balls >= 37 && hidden >= 20, $"{balls} balls and {hidden} hidden items");
    }

    /// <summary>A Nitro archive of the given members, as the original's NARCs are laid out (header, BTAF, BTNF, GMIF).</summary>
    private static byte[] Narc(params byte[][] members)
    {
        var data = new List<byte>();
        var table = new List<byte>(Le(members.Length));
        foreach (var member in members)
        {
            table.AddRange(Le(data.Count, data.Count + member.Length));
            data.AddRange(member);
            while (data.Count % 4 != 0) data.Add(0xFF);
        }
        byte[] Section(string magic, List<byte> body) => Join(Encoding.ASCII.GetBytes(magic), Le(8 + body.Count), body.ToArray());
        var btaf = Section("BTAF", table);
        var btnf = Section("BTNF", new List<byte>(Le(4, 0x10000)));
        var gmif = Section("GMIF", data);
        int size = 16 + btaf.Length + btnf.Length + gmif.Length;
        var header = Join(Encoding.ASCII.GetBytes("NARC"), Le16(0xFFFE, 0x0100), Le(size), Le16(16, 3));
        return Join(header, btaf, btnf, gmif);
    }

    [Fact]
    public void TheDistortionWorldsFloorsLieAtOffsetsAndItsFloatingFloorsAreRead()
    {
        // Member 0: two maps, { header, file, x, altitude, z }; then each map's file: five sizes, then its floating
        // platforms (a count and 20 bytes each). B2F has a floor nine up with attributes 0, and a wall that is left out
        var mapInfo = Join(Le(2), Le(573), Le16(0, 21, 288, 10), Le(575), Le16(1, 15, 224, 0));
        var firstFloor = Join(Le(0, 0, 0, 0, 4), Le(0));
        var platforms = Join(Le(2),
            Le16(0, 0, 15, 233, 0, 3, 0, 1, 4, 4),     // a floor: x 15 to 18, z 0 to 1, attributes 0, rows of 4
            Le16(1, 0, 30, 225, 15, 0, 8, 8, 32, 32)); // a west wall: never walked here
        var b2f = Join(Le(0, platforms.Length, 0, 0, 0), platforms);
        // Attributes: east first, a row of four to each step south; open at (1,0) and (2,1), the rest blocked
        var attributes = Le16(0x8000, 0x005B, 0x8000, 0x8000, 0x8000, 0x8000, 0x0000, 0x8000);

        var world = DistortionWorld.Parse(Narc(mapInfo, firstFloor, b2f), Narc(attributes));
        Assert.Equal((21, 288, 10), world.Offsets[573]);
        Assert.Equal((15, 224, 0), world.Offsets[575]);
        Assert.False(world.Floors.ContainsKey(573));
        var floor = Assert.Single(world.Floors[575]);
        // In tiles of B2F's own map: the floor's corner less the map's offset, and its height above the map's
        Assert.Equal((0, 0, 4, 2, 9), (floor.X, floor.Z, floor.Width, floor.Depth, floor.Height));
        Assert.False(floor.Solid(1, 0));
        Assert.Equal((byte)TileBehavior.LongLedgeSouth, floor.Behaviour(1, 0));
        Assert.False(floor.Solid(2, 1));
        Assert.True(floor.Solid(0, 0) && floor.Solid(3, 1));

        Assert.Throws<InvalidDataException>(() => DistortionWorld.Narc(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 }));
    }

    [Fact]
    public void BehaviourNamesAreNumberedByTheirPlaceInTheList()
    {
        var names = DecompMaps.ParseBehaviourNames("""
            enum TileBehavior {
                TILE_BEHAVIOR_NONE = 0,
                TILE_BEHAVIOR_UNUSED_x01,
                TILE_BEHAVIOR_TALL_GRASS,

                TILE_BEHAVIOR_MAX,
            };
            """);
        Assert.Equal(new[] { "NONE", "UNUSED_x01", "TALL_GRASS" }, names);
        Assert.Equal("TALL_GRASS", names[(int)TileBehavior.TallGrass]);
    }

    [Fact]
    public void TheWeatherCalendarIsADayOfALeapYearForEachOfFivePlaces()
    {
        // sYearlyWeather's rows, written as the original writes them, in any order
        var source = new System.Text.StringBuilder("static const u8 sYearlyWeather[DAY_OF_YEAR_COUNT][OVERWORLD_WEATHER_YEARLY_COUNT] = {\n");
        string[] months = { "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" };
        for (var day = new DateTime(2000, 12, 31); day.Year == 2000; day = day.AddDays(-1))
        {
            string first = day is { Month: 2, Day: 29 } ? "OVERWORLD_WEATHER_THUNDERSTORM" : "OVERWORLD_WEATHER_RAINING";
            source.Append($"    [DAY_OF_YEAR_{months[day.Month - 1]}_{day.Day:00} - 1] = {{{first}, OVERWORLD_WEATHER_CLEAR, OVERWORLD_WEATHER_HEAVY_SNOW, OVERWORLD_WEATHER_SNOWING, OVERWORLD_WEATHER_HAILING}},\n");
        }
        source.Append("};\n");

        var (places, days) = DecompMaps.ParseCalendar(source.ToString());
        Assert.Equal(new[] { "Route212South", "Route213", "Route216", "AcuityLakefront", "SnowpointCity" }, places);
        Assert.Equal(366, days.Count);
        Assert.Equal(new[] { "Raining", "Clear", "HeavySnow", "Snowing", "Hailing" }, days[0]);
        Assert.Equal("Thunderstorm", days[59][0]);
        Assert.Equal("Raining", days[60][0]);

        // A table that lost a day is no calendar
        string shortOne = string.Join("\n", source.ToString().Split('\n').Where(l => !l.Contains("DAY_OF_YEAR_JUL_04")));
        Assert.Throws<InvalidDataException>(() => DecompMaps.ParseCalendar(shortOne));
    }

    [Fact]
    public void EventsTakeNumbersOrNamesForScriptsFlagsAndValues()
    {
        var events = AreaEvents.Parse("""
            {
                "bg_events": [{ "script": 8201, "type": 2, "x": 107, "z": 893, "y": 0, "player_facing_dir": "BG_EVENT_DIR_ALL" }],
                "object_events": [{
                    "id": "LOCALID_RIVAL", "graphics_id": "OBJ_EVENT_GFX_BARRY", "movement_type": "MOVEMENT_TYPE_LOOK_SOUTH",
                    "trainer_type": "TRAINER_TYPE_NONE", "hidden_flag": "FLAG_HIDE_TWINLEAF_TOWN_RIVAL", "script": "SCRIPT_ID(TRAINER, 12)",
                    "initial_dir": 1, "data": [], "movement_range_x": 0, "movement_range_z": 2, "x": 105, "z": 875, "y": 0
                }],
                "warp_events": [{ "x": 116, "z": 885, "dest_header_id": "MAP_HEADER_TWINLEAF_TOWN_PLAYER_HOUSE_1F", "dest_warp_id": 0 }],
                "coord_events": [{ "script": 4, "x": 108, "z": 867, "y": 0, "width": 8, "length": 1, "var": "VAR_TWINLEAF_TOWN_GUITARIST_TRIGGER_STATE", "value": 1 }]
            }
            """);

        Assert.Equal("8201", Assert.Single(events.Signs).Script);
        var rival = Assert.Single(events.Objects);
        Assert.Equal(("LOCALID_RIVAL", "SCRIPT_ID(TRAINER, 12)", 105, 875, 2), (rival.Id, rival.Script, rival.X, rival.Z, rival.MovementRangeZ));
        Assert.Equal("MAP_HEADER_TWINLEAF_TOWN_PLAYER_HOUSE_1F", Assert.Single(events.Warps).DestHeaderId);
        var trigger = Assert.Single(events.Triggers);
        Assert.Equal((8, 1, "1"), (trigger.Width, trigger.Length, trigger.Value));

        Assert.Empty(AreaEvents.Parse("{}").Warps);
    }

    [Fact]
    public void NamesAreOursNotTheOriginalsConstants()
    {
        Assert.Equal("twinleaf_town_player_house_1f", WorldWriter.KeyOf("MAP_HEADER_TWINLEAF_TOWN_PLAYER_HOUSE_1F"));
        Assert.Equal("HeavyRain", WorldWriter.Pascal("HEAVY_RAIN"));
        Assert.Equal("Clear", WorldWriter.Pascal("CLEAR"));
        Assert.Equal("MtCoronetExtSouth", WorldWriter.Pascal("MT_CORONET_EXT_SOUTH"));
    }

    // ---------------------------------------------------------------- pictures

    [Fact]
    public void APictureIsSavedAsAPngAnyViewerOpens()
    {
        var picture = new Picture(3, 2, Rgb.Of(10, 20, 30));
        picture.Set(1, 0, Rgb.Of(200, 100, 50));
        picture.Text(0, 0, "A", Rgb.Of(255, 255, 255));
        string path = Path.Combine(Path.GetTempPath(), $"map-import-test-{Guid.NewGuid():N}.png");
        try
        {
            picture.Save(path);
            var bytes = File.ReadAllBytes(path);
            Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, bytes.Take(8));
            Assert.Equal("IHDR", Encoding.ASCII.GetString(bytes, 12, 4));
            Assert.Equal(3, BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16)));
            Assert.Equal(2, BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20)));
            // The header chunk's checksum is the standard one over its type and data
            Assert.Equal(Crc32(bytes.AsSpan(12, 17)), BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(29)));
            Assert.Equal("IEND", Encoding.ASCII.GetString(bytes, bytes.Length - 8, 4));
        }
        finally { File.Delete(path); }
    }

    private static uint Crc32(ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte b in data)
        {
            crc ^= b;
            for (int k = 0; k < 8; k++) crc = (crc & 1) != 0 ? 0xEDB88320 ^ (crc >> 1) : crc >> 1;
        }
        return crc ^ 0xFFFFFFFF;
    }

    [Fact]
    public void EveryBehaviourHasItsOwnColourGroupOrIsPlainGround()
    {
        foreach (TileBehavior behaviour in Enum.GetValues<TileBehavior>())
        {
            if (behaviour is TileBehavior.None or TileBehavior.NoExplorerKit) continue;
            if (behaviour.ToString().StartsWith("Unknown", StringComparison.Ordinal)) continue;
            Assert.True(Renders.Groups.Count(g => g.Covers((byte)behaviour)) == 1, $"{behaviour} must be in exactly one colour group");
        }
    }
}

/// <summary>The world files the importer writes and the game will load (plan 01 · M2).</summary>
public class WorldFileTests
{
    private static WorldChunkFile Chunk()
    {
        var chunk = new WorldChunkFile { Id = 7 };
        for (int z = 0; z < WorldChunkFile.Tiles; z++)
        {
            chunk.Behaviours.Add(string.Concat(Enumerable.Repeat("00", WorldChunkFile.Tiles)));
            chunk.Solid.Add(new string('.', WorldChunkFile.Tiles));
            chunk.Cover.Add(new string('.', WorldChunkFile.Tiles));
        }
        chunk.Behaviours[5] = "0002" + chunk.Behaviours[5][4..];
        chunk.Behaviours[9] = chunk.Behaviours[9][..14] + "69" + chunk.Behaviours[9][16..];
        chunk.Solid[9] = chunk.Solid[9][..7] + "#" + chunk.Solid[9][8..];
        chunk.Cover[5] = ".w" + chunk.Cover[5][2..];
        chunk.Cover[9] = chunk.Cover[9][..7] + "B" + chunk.Cover[9][8..];
        chunk.Heights.Add(new HeightPlate { X = 0, Z = 0, Width = 32, Depth = 32, Height = 0 });
        chunk.Heights.Add(new HeightPlate { X = 4, Z = 10, Width = 2, Depth = 4, Height = 0, SlopeZ = 0.5f });
        chunk.Heights.Add(new HeightPlate { X = 20, Z = 0, Width = 4, Depth = 32, Height = 3 });
        chunk.Props.Add(new ChunkProp { Model = 22, Name = "t1_h01", X = 7.5f, Y = 0, Z = 9.5f, BoxX = 5.4f, BoxZ = 6.7f, Width = 4.2f, Depth = 2.8f, Height = 4.4f });
        return chunk;
    }

    [Fact]
    public void AChunkSurvivesBeingWrittenAndReadBack()
    {
        var chunk = Chunk();
        string json = GameDataFiles.Serialize(chunk);
        var back = System.Text.Json.JsonSerializer.Deserialize<WorldChunkFile>(json, GameDataFiles.Json)!;
        back.Validate();

        Assert.Equal(json, GameDataFiles.Serialize(back));
        Assert.Equal(TileBehavior.TallGrass, back.BehaviourAt(1, 5));
        Assert.Equal(TileBehavior.Door, back.BehaviourAt(7, 9));
        Assert.True(back.SolidAt(7, 9));
        Assert.False(back.SolidAt(6, 9));
        Assert.Equal(TerrainCover.TallGrass, back.CoverAt(1, 5));
        Assert.Equal(TerrainCover.Building, back.CoverAt(7, 9));
        Assert.Equal(3, back.Heights.Count);
        Assert.Equal(0.5f, back.Heights[1].SlopeZ);
        var prop = Assert.Single(back.Props);
        Assert.Equal(("t1_h01", 22, 4.2f), (prop.Name, prop.Model, prop.Width));
    }

    [Fact]
    public void PlatesAndPropsTakeOneLineEach()
    {
        string json = GameDataFiles.Serialize(Chunk());
        Assert.Contains("""{ "x": 4, "z": 10, "width": 2, "depth": 4, "height": 0, "slopeX": 0, "slopeZ": 0.5 }""", json);
        Assert.Contains("\"name\": \"t1_h01\"", json);
        var lines = json.Split('\n');
        Assert.Equal(3, lines.Count(line => line.Contains("slopeX")));
        Assert.Single(lines, line => line.Contains("t1_h01"));
        Assert.All(lines.Where(line => line.Contains("slopeX")), line => Assert.StartsWith("    { \"x\":", line));
    }

    [Fact]
    public void TheGroundsHeightFollowsThePlateAndTheWalkersLevel()
    {
        var chunk = Chunk();
        Assert.Equal(0f, chunk.HeightAt(1.5f, 1.5f));
        Assert.Equal(1f, chunk.HeightAt(5f, 12f, from: 1f)!.Value, 3);     // half-way up the ramp, coming from above
        Assert.Equal(0f, chunk.HeightAt(5f, 12f, from: 0f)!.Value, 3);     // the ground under it, for a walker down there
        Assert.Equal(3f, chunk.HeightAt(21f, 5f, from: 2.6f));              // on the bridge
        Assert.Equal(0f, chunk.HeightAt(21f, 5f, from: 0.2f));              // under it
        Assert.Null(new WorldChunkFile().HeightAt(1, 1));
    }

    [Fact]
    public void ADamagedChunkIsRefusedWithItsFaultNamed()
    {
        var chunk = Chunk();
        chunk.Validate();

        chunk.Solid[3] = chunk.Solid[3][..5] + "x" + chunk.Solid[3][6..];
        Assert.Contains("solid row 3", Assert.Throws<InvalidDataException>(chunk.Validate).Message);

        chunk = Chunk();
        chunk.Behaviours[2] = "ZZ" + chunk.Behaviours[2][2..];
        Assert.Contains("behaviours row 2", Assert.Throws<InvalidDataException>(chunk.Validate).Message);

        chunk = Chunk();
        chunk.Cover[0] = "!" + chunk.Cover[0][1..];
        Assert.Throws<InvalidDataException>(chunk.Validate);

        chunk = Chunk();
        chunk.Cover.RemoveAt(0);
        Assert.Contains("32 rows", Assert.Throws<InvalidDataException>(chunk.Validate).Message);
    }

    [Fact]
    public void EveryKindOfCoverHasACodeOfItsOwn()
    {
        var covers = Enum.GetValues<TerrainCover>();
        Assert.Equal(covers.OrderBy(c => c), TerrainCoverCodes.All.OrderBy(c => c));
        Assert.Equal(covers.Length, covers.Select(TerrainCoverCodes.CodeOf).Distinct().Count());
        foreach (var cover in covers) Assert.Equal(cover, TerrainCoverCodes.Parse(TerrainCoverCodes.CodeOf(cover)));
        Assert.Throws<InvalidDataException>(() => TerrainCoverCodes.Parse('!'));
    }

    [Fact]
    public void AMatrixKeepsItsCellsInRowsThatLineUp()
    {
        var matrix = new WorldMatrixFile
        {
            Id = 0,
            Width = 3,
            Height = 2,
            Chunks = { WorldMatrixFile.Row(new[] { "-", "0", "172" }, 3), WorldMatrixFile.Row(new[] { "3", "-", "8" }, 3) },
            Areas = new() { WorldMatrixFile.Row(new[] { "0", "1", "0" }, 1), WorldMatrixFile.Row(new[] { "2", "0", "2" }, 1) },
            AreaKeys = new() { "everywhere", "twinleaf_town", "route_201" },
            Altitudes = new() { WorldMatrixFile.Row(new[] { "0", "2", "0" }, 2), WorldMatrixFile.Row(new[] { "0", "0", "10" }, 2) }
        };
        Assert.Equal("  -   0 172", matrix.Chunks[0]);

        var back = System.Text.Json.JsonSerializer.Deserialize<WorldMatrixFile>(GameDataFiles.Serialize(matrix), GameDataFiles.Json)!;
        Assert.Equal(WorldMatrixFile.NoChunk, back.ChunkAt(0, 0));
        Assert.Equal(172, back.ChunkAt(2, 0));
        Assert.Equal(3, back.ChunkAt(0, 1));
        Assert.Equal("twinleaf_town", back.AreaAt(1, 0));
        Assert.Equal("route_201", back.AreaAt(2, 1));
        Assert.Equal(10, back.AltitudeAt(2, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => back.ChunkAt(3, 0));

        var room = new WorldMatrixFile { Id = 1, Width = 1, Height = 1, Chunks = { "1" } };
        Assert.Null(room.AreaAt(0, 0));
        Assert.Equal(0, room.AltitudeAt(0, 0));
    }

    [Fact]
    public void AnAreaKeepsItsEventsAndLeavesOutWhatItLacks()
    {
        var area = new WorldAreaFile
        {
            Key = "twinleaf_town", Index = 411, Name = "Twinleaf Town", Matrix = 0, Kind = "Town", Sign = "Town",
            Weather = "Clear", Camera = "Default", BattleBackground = "Forest", DayMusic = "TOWN01_D", NightMusic = "TOWN01_N",
            Bike = true, Running = true, Fly = true,
            Warps = { new AreaWarp { X = 116, Z = 885, To = "twinleaf_town_player_house_1f", ToWarp = 0 } },
            Objects = { new AreaObject { Id = "rival", Looks = "barry", Movement = "look_south", X = 105, Z = 875, Facing = 1, HiddenBy = "FLAG_HIDE_TWINLEAF_TOWN_RIVAL", Script = "0" } },
            Signs = { new AreaSign { X = 107, Z = 893, Type = 2, Script = "8201" } },
            Triggers = { new AreaTrigger { X = 108, Z = 867, Width = 8, Script = "4", Variable = "VAR_TWINLEAF_TOWN_GUITARIST_TRIGGER_STATE", Value = "1" } }
        };

        string json = GameDataFiles.Serialize(area);
        Assert.DoesNotContain("encounters", json);
        Assert.DoesNotContain("\"trainer\"", json);
        Assert.Contains("    { \"id\": \"rival\", \"looks\": \"barry\", \"movement\": \"look_south\", \"x\": 105, \"z\": 875, \"facing\": 1, \"rangeX\": 0, \"rangeZ\": 0, \"hiddenBy\": \"FLAG_HIDE_TWINLEAF_TOWN_RIVAL\", \"script\": \"0\" }", json);
        Assert.Contains("""{ "x": 116, "z": 885, "to": "twinleaf_town_player_house_1f", "toWarp": 0 }""", json);

        var back = System.Text.Json.JsonSerializer.Deserialize<WorldAreaFile>(json, GameDataFiles.Json)!;
        Assert.Equal(json, GameDataFiles.Serialize(back));
        Assert.Equal("twinleaf_town_player_house_1f", Assert.Single(back.Warps).To);
        Assert.Equal((8, 1), (back.Triggers[0].Width, back.Triggers[0].Depth));
        Assert.Null(back.Objects[0].Trainer);
        Assert.Null(back.Encounters);
    }
}

/// <summary>The vocabulary of the imported world's tiles.</summary>
public class TileBehaviorTests
{
    [Fact]
    public void EveryNamedBehaviourSaysWhatItIs()
    {
        foreach (TileBehavior behaviour in Enum.GetValues<TileBehavior>())
        {
            Assert.True(TileBehaviors.IsKnown((byte)behaviour));
            string meaning = TileBehaviors.Meaning(behaviour);
            Assert.False(string.IsNullOrWhiteSpace(meaning));
            Assert.EndsWith(".", meaning);
        }
        Assert.False(TileBehaviors.IsKnown(0x01));
        Assert.Equal("Not used by any map.", TileBehaviors.Meaning((TileBehavior)0x01));
    }

    [Fact]
    public void TheValuesArePlatinumsOwn()
    {
        Assert.Equal(0x02, (int)TileBehavior.TallGrass);
        Assert.Equal(0x15, (int)TileBehavior.Sea);
        Assert.Equal(0x3B, (int)TileBehavior.LedgeSouth);
        Assert.Equal(0x69, (int)TileBehavior.Door);
        Assert.Equal(0x6F, (int)TileBehavior.ExitSouth);
        Assert.Equal(0xA8, (int)TileBehavior.ShallowSnow);
        Assert.Equal(0xE5, (int)TileBehavior.ShopShelf);
    }

    [Fact]
    public void WildPokemonAndSurfingFollowTheOriginalsTable()
    {
        Assert.True(TileBehaviors.HasEncounters(TileBehavior.TallGrass));
        Assert.True(TileBehaviors.HasEncounters(TileBehavior.CaveFloor));
        Assert.True(TileBehaviors.HasEncounters(TileBehavior.Sea));
        Assert.True(TileBehaviors.HasEncounters(TileBehavior.MarshGrass));
        Assert.False(TileBehaviors.HasEncounters(TileBehavior.MountainFloor));
        Assert.False(TileBehaviors.HasEncounters(TileBehavior.Waterfall));
        Assert.False(TileBehaviors.HasEncounters(TileBehavior.None));

        Assert.True(TileBehaviors.IsSurfable(TileBehavior.Sea));
        Assert.True(TileBehaviors.IsSurfable(TileBehavior.River));
        Assert.True(TileBehaviors.IsSurfable(TileBehavior.Waterfall));
        Assert.True(TileBehaviors.IsSurfable(TileBehavior.BridgeOverWater));
        Assert.False(TileBehaviors.IsSurfable(TileBehavior.Puddle));
        Assert.False(TileBehaviors.IsSurfable(TileBehavior.ShallowWater));
    }
}

/// <summary>
/// Black 2's reader (plan 22 · U1), on small files written here in the decompilation's own formats: a terrain grid
/// and its splits, a matrix with its zones, a zone's header, a binary glTF whose JSON chunk alone is read, and the
/// table of tile classes. No test needs the decompilation.
/// </summary>
public class Bw2ImportTests
{
    // A flat plane (slope 0) and one rising a tile to the south over a tile (slope 1), with two offsets: nought and a tile up
    private const string HeightSource = """
        static const fx16 MAP_HEIGHT_SLOPE_TABLE[] = {
            0, 0x1000, 0,
            0, 0x1000, 0x100,
        };
        static const fx32 MAP_HEIGHT_OFFSET_TABLE[] = {
            0,
            -0x10000,
        };
        """;

    [Fact]
    public void TheHeightTablesAreReadFromTheirCSource()
    {
        var heights = Bw2Heights.Parse(HeightSource);
        Assert.Equal(new short[] { 0, 0x1000, 0, 0, 0x1000, 0x100 }, heights.Slopes);
        Assert.Equal(new[] { 0, -0x10000 }, heights.Offsets);
        // The third value turned round, as the original sets it
        Assert.Equal((0, 0x1000, -0x100), heights.Normal(1));
        // A slope past the table's end is level
        Assert.Equal((0, 4096, 0), heights.Normal(9));
        // -(offset + n.x·x + n.z·z) / n.y: a tile (sixteen units) up for the offset of -16 in fx32
        Assert.Equal(16f, Bw2Heights.HeightOn(heights.Normal(0), heights.Offset(1), 5, -7));
        // The slope rises a sixteenth of a unit for each unit south
        Assert.Equal(1f, Bw2Heights.HeightOn(heights.Normal(1), 0, 0, 16), 3);
    }

    [Fact]
    public void ABlack2TerrainGridIsReadWithItsClassesFlagsAndHeights()
    {
        var heights = Bw2Heights.Parse(HeightSource);
        // Three tiles in a row: a whole tile on the flat plane a tile up, blocked tall grass (class 4 with flag 1);
        // a tile cut in two (shape 2) whose planes are the ground and a tile up; and a class of no tile, blocked
        string csv = string.Join('\n',
            "x,z,shape,slope,offset,tile_type,diagonal",
            "0,0,0,0,1,65540,0",
            "1,0,2,0,0,4,1",
            "2,0,0,0,0,255,0");
        string splits = string.Join('\n', "index,slope0,slope1,offset0,offset1", "0,0,0,0,1");
        var grid = Bw2Terrain.Read(csv, splits, heights);

        Assert.Equal(3, grid.Width);
        Assert.Equal(1, grid.Height);
        Assert.False(grid.RawPlanes);

        var grass = grid.At(0, 0);
        Assert.Equal(0x04, grass.Class);
        Assert.Equal(1, grass.Flags);
        Assert.True(grass.Blocked);
        Assert.Equal(1f, grass.Height, 3);
        Assert.Equal(TileBehavior.TallGrass, Bw2Behaviours.Of(grass.Class));

        // The middle of a split tile lies on its cut: the mean of the two planes
        var split = grid.At(1, 0);
        Assert.False(split.Blocked);
        Assert.Equal(0.5f, split.Height, 3);

        Assert.True(grid.At(2, 0).Blocked);
        Assert.Equal(0f, grid.At(2, 0).Height, 3);
    }

    [Fact]
    public void ABlack2TerrainGridThatWritesItsPlanesOutInFullIsReadToo()
    {
        var heights = Bw2Heights.Parse(HeightSource);
        // Two planes written out, both level: one at the ground and one two tiles up, so the middle is a tile up
        string csv = string.Join('\n',
            "x,z,normal0_x,normal0_y,normal0_z,offset0,normal1_x,normal1_y,normal1_z,offset1,tile_type",
            "0,0,0,4096,0,0,0,4096,0,-131072,10");
        var grid = Bw2Terrain.Read(csv, null, heights);
        Assert.True(grid.RawPlanes);
        Assert.Equal(0x0A, grid.At(0, 0).Class);
        Assert.Equal(1f, grid.At(0, 0).Height, 3);
        Assert.Equal(TileBehavior.CaveFloor, Bw2Behaviours.Of(grid.At(0, 0).Class));
    }

    [Fact]
    public void ABlack2MatrixFillsTheZonesOfItsJoinsFromTheNearestChunk()
    {
        // Two rows of chunks, the second shorter and with a hole; zones only for the first and the third chunk
        const string json = """
            {
              "maps": [["MAP_MODEL_A", "MAP_MODEL_B", "MAP_MODEL_C"], [null, "MAP_MODEL_D"]],
              "zones": [["ZONE_WEST", null, "ZONE_EAST"], [null, null, null]]
            }
            """;
        var matrix = Bw2Matrix.Parse("MAP_MATRIX_TEST", json);
        Assert.Equal(3, matrix.Width);
        Assert.Equal(2, matrix.Height);
        Assert.True(matrix.HasZones);
        Assert.Equal("MAP_MODEL_D", matrix.ModelAt(1, 1));
        Assert.Null(matrix.ModelAt(2, 1));   // the short row's end
        Assert.Null(matrix.ModelAt(0, 1));
        Assert.Null(matrix.ZoneAt(1, 0));

        // The breadth-first fill: the north row's middle from the west (the first in the queue), the chunk under it from that
        Assert.Equal(2, matrix.FillZones());
        Assert.Equal(2, matrix.ZonesFilled);
        Assert.Equal("ZONE_WEST", matrix.ZoneAt(1, 0));
        Assert.Equal("ZONE_WEST", matrix.ZoneAt(1, 1));
        Assert.Equal("ZONE_EAST", matrix.ZoneAt(2, 0));
        // No chunk, no zone
        Assert.Null(matrix.ZoneAt(0, 1));
        Assert.Null(matrix.ZoneAt(2, 1));
        Assert.Equal(new[] { "MAP_MODEL_A", "MAP_MODEL_B", "MAP_MODEL_C", "MAP_MODEL_D" }, matrix.Models.ToArray());

        // A matrix of a place of its own has no zones of its own, and nothing to fill
        var room = Bw2Matrix.Parse("MAP_MATRIX_ROOM", """{ "maps": [["MAP_MODEL_ROOM"]] }""");
        Assert.False(room.HasZones);
        Assert.Equal(0, room.FillZones());
        Assert.Null(room.ZoneAt(0, 0));

        Assert.Equal("overworld_2_map03_20", Bw2Matrix.Folder("MAP_MODEL_OVERWORLD_2_MAP03_20"));
        Assert.Equal("MAP_MATRIX_CASTELIA_CITY", Bw2Matrix.Constant("castelia_city"));
    }

    [Fact]
    public void ABlack2ZoneHeaderIsRead()
    {
        const string json = """
            {
              "map_type": 1, "matrix": "MAP_MATRIX_OVERWORLD", "area": "AREA_ASPERTIA", "place": "PLACE_ASPERTIA_CITY",
              "parent": "ZONE_ASPERTIA_CITY", "bgm": { "spring": "SEQ_BGM_T_01", "summer": "SEQ_BGM_T_02" },
              "encounters": "ENCOUNTER_NONE", "weather": 3, "camera": 2, "battle_bg": 5,
              "cycling": true, "escape_rope": false, "fly_from": true, "fly": { "x": 10, "y": 0, "z": 22 }
            }
            """;
        var zone = Bw2Zone.Parse("aspertia_city", 42, json);
        Assert.Equal("ZONE_ASPERTIA_CITY", zone.Constant);
        Assert.Equal("ASPERTIA CITY", zone.Name);
        Assert.Equal(42, zone.Id);
        Assert.Equal(1, zone.MapType);
        Assert.Equal("MAP_MATRIX_OVERWORLD", zone.Matrix);
        Assert.Equal("AREA_ASPERTIA", zone.Area);
        // A song that changes with the season is its spring's
        Assert.Equal("SEQ_BGM_T_01", zone.Music);
        Assert.Equal("ENCOUNTER_NONE", zone.Encounters);
        Assert.Equal(3, zone.Weather);
        Assert.Equal(2, zone.Camera);
        Assert.Equal(5, zone.BattleBackground);
        Assert.True(zone.Cycling);
        Assert.False(zone.EscapeRope);
        Assert.True(zone.FlyFrom);
        Assert.Equal((10, 0, 22), zone.Fly);

        // A plain song, no place's words, nothing else
        var room = Bw2Zone.Parse("pokecenter_1f", 7, """{ "matrix": "MAP_MATRIX_ROOM", "bgm": "SEQ_BGM_PC" }""");
        Assert.Equal("SEQ_BGM_PC", room.Music);
        Assert.Equal("POKECENTER 1F", room.Name);
        Assert.Null(room.Fly);
        Assert.False(room.Cycling);

        // The lists the constants are numbered by: from the last "= N" on, comments left out
        var constants = Bw2Constants.Read("# a list\nZONE_A\nZONE_B = 5\nZONE_C\n");
        Assert.Equal(0, constants["ZONE_A"]);
        Assert.Equal(5, constants["ZONE_B"]);
        Assert.Equal(6, constants["ZONE_C"]);
    }

    // A binary glTF of a JSON chunk and a binary chunk of whatever is given
    private static byte[] Glb(string json, byte[] binary, int binaryLengthClaimed)
    {
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        int padded = (jsonBytes.Length + 3) & ~3;
        var file = new MemoryStream();
        void U32(uint v) { Span<byte> b = stackalloc byte[4]; BinaryPrimitives.WriteUInt32LittleEndian(b, v); file.Write(b); }
        U32(0x46546C67);                                   // "glTF"
        U32(2);
        U32((uint)(12 + 8 + padded + 8 + binaryLengthClaimed));
        U32((uint)padded);
        U32(0x4E4F534A);                                   // "JSON"
        file.Write(jsonBytes);
        for (int i = jsonBytes.Length; i < padded; i++) file.WriteByte((byte)' ');
        U32((uint)binaryLengthClaimed);
        U32(0x004E4942);                                   // "BIN"
        file.Write(binary);
        return file.ToArray();
    }

    [Fact]
    public void ABlack2GltfGivesItsPropsAndTexturesFromItsJsonChunkAlone()
    {
        float half = MathF.Sqrt(0.5f);
        string json = $$"""
            {
              "asset": { "version": "2.0" },
              "images": [ { "uri": "textures/gake_01.png" }, { "uri": "michi.png" }, { "uri": "textures\\gake_01.png" } ],
              "nodes": [
                { "name": "terrain", "mesh": 0 },
                { "name": "bw2_prop_0003_id_00042", "translation": [16, 0, -32], "rotation": [0, {{half.ToString(System.Globalization.CultureInfo.InvariantCulture)}}, 0, {{half.ToString(System.Globalization.CultureInfo.InvariantCulture)}}] },
                { "name": "bw2_prop_0001_id_7", "translation": [-8, 4, 0] },
                { "name": "bw2_prop_x_id_9" }
              ]
            }
            """;
        // The binary chunk claims far more than the file holds: a reader that went past the JSON would fail
        var glb = Glb(json, new byte[] { 1, 2, 3, 4 }, binaryLengthClaimed: 1 << 20);

        var model = Bw2Model.Read(glb);
        Assert.Equal(new[] { "gake_01", "michi" }, model.Textures);
        Assert.Equal(2, model.Props.Count);

        var turned = model.Props[0];
        Assert.Equal(3, turned.Index);
        Assert.Equal(42, turned.Resource);
        Assert.Equal(new Vector3(16, 0, -32), turned.Position);
        Assert.Equal(90f, turned.Turn, 1);

        var plain = model.Props[1];
        Assert.Equal(1, plain.Index);
        Assert.Equal(7, plain.Resource);
        Assert.Equal(new Vector3(-8, 4, 0), plain.Position);
        Assert.Equal(0f, plain.Turn);

        // From a file the same: its header and JSON chunk are read and nothing after them
        string path = Path.Combine(Path.GetTempPath(), $"bw2-{Guid.NewGuid():N}.glb");
        try
        {
            File.WriteAllBytes(path, glb);
            var read = Bw2Model.ReadFile(path);
            Assert.Equal(model.Textures, read.Textures);
            Assert.Equal(model.Props, read.Props);
        }
        finally { File.Delete(path); }

        // Anything that isn't a binary glTF is refused
        Assert.Throws<InvalidDataException>(() => Bw2Model.Read(Encoding.UTF8.GetBytes("{ \"asset\": {} } and more bytes")));
    }

    // The classes src/ov036/map_attr.c and field_water_current.c ask about, each of which must have a row
    private static readonly int[] ClassesTheFieldCodeNames =
    {
        0x00, 0x01, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0E, 0x0F, 0x10, 0x18, 0x19, 0x1C, 0x1D,
        0x20, 0x21, 0x22, 0x23, 0x24, 0x30, 0x3D, 0x3E, 0x3F, 0x40, 0x41, 0x42, 0x43, 0x44,
        0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x72, 0x73, 0x74, 0x75, 0x7C, 0x94, 0x95, 0x96, 0x97,
        0xBE, 0xBF, 0xD4, 0xD6, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF, 0xE0, 0xE1, 0xE2, 0xFE, 0xFF, 0xFFFF,
    };

    [Fact]
    public void EveryBlack2ClassTheFieldCodeNamesHasARowAndEveryValueOfItsOwnIsDescribed()
    {
        foreach (int cls in ClassesTheFieldCodeNames)
        {
            Assert.True(Bw2Behaviours.Table.ContainsKey(cls), $"class 0x{cls:X2} has no row");
            Assert.False(string.IsNullOrWhiteSpace(Bw2Behaviours.Table[cls].What));
        }
        Assert.Equal(ClassesTheFieldCodeNames.Order().ToArray(), Bw2Behaviours.Table.Keys.Order().ToArray());

        // A class in no row is plain ground, and says so
        Assert.Equal(TileBehavior.None, Bw2Behaviours.Of(0x02, out bool known));
        Assert.False(known);

        // The type's two halves
        Assert.Equal(0x41, Bw2Behaviours.ClassOf(0x10041));
        Assert.Equal(1, Bw2Behaviours.FlagsOf(0x10041));
        Assert.True(Bw2Behaviours.Blocked(0x10041));
        Assert.False(Bw2Behaviours.Blocked(0x00004));
        Assert.True(Bw2Behaviours.Blocked(0x000FF));
        Assert.True(Bw2Behaviours.Blocked(0x0FFFF));

        // The ledges and the currents by the order of field_water_current.c's moves: north, south, west, east
        Assert.Equal(TileBehavior.LedgeEast, Bw2Behaviours.Of(0x72));
        Assert.Equal(TileBehavior.LedgeSouth, Bw2Behaviours.Of(0x75));
        Assert.Equal(TileBehavior.CurrentEast, Bw2Behaviours.Of(0x94));
        Assert.Equal(TileBehavior.CurrentSouth, Bw2Behaviours.Of(0x97));

        // Black 2's own values: 0xB0 to 0xB8, each described, waiting for its session, in the document and given a look
        var own = Bw2Import.OwnValues.ToList();
        Assert.Equal(Enumerable.Range(0xB0, 9).ToArray(), own.Select(b => (int)b).ToArray());
        string doc = File.ReadAllText(Path.Combine(Repository.Root, "docs", "tile-behaviours.md"));
        string section = DocSections.Of(doc, Bw2Import.SectionTitle);
        Assert.False(string.IsNullOrEmpty(section), "docs/tile-behaviours.md has no Black 2 section");
        foreach (var behaviour in own)
        {
            Assert.True(TileBehaviors.IsKnown((byte)behaviour), $"{behaviour} has no meaning");
            Assert.Equal(BehaviourSupport.Waiting, TileBehaviors.InGame(behaviour).Support);
            Assert.Contains($"| `0x{(int)behaviour:X2}` | `{behaviour}` |", section);
            // Each has a look but the boulder's hole, whose look is the ground round it
            if (behaviour != TileBehavior.StrengthHole) Assert.NotNull(Bw2Cover.OfBehaviour(behaviour));
        }
    }

    [Fact]
    public void Black2sTextureNamesAreSortedBeforePlatinums()
    {
        Assert.Equal(TerrainCover.Path, Bw2Cover.OfTexture("gake_michi_01", out bool known));
        Assert.True(known);
        Assert.Equal(TerrainCover.Cliff, Bw2Cover.OfTexture("gake_02", out _));
        Assert.Equal(TerrainCover.Tree, Bw2Cover.OfTexture("ki03", out _));
        // A plain kusa texture is the lawn: the long grass is nagakusa, and grass Pokémon live in is told by its behaviour (plan 22 · U2)
        Assert.Equal(TerrainCover.Grass, Bw2Cover.OfTexture("kusa_01", out _));
        Assert.Equal(TerrainCover.TallGrass, Bw2Cover.OfTexture("nagakusa_01", out _));
        Assert.Null(Bw2Cover.OfTexture("kage_01", out known));
        Assert.True(known);
        Assert.Equal(TerrainCover.Water, Bw2Cover.OfTile(Bw2Behaviours.Of(0x3F)));
        Assert.Equal(TerrainCover.TallGrass, Bw2Cover.OfTile(Bw2Behaviours.Of(0x06)));
        Assert.Equal(TerrainCover.Unknown, Bw2Cover.OfTile(TileBehavior.None));
    }

    // ---------------------------------------------------------------- plan 22 · U2: where the textures lie

    [Fact]
    public void AChunksTrianglesSayWhichTextureLiesOverEachTileAndWhatRisesAboveIt()
    {
        // Two tiles side by side (a grid of 2 × 1, its middle the model's origin, sixteen units to a tile): a level quad of
        // path over both at the walking height, and a level square of leaves forty units up over the east tile alone
        var bin = new MemoryStream();
        void F(float v) { Span<byte> b = stackalloc byte[4]; BinaryPrimitives.WriteSingleLittleEndian(b, v); bin.Write(b); }
        void S(ushort v) { Span<byte> b = stackalloc byte[2]; BinaryPrimitives.WriteUInt16LittleEndian(b, v); bin.Write(b); }
        foreach (var (x, y, z) in new[] { (-16f, 0f, -8f), (16f, 0f, -8f), (16f, 0f, 8f), (-16f, 0f, 8f), (0f, 40f, -8f), (16f, 40f, -8f), (16f, 40f, 8f), (0f, 40f, 8f) })
        { F(x); F(y); F(z); }
        foreach (ushort i in new ushort[] { 0, 1, 2, 0, 2, 3, 0, 1, 2, 0, 2, 3 }) S(i);
        var binary = bin.ToArray();
        const string json = """
            {
              "asset": { "version": "2.0" },
              "images": [ { "uri": "textures/michi_01.png" }, { "uri": "ki03.png" } ],
              "textures": [ { "source": 0 }, { "source": 1 } ],
              "materials": [ { "pbrMetallicRoughness": { "baseColorTexture": { "index": 0 } } }, { "pbrMetallicRoughness": { "baseColorTexture": { "index": 1 } } } ],
              "meshes": [ { "primitives": [ { "attributes": { "POSITION": 0 }, "indices": 1, "material": 0 }, { "attributes": { "POSITION": 2 }, "indices": 3, "material": 1 } ] } ],
              "nodes": [ { "name": "terrain", "mesh": 0 } ],
              "buffers": [ { "byteLength": 120 } ],
              "bufferViews": [ { "buffer": 0, "byteOffset": 0, "byteLength": 96 }, { "buffer": 0, "byteOffset": 96, "byteLength": 24 } ],
              "accessors": [
                { "bufferView": 0, "componentType": 5126, "count": 4, "type": "VEC3" },
                { "bufferView": 1, "componentType": 5123, "count": 6, "type": "SCALAR" },
                { "bufferView": 0, "byteOffset": 48, "componentType": 5126, "count": 4, "type": "VEC3" },
                { "bufferView": 1, "byteOffset": 12, "componentType": 5123, "count": 6, "type": "SCALAR" }
              ]
            }
            """;
        var triangles = Bw2Geometry.Read(Glb(json, binary, binary.Length));
        Assert.Equal(4, triangles.Count);
        Assert.Equal(new[] { "michi_01", "michi_01", "ki03", "ki03" }, triangles.Select(t => t.Texture).ToArray());
        Assert.Equal(new Vector3(0, 40, 8), triangles[3].C);

        var layers = Bw2Geometry.LayersOver(triangles, 2, 1, (_, _, _) => 0f);
        Assert.Equal(new[] { "michi_01", "michi_01" }, layers.Ground);
        Assert.Equal(new string?[] { null, "ki03" }, layers.Above);
        Assert.Equal(40f, layers.AboveHeight[1], 3);
        // With nothing known of the walking surface, the topmost is the ground
        Assert.Equal("ki03", Bw2Geometry.LayersOver(triangles, 2, 1, (_, _, _) => null).Ground[1]);
        // A wall covers no tile
        var wall = new Bw2Geometry.Triangle("kabe", new Vector3(-16, 0, 0), new Vector3(16, 0, 0), new Vector3(16, 16, 0));
        Assert.All(Bw2Geometry.LayersOver(new[] { wall }, 2, 1, (_, _, _) => 0f).Ground, Assert.Null);
    }

    // ---------------------------------------------------------------- plan 22 · U2: events, encounters, trainers

    // A zone's events in the decompilation's shape: a person, one on a rail (no grid), a warp whose grid is in sixteenths of
    // a tile, a sign whose grid is x, the row and the height, and a trigger
    private const string EventsSource = """
        {
          "npcs": [
            { "id": 3, "model": "OBJ_CODE_YOUNGSTER", "movement": 3, "type": 0, "flag": "EVENT_FLAG_0x02e9", "script": 3012,
              "direction": "DIR_LEFT", "params": [ 0, 2, 0 ], "area": { "width": 2, "height": 1 }, "grid": { "x": 12, "z": 7, "y": 0 } },
            { "id": 4, "model": "OBJ_CODE_HIKER", "movement": 0, "script": 1, "rail": { "line": 2 } }
          ],
          "warps": [ { "zone": "ZONE_ASPERTIA_GATE", "warp": 1, "grid": { "x": 160, "z": 48, "y": 0, "width": 2, "height": 1 } } ],
          "bg_events": [ { "script": 8004, "condition": 2, "grid": { "x": 5, "y": 9, "z": 1 } } ],
          "triggers": [ { "script": 20, "var": "EVENT_WORK_0x40a1", "value": 1, "grid": { "x": 3, "z": 4, "width": 0, "height": 2 } } ]
        }
        """;

    [Fact]
    public void AZonesEventsAreReadWithTheirTilesAndTheRailsLeftOut()
    {
        var events = Bw2Events.Parse(EventsSource);
        var person = Assert.Single(events.Npcs);
        Assert.Equal((3, "youngster", 12, 7), (person.Id, person.Looks, person.X, person.Z));
        Assert.Equal(("EVENT_FLAG_0x02e9", 3012, "DIR_LEFT", 2, 1), (person.Flag, person.Script, person.Direction, person.RangeX, person.RangeZ));
        // A warp's grid is in sixteenths of a tile
        var warp = Assert.Single(events.Warps);
        Assert.Equal(("ZONE_ASPERTIA_GATE", 1, 10, 3, 2, 1), (warp.Zone, warp.To, warp.X, warp.Z, warp.Width, warp.Height));
        // A sign's row is the grid's y, and a hidden item's condition is 2
        var sign = Assert.Single(events.Signs);
        Assert.Equal((8004, Bw2Events.Hidden, 5, 9, 1), (sign.Script, sign.Condition, sign.X, sign.Z, sign.Y));
        var trigger = Assert.Single(events.Triggers);
        Assert.Equal(("EVENT_WORK_0x40a1", 1, 3, 4, 1, 2), (trigger.Var, trigger.Value, trigger.X, trigger.Z, trigger.Width, trigger.Depth));
        Assert.Empty(Bw2Events.Parse("{}").Warps);
    }

    [Fact]
    public void UnovasFlagsAndVariablesCarryTheRegionsName()
    {
        Assert.Equal("FLAG_UNOVA_0x02e9", Bw2Events.Flag("EVENT_FLAG_0x02e9"));
        Assert.Equal("FLAG_UNOVA_0x0bc4", Bw2Events.Flag(0xbc4));
        Assert.Equal("FLAG_UNOVA_SOMETHING", Bw2Events.Flag("FLAG_SOMETHING"));
        Assert.Equal("VAR_UNOVA_0x40a1", Bw2Events.Var("EVENT_WORK_0x40a1"));
        // The movements as Platinum's words, a path of its own standing still; the facings as the area files keep them
        Assert.Equal("wander_around", Bw2Events.Movement(3));
        Assert.Equal("wander_west_and_east", Bw2Events.Movement(5));
        Assert.Equal("look_around", Bw2Events.Movement(2));
        Assert.Equal("none", Bw2Events.Movement(30));
        Assert.Equal((0, 1, 2, 3), (Bw2Events.Facing("DIR_UP"), Bw2Events.Facing("DIR_DOWN"), Bw2Events.Facing("DIR_LEFT"), Bw2Events.Facing("DIR_RIGHT")));
        // Every flag and variable the open areas' files name is Unova's own
        foreach (string file in Directory.GetFiles(GameDataFiles.PathOf(Path.Combine(World.Folder, "unova", "areas")), "*.json"))
        {
            var area = GameDataFiles.Load<WorldAreaFile>(Path.Combine(World.Folder, "unova", "areas", Path.GetFileName(file)));
            foreach (var o in area.Objects.Where(o => o.HiddenBy != null)) Assert.StartsWith("FLAG_UNOVA_", o.HiddenBy);
            foreach (var t in area.Triggers.Where(t => !string.IsNullOrEmpty(t.Variable))) Assert.StartsWith("VAR_UNOVA_", t.Variable);
        }
    }

    [Fact]
    public void TheGlobalScriptsSayWhatEachBallAndHiddenItemSets()
    {
        const string source = """
            Script_1:
                WorkSetConst 0x800c, 17
                WorkSetConst 0x800d, 1
                CallStd 0
            Script_2:
                End
            Script_3:
                WorkSet 0x8000, 213
                WorkSet 0x8001, 1
                WorkSet 0x8002, 3012
            """;
        var scripts = Bw2Events.GlobalScripts(source);
        Assert.Equal(new[] { 0, 2 }, scripts.Keys.OrderBy(k => k).ToArray());
        Assert.Equal(17, scripts[0][0x800c]);
        Assert.Equal(1, scripts[0][0x800d]);
        Assert.Equal(3012, scripts[2][0x8002]);
    }

    // A zone's tables: spring with a grass table Black 2 and White 2 share and a dark grass one they don't; summer with
    // its own grass
    private const string EncounterSource = """
        {
          "spring": {
            "rates": { "grass": 1, "dark_grass": 2, "surf": 0 },
            "grass": [ [ "SPECIES_PATRAT", 2, 2 ], [ "SPECIES_PURRLOIN", 2, 4 ] ],
            "dark_grass": { "black2": [ [ "SPECIES_SEWADDLE", 5, 5 ] ], "white2": [ [ "SPECIES_VENIPEDE", 5, 5 ] ] }
          },
          "summer": { "rates": { "grass": 2 }, "grass": [ [ "SPECIES_SUNKERN", 3, 3 ] ] },
          "autumn": { "rates": { "grass": 1 }, "grass": [ [ "SPECIES_PATRAT", 2, 2 ] ] }
        }
        """;

    [Fact]
    public void AZonesTablesAreReadBySeasonAndBlack2sOwnWhereTheVersionsDiffer()
    {
        var tables = Bw2Encounters.Parse(EncounterSource);
        Assert.True(tables.Seasonal);
        Assert.Equal(new[] { "spring", "summer", "autumn" }, tables.BySeason.Select(s => s.Name).ToArray());
        var spring = tables.BySeason[0];
        Assert.Equal(1, spring.Rates["grass"]);
        Assert.Equal("SPECIES_SEWADDLE", Assert.Single(spring.Tables["dark_grass"]).Species);
        var problems = new List<string>();
        var grass = Bw2Encounters.Slots(spring, "grass", range: false, weights: null, "test", problems)!;
        Assert.Empty(problems);
        Assert.Equal(("Patrat", 2, (int?)null), (grass[0].Species, grass[0].Level, grass[0].MaxLevel));
        Assert.Equal(("Purrloin", 2, (int?)4), (grass[1].Species, grass[1].Level, grass[1].MaxLevel));
        Assert.Null(Bw2Encounters.Slots(spring, "surf", range: true, Bw2Encounters.RodWeights, "test", problems));
        var rods = Bw2Encounters.Slots(spring, "grass", range: true, Bw2Encounters.RodWeights, "test", problems)!;
        Assert.Equal((60, 30), (rods[0].Weight, rods[1].Weight));
        Assert.Equal(2, rods[0].MaxLevel);
        // A species the data doesn't have is told of
        var none = new Bw2Encounters.Season { Name = "spring" };
        none.Tables["grass"] = new() { ("SPECIES_NOBODY", 1, 1) };
        Bw2Encounters.Slots(none, "grass", false, null, "here", problems);
        Assert.Single(problems);
        // A zone the same all year is spring alone
        Assert.False(Bw2Encounters.Parse("""{ "all_year": { "rates": { "grass": 1 }, "grass": [ [ "SPECIES_PATRAT", 2, 2 ] ] } }""").Seasonal);
        // The game's rate is the row's largest number; none for a setting of nought or past the table
        Assert.Equal(13, Bw2Encounters.Rate(1));
        Assert.Equal(19, Bw2Encounters.Rate(2));
        Assert.Null(Bw2Encounters.Rate(0));
        Assert.Null(Bw2Encounters.Rate(13));
        Assert.Equal(100, Bw2Encounters.RodWeights.Sum());
    }

    [Fact]
    public void ASeasonsTablesGoThroughTheAreasFileAndBack()
    {
        var area = new WorldAreaFile
        {
            Key = "unova_route_20", Name = "Route 20",
            Land = new() { new AreaEncounter { Species = "Pidove", Level = 2 } }, LandRate = 13,
            DarkGrass = new() { new AreaEncounter { Species = "Venipede", Level = 3, MaxLevel = 4 } }, DarkGrassRate = 13,
            Seasons = new()
            {
                new AreaSeason
                {
                    Season = "Summer", LandRate = 19, Land = new() { new AreaEncounter { Species = "Sunkern", Level = 3 } },
                    RipplingRod = new() { new AreaEncounter { Species = "Basculin", Level = 10, MaxLevel = 20, Weight = 60 } }, RipplingRodRate = 10
                }
            }
        };
        var back = GameDataFiles.Deserialize<WorldAreaFile>(GameDataFiles.Serialize(area))!;
        Assert.Equal("Venipede", Assert.Single(back.DarkGrass!).Species);
        Assert.Equal(4, back.DarkGrass![0].MaxLevel);
        var summer = Assert.Single(back.Seasons!);
        Assert.Equal(("Summer", (int?)19, "Sunkern"), (summer.Season, summer.LandRate, summer.Land![0].Species));
        Assert.Equal((60, (int?)10), (summer.RipplingRod![0].Weight, summer.RipplingRodRate));
        Assert.Null(summer.Water);
        // An area the same all year writes no seasons at all
        Assert.DoesNotContain("seasons", GameDataFiles.Serialize(new WorldAreaFile { Key = "aspertia_city" }));
    }

    [Fact]
    public void UnovasKeysAndTrainerClassesAreOurs()
    {
        Assert.Equal("aspertia_city", Bw2World.KeyOf("ZONE_ASPERTIA_CITY"));
        Assert.Equal("unova_route_19", Bw2World.KeyOf("ZONE_ROUTE_19"));
        Assert.Equal("unova_victory_road", Bw2World.KeyOf("ZONE_VICTORY_ROAD"));
        Assert.Equal("Pokémon Ranger", Bw2World.ClassName("TRAINER_CLASS_PKMN_RANGER_M"));
        Assert.Equal("Youngster", Bw2World.ClassName("TRAINER_CLASS_YOUNGSTER"));
        // Nothing the importer writes for Unova carries the decompilation's message ids, which are made of the original's
        // words: a person's lines are their overlay's, in our own
        foreach (string folder in new[] { "areas", "overlays" })
            foreach (string file in Directory.GetFiles(GameDataFiles.PathOf(Path.Combine(World.Folder, "unova", folder)), "*.json"))
            {
                string text = File.ReadAllText(file);
                Assert.DoesNotContain("_Text_", text);
                Assert.DoesNotContain("msg_", text);
            }
        string trainers = File.ReadAllText(GameDataFiles.PathOf(Path.Combine(World.Folder, "unova", "trainers.json")));
        Assert.DoesNotContain("\"messages\"", trainers);
    }
}
