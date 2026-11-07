using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenWorldBake;
using PokemonPlatinumEngine.OpenWorld;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 32 · OW2, the baker and the terrain: a region's descriptions read strictly and baked into what the game loads,
/// on the open test world (tools/OpenWorldBake/worlds/lab: OW0's hill with its tunnel and cavern, a river with a rock
/// bridge, a pond with a stack, a ledge and two graded roads). GPU-free.
/// </summary>
public class OpenWorldBakeTests
{
    public static string LabFolder => Path.Combine(Repository.Root, "tools", "OpenWorldBake", "worlds", "lab");

    static readonly Lazy<(RegionDescription Description, TerrainWorld Land)> lab = new(() =>
    {
        var description = RegionDescription.Read(LabFolder);
        return (description, description.Land());
    });

    // Every file of the lab's bake, once for the class
    static readonly Lazy<SortedDictionary<string, byte[]>> files = new(() => RegionBake.Files(lab.Value.Description));

    // The hill's four chunks, each at its three levels
    static readonly (int X, int Z)[] Hill = { (-1, -1), (0, -1), (-1, 0), (0, 0) };
    static readonly Lazy<Dictionary<(int, int), ChunkMesh[]>> hill = new(() =>
        Hill.ToDictionary(c => c, c => RegionBake.Meshes(lab.Value.Land, lab.Value.Description.World, c.X, c.Z)));

