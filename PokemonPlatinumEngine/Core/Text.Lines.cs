using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Core;

/// <summary>Which of a person's lines a translation is asked for.</summary>
public enum PersonField { Dialog, Before, After }

/// <summary>
/// Lines of the game's own files, translated as they are shown (plan 17 · N4). The scripts, the overlays and the
/// hand-made rooms keep their English; a language's sibling files (<see cref="TranslationFiles"/>) hold the same
/// lines in order, each entry carrying the English it translates, and every lookup here gives the translation where
/// there is one that is not stale, and the English line itself otherwise. The placeholders and the markup of a line
/// pass through untouched: <c>{player}</c> is filled after the lookup, so a translation uses the English line's own.
/// Under the pseudo-language every line comes back in brackets. GPU-free.
/// </summary>
public static partial class Text
{
    /// <summary>
    /// Line <paramref name="k"/> of the text-bearing instruction numbered <paramref name="index"/> in a script
    /// (<see cref="Story.Instruction.TextIndex"/>), the script by its full name (<c>file.Name</c>);
    /// <paramref name="english"/> is every line of that instruction as the script has them.
    /// </summary>
    public static string Line(string script, int index, int k, IReadOnlyList<string> english)
    {
        string source = english[k];
        var lang = Language;
        if (lang.Pseudo) return $"[{source}]";
        if (lang.Tag == English) return source;
        int dot = script.IndexOf('.');
        if (dot < 0) return source;
        var scripts = TranslationFiles.Scripts(lang.Tag, script[..dot]);
        if (scripts == null || !scripts.TryGetValue(script[(dot + 1)..], out var entries) || index < 0 || index >= entries.Count) return source;
        return Pick(entries[index], k, english) ?? source;
    }

    /// <summary>A person's name as the chosen language writes it; <paramref name="place"/> and <paramref name="key"/> are where their lines are found (<see cref="Overworld.NPC.LinesPlace"/>).</summary>
    public static string PersonName(string? place, string? key, string english)
    {
        var lang = Language;
        if (lang.Pseudo) return english.Length == 0 ? english : $"[{english}]";
        if (lang.Tag == English || english.Length == 0) return english;
        return Pick(PersonOf(lang.Tag, place, key)?.Name, 0, new[] { english }) ?? english;
    }

    /// <summary>Line <paramref name="k"/> of what a person says, or of a trainer's words before or after a battle.</summary>
    public static string PersonLine(string? place, string? key, PersonField field, int k, IReadOnlyList<string> english)
    {
        string source = english[k];
        var lang = Language;
        if (lang.Pseudo) return $"[{source}]";
        if (lang.Tag == English) return source;
        var person = PersonOf(lang.Tag, place, key);
        var entry = field switch { PersonField.Before => person?.Before, PersonField.After => person?.After, _ => person?.Dialog };
        return Pick(entry, k, english) ?? source;
    }

    /// <summary>Line <paramref name="k"/> of a signboard, by its id in the area's file (or <c>x,y</c> on a hand-made map).</summary>
    public static string SignLine(string? place, string? id, int k, IReadOnlyList<string> english)
    {
        string source = english[k];
        var lang = Language;
        if (lang.Pseudo) return $"[{source}]";
        if (lang.Tag == English || place == null || id == null) return source;
        var signs = TranslationFiles.Place(lang.Tag, place)?.Signs;
        return signs != null && signs.TryGetValue(id, out var entry) ? Pick(entry, k, english) ?? source : source;
    }

    /// <summary>
    /// What a language calls a room (a hand-made map's <c>displayName</c>, or an area overlay's <c>name</c>), or null
    /// where it has no name of its own for it, so the caller falls back to the names of places PokeAPI has.
    /// </summary>
    public static string? PlaceName(string? place, string english)
    {
        var lang = Language;
        if (lang.Pseudo || lang.Tag == English || place == null || english.Length == 0) return null;
        return Pick(TranslationFiles.Place(lang.Tag, place)?.DisplayName, 0, new[] { english });
    }

    /// <summary>
    /// The address of the k-th string of a script's text-bearing instruction, which plan 26's voice catalogue and these
    /// files share: <c>script:twinleaf_town.RivalRunsOut#2.0</c>. The numbering is <see cref="Story.Instruction.TextIndex"/>.
    /// </summary>
    public static string ScriptLineKey(string script, int index, int k) => $"script:{script}#{index}.{k}";

    /// <summary>The address of the k-th line of a person: <c>world:twinleaf_town.mom.dialog.1</c>, <c>map:PlayerHouse.12,3.before.0</c>.</summary>
    public static string PersonLineKey(string kind, string place, string key, PersonField field, int k) =>
        $"{kind}:{place}.{key}.{field.ToString().ToLowerInvariant()}.{k}";

    private static PersonTexts? PersonOf(string tag, string? place, string? key)
    {
        if (place == null || key == null) return null;
        var texts = TranslationFiles.Place(tag, place);
        return texts != null && texts.People.TryGetValue(key, out var person) ? person : null;
    }

    private static string? Pick(LineEntry? entry, int k, IReadOnlyList<string> english) =>
        entry == null || entry.IsStale(english) ? null : entry.Translation(k);
}
