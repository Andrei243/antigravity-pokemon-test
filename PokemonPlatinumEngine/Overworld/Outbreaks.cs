using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// One day's outbreak in one place (plan 38 · EX1): a species that fills the first <see cref="Slots"/> slots of the
/// place's grass, which together are <see cref="Outbreaks.ShareOf"/> meetings in a hundred. A row of the region's own
/// game also has a level and moves (<see cref="Level"/> nought leaves the slots' own levels).
/// </summary>
public sealed record Outbreak(string Area, string Species, int Slots, int Level = 0, IReadOnlyList<string>? Moves = null)
{
    /// <summary>How many meetings in a hundred are the outbreak's.</summary>
    public int Share => Outbreaks.ShareOf(Slots);
}

/// <summary>
/// Outbreaks of one species (plan 38 · EX1; <c>docs/mechanics/rulings.md</c>, "The field beyond the tables"): once a
/// region's Hall of Fame is entered it has <see cref="PerDay"/> a day, each at a place with a land table of twelve slots
/// and one species of that place's own table that isn't its commonest, a legendary, a mythical or a slot of ours, and
/// never at the place of the day's swarm, which stays as it is. Eight meetings in ten in the place are the outbreak's
/// (the grass's slots 0 to 5), and a region whose own game announces one (Emerald's television) has one of its rows each
/// day too, at the share its game gave. What a day holds is worked out from the day's number and the region by
/// <see cref="SplitMix"/> alone, never drawn from <see cref="Dice"/>, so a game that has none in force draws what it always
/// drew. The species beaten or caught in the place are counted (<see cref="SpecialEncounters.OutbreakMet"/>), and from
/// <see cref="FirstBonusAt"/> met a meeting of that species there is given one more shiny roll, from
/// <see cref="SecondBonusAt"/> two (Scarlet's and Violet's steps). GPU-free; no input or drawing.
/// </summary>
public static class Outbreaks
{
    /// <summary>How many outbreaks a region has a day.</summary>
    public const int PerDay = 2;

    /// <summary>How many meetings in a hundred a generated outbreak takes: the grass's slots 0 to 5 (20, 20, 10, 10, 10, 10).</summary>
    public const int GrassShare = 80;

    /// <summary>The most wild Pokémon seen in the grass of an outbreak's place at once (<see cref="FieldPokemon.MostInView"/> elsewhere).</summary>
    public const int MostInOutbreak = 10;

    /// <summary>Met in the place so far: one more shiny roll from this many, and two from <see cref="SecondBonusAt"/>.</summary>
    public const int FirstBonusAt = 30, SecondBonusAt = 60;

    // The kinds of area an outbreak may be in: tall grass under the open sky
    private static readonly string[] Kinds = { "Outdoors", "Route", "Town" };

    // A place the story, a mechanic or a contest holds, by its key: none has an outbreak
    private static readonly string[] NeverHere =
    {
        "safari", "great_marsh", "marsh", "national_park", "contest", "trophy_garden", "gym", "frontier", "battle_",
        "spear_pillar", "stark_mountain", "distortion", "turnback", "mirage", "hall_of_fame", "victory_road", "sendoff"
    };

    /// <summary>Whether a region has outbreaks now: its Hall of Fame is entered.</summary>
    public static bool Open(Region? region, Story.StoryState story) => region != null && story.Has(region.StoryCompleteFlag);

    /// <summary>The slots of the grass an outbreak of this many meetings in a hundred takes: the first whose weights come to it.</summary>
    public static int SlotsFor(int share)
    {
        int sum = 0;
        for (int i = 0; i < WorldAreaFile.LandSlotWeights.Length; i++)
        {
            sum += WorldAreaFile.LandSlotWeights[i];
            if (sum >= share) return i + 1;
        }
        return WorldAreaFile.LandSlotWeights.Length;
    }

    /// <summary>How many meetings in a hundred the first <paramref name="slots"/> slots are.</summary>
    public static int ShareOf(int slots) => WorldAreaFile.LandSlotWeights.Take(slots).Sum();

