using System.Text;
using System.Text.Json;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// One spoken line of the game (plan 26 · V1), by the key plan 17 · N4 gave it: who speaks it, the English as shown and
/// a hash of it, the spoken text, and what a human wrote about how it goes. <c>Data/voice/lines.json</c> is these,
/// written by <c>tools/VoiceTool catalogue</c>; <see cref="Direction"/>, <see cref="Notes"/> and a hand-written spoken
/// text (<see cref="Manual"/>) are kept across runs, everything else is made afresh.
/// </summary>
public sealed class VoiceLine
{
    /// <summary>The line's address: <c>script:file.Name#2.0</c>, <c>world:area.person.dialog.1</c>, <c>map:Room.person.before.0</c>, <c>intro:sinnoh.Hello.0</c>.</summary>
    public string Key { get; set; } = "";

    /// <summary>The cast's voice (<c>rival</c>, <c>look:lass#2</c>), or <see cref="VoiceCast.Self"/> where the person spoken to decides at run time.</summary>
    public string Voice { get; set; } = "";

    /// <summary>The display name a script's <c>speaker</c> gave, or the person's name; null where there is none.</summary>
    public string? Speaker { get; set; }

    /// <summary>The line as the text box shows it (English), placeholders in.</summary>
    public string En { get; set; } = "";

    /// <summary>Eight hexadecimal digits of <see cref="En"/>: audio made from other English is stale.</summary>
    public string Hash { get; set; } = "";

    /// <summary>What is said, or null where the line is shown only. May still hold placeholders with few values, which <see cref="Variants"/> fill.</summary>
    public string? Spoken { get; set; }

    public string Status { get; set; } = StatusSpoken;

    /// <summary>calm, excited, shouting, whispering, sad or smug; written by hand.</summary>
    public string? Direction { get; set; }
    public string? Notes { get; set; }

    /// <summary>The spoken text, or the decision to leave the line shown only, was written by hand and is kept while <see cref="Hash"/> holds.</summary>
    public bool? Manual { get; set; }

    public List<VoiceVariant>? Variants { get; set; }

    public const string StatusSpoken = "spoken", StatusShownOnly = "shownOnly";
}

/// <summary>A take of a line for a value of a placeholder with few values (<c>~starter=Piplup</c>), or for a default name the player kept (<c>~named</c>, by hand).</summary>
public sealed class VoiceVariant
{
    public string Key { get; set; } = "";
    public string Spoken { get; set; } = "";
    public bool? Manual { get; set; }
}

public sealed class VoiceFile
{
    public int Version { get; set; } = 1;
    public List<VoiceLine> Lines { get; set; } = new();
}

/// <summary>
/// The catalogue of every spoken line (plan 26 · V1): <see cref="Build"/> reads the scripts, the overlays, the hand-made
/// maps and the introduction exactly as <see cref="TextSources"/> does for the translations, gives each line that is said
/// by a character its key, its voice and its spoken text, and counts what is left unvoiced by decision (the game's own
/// voice, signs, a question's answers, Pokémon's cries). Free of the GPU and of any window.
/// </summary>
public static class VoiceCatalogue
{
    public const string LinesPath = "Data/voice/lines.json", CastPath = "Data/voice/cast.json", CoveragePath = "docs/voice/coverage.md";

    /// <summary>The result of reading the game: the lines, and what is wrong with the casting.</summary>
    public sealed class Catalogue
    {
        public List<VoiceLine> Lines { get; } = new();

        /// <summary>Speakers of scripts whose display name matches no cast entry (and ids the cast lacks), by name, with where they are first written.</summary>
        public SortedDictionary<string, string> Unlisted { get; } = new(StringComparer.Ordinal);

        /// <summary>Ids a script writes before a speaker's name that the cast lacks, with where.</summary>
        public SortedDictionary<string, string> UnknownIds { get; } = new(StringComparer.Ordinal);

        /// <summary>What is not voiced by decision, by reason, with how many lines.</summary>
        public SortedDictionary<string, int> Unvoiced { get; } = new(StringComparer.Ordinal);
    }

    // ------------------------------------------------------------------ reading the game

    private sealed record Person(string Kind, string Place, string Key, string Name, string? Look, string? TrainerName, string? TrainerClass,
        string? Script, IReadOnlyList<string> Dialog, string? Before, string? After);

