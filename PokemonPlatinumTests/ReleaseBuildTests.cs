using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 16 · T2, release builds: the version a tag gives a build and what the title shows of it, where a published
/// build keeps its caches and how it reads the meshes it was shipped with, the Steam Deck's window, the icon file and
/// the save's version.
/// </summary>
public class ReleaseBuildTests
{
    [Theory]
    [InlineData("v0.1.0", "0.1.0")]
    [InlineData("v1.12.3", "1.12.3")]
    [InlineData("V2.0.0", "2.0.0")]
    [InlineData("0.3.0", "0.3.0")]
    [InlineData("v0.2.0-beta.1", "0.2.0-beta.1")]
    [InlineData("refs/tags/v0.4.1", "0.4.1")]
    [InlineData(" v0.1.0 ", "0.1.0")]
    public void ATagGivesTheBuildItsVersion(string tag, string version)
    {
        Assert.Equal(version, GameVersion.FromTag(tag));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("main")]
    [InlineData("v1.2")]
    [InlineData("v1.2.3.4")]
    [InlineData("v1.2.x")]
    [InlineData("v1.2.3-")]
    [InlineData("v1.2.3-beta 1")]
    [InlineData("release-1.0.0")]
    public void NoTagOrOneThatIsNoVersionIsADevelopmentBuild(string? tag)
    {
        Assert.Equal(GameVersion.Development, GameVersion.FromTag(tag));
    }

    [Fact]
    public void TheDevelopmentVersionIsAVersionAndThisBuildCarriesOne()
    {
        Assert.True(GameVersion.IsVersion(GameVersion.Development));
        // The test run is a build made from the source unless a release's -p:Version says otherwise
        Assert.True(GameVersion.IsVersion(GameVersion.Shown), GameVersion.Shown);
        Assert.False(GameVersion.Shipped);
    }

    [Fact]
    public void TheTitleAndTheOptionsShowTheVersionWithoutTheCommit()
    {
        // The SDK writes the commit after a plus sign: two builds of one version draw the same title
        Assert.DoesNotContain("+", GameVersion.Shown);
        Assert.Equal("0.1.0", GameVersion.Parse("0.1.0+0123456789abcdef").Version);
        Assert.Equal($"v{GameVersion.Shown}", TitleScreen.VersionLine);
        var (_, _, help) = OptionsScreen.Describe(new GameSettings(), OptionRow.Help);
        Assert.Contains(GameVersion.Shown, help);
    }

    [Fact]
    public void APublishedBuildWritesItsCachesBesideTheSavesAndReadsWhatItShipped()
    {
        string saves = Path.Combine(Path.GetTempPath(), "saves"), bin = Path.Combine(Path.GetTempPath(), "game");
        var (models, sprites, shipped) = CacheFolders.ForShippedBuild(saves, bin);
        Assert.Equal(Path.Combine(saves, "cache", "models"), models);
        Assert.Equal(Path.Combine(saves, "cache", "sprites"), sprites);
        Assert.Equal(new[] { Path.Combine(bin, "cache", "models") }, shipped);

        // A portable copy keeps its saves beside the executable: its own cache is the shipped one, read once
        var (portable, _, none) = CacheFolders.ForShippedBuild(bin, bin + Path.DirectorySeparatorChar);
        Assert.Equal(Path.Combine(bin, "cache", "models"), portable);
        Assert.Empty(none);
    }

    [Fact]
    public void TheDecksScreenTakesItsOwnSizeAndOtherScreensKeepTheirs()
    {
        Assert.Equal((1280, 800), GameSettings.LargestWindowFor(1280, 800));
        // Safe mode's window is still the first listed
        Assert.Equal((1280, 720), GameSettings.WindowSizes[0]);
        Assert.Contains((1280, 800), GameSettings.WindowSizes);
        // A desktop's monitor is never given the 16:10 size
        Assert.Equal((1280, 720), GameSettings.LargestWindowFor(1366, 768));
        Assert.Equal((1280, 720), GameSettings.LargestWindowFor(1440, 900));
        Assert.Equal((1600, 900), GameSettings.LargestWindowFor(1920, 1080));
        Assert.Equal((1920, 1080), GameSettings.LargestWindowFor(1920, 1200));
    }

    [Fact]
    public void TheOptionsWindowRowStepsThroughEverySizeAndStartsFromTheDefault()
    {
        var settings = new GameSettings { WindowWidth = 1000, WindowHeight = 600 };
        OptionsScreen.Change(settings, OptionRow.WindowSize, +1);
        // An unlisted size steps on from 1920×1080, as it always did
        Assert.Equal((2560, 1440), (settings.WindowWidth, settings.WindowHeight));
        var seen = new System.Collections.Generic.HashSet<(int, int)>();
        for (int i = 0; i < GameSettings.WindowSizes.Length; i++)
        {
            seen.Add((settings.WindowWidth, settings.WindowHeight));
            OptionsScreen.Change(settings, OptionRow.WindowSize, +1);
        }
        Assert.Equal(GameSettings.WindowSizes.Length, seen.Count);
    }

