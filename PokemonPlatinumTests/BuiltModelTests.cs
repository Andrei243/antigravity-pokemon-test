using System.Linq;
using PokemonPlatinumEngine.Graphics;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The models built from the Blender sculpts (plan 48 · BL1): where they are looked for and by whom, and what the
/// reader of model files takes from a file's names (its eyes in the game's five states, its surfaces).
/// </summary>
[Collection("Model files")]
public class BuiltModelTests
{
    [Fact]
    public void NothingLooksForBuiltModelsUnlessItAsks()
    {
        // Only the game's own entry point calls LookForBuiltModels: a test's or a tool's models are the sculpts,
        // whatever the machine has built
        foreach (string folder in ModelOverrides.BuiltFolders)
            Assert.DoesNotContain(folder, ModelOverrides.Folders);
    }

    [Fact]
    public void OnlyTheGamesEntryPointAsksForBuiltModels()
    {
        string engine = System.IO.Path.Combine(Repository.Root, "PokemonPlatinumEngine");
        var callers = System.IO.Directory.EnumerateFiles(engine, "*.cs", System.IO.SearchOption.AllDirectories)
            .Where(f => !f.Contains(System.IO.Path.DirectorySeparatorChar + "obj" + System.IO.Path.DirectorySeparatorChar)
                        && !f.Contains(System.IO.Path.DirectorySeparatorChar + "bin" + System.IO.Path.DirectorySeparatorChar))
            .Where(f => System.IO.File.ReadAllText(f).Contains("LookForBuiltModels()"))
            .Select(System.IO.Path.GetFileName).OrderBy(n => n).ToArray();
        Assert.Equal(new[] { "ImportedModels.cs", "Program.cs" }, callers);
    }

    [Theory]
    [InlineData("eyes.Open", "Open")]
    [InlineData("eyes.Shut", "Shut")]
    [InlineData("eyes.Squeeze", "Squeeze")]
    [InlineData("eyes.Fierce", "Fierce")]
    [InlineData("eyes.Smile", "Smile")]
    [InlineData("EYES.shut", "Shut")]
    [InlineData("eyes.Shut.001", "Shut")]
    public void AnEyePieceIsToldByItsNodesName(string node, string state) => Assert.Equal(System.Enum.Parse<EyeState>(state), ImportedModels.EyesOf(node));

    [Theory]
    [InlineData("Turtwig")]
    [InlineData("eyes")]
    [InlineData("eyes.Wink")]
    [InlineData("eyeshadow.Open")]
    public void AnyOtherNodeIsAlwaysDrawn(string node) => Assert.Null(ImportedModels.EyesOf(node));

    [Fact]
    public void EveryEyeStateOfTheGameHasItsName()
    {
        foreach (var state in System.Enum.GetValues<EyeState>())
            Assert.Equal(state, ImportedModels.EyesOf("eyes." + state));
    }

    [Theory]
    [InlineData("fur", "Fur")]
    [InlineData("Scales", "Scales")]
    [InlineData("shell.002", "Shell")]
    [InlineData("leaf", "Leaf")]
    [InlineData("glow", "Glow")]
    public void AMaterialsNameGivesItsSurface(string material, string surface) => Assert.Equal(System.Enum.Parse<SurfaceMaterial>(surface), ImportedModels.SurfaceNamed(material));

    [Theory]
    [InlineData("body")]
    [InlineData("glow0")]
    [InlineData("default")]
    [InlineData("")]
    [InlineData(null)]
    public void AnyOtherMaterialIsJudgedByItsValues(string? material) => Assert.Null(ImportedModels.SurfaceNamed(material));
}

/// <summary>The sculpt command's export (tools/BlenderAnim, plan 48 · BL1).</summary>
[Collection("Model files")]
public class SculptExportTests
{
    [Fact]
    public void ASpeciesAlreadyBuiltIsStillExportedAsItsSculpt()
    {
        // A built Turtwig stands in for the sculpt everywhere a model is asked for; the export a new build stands on
        // must be the sculpt all the same, with its nine bones and eleven clips, never the file read back
        string folder = System.IO.Directory.CreateTempSubdirectory("pokemon-built-").FullName;
        var saved = ModelOverrides.Folders.ToList();
        try
        {
            GltfWriter.Save(PokemonModels.Build("Piplup"), System.IO.Path.Combine(folder, "Turtwig.glb"));
            ModelOverrides.Folders.Clear();
            ModelOverrides.Folders.Add(folder);
            ModelOverrides.Refresh();
            Assert.NotNull(ModelOverrides.Find("Turtwig"));

            string export = System.IO.Path.Combine(folder, "export.glb");
            BlenderAnim.Sculpt.Export("Turtwig", export);
            var asset = GltfReader.Read(export);
            Assert.Contains(asset.Nodes, n => n.Name == "leaves");
            Assert.DoesNotContain(asset.Nodes, n => n.Name == "wingL");
            Assert.Equal(11, asset.Animations.Count);
            Assert.True(Overrides.Enabled);
        }
        finally
        {
            ModelOverrides.Folders.Clear();
            ModelOverrides.Folders.AddRange(saved);
            ModelOverrides.Refresh();
            System.IO.Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void ASculptsScriptIsNamedAfterItsSpecies()
    {
        Assert.EndsWith("turtwig.py", BlenderAnim.Sculpt.ScriptOf("Turtwig"));
        Assert.EndsWith("mrmime.py", BlenderAnim.Sculpt.ScriptOf("Mr. Mime"));
        Assert.EndsWith("charizardmegax.py", BlenderAnim.Sculpt.ScriptOf("Charizard-Mega-X"));
    }
}
