using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The command of plan 06 · R18d: <c>arcade</c>, a Battle Arcade challenge from the counter to the last battle and
/// back, on the original's script commands and <c>CallBattleArcadeFunction</c>'s functions, named beside each. What
/// the Arcade shares with the other facilities (Battle Points, the board of records, the print, saving) is
/// <c>frontier</c>'s (<see cref="Frontier"/>).
/// </summary>
public sealed partial class ScriptRunner
{
    private IReadOnlyList<int> arcadeSlots = Array.Empty<int>();
    private string lastEvent = "", lastStruck = "";

    /// <summary>
    /// The Battle Arcade (<c>scripts_battle_arcade.s</c>, <c>frontier_scripts_battle_arcade.s</c>). At the counter:
    /// <c>enough</c> is 1 when the team holds three Pokémon of different species that may go in
    /// (<c>BA_LOBBY_FUNC_CHECK_PARTY_ELIGIBLE</c>; <c>{banned}</c>); <c>choose</c> has three chosen (RESULT 0 when
    /// backed out of) and <c>check</c> is 1 when two of them are of one species; <c>begin single|double</c> starts
    /// a challenge with them (<c>InitBattleArcade</c>: their items are taken); <c>enter</c> draws the round's
    /// trainers and the first one's three (<c>BattleArcade_LoadTrainersForRound</c>) and counts a challenge begun;
    /// <c>pause</c>, <c>resume</c>, <c>forfeit</c>, <c>end</c>, <c>active</c>, <c>paused</c> and <c>way</c> as the
    /// Castle's. In the game room: <c>battles</c>, <c>next</c>, <c>nth</c> and <c>round</c> as the Castle's;
    /// <c>draw</c> draws the next trainer's three (<c>BattleArcade_SetupNextOpponent</c>); <c>opponent</c> stands
    /// the trainer in the room (<c>{foe}</c>); <c>board</c> brings up the game board and plays it (the script
    /// waits), and carries out the event its cursor stopped on (<c>OpenBattleArcadeApp</c>, <c>ApplyArcadeEffect</c>):
    /// RESULT is the event's number, <c>{event}</c> its name, <c>{struck}</c> whose Pokémon it fell on (the trainer's
    /// name, or the player's) and <c>{item}</c> what it gave to hold; <c>event</c> is
    /// that number again (<c>BA_FUNC_GET_ACTIVE_EFFECT</c>); <c>side</c> is whom it fell on (0 the trainer, 1 the
    /// challenger, 2 the battle or the board, 3 a bonus); <c>immune</c> is 1 when everyone it fell on was kept from
    /// it (<c>BA_FUNC_CHECK_IMMUNE_TO_EFFECT</c>); <c>free</c> hands over the Battle Points an event gives outright
    /// (<c>BA_FUNC_GET_FREE_BP</c>; RESULT and <c>{number}</c> how many); <c>skip</c> counts a battle the board passed
    /// (<c>_1FFC</c>); <c>battle</c> is fought with both threes as the board left them, swapped where it said so,
    /// under the weather it set (RESULT 1 won, 0 lost), and afterwards the board's gifts are kept and the battle
    /// scored (<c>BattleArcade_CleanupBattle</c>); <c>won</c> counts the win; <c>done</c> is 1 once the round's seven
    /// are through; <c>dahlia</c> is who comes next (0, 1 for the silver print, 2 for the gold) and <c>announce</c>
    /// 1 the one time the Arcade Star is to be announced; <c>brain</c> counts her met and <c>beat silver|gold</c>
    /// notes her beaten. At the end: <c>finish won|lost</c> writes the records; <c>points</c> hands over the Battle
    /// Points earned (<c>{number}</c>); <c>beaten</c> is who was beaten (0, 1, 2).
    /// </summary>
    private void Arcade(Instruction i)
    {
        var records = host.Frontier;
        var arcade = host.Arcade;
        BattleArcadeChallenge Challenge() => host.Arcade ?? throw Wrong(i, $"'arcade {i.Name}' needs a challenge under way");
        int Draw() => host.Chance.Next(0x10000);
        switch (i.Name)
        {
            case "enough":
                lastNumber = BattleArcadeChallenge.TeamSize;
                lastBanned = Listed(FrontierRules.BannedSeen(species => Data.PokemonDatabase.Get(species) is { } s && host.FrontierAct("seen", s.DexNumber, Subject) == 1));
                Result = BattleArcadeChallenge.EnoughToEnter(host.Party) ? 1 : 0;
                break;
            case "choose":
                Result = host.FrontierAct("enter", BattleArcadeChallenge.TeamSize, Subject);
                afterBusy = () =>
                {
                    arcadeSlots = host.Entered.ToList();
                    Result = host.Answer == 1 && arcadeSlots.Count == BattleArcadeChallenge.TeamSize ? 1 : 0;
                };
                break;
            case "check":
                Result = BattleArcadeChallenge.ValidEntry(host.Party, arcadeSlots) ? 0 : 1;
                break;
            case "begin":
            {
                var way = i.Other == "double" ? FrontierChallenge.Double : FrontierChallenge.Single;
                if (!BattleArcadeChallenge.ValidEntry(host.Party, arcadeSlots)) throw Wrong(i, "'arcade begin' needs three Pokémon to have been chosen ('arcade choose')");
                host.Arcade = BattleArcadeChallenge.Begin(records, way, host.Party, arcadeSlots);
                host.Story.SetVar(BattleArcadeChallenge.WayVariable, (int)way);
                break;
            }
            case "enter":
            {
                var challenge = Challenge();
                if (!challenge.Resumed) challenge.DrawRound(Draw);
                records.ChallengesStarted++;
                host.FrontierAct("journal", (int)FrontierFacility.Arcade, Subject);
                break;
            }
            case "pause":
                Challenge().Pause(records, host.Party);
                host.Arcade = null;
                break;
            case "resume":
            {
                var way = records.Arcade.Paused?.Way ?? FrontierChallenge.Single;
                if (records.Arcade.Paused != null && !BattleArcadeChallenge.StillValid(records, host.Party)) BattleArcadeChallenge.Forfeit(records, way);
                var resumed = BattleArcadeChallenge.Resume(records, host.Party);
                host.Arcade = resumed;
                Result = resumed != null ? 1 : 0;
                if (resumed != null) host.Story.SetVar(BattleArcadeChallenge.WayVariable, (int)resumed.Way);
                break;
            }
            case "forfeit":
                // DeleteActiveBattleArcadeStreak, by the kind of challenge the lobby last sent in
                BattleArcadeChallenge.Forfeit(records, arcade?.Way ?? (host.Story.Var(BattleArcadeChallenge.WayVariable) == 1 ? FrontierChallenge.Double : FrontierChallenge.Single));
                host.Arcade = null;
                break;
            case "end":
                host.Arcade = null;
                break;
            case "active":
                Result = arcade != null ? 1 : 0;
                break;
            case "paused":
                Result = records.Arcade.Paused != null ? 1 : 0;
                break;
            case "way":
                Result = arcade != null ? (int)arcade.Way : -1;
                break;

            case "battles":
                Result = lastNumber = Challenge().Battle;
                break;
            case "next":
                Result = lastNumber = Challenge().Battle + 1;
                break;
            case "nth":
                // _32A0: the streak's own count of the battle to come
                Result = lastNumber = Math.Min(FrontierRecords.Most, Challenge().Streak + 1);
                break;
            case "round":
                Result = arcade?.Round ?? 0;
                break;
            case "draw":
                Challenge().DrawFoe(Draw);
                break;
            case "opponent":
            {
                var foe = Challenge().Foe ?? throw Wrong(i, "no trainer has been drawn for the battle ('arcade enter', 'arcade draw')");
                towerFoes = new List<FrontierOpponent> { foe };
                lastFoe = $"{foe.Record.Class} {foe.Record.Name}";
                host.FrontierAct("opponents", 0, Subject);
                Result = 1;
                break;
            }
            case "board":
            {
                var challenge = Challenge();
                if (challenge.Foe == null) throw Wrong(i, "no trainer has been drawn for the battle ('arcade enter', 'arcade draw')");
                challenge.OpenBoard(Draw);
                Result = host.FrontierAct("arcadeboard", 0, Subject);
                afterBusy = () =>
                {
                    var effect = challenge.TakeBoard(Draw);
                    lastEvent = ArcadeBoard.NameOf(effect);
                    // BA_FUNC_SET_NAME_OF_AFFECTED_TRAINER: the trainer to come for what falls on them, and the challenger otherwise
                    lastStruck = ArcadeBoard.CategoryOf(effect) == ArcadeCategory.Foe && challenge.Foe is { } struck ? $"{struck.Record.Class} {struck.Record.Name}" : "{player}";
                    if (challenge.Given != null) lastItem = challenge.Given;
                    Result = (int)effect;
                };
                break;
            }
            case "event":
                Result = (int)Challenge().Effect;
                break;
            case "side":
                Result = (int)ArcadeBoard.CategoryOf(Challenge().Effect);
                break;
            case "immune":
                Result = Challenge().Immune ? 1 : 0;
                break;
            case "free":
                Result = lastNumber = Challenge().GiveFreePoints(records);
                host.FrontierAct("points", 1, Subject);
                break;
            case "skip":
                Challenge().Skipped();
                towerFoes = new List<FrontierOpponent>();
                break;
            case "battle":
            {
                var challenge = Challenge();
                if (challenge.Foe == null) throw Wrong(i, "no trainer has been drawn for the battle ('arcade enter', 'arcade draw')");
                towerFoes = new List<FrontierOpponent>();
                var fought = challenge.ForBattle();
                host.FrontierBattle(fought.Mine, new List<Trainer> { fought.Theirs }, null, doubles: challenge.Way == FrontierChallenge.Double, fought.Conditions);
                afterBusy = () =>
                {
                    challenge.AfterBattle(fought, host.BattleTurns);
                    Result = host.Outcome == BattleOutcome.Won ? 1 : 0;
                };
                break;
            }
            case "won":
                Challenge().Won(records);
                break;
            case "done":
                Result = Challenge().RoundDone ? 1 : 0;
                break;
            case "dahlia":
                Result = (int)Challenge().DahliaNext;
                break;
            case "announce":
            {
                // _2F86: not when the challenge has just been taken up again, and only the once
                var challenge = Challenge();
                Result = challenge.DahliaNext != FrontierPrint.None && !challenge.Resumed && !challenge.AnnounceStar() ? 1 : 0;
                break;
            }
            case "brain":
                // RECORD_UNK_059, counted as the Frontier Brain walks in
                records.BrainsMet++;
                break;
            case "beat":
                Challenge().BeatStar(host.Story, i.Other == "gold" ? FrontierPrint.Gold : FrontierPrint.Silver);
                break;

            case "finish":
            {
                var challenge = Challenge();
                if (i.Other == "won")
                {
                    challenge.EndRound(records);
                    host.FrontierAct("score", BattleArcadeChallenge.RoundScore, Subject);
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
                Result = arcade != null ? (int)arcade.BeatDahlia : 0;
                break;
        }
    }
}
