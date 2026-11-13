using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// The game's small data files as one file (GPU-free). The maps, the worlds and the scripts are some eleven thousand
/// files of a few kilobytes each, and opening them one by one is most of a slow start: each open is checked by the
/// system, and a file written a moment ago (a build, a new checkout) costs many times a file known already. So the
/// game keeps a copy of them all in one archive in its cache folder, made by itself and never by hand:
/// <list type="bullet">
/// <item>As it starts, the folders are listed (names, sizes and times of writing: no file is opened) and the list's
/// hash is the archive's <see cref="Fingerprint"/>. An archive of that fingerprint is read in one piece; with none, every
/// file is read on all cores at once, served from memory, and written out as the archive behind the game's back.</item>
/// <item>The loose files stay what the game is made of: a file changed, added or taken away changes the fingerprint,
/// and the archive is made again. Nothing reads the archive but through <see cref="Exists"/> and <see cref="ReadText"/>,
/// which answer from the files themselves for anything it doesn't cover (a pack's folder, another data folder, a path
/// with a way up in it) and whenever none is in use.</item>
/// <item>Only the game itself puts one in use (<c>Program.Main</c>, never with <c>--safe-mode</c> or developer mode,
/// whose files change under it): a test's or a tool's engine reads the files as it always did.</item>
/// </list>
/// The cache folder can be removed whole at any time.
/// </summary>
public sealed class DataArchive
{
    /// <summary>The folders of the data the archive holds, whole.</summary>
    public static readonly string[] Folders = { "maps", "scripts", "world" };

    private const uint Magic = 0x31414450; // "PDA1"
    private const string Prefix = "data-", Suffix = ".pak";

    private static volatile DataArchive? inUse;

    /// <summary>The archive the game reads through, or null: every read is then the file's own.</summary>
    public static DataArchive? InUse => inUse;

    /// <summary>Puts an archive in use (null for none).</summary>
    public static void Use(DataArchive? archive) => inUse = archive;

    /// <summary>Whether a data file is there: by the archive in use for what it covers, by the disk for the rest.</summary>
    public static bool Exists(string path) => inUse is { } archive ? archive.Has(path) : File.Exists(path);

    /// <summary>A data file's text, read as <see cref="File.ReadAllText(string)"/> reads it.</summary>
    public static string ReadText(string path) => inUse is { } archive ? archive.TextOf(path) : File.ReadAllText(path);

    /// <summary>What <see cref="Exists"/> answers with this archive in use.</summary>
    public bool Has(string path) => KeyOf(path) is { } key ? entries.ContainsKey(key) : File.Exists(path);

    /// <summary>What <see cref="ReadText"/> answers with this archive in use.</summary>
    public string TextOf(string path) =>
        KeyOf(path) is { } key && entries.TryGetValue(key, out var entry) ? Decode(entry) : File.ReadAllText(path);

    private readonly record struct Entry(byte[] Buffer, int Offset, int Length);

    private readonly string root;
    private readonly Dictionary<string, Entry> entries;

    /// <summary>The hash of the listing the archive was made from, sixteen hexadecimal digits.</summary>
    public string Fingerprint { get; }

    /// <summary>How many files it holds.</summary>
    public int Count => entries.Count;

    /// <summary>Whether it was read from the cache (true) or made from the loose files this time.</summary>
    public bool FromCache { get; }

    /// <summary>The writing of a newly made archive into the cache, for whoever must see it done (a test); null when it was read.</summary>
    public Task? Written { get; private set; }

    private DataArchive(string root, Dictionary<string, Entry> entries, string fingerprint, bool fromCache)
    {
        this.root = root;
        this.entries = entries;
        Fingerprint = fingerprint;
        FromCache = fromCache;
    }

    // A file's name on a system whose names don't tell upper case from lower is found either way
    private static StringComparer Names => OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    /// <summary>
    /// The archive of a data folder: read from <paramref name="cacheFolder"/> when the one there was made from the files
    /// as they are now, otherwise made from them (and written there in the background, where
    /// <paramref name="prune"/> also takes the older ones away).
    /// </summary>
    public static DataArchive Open(string dataRoot, string cacheFolder, bool prune = true)
    {
        string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(dataRoot)) + Path.DirectorySeparatorChar;
        var listing = List(root);
        string fingerprint = FingerprintOf(listing);
        string file = Path.Combine(cacheFolder, Prefix + fingerprint + Suffix);
        if (TryRead(file, root, fingerprint) is { } read) return read;

