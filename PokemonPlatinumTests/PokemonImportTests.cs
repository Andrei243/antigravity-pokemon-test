using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;

namespace PokemonPlatinumTests;

/// <summary>The tests that change where model files are looked for run on their own.</summary>
[CollectionDefinition("Model files", DisableParallelization = true)]
public class ModelFilesCollection { }

/// <summary>
/// Models imported from glTF files (plan 03 · D5): reading the format, cutting big rigs into palettes, finding
/// clips by name, sizing and placing, and the round trip through <see cref="GltfWriter"/>.
/// </summary>
[Collection("Model files")]
public class PokemonImportTests
{
    private readonly ITestOutputHelper output;

    public PokemonImportTests(ITestOutputHelper output) => this.output = output;

    private static PokeModel Import(PokeModel sculpted, string species) =>
        ImportedModels.FromAsset(GltfReader.Parse(GltfWriter.Write(sculpted), null), species);

    /// <summary>Where a vertex of a mesh goes under a skin (four weighted bones; <paramref name="palette"/> maps them to the skin).</summary>
    private static Vector3 Posed(SdfMesh mesh, int v, Matrix4x4[] skin, int[]? palette = null)
    {
        var sum = Vector3.Zero;
        for (int k = 0; k < 4; k++)
        {
            float w = mesh.BoneWeights[v * 4 + k];
            if (w <= 0f) continue;
            int bone = mesh.BoneIndices[v * 4 + k];
            sum += w * Vector3.Transform(mesh.Positions[v], skin[palette != null ? palette[bone] : bone]);
        }
        return sum;
    }

    /// <summary>The original vertex behind each vertex of an imported part: parts number vertices in the order their triangles first use them.</summary>
    private static List<int> FirstUse(SdfMesh mesh)
    {
        var order = new List<int>();
        var seen = new HashSet<int>();
        foreach (int i in mesh.Indices)
            if (seen.Add(i)) order.Add(i);
        return order;
    }

    [Fact]
    public void AModelWrittenToGltfComesBackTheSame()
    {
        var gible = PokemonModels.Build("Gible");
        var bytes = GltfWriter.Write(gible);
        output.WriteLine($"Gible as glTF: {bytes.Length / 1024} KB");
        var m = Import(gible, "Gible");
        var rig = m.Imported!;

        // Every triangle comes back, in parts that each fit the shaders' bones
        int triangles = gible.Mesh.TriangleCount + (gible.DecalPatch?.TriangleCount ?? 0);
        Assert.Equal(triangles, rig.Parts.Sum(p => p.Mesh.TriangleCount));
        Assert.All(rig.Parts, p => Assert.InRange(p.Palette.Length, 1, SkinnedModel.MaxBones));

        // Sized to a height of one and standing on the ground, facing the same way
        Assert.InRange(m.Mesh.Positions.Min(p => p.Y), -1e-3f, 1e-3f);
        Assert.InRange(m.Height, 0.98f, 1.001f);
        float scale = 1f / (gible.Mesh.Positions.Max(p => p.Y) - gible.Mesh.Positions.Min(p => p.Y));
        float frontZ = gible.Mesh.Positions.Max(p => p.Z) * scale, backZ = gible.Mesh.Positions.Min(p => p.Z) * scale;
        float importedFront = m.Mesh.Positions.Max(p => p.Z), importedBack = m.Mesh.Positions.Min(p => p.Z);
        Assert.Equal(frontZ - backZ, importedFront - importedBack, 2);

        // Every clip of the body plan was written and is found again by its name
        foreach (var role in Enum.GetValues<ClipRole>()) Assert.True(rig.Has(role), role.ToString());
        Assert.True(m.OwnIdle);

        // Played at a keyframe, the imported idle puts the body where the sculpted model's idle does
        var pose = new PokePose { Time = 1f };
        gible.Animate(pose);
        m.Animate(pose);
        var body = rig.Parts.First(p => p.Mesh.VertexCount == gible.Mesh.VertexCount);
        var source = FirstUse(gible.Mesh);
        for (int v = 0; v < body.Mesh.VertexCount; v += 997)
        {
            var original = Vector3.Transform(Posed(gible.Mesh, source[v], gible.Skin), rig.Normalize);
            var imported = Posed(body.Mesh, v, m.Skin, body.Palette);
            Assert.True(Vector3.Distance(original, imported) < 2e-3f, $"vertex {v}: {original} against {imported}");
        }

        // The eyes come back as a texture
        Assert.Contains(rig.Parts, p => p.Image >= 0);
        Assert.NotNull(rig.Images[0]);
        Assert.Equal(gible.AtlasColumns * PokemonDecals.Cell, rig.Images[0]!.Width);
    }

