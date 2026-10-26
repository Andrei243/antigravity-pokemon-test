using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.Data;

/// <summary>The ways a wild Pokémon is met in a place, as the Pokédex's area page tells them apart.</summary>
[Flags]
public enum HabitatWays
{
    None = 0,
    Morning = 1,
    Day = 2,
    Night = 4,
    Surfing = 8,
    OldRod = 16,
    GoodRod = 32,
    SuperRod = 64,
    /// <summary>Met in a way of its own, which the habitat's <see cref="Habitat.How"/> tells (plan 08 · P12).</summary>
    Special = 128,
    /// <summary>One of our own species past level 100, in a rare slot of its own after the Hall of Fame (plan 03 · D15).</summary>
    Ours = 256,
    Grass = Morning | Day | Night,
    Fishing = OldRod | GoodRod | SuperRod
}

/// <summary>A place a species lives: the areas of one name (a cave's floors are one place), where it is on the map and how it is met there.</summary>
public sealed record Habitat(string Name, IReadOnlyList<(int X, int Y)> Cells, HabitatWays Ways)
{
    /// <summary>How it is met where it is <see cref="HabitatWays.Special"/>: "The Hallowed Tower".</summary>
    public string? How { get; init; }
}

/// <summary>
/// Where each species lives in a region, for the Pokédex's area page (plan 03 · D10): read from the region's
/// habitats file (<see cref="WorldHabitatsFile"/>), which <c>tools/MapImporter</c> writes for every area with wild
/// Pokémon, open or not, with a coarse picture of the overworld to show them on; and from the hand-written special
/// file (<see cref="SpecialFileName"/>, plan 08 · P12) the Pokémon no table lists, met in a way of their own.
/// </summary>
public sealed class Habitats
{
    private readonly Dictionary<string, List<Habitat>> bySpecies = new(StringComparer.OrdinalIgnoreCase);
    private readonly string[] map;

    public int Width { get; }
    public int Height { get; }

    /// <summary>The hand-written file beside the habitats file, which the importer never writes.</summary>
    public const string SpecialFileName = "special.json";

