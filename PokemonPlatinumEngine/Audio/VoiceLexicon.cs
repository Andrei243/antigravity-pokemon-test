using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Audio;

/// <summary>A name and how every voice says it (<c>Data/voice/lexicon.json</c>).</summary>
public sealed class LexiconEntry
{
    public string Name { get; set; } = "";

    /// <summary>A respelling in plain letters, the stressed syllable in capitals, syllables joined by hyphens: <c>POH-kay-mon</c>.</summary>
    public string Say { get; set; } = "";

    /// <summary>word, place, person or species: what kind of name it is, for reading the list.</summary>
    public string Kind { get; set; } = "word";
}

public sealed class LexiconFile
{
    public int Version { get; set; } = 1;
    public string? Note { get; set; }
    public List<LexiconEntry> Entries { get; set; } = new();
}

/// <summary>
/// The pronunciation list (plan 26 · V4), GPU-free: how the names are said, so Pokémon, Piplup and Sandgem sound the same
/// in every voice, the user's samples included. What an engine is handed is the spoken text with each name followed by
/// its respelling where the engine wants one (<see cref="Respell"/>); which form the chosen engine takes is V4's trial's
/// to decide, so nothing here is tied to one.
/// </summary>
public sealed class VoiceLexicon
{
    private static readonly Lazy<VoiceLexicon> loaded = new(() => new VoiceLexicon(GameDataFiles.Load<LexiconFile>(Path.Combine("voice", "lexicon.json"))));

    /// <summary>The game's own list, read once.</summary>
    public static VoiceLexicon Default => loaded.Value;

    /// <summary>The form a respelling must have: syllables of letters joined by hyphens or spaces, at least one in capitals.</summary>
    public static readonly Regex Form = new(@"^[A-Za-z]+([- ][A-Za-z]+)*$");

    public LexiconFile File { get; }
    private readonly List<(LexiconEntry Entry, Regex Pattern)> patterns;

    public VoiceLexicon(LexiconFile file)
    {
        File = file;
        // Longest names first, so "Poké Ball" is found before "Poké"
        patterns = file.Entries.OrderByDescending(e => e.Name.Length)
            .Select(e => (e, new Regex(@"(?<![\p{L}\p{N}])" + Regex.Escape(e.Name) + @"(?![\p{L}\p{N}])", RegexOptions.CultureInvariant)))
            .ToList();
    }

    public LexiconEntry? Of(string name) => File.Entries.FirstOrDefault(e => string.Equals(e.Name, name, StringComparison.Ordinal));

    /// <summary>Whether the respelling is well made: its form, and a stressed syllable in capitals.</summary>
    public static bool WellMade(string say) => Form.IsMatch(say) && say.Split('-', ' ').Any(s => s.Length > 0 && s.All(char.IsUpper));

    /// <summary>
    /// A spoken text with every name the list has written as its respelling, for an engine that reads text and nothing
    /// else: "I'm taking Piplup!" becomes "I'm taking PIP-lup!" (with <paramref name="lowerCase"/>, "pip-lup", for an
    /// engine that reads capitals as letters).
    /// </summary>
    public string Respell(string text, bool lowerCase = false)
    {
        foreach (var (entry, pattern) in patterns)
            text = pattern.Replace(text, lowerCase ? entry.Say.ToLowerInvariant() : entry.Say);
        return text;
    }

    /// <summary>The names the list has that the text says, in the order the list has them.</summary>
    public IEnumerable<LexiconEntry> NamesIn(string text) => patterns.Where(p => p.Pattern.IsMatch(text)).Select(p => p.Entry);

    /// <summary>
    /// The names of <paramref name="candidates"/> that the catalogue's spoken texts say and the list lacks, with how many
    /// lines say each, the most said first: what <c>VoiceTool lexicon</c> reports.
    /// </summary>
    public List<(string Name, int Lines)> Missing(VoiceFile catalogue, IEnumerable<string> candidates)
    {
        var have = File.Entries.Select(e => e.Name).ToHashSet(StringComparer.Ordinal);
        var wanted = candidates.Where(c => c.Length >= 3 && !have.Contains(c)).Distinct(StringComparer.Ordinal).ToList();
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var tests = wanted.Select(w => (w, new Regex(@"(?<![\p{L}\p{N}])" + Regex.Escape(w) + @"(?![\p{L}\p{N}])", RegexOptions.CultureInvariant))).ToList();
        foreach (var line in catalogue.Lines)
        {
            if (line.Spoken is not { Length: > 0 } spoken) continue;
            foreach (var (name, regex) in tests)
                if (regex.IsMatch(spoken)) counts[name] = counts.GetValueOrDefault(name) + 1;
        }
        return counts.OrderByDescending(c => c.Value).ThenBy(c => c.Key, StringComparer.Ordinal).Select(c => (c.Key, c.Value)).ToList();
    }

    /// <summary>The list as a table for <c>docs/voice/samples.md</c>'s pronunciation guide.</summary>
    public string Table(IEnumerable<string> names)
    {
        var sb = new StringBuilder("| Name | Say |\n| --- | --- |\n");
        foreach (string name in names)
            if (Of(name) is { } e) sb.Append("| ").Append(e.Name).Append(" | ").Append(e.Say).Append(" |\n");
        return sb.ToString();
    }
}
