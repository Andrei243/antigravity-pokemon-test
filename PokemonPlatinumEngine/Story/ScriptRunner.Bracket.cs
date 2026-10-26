using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The command of plan 14 · B8: <c>bracket</c>, a Battle Dome tournament from the desk to the final and back
/// (<see cref="BattleDomeChallenge"/> over a <see cref="Models.Frontier.Bracket"/>). What the Dome shares with the
/// Frontier's facilities (the team entered, the copies at level 50, a battle of the Frontier, Battle Points) goes
/// through <c>frontier</c>'s host words (<see cref="IScriptHost.FrontierAct"/>, <see cref="IScriptHost.FrontierBattle"/>).
/// </summary>
public sealed partial class ScriptRunner
{
    // The places in the team of the three chosen at the desk, before a tournament is begun with them
    private IReadOnlyList<int> domeSlots = Array.Empty<int>();

    /// <summary>
    /// At the desk: <c>enough</c> is 1 when the team holds three Pokémon that could go in together (<c>{banned}</c>
    /// lists the species of the banlist the Pokédex has seen); <c>choose</c> has the three chosen (RESULT 1, 0 when
    /// backed out of) and <c>check</c> is 0, or 1 for two of a species and 2 for two holding the same item;
    /// <c>begin</c> draws the tournament's fifteen trainers and its bracket; <c>streak</c> and <c>best</c> are the
    /// tournaments won in a row and the most ever (<c>{number}</c>). In the hall: <c>active</c> is 1 while a
    /// tournament is under way; <c>round</c> is the round to come (0 to 3, 3 the final; <c>{number}</c> 1 to 4);
    /// <c>show</c> opens the bracket to be looked at (the script waits); <c>opponent</c> stands the round's opponent
    /// on the stage (<c>{foe}</c>); <c>pick</c> opens the bracket on the opponent's card for two of the three to be
    /// sent in (the script waits; backing out sends the first two); <c>battle</c> is fought (RESULT 1 won, 0 lost);
    /// <c>won</c> decides the round, the other matches fought headless (RESULT 1 when it was the final) and
    /// <c>lost</c> the same with the player out. At the end: <c>finish won|lost</c> writes the record, <c>points</c>
    /// hands over the Battle Points a win earns (RESULT and <c>{number}</c>), and <c>end</c> lets the tournament go.
    /// </summary>
    private void DomeBracket(Instruction i)
    {
        var records = host.Frontier;
        BattleDomeChallenge Dome() => host.Dome ?? throw Wrong(i, $"'bracket {i.Name}' needs a tournament under way");
        int Draw() => host.Chance.Next(0x10000);
        switch (i.Name)
        {
            case "enough":
                lastNumber = BattleDomeChallenge.TeamSize;
                lastBanned = Listed(FrontierRules.BannedSeen(species => Data.PokemonDatabase.Get(species) is { } s && host.FrontierAct("seen", s.DexNumber, Subject) == 1));
                Result = FrontierRules.EnoughToEnter(host.Party, BattleDomeChallenge.TeamSize) ? 1 : 0;
                break;
            case "choose":
                Result = host.FrontierAct("enter", BattleDomeChallenge.TeamSize, Subject);
                afterBusy = () =>
                {
                    domeSlots = host.Entered.ToList();
                    Result = host.Answer == 1 && domeSlots.Count == BattleDomeChallenge.TeamSize ? 1 : 0;
                };
                break;
            case "check":
            {
                var chosen = domeSlots.Where(s => s >= 0 && s < host.Party.Count).Select(s => host.Party.Members[s]).ToList();
                Result = (int)FrontierRules.WhyNot(chosen);
                break;
            }
            case "begin":
                if (domeSlots.Count != BattleDomeChallenge.TeamSize) throw Wrong(i, "'bracket begin' needs the three to have been chosen ('bracket choose')");
                host.Dome = BattleDomeChallenge.Begin(records, domeSlots, Draw);
                records.ChallengesStarted++;
                lastNumber = host.Dome.StreakBefore;
                break;
            case "active":
                Result = host.Dome != null ? 1 : 0;
                break;
            case "round":
                Result = Dome().Round;
                lastNumber = Result + 1;
                break;
            case "show":
                Dome();
                host.Open(ScriptScreen.Bracket, Subject);
                afterBusy = () => Result = 1;
                break;
            case "opponent":
            {
                var opponent = Dome().Opponent ?? throw Wrong(i, "the player meets nobody in this round");
                towerFoes = new List<FrontierOpponent> { opponent.Opponent! };
                lastFoe = opponent.Title;
                host.FrontierAct("opponents", 0, Subject);
                Result = 1;
                break;
            }
            case "pick":
            {
                var dome = Dome();
                host.Open(ScriptScreen.Bracket, Subject, "pick");
                afterBusy = () => dome.Send(host.Answer == 1 ? host.Entered : Array.Empty<int>());
                break;
            }
            case "battle":
            {
                var dome = Dome();
                var foe = dome.OpponentAsTrainer(host.Party);
                towerFoes = new List<FrontierOpponent>();
                host.FrontierBattle(dome.TeamForBattle(host.Party), new List<Trainer> { foe }, null, doubles: false);
                afterBusy = () => Result = host.Outcome == BattleOutcome.Won ? 1 : 0;
                break;
            }
            case "won":
                Result = Dome().Won(records, host.Party, Draw) ? 1 : 0;
                lastNumber = Dome().Round + 1;
                break;
            case "lost":
                Dome().Lost(host.Party, Draw);
                break;
            case "finish":
                Dome().Finish(records, i.Other == "won");
                Result = lastNumber = records.Dome.Streak;
                break;
            case "points":
            {
                int points = Dome().PointsEarned;
                if (points > 0)
                {
                    records.GivePoints(points);
                    host.FrontierAct("points", 1, Subject);
                }
                Result = lastNumber = points;
                break;
            }
            case "streak":
                Result = lastNumber = records.Dome.Streak;
                break;
            case "best":
                Result = lastNumber = records.Dome.Best;
                break;
            case "end":
                host.Dome = null;
                domeSlots = Array.Empty<int>();
                towerFoes = new List<FrontierOpponent>();
                break;
        }
    }
}
