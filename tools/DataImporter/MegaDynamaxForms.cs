using System.Text.Json;
using System.Text.Json.Nodes;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace DataImporter;

/// <summary>
/// Plan 06 · R20c: the Mega Dynamax forms of our own (<c>docs/plans/R20c-mega-dynamax-proposals.md</c>, approved by the
/// user on 2026-10-25). Each row of <c>Overrides/mega-dynamax.json</c> names a Mega form and the height of its Mega
/// Dynamax form; this writes <c>&lt;Mega form&gt;-Mmax</c> (<see cref="FormKind.MegaDynamax"/>) right after the Mega,
/// with the Mega's types, base stats, abilities, yields and colour (so taking it in battle changes nothing but the look
/// and the size), no moves of its own, and the weight every Gigantamax form has, 1000 kg. A row whose Mega the data
/// hasn't got yet (R20b's Megas of our own) is named and waits.
/// </summary>
public static class MegaDynamaxForms
{
    public const string FileName = "mega-dynamax.json";

    /// <summary>What a Mega Dynamax form's name adds to its Mega's.</summary>
    public const string Suffix = "-Mmax";

    public const float Weight = 1000f;

    public static Dictionary<string, float> Load(string path)
    {
        var table = JsonNode.Parse(File.ReadAllText(path), documentOptions: new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true })!.AsObject();
        return table.ToDictionary(p => p.Key, p => (float)p.Value!);
    }

    /// <summary>Adds the forms; returns the rows that wait for their Mega.</summary>
    public static List<string> Add(List<PokemonSpecies> species, Dictionary<string, float> table)
    {
        var waiting = new List<string>();
        foreach (var (megaName, height) in table)
        {
            var owner = species.FirstOrDefault(s => s.Forms?.Any(f => f.Name == megaName) == true);
            if (owner == null) { waiting.Add(megaName); continue; }
            var forms = owner.Forms!;
            int at = forms.FindIndex(f => f.Name == megaName);
            var mega = forms[at];
            if (mega.Kind != FormKind.Mega) throw new InvalidDataException($"{FileName}: {megaName} is no Mega form");
            string name = megaName + Suffix;
            forms.RemoveAll(f => f.Name == name);
            forms.Insert(at + 1, new PokemonForm
            {
                Name = name,
                Kind = FormKind.MegaDynamax,
                Types = mega.Types?.ToList(),
                BaseStats = mega.BaseStats,
                Abilities = mega.Abilities?.ToList(),
                HiddenAbility = mega.HiddenAbility,
                Height = height,
                Weight = Weight,
                BaseExpYield = mega.BaseExpYield,
                EvYield = mega.EvYield,
                CatchRate = mega.CatchRate,
                Color = mega.Color,
                Modern = mega.Modern is { } m && (m.Types != null || m.BaseStats != null)
                    ? new SpeciesValues { Types = m.Types?.ToList(), BaseStats = m.BaseStats }
                    : null
            });
        }
        return waiting;
    }
}
