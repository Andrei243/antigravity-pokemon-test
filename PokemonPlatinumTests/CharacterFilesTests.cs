using System;
using System.IO;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Graphics;
using Raylib_cs;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>Characters read from files made in Blender (plan 47, route b; style guide, "Characters from files").</summary>
public class CharacterFilesTests
{
    private static string FramesPath => Path.Combine(Repository.Root, "tools", "BlenderAnim", "blender", "people_frames.json");

    [Fact]
    public void TheFramesBlenderBuildsOnAreTheEnginesOwn()
    {
        string made = CharacterModels.FramesJson();
        string? old = File.Exists(FramesPath) ? File.ReadAllText(FramesPath).Replace("\r\n", "\n") : null;
        if (old == made) return;
        if (Golden.GoldenFile.Accepting)
        {
            File.WriteAllText(FramesPath, made);
            return;
        }
        Assert.Fail($"{FramesPath} is not the frames the engine has today: run with GOLDEN=accept, then build the characters' files again.");
    }

    [Fact]
    public void AZoneIsFilledWithItsColour()
    {
        var palette = (Primary: new Color(200, 40, 40, 255), Accent: new Color(40, 200, 40, 255), Stripe: new Color(40, 40, 200, 255), Third: new Color(90, 80, 70, 255));
        Assert.Equal(palette.Primary, CharacterFiles.Fill(new Vector3(1, 0, 0), palette));
        Assert.Equal(palette.Accent, CharacterFiles.Fill(new Vector3(0, 1, 0), palette));
        Assert.Equal(new Color(250, 250, 252, 255), CharacterFiles.Fill(new Vector3(0, 0, 1), palette));
        Assert.Equal(new Color(34, 26, 40, 255), CharacterFiles.Fill(new Vector3(0, 0, 0), palette));
        // Two channels full together are the paired zones: the stripe, the primary's shade, the third colour
        Assert.Equal(palette.Stripe, CharacterFiles.Fill(new Vector3(0, 1, 1), palette));
        Assert.Equal(MeshBuilder.Scale(palette.Primary, 0.72f), CharacterFiles.Fill(new Vector3(1, 1, 0), palette));
        Assert.Equal(palette.Third, CharacterFiles.Fill(new Vector3(1, 0, 1), palette));
        // Half way between the primary and the stripe is half of each
        var half = CharacterFiles.Fill(new Vector3(0.5f, 0.5f, 0.5f), palette);
        Assert.InRange(half.R, 119, 121);
        Assert.InRange(half.B, 119, 121);
        // A fifth of the way from the primary into the stripe is a fifth of the stripe, never the shade
        var fifth = CharacterFiles.Fill(new Vector3(0.8f, 0.2f, 0.2f), palette);
        Assert.InRange(fifth.R, 167, 169);
        Assert.InRange(fifth.B, 71, 73);
        // And a fifth of the way into the shade is the shade's
        Assert.InRange(CharacterFiles.Fill(new Vector3(1f, 0.2f, 0f), palette).R, 188, 190);
    }

    [Fact]
    public void AStripeIsThePrimaryColourOnALookWithoutStripes()
    {
        var plain = new CharacterStyle { Stripes = false };
        var striped = new CharacterStyle { Stripes = true };
        Assert.Equal(plain.Top, CharacterFiles.Palette("top", plain).Stripe);
        Assert.Equal(striped.Accent, CharacterFiles.Palette("top", striped).Stripe);
    }

    [Fact]
    public void EveryLookAsksForPiecesInTheSlotsTheFormatHas()
    {
        string[] slots = { "body", "hair", "face", "hat", "top", "bottoms", "shoes", "bag", "extra", "held" };
        foreach (string name in PokemonPlatinumEngine.Data.CharacterStyles.Default.Names)
        {
            var (body, clothes, held) = CharacterFiles.Wanted(CharacterStyle.For(name));
            foreach (string piece in body.Concat(clothes).Concat(held))
                Assert.Contains(piece.Split('.')[0], slots);
        }
        foreach (string piece in CharacterFiles.EveryFacePiece()) Assert.StartsWith("face.", piece);
    }

    [Fact]
    public void WithNoFileALookIsSculptedAsBefore()
    {
        // The tests' folders hold no characters: the reader answers with nothing and the kits sculpt
        var skeleton = new Skeleton();
        Assert.Null(CharacterFiles.For(BodyBuild.Kid, false, skeleton));
    }

