using System.Collections.Generic;
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
/// <c>{item}</c> names it; 0 when nothing is owed). A host that keeps no records gives no prize. <c>peak check</c> and
/// <c>peak</c> are peak training (plan 36 · TR3): whether the chosen Pokémon may be trained, and the question that does it.
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

            case "peak" when i.Other == "check":
                Result = (int)PeakCheck(chosenMember);
                return;

            case "peak":
                PeakQuestion();
                return;
        }
    }

    /// <summary>What <c>train peak check</c> answers (plan 36 · TR3), in the order it is asked.</summary>
    public enum PeakReadiness
    {
        /// <summary>Nobody was chosen, or an Egg was.</summary>
        Nobody = 0,
        Ready = 1,
        /// <summary>Under level 50 (<see cref="HyperTraining.Level"/>).</summary>
        TooLow = 2,
        /// <summary>Every stat's IV is 31 or brought to its peak already.</summary>
        AtItsPeak = 3,
        /// <summary>No Bottle Cap and no Gold Bottle Cap in the bag.</summary>
        NoCap = 4
    }

    private PeakReadiness PeakCheck(Pokemon? member)
    {
        if (member is not { IsEgg: false }) return PeakReadiness.Nobody;
        if (member.Level < HyperTraining.Level) return PeakReadiness.TooLow;
        if (!EffortRules.Order.Any(s => HyperTraining.CanTrain(member, s))) return PeakReadiness.AtItsPeak;
        return Held(HyperTraining.BottleCap) || Held(HyperTraining.GoldBottleCap) ? PeakReadiness.Ready : PeakReadiness.NoCap;
    }

    private bool Held(string item) => ItemDatabase.Get(item) is { } data && host.Bag.GetQuantity(data) > 0;

    /// <summary>
    /// <c>train peak</c> (plan 36 · TR3, peak training: <see cref="HyperTraining"/>): asks which stat of the chosen Pokémon shall
    /// reach its peak, each stat it may with the judge's word for its IV, where a Bottle Cap is held, and all six where a Gold
    /// Bottle Cap is; the answer takes one cap from the bag and trains. <c>RESULT</c> 1 for one stat (<c>{thing}</c> names
    /// it), 2 for all six, 0 when nothing was trained.
    /// </summary>
    private void PeakQuestion()
    {
        Result = 0;
        var member = chosenMember;
        if (PeakCheck(member) != PeakReadiness.Ready || member == null) return;
        var choices = new List<(StatType? Stat, string Row)>();
        if (Held(HyperTraining.BottleCap))
            foreach (var stat in IvJudge.Stats)
            {
                if (!HyperTraining.CanTrain(member, stat)) continue;
                var grade = IvJudge.Grade(IvJudge.IvOf(member, stat));
                string word = Core.Text.Of($"boxTeams.judge.grade.{grade}");
                choices.Add((stat, Core.Text.Of("training.peak.row", ("stat", Core.Words.Stat(stat)), ("word", word))));
            }
        if (Held(HyperTraining.GoldBottleCap))
            choices.Add((null, Core.Text.Of("training.peak.all", ("item", LocalNames.Item(HyperTraining.GoldBottleCap)))));
        var rows = choices.Select(c => c.Row).Append(Core.Text.Of("training.peak.never")).ToList();
        host.Ask(speakerShown, Core.Text.Of("training.peak.question"), rows, cancel: choices.Count);
        afterBusy = () =>
        {
            Result = 0;
            if (host.Answer < 0 || host.Answer >= choices.Count) return;
            var (stat, _) = choices[host.Answer];
            string capName = stat == null ? HyperTraining.GoldBottleCap : HyperTraining.BottleCap;
            if (ItemDatabase.Get(capName) is not { } cap || host.Bag.GetQuantity(cap) <= 0) return;
            if (!HyperTraining.Train(cap, member, stat ?? StatType.HP)) return;
            host.Bag.RemoveItem(cap, 1);
            lastItem = cap.Name;
            lastThing = stat is { } one ? Core.Words.Stat(one) : "";
            Result = stat == null ? 2 : 1;
        };
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
