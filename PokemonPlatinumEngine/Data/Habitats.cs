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
    /// <summary>Met in a way of its own that no table lists (plan 08 · P12): <see cref="Habitat.How"/> says how.</summary>
    Special = 128,
    Grass = Morning | Day | Night,
    Fishing = OldRod | GoodRod | SuperRod
}

/// <summary>
/// A place a species lives: the areas of one name (a cave's floors are one place), where it is on the map and how it
/// is met there; <paramref name="How"/> in words for a place no table lists (<see cref="HabitatWays.Special"/>).
/// </summary>
public sealed record Habitat(string Name, IReadOnlyList<(int X, int Y)> Cells, HabitatWays Ways, string? How = null);

/// <summary>
/// Where each species lives in a region, for the Pokédex's area page (plan 03 · D10): read from the region's
/// habitats file (<see cref="WorldHabitatsFile"/>), which <c>tools/MapImporter</c> writes for every area with wild
/// Pokémon, open or not, with a coarse picture of the overworld to show them on, and from the hand-written file of
/// the places no table lists (<see cref="WorldSpecialFile"/>, plan 08 · P12).
/// </summary>
public sealed class Habitats
{
    private readonly Dictionary<string, List<Habitat>> bySpecies = new(StringComparer.OrdinalIgnoreCase);
    private readonly string[] map;

    public int Width { get; }
    public int Height { get; }

    public Habitats(WorldHabitatsFile file, WorldSpecialFile? special = null)
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
        foreach (var ((species, name), (w, cells, _)) in ways.OrderBy(p => p.Value.First))
        {
            if (!bySpecies.TryGetValue(species, out var list)) bySpecies[species] = list = new List<Habitat>();
            list.Add(new Habitat(name, cells.ToList(), w));
        }

        // The places of their own that no table lists (plan 08 · P12), after the tables' own
        foreach (var place in special?.Places ?? new List<SpecialPlace>())
        {
            if (!bySpecies.TryGetValue(place.Species, out var list)) bySpecies[place.Species] = list = new List<Habitat>();
            list.Add(new Habitat(place.Name, ParseCells(place.Cells), HabitatWays.Special, place.How));
        }
    }

    /// <summary>The places a species lives, in the order the region's areas come (empty if none).</summary>
    public IReadOnlyList<Habitat> Of(string species) => bySpecies.TryGetValue(species, out var list) ? list : Array.Empty<Habitat>();

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

    private static Habitats? sinnoh;
    private static bool loaded;

    /// <summary>Sinnoh's habitats, read once; null if the file isn't there.</summary>
    public static Habitats? Sinnoh
    {
        get
        {
            if (loaded) return sinnoh;
            loaded = true;
            string path = Path.Combine(World.Folder, "sinnoh", WorldHabitatsFile.FileName);
            string special = Path.Combine(World.Folder, "sinnoh", WorldSpecialFile.FileName);
            if (File.Exists(GameDataFiles.PathOf(path)))
                sinnoh = new Habitats(GameDataFiles.Load<WorldHabitatsFile>(path),
                    File.Exists(GameDataFiles.PathOf(special)) ? GameDataFiles.Load<WorldSpecialFile>(special) : null);
            return sinnoh;
        }
    }
}
