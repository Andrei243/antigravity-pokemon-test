using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The command of plan 36 · TR2: <c>train</c>, the training hall. <c>open</c> asks whether a Hall of Fame of any region
/// has been entered (<c>RESULT</c> 1 or 0), which is when the hall's Champion's things open (plan 36's decision 1);
/// <c>drill</c> opens the drill screen on the Pokémon the last <c>choosepokemon</c> chose and waits for it (<c>RESULT</c> the
/// grade won and one: 1 bronze, 2 silver, 3 gold; 0 when it was left before a drill, or nobody fit was chosen);
/// <c>reset all</c> or <c>reset attack</c> (and <c>hp</c>, <c>defense</c>, <c>spatk</c>, <c>spdef</c>, <c>speed</c>) takes that
/// Pokémon's effort in the stat, or in all six, to nought (<c>RESULT</c> 1 when there was any to take); <c>prize</c> hands
/// over the first prize the drills' records have earned and the coach hasn't given, into the bag (<c>RESULT</c> 1, and
/// <c>{item}</c> names it; 0 when nothing is owed). A host that keeps no records gives no prize.
/// </summary>
public sealed partial class ScriptRunner
{
    private void TrainCommand(Instruction i)
    {
        switch (i.Name)
        {
            case "open":
                Result = HallOfFameEntered ? 1 : 0;
                return;

            case "drill":
            {
                Result = 0;
                var member = chosenMember;
                int index = member == null ? -1 : host.Party.Members.ToList().IndexOf(member);
                if (member == null || member.IsEgg || index < 0 || host.Training == null) return;
                host.Open(ScriptScreen.Training, Subject, index.ToString());
                afterBusy = () => Result = host.Answer;
                return;
            }

            case "reset":
            {
                Result = 0;
                if (chosenMember is not { IsEgg: false } member) return;
                bool any = false;
                foreach (var stat in DrillKinds.Stats)
                {
                    if (i.Other != "all" && StatWord(stat) != i.Other) continue;
                    if (EffortRules.Get(member, stat) == 0) continue;
                    EffortRules.Set(member, stat, 0);
                    any = true;
                }
                if (!any) return;
                member.ReckonStats();
                Result = 1;
                return;
            }

            case "prize":
            {
                Result = 0;
                if (host.Training?.Hand(HallOfFameEntered) is not { } prize || ItemDatabase.Get(prize) is not { } item) return;
                host.Bag.AddItem(item, 1);
                lastItem = item.Name;
                Result = 1;
                return;
            }
        }
    }

    /// <summary>Whether a Hall of Fame of any region has been entered.</summary>
    private bool HallOfFameEntered => RegionDatabase.All.Any(r => host.Story.Has(r.StoryCompleteFlag));

    /// <summary>A stat as the script writes it: <c>hp</c>, <c>attack</c>, <c>defense</c>, <c>spatk</c>, <c>spdef</c>, <c>speed</c>.</summary>
    private static string StatWord(StatType stat) => stat switch
    {
        StatType.HP => "hp",
        StatType.Attack => "attack",
        StatType.Defense => "defense",
        StatType.SpAttack => "spatk",
        StatType.SpDefense => "spdef",
        _ => "speed"
    };
}
