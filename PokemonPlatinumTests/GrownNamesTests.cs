using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 18 · W4b-2 with plan 03 · D15-A: in a region the game crossed into, a trainer's Pokémon is met at the stage its
/// mapped level allows, the games' and then ours, so a line of theirs that names a forebear their team no longer has
/// ("my Rattata" from a Youngster whose Rattata is met as Ratchisel) would say what isn't there. Every line a trainer
/// says (their own two lines and words of the place where they stand, their lines in battle, the scripts that battle
/// them and their calls) names the species they are met with, but those <c>tools/dev/grown-names.json</c> keeps on
/// purpose (a wild one, a find, a catch). <c>node tools/dev/grown-names.mjs --write</c> rewrites what this finds.
/// </summary>
public class GrownNamesTests
{
    /// <summary>What each crossed region's trainers' Pokémon become, by id: a forebear's name and its one new species (none where two of it part ways).</summary>
    private static readonly Lazy<Dictionary<string, Dictionary<string, string>>> Changes = new(() =>
    {
        var changes = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        foreach (var region in RegionDatabase.All.Where(r => r.Levels != null && r.Id != RegionDatabase.First.Id))
        {
            var records = TrainerDatabase.OfRegion(region.Folder).Concat(TrainerDatabase.Ours.Where(t => t.Id.StartsWith(region.Folder + "_", StringComparison.Ordinal)));
            foreach (var record in records)
            {
                var met = new Trainer { Party = TrainerDatabase.PartyOf(record) }.Relevelled(region.Levels!.Map, grow: true);
                var into = new Dictionary<string, string?>(StringComparer.Ordinal);
                foreach (var (own, now) in record.Party.Zip(met.Party.Members))
                {
                    string grown = now.Species.Name;
                    if (into.TryGetValue(own.Species, out var before)) { if (before != grown) into[own.Species] = null; }
                    else into[own.Species] = grown;
                }
                changes[record.Id] = into.Where(kv => kv.Value != null && kv.Value != kv.Key).ToDictionary(kv => kv.Key, kv => kv.Value!);
            }
        }
        return changes;
    });

    private static readonly Lazy<string[]> GamesNames = new(() =>
        PokemonDatabase.GetAll().Select(s => s.Name).OrderByDescending(n => n.Length).ToArray());

    private static readonly Lazy<HashSet<string>> Kept = new(() =>
    {
        using var file = JsonDocument.Parse(File.ReadAllText(Path.Combine(Repository.Root, "tools", "dev", "grown-names.json")));
        return file.RootElement.GetProperty("kept").EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
    });

    /// <summary>The forebears a line names whose trainer's team has them only as something else.</summary>
    private static IEnumerable<string> Stale(string line, IEnumerable<string> ids)
    {
        if (Kept.Value.Contains(line)) yield break;
        foreach (string name in GamesNames.Value)
        {
            if (!Regex.IsMatch(line, $@"(?<![\p{{L}}]){Regex.Escape(name)}(?![\p{{L}}])")) continue;
            var intos = ids.Select(id => Changes.Value.TryGetValue(id, out var c) && c.TryGetValue(name, out var into) ? into : null).Where(i => i != null).Distinct().ToList();
            if (intos.Count == 1) yield return $"{name} (now {intos[0]})";
        }
    }

    [Fact]
    public void SomeTrainersOfEveryCrossedRegionAreMetWithSpeciesOfOurs()
    {
        foreach (var region in RegionDatabase.All.Where(r => r.Levels != null && r.Id != RegionDatabase.First.Id && r.Id != RegionDatabase.Kanto))
            Assert.Contains(Changes.Value, kv => kv.Key.StartsWith(region.Folder + "_", StringComparison.Ordinal)
                && kv.Value.Values.Any(s => PokemonDatabase.Get(s) is { IsOurs: true }));
    }

