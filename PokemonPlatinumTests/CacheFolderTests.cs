using System;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Graphics;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Where meshes and sprites are kept (plan 16 · T16): beside the executable, or in the folder POKEMON_CACHE names,
/// which several trees share and where nothing is deleted for being older.
/// </summary>
public class CacheFolderTests
{
    [Fact]
    public void WithoutTheVariableTheCacheIsBesideTheExecutable()
    {
        Assert.Null(CacheFolders.SharedFrom(null));
        Assert.Null(CacheFolders.SharedFrom("  "));
        string bin = Path.Combine(Path.GetTempPath(), "bin");
        Assert.Equal(Path.Combine(bin, "cache", "models"), CacheFolders.Of("models", null, bin));
        Assert.Equal(Path.Combine(bin, "cache", "sprites"), CacheFolders.Of("sprites", null, bin));
    }

    [Fact]
    public void WithTheVariableEveryTreeUsesItsFolder()
    {
        string shared = Path.Combine(Path.GetTempPath(), "pokemon-cache");
        Assert.Equal(shared, CacheFolders.SharedFrom(shared));
        Assert.Equal(Path.Combine(shared, "models"), CacheFolders.Of("models", shared, "/anywhere/bin"));
        Assert.Equal(Path.Combine(shared, "sprites"), CacheFolders.Of("sprites", shared, "/elsewhere/bin"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OlderVersionsGoOnlyFromAFolderOfItsOwn(bool prune)
    {
        string folder = Path.Combine(Path.GetTempPath(), "cache-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(folder);
            string older = Path.Combine(folder, "turtwig-0001.mesh"), other = Path.Combine(folder, "piplup-0001.mesh");
            File.WriteAllText(older, "old");
            File.WriteAllText(other, "someone else");
            string path = Path.Combine(folder, "turtwig-0002.mesh");

            CacheFolders.Write(path, s => s.Write(new byte[] { 1, 2, 3 }), () => Directory.GetFiles(folder, "turtwig-*.mesh"), prune);

            Assert.Equal(new byte[] { 1, 2, 3 }, File.ReadAllBytes(path));
            Assert.Equal(!prune, File.Exists(older));
            Assert.True(File.Exists(other));
            // Written through a temporary file, which is gone
            Assert.Empty(Directory.GetFiles(folder, "*.tmp"));
        }
        finally { Directory.Delete(folder, recursive: true); }
    }

    [Fact]
    public void AFolderThatCantBeWrittenIsNoError()
    {
        string file = Path.Combine(Path.GetTempPath(), "cache-test-" + Guid.NewGuid().ToString("N"));
        File.WriteAllText(file, "a file where the folder should be");
        try
        {
            CacheFolders.Write(Path.Combine(file, "models", "a.mesh"), s => s.WriteByte(1), () => Enumerable.Empty<string>(), prune: true);
            Assert.True(File.Exists(file));
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public void AMeshWrittenToTheCacheReadsBack()
    {
        string folder = Path.Combine(Path.GetTempPath(), "cache-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var model = new SdfModel("cache test") { BoneCount = 1 };
            model.Sphere(System.Numerics.Vector3.Zero, 0.2f, new Raylib_cs.Color(200, 100, 50, 255), 0);
            var mesh = SdfMesher.Mesh(model, 0.04f);
            string path = Path.Combine(folder, "cache_test-1.mesh");
            CacheFolders.Write(path, s => SdfCache.Write(s, mesh), () => Enumerable.Empty<string>(), prune: false);
            var back = SdfCache.Read(path);
            Assert.Equal(mesh.Positions, back.Positions);
            Assert.Equal(mesh.Indices, back.Indices);
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true); }
    }
}
