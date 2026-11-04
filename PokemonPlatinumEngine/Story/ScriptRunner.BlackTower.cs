using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The command of plan 22 · U10's last part: <c>blacktower</c>, a climb of Black City's Black Tower (<see cref="BlackTower"/>),
/// the Frontier's rules lent as <c>subway</c> lends them. A climb lives in the story's own variables.
/// </summary>
public sealed partial class ScriptRunner
{
    // The one waiting in the area's battle, once made ready
    private Trainer? blackTowerFoe;

    /// <summary>
    /// <c>enough</c> is 1 when the team holds three that could go in together; <c>begin</c> starts a climb; <c>choose</c>
    /// has the three chosen (RESULT 1, or 0 when backed out of) and <c>check</c> is 0, or 1 for two of a species and 2
    /// for two holding the same item; <c>active</c> is 1 while a climb is under way; <c>next</c> the battle to come and
    /// <c>area</c> its area (<c>{number}</c>); <c>top</c> 1 when Benga waits at it; <c>opponent</c> makes the battle's
    /// trainer ready and stands them in the room as <c>opponent</c> (<c>{foe}</c>); <c>battle</c> fights them (RESULT 1
    /// won, 0 lost); <c>won</c> counts the win (RESULT 1 once an area is cleared, 2 once the top is); <c>finish</c> keeps
    /// the best and hands over the Battle Points earned (RESULT and <c>{number}</c>) and ends the climb; <c>best</c> and
    /// <c>cleared</c> are the records.
    /// </summary>
    private void BlackTowerCommand(Instruction i)
    {
        var story = host.Story;
        int Draw() => host.Chance.Next(0x10000);
        int next = story.Var(BlackTower.Vars.Wins) + 1;
        switch (i.Name)
        {
            case "enough":
                lastNumber = BlackTower.PartySize;
                lastBanned = Listed(FrontierRules.BannedSeen(species => Data.PokemonDatabase.Get(species) is { } s && host.FrontierAct("seen", s.DexNumber, Subject) == 1));
                Result = FrontierRules.EnoughToEnter(host.Party, BlackTower.PartySize) ? 1 : 0;
                break;
            case "begin":
                story.SetVar(BlackTower.Vars.State, 1);
                story.SetVar(BlackTower.Vars.Wins, 0);
                BlackTower.KeepSlots(story, Array.Empty<int>());
                blackTowerFoe = null;
                break;
            case "choose":
                Result = host.FrontierAct("enter", BlackTower.PartySize, Subject);
                afterBusy = () =>
                {
                    Result = host.Answer == 1 && host.Entered.Count == BlackTower.PartySize ? 1 : 0;
                    if (Result == 1) BlackTower.KeepSlots(story, host.Entered.ToList());
                };
                break;
            case "check":
            {
                var chosen = BlackTower.SlotsOf(story).Where(s => s < host.Party.Count).Select(s => host.Party.Members[s]).ToList();
                Result = (int)FrontierRules.WhyNot(chosen);
                break;
            }
            case "active":
                Result = story.Var(BlackTower.Vars.State) == 1 ? 1 : 0;
                break;
            case "next":
                Result = lastNumber = next;
                break;
            case "area":
                Result = lastNumber = BlackTower.AreaOf(next);
                break;
            case "top":
                Result = BlackTower.IsTop(next) && story.Has(BattleSubway.FriendsFlag) ? 1 : 0;
                break;
            case "opponent":
            {
                if (BlackTower.IsTop(next) && story.Has(BattleSubway.FriendsFlag) && BlackTower.Benga() is { } benga)
                {
                    blackTowerFoe = benga;
                    towerFoes = new List<FrontierOpponent>();
                    lastFoe = $"{benga.TrainerClass} {benga.Name}";
                    Result = 1;
                    break;
                }
                towerFoes = BlackTower.MakeOpponent(next, Draw);
                blackTowerFoe = towerFoes[0].AsTrainer();
                lastFoe = string.Join(" and ", towerFoes.Select(f => $"{f.ShownClass} {f.ShownName}"));
                host.FrontierAct("opponents", 0, Subject);
                Result = 1;
                break;
            }
            case "battle":
            {
                var team = FrontierRules.TeamFor(host.Party, BlackTower.SlotsOf(story));
                var foe = blackTowerFoe ?? BlackTower.MakeOpponent(next, Draw)[0].AsTrainer();
                blackTowerFoe = null;
                towerFoes = new List<FrontierOpponent>();
                host.FrontierBattle(team, new List<Trainer> { foe }, null, doubles: false);
                afterBusy = () => Result = host.Outcome == BattleOutcome.Won ? 1 : 0;
                break;
            }
            case "won":
            {
                int wins = story.Var(BlackTower.Vars.Wins) + 1;
                story.SetVar(BlackTower.Vars.Wins, wins);
                Result = wins >= BlackTower.Battles ? 2 : wins % BlackTower.PerArea == 0 ? 1 : 0;
                lastNumber = BlackTower.AreaOf(wins);
                break;
            }
            case "finish":
            {
                int wins = story.Var(BlackTower.Vars.Wins);
                int areas = wins / BlackTower.PerArea;
                if (areas > story.Var(BlackTower.Vars.Best)) story.SetVar(BlackTower.Vars.Best, areas);
                if (wins >= BlackTower.Battles) story.SetVar(BlackTower.Vars.Cleared, story.Var(BlackTower.Vars.Cleared) + 1);
                int points = BlackTower.PointsFor(wins);
                if (points > 0)
                {
                    host.Frontier.GivePoints(points);
                    host.FrontierAct("points", 1, Subject);
                }
                Result = lastNumber = points;
                story.SetVar(BlackTower.Vars.State, 0);
                story.SetVar(BlackTower.Vars.Wins, 0);
                BlackTower.KeepSlots(story, Array.Empty<int>());
                break;
            }
            case "best":
                Result = lastNumber = story.Var(BlackTower.Vars.Best);
                break;
            case "cleared":
                Result = lastNumber = story.Var(BlackTower.Vars.Cleared);
                break;
        }
    }
}
