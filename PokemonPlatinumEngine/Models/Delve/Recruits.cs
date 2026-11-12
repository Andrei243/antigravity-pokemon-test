using System;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.Delve;

/// <summary>
/// Recruits (plan 44 · SD7; <c>docs/mechanics/rulings.md</c>, "The delve"): a resident a team member knocks out may ask
/// to join, by a draw of a hundred under <see cref="ChanceOf"/>, while the team has room (<see cref="DelveRun.MostOnTheTeam"/>).
/// The leader answers (no turn); one that joins comes back at half its HP, cured, where it fell. No drawing or input.
/// </summary>
public static class Recruits
{
    /// <summary>The most a resident's chance comes to, in a hundred.</summary>
    public const int MostChance = 50;

    /// <summary>
    /// A resident's chance to ask, in a hundred: its catch rate's share of thirty, two more for each level the leader
    /// stands over it (two less for each under), from nought to <see cref="MostChance"/>; nought for a legendary or a
    /// mythical species.
    /// </summary>
    public static int ChanceOf(Pokemon resident, int leaderLevel)
    {
        if (resident.Species.Legendary || resident.Species.Mythical) return 0;
        return Math.Clamp(resident.CatchRate * 30 / 255 + (leaderLevel - resident.Level) * 2, 0, MostChance);
    }

    internal static void MayAsk(DelveRun run, DelveMon resident)
    {
        if (run.Asking != null || run.Team.Count >= DelveRun.MostOnTheTeam || resident.IsGuardian) return;
        int roll = run.FixedRecruitRoll ?? run.Rng.Next(100);
        if (roll >= ChanceOf(resident.Pokemon, run.Leader.Pokemon.Level)) return;
        run.Asking = resident;
        run.Say(DelveEventKind.AsksToJoin, resident.Name);
    }

    internal static void Answer(DelveRun run, bool yes)
    {
        var m = run.Asking!;
        run.Asking = null;
        run.Residents.Remove(m);
        if (!yes || run.Team.Count >= DelveRun.MostOnTheTeam)
        {
            run.Say(DelveEventKind.Declined, m.Name);
            return;
        }
        m.OnTeam = true;
        m.Recruit = true;
        m.Dormant = false;
        m.Shy = false;
        m.ConfusedTurns = 0;
        m.Flinched = false;
        m.Pokemon.Status = StatusCondition.None;
        m.Pokemon.CurrentHP = Math.Max(1, m.Pokemon.MaxHP / 2);
        // Where it fell, unless someone stands there since
        if (run.Team.Concat(run.Residents).Any(o => !o.Fainted && o.X == m.X && o.Y == m.Y))
        {
            var free = run.Floor.Grid.Reachable((run.Leader.X, run.Leader.Y), new System.Collections.Generic.HashSet<(int, int)>())
                .Where(t => run.At(t.X, t.Y) == null).OrderBy(t => Math.Abs(t.X - run.Leader.X) + Math.Abs(t.Y - run.Leader.Y))
                .ThenBy(t => t.Y).ThenBy(t => t.X).First();
            (m.X, m.Y) = free;
        }
        run.Team.Add(m);
        run.Say(DelveEventKind.Joined, m.Name);
    }
}
