using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>One horde a place's tall grass may hold (plan 14 · B4): its species, the levels each of the five is drawn from, its weight.</summary>
public sealed class HordeRow
{
    public string Species { get; set; } = "";
    public int MinLevel { get; set; }
    public int MaxLevel { get; set; }

    /// <summary>How likely this horde is beside the place's others; one each unless said.</summary>
    public int Weight { get; set; } = 1;
}

/// <summary>A place whose tall grass holds hordes (an area's key, as its world file names it) and the hordes it holds.</summary>
public sealed class HordePlace
{
    public string Area { get; set; } = "";
    public List<HordeRow> Hordes { get; set; } = new();
}

/// <summary>The hordes of a region's places as a moment carries them: a table of rows of five for each place.</summary>
public sealed class HordeTables
{
    private readonly Dictionary<string, IReadOnlyList<WildEncounterEntry>> byArea;

    public HordeTables(IEnumerable<HordePlace> places)
    {
        byArea = places.ToDictionary(p => p.Area, p => (IReadOnlyList<WildEncounterEntry>)Hordes.TableOf(p), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>The hordes of an area's tall grass; null for a place with none.</summary>
    public IReadOnlyList<WildEncounterEntry>? At(string? area) => area != null && byArea.TryGetValue(area, out var rows) ? rows : null;

    /// <summary>Every place that holds hordes.</summary>
    public IEnumerable<string> Places => byArea.Keys;
}

/// <summary>
/// Hordes in the field (plan 14 · B4; <c>docs/mechanics/rulings.md</c>, "Hordes"): X and Y's, met in the tall grass of
/// the places <c>Data/hordes.json</c> names for each region, and only once that region's Hall of Fame is entered
/// (plan 14, decision 1: the later formats are met after the Hall of Fame) under rules that have hordes
/// (<see cref="Ruleset.Hordes"/>). A step in such grass that meets a Pokémon meets a horde one time in twenty, as X and
/// Y have it, and Sweet Scent there always draws one out. Each species is one of the region's own Pokédex, of a family
/// the place's own grass has. No table of the places changes (a slot of one stays a slot of one): the hordes are a
/// table of their own beside it, which nothing reads in Platinum's story. GPU-free.
/// </summary>
public static class Hordes
{
    /// <summary>A step that meets a Pokémon in a horde's grass meets a horde this many times in a hundred: X and Y's one in twenty.</summary>
    public const int ChanceInHundred = 5;

    /// <summary>How many come in a horde.</summary>
    public const int Size = 5;

    /// <summary>The file, by region id.</summary>
    public const string FileName = "hordes.json";

    private static Dictionary<string, List<HordePlace>>? file;

    /// <summary>Every region's places, as the file lists them (an empty list for a region it doesn't name).</summary>
    public static IReadOnlyDictionary<string, List<HordePlace>> All => file ??= Load();

    private static Dictionary<string, List<HordePlace>> Load()
    {
        if (!File.Exists(GameDataFiles.PathOf(FileName))) return new(StringComparer.OrdinalIgnoreCase);
        var read = GameDataFiles.Load<Dictionary<string, List<HordePlace>>>(FileName);
        return new Dictionary<string, List<HordePlace>>(read, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>A region's places with hordes.</summary>
    public static IReadOnlyList<HordePlace> PlacesOf(string region) =>
        All.TryGetValue(region, out var places) ? places : Array.Empty<HordePlace>();

    /// <summary>
    /// The hordes the region's grass holds now: its places' once its Hall of Fame has been entered, under rules that
    /// have hordes; null otherwise, or for a region with none.
    /// </summary>
    public static HordeTables? Open(Region? region, Story.StoryState story, Ruleset rules)
    {
        if (region == null || !rules.Hordes || !story.Has(region.StoryCompleteFlag)) return null;
        var places = PlacesOf(region.Id);
        return places.Count == 0 ? null : new HordeTables(places);
    }

    /// <summary>A place's hordes as a table: a row of <see cref="Size"/> for each, its levels a range each of the five is drawn from.</summary>
    public static List<WildEncounterEntry> TableOf(HordePlace place) => place.Hordes
        .Select(h => new WildEncounterEntry { SpeciesName = h.Species, MinLevel = h.MinLevel, MaxLevel = h.MaxLevel, Weight = Math.Max(1, h.Weight), Count = Size })
        .ToList();

    /// <summary>
    /// Whether a step that has met something in grass where hordes live meets a horde instead (one draw, made only
    /// where a horde could come: nothing else's chance moves for it).
    /// </summary>
    public static bool Comes(Random rng) => rng.Next(100) < ChanceInHundred;
}
