using System.Text;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// The cast of plan 26's voices (<c>Data/voice/cast.json</c>; V1): who speaks each line. A named character has a voice
/// of their own, found by the display name a script's <c>speaker</c> gives (or by an id the script writes before the
/// name, <c>speaker rowan "Prof. Rowan"</c>); everyone else belongs to an <b>archetype</b> (<c>class:lass</c> for a
/// trainer's class, <c>look:gentleman</c> for the character that plays them) with a few voices, and is given one of them
/// by a hash of their place and key, the same on every run. Nothing here synthesises or plays anything. GPU-free.
/// </summary>
public sealed class CastFile
{
    /// <summary>How many voices an archetype has unless <see cref="Archetypes"/> says otherwise.</summary>
    public int ArchetypeVoices { get; set; } = 3;

    /// <summary>The voices of named characters, by id.</summary>
    public SortedDictionary<string, CastVoice> Voices { get; set; } = new(StringComparer.Ordinal);

    /// <summary>A display name (what a script's <c>speaker</c> says, or a person's name) to a voice id, or to an archetype (<c>look:man</c>).</summary>
    public SortedDictionary<string, string> Names { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>A character look (<c>npcType</c>) that stands for a named character, to the voice id, or an archetype to use for it.</summary>
    public SortedDictionary<string, string> Looks { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>A person of the world (<c>place.key</c>, as the line keys write it) to a voice id or an archetype, where neither their name nor their class says it.</summary>
    public SortedDictionary<string, string> People { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Archetypes that need a count of voices or a note; any other archetype has <see cref="ArchetypeVoices"/>.</summary>
    public SortedDictionary<string, CastArchetype> Archetypes { get; set; } = new(StringComparer.Ordinal);
}

public sealed class CastVoice
{
    public string? Note { get; set; }

    /// <summary>For a voice cloned from the user's samples (plan 26 · V3): the folder of the samples.</summary>
    public string? Clone { get; set; }

    /// <summary>For a voice that is one of several by the game's state (the assistant, by the player's look): the ids it is chosen from at run time.</summary>
    public List<string>? Chooses { get; set; }

    /// <summary>A voice added by <c>VoiceTool catalogue --add-missing</c> for a name nobody has cast yet: it has its own id, and nothing about how it should sound.</summary>
    public bool? Provisional { get; set; }
}

public sealed class CastArchetype
{
    public int? Voices { get; set; }
    public string? Note { get; set; }
}

public sealed class VoiceCast
{
    private static readonly Lazy<VoiceCast> loaded = new(() => new VoiceCast(GameDataFiles.Load<CastFile>(Path.Combine("voice", "cast.json"))));

    /// <summary>The game's own cast, read once.</summary>
    public static VoiceCast Default => loaded.Value;

    /// <summary>The id of the cast's rival, who is cloned from the user's samples.</summary>
    public const string Rival = "rival";

    /// <summary>The voice of lines whose speaker is the person spoken to, decided as the talk starts (plan 26 · V2).</summary>
    public const string Self = "@self";

    public CastFile File { get; }

    // Names and looks are matched without regard to case ("lass" and "Lass"); a deserialised sorted dictionary has lost its comparer
    private readonly Dictionary<string, string> names = new(StringComparer.OrdinalIgnoreCase), looks = new(StringComparer.OrdinalIgnoreCase);

    public VoiceCast(CastFile file)
    {
        File = file;
        foreach (var (name, target) in file.Names) names[name] = target;
        foreach (var (look, target) in file.Looks) looks[look] = target;
    }

    public bool HasVoice(string id)
    {
        if (File.Voices.ContainsKey(id)) return true;
        int hash = id.IndexOf('#');
        string archetype = hash < 0 ? id : id[..hash];
        return archetype.Contains(':') && (hash < 0 || (int.TryParse(id[(hash + 1)..], out int n) && n >= 1 && n <= VoicesOf(archetype)));
    }

    public int VoicesOf(string archetype) => File.Archetypes.TryGetValue(archetype, out var a) && a.Voices is { } n ? n : File.ArchetypeVoices;

    /// <summary>Every voice id the cast has: the named ones and each archetype's, for the archetypes <paramref name="used"/> names.</summary>
    public IEnumerable<string> ArchetypeVoiceIds(string archetype) =>
        Enumerable.Range(1, VoicesOf(archetype)).Select(n => $"{archetype}#{n}");

    public static bool IsArchetype(string target) => target.Contains(':');

    /// <summary>A name as a cast table keeps it: its letters and digits in lower case, joined by underscores (<c>Prof. Rowan</c> is <c>prof_rowan</c>).</summary>
    public static string Slug(string name)
    {
        var sb = new StringBuilder();
        foreach (char c in name.Normalize(NormalizationForm.FormD))
        {
            if (char.IsAsciiLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
            else if ((c is ' ' or '-' or '_' or '.') && sb.Length > 0 && sb[^1] != '_') sb.Append('_');
        }
        while (sb.Length > 0 && sb[^1] == '_') sb.Length--;
        return sb.Length == 0 ? "unknown" : sb.ToString();
    }

    /// <summary>FNV-1a of 32 bits over the text's UTF-8 (a string's <c>GetHashCode</c> changes from run to run and is a build error).</summary>
    public static uint Hash32(string text)
    {
        uint hash = 2166136261;
        foreach (byte b in Encoding.UTF8.GetBytes(text)) hash = (hash ^ b) * 16777619;
        return hash;
    }

    /// <summary>The voice of an archetype for whoever the seed names: <c>look:lass#2</c>.</summary>
    public string Pick(string archetype, string seed) => $"{archetype}#{Hash32(seed) % (uint)VoicesOf(archetype) + 1}";

    private string Settle(string target, string seed) => IsArchetype(target) ? Pick(target, seed) : target;

    /// <summary>
    /// The voice of a script's speaker: the id the script gave (null when it names an id the cast lacks), else the cast's
    /// entry for the display name, else null. <paramref name="seed"/> picks among an archetype's voices.
    /// </summary>
    public string? ForSpeaker(string voiceId, string displayName, string seed)
    {
        if (voiceId.Length > 0) return HasVoice(voiceId) ? voiceId : null;
        return names.TryGetValue(displayName, out var target) ? Settle(target, seed) : null;
    }

    /// <summary>
    /// The voice of a person of the world (never null: someone with no cast of their own falls to the archetype of
    /// their class or their look). In order: the cast's entry for the person, a look that stands for a named character,
    /// a name that does, the rival's class, the trainer's own name, the class, the look, a generic name, and last
    /// <c>look:person</c>.
    /// </summary>
    public string ForPerson(string place, string key, string name, string? trainerName, string? trainerClass, string? look, bool rivalClass)
    {
        string seed = place + "." + key;
        if (File.People.TryGetValue(seed, out var own)) return Settle(own, seed);
        string? lookKey = look?.Trim();
        if (!string.IsNullOrEmpty(lookKey) && looks.TryGetValue(lookKey, out var named) && !IsArchetype(named)) return named;
        if (name.Length > 0 && names.TryGetValue(name, out var byName) && !IsArchetype(byName)) return byName;
        if (rivalClass) return Rival;
        if (!string.IsNullOrEmpty(trainerName) && names.TryGetValue(trainerName, out var byTrainer) && !IsArchetype(byTrainer)) return byTrainer;
        if (!string.IsNullOrEmpty(trainerClass)) return Pick("class:" + trainerClass.ToLowerInvariant().Replace(' ', '_'), seed);
        if (!string.IsNullOrEmpty(lookKey))
            return looks.TryGetValue(lookKey, out var viaLook) ? Settle(viaLook, seed) : Pick("look:" + lookKey.ToLowerInvariant().Replace(' ', '_'), seed);
        if (name.Length > 0 && names.TryGetValue(name, out var generic)) return Settle(generic, seed);
        return Pick("look:person", seed);
    }
}