    /// <summary>
    /// The shiny rolls a meeting of the outbreak's species in its place is given beyond its own, by how many of them have
    /// been beaten or caught there today: none before <see cref="FirstBonusAt"/>, one from it, two from <see cref="SecondBonusAt"/>.
    /// </summary>
    public static int ExtraRolls(int met) => met >= SecondBonusAt ? 2 : met >= FirstBonusAt ? 1 : 0;

    /// <summary>A place an outbreak may be in: its key and the species of its own table that one may be of.</summary>
    public sealed record Place(string Area, IReadOnlyList<string> Species);

    private sealed class Held
    {
        public List<Place> Places { get; } = new();
        public List<OutbreakRow> Rows { get; } = new();
    }

    private static readonly Dictionary<string, Held> held = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object gate = new();

    private static Held Of(string region)
    {
        lock (gate)
        {
            if (held.TryGetValue(region, out var own)) return own;
            own = new Held();
            foreach (var world in World.LoadAll().Where(w => string.Equals(w.Index.Region, region, StringComparison.OrdinalIgnoreCase)))
            {
                foreach (string key in world.Index.Areas.OrderBy(k => k, StringComparer.Ordinal))
                    if (PlaceOf(world, key) is { } place) own.Places.Add(place);
                own.Rows.AddRange(world.Encounters.Outbreaks ?? new());
                own.Rows.AddRange(world.OwnOutbreaks.Rows);
            }
            return held[region] = own;
        }
    }

    /// <summary>Forgets what was read, for a tool or a test that has laid other data layers.</summary>
    public static void Forget()
    {
        lock (gate) { held.Clear(); today = null; }
    }

    /// <summary>The places of a region that can have an outbreak, in the order of their keys.</summary>
    public static IReadOnlyList<Place> PlacesOf(string region) => Of(region).Places;

    /// <summary>The outbreaks a region's own game and our rows add to the day's, row by row.</summary>
    public static IReadOnlyList<OutbreakRow> RowsOf(string region) => Of(region).Rows;

    private static Place? PlaceOf(World world, string key)
    {
        if (NeverHere.Any(n => key.Contains(n, StringComparison.Ordinal))) return null;
        if (world.Area(key) is not { Land: { Count: 12 } land } file || !Kinds.Contains(file.Kind)) return null;
        // The species of the table: its slots', the hour's and (Sinnoh's) the two the day and night put in slots 2 and 3
        var all = new List<string>();
        var weight = new Dictionary<string, int>(StringComparer.Ordinal);
        var order = new List<string>();
        for (int i = 0; i < land.Count; i++)
        {
            string species = land[i].Species;
            if (!weight.ContainsKey(species)) order.Add(species);
            weight[species] = weight.GetValueOrDefault(species) + (land[i].Weight ?? WorldAreaFile.LandSlotWeights[i]);
            all.Add(species);
            if (land[i].Day != null) all.Add(land[i].Day!);
            if (land[i].Night != null) all.Add(land[i].Night!);
        }
        all.AddRange(file.Day ?? new());
        all.AddRange(file.Night ?? new());
        // A place whose table holds a legendary or a mythical is one the story holds
        var known = all.Distinct(StringComparer.Ordinal).Select(PokemonDatabase.Get).Where(s => s != null).Select(s => s!).ToList();
        if (known.Any(s => s.Legendary || s.Mythical)) return null;
        // The commonest is the species the most slots' weights come to; the first of the table on a tie
        string commonest = order.OrderByDescending(s => weight[s]).First();
        var candidates = known.Select(s => s.Name).Where(n => n != commonest).Distinct(StringComparer.Ordinal).OrderBy(n => n, StringComparer.Ordinal).ToList();
        return new Place(key, candidates);
    }

    // The last day asked for, so that the grass's every step doesn't work it out again
    private static (string Region, uint Daily, string? Swarm, IReadOnlyList<Outbreak> Outbreaks)? today;

