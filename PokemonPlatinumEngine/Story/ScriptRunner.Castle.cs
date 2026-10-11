using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The second command of plan 06 · R18c: <c>castle</c>, a Battle Castle challenge from the counter to the last
/// battle and back, on the original's script commands and <c>CallBattleCastleFunction</c>'s functions, named beside
/// each. What the Castle shares with the other facilities (Battle Points, the board, the print, saving) is
/// <c>frontier</c>'s (<see cref="Frontier"/>).
/// </summary>
public sealed partial class ScriptRunner
{
    private IReadOnlyList<int> castleSlots = Array.Empty<int>();

    /// <summary>
    /// The Battle Castle (<c>scripts_battle_castle.s</c>, <c>frontier_scripts_battle_castle.s</c>). At the counter:
    /// <c>enough</c> is 1 when the team holds three Pokémon of different species that may go in
    /// (<c>BC_LOBBY_FUNC_CHECK_PARTY_ELIGIBLE</c>; <c>{banned}</c>); <c>choose</c> has three chosen (RESULT 0 when
    /// backed out of) and <c>check</c> is 1 when two of them are of one species; <c>begin single|double</c> starts
    /// a challenge with them (<c>InitBattleCastle</c>: their items are taken, ten Castle Points are added);
    /// <c>enter</c> draws the round's trainers and the first one's three (<c>LoadTrainersForRound</c>) and counts a
    /// challenge begun; <c>pause</c>, <c>resume</c>, <c>forfeit</c>, <c>end</c>, <c>active</c>, <c>paused</c> and
    /// <c>way</c> as the Hall's. In the valet's room: <c>battles</c>, <c>next</c>, <c>nth</c> and <c>round</c> as
    /// the Hall's; <c>heard</c> is 1 once the valet has explained why items are taken (it is told now); <c>cp</c> is
    /// the Castle Points held, shown over the field until the script ends (<c>{cp}</c>); <c>self</c> opens the
    /// counter for one's own three and <c>foe</c> the one about the trainer's three (the script waits);
    /// <c>passable</c> is 1 where a battle may be passed and <c>pass</c> passes it for fifty Castle Points (RESULT 1,
    /// or 0 with too few); <c>draw</c> draws the next trainer's three (<c>BattleCastle_SetupNextOpponent</c>);
    /// <c>darach</c> is who comes next (0, 1 for the silver print, 2 for the gold) and <c>announce</c> 1 the one
    /// time the Castle Valet is to be announced; <c>fainted</c> is 1 when one of the three fainted in the battle
    /// just won. In the throne room: <c>opponent</c> stands the trainer in the room (<c>{foe}</c>); <c>battle</c> is
    /// fought with the three as they stand (RESULT 1 won, 0 lost); <c>won</c> counts the win and earns its Castle
    /// Points (RESULT and <c>{number}</c> what was earned); <c>done</c> is 1 once the round's seven are through;
    /// <c>brain</c> counts the Castle Valet met and <c>beat silver|gold</c> notes him beaten. At the end:
    /// <c>finish won|lost</c> writes the records; <c>points</c> hands over the Battle Points earned
    /// (<c>{number}</c>); <c>beaten</c> is who was beaten (0, 1, 2).
    /// </summary>
    private void Castle(Instruction i)
    {
        var records = host.Frontier;
        var castle = host.Castle;
        BattleCastleChallenge Challenge() => host.Castle ?? throw Wrong(i, $"'castle {i.Name}' needs a challenge under way");
        int Draw() => host.Chance.Next(0x10000);
        switch (i.Name)
        {
            case "enough":
                lastNumber = BattleCastleChallenge.TeamSize;
                lastBanned = Listed(FrontierRules.BannedSeen(species => Data.PokemonDatabase.Get(species) is { } s && host.FrontierAct("seen", s.DexNumber, Subject) == 1));
                Result = BattleCastleChallenge.EnoughToEnter(host.Party) ? 1 : 0;
                break;
            case "choose":
                Result = host.FrontierAct("enter", BattleCastleChallenge.TeamSize, Subject);
                afterBusy = () =>
                {
                    castleSlots = host.Entered.ToList();
                    Result = host.Answer == 1 && castleSlots.Count == BattleCastleChallenge.TeamSize ? 1 : 0;
                };
                break;
            case "check":
                Result = BattleCastleChallenge.ValidEntry(host.Party, castleSlots) ? 0 : 1;
                break;
            case "begin":
            {
                var way = i.Other == "double" ? FrontierChallenge.Double : FrontierChallenge.Single;
                if (!BattleCastleChallenge.ValidEntry(host.Party, castleSlots)) throw Wrong(i, "'castle begin' needs three Pokémon to have been chosen ('castle choose')");
                host.Castle = BattleCastleChallenge.Begin(records, way, host.Party, castleSlots);
                host.Story.SetVar(BattleCastleChallenge.WayVariable, (int)way);
                break;
            }
            case "enter":
            {
                var challenge = Challenge();
                if (!challenge.Resumed) challenge.DrawRound(Draw);
                records.ChallengesStarted++;
                host.FrontierAct("journal", (int)FrontierFacility.Castle, Subject);
                break;
            }
            case "pause":
                Challenge().Pause(records, host.Party);
                host.Castle = null;
                break;
            case "resume":
            {
                var way = records.Castle.Paused?.Way ?? FrontierChallenge.Single;
                if (records.Castle.Paused != null && !BattleCastleChallenge.StillValid(records, host.Party)) BattleCastleChallenge.Forfeit(records, way);
                var resumed = BattleCastleChallenge.Resume(records, host.Party);
                host.Castle = resumed;
                Result = resumed != null ? 1 : 0;
                if (resumed != null) host.Story.SetVar(BattleCastleChallenge.WayVariable, (int)resumed.Way);
                break;
            }
            case "forfeit":
                // DeleteActiveBattleCastleStreak, by the kind of challenge the lobby last sent in
                BattleCastleChallenge.Forfeit(records, castle?.Way ?? (host.Story.Var(BattleCastleChallenge.WayVariable) == 1 ? FrontierChallenge.Double : FrontierChallenge.Single));
                host.Castle = null;
                break;
            case "end":
                host.Castle = null;
                break;
            case "active":
                Result = castle != null ? 1 : 0;
                break;
            case "paused":
                Result = records.Castle.Paused != null ? 1 : 0;
                break;
            case "way":
                Result = castle != null ? (int)castle.Way : -1;
                break;

            case "battles":
                Result = lastNumber = Challenge().Battle;
                break;
            case "next":
                Result = lastNumber = Challenge().Battle + 1;
                break;
            case "nth":
                Result = lastNumber = Math.Min(FrontierRecords.Most, Challenge().Streak + 1);
                break;
            case "round":
                Result = castle?.Round ?? 0;
                break;
            case "heard":
                // BC_FUNC_CHECK_HAVE_HEARD_DARACHS_INTRO: asked, and from then on heard
                Result = records.Castle.HeardIntro ? 1 : 0;
                records.Castle.HeardIntro = true;
                break;
            case "cp":
                Result = lastNumber = Challenge().Points(records);
                host.FrontierAct("castlepoints", 1, Subject);
                break;
            case "self":
                Challenge();
                Result = host.FrontierAct("castleself", 0, Subject);
                break;
            case "foe":
                Challenge();
                Result = host.FrontierAct("castlefoe", 0, Subject);
                break;
            case "passable":
                Result = Challenge().MayPass ? 1 : 0;
                break;
            case "pass":
                Result = Challenge().Pass(records) == CastleAnswer.Done ? 1 : 0;
                break;
            case "draw":
                Challenge().DrawFoe(Draw);
                break;
            case "darach":
                Result = (int)Challenge().DarachNext;
                break;
            case "announce":
            {
                // _1A60: not when the challenge has just been taken up again, and only the once
                var challenge = Challenge();
                Result = challenge.DarachNext != FrontierPrint.None && !challenge.Resumed && !challenge.AnnounceValet() ? 1 : 0;
                break;
            }
            case "fainted":
                Result = castle is { Fainted: true } ? 1 : 0;
                break;

            case "opponent":
            {
                var foe = Challenge().Foe ?? throw Wrong(i, "no trainer has been drawn for the battle ('castle enter', 'castle draw')");
                towerFoes = new List<FrontierOpponent> { foe };
                lastFoe = $"{foe.Record.Class} {foe.Record.Name}";
                host.FrontierAct("opponents", 0, Subject);
                Result = 1;
                break;
            }
            case "battle":
            {
                var challenge = Challenge();
                if (challenge.Foe == null) throw Wrong(i, "no trainer has been drawn for the battle ('castle enter', 'castle draw')");
                towerFoes = new List<FrontierOpponent>();
                host.FrontierBattle(challenge.Team, new List<Trainer> { challenge.OpponentForBattle() }, null, doubles: challenge.Way == FrontierChallenge.Double);
                afterBusy = () => Result = host.Outcome == BattleOutcome.Won ? 1 : 0;
                break;
            }
            case "won":
                Result = lastNumber = Challenge().Won(records);
                break;
            case "done":
                Result = Challenge().RoundDone ? 1 : 0;
                break;
            case "brain":
                // RECORD_UNK_059, counted as the Frontier Brain walks in
                records.BrainsMet++;
                break;
            case "beat":
                Challenge().BeatValet(host.Story, i.Other == "gold" ? FrontierPrint.Gold : FrontierPrint.Silver);
                break;

            case "finish":
            {
                var challenge = Challenge();
                if (i.Other == "won")
                {
                    challenge.EndRound(records);
                    host.FrontierAct("score", BattleCastleChallenge.RoundScore, Subject);
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
                Result = castle != null ? (int)castle.BeatDarach : 0;
                break;
        }
    }
}
