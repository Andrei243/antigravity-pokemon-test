using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The command of plan 14 · B11: <c>tournament</c>, the World Tournament from the desk to the final and back
/// (<see cref="Models.Tournament"/> over a <see cref="Models.Frontier.Bracket"/> of eight). What it shares with the
/// Frontier's facilities (the team entered, a battle of the Frontier, Battle Points) goes through <c>frontier</c>'s
/// host words (<see cref="IScriptHost.FrontierAct"/>, <see cref="IScriptHost.FrontierBattle"/>).
/// </summary>
public sealed partial class ScriptRunner
{
    // The event chosen at the desk, and the places in the team of the three entered, before a tournament is begun
    private TournamentEventRecord? tournamentEvent;
    private IReadOnlyList<int> tournamentSlots = Array.Empty<int>();

    /// <summary>
    /// At the desk: <c>events</c> opens the events (the screen <see cref="ScriptScreen.Tournament"/>, the counter
    /// <c>events</c>) and waits, RESULT the event chosen counted from 1 (Sinnoh's Leaders 1, the Champion's event 2), or
    /// 0 when backed out of or when the event isn't open yet; <c>enough</c> is 1 when the team holds three that could go
    /// in together (<c>{banned}</c> as the Frontier's); <c>choose</c> has the three chosen (RESULT 1, or 0) and
    /// <c>check</c> holds them to the clauses (0, or 1 for two of a species and 2 for two holding the same item);
    /// <c>begin</c> seats the event's eight and draws the bracket; <c>titles</c> is every title won (<c>{number}</c>).
    /// In the hall: <c>active</c>, <c>round</c> (0 to 2, 2 the final; <c>{number}</c> 1 to 3), <c>show</c> (the bracket
    /// looked at; the script waits), <c>opponent</c> (the round's opponent stood on the stage as <c>opponent</c> in
    /// their look; <c>{foe}</c>), <c>battle</c> (three against three at level 50: RESULT 1 won, 0 lost), <c>won</c>
    /// (the round decided and the rest of it fought headless: RESULT 1 when it was the final) and <c>lost</c>. At the
    /// end: <c>finish won|lost</c> writes the record, <c>points</c> hands over the title's Battle Points (RESULT and
    /// <c>{number}</c>), and <c>end</c> lets the tournament go.
    /// </summary>
    private void WorldTournament(Instruction i)
    {
        var records = host.Frontier;
        Tournament Under() => host.Tournament ?? throw Wrong(i, $"'tournament {i.Name}' needs a tournament under way");
        int Draw() => host.Chance.Next(0x10000);
        switch (i.Name)
        {
            case "events":
                host.Open(ScriptScreen.Tournament, Subject, "events");
                afterBusy = () =>
                {
                    var events = TournamentDatabase.Events;
                    int chosen = host.Answer;
                    tournamentEvent = chosen >= 1 && chosen <= events.Count && Tournament.IsOpen(events[chosen - 1], records.Tournament) ? events[chosen - 1] : null;
                    Result = tournamentEvent == null ? 0 : chosen;
                };
                break;
            case "enough":
                lastNumber = Tournament.TeamSize;
                lastBanned = Listed(FrontierRules.BannedSeen(species => PokemonDatabase.Get(species) is { } s && host.FrontierAct("seen", s.DexNumber, Subject) == 1));
                Result = FrontierRules.EnoughToEnter(host.Party, Tournament.TeamSize) ? 1 : 0;
                break;
            case "choose":
                Result = host.FrontierAct("enter", Tournament.TeamSize, Subject);
                afterBusy = () =>
                {
                    tournamentSlots = host.Entered.ToList();
                    Result = host.Answer == 1 && tournamentSlots.Count == Tournament.TeamSize ? 1 : 0;
                };
                break;
            case "check":
            {
                var chosen = tournamentSlots.Where(s => s >= 0 && s < host.Party.Count).Select(s => host.Party.Members[s]).ToList();
                Result = (int)FrontierRules.WhyNot(chosen);
                break;
            }
            case "begin":
                if (tournamentSlots.Count != Tournament.TeamSize) throw Wrong(i, "'tournament begin' needs the three to have been chosen ('tournament choose')");
                var ev = tournamentEvent ?? TournamentDatabase.Events.FirstOrDefault(e => Tournament.IsOpen(e, records.Tournament))
                    ?? throw Wrong(i, "there is no event of the World Tournament to enter");
                host.Tournament = Tournament.Begin(records, ev, host.Party, tournamentSlots, Draw);
                records.ChallengesStarted++;
                break;
            case "titles":
                Result = lastNumber = records.Tournament.Titles.Values.Sum();
                break;
            case "active":
                Result = host.Tournament != null ? 1 : 0;
                break;
            case "round":
                Result = Under().Round;
                lastNumber = Result + 1;
                break;
            case "show":
                Under();
                host.Open(ScriptScreen.Tournament, Subject, "bracket");
                afterBusy = () => Result = 1;
                break;
            case "opponent":
            {
                var opponent = Under().Opponent ?? throw Wrong(i, "the player meets nobody in this round");
                lastFoe = opponent.Title;
                host.FrontierAct("tournamentfoe", 0, Subject);
                Result = 1;
                break;
            }
            case "battle":
            {
                var t = Under();
                host.FrontierBattle(t.TeamForBattle(), new List<Trainer> { t.OpponentAsTrainer() }, null, doubles: false, new FrontierConditions(Tournament: true));
                afterBusy = () => Result = host.Outcome == BattleOutcome.Won ? 1 : 0;
                break;
            }
            case "won":
                Result = Under().Won(records, Draw) ? 1 : 0;
                lastNumber = Under().Round + 1;
                break;
            case "lost":
                Under().Lost(Draw);
                break;
            case "finish":
                Under().Finish(records, i.Other == "won");
                Result = lastNumber = records.Tournament.Titles.Values.Sum();
                break;
            case "points":
            {
                int points = Under().PointsEarned;
                if (points > 0)
                {
                    records.GivePoints(points);
                    host.FrontierAct("points", 1, Subject);
                }
                Result = lastNumber = points;
                break;
            }
            case "end":
                host.Tournament = null;
                tournamentSlots = Array.Empty<int>();
                tournamentEvent = null;
                break;
        }
    }
}
