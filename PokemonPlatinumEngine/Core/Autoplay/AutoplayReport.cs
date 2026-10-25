using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PokemonPlatinumEngine.Core.Autoplay;

/// <summary>
/// What a run of the playthrough bot or the key fuzzer saw (plan 16 · T7), GPU-free: the places come to in order,
/// the people, items, obstacles, triggers and ways it went for, the scripts that ran, the battles, and every finding
/// (an exception, a softlock, a value out of range, a script gone wrong, a goal dropped). <see cref="ToMarkdown"/>
/// writes it as <c>play.md</c>, with the maps a walk of the whole game reaches that the run did not.
/// </summary>
public sealed class AutoplayReport
{
    public List<string> Places { get; } = new();
    public HashSet<string> Maps { get; } = new(StringComparer.Ordinal);
    public List<string> Spoken { get; } = new();
    public List<string> Items { get; } = new();
    public List<string> Obstacles { get; } = new();
    public List<string> Triggers { get; } = new();
    public List<string> Warps { get; } = new();
    public HashSet<string> Scripts { get; } = new(StringComparer.Ordinal);
    public int Battles { get; set; }
    public int BattlesWon { get; set; }
    public int TrainersBeaten { get; set; }
    public int Tiles { get; set; }
    public double Seconds { get; set; }
    public bool Finished { get; set; }

    public List<string> Exceptions { get; } = new();
    public List<string> Softlocks { get; } = new();
    public List<string> OutOfRange { get; } = new();
    public List<string> ScriptsGoneWrong { get; } = new();
    public List<string> Dropped { get; } = new();

    /// <summary>Nothing went wrong: no exception, softlock, value out of range or script gone wrong (a dropped goal is no finding).</summary>
    public bool Clean => Exceptions.Count == 0 && Softlocks.Count == 0 && OutOfRange.Count == 0 && ScriptsGoneWrong.Count == 0;

    /// <summary>Notes a place come to; true the first time.</summary>
    public bool Arrive(string place)
    {
        if (Places.Contains(place)) return false;
        Places.Add(place);
        return true;
    }

    /// <summary>The maps <paramref name="reachable"/> names that the run never stood on, in order.</summary>
    public IEnumerable<string> NotReached(IEnumerable<string> reachable) =>
        reachable.Where(m => !Maps.Contains(m)).Distinct(StringComparer.Ordinal).OrderBy(m => m, StringComparer.Ordinal);

    public string ToMarkdown(IEnumerable<string>? reachable = null)
    {
        var md = new StringBuilder();
        md.AppendLine("# play");
        md.AppendLine();
        md.AppendLine($"{Seconds:0} seconds of game time, {Tiles} tiles stood on, {Maps.Count} maps, {Places.Count} places{(Finished ? "; nothing left within reach" : "")}.");
        md.AppendLine();
        md.AppendLine($"- spoken to: {Spoken.Count}");
        md.AppendLine($"- items picked up: {Items.Count}");
        md.AppendLine($"- obstacles looked at: {Obstacles.Count}");
        md.AppendLine($"- triggers stepped on: {Triggers.Count}");
        md.AppendLine($"- ways taken: {Warps.Count}");
        md.AppendLine($"- scripts run: {Scripts.Count}");
        md.AppendLine($"- battles: {Battles}, won {BattlesWon}; trainers beaten: {TrainersBeaten}");
        Section(md, "Findings: exceptions", Exceptions);
        Section(md, "Findings: softlocks", Softlocks);
        Section(md, "Findings: values out of range", OutOfRange);
        Section(md, "Findings: scripts gone wrong", ScriptsGoneWrong);
        Section(md, "Goals dropped after three tries", Dropped);
        Section(md, "Places, in the order come to", Places);
        if (reachable != null) Section(md, "Maps a walk of the whole game reaches that the run did not", NotReached(reachable).ToList());
        return md.ToString();
    }

    private static void Section(StringBuilder md, string title, IReadOnlyCollection<string> lines)
    {
        md.AppendLine();
        md.AppendLine($"## {title} ({lines.Count})");
        if (lines.Count == 0) return;
        md.AppendLine();
        foreach (string line in lines) md.AppendLine("- " + line.Replace('\n', ' ').Replace('\r', ' '));
    }
}
