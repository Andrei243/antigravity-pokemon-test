using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokemonPlatinumEngine.Data;
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

    /// <summary>
    /// An area's species of our own past level 100 (plan 03 · D15): their own levels, never mapped, which alone may pass
    /// 100 (to 260), each from its evolution's level; a line only where the area has any.
    /// </summary>
    private static void Ours(StringBuilder text, MapArea area)
    {
        var slots = area.OursLand.Concat(area.OursSurf).Concat(area.OursSuperRod).ToList();
        if (slots.Count == 0) return;
        Assert.All(slots, s => Assert.InRange(s.MinLevel, OursSlots.EvolutionLevel(PokemonDatabase.Get(s.Species)!), s.MaxLevel));
        Assert.All(slots, s => Assert.InRange(s.MaxLevel, s.MinLevel, Ruleset.HighestLevel));
        text.Append($"ours {string.Join(", ", slots.Select(s => $"{s.Species} {(s.MinLevel == s.MaxLevel ? $"{s.MinLevel}" : $"{s.MinLevel}-{s.MaxLevel}")}"))}\n");
    }

    [Fact]
    public void TheLevelsOfEveryPlaceAreWhatTheyWere()
    {
        var maps = MapDatabase.Fresh();
        // A place: an area of the imported world, or a room or cave made by hand, which is a map of its own
        var places = new SortedDictionary<string, (string Name, Action<StringBuilder> Wild, List<NPC> Trainers)>(StringComparer.Ordinal);
        foreach (string mapName in MapDatabase.MapNames)
        {
            var map = maps(mapName);
            if (map.Areas.Count == 0)
            {
                places[map.Name] = (map.DisplayName, text => Wild(text, "grass", map.WildEncounters), new List<NPC>());
                foreach (var person in map.Everyone.Where(n => n.TrainerData != null)) places[map.Name].Trainers.Add(person);
                continue;
            }
            foreach (var area in map.Areas)
                places[area.Key] = (area.DisplayName, text =>
                {
                    Wild(text, "grass", area.WildEncounters);
                    Wild(text, "surf", area.WaterEncounters);
                    Wild(text, "old rod", area.RodEncounters[0]);
                    Wild(text, "good rod", area.RodEncounters[1]);
                    Wild(text, "super rod", area.RodEncounters[2]);
                    Ours(text, area);
                }, new List<NPC>());
            foreach (var person in map.Everyone.Where(n => n.TrainerData != null))
                if (map.AreaAt(person.GridX, person.GridY) is { } stands) places[stands.Key].Trainers.Add(person);
        }

        var text = new StringBuilder($"# The levels of every place with wild Pokémon or trainers in it (plan 03 · D13), made by GoldenLevelTests. Rewritten by: {GoldenFile.AcceptCommand}\n");
        text.Append("# A place's wild Pokémon by how they are met, then its trainers with the levels of their teams, each trainer by id with the highest of its own.\n");
        int listed = 0;
        foreach (var (key, place) in places)
        {
            var block = new StringBuilder();
            place.Wild(block);
            var trainers = place.Trainers.Where(t => t.TrainerData!.Party.Count > 0)
                .GroupBy(t => t.TrainerData!.Id ?? t.Name, StringComparer.Ordinal).Select(g => g.First())
                .OrderBy(t => t.TrainerData!.Id ?? t.Name, StringComparer.Ordinal).ToList();
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
}
