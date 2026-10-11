using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The command of plan 06 · R18b: <c>factory</c>, a Battle Factory challenge from the counter to the last battle and
/// back, on the original's script commands and <c>CallBattleFactoryFunction</c>'s functions, named beside each. What
/// the Factory shares with the other facilities (Battle Points, the board, the print, saving) is <c>frontier</c>'s
/// (<see cref="Frontier"/>).
/// </summary>
public sealed partial class ScriptRunner
{
    // What the staff's last hint named: species, a move, a type
    private readonly string[] hints = { "", "", "" };

    /// <summary>
    /// The Battle Factory (<c>scripts_battle_factory.s</c>, <c>frontier_scripts_battle_factory.s</c>). At the counter:
    /// <c>begin single|double 50|open</c> starts a challenge (<c>InitBattleFactory</c>; the story's
    /// <c>VAR_BATTLE_FACTORY_CHALLENGE_TYPE</c> and <c>…_LEVEL</c> keep its kind); <c>enter</c> draws the round's
    /// trainers and the six on offer (<c>BattleFactory_LoadTrainersForRound</c>) and counts a challenge begun;
    /// <c>pause</c> puts the challenge down and <c>resume</c> takes it up (RESULT 0 when none waits);
    /// <c>forfeit</c> ends the streak of a challenge that was walked out on; <c>end</c> lets go of it; <c>active</c>
    /// is 1 while one is under way, <c>paused</c> 1 while one is put down, <c>way</c> 0 for a Single and 1 for a
    /// Double challenge (-1 with none), <c>level</c> 0 for Level 50 and 1 for the Open Level. In the corridor:
    /// <c>battles</c> is the battles won in the round and <c>next</c> the number of the one to come
    /// (<c>{number}</c>); <c>hint</c> tells of the trainer to come (RESULT 0 three species, 1 two, 2 a species and a
    /// move, 3 a move, 4 a type, 5 no type in common; <c>{hint:0}</c> to <c>{hint:2}</c>); <c>rent</c> has three of
    /// the six chosen (the script waits); <c>draw</c> draws the next trainer's three
    /// (<c>BattleFactory_SetupNextOpponent</c>); <c>trade</c> offers one of the beaten trainer's three for one in
    /// hand (RESULT 1 when a trade was made; <c>{hint:0}</c> what was given and <c>{hint:1}</c> what was taken);
    /// <c>stand</c> has the trainer to come stand ready (<c>BattleFactory_SetupNextOpponentsParty</c>);
    /// <c>head</c> is who comes next (0, 1 for the silver print, 2 for the gold) and <c>announce</c> 1 the one time
    /// the Factory Head is to be announced; <c>odds</c> is the number his machine comes up with, 10 to 99
    /// (<c>{number}</c>). In the battle room: <c>opponent</c> stands the trainer in the room
    /// (<c>{foe}</c>); <c>battle</c> is fought (RESULT 1 won, 0 lost); <c>won</c> counts the win (RESULT 1 once seven
    /// are beaten); <c>brain</c> counts the Factory Head met and <c>beat silver|gold</c> notes him beaten;
    /// <c>streak</c> is the streak as it stands. At the end: <c>finish won|lost</c> writes the records;
    /// <c>points</c> hands over the Battle Points earned (<c>{number}</c>); <c>beaten</c> is who was beaten (0, 1, 2).
    /// <c>{rental:0}</c> to <c>{rental:2}</c> in a line are the three in hand.
    /// </summary>
    private void Factory(Instruction i)
    {
        var records = host.Frontier;
        var factory = host.Factory;
        BattleFactoryChallenge Challenge() => host.Factory ?? throw Wrong(i, $"'factory {i.Name}' needs a challenge under way");
        int Draw() => host.Chance.Next(0x10000);
        switch (i.Name)
        {
            case "begin":
            {
                var way = i.Other.StartsWith("double") ? FrontierChallenge.Double : FrontierChallenge.Single;
                bool open = i.Other.EndsWith("open");
                host.Factory = BattleFactoryChallenge.Begin(records, way, open);
                host.Story.SetVar(BattleFactoryChallenge.WayVariable, (int)way);
                host.Story.SetVar(BattleFactoryChallenge.LevelVariable, open ? 1 : 0);
                break;
            }
            case "enter":
            {
                var challenge = Challenge();
                // BattleFactory_LoadTrainersAndRentalsForRound: a challenge taken up again has its trainers and its Pokémon already
                if (!challenge.Resumed) challenge.DrawRound(Draw);
                records.ChallengesStarted++;
                host.FrontierAct("journal", (int)FrontierFacility.Factory, Subject);
                break;
            }
            case "pause":
                Challenge().Pause(records);
                host.Factory = null;
                break;
            case "resume":
            {
                var resumed = BattleFactoryChallenge.Resume(records);
                host.Factory = resumed;
                Result = resumed != null ? 1 : 0;
                if (resumed != null)
                {
                    host.Story.SetVar(BattleFactoryChallenge.WayVariable, (int)resumed.Way);
                    host.Story.SetVar(BattleFactoryChallenge.LevelVariable, resumed.OpenLevel ? 1 : 0);
                }
                break;
            }
            case "forfeit":
            {
                // DeleteActiveBattleFactoryStreak, by the kind of challenge the lobby last sent in
                var way = factory?.Way ?? (host.Story.Var(BattleFactoryChallenge.WayVariable) == 1 ? FrontierChallenge.Double : FrontierChallenge.Single);
                bool open = factory?.OpenLevel ?? host.Story.Var(BattleFactoryChallenge.LevelVariable) == 1;
                BattleFactoryChallenge.Forfeit(records, way, open);
                host.Factory = null;
                break;
            }
            case "end":
                host.Factory = null;
                break;
            case "active":
                Result = factory != null ? 1 : 0;
                break;
            case "paused":
                Result = records.Factory.Paused != null ? 1 : 0;
                break;
            case "way":
                Result = factory != null ? (int)factory.Way : -1;
                break;
            case "level":
                Result = factory is { OpenLevel: true } ? 1 : 0;
                break;

            case "battles":
                Result = lastNumber = Challenge().Battle;
                break;
            case "next":
                Result = lastNumber = Challenge().Battle + 1;
                break;
            case "hint":
            {
                var (kind, names) = Challenge().Hint();
                for (int n = 0; n < hints.Length; n++) hints[n] = n < names.Count ? names[n] : "";
                Result = (int)kind;
                break;
            }
            case "rent":
            {
                var challenge = Challenge();
                Result = host.FrontierAct("rent", BattleFactoryChallenge.TeamSize, Subject);
                afterBusy = () =>
                {
                    Result = host.Answer == 1 && host.Entered.Count == BattleFactoryChallenge.TeamSize ? 1 : 0;
                    if (Result == 1) challenge.Rent(host.Entered);
                };
                break;
            }
            case "draw":
                Challenge().DrawNextFoe(Draw);
                break;
            case "trade":
            {
                var challenge = Challenge();
                Result = host.FrontierAct("trade", 0, Subject);
                afterBusy = () =>
                {
                    Result = host.Answer == 1 && host.Entered.Count == 2 ? 1 : 0;
                    if (Result != 1) return;
                    int mine = host.Entered[0], theirs = host.Entered[1];
                    hints[0] = challenge.Team[mine].Species;
                    hints[1] = challenge.Foe[theirs].Species;
                    challenge.Trade(records, mine, theirs);
                };
                break;
            }
            case "stand":
                Challenge().TakeNextFoe();
                break;
            case "head":
                Result = (int)Challenge().ThortonNext;
                break;
            case "announce":
            {
                // _14AE: not when the challenge has just been taken up again, and only the once
                var challenge = Challenge();
                Result = challenge.ThortonNext != FrontierPrint.None && !challenge.Resumed && !challenge.AnnounceHead() ? 1 : 0;
                break;
            }

            case "odds":
                // _1530: GetRandom 90 and ten more, the chance of winning the Factory Head's machine comes up with
                Result = lastNumber = 10 + host.Random(90);
                break;

            case "opponent":
            {
                towerFoes = new List<FrontierOpponent> { Challenge().Opponent() };
                lastFoe = $"{towerFoes[0].Record.Class} {towerFoes[0].Record.Name}";
                host.FrontierAct("opponents", 0, Subject);
                Result = 1;
                break;
            }
            case "battle":
            {
                var challenge = Challenge();
                var foe = challenge.Opponent().AsTrainer(challenge.Mind);
                towerFoes = new List<FrontierOpponent>();
                host.FrontierBattle(challenge.TeamForBattle(), new List<Trainer> { foe }, null, doubles: challenge.Way == FrontierChallenge.Double);
                afterBusy = () => Result = host.Outcome == BattleOutcome.Won ? 1 : 0;
                break;
            }
            case "won":
                Result = Challenge().Won(records) ? 1 : 0;
                break;
            case "brain":
                // RECORD_UNK_059, counted as the Frontier Brain walks in
                records.BrainsMet++;
                break;
            case "beat":
                Challenge().BeatHead(host.Story, i.Other == "gold" ? FrontierPrint.Gold : FrontierPrint.Silver);
                break;
            case "streak":
                Result = lastNumber = Challenge().Streak;
                break;

            case "finish":
            {
                var challenge = Challenge();
                if (i.Other == "won")
                {
                    challenge.EndRound(records);
                    host.FrontierAct("score", BattleFactoryChallenge.RoundScore, Subject);
                }
                else challenge.EndLost(records);
                break;
            }
            case "points":
                Result = lastNumber = Challenge().PointsEarned;
                records.GivePoints(Result);
                host.FrontierAct("points", 1, Subject);
                break;
            case "beaten":
                Result = factory != null ? (int)factory.BeatThorton : 0;
                break;
        }
    }
}
