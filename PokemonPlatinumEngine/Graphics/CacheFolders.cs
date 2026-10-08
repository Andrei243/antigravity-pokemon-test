using System;
using System.Collections.Generic;
using System.IO;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Where meshed models and baked sprites are kept between runs (plan 16 · T16). Beside the executable
/// (<c>cache/models</c>, <c>cache/sprites</c>) unless <c>POKEMON_CACHE</c> names a folder, which several trees
/// of the repository then share (<c>&lt;cache&gt;/models</c>, <c>&lt;cache&gt;/sprites</c>), so a commit's harness
/// built for a comparison meshes nothing the working tree's has meshed already. In a shared folder a file is
/// never deleted for being older: the other tree may still be at the commit that wrote it. Everything in either
/// folder is made again when missing, so it can be removed whole at any time.
/// </summary>
internal static class CacheFolders
{
    public const string Variable = "POKEMON_CACHE";

    /// <summary>The shared folder <c>POKEMON_CACHE</c> names, or null when it names none.</summary>
    public static string? Shared { get; } = SharedFrom(Environment.GetEnvironmentVariable(Variable));

    /// <summary>Whether older versions of a model or a sprite are deleted as a new one is written.</summary>
    public static bool Prunes => Shared == null;

    public static string Models => Of("models", Shared, AppContext.BaseDirectory);

    public static string Sprites => Of("sprites", Shared, AppContext.BaseDirectory);

    public static string? SharedFrom(string? variable) =>
        string.IsNullOrWhiteSpace(variable) ? null : Path.GetFullPath(variable.Trim());

    /// <summary>The folder of one kind of file: under the shared folder if there is one, else beside the executable.</summary>
    public static string Of(string kind, string? shared, string baseDirectory) =>
        shared != null ? Path.Combine(shared, kind) : Path.Combine(baseDirectory, "cache", kind);

    /// <summary>
    /// Writes a file of a cache folder through a temporary file moved into place, so another process reading
    /// the same folder never finds it half written, then deletes the <paramref name="older"/> files when
    /// <paramref name="prune"/> says so. A folder that can't be written is no error: the file is made again
    /// next time.
    /// </summary>
    public static void Write(string path, Action<Stream> write, Func<IEnumerable<string>> older, bool prune)
    {
        string folder = Path.GetDirectoryName(path)!;
        string temporary = Path.Combine(folder, $".{Path.GetFileName(path)}.{Environment.ProcessId}.{Guid.NewGuid():N}.tmp");
        try
        {
            Directory.CreateDirectory(folder);
            using (var stream = File.Create(temporary)) write(stream);
            File.Move(temporary, path, overwrite: true);
            if (prune)
                foreach (var old in older())
                    if (!old.Equals(path, StringComparison.Ordinal)) File.Delete(old);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            try { File.Delete(temporary); } catch (Exception f) when (f is IOException or UnauthorizedAccessException) { }
        }
    }
}
