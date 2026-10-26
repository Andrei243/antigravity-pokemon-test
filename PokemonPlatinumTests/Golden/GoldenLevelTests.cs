using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests.Golden;

/// <summary>
/// The game's level curve (plan 03 · D13), held to <c>levels.txt</c>: every place with wild Pokémon or trainers
/// standing in it, by its area's key, with the levels of what is met in its grass or cave, on its water and on each
/// rod, and of the trainers there. They are Platinum's own, read from the imported world and the trainer table, so a
/// block that moves is an import that changed a level; and they are the yardstick for whatever this game places of
/// its own (plan 03 · D12a: the other versions' exclusives take the levels of the slots they stand in, in the places
/// Diamond and Pearl have them; Pal Park's visitors are set against the Battle Zone's rows).
/// </summary>
public class GoldenLevelTests
{
    private static string Range(IEnumerable<int> levels)
    {
        var all = levels.ToList();
        return all.Min() == all.Max() ? $"{all.Min()}" : $"{all.Min()}-{all.Max()}";
    }

    private static void Wild(StringBuilder text, string how, IReadOnlyCollection<WildEncounterEntry> table)
    {
        if (table.Count == 0) return;
        Assert.All(table, e => Assert.InRange(e.MinLevel, 1, e.MaxLevel));
        Assert.All(table, e => Assert.InRange(e.MaxLevel, e.MinLevel, 100));
        text.Append($"{how} {Range(table.SelectMany(e => new[] { e.MinLevel, e.MaxLevel }))}\n");
    }

    /// <summary>A place: an area of the imported world, or a room or cave made by hand, which is a map of its own.</summary>
    private sealed record Place(string Name, string? Region, List<(string How, IReadOnlyCollection<WildEncounterEntry> Table)> Tables, List<NPC> Trainers);

    /// <summary>Every place of every map, gathered once for both files (building every map is the costly part).</summary>
    private static readonly Lazy<SortedDictionary<string, Place>> Places = new(() =>
    {
        var maps = MapDatabase.Fresh();
        var places = new SortedDictionary<string, Place>(StringComparer.Ordinal);
        foreach (string mapName in MapDatabase.MapNames)
        {
            var map = maps(mapName);
            string? region = RegionDatabase.RegionOfMap(mapName)?.Id;
            if (map.Areas.Count == 0)
            {
                places[map.Name] = new Place(map.DisplayName, region, new() { ("grass", map.WildEncounters) }, new List<NPC>());
                foreach (var person in map.Everyone.Where(n => n.TrainerData != null)) places[map.Name].Trainers.Add(person);
                continue;
            }
            foreach (var area in map.Areas)
                places[area.Key] = new Place(area.DisplayName, region, new()
                {
                    ("grass", area.WildEncounters),
                    ("surf", area.WaterEncounters),
                    ("old rod", area.RodEncounters[0]),
                    ("good rod", area.RodEncounters[1]),
                    ("super rod", area.RodEncounters[2])
                }, new List<NPC>());
            foreach (var person in map.Everyone.Where(n => n.TrainerData != null))
                if (map.AreaAt(person.GridX, person.GridY) is { } stands) places[stands.Key].Trainers.Add(person);
        }
        return places;
    });

    /// <summary>A place's trainers with a team, one to an id, by id.</summary>
    private static List<NPC> TrainersOf(Place place) =>
        place.Trainers.Where(t => t.TrainerData!.Party.Count > 0)
            .GroupBy(t => t.TrainerData!.Id ?? t.Name, StringComparer.Ordinal).Select(g => g.First())
            .OrderBy(t => t.TrainerData!.Id ?? t.Name, StringComparer.Ordinal).ToList();

    [Fact]
    public void TheLevelsOfEveryPlaceAreWhatTheyWere()
    {
        var text = new StringBuilder($"# The levels of every place with wild Pokémon or trainers in it (plan 03 · D13), made by GoldenLevelTests. Rewritten by: {GoldenFile.AcceptCommand}\n");
        text.Append("# A place's wild Pokémon by how they are met, then its trainers with the levels of their teams, each trainer by id with the highest of its own.\n");
        int listed = 0;
        foreach (var (key, place) in Places.Value)
        {
            var block = new StringBuilder();
            foreach (var (how, table) in place.Tables) Wild(block, how, table);
            var trainers = TrainersOf(place);
            if (trainers.Count > 0)
            {
                var levels = trainers.SelectMany(t => t.TrainerData!.Party.Members.Select(p => p.Level)).ToList();
                Assert.All(levels, level => Assert.InRange(level, 1, 100));
                block.Append($"trainers {trainers.Count}, {Range(levels)}: {string.Join(", ", trainers.Select(t => $"{t.TrainerData!.Id ?? t.Name} {t.TrainerData!.Party.Members.Max(p => p.Level)}"))}\n");
            }
            if (block.Length == 0) continue;
            listed++;
            text.Append($"{GoldenFile.BlockMark}{key}\n{place.Name}\n").Append(block);
        }
        Assert.True(listed > 150, $"only {listed} places have wild Pokémon or trainers");

        GoldenFile.Check("levels.txt", text.ToString());
    }

