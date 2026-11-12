using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The care book in the game (plan 35 · BN1; docs/mechanics/rulings.md, "Care"): what writes in it as the game is played
/// (a battle's end, a walk cycle, a hatching, an evolution, a heal, poison's bite, the days) and what the save keeps of
/// it. The rules are <see cref="CareRules"/>'s; nothing here draws from any generator, so nothing seeded moves.
/// </summary>
public partial class GameEngine
{
    /// <summary>Today, as the care book counts days.</summary>
    internal int CareDay => CareRules.DayOf(GameClock.Today);

    /// <summary>
    /// A battle over: the Pokémon of the player's it sent out take the mark of where it was fought and the mood of how it
    /// went, and those that gained a level are glad of it. Not the catching lesson (the assistant's), nor a battle of the
    /// Battle Frontier's copies, nor one watched on the Vs. Recorder (which never ends here).
    /// </summary>
    private void CareAfterBattle(BattleEngine fought, bool defeat)
    {
        if (fought.Kind == BattleKind.CatchingLesson || fought.IsFrontier) return;
        var own = new HashSet<Pokemon>(flow.Party.Members, ReferenceEqualityComparer.Instance);
        var sent = fought.SentOutShown.Where(own.Contains).ToList();
        var levelled = fought.LeveledUp.Where(own.Contains).ToList();
        bool won = fought.Result is BattleResult.PlayerVictory or BattleResult.EnemyCaught;
        CareRules.AfterBattle(flow.Care, new CareRules.BattleOutcome(sent, won && !defeat, defeat, levelled,
            fought.Core.Conditions.Terrain, fought.Core.Field.SkyInEffect));
        // The follower answers it with a bubble of its own (plan 35 · BN4)
        NoteForTheLead(levelled.Count > 0 && !defeat ? LeadFollower.Moment.Levelled : defeat ? LeadFollower.Moment.Lost : LeadFollower.Moment.Won);
    }

    /// <summary>A Pokémon has just hatched: it is glad to be out.</summary>
    private void CareHatched(Pokemon hatched) => CareRules.Lift(flow.Care.Of(hatched), CareRules.MoodHatched);

    /// <summary>A Pokémon has just evolved: it is glad of it.</summary>
    private void CareEvolved(Pokemon evolved) => CareRules.Lift(flow.Care.Of(evolved), CareRules.MoodEvolved);

    /// <summary>The team was healed by a script's <c>heal</c> (a nurse, a bed, Mom): each is glad of the rest.</summary>
    internal void CareHealed()
    {
        foreach (var p in flow.Party.Members)
            if (!p.IsEgg) CareRules.Lift(flow.Care.Of(p), CareRules.MoodHealed);
    }

    /// <summary>Poison bit in the field: each Pokémon still poisoned is a little lower for it.</summary>
    private void CarePoisonBite()
    {
        foreach (var p in flow.Party.Members)
            if (!p.IsEgg && p.Status == StatusCondition.Poison) CareRules.Lift(flow.Care.Of(p), CareRules.MoodPoisonBite);
    }

    /// <summary>
    /// The book as the save keeps it: pruned first to the Pokémon the save holds (the team, the boxes, the Day Care, those
    /// away on a job, the team set aside for the Bug-Catching Contest), so a Pokémon that left the save leaves the book.
    /// </summary>
    private CareSave? CareToSave()
    {
        var held = OwnedPokemon().Select(p => p.Id)
            .Concat(flow.Jobs.Away.Where(j => j.Id is { }).Select(j => j.Id!.Value))
            .Concat(flow.BugContest.Team.Select(p => p.Id))
            .Where(id => id != Guid.Empty);
        flow.Care.Prune(held);
        return flow.Care.ToSave();
    }
}
