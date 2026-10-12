using System.Text.Json;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace ContentCheck;

/// <summary>
/// The hand-written files of a data folder as the checks read them: every region's world with its overlays, every
/// room of <c>maps/</c> and every script, each kept with its file's name and text, and the maps the game makes of
/// them. A folder is the game's own <c>Data</c> or any folder laid out like it (a test's, with something broken in
/// it); the species, moves, items, trainers and songs they are held against are always the game's own.
/// <para>
/// Reading never throws: a file that can't be read, and a world or a room that would stop the game as it loads, is
/// a finding of its own (<see cref="Unreadable"/>), and what depends on it is left out of the checks.
/// </para>
/// </summary>
public sealed class GameContent
{
    /// <summary>One region's world: its folder under the data folder, its overlays and the maps built of it (null when it wouldn't load).</summary>
    public sealed record Region(string Folder, World World, IReadOnlyList<OverlaySource> Overlays, IReadOnlyList<Map>? Maps);

    /// <summary>An overlay as it is on disk: the area its file is named for, the file, its text, and what was read of it (null when it can't be).</summary>
    public sealed record OverlaySource(string Key, string File, string Text, WorldOverlayFile? Overlay);

    /// <summary>A room as it is on disk: the name its file gives it, the file, its text, what was read of it and the map made of that.</summary>
    public sealed record Room(string Name, string File, string Text, MapFile? Source, Map? Map);

    /// <summary>The data folder the files were read from.</summary>
    public string Root { get; }

    public IReadOnlyList<Region> Regions => regions;
    public IReadOnlyList<Room> Rooms => rooms;

    /// <summary>Every script of the folder; an empty library when a file of it can't be read.</summary>
    public ScriptLibrary Scripts { get; private set; } = ScriptLibrary.FromSources();

    /// <summary>What couldn't be read, or would stop the game as it loads.</summary>
    public IReadOnlyList<Finding> Unreadable => unreadable;

    private readonly List<Region> regions = new();
    private readonly List<Room> rooms = new();
    private readonly List<Finding> unreadable = new();
    private readonly Dictionary<string, Map> maps = new(StringComparer.OrdinalIgnoreCase);

    private GameContent(string root) => Root = root;

    private static readonly Lazy<GameContent> own = new(() => Load(GameDataFiles.Directory));

    /// <summary>The game's own files, read once: the <c>Data</c> folder beside the program.</summary>
    public static GameContent OfTheGame => own.Value;

    /// <summary>
    /// A map by its name, as warps and scripts name it: a room, or a map of a world. Null for a name that is no map
    /// (the game itself would answer with the map of Sinnoh, <see cref="MapDatabase.Get"/>, which is how a door to
    /// nowhere goes unseen).
    /// </summary>
    public Map? MapNamed(string name) => maps.GetValueOrDefault(name);

    public IEnumerable<Map> Maps => maps.Values;

    /// <summary>A file's name as findings give it: from the data folder, with forward slashes.</summary>
    public string Relative(string path) => Path.GetRelativePath(Root, path).Replace('\\', '/');

    public static GameContent Load(string root)
    {
        var content = new GameContent(Path.GetFullPath(root));
        content.ReadScripts();
        content.ReadRooms();
        content.ReadWorlds();
        return content;
    }

    private void ReadScripts()
    {
        string folder = Path.Combine(Root, ScriptLibrary.Folder);
        if (!Directory.Exists(folder)) return;
        try
        {
            Scripts = ScriptLibrary.Load(folder);
        }
        catch (InvalidDataException e)
        {
            // The parser's message begins with the file and the line: "common.txt(12): ..."
            string message = e.InnerException?.Message ?? e.Message;
            int colon = message.IndexOf("): ", StringComparison.Ordinal);
            int open = colon < 0 ? -1 : message.LastIndexOf('(', colon);
            unreadable.Add(open > 0
                ? new Finding(Kind.Unreadable, $"{ScriptLibrary.Folder}/{message[..open]}", "line " + message[(open + 1)..colon], "the scripts can't be read: " + message[(colon + 3)..])
                : new Finding(Kind.Unreadable, ScriptLibrary.Folder, "scripts", message));
        }
    }

    private void ReadRooms()
    {
        string folder = Path.Combine(Root, MapDatabase.Folder);
        if (!Directory.Exists(folder)) return;
        foreach (string path in Directory.GetFiles(folder, "*.json").OrderBy(p => p, StringComparer.Ordinal))
        {
            string file = Relative(path), name = Path.GetFileNameWithoutExtension(path), text = File.ReadAllText(path);
            MapFile? source = null;
            Map? map = null;
            try
            {
                source = GameDataFiles.Deserialize<MapFile>(text);
                if (source.Name != name)
                    unreadable.Add(new Finding(Kind.Unreadable, file, name, $"the file names its map '{source.Name}': the game stops on a map file that isn't called after its map"));
                map = source.ToMap();
            }
            catch (Exception e) when (e is JsonException or InvalidDataException or ArgumentException or IndexOutOfRangeException or ArgumentOutOfRangeException)
            {
                unreadable.Add(new Finding(Kind.Unreadable, file, name, "the game would not load it: " + e.Message));
            }
            rooms.Add(new Room(name, file, text, source, map));
            if (map != null) maps[name] = map;
        }
    }

    private void ReadWorlds()
    {
        string root = Path.Combine(Root, World.Folder);
        if (!Directory.Exists(root)) return;
        foreach (string dir in Directory.GetDirectories(root).OrderBy(d => d, StringComparer.Ordinal))
        {
            string indexPath = Path.Combine(dir, World.IndexFile);
            if (!File.Exists(indexPath)) continue;
            string folder = Relative(dir);
            World world;
            try
            {
                world = World.Open(dir, GameDataFiles.Deserialize<WorldIndexFile>(File.ReadAllText(indexPath)));
            }
            catch (JsonException e)
            {
                unreadable.Add(new Finding(Kind.Unreadable, Relative(indexPath), World.IndexFile, "it can't be read: " + e.Message));
                continue;
            }

            var overlays = new List<OverlaySource>();
            string overlayFolder = Path.Combine(dir, "overlays");
            bool whole = true;
            foreach (string path in Directory.Exists(overlayFolder) ? Directory.GetFiles(overlayFolder, "*.json").OrderBy(p => p, StringComparer.Ordinal) : Enumerable.Empty<string>())
            {
                string key = Path.GetFileNameWithoutExtension(path), text = File.ReadAllText(path);
                WorldOverlayFile? overlay = null;
                try
                {
                    overlay = GameDataFiles.Deserialize<WorldOverlayFile>(text);
                }
                catch (JsonException e)
                {
                    whole = false;
                    unreadable.Add(new Finding(Kind.Unreadable, Relative(path), key, "it can't be read: " + e.Message));
                }
                overlays.Add(new OverlaySource(key, Relative(path), text, overlay));
            }

            // The maps the game makes of the region, as it makes them. What stops it stops the game
            List<Map>? built = null;
            if (whole)
            {
                try
                {
                    built = world.BuildMaps();
                    foreach (var map in built) maps[map.Name] = map;
                }
                catch (Exception e)
                {
                    unreadable.Add(new Finding(Kind.Unreadable, folder, world.Index.Region, "the game would not load this world: " + e.Message));
                }
            }
            regions.Add(new Region(folder, world, overlays, built));
        }
    }
}
