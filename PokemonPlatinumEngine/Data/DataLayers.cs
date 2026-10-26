using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// What a pack says of itself, its <c>pack.json</c> (plan 16 · T14): its name (which the options and the save go by),
/// its version, who made it and the credit it must carry, the game version it was made for, the packs it loads after,
/// and a line saying what it is.
/// </summary>
public sealed record PackManifest(string Name, string Version, string? Author, string? Credits, string? GameVersion,
    IReadOnlyList<string> After, string? Description);

/// <summary>A root the data files are read from: the game's own <c>Data</c> (no pack), or a pack's folder.</summary>
public sealed record DataLayer(string Root, PackManifest? Pack)
{
    public string Name => Pack?.Name ?? "game";
}

/// <summary>A pack the options name: in that order, on or off (plan 16 · T14; the screen that changes them is T15's).</summary>
public sealed class PackChoice
{
    public string Name { get; set; } = "";
    public bool Enabled { get; set; } = true;
}

/// <summary>A pack's folder that can't be read as a pack: its <c>pack.json</c> named, and what is wrong with it.</summary>
public sealed class PackException : Exception
{
    public PackException(string message) : base(message) { }
}

/// <summary>
/// The data layers (plan 16 · T14; GPU-free): the roots the data files are read from, in load order. The game's own
/// <c>Data</c> (<see cref="GameDataFiles.Directory"/>) is always first; each pack enabled after it, from the
/// <c>packs</c> folders beside the game and beside the saves (<see cref="Discover"/>, <see cref="Start"/>). With no
/// pack in use (the harness, every test, <c>--safe-mode</c>, <c>--packs none</c>) every path is what it was.
/// <list type="bullet">
/// <item><see cref="PathOf"/>: a file a later layer has replaces the earlier ones' whole, but <c>world.json</c>
/// (what is open is the game's), which is always the game's own.</item>
/// <item><see cref="PatchesOf"/> and <see cref="Patch"/>: a <c>&lt;file&gt;.patch.json</c> in a pack patches the file of
/// that name as the importer's <c>Overrides</c> do: entries of an array by <c>name</c> (<c>id</c> for
/// <c>trainers.json</c>), each field given replacing the field whole, <c>null</c> taking it away, an entry of a name
/// that isn't there added; an object (<c>characters.json</c>) merged key by key, the same way, all the way down.</item>
/// <item><see cref="FilesIn"/>: every file of a folder across the layers, a later file replacing an earlier of the
/// same relative name: what the scripts, the maps and the songs are read from.</item>
/// </list>
/// </summary>
public static class DataLayers
{
    public const string PacksFolder = "packs", ManifestFile = "pack.json", PatchSuffix = ".patch.json";

    /// <summary>The files a pack may patch, and the field each array's entries are found by.</summary>
    public static IReadOnlyDictionary<string, string> Patchable { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["species.json"] = "name", ["moves.json"] = "name", ["abilities.json"] = "name", ["items.json"] = "name",
        ["characters.json"] = "name", ["trainers.json"] = "id"
    };

    private static IReadOnlyList<DataLayer> packs = Array.Empty<DataLayer>();

    /// <summary>The packs in use, in load order, after the game's own data.</summary>
    public static IReadOnlyList<DataLayer> Packs => packs;

    /// <summary>Every layer in load order: the game's own data first.</summary>
    public static IReadOnlyList<DataLayer> All => new[] { new DataLayer(GameDataFiles.Directory, null) }.Concat(packs).ToList();

    /// <summary>Puts these packs in use, over the game's own data (nothing for none). The data version is worked out again.</summary>
    public static void Use(IEnumerable<DataLayer>? layers)
    {
        packs = layers?.Where(l => l.Pack != null).ToList() ?? (IReadOnlyList<DataLayer>)Array.Empty<DataLayer>();
        GameDataFiles.ForgetVersion();
    }

    /// <summary>Whether a file is read from the game's own data whatever the packs hold: a region's <c>world.json</c>.</summary>
    public static bool IsFixed(string relativePath) =>
        string.Equals(Path.GetFileName(relativePath), "world.json", StringComparison.OrdinalIgnoreCase);

    /// <summary>The file a relative path is read from: the last layer that has it, or the game's own path.</summary>
    public static string PathOf(string relativePath, IReadOnlyList<DataLayer>? over = null)
    {
        var layers = over ?? packs;
        string own = Path.Combine(GameDataFiles.Directory, relativePath);
        if (layers.Count == 0 || Path.IsPathRooted(relativePath) || IsFixed(relativePath)) return own;
        for (int i = layers.Count - 1; i >= 0; i--)
        {
            string path = Path.Combine(layers[i].Root, relativePath);
            if (File.Exists(path)) return path;
        }
        return own;
    }

