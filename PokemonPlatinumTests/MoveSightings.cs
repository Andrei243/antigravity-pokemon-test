using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// Where in the story each of Platinum's moves is first seen (plan 25 · Y6): the order the move batches take, worked
/// out once from the data. A place is an area of the imported world, in plan 01's order of opening (<c>world.json</c>'s
/// <c>areas</c>), or a room, which takes the place of the first area (or room) whose warp leads into it. A move is seen
/// in a place when a trainer standing there (as <see cref="TrainerDatabase.Fill"/> builds the team) or a trainer its
/// scripts battle (<c>as "…"</c>, <c>with "…"</c>) knows it, when a wild Pokémon of its grass, water or rods knows it at a
/// level it is met at, or when a TM or HM is found or given there (a ball, a hidden item, a script). The starters, which
/// the player's own team and the rival's first one are made of, count as seen in Twinleaf Town. GPU-free; reads the
/// game's own files.
/// </summary>
internal static class MoveSightings
{
    /// <summary>The last area of the first chapter (Twinleaf Town to Oreburgh's Gym) in the order of opening.</summary>
    public const string FirstChapterEnds = "oreburgh_mine_b2f";

    /// <summary>A move, and the place where it is first seen with its rank in the order.</summary>
    public sealed record Sighting(string Move, string Place, int Rank, string How);

    private static readonly Lazy<IReadOnlyList<Sighting>> all = new(Work);

    /// <summary>Every Platinum move nothing shows before the post-game is not in it; in order of first sighting.</summary>
    public static IReadOnlyList<Sighting> All => all.Value;

    /// <summary>Platinum's moves, by name, that are seen by the end of the first chapter.</summary>
    public static IReadOnlyList<string> FirstChapter
    {
        get
        {
            int end = PlaceRanks.Value[FirstChapterEnds];
            return All.Where(s => s.Rank <= end).Select(s => s.Move).ToList();
        }
    }

    /// <summary>The first area of Y7's second part (plan 25 · Y7b): Y7's first part is everything before it.</summary>
    public const string SeventhBatchSplits = "route_206";

    /// <summary>
    /// Platinum's moves, by name, that Y7's first part takes: seen after the first chapter and before
    /// <see cref="SeventhBatchSplits"/> (Route 204 to Team Galactic's Eterna Building, its rooms with it).
    /// </summary>
    public static IReadOnlyList<string> SeventhBatchFirstPart
    {
        get
        {
            int after = PlaceRanks.Value[FirstChapterEnds], before = PlaceRanks.Value[SeventhBatchSplits];
            return All.Where(s => s.Rank > after && s.Rank < before).Select(s => s.Move).ToList();
        }
    }

    private static readonly Lazy<Dictionary<string, int>> PlaceRanks = new(() => Ranks().Ranks);

    /// <summary>The areas in their order, then every room ranked by the place whose warp first leads into it.</summary>
    private static (Dictionary<string, int> Ranks, Func<string, Map> Maps) Ranks()
    {
        var maps = MapDatabase.Fresh();
        var world = World.LoadAll().First(w => w.Index.Region == "Sinnoh");
        var ranks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < world.Index.Areas.Count; i++) ranks.TryAdd(world.Index.Areas[i], i * 100);