    public static Catalogue Build(CastFile castFile, VoiceFile? previous = null, ScriptLibrary? library = null)
    {
        var cast = new VoiceCast(castFile);
        library ??= ScriptLibrary.Default;
        var result = new Catalogue();
        var persons = PersonsOfTheGame(result);
        var voiceOf = new Dictionary<Person, string>();
        foreach (var p in persons)
            voiceOf[p] = cast.ForPerson(p.Place, p.Key, p.Name, p.TrainerName, p.TrainerClass, p.Look, p.TrainerClass != null && TrainerDatabase.IsRival(p.TrainerClass));

        // Who runs which script: the voice of "the person spoken to" is known where one voice does
        var owners = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var p in persons.Where(p => !string.IsNullOrEmpty(p.Script)))
        {
            string full = p.Script!.Contains('.') ? p.Script : p.Place + "." + p.Script;
            if (!owners.TryGetValue(full, out var set)) owners[full] = set = new HashSet<string>(StringComparer.Ordinal);
            set.Add(voiceOf[p]);
        }

        var regions = RegionOfFiles();
        Func<string, IReadOnlyList<string>?> ValuesFor(string? region) => name => name switch
        {
            "assistant" => new[] { PlayerIdentity.DefaultName(PlayerLook.Boy), PlayerIdentity.DefaultName(PlayerLook.Girl) },
            "starter" or "rivalstarter" => StartersOf(region),
            _ => null
        };

        // Scripts, in the files' order
        foreach (var script in library.All.OrderBy(s => s.File, StringComparer.Ordinal).ThenBy(s => s.Line))
        {
            regions.TryGetValue(script.File, out var region);
            string? selfVoice = owners.TryGetValue(script.FullName, out var who) && who.Count == 1 ? who.First() : VoiceCast.Self;
            string? voice = selfVoice, speaker = null;
            bool none = false;
            foreach (var i in script.Everything())
            {
                switch (i.Op)
                {
                    case Op.Speaker:
                        none = false;
                        if (i.Name.Length > 0)
                        {
                            speaker = i.Name;
                            voice = cast.ForSpeaker(i.VoiceId, i.Name, script.FullName + "/" + i.Name);
                            if (voice == null)
                            {
                                voice = VoiceCast.Self;
                                if (i.VoiceId.Length > 0) result.UnknownIds.TryAdd(i.VoiceId, $"{script.File}.txt({i.Line})"); else result.Unlisted.TryAdd(i.Name, $"{script.File}.txt({i.Line})");
                            }
                        }
                        else if (i.Option) { speaker = null; voice = selfVoice; }
                        else { speaker = null; none = true; }
                        break;
                    case Op.Say when none:
                        result.Unvoiced["the game's own voice (speaker none)"] = result.Unvoiced.GetValueOrDefault("the game's own voice (speaker none)") + i.Lines.Count;
                        break;
                    case Op.Text:
                        result.Unvoiced["the game's own voice (text)"] = result.Unvoiced.GetValueOrDefault("the game's own voice (text)") + i.Lines.Count;
                        break;
                    case Op.Say:
                        for (int k = 0; k < i.Lines.Count; k++)
                            result.Lines.Add(Make(Text.ScriptLineKey(script.FullName, i.TextIndex, k), voice!, speaker, i.Lines[k], ValuesFor(region)));
                        break;
                    case Op.Ask or Op.Choose when none:
                        result.Unvoiced["the game's own voice (speaker none)"] = result.Unvoiced.GetValueOrDefault("the game's own voice (speaker none)") + 1;
                        break;
                    case Op.Ask or Op.Choose:
                        result.Lines.Add(Make(Text.ScriptLineKey(script.FullName, i.TextIndex, 0), voice!, speaker, i.Lines[0], ValuesFor(region)));
                        if (i.Op == Op.Choose) result.Unvoiced["a question's answers"] = result.Unvoiced.GetValueOrDefault("a question's answers") + i.Lines.Count - 1;
                        break;
                }
            }
        }