    static string Scratch()
    {
        string dir = Path.Combine(Path.GetTempPath(), "openworldbake-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void AChunkBakedTwiceIsTheSameToTheByte()
    {
        // The chunk north-west of the hill's middle (the tunnel along its south border, the cavern and the hill's
        // slope all in it), from two readings of the descriptions, at each level
        var first = RegionDescription.Read(LabFolder);
        var second = RegionDescription.Read(LabFolder);
        var a = RegionBake.Chunk(first.Land(), first.World, -1, -1);
        var b = RegionBake.Chunk(second.Land(), second.World, -1, -1);
        Assert.Equal(3, a.Length);
        Assert.True(a[0].TriangleCount > 1000);
        for (int level = 0; level < 3; level++) Assert.Equal(a[level].Bytes(), b[level].Bytes());
        // And the same as the whole region's bake made side by side
        for (int level = 0; level < 3; level++) Assert.Equal(files.Value[BakedChunk.FileName(-1, -1, level)], a[level].Bytes());
    }

    [Fact]
    public void TheGameReadsWhatTheBakerWrites()
    {
        string dir = Scratch();
        try
        {
            RegionBake.Write(dir, files.Value);
            var region = BakedRegion.Open(dir);
            Assert.Equal("lab", region.Index.Region);
            Assert.Equal(16, region.Chunks.Count());
            Assert.Equal(new[] { 0, 60, 300 }, region.Index.LevelErrorsMm);
            Assert.Equal(lab.Value.Description.Source, region.Index.Source);
            foreach (var (x, z) in region.Chunks)
                for (int level = 0; level < region.Levels; level++)
                {
                    var chunk = region.Chunk(x, z, level)!;
                    Assert.Equal((x, z, level), (chunk.ChunkX, chunk.ChunkZ, chunk.Level));
                    Assert.Equal(files.Value[BakedChunk.FileName(x, z, level)], chunk.Bytes());
                    // Every vertex of the chunk's own ground stands in its columns or the row it shares east and south
                    for (int t = 0; t < chunk.GroundTriangles * 3; t++)
                    {
                        var p = chunk.Vertex(chunk.Indices[t]);
                        Assert.InRange(p.X, x * OpenMap.ChunkSide, (x + 1) * OpenMap.ChunkSide + 500);
                        Assert.InRange(p.Z, z * OpenMap.ChunkSide, (z + 1) * OpenMap.ChunkSide + 500);
                    }
                }
            Assert.Null(region.Chunk(5, 5, 0));
            // A map is given its land
            var map = new OpenMap("OpenLab", "lab") { Land = region };
            Assert.Same(region, map.Land);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void ABakedChunkRoundTripsAndAnotherFormatIsRefused()
    {
        var water = Enumerable.Repeat(BakedChunk.NoWater, BakedChunk.WaterCells * BakedChunk.WaterCells).ToArray();
        water[3] = -1500; water[4] = -1500; water[200] = 2_000_000;
        var chunk = new BakedChunk
        {
            ChunkX = -3, ChunkZ = 7, Level = 2, ErrorMm = 300,
            Positions = new[] { -96_000, -1200, 224_000, -95_500, 40_000, 230_000, -70_000, 0, 255_999 },
            Normals = new sbyte[] { 0, 127, 0, -90, 90, 0, 1, 2, -3 },
            Covers = new[] { OpenCover.Grass, OpenCover.Mud, OpenCover.Snow },
            Indices = new[] { 0, 2, 1 }, GroundTriangles = 1, ApronTriangles = 0, Water = water
        };
        var bytes = chunk.Bytes();
        var back = BakedChunk.Read(new MemoryStream(bytes));
        Assert.Equal(chunk.Positions, back.Positions);
        Assert.Equal(chunk.Normals, back.Normals);
        Assert.Equal(chunk.Covers, back.Covers);
        Assert.Equal(chunk.Indices, back.Indices);
        Assert.Equal(chunk.Water, back.Water);
        Assert.Equal(bytes, back.Bytes());
        bytes[4] = BakedChunk.Format + 1;
        Assert.Throws<InvalidDataException>(() => BakedChunk.Read(new MemoryStream(bytes)));
    }

    [Fact]
    public void CheckFindsWhereTheBakedFilesDiffer()
    {
        string dir = Scratch();
        try
        {
            RegionBake.Write(dir, files.Value);
            Assert.Empty(RegionBake.Differences(dir, files.Value));
            // A byte changed, a file gone and a chunk the region doesn't have are each found
            string changed = Path.Combine(dir, BakedChunk.FileName(0, 0, 1));
            var bytes = File.ReadAllBytes(changed);
            bytes[^2] ^= 1;
            File.WriteAllBytes(changed, bytes);
            File.Delete(Path.Combine(dir, BakedChunk.FileName(1, 1, 2)));
            File.WriteAllBytes(Path.Combine(dir, BakedChunk.FileName(9, 9, 0)), new byte[] { 1 });
            var found = RegionBake.Differences(dir, files.Value);
            Assert.Equal(3, found.Count);
            Assert.Contains(found, f => f.StartsWith("1/0_0.chunk") && f.Contains("differs"));
            Assert.Contains(found, f => f.StartsWith("2/1_1.chunk") && f.Contains("not baked"));
            Assert.Contains(found, f => f.StartsWith("0/9_9.chunk"));
            // Writing again puts it right, the stray file taken away
            RegionBake.Write(dir, files.Value);
            Assert.Empty(RegionBake.Differences(dir, files.Value));

            // A description changed: the hill a metre higher bakes the hill's chunks otherwise
            string copy = CopyOfLab();
            try
            {
                string terrain = Path.Combine(copy, "terrain", "ground.json");
                File.WriteAllText(terrain, File.ReadAllText(terrain).Replace("\"radius\": 30, \"rise\": 12", "\"radius\": 30, \"rise\": 13"));
                var changedDescription = RegionDescription.Read(copy);
                Assert.NotEqual(lab.Value.Description.Source, changedDescription.Source);
                var rebaked = RegionBake.Files(changedDescription, (x, z) => x == 0 && z == 0);
                var differences = RegionBake.Differences(dir, rebaked, partial: true);
                Assert.Contains(differences, d => d.StartsWith("0/0_0.chunk"));
                Assert.Contains(differences, d => d.StartsWith("index.json"));
            }
            finally { Directory.Delete(copy, recursive: true); }
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    static string CopyOfLab()
    {
        string copy = Scratch();
        foreach (string file in RegionDescription.FilesIn(LabFolder))
        {
            string to = Path.Combine(copy, file);
            Directory.CreateDirectory(Path.GetDirectoryName(to)!);
            File.Copy(Path.Combine(LabFolder, file), to);
        }
        return copy;
    }

    [Theory]
    [InlineData("world.json", "\"chunks\":", "\"chunkz\": [0], \"chunks\":", "chunkz")]
    [InlineData("world.json", "\"region\": \"lab\",", "\"region\": \"lab\", \"chunkSize\": 16,", "chunkSize")]
    [InlineData("terrain/ground.json", "\"kind\": \"dome\", \"name\": \"the hill\"", "\"kind\": \"dune\", \"name\": \"the hill\"", "dune")]
    [InlineData("terrain/ground.json", "\"radius\": 30, \"rise\": 12", "\"radius\": 30", "rise")]
    [InlineData("carve/hill.json", "\"radius\": 2.4, \"floor\": 0", "\"radius\": 2.4, \"flor\": 0", "flor")]
    [InlineData("water.json", "\"level\": -1.0", "\"level\": \"low\"", "level")]
    [InlineData("roads.json", "[-50, 8]", "[-50, 8, 1, 2]", "points[1]")]
    public void TheDescriptionsAreReadStrictly(string file, string from, string to, string named)
    {
        // A key no field has, a field left out, a value of the wrong type or a shape the format can't hold stops the
        // bake, naming the file and the key
        string copy = CopyOfLab();
        try
        {
            string path = Path.Combine(copy, file);
            string text = File.ReadAllText(path);
            Assert.Contains(from, text);
            File.WriteAllText(path, text.Replace(from, to));
            var e = Assert.Throws<InvalidDataException>(() => RegionDescription.Read(copy));
            Assert.StartsWith(file, e.Message);
            Assert.Contains(named, e.Message);
        }
        finally { Directory.Delete(copy, recursive: true); }
    }

    // Every surface a column meets at a level, highest first, with which way each faces
    static List<(double Y, double Up)> Column(ChunkMesh chunk, double x, double z)
    {
        var mesh = RegionPictures.Surface(new[] { chunk });
        return mesh.Column(x, z).Select(c => (c.Y, mesh.Triangles[c.Triangle].Normal.Y)).ToList();
    }

    [Theory]
    [InlineData(6, -3)]       // over the cavern
    [InlineData(-15, -0.5)]   // over the tunnel, in the hill's west flank
    public void ACarveMakesAHoleTheMeshHasBothSidesOf(double x, double z)
    {
        // A column down through the hill meets its top facing up, the carve's roof facing down and its floor facing
        // up, and nothing under the floor: at every level, so a thinned level keeps the hole (decision 16)
        var land = lab.Value.Land;
        var chunk = hill.Value[(x < 0 ? -1 : 0, z < 0 ? -1 : 0)];
        foreach (var level in chunk)
        {
            var column = Column(level, x, z);
            Assert.True(column.Count == 3, $"level {level.Lod}: {column.Count} surfaces at {x},{z}");
            Assert.True(column[0].Up > 0 && column[1].Up < 0 && column[2].Up > 0, $"level {level.Lod}: {string.Join(", ", column)}");
            Assert.InRange(column[0].Y, land.Height(x, z) - 0.35, land.Height(x, z) + 0.35);
            Assert.InRange(column[2].Y, -0.35, 0.35);   // the carves' floors are at nought
        }
    }

    [Fact]
    public void TheHillsChunksCloseRoundTheirCavesAtEveryLevel()
    {
        // The hill's four chunks together, at any level and at levels mixed, have no edge with one face but on the
        // outside of their block: the tunnel runs along the border two of them share, and is closed across it
        for (int a = 0; a < 3; a++)
            for (int b = 0; b < 3; b++)
            {
                var edges = new Dictionary<(OpenPos, OpenPos), int>();
                foreach (var c in Hill)
                {
                    // Two of the chunks at one level and two at the other, chequered
                    var chunk = hill.Value[c][((c.X + c.Z) & 1) == 0 ? a : b];
                    for (int t = 0; t < chunk.GroundTriangles; t++)
                        for (int e = 0; e < 3; e++)
                        {
                            var p = Pos(chunk, chunk.Indices[t * 3 + e]); var q = Pos(chunk, chunk.Indices[t * 3 + (e + 1) % 3]);
                            var key = Compare(p, q) < 0 ? (p, q) : (q, p);
                            edges[key] = edges.GetValueOrDefault(key) + 1;
                        }
                }
                bool Outside(OpenPos p) => p.X < -31_000 || p.X > 31_900 || p.Z < -31_000 || p.Z > 31_900;
                var open = edges.Where(e => e.Value != 2 && !(Outside(e.Key.Item1) && Outside(e.Key.Item2))).Select(e => e.Key).ToList();
                Assert.True(open.Count == 0, $"levels {a} and {b}: {open.Count} edges inside the block not two faces', the first {string.Join("; ", open.Take(3))}");
            }

        static OpenPos Pos(ChunkMesh c, int v) => new(c.Positions[v * 3], c.Positions[v * 3 + 1], c.Positions[v * 3 + 2]);
        static int Compare(OpenPos p, OpenPos q) => p.X != q.X ? p.X.CompareTo(q.X) : p.Y != q.Y ? p.Y.CompareTo(q.Y) : p.Z.CompareTo(q.Z);
    }

    [Fact]
    public void TheThinnedLevelsKeepTheBorderAndStayNearTheFinest()
    {
        foreach (var (key, levels) in hill.Value)
        {
            var finest = levels[0];
            var finestVertices = Enumerable.Range(0, finest.VertexCount).Select(v => (finest.Positions[v * 3], finest.Positions[v * 3 + 1], finest.Positions[v * 3 + 2])).ToHashSet();
            for (int l = 1; l < 3; l++)
            {
                var level = levels[l];
                // Fewer faces, every vertex one of the finest's, and none of apron or skirt
                Assert.True(level.GroundTriangles < levels[l - 1].GroundTriangles, $"chunk {key} level {l}: {level.GroundTriangles} faces, the level before {levels[l - 1].GroundTriangles}");
                Assert.Equal(0, level.ApronTriangles);
                Assert.Equal(level.GroundTriangles, level.TriangleCount);
                for (int v = 0; v < level.VertexCount; v++)
                    Assert.Contains((level.Positions[v * 3], level.Positions[v * 3 + 1], level.Positions[v * 3 + 2]), finestVertices);
                // Every vertex of the finest ground stands within reach of the level's surface
                var surface = RegionPictures.Surface(new[] { level });
                double worst = 0;
                for (int t = 0; t < finest.GroundTriangles * 3; t += 7)
                {
                    var p = finest.Vertex(finest.Indices[t]);
                    double best = double.MaxValue;
                    foreach (int s in surface.Near(p.X, p.Z, 2.5))
                    {
                        var (a, b, c, _) = surface.Triangles[s];
                        best = Math.Min(best, (Tri.ClosestPoint(p, a, b, c) - p).Length);
                    }
                    worst = Math.Max(worst, best);
                }
                double bound = RegionBake.Thinned[l - 1].MaxError * 2;
                Assert.True(worst <= bound, $"chunk {key} level {l}: a finest vertex {worst:0.000} m off the level, over {bound} m");
            }
            Assert.True(levels[2].GroundTriangles * 4 < finest.GroundTriangles, $"chunk {key}: level 2 has {levels[2].GroundTriangles} of the finest's {finest.GroundTriangles} faces");
        }
    }

    [Fact]
    public void EveryFaceOfEveryLevelFacesTheAir()
    {
        // The finest by the field (air just in front of each face away from a carve's sharp edges, rock just behind);
        // every level by its vertices' normals, which are the finest's: a thinned face turned over would disagree
        var land = lab.Value.Land;
        bool NearACarve(D3 p) => land.Carves.Concat(land.Additions).Any(c => p.X > c.Min.X - 1 && p.X < c.Max.X + 1 && p.Z > c.Min.Z - 1 && p.Z < c.Max.Z + 1 && p.Y < c.Max.Y + 1);
        int faces = 0, wrong = 0;
        var turned = new List<string>();
        foreach (var levels in hill.Value.Values)
            foreach (var chunk in levels)
                for (int t = 0; t < chunk.GroundTriangles + chunk.ApronTriangles; t++)
                {
                    int ia = chunk.Indices[t * 3], ib = chunk.Indices[t * 3 + 1], ic = chunk.Indices[t * 3 + 2];
                    var a = chunk.Vertex(ia); var b = chunk.Vertex(ib); var c = chunk.Vertex(ic);
                    var n = D3.Cross(b - a, c - a).Normalized();
                    D3 Normal(int v) => new(chunk.Normals[v * 3], chunk.Normals[v * 3 + 1], chunk.Normals[v * 3 + 2]);
                    if (D3.Dot(n, Normal(ia) + Normal(ib) + Normal(ic)) <= 0) turned.Add($"level {chunk.Lod} at {(a + b + c) * (1.0 / 3)}{(NearACarve((a + b + c) * (1.0 / 3)) ? " near a carve" : "")}");
                    if (chunk.Lod != 0) continue;
                    var middle = (a + b + c) * (1.0 / 3);
                    if (NearACarve(middle)) continue;
                    faces++;
                    if (land.Field(middle + n * 0.05) <= 0 || land.Field(middle - n * 0.05) >= 0) wrong++;
                }
        Assert.True(faces > 30_000);
        Assert.True(wrong == 0, $"{wrong} of {faces} faces of the finest don't face the air");
        // Away from a carve none; at one, where its shell is thinner than a cell (the tunnel's roof under the hill's
        // flank at x ±19) the finest has a few, and a thinned level never more than the finest has
        Assert.True(turned.All(t => t.EndsWith("near a carve")), $"faces turned against their vertices' normals: {string.Join("; ", turned.Where(t => !t.EndsWith("near a carve")).Take(12))}");
        int finestTurned = turned.Count(t => t.StartsWith("level 0"));
        Assert.True(finestTurned <= 8, $"{finestTurned} faces of the finest turned at a carve's thin shell");
        for (int level = 1; level < 3; level++)
            Assert.True(turned.Count(t => t.StartsWith($"level {level}")) <= finestTurned, $"level {level} turns more faces than the finest: {string.Join("; ", turned)}");
    }

    [Fact]
    public void ARoadIsGradedAlongItsSpline()
    {
        var land = lab.Value.Land;
        var road = land.GradedRoads.Single(r => r.Name == "the west road");
        var line = road.Line;
        // Its grade is what its points say at its ends and straight between its points by the length along it
        Assert.Equal(1.2, line.Values[0], 9);
        Assert.Equal(0.0, line.Values[^1], 9);
        var bake = RegionBake.Meshes(land, lab.Value.Description.World, -2, 0);
        var surface = RegionPictures.Surface(new[] { bake[0] });
        int measured = 0;
        for (int i = 2; i + 2 < line.Samples.Length; i += 3)
        {
            var p = line.Samples[i]; var q = line.Samples[i + 1];
            double dx = q.X - p.X, dz = q.Z - p.Z, len = Math.Sqrt(dx * dx + dz * dz);
            double nx = -dz / len, nz = dx / len;
            double grade = line.Values[i];
            // Level across its width: the land's own height, and the baked surface to within a few millimetres
            foreach (double across in new[] { -1.2, 0, 1.2 })
            {
                double x = p.X + nx * across, z = p.Z + nz * across;
                Assert.InRange(land.Height(x, z), grade - 0.005, grade + 0.005);
                // (Where the road runs into the tunnel's cutting, the carve's floor is the ground)
                if (Math.Floor(x / 32) != -2 || Math.Floor(z / 32) != 0 || land.Carves.Any(c => c.Distance(new D3(x, grade, z)) < 0.25)) continue;
                var top = surface.Column(x, z);
                Assert.NotEmpty(top);
                Assert.InRange(top[0].Y, grade - 0.01, grade + 0.01);
                Assert.Equal(OpenCover.Path, land.CoverAt(new D3(x, top[0].Y, z), new D3(0, 1, 0)));
                measured++;
            }
        }
        Assert.True(measured > 20, $"only {measured} points of the road in chunk -2,0");
        // Its middle point gives no height, so the grade there is the land's under it; and between two of its points
        // the grade runs straight by the length along the line
        int at = Array.FindIndex(line.Samples, s => s.X == -50 && s.Z == 8);
        Assert.True(at > 0);
        Assert.Equal(land.BaseAt(-50, 8), line.Values[at], 9);
        foreach (var (from, to) in new[] { (0, at), (at, line.Samples.Length - 1) })
            for (int i = from; i <= to; i++)
            {
                double share = (line.Along[i] - line.Along[from]) / (line.Along[to] - line.Along[from]);
                Assert.Equal(line.Values[from] + (line.Values[to] - line.Values[from]) * share, line.Values[i], 9);
            }
    }

    [Fact]
    public void TheWaterIsBakedWhereItStandsOverTheLand()
    {
        var land = lab.Value.Land;
        var world = lab.Value.Description.World;
        int Cell(double v) => (int)Math.Floor((v - Math.Floor(v / 32) * 32) / 2);
        // The pond's middle, under its level; and a chunk of the meadow with no water at all
        var pond = RegionBake.Chunk(land, world, 1, -2)[0];
        Assert.Equal(-1000, pond.WaterAt(Cell(40), Cell(-38)));
        Assert.False(RegionBake.Chunk(land, world, -2, -2)[0].HasWater);
        // The river along the south, its surface between its ends' levels
        var river = RegionBake.Chunk(land, world, 0, 1)[0];
        var levels = river.Water.Where(w => w != BakedChunk.NoWater).ToList();
        Assert.True(levels.Count > 20, $"the river covers {levels.Count} cells");
        Assert.All(levels, w => Assert.InRange(w, -950, -600));
        // Every level of a chunk carries the same water
        foreach (var level in RegionBake.Chunk(land, world, 0, 1)) Assert.Equal(river.Water, level.Water);
    }

    [Fact]
    public void TheCoverIsPaintedAndRuled()
    {
        var land = lab.Value.Land;
        var world = lab.Value.Description.World;
        var all = new[] { (-2, 0), (1, -2), (-1, -1) }.ToDictionary(c => c, c => RegionBake.Chunk(land, world, c.Item1, c.Item2)[0]);
        OpenCover Near(BakedChunk chunk, double x, double z, double y = double.NaN)
        {
            int best = -1; double bestD = double.MaxValue;
            for (int v = 0; v < chunk.VertexCount; v++)
            {
                var p = chunk.Vertex(v);
                double dx = p.MetresX - x, dz = p.MetresZ - z, dy = double.IsNaN(y) ? 0 : p.MetresY - y;
                double d = dx * dx + dz * dz + dy * dy;
                if (d < bestD) { bestD = d; best = v; }
            }
            return chunk.Covers[best];
        }
        Assert.Equal(OpenCover.Rock, Near(all[(-2, 0)], -40, 26));          // painted
        var road = land.GradedRoads.Single(r => r.Name == "the west road").Line.Samples[5];
        Assert.Equal(OpenCover.Path, Near(all[(-2, 0)], road.X, road.Z));  // the west road
        Assert.Equal(OpenCover.Sand, Near(all[(1, -2)], 40, -44.5));        // the pond's shore
        Assert.Equal(OpenCover.Mud, Near(all[(1, -2)], 40, -38));           // its bed, deeper than a metre
        Assert.Equal(OpenCover.CaveFloor, Near(all[(-1, -1)], -6, -3, 0));  // the cavern's floor
        Assert.Contains(all[(-1, -1)].Covers, c => c is OpenCover.Grass or OpenCover.GrassLight);
    }
}