    [Fact]
    public void NoLineOfATrainersNamesAForebearTheirTeamNowReplaces()
    {
        var found = new List<string>();
        void Check(string where, string line, IEnumerable<string> ids)
        {
            foreach (string stale in Stale(line, ids.ToList())) found.Add($"{where}: {stale}: {line}");
        }

        // Their own two lines and their words, on the maps
        var maps = MapDatabase.Fresh();
        foreach (string mapName in MapDatabase.MapNames)
        {
            if (RegionDatabase.RegionOfMap(mapName) is not { Levels: not null } region || region.Id == RegionDatabase.First.Id) continue;
            foreach (var person in maps(mapName).Everyone.Where(n => n.TrainerData is { Id.Length: > 0 }))
            {
                var ids = new[] { person.TrainerData!.Id };
                Check(mapName, person.TrainerData.DialogueBefore, ids);
                Check(mapName, person.TrainerData.DialogueAfter, ids);
                foreach (string line in person.DialogLines) Check(mapName, line, ids);
            }
        }

        // Their lines in battle
        foreach (var (key, set) in TrainerLines.All)
        {
            var ids = Changes.Value.Keys.Where(id => TrainerLines.IsStem(key) ? id.StartsWith(key[..^1], StringComparison.Ordinal) : id == key).ToList();
            foreach (var line in new[] { set.LastPokemon, set.FirstDown, set.LowHp })
                if (line != null) Check($"trainer-lines {key}", line.Text, ids);
        }

        // The scripts that battle them, and their calls
        var phone = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var region in RegionDatabase.All)
        {
            string book = Path.Combine(Repository.Root, "PokemonPlatinumEngine", "Data", "world", region.Folder, "phone.json");
            if (!File.Exists(book)) continue;
            using var contacts = JsonDocument.Parse(File.ReadAllText(book), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
            foreach (var contact in contacts.RootElement.EnumerateArray())
            {
                if (!contact.TryGetProperty("trainer", out var trainer)) continue;
                string id = trainer.GetString()!;
                var ids = Changes.Value.Keys.Where(k => k == id || k.StartsWith(id + "_", StringComparison.Ordinal) && Regex.IsMatch(k[id.Length..], @"^_\d+$")).ToList();
                string name = contact.GetProperty("id").GetString()!.Replace("_", "");
                phone[name] = ids;
                phone[region.Id + name] = ids;
            }
        }
        foreach (string file in Directory.GetFiles(Path.Combine(Repository.Root, "PokemonPlatinumEngine", "Data", "scripts"), "*.txt"))
        {
            bool calls = Path.GetFileNameWithoutExtension(file) == "phone";
            foreach (string block in Regex.Split(File.ReadAllText(file), @"^(?=script )", RegexOptions.Multiline))
            {
                var ids = Regex.Matches(block, @"^\s*battle\b[^\n]*", RegexOptions.Multiline)
                    .SelectMany(m => Regex.Matches(m.Value, @"(?:as|with) ""([a-z0-9_]+)""").Select(a => a.Groups[1].Value))
                    .Where(Changes.Value.ContainsKey).ToList();
                if (calls && Regex.Match(block, @"^script (\S+)") is { Success: true } named && phone.TryGetValue(named.Groups[1].Value, out var called)) ids.AddRange(called);
                if (ids.Count == 0) continue;
                foreach (Match said in Regex.Matches(block, @"\b(?:say|text|ask|sayown)\s+""((?:[^""\\]|\\.)*)"""))
                    Check(Path.GetFileName(file), said.Groups[1].Value, ids);
            }
        }

        Assert.True(found.Count == 0, "Lines that name a Pokémon their trainer is no longer met with (node tools/dev/grown-names.mjs --write):\n" + string.Join("\n", found));
    }

    [Fact]
    public void EveryLineKeptIsStillALineOfTheGame()
    {
        string data = Path.Combine(Repository.Root, "PokemonPlatinumEngine", "Data");
        var all = string.Join("\n", Directory.GetFiles(data, "*.txt", SearchOption.AllDirectories).Concat(Directory.GetFiles(Path.Combine(data, "world"), "*.json", SearchOption.AllDirectories))
            .Select(File.ReadAllText));
        foreach (string line in Kept.Value) Assert.Contains(line, all);
    }
}