        // The people of the world and of the hand-made maps
        foreach (var p in persons)
        {
            string voice = voiceOf[p];
            string? region = regions.GetValueOrDefault(p.Place);
            string? speaker = p.Name.Length > 0 ? p.Name : p.TrainerName;
            if (string.Equals(p.Look, NPCPokemon, StringComparison.OrdinalIgnoreCase))
            {
                int cries = p.Dialog.Count + (p.Before != null ? 1 : 0) + (p.After != null ? 1 : 0);
                if (cries > 0) result.Unvoiced["a Pokémon's cry"] = result.Unvoiced.GetValueOrDefault("a Pokémon's cry") + cries;
                continue;
            }
            for (int k = 0; k < p.Dialog.Count; k++)
                result.Lines.Add(Make(Text.PersonLineKey(p.Kind, p.Place, p.Key, PersonField.Dialog, k), voice, speaker, p.Dialog[k], ValuesFor(region)));
            if (p.Before != null) result.Lines.Add(Make(Text.PersonLineKey(p.Kind, p.Place, p.Key, PersonField.Before, 0), voice, speaker, p.Before, ValuesFor(region)));
            if (p.After != null) result.Lines.Add(Make(Text.PersonLineKey(p.Kind, p.Place, p.Key, PersonField.After, 0), voice, speaker, p.After, ValuesFor(region)));
        }

        // The introduction: each region's professor
        foreach (var region in RegionDatabase.All)
        {
            if (region.Professor is not { Name.Length: > 0 } professor) continue;
            string? voice = cast.ForSpeaker("", professor.Name, "intro/" + region.Id);
            if (voice == null) { voice = VoiceCast.Self; result.Unlisted.TryAdd(professor.Name, $"regions/{region.Id}.json"); }
            foreach (var property in typeof(RegionIntroLines).GetProperties())
                if (property.GetValue(professor.Lines) is string { Length: > 0 } line)
                    result.Lines.Add(Make($"intro:{region.Id}.{property.Name}.0", voice, professor.Name, line, _ => null));
        }