    [Fact]
    public void ARigWithMoreBonesThanTheShadersHoldIsDrawnInPieces()
    {
        // A strip of 40 quads, each pair of rows bound to its own joint of a 41-joint chain
        const int joints = 41;
        var asset = new GltfAsset();
        var positions = new List<Vector3>();
        var jointIds = new List<int>();
        var weights = new List<float>();
        for (int i = 0; i < joints; i++)
            foreach (float x in new[] { -0.1f, 0.1f })
            {
                positions.Add(new Vector3(x, i * 0.05f, 0));
                jointIds.AddRange(new[] { i, 0, 0, 0 });
                weights.AddRange(new[] { 1f, 0f, 0f, 0f });
            }
        var indices = new List<int>();
        for (int i = 0; i < joints - 1; i++)
            indices.AddRange(new[] { i * 2, i * 2 + 1, i * 2 + 3, i * 2, i * 2 + 3, i * 2 + 2 });
        var mesh = new GltfMesh();
        mesh.Primitives.Add(new GltfPrimitive { Positions = positions.ToArray(), Joints = jointIds.ToArray(), Weights = weights.ToArray(), Indices = indices.ToArray() });
        asset.Meshes.Add(mesh);
        asset.Nodes.Add(new GltfNode { Name = "mesh", Mesh = 0, Skin = 0 });
        var skin = new GltfSkin { Joints = new int[joints], InverseBind = new Matrix4x4[joints] };
        for (int i = 0; i < joints; i++)
        {
            asset.Nodes.Add(new GltfNode { Name = "j" + i, Translation = new Vector3(0, i == 0 ? 0f : 0.05f, 0), Parent = i == 0 ? -1 : i });
            skin.Joints[i] = 1 + i;
            skin.InverseBind[i] = Matrix4x4.CreateTranslation(0, -i * 0.05f, 0);
        }
        for (int i = 1; i < joints; i++) asset.Nodes[i].Children = new[] { i + 1 };
        asset.Skins.Add(skin);
        asset.Order = Enumerable.Range(0, joints + 1).ToArray();

        var m = ImportedModels.FromAsset(asset, "Unown");
        var rig = m.Imported!;
        output.WriteLine($"{rig.Bones.Length} bones in {rig.Parts.Count} parts: {string.Join(", ", rig.Parts.Select(p => p.Palette.Length))}");
        Assert.Equal(joints, rig.Bones.Length);
        Assert.True(rig.Parts.Count >= 2);
        Assert.All(rig.Parts, p => Assert.InRange(p.Palette.Length, 1, SkinnedModel.MaxBones));
        Assert.Equal(indices.Count / 3, rig.Parts.Sum(p => p.Mesh.TriangleCount));

        // At rest every part's vertices are where the file put them, through their palettes, moved only by the
        // body plan's idle (the file has no clips)
        m.Animate(new PokePose());
        foreach (var part in rig.Parts)
            for (int v = 0; v < part.Mesh.VertexCount; v++)
            {
                var atRest = Vector3.Transform(part.Mesh.Positions[v], rig.BodySkin[1]);
                Assert.True(Vector3.Distance(atRest, Posed(part.Mesh, v, m.Skin, part.Palette)) < 1e-4f);
            }
    }

