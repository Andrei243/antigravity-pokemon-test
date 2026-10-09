using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using PokemonPlatinumEngine.Graphics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// What <c>characters.json</c> holds (plan 11 · C1): every look by its name, the names that are another look's
/// (<c>trainer</c> and <c>lucas</c> are the boy), the original's object looks that stand as a look of ours until they
/// have their own (<c>fallbacks</c>, and <c>fallback</c> for any other), and the look of each trainer class in battle.
/// </summary>
internal sealed class CharacterData
{
    public Dictionary<string, CharacterStyle> Looks { get; set; } = new();
    public Dictionary<string, string> Same { get; set; } = new();
    public Dictionary<string, string> Fallbacks { get; set; } = new();
    public string Fallback { get; set; } = "Trainer";

    /// <summary>A class's look: one name, several (the person's look tells which: the two Ace Trainers), or null for a class that names nobody in particular (a Leader: the person's own look).</summary>
    public Dictionary<string, JsonElement> Classes { get; set; } = new();
}

/// <summary>
/// The table of looks (plan 11 · C1): one name for a person in the field, in battle and in the files. A look is a
/// <see cref="CharacterStyle"/> written as values, so a new one is an entry of <c>Data/characters.json</c>, not a
/// case in a switch; the kit's parts stay code (<see cref="CharacterModels"/>). GPU-free. Names are matched without
/// regard to case: a map's <c>Lass</c> is the file's <c>lass</c>.
/// </summary>
internal sealed class CharacterStyles
{
    public const string FileName = "characters.json";

    /// <summary>The game's own table, read from the data folder the first time it is asked.</summary>
    public static CharacterStyles Default => table.Value;

    private static readonly Lazy<CharacterStyles> table = new(() => Parse(System.IO.File.ReadAllText(GameDataFiles.PathOf(FileName))));

    private readonly Dictionary<string, CharacterStyle> looks;
    private readonly Dictionary<string, string> same, fallbacks;
    private readonly Dictionary<string, string[]?> classes;
    private readonly string fallback;

    private CharacterStyles(CharacterData data)
    {
        looks = new Dictionary<string, CharacterStyle>(data.Looks, StringComparer.OrdinalIgnoreCase);
        same = new Dictionary<string, string>(data.Same, StringComparer.OrdinalIgnoreCase);
        fallbacks = new Dictionary<string, string>(data.Fallbacks, StringComparer.OrdinalIgnoreCase);
        fallback = data.Fallback;
        classes = new Dictionary<string, string[]?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in data.Classes)
        {
            classes[name] = value.ValueKind switch
            {
                JsonValueKind.Null => null,
                JsonValueKind.String => new[] { value.GetString()! },
                JsonValueKind.Array when value.GetArrayLength() > 0 && value.EnumerateArray().All(e => e.ValueKind == JsonValueKind.String)
                    => value.EnumerateArray().Select(e => e.GetString()!).ToArray(),
                _ => throw new JsonException($"The class '{name}' must name a look, several, or null.")
            };
        }
        foreach (var (name, of) in same)
            if (!looks.ContainsKey(of)) throw new JsonException($"'{name}' is the same as '{of}', which is no look.");
    }

    /// <summary>Reads a table written as <c>characters.json</c> is: an unknown field of a look is an error, not a look silently left plain.</summary>
    public static CharacterStyles Parse(string json)
    {
        var options = new JsonSerializerOptions(GameDataFiles.Json)
        {
            IncludeFields = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        };
        options.Converters.Add(new HexColor());
        return new CharacterStyles(JsonSerializer.Deserialize<CharacterData>(json, options)
            ?? throw new JsonException("The table of looks is empty."));
    }

    /// <summary>Every look's own name (not the names that are another's).</summary>
    public IEnumerable<string> Names => looks.Keys;

    /// <summary>The names that are another look's, with whose.</summary>
    public IReadOnlyDictionary<string, string> Aliases => same;

    /// <summary>The original's looks that stand as one of ours for now, with whose.</summary>
    public IReadOnlyDictionary<string, string> Fallbacks => fallbacks;

    /// <summary>The trainer classes the table names, with their looks (null: the person's own).</summary>
    public IReadOnlyDictionary<string, string[]?> Classes => classes;

    /// <summary>True for a look of the table, or a name that is one's.</summary>
    public bool Has(string name) => looks.ContainsKey(name) || same.ContainsKey(name);

    /// <summary>
    /// The style of a look, a copy to change as the caller likes; the plain default (a green shirt, short brown hair,
    /// a child's build) for a name the table doesn't have.
    /// </summary>
    public CharacterStyle Get(string name) =>
        (looks.TryGetValue(name, out var style) || (same.TryGetValue(name, out var of) && looks.TryGetValue(of, out style))
            ? style : new CharacterStyle()).Clone();

    /// <summary>
    /// The look a person of the world stands as, by the name of the original's object looks: their own when the table
    /// has it, else the one the <c>fallbacks</c> block gives, else the <c>fallback</c> (the boy).
    /// </summary>
    public string CharacterFor(string looksName) =>
        Has(looksName) ? looksName : fallbacks.TryGetValue(looksName, out var instead) ? instead : fallback;

    /// <summary>
    /// The look a trainer class is drawn as in battle, where the original shows the class whatever the person in the
    /// field wears: the class's look; of a class of two looks, the one the person (<paramref name="look"/>) wears, or
    /// else the first; null for a class that names nobody in particular ("Leader", "Elite Four", "Commander"), whose
    /// person's own look is drawn. A class the table doesn't name yet is drawn under its own name, which is no look:
    /// the plain default, as before the table.
    /// </summary>
    public string? OfClass(string trainerClass, string? look)
    {
        if (!classes.TryGetValue(trainerClass, out var named)) return trainerClass;
        if (named == null) return null;
        return named.FirstOrDefault(n => look != null && n.Equals(look, StringComparison.OrdinalIgnoreCase)) ?? named[0];
    }

    /// <summary>
    /// The look a trainer stands on the battle's platform in: <see cref="OfClass"/>, the person's own
    /// (<paramref name="look"/>) where the class names nobody in particular, and the class's name, which is the plain
    /// default, where there is no person either (a trainer a test or the harness makes).
    /// </summary>
    public string InBattle(string trainerClass, string? look) => OfClass(trainerClass, look) ?? look ?? trainerClass;

    /// <summary>A colour written <c>#rrggbb</c> (or <c>#rrggbbaa</c>).</summary>
    private sealed class HexColor : JsonConverter<Color>
    {
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string text = reader.GetString() ?? "";
            string h = text.TrimStart('#');
            if (h.Length is not (6 or 8) || !uint.TryParse(h, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint v))
                throw new JsonException($"'{text}' is not a colour written #rrggbb");
            if (h.Length == 6) v = (v << 8) | 0xff;
            return new Color((byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v);
        }

        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.A == 255 ? $"#{value.R:x2}{value.G:x2}{value.B:x2}" : $"#{value.R:x2}{value.G:x2}{value.B:x2}{value.A:x2}");
    }
}
