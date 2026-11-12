using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.Delve;

/// <summary>What a run's end left at the camp (plan 44 · SD9).</summary>
public sealed record DelveSettled(IReadOnlyList<Pokemon> Home, IReadOnlyList<Pokemon> Stayed, IReadOnlyList<string> Kept, IReadOnlyList<DelveRequest> Done);

/// <summary>
/// A run's end at the camp (plan 44 · SD9; <c>docs/mechanics/rulings.md</c>, "The delve"), the same for the game and a
/// host with no screen: what the bag found is put into the player's bag (a clear or a rope; nothing on a fall), the
/// recruits that reached the end of a clear come home whole, met in the hollow at their level, where the run's rules let
/// them, and the record takes the run. The caller gives each its id and its place, as a gift. No drawing or input.
/// </summary>
public static class DelveHome
{
    /// <summary>What <c>delve go</c> answers: 1 cleared, 2 fallen, 3 escaped (0 is a run that never went).</summary>
    public static int AnswerOf(DelveOutcome outcome) => outcome switch
    {
        DelveOutcome.Cleared => 1,
        DelveOutcome.Fallen => 2,
        DelveOutcome.Escaped => 3,
        _ => 0
    };

    /// <summary>The first two of a team who may go down, or null.</summary>
    public static (Pokemon Leader, Pokemon Partner)? PairOf(IEnumerable<Pokemon> team)
    {
        var able = team.Where(p => DelveRun.WhyNot(p) == null).Take(2).ToList();
        return able.Count == 2 ? (able[0], able[1]) : null;
    }

    /// <summary>
    /// A run's end settled. <paramref name="mayCome"/> is the run's rules' word on a recruit (a Monotype, no catching);
    /// <paramref name="take"/> puts one on the team or in the PC and says whether there was room.
    /// </summary>
    public static DelveSettled Settle(DelveRun run, DelveRecord record, IReadOnlyList<DelveRequest> board, int day, Inventory bag,
        Func<Pokemon, bool> mayCome, Func<Pokemon, bool> take, string metAt)
    {
        var kept = run.Outcome is DelveOutcome.Cleared or DelveOutcome.Escaped ? run.Bag.ToList() : new List<string>();
        foreach (var name in kept)
            if (ItemDatabase.Get(name) is { } item) bag.AddItem(item, 1);
        var home = new List<Pokemon>();
        var stayed = new List<Pokemon>();
        if (run.Outcome == DelveOutcome.Cleared)
            foreach (var p in run.RecruitsAtTheEnd)
            {
                p.HealFull();
                p.HeldItem = null;
                p.MetLocation = metAt;
                p.MetLevel = p.Level;
                if (mayCome(p) && take(p))
                {
                    home.Add(p);
                    record.RecruitsHome++;
                }
                else stayed.Add(p);
            }
        var done = record.Take(run, board, day);
        return new DelveSettled(home, stayed, kept, done);
    }
}