    [Fact]
    public void ClipsAreFoundByWhatTheirNamesSay()
    {
        var names = new[]
        {
            "pm0025_00_00_00000_defaultwait01_loop", "pm0025_00_00_00100_battlewait01_loop", "pm0025_00_00_00110_attack01",
            "pm0025_00_00_00111_attack02", "pm0025_00_00_00120_damage01", "pm0025_00_00_00130_down01", "pm0025_00_00_00140_appear01"
        };
        var clips = ImportedModels.MatchClips(names);
        Assert.Equal(1, clips[ClipRole.Idle]);
        Assert.Equal(2, clips[ClipRole.Physical]);
        Assert.Equal(3, clips[ClipRole.Special]);
        Assert.Equal(4, clips[ClipRole.Hit]);
        Assert.Equal(5, clips[ClipRole.Faint]);
        Assert.Equal(6, clips[ClipRole.Entry]);

        // Plain names work too, a lone clip is the idle, and the options file can name any of them
        Assert.Equal(0, ImportedModels.MatchClips(new[] { "Take 001" })[ClipRole.Idle]);
        var chosen = ImportedModels.MatchClips(new[] { "Idle", "Kick", "Hurt" }, new Dictionary<string, string> { ["physical"] = "Kick" });
        Assert.Equal(1, chosen[ClipRole.Physical]);
        Assert.Equal(2, chosen[ClipRole.Hit]);
        Assert.Equal(0, chosen[ClipRole.Idle]);
    }

    [Fact]
    public void TheModelsOwnClipsTakeOverFromTheBodyPlansMotion()
    {
        // With every clip of its own, an imported model holds still between keys the file doesn't move
        var gible = PokemonModels.Build("Gible");
        var m = Import(gible, "Gible");
        m.Animate(new PokePose { Time = 0f, Faint = 1f });
        var down = m.Skin.ToArray();
        gible.Animate(new PokePose { Time = 0f, Faint = 1f });
        var body = m.Imported!.Parts.First(p => p.Mesh.VertexCount == gible.Mesh.VertexCount);
        var source = FirstUse(gible.Mesh);
        // The file's faint clip lays it down as the sculpted model's does, not twice over
        for (int v = 0; v < body.Mesh.VertexCount; v += 1499)
            Assert.True(Vector3.Distance(Vector3.Transform(Posed(gible.Mesh, source[v], gible.Skin), m.Imported.Normalize), Posed(body.Mesh, v, down, body.Palette)) < 5e-3f);

        // Without clips, the body plan's motion moves the whole model: a physical move leans it forward
        var still = ImportedModels.FromAsset(StripClips(GltfReader.Parse(GltfWriter.Write(gible), null)), "Gible");
        Assert.False(still.OwnIdle);
        still.Animate(new PokePose { Time = 0.3f });
        var rest = still.Skin[0];
        still.Animate(new PokePose { Time = 0.3f, Attack = 0.48f, Kind = MoveCategory.Physical });
        var top = new Vector3(0, still.Height, 0);
        Assert.True(Vector3.Transform(top, still.Skin[0]).Z > Vector3.Transform(top, rest).Z + 0.02f);
    }

    private static GltfAsset StripClips(GltfAsset asset)
    {
        asset.Animations.Clear();
        return asset;
    }

    [Fact]
    public void CompressedModelsAreRefusedWithAReason()
    {
        var json = "{\"asset\":{\"version\":\"2.0\"},\"extensionsRequired\":[\"KHR_draco_mesh_compression\"],\"extensionsUsed\":[\"KHR_draco_mesh_compression\"]}";
        var e = Assert.Throws<NotSupportedException>(() => GltfReader.Parse(Encoding.UTF8.GetBytes(json), null));
        Assert.Contains("compress", e.Message);
    }

