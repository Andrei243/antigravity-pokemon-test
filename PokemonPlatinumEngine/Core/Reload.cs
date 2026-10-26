using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PokemonPlatinumEngine.Core;

/// <summary>What a change of a file asks to be read again (plan 16 · T6): a set of these.</summary>
[Flags]
public enum ReloadKind
{
    None = 0,
    /// <summary>A script file, <c>Data/scripts/*.txt</c>.</summary>
    Scripts = 1,
    /// <summary>A room's file, <c>Data/maps/*.json</c>.</summary>
    Rooms = 2,
    /// <summary>An overlay of a world, <c>Data/world/&lt;region&gt;/overlays/*.json</c>: every map is made again.</summary>
    Overlays = 4,
    /// <summary>A song, <c>Data/music/**/*.mml</c>.</summary>
    Music = 8,
    /// <summary>A model file in <c>overrides/models</c> (or another folder <c>ModelOverrides</c> reads).</summary>
    Models = 16,
    All = Scripts | Rooms | Overlays | Music | Models
}

/// <summary>
/// Which files the developer's live reload cares for and what each asks for (plan 16 · T6), and the changes noted
/// until the files have been quiet a moment (<see cref="ReloadQueue"/>): an editor writes a file in several steps,
/// and a save of many files at once is read as one. No drawing, no input, no game.
/// </summary>
public static class Reload
{
    /// <summary>How long the files must have been left alone before what changed is read: a quarter of a second.</summary>
    public const double QuietSeconds = 0.25;

    /// <summary>What a file at <paramref name="path"/> asks for, by where it is under the data folder or among the model folders.</summary>
    public static ReloadKind KindOf(string dataFolder, string path, IEnumerable<string>? modelFolders = null)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        if (modelFolders != null && ext is (".glb" or ".gltf")
            && modelFolders.Any(folder => SameFolder(Path.GetDirectoryName(path), folder)))
            return ReloadKind.Models;

        string relative = Path.GetRelativePath(dataFolder, path).Replace('\\', '/');
        if (relative.StartsWith("../", StringComparison.Ordinal) || Path.IsPathRooted(relative)) return ReloadKind.None;
        var parts = relative.Split('/');
        if (parts.Length == 2 && parts[0] == "scripts" && ext == ".txt") return ReloadKind.Scripts;
        if (parts.Length == 2 && parts[0] == "maps" && ext == ".json") return ReloadKind.Rooms;
        if (parts.Length == 4 && parts[0] == "world" && parts[2] == "overlays" && ext == ".json") return ReloadKind.Overlays;
        if (parts.Length >= 2 && parts[0] == "music" && ext == ".mml") return ReloadKind.Music;
        return ReloadKind.None;
    }

    private static bool SameFolder(string? a, string b) =>
        a != null && string.Equals(Path.GetFullPath(a).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(b).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);

    /// <summary>A change's words for the toast and the console: <c>scripts, rooms</c>.</summary>
    public static string Describe(ReloadKind kinds) =>
        kinds == ReloadKind.None ? "nothing"
            : string.Join(", ", Enum.GetValues<ReloadKind>().Where(k => k is not (ReloadKind.None or ReloadKind.All) && kinds.HasFlag(k))
                .Select(k => k.ToString().ToLowerInvariant()));

    /// <summary>The kind a word of the console names (<c>reload scripts</c>), or null.</summary>
    public static ReloadKind? Named(string word) => word.ToLowerInvariant() switch
    {
        "scripts" => ReloadKind.Scripts,
        "maps" or "rooms" or "overlays" => ReloadKind.Rooms | ReloadKind.Overlays,
        "music" => ReloadKind.Music,
        "models" => ReloadKind.Models,
        "all" => ReloadKind.All,
        _ => null
    };
}

/// <summary>A batch of changes taken off the queue: what they ask for, and the files (full paths) that changed.</summary>
public sealed record ReloadBatch(ReloadKind Kinds, IReadOnlyList<string> Paths);

/// <summary>
/// Changes of files noted as they come, from any thread, and handed over once nothing has changed for
/// <see cref="Reload.QuietSeconds"/> (plan 16 · T6). Its clock is the caller's, so a test holds the debounce still.
/// </summary>
public sealed class ReloadQueue
{
    private readonly object gate = new();
    private ReloadKind kinds;
    private readonly SortedSet<string> paths = new(StringComparer.Ordinal);
    private double lastChange = double.NegativeInfinity;

    /// <summary>A file changed at <paramref name="now"/> (seconds of the caller's clock).</summary>
    public void Note(string path, ReloadKind kind, double now)
    {
        if (kind == ReloadKind.None) return;
        lock (gate)
        {
            kinds |= kind;
            paths.Add(path);
            lastChange = now;
        }
    }

    /// <summary>Whether anything is waiting, quiet or not.</summary>
    public bool Waiting
    {
        get { lock (gate) return kinds != ReloadKind.None; }
    }

    /// <summary>What changed, once the files have been quiet long enough; null while they haven't, or when nothing has.</summary>
    public ReloadBatch? Take(double now)
    {
        lock (gate)
        {
            if (kinds == ReloadKind.None || now - lastChange < Reload.QuietSeconds) return null;
            var batch = new ReloadBatch(kinds, paths.ToList());
            kinds = ReloadKind.None;
            paths.Clear();
            return batch;
        }
    }
}

/// <summary>
/// The developer's watch on the game's files (plan 16 · T6): a <see cref="FileSystemWatcher"/> over the data folder
/// (the scripts, the rooms, the worlds' overlays, the music) and one over each model folder that is there, their
/// changes noted on a <see cref="ReloadQueue"/> by a clock of its own. The engine takes what is quiet once a frame.
/// </summary>
public sealed class ReloadWatch : IDisposable
{
    private readonly List<FileSystemWatcher> watchers = new();
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly string dataFolder;
    private readonly List<string> modelFolders;

    public ReloadQueue Queue { get; } = new();

    public ReloadWatch(string dataFolder, IEnumerable<string> modelFolders)
    {
        this.dataFolder = dataFolder;
        this.modelFolders = modelFolders.Distinct().ToList();
        Watch(dataFolder, subfolders: true);
        foreach (string folder in this.modelFolders) Watch(folder, subfolders: false);
    }

    private void Watch(string folder, bool subfolders)
    {
        if (!Directory.Exists(folder)) return;
        var watcher = new FileSystemWatcher(folder)
        {
            IncludeSubdirectories = subfolders,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
        };
        watcher.Changed += (_, e) => Noted(e.FullPath);
        watcher.Created += (_, e) => Noted(e.FullPath);
        watcher.Deleted += (_, e) => Noted(e.FullPath);
        watcher.Renamed += (_, e) => Noted(e.FullPath);
        watcher.EnableRaisingEvents = true;
        watchers.Add(watcher);
    }

    private void Noted(string path) => Queue.Note(path, Reload.KindOf(dataFolder, path, modelFolders), clock.Elapsed.TotalSeconds);

    /// <summary>What changed, once quiet; null otherwise.</summary>
    public ReloadBatch? Take() => Queue.Take(clock.Elapsed.TotalSeconds);

    public void Dispose()
    {
        foreach (var watcher in watchers) watcher.Dispose();
        watchers.Clear();
    }
}
