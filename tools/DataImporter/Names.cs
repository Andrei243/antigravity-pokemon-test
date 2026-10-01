using System.Globalization;
using System.Text.RegularExpressions;

namespace DataImporter;

/// <summary>Turns source spellings into the names the game uses.</summary>
public static partial class Names
{
    /// <summary>Curly apostrophes become straight ones (the game's font and existing data use <c>'</c>).</summary>
    public static string Clean(string name) => name.Replace('’', '\'').Replace('‘', '\'').Trim();

    /// <summary><c>bug-wings</c> → <c>Bug Wings</c>, with PokeAPI's identifiers that read differently in the games mapped.</summary>
    public static string Title(string identifier) => identifier switch
    {
        "water1" => "Water 1",
        "water2" => "Water 2",
        "water3" => "Water 3",
        "ground" => "Field",
        "plant" => "Grass",
        "humanshape" => "Human-Like",
        "indeterminate" => "Amorphous",
        "no-eggs" => "Undiscovered",
        _ => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(identifier.Replace('-', ' '))
    };

    /// <summary><c>BATTLE_EFFECT_RECOIL_THIRD</c> → <c>RecoilThird</c>.</summary>
    public static string Pascal(string constant, string prefix)
    {
        string body = constant.StartsWith(prefix) ? constant[prefix.Length..] : constant;
        return string.Concat(body.Split('_', '-', ' ').Where(p => p.Length > 0)
            .Select(p => char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()));
    }

    /// <summary>PokeAPI prose links words like <c>[HP]{mechanic:hp}</c>; keep the words (or the target's name when empty).</summary>
    public static string StripMarkup(string text)
    {
        text = Link().Replace(text, m => m.Groups[1].Value.Length > 0
            ? m.Groups[1].Value
            : Title(m.Groups[2].Value.Contains(':') ? m.Groups[2].Value[(m.Groups[2].Value.IndexOf(':') + 1)..] : m.Groups[2].Value));
        return Clean(Spaces().Replace(text.Replace('\n', ' '), " "));
    }

    [GeneratedRegex(@"\[([^\]]*)\]\{([^}]*)\}")]
    private static partial Regex Link();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex Spaces();
}
