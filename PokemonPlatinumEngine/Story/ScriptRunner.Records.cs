using System.Linq;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The command of plan 40 · GE10: <c>records</c>, the record desk's. <c>mix</c> mixes with the player's other saves the
/// host hands over (<see cref="IScriptHost.OtherSaves"/>, read and never written), <c>RESULT</c> the number mixed and
/// <c>{thing}</c> their players' names; <c>count</c> is how many records the game keeps and <c>{thing}</c> their names;
/// <c>bases</c> how many friends' secret bases are registered and <c>{thing}</c> whose. A host that keeps no records
/// (<see cref="IScriptHost.MixedRecords"/> null) mixes nothing.
/// </summary>
public sealed partial class ScriptRunner
{
    private void RecordsCommand(Instruction i)
    {
        if (host.MixedRecords is not { } state)
        {
            Result = 0;
            lastThing = "";
            return;
        }
        switch (i.Name)
        {
            case "mix":
            {
                var others = host.OtherSaves();
                var (trainerId, started) = host.OwnGame;
                Result = RecordMix.Mix(state, others, trainerId, started, host.Today);
                lastThing = Names(state.Records.Take(Result).Select(r => r.Name));
                break;
            }
            case "count":
                Result = state.Records.Count;
                lastThing = Names(state.Records.Select(r => r.Name));
                break;
            case "bases":
            {
                var bases = RecordMix.Bases(state);
                Result = bases.Count;
                lastThing = Names(bases.Select(r => r.Name));
                break;
            }
        }
    }

    // Names one after another, as a list is said: "A", "A and B", "A, B and C"
    private static string Names(System.Collections.Generic.IEnumerable<string> names)
    {
        var list = names.Where(n => n.Length > 0).ToList();
        return list.Count switch
        {
            0 => "",
            1 => list[0],
            _ => Core.Text.Of("recordMix.list", ("first", string.Join(", ", list.Take(list.Count - 1))), ("last", list[^1]))
        };
    }
}
