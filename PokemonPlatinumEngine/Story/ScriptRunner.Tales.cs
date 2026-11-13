using System.Collections.Generic;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The command of plan 46 · CP12: <c>tale</c>, which an elder, a painted wall, a carved gate or a festival says.
/// <c>learn "id"</c> writes a tale into the Book of Tales: its flag (<see cref="TaleTable.FlagOf"/>, the story's own,
/// so nothing new is saved) is set, the key item's fanfare plays and two lines say so, <c>RESULT</c> 1; a tale already
/// learned changes and says nothing, <c>RESULT</c> 0. <c>has "id"</c> sets <c>RESULT</c> to 1 when it is learned. The
/// id is checked as the script is read, so a tale that isn't in <c>Data/tales.json</c> is a mistake of its file and line.
/// </summary>
public sealed partial class ScriptRunner
{
    private void TaleCommand(Instruction i)
    {
        string flag = TaleTable.FlagOf(i.Other);
        bool known = host.Story.Has(flag);
        switch (i.Name)
        {
            case "has":
                Result = known ? 1 : 0;
                break;
            case "learn":
            {
                if (known)
                {
                    Result = 0;
                    break;
                }
                host.Story.Set(flag);
                host.Fanfare(MusicRole.FanfareKeyItem);
                host.Say(null, new List<string>
                {
                    Text.Of("basme.learned", ("tale", TaleTable.NameOf(i.Other))),
                    Text.Of("basme.writtenDown")
                });
                Result = 1;
                break;
            }
        }
    }
}