        // Rooms take the rank of the place they are first come to from, a little after it
        var queue = new List<(string Room, int Rank)>();
        foreach (string mapName in MapDatabase.MapNames)
        {
            var map = maps(mapName);
            foreach (var warp in map.Warps)
            {
                int from = map.AreaAt(warp.SourceX, warp.SourceY) is { } area && ranks.TryGetValue(area.Key, out int r) ? r
                    : map.Areas.Count == 0 ? -1 : -1;
                if (from >= 0 && map.Areas.Count > 0) queue.Add((warp.TargetMap, from + 1));
            }
        }
        // Rooms of rooms, until nothing new is found
        bool changed = true;
        foreach (var (room, rank) in queue)
            if (maps(room).Areas.Count == 0 && (!ranks.TryGetValue(room, out int had) || rank < had)) ranks[room] = rank;
        while (changed)
        {
            changed = false;
            foreach (string mapName in MapDatabase.MapNames)
            {
                var map = maps(mapName);
                if (map.Areas.Count > 0 || !ranks.TryGetValue(map.Name, out int rank)) continue;
                foreach (var warp in map.Warps)
                {
                    var target = maps(warp.TargetMap);
                    if (target.Areas.Count > 0) continue;
                    if (!ranks.TryGetValue(target.Name, out int had) || rank + 1 < had)
                    {
                        ranks[target.Name] = rank + 1;
                        changed = true;
                    }
                }
            }
        }
        return (ranks, maps);
    }

    /// <summary>Places that come early in the order of opening but that only Surf leads to.</summary>
    private static readonly HashSet<string> OverWater = new(StringComparer.OrdinalIgnoreCase) { "route_219", "route_220" };

    private static readonly Regex ScriptTrainer = new("\\b(?:as|with) \"([a-z0-9_]+)\"", RegexOptions.Compiled);
    private static readonly Regex Machine = new("\"((?:TM|HM)\\d\\d)\"", RegexOptions.Compiled);

    private static IReadOnlyList<Sighting> Work()
    {
        var (ranks, maps) = Ranks();
        var first = new Dictionary<string, Sighting>(StringComparer.OrdinalIgnoreCase);
        var platinum = MoveDatabase.GetAll().Where(m => m.Id <= 467).Select(m => m.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // What the water and the rods show is seen only once the player can reach it: HM03 and each rod are given by a
        // script somewhere, and a place only Surf leads to (Route 219, south of Sandgem Town) waits for HM03 too
        var given = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var (place, rank) in ranks)
        {
            string path = GameDataFiles.PathOf(Path.Combine("scripts", place + ".txt"));
            if (!File.Exists(path)) continue;
            string text = File.ReadAllText(path);
            foreach (string tool in new[] { "HM03", "Old Rod", "Good Rod", "Super Rod" })
                if (text.Contains($"give \"{tool}\"") && (!given.TryGetValue(tool, out int had) || rank < had)) given[tool] = rank;
        }
        int surf = given["HM03"];
        int[] rods = { given["Old Rod"], given["Good Rod"], given["Super Rod"] };

        int late = 0;
        void Saw(string move, string place, string how)
        {
            if (!platinum.Contains(move)) return;
            int rank = Math.Max(late, ranks[place]);
            if (OverWater.Contains(place)) rank = Math.Max(rank, surf);
            if (first.TryGetValue(move, out var had) && had.Rank <= rank) return;
            first[move] = new Sighting(MoveDatabase.Get(move)!.Name, place, rank, how);
        }
        void Later(int when, Action see)
        {
            late = when;
            see();
            late = 0;
        }
        void Team(Party party, string place, string who)
        {
            foreach (var p in party.Members)
                foreach (var m in p.Moves) Saw(m.Data.Name, place, $"{who}'s {p.Species.Name}");
        }
        void Wild(IEnumerable<WildEncounterEntry> table, string place, string how)
        {
            foreach (var e in table)
            {
                if (PokemonDatabase.Get(e.SpeciesName) is not { } species) continue;
                for (int level = e.MinLevel; level <= e.MaxLevel; level++)
                    foreach (var m in new Pokemon(species, level, new Random(level)).Moves) Saw(m.Data.Name, place, $"a wild {species.Name} ({how})");
            }
        }
        void Item(string? item, string place, string how)
        {
            if (item != null && ItemDatabase.Get(item) is { } data && MoveTeaching.IsMachine(data)) Saw(data.TeachesMove!, place, $"{data.Name} ({how})");
        }
        void Scripts(string file, string place)
        {
            string path = GameDataFiles.PathOf(Path.Combine("scripts", file + ".txt"));
            if (!File.Exists(path)) return;
            string text = File.ReadAllText(path);
            foreach (Match match in ScriptTrainer.Matches(text))
                if (TrainerDatabase.Get(match.Groups[1].Value) is { } record) Team(TrainerDatabase.PartyOf(record), place, record.Id);
            foreach (Match match in Machine.Matches(text)) Item(match.Groups[1].Value, place, "a script");
        }

        // The starters
        string start = ranks.OrderBy(r => r.Value).First().Key;
        foreach (string starter in RegionDatabase.Get("Sinnoh")!.Starters)
            foreach (var m in new Pokemon(PokemonDatabase.Get(starter)!, 5, new Random(5)).Moves) Saw(m.Data.Name, start, $"the starter {starter}");

        foreach (string mapName in MapDatabase.MapNames)
        {
            var map = maps(mapName);
            if (map.Areas.Count == 0)
            {
                if (!ranks.ContainsKey(map.Name)) continue;
                Wild(map.WildEncounters, map.Name, "grass");
                foreach (var person in map.Everyone)
                {
                    if (person.TrainerData is { } t) Team(t.Party, map.Name, t.Id ?? person.Name);
                    if (person.IsItemBall) Item(person.Item, map.Name, "a ball");
                }
                foreach (var hidden in map.HiddenItems.Values) Item(hidden.Item, map.Name, "hidden");
                Scripts(map.Name, map.Name);
                continue;
            }
            foreach (var area in map.Areas)
            {
                if (!ranks.ContainsKey(area.Key)) continue;
                Wild(area.WildEncounters, area.Key, "grass");
                Later(surf, () => Wild(area.WaterEncounters, area.Key, "surf"));
                for (int rod = 0; rod < 3; rod++)
                {
                    int r = rod;
                    Later(rods[r], () => Wild(area.RodEncounters[r], area.Key, "rod"));
                }
                Scripts(area.Key, area.Key);
            }
            foreach (var person in map.Everyone)
            {
                if (map.AreaAt(person.GridX, person.GridY) is not { } stands || !ranks.ContainsKey(stands.Key)) continue;
                if (person.TrainerData is { } t) Team(t.Party, stands.Key, t.Id ?? person.Name);
                if (person.IsItemBall) Item(person.Item, stands.Key, "a ball");
            }
            foreach (var ((x, y), hidden) in map.HiddenItems)
                if (map.AreaAt(x, y) is { } at && ranks.ContainsKey(at.Key)) Item(hidden.Item, at.Key, "hidden");
        }
        return first.Values.OrderBy(s => s.Rank).ThenBy(s => s.Move, StringComparer.Ordinal).ToList();
    }
}