        CountSigns(result);
        Carry(result, previous);
        return result;
    }

    private const string NPCPokemon = "Pokemon";

    private static VoiceLine Make(string key, string voice, string? speaker, string english, Func<string, IReadOnlyList<string>?> values)
    {
        var made = SpokenText.Make(english, values);
        var line = new VoiceLine
        {
            Key = key, Voice = voice, Speaker = speaker, En = english, Hash = HashOf(english),
            Spoken = made.Spoken, Status = made.Spoken == null ? VoiceLine.StatusShownOnly : VoiceLine.StatusSpoken
        };
        if (made.Variants.Count > 0) line.Variants = made.Variants.Select(v => new VoiceVariant { Key = v.Key, Spoken = v.Spoken }).ToList();
        return line;
    }

    /// <summary>Eight hexadecimal digits of the line's English (<see cref="VoiceCast.Hash32"/>).</summary>
    public static string HashOf(string english) => VoiceCast.Hash32(english).ToString("x8");

    private static IReadOnlyList<string>? StartersOf(string? region)
    {
        if (region != null && RegionDatabase.Get(region) is { } r) return r.Starters.Count > 0 ? r.Starters : null;
        var all = RegionDatabase.All.SelectMany(x => x.Starters).Distinct().ToList();
        return all.Count > 0 ? all : null;
    }

    /// <summary>The region each area and hand-made map belongs to, by the folder its files are in (a hand-made map is Sinnoh's unless a region's folder lists the name).</summary>
    private static Dictionary<string, string> RegionOfFiles()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        string root = GameDataFiles.PathOf(World.Folder);
        if (Directory.Exists(root))
            foreach (string region in Directory.GetDirectories(root).OrderBy(p => p, StringComparer.Ordinal))
                foreach (string kind in new[] { "areas", "overlays" })
                {
                    string folder = Path.Combine(region, kind);
                    if (!Directory.Exists(folder)) continue;
                    foreach (string path in Directory.GetFiles(folder, "*.json"))
                        map.TryAdd(Path.GetFileNameWithoutExtension(path), Path.GetFileName(region));
                }
        return map;
    }

    private static List<Person> PersonsOfTheGame(Catalogue result)
    {
        var persons = new List<Person>();
        string root = GameDataFiles.PathOf(World.Folder);
        if (Directory.Exists(root))
            foreach (string regionFolder in Directory.GetDirectories(root).OrderBy(p => p, StringComparer.Ordinal))
            {
                string region = Path.GetFileName(regionFolder);
                string overlays = Path.Combine(regionFolder, "overlays");
                if (!Directory.Exists(overlays)) continue;
                foreach (string path in Directory.GetFiles(overlays, "*.json").OrderBy(p => p, StringComparer.Ordinal))
                {
                    var overlay = JsonSerializer.Deserialize<WorldOverlayFile>(File.ReadAllText(path), GameDataFiles.Json);
                    if (overlay == null) continue;
                    string area = Path.GetFileNameWithoutExtension(path);
                    Dictionary<string, AreaObject>? objects = null;
                    AreaObject? ObjectOf(string id)
                    {
                        objects ??= LoadObjects(regionFolder, area);
                        return objects.GetValueOrDefault(id);
                    }

                    foreach (var (id, person) in overlay.People ?? new())
                    {
                        if (person.Dialog is not { Count: > 0 } && person.Trainer == null) continue;
                        var o = ObjectOf(id);
                        string? trainerClass = null, trainerName = null;
                        if (person.Trainer is { } lines)
                        {
                            var record = TrainerDatabase.Get(o?.Script, region) ?? TrainerDatabase.Get(lines.Id, region);
                            trainerClass = record?.Class; trainerName = record?.Name;
                        }
                        string? look = person.NpcType ?? (o != null ? WorldMapBuilder.CharacterFor(o.Looks) : null);
                        if (person.NpcType == null && o != null && WorldMapBuilder.SpeciesFor(o.Looks) != null && person.Trainer == null) look = NPCPokemon;
                        persons.Add(new Person("world", area, id, person.Name, look, trainerName, trainerClass, person.Script,
                            person.Dialog ?? new List<string>(), Blank(person.Trainer?.DialogueBefore), Blank(person.Trainer?.DialogueAfter)));
                    }
                    foreach (var n in overlay.Npcs ?? new())
                        persons.Add(PersonOfRecord("world", area, n, region));
                }
            }

        string maps = GameDataFiles.PathOf(MapDatabase.Folder);
        foreach (string path in Directory.GetFiles(maps, "*.json").OrderBy(p => p, StringComparer.Ordinal))
        {
            var map = JsonSerializer.Deserialize<MapFile>(File.ReadAllText(path), GameDataFiles.Json);
            if (map == null) continue;
            string name = Path.GetFileNameWithoutExtension(path);
            foreach (var n in map.Npcs) persons.Add(PersonOfRecord("map", name, n, null));
        }
        return persons.Where(p => p.Dialog.Count > 0 || p.Before != null || p.After != null).ToList();
    }

    private static Person PersonOfRecord(string kind, string place, MapFile.NpcRecord n, string? region)
    {
        string? trainerClass = null, trainerName = null;
        if (n.Trainer is { } t)
        {
            var record = TrainerDatabase.Get(t.Id, region);
            trainerClass = record?.Class ?? (t.TrainerClass is { Length: > 0 } and not "Trainer" ? t.TrainerClass : null);
            trainerName = record?.Name;
        }
        return new Person(kind, place, n.Id ?? $"{n.X},{n.Y}", n.Name, n.NpcType, trainerName, trainerClass, n.Script,
            n.Dialog ?? new List<string>(), Blank(n.Trainer?.DialogueBefore), Blank(n.Trainer?.DialogueAfter));
    }

    private static string? Blank(string? line) => string.IsNullOrEmpty(line) ? null : line;

    private static Dictionary<string, AreaObject> LoadObjects(string regionFolder, string area)
    {
        string path = Path.Combine(regionFolder, "areas", area + ".json");
        if (!File.Exists(path)) return new Dictionary<string, AreaObject>();
        var file = JsonSerializer.Deserialize<WorldAreaFile>(File.ReadAllText(path), GameDataFiles.Json);
        var objects = new Dictionary<string, AreaObject>(StringComparer.Ordinal);
        foreach (var o in file?.Objects ?? new()) objects.TryAdd(o.Id, o);
        return objects;
    }

    private static void CountSigns(Catalogue result)
    {
        int signs = 0;
        foreach (var place in TextSources.Overlays().Values.Concat(TextSources.Maps().Values)) signs += place.Signs.Count;
        result.Unvoiced["a sign"] = signs;
    }

    // ------------------------------------------------------------------ what a human wrote, and what went stale

    private static void Carry(Catalogue result, VoiceFile? previous)
    {
        if (previous == null) return;
        var old = previous.Lines.ToDictionary(l => l.Key, StringComparer.Ordinal);
        foreach (var line in result.Lines)
        {
            if (!old.TryGetValue(line.Key, out var was)) continue;
            line.Direction = was.Direction;
            line.Notes = was.Notes;
            if (was.Hash != line.Hash) continue;
            if (was.Manual == true)
            {
                line.Manual = true;
                line.Spoken = was.Spoken;
                line.Status = was.Status;
            }
            foreach (var v in was.Variants?.Where(v => v.Manual == true) ?? Enumerable.Empty<VoiceVariant>())
            {
                line.Variants ??= new List<VoiceVariant>();
                line.Variants.RemoveAll(x => x.Key == v.Key);
                line.Variants.Add(v);
            }
        }
    }

    /// <summary>What a rebuilt catalogue does to the one on disk: lines to voice, lines whose English changed (their audio is stale), lines recast, and lines that are gone.</summary>
    public sealed record Changes(IReadOnlyList<string> Added, IReadOnlyList<string> Stale, IReadOnlyList<string> Recast, IReadOnlyList<string> Orphaned)
    {
        public bool Any => Added.Count + Stale.Count + Recast.Count + Orphaned.Count > 0;
    }

    public static Changes Compare(VoiceFile? previous, Catalogue now)
    {
        if (previous == null) return new Changes(now.Lines.Select(l => l.Key).ToList(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>());
        var old = previous.Lines.ToDictionary(l => l.Key, StringComparer.Ordinal);
        var keys = now.Lines.Select(l => l.Key).ToHashSet(StringComparer.Ordinal);
        return new Changes(
            now.Lines.Where(l => !old.ContainsKey(l.Key)).Select(l => l.Key).ToList(),
            now.Lines.Where(l => old.TryGetValue(l.Key, out var w) && w.Hash != l.Hash).Select(l => l.Key).ToList(),
            now.Lines.Where(l => old.TryGetValue(l.Key, out var w) && w.Hash == l.Hash && w.Voice != l.Voice).Select(l => l.Key).ToList(),
            previous.Lines.Where(l => !keys.Contains(l.Key)).Select(l => l.Key).ToList());
    }

    // ------------------------------------------------------------------ the files

    private static readonly JsonSerializerOptions Compact = new(GameDataFiles.Json) { WriteIndented = false };

    /// <summary><c>lines.json</c> as it is written: a line to a row, so a rewritten line shows as one row in a diff.</summary>
    public static string Serialize(IEnumerable<VoiceLine> lines)
    {
        var sb = new StringBuilder("{\n  \"version\": 1,\n  \"lines\": [\n");
        bool first = true;
        foreach (var line in lines)
        {
            if (!first) sb.Append(",\n");
            first = false;
            sb.Append("    ").Append(JsonSerializer.Serialize(line, Compact));
        }
        sb.Append("\n  ]\n}\n");
        return sb.ToString();
    }

    public static VoiceFile Parse(string text) => JsonSerializer.Deserialize<VoiceFile>(text, GameDataFiles.Json) ?? new VoiceFile();

    // ------------------------------------------------------------------ the report

    /// <summary><c>docs/voice/coverage.md</c>: what is voiced, what is shown only, by voice and by kind of source, and what is unvoiced by decision.</summary>
    public static string Report(Catalogue c, CastFile castFile)
    {
        var cast = new VoiceCast(castFile);
        var sb = new StringBuilder();
        sb.AppendLine("# Voice coverage");
        sb.AppendLine();
        sb.AppendLine("Written by `dotnet run --project tools/VoiceTool -- catalogue` (plan 26 · V1); `VoiceTests` fails when this is not what the files give. Do not edit by hand.");
        sb.AppendLine();
        sb.AppendLine("A **line** is one string a character says in a text box (a script's `say` and the question of an `ask` or `choose`, a person's `dialog`, `before` and `after`, the introduction's professor), under the key plan 17 · N4 gave it. **Spoken** lines have a spoken text (the line with the names the player types left out, which `Data/voice/lines.json` holds); **shown only** lines have none yet, because a placeholder cannot be left out gracefully, and show their text alone until one is written by hand. No audio exists yet: V3 to V5 make it, V2 plays it.");
        sb.AppendLine();
        sb.AppendLine("## Lines by kind");
        sb.AppendLine();
        sb.AppendLine("| Kind | Lines | Spoken | Shown only | With variants | Written by hand |");
        sb.AppendLine("|---|---|---|---|---|---|");
        foreach (var group in c.Lines.GroupBy(l => l.Key[..l.Key.IndexOf(':')]).OrderBy(g => g.Key, StringComparer.Ordinal))
            sb.AppendLine($"| {KindName(group.Key)} | {group.Count()} | {group.Count(l => l.Status == VoiceLine.StatusSpoken)} | {group.Count(l => l.Status == VoiceLine.StatusShownOnly)} | {group.Count(l => l.Variants != null)} | {group.Count(l => l.Manual == true)} |");
        sb.AppendLine($"| **All** | {c.Lines.Count} | {c.Lines.Count(l => l.Status == VoiceLine.StatusSpoken)} | {c.Lines.Count(l => l.Status == VoiceLine.StatusShownOnly)} | {c.Lines.Count(l => l.Variants != null)} | {c.Lines.Count(l => l.Manual == true)} |");
        sb.AppendLine();
        sb.AppendLine("## Not voiced, by decision");
        sb.AppendLine();
        sb.AppendLine("Plan 26's decision 7: the game's own voice, a question's answers, signs and a Pokémon's cry.");
        sb.AppendLine();
        sb.AppendLine("| What | Lines |");
        sb.AppendLine("|---|---|");
        foreach (var (reason, n) in c.Unvoiced) sb.AppendLine($"| {reason} | {n} |");
        sb.AppendLine();
        sb.AppendLine("## The cast");
        sb.AppendLine();
        var named = castFile.Voices.Where(v => v.Value.Chooses == null).ToList();
        sb.AppendLine($"- Named characters: {castFile.Voices.Count} voices ({castFile.Voices.Count(v => v.Value.Provisional == true)} provisional: added for a name nobody has cast yet, with nothing said about how they sound).");
        sb.AppendLine($"- Cloned from the user's samples: {string.Join(", ", castFile.Voices.Where(v => v.Value.Clone != null).Select(v => "`" + v.Key + "`"))}.");
        var archetypes = c.Lines.Where(l => VoiceCast.IsArchetype(l.Voice)).Select(l => l.Voice[..l.Voice.IndexOf('#')]).Distinct().OrderBy(a => a, StringComparer.Ordinal).ToList();
        sb.AppendLine($"- Archetypes in use: {archetypes.Count}, with {archetypes.Sum(cast.VoicesOf)} voices to cast (V5).");
        sb.AppendLine($"- Lines whose voice is the person spoken to, decided as the talk starts (`@self`): {c.Lines.Count(l => l.Voice == VoiceCast.Self)}.");
        sb.AppendLine();
        sb.AppendLine("## Lines by voice");
        sb.AppendLine();
        sb.AppendLine("The voices with twenty lines or more; archetypes are summed over their voices.");
        sb.AppendLine();
        sb.AppendLine("| Voice | Lines | Shown only |");
        sb.AppendLine("|---|---|---|");
        var byVoice = c.Lines.GroupBy(l => VoiceCast.IsArchetype(l.Voice) ? l.Voice[..l.Voice.IndexOf('#')] + " (any)" : l.Voice)
            .Select(g => (Voice: g.Key, Lines: g.Count(), Shown: g.Count(l => l.Status == VoiceLine.StatusShownOnly)))
            .Where(g => g.Lines >= 20).OrderByDescending(g => g.Lines).ThenBy(g => g.Voice, StringComparer.Ordinal);
        foreach (var (voice, n, shown) in byVoice) sb.AppendLine($"| `{voice}` | {n} | {shown} |");
        sb.AppendLine();
        sb.AppendLine("## Casting problems");
        sb.AppendLine();
        if (c.Unlisted.Count == 0) sb.AppendLine("None: every speaker of every script has a cast entry.");
        else
        {
            sb.AppendLine("Speakers with no cast entry (add them to `Data/voice/cast.json`, or run `catalogue --add-missing` for a provisional voice each):");
            sb.AppendLine();
            foreach (var (name, where) in c.Unlisted) sb.AppendLine($"- `{name}`, first at {where}");
        }
        return sb.ToString().Replace("\r\n", "\n");
    }

    private static string KindName(string kind) => kind switch
    {
        "script" => "Scripts (`script:`)",
        "world" => "People of the world (`world:`)",
        "map" => "People of the hand-made maps (`map:`)",
        "intro" => "The introduction (`intro:`)",
        _ => kind
    };
}
