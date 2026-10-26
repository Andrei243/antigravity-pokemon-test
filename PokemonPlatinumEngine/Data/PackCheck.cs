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
        return findings;
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