    /// <summary>A range of the region's own levels and the range it is met at, the mapping rising.</summary>
    private static string Crossed(IEnumerable<int> own, LevelMapping levels)
    {
        var all = own.ToList();
        return $"{Range(all.Select(levels.Map))} (own {Range(all)})";
    }

    /// <summary>The Pokémon of a team whose stage is another as it is met (plan 18 · W4b-2, plan 03 · D15-A), in square brackets; nothing where none is.</summary>
    private static string Grown(Party own, LevelMapping levels)
    {
        var met = new Trainer { Party = own }.Relevelled(levels.Map, grow: true);
        var changed = own.Members.Zip(met.Party.Members).Where(p => p.First.Species.Name != p.Second.Species.Name)
            .Select(p => $"{p.First.Species.Name} → {p.Second.Species.Name}").ToList();
        return changed.Count == 0 ? "" : $" [{string.Join(", ", changed)}]";
    }

    /// <summary>
    /// Plan 18 · W4b-1: <c>levels-crossed.txt</c>, every region crossed into as a game begun in Sinnoh meets it: first a
    /// block for each region (its pairs, its caps own and met, and the trainers its file has that stand on no map:
    /// the Leaders, the League, the rivals, the rematches), then every place of the region with its wild Pokémon and
    /// its trainers at the levels met there. <c>levels.txt</c> keeps the games' own; this one moves when a region's
    /// pairs or caps do, and with plan 18 · W4b-2 when a trainer's Pokémon takes the stage its level allows.
    /// </summary>
    [Fact]
    public void TheLevelsOfEveryCrossedPlaceAreWhatTheyWere()
    {
        var text = new StringBuilder($"# Every region crossed into, as a game begun in Sinnoh meets it (plan 18 · W4b-1), made by GoldenLevelTests. Rewritten by: {GoldenFile.AcceptCommand}\n");
        text.Append("# A region's pairs and caps, then its trainers that stand on no map; then each of its places' wild Pokémon and trainers. Each level as met, its own game's in brackets; in square brackets the species a trainer's Pokémon is met as where its stage is another (plan 18 · W4b-2).\n");
        var crossed = RegionDatabase.All.Where(r => r.Levels != null && !string.Equals(r.Id, RegionDatabase.First.Id, StringComparison.Ordinal)).ToList();
        Assert.True(crossed.Count >= 4, "Kanto, Johto, Hoenn and Unova are crossed into");

        var placed = new HashSet<string>(Places.Value.Values.SelectMany(p => p.Trainers).Select(t => t.TrainerData!.Id ?? ""), StringComparer.OrdinalIgnoreCase);
        foreach (var region in crossed)
        {
            var levels = region.Levels!;
            text.Append($"{GoldenFile.BlockMark}region {region.Id}\n");
            text.Append($"levels {string.Join(", ", levels.Points.Select(p => $"{p.From} → {p.To}"))}\n");
            if (LevelCaps.TableOf(region.Id) is { } own)
                text.Append($"caps {string.Join(" ", LevelCaps.TableOf(region.Id, crossed: true)!)} (own {string.Join(" ", own)})\n");
            foreach (var record in TrainerDatabase.OfRegion(region.Folder).Where(r => r.Party.Count > 0 && !placed.Contains(r.Id)).OrderBy(r => r.Id, StringComparer.Ordinal))
            {
                int top = record.Party.Max(m => m.Level);
                Assert.InRange(levels.Map(top), 1, Ruleset.HighestLevel);
                text.Append($"{record.Id} {levels.Map(top)} ({top}){Grown(TrainerDatabase.PartyOf(record), levels)}\n");
            }
        }

        int listed = 0;
        foreach (var (key, place) in Places.Value)
        {
            if (crossed.FirstOrDefault(r => string.Equals(r.Id, place.Region, StringComparison.Ordinal)) is not { } region) continue;
            var levels = region.Levels!;
            var block = new StringBuilder();
            foreach (var (how, table) in place.Tables)
                if (table.Count > 0) block.Append($"{how} {Crossed(table.SelectMany(e => new[] { e.MinLevel, e.MaxLevel }), levels)}\n");
            var trainers = TrainersOf(place);
            if (trainers.Count > 0)
            {
                var all = trainers.SelectMany(t => t.TrainerData!.Party.Members.Select(p => p.Level)).ToList();
                block.Append($"trainers {trainers.Count}, {Crossed(all, levels)}: {string.Join(", ", trainers.Select(t =>
                {
                    int top = t.TrainerData!.Party.Members.Max(p => p.Level);
                    return $"{t.TrainerData!.Id ?? t.Name} {levels.Map(top)} ({top}){Grown(t.TrainerData!.Party, levels)}";
                }))}\n");
            }
            if (block.Length == 0) continue;
            listed++;
            text.Append($"{GoldenFile.BlockMark}{key}\n{place.Name}, {region.Id}\n").Append(block);
        }
        Assert.True(listed > 100, $"only {listed} crossed places have wild Pokémon or trainers");

        GoldenFile.Check("levels-crossed.txt", text.ToString());
    }
}
