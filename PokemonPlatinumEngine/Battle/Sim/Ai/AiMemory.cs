using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim.Ai;

/// <summary>
/// What the trainer AI knows of the battle beyond what it can see (the original's <c>AIContext</c> fields that last
/// from turn to turn, plan 06 · R9): for each place, the moves seen used by whoever stands there, and the ability and
/// item a line has named; each forgotten when another Pokémon comes into the place, as the original clears them on a
/// switch. Also each trainer's items for the battle, used up as the AI uses them, and where a switch the AI has
/// decided on is going. One per battle, kept by the core, so two battles never share what they know.
/// </summary>
public sealed class AiMemory
{
    private sealed class Seen
    {
        public Pokemon? Who;
        public readonly List<MoveData> Moves = new();
        public string? Ability;
        public ItemData? Item;
    }

    private readonly Dictionary<Place, Seen> seen = new();
    private int logRead;

    /// <summary>The items each trainer has left for the battle (<c>trainerItems</c>), by the trainer.</summary>
    private readonly Dictionary<Trainer, List<string?>> items = new();

    /// <summary>The party member a place's switch is going to (<c>aiSwitchedPartySlot</c>; −1 for none, 6 for "choose it as after a faint").</summary>
    internal readonly Dictionary<Place, int> SwitchedTo = new();

    private Seen At(Place place, Pokemon? who)
    {
        if (!seen.TryGetValue(place, out var s) || s.Who != who) seen[place] = s = new Seen { Who = who };
        return s;
    }

    /// <summary>The moves the AI has seen the Pokémon in this place use, at most four (<c>battlerMoves</c>).</summary>
    public IReadOnlyList<MoveData> MovesSeen(Battler b) => At(b.Place, b.Pokemon).Moves;

    /// <summary>The ability a line has named for the Pokémon in this place (<c>battlerAbilities</c>), null while none has.</summary>
    public string? AbilityShown(Battler b) => At(b.Place, b.Pokemon).Ability;

    /// <summary>The item a line has named for the Pokémon in this place (<c>battlerHeldItems</c>).</summary>
    public ItemData? ItemShown(Battler b) => At(b.Place, b.Pokemon).Item;

    /// <summary>
    /// Reads what has happened since it last looked (<c>BattleAI_SetAbility</c>, <c>BattleAI_SetHeldItem</c>, the clearing
    /// on a switch): a Pokémon coming in is new to the AI; a line that names a Pokémon's ability or item tells it.
    /// </summary>
    internal void CatchUp(BattleCore battle)
    {
        var log = battle.Log;
        for (; logRead < log.Count; logRead++)
        {
            foreach (var happening in Flatten(log[logRead]))
            {
                switch (happening)
                {
                    case Entered entered:
                        seen[entered.Place] = new Seen { Who = entered.Pokemon };
                        break;
                    case Said said:
                        foreach (var b in battle.AllBattlers.Where(b => b.Pokemon != null))
                        {
                            var p = b.Pokemon!;
                            if (p.AbilityName is { } ability && said.Text.Contains($"{b.Name}'s {ability}")) At(b.Place, p).Ability = ability;
                            if (p.HeldItem is { } item && said.Text.Contains(b.Name) && said.Text.Contains(item.Name)) At(b.Place, p).Item = item;
                        }
                        break;
                }
            }
        }
    }

    private static IEnumerable<BattleEvent> Flatten(BattleEvent happening)
    {
        if (happening is Said said)
        {
            foreach (var shown in said.Shows) yield return shown;
            yield return said;
            foreach (var landed in said.OnImpact) yield return landed;
        }
        else yield return happening;
    }

    /// <summary><c>TrainerAI_RecordLastMove</c>: the defender's last move joins the moves seen, if it isn't there yet.</summary>
    internal void RecordLastMove(Battler defender)
    {
        if (defender.Pokemon == null || defender.Volatile.LastMove is not { } last) return;
        var moves = At(defender.Place, defender.Pokemon).Moves;
        if (!moves.Contains(last) && moves.Count < 4) moves.Add(last);
    }

    /// <summary>A trainer's items for this battle, in the order of their data; a used one is null.</summary>
    internal List<string?> ItemsOf(Trainer trainer)
    {
        if (!items.TryGetValue(trainer, out var list)) items[trainer] = list = trainer.Items.Select(i => (string?)i).ToList();
        return list;
    }
}