        // No archive of these files yet: each is read and packed on whichever core is free
        var packed = new Entry[listing.Count];
        Parallel.For(0, listing.Count, i =>
        {
            byte[] raw = File.ReadAllBytes(Path.Combine(root, listing[i].Key));
            using var into = new MemoryStream(raw.Length / 4 + 64);
            using (var deflate = new DeflateStream(into, CompressionLevel.Fastest, leaveOpen: true)) deflate.Write(raw);
            packed[i] = new Entry(into.GetBuffer(), 0, (int)into.Length);
        });
        var entries = new Dictionary<string, Entry>(listing.Count, Names);
        for (int i = 0; i < listing.Count; i++) entries[listing[i].Key] = packed[i];
        var made = new DataArchive(root, entries, fingerprint, fromCache: false);
        var keys = listing.Select(l => l.Key).ToList();
        made.Written = Task.Run(() => Write(file, cacheFolder, fingerprint, keys, packed, prune));
        return made;
    }

    // The path under the root with forward slashes, or null for a path the archive has no say over
    private string? KeyOf(string path)
    {
        if (path.Length <= root.Length || !SameStart(path)) return null;
        string rest = path[root.Length..].Replace('\\', '/');
        if (rest.Contains("..", StringComparison.Ordinal) || rest.Contains("//", StringComparison.Ordinal)) return null;
        int slash = rest.IndexOf('/');
        if (slash <= 0) return null;
        foreach (string folder in Folders)
            if (slash == folder.Length && string.Compare(rest, 0, folder, 0, slash, StringComparison.OrdinalIgnoreCase) == 0)
                return rest;
        return null;
    }

    private bool SameStart(string path)
    {
        for (int i = 0; i < root.Length; i++)
        {
            char a = path[i], b = root[i];
            if (a == b) continue;
            bool separators = (a == '/' || a == '\\') && (b == '/' || b == '\\');
            if (!separators && char.ToUpperInvariant(a) != char.ToUpperInvariant(b)) return false;
        }
        return true;
    }

    private static string Decode(Entry entry)
    {
        using var packed = new MemoryStream(entry.Buffer, entry.Offset, entry.Length, writable: false);
        using var deflate = new DeflateStream(packed, CompressionMode.Decompress);
        using var reader = new StreamReader(deflate, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    /// <summary>Every file of the covered folders: its key, its size and when it was written, in the keys' order. No file is opened.</summary>
    private static List<(string Key, long Length, long Written)> List(string root)
    {
        var found = new List<(string, long, long)>();
        foreach (string folder in Folders)
        {
            var info = new DirectoryInfo(Path.Combine(root, folder));
            if (!info.Exists) continue;
            foreach (var file in info.EnumerateFiles("*", SearchOption.AllDirectories))
                found.Add((file.FullName[root.Length..].Replace('\\', '/'), file.Length, file.LastWriteTimeUtc.Ticks));
        }
        found.Sort((a, b) => string.CompareOrdinal(a.Item1, b.Item1));
        return found;
    }

    // FNV-1a of 64 bits (a string's own hash changes from run to run)
    private static string FingerprintOf(List<(string Key, long Length, long Written)> listing)
    {
        const ulong offset = 14695981039346656037UL, prime = 1099511628211UL;
        ulong hash = offset;
        void Mix(ulong value)
        {
            for (int i = 0; i < 8; i++, value >>= 8) hash = (hash ^ (byte)value) * prime;
        }
        foreach (var (key, length, written) in listing)
        {
            foreach (char c in key) hash = (hash ^ c) * prime;
            Mix((ulong)length);
            Mix((ulong)written);
        }
        return hash.ToString("x16");
    }

    private static DataArchive? TryRead(string file, string root, string fingerprint)
    {
        try
        {
            if (!File.Exists(file)) return null;
            byte[] all = File.ReadAllBytes(file);
            using var reader = new BinaryReader(new MemoryStream(all, writable: false), Encoding.UTF8);
            if (reader.ReadUInt32() != Magic || reader.ReadString() != fingerprint) return null;
            int count = reader.ReadInt32();
            var index = new (string Key, int Offset, int Length)[count];
            for (int i = 0; i < count; i++) index[i] = (reader.ReadString(), reader.ReadInt32(), reader.ReadInt32());
            int start = (int)reader.BaseStream.Position;
            var entries = new Dictionary<string, Entry>(count, Names);
            foreach (var (key, offset, length) in index)
            {
                if (offset < 0 || length < 0 || (long)start + offset + length > all.Length) return null;
                entries[key] = new Entry(all, start + offset, length);
            }
            return new DataArchive(root, entries, fingerprint, fromCache: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or EndOfStreamException or ArgumentException or FormatException)
        {
            // An archive that can't be read is as good as none: it is made again
            return null;
        }
    }

    private static void Write(string file, string folder, string fingerprint, List<string> keys, Entry[] packed, bool prune)
    {
        Graphics.CacheFolders.Write(file, stream =>
        {
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            writer.Write(Magic);
            writer.Write(fingerprint);
            writer.Write(keys.Count);
            int offset = 0;
            for (int i = 0; i < keys.Count; i++)
            {
                writer.Write(keys[i]);
                writer.Write(offset);
                writer.Write(packed[i].Length);
                offset += packed[i].Length;
            }
            foreach (var entry in packed) writer.Write(entry.Buffer, entry.Offset, entry.Length);
        }, () => Directory.GetFiles(folder, Prefix + "*" + Suffix), prune);
    }
}