    [Fact]
    public void OnlyPlainlyMetalSurfacesAreLitAsMetal()
    {
        var json = "{\"asset\":{\"version\":\"2.0\"},\"materials\":[" +
            "{\"name\":\"chrome\",\"pbrMetallicRoughness\":{\"roughnessFactor\":0.3}}," +
            "{\"name\":\"fur\",\"pbrMetallicRoughness\":{\"metallicFactor\":0}}," +
            "{\"name\":\"painted\",\"pbrMetallicRoughness\":{\"metallicRoughnessTexture\":{\"index\":0}}}," +
            "{\"name\":\"bare\"}," +
            "{\"name\":\"flat\",\"pbrMetallicRoughness\":{},\"extensions\":{\"KHR_materials_unlit\":{}}}]}";
        var metal = GltfReader.Parse(Encoding.UTF8.GetBytes(json), null).Materials.ToDictionary(m => m.Name, m => m.Metallic);
        // glTF's default is metal, which holds only where nothing else says how the surface varies or how it is lit
        Assert.Equal(1f, metal["chrome"]);
        Assert.Equal(0f, metal["fur"]);
        Assert.Equal(0f, metal["painted"]);
        Assert.Equal(0f, metal["bare"]);
        Assert.Equal(0f, metal["flat"]);
    }

    [Fact]
    public void ATurnedModelFacesTheWayItsOptionsSay()
    {
        var gible = PokemonModels.Build("Gible");
        var asset = GltfReader.Parse(GltfWriter.Write(gible), null);
        var turned = ImportedModels.FromAsset(asset, "Gible", new ImportOptions { Yaw = 90f });
        // Gible's snout points along +Z; turned a quarter round it points along +X
        float depth = turned.Mesh.Positions.Max(p => p.Z) - turned.Mesh.Positions.Min(p => p.Z);
        float width = turned.Mesh.Positions.Max(p => p.X) - turned.Mesh.Positions.Min(p => p.X);
        float origDepth = gible.Mesh.Positions.Max(p => p.Z) - gible.Mesh.Positions.Min(p => p.Z);
        float origWidth = gible.Mesh.Positions.Max(p => p.X) - gible.Mesh.Positions.Min(p => p.X);
        Assert.Equal(origDepth / origWidth, width / depth, 2);
    }

