using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Graphics;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>The GPU ledger (plan 16 · T21): counts by kind and owner, and nothing loaded behind its back.</summary>
public class GpuLedgerTests
{
    [Fact]
    public void TheLedgerAddsAndFreesByOwner()
    {
        var ledger = new GpuLedger();
        ledger.Add(GpuKind.Texture, 3, "sprites", GpuLedger.PictureBytes(128, 128));
        ledger.Add(GpuKind.Texture, 4, "sprites", GpuLedger.PictureBytes(48, 48));
        ledger.Add(GpuKind.Mesh, 3, "chunk", 1000);
        ledger.Add(GpuKind.Target, 9, "post", 64);

        var snap = ledger.Snapshot();
        Assert.Equal(2, snap.Count(GpuKind.Texture, "sprites"));
        Assert.Equal(1, snap.Count(GpuKind.Mesh, "chunk"));
        Assert.Equal(4, snap.Total);
        Assert.Equal(128 * 128 * 4 + 48 * 48 * 4 + 1000 + 64, snap.Bytes);
        Assert.Equal(new[] { "chunk", "post", "sprites" }, snap.Owners.ToArray());

        // The same id of another kind is another thing
        Assert.True(ledger.Remove(GpuKind.Texture, 3));
        Assert.True(ledger.Holds(GpuKind.Mesh, 3));
        Assert.Equal(1, ledger.Snapshot().Count(GpuKind.Texture, "sprites"));
    }

    [Fact]
    public void TheLedgerNeverGoesBelowNought()
    {
        var ledger = new GpuLedger();
        Assert.False(ledger.Remove(GpuKind.Texture, 7));
        ledger.Add(GpuKind.Shader, 7, "shader", 0);
        Assert.True(ledger.Remove(GpuKind.Shader, 7));
        Assert.False(ledger.Remove(GpuKind.Shader, 7));
        Assert.Equal(0, ledger.Snapshot().Total);
        // Id 0 is nothing the driver made (a failed load)
        ledger.Add(GpuKind.Texture, 0, "ui", 10);
        Assert.Equal(0, ledger.Snapshot().Total);
    }

    [Fact]
    public void ALoadReplacedUnderTheSameIdIsCountedOnce()
    {
        var ledger = new GpuLedger();
        ledger.Add(GpuKind.Texture, 5, "ui", 10);
        ledger.Add(GpuKind.Texture, 5, "chunk", 20);
        var snap = ledger.Snapshot();
        Assert.Equal(1, snap.Total);
        Assert.Equal(1, snap.CountOf("chunk"));
    }

    private static readonly Regex Load = new(
        @"\bRaylib\.(Load(Texture\w*|RenderTexture|Shader\w*|Font\w*)|Unload(Texture|RenderTexture|Shader|Font|Mesh)|UploadMesh)\s*\(" +
        @"|\bRlgl\.(Load|Unload)(Framebuffer|Texture\w*|VertexBuffer\w*|VertexArray|ShaderCode|ShaderProgram)\s*\(");

    [Fact]
    public void NothingIsLoadedOrFreedOnTheCardButThroughGpu()
    {
        string engine = Path.Combine(Repository.Root, "PokemonPlatinumEngine");
        var found = Directory.EnumerateFiles(engine, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                     && !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                     && Path.GetFileName(f) != "Gpu.cs")
            .SelectMany(f => File.ReadLines(f).Select((line, i) => (f, i, line)))
            .Where(x => !x.line.TrimStart().StartsWith("//") && !x.line.TrimStart().StartsWith("///") && Load.IsMatch(x.line))
            .Select(x => $"{Path.GetRelativePath(engine, x.f)}:{x.i + 1}: {x.line.Trim()}")
            .ToList();
        Assert.True(found.Count == 0, "Loads and unloads outside Graphics/Gpu.cs:\n" + string.Join("\n", found));
    }

    [Fact]
    public void TheScanFindsALoad()
    {
        Assert.Matches(Load, "var t = Raylib.LoadTextureFromImage(image);");
        Assert.Matches(Load, "Raylib.UnloadMesh(mesh);");
        Assert.Matches(Load, "uint id = Rlgl.LoadFramebuffer();");
        Assert.DoesNotMatch(Load, "var image = Raylib.LoadImageFromTexture(texture);");
    }
}
