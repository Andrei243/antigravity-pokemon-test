using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The first command of plan 06 · R18c: <c>hall</c>, a Battle Hall challenge from the counter to the last battle and
/// back, on the original's script commands and <c>CallBattleHallFunction</c>'s functions, named beside each. What the
/// Hall shares with the other facilities (Battle Points, the board, the print, saving) is <c>frontier</c>'s
/// (<see cref="Frontier"/>).
/// </summary>
public sealed partial class ScriptRunner
{
    // Who was entered, the type last chosen and the rank it stands at, and what the record keeper counted
    private string lastEntrant = "", lastType = "";
    private int lastRank;
    private IReadOnlyList<int> hallSlots = Array.Empty<int>();
    private (int Total, int Passed, int Coming, int Points) keeperTold;

    /// <summary>
    /// The Battle Hall (<c>scripts_battle_hall.s</c>, <c>frontier_scripts_battle_hall.s</c>). At the counter:
    /// <c>enough single|double</c> is 1 when the team holds a Pokémon of level 30 or more that may go in, or two of
    /// one species (<c>BH_LOBBY_FUNC_CHECK_PARTY_ELIGIBLE</c>; <c>{banned}</c>); <c>choose single|double</c> has them
    /// chosen (RESULT 1, 0 when backed out of, 2 for two of different species; <c>{entrant}</c>); <c>streak
    /// single|double</c> is 0 with no streak running, 1 with one of the species chosen and 2 with another's
    /// (<c>{thing}</c>), which <c>drop single|double</c> lets go of (<c>BH_LOBBY_FUNC_DELETE_ACTIVE_STREAK</c>);
    /// <c>begin single|double</c> starts a challenge with the Pokémon chosen (<c>InitBattleHall</c>); <c>enter</c>
    /// counts a challenge begun; <c>pause</c> puts the challenge down and <c>resume</c> takes it up (RESULT 0 when
    /// none waits or its Pokémon is no longer there, which ends the streak); <c>forfeit</c> ends the streak of a
    /// challenge that was walked out on; <c>end</c> lets go of it; <c>active</c>, <c>paused</c> and <c>way</c> as the
    /// Factory's. On the stage: <c>battles</c> is the battles won in the round, <c>next</c> the number of the one to
    /// come and <c>nth</c> the streak's (<c>{number}</c>); <c>round</c> the rounds the streak has come through;
    /// <c>board</c> opens the board of types and draws the opponent for the one chosen (the script waits;
    /// <c>{type}</c>, <c>{rank}</c>); <c>argenta</c> is who comes next (0, 1 for the silver print, 2 for the gold);
    /// <c>opponent</c> stands the trainer on the stage (<c>{foe}</c>); <c>battle</c> is fought (RESULT 1 won, 0
    /// lost); <c>won</c> counts the win and raises the type's rank (RESULT 1 once ten are beaten; <c>{rank}</c>);
    /// <c>rank</c> is the wins over the type last chosen, 0 to 10; <c>brain</c> counts the Hall Matron met and
    /// <c>beat silver|gold</c> notes her beaten. At the end: <c>finish won|lost</c> writes the records and the
    /// species' own (RESULT 1 when that moved); <c>points</c> hands over the Battle Points earned
    /// (<c>{number}</c>); <c>beaten</c> is who was beaten (0, 1, 2). In the lobby: <c>keeper</c> asks the record
    /// keeper (RESULT 0 nothing won yet, 1 a mark passed and its Battle Points handed over, 2 the next still to
    /// come, 3 every mark passed) and <c>told total|passed|coming|points</c> are what was counted
    /// (<c>{number}</c>); <c>fan</c> is which of ten things the fan says, by the species with a record, and
    /// <c>fanstage</c> how near the counter the fan stands (0 to 2); <c>visitors</c> how many well-known visitors may
    /// turn up (0, 1, 2 or 4); <c>species</c> the species with a record.
    /// </summary>
    private void Hall(Instruction i)
    {
        var records = host.Frontier;
        var hall = host.Hall;
        BattleHallChallenge Challenge() => host.Hall ?? throw Wrong(i, $"'hall {i.Name}' needs a challenge under way");
        int Draw() => host.Chance.Next(0x10000);
        FrontierChallenge Way() => i.Other == "double" ? FrontierChallenge.Double : FrontierChallenge.Single;
        switch (i.Name)
        {
            case "enough":
                lastBanned = Listed(FrontierRules.BannedSeen(species => Data.PokemonDatabase.Get(species) is { } s && host.FrontierAct("seen", s.DexNumber, Subject) == 1));
                Result = BattleHallChallenge.EnoughToEnter(host.Party, Way()) ? 1 : 0;
                break;
            case "choose":
            {
                var way = Way();
                int needed = way == FrontierChallenge.Double ? 2 : 1;
                Result = host.FrontierAct("enterhall", needed, Subject);
                afterBusy = () =>
                {
                    hallSlots = host.Entered.ToList();
                    if (host.Answer != 1 || hallSlots.Count != needed) { Result = 0; return; }
                    lastEntrant = host.Party.Members[hallSlots[0]].Species.Name;
                    Result = BattleHallChallenge.ValidEntry(host.Party, hallSlots, way) ? 1 : 2;
                };
                break;
            }
            case "streak":
            {
                string? species = BattleHallChallenge.StreakSpecies(records, Way());
                lastThing = species ?? "";
                Result = species == null ? 0 : species == lastEntrant ? 1 : 2;
                break;
            }
            case "drop":
                BattleHallChallenge.Forfeit(records, Way());
                break;
            case "begin":
            {
                var way = Way();
                if (!BattleHallChallenge.ValidEntry(host.Party, hallSlots, way)) throw Wrong(i, "'hall begin' needs the Pokémon to have been chosen ('hall choose')");
                host.Hall = BattleHallChallenge.Begin(records, way, hallSlots);
                host.Story.SetVar(BattleHallChallenge.WayVariable, (int)way);
                break;
            }
            case "enter":
                Challenge();
                records.ChallengesStarted++;
                host.FrontierAct("journal", (int)FrontierFacility.Hall, Subject);
                break;
            case "pause":
                Challenge().Pause(records, host.Party);
                host.Hall = null;
                break;
            case "resume":
            {
                var way = records.Hall.Paused?.Way ?? FrontierChallenge.Single;
                if (records.Hall.Paused != null && !BattleHallChallenge.StillValid(records, host.Party)) BattleHallChallenge.Forfeit(records, way);
                var resumed = BattleHallChallenge.Resume(records);
                host.Hall = resumed;
                Result = resumed != null ? 1 : 0;
                if (resumed == null) break;
                host.Story.SetVar(BattleHallChallenge.WayVariable, (int)resumed.Way);
                lastEntrant = host.Party.Members[resumed.Slots[0]].Species.Name;
                break;
            }
            case "forfeit":
                // DeleteActiveBattleHallStreak, by the kind of challenge the lobby last sent in
                BattleHallChallenge.Forfeit(records, hall?.Way ?? (host.Story.Var(BattleHallChallenge.WayVariable) == 1 ? FrontierChallenge.Double : FrontierChallenge.Single));
                host.Hall = null;
                break;
            case "end":
                host.Hall = null;
                break;
            case "active":
                Result = hall != null ? 1 : 0;
                break;
            case "paused":
                Result = records.Hall.Paused != null ? 1 : 0;
                break;
            case "way":
                Result = hall != null ? (int)hall.Way : -1;
                break;

            case "battles":
                Result = lastNumber = Challenge().Battle;
                break;
            case "next":
                Result = lastNumber = Challenge().Battle + 1;
                break;
            case "nth":
                // _13C3: the streak's own count of the battle to come, as the announcer calls it
                Result = lastNumber = Math.Min(FrontierRecords.Most, Challenge().Streak + 1);
                break;
            case "round":
                Result = hall?.Round ?? 0;
                break;
            case "board":
            {
                var challenge = Challenge();
                challenge.CapRanks();
                Result = host.FrontierAct("board", 0, Subject);
                afterBusy = () =>
                {
                    int cell = host.Entered.Count > 0 ? host.Entered[0] : -1;
                    if (cell < 0 || cell >= BattleHallChallenge.Board.Length || challenge.Beaten(cell)) throw Wrong(i, "the Battle Hall's board gave no type that can be chosen");
                    challenge.Choose(cell, host.Party, Draw);
                    lastType = BattleHallChallenge.Board[cell].ToString();
                    lastRank = challenge.ShownRank(cell);
                    Result = 1;
                };
                break;
            }
            case "argenta":
                Result = (int)Challenge().ArgentaNext;
                break;
            case "opponent":
            {
                towerFoes = new List<FrontierOpponent> { Challenge().MakeOpponent(host.Party, Draw) };
                lastFoe = $"{towerFoes[0].Record.Class} {towerFoes[0].Record.Name}";
                host.FrontierAct("opponents", 0, Subject);
                Result = 1;
                break;
            }
            case "battle":
            {
                var challenge = Challenge();
                var foe = (challenge.Standing ?? challenge.MakeOpponent(host.Party, Draw)).AsTrainer(challenge.Mind);
                towerFoes = new List<FrontierOpponent>();
                host.FrontierBattle(challenge.TeamForBattle(host.Party), new List<Trainer> { foe }, null, doubles: challenge.Way == FrontierChallenge.Double);
                afterBusy = () => Result = host.Outcome == BattleOutcome.Won ? 1 : 0;
                break;
            }
            case "won":
            {
                var challenge = Challenge();
                Result = challenge.Won(records) ? 1 : 0;
                if (challenge.Cell >= 0) lastRank = challenge.Ranks[challenge.Cell];
                break;
            }
            case "rank":
                Result = lastRank = hall is { Cell: >= 0 } ? hall.Ranks[hall.Cell] : 0;
                break;
            case "brain":
                // RECORD_UNK_059, counted as the Frontier Brain walks in
                records.BrainsMet++;
                break;
            case "beat":
                Challenge().BeatMatron(host.Story, i.Other == "gold" ? FrontierPrint.Gold : FrontierPrint.Silver);
                break;

            case "finish":
            {
                var challenge = Challenge();
                if (i.Other == "won")
                {
                    challenge.EndRound(records, host.Party);
                    host.FrontierAct("score", BattleHallChallenge.RoundScore, Subject);
                }
                else challenge.EndLost(records, host.Party);
                // _109F: the species' own record, as the challenge is left
                Result = challenge.UpdateRecord(records) ? 1 : 0;
                break;
            }
            case "points":
                Result = lastNumber = Challenge().PointsEarned;
                records.GivePoints(Result);
                host.FrontierAct("points", 1, Subject);
                break;
            case "beaten":
                Result = hall != null ? (int)hall.BeatArgenta : 0;
                break;

            case "keeper":
            {
                int next = host.Story.Var(BattleHallChallenge.MilestoneVariable);
                var told = BattleHallChallenge.AskKeeper(records, ref next);
                host.Story.SetVar(BattleHallChallenge.MilestoneVariable, next);
                keeperTold = (told.Total, told.Passed, told.Coming, told.Points);
                lastNumber = told.Total;
                if (told.Points > 0) host.FrontierAct("points", 1, Subject);
                Result = (int)told.Answer;
                break;
            }
            case "told":
                Result = lastNumber = i.Other switch { "passed" => keeperTold.Passed, "coming" => keeperTold.Coming, "points" => keeperTold.Points, _ => keeperTold.Total };
                break;
            case "fan":
                Result = BattleHallChallenge.FanTalk(records);
                break;
            case "fanstage":
                Result = BattleHallChallenge.FanStage(records);
                break;
            case "visitors":
                Result = BattleHallChallenge.Visitors(records);
                break;
            case "species":
                Result = lastNumber = records.Hall.SpeciesWithRecords;
                break;
        }
    }
}
