using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// A line of a paste that could not be read, as <c>ScriptParser</c> names a script's: its number from 1 and the key of
/// the words that say what is wrong (<c>paste.*</c> in the table). It carries none of the text of the line itself: a
/// paste may come from anyone, and what a refusal shows is the game's own words.
/// </summary>
public sealed record PasteProblem(int Line, string Key);

/// <summary>A paste read: the Pokémon it holds, in order, and the lines that could not be read (a paste with a problem holds none).</summary>
public sealed class PasteResult
{
    public List<BuiltPokemon> Members { get; } = new();
    public List<PasteProblem> Problems { get; } = new();
    public bool Ok => Problems.Count == 0 && Members.Count > 0;
}

/// <summary>
/// A team in Pokémon Showdown's paste format (plan 13 · V22; style guide, "Menu screens", "The friends' page"): the
/// species (a form by the name <see cref="PokemonForm"/> and <see cref="ShowdownNames"/> already use) and item on a line,
/// then Ability, Level, Nature and the moves. Written for a <see cref="BuiltTeam"/>, whose Pokémon have every IV 31 and no
/// effort, so EVs and IVs are never written; read, a paste's nicknames, genders, shininess, happiness, effort, IVs and
/// Tera Type are checked for their shape and then let go, because a built Pokémon has none of them. The names are
/// Showdown's own and so English, whatever language the game is in. GPU-free, no input; the legality of what was read
/// is <see cref="BuiltTeams.WhyNot(BuiltTeam)"/>'s, asked by whoever takes the team.
/// </summary>
public static class ShowdownPaste
{
    /// <summary>The longest paste read: more is refused before a line of it is looked at.</summary>
    public const int LongestText = 8000;

    private static readonly HashSet<string> Stats = new(StringComparer.OrdinalIgnoreCase) { "HP", "Atk", "Def", "SpA", "SpD", "Spe" };

    // ------------------------------------------------------------------ writing

    /// <summary>The paste of a team: its Pokémon one after another with a blank line between.</summary>
    public static string Of(BuiltTeam team) => string.Join("\n\n", team.Members.Select(Of));

    /// <summary>The paste of one built Pokémon.</summary>
    public static string Of(BuiltPokemon p)
    {
        string name = ShowdownNames.ToShowdown(p.Form ?? p.Species);
        var lines = new List<string> { p.Item == null ? name : $"{name} @ {p.Item}" };
        if (p.Ability != null) lines.Add($"Ability: {p.Ability}");
        lines.Add($"Level: {p.Level}");
        lines.Add($"{p.Nature} Nature");
        foreach (string move in p.Moves) lines.Add($"- {move}");
        return string.Join("\n", lines);
    }

    // ------------------------------------------------------------------ reading

