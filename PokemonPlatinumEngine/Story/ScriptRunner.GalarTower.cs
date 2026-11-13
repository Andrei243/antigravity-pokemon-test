using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The command of plan 33 · GL7, part b: <c>galartower</c>, a run up Galar's Battle Tower ladder in Rose Tower
/// (<see cref="GalarTower"/>), the Frontier's rules lent as <c>blacktower</c> lends them. The ladder lives in the story's
/// own variables.
/// </summary>
public sealed partial class ScriptRunner
{
    // The one waiting in the battle to come, once made ready
    private Trainer? galarTowerFoe;

    /// <summary>
    /// <c>enough</c> is 1 when the team holds three that could go in together; <c>begin</c> starts a run; <c>choose</c>
    /// has the three chosen (RESULT 1, or 0 when backed out of) and <c>check</c> is 0, or 1 for two of a species and 2 for
    /// two holding the same item; <c>active</c> is 1 while a run is under way; <c>rank</c> is the rank from 1 and
    /// <c>wins</c> the wins at it (<c>{number}</c>); <c>leon</c> is 1 when Leon waits at the battle to come;
    /// <c>opponent</c> makes the battle's trainer ready (<c>{foe}</c>); <c>battle</c> fights them (RESULT 1 won, 0 lost);
    /// <c>won</c> counts the win (RESULT 1 the rank rose, 2 Leon beaten; <c>{number}</c> the rank); <c>lost</c> counts the
    /// loss; <c>finish</c> hands over the Battle Points the run earned (RESULT and <c>{number}</c>) and ends the run;
    /// <c>beaten</c> is how many times Leon has been beaten there (<c>{number}</c>).
    /// </summary>
    private void GalarTowerCommand(Instruction i)
    {
        var story = host.Story;
        int Draw() => host.Chance.Next(0x10000);
        int rank = story.Var(GalarTower.Vars.Rank), wins = story.Var(GalarTower.Vars.Wins);
        switch (i.Name)
        {
            case "enough":
                lastNumber = GalarTower.PartySize;
                lastBanned = Listed(FrontierRules.BannedSeen(species => Data.PokemonDatabase.Get(species) is { } s && host.FrontierAct("seen", s.DexNumber, Subject) == 1));
                Result = FrontierRules.EnoughToEnter(host.Party, GalarTower.PartySize) ? 1 : 0;
                break;
            case "begin":
                story.SetVar(GalarTower.Vars.State, 1);
                story.SetVar(GalarTower.Vars.Earned, 0);
                GalarTower.KeepSlots(story, Array.Empty<int>());
                galarTowerFoe = null;
                break;
            case "choose":
                Result = host.FrontierAct("enter", GalarTower.PartySize, Subject);
                afterBusy = () =>
                {
                    Result = host.Answer == 1 && host.Entered.Count == GalarTower.PartySize ? 1 : 0;
                    if (Result == 1) GalarTower.KeepSlots(story, host.Entered.ToList());
                };
                break;
            case "check":
            {
                var chosen = GalarTower.SlotsOf(story).Where(s => s < host.Party.Count).Select(s => host.Party.Members[s]).ToList();
                Result = (int)FrontierRules.WhyNot(chosen);
                break;
            }
            case "active":
                Result = story.Var(GalarTower.Vars.State) == 1 ? 1 : 0;
                break;
            case "rank":
                Result = lastNumber = rank + 1;
                break;
            case "wins":
                Result = lastNumber = wins;
                break;
            case "leon":
                Result = GalarTower.LeonWaits(rank, wins) ? 1 : 0;
                break;
            case "opponent":
            {
                if (GalarTower.LeonWaits(rank, wins) && GalarTower.Leon() is { } leon)
                {
                    galarTowerFoe = leon;
                    towerFoes = new List<FrontierOpponent>();
                    lastFoe = $"{leon.TrainerClass} {leon.Name}";
                    Result = 1;
                    break;
                }
                towerFoes = GalarTower.MakeOpponent(rank, wins, Draw);
                galarTowerFoe = towerFoes[0].AsTrainer();
                lastFoe = string.Join(" and ", towerFoes.Select(f => $"{f.ShownClass} {f.ShownName}"));
                host.FrontierAct("opponents", 0, Subject);
                Result = 1;
                break;
            }
            case "battle":
            {
                var team = FrontierRules.TeamFor(host.Party, GalarTower.SlotsOf(story));
                var foe = galarTowerFoe ?? (GalarTower.LeonWaits(rank, wins) ? GalarTower.Leon() : null)
                    ?? GalarTower.MakeOpponent(rank, wins, Draw)[0].AsTrainer();
                galarTowerFoe = null;
                towerFoes = new List<FrontierOpponent>();
                host.FrontierBattle(team, new List<Trainer> { foe }, null, doubles: false);
                afterBusy = () => Result = host.Outcome == BattleOutcome.Won ? 1 : 0;
                break;
            }
            case "won":
                Result = GalarTower.Won(story);
                lastNumber = story.Var(GalarTower.Vars.Rank) + 1;
                break;
            case "lost":
                GalarTower.Lost(story);
                lastNumber = rank + 1;
                break;
            case "finish":
            {
                int points = story.Var(GalarTower.Vars.Earned);
                if (points > 0)
                {
                    host.Frontier.GivePoints(points);
                    host.FrontierAct("points", 1, Subject);
                }
                Result = lastNumber = points;
                story.SetVar(GalarTower.Vars.State, 0);
                story.SetVar(GalarTower.Vars.Earned, 0);
                GalarTower.KeepSlots(story, Array.Empty<int>());
                break;
            }
            case "beaten":
                Result = lastNumber = story.Var(GalarTower.Vars.LeonBeaten);
                break;
        }
    }
}
