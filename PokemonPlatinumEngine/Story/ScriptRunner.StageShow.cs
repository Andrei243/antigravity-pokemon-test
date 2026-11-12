using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models.StageShow;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// Plan 42 · PS1, PS2 (<c>docs/scripts.md</c>, "The stage show"): the command <c>stageshow</c>, which the theatre's owner,
/// its desk and its three fans say.
/// </summary>
public sealed partial class ScriptRunner
{
    /// <summary>
    /// <c>stageshow trunk</c>: the owner's gift of the trunk and a first handful of props, once: RESULT 1 (and
    /// <c>{number}</c> how many pieces), 0 when it was given already. <c>stageshow has</c>: 1 once the trunk is given.
    /// <c>stageshow dress</c>: the dressing room (the script waits): RESULT 1 when a dressing was kept, 0 otherwise.
    /// <c>stageshow perform</c>: a show on the stage (the script waits): RESULT the player's place, 1 to 4, or 0 backed out
    /// of. <c>stageshow fan fan_1</c>: the piece that fan waits with goes into the trunk: RESULT 1 (and <c>{thing}</c> its
    /// name), 0 when they wait with none, 2 when the trunk holds nine of it already (they go on waiting).
    /// </summary>
    private void StageShowCommand(Instruction i)
    {
        var records = host.StageShow;
        switch (i.Name)
        {
            case "trunk":
                Result = records != null && records.GiveTrunk() ? 1 : 0;
                lastNumber = ShowRecords.FirstHandful.Length;
                break;
            case "has":
                Result = records?.TrunkGiven == true ? 1 : 0;
                break;
            case "dress":
                if (records?.TrunkGiven != true) { Result = 0; break; }
                host.Open(ScriptScreen.DressingRoom, Subject);
                afterBusy = () => Result = host.Answer;
                break;
            case "perform":
                if (records?.TrunkGiven != true) { Result = 0; break; }
                host.Open(ScriptScreen.StageShow, Subject);
                afterBusy = () => Result = host.Answer;
                break;
            case "fan":
            {
                string? prop = records?.WaitingWith(i.Other);
                if (records == null || prop == null) { Result = 0; break; }
                if (records.Trunk.Add(prop) == 0) { Result = 2; break; }
                records.TakeFrom(i.Other);
                lastThing = ShowPropDatabase.Get(prop) is { } piece ? Text.Of(piece.NameKey) : prop;
                Result = 1;
                break;
            }
        }
    }
}