    [Fact]
    public void ModelFilesAreFoundByNameOrNumber()
    {
        string folder = Path.Combine(Path.GetTempPath(), "model-overrides-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var saved = ModelOverrides.Folders.ToList();
        try
        {
            foreach (var name in new[] { "mr-mime.glb", "0201.glb", "Nidoran♀.gltf", "notes.txt" })
                File.WriteAllBytes(Path.Combine(folder, name), Array.Empty<byte>());
            ModelOverrides.Folders.Clear();
            ModelOverrides.Folders.Add(folder);
            ModelOverrides.Refresh();
            Assert.EndsWith("mr-mime.glb", ModelOverrides.Find("Mr. Mime"));
            Assert.EndsWith("0201.glb", ModelOverrides.Find("Unown"));
            Assert.EndsWith("Nidoran♀.gltf", ModelOverrides.Find("Nidoran♀"));
            Assert.Null(ModelOverrides.Find("Nidoran♂"));
            Assert.Null(ModelOverrides.Find("Pikachu"));
        }
        finally
        {
            ModelOverrides.Folders.Clear();
            ModelOverrides.Folders.AddRange(saved);
            ModelOverrides.Refresh();
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void AModelFileReplacesTheSpeciesModelAndABrokenOneIsReported()
    {
        string folder = Path.Combine(Path.GetTempPath(), "model-overrides-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var saved = ModelOverrides.Folders.ToList();
        try
        {
            // Gible's own model, written out and dropped in as Piplup's
            GltfWriter.Save(PokemonModels.Build("Gible"), Path.Combine(folder, "Piplup.glb"));
            File.WriteAllText(Path.Combine(folder, "Turtwig.glb"), "not a model");
            ModelOverrides.Folders.Clear();
            ModelOverrides.Folders.Add(folder);
            ModelOverrides.Refresh();
            while (ImportedModels.Problems.TryDequeue(out _)) { }

            var piplup = PokemonModels.Build("Piplup");
            Assert.NotNull(piplup.Imported);
            Assert.Equal("Piplup", piplup.Species);

            var turtwig = PokemonModels.Build("Turtwig");
            Assert.Null(turtwig.Imported);
            Assert.Contains(ImportedModels.Problems, p => p.Contains("Turtwig.glb"));
        }
        finally
        {
            ModelOverrides.Folders.Clear();
            ModelOverrides.Folders.AddRange(saved);
            ModelOverrides.Refresh();
            Directory.Delete(folder, true);
        }
    }

    // Four flat quadrants (red, green / blue, white) at quality 95: 4:2:0 baseline and 4:4:4 progressive
    private const string QuadsBaseline = "/9j/4AAQSkZJRgABAQAAAAAAAAD/2wBDAAIBAQEBAQIBAQECAgICAgQDAgICAgUEBAMEBgUGBgYFBgYGBwkIBgcJBwYGCAsICQoKCgoKBggLDAsKDAkKCgr/2wBDAQICAgICAgUDAwUKBwYHCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgr/wAARCAAQABADASIAAhEBAxEB/8QAFgABAQEAAAAAAAAAAAAAAAAACAkK/8QAFBABAAAAAAAAAAAAAAAAAAAAAP/EABUBAQEAAAAAAAAAAAAAAAAAAAIE/8QAKREAAQICBgsAAAAAAAAAAAAAExESBxQAAgMGQWIIFRYYISMlQkNhY//aAAwDAQACEQMRAD8AO5UJ/tUCqK+jlsLJ9UMYngYjGfasqu9ImK0Me70byuruTISBu45Diy2LWhzOdgnH/9k=";
    private const string QuadsProgressive = "/9j/4AAQSkZJRgABAQAAAAAAAAD/2wBDAAIBAQEBAQIBAQECAgICAgQDAgICAgUEBAMEBgUGBgYFBgYGBwkIBgcJBwYGCAsICQoKCgoKBggLDAsKDAkKCgr/2wBDAQICAgICAgUDAwUKBwYHCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgr/wgARCAAQABADAREAAhEBAxEB/8QAFgABAQEAAAAAAAAAAAAAAAAABwgJ/8QAGAEAAgMAAAAAAAAAAAAAAAAABggFBwn/2gAMAwEAAhADEAAAATsDbRUhcwZ/fy3NUFZLv//EABQQAQAAAAAAAAAAAAAAAAAAACD/2gAIAQEAAQUCH//EABQRAQAAAAAAAAAAAAAAAAAAACD/2gAIAQMBAT8BH//EABQRAQAAAAAAAAAAAAAAAAAAACD/2gAIAQIBAT8BH//EABQQAQAAAAAAAAAAAAAAAAAAACD/2gAIAQEABj8CH//EABQQAQAAAAAAAAAAAAAAAAAAACD/2gAIAQEAAT8hH//aAAwDAQACAAMAAAAQAA//xAAUEQEAAAAAAAAAAAAAAAAAAAAg/9oACAEDAQE/EB//xAAUEQEAAAAAAAAAAAAAAAAAAAAg/9oACAECAQE/EB//xAAUEAEAAAAAAAAAAAAAAAAAAAAg/9oACAEBAAE/EB//2Q==";

    [Theory]
    [InlineData(QuadsBaseline)]
    [InlineData(QuadsProgressive)]
    public void JpegTexturesAreDecoded(string base64)
    {
        var c = JpegDecoder.Decode(Convert.FromBase64String(base64));
        Assert.Equal(16, c.Width);
        Assert.Equal(16, c.Height);
        void Near(int x, int y, int r, int g, int b)
        {
            var p = c.Get(x, y);
            Assert.True(Math.Abs(p.R - r) <= 12 && Math.Abs(p.G - g) <= 12 && Math.Abs(p.B - b) <= 12, $"({x},{y}) is {p.R},{p.G},{p.B}");
        }
        Near(3, 3, 220, 40, 40);
        Near(12, 3, 40, 200, 60);
        Near(3, 12, 40, 60, 220);
        Near(12, 12, 240, 240, 240);
    }

    [Fact]
    public void ModelsAreBuiltInTheBackgroundAndLetGoBetweenScenes()
    {
        PokemonModels.Release("Caterpie");
        Assert.False(PokemonModels.TryGet("Caterpie", out _));
        PokemonModels.Request("Caterpie");
        var clock = System.Diagnostics.Stopwatch.StartNew();
        PokeModel? built = null;
        while (clock.Elapsed.TotalSeconds < 60 && !PokemonModels.TryGet("Caterpie", out built)) System.Threading.Thread.Sleep(20);
        Assert.NotNull(built);
        Assert.Same(built, PokemonModels.Get("Caterpie"));

        // Between scenes the models no one needs go, the team's and the preloaded ones stay
        PokemonModels.Get("Metapod");
        PokemonModels.Trim(new[] { "Metapod" });
        Assert.False(PokemonModels.TryGet("Caterpie", out _));
        Assert.True(PokemonModels.TryGet("Metapod", out _));
    }

    [Fact]
    public void SpriteCachesFollowTheirModels()
    {
        PokemonModels.ForgetSignatures();
        string bulbasaur = PokemonModels.Signature("Bulbasaur");
        Assert.Equal(bulbasaur, PokemonModels.Signature("Bulbasaur"));
        Assert.NotEqual(bulbasaur, PokemonModels.Signature("Ivysaur"));

        // A model file dropped in for the species changes it, and taking the file away changes it back
        string folder = Path.Combine(Path.GetTempPath(), "model-overrides-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var saved = ModelOverrides.Folders.ToList();
        try
        {
            GltfWriter.Save(PokemonModels.Build("Gible"), Path.Combine(folder, "Bulbasaur.glb"));
            ModelOverrides.Folders.Clear();
            ModelOverrides.Folders.Add(folder);
            ModelOverrides.Refresh();
            PokemonModels.ForgetSignatures();
            Assert.NotEqual(bulbasaur, PokemonModels.Signature("Bulbasaur"));
        }
        finally
        {
            ModelOverrides.Folders.Clear();
            ModelOverrides.Folders.AddRange(saved);
            ModelOverrides.Refresh();
            PokemonModels.ForgetSignatures();
            Directory.Delete(folder, true);
        }
        Assert.Equal(bulbasaur, PokemonModels.Signature("Bulbasaur"));
    }

    [Fact]
    public void ChannelsHoldBeforeAndAfterTheirKeysAndInterpolateBetween()
    {
        var ch = new GltfChannel { Path = GltfPath.Translation, Width = 3, Times = new[] { 1f, 2f }, Values = new[] { 0f, 0f, 0f, 2f, 4f, 6f } };
        var v = new float[4];
        ImportedModels.Sample(ch, 0.5f, v);
        Assert.Equal(0f, v[0]);
        ImportedModels.Sample(ch, 1.5f, v);
        Assert.Equal(new[] { 1f, 2f, 3f }, v.Take(3));
        ImportedModels.Sample(ch, 9f, v);
        Assert.Equal(6f, v[2]);
        ch.Interpolation = GltfInterpolation.Step;
        ImportedModels.Sample(ch, 1.9f, v);
        Assert.Equal(0f, v[1]);

        var turn = new GltfChannel
        {
            Path = GltfPath.Rotation, Width = 4, Times = new[] { 0f, 1f },
            Values = new[] { 0f, 0f, 0f, 1f, 0f, MathF.Sin(MathF.PI / 4f), 0f, MathF.Cos(MathF.PI / 4f) }
        };
        ImportedModels.Sample(turn, 0.5f, v);
        var q = new Quaternion(v[0], v[1], v[2], v[3]);
        Assert.Equal(1f, q.Length(), 4);
        Assert.Equal(MathF.Sin(MathF.PI / 8f), q.Y, 4);
    }
}
