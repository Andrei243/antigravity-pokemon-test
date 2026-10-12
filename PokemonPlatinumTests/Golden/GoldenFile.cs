using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PokemonPlatinumTests.Golden;

/// <summary>
/// A golden file (plan 24 · X2): plain text the code gave once and was read, kept in the repository beside this
/// file, which the code must go on giving. A file is made of blocks, each beginning at a line <c>=== name</c> and
/// standing alone (a battle starts with its seed, its rules and its teams), so that a difference is told by the
/// blocks it moved and a <c>git diff</c> reads without the rest of the file.
/// <para>
/// The repository's copy is read, never the build output's. When the text the code gives isn't the file, the new
/// text is written beside it as <c>&lt;name&gt;.received.txt</c> (git-ignored) and the test fails naming the blocks
/// that moved. A change that is meant is accepted with <see cref="AcceptCommand"/>, which rewrites the files; the
/// session that does it reads <c>git diff PokemonPlatinumTests/Golden</c> and says which blocks moved and why.
/// </para>
/// </summary>
internal static class GoldenFile
{
    /// <summary>The environment variable that accepts what the code gives now: <c>GOLDEN=accept</c>.</summary>
    public const string Switch = "GOLDEN";

    public const string AcceptCommand = "GOLDEN=accept dotnet test PokemonPlatinumTests --filter Golden";

    /// <summary>What begins a block, the rest of the line being its name.</summary>
    public const string BlockMark = "=== ";

    /// <summary>How many of the moved blocks a failure names.</summary>
    private const int Named = 20;

    /// <summary>Where the golden files are kept: <c>PokemonPlatinumTests/Golden</c> of the repository.</summary>
    public static string Folder => Path.Combine(Repository.Root, "PokemonPlatinumTests", "Golden");

    /// <summary>Whether this run accepts what the code gives (<c>GOLDEN=accept</c>).</summary>
    public static bool Accepting => string.Equals(Environment.GetEnvironmentVariable(Switch), "accept", StringComparison.OrdinalIgnoreCase);

    /// <summary>Holds the text to the golden file of this name, or writes the file under <c>GOLDEN=accept</c>.</summary>
    public static void Check(string name, string text)
    {
        if (Compare(Folder, name, text, Accepting) is { } why) Assert.Fail(why);
    }

    /// <summary>
    /// Compares the text with the file <paramref name="name"/> of <paramref name="folder"/>: null when they are the
    /// same, or when <paramref name="accept"/> has written the text as the file; otherwise why not, with the new
    /// text left in the received file beside it.
    /// </summary>
    public static string? Compare(string folder, string name, string text, bool accept)
    {
        text = Normalised(text);
        string path = Path.Combine(folder, name), received = ReceivedPath(path);
        string? kept = File.Exists(path) ? Normalised(File.ReadAllText(path)) : null;

        if (accept)
        {
            if (kept != text)
            {
                Directory.CreateDirectory(folder);
                File.WriteAllText(path, text);
            }
            File.Delete(received);
            return null;
        }
        if (kept == text)
        {
            File.Delete(received);
            return null;
        }

        Directory.CreateDirectory(folder);
        File.WriteAllText(received, text);
        if (kept == null)
            return $"The golden file {name} doesn't exist yet (what the code gives is in {received}). Write it with: {AcceptCommand}";

        var before = Blocks(kept);
        var moved = Moved(before, Blocks(text));
        string first = moved.Count > 0 ? FirstDifference(before, Blocks(text), moved[0]) : "";
        string names = string.Join(", ", moved.Take(Named)) + (moved.Count > Named ? $" and {moved.Count - Named} more" : "");
        return $"{moved.Count} of {before.Count} blocks of {name} moved: {names}.{first}\n" +
            $"What the code gives now is in {received}. If the change is meant, accept it with: {AcceptCommand}, " +
            "read git diff PokemonPlatinumTests/Golden and say which blocks moved and why; a block of Platinum's rules " +
            "that moved without a reason is a bug.";
    }

    /// <summary>The file a difference is written to: <c>battles-platinum.txt</c>'s is <c>battles-platinum.received.txt</c>.</summary>
    public static string ReceivedPath(string path) => Path.ChangeExtension(path, ".received.txt");

    /// <summary>A text cut into its blocks by name, in order; the lines before the first block are "(the head)".</summary>
    public static List<(string Name, string Text)> Blocks(string text)
    {
        var blocks = new List<(string Name, List<string> Lines)>();
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string line in Normalised(text).Split('\n'))
        {
            if (line.StartsWith(BlockMark, StringComparison.Ordinal))
            {
                // A name that comes twice is told apart by its count, so that no block hides another
                string name = line[BlockMark.Length..];
                int times = seen[name] = seen.GetValueOrDefault(name) + 1;
                blocks.Add((times == 1 ? name : $"{name} ({times})", new List<string> { line }));
            }
            else
            {
                if (blocks.Count == 0) blocks.Add(("(the head)", new List<string>()));
                blocks[^1].Lines.Add(line);
            }
        }
        return blocks.Select(b => (b.Name, string.Join("\n", b.Lines))).ToList();
    }

    /// <summary>The blocks whose text changed, came or went, in the order of the file as it was and then of the new one.</summary>
    public static List<string> Moved(List<(string Name, string Text)> before, List<(string Name, string Text)> after)
    {
        var then = before.ToDictionary(b => b.Name, b => b.Text, StringComparer.Ordinal);
        var now = after.ToDictionary(b => b.Name, b => b.Text, StringComparer.Ordinal);
        return before.Select(b => b.Name).Concat(after.Select(b => b.Name).Where(n => !then.ContainsKey(n)))
            .Where(n => !then.TryGetValue(n, out var a) || !now.TryGetValue(n, out var b) || a != b)
            .ToList();
    }

    /// <summary>The first line of a moved block that isn't what it was.</summary>
    private static string FirstDifference(List<(string Name, string Text)> before, List<(string Name, string Text)> after, string block)
    {
        string? was = before.FirstOrDefault(b => b.Name == block).Text, @is = after.FirstOrDefault(b => b.Name == block).Text;
        if (was == null) return $" The first, {block}, is new.";
        if (@is == null) return $" The first, {block}, is gone.";
        string[] old = was.Split('\n'), fresh = @is.Split('\n');
        for (int i = 0; i < Math.Max(old.Length, fresh.Length); i++)
        {
            string a = i < old.Length ? old[i] : "(nothing)", b = i < fresh.Length ? fresh[i] : "(nothing)";
            if (a != b) return $"\nIn {block}, line {i + 1} of the block was\n    {a}\nand is now\n    {b}";
        }
        return "";
    }

    /// <summary>One line ending, and one at the end of the file.</summary>
    private static string Normalised(string text)
    {
        text = text.ReplaceLineEndings("\n");
        return text.EndsWith('\n') ? text : text + "\n";
    }
}