    /// <summary>Reads a paste; the result holds the Pokémon, or the lines that could not be read.</summary>
    public static PasteResult Read(string? text)
    {
        var result = new PasteResult();
        if (string.IsNullOrWhiteSpace(text)) { result.Problems.Add(new(1, "paste.empty")); return result; }
        if (text.Length > LongestText) { result.Problems.Add(new(1, "paste.tooLong")); return result; }

        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        BuiltPokemon? current = null;
        var found = new List<BuiltPokemon>();
        var levelSet = new HashSet<BuiltPokemon>();
        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i].Trim();
            if (line.Length == 0) { current = null; continue; }
            // A header of Showdown's exports ("=== [gen9] Name ===") is no Pokémon
            if (line.StartsWith("===", StringComparison.Ordinal)) { current = null; continue; }
            if (current == null)
            {
                if (found.Count == Party.MaxSize) { result.Problems.Add(new(number, "paste.tooMany")); return result; }
                current = Head(line, number, result);
                if (current != null) found.Add(current);
                continue;
            }
            if (line.StartsWith('-'))
            {
                string move = line.TrimStart('-').Trim();
                if (move.Length == 0 || !MoveDatabase.Has(move)) { result.Problems.Add(new(number, "paste.unknownMove")); continue; }
                current.Moves.Add(MoveDatabase.Get(move).Name);
                continue;
            }
            if (line.EndsWith(" Nature", StringComparison.OrdinalIgnoreCase))
            {
                string nature = line[..^" Nature".Length].Trim();
                if (Enum.TryParse<Nature>(nature, true, out var n) && Enum.IsDefined(n)) current.Nature = n;
                else result.Problems.Add(new(number, "paste.unknownNature"));
                continue;
            }
            int colon = line.IndexOf(':');
            if (colon <= 0) { result.Problems.Add(new(number, "paste.unknownLine")); continue; }
            string key = line[..colon].Trim(), value = line[(colon + 1)..].Trim();
            switch (key.ToLowerInvariant())
            {
                case "ability":
                    if (value.Length == 0 || AbilityDatabase.Get(value) == null) result.Problems.Add(new(number, "paste.unknownAbility"));
                    else current.Ability = AbilityDatabase.Get(value)!.Name;
                    break;
                case "level":
                    if (!int.TryParse(value, out int level) || level < 1 || level > BuiltTeams.HighestLevel) result.Problems.Add(new(number, "paste.badLevel"));
                    else { current.Level = level; levelSet.Add(current); }
                    break;
                case "shiny": case "gigantamax":
                    if (!value.Equals("Yes", StringComparison.OrdinalIgnoreCase) && !value.Equals("No", StringComparison.OrdinalIgnoreCase)) result.Problems.Add(new(number, "paste.unknownLine"));
                    break;
                case "happiness": case "dynamax level":
                    if (!int.TryParse(value, out int h) || h is < 0 or > 255) result.Problems.Add(new(number, "paste.unknownLine"));
                    break;
                case "tera type":
                    if (value.Length == 0 || value.Length > 12) result.Problems.Add(new(number, "paste.unknownLine"));
                    break;
                case "evs":
                    if (!Spread(value, 252)) result.Problems.Add(new(number, "paste.badSpread"));
                    break;
                case "ivs":
                    if (!Spread(value, 31)) result.Problems.Add(new(number, "paste.badSpread"));
                    break;
                default:
                    result.Problems.Add(new(number, "paste.unknownLine"));
                    break;
            }
        }
        // Showdown writes no Level line for a Pokémon of 100
        foreach (var p in found) if (!levelSet.Contains(p)) p.Level = BuiltTeams.HighestLevel;
        if (found.Count == 0 && result.Problems.Count == 0) result.Problems.Add(new(1, "paste.empty"));
        if (result.Problems.Count == 0) result.Members.AddRange(found);
        return result;
    }

    /// <summary>
    /// A Pokémon's first line: <c>Nickname (Species) (M) @ Item</c>, any part but the species optional. The nickname and the
    /// gender are let go. Null, with the problem noted, for a line that names no species the data has.
    /// </summary>
    private static BuiltPokemon? Head(string line, int number, PasteResult result)
    {
        string head = line;
        string? item = null;
        int at = head.IndexOf(" @ ", StringComparison.Ordinal);
        if (at >= 0) { item = head[(at + 3)..].Trim(); head = head[..at].Trim(); }
        else if (head.EndsWith(" @", StringComparison.Ordinal)) head = head[..^2].Trim();
        foreach (string gender in new[] { " (M)", " (F)" })
            if (head.EndsWith(gender, StringComparison.Ordinal)) { head = head[..^gender.Length]; break; }
        // "Nickname (Species)": the species is in the last brackets
        if (head.EndsWith(')') && head.LastIndexOf(" (", StringComparison.Ordinal) is var open and > 0) head = head[(open + 2)..^1];
        head = head.Trim();

        if (!Resolve(head, out var species, out var form)) { result.Problems.Add(new(number, "paste.unknownSpecies")); return null; }
        var built = new BuiltPokemon { Species = species!.Name, Form = form?.Name, Nature = Nature.Hardy };
        built.Ability = (form?.Abilities ?? species.Abilities).FirstOrDefault();
        if (item != null)
        {
            if (ItemDatabase.Get(item) is { } known) built.Item = known.Name;
            else result.Problems.Add(new(number, "paste.unknownItem"));
        }
        return built;
    }

    /// <summary>The species and form Showdown's name stands for, if the data has one.</summary>
    public static bool Resolve(string showdownName, out PokemonSpecies? species, out PokemonForm? form)
    {
        species = null;
        form = null;
        if (string.IsNullOrWhiteSpace(showdownName)) return false;
        string ours = ShowdownNames.FromShowdown(showdownName.Trim());
        if (PokemonDatabase.Get(ours) is { } whole) { species = whole; return true; }
        if (PokemonDatabase.SpeciesOfForm(ours) is { } owner && owner.Form(ours) is { } named) { species = owner; form = named; return true; }
        return false;
    }

    // "252 Atk / 4 SpD": numbers of 0 to the most, each on a stat
    private static bool Spread(string value, int most)
    {
        if (value.Length == 0) return false;
        foreach (string part in value.Split('/'))
        {
            var bits = part.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (bits.Length != 2 || !int.TryParse(bits[0], out int n) || n < 0 || n > most || !Stats.Contains(bits[1])) return false;
        }
        return true;
    }

    // ------------------------------------------------------------------ a team

    /// <summary>
    /// A team made of a paste, named: null, with the reason, if the paste can't be read or the team it makes can't battle
    /// (<see cref="BuiltTeams.WhyNot(BuiltTeam)"/>, as a key of the table and what it names).
    /// </summary>
    public static (BuiltTeam? Team, IReadOnlyList<PasteProblem> Problems, (string Key, string Value)? Illegal) Team(string name, string? text)
    {
        var read = Read(text);
        if (!read.Ok) return (null, read.Problems, null);
        var team = new BuiltTeam { Name = PlayerIdentity.Clean(name, BuiltTeams.NameLength), Members = read.Members };
        if (NotTheGames(team) is { } ours) return (null, Array.Empty<PasteProblem>(), ours);
        if (BuiltTeams.WhyNot(team) is { } why) return (null, Array.Empty<PasteProblem>(), why);
        return (team, Array.Empty<PasteProblem>(), null);
    }

    // The builder lists the games' 1,025 alone, and so does a paste
    private static (string Key, string Value)? NotTheGames(BuiltTeam team)
    {
        foreach (var m in team.Members)
            if (PokemonDatabase.Get(m.Species) is { IsOurs: true }) return ("battleRoom.illegal.species", m.Species);
        return null;
    }
}
