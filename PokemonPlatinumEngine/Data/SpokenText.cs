using System.Text.RegularExpressions;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// What a line is said as (plan 26 · V1): the line as the text box shows it, with a free placeholder never spoken.
/// A name the player typed (<c>{player}</c>, <c>{rival}</c>) is dropped with the comma round it ("Hey, {player}! Over
/// here!" is said "Hey! Over here!"); a placeholder with few values (<c>{starter}</c>, <c>{assistant}</c>) stays in the
/// spoken text and is filled once for each value, as a variant; a line that cannot lose a placeholder gracefully has no
/// spoken text and is shown only until someone writes one. Markup (plan 09 · L3's <c>{pause 0.4}</c>, a colour round
/// words) is taken out. GPU-free.
/// </summary>
public static class SpokenText
{
    /// <summary>The result for one line: the spoken text (null when there is none), what it was made from and its variants.</summary>
    public sealed record Made(string? Spoken, IReadOnlyList<string> Placeholders, IReadOnlyList<(string Key, string Spoken)> Variants);

    private static readonly Regex Braces = new(@"\{([^{}]*)\}", RegexOptions.Compiled);
    private static readonly Regex Placeholder = new(@"\{([A-Za-z_]+)(?::[^{}]*)?\}", RegexOptions.Compiled);
    // The text box's markup (plan 09 · L3, UI/Kit/RichText.cs): {red:..} {blue:..} {name:..} wrap words; {pause S}, {speed N}, {sfx X} and {br} are not said; {money N} says its number
    private static readonly HashSet<string> Colours = new(StringComparer.Ordinal) { "red", "blue", "name" };
    private static readonly HashSet<string> Codes = new(StringComparer.Ordinal) { "pause", "speed", "sfx", "money" };

    /// <summary>The placeholders of a line (markup is not one), each as written: <c>{player}</c>, <c>{var:VAR_X}</c>, once each in order.</summary>
    public static IReadOnlyList<string> PlaceholdersOf(string line)
    {
        var found = new List<string>();
        foreach (Match m in Braces.Matches(line))
        {
            if (IsMarkup(m.Groups[1].Value)) continue;
            if (!found.Contains(m.Value)) found.Add(m.Value);
        }
        return found;
    }

    private static bool IsMarkup(string inner)
    {
        if (inner == "br") return true;
        int colon = inner.IndexOf(':'), space = inner.IndexOf(' ');
        if (colon > 0) return Colours.Contains(inner[..colon]);
        return space > 0 && Codes.Contains(inner[..space]);
    }

    /// <summary>Takes the markup out: a pause goes, a colour or effect leaves the words it wraps.</summary>
    public static string WithoutMarkup(string line) =>
        Braces.Replace(line, m =>
        {
            string inner = m.Groups[1].Value;
            if (!IsMarkup(inner)) return m.Value;
            if (inner == "br") return " ";
            int colon = inner.IndexOf(':');
            if (colon > 0) return inner[(colon + 1)..];
            int space = inner.IndexOf(' ');
            return inner[..space] == "money" ? inner[(space + 1)..] : "";
        });

    /// <summary>
    /// Makes the spoken text of a line. <paramref name="values"/> says which placeholders have few values and what they
    /// are (by the placeholder's name, <c>starter</c>); a placeholder it returns null for is free.
    /// </summary>
    public static Made Make(string english, Func<string, IReadOnlyList<string>?> values)
    {
        var placeholders = PlaceholdersOf(english);
        string text = WithoutMarkup(english);
        var enumerated = new List<(string Name, string Token, IReadOnlyList<string> Values)>();
        foreach (string token in placeholders)
        {
            string name = Placeholder.Match(token).Groups[1].Value;
            if (token.Contains(':')) continue;
            if (values(name) is { Count: > 0 } list) enumerated.Add((name, token, list));
        }

        foreach (string token in placeholders)
        {
            if (enumerated.Any(e => e.Token == token)) continue;
            string? dropped = Drop(text, token);
            if (dropped == null) return new Made(null, placeholders, Array.Empty<(string, string)>());
            text = dropped;
        }
        if (text != english) text = Tidy(text);
        if (!text.Any(char.IsLetterOrDigit)) return new Made(null, placeholders, Array.Empty<(string, string)>());

        var variants = new List<(string Key, string Spoken)>();
        if (enumerated.Count > 0)
        {
            var rows = new List<(string Key, string Spoken)> { ("", text) };
            foreach (var (name, token, list) in enumerated)
                rows = rows.SelectMany(row => list.Select(value => (row.Key + $"~{name}={value}", row.Spoken.Replace(token, value)))).ToList();
            variants = rows;
        }
        return new Made(text, placeholders, variants);
    }

    /// <summary>
    /// The line without a name in it, or null where the name cannot go gracefully: after a comma at the end of a
    /// sentence or between commas, or opening the line or a sentence with its own comma or mark after it.
    /// </summary>
    public static string? Drop(string line, string token)
    {
        string t = Regex.Escape(token);
        string s = line;
        s = Regex.Replace(s, @",\s*" + t + @"(?=\s*,)", "");
        s = Regex.Replace(s, @",\s*" + t + @"(?=\s*[.!?…]|\s*$)", "");
        s = Regex.Replace(s, @"^(\s*(?:\.\.\.|…)?\s*)" + t + @"\s*[,!.?…:]+\s*", m => m.Groups[1].Value);
        s = Regex.Replace(s, @"([.!?…]\s+)" + t + @"\s*[,!?]\s*", m => m.Groups[1].Value);
        return s.Contains(token) ? null : s;
    }

    private static string Tidy(string s)
    {
        s = Regex.Replace(s, @"\s+", " ").Trim();
        s = Regex.Replace(s, @"\s+([,.!?…;:])", "$1");
        // A sentence that began with the name now begins with its next word
        s = Regex.Replace(s, @"(^|[.!?…]\s)([a-z])", m => m.Groups[1].Value + char.ToUpperInvariant(m.Groups[2].Value[0]));
        return s;
    }
}
