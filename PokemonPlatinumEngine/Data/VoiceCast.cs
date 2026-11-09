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

    /// <summary>
    /// What a region says differently (plan 26, Kanto's session): the same display name or class is another person in another
    /// region (Kanto's rival, Kanto's mother), by region id in lower case (<c>kanto</c>). A region with no entry here is cast
    /// by the tables above alone, so no line of Sinnoh changes its voice.
    /// </summary>
    public SortedDictionary<string, CastRegion> Regions { get; set; } = new(StringComparer.Ordinal);
}

/// <summary>A region's own casting: its entries are looked at before the cast's own tables, for lines of that region's places and scripts.</summary>
public sealed class CastRegion
{
    /// <summary>A display name (a script's <c>speaker</c>, a person's name or their trainer's) to this region's voice id or archetype.</summary>
    public SortedDictionary<string, string> Names { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>A character look (<c>npcType</c>) to this region's voice id or archetype.</summary>
    public SortedDictionary<string, string> Looks { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>A trainer's class (<c>champion</c>, in lower case with underscores) to this region's voice id or archetype.</summary>
    public SortedDictionary<string, string> Classes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The voice of this region's rival: his class and his placeholder name (<c>{rival}</c>); the cast's <c>rival</c> where none.</summary>
    public string? Rival { get; set; }

    /// <summary>
    /// A trainer's own name picks no voice here, only their class does (unless <see cref="Names"/> says it): a route's Youngster Ben
    /// is a Youngster of this region, not the Ben some other region's cast gave a voice.
    /// </summary>
    public bool? TrainerNamesByClass { get; set; }
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

    /// <summary>How the voice should sound (plan 26 · V4's voice design), whatever engine makes it: the brief a designed voice is chosen or tuned to.</summary>
    public CastBrief? Brief { get; set; }

    /// <summary>
    /// The voice as the chosen engine makes it (plan 26 · V4, Kokoro-82M, approved 2026-11-07): a blend of the engine's own
    /// voices, a speed and a pitch, tuned to the <see cref="Brief"/>. Read by <c>tools/VoiceGen/kokoro_speak.py</c>.
    /// </summary>
    public CastDesign? Design { get; set; }
}

/// <summary>
/// A designed voice (plan 26 · V4): never a clone of anyone. <see cref="Blend"/> is a weighted mean of the engine's
/// built-in voices (<c>am_puck:0.7,am_echo:0.3</c>), <see cref="Speed"/> its pace, and <see cref="Pitch"/> a share the
/// take is raised by (it is made slower by the same share and played faster, which moves its pitch and its formants
/// together: a younger or an older sound of the same voice).
/// </summary>
public sealed class CastDesign
{
    public const string Kokoro = "kokoro-82m";

    public string Engine { get; set; } = Kokoro;
    public string Blend { get; set; } = "";
    public double Speed { get; set; } = 1.0;
    public double Pitch { get; set; } = 1.0;

    /// <summary>en-us or en-gb: the accent the words are read in (Kokoro's British voices want en-gb).</summary>
    public string Accent { get; set; } = "en-us";

    /// <summary>Kokoro-82M v1.0's English voices (<c>a</c> American, <c>b</c> British; <c>f</c> female, <c>m</c> male).</summary>
    public static IReadOnlyList<string> KokoroVoices { get; } = new[]
    {
        "af_alloy", "af_aoede", "af_bella", "af_heart", "af_jessica", "af_kore", "af_nicole", "af_nova", "af_river", "af_sarah", "af_sky",
        "am_adam", "am_echo", "am_eric", "am_fenrir", "am_liam", "am_michael", "am_onyx", "am_puck", "am_santa",
        "bf_alice", "bf_emma", "bf_isabella", "bf_lily", "bm_daniel", "bm_fable", "bm_george", "bm_lewis",
    };

    public static IReadOnlyList<string> Accents { get; } = new[] { "en-us", "en-gb" };

    /// <summary>The blend's voices and their weights, in the order written; empty where it is not well made.</summary>
    public IReadOnlyList<(string Voice, double Weight)> Parts()
    {
        var parts = new List<(string, double)>();
        foreach (string piece in Blend.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int colon = piece.IndexOf(':');
            string name = colon < 0 ? piece : piece[..colon];
            double weight = 1;
            if (colon >= 0 && !double.TryParse(piece[(colon + 1)..], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out weight))
                return Array.Empty<(string, double)>();
            parts.Add((name, weight));
        }
        return parts;
    }
}

/// <summary>
/// A voice's design brief (plan 26 · V4), in words any engine's settings can be chosen by: who it is, how high, how fast,
/// and the manner. Nothing in it names or imitates a real performer.
/// </summary>
public sealed class CastBrief
{
    /// <summary>man, woman, boy or girl (a voice's register, not who the character is).</summary>
    public string Register { get; set; } = "";

    /// <summary>child, teen, young, adult, middle-aged or elder.</summary>
    public string Age { get; set; } = "";

    /// <summary>low, middle or high, for the register.</summary>
    public string Pitch { get; set; } = "middle";

    /// <summary>slow, even or quick.</summary>
    public string Pace { get; set; } = "even";

    /// <summary>The manner in a few words: "warm, dry humour, careful with words".</summary>
    public string Manner { get; set; } = "";

    public static IReadOnlyList<string> Registers { get; } = new[] { "man", "woman", "boy", "girl" };
    public static IReadOnlyList<string> Ages { get; } = new[] { "child", "teen", "young", "adult", "middle-aged", "elder" };
    public static IReadOnlyList<string> Pitches { get; } = new[] { "low", "middle", "high" };
    public static IReadOnlyList<string> Paces { get; } = new[] { "slow", "even", "quick" };
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

    /// <summary>The id of the cast's rival (a designed voice since the user's ruling of 2026-11-07: no cloning, no recordings).</summary>
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
        if (RegionCast(seed, person: false) is { } region)
        {
            if (region.Names.TryGetValue(displayName, out var own)) return Settle(own, seed);
            if (region.Rival != null && displayName == "{rival}") return region.Rival;
        }
        return names.TryGetValue(displayName, out var target) ? Settle(target, seed) : null;
    }

    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, CastRegion?> regionOfPlace = new(StringComparer.Ordinal);

    /// <summary>
    /// The region's own casting for a seed, or null where the region has none (every line of Sinnoh, and of a place no region
    /// lists). A script's seed is <c>file.Name/speaker</c>, a person's <c>place.key</c>, an introduction's <c>intro/region</c>.
    /// </summary>
    private CastRegion? RegionCast(string seed, bool person)
    {
        if (File.Regions.Count == 0) return null;
        string place;
        if (seed.StartsWith("intro/", StringComparison.Ordinal)) place = seed;
        else
        {
            int slash = person ? -1 : seed.IndexOf('/');
            string head = slash < 0 ? seed : seed[..slash];
            int dot = head.IndexOf('.');
            place = dot < 0 ? head : head[..dot];
            // (a common script by its name, a battle line by its set)
            if (dot >= 0 && place is "common" or "battle") place = head;
        }
        string key = (person ? "p|" : "s|") + place;
        return regionOfPlace.GetOrAdd(key, _ =>
        {
            string? region = person
                ? place.StartsWith("battle.", StringComparison.Ordinal) ? VoiceCatalogue.RegionOf("battle:" + place[7..]) : VoiceCatalogue.RegionOf("world:" + place) ?? VoiceCatalogue.RegionOf("map:" + place)
                : place.StartsWith("intro/", StringComparison.Ordinal) ? place[6..].ToLowerInvariant() : VoiceCatalogue.RegionOf("script:" + place);
            return region != null && File.Regions.TryGetValue(region, out var cast) ? cast : null;
        });
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
        var region = RegionCast(seed, person: true);
        string? lookKey = look?.Trim();
        if (region != null)
        {
            if (!string.IsNullOrEmpty(lookKey) && region.Looks.TryGetValue(lookKey, out var regional)) return Settle(regional, seed);
            if (name.Length > 0 && region.Names.TryGetValue(name, out var regionName)) return Settle(regionName, seed);
        }
        if (!string.IsNullOrEmpty(lookKey) && looks.TryGetValue(lookKey, out var named) && !IsArchetype(named)) return named;
        // (where a region's trainers are cast by class, a trainer's name is no one else's voice, whether it is the person's or the trainer's)
        bool classOnly = region?.TrainerNamesByClass == true && !string.IsNullOrEmpty(trainerClass);
        if (!classOnly && name.Length > 0 && names.TryGetValue(name, out var byName) && !IsArchetype(byName)) return byName;
        if (rivalClass) return region?.Rival ?? Rival;
        if (region != null && !string.IsNullOrEmpty(trainerClass) && region.Classes.TryGetValue(trainerClass.ToLowerInvariant().Replace(' ', '_'), out var byClass)) return Settle(byClass, seed);
        if (!string.IsNullOrEmpty(trainerName))
        {
            if (region != null && region.Names.TryGetValue(trainerName, out var regionTrainer)) return Settle(regionTrainer, seed);
            if (!classOnly && names.TryGetValue(trainerName, out var byTrainer) && !IsArchetype(byTrainer)) return byTrainer;
        }
        if (!string.IsNullOrEmpty(trainerClass)) return Pick("class:" + trainerClass.ToLowerInvariant().Replace(' ', '_'), seed);
        if (!string.IsNullOrEmpty(lookKey))
            return looks.TryGetValue(lookKey, out var viaLook) ? Settle(viaLook, seed) : Pick("look:" + lookKey.ToLowerInvariant().Replace(' ', '_'), seed);
        if (name.Length > 0 && names.TryGetValue(name, out var generic)) return Settle(generic, seed);
        return Pick("look:person", seed);
    }
}
