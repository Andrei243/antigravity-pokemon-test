using System.Globalization;
using System.Text;
using System.Text.Json;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The string table (plan 17 · N1): every word the interface says, asked for by key in the language the options
/// chose. <c>Text.Of("options.textSpeed.label")</c> gives the language's string, English's where the language has
/// none, and the key in brackets where English has none either (<c>TextTests</c> keeps that from shipping).
/// Arguments are named, so a translation may put them in another order: <c>Text.Of("title.slotDamaged", ("slot", 2))</c>
/// with <c>"Slot {slot} can't be read"</c>; a brace the call names no argument for is left as it is, for the
/// placeholders filled later (<c>{player}</c>). An entry may have forms: <c>one</c> and <c>other</c>, chosen by the
/// argument <c>n</c> under the language's plural rule, or <c>boy</c> and <c>girl</c>, chosen by the player's look.
/// The English is <c>Data/text/en/ui.json</c>, sections by screen; the code holds keys only.
/// Like <see cref="Data.Ruleset"/>, the language is the whole process's (<see cref="Use"/>, which the engine calls
/// from <c>ApplySettings</c>): a test that runs under another runs alone. The pseudo-language (<see cref="Pseudo"/>)
/// has no file: it is English with every string in brackets, so whatever slipped past the table shows bare.
/// GPU-free.
/// </summary>
public static class Text
{
    public const string English = "en";

    /// <summary>The pseudo-language's tag: English, every string in brackets.</summary>
    public const string Pseudo = "xx";

    private static readonly string[] FormNames = { "zero", "one", "two", "few", "many", "other", "boy", "girl" };

    private static readonly object gate = new();
    private static Dictionary<string, Entry>? english;
    private static Dictionary<string, Entry>? chosen;
    private static Language? language;

    /// <summary>The language strings are given in.</summary>
    public static Language Language
    {
        get { lock (gate) return language ??= Core.Language.Of(English) ?? new Language(); }
    }

    /// <summary>The tag of the language strings are given in.</summary>
    public static string Current => Language.Tag;

    /// <summary>
    /// Gives every later string in the language of <paramref name="tag"/>: its <c>ui.json</c> over English's. A tag
    /// the list doesn't have, or none, is English.
    /// </summary>
    public static void Use(string? tag)
    {
        var next = Core.Language.Of(tag) ?? Core.Language.Of(English) ?? new Language();
        lock (gate)
        {
            if (language == next && chosen != null) return;
            language = next;
            chosen = next.Tag == English || next.Pseudo ? null : Read(next.Tag);
        }
    }

    /// <summary>The string of <paramref name="key"/> in the chosen language, its named arguments put in.</summary>
    public static string Of(string key, params (string Name, object? Value)[] args)
    {
        Entry? entry;
        Language lang;
        lock (gate)
        {
            lang = language ??= Core.Language.Of(English) ?? new Language();
            english ??= Read(English);
            entry = chosen != null && chosen.TryGetValue(key, out var own) ? own
                : english.TryGetValue(key, out var en) ? en : null;
        }
        string text = entry == null ? $"[{key}]" : Fill(entry.Pick(lang, args), args);
        return lang.Pseudo ? $"[{text}]" : text;
    }

    /// <summary>Whether English has <paramref name="key"/>.</summary>
    public static bool Has(string key)
    {
        lock (gate) return (english ??= Read(English)).ContainsKey(key);
    }

    /// <summary>Every key of English's table.</summary>
    public static IReadOnlySet<string> EnglishKeys
    {
        get { lock (gate) return (english ??= Read(English)).Keys.ToHashSet(); }
    }

    /// <summary>A whole number as the language writes it.</summary>
    public static string Number(long n) => n.ToString(CultureInfo.InvariantCulture);

    /// <summary>A number with <paramref name="digits"/> places after the language's own decimal mark.</summary>
    public static string Decimal(double value, int digits) =>
        value.ToString("F" + digits, CultureInfo.InvariantCulture).Replace(".", Language.Decimal);

    // ------------------------------------------------------------------ the table

    private sealed class Entry
    {
        public string? Plain;
        public Dictionary<string, string>? Forms;

        public string Pick(Language lang, (string Name, object? Value)[] args)
        {
            if (Forms == null) return Plain ?? "";
            string form = "other";
            if (Forms.ContainsKey("boy") || Forms.ContainsKey("girl"))
                form = FormOfLook(PlayerIdentity.Look);
            else
            {
                foreach (var (name, value) in args)
                    if (name == "n" && value is IConvertible number) form = lang.FormFor(number.ToInt64(CultureInfo.InvariantCulture));
            }
            return Forms.TryGetValue(form, out var text) ? text : Forms.TryGetValue("other", out var other) ? other : Forms.Values.First();
        }
    }

    /// <summary>The form of an entry of the boy and the girl that goes with a look.</summary>
    public static string FormOfLook(PlayerLook look) => look == PlayerLook.Girl ? "girl" : "boy";

    /// <summary>A string with its named arguments put in; a brace no argument is named for is left as it is.</summary>
    public static string Fill(string text, params (string Name, object? Value)[] args)
    {
        if (args.Length == 0 || text.IndexOf('{') < 0) return text;
        var built = new StringBuilder(text);
        foreach (var (name, value) in args)
            built.Replace("{" + name + "}", value switch
            {
                null => "",
                int or long or short or byte => Number(Convert.ToInt64(value, CultureInfo.InvariantCulture)),
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()
            });
        return built.ToString();
    }

    /// <summary>A language's <c>ui.json</c>, its sections flattened into dotted keys; nothing for a language with no file.</summary>
    private static Dictionary<string, Entry> Read(string tag)
    {
        var table = new Dictionary<string, Entry>(StringComparer.Ordinal);
        string path = GameDataFiles.PathOf(Path.Combine("text", tag, "ui.json"));
        if (!File.Exists(path)) return table;
        using var document = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        Walk(document.RootElement, "", table, path);
        return table;
    }

    private static void Walk(JsonElement element, string prefix, Dictionary<string, Entry> table, string path)
    {
        foreach (var property in element.EnumerateObject())
        {
            string key = prefix.Length == 0 ? property.Name : prefix + "." + property.Name;
            var value = property.Value;
            if (value.ValueKind == JsonValueKind.String) table[key] = new Entry { Plain = value.GetString() };
            else if (value.ValueKind == JsonValueKind.Object && IsForms(value))
                table[key] = new Entry { Forms = value.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString() ?? "") };
            else if (value.ValueKind == JsonValueKind.Object) Walk(value, key, table, path);
            else throw new InvalidDataException($"{path}: {key} is neither a string, its forms nor a section.");
        }
    }

    private static bool IsForms(JsonElement value)
    {
        bool any = false;
        foreach (var p in value.EnumerateObject())
        {
            if (p.Value.ValueKind != JsonValueKind.String || Array.IndexOf(FormNames, p.Name) < 0) return false;
            any = true;
        }
        return any;
    }
}
