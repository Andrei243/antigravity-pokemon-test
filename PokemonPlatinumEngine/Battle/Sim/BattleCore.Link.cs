using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.Battle.Sim;

// A battle between two players (plan 28 · I4): giving the match up, and the choice a turn's timer makes. Nothing here
// runs in a battle that isn't a link battle but DefaultChoice, which only reads.
public sealed partial class BattleCore
{
    /// <summary>A battle between two players (<see cref="CoreSetup.Link"/>).</summary>
    public bool Link { get; }

    /// <summary>
    /// The side of <paramref name="giving"/> gives the match up (<see cref="ChoiceKind.Forfeit"/>): it is lost there and
    /// then, whatever else was asked, and the record keeps the answer, so a replay ends where the battle did.
    /// </summary>
    private void Forfeit(BattleChoice giving)
    {
        if (Request == null) throw new InvalidOperationException("The battle isn't waiting for a choice");
        // Whatever stands in the place (a question to fill a fainted one's is asked too): only the battle's kind decides
        if (!Link) throw new ArgumentException(Text.Of("battle.noThereNoRunning"));
        answer = new List<BattleChoice> { giving };
        answers.Add(answer);
        using var narration = Narration.ForBothSides(Link);
        bool mine = giving.Who.Side == BattleSide.Player;
        string who = mine ? playerName : Trainers.Count > 0 ? Trainers[0].Name : playerName;
        Say(Text.Of("battle.forfeitedTheMatch", ("player", who)));
        End(mine ? BattleResult.PlayerDefeat : BattleResult.PlayerVictory);
        // The battle's course stops at the question it was asked: nothing more is worked out
        Request = null;
    }

    /// <summary>
    /// The choice a turn's timer makes for a place that has not answered (plan 28 · I4): so a server never decides what a
    /// player "would have" done. For a turn: the first of its moves it may use, Struggle where it may use none, the first
    /// of its party it may switch to where neither is allowed; for a place to fill: the first of its party able to come
    /// in; at Battle Style Shift's offer: staying in. Only reads the battle.
    /// </summary>
    public BattleChoice DefaultChoice(Place place)
    {
        var b = At(place);
        switch (Request)
        {
            case ReplacementRequest replacement when replacement.Place == place:
                for (int i = 0; b.Roster != null && i < b.Roster.Count; i++)
                    if (CanSendIn(b, i)) return BattleChoice.Switch(place, i);
                break;
            case SwitchOfferRequest offer when offer.Place == place:
                return BattleChoice.Stay(place);
            case ActionRequest asked when asked.Places.Contains(place):
                for (int i = 0; i < b.Pokemon!.Moves.Count; i++)
                {
                    var fight = BattleChoice.Fight(place, i);
                    if (WhyNot(fight) == null) return fight;
                }
                var struggle = BattleChoice.Fight(place, -1);
                if (WhyNot(struggle) == null) return struggle;
                for (int i = 0; b.Roster != null && i < b.Roster.Count; i++)
                {
                    var switching = BattleChoice.Switch(place, i);
                    if (WhyNot(switching) == null) return switching;
                }
                return struggle;
        }
        throw new InvalidOperationException($"Nothing is asked of {place}");
    }
}
