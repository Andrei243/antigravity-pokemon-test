using System;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The commands of plan 41 · EG4 and EG7.
/// <para>
/// <c>circuit</c> is the Laurel Circuit's (<see cref="ChampionsCircuit"/>), on the host's <see cref="IScriptHost.Circuit"/>:
/// <c>begin</c> begins one where it may be (RESULT 1, else 0); <c>running</c> and <c>ready</c> ask whether one is under
/// way and whether every finished region is stamped; <c>level</c> sets RESULT to the level its teams are met at;
/// <c>stamps</c> to the regions stamped, <c>regions</c> to the regions finished, <c>count</c> to the circuits won;
/// <c>final &lt;Region&gt;</c> asks whether the region's Champion stands in the final, <c>stamped &lt;Region&gt;</c>
/// whether the region is stamped; <c>won</c> records the final won (the team on the circuit's plinth) and sets RESULT
/// to the circuits won. A host that keeps no circuit answers 0 to everything.
/// </para>
/// <para>
/// <c>deduce &lt;case&gt; &lt;n&gt;</c> asks the case's <paramref name="n"/>th deduction (<c>cases.&lt;id&gt;.deduce.&lt;n&gt;</c>)
/// with the clues found as its answers and a last one to think it over: RESULT 1 for the clue that answers, 0 otherwise.
/// </para>
/// </summary>
public sealed partial class ScriptRunner
{
    private void CircuitCommand(Instruction i)
    {
        if (host.Circuit is not { } circuit)
        {
            Result = 0;
            return;
        }
        Result = i.Name switch
        {
            "begin" => ChampionsCircuit.Begin(circuit, host.Story) ? 1 : 0,
            "running" => circuit.Running ? 1 : 0,
            "ready" => ChampionsCircuit.Ready(circuit, host.Story) ? 1 : 0,
            "level" => ChampionsCircuit.Level(circuit, host.Story),
            "stamps" => circuit.Stamps.Count,
            "regions" => ChampionsCircuit.Finished(host.Story).Count,
            "count" => circuit.Won,
            "final" => circuit.Running && ChampionsCircuit.FinalOrder(host.Story).Any(r => string.Equals(r.Region, i.Other, StringComparison.OrdinalIgnoreCase)) ? 1 : 0,
            "stamped" => circuit.Stamps.Contains(i.Other, StringComparer.OrdinalIgnoreCase) ? 1 : 0,
            "won" => ChampionsCircuit.WinFinal(circuit, host.Party,
                p => p.OriginalTrainer is { } mark ? (mark.Name, mark.Id) : (host.PlayerName, 0), GameClock.Today),
            _ => 0
        };
    }

    private void Deduce(Instruction i)
    {
        var @case = Cases.Find(i.Name) ?? throw Wrong(i, $"there is no case '{i.Name}'");
        var offered = Cases.Answers(@case, host.Story);
        string id = @case.Id;
        var answers = offered.Select(c => ClueWords(id, c.Key)).Append(Text.Of("cases.thinkAgain")).ToList();
        int n = i.Number;
        host.Ask(speakerShown, Text.Of($"cases.{id}.deduce.{n}"), answers, cancel: answers.Count - 1);
        afterBusy = () => Result = Cases.Answers(@case, n, host.Story, host.Answer) ? 1 : 0;
    }

    /// <summary>A clue as the deduction's answers name it.</summary>
    private static string ClueWords(string id, string key) => Text.Of($"cases.{id}.clue.{key}");
}