    [Fact]
    public void ASaveKeepsTheVersionThatWroteItAndAnOlderOneHasNone()
    {
        var save = new SaveData { GameVersion = "0.1.0" };
        string json = JsonSerializer.Serialize(save);
        Assert.Equal("0.1.0", JsonSerializer.Deserialize<SaveData>(json)!.GameVersion);
        Assert.Null(JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(new SaveData()))!.GameVersion);
        Assert.DoesNotContain("GameVersion", JsonSerializer.Serialize(new SaveData()));
    }

    [Fact]
    public void AnIconHoldsEachPictureAndReadsBack()
    {
        static byte[] Png(int marker) => new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, (byte)marker, 1, 2, 3 };
        byte[] icon = IconFile.Write(new[] { (16, Png(16)), (256, Png(255)), (32, Png(32)) });
        // Largest first; 256 is written as nought in the directory and read back as 256
        Assert.Equal(new[] { 256, 32, 16 }, IconFile.SizesIn(icon));
        Assert.Equal(0, icon[6]);
        Assert.Equal(6 + 3 * 16 + 3 * 12, icon.Length);
        Assert.Throws<ArgumentException>(() => IconFile.Write(new[] { (16, new byte[] { 1, 2, 3 }) }));
        Assert.Throws<ArgumentOutOfRangeException>(() => IconFile.Write(new[] { (512, Png(1)) }));
    }

    [Fact]
    public void TheReleaseWorkflowNamesTheFilesItShips()
    {
        string root = RepositoryRoot();
        string workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "release.yml"));
        foreach (string rid in new[] { "win-x64", "linux-x64", "osx-x64", "osx-arm64" })
        {
            Assert.Contains($"rid: {rid}", workflow);
            Assert.True(File.Exists(Path.Combine(root, "PokemonPlatinumEngine", "Properties", "PublishProfiles", $"{rid}.pubxml")), rid);
            string profile = File.ReadAllText(Path.Combine(root, "PokemonPlatinumEngine", "Properties", "PublishProfiles", $"{rid}.pubxml"));
            Assert.Contains($"<RuntimeIdentifier>{rid}</RuntimeIdentifier>", profile);
            Assert.Contains("<ShippedBuild>true</ShippedBuild>", profile);
        }
        // What the zip carries beside the game, and the list --verify reads
        foreach (string shipped in new[] { "docs/playing.md", Manifest.FileName, "THIRD-PARTY-NOTICES.md", "LICENSE", "README.md", "mesh-cache" })
            Assert.Contains(shipped, workflow);
        Assert.True(File.Exists(Path.Combine(root, "docs", "playing.md")));
        Assert.Contains("## [Unreleased]", File.ReadAllText(Path.Combine(root, "CHANGELOG.md")));
    }

    private static string RepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "PokemonPlatinum.sln"))) return dir.FullName;
        throw new InvalidOperationException("No repository above the tests' folder.");
    }
}

/// <summary>The cache a release ships, read beside the one the game writes (plan 16 · T2). It swaps the mesh cache's folders, so it runs alone.</summary>
[Collection("Model files")]
public class ShippedMeshCacheTests
{
    [Fact]
    public void AMeshShippedBesideTheGameIsReadAndNotWrittenAgain()
    {
        string root = Path.Combine(Path.GetTempPath(), "shipped-cache-" + Guid.NewGuid().ToString("N"));
        string shipped = Path.Combine(root, "game", "cache", "models"), own = Path.Combine(root, "saves", "cache", "models");
        string folder = SdfCache.Folder;
        var shippedFolders = SdfCache.ShippedFolders;
        try
        {
            var model = new SdfModel("shipped test") { BoneCount = 1 };
            model.Sphere(System.Numerics.Vector3.Zero, 0.2f, new Raylib_cs.Color(80, 160, 220, 255), 0);
            // Meshed once into what the release ships
            SdfCache.Folder = shipped;
            SdfCache.ShippedFolders = Array.Empty<string>();
            var meshed = SdfCache.Get(model, 0.04f);
            Assert.Single(Directory.GetFiles(shipped, "*.mesh"));

            // The game's own folder has nothing; the shipped mesh is read as it is, and nothing is written beside the saves
            SdfCache.Folder = own;
            SdfCache.ShippedFolders = new[] { shipped };
            var read = SdfCache.Get(model, 0.04f);
            Assert.Equal(meshed.Positions, read.Positions);
            Assert.Equal(meshed.Indices, read.Indices);
            Assert.False(Directory.Exists(own) && Directory.GetFiles(own).Length > 0);

            // A model the release didn't ship is meshed and kept in the game's own folder
            var other = new SdfModel("not shipped") { BoneCount = 1 };
            other.Sphere(System.Numerics.Vector3.Zero, 0.3f, new Raylib_cs.Color(220, 160, 80, 255), 0);
            SdfCache.Get(other, 0.04f);
            Assert.Single(Directory.GetFiles(own, "*.mesh"));
            Assert.Single(Directory.GetFiles(shipped, "*.mesh"));
        }
        finally
        {
            SdfCache.Folder = folder;
            SdfCache.ShippedFolders = shippedFolders;
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