    /// <summary>
    /// The day's outbreaks in a region (none for a region with no places): one of the region's own rows where it has any
    /// and the rest at places drawn by the day's number, never at the swarm's place. The same day always gives the same.
    /// </summary>
    /// <param name="region">The region's id.</param>
    /// <param name="daily">The day's number (<see cref="SpecialEncounters.DailyNumber"/>).</param>
    /// <param name="swarmArea">The place of the day's swarm, which keeps it to itself; null for none.</param>
    public static IReadOnlyList<Outbreak> Today(string region, uint daily, string? swarmArea = null)
    {
        lock (gate)
        {
            if (today is { } last && last.Region == region && last.Daily == daily && last.Swarm == swarmArea) return last.Outbreaks;
        }
        var made = Work(region, daily, swarmArea);
        lock (gate) today = (region, daily, swarmArea, made);
        return made;
    }

    private static IReadOnlyList<Outbreak> Work(string region, uint daily, string? swarmArea)
    {
        var own = Of(region);
        var result = new List<Outbreak>();
        uint seed = SplitMix.Fnv(region);
        // A row of the region's own game or ours, where its place is one that can have it
        var rows = own.Rows.Where(r => own.Places.Any(p => p.Area == r.Area) && PokemonDatabase.Get(r.Species) != null).ToList();
        if (rows.Count > 0)
        {
            var row = rows[(int)(SplitMix.Mix(daily, seed, 0x0B1) % (uint)rows.Count)];
            if (row.Area != swarmArea)
                result.Add(new Outbreak(row.Area, row.Species, SlotsFor(row.Share), row.Level, row.Moves));
        }
        // The rest at places of the day's drawing, each of a species of its own table
        var open = own.Places.Where(p => p.Species.Count > 0 && p.Area != swarmArea && result.All(o => o.Area != p.Area)).ToList();
        for (uint n = 0; result.Count < PerDay && open.Count > 0; n++)
        {
            var place = open[(int)(SplitMix.Mix(daily, seed, 0x0B2 + n) % (uint)open.Count)];
            open.Remove(place);
            string species = place.Species[(int)(SplitMix.Mix(daily, SplitMix.Fnv(place.Area), 0x0B3) % (uint)place.Species.Count)];
            result.Add(new Outbreak(place.Area, species, SlotsFor(GrassShare)));
        }
        return result;
    }

    /// <summary>The day's outbreak at a place, or null.</summary>
    public static Outbreak? At(IReadOnlyList<Outbreak>? day, string? area) =>
        area == null ? null : day?.FirstOrDefault(o => o.Area == area);

    /// <summary>
    /// Lays an outbreak over a place's twelve slots: its species in the first slots, each keeping its weight (and its levels,
    /// unless the outbreak has a level of its own). The slots are copies, as the moment's species are laid in.
    /// </summary>
    public static void Lay(List<WildEncounterEntry> slots, Outbreak outbreak)
    {
        for (int i = 0; i < outbreak.Slots && i < slots.Count; i++)
        {
            var slot = slots[i];
            slot.SpeciesName = outbreak.Species;
            slot.Outbreak = true;
            slot.Moves = outbreak.Moves;
            if (outbreak.Level > 0) slot.MinLevel = slot.MaxLevel = outbreak.Level;
        }
    }

    /// <summary>
    /// Gives a Pokémon the moves an outbreak of the game's own names, up to four of those the data has; draws nothing, and
    /// leaves the Pokémon as it is when none is known.
    /// </summary>
    public static void GiveMoves(Pokemon pokemon, IReadOnlyList<string> moves)
    {
        var known = moves.Where(MoveDatabase.Has).Distinct(StringComparer.Ordinal).Take(4).ToList();
        if (known.Count == 0) return;
        pokemon.Moves.Clear();
        foreach (string name in known) pokemon.Moves.Add(new Move(MoveDatabase.Get(name)));
    }
}
