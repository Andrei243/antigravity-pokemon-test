using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// A pack held to what the game can read (plan 16 · T15; GPU-free; <c>--validate-packs</c>): its manifest; every JSON
/// file read as JSON; each patch laid over the game's file and the result read as the game reads that file; its
/// overlays and rooms read as the world reads them; every script parsed, and the scripts of its files asked
/// <see cref="ScriptLibrary.Problems"/> beside the game's own; every song parsed; and nothing it carries that the game
/// never reads from a pack (a <c>world.json</c>, a patch of a file that can't be patched). A finding is a line naming
/// the file and what is wrong; none means the pack passes. The check reads the game's data beside the pack's and puts
/// nothing in use.
/// </summary>
public static class PackCheck
{
    /// <summary>What <c>--validate-packs</c> exits with when a pack has a finding.</summary>
    public const int Findings = 6;

    /// <summary>Every finding of one pack's folder; empty when it passes.</summary>
    public static List<string> Check(string folder)
    {
        var findings = new List<string>();
        PackManifest manifest;
        try
        {
            manifest = DataLayers.ReadManifest(folder);
        }
        catch (PackException e)
        {
            findings.Add(e.Message);
            return findings;
        }
        var pack = new DataLayer(folder, manifest);
        var over = new[] { pack };
        string Rel(string path) => Path.GetRelativePath(folder, path).Replace('\\', '/');

        foreach (string path in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal))
        {
            string rel = Rel(path);
            if (rel.StartsWith("overrides/", StringComparison.Ordinal) || rel == DataLayers.ManifestFile) continue;
            if (DataLayers.IsFixed(rel)) findings.Add($"{rel}: what is open of a region is the game's own; a pack's world.json is never read.");
            if (!rel.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;
            try
            {
                JsonNode.Parse(File.ReadAllText(path), documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            }
            catch (JsonException e)
            {
                findings.Add($"{rel}: it isn't JSON ({e.Message}).");
                continue;
            }

            if (rel.EndsWith(DataLayers.PatchSuffix, StringComparison.OrdinalIgnoreCase))
            {
                string patched = rel[..^DataLayers.PatchSuffix.Length] + ".json";
                if (!DataLayers.Patchable.ContainsKey(Path.GetFileName(patched)))
                {
                    findings.Add($"{rel}: {Path.GetFileName(patched)} can't be patched; a pack patches {string.Join(", ", DataLayers.Patchable.Keys)}.");
                    continue;
                }
                if (!File.Exists(Path.Combine(GameDataFiles.Directory, patched)) && !File.Exists(Path.Combine(folder, patched)))
                {
                    findings.Add($"{rel}: there is no {patched} to patch.");
                    continue;
                }
                Read(rel, () => ReadAs(Path.GetFileName(patched), DataLayers.ReadPatched(patched, over)), findings);
            }
            else if (rel.Contains("/overlays/", StringComparison.Ordinal))
                Read(rel, () => GameDataFiles.Deserialize<WorldOverlayFile>(File.ReadAllText(path)), findings);
            else if (rel.StartsWith("regions/", StringComparison.Ordinal))
                Read(rel, () => GameDataFiles.Deserialize<RegionFile>(File.ReadAllText(path)).MusicSound ?? (object)rel, findings);
            else if (rel.StartsWith(MapDatabase.Folder + "/", StringComparison.Ordinal))
                Read(rel, () => GameDataFiles.Deserialize<MapFile>(File.ReadAllText(path)).ToMap(), findings);
            else if (DataLayers.Patchable.ContainsKey(Path.GetFileName(rel)))
                Read(rel, () => ReadAs(Path.GetFileName(rel), File.ReadAllText(path)), findings);
        }

        // The songs: every one of the pack's parsed; the ids the scripts may name are the game's and the pack's
        var songs = new HashSet<string>(MusicLibrary.Ids, StringComparer.OrdinalIgnoreCase);
        string music = Path.Combine(folder, "music");
        if (Directory.Exists(music))
            foreach (string path in Directory.EnumerateFiles(music, "*.mml", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal))
            {
                string id = Path.GetRelativePath(music, path).Replace('\\', '/')[..^".mml".Length];
                songs.Add(id);
                Read(Rel(path), () => Mml.Parse(id, File.ReadAllText(path)), findings);
            }

        // The scripts: the pack's files with the game's, a pack's file in place of the game's of the same name
        string scripts = Path.Combine(folder, ScriptLibrary.Folder);
        if (Directory.Exists(scripts))
        {
            var own = Directory.EnumerateFiles(scripts, "*.txt").Select(Path.GetFileNameWithoutExtension).ToHashSet(StringComparer.Ordinal);
            try
            {
                var library = ScriptLibrary.FromSources(DataLayers.FilesIn(ScriptLibrary.Folder, "*.txt", over: over)
                    .Select(f => (Path.GetFileNameWithoutExtension(f.Path), File.ReadAllText(f.Path))).ToArray());
                foreach (string problem in library.Problems(songExists: songs.Contains))
                    if (own.Any(f => problem.StartsWith(f + ".txt(", StringComparison.Ordinal)))
                        findings.Add($"{ScriptLibrary.Folder}/{problem}");
            }
            catch (ScriptException e)
            {
                findings.Add($"{ScriptLibrary.Folder}/{e.Message}");
            }
        }
        CheckRegions(folder, pack, findings);
        return findings;
    }

    /// <summary>The words an attendant has for the way across (<c>attendant.vessel.*</c>), and so the vessels a pack's link may name.</summary>
    public static readonly IReadOnlyList<string> Vessels = new[] { "ship", "ferry", "plane", "way" };

    /// <summary>
    /// A region a pack adds (plan 16 · T14; docs/packs.md, "A region of your own") held to the rules the game's regions
    /// are: it joins the chain after the last region with maps; every map of it is the pack's own and belongs to it
    /// alone, and every map the pack's world makes belongs to exactly one region; no way out of its maps leads into
    /// another region; its start and arrival stand on open ground of its own maps; and the way to it names where it
    /// leaves from, in the region before, with an attendant standing there on open ground. Read from the pack's files
    /// as they are, with nothing put in use and no map of the pack built (a trainer of its own is no trainer until it is).
    /// </summary>
    private static void CheckRegions(string folder, DataLayer pack, List<string> findings)
    {
        string Rel(string path) => Path.GetRelativePath(folder, path).Replace('\\', '/');
        var chain = RegionDatabase.With(new[] { pack });
        string regionFolder = Path.Combine(folder, "regions");
        foreach (string problem in chain.Problems)
            findings.Add(problem.Replace(regionFolder + Path.DirectorySeparatorChar, "regions/", StringComparison.Ordinal));
        // The file a region of the pack's was read from, as a finding names it
        string FileOf(Region region) =>
            (Directory.Exists(regionFolder) ? Directory.GetFiles(regionFolder, "*.json") : Array.Empty<string>())
            .Where(p => string.Equals(Path.GetFileNameWithoutExtension(p), region.Id, StringComparison.OrdinalIgnoreCase))
            .Select(Rel).FirstOrDefault() ?? "regions/";
        var own = chain.OfPacks.ToList();

        // The maps the pack makes: its rooms, and its worlds' maps (from the worlds' own index, a region's of its own),
        // each with the maps its ways lead to and whether a tile of it is open ground
        var ways = new Dictionary<string, (string File, List<string> To, Func<int, int, bool?> Open)>(StringComparer.OrdinalIgnoreCase);
        var worldMaps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string rooms = Path.Combine(folder, MapDatabase.Folder);
        if (Directory.Exists(rooms))
            foreach (string path in Directory.GetFiles(rooms, "*.json").OrderBy(p => p, StringComparer.Ordinal))
            {
                try
                {
                    var room = GameDataFiles.Deserialize<MapFile>(File.ReadAllText(path));
                    var map = room.ToMap();
                    ways[room.Name] = (Rel(path), room.Warps.Select(w => w.TargetMap).ToList(),
                        (x, y) => map.InBounds(x, y) ? !map.IsSolid(x, y) : null);
                }
                catch (Exception e) when (e is JsonException or InvalidDataException or ArgumentException or IndexOutOfRangeException or ArgumentOutOfRangeException)
                {
                    // Said above, as the room was read
                }
            }
        string worlds = Path.Combine(folder, World.Folder);
        if (Directory.Exists(worlds))
            foreach (string dir in Directory.GetDirectories(worlds).OrderBy(d => d, StringComparer.Ordinal))
            {
                string index = Path.Combine(dir, World.IndexFile);
                if (!File.Exists(index) || DataLayers.IsFixed(Rel(index))) continue;
                WorldIndexFile file;
                try
                {
                    file = GameDataFiles.Deserialize<WorldIndexFile>(File.ReadAllText(index));
                }
                catch (JsonException)
                {
                    continue; // Said above, as every JSON file was read
                }
                if (!own.Any(r => string.Equals(r.Id, file.Region, StringComparison.OrdinalIgnoreCase)))
                    findings.Add($"{Rel(index)}: its region, \"{file.Region}\", is no region this pack adds; a world of a region the game has is the game's own.");
                var world = World.Open(dir, file);
                foreach (var entry in file.Maps)
                {
                    worldMaps[entry.Name] = Rel(index);
                    var to = new List<string>();
                    foreach (string key in file.Areas)
                    {
                        try
                        {
                            if (world.Area(key) is { } area && area.Matrix == entry.Matrix) to.AddRange(area.Warps.Select(w => w.To));
                            if (world.Overlay(key) is { } overlay)
                            {
                                to.AddRange((overlay.Doors ?? new()).Select(d => d.Map));
                                to.AddRange((overlay.Exits ?? new()).Select(x => x.Map));
                            }
                        }
                        catch (Exception e) when (e is JsonException or InvalidDataException)
                        {
                            findings.Add($"{Rel(dir)}: the area {key} can't be read ({e.Message}).");
                        }
                    }
                    int matrix = entry.Matrix;
                    ways[entry.Name] = (Rel(index), to.Where(t => t.Length > 0).ToList(), (x, y) =>
                    {
                        try
                        {
                            return world.ChunkAt(matrix, x, y) is { } at ? !at.Chunk.SolidAt(at.X, at.Z) : null;
                        }
                        catch (Exception e) when (e is IOException or InvalidDataException or JsonException)
                        {
                            return null;
                        }
                    });
                }
            }

        // Every map of the pack's own worlds belongs to exactly one region
        foreach (var (map, file) in worldMaps)
        {
            int count = chain.Regions.Count(r => r.Maps.Contains(map, StringComparer.OrdinalIgnoreCase));
            if (count != 1) findings.Add($"{file}: the map {map} belongs to {(count == 0 ? "no region" : $"{count} regions")}; every map belongs to exactly one.");
        }

        var game = RegionDatabase.With(Array.Empty<DataLayer>());
        foreach (var region in own)
        {
            string file = FileOf(region);
            if (region.Maps.Count == 0) findings.Add($"{file}: {region.Id} lists no maps (\"maps\").");
            // The field its outdoors are walked on is a word the game knows (plan 32 · OW1)
            try { _ = region.Field; }
            catch (FormatException e) { findings.Add($"{file}: {e.Message}."); }
            foreach (string map in region.Maps)
            {
                if (!ways.ContainsKey(map)) findings.Add($"{file}: {region.Id} lists the map {map}, which the pack doesn't make (a room of maps/ or a map of its world).");
                if (game.RegionOfMap(map) is { } theirs) findings.Add($"{file}: the map {map} is {theirs.Id}'s; a map belongs to exactly one region.");
                foreach (var other in own.Where(o => o != region && o.Maps.Contains(map, StringComparer.OrdinalIgnoreCase)))
                    findings.Add($"{file}: the map {map} is listed by {other.Id} too; a map belongs to exactly one region.");
                // No way out of the region's maps leads into another region, nor nowhere
                if (ways.TryGetValue(map, out var made))
                    foreach (string to in made.To.Distinct(StringComparer.OrdinalIgnoreCase))
                        if (!region.Maps.Contains(to, StringComparer.OrdinalIgnoreCase))
                            findings.Add($"{made.File}: a way out of {map} leads to {to}, which is {(chain.RegionOfMap(to) is { } beyond ? $"{beyond.Id}'s" : "no map of the region")}; no way crosses regions.");
            }
            void Place(string what, MapSpot? spot)
            {
                if (spot == null)
                {
                    if (what == "start") findings.Add($"{file}: {region.Id} has no \"start\": a region of a pack's needs one, or it has no maps to play in.");
                    return;
                }
                if (!region.Maps.Contains(spot.Map, StringComparer.OrdinalIgnoreCase)) findings.Add($"{file}: its {what} is on {spot.Map}, which isn't one of its maps.");
                else if (ways.TryGetValue(spot.Map, out var made) && made.Open(spot.X, spot.Y) != true)
                    findings.Add($"{file}: its {what}, {spot.X},{spot.Y} on {spot.Map}, is no open ground.");
            }
            Place("start", region.Start);
            Place("arrival", region.Arrival);

            // The way to it: from the region before, named in the file, with someone to take the player across
            var link = chain.Links.FirstOrDefault(l => string.Equals(l.To, region.Id, StringComparison.OrdinalIgnoreCase));
            if (link == null) continue;
            if (link.Vessel != null && !Vessels.Contains(link.Vessel))
                findings.Add($"{file}: the way to {region.Id} is a \"{link.Vessel}\"; an attendant knows {string.Join(", ", Vessels)}.");
            var before = chain.Get(link.From)!;
            if (link.DepartureMap == null)
            {
                findings.Add($"{file}: the way to {region.Id} from {before.Id} names no \"departureMap\": every way between two regions with maps leaves from one, with an attendant.");
                continue;
            }
            if (!before.Maps.Contains(link.DepartureMap, StringComparer.OrdinalIgnoreCase))
            {
                findings.Add($"{file}: the way to {region.Id} leaves from {link.DepartureMap}, which is no map of {before.Id}.");
                continue;
            }
            var departure = MapDatabase.TryGet(link.DepartureMap);
            if (link.Attendant is { } who)
            {
                if (who.IsTransportAttendant != true) findings.Add($"{file}: the attendant at {link.DepartureMap} needs \"isTransportAttendant\": true.");
                if (departure != null && (!departure.InBounds(who.X, who.Y) || !departure.IsWalkable(who.X, who.Y)))
                    findings.Add($"{file}: the attendant stands at {who.X},{who.Y} on {link.DepartureMap}, which is no open ground.");
                else if (departure != null && departure.Everyone.Any(n => n.GridX == who.X && n.GridY == who.Y))
                    findings.Add($"{file}: the attendant stands at {who.X},{who.Y} on {link.DepartureMap}, where someone stands already.");
            }
            else if (departure == null || !departure.Everyone.Any(n => n.IsTransportAttendant))
                findings.Add($"{file}: nobody takes the player from {link.DepartureMap} to {region.Id}: the link needs an \"attendant\".");
        }
    }

    /// <summary>A file of the data read as the game reads it.</summary>
    private static object ReadAs(string file, string text) => file.ToLowerInvariant() switch
    {
        "species.json" => GameDataFiles.Deserialize<List<PokemonSpecies>>(text),
        "moves.json" => GameDataFiles.Deserialize<List<MoveData>>(text),
        "abilities.json" => GameDataFiles.Deserialize<List<AbilityDatabase.AbilityRecord>>(text),
        "items.json" => GameDataFiles.Deserialize<List<ItemData>>(text),
        "trainers.json" => GameDataFiles.Deserialize<List<TrainerRecord>>(text),
        "characters.json" => CharacterStyles.Parse(text),
        _ => JsonNode.Parse(text)!
    };

    private static void Read(string rel, Func<object> read, List<string> findings)
    {
        try
        {
            read();
        }
        catch (Exception e) when (e is JsonException or InvalidDataException or ScriptException or FormatException or ArgumentException or InvalidOperationException or KeyNotFoundException or NotSupportedException)
        {
            findings.Add($"{rel}: {e.Message}");
        }
    }

    /// <summary>
    /// <c>--validate-packs</c>: every pack of these folders checked, a line to each finding (and a line to each pack
    /// that passes) said through <paramref name="say"/>; <see cref="Findings"/> when any pack has one, nought otherwise.
    /// </summary>
    public static int CheckAll(IEnumerable<string> packFolders, Action<string> say)
    {
        int found = 0, packs = 0;
        foreach (string root in packFolders.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase))
            foreach (string dir in Directory.GetDirectories(root).OrderBy(d => d, StringComparer.Ordinal))
            {
                packs++;
                var findings = Check(dir);
                if (findings.Count == 0) say($"{dir}: passes.");
                foreach (string finding in findings) say($"{dir}: {finding}");
                found += findings.Count;
            }
        say(packs == 0 ? $"No packs were found in {string.Join(" or ", packFolders)}." : found == 0 ? $"{packs} pack(s) checked: nothing found." : $"{packs} pack(s) checked: {found} finding(s).");
        return found > 0 ? Findings : 0;
    }
}