    /// <summary>The patches of a file the packs carry, in load order (none for a file that can't be patched).</summary>
    public static IReadOnlyList<string> PatchesOf(string relativePath, IReadOnlyList<DataLayer>? over = null)
    {
        var layers = over ?? packs;
        if (layers.Count == 0 || Path.IsPathRooted(relativePath) || !Patchable.ContainsKey(Path.GetFileName(relativePath))) return Array.Empty<string>();
        string patch = Path.ChangeExtension(relativePath, null) + PatchSuffix;
        return layers.Select(p => Path.Combine(p.Root, patch)).Where(File.Exists).ToList();
    }

    /// <summary>
    /// Every file of a folder across the layers whose name matches, keyed by its path under the folder with forward
    /// slashes, a later layer's file replacing an earlier's of the same key; in the keys' order.
    /// </summary>
    public static IReadOnlyList<(string Key, string Path)> FilesIn(string relativeFolder, string pattern, bool recursive = false, IReadOnlyList<DataLayer>? over = null)
    {
        var found = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var roots = new List<string> { Path.Combine(GameDataFiles.Directory, relativeFolder) };
        roots.AddRange((over ?? packs).Select(p => Path.Combine(p.Root, relativeFolder)));
        foreach (string root in roots)
        {
            if (!Directory.Exists(root)) continue;
            foreach (string path in Directory.EnumerateFiles(root, pattern, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                found[Path.GetRelativePath(root, path).Replace('\\', '/')] = path;
        }
        return found.Select(kv => (kv.Key, kv.Value)).ToList();
    }

    /// <summary>
    /// A file's text with every pack's patch laid over it, in load order; the file as it is when there is no patch.
    /// A patch that can't be read stops the start with its file named.
    /// </summary>
    public static string ReadPatched(string relativePath, IReadOnlyList<DataLayer>? over = null)
    {
        string path = PathOf(relativePath, over);
        string text = File.ReadAllText(path);
        var patches = PatchesOf(relativePath, over);
        if (patches.Count == 0) return text;
        var options = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
        var root = JsonNode.Parse(text, documentOptions: options) ?? throw new InvalidDataException($"Game data file is empty: {path}");
        string key = Patchable[Path.GetFileName(relativePath)];
        foreach (string patchPath in patches)
        {
            JsonNode? patch;
            try
            {
                patch = JsonNode.Parse(File.ReadAllText(patchPath), documentOptions: options);
            }
            catch (JsonException e)
            {
                throw new InvalidDataException($"The patch {patchPath} is malformed: {e.Message}", e);
            }
            if (patch == null) continue;
            root = Patch(root, patch, key, patchPath);
        }
        return root.ToJsonString();
    }

    /// <summary>
    /// One patch laid over a file's content: an array of entries by <paramref name="key"/>, each field given replacing
    /// the field whole and <c>null</c> taking it away, a new key added; an object merged key by key all the way down.
    /// </summary>
    public static JsonNode Patch(JsonNode target, JsonNode patch, string key, string patchName = "the patch")
    {
        switch (target, patch)
        {
            case (JsonArray entries, JsonArray changes):
                foreach (var change in changes)
                {
                    if (change is not JsonObject fields || fields[key] is not JsonValue named)
                        throw new InvalidDataException($"{patchName}: every entry needs its \"{key}\".");
                    string name = named.ToString();
                    var entry = entries.OfType<JsonObject>().FirstOrDefault(e => e[key]?.ToString() == name);
                    if (entry == null)
                    {
                        entries.Add(JsonNode.Parse(fields.ToJsonString()));
                        continue;
                    }
                    foreach (var (field, value) in fields.ToList())
                    {
                        if (value == null) entry.Remove(field);
                        else entry[field] = JsonNode.Parse(value.ToJsonString());
                    }
                }
                return entries;
            case (JsonObject whole, JsonObject changes):
                MergeObject(whole, changes);
                return whole;
            default:
                throw new InvalidDataException($"{patchName} is not the shape of the file it patches.");
        }
    }

    private static void MergeObject(JsonObject target, JsonObject changes)
    {
        foreach (var (field, value) in changes.ToList())
        {
            if (value == null) target.Remove(field);
            else if (value is JsonObject inner && target[field] is JsonObject existing) MergeObject(existing, inner);
            else target[field] = JsonNode.Parse(value.ToJsonString());
        }
    }

    /// <summary>Reads a pack's <c>pack.json</c>; a manifest that can't be read is named with what is wrong with it.</summary>
    public static PackManifest ReadManifest(string folder)
    {
        string path = Path.Combine(folder, ManifestFile);
        if (!File.Exists(path)) throw new PackException($"{path}: there is no {ManifestFile}.");
        JsonObject? json;
        try
        {
            json = JsonNode.Parse(File.ReadAllText(path), documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true }) as JsonObject;
        }
        catch (JsonException e)
        {
            throw new PackException($"{path}: it isn't JSON ({e.Message}).");
        }
        if (json == null) throw new PackException($"{path}: it should be an object.");
        string? Text(string field) => json[field] is JsonValue v && v.TryGetValue(out string? s) ? s : null;
        string name = Text("name") ?? throw new PackException($"{path}: it has no \"name\".");
        if (name.Length == 0 || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) throw new PackException($"{path}: \"{name}\" can't be a pack's name.");
        string version = Text("version") ?? throw new PackException($"{path}: it has no \"version\".");
        if (Text("credits") is not { Length: > 0 } credits) throw new PackException($"{path}: it has no \"credits\": a pack says whose work it is.");
        var after = new List<string>();
        if (json["after"] is JsonArray list)
            foreach (var item in list)
                after.Add(item is JsonValue v && v.TryGetValue(out string? s) ? s : throw new PackException($"{path}: \"after\" names packs, each a string."));
        else if (json["after"] != null) throw new PackException($"{path}: \"after\" is a list of packs' names.");
        return new PackManifest(name, version, Text("author"), credits, Text("gameVersion"), after, Text("description"));
    }

    /// <summary>
    /// The packs of these folders (each a folder of packs, one pack to a folder in it), in the order they load: the
    /// options' order first (<paramref name="chosen"/>; a pack it turns off is left out), the rest by name, then each
    /// moved after the packs its manifest names in <c>after</c>. A pack that can't be read, or a second of one name,
    /// is left out and said in <paramref name="problems"/>.
    /// </summary>
    public static List<DataLayer> Discover(IEnumerable<string> packFolders, IReadOnlyList<PackChoice>? chosen, List<string> problems)
    {
        var found = new List<DataLayer>();
        foreach (string folder in packFolders.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase))
            foreach (string dir in Directory.GetDirectories(folder).OrderBy(d => d, StringComparer.Ordinal))
            {
                try
                {
                    var manifest = ReadManifest(dir);
                    if (found.Any(l => string.Equals(l.Pack!.Name, manifest.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        problems.Add($"{Path.Combine(dir, ManifestFile)}: a pack called \"{manifest.Name}\" is loaded already; this one is left out.");
                        continue;
                    }
                    found.Add(new DataLayer(dir, manifest));
                }
                catch (PackException e)
                {
                    problems.Add(e.Message);
                }
            }

        chosen ??= Array.Empty<PackChoice>();
        int Rank(DataLayer l)
        {
            int at = chosen.ToList().FindIndex(c => string.Equals(c.Name, l.Pack!.Name, StringComparison.OrdinalIgnoreCase));
            return at < 0 ? int.MaxValue : at;
        }
        var on = found.Where(l => !chosen.Any(c => !c.Enabled && string.Equals(c.Name, l.Pack!.Name, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(Rank).ThenBy(l => l.Pack!.Name, StringComparer.Ordinal).ToList();

        // Each pack after those it names: taken in order, a pack whose "after" are all placed (or not loaded) goes next
        var ordered = new List<DataLayer>();
        var left = new List<DataLayer>(on);
        while (left.Count > 0)
        {
            var next = left.FirstOrDefault(l => l.Pack!.After.All(a => ordered.Any(o => string.Equals(o.Pack!.Name, a, StringComparison.OrdinalIgnoreCase))
                || !left.Any(o => string.Equals(o.Pack!.Name, a, StringComparison.OrdinalIgnoreCase))));
            if (next == null)
            {
                problems.Add($"The packs {string.Join(", ", left.Select(l => l.Pack!.Name))} each name another to load after: they load in the options' order.");
                ordered.AddRange(left);
                break;
            }
            ordered.Add(next);
            left.Remove(next);
        }
        return ordered;
    }

    /// <summary>The folders packs are looked for in: beside the game, and beside the saves.</summary>
    public static IReadOnlyList<string> PackFolders(string saveFolder) =>
        new[] { Path.Combine(AppContext.BaseDirectory, PacksFolder), Path.Combine(saveFolder, PacksFolder) };
}
