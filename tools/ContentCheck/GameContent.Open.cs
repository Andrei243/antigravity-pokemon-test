using OpenWorldBake;
using PokemonPlatinumEngine.OpenWorld;

namespace ContentCheck;

/// <summary>
/// The open regions of a data folder (plan 32 · OW5): <c>world3d/&lt;region&gt;/</c>, each read as the baker reads its
/// descriptions (strictly, <see cref="RegionDescription"/>) and as the game lays its places on its maps
/// (<see cref="OpenPlaceFiles.LoadRegion"/>). What can't be read is a finding of its own, and what depends on it is
/// left out of the checks.
/// </summary>
public sealed partial class GameContent
{
    /// <summary>An open region as the checks read it: its folder, its descriptions and the land they make, its maps with their places.</summary>
    public sealed record OpenRegion(string Folder, RegionDescription? Description, TerrainWorld? Land, IReadOnlyList<OpenMap>? Maps);

    /// <summary>The folder open regions are described in, under the data folder.</summary>
    public const string OpenFolder = "world3d";

    private readonly List<OpenRegion> openRegions = new();
    private readonly Dictionary<string, OpenMap> openMaps = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<OpenRegion> OpenRegions => openRegions;

    /// <summary>An open map by its name, as a room's way out and a script's <c>warpto</c> name it; null for none.</summary>
    public OpenMap? OpenMapNamed(string name) => openMaps.GetValueOrDefault(name);

    private void ReadOpenWorlds()
    {
        string root = Path.Combine(Root, OpenFolder);
        if (!Directory.Exists(root)) return;
        foreach (string dir in Directory.GetDirectories(root).OrderBy(d => d, StringComparer.Ordinal))
        {
            if (!File.Exists(Path.Combine(dir, "world.json"))) continue;
            var region = ReadOpenRegion(dir, Relative(dir), unreadable);
            openRegions.Add(region);
            foreach (var map in region.Maps ?? Array.Empty<OpenMap>()) openMaps[map.Name] = map;
        }
    }

    /// <summary>
    /// One open region's folder read for the checks (a test reads the baker's open test world so): its descriptions and
    /// its places, each a finding of <see cref="Kind.Unreadable"/> where it can't be read. The land is the descriptions'
    /// own field; no bake is opened.
    /// </summary>
    public static OpenRegion ReadOpenRegion(string dir, string folder, List<Finding> unreadable)
    {
        RegionDescription? description = null;
        TerrainWorld? land = null;
        IReadOnlyList<OpenMap>? maps = null;
        try
        {
            description = RegionDescription.Read(dir);
            land = description.Land();
        }
        catch (InvalidDataException e)
        {
            unreadable.Add(new Finding(Kind.Unreadable, $"{folder}/{Before(e.Message)}", "description", "the baker stops on it: " + After(e.Message)));
        }
        try
        {
            maps = OpenPlaceFiles.LoadRegion(dir, withLand: false);
        }
        catch (Exception e) when (e is InvalidDataException or ArgumentException or System.Text.Json.JsonException)
        {
            unreadable.Add(new Finding(Kind.Unreadable, $"{folder}/{Before(e.Message)}", "places", "the game stops on it: " + After(e.Message)));
        }
        return new OpenRegion(folder, description, land, maps);

        // A finding's message begins with its file: "places/lab_cave.json: $.triggers[0]: ..."
        static string Before(string message) => message.IndexOf(": ", StringComparison.Ordinal) is int at and > 0 && !message[..at].Contains(' ') ? message[..at].Replace('\\', '/') : "world.json";
        static string After(string message) => message.IndexOf(": ", StringComparison.Ordinal) is int at and > 0 && !message[..at].Contains(' ') ? message[(at + 2)..] : message;
    }
}
