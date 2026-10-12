using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests.Golden;

/// <summary>
/// Every script of the game on every way through it (plan 24 · X3), held to <c>scripts.txt</c>: played as
/// <c>StoryTests.EveryScriptOfTheGamePlaysToItsEndOnEveryWayThroughIt</c> plays them (<see cref="ScriptWalk"/>, on
/// a walk of this class's own), a block to each script, named by its full name. In it, whatever started the script
/// (several starters that make it say and do the same are listed together over one telling), then each way through
/// it: the answers taken, what was said (<c>Transcript</c>: the speaker and the line), each question with the answer
/// taken (<c>Asked</c>), what the script did (<c>Log</c>), what went wrong (<c>Problems</c>, none in a sound game), and
/// the flags, variables, trainers, items, badges and starters it left changed. A chapter of plan 02 shows its whole
/// scene in the diff, and a change to the language's runner shows every line it moved.
/// <para>
/// To keep the file small, a way after the first is told only from the answer where it parts from the ways before it:
/// everything said, asked and done before that answer is what the way it parts from said, asked and did. What it left
/// is told whole.
/// </para>
/// </summary>
public class GoldenScriptTests
{
    [Fact]
    public void EveryScriptSaysAndDoesWhatItDid()
    {
        var walk = new ScriptWalk();
        var told = new SortedDictionary<string, List<(string What, string Ways)>>(StringComparer.Ordinal);
        foreach (var (what, script, ways) in walk.PlayEveryScript())
        {
            if (!told.TryGetValue(script.FullName, out var starters)) told[script.FullName] = starters = new();
            starters.Add((what, Ways(ways)));
        }
        Assert.True(told.Count > 100, $"only {told.Count} scripts were played");

        var text = new StringBuilder("# Every script of the game on every way through it (plan 24 · X3), made by GoldenScriptTests. " +
            $"Rewritten by: {GoldenFile.AcceptCommand}\n" +
            "# '>' a line said, '?' a question and the answer taken, '*' what the script did, '!' a problem, " +
            "'+'/'-' a flag set or cleared, '=' a variable changed; a way after the first is told from the answer where it parts from those before it.\n");
        foreach (var (name, starters) in told)
        {
            text.Append($"{GoldenFile.BlockMark}{name}\n");
            foreach (var same in starters.OrderBy(s => s.What, StringComparer.Ordinal).GroupBy(s => s.Ways, StringComparer.Ordinal))
            {
                foreach (var (what, _) in same) text.Append($"from {OneLine(what)}\n");
                text.Append(same.Key);
            }
        }

        GoldenFile.Check("scripts.txt", text.ToString());
    }

    /// <summary>Every way through a script as text, each from where it parts from the ways before it.</summary>
    private static string Ways(IReadOnlyList<ScriptWalk.Way> ways)
    {
        var text = new StringBuilder();
        for (int k = 0; k < ways.Count; k++)
        {
            var way = ways[k];
            // The way before with the longest run of the same answers: everything up to the answer that differs is its
            int parts = 0;
            for (int j = 0; j < k; j++)
            {
                var other = ways[j].Answers;
                int same = 0;
                while (same < other.Count && same < way.Answers.Count && other[same] == way.Answers[same]) same++;
                parts = Math.Max(parts, same);
            }
            bool whole = k == 0 || parts >= way.Marks.Count;
            var (said, asked, logged) = whole ? (0, 0, 0) : way.Marks[parts];
            string answers = way.Answers.Count == 0 ? "-" : string.Join(".", way.Answers);
            text.Append(whole ? $"way {answers}\n" : $"way {answers}, from answer {parts + 1}\n");

            var host = way.Host;
            foreach (var (speaker, line) in host.Transcript.Skip(said))
                text.Append(speaker != null ? $"  {OneLine(speaker)}> {OneLine(line)}\n" : $"  > {OneLine(line)}\n");
            foreach (var (question, answer) in host.Asked.Skip(asked)) text.Append($"  ? {OneLine(question)} = {OneLine(answer)}\n");
            foreach (string entry in host.Log.Skip(logged)) text.Append($"  * {OneLine(entry)}\n");
            foreach (string problem in host.Problems) text.Append($"  ! {OneLine(problem)}\n");
            foreach (string change in Changes(way.Before, host.Story.Snapshot())) text.Append($"  {change}\n");
        }
        return text.ToString();
    }

    /// <summary>What a script changed of the story, in order: flags, variables, trainers beaten, items taken, badges, starters.</summary>
    internal static IEnumerable<string> Changes(StorySnapshot before, StorySnapshot after)
    {
        var had = before.Flags.ToHashSet(StringComparer.Ordinal);
        var has = after.Flags.ToHashSet(StringComparer.Ordinal);
        foreach (string flag in had.Union(has).Order(StringComparer.Ordinal))
            if (had.Contains(flag) != has.Contains(flag)) yield return $"{(has.Contains(flag) ? "+" : "-")} {flag}";
        foreach (string name in before.Variables.Keys.Union(after.Variables.Keys).Order(StringComparer.Ordinal))
        {
            int was = before.Variables.GetValueOrDefault(name), now = after.Variables.GetValueOrDefault(name);
            if (was != now) yield return $"= {name} {was} -> {now}";
        }
        foreach (string id in after.DefeatedTrainers.Except(before.DefeatedTrainers).Order(StringComparer.Ordinal)) yield return $"+ beaten {id}";
        foreach (string id in before.DefeatedTrainers.Except(after.DefeatedTrainers).Order(StringComparer.Ordinal)) yield return $"- beaten {id}";
        foreach (string id in after.TakenItems.Except(before.TakenItems).Order(StringComparer.Ordinal)) yield return $"+ taken {id}";
        foreach (string id in before.TakenItems.Except(after.TakenItems).Order(StringComparer.Ordinal)) yield return $"- taken {id}";
        if (before.Badges != after.Badges) yield return $"= badges {Convert.ToString(before.Badges, 2)} -> {Convert.ToString(after.Badges, 2)}";
        if (before.PlayerStarter != after.PlayerStarter || before.RivalStarter != after.RivalStarter)
            yield return $"= starters {before.PlayerStarter ?? "none"}/{before.RivalStarter ?? "none"} -> {after.PlayerStarter ?? "none"}/{after.RivalStarter ?? "none"}";
    }

    /// <summary>A line as one line of the file.</summary>
    internal static string OneLine(string text) => text.ReplaceLineEndings("\\n");
}