    /// <param name="ours">The overlays' slots of our own species past level 100, by area key (plan 03 · D15): shown in the areas of those keys.</param>
    public Habitats(WorldHabitatsFile file, WorldHabitatsFile? special = null, IEnumerable<(string Key, OverlayOurs Ours)>? ours = null)
    {
        map = file.Map.ToArray();
        Height = map.Length;
        Width = map.Length == 0 ? 0 : map.Max(r => r.Length);

        // Every way each species is met in each area, then the areas of one name taken together
        var ways = new Dictionary<(string Species, string Name), (HabitatWays Ways, SortedSet<(int, int)> Cells, int First)>();
        int order = 0;
        foreach (var area in file.Areas)
        {
            var cells = ParseCells(area.Cells);
            void Add(List<string>? species, HabitatWays way)
            {
                foreach (string s in species ?? new List<string>())
                {
                    var key = (s, area.Name);
                    if (!ways.TryGetValue(key, out var entry)) entry = (HabitatWays.None, new SortedSet<(int, int)>(), order++);
                    foreach (var c in cells) entry.Cells.Add(c);
                    ways[key] = (entry.Ways | way, entry.Cells, entry.First);
                }
            }
            Add(area.Morning, HabitatWays.Morning);
            Add(area.Day, HabitatWays.Day);
            Add(area.Night, HabitatWays.Night);
            Add(area.Surf, HabitatWays.Surfing);
            Add(area.OldRod, HabitatWays.OldRod);
            Add(area.GoodRod, HabitatWays.GoodRod);
            Add(area.SuperRod, HabitatWays.SuperRod);
        }
        // Our own species past level 100, a way of their own in the areas their overlays name (plan 03 · D15)
        var byKey = file.Areas.GroupBy(a => a.Key, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        foreach (var (key, slots) in ours ?? Enumerable.Empty<(string, OverlayOurs)>())
        {
            if (!byKey.TryGetValue(key, out var area)) continue;
            var cells = ParseCells(area.Cells);
            void AddOurs(List<OursSlot>? list, HabitatWays way)
            {
                foreach (var slot in list ?? new List<OursSlot>())
                {
                    var at = (slot.Species, area.Name);
                    if (!ways.TryGetValue(at, out var entry)) entry = (HabitatWays.None, new SortedSet<(int, int)>(), order++);
                    foreach (var c in cells) entry.Cells.Add(c);
                    ways[at] = (entry.Ways | way | HabitatWays.Ours, entry.Cells, entry.First);
                }
            }
            AddOurs(slots.Land, HabitatWays.Grass);
            AddOurs(slots.Surf, HabitatWays.Surfing);
            AddOurs(slots.SuperRod, HabitatWays.SuperRod);
        }
        foreach (var ((species, name), (w, cells, _)) in ways.OrderBy(p => p.Value.First))
        {
            if (!bySpecies.TryGetValue(species, out var list)) bySpecies[species] = list = new List<Habitat>();
            list.Add(new Habitat(name, cells.ToList(), w));
        }

        // The Pokémon met in a way of their own, after the tables' places
        foreach (var area in special?.Areas ?? new List<HabitatArea>())
            foreach (string s in area.Special ?? new List<string>())
            {
                if (!bySpecies.TryGetValue(s, out var list)) bySpecies[s] = list = new List<Habitat>();
                list.Add(new Habitat(area.Name, ParseCells(area.Cells), HabitatWays.Special) { How = area.How });
            }
    }

    /// <summary>The places a species lives, in the order the region's areas come (empty if none).</summary>
    public IReadOnlyList<Habitat> Of(string species) => bySpecies.TryGetValue(species, out var list) ? list : Array.Empty<Habitat>();

    /// <summary>
    /// The places a species lives in a randomised run (plan 13 · V6), where the species of the tables stand for others: the
    /// places of every species that goes to it (<c>Randomizer.Sources</c>), the areas of one name taken together, in the
    /// order the region's areas come.
    /// </summary>
    public IReadOnlyList<Habitat> Of(IEnumerable<string> standingFor)
    {
        var merged = new Dictionary<string, (HabitatWays Ways, SortedSet<(int, int)> Cells, string? How, int First)>(StringComparer.Ordinal);
        int order = 0;
        foreach (string source in standingFor)
            foreach (var h in Of(source))
            {
                if (!merged.TryGetValue(h.Name, out var m)) m = (HabitatWays.None, new SortedSet<(int, int)>(), h.How, order++);
                foreach (var c in h.Cells) m.Cells.Add(c);
                merged[h.Name] = (m.Ways | h.Ways, m.Cells, m.How, m.First);
            }
        return merged.OrderBy(p => p.Value.First).Select(p => new Habitat(p.Key, p.Value.Cells.ToList(), p.Value.Ways) { How = p.Value.How }).ToList();
    }

    /// <summary>
    /// What a chunk of the overworld looks like on the map: <c>~</c> water, <c>.</c> land, <c>T</c> a town or a city,
    /// a space where there is nothing.
    /// </summary>
    public char LookAt(int x, int y) => y >= 0 && y < Height && x >= 0 && x < map[y].Length ? map[y][x] : ' ';

    /// <summary>The way of meeting Pokémon in the grass at a time of day: Platinum's evening counts as day, its late night as night.</summary>
    public static HabitatWays GrassAt(TimeOfDay time) => time switch
    {
        TimeOfDay.Morning => HabitatWays.Morning,
        TimeOfDay.Day or TimeOfDay.Twilight => HabitatWays.Day,
        _ => HabitatWays.Night
    };

    private static List<(int, int)> ParseCells(string text)
    {
        var cells = new List<(int, int)>();
        foreach (string pair in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split(',');
            if (parts.Length != 2) throw new InvalidDataException($"Habitat cell '{pair}' is not 'x,y'.");
            cells.Add((int.Parse(parts[0], CultureInfo.InvariantCulture), int.Parse(parts[1], CultureInfo.InvariantCulture)));
        }
        return cells;
    }

    private static readonly Dictionary<string, Habitats?> loaded = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>A region's habitats (by its folder under <c>Data/world</c>), read once; null if the file isn't there.</summary>
    public static Habitats? For(string region)
    {
        lock (loaded)
        {
            if (loaded.TryGetValue(region, out var habitats)) return habitats;
            string folder = region.ToLowerInvariant();
            string path = Path.Combine(World.Folder, folder, WorldHabitatsFile.FileName);
            string special = Path.Combine(World.Folder, folder, SpecialFileName);
            if (File.Exists(GameDataFiles.PathOf(path)))
                habitats = new Habitats(GameDataFiles.Load<WorldHabitatsFile>(path),
                    File.Exists(GameDataFiles.PathOf(special)) ? GameDataFiles.Load<WorldHabitatsFile>(special) : null,
                    OursOf(Path.Combine(World.Folder, folder)));
            return loaded[region] = habitats;
        }
    }

    /// <summary>The slots of ours a region's overlays give (plan 03 · D15), by area key, in the order of the keys.</summary>
    private static List<(string Key, OverlayOurs Ours)> OursOf(string regionFolder)
    {
        var found = new List<(string, OverlayOurs)>();
        string overlays = GameDataFiles.PathOf(Path.Combine(regionFolder, "overlays"));
        if (!Directory.Exists(overlays)) return found;
        foreach (string file in Directory.GetFiles(overlays, "*.json").OrderBy(f => f, StringComparer.Ordinal))
        {
            // Most overlays hold none: only those that name the key are read whole
            if (!File.ReadAllText(file).Contains("\"ours\"", StringComparison.Ordinal)) continue;
            var overlay = GameDataFiles.Load<WorldOverlayFile>(Path.Combine(regionFolder, "overlays", Path.GetFileName(file)));
            if (overlay.Ours != null) found.Add((Path.GetFileNameWithoutExtension(file), overlay.Ours));
        }
        return found;
    }

    /// <summary>A region's habitats, read from its folder; null for a region with none (plan 18 · W3).</summary>
    public static Habitats? Of(Region region) => For(region.Folder);

    /// <summary>Sinnoh's habitats.</summary>
    public static Habitats? Sinnoh => For("sinnoh");
}
