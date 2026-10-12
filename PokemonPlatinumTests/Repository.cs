using System;
using System.IO;

namespace PokemonPlatinumTests;

/// <summary>
/// The repository the tests run from, found by walking up from the build output to the solution. Whatever a test
/// holds the code to is read from here, never from the build output: a document (<c>docs/mechanics/coverage.md</c>),
/// the engine's own source, the golden files (plan 24 · X2, <see cref="Golden.GoldenFile"/>).
/// </summary>
internal static class Repository
{
    private static readonly Lazy<string> root = new(Find);

    /// <summary>The folder that holds <c>PokemonPlatinum.sln</c>.</summary>
    public static string Root => root.Value;

    private static string Find()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "PokemonPlatinum.sln"))) return dir.FullName;
        throw new DirectoryNotFoundException("The tests don't run from inside the repository");
    }
}
