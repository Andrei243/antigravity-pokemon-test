using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

/// <summary>
/// A voice pack in another language than English (<c>generate --language ro</c>): each job's text is the line as the
/// language's translation files have it (plan 17 · N4), found by the line's key, which the catalogue and those files
/// share, and made spoken as the English is (<see cref="SpokenText.Make"/>: a typed name dropped, the markup taken out).
/// A line the language has no current translation of is left out, so a pack never holds English said in the language's
/// voice. The take is still stamped with the English it stands for, so it goes stale with the English as an English take does.
/// </summary>
public static class Translated
{
    /// <summary>
    /// The jobs whose lines the language has, each with its text in the language; the rest are dropped and counted.
    /// </summary>
    public static (VoiceJobFile Jobs, int Dropped) Apply(VoiceJobFile jobs, string language)
    {
        var kept = new List<VoiceJob>();
        int dropped = 0;
        foreach (var job in jobs.Jobs)
        {
            int tilde = job.Key.IndexOf('~');
            string key = tilde < 0 ? job.Key : job.Key[..tilde];
            string? spoken = LineOf(language, key, job.English) is { } line ? SpokenOf(line, tilde < 0 ? "" : job.Key[tilde..]) : null;
            if (spoken == null) { dropped++; continue; }
            job.Text = spoken;
            job.Respelled = null; // the lexicon's respellings are English sounds
            kept.Add(job);
        }
        jobs.Jobs = kept;
        jobs.Language = language;
        return (jobs, dropped);
    }

    /// <summary>The language's line for a key of the catalogue, or null where it has none that translates <paramref name="english"/>.</summary>
    public static string? LineOf(string language, string key, string english)
    {
        int colon = key.IndexOf(':');
        if (colon < 0) return null;
        string kind = key[..colon], rest = key[(colon + 1)..];
        if (kind == "script")
        {
            // script:file.Name#index.k
            int hash = rest.LastIndexOf('#');
            if (hash < 0) return null;
            string script = rest[..hash];
            string[] at = rest[(hash + 1)..].Split('.');
            int dot = script.IndexOf('.');
            if (dot < 0 || at.Length != 2 || !int.TryParse(at[0], out int index) || !int.TryParse(at[1], out int k)) return null;
            var scripts = TranslationFiles.Scripts(language, script[..dot]);
            if (scripts == null || !scripts.TryGetValue(script[(dot + 1)..], out var entries) || index < 0 || index >= entries.Count) return null;
            return Pick(entries[index], k, english);
        }
        if (kind is "world" or "map")
        {
            // world:place.key.field.k, map:Place.x,y.field.k
            int first = rest.IndexOf('.');
            int last = rest.LastIndexOf('.');
            int field = last > 0 ? rest.LastIndexOf('.', last - 1) : -1;
            if (first < 0 || field <= first || !int.TryParse(rest[(last + 1)..], out int k)) return null;
            string place = rest[..first], person = rest[(first + 1)..field], which = rest[(field + 1)..last];
            var texts = TranslationFiles.Place(language, place, kind == "world" ? "world" : "maps");
            if (texts == null || !texts.People.TryGetValue(person, out var p)) return null;
            var entry = which switch { "before" => p.Before, "after" => p.After, "dialog" => p.Dialog, _ => null };
            return Pick(entry, k, english);
        }
        return null;
    }

    private static string? Pick(LineEntry? entry, int k, string english) =>
        entry != null && k >= 0 && k < entry.En.Count && entry.En[k] == english ? entry.Translation(k) : null;

    /// <summary>The line as it is said, for the variant the job's key names (<c>~starter=Piplup</c>), or the plain line.</summary>
    private static string? SpokenOf(string line, string variant)
    {
        var values = variant.Split('~', StringSplitOptions.RemoveEmptyEntries)
            .Select(v => v.Split('=', 2)).Where(v => v.Length == 2).ToDictionary(v => v[0], v => (IReadOnlyList<string>)new[] { v[1] });
        var made = SpokenText.Make(line, name => values.TryGetValue(name, out var list) ? list : null);
        if (made.Spoken == null) return null;
        if (variant.Length == 0) return made.Spoken;
        return made.Variants.FirstOrDefault(v => v.Key == variant).Spoken;
    }
}