    [Fact]
    public void APieceIsReadOntoTheEnginesBonesByName()
    {
        // A triangle skinned to "head" and "hair", in a file whose joints come in another order than the engine's
        var asset = new GltfAsset();
        asset.Nodes.Add(new GltfNode { Name = "hair" });
        asset.Nodes.Add(new GltfNode { Name = "head" });
        asset.Nodes.Add(new GltfNode { Name = "hat.Cap", Mesh = 0, Skin = 0 });
        asset.Nodes[2].Extras["colours"] = "zones";
        asset.Nodes[2].Extras["tint"] = "hat";
        asset.Nodes[2].Extras["hides"] = "hair.top, torso";
        asset.Skins.Add(new GltfSkin { Joints = new[] { 0, 1 } });
        var mesh = new GltfMesh();
        mesh.Primitives.Add(new GltfPrimitive
        {
            Positions = new[] { new Vector3(0, 1, 0), new Vector3(0.1f, 1, 0), new Vector3(0, 1.1f, 0) },
            Normals = new[] { Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ },
            Colors = new[] { new Vector4(1, 0, 0, 1), new Vector4(0, 1, 0, 1), new Vector4(0, 0, 1, 1) },
            Joints = new[] { 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0 },
            Weights = new[] { 0.25f, 0.75f, 0, 0, 1f, 0, 0, 0, 1f, 0, 0, 0 },
            Indices = new[] { 0, 1, 2 }
        });
        asset.Meshes.Add(mesh);
        asset.Order = new[] { 0, 1, 2 };

        var skeleton = new Skeleton();
        skeleton.Add("root", -1, Vector3.Zero);
        skeleton.Add("head", 0, Vector3.UnitY);
        skeleton.Add("hair", 1, Vector3.UnitY);
        var file = CharacterFiles.Read(asset, skeleton);
        var piece = file.Pieces["hat.Cap"];
        Assert.True(piece.Zones);
        Assert.Equal("hat", piece.Tint);
        Assert.Equal(new[] { "hair.top", "torso" }, piece.Hides);
        Assert.Equal(0.25f, piece.Mesh.WeightOf(0, 2), 3);
        Assert.Equal(0.75f, piece.Mesh.WeightOf(0, 1), 3);
        Assert.Equal(1f, piece.Mesh.WeightOf(1, 1), 3);

        var style = new CharacterStyle { HatColor = new Color(10, 20, 30, 255), HatBand = new Color(200, 210, 220, 255) };
        var made = CharacterFiles.Assemble(file, new[] { "hat.Cap", "hat.Missing?" }, style)!;
        Assert.Equal(style.HatColor, made.Colors[0]);
        Assert.Equal(style.HatBand, made.Colors[1]);
        Assert.Equal(new[] { "body.torso", "hair.top" }, CharacterFiles.HiddenBy(file, new[] { "hat.Cap" }).OrderBy(n => n).ToArray());
    }

    [Fact]
    public void ACutUnderAHatIsThePlainCutWhereTheFileHasNoneOfItsOwn()
    {
        var file = new CharacterFile();
        file.Pieces["hair.Afro"] = new CharacterPiece { Name = "hair.Afro" };
        file.Pieces["hair.Short"] = new CharacterPiece { Name = "hair.Short" };
        file.Pieces["hair.Short.hat"] = new CharacterPiece { Name = "hair.Short.hat" };
        Assert.Equal("hair.Afro", CharacterFiles.Find(file, "hair.Afro.hat")!.Name);
        Assert.Equal("hair.Short.hat", CharacterFiles.Find(file, "hair.Short.hat")!.Name);
        Assert.Null(CharacterFiles.Find(file, "hair.Long.hat"));
        // A hat that leaves the crown bare leaves the cut as it is
        Assert.Contains("hair.Long", CharacterFiles.Wanted(new CharacterStyle { Hair = HairCut.Long, Hat = Headwear.Bow }).Body);
        Assert.Contains("hair.Long.hat", CharacterFiles.Wanted(new CharacterStyle { Hair = HairCut.Long, Hat = Headwear.Beret }).Body);
    }

    [Fact]
    public void AFaceIsItsEyesBrowsAndMouthForThePose()
    {
        Assert.Equal(new[] { "face.eyes.open", "face.brows.Happy", "face.mouth.Happy" }, CharacterFiles.FacePieces(Expression.Happy, false, false));
        Assert.Equal(new[] { "face.eyes.shut", "face.lashes.shut", "face.brows.Sad", "face.mouth.open" }, CharacterFiles.FacePieces(Expression.Sad, true, true, lashes: true));
        Assert.Contains("face.eyes.wide", CharacterFiles.FacePieces(Expression.Surprised, false, false));
        // 3 eyes, 3 lashes, 5 brows, 5 mouths and the open mouth
        Assert.Equal(17, CharacterFiles.EveryFacePiece().Count());
    }

