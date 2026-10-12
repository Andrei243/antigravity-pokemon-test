using System.Security.Cryptography;

namespace PokemonPlatinumEngine.Core.Diagnostics;

/// <summary>What checking the game's files against the manifest came to.</summary>
/// <param name="Code">What <c>--verify</c> exits with: <see cref="Manifest.AsShipped"/>, <see cref="Manifest.Differs"/> or <see cref="Manifest.NoManifest"/>.</param>
/// <param name="Lines">What it prints: a line to each file that is missing or changed, and a last line that sums up.</param>
public sealed record Verification(int Code, IReadOnlyList<string> Lines);

/// <summary>
/// <c>--verify</c> (plan 16 · T3): the game's files held against <c>manifest.sha256</c>, the list a release build
/// writes beside the executable (plan 16 · T2: over <c>Data</c> and <c>Assets</c>). The list is in the form
/// <c>sha256sum</c> writes and reads: a line to a file, its SHA-256 as 64 hexadecimal digits, a space, a space or
/// an asterisk, and the file's path from the folder the list is in, with forward slashes; a blank line and one that
/// begins with <c>#</c> say nothing. Files the list doesn't name are nobody's business (a save, the caches,
/// <c>overrides/</c>, packs).
/// <para>
/// No build writes the list yet, so today the answer is always <see cref="NoManifest"/>, said plainly. No drawing,
/// no input, and no window: the check runs before one is opened.
/// </para>
/// </summary>
public static class Manifest
{
    public const string FileName = "manifest.sha256";

    /// <summary>Every file the list names is there and is what the list says.</summary>
    public const int AsShipped = 0;

    /// <summary>A file is missing or changed, or a line of the list can't be read.</summary>
    public const int Differs = 3;

    /// <summary>There is no list to check against: this build wasn't made by a release.</summary>
    public const int NoManifest = 4;

    /// <summary>Checks the files of <paramref name="folder"/> (the game's own: <see cref="AppContext.BaseDirectory"/>) against the list in it.</summary>
    public static Verification Verify(string folder)
    {
        string path = Path.Combine(folder, FileName);
        if (!File.Exists(path))
            return new Verification(NoManifest, new[]
            {
                $"There is no {FileName} beside the game, so there is nothing to check its files against.",
                "Only a release build has one; a build made from the source doesn't."
            });

        var lines = new List<string>();
        int named = 0, wrong = 0, number = 0;
        string root = Path.GetFullPath(folder);
        foreach (string raw in File.ReadLines(path))
        {
            number++;
            string line = raw.Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            if (!TryParse(line, out string hash, out string file))
            {
                lines.Add($"unreadable  line {number} of {FileName}");
                wrong++;
                continue;
            }
            named++;
            string full = Path.GetFullPath(Path.Combine(root, file));
            // A list must name the game's own files and nothing else
            if (!full.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                lines.Add($"outside     {file}");
                wrong++;
            }
            else if (!File.Exists(full))
            {
                lines.Add($"missing     {file}");
                wrong++;
            }
            else if (!string.Equals(HashOf(full), hash, StringComparison.OrdinalIgnoreCase))
            {
                lines.Add($"changed     {file}");
                wrong++;
            }
        }
        lines.Add(wrong == 0
            ? $"{named} file{(named == 1 ? "" : "s")} checked against {FileName}: all as shipped."
            : $"{named} file{(named == 1 ? "" : "s")} checked against {FileName}: {wrong} not as shipped.");
        return new Verification(wrong == 0 ? AsShipped : Differs, lines);
    }

    /// <summary>A line of the list taken apart: its hash and its file. False for a line that isn't one.</summary>
    public static bool TryParse(string line, out string hash, out string file)
    {
        hash = file = "";
        if (line.Length < 67 || line[64] != ' ' || line[65] is not (' ' or '*')) return false;
        hash = line[..64];
        file = line[66..].Trim();
        return file.Length > 0 && hash.All(Uri.IsHexDigit);
    }

    /// <summary>A file's SHA-256 as the list writes it: 64 hexadecimal digits in lower case.</summary>
    public static string HashOf(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    /// <summary>A line of the list for a file, as a release build writes it.</summary>
    public static string LineFor(string folder, string file) =>
        $"{HashOf(Path.Combine(folder, file))}  {file.Replace('\\', '/')}";
}
