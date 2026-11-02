using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Whether the hand-made files beside the game are read at all (plan 16 · T3): the models of
/// <c>overrides/models</c> (<see cref="ModelOverrides"/>), the people's sprites of <c>overrides/sprites</c>
/// (<see cref="CharacterSprites"/>) and the Pokémon's field sprites under it (<see cref="FieldSprites"/>). They are,
/// unless the game was started with <c>--safe-mode</c>, which shows the game as it is built: a file of the player's
/// own that the game can't take is then not what keeps it from starting. Whatever reads an override asks here first.
/// <para>
/// Packs carry the same folders (plan 16 · T14): <see cref="PackRoots"/> is every pack in use, the latest first, and
/// <see cref="FoldersFor"/> is where an override is looked for, each pack's <c>overrides/…</c> before the game's own,
/// so a later pack's file wins over an earlier pack's and both over the one beside the game. GPU-free.
/// </para>
/// </summary>
internal static class Overrides
{
    public static bool Enabled { get; set; } = true;

    /// <summary>The folders of the packs in use, the latest pack first (<c>Packs.Start</c> fills it; tests may).</summary>
    public static List<string> PackRoots { get; } = new();

    /// <summary>
    /// Where an override of one kind is looked for, in order: each pack's <c>overrides/&lt;under…&gt;</c>, the latest
    /// first, then <paramref name="ownFolder"/>, the game's own.
    /// </summary>
    public static IReadOnlyList<string> FoldersFor(string ownFolder, params string[] under) =>
        PackRoots.Select(root => Path.Combine(new[] { root, "overrides" }.Concat(under).ToArray())).Append(ownFolder).ToList();

    /// <summary>The first of <paramref name="folders"/> that holds <paramref name="relative"/> as a file, or null.</summary>
    public static string? FindFile(IEnumerable<string> folders, string relative)
    {
        foreach (string folder in folders)
        {
            string path = Path.Combine(folder, relative);
            if (File.Exists(path)) return path;
        }
        return null;
    }

    /// <summary>The first of <paramref name="folders"/> that holds <paramref name="relative"/> as a folder, or null.</summary>
    public static string? FindFolder(IEnumerable<string> folders, string relative)
    {
        foreach (string folder in folders)
        {
            string path = Path.Combine(folder, relative);
            if (Directory.Exists(path)) return path;
        }
        return null;
    }
}