    [Fact]
    public void ALookMissingAPieceIsNamedSoItIsSculptedInstead()
    {
        var file = new CharacterFile();
        foreach (string name in CharacterFiles.EveryFacePiece().Concat(CharacterFiles.BodyParts.Select(p => "body." + p))
                     .Concat(new[] { "hair.Short", "top.Shirt", "bottoms.Trousers", "shoes.Shoes" }))
            file.Pieces[name] = new CharacterPiece { Name = name };
        Assert.Empty(CharacterFiles.Missing(file, new CharacterStyle()));
        Assert.Equal(new[] { "hat.Fedora", "top.Coat", "extra.Scarf" }, CharacterFiles.Missing(file, new CharacterStyle { Hat = Headwear.Fedora, Coat = true, Scarf = true }));
    }

    /// <summary>
    /// Every look of the table in both kits against built files (not part of a plain run: the files are in no
    /// repository): CHARACTER_FILES names the folder that holds them, and the sweep writes <c>sweep.txt</c> there
    /// with what each file took to read, the looks a file can't make and the largest meshes.
    /// </summary>
    [Fact]
    public void EveryLookOfTheTableIsMadeFromTheBuiltFiles()
    {
        string? folder = Environment.GetEnvironmentVariable("CHARACTER_FILES");
        if (string.IsNullOrEmpty(folder)) return;
        var report = new System.Text.StringBuilder();
        var files = new System.Collections.Generic.Dictionary<string, CharacterFile?>();
        var table = PokemonPlatinumEngine.Data.CharacterStyles.Default;
        int made = 0, fallen = 0, most = 0;
        string mostOf = "";
        var problems = new System.Collections.Generic.List<string>();
        foreach (string name in table.Names.Concat(table.GrownNames).Distinct(StringComparer.OrdinalIgnoreCase))
            foreach (var kit in Enum.GetValues<PokemonPlatinumEngine.Data.CharacterKit>())
            {
                string type = CharacterStyle.InKit(name, kit);
                var s = CharacterStyle.For(type);
                bool curved = s.Kit == PokemonPlatinumEngine.Data.CharacterKit.Grown && s.CurvedFigure();
                string fileName = CharacterFiles.NameOf(s.Build, curved);
                if (!files.TryGetValue(fileName, out var file))
                {
                    string path = Path.Combine(folder, fileName + ".glb");
                    var clock = System.Diagnostics.Stopwatch.StartNew();
                    long before = GC.GetTotalMemory(true);
                    file = File.Exists(path) ? CharacterFiles.Read(path, CharacterModels.SkeletonOf(s)) : null;
                    long ms = clock.ElapsedMilliseconds;
                    report.Append(file == null ? $"{fileName}: no file\n"
                        : $"{fileName}: {file.Pieces.Count} pieces, read in {ms} ms, {(GC.GetTotalMemory(true) - before) / (1024 * 1024)} MB held\n");
                    files[fileName] = file;
                }
                if (file == null) continue;
                var missing = CharacterFiles.Missing(file, s);
                if (missing.Count > 0) { fallen++; problems.Add($"{type}: lacks {string.Join(", ", missing)}"); continue; }
                var (body, clothes, held) = CharacterFiles.Wanted(s);
                var hidden = CharacterFiles.HiddenBy(file, clothes);
                int Count(System.Collections.Generic.IEnumerable<string> names, System.Collections.Generic.ISet<string>? gone = null) =>
                    names.Where(n => gone == null || !gone.Contains(n.TrimEnd('?'))).Sum(n => CharacterFiles.Find(file, n)?.Mesh.VertexCount ?? 0);
                int b = Count(body, hidden), c = Count(clothes), h = Count(held);
                if (Math.Max(b, Math.Max(c, h)) > ushort.MaxValue) { fallen++; problems.Add($"{type}: too large (body {b}, clothes {c}, held {h})"); continue; }
                made++;
                if (Math.Max(b, c) > most) (most, mostOf) = (Math.Max(b, c), $"{type} (body {b}, clothes {c}, held {h})");
            }
        report.Append($"made from files: {made}; sculpted instead: {fallen}; the largest mesh: {most} vertices, {mostOf}\n");
        foreach (string problem in problems) report.Append(problem).Append('\n');
        File.WriteAllText(Path.Combine(folder, "sweep.txt"), report.ToString());
    }
}
